import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
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
    MatChipsModule
  ],
  templateUrl: './authorizations.component.html',
  styleUrl: './authorizations.component.scss'
})
export class AuthorizationsComponent implements OnInit {
  private http = inject(HttpClient);
  private snackbar = inject(SnackbarService);

  authorizations: Authorization[] = [];
  loading = false;

  ngOnInit() {
    this.loadAuthorizations();
  }

  loadAuthorizations() {
    this.loading = true;
    this.http.get<Authorization[]>('https://localhost:7070/api/authorization')
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

  revokeAuthorization(id: string) {
    if (!confirm('确定要撤销此授权吗？')) {
      return;
    }

    this.loading = true;
    this.http.delete(`https://localhost:7070/api/authorization/${id}`)
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
