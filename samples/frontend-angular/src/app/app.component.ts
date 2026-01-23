import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatChipsModule } from '@angular/material/chips';
import { map } from 'rxjs/operators';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    MatToolbarModule,
    MatSidenavModule,
    MatButtonModule,
    MatIconModule,
    MatListModule,
    MatChipsModule
  ],
  templateUrl: './app.component.html'
})
export class AppComponent implements OnInit {
  private oidcSecurityService = inject(OidcSecurityService);

  isAuthenticated = false;
  userData: any;
  darkMode = true;
  sidenavOpened = true;
  isInitialized = false;

  async ngOnInit() {
    console.log('🚀 开始 OIDC 初始化...');

    // 检查所有存储位置
    console.log('📦 localStorage 数量:', localStorage.length);

    console.log('📦 localStorage 所有键:');
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      console.log(`  [${i}] ${key}`);
    }

    try {
      // 等待库完全初始化，这很关键！
      const checkAuthResult = await firstValueFrom(this.oidcSecurityService.checkAuth());
      console.log('✅ OIDC checkAuth 完成，结果:', checkAuthResult);
      this.isInitialized = true;

      // 监听认证状态变化
      this.oidcSecurityService.isAuthenticated$
        .pipe(
          map((result) => result.isAuthenticated)
        )
        .subscribe((isAuthenticated) => {
          this.isAuthenticated = isAuthenticated;
          console.log('🔐 认证状态变化:', isAuthenticated);
        });

      this.oidcSecurityService.userData$.subscribe((userData) => {
        this.userData = userData;
        console.log('👤 用户信息:', userData);
      });

      // 调试：检查当前状态
      const authState = await firstValueFrom(this.oidcSecurityService.isAuthenticated$);
      console.log('📊 当前认证状态详情:', authState);

      // 获取所有 token
      const accessToken = await firstValueFrom(this.oidcSecurityService.getAccessToken());
      console.log('🔑 AccessToken:', accessToken || '(无)');

      const idToken = await firstValueFrom(this.oidcSecurityService.getIdToken());
      console.log('🔑 IdToken:', idToken || '(无)');

      const refreshToken = await firstValueFrom(this.oidcSecurityService.getRefreshToken());
      console.log('🔑 RefreshToken:', refreshToken || '(无)');

    } catch (error) {
      console.error('❌ OIDC 初始化失败:', error);
      this.isInitialized = true;
    }
  }

  login() {
    if (!this.isInitialized) {
      console.warn('OIDC库还未初始化，请稍候...');
      return;
    }
    this.oidcSecurityService.authorize();
  }

  logout() {
    this.oidcSecurityService.logoff().subscribe(() => {
      console.log('登出成功');
    });
  }

}
