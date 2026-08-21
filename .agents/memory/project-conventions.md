---
type: project
created: 2026-05-25
updated: 2026-07-12
---

# Project Conventions

## Git Workflow
- Always create a new dedicated branch for major code changes.
- Branch name format should follow: `feature/[task-slug]` or `fix/[bug-slug]`.

## Supported AI platforms (AG Kit)
- AG Kit **only supports Gemini CLI and Google Antigravity**.
- Do not claim compatibility with Claude Code, Cursor, Copilot, Windsurf, or other assistants unless the user explicitly expands scope.
- Copy on the website, docs, FAQ, README, and marketing should describe AG Kit as a toolkit for Gemini CLI / Antigravity-style agent setups.

## C# Code & Payload Rules
- **DTO vs Anonymous Objects**: Khi dữ liệu CÓ XỬ LÝ logic nội bộ thì PHẢI tạo **DTO**. Nếu dữ liệu CHỈ MAP để gửi đi (bên khác xử lý) thì PHẢI dùng **Anonymous Objects** (hoặc `Dictionary`).
- **Namespaces**: Không dùng Full Namespace dài dòng trong thân class (VD: `ShareDataWorker.Core.Dto...`). Bắt buộc khai báo `using` ở đầu file.
- **Null Reference (CS8601)**: Tuyệt đối chú ý gán giá trị dự phòng (VD: `?? string.Empty`) để dập tắt cảnh báo CS8601 khi gán string? cho string.
- **Async Method Naming**: Tuyệt đối không thêm hậu tố `Async` vào tên các phương thức bất đồng bộ (VD: Dùng `HandleIncomingConnection` thay vì `HandleIncomingConnectionAsync`).
- **Primary Constructor ([IDE0290](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0290))**: Luôn ưu tiên áp dụng C# Primary Constructors (`public class Service(ILogger<Service> logger) : IService`) thay vì khai báo constructor truyền thống kèm backing fields thủ công.
- **Structured Logging ([CA1873](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1873))**: Luôn dùng message templates cho `ILogger` (VD: `_logger.LogInformation("Done {Id}", id)`), TUYỆT ĐỐI KHÔNG dùng string interpolation `$""` hoặc tính toán nặng (LINQ, `string.Join`) trong arguments của log khi chưa kiểm tra `_logger.IsEnabled(...)`.
- **Remove Unused Using Directives ([IDE0005](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0005))**: Luôn kiểm tra và xóa bỏ `using` thừa, không dùng hoặc bị trùng lặp với `GlobalUsings.cs` sau mỗi lần sửa code.
- **Command & Validator Integration Testing**: Khi viết kiểm thử tích hợp cho Command (Delete, BatchDelete, Update, Add) có Validator (FluentValidation), BẮT BUỘC phải kiểm tra (validate) Negative Rule trước (ví dụ: kiểm tra payload có ID rỗng/null và `Assert.False(invalidResult.IsValid)`) trước khi thực hiện bước Arrange insert dữ liệu vào CSDL (`_db.Insertable`) và gọi Command qua MessageBus.
- **SqlSugar CodeFirst Reflection (`inherit: false`)**: Khi quét các class thực thể để tạo bảng qua CodeFirst (`InitTables`), BẮT BUỘC sử dụng `t.IsDefined(typeof(SugarTable), inherit: false)` để ngăn ngừa việc các DTO kế thừa từ Entity (ví dụ: `AddXxxInput : EntityBase`, `PageXxxOutput : EntityBase`) bị CodeFirst nhận diện nhầm và tự tạo bảng CSDL cho DTO.
- **ASP.NET Core Configuration (`GetConnectionString`)**: Luôn ưu tiên sử dụng `config.GetConnectionString("Default")` theo chuẩn Native ASP.NET Core thay vì truy vấn các key phân cấp thô `config["DbConnection:ConnectionConfigs:0:ConnectionString"]`.
