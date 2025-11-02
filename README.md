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
- PostgreSQL 14+
- Redis（可选，用于缓存）

### 后端启动

```bash
# 克隆仓库
git clone https://github.com/AterDev/IAM.git
cd IAM

# 配置数据库连接
# 编辑 src/Services/ApiService/appsettings.Development.json

# 运行数据库迁移
cd src/Services/MigrationService
dotnet run

# 启动API服务
cd ../ApiService
dotnet run
```

API将在 `https://localhost:5001` 启动

### 前端启动

```bash
cd src/ClientApp/WebApp

# 安装依赖
pnpm install

# 启动开发服务器
pnpm start
```

管理门户将在 `http://localhost:4200` 启动

默认管理员账号：
- 用户名: `admin`
- 密码: `Admin@123`

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
- **ORM**: Entity Framework Core
- **数据库**: PostgreSQL
- **认证**: JWT Bearer
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

### .NET后端集成
参见 [samples/backend-dotnet/](samples/backend-dotnet/)

```csharp
// 配置JWT认证
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://your-iam-server";
        options.Audience = "your-api";
    });
```

### Angular前端集成
参见 [samples/frontend-angular/](samples/frontend-angular/)

```typescript
// 配置OIDC客户端
export const authConfig: AuthConfig = {
  issuer: 'https://your-iam-server',
  clientId: 'your-client-id',
  redirectUri: window.location.origin + '/callback',
  scope: 'openid profile email',
  responseType: 'code',
  usePkce: true
};
```

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

