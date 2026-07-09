# 项目结构说明

## 当前事实入口

| 路径 | 状态 | 说明 |
|------|------|------|
| `AGENTS.md` | 当前 | 项目规则、Canonical Source Root 和硬件/知识沉淀约束 |
| `CentralCleanLineHmi.sln` | 当前 | 主解决方案 |
| `src/` | 当前 | 当前发布源码根 |
| `tests/` | 当前 | 当前测试项目根 |
| `agent-universal-harness/` | 当前 | Agent 任务卡、验证入口、权限表、进度报告 |
| `project-progress.md` | 当前 | 阶段状态、能力、未接入项和验证记录 |
| `implementation-notes.md` | 当前 | 中等以上变更的决策、验证和风险 |
| `docs/` | 当前 | 架构、交接、硬件、截图、Obsidian 源笔记和清理记录 |

## 目录分区

### 当前源码

- `src/PipelineControl.UI/`：WPF 应用、页面、ViewModel、服务、主题、资源映射、部署辅助文件。
- `src/PipelineControl.Application/`：应用层边界。
- `src/PipelineControl.Infrastructure/`：基础设施边界。
- `tests/PipelineControl.UI.Tests/`：当前主测试入口。

### 参考和外部资料

- `PipelineControl/`：早期/参考骨架，有独立解决方案。默认不修改。
- `C#例程源代码及库文件/`：厂家 demo、手册和 DLL 来源。默认不修改示例源码。
- `docs/hardware/`：硬件手册和现场参考资料。

### 生成物和临时内容

- `deploy/`：发布输出目录，可由 `dotnet publish` 重建。
- `.vs/`：Visual Studio 本地状态。
- `tmp/`：临时内容。
- `run_output.txt`、`temp_run.txt`：历史空输出文件，当前不作为事实来源。
- `bin/`、`obj/`：.NET 构建输出。

## 整理原则

1. 先登记，后移动；先验证，后删除。
2. 当前源码只在 `src/` 和 `tests/` 内改。
3. 厂家资料、早期骨架、部署包和历史交接资料不做自动删除。
4. 清理候选统一写入 `docs/cleanup/project-structure-cleanup-plan.md`。
5. 与现场硬件相关的变更必须在任务卡里写明预授权范围。
