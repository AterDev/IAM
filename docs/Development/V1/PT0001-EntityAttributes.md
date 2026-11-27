# PT0001-EntityAttributes

- 迭代：V1
- 来源 PD：`docs/UserStory/V1/PD0001-EntityAttributes.md`；背景 `docs/Development/V1/重构说明.md`
- 目标：清除实体上无效的数据库映射特性并验证构建
- 总状态：`done`
- 进度：`2/2`

## Plan

- 影响范围：`src/Definition/Entity/IAMMod`、`src/Definition/Entity/UserCenterMod` 共 25 个实体文件。
- 不涉及 endpoint、Manager、DTO/JSON 契约或包依赖变更。
- 验证：实体与 DbContext 项目独立构建；全解决方案构建；构建恢复后补跑 Native AOT 发布。
- 数据库：删除列长度与索引声明后，schema/迁移影响需在部署前单独评估，本次不生成迁移。

## Tasks

- [x] TASK-001 [REQ-001] 删除 25 个实体文件中的 75 处 `Perigon.PostgreSQL.Attributes.Column` / `Index` 特性
  - Depends on: none
  - Done when: 实体目录无 `Perigon.PostgreSQL.Attributes` 残留，实体与 DbContext 项目构建通过
  - Verify: `dotnet build src/Definition/Entity/Entity.csproj --nologo -v:q`；`dotnet build src/Definition/EntityFramework/EntityFramework.csproj --nologo -v:q`
- [x] TASK-002 [SC-002] 全解决方案构建通过并补充 AOT 发布证据
  - Depends on: TASK-001
  - Done when: `dotnet build IAM.slnx --nologo -v:q` 成功，且 Native AOT 发布验证完成
  - Verify: `dotnet build IAM.slnx --nologo -v:q`
  - Result: [PT0003](PT0003-MiniApiFoundation.md) 修复枚举生成契约，验证了解决方案构建及 ApiService Windows x64 原生发布。

## 实现记录

### `2026-11-27` — TASK-001 / 删除实体无效映射特性

- Status: `done`
- Implementation: 删除 25 个实体文件中的 64 处 `Column` 与 11 处 `Index` 特性；未改动其他代码。
- Code evidence: `src/Definition/Entity/IAMMod/User.cs:13`、`src/Definition/Entity/UserCenterMod/ProxyUsage.cs:8`、`src/Definition/Entity/UserCenterMod/UserEntitlementDefinition.cs:8`
- Verification:
  - `dotnet build c:/codes/IAM/IAM.slnx --nologo -v:q` — `failed`（整改前 131 个 CS0234，全部来自上述特性）
  - `dotnet build c:/codes/IAM/src/Definition/Entity/Entity.csproj --nologo -v:q` — `passed`（0 错误）
  - `dotnet build c:/codes/IAM/src/Definition/EntityFramework/EntityFramework.csproj --nologo -v:q` — `passed`（0 错误）
  - `dotnet build c:/codes/IAM/src/Services/ApiService/ApiService.csproj --nologo -v:q` — `failed`（仅剩 3 个与本任务无关的 CS0246）
  - 目录全文检索 `Perigon.PostgreSQL.Attributes` — `0` 处残留
- AOT evidence: `blocked`；实体进入发布闭包，需在整体构建恢复后执行 Native AOT 发布验证。
- Documentation: 已同步 `docs/UserStory/V1/PD0001-EntityAttributes.md` 与本月个人进度文件。
- Remaining: 列长度和唯一索引声明删除后的数据库 schema 迁移需单独评估。
