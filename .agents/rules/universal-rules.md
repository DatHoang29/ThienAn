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
- **Formatting**: Single-statement `if` MUST ALWAYS break line and indent (e.g. `if (condition)\n    return;`). NEVER write inline on the same line (`if (condition) return;`).
- **Object Initializer Formatting**: Object initializers with multiple properties (e.g., `new TmsEquipment { ID = eqId, Code = "...", ... }`) MUST ALWAYS break lines and format properties on separate indented lines (one property per line). NEVER write multi-property object initializations inline on a single horizontal line.
- **Multi-Condition LINQ Formatting**: LINQ/SqlSugar queries with multiple conditions (e.g. `.Where(s => s.IsDelete == null && s.Direction == ... && s.Mode != ...)` MUST ALWAYS break lines per condition — either by splitting into separate chained `.Where(...)` calls (one condition per `.Where`) or breaking each `&&` / `||` clause onto separate indented lines. NEVER write long multi-condition logic on a single horizontal line.
- **No Duplicate XML Comments**: NEVER generate stacked or duplicate `/// <summary>` XML comment blocks on any class, method, or property. Each symbol MUST have at most ONE concise `<summary>` block. Always update existing docstrings in-place.
- **Async Method Naming**: Do NOT append the `Async` suffix to asynchronous method names (e.g. use `ProcessBatchSubscriptions` instead of `ProcessBatchSubscriptionsAsync`), as the method return type (`Task` / `Task<T>`) already explicitly indicates asynchrony.
- **Dependency Injection Naming**: ALWAYS name injected dependencies in constructors using camelCase (e.g., `IFileExportService fileExportService`). NEVER use PascalCase (e.g., `IFileExportService FileExportService`) for constructor parameters or injected fields.
- **C# / .NET CA2263**: ALWAYS prefer generic `Enum.IsDefined<TEnum>(value)` (or `Enum.IsDefined(enumValue)` in .NET 7+) over the non-generic `Enum.IsDefined(typeof(TEnum), value)` to prevent unnecessary object boxing and reflection overhead.
- **Primary Constructor ([IDE0290](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0290))**: ALWAYS prefer and use C# Primary Constructors (e.g., `public class MyService(ILogger<MyService> logger, IConfiguration configuration) : IMyService`) for classes, records, structs, and dependency injection services whenever possible, instead of declaring explicit constructor bodies with boilerplate private backing fields, unless an explicit constructor body or multi-constructor chaining is strictly required.
- **Structured Logging ([CA1873](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1873))**: ALWAYS use structured logging message templates (e.g. `_logger.LogInformation("Processing {Id} for {Partner}", id, partner)`) instead of string interpolation (e.g. `_logger.LogInformation($"Processing {id} for {partner}")`) or eagerly evaluating expensive expressions (like `string.Join(...)`, `.Count()`, LINQ) in logging arguments. Check `_logger.IsEnabled(...)` before preparing expensive log data to avoid unnecessary allocations and CPU overhead when logging is disabled.
- **Remove Unused Using Directives ([IDE0005](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0005))**: ALWAYS check for and remove unnecessary/unused `using` directives (`using ...;`) after creating or editing any C# code file to keep imports clean, minimal, and prevent IDE0005 warnings and CS0105 duplicates.
- **Test Class & Helper Naming Suffix**: ALWAYS use `Test` or `Tests` as a SUFFIX for all test classes, test helper/mock/stub classes, and test methods (e.g. `StringLocalizerTest`, `VwControllerTests`, `VwControllerCommand_AddVwController_InsertsRecord_Test`). NEVER prefix with `Test` (e.g. do NOT use `TestStringLocalizer` or `TestVwController`).
- **Auto-Cleanup Temporary Plan Files**: ALWAYS automatically delete temporary plan files created at the repository root (e.g. `{task-slug}.md`, `ide0290-primary.md`, etc.) upon completing task execution, to prevent leftover artifact clutter in the workspace.
- **Command & Validator Integration Testing**: When writing integration tests for Commands (Delete, BatchDelete, Update, Add) that have a FluentValidation Validator, ALWAYS test the negative validation rule first (e.g. assert that an invalid/null ID payload returns `Assert.False(invalidResult.IsValid)`) BEFORE performing the Arrange insert into the database (`_db.Insertable`) and invoking the command.


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


