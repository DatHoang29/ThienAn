# Mục lục agent (vai chuyên gia)

<!-- Nguồn duy nhất: .agents/agent/<tên>.md; không chép tệp vai sang thư mục riêng của công cụ nào. Thêm, đổi tên, xóa vai thì sửa bảng này cùng lúc; name trong tệp vai phải trùng tên tệp. Claude Code, Gemini CLI nạp mục lục qua dòng @ trong AGENTS.md, Codex đọc theo đường dẫn. -->

Việc khớp cột "Dùng khi": đọc `.agents/agent/<tên>.md`, thông báo `🤖 **Applying knowledge of @[tên]...**`, làm theo vai đó kèm các skill ở dòng `skills:` của tệp vai. Cần chạy riêng (Rule 19.70) thì gọi subagent thường kèm câu "đọc `.agents/agent/<tên>.md` và làm theo".

| Vai | Dùng khi |
|---|---|
| `backend-specialist` | Việc backend .NET (WebAPI, Worker, Wolverine, SqlSugar, SQL Server): API, logic phía máy chủ, nối CSDL, bảo mật |
| `code-archaeologist` | Đọc mã cũ, mã rối, hệ thống thiếu tài liệu; refactor, lập kế hoạch hiện đại hóa, giải thích codebase |
| `database-architect` | Thiết kế schema, tối ưu truy vấn, migration, index, mô hình dữ liệu |
| `debugger` | Lỗi phức tạp, lỗi production, sập, tìm nguyên nhân gốc |
| `documentation-writer` | Chỉ khi người dùng yêu cầu viết tài liệu (README, tài liệu API, changelog); không tự dùng khi đang lập trình |
| `explorer-agent` | Khảo sát codebase, phân tích kiến trúc sâu, rà soát ban đầu, điều tra |
| `frontend-specialist` | Giao diện Vue 3 (Element Plus, VxeTable, SCSS, i18n): component, style, state, bố cục, kiến trúc FE |
| `orchestrator` | Việc cần nhiều vai phối hợp hoặc chạy song song nhiều miền (BE, FE, test, bảo mật) |
| `performance-optimizer` | Chậm, tốn bộ nhớ hoặc CPU: đo, profiling, tối ưu tốc độ |
| `project-planner` | Lập kế hoạch tính năng lớn hoặc dự án mới: chia việc, phân vai, thứ tự phụ thuộc |
| `security-auditor` | Rà bảo mật: lỗ hổng, OWASP, XSS, injection, xác thực, mã hóa, chuỗi cung ứng |
| `test-engineer` | Viết test, tăng độ phủ, gỡ test lỗi (xUnit BE, Playwright FE) |
