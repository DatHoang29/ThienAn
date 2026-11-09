# Mục lục skill

> Nguồn duy nhất: `.agents/skills/<tên-skill>/SKILL.md`. Codex, Gemini CLI, Cline, Antigravity tự quét `.agents/skills/`; Claude Code, Gemini CLI nạp mục lục này qua dòng `@` trong `AGENTS.md`, Kiro qua `.kiro/steering/main.md`.
> Thêm, đổi tên hay xóa skill thì sửa bảng này cùng lúc.

Khi việc đang làm khớp cột "Dùng khi": đọc `SKILL.md` của skill đó (tệp phụ trong thư mục chỉ đọc khi `SKILL.md` chỉ tới), thông báo `📚 Using skill: @[tên-skill]...` rồi áp dụng. Rule trong `.agents/rules/` ưu tiên hơn skill khi trùng (`core-protocol.md` §1).

| Skill | Dùng khi |
|---|---|
| `api-patterns` | Thiết kế API REST/GraphQL/tRPC: định dạng phản hồi, phiên bản, phân trang, xác thực. Không dùng cho giao diện |
| `architecture` | Ra quyết định kiến trúc, cân nhắc đánh đổi, viết ADR. Không dùng khi viết mã trực tiếp |
| `brainstorming` | Yêu cầu phức tạp, tính năng mới, yêu cầu chưa rõ: hỏi làm rõ, đưa phương án trước khi làm |
| `clean-code` | Luôn áp dụng khi viết mã |
| `code-review` | Review nhánh, PR hoặc thay đổi đang làm so với một mốc (commit, nhánh, tag) theo hai trục: chuẩn mã của repo và đúng yêu cầu |
| `database-design` | Thiết kế schema, chọn ORM, lập kế hoạch migration, tối ưu truy vấn, làm việc với tệp SQL |
| `karpathy-guidelines` | Viết, review, refactor mã: nêu giả định, chọn cách đơn giản nhất, sửa đúng chỗ, đặt tiêu chí kiểm chứng |
| `memory-system` | Người dùng bảo ghi nhớ, hoặc đầu phiên cần nhớ lại quyết định cũ |
| `parallel-agents` | Việc cần từ 2 vai chuyên gia trở lên hoặc phân tích nhiều miền song song |
| `plan-writing` | Lập kế hoạch nhiều bước: chia việc, phụ thuộc, tiêu chí kiểm chứng |
| `systematic-debugging` | Gỡ lỗi phức tạp, tìm nguyên nhân gốc dựa trên bằng chứng |
| `tdd` | Làm tính năng hoặc sửa lỗi theo kiểu test trước (red-green-refactor), test tích hợp |
| `test-audit` | Viết, sửa, review hoặc rà soát test |
| `verify-changes` | Sau khi viết hoặc sửa mã: chạy thật để chứng minh mã chạy đúng |
| `webapp-testing` | Viết test E2E Playwright, kiểm luồng người dùng trên web |
