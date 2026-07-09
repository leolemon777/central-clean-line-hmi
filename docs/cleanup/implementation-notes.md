# Cleanup Notes

## Decisions

- Moved loose page handoff notes from the repository root into `docs/handovers/`.
- Moved loose visual QA screenshots from the repository root into `docs/screenshots/`.
- Moved the hardware manual PDF into `docs/hardware/`.
- Added `docs/README.md` to make the file categories explicit.

## Not Changed

- Did not delete or move source code under `src/` or tests under `tests/`.
- Did not delete `C#例程源代码及库文件/` because `src/PipelineControl.UI/PipelineControl.UI.csproj` copies MultiCard DLLs from that vendor SDK path.
- Did not delete or move `PipelineControl/` because it contains a separate solution with driver abstraction/Bopai/simulator projects that may be future hardware-integration work.
- Did not delete `bin/`, `obj/`, `.vs/`, or runtime logs in this pass because the app was recently launched and the workspace is not under git, so recovery would be less traceable.

## Verification

- `dotnet build src\PipelineControl.UI\PipelineControl.UI.csproj --no-restore` passed.
- `dotnet build src\PipelineControl.Application\PipelineControl.Application.csproj --no-restore` passed.
- `dotnet build src\PipelineControl.Infrastructure\PipelineControl.Infrastructure.csproj --no-restore` passed.
- `dotnet build CentralCleanLineHmi.sln --no-restore` reached the source projects but failed on `tests\PipelineControl.UI.Tests` because `tests\PipelineControl.UI.Tests\obj\project.assets.json` is missing. A NuGet restore is required before solution-level test project builds.

## Risks

- The nested `PipelineControl/` solution may be obsolete, but it should be reviewed before removal.
- Generated build folders can be cleaned in a second pass once no running application process depends on the current `bin/Debug` output.
