# PD0001-EntityAttributes

## 来源

- 用户请求：删除实体上类似 `[Perigon.PostgreSQL.Attributes.Column]` 的标签；这些标签无效且导致编译错误。
- 背景：`docs/Development/V1/重构说明.md` 已要求去除实体上的无效映射特性，并按 `ApiService` 构建结果修正。

## 需求

- REQ-001：`src/Definition/Entity` 下的实体不得再引用 `Perigon.PostgreSQL.Attributes` 命名空间。
- SC-001：构建时不再出现 `CS0234`（`ColumnAttribute`、`IndexAttribute` 不存在）。
- SC-002：实体项目和 `EntityFramework` 项目均可独立构建通过。

## 设计影响

- 删除的 `Column(TypeName = ...)` 提供字符串列长度；删除的 `Index(...)` 提供唯一/普通索引声明。schema 若已部署，需另行评估迁移与约束变化；本次不生成迁移。
- 不新增 DTO、反射、动态代码或包依赖；但实体进入了 Native AOT 发布闭包，按项目规则需在整体构建可用后执行 AOT 发布验证。

## 待确认项

- 无。剩余构建错误来自工作区已有的 `Perigon.AspNetCore.Toolkit` 包引用调整，与本需求无关。
