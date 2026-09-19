@AGENTS.md
@.agents/rules/core-protocol.md
@.agents/rules/request-routing.md
@.agents/rules/universal-rules.md
@.agents/rules/thienan_rules.md

# Master AI Harness - ThienAn (Claude Code / Desktop)

> 🔴 **SINGLE SOURCE OF TRUTH (SSOT): `.agents/`**
> Tất cả quy tắc kiến trúc, specialist agents, modular skills, workflows, lifecycle hooks và memory tập trung DUY NHẤT tại thư mục **`.agents/`**.
> Bạn PHẢI tra cứu và thực thi trực tiếp từ `.agents/`, KHÔNG hardcode danh sách thủ công.

---

## 🗺️ Dynamic AG-Kit Registry (`.agents/`)
- **Quy tắc dự án (P0):** Đã nạp tự động qua `@.agents/rules/` ở trên (xem chi tiết [thienan_rules.md](file:///.agents/rules/thienan_rules.md)).
- **Specialist Agents:** Tự động khám phá và nhận diện vai trò từ thư mục [`.agents/agent/`](file:///.agents/agent/).
- **Modular Skills:** Tự động khám phá và nạp file `SKILL.md` từ [`.agents/skills/`](file:///.agents/skills/) (bao gồm cả các skill `gortex-*` do Gortex sinh ra). Luôn thông báo `📚 Using skill: @[skill-name]...` trước khi áp dụng.
- **Workflows & Slash Commands:** Tra cứu quy trình tương ứng tại [`.agents/workflows/{command}.md`](file:///.agents/workflows/).
- **Memory (SSOT):** Luôn đọc [`.agents/memory/MEMORY.md`](file:///.agents/memory/MEMORY.md). Tuyệt đối KHÔNG ghi vào `~/.claude/projects/*/memory/`.
- **System Architecture Map:** Xem bảng tổng hợp tại [`.agents/ARCHITECTURE.md`](file:///.agents/ARCHITECTURE.md).

---

## 🛠️ Build & Test Commands
- Backend WebAPI: `dotnet build src/TAC_WebAPI/TAC_WebAPI.csproj`
- Backend ShareData: `dotnet build src/Services/ShareDataWorker/ShareDataWorker.csproj`
- Run Tests: `dotnet test tests/test.csproj`
- Frontend: `cd TA-ITS015-WEBVUE-V1.0 && npm run build`

---

## 🛑 Critical Mandatory Safeguards
1. **Strict Local Database for `dotnet test`**: Mọi connection string test PHẢI trỏ về `local` (`localhost`, `127.0.0.1`, `(localdb)`, `.`). Nếu phát hiện IP remote (ví dụ `10.10.8.30`), **HỦY test ngay lập tức và báo cáo cho user**.
2. **Auto-Cleanup Completed Prompt & Plan Files**: Tự động xóa các file prompt/plan tạm sau khi hoàn thành nhiệm vụ (ví dụ `*-prompt-*.md`, `{task-slug}.md`).
3. **Strict Manual SQL Execution**: Chỉ xuất file `.sql` ra đĩa để user review. KHÔNG tự ý thực thi DDL/DML trực tiếp làm thay đổi database.
