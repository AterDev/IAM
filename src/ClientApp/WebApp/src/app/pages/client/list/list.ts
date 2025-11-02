import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModules, BaseMatModules, CommonFormModules } from 'src/app/share/shared-modules';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatMenuModule } from '@angular/material/menu';
import { ApiClient } from 'src/app/services/api/api-client';
import { ClientItemDto } from 'src/app/services/api/models/access-mod/client-item-dto.model';
import { PageList } from 'src/app/services/api/models/ater/page-list.model';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmDialogComponent } from 'src/app/share/components/confirm-dialog/confirm-dialog.component';
import { ClientAddComponent } from '../add/add';

@Component({
  selector: 'app-list',
  imports: [
    ...CommonModules,
    ...BaseMatModules,
    ...CommonFormModules,
    MatTableModule,
    MatPaginatorModule,
    MatCheckboxModule,
    MatChipsModule,
    MatDialogModule,
    MatMenuModule,
    FormsModule
  ],
  templateUrl: './list.html',
  styleUrls: ['./list.scss']
})
export class ClientListComponent implements OnInit {
  displayedColumns: string[] = ['select', 'clientId', 'displayName', 'type', 'applicationType', 'createdTime', 'actions'];

  dataSource = signal<ClientItemDto[]>([]);
  total = signal(0);
  selectedIds = signal<Set<string>>(new Set());

  pageSize = 10;
  pageIndex = 0;
  isLoading = signal(false);
  searchText = '';
  typeFilter: string | null = null;
  applicationTypeFilter: string | null = null;

  allSelected = computed(() => {
    const data = this.dataSource();
    const selected = this.selectedIds();
    return data.length > 0 && data.every(item => selected.has(item.id));
  });

  someSelected = computed(() => {
    const data = this.dataSource();
    const selected = this.selectedIds();
    return selected.size > 0 && !this.allSelected();
  });

  constructor(
    private api: ApiClient,
    private router: Router,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
  this.isLoading.set(true);

    this.api.clients.getClients(
      this.searchText || null,
      this.searchText || null,
      this.typeFilter,
      this.applicationTypeFilter,
      this.pageIndex + 1,
      this.pageSize,
      null
    ).subscribe({
      next: (res: PageList<ClientItemDto>) => {
        this.dataSource.set(res.data);
        this.total.set(res.count);
  this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Failed to load clients:', error);
        this.snackBar.open(
          this.translate.instant('error.loadClientsFailed'),
          this.translate.instant('common.close'),
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
    this.typeFilter = null;
    this.applicationTypeFilter = null;
    this.pageIndex = 0;
    this.loadData();
  }

  toggleSelectAll(): void {
    const data = this.dataSource();
    if (this.allSelected()) {
      this.selectedIds.set(new Set());
    } else {
      this.selectedIds.set(new Set(data.map(item => item.id)));
    }
  }

  toggleSelect(id: string): void {
    const selected = new Set(this.selectedIds());
    if (selected.has(id)) {
      selected.delete(id);
    } else {
      selected.add(id);
    }
    this.selectedIds.set(selected);
  }

  viewDetail(id: string): void {
    this.router.navigate(['/client', id]);
  }

  openAddDialog(): void {
    const dialogRef = this.dialog.open(ClientAddComponent, {
      width: '600px'
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadData();
      }
    });
  }

  deleteClient(id: string): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: this.translate.instant('client.deleteConfirmTitle'),
        message: this.translate.instant('client.deleteConfirmMessage')
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.api.clients.deleteClient(id).subscribe({
          next: () => {
            this.snackBar.open(
              this.translate.instant('client.deleteSuccess'),
              this.translate.instant('common.close'),
              { duration: 3000 }
            );
            this.loadData();
          },
          error: (error) => {
            console.error('Failed to delete client:', error);
            this.snackBar.open(
              this.translate.instant('error.deleteClientFailed'),
              this.translate.instant('common.close'),
              { duration: 3000 }
            );
          }
        });
      }
    });
  }

  deleteSelected(): void {
    const selectedCount = this.selectedIds().size;
    if (selectedCount === 0) {
      return;
    }

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: this.translate.instant('client.deleteConfirmTitle'),
        message: this.translate.instant('client.deleteMultipleConfirmMessage', { count: selectedCount })
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        const ids = Array.from(this.selectedIds());
        let deleteCount = 0;
        let errorCount = 0;

        ids.forEach(id => {
          this.api.clients.deleteClient(id).subscribe({
            next: () => {
              deleteCount++;
              if (deleteCount + errorCount === ids.length) {
                this.handleBatchDeleteComplete(deleteCount, errorCount);
              }
            },
            error: (error) => {
              console.error('Failed to delete client:', error);
              errorCount++;
              if (deleteCount + errorCount === ids.length) {
                this.handleBatchDeleteComplete(deleteCount, errorCount);
              }
            }
          });
        });
      }
    });
  }

  private handleBatchDeleteComplete(successCount: number, errorCount: number): void {
    this.selectedIds.set(new Set());
    this.loadData();

    if (errorCount === 0) {
      this.snackBar.open(
        this.translate.instant('client.deleteMultipleSuccess', { count: successCount }),
        this.translate.instant('common.close'),
        { duration: 3000 }
      );
    } else {
      this.snackBar.open(
        this.translate.instant('client.deleteMultiplePartial', { success: successCount, error: errorCount }),
        this.translate.instant('common.close'),
        { duration: 5000 }
      );
    }
  }
}
