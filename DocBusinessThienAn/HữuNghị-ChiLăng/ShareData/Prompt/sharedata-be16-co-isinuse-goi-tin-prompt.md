# BE-16 · Trả cờ `IsInUse` trong API phân trang Gói tin

**Tệp prompt:** `DocBusinessThienAn/HữuNghị-ChiLăng/ShareData/Prompt/sharedata-be16-co-isinuse-goi-tin-prompt.md`

> 📌 Soạn 05/10/2026 · Nguồn: `Sharedata_MasterPlan.md` dòng `BE-16` + PHẦN III mục B ("Làm mờ nút Xóa").
> Nhánh đề xuất: `feat/20261006-XD001.5.6-sharedata-be16-isinuse` (repo `TA-ITS015-WEBAPI-V1.0`, tách từ `dev`).

## 1. Mục tiêu

FE màn **Cấu hình gói tin** (`dataSource/index.vue`) cần biết gói tin nào **đang được dùng** để làm mờ nút Xoá + tooltip. Hiện `ShareDataPagePacketOutput` là class rỗng, luật "đang dùng" chỉ chạy lúc bấm Sửa/Xoá.

🔴 **Đối chiếu code 05/10/2026 — luật này hiện đã bị chép ĐÔI**, giống nhau từng ký tự:

| Tệp | Hàm | Call-site |
| --- | --- | --- |
| `Controllers/Packet/Commands/PacketCommandHandler.cs` | `private IsDatatypeInUseAsync` (dòng 155) | dòng 90 (khoá Mã/Tên), 112, 138 → `PacketInUse` |
| `Controllers/PacketField/Commands/PacketFieldCommandHandler.cs` | `private IsDatatypeInUseAsync` (dòng 162) | dòng 88, 120, 146 → `PacketFieldInUse` |

Thêm cờ `IsInUse` mà viết luật lần thứ 3 thì thành **3 bản** — prompt này gom cả 3 về một nơi.

**Luật "đang dùng"** (giữ nguyên y hệt hiện tại):
- có hồ sơ ánh xạ chưa xoá (`ShareDataMapping.DatatypeId`), **HOẶC**
- có đăng ký chưa xoá ở trạng thái còn sống (`ShareDataSubscription.DatatypeId` + `ShareDataConst.SubStateGroup.Alive` — kiểu `BaseEnums.SubSubscriptionState?[]`).

📌 Ngoài phạm vi: `MappingCommandHandler.cs:381`, `PartnerCommandHandler.cs:244`, `SubscriptionCommandHandler` cũng dùng `SubStateGroup.Alive` nhưng **khác luật** (không xét hồ sơ ánh xạ) ⇒ ⛔ không gom.

## 2. Ràng buộc

- ⛔ **Không** nâng `IsDatatypeInUseAsync` lên `public` (rule 7 — phạm vi truy cập tối thiểu).
- ⛔ **Không** gọi kiểm tra theo từng dòng trong vòng lặp (N+1). Gom **2 truy vấn cho cả trang** — khuôn `SubscriptionQueryHandler.FillMappingInfoAsync`.
- 🔴 Luật "đang dùng" chỉ được tồn tại **1 bản** — 2 cổng chặn (gói tin, trường gói tin) và cờ hiển thị phải dùng chung.
- ⛔ Không thêm `using` trùng `GlobalUsings.cs` (rule 4.12). Không hậu tố `Async` cho hàm mới (rule 7).
- Phạm vi: **chỉ API phân trang**. API danh sách không phân trang (dropdown) giữ nguyên — đổi sau nếu FE cần.

## 3. Thay đổi

### TĐ1 · [NEW] `Module.ShareData/Infrastructure/Services/ShareDataPacket/PacketUsageService.cs`

Đặt cạnh các service sẵn có, đăng ký bằng `IScoped` giống `ShareDataMappingResolver`.

📌 **Quy ước namespace đo từ code**: thư mục `ShareDataX/` ↔ namespace `...Services.ShareDataXService` (`ShareDataActivity` → `ShareDataActivityService`, `ShareDataCodeSet` → `ShareDataCodeSetService`). 🔴 ⛔ Không đặt namespace `...Services.ShareDataPacket` — trùng tên entity `ShareDataPacket`, gây nhập nhằng kiểu ở mọi tệp `using` nó.

```csharp
namespace Module.ShareData.Infrastructure.Services.ShareDataPacketService
{
    /// <summary>
    /// Author: <tên người áp>
    /// Description: Luật DUY NHẤT xác định gói tin đang được dùng — có hồ sơ ánh xạ hoặc đăng ký còn sống.
    ///              Dùng chung cho cổng chặn của PacketCommandHandler, PacketFieldCommandHandler và cờ IsInUse của lưới.
    /// Created date: 06/10/2026
    /// </summary>
    public class PacketUsageService : IScoped
    {
        private readonly BaseRepository<ShareDataMapping> _mappingRep;
        private readonly BaseRepository<ShareDataSubscription> _subscriptionRep;

        public PacketUsageService(
            BaseRepository<ShareDataMapping> mappingRep,
            BaseRepository<ShareDataSubscription> subscriptionRep)
        {
            _mappingRep = mappingRep;
            _subscriptionRep = subscriptionRep;
        }

        /// <summary>
        /// Description: Trả tập ID gói tin đang được dùng trong danh sách truyền vào — đúng 2 truy vấn cho mọi kích thước danh sách.
        /// Created date: 06/10/2026
        /// </summary>
        public async Task<HashSet<string>> GetInUseIds(IReadOnlyCollection<string> datatypeIds)
        {
            if (datatypeIds.Count == 0) return [];

            var mappedIds = await _mappingRep.AsQueryable()
                .Where(u => u.IsDelete == null && datatypeIds.Contains(u.DatatypeId))
                .Select(u => u.DatatypeId)
                .Distinct()
                .ToListAsync();

            var subscribedIds = await _subscriptionRep.AsQueryable()
                .Where(u => u.IsDelete == null
                    && datatypeIds.Contains(u.DatatypeId)
                    && ShareDataConst.SubStateGroup.Alive.Contains(u.State))
                .Select(u => u.DatatypeId)
                .Distinct()
                .ToListAsync();

            return mappedIds.Concat(subscribedIds)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id!)
                .ToHashSet();
        }
    }
}
```

📌 Giữ `u.IsDelete == null` tường minh y như `IsDatatypeInUseAsync` hiện tại (dù bộ lọc toàn cục `EntityTenant` cũng tự thêm) — đổi hành vi không thuộc phạm vi prompt này.

### TĐ2 · [MODIFY] `Module.ShareData.Core/Dto/Packet/ShareDataPacketOutput.cs`

```diff
-    public class ShareDataPagePacketOutput : ShareDataPacketOutput { }
+    public class ShareDataPagePacketOutput : ShareDataPacketOutput
+    {
+        /// <summary>Gói tin đang có hồ sơ ánh xạ hoặc đăng ký còn sống — FE dùng để khoá nút Xoá.</summary>
+        [SugarColumn(IsIgnore = true)]
+        public bool IsInUse { get; set; }
+    }
```

🔴 `IsIgnore = true` bắt buộc: DTO kế thừa entity và được `Select(..., true)` tự ánh xạ theo tên cột — thiếu cờ này SqlSugar sẽ tìm cột `IsInUse` trong bảng và ném lỗi.

### TĐ3 · [MODIFY] `Module.ShareData/Controllers/Packet/Queries/PacketQueryHandler.cs`

- Thêm field + tham số ctor `PacketUsageService _packetUsage`.
- Trong `HandleAsync(ShareDataPagePacketInput command)`:

```diff
                 .ToPagedListAsync(command.Page, command.PageSize);
+
+            var inUseIds = await _packetUsage.GetInUseIds(
+                pagedList.Records.Select(u => u.ID).ToList());
+            foreach (var item in pagedList.Records)
+                item.IsInUse = inUseIds.Contains(item.ID);
+
             return pagedList;
```

📌 `SqlSugarPagedList<T>` nằm trong `Shared.Infrastructure.dll` (không có mã nguồn trong repo). Tiền lệ `SubscriptionQueryHandler.cs:53` gọi `FillMappingInfoAsync(pagedList.Records)` rồi **gán thuộc tính lên chính các phần tử đó** và lưới hiện đúng ⇒ `Records` là danh sách đã vật chất hoá, gán trực tiếp như trên là an toàn.

### TĐ4 · [MODIFY] `Module.ShareData/Controllers/Packet/Commands/PacketCommandHandler.cs`

- Thay thân `IsDatatypeInUseAsync` (giữ `private`, giữ tên để không đụng 3 call-site dòng 90 · 112 · 138):

```diff
         private async Task<bool> IsDatatypeInUseAsync(string? datatypeId)
         {
             if (string.IsNullOrWhiteSpace(datatypeId)) return false;
-
-            if (await _mappingRep.IsAnyAsync(u => u.IsDelete == null && u.DatatypeId == datatypeId))
-                return true;
-
-            return await _subscriptionRep.IsAnyAsync(u => u.IsDelete == null
-                && u.DatatypeId == datatypeId
-                && ShareDataConst.SubStateGroup.Alive.Contains(u.State));
+
+            var inUseIds = await _packetUsage.GetInUseIds([datatypeId]);
+            return inUseIds.Contains(datatypeId);
         }
```

- 🔴 **Đã đo**: `_mappingRep` / `_subscriptionRep` trong tệp này **chỉ** được dùng bên trong `IsDatatypeInUseAsync` (dòng 159, 162 — ngoài ra chỉ có khai báo dòng 24-25 và gán dòng 37-38). Sau khi thay thân hàm ⇒ **xoá cả 2 field + 2 tham số ctor**, thay bằng 1 field + 1 tham số `PacketUsageService packetUsage`. Giữ lại sẽ dính IDE0052.
- Rà `using Module.ShareData.Core.Constants;` (dòng 2): nếu `ShareDataConst` không còn được dùng ở chỗ nào khác trong tệp thì xoá (rule 4.12).
- Cập nhật XML doc của `IsDatatypeInUseAsync`: luật đã dời sang `PacketUsageService`; đổi chữ "phễu lọc" → "hồ sơ ánh xạ" cho khớp thuật ngữ đã chốt.

### TĐ4b · [MODIFY] `Module.ShareData/Controllers/PacketField/Commands/PacketFieldCommandHandler.cs`

Bản chép thứ hai của cùng luật (dòng 162-172). Làm **y hệt TĐ4**:
- Thân `IsDatatypeInUseAsync` gọi `_packetUsage.GetInUseIds([datatypeId])`; giữ `private`, giữ tên ⇒ 3 call-site dòng 88 · 120 · 146 (ném `PacketFieldInUse`) không đổi.
- 🔴 **Đã đo**: `_mappingRep` / `_subscriptionRep` ở đây cũng **chỉ** dùng trong `IsDatatypeInUseAsync` (dòng 166, 169) ⇒ xoá 2 field + 2 tham số ctor, thay bằng `PacketUsageService`.
- Rà `using` thừa như TĐ4; sửa "phễu lọc" → "hồ sơ ánh xạ" trong XML doc.

### TĐ5 · [NEW] Test toàn trình — `tests/BE/ITS/ShareData/Controllers/ShareDataPacketControllerTests.cs`

Theo khuôn `ShareDataActivityLogControllerTests` (`[Collection("api")]`, `IMessageBus` + `ISqlSugarClient` từ `Host`). Full business flow (rule 4.9), không mock ngoài (rule 15), không `try-finally`, không tự xoá DB trong từng test.

| Test | Arrange | Assert |
| --- | --- | --- |
| `Page_WhenPacketHasActiveMapping_ReturnsIsInUseTrue_Test` | Seed gói A + `ShareDataMapping` (DatatypeId = A) | Dòng A `IsInUse == true` |
| `Page_WhenPacketHasAliveSubscriptionOnly_ReturnsIsInUseTrue_Test` | Seed gói B + đăng ký trạng thái thuộc `Alive` | Dòng B `IsInUse == true` |
| `Page_WhenSubscriptionIsNotAliveAndNoMapping_ReturnsIsInUseFalse_Test` | Seed gói C + đăng ký trạng thái **ngoài** `Alive` | Dòng C `IsInUse == false` |
| `Page_WhenInUseFlagTrue_DeleteIsBlockedConsistently_Test` | Seed gói D có mapping, gọi Page rồi gọi lệnh Xoá D | `IsInUse == true` **và** lệnh Xoá ném đúng exception `packetInUse` — khoá cam kết "lưới và cổng chặn không nói khác nhau" |
| `DeletePacketField_WhenPacketInUse_IsBlockedBySharedRule_Test` | Seed gói E có đăng ký `Alive` + 1 trường gói tin, gọi lệnh Xoá trường | Ném `packetFieldInUse` — khoá TĐ4b: cổng chặn trường gói tin vẫn đúng sau khi chuyển sang `PacketUsageService` |

📌 Lọc `Code` bằng tiền tố ngẫu nhiên mỗi test (`$"T16_{Guid:N}"[..12]`) để không đụng dữ liệu test khác trong cùng collection.

## 4. Kiểm chứng

```powershell
# Rule 11: rà tests/BE/appsettings.Test.json trỏ local trước
dotnet build TA-ITS015-WEBAPI-V1.0/src/TAC_WebAPI/TAC_WebAPI.csproj
dotnet test tests/BE/test.csproj --filter "FullyQualifiedName~ShareData"
```

- ✅ 4 test mới xanh, toàn bộ test ShareData cũ vẫn xanh (đặc biệt các test xoá/sửa gói tin đang dùng).
- 🔴 Sau khi BE xanh: chạy **`pnpm build-api`** ở `TA-ITS015-WEBVUE-V1.0` để regen `api-services/` (rule 20.2) — model `share-data-page-packet-output.ts` phải có `isInUse?: boolean`. ⛔ Không sửa tay `api-services/`.

## 5. Việc FE kế tiếp (prompt riêng, không thuộc prompt này)

`dataSource/index.vue`: bê khuôn `subscriptionTable.vue` (`canToggle()` + `toggleTitle()`) → nút Xoá `:disabled="row.isInUse"` + `el-tooltip` key mới `lz.tooltip.sharedataDataSource.packetInUse`. ⛔ Không làm mờ nút Sửa (backend chỉ khoá Mã/Tên — MasterPlan PHẦN III mục B).

## 6. Đồng bộ tài liệu sau khi áp

- `Sharedata_MasterPlan.md`: tích `[x]` `BE-16` kèm ngày; dòng FE mục B đổi "Bị chặn bởi BE-16" → "Sẵn sàng".
- `Prompt/README.md`: thêm dòng nhật ký.

## 7. Commit gợi ý (AI không tự commit — rule 4.2)

```
feat(sharedata): XD001.5.6 - trả cờ IsInUse cho API phân trang gói tin (BE-16)
```
