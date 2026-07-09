---
type: pattern
domain: 工控
tags:
  - 工控
  - IO
  - 点位
  - 维护性
  - 可复用
---

# Pattern:现场点位映射不要硬编码

> 适用：IO 点位会在现场反复确认和调整的上位机项目。

## 本项目事实

中央净软线的点位事实入口是：

```text
src\PipelineControl.UI\Resources\io-points.json
```

这里记录：

- pointNo
- name
- ioType
- moduleIndex
- bitIndex
- description
- isEnabled
- safeDefaultValue

## 为什么不能硬编码

现场点位必然变化：

```text
接线调整
端子换位
名称确认
扩展卡顺序调整
动作点增加
```

如果把点位名、module、bit 写死在 C# 里，每次现场确认都要改代码、重编译、重新部署，风险很高。

## 推荐边界

### JSON 负责事实

```text
X0 = 线头第一工位光电
moduleIndex = 0
bitIndex = 0
```

### C# 负责行为

```text
如何渲染点位
如何按动作组写输出
如何互锁
如何复位
```

### 文档负责解释

```text
为什么这个点这么命名
现场接线怎么确认
哪些点还没确认
```

## 本项目已确认点位

主卡 X：

| 点位 | 名称 |
|------|------|
| X0 | 线头第一工位光电 |
| X1 | 线头升降台行程开关 |
| X2 | 线头下限位开关 |
| X3 | 线头上限位开关 |
| X4 | 线尾AGV信号 |
| X5 | 线尾升降台行程开关 |
| X6 | 线尾下限开关 |
| X7 | 线尾上限开关 |
| X8 | 线头防呆光电 |

扩展卡1 Y0-Y9 已做动作组，见 [[Pattern 工控-同步输出动作组与互锁]]。

## 反模式

| 反模式 | 后果 |
|--------|------|
| 在 ViewModel 里写 `if X0 then xxx` | 点位变化要改代码 |
| 页面写死中文点位名 | JSON 更新后 UI 不同步 |
| C# 里散落 bitIndex | 现场换线难以维护 |
| 文档和 JSON 双重维护但不校验 | 两边漂移 |

## 来源项目

- [[Project 中央净软线]]
