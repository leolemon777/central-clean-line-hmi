# 中央净软线架构说明

## 当前主线

- 主解决方案：`CentralCleanLineHmi.sln`
- 当前发布源码：`src/`
- 当前运行程序：`src/PipelineControl.UI/PipelineControl.UI.csproj`
- 当前测试项目：`tests/PipelineControl.UI.Tests/PipelineControl.UI.Tests.csproj`

`PipelineControl/` 是早期/参考骨架，保留用于查阅分层思路和历史实现，不作为当前发布源码入口。

## 运行入口

- `总控`：自动升降与整线运行状态。
- `IO 点位`：X 输入显示、Y 输出手动操作、扩展卡动作组和触摸兜底。
- `伺服`：禾川 SV-X4EA 4 轴 Modbus TCP/RTU 手动测试盘。
- `通讯`：本机 IP、主卡 IP、扩展卡数、扫描周期、心跳、仿真和伺服通讯参数。

## 分层职责

| 层 | 目录 | 职责 |
|----|------|------|
| UI | `src/PipelineControl.UI` | WPF Shell、页面、ViewModel、主题、配置、IO/伺服运行服务和部署辅助文件 |
| Application | `src/PipelineControl.Application` | 应用层边界，保留给业务用例和流程编排 |
| Infrastructure | `src/PipelineControl.Infrastructure` | 基础设施边界，保留给外部资源适配 |
| Tests | `tests/PipelineControl.UI.Tests` | UI ViewModel、服务、映射、配置和工控交互相关测试 |

当前项目的大部分现场逻辑仍集中在 `PipelineControl.UI` 内。新增大型流程前，应优先评估是否需要把纯业务逻辑下沉到 `Application`。

## 硬件边界

- 博派 IO 驱动和模拟驱动通过服务层隔离，手动输出和自动输出归属分离。
- IO 点位事实入口是 `src/PipelineControl.UI/Resources/io-points.json`，不要把现场点位名、module 或 bit 硬编码进 C#。
- 伺服寄存器事实入口是 `src/PipelineControl.UI/Resources/servo-registers.json`，速度寄存器仍需现场实测确认。
- 未经任务卡预授权，不运行主程序、不连接硬件、不写 IO 输出、不写伺服寄存器。

## 部署边界

- `src/PipelineControl.UI/Deploy` 存放随程序发布的启动和 DLL 检查脚本。
- 根目录 `deploy/` 是生成的 Win10 x64 自包含部署输出，可重建，不作为源码事实来源。
- 厂家 DLL 来源在 `C#例程源代码及库文件/`，当前 UI 项目通过 csproj 引用并复制输出。

## 验证入口

- 快速验证：`.\agent-universal-harness\scripts\verify.ps1 -Quick`
- 全量验证：`.\agent-universal-harness\scripts\verify.ps1`

快速验证只执行 build、配置/结构检查和测试项目编译。全量验证会运行测试；当前机器曾出现 WDAC/SmartScreen 拦截测试 DLL 的历史风险，失败时必须如实报告。
