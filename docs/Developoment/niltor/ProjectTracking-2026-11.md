# ProjectTracking-2026-11

- Git 用户：`niltor`
- 当前迭代：V1
- 本跟踪记录仅反映当前用户当月工作，不代表项目整体进度。

## 当前任务

- `docs/Development/V1/PT0001-EntityAttributes.md` — 状态 `done`，进度 `2/2`
- `docs/Development/V1/PT0002-TestProjectRemoval.md` — 状态 `done`，进度 `2/2`
- `docs/Development/V1/PT0003-MiniApiFoundation.md` — 状态 `done`，进度 `3/3`
- 来源：`docs/UserStory/V1/PD0001-EntityAttributes.md`、`docs/UserStory/V1/PD0002-TestProjectRemoval.md`

## 实现记录

### `2026-11-27` — TASK-001 / 删除实体无效映射特性

- 删除 `src/Definition/Entity` 下 25 个实体文件中的 75 处 `Perigon.PostgreSQL.Attributes.Column` / `Index` 特性。
- 验证：实体项目、DbContext 项目构建通过（0 错误）；目录中无特性残留。
- 证据：`dotnet build c:/codes/IAM/src/Definition/Entity/Entity.csproj --nologo -v:q`；`dotnet build c:/codes/IAM/src/Definition/EntityFramework/EntityFramework.csproj --nologo -v:q`。
- AOT：尚未验证；整体构建恢复后需执行 Native AOT 发布。

### `2026-11-27` — TASK-001 / 移除旧测试项目

- 删除 `tests/Tests` 下 19 个 `.cs`、`.csproj` 及 8 个构建缓存文件；`IAM.slnx` 移除 `/tests/` 节点，README 同步说明。
- 验证：`tests` 目录文件数为 0；解决方案与 README 无旧测试项目引用；构建已不再包含测试项目。
- 证据：`dotnet build c:/codes/IAM/IAM.slnx --nologo -v:q` — 失败于 `GeneratedEnumOpenApiMetadata` CS0103（非本次改动）。
- AOT：`not-affected`，旧测试项目不在发布闭包内。

- 2026-11-27：已修复枚举生成和 AppHost 编译阻塞。仅使用公开 NuGet 源和独立缓存构建解决方案及还原 PostgreSQL 0.4.0 迁移工具通过，迁移列表可读取。ApiService Windows x64 原生发布和健康检查、OpenAPI 3.1 冒烟通过。
- 2026-11-27：Angular 管理端同步到 Angular 22.3.0、TypeScript 6.0.3 和 pnpm 10.33.2，生产构建及 Jest 16 项通过。
- 2026-11-27：OIDC 示例前端同步相同 Angular 基线，冻结安装和生产构建通过。示例 API 原生进程实际验证公开和受保护接口状态 200/401。

## 阻塞与下一步
- 列长度、唯一索引声明删除后的数据库 schema 迁移需在部署前评估。
- 后续按 TUnit/Aspire 方案重写测试项目，重建验证矩阵。
