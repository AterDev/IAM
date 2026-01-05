import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatExpansionModule } from '@angular/material/expansion';
import { SnackbarService } from '../shared/snackbar.service';

@Component({
  selector: 'app-protected',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatListModule,
    MatProgressSpinnerModule,
    MatExpansionModule
  ],
  templateUrl: './protected.component.html'
})
export class ProtectedComponent implements OnInit {
  private oidcSecurityService = inject(OidcSecurityService);
  private http = inject(HttpClient);
  private snackbar = inject(SnackbarService);

  userData: any;
  apiResponse: any;
  loading = false;

  ngOnInit() {
    // 方式1：从 OIDC 库的 userData$ 获取
    this.oidcSecurityService.userData$.subscribe(userData => {
      console.log('📝 Protected 页面 - 库返回的用户数据:', userData);
      if (userData?.userData) {
        this.userData = userData.userData;
      }
    });
    
    // 方式2：直接调用 userinfo 端点（备选方案）
    this.oidcSecurityService.getAccessToken().subscribe(token => {
      console.log('🔑 Protected 页面 - AccessToken:', token);
      
      if (token) {
        // 直接调用 IAM 的 userinfo 端点
        this.http.get('https://localhost:7070/connect/userinfo')
          .subscribe({
            next: (userInfo: any) => {
              console.log('👤 从 userinfo 端点获取的用户信息:', userInfo);
              this.userData = userInfo;
            },
            error: (err) => {
              console.error('❌ 获取用户信息失败:', err);
            }
          });
      }
    });
    
    // 调试：获取认证状态
    this.oidcSecurityService.isAuthenticated$.subscribe(authState => {
      console.log('🔐 Protected 页面 - 认证状态:', authState);
    });
  }

  callPublicApi() {
    this.loading = true;
    this.apiResponse = null;

    this.http.get('https://localhost:7000/api/public')
      .subscribe({
        next: (response) => {
          this.apiResponse = response;
          this.loading = false;
          this.snackbar.showSuccess('公开API调用成功');
        },
        error: (err) => {
          this.handleError(err, '调用公开API失败');
        }
      });
  }

  callProtectedApi() {
    this.loading = true;
    this.apiResponse = null;

    this.http.get('https://localhost:7000/api/protected')
      .subscribe({
        next: (response) => {
          this.apiResponse = response;
          this.loading = false;
          this.snackbar.showSuccess('受保护API调用成功');
        },
        error: (err) => {
          this.handleError(err, '调用受保护API失败');
        }
      });
  }

  callWeatherApi() {
    this.loading = true;
    this.apiResponse = null;

    this.http.get('https://localhost:7000/api/weatherforecast')
      .subscribe({
        next: (response) => {
          this.apiResponse = response;
          this.loading = false;
          this.snackbar.showSuccess('天气预报获取成功');
        },
        error: (err) => {
          this.handleError(err, '获取天气预报失败');
        }
      });
  }

  private handleError(err: any, message: string) {
    this.loading = false;
    let finalMessage: string;
    if (err.status === 401) {
      finalMessage = `${message}: 未授权 (401)`;
    } else if (err.status === 403) {
      finalMessage = `${message}: 禁止访问 (403)`;
    } else if (err.status === 0) {
      finalMessage = `${message}: 无法连接到API服务器`;
    } else {
      finalMessage = `${message}: ${err.error?.message || err.message || '未知错误'}`;
    }
    this.snackbar.showError(finalMessage);
  }
}
