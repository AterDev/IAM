import { ApplicationConfig, importProvidersFrom } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { AuthModule, LogLevel } from 'angular-auth-oidc-client';
import { routes } from './app.routes';
import { authInterceptor } from './auth.interceptor';
import { provideAnimations } from '@angular/platform-browser/animations';
import { MatSnackBarModule } from '@angular/material/snack-bar';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideAnimations(),
    importProvidersFrom(MatSnackBarModule),
    importProvidersFrom(
      AuthModule.forRoot({
        config: {
          authority: 'https://localhost:7070',
          redirectUrl: window.location.origin,
          postLogoutRedirectUri: window.location.origin,
          clientId: 'FrontTest',
          scope: 'openid profile email ApiTest',
          responseType: 'code',
          silentRenew: true,
          useRefreshToken: true,
          logLevel: LogLevel.Debug,
          secureRoutes: ['https://localhost:7000/api'],
          customParamsAuthRequest: {
          }
        }
      })
    )
  ]
};
