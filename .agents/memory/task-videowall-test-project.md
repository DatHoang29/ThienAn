---
type: project
created: 2026-08-14
updated: 2026-08-14
title: Kế hoạch & Kiến trúc Test Project đầu tiên (tests/) - Module.VideoWall
---

# Kế Hoạch & Context: Project Test Đầu Tiên (tests/) - Module.VideoWall

> **Mục tiêu**: Khởi tạo project test đầu tiên cho toàn bộ repo `TA-ITS015-WEBAPI-V1.0` tại thư mục `tests/` làm khuôn mẫu, bắt đầu bằng bộ integration test cho `Module.VideoWall` (`VwController`).

---

## 1. Bối Cảnh & Quyết Định Kiến Trúc

- **Quy định repo (§6 rule)**: Toàn bộ repo dùng **1 project test duy nhất** ở `tests/` (`tests/test.csproj` hoặc `tests/TAC_WebAPI.Tests.csproj`), không tách mỗi module 1 project test.
- **Host Test (`Host.cs`)**:
  - Dùng `WebApplicationFactory<TAC_WebAPI.Program>` để boot toàn bộ `TAC_WebAPI.Startup` thật (vì các service/handler phụ thuộc các assembly dựng sẵn trong `src/DLLs/Shared.Infrastructure.dll` và DI scan của Furion).
  - Không cần sửa `Program.cs` vì `Program` đã là `public class` theo pattern `CreateHostBuilder`.
  - Override cấu hình qua `ConfigureAppConfiguration` + `AddInMemoryCollection`:
    - `DbConnection:ConnectionConfigs:0:ConnectionString` → `Server=localhost,14333;Database=test;User Id=sa;Password=Password123!;TrustServerCertificate=true;`
    - `DbConnection:ConnectionConfigs:1:ConnectionString` → cùng chuỗi test
    - `Hangfire:Enable` → `false`
    - `Cache:FusionCache:CacheType` → `Memory`
    - `Cache:Redis:Enable` → `false`
  - **Guard 2 lớp chống nhầm DB thật**:
    1. Lớp 1: Kiểm tra chuỗi kết nối không chứa server/DB cấm (`10.10.8.30`, `DEV_ITS10`, `STAGING_ITS10`).
    2. Lớp 2: Mở `SqlConnection` thật, `SELECT DB_NAME()` phải bằng `test` và `SELECT @@SERVERNAME` không thuộc danh sách cấm.
  - **EnsureDatabase**: Tự động connect `master` tạo DB `test` nếu chưa có trên Docker.
  - **InitTables (11 entities Vw\*)**: Tự động chạy `db.CodeFirst.InitTables(...)` cho 11 entity VideoWall:
    - `VwController`, `VwControllerSlot`, `VwEventRule`, `VwEventTriggerLog`, `VwScene`, `VwSchedule`, `VwScreen`, `VwSlotPort`, `VwSource`, `VwWallTopology`, `VwWindowScene`.

---

## 2. Danh Sách File Cần Tạo / Cập Nhật

1. `tests/test.csproj`:
   - Target `net10.0`, `IsTestProject=true`.
   - Packages: `Microsoft.NET.Test.Sdk` (17.14.1), `xunit` (2.9.3), `xunit.runner.visualstudio` (3.1.0), `coverlet.collector` (6.0.4), `Microsoft.AspNetCore.Mvc.Testing` (bản preview .NET 10).
   - ProjectReference: `..\src\TAC_WebAPI\TAC_WebAPI.csproj`.
2. `tests/GlobalUsings.cs`:
   - Global usings: `Xunit`, `SqlSugar`, `Wolverine`, `Module.VideoWall.Core.Entities`, `Module.VideoWall.Core.Dto.Controller`, `Shared.Infrastructure.Persistence.SqlSugar`, `Shared.Infrastructure.Cache`, `Shared.DTO.Constants.Application`.
3. `tests/Host.cs`:
   - Kế thừa `WebApplicationFactory<TAC_WebAPI.Program>`, chứa cấu hình override, guard 2 lớp, InitTables 11 entity, expose `Services`.
4. `tests/Modules/VideoWall/VideoWallTests.cs`:
   - Kế thừa `IClassFixture<Host>`, `IDisposable`.
   - Prefix test isolation: `TEST_VWCTRL_` cho cả `Code` và `Name`.
   - Cleanup trong `Dispose()`: `db.Deleteable<VwController>().Where(x => _insertedIds.Contains(x.ID) || (x.Code != null && x.Code.StartsWith("TEST_"))).ExecuteCommand()`.
   - **7 test methods**:
     1. `VwControllerQuery_Page_ReturnsSuccess_Test`
     2. `VwControllerQuery_GetList_ReturnsSuccess_Test` (chú ý: phải xóa cache `_cache.RemoveByPrefixKey(CacheConst.Vw.VwController)` trước khi gọi)
     3. `VwControllerQuery_GetById_ReturnsSuccess_Test` (input: `VwIdControllerInput`)
     4. `VwControllerCommand_AddVwController_InsertsRecord_Test`
     5. `VwControllerCommand_UpdateVwController_UpdatesRecord_Test` (gọi non-generic `_bus.InvokeAsync(...)` để tránh bug generic mismatch của controller)
     6. `VwControllerCommand_DeleteVwController_SoftDeletesRecord_Test` (dùng `_db.Queryable<VwController>().ClearFilter()` để verify `IsDelete != null`)
     7. `VwControllerCommand_BatchDeleteVwController_SoftDeletesRecords_Test`
5. `src/TAC_WebAPI.sln`:
   - Đăng ký project: `dotnet sln "src\TAC_WebAPI.sln" add "tests\test.csproj"`.

---

## 3. Các Lưu Ý Kỹ Thuật Đã Phát Hiện

- **Bug production có sẵn**: `VwControllerController.UpdateVwController` và `DeleteVwController` gọi generic `InvokeAsync<TOutput>` nhưng CommandHandler trả `Task` (void). Trong test, gọi non-generic `_bus.InvokeAsync(command)` và verify qua DB.
- **`VwOrgAccessService`**: Trong test khi `App.User == null`, `IsFullAccess` tự động trả về `true`, không cần mock user/claims.
- **SQL Docker Local**: Chạy container tại `localhost:14333` qua `Docker/Compose/docker-compose.yml` (`mcr.microsoft.com/azure-sql-edge:1.0.7`, password `Password123!`).

---

## 4. Bước Tiếp Theo Khi Mở Lại Trên Máy Mới

1. Bật Docker SQL: `docker compose -f Docker/Compose/docker-compose.yml up -d` (nếu chưa chạy).
2. Tạo các file trong `tests/` theo kế hoạch trên.
3. Đăng ký test project vào `src/TAC_WebAPI.sln`.
4. Chạy `dotnet test tests/test.csproj` để kiểm thử và xác nhận toàn bộ 7 test pass.
