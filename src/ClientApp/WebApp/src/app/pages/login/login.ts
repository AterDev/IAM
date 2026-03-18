import { Component, inject, OnInit, AfterViewInit, ChangeDetectionStrategy } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { CommonModule } from '@angular/common';
import { AuthService } from 'src/app/services/auth.service';
import { I18N_KEYS } from 'src/app/share/i18n-keys';
import { initStarfield } from './starfield';

@Component({
  selector: 'app-login',
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    TranslateModule
  ],
  templateUrl: './login.html',
  styleUrls: ['./login.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Login implements OnInit, AfterViewInit {
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private translate = inject(TranslateService);

  i18nKeys = I18N_KEYS;
  isLoading = false;
  errorMessage = '';
  private returnUrl = '/user';

  ngAfterViewInit(): void {
    const canvas = document.getElementById('starfield') as HTMLCanvasElement | null;
    if (canvas) {
      initStarfield(canvas);
    }
  }

  ngOnInit(): void {
    this.returnUrl = this.route.snapshot.queryParamMap.get('returnUrl')
      ?? this.authService.peekReturnUrl()
      ?? '/user';

    this.authService.updateUserLoginState();
    if (this.authService.isAuthenticated()) {
      this.router.navigateByUrl(this.authService.consumeReturnUrl(this.returnUrl));
    }
  }

  startLogin(): void {
    this.isLoading = true;
    this.errorMessage = '';

    try {
      this.authService.startLogin(this.returnUrl);
    } catch (error) {
      this.errorMessage = this.translate.instant('login.unifiedError');
      console.error('Login error:', error);
    }
  }

  goToRegister(): void {
    this.router.navigate(['/register']);
  }

  goToForgotPassword(): void {
    this.router.navigate(['/forgot-password']);
  }

  startExternalLogin(provider: 'google' | 'microsoft'): void {
    this.isLoading = true;
    this.errorMessage = '';

    try {
      const callbackUrl = new URL(`${window.location.origin}/external-auth/callback`);
      callbackUrl.searchParams.set('returnUrl', this.returnUrl);

      const signInUrl = new URL(`/api/ExternalAuth/signin-${provider}`, window.location.origin);
      signInUrl.searchParams.set('returnUrl', callbackUrl.toString());

      window.location.assign(signInUrl.toString());
    } catch (error) {
      this.isLoading = false;
      this.errorMessage = this.translate.instant('externalAuth.failed');
      console.error('External login error:', error);
    }
  }
}
