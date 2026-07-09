# Evaluation Protocol

> 触发条件：需要持续评估 Agent 输出、模型选择、复杂质量指标或非确定性任务。

## Eval 优先级

1. Deterministic checks：build、test、lint、typecheck、simulation。
2. Structural checks：架构依赖、覆盖率、复杂度、静态分析。
3. Scenario checks：端到端业务场景。
4. LLM review：语义、可维护性、文档质量、安全风险初筛。
5. Human review：最终判断。

## 输出要求

每次 eval 尽量机器可读：

```json
{
  "passed": true,
  "score": 0.95,
  "failed_checks": [],
  "risks": [],
  "timestamp": "YYYY-MM-DDTHH:mm:ss"
}
```

## 迭代优化规则

如果目标是质量提升，不要说“继续改好一点”。必须定义：

- 当前 baseline。
- 指标。
- 目标阈值。
- 每轮只改一个主要瓶颈。
- 每轮记录分数和变化。

## LLM Judge 限制

LLM Judge 只能补充，不得替代 deterministic checks。
