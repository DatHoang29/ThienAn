---
name: universal-rules
version: 1.1.0
priority: P0
trigger: always_on
---

# Universal Rules (TIER 0) - AG Kit

> Always-active rules. Áp dụng cho mọi yêu cầu độc lập với miền nghiệp vụ.
> 📎 Quy tắc dự án ThienAn: xem `.agents/rules/thienan_rules.md` (ưu tiên cao hơn khi trùng).

---

## 🌐 Language Handling

Khi prompt của người dùng KHÔNG phải tiếng Anh:
1. Tự động dịch hiểu nội dung.
2. Phản hồi bằng ngôn ngữ của người dùng (tiếng Việt).
3. Code comments / biến / hàm: giữ nguyên bằng tiếng Anh.

---

## 🧹 Clean Code (Global Mandatory)

Mọi code BẮT BUỘC tuân thủ `@[skills/clean-code]`:
- Code ngắn gọn, trực diện, không over-engineering, tự giải thích (self-documenting).
- Tuân thủ Minimal Diff (Rule 19.6, 19.35, 19.89) và dọn sạch mã thừa/obsolete (Rule 19.68).
- **Bỏ là bỏ hết các liên đới (Cascading Cleanup):** Khi gỡ bỏ một tính năng, nút bấm, thành phần giao diện hoặc API: BẮT BUỘC rà soát và xóa bỏ triệt để toàn bộ thành phần liên đới (hàm xử lý, biến state/computed/props, DTO, API Controller, Handler CQRS, hằng số BaseMsg, i18n không còn dùng, và cập nhật/xóa test tương ứng). Tuyệt đối không để lại mã chết / zombie code.
