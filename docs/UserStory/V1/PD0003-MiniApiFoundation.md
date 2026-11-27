# PD0003 MiniApi 基础架构同步

- REQ-001：使用 MiniApi 当前基础依赖和生成器契约恢复 IAM 解决方案的构建。
- REQ-002：AppHost 使用 Aspire 17 的路径式服务注册，保留迁移、认证颁发者、缓存和示例资源。
- REQ-003：管理前端和 OIDC 示例对齐模板的 Angular 和包管理器基线，保持已有路由与交互。
- SC-001：解决方案、管理前端和示例前端可构建，ApiService 原生发布产物可以启动并生成 OpenAPI 文档。

IAM 继续拥有认证、会话、缓存序列化和数据库迁移的应用特定逻辑。OpenAPI 改为通过构建期生成的 XML 描述整数枚举，保留 IAM 的全限定 schema ID。
