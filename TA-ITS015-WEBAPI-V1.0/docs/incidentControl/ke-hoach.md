# Kế hoạch phát triển module TmsIncidentControl

> Trạng thái: **Đã chốt hướng — sẵn sàng code**
> Tác giả: ChuongLP
> Ngày tạo: 02/07/2026 · Cập nhật: 02/07/2026
> Module: TMS — Feature: `IncidentControl`

---

## 1. Mục tiêu

Tạo bảng DB mới **`TmsIncidentControl`** để lưu lại **sự kiện điều khiển thiết bị** (thiết bị VMS nào đang được điều khiển theo sự cố nào, và trạng thái *đè hình* / *lặp hình*).

Bảng chạy **song song** với luồng ghi log hiện tại ở `TmsSignalLog`. Lý do tách bảng:

- `TmsSignalLog` ghi **quá nhiều** (append-only, mỗi lần điều khiển 1 dòng).
- `TmsIncidentControl` là bảng **gọn**, phản ánh quan hệ Incident × Equipment và trạng thái điều khiển, tách luồng xử lý riêng.

Phạm vi session này:
- Rà soát entity + gen bảng DB (bạn tự bật cơ chế gen).
- Làm trước 2 tính năng **Add** và **Update**.
- Chỉ tạo **Command (+ Query nếu cần)** theo cấu trúc CQRS, **KHÔNG tạo Controller / API**.
- DTO dùng chung đặt ở project **`Modules.TMS.Core`** để VMS gọi qua `_messBus.InvokeAsync`.

---

## 2. Quyết định thiết kế đã chốt

| # | Vấn đề | Quyết định |
|---|--------|-----------|
| 1 | Unique key | **KHÔNG cần unique.** Quan hệ nhiều–nhiều: 1 sự cố có nhiều thiết bị, 1 thiết bị thuộc nhiều sự cố. Bảng ít data. |
| 2 | Cách VMS gọi command | Dùng **`_messBus.InvokeAsync`** (in-process). DTO dùng chung đặt ở project mới **`Modules.TMS.Core`**. |
| 3 | Gen DB | Bạn **tự bật** cơ chế gen. Không cần script tay trong kế hoạch. |
| 4 | Trường thời gian | **Kế thừa từ `EntityTenant`** (`CreateTime`, `UpdateTime`...). Không thêm trường thời gian riêng. |
| 5 | Ý nghĩa Add | Khi thiết bị được gắn/điều khiển theo một sự cố → **Add** dòng tương ứng vào bảng, **`State = null`**. |
| 6 | Ý nghĩa Update | Chỉ khi **Update** mới set **`State = override`** (đánh dấu đã bị đè hình). |
| 7 | Soft-delete | **Giải quyết sau.** Chưa xử lý trong phạm vi này. |

---

## 3. Hiện trạng đã khảo sát

### 3.1. Entity đã tồn tại

File: `src/Modules/TMS/Modules.TMS/Core/Entities/TmsIncidentControl.cs`

```csharp
[SugarTable("TmsIncidentControl")]
public class TmsIncidentControl : EntityTenant
{
    [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
    public string? IncidentId { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
    public string? EquipmentId { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
    public string? State { get; set; }
}
```

- Kế thừa `EntityTenant` → đã có `ID`, `Code`, `TenantId`, audit fields (`CreateTime`, `CreateUId`, `UpdateTime`...), cờ soft-delete `IsDelete`.
- Entity **giữ nguyên**, không cần sửa (trường thời gian đã có sẵn từ `EntityTenant` theo quyết định #4).

### 3.2. Project DTO dùng chung: `Modules.TMS.Core`

File: `src/Modules/TMS/Modules.TMS.Core/Modules.TMS.Core.csproj`

- Đã có folder `Dto\Incident\`.
- Tham chiếu: `Shared.DTO`, `Shared.Reference`, `Shared.Utility`, và `Shared.Core.dll`.
- **KHÔNG** tham chiếu `Modules.TMS` (đúng chiều: `Modules.TMS` sẽ ref `Modules.TMS.Core`).

> ⚠️ **Lưu ý kỹ thuật quan trọng:** vì entity `TmsIncidentControl` nằm ở `Modules.TMS`, còn DTO dùng chung nằm ở `Modules.TMS.Core`, **DTO Input KHÔNG được kế thừa entity** (sẽ gây vòng tham chiếu). ⇒ DTO Input là **POCO độc lập** (chỉ khai báo `IncidentId`, `EquipmentId`, `State`), sang handler dùng **Mapster** `command.Adapt<TmsIncidentControl>()` để map.

### 3.3. Luồng ghi log hiện tại (nơi tích hợp gọi Add/Update)

| Vị trí | Mô tả |
|--------|-------|
| `Modules.VMS/Infrastructure/Services/Process/VmsProcessService.cs` (~346–388) | Sau khi update `VmsCurrent` + gửi lệnh thiết bị, nếu `isWriteLog` → build `TmsSignalLogAddRequest` rồi gọi `InsertIntoTmsSignaLog(...)`. Điểm chính điều khiển hiển thị hình. |
| `Modules.VMS/Controllers/Process/Commands/ScheduleCommandHandler.cs` (~637–657) | Luồng lập lịch / stop, cũng ghi `TmsSignalLog`. |

Thông tin có sẵn tại điểm này: `equipmentId`, `prepareVmsData.ContentInfoInput.IncidentId`, `ProcessType`, `ActionType`, `State` ("success"/"fail").

### 3.4. Pattern CQRS tham chiếu

- Handler mẫu: `Controllers/SignalLogs/Commands/SignalLogCommandHandler.cs`, `Controllers/Incident/Commands/TmsIncidentCommandHandler.cs`.
- Handler `IWolverineHandler`, method `HandleAsync({Input})`, dispatch qua `MessBus.InvokeAsync(...)` — Wolverine tự discover handler ở mọi assembly (nên VMS dispatch được tới handler của TMS).
- Repository: `BaseRepository<T>` — `InsertAsync`, `AsUpdateable(...).ExecuteCommandAsync()`, v.v.
- Cache: `_cache.RemoveByPrefixKey(CacheConst.Tms.XXX)`.

---

## 4. Thiết kế nghiệp vụ Add / Update

Vì **không dùng unique key**, bảng cho phép nhiều dòng cho cùng thiết bị / cùng sự cố.

### 4.1. Add — ghi nhận thiết bị được điều khiển theo sự cố

Khi một thiết bị được gắn/điều khiển theo một sự cố:
1. Nhận `IncidentId`, `EquipmentId`.
2. `State = null` (mặc định — nghĩa là dòng mới, chưa bị đè).
3. `InsertAsync` một dòng mới (append, không kiểm tra tồn tại).
4. `_cache.RemoveByPrefixKey(CacheConst.Tms.TmsIncidentControl)`.

> Ghi chú: nếu tại 1 sự cố có nhiều thiết bị → Add nhiều dòng tương ứng (có thể hỗ trợ overload `HandleAsync(List<AddTmsIncidentControlInput>)` để insert range).

### 4.2. Update — đánh dấu đè hình

Khi thiết bị bị điều khiển đè bằng nội dung mới:
1. Nhận `EquipmentId` (và/hoặc `IncidentId`).
2. Set **`State = override`** cho (các) dòng tương ứng của thiết bị.
   - Dùng `AsUpdateable().SetColumns(x => x.State == IncidentControlConst.State.Override).Where(x => x.EquipmentId == command.EquipmentId).ExecuteCommandAsync()`.
3. Xóa cache.

> `State` để mở tham số trong Input; tạm mặc định `override`. Tính năng chọn `override` / `append` phát triển sau.

### 4.3. Thứ tự gọi so với ghi `TmsSignalLog`

- Gọi **sau** khi ghi `TmsSignalLog` thành công, cùng điều kiện `isWriteLog == true`.
- **Bọc try/catch riêng** để lỗi cập nhật `TmsIncidentControl` không làm hỏng luồng điều khiển / ghi log chính.

---

## 5. Hằng số State (tạo mới)

Theo convention `DutyScheduleConst.cs`, tạo `src/Modules/TMS/Modules.TMS/Core/Constants/IncidentControlConst.cs`:

```csharp
namespace Modules.TMS.Core.Constants
{
    public static class IncidentControlConst
    {
        /// <summary>Trạng thái điều khiển thiết bị theo sự cố</summary>
        public static class State
        {
            /// <summary>Đè hình — thiết bị đã bị ghi đè nội dung mới (set khi Update)</summary>
            public const string Override = "override";

            /// <summary>Lặp/nối hình — phát triển sau</summary>
            public const string Append = "append";
        }
    }
}
```

> Nếu VMS cũng cần dùng hằng số này khi build Input, cân nhắc đặt hằng số ở `Modules.TMS.Core` (project dùng chung) thay vì trong `Modules.TMS`.

---

## 6. Cấu trúc thư mục dự kiến

### 6.1. DTO dùng chung — trong `Modules.TMS.Core`

```
Modules.TMS.Core/
└── Dto/
    └── Incident/
        └── TmsIncidentControlInput.cs   # POCO độc lập (KHÔNG kế thừa entity)
```

```csharp
namespace Modules.TMS.Core.Dto.Incident
{
    // POCO độc lập — map sang entity bằng Mapster ở handler
    public class AddTmsIncidentControlInput
    {
        public string? IncidentId { get; set; }
        public string? EquipmentId { get; set; }
        public string? State { get; set; }   // Add: null
    }

    public class UpdateTmsIncidentControlInput
    {
        public string? IncidentId { get; set; }
        public string? EquipmentId { get; set; }
        public string? State { get; set; }   // Update: "override"
    }
}
```

### 6.2. Handler — trong `Modules.TMS` (chỉ Command, KHÔNG Controller)

```
Controllers/IncidentControl/
├── Commands/
│   └── TmsIncidentControlCommandHandler.cs   # HandleAsync(Add...), HandleAsync(Update...)
├── Queries/                                   # (tùy chọn, làm sau)
│   └── TmsIncidentControlQueryHandler.cs
└── Validators/                                # (tùy chọn)
    └── IncidentControlValidator.cs
```

Handler map DTO → entity:
```csharp
var entity = command.Adapt<TmsIncidentControl>();
await _repo.InsertAsync(entity);
```

---

## 7. Thay đổi phụ trợ cần làm

| Việc | File | Ghi chú |
|------|------|---------|
| Thêm cache key | `Shared.DTO/Constants/Application/CacheConst.cs` → class `Tms` | `public const string TmsIncidentControl = "tms_incident_control";` |
| Thêm hằng số State | `Modules.TMS/Core/Constants/IncidentControlConst.cs` (mới) | Xem mục 5 (cân nhắc đặt ở `Modules.TMS.Core` nếu VMS cần) |
| DTO dùng chung | `Modules.TMS.Core/Dto/Incident/TmsIncidentControlInput.cs` (mới) | POCO độc lập |
| Command handler | `Modules.TMS/Controllers/IncidentControl/Commands/TmsIncidentControlCommandHandler.cs` (mới) | Add + Update |
| Tham chiếu project | `Modules.TMS.csproj` ref `Modules.TMS.Core`; `Modules.VMS.csproj` ref `Modules.TMS.Core` | Để cả hai dùng chung DTO |
| Tích hợp gọi | `VmsProcessService.cs`, `ScheduleCommandHandler.cs` | Bọc try/catch riêng |

> Cần kiểm tra `Modules.TMS.csproj` và `Modules.VMS.csproj` đã tham chiếu `Modules.TMS.Core` chưa; nếu chưa thì thêm `ProjectReference`.

---

## 8. Checklist triển khai

- [ ] Tạo DTO `TmsIncidentControlInput.cs` (Add/Update — POCO) trong `Modules.TMS.Core/Dto/Incident/`.
- [ ] Tạo `IncidentControlConst.cs` (hằng số State).
- [ ] Thêm cache key `TmsIncidentControl` vào `CacheConst.Tms`.
- [ ] Tạo `TmsIncidentControlCommandHandler.cs`:
  - [ ] `HandleAsync(AddTmsIncidentControlInput)` → InsertAsync, `State = null`.
  - [ ] `HandleAsync(UpdateTmsIncidentControlInput)` → set `State = override` cho thiết bị.
  - [ ] (tùy chọn) `HandleAsync(List<AddTmsIncidentControlInput>)` → InsertRange.
- [ ] Đảm bảo `Modules.TMS` & `Modules.VMS` đều ref `Modules.TMS.Core`.
- [ ] Gen/đồng bộ bảng DB (bạn tự bật cơ chế gen).
- [ ] Tích hợp `_messBus.InvokeAsync(...)` tại `VmsProcessService.cs` & `ScheduleCommandHandler.cs` (try/catch riêng).
- [ ] Build `dotnet build TAC_WebAPI.sln` + kiểm thử luồng điều khiển VMS.
- [ ] (Sau) Xử lý soft-delete / dọn dữ liệu khi sự cố kết thúc.

---

## 9. Sơ đồ luồng đề xuất (tóm tắt)

```
Điều khiển VMS (VmsProcessService / ScheduleCommandHandler)
        │
        ├─ 1. Update VmsCurrent
        ├─ 2. Ghi TmsSignalLog (append log — như hiện tại)
        ├─ 3. [MỚI] Ghi TmsIncidentControl
        │        ├─ Thiết bị mới theo sự cố  → Add  (State = null)
        │        └─ Thiết bị bị đè hình       → Update (State = override)
        │        (gọi qua _messBus.InvokeAsync — in-process, DTO ở Modules.TMS.Core)
        └─ 4. Broadcast socket
```
```
