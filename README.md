# IAM 身份与访问管理中心

基于 **.NET 10 / ASP.NET Core / EF Core / Angular 20 / .NET Aspire** 的集中式身份认证与授权中心，目标是为多个业务系统提供统一的登录、授权、令牌签发与管理能力。

当前仓库已经具备 **OAuth 2.0 / OpenID Connect 核心服务雏形**，并提供：

- 认证中心后端：发现文档、授权、令牌、JWKS、UserInfo、撤销、自省、设备码等端点
- 管理后台前端：用户、角色、组织、客户端、作用域、资源、会话、审计日志等管理页面
- 示例项目：一个受保护的示例 API 与一个 OIDC 前端示例应用
- Aspire 编排：本地一键拉起 API、管理端、示例服务和迁移任务

> 说明：当前版本更适合看作“可运行的认证中心骨架 + 管理平台基础能力”。
> 核心协议链路已具备，但在 MFA、外部身份联邦落地、自助找回密码、样例开箱即用一致性、安全加固等方面仍需继续完善。

## 文档导航

- [IAM解决方案设计文档](docs/IAM解决方案设计文档.md)
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

## 本地配置

本地开发默认通过 `src/AppHost/appsettings.Development.json` 读取数据库与缓存连接。

你至少需要准备：

- `ConnectionStrings:Default`：PostgreSQL 连接字符串
- `ConnectionStrings:Cache`：Redis 连接字符串

推荐做法：

- 使用本机或开发环境专用的 PostgreSQL / Redis
- 使用本地覆盖配置、用户机密或环境变量管理连接信息
- 不要把真实生产凭据直接提交到仓库

如果希望 Discovery 文档在反向代理或非本地地址下工作稳定，建议同时配置认证服务的发行者地址（Issuer）。

## 运行项目

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

| 服务 | 地址 | 说明 |
| --- | --- | --- |
| IAM API | `https://localhost:7070` | 认证与授权服务 |
| 管理后台 | `http://localhost:4200` | Angular 管理端 |
| 示例 API | `https://localhost:7000` | 受保护 API |
| 示例前端 | `http://localhost:4201` | OIDC 客户端样例 |

## 初始化数据

应用首次启动时，会在 `InitHostService` 中自动初始化以下数据：

- 一个默认管理员账号
  - 用户名：`admin`
  - 密码：`Perigon.2026`
- 一个默认签名密钥（RSA）
- 默认作用域：`openid`、`profile`、`email`、`offline_access`
- 一个默认前端客户端：`FrontClient`
- 一个默认 API 客户端：`ApiService`
- 一个默认 API 资源：`SampleAPI`

> 建议仅将默认管理员口令用于本地开发，首次登录后立即修改。

## 如何运行并验证示例项目

### 第一步：启动全套服务

按上面的 Aspire 方式启动项目。

### 第二步：登录管理后台

打开 `http://localhost:4200`，使用默认管理员账号登录：

- 用户名：`admin`
- 密码：`Perigon.2026`

### 第三步：检查示例项目所需配置

这里有一个**当前仓库必须注意的配置差异**：

- 初始化种子默认创建的 API 资源名称是：`SampleAPI`
- 示例前端与示例 API 当前使用的目标标识是：`ApiTest`

也就是说，**示例项目默认并不是完全开箱即用一致的**。要跑通示例，需要二选一：

1. 在管理后台中补充一套与示例一致的配置（推荐不改代码时使用）
   - 新建作用域：`ApiTest`
   - 新建 API 资源：`ApiTest`
   - 将 `ApiTest` 作用域分配给 `FrontClient`
   - 将 `ApiTest` 资源分配给 `FrontClient`

2. 或者把示例工程中的 `ApiTest` 改成 `SampleAPI`
   - `src/Services/ApiSampleService/appsettings.Development.json`
   - `src/Services/FrontSampleService/src/app/app.config.ts`

如果只是为了最快验证样例链路，建议优先在管理后台补齐 `ApiTest` 的作用域和资源配置。

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
- 样例配置与初始化种子的一致性

详细清单见 [`docs/IAM功能清单.md`](docs/IAM功能清单.md)。
