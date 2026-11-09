# AI Harness - Single Source of Truth

> 🔴 **SINGLE SOURCE OF TRUTH (SSOT): `.agents/`**
> Tất cả quy tắc kiến trúc, specialist agents, modular skills, workflows và memory tập trung DUY NHẤT tại thư mục **`.agents/`**.
> Tra cứu và thực thi trực tiếp từ `.agents/`, không hardcode danh sách thủ công.

---

## 📥 Nạp sẵn mỗi phiên

Đọc các tệp dưới đây trước khi làm việc. Claude Code (qua `.claude/CLAUDE.md`) tự nạp các dòng `@`; Codex, Antigravity nạp qua hook SessionStart (`.agents/hooks/session-context.mjs`).

@./.agents/rules/thienan_rules.md
@./.agents/rules/core-protocol.md
@./.agents/rules/request-routing.md
@./.agents/skills/INDEX.md
@./.agents/agent/INDEX.md
@./.agents/memory/MEMORY.md

Bản đồ `.agents/` (rule, skill, agent, hook, MCP): [`.agents/README.md`](.agents/README.md).

---

## 🛠️ Build & Test Commands
- Backend WebAPI: `dotnet build TA-ITS015-WEBAPI-V1.0/src/TAC_WebAPI/TAC_WebAPI.csproj`
- Backend ShareData: `dotnet build TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker/ShareDataWorker.csproj`
- Run Tests: `dotnet test tests/BE/test.csproj`


<!-- gortex:communities:start -->
<!-- gortex:communities:end -->
