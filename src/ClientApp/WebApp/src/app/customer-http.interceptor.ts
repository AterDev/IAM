import { Injectable, inject } from '@angular/core';
import { HttpRequest, HttpHandler, HttpEvent, HttpInterceptor, HttpErrorResponse } from '@angular/common/http';

import { catchError, switchMap } from 'rxjs/operators';
import { MatSnackBar } from '@angular/material/snack-bar';
import { from, Observable, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { AuthService } from './services/auth.service';

@Injectable()
export class CustomerHttpInterceptor implements HttpInterceptor {
  private snb = inject(MatSnackBar);
  private router = inject(Router);
  private auth = inject(AuthService);
  private readonly refreshAttemptHeader = 'X-Refresh-Attempt';
  
  intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    return next.handle(request)
      .pipe(
        catchError((error: HttpErrorResponse) => {
          if (this.shouldRefreshToken(request, error)) {
            return from(this.auth.refreshAccessToken()).pipe(
              switchMap((accessToken) => {
                if (!accessToken) {
                  return this.handleError(error);
                }

                const retriedRequest = request.clone({
                  setHeaders: {
                    Authorization: `Bearer ${accessToken}`,
                    [this.refreshAttemptHeader]: 'true',
                  },
                });

                return next.handle(retriedRequest).pipe(
                  catchError((retryError: HttpErrorResponse) => this.handleError(retryError))
                );
              }),
              catchError(() => this.handleError(error))
            );
          }

          return this.handleError(error);
        })
      );
  }

  private shouldRefreshToken(request: HttpRequest<any>, error: HttpErrorResponse): boolean {
    if (error.status !== 401) {
      return false;
    }

    if (request.headers.has(this.refreshAttemptHeader)) {
      return false;
    }

    if (request.url.includes('/connect/token')) {
      return false;
    }

    return this.auth.hasRefreshToken();
  }

  handleError(error: HttpErrorResponse) {
    if (error.error instanceof Blob) {
      return from(error.error.text()).pipe(
        switchMap((text: string) => {
          let errorBody = error.error;
          try {
            errorBody = JSON.parse(text);
          } catch (e) {
            console.error('Error parsing error blob', e);
          }
          const newError = new HttpErrorResponse({
            error: errorBody,
            headers: error.headers,
            status: error.status,
            url: error.url || undefined
          });
          return this.showError(newError);
        })
      );
    }
    return this.showError(error);
  }
  showError(error: HttpErrorResponse) {
    const errors = {
      detail: 'Server Error',
      status: 500,
    };

    switch (error.status) {
      case 401:
        errors.detail = error.error?.detail === 'session_revoked'
          ? '401: Session revoked'
          : '401: Unauthorized request';
        this.auth.handleUnauthorized();
        this.router.navigate(['/login'], {
          queryParams: {
            returnUrl: this.router.url
          }
        });
        break;
      case 403:
        errors.detail = error.error?.detail || error.error?.title || '403: 没有访问权限';
        break;
      case 404:
      case 409:
        errors.detail = error.error.detail;
        break;
      default:

        if (!error.error) {
          if (error.message) {
            errors.detail = error.message;
          }
          errors.status = error.status;
        } else {
          if (error.error.detail) {
            errors.detail = error.error.detail;
          }
          if (error.error.title) {
            errors.detail = error.error.title + ':' + errors.detail;
          }
        }
        break;
    }
    errors.status = error.status;
    this.snb.open(errors.detail, '了解', { duration: 10000 });
    return throwError(() => errors);
  }
}
