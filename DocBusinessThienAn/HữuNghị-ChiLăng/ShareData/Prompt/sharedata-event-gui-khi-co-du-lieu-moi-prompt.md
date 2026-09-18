# Prompt thực thi — Cờ "Gửi ngay khi có dữ liệu mới": polling + cursor theo từng gói

> **Tài liệu sống**: `../Plan/sharedata-outbound-kiem-tra-anh-xa-va-dinh-dang.md` — mục 3.5, mục 4 **S2**
> **Nguồn nghiệp vụ**: `../doc/transcript/2026-09-09-review-sharedata.md` và
> `../doc/02-mapping-goi-tin-101-111.md`

## Mục tiêu

Không dùng NATS/CDC trong đợt này. Khi cờ do WebAPI cung cấp được bật, worker thăm dò CSDL theo
`IntervalSeconds`, chỉ lấy các dòng **mới · cập nhật · soft-delete** kể từ cursor đã giao thành công,
rồi gửi đúng các dòng đó. Khi cờ tắt, worker chạy theo lịch và phạm vi lấy dữ liệu bình thường của
từng gói.

**Khả thi trên pipeline hiện tại**: Scheduler → Extraction → Mapping → Transport đã có; query handler
đã nhận `lastTime`/`lastId`, extraction đã trả `MaxWatermark`/`MaxLastId`. Phần còn thiếu là cờ thật,
lưu `LastDataId`, dùng `lastId` trong query, chuẩn hóa thời điểm thay đổi và chỉ tiến cursor sau HTTP
thành công.

---

## Vì sao prompt cũ bị thay thế

Prompt cũ giả định 6 gói là "snapshot", thêm một `MAX(__watermark)` rồi nếu dữ liệu đổi thì vẫn gửi
lại toàn bộ. Sau khi đọc đủ 7 transcript và tài liệu mapping 101–111, giả định đó không phải lời chốt
nghiệp vụ:

- Anh Sơn 09/09: bật switch thì khi DB phát sinh bản ghi mới, hệ thống đẩy gói tin ngay; tắt thì chỉ
  chạy theo lịch.
- Biên bản 16/09: phát hiện dữ liệu mới dựa `CreateTime`/`UpdateTime`.
- Tài liệu mapping tách riêng gói `all/update`, gói `>= key`, và gói `skip`.
- Không có nguồn nào chốt `LastPayloadHash` hoặc "một dòng đổi thì gửi lại toàn bộ".

**Quyết định dùng cho prompt này**: cờ bật ⇒ gửi **phần dòng mới/cập nhật/soft-delete**. Vì vậy:

- **KHÔNG thêm `LastPayloadHash`.**
- **KHÔNG thêm MAX watermark cho 6 gói rồi chỉ làm send-gate.**
- Dùng cursor `(LastTimeRun, LastDataId)` cho từng subscription.
- Hardcode hiện tại là hành vi đang chạy, **không phải nguồn sự thật nghiệp vụ**; không được giữ một
  query chỉ vì comment đang ghi `SNAPSHOT`.

---

## Điều kiện trước — thiếu thì DỪNG và báo

1. 🟢 **ĐÃ CÓ — hết chờ.** Hiếu bàn giao cột **`ShareDataSubscription.SendOnNewData`** (`bool?`) ở commit
   `7f035e63` (18/09). Ngữ nghĩa Hiếu khai trong chú thích entity:

   > *"Gửi ngay khi có dữ liệu mới: bật thì ngoài lịch định kỳ, service còn dò bản ghi mới của gói tin
   > này và gửi ngay. **Null hoặc false = chỉ gửi theo lịch.** **Chỉ có nghĩa với đăng ký GỬI ĐI; chiều nhận
   > không dùng."*

   ⚠️ `null` và `false` **cùng nghĩa** — chỉ gửi theo lịch. Đừng coi `null` là trạng thái thứ ba.
2. 🔴 **Cột `LastDataId` CHƯA TỒN TẠI** — đo 18/09: `grep -rn "LastDataId" src/` ra **0**.
   Chốt anh Đạt: **worker tự thêm**.

   - Thêm property vào `Module.ShareData.Core/Entities/ShareDataSubscription.cs`, **đặt cạnh
     `SendOnNewData`** cho dễ đối chiếu.
   - 🔴 **Chỉ xuất tệp `.sql`** để người dùng review và chạy tay. **KHÔNG tự chạy DDL**
     (rule *Strict Manual SQL Execution*).
   - ⚠️ **Báo Hiếu trước khi đụng tệp này** — anh ấy vừa sửa chính nó ở commit `7f035e63`. Hai người
     sửa cùng lúc là đụng nhau.
3. Với soft delete, đối tác phải có contract biểu diễn thao tác xoá. Target shape phải có binding từ
   `$field: "__operation"` sang field đã thống nhất (`operation`, `isDeleted`...). Nếu batch có dòng
   delete mà shape không có binding này: **huỷ batch, ghi cảnh báo, không tiến cursor**.
4. Gói 110 chưa đủ nguồn/contract; gói 111 được tài liệu ghi `skip`. Không tự chế dữ liệu để ép hai
   gói này chạy.

---

## Nền thực tế — đo ngày 18/09

Đừng đoán, đây là con số thật để biết chỗ nào có sẵn, chỗ nào phải dựng từ đầu:

| Prompt cần | Thực tế |
|---|---|
| Cờ bật/tắt | 🟢 `SendOnNewData` |
| Cột audit dựng `ChangeTime` | 🟢 `EntityTenant` có `CreateTime`·`UpdateTime`·`IsDelete` — **cả 7 bảng nguồn kế thừa** |
| `DataOutboundExtractionResult.MaxLastId` · handler nhận `lastId` · `ExecuteExportForSubscription` trả `LastId` | 🟢 **đã có sẵn** |
| `ProcessBatchSubscriptions` dùng `LastId` | 🔴 **đang BỎ** — dòng ~171 chỉ lấy `result.LastTimeRun`. **Đây là chỗ nối lại cursor** |
| Cột `LastDataId` | 🔴 **0** |
| Bí danh `__operation` | 🔴 **0** |
| Query dùng `@lastId` | 🔴 **0 / 11** |
| Query có cursor thời gian | 🟠 **5 / 11** |
| Query lọc `IsDelete IS NULL` | 🔴 **0 / 11** |

---

## ⚠️ RỦI RO 1 — `UpdateTime` là NULL ở dòng vừa INSERT

`Shared.Core.dll` không có mã nguồn trong repo. Nơi **duy nhất** ghi lại cờ SqlSugar của `EntityTenant`
là chú thích ở `Services/WP_Sync/src/ITS.Sync.Infrastructure/Persistence/SqlSugarFactory.cs:31-39`:

> `CreateTime : InsertServerTime = true` → INSERT tự ghi `GETDATE()`
> `UpdateTime : UpdateServerTime = true` → UPDATE tự ghi `GETDATE()`
> `UpdateTime : IsOnlyIgnoreInsert = true` → **bị LOẠI khỏi INSERT (nên bản ghi mới = null)**

⇒ **Cursor dựa trên `UpdateTime` trần sẽ KHÔNG BAO GIỜ bắt được dòng vừa INSERT.** `COALESCE` ở mục
*"Thời điểm thay đổi một bảng"* đã chặn đúng chỗ này — `UpdateTime` NULL thì rơi xuống `CreateTime`.

**Hiện trạng 5/11 gói có cursor.** Cột cuối chỉ trả lời **một câu hỏi duy nhất**: gói đó có dính bẫy
`UpdateTime` NULL không — **không** có nghĩa là gói đó đã đúng và được để nguyên:

| Gói | `__watermark` hiện tại | Dính bẫy `UpdateTime` NULL? | Việc S2 vẫn phải làm |
|---|---|---|---|
| 103 · 106 | `td.DetectTime` | 🟢 không — cột nghiệp vụ | ⚠️ **vẫn phải đổi sang audit time**, `DetectTime` chỉ còn là fallback (xem *Ma trận nguồn*) |
| 104 | `w.TimeDetect` | 🟢 không — cột nghiệp vụ | ⚠️ như trên |
| 109 | `t.TransactionDateTime` | 🟢 không — cột nghiệp vụ | ⚠️ như trên |
| **107** | `ISNULL(i.UpdateTime, i.StartDate)` | 🟢 không — **đã bọc `ISNULL`** | đổi `>=` thành cursor kép; **giữ nguyên khuôn `ISNULL`** |

⚠️ **Đừng đọc cột giữa thành "khỏi sửa".** Bốn gói `103·104·106·109` đang lấy **thời gian nghiệp vụ**
làm cursor — chúng thoát bẫy `UpdateTime` NULL chỉ vì **không đụng cột audit**, nhưng lại dính lỗi
khác: dòng insert trễ mà mang thời gian nghiệp vụ cũ thì cursor bỏ sót. Chuyển sang audit time là
**bắt buộc**, và ngay khi chuyển thì **bẫy `UpdateTime` NULL bắt đầu áp dụng cho chúng**.

🔴 **Mọi cursor S2 dựng hoặc sửa — 6 gói chưa có (101·102·105·108·110·111) lẫn 4 gói chuyển sang audit
time — đều CẤM dùng `UpdateTime` trần.** Bám khuôn gói 107 / khuôn `COALESCE` ở dưới. Sai chỗ này là
**mất dữ liệu im lặng, không báo lỗi gì cả**.

⚠️ **Ranh giới CSDL — đọc trước khi lo về WP_Sync.** WP_Sync đọc `DEV_ITS10` và **chỉ ghi vào**
`DEV_ITS015_WP` (`DbConstants.SourceDb`/`TargetDb`). Luồng gửi đọc `DEV_ITS10`
(`ShareDataWorker/appsettings.json` → `DefaultConnection`) ⇒ **hai bên hiện KHÔNG giao nhau**, WP_Sync
không đụng bảng nào luồng gửi đọc. Rủi ro "mirror timestamp làm dòng về sau mà mang mốc cũ" **chỉ sống
lại nếu có ngày luồng gửi bị trỏ sang `DEV_ITS015_WP`** — lúc đó mới phải đo lại cursor.

---

## 🔴 RỦI RO 2 — `IsDelete` phải lọc, nhưng **KHÔNG được lọc ở cả hai nhánh**

**Hiện trạng:** worker chiều gửi lọc `IsDelete == null` cho **bảng cấu hình**
(`DataOutboundService.cs:80`·`:84` — subscription, partner) nhưng **0/11** câu truy vấn **dữ liệu
nghiệp vụ** có lọc. ⇒ Sự cố đã xoá mềm trên giao diện **vẫn đang được gửi cho đối tác** như dữ liệu sống.

🟢 **Anh Đạt chốt: phải lọc.** Đây là sửa lỗi, không phải tuỳ chọn.

🔴 **Nhưng chỉ lọc ở nhánh "query all". Nhánh "changed" mà lọc là HỎNG.**

| Nhánh | `IsDelete` | Vì sao |
|---|---|---|
| **query all** (`FullOrChanged` + cờ tắt) | ✅ `WHERE main.IsDelete IS NULL` | đối tác nhận lại trọn bộ mỗi lần ⇒ dòng đã xoá **biến mất là đúng** |
| **query changed** (cờ bật · `AlwaysIncremental`) | ❌ **KHÔNG lọc trước** | dòng vừa bị xoá mềm **chính là thay đổi cần báo** ⇒ gửi kèm `__operation = 'delete'` |

⚠️ **Lọc nhầm ở nhánh changed thì mất tin xoá vĩnh viễn.** Đối tác chỉ thấy dòng **im lặng ngừng xuất
hiện**, không có cách nào phân biệt *"bị xoá"* với *"chưa tới lượt gửi"*. Gói **107 — sự cố giao thông**
là chỗ đau nhất: sự cố bị huỷ/nhập nhầm là chuyện thường ngày, đối tác phải biết để gỡ khỏi bảng tin.

### Vẫn phải đếm trước khi sửa

Không phải để xin phép nữa, mà để **biết trước cú sốc số liệu**: đếm `IsDelete IS NOT NULL` ở 7 bảng
nguồn. Đó đúng bằng lượng dòng đối tác **sẽ thôi nhận** ngay lần chạy đầu sau khi sửa. Con số lớn bất
thường ⇒ báo để bên vận hành khỏi tưởng hệ thống chết.

⚠️ Chuyện luồng gửi **chưa bao giờ lọc `IsDelete`** là **lỗi độc lập với S2** — đã ghi vào mục 5 tài
liệu sống.

---

## Ngữ nghĩa cờ và lịch chạy

Cô lập việc đọc cờ tại **đúng một hàm**:

```csharp
/// <summary>
/// Description: Xác định subscription có tự động thăm dò và gửi phần dữ liệu mới hay không.
/// Cờ thật do WebAPI cung cấp (`SendOnNewData`); toàn worker chỉ đọc qua hàm này.
/// Null hoặc false = chỉ gửi theo lịch. Chỉ có nghĩa với đăng ký chiều GỬI.
/// Created date: 18/09/2026
/// </summary>
public static bool IsSendOnNewDataOnly(ShareDataSubscription sub)
    => sub.SendOnNewData == true;
```

Không nơi nào khác đọc trực tiếp property cờ. Không thêm fallback `Mode == Event` sau khi cờ thật có
mặt.

| Cờ | Scheduler | Phạm vi dữ liệu |
|---|---|---|
| **Tắt** | lịch `continuous` / `daily` hiện hữu | theo policy cố hữu của gói |
| **Bật** | polling mỗi `IntervalSeconds` (default hiện hữu nếu rỗng) | phần mới/cập nhật/soft-delete sau cursor |

Không có NATS nên chữ "ngay" nghĩa là **độ trễ tối đa xấp xỉ `IntervalSeconds` + thời gian xử lý**.
Bỏ hằng `EventPollIntervalSeconds = 5`; không hardcode nhịp quét. Nếu cần gần thời gian thực, cấu hình
`IntervalSeconds = 5` và đo tải.

Khi cờ bật, không chờ mốc `daily`; dùng polling liên tục. Có thể giữ khung `StartTime`/`EndTime` của
`continuous` để giới hạn giờ hoạt động. Một `NextTimeRun` không biểu diễn đồng thời cả polling và một
lịch daily fallback; fallback định kỳ là việc riêng nếu nghiệp vụ yêu cầu sau này.

---

## Policy lấy dữ liệu theo gói

Không dùng `Snapshot/Incremental` từ comment hiện tại làm business truth. Thêm một policy tường minh
(đặt cạnh `PacketMetadataResolver`) và viện dẫn tài liệu mapping:

| Policy | Gói | Cờ tắt | Cờ bật |
|---|---|---|---|
| `FullOrChanged` | 101 · 102 · 105 · 108 | lấy all theo lịch | lấy dòng mới/cập nhật/xoá |
| `AlwaysIncremental` | 103 · 104 · 106 · 107 · 109 | lấy sau cursor theo lịch | lấy sau cursor theo nhịp polling |
| `NotReady` | 110 | không mở rộng trong S2 | dừng rõ ràng, không tự chế nguồn |
| `Disabled` | 111 | tài liệu ghi `skip` | không query `TmsIncident` thay thế |

Cờ áp dụng cho mọi subscription, nhưng với `AlwaysIncremental` nó chủ yếu đổi **thời điểm kiểm tra**;
phạm vi dòng vốn đã tăng dần dù cờ tắt. Không bao giờ để cờ tắt làm 103/104/106/107/109 gửi lại toàn
bộ lịch sử.

---

## Chuẩn nội bộ chung cho mọi query

Mỗi `QueryPacketNNN` tự biết bảng/cột nguồn, nhưng phải trả ba alias nội bộ thống nhất:

```text
__watermark  = thời điểm DB thay đổi dòng nghiệp vụ
__rowid      = khoá ổn định để phân trang khi trùng thời gian
__operation  = upsert | delete
```

- `__watermark` và `__rowid` **không bao giờ** xuất hiện trong JSON đối tác.
- `__operation` chỉ ra ngoài nếu `TargetShapeJson` chủ động bind nó.
- Không dùng `DetectTime`/`TransactionDateTime` làm cursor chính nếu đó chỉ là thời gian nghiệp vụ:
  dữ liệu có thể được insert trễ nhưng mang thời gian cũ.

### Thời điểm thay đổi một bảng

Các entity dự án kế thừa `CreateTime`, `UpdateTime`, `IsDelete` (`IsDelete` là thời điểm soft delete):

```sql
COALESCE(t.IsDelete, t.UpdateTime, t.CreateTime, <domainTime>)
```

Thứ tự có nghĩa:

1. Đã xoá mềm ⇒ thời điểm xoá.
2. Đã cập nhật ⇒ thời điểm cập nhật.
3. Mới tạo ⇒ thời điểm tạo.
4. Chỉ fallback về thời gian nghiệp vụ nếu ba cột audit đều rỗng; phải ghi warning.

🔴 **Bậc 3 không phải cho đẹp đội hình.** `EntityTenant` loại `UpdateTime` khỏi INSERT (rủi ro 1), nên
**dòng vừa tạo luôn có `UpdateTime = NULL`** — bỏ `CreateTime` khỏi `COALESCE` là mất trắng mọi dòng mới.

### Query JOIN nhiều bảng

Nếu output lấy từ nhiều bảng, `__watermark` là thời điểm lớn nhất của **mọi nguồn đóng góp vào dòng**.
Ví dụ khung cho gói 101:

```sql
CROSS APPLY
(
    SELECT MAX(sourceTimes.ChangeTime) AS ChangeTime
    FROM
    (
        VALUES
            (COALESCE(zs.IsDelete, zs.UpdateTime, zs.CreateTime)),
            (COALESCE(z.IsDelete, z.UpdateTime, z.CreateTime)),
            (COALESCE(ts.IsDelete, ts.UpdateTime, ts.CreateTime))
    ) sourceTimes(ChangeTime)
) changeInfo
```

Xoá bảng chính ⇒ `__operation = 'delete'`. Xoá/cập nhật bảng join nhưng bản ghi chính còn sống ⇒ đó là
`upsert` của bản ghi chính với nội dung mới, không phải delete bản ghi chính.

---

## Cursor chính xác — `LastTimeRun` + `LastDataId`

### Bổ sung field duy nhất thuộc phạm vi worker

`Module.ShareData.Core/Entities/ShareDataSubscription.cs`:

```csharp
[SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength,
    ColumnDescription = "ID dòng dữ liệu cuối đã gửi thành công tại LastTimeRun")]
public string? LastDataId { get; set; }
```

Tạo script SQL tương ứng để review/chạy tay, **không tự thực thi**. Không thêm `LastPayloadHash` hoặc
`LastDataWatermark`.

### Vì sao cần cặp hai giá trị

Nhiều dòng có thể cùng timestamp. Với `TOP 50`, chỉ lưu thời gian sẽ đọc lặp 50 dòng đầu. Mọi query
changed/incremental phải dùng keyset cursor:

```sql
WHERE changeInfo.ChangeTime > @lastTime
   OR (
       changeInfo.ChangeTime = @lastTime
       AND businessRow.ID > @lastId
   )
ORDER BY changeInfo.ChangeTime, businessRow.ID
```

Không dùng `>= @lastTime` một mình. Dùng parameter SqlSugar; không nối dữ liệu vào SQL.

`DataOutboundExtractionProcess.Extract` đổi:

```csharp
var lastTime = sub.LastTimeRun;
var lastId = sub.LastDataId ?? string.Empty;
```

Các handler hiện đã nhận `lastId`; sửa chúng dùng thật. `DataOutboundExtractionResult` và
`ExecuteExportForSubscription` đã có `MaxLastId`/`LastId`; không tạo DTO trùng.

---

## Query theo cờ — không dùng OR làm hỏng index

Mở rộng query context/handler để nhận `onlyNewData`. Mỗi handler chọn câu SQL/nhánh filter trong C#:

```text
FullOrChanged + cờ tắt → query all, chỉ IsDelete IS NULL
FullOrChanged + cờ bật → query cursor, gồm cả soft-delete
AlwaysIncremental       → query cursor ở cả hai trạng thái cờ
```

Không viết `WHERE @onlyNew = 0 OR ...` cho mọi query nếu làm SQL Server bỏ index seek. Clause là mã
hardcode đã kiểm soát; giá trị cursor luôn truyền parameter.

### Full query

```sql
WHERE main.IsDelete IS NULL
ORDER BY <business-key>
```

Nếu dùng `TOP N`, phải phân trang đến hết; không được gọi `TOP 50` là "all" rồi gửi thiếu dữ liệu.

### Changed query có soft delete

Không lọc `IsDelete IS NULL` trước khi phát hiện thay đổi:

```sql
SELECT
    ...,
    changeInfo.ChangeTime AS __watermark,
    main.ID AS __rowid,
    CASE WHEN main.IsDelete IS NULL THEN 'upsert' ELSE 'delete' END AS __operation
FROM ...
WHERE changeInfo.ChangeTime > @lastTime
   OR (changeInfo.ChangeTime = @lastTime AND main.ID > @lastId)
ORDER BY changeInfo.ChangeTime, main.ID
```

Nếu target shape không bind `__operation` mà batch có delete, fail-safe: cảnh báo lỗi cấu hình, không
gửi dòng xoá như một upsert và không tiến cursor.

---

## Ma trận nguồn cần hoàn thiện

| Gói | Policy | Bảng chính / row ID | Nguồn `ChangeTime` phải xét | Trạng thái |
|---|---|---|---|---|
| 101 | FullOrChanged | `TmsZoneStatus` / `ZoneId` | ZoneStatus + Zone + bản thống kê được chọn | phải sửa chọn latest/aggregate trước, tránh nhân dòng |
| 102 | FullOrChanged | `CctvDevice` / `ID` | CctvDevice + Equipment | join IP chỉ là tạm; code đang `NULL AS snapshot` |
| 103 | AlwaysIncremental | `TmsTrafficData` / `ID` | audit time của TrafficData; `DetectTime` chỉ fallback | sửa `>=` thành cursor kép |
| 104 | AlwaysIncremental | `TmsWeather` / `ID` | audit time Weather; `TimeDetect` chỉ fallback | sửa cursor kép |
| 105 | FullOrChanged | giao dịch / `ID` | Transaction In/Out + VehicleRegistration | code đang thiếu nguồn In |
| 106 | AlwaysIncremental | dữ liệu WIM / `ID` | audit time nguồn WIM | nguồn WIM thật chưa chốt; không suy diễn |
| 107 | AlwaysIncremental | `TmsIncident` / `ID` | Incident + EventType | phải bắt cả sự cố cũ đổi trạng thái |
| 108 | FullOrChanged | VMS / `EquipmentId` | VmsCurrent + Equipment | staging có nhiều dòng/EquipmentId; chọn dòng mới nhất trước |
| 109 | AlwaysIncremental | giao dịch / `ID` | Transaction In/Out + Lane + Station | code thiếu In và đang `tollPrice = NULL` |
| 110 | NotReady | chưa có `messageId` bền vững | Incident + Weather + VMS/outbox | dừng, không mở rộng query sai hiện tại |
| 111 | Disabled | — | — | tài liệu ghi `skip`; bỏ khỏi S2 |

S2 không được âm thầm "sửa gần đúng" các lỗ hổng nguồn. Handler nào chưa đủ điều kiện phải báo rõ
`NotReady`, không gửi payload sai.

---

## Lần đầu bật cờ

Ngữ nghĩa đợt này là **chỉ dữ liệu phát sinh sau khi bật**, không backfill toàn bộ lịch sử.

API/WebAPI khi chuyển cờ từ tắt → bật phải khởi tạo:

```text
LastTimeRun = thời điểm bật/duyệt subscription
LastDataId  = null
```

Nếu cờ đã bật nhưng `LastTimeRun == null`, worker **không** được tự dùng năm 1900 rồi gửi toàn bộ lịch
sử. Ghi lỗi cấu hình và dừng subscription đó cho tới khi có cursor nền.

Nếu sau này cần gửi baseline đầu tiên, đó là option nghiệp vụ riêng; không trộn vào S2.

---

## Luồng service và persist

`DataOutboundService.ProcessBatchSubscriptions` hiện bỏ `result.LastId`. Nối lại đường đã có:

1. Nhận cả `result.LastTimeRun` và `result.LastId`.
2. Truyền cả hai vào `TryPersistExportResult`.
3. Chỉ set `LastTimeRun` + `LastDataId` + tăng `SerialNbr` khi HTTP thành công.
4. Không có dòng mới: ghi `Success + NoNewData`, chỉ tính `NextTimeRun`; giữ cursor và `SerialNbr`.
5. HTTP thất bại: giữ cursor cũ để lần sau query/gửi lại.
6. Lưu cặp cursor trong cùng update có điều kiện lease; mất lease thì không ghi nửa cặp.

Bảng trạng thái:

| Kết quả | `LastTimeRun` | `LastDataId` | `SerialNbr` | `NextTimeRun` |
|---|---|---|---|---|
| không có dòng mới | giữ | giữ | giữ | tiến |
| mapping lỗi | giữ | giữ | giữ | tiến theo cơ chế hiện hữu |
| HTTP lỗi | giữ | giữ | giữ | tiến để retry |
| HTTP thành công | max watermark | row ID cùng watermark | +1 | tiến |

---

## Scheduler không NATS

`DataOutboundScheduler`:

- Gỡ nhánh `Mode == Event → 5s` và hằng `EventPollIntervalSeconds`.
- Cờ bật: `now + IntervalSeconds` (hoặc default), dùng như nhịp **kiểm tra dữ liệu**, không phải gửi vô
  điều kiện; có thể kẹp khung giờ continuous.
- Cờ tắt: giữ nguyên continuous/daily.
- Không dọn `SubMode.Single`, `EventSourceId`, `DebounceSec` trong S2.

---

## Kiểm thử bắt buộc

Tệp chính: `tests/ShareData/Services/DataOutboundServiceTests.cs` (hoặc lớp test DataOutbound hiện
hữu; không tạo suite trùng).

| Ca | Kỳ vọng |
|---|---|
| cờ bật, chưa đến interval | chưa query |
| cờ bật, cursor hợp lệ, query 0 dòng | không gọi sender; `Success + NoNewData`; cursor giữ nguyên |
| cờ bật, có insert | gửi `upsert`; thành công mới tiến cặp cursor |
| cờ bật, có update | gửi lại dòng cũ với nội dung mới |
| cờ bật, có soft delete + shape có operation | gửi `delete` |
| soft delete + shape thiếu binding operation | fail batch; không tiến cursor |
| HTTP lỗi | không tiến cả hai cursor |
| 100 dòng cùng timestamp, TOP 50 | lần 1 lấy 1–50; lần 2 lấy 51–100, không lặp |
| dữ liệu insert trễ nhưng domain time cũ | vẫn bắt bằng `CreateTime` |
| 🔴 **dòng vừa INSERT, `UpdateTime = NULL`** *(rủi ro 1)* | **vẫn bắt được** bằng `CreateTime` — ca này chốt khuôn `COALESCE`, thiếu nó thì bẫy `UpdateTime` trần lọt qua test |
| cờ tắt + FullOrChanged | lấy all, loại `IsDelete != null` |
| cờ tắt + AlwaysIncremental | vẫn lấy sau cursor, không dump lịch sử |
| gói 110 | NotReady rõ ràng |
| gói 111 | không chạy query incident thay thế |

### Nghiệm thu

```text
dotnet build src/Services/ShareData/ShareDataWorker/ShareDataWorker.csproj
dotnet test tests/test.csproj --filter "FullyQualifiedName~Services.DataOutbound"
```

Trước `dotnet test`, bắt buộc kiểm connection string test chỉ trỏ local (`localhost`, `127.0.0.1`,
`(localdb)`, `.`). Thấy IP xa như `10.10.8.30` thì huỷ test và báo; không chạy.

---

## Không làm

- Không dùng NATS, CDC, trigger SQL hoặc physical-delete tracking trong S2.
- Không thêm `LastPayloadHash`.
- Không dùng `Mode = Event` làm cờ nữa — đã có `SendOnNewData`.
- Không áp cờ cho đăng ký **chiều NHẬN** — Hiếu ghi rõ cờ chỉ có nghĩa với chiều GỬI. Anh Đạt chốt
  18/09: chiều nhận **không cần ô tương ứng** (việc **SV-13** đã bị bỏ khỏi kế hoạch) — bên nhận có dữ
  liệu mới là do đối tác đẩy sang, mình không dò. **Đừng tự đẻ cờ mới cho chiều nhận.**
- 🔴 Không dùng **`UpdateTime` đứng trần** làm `__watermark` — `EntityTenant` loại nó khỏi INSERT nên
  dòng mới luôn `NULL` (rủi ro 1). Luôn bọc `COALESCE`/`ISNULL` có `CreateTime`.
- Không dùng `DetectTime`/`TransactionDateTime` thay audit time nếu `CreateTime`/`UpdateTime` có dữ liệu.
- Không tự chạy DDL/DML; chỉ ghi tệp `.sql` để review.
- Không thay đổi luồng NHẬN.
- Không tự tạo contract delete cho đối tác; thiếu binding thì dừng an toàn.
- Không coi comment `SNAPSHOT` trong hardcode là bằng chứng nghiệp vụ.
- Không tự làm gói 110/111 bằng dữ liệu gần đúng.

## Tự kiểm trước khi báo xong

| Kiểm | Kỳ vọng |
|---|---|
| `grep -rn "SendOnNewData" src/` | **đúng 1** chỗ đọc — trong `IsSendOnNewDataOnly` |
| `grep -c "@lastId" .../DataOutboundExtractionProcess.cs` | **> 0** (trước khi sửa: **0**) |
| `grep -rn "__operation" src/` | **> 0** (trước: **0**) |
| `grep -rn "LastDataId" src/` | có property trong entity **+** có tệp `.sql` chưa chạy |
| `grep -c "IsDelete IS NULL" .../DataOutboundExtractionProcess.cs` | **> 0** (trước: **0**) |
| Soi từng câu **changed query** | 🔴 **không** câu nào có `IsDelete IS NULL` trong `WHERE` — nếu có là **mất tin xoá** |
| `grep -rn "EventPollIntervalSeconds" src/` | **0** |
| Soi từng `__watermark` mới dựng | **không có** `UpdateTime` đứng trần — phải nằm trong `COALESCE`/`ISNULL` |

## Báo cáo khi xong

1. Xác nhận worker chỉ đọc `SendOnNewData` ở **một** hàm duy nhất.
2. Field `LastDataId`/script SQL đã tạo ở đâu; xác nhận **chưa chạy DDL** và **đã báo Hiếu**.
3. ⚠️ **Rủi ro 1** — liệt kê biểu thức `__watermark` của **từng gói vừa dựng cursor**, chứng minh
   không gói nào dùng `UpdateTime` trần.
4. 🔴 **Rủi ro 2** — số dòng `IsDelete IS NOT NULL` ở 7 bảng nguồn (lượng dữ liệu đối tác **thôi
   nhận**), **và** xác nhận nhánh *changed* **không** lọc `IsDelete` trước khi phát hiện thay đổi.
5. Policy thực tế của từng gói 101–111 và handler nào còn `NotReady`.
6. Với mỗi handler đã bật: biểu thức audit `__watermark`, `__rowid`, cách xử lý soft delete.
7. Số test trước/sau; kết quả ca cùng timestamp, HTTP retry và soft delete.
8. Xác nhận không còn query `>= @lastTime` một mình ở handler phân trang và không còn nhánh Event
   hardcode 5 giây.
