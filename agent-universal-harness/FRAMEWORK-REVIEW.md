# Framework Review

## 结论

三份 Harness 里最值得保留的是“生长版”的方向：用 `verify` 做地基，用任务卡限制边界，用 stop rules 管高风险，用 rules ledger 防止规则膨胀。

最终版本不应该追求一次性满配，而应该追求“所有项目都能从 L0 开始，复杂度只在有证据时增加”。

## 三版对比

| 来源 | 优点 | 问题 | 处理 |
|------|------|------|------|
| `agent-coding-master-template` | 文档完整，覆盖 requirements、architecture、safety、evals、subagents | 默认太重，容易把填模板误当成进度 | 拆成 `packs/`，只在触发时启用 |
| `agent-framework-reviewed-v2` | verify 默认失败、规则登记完整、loop 不硬编码命令 | 可再强化“旧项目接入”和“packs”策略 | 作为主基线 |
| `111/agent-framework` | 解释最清楚，强调生长、剪枝、spike | 脚本仍是 `.example` 形式，易被误用 | 吸收叙事，不沿用 `.example` |

## 最终设计

默认包：

- `AGENTS.md`：Agent 行为宪法，短且硬。
- `scripts/verify.*`：唯一真相入口，默认失败，必须改成真实命令。
- `scripts/check-harness.*`：Harness 自检入口，发现占位符、空任务卡、未配置 verify。
- `PROJECT.md`：项目宪章，强调 In Scope / Out of Scope / 成功标准 / 风险声明。
- `tasks/current-task.md`：每轮工作的边界。
- `reports/progress.md`：跨会话交接。
- `harness/loop.md`：Read -> Plan -> Act -> Verify -> Fix -> Review -> Report。
- `harness/quality-gates.md`：Bootstrap、Task Start、Before Commit、Before Release 四个硬闸门。
- `harness/stop-rules.md`：遇到歧义、高风险、不可逆操作时停。
- `harness/rules-ledger.md`：每条长期规则有起因、状态和复审。
- `harness/review-checklist.md`：看 diff 的标准。
- `harness/tool-permissions.md`：外部工具和真实世界副作用的权限分级。
- `docs/context-index.md`：避免过时文档污染 Agent。
- `docs/lessons-learned.md`：事故后改系统，而不是只修代码。
- `docs/verify-recipes.md`：常见技术栈的 quick / full / safety 配方。
- `docs/adoption-playbook.md`：新项目、老项目、高风险项目、团队接入路线。

可选包：

- `packs/l2/`：requirements、architecture、acceptance、ADR。
- `packs/l3/`：security、safety、operations。
- `packs/evals/`：eval protocol、scorecard。
- `packs/subagents/`：subagent protocol、handoff。

## 最重要的改动

1. `verify` 脚本不是 `.example`，但默认失败，避免 TODO 被误报为通过。
2. Loop 优先读取任务卡里的 Verification，不硬编码某个技术栈命令。
3. 停止规则和 AGENTS 铁律都登记到 rules ledger，避免约束越积越多。
4. 过时文档被定义为风险，必须更新、降级或删除。
5. spike 模式单独列出，防止探索代码悄悄变成生产代码。
6. 新增 `check-harness`，让框架能检查自己是否真的配置好。

## 使用建议

新项目：

1. 只复制默认包。
2. 先改 `scripts/verify.*`。
3. verify 跑通前，不让 Agent 写业务代码。
4. 超过一天再填 `PROJECT.md`。
5. 出现真实触发器，再复制 `packs/`。

老项目：

1. 不补全历史文档。
2. 先把已有命令收敛到 `scripts/verify.*`。
3. `AGENTS.md` 只写真实禁区和验证入口。
4. 从下一个任务开始用任务卡。

高风险项目：

1. 风险声明只要勾选任一项，就启用 L2+ stop rules。
2. 涉及硬件、生产数据、资金、隐私、外部写操作时复制 `packs/l3/`。
3. 所有真实世界副作用先 dry-run / simulation，再人工确认。

## 后续可升级方向

- 为常见栈生成 `verify` 配方：Node、Python、.NET、Rust、Go、WPF、嵌入式。
- 加一个 `scripts/bootstrap-harness.*`，按层级选择复制文件。
- 加一个 `scripts/check-harness.*`，检查任务卡是否缺 Verification、AGENTS 是否缺 verify 命令、rules ledger 是否漏登规则。
- 为 Codex / Claude Code / Cursor 分别生成启动 prompt 变体。
