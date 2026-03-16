# Stage2：后端功能增强开发任务

## 目标

本阶段聚焦 `IAM功能清单` 中“推荐补充清单”的后端部分，目标是在 Stage1 完成核心安全闭环后，把系统从“可用的认证中心基础版”增强为“具备平台化、治理化、扩展性更强的 IAM 平台”。

本文档面向后续编码型 LLM 或工程师，要求：

- 仅在现有架构基础上增强，不推翻 Stage1 已完成能力
- 优先复用 `IAMMod`、`Entity`、`EntityFramework`、`ApiService` 现有分层结构
- 强调扩展点、配置化与平台化，而不是一次性硬编码实现
- 所有增强能力都要考虑安全、审计、运维与兼容性

## 范围边界

本阶段后端优先处理以下事项：

1. Passkey / WebAuthn 基础设施
2. 风险控制与防滥用能力
3. 动态客户端注册与开发者门户后端能力
4. SCIM / 外部组织与账号同步能力
5. 多租户隔离能力
6. 高级 OAuth 安全规范增强
7. 审计报表与导出分析能力
8. 运营监控与安全告警能力
9. Claims 映射与身份资料治理
10. Password Grant 收敛与替代策略

## 任务执行方式

Stage2 任务更偏平台增强，建议用“增量可交付”方式推进，而不是一次性大改。每个任务都建议至少看四个维度：

- **先后依赖**：确认是否依赖 Stage1 能力或其他 Stage2 任务
- **建议拆分**：控制单个 PR 或单轮 LLM 编码的范围
- **预计改动文件**：提前锁定主要改动面
- **测试点**：明确最小验收闭环与回归风险

---

## 任务 1：Passkey / WebAuthn 后端基础设施

- 目标：

建立无密码认证与强认证增强能力，优先支持 WebAuthn / Passkey 的注册与验证基础流程。

- 重点文件：

- `src/Modules/IAMMod/Managers/UserManager.cs`
- `src/Services/ApiService/Controllers/IAMMod/*`
- 可新增 WebAuthn 相关实体、DTO、Manager 与控制器

- 实现要点：

- 提供注册 challenge 生成接口
- 提供注册结果验证接口
- 提供登录 challenge 生成接口
- 提供登录断言验证接口
- 持久化凭证 ID、公钥、sign count、设备元数据等
- 与现有 MFA / 登录审计体系打通

- 注意事项：

- 不要手写不完整的 WebAuthn 验证逻辑，优先使用成熟库
- 凭证验证必须校验 origin、rpId、challenge、counter
- 要考虑多设备绑定、凭证吊销与设备管理能力

- 先后依赖：

- 建议在 Stage1 的统一登录 / MFA 基础稳定后实施
- 与前端 Stage2 的 Passkey 页面同步设计

- 建议拆分：

1. 注册 challenge / 验证接口
2. 登录 challenge / assertion 验证接口
3. 凭证管理与吊销接口

- 预计改动文件：

- `src/Services/ApiService/Controllers/IAMMod/*`
- `src/Modules/IAMMod/Managers/*`
- `src/Definition/Entity/IAMMod/*`
- `tests/Tests/**`

- 测试点：

- Passkey 注册成功
- Passkey 登录成功 / 用户取消 / challenge 失效
- 删除凭证后不可继续使用

---

## 任务 2：风险控制与防滥用能力

- 目标：

为认证中心增加基础风控能力，降低撞库、暴力破解、异常授权请求与恶意轮询风险。

- 实现要点：

- 登录、token、device、external callback 等关键接口限流
- 对失败登录、异常 IP、异常 user-agent 做统计与拦截策略
- 为验证码、人机校验或二次挑战预留接入点
- 对高风险登录或敏感变更触发 step-up auth
- 为设备流轮询增加频率保护与告警

- 注意事项：

- 风控不应与业务逻辑强耦合，建议抽成独立服务或中间层
- 规则需支持配置化，避免把阈值写死在代码中
- 所有拦截行为要落审计日志

- 先后依赖：

- 建议作为 Stage2 后端的优先任务之一
- 与运营监控、告警能力天然联动

- 建议拆分：

1. 限流与基础风控规则
2. 异常事件统计与审计
3. step-up / challenge 扩展点

- 预计改动文件：

- `src/Modules/IAMMod/**`
- `src/Services/ApiService/**`
- `src/Definition/ServiceDefaults/**`
- `tests/Tests/**`

- 测试点：

- 高频登录失败触发限制
- 设备流异常轮询被限流
- 风控触发后可被审计和告警消费

---

## 任务 3：动态客户端注册与开发者门户后端能力

- 目标：

让平台具备更强的自助接入能力，减少人工在后台创建客户端的成本。

- 实现要点：

- 支持注册新的 OAuth/OIDC 客户端
- 支持客户端元数据校验与默认策略填充
- 支持客户端 secret 发放、轮换、吊销
- 支持开发者视角查看回调地址、scope、resource、secret 状态
- 为审批流或管理员审核预留状态字段

- 重点文件：

- `src/Modules/IAMMod/Managers/ClientManager.cs`
- `src/Definition/Entity/IAMMod/Client.cs`

- 注意事项：

- 不能让任意匿名用户创建客户端
- Public client 与 Confidential client 的能力边界必须明确
- 动态注册接口要考虑幂等与审核状态

- 先后依赖：

- 建议在客户端管理模型稳定后推进
- 与前端 Stage2 的开发者门户页面强相关

- 建议拆分：

1. 注册申请接口
2. 审核 / 状态流转接口
3. secret 轮换与查看接口

- 预计改动文件：

- `src/Modules/IAMMod/Managers/ClientManager.cs`
- `src/Services/ApiService/Controllers/IAMMod/ClientsController.cs`
- `src/Definition/Entity/IAMMod/Client.cs`
- `tests/Tests/**`

- 测试点：

- 新客户端申请成功
- 审核状态影响客户端可用性
- secret 轮换后旧 secret 失效

---

## 任务 4：SCIM / 外部组织与账号同步能力

- 目标：

支持从企业目录、人事系统或第三方身份源批量同步用户、组织、组与角色映射。

- 实现要点：

- 设计用户、组织、外部目录对象的映射关系
- 支持 upsert 同步策略
- 支持删除、禁用、离职状态同步
- 支持全量同步与增量同步
- 支持同步冲突日志与回滚策略

- 注意事项：

- 不要直接覆盖本地人工维护字段，需明确主数据归属
- 同步过程必须支持审计与失败重试
- 用户唯一标识应优先使用稳定外部 ID，而不是仅依赖邮箱

- 先后依赖：

- 建议在组织、用户模型稳定后实施
- 与多租户能力存在交叉，需先明确租户边界

- 建议拆分：

1. 外部目录对象映射模型
2. 同步任务与状态管理
3. 冲突处理与重试机制

- 预计改动文件：

- `src/Modules/IAMMod/Managers/*`
- `src/Definition/Entity/IAMMod/*`
- 可新增同步任务实体与服务
- `tests/Tests/**`

- 测试点：

- 全量同步成功
- 增量同步更新正确
- 冲突对象能被记录和重试

---

## 任务 5：多租户隔离能力

- 目标：

在现有基础上增强租户隔离模型，使系统可支撑多个组织或业务域的身份治理。

- 实现要点：

- 明确租户实体与租户上下文传递方式
- 用户、客户端、作用域、资源、角色、组织等对象增加租户边界
- 数据查询默认按租户过滤
- 支持平台租户与普通租户的管理边界
- 为 token / issuer / audience 中的租户语义预留设计

- 注意事项：

- 多租户不能只做前端分组，必须落实到服务端与数据层
- 需要定义共享资源与租户专属资源的边界
- 迁移时要兼顾现有单租户数据兼容性

- 先后依赖：

- 建议在 Stage1 权限模型与配置治理稳定后推进
- 与 SCIM / 同步能力、Claims 治理能力关联较强

- 建议拆分：

1. 租户实体与上下文模型
2. 数据层默认租户过滤
3. 平台级与租户级权限边界

- 预计改动文件：

- `src/Definition/Entity/**`
- `src/Definition/EntityFramework/**`
- `src/Modules/IAMMod/**`
- `tests/Tests/**`

- 测试点：

- 不同租户数据互不可见
- 平台管理员与租户管理员权限不同
- 旧单租户数据迁移后仍可使用

---

## 任务 6：高级 OAuth 安全规范增强

- 目标：

增强系统对更高安全等级 OAuth 场景的支持能力。

- 建议优先级：

1. PAR（Pushed Authorization Requests）
2. DPoP
3. Token Exchange
4. JAR / JARM
5. RAR / FAPI 相关能力

- 实现要点：

- 为授权请求对象增加安全封装能力
- 对受保护 API 场景引入 proof-of-possession 思路
- 为服务间令牌交换建立授权模型
- 提前抽象授权请求、token 绑定与能力声明结构

- 注意事项：

- 不建议 Stage2 一次把所有规范一起落地
- 每一项增强都要先明确适用场景与客户端类型
- 需要确保不破坏现有标准授权码流程

- 先后依赖：

- 建议在 Stage1 核心 OAuth 流程稳定后实施
- 与动态客户端注册、Claims 治理存在配置层联动

- 建议拆分：

1. 先选 1 项高价值能力（推荐 PAR）
2. 完成模型抽象与配置开关
3. 再逐项叠加 DPoP / Token Exchange 等

- 预计改动文件：

- `src/Services/ApiService/Controllers/IAMMod/OAuthController.cs`
- `src/Modules/IAMMod/Managers/*`
- `src/Definition/Entity/IAMMod/*`
- `tests/Tests/**`

- 测试点：

- 新能力启用时请求流程正确
- 未启用客户端不会误用高级能力
- 标准授权码流程不回归

---

## 任务 7：审计报表与导出分析能力

- 目标：

让现有审计日志从“能查”升级为“能分析、能导出、能支撑合规”。

- 实现要点：

- 提供登录报表、授权报表、失败事件报表、安全事件报表
- 支持按时间范围、用户、客户端、事件类型导出
- 支持异步导出与任务状态查询
- 支持 CSV / Excel / JSON 等输出格式
- 为聚合统计建立查询优化策略

- 注意事项：

- 导出任务不能阻塞主线程接口
- 大数据量场景需考虑分页、缓存、后台任务或临时文件策略
- 导出数据需要权限控制与脱敏策略

- 先后依赖：

- 建议在 Stage1 审计日志稳定后推进
- 与前端 Stage2 的报表与导出页同步设计导出任务模型

- 建议拆分：

1. 聚合查询接口
2. 导出任务接口
3. 下载与权限校验逻辑

- 预计改动文件：

- `src/Modules/IAMMod/Managers/AuditLogManager.cs`
- `src/Services/ApiService/Controllers/IAMMod/SecurityController.cs`
- 可新增导出任务实体 / 服务
- `tests/Tests/**`

- 测试点：

- 报表聚合数据正确
- 导出任务可创建、查询、下载
- 大数据量导出不会阻塞主接口

---

## 任务 8：运营监控与安全告警能力

- 目标：

让系统具备基础运营观测与安全告警能力，方便日常维护与风险响应。

- 实现要点：

- 监控签名密钥过期时间
- 监控客户端 secret 即将过期状态
- 监控失败登录峰值、异常 token 请求、设备流异常轮询
- 提供告警规则与告警记录模型
- 预留邮件、Webhook、消息通知等告警通道

- 注意事项：

- 告警规则应配置化
- 告警不能直接依赖前端轮询，后端需具备主动触发机制
- 告警触发与处理过程应可审计

- 先后依赖：

- 建议与风险控制、报表能力串联实施
- 依赖基础指标、日志或事件模型可被消费

- 建议拆分：

1. 定义告警规则与告警实体
2. 增加关键事件监控逻辑
3. 增加通知与已处理状态流转

- 预计改动文件：

- `src/Modules/IAMMod/**`
- `src/Definition/Entity/IAMMod/*`
- 可新增告警服务与后台任务
- `tests/Tests/**`

- 测试点：

- 告警阈值达到时可生成告警记录
- 告警状态可标记为已处理
- 不会因告警逻辑拖慢主认证链路

---

## 任务 9：Claims 映射与身份资料治理

- 目标：

增强系统对 claims 下发、字段映射、最小化披露与客户端差异化身份数据的治理能力。

- 实现要点：

- 建立用户属性、标准 claim、自定义 claim 的映射模型
- 支持按 scope / client / resource 决定 claim 下发策略
- 支持敏感字段分级与脱敏策略
- 支持 UserInfo 与 ID Token / Access Token 的差异化输出
- 预留多语言资料字段与扩展 profile 字段支持

- 注意事项：

- 不要把所有用户属性都塞进 token
- claim 下发规则必须可审计、可配置、可测试
- 要兼容 OIDC 标准 claim 与本项目自定义 claim

- 先后依赖：

- 建议在多租户 / 客户端模型较稳定后推进
- 与前端 Stage2 的 Claims 配置页面直接联动

- 建议拆分：

1. 建立 claim 映射模型
2. 为 token / userinfo 增加差异化下发逻辑
3. 增加预览与审计支持

- 预计改动文件：

- `src/Modules/IAMMod/Managers/DiscoveryManager.cs`
- `src/Modules/IAMMod/Managers/TokenManager.cs`
- `src/Definition/Entity/IAMMod/*`
- `tests/Tests/**`

- 测试点：

- 不同 client / scope 获得不同 claims
- UserInfo 与 token 输出规则可区分
- 敏感字段不会被错误下发

---

## 任务 10：Password Grant 收敛与替代策略

- 目标：

逐步收敛现有 Password Grant 的使用范围，降低长期安全风险。

- 实现要点：

- 梳理当前哪些客户端仍依赖 password grant
- 增加客户端级开关，限制仅允许特定受控客户端使用
- 为替代方案提供迁移路径：
  - 授权码 + PKCE
  - 设备码
  - 客户端凭证
  - WebAuthn / MFA 增强登录
- 在日志和后台中标记仍使用 password grant 的客户端

- 注意事项：

- Stage2 不要求立即移除 password grant，但要先完成风险收敛
- 任何收敛策略都要兼顾现有集成兼容性
- 需要在文档中明确弃用计划与迁移说明

- 先后依赖：

- 建议在风控、客户端治理、报表能力初步落地后推进
- 与前端 Stage2 的迁移辅助页面联动

- 建议拆分：

1. 统计现状与识别依赖客户端
2. 增加客户端级限制与审计标记
3. 输出迁移方案与弃用节奏

- 预计改动文件：

- `src/Modules/IAMMod/Managers/TokenManager.cs`
- `src/Modules/IAMMod/Managers/ClientManager.cs`
- `src/Services/ApiService/Controllers/IAMMod/OAuthController.cs`
- `tests/Tests/**`

- 测试点：

- 被限制客户端无法再使用 password grant
- 白名单客户端仍可临时兼容
- 后台可看到仍在使用 password grant 的客户端清单

---

## 交付要求

本阶段后端任务完成后，应至少满足：

- 具备面向平台化扩展的后端结构
- 增强能力支持配置化和审计追踪
- 不破坏 Stage1 已完成的核心认证与授权链路
- 至少对新增关键能力提供集成测试或契约验证

## 建议验证顺序

1. 风控与限流基础能力
2. Claims 治理与审计增强
3. Password Grant 收敛策略
4. 动态客户端注册
5. WebAuthn 基础流程
6. 多租户与同步能力
7. 高级 OAuth 安全规范增强
