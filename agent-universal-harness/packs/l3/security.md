# Security

> 触发条件：鉴权、授权、隐私、密钥、文件上传下载、远程执行、生产配置、外部写 API。

## Authentication

- 认证方式：
- session / token 策略：
- 过期与撤销：

## Authorization

| 角色 | 权限 | 禁止 | 验证方式 |
|------|------|------|----------|
|      |      |      |          |

## Secrets

- 密钥不得提交到仓库。
- 本地使用 `.env` 或系统密钥管理。
- 生产环境使用受控 secret manager。
- 日志、错误报告、截图不得包含 token、真实账号、真实 IP。

## Input Validation

所有外部输入必须验证：

- 类型
- 范围
- 长度
- 格式
- 权限

## Security Review Triggers

以下改动必须触发安全 review：

- 登录 / 鉴权 / 授权。
- 加密 / 签名。
- 文件上传 / 下载。
- 远程命令执行。
- 数据删除 / 导出。
- 网络访问策略。
- 生产配置。
