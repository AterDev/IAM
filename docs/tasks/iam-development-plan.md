# IAM 系统开发任务规划

本规划依据 `README.md`、`.github/copilot-instructions.md` 以及《IAM解决方案设计文档》整理，面向开发团队（含 AI 助手）提供可执行的分阶段任务清单。任务按后端优先、前端随后、再到运维与验收的顺序组织，并标明依赖关系、所需数据结构、接口设计与实现要点。

## 1. 总体规划

- **阶段划分**：后端基础设施 → 身份与访问后端能力 → 前端管理门户 → 集成测试与交付。
- **模块对齐**：CommonMod（共用服务）、IdentityMod（认证授权）、AccountMod（账号管理）、AccessMod（客户端与作用域管理）。
- **协作约定**：实体 → Dto → Manager → Controller → 前端 API 对接，遵循模板规范；所有集合默认使用 `[]`，if/for 强制花括号；优先使用模式匹配与可空类型。

## 2. 后端阶段任务

### B1. 基础服务与基础设施

- **目标**：打通全局依赖，提供可复用的系统服务（配置、缓存、密码哈希、审计日志钩子等）。
- **范围**：`src/Definition/ServiceDefaults`、`src/Definition/Share`、`src/Modules/CommonMod`。
- **数据结构**：
  - `CommonMod` 中新增 `AuditLog`、`SystemSetting` 实体（继承 `EntityBase`，标注 `[Modules(ModuleCodes.Common)]`）。
- **接口设计**：
  - `CommonSettingsController` 提供 `GET /api/common/settings`、`PUT /api/common/settings`。
  - `AuditTrailController` 提供 `GET /api/common/audit-logs`，支持筛选分页。
- **实现步骤**：
  1. 在 `CommonMod` 定义实体 + DTO（`AuditLogDtos/*`, `SystemSettingDtos/*`）。
  2. 实现 `AuditLogManager`、`SystemSettingManager`，封装写入/查询逻辑。
  3. 在 `Share/Services` 封装 `IAuditTrailService`、`IPasswordHasher` 等跨模块服务。
  4. ApiService 中创建对应控制器，接入 `Manager`。
- **依赖**：无，最优先执行。

### B2. 身份与访问数据模型

- **目标**：完成 IdentityServer / OpenIddict 所需核心实体建模与 EF 配置。
- **范围**：`src/Definition/Entity`、`src/Definition/EntityFramework`，模块 `IdentityMod`、`AccessMod`。
- **实体设计**：
  - `IdentityMod`: `User`, `UserClaim`, `UserLogin`, `UserToken`, `Role`, `RoleClaim`, `UserRole`, `Organization`, `OrganizationUser`, `LoginSession`。
  - `AccessMod`: `Client`, `ClientRedirectUri`, `ClientGrantType`, `ClientScope`, `ApiResource`, `ApiScope`, `ScopeClaim`, `Authorization`, `Token`。
  - 字段示例：`User` 包含 `UserName`, `NormalizedUserName`, `Email`, `PasswordHash`, `SecurityStamp`, `LockoutEnd`, `TenantId` 等；
    `Client` 包含 `ClientId`, `ClientSecret`, `DisplayName`, `Type`, `RequirePkce`, `ConsentType`。
- **关系要求**：
  - 用户与组织多对多；角色与权限多对多；客户端与作用域多对多；授权与令牌一对多。
  - 所有实体启用软删除（继承 `EntityBase`，保留 `Deleted` 段）。
- **EF 配置**：在 `EntityFramework` 项目内为新增实体配置 `EntityTypeConfiguration`（键、索引、并发字段、关系约束）。
- **实现步骤**：
  1. 定义实体文件及对应 `Modules` 特性。
  2. 创建 DTO 骨架：`UserDtos`, `RoleDtos`, `OrganizationDtos`, `ClientDtos`, `ScopeDtos` 等。
  3. 更新 `DbContext` 注册实体 + Fluent 配置。
  4. 生成初始迁移（暂不执行数据库更新，保留脚本）。
- **依赖**：B1。

### B3. Common 服务实现（密码、加密、Token 工具）

- **目标**：提供后续模块依赖的核心安全算法及助手。
- **范围**：`CommonMod`、`Share/Services`。
- **组件**：
  - `PasswordHasherService`（基于 ASP.NET Identity PBKDF2）。
  - `JwtTokenService`（封装 Access/ID Token 创建与签名）。
  - `KeyManagementService`（JWK 轮换、持久化：存储于 `CommonMod` 的 `SigningKey` 实体）。
- **实现步骤**：
  1. 定义服务接口与实现，注入 DI。
  2. 编写单元测试：密码哈希验证、Token 生成与验证。
- **依赖**：B1, B2。

### B4. 身份认证核心（IdentityMod）

- **目标**：实现 OAuth2/OIDC 授权码、客户端凭证、刷新令牌、设备码流程。
- **接口设计**：
  - `POST /connect/authorize`、`POST /connect/token`、`POST /connect/device`、`POST /connect/introspect`、`POST /connect/revoke`、`POST /connect/logout`。
  - 采用 `RestControllerBase` 或中间件方式输出标准响应。
- **实现步骤**：
  1. 在 `IdentityMod` 实现 `AuthorizationManager`、`TokenManager`、`DeviceFlowManager`，对接 B3 服务。
  2. 处理授权请求验证、同意记录、PKCE 校验、多因子 hooks。
  3. ApiService 中实现 `OAuthController`（或分离为多控制器）。
  4. 完成端到端单元/集成测试（授权码、刷新令牌、客户端凭证）。
- **依赖**：B1-B3。

### B5. 账号与组织管理（AccountMod）

- **目标**：提供用户、角色、组织的 CRUD 与权限分配能力。
- **接口设计**：
  - 用户：`GET /api/users`, `GET /api/users/{id}`, `POST /api/users`, `PUT /api/users/{id}`, `PATCH /api/users/{id}/status`, `DELETE /api/users/{id}`。
  - 角色：`GET /api/roles`、`POST /api/roles`、`PUT /api/roles/{id}`、`DELETE ...`、`POST /api/roles/{id}/permissions`。
  - 组织：`GET /api/organizations/tree`, `POST /api/organizations`, `PUT /api/organizations/{id}`、`DELETE ...`、`POST /api/organizations/{id}/users`。
- **实现步骤**：
  1. 完善 DTO（`UserAddDto`, `UserUpdateDto`, `RoleGrantPermissionDto`, `OrganizationTreeDto`）。
  2. 实现 `UserManager`, `RoleManager`, `OrganizationManager`，包含筛选、分页、软删除、权限检查。
  3. 在 ApiService 创建对应控制器，接入授权验证。
  4. 编写集成测试覆盖用户注册、角色授权、组织树。
- **依赖**：B2, B3。

### B6. 客户端与作用域管理（AccessMod）

- **目标**：提供客户端、作用域、API 资源的配置能力。
- **接口设计**：
  - 客户端：`GET /api/clients`, `GET /api/clients/{id}`, `POST /api/clients`, `PUT /api/clients/{id}`, `DELETE /api/clients/{id}`, `POST /api/clients/{id}/secret:rotate`, `POST /api/clients/{id}/scopes`。
  - 作用域：`GET /api/scopes`, `POST /api/scopes`, `PUT /api/scopes/{id}`, `DELETE /api/scopes/{id}`。
  - API 资源：`GET /api/resources`、`POST /api/resources` 等。
- **实现步骤**：
  1. 定义 `ClientManager`, `ScopeManager`, `ResourceManager`。
  2. 支持密钥轮换、授权记录查询、客户端权限校验。
  3. 控制器实现 + 集成测试（创建客户端、授权范围校验）。
- **依赖**：B2, B3, B4。

### B7. 安全审计与会话管理

- **目标**：实现会话监控、审计日志、令牌撤销。
- **接口设计**：
  - `GET /api/security/sessions`, `POST /api/security/sessions/{id}/revoke`。
  - `GET /api/security/logs`（复用 B1 audit）。
- **实现步骤**：
  1. 在 `IdentityMod` 增加 `SessionManager`，记录 `LoginSession`。
  2. 接入 `AuditLogManager`，在授权成功/失败、权限变更时写日志。
  3. ApiService 控制器 + 集成测试。
- **依赖**：B1-B6。

### B8. 集成测试与文档

- **目标**：覆盖主要授权流程、用户/角色/客户端 CRUD。
- **范围**：`tests` 目录（需新建）。
- **实现步骤**：
  1. 搭建测试基础（xUnit 或 NUnit）+ TestServer。
  2. 编写端到端测试用例。
  3. 输出 API 文档（Swagger/OpenAPI 自动化）。
- **依赖**：B4-B7。

## 3. 前端阶段任务（Angular 20+）

### F1. Admin Portal 骨架与共享模块

- **目标**：初始化 Angular 应用结构、路由、主题、身份拦截器。
- **实现要点**：
  - 现有基础骨架位于 `src/ClientApp/WebApp`，在该项目内扩展模块与配置。
  - 设置 `CoreModule`, `SharedModule`，引入 Angular Material。
  - 实现 `AuthHttpInterceptor`，注入 Access Token。
  - 基础布局（导航、侧边栏、面包屑）。
- **依赖**：后端认证端点可用（B4）。

### F2. 认证与登录流程

- **目标**：实现登录、注册、MFA、授权同意页。
- **页面**：登录、注册、忘记密码、设备码输入、授权同意。
- **接口对接**：`/connect/token`, `/connect/authorize`, `/api/users`（注册）。
- **实现步骤**：
  1. 编写表单组件 + 表单验证。
  2. 集成 PKCE/OIDC 客户端逻辑。
  3. 处理状态管理（NgRx 或 Signals）。
- **依赖**：F1, B4, B5。

### F3. 用户与组织管理界面

- **目标**：列表、详情、创建、编辑、组织树展示。
- **接口**：`/api/users/*`, `/api/organizations/*`。
- **实现步骤**：
  1. 构建可复用的表格与树组件。
  2. 支持批量操作、状态切换。
  3. 集成权限控制（路由守卫 + 指令）。
- **依赖**：F1, F2, B5。

### F4. 角色与权限管理界面

- **目标**：角色列表、权限分配、作用域管理入口。
- **接口**：`/api/roles/*`, `/api/scopes/*`（查看）。
- **实现步骤**：
  1. 角色权限编辑（树状或勾选）。
  2. 权限明细查看、搜索。
- **依赖**：F1-F3, B5, B6。

### F5. 客户端与作用域配置界面

- **目标**：客户端 CRUD、密钥轮换、作用域管理。
- **接口**：`/api/clients/*`, `/api/scopes/*`, `/api/resources/*`。
- **实现步骤**：
  1. 密钥轮换弹窗、复制功能。
  2. 授权记录查看。
- **依赖**：F1-F4, B6。

### F6. 安全监控与审计

- **目标**：展示会话、审计日志、令牌撤销。
- **接口**：`/api/security/sessions`, `/api/security/logs`。
- **实现步骤**：
  1. 会话列表 + 强制注销。
  2. 日志筛选（时间、用户、事件）。
- **依赖**：F1-F5, B7。

### F7. 前端自动化测试与文档

- **目标**：单元测试（Jest）、端到端测试（Cypress/Playwright），编写用户手册。
- **依赖**：F1-F6。

## 4. 运维与交付

- **部署脚本**：完善 `scripts/` 下发布脚本，与 CI/CD 集成。
- **配置管理**: appsettings 模板、密钥注入策略。
- **文档**：部署指南、API 使用说明。
- **验收标准**：通过集成测试、前端端到端测试、功能验收 checklist。

## 5. 依赖关系总览

1. B1 → B2 → B3 → B4 → B5 → B6 → B7 → B8。
2. F1 依赖 B4 基本可用；F2 依赖 B4/B5；F3-F6 依赖相应后端模块；F7 为收尾。
3. 运维与交付在主要功能完成后执行，与 B8/F7 并行。

## 6. 附录：实体与接口摘要

### 核心实体字段示例

| 实体 | 关键字段 | 说明 |
| --- | --- | --- |
| User | `UserName`, `NormalizedUserName`, `Email`, `PasswordHash`, `TenantId`, `IsTwoFactorEnabled` | 继承 `EntityBase`，软删除 |
| Role | `Name`, `NormalizedName`, `ConcurrencyStamp`, `TenantId` | 与权限关联 |
| Organization | `Name`, `ParentId`, `Path`, `TenantId` | 多租户组织树 |
| Client | `ClientId`, `ClientSecret`, `Type`, `RequirePkce`, `ConsentType` | OAuth 客户端 |
| ApiScope | `Name`, `DisplayName`, `Description`, `Required`, `Emphasize` | 作用域定义 |
| Authorization | `SubjectId`, `ClientId`, `Scope`, `Type`, `Status`, `CreationDate`, `ExpirationDate` | 授权记录 |
| Token | `ReferenceId`, `Type`, `Status`, `ExpirationDate`, `Payload` | Access/Refresh/Device Token |
| AuditLog | `Category`, `Event`, `SubjectId`, `Payload`, `CreatedTime` | 审计记录 |

### 关键 API 列表

- OAuth：`POST /connect/authorize`, `POST /connect/token`, `POST /connect/device`, `POST /connect/introspect`, `POST /connect/revoke`, `POST /connect/logout`
- 用户：`GET/POST/PUT/PATCH/DELETE /api/users`
- 角色：`GET/POST/PUT/DELETE /api/roles`, `POST /api/roles/{id}/permissions`
- 组织：`GET /api/organizations/tree`, `POST/PUT/DELETE /api/organizations`, `POST /api/organizations/{id}/users`
- 客户端：`GET/POST/PUT/DELETE /api/clients`, `POST /api/clients/{id}/secret:rotate`, `POST /api/clients/{id}/scopes`
- 作用域：`GET/POST/PUT/DELETE /api/scopes`
- 安全：`GET /api/security/sessions`, `POST /api/security/sessions/{id}/revoke`, `GET /api/security/logs`

---
本文件将作为后续 Issue 拆分与评审的参考基线，更新请保持与开发进度同步。
