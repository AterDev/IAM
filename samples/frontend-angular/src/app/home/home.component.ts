import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';
import { MatChipsModule } from '@angular/material/chips';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatListModule, MatChipsModule],
  templateUrl: './home.component.html'
})
export class HomeComponent implements OnInit {
  private oidcSecurityService = inject(OidcSecurityService);
  isAuthenticated$ = this.oidcSecurityService.isAuthenticated$;

  ngOnInit(): void {
    // 输出认证状态
    this.oidcSecurityService.isAuthenticated$.subscribe((isAuth) => {
      console.log('🔐 认证状态:', isAuth);
    });

    // 输出 accessToken
    this.oidcSecurityService.getAccessToken().subscribe((token) => {
      console.log('🔑 AccessToken:', token);
      if (token) {
        console.log('✅ Token 已成功获取！');
      }
    });

    // 输出用户信息
    this.oidcSecurityService.userData$.subscribe((userData) => {
      console.log('👤 用户信息:', userData);
    });
  }
}
