import { ApplicationConfig, importProvidersFrom } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  AbstractSecurityStorage,
  DefaultLocalStorageService,
  LogLevel,
  provideAuth,
} from 'angular-auth-oidc-client';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { provideTranslateService } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';
import { routes } from './app.routes';
import { authInterceptor } from './auth.interceptor';
import { environment } from '../environments/environment';

export const languageStorageKey = 'front-sample-lang';
const defaultLanguage = 'zh';

export function getInitialLanguage(): 'zh' | 'en' {
  if (typeof window === 'undefined') {
    return defaultLanguage;
  }

  return window.localStorage.getItem(languageStorageKey) === 'en' ? 'en' : defaultLanguage;
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    importProvidersFrom(MatSnackBarModule),
    provideTranslateService({
      fallbackLang: defaultLanguage,
      lang: getInitialLanguage(),
      defaultLanguage: defaultLanguage,
      extend: true,
      useDefaultLang: true,
    }),
    provideTranslateHttpLoader({
      prefix: './assets/i18n/',
      suffix: '.json',
    }),
    provideAuth({
      config: {
        authority: environment.iamApiUrl,
        authWellknownEndpointUrl: `${environment.iamApiUrl}/.well-known/openid-configuration`,
        redirectUrl: `${window.location.origin}/auth/callback`,
        postLoginRoute: '/home',
        postLogoutRedirectUri: window.location.origin,
        clientId: 'FrontSampleClient',
        scope: 'openid profile email offline_access SampleAPI',
        responseType: 'code',
        silentRenew: true,
        useRefreshToken: true,
        logLevel: LogLevel.Debug,
        secureRoutes: [environment.backendApiUrl, `${environment.backendApiUrl}/api`],
        customParamsAuthRequest: {},
        renewUserInfoAfterTokenRenew: false,
        unauthorizedRoute: '/home',
        ignoreNonceAfterRefresh: true,
      },
    }),
    { provide: AbstractSecurityStorage, useClass: DefaultLocalStorageService },
  ],
};
