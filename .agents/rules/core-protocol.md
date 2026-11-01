---
name: core-protocol
version: 1.1.0
priority: P0
trigger: always_on
---

# Core Protocol - ThienAn

> Quy tắc nền luôn bật cho mọi yêu cầu: nạp skill, đọc memory, kế thừa phiên, ngôn ngữ, hỏi lại, clean code.
> 📎 Quy tắc dự án ThienAn: xem [thienan_rules.md](./thienan_rules.md) (ưu tiên cao hơn khi trùng).

---

## 1. Modular Skill Loading Protocol

Agent activated → Check frontmatter `skills:` → Read `SKILL.md` (INDEX) → Read specific sections.
- **Selective Reading:** DO NOT read all files in a skill folder. Read `SKILL.md` first, then only read sections matching the request.
- **Rule Priority:** P0 (Workspace Rules in `.agents/rules/`) > P1 (Agent `.md`) > P2 (SKILL.md).

### 1.1 Skill Announcement (MANDATORY)

Mỗi khi nạp và áp dụng skill, BẮT BUỘC thông báo trước:
```markdown
📚 **Using skill: `@[skill-name]`...**
```

---

## 2. Enforcement Protocol

1. **Khi agent kích hoạt:** Đọc Rules → Kiểm tra Frontmatter → Nạp SKILL.md → Thực thi.
2. **Tuân thủ quy trình kiểm tra:** Đối chiếu bảng định tuyến tại [request-routing.md](./request-routing.md).

---

## 3. System Map & Memory Read

> 🔴 **MANDATORY:** Khi bắt đầu phiên làm việc, BẮT BUỘC đọc [`.agents/memory/MEMORY.md`](../memory/MEMORY.md) để nạp các quy ước và quyết định dự án.
> Bản đồ thư mục `.agents/` (rule, skill, agent, hook, MCP) xem [`.agents/README.md`](../README.md).

---

## 4. Multi-Agent & Session Handover Protocol (Rule 19.112)

Khi tiếp quản phiên làm việc từ AI khác (Claude Code, Antigravity, phiên trước):
1. **Đọc bộ nhớ phiên trước:** BẮT BUỘC đọc thư mục memory (`.claude/projects/.../memory/` hoặc `.agents/memory/`) và các sự kiện/prompt gần nhất trong transcript (`.jsonl`).
2. **Nắm bắt 3 yếu tố cốt lõi:**
   - Quyết định kiến trúc & trade-off đã thống nhất với người dùng.
   - Các công việc đang làm dở dang và các bài test đỏ trong quy trình TDD.
   - Các chỉ đạo đặc biệt của người dùng (gộp file, quy ước SSOT, chặn lỗi).
3. **⛔ CẤM TUYỆT ĐỐI:** Tự ý phán đoán thay thế, cấm nới lỏng validation hoặc sửa ngọn chỉ để làm xanh test tạm bợ. Mọi hành động phải liền mạch và bảo toàn hướng giải quyết gốc rễ của hệ thống.

---

## 5. Language Handling

Khi prompt của người dùng KHÔNG phải tiếng Anh:
1. Tự động dịch hiểu nội dung.
2. Phản hồi bằng ngôn ngữ của người dùng (tiếng Việt).
3. Code comments / biến / hàm: giữ nguyên bằng tiếng Anh.

---

## 6. Socratic Gate

- **Nguyên tắc:** Chỉ hỏi khi yêu cầu mơ hồ hoặc thiếu thông tin mang tính quyết định thiết kế. Khi yêu cầu đã rõ ràng, bắt tay vào thực hiện ngay, không hỏi rườm rà.
- **Reference:** Quy trình chi tiết tại `@[skills/brainstorming]`.

---

## 7. Clean Code (Global Mandatory)

Mọi code BẮT BUỘC tuân thủ `@[skills/clean-code]`:
- Code ngắn gọn, trực diện, không over-engineering, tự giải thích (self-documenting).
- Tuân thủ Minimal Diff (Rule 19.6, 19.35, 19.89) và dọn sạch mã thừa/obsolete (Rule 19.68).
- **Bỏ là bỏ hết các liên đới (Cascading Cleanup):** Khi gỡ bỏ một tính năng, nút bấm, thành phần giao diện hoặc API: BẮT BUỘC rà soát và xóa bỏ triệt để toàn bộ thành phần liên đới (hàm xử lý, biến state/computed/props, DTO, API Controller, Handler CQRS, hằng số BaseMsg, i18n không còn dùng, và cập nhật/xóa test tương ứng). Tuyệt đối không để lại mã chết / zombie code.
