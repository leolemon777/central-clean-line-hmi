# Progress

> 每轮结束更新。下一位接手者应该能只读本文件、任务卡和 diff 就知道当前状态。

## Current State

- 当前任务：2026-07-07-network-ip-unification-check
- 当前分支：main（Git 已初始化，尚未建立初始提交）
- 最近完成：完成 IO/伺服网络 IP 统一核对。项目配置中 IO 控制卡主卡为 `192.168.0.1`，伺服 485 网关 / USR 304-C7 为 `192.168.0.7:502`，工控机/HMI 期望本机 IP 为 `192.168.0.200`；只读 `ping` 确认 `.0.1` 和 `.0.7` 在线，当前电脑实际网卡为 `.0.3`、`.0.60`、`.0.91`，没有 `.0.200`。
- 当前阻塞：真实伺服动作尚未执行；在确认急停、机械安全、网关 IP、RS485 参数和驱动器参数前，不能启动主程序连接硬件或写伺服寄存器。工控机网卡还需要现场确认是否设置为 `192.168.0.200`。2026-07-09 已准备 Git 初始提交基线，GitHub 远端推送仍需可用认证通道。

## Last Verification

- Command：`.\agent-universal-harness\scripts\check-harness.ps1`
- Exit Code：0
- Output Excerpt：`HARNESS CHECK PASSED (Project)`
- Log Path（如有）：无

- Command：对 harness 已配置文件执行模板占位符和未配置 verify 标记扫描。
- Exit Code：1
- Output Excerpt：无输出；表示目标 harness 配置文件未匹配到模板占位或未配置 verify 标记。
- Log Path（如有）：无

- Command：`.\agent-universal-harness\scripts\verify.ps1 -Quick`
- Exit Code：0
- Output Excerpt：`已成功生成。0 个警告 0 个错误`；配置和结构检查通过；测试项目编译通过；`VERIFY PASSED`
- Log Path（如有）：无

- Command：`.\scripts\verify.ps1 -Quick`
- Exit Code：0
- Output Excerpt：根目录快捷入口成功调用 harness；主解决方案和测试项目编译均为 `0 个警告 0 个错误`；`VERIFY PASSED`
- Log Path（如有）：无

- Command：`git rev-parse --show-toplevel; git branch --show-current; git status --short --untracked-files=no`
- Exit Code：0
- Output Excerpt：仓库根为 `E:/Desktop/开发项目汇总/中央净软线`，当前分支 `main`；无已跟踪文件变更，因为尚未创建初始提交。
- Log Path（如有）：无

- Command：`.\agent-universal-harness\scripts\check-harness.ps1`
- Exit Code：0
- Output Excerpt：Git 状态文档同步后仍为 `HARNESS CHECK PASSED (Project)`。
- Log Path（如有）：无

- Command：`.\agent-universal-harness\scripts\verify.ps1`
- Exit Code：0
- Output Excerpt：build 通过（0 warnings / 0 errors），配置和结构检查通过，测试通过（149 passed），安全检查确认未启动 App、未连接硬件、未写 IO 或伺服寄存器；`VERIFY PASSED`。
- Log Path（如有）：无

- Command：`dotnet test tests\PipelineControl.UI.Tests\PipelineControl.UI.Tests.csproj --no-restore --filter "FullyQualifiedName~ButtonTemplateReadabilityTests|FullyQualifiedName~Servo"`
- Exit Code：0
- Output Excerpt：按钮模板和伺服相关测试通过（37 passed）。
- Log Path（如有）：无

- Command：`dotnet test tests\PipelineControl.UI.Tests\PipelineControl.UI.Tests.csproj --no-restore --filter "FullyQualifiedName~AppThemePageSmokeTests|FullyQualifiedName~SettingsPageSmokeTests"`
- Exit Code：0
- Output Excerpt：WPF 页面 smoke test 通过（3 passed）。
- Log Path（如有）：无

- Command：`.\agent-universal-harness\scripts\verify.ps1 -Quick`
- Exit Code：0
- Output Excerpt：build 通过（0 warnings / 0 errors），配置和结构检查通过，测试项目编译通过（0 warnings / 0 errors），安全检查确认未启动 App、未连接硬件、未写 IO 或伺服寄存器；`VERIFY PASSED`。
- Log Path（如有）：无

- Command：`dotnet restore CentralCleanLineHmi.sln`
- Exit Code：0
- Output Excerpt：清理 `obj/` 后重新生成 NuGet assets，4 个项目还原成功。
- Log Path（如有）：无

- Command：`.\agent-universal-harness\scripts\verify.ps1 -Quick`
- Exit Code：0
- Output Excerpt：目录清理后 build 通过（0 warnings / 0 errors），配置和结构检查通过，测试项目编译通过（0 warnings / 0 errors），安全检查确认未启动 App、未连接硬件、未写 IO 或伺服寄存器；`VERIFY PASSED`。
- Log Path（如有）：无

- Command：`.\agent-universal-harness\scripts\check-harness.ps1`
- Exit Code：0
- Output Excerpt：`HARNESS CHECK PASSED (Project)`；并确认 `.vs`、`tmp`、`work`、`run_output.txt`、`temp_run.txt` 当前不存在。
- Log Path（如有）：无

- Command：读取 `src/`、Debug 输出目录和 `deploy/` 下的 `appsettings*.json`。
- Exit Code：0
- Output Excerpt：`CardComm.PcIp=192.168.0.200`，`CardComm.MainCardIp=192.168.0.1`，`ServoComm.GatewayIp=192.168.0.7`，`ServoComm.GatewayPort=502`。
- Log Path（如有）：无

- Command：`Get-NetIPAddress -AddressFamily IPv4`。
- Exit Code：0
- Output Excerpt：当前电脑存在 `192.168.0.3/24`、`192.168.0.60/24`、`192.168.0.91/24`，未发现 `192.168.0.200`。
- Log Path（如有）：无

- Command：`ping -n 1 -w 1000 192.168.0.1` / `192.168.0.7` / `192.168.0.200`。
- Exit Code：0
- Output Excerpt：`192.168.0.1` 响应，`192.168.0.7` 响应，`192.168.0.200` 超时。
- Log Path（如有）：无

## Decisions

| 日期 | 决策 | 原因 | 影响 |
|------|------|------|------|
| 2026-07-07 | harness 目录采用稳定名 `agent-universal-harness` | 用户复制进来的目录名带 fresh/date，不适合作长期项目入口 | 后续命令和文档统一引用稳定路径 |
| 2026-07-07 | 不物理删除或移动业务目录 | 项目无可用 Git 基准且包含厂家资料、部署包和早期骨架 | 清理先登记候选项，避免破坏可追溯资料 |
| 2026-07-07 | 快速验证不运行测试 DLL，只编译测试项目 | 当前项目已有测试宿主/策略类风险；日常无硬件验证需要稳定入口 | 快速 verify 可用；全量 verify 仍保留真实测试并暴露失败 |
| 2026-07-07 | 执行 `git init -b main` | `.git` 目录为空壳，缺 `HEAD/config/objects/refs` | Git 命令可用；仍需人工确认后创建初始提交 |
| 2026-07-07 | 伺服首次现场动作前默认使用点动、单轴、100 rpm | 降低误触后连续运动和多轴同步风险 | 真实动作仍需现场确认后执行；同步/连续模式保留但不作为首次调试默认 |
| 2026-07-07 | WPF smoke test 使用共享 STA Dispatcher，不在每个测试中反复 `Application.Shutdown()` | 反复创建/关闭 WPF Application 会在测试宿主退出阶段触发 `HwndSubclass` 崩溃 | 全量 verify 恢复稳定，仍保留真实页面资源加载测试 |
| 2026-07-07 | 485 Modbus 资料按“驱动器侧 RS485 RTU，HMI 侧 TCP→RTU 网关”记录 | 当前代码是 `TcpClient + NModbus`，不是 COM 口串口主站 | 若现场改为 USB-RS485 直连，需要新增串口 RTU 驱动 |
| 2026-07-07 | `P09.02=0` 按厂家资料记录为无校验、2 停止位 | 避免 USR 串口侧误配为 1 停止位导致 RTU 通讯失败 | USR 串口参数必须与伺服 P09 参数一致 |
| 2026-07-07 | 项目清理只删除 `.vs`、临时目录、0 字节输出并清理当前源码/测试旧 `obj` | 项目尚无初始提交，且 `deploy`、厂家资料、参考骨架仍有现场价值 | 保留可运行 `bin` 和所有业务/资料目录；清理后需要先 restore 再 quick verify，验证会重新生成必要 `obj` |
| 2026-07-07 | 现场 IP 统一按 `192.168.0.0/24` 规划 | IO 主卡 `.0.1` 和伺服网关 `.0.7` 已在线，项目配置期望工控机 `.0.200` | 后续工控机网卡应配置/增加 `192.168.0.200`，不要把 IO 主卡和伺服网关改成同一个 IP |
| 2026-07-09 | GitHub 基线提交前补充忽略规则 | 避免把 Visual Studio 临时文件、构建输出和可重建部署内容纳入初始提交 | 初始提交聚焦源码、文档、厂家资料和必要 DLL；远端建议使用 private 仓库 |

## Risks / Open Items

- Git 已初始化为 `main`，但无初始提交；创建基线前仍无法依赖提交历史回滚。
- 早前全量 `dotnet test` 曾出现 WPF 测试宿主退出阶段崩溃；已通过共享 STA Dispatcher 修复，当前完整 verify 通过。
- `deploy/`、厂家资料、参考骨架和 `bin` 已明确保留；`.vs/`、`tmp/`、`work/`、空输出文件和当前源码/测试旧 `obj/` 已完成低风险清理。当前存在的 `obj/` 是 restore/build 验证重新生成的必要中间产物。
- 项目涉及真实 IO 和伺服控制；未获授权时不能启动主程序、连接硬件或写硬件输出/伺服寄存器。
- 伺服速度寄存器 `0324H` 已有厂家 X5 Modbus 资料依据，但当前实物为 X4EA，仍需现场用 Modbus Poll 实测；若不对，只改 `src/PipelineControl.UI/Resources/servo-registers.json`，不要硬编码。
- 当前 HMI 不支持电脑直连 USB-RS485/COM 口；如现场不用 TCP→RTU 网关，需要先做串口 RTU 驱动。
- 当前电脑实际未配置项目期望的 `192.168.0.200`；现场启动 HMI 连接 IO 前，应确认工控机用于设备通讯的网卡是否设置为 `192.168.0.200/24`，或同步修改 `CardComm.PcIp`。

## Next Step

- 先把工控机设备通讯网卡统一到 `192.168.0.200/24`，并保持 IO 主卡 `192.168.0.1`、伺服网关 `192.168.0.7` 不冲突；随后现场确认急停、机械安全、RS485 参数和驱动器 P09/P00/P03/P04 后，再进入 HMI 连接和点动。
