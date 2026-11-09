---
name: thienan-rules
version: 1.2.0
priority: P0
trigger: always_on
description: Single Source of Truth for ThienAn project — P0 safeguards; domain rules live in the domain files routed by request-routing.md.
---

# 📌 Quy Định Chung Phòng Phần Mềm: Thiên Ân (Single Source of Truth)

> 🔴 **SSOT: `.agents/rules/`.** Quy tắc P0 dưới đây áp dụng cho mọi phiên. Quy chuẩn chi tiết theo miền (FE, BE, test, DB, tài liệu, git) nằm ở tệp miền — tra bảng định tuyến trong [request-routing.md](./request-routing.md).

## 🛑 MỤC 19: QUY TẮC CỐT LÕI BẮT BUỘC (P0)

| STT | Quy tắc | Yêu cầu |
| :---: | :--- | :--- |
| **1** | **Gortex-First** (19.62) | Tìm, đọc, sửa mã (C#, Vue, TS, SQL) bằng Gortex MCP; Read/Grep/shell chỉ cho MD, cấu hình hoặc khi Gortex lỗi. database-mcp §1.1 |
| **2** | **Bộ test hợp nhất** (19.63) | FE: Playwright `tests/FE/` (`pnpm test:e2e`); BE: xUnit `tests/BE/` (`dotnet test tests/BE/test.csproj`). ⛔ Script test ad-hoc. testing §1 |
| **3** | **Cấm tự ý Git stage/commit/discard** (19.58) | ⛔ Tự ý `git add`, `reset`, `commit`, `push`, `checkout --`, `restore`, `clean` hay bỏ thay đổi của dev; mọi thay đổi để ở Working Tree. git §4 |
| **4** | **Cấm auto-build FE** (19.13) | Vite HMR tự nạp; ⛔ `npm run build` / `pnpm build` sau khi sửa FE. frontend §7 |
| **5** | **CSDL test local** | Test BE chạy SQL Server local (`Host.GuardAllConnectionsLocal`); ⛔ trỏ DB ngoài, cờ `--no-build`. testing §2 |
| **6** | **Cấm mock** (19.18) | ⛔ Moq, NSubstitute, fake service/NATS; dùng service thật từ `host.Services` trên DB local. testing §2 |
| **7** | **AAA, cấm try-catch trong test** (19.43) | ⛔ `try-catch` / `try-finally` quanh Act, Assert; test phẳng theo AAA. testing §2 |
| **8** | **Không xóa DB trong test** (19.45) | ⛔ Lệnh xóa bảng trong test method; dọn ở `Host.ClearAllData()`, cô lập bằng Unique ID. testing §2 |
| **12** | **Sinh ID** (19.44) | `ID` là GUID: WebAPI để AOP sinh (⛔ gán tay `entity.ID`); Worker `Guid.NewGuid().ToString()`; SQL `LOWER(CONVERT(NVARCHAR(64), NEWID()))`. backend §3 |
| **13** | **Can thiệp tối thiểu** (19.6, 19.35) | Sửa ít dòng nhất, giữ logic và format xung quanh; ⛔ sửa tệp/hàm không liên quan, tự refactor hay format lại. |
| **14** | **1 MasterPlan, giữ prompt** (19.52, 19.34) | Mỗi phân hệ một MasterPlan sống (`<PhânHệ>_MasterPlan.md`); ⛔ xóa tệp prompt trừ khi người dùng bảo. documentation §1 |
| **15** | **Dọn test artifacts** (19.64) | Test pass thì xóa `test-results/`, trace, screenshot do runner sinh. testing §4 |
| **16** | **Prompt-first, ngoại lệ `.sql`** | Phát hiện chỗ cần sửa: viết prompt ở `DocBusinessThienAn/<DựÁn>/<PhânHệ>/Prompt/`, không tự sửa; được sửa thẳng `.sql` (không tự chạy DB). database-mcp §2.2 |
| **17** | **Repo cha `ThienAn` chỉ `main`** | Sửa thẳng `main`, push `origin main`; ⛔ nhánh `fix/*`, `feature/*`, `agents/*`, worktree. git §4.3 |
| **18** | **Có test trước khi chốt** (19.67) | Vòng sửa nhanh thì sửa code trước; trước khi chốt, chạy full test hay xuất commit phải có test cho mọi code mới sửa trong `tests/FE/`, `tests/BE/` (19.109). testing §1, §2.7, §3 |
| **21** | **Cấm hardcode text** (20.15) | ⛔ Text cứng, tooltip, placeholder, `ElMessage` ở FE; 100% `$t(...)`, đồng bộ `vi-vn.json` và `en-us.json`. frontend §5 |
| **24** | **Subagent, task lệ thuộc** (19.70) | Từ 2 task: task độc lập chạy subagent song song, task lệ thuộc chạy tuần tự. request-routing §2 |
| **30** | **Chỉ báo cáo thay đổi thực tế** (19.74) | Chỉ ghi việc đã làm hoặc thành phần có thật đã xóa; ⛔ ghi xóa thứ chưa từng tồn tại. documentation §9 |
| **34** | **Cấm sửa dữ liệu ngoài yêu cầu** (19.76) | ⛔ Sửa, ghi đè, revert dữ liệu, chỉ số, ngày công, điểm, cấu hình ngoài phạm vi. |
| **38** | **Cấm tham chiếu prompt trong mã** (19.80) | ⛔ Tên tệp prompt, định danh AI, nhật ký đổi tên trong comment, docstring, commit; `Modified date` chỉ ghi ngày. backend §4.16 |
| **48** | **Giữ dòng cũ** (19.89) | Logic không đổi thì giữ nguyên dòng; format, ngắt dòng chỉ áp cho dòng mới hoặc dòng sửa. backend §4.21 |
| **58** | **Cấm commit tệp local, bí mật** (19.100) | ⛔ Stage, commit, push `.env.development`, `.env.local`, `Database.json`, `TAKey*.key`, `Shared.Core.Security.dll`. git §4.6 |
| **61** | **Locale phân hệ, thoát `@`** (20.29) | Locale riêng `its/<module>/` nạp qua `import.meta.glob`; ⛔ `@` trần cạnh tên biến (dùng `{'@'}`); seed SQL cập nhật có `WHERE Value = N'<cũ>'`. frontend §5 |
| **62** | **Thẻ rỗng tự đóng** (20.30) | Thẻ không có nội dung viết `<div ... />`; ⛔ cặp rỗng `<div ...></div>` (áp cho thẻ mới hoặc thẻ sửa). frontend §6.2 |
| **63** | **Commit message chỉ việc chính** (19.102) | ⛔ Ghi viết, chạy test, kết quả test, cập nhật tài liệu, dọn mã. git §3 |
| **64** | **Mỗi `.sql` một bảng** (19.103) | Một tệp chỉ ghi một bảng, tên `<phan-he>-<vai-tro>-<TênBảng>.sql` (giả lập `..._local.sql`); đầu tệp ghi `BẢNG GHI`, `THỨ TỰ CHẠY`, kiểm bảng bằng `SET NOEXEC ON`. database-mcp §2.2 |
| **65** | **Định dạng nội dung commit** (19.104) | Xuất khối `text` riêng từng repo: 2 dòng Summary lặp lại và 2–3 gạch đầu dòng việc chính. git §3 |
| **66** | **Thông báo không lộ thông tin nội bộ** (19.105) | ⛔ Tên bảng, cột cờ, đường dẫn, chuỗi kết nối trong câu báo người dùng; chỉ nêu việc cần làm, chi tiết ghi log. backend §4.26 |
| **67** | **Cấm sửa resource BE** (19.107) | ⛔ Thêm, sửa, xóa, format, sort `src/TAC_WebAPI/Resources/*.json`. backend §4.27 |
| **68** | **Cấm đường dẫn tuyệt đối máy cá nhân** (19.108) | ⛔ `/Users/...`, `C:\Users\...`; dùng đường dẫn tương đối hoặc biến môi trường. backend §4.28 |
| **69** | **Test cho cơ chế mới** (19.109) | Thêm tính năng, cơ chế mới phải có test toàn trình qua API xác nhận dữ liệu ghi đúng CSDL, DI/Wolverine thông suốt. testing §1.6, backend §4.29 |
| **70** | **Dấu `_` trong tên nhánh** (19.110) | `_` chỉ khi có TaskCode/Issue (`...-[TaskCode]_[mô-tả]`); nhánh thường dùng `-` kèm tiền tố module. git §2 |
| **71** | **Entity thêm cột thì bật tạo bảng khi test** (19.111) | Bật `TableSettings.EnableInitTable = true` đúng ConfigId trong `tests/BE/appsettings.Test.json`, giữ `EnableIncreTable = false`; ⛔ `ALTER TABLE` tay, sửa `Database.json`. testing §2 |
| **72** | **Kế thừa phiên AI** (19.112) | Tiếp quản phiên trước: đọc memory `.agents/memory/` và log phiên gần nhất; ⛔ suy đoán, làm ngược quyết định đã chốt. core-protocol §4 |
| **73** | **Cấm nới bộ kiểm để test xanh** (19.113) | ⛔ Nới Validator, Guard đang chặn nguy cơ mất/lặp dữ liệu; test cũ sai chuẩn thì sửa test (`AssertRejectedWith`). backend §4.30, testing §2.8 |
| **74** | **Bảo toàn tệp SSOT** (19.114) | Tài liệu đã gom một nguồn: ⛔ tạo lại, khôi phục, sửa tệp cũ đã xóa; bổ sung ghi vào tệp SSOT đang sống. documentation §1.4 |
| **75** | **Giữ khế ước dữ liệu khi sửa UI** (20.32) | Sửa UI chỉ ở template/CSS; ⛔ đổi payload API trừ khi có yêu cầu; trường mới phải tương thích ngược, không gửi thừa/null. frontend §11 |
| **76** | **Câu báo lỗi không nhắc dạng cũ** (19.115) | ⛔ Câu báo, tooltip, placeholder nhắc cách làm trước; dữ liệu dạng cũ xử lý như chưa hợp lệ; lưu ý ghi vào `.md` phân hệ. frontend §5 |
| **77** | **Không lồng lời gọi trên một dòng** (19.116) | C#: lời gọi nằm trong tham số của lời gọi khác thì tách mỗi bước thành biến `var`; chuỗi LINQ/SqlSugar xuống dòng không tính. backend §4.31 |
| **78** | **Spinner loading chuẩn** (20.33) | Mọi `vxe-grid` / `el-table` dùng spinner 8-dots SVG 50px màu primary, chữ "Đang tải..."; bắt buộc test E2E. frontend §12 |

## Luật cốt lõi theo miền (áp cho mọi mã mới; mã lân cận sai thì không chép theo)

- **BE CQRS** (backend §2, §4.8): handler `Controllers/<ChứcNăng>/{Commands,Queries}` cài `IWolverineHandler`, ⛔ `ITransient`/`IScoped`/`ISingleton`; ⛔ DataAnnotation trên DTO, kiểm bằng FluentValidation trong `Validators/`; thêm `using`, ⛔ viết tên kiểu kèm namespace (19.65).
- **Test** (testing §1.4): đi qua nguyên luồng từ điểm vào chính thức (API qua `IMessageBus`, Worker như `ProcessSubscriptions`); ⛔ unit test helper, mock ngữ cảnh, FE đọc mã rồi so chuỗi; cùng luồng khác dữ liệu → `[Theory]` + `[InlineData]`. Xóa test mà không lỗi nào lọt thì không viết.
