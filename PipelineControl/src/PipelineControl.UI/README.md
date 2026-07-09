# PipelineControl.UI

WPF 启动项目，承载 `Views`、`ViewModels`、`Themes`、`Bootstrap`、`App.xaml` 和 `appsettings.json`。

该项目负责组合 DI、加载配置、启动 MainWindow 和合并全局资源字典。UI 不直接引用 `PipelineControl.Drivers.Bopai` 或 `PipelineControl.Drivers.Simulator`。
