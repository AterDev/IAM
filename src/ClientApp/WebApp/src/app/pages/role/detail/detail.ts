import { Component, OnInit, signal } from '@angular/core';
import { CommonModules, BaseMatModules } from 'src/app/share/shared-modules';
import { ActivatedRoute, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import { TranslateService } from '@ngx-translate/core';
import { ApiClient } from 'src/app/services/api/api-client';
import { RoleDetailDto } from 'src/app/services/api/models/identity-mod/role-detail-dto.model';
import { ConfirmDialogComponent } from 'src/app/share/components/confirm-dialog/confirm-dialog.component';
import { RoleEditComponent } from '../edit/edit';
import { RolePermissionsComponent } from '../permissions/permissions';
import { MatCardModule } from '@angular/material/card';
import { AppLoadingComponent } from 'src/app/share/components/loading/loading';

@Component({
  selector: 'app-detail',
  imports: [
    ...CommonModules,
    ...BaseMatModules,
  MatCardModule,
  AppLoadingComponent
  ],
  templateUrl: './detail.html',
  styleUrls: ['./detail.scss']
})
export class RoleDetailComponent implements OnInit {
  role = signal<RoleDetailDto | null>(null);
  isLoading = signal(true);

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private api: ApiClient,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadRole(id);
    }
  }

  loadRole(id: string): void {
  this.isLoading.set(true);
    this.api.roles.getDetail(id).subscribe({
      next: (data) => {
        this.role.set(data);
  this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Failed to load role:', error);
        this.snackBar.open(
          this.translate.instant('error.loadRoleDetailFailed'),
          this.translate.instant('common.close'),
          { duration: 3000 }
        );
  this.isLoading.set(false);
      }
    });
  }

  openEditDialog(): void {
    const currentRole = this.role();
    if (!currentRole) {
      return;
    }

    const dialogRef = this.dialog.open(RoleEditComponent, {
      width: '500px',
      data: currentRole
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadRole(currentRole.id);
      }
    });
  }

  openPermissionsDialog(): void {
    const currentRole = this.role();
    if (!currentRole) {
      return;
    }

    const dialogRef = this.dialog.open(RolePermissionsComponent, {
      width: '700px',
      maxHeight: '80vh',
      data: currentRole
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadRole(currentRole.id);
      }
    });
  }

  deleteRole(): void {
    const currentRole = this.role();
    if (!currentRole) {
      return;
    }

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: this.translate.instant('dialog.confirmDelete.title'),
        message: this.translate.instant('dialog.confirmDelete.message')
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.api.roles.deleteRole(currentRole.id, false).subscribe({
          next: () => {
            this.snackBar.open(
              this.translate.instant('success.roleDeleted'),
              this.translate.instant('common.close'),
              { duration: 3000 }
            );
            this.router.navigate(['/role']);
          },
          error: (error) => {
            console.error('Failed to delete role:', error);
            this.snackBar.open(
              this.translate.instant('error.deleteRoleFailed'),
              this.translate.instant('common.close'),
              { duration: 3000 }
            );
          }
        });
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/role']);
  }
}
