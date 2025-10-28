import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { CommonModules, BaseMatModules } from 'src/app/share/shared-modules';
import { AuditLogDetailDto } from 'src/app/services/api/models/common-mod/audit-log-detail-dto.model';

@Component({
  selector: 'app-audit-log-detail-dialog',
  imports: [
    ...CommonModules,
    ...BaseMatModules,
    MatDialogModule
  ],
  template: `
    <h2 mat-dialog-title>{{ 'auditLog.detailTitle' | translate }}</h2>
    <mat-dialog-content>
      <div class="detail-section">
        <div class="detail-item">
          <label>{{ 'auditLog.id' | translate }}:</label>
          <span class="value">{{ data.id }}</span>
        </div>
        <div class="detail-item">
          <label>{{ 'auditLog.category' | translate }}:</label>
          <span class="value">{{ data.category }}</span>
        </div>
        <div class="detail-item">
          <label>{{ 'auditLog.event' | translate }}:</label>
          <span class="value">{{ data.event }}</span>
        </div>
        <div class="detail-item">
          <label>{{ 'auditLog.subjectId' | translate }}:</label>
          <span class="value">{{ data.subjectId || '-' }}</span>
        </div>
        <div class="detail-item">
          <label>{{ 'auditLog.ipAddress' | translate }}:</label>
          <span class="value">{{ data.ipAddress || '-' }}</span>
        </div>
        <div class="detail-item">
          <label>{{ 'auditLog.userAgent' | translate }}:</label>
          <span class="value">{{ data.userAgent || '-' }}</span>
        </div>
        <div class="detail-item">
          <label>{{ 'auditLog.createdTime' | translate }}:</label>
          <span class="value">{{ data.createdTime | date:'medium' }}</span>
        </div>
        <div class="detail-item full-width" *ngIf="data.payload">
          <label>{{ 'auditLog.payload' | translate }}:</label>
          <pre class="payload">{{ formatPayload(data.payload) }}</pre>
        </div>
      </div>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="close()">{{ 'common.close' | translate }}</button>
    </mat-dialog-actions>
  `,
  styles: [`
    .detail-section {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 16px;
      padding: 16px 0;

      .detail-item {
        display: flex;
        flex-direction: column;
        gap: 4px;

        &.full-width {
          grid-column: 1 / -1;
        }

        label {
          font-weight: 600;
          color: rgba(0, 0, 0, 0.6);
          font-size: 12px;
          text-transform: uppercase;
        }

        .value {
          font-size: 14px;
          color: rgba(0, 0, 0, 0.87);
          word-break: break-all;
        }

        .payload {
          background-color: #f5f5f5;
          padding: 12px;
          border-radius: 4px;
          font-size: 12px;
          font-family: monospace;
          overflow-x: auto;
          white-space: pre-wrap;
          word-wrap: break-word;
          max-height: 300px;
          margin: 0;
        }
      }
    }

    mat-dialog-content {
      min-width: 500px;
    }
  `]
})
export class AuditLogDetailDialogComponent {
  constructor(
    public dialogRef: MatDialogRef<AuditLogDetailDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: AuditLogDetailDto
  ) {}

  close(): void {
    this.dialogRef.close();
  }

  formatPayload(payload: string | null | undefined): string {
    if (!payload) {
      return '';
    }

    try {
      const parsed = JSON.parse(payload);
      return JSON.stringify(parsed, null, 2);
    } catch {
      return payload;
    }
  }
}
