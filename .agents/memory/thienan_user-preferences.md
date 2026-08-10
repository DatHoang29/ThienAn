---
type: user_preferences
created: 2026-07-21
updated: 2026-07-29
---

# Thiên An User Preferences & Project Conventions

## Git Workflow
- Always create a new dedicated branch for major code changes.
- Branch name format should follow: `feature/[task-slug]` or `fix/[bug-slug]`.
- **DO NOT auto-commit or push Git**: AI KHÔNG ĐƯỢC tự ý thực hiện `git add .` hoặc `git commit` full thư mục. Chỉ khi người dùng yêu cầu commit và chính người dùng tự chọn/đưa file vào Staged Changes (`git add` thủ công), AI mới thực hiện commit đúng các file trong Staged Changes đó.
- **DESCRIPTIVE COMMIT MESSAGE CONVENTION**: Nội dung commit message BẮT BUỘC phải mô tả rõ ràng, chính xác theo tính năng thực tế đã áp dụng (dùng chuẩn Semantic Commit như `feat(module): description`, `refactor(module): description`, `fix(module): description`...). KHÔNG dùng các chuỗi chung chung ngắn ngủi như "up", "test".

## Docker & Infrastructure Rules
- **Docker SQL Server on macOS**: ALWAYS use `mcr.microsoft.com/azure-sql-edge:latest` for SQL Server containers on macOS (Apple Silicon M1/M2/M3/M4). NEVER use `mcr.microsoft.com/mssql/server:2022-latest` as x86_64 emulation under QEMU causes immediate memory crashes (`Invalid mapping of address`).

## Workflow & Execution Rules
- **MINIMAL DIFF & MAINTENANCE**: ONLY modify files and code strictly necessary to accomplish the requested feature or bugfix. NEVER auto-upgrade package versions (`PackageReference` in `.csproj`), edit unrelated shared projects (`Shared.Reference`), or modify existing files outside the task scope unless explicitly instructed by the user.
- **DO NOT auto-run build/test/dev server**: AI KHÔNG ĐƯỢC tự ý chạy `dotnet build`, `dotnet test`, `dotnet run`, hoặc khởi động dev server/app sau khi sửa code, trừ khi người dùng yêu cầu trực tiếp.
- **DISTINGUISH REFERENCE FROM ACTION**: Khi người dùng yêu cầu "tham khảo", "xem thử", "giải thích" hoặc hỏi ý kiến, AI BẮT BUỘC phải phân tích và trả lời thảo luận trước, KHÔNG ĐƯỢC tự ý nhảy vào áp dụng hoặc thêm/sửa code khi chưa có xác nhận từ người dùng.
- **PRESERVE USER MANUAL EDITS & PREFERENCES (Tôn trọng code sửa tay & ý định người dùng)**: Khi người dùng đã chỉ định cách viết (VD: dùng `while (reader.Read())` đồng bộ) hoặc tự sửa tay/bỏ bớt điều kiện, AI KHÔNG ĐƯỢC TỰ Ý hoàn tác (revert) or sửa ngược lại về cách viết cũ trong các lần refactor tiếp theo.
- **SINGLE-STATEMENT IF FORMATTING**: Đối với câu lệnh `if` chỉ chứa 1 dòng thực thi (VD: `if (targetSubs.Count == 0)`, `if (isCodeExists)`...), BẮT BUỘC ngắt dòng và thụt lề cho câu lệnh thực thi (VD: `if (targetSubs.Count == 0)\n    return;`), TUYỆT ĐỐI KHÔNG VIẾT NGANG TRÊN CÙNG 1 DÒNG (`if (targetSubs.Count == 0) return;`). Không bắt buộc dùng dấu ngoặc nhọn `{}` cho câu lệnh 1 dòng nhưng BẮT BUỘC NGUYÊN TẮC NGẮT DÒNG.
- **DO NOT USE `#region`**: KHÔNG tự ý chèn thẻ `#region` hoặc `#endregion` vào code C# trừ khi người dùng yêu cầu. Giữ biến/field nguyên bản và sạch sẽ.
- **NO MANUAL VALIDATE/MAP HELPER METHODS IN COMMAND HANDLERS**: KHÔNG tự viết các hàm trợ giúp thủ công như `ValidateInput` hoặc `MapToOutput` bên trong CommandHandler. BẮT BUỘC sử dụng FluentValidation (`AbstractValidator<T>`) cho toàn bộ logic kiểm tra dữ liệu và sử dụng Mapster (`entity.Adapt<T>()` / `ShareDataMapsterRegister`) cho toàn bộ logic ánh xạ DTO Output.
- **PAGED LIST PROJECTION DIRECT RETURN**: Trong QueryHandler phân trang, khi dùng `.Select(x => new TOutput { ... }, true)` hoặc truy vấn trực tiếp ra `SqlSugarPagedList<TOutput>`, BẮT BUỘC trả thẳng đối tượng phân trang (VD: `return paged;` hoặc `return await query.ToPagedListAsync(...)`), KHÔNG bọc qua `.Adapt<SqlSugarPagedList<TOutput>>()` nữa.
- **REPOSITORY NAMING CONVENTION**: Đặt tên biến Repository trong CommandHandler / QueryHandler theo chuẩn prefix `_rsp{EntityName}` (VD: `SqlSugarRepository<EshPartner> _rspEshPartner;`, `SqlSugarRepository<EshDataSource> _rspEshDataSource;`). KHÔNG dùng các tên chung chung như `_repository`, `_repo`, hay `_baseRepository`.
- **MAPSTER MAPPER LOCATION CONVENTION**: KHÔNG tạo thư mục `Mappings` riêng rẽ để chứa file Register tập trung. Tất cả cấu hình ánh xạ Mapster (`IRegister`) BẮT BUỘC viết trực tiếp bên trong file DTO Output tương ứng (VD: `EshPartnerOutput.cs` chứa `public class EshPartnerMapper : IRegister`, `MenuOutput.cs` / `SysMenuOutput.cs` chứa mapper tương ứng).
- **NO EXTRA/DESIGN-TIME CLASSES IN VALIDATORS**: Validator chỉ khai báo duy nhất 1 Constructor nhận `IStringLocalizer lz` (hoặc `localizer`), KHÔNG tự ý chèn các class phụ/mock như `DesignTimeLocalizer` hay constructor không tham số `: this(...)`.
- **NO DATAANNOTATIONS IN DTO (Strict FluentValidation Only)**: KHÔNG dùng các thuộc tính DataAnnotation validation (như `[Required(ErrorMessage = "...")]`, `[Range(...)]`, `[StringLength(...)]`...) trong các class DTO. Tất cả logic kiểm tra dữ liệu và thông báo lỗi đa ngôn ngữ BẮT BUỘC thực hiện 100% qua `FluentValidation` (`AbstractValidator<T>`) kết hợp `IStringLocalizer lz` và `BaseMsg`.
- **API CONTROLLER SUMMARY CONVENTION**: Trên mỗi phương thức Action trong Controller, comment XML Doc `/// <summary>` BẮT BUỘC mô tả rõ ràng, tự nhiên ý nghĩa và chức năng thực tế của hàm (VD: `/// <summary>\n/// Lấy danh sách cảnh báo & lỗi (phân trang)\n/// </summary>`). Tuyệt đối KHÔNG chèn mã prefix/số thứ tự rườm rà (như L1., E2., DS3...). Thẻ `[DisplayName("...")]` giữ nguyên tên hiển thị chuẩn.
- **XML COMMENT CONVENTION FOR HANDLERS/DTO/VALIDATORS/TESTS**: Tất cả các lớp và phương thức trong `Commands/`, `Queries/`, `Dto/`, `Validators/` và toàn bộ phương thức kiểm thử Test (`[Fact]` / `[Theory]` trong `tests/`) BẮT BUỘC phải có comment XML `/// <summary>` theo thứ tự chuẩn: `Author: Đạt` ở dòng đầu, `Description: <Mô tả chức năng/nghiệp vụ>` ở giữa, và `Created date: DD/MM/YYYY` ở cuối. BỎ HẲN và KHÔNG DÙNG field `Updated date:`.
- **NO DUPLICATE XML SUMMARY BLOCKS (Cấm lặp thẻ summary)**: TUYỆT ĐỐI KHÔNG tự ý chèn chồng hoặc nhân bản các khối `/// <summary>` rườm rà trên cùng một class/hàm/property. Mỗi đối tượng code CHỈ ĐƯỢC CÓ DUY NHẤT 1 khối `/// <summary>`. Khi cập nhật nội dung comment, BẮT BUỘC sửa trực tiếp vào khối comment cũ thay vì thêm khối `/// <summary>` thứ 2.
- **NO DELETE ON READ-ONLY TARGET TABLES IN TESTS**: Bảng dữ liệu nghiệp vụ thuộc hệ thống ngoài hoặc module khác (như `TmsTrafficData`, `TmsOrder`...) là bảng ĐỌC (READ-ONLY). AI TUYỆT ĐỐI KHÔNG ĐƯỢC thực thi các câu lệnh `DELETE` hay `TRUNCATE` tràn lan trên toàn bộ bảng. Đối với dữ liệu do bài test tự chèn vào hoặc file tạm tạo ra, bài test BẮT BUỘC thực hiện dọn dẹp (chỉ cần xóa/clean trực tiếp ở đầu hàm test hoặc cuối hàm, KHÔNG BẮT BUỘC dùng `try ... finally` rườm rà). Điều này đảm bảo code test gọn gàng, đơn giản và 100% không ảnh hưởng đến dữ liệu sẵn có khác.
- **TEST METHOD NAMING CONVENTION**: Tên phương thức test BẮT BUỘC theo chuẩn `{MethodName}_{CaseDescription}_Test`. Trong đó `{MethodName}` là tên hàm đang được kiểm thử (VD: `QueryPacket107Async`, `ProcessBatchSubscriptionsAsync`), `{CaseDescription}` mô tả ngắn gọn scenario/case (VD: `ReturnsIncidentData`, `WhenNoData_ReturnsEmptyList`, `WithInvalidEnum_ReturnsError`). Ví dụ đầy đủ: `QueryPacket107Async_ReturnsIncidentData_Test`, `ProcessBatchSubscriptionsAsync_WhenNoSubscriptions_ReturnsSafely_Test`.
- **SQL SCRIPT LOCATION & NAMING RULE**: Mỗi khi tạo script SQL (tạo bảng, chỉnh sửa schema, tạo/xóa index...), BẮT BUỘC lưu vào thư mục `/Users/hoangquydat/ThienAn/SQL/scripts` (hoặc `@[SQL/scripts]`) và TÊN FILE BẮT BUỘC VIẾT HOA HOÀN TOÀN (VD: `SCRIPT_MANAGE_INDEXES_SHAREDATA.SQL`).
- **STRICT READ-ONLY MCP & DB OPERATION RULE**: Kết nối CSDL thông qua MCP hoặc bất kỳ công cụ truy vấn CSDL nào BẮT BUỘC chỉ được phép ĐỌC (READ-ONLY). AI TUYỆT ĐỐI KHÔNG ĐƯỢC thực thi các câu lệnh thay đổi dữ liệu hoặc biến đổi cấu trúc DB (`INSERT`, `UPDATE`, `DELETE`, `DROP`, `ALTER`, `TRUNCATE`...) qua MCP Server. Chỉ sử dụng lệnh `SELECT` hoặc các API đọc thông tin Schema.
- **SQL SCRIPT MODULE ISOLATION SCOPE RULE**: Tất cả các script SQL (DDL & DML) tạo mới hoặc cập nhật cho một Module BẮT BUỘC CHỈ ĐƯỢC PHÉP tác động (`CREATE`, `ALTER`, `DROP`, `INSERT`, `UPDATE`, `DELETE`) lên đúng danh sách các bảng CSDL thuộc phạm vi sở hữu của Module đó (Ví dụ: đối với Module `ShareData`, CHỈ ĐƯỢC TÁC ĐỘNG lên 8 bảng `Esh*`: `EshPartner`, `EshDataSource`, `EshMappingProfile`, `EshFieldMapping`, `EshSubscription`, `EshExportLog`, `EshSystemLog`, `EshEventSource`). TUYỆT ĐỐI KHÔNG ĐƯỢC tự ý tạo, sửa cấu trúc, xóa hoặc nạp/sửa dữ liệu trên các bảng CSDL của module khác (như `TmsTrafficData`, `TmsWeather`, `TmsIncident`, `TollTransactionOut`...).
- **ENTITY UPDATE IS AUTHORITATIVE DESIGN SOURCE (Thư mục Entity gốc từ thiết kế)**: Khi có thư mục `EntityUpdate` hoặc bất kỳ bộ Entity gốc nào được đưa vào từ team thiết kế, đó là **bản thiết kế chính thức (source of truth)**. AI BẮT BUỘC phải đồng bộ entity trong code hiện tại theo ĐÚNG cấu trúc, kiểu dữ liệu, tên property, và attribute của bản thiết kế gốc. TUYỆT ĐỐI KHÔNG ĐƯỢC tự ý đề xuất phương án thay thế hoặc giữ nguyên kiểu cũ nếu bản thiết kế gốc đã thay đổi. Khi phát hiện khác biệt, AI phải báo cáo rõ sự khác biệt và thực hiện đồng bộ theo hướng bản thiết kế gốc, KHÔNG đề xuất đi ngược lại.

## Code Style & Class Conventions
- **Class Header XML Comments Required**: Every new or updated class MUST include an XML summary header block formatted as follows (Author is always `Đạt`, ONLY `Created date:`, NO `Updated date:`):
```csharp
/// <summary>
/// [Description / Table Name]
/// Author: Đạt
/// Created date: [dd/MM/yyyy]
/// </summary>
```
- **Using Directives Instead of Inline Namespaces**: BẮT BUỘC dùng `using` directive ở đầu file (VD: `using Modules.ShareData.Core.Entities;`) để gọi tên class ngắn gọn (VD: `EshPartner`) thay vì gõ namespace dài inline trong code (VD: `Core.Entities.EshPartner`).
- **No Unnecessary Using Directives (IDE0005)**: KHÔNG import `using` mà không sử dụng trong file. Chỉ thêm `using` khi thực sự cần dùng type/namespace đó trong code.

## Entity Conventions & Length Constants
- **Entity Inheritance**: All Entity classes MUST inherit from `EntityTenant` (from `Shared.Core.Domain`).
- **EntityConst for Column Lengths**: BẮT BUỘC dùng các hằng số `EntityConst` (từ namespace `Shared.DTO.Constants.Application`, VD: `EntityConst.Length32`, `EntityConst.Length64`, `EntityConst.Length128`, `EntityConst.Length256`, `EntityConst.Length512`, `EntityConst.KeyFieldLength`) cho tất cả attribute `[SugarColumn(Length = ...)]` và `[MaxLength(...)]` trong Entity và DTO. KHÔNG ĐƯỢC dùng số hardcode trực tiếp (như `Length = 32`).
- **Shared Enums for ShareData**: Tất cả các Enum dùng chung cho Module ShareData (ESHARE V1) được định nghĩa trong `Modules.ShareData.Core.Enums.EshEnums` (`DataSourceKind`, `MappingDirection`, `DatatypeIdEnum`, `SubDirection`, `SubMode`, `SubState`, `PublishFormat`, `PubState`, `ExportStatus`, `PartnerStatus`, `ProtocolProfile`).
- **Validators Directory Conventions**: Mỗi feature/controller trong module (VD: `Controllers/DataSource/`, `Controllers/Partner/`) BẮT BUỘC có thư mục `Validators/` chứa các class FluentValidation (`Add[Entity]Validator`, `Update[Entity]Validator`, `Delete[Entity]Validator` kế thừa `AbstractValidator<T>`) tương tự cấu trúc chuẩn trong `Modules.Samplev2/Controllers/Category/Validators/`.
- **Controller Directory & Namespace Naming Convention**: Đặt tên thư mục Controller theo tên Feature/Entity ngắn gọn bằng cách **BỎ TIỀN TỐ MODULE** (VD: Entity `EshDataSource` ➡️ folder `Controllers/DataSource/` ➡️ namespace `Modules.ShareData.Controllers.DataSource`; Entity `SysConfigData` ➡️ folder `Controllers/ConfigData/`). Cách này đảm bảo tên Namespace khác hoàn toàn tên Entity Class, tránh bị xung đột CS0118 và cho phép dùng `using` trực tiếp sạch sẽ.

## EntityTenant Base Class Properties (CRITICAL)
- **Property thời gian**: Base class `EntityTenant` dùng `CreateTime` và `UpdateTime` (KHÔNG phải `CreatedTime` hay `UpdatedTime`). Khi tạo Output DTO hoặc mapping, BẮT BUỘC dùng đúng tên:
  - ✅ `entity.CreateTime` / `entity.UpdateTime`
  - ❌ `entity.CreatedTime` / `entity.UpdatedTime` → sẽ gây lỗi `CS1061`
- **Property ID**: Base class dùng `ID` (viết HOA cả hai ký tự), KHÔNG phải `Id`.

## Module Architecture & Structure
- **SINGLE-PROJECT MODULE ARCHITECTURE (Cấu trúc Modules 2.1.3)**: Tất cả các module đều BẮT BUỘC phải theo cấu trúc project duy nhất (VD: `Modules.ShareData.csproj`), không tách làm project `.Core` phụ. Thư mục project module bao gồm:
  - `Controllers`: Chứa xử lý các API truy cập từ bên ngoài.
  - `Core`: Chứa các phần căn bản của module, bao gồm Abstraction, Entity, Exception.
  - `[Bắt buộc] Extensions`: Chứa các class khai báo module và các thư viện tích hợp. Thư mục này bắt buộc phải có để khai báo sử dụng.
  - `Infrastructure`: Chứa các phần xử lý liên quan tới CSDL (`BaseRepository.cs`), xử lý dữ liệu (`Services/`).
  - `GlobalUsings.cs`
  *(Nếu module không có phần nào thì có thể bỏ qua phần đó, nhưng tuyệt đối không tự tạo project phụ rải rác).*

## Module Localization & Translations
- **MODULE LOCALIZATION SPECIFICATION (Dịch thuật 2.1.4)**:
  - Tất cả các Module có sử dụng dịch thuật BẮT BUỘC tạo file `Core/Exceptions/BaseMsg.cs` kế thừa `BaseLocaleManager` (từ `Shared.DTO.Constants.Localization`).
  - Trong `BaseMsg`, tạo các class đại diện cho từng Chức năng/Entity (VD: `EshPartner`, `EshDataSource`...).
  - Trong mỗi class chức năng, chia thành các class con chứa hằng số dịch thuật: `Validation`, `Message`, `Exception`, `Entity` (group action).
  - **Vị trí thư mục Resources**: Thư mục `Resources` nằm ngang hàng với `Controllers`, `Core`, `Extensions`, `Infrastructure` trong root project của Module (VD: `Modules.ShareData/Resources/vi-VN.json`). KHÔNG đặt bên trong thư mục `Controllers`. Dịch thuật được cập nhật đồng bộ vào `src/TAC_WebAPI/Resources/` để hệ thống load đầy đủ.

## SqlSugar Db First Rules
- **SQLSUGAR DB FIRST SPECIFICATION (Quy trình DB First 2.1.5)**:
  1. Cấu hình chuỗi kết nối mới trong file `Database.json` với `ConfigId` tương ứng. Tắt tất cả các cài đặt khởi tạo tự động trong `DbSettings` (`Enable... = false`).
  2. **Bắt buộc thuộc tính `[Tenant(ConfigId)]`**: Tất cả các Entity sinh ra hoặc tạo tay cho DB First BẮT BUỘC phải có thuộc tính `[Tenant("ConfigId_Tuong_Ung")]` trên đầu class Entity.
  3. **Auto Generate DbFirst Class File**: Khi dùng tính năng tự động sinh Entity của SqlSugar trong `Extensions/AddxxxInfrastructure`:
     - Phải cấu hình template chèn `[Tenant("ConfigId")]` vào class tự động:
       `_baseRepository.Context.DbFirst.SettingClassDescriptionTemplate(it => it + "\r\n [Tenant(\"" + ConfigId + "\")]").IsCreateAttribute().StringNullable().CreateClassFile(directoryPath, nameSpace);`
     - **Chỉ mở mã sinh tự động 1 lần đầu** khi cần khởi tạo số lượng lớn hoặc có thay đổi cấu trúc DB nhiều. Khi cấu trúc DB ổn định hoặc chỉ thay đổi ít, tiến hành đóng/comment đoạn mã DbFirst lại và cập nhật thủ công trực tiếp trên class Entity.
  4. Chú ý kiểm tra lại đường dẫn tuyệt đối `directoryPath` tới thư mục `Core/Entities/DbFirst/` của module để tương thích môi trường máy cá nhân.
  5. Khi gặp lỗi IDE không bắt được Unit Test hoặc báo thiếu tham chiếu (Missing reference/assembly):
     - Kiểm tra xem project (nhất là `*.Tests.csproj`) đã được add vào file `.sln` chưa. Chạy lệnh `dotnet sln <sln-file> add <csproj-file>` để tự động fix.
     - Kiểm tra xUnit lifecycle: Dùng `IClassFixture` khi có khởi tạo DB/Host nặng để tránh đụng độ `DROP TABLE` khi test song song.