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
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { environment } from '../../environments/environment';
import { SnackbarService } from '../shared/snackbar.service';
import { I18N_KEYS } from '../shared/i18n-keys';

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
    TranslateModule,
  ],
  templateUrl: './authorizations.component.html',
  styleUrl: './authorizations.component.scss',
})
export class AuthorizationsComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly snackbar = inject(SnackbarService);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);
  private readonly translate = inject(TranslateService);

  authorizations: AuthorizationViewModel[] = [];
  loading = false;
  private readonly apiUrl = `${environment.iamApiUrl}/api/authorization`;
  private rawAuthorizations: Authorization[] = [];
  readonly i18n = I18N_KEYS;

  ngOnInit() {
    this.translate.onLangChange
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.authorizations = this.rawAuthorizations.map((item) => this.toViewModel(item));
      });

    this.loadAuthorizations();
  }

  loadAuthorizations() {
    this.loading = true;
    this.http.get<Authorization[]>(this.apiUrl)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.rawAuthorizations = data;
          this.authorizations = data.map((item) => this.toViewModel(item));
          this.loading = false;
        },
        error: (err) => {
          this.loading = false;
          this.snackbar.showError(`${this.translate.instant(this.i18n.authorizations.loadFailed)}: ${err.error?.message || err.message}`);
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
              this.snackbar.showSuccess(this.translate.instant(this.i18n.authorizations.revokeSuccess));
              this.loadAuthorizations();
            },
            error: (err) => {
              this.loading = false;
              this.snackbar.showError(`${this.translate.instant(this.i18n.authorizations.revokeFailed)}: ${err.error?.message || err.message}`);
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
        return this.translate.instant(this.i18n.authorizations.types.permanent);
      case 'ad_hoc':
        return this.translate.instant(this.i18n.authorizations.types.adHoc);
      default:
        return type;
    }
  }

  private getStatusLabel(status: string): string {
    switch (status.toLowerCase()) {
      case 'valid':
        return this.translate.instant(this.i18n.authorizations.statuses.valid);
      case 'revoked':
        return this.translate.instant(this.i18n.authorizations.statuses.revoked);
      default:
        return status;
    }
  }

  private formatDate(date: string | null): string {
    if (!date) {
      return this.translate.instant(this.i18n.authorizations.neverExpires);
    }

    const locale = this.translate.currentLang === 'en' ? 'en-US' : 'zh-CN';
    return new Date(date).toLocaleString(locale);
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
  imports: [CommonModule, MatDialogModule, MatButtonModule, TranslateModule],
  template: `
    <h2 mat-dialog-title>{{ i18n.authorizations.confirmTitle | translate }}</h2>
    <mat-dialog-content>
      <p>{{ i18n.authorizations.confirmMessage | translate: { clientName: data.clientName } }}</p>
      <p>{{ i18n.authorizations.confirmHint | translate }}</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>{{ i18n.authorizations.cancel | translate }}</button>
      <button mat-raised-button color="warn" [mat-dialog-close]="true">{{ i18n.authorizations.confirmRevoke | translate }}</button>
    </mat-dialog-actions>
  `,
})
export class ConfirmDialogComponent {
  readonly i18n = I18N_KEYS;

  constructor(@Inject(MAT_DIALOG_DATA) public data: { clientName: string }) {}
}
