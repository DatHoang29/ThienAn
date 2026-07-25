---
type: project
created: 2026-05-25
updated: 2026-07-23
---

# Project Conventions

## Git Workflow
- Always create a new dedicated branch for major code changes.
- Branch name format should follow: `feature/[task-slug]` or `fix/[bug-slug]`.

## Docker & Infrastructure Rules
- **Docker SQL Server on macOS**: ALWAYS use `mcr.microsoft.com/azure-sql-edge:latest` for SQL Server containers on macOS (Apple Silicon M1/M2/M3/M4). NEVER use `mcr.microsoft.com/mssql/server:2022-latest` as x86_64 emulation under QEMU causes immediate memory crashes (`Invalid mapping of address`).

## Code Style & Class Header Comments
- **Class Header XML Comments Required**: Every new or updated class MUST include an XML summary header block formatted as follows (Author is always `Đạt`):
```csharp
/// <summary>
/// [Description / Table Name]
/// Author: Đạt
/// Created date: [dd/MM/yyyy]
/// </summary>
```



## Entity Conventions
- **Entity Inheritance**: All Entity classes MUST inherit from `EntityTenant` (from `Shared.Core.Domain`).


## Minimal Diff & Maintenance Rules
- **Minimal Diff Principle**: ONLY modify files and code strictly necessary to accomplish the requested feature or bugfix. NEVER auto-upgrade package versions (`PackageReference` in `.csproj`), edit unrelated shared projects (`Shared.Reference`), or modify existing files outside the task scope unless explicitly instructed by the user.

## Module Architecture & Controller Conventions

- **New Module Structure**: Standardize all new modules to place Entities, DTOs, Enums, and Interfaces into `Modules.[TênHệ].Core` (e.g., `Modules.Shares.Core`), while Controllers, Services, and Extensions live in `Modules.[TênHệ]` (e.g., `Modules.Shares`).
- **Report Sub-modules**: If creating a dedicated Report sub-project, name it `Modules.[TênHệ].Report` (e.g., `Modules.Shares.Report`), with namespace `Modules.Shares.Report.Controllers` and `GroupName = "ShareReport"`. If inside `Modules.Shares`, place in `Controllers/Report/`.
- **BaseController for Shares Module**: Always use `GroupName = "Share"` and `BasePath = "api/vms"` in `Modules.Shares.Controllers.BaseController`. Use `[ApiDescriptionSettings(GroupName)]` for Swagger Auto-Discovery.
- **Inter-Module Communication**: Prefer Event Bus (`MessBus`) for decoupled communication, or Refit API Interfaces in `Shared.Utility.Apis.[TênHệ]` for synchronous calls.
- **Git Rules**: Never run `git commit` or `git push` automatically. Always leave modified files uncommitted for developer manual review and commit.




