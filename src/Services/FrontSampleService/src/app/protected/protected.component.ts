import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { environment } from '../../environments/environment';
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
    MatExpansionModule,
  ],
  templateUrl: './protected.component.html',
})
export class ProtectedComponent implements OnInit {
  private readonly oidcSecurityService = inject(OidcSecurityService);
  private readonly http = inject(HttpClient);
  private readonly snackbar = inject(SnackbarService);

  userData: Record<string, unknown> | null = null;
  apiResponse: unknown;
  loading = false;

  ngOnInit() {
    this.oidcSecurityService.userData$.subscribe((userData) => {
      this.userData = (userData?.userData ?? userData) as Record<string, unknown> | null;
    });

    this.oidcSecurityService.getAccessToken().subscribe((token) => {
      if (token) {
        this.http.get(`${environment.iamApiUrl}/connect/userinfo`).subscribe({
          next: (userInfo) => {
            this.userData = userInfo as Record<string, unknown>;
          },
          error: (error) => {
            console.error('获取用户信息失败', error);
          },
        });
      }
    });
  }

  callPublicApi() {
    this.invokeApi(`${environment.backendApiUrl}/api/public`, '公开 API 调用成功', '调用公开 API 失败');
  }

  callProtectedApi() {
    this.invokeApi(`${environment.backendApiUrl}/api/protected`, '受保护 API 调用成功', '调用受保护 API 失败');
  }

  callWeatherApi() {
    this.invokeApi(`${environment.backendApiUrl}/api/weatherforecast`, '天气预报获取成功', '获取天气预报失败');
  }

  private invokeApi(url: string, successMessage: string, errorMessage: string) {
    this.loading = true;
    this.apiResponse = null;

    this.http.get(url).subscribe({
      next: (response) => {
        this.apiResponse = response;
        this.loading = false;
        this.snackbar.showSuccess(successMessage);
      },
      error: (err) => {
        this.handleError(err, errorMessage);
      },
    });
  }

  private handleError(err: { status?: number; error?: { message?: string }; message?: string }, message: string) {
    this.loading = false;

    if (err.status === 401) {
      this.snackbar.showError(`${message}: 未授权 (401)`);
      return;
    }

    if (err.status === 403) {
      this.snackbar.showError(`${message}: 禁止访问 (403)`);
      return;
    }

    if (err.status === 0) {
      this.snackbar.showError(`${message}: 无法连接到 API 服务`);
      return;
    }

    this.snackbar.showError(`${message}: ${err.error?.message || err.message || '未知错误'}`);
  }
}
