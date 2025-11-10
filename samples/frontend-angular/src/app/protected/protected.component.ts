import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { OidcSecurityService } from 'angular-auth-oidc-client';

@Component({
  selector: 'app-protected',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="protected">
      <h2>受保护资源</h2>
      
      <div class="user-info">
        <h3>👤 用户信息</h3>
        @if (userData) {
          <dl>
            <dt>姓名:</dt>
            <dd>{{ userData.name || userData.preferred_username || 'N/A' }}</dd>
            <dt>邮箱:</dt>
            <dd>{{ userData.email || 'N/A' }}</dd>
            <dt>用户ID:</dt>
            <dd><code>{{ userData.sub || 'N/A' }}</code></dd>
            @if (userData.role) {
              <dt>角色:</dt>
              <dd>{{ userData.role }}</dd>
            }
          </dl>
          
          <details class="token-info">
            <summary>🔑 查看访问令牌信息</summary>
            <div class="token-content">
              <p><strong>令牌声明 (Token Claims):</strong></p>
              <pre>{{ userData | json }}</pre>
            </div>
          </details>
        } @else {
          <p>正在加载用户信息...</p>
        }
      </div>

      <div class="api-test">
        <h3>🚀 API调用测试</h3>
        <p>测试调用受保护的API端点，访问令牌会自动通过HTTP拦截器添加：</p>
        
        <div class="button-group">
          <button (click)="callPublicApi()" [disabled]="loading" class="btn-secondary">
            {{ loading ? '加载中...' : '调用公开API' }}
          </button>
          <button (click)="callProtectedApi()" [disabled]="loading" class="btn-primary">
            {{ loading ? '加载中...' : '调用受保护API' }}
          </button>
          <button (click)="callWeatherApi()" [disabled]="loading" class="btn-primary">
            {{ loading ? '加载中...' : '获取天气预报' }}
          </button>
        </div>

        @if (apiResponse) {
          <div class="response success">
            <h4>✅ API响应成功:</h4>
            <pre>{{ apiResponse | json }}</pre>
          </div>
        }

        @if (error) {
          <div class="response error">
            <h4>❌ 错误:</h4>
            <p>{{ error }}</p>
            <details>
              <summary>查看详细错误信息</summary>
              <pre>{{ errorDetails | json }}</pre>
            </details>
          </div>
        }
      </div>

      <div class="tips">
        <h3>💡 提示</h3>
        <ul>
          <li>访问令牌会自动包含在API请求的Authorization头中</li>
          <li>令牌格式为: <code>Bearer {access_token}</code></li>
          <li>后端API会验证令牌的签名、有效期和audience</li>
          <li>如果令牌即将过期，会自动使用刷新令牌续订</li>
        </ul>
      </div>
    </div>
  `,
  styles: [`
    .protected {
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

    .user-info {
      background: #e8f5e9;
      padding: 20px;
      border-radius: 8px;
      margin-bottom: 20px;
      border-left: 4px solid #4caf50;
    }

    dl {
      display: grid;
      grid-template-columns: 120px 1fr;
      gap: 10px;
      margin-bottom: 15px;
    }

    dt {
      font-weight: bold;
      color: #555;
    }

    dd {
      margin: 0;
    }

    .token-info {
      margin-top: 15px;
      cursor: pointer;
    }

    .token-info summary {
      color: #1976d2;
      font-weight: 500;
      padding: 8px;
      background: #f5f5f5;
      border-radius: 4px;
    }

    .token-content {
      margin-top: 10px;
      padding: 10px;
      background: white;
      border-radius: 4px;
    }

    .api-test {
      margin-top: 30px;
    }

    .button-group {
      display: flex;
      gap: 10px;
      margin: 15px 0;
      flex-wrap: wrap;
    }

    button {
      border: none;
      padding: 12px 24px;
      border-radius: 4px;
      cursor: pointer;
      font-size: 16px;
      transition: all 0.3s;
    }

    .btn-primary {
      background: #1976d2;
      color: white;
    }

    .btn-primary:hover:not(:disabled) {
      background: #1565c0;
      transform: translateY(-2px);
      box-shadow: 0 4px 8px rgba(0,0,0,0.2);
    }

    .btn-secondary {
      background: #757575;
      color: white;
    }

    .btn-secondary:hover:not(:disabled) {
      background: #616161;
      transform: translateY(-2px);
      box-shadow: 0 4px 8px rgba(0,0,0,0.2);
    }

    button:disabled {
      opacity: 0.6;
      cursor: not-allowed;
      transform: none !important;
    }

    .response {
      margin-top: 20px;
      padding: 15px;
      border-radius: 8px;
      animation: slideIn 0.3s ease-out;
    }

    @keyframes slideIn {
      from {
        opacity: 0;
        transform: translateY(-10px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .response.success {
      background: #e8f5e9;
      border: 1px solid #4caf50;
      border-left: 4px solid #4caf50;
    }

    .response.error {
      background: #ffebee;
      border: 1px solid #f44336;
      border-left: 4px solid #f44336;
    }

    .response h4 {
      margin-top: 0;
      margin-bottom: 10px;
    }

    pre {
      overflow-x: auto;
      white-space: pre-wrap;
      word-wrap: break-word;
      background: white;
      padding: 10px;
      border-radius: 4px;
      font-size: 14px;
    }

    code {
      background: #f5f5f5;
      padding: 2px 6px;
      border-radius: 3px;
      font-family: 'Courier New', monospace;
      color: #d32f2f;
    }

    .tips {
      background: #fff3e0;
      padding: 20px;
      border-radius: 8px;
      margin-top: 30px;
      border-left: 4px solid #ff9800;
    }

    .tips ul {
      line-height: 1.8;
      margin: 10px 0;
    }

    details {
      margin-top: 10px;
    }

    details summary {
      cursor: pointer;
      color: #1976d2;
      font-weight: 500;
    }
  `]
})
export class ProtectedComponent implements OnInit {
  private oidcSecurityService = inject(OidcSecurityService);
  private http = inject(HttpClient);

  userData: any;
  apiResponse: any;
  error: string = '';
  errorDetails: any;
  loading = false;

  ngOnInit() {
    this.oidcSecurityService.userData$.subscribe(userData => {
      this.userData = userData;
    });
  }

  callPublicApi() {
    this.loading = true;
    this.error = '';
    this.errorDetails = null;
    this.apiResponse = null;

    this.http.get('https://localhost:5001/api/public')
      .subscribe({
        next: (response) => {
          this.apiResponse = response;
          this.loading = false;
        },
        error: (err) => {
          this.handleError(err, '调用公开API失败');
        }
      });
  }

  callProtectedApi() {
    this.loading = true;
    this.error = '';
    this.errorDetails = null;
    this.apiResponse = null;

    this.http.get('https://localhost:5001/api/protected')
      .subscribe({
        next: (response) => {
          this.apiResponse = response;
          this.loading = false;
        },
        error: (err) => {
          this.handleError(err, '调用受保护API失败');
        }
      });
  }

  callWeatherApi() {
    this.loading = true;
    this.error = '';
    this.errorDetails = null;
    this.apiResponse = null;

    // Call the protected API endpoint
    this.http.get('https://localhost:5001/api/weatherforecast')
      .subscribe({
        next: (response) => {
          this.apiResponse = response;
          this.loading = false;
        },
        error: (err) => {
          this.handleError(err, '获取天气预报失败');
        }
      });
  }

  private handleError(err: any, message: string) {
    this.loading = false;
    this.errorDetails = err;
    
    if (err.status === 401) {
      this.error = `${message}: 未授权 (401) - 请确保已登录并且访问令牌有效`;
    } else if (err.status === 403) {
      this.error = `${message}: 禁止访问 (403) - 您没有访问此资源的权限`;
    } else if (err.status === 0) {
      this.error = `${message}: 无法连接到API服务器 - 请确保后端API正在运行在 https://localhost:5001`;
    } else {
      this.error = `${message}: ${err.error?.message || err.message || '未知错误'}`;
    }
  }
}
