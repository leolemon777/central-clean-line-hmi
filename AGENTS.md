# 中央净软线 项目规则

## 项目架构
- .NET 8 WPF，MVVM，管线控制系统
- 分层：UI / Application / Infrastructure
- 测试：UI.Tests

## Canonical Source Root
- 本项目当前唯一源码根目录：`E:\Desktop\开发项目汇总\中央净软线`
- 当前解决方案：`CentralCleanLineHmi.sln`
- 当前运行程序项目：`src\PipelineControl.UI\PipelineControl.UI.csproj`
- `PipelineControl\` 是早期/参考骨架，不作为当前发布源码修改入口。
- `C#例程源代码及库文件\` 是厂家 demo、手册和 DLL 来源，不作为当前产品源码修改入口。
- `deploy\` 是生成的工控机部署包输出目录，可重建，不作为源码事实来源。

## Harness / Verify
- Agent harness 目录：`agent-universal-harness\`
- 快速验证：`.\agent-universal-harness\scripts\verify.ps1 -Quick`
- 全量验证：`.\agent-universal-harness\scripts\verify.ps1`
- 根目录快捷入口：`.\scripts\verify.ps1 -Quick` 或 `.\scripts\verify.ps1`
- Git 已初始化为 `main` 分支，但尚未建立初始提交；建立基线前报告必须明确说明缺少可比较的提交历史。

## 知识沉淀规则
- repo 是项目运行、维护、调试、部署的事实来源；项目关键事实优先写入 `project-progress.md`、`implementation-notes.md`、`docs/` 或本文件。
- 中等以上功能、现场 bug、硬件接入、部署打包、自动流程变更，都要维护 `implementation-notes.md`，记录决策、验证、风险。
- 阶段状态和后续待办维护在 `project-progress.md`，不要只留在聊天记录里。
- 现场点位名称和 module/bit 修正统一进入 `src\PipelineControl.UI\Resources\io-points.json`，不要把现场点位硬编码进 C#。
- 写入文档的路径、命令、部署目录必须先实际验证；Windows 路径优先用 `Test-Path` 确认。
- 具有跨项目复用价值的经验要沉淀到 Obsidian；项目构建、测试、部署不能依赖个人 Obsidian。
- Obsidian 沉淀默认使用项目文件夹形式：`E:\Desktop\PLC知识库\中央净软线\`。
- Obsidian 项目首页必须包含指向源码的 Canonical Source Root：`E:\Desktop\开发项目汇总\中央净软线`，避免后续改错副本。
- 当前沙箱不能直接写 Obsidian 时，先写入 `docs\obsidian\`，再使用 `scripts\复制中央净软线笔记到Obsidian.cmd` 同步到 Obsidian 项目文件夹。

## 全局 WPF 红线规范
遵守 ~/.Codex/AGENTS.md 中的 32 条 WPF 红线规则。
