import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { CommonModules, BaseMatModules } from 'src/app/share/shared-modules';
import { AuditLogDetailDto } from 'src/app/services/api/models/common-mod/audit-log-detail-dto.model';

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
