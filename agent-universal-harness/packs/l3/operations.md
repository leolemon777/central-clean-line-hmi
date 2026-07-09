# Deployment and Operations

> 触发条件：代码会部署到生产、影响外部用户、需要回滚或审计。

## Environments

| 环境 | 用途 | 数据类型 | 权限 | 部署方式 |
|------|------|----------|------|----------|
| dev |      |          |      |          |
| staging |  |          |      |          |
| production | |        |      |          |

## Deployment

- 部署命令：
- 前置检查：
- 后置检查：
- 负责人：

## Rollback

- 回滚命令：
- 回滚验证：
- 数据兼容性：
- 不能回滚的部分：

## Observability

- 日志：
- 指标：
- Trace：
- 告警：

## Agent Restrictions

- Agent 不直接部署生产，除非专项流程明确授权。
- Agent 不直接改生产配置。
- Agent 不隐藏失败检查，不把失败降级成 warning。
