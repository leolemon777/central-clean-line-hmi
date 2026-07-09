# Usage Checklist

## 第一次复制到项目

- [ ] 把本目录内容复制到项目根。
- [ ] 删除暂时不用的 `packs/` 文件，或保留但不要让 Agent 默认读取。
- [ ] 修改 `scripts/verify.sh` 或 `scripts/verify.ps1`。
- [ ] 按 `docs/verify-recipes.md` 选择 quick / full / safety 配方。
- [ ] 运行 `scripts/check-harness.*`，修掉 ERROR。
- [ ] 手动运行一次 full verify。
- [ ] 填 `AGENTS.md` 项目速览。
- [ ] 判断当前层级：L0 / L1 / L2 / L3。
- [ ] 如果跨会话，填 `PROJECT.md`。
- [ ] 填 `tasks/current-task.md`。

## 每个任务开工前

- [ ] 任务卡有 In Scope。
- [ ] 任务卡有 Out of Scope。
- [ ] Acceptance Criteria 可验证，人工检查步骤写清。
- [ ] Verification 写了 quick 和 full。
- [ ] 风险等级写清。
- [ ] Size Check 写清。
- [ ] Context 只列必要文件。
- [ ] Gate 1 没有未满足项。

## 每轮 Agent 完成后

- [ ] 看真实 diff。
- [ ] 看 verify 命令、退出码、关键输出。
- [ ] 对照 Acceptance Criteria。
- [ ] 看是否触发 stop rules。
- [ ] 确认 `reports/progress.md` 更新。
- [ ] Gate 2 没有 Blocking。

## 事故后

- [ ] `docs/lessons-learned.md` 写了失守层。
- [ ] 如果是 verify 盲区，优先补机器检查。
- [ ] 如果是规则缺失，补 stop rule 并登记 `rules-ledger.md`。
- [ ] 如果是文档过时，更新、降级或删除文档。

## 每 4 到 6 周

- [ ] 运行 rules ledger 剪枝流程。
- [ ] 核对 context index 文档新鲜度。
- [ ] 跑一次 `scripts/check-harness.* --strict` 或 `-Strict`。
- [ ] 删除或退役长期未触发且已无价值的规则。
- [ ] 把重复事故升级成 verify、测试或 checklist。
