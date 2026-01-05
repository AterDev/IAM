import { ApplicationConfig, importProvidersFrom, APP_INITIALIZER } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAuth, withAppInitializerAuthCheck, LogLevel, AbstractSecurityStorage, DefaultLocalStorageService } from 'angular-auth-oidc-client';
import { routes } from './app.routes';
import { authInterceptor } from './auth.interceptor';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { environment } from '../environments/environment';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    importProvidersFrom(MatSnackBarModule),
    // 使用现代的 provideAuth API 替代 AuthModule.forRoot
    provideAuth(
      {
        config: {
          authority: environment.iamApiUrl,
          authWellknownEndpointUrl: `${environment.iamApiUrl}/.well-known/openid-configuration`,
          redirectUrl: window.location.origin,
          postLogoutRedirectUri: window.location.origin,
          clientId: 'FrontClient',
          scope: 'openid profile email offline_access',
          responseType: 'code',
          silentRenew: true,
          useRefreshToken: true,
          logLevel: LogLevel.Debug,
          secureRoutes: [`${environment.backendApiUrl}/api`],
          customParamsAuthRequest: {},
          renewUserInfoAfterTokenRenew: false,
          unauthorizedRoute: '/unauthorized',
          // 关键：禁用各种验证（开发环境）
          disableIdTokenValidation: true,
          disableIatOffsetValidation: true,
          ignoreNonceAfterRefresh: true,
          allowUnsafeReuseRefreshToken: true
        }
      },
      // 关键：使用应用初始化器自动处理 OAuth 回调
      withAppInitializerAuthCheck()
    ),
    // 关键：在 provideAuth 之后显式提供 localStorage
    { provide: AbstractSecurityStorage, useClass: DefaultLocalStorageService }
  ]
};
