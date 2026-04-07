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

## 单镜像部署（外部数据库 / 缓存）

此模式下：

- **只运行一个应用镜像**
- **数据库与缓存由用户自己准备**
- **不要求使用 Docker Compose**
- 应用通过环境变量或挂载配置文件读取连接串

### 本地发布并验证镜像

在仓库根目录执行：

```powershell
.\scripts\Publish-LocalContainerImage.ps1 -Tag latest
```

默认会完成以下步骤：

- 构建 Angular 管理后台并同步到 `ApiService/wwwroot`
- 发布 `ApiService` 容器镜像到本地运行时（Alpine extra 运行时变体，保留本地化支持）
- 本地镜像名为 `niltor/iam:<tag>`

> 这一步由 .NET SDK 的 `dotnet publish /t:PublishContainer` 完成，不再依赖 `Dockerfile`、`docker build` 或 `docker push`。
> Alpine extra 变体会保留 globalization 支持，否则 `zh-CN` 等区域设置无法正常加载。

### 使用本地镜像进行验证

先准备数据库与缓存容器，例如：

```bash
docker network create iam-test-net

docker run -d --name iam-db \
  --network iam-test-net \
  -e POSTGRES_USER=iam \
  -e POSTGRES_PASSWORD=iam_test_pwd \
  -e POSTGRES_DB=IAM \
  postgres:18.1-alpine

docker run -d --name iam-redis \
  --network iam-test-net \
  redis:7.4-alpine
```

然后启动本地镜像：

```bash
docker run -d --name iam-app \
  --network iam-test-net \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e Components__Database=PostgreSQL \
  -e Components__Cache=Redis \
  -e ConnectionStrings__Default="Host=iam-db;Port=5432;Database=IAM;Username=iam;Password=iam_test_pwd;Include Error Detail=true" \
  -e ConnectionStrings__Cache="iam-redis:6379" \
  niltor/iam:latest
```

### 容器部署运行

请在应用运行前，确保已经准备好数据库与缓存，并且应用能够通过连接串访问到它们。

如果你希望通过配置文件管理生产环境参数，可以在宿主机准备一个 `appsettings.Production.json`，例如：

```json
{
  "Components": {
    "Database": "PostgreSQL",
    "Cache": "Memory"
  },
  "Authentication": {
    "PublicOrigin": "https://auth.xxx.cn"
  },
  "ConnectionStrings": {
    "Default": "Host=host.docker.internal;Port=5432;Database=IAM;Username=iam;Password=iam_test_pwd;Include Error Detail=true"
  }
}
```

然后把这个文件挂载到容器内：

```bash
docker run -d --name iam-app \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  --mount type=bind,source=/opt/iam/appsettings.Production.json,target=/app/appsettings.Production.json,readonly \
  niltor/iam:latest
```

这样在生产环境里，`InitHostService` 初始化 `AdminWebClient` 时会优先使用 `Authentication:PublicOrigin`，例如：

- `https://auth.xxx.cn`
- `https://auth.xxx.cn/auth/callback`

如果不想挂载配置文件，也可以继续通过环境变量传入同样的值：

```bash
docker run -d --name iam-app \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e Authentication__PublicOrigin="https://auth.xxx.cn" \
  -e Components__Database=PostgreSQL \
  -e Components__Cache=Memory \
  -e ConnectionStrings__Default="Host=host.docker.internal;Port=5432;Database=IAM;Username=iam;Password=iam_test_pwd;Include Error Detail=true" \
  niltor/iam:latest
```

> 数据库在宿主机上时，使用 `host.docker.internal` 作为对应的主机名，并添加 `--add-host=host.docker.internal:host-gateway` 参数。

如果使用 Redis，可追加：

```bash
-e Components__Cache=Redis \
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

```bash
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

管理后台统一认证的详细默认客户端、回调地址和验证步骤，单独整理在 [`docs/管理后台统一认证使用说明.md`](docs/管理后台统一认证使用说明.md)。
