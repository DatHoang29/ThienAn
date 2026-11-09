---
name: core-protocol
version: 1.2.0
priority: P0
trigger: always_on
---

# Core Protocol - ThienAn

> Quy tắc nền luôn bật. [thienan_rules.md](./thienan_rules.md) ưu tiên cao hơn khi trùng.

## 1. Skill & agent

- Thứ tự ưu tiên: rule (`.agents/rules/`) > vai (`.agents/agent/`) > skill (`.agents/skills/`).
- Chỉ đọc `SKILL.md` rồi phần khớp việc, không đọc cả thư mục skill. Thông báo trước khi áp dụng: `📚 **Using skill: @[tên-skill]...**`.

## 2. Định tuyến

Đối chiếu bảng định tuyến [request-routing.md](./request-routing.md) và đọc tệp rule của miền trước khi làm.

## 3. Memory

- Memory duy nhất ở `.agents/memory/`: mục lục `MEMORY.md` nạp sẵn qua `AGENTS.md` (Codex, Antigravity nạp qua hook SessionStart). Ghi nhớ mới ghi vào đó, không ghi kho riêng của công cụ.
- Bản đồ `.agents/`: [`.agents/README.md`](../README.md).

## 4. Kế thừa phiên (Rule 19.112)

Ngoài P0 STT 72, 73: từ log phiên gần nhất (`.jsonl`) nắm việc dở dang, test đỏ và chỉ đạo riêng của người dùng (gộp tệp, quy ước SSOT, chặn lỗi).

## 5. Ngôn ngữ

Trả lời bằng ngôn ngữ của người dùng (tiếng Việt); comment, tên biến, hàm giữ tiếng Anh.

## 6. Socratic Gate

Chỉ hỏi khi yêu cầu mơ hồ hoặc thiếu thông tin quyết định thiết kế; yêu cầu rõ thì làm ngay (`@[skills/brainstorming]`).

## 7. Clean Code

Mọi code tuân thủ `@[skills/clean-code]` (gồm nguyên tắc Karpathy), sửa tối thiểu theo P0 STT 13, 48; dọn mã thừa, obsolete (Rule 19.68).
- **Bỏ là bỏ hết liên đới:** gỡ tính năng, nút, API thì xóa hết phần liên quan (hàm, state/computed/props, DTO, controller, handler CQRS, BaseMsg, i18n, test); ⛔ để lại mã chết.
