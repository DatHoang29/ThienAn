# Master AI Steering - ThienAn (Kiro AI IDE)

> 🔴 **CRITICAL STEERING DIRECTIVE FOR KIRO AI:**
> All architecture rules, specialist agents, modular skills, workflows, lifecycle hooks, memory, contracts, scripts, and MCP configurations are centralized in the **`.agents/`** directory.
> You MUST inspect and strictly execute from `.agents/` before answering or modifying code.

---

## 🗺️ 8-Pillar AG-Kit Architecture in `.agents/`

### 1. 📋 Workspace Rules (`.agents/rules/`) — P0 (Highest Priority)
- **`core-protocol.md`**: Mandatory skill announcement format: `📚 Using skill: @[skill-name]...`
- **`request-routing.md`**: Mandatory agent announcement: `🤖 Applying knowledge of @[agent-name]...`
- **`universal-rules.md`**: Clean Code, single-statement `if` indentation, object initializers (1 prop/line), LINQ formatting, Primary Constructors on new classes only, structured logging (`CA1873`), strict module scope, no live DB mutations, strict local DB for `dotnet test`, auto-cleanup completed prompt files.
- **`code-rules.md`**: 4-phase planning, Socratic Gate, test pyramid, AAA pattern.
- **`thienan_rules.md`**: Git branch naming (`feat/`, `fix/`, `release/`), semantic commit conventions.

### 2. 🤖 Specialist Agents (`.agents/agent/`)
Adopt the persona, guidelines, and frontmatter skills from:
- `backend-specialist.md`: C# .NET, ASP.NET Core, SqlSugar, ShareDataWorker, C2C protocol.
- `frontend-specialist.md`: Vue.js 3, TypeScript, Vite, Pinia, Tailwind CSS.
- `database-architect.md`: MS SQL Server, DAB, database schema design & indexing.
- `orchestrator.md`: Master coordinator for complex multi-domain tasks.
- `debugger.md`: 4-phase root cause analysis & systematic debugging.
- `qa-automation-engineer.md` & `test-engineer.md`: Unit & integration tests (xUnit, Moq).
- `security-auditor.md`: OWASP, module isolation, vulnerability analysis.
- `project-planner.md`: 4-phase implementation plans.

### 3. 📚 Modular Skills & Scripts (`.agents/skills/`)
Read `SKILL.md` before applying knowledge. Execute companion scripts when needed:
- `clean-code`: Pragmatic coding standards.
- `systematic-debugging`: 4-phase debugging.
- `testing-patterns`: Unit & integration tests (`tests/`).
- `vulnerability-scanner`: Security scans via `python .agents/skills/vulnerability-scanner/scripts/security_scan.py`.
- `performance-profiling`: Performance audits via `.agents/skills/performance-profiling/scripts/`.
- `api-patterns`: REST / ISO 14827 API design & validation.
- `frontend-design` & `design-spec`: Anti-slop UI design systems.

### 4. ⚡ Workflows & Slash Commands (`.agents/workflows/`)
When executing slash commands, follow the corresponding workflow file:
- `/brainstorm` $\rightarrow$ `.agents/workflows/brainstorm.md`
- `/create` $\rightarrow$ `.agents/workflows/create.md`
- `/orchestrate` $\rightarrow$ `.agents/workflows/orchestrate.md`
- `/plan` $\rightarrow$ `.agents/workflows/plan.md`
- `/test` $\rightarrow$ `.agents/workflows/test.md`
- `/verify` $\rightarrow$ `.agents/workflows/verify.md`
- `/debug` $\rightarrow$ `.agents/workflows/debug.md`
- `/deploy` $\rightarrow$ `.agents/workflows/deploy.md`
- `/status` $\rightarrow$ `.agents/workflows/status.md`

### 5. 🧠 Persistent Memory (`.agents/memory/`)
- At the start of tasks, check `.agents/memory/MEMORY.md` and `.agents/memory/thienan-user-preferences.md`.
- Persist new user decisions, architectural choices, and project conventions to `.agents/memory/MEMORY.md`.

### 6. 🛡️ Lifecycle Hooks & Safeguards (`.agents/hooks/`)
- Tool call validation & Doctor checks: `.agents/hooks/validate-tool-call.mjs`, `.agents/hooks/antigravity-doctor.mjs`.

### 7. 📜 Contracts & Schemas (`.agents/schemas/`)
- Component schemas & runtime contracts in `.agents/schemas/`.

### 8. 🔌 Database & MCP (`.kiro/settings/mcp.json` / `dab-config.dev.json`)
- **`mssql_dev`**: Connects to MS SQL Dev (`localhost:14333/dev_its10`) via Data API Builder (DAB) using `.agents/dab-config.dev.json`.
- **Strictly READ-ONLY**: Use MCP only to inspect tables, describe schemas, and read test data. NEVER execute state mutations.

---

## 🛑 Critical Mandatory Safeguards
1. **Strict Local Database for `dotnet test`**: All connection strings must point to `local` (`localhost`, `127.0.0.1`, `(localdb)`, `.`). If remote IP (e.g. `10.10.8.30`) is detected, **CANCEL test immediately and report to user**.
2. **Auto-Cleanup Completed Prompt & Plan Files**: Automatically delete executed prompt/plan files (e.g. `*-prompt-*.md`, `{task-slug}.md`) after task completion.
3. **Strict Manual SQL Execution**: Only write `.sql` files to disk. Never auto-execute database mutations.
