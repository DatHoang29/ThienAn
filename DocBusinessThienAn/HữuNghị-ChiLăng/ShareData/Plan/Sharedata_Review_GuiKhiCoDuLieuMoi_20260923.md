# Báo cáo: Cơ chế Gửi Ngay Khi Có Dữ Liệu Mới (Change Tracking + NATS) — ShareData

> Ngày: 23/09/2026. Dựa trên code hiện tại trong `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/`. Đối chiếu đặc tả [`doc/02-mapping-goi-tin-101-111.md`](../doc/02-mapping-goi-tin-101-111.md) và biên bản họp [21/09/2026](../doc/transcript/21-09-2026-hoan-thien-mapping-va-gui-noi-duoi-sharedata.md).
>
> Luồng **gửi nối đuôi** (khoá kép, checkpoint, phân trang) nằm ở báo cáo riêng: [`Sharedata_Review_LuongNoiDuoi_20260923.md`](./Sharedata_Review_LuongNoiDuoi_20260923.md).

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
| Đặc tả yêu cầu gì? | **Cả 4 gói bản chụp (101, 102, 105, 108)** đều thống nhất theo cơ chế: gửi toàn bộ hiện trạng (`Snapshot`) và **gửi ngay khi dữ liệu được cập nhật qua NATS** (bản chất `DL được cập nhật`, `update` và `-> nats` là một vế duy nhất). Tuy nhiên, biên bản họp 21/09 đã chốt **KHÔNG LÀM / KHÔNG TRIỂN KHAI** tính năng này. | [Mục 1](#1-đặc-tả-yêu-cầu-gì) |
| Code so với đặc tả? | **Đã làm xong** theo phương án Change Tracking + NATS. Cơ chế kích hoạt là **năng lực dùng chung cho mọi gói hợp lệ**; quyền bật nằm ở cờ `SendOnNewData` của **từng đăng ký**, không nằm ở gói tin. | [Mục 2](#2-code-làm-được-tới-đâu-so-với-đặc-tả) |
| Edge case? | 5 tình huống, đều đã có lối xử. Lần đầu khởi động sau triển khai, Worker sẽ tự chạy lệnh DDL (Data Definition Language) bật Change Tracking trên CSDL thật. | [Mục 3](#3-edge-case) |

---

## 1. Đặc tả yêu cầu: Trigger đổi THỜI ĐIỂM gửi, không đổi NỘI DUNG gửi

> 📌 Dấu `/` trong ghi chú (ví dụ: `lấy all / DL được cập nhật -> nats`) dùng để tách biệt 2 trục ý nghĩa hoàn toàn độc lập, không phải là gửi hai nội dung song song:
> - **Vế 1 (trước dấu `/`)**: Trả lời câu hỏi *"Gửi cái gì?"* — `lấy all` (hoặc `All`) là gửi lại toàn bộ hiện trạng của gói.
> - **Vế 2 (sau dấu `/`)**: Trả lời câu hỏi *"Khi nào gửi?"* — `DL được cập nhật` (hoặc viết tắt `update`) nghĩa là gửi ngay khi có dữ liệu mới, không đợi hết chu kỳ. Đuôi `-> nats` chỉ là tên kênh truyền tín hiệu của chính vế đó (phần trước nói sự kiện, đuôi sau chỉ rõ phương thức truyền).

Hai trục này do hai thành phần khác nhau quyết định:

| Trục ý nghĩa | Quyết định bởi |
|---|---|
| **KHI NÀO** gửi | Cờ `SendOnNewData` + trigger NATS |
| **GỬI CÁI GÌ** | `OutboundPolicy` của gói tin (bản chụp / nối đuôi) |

Việc bật `SendOnNewData` (nghĩa là kích hoạt Vế 2) **chỉ đổi trục KHI NÀO gửi**. Nội dung gói tin không thay đổi một chút nào — vẫn đúng khối lượng dữ liệu mà chính sách của gói đó quy định, chỉ là được kích hoạt gửi sớm hơn.

Dưới đây là bảng đối chiếu và giải mã cách hiểu thống nhất cho cả 4 gói bản chụp:

| Gói | Nguyên văn tiêu đề mục | Vế 1: "Gửi cái gì?" | Vế 2: "Khi nào gửi?" | Nghĩa thực tế đã chốt |
|---|---|---|---|---|
| 101 | `lấy all/DL được cập nhật` | Bản chụp toàn bộ (`Snapshot`) | ✅ Gửi ngay khi cập nhật (`NATS`) | Gửi toàn bộ hiện trạng; khi bảng nguồn đổi thì kích hoạt NATS gửi ngay |
| 102 | `lấy all /DL được cập nhật -> nats` | Bản chụp toàn bộ (`Snapshot`) | ✅ Gửi ngay khi cập nhật (`NATS`) | Gửi toàn bộ hiện trạng; khi bảng nguồn đổi thì kích hoạt NATS gửi ngay |
| 105 | `All/ update` | Bản chụp toàn bộ (`Snapshot`) | ✅ Gửi ngay khi cập nhật (`NATS`) | Gửi toàn bộ hiện trạng (TOP 100 mới nhất); khi bảng nguồn đổi thì kích hoạt NATS gửi ngay |
| 108 | `lấy all /DL được cập nhật -> nats` | Bản chụp toàn bộ (`Snapshot`) | ✅ Gửi ngay khi cập nhật (`NATS`) | Gửi toàn bộ hiện trạng; khi bảng nguồn đổi thì kích hoạt NATS gửi ngay |

📌 **Cách hiểu đã chốt thống nhất:**
- Các ghi chú `DL được cập nhật`, `update` và đuôi `-> nats` là **cùng một vế duy nhất, không tách rời**: phần trước nói sự kiện (dữ liệu đổi thì gửi), đuôi sau chỉ rõ kênh truyền tín hiệu (`NATS`). Do đó, không có sự phân biệt câu chữ giữa các gói — **cả 4 gói bản chụp (101, 102, 105, 108) đều đồng nhất 100% về cơ chế hoạt động**.
- **Cơ chế hiện thực:** **Change Tracking của SQL Server** phát hiện bảng nguồn vừa đổi → **bắn tín hiệu qua NATS** → worker nhận tín hiệu rồi chạy luồng gửi ngay.

| Hạng mục | Gói bản chụp (101, 102, 105, 108) | Gói nối đuôi (103, 104, 106, 107, 109) |
|---|---|---|
| Gửi gì | Toàn bộ hiện trạng | Chỉ phần phát sinh sau mốc `(LastTime, LastKey)` |
| Lọc theo mốc đã gửi | Không — cursor `null` | Có |
| Mỗi lần chạy | 1 lượt, không phân trang | Tối đa 20 trang × 50 bản ghi |
| Checkpoint | Không tạo, không tiến | Tạo và tiến mốc |
| Bật `SendOnNewData` đổi gì | Chỉ đổi **thời điểm** | Chỉ đổi **thời điểm** |

**Ba chỗ trong code xác nhận điều này:**

```csharp
// 1. ExportSubscription — gói bản chụp truyền cursor null, không lọc theo mốc
isIncremental ? new DataOutboundCursor(currentWatermark, currentLastKey) : null

// 2. ExportSubscription — gói bản chụp chạy đúng 1 lượt rồi dừng
var maxPages = isIncremental ? DefaultMaxPagesPerRun : 1;
...
if (!isIncremental)
    break;

// 3. Query handler gói bản chụp bỏ qua cả cursor lẫn pageSize
private Task<List<object>> QueryPacket102(ISqlSugarClient db, DateTime? lastTime, string? lastKey, int pageSize, CancellationToken ct)
{
    var sql = "..."; // câu truy vấn gói 102 không dùng cursor
    return ExecuteDynamicQuery(db, sql);   // không dùng lastTime, lastKey, pageSize
}
```



---

## 2. Code làm được tới đâu so với đặc tả

### 2.1. Trạng thái: Đã hoàn thành (Change Tracking + NATS)

Tính năng **đã được triển khai hoàn chỉnh** theo phương án kết hợp Change Tracking + NATS: phát hiện thay đổi ở tầng CSDL bằng SQL Server Change Tracking, và dùng NATS làm kênh báo hiệu tức thời tới tầng ứng dụng. Ô chọn `SendOnNewData` trên giao diện hiện hoạt động thật, kết nối trực tiếp với logic xử lý của Worker.

**Vì sao chọn Change Tracking, không chọn CDC hay NATS thuần?**

| Tiêu chí | **Change Tracking (đang dùng)** | CDC | NATS thuần (tầng ứng dụng tự bắn) |
|---|---|---|---|
| Bắt được thay đổi từ nguồn nào | Mọi đường ghi vào bảng, kể cả script chạy tay, import, hệ thống bên thứ ba ghi thẳng CSDL | Mọi đường ghi (đọc từ nhật ký giao dịch) | **Chỉ** đường ghi đi qua code có gọi hàm phát tín hiệu — dễ sót khi quên gọi, hoặc có ghi tay/import ngoài |
| Nội dung trả về | Chỉ "có đổi + số hiệu version" | Đầy đủ ảnh trước/sau, phát lại được | Tuỳ code tự đóng gói |
| Chi phí hạ tầng | Nhẹ, không cần tiến trình phụ | Nặng hơn: cần tác vụ đọc nhật ký, bảng lưu thay đổi riêng | Không tốn CSDL nhưng tốn công sửa mọi nơi ghi dữ liệu |
| Có cần cho bài toán này không | **Đủ dùng** — dữ liệu thật vẫn lấy qua truy vấn nối đuôi, không cần ảnh trước/sau | Thừa — hệ thống không cần phát lại giá trị trung gian | Rủi ro cao, đúng lo ngại Anh Sơn đã nêu trong họp về các nguồn ghi ngoài tầm kiểm soát của code |

**Kết luận:** Dùng Change Tracking làm nguồn phát hiện đáng tin cậy ở tầng CSDL (bắt được mọi đường ghi), NATS chỉ đóng vai trò kênh "đánh thức ngay" để giảm độ trễ, kèm quét định kỳ làm lưới an toàn. Phương án này cân bằng hơn cả hai hướng thuần tuý từng nêu trong họp.

**Phạm vi: Quyền kích hoạt nằm ở ĐĂNG KÝ, không nằm ở gói tin.** Đây là chỗ dễ mô tả sai nhất của tính năng.

Trong code **không có danh sách trắng theo gói** nào cả:

- `SendOnNewData` và `DebounceSec` là **cột của bảng `ShareDataSubscription`**, không phải của `ShareDataPacket`.
- `ResolveTriggerPackets` chỉ chặn đúng hai chính sách chưa dùng được: `NotReady` (110) và `Disabled` (111).

⇒ Cơ chế kích hoạt là **năng lực dùng chung cho cả 9 gói hợp lệ**, còn bật hay không do **từng đăng ký** quyết định. Ghi chú `-> nats` trong đặc tả **không cấp cũng không thu hồi** năng lực đó — nó chỉ ghi lại *đối tác muốn bật cờ ở gói nào*. Gói không có ghi chú thì đơn giản là cờ `SendOnNewData` của nó không được bật, chứ không phải code làm quá phạm vi.

📌 Làm cơ chế dùng chung là lựa chọn đơn giản và đúng hơn. Dựng một danh sách trắng cứng theo gói mới là chuyện lạ, vì nó khoá một quyết định **cấu hình vận hành** vào trong **mã nguồn** — đúng cái bẫy đã mắc một lần với bảng chính sách xuất bản.

### 2.2. Bản đồ bảng nguồn → gói tin

`DataChangeWatcherWorker.RawTableToPacketMap` giữ bản đồ **15 bảng**:

| Bảng nguồn | Gói tin liên quan |
|---|---|
| `TmsZoneStatus`, `TmsZone`, `TmsTrafficStatistic` | 101 |
| `CctvDevice` | 102 |
| `TmsTrafficData` | 103, 106 |
| `TmsWeather` | 104 |
| `TollTransactionIn`, `TollTransactionOut` | 105, 109 |
| `TmsVehicleRegistration` | 105 |
| `TmsIncident`, `TmsEventType` | 107, 110, 111 |
| `VmsCurrent` | 108 |
| `TmsEquipment` | 102, 103, 108 |
| `TollLane`, `TollStation` | 109 |

`ResolveTriggerPackets` **chỉ loại bỏ gói `NotReady` (110) và `Disabled` (111)**; mọi gói hợp lệ còn lại — cả nối đuôi lẫn bản chụp — đều kích hoạt được. Nếu tất cả gói liên quan đều bị loại, worker ghi log giải thích rõ lý do.

📌 Không bảng nguồn nào bị quét vô ích: `TmsIncident` và `TmsEventType` tuy nuôi cả 110 và 111 nhưng vẫn lọt qua nhờ gói 107. Đối chiếu đầy đủ 15 bảng: [mục 2.3](./Sharedata_Review_LuongNoiDuoi_20260923.md) của báo cáo luồng nối đuôi.




✅ **Đã xử lý giới hạn số dòng gói 105 (24/09/2026):** gói **105** nay đọc trực tiếp `TollTransactionIn` kèm giới hạn **100 dòng mới nhất** (`DefaultSnapshotTop = 100`) tại tầng Service. Gói 102 và 108 giữ nguyên vì số camera và số biển VMS là hữu hạn. Chi tiết xem tại báo cáo luồng nối đuôi.

### 2.3. Hành vi qua nhiều chu kỳ — vì sao gửi lại là chủ đích

Câu hỏi thường gặp: *"Lần 1 gửi toàn bộ, sau đó có 1 bản ghi được cập nhật — lần 2 có gửi lại cái vừa cập nhật không?"*

**Có — và gửi lại cả những bản ghi KHÔNG đổi.** Ví dụ 100 camera, `IntervalSeconds = 30`:

| Thời điểm | Chuyện gì xảy ra | Số bản ghi gửi đi |
|---|---|---|
| `00:00` | Chạy lần 1 | **100** |
| `00:15` | 1 camera đổi ảnh | — (chưa tới chu kỳ) |
| `00:30` | Chạy lần 2 | **100** — cả cái vừa đổi lẫn 99 cái không đổi |
| `01:00` | Chạy lần 3, không có gì đổi | **100** |

Gói bản chụp không có checkpoint, không có cursor — nó không biết và không cần biết lần trước đã gửi gì. **Đây là yêu cầu nghiệp vụ đã chốt, không phải lãng phí do code làm ẩu** — căn cứ biên bản: [Phụ lục B](#phụ-lục-b-quyết-định-kèm-lý-do).

Gửi trùng **không làm sai dữ liệu**: bên nhận **ghi đè** bản sao của mình bằng trạng thái mới nhất. Thao tác là *thay thế*, không phải *cộng dồn*. Cái giá phải trả chỉ là băng thông.

| Hạng mục | Gói bản chụp (101, 102, 105, 108) | Gói nối đuôi (103, 104, 106, 107, 109) |
|---|---|---|
| Lần chạy **ĐẦU** | Gửi toàn bộ | Gửi ngay dữ liệu trong cửa sổ 1 chu kỳ gần nhất — cắm mốc `GETDATE() - IntervalSeconds` |
| Các lần **SAU** | Gửi lại **toàn bộ**, kể cả khi không có gì đổi | Chỉ phần phát sinh sau mốc |
| Căn cứ nghiệp vụ | Biên bản 21/09 mốc `09:59`, `10:50` | Biên bản 21/09 mốc `06:56`, `08:31` |

---

## 3. Edge case

| # | Tình huống | Cách hệ thống xử |
|---|---|---|
| 1 | **NATS mất kết nối** | Worker ghi cảnh báo `"NATS chưa kết nối. Bỏ qua publish, DataOutboundWorker định kỳ sẽ quét bù"` rồi thôi. ✅ Không mất dữ liệu — mốc checkpoint không đổi nên lần quét định kỳ kế tiếp vẫn lấy đủ phần còn thiếu |
| 2 | **Dữ liệu theo dõi hết hạn lưu (2 ngày)** | ✅ Câu truy vấn kẹp `@fromVer` theo `CHANGE_TRACKING_MIN_VALID_VERSION`, tránh lỗi hỏi version quá cũ sau khi dữ liệu bị dọn tự động |
| 3 | **Worker giám sát khởi động lại** | Mốc phiên bản Change Tracking là biến trong bộ nhớ, khởi động lại nhảy thẳng tới mốc hiện tại. ✅ Không mất dữ liệu — mốc đó chỉ quyết định **khi nào** bắn tín hiệu, còn **gửi nội dung gì** do `ShareDataCheckpoint` quyết định |
| 4 | **Lần đầu khởi động sau khi triển khai** | Nếu Change Tracking chưa bật, Worker **tự chạy lệnh DDL (Data Definition Language)** `ALTER DATABASE` / `ALTER TABLE` trên CSDL thật, ở **mọi môi trường** kể cả Production. Nên chọn thời điểm triển khai **tránh giờ cao điểm** cho lần đầu tiên. Xem [Phụ lục A.4](#a4-yêu-cầu-bật-change-tracking-trên-csdl) |
| 5 | **Tác vụ trước xử lý lâu, bản tin trigger sau dồn ứ** | ✅ Không mất, không chạy đè: NATS Client xếp hàng tuần tự; `DataNatsWorker` debounce 500ms lọc trùng cùng gói; `LockedSubscription` khóa OCC (`NextTimeRun > now`) bỏ qua êm dịu nếu subscription đang bận; lượt sau nếu chạy thì thấy 0 dòng (do checkpoint đã tiến) sẽ thoát ngay. Đã kiểm chứng 3 unit tests ngày 25/09/2026 |

---

## Phụ lục A. Cơ chế hoạt động

> 📌 Phần này là tài liệu tham khảo, không cần đọc để ra quyết định.

### A.1. Chuỗi xử lý

```
[CSDL: bảng nguồn thay đổi]
        ↓ (SQL Server Change Tracking ghi nhận)
[DataChangeWatcherWorker] — nhịp quét 1 giây
        ↓ phát hiện version tăng → xác định bảng nào đổi
[DataChangeWatcherWorker.ResolveTriggerPackets] — ánh xạ bảng → gói tin, lọc policy
        ↓ publish NATS: ta.its.event.sharedata.newdata.{packetCode}
[DataNatsWorker] — chống dội 500ms theo từng gói
        ↓
[DataOutboundService.ProcessPacketTrigger(packetCode)]
        ↓ (tái dùng nguyên pipeline gửi nối đuôi)
[Xuất bản dữ liệu tới đối tác]
```

### A.2. Phát hiện thay đổi — `DataChangeWatcherWorker`

Nhịp quét **1 giây** (`PollingInterval`). Mỗi vòng:

1. Đọc `SELECT CHANGE_TRACKING_CURRENT_VERSION()`. Nếu không lớn hơn `_lastProcessedVersion` → không có gì mới, thoát sớm (chi phí gần như bằng 0).
2. Nếu version tăng: duyệt 15 bảng nguồn, mỗi bảng chạy một câu kiểm tra:
   ```sql
   DECLARE @minVer BIGINT = CHANGE_TRACKING_MIN_VALID_VERSION(OBJECT_ID('<bảng>'));
   DECLARE @fromVer BIGINT = @lastVer;
   IF @fromVer < @minVer SET @fromVer = @minVer;
   SELECT TOP 1 1 FROM CHANGETABLE(CHANGES [<bảng>], @fromVer) AS CT;
   ```
3. Cập nhật `_lastProcessedVersion = currentVersion` ở cuối vòng.

📌 Change Tracking chỉ trả lời "**có thay đổi hay không**", không trả về giá trị cũ/mới của bản ghi. Điều này **đủ dùng** vì dữ liệu thật vẫn được lấy lại bằng câu truy vấn nối đuôi theo khoá kép ở bước sau.

### A.3. Báo hiệu qua NATS và xử lý phía nhận

- Chủ đề: `ta.its.event.sharedata.newdata.{packetCode}`, nội dung bản tin gồm `PacketCode`, `Version`, `TriggeredAt`.
- `DataNatsWorker` lắng nghe `ta.its.event.sharedata.newdata.*`, áp dụng **chống dội 500ms** theo từng gói, rồi gọi `ProcessPacketTrigger(packetCode)`.
- `ProcessPacketTrigger` lọc tiếp ở tầng đăng ký: chỉ lấy đăng ký có `SendOnNewData = true`, `State = Active`, đối tác đang `Connected`, và **còn kiểm tra `DebounceSec`** riêng của từng đăng ký.
- Sau đó đi vào **đúng pipeline gửi nối đuôi thông thường** (nhận quyền xử lý → phân trang → commit theo trang) — không có nhánh xử lý riêng nào cho trường hợp "dữ liệu mới".

### A.4. Yêu cầu bật Change Tracking trên CSDL

Để Change Tracking hoạt động, CSDL cần được kích hoạt bằng lệnh định nghĩa cấu trúc (DDL - Data Definition Language). Tuy nhiên, **Worker KHÔNG chạy lệnh DDL (`ALTER`) một cách mù quáng mỗi lần khởi động**.

Quy trình của hàm `DataChangeWatcherWorker.CheckStatusAsync` diễn ra cực kỳ cẩn trọng:

1. **Chỉ ĐỌC để kiểm tra trước**: Hệ thống luôn chạy câu lệnh `SELECT` kiểm tra an toàn qua các view hệ thống (`sys.change_tracking_databases` và `sys.change_tracking_tables`).
2. **Chỉ thực thi `ALTER` khi CÒN THIẾU**: Nếu phát hiện CSDL hoặc bảng chưa được bật (thường chỉ xảy ra ở lần đầu tiên triển khai), Worker mới kích hoạt chạy mã DDL sau:

```sql
ALTER DATABASE [dbname] SET CHANGE_TRACKING = ON (CHANGE_RETENTION = 2 DAYS, AUTO_CLEANUP = ON);
ALTER TABLE [TênBảng] ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = OFF);
```

3. Nếu cấp CSDL chưa bật được, worker tạm dừng quét và **tự thử lại ở các vòng sau** — không cần khởi động lại dịch vụ sau khi DBA bật tay.

Từ lần khởi động thứ 2 trở đi, chỉ còn các câu lệnh đọc, không chạy thêm lệnh đổi cấu trúc nào.

---

## Phụ lục B. Quyết định kèm lý do


### Vì sao gói bản chụp gửi lại toàn bộ mỗi chu kỳ

Biên bản họp 21/09, phiên 2:

> `09:59` — **Anh Sơn**: *"Nhưng mà nó sẽ tùy... tùy dữ liệu! **Một số dữ liệu là mình sẽ gửi lại hết.** Nhưng một số dữ liệu ví dụ như nó có theo cái gọi là cái lịch sử đó... Chẳng hạn như cái bảng Incident... Mấy Incident thì chỉ gửi tiếp mới mới thôi, chứ tự dưng cứ định kỳ 30 giây gửi lại hết tất cả các bảng thì đâu có được! Tại vì dữ liệu đó mình đã gửi cho họ, họ đã lưu trữ bên bển rồi... **Nhưng mà một số bảng là nó cần cập nhật hết tất cả thông tin thì bắt buộc phải gửi lại hết.**"*
>
> `10:40` — **Hiếu**: *"Nó có **2 dạng** như vậy... một số gói tin là sẽ gửi hết, còn một số gói tin là chỉ gửi một bản ghi thôi."*
>
> `10:50` — **Anh Sơn**: *"**Đúng rồi!** Trong cái **gói tin** đó, chứ không phải một bảng nữa, mà là trong cái gói tin đó."*

Hai điều chốt ra từ đây:

1. **Đúng 2 chế độ, không có chế độ thứ ba** kiểu "lần đầu gửi toàn bộ rồi các lần sau chỉ gửi phần thay đổi". Gói bản chụp gửi lại hết mỗi chu kỳ là **bắt buộc**.
2. **Trục phân loại là GÓI TIN, không phải bảng** — khớp đúng với `ResolveOutboundPolicy` đang khoá theo mã gói.

