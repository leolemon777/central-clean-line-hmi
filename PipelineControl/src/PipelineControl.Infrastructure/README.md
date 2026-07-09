# PipelineControl.Infrastructure

基础设施层，后续承载持久化、日志、事件总线和运行时适配。

当前 T01 提供 `AddInfrastructureServices()` 和驱动选择注册入口，不实现 EF Core Migrations 或具体业务存储。
