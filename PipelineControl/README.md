# 中央净软线控制台

中央净软线控制台是一个基于 .NET 8 WPF 的工业上位机项目骨架，用 PC + 博派以太网 IO 卡替代 PLC 控制中央净软线流水线。

当前版本仅完成 T01 骨架：解决方案分层、项目引用、DI 容器、配置加载、Serilog 日志、资源字典框架和占位 MainWindow。未实现登录、Shell、业务页面、数据库迁移、图表或驱动调用。

## 目录结构

```text
PipelineControl/
  PipelineControl.sln
  src/
    PipelineControl.UI/
    PipelineControl.Application/
    PipelineControl.Domain/
    PipelineControl.Infrastructure/
    PipelineControl.Drivers.Abstractions/
    PipelineControl.Drivers.Bopai/
    PipelineControl.Drivers.Simulator/
    PipelineControl.Shared/
  tests/
    PipelineControl.Application.Tests/
    PipelineControl.Domain.Tests/
    PipelineControl.Drivers.Bopai.Tests/
  vendor/
  docs/
  Directory.Build.props
  .editorconfig
  .gitignore
```

## 启动

```powershell
dotnet restore
dotnet build
dotnet run --project src/PipelineControl.UI
```

启动后会显示占位窗口「中央净软线控制台 · 骨架已就绪」。点击「显示版本信息」会显示 `App` 配置、版本号和当前 UI 项目 NuGet 包列表。

## 切换 Simulator / 真实硬件

默认配置在 `src/PipelineControl.UI/appsettings.json`：

```json
"BopaiCard": {
  "UseSimulator": true
}
```

开发机本地可新增 `appsettings.local.json` 覆盖：

```json
{
  "BopaiCard": {
    "UseSimulator": false
  }
}
```

T01 只注册驱动选择入口，不实现博派卡或模拟器业务代码。真实硬件实现后仍通过 `BopaiCard.UseSimulator` 切换。

## 添加新页面

1. 在 `src/PipelineControl.UI/Views/` 添加页面或窗口。
2. 在 `src/PipelineControl.UI/ViewModels/` 添加对应 ViewModel。
3. 在 `src/PipelineControl.UI/Bootstrap/ServiceRegistration.cs` 注册 View 和 ViewModel。
4. 公共颜色放到 `Themes/Colors.xaml`，字号放到 `Themes/Typography.xaml`，通用控件样式放到 `Themes/ControlStyles.xaml`。
5. 页面业务调用进入 `PipelineControl.Application`，不要让 UI 直接引用 `PipelineControl.Drivers.Bopai` 或 `PipelineControl.Drivers.Simulator`。

## 日志

日志使用 Serilog 接入 `Microsoft.Extensions.Logging`。应用代码注入 `ILogger<T>`，不要直接调用 `Serilog.Log` 静态类。

默认日志输出：

```text
logs/app-{date}.log
```

## vendor

`vendor/` 用于预留厂家 DLL、Demo、说明文档等资料路径。T01 不引用任何厂家 DLL。
