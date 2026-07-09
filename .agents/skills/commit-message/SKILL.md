---
description: "Generate a concise git commit message that follows Conventional Commits and fits git-flow branch context."
name: "Commit Message"
argument-hint: "Describe the intent or constraints for this commit"
agent: "agent"
model: [MAI-Code-1-Flash (copilot), DeepSeek V4 Flash (deepseek), Raptor mini (Preview) (copilot)]
tools: [execute]
---

Generate a git commit message for the current repository changes.

First inspect the current git diff or staged diff, then combine it with any user-provided intent. and update the develop process.

**前提条件**:

- 检查本次提交内容是否存在机密信息泄露风险，如密码、token、敏感配置等。如存在，则给出警告，不再生成提交信息。
- 如果包含大量测试或生成内容，或者体积很大的文件，给出警告，让用户确认是否需要添加到忽略列表。

<workflow>

1. 检查前提条件
2. 按 <output> 格式生成提交信息
3. update the develop process

</workflow>

<rules>

1. Use Conventional Commits format:
   - `type(scope): subject`
2. `type` must be one of:
   - `feat` `fix` `docs` `refactor` `test` `chore`
3. Infer `scope` from the main module, such as:
   - `api`, `admin`, `webapp`, `entity`, `ef`, `modules`, `apphost`, `aspire`, `perigon`, `docs`, `test`, `templates`
4. If one commit contains several **related** changes, choose the primary type for the header and use a short body to summarize secondary changes.

</rules>

<output>

`type(scope): subject`
   - item1
   - item2
</output>

**update the develop process**
- update `docs/Development/ProjectTracking.md` for tracing the current task progress.
- update `docs/Development/Changelog.md` for recording the change details and impact.
