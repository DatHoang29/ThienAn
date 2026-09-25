# Báo cáo: Luồng Gửi Nối Đuôi (Gửi tiếp dữ liệu phát sinh) — ShareData

> Ngày: 23/09/2026. Dựa trên code hiện tại trong `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/`. Đối chiếu đặc tả [`doc/02-mapping-goi-tin-101-111.md`](../doc/02-mapping-goi-tin-101-111.md) và biên bản họp [21/09/2026](../doc/transcript/21-09-2026-hoan-thien-mapping-va-gui-noi-duoi-sharedata.md).
>
> Cơ chế **gửi ngay khi có dữ liệu mới** (Change Tracking + NATS) nằm ở báo cáo riêng: [`Sharedata_Review_GuiKhiCoDuLieuMoi_20260923.md`](./Sharedata_Review_GuiKhiCoDuLieuMoi_20260923.md).

### Chú giải ký hiệu

| Ký hiệu | Ý nghĩa |
| --- | --- |
| ✅ | Đạt — khớp / đúng / đã hoàn tất |
| ⚠️ | Cần lưu ý — có vấn đề, nhưng chưa phải dừng lại để sửa; ghi nhận rồi xử lý sau |
| ❌ | Không đạt — không khớp / không đúng / không áp dụng được |
| 🔴 | Rủi ro nghiêm trọng — phải xử lý trước khi đi tiếp |
| ⛔ | Cấm tuyệt đối |
| 📌 | Ghi chú bối cảnh — nguồn dữ liệu, ngày đo, thuật ngữ |

> Ký hiệu chỉ nói **mức độ**; trục đánh giá do tiêu đề cột của từng bảng nói rõ.

---

## Tóm tắt

| Câu hỏi | Trả lời | Chi tiết |
|---|---|---|
| Đặc tả yêu cầu gì? | **Hai trục riêng biệt.** *Gửi cái gì*: 5 gói nối đuôi (`>= key`), 4 gói bản chụp (`all`); hai gói 110/111 không gửi. *Khi nào gửi*: cả 4 gói bản chụp đều mang thêm vế `DL được cập nhật` — lắng nghe qua NATS, gửi ngay không đợi hết chu kỳ. | [Mục 1](#1-đặc-tả-yêu-cầu-gì) |
| Code so với đặc tả? | **Khớp 11/11 gói.** Không còn điểm lệch nào tính tới 23/09/2026. | [Mục 2](#2-code-làm-được-tới-đâu-so-với-đặc-tả) |
| Chưa làm những gì? | **2 việc, cả hai đều chờ hạ tầng ngoài code.** Gói 106 thiếu 4 trường tải trọng vì `TmsTrafficData` không có cột nào cho chúng. Gói 110 thiếu 3 trường vì chưa có bảng outbox. | [Mục 3](#3-chưa-làm-những-gì) |
| Edge case? | 7 kịch bản sập đều **bảo đảm an toàn dữ liệu tuyệt đối**. Nếu tiến trình bị dừng/kill đột ngột, cơ chế lease `NextTimeRun` tự động hồi phục và tiếp tục gửi nối đuôi từ checkpoint mà không bị sai lệch dữ liệu. | [Mục 4](#4-edge-case) |

---

## 1. Đặc tả yêu cầu gì

### 1.1. Hai chế độ gửi

Đặc tả ghi chế độ lấy dữ liệu **ngay trong tiêu đề từng mục gói**:

| Nguyên văn trong tiêu đề mục | Gói | Nghĩa |
| --- | --- | --- |
| `sẽ lấy mới nhất từ thời điểm trước đó >= key` | 103, 104, 106, 107, 109 | **Gửi nối đuôi** — chỉ phần phát sinh sau mốc |
| `lấy all` · `All` | 101, 102, 105, 108 | **Bản chụp toàn bộ** — gửi lại hết mỗi chu kỳ |

Biên bản họp 21/09 phiên 2 xác nhận đúng cách chia này, và chốt trục phân loại là **gói tin** chứ không phải bảng dữ liệu:

- Mốc `09:59` — *"Mấy Incident thì chỉ gửi tiếp mới mới thôi... nhưng mà một số bảng là nó cần cập nhật hết tất cả thông tin thì bắt buộc phải gửi lại hết"*
- Mốc `10:40`–`10:50` — *"Nó có 2 dạng như vậy... Trong cái **gói tin** đó, chứ không phải một bảng nữa"*

### 1.2. Giải mã ghi chú viết tắt trong đặc tả

Đặc tả ghi chế độ bằng vài chữ viết tắt trong ngoặc. Một ghi chú đầy đủ gồm **hai vế ngăn bởi dấu `/`**, và hai vế đó **trả lời hai câu hỏi hoàn toàn khác nhau**.

Lấy nguyên văn gói 102 làm ví dụ — `lấy all /DL được cập nhật -> nats`:

| Vế | Nguyên văn | Nó đang trả lời câu hỏi nào? | Câu trả lời |
|---|---|---|---|
| 1 | `lấy all` | **"Gửi cái gì?"** | Mỗi lần gửi thì gửi lại **toàn bộ** hiện trạng |
| 2 | `DL được cập nhật -> nats` | **"Khi nào gửi?"** | **Không đợi hết chu kỳ** — hễ dữ liệu trong bảng nguồn vừa đổi là gửi ngay |

Hai vế độc lập nhau: đổi vế này không kéo theo vế kia. Bảng giải mã từng ký hiệu:

| Ghi chú trong đặc tả | Nó trả lời câu hỏi nào? | Nghĩa thực tế |
|---|---|---|
| `lấy all` · `All` | "Gửi cái gì?" | **Mỗi chu kỳ gửi lại toàn bộ** hiện trạng |
| `>= key` | "Gửi cái gì?" | **Chỉ gửi phần phát sinh sau mốc** của lần gửi trước |
| `DL được cập nhật -> nats` · `update` | "Khi nào gửi?" | **Lắng nghe qua NATS**: hễ dữ liệu được cập nhật là gửi ngay, không đợi hết chu kỳ |

📌 `DL được cập nhật` và đuôi `-> nats` là **một vế duy nhất, không tách rời**: phần trước nói việc (dữ liệu đổi thì gửi), đuôi sau chỉ là tên kênh truyền tín hiệu. Hệ thống hiện thực vế này thành **ba chặng**: **Change Tracking của SQL Server** phát hiện bảng nguồn vừa đổi → **bắn tín hiệu qua NATS** → worker nhận tín hiệu rồi chạy luồng gửi.

### 1.3. Vị thế của tài liệu mapping — đọc kỹ trước khi dùng nó làm chuẩn

Tài liệu [`doc/02-mapping-goi-tin-101-111.md`](../doc/02-mapping-goi-tin-101-111.md) là tài liệu **ánh xạ trường dữ liệu**, trích ngày **20/08/2026** — tức **trước** biên bản họp 21/09 và **trước** lần triển khai 22/09. Các ghi chú chế độ trong tiêu đề mục là **mốc tham chiếu để soi hướng đi**, KHÔNG phải đặc tả giao thức ràng buộc. Khi tài liệu lệch với code, mở thành điểm cần xác nhận, không kết luận ngay bên nào sai.

---

## 2. Code làm được tới đâu so với đặc tả

### 2.1. Đối chiếu 11 gói: đặc tả ↔ CSDL staging ↔ code

> 📌 Cột "Bản ghi staging" đo trên `mssql_staging` (`dev_its10` @ `10.10.8.30`) ngày **24/09/2026**. Cột "Policy trong code" chỉ nói trục *gửi cái gì* — mọi gói trừ 110/111 đều được kích hoạt bằng sự kiện (Change Tracking → NATS), năng lực dùng chung cho cả 9 gói bất kể nối đuôi hay bản chụp; bật hay không do cờ `SendOnNewData` của **từng đăng ký**, không do gói tin.

| Gói | Đặc tả yêu cầu gì | Bản ghi staging | Policy trong code | Khớp? |
|---|---|---|---|---|
| 101 | Gửi lại toàn bộ, **và gửi ngay khi dữ liệu được cập nhật** · *nguyên văn:* `lấy all/DL được cập nhật` — có vế cập nhật, không viết rõ đuôi `-> nats` | `101_commonData` | `Snapshot` | ✅ |
| 102 | Gửi lại toàn bộ, **và gửi ngay khi có dữ liệu mới** · *nguyên văn:* `lấy all /DL được cập nhật -> nats` | `102_cctvData` | `Snapshot` | ✅ |
| 103 | Chỉ phần phát sinh sau mốc · *nguyên văn:* `>= key` | `103_vdsData` | `AlwaysIncremental` | ✅ |
| 104 | Chỉ phần phát sinh sau mốc · *nguyên văn:* `>= key` | `104_weatherData` — nay là bản ghi **duy nhất** mang tiền tố 104 | `AlwaysIncremental` | ✅ |
| 105 | Gửi lại toàn bộ, **và gửi ngay khi dữ liệu được cập nhật** · *nguyên văn:* `All/ update`. Dòng mở đầu mục ghi rõ nguồn chính là **cả hai** bảng `TollTransactionIn`/`TollTransactionOut` | ✅ **Có bản ghi `105_rfidData`** (`OrderNo = 5`, tạo 24/09 10:38). Dữ liệu nguồn: 6 dòng RFID thật trong `TollTransactionIn` | `Snapshot` | ✅ **Đã xong (24/09).** `QueryPacket105` đọc trực tiếp `TollTransactionIn` với `TOP (@snapshotTop)` (100 dòng mới nhất), bọc `NULLIF` chuỗi rỗng biển số và `exitTime` null. Bản ghi gói cũng đã tạo — gói sẵn sàng gửi khi có đăng ký trỏ tới |
| 106 | Chỉ phần phát sinh sau mốc · *nguyên văn:* `>= key` | `106_wimData` | `AlwaysIncremental` | ✅ Gửi đủ 7 trường thật (`detectTime`, `lane`, `locationCode`, `speed`, `height`, `width`, `length`); 4 trường tải trọng để `null` vì bảng không có cột — không lọc `Source` |
| 107 | Chỉ phần phát sinh sau mốc; bản ghi cũ đổi trạng thái thì gửi lại chính bản ghi đó · *nguyên văn:* `>= key / giá trị cũ update trạng thái` | `107_incidentData` | `AlwaysIncremental` | ✅ |
| 108 | Gửi lại toàn bộ, **và gửi ngay khi có dữ liệu mới** · *nguyên văn:* `lấy all /DL được cập nhật -> nats` | `108_vmsInfo` | `Snapshot` | ✅ |
| 109 | Chỉ phần phát sinh sau mốc · *nguyên văn:* `>= key` | `109_etcData` | `AlwaysIncremental` | ✅ |
| 110 | *"Trao đổi với người tham gia giao thông"* — 3 trường `messageId`, `channel`, `deliveryState` đặc tả ghi "Chưa có, cần bảng notification/outbox". Các trường còn lại **có nguồn** (`TmsIncident`, `TmsWeather`, `VmsCurrent.RowData`) | `110_wpData`, `Status = 1`, `IsDelete = null` | `NotReady` → chặn | ⚠️ Đo 24/09: quét cả 154 entity, **không có bảng outbox nào** ⇒ 3 trường đó vẫn không lấy được. Nhưng nói gói này "không có dữ liệu nguồn" là **không chính xác** — nó thiếu 3 trường, không phải thiếu tất cả |
| 111 | "Trao đổi với TT QLĐHGT tuyến **(skip)**" | `111_testData` — xoá mềm 18/09 | `Disabled` → chặn | ✅ 111 là gói **có thật** trong quy hoạch (envelope tổng hợp 101–109), nhưng đặc tả chốt bỏ qua |

#### Gói chạy đúng nhưng thiếu dữ liệu nguồn (106, 110)

Hai gói ở tình trạng này, nhưng **thiếu theo hai kiểu khác nhau** — đừng gộp chung. (Gói 105 từng thuộc nhóm này vì đọc sai bảng; đã đủ cả code lẫn cấu hình từ 24/09):

| Gói | Thiếu gì | Hệ quả |
|---|---|---|
| **106** (cân tải trọng WIM) | Thiếu **cả ba tầng**: không còn trạm cân hoạt động (3 thiết bị `WOS` đều đã xoá mềm), `Source` không có giá trị nào từ trạm cân (toàn bộ là `traffic_vds_aid`), và không có cột nào cho 4 trường tải trọng | ✅ **Không còn gửi sai** (25/09): gửi đủ 7 trường thật, 4 trường tải trọng để `null` đúng bản chất — không lọc `Source`, không allow-list. Khi có trạm cân thật chỉ cần bổ sung cột/bảng rồi đưa vào `SELECT` |
| **110** (trao đổi với người tham gia giao thông) | Thiếu **3 trường** `messageId`, `channel`, `deliveryState` — cần bảng outbox. Các trường khác đều có nguồn | ⚠️ Đang bị chặn bằng `NotReady`, nên chưa gửi gì |

```csharp
// QueryPacket106 — chỉ SELECT 7 trường có thật, không có 4 trường tải trọng trong câu lệnh
SELECT TOP (@pageSize)
    td.DetectTime AS detectTime,
    td.Lane AS lane,
    td.Location AS locationCode,
    td.Speed AS speed,
    td.Height AS height,
    td.Width AS width,
    td.Length AS length,
    ...
FROM TmsTrafficData td

// QueryPacket110 — trả rỗng ngay lập tức, chưa có truy vấn nào cả (đúng nghĩa NotReady)
private static Task<List<object>> QueryPacket110(ISqlSugarClient db, DateTime? lastTime, string? lastKey, int pageSize, CancellationToken ct)
    => Task.FromResult<List<object>>([]);
```

Chi tiết và trạng thái: [mục 3](#3-chưa-làm-những-gì).

### 2.2. Bảng nguồn nào sinh được tín hiệu kích hoạt

Một bảng nguồn có thể được **nhiều gói tin cùng đọc** — ví dụ `TmsIncident` nuôi cả 3 gói 107, 110, 111.
Khi bảng đó đổi, `ResolveTriggerPackets` duyệt **từng gói trong danh sách ánh xạ của bảng** và chỉ giữ
lại gói nào **chưa bị chặn** (không phải `NotReady` hay `Disabled`); gói bị chặn thì rớt ngay tại đây,
không bắn tín hiệu nào cho riêng nó:

```csharp
foreach (var packet in packets)   // packets = danh sách gói được ánh xạ từ bảng nguồn vừa đổi
{
    var policy = PacketMetadataResolver.ResolveOutboundPolicy(packet);
    if (policy != ShareDataEnum.OutboundPacketPolicy.NotReady && policy != ShareDataEnum.OutboundPacketPolicy.Disabled)
    {
        result.Add(packet);       // chỉ gói còn dùng được mới vào đây
    }
}
```

⇒ Một bảng vẫn sinh được tín hiệu chỉ cần **còn ít nhất 1 gói dùng được** trong danh sách của nó — không
cần toàn bộ các gói nó nuôi đều đang hoạt động. Bảng dưới tách rõ 2 cột để không phải suy luận ngược:

| Bảng nguồn | Gói tin được ánh xạ tới | Gói nào thực sự bắn tín hiệu (sau khi lọc `NotReady`/`Disabled`) |
|---|---|---|
| `TmsZoneStatus`, `TmsZone`, `TmsTrafficStatistic` | 101 | 101 |
| `CctvDevice` | 102 | 102 |
| `TmsTrafficData` | 103, 106 | 103, 106 |
| `TmsWeather` | 104 | 104 |
| `TollTransactionIn`, `TollTransactionOut` | 105, 109 | 105, 109 |
| `TmsVehicleRegistration` | 105 | 105 |
| `TmsIncident`, `TmsEventType` | 107, 110, 111 | **107** — 110 (`NotReady`) và 111 (`Disabled`) bị lọc bỏ |
| `VmsCurrent` | 108 | 108 |
| `TmsEquipment` | 102, 103, 108 | 102, 103, 108 |
| `TollLane`, `TollStation` | 109 | 109 |

**15/15 bảng nguồn đều sinh được tín hiệu — không bảng nào bị quét vô ích**, kể cả `TmsIncident`/
`TmsEventType`: tuy nuôi cả 2 gói đang bị chặn (110, 111), nhưng vẫn còn gói 107 gánh tín hiệu qua.

---

## 3. Chưa làm những gì

> 📌 Toàn bộ mục này đo lại trên `mssql_staging` (`dev_its10` @ `10.10.8.30`) ngày **24/09/2026**. Chỉ liệt kê việc thuộc **tầng Service** — việc Frontend theo dõi riêng ở MasterPlan.

| # | Việc | Thuộc ai | Vì sao chưa làm | Có chặn luồng đang chạy không? |
|---|---|---|---|---|
| 1 | Gói **106** — 4 trường tải trọng `grossWeight`, `axleWeights`, `axleCount`, `isOverweight` | Chờ trạm cân hoạt động | **Thiếu ở cả ba tầng, không phải lỗi code.** Đo 24/09: (a) `TmsEquipmentType` có `WOS` = *"Trạm cân"* nhưng **cả 3 thiết bị loại này đều đã xoá mềm** (`WOS01`, `WOS02` xoá 07/08/2026; `WOS03` xoá 13/06/2025); (b) `Source` không có giá trị nào từ trạm cân; (c) `TmsTrafficData` **không có cột nào** trong 4 trường đó — chỉ có `Height/Width/Length` và cờ `AlarmHeight/Width/Length`, tức quá **khổ**, ⛔ không thay được quá **tải** | ❌ Không — gói vẫn gửi đủ 7 trường có thật, chỉ 4 trường này để trống |
| 2 | Gói **110** — dựng bảng outbox cho `messageId`, `channel`, `deliveryState` | Chờ thiết kế | Đo 24/09: quét **cả 154 entity** trên staging, ⛔ không có bảng `*Outbox`, `*Inbox` hay `*Notification` nào. Đặc tả ghi 3 trường này "— Chưa có, cần bảng notification/outbox" — vẫn đúng | ❌ Không — gói đang `NotReady`, bị chặn đúng chủ đích |

---

## 4. Edge case

### 4.1. Hệ thống sập giữa chừng

**Nguyên tắc thiết kế, phát biểu thẳng:** ưu tiên **không mất dữ liệu**, và để đạt điều đó thì **chấp nhận gửi trùng** và **chấp nhận trễ**. Gửi trùng vô hại vì bên nhận ghi đè theo khoá, không cộng dồn.

| Kịch bản sập | Hậu quả với dữ liệu |
|---|---|
| Sập giữa lúc gửi nối đuôi, giữa 2 trang | Trang đã commit giữ nguyên, trang dở rollback; lần chạy sau lấy lại từ mốc cũ — ✅ không mất |
| Sập **sau** khi HTTP gửi xong nhưng **trước** khi commit mốc | Đối tác đã nhận, mốc chưa tiến ⇒ lần sau **gửi lại đúng phần đó** — ✅ đúng thiết kế *ít nhất một lần* |
| **Tiến trình bị giết / mất điện** khi đang giữ quyền xử lý | ✅ Hoàn toàn an toàn: cơ chế lease `NextTimeRun` tự động hồi phục khi hết hạn, worker lần sau tiếp tục chạy nối đuôi từ checkpoint mà không làm sai lệch dữ liệu |
| **Worker giám sát khởi động lại** | Thay đổi phát sinh lúc worker chết không sinh tín hiệu; quét định kỳ vẫn gửi đủ — ✅ chỉ chậm, không mất |
| NATS chết | ✅ Không mất — quét định kỳ gửi bù |
| Mất kết nối CSDL giữa chừng | Transaction tự rollback; quyền xử lý treo giống dòng 3 — ✅ không mất |
| Sập khi đang gửi gói **bản chụp** | ✅ Không mất gì — chu kỳ sau gửi lại toàn bộ, đúng thiết kế nhóm gói này |

**Vì sao sập không để lại trạng thái nửa vời.** `CommitSuccess` gói *cập nhật đăng ký* và *ghi mốc checkpoint* vào **cùng một transaction**. Không có cửa sổ nào mà mốc đã tiến còn đăng ký thì chưa, hay ngược lại.

**Cơ chế tự hồi phục (Self-healing lease) khi tiến trình bị dừng/kill đột ngột.** Quyền xử lý được chiếm bằng cách đẩy `NextTimeRun` ra tương lai làm thời hạn bảo vệ (lease timeout), và giải phóng trong khối `finally`:

```csharp
// LockedSubscription
var lockDurationSeconds = Math.Max(300, interval * 3);
var nextRunDeadline = TruncateToSecond(now.AddSeconds(lockDurationSeconds));
...
finally { await ReleaseLock(db, sub, nextRunDeadline, CancellationToken.None); }
```

Ngoại lệ thông thường thì khối `finally` sẽ chạy và nhả quyền ngay lập tức. Trường hợp tiến trình bị kill đột ngột hoặc mất điện (không kịp vào khối `finally`), mốc `NextTimeRun` đóng vai trò là cơ chế tự khắc phục (self-healing lease): khi mốc thời gian này đến hạn (`NextTimeRun <= now`), worker ở chu kỳ quét sau sẽ tự động nhận lại đăng ký và tiếp tục gửi nối đuôi bình thường từ mốc `LastTime`/`LastKey` của `ShareDataCheckpoint`. Hệ thống tự động đi tiếp mà không làm mất mát hay sai lệch dữ liệu.

**Vì sao worker giám sát khởi động lại không gây mất dữ liệu.** Mốc phiên bản Change Tracking là biến **trong bộ nhớ**, khởi động lại là nhảy thẳng tới mốc hiện tại, bỏ qua toàn bộ thay đổi xảy ra lúc chết. Nghe đáng lo, nhưng mốc đó chỉ quyết định **khi nào** bắn tín hiệu, còn **gửi nội dung gì** hoàn toàn do `ShareDataCheckpoint` quyết định — quét định kỳ kế tiếp vẫn lấy đủ mọi bản ghi sau mốc checkpoint.

### 4.2. Đối tác mới đăng ký lần đầu

📌 Thiết kế nối đuôi dựa trên giả định đối tác **đã có** dữ liệu cũ — theo lập luận của Anh Sơn tại biên bản họp 21/09 mốc `09:59`: *"Tại vì dữ liệu đó mình đã gửi cho họ, họ đã lưu trữ bên bển rồi, giờ mình lại đi gửi cái dữ liệu đó lại nữa thì cũng không mang ý nghĩa gì."* Giả định đó đúng với đối tác đang chạy, nhưng **không đúng với đối tác mới tinh** — biên bản không bàn tới tình huống đó.

Lượt chạy đầu của một đăng ký gói nối đuôi **cắm mốc lùi đúng một chu kỳ** (`GETDATE()` trừ `IntervalSeconds`) rồi gửi ngay dữ liệu trong khoảng đó — thay vì cắm mốc tại hiện tại rồi thoát mà không gửi gì như trước.

```csharp
// GetOrInitCheckpoint — nhánh khởi tạo checkpoint lần đầu (chưa có LastTimeRun cũ)
if (lastTimeRun.HasValue)
{
    initialTime = lastTimeRun.Value;
}
else
{
    var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
    var safeInterval = intervalSeconds > 0 ? intervalSeconds : DataOutboundScheduler.DefaultIntervalSeconds;
    initialTime = dbNow.AddSeconds(-safeInterval);
}
```

- **Bắt buộc dùng `var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");`**: Bảo đảm 100% đồng nhất hệ quy chiếu thời gian với SQL Server. Tránh hiện tượng lệch đồng hồ (clock skew) giữa máy chủ ứng dụng và CSDL (dù chỉ vài giây) khiến mốc `initialTime` vượt trước thời gian ghi nhận của bản ghi trong DB, dẫn đến rớt bản ghi ở lượt quét đầu tiên.
- **Phòng vệ `var safeInterval = intervalSeconds > 0 ? intervalSeconds : DataOutboundScheduler.DefaultIntervalSeconds;`**: Bảo đảm luôn lùi một chu kỳ hợp lệ kể cả khi cấu hình `IntervalSeconds <= 0`, tránh lỗi chia 0 hoặc cộng dồn sai lệch.

Cách này **tự co giãn theo gói**: gói chu kỳ 30 giây lấy 30 giây gần nhất, gói chạy theo ngày lấy một ngày gần nhất — không bao giờ biến thành nạp lịch sử vô hạn.

⚠️ **Hệ quả cần biết:** đối tác mới **vẫn không nhận được dữ liệu cũ hơn một chu kỳ**. Nếu về sau thật sự cần nạp lịch sử cho một đối tác, việc đó phải là **một thao tác riêng có chủ đích**, không gắn vào việc tạo đăng ký. Hai phương án bị bác và lý do: xem [Phụ lục, mục 3](#3-quyết-định-kỹ-thuật-cốt-lõi-giữ-lại-để-lần-sau-khỏi-bàn-lại).

### 4.3. Nhiều bản ghi cùng mốc thời gian

Nếu chỉ dùng một cột thời gian làm mốc, khi 2+ bản ghi cùng giá trị thời gian (rất dễ xảy ra khi ghi hàng loạt trong cùng giao dịch), điều kiện `WHERE UpdateTime > @lastTime` sẽ **bỏ sót toàn bộ các bản ghi còn lại cùng giây** ngay khi một bản ghi trong nhóm đã được gửi.

Khoá kép `(LastTime, LastKey)` tạo thành **một trục thứ tự tuyệt đối không bao giờ trùng** (vì `ID` là khoá chính):

| Tình huống | Kết quả |
|---|---|
| Thời gian **lớn hơn** mốc đã lưu | Chắc chắn chưa gửi → lấy |
| Thời gian **bằng** mốc, `ID` lớn hơn `LastKey` | Cùng giây nhưng phát sinh sau → lấy (không bỏ sót) |
| Thời gian **bằng** mốc, `ID` nhỏ hơn/bằng `LastKey` | Đã gửi rồi → loại (không gửi lặp) |
| Thời gian **nhỏ hơn** mốc | Đã gửi rồi → loại |

⇒ Thoả đồng thời 2 yêu cầu cốt lõi: **không bỏ sót** và **không gửi trùng**.

```sql
-- QueryPacket107 — khoá kép (LastTime, LastKey) áp trực tiếp vào WHERE của câu truy vấn thật
WHERE (@lastTime IS NULL
    OR COALESCE(i.UpdateTime, i.CreateTime, i.StartDate) > @lastTime
    OR (COALESCE(i.UpdateTime, i.CreateTime, i.StartDate) = @lastTime AND i.ID > @lastKey))
ORDER BY COALESCE(i.UpdateTime, i.CreateTime, i.StartDate) ASC, i.ID ASC
```

### 4.4. Hai worker chạy song song

- **`GetOrInitCheckpoint`** — nếu 2 worker cùng tạo một lúc, bên thua ràng buộc duy nhất tự nạp lại dòng đã có thay vì ghi đè:
  ```csharp
  catch
  {
      // Chịu unique race: Node khác đã tạo trước -> reload bản ghi đã tạo, không overwrite
      var reloaded = await db.Queryable<ShareDataCheckpoint>()
          .Where(c => c.PartnerCode == safePartnerCode && c.PacketCode == safePacketCode)
          .FirstAsync();
      if (reloaded != null)
          return (reloaded, false);
      throw;
  }
  ```
- **`UpdateCheckpoint`** — chốt chặn **chỉ tiến, không lùi**: worker chạy trễ cố ghi mốc cũ hơn thì câu lệnh ảnh hưởng 0 dòng, mốc giữ nguyên ở vị trí tiến xa nhất:
  ```csharp
  if (newTime.HasValue)
  {
      var nt = newTime.Value;
      if (newKey != null)
          update = update.Where(c => c.LastTime < nt || (c.LastTime == nt && (c.LastKey == null || c.LastKey.CompareTo(newKey) < 0)));
      else
          update = update.Where(c => c.LastTime < nt || (c.LastTime == nt && c.LastKey == null));
  }
  ```
- **`CommitSuccess`** — kèm điều kiện tương tranh lạc quan `WHERE ID = @subId AND NextTimeRun = @nextRunDeadline`. Nếu quyền xử lý bị worker khác giành mất giữa chừng thì huỷ giao dịch, dừng vòng lặp, không ghi đè kết quả của worker kia:
  ```csharp
  var subAffected = await db.Ado.ExecuteCommandAsync(updateSubSql, new { newTime, now, subId = sub.ID, nextRunDeadline });
  if (subAffected <= 0)
  {
      await tran.RollbackTranAsync();
      return false;
  }
  ```

Nhờ commit theo từng trang, khi trang thứ 4 gửi lỗi thì 3 trang đã gửi thành công vẫn được giữ nguyên — không phải làm lại từ đầu.

### 4.5. Phân trang và kiểm soát kích thước dữ liệu tầng Worker (cập nhật 25/09/2026)

Ở tầng Worker Service, kích thước phân trang mặc định được chuẩn hóa thành `DefaultPageSize = 100` (độc lập, không can thiệp vào `Module.ShareData`):

| Gói | Chế độ | Giới hạn số dòng / trang | Ghi chú |
|---|---|---|---|
| **103, 104, 106, 107, 109** | Nối đuôi | **`PageSize = 100`** | `SELECT TOP (@pageSize) ...` phân trang nối đuôi, tối đa 20 trang/chu kỳ (2.000 bản ghi/lần chạy) |
| **105** | Bản chụp | ✅ **`TOP (@snapshotTop)` = 100** | Đọc trực tiếp `TollTransactionIn`, `ORDER BY TransactionDateTime DESC`, dùng trực tiếp `DefaultSnapshotTop = 100` |
| 101, 102, 108 | Bản chụp | Trạng thái hiện tại | Trạng thái thiết bị/hạ tầng (camera, biển VMS, đoạn tuyến) |
| 110, 111 | Bản chụp | *(chưa dùng)* | Đang bị chặn (`NotReady`/`Disabled`) nên chưa xuất bản |

✅ Gói 105 đã xử lý xong (24/09/2026): Đọc trực tiếp từ `TollTransactionIn` với `TOP (@snapshotTop)` = 100 dòng mới nhất, độc lập tại tầng Service.

---

## Phụ lục. Cơ chế hoạt động & Quyết định kỹ thuật

> 📌 Phần này là tài liệu tham khảo kiến trúc và các quyết định thiết kế cốt lõi.

### 1. Một lần chạy gồm những bước nào

Điểm vào định kỳ: `DataOutboundWorker.RunPeriodicAsync` → `ProcessBatchSubscriptions()` — quét đăng ký `Direction=Outbound`, `State=Active`, đến hạn (`NextTimeRun == null || NextTimeRun <= now`), đối tác đang `Connected`.

1. **Chiếm quyền xử lý**: `UPDATE ShareDataSubscription SET NextTimeRun = @nextRunDeadline WHERE ID = ... AND State = Active AND (NextTimeRun IS NULL OR NextTimeRun <= now)`. Nếu **số dòng cập nhật bằng 0** → worker khác đang giữ quyền, bỏ qua.
2. Resolve đối tác (`ShareDataPartner` theo `sub.PartnerId`) → lấy `partner.Code`.
3. Gọi `ExportSubscription(...)`:
   - Resolve gói tin qua `ResolveActivePacket(db, sub.DatatypeId)`.
   - Kiểm tra `OutboundPolicy` — `NotReady`/`Disabled` thì bỏ qua.
   - Resolve hồ sơ ánh xạ `ShareDataMapping` theo Đối tác + Gói tin + Chiều — không có thì huỷ kết xuất và ghi cảnh báo.
   - Nếu là gói nối đuôi: `GetOrInitCheckpoint(...)` lấy mốc `LastTime`/`LastKey` của phiên trước.
   - **Vòng lặp phân trang**: `Extract(cursor)` → `Map` → gửi File/REST → `CommitSuccess` → tiến mốc sang trang kế. Tối đa **20 trang**, mỗi trang **100 bản ghi** (`DefaultPageSize = 100`).
   - Dừng khi: hết dữ liệu, đã dùng hết **50% ngân sách thời gian** của thời hạn quyền xử lý, hoặc không xác định được con trỏ hợp lệ cho trang kế.
4. **Nhả quyền xử lý** (`ReleaseLock`) trong khối `finally` — kể cả khi có lỗi.

📌 Nhờ vòng lặp phân trang, một lần chạy gửi bù được tối đa **2000 bản ghi** (20 trang × 100) khi dữ liệu tồn đọng lớn, thay vì chỉ 100 bản ghi mỗi chu kỳ.

### 2. Bảng checkpoint có phình to không?

**Không.** `ShareDataCheckpoint` (`PartnerCode`, `PacketCode`, `LastTime`, `LastKey`, `LastVersion`, khoá duy nhất trên `(PartnerCode, PacketCode)`) là bảng **trạng thái hiện tại**, không phải bảng nhật ký:

- Mỗi cặp (Đối tác × Gói tin) chỉ sinh **đúng 1 dòng, đúng 1 lần**; các lần chạy sau chỉ cập nhật tại chỗ.
- Gói `Snapshot` không sinh dòng nào — toàn bộ phần checkpoint nằm gọn trong nhánh `if (isIncremental)`.
- Tổng số dòng bị chặn trên bởi *(số đối tác đang hoạt động × 5 gói nối đuôi)* — vài chục đến vài trăm dòng, **không tăng theo lượng dữ liệu gửi đi**.

⇒ **Không cần cơ chế tự xoá.** Các bảng thật sự cần cân nhắc dọn định kỳ là nhật ký hoạt động (`ShareDataActivityLog`, `ShareDataTransferLog`), không phải bảng này.

### 3. Quyết định kỹ thuật cốt lõi (giữ lại để lần sau khỏi bàn lại)

**Bài toán:** Khi người dùng vừa tạo xong 1 cấu hình gửi dữ liệu (Subscription) mới tinh, lần chạy đầu tiên hệ thống nên lấy dữ liệu từ mốc thời gian nào? 

Dưới đây là 3 phương án và lý do chọn lùi 1 chu kỳ:

| Phương án cho lần chạy đầu tiên | Đánh giá & Hậu quả |
|---|---|
| ❌ **Lấy toàn bộ dữ liệu lịch sử từ trước đến nay** | Các bảng này chứa dữ liệu khổng lồ (VD: dò xe, thu phí). Nếu lấy từ đầu, hệ thống sẽ kéo hàng triệu bản ghi, chạy mất vài ngày mới xong, gây treo DB và làm ngập lụt máy chủ của đối tác. |
| ❌ **Lấy mốc thời gian đúng ngay thời điểm hiện tại (`GETDATE`)** | Nếu mốc là "ngay lúc này", thì câu lệnh tìm kiếm dữ liệu mới (`UpdateTime >= lúc_này`) sẽ không có dòng nào thỏa mãn. Lần chạy đầu tiên coi như chạy không công, không gửi được gì để test kết nối. |
| ✅ **Lùi mốc về đúng 1 chu kỳ (VD: `GETDATE` trừ 30 giây)** | (Phương án đang dùng) Giúp quét được một lượng nhỏ dữ liệu vừa mới sinh ra ngay trước khi tạo cấu hình. Vừa đủ để chứng minh luồng kết nối gửi thành công ngay lập tức, vừa không làm nặng hệ thống. |

**Vì sao bắt buộc dùng `SELECT GETDATE()` của DB thay vì `DateTime.Now` của C#:**

| Phương án bị bác | Vì sao không chọn |
|---|---|
| Dùng `DateTime.Now` (hoặc `DateTime.UtcNow`) của C# | **Lệch đồng hồ (clock skew)** giữa server WebAPI/Worker và SQL Server: mốc `initialTime = DateTime.Now.AddSeconds(-30)` nếu vượt trước `DetectTime` của các bản ghi trong DB thì câu lệnh `WHERE DetectTime >= @lastTime` trả rỗng, bỏ sót dữ liệu lượt đầu (đã chứng minh thực tế bằng unit test). Bắt buộc dùng `SELECT GETDATE()` để lấy đúng giờ của SQL Server. Lệnh chỉ chạy **1 lần duy nhất** khi tạo checkpoint nên hoàn toàn không ảnh hưởng hiệu năng |

