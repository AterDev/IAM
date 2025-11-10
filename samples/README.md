# IAM 集成示例

本目录包含演示如何对接IAM（身份与访问管理）系统的示例项目。

## 可用示例

### 1. 后端示例 (ASP.NET Core)
位置: `backend-dotnet/`

ASP.NET Core Web API示例，演示：
- JWT Bearer认证
- 使用IAM进行令牌验证
- 受保护的API端点
- 为SPA配置CORS
- Swagger UI集成，支持JWT测试

**使用的客户端**: `ApiTest`（已在IAM后台创建）

[查看后端示例文档](backend-dotnet/README.md)

### 2. 前端示例 (Angular)
位置: `frontend-angular/`

Angular应用示例，演示：
- OAuth 2.0 / OpenID Connect认证
- 授权码流程 + PKCE
- 自动令牌管理
- 受保护路由
- 调用API的HTTP拦截器

**使用的客户端**: `FrontTest`（已在IAM后台创建）

[查看前端示例文档](frontend-angular/README.md)

## 快速开始指南

### 前置要求

1. **IAM服务器运行中**
   - 默认URL: `https://localhost:7001`
   - 管理员凭据: `admin` / `MakeDotnetGreatAgain`

2. **开发工具**
   - .NET 9 SDK（用于后端示例）
   - Node.js 20+ 和 npm（用于前端示例）

### 步骤 1: 验证IAM配置

IAM后台已经配置了两个客户端用于示例项目：

#### ApiTest 客户端（后端API）
- **客户端ID**: `ApiTest`
- **用途**: 后端API资源服务器
- **类型**: 资源服务器/API
- **验证**: 登录IAM管理后台，导航到"应用管理"，确认客户端存在

#### FrontTest 客户端（前端应用）
- **客户端ID**: `FrontTest`  
- **用途**: 前端单页应用
- **类型**: 公共客户端/SPA
- **授权类型**: 授权码 + PKCE
- **重定向URI**: 应包含 `http://localhost:4200`
- **验证**: 登录IAM管理后台，导航到"应用管理"，确认客户端存在并配置正确

如果客户端不存在，请参考各示例的README中的配置说明进行创建。

### 步骤 2: 运行后端示例

```bash
cd samples/backend-dotnet
dotnet run
```

API将在 `https://localhost:5001` 启动

**测试端点:**
- 公开端点: `GET https://localhost:5001/api/public`
- 受保护端点: `GET https://localhost:5001/api/protected` （需要认证）
- Swagger UI: `https://localhost:5001/swagger`

### 步骤 3: 运行前端示例

```bash
cd samples/frontend-angular
npm install
npm start
```

应用将在 `http://localhost:4200` 启动

### 步骤 4: 测试集成

1. 在浏览器中打开 `http://localhost:4200`
2. 点击"登录"按钮
3. 您将被重定向到IAM登录页面
4. 输入凭据: `admin` / `MakeDotnetGreatAgain`
5. 成功登录后，您将被重定向回Angular应用
6. 导航到"受保护页面"查看用户信息
7. 点击"调用受保护API"测试API集成

## 架构概览

```
┌─────────────────┐
│  Angular应用    │
│  (端口 4200)    │
│                 │
│  - OIDC客户端   │
│  - UI/UX        │
└────────┬────────┘
         │
         │ 1. 认证请求
         │ 3. 令牌请求
         ▼
┌─────────────────┐
│   IAM服务器     │
│  (端口 7001)    │
│                 │
│  - 认证服务器   │
│  - 用户存储     │
│  - 令牌颁发     │
└────────┬────────┘
         │
         │ 2. 用户登录
         │
         │
         │ 4. 访问令牌
         ▼
┌─────────────────┐
│  API服务器      │
│  (端口 5001)    │
│                 │
│  - 令牌验证     │
│  - 资源保护     │
└─────────────────┘
```

## 认证流程

1. **用户发起登录** - 从Angular应用开始
2. **Angular重定向** - 到IAM授权端点
3. **用户认证** - 在IAM登录页面
4. **IAM重定向回** - Angular应用，携带授权码
5. **Angular交换令牌** - 使用授权码换取访问令牌、ID令牌和刷新令牌
6. **Angular存储令牌** - 安全存储
7. **API调用包含令牌** - 在Authorization头中
8. **API验证令牌** - 使用IAM的公钥验证
9. **API返回资源** - 受保护的数据

## 令牌类型

### 访问令牌 (Access Token)
- 用于访问受保护的API资源
- 短期有效（通常1小时）
- 由资源服务器验证
- 格式: JWT

### ID令牌 (ID Token)
- 包含用户身份信息
- 由客户端应用使用
- 不发送到API
- 格式: JWT

### 刷新令牌 (Refresh Token)
- 用于获取新的访问令牌
- 长期有效（通常30天）
- 实现静默认证续订
- 不透明令牌

## 安全考虑

### 生产部署

部署到生产环境时：

1. **全程使用HTTPS**
   - IAM服务器必须使用HTTPS
   - API服务器必须使用HTTPS
   - 配置有效的SSL证书

2. **安全令牌存储**
   - Angular: 默认使用会话存储
   - 考虑为刷新令牌使用HTTP-only cookie

3. **正确配置CORS**
   - 仅允许特定源
   - 生产环境不要使用通配符(*)

4. **令牌生命周期**
   - 保持访问令牌短期有效（5-60分钟）
   - 对长会话使用刷新令牌
   - 实施适当的令牌撤销

5. **客户端密钥**
   - 公共客户端（SPA）不应使用客户端密钥
   - 后端客户端应使用强客户端密钥
   - 安全存储密钥（环境变量、密钥保管库）

6. **验证所有令牌**
   - 验证令牌签名
   - 检查过期时间
   - 验证颁发者和受众
   - 检查必需的声明

## 开发技巧

### 调试OIDC流程

在Angular应用中启用调试日志：
```typescript
// 在 app.config.ts 中
logLevel: LogLevel.Debug
```

### 使用不同用户测试

在IAM中创建额外的测试用户：
1. 导航到 用户 → 创建新用户
2. 分配适当的角色
3. 测试不同的授权场景

### 使用Postman测试API

1. 获取访问令牌：
   - 在Postman中使用OAuth 2.0
   - 授权URL: `https://localhost:7001/connect/authorize`
   - 令牌URL: `https://localhost:7001/connect/token`
   - 客户端ID: 您的客户端ID
   - 作用域: 所需的作用域

2. 在请求中使用令牌：
   - 添加头: `Authorization: Bearer {access_token}`

### 直接获取令牌（用于测试）

使用密码流程直接获取令牌：

```bash
curl -X POST https://localhost:7001/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password" \
  -d "client_id=FrontTest" \
  -d "username=admin" \
  -d "password=MakeDotnetGreatAgain" \
  -d "scope=openid profile email ApiTest"
```

## 常见问题和解决方案

### 问题: CORS错误
**解决方案**: 确保客户端的允许CORS源包括Angular应用URL

### 问题: 重定向URI不匹配
**解决方案**: 验证IAM中的重定向URI与应用URL完全匹配（检查尾部斜杠）

### 问题: 令牌验证失败
**解决方案**: 检查API的authority配置是否匹配IAM的颁发者URL

### 问题: 无限重定向循环
**解决方案**: 清除浏览器存储并检查OIDC配置

### 问题: SSL证书错误
**解决方案**: 对于开发环境，接受自签名证书或配置适当的证书

### 问题: 后端API无法验证令牌
**解决方案**: 
- 确认Audience配置与客户端ID匹配
- 检查IAM的JWKS端点是否可访问
- 验证令牌未过期

## 配置检查清单

### IAM服务器配置
- [ ] IAM服务器正在运行
- [ ] 管理员账号可以登录
- [ ] ApiTest客户端已创建
- [ ] FrontTest客户端已创建
- [ ] FrontTest的重定向URI配置正确
- [ ] 作用域配置正确

### 后端示例配置
- [ ] .NET SDK已安装
- [ ] appsettings.json中的Authority配置正确
- [ ] Audience设置为"ApiTest"
- [ ] CORS允许的源包括前端URL

### 前端示例配置
- [ ] Node.js和npm已安装
- [ ] app.config.ts中的authority配置正确
- [ ] clientId设置为"FrontTest"
- [ ] scope包括所需的作用域
- [ ] secureRoutes配置指向API URL

## 额外资源

- [IAM项目文档](../../README.md)
- [OAuth 2.0 RFC](https://tools.ietf.org/html/rfc6749)
- [OpenID Connect规范](https://openid.net/specs/openid-connect-core-1_0.html)
- [PKCE RFC](https://tools.ietf.org/html/rfc7636)
- [客户端集成指南](../../docs/CLIENT-INTEGRATION-GUIDE.md)

## 支持

如有问题或疑问：
1. 查看各示例的README获取具体指导
2. 查看主IAM文档
3. 在GitHub上查看问题跟踪器

## 贡献

欢迎改进示例项目！如有建议或发现问题，请提交Issue或Pull Request。
