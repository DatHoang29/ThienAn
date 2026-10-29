---
name: thienan-rules
version: 1.1.0
priority: P0
trigger: always_on
description: Single Source of Truth for ThienAn project — P0 safeguards and domain rules index.
---

# 📌 Quy Định Chung Phòng Phần Mềm: Thiên Ân (Single Source of Truth)

> 🔴 **NGUỒN SỰ THẬT DUY NHẤT (SSOT): `.agents/rules/`**  
> Toàn bộ quy tắc kiến trúc, quy chuẩn code, quy trình Git và an toàn AI chuẩn hóa tập trung tại đây.
> Quy tắc cốt lõi (P0) áp dụng cho mọi phiên. Quy chuẩn chuyên sâu tra cứu qua [Bảng Chỉ Mục](#-bảng-chỉ-mục-quy-tắc-theo-chủ-đề-domain-rules-index).

---

## 🛑 MỤC 19: BẢNG QUY TẮC CỐT LÕI BẮT BUỘC (P0 SAFEGUARDS & QUY TẮC CHUNG)

Mọi AI Agent **BẮT BUỘC** tuân thủ tuyệt đối các quy tắc cốt lõi sau trong mọi phiên:

| STT | Quy Tắc Cốt Lõi | Yêu Cầu Chi Tiết (Viết Gọn) |
| :---: | :--- | :--- |
| **1** | **Gortex-First Protocol** (Rule 19.62) | Bắt buộc dùng Gortex MCP khi tìm, đọc, sửa mã (C#, Vue, TS, SQL). Chỉ dùng shell/read khi sửa MD/config hoặc Gortex lỗi. Chi tiết: [database-mcp_thienan.md](./database-mcp_thienan.md) §1.1. |
| **2** | **Bộ Test Hợp Nhất** (Rule 19.63) | Test FE viết bằng Playwright tại `tests/FE/` (`pnpm test:e2e`); Test BE bằng xUnit tại `tests/BE/` (`dotnet test tests/BE/test.csproj`). ⛔ CẤM script test ad-hoc. Chi tiết: [testing-rules_thienan.md](./testing-rules_thienan.md) §1. |
| **3** | **Cấm Tự Ý Git Stage / Commit / Discard** (Rule 19.58) | ⛔ CẤM tự ý `git add`, `git reset`, `git commit`, `git push`, `git checkout --`, `git restore`, `git clean` hoặc tự ý discard bất kỳ thay đổi nào trên Working Tree của dev. Mọi thay đổi giữ nguyên ở Working Tree. Chi tiết: [git-workflow_thienan.md](./git-workflow_thienan.md) §4. |
| **4** | **Cấm Auto-Build Frontend** (Rule 19.13) | FE đã có Vite dev server HMR tự nạp thay đổi. ⛔ CẤM chạy `npm run build` / `pnpm build` sau khi sửa FE. Chi tiết: [frontend-vue_thienan.md](./frontend-vue_thienan.md) §7. |
| **5** | **CSDL Test Local Bắt Buộc** | Test BE chạy trên SQL Server local (`localhost`/`127.0.0.1` qua `Host.GuardAllConnectionsLocal`). ⛔ CẤM trỏ DB ngoài, cấm cờ `--no-build`. Chi tiết: [testing-rules_thienan.md](./testing-rules_thienan.md) §2. |
| **6** | **Cấm Thư Viện Mock Ngoài** (Rule 19.18) | ⛔ CẤM Moq, NSubstitute, fake mock nội bộ cho service/NATS. Dùng service thật từ `host.Services` trên DB local. Chi tiết: [testing-rules_thienan.md](./testing-rules_thienan.md) §2. |
| **7** | **Strict AAA — Cấm Try-Catch Trong Test** (Rule 19.43) | ⛔ CẤM `try-catch`/`try-finally` quanh Act và Assert trong test method; luồng test phẳng theo AAA để fail tự nhiên. Chi tiết: [testing-rules_thienan.md](./testing-rules_thienan.md) §2. |
| **8** | **Không Tự Xóa DB Trong Test Method** (Rule 19.45) | ⛔ CẤM viết lệnh xóa bảng DB trong test method. Dọn tập trung tại `Host.ClearAllData()` và cô lập bằng Unique ID. Chi tiết: [testing-rules_thienan.md](./testing-rules_thienan.md) §2. |
| **12** | **Quy Tắc Sinh ID** (Rule 19.44) | `ID` là GUID. WebAPI do AOP tự sinh khi Insert (⛔ CẤM gán tay `entity.ID`); Worker gán `Guid.NewGuid().ToString()`; SQL dùng `LOWER(CONVERT(NVARCHAR(64), NEWID()))`. Chi tiết: [backend-dotnet_thienan.md](./backend-dotnet_thienan.md) §3. |
| **13** | **Can Thiệp Tối Thiểu & Cấm Sửa Ngoài Phạm Vi** (Rule 19.6, Rule 19.35) | Chỉ sửa dòng tối thiểu cần thiết; giữ 100% logic, format xung quanh. ⛔ CẤM sửa file/hàm không liên quan trực tiếp; cấm tự ý refactor/format lại file. |
| **14** | **Duy Nhất 1 MasterPlan Nghiệp Vụ & Giữ Prompt** (Rule 19.52, Rule 19.34) | Mỗi phân hệ duy nhất 1 MasterPlan sống (`<PhânHệ>_MasterPlan.md`) báo cáo luồng nghiệp vụ. ⛔ CẤM xóa file prompt sau khi xong. Chi tiết: [documentation-standards_thienan.md](./documentation-standards_thienan.md) §1. |
| **15** | **Tự Động Dọn Dẹp Test Artifacts** (Rule 19.64) | Sau khi test pass, AI tự động xóa file/thư mục tạm do test runner sinh ra (`test-results/`, traces, screenshots) để giữ sạch Working Tree. Chi tiết: [testing-rules_thienan.md](./testing-rules_thienan.md) §4. |
| **16** | **Prompt-First Khi Cần Sửa & Ngoại Lệ File `.sql`** | Phát hiện chỗ cần sửa: ⛔ KHÔNG tự sửa mà viết prompt tại `DocBusinessThienAn/<DựÁn>/<PhânHệ>/Prompt/`. Ngoại lệ: được sửa trực tiếp file `.sql` (không tự chạy DB). Chi tiết: [database-mcp_thienan.md](./database-mcp_thienan.md) §2.2. |
| **17** | **Repo Cha `ThienAn` Chỉ Dùng `main`** | Repo cha `ThienAn`: mọi fix làm thẳng trên `main` rồi push `origin main`; ⛔ CẤM tạo nhánh `fix/*`, `feature/*`, `agents/*` hoặc worktree. Chi tiết: [git-workflow_thienan.md](./git-workflow_thienan.md) §4.3. |
| **18** | **Ưu Tiên Sửa Code Trước — Chỉ Viết Test Khi Có Yêu Cầu** (Rule 19.67) | Áp dụng cho cả FE & BE: Sửa dứt điểm code trước để review dev; ⛔ CẤM tự ý viết thêm test mới cho mỗi lần sửa nhỏ làm chậm tiến độ. Chỉ viết test khi người dùng yêu cầu. Chi tiết: [testing-rules_thienan.md](./testing-rules_thienan.md) §1, §2.7, §3. |
| **21** | **Cấm Hardcode Text Mẫu — 100% Đưa Vào i18n** (Rule 20.15) | ⛔ CẤM text cứng, tooltip, placeholder, `ElMessage` trong code FE. 100% qua `$t(...)` và đồng bộ cả `vi-vn.json` và `en-us.json`. Chi tiết: [frontend-vue_thienan.md](./frontend-vue_thienan.md) §5. |
| **24** | **Tự Động Phân Luồng Subagents & Xử Lý Tuần Tự Task Lệ Thuộc** (Rule 19.70) | Prompt có từ 2 task hoặc nhiều message liên tiếp: tự động kích hoạt subagents. Task độc lập chạy song song; task lệ thuộc BẮT BUỘC chạy tuần tự (Sequential Chain). |
| **30** | **Chỉ Báo Cáo Thay Đổi Thực Tế** (Rule 19.74) | Báo cáo/portal chỉ ghi kết quả thực tế đã làm hoặc thành phần đã tồn tại bị xóa. ⛔ CẤM ghi xóa thành phần chưa từng tồn tại. Chi tiết: [documentation-standards_thienan.md](./documentation-standards_thienan.md) §9. |
| **34** | **Cấm Tự Ý Sửa Dữ Liệu, Chỉ Số Ngoài Yêu Cầu** (Rule 19.76) | ⛔ CẤM tự ý sửa, ghi đè hoặc revert dữ liệu, chỉ số, ngày công (man-days), điểm số, cấu hình ngoài phạm vi yêu cầu. Giữ nguyên 100% dữ liệu đã nhập. |
| **38** | **Cấm Tham Chiếu Prompt & Ghi Changelog Đổi Tên Trong Mã** (Rule 19.80) | ⛔ CẤM ghi tên file prompt, định danh AI, hoặc nhật ký đổi tên/sửa code (vd `Modified date: ... (Đổi tên...)`) vào comment, docstring, commit. `Modified date` chỉ ghi ngày tháng; lịch sử do Git quản lý. Chi tiết: [backend-dotnet_thienan.md](./backend-dotnet_thienan.md) §4.16. |
| **48** | **Giữ Nguyên Dòng Cũ Khi Nội Dung Không Đổi** (Rule 19.89) | Minimal Diff: giữ nguyên 100% dòng cũ khi logic không đổi. Format/ngắt dòng chỉ áp dụng cho dòng code mới viết hoặc sửa đổi trực tiếp. Chi tiết: [backend-dotnet_thienan.md](./backend-dotnet_thienan.md) §4.21. |
| **58** | **Cấm Commit Tệp Cấu Hình Local & Bí Mật** (Rule 19.100) | ⛔ CẤM stage, commit, push file cấu hình local và bí mật: `.env.development`, `.env.local`, `Database.json`, `TAKey*.key`, `Shared.Core.Security.dll`. Chi tiết: [git-workflow_thienan.md](./git-workflow_thienan.md) §4.6. |
| **61** | **Tách Locale Phân Hệ & Thoát '@' Trong i18n** (Rule 20.29) | Tách locale riêng `its/<module>/`; tự động nạp qua `import.meta.glob` (cấm import thủ công từng file); ⛔ CẤM `@` trần đi liền tên biến (bắt buộc `{'@'}`) chống lỗi vue-i18n Invalid linked format làm trắng màn hình; seed SQL cập nhật có điều kiện `WHERE Value = N'<cũ>'`. Chi tiết: [frontend-vue_thienan.md](./frontend-vue_thienan.md) §5. |
| **62** | **Thẻ Không Có Nội Dung Viết Tự Đóng** (Rule 20.30) | Thẻ HTML/component trong `<template>` `.vue` không có nội dung (không chữ, không thẻ con, không slot) viết `<div ... />`; ⛔ CẤM cặp rỗng `<div ...></div>`. Chỉ áp cho thẻ mới viết hoặc thẻ có sửa thuộc tính. Chi tiết: [frontend-vue_thienan.md](./frontend-vue_thienan.md) §6.2. |
| **63** | **Commit Message Chỉ Nêu Việc Chính** (Rule 19.102) | Gạch đầu dòng trong commit message chỉ nêu việc chính đã làm, ngắn gọn. ⛔ CẤM ghi viết/chạy test, kết quả kiểm thử, cập nhật tài liệu đi kèm, dọn mã. Chi tiết: [git-workflow_thienan.md](./git-workflow_thienan.md) §3. |
| **64** | **Mỗi File `.sql` Chỉ Ghi Một Bảng** (Rule 19.103) | Một file `.sql` chỉ thêm / sửa / xóa trên đúng một bảng (bảng khác chỉ đọc); tên file mang hậu tố tên bảng `<phan-he>-<vai-tro>-<TênBảng>.sql`, giả lập `..._local.sql`. Đầu file ghi `BẢNG GHI`, `THỨ TỰ CHẠY`; kiểm tra bảng dùng `SET NOEXEC ON`. Chi tiết: [database-mcp_thienan.md](./database-mcp_thienan.md) §2.2. |
| **65** | **Định Dạng Trả Về Nội Dung Commit (Copy-Paste Ready)** (Rule 19.104) | Khi được yêu cầu nội dung commit (`commit -m`): AI xuất khối văn bản thuần (`text`) riêng từng repo (BE/FE) gồm 2 dòng Summary lặp lại và 2-3 gạch đầu dòng chỉ nêu tính năng/việc chính đã làm (⛔ CẤM thêm mã rule hay việc phụ trợ). Chi tiết: [git-workflow_thienan.md](./git-workflow_thienan.md) §3. |
| **66** | **Thông Báo Không Lộ Thông Tin Nhạy Cảm** (Rule 19.105) | ⛔ CẤM thông báo cho người dùng chứa tên bảng cấu hình / cột cờ / bảng CSDL / đường dẫn / chuỗi kết nối nội bộ (vd `ShareDataSourceTable`, `IsQueryable`). Chỉ nêu việc người dùng cần làm (vd liên hệ quản trị viên); chi tiết kỹ thuật ghi log. Chi tiết: [backend-dotnet_thienan.md](./backend-dotnet_thienan.md) §4.26. |
| **67** | **Cấm Tự Ý Sửa Tệp Resource Backend** (Rule 19.107) | ⛔ CẤM tự ý thêm, sửa, xóa, format hoặc sort lại các tệp chuỗi resource của Backend (`src/TAC_WebAPI/Resources/*.json`, `vi-VN.json`, `en-US.json`). Giữ nguyên 100% nội dung resource hiện có. Chi tiết: [backend-dotnet_thienan.md](./backend-dotnet_thienan.md) §4.27. |
| **68** | **Cấm Hardcode Đường Dẫn Tuyệt Đối Của Máy Cá Nhân** (Rule 19.108) | ⛔ CẤM hardcode đường dẫn tuyệt đối (absolute path) gắn với profile/thư mục máy cá nhân (vd `/Users/...`, `C:\Users\...`). 100% dùng đường dẫn tương đối (relative path) hoặc biến môi trường để đảm bảo chuyển qua máy khác hoặc CI/CD chạy được bình thường. Chi tiết: [backend-dotnet_thienan.md](./backend-dotnet_thienan.md) §4.28. |
| **69** | **Bắt Buộc Viết Test Cho Thành Phần / Cơ Chế Bổ Sung Mới** (Rule 19.109) | Bổ sung tính năng, cơ chế hoặc xử lý mới vào hệ thống (như ghi nhật ký hoạt động / audit log, logging đối tượng nghiệp vụ, tra cứu gói tin/đối tác...): BẮT BUỘC viết test kiểm tra tính toàn vẹn (End-to-End hoặc Integration test qua API) xác nhận dữ liệu mới ghi đúng vào CSDL, constructor/DI/Wolverine thông suốt và luồng chạy không bị lỗi. Chi tiết: [testing-rules_thienan.md](./testing-rules_thienan.md) §1.6 & [backend-dotnet_thienan.md](./backend-dotnet_thienan.md) §4.29. |
| **70** | **Dấu Gạch Dưới `_` Trong Tên Nhánh Chỉ Dùng Khi Có TaskCode/Issue** (Rule 19.110) | Dấu `_` **CHỈ ĐƯỢC PHÉP DÙNG** khi nhánh có mã TaskCode/Issue (`...-[TaskCode]_[mô-tả]`). Nhánh thông thường không có TaskCode/Issue: 100% dùng dấu gạch ngang `-` kèm tiền tố module (`...-[module]-[mô-tả]`), ⛔ CẤM dùng dấu `_`. Chi tiết: [git-workflow_thienan.md](./git-workflow_thienan.md) §2. |

---

## 📑 BẢNG CHỈ MỤC QUY TẮC THEO CHỦ ĐỀ (DOMAIN RULES INDEX)

Quy tắc kỹ thuật chi tiết lưu giữ toàn văn tại các tệp chuyên biệt. Tra cứu theo số hiệu rule:

### 1. Frontend Vue 3 & TypeScript ([frontend-vue_thienan.md](./frontend-vue_thienan.md))
- **Rule 19.67** (STT 18) — Ưu tiên sửa code trước, chỉ viết test khi có yêu cầu (Fix-First & On-Demand) (§9)
- **Rule 20.6** (STT 9) — Cấm `!important` trong CSS/SCSS (§2.2)
- **Rule 20.7, Rule 19.27** (STT 10) — CSS-First & cấm xóa props template (§3)
- **Rule 20.8** (STT 11) — Cấm toán tử phủ định kép `!!` (§6.1)
- **Rule 20.13** (STT 19) — Cấm nhãn song ngữ lai, 100% qua i18n (§5)
- **Rule 20.14** (STT 20) — Tên tệp component viết theo `camelCase` (§1)
- **Rule 20.16, Rule 20.18, Rule 20.19** (STT 26) — Code, style, i18n mới move dòng cuối; edit giữ cùng hàng (§6.5)
- **Rule 20.17** (STT 27) — Khai báo `defineProps` inline, cấm tạo `types.ts` lẻ (§6.4)
- **Rule 20.20** (STT 31) — Clean TypeScript State, cấm ép kiểu rườm rà (§6.6)
- **Rule 20.21** (STT 32) — Tương thích kiểu TS, khai báo type sự kiện `(e)` chuẩn (cấm `:any`) & Zero lỗi đỏ IDE (`ts-plugin(7006)`) (§6.7)
- **Rule 20.22** (STT 41) — Đóng gói CSS phân hệ nội bộ, CẤM TUYỆT ĐỐI viết/sửa CSS global (`src/theme/`) trừ khi người dùng yêu cầu trực tiếp (§2.5)
- **Rule 20.25** (STT 54) — Quy chuẩn VxeTable (`useVxeTable`), dữ liệu cấu hình & i18n (§10)
- **Rule 20.26** (STT 55) — Bấm ra ngoài thì đóng dialog; form có sửa chưa lưu phải hỏi trước (§4)
- **Rule 20.27** (STT 59) — Định dạng ngày tháng bộ lọc FE (`el-date-picker` bắt buộc `format="DD/MM/YYYY"`) (§10.4)
- **Rule 20.28** (STT 60) — Gọi API 100% qua `getAPI(XxxApi)`, CẤM tạo service tùy tiện & CẤM gọi raw `axiosInstance` (`${serveConfig.basePath}`) (§1.1)
- **Rule 20.29** (STT 61) — Tách locale riêng phân hệ (`its/<module>/`), bọc escape `{'@'}` chống lỗi vue-i18n & đồng bộ seed SQL idempotent an toàn (§5)
- **Rule 20.30** (STT 62) — Thẻ không có nội dung viết tự đóng `<div ... />`, cấm cặp rỗng `<div ...></div>` (§6.2)
- **Rule 20.31** — Quy chuẩn Smoke Test nạp trang & Chống trắng màn hình (kiểm tra nạp trang, click vào dòng xem chi tiết, và bắt pageerror chống lỗi trắng màn hình) (§5.3)

### 2. Backend .NET & Clean Architecture ([backend-dotnet_thienan.md](./backend-dotnet_thienan.md))
> ⚠️ **3 luật CQRS cốt lõi (P0, áp cho mọi mã BE mới; mã lân cận sai thì không chép theo):**
> - Handler `Controllers/<ChứcNăng>/{Commands,Queries}` cài `IWolverineHandler` (`using Wolverine;`); ⛔ CẤM `ITransient` / `IScoped` / `ISingleton` (§2).
> - ⛔ CẤM DataAnnotation (`[Required]`, `[Range]`, `[StringLength]`) trên DTO; kiểm tra 100% bằng FluentValidation trong `Controllers/<ChứcNăng>/Validators/` (§2).
> - **Rule 19.44** — WebAPI: ⛔ CẤM gán tay `ID = Guid.NewGuid()` khi Insert, AOP tự sinh (§3).
> - **Rule 19.65** — Thêm `using <Namespace>;` đầu tệp; ⛔ CẤM viết tên kiểu đầy đủ kèm namespace trong mã (vd `Module.ShareData.Core.Helpers.SqlVariableSet`) (§4.8).
- **Rule 19.67** (STT 18) — Ưu tiên sửa code trước, chỉ viết test khi có yêu cầu (Fix-First & On-Demand) (§4.25)
- **Rule 19.68** (STT 22) — Cấm mã obsolete trong C#, dùng `[SugarColumn(IsIgnore=true)]` (§4.10)
- **Rule 19.69, Rule 19.26** (STT 23) — Cấm hardcode string trong `Oops.Oh`, dùng `BaseMsg` & `SysTerminology` (§4.11)
- **Rule 19.71** (STT 25) — Cấm thêm project & solution folder dư thừa vào `.sln` (§4.12)
- **Rule 19.73, Rule 19.94** (STT 29) — Tránh lặp tiền tố phân hệ, lớp vai trò cấm hậu tố `Service` (§4.13, §4.24)
- **Rule 19.75** (STT 33) — Quy chuẩn ngắt dòng khi gọi hàm/phương thức nhiều tham số (§4.14)
- **Rule 19.78** (STT 36) — Cấm bí danh thừa & hàm bọc mỏng chỉ chuyển tiếp (§4.15)
- **Rule 19.80** (STT 38) — Cấm tham chiếu prompt & cấm ghi changelog/đổi tên trong comment (§4.16)
- **Rule 19.47, Rule 19.83** (STT 42) — Khai báo field đầu lớp; cấm tạo hàm `Reset*` chỉ phục vụ test (§4.17)
- **Rule 19.86** (STT 45) — Ưu tiên Dependency Injection qua hàm khởi tạo (§4.18)
- **Rule 19.87** (STT 46) — Đặt tên bằng từ thông dụng; thuật ngữ chuyên biệt chỉ dùng khi bắt buộc (§4.19)
- **Rule 19.88** (STT 47) — `BackgroundService` .NET 10: Không thêm `await Task.Yield()` đầu ExecuteAsync (§4.20)
- **Rule 19.90** (STT 49) — Chỗ đặt enum: Giữ enum cũ ở `BaseEnums`; phân hệ mới bọc ở Core (§4.22)
- **Rule 19.94** (STT 53) — Ưu tiên thư viện có sẵn (Furion), hạn chế interface 1:1, phân cấp Base/Helper (§4.24)
- **Rule 19.105** (STT 66) — Thông báo cho người dùng không lộ tên bảng / cột / cấu hình nội bộ (§4.26)
- **Rule 19.107** (STT 67) — Cấm tự ý sửa tệp resource Backend (`Resources/*.json`) (§4.27)
- **Rule 19.108** (STT 68) — Cấm hardcode đường dẫn tuyệt đối của máy cá nhân (§4.28)
- **Rule 19.109** (STT 69) — Bắt buộc viết test kiểm chứng cho cơ chế & nhật ký hoạt động mới (§4.29)

### 3. Testing Suite Protocol ([testing-rules_thienan.md](./testing-rules_thienan.md))
> ⚠️ **Chỉ viết test mang giá trị & Đi qua nguyên luồng nghiệp vụ (Rule 19.10, bổ sung 24/10/2026, 28/10/2026, §1.4):**
> - Test bắt buộc đi qua toàn bộ luồng nghiệp vụ (End-to-End Business Flow) từ điểm vào chính thức (API qua `IMessageBus`; Worker như `DataOutboundService.ProcessSubscriptions` trích xuất -> ánh xạ -> ghi file thật xuống đĩa); ⛔ CẤM unit test gọi thẳng helper tĩnh (`ExportPathHelper.ResolveRootPath`), mock/fake ngữ cảnh giả lập (`CreateDummyContext`), kiểm hằng, kiểm DI, hoặc chỉ test hàm đơn lẻ không phản ánh luồng nghiệp vụ thật.
> - Cùng luồng khác dữ liệu → một `[Theory]` + `[InlineData]` (FE: mảng dữ liệu + vòng `for`); ⛔ CẤM test FE chỉ đọc mã nguồn rồi so chuỗi.
> - Tự hỏi: "Xóa test này thì lỗi nào lọt qua mà không test nào khác bắt?" — không trả lời được thì không viết.
- **Rule 19.67** (STT 18) — Ưu tiên sửa code trước, chỉ viết test khi có yêu cầu (Fix-First & On-Demand cho cả FE & BE) (§1, §2.7, §3)
- **Rule 19.81** (STT 39) — Kiểm thử NATS: Cách ly subject độc lập từng test & Retry Probe Loop (§2.6)
- **Rule 19.84** (STT 43) — Tệp test bám đúng luồng nghiệp vụ chính, thêm vào tệp có sẵn (§5)
- **Rule 19.92, Rule 51** (STT 51) — Chạy test theo phạm vi sửa để tiết kiệm thời gian (filter test) (§6)
- **Rule 19.109** (STT 69) — Bắt buộc viết test cho thành phần / cơ chế bổ sung mới (§1.6)

### 4. Database & MCP Tooling ([database-mcp_thienan.md](./database-mcp_thienan.md))
- **Rule 19.85** (STT 44) — Bảng có entity: CodeFirst khung trước; nạp data qua UI hoặc `.sql` (§2.3)
- **Rule 19.103** (STT 64) — Mỗi file `.sql` chỉ ghi một bảng, tên file mang hậu tố tên bảng (§2.2)

### 5. Tài Liệu, Plan & Báo Cáo ([documentation-standards_thienan.md](./documentation-standards_thienan.md))
- **Rule 19.53, Rule 19.72** (STT 28) — Zero corruption, bảo toàn 100% công thức/format file Excel (§8)
- **Rule 19.77** (STT 35) — Quy chuẩn báo cáo KPI tháng (Row 20, 5.5 công/tuần, viền FFD5DEE8) (§10)
- **Rule 19.79** (STT 37) — Đồng bộ tuyệt đối URL hyperlink và text hiển thị (§10.6)
- **Rule 19.82** (STT 40) — Kịch bản nghiệm thu: Ưu tiên giao diện (UI-First) làm đường chính (§11)
- **Rule 19.98** (STT 56) — Tách biệt file prompt Backend và Frontend riêng để chạy song song (§1)
- **Rule 19.99** (STT 57) — Bảo toàn nguyên văn gỡ băng kỹ thuật (Audio Verbatim Fidelity) (§5.4)

### 6. Git Workflow & Commit ([git-workflow_thienan.md](./git-workflow_thienan.md))
- **Rule 19.91** (STT 50) — Đồng bộ `dev` trước khi push; commit một phần khi thư mục có mã dở (§4.4)
- **Rule 19.93** (STT 52) — Toàn vẹn phụ thuộc khi commit: Bắt buộc kèm `.sln`, `.csproj` (§4.5)
- **Rule 19.102** (STT 63) — Commit message chỉ nêu việc chính, cấm ghi viết/chạy test, kết quả kiểm thử (§3)
- **Rule 19.104** (STT 65) — Định dạng trả về nội dung commit: xuất khối văn bản thuần (`text`) kèm mã Rule để copy-paste trực tiếp (§3)
- **Rule 19.110** (STT 70) — Quy tắc đặt tên nhánh: Dấu `_` chỉ dùng khi có TaskCode/Issue, nhánh thường 100% dùng dấu `-` kèm tiền tố module (§2)
