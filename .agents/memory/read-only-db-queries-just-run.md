---
name: read-only-db-queries-just-run
description: The user wants read-only DB/MCP queries executed without stopping to ask
metadata: 
  node_type: memory
  type: feedback
  originSessionId: 8862bd9e-5d13-42a7-8b51-9079f8a21d37
  modified: 2026-08-27T10:03:24.871Z
---

When inspecting the database (MCP `read_records` / `describe_entities` / `aggregate_records`, or a
`SELECT`-only `sqlcmd`), **just run it** — don't pause for confirmation.

**Why:** the user said "mặc định là đọc nên cứ đọc nhé, không update xóa là được hoặc thêm" — reads
are the safe default; only writes need caution.

**How to apply:** run SELECT / MCP read tools freely. Still never issue INSERT/UPDATE/DELETE/DDL
against any DB, and keep `dotnet test` pointed at a **local** DB only (CLAUDE.md safeguard: abort if
a test connection string points at a remote server such as `10.10.12.80` or the old `10.10.8.30`). Prefer MCP
`mssql_staging` over a raw client when it's connected; a raw read-only client to staging (`10.10.12.80`, account in
`.agents/mcp/.env`, which git ignores) is acceptable when MCP isn't loaded. See [mcp-dab-database-access](mcp-dab-database-access).
