# AI Harness - Single Source of Truth

> 🔴 **SINGLE SOURCE OF TRUTH (SSOT): `.agents/`**
> Tất cả quy tắc kiến trúc, specialist agents, modular skills, workflows và memory tập trung DUY NHẤT tại thư mục **`.agents/`**.
> Tra cứu và thực thi trực tiếp từ `.agents/`, không hardcode danh sách thủ công.

---

## 📥 Nạp sẵn mỗi phiên

Đọc các tệp dưới đây trước khi làm việc. Claude Code (qua `.claude/CLAUDE.md`) và Gemini CLI (qua `context.fileName` trong `.gemini/settings.json`) tự nạp các dòng `@`; Codex đọc theo đường dẫn.

@./.agents/rules/thienan_rules.md
@./.agents/rules/core-protocol.md
@./.agents/rules/request-routing.md
@./.agents/rules/gortex-tool-map.md
@./.agents/skills/INDEX.md
@./.agents/agent/INDEX.md
@./.agents/memory/MEMORY.md

---

## 🗺️ Dynamic AG-Kit Registry (`.agents/`)
- **Specialist Agents:** Vai trò trong [`.agents/agent/`](.agents/agent/), mục lục và khi nào dùng ở [`.agents/agent/INDEX.md`](.agents/agent/INDEX.md).
- **Modular Skills:** Nạp `SKILL.md` từ [`.agents/skills/`](.agents/skills/), mục lục và khi nào dùng ở [`.agents/skills/INDEX.md`](.agents/skills/INDEX.md). Thông báo `📚 Using skill: @[skill-name]...` trước khi áp dụng.
- **Bản đồ `.agents/` (rule, skill, agent, hook, MCP):** Xem [`.agents/README.md`](.agents/README.md).

---

## 🛠️ Build & Test Commands
- Backend WebAPI: `dotnet build TA-ITS015-WEBAPI-V1.0/src/TAC_WebAPI/TAC_WebAPI.csproj`
- Backend ShareData: `dotnet build TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker/ShareDataWorker.csproj`
- Run Tests: `dotnet test tests/BE/test.csproj`
- Frontend: `cd TA-ITS015-WEBVUE-V1.0 && npm run build`


<!-- gortex:communities:start -->
<!-- gortex:communities:end -->
