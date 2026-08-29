---
name: mcp-dab-database-access
description: "How to query the project's SQL Server DBs read-only via the DAB MCP servers, and which one has the current schema"
metadata: 
  node_type: memory
  type: reference
  originSessionId: 8862bd9e-5d13-42a7-8b51-9079f8a21d37
  modified: 2026-08-27T10:03:14.973Z
---

`.mcp.json` (root, mirrors `.agents/.mcp.json`) defines 3 DAB MCP servers, launched with
`dotnet tool run --allow-roll-forward dab start --mcp-stdio ... --config .agents/dab-config.<env>.json`:

| MCP server | DB | Notes |
|---|---|---|
| `mssql_dev` | local Docker `localhost:14333/DEV_ITS10` (container `tac_webapi_sqlserver`) | **stale** ShareData schema (has ShareDataDataSource/EventSource/MappingProfile; missing ShareDataPacket/Table/Mapping/CodeSet) |
| `mssql_test` | local Docker `localhost:14333/test` | |
| `mssql_staging` | **`10.10.8.30/dev_its10`** (`sa` / `Tac@@1234`) | **source of truth** — has the current branch ShareData schema. Use this for ShareData work. |

Setup fixed 2026-08-27 (was fully broken — DAB CLI not installed):
- Created `c:\ThienAn\dotnet-tools.json` pinning `Microsoft.DataApiBuilder` 2.0.12. Needs
  `--allow-roll-forward` because the box has ASP.NET Core **10** runtime only, not 8.
- `.agents/dab-config.{dev,staging,test}.json`: removed schema-invalid `runtime.telemetry` block;
  regenerated `entities` to **every base table, `anonymous:read` only** (no create/update/delete
  anywhere); connection strings keep `ApplicationIntent=ReadOnly`; `rest`/`graphql` disabled, `mcp`
  enabled. `dab validate --config <f>` passes for all three.

Gotcha: MCP tools load only at Claude Code **startup** — after editing `.mcp.json` / a dab-config,
restart the session or `/mcp` reconnect, then approve the server. Tools appear as
`mcp__mssql_staging__{describe_entities,read_records,aggregate_records}` (write tools also appear
but every entity is read-only so writes are rejected server-side).

See [read-only-db-queries-just-run](read-only-db-queries-just-run).
