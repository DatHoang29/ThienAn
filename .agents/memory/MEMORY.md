# Memory Index

> 🔴 **Mọi file memory session GHI VÀO thư mục NÀY** (`<repo>/.agents/memory/`) — **KHÔNG BAO GIỜ**
> ghi vào `~/.claude/projects/*/memory/`. Đây là **nguồn duy nhất**; path global chỉ là **junction
> trỏ về đây**. Nếu path global là thư mục thật (không phải junction) trên máy hiện tại ⇒ junction
> chưa/hỏng: cứ ghi local, rồi tạo lại junction (snippet cuối file).

## User & Conventions
- [User profile](user-profile.md) — backend dev trên TA-ITS015 (ITS/C2C ISO 14827); viết & trả lời tiếng Việt
- [Văn phong plan/báo cáo: báo cáo + hành động, bỏ chi tiết forensic](feedback-report-style-no-forensic-detail.md) — bảng ngắn Trạng thái/Chốt ở họp, không kể ngày commit/diễn giải diff
- [Thiên An preferences & project conventions](thienan-user-preferences.md) — git/docker/code-style/entity/SqlSugar-DbFirst/Vue/testing/SQL rules (đã gộp phần C# của project-conventions)

## Project — việc đang làm
- [ShareData Worker = DataPublicationService](sharedata-worker-datapublication.md) — outbound→file-only publication service; scope + rename
- [Do not modify shared ShareData entities](do-not-modify-shared-sharedata-entities.md) — API team sở hữu; worker thích ứng trong code
- [VideoWall Live & Auto-Log plan](videowall-record-replay-plan.md) — plan chốt 2026-08-29; chỉ 2 tầng WPF↔thiết bị; thiết lập scene & auto log ra file; prompt thực thi ở DocBusinessThienAn/HữuNghị-ChiLăng/VideoWall/
- [VideoWall cascade — kiến trúc](videowall-cascade-architecture.md) — DS-C66S 1 bộ trung tâm + 3 bộ con; backend chỉ nói ISAPI với bộ trung tâm; 2 doc cũ (`GiaiThich_KetNoi_*`, `SoDoCauHinh_*`) đã xoá vì sai mô hình phần mềm
- [VideoWall cascade — tiến độ](videowall-cascade-status.md) — backend + cleanup đã áp, `dotnet test` pass, staged chưa commit; còn KB-00 probe + seed + chốt panel px + WPF + FE (từ 2026-09-09)

## Database / MCP
- [MCP DAB database access](mcp-dab-database-access.md) — 3 DAB MCP server; `mssql_staging` = 10.10.8.30 = source of truth
- [Read-only DB queries: just run them](read-only-db-queries-just-run.md) — chạy read không cần hỏi; vẫn cấm write

## Reference
- [Tài liệu Nghiệp vụ HN-CL](../../DocBusinessThienAn/HữuNghị-ChiLăng/INDEX.md) — Nguồn sự thật duy nhất cho tài liệu nghiệp vụ Hữu Nghị - Chi Lăng (luôn mở file này trước).
- [SqlSugar docs](sqlsugar-docs.md) — link no-entity / raw SQL / JSON→SQL
- [Tech decisions](tech-decisions.md) — bảo trì `.agents/manifest.json` ↔ frontmatter (AG-Kit tooling, không phải app)

---

## Tạo lại junction (chạy 1 lần trên mỗi máy, không cần admin)

Tên thư mục project (`c--ThienAn` / `d--ThienAn` …) và ổ đĩa theo **đường dẫn repo trên máy đó**.

Máy hiện tại (`D:\ThienAn`):
```cmd
rmdir /S /Q "%USERPROFILE%\.claude\projects\d--ThienAn\memory"
mklink /J "%USERPROFILE%\.claude\projects\d--ThienAn\memory" "D:\ThienAn\.agents\memory"
```
Máy repo ở `C:\ThienAn`:
```cmd
rmdir /S /Q "%USERPROFILE%\.claude\projects\c--ThienAn\memory"
mklink /J "%USERPROFILE%\.claude\projects\c--ThienAn\memory" "C:\ThienAn\.agents\memory"
```
