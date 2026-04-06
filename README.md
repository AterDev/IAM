# IAM 身份与访问管理中心

基于 **.NET 10 / ASP.NET Core / EF Core / Angular 20 / .NET Aspire** 的集中式身份认证与授权中心，目标是为多个业务系统提供统一的登录、授权、令牌签发与管理能力。

## 文档导航

- [IAM功能清单](docs/IAM功能清单.md)

## 项目作用

本项目旨在提供一个统一的 IAM（Identity and Access Management）中心，用于：

- 统一登录入口与单点登录（SSO）
- 对外提供 OAuth 2.0 / OIDC 标准协议能力
- 为前后端应用、移动端、设备端、服务端应用提供令牌签发与验证能力
- 管理用户、角色、组织、客户端、作用域与 API 资源
- 提供会话审计、安全追踪与后台管理能力

## 当前代码结构

- `src/AppHost`：Aspire 编排入口
- `src/Services/ApiService`：认证中心后端服务
- `src/ClientApp/WebApp`：Angular 管理后台
- `src/Services/ApiSampleService`：示例受保护 API
- `src/Services/FrontSampleService`：示例前端 OIDC 客户端
- `src/Modules/IAMMod`：IAM 领域模块（Manager、服务、初始化种子）
- `src/Definition/Entity` / `src/Definition/EntityFramework`：实体与持久化层
- `tests/Tests`：测试工程

## 已实现的核心能力概览

### 协议与令牌服务

- OIDC Discovery：`/.well-known/openid-configuration`
- JWKS：`/.well-known/jwks`
- 授权端点：`/connect/authorize`
- 令牌端点：`/connect/token`
- 自省端点：`/connect/introspect`
- 撤销端点：`/connect/revoke`
- 登出端点：`/connect/logout`
- UserInfo：`/connect/userinfo`
- 设备授权入口：`/connect/device`

### 管理能力

- 用户管理
- 角色管理
- 组织管理
- OAuth/OIDC 客户端管理
- 作用域与 API 资源管理
- 会话查询与撤销
- 审计日志查询

### 运行与交付形态

- Aspire 一键启动本地开发环境
- 管理端与示例前端均为 Angular 应用
- 自动初始化签名密钥、默认管理员、默认作用域与默认客户端

## 基础环境

建议使用以下环境：

- Windows 11（仓库当前开发环境）
- .NET SDK 10
- Node.js 20+
- pnpm 9+
- Docker（用于构建和运行单镜像）

## 运行模式

当前仓库推荐分成两种运行方式：

1. **本地开发：Aspire 编排**
  - 适用于开发、调试、查看资源状态
  - 会编排数据库、缓存、前后端示例项目
  - 本地迁移仍通过 `MigrationService` 完成
2. **单镜像运行：外部数据库 / 外部缓存**
  - 适用于自托管部署与镜像分发
  - 只运行一个应用镜像
  - 数据库与缓存由用户自行准备
  - 应用启动时可自动执行迁移与初始化数据

## 本地开发

### 推荐方式：使用 Aspire 一键启动

在仓库根目录执行：

```bash
cd src/AppHost
dotnet run
```

启动后会编排以下服务：

- `MigrationService`：数据库迁移
- `ApiService`：认证中心后端
- `AdminApp`：Angular 管理后台
- `ApiSampleService`：示例 API
- `FrontSampleService`：示例前端

### 默认访问地址

> 为避免本地受限/冲突端口，后端服务默认开发端口已调整。

| 服务     | 地址                     | 说明            |
| -------- | ------------------------ | --------------- |
| IAM API  | `https://localhost:9900` | 认证与授权服务  |
| 管理后台 | `http://localhost:4200`  | Angular 管理端  |
| 示例 API | `https://localhost:9001` | 受保护 API      |
| 示例前端 | `http://localhost:4201`  | OIDC 客户端样例 |

### 本地初始化说明

- 本地开发环境下，数据库迁移由 `MigrationService` 负责。
- `ApiService` 中的 `InitHostService` 负责补齐签名密钥、默认管理员、默认客户端、默认作用域与权限种子。
- 如果首次启动后管理后台点击登录出现 `invalid_client`，或者 `https://localhost:9900` 长时间超时，请优先检查 `MigrationService` 是否已完成数据库初始化。当前本地编排下，若 `ApiService` 早于数据库创建完成而启动，`InitHostService` 可能会提前失败，导致默认管理员、`AdminWebClient`、`FrontSampleClient` 等种子尚未写入。此时重新启动一次 `ApiService` 资源即可恢复。

## 单镜像部署（外部数据库 / 缓存）

此模式下：

- **只运行一个应用镜像**
- **数据库与缓存由用户自己准备**
- **不要求使用 Docker Compose**
- 应用通过环境变量或挂载配置文件读取连接串

### 部署前提

至少需要准备以下基础设施：

- 一个 PostgreSQL 或 SQL Server 数据库
- 一个可选缓存（当前默认 `Memory`，因此不配 Redis 也能运行）

### 配置方式

推荐优先通过环境变量提供配置：

- `ASPNETCORE_ENVIRONMENT=Production`
- `Components__Database=PostgreSQL` 或 `SQLServer`
- `Components__Cache=Memory` 或 `Redis`
- `ConnectionStrings__Default=<数据库连接串>`
- `ConnectionStrings__Cache=<缓存连接串，仅在 Components__Cache=Redis 时需要>`

如果你希望通过文件管理配置，也可以直接挂载 `appsettings.Production.json` 到容器内，例如：

```powershell
docker run -d --name iam-app `
  -p 8080:8080 `
  --mount "type=bind,source=${PWD}\deploy\appsettings.Production.json,target=/app/appsettings.Production.json,readonly" `
  niltor/iam:latest
```

其中 `deploy/appsettings.Production.json` 可以写成：

```json
{
  "Components": {
    "Database": "PostgreSQL",
    "Cache": "Redis"
  },
  "ConnectionStrings": {
    "Default": "Host=your-db-host;Port=5432;Database=IAM;Username=iam;Password=your_password;Include Error Detail=true",
    "Cache": "your-redis-host:6379,password=your_password"
  }
}
```

如果使用默认内存缓存，则将 `Components:Cache` 设为 `Memory`，并移除 `ConnectionStrings:Cache` 即可。

### 构建并推送镜像

在仓库根目录执行：

```powershell
.\scripts\Publish-DockerImage.ps1 -Tag latest
```

默认会完成以下步骤：

- 构建 Angular 管理后台并同步到 `ApiService/wwwroot`
- 发布 `ApiService`
- 构建 Docker 镜像
- 推送镜像到 `docker.io/niltor/iam:<tag>`

### 运行示例：PostgreSQL

先准备数据库，例如本地测试环境可运行：

```powershell
docker run -d --name iam-db `
  -e POSTGRES_USER=iam `
  -e POSTGRES_PASSWORD=iam_test_pwd `
  -e POSTGRES_DB=IAM `
  -p 5432:5432 `
  postgres:18.1-alpine
```

然后启动应用镜像：

```powershell
docker run -d --name iam-app `
  -p 8080:8080 `
  -e ASPNETCORE_ENVIRONMENT=Production `
  -e Components__Database=PostgreSQL `
  -e Components__Cache=Memory `
  -e ConnectionStrings__Default="Host=host.docker.internal;Port=5432;Database=IAM;Username=iam;Password=iam_test_pwd;Include Error Detail=true" `
  niltor/iam:latest
```

> 如果数据库不在宿主机上，请将 `host.docker.internal` 替换为对应的主机名或 IP。

如果使用 Redis，可追加：

```powershell
-e Components__Cache=Redis `
-e ConnectionStrings__Cache="host.docker.internal:6379,password=your_redis_password"
```

### 启动行为

生产环境下，`ApiService` 启动后会：

1. 自动执行 `MigrateAsync()`
2. 幂等初始化签名密钥、默认管理员、默认客户端、默认作用域、默认 API 资源与权限种子
3. 正常启动 HTTP 服务

### 访问地址

默认容器端口为 `8080`，如果使用上面的示例命令，可访问：

- `http://localhost:8080/`
- `http://localhost:8080/swagger`

### 日志查看

```powershell
docker logs -f iam-app
```

如果启动迁移与初始化成功，日志中会看到类似输出：

- `Starting application initialization...`
- `Running startup database migrations...`
- `Application initialization completed successfully`

## 初始化数据

应用首次启动并在数据库可用时，会在 `InitHostService` 中自动初始化以下数据：

- 一个默认管理员账号
  - 用户名：`admin`
  - 密码：`Perigon.2026`
- 一个默认签名密钥（RSA）
- 默认作用域：`openid`、`profile`、`email`、`offline_access`、`SampleAPI`
- 一个管理后台客户端：`AdminWebClient`
- 一个示例前端客户端：`FrontSampleClient`
- 一个默认 API 客户端：`ApiService`
- 一个默认 API 资源：`SampleAPI`

> 建议仅将默认管理员口令用于本地开发，首次登录后立即修改。

### 管理后台统一认证默认数据说明

管理后台现在默认复用系统自身的 OAuth 2.0 / OIDC 能力，不再以“独立管理员 JWT 登录”作为主入口。

IAM 自身管理接口继续通过现有管理员策略（`WebConst.AdminUser`）与角色来控制访问；权限 claims 主要用于对外下发给业务系统消费，而不是统一替代 IAM 内部业务接口判断。

本地开箱即用**不需要手工新增额外种子数据**，因为启动时会确保默认种子里分别存在管理后台与示例前端的独立客户端：

- `AdminWebClient`
  - 管理后台回调地址：`http://localhost:4200`、`https://localhost:4200`
  - 管理后台 OIDC callback：`http://localhost:4200/auth/callback`、`https://localhost:4200/auth/callback`
  - 对应登出回调地址（4200）
- `FrontSampleClient`
  - 示例前端回调地址：`http://localhost:4201`、`https://localhost:4201`
  - 示例前端 OIDC callback：`http://localhost:4201/auth/callback`、`https://localhost:4201/auth/callback`
  - 对应登出回调地址（4201）
- 默认作用域：`openid profile email offline_access SampleAPI`

如果你使用的是较早版本数据库，`InitHostService` 会在启动时自动补齐缺失的客户端与回调地址。

旧的 `api/admin/login` 端点仍保留作兼容入口，但管理后台 WebApp 的默认登录方式已经切换为统一 OIDC 授权码 + PKCE 流程。

## 如何运行并验证示例项目

### 第一步：启动全套服务

按上面的 Aspire 方式启动项目。

### 第二步：登录管理后台

打开 `http://localhost:4200`，点击“前往统一登录”，然后在 IAM 登录页使用默认管理员账号登录：

- 用户名：`admin`
- 密码：`Perigon.2026`

登录成功后，如首次授权，会看到认证中心提供的授权确认页；确认后会自动回到管理后台。

### 第三步：检查示例项目所需配置

当前示例工程已经统一使用默认种子资源 **`SampleAPI`**。

建议确认以下配置存在：

- 默认管理后台客户端：`AdminWebClient`
- 默认示例前端客户端：`FrontSampleClient`
- 默认作用域：`openid`、`profile`、`email`、`offline_access`、`SampleAPI`
- 默认 API 资源：`SampleAPI`

如果你在本地数据库中已经保留了较早版本的初始化数据，建议登录管理后台检查 `FrontSampleClient` 是否已关联 `SampleAPI` 对应的资源与作用域配置，并确认已包含 `/auth/callback` 回调地址；如无，则补齐后再测试示例登录与 API 调用。

如需查看管理后台统一认证的详细链路、兼容策略与验证步骤，请参考 [`docs/管理后台统一认证使用说明.md`](docs/管理后台统一认证使用说明.md)。

### 第四步：访问示例前端

打开 `http://localhost:4201`：

1. 点击登录
2. 跳转到 IAM 登录页
3. 使用默认管理员账号登录
4. 完成授权后回到示例前端
5. 调用示例 API，验证受保护接口访问

## 手动单独启动（可选）

如果不使用 Aspire，也可以分别启动：

### 后端

```bash
cd src/Services/MigrationService
dotnet run

cd ../ApiService
dotnet run
```

> 该方式主要用于本地调试，不是推荐的生产部署模式。

### 管理后台

```bash
cd src/ClientApp/WebApp
pnpm install
pnpm start
```

### 示例前端

```bash
cd src/Services/FrontSampleService
pnpm install
pnpm start
```

## 当前实现状态提醒

从产品与技术成熟度来看，当前仓库已经具备：

- 集中式认证中心的基础架构
- OAuth/OIDC 核心端点与基本令牌流转
- 基础后台管理能力
- 示例集成演示能力

但以下内容仍属于重点完善项：

- MFA / WebAuthn / 恢复码等强认证能力
- 外部身份源登录后的账号绑定、自动建档与联邦注销
- 忘记密码 / 重置密码完整后端链路
- 权限校验与后台授权策略细化
- Token 安全策略与刷新令牌轮换加固
- 统一注销与跨客户端会话传播

详细清单见 [`docs/IAM功能清单.md`](docs/IAM功能清单.md)。
