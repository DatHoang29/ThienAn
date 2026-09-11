# Plan: Tạo Core riêng cho Service VideoWall, gom Worker và triệt tiêu phụ thuộc ngược vào WebAPI

## 1. Context & Vấn đề cần giải quyết

`Services/VideoWall` (`ITS.VideoWall`, `ITS.VideoWall.Worker`, `ITS.VideoWall.WPF`) hiện tham chiếu trực tiếp `Module.VideoWall.Core` (project Core của WebAPI) để lấy DTO/Interface/Entity dùng chung. Đã rà soát TOÀN BỘ 67 file trong `Module.VideoWall.Core` (đếm type nào được `Module.VideoWall` — WebAPI — dùng, type nào được `Services/VideoWall` dùng) để xác định:
- Cái gì thật sự chỉ Worker cần: tách ra Core riêng (`ITS.VideoWall.Core`).
- Cái gì là hợp đồng dùng chung thật (Entities, DTOs chung, Enums, Options, Shared Interfaces): giữ lại trong `Module.VideoWall.Core`.

**Phát hiện phụ thuộc ngược nghiêm trọng**: `ITS.VideoWall.Worker.csproj` đang có `ProjectReference` THẲNG tới `Module.VideoWall.csproj` (không phải chỉ `.Core`!) — chỉ để lấy 2 class cài đặt cụ thể cho DI:
1. `Program.cs:30`: `builder.Services.AddScoped<IVwSceneRegionService, Module.VideoWall.Infrastructure.Services.Scene.VwSceneRegionService>();`
2. `Program.cs:31`: `builder.Services.AddScoped<IVwEventTriggerLogWriter, Module.VideoWall.Infrastructure.Services.EventTriggerLog.VwEventTriggerLogWriter>();`

Đây là kiểu phụ thuộc ngược kéo toàn bộ project WebAPI (kèm Furion, MVC, toàn bộ controller) vào Worker. Cần xử lý triệt để đồng thời với việc tách Core riêng cho Worker.

---

## 2. Kết quả khảo sát thực tế & Chốt hướng xử lý

### 2.1. Khảo sát code `VwSceneRegionService.cs` (Chốt mục 3 của Plan)
- **Kiểm tra phụ thuộc WebAPI-only**:
  - `App.User`: **KHÔNG DÙNG** (0 kết quả).
  - `HttpContext` / MVC / Controllers: **KHÔNG DÙNG** (0 kết quả).
  - `Furion`: Chỉ dùng `IScoped` và `Oops.Oh(...)` (thuộc `Shared.Reference`, cả WebAPI lẫn Worker đều đã reference).
  - Database: Sử dụng `BaseRepository<VwScreen>`, `BaseRepository<VwScene>`, `BaseRepository<VwWallTopology>`. Cả WebAPI và Worker đều đã có class `BaseRepository<T> : SqlSugarRepository<T>` riêng của mình.
  - Toạ độ & hình học: Chứa các thuật toán thuần static (`SliceWindowForControllers`, `ToIsapiLocalRect`, `ToCenterUniformRect`).
- **Chọn hướng xử lý (a) hay (b)**:
  - Nếu chọn (a) "Move implementation `VwSceneRegionService` sang `ITS.VideoWall.Core` để 2 bên dùng chung": Sẽ phát sinh nghịch lý — vì theo thiết kế chuẩn, WebAPI (`Module.VideoWall`) **KHÔNG ĐƯỢC PHÉP** reference `ITS.VideoWall.Core`. Nếu WebAPI reference `ITS.VideoWall.Core`, WebAPI lại phụ thuộc ngược vào Core của Service! Mặt khác, `Module.VideoWall.Core` theo Rule 5.1 chỉ chứa Entity, DTO, Interface, Helper, không chứa Service truy vấn Repository.
  - **Quyết định (Hướng b+)**:
    1. **Chia sẻ logic tính toán không I/O**: Di chuyển các hàm tính toán toạ độ static thuần túy (`SliceWindowForControllers`, `ToIsapiLocalRect`) sang `VwGeometryHelper.cs` trong `Module.VideoWall.Core` (nơi đã có sẵn `ToCenterUniformRect`). Đảm bảo **100% không trùng lặp thuật toán**.
    2. **WebAPI**: Giữ nguyên `VwSceneRegionService.cs` trong `Module.VideoWall` phục vụ cho `VwWindowSceneCommandHandler`, các hàm static ủy quyền sang `VwGeometryHelper`.
    3. **Worker**: Cung cấp bản cài đặt riêng `VwSceneRegionService.cs` trong `ITS.VideoWall/Services/Scene/` triển khai `IVwSceneRegionService`, inject `BaseRepository<T>` của Worker và gọi `VwGeometryHelper`.

### 2.2. Xử lý `VwEventTriggerLogWriter` (Dòng 31 `Program.cs`)
- `VwISAPIDeviceService` trong Worker cần `IVwEventTriggerLogWriter` để ghi log các bước Setup thiết bị (Ping, Probe, Sync, Setup Scene) vào bảng `VwEventTriggerLog`.
- Trong WebAPI, `VwEventTriggerLogWriter` lấy user từ `App.User` và IP từ `HttpContext`.
- Trong Worker, không có HTTP context.
- **Giải pháp**: Tạo bản cài đặt riêng `VwEventTriggerLogWriter : IVwEventTriggerLogWriter` trong `ITS.VideoWall/Services/EventTriggerLog/` ghi nhận trực tiếp vào bảng `VwEventTriggerLog` với `operator = "system"` hoặc `WorkerName`.
- Cả `IVwSceneRegionService` và `IVwEventTriggerLogWriter` sẽ được đăng ký tự động qua `AddVideoWallWorker()`. Khi đó `Program.cs` sạch hoàn toàn mọi tham chiếu tới `Module.VideoWall.Infrastructure.*`.

---

## 3. Danh sách việc cần làm

### Việc 0. Gom `ITS.VideoWall.Worker` vào `ITS.VideoWall`
Hiện `ITS.VideoWall.Worker` chỉ là project host rỗng (SDK `Microsoft.NET.Sdk.Worker`, `OutputType=Exe`, `net10.0-windows`, có `Program.cs` + `appsettings.json` + link `camera.creds`), `ProjectReference` sang cả `ITS.VideoWall.csproj` lẫn `Module.VideoWall.csproj`. Gom lại thành 1 project duy nhất `ITS.VideoWall`:
1. Đổi `ITS.VideoWall.csproj`:
   - Thêm `<OutputType>Exe</OutputType>`.
   - Đổi `TargetFramework` từ `net10.0` → `net10.0-windows` (để dùng `UseWindowsService()`).
   - Thêm PackageReference `Microsoft.Extensions.Hosting.WindowsServices` Version `10.0.10`.
   - Thêm `<ProjectReference Include="..\ITS.VideoWall.Core\ITS.VideoWall.Core.csproj" />`.
   - Thêm copy `appsettings.json` và link `camera.creds`.
2. Move `Program.cs`, `appsettings.json` từ `ITS.VideoWall.Worker/` sang `ITS.VideoWall/`.
3. Xoá hẳn project `ITS.VideoWall.Worker` (thư mục + entry trong `TAC_WebAPI.sln`).
4. Không ảnh hưởng `ITS.VideoWall.WPF`.

### Việc 1. Tạo project mới `ITS.VideoWall.Core.csproj`
- Vị trí: `Services/VideoWall/ITS.VideoWall.Core/ITS.VideoWall.Core.csproj`.
- Target: `net10.0`.
- Tham chiếu: `Module.VideoWall.Core.csproj`, `Shared.Reference.csproj`, `Shared.Core.dll`, `Shared.Infrastructure.dll`.
- Chỉ Worker (`ITS.VideoWall`, `ITS.VideoWall.WPF`) tham chiếu — `Module.VideoWall` (WebAPI) KHÔNG tham chiếu project này.
- Thêm entry vào `TAC_WebAPI.sln` dưới folder `VideoWall`.

### Việc 2. Xoá 22 endpoint diagnostic (Việc 4a của plan phân quyền)
> *Bắt buộc làm trước khi chuyển file `VwISAPIWindowRequest.cs`.*
1. Xoá thư mục `Module.VideoWall/Controllers/Device/` (`VwDeviceController.cs`, Handlers, Validators).
2. Xoá các DTO diagnostic trong `Module.VideoWall.Core/Dto/Device/VwDeviceInput.cs`, giữ lại `VwDeviceTargetInput` và `VwDeviceInputChannelsInput`.
3. Xoá `VwDeviceProxyCommandDispatcher.cs`, rút gọn `VwDeviceProxyQueryDispatcher.cs` (chỉ giữ `InputChannels`), xoá 22 hằng số trong `VwCommandActions.cs`.

### Việc 3. Move 5 file Worker-only sang `ITS.VideoWall.Core`
Di chuyển 5 file từ `Module.VideoWall.Core` sang `ITS.VideoWall.Core`:
1. `Interfaces/IVwISAPIDeviceClient.cs`
2. `Interfaces/IVwISAPIDeviceService.cs`
3. `Dto/ISAPI/VwISAPIResult.cs`
4. `Constants/VwDeviceProfile.cs`
5. `Dto/ISAPI/VwISAPIWindowRequest.cs` (sau khi Việc 2 xong)

Đổi namespace các file trên sang `ITS.VideoWall.Core.*` và cập nhật các file trong `ITS.VideoWall` đang import (`VwISAPIDeviceClient.cs`, `VwISAPIDeviceService.cs`, `VwISAPIDeviceService.DeviceSetup.cs`, `VwISAPIDigestHandler.cs`, `VwDeviceHandlerRunner.cs`, `IVwDeviceProxyDispatcher.cs`).

### Việc 4. Tách logic hình học sang `VwGeometryHelper` & viết cài đặt Worker cho DI
1. Chuyển `SliceWindowForControllers` và `ToIsapiLocalRect` sang `VwGeometryHelper.cs` (`Module.VideoWall.Core/Utilities/`).
2. Viết `VwSceneRegionService.cs` trong `ITS.VideoWall/Services/Scene/` triển khai `IVwSceneRegionService`.
3. Viết `VwEventTriggerLogWriter.cs` trong `ITS.VideoWall/Services/EventTriggerLog/` triển khai `IVwEventTriggerLogWriter`.
4. Đăng ký trong `ITS.VideoWall/Extensions/ServiceCollectionExtensions.cs` (`AddVideoWallWorker`):
   ```csharp
   services.AddScoped<IVwSceneRegionService, ITS.VideoWall.Services.Scene.VwSceneRegionService>();
   services.AddScoped<IVwEventTriggerLogWriter, ITS.VideoWall.Services.EventTriggerLog.VwEventTriggerLogWriter>();
   ```
5. Trong `ITS.VideoWall/Program.cs`: Bỏ 2 dòng `builder.Services.AddScoped<...Module.VideoWall.Infrastructure...>` cũ.

### Việc 5. Cập nhật `tests/test.csproj` & Test Suite
1. Cập nhật `tests/test.csproj`:
   - Đổi path `<VideoWallIsapiSource>` trỏ sang `$(RepoRoot)\src\Services\VideoWall\ITS.VideoWall.Core\Interfaces\IVwISAPIDeviceClient.cs`.
   - Thêm `<ProjectReference Include="$(RepoRoot)\src\Services\VideoWall\ITS.VideoWall.Core\ITS.VideoWall.Core.csproj" />` vào ItemGroup kiểm thử Worker.
2. Cập nhật `tests/Modules/VideoWall/GlobalUsings.VideoWall.cs`:
   - Thêm `global using ITS.VideoWall.Core.Constants;`
   - Thêm `global using ITS.VideoWall.Core.Dto.ISAPI;`
   - Thêm `global using ITS.VideoWall.Core.Interfaces;`

---

## 4. Những thành phần GIỮ NGUYÊN (KHÔNG move khỏi Module.VideoWall.Core)

- **Entities**: `VwController`, `VwScene`, `VwScreen`, `VwSource`, `VwSchedule`, `VwSlotPort`, `VwEventRule`, `VwEventTriggerLog`, `VwUserAreaPermission`, `VwWindowScene`, `VwWallTopology`.
- **Command & Messaging DTOs**: `VwCommandActions`, `VwCommandEnvelope`, `VwCommandPayloads`, `VwSubjects`.
- **Shared Enums & Constants**: `VwWallProfile`, `VwEventTriggerAction`.
- **Shared DTOs & Output**: `VwDeviceGenericOutput<T>`, `VwDeviceSetupInput/Output`, `VwISAPIResponse/SceneResponse/WallResponse`, `VwSetWindowLayerInput`, `VwSwitchWindowSourceInput`.
- **Shared Interfaces**: `IVwEventTriggerLogWriter`, `IVwNatsPublisher`, `IVwSceneRegionService`.
- **Utilities & Config**: `BaseMsg`, `VwGeometryHelper`, `VwDeviceOptions`.

---

## 5. Verification Plan

1. `dotnet build src/Services/VideoWall/ITS.VideoWall/ITS.VideoWall.csproj`:
   - Build thành công dạng Exe (`net10.0-windows`).
   - Không còn `ProjectReference` tới `Module.VideoWall.csproj`.
   - Chạy được như Windows Service (`UseWindowsService`).
2. `Project ITS.VideoWall.Worker` không còn tồn tại trong ổ đĩa và trong `TAC_WebAPI.sln`.
3. `Grep` kiểm tra sạch `Module.VideoWall.Core`:
   - `IVwISAPIDeviceClient|IVwISAPIDeviceService|VwISAPIResult\b|VwDeviceProfile|VwISAPIWindowRequest` trong `Module.VideoWall.Core/` → **0 kết quả**.
4. `Grep` kiểm tra sạch namespace WebAPI trong Worker:
   - `Module.VideoWall\.` (không tính `.Core`) trong toàn bộ `src/Services/VideoWall/ITS.VideoWall/` → **0 kết quả**.
5. Build độc lập `Module.VideoWall.csproj` → thành công, không phụ thuộc `ITS.VideoWall.Core`.
6. Chạy `dotnet test tests/test.csproj --filter "FullyQualifiedName~VideoWall"` → pass 100%.
