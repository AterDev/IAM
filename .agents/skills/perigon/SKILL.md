---
name: perigon
description: >-
  Perigon 主入口技能：用于基于 Perigon CLI/MCP 进行项目脚手架、模块与服务生成、DTO/Manager/Controller/Request 客户端生成、MCP 配置与 Studio 操作，以及日常后端/前端开发中的代码生成与模板化工作。Use when: any Perigon-based development task, scaffolding, code generation, module/service creation, API client generation, MCP setup, Studio usage, backend/frontend implementation that should follow Perigon conventions.
---

# Perigon Skill

This repository uses `Perigon.CLI` for scaffolding, code generation, and MCP integration.
This is now the single top-level Perigon skill. Framework-specific guidance for Angular, backend, testing, and code review is kept as project-local references under this folder.

## When to use this skill

Use this skill for nearly all Perigon-related development work, especially when the task involves:
- 创建新解决方案、模块、服务，或启动 Studio；
- 生成或更新 DTO、Manager、Controller、Entity、Request Client、前端请求模型；
- 按照 Perigon 约定完成后端/前端代码骨架与业务代码生成；
- 安装、列出、打包模块包，或初始化/启动 MCP 服务；
- 需要先查看 CLI 帮助再执行命令，避免猜测参数；
- 任何涉及“优先使用 Perigon 生成而不是手写模板代码”的开发任务。

In short: if the work is about implementing or extending a Perigon-based project, this skill should be the default entry point.

## Project structure

```sh
src/
├── Perigon/                 # 基础类库、工具扩展与源生成项目
├── Definition/
│   ├── Entity/              # 实体定义（按模块分文件夹）
│   ├── EntityFramework/     # Perigon.PostgreSQL DbContext 与迁移
│   ├── Share/               # 共享常量、扩展、服务
│   └── ServiceDefaults/     # 服务注册与中间件
├── Modules/
│   └── {ModuleName}/
│       ├── Managers/        # 业务逻辑层
│       ├── Models/          # DTO 定义
│       └── Services/        # 模块内服务（可选）
└── Services/
    ├── ApiService/          # 公共 API
    ├── AdminService/        # 管理后台 API
    └── MigrationService/    # 数据库迁移服务
```

## Reference routing

| Task area | Reference |
|---|---|
| Perigon CLI / MCP / Studio commands | [references/perigon-cli.md](references/perigon-cli.md) |
| Angular frontend development | [references/angular.md](references/angular.md) |
| Backend architecture and service patterns | [references/backend.md](references/backend.md) |

## Project scripts

Run project scripts when needed ,Use PowerShell 7 (`pwsh`) when available.

Treat these scripts as part of the template workflow; do not reimplement their behavior manually.

| Script | Purpose and when to run | Command |
|---|---|---|
| `scripts/UpdateMenus.ps1` | Synchronize `src/ClientApp/WebApp/src/assets/menus.json` into the Admin service database. Run after changing frontend menu items, hierarchy, access codes, or menu types; ensure the target Admin service is running first. The default target is `http://localhost:5002`; pass `production` only after confirming the script's production URL is correct. | `pwsh ./scripts/UpdateMenus.ps1`<br>`pwsh ./scripts/UpdateMenus.ps1 production` |
| `scripts/Migrations.ps1` | Generate, list or script PostgreSQL migrations. | `pwsh ./scripts/Migrations.ps1 Add AddOrderStatus` |
| `scripts/GenSwagger.ps1` | Export OpenAPI from the running service before regenerating clients. | `pwsh ./scripts/GenSwagger.ps1 -ServiceName ApiService -BaseUri https://localhost:9900` |


Run `dotnet tool restore` before generating migrations. Review the generated migration and OpenAPI document.

## Important rules

- **Use Perigon for scaffolding and code generation**, not for build/test/run.
- **Prefer generated code over hand-written boilerplate** for modules, services, DTOs, managers, controllers, and request clients.
- **For frontend API clients, prefer `perigon generate request`** and keep generated contracts consistent with backend OpenAPI definitions.
- **Do not manually edit generated request contracts unless necessary**; regenerate when the backend changes.
- **Check help before guessing options** with `perigon -h` or `perigon <command> -h`.
- **Prefer the current subcommand form** (`perigon module ...`) over older shorthand aliases when the help output is ambiguous.
- **Enforce the DTO layout and naming rule**: module DTOs belong under `Models/{Entity}Dtos`, one type per file, and every data-transfer type ends in `Dto`; use `{Entity}AddDto`, `{Entity}UpdateDto`, `{Entity}FilterDto`, `{Entity}DetailDto`, and `{Entity}ItemDto` where applicable. Do not introduce `Input`, `Request`, or `Response` DTO names.
