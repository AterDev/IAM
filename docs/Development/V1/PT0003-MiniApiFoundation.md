# PT0003 MiniApi 基础架构同步

- 迭代：V1
- 来源：[PD0003](../../UserStory/V1/PD0003-MiniApiFoundation.md)
- 状态：done，进度 3/3

- [x] TASK-001 对齐源生成器、OpenAPI、基础服务和 Aspire 17 编排并验证解决方案构建。
- [x] TASK-002 对齐管理前端和 OIDC 示例的依赖并验证生产构建。
- [x] TASK-003 发布并启动 Windows x64 ApiService，核对 OpenAPI 枚举元数据和健康端点。

## 实现与验证

- SourceGeneration 1.1.5 生成并嵌入枚举 XML。ApiService 输出 OpenAPI 3.1 数值枚举注解并保留原有全限定 schema ID。`IAM.slnx` 移除了未被引用的旧分析器项目条目。
- AppHost 补齐 Aspire 17 的 AppHost 与 Dotnet 引用，改用 `AddDotnetProject`。保留 IAM 的数据库迁移、OAuth 颁发者和示例应用依赖。
- PostgreSQL 连接池使用 Npgsql 注册和 OpenTelemetry，继续读取宿主连接配置。修复请求错误状态和 MFA 流程的空值契约。迁移工具版本与运行时 0.4.0 一致。
- `dotnet build IAM.slnx` 使用公开 NuGet 源和独立包缓存成功，保留业务实体的 34 条 CS8618 告警。CI 构建也改用公开源。`dotnet tool restore` 和 `pwsh ./scripts/Migrations.ps1 List` 成功。
- `dotnet publish src/Services/ApiService/ApiService.csproj -c Release -r win-x64 --self-contained true` 成功，保留 HybridCache 的裁剪和 AOT 告警。原生进程的 `/health`、`/alive`、`/openapi/v1.json` 均返回 200，验证枚举成员的数字 `const`。未连接 PostgreSQL 验证业务流程。
- MigrationService 和 ApiSampleService 的 Windows x64 Native AOT 发布通过。ApiSampleService 原生进程的公开和受保护接口分别返回 200 和 401。MigrationService 仅验证发布。
- 管理前端更新 Angular、Material/CDK、CLI 22.3.0，TypeScript 6.0.3 和 pnpm 10.33.2。使用显式 `src/*` 路径映射。现有页面继续使用 ngx-translate 17 的 `TranslateModule`，因此保留该版本。
- 管理前端冻结安装和生产构建通过，Jest 7 套 16 项通过。首包约 1.06 MB，保留预算和 qrcode CommonJS 告警。
- 示例前端同步 Angular 22.3.0、TypeScript 6.0.3、pnpm 10.33.2 和 OIDC 客户端 22.0.3，保留 ngx-translate 17 和 Zone.js 变更检测。生产构建不下载远程字体，浏览器继续使用原有字体链接。冻结安装及生产构建通过，未执行连接实际 IAM 的浏览器登录。
