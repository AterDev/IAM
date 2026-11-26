# AGENTS

## 总体原则

编写结构清晰，可读性高，简洁的代码和文档，专注于程序的稳定性，高性能和安全性，遵循以下原则：

- 以官方文档、仓库代码、配置、工具帮助和实际验证为事实依据，不做猜测，不使用不成熟或非官方的方案；
- 遵循项目层级结构和分层边界，非必要不进行抽象，不使用接口；
- 代码和注释保持良好格式，使用官方推荐写法，命名清晰、注释简洁，避免非必要的重复验证和处理;
- 纯调研或只读审查不产生文档。所有修改都必须在结束前同步对应迭代 PT、当前 Git 用户的月度进度文件 和受影响 PD，记录实现、验证及 AOT 影响；
- 代码修改后执行与影响面匹配的最小充分验证。没有实现证据和通过的必需验证，不宣称任务完成。

## 技术栈与架构边界

1. C# 14、.NET 11、ASP.NET Core Minimal API, Perigon.Postgres。
2. ApiService 默认启用 Native AOT、Trim、Request Delegate Generator。
3. Aspire 17+ 负责 PostgreSQL、缓存、服务编排和可观测性。
4. 前端默认使用 Angular 22+，具体以仓库现有代码和版本为准。

## 项目结构

- 客户端：`src/ClientApp`
- 接口服务：`src/Services/ApiService`
  - Endpoint groups：`Endpoints`
- 业务逻辑：`src/Modules`，按模块划分
- 实体：`src/Definition/Entity`
- DbContext(Perigon.Postgres)：`src/Definition/EntityFramework`
- 共享定义：`src/Definition/Share`
- 服务扩展：`src/Definition/ServiceDefaults`
- AppHost：`src/AppHost`
- 文档：`docs`
  - 总览：`docs/UserStory/Demand.md`、`docs/UserStory/Design.md`
  - 功能设计：`docs/UserStory/Iter<number>-<Name>/PD<4-digit>-<Module>.md`
  - 计划记录：`docs/Development/Iter<number>-<Name>/PT<4-digit>-<Module>.md`
  - 个人月度进度：`docs/Developoment/<username>/ProjectTracking-<YYYY-MM>.md`（用户名取 `git config user.name`，月份取本地当前年月；读取和创建规则见 `delivery-loop`）
- 脚本：`scripts`
- 测试：`tests`

## Minimal API 与 AOT 规则

- Endpoint group 继承 `RestEndpointBase` 并提供 `public static void MapEndpoints(IEndpointRouteBuilder)`，由源生成的 `MapEndpointGroups()` 注册。
- handler 优先使用具名 `public static` typed method；绑定来源和响应契约保持静态可分析。
- Endpoint 负责路由、绑定、授权、验证和 HTTP 结果；业务流程与数据访问进入 Manager/Service。
- 新增 DTO、converter、多态、反射、动态代码、程序集扫描、DI 自动发现、实体模型或第三方依赖时必须评估 AOT/Trim。
- 优先源生成、显式注册和静态映射。不要用 warning suppression 或 linker descriptor 掩盖未理解的可达性问题。
- 普通 Debug/Release build 不能代替 Native AOT publish；AOT-sensitive 变更还应运行发布产物验证真实绑定和序列化。

## DTO 目录与命名（强制）

- 模块中所有 DTO 放在该模块 `Models/{Entity}Dtos` 目录，例如 `Models/ResourceDtos`。
- 一个文件只定义一个类型；不将 DTO 集中到 `Contracts.cs`、`Dtos.cs` 等多类型文件。
- 数据转换边界类型以 `Dto` 结尾，不使用 `Input`、`Request`、`Response` 作为 DTO 类型名的一部分。
- 新增、修改、筛选、详情和列表优先命名为 `{Entity}AddDto`、`{Entity}UpdateDto`、`{Entity}FilterDto`、`{Entity}DetailDto`、`{Entity}ItemDto`。
- DTO 的嵌套属性也引用符合规则的 DTO，不使用 `Input` 或实体代替。
- 新增或修改模块时同时检查目录、文件粒度、类型命名和所有引用；生成代码后仍要整理并构建验证。

## 工具与 skill 路由

- 当前AI工具中要安装 aspire 插件,`https://github.com/microsoft/aspire-skills`
- Perigon 架构、CLI、生成、Endpoint/Manager/模型和模板约定：`perigon`。
- Native AOT、Trim、RDG、JSON、反射、依赖和发布兼容：`native-aot`。
- AppHost、资源、启停、状态、日志或部署：使用环境中可用的 Aspire skill；普通构建/测试直接使用 `dotnet`。
- 需求、设计、迭代 PD/PT、实现记录：使用模板项目内的 `docs`。
- AI coding 后同步、计划执行、完成审计和收敛：`delivery-loop`。
- TUnit、Aspire API 集成测试和 AOT 测试证据：`test`。
- 差异审查和质量门：`code-review`。
- 页面布局与交互：`ux`；Angular 实现同时遵循 `perigon` 的 Angular reference。

规划、实现和审查业务代码前读取 `.agents/skills/perigon/SKILL.md`。只有任务触及相应边界时再读取 `native-aot`、测试、Aspire 或 UX 技能，避免无关工作启动基础设施。

## AI coding 交付流程

1. 只读探索现状，确认迭代、功能范围和关键待确认项。
2. 编写或更新对应 PD 与 PT；简单修复可用简短 PT，但不跳过跟踪。
3. 实现前检查需求覆盖、依赖、验证以及 Minimal API/AOT 风险；阻断问题未解决时不开始。
4. 按依赖执行最小 ready task，每项实现后立即验证；AOT-sensitive 任务按 `native-aot` 验证。
5. 对照意图检查 Completeness、Correctness、Coherence；缺口以新任务追加。
6. 每次代码修改后更新 PT 的 checkbox、进度、实现结果、验证/AOT 证据，同步 当前 Git 用户的月度进度文件 和受影响 PD。

## 完成定义

只有同时满足以下条件才声明完成：

- 纳入范围的必须行为均已实现，无未说明范围漂移；
- 必需 build、test、契约、迁移、前端或运行验证已通过，精确命令可报告；
- 验收场景、失败路径、授权/租户和回归风险按影响面覆盖；
- 生成结果、DTO 边界、公开契约、文档和跟踪状态已同步；
- 无未解决的阻断问题，剩余风险和未运行验证已明确报告。

## 工程决策

1. 优先官方、主流、活跃维护且公开声明支持 Native AOT 的方案。
2. 复用现有 `src/Perigon`、`Definition/Share`、`ServiceDefaults` 和源生成能力，避免过度抽象。
3. 以类型系统、静态可分析契约、清晰分层和单一职责表达业务意图。
