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
- Run Tests: `dotnet test tests/BE/test.csproj`
- Frontend: `cd TA-ITS015-WEBVUE-V1.0 && npm run build`

---

## 🛑 Critical Mandatory Safeguards
1. **Strict Local Database for `dotnet test`**: Mọi connection string test PHẢI trỏ về `local` (`localhost`, `127.0.0.1`, `(localdb)`, `.`). Nếu phát hiện IP remote (ví dụ `10.10.8.30`), **HỦY test ngay lập tức và báo cáo cho user**.
2. **CẤM tự động xóa file Prompt sau khi hoàn thành (No Auto-Delete Prompt Files)**: Sau khi xong task, AI **TUYỆT ĐỐI KHÔNG tự động xóa** file prompt (`*-prompt*.md`, `{task-slug}.md`). Bắt buộc giữ lại file prompt để người dùng review và đối chiếu sau khi code change. **✅ Được phép xóa khi và chỉ khi người dùng trực tiếp yêu cầu** (ví dụ: *"xóa prompt X đi"*, *"prompt nào xong xóa đi"*) — lúc đó AI thực hiện xóa ngay, không cần hỏi lại.
3. **Strict Manual SQL Execution**: Chỉ xuất file `.sql` ra đĩa để user review. KHÔNG tự ý thực thi DDL/DML trực tiếp làm thay đổi database.
4. **Ưu tiên tuyệt đối Gortex MCP + DAB MCP staging — Dual Source of Truth (Gortex-First & DAB-MCP-First)**: Mọi thao tác tìm kiếm code / đọc file C#/Vue/TS **BẮT BUỘC ƯU TIÊN Gortex MCP** (`call_mcp_tool` với `ServerName: "gortex"`); mọi kiểm tra schema/cột/bảng/dữ liệu **BẮT BUỘC đọc trực tiếp DB staging `mssql_staging` (`10.10.8.30/DEV_ITS10`) qua DAB MCP** (`mssql_staging__*` với `autoentities: dbo-readonly`) hoặc `sqlcmd -C -S 10.10.8.30` read-only. Chỉ fallback về built-in tools khi Gortex/DAB MCP báo lỗi/timeout/`Connection closed` hoặc khi thao tác trên Markdown/config.
5. **CẤM tự ý thêm `using` dư thừa / trùng lặp (No Unnecessary Usings - IDE0005)**: Luôn kiểm tra `GlobalUsings.cs` trước khi thêm `using`; cấm thêm các using đã có global; luôn rà soát và xóa using không dùng trước khi kết thúc task.
6. **CẤM dùng cờ `--no-build` khi chạy `dotnet test` (No `--no-build` in Testing - P0)**: Khi chạy kiểm thử `dotnet test`, TUYỆT ĐỐI KHÔNG sử dụng tham số `--no-build`. Việc bỏ qua bước build dẫn đến nguy cơ test chạy trên binary cũ (stale cache), không nạp các thay đổi code mới nhất trên đĩa, gây ra kết quả kiểm thử ảo (sai lệch pass/fail). Luôn chạy `dotnet test tests/BE/test.csproj --filter "..."` để dotnet tự biên dịch incremental bảo đảm an toàn và chính xác 100%.
7. **CẤM tự ý can thiệp Staging Area: CẤM tự ý `git add` lẫn `git reset` / unstage (Strict No Auto-Stage / No Auto-Unstage - P0)**: AI **TUYỆT ĐỐI KHÔNG chạy lệnh `git add`** hoặc đưa bất kỳ file nào vào Staged Changes khi người dùng không yêu cầu trực tiếp, và **TUYỆT ĐỐI KHÔNG chạy `git reset` / `git restore --staged`** hoặc tự ý unstage các file mà người dùng đã chủ động đưa vào Staged Changes. Mọi lần sửa code **BẮT BUỘC để nguyên ở trạng thái Changes (Working Tree / Unstaged)** để người dùng tự review qua giao diện IDE trước khi stage.
8. **CẤM tự động chạy `npm run build` khi sửa code Frontend (Client đang chạy có Hot-reload - Strict No Auto-Build on Frontend - P0)**: Khi làm việc trên Frontend (`TA-ITS015-WEBVUE-V1.0`), môi trường client dev server đã được khởi chạy với cơ chế Hot-Reload (HMR). Sau khi sửa code (`.vue`, `.ts`, `.js`, `.scss`...), AI **TUYỆT ĐỐI KHÔNG tự động chạy lệnh `npm run build` / `pnpm run build`** làm tốn thời gian và gián đoạn công việc. Chỉ chạy khi người dùng trực tiếp yêu cầu.
9. **CẤM tự ý sửa/bỏ nội dung UI khi fix UI/Responsive — BẮT BUỘC chỉ sửa bằng CSS (Strict CSS-Only for UI/Responsive Fixes - P0)**: Khi tối ưu hiển thị, sửa lỗi giao diện, tràn viền hoặc responsive, AI **TUYỆT ĐỐI KHÔNG tự ý xóa bỏ, rút gọn hoặc thay đổi nội dung, thuộc tính nghiệp vụ, component props của template HTML/Vue** (như `show-word-limit`, `:maxlength`, nhãn, placeholder, thẻ, icon...) nếu người dùng không yêu cầu trực tiếp. Mọi xử lý layout và co giãn **BẮT BUỘC PHẢI GIẢI QUYẾT HOÀN TOÀN BẰNG CSS/SCSS**. Nếu gặp trường hợp khó, không gian quá chật hẹp không thể xử lý thuần CSS, **BẮT BUỘC DỪNG LẠI VÀ HỎI NGƯỜI DÙNG** xem có được phép lược bỏ nội dung hay không trước khi thực hiện.
10. **Tách biệt Logic và UI — CẤM đổi component / layout UI khi fix logic hành vi (Strict Separation of Logic & UI - No UI Modification on Logic Fixes - P0)**: Khi người dùng yêu cầu sửa lỗi logic, xử lý dữ liệu, validation, chặn nhập (clamp, max, min, filter, format...) hoặc hành vi nghiệp vụ, AI **BẮT BUỘC tập trung 100% vào logic và hành vi bên dưới**. TUYỆT ĐỐI KHÔNG tự ý thay đổi component UI, thay đổi layout, đổi sang component khác (ví dụ: đang dùng `el-input-number` tự ý đổi sang `el-input` thường, bỏ nút controls, đổi cấu trúc HTML...) chỉ để tiện xử lý logic cho bản thân. **Chỉ được phép can thiệp/sửa giao diện (UI/UX) khi và chỉ khi người dùng trực tiếp yêu cầu sửa UI**. Luôn bảo toàn 100% giao diện gốc hiện hữu khi giải quyết bài toán logic.
11. **Quy định dịch thuật Backend & Frontend (Strict Backend No-Resource & Database-Only Translations - P0)**: Phía Backend WebAPI (`TA-ITS015-WEBAPI-V1.0`), AI **TUYỆT ĐỐI KHÔNG sửa hoặc thêm vào các file resource** (`src/TAC_WebAPI/Resources/*.json`). Mọi nhu cầu dịch thuật Backend (exception, validation, message, entity) **BẮT BUỘC lưu trên CSDL (`SysTerminology`)** thông qua 2 cách: (1) Viết script SQL seed idempotent xuất ra thư mục `sql/` hoặc (2) Gọi API quản trị thuật ngữ (`SysTerminologyController`). Phía Frontend (`TA-ITS015-WEBVUE-V1.0`) vẫn sử dụng từ điển `i18n` JSON (`vi-vn.json`, `en-us.json`) bình thường.
12. **BẮT BUỘC ghi tên & đường dẫn file Prompt ngay đầu file .md (Prompt Filename in Header - P0)**: Khi tạo/viết bất kỳ file prompt nào (`*-prompt*.md`), AI **BẮT BUỘC phải ghi rõ đường dẫn tương đối của file prompt ngay tại phần đầu nội dung file markdown** (dưới dạng inline code hoặc code line, ví dụ: `**Tệp prompt:** `DocBusinessThienAn/<Dự-án>/<PhânHệ>/Prompt/<task-slug>-prompt.md``) để người dùng có thể double-click copy một chạm khi giao việc hoặc chuyển ngữ cảnh hội thoại.
13. **Can thiệp tối thiểu / CẤM tự ý bulk regenerate / bulk format làm bẩn Git Diff (Strict Minimal Footprint & No Unrequested Bulk Changes - P0)**: Khi sửa lỗi, thêm endpoint hoặc cập nhật API/DTO/Client (như sinh mã Swagger codegen, sửa models, refactor), AI **CHỈ ĐƯỢC PHÉP can thiệp đúng các file và vị trí cần thiết**. TUYỆT ĐỐI KHÔNG chạy các công cụ sinh mã hàng loạt đè lên toàn bộ module, không auto-format hoặc search-replace hàng loạt làm thay đổi định dạng, thụt dòng, dấu `*`, comment trên hàng chục/hàng trăm file không liên quan. Mọi hành vi regenerate hoặc thay đổi diện rộng **CHỈ ĐƯỢC PHÉP KHI VÀ CHỈ KHI NGƯỜI DÙNG TRỰC TIẾP YÊU CẦU**.
14. **Sửa code sai phải hoàn nguyên sạch về mặc định cũ — CẤM để lại thay đổi dư thừa (Clean Revert & No Unnecessary Diff - P0)**: Khi sửa code bị sai hoặc khi người dùng yêu cầu discard/hoàn nguyên, AI **BẮT BUỘC khôi phục tệp về đúng 100% nguyên trạng mặc định ban đầu**. TUYỆT ĐỐI KHÔNG để sót khoảng trắng vô nghĩa, dòng trống thừa (như khoảng trống sau `{` của class), và KHÔNG để lại using dư thừa vi phạm `IDE0005` (như `using SqlSugar;` khi đã có trong `GlobalUsings.cs`). Git diff của tệp hoàn nguyên phải hoàn toàn sạch sẽ (0 thay đổi).

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
