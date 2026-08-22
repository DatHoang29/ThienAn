---
trigger: always_on
---

# Universal Rules (TIER 0) - AG Kit

> Always-active rules that apply to every request, regardless of domain.

---

## 🌐 Language Handling

When user's prompt is NOT in English:

1. **Internally translate** for better comprehension
2. **Respond in user's language** - match their communication
3. **Code comments/variables** remain in English

---

## 🧹 Clean Code (Global Mandatory)

**ALL code MUST follow `@[skills/clean-code]` rules. No exceptions.**

- **Code**: Concise, direct, no over-engineering. Self-documenting.
- **Testing**: Mandatory. Pyramid (Unit > Int > E2E) + AAA Pattern.
- **Performance**: Measure first. Adhere to current Core Web Vitals standards.
- **No Hardcoded Magic Strings**: NEVER write hardcoded string literals (e.g. status codes, state names) directly in business logic queries or conditional logic. ALWAYS define and use strongly-typed Enums or Constants (e.g., `ShareDataEnum.IncidentState`).
- **Formatting (Single-Statement `if` Without Braces)**: Đối với câu lệnh `if` chỉ chứa 1 dòng thực thi (dù điều kiện `if` nằm trên 1 dòng hay nhiều dòng `&&`/`||`, ví dụ: `if (!string.IsNullOrWhiteSpace(command.SourceId) && !await _vwSourceRep.IsAnyAsync(...))\n    throw Oops.Oh(BaseLocaleManager.BaseException.NotExist, BaseMsg.Vw.Entity.SourceId);` hoặc `if (condition)\n    return;`), BẮT BUỘC ngắt dòng và thụt lề cho câu lệnh thực thi. TUYỆT ĐỐI KHÔNG viết inline trên cùng 1 dòng (`if (condition) return;`) và TUYỆT ĐỐI KHÔNG TỰ Ý THÊM cặp dấu ngoặc nhọn `{}` (`if (condition) { ... }`) khi code hiện hữu đang viết theo chuẩn single-statement không có ngoặc nhọn.
- **Object Initializer Formatting**: Object initializers with multiple properties (e.g., `new TmsEquipment { ID = eqId, Code = "...", ... }`) MUST ALWAYS break lines and format properties on separate indented lines (one property per line). NEVER write multi-property object initializations inline on a single horizontal line.
- **Multi-Condition LINQ Formatting**: LINQ/SqlSugar queries with multiple conditions (e.g. `.Where(s => s.IsDelete == null && s.Direction == ... && s.Mode != ...)` MUST ALWAYS break lines per condition — either by splitting into separate chained `.Where(...)` calls (one condition per `.Where`) or breaking each `&&` / `||` clause onto separate indented lines. NEVER write long multi-condition logic on a single horizontal line.
- **No Duplicate XML Comments**: NEVER generate stacked or duplicate `/// <summary>` XML comment blocks on any class, method, or property. Each symbol MUST have at most ONE concise `<summary>` block. Always update existing docstrings in-place.
- **Async Method Naming**: Do NOT append the `Async` suffix to asynchronous method names (e.g. use `ProcessBatchSubscriptions` instead of `ProcessBatchSubscriptionsAsync`), as the method return type (`Task` / `Task<T>`) already explicitly indicates asynchrony.
- **Dependency Injection Naming**: ALWAYS name injected dependencies in constructors using camelCase (e.g., `IFileExportService fileExportService`). NEVER use PascalCase (e.g., `IFileExportService FileExportService`) for constructor parameters or injected fields.
- **C# / .NET CA2263**: ALWAYS prefer generic `Enum.IsDefined<TEnum>(value)` (or `Enum.IsDefined(enumValue)` in .NET 7+) over the non-generic `Enum.IsDefined(typeof(TEnum), value)` to prevent unnecessary object boxing and reflection overhead.
- **Primary Constructor ([IDE0290](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0290))**: Chỉ áp dụng Primary Constructor khi **VIẾT CLASS MỚI** (e.g., `public class MyService(ILogger<MyService> logger, IConfiguration configuration) : IMyService`). Đối với **CLASS CŨ ĐÃ TỒN TẠI** đang dùng explicit constructor truyền thống → KHÔNG sửa, KHÔNG refactor sang primary constructor — giữ nguyên style cũ để tránh diff không cần thiết.
- **Structured Logging ([CA1873](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1873))**: ALWAYS use structured logging message templates (e.g. `_logger.LogInformation("Processing {Id} for {Partner}", id, partner)`) instead of string interpolation (e.g. `_logger.LogInformation($"Processing {id} for {partner}")`) or eagerly evaluating expensive expressions (like `string.Join(...)`, `.Count()`, LINQ) in logging arguments. Check `_logger.IsEnabled(...)` before preparing expensive log data to avoid unnecessary allocations and CPU overhead when logging is disabled.
- **Test File & Class Naming 1-1 Correspondence ([Mandatory])**: Tên file test và tên class test BẮT BUỘC phải đồng nhất 1-1 với tên class/controller/service bên mã nguồn gốc và luôn kết thúc bằng hậu tố `Tests` (hoặc `Test`) (ví dụ: `VwDeviceClient` → `VwDeviceClientTests.cs`, `VwDeviceService` → `VwDeviceServiceTests.cs`, `VwSchedule` → `VwScheduleTests.cs`, `VwEventRule` → `VwEventRuleTests.cs`, `VwScreen` → `VwScreenTests.cs`). TUYỆT ĐỐI KHÔNG đặt tên gộp lạ (như `VwAutomationTests`, `VwServicesTests`, `VwScreenAndTopologyTests`) mà phải phân tách và đặt tên khớp 1-1.
- **No Separate Utils Test Folders / Service-Level Testing Focus ([Mandatory])**: TUYỆT ĐỐI KHÔNG tạo thư mục test `Utils` / `Util` riêng biệt hay viết unit test cô lập cho các class tiện ích (Utils/Helpers). Chỉ cần tập trung viết test ở tầng **Service / Handler / Controller** chính. Nếu logic nghiệp vụ có liên quan đến Util/Helper thì các bài test tại tầng Service bao phủ và kiểm thử các tiện ích đó trong luồng thực thi thực tế là đủ.
- **Test Class & Helper Naming Suffix**: ALWAYS use `Test` or `Tests` as a SUFFIX for all test classes, test helper/mock/stub classes, and test methods (e.g. `StringLocalizerTest`, `VwControllerTests`, `VwControllerCommand_AddVwController_InsertsRecord_Test`). NEVER prefix with `Test` (e.g. do NOT use `TestStringLocalizer` or `TestVwController`).
- **Auto-Cleanup Completed Prompt & Plan Files [Mandatory]**: Khi thực thi xong bất kỳ nhiệm vụ nào được giao qua file prompt thực thi hoặc plan tạm (ví dụ: các file `*-prompt-*.md`, `07-prompt-xoa-nats.md`, `{task-slug}.md`, `ide0290-primary.md`, v.v. trong thư mục `DocBusinessThienAn/...` hoặc thư mục gốc), AI **BẮT BUỘC TỰ ĐỘNG XÓA** file prompt/plan đó ngay sau khi hoàn thành task để tránh rác workspace và tài liệu dư thừa (tuyệt đối không xóa các tài liệu đặc tả/nghiệp vụ tham khảo gốc).
- **Command & Validator Integration Testing**: When writing integration tests for Commands (Delete, BatchDelete, Update, Add) that have a FluentValidation Validator, ALWAYS test the negative validation rule first (e.g. assert that an invalid/null ID payload returns `Assert.False(invalidResult.IsValid)`) BEFORE performing the Arrange insert into the database (`_db.Insertable`) and invoking the command.
- **Class Member & Helper Ordering (Private Helpers at Bottom [Mandatory])**: Trong tất cả các class/service/handler C#, toàn bộ các phương thức `private` (như private helpers, private async methods, `AcquireSceneLock`, `CheckReferenceAsync`...) và các nested helper classes/structs (như `ActionDisposable`) BẮT BUỘC phải được đặt ở **CUỐI CÙNG CỦA CLASS / FILE**, sau toàn bộ các phương thức `public` (`HandleAsync`, `Get...`, `Execute...`). TUYỆT ĐỐI KHÔNG đặt các hàm private xen kẽ ở đầu class hoặc giữa các public handler methods.
- **Service Lifetime for IVwISAPIDeviceService (Singleton [Mandatory])**: `IVwISAPIDeviceService` (và `VwISAPIDeviceService`) BẮT BUỘC luôn được đăng ký ở vòng đời **Singleton** (`services.AddSingleton<IVwISAPIDeviceService, VwISAPIDeviceService>()` hoặc triển khai `ISingleton`) trong DI container (cả trong `ServiceCollectionExtensions.cs` và `Host.cs`) để duy trì trạng thái kết nối và bộ đệm điều khiển thiết bị xuyên suốt ứng dụng.
- **Vue SFC Section Ordering ([Mandatory])**: Trong tất cả các file component Vue.js (`.vue`), thứ tự các khối BẮT BUỘC phải tuân thủ chuẩn:
  1. `<script setup lang="ts">` (hoặc `<script>`) **đặt ở ĐẦU TIÊN**.
  2. `<template>` (Giao diện UI / HTML) **đặt ở THỨ HAI**.
  3. `<style scoped>` (CSS / SCSS) **đặt ở CUỐI CÙNG**.
  TUYỆT ĐỐI KHÔNG đặt `<template>` trước `<script>`.
- **Vue `<script setup>` Internal Structure ([Mandatory])**: Nội dung bên trong khối `<script setup>` BẮT BUỘC phải được sắp xếp theo thứ tự phân tầng mạch lạc:
  1. **Imports** (Thư viện ngoài, component con, composables/hooks, types/interfaces) luôn ở đầu dòng.
  2. **Props / Emits / Models** (`defineProps`, `defineEmits`, `defineModel`).
  3. **Reactive State & Stores** (`ref`, `reactive`, Pinia store instances).
  4. **Computed & Watchers** (`computed`, `watch`, `watchEffect`).
  5. **Lifecycle Hooks** (`onMounted`, `onActivated`, `onUnmounted`...).
  6. **Methods & Event Handlers** (Các hàm xử lý sự kiện, hàm gọi API, logic nghiệp vụ).
  7. **Expose** (`defineExpose` nếu có).


---

## 🔒 SQL & Module Isolation Scope (Mandatory Rule)

- **Strict Module Scope**: All SQL scripts (DDL & DML) generated or updated for a module MUST ONLY target tables within that module's official entity scope (e.g., for `ShareData` module: `EshPartner`, `EshDataSource`, `EshMappingProfile`, `EshFieldMapping`, `EshSubscription`, `EshExportLog`, `EshSystemLog`, `EshEventSource`).
- **FORBIDDEN Outside Operations**: NEVER perform `CREATE`, `ALTER`, `DROP`, `INSERT`, `UPDATE`, or `DELETE` operations on tables owned by other modules (such as `TmsTrafficData`, `TmsWeather`, `TmsIncident`, `TollTransactionOut`...). External tables belong strictly to their host modules and must never be created, altered, or mutated by another module's scripts.

---

## 🛑 Strict Manual SQL Execution Rule (Mandatory Rule)

- **ABSOLUTELY NO Auto-Executing Database Mutations**: When creating or updating SQL scripts (`.SQL`) or database configurations, ONLY write or modify files on disk. 
- **FORBIDDEN Auto-Mutations**: NEVER automatically run or execute any DDL/DML operations (`INSERT`, `UPDATE`, `DELETE`, `ALTER`, `DROP`, `TRUNCATE`) against any database (remote or local, via scripts, C# code, or tools) without explicit prior request from the user.
- **User Review First**: Always present the generated SQL script file to the user for inspection so they can manually review and execute it themselves.

---

## 🔒 MCP Database Read-Only Rule (Mandatory Rule)

- **Priority 1 - Mandatory MCP for Database Operations**: Khi cần tra cứu, kiểm tra schema, đọc dữ liệu CSDL (Dev, Staging, Test), AI BẮT BUỘC đọc file cấu hình `@[.agents/mcp_config.json]` và khởi tạo Subagent với `enable_mcp_tools: true` để gọi trực tiếp các MCP server (`mssql_staging`, `mssql_dev`, `mssql_test`).
- **MCP Failure Reporting & Fallback Protocol**: 
  - Nếu MCP server gặp sự cố (không kết nối được, lỗi runtime, hoặc thiếu tool), AI **BẮT BUỘC BÁO CÁO RÕ LỖI MCP CHO NGƯỜI DÙNG BIẾT TRƯỚC**.
  - **CHỈ KHI** không còn cách nào khác qua MCP và được người dùng đồng ý, AI mới được phép sử dụng script PowerShell/Shell để truy vấn CSDL ở chế độ strictly READ-ONLY.
- **Strict MCP Read-Only Mode**: ALL database interactions executed via MCP servers (`mssql_dev`, `mssql_staging`, `mssql_test`, etc.) MUST be strictly READ-ONLY. Allowed tools & queries are limited to inspecting schemas and reading data (`SELECT` queries, `list_tables`, `describe_table`, `sample_data`, `get_relationships`, etc.).
- **ABSOLUTELY FORBIDDEN Database Mutations**: NEVER execute any `INSERT`, `UPDATE`, `DELETE`, `DROP`, `ALTER`, `TRUNCATE`, `CREATE`, or stored procedure execution mutating state via MCP or scripts under any circumstances without explicit user request.

---

## 🛑 Strict Local Database Rule for Testing (`dotnet test` [Mandatory Rule])

- **Mandatory Local Connection for Tests**: Khi thực thi lệnh `dotnet test` (hoặc bất kỳ kịch bản unit/integration test nào), **TẤT CẢ** các chuỗi kết nối (Connection Strings) — bao gồm toàn bộ cơ sở dữ liệu quan hệ (RDBMS: SQL Server, PostgreSQL, MySQL...) và NoSQL / Caching (Redis, v.v.) — **BẮT BUỘC PHẢI LÀ MÔI TRƯỜNG LOCAL** (`localhost`, `127.0.0.1`, `(localdb)`, `.`, `local` container).
- **CANCEL & REPORT ON REMOTE DETECTED**: Trước khi chạy `dotnet test`, nếu phát hiện bất kỳ chuỗi kết nối nào trong `appsettings.json`, `appsettings.Local.json`, `appsettings.Test.json`, `Host.cs`, hoặc cấu hình test trỏ tới máy chủ từ xa / IP remote (ví dụ: `10.10.8.30`, domain staging/prod/dev remote...), AI **BẮT BUỘC PHẢI HỦY (CANCEL) NGAY LẬP TỨC VIỆC CHẠY TEST VÀ BÁO CÁO LẠI CHO NGƯỜI DÙNG**.
- **FORBIDDEN Remote Database Testing**: TUYỆT ĐỐI KHÔNG ĐƯỢC PHÉP chạy test khi chuỗi kết nối tới RDBMS hoặc Redis không phải là local, để ngăn ngừa hoàn toàn nguy cơ làm sai lệch, rò rỉ hoặc xoá/thao tác dữ liệu trên hệ thống server từ xa.



