# 项目结构清理计划

> 本文件只登记候选项，不代表已经删除或移动。涉及删除、迁移、合并目录前必须人工确认。

## 已完成的低风险整理

- 将复制进来的 `agent-universal-harness-fresh-20260707` 重命名为稳定目录 `agent-universal-harness`。
- 新增 `.gitignore`，覆盖常见 .NET 输出、Visual Studio 本地状态、临时文件和根部署输出。
- 新增 `docs/architecture.md` 和 `docs/project-structure.md`，明确当前源码、参考骨架、厂家资料、部署输出和清理候选。
- 2026-07-07 按 harness 流程执行低风险清理：
  - 已删除 `.vs/`、`tmp/`、`work/`、`run_output.txt`、`temp_run.txt`。
  - 已清理当前源码/测试项目下可重建的旧 `obj/` 目录：`src/PipelineControl.Application/obj`、`src/PipelineControl.Infrastructure/obj`、`src/PipelineControl.UI/obj`、`tests/PipelineControl.UI.Tests/obj`。后续 `dotnet restore` / `dotnet build` 会按当前状态重新生成必要的 `obj/`。
  - 已保留 `src/**/bin`、`tests/**/bin`，避免破坏当前可运行 exe 和本地 `appsettings.local.json`。
  - 已保留 `deploy/`、`C#例程源代码及库文件/`、`PipelineControl/`、`src/`、`tests/`、`docs/`、`scripts/`、`agent-universal-harness/`。
  - 清理后先执行 `dotnet restore CentralCleanLineHmi.sln` 重新生成 `project.assets.json`，再执行 `.\agent-universal-harness\scripts\verify.ps1 -Quick`，结果通过。

## 清理候选

| 路径 | 当前判断 | 建议动作 | 风险 |
|------|----------|----------|------|
| `.git/` | 已重新初始化为 `main`，尚无初始提交 | 人工确认纳入范围后创建初始提交，或如需旧历史则从备份/远程恢复 | 高：影响版本历史和回滚 |
| `run_output.txt` / `temp_run.txt` | 当前为 0 字节历史输出 | 已删除 | 低 |
| `tmp/` | 临时目录 | 已删除 | 中 |
| `src/**/bin`、`tests/**/bin` | 当前可运行/测试输出 | 保留，避免影响现场马上运行和本地配置 | 中 |
| `src/**/obj`、`tests/**/obj` | 构建中间输出 | 旧内容已清理；restore/build 会重新生成当前资产 | 低 |
| `deploy/` | 工控机部署输出，可重建但可能用于现场 U 盘复制 | 不自动删除；按日期归档或重新发布后替换 | 高：可能是现场正在使用的部署包 |
| `PipelineControl/` | 早期/参考骨架 | 保留；若要归档，先确认没有仍需参考的代码 | 高：可能包含历史设计依据 |
| `C#例程源代码及库文件/` | 厂家资料和 DLL 来源 | 保留 | 高：当前 UI 项目依赖其中 64 位 DLL |

## 后续建议

- 建立初始提交前，不做批量删除。
- 清理构建输出前，先运行 `.\agent-universal-harness\scripts\verify.ps1 -Quick` 并记录结果。
- 若要压缩历史部署包，先确认当前工控机使用的版本和 U 盘交付目录。
