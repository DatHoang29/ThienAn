# Project Conventions - ThienAn

## Commands
- Build: `dotnet build` / `npm run build`
- Test: `dotnet test` / `npm test`
- Lint: `npm run lint`

## Stack & Architecture
- Backend: C# / .NET (.NET 7/8+), MS SQL Server
- Frontend: TypeScript, React, Vanilla CSS
- Architecture: Modular Architecture with strict module isolation

## Core Protocols & Rules
- **Skill Announcements**: Always announce loaded skills before applying (`📚 Using skill: @[skill-name]...`).
- **Agent Announcements**: Announce auto-selected agent knowledge (`🤖 Applying knowledge of @[agent-name]...`).
- **File Dependency Awareness**: Check file dependencies before edits; update all dependent files together.
- **SQL & Module Isolation**: DDL/DML scripts MUST ONLY target tables within that module's official entity scope. NEVER mutate external module tables.
- **Strict Manual SQL Execution**: ONLY write or modify `.sql` files on disk. NEVER auto-execute DDL/DML mutations against databases without explicit user request.
- **Clean Code & Formatting**:
  - Keep code concise, self-documenting, and free of unnecessary comments.
  - Single-statement `if` MUST break line and indent.
  - Prefer generic `Enum.IsDefined<TEnum>(value)` (.NET CA2263).

## Git Branching & Commit Conventions
- **Branch Types**: `release`, `staging`, `dev`, `feat/`, `fix/`, `merge/`, `hotfix/`, `preview/`, `exp/`.
- **Branch Naming**: `[BranchKey]/[yyyyMMdd]-[task-description]` or `[BranchKey]/[TaskCode]_[task-description]`.
- **Commit Messages**: Prefix with type (`feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:`).
