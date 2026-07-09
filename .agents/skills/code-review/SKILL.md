---
name: code-review
description: >-
  代码审查技能：用于基于项目规范和事实对代码进行审查，识别质量问题、性能瓶颈、安全风险和架构一致性问题，并提供改进建议。Use when: any code review task, quality gate, risk identification, PR review.
---

## 审查范围

- 功能实现逻辑
- 全栈代码质量
- 性能瓶颈
- 安全风险
- 架构一致性

## 核心要求

- 基于项目规范和事实审查，不凭个人偏好泛审。
- 审查时优先关注本次变更及其直接影响范围。
- 如有必要，先执行最小必要的构建或测试验证。
- 输出结论时区分阻断问题与改进建议。
- 不直接修复代码，重点是指出问题、风险和建议。

## 必读 skill

- `.agents/skills/perigon/SKILL.md`
- `.agents/skills/aspire/SKILL.md`
- `.agents/skills/dotnet-guidelines/SKILL.md`
- `.agents/skills/angular-guidelines/SKILL.md`

## 审查清单

- 架构分层是否合理，Controller 是否越界访问 DbContext。
- 业务逻辑是否位于 Manager 或等价业务层，而不是 HTTP 边界层。
- 是否存在 N+1、无分页全量查询、过度 `Include` 或缺失必要索引。
- 输入验证、权限检查、敏感信息处理是否充分。
- 前端是否遵循 standalone、signals、Typed Reactive Forms、i18n 等约定。
- 命名、重复代码、复杂度和必要注释是否可维护。

## 输出格式

```text
REVIEW_STATUS: PASS | FAIL
NEXT_ACTION: NONE | ENGINEER_FIX_REQUIRED

审查范围:
- ...

阻断问题:
1. 文件: path/to/file
   问题: ...
   风险: ...
   建议: ...

验证结果:
- build: passed | failed | not-run
- tests: passed | failed | not-run
```
