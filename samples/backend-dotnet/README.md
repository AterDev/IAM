# Sample ASP.NET Core API with IAM Integration

这是一个ASP.NET Core Web API示例项目，展示如何对接IAM授权系统，实现OAuth 2.0/OIDC集成。

## 功能特性

- JWT Bearer认证
- 受保护的API端点
- 为Angular前端配置的CORS
- Swagger/OpenAPI文档支持JWT认证
- 用户声明提取和展示

## 配置说明

### IAM客户端配置

本示例使用IAM后台中已创建的客户端：
- **客户端ID**: `ApiTest`
- **客户端名称**: API测试客户端
- **用于**: 后端API资源服务器

### 应用配置

配置文件 `appsettings.json` 和 `appsettings.Development.json` 包含以下设置：

```json
{
  "Authentication": {
    "Authority": "https://localhost:7001",  // IAM服务器地址
    "Audience": "ApiTest",                  // API资源名称(必须匹配IAM中注册的客户端ID)
    "ClientId": "ApiTest",                  // 客户端ID
    "ValidateAudience": true,               // 是否验证audience
    "RequireHttpsMetadata": false           // 开发环境允许HTTP
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:4200",              // Angular开发服务器
      "https://localhost:4200"
    ]
  }
}
```

### IAM设置要求

在运行此示例之前，请确保在IAM管理后台中：

1. **客户端已注册**：
   - 客户端ID: `ApiTest`
   - 客户端类型: 资源服务器/API
   - 允许的作用域: 根据需要配置

2. **用户已创建**：
   - 至少有一个测试用户用于获取访问令牌
   - 默认管理员账号: `admin` / `MakeDotnetGreatAgain`

## 运行应用

### 前置要求

- .NET 9.0 SDK 或更高版本
- IAM服务器运行在 `https://localhost:7001`

### 启动步骤

1. 确保IAM服务器正在运行
2. 导航到示例目录：
   ```bash
   cd samples/backend-dotnet
   ```

3. 运行应用：
   ```bash
   dotnet run
   ```

4. 访问Swagger UI: `https://localhost:5001/swagger`

## API端点

### 公开端点（无需认证）

- `GET /api/public` - 可公开访问的端点，返回欢迎消息

**示例请求：**
```bash
curl https://localhost:5001/api/public
```

**响应：**
```json
{
  "message": "这是一个公开端点，无需认证",
  "timestamp": "2024-01-10T10:00:00Z"
}
```

### 受保护端点（需要认证）

#### 1. 基本受保护端点

- `GET /api/protected` - 需要有效JWT令牌，返回用户信息

**示例请求：**
```bash
curl -H "Authorization: Bearer YOUR_ACCESS_TOKEN" https://localhost:5001/api/protected
```

**响应：**
```json
{
  "message": "这是一个受保护端点，需要有效的JWT令牌",
  "user": {
    "isAuthenticated": true,
    "name": "admin",
    "subject": "user-id",
    "email": "admin@iam.local",
    "claims": [...]
  },
  "timestamp": "2024-01-10T10:00:00Z"
}
```

#### 2. 天气预报端点

- `GET /api/weatherforecast` - 获取5天天气预报（需要认证）
- `GET /api/weatherforecast/forecast/{days}` - 获取指定天数的天气预报（需要认证）

**示例请求：**
```bash
curl -H "Authorization: Bearer YOUR_ACCESS_TOKEN" https://localhost:5001/api/weatherforecast
```

## 获取访问令牌

### 方式1: 通过前端示例应用

1. 启动Angular前端示例 (`samples/frontend-angular`)
2. 登录获取访问令牌
3. 使用该令牌调用API

### 方式2: 使用Swagger UI

1. 访问 `https://localhost:5001/swagger`
2. 点击右上角的 "Authorize" 按钮
3. 在弹出的对话框中输入访问令牌: `Bearer {your_token}`
4. 点击 "Authorize"
5. 现在可以直接在Swagger UI中测试受保护的端点

### 方式3: 使用IAM令牌端点（客户端凭证流程）

如果您有客户端密钥，可以直接从IAM获取令牌：

```bash
curl -X POST https://localhost:7001/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials" \
  -d "client_id=ApiTest" \
  -d "client_secret=YOUR_CLIENT_SECRET" \
  -d "scope=openid profile"
```

### 方式4: 使用密码流程（仅用于测试）

```bash
curl -X POST https://localhost:7001/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password" \
  -d "client_id=ApiTest" \
  -d "username=admin" \
  -d "password=MakeDotnetGreatAgain" \
  -d "scope=openid profile email"
```

## 与Angular前端集成

此API已配置为与Angular示例前端 (`samples/frontend-angular`) 协同工作。

CORS策略允许来自 `http://localhost:4200` （Angular开发服务器）的请求。

### 集成测试步骤：

1. 启动IAM服务器 (`https://localhost:7001`)
2. 启动此API (`https://localhost:5001`)
3. 启动Angular前端 (`http://localhost:4200`)
4. 在前端登录
5. 在前端的"Protected"页面点击"Call Protected API"按钮
6. 验证API返回用户信息和天气数据

## 认证流程

```
┌─────────────┐          ┌─────────────┐          ┌─────────────┐
│   客户端    │          │  IAM服务器   │          │  此API服务   │
│ (Angular)   │          │  认证服务器  │          │  资源服务器  │
└──────┬──────┘          └──────┬──────┘          └──────┬──────┘
       │                        │                        │
       │ 1. 用户发起登录         │                        │
       ├───────────────────────>│                        │
       │                        │                        │
       │ 2. 用户认证并授权       │                        │
       │<───────────────────────┤                        │
       │                        │                        │
       │ 3. 获取访问令牌         │                        │
       │<───────────────────────┤                        │
       │                        │                        │
       │ 4. 携带令牌访问API      │                        │
       ├────────────────────────┼───────────────────────>│
       │                        │                        │
       │                        │ 5. 验证令牌             │
       │                        │<───────────────────────┤
       │                        │ (通过JWKS端点验证签名)  │
       │                        │                        │
       │ 6. 返回受保护资源       │                        │
       │<───────────────────────┼────────────────────────┤
```

## 令牌验证过程

API使用以下参数验证JWT令牌：

1. **Issuer验证**: 确认令牌由IAM服务器签发
2. **Audience验证**: 确认令牌是为此API签发的
3. **Lifetime验证**: 确认令牌未过期
4. **Signature验证**: 使用IAM的公钥验证令牌签名
5. **Clock Skew**: 允许5分钟的时钟偏移

## 故障排除

### 问题1: CORS错误

**错误**: `Access to XMLHttpRequest has been blocked by CORS policy`

**解决方案**:
- 检查Angular应用的URL是否在 `appsettings.json` 的 `Cors:AllowedOrigins` 中
- 确保CORS中间件在认证中间件之前注册

### 问题2: 令牌验证失败

**错误**: `401 Unauthorized`

**解决方案**:
- 验证 `Authority` 配置是否正确指向IAM服务器
- 验证 `Audience` 是否匹配IAM中注册的客户端ID
- 确认令牌未过期
- 检查IAM服务器的JWKS端点是否可访问: `https://localhost:7001/.well-known/jwks`

### 问题3: 无法访问Swagger

**错误**: `Unable to connect`

**解决方案**:
- 检查应用是否正在运行
- 验证端口5001是否被占用
- 尝试使用HTTP而非HTTPS: `http://localhost:5000/swagger`

### 问题4: 开发环境HTTPS证书警告

**解决方案**:
```bash
# 信任开发证书
dotnet dev-certs https --trust
```

## 生产环境部署注意事项

在生产环境部署时：

1. **启用HTTPS元数据验证**:
   ```json
   "RequireHttpsMetadata": true
   ```

2. **配置正确的CORS源**:
   - 不要使用通配符 `*`
   - 只允许可信的前端应用域名

3. **使用环境变量存储敏感配置**:
   ```bash
   export Authentication__Authority="https://your-iam-server"
   export Authentication__Audience="your-api-name"
   ```

4. **启用HTTPS**:
   - 配置有效的SSL证书
   - 强制HTTPS重定向

5. **实施速率限制和其他安全措施**

## 其他资源

- [ASP.NET Core认证文档](https://learn.microsoft.com/zh-cn/aspnet/core/security/authentication/)
- [JWT Bearer认证](https://learn.microsoft.com/zh-cn/aspnet/core/security/authentication/jwt-authn)
- [OAuth 2.0和OpenID Connect](https://openid.net/developers/how-connect-works/)
- [IAM项目文档](../../README.md)
