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
- Backend WebAPI: `dotnet build src/TAC_WebAPI/TAC_WebAPI.csproj`
- Backend ShareData: `dotnet build src/Services/ShareDataWorker/ShareDataWorker.csproj`
- Run Tests: `dotnet test tests/test.csproj`
- Frontend: `cd TA-ITS015-WEBVUE-V1.0 && npm run build`

---

## 🛑 Critical Mandatory Safeguards
1. **Strict Local Database for `dotnet test`**: Mọi connection string test PHẢI trỏ về `local` (`localhost`, `127.0.0.1`, `(localdb)`, `.`). Nếu phát hiện IP remote (ví dụ `10.10.8.30`), **HỦY test ngay lập tức và báo cáo cho user**.
2. **CẤM tự động xóa file Prompt sau khi hoàn thành (No Auto-Delete Prompt Files)**: Sau khi xong task, AI **TUYỆT ĐỐI KHÔNG tự động xóa** file prompt (`*-prompt*.md`, `{task-slug}.md`). Bắt buộc giữ lại file prompt để người dùng review và đối chiếu sau khi code change. Chỉ xóa khi người dùng kiểm tra xong và trực tiếp yêu cầu xóa.
3. **Strict Manual SQL Execution**: Chỉ xuất file `.sql` ra đĩa để user review. KHÔNG tự ý thực thi DDL/DML trực tiếp làm thay đổi database.

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
