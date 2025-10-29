import { ApplicationConfig, importProvidersFrom } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { AuthModule, LogLevel } from 'angular-auth-oidc-client';
import { routes } from './app.routes';
import { authInterceptor } from './auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    importProvidersFrom(
      AuthModule.forRoot({
        config: {
          authority: 'https://localhost:7001',
          redirectUrl: window.location.origin,
          postLogoutRedirectUri: window.location.origin,
          clientId: 'sample-angular-client',
          scope: 'openid profile email sample-api',
          responseType: 'code',
          silentRenew: true,
          useRefreshToken: true,
          logLevel: LogLevel.Debug,
          secureRoutes: ['https://localhost:5001/api'],
          customParamsAuthRequest: {
            prompt: 'login'
          }
        }
      })
    )
  ]
};
