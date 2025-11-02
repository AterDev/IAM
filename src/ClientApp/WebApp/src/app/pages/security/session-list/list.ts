import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModules, BaseMatModules, CommonFormModules } from 'src/app/share/shared-modules';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatMenuModule } from '@angular/material/menu';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { ApiClient } from 'src/app/services/api/api-client';
import { LoginSessionItemDto } from 'src/app/services/api/models/identity-mod/login-session-item-dto.model';
import { PageList } from 'src/app/services/api/models/ater/page-list.model';
import { Router } from '@angular/router';
import { FormsModule, FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmDialogComponent } from 'src/app/share/components/confirm-dialog/confirm-dialog.component';

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
    MatDatepickerModule,
    MatNativeDateModule,
    FormsModule,
    ReactiveFormsModule
  ],
  templateUrl: './list.html',
  styleUrls: ['./list.scss']
})
export class SessionListComponent implements OnInit {
  displayedColumns: string[] = ['select', 'sessionId', 'ipAddress', 'userAgent', 'loginTime', 'lastActivityTime', 'isActive', 'actions'];

  dataSource = signal<LoginSessionItemDto[]>([]);
  total = signal(0);
  selectedIds = signal<Set<string>>(new Set());

  pageSize = 10;
  pageIndex = 0;
  isLoading = signal(false);

  // Filter controls
  searchText = '';
  ipAddressFilter = '';
  isActiveFilter: boolean | null = true;
  startDateControl = new FormControl<Date | null>(null);
  endDateControl = new FormControl<Date | null>(null);

  // Auto-refresh
  autoRefreshEnabled = false;
  autoRefreshInterval: any = null;

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

  ngOnDestroy(): void {
    this.stopAutoRefresh();
  }

  loadData(): void {
  this.isLoading.set(true);

    this.api.security.getSessions(
      null,
      this.searchText || null,
      this.ipAddressFilter || null,
      this.isActiveFilter,
      this.startDateControl.value,
      this.endDateControl.value,
      this.pageIndex + 1,
      this.pageSize,
      null
    ).subscribe({
      next: (res: PageList<LoginSessionItemDto>) => {
        this.dataSource.set(res.data);
        this.total.set(res.count);
  this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Failed to load sessions:', error);
        this.snackBar.open(
          this.translate.instant('error.loadSessionsFailed'),
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

  clearFilters(): void {
    this.searchText = '';
    this.ipAddressFilter = '';
    this.isActiveFilter = true;
    this.startDateControl.setValue(null);
    this.endDateControl.setValue(null);
    this.pageIndex = 0;
    this.loadData();
  }

  onPageChange(event: PageEvent): void {
    this.pageSize = event.pageSize;
    this.pageIndex = event.pageIndex;
    this.loadData();
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

  toggleSelectAll(): void {
    const data = this.dataSource();
    const selected = new Set(this.selectedIds());

    if (this.allSelected()) {
      data.forEach(item => selected.delete(item.id));
    } else {
      data.forEach(item => selected.add(item.id));
    }

    this.selectedIds.set(selected);
  }

  revokeSession(id: string): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: this.translate.instant('session.revokeConfirmTitle'),
        message: this.translate.instant('session.revokeConfirmMessage')
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.api.security.revokeSession(id).subscribe({
          next: () => {
            this.snackBar.open(
              this.translate.instant('session.revokeSuccess'),
              this.translate.instant('common.close'),
              { duration: 3000 }
            );
            this.loadData();
          },
          error: (error) => {
            console.error('Failed to revoke session:', error);
            this.snackBar.open(
              this.translate.instant('error.revokeSessionFailed'),
              this.translate.instant('common.close'),
              { duration: 3000 }
            );
          }
        });
      }
    });
  }

  revokeSelected(): void {
    const selected = this.selectedIds();
    if (selected.size === 0) {
      return;
    }

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: this.translate.instant('session.revokeBatchConfirmTitle'),
        message: this.translate.instant('session.revokeBatchConfirmMessage', { count: selected.size })
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        let completed = 0;
        let failed = 0;

        selected.forEach(id => {
          this.api.security.revokeSession(id).subscribe({
            next: () => {
              completed++;
              if (completed + failed === selected.size) {
                this.showBatchResult(completed, failed);
              }
            },
            error: () => {
              failed++;
              if (completed + failed === selected.size) {
                this.showBatchResult(completed, failed);
              }
            }
          });
        });
      }
    });
  }

  private showBatchResult(completed: number, failed: number): void {
    this.selectedIds.set(new Set());
    this.loadData();

    const message = failed === 0
      ? this.translate.instant('session.revokeBatchSuccess', { count: completed })
      : this.translate.instant('session.revokeBatchPartial', { success: completed, failed });

    this.snackBar.open(message, this.translate.instant('common.close'), { duration: 3000 });
  }

  viewDetail(session: LoginSessionItemDto): void {
    this.router.navigate(['/security/sessions', session.id]);
  }

  toggleAutoRefresh(): void {
    this.autoRefreshEnabled = !this.autoRefreshEnabled;

    if (this.autoRefreshEnabled) {
      this.startAutoRefresh();
    } else {
      this.stopAutoRefresh();
    }
  }

  private startAutoRefresh(): void {
    this.autoRefreshInterval = setInterval(() => {
      this.loadData();
    }, 10000); // Refresh every 10 seconds
  }

  private stopAutoRefresh(): void {
    if (this.autoRefreshInterval) {
      clearInterval(this.autoRefreshInterval);
      this.autoRefreshInterval = null;
    }
  }

  formatUserAgent(userAgent: string | null | undefined): string {
    if (!userAgent) {
      return '-';
    }

    // Extract browser name and version
    if (userAgent.includes('Chrome')) {
      return 'Chrome';
    } else if (userAgent.includes('Firefox')) {
      return 'Firefox';
    } else if (userAgent.includes('Safari') && !userAgent.includes('Chrome')) {
      return 'Safari';
    } else if (userAgent.includes('Edge')) {
      return 'Edge';
    }

    return userAgent.substring(0, 20) + '...';
  }
}
