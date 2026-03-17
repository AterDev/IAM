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

## 本地配置

通过Aspire提供本地开发环境，或使用现有资源。主要依赖数据库和缓存。

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

| 服务     | 地址                     | 说明            |
| -------- | ------------------------ | --------------- |
| IAM API  | `https://localhost:7070` | 认证与授权服务  |
| 管理后台 | `http://localhost:4200`  | Angular 管理端  |
| 示例 API | `https://localhost:7000` | 受保护 API      |
| 示例前端 | `http://localhost:4201`  | OIDC 客户端样例 |

## 初始化数据

应用首次启动时，会在 `InitHostService` 中自动初始化以下数据：

- 一个默认管理员账号
  - 用户名：`admin`
  - 密码：`Perigon.2026`
- 一个默认签名密钥（RSA）
- 默认作用域：`openid`、`profile`、`email`、`offline_access`
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
  - 对应登出回调地址（4201）
- 默认作用域：`openid profile email offline_access`

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
- 默认作用域：`openid`、`profile`、`email`、`offline_access`
- 默认 API 资源：`SampleAPI`

如果你在本地数据库中已经保留了较早版本的初始化数据，建议登录管理后台检查 `FrontClient` 是否已关联 `SampleAPI` 对应的资源与作用域配置；如无，则补齐后再测试示例登录与 API 调用。

如需查看管理后台统一认证的详细链路、兼容策略与验证步骤，请参考 [`docs/管理后台统一认证使用说明.md`](docs/管理后台统一认证使用说明.md)。

如果首次启动后管理后台点击登录出现 `invalid_client`，或者 `https://localhost:7070` 长时间超时，请优先检查 `MigrationService` 是否已完成数据库初始化。当前本地编排下，若 `ApiService` 早于数据库创建完成而启动，`InitHostService` 可能会提前失败，导致默认管理员、`AdminWebClient`、`FrontSampleClient` 等种子尚未写入。此时重新启动一次 `ApiService` 资源即可恢复。

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
- 统一注销与跨客户端会话传播

详细清单见 [`docs/IAM功能清单.md`](docs/IAM功能清单.md)。
