import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { switchMap, take } from 'rxjs/operators';
import { environment } from '../environments/environment';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith(environment.backendApiUrl) && !req.url.startsWith(environment.iamApiUrl)) {
    return next(req);
  }

  const oidcSecurityService = inject(OidcSecurityService);

  return oidcSecurityService.getAccessToken().pipe(
    take(1),
    switchMap((token: string) => {
      if (token) {
        return next(req.clone({
          headers: req.headers.set('Authorization', `Bearer ${token}`),
        }));
      }

      return next(req);
    }),
  );
};
