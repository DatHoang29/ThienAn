# Memory Index

> Thư mục này là **nguồn duy nhất**. Auto-memory global
> (`~/.claude/projects/c--ThienAn/memory`) là **junction trỏ về đây** — sửa 1 chỗ, ăn cả 2.
> Tạo lại junction trên máy mới: xem cuối file.

## User & Conventions
- [User profile](user-profile.md) — backend dev trên TA-ITS015 (ITS/C2C ISO 14827); viết & trả lời tiếng Việt
- [Thiên An preferences & project conventions](thienan-user-preferences.md) — git/docker/code-style/entity/SqlSugar-DbFirst/Vue/testing/SQL rules (đã gộp phần C# của project-conventions)

## Project — việc đang làm
- [ShareData Worker = DataPublicationService](sharedata-worker-datapublication.md) — outbound→file-only publication service; scope + rename
- [Do not modify shared ShareData entities](do-not-modify-shared-sharedata-entities.md) — API team sở hữu; worker thích ứng trong code
- [VideoWall Live & Auto-Log plan](videowall-record-replay-plan.md) — plan chốt 2026-08-29; chỉ 2 tầng WPF↔thiết bị; thiết lập scene & auto log ra file; prompt thực thi ở DocBusinessThienAn/HữuNghị-ChiLăng/VideoWall/

## Database / MCP
- [MCP DAB database access](mcp-dab-database-access.md) — 3 DAB MCP server; `mssql_staging` = 10.10.8.30 = source of truth
- [Read-only DB queries: just run them](read-only-db-queries-just-run.md) — chạy read không cần hỏi; vẫn cấm write

## Reference
- [Tài liệu Nghiệp vụ HN-CL](../../DocBusinessThienAn/HữuNghị-ChiLăng/INDEX.md) — Nguồn sự thật duy nhất cho tài liệu nghiệp vụ Hữu Nghị - Chi Lăng (luôn mở file này trước).
- [SqlSugar docs](sqlsugar-docs.md) — link no-entity / raw SQL / JSON→SQL
- [Tech decisions](tech-decisions.md) — bảo trì `.agents/manifest.json` ↔ frontmatter (AG-Kit tooling, không phải app)

---

## Tạo lại junction (chạy 1 lần trên mỗi máy, không cần admin)

```cmd
rmdir /S /Q "%USERPROFILE%\.claude\projects\c--ThienAn\memory"
mklink /J "%USERPROFILE%\.claude\projects\c--ThienAn\memory" "C:\ThienAn\.agents\memory"
```
