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
          // IAM服务器地址
          authority: 'https://localhost:7001',
          // 登录成功后重定向到的URL
          redirectUrl: window.location.origin,
          // 登出后重定向到的URL
          postLogoutRedirectUri: window.location.origin,
          // 客户端ID - 使用IAM后台中已创建的FrontTest客户端
          clientId: 'FrontTest',
          // 请求的作用域
          scope: 'openid profile email ApiTest',
          // 使用授权码流程
          responseType: 'code',
          // 启用静默令牌续订
          silentRenew: true,
          // 使用刷新令牌
          useRefreshToken: true,
          // 日志级别 - 开发环境使用Debug，生产环境应使用None或Warn
          logLevel: LogLevel.Debug,
          // 受保护的路由 - 自动在请求这些URL时添加访问令牌
          secureRoutes: ['https://localhost:5001/api'],
          // 自定义授权请求参数
          customParamsAuthRequest: {
            // 可选：强制用户重新登录
            // prompt: 'login'
          }
        }
      })
    )
  ]
};
