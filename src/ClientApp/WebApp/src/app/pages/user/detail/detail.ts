import { Component, OnInit, signal } from '@angular/core';
import { CommonModules, BaseMatModules } from 'src/app/share/shared-modules';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { ApiClient } from 'src/app/services/api/api-client';
import { UserDetailDto } from 'src/app/services/api/models/iammod/user-detail-dto.model';
import { UserEditComponent } from '../edit/edit';
import { ConfirmDialogComponent } from 'src/app/share/components/confirm-dialog/confirm-dialog.component';
import { AppLoadingComponent } from 'src/app/share/components/loading/loading';
import { I18N_KEYS } from 'src/app/share/i18n-keys';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-detail',
  imports: [
    ...CommonModules,
    ...BaseMatModules,
    MatCardModule,
    MatProgressSpinnerModule,
  MatChipsModule,
  AppLoadingComponent
  ],
  templateUrl: './detail.html',
  styleUrls: ['./detail.scss']
})
export class UserDetailComponent implements OnInit {
  readonly i18n = I18N_KEYS;
  // Keep signals only for template-reactive values
  user = signal<UserDetailDto | null>(null);

  isLoading = signal(false);
  userId?: string;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private api: ApiClient,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.userId = this.route.snapshot.paramMap.get('id') || '';
    if (this.userId) {
      this.loadUser();
    }
  }

  loadUser(): void {
  this.isLoading.set(true);
    this.api.users.getDetail(this.userId!).subscribe({
      next: (user) => {
  this.user.set(user);
  this.isLoading.set(false);
      },
      error: () => {
  this.isLoading.set(false);
        this.snackBar.open(this.translate.instant(this.i18n.user.loadFailed), this.translate.instant(this.i18n.common.close), { duration: 3000 });
        this.router.navigate(['/user/list']);
      }
    });
  }

  openEditDialog(): void {
    const dialogRef = this.dialog.open(UserEditComponent, {
      width: '600px',
      data: { userId: this.userId }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadUser();
      }
    });
  }

  toggleUserStatus(): void {
    const user = this.user();
    if (!user) {
      return;
    }

    const lockoutEnd = user.lockoutEnabled ? null : new Date(Date.now() + 365 * 24 * 60 * 60 * 1000);

    this.api.users.updateStatus(this.userId!, lockoutEnd as any).subscribe({
      next: () => {
        this.snackBar.open(
          user.lockoutEnabled ? this.translate.instant(this.i18n.user.statusUnlocked) : this.translate.instant(this.i18n.user.statusLocked),
          this.translate.instant(this.i18n.common.close),
          { duration: 3000 }
        );
        this.loadUser();
      },
      error: () => {
        this.snackBar.open(this.translate.instant(this.i18n.user.statusUpdateFailed), this.translate.instant(this.i18n.common.close), { duration: 3000 });
      }
    });
  }

  deleteUser(): void {
    const user = this.user();
    if (!user) {
      return;
    }

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: this.translate.instant(this.i18n.user.deleteConfirmTitle),
        message: this.translate.instant(this.i18n.user.deleteConfirmMessage, { userName: user.userName })
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.api.users.deleteUser(this.userId!, false).subscribe({
          next: () => {
            this.snackBar.open(this.translate.instant(this.i18n.user.deletedSuccess), this.translate.instant(this.i18n.common.close), { duration: 3000 });
            this.router.navigate(['/user/list']);
          },
          error: () => {
            this.snackBar.open(this.translate.instant(this.i18n.user.deleteFailed), this.translate.instant(this.i18n.common.close), { duration: 3000 });
          }
        });
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/user/list']);
  }
}
