---
name: ".NET Guidelines"
description: "Use when editing C#/.NET files: ASP.NET Core, EF Core, Controller, Manager, DTO, Entity, DbContext, migration, tests, csproj."
applyTo: "**/*.cs"
---

## .NET Guidelines

- 使用文件作用域命名空间、主构造函数、集合表达式和可空引用类型。
- Controller 只处理 HTTP 边界、权限和输入验证；业务逻辑放在 Manager；不要在 Controller 中直接访问 DbContext。
- 业务验证错误使用 `BusinessException`；API 错误优先使用 `Problem()` 或 `NotFound()`。
- 异步方法优先传递 `CancellationToken`，避免同步阻塞和不必要的内存分配。
- 优先复用 `src/Perigon` 提供的扩展和工具，例如 `Merge`、`MapTo`；多语言文本使用 `Localizer` 常量。
- 修改实体或 DbContext 后，通过 `scripts/EFMigrations.ps1` 生成迁移；不要手动编写或修改迁移文件。
- 优先保持一个类型一个文件；对于小型枚举，可以在同一文件中定义；对于实体和 DTO，优先使用 class 而非 record。
- 测试代码可适当使用 `record` 来简化数据结构定义，提升可读性和维护性。
