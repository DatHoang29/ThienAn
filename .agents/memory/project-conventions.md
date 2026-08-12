---
type: project
created: 2026-05-25
updated: 2026-07-12
---

# Project Conventions

## Git Workflow
- Always create a new dedicated branch for major code changes.
- Branch name format should follow: `feature/[task-slug]` or `fix/[bug-slug]`.

## Supported AI platforms (AG Kit)
- AG Kit **only supports Gemini CLI and Google Antigravity**.
- Do not claim compatibility with Claude Code, Cursor, Copilot, Windsurf, or other assistants unless the user explicitly expands scope.
- Copy on the website, docs, FAQ, README, and marketing should describe AG Kit as a toolkit for Gemini CLI / Antigravity-style agent setups.

## C# Code & Payload Rules
- **DTO vs Anonymous Objects**: Khi dữ liệu CÓ XỬ LÝ logic nội bộ thì PHẢI tạo **DTO**. Nếu dữ liệu CHỈ MAP để gửi đi (bên khác xử lý) thì PHẢI dùng **Anonymous Objects** (hoặc `Dictionary`).
- **Namespaces**: Không dùng Full Namespace dài dòng trong thân class (VD: `ShareDataWorker.Core.Dto...`). Bắt buộc khai báo `using` ở đầu file.
- **Null Reference (CS8601)**: Tuyệt đối chú ý gán giá trị dự phòng (VD: `?? string.Empty`) để dập tắt cảnh báo CS8601 khi gán string? cho string.
- **Async Method Naming**: Tuyệt đối không thêm hậu tố `Async` vào tên các phương thức bất đồng bộ (VD: Dùng `HandleIncomingConnection` thay vì `HandleIncomingConnectionAsync`).
