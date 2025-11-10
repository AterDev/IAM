# IAM Angular示例 - OIDC集成

这是一个Angular示例应用，展示如何使用OpenID Connect对接IAM授权系统，使用 `angular-auth-oidc-client` 库实现。

## 功能特性

- ✨ OpenID Connect认证流程
- 🔄 自动访问令牌管理
- 🔒 使用认证守卫保护路由
- 🚀 HTTP拦截器自动注入令牌到API请求
- ⏰ 静默令牌续订
- 👤 用户资料显示
- 📡 调用受保护API的示例

## 前置要求

- Node.js 20+ 和 npm
- IAM服务器运行在 `https://localhost:7001`
- 示例API运行在 `https://localhost:5001`（可选，用于测试API调用）

## IAM配置

在运行此应用之前，需要在IAM管理后台配置客户端。

### 客户端配置

本示例使用IAM后台中已创建的客户端：
- **客户端ID**: `FrontTest`
- **客户端名称**: 前端测试客户端
- **应用类型**: 单页应用(SPA)

### 必需的配置设置

如果客户端尚未创建，请按以下步骤在IAM中配置：

1. 登录IAM管理后台，使用凭据：
   - 用户名: `admin`
   - 密码: `MakeDotnetGreatAgain`

2. 创建新客户端，配置如下：
   - **客户端ID**: `FrontTest`
   - **客户端名称**: 前端示例应用
   - **应用类型**: 单页应用(SPA)
   - **客户端类型**: 公共客户端
   - **授权类型**: 授权码 + PKCE
   - **重定向URI**: 
     - `http://localhost:4200`
     - `http://localhost:4200/`
   - **登出后重定向URI**:
     - `http://localhost:4200`
     - `http://localhost:4200/`
   - **允许的CORS源**: `http://localhost:4200`
   - **允许的作用域**:
     - `openid` （必需）
     - `profile`
     - `email`
     - `ApiTest` （用于调用示例API）
   - **需要PKCE**: 是
   - **需要客户端密钥**: 否
   - **允许刷新令牌**: 是

### API资源配置（可选）

如果要调用受保护的API：

1. 确保API资源已创建：
   - **名称**: `ApiTest`
   - **显示名称**: API测试
   - **作用域**: 定义API需要的作用域

2. 确保客户端有访问 `ApiTest` 作用域的权限

## 安装

1. 导航到示例目录：
   ```bash
   cd samples/frontend-angular
   ```

2. 安装依赖：
   ```bash
   npm install
   ```

## 配置

OIDC配置在 `src/app/app.config.ts` 中：

```typescript
{
  authority: 'https://localhost:7001',      // IAM服务器地址
  redirectUrl: window.location.origin,      // 登录成功后重定向URL
  postLogoutRedirectUri: window.location.origin, // 登出后重定向URL
  clientId: 'FrontTest',                    // 客户端ID
  scope: 'openid profile email ApiTest',    // 请求的作用域
  responseType: 'code',                     // 使用授权码流程
  silentRenew: true,                        // 启用静默令牌续订
  useRefreshToken: true                     // 使用刷新令牌
}
```

如果IAM服务器运行在不同的URL或端口，请调整这些设置。

## 运行应用

启动开发服务器：

```bash
npm start
```

应用将在 `http://localhost:4200` 可用。

## 应用结构

```
src/
├── app/
│   ├── home/                    # 首页组件
│   ├── protected/               # 受保护页面组件（需要认证）
│   ├── unauthorized/            # 未授权页面组件
│   ├── app.component.ts         # 根组件，包含导航
│   ├── app.config.ts            # 应用配置，包含OIDC设置
│   ├── app.routes.ts            # 路由配置
│   └── auth.interceptor.ts      # HTTP拦截器，用于添加认证令牌
├── index.html
├── main.ts
└── styles.scss
```

## 演示的功能

### 1. 认证流程

- 点击"登录"按钮启动OAuth 2.0授权码流程 + PKCE
- 用户被重定向到IAM登录页面
- 成功认证后，用户被重定向回应用
- 访问令牌和刷新令牌自动管理

### 2. 受保护路由

`/protected` 路由使用 `AutoLoginPartialRoutesGuard` 保护：
- 未认证用户被重定向到IAM登录
- 登录后，用户被重定向回受保护路由

### 3. HTTP拦截器

`authInterceptor` 自动将访问令牌添加到HTTP请求：
- 配置为向 `https://localhost:5001/api` 的请求添加令牌
- 令牌作为 `Authorization: Bearer {token}` 头添加

### 4. 用户信息

受保护页面显示来自ID令牌的用户信息：
- 姓名
- 邮箱
- 用户ID (Subject)
- 所有令牌声明

### 5. API调用

受保护页面包含多个按钮来调用示例API：
- **调用公开API**: 不需要认证的端点
- **调用受保护API**: 需要有效令牌的端点
- **获取天气预报**: 演示调用实际业务API
- 演示自动令牌注入
- 显示API响应和用户声明

## 认证状态

应用监控认证状态：
- 导航栏根据登录状态更新
- 适当显示登录/登出按钮
- 认证时显示用户名

## 令牌管理

`angular-auth-oidc-client` 库处理：
- 令牌存储（默认使用会话存储）
- 令牌过期前的静默续订
- 刷新令牌使用
- 登出时自动令牌清理

## 测试集成

### 完整测试流程

1. **启动IAM服务器** （确保运行在 `https://localhost:7001`）

2. **在IAM中注册客户端** （如上所述）

3. **启动此Angular应用**:
   ```bash
   npm start
   ```

4. **导航到** `http://localhost:4200`

5. **点击"登录"** 并使用以下凭据认证:
   - 用户名: `admin`
   - 密码: `MakeDotnetGreatAgain`

6. **访问受保护路由** 验证认证工作正常

7. **（可选）启动示例API** 并点击"Call Protected API"测试API集成:
   ```bash
   cd samples/backend-dotnet
   dotnet run
   ```

### 测试场景

1. **登录流程测试**:
   - 点击登录按钮
   - 验证重定向到IAM
   - 输入凭据
   - 验证重定向回应用
   - 确认显示用户信息

2. **受保护路由测试**:
   - 未登录时访问 `/protected`
   - 验证重定向到IAM登录
   - 登录后验证访问受保护页面

3. **API调用测试**:
   - 登录后访问受保护页面
   - 点击"调用公开API" - 应该成功
   - 点击"调用受保护API" - 应该返回用户信息
   - 点击"获取天气预报" - 应该返回天气数据

4. **登出测试**:
   - 点击登出按钮
   - 验证重定向到IAM
   - 验证返回应用时未认证状态

## 故障排除

### CORS错误

如果看到CORS错误：
- 确保客户端的"允许的CORS源"包括 `http://localhost:4200`
- 检查IAM的CORS策略允许Angular应用源

### 重定向URI不匹配

如果认证失败并显示重定向URI错误：
- 验证IAM客户端配置中的重定向URI完全匹配 `http://localhost:4200`
- 检查尾部斜杠

### 令牌未发送到API

如果API调用不包含令牌：
- 验证API URL匹配 `app.config.ts` 中的 `secureRoutes` 配置
- 检查浏览器控制台的拦截器错误

### SSL证书警告

对于使用自签名证书的开发环境：
- 首次访问IAM时接受证书警告
- 您可能需要直接访问 `https://localhost:7001` 来接受证书

### 无限重定向循环

如果遇到无限重定向：
- 清除浏览器存储
- 检查OIDC配置
- 验证IAM中的重定向URI配置

## 了解更多

- [angular-auth-oidc-client 文档](https://github.com/damienbod/angular-auth-oidc-client)
- [OpenID Connect 规范](https://openid.net/specs/openid-connect-core-1_0.html)
- [OAuth 2.0 授权码流程 + PKCE](https://oauth.net/2/pkce/)

## 与示例API集成

此Angular应用设计用于与 `samples/backend-dotnet` 中的示例API协同工作。

完整集成测试步骤：

1. 启动IAM服务器
2. 启动示例API (`cd samples/backend-dotnet && dotnet run`)
3. 启动此Angular应用 (`cd samples/frontend-angular && npm start`)
4. 登录并导航到受保护页面
5. 点击"Call Protected API"测试认证的API调用

示例API将使用IAM验证令牌，并返回受保护数据以及用户信息。

## 开发技巧

### 调试OIDC流程

在 `app.config.ts` 中启用调试日志：
```typescript
logLevel: LogLevel.Debug
```

### 测试不同用户

在IAM中创建额外的测试用户：
1. 导航到 用户 → 创建新用户
2. 分配适当的角色
3. 测试不同的授权场景

### 使用Postman测试API

1. 获取访问令牌：
   - 在Postman中使用OAuth 2.0
   - 授权URL: `https://localhost:7001/connect/authorize`
   - 令牌URL: `https://localhost:7001/connect/token`
   - 客户端ID: `FrontTest`
   - 作用域: 所需的作用域

2. 在请求中使用令牌：
   - 添加头: `Authorization: Bearer {access_token}`

## 生产部署注意事项

部署到生产环境时：

1. **更新配置**:
   - 使用生产IAM服务器URL
   - 配置正确的重定向URI
   - 将日志级别设置为 `LogLevel.None` 或 `LogLevel.Warn`

2. **安全考虑**:
   - 始终使用HTTPS
   - 验证重定向URI配置
   - 实施内容安全策略(CSP)
   - 启用子资源完整性(SRI)

3. **令牌存储**:
   - 默认使用会话存储
   - 考虑为刷新令牌使用HTTP-only cookie
   - 不要将令牌存储在本地存储中

4. **性能优化**:
   - 启用生产构建优化
   - 实施适当的缓存策略
   - 考虑使用CDN

## 贡献

欢迎贡献！如有问题或建议，请提交Issue。

## 其他资源

- [IAM项目文档](../../README.md)
- [客户端集成指南](../../docs/CLIENT-INTEGRATION-GUIDE.md)
- [示例项目总览](../README.md)
