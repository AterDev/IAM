import { Component, OnInit, signal } from '@angular/core';
import { CommonModules, BaseMatModules, CommonFormModules } from 'src/app/share/shared-modules';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatChipsModule } from '@angular/material/chips';
import { FormsModule } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { ApiClient } from 'src/app/services/api/api-client';
import { ScopeItemDto } from 'src/app/services/api/models/iammod/scope-item-dto.model';
import { PageList } from 'src/app/services/api/models/perigon/page-list.model';
import { ScopeAddComponent } from '../add/add';
import { ConfirmDialogComponent } from 'src/app/share/components/confirm-dialog/confirm-dialog.component';
import { I18N_KEYS } from 'src/app/share/i18n-keys';

@Component({
  selector: 'app-list',
  imports: [
    ...CommonModules,
    ...BaseMatModules,
    ...CommonFormModules,
    MatTableModule,
    MatPaginatorModule,
    MatChipsModule,
    FormsModule
  ],
  templateUrl: './list.html',
  styleUrls: ['./list.scss']
})
export class ScopeListComponent implements OnInit {
  readonly i18n = I18N_KEYS;
  displayedColumns: string[] = ['name', 'displayName', 'required', 'emphasize', 'description', 'actions'];

  dataSource = signal<ScopeItemDto[]>([]);
  total = signal(0);

  pageSize = 10;
  pageIndex = 0;
  isLoading = signal(false);
  searchText = '';
  requiredFilter: boolean | null = null;

  constructor(
    private api: ApiClient,
    private snackBar: MatSnackBar,
    private translate: TranslateService,
    private dialog: MatDialog,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.isLoading.set(true);

    this.api.scopes.getScopes(
      this.searchText || null,
      this.searchText || null,
      this.requiredFilter,
      this.pageIndex + 1,
      this.pageSize,
      null
    ).subscribe({
      next: (res: PageList<ScopeItemDto>) => {
        this.dataSource.set(res.data);
        this.total.set(res.count);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Failed to load scopes:', error);
        this.snackBar.open(
          this.translate.instant(this.i18n.error.loadScopesFailed),
          this.translate.instant(this.i18n.common.close),
          { duration: 3000 }
        );
        this.isLoading.set(false);
      }
    });
  }

  onSearch(): void {
    this.pageIndex = 0;
    this.loadData();
  }

  onFilterChange(): void {
    this.pageIndex = 0;
    this.loadData();
  }

  onPageChange(event: PageEvent): void {
    this.pageSize = event.pageSize;
    this.pageIndex = event.pageIndex;
    this.loadData();
  }

  clearFilters(): void {
    this.searchText = '';
    this.requiredFilter = null;
    this.pageIndex = 0;
    this.loadData();
  }

  viewDetail(id: string): void {
    this.router.navigate(['/scope/detail', id]);
  }

  openAddDialog(): void {
    const dialogRef = this.dialog.open(ScopeAddComponent, {
      width: '600px',
      maxHeight: '90vh'
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadData();
      }
    });
  }

  deleteScope(id: string): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: this.translate.instant(this.i18n.scope.deleteConfirmTitle),
        message: this.translate.instant(this.i18n.scope.deleteConfirmMessage)
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.api.scopes.deleteScope(id).subscribe({
          next: () => {
            this.snackBar.open(
              this.translate.instant(this.i18n.scope.deleteSuccess),
              this.translate.instant(this.i18n.common.close),
              { duration: 3000 }
            );
            this.loadData();
          },
          error: (error) => {
            console.error('Failed to delete scope:', error);
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
}
