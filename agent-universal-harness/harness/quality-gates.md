# Quality Gates

> Gate 是硬闸门，不是建议。低风险项目可以少，但不能跳过 L0 的验证闸门。

## Gate 0 - Bootstrap

进入业务实现前必须满足：

- [ ] `scripts/verify.*` 已替换为真实命令，或明确写了当前层级的 skip 理由。
- [ ] `./scripts/check-harness.*` 没有 ERROR。
- [ ] `AGENTS.md` 项目速览已填写。
- [ ] 真实风险点已写入 `PROJECT.md` 或任务卡。

## Gate 1 - Task Start

Agent 开工前必须满足：

- [ ] `tasks/current-task.md` 有 Goal。
- [ ] In Scope / Out of Scope 都不为空。
- [ ] Acceptance Criteria 可被 verify 或人工步骤检查。
- [ ] Quick / Full Verification 写清。
- [ ] Risk Level 和计划确认策略写清。
- [ ] Context 只列必要文件。

## Gate 2 - Before Commit

提交前必须满足：

- [ ] diff 只含任务相关变更。
- [ ] quick verify 已通过或失败被停止规则处理。
- [ ] full verify 已通过，或失败原因和风险被明确接受。
- [ ] `reports/progress.md` 已更新。
- [ ] Review checklist 没有 Blocking。

## Gate 3 - Before Release / External Effect

发布、真实硬件动作、生产数据写入、资金操作、对外写 API 前必须满足：

- [ ] L3 文档或等价流程已启用。
- [ ] dry-run / simulation / sandbox 结果符合预期。
- [ ] 回滚、补偿或急停方案明确。
- [ ] 人类确认记录写在任务卡或发布记录中。
- [ ] 相关日志、指标、告警可观测。

## Gate Failure Report

```markdown
## Gate Failed
- Gate:
- Failed Checks:
- Evidence:
- Risk:
- Required Human Decision:
```
