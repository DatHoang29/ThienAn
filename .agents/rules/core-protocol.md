---
name: core-protocol
version: 1.2.0
priority: P0
trigger: always_on
---

# Core Protocol - ThienAn

> Quy tắc nền luôn bật. [thienan_rules.md](./thienan_rules.md) ưu tiên cao hơn khi trùng.

## 1. Skill & agent

- Thứ tự ưu tiên: rule (`.agents/rules/`) > vai (`.agents/agent/`) > skill (`.agents/skills/`). Mục lục: `skills/INDEX.md`, `agent/INDEX.md`.
- Chỉ đọc `SKILL.md` rồi phần khớp việc, không đọc cả thư mục skill. Thông báo trước khi áp dụng: `📚 **Using skill: @[tên-skill]...**`.

## 2. Định tuyến

Đối chiếu bảng định tuyến [request-routing.md](./request-routing.md) và đọc tệp rule của miền trước khi làm.

## 3. Memory

- Memory duy nhất ở `.agents/memory/`: mục lục `MEMORY.md` nạp sẵn qua `AGENTS.md` (Codex đọc khi bắt đầu phiên). Ghi nhớ mới ghi vào đó, không ghi kho riêng của công cụ.
- Bản đồ `.agents/`: [`.agents/README.md`](../README.md).

## 4. Kế thừa phiên (Rule 19.112)

Tiếp quản phiên trước (Claude Code, Antigravity, phiên cũ): đọc `.agents/memory/` và log phiên gần nhất (`.jsonl`); nắm quyết định kiến trúc đã chốt, việc dở dang và test đỏ, chỉ đạo riêng của người dùng (gộp tệp, quy ước SSOT, chặn lỗi). ⛔ Tự phán đoán thay thế, nới validation hay sửa ngọn để test xanh.

## 5. Ngôn ngữ

Trả lời bằng ngôn ngữ của người dùng (tiếng Việt); comment, tên biến, hàm giữ tiếng Anh.

## 6. Socratic Gate

Chỉ hỏi khi yêu cầu mơ hồ hoặc thiếu thông tin quyết định thiết kế; yêu cầu rõ thì làm ngay (`@[skills/brainstorming]`).

## 7. Clean Code

Mọi code tuân thủ `@[skills/clean-code]` (gồm nguyên tắc Karpathy):
- Trước khi viết: nêu giả định, chọn cách đơn giản nhất; khi sửa: mọi dòng đổi phải truy được về yêu cầu; đặt tiêu chí kiểm chứng (test) rồi lặp đến khi đạt.
- Minimal Diff (Rule 19.6, 19.35, 19.89); dọn mã thừa, obsolete (Rule 19.68).
- **Bỏ là bỏ hết liên đới:** gỡ tính năng, nút, API thì xóa hết phần liên quan (hàm, state/computed/props, DTO, controller, handler CQRS, BaseMsg, i18n, test); ⛔ để lại mã chết.
