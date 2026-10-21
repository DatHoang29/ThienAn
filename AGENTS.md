# AI Harness - Single Source of Truth

> 🔴 **SINGLE SOURCE OF TRUTH (SSOT): `.agents/`**
> Tất cả quy tắc kiến trúc, specialist agents, modular skills, workflows và memory tập trung DUY NHẤT tại thư mục **`.agents/`**.
> Tra cứu và thực thi trực tiếp từ `.agents/`, không hardcode danh sách thủ công.

---

## 🗺️ Dynamic AG-Kit Registry (`.agents/`)
- **Quy tắc dự án (P0):** Bắt buộc đọc và tuân thủ [`.agents/rules/thienan_rules.md`](.agents/rules/thienan_rules.md).
- **Specialist Agents:** Khám phá vai trò từ [`.agents/agent/`](.agents/agent/).
- **Modular Skills:** Nạp `SKILL.md` từ [`.agents/skills/`](.agents/skills/). Thông báo `📚 Using skill: @[skill-name]...` trước khi áp dụng.
- **Workflows:** Tra cứu tại [`.agents/workflows/`](.agents/workflows/).
- **Memory (SSOT):** Đọc [`.agents/memory/MEMORY.md`](.agents/memory/MEMORY.md).
- **System Architecture Map:** Xem tại [`.agents/ARCHITECTURE.md`](.agents/ARCHITECTURE.md).

---

## 🛠️ Build & Test Commands
- Backend WebAPI: `dotnet build TA-ITS015-WEBAPI-V1.0/src/TAC_WebAPI/TAC_WebAPI.csproj`
- Backend ShareData: `dotnet build TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker/ShareDataWorker.csproj`
- Run Tests: `dotnet test tests/BE/test.csproj`
- Frontend: `cd TA-ITS015-WEBVUE-V1.0 && npm run build`

---

## 🛑 Critical Mandatory Safeguards
> 🔴 **SINGLE SOURCE OF TRUTH (SSOT): [`.agents/rules/thienan_rules.md`](.agents/rules/thienan_rules.md)**
> Toàn bộ quy tắc cốt lõi (P0 Safeguards) và bảng chỉ mục tra cứu theo chủ đề định nghĩa tập trung tại [`.agents/rules/thienan_rules.md`](.agents/rules/thienan_rules.md).

<!-- gortex:communities:start -->
<!-- gortex:communities:end -->
