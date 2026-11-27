# PT0002-TestProjectRemoval

- 迭代：V1
- 来源 PD：`docs/UserStory/V1/PD0002-TestProjectRemoval.md`
- 目标：移除旧测试项目，为后续重构重写让路
- 总状态：`done`
- 进度：`2/2`

## Plan

- 影响范围：`tests/Tests` 全部文件、`IAM.slnx` 的 `/tests/` 节点、`README.md` 的目录说明。
- 不改动生产代码、包版本或 CI 配置；未发现其他项目通过 `InternalsVisibleTo` 等机制依赖测试程序集。
- 验证：枚举 `tests` 目录、检查解决方案与 README 引用、执行解决方案构建。
- AOT：旧测试项目不在发布闭包内，`AOT impact: not-affected`。

## Tasks

- [x] TASK-001 [REQ-001, REQ-002] 删除 `tests/Tests` 并移除解决方案与 README 中的引用
  - Depends on: none
  - Done when: `tests` 下无文件，`IAM.slnx` 无 `/tests/` 节点，README 不再声明 `tests/Tests` 为测试工程
  - Verify: `Get-ChildItem -Recurse -File c:/codes/IAM/tests`；检索 `IAM.slnx`、`README.md` 中的 `tests/Tests`
- [x] TASK-002 [SC-002] 解决方案构建验证通过
  - Depends on: TASK-001
  - Done when: `dotnet build IAM.slnx --nologo -v:q` 成功
  - Verify: `dotnet build c:/codes/IAM/IAM.slnx --nologo -v:q`
  - Result: [PT0003](PT0003-MiniApiFoundation.md) 修复生成器接线后，解决方案构建通过。

## 实现记录

### `2026-11-27` — TASK-001 / 移除旧测试项目

- Status: `done`
- Implementation: 删除 `tests/Tests` 下 19 个 `.cs`、`.csproj` 及 8 个构建缓存文件；`IAM.slnx` 移除 `/tests/` 文件夹节点；README 目录说明改为旧测试工程已移除。
- Code evidence: `IAM.slnx:40`（删除前 `/tests/` 节点）、`README.md:29`、`tests/Tests/Tests.csproj`（已删除）
- Verification:
  - `Get-ChildItem -Recurse -File c:/codes/IAM/tests` — `passed`（0 个文件）
  - 检索 `tests/Tests/Tests.csproj` 的解决方案/README 引用 — `passed`（0 处）
  - `dotnet build c:/codes/IAM/IAM.slnx --nologo -v:q` — `failed`（1 个 CS0103，来自 ApiService 的 `GeneratedEnumOpenApiMetadata`，与本次改动无关；构建已不再包含测试项目）
- AOT evidence: `not-affected`；旧测试项目不进入生产发布闭包。
- Documentation: 已同步 `docs/UserStory/V1/PD0002-TestProjectRemoval.md`、本月个人进度文件；PT0001 的阻断原因已更新。
- Remaining: 后续重写测试时重新建立 TUnit/Aspire 验证矩阵。
