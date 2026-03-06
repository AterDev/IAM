import { ApplicationConfig, importProvidersFrom } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  AbstractSecurityStorage,
  DefaultLocalStorageService,
  LogLevel,
  provideAuth,
  withAppInitializerAuthCheck,
} from 'angular-auth-oidc-client';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { routes } from './app.routes';
import { authInterceptor } from './auth.interceptor';
import { environment } from '../environments/environment';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    importProvidersFrom(MatSnackBarModule),
    provideAuth(
      {
        config: {
          authority: environment.iamApiUrl,
          authWellknownEndpointUrl: `${environment.iamApiUrl}/.well-known/openid-configuration`,
          redirectUrl: window.location.origin,
          postLogoutRedirectUri: window.location.origin,
          clientId: 'FrontClient',
          scope: 'openid profile email offline_access ApiTest',
          responseType: 'code',
          silentRenew: true,
          useRefreshToken: true,
          logLevel: LogLevel.Debug,
          secureRoutes: [environment.backendApiUrl, `${environment.backendApiUrl}/api`],
          customParamsAuthRequest: {},
          renewUserInfoAfterTokenRenew: false,
          unauthorizedRoute: '/unauthorized',
          disableIdTokenValidation: true,
          disableIatOffsetValidation: true,
          ignoreNonceAfterRefresh: true,
          allowUnsafeReuseRefreshToken: true,
        },
      },
      withAppInitializerAuthCheck(),
    ),
    { provide: AbstractSecurityStorage, useClass: DefaultLocalStorageService },
  ],
};
