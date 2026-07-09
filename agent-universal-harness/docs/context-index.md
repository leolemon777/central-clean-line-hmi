# 上下文索引

> 原则：给 Agent 精准的少量上下文，胜过全量倾倒。
> 本项目含真实硬件控制；涉及 IO、伺服、部署、自动流程时必须读完整上下文，不许只看片段。

## 新鲜度规则

本索引指向的文档，若满足任一条件，视为疑似过时：

- 未登记最后核对日期；
- 最后核对超过 30 天；
- 明显落后当前代码；
- 与测试、代码或任务卡冲突。

遇到疑似过时文档：以代码 + 测试 + 任务卡为准，并在报告中标注“文档疑似过时”。

## 按任务类型选读

| 任务类型 | 必读 | 选读 |
|----------|------|------|
| 任意任务 | 项目根 `AGENTS.md`、当前任务卡、`project-progress.md` | `implementation-notes.md` |
| 修改 UI / WPF | `docs/architecture.md`、相关 `src\PipelineControl.UI\Views` / `ViewModels` / `Themes` | `docs\screenshots` |
| 修改 IO | `io-points.json`、`IoBoardService` 相关代码、`project-progress.md` 风险段 | `docs\hardware`、现场检查清单 |
| 修改伺服 | `servo-registers.json`、`Services\Servo`、`docs\servo-field-checklist.md` | 禾川手册、Modbus Poll 验证记录 |
| 部署打包 | `project-progress.md` 部署记录、`src\PipelineControl.UI\Deploy`、`deploy` 目录 | `docs\obsidian\Playbook 中央净软线工控机部署与IO测试.md` |
| 清理目录 | `docs/project-structure.md`、`docs/cleanup/project-structure-cleanup-plan.md` | `docs/README.md` |
| 涉及外部工具 / 硬件 | `harness/tool-permissions.md`、`harness/stop-rules.md` | 任务卡风险确认 |

## 文档新鲜度登记

| 文档 | 最后核对（日期 / commit） | 核对人 | 状态 |
|------|---------------------------|--------|------|
| 项目根 `AGENTS.md` | 2026-07-07 / main（无初始提交） | Codex | 当前 |
| `project-progress.md` | 2026-07-07 / main（无初始提交） | Codex | 当前 |
| `implementation-notes.md` | 2026-07-07 / main（无初始提交） | Codex | 当前 |
| `docs/README.md` | 2026-07-07 / main（无初始提交） | Codex | 当前 |
| `docs/architecture.md` | 2026-07-07 / main（无初始提交） | Codex | 当前 |
| `docs/project-structure.md` | 2026-07-07 / main（无初始提交） | Codex | 当前 |
| `CentralCleanLineHmi.sln` | 2026-07-07 / main（无初始提交） | Codex | 当前 |
| `src/PipelineControl.UI/PipelineControl.UI.csproj` | 2026-07-07 / main（无初始提交） | Codex | 当前 |
