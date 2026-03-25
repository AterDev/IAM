import { CommonModule } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { Component, computed, effect, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { SamplePermissionsService } from '../shared/sample-permissions.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatListModule, MatChipsModule, MatProgressSpinnerModule, MatIconModule, MatDividerModule],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
})
export class HomeComponent {
  private readonly oidcSecurityService = inject(OidcSecurityService);
  private readonly permissionsService = inject(SamplePermissionsService);

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

  readonly displayName = computed(() => this.readFirstString(['name', 'preferred_username', 'email', 'sub']) ?? '未获取到');
  readonly email = computed(() => this.readFirstString(['email']) ?? '未提供');
  readonly userId = computed(() => this.readFirstString(['sub']) ?? '未提供');
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
