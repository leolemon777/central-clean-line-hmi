# Verify Recipes

> `scripts/verify.*` 是项目的唯一真相入口。本文件只提供配方，不要让 Agent 直接绕过 verify 运行零散命令。

## 设计原则

- Quick 用于 Loop 内每轮：快、确定、覆盖当前任务最相关风险。
- Full 用于提交前 / CI：完整、慢一点可以接受。
- Safety 用于 L2/L3：secret scan、migration dry-run、硬件 simulation、权限边界检查。
- 失败必须返回非 0 退出码，不能只打印 warning。

## Node / TypeScript

Quick：

```bash
npm run lint
npm test -- --runInBand
npm run typecheck
```

Full：

```bash
npm ci
npm run lint
npm run typecheck
npm test
npm run build
```

说明：不要在 verify 里自动 `npm install`，依赖变化应单独审查。

## Python

Quick：

```bash
ruff check .
pytest -q -m "not slow"
python -m compileall src
```

Full：

```bash
ruff check .
pytest
python -m compileall src
```

说明：如果项目使用 `mypy` 或 `pyright`，放进 Full，关键模块可放进 Quick。

## .NET / C# / WPF

Quick：

```bash
dotnet build ./src/App.sln
dotnet test ./src/App.sln --filter "Category!=Slow"
```

Full：

```bash
dotnet format ./src/App.sln --verify-no-changes
dotnet build ./src/App.sln
dotnet test ./src/App.sln
```

说明：WPF UI 改动要补人工检查步骤，截图或录屏路径写进任务报告。

## Go

Quick：

```bash
go test ./...
go vet ./...
```

Full：

```bash
gofmt -w <仅在人工确认格式化范围后运行>
go test ./...
go vet ./...
```

说明：verify 里不要静默改文件。格式检查可用 `gofmt -l .`。

## Rust

Quick：

```bash
cargo check
cargo test
```

Full：

```bash
cargo fmt --check
cargo clippy -- -D warnings
cargo test
```

## Java / Maven

Quick：

```bash
mvn -q test
```

Full：

```bash
mvn verify
```

## Web UI

Quick：

```bash
npm run lint
npm run typecheck
npm test
```

Full：

```bash
npm run build
npm run test:e2e
```

人工检查应写清：

- 视口尺寸。
- 要走的关键流程。
- 截图路径。
- 可访问性或键盘操作要求。

## L3 Safety

把下面检查放入 Full 或单独 safety step：

```bash
# secret scan
# migration dry-run
# hardware simulator safety cases
# external API sandbox write test
# permission boundary tests
```

原则：真实硬件、资金、生产数据、对外写 API 不直接放进 verify 自动执行。verify 只能执行 simulation、dry-run、sandbox。
