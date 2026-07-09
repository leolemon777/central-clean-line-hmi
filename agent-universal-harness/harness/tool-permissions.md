# 工具权限表（中央净软线）

> 原则：模型只提出工具调用意图；真正执行的是 Runtime / CLI / MCP / 脚本。权限边界必须写在这里，而不是靠 Agent 自觉。

## 权限等级

| 等级 | 含义 | 默认策略 |
|------|------|----------|
| T0 | 读本仓库文件、查看 diff、运行只读命令 | 可自动 |
| T1 | 修改工作区文件、运行 build/test/lint | 可自动，但必须 verify |
| T2 | 新增依赖、启动长期进程、连接外部只读 API、生成部署包 | 需任务卡授权 |
| T3 | 写数据库、写外部 API、发送消息、修改外部配置 | 必须人类确认 |
| T4 | 真实硬件动作、资金、生产数据写入、删除性迁移 | 默认禁止；必须专项流程 |

## 工具登记

| Tool / Command | 用途 | 等级 | 自动允许? | 需要确认? | 返回结果要求 |
|----------------|------|------|-----------|-----------|--------------|
| `Get-Content` / `Get-ChildItem` / `rg` | 读取项目文件、搜索上下文、检查占位符 | T0 | 是 | 否 | stdout |
| `git status` / `git diff` | 查看仓库状态和变更；初始提交前 diff 基准有限 | T0 | 是 | 否 | stdout/stderr + exit code |
| `dotnet build CentralCleanLineHmi.sln --no-restore` | 编译当前主解决方案 | T1 | 是 | 否 | command + exit code + excerpt |
| `dotnet build tests\PipelineControl.UI.Tests\PipelineControl.UI.Tests.csproj --no-restore` | 快速模式下编译测试项目 | T1 | 是 | 否 | command + exit code + excerpt |
| `dotnet test tests\PipelineControl.UI.Tests\PipelineControl.UI.Tests.csproj --no-restore` | 全量测试；本机可能受 WDAC 拦截 | T1 | 是 | 否 | command + exit code + excerpt |
| `.\agent-universal-harness\scripts\verify.ps1` | 统一验证入口 | T1 | 是 | 否 | command + exit code + excerpt |
| `dotnet restore` / `dotnet publish` | 还原包或生成 Win10 x64 部署包 | T2 | 否 | 是 | command + exit code + 输出目录核对 |
| 启动 `PipelineControl.UI.exe` 或 `dotnet run` | 可能连接真实 IO/伺服或触发现机交互 | T4 | 否 | 是 | 必须写明仿真/真实硬件状态 |
| 写 IO 输出、写伺服寄存器、运行自动流程 | 真实设备动作 | T4 | 否 | 是 | 人类现场确认 + 回滚/急停方案 |

## 禁止项

- 禁止把密钥、token、真实账号、真实 IP 写入代码或日志。
- 禁止绕过本文件权限等级直接调用外部写操作。
- 禁止让 Agent 直接操作真实硬件；必须先经过仿真 / dry-run / 人类确认。
- 禁止删除或移动厂家资料、早期骨架、部署包和历史交接资料；需要清理时先登记候选项并确认。
