---
type: playbook
domain: 工控
tags:
  - 工控
  - 部署
  - IO测试
  - WPF
  - 可复用
---

# Playbook:中央净软线工控机部署与 IO 测试

> 适用：中央净软线从开发机打包到工控机，上机连接板卡并做 IO 测试。
> 目标不是“程序能打开”，而是确认 DLL、通讯、X 输入、Y 输出、同步动作组、点动语义都成立。

---

## Step 0:确认源码入口

```text
Canonical Source Root:
E:\Desktop\开发项目汇总\中央净软线
```

不要改：

```text
E:\Desktop\开发项目汇总\中央净软线\PipelineControl\
```

那是早期参考骨架。

---

## Step 1:打包前本地验证

```text
dotnet test tests\PipelineControl.UI.Tests\PipelineControl.UI.Tests.csproj --no-restore
dotnet build CentralCleanLineHmi.sln --no-restore
```

当前最近记录：

```text
112 passed
0 warnings, 0 errors
```

当前可用部署包：

```text
E:\Desktop\开发项目汇总\中央净软线\deploy\CentralCleanLineHmi-win10-x64-20260622\
```

直接把整个 `CentralCleanLineHmi-win10-x64-20260622` 文件夹复制到 U 盘；到工控机后建议先复制到本地磁盘再运行。

发布命令记录：

```text
dotnet restore src\PipelineControl.UI\PipelineControl.UI.csproj -r win-x64
dotnet publish src\PipelineControl.UI\PipelineControl.UI.csproj -c Release -r win-x64 --self-contained true --no-restore -o E:\Desktop\开发项目汇总\中央净软线\deploy\CentralCleanLineHmi-win10-x64-20260622 /p:PublishSingleFile=false /p:PublishReadyToRun=false
```

---

## Step 2:部署包必须包含

```text
PipelineControl.UI.exe
appsettings.json
appsettings.local.json(如有现场配置)
Resources\io-points.json
MultiCard.dll
MultiCardCLR.dll
MultiCardCS.dll
msvcr100.dll
必要 .NET 自包含运行文件
检查DLL环境.cmd
```

现场曾出现 DLL/运行库问题，不能只看 exe。

2026-06-22 包内已确认：

```text
PipelineControl.UI.exe     x64
MultiCard.dll              x64
MultiCardCLR.dll           x64
MultiCardCS.dll            x64
msvcr100.dll               x64
Resources\io-points.json   exists
appsettings.json           exists
```

`MultiCardCLR.dll` 依赖 VC++ 2010 x64 运行库 `MSVCR100.dll`。旧部署包如果只有 MultiCard 三件套，没有 `msvcr100.dll`，在未安装 VC++ 2010 x64 运行库的工控机上会报“加载 DLL 失败”。

如果这个包在工控机仍提示 DLL 缺失，优先查：

```text
1. 是否只复制了 exe，而不是整个文件夹
2. 工控机是否 Windows 10 64 位
3. 先运行 检查DLL环境.cmd
4. msvcr100.dll 是否存在且为 x64
5. Native LoadLibrary 检查是否 OK
6. 厂家板卡驱动是否安装
7. 杀软/权限是否拦截 DLL
8. appsettings.local.json 是否被旧包覆盖成错误配置
```

---

## Step 3:通讯配置

通讯页确认：

```text
PC IP
主卡 IP
扩展卡数 = 2
扫描周期
心跳周期
仿真模式 = 关闭
```

连接成功只代表打开板卡成功，不代表输出链路已经验证。

---

## Step 4:X 输入验证

在 IO 点位页观察主卡 X：

| 点位 | 动作 |
|------|------|
| X0 | 遮挡/触发线头第一工位光电 |
| X1 | 触发行程开关 |
| X2/X3 | 验证线头上下限 |
| X4 | 验证线尾 AGV 信号 |
| X5 | 验证线尾升降台行程 |
| X6/X7 | 验证线尾上下限 |
| X8 | 验证线头防呆光电 |

记录任何和 UI 名称不一致的点，回填 `io-points.json`。

---

## Step 5:Y 点动输出验证

### 先确认手动模式

顶部必须显示：

```text
手动 ON
```

状态区应提示：

```text
手动模式已开启 · 点动按钮需按住输出，松开断开
```

### 点动语义

```text
按住动作按钮 -> Y 点亮 / 输出 ON
松开动作按钮 -> Y 点灭 / 输出 OFF
```

不要用“轻点一下”判断有没有动作。

### 动作组验证

| 动作 | 预期 |
|------|------|
| 线头电缸上升 | Y0 + Y1 同时 ON，松开同时 OFF |
| 线头电缸下降 | Y2 + Y3 同时 ON，松开同时 OFF |
| 线尾电缸上升 | Y4 + Y5 同时 ON，松开同时 OFF |
| 线尾电缸下降 | Y6 + Y7 同时 ON，松开同时 OFF |
| 线头气缸伸出 | X8 无信号时 Y8 可点动；X8 有信号时禁止，若已输出应立即 OFF |
| 线尾气缸伸出 | Y9 ON/OFF |

---

## Step 6:互锁验证

```text
按住线头电缸上升
同时尝试线头电缸下降
=> 应拒绝，提示先复位/松开当前方向
```

线尾同理。

---

## Step 7:现场硬件排查顺序

如果连接成功但输出没动作，按这个顺序排查：

```text
1. 手动模式是否 ON
2. 触摸屏点击手动/连接/复位是否有 UI 反馈
3. 是否按住而不是轻点
4. UI Y 点是否变色
5. 状态栏是否有写入失败
6. 输出反馈是否读回 ON
7. 中间继电器线圈电源是否和板卡输出侧匹配
8. 板卡电源和电磁阀/继电器电源是否分路正确
9. 是否需要 COM/公共端，或该板卡输出方式不同
10. 是否同步点必须两点同时通
11. 是否点错了原始 Y 格子，而不是动作按钮
```

本项目现场已经确认过一个关键坑：板卡电源和电磁阀电源不是一路时，会出现输出逻辑和实际动作不一致。

---

## Step 8:记录回填

现场确认后必须回填：

```text
src\PipelineControl.UI\Resources\io-points.json
project-progress.md
implementation-notes.md
Obsidian 项目文件夹
```

如果是可复用问题，新增 Pattern / Postmortem。

## 关联

- [[Project 中央净软线]]
- [[Postmortem 中央净软线-工控机手动输出无反应]]
- [[Pattern 工控-WPF触摸屏点动输出三事件兜底]]
- [[Pattern 工控-同步输出动作组与互锁]]
- [[Pattern 工控-点位映射不要硬编码]]
