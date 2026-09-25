# Memory Index

> 🔴 **Mọi file memory session GHI VÀO thư mục NÀY** (`<repo>/.agents/memory/`) — **KHÔNG BAO GIỜ**
> ghi vào `~/.claude/projects/*/memory/`. Đây là **nguồn duy nhất**; path global chỉ là **junction
> trỏ về đây**. Nếu path global là thư mục thật (không phải junction) trên máy hiện tại ⇒ junction
> chưa/hỏng: cứ ghi local, rồi tạo lại junction (snippet cuối file).
>
> ⚠️ **LƯU Ý QUAN TRỌNG**: Toàn bộ quy tắc, quy ước dự án và hướng dẫn AI tập trung DUY NHẤT tại
> [`../rules/thienan_rules.md`](../rules/thienan_rules.md). **TUYỆT ĐỐI KHÔNG TẠO FILE FEEDBACK-*.MD RẢI RÁC.**

## User & Quy định
- [User profile](user-profile.md) — backend dev trên TA-ITS015 (ITS/C2C ISO 14827); viết & trả lời tiếng Việt
- [Quy định chung & Kiến trúc Thiên An](../rules/thienan_rules.md) — **Nguồn sự thật duy nhất (SSOT)** cho toàn bộ quy định dự án (BẮT BUỘC AI ĐỌC TRỰC TIẾP TỪ FILE NÀY: quy định git, commit feat/fix, ưu tiên ORM SqlSugar cấm raw SQL DML, không tự xoá prompt - giữ để review, no-Async suffix, plan-handoff, no manual DDL, code-first, entity, testing, chuẩn code Frontend Vue 3 / VxeTable / Element Plus...). Mọi rule mới chỉ thêm vào `thienan_rules.md`, không ghi đúp vào file memory này.
- **Gortex-First & Default-as-Fallback Protocol**: Luôn luôn ưu tiên dùng bộ công cụ Gortex MCP (`call_mcp_tool` với `ServerName: "gortex"`) cho mọi thao tác mã nguồn (tìm kiếm symbol, usages, dependencies, call chain, đọc và sửa code); nếu Gortex không thực hiện được (bị lỗi, timeout, chưa index file, hoặc thao tác trên tài liệu Markdown) thì mới chuyển sang (fallback) dùng các công cụ mặc định (`view_file`, `grep_search`, `replace_file_content`).

## Database / MCP
- [MCP DAB database access](mcp-dab-database-access.md) — 3 DAB MCP server; `mssql_staging` = 10.10.8.30 = source of truth
- [Read-only DB queries: just run them](read-only-db-queries-just-run.md) — chạy read không cần hỏi; vẫn cấm write

## Reference
- [Biểu mẫu Git Flow F10.TAC-CN01](../../DocBusinessThienAn/QuyDinhChung/doc/F10.TAC-CN01-git-rules.md) — Tài liệu hướng dẫn sử dụng Git Flow, tạo nhánh và các lưu ý khi commit code (chuẩn ISO F10 của Thiên Ân).
- [Tài liệu Nghiệp vụ HN-CL](../../DocBusinessThienAn/HữuNghị-ChiLăng/INDEX.md) — Nguồn sự thật duy nhất cho tài liệu nghiệp vụ Hữu Nghị - Chi Lăng (luôn mở file này trước).
- [SqlSugar docs](sqlsugar-docs.md) — link no-entity / raw SQL / JSON→SQL
- [Tech decisions](tech-decisions.md) — bảo trì `.agents/manifest.json` ↔ frontmatter (AG-Kit tooling, không phải app)

---

## Tạo lại junction (chạy 1 lần trên mỗi máy, không cần admin)

Máy repo ở `C:\ThienAn`:
```cmd
rmdir /S /Q "%USERPROFILE%\.claude\projects\c--ThienAn\memory"
mklink /J "%USERPROFILE%\.claude\projects\c--ThienAn\memory" "C:\ThienAn\.agents\memory"
```
