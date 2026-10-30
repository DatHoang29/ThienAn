---
name: core-protocol
version: 1.1.0
priority: P0
trigger: always_on
---

# Core Protocol - AG Kit

> The highest-priority workspace rules. Modular skill loading, announcement protocol, and memory initialization.

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
> Tra cứu toàn bộ Agent/Skill/Script tại [`.agents/ARCHITECTURE.md`](../ARCHITECTURE.md) khi thực sự cần.

---

## 4. Multi-Agent & Session Handover Protocol (Rule 19.112)

Khi tiếp quản phiên làm việc từ AI khác (Claude Code, Antigravity, phiên trước):
1. **Đọc bộ nhớ phiên trước:** BẮT BUỘC đọc thư mục memory (`.claude/projects/.../memory/` hoặc `.agents/memory/`) và các sự kiện/prompt gần nhất trong transcript (`.jsonl`).
2. **Nắm bắt 3 yếu tố cốt lõi:**
   - Quyết định kiến trúc & trade-off đã thống nhất với người dùng.
   - Các công việc đang làm dở dang và các bài test đỏ trong quy trình TDD.
   - Các chỉ đạo đặc biệt của người dùng (gộp file, quy ước SSOT, chặn lỗi).
3. **⛔ CẤM TUYỆT ĐỐI:** Tự ý phán đoán thay thế, cấm nới lỏng validation hoặc sửa ngọn chỉ để làm xanh test tạm bợ. Mọi hành động phải liền mạch và bảo toàn hướng giải quyết gốc rễ của hệ thống.
