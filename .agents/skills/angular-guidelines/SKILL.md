---
name: "Angular Guidelines"
description: "Use when editing Angular files: component, template, route, form, Material UI, signals, i18n, generated request client."
applyTo: ["src/ClientApp/WebApp/**/*.ts", "src/ClientApp/WebApp/**/*.html", "src/ClientApp/WebApp/**/*.scss", "src/ClientApp/WebApp/**/*.json"]
---

# Angular Guidelines

- 使用 Angular 21+ standalone 组件，不使用 NgModule。
- UI 优先使用 Angular Material；布局优先复用现有样式和 Bootstrap grid，避免内联样式。
- 状态优先使用 signals、async pipe 或框架推荐响应式写法；模板中避免调用复杂函数。
- 模板控制流使用 `@if`、`@for`、`@switch`，`@for` 必须包含 `track`。
- 表单使用 Typed Reactive Forms；在 FormGroup 内优先用 `[formControl]` + getter，不优先使用 `formControlName`。
- 文本使用 `i18nKeys` 和 `translate`，不要硬编码用户可见字符串。
- 不手动修改 Perigon 生成的请求服务和 DTO；后端 OpenAPI 变化后重新生成。
- 前端验证在 `src/ClientApp/WebApp` 下执行 `pnpm build`。
