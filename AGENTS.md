# AI Harness - Single Source of Truth

> 🔴 **SINGLE SOURCE OF TRUTH (SSOT): `.agents/`**
> Tất cả quy tắc kiến trúc, specialist agents, modular skills, workflows và memory tập trung DUY NHẤT tại thư mục **`.agents/`**.
> Bạn PHẢI tra cứu và thực thi trực tiếp từ `.agents/`, KHÔNG hardcode danh sách thủ công.

---

## 🗺️ Dynamic AG-Kit Registry (`.agents/`)
- **Quy tắc dự án (P0):** Bắt buộc đọc và tuân thủ tuyệt đối:
  - [`.agents/rules/thienan_rules.md`](file:///.agents/rules/thienan_rules.md) (Git branch/commit, coding conventions, architecture, cấm cross-module comments).
  - [`.agents/rules/universal-rules.md`](file:///.agents/rules/universal-rules.md) (Clean Code, logging, single-statement if).
  - [`.agents/rules/core-protocol.md`](file:///.agents/rules/core-protocol.md) (Announcement protocol).
  - [`.agents/rules/code-rules.md`](file:///.agents/rules/code-rules.md) (4-phase planning, Socratic Gate).
- **Specialist Agents:** Tự động khám phá và nhận diện vai trò từ thư mục [`.agents/agent/`](file:///.agents/agent/).
- **Modular Skills:** Tự động khám phá và nạp file `SKILL.md` từ [`.agents/skills/`](file:///.agents/skills/) (bao gồm cả các skill `gortex-*` do Gortex sinh ra). Luôn thông báo `📚 Using skill: @[skill-name]...` trước khi áp dụng.
- **Workflows & Slash Commands:** Tra cứu quy trình tương ứng tại [`.agents/workflows/{command}.md`](file:///.agents/workflows/).
- **Memory (SSOT):** Luôn đọc [`.agents/memory/MEMORY.md`](file:///.agents/memory/MEMORY.md). Mọi file memory session chỉ ghi vào `.agents/memory/`, KHÔNG ghi ra bên ngoài.
- **System Architecture Map:** Xem bảng tổng hợp tại [`.agents/ARCHITECTURE.md`](file:///.agents/ARCHITECTURE.md).

---

## 🛠️ Build & Test Commands
- Backend WebAPI: `dotnet build TA-ITS015-WEBAPI-V1.0/src/TAC_WebAPI/TAC_WebAPI.csproj`
- Backend ShareData: `dotnet build TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker/ShareDataWorker.csproj`
- Run Tests: `dotnet test tests/BE/test.csproj`
- Frontend: `cd TA-ITS015-WEBVUE-V1.0 && npm run build`

---

## 🛑 Critical Mandatory Safeguards
> 🔴 **SINGLE SOURCE OF TRUTH (SSOT): [`.agents/rules/thienan_rules.md`](file:///.agents/rules/thienan_rules.md)**
> Toàn bộ các quy tắc bắt buộc cốt lõi (P0 Safeguards) của dự án được định nghĩa tập trung và duy nhất tại:
> 👉 **[`.agents/rules/thienan_rules.md`](file:///.agents/rules/thienan_rules.md)** (Mục 19: Các Quy Tắc Cốt Lõi Bắt Buộc - P0 Safeguards).
>
> Mọi AI Agent **BẮT BUỘC** tra cứu, đọc và tuân thủ tuyệt đối các quy tắc từ tệp SSOT trên trước khi thực hiện bất kỳ thao tác nào (bao gồm: kết nối DB local, quy tắc test không dùng `--no-build`, không dùng `try-finally` trong test, không tự xóa DB trong từng test method, không tự ý `git add`/`reset`, không tự động build frontend, cấm tự gán ID SnowFlake, CSS-only cho UI fixes...).
> Tuyệt đối không duplicate danh sách quy tắc tại đây để đảm bảo nguyên tắc sửa 1 nơi duy nhất.

---

<!-- gortex:communities:start -->
## Community Skills

| Area | Description | Explore |
|------|-------------|---------|
| Tms Apis 20 Dirs | 7021 symbols | `analyze(operation:"communities", id:"community-2337")` |
| Tms Models 100 Dirs | 3401 symbols | `analyze(operation:"communities", id:"community-1742")` |
| Modules Tms Core Entities 121 Dirs | 2230 symbols | `analyze(operation:"communities", id:"community-301")` |
| Controllers Maintenanceschedule 203 Dirs | 1441 symbols | `analyze(operation:"communities", id:"community-13")` |
| Modules Vmschainzoneapi | 1011 symbols | `analyze(operation:"communities", id:"community-64")` |
| Videowall Controllers 63 Dirs | 925 symbols | `analyze(operation:"communities", id:"community-2407")` |
| Configuration Options 37 Dirs | 831 symbols | `analyze(operation:"communities", id:"community-651")` |
| Maintenanceschedule Dto 50 Dirs | 765 symbols | `analyze(operation:"communities", id:"community-323")` |
| Src Utils 35 Dirs | 753 symbols | `analyze(operation:"communities", id:"community-1366")` |
| Module Sharedata Core Entities 20 Dirs | 695 symbols | `analyze(operation:"communities", id:"community-2401")` |
| Modules Chainzones Vmschainzonecontroller | 684 symbols | `analyze(operation:"communities", id:"community-59")` |
| Tms Apis 12 Dirs | 651 symbols | `analyze(operation:"communities", id:"community-2342")` |
| Src Api Services Statusenum | 584 symbols | `analyze(operation:"communities", id:"community-868")` |
| Equipment Dto 87 Dirs | 506 symbols | `analyze(operation:"communities", id:"community-300")` |
| 15 Dirs | 506 symbols | `analyze(operation:"communities", id:"community-2386")` |
| Infrastructure Adapter Chainzoneapiadapter Chainzoneapiadapter | 484 symbols | `analyze(operation:"communities", id:"community-158")` |
| Videowall Models 6 Dirs | 483 symbols | `analyze(operation:"communities", id:"community-1365")` |
| Services Device 17 Dirs | 470 symbols | `analyze(operation:"communities", id:"community-25")` |
| Dto Process 29 Dirs | 469 symbols | `analyze(operation:"communities", id:"community-502")` |
| Tms Models 8 Dirs | 451 symbols | `analyze(operation:"communities", id:"community-2053")` |

<!-- gortex:communities:end -->
