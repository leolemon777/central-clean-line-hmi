# PROJECT.md - 中央净软线项目宪章

> 本文件描述 `中央净软线` 真实项目的当前阶段边界。项目细节以项目根 `AGENTS.md`、`project-progress.md`、`implementation-notes.md`、`docs/` 和源码为准。

## 一句话目标

构建并维护一个基于 .NET 8 WPF 的中央净软线工控 HMI，用 PC、博派 IO 卡和禾川伺服通信替代 PLC 完成整线监控、手动测试、自动升降和现场部署。

## 用户是谁

工控机现场操作员、调试工程师和维护人员；使用场景是中央净软线现场调试、IO/伺服验证、自动升降运行、部署包复制和故障排查。

## 本阶段做什么（In Scope）

- 维护当前发布源码根 `src/`、主解决方案 `CentralCleanLineHmi.sln` 和 UI 测试 `tests/PipelineControl.UI.Tests`。
- 维护 `总控`、`IO 点位`、`伺服`、`通讯` 四个运行入口。
- 维护 IO 点位映射 `src\PipelineControl.UI\Resources\io-points.json` 和伺服寄存器映射 `src\PipelineControl.UI\Resources\servo-registers.json`。
- 维护工控机 Win10 x64 自包含部署包生成与 DLL 环境检查脚本。
- 维护项目知识：`AGENTS.md`、`project-progress.md`、`implementation-notes.md`、`docs/` 和本 harness。

## 本阶段明确不做什么（Out of Scope）

- 不把 `PipelineControl\` 早期骨架当作当前发布源码修改入口。
- 不直接修改厂家 demo 目录里的示例源码来实现产品功能。
- 不在未授权情况下运行真实硬件动作、写 IO 输出、写伺服命令、启动现场自动流程。
- 不删除 `deploy\`、`C#例程源代码及库文件\`、`PipelineControl\`、`docs\obsidian\` 或历史交接资料；清理只先登记候选项。
- 不新增依赖、改 NuGet 版本或改部署输出策略，除非任务卡明确授权。

## 成功标准（必须可检验）

- [ ] `.\agent-universal-harness\scripts\check-harness.ps1` 通过 Project 模式自检。
- [ ] 快速验证命令 `.\agent-universal-harness\scripts\verify.ps1 -Quick` 能执行构建、配置检查和测试项目编译。
- [ ] 项目结构文档能清楚区分当前源码、早期骨架、厂家资料、部署产物、文档和临时/清理候选。
- [ ] 涉及真实硬件的任务必须在任务卡中写明预授权范围；未授权时只允许 build/test/config/doc 验证。

## 最大风险（按严重度排序）

1. 真实 IO 或伺服输出误动作，造成设备、机构或人员安全风险。
2. 改错目录：把 `PipelineControl\` 早期骨架或厂家 demo 当成当前发布源码。
3. 点位或寄存器硬编码到 C#，绕过 JSON 映射入口，导致现场修正困难。
4. 部署包缺 DLL、VC++ 运行库或架构不匹配，导致工控机启动失败。
5. Git 已初始化但尚未建立初始提交，暂时缺少可靠 diff 基准和回滚记录。

## 风险等级声明

本项目涉及（勾选）：

- [ ] 生产数据
- [x] 真实硬件
- [ ] 资金 / 支付
- [ ] 用户隐私
- [ ] 对外自动操作（发邮件、调外部写 API 等）
- [x] MCP / 外部工具 / 数据库写入 / 设备控制

勾选任意一项 -> 必须启用 `harness/stop-rules.md` 的 L2+ 条款；若涉及真实硬件、资金、生产数据、隐私或合规，应评估是否升 L3。

## 当前层级

L3

| 日期 | 层级变更 | 触发原因 |
|------|----------|----------|
| 2026-07-07 | 接入为 L3 | 项目包含真实 IO、伺服和工控机部署；需要 stop rules、权限表、任务卡和验证入口。 |
