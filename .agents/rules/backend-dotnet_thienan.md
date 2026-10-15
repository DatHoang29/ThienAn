---
name: backend-dotnet
version: 1.0.0
priority: P1
trigger: model_decision
description: Backend .NET standards, Wolverine bus, SqlSugar ORM, Clean Architecture, and C# coding conventions.
---

# 🏗️ Quy Chuẩn Backend .NET & Clean Architecture — Thiên Ân

> **Thuộc hệ thống SSOT `.agents/rules/`**. Quy định kiến trúc module, Wolverine bus, SqlSugar ORM, FluentValidation và quy chuẩn C# .NET.

---

## 🏛️ 1. Cấu Trúc Module Chuẩn (Layer Separation)

Hệ thống phân tách thành 2 tầng project chính:
1. **`Modules.[TênHệ].Core`**: Chứa toàn bộ Entity, DTO (Data Transfer Object), Interfaces, Enums và Business Core Logic.
2. **`Modules.[TênHệ]`**: Chứa Controllers, Application Services, Commands/Queries (Wolverine), FluentValidation, DI Extensions.

### Cấu Hình `BaseController` & Swagger Auto-Discovery
* Mỗi Module bắt buộc có `BaseController.cs` riêng tại `Modules.[TênHệ].Controllers`.
* Khai báo hằng số `GroupName` và `BasePath`, gắn `[ApiDescriptionSettings(GroupName)]`:
```csharp
namespace Modules.ShareData.Controllers
{
    [ApiDescriptionSettings(GroupName)]
    [Route(BasePath + "/[controller]")]
    public abstract class BaseController : AppControllerBase
    {
        public const string GroupName = "ShareData";
        public const string BasePath = "api/vms";
    }
}
```

---

## ⚙️ 2. Wolverine Bus, CQRS & Validation

### Cấu Trúc Thư Mục Chức Năng (`Controllers/<ChứcNăng>/`)
* **`<ChứcNăng>Controller.cs`**: Thin Controller, **BẮT BUỘC chỉ gọi `MessBus.InvokeAsync()`**. Tuyệt đối không viết business logic tại controller.
* **`Commands/`**: Handler xử lý Ghi (Add/Update/Delete). Bắt buộc implement `IWolverineHandler` với phương thức `HandleAsync(<InputType> command)`. Dùng Mapster để ánh xạ DTO sang Entity.
* **`Queries/`**: Handler xử lý Đọc (Page/GetList/GetById). Truy vấn phân trang trả về `SqlSugarPagedList<Output>`, dùng `.OrderBuilder()` và `.ToPagedListAsync()`. Khi cần sắp xếp bản ghi mới nhất, bắt buộc dùng `BaseConst.SortFieldConst.UpdateOrCreateTime` (Rule 19.60). Trả thẳng đối tượng phân trang, **CẤM** bọc qua `.Adapt<SqlSugarPagedList<TOutput>>()`.
* **`Dto/`**:
  - Input: `PageXxxInput` (kế thừa `BasePageInput`), `AddXxxInput` (kế thừa Entity), `UpdateXxxInput`, `DeleteXxxInput`.
  - Output: `XxxOutput` / `PageXxxOutput`. Cấu hình ánh xạ Mapster (`IRegister`) viết trực tiếp trong file DTO Output, **CẤM** tạo folder `Mappings/` riêng.
  - ⛔ **CẤM dùng DataAnnotation** (`[Required]`, `[Range]`, `[StringLength]`). Toàn bộ kiểm tra dữ liệu thực hiện 100% qua FluentValidation.
* **`Validators/`**: Chứa `AbstractValidator<T>` nhận `IStringLocalizer lz` qua DI. Không tạo class phụ/mock hay constructor không tham số.
* **Repository Naming**: Đặt tên theo prefix `_rsp{EntityName}` (VD: `SqlSugarRepository<EshPartner> _rspEshPartner;`).

---

## 💾 3. Thực Thể (Entity) & SqlSugar ORM

### Quy Chuẩn Entity Class
* Tất cả Entity kế thừa `EntityTenant` (từ `Shared.Core.Domain`).
* Base class sử dụng `CreateTime` và `UpdateTime` (ID viết HOA cả hai ký tự: `ID`).
* **Hằng số độ dài `EntityConst`**: BẮT BUỘC dùng `EntityConst.Length32`, `EntityConst.Length64`, `Length128`, `Length256`, `Length512`, `KeyFieldLength`. CẤM hardcode số (như `Length = 32`).
* ⛔ **CẤM gán `Length` cho kiểu không phải chuỗi (Lỗi SQL Server 2716)**: `Length = EntityConst...` CHỈ dùng cho chuỗi (`string` / `string?`). Tuyệt đối cấm gán vào `int`, `long`, `float`, `DateTime`, `bool`.
* **Quy định `IsNullable = true`**: Tất cả cột Entity (trừ khóa chính `ID`) bắt buộc khai báo `[SugarColumn(IsNullable = true, ...)]` và dùng kiểu nullable (`string?`, `DateTime?`, `int?`...).
* **Thuộc Tính Entity Không Ánh Xạ CSDL (Rule 22 / Rule 19.68)**: Nếu Entity class có thuộc tính phụ trợ, tính toán, trạng thái runtime hoặc tạm thời không lưu trữ trong database, **BẮT BUỘC gắn `[SugarColumn(IsIgnore = true)]`** để SqlSugar bỏ qua không sinh/truy vấn cột trên bảng CSDL, tránh lỗi lệch schema (`Invalid column name`).

### Khóa Chính `ID` (SnowFlake ID - Rule 19.44)
* **WebAPI**: Được AOP/Interceptors tự động sinh Snowflake ID khi Insert. **CẤM tự gán tay `entity.ID`** trong WebAPI.
* **Background Worker**: BẮT BUỘC gán thủ công `entity.ID = Yitter.IdGenerator.YitIdHelper.NextId()` trước khi insert.

### Quy Tắc Thao Tác SqlSugar (P0)
1. **Ưu tiên ORM tuyệt đối khi đã có Entity (Rule 19.20)**: TUYỆT ĐỐI CẤM dùng raw SQL DML (`UPDATE`, `INSERT`, `DELETE`) thủ công.
2. **CẤM nối chuỗi SQL (Rule 19.17)**: Khi bắt buộc dùng SQL truy vấn động, BẮT BUỘC dùng tham số hóa qua `SugarParameter`.
3. **SqlSugar Client trong Worker (Rule 19.41)**: Trong Background Worker, Polling Loop hoặc Concurrent Tasks, BẮT BUỘC khởi tạo bản copy độc lập:
   ```csharp
   using var db = baseClient.CopyNew();
   ```
4. **Không tự dựng tính năng có sẵn (Rule 19.33)**: SqlSugar đã có API sẵn (`InsertReturnIdentity`, `UpdateColumns`, `SplitTable`...) thì BẮT BUỘC dùng, cấm tự viết lại.

---

## 🧹 4. Quy Chuẩn Code C# (Clean Code Standards)

1. **Zero Unnecessary Usings (IDE0005)**:
   - Kiểm tra `GlobalUsings.cs` trước khi thêm `using`. Nếu namespace đã có trong `GlobalUsings.cs`, **TUYỆT ĐỐI CẤM** thêm vào file riêng lẻ.
   - Luôn rà soát và dọn dẹp các `using` thừa sau khi sửa code.
2. **Không Dùng Hậu Tố `-Async` (No-Async Suffix)**:
   - Mọi phương thức trả về `Task` hoặc `Task<T>` đặt tên trực diện: `GetData()`, `ExecuteCommand()`, `ProcessQueue()`.
   - ⛔ KHÔNG đặt tên `GetDataAsync()`, `ExecuteCommandAsync()`. (Ngoại lệ: implement interface từ thư viện ngoài như Wolverine `HandleAsync`).
3. **Ưu tiên `var` cho biến cục bộ (IDE0007 / Rule 19.39)**:
   - Dùng `var` khi kiểu dữ liệu hiển nhiên hoặc có thể suy luận rõ ràng: `var entity = new EshPartner();`.
4. **Auto-Property & Cấm Backing Field Thừa (Rule 19.22)**:
   - Dùng auto-property: `public string Name { get; set; }`. Cấm tách `private string _name` nếu không có logic phụ trợ đặc thù.
5. **Xử lý Exception Tập Trung (Rule 19.19, 19.48)**:
   - ⛔ CẤM `try-catch` dồn dập, lồng nhau nhiều lớp.
   - ⛔ CẤM nuốt lỗi ở hàm con / helper (`catch { return null; }`). Để exception ném tự nhiên lên orchestrator / tầng cha điều phối để log và rollback giao dịch.
6. **Thứ Tự Thành Viên Class (Rule 19.47)**:
   - Cấu trúc theo nhóm: Constants/Fields tĩnh -> Fields/Dependencies DI -> Constructor -> Public Methods -> Protected/Internal Methods -> Private Helper Methods.
   - Khi thêm code mới, thêm vào cuối nhóm tương ứng (Append-only).
7. **Tôn Trọng Phong Cách Mã Nguồn & Signature (Rule 19.49, 19.50, 19.51)**:
   - Cấm tự ý đổi signature phương thức / service dùng chung.
   - Cấm sinh hàm mới làm hàm cũ mồ côi (dead code).
8. **Vị Trí `using` & Quy Chuẩn `namespace` (Rule 19.65)**:
   - **Vị trí `using` luôn đứng đầu file**: Mọi chỉ thị `using` **BẮT BUỘC đặt ở đầu tệp tin**, phía trước khai báo `namespace`. ⛔ **TUYỆT ĐỐI CẤM** đặt `using` sau khai báo `namespace` (kể cả khi dùng file-scoped namespace).
   - **Đồng nhất kiểu `namespace`**: Tuân thủ phong cách namespace của module / file gốc hiện hữu. Khi tạo file mới hoặc tách `partial class`, **BẮT BUỘC đồng nhất** với các file cùng class/module: nếu các file hiện tại dùng block-scoped namespace (`namespace X { ... }`) thì file mới bắt buộc dùng block-scoped namespace, ⛔ **CẤM** viết lẫn lộn file-scoped namespace (`namespace X;`) trong cùng một partial class.
9. **Can Thiệp Tối Thiểu & Cấm Sửa Code Ngoài Phạm Vi (Minimal Diff / Scope Discipline)**:
   - ⛔ **TUYỆT ĐỐI CẤM** sửa bất kỳ file, class, phương thức hoặc dòng code nào **KHÔNG LIÊN QUAN TRỰC TIẾP** đến yêu cầu hiện tại để tránh làm biến động (change) mã nguồn quá nhiều không cần thiết.
   - Chỉ sửa đúng các dòng code tối thiểu cần thiết để đạt mục tiêu.
   - Giữ nguyên 100% logic, định dạng, biến, chú thích của các hàm/file xung quanh đang chạy ổn định.
   - Cấm tự ý tiện tay refactor, format lại cả file, đổi namespace, đổi kiểu dữ liệu hay sửa file ngoài phạm vi.
10. **Xóa Triệt Để Mã Không Dùng — Cấm Gắn `[Obsolete]` Để Lại Mã Thừa (Rule 22 / Rule 19.68)**:
    - Đối với code C# thông thường (service, helper, util, DTO, class, method, handler...): Khi không dùng hoặc đã được thay thế bằng giải pháp mới, **BẮT BUỘC XÓA BỎ HOÀN TOÀN (Clean Delete)** khỏi solution.
    - ⛔ **TUYỆT ĐỐI CẤM** để lại mã rác rồi gắn thuộc tính `[Obsolete]` làm bẩn codebase và sinh warning trình biên dịch (CS0618).
    - Mọi test case kiểm thử mã cũ cũng phải được xóa bỏ hoặc thay thế tương ứng bằng bài test cho giải pháp mới.
11. **Chuẩn Hóa Thông Báo Lỗi & Ngoại Lệ — Cấm Hardcode String Trong `Oops.Oh` (Rule 23 / Rule 19.69)**:
    - ⛔ **TUYỆT ĐỐI CẤM** hardcode chuỗi string trực tiếp (tiếng Việt hoặc tiếng Anh) trong `Oops.Oh("...")` hay `throw new AppFriendlyException("...")`.
    - **Định danh thông báo qua `BaseMsg`**: Mọi thông báo lỗi, thông điệp kiểm tra dữ liệu (validation), thông báo ngoại lệ nghiệp vụ (exception) **BẮT BUỘC** khai báo tập trung trong `BaseMsg` của module (kế thừa `BaseLocaleManager`) theo tiền tố chuẩn:
      - `lz.validation.<module>.<mãKey>` (hoặc `BaseMsg.BaseValidation.*`): Dùng cho lỗi kiểm tra dữ liệu đầu vào.
      - `lz.exception.<module>.<mãKey>` (hoặc `BaseMsg.BaseException.*`): Dùng cho lỗi logic nghiệp vụ, vi phạm trạng thái.
      - `lz.entity.<module>.<mãKey>` (hoặc `BaseMsg.BaseEntity.*`): Dùng cho tên trường, tên thực thể phục vụ ghép vào placeholder `{0}`, `{1}`.
    - **Tham số hóa thông điệp qua Furion**: Khi thông báo cần chèn tham số động (phạm vi, tên đối tượng, mã lỗi), sử dụng placeholder `{0}`, `{1}` trong CSDL và truyền đối số vào `Oops.Oh`:
      ```csharp
      throw Oops.Oh(BaseMsg.ShareData.Validation.LookbackDaysRange, 1, 365);
      ```
    - **Cấu hình trên CSDL `SysTerminology`**: Mọi key thông báo mới **BẮT BUỘC** tạo script SQL idempotent (`DocBusinessThienAn/**/sql/*.sql`) để seed vào bảng `SysTerminology` trên CSDL với đầy đủ 2 ngôn ngữ (`Lang = 'vi-VN'` và `Lang = 'en-US'`), `Status = 1`:
      - `Name`: Tiền tố nhóm (`lz.validation`, `lz.exception`, `lz.entity`...).
      - `Code`: Mã phân cấp sau tiền tố (ví dụ `sharedata.lookbackDaysRange`, `cctv.deviceNotFound`).
      - `Value`: Nội dung thông điệp (hỗ trợ tham số hóa qua placeholder `{0}`, `{1}`...).
    - **Cấm sửa tay file tĩnh Backend**: ⛔ **TUYỆT ĐỐI CẤM** sửa tay vào `TAC_WebAPI/Resources/*.json` vì các file này được sinh tự động (auto-generated) từ CSDL `SysTerminology` qua endpoint `LoadServerTerm`.



