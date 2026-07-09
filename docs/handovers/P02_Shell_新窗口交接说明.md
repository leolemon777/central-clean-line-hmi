# P02 Shell 新窗口交接说明

## 结论

当前文件夹是 WPF 工业上位机项目「中央净软线控制台」：

```text
E:\Users\29376\Desktop\中央净软线
```

本轮已完成 `P02 主操作窗口 Shell 框架`，项目主工程在：

```text
E:\Users\29376\Desktop\中央净软线\src\PipelineControl.UI
```

当前 Shell 已包含：

- 顶部一级导航栏 `TopNavBar`
- 左侧二级导航 `SideNav`
- 中央内容承载区 `ContentControl`
- 底部状态栏 `StatusBar`
- 统一占位页 `PlaceholderPage`
- Mock 状态数据、导航切换、底部时钟
- README 接入说明

业务页面还没有做，后续页面都先接入到 Shell 的内容区。

## 参考设计稿

用户给的 P02 参考 HTML 在：

```text
C:\Users\29376\AppData\Local\Temp\MicrosoftEdgeDownloads\52f20d14-84af-41b0-830a-5e952434d986\P02_main_shell_framework.html
```

浏览器工具不能直接打开这个 `file://`，当时是直接读取 HTML/CSS 内容作为视觉基准。设计核心是米白工业 HMI 风格：

- 顶部 `56px`
- 左侧 `240px`
- 底部 `32px`
- 内容区背景 `#FAF7F0`
- 顶/侧/底背景 `#F5F0E2`
- 主选中底色 `#2C2C2A`
- 主选中文字 `#F5C4B3`

颜色已集中放入 `Themes/Colors.xaml`，其它 XAML 不写硬编码颜色。

## 已创建/修改的关键文件

### 应用入口

- `src/PipelineControl.UI/App.xaml`
- `src/PipelineControl.UI/App.xaml.cs`

`App.xaml` 通过 `MergedDictionaries` 引入：

- `Themes/Colors.xaml`
- `Themes/Typography.xaml`
- `Themes/ControlStyles.xaml`

`App.xaml.cs` 注册：

- `PageNavigator`
- `IPageNavigator`
- `StatusBarViewModel`
- `MainWindowViewModel`
- `PlaceholderPage`
- `MainWindow`

### Shell View

- `src/PipelineControl.UI/Views/Shell/MainWindow.xaml`
- `src/PipelineControl.UI/Views/Shell/MainWindow.xaml.cs`
- `src/PipelineControl.UI/Views/Shell/PlaceholderPage.xaml`
- `src/PipelineControl.UI/Views/Shell/PlaceholderPage.xaml.cs`
- `src/PipelineControl.UI/Views/Shell/Components/TopNavBar.xaml`
- `src/PipelineControl.UI/Views/Shell/Components/TopNavBar.xaml.cs`
- `src/PipelineControl.UI/Views/Shell/Components/SideNav.xaml`
- `src/PipelineControl.UI/Views/Shell/Components/SideNav.xaml.cs`
- `src/PipelineControl.UI/Views/Shell/Components/StatusBar.xaml`
- `src/PipelineControl.UI/Views/Shell/Components/StatusBar.xaml.cs`

所有 `.xaml.cs` 只保留 `InitializeComponent()`，没有业务逻辑。

### ViewModel 和模型

- `src/PipelineControl.UI/ViewModels/Shell/MainWindowViewModel.cs`
- `src/PipelineControl.UI/ViewModels/Shell/StatusBarViewModel.cs`
- `src/PipelineControl.UI/ViewModels/Shell/Models/TopNavItem.cs`
- `src/PipelineControl.UI/ViewModels/Shell/Models/SideNavGroup.cs`
- `src/PipelineControl.UI/ViewModels/Shell/Models/SideNavItem.cs`

使用 `CommunityToolkit.Mvvm`：

- `[ObservableProperty]`
- `[RelayCommand]`
- `ObservableObject`
- `IRelayCommand` 由源生成器生成

### 页面导航

- `src/PipelineControl.UI/Services/Navigation/IPageNavigator.cs`
- `src/PipelineControl.UI/Services/Navigation/PageNavigator.cs`

`PageNavigator` 当前把所有 `pageKey` 统一路由到 `PlaceholderPage`，并把 `pageKey` 设为占位页 `DataContext`，所以占位页会显示当前页面 key。

### 资源

- `src/PipelineControl.UI/Themes/Colors.xaml`
- `src/PipelineControl.UI/Themes/Typography.xaml`
- `src/PipelineControl.UI/Themes/ControlStyles.xaml`

注意：`Colors.xaml` 是颜色源头，里面有 `#XXXXXX` 是正常的；其它 XAML 不应该出现硬编码颜色。

### 文档

- `src/PipelineControl.UI/README.md`

里面说明了 Shell 结构和后续页面接入方式。

## 当前 Mock 数据

在 `MainWindowViewModel` / `StatusBarViewModel` 中：

- `UserName = "王工"`
- `UserRoleName = "工程师"`
- `ActiveAlarmCount = 3`
- `SystemRunningState = Running`
- `OperationMode = Manual`
- `MainCardOnline = true`
- `MainCardIp = "192.168.0.1"`
- `ExtCardsOnline = true`
- `ScanCycleMs = 10.2`
- `KeepAliveRemainMs = 980`

底部时钟由 `StatusBarViewModel` 内部 `DispatcherTimer` 每秒刷新。

## 当前导航结构

顶部一级导航共 9 个：

```text
总览 / 大屏 / 点位 / 手动 / 自动 / 报警 / 数据 / 配方 / 系统
```

点击一级导航会：

1. 切换顶部选中态
2. 替换左侧 `CurrentSideNavGroups`
3. 选择该一级导航下第一个普通页面项
4. 调用 `PageNavigator.NavigateTo(pageKey)`
5. 内容区显示占位页

其中：

- `总览` 左侧项：总览首页 / 设备健康 / 生产看板 / 班次摘要
- `系统` 左侧项：用户管理 / 系统设置 / 日志查看 / 诊断维护 / 关于
- 其它一级导航目前只放 1 到 2 个示例项
- 每组底部都有 `QUICK` 区：
  - 打开大屏窗口
  - 一键打包诊断

QUICK 命令目前只 `Console.WriteLine`，不实现大屏窗口和诊断包。

## 验证结果

最后一次验证命令：

```powershell
dotnet build CentralCleanLineHmi.sln -v:minimal
```

结果：

```text
已成功生成。
0 个警告
0 个错误
```

硬编码检查：

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

## 重要注意点

1. WPF `TextBlock` 不支持 `CharacterSpacing`，所以没有强行实现字间距。当前保持字体、字号、颜色资源化，未引入自定义控件。
2. 不要引入 `MaterialDesignInXaml`、`Prism`、`MahApps`。
3. 不要使用 `WindowChrome` 自定义标题栏，当前保留系统标题栏。
4. 不要连接 IO 卡、不要引入数据库，P02 只做 Shell。
5. 当前目录不是 git 仓库，不能依赖 git diff。

## 后续页面接入方式

如果下一步要做某个具体业务页面，比如手动页或点位页：

1. 创建业务页面 View，例如：

```text
src/PipelineControl.UI/Views/Manual/ManualControlPage.xaml
```

2. 创建对应 ViewModel，例如：

```text
src/PipelineControl.UI/ViewModels/Manual/ManualControlViewModel.cs
```

3. 在 `App.xaml.cs` 注册页面和 VM。

4. 在 `PageNavigator` 的 `pageRegistry` 中增加：

```csharp
["manual-control"] = typeof(ManualControlPage)
```

5. 确认 `MainWindowViewModel.CreatePrimaryGroup` 中的 `SideNavItem.PageKey` 与 `pageRegistry` 的 key 一致。

这样左侧菜单点击后就会把页面显示到 `MainWindow.xaml` 的 `ContentControl`。

## 新窗口建议第一步

新对话继续时，建议先让 Codex 读取：

```text
E:\Users\29376\Desktop\中央净软线\P02_Shell_新窗口交接说明.md
```

然后读取：

```text
src/PipelineControl.UI\README.md
src/PipelineControl.UI\ViewModels\Shell\MainWindowViewModel.cs
src/PipelineControl.UI\Views\Shell\MainWindow.xaml
```

如果要做下一步前端设计，优先在现有 Shell 的内容区内做页面，不要重做 Shell。
