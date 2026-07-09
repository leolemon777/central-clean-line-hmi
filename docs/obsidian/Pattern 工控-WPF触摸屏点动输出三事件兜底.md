---
type: pattern
domain: 工控
tags:
  - 工控
  - WPF
  - 触摸屏
  - 点动
  - 可复用
---

# Pattern:WPF 触摸屏点动输出三事件兜底

> 适用：WPF 工控 HMI 里的点动按钮、长按输出、按住开阀、按住电缸动作。

## 问题

开发电脑上鼠标点击正常，部署到工控机触摸屏后，按钮表现为：

```text
按了没反应
只闪一下
偶尔触发
松手没有关断
```

这不一定是 IO 或 PLC 问题，可能是 WPF 输入事件链差异。

## 核心事实

WPF 里触摸屏可能走三类事件：

| 输入路径 | 常见设备 |
|----------|----------|
| Mouse | 普通鼠标、部分触摸模拟鼠标 |
| Touch | 支持 Windows Touch 的屏 |
| Stylus | 很多工业触摸屏、手写笔兼容层 |

只绑定 `MouseDown/MouseUp` 不够。

## 推荐实现

点动按钮要覆盖：

```text
PreviewMouseLeftButtonDown
PreviewMouseLeftButtonUp
LostMouseCapture

PreviewTouchDown
PreviewTouchUp
LostTouchCapture

PreviewStylusDown
PreviewStylusUp
LostStylusCapture
```

普通命令按钮也不要只依赖 `Click`：

```text
PreviewTouchDown
PreviewTouchUp
PreviewStylusDown
PreviewStylusUp
```

在 `PreviewTouchUp` / `PreviewStylusUp` 中主动执行 `ButtonBase.Command`；如果控件是 `ToggleButton`，先切换 `IsChecked` 再执行命令，保证触摸屏上 `手动 ON/OFF` 这类模式按钮有明确反馈。

## 状态机原则

```text
Down 成功 -> 加入 activeJogGroups -> 写 ON
Up / LostCapture -> 从 activeJogGroups 移除 -> 写 OFF
Down 失败 -> 不加入 activeJogGroups
```

关键是：只有成功进入 active 集合的按钮，才允许在松开时 OFF，避免重复 OFF；但一旦进入，任何丢失捕获都要 OFF。

## 为什么用 Preview 事件

普通 Button 的 `Click` 语义是松开后触发，不适合点动。

点动需要在按下瞬间 ON，不能等 Click。

```text
Click: 松手后触发
PreviewDown: 按下时触发
```

## UI 提示要求

不要只写“点动输出”，现场不一定理解。

至少有一处明确显示：

```text
按住输出，松开断开
```

手动模式也要大按钮显示 ON/OFF，而不是小复选框。

## 反模式

| 反模式 | 后果 |
|--------|------|
| 用 Click 做点动 | 变成点击切换或一闪即断 |
| 只处理 Mouse | 触摸屏可能无反应 |
| 顶部模式按钮只依赖 Click | 触摸屏可能无法开启手动模式 |
| 不处理 LostCapture | 手指滑出或窗口失焦后可能卡输出 |
| 手动模式用小 CheckBox | 现场误以为已开启 |
| 状态只在日志里 | 操作员看不到 |

## 来源项目

- [[Project 中央净软线]]
- [[Postmortem 中央净软线-工控机手动输出无反应]]
