# Current Task

> 从 `tasks/task-template.md` 复制后填写。不要让 Agent 在空任务卡下写业务代码。

## Metadata

- Task ID：2026-07-07-network-ip-unification-check
- Branch：main（尚未建立初始提交）
- Risk Level：Medium（真实设备网络核对；本轮只读探测）
- Layer：L3
- Base Branch / Commit：main / 无初始提交

## Goal（一句话）

确认中央净软线现场网络 IP 规划：IO 控制卡、伺服 485 网关和工控机应统一在 `192.168.0.0/24` 网段，且各设备 IP 唯一。

## Context（读哪些文件，为什么）

- `src/PipelineControl.UI/appsettings.json`：源码默认通讯配置。
- `src/PipelineControl.UI/bin/Debug/net8.0-windows/appsettings.json`：Debug exe 旁默认配置。
- `src/PipelineControl.UI/bin/Debug/net8.0-windows/appsettings.local.json`：Debug exe 旁本地覆盖配置。
- `deploy/CentralCleanLineHmi-win10-x64-20260622/appsettings.json`：部署包默认配置。
- `deploy/CentralCleanLineHmi-win10-x64-20260622/appsettings.local.json`：部署包本地覆盖配置。
- `src/PipelineControl.UI/Services/Io/IoBoardConnectionOptions.cs`、`SystemSettings.cs`、`RealIoBoardDriver.cs`：确认 IO 通讯字段和厂家 DLL 调用方式。

## In Scope

- 只读检查配置中的 `CardComm.PcIp`、`CardComm.MainCardIp`、`ServoComm.GatewayIp`。
- 只读检查当前电脑网卡 IPv4。
- 使用 `ping` 做只读在线性探测。
- 将结论写入 Harness 进度和项目记录。

## Out of Scope

- 不修改程序配置。
- 不启动 HMI 主程序。
- 不连接厂家 `MultiCardCS` 驱动。
- 不写 IO 输出。
- 不写伺服使能、速度或任何寄存器。
- 不改 Windows 网卡 IP。

## Constraints（技术约束与禁止事项）

- 真实硬件网络存在，所有动作必须保持只读。
- `192.168.0.1`、`192.168.0.7`、`192.168.0.200` 不能互相冲突。
- 若需要改工控机网卡 IP 或设备 IP，必须由用户现场确认后执行。

## Acceptance Criteria

- [x] 已确认 IO 控制卡主卡配置 IP。
- [x] 已确认伺服 485 网关配置 IP。
- [x] 已确认当前电脑网卡已有的 `192.168.0.x` 地址。
- [x] 已给出统一 IP 规划建议。
- [x] 已记录到 Harness 和项目进度文件。

## Verification

Quick（Loop 内每轮）：

```powershell
.\agent-universal-harness\scripts\verify.ps1 -Quick
```

Full（提交前 / CI）：

```powershell
.\agent-universal-harness\scripts\verify.ps1
```

本轮实际执行：

- 读取配置确认：
  - `CardComm.PcIp = 192.168.0.200`
  - `CardComm.MainCardIp = 192.168.0.1`
  - `ServoComm.GatewayIp = 192.168.0.7`
  - `ServoComm.GatewayPort = 502`
- 读取当前电脑网卡确认：
  - `WLAN 2 = 192.168.0.3/24`
  - `以太网 = 192.168.0.91/24`
  - `以太网 = 192.168.0.60/24`
- 只读 `ping` 探测确认：
  - `192.168.0.1` 有响应，判断为 IO 控制卡主卡在线。
  - `192.168.0.7` 有响应，判断为 USR 304-C7 / 伺服 485 网关在线。
  - `192.168.0.200` 超时，当前电脑未使用项目配置中的工控机 IP。

## Decision

- 当前项目内统一网络规划按以下方式记录：

```text
工控机 / HMI:      192.168.0.200
IO 控制卡主卡:     192.168.0.1
伺服 485 网关:     192.168.0.7
子网掩码:          255.255.255.0
```

- 结论：IO 控制卡不是 `.0.7`，IO 控制卡主卡是 `192.168.0.1`；`.0.7` 是 USR 304-C7 串口服务器/伺服 485 网关。
- 当前需要现场处理的是工控机本机网卡：项目配置期望 `192.168.0.200`，但当前电脑实际没有这个 IP。

## Stop Conditions

- 需要改 Windows 网卡 IP 时停止并请求用户确认。
- 需要打开 HMI 并连接 IO/伺服硬件时停止。
- 需要写 IO 输出或伺服寄存器时停止。

## Risk & Confirmation

- 风险等级：Medium。
- 是否触碰 stop-rules 风险点：否，本轮只读配置和 `ping`，未启动 App、未连接厂家驱动、未写 IO 或伺服寄存器。
- 预授权风险点：无。

## Review Requirement

- [x] Agent 自审即可
- [ ] 需要 Reviewer 会话
- [x] 需要人类现场确认后才能执行硬件动作或修改网卡 IP
