# PD0002-TestProjectRemoval

## 来源

- 用户请求：移除仓库 `tests` 下的测试项目，后续重构重写。

## 需求

- REQ-001：删除 `tests/Tests` 测试项目及其全部源文件和构建缓存。
- REQ-002：`IAM.slnx` 与仓库结构说明不再引用旧测试项目。
- SC-001：`tests` 目录中无 `*.csproj`，解决方案中无 `tests/Tests/Tests.csproj`。
- SC-002：解决方案构建不再因旧测试项目产生错误。

## 设计影响

- 不改动生产代码、DTO/JSON 契约、endpoint 或依赖版本；旧测试项目不在发布闭包内，Native AOT/Trim 不受影响。
- `Directory.Packages.props` 中 TUnit/Aspire.Hosting.Testing 等测试包由用户同期添加，保留供后续重写使用；xunit/Moq 版本已由用户同期移除，本次不再处理。
- `docs/Development/V1/Stage1：后端功能开发任务.md`、`Stage2：后端功能增强开发任务.md` 中 `tests/Tests/**` 属于历史任务验收记录，保留原文，不作为当前目录约定。

## 待确认项

- 后续重写测试项目时，需按新的 TUnit/Aspire 方案重新建立入口与验证矩阵。
