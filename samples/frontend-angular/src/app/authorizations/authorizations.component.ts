import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { SnackbarService } from '../shared/snackbar.service';
import { environment } from '../../environments/environment';

interface Authorization {
  id: string;
  clientId: string;
  clientName: string;
  scopes: string;
  type: string;
  status: string;
  creationDate: string;
  expirationDate: string | null;
}

@Component({
  selector: 'app-authorizations',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatListModule,
    MatProgressSpinnerModule,
    MatIconModule,
    MatChipsModule,
    MatDialogModule
  ],
  templateUrl: './authorizations.component.html',
  styleUrl: './authorizations.component.scss'
})
export class AuthorizationsComponent implements OnInit {
  private http = inject(HttpClient);
  private snackbar = inject(SnackbarService);
  private dialog = inject(MatDialog);

  authorizations: Authorization[] = [];
  loading = false;
  private apiUrl = `${environment.iamApiUrl}/api/authorization`;

  ngOnInit() {
    this.loadAuthorizations();
  }

  loadAuthorizations() {
    this.loading = true;
    this.http.get<Authorization[]>(this.apiUrl)
      .subscribe({
        next: (data) => {
          this.authorizations = data;
          this.loading = false;
        },
        error: (err) => {
          this.loading = false;
          this.snackbar.showError('加载授权记录失败: ' + (err.error?.message || err.message));
        }
      });
  }

  revokeAuthorization(id: string, clientName: string) {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: { clientName }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loading = true;
        this.http.delete(`${this.apiUrl}/${id}`)
          .subscribe({
            next: () => {
              this.loading = false;
              this.snackbar.showSuccess('授权已撤销');
              this.loadAuthorizations();
            },
            error: (err) => {
              this.loading = false;
              this.snackbar.showError('撤销授权失败: ' + (err.error?.message || err.message));
            }
          });
      }
    });
  }

  getScopesList(scopes: string): string[] {
    return scopes?.split(' ').filter(s => s) || [];
  }

  getStatusColor(status: string): string {
    switch (status.toLowerCase()) {
      case 'valid':
        return 'primary';
      case 'revoked':
        return 'warn';
      default:
        return 'accent';
    }
  }

  getTypeLabel(type: string): string {
    switch (type.toLowerCase()) {
      case 'permanent':
        return '永久';
      case 'ad_hoc':
        return '临时';
      default:
        return type;
    }
  }

  getStatusLabel(status: string): string {
    switch (status.toLowerCase()) {
      case 'valid':
        return '有效';
      case 'revoked':
        return '已撤销';
      default:
        return status;
    }
  }

  formatDate(date: string | null): string {
    if (!date) return '永不过期';
    return new Date(date).toLocaleString('zh-CN');
  }
}

// Confirmation Dialog Component
@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>确认撤销授权</h2>
    <mat-dialog-content>
      <p>确定要撤销对 <strong>{{ data.clientName }}</strong> 的授权吗？</p>
      <p>撤销后，该应用将无法继续访问您的数据，您需要重新授权才能使用。</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>取消</button>
      <button mat-raised-button color="warn" [mat-dialog-close]="true">确认撤销</button>
    </mat-dialog-actions>
  `
})
export class ConfirmDialogComponent {
  constructor(@inject(MAT_DIALOG_DATA) public data: { clientName: string }) {}
}

import { MAT_DIALOG_DATA } from '@angular/material/dialog';
