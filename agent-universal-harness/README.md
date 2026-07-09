# Agent Universal Harness

一句话：先把完成标准变成机器能检查的东西，再让 Agent 在清晰边界内小步执行。

这不是一套“把所有文档一次填完”的模板。它是一套可生长的工程框架：任何项目都从 L0 开始，只有出现真实痛点或真实风险时才升层。

## 核心公理

1. 验证是地基。开工前先回答：怎么机械判断完成？答案必须落到 `scripts/verify.*`。
2. 流程按痛点生长。不预填一堆空文档，出现升层触发器再加结构。
3. 约束有生命周期。每条长期规则都要登记起因，能转成 `verify` 的文字规则优先转成机器检查。
4. 人类保留三类决策：什么算好、什么风险必须停、规格本身是否错了。

## 默认目录

```text
agent-universal-harness/
├─ README.md
├─ AGENTS.md
├─ PROJECT.md
├─ scripts/
│  ├─ verify.sh
│  ├─ verify.ps1
│  ├─ check-harness.sh
│  └─ check-harness.ps1
├─ tasks/
│  ├─ task-template.md
│  └─ current-task.md
├─ reports/
│  └─ progress.md
├─ harness/
│  ├─ loop.md
│  ├─ quality-gates.md
│  ├─ stop-rules.md
│  ├─ rules-ledger.md
│  ├─ review-checklist.md
│  ├─ prompt-template.md
│  └─ tool-permissions.md
├─ docs/
│  ├─ context-index.md
│  ├─ lessons-learned.md
│  ├─ verify-recipes.md
│  └─ adoption-playbook.md
└─ packs/
   ├─ l2/
   ├─ l3/
   ├─ evals/
   └─ subagents/
```

## 分层

| 层 | 名称 | 默认内容 | 什么时候用 |
|----|------|----------|------------|
| L0 | 地基 | `AGENTS.md` + `scripts/verify.*` + Git 纪律 | 所有项目，包括一次性脚本 |
| L1 | 骨架 | + `PROJECT.md` + 任务卡 + `reports/progress.md` | 超过一天、跨会话、需要交接 |
| L2 | 协作 | + stop rules、context index、review checklist、tool permissions、可选 requirements/architecture/acceptance | 多模块、多 Agent、多人、重复返工 |
| L3 | 高风险 | + security/safety/ops/evals/subagents/CI 强制和审计记录 | 生产数据、真实硬件、资金、隐私、合规、对外写操作 |

## 升层触发器

L0 到 L1：

- 项目会跨多个会话继续。
- 你第一次想不起来上次做到哪。
- 任务开始需要明确“不做什么”。

L1 到 L2：

- Agent 第二次因为需求歧义返工。
- 出现第二个模块，且边界被踩过一次。
- 需要多人或多个 Agent 并行。
- `PROJECT.md` 风险声明勾选任意一项。

L2 到 L3：

- 触碰真实硬件、生产数据、资金、用户隐私、合规系统。
- 需要审计留痕或可回滚流程。
- 发生过一次造成真实损失或对外副作用的事故。

降层与剪枝同样重要。每 4 到 6 周运行 `harness/rules-ledger.md` 的剪枝流程，删除已经被 `verify` 覆盖、长期不触发、或当前模型能力下不再有价值的规则。

## 两种项目模式

### 建造型

需求基本确定，边界可描述，验收可检查。

流程：

```text
PROJECT.md -> 任务卡 -> Loop -> Verify -> Review -> Commit -> Learn
```

### 探索型

做出来才知道要什么，验收标准暂时写不清。

规则：

1. 只用 L0，在 `spike/<name>` 分支快速验证假设。
2. `verify` 可以先只有 build，但真实世界副作用仍需确认。
3. spike 代码默认作废，不直接合入主干。
4. 方向确认后，从原型提取规格，再回到建造型流程。

判断标准：如果写不出可检验验收标准，就是探索型，不要硬套建造型。

## 新项目冷启动

1. 复制本目录到项目根。
2. 修改 `scripts/verify.sh` 或 `scripts/verify.ps1`，填入真实 build、lint、test、safety 命令。
3. 运行 `./scripts/check-harness.ps1` 或 `./scripts/check-harness.sh`，修掉 ERROR。
4. 手动跑通一次 verify。没跑通前，不让 Agent 写业务代码。
5. 填 `AGENTS.md` 的项目速览。
6. 如果项目会跨会话，填 `PROJECT.md`。
7. 从 `tasks/task-template.md` 复制出 `tasks/current-task.md` 并写第一张任务卡。
8. 用 `harness/prompt-template.md` 启动 Agent。

## 已有项目接入

不要一次性补齐所有文档。三步：

1. 写 `scripts/verify.*`，把已有 build/test/lint 收敛成一个入口。
2. 写 `AGENTS.md`，重点写禁止事项和真实验证命令。
3. 跑 `scripts/check-harness.*`，只修 ERROR，不追求一次性消灭所有 WARN。
4. 从下一个任务开始用任务卡，architecture/requirements 只在被踩过或必须协同时逐步补。

## 日常循环

```text
Gate 1 -> 任务卡 -> 分支 -> Agent 计划 -> 小步修改 -> quick verify -> 自审 -> 人类看 diff -> full verify -> Gate 2 -> commit -> progress/lessons 更新
```

每轮必须看三件事：

- diff 是否只做了任务卡内的事。
- verify 是否真实运行，命令、退出码、关键输出是否可信。
- `reports/progress.md` 是否足够让下一轮接手。

## 自检命令

真实项目中运行：

```bash
./scripts/check-harness.sh
```

或：

```powershell
./scripts/check-harness.ps1
```

维护模板本身时运行：

```bash
./scripts/check-harness.sh --mode template
```

或：

```powershell
./scripts/check-harness.ps1 -Mode Template
```

自检只回答“Harness 是否配置到可开工”，不替代 `scripts/verify.*`。

## 事故处理

Agent 犯错后，禁止只修代码。必须在 `docs/lessons-learned.md` 回答：系统哪一层失守？

| 失守层 | 系统修改 |
|--------|----------|
| 任务卡含糊 | 改 task template 或本类任务验收条款 |
| 上下文错误 | 改 context index，更新、降级或删除过时文档 |
| 缺停止规则 | 加 stop rule，并登记 rules ledger |
| verify 盲区 | 优先给 verify 加机器检查 |
| review 盲区 | 改 review checklist |
| 纯执行失误 | 记录即可，避免过度加规则 |

## 使用 `packs/`

默认不要复制所有 `packs/` 文件。按触发器启用：

- 需求或边界反复歧义：复制 `packs/l2/requirements.md`、`packs/l2/acceptance.md`。
- 模块边界被踩：复制 `packs/l2/architecture.md`。
- 安全、硬件、生产数据、资金、隐私：复制 `packs/l3/` 对应文件。
- 需要定量评估 Agent 或模型输出：复制 `packs/evals/`。
- 审查跟不上或任务天然并行：复制 `packs/subagents/`。

## 一页速记

```text
开工三问：verify 是什么？不做什么？碰到什么必须停？
开工一跑：check-harness 没有 ERROR
每轮三看：diff、verify 真实输出、progress 是否更新
事故一问：哪一层失守？改系统，不只改代码
每月一剪：哪条规则该退役？
```
