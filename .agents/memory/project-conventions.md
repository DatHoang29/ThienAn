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
- **Class Header XML Comments Required**: Every new or updated class MUST include an XML summary header block formatted as follows:
```csharp
/// <summary>
/// [Description / Table Name]
/// Author: [Author Name]
/// Created date: [dd/MM/yyyy]
/// </summary>
```


## Entity Conventions
- **Entity Inheritance**: All Entity classes MUST inherit from `EntityTenant` (from `Shared.Core.Domain`).


## Module Shares & Controller Conventions
- **BaseController for Shares Module**: Always use `GroupName = "Share"` and `BasePath = "api/vms"` in `Modules.Shares.Controllers.BaseController`.
- **Git Rules**: Never run `git commit` or `git push` automatically. Always leave modified files uncommitted for developer manual review and commit.


