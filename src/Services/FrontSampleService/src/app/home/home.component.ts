import { CommonModule } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { Component, computed, effect, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { map, startWith } from 'rxjs';
import { SamplePermissionsService } from '../shared/sample-permissions.service';
import { I18N_KEYS } from '../shared/i18n-keys';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatListModule, MatChipsModule, MatProgressSpinnerModule, MatIconModule, MatDividerModule, TranslateModule],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
})
export class HomeComponent {
  private readonly oidcSecurityService = inject(OidcSecurityService);
  private readonly permissionsService = inject(SamplePermissionsService);
  private readonly translate = inject(TranslateService);
  private readonly currentLang = toSignal(
    this.translate.onLangChange.pipe(
      map((event) => event.lang),
      startWith(this.translate.currentLang || 'zh'),
    ),
    { initialValue: this.translate.currentLang || 'zh' },
  );

  readonly i18n = I18N_KEYS;

  readonly authState = toSignal(this.oidcSecurityService.isAuthenticated$);

  readonly rawUserData = toSignal(this.oidcSecurityService.userData$);

  readonly isAuthenticated = computed(() => this.authState()?.isAuthenticated ?? false);
  readonly userData = computed<Record<string, unknown> | null>(() => {
    const value = this.rawUserData();

    if (!value || typeof value !== 'object') {
      return null;
    }

    return 'userData' in value
      ? (value.userData as Record<string, unknown> | null | undefined) ?? null
      : (value as Record<string, unknown>);
  });

  readonly displayName = computed(() => {
    this.currentLang();
    return this.readFirstString(['name', 'preferred_username', 'email', 'sub'])
      ?? this.translate.instant(this.i18n.common.notAvailable);
  });

  readonly email = computed(() => {
    this.currentLang();
    return this.readFirstString(['email']) ?? this.translate.instant(this.i18n.common.notAvailable);
  });

  readonly userId = computed(() => {
    this.currentLang();
    return this.readFirstString(['sub']) ?? this.translate.instant(this.i18n.common.notAvailable);
  });
  readonly permissions = this.permissionsService.permissions;
  readonly permissionsLoading = this.permissionsService.loading;

  constructor() {
    effect(() => {
      if (this.isAuthenticated()) {
        this.permissionsService.loadForCurrentUser();
      }
    });
  }

  private readFirstString(fields: string[]): string | null {
    const currentUser = this.userData();
    if (!currentUser) {
      return null;
    }

    for (const field of fields) {
      const value = currentUser[field];
      if (typeof value === 'string' && value.trim().length > 0) {
        return value;
      }
    }

    return null;
  }
}
