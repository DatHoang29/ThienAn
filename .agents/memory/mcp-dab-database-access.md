---
name: mcp-dab-database-access
description: "How to query the project's SQL Server DBs read-only via the DAB MCP servers, which one has the current schema, and how to fix 'Connection closed' when config drifts from real DB schema"
metadata: 
  node_type: memory
  type: reference
  originSessionId: 8862bd9e-5d13-42a7-8b51-9079f8a21d37
  modified: 2026-09-14T00:00:00.000Z
---

`.agents/mcp_config.json` là nguồn duy nhất (từ 01/11/2026); `.mcp.json`, `.codex/config.toml`… sinh ra bằng
`node .agents/mcp/sync.mjs --apply`. 3 DAB MCP server chạy qua
`node .agents/mcp/dab.mjs <dev|test|staging>`: launcher đọc `DAB_<ENV>_CONNECTION_STRING` trong
`.agents/mcp/.env` (không commit) rồi chạy `dotnet tool run --allow-roll-forward dab start --mcp-stdio ... --config .agents/mcp/dab-config.json`.
Chỉ còn **một** `.agents/mcp/dab-config.json` cho cả 3 DB, nên mọi entity khai trong đó phải có ở cả 3 DB:

| MCP server | DB | Notes |
|---|---|---|
| `mssql_dev` | local Docker `localhost:14333/DEV_ITS10` (container `tac_webapi_sqlserver`) | **Nguồn DB chuẩn khi review/plan/prompt** (rule 16/10/2026). Người dùng đã import bản export staging vào đây ngày 14/10/2026 (mới có schema). |
| `mssql_test` | local Docker `localhost:14333/test` | |
| `mssql_staging` | **`10.10.12.80/DEV_ITS015`** (từ 06/11/2026; trước là `10.10.8.30/dev_its10`). Máy mới còn `DEV_ITS016` là bản cũ hơn | Chỉ dùng khi người dùng yêu cầu trực tiếp (rule 16/10/2026: review/plan/prompt chỉ đối chiếu local; staging do người dùng tự kiểm). |

Setup fixed 2026-08-27 (was fully broken — DAB CLI not installed):
- Created `dotnet-tools.json` (gốc repo) pinning `Microsoft.DataApiBuilder` 2.0.12. Needs
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
```bash
# chạy ở gốc repo; lấy chuỗi kết nối của DB cần kiểm từ .agents/mcp/.env
export DAB_CONNECTION_STRING='<giá trị DAB_DEV_CONNECTION_STRING trong .agents/mcp/.env>'
dotnet tool run --allow-roll-forward dab validate --config .agents/mcp/dab-config.json
```
Hoặc chạy chính launcher: `node .agents/mcp/dab.mjs dev` — config lỗi thì `dab` in `fail:` ra stderr rồi thoát.
Đọc các dòng `fail: Cannot obtain Schema for entity <X> ... Invalid object name '<schema>.<X>'` —
đó chính xác là tên entity cần xoá khỏi object `entities` trong file config tương ứng.

**Cách sửa nhanh (không cần `jq`, máy không có sẵn — dùng PowerShell)**:
```powershell
$path = ".agents/mcp/dab-config.json"
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

## CSDL máy chủ mới (06/11/2026)

- MCP `mssql_staging` trỏ CSDL `DEV_ITS015` trên máy chủ mới qua `.agents/mcp/.env` (git bỏ qua, chỉ đọc, `ApplicationIntent=ReadOnly`); từ 09/11/2026 tắt mặc định, bật khi người dùng yêu cầu.
- Máy đó còn `DEV_ITS016` (bản cũ hơn: 15 gói, 118 câu dịch ShareData so với 20 gói, 222 câu dịch ở `DEV_ITS015`). Cả hai CSDL DEV chưa có cột `CursorKey` ở `ShareDataLastSend`; `DEV_ITS015` còn bảng cũ `ShareDataLastSendKey`.
- Kiểm CSDL không có sqlcmd: dựng app .NET nhỏ trong scratchpad dùng `Microsoft.Data.SqlClient`, chuỗi kết nối qua biến môi trường, chỉ `SELECT`; viết tệp bằng Write vì hook chặn `cat > *.cs`.
- Phiên Claude đang mở thì `mssql_staging` không tự kết nối lại: dùng `/mcp` hoặc mở phiên mới.
