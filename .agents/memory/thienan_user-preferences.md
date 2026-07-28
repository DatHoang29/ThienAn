---
type: user_preferences
created: 2026-07-21
updated: 2026-07-22
---

# Thiên An User Preferences

## Workflow & Execution Rules
- **DO NOT auto-run `dotnet build`**: The AI must NOT automatically execute `dotnet build` or compilation commands after code changes unless explicitly requested by the user.
- **DO NOT auto-run `dotnet test`**: AI KHÔNG ĐƯỢC tự ý chạy `dotnet test` sau mỗi lần sửa code trừ khi người dùng yêu cầu trực tiếp.
- **DO NOT auto-commit or push Git**: AI KHÔNG ĐƯỢC tự ý thực hiện `git add .` hoặc `git commit` full thư mục. Chỉ khi người dùng yêu cầu commit và chính người dùng tự chọn/đưa file vào Staged Changes (`git add` thủ công), AI mới thực hiện commit đúng các file trong Staged Changes đó.
- **DESCRIPTIVE COMMIT MESSAGE CONVENTION**: Nội dung commit message BẮT BUỘC phải mô tả rõ ràng, chính xác theo tính năng thực tế đã áp dụng (dùng chuẩn Semantic Commit như `feat(module): description`, `refactor(module): description`, `fix(module): description`...). KHÔNG dùng các chuỗi chung chung ngắn ngủi như "up", "test".
- **DO NOT auto-run dev server / app**: The AI must NOT automatically run `dotnet run`, dev servers, or start the web app in background after building code unless explicitly requested by the user.
- **PRESERVE USER MANUAL EDITS & PREFERENCES (Tôn trọng code sửa tay & ý định người dùng)**: Khi người dùng đã chỉ định cách viết (VD: dùng `while (reader.Read())` đồng bộ) hoặc tự sửa tay/bỏ bớt điều kiện, AI KHÔNG ĐƯỢC TỰ Ý hoàn tác (revert) hoặc sửa ngược lại về cách viết cũ trong các lần refactor tiếp theo.
- **SINGLE-STATEMENT IF FORMATTING**: Đối với câu lệnh `if` 1 dòng không ngoặc `{}` (Single-Statement If), BẮT BUỘC ngắt dòng và thụt lề cho câu lệnh thực thi (VD: `if (condition)\n    statement;`), tuyệt đối không viết trên cùng 1 dòng.
- **DO NOT USE `#region`**: KHÔNG tự ý chèn thẻ `#region` hoặc `#endregion` vào code C# trừ khi người dùng yêu cầu. Giữ biến/field nguyên bản và sạch sẽ.
- **NO MANUAL VALIDATE/MAP HELPER METHODS IN COMMAND HANDLERS**: KHÔNG tự viết các hàm trợ giúp thủ công như `ValidateInput` hoặc `MapToOutput` bên trong CommandHandler. BẮT BUỘC sử dụng FluentValidation (`AbstractValidator<T>`) cho toàn bộ logic kiểm tra dữ liệu và sử dụng Mapster (`entity.Adapt<T>()` / `ShareDataMapsterRegister`) cho toàn bộ logic ánh xạ DTO Output.
- **PAGED LIST PROJECTION DIRECT RETURN**: Trong QueryHandler phân trang, khi dùng `.Select(x => new TOutput { ... }, true)` hoặc truy vấn trực tiếp ra `SqlSugarPagedList<TOutput>`, BẮT BUỘC trả thẳng đối tượng phân trang (VD: `return paged;` hoặc `return await query.ToPagedListAsync(...)`), KHÔNG bọc qua `.Adapt<SqlSugarPagedList<TOutput>>()` nữa.
- **REPOSITORY NAMING CONVENTION**: Đặt tên biến Repository trong CommandHandler / QueryHandler theo chuẩn prefix `_rsp{EntityName}` (VD: `SqlSugarRepository<EshPartner> _rspEshPartner;`, `SqlSugarRepository<EshDataSource> _rspEshDataSource;`). KHÔNG dùng các tên chung chung như `_repository`, `_repo`, hay `_baseRepository`.
- **MAPSTER MAPPER LOCATION CONVENTION**: KHÔNG tạo thư mục `Mappings` riêng rẽ để chứa file Register tập trung. Tất cả cấu hình ánh xạ Mapster (`IRegister`) BẮT BUỘC viết trực tiếp bên trong file DTO Output tương ứng (VD: `EshPartnerOutput.cs` chứa `public class EshPartnerMapper : IRegister`, `MenuOutput.cs` / `SysMenuOutput.cs` chứa mapper tương ứng).
- **NO EXTRA/DESIGN-TIME CLASSES IN VALIDATORS**: Validator chỉ khai báo duy nhất 1 Constructor nhận `IStringLocalizer lz` (hoặc `localizer`), KHÔNG tự ý chèn các class phụ/mock như `DesignTimeLocalizer` hay constructor không tham số `: this(...)`.
- **NO DATAANNOTATIONS IN DTO (Strict FluentValidation Only)**: KHÔNG dùng các thuộc tính DataAnnotation validation (như `[Required(ErrorMessage = "...")]`, `[Range(...)]`, `[StringLength(...)]`...) trong các class DTO. Tất cả logic kiểm tra dữ liệu và thông báo lỗi đa ngôn ngữ BẮT BUỘC thực hiện 100% qua `FluentValidation` (`AbstractValidator<T>`) kết hợp `IStringLocalizer lz` và `BaseMsg`.
- **API CONTROLLER SUMMARY CONVENTION**: Trên mỗi phương thức Action trong Controller, comment XML Doc `/// <summary>` BẮT BUỘC mô tả rõ ràng, tự nhiên ý nghĩa và chức năng thực tế của hàm (VD: `/// <summary>\n/// Lấy danh sách cảnh báo & lỗi (phân trang)\n/// </summary>`). Tuyệt đối KHÔNG chèn mã prefix/số thứ tự rườm rà (như L1., E2., DS3...). Thẻ `[DisplayName("...")]` giữ nguyên tên hiển thị chuẩn.
- **NO XML COMMENTS IN INTERNAL MODULE LAYERS (Commands/Queries/Dto/Validators)**: KHÔNG viết comment XML Doc (`/// <summary>...`) rườm rà dư thừa trong các file thuộc thư mục `Commands/`, `Queries/`, `Dto/`, `Validators/`. Giữ code ngắn gọn, sạch sẽ. XML Doc comment chỉ duy trì trên các Action công khai trong `Controller` để phục vụ Swagger / API Docs.
- **DISTINGUISH REFERENCE FROM ACTION**: Khi người dùng yêu cầu "tham khảo", "xem thử", "giải thích" hoặc hỏi ý kiến, AI BẮT BUỘC phải phân tích và trả lời thảo luận trước, KHÔNG ĐƯỢC tự ý nhảy vào áp dụng hoặc thêm/sửa code khi chưa có xác nhận từ người dùng.
- **SINGLE-PROJECT MODULE ARCHITECTURE (Cấu trúc Modules 2.1.3)**: Tất cả các module đều BẮT BUỘC phải theo cấu trúc project duy nhất (VD: `Modules.ShareData.csproj`), không tách làm project `.Core` phụ. Thư mục project module bao gồm:
  - `Controllers`: Chứa xử lý các API truy cập từ bên ngoài.
  - `Core`: Chứa các phần căn bản của module, bao gồm Abstraction, Entity, Exception.
  - `[Bắt buộc] Extensions`: Chứa các class khai báo module và các thư viện tích hợp. Thư mục này bắt buộc phải có để khai báo sử dụng.
  - `Infrastructure`: Chứa các phần xử lý liên quan tới CSDL (`BaseRepository.cs`), xử lý dữ liệu (`Services/`).
  - `GlobalUsings.cs`
  *(Nếu module không có phần nào thì có thể bỏ qua phần đó, nhưng tuyệt đối không tự tạo project phụ rải rác).*
- **MODULE LOCALIZATION SPECIFICATION (Dịch thuật 2.1.4)**:
  - Tất cả các Module có sử dụng dịch thuật BẮT BUỘC tạo file `Core/Exceptions/BaseMsg.cs` kế thừa `BaseLocaleManager` (từ `Shared.DTO.Constants.Localization`).
  - Trong `BaseMsg`, tạo các class đại diện cho từng Chức năng/Entity (VD: `EshPartner`, `EshDataSource`...).
  - Trong mỗi class chức năng, chia thành các class con chứa hằng số dịch thuật: `Validation`, `Message`, `Exception`, `Entity` (group action).
  - **Vị trí thư mục Resources**: Thư mục `Resources` nằm ngang hàng với `Controllers`, `Core`, `Extensions`, `Infrastructure` trong root project của Module (VD: `Modules.ShareData/Resources/vi-VN.json`). KHÔNG đặt bên trong thư mục `Controllers`. Dịch thuật được cập nhật đồng bộ vào `src/TAC_WebAPI/Resources/` để hệ thống load đầy đủ.
- **SQLSUGAR DB FIRST SPECIFICATION (Quy trình DB First 2.1.5)**:
  1. Cấu hình chuỗi kết nối mới trong file `Database.json` với `ConfigId` tương ứng. Tắt tất cả các cài đặt khởi tạo tự động trong `DbSettings` (`Enable... = false`).
  2. **Bắt buộc thuộc tính `[Tenant(ConfigId)]`**: Tất cả các Entity sinh ra hoặc tạo tay cho DB First BẮT BUỘC phải có thuộc tính `[Tenant("ConfigId_Tuong_Ung")]` trên đầu class Entity.
  3. **Auto Generate DbFirst Class File**: Khi dùng tính năng tự động sinh Entity của SqlSugar trong `Extensions/AddxxxInfrastructure`:
     - Phải cấu hình template chèn `[Tenant("ConfigId")]` vào class tự động:
       `_baseRepository.Context.DbFirst.SettingClassDescriptionTemplate(it => it + "\r\n [Tenant(\"" + ConfigId + "\")]").IsCreateAttribute().StringNullable().CreateClassFile(directoryPath, nameSpace);`
     - **Chỉ mở mã sinh tự động 1 lần đầu** khi cần khởi tạo số lượng lớn hoặc có thay đổi cấu trúc DB nhiều. Khi cấu trúc DB ổn định hoặc chỉ thay đổi ít, tiến hành đóng/comment đoạn mã DbFirst lại và cập nhật thủ công trực tiếp trên class Entity.
  4. Chú ý kiểm tra lại đường dẫn tuyệt đối `directoryPath` tới thư mục `Core/Entities/DbFirst/` của module để tương thích môi trường máy cá nhân.
