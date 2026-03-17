import { Component, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from 'src/app/services/auth.service';

@Component({
  selector: 'app-auth-callback',
  template: '<p style="padding:24px;">Signing you in...</p>',
})
export class AuthCallbackComponent {
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
            this.router.navigateByUrl(this.authService.consumeReturnUrl('/user'));
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