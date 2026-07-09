# PipelineControl.Drivers.Abstractions

硬件抽象接口层。

当前 T01 只保留驱动选择抽象，用于根据 `BopaiCard.UseSimulator` 标识当前选用 Simulator 或 Bopai。后续具体 IO、AD、DA 接口在这里扩展。
