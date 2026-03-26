import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { environment } from '../../environments/environment';
import { SnackbarService } from '../shared/snackbar.service';
import { I18N_KEYS } from '../shared/i18n-keys';

@Component({
  selector: 'app-protected',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatListModule,
    MatProgressSpinnerModule,
    MatExpansionModule,
    TranslateModule,
  ],
  templateUrl: './protected.component.html',
})
export class ProtectedComponent implements OnInit {
  private readonly oidcSecurityService = inject(OidcSecurityService);
  private readonly http = inject(HttpClient);
  private readonly snackbar = inject(SnackbarService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly translate = inject(TranslateService);

  userData: Record<string, unknown> | null = null;
  apiResponse: unknown;
  loading = false;
  readonly i18n = I18N_KEYS;

  ngOnInit() {
    this.oidcSecurityService.userData$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((userData) => {
        this.userData = (userData?.userData ?? userData) as Record<string, unknown> | null;
      });

    this.oidcSecurityService.getAccessToken()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((token) => {
        if (token) {
          this.http.get(`${environment.iamApiUrl}/connect/userinfo`)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
              next: (userInfo) => {
                this.userData = userInfo as Record<string, unknown>;
              },
              error: (error) => {
                console.error('获取用户信息失败', error);
              },
            });
        }
      });
  }

  callPublicApi() {
    this.invokeApi(`${environment.backendApiUrl}/api/public`, this.i18n.protected.publicApiSuccess, this.i18n.protected.publicApiError);
  }

  callProtectedApi() {
    this.invokeApi(`${environment.backendApiUrl}/api/protected`, this.i18n.protected.protectedApiSuccess, this.i18n.protected.protectedApiError);
  }

  callWeatherApi() {
    this.invokeApi(`${environment.backendApiUrl}/api/weatherforecast`, this.i18n.protected.weatherSuccess, this.i18n.protected.weatherError);
  }

  private invokeApi(url: string, successKey: string, errorKey: string) {
    this.loading = true;
    this.apiResponse = null;

    this.http.get(url)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.apiResponse = response;
          this.loading = false;
          this.snackbar.showSuccess(this.translate.instant(successKey));
        },
        error: (err) => {
          this.handleError(err, errorKey);
        },
      });
  }

  private handleError(err: { status?: number; error?: { message?: string }; message?: string }, messageKey: string) {
    this.loading = false;
    const message = this.translate.instant(messageKey);

    if (err.status === 401) {
      this.snackbar.showError(`${message}: ${this.translate.instant(this.i18n.protected.errors.unauthorized401)}`);
      return;
    }

    if (err.status === 403) {
      this.snackbar.showError(`${message}: ${this.translate.instant(this.i18n.protected.errors.forbidden403)}`);
      return;
    }

    if (err.status === 0) {
      this.snackbar.showError(`${message}: ${this.translate.instant(this.i18n.protected.errors.apiUnavailable)}`);
      return;
    }

    this.snackbar.showError(`${message}: ${err.error?.message || err.message || this.translate.instant(this.i18n.common.unknownError)}`);
  }
}
