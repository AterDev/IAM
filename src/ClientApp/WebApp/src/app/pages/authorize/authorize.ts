import { Component, inject, OnInit, ChangeDetectionStrategy, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { Router, ActivatedRoute } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { CommonModule } from '@angular/common';
import { ApiClient } from 'src/app/services/api/api-client';
import { OidcAuthService } from 'src/app/services/oidc-auth.service';

interface ScopeInfo {
  name: string;
  description: string;
  required: boolean;
}

@Component({
  selector: 'app-authorize',
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatCheckboxModule,
    MatListModule,
    MatIconModule,
    TranslateModule
  ],
  templateUrl: './authorize.html',
  styleUrls: ['./authorize.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Authorize implements OnInit {
  private apiClient = inject(ApiClient);
  private authService = inject(OidcAuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private translate = inject(TranslateService);

  clientName = signal('');
  clientDescription = signal('');
  scopes = signal<ScopeInfo[]>([]);
  isLoading = signal(false);
  errorMessage = signal('');
  username = signal('');

  // OAuth parameters from query string
  private authParams = signal<Record<string, string>>({});

  ngOnInit(): void {
    // Check if user is authenticated
    if (!this.authService.isAuthenticated()) {
      // Redirect to login with return URL
      this.router.navigate(['/login'], {
        queryParams: {
          returnUrl: this.router.url
        }
      });
      return;
    }

    // Get OAuth parameters from query string
    this.route.queryParams.subscribe(params => {
      this.authParams.set(params as Record<string, string>);
      this.loadAuthorizationRequest();
    });

    // Set username
    const user = this.authService.user();
    this.username.set(user?.preferred_username || user?.name || 'User');
  }

  private loadAuthorizationRequest(): void {
    const params = this.authParams();
    const clientId = params['client_id'];
    const requestedScopes = (params['scope'] || '').split(' ');

    // Set client info (in real app, fetch from API)
    this.clientName.set(clientId || 'Unknown Client');
    this.clientDescription.set(this.translate.instant('authorize.clientDescription'));

    // Parse scopes
    const scopeInfos: ScopeInfo[] = requestedScopes
      .filter(s => s)
      .map(scope => ({
        name: scope,
        description: this.getScopeDescription(scope),
        required: scope === 'openid'
      }));

    this.scopes.set(scopeInfos);
  }

  private getScopeDescription(scope: string): string {
    const descriptions: Record<string, string> = {
      'openid': this.translate.instant('scopes.openid'),
      'profile': this.translate.instant('scopes.profile'),
      'email': this.translate.instant('scopes.email'),
      'phone': this.translate.instant('scopes.phone'),
      'address': this.translate.instant('scopes.address'),
      'offline_access': this.translate.instant('scopes.offline_access')
    };

    return descriptions[scope] || scope;
  }

  async allowAccess(): Promise<void> {
    this.isLoading.set(true);
    this.errorMessage.set('');

    const params = this.authParams();

    try {
      // Call authorize endpoint
      // In a real implementation, this would POST to /connect/authorize
      // with user consent
      
      // For now, simulate redirect to callback URL
      const redirectUri = params['redirect_uri'];
      const state = params['state'];
      const code = 'simulated_auth_code';

      if (redirectUri) {
        const callbackUrl = new URL(redirectUri);
        callbackUrl.searchParams.set('code', code);
        if (state) {
          callbackUrl.searchParams.set('state', state);
        }
        window.location.href = callbackUrl.toString();
      }
    } catch (error) {
      this.errorMessage.set(this.translate.instant('authorize.error'));
      this.isLoading.set(false);
    }
  }

  denyAccess(): void {
    const params = this.authParams();
    const redirectUri = params['redirect_uri'];
    const state = params['state'];

    if (redirectUri) {
      const callbackUrl = new URL(redirectUri);
      callbackUrl.searchParams.set('error', 'access_denied');
      callbackUrl.searchParams.set('error_description', 'User denied authorization');
      if (state) {
        callbackUrl.searchParams.set('state', state);
      }
      window.location.href = callbackUrl.toString();
    } else {
      this.router.navigate(['/']);
    }
  }
}
