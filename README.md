# IAM 身份与访问管理系统

基于 .NET 和 Angular 的开箱即用的身份认证与授权解决方案，实现 OAuth 2.0 和 OpenID Connect (OIDC) 标准协议。

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/)
[![Angular](https://img.shields.io/badge/Angular-20-red.svg)](https://angular.dev/)

## 🌟 核心特性

### OAuth 2.0 / OpenID Connect
- ✅ **完整的OAuth 2.0流程**
  - 授权码流程（Authorization Code）+ PKCE
  - 客户端凭证流程（Client Credentials）
  - 密码流程（Resource Owner Password）
  - 刷新令牌流程（Refresh Token）
  - 设备授权流程（Device Code）

- ✅ **OIDC标准端点**
  - Discovery文档 (`/.well-known/openid-configuration`)
  - JWKS公钥端点 (`/.well-known/jwks`)
  - UserInfo端点 (`/connect/userinfo`)
  - 授权、令牌、撤销、自省、登出端点

### 身份与访问管理
- ✅ **用户管理** - 用户注册、认证、状态管理
- ✅ **角色管理** - 基于角色的访问控制（RBAC）
- ✅ **组织管理** - 多层级组织架构
- ✅ **应用管理** - OAuth客户端注册与配置
- ✅ **作用域管理** - API权限和资源定义
- ✅ **会话管理** - 活跃会话监控与强制登出
- ✅ **审计日志** - 完整的操作审计追踪

### 安全特性
- ✅ JWT访问令牌（RS256签名）
- ✅ PKCE防止授权码拦截
- ✅ 客户端密钥哈希存储
- ✅ 令牌自省与撤销
- ✅ 签名密钥轮换支持
- ✅ 防重放攻击保护

### 管理门户
- ✅ 现代化Web管理界面（Angular 20 + Material Design）
- ✅ 用户、角色、组织可视化管理
- ✅ OAuth应用配置界面
- ✅ 会话监控与审计日志查看
- ✅ 中英文双语支持
- ✅ 响应式设计

## 📚 文档

- [IAM解决方案设计文档](docs/IAM解决方案设计文档.md)
- [IAM开发任务规划](docs/tasks/iam-development-plan.md)
- [开发指南](docs/DEVELOPMENT-GUIDE.md) - 详细的开发规范和约定
- [未实现功能分析](docs/MISSING-FEATURES-ANALYSIS.md)
- [OAuth实现文档](docs/oauth-implementation.md)
- [OAuth安全分析](docs/oauth-security-analysis.md)
- [API文档](docs/api-documentation.md)
- [集成测试文档](docs/integration-testing.md)
- [快速开始指南](docs/quick-start.md)

### 用户文档
- [用户操作手册](src/ClientApp/WebApp/docs/USER-MANUAL.md)
- [管理员操作手册](src/ClientApp/WebApp/docs/ADMIN-MANUAL.md)
- [部署指南](src/ClientApp/WebApp/docs/DEPLOYMENT-GUIDE.md)
- [测试指南](src/ClientApp/WebApp/docs/TESTING-GUIDE.md)

## 🚀 快速开始

### 环境要求
- .NET 9.0 SDK
- Node.js 20+ / pnpm 9+
- Docker Desktop（用于数据库和缓存容器）

### 使用 .NET Aspire 一键启动（推荐）

.NET Aspire 可以自动编排和管理所有服务，包括数据库、缓存、API服务和前端应用。

```bash
# 克隆仓库
git clone https://github.com/AterDev/IAM.git
cd IAM

# 启动所有服务
cd src/AppHost
dotnet run
```

这将自动启动：
- **PostgreSQL** 数据库容器 (端口 15432)
- **Redis** 缓存容器 (端口 16379)
- **数据库迁移服务** - 自动执行数据库迁移和数据种子
- **IAM API 服务** - `https://localhost:7070`
- **IAM 管理前端** - `http://localhost:4200`
- **示例 API 服务** - `https://localhost:7000`
- **示例前端应用** - `http://localhost:4201`

Aspire Dashboard 将自动在浏览器中打开，提供：
- 所有服务的实时状态监控
- 日志查看和搜索
- 分布式追踪
- 指标和性能监控

**服务端口说明：**
| 服务 | 端口 | 说明 |
|------|------|------|
| IAM API | 7070 | 身份认证和授权服务 |
| IAM 管理前端 | 4200 | 管理后台界面 |
| 示例 API | 7000 | 示例后端服务 |
| 示例前端 | 4201 | 示例前端应用 |

**默认配置已自动创建：**
- 管理员账号: `admin` / `MakeDotnetGreatAgain`
- OAuth作用域: openid, profile, email, address, phone, offline_access
- 前端客户端: `FrontClient` (支持授权码+PKCE流程)
- API客户端: `ApiClient` (支持客户端凭证流程)

### 手动启动（高级用户）

如果需要单独启动各个服务：

#### 后端启动

```bash
# 确保 PostgreSQL 和 Redis 正在运行

# 运行数据库迁移
cd src/Services/MigrationService
dotnet run

# 启动API服务
cd ../ApiService
dotnet run
```

API将在 `https://localhost:7070` 启动

#### 前端启动

```bash
cd src/ClientApp/WebApp

# 安装依赖
pnpm install

# 启动开发服务器
pnpm start
```

管理门户将在 `http://localhost:4200` 启动

### Docker部署

```bash
# 构建镜像
docker-compose build

# 启动服务
docker-compose up -d
```

## 📁 项目结构

```
IAM/
├── src/
│   ├── AppHost/                  # .NET Aspire 应用编排
│   ├── Ater/                    # 基础类库
│   │   ├── Ater.Common/         # 通用帮助类
│   │   ├── Ater.Web.Convention/ # Web约定
│   │   └── Ater.Web.Extension/  # Web扩展
│   ├── Definition/              # 定义层
│   │   ├── Entity/              # 实体模型
│   │   ├── EntityFramework/     # EF Core上下文
│   │   ├── ServiceDefaults/     # 服务默认配置
│   │   └── Share/               # 共享服务
│   ├── Modules/                 # 业务模块
│   │   ├── CommonMod/           # 公共模块
│   │   ├── IdentityMod/         # 身份认证模块
│   │   └── AccessMod/           # 访问控制模块
│   ├── Services/                # 服务层
│   │   ├── ApiService/          # API服务
│   │   └── MigrationService/    # 数据库迁移服务
│   └── ClientApp/               # 前端应用
│       └── WebApp/              # Angular管理门户
├── tests/                       # 测试项目
│   └── Integration/             # 集成测试
├── samples/                     # 示例项目
│   ├── backend-dotnet/          # .NET后端集成示例
│   └── frontend-angular/        # Angular前端集成示例
├── docs/                        # 文档
└── scripts/                     # 脚本工具
```

### 核心模块说明

#### IdentityMod（身份认证模块）
- **Managers**: AuthorizationManager, TokenManager, DeviceFlowManager, DiscoveryManager
- **功能**: OAuth/OIDC流程实现、令牌管理、用户认证

#### AccessMod（访问控制模块）
- **Managers**: ClientManager, ScopeManager, ResourceManager
- **功能**: 客户端管理、作用域配置、API资源定义

#### CommonMod（公共模块）
- **Managers**: AuditLogManager, SystemSettingManager
- **功能**: 审计日志、系统配置、密钥管理

## 🔧 技术栈

### 后端
- **框架**: ASP.NET Core 9.0
- **编排**: .NET Aspire (开发环境)
- **ORM**: Entity Framework Core
- **数据库**: PostgreSQL
- **缓存**: Redis
- **认证**: JWT Bearer / OAuth 2.0 / OIDC
- **文档**: Swagger/OpenAPI

### 前端
- **框架**: Angular 20 (Standalone Components)
- **UI**: Angular Material
- **状态**: Signals
- **国际化**: ngx-translate
- **测试**: Jest + Playwright

## 🧪 测试

### 后端测试
```bash
cd tests/Integration
dotnet test
```

### 前端测试
```bash
cd src/ClientApp/WebApp

# 单元测试
pnpm test

# E2E测试
pnpm e2e

# 覆盖率
pnpm test:coverage
```

## 📖 开发规范

### 实体定义
- 继承 `EntityBase`
- 使用 `[Module]` 特性标注所属模块
- 所有属性添加XML注释

### DTO模型
- 按实体组织目录：`XxxDtos/`
- 命名规范：`XxxAddDto`, `XxxUpdateDto`, `XxxItemDto`, `XxxDetailDto`, `XxxFilterDto`

### Manager层
- 继承 `ManagerBase<TEntity>`
- 实现业务逻辑，不直接调用其他Manager
- 公共逻辑放在 `CommonMod`

### Controller层
- 继承 `RestControllerBase<TEntity>`
- RESTful风格接口
- 方法命名：`AddAsync`, `UpdateAsync`, `DeleteAsync`, `GetDetailAsync`, `FilterAsync`

详见 [编码规范](src/ClientApp/WebApp/docs/CODING-STANDARDS.md)

## 🔐 安全最佳实践

1. **密钥管理**
   - 定期轮换签名密钥
   - 使用环境变量存储敏感配置
   - 客户端密钥使用哈希存储

2. **令牌安全**
   - 使用短期访问令牌（15分钟）
   - 实现刷新令牌轮换
   - 启用令牌撤销

3. **PKCE**
   - 所有公共客户端强制PKCE
   - 使用S256挑战方法

4. **速率限制**
   - 登录端点限流
   - 令牌端点限流
   - IP黑名单

## 🤝 示例集成

IAM 提供了完整的前后端集成示例，展示如何使用 OAuth 2.0 / OIDC 进行身份认证和授权。

所有示例项目都已集成到 .NET Aspire 中，可以一键启动所有服务。详见 [samples/README.md](samples/README.md)

### .NET后端集成
参见 [samples/backend-dotnet/](samples/backend-dotnet/)

演示内容：
- JWT Bearer 令牌验证
- 使用 IAM 作为授权服务器
- 受保护的 API 端点
- CORS 配置

```csharp
// 配置JWT认证
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://localhost:7070";  // IAM 服务器地址
        options.Audience = "ApiClient";  // API 客户端标识
    });
```

**运行端口**: `https://localhost:7000`

### Angular前端集成
参见 [samples/frontend-angular/](samples/frontend-angular/)

演示内容：
- OAuth 2.0 授权码流程 + PKCE
- 自动令牌管理和刷新
- 受保护路由
- HTTP 拦截器自动添加令牌

```typescript
// 配置OIDC客户端
export const authConfig: AuthConfig = {
  authority: 'https://localhost:7070',  // IAM 服务器地址
  clientId: 'FrontClient',  // 前端客户端标识
  redirectUri: window.location.origin,
  scope: 'openid profile email',
  responseType: 'code',
  usePkce: true  // 启用 PKCE
};
```

**运行端口**: `http://localhost:4201`

### 快速体验

使用 .NET Aspire 一键启动所有服务：

```bash
cd src/AppHost
dotnet run
```

然后访问：
- IAM 管理后台: `http://localhost:4200`
- 示例前端应用: `http://localhost:4201`

使用默认账号登录：
- 用户名: `admin`
- 密码: `MakeDotnetGreatAgain`

## 📋 待实现功能

详见 [未实现功能分析](docs/MISSING-FEATURES-ANALYSIS.md)

**高优先级**：
- [ ] 刷新令牌自动轮换
- [ ] 速率限制和防暴力破解
- [ ] 多因子认证（MFA）

**中优先级**：
- [ ] 外部身份提供商集成（Google, Microsoft等）
- [ ] 完善的用户同意管理
- [ ] 密钥自动轮换

## 📄 License

本项目采用 [MIT License](LICENSE) 开源协议。

## 🙏 致谢

基于 [Ater.Web.Template](https://github.com/AterDev/ater.web) 项目模板构建。

---

**项目状态**: ✅ 生产就绪（测试/开发环境）  
**维护者**: [@AterDev](https://github.com/AterDev)  
**最后更新**: 2025-11-02

