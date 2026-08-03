import { Component, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from 'src/app/services/auth.service';
import { I18N_KEYS } from 'src/app/share/i18n-keys';

@Component({
  selector: 'app-auth-callback',
  imports: [TranslateModule],
  template: '<p style="padding:24px;">{{ i18n.externalAuth.redirecting | translate }}</p>',
})
export class AuthCallbackComponent {
  readonly i18n = I18N_KEYS;
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  constructor() {
    queueMicrotask(() => {
      const code = this.route.snapshot.queryParamMap.get('code');
      const state = this.route.snapshot.queryParamMap.get('state');

      if (!code) {
        this.router.navigate(['/login']);
        return;
      }

      this.authService.completeLogin(code, state)
        .then(() => {
          if (this.authService.isAuthenticated()) {
            this.router.navigateByUrl(this.authService.consumeReturnUrl('/user/list'));
            return;
          }

          this.router.navigate(['/login']);
        })
        .catch(() => {
          this.router.navigate(['/login']);
        });
    });
  }
}
