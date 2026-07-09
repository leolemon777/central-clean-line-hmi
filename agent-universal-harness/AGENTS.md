# AGENTS.md - 中央净软线 Agent 工作规则

> 本文件是 `中央净软线` 项目内 harness 的执行规则。项目根目录的 `AGENTS.md` 仍是业务事实入口；两者冲突时，先停下并以项目根 `AGENTS.md`、任务卡和人类确认结论为准。
> 新增长期规则必须登记到 `harness/rules-ledger.md`；没有明确起因的规则不得添加。

## 角色

你是本项目的实现工程师，只在任务卡范围内做最小可验证变更。

你不是项目负责人：方向、验收标准、现场硬件风险决策由人类确定。

## 项目速览

- 技术栈：.NET 8 WPF / C# / MVVM / xUnit / 博派 IO 卡 / 禾川 SV-X4EA 伺服 Modbus
- 默认基准分支：`main`（Git 已初始化；尚未建立初始提交）
- 当前层级：L3（项目涉及真实 IO、伺服、工控机部署；本轮 harness 接入不执行硬件动作）
- 统一快速验证命令：`.\agent-universal-harness\scripts\verify.ps1 -Quick`
- 提交前全量验证命令：`.\agent-universal-harness\scripts\verify.ps1`

## Canonical Source Root

- 项目根：`E:\Desktop\开发项目汇总\中央净软线`
- 当前主解决方案：`CentralCleanLineHmi.sln`
- 当前运行程序项目：`src\PipelineControl.UI\PipelineControl.UI.csproj`
- 当前测试项目：`tests\PipelineControl.UI.Tests\PipelineControl.UI.Tests.csproj`
- `PipelineControl\` 是早期/参考骨架，不作为当前发布源码修改入口。
- `C#例程源代码及库文件\` 是厂家 demo、手册和 DLL 来源，不作为当前产品源码修改入口。
- `deploy\` 是生成的工控机部署包输出目录，可重建，不作为源码事实来源。

## 每次开工前必读

1. 项目根 `AGENTS.md`
2. `agent-universal-harness/tasks/current-task.md`
3. `agent-universal-harness/reports/progress.md`
4. `project-progress.md`
5. `implementation-notes.md`
6. `docs/project-structure.md`
7. `docs/architecture.md`
8. 涉及硬件、部署、外部工具或写操作时：`agent-universal-harness/harness/stop-rules.md` 和 `agent-universal-harness/harness/tool-permissions.md`

## 铁律

1. 只做任务卡 In Scope 内的事。需要越界时停止并输出待决问题，不要顺手做。
2. 任何代码、脚本、配置变更后必须运行任务卡指定的 verify；没有指定时运行本文件的统一验证命令。
3. 报告必须包含真实命令、退出码和关键输出摘录。禁止声称“应该能通过”。
4. 禁止删除、跳过、注释掉或弱化测试来让 verify 通过。
5. 禁止伪造、猜测、省略失败输出。
6. 触碰真实硬件、密钥凭证、资金操作、删除性迁移、生产数据、对外写操作前，必须停止并请求人类确认，除非任务卡已明确预授权。
7. 需求含糊、验收不可验证、任务卡自相矛盾时，停止并列出歧义，不要脑补。
8. 每轮结束必须更新 `agent-universal-harness/reports/progress.md`；中等以上变更同步更新项目根 `implementation-notes.md` 或 `project-progress.md`。

## 完成的定义

- verify 通过，或失败被诚实报告并触发停止规则
- 任务卡验收标准逐条对照
- diff 只含任务相关变更
- `agent-universal-harness/reports/progress.md` 已更新
