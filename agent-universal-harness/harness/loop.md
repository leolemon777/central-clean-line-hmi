# 执行循环协议

每一轮 = Read -> Plan -> Act -> Verify -> Fix -> Review -> Report

## 1. Read

读取：

1. `AGENTS.md`
2. `tasks/current-task.md`
3. `reports/progress.md`
4. `harness/quality-gates.md`
5. 任务卡 Context 指定的文件
6. L2+ 或触发风险时：`harness/stop-rules.md`
7. 涉及外部工具 / MCP / API / 硬件 / 写操作时：`harness/tool-permissions.md`

输出：任务理解、假设、发现的疑问或矛盾。有疑问就停止并列出待决问题。

开工前必须检查 Gate 1。未满足时先报告缺口，不进入 Act。

## 2. Plan

输出计划：改哪些文件、为什么、预估风险、预计验证方式。

中高风险任务：计划输出后必须等待人类确认，未确认不得修改。

低风险任务：只有任务卡明确写“计划后无需等待确认”时，才可直接进入 Act。

## 3. Act

最小变更。一轮只推进一个可验证的小步。

禁止顺手重构、顺手改格式、顺手升级依赖。

## 4. Verify

优先运行任务卡 Verification 段指定的 quick 命令。

没有指定时，运行 `AGENTS.md` 中的统一快速验证命令。

报告必须包含真实命令、退出码、关键输出摘录；长日志可写入文件并给出路径。

## 5. Fix

verify 失败 -> 分析并修复，最多自动修复 3 轮。

第 3 轮仍失败 -> 触发停止规则 S1，输出失败分析报告：现象、已尝试、怀疑方向、需要人类决策的问题。

## 6. Review

按 `harness/review-checklist.md` 自审本轮 diff，列出发现的问题。

自审不能替代人类看 diff，但能提前拦下低级问题。

## 7. Report

固定格式：

```markdown
## Summary
## Changed Files
## Verification
- Command:
- Exit Code:
- Output Excerpt:
## Acceptance Check
## Risks
## Open Items
## Recommended Next Step
```

同时更新 `reports/progress.md`。

## 提交前（人类操作）

1. `git diff` 逐文件过一遍，看 diff，不只看 Summary。
2. 对照 `harness/review-checklist.md` 抽查。
3. 对照 `harness/quality-gates.md` 检查 Gate 2。
4. 高风险任务：另开会话按 `harness/prompt-template.md` 的审查模式跑 Reviewer。
5. 全量 verify 通过后再 commit。
