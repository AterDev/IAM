import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { CommonModules, BaseMatModules } from 'src/app/share/shared-modules';
import { AuditLogDetailDto } from 'src/app/services/api/models/iammod/audit-log-detail-dto.model';
import { I18N_KEYS } from 'src/app/share/i18n-keys';

@Component({
  selector: 'app-detail-dialog',
  imports: [
    ...CommonModules,
    ...BaseMatModules,
    MatDialogModule
  ],
  templateUrl: './detail-dialog.html',
  styleUrls: ['./detail-dialog.scss']
})
export class AuditLogDetailDialogComponent {
  readonly i18n = I18N_KEYS;
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
