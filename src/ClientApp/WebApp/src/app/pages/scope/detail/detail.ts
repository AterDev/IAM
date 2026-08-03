import { Component, OnInit, signal } from '@angular/core';
import { CommonModules, BaseMatModules } from 'src/app/share/shared-modules';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { ApiClient } from 'src/app/services/api/api-client';
import { ScopeDetailDto } from 'src/app/services/api/models/iammod/scope-detail-dto.model';
import { ScopeEditComponent } from '../edit/edit';
import { ConfirmDialogComponent } from 'src/app/share/components/confirm-dialog/confirm-dialog.component';
import { TranslateService } from '@ngx-translate/core';
import { AppLoadingComponent } from 'src/app/share/components/loading/loading';
import { I18N_KEYS } from 'src/app/share/i18n-keys';

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
export class ScopeDetailComponent implements OnInit {
  readonly i18n = I18N_KEYS;
  scope = signal<ScopeDetailDto | null>(null);

  isLoading = signal(false);
  scopeId?: string;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private api: ApiClient,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.scopeId = this.route.snapshot.paramMap.get('id') || '';
    if (this.scopeId) {
      this.loadScope();
    }
  }

  loadScope(): void {
  this.isLoading.set(true);
    this.api.scopes.getDetail(this.scopeId!).subscribe({
      next: (scope) => {
  this.scope.set(scope);
  this.isLoading.set(false);
      },
      error: () => {
  this.isLoading.set(false);
        this.snackBar.open(
          this.translate.instant(this.i18n.error.loadScopeFailed),
          this.translate.instant(this.i18n.common.close),
          { duration: 3000 }
        );
        this.router.navigate(['/scope/list']);
      }
    });
  }

  openEditDialog(): void {
    const dialogRef = this.dialog.open(ScopeEditComponent, {
      width: '600px',
      data: { scopeId: this.scopeId }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadScope();
      }
    });
  }

  deleteScope(): void {
    const scope = this.scope();
    if (!scope) {
      return;
    }

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: this.translate.instant(this.i18n.scope.deleteConfirmTitle),
        message: this.translate.instant(this.i18n.scope.deleteConfirmDetailMessage, { name: scope.displayName })
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.api.scopes.deleteScope(this.scopeId!).subscribe({
          next: () => {
            this.snackBar.open(
              this.translate.instant(this.i18n.scope.deleteSuccess),
              this.translate.instant(this.i18n.common.close),
              { duration: 3000 }
            );
            this.router.navigate(['/scope/list']);
          },
          error: () => {
            this.snackBar.open(
              this.translate.instant(this.i18n.error.deleteScopeFailed),
              this.translate.instant(this.i18n.common.close),
              { duration: 3000 }
            );
          }
        });
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/scope/list']);
  }
}
