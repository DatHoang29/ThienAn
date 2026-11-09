# Mục lục agent (vai chuyên gia)

<!-- Nguồn duy nhất: .agents/agent/<tên>.md; không chép tệp vai sang thư mục riêng của công cụ nào. Thêm, đổi tên, xóa vai thì sửa bảng này cùng lúc; name trong tệp vai phải trùng tên tệp. Mục lục nạp sẵn theo dòng @ trong AGENTS.md (Claude Code tự nạp; Codex, Antigravity qua hook SessionStart). -->

Việc khớp cột "Dùng khi": đọc `.agents/agent/<tên>.md`, thông báo `🤖 **Applying knowledge of @[tên]...**`, làm theo vai đó kèm các skill ở dòng `skills:` của tệp vai. Cần chạy riêng (Rule 19.70) thì gọi subagent thường kèm câu "đọc `.agents/agent/<tên>.md` và làm theo".

| Vai | Dùng khi |
|---|---|
| `backend-specialist` | Backend .NET: WebAPI, Worker, Wolverine, SqlSugar, SQL Server |
| `code-archaeologist` | Mã cũ, mã rối, thiếu tài liệu: giải thích, refactor, hiện đại hóa |
| `database-architect` | Schema, truy vấn, migration, index, mô hình dữ liệu |
| `debugger` | Lỗi phức tạp, lỗi production, tìm nguyên nhân gốc |
| `documentation-writer` | Chỉ khi người dùng yêu cầu viết tài liệu (README, API, changelog) |
| `explorer-agent` | Khảo sát codebase, phân tích kiến trúc, điều tra |
| `frontend-specialist` | Giao diện Vue 3: Element Plus, VxeTable, SCSS, i18n |
| `orchestrator` | Việc cần nhiều vai phối hợp, nhiều miền chạy song song |
| `performance-optimizer` | Chậm, tốn bộ nhớ, CPU: đo và tối ưu |
| `project-planner` | Kế hoạch tính năng lớn, dự án mới: chia việc, thứ tự phụ thuộc |
| `security-auditor` | Rà bảo mật: OWASP, injection, xác thực, mã hóa, chuỗi cung ứng |
| `test-engineer` | Viết, sửa test, tăng độ phủ (xUnit BE, Playwright FE) |
