import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Component, DestroyRef, Inject, OnInit, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { environment } from '../../environments/environment';
import { SnackbarService } from '../shared/snackbar.service';

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

interface AuthorizationViewModel {
  id: string;
  clientId: string;
  clientName: string;
  scopes: string[];
  typeLabel: string;
  statusLabel: string;
  statusColor: 'primary' | 'warn' | 'accent';
  creationDateText: string;
  expirationDateText: string | null;
  isRevoked: boolean;
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
    MatDialogModule,
  ],
  templateUrl: './authorizations.component.html',
  styleUrl: './authorizations.component.scss',
})
export class AuthorizationsComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly snackbar = inject(SnackbarService);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  authorizations: AuthorizationViewModel[] = [];
  loading = false;
  private readonly apiUrl = `${environment.iamApiUrl}/api/authorization`;

  ngOnInit() {
    this.loadAuthorizations();
  }

  loadAuthorizations() {
    this.loading = true;
    this.http.get<Authorization[]>(this.apiUrl)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.authorizations = data.map((item) => this.toViewModel(item));
          this.loading = false;
        },
        error: (err) => {
          this.loading = false;
          this.snackbar.showError(`加载授权记录失败: ${err.error?.message || err.message}`);
        },
      });
  }

  revokeAuthorization(id: string, clientName: string) {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: { clientName },
    });

    dialogRef.afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (!result) {
          return;
        }

        this.loading = true;
        this.http.delete(`${this.apiUrl}/${id}`)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: () => {
              this.loading = false;
              this.snackbar.showSuccess('授权已撤销');
              this.loadAuthorizations();
            },
            error: (err) => {
              this.loading = false;
              this.snackbar.showError(`撤销授权失败: ${err.error?.message || err.message}`);
            },
          });
      });
  }

  private getStatusColor(status: string): 'primary' | 'warn' | 'accent' {
    switch (status.toLowerCase()) {
      case 'valid':
        return 'primary';
      case 'revoked':
        return 'warn';
      default:
        return 'accent';
    }
  }

  private getTypeLabel(type: string): string {
    switch (type.toLowerCase()) {
      case 'permanent':
        return '永久';
      case 'ad_hoc':
        return '临时';
      default:
        return type;
    }
  }

  private getStatusLabel(status: string): string {
    switch (status.toLowerCase()) {
      case 'valid':
        return '有效';
      case 'revoked':
        return '已撤销';
      default:
        return status;
    }
  }

  private formatDate(date: string | null): string {
    if (!date) {
      return '永不过期';
    }

    return new Date(date).toLocaleString('zh-CN');
  }

  private toViewModel(item: Authorization): AuthorizationViewModel {
    return {
      id: item.id,
      clientId: item.clientId,
      clientName: item.clientName,
      scopes: item.scopes?.split(' ').filter(Boolean) || [],
      typeLabel: this.getTypeLabel(item.type),
      statusLabel: this.getStatusLabel(item.status),
      statusColor: this.getStatusColor(item.status),
      creationDateText: this.formatDate(item.creationDate),
      expirationDateText: item.expirationDate ? this.formatDate(item.expirationDate) : null,
      isRevoked: item.status.toLowerCase() === 'revoked',
    };
  }
}

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>确认撤销授权</h2>
    <mat-dialog-content>
      <p>确定要撤销对 <strong>{{ data.clientName }}</strong> 的授权吗？</p>
      <p>撤销后，该应用将无法继续访问您的数据，需要重新授权后才能再次使用。</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>取消</button>
      <button mat-raised-button color="warn" [mat-dialog-close]="true">确认撤销</button>
    </mat-dialog-actions>
  `,
})
export class ConfirmDialogComponent {
  constructor(@Inject(MAT_DIALOG_DATA) public data: { clientName: string }) {}
}
