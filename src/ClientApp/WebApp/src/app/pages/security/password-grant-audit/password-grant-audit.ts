import { Component, OnDestroy, OnInit, input, signal } from '@angular/core';
import { CommonModules, BaseMatModules, CommonFormModules } from 'src/app/share/shared-modules';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatCardModule } from '@angular/material/card';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';
import { forkJoin, map, of, switchMap } from 'rxjs';
import { Router } from '@angular/router';
import { ApiClient } from 'src/app/services/api/api-client';
import { AuditLogFilterDto } from 'src/app/services/api/models/iammod/audit-log-filter-dto.model';
import { AuditLogDetailDto } from 'src/app/services/api/models/iammod/audit-log-detail-dto.model';
import { AuditLogItemDto } from 'src/app/services/api/models/iammod/audit-log-item-dto.model';
import { AuditLogDetailDialogComponent } from '../audit-log-detail-dialog/detail-dialog';
import { PasswordGrantEventFilter, PasswordGrantAuditRow, PasswordGrantAuditSummary } from './password-grant-audit.model';
import { I18N_KEYS } from 'src/app/share/i18n-keys';

@Component({
  selector: 'app-password-grant-audit',
  imports: [
    ...CommonModules,
    ...BaseMatModules,
    ...CommonFormModules,
    MatTableModule,
    MatPaginatorModule,
    MatChipsModule,
    MatDialogModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatCardModule,
    ReactiveFormsModule,
  ],
  templateUrl: './password-grant-audit.html',
  styleUrls: ['./password-grant-audit.scss']
})
export class PasswordGrantAuditComponent implements OnInit, OnDestroy {
  readonly i18n = I18N_KEYS;
  readonly embedded = input(false);
  readonly displayedColumns: string[] = ['event', 'clientId', 'summary', 'subjectId', 'ipAddress', 'createdTime', 'actions'];
  readonly eventOptions = [
    { value: 'all', label: 'passwordGrantAudit.events.all' },
    { value: 'PasswordGrantRejected', label: 'passwordGrantAudit.events.rejected' },
    { value: 'PasswordGrantFailed', label: 'passwordGrantAudit.events.failed' },
  ] as const;

  readonly dataSource = signal<PasswordGrantAuditRow[]>([]);
  readonly summary = signal<PasswordGrantAuditSummary>({ total: 0, rejected: 0, failed: 0 });
  readonly total = signal(0);
  readonly isLoading = signal(false);

  pageSize = 10;
  pageIndex = 0;
  autoRefreshEnabled = false;
  autoRefreshInterval: ReturnType<typeof setInterval> | null = null;

  readonly filterForm: FormGroup;

  constructor(
    private readonly api: ApiClient,
    private readonly router: Router,
    private readonly dialog: MatDialog,
    private readonly snackBar: MatSnackBar,
    private readonly translate: TranslateService,
    private readonly fb: FormBuilder,
  ) {
    const endDate = new Date();
    const startDate = new Date();
    startDate.setDate(startDate.getDate() - 7);

    this.filterForm = this.fb.group({
      event: ['all'],
      subjectId: [''],
      startDate: [startDate],
      endDate: [endDate],
    });
  }

  ngOnInit(): void {
    this.loadData();
  }

  ngOnDestroy(): void {
    this.stopAutoRefresh();
  }

  get eventControl() {
    return this.filterForm.get('event') as FormControl;
  }

  get subjectIdControl() {
    return this.filterForm.get('subjectId') as FormControl;
  }

  get startDateControl() {
    return this.filterForm.get('startDate') as FormControl;
  }

  get endDateControl() {
    return this.filterForm.get('endDate') as FormControl;
  }

  loadData(): void {
    this.isLoading.set(true);
    const filterState = this.getFilterState();

    forkJoin({
      rejectedCount: this.getEventCount('PasswordGrantRejected', filterState),
      failedCount: this.getEventCount('PasswordGrantFailed', filterState),
      rows: this.getRows(filterState),
    }).subscribe({
      next: ({ rejectedCount, failedCount, rows }) => {
        const total = rejectedCount + failedCount;
        this.summary.set({
          total,
          rejected: rejectedCount,
          failed: failedCount,
        });
        this.total.set(filterState.event === 'all'
          ? total
          : (filterState.event === 'PasswordGrantRejected' ? rejectedCount : failedCount));
        this.dataSource.set(rows);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('[PasswordGrantAudit] Failed to load report:', error);
        this.snackBar.open(
          this.translate.instant(this.i18n.error.loadAuditLogsFailed),
          this.translate.instant(this.i18n.common.close),
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
    const endDate = new Date();
    const startDate = new Date();
    startDate.setDate(startDate.getDate() - 7);

    this.filterForm.patchValue({
      event: 'all',
      subjectId: '',
      startDate,
      endDate,
    });

    this.pageIndex = 0;
    this.loadData();
  }

  onPageChange(event: PageEvent): void {
    this.pageSize = event.pageSize;
    this.pageIndex = event.pageIndex;
    this.loadData();
  }

  viewDetail(row: PasswordGrantAuditRow): void {
    this.dialog.open(AuditLogDetailDialogComponent, {
      width: '640px',
      data: row.detail,
    });
  }

  openClientDetail(row: PasswordGrantAuditRow): void {
    const clientId = row.clientId?.trim();
    if (!clientId) {
      this.snackBar.open(
        this.translate.instant(this.i18n.passwordGrantAudit.clientLookupMissing),
        this.translate.instant(this.i18n.common.close),
        { duration: 2500 }
      );
      return;
    }

    this.api.clients.getClients(clientId, null, null, null, 1, 1, null).subscribe({
      next: result => {
        const target = result.data.find(client => client.clientId === clientId);
        if (!target) {
          this.snackBar.open(
            this.translate.instant(this.i18n.passwordGrantAudit.clientLookupNotFound, { clientId }),
            this.translate.instant(this.i18n.common.close),
            { duration: 3000 }
          );
          return;
        }

        this.router.navigate(['/client/detail', target.id]);
      },
      error: (error) => {
        console.error('[PasswordGrantAudit] Failed to resolve client detail route:', error);
        this.snackBar.open(
          this.translate.instant(this.i18n.error.loadClientsFailed),
          this.translate.instant(this.i18n.common.close),
          { duration: 3000 }
        );
      }
    });
  }

  toggleAutoRefresh(): void {
    this.autoRefreshEnabled = !this.autoRefreshEnabled;
    if (this.autoRefreshEnabled) {
      this.autoRefreshInterval = setInterval(() => this.loadData(), 15000);
      return;
    }

    this.stopAutoRefresh();
  }

  getEventColor(eventName: string): 'primary' | 'accent' | 'warn' | undefined {
    return eventName === 'PasswordGrantRejected' ? 'warn' : 'accent';
  }

  private stopAutoRefresh(): void {
    if (this.autoRefreshInterval) {
      clearInterval(this.autoRefreshInterval);
      this.autoRefreshInterval = null;
    }
  }

  private getFilterState() {
    const formValue = this.filterForm.value;
    return {
      event: (formValue.event || 'all') as PasswordGrantEventFilter,
      subjectId: formValue.subjectId?.trim() || null,
      startDate: formValue.startDate instanceof Date ? formValue.startDate : null,
      endDate: formValue.endDate instanceof Date ? formValue.endDate : null,
    };
  }

  private getEventCount(eventName: 'PasswordGrantRejected' | 'PasswordGrantFailed', filterState: ReturnType<PasswordGrantAuditComponent['getFilterState']>) {
    return this.api.security
      .getAuditLogs(this.createAuditLogFilter(filterState, eventName, 1, 1))
      .pipe(map(result => result.count));
  }

  private getRows(filterState: ReturnType<PasswordGrantAuditComponent['getFilterState']>) {
    const requestedRows = (this.pageIndex + 1) * this.pageSize;
    if (filterState.event === 'PasswordGrantRejected' || filterState.event === 'PasswordGrantFailed') {
      return this.getRowsForEvent(filterState.event, filterState, requestedRows).pipe(
        map(rows => rows.slice(this.pageIndex * this.pageSize, (this.pageIndex + 1) * this.pageSize))
      );
    }

    return forkJoin([
      this.getRowsForEvent('PasswordGrantRejected', filterState, requestedRows),
      this.getRowsForEvent('PasswordGrantFailed', filterState, requestedRows),
    ]).pipe(
      map(([rejectedRows, failedRows]) => [...rejectedRows, ...failedRows]
        .sort((left, right) => new Date(right.log.createdTime).getTime() - new Date(left.log.createdTime).getTime())
        .slice(this.pageIndex * this.pageSize, (this.pageIndex + 1) * this.pageSize))
    );
  }

  private getRowsForEvent(
    eventName: 'PasswordGrantRejected' | 'PasswordGrantFailed',
    filterState: ReturnType<PasswordGrantAuditComponent['getFilterState']>,
    requestedRows: number,
  ) {
    return this.api.security
      .getAuditLogs(this.createAuditLogFilter(filterState, eventName, 1, requestedRows))
      .pipe(
        switchMap(result => {
          if (!result.data.length) {
            return of([] as PasswordGrantAuditRow[]);
          }

          const detailRequests = result.data.map(log => this.api.security.getAuditLogDetail(log.id).pipe(
            map(detail => this.mapToRow(log, detail))
          ));

          return forkJoin(detailRequests);
        })
      );
  }

  private createAuditLogFilter(
    filterState: ReturnType<PasswordGrantAuditComponent['getFilterState']>,
    eventName: 'PasswordGrantRejected' | 'PasswordGrantFailed',
    pageIndex: number,
    pageSize: number,
  ): AuditLogFilterDto {
    return {
      category: 'Authentication',
      event: eventName,
      subjectId: filterState.subjectId,
      startDate: filterState.startDate,
      endDate: filterState.endDate,
      pageIndex,
      pageSize,
      orderBy: null,
    };
  }

  private mapToRow(log: AuditLogItemDto, detail: AuditLogDetailDto): PasswordGrantAuditRow {
    const payload = this.tryParsePayload(detail.payload);
    const clientId = this.getPayloadValue(payload, 'clientId');
    const restrictionReason = this.getPayloadValue(payload, 'PasswordGrantRestrictionReason')
      ?? this.getPayloadValue(payload, 'passwordGrantRestrictionReason');

    if (log.event === 'PasswordGrantRejected') {
      return {
        log,
        detail,
        clientId,
        restrictionReason,
        summary: restrictionReason || this.translate.instant(this.i18n.passwordGrantAudit.defaultRejectedSummary),
      };
    }

    const reason = this.getPayloadValue(payload, 'reason');
    const failedCount = this.getPayloadValue(payload, 'failedCount');
    const lockoutEnd = this.getPayloadValue(payload, 'lockoutEnd');
    const summaryParts = [reason];
    if (failedCount) {
      summaryParts.push(this.translate.instant(this.i18n.passwordGrantAudit.failedCountSummary, { count: failedCount }));
    }
    if (lockoutEnd) {
      summaryParts.push(this.translate.instant(this.i18n.passwordGrantAudit.lockoutSummary, { time: lockoutEnd }));
    }

    return {
      log,
      detail,
      clientId,
      summary: summaryParts.filter(Boolean).join(' · ') || this.translate.instant(this.i18n.passwordGrantAudit.defaultFailedSummary),
    };
  }

  private tryParsePayload(payload: string | null | undefined): Record<string, unknown> | null {
    if (!payload) {
      return null;
    }

    try {
      return JSON.parse(payload) as Record<string, unknown>;
    } catch {
      return null;
    }
  }

  private getPayloadValue(payload: Record<string, unknown> | null, key: string): string | null {
    const value = payload?.[key];
    if (value === null || value === undefined || value === '') {
      return null;
    }

    return String(value);
  }
}
