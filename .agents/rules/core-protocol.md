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
