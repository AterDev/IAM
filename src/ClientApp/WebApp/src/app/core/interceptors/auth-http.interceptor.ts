import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../services/auth.service';

/**
 * AuthHttpInterceptor handles:
 * 1. Injecting Access Token into HTTP requests
 * 2. Global error handling for authentication and authorization
 * 3. Automatic redirect to login on 401
 */
@Injectable()
export class AuthHttpInterceptor implements HttpInterceptor {
  constructor(
    private snb: MatSnackBar,
    private router: Router,
    private auth: AuthService
  ) { }

  intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // Clone the request and add Authorization header if token exists
    const token = this.getAccessToken();
    if (token && !this.isExcludedUrl(request.url)) {
      request = request.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
    }

    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        return this.handleError(error);
      })
    );
  }

  /**
   * Get the access token from localStorage
   */
  private getAccessToken(): string | null {
    return localStorage.getItem('accessToken');
  }

  /**
   * Check if URL should be excluded from token injection
   * (e.g., login, token refresh, public assets)
   */
  private isExcludedUrl(url: string): boolean {
    const excludedPatterns = [
      '/connect/token',
      '/connect/authorize',
      '/assets/',
      '.json'
    ];
    return excludedPatterns.some(pattern => url.includes(pattern));
  }

  /**
   * Handle HTTP errors with user-friendly messages
   */
  private handleError(error: HttpErrorResponse): Observable<never> {
    const errors = {
      detail: '无法连接到服务器，请检查网络连接!',
      status: 500,
    };

    switch (error.status) {
      case 401:
        errors.detail = '401: 未授权的请求，请重新登录';
        this.auth.logout();
        this.router.navigateByUrl('/login');
        break;
      case 403:
        errors.detail = '403: 已拒绝请求，您没有权限访问此资源';
        break;
      case 404:
        errors.detail = error.error?.detail || '404: 请求的资源不存在';
        break;
      case 409:
        errors.detail = error.error?.detail || '409: 资源冲突';
        break;
      case 500:
        errors.detail = error.error?.detail || '500: 服务器内部错误';
        if (error.error?.title) {
          errors.detail = `${error.error.title}: ${errors.detail}`;
        }
        break;
      default:
        if (error.error) {
          if (error.error.detail) {
            errors.detail = error.error.detail;
          }
          if (error.error.title) {
            errors.detail = `${error.error.title}: ${errors.detail}`;
          }
        } else if (error.message) {
          errors.detail = error.message;
        }
        break;
    }

    errors.status = error.status;
    this.snb.open(errors.detail, '了解', { duration: 10000 });
    return throwError(() => errors);
  }
}
