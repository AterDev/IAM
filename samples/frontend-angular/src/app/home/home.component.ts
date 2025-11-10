import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OidcSecurityService } from 'angular-auth-oidc-client';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="home">
      <h2>欢迎使用IAM示例应用</h2>
      
      @if (isAuthenticated$ | async) {
        <div class="welcome-message">
          <p>✅ 您已成功登录！</p>
          <p>现在可以访问受保护的资源了。</p>
        </div>
      } @else {
        <div class="info">
          <p>这是一个演示如何使用Angular和OIDC对接IAM系统的示例应用。</p>
          <p>点击导航栏中的"Login"按钮进行身份验证。</p>
        </div>
      }

      <div class="features">
        <h3>功能特性</h3>
        <ul>
          <li>✨ OAuth 2.0 / OpenID Connect 认证</li>
          <li>🔄 自动令牌管理和刷新</li>
          <li>🔒 使用认证守卫保护路由</li>
          <li>🚀 HTTP拦截器自动注入令牌</li>
          <li>⏰ 静默令牌续订</li>
          <li>👤 用户信息显示</li>
        </ul>
      </div>

      <div class="getting-started">
        <h3>快速开始</h3>
        <ol>
          <li>确保IAM服务器运行在 <code>https://localhost:7001</code></li>
          <li>在IAM中注册客户端，客户端ID: <code>FrontTest</code></li>
          <li>配置重定向URI: <code>http://localhost:4200</code></li>
          <li>添加允许的作用域: <code>openid profile email ApiTest</code></li>
          <li>点击"Login"开始认证流程</li>
          <li>认证成功后，访问"Protected"页面查看用户信息</li>
          <li>点击"Call Protected API"测试调用后端API</li>
        </ol>
      </div>

      <div class="architecture">
        <h3>架构说明</h3>
        <p>本示例演示了三层架构的认证流程：</p>
        <ul>
          <li><strong>前端应用</strong> (本应用) - 运行在 http://localhost:4200</li>
          <li><strong>IAM认证服务器</strong> - 运行在 https://localhost:7001</li>
          <li><strong>后端API</strong> - 运行在 https://localhost:5001</li>
        </ul>
      </div>
    </div>
  `,
  styles: [`
    .home {
      padding: 20px;
    }

    h2 {
      color: #1976d2;
      margin-bottom: 20px;
    }

    h3 {
      color: #333;
      margin-top: 30px;
      margin-bottom: 15px;
    }

    .welcome-message {
      background: #e8f5e9;
      padding: 20px;
      border-radius: 8px;
      margin-bottom: 20px;
      border-left: 4px solid #4caf50;
    }

    .info {
      background: #e3f2fd;
      padding: 20px;
      border-radius: 8px;
      margin-bottom: 20px;
      border-left: 4px solid #2196f3;
    }

    .features ul, .getting-started ol, .architecture ul {
      line-height: 1.8;
    }

    .features ul {
      list-style: none;
      padding-left: 0;
    }

    code {
      background: #f5f5f5;
      padding: 2px 6px;
      border-radius: 3px;
      font-family: 'Courier New', monospace;
      color: #d32f2f;
    }

    .architecture {
      background: #fff3e0;
      padding: 20px;
      border-radius: 8px;
      margin-top: 20px;
      border-left: 4px solid #ff9800;
    }

    .architecture ul {
      margin-top: 10px;
    }
  `]
})
export class HomeComponent {
  private oidcSecurityService = inject(OidcSecurityService);
  isAuthenticated$ = this.oidcSecurityService.isAuthenticated$;
}
