# Mục lục skill

> Nguồn duy nhất: `.agents/skills/<tên-skill>/SKILL.md`. Codex, Gemini CLI, Antigravity tự quét `.agents/skills/`; Claude Code, Gemini CLI nạp mục lục này qua dòng `@` trong `AGENTS.md`.
> Thêm, đổi tên hay xóa skill thì sửa bảng này cùng lúc.

Khi việc đang làm khớp cột "Dùng khi": đọc `SKILL.md` của skill đó (tệp phụ trong thư mục chỉ đọc khi `SKILL.md` chỉ tới), thông báo `📚 Using skill: @[tên-skill]...` rồi áp dụng. Rule trong `.agents/rules/` ưu tiên hơn skill khi trùng (`core-protocol.md` §1).

| Skill | Dùng khi |
|---|---|
| `clean-code` | Luôn áp dụng khi viết, sửa, review hoặc refactor mã: nêu giả định, chọn cách đơn giản nhất, sửa đúng chỗ, đặt tiêu chí kiểm chứng |
| `tdd` | Làm tính năng hoặc sửa lỗi theo kiểu test trước; viết, sửa, rà test (xUnit theo luồng thật trên DB local, Playwright E2E) |
| `brainstorming` | Yêu cầu mơ hồ hoặc cần quyết định thiết kế: hỏi đúng chỗ, đưa 2-3 phương án kèm đánh đổi và đề xuất |
| `plan-writing` | Lập kế hoạch nhiều bước hoặc ghi chỗ cần sửa: tệp prompt theo documentation-standards §1, mỗi bước có cách kiểm |
| `code-review` | Review nhánh, PR hoặc mã đang sửa so với một mốc theo hai trục: đúng rule `.agents/rules/` và đúng tài liệu nghiệp vụ |
