# Adoption Playbook

## 15 分钟接入

适合一次性脚本、小工具、低风险修改。

1. 复制 `AGENTS.md`、`scripts/verify.*`、`tasks/current-task.md`。
2. 把现有 build/test 命令写进 `scripts/verify.*`。
3. 运行 `./scripts/check-harness.*`，修掉 ERROR。
4. 填一张最小任务卡。

目标：先有边界和验证，不追求完整项目文档。

## 1 小时接入

适合会跨会话继续的项目。

1. 完成 15 分钟接入。
2. 填 `PROJECT.md` 的目标、In Scope、Out of Scope、成功标准。
3. 启用 `reports/progress.md`。
4. 用 `harness/prompt-template.md` 开第一轮。

目标：下一次打开项目，不需要靠聊天记录恢复状态。

## 老项目接入

不要考古式补文档。

1. 收敛 verify：把现有 CI、本地脚本、人工命令整理成一个入口。
2. 写禁区：在 `AGENTS.md` 里写真实不能碰的目录、命令、数据、外部系统。
3. 从下一个任务开始用任务卡。
4. 被踩到的边界再写 architecture，被脑补的需求再写 requirements。

目标：先阻止新错误，不补历史仪式。

## 高风险接入

适合生产数据、真实硬件、资金、隐私、合规、外部写 API。

1. `PROJECT.md` 风险声明必须勾选。
2. 启用 `harness/stop-rules.md` 的 L2+ 条款。
3. 复制 `packs/l3/security.md`、`packs/l3/safety-rules.md`、`packs/l3/operations.md`。
4. `harness/tool-permissions.md` 登记所有外部工具和命令。
5. verify 只跑 simulation / dry-run / sandbox，不自动触发真实副作用。

目标：所有真实世界副作用都有预授权、回滚或人工确认。

## 团队接入

1. 把 `scripts/verify.*` 接入 CI。
2. 约定 PR 模板引用 `tasks/current-task.md` 和 verify 输出。
3. 高风险 PR 强制 Reviewer 会话或人类逐行审查。
4. 每 4 到 6 周跑 rules ledger 剪枝。

目标：Harness 成为团队操作系统，而不是某个 Agent 的提示词。
