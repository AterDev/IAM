import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-callback',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatProgressSpinnerModule],
  templateUrl: './callback.component.html',
})
export class CallbackComponent implements OnInit {
  private readonly oidcSecurityService = inject(OidcSecurityService);
  private readonly router = inject(Router);

  statusText = '正在完成登录回调，请稍候...';

  async ngOnInit(): Promise<void> {
    const search = new URLSearchParams(window.location.search);
    const hasOidcCallbackParams = search.has('code') || search.has('state') || search.has('error');

    if (!hasOidcCallbackParams) {
      await this.router.navigateByUrl('/home', { replaceUrl: true });
      return;
    }

    try {
      const result = await firstValueFrom(this.oidcSecurityService.checkAuth());
      await this.router.navigateByUrl(result.isAuthenticated ? '/home' : '/home', { replaceUrl: true });
    } catch (error) {
      console.error('OIDC 回调处理失败', error);
      this.statusText = '登录回调处理失败，正在返回首页...';
      await this.router.navigateByUrl('/home', { replaceUrl: true });
    }
  }
}
