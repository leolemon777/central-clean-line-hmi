# 启动提示词模板

Prompt 在这套体系里不是临时话术，而是固化的工程协议。按模式选用，替换尖括号内容。

## 模式一：标准执行

```text
请进入本仓库的 Harness Loop 模式。

先读：
- AGENTS.md
- tasks/current-task.md
- reports/progress.md
- harness/loop.md
- harness/quality-gates.md
- harness/stop-rules.md（L2+ 或触发风险时）
- harness/tool-permissions.md（涉及外部工具/MCP/API/硬件/写操作时）
- 任务卡 Context 段指定的文件

要求：
1. 按 harness/loop.md 执行。先输出理解与计划。
2. 本任务风险等级为 <低/中/高>：<中高风险：未经我确认不得修改 / 低风险且任务卡允许：可直接执行>。
3. 开工前检查 Gate 1；提交前检查 Gate 2。
4. 每轮只做最小变更。
5. 修改后运行任务卡 Verification 指定的 quick 命令；没有指定时运行 AGENTS.md 的统一快速验证命令。
6. 失败自动修复最多 3 轮；之后停止并输出失败分析。
7. 触发 harness/stop-rules.md 任意条款立即停止。
8. 报告必须包含真实命令、退出码、关键输出摘录，并更新 reports/progress.md。
```

## 模式二：审查

```text
你是本仓库的 Reviewer，只审查，不修改任何代码。

读：
- AGENTS.md
- tasks/current-task.md
- reports/progress.md
- harness/review-checklist.md
- 当前 diff：git diff <基准分支>...HEAD

按 harness/review-checklist.md 逐项检查，输出四级结论：
- Blocking（必须修复才能合并）
- Non-blocking（建议）
- Missing tests（缺失的测试点）
- Safety risks（安全隐患）

特别核对：verify 输出是否真实、有无删弱测试、有无越界改动、有无未经授权的外部副作用。
```

## 模式三：探索型 spike

```text
本任务为探索型原型，在 spike/<名称> 分支上进行。

放宽：允许快速迭代，verify 仅需 build 通过，允许粗糙实现。
不放宽：AGENTS.md 铁律 5/6/7 仍然有效；任何真实世界副作用仍需人类确认。

目标：<要验证的假设或要探明的问题>
产出：除代码外，输出一份发现清单：
- 哪些假设成立
- 哪些假设不成立
- 若正式实现，规格里必须写清的点

提醒：本分支代码默认作废，不直接合入主干。确认方向后，从原型中提取规格，再进入建造型流程。
```
