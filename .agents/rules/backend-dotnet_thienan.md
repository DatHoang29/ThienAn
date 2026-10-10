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
