# P04 IO 监视页新窗口交接说明

## 结论

当前文件夹是 WPF 工业上位机项目「中央净软线控制台」：

```text
E:\Users\29376\Desktop\中央净软线
```

P02 Shell 已完成，P03 Overview 已完成，P04「IO 监视调试页」已初版实现并接入 Shell 内容区。

当前主工程：

```text
E:\Users\29376\Desktop\中央净软线\src\PipelineControl.UI
```

本项目当前不是 git 仓库，不要依赖 `git diff`。

## 新窗口第一步

新窗口继续设计或开发时，优先读取本文件，然后读取：

```text
P03_Overview_新窗口交接说明.md
src\PipelineControl.UI\Themes\Colors.xaml
src\PipelineControl.UI\Themes\Typography.xaml
src\PipelineControl.UI\ViewModels\Shell\MainWindowViewModel.cs
src\PipelineControl.UI\Services\Navigation\PageNavigator.cs
src\PipelineControl.UI\Views\Pages\IoMonitor\IoMonitorPage.xaml
src\PipelineControl.UI\ViewModels\Pages\IoMonitor\IoMonitorViewModel.cs
```

如果用户继续给 P05 或下一页提示词，先基于现有 Shell 内容区继续，不要重做 Shell。

## 当前完成状态

### P02 Shell

Shell 已包含：

- 顶部一级导航 `TopNavBar`
- 左侧二级导航 `SideNav`
- 中央内容承载区 `ContentControl`
- 底部状态栏 `StatusBar`
- 统一占位页 `PlaceholderPage`

业务页面必须显示在 `MainWindow.xaml` 的中央 `ContentControl`，不要重做顶部、左侧、底部。

### P03 Overview

总览首页已完成，pageKey：

```text
Overview
```

入口是「总览 -> 总览首页」。

### P04 IO Monitor

IO 监视页已实现，pageKey：

```text
IoMonitor
```

入口是：

```text
顶部「点位」 -> 左侧「IO 监视」
```

`PageNavigator` 已注册：

```csharp
["IoMonitor"] = typeof(IoMonitorPage)
```

`App.xaml.cs` 已注册：

- `IoMonitorViewModel`
- `IoMonitorPage`

## P04 参考设计稿

用户给的 HTML 设计稿：

```text
E:\Users\29376\Desktop\P04_io_monitor_page.html
```

P03 参考 HTML：

```text
E:\Users\29376\Desktop\P03_overview_homepage.html
```

用户刚反馈过颜色问题：WPF 背景看起来比 HTML 偏黄。已修正 `Colors.xaml`：

- `Bg.Surface` 从 `#F5F0E2` 改为 `#FAF7F0`
- `Bg.Hover` 从 `#EFEAD8` 改为 `#F1EFE8`

原因：HTML 的主内容背景是 `#FAF7F0`，Shell 顶栏/左栏/底栏之前使用更黄的 `#F5F0E2`，导致整体视觉偏黄。

## P04 已创建文件

### 页面

```text
src\PipelineControl.UI\Views\Pages\IoMonitor\IoMonitorPage.xaml
src\PipelineControl.UI\Views\Pages\IoMonitor\IoMonitorPage.xaml.cs
```

`IoMonitorPage.xaml.cs` 只保留 `InitializeComponent()`。

### ViewModel

```text
src\PipelineControl.UI\ViewModels\Pages\IoMonitor\IoMonitorViewModel.cs
```

使用 `CommunityToolkit.Mvvm`：

- `[ObservableProperty]`
- `[RelayCommand]`
- `ObservableObject`

当前全部是 Mock 数据，不连接真实 IO 卡，不接数据库。

### 模型

```text
src\PipelineControl.UI\ViewModels\Pages\IoMonitor\Models\IoModuleViewModel.cs
src\PipelineControl.UI\ViewModels\Pages\IoMonitor\Models\IoPointViewModel.cs
src\PipelineControl.UI\ViewModels\Pages\IoMonitor\Models\ForcedPointViewModel.cs
src\PipelineControl.UI\ViewModels\Pages\IoMonitor\Models\IoEventViewModel.cs
src\PipelineControl.UI\ViewModels\Pages\IoMonitor\Models\EdgeSample.cs
```

### 可复用控件

```text
src\PipelineControl.UI\Views\Pages\IoMonitor\Controls\IoBitGridView.xaml
src\PipelineControl.UI\Views\Pages\IoMonitor\Controls\IoBitGridView.xaml.cs
src\PipelineControl.UI\Views\Pages\IoMonitor\Controls\IoBitCellView.xaml
src\PipelineControl.UI\Views\Pages\IoMonitor\Controls\IoBitCellView.xaml.cs
src\PipelineControl.UI\Views\Pages\IoMonitor\Controls\EdgeSparklineView.xaml
src\PipelineControl.UI\Views\Pages\IoMonitor\Controls\EdgeSparklineView.xaml.cs
```

控件说明：

- `IoBitCellView`：单个位指示灯，DP 暴露 `BitIndex`、`IsOn`、`IsOutput`、`IsForced`、`IsSelected`、`CellTooltip`、`ClickCommand`、`ShowBitText`
- `IoBitGridView`：模块位网格，DP 暴露 `ModuleLabel`、`RegisterValueHex`、`Columns`、`Points`、`IsOutput`、`SelectedBit`、`SelectionChangedCommand`
- `EdgeSparklineView`：边沿方波小图，用 `StreamGeometry` 生成 WPF `Path.Data`

## P04 当前页面结构

页面是 Shell 内容承载区里的子页面，不包含顶部导航、左侧导航、底部状态栏。

布局：

1. 标题栏：`DIAGNOSTICS / IO MONITOR` + 搜索框 + 扫描周期 + 手动模式 + 急停
2. Digital inputs 卡：主卡 16 位、扩展1 24 位、扩展2 24 位
3. Digital outputs 卡：主卡 16 位、扩展1 24 位、扩展2 24 位
4. Selected point 点位详情卡：当前点、描述、当前状态、方波、强制按钮
5. Forced outputs 强制清单卡：3 条强制输出、解除按钮、解除全部强制
6. Event stream 事件流：最近事件列表

## P04 Mock 数据

当前初始化数据按任务提示词实现：

- 输入模块 3 个，共 64 DI
- 输出模块 3 个，共 64 DO
- 选中点：`X005 / Station1.ClampOk`
- 强制清单：`Y000`、`Y007`、`Y028`
- 初始强制数量：3
- 初始事件流：5 条
- `ScanCycleMs = 10.2`
- `ScanJitterMs = 1.2`
- `OperationModeText = "手动模式"`
- `CanForce = true`

注意：测试里固定断言了当前初始统计：

```text
InputOnCount = 28
InputOffCount = 36
OutputOnCount = 12
OutputOffCount = 52
ForcedCount = 3
```

如果后续修改 Mock 点位分布，要同步更新测试。

## 命令行为

当前命令：

- `SelectPointCommand`：点击位点后刷新详情区
- `ForceOnCommand`：仅选中输出点且 `CanForce=true` 可用，命令体只 `Debug.WriteLine`
- `ForceOffCommand`：同上
- `ReleaseForceCommand`：仅选中已强制输出点可用，命令体只 `Debug.WriteLine`
- `ReleaseForcedPointCommand`：从强制清单移除单点，并更新强制数量
- `ReleaseAllForcedCommand`：清空全部强制状态和强制清单
- `EmergencyStopCommand`：占位 `Debug.WriteLine`
- `ExportEventsCommand`：占位 `Debug.WriteLine`

## 自动刷新

`IoMonitorViewModel` 内部用了 `DispatcherTimer`：

```text
Interval = 100ms
```

当前模拟方式：

- 每隔几次 tick 随机翻转少量点位
- 不重建点位集合，只更新 `IoPointViewModel.IsOn`
- 点位变化后刷新模块寄存器值和统计
- 选中点变化时刷新详情状态和方波样本

这是 UI 快照刷新模拟，不是真实 IO 扫描。

## 测试文件

已新增：

```text
tests\PipelineControl.UI.Tests\IoMonitorViewModelTests.cs
tests\PipelineControl.UI.Tests\IoMonitorPageSmokeTests.cs
```

已修改：

```text
tests\PipelineControl.UI.Tests\PageNavigatorOverviewTests.cs
```

测试覆盖：

- P04 Mock 初始化
- 输入/输出点强制按钮 CanExecute 规则
- 解除全部强制
- `PageNavigator.NavigateTo("IoMonitor")` 会解析到 `IoMonitorPage`
- 真实创建 `IoMonitorPage + IoMonitorViewModel` 并加载资源、Measure/Arrange

## 最近验证结果

最后一次测试：

```powershell
dotnet test tests\PipelineControl.UI.Tests\PipelineControl.UI.Tests.csproj -v:minimal
```

结果：

```text
已通过
失败: 0，通过: 8，已跳过: 0，总计: 8
```

最后一次构建：

```powershell
dotnet build CentralCleanLineHmi.sln -v:minimal
```

结果：

```text
已成功生成。
0 个警告
0 个错误
```

硬编码颜色检查：

```powershell
Get-ChildItem -LiteralPath 'src\PipelineControl.UI' -Recurse -Filter *.xaml |
  Where-Object { $_.FullName -notlike '*\Themes\Colors.xaml' } |
  Select-String -Pattern '#[0-9A-Fa-f]{3,8}'
```

结果为空。

裸字号/禁用框架检查：

```powershell
rg -n "FontSize=\"[0-9]|CharacterSpacing=|WindowChrome|MaterialDesign|MahApps|Prism" 'src\PipelineControl.UI' -g '*.xaml' -g '*.cs'
```

结果为空。

启动验证：

```text
PipelineControl.UI.exe
Responding = True
MainWindowTitle = 中央净软线控制台
```

## 后续设计约束

继续开发时遵守：

1. 不要重做 Shell。
2. 不要引入 `MaterialDesignInXaml`、`Prism`、`MahApps`。
3. 不要使用 `WindowChrome` 自定义标题栏。
4. 不要连接 IO 卡。
5. 不要引入数据库。
6. 当前阶段数据优先 Mock。
7. 页面颜色走 `Themes/Colors.xaml`。
8. 页面字号走 `Themes/Typography.xaml`。
9. View 的 `.xaml.cs` 不写业务逻辑。
10. 优先使用 `CommunityToolkit.Mvvm` 源生成器。
11. 新增页面要接入 `PageNavigator`，并在 `App.xaml.cs` 注册页面和 ViewModel。

## 当前已知注意点

1. WPF 没有 CSS 的 `aspect-ratio`，当前 IO cell 用固定 `32x32` 和 `UniformGrid` 实现。
2. `IoBitGridView` 的 `ItemsPanelTemplate` 内部绑定 `Columns` 时，通过 `ItemsControl.Tag` 转发，避免模板命名作用域问题。
3. `EdgeSparklineView` 用 `StreamGeometry`，不是外部图表库。
4. 当前 P04 视觉还可以继续精修，尤其是 1366x768 低分辨率下的密度和卡片高度。
5. 如果用户说“还是不像 HTML”，下一步优先对照 `P04_io_monitor_page.html` 调整具体间距、字号、卡片高度，而不是改架构。

## 新窗口建议回复方式

如果用户新窗口发“继续 P05”或“继续下一个页面”，建议先说：

```text
我已读取 P04 交接说明。当前 Shell/P03/P04 已接好，后续我会继续在 Shell 内容区做新页面，不重做外壳；颜色和字号继续走资源字典。
```

然后再按用户的新提示词输出计划或直接执行。
