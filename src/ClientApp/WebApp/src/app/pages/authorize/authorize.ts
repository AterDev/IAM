import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit, ChangeDetectionStrategy, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { Router, ActivatedRoute } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from 'src/app/services/auth.service';
import { AuthorizeInteractionContext, OauthInteractionService } from 'src/app/services/oauth-interaction.service';

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
  private destroyRef = inject(DestroyRef);
  private interactionService = inject(OauthInteractionService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  clientName = signal('');
  clientDescription = signal('');
  scopes = signal<AuthorizeInteractionContext['requestedScopes']>([]);
  isLoading = signal(false);
  errorMessage = signal('');
  username = signal('');
  rememberConsent = signal(false);
  interaction = signal<AuthorizeInteractionContext | null>(null);

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
    this.route.queryParams
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(params => {
        this.authParams.set(params as Record<string, string>);
        this.loadAuthorizationRequest();
      });

    // Set username
    const user = this.authService.user();
    this.username.set(user?.preferred_username || user?.name || 'User');
  }

  private loadAuthorizationRequest(): void {
    const params = this.authParams();
    this.isLoading.set(true);
    this.errorMessage.set('');

    this.interactionService.getAuthorizeInteraction(params)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: interaction => {
          this.isLoading.set(false);
          this.interaction.set(interaction);
          this.clientName.set(interaction.clientName || interaction.clientId);
          this.clientDescription.set(interaction.clientDescription || '');
          this.scopes.set(interaction.requestedScopes);
          this.username.set(interaction.userName || this.username());
        },
        error: (error: HttpErrorResponse) => {
          this.isLoading.set(false);
          this.interaction.set(null);

          if (error.status === 401) {
            this.router.navigate(['/login'], {
              queryParams: {
                returnUrl: this.router.url
              }
            });
            return;
          }

          this.errorMessage.set('authorize.error');
        }
      });
  }

  async allowAccess(): Promise<void> {
    this.submitDecision(true);
  }

  denyAccess(): void {
    this.submitDecision(false);
  }

  toggleRememberConsent(checked: boolean): void {
    this.rememberConsent.set(checked);
  }

  private submitDecision(approve: boolean): void {
    const interaction = this.interaction();
    if (!interaction) {
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    this.interactionService.submitAuthorizeDecision({
      clientId: interaction.clientId,
      redirectUri: interaction.redirectUri,
      responseType: interaction.responseType,
      scope: interaction.scope,
      state: interaction.state,
      nonce: interaction.nonce,
      codeChallenge: interaction.codeChallenge,
      codeChallengeMethod: interaction.codeChallengeMethod,
      responseMode: interaction.responseMode,
      approve,
      rememberConsent: this.rememberConsent()
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: result => {
          window.location.assign(result.redirectUrl);
        },
        error: () => {
          this.isLoading.set(false);
          this.errorMessage.set('authorize.error');
        }
      });
  }
}
