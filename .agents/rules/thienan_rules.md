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
| **3** | **Cấm Tự Ý Git Stage / Commit** (Rule 19.58) | ⛔ CẤM tự ý `git add`, `git reset`, `git commit`, `git push`. Mọi thay đổi giữ ở Working Tree để dev review qua Git Diff. Chi tiết: [git-workflow_thienan.md](./git-workflow_thienan.md) §4. |
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
| **21** | **Cấm Hardcode Text Mẫu — 100% Đưa Vào i18n** (Rule 20.15) | ⛔ CẤM text cứng, tooltip, placeholder, `ElMessage` trong code FE. 100% qua `$t(...)` và đồng bộ cả `vi-vn.json` và `en-us.json`. Chi tiết: [frontend-vue_thienan.md](./frontend-vue_thienan.md) §5. |
| **24** | **Tự Động Phân Luồng Subagents & Xử Lý Tuần Tự Task Lệ Thuộc** (Rule 19.70) | Prompt có từ 2 task hoặc nhiều message liên tiếp: tự động kích hoạt subagents. Task độc lập chạy song song; task lệ thuộc BẮT BUỘC chạy tuần tự (Sequential Chain). |
| **30** | **Chỉ Báo Cáo Thay Đổi Thực Tế** (Rule 19.74) | Báo cáo/portal chỉ ghi kết quả thực tế đã làm hoặc thành phần đã tồn tại bị xóa. ⛔ CẤM ghi xóa thành phần chưa từng tồn tại. Chi tiết: [documentation-standards_thienan.md](./documentation-standards_thienan.md) §9. |
| **34** | **Cấm Tự Ý Sửa Dữ Liệu, Chỉ Số Ngoài Yêu Cầu** (Rule 19.76) | ⛔ CẤM tự ý sửa, ghi đè hoặc revert dữ liệu, chỉ số, ngày công (man-days), điểm số, cấu hình ngoài phạm vi yêu cầu. Giữ nguyên 100% dữ liệu đã nhập. |
| **38** | **Cấm Tham Chiếu Prompt / Phiên AI Trong Mã Nguồn** (Rule 19.80) | ⛔ CẤM ghi tên file prompt, mã prompt, định danh phiên AI vào comment, docstring, tên hàm, commit message, mô tả test. Chi tiết: [backend-dotnet_thienan.md](./backend-dotnet_thienan.md) §4.16. |
| **48** | **Giữ Nguyên Dòng Cũ Khi Nội Dung Không Đổi** (Rule 19.89) | Minimal Diff: giữ nguyên 100% dòng cũ khi logic không đổi. Format/ngắt dòng chỉ áp dụng cho dòng code mới viết hoặc sửa đổi trực tiếp. Chi tiết: [backend-dotnet_thienan.md](./backend-dotnet_thienan.md) §4.21. |
| **58** | **Cấm Commit Tệp Cấu Hình Local & Bí Mật** (Rule 19.100) | ⛔ CẤM stage, commit, push file cấu hình local và bí mật: `.env.development`, `.env.local`, `Database.json`, `TAKey*.key`, `Shared.Core.Security.dll`. Chi tiết: [git-workflow_thienan.md](./git-workflow_thienan.md) §4.6. |

---

## 📑 BẢNG CHỈ MỤC QUY TẮC THEO CHỦ ĐỀ (DOMAIN RULES INDEX)

Quy tắc kỹ thuật chi tiết lưu giữ toàn văn tại các tệp chuyên biệt. Tra cứu theo số hiệu rule:

### 1. Frontend Vue 3 & TypeScript ([frontend-vue_thienan.md](./frontend-vue_thienan.md))
- **Rule 20.6** (STT 9) — Cấm `!important` trong CSS/SCSS (§2.2)
- **Rule 20.7, Rule 19.27** (STT 10) — CSS-First & cấm xóa props template (§3)
- **Rule 20.8** (STT 11) — Cấm toán tử phủ định kép `!!` (§6.1)
- **Rule 20.13** (STT 19) — Cấm nhãn song ngữ lai, 100% qua i18n (§5)
- **Rule 20.14** (STT 20) — Tên tệp component viết theo `camelCase` (§1)
- **Rule 20.16, Rule 20.18, Rule 20.19** (STT 26) — Code, style, i18n mới move dòng cuối; edit giữ cùng hàng (§6.5)
- **Rule 20.17** (STT 27) — Khai báo `defineProps` inline, cấm tạo `types.ts` lẻ (§6.4)
- **Rule 20.20** (STT 31) — Clean TypeScript State, cấm ép kiểu rườm rà (§6.6)
- **Rule 20.21** (STT 32) — Tương thích kiểu TS & Zero lỗi đỏ trong IDE (§6.7)
- **Rule 20.22** (STT 41) — Đóng gói CSS phân hệ nội bộ, cấm viết CSS đặc thù ra global (§2.7)
- **Rule 20.25** (STT 54) — Quy chuẩn VxeTable (`useVxeTable`), dữ liệu cấu hình & i18n (§10)
- **Rule 20.26** (STT 55) — Bấm ra ngoài thì đóng dialog; form có sửa chưa lưu phải hỏi trước (§4)

### 2. Backend .NET & Clean Architecture ([backend-dotnet_thienan.md](./backend-dotnet_thienan.md))
- **Rule 19.68** (STT 22) — Cấm mã obsolete trong C#, dùng `[SugarColumn(IsIgnore=true)]` (§4.10)
- **Rule 19.69, Rule 19.26** (STT 23) — Cấm hardcode string trong `Oops.Oh`, dùng `BaseMsg` & `SysTerminology` (§4.11)
- **Rule 19.71** (STT 25) — Cấm thêm project & solution folder dư thừa vào `.sln` (§4.12)
- **Rule 19.73, Rule 19.94** (STT 29) — Tránh lặp tiền tố phân hệ, lớp vai trò cấm hậu tố `Service` (§4.13, §4.24)
- **Rule 19.75** (STT 33) — Quy chuẩn ngắt dòng khi gọi hàm/phương thức nhiều tham số (§4.14)
- **Rule 19.78** (STT 36) — Cấm bí danh thừa & hàm bọc mỏng chỉ chuyển tiếp (§4.15)
- **Rule 19.47, Rule 19.83** (STT 42) — Khai báo field đầu lớp; cấm tạo hàm `Reset*` chỉ phục vụ test (§4.17)
- **Rule 19.86** (STT 45) — Ưu tiên Dependency Injection qua hàm khởi tạo (§4.18)
- **Rule 19.87** (STT 46) — Đặt tên bằng từ thông dụng; thuật ngữ chuyên biệt chỉ dùng khi bắt buộc (§4.19)
- **Rule 19.88** (STT 47) — `BackgroundService` .NET 10: Không thêm `await Task.Yield()` đầu ExecuteAsync (§4.20)
- **Rule 19.90** (STT 49) — Chỗ đặt enum: Giữ enum cũ ở `BaseEnums`; phân hệ mới bọc ở Core (§4.22)
- **Rule 19.94** (STT 53) — Ưu tiên thư viện có sẵn (Furion), hạn chế interface 1:1, phân cấp Base/Helper (§4.24)

### 3. Testing Suite Protocol ([testing-rules_thienan.md](./testing-rules_thienan.md))
- **Rule 19.67** (STT 18) — Bắt buộc viết test FE Playwright cho mọi fix lặt vặt (§3)
- **Rule 19.81** (STT 39) — Kiểm thử NATS: Cách ly subject độc lập từng test & Retry Probe Loop (§2.6)
- **Rule 19.84** (STT 43) — Tệp test bám đúng luồng nghiệp vụ chính, thêm vào tệp có sẵn (§5)
- **Rule 19.92, Rule 51** (STT 51) — Chạy test theo phạm vi sửa để tiết kiệm thời gian (filter test) (§6)

### 4. Database & MCP Tooling ([database-mcp_thienan.md](./database-mcp_thienan.md))
- **Rule 19.85** (STT 44) — Bảng có entity: CodeFirst khung trước; nạp data qua UI hoặc `.sql` (§2.3)

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
