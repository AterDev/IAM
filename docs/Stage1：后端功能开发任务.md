# Stage1：后端功能开发任务

## 目标

本阶段聚焦 `IAM功能清单` 中“必要补充清单”的后端部分，目标不是一次做完所有高级能力，而是先把认证中心的**核心安全链路、用户闭环、联邦闭环、权限闭环**补齐到可继续扩展的程度。

本文档面向后续编码型 LLM 或工程师，要求：

- 不编造不存在的 API
- 优先复用当前 `IAMMod` 领域模型与 Manager 结构
- 保持与现有 Aspire / ApiService / Angular 管理端兼容

## 范围边界

本阶段后端优先处理以下事项：

1. 刷新令牌轮换与复用检测
2. 管理后台与 OAuth 会话模型统一所需的后端基础能力
3. 用户自助认证闭环 API
4. MFA 后端能力
5. 外部身份源联邦登录闭环
6. 敏感端点安全加固
7. 后台权限模型落地
8. 设备码与授权交互闭环的后端部分
9. 生产级配置治理
10. 统一注销与会话传播的后端能力

## 任务执行方式

为便于后续继续交给 LLM 或工程师直接编码，下面每个任务都建议按以下维度理解与拆解：

- **先后依赖**：说明该任务应在什么前置能力之后实施
- **建议拆分**：说明建议分几步落地，避免一次改动过大
- **预计改动文件**：列出高概率要改动的核心文件或目录
- **测试点**：列出最小可验证闭环，确保改动能验收

---

## 任务 1：刷新令牌轮换与复用检测

- 目标：

增强 `TokenManager` 中 refresh token 流程，做到：

- 每次刷新后轮换 refresh token
- 旧 refresh token 立即失效
- 发现已失效 token 再次使用时，判定为复用风险
- 对关联授权/会话做进一步处置（至少审计，必要时撤销整条授权链）

- 重点文件：

- `src/Modules/IAMMod/Managers/TokenManager.cs`
- `src/Definition/Entity/IAMMod/Token.cs`
- `src/Definition/Entity/IAMMod/Authorization.cs`

- 实现要点：

- 为 refresh token 建立明确的“父子轮换关系”或“替换标记”
- 区分以下状态：
  - valid
  - redeemed / rotated
  - revoked
  - compromised
- 一旦检测到旧 refresh token 被复用：
  - 写审计日志
  - 失效该授权链下未过期 refresh token
  - 视设计决定是否同时撤销当前会话

- 注意事项：

- 不要只“再签一个新 token”，必须把旧 token 的生命周期处理清楚
- 不要破坏现有密码模式、授权码模式、客户端凭证模式

- 先后依赖：

- 建议作为 Stage1 后端的第 1 个任务优先落地
- 需要先确认现有 `Token` / `Authorization` 状态模型可扩展

- 建议拆分：

1. 扩展 token 状态与轮换字段
2. 改造 refresh token 签发与旧 token 失效逻辑
3. 增加复用检测与审计
4. 补集成测试

- 预计改动文件：

- `src/Modules/IAMMod/Managers/TokenManager.cs`
- `src/Definition/Entity/IAMMod/Token.cs`
- `src/Definition/EntityFramework/**`
- `tests/Tests/**`

- 测试点：

- 正常刷新后拿到新的 refresh token，旧 token 失效
- 旧 refresh token 再次使用时触发复用风险逻辑
- 授权码、密码模式、客户端凭证模式不受影响

---

## 任务 2：统一会话模型的后端基础能力

- 目标：

为后续统一管理后台登录与 OAuth 登录打底，避免长期并存两套割裂认证体系。

- 现状问题：

- 管理后台使用 `api/admin/login` 返回自定义 JWT
- OAuth 授权页使用 Cookie + Razor Pages
- 会话记录与注销策略尚未统一

- 实现要点：

- 明确系统内的主认证会话模型：
  - 浏览器登录会话
  - API 调用访问令牌
  - 刷新令牌/授权链
- 补充统一会话标识（如 `sid`）在 cookie、token、session record 中的关联
- 为后续前端切换到统一认证模型预留后端接口与 claim 支撑

- 注意事项：

- Stage1 不要求一次重写整个登录体系
- 重点是“统一数据模型与会话语义”，不是先大规模改 UI

- 先后依赖：

- 建议在任务 1 完成后推进
- 与任务 10（统一注销）强相关，二者需要前后呼应

- 建议拆分：

1. 定义统一 session / sid 模型
2. 建立 cookie、token、session record 的关联关系
3. 为前端统一认证切换预留接口字段

- 预计改动文件：

- `src/Modules/IAMMod/Managers/SessionManager.cs`
- `src/Modules/IAMMod/ModuleExtensions.cs`
- `src/Services/ApiService/Controllers/IAMMod/AdminAuthController.cs`
- `src/Services/ApiService/Pages/Account/Login.cshtml.cs`

- 测试点：

- 后台登录与 OAuth 登录都能拿到统一 session 标识
- 登录会话记录与实际登录态一致
- 不引入现有登录回归问题

---

## 任务 3：补齐用户自助认证闭环 API

- 目标：

提供终端用户可用的基础认证自助接口。

- 建议优先级：

1. 忘记密码申请
2. 重置密码确认
3. 注册后邮箱验证 / 手机验证
4. 用户自助修改密码

- 重点文件：

- `src/Modules/IAMMod/Managers/UserManager.cs`
- `src/Services/ApiService/Controllers/IAMMod/*`
- 可新增专门的 Account / SelfService 控制器与 DTO

- 实现要点：

- 不要复用管理员接口直接给终端用户使用
- 重置密码令牌要有：
  - 单次使用
  - 明确过期时间
  - 明确用途
- 注册验证与重置密码流程都要落审计日志

- 注意事项：

- Stage1 可先把邮件/短信发送抽象为服务接口，允许开发环境使用日志/假发送器
- 不要在接口里返回敏感令牌明文给前端展示

- 先后依赖：

- 可与任务 2 并行推进，但建议先完成统一会话语义设计
- 与前端 Stage1 的注册/找回密码页面直接联动

- 建议拆分：

1. 定义 Account / SelfService DTO 与控制器
2. 实现忘记密码申请与重置密码确认接口
3. 实现邮箱/手机验证接口
4. 增加审计与开发环境假发送器

- 预计改动文件：

- `src/Modules/IAMMod/Managers/UserManager.cs`
- `src/Services/ApiService/Controllers/IAMMod/*`
- `src/Definition/Entity/IAMMod/UserToken.cs` 或新增相关实体
- `tests/Tests/**`

- 测试点：

- 找回密码申请成功 / 失败 / 令牌过期
- 重置密码后旧密码失效、新密码可登录
- 注册后验证状态可正确变更

---

## 任务 4：MFA 后端能力

- 目标：

至少完成第一版可用 MFA 基础设施，推荐优先 TOTP。

- 建议交付：

- 生成 TOTP 密钥
- 绑定/启用 MFA
- 校验 TOTP 验证码
- 关闭 MFA
- 恢复码生成与校验

- 实现要点：

- MFA 状态与用户表中 `IsTwoFactorEnabled` 保持一致
- MFA 验证通过后再签发完整登录结果
- 对高风险操作预留 step-up auth 扩展点

- 注意事项：

- 不要把共享密钥明文长期暴露到前端
- 恢复码只能展示一次，数据库中应保存哈希而非明文

- 先后依赖：

- 建议在任务 3 后推进
- 与统一会话模型和登录流程高度相关

- 建议拆分：

1. 建立 MFA 实体/配置模型
2. 实现 TOTP 绑定与验证接口
3. 实现恢复码生成与校验
4. 接入登录流程

- 预计改动文件：

- `src/Modules/IAMMod/Managers/UserManager.cs`
- `src/Services/ApiService/Controllers/IAMMod/*`
- `src/Definition/Entity/IAMMod/User.cs` 或新增 MFA 实体
- `tests/Tests/**`

- 测试点：

- 开启 MFA 后登录需二次验证
- 错误验证码不可登录
- 恢复码仅可使用一次

---

## 任务 5：外部身份源联邦登录闭环

- 目标：

把 `ExternalAuthController` 从“回调拿资料”补齐为“可真正登录系统”。

- 必须补齐的能力：

- 外部账号回调后识别唯一身份
- 已存在用户自动绑定 / 首次登录建档策略
- 邮箱冲突处理
- 关联本地用户与外部登录记录
- 登录完成后建立本地会话
- 审计日志记录

- 重点文件：

- `src/Services/ApiService/Controllers/IAMMod/ExternalAuthController.cs`
- `src/Definition/Entity/IAMMod/UserLogin.cs`
- `src/Modules/IAMMod/Managers/UserManager.cs`

- 注意事项：

- 不要默认“只要邮箱相同就自动绑定”，需要可配置策略
- 需要预留联邦注销和 claims 映射能力

- 先后依赖：

- 建议在任务 2 和任务 3 完成基础登录/用户闭环后实施
- 与任务 10 的统一注销能力有关联

- 建议拆分：

1. 设计外部登录映射与绑定策略
2. 实现首次登录建档 / 绑定 / 冲突分流
3. 建立本地会话与审计落地

- 预计改动文件：

- `src/Services/ApiService/Controllers/IAMMod/ExternalAuthController.cs`
- `src/Definition/Entity/IAMMod/UserLogin.cs`
- `src/Modules/IAMMod/Managers/UserManager.cs`
- `tests/Tests/**`

- 测试点：

- 外部登录首次建档成功
- 已绑定用户可重复登录
- 邮箱冲突时返回明确错误语义

---

## 任务 6：敏感端点安全加固

- 目标：

增强下列端点的客户端认证和访问控制：

- `/connect/introspect`
- `/connect/revoke`
- `/connect/token` 中的敏感 grant

- 实现要点：

- 自省和撤销端点要求客户端身份校验
- 区分公共客户端和机密客户端的能力边界
- 明确哪些客户端允许调用哪些端点
- 对失败请求写安全审计日志

- 注意事项：

- 不能让任意匿名请求自省或撤销第三方 token
- 错误响应要兼容 OAuth 规范语义

- 先后依赖：

- 建议在任务 1 完成后推进
- 与任务 7 的权限模型可以并行，但建议先做客户端级安全边界

- 建议拆分：

1. 定义 introspect / revoke 的客户端认证规则
2. 增加端点保护与错误语义
3. 增加失败审计与测试覆盖

- 预计改动文件：

- `src/Services/ApiService/Controllers/IAMMod/OAuthController.cs`
- `src/Modules/IAMMod/Managers/TokenManager.cs`
- `src/Modules/IAMMod/Managers/ClientManager.cs`
- `tests/Tests/**`

- 测试点：

- 合法客户端可自省 / 撤销合法 token
- 非法客户端或匿名请求被拒绝
- 错误响应保持 OAuth 兼容语义

---

## 任务 7：后台权限模型落地

- 目标：

把当前多个 `HasPermissionAsync` 的占位实现替换为可执行的后台授权策略。

- 实现要点：

- 明确角色、权限点、资源对象三者关系
- 至少覆盖：
  - 用户管理
  - 角色管理
  - 客户端管理
  - 作用域/资源管理
  - 安全中心
- 区分“列表权限、详情权限、编辑权限、删除权限、敏感操作权限”

- 注意事项：

- 不要仅靠前端菜单控制权限
- 服务端必须做最终授权判断

- 先后依赖：

- 建议在任务 2 后推进
- 与前端 Stage1 的权限感知任务需同步约定权限语义

- 建议拆分：

1. 梳理权限点与管理动作矩阵
2. 为关键 Manager 落地 `HasPermissionAsync`
3. 为接口补充统一权限校验与错误返回

- 预计改动文件：

- `src/Modules/IAMMod/Managers/*Manager.cs`
- `src/Services/ApiService/Controllers/IAMMod/*`
- `src/Definition/Entity/IAMMod/RoleClaim.cs`
- `tests/Tests/**`

- 测试点：

- 无权限用户无法访问敏感接口
- 有权限用户可正常完成操作
- 前后端权限语义一致

---

## 任务 8：设备码与授权交互闭环（后端部分）

- 目标：

完善设备码与授权确认场景的后端能力。

- 实现要点：

- 设备码申请后的用户确认接口
- 用户拒绝授权后的错误回写
- 授权页 consent 决策结果与授权记录关联
- 设备码轮询频率与超时控制

- 注意事项：

- 设备流必须考虑轮询节流
- 授权拒绝、超时、重复提交都要有清晰错误语义

- 先后依赖：

- 建议在任务 3 后推进
- 与前端 Stage1 的授权页 / 设备码页改造应同步设计

- 建议拆分：

1. 完成设备码确认接口
2. 完成授权拒绝与超时错误回写
3. 完成 consent 结果与授权记录绑定

- 预计改动文件：

- `src/Services/ApiService/Controllers/IAMMod/OAuthController.cs`
- `src/Modules/IAMMod/Managers/DeviceFlowManager.cs`
- `src/Modules/IAMMod/Managers/ConsentManager.cs`
- `tests/Tests/**`

- 测试点：

- device code 正常确认成功
- 过期 / 重复 / 拒绝场景返回准确错误
- 设备轮询频率控制生效

---

## 任务 9：生产级配置治理

- 目标：

清理当前对生产环境不安全或不稳定的默认行为。

- 实现要点：

- 收敛 CORS
- 强制配置 Issuer
- 敏感连接串与外部身份源密钥使用环境变量/安全存储
- 区分开发模式与生产模式配置

- 注意事项：

- 不要让 Discovery 在生产依赖 `Request.Host` 推导 issuer
- 不要保留 `AllowAnyOrigin` 作为生产默认值

- 先后依赖：

- 建议在核心认证流程稳定后统一处理
- 与部署文档和运行配置同步修改

- 建议拆分：

1. 梳理开发 / 测试 / 生产配置差异
2. 收紧 CORS 与 issuer 配置
3. 敏感配置迁移到环境变量或安全存储

- 预计改动文件：

- `src/Modules/IAMMod/ModuleExtensions.cs`
- `src/Services/ApiService/Controllers/IAMMod/DiscoveryController.cs`
- `src/AppHost/appsettings*.json`
- `README.md` / 部署相关文档

- 测试点：

- 开发环境仍可运行
- 生产配置缺失时能给出明确错误
- 非法来源跨域请求被拒绝

---

## 任务 10：统一注销与会话传播（后端部分）

- 目标：

为集中式登录中心建立一致的注销语义。

- 实现要点：

- 本地 cookie 会话注销
- 访问令牌 / 刷新令牌撤销联动
- 登录会话表状态同步
- 为前端和其他客户端提供统一退出结果

- 注意事项：

- 注销不应只清 cookie，而忽略 refresh token 与 session record
- 需要兼顾浏览器交互场景与 API 场景

- 先后依赖：

- 强依赖任务 2 的统一会话模型设计
- 最好在任务 1 完成 token 生命周期治理后推进

- 建议拆分：

1. 定义统一注销语义与联动范围
2. 实现 cookie、refresh token、session record 联动失效
3. 增加审计与接口测试

- 预计改动文件：

- `src/Services/ApiService/Controllers/IAMMod/OAuthController.cs`
- `src/Modules/IAMMod/Managers/SessionManager.cs`
- `src/Modules/IAMMod/Managers/TokenManager.cs`
- `tests/Tests/**`

- 测试点：

- 浏览器登出后本地会话失效
- refresh token 无法继续换发新 token
- 会话记录与实际状态一致

---

## 交付要求

本阶段后端任务完成后，应至少满足：

- 示例登录链路不回退
- 现有 OAuth 核心端点保持兼容
- 新增能力具备最基本的审计记录
- 关键安全路径有集成测试或端到端验证

## 建议验证顺序

1. Token 刷新与复用检测
2. 自助找回密码 / 重置密码
3. MFA 启停与验证
4. 外部登录回调闭环
5. 自省/撤销安全校验
6. 权限控制与统一注销
