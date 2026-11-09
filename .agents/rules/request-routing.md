---
name: request-routing
version: 1.1.0
priority: P0
trigger: always_on
description: Phân loại yêu cầu và định tuyến quy chuẩn theo miền nghiệp vụ.
---

# Request Routing - ThienAn

> Always-active. Phân loại yêu cầu và định tuyến đọc tệp quy chuẩn chuyên biệt trước khi phản hồi.

---

## 📥 1. BẢNG PHÂN LOẠI YÊU CẦU & ĐỊNH TUYẾN TỆP QUY CHUẨN

| Loại Việc / Miền | Từ Khóa Nhận Diện | Tệp Quy Chuẩn Bắt Buộc |
| :--- | :--- | :--- |
| **Backend .NET** | C#, WebAPI, Worker, Wolverine, SqlSugar, Entity, DTO | [backend-dotnet.md](./backend-dotnet.md) |
| **Frontend Vue** | Vue 3, SFC, Element Plus, VxeTable, SCSS, i18n, TS | [frontend-vue_thienan.md](./frontend-vue_thienan.md) |
| **Kiểm Thử** | xUnit BE local DB, Playwright FE E2E, dọn artifacts | [testing-rules_thienan.md](./testing-rules_thienan.md) |
| **Database** | Gortex MCP, DAB MCP local, quy chuẩn script `.sql` | [database-mcp_thienan.md](./database-mcp_thienan.md) |
| **Tài Liệu / KPI** | MasterPlan, file Prompt, Excel OpenXML, transcript, KPI | [documentation-standards_thienan.md](./documentation-standards_thienan.md) |
| **Git Workflow** | Nhánh, commit tiếng Việt có dấu, đồng bộ `dev` | [git-workflow_thienan.md](./git-workflow_thienan.md) |
| **Hỏi / Giải thích** | "là gì", "thế nào", "giải thích", "what is" | Trả lời trực tiếp (TIER 0) |
| **App / Feature mới** | "xây dựng", "triển khai", "từ đầu", "refactor" | Lập kế hoạch thành prompt `DocBusinessThienAn/<DựÁn>/<PhânHệ>/Prompt/<task-slug>-prompt.md` ([documentation-standards §1](./documentation-standards_thienan.md)) |

---

## ⚡ 2. SUBAGENTS & XỬ LÝ LỆ THUỘC (Rule 19.70)

Prompt có từ 2 task trở lên hoặc nhiều message liên tiếp:
- **Task độc lập:** Kích hoạt subagents chạy song song.
- **Task lệ thuộc:** BẮT BUỘC chờ task tiền đề hoàn tất trước khi thực hiện task tiếp theo.
- **Nhận diện:** danh sách đánh số (`1.`, `2.`…), gạch đầu dòng, nhiều tệp `@prompt-*.md`, hoặc các phần ngăn bởi `---`/`===`.
- **Tiết kiệm ngữ cảnh:** subagent thường nạp lại toàn bộ rule; khảo sát, tìm kiếm thì dùng subagent Explore; yêu cầu subagent trả tóm tắt ngắn.

Thông báo kích hoạt agent chuyên gia:
```markdown
🤖 **Applying knowledge of `@[agent-name]`...**
```
