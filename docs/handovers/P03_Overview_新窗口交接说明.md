# P03 Overview 新窗口交接说明

## 结论

当前文件夹是 WPF 工业上位机项目「中央净软线控制台」：

```text
E:\Users\29376\Desktop\中央净软线
```

P02 已完成 Shell 框架，P03 本轮已完成「总览首页 Overview」并接入 Shell 内容区。后续新窗口继续做页面设计时，优先读取本文件，再读取关键源码文件。

当前主工程：

```text
E:\Users\29376\Desktop\中央净软线\src\PipelineControl.UI
```

本项目当前不是 git 仓库，不要依赖 `git diff`。

## 当前完成状态

### P02 Shell 已完成

Shell 包含：

- 顶部一级导航 `TopNavBar`
- 左侧二级导航 `SideNav`
- 中央内容承载区 `ContentControl`
- 底部状态栏 `StatusBar`
- 统一占位页 `PlaceholderPage`
- Mock 状态数据、导航切换、底部时钟

Shell 页面位置：

```text
src\PipelineControl.UI\Views\Shell\MainWindow.xaml
src\PipelineControl.UI\ViewModels\Shell\MainWindowViewModel.cs
src\PipelineControl.UI\Services\Navigation\PageNavigator.cs
```

注意：业务页面应该接入 Shell 的中央 `ContentControl`，不要重做顶部、左侧、底部。

### P03 Overview 已完成

总览首页已经实现并作为开机后第一屏加载：

```text
src\PipelineControl.UI\Views\Pages\Overview\OverviewPage.xaml
src\PipelineControl.UI\ViewModels\Pages\Overview\OverviewViewModel.cs
```

`MainWindowViewModel` 中「总览首页」的 `PageKey` 已改为：

```text
Overview
```

`PageNavigator` 已注册：

```csharp
["Overview"] = typeof(OverviewPage)
```

`PageNavigator` 现在只会给 `PlaceholderPage` 设置 `DataContext = pageKey`，不会覆盖真实业务页面的 ViewModel。

## P03 新增/修改文件

### 页面

```text
src\PipelineControl.UI\Views\Pages\Overview\OverviewPage.xaml
src\PipelineControl.UI\Views\Pages\Overview\OverviewPage.xaml.cs
```

`OverviewPage.xaml.cs` 只保留 `InitializeComponent()`。

### ViewModel

```text
src\PipelineControl.UI\ViewModels\Pages\Overview\OverviewViewModel.cs
```

使用 `CommunityToolkit.Mvvm`：

- `[ObservableProperty]`
- `[RelayCommand]`
- `ObservableObject`

P03 所有数据都是 Mock，由构造函数初始化。

命令目前只 `Debug.WriteLine`，不做真实业务动作：

- `OpenDashboardWindowCommand`
- `NavigateToIoMonitorCommand`
- `OpenRecipeSelectorCommand`
- `ExportShiftReportCommand`
- `ViewAllAlarmsCommand`

### 模型

```text
src\PipelineControl.UI\ViewModels\Pages\Overview\Models\ShiftSummary.cs
src\PipelineControl.UI\ViewModels\Pages\Overview\Models\KpiCard.cs
src\PipelineControl.UI\ViewModels\Pages\Overview\Models\StationStatus.cs
src\PipelineControl.UI\ViewModels\Pages\Overview\Models\AlarmDigest.cs
src\PipelineControl.UI\ViewModels\Pages\Overview\Models\DeviceHealth.cs
src\PipelineControl.UI\ViewModels\Pages\Overview\Models\HourlyOutput.cs
```

关键 enum：

- `KpiVisualType`
- `StationState`
- `AlarmSeverity`
- `HourlyOutputLevel`

### 可复用控件

```text
src\PipelineControl.UI\Views\Pages\Overview\Controls\KpiCardView.xaml
src\PipelineControl.UI\Views\Pages\Overview\Controls\KpiCardView.xaml.cs
src\PipelineControl.UI\Views\Pages\Overview\Controls\StationCardView.xaml
src\PipelineControl.UI\Views\Pages\Overview\Controls\StationCardView.xaml.cs
src\PipelineControl.UI\Views\Pages\Overview\Controls\AlarmDigestItemView.xaml
src\PipelineControl.UI\Views\Pages\Overview\Controls\AlarmDigestItemView.xaml.cs
```

这三个控件用 `DependencyProperty` 暴露参数，方便后续页面复用。控件 `.xaml.cs` 只放 DP 声明和必要显示转换，不放业务逻辑。

### 转换器

```text
src\PipelineControl.UI\Views\Pages\Overview\Converters\ResponsiveMaxWidthConverter.cs
```

用于窄窗口下限制 P03 视觉块最大宽度，避免低于目标分辨率时横向溢出。

### 资源字典

已修改：

```text
src\PipelineControl.UI\Themes\Colors.xaml
src\PipelineControl.UI\Themes\Typography.xaml
```

新增/补充了 P03 需要的语义颜色与字号资源。后续页面继续遵守：

- 颜色只写在 `Colors.xaml`
- 页面 XAML 禁止硬编码 `#XXXXXX`
- 字号只写在 `Typography.xaml`
- 页面 XAML 禁止裸写 `FontSize="12"` 这类数字

### 导航/DI 接入

已修改：

```text
src\PipelineControl.UI\App.xaml.cs
src\PipelineControl.UI\Services\Navigation\PageNavigator.cs
src\PipelineControl.UI\ViewModels\Shell\MainWindowViewModel.cs
src\PipelineControl.UI\Views\Shell\MainWindow.xaml
```

`MainWindow.xaml` 中 `ContentControl` 已补：

```xml
HorizontalContentAlignment="Stretch"
VerticalContentAlignment="Stretch"
```

这是为了让业务子页面服从 Shell 内容区宽度。

### 测试

新增测试项目：

```text
tests\PipelineControl.UI.Tests\PipelineControl.UI.Tests.csproj
tests\PipelineControl.UI.Tests\OverviewViewModelTests.cs
tests\PipelineControl.UI.Tests\PageNavigatorOverviewTests.cs
```

测试覆盖：

- P03 Mock 数据初始化
- 报警空状态 `HasRecentAlarms`
- `PageNavigator.NavigateTo("Overview")` 会解析到 `OverviewPage`

## 当前 P03 页面内容

页面是 Shell 内容承载区里的子页面，不包含顶部导航、左侧导航、底部状态栏。

布局：

1. 标题栏：早班 + 当前配方
2. KPI 四宫格
3. 工艺流程卡 + 活跃报警卡
4. 设备健康 + 小时产量 + 快捷操作

Mock 内容：

- 早班 A 班组
- 日期：`2025-11-13 周四`
- `08:00` 开班
- 已运行 `2 小时 24 分`
- 配方：`软化模式 / A-Soft-02`
- 王工 `08:14` 加载
- KPI：
  - 产量 `847 / 1200`
  - 节拍 `4.82 s`
  - 良率 `98.3%`
  - OEE `82.4%`
- 工位：
  - 上料运行
  - 缓存在料 3
  - 工艺需复位
  - 下料待机
- 报警：
  - 1 紧急
  - 2 重要
  - 0 一般
- 板卡：
  - 主卡正常
  - 扩展1正常
  - 扩展2正常
- 小时产量：8 根柱子

## 设计约束

后续继续设计页面时请遵守：

1. 不要重做 Shell。
2. 不要引入 `MaterialDesignInXaml`、`Prism`、`MahApps`。
3. 不要使用 `WindowChrome` 自定义标题栏。
4. 不要连接 IO 卡。
5. 不要引入数据库。
6. 当前阶段全部数据优先 Mock。
7. 页面颜色走 `Themes/Colors.xaml`。
8. 页面字号走 `Themes/Typography.xaml`。
9. View 的 `.xaml.cs` 不写业务逻辑。
10. 优先使用 `CommunityToolkit.Mvvm` 源生成器。

## 已验证结果

最后一次验证命令：

```powershell
dotnet test tests\PipelineControl.UI.Tests\PipelineControl.UI.Tests.csproj -v:minimal
```

结果：

```text
已通过
失败: 0，通过: 3，已跳过: 0，总计: 3
```

最后一次构建命令：

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

## 重要注意

1. 本轮为了兼容低于目标宽度的启动环境，`OverviewPage` 外层用了 `Grid + ScrollViewer`，内容宽度绑定外层 `OverviewRoot.ActualWidth`，避免 `ScrollViewer` 横向无限测量导致内容撑出屏幕。
2. P03 设计稿目标是 1920×1080；当前本机截图环境约 1295×767，可运行但视觉密度会更紧。
3. `ResponsiveMaxWidthConverter` 目前只做窄窗口保护，不是业务逻辑。
4. 如果后续用户要求严格复刻 1920 设计稿，请以 `E:\Users\29376\Desktop\P03_overview_homepage.html` 作为视觉基准，但仍要把实现放在 Shell 内容区。

## 新窗口建议第一步

新窗口继续时，建议先让 Codex 读取：

```text
E:\Users\29376\Desktop\中央净软线\P03_Overview_新窗口交接说明.md
```

然后读取：

```text
src\PipelineControl.UI\README.md
src\PipelineControl.UI\ViewModels\Shell\MainWindowViewModel.cs
src\PipelineControl.UI\Services\Navigation\PageNavigator.cs
src\PipelineControl.UI\Views\Pages\Overview\OverviewPage.xaml
src\PipelineControl.UI\ViewModels\Pages\Overview\OverviewViewModel.cs
```

如果下一步做 P04，请让新窗口先输出执行计划，确认后再写代码。
