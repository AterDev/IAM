# IAM 集成示例

本目录包含演示如何对接IAM（身份与访问管理）系统的示例项目。

所有应用现在通过 .NET Aspire 统一启动和管理，简化了开发流程。

## 服务端口配置

所有服务使用以下固定端口：

| 服务 | 端口 | 说明 |
|------|------|------|
| IAM 后端 API | 7070 | 身份认证和授权服务 |
| IAM 前端管理平台 | 4200 | 管理后台界面 |
| 示例后端 API | 7000 | 示例API服务 |
| 示例前端应用 | 4201 | 示例前端界面 |

## 可用示例

### 1. 后端示例 (ASP.NET Core)
位置: `backend-dotnet/`

ASP.NET Core Web API示例，演示：
- JWT Bearer认证
- 使用IAM进行令牌验证
- 受保护的API端点
- 为SPA配置CORS
- Swagger UI集成，支持JWT测试

**服务端口**: `https://localhost:7000`  
**使用的客户端**: `ApiClient`（自动创建的默认客户端）

[查看后端示例文档](backend-dotnet/README.md)

### 2. 前端示例 (Angular)
位置: `frontend-angular/`

Angular应用示例，演示：
- OAuth 2.0 / OpenID Connect认证
- 授权码流程 + PKCE
- 自动令牌管理
- 受保护路由
- 调用API的HTTP拦截器

**服务端口**: `http://localhost:4201`  
**使用的客户端**: `FrontClient`（自动创建的默认客户端）

[查看前端示例文档](frontend-angular/README.md)

## 快速开始指南

### 前置要求

1. **开发工具**
   - .NET 9 SDK
   - Node.js 20+ 和 pnpm
   - Docker（用于运行PostgreSQL和Redis容器）

### 步骤 1: 使用 Aspire 启动所有服务

从项目根目录运行：

```bash
cd src/AppHost
dotnet run
```

这将自动启动：
- PostgreSQL 数据库
- Redis 缓存
- IAM 数据库迁移服务
- IAM API 服务 (https://localhost:7070)
- IAM 管理前端 (http://localhost:4200)
- 示例 API 服务 (https://localhost:7000)
- 示例前端应用 (http://localhost:4201)

Aspire Dashboard 将在浏览器中自动打开，您可以在其中监控所有服务的状态。

### 步骤 2: 访问管理后台配置（可选）

系统已自动创建默认配置，但您可以登录管理后台查看：

1. 访问 IAM 管理后台: `http://localhost:4200`
2. 使用默认管理员凭据登录: 
   - 用户名: `admin`
   - 密码: `MakeDotnetGreatAgain`

### 步骤 3: 默认客户端和作用域

系统已自动创建以下默认配置：

#### 默认作用域 (Scopes)
- `openid` - OpenID Connect身份认证（必需）
- `profile` - 用户基本信息
- `email` - 用户邮箱地址
- `address` - 用户地址信息
- `phone` - 用户电话号码
- `offline_access` - 离线访问权限（刷新令牌）

#### FrontClient 客户端（前端应用）
- **客户端ID**: `FrontClient`
- **客户端类型**: 公共客户端（SPA）
- **授权流程**: 授权码 + PKCE
- **允许的作用域**: openid, profile, email, offline_access
- **重定向URI**: 
  - `http://localhost:4200`
  - `https://localhost:4200`
  - `http://localhost:4201`
  - `https://localhost:4201`

#### ApiClient 客户端（后端API）
- **客户端ID**: `ApiClient`
- **客户端密钥**: `ApiClient_Secret_2025`
- **客户端类型**: 机密客户端
- **授权流程**: 客户端凭证流程
- **允许的作用域**: openid

### 步骤 4: 测试集成

1. 在浏览器中打开示例前端: `http://localhost:4201`
2. 点击"登录"按钮
3. 您将被重定向到IAM登录页面 (端口 7070)
4. 输入凭据: `admin` / `MakeDotnetGreatAgain`
5. 成功登录后，您将被重定向回示例应用
6. 导航到"受保护页面"查看用户信息
7. 点击"调用受保护API"测试与后端API (端口 7000) 的集成

## 架构概览

```
┌─────────────────────┐       ┌─────────────────────┐
│  IAM管理前端        │       │  示例前端应用       │
│  (端口 4200)        │       │  (端口 4201)        │
│                     │       │                     │
│  - 管理界面         │       │  - OIDC客户端       │
│  - 配置管理         │       │  - UI/UX            │
└──────────┬──────────┘       └──────────┬──────────┘
           │                             │
           │ 管理API调用                 │ 1. 认证请求
           │                             │ 3. 令牌请求
           ▼                             ▼
┌──────────────────────────────────────────────────┐
│              IAM 服务器 (端口 7070)               │
│                                                   │
│  - OAuth/OIDC认证服务器                          │
│  - 用户存储和管理                                │
│  - 令牌颁发和验证                                │
│  - 作用域和权限管理                              │
└────────────────────┬─────────────────────────────┘
                     │
                     │ 2. 用户登录
                     │ 4. 访问令牌
                     ▼
           ┌─────────────────┐
           │  示例API服务器  │
           │  (端口 7000)    │
           │                 │
           │  - JWT令牌验证  │
           │  - 资源保护     │
           │  - 业务逻辑     │
           └─────────────────┘
```

## OAuth 2.0 / OIDC 认证授权流程

### 授权码流程 + PKCE (用于前端SPA)

1. **用户发起登录**
   - 用户在示例前端 (4201) 点击登录按钮
   
2. **生成PKCE挑战**
   - 前端生成 code_verifier (随机字符串)
   - 计算 code_challenge = BASE64URL(SHA256(code_verifier))
   
3. **重定向到授权端点**
   - 前端重定向到: `https://localhost:7070/connect/authorize`
   - 参数包括: client_id, redirect_uri, scope, response_type=code, code_challenge
   
4. **用户认证**
   - IAM显示登录页面
   - 用户输入用户名和密码
   - IAM验证凭据
   
5. **授权确认（可选）**
   - 如果配置需要，显示授权确认页面
   - 用户同意授予权限
   
6. **返回授权码**
   - IAM重定向回: `http://localhost:4201?code=xxx`
   - 授权码是一次性使用的临时凭证
   
7. **交换令牌**
   - 前端发送POST请求到: `https://localhost:7070/connect/token`
   - 包含: code, client_id, redirect_uri, code_verifier
   - IAM验证授权码和PKCE挑战
   
8. **获取令牌**
   - IAM返回:
     - access_token (访问令牌)
     - id_token (ID令牌，包含用户信息)
     - refresh_token (刷新令牌)
   
9. **调用受保护API**
   - 前端在HTTP请求头中添加: `Authorization: Bearer {access_token}`
   - 调用: `https://localhost:7000/api/protected`
   
10. **API验证令牌**
    - 示例API从IAM的JWKS端点获取公钥
    - 验证JWT签名、过期时间、颁发者等
    - 验证通过后返回受保护资源
    
11. **令牌刷新**
    - 当access_token过期时
    - 前端使用refresh_token请求新的access_token
    - 无需用户重新登录

### 客户端凭证流程 (用于后端服务)

1. **服务认证**
   - 后端服务使用client_id和client_secret
   - 发送POST请求到: `https://localhost:7070/connect/token`
   - grant_type=client_credentials
   
2. **获取令牌**
   - IAM验证客户端凭证
   - 返回access_token
   
3. **服务间调用**
   - 使用access_token调用其他受保护的API

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

### 使用 Aspire Dashboard 监控

Aspire Dashboard 提供了所有服务的实时监控：
- 查看服务状态和日志
- 监控HTTP请求和响应
- 查看数据库和缓存连接
- 跟踪分布式追踪信息

### 调试OIDC流程

在Angular应用中启用调试日志：
```typescript
// 在 app.config.ts 中
logLevel: LogLevel.Debug
```

在浏览器开发者工具中查看：
- Network标签：查看OAuth请求和响应
- Application/Storage标签：查看存储的令牌
- Console标签：查看OIDC客户端日志

### 使用不同用户测试

在IAM管理后台创建额外的测试用户：
1. 访问 `http://localhost:4200`
2. 导航到 用户 → 创建新用户
3. 分配适当的角色
4. 测试不同的授权场景

### 使用Postman测试API

1. 获取访问令牌：
   - 在Postman中使用OAuth 2.0
   - 授权URL: `https://localhost:7070/connect/authorize`
   - 令牌URL: `https://localhost:7070/connect/token`
   - 客户端ID: `FrontClient`
   - 作用域: `openid profile email`

2. 在请求中使用令牌：
   - 添加头: `Authorization: Bearer {access_token}`

### 直接获取令牌（用于测试）

使用密码流程直接获取令牌：

```bash
curl -X POST https://localhost:7070/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password" \
  -d "client_id=FrontClient" \
  -d "username=admin" \
  -d "password=MakeDotnetGreatAgain" \
  -d "scope=openid profile email"
```

## 常见问题和解决方案

### 问题: 服务无法启动
**解决方案**: 
- 确保Docker正在运行
- 检查端口是否被占用 (7070, 7000, 4200, 4201)
- 查看Aspire Dashboard中的服务日志

### 问题: 数据库连接失败
**解决方案**: 
- 确保PostgreSQL容器已启动
- 在Aspire Dashboard中检查数据库服务状态
- 等待迁移服务完成

### 问题: CORS错误
**解决方案**: 
- 确保后端API的CORS配置包含前端URL
- 检查端口号是否正确 (示例前端应该是4201)

### 问题: 重定向URI不匹配
**解决方案**: 
- 验证使用的端口号正确 (IAM: 7070, 示例前端: 4201)
- 检查是否使用了正确的协议 (http vs https)
- 默认配置已包含常用的重定向URI

### 问题: 令牌验证失败
**解决方案**: 
- 确认示例API的Authority配置为 `https://localhost:7070`
- 检查IAM的JWKS端点是否可访问: `https://localhost:7070/.well-known/jwks`
- 验证令牌未过期

### 问题: 无限重定向循环
**解决方案**: 
- 清除浏览器存储 (localStorage和sessionStorage)
- 检查OIDC配置中的redirectUrl和postLogoutRedirectUri
- 验证客户端ID和作用域配置正确

### 问题: SSL证书错误
**解决方案**: 
- 对于开发环境，在浏览器中接受自签名证书
- 访问 `https://localhost:7070` 和 `https://localhost:7000` 并接受证书警告
- 或配置开发证书信任

### 问题: 后端API无法验证令牌
**解决方案**: 
- 确认Audience配置与预期一致
- 检查Authority配置指向正确的IAM服务器: `https://localhost:7070`
- 验证IAM的discovery端点可访问: `https://localhost:7070/.well-known/openid-configuration`

## 配置检查清单

### Aspire 运行环境
- [ ] Docker Desktop 正在运行
- [ ] .NET 9 SDK 已安装
- [ ] Node.js 20+ 和 pnpm 已安装
- [ ] 端口 7070, 7000, 4200, 4201 未被占用

### IAM 服务配置（自动完成）
- [x] PostgreSQL 数据库容器已启动
- [x] Redis 缓存容器已启动
- [x] 数据库迁移已完成
- [x] 管理员账号已创建 (admin / MakeDotnetGreatAgain)
- [x] 默认作用域已创建 (openid, profile, email等)
- [x] FrontClient 客户端已创建
- [x] ApiClient 客户端已创建
- [x] IAM API 服务运行在 https://localhost:7070
- [x] IAM 管理前端运行在 http://localhost:4200

### 示例项目配置（自动完成）
- [x] 示例 API 服务运行在 https://localhost:7000
- [x] 示例前端应用运行在 http://localhost:4201
- [x] 示例 API 的 Authority 指向 https://localhost:7070
- [x] 示例前端的 authority 指向 https://localhost:7070
- [x] 示例前端的 secureRoutes 指向 https://localhost:7000
- [x] CORS 配置正确

### 验证步骤
- [ ] 访问 IAM 管理后台并成功登录
- [ ] 在应用管理中查看 FrontClient 和 ApiClient
- [ ] 访问示例前端应用
- [ ] 在示例应用中完成登录流程
- [ ] 成功调用示例 API 的受保护端点

## 额外资源

- [IAM项目文档](../../README.md)
- [OAuth 2.0 RFC](https://tools.ietf.org/html/rfc6749)
- [OpenID Connect规范](https://openid.net/specs/openid-connect-core-1_0.html)
- [PKCE RFC](https://tools.ietf.org/html/rfc7636)
- [.NET Aspire文档](https://learn.microsoft.com/dotnet/aspire/)

## 支持

如有问题或疑问：
1. 查看各示例的README获取具体指导
2. 查看主IAM文档
3. 在GitHub上查看问题跟踪器

## 贡献

欢迎改进示例项目！如有建议或发现问题，请提交Issue或Pull Request。
