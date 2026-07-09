# Subagent Protocol

> 默认单 Agent。只有审查跟不上、任务天然可并行、或风险需要独立复核时才启用。

## 推荐角色

| Subagent | 责任 | 输出 |
|----------|------|------|
| Planner | 拆任务、识别风险 | 计划和任务卡 |
| Implementer | 小步写代码 | diff 和说明 |
| Tester | 补测试、跑验证 | 测试结果和缺口 |
| Reviewer | 审查质量、架构、安全 | review 报告 |
| Documenter | 更新文档、ADR、handoff | 文档 diff |
| Security Reviewer | 权限、密钥、输入验证 | 安全风险清单 |

## 使用规则

- 每个 subagent 必须有明确输入和输出。
- Reviewer 默认只审查，不修改代码。
- 多 Agent 并行时必须使用独立分支或 worktree，避免互相覆盖。
- 主会话负责整合结论，不能让多个 subagent 同时改同一文件。

## Reviewer Prompt

```text
请作为 Reviewer 审查当前 diff。
只审查，不修改。
按 Correctness、Architecture、Tests、Security、Maintainability、Scope Creep 输出。
每个问题必须给出文件位置、严重级别和建议修复。
```
