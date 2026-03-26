import { Component, OnInit, signal } from '@angular/core';
import { computed } from '@angular/core';
import { CommonModules, BaseMatModules } from 'src/app/share/shared-modules';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatTableModule } from '@angular/material/table';
import { MatTabsModule } from '@angular/material/tabs';
import { ApiClient } from 'src/app/services/api/api-client';
import { ClientEditComponent } from '../edit/edit';
import { ConfirmDialogComponent } from 'src/app/share/components/confirm-dialog/confirm-dialog.component';
import { TranslateService } from '@ngx-translate/core';
import { Clipboard } from '@angular/cdk/clipboard';
import { AppLoadingComponent } from 'src/app/share/components/loading/loading';
import { AuthorizationItemDto } from 'src/app/services/api/models/iammod/authorization-item-dto.model';
import { EnumTextPipe } from 'src/app/pipe/api/enum-text.pipe';
import { ClientDetailViewModel } from '../client-password-grant-policy.model';
import { ClientPermissionsComponent } from '../permissions/permissions';
import { I18N_KEYS } from 'src/app/share/i18n-keys';

@Component({
  selector: 'app-detail',
  imports: [
    ...CommonModules,
    ...BaseMatModules,
    MatCardModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    MatTableModule,
    MatTabsModule,
    AppLoadingComponent,
    EnumTextPipe,
    ClientPermissionsComponent,
  ],
  templateUrl: './detail.html',
  styleUrls: ['./detail.scss']
})
export class ClientDetailComponent implements OnInit {
  readonly i18n = I18N_KEYS;
  client = signal<ClientDetailViewModel | null>(null);
  authorizations = signal<AuthorizationItemDto[]>([]);
  isLoading = signal(false);
  isLoadingAuthorizations = signal(false);
  clientId?: string;
  readonly isPasswordGrantEnabled = computed(() => this.client()?.allowPasswordGrant ?? false);
  readonly passwordGrantRestrictionReason = computed(() => this.client()?.passwordGrantRestrictionReason?.trim() ?? '');

  authDisplayedColumns: string[] = ['subjectId', 'status', 'creationDate'];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private api: ApiClient,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private translate: TranslateService,
    private clipboard: Clipboard
  ) { }

  ngOnInit(): void {
    this.clientId = this.route.snapshot.paramMap.get('id') || '';
    if (this.clientId) {
      this.loadClient();
      this.loadAuthorizations();
    }
  }

  loadClient(): void {
    this.isLoading.set(true);
    this.api.clients.getDetail(this.clientId!).subscribe({
      next: (client) => {
        this.client.set(client as ClientDetailViewModel);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.snackBar.open(
          this.translate.instant('error.loadClientFailed'),
          this.translate.instant('common.close'),
          { duration: 3000 }
        );
        this.router.navigate(['/client/list']);
      }
    });
  }

  loadAuthorizations(): void {
    this.isLoadingAuthorizations.set(true);
    this.api.clients.getAuthorizations(this.clientId!).subscribe({
      next: (authorizations) => {
        this.authorizations.set(authorizations);
        this.isLoadingAuthorizations.set(false);
      },
      error: () => {
        this.isLoadingAuthorizations.set(false);
        this.snackBar.open(
          this.translate.instant('error.loadAuthorizationsFailed'),
          this.translate.instant('common.close'),
          { duration: 3000 }
        );
      }
    });
  }

  openEditDialog(): void {
    const dialogRef = this.dialog.open(ClientEditComponent, {
      width: '600px',
      data: { clientId: this.clientId }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadClient();
      }
    });
  }

  rotateSecret(): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: this.translate.instant('client.rotateSecretTitle'),
        message: this.translate.instant('client.rotateSecretMessage')
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.api.clients.rotateSecret(this.clientId!).subscribe({
          next: (response) => {
            const secret = response.secret;
            this.clipboard.copy(secret!);
            this.snackBar.open(
              this.translate.instant('client.secretRotatedAndCopied'),
              this.translate.instant('common.close'),
              { duration: 5000 }
            );

            alert(`${this.translate.instant('client.newSecret')}: ${secret}\n\n${this.translate.instant('client.secretWarning')}`);
          },
          error: () => {
            this.snackBar.open(
              this.translate.instant('error.rotateSecretFailed'),
              this.translate.instant('common.close'),
              { duration: 3000 }
            );
          }
        });
      }
    });
  }

  copyClientId(): void {
    const client = this.client();
    if (client) {
      this.clipboard.copy(client.clientId);
      this.snackBar.open(
        this.translate.instant('client.clientIdCopied'),
        this.translate.instant('common.close'),
        { duration: 2000 }
      );
    }
  }

  deleteClient(): void {
    const client = this.client();
    if (!client) {
      return;
    }

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: this.translate.instant('client.deleteConfirmTitle'),
        message: this.translate.instant('client.deleteConfirmDetailMessage', { name: client.displayName })
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.api.clients.deleteClient(this.clientId!).subscribe({
          next: () => {
            this.snackBar.open(
              this.translate.instant('client.deleteSuccess'),
              this.translate.instant('common.close'),
              { duration: 3000 }
            );
            this.router.navigate(['/client/list']);
          },
          error: () => {
            this.snackBar.open(
              this.translate.instant('error.deleteClientFailed'),
              this.translate.instant('common.close'),
              { duration: 3000 }
            );
          }
        });
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/client/list']);
  }
}
