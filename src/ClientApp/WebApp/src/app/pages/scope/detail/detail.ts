import { Component, OnInit, signal } from '@angular/core';
import { CommonModules, BaseMatModules } from 'src/app/share/shared-modules';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { ApiClient } from 'src/app/services/api/api-client';
import { ScopeDetailDto } from 'src/app/services/api/models/access-mod/scope-detail-dto.model';
import { ScopeEditComponent } from '../edit/edit';
import { ConfirmDialogComponent } from 'src/app/share/components/confirm-dialog/confirm-dialog.component';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-detail',
  imports: [
    ...CommonModules,
    ...BaseMatModules,
    MatCardModule,
    MatProgressSpinnerModule,
    MatChipsModule
  ],
  templateUrl: './detail.html',
  styleUrls: ['./detail.scss']
})
export class ScopeDetailComponent implements OnInit {
  scope = signal<ScopeDetailDto | null>(null);
  
  isLoading = false;
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
    this.isLoading = true;
    this.api.scopes.getDetail(this.scopeId!).subscribe({
      next: (scope) => {
        this.scope.set(scope);
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.snackBar.open(
          this.translate.instant('error.loadScopeFailed'),
          this.translate.instant('common.close'),
          { duration: 3000 }
        );
        this.router.navigate(['/scope']);
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
        title: this.translate.instant('scope.deleteConfirmTitle'),
        message: this.translate.instant('scope.deleteConfirmDetailMessage', { name: scope.displayName })
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.api.scopes.deleteScope(this.scopeId!).subscribe({
          next: () => {
            this.snackBar.open(
              this.translate.instant('scope.deleteSuccess'),
              this.translate.instant('common.close'),
              { duration: 3000 }
            );
            this.router.navigate(['/scope']);
          },
          error: () => {
            this.snackBar.open(
              this.translate.instant('error.deleteScopeFailed'),
              this.translate.instant('common.close'),
              { duration: 3000 }
            );
          }
        });
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/scope']);
  }
}
