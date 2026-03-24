import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from 'src/app/services/auth.service';
import { I18N_KEYS } from 'src/app/share/i18n-keys';

@Component({
  selector: 'app-external-auth-callback',
  imports: [
    CommonModule,
    MatButtonModule,
    MatCardModule,
    MatProgressSpinnerModule,
    TranslateModule
  ],
  templateUrl: './external-auth-callback.html',
  styleUrl: './external-auth-callback.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ExternalAuthCallbackComponent {
  readonly i18n = I18N_KEYS;
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly isProcessing = signal(true);
  readonly provider = signal('');
  readonly messageKey = signal('externalAuth.redirecting');

  constructor() {
    this.route.queryParamMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(params => {
        const status = params.get('status') ?? 'failed';
        const provider = (params.get('provider') ?? '').toLowerCase();
        const returnUrl = params.get('returnUrl') ?? '/user';

        this.provider.set(provider);

        if (status === 'success') {
          this.messageKey.set('externalAuth.redirecting');
          void this.authService.startLogin(returnUrl).catch(() => {
            this.isProcessing.set(false);
            this.messageKey.set('externalAuth.failed');
          });
          return;
        }

        this.isProcessing.set(false);
        this.messageKey.set(this.mapStatusToMessage(status));
      });
  }

  backToLogin(): void {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/user';
    this.router.navigate(['/login'], {
      queryParams: { returnUrl }
    });
  }

  private mapStatusToMessage(status: string): string {
    switch (status) {
      case 'provider_not_configured':
        return 'externalAuth.providerNotConfigured';
      case 'email_conflict':
        return 'externalAuth.emailConflict';
      case 'locked':
        return 'externalAuth.locked';
      case 'invalid_external_identity':
        return 'externalAuth.invalidIdentity';
      default:
        return 'externalAuth.failed';
    }
  }
}