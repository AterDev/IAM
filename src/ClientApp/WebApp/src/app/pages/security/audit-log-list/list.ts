import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModules, BaseMatModules, CommonFormModules } from 'src/app/share/shared-modules';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatMenuModule } from '@angular/material/menu';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { ApiClient } from 'src/app/services/api/api-client';
import { AuditLogItemDto } from 'src/app/services/api/models/common-mod/audit-log-item-dto.model';
import { AuditLogFilterDto } from 'src/app/services/api/models/common-mod/audit-log-filter-dto.model';
import { PageList } from 'src/app/services/api/models/ater/page-list.model';
import { Router } from '@angular/router';
import { FormControl, FormGroup, FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';
import { AuditLogDetailDialogComponent } from '../audit-log-detail-dialog/detail-dialog';

@Component({
  selector: 'app-list',
  imports: [
    ...CommonModules,
    ...BaseMatModules,
    ...CommonFormModules,
    MatTableModule,
    MatPaginatorModule,
    MatChipsModule,
    MatDialogModule,
    MatMenuModule,
    MatDatepickerModule,
    MatNativeDateModule,
    ReactiveFormsModule
  ],
  templateUrl: './list.html',
  styleUrls: ['./list.scss']
})
export class AuditLogListComponent implements OnInit {
  displayedColumns: string[] = ['category', 'event', 'subjectId', 'ipAddress', 'createdTime', 'actions'];

  dataSource = signal<AuditLogItemDto[]>([]);
  total = signal(0);

  pageSize = 10;
  pageIndex = 0;
  isLoading = signal(false);

  // Filter form
  filterForm: FormGroup;

  // Auto-refresh
  autoRefreshEnabled = false;
  autoRefreshInterval: any = null;

  constructor(
    private api: ApiClient,
    private router: Router,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private translate: TranslateService,
    private fb: FormBuilder
  ) {
    // Initialize filter form with default values
    const endDate = new Date();
    const startDate = new Date();
    startDate.setDate(startDate.getDate() - 7);

    this.filterForm = this.fb.group({
      category: [''],
      event: [''],
      subjectId: [''],
      startDate: [startDate],
      endDate: [endDate]
    });
  }

  ngOnInit(): void {
    this.loadData();
  }

  ngOnDestroy(): void {
    this.stopAutoRefresh();
  }

  loadData(): void {
    this.isLoading.set(true);

    const formValue = this.filterForm.value;

    // Build filter DTO
    const filter: AuditLogFilterDto = {
      category: formValue.category?.trim() || null,
      event: formValue.event?.trim() || null,
      subjectId: formValue.subjectId?.trim() || null,
      startDate: formValue.startDate instanceof Date ? formValue.startDate : null,
      endDate: formValue.endDate instanceof Date ? formValue.endDate : null,
      pageIndex: this.pageIndex + 1,
      pageSize: this.pageSize,
      orderBy: null
    };

    this.api.security.getAuditLogs(filter).subscribe({
      next: (res: PageList<AuditLogItemDto>) => {
        this.dataSource.set(res.data);
        this.total.set(res.count);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Failed to load audit logs:', error);
        this.snackBar.open(
          this.translate.instant('error.loadAuditLogsFailed'),
          this.translate.instant('common.close'),
          { duration: 3000 }
        );
        this.isLoading.set(false);
      }
    });
  }

  onFilterChange(): void {
    this.pageIndex = 0;
    this.loadData();
  }

  clearFilters(): void {
    // Reset to last 7 days
    const endDate = new Date();
    const startDate = new Date();
    startDate.setDate(startDate.getDate() - 7);

    this.filterForm.patchValue({
      category: '',
      event: '',
      subjectId: '',
      startDate: startDate,
      endDate: endDate
    });

    this.pageIndex = 0;
    this.loadData();
  }

  onPageChange(event: PageEvent): void {
    this.pageSize = event.pageSize;
    this.pageIndex = event.pageIndex;
    this.loadData();
  }

  viewDetail(log: AuditLogItemDto): void {
    this.api.security.getAuditLogDetail(log.id).subscribe({
      next: (detail) => {
        this.dialog.open(AuditLogDetailDialogComponent, {
          width: '600px',
          data: detail
        });
      },
      error: (error) => {
        console.error('Failed to load audit log detail:', error);
        this.snackBar.open(
          this.translate.instant('error.loadAuditLogDetailFailed'),
          this.translate.instant('common.close'),
          { duration: 3000 }
        );
      }
    });
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
    }, 15000); // Refresh every 15 seconds
  }

  private stopAutoRefresh(): void {
    if (this.autoRefreshInterval) {
      clearInterval(this.autoRefreshInterval);
      this.autoRefreshInterval = null;
    }
  }

  getCategoryColor(category: string): string {
    const categoryMap: Record<string, string> = {
      'Authentication': 'primary',
      'Authorization': 'accent',
      'Security': 'warn',
      'System': 'basic'
    };

    return categoryMap[category] || 'basic';
  }

  exportLogs(): void {
    // TODO: Implement export functionality
    this.snackBar.open(
      this.translate.instant('auditLog.exportNotImplemented'),
      this.translate.instant('common.close'),
      { duration: 3000 }
    );
  }
}
