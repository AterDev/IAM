# OAuth 2.0 / OIDC 核心实现

本文档描述了在 B4 任务中实现的 OAuth 2.0 和 OpenID Connect (OIDC) 核心功能。

## 实现概述

### 已实现的功能

1. **授权端点** (`/connect/authorize`)
   - 支持授权码流程 (Authorization Code Flow)
   - PKCE (Proof Key for Code Exchange) 验证
   - 支持 plain 和 S256 code challenge 方法
   - 作用域 (Scope) 验证
   - 重定向 URI 验证

2. **令牌端点** (`/connect/token`)
   - 授权码授权 (authorization_code)
   - 刷新令牌授权 (refresh_token)
   - 客户端凭证授权 (client_credentials)
   - 密码授权 (password)
   - 设备码授权 (device_code)

3. **设备授权端点** (`/connect/device`)
   - 设备码流程 (RFC 8628)
   - 用户代码生成 (8位字母数字，格式：XXXX-XXXX)
   - 设备码轮询支持

4. **令牌自省端点** (`/connect/introspect`)
   - RFC 7662 令牌自省
   - 返回令牌元数据（active, scope, client_id, exp 等）

5. **令牌撤销端点** (`/connect/revoke`)
   - RFC 7009 令牌撤销
   - 支持访问令牌和刷新令牌撤销

6. **登出端点** (`/connect/logout`)
   - OIDC 登出
   - 支持 post_logout_redirect_uri

## 架构设计

### Managers

#### AuthorizationManager
位置: `src/Modules/IdentityMod/Managers/AuthorizationManager.cs`

职责:
- 验证授权请求
- 创建授权码
- 验证授权码和 PKCE
- 管理授权记录

关键方法:
- `ValidateAuthorizationRequestAsync()` - 验证授权请求参数
- `CreateAuthorizationCodeAsync()` - 创建授权码
- `ValidateAuthorizationCodeAsync()` - 验证授权码和 PKCE

#### TokenManager
位置: `src/Modules/IdentityMod/Managers/TokenManager.cs`

职责:
- 处理令牌请求
- 生成访问令牌和刷新令牌
- 生成 ID 令牌 (OIDC)
- 令牌撤销和自省
- 客户端验证

关键方法:
- `ProcessTokenRequestAsync()` - 处理各类令牌请求
- `ProcessAuthorizationCodeGrantAsync()` - 处理授权码授权
- `ProcessRefreshTokenGrantAsync()` - 处理刷新令牌授权
- `ProcessClientCredentialsGrantAsync()` - 处理客户端凭证授权
- `ProcessPasswordGrantAsync()` - 处理密码授权
- `ProcessDeviceCodeGrantAsync()` - 处理设备码授权
- `RevokeTokenAsync()` - 撤销令牌
- `IntrospectTokenAsync()` - 令牌自省

#### DeviceFlowManager
位置: `src/Modules/IdentityMod/Managers/DeviceFlowManager.cs`

职责:
- 管理设备授权流程
- 生成设备码和用户码
- 处理用户授权/拒绝

关键方法:
- `InitiateDeviceAuthorizationAsync()` - 初始化设备授权
- `GetDeviceAuthorizationByUserCodeAsync()` - 通过用户码获取授权
- `ApproveDeviceAuthorizationAsync()` - 批准设备授权
- `DenyDeviceAuthorizationAsync()` - 拒绝设备授权

### DTOs

所有 DTO 位于 `src/Modules/IdentityMod/Models/OAuthDtos/`:

- `AuthorizeRequestDto` - 授权请求
- `AuthorizeResponseDto` - 授权响应
- `TokenRequestDto` - 令牌请求
- `TokenResponseDto` - 令牌响应
- `DeviceAuthorizationRequestDto` - 设备授权请求
- `DeviceAuthorizationResponseDto` - 设备授权响应
- `IntrospectRequestDto` - 自省请求
- `IntrospectResponseDto` - 自省响应
- `RevokeRequestDto` - 撤销请求
- `LogoutRequestDto` - 登出请求

### Controllers

#### OAuthController
位置: `src/Services/ApiService/Controllers/OAuthController.cs`

所有 OAuth/OIDC 端点的 HTTP 控制器，将请求委托给相应的 Manager。

## 安全特性

### PKCE 支持
- 支持 `plain` 和 `S256` 方法
- 对于公共客户端，可以要求 PKCE (通过 Client.RequirePkce)
- 符合 RFC 7636

### 客户端认证
- 支持客户端密钥验证
- 密钥使用密码哈希存储
- 区分公共客户端和机密客户端

### 令牌安全
- 访问令牌使用 JWT 格式
- 刷新令牌使用加密随机生成
- 所有令牌记录状态 (valid, redeemed, revoked)
- 令牌过期时间可配置

### 作用域验证
- 请求的作用域必须在客户端允许的作用域范围内
- 支持 OIDC 标准作用域 (openid, profile)

## 数据库实体

### Authorization
存储授权记录，包括:
- SubjectId - 用户 ID
- ClientId - 客户端 ID
- Type - 授权类型 (code, client_credentials, password, device_code)
- Status - 状态 (pending, valid, authorized, denied, revoked)
- Scopes - 授权的作用域
- CreationDate, ExpirationDate - 时间戳
- Properties - 附加属性 (JSON)

### Token
存储令牌记录，包括:
- AuthorizationId - 关联的授权 ID
- ReferenceId - 令牌引用 (用于刷新令牌、授权码等)
- Type - 令牌类型 (authorization_code, access_token, refresh_token, device_code, user_code)
- Status - 状态 (pending, valid, redeemed, revoked)
- SubjectId - 用户 ID
- Payload - 令牌内容 (JWT 或加密数据)
- CreationDate, ExpirationDate, RedemptionDate - 时间戳

## 使用示例

### 授权码流程

1. **授权请求**
```http
GET /connect/authorize?
    response_type=code
    &client_id=my_client
    &redirect_uri=https://client.example.com/callback
    &scope=openid profile
    &state=xyz
    &code_challenge=E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM
    &code_challenge_method=S256
```

2. **令牌请求**
```http
POST /connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=authorization_code
&code={authorization_code}
&redirect_uri=https://client.example.com/callback
&client_id=my_client
&code_verifier=dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk
```

### 客户端凭证流程

```http
POST /connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials
&client_id=my_client
&client_secret=my_secret
&scope=api.read
```

### 刷新令牌

```http
POST /connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=refresh_token
&refresh_token={refresh_token}
&client_id=my_client
```

### 设备码流程

1. **设备授权请求**
```http
POST /connect/device
Content-Type: application/x-www-form-urlencoded

client_id=my_device_client
&scope=openid profile
```

响应:
```json
{
  "device_code": "GmRhmhcxhwAzkoEqiMEg_DnyEysNkuNhszIySk9eS",
  "user_code": "WDJB-MJHT",
  "verification_uri": "https://localhost:5001/device",
  "verification_uri_complete": "https://localhost:5001/device?user_code=WDJB-MJHT",
  "expires_in": 600,
  "interval": 5
}
```

2. **设备令牌请求** (轮询)
```http
POST /connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=urn:ietf:params:oauth:grant-type:device_code
&device_code=GmRhmhcxhwAzkoEqiMEg_DnyEysNkuNhszIySk9eS
&client_id=my_device_client
```

### 令牌自省

```http
POST /connect/introspect
Content-Type: application/x-www-form-urlencoded

token={access_token}
&token_type_hint=access_token
```

### 令牌撤销

```http
POST /connect/revoke
Content-Type: application/x-www-form-urlencoded

token={token}
&token_type_hint=refresh_token
```

## 测试

### 单元测试

位于 `tests/OAuth/`:

1. **PkceTests.cs** - PKCE 验证逻辑测试
   - Plain 方法验证
   - S256 方法验证
   - 边界情况测试

2. **TokenGenerationTests.cs** - 令牌生成测试
   - 授权码生成
   - 令牌引用生成
   - 用户代码生成
   - 唯一性验证

### 集成测试

完整的集成测试需要:
- .NET 10 SDK
- 数据库连接
- 完整的应用程序上下文

集成测试应覆盖:
- 完整的授权码流程
- 刷新令牌流程
- 客户端凭证流程
- 设备码流程
- 错误场景处理

## 配置

### JWT 配置

在 `appsettings.json` 中配置:

```json
{
  "Jwt": {
    "Issuer": "IAM",
    "Audience": "IAM"
  }
}
```

### 依赖注入

Managers 在 `IdentityMod/ModuleExtensions.cs` 中注册:

```csharp
builder.Services.AddScoped<AuthorizationManager>();
builder.Services.AddScoped<TokenManager>();
builder.Services.AddScoped<DeviceFlowManager>();
```

## 限制和待办事项

### 当前限制

1. 隐式流程 (Implicit Flow) 未实现 - 不推荐使用
2. 混合流程 (Hybrid Flow) 未实现
3. 同意页面需要前端实现
4. 多因子认证钩子需要集成
5. 设备流程验证 URI 需要配置

### 待办事项

1. 实现同意管理
2. 添加多因子认证支持
3. 实现会话管理
4. 添加更多的审计日志
5. 实现 OIDC Discovery 端点
6. 添加 JWKS 端点
7. 实现用户信息端点 (/userinfo)

## 参考规范

- [RFC 6749 - OAuth 2.0](https://tools.ietf.org/html/rfc6749)
- [RFC 7636 - PKCE](https://tools.ietf.org/html/rfc7636)
- [RFC 7662 - Token Introspection](https://tools.ietf.org/html/rfc7662)
- [RFC 7009 - Token Revocation](https://tools.ietf.org/html/rfc7009)
- [RFC 8628 - Device Authorization Grant](https://tools.ietf.org/html/rfc8628)
- [OpenID Connect Core 1.0](https://openid.net/specs/openid-connect-core-1_0.html)
