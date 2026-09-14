---
name: mcp-dab-database-access
description: "How to query the project's SQL Server DBs read-only via the DAB MCP servers, which one has the current schema, and how to fix 'Connection closed' when config drifts from real DB schema"
metadata: 
  node_type: memory
  type: reference
  originSessionId: 8862bd9e-5d13-42a7-8b51-9079f8a21d37
  modified: 2026-09-14T00:00:00.000Z
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

## Khi MCP báo "Connection closed" cho mssql_dev/test/staging (gặp lại 2026-09-14)

**Nguyên nhân**: `dab start --mcp-stdio` validate TOÀN BỘ entity khai trong `dab-config.<env>.json`
so với schema DB THẬT trước khi chạy — hễ 1 bảng trong config không còn tồn tại/đổi tên trong DB
thật (schema drift theo thời gian, DB đổi mà config không cập nhật lại) là `dab` in `fail: ... Config
is invalid` rồi THOÁT TIẾN TRÌNH NGAY. Claude Code thấy tiến trình vừa mở đã đóng → báo
"Connection closed" — không phải lỗi mạng, không phải lỗi Claude.

**Cách tự chẩn đoán (chạy trực tiếp, an toàn, chỉ đọc — không cần MCP)**:
```
cd C:\ThienAn
dotnet tool run --allow-roll-forward dab validate --config .agents/dab-config.dev.json
dotnet tool run --allow-roll-forward dab validate --config .agents/dab-config.test.json
dotnet tool run --allow-roll-forward dab validate --config .agents/dab-config.staging.json
```
Đọc các dòng `fail: Cannot obtain Schema for entity <X> ... Invalid object name '<schema>.<X>'` —
đó chính xác là tên entity cần xoá khỏi object `entities` trong file config tương ứng.

**Cách sửa nhanh (không cần `jq`, máy không có sẵn — dùng PowerShell)**:
```powershell
$path = ".agents/dab-config.dev.json"
$json = Get-Content $path -Raw | ConvertFrom-Json
foreach ($name in @("EntityName1", "EntityName2")) { $json.entities.PSObject.Properties.Remove($name) }
$json | ConvertTo-Json -Depth 100 | Set-Content $path
```
Lặp lại cho từng file/entity bị lỗi, sau đó `dab validate` lại để xác nhận về 0 lỗi (thấy dòng
"The config satisfies the schema requirements", không còn dòng `fail:` nào).

**Sau khi sửa xong file config**: phải `/mcp` reconnect (hoặc restart session) — MCP tools chỉ nạp
lại lúc khởi động, sửa file xong không tự áp dụng ngay.

Lần gặp 2026-09-14: `dev` lệch 3 bảng (`ShareDataEventSource`/`ShareDataMappingProfile`/
`TollTransaction`), `test` lệch 36 bảng (nhiều `Sys*`/`HangFire_*`/`Toll*`/`ShareData*` +
`VwActivityLog`), `staging` lệch đúng 1 bảng (`ShareDataDataSource`) — không có entity `Vw*`
(VideoWall) nào khác bị lỗi ngoài `VwActivityLog` ở test.
