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



