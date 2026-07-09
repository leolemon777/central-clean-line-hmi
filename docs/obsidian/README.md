# 中央净软线 Obsidian 笔记源

本目录是中央净软线项目的 Obsidian 知识库源文件。它不是单个项目说明，而是按“索引 / Project / Postmortem / Pattern / Playbook”拆分，方便长期接手和复用。

## 目标 vault 结构

```text
E:\Desktop\PLC知识库\
  中央净软线\
    索引 中央净软线.md
    Project 中央净软线.md
    Postmortem 中央净软线-工控机手动输出无反应.md
    Pattern 工控-WPF触摸屏点动输出三事件兜底.md
    Pattern 工控-同步输出动作组与互锁.md
    Pattern 工控-点位映射不要硬编码.md
    Playbook 中央净软线工控机部署与IO测试.md
```

## 维护规则

- `索引 中央净软线.md` 是 Obsidian 入口。
- `Project 中央净软线.md` 写项目判断、当前状态和接手路径。
- `Postmortem ...` 记录已经踩过的现场问题。
- `Pattern ...` 记录可复用的工控开发模式。
- `Playbook ...` 写上机部署、IO 测试和排查清单。

## 必须保留的源码指向

项目首页和索引必须指向当前源码根目录：

```text
E:\Desktop\开发项目汇总\中央净软线
```

不要把 `PipelineControl\` 早期骨架或 `C#例程源代码及库文件\` 厂家 demo 当成当前发布源码。
