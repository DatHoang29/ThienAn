---
type: project
created: 2026-05-25
updated: 2026-07-23
---

# Project Conventions

## Git Workflow
- Always create a new dedicated branch for major code changes.
- Branch name format should follow: `feature/[task-slug]` or `fix/[bug-slug]`.

## Docker & Infrastructure Rules
- **Docker SQL Server on macOS**: ALWAYS use `mcr.microsoft.com/azure-sql-edge:latest` for SQL Server containers on macOS (Apple Silicon M1/M2/M3/M4). NEVER use `mcr.microsoft.com/mssql/server:2022-latest` as x86_64 emulation under QEMU causes immediate memory crashes (`Invalid mapping of address`).

## Code Style & Class Header Comments
- **Class Header XML Comments Required**: Every new or updated class MUST include an XML summary header block formatted as follows (Author is always `Đạt`):
```csharp
/// <summary>
/// [Description / Table Name]
/// Author: Đạt
/// Created date: [dd/MM/yyyy]
/// </summary>
- **Single-Statement If Braces**: Đối với câu lệnh `if` chỉ chứa 1 dòng thực thi (VD: `if (isCodeExists) throw Oops.Oh(...);`), KHÔNG DÙNG dấu ngoặc nhọn `{}`.
- **Using Directives Instead of Inline Namespaces**: BẮT BUỘC dùng `using` directive ở đầu file (VD: `using Modules.ShareData.Core.Entities;`) để gọi tên class ngắn gọn (VD: `EshPartner`) thay vì gõ namespace dài inline trong code (VD: `Core.Entities.EshPartner`).





## Entity Conventions & Length Constants
- **Entity Inheritance**: All Entity classes MUST inherit from `EntityTenant` (from `Shared.Core.Domain`).
- **EntityConst for Column Lengths**: BẮT BUỘC dùng các hằng số `EntityConst` (từ namespace `Shared.DTO.Constants.Application`, VD: `EntityConst.Length32`, `EntityConst.Length64`, `EntityConst.Length128`, `EntityConst.Length256`, `EntityConst.Length512`, `EntityConst.KeyFieldLength`) cho tất cả attribute `[SugarColumn(Length = ...)]` và `[MaxLength(...)]` trong Entity và DTO. KHÔNG ĐƯỢC dùng số hardcode trực tiếp (như `Length = 32`).
- **Shared Enums for ShareData**: Tất cả các Enum dùng chung cho Module ShareData (ESHARE V1) được định nghĩa trong `Modules.ShareData.Core.Enums.EshEnums` (`DataSourceKind`, `MappingDirection`, `DatatypeIdEnum`, `SubDirection`, `SubMode`, `SubState`, `PublishFormat`, `PubState`, `ExportStatus`, `PartnerStatus`, `ProtocolProfile`).
- **Validators Directory Conventions**: Mỗi feature/controller trong module (VD: `Controllers/EshDataSource/`, `Controllers/EshPartner/`) BẮT BUỘC có thư mục `Validators/` chứa các class FluentValidation (`Add[Entity]Validator`, `Update[Entity]Validator`, `Delete[Entity]Validator` kế thừa `AbstractValidator<T>`) tương tự cấu trúc chuẩn trong `Modules.Samplev2/Controllers/Category/Validators/`.





## Minimal Diff & Maintenance Rules
- **DO NOT auto-run `dotnet build`**: KHÔNG tự động chạy lệnh `dotnet build` sau khi tạo/sửa code trừ khi người dùng yêu cầu trực tiếp.
- **Minimal Diff Principle**: ONLY modify files and code strictly necessary to accomplish the requested feature or bugfix. NEVER auto-upgrade package versions (`PackageReference` in `.csproj`), edit unrelated shared projects (`Shared.Reference`), or modify existing files outside the task scope unless explicitly instructed by the user.




