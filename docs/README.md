# Project File Layout

This folder keeps non-source project materials grouped away from the solution root.

## Folders

- `handovers/`: staged handoff notes for UI pages and implementation tasks.
- `screenshots/`: layout QA screenshots and visual verification captures.
- `hardware/`: hardware manuals and vendor reference documents.
- `cleanup/`: cleanup decisions, risks, and follow-up notes.
- `obsidian/`: source notes staged for the user's Obsidian project folder.

## Key Docs

- `architecture.md`: current application layers, hardware boundaries, deployment boundary, and verify entrypoints.
- `project-structure.md`: active source roots, reference folders, generated folders, and cleanup rules.
- `cleanup/project-structure-cleanup-plan.md`: cleanup candidates that require confirmation before delete/move.

## Kept At Root

- `CentralCleanLineHmi.sln`: current main solution.
- `AGENTS.md`: local agent/project rules.
- `agent-universal-harness/`: task card, verify scripts, stop rules, tool permissions, and progress report.
- `src/`: main application source.
- `tests/`: main test projects.
- `C#例程源代码及库文件/`: vendor SDK/demo files required by the current UI project for MultiCard DLL references.
- `PipelineControl/`: separate layered skeleton/driver solution kept intact until ownership is confirmed.
