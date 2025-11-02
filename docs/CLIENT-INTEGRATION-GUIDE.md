# 客户端应用对接指南

本文档详细说明前端应用和后端应用如何对接IAM系统，包括管理后台配置和代码实现。

---

## 目录

1. [对接流程总览](#对接流程总览)
2. [后端应用对接](#后端应用对接)
3. [前端应用对接](#前端应用对接)
4. [管理后台配置](#管理后台配置)
5. [常见问题](#常见问题)

---

## 对接流程总览

### 基本流程

```
┌─────────────┐      ┌─────────────┐      ┌─────────────┐
│  客户端应用  │      │  IAM服务器   │      │  资源API    │
└──────┬──────┘      └──────┬──────┘      └──────┬──────┘
       │                    │                    │
       │ 1. 发起登录请求     │                    │
       ├───────────────────>│                    │
       │                    │                    │
       │ 2. 显示登录页面     │                    │
       │<───────────────────┤                    │
       │                    │                    │
       │ 3. 提交用户凭证     │                    │
       ├───────────────────>│                    │
       │                    │                    │
       │ 4. 返回授权码       │                    │
       │<───────────────────┤                    │
       │                    │                    │
       │ 5. 交换访问令牌     │                    │
       ├───────────────────>│                    │
       │                    │                    │
       │ 6. 返回访问令牌     │                    │
       │<───────────────────┤                    │
       │                    │                    │
       │ 7. 携带令牌访问API  │                    │
       ├────────────────────┼───────────────────>│
       │                    │                    │
       │                    │ 8. 验证令牌         │
       │                    │<───────────────────┤
       │                    │                    │
       │ 9. 返回资源数据     │                    │
       │<───────────────────┼────────────────────┤
```

### 主要步骤

1. **注册应用** - 在IAM管理后台注册客户端应用
2. **配置应用** - 设置重定向URI、授权类型、作用域等
3. **集成代码** - 在应用中添加认证配置和代码
4. **测试流程** - 验证登录、授权、API访问等功能

---

## 后端应用对接

后端应用（如ASP.NET Core API）需要验证访问令牌来保护资源。

### 1. 管理后台配置

#### 1.1 注册API资源

1. 登录IAM管理后台（默认 `https://localhost:5001`）
2. 导航到 **资源管理 → API资源**
3. 点击"添加资源"
4. 填写信息：
   - **名称**: `your-api` （英文，用于令牌的audience）
   - **显示名称**: 您的API
   - **描述**: （可选）
   - **作用域**: 定义API支持的作用域，如：
     - `api.read` - 读取权限
     - `api.write` - 写入权限

5. 点击"保存"

#### 1.2 注册后端客户端（可选）

如果后端需要调用其他API（服务间调用），需要注册客户端：

1. 导航到 **应用管理**
2. 点击"添加应用"
3. 填写信息：
   - **客户端ID**: `backend-service`
   - **显示名称**: 后端服务
   - **应用类型**: Web应用
   - **客户端类型**: 机密客户端
   - **授权类型**: 勾选"客户端凭证"
   - **作用域**: 选择需要访问的API作用域
4. 点击"保存"，记录生成的**客户端密钥**

### 2. 代码实现

#### 2.1 安装NuGet包

```bash
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

#### 2.2 配置appsettings.json

```json
{
  "Authentication": {
    "Authority": "https://your-iam-server",
    "Audience": "your-api",
    "RequireHttpsMetadata": true
  }
}
```

#### 2.3 添加认证服务（Program.cs）

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// 配置JWT认证
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // IAM服务器地址
        options.Authority = builder.Configuration["Authentication:Authority"];
        
        // API资源名称（audience）
        options.Audience = builder.Configuration["Authentication:Audience"];
        
        // 令牌验证参数
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(5) // 允许5分钟时钟偏移
        };

        // 开发环境配置
        if (builder.Environment.IsDevelopment())
        {
            options.RequireHttpsMetadata = false; // 允许HTTP（仅开发）
        }
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// 启用认证和授权中间件
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
```

#### 2.4 保护API端点

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ResourcesController : ControllerBase
{
    // 公开端点（无需认证）
    [HttpGet("public")]
    public IActionResult GetPublic()
    {
        return Ok(new { Message = "公开资源" });
    }

    // 需要认证
    [Authorize]
    [HttpGet("protected")]
    public IActionResult GetProtected()
    {
        var userId = User.FindFirst("sub")?.Value;
        var username = User.Identity?.Name;
        
        return Ok(new 
        { 
            Message = "受保护资源",
            UserId = userId,
            Username = username
        });
    }

    // 需要特定作用域
    [Authorize(Policy = "RequireReadScope")]
    [HttpGet("data")]
    public IActionResult GetData()
    {
        return Ok(new { Data = "敏感数据" });
    }
}
```

#### 2.5 配置基于作用域的授权策略

```csharp
// 在Program.cs中添加
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireReadScope", policy =>
        policy.RequireClaim("scope", "api.read"));
        
    options.AddPolicy("RequireWriteScope", policy =>
        policy.RequireClaim("scope", "api.write"));
});
```

#### 2.6 服务间调用（客户端凭证模式）

如果后端需要调用其他API：

```csharp
using System.Net.Http.Headers;
using System.Text.Json;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public ApiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<string> CallOtherApiAsync()
    {
        // 1. 获取访问令牌
        var token = await GetAccessTokenAsync();
        
        // 2. 设置Authorization头
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);
        
        // 3. 调用API
        var response = await _httpClient.GetAsync("https://other-api/resource");
        return await response.Content.ReadAsStringAsync();
    }

    private async Task<string> GetAccessTokenAsync()
    {
        var tokenEndpoint = $"{_configuration["Authentication:Authority"]}/connect/token";
        
        var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _configuration["Authentication:ClientId"],
            ["client_secret"] = _configuration["Authentication:ClientSecret"],
            ["scope"] = "target-api.read target-api.write"
        });

        var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(content);
        
        return tokenResponse?.AccessToken ?? throw new Exception("获取令牌失败");
    }
}

public class TokenResponse
{
    public string AccessToken { get; set; }
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; }
}
```

---

## 前端应用对接

前端应用（如Angular、React、Vue）使用OAuth 2.0授权码流程 + PKCE。

### 1. 管理后台配置

#### 1.1 注册前端客户端

1. 登录IAM管理后台
2. 导航到 **应用管理**
3. 点击"添加应用"
4. 填写信息：
   - **客户端ID**: `frontend-app`
   - **显示名称**: 前端应用
   - **应用类型**: 单页应用（SPA）
   - **客户端类型**: 公共客户端
   - **授权类型**: 勾选"授权码"
   - **需要PKCE**: 是（必须）
   - **需要客户端密钥**: 否
   - **重定向URI**: 
     - `http://localhost:4200` （开发）
     - `http://localhost:4200/callback` （回调）
     - `https://yourdomain.com` （生产）
     - `https://yourdomain.com/callback`
   - **登出后重定向URI**:
     - `http://localhost:4200`
     - `https://yourdomain.com`
   - **允许的作用域**: 
     - `openid` （必需）
     - `profile`
     - `email`
     - `offline_access` （如需刷新令牌）
     - 以及需要访问的API作用域

5. 点击"保存"

### 2. 代码实现（Angular示例）

#### 2.1 安装依赖

```bash
npm install angular-auth-oidc-client
```

#### 2.2 配置认证（app.config.ts）

```typescript
import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { authInterceptor, provideAuth } from 'angular-auth-oidc-client';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(
      withInterceptors([authInterceptor()])
    ),
    provideAuth({
      config: {
        authority: 'https://your-iam-server',
        redirectUrl: window.location.origin,
        postLogoutRedirectUri: window.location.origin,
        clientId: 'frontend-app',
        scope: 'openid profile email offline_access your-api.read',
        responseType: 'code',
        silentRenew: true,
        useRefreshToken: true,
        renewTimeBeforeTokenExpiresInSeconds: 30,
        secureRoutes: ['https://your-api-server/'],
        customParamsAuthRequest: {
          prompt: 'login' // 可选：强制登录
        }
      }
    })
  ]
};
```

#### 2.3 创建认证服务（auth.service.ts）

```typescript
import { Injectable, inject } from '@angular/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private oidcSecurityService = inject(OidcSecurityService);

  // 检查是否已认证
  get isAuthenticated$(): Observable<boolean> {
    return this.oidcSecurityService.isAuthenticated$;
  }

  // 获取用户数据
  get userData$() {
    return this.oidcSecurityService.userData$;
  }

  // 获取访问令牌
  getAccessToken(): string {
    return this.oidcSecurityService.getAccessToken();
  }

  // 登录
  login(): void {
    this.oidcSecurityService.authorize();
  }

  // 登出
  logout(): void {
    this.oidcSecurityService.logoff().subscribe();
  }

  // 检查授权
  checkAuth(): Observable<boolean> {
    return this.oidcSecurityService.checkAuth();
  }
}
```

#### 2.4 创建路由守卫（auth.guard.ts）

```typescript
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { map, take } from 'rxjs/operators';

export const authGuard = () => {
  const oidcSecurityService = inject(OidcSecurityService);
  const router = inject(Router);

  return oidcSecurityService.isAuthenticated$.pipe(
    take(1),
    map((isAuthenticated) => {
      if (!isAuthenticated) {
        router.navigate(['/unauthorized']);
        return false;
      }
      return true;
    })
  );
};
```

#### 2.5 配置路由（app.routes.ts）

```typescript
import { Routes } from '@angular/router';
import { authGuard } from './auth.guard';

export const routes: Routes = [
  {
    path: '',
    component: HomeComponent
  },
  {
    path: 'protected',
    component: ProtectedComponent,
    canActivate: [authGuard] // 需要认证
  },
  {
    path: 'unauthorized',
    component: UnauthorizedComponent
  }
];
```

#### 2.6 在组件中使用

```typescript
import { Component, inject } from '@angular/core';
import { AuthService } from './auth.service';
import { AsyncPipe } from '@angular/common';

@Component({
  selector: 'app-root',
  template: `
    <div>
      @if (isAuthenticated$ | async) {
        <p>欢迎, {{ (userData$ | async)?.name }}</p>
        <button (click)="logout()">登出</button>
      } @else {
        <button (click)="login()">登录</button>
      }
    </div>
  `,
  imports: [AsyncPipe]
})
export class AppComponent {
  private authService = inject(AuthService);
  
  isAuthenticated$ = this.authService.isAuthenticated$;
  userData$ = this.authService.userData$;

  login(): void {
    this.authService.login();
  }

  logout(): void {
    this.authService.logout();
  }
}
```

#### 2.7 调用受保护的API

```typescript
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private http = inject(HttpClient);
  private apiUrl = 'https://your-api-server/api';

  // HTTP拦截器会自动添加Bearer令牌
  getData() {
    return this.http.get(`${this.apiUrl}/protected/data`);
  }

  createResource(data: any) {
    return this.http.post(`${this.apiUrl}/protected/resource`, data);
  }
}
```

### 3. React / Vue示例

#### React (使用 oidc-client-ts)

```javascript
import { UserManager } from 'oidc-client-ts';

const userManager = new UserManager({
  authority: 'https://your-iam-server',
  client_id: 'frontend-app',
  redirect_uri: window.location.origin + '/callback',
  post_logout_redirect_uri: window.location.origin,
  response_type: 'code',
  scope: 'openid profile email your-api.read',
  filterProtocolClaims: true,
  loadUserInfo: true
});

// 登录
export const login = () => userManager.signinRedirect();

// 处理回调
export const handleCallback = () => userManager.signinRedirectCallback();

// 登出
export const logout = () => userManager.signoutRedirect();

// 获取用户
export const getUser = () => userManager.getUser();
```

---

## 管理后台配置

### 配置应用步骤

#### 1. 访问管理后台

```
URL: https://your-iam-server
用户名: admin
密码: （您设置的管理员密码）
```

#### 2. 创建应用

1. 点击左侧菜单 **应用管理**
2. 点击右上角"添加应用"按钮
3. 填写表单：

**基本信息**：
- 客户端ID: 唯一标识符（如`my-app`）
- 显示名称: 用户看到的名称
- 描述: 应用描述（可选）

**应用类型**：
- Web应用: 传统服务器端应用
- 单页应用(SPA): Angular/React/Vue等
- 原生应用: 移动应用或桌面应用

**客户端类型**：
- 机密客户端: 可以安全存储密钥（后端应用）
- 公共客户端: 无法安全存储密钥（前端应用）

**授权类型**（可多选）：
- ☑ 授权码: 标准OAuth 2.0流程
- ☐ 客户端凭证: 服务间调用
- ☐ 密码: 信任的客户端（不推荐）
- ☐ 刷新令牌: 启用令牌刷新
- ☐ 设备码: 受限输入设备

**安全设置**：
- 需要PKCE: SPA必须启用
- 需要客户端密钥: 机密客户端必须启用

**重定向设置**：
- 重定向URI: 登录成功后跳转地址（每行一个）
- 登出后重定向URI: 登出后跳转地址

**作用域**：
- 选择应用可以访问的作用域

4. 点击"保存"

#### 3. 配置作用域

如果需要自定义API作用域：

1. 导航到 **作用域管理**
2. 点击"添加作用域"
3. 填写：
   - 名称: `api.read`
   - 显示名称: 读取API
   - 描述: 允许读取API资源
   - 是否必需: 否
   - 是否强调: 否

#### 4. 分配用户

1. 导航到 **用户管理**
2. 选择用户
3. 点击"编辑"
4. 在"角色"或"权限"中分配相应权限

### 常见配置场景

#### 场景1: Angular SPA + ASP.NET Core API

**SPA配置**：
- 客户端ID: `angular-app`
- 应用类型: 单页应用
- 客户端类型: 公共
- 授权类型: ☑ 授权码, ☑ 刷新令牌
- 需要PKCE: ☑ 是
- 需要密钥: ☐ 否
- 重定向URI: `http://localhost:4200`, `http://localhost:4200/callback`
- 作用域: `openid`, `profile`, `email`, `my-api.read`

**API配置**：
- 在"资源管理"中创建API资源
- 名称: `my-api`
- 作用域: `my-api.read`, `my-api.write`

#### 场景2: 微服务架构

**服务A调用服务B**：
- 客户端ID: `service-a`
- 应用类型: Web应用
- 客户端类型: 机密
- 授权类型: ☑ 客户端凭证
- 需要密钥: ☑ 是
- 作用域: `service-b.api`

---

## 常见问题

### Q1: 重定向URI不匹配

**错误**: `redirect_uri_mismatch`

**解决**:
- 确保应用配置中的重定向URI与代码中完全一致
- 注意协议（http/https）
- 注意端口号
- 注意尾部斜杠

### Q2: CORS错误

**错误**: `Access to XMLHttpRequest has been blocked by CORS policy`

**解决**:
- API服务器需要配置CORS
- 允许来自前端应用的origin
- 允许Authorization头

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
```

### Q3: 令牌验证失败

**错误**: `401 Unauthorized`

**解决**:
- 检查Authority配置是否正确
- 检查Audience是否匹配API资源名称
- 确保令牌未过期
- 检查时钟同步（ClockSkew）

### Q4: 作用域不足

**错误**: `insufficient_scope`

**解决**:
- 在客户端配置中添加所需作用域
- 在用户或角色中授予相应权限
- 重新登录以获取新令牌

### Q5: PKCE验证失败

**错误**: `invalid_request: code_verifier`

**解决**:
- 确保客户端配置启用了PKCE
- 检查PKCE实现是否正确
- 使用标准OIDC库而非手动实现

### Q6: 刷新令牌失效

**错误**: `invalid_grant`

**解决**:
- 检查是否启用了refresh_token授权类型
- 检查刷新令牌是否过期
- 检查刷新令牌是否已被撤销

---

## 总结

### 对接检查清单

**后端API**：
- [ ] 安装JWT Bearer认证包
- [ ] 配置Authority和Audience
- [ ] 添加认证和授权中间件
- [ ] 使用`[Authorize]`保护端点
- [ ] 在IAM中注册API资源

**前端应用**：
- [ ] 安装OIDC客户端库
- [ ] 配置客户端ID和作用域
- [ ] 实现登录/登出功能
- [ ] 配置路由守卫
- [ ] 在IAM中注册客户端应用

**管理后台**：
- [ ] 创建API资源（如需）
- [ ] 创建客户端应用
- [ ] 配置重定向URI
- [ ] 分配作用域
- [ ] 创建测试用户

---

## 参考资源

- [OAuth 2.0 规范](https://tools.ietf.org/html/rfc6749)
- [OpenID Connect 规范](https://openid.net/specs/openid-connect-core-1_0.html)
- [PKCE 规范](https://tools.ietf.org/html/rfc7636)
- [JWT 规范](https://tools.ietf.org/html/rfc7519)
