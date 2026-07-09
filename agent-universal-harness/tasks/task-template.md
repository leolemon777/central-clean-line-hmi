# Task-XXX：<标题>

## Metadata

- Task ID：Task-XXX
- Branch：task/<name>
- Risk Level：低 / 中 / 高
- Layer：L0 / L1 / L2 / L3
- Base Branch / Commit：<填写>

## Goal（一句话）

<本任务完成后，什么从“不能”变成“能”>

## Context（读哪些文件，为什么）

> 精准指定，不要写“熟悉整个项目”。参考 `docs/context-index.md`。

-

## In Scope

-

## Out of Scope

> 明确写出容易被顺手做掉的相邻事项。

-

## Constraints（技术约束与禁止事项）

-

## Acceptance Criteria

> 每一条必须满足二者之一：
> (a) 可被 verify 机械验证；
> (b) 标注“人工检查”并写明具体操作步骤。

- [ ]
- [ ]

## Verification

Quick（Loop 内每轮）：

```bash
<例如 ./scripts/verify.sh --quick 或 ./scripts/verify.ps1 -Quick>
```

Full（提交前 / CI）：

```bash
<例如 ./scripts/verify.sh 或 ./scripts/verify.ps1>
```

额外验证步骤：

-

## Stop Conditions

> 通用停止规则见 `harness/stop-rules.md`，这里只写本任务特有的。

-

## Risk & Confirmation

- 风险等级：低 / 中 / 高
- 是否触碰 stop-rules 风险点：否 / 是，具体为：<填写>
- 计划确认：低风险可写“Agent 出计划后无需等待确认”；中高风险必须等人类确认计划后再动代码。
- 预授权风险点（如有）：<明确写人类已确认的风险范围>

## Size Check

预计改动 <= ___ 个文件 / <= ___ 行。明显超出 -> 停止，建议拆分任务。

## Review Requirement

- [ ] Agent 自审即可
- [ ] 需要 Reviewer 会话
- [ ] 需要人类逐行审查
