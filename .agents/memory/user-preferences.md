---
type: user_preferences
created: 2026-07-21
updated: 2026-07-22
---

# User Preferences

## Workflow & Execution Rules
- **DO NOT auto-run `dotnet build`**: The AI must NOT automatically execute `dotnet build` or compilation commands after code changes unless explicitly requested by the user.
- **DO NOT auto-run `dotnet test`**: AI KHÔNG ĐƯỢC tự ý chạy `dotnet test` sau mỗi lần sửa code trừ khi người dùng yêu cầu trực tiếp.
- **DO NOT auto-commit or push Git**: The AI must NOT automatically execute `git add`, `git commit`, or `git push` after completing code changes under any circumstances unless explicitly requested by the user.
- **DO NOT auto-run dev server / app**: The AI must NOT automatically run `dotnet run`, dev servers, or start the web app in background after building code unless explicitly requested by the user.
- **PRESERVE USER MANUAL EDITS & PREFERENCES (Tôn trọng code sửa tay & ý định người dùng)**: Khi người dùng đã chỉ định cách viết (VD: dùng `while (reader.Read())` đồng bộ) hoặc tự sửa tay/bỏ bớt điều kiện, AI KHÔNG ĐƯỢC TỰ Ý hoàn tác (revert) hoặc sửa ngược lại về cách viết cũ trong các lần refactor tiếp theo.
- **SINGLE-STATEMENT IF FORMATTING**: Đối với câu lệnh `if` 1 dòng không ngoặc `{}` (Single-Statement If), BẮT BUỘC ngắt dòng và thụt lề cho câu lệnh thực thi (VD: `if (condition)\n    statement;`), tuyệt đối không viết trên cùng 1 dòng.
- **DO NOT USE `#region`**: KHÔNG tự ý chèn thẻ `#region` hoặc `#endregion` vào code C# trừ khi người dùng yêu cầu. Giữ biến/field nguyên bản và sạch sẽ.
- **NO MANUAL VALIDATE/MAP HELPER METHODS IN COMMAND HANDLERS**: KHÔNG tự viết các hàm trợ giúp thủ công như `ValidateInput` hoặc `MapToOutput` bên trong CommandHandler. BẮT BUỘC sử dụng FluentValidation (`AbstractValidator<T>`) cho toàn bộ logic kiểm tra dữ liệu và sử dụng Mapster (`entity.Adapt<T>()` / `ShareDataMapsterRegister`) cho toàn bộ logic ánh xạ DTO Output.
- **PAGED LIST PROJECTION DIRECT RETURN**: Trong QueryHandler phân trang, khi dùng `.Select(x => new TOutput { ... }, true)` hoặc truy vấn trực tiếp ra `SqlSugarPagedList<TOutput>`, BẮT BUỘC trả thẳng đối tượng phân trang (VD: `return paged;` hoặc `return await query.ToPagedListAsync(...)`), KHÔNG bọc qua `.Adapt<SqlSugarPagedList<TOutput>>()` nữa.
- **REPOSITORY NAMING CONVENTION**: Đặt tên biến Repository trong CommandHandler / QueryHandler theo chuẩn prefix `_rsp{EntityName}` (VD: `SqlSugarRepository<EshPartner> _rspEshPartner;`, `SqlSugarRepository<EshDataSource> _rspEshDataSource;`). KHÔNG dùng các tên chung chung như `_repository`, `_repo`, hay `_baseRepository`.
- **MAPSTER MAPPER LOCATION CONVENTION**: KHÔNG tạo thư mục `Mappings` riêng rẽ để chứa file Register tập trung. Tất cả cấu hình ánh xạ Mapster (`IRegister`) BẮT BUỘC viết trực tiếp bên trong file DTO Output tương ứng (VD: `EshPartnerOutput.cs` chứa `public class EshPartnerMapper : IRegister`, `MenuOutput.cs` / `SysMenuOutput.cs` chứa mapper tương ứng).
