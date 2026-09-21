# Prompt thực thi — Cờ "Chỉ gửi khi có dữ liệu mới" (SV-12)

> **Tài liệu sống**: `../Plan/sharedata-outbound-kiem-tra-anh-xa-va-dinh-dang.md` mục 3.5
> **Ma trận gói tin**: `../doc/02-mapping-goi-tin-101-111.md`

---

## Ký hiệu dùng xuyên tài liệu

Tài liệu tham chiếu chéo bằng mã ngắn. Bảng này để không phải đoán nghĩa:

| Mã | Là gì | Định nghĩa ở |
|---|---|---|
| **M1–M5** | Yêu cầu đã chốt trong họp, dẫn từ transcript | mục **MONG MUỐN** |
| **L1–L7** | **L**ỗi đang có trong mã hôm nay — chạy sai *ngay bây giờ*, không liên quan tới cờ | **§2** |
| **T1–T4** | **T**hiếu — thứ chưa tồn tại nên cờ bật cũng chưa chạy được | **§3** |
| **V1–V13** | **V**iệc phải làm; mỗi việc trỏ về một **L** hoặc **T** cụ thể | **§5** |
| **§N** | Số mục trong chính tài liệu này | tiêu đề `## N.` |

Ba mã bị nhắc sớm nhất, ghi luôn ở đây cho khỏi phải nhảy xuống:

| Mã | Một câu |
|---|---|
| **L1** | Cursor dùng `>=` thay vì `>` ⇒ lần sau đọc lại đúng mốc cũ ⇒ **kẹt vĩnh viễn ở 50 dòng đầu**, dòng thứ 51 không bao giờ được gửi |
| **L2** | **11/11** query không lọc `IsDelete` ⇒ dòng đã xóa mềm trên giao diện **vẫn đang gửi** cho đối tác |
| **L7** | Bộ lọc `State` của gói 110 so **chuỗi chữ** với cột lưu **chuỗi số** ⇒ **không loại được dòng nào** |

---

## HIỆN TRẠNG — mã hôm nay đang chạy thế nào

Đọc mục này **trước**. Chưa thấy mã hiện tại làm gì thì không đánh giá được phần sau là sửa hay làm
mới. Toàn bộ 11 câu query nằm ở `DataOutboundExtractionProcess.cs`.

| Gói | Bảng nguồn | `TOP` | `WHERE` | `ORDER BY` | `__watermark` | `__rowid` |
|---|---|---|---|---|---|---|
| 101 | `TmsZoneStatus` + `TmsZone` + `TmsTrafficStatistic` | — | — | — | — | — |
| 102 | `CctvDevice` + `TmsEquipment` *(join theo `Ip`)* | — | — | — | — | — |
| 103 | `TmsTrafficData` + `TmsEquipment` | **50** | `DetectTime >= @lastTime` | `DetectTime, ID` ASC | `td.DetectTime` | `td.ID` |
| 104 | `TmsWeather` | — | `TimeDetect >= @lastTime` | `TimeDetect, ID` ASC | `w.TimeDetect` | `w.ID` |
| 105 | `TollTransactionOut` + `TmsVehicleRegistration` *(join theo biển số)* | — | — | — | — | — |
| 106 | 🔴 **CHỐT BỎ — không xử lý gói này.** Hôm nay đang đọc `TmsTrafficData`, tức bảng dò xe VDS của gói 103; CSDL **không có bảng trạm cân, không có cột tải trọng** | **50** | `DetectTime >= @lastTime` | `DetectTime, ID` ASC | `td.DetectTime` | `td.ID` |
| 107 | `TmsIncident` + `TmsEventType` | — | `ISNULL(UpdateTime, StartDate) >= @lastTime` | cùng biểu thức ASC | `ISNULL(UpdateTime, StartDate)` | `i.ID` |
| 108 | `VmsCurrent` + `TmsEquipment` | — | — | — | — | — |
| 109 | `TollTransactionOut` + `TollLane` + `TollStation` | **50** | `TransactionDateTime >= @lastTime` | `TransactionDateTime, ID` ASC | `t.TransactionDateTime` | `t.ID` |
| 110 | `TmsIncident` + 2 lớp `OUTER APPLY` | — | 🔴 `State != 'FINISHED'/'CANCELED'/'Closed'/'Cancelled'` — **so chuỗi chữ với cột lưu chuỗi số ⇒ loại được 0 dòng** *(lỗi **L7**, §2)* | — | — | — |
| 111 | `TmsIncident` — `SELECT` trần | — | — | — | — | — |

Đọc ngang bảng ra ba nhóm: **5 gói có cursor** (103·104·106·107·109) · **3 gói có trần dòng**
(103·106·109) · **6 gói lấy trọn bảng mỗi chu kỳ** (101·102·105·108·110·111).

### `__watermark` và `__rowid` là gì

Hai cột cuối bảng trên là **alias nội bộ** do chính câu query tự sinh ra. Chúng **không bao giờ** xuất
hiện trong JSON gửi đối tác — `Extract` đọc chúng để biết *"đã lấy tới đâu"*:

| Alias | Trả lời câu | Dùng làm gì |
|---|---|---|
| `__watermark` | *"dòng này đổi lúc nào?"* | gửi xong thì lưu vào `LastTimeRun`; lần chạy sau lấy tiếp từ mốc đó |
| `__rowid` | *"dòng này là dòng nào?"* | phân biệt các dòng **trùng mốc thời gian**; lưu vào `LastDataId` |

Cần **cả hai** vì thời gian một mình không đủ. 50 giao dịch xảy ra trong cùng một giây thì chỉ có
`__watermark` sẽ không biết đã gửi tới dòng nào trong 50 dòng đó — đó đúng là **L1** *(cursor dùng `>=`
nên kẹt vĩnh viễn ở 50 dòng đầu; chi tiết §2)*.

Gói nào để trống hai cột này ở bảng trên là gói **chưa có cursor**: mỗi chu kỳ nó lấy lại từ đầu, không
có khái niệm "phần mới".

### 🔴 Một giá trị, năm cái tên — tài liệu này chỉ dùng `LastDataId`

Mã hôm nay gọi cùng một thứ bằng năm tên khác nhau. Đây là nguồn nhầm lẫn thật, nên **V2 gộp về một tên**:

| Tên trong mã hôm nay | Ở đâu | Đổi thành |
|---|---|---|
| `LastDataId` | cột/property trên `ShareDataSubscription` | **giữ — đây là tên chuẩn** |
| `lastId` | biến local `Extract:82` và tham số handler `(db, lastTime, lastId, ct)` | `lastDataId` |
| `@lastId` | tham số SQL trong query cursor | `@lastDataId` |
| `MaxLastId` | property của `DataOutboundExtractionResult` | `MaxLastDataId` |
| `result.LastId` | tuple trả về của `ExecuteExportForSubscription` | `result.LastDataId` |

⚠️ **`__rowid` KHÔNG phải tên thứ sáu** — đừng gộp nó vào. Hai thứ khác cấp:

| | Là gì |
|---|---|
| `__rowid` | cột trong **kết quả query**, mỗi dòng một giá trị |
| `LastDataId` | **một** giá trị lưu xuống CSDL = `__rowid` của **dòng cuối đã gửi thành công** |

### Khi nào `LastDataId` được ghi

Cột `LastDataId` là **bắt buộc phải có** — thiếu nó thì không gói active nào chạy được nhánh cursor, kể
cả 4 gói `FullOrChanged` vì cờ có thể bật bất cứ lúc nào. Nhưng **giá trị** thì không phải lúc nào cũng ghi:

| Ca | Ghi `LastDataId`? | Ghi `LastTimeRun`? |
|---|---|---|
| `AlwaysIncremental` (103·104·107·109) — cờ bật hay tắt | ✅ **luôn ghi** | ✅ max watermark |
| `FullOrChanged` (101·102·105·108) + **cờ bật** | ✅ **ghi** | ✅ max watermark |
| `FullOrChanged` + **cờ tắt** → nhánh all | 🔴 **KHÔNG ghi** | ✅ **vẫn ghi** = `GETDATE()` |

Hai điều phải đọc kèm bảng này:

**Vì sao nhánh all không ghi `LastDataId`.** Nhánh all chụp lại **toàn bộ** mỗi lần chạy. Khoá phân trang
của nó chỉ sống trong bộ nhớ một lần chạy; ghi xuống `LastDataId` sẽ khiến lần chạy sau bắt đầu từ
**giữa** ảnh chụp và **không bao giờ** chụp lại phần đầu. Xem §6.

**Vì sao nhánh all vẫn PHẢI ghi `LastTimeRun`.** Đó là mốc *"đã giao hàng tới đây"*. Không có nó thì lúc
bật cờ trở lại, hệ thống không biết bắt đầu từ đâu — xem mục *Bật, tắt, rồi bật lại* ở §5.

### Sáu điều cấu trúc phải biết trước khi sửa

**1. `LastDataId` đã được truyền vào cả 11 handler nhưng 0 câu SQL dùng.** Chữ ký thống nhất
`(db, lastTime, lastId, ct)` — tham số thứ ba chính là `LastDataId`, chỉ đang mang tên khác. Đường ống
có sẵn, chỉ chưa nối. Vì vậy **việc V2** *(§5)* thật sự chỉ là **một dòng**.

**2. Sáu gói snapshot gọi `SqlQueryAsync<dynamic>(sql)` không truyền parameter nào** — không `WHERE`,
không `TOP`, không `ORDER BY`. Lấy trọn bảng, mỗi chu kỳ.

**3. Gói snapshot không bao giờ tính cursor.** `Extract:99` bọc toàn bộ phần tính watermark trong
`if (!isSnapshot)`. Với snapshot thì `MaxWatermark` và `MaxLastId` *(tên mã của `LastDataId`)* **luôn
`null`**, rồi
`DataOutboundService.cs:301` rơi xuống `MaxWatermark ?? exportedAt` ⇒ **`LastTimeRun` của gói snapshot
là giờ chạy, không phải mốc dữ liệu**. Đừng tưởng nhánh all không chạm cursor — nó có chạm.

**4. Cách lấy max watermark phụ thuộc hoàn toàn vào `ORDER BY … ASC`.** `Extract` quét **từ cuối danh
sách lùi về đầu** (`for i = Count-1 downto 0`), lấy dòng **đầu tiên** có đủ cặp `__watermark` +
`__rowid` rồi `break`. Query nào thiếu `ORDER BY`, hoặc đổi sang `DESC`, thì cursor lấy sai dòng —
**nhảy hoặc lùi, im lặng**. Ràng buộc ngầm này trước nay chưa ghi ở đâu; xem §6.

**5. `SqlDateTimeFloor = new(1900, 1, 1)` (`:14`), và 5 gói incremental đều dùng
`floor = lastTime ?? SqlDateTimeFloor`.** Việc *"tự lùi về năm 1900 rồi dump toàn bộ lịch sử"* mà §5
**cấm** thì mã **đang làm sẵn**. Không phải phòng xa — là hiện trạng.

**6. 11/11 câu không có `IsDelete`** — soi mắt thường xác nhận **L2** *(dòng đã xóa mềm vẫn đang gửi
cho đối tác; chi tiết §2)*.

### Khối lượng thật — hai quả bom lớn hơn 107

🔴 **Gói 101 hôm nay ra ≈ 449.000 dòng, không phải 299.**

```sql
FROM TmsZoneStatus zs
LEFT JOIN TmsZone z ON zs.ZoneId = z.ID
LEFT JOIN TmsTrafficStatistic ts ON zs.ZoneId = ts.ZoneId   -- KHÔNG chọn dòng mới nhất
```

`TmsZoneStatus` 299 dòng, `TmsTrafficStatistic` **448.939 dòng / 10 `ZoneId`** ⇒ 10 zone có thống kê
bị nhân ≈ 44.894 lần mỗi zone. ⇒ **101 buộc phải chọn latest/aggregate TRƯỚC**, không phải "sửa sau
cho gọn"; chưa sửa thì phân trang vô nghĩa.

🔴 **Gói 105 lấy trọn `TollTransactionOut`** — bảng giao dịch thu phí, không `TOP`, không `WHERE`.
*(Chưa đo số dòng bảng này — phải đo trước khi bật.)*

🔴 **Gói 111** `SELECT … FROM TmsIncident` trần = 1.728 dòng staging, dù tài liệu ghi `skip`.

### 🔴 Gói 106 — CHỐT BỎ, không xử lý *(đo MCP 20/09)*

**Kết luận trước, bằng chứng sau:**

| Câu hỏi | Trả lời |
|---|---|
| Gói 106 lấy dữ liệu từ đâu? | **Không từ đâu cả.** Nguồn không tồn tại trong CSDL |
| Vậy đổi sang bảng khác? | **Không có bảng nào để đổi sang** — đã soi hết 150 entity |
| Vậy làm gì? | **Không xử lý.** Gỡ `QueryPacket106` khỏi registry — *việc **V13** ở §5* — gói báo `Disabled` |
| Query `TmsTrafficData` đang có thì sao? | **Bỏ**, không sửa cursor, không mở rộng — nó là bảng dò xe VDS, không phải số đo cân |
| Bao giờ làm lại? | Khi có bảng WIM thật. Lúc đó 106 mới lên `NotReady` rồi `AlwaysIncremental` |

Bằng chứng bên dưới.

Chính nghiệp vụ đã khai và đã tự đánh dấu là thiếu — bảng `ShareDataTable`, bản ghi `TBL106A`:

| Khai gì | Giá trị |
|---|---|
| `PacketCode` | `106_wimData` |
| `TableName` | `TmsTrafficData` · `IsRoot = true` |
| `Remark` | *"Chỉ dùng khi WIM thực sự ghi vào TmsTrafficData; hiện chưa có cờ nguồn WIM."* |

`FieldsJson` khai 11 trường, **4 trường cốt lõi có `columnName: null`** (`noSource: true`):

| Trường | Tên | Nguồn |
|---|---|---|
| `grossWeight` | Tổng tải trọng (kg) | ❌ không có cột |
| `axleWeights` | Tải trọng theo trục | ❌ |
| `axleCount` | Số trục | ❌ |
| `isOverweight` | Quá tải (bool) | ❌ |

Bảy trường còn lại (`detectTime`·`lane`·`locationCode`·`speed`·`height`·`width`·`length`) đều lấy từ
`TmsTrafficData` — tức **dữ liệu kích thước xe do cảm biến dò xe VDS sinh ra**, không phải số đo cân.

Và `ShareDataPacket.PKT106` ghi thẳng: *"Nguồn: TmsTrafficData. Gói dự kiến làm sau; thiếu bảng/cột tải
trọng."*

**Điều kiện trong `Remark` đo được là KHÔNG thỏa:**

| Đo gì | Kết quả |
|---|---|
| Loại thiết bị trạm cân | ✅ có — `TmsEquipmentType.Code = 'WOS'`, Name *"Trạm cân"* |
| Trạm cân đã khai thiết bị | ✅ 3 cái — `WOS01` Trạm cân E (Km103) · `WOS02` Trạm cân F (Km103) · `WOS03` |
| Dòng `TmsTrafficData` từ 3 trạm cân đó | 🔴 **0** |
| Phân bố `Code` trên 254.013 dòng | `null` 254.003 · `VDS001` 2 · `VDS004` 1 · `CCTV0xx` 7 — **không có `WOS` nào** |
| Cột tải trọng trong `TmsTrafficData` | 🔴 **không có** — chỉ `AlarmHeight/Width/Length/Speed`, tức cảnh báo **quá khổ**, không phải **quá tải** |
| Bảng nào khác chứa tải trọng | 🔴 **không** — 150 entity, không có `Wim`/`Weigh`/`Axle`/`Weight`. `TmsEventData` và `TmsVehicleFilter` đều không có |

⇒ Trạm cân được khai trong danh mục thiết bị nhưng **không đẩy dữ liệu vào đâu cả**.

**Nếu có đối tác đăng ký gói 106**, họ nhận các dòng do cảm biến dò xe sinh ra, **dán nhãn là dữ liệu
trạm cân**, với 4/11 trường luôn `null`. Hiện **chưa có đăng ký nào** dùng 106 (chỉ 2 đăng ký, cả hai
là `101_commonData`) ⇒ rủi ro **tiềm ẩn, chưa phát tác**.

⇒ Vì vậy 106 xuống nhóm **`Disabled`** cùng 111, không phải `NotReady` — xem §7.

*Giới hạn của phép đo*: DAB chỉ phơi entity đã khai trong `dab-config`, nên nghiêm ngặt kết luận là
"config không khai bảng WIM". Config có 150 entity kể cả `HangFire_*` nội bộ nên khả năng cao sinh từ
full schema; muốn chắc tuyệt đối cần một lần soi `INFORMATION_SCHEMA.TABLES`.

---

## MONG MUỐN — chốt trong họp, dẫn từ transcript

Mục này **không phải** thiết kế. Đây là yêu cầu đã chốt, kèm số dòng transcript để tra lại. Phần kỹ
thuật đáp ứng nó nằm từ §1 trở đi. **Không tự đổi** các gạch đầu dòng ở đây — muốn đổi thì họp lại.

### M1 · Tách rõ hai kiểu lịch — *họp 09/09*

Anh Sơn: đối tác cần **đúng 9h sáng mỗi ngày gửi một lần** dữ liệu tổng hợp của ngày hôm trước
(`2026-09-09-review-sharedata.md:136-139`). Hai kiểu phải tách rạch ròi:

| Kiểu | Là gì | Ví dụ |
|---|---|---|
| **Định kỳ** (`continuous`) | lặp lại sau mỗi chu kỳ | 30s · 60s · 5 phút |
| **Giờ cố định hàng ngày** (`daily`) | chạy một lần vào giờ đã hẹn | `09:00:00` |

### M2 · Bỏ "theo sự kiện" và "gửi 1 lần", thay bằng MỘT switch — *họp 09/09*

Anh Sơn: gửi theo *sự kiện* với *gửi 1 lần* **bỏ đi**; thay vào đó thêm một switch *"Tự động gửi khi
có dữ liệu mới"* (`:142-147`, nhắc lại ở `:169-171`).

- **Bật**: DB phát sinh bản ghi mới thì đẩy gói tin đi.
- **Tắt**: chỉ gửi theo lịch định kỳ hoặc giờ cố định đã cài.

Lý do bỏ *"gửi 1 lần"*: muốn test thì bấm nút test trên giao diện, không cần dựng riêng một chế độ
gửi (`:145`).

### M3 · Nhận biết "dữ liệu mới" bằng cột thời gian CSDL — *họp 16/09*

Thay radio *"Gửi theo sự kiện"* bằng *"Gửi ngay khi có dữ liệu mới"*, dựa vào `UpdateTime` /
`CreateTime` trong CSDL để phát hiện bản ghi mới sinh ra (`16-09-2026-sua-ui-sharedata.md:30`).

### M4 · Cờ chỉ áp chiều GỬI — *họp 16/09*

Bỏ toàn bộ cấu hình kiểu lịch ở **chiều nhận**, vì nhận dữ liệu là **bị động** theo request của đối tác
(`16-09-2026-sua-ui-sharedata.md:32`).

⇒ Không đẻ cờ cho chiều NHẬN. Việc **SV-13** đã bỏ khỏi kế hoạch.

### M5 · Hai rủi ro đã được cảnh báo trước — *họp 16/09*

| Rủi ro | Nội dung | Nguồn |
|---|---|---|
| **Nghẽn CSDL** | nhiều đối tác cùng gửi chu kỳ dày (29–30s) ⇒ nhiều tiến trình cùng truy vấn/insert một bảng ⇒ quá tải CPU/RAM, treo hệ thống ITS | `16-09-2026-dinh-danh-doi-tac-va-test-tai.md:40` |
| **Cắt cụt chuỗi dài** | trường text/JSON dài (giới hạn 4000 ký tự ở một số hệ thống) bị cắt đuôi ⇒ **lỗi parse JSON** ở bên nhận | `:41` |

Dự phòng nếu test tải cho thấy `DEV_ITS10` bị ảnh hưởng: tách CSDL phụ chuyên nhận ShareData rồi đồng
bộ có kiểm soát sang DB chính (`:43`).

> Hai rủi ro này là lý do **L5** (8 gói không có trần dòng) và **L6** (mồi 1900) không được xem là
> chuyện nhỏ: gói 101 ra ≈449.000 dòng vừa nghẽn DB vừa chắc chắn vượt mọi giới hạn chuỗi.

### 🔴 Chỗ mong muốn KHÔNG khớp được với hiện thực

Transcript `:146` ghi *"đẩy gói tin đi **ngay**"*. Đợt này **không có** NATS · CDC · trigger SQL, nên
không tồn tại đường nào nhanh hơn nhịp lịch:

| Cấu hình | Độ trễ thật |
|---|---|
| `continuous` 30s + cờ bật | ≤ 30 giây |
| `daily` 09:00 + cờ bật | tới 09:00 hôm sau |

Vì vậy anh Đạt chốt **20/09**: **lịch quyết định KHI NÀO chạy, cờ chỉ quyết định LẤY BAO NHIÊU dữ
liệu** — xem §1. Tên gọi đúng của cờ là *"chỉ gửi khi có dữ liệu mới"*, không phải *"gửi ngay"*.

Đây là chỗ duy nhất trong tài liệu mà hiện thực đi chệch nguyên văn họp; ghi ra để ai đọc sau không
tưởng là làm thiếu.

---

## 1. Cách làm

**Lịch quyết định KHI NÀO chạy. Cờ chỉ quyết định LẤY BAO NHIÊU dữ liệu.**

Hai việc này độc lập nhau. `SendOnNewData` **không** tham gia tính `NextTimeRun` — nó không đổi
nhịp, không thay lịch, không dựng đồng hồ thứ hai. Lịch `continuous`/`daily` sẵn có vẫn là thứ duy
nhất quyết định thời điểm chạy; cờ chỉ chọn câu query nào được dùng khi lần chạy đó đã tới.

### Ma trận 4 ca

| Lịch | Cờ | Chạy khi nào | Query lấy gì |
|---|---|---|---|
| `continuous` (`IntervalSeconds`) | tắt | mỗi N giây | **toàn bộ**, phân trang đến hết |
| `continuous` | bật | mỗi N giây | **chỉ dòng đổi sau cursor** |
| `daily` (`HH:mm` + `DaysOfWeek`) | tắt | đúng giờ hẹn | **toàn bộ** |
| `daily` | bật | đúng giờ hẹn | **chỉ dòng đổi sau cursor**, tích luỹ từ lần hẹn trước |

Vì `daily` + cờ bật vẫn **chờ tới giờ hẹn**, tên gọi đúng của cờ là *"chỉ gửi khi có dữ liệu mới"*,
không phải *"gửi ngay"*. Không có NATS, không có trigger ⇒ không có đường nào "ngay" hơn lịch.

Gói `AlwaysIncremental` (§7) đi nhánh cursor ở **cả hai** trạng thái cờ — với nhóm này cờ vô hại.
Tuyệt đối không được để cờ tắt làm các gói đó gửi lại toàn bộ lịch sử.

> Tài liệu này gọi cờ là *"chỉ gửi khi có dữ liệu mới"*, nhưng **không** đổi
> `ColumnDescription = "Gửi ngay khi có dữ liệu mới"` trong `ShareDataSubscription.cs:65`. Hiếu vừa
> sửa tệp entity đó; rule 5 `thienan_rules.md` tôn trọng code sửa tay. Chỉ đổi cách gọi trong tài liệu.

### Ba định nghĩa cho nhánh cursor

| Khái niệm | Là gì |
|---|---|
| *"đổi lúc nào"* | `COALESCE(UpdateTime, CreateTime, <domainTime>)` — cột **audit**, không phải thời gian nghiệp vụ |
| *"chỗ tôi dừng"* | cặp `(LastTimeRun, LastDataId)` lưu trên từng subscription |
| *"ghi lại chỗ dừng"* | **chỉ sau khi** HTTP trả 200, và ghi **sau từng trang** (§6) |

### Một lần chạy thật

Gói 107 (sự cố giao thông) · `IntervalSeconds = 30` · lịch `continuous` · **cờ bật** · đầu vào
`LastTimeRun = 14:00:00`:

| Lúc | Chuyện xảy ra | Worker làm gì |
|---|---|---|
| `14:00:30` | — | hỏi *"dòng nào đổi sau 14:00:00?"* → **0 dòng** → không gọi API, cursor giữ nguyên |
| `14:00:45` | trực ban nhập sự cố **#501** | CSDL ghi `CreateTime = 14:00:45`, `UpdateTime = **NULL**` |
| `14:01:00` | — | mốc đổi của #501 = `COALESCE(NULL, 14:00:45)` = `14:00:45` ✓ → **gửi** → HTTP 200 → *bây giờ mới* ghi `LastTimeRun = 14:00:45`, `LastDataId = 501`, `SerialNbr +1` |
| `14:02:00` | trực ban sửa #501 | CSDL ghi `UpdateTime = 14:02:00` |
| `14:02:30` | — | mốc đổi = `COALESCE(14:02:00, …)` > `14:00:45` ✓ → **gửi lại #501** nội dung mới |
| `14:05:30` | (#501 đã bị xóa mềm lúc `14:05:00`) | `WHERE IsDelete IS NULL` loại #501 → **0 dòng, không gửi**. Đối tác vẫn treo #501 — xem §5 *tin xóa* |

Hai điều phải đọc kèm bảng này:

- 107 thuộc `AlwaysIncremental`, nên diễn biến trên đúng cho **cả** cờ bật và cờ tắt.
- Bảng mô tả hành vi **sau khi** làm V3. Mã hôm nay dùng `ISNULL(i.UpdateTime, i.StartDate)`
  (`:391`) — thiếu `CreateTime`, nên dòng `14:01:00` chưa chạy đúng như vậy. Xem **L4**.

### Nguồn chốt

Yêu cầu gốc nằm trọn ở mục **MONG MUỐN** phía trên (M1–M5), kèm số dòng transcript. §1 chỉ mô tả **cơ
chế đáp ứng** — không phải nguồn sự thật nghiệp vụ, và được phép bàn lại.

---

## 2. Lỗi ĐANG CÓ trong mã hôm nay

Bảy lỗi này chạy sai **ngay bây giờ**, không liên quan gì tới cờ:

| # | Lỗi | Ở đâu | Hậu quả |
|---|---|---|---|
| **L1** | `TOP 50` + `>= @lastTime` mà `lastTime` = `MaxWatermark` lần trước ⇒ luôn đọc lại mốc đó | 103·106·109 — `DataOutboundExtractionProcess.cs:251`·`:346`·`:441` | ≥50 dòng cùng một giây ⇒ **kẹt vĩnh viễn** ở 50 dòng đó; dòng thứ 51 trở đi **không bao giờ được gửi**, đối tác nhận đi nhận lại 50 dòng cũ |
| **L2** | **0/11** query dữ liệu nghiệp vụ lọc `IsDelete` *(bảng cấu hình thì có — `DataOutboundService.cs:80`·`:84`)* | cả 11 gói | sự cố đã xóa mềm trên giao diện **vẫn đang gửi** cho đối tác như dữ liệu sống |
| **L3** | cursor lấy **thời gian nghiệp vụ** thay vì cột audit: `td.DetectTime` (`:267`·`:357`), `w.TimeDetect` (`:301`), `t.TransactionDateTime` (`:459`) | 103·104·106·109 | dòng insert trễ mà mang mốc nghiệp vụ cũ ⇒ **bỏ sót im lặng** |
| **L4** | 🔴 gói 107 watermark = `ISNULL(i.UpdateTime, i.StartDate)` — **thiếu `CreateTime`** | `:391` (SELECT) · `:395` (WHERE) · `:396` (ORDER BY) | sự cố mới có `UpdateTime = NULL` (§4) rơi xuống `StartDate` = thời gian nghiệp vụ ⇒ dính đúng L3. 107 là gói duy nhất *trông như* đã dùng audit time nhưng **chưa** |
| **L5** | **8 gói không có trần dòng nào**: 101·102·104·105·107·108·110·111 *(`TOP 50` chỉ có ở 103·106·109)* | cả 8 | Nặng nhất **không phải 107**: gói **101** ra ≈ **449.000 dòng** vì JOIN `TmsTrafficStatistic` không chọn dòng mới nhất · gói **105** lấy trọn `TollTransactionOut` (chưa đo) · gói **111** trần 1.728 dòng dù tài liệu ghi `skip` · 107 = 1.728 dòng. Tất cả trong **một** POST |
| **L6** | 🔴 `SqlDateTimeFloor = new(1900, 1, 1)` (`:14`) làm giá trị mồi: `floor = lastTime ?? SqlDateTimeFloor` | 103·104·106·107·109 | `LastTimeRun == null` ⇒ quét **từ năm 1900** ⇒ dump toàn bộ lịch sử. Cộng với **L5** (không trần) thì lần chạy đầu tiên là lần nổ payload. §5 **cấm** hành vi này, nhưng mã **đang làm sẵn** |
| **L7** | 🔴 Gói 110 lọc `State` bằng **chuỗi chữ**, trong khi CSDL lưu **chuỗi số** | `:474-478` | `State != 'FINISHED'/'CANCELED'/'Closed'/'Cancelled'` không bao giờ khớp ⇒ **loại được 0 dòng**. Đo dev 386 dòng: `5` Đã hủy **160** · `4` Đã xử lý xong **141** · `0` Đã bỏ qua **74** ⇒ **375/386 = 97%** sự cố đã đóng vẫn lọt. Gói 110 là gói phát cảnh báo cho **người đi đường** ⇒ biển báo hiển thị sự cố đã kết thúc từ lâu |

`>= @lastTime` có **đúng 5 chỗ**: `:267` `:301` `:357` `:395` `:459`. Bốn trong năm dùng thời gian
nghiệp vụ, chỗ thứ năm là L4.

🔴 **Chỉ sửa 4 chỗ, chỗ thứ 5 thì xoá.** `:357` thuộc `QueryPacket106` — gói đó **đã chốt bỏ**, nên
V13 gỡ cả hàm thay vì sửa cursor. Vì vậy L1 và L3 tính trên 103·104·109; 106 xuất hiện trong danh sách
chỉ để nói *"hôm nay nó cũng sai như vậy"*, không phải việc phải sửa.

---

## 3. Đang THIẾU — nên cờ bật cũng chưa chạy được

| # | Thiếu | Ở đâu |
|---|---|---|
| **T1** | cột `LastDataId` | chưa tồn tại (`grep -rn "LastDataId" src/` = **0**) |
| **T2** | cursor cho các gói active còn lại | **4 gói cần dựng**: 101 · 102 · 105 · 108. **3 gói gate lại**, không viết query: 110 (`NotReady`) · **106 · 111** (`Disabled`) — xem §7. Riêng 106 còn phải **gỡ query đang có** (V13) |
| **T3** | caller **bỏ** `result.LastId` | `DataOutboundService.cs:153` chỉ lấy `LastTimeRun`, trong khi `DataOutboundExtractionResult` đã có `MaxLastId` — **có sẵn mà không ai dùng** |
| **T4** | nhánh `Mode == Event → 5s` cắt ngang lịch | `DataOutboundScheduler.cs:16`·`:38-39` — đây là thứ **duy nhất** trong mã hôm nay đang "thay lịch". Gỡ nó là **điều kiện tiên quyết** của §1, không phải một tối ưu rời. Còn nó thì `daily` + cờ bật vẫn bị kéo về poll 5 giây |

---

## 4. Bẫy duy nhất phải tránh khi sửa

`EntityTenant` đặt `IsOnlyIgnoreInsert = true` cho `UpdateTime` ⇒ **`UpdateTime` bị loại khỏi câu
INSERT** ⇒ **dòng vừa tạo luôn có `UpdateTime = NULL`**. `Shared.Core.dll` không có mã nguồn trong
repo; nơi **duy nhất** ghi lại điều này là chú thích `SqlSugarFactory.cs:27-42` của WP_Sync.

⇒ Dùng `UpdateTime` **đứng trần** làm mốc là **mất dữ liệu im lặng** — không lỗi, không cảnh báo.

Đo được trên staging: `TmsZoneStatus` có **35/299 dòng** `UpdateTime = NULL`. Không phải lý thuyết.

### `COALESCE` làm gì

Hàm SQL trả về **giá trị không-NULL đầu tiên** trong danh sách — đúng bằng toán tử `??` của C#:

```csharp
UpdateTime ?? CreateTime           // C#
COALESCE(UpdateTime, CreateTime)   // SQL   (ISNULL(a, b) cũng vậy nhưng chỉ nhận 2 tham số)
```

Cần nó vì **không cột nào trả lời trọn câu *"dòng này đổi lần cuối lúc nào"*** — mỗi cột kể một nửa:

| Dòng `TmsIncident` | `CreateTime` | `UpdateTime` | `COALESCE(UpdateTime, CreateTime)` |
|---|---|---|---|
| #501 vừa nhập lúc 14:00:45 | `14:00:45` | `NULL` | **`14:00:45`** ← rơi xuống `CreateTime` |
| #501 sau khi sửa lúc 14:02 | `14:00:45` | `14:02:00` | **`14:02:00`** ← lấy `UpdateTime` |

| Nếu chỉ dùng | Hỏng ở đâu |
|---|---|
| `UpdateTime` | dòng mới có `UpdateTime = NULL`; `NULL > @lastTime` ra **`NULL`**, mà `WHERE` coi `NULL` là **không khớp** ⇒ **sự cố mới không bao giờ được gửi** |
| `CreateTime` | `CreateTime` **không đổi khi sửa bản ghi** ⇒ #501 sửa lúc 14:02 vẫn mang mốc 14:00:45, đã nhỏ hơn `LastTimeRun` ⇒ **bản sửa không bao giờ được gửi** |
| `ISNULL(UpdateTime, <domainTime>)` *(L4)* | bỏ qua `CreateTime`, nhảy thẳng xuống thời gian nghiệp vụ ⇒ dòng insert trễ mang mốc cũ bị **bỏ sót im lặng** |
| `COALESCE(UpdateTime, CreateTime)` | ✅ bắt được **cả hai** |

Tham số thứ ba `<domainTime>` (`DetectTime`, `StartDate`…) là lưới an toàn cuối: chỉ rơi xuống khi
**cả hai** cột audit rỗng, **và phải ghi warning** — vì lúc đó dữ liệu bảng đó có vấn đề.

---

## 5. Việc phải làm

| # | Việc | Sửa |
|---|---|---|
| **V1** | Thêm property `LastDataId` vào `Module.ShareData.Core/Entities/ShareDataSubscription.cs`, đặt cạnh `SendOnNewData` | T1 |
| **V2** | `DataOutboundExtractionProcess.Extract`: `var lastId = "";` (`:82`) → `sub.LastDataId ?? string.Empty`. **Kèm gộp tên**: `lastId` → `lastDataId` · `@lastId` → `@lastDataId` · `MaxLastId` → `MaxLastDataId` · `result.LastId` → `result.LastDataId`, để mã và tài liệu dùng **đúng một** tên *(xem bảng ở mục HIỆN TRẠNG)* | T1 |
| **V3** | **4 gói** đã có cursor (103·104·107·109): đổi `>=` → **cursor kép** và đổi sang **audit time** | L1 · L3 · L4 |
| **V4** | **4 gói** chưa có cursor (101·102·105·108): dựng `__watermark` / `__rowid` theo khuôn §6 | T2 |
| **V5** | **Mọi** query thêm `WHERE main.IsDelete IS NULL` — cả nhánh all lẫn nhánh cursor | L2 |
| **V6** | `ProcessBatchSubscriptions` nhận **cả** `result.LastDataId` *(tên sau khi V2 gộp; hôm nay là `result.LastId`)*, truyền vào `TryPersistExportResult` | T3 |
| **V7** | `DataOutboundScheduler`: gỡ nhánh `Mode == Event → 5s` và hằng `EventPollIntervalSeconds` | T4 |
| **V8** | Cô lập việc đọc cờ vào **đúng một hàm** ở Worker; không nơi nào khác đọc trực tiếp property | — |
| **V9** | 🔴 Chặn `LastTimeRun` / `LastDataId` đi vào từ DTO API | mới |
| **V10** | 🔴 Khởi tạo cursor bằng **giờ CSDL**, và chỉ ở đúng hai chỗ | mới |
| **V11** | 🔴 Vòng lặp phân trang **có phanh**; tách *commit trang* khỏi *nhả lease* | L5 · R9 |
| **V12** | Page size và budget đọc từ **cấu hình**, không hardcode | L5 |
| **V13** | 🔴 **Gỡ `QueryPacket106`** khỏi `PacketQueryRegistry`; gói 106 báo `Disabled` | nguồn không tồn tại |

### V1 — CodeFirst, không tệp `.sql`

```csharp
// Rule 7 .agents/rules/thienan_rules.md:147 — CẤM soạn/chạy DDL thủ công.
// Thêm cột entity thì BẮT BUỘC đi qua SqlSugar CodeFirst (EnableInitTable / EnableIncreTable).
// KHÔNG sinh tệp .sql cho việc này.
// Báo Hiếu trước khi đụng tệp entity (anh ấy vừa sửa chính nó ở commit 7f035e63).
[SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength,
    ColumnDescription = "ID dòng dữ liệu cuối đã gửi thành công tại LastTimeRun")]
public string? LastDataId { get; set; }
```

`Length` chỉ gán cho `string`; gán vào kiểu số/ngày sẽ sinh DDL sai dạng `INT(64)`.

### V8 — một cửa đọc cờ

```csharp
/// <summary>
/// Description: Xác định subscription chỉ gửi phần dữ liệu mới hay lấy toàn bộ.
/// Không liên quan tới thời điểm chạy — lịch continuous/daily vẫn quyết định NextTimeRun.
/// Null hoặc false = lấy toàn bộ. Chỉ có nghĩa với đăng ký chiều GỬI.
/// Created date: 20/09/2026
/// </summary>
public static bool IsChangedRowsOnly(ShareDataSubscription sub)
    => sub.SendOnNewData == true;
```

### V9 — lỗ hổng cursor qua request body

`ShareDataAddSubscriptionInput : ShareDataSubscription` (`ShareDataSubscriptionInput.cs:48`) kế thừa
thẳng entity, nên `LastTimeRun` và `LastDataId` **nằm trong request body của API**. Add handler gọi
`command.Adapt<ShareDataSubscription>()` (`ShareDataSubscriptionCommandHandler.cs:105`) copy thẳng
mọi property ⇒ ai gọi API cũng đặt được `LastTimeRun = 1900-01-01` và **ép hệ thống dump toàn bộ
lịch sử** cho đối tác.

| Chỗ sửa | Làm gì |
|---|---|
| Add handler (`:98-105`) | null hoá `LastTimeRun` · `LastDataId` **sau** `Adapt`, trước khi insert |
| Update handler (`:152`) | **không** map hai field này từ command; chỉ Worker được ghi |

Cursor là trạng thái nội bộ của Worker. Không field nào của nó được nhận giá trị từ ngoài vào.

### V10 — khởi tạo cursor: giờ CSDL, và chỉ khi chưa từng gửi

Lấy mốc bằng `SELECT GETDATE()` trên chính connection đang dùng, **không** dùng `DateTime.Now` của
WebAPI: Worker và WebAPI có thể lệch giờ — lệch âm ⇒ gửi trùng, lệch dương ⇒ **mất dữ liệu im lặng**.

🔴 **Điều kiện không phải "tắt → bật", mà là "đã từng gửi thành công lần nào chưa".** Chỉ một quy tắc,
áp cho **mọi** đường vào — tạo mới, sửa cờ, Resume:

| Tình huống | Cursor |
|---|---|
| **`LastTimeRun == null`** — chưa gửi thành công lần nào | `LastTimeRun = GETDATE()`, `LastDataId = null`. **Không** backfill lịch sử |
| **`LastTimeRun != null`** — đã gửi rồi, **bằng nhánh all hay cursor đều vậy** | **GIỮ NGUYÊN** `LastTimeRun`; `LastDataId = null` |
| **Pause → Resume** (`ChangeStateAsync:377`) | **giữ nguyên cursor** — xem *Resume* bên dưới. Cùng một quy tắc, không cần ngoại lệ |
| update cờ **bật → tắt** | **không chạm cursor**. Nhánh all không đọc `LastDataId`, nhưng vẫn ghi `LastTimeRun` (§6) nên mốc luôn tươi |

`LastTimeRun` luôn có một nghĩa duy nhất: **"đã giao hàng tới mốc này"** — bất kể lần chạy trước đi
nhánh nào. Nhờ vậy không cần nhớ trạng thái cờ trong quá khứ.

Worker gặp cờ bật mà `LastTimeRun == null` *(tức WebAPI chưa khởi tạo)* thì **không** được tự lùi về
năm 1900 rồi dump lịch sử: ghi lỗi cấu hình và **dừng** subscription đó.

#### 🔴 Ca bật → tắt → bật lại

Đây là ca làm lộ ra vì sao **không** được dựng lại mốc bằng `GETDATE()`. Gói 101, `continuous` 30s:

| Lúc | Cờ | Nhánh | Gửi gì | `LastTimeRun` sau khi chạy |
|---|---|---|---|---|
| `09:00:30` | tắt | all | toàn bộ 299 zone | `09:00:30` |
| `09:01:00` | tắt | all | toàn bộ 299 zone | `09:01:00` |
| `09:01:20` | — | — | *(zone A vừa đổi)* | — |
| `09:01:40` | **bật** | — | — | ❓ |
| `09:02:00` | bật | cursor | ? | |

Zone A đổi lúc `09:01:20`, **không** nằm trong ảnh chụp `09:01:00` vì lúc đó chưa đổi.

| Nếu lúc bật cờ | Query `09:02:00` lấy | Zone A |
|---|---|---|
| dựng lại `LastTimeRun = GETDATE() = 09:01:40` | `> 09:01:40` | 🔴 **mất vĩnh viễn** |
| **giữ nguyên** `LastTimeRun = 09:01:00` | `> 09:01:00` | ✅ **bắt được** |

**Trả lời câu hỏi thường gặp**: bật lại **không** làm gửi trùng phần dữ liệu của lúc cờ tắt. Lúc tắt,
nhánh all đã gửi **toàn bộ** bảng mỗi lần chạy nên đối tác có đủ; lúc bật, cursor chỉ lấy phần đổi **sau
ảnh chụp cuối cùng**. Không trùng, không thiếu.

Một hệ quả nhỏ chấp nhận được: `LastDataId = null` làm điều kiện keyset thành
`ChangeTime = @lastTime AND ID > ''` ⇒ **mọi dòng đúng tại mốc đó** được gửi lại. Trùng có giới hạn bằng
số dòng cùng một mốc giây — thà vậy còn hơn mất dữ liệu, và đúng với **at-least-once** đã chốt ở §6.

### V11 · V12 — vòng lặp có phanh

`DataOutboundService.cs:118` đặt `leaseSeconds = Math.Max(300, interval * 3)` ⇒ **lease tối thiểu
300 giây**. Một lần chạy được phép gửi nhiều trang trong khoảng đó, nhưng phải có hai lớp phanh:

| Phanh | Mặc định | Vì sao cần |
|---|---|---|
| trần số trang | **20 trang** | chặn một lần chạy ôm hết 448.939 dòng |
| trần thời gian | **50% lease** (= 150s khi lease 300s) | số trang **không** chặn được thời gian — một trang chậm 30s vẫn phá lease |

Chạm **bất kỳ** lớp nào thì thoát vòng lặp, nhả lease, tick sau tiếp tục từ cursor đã commit.

**Tách hai việc ghi** — đây là điểm dễ làm sai nhất:

| Việc | Ghi gì | Điều kiện | Bao nhiêu lần |
|---|---|---|---|
| **commit trang** | `LastTimeRun` · `LastDataId` · `SerialNbr +1` | OCC `ID = @id AND NextTimeRun = @leaseExpiry` | mỗi trang gửi thành công |
| **nhả lease** | `NextTimeRun = ComputeNextTimeRun(...)` | cùng OCC | **đúng một lần**, trong `finally` |

Commit trang **tuyệt đối không đụng `NextTimeRun`** — nhờ vậy điều kiện OCC
(`TryPersistExportResult:308-332`) vẫn khớp suốt vòng lặp và lease không bị nhả giữa đường. Hôm nay
`TryPersistExportResult` được gọi **một lần** trong `finally` (`:158`) với một `lastTimeRunUpdate`
duy nhất; V11 là việc tách nó thành hai đường ghi khác nhau.

`SerialNbr` tăng **mỗi trang**, vì mỗi trang là **một gói tin** gửi đối tác — không phải mỗi lần chạy.

### 🔴 Thứ tự bắt buộc: commit cursor TRƯỚC, ghi log SAU

Hôm nay `LogExportResult` nằm ở `DataOutboundService.cs:297`, **trước** dòng `return` trả giá trị
cursor ở `:301`. Câu INSERT đó gán `DatatypeId = sub.DatatypeId` (`ShareDataTransferLog.cs:64`) vào
cột `Length32`, mà `DatatypeId` trên staging là **GUID 36 ký tự** ⇒ ném
`String or binary data would be truncated` ⇒ `:301` không chạy ⇒ `lastTimeRunUpdate` vẫn `null` ⇒
`finally` ghi `null` ⇒ **cursor không tiến, gửi lại đúng lô đó mãi**.

Đảo thứ tự gỡ được ngay trong V11, **không cần chờ ai sửa cột**:

```text
POST 200
  → commit trang (LastTimeRun · LastDataId · SerialNbr)   ← TRƯỚC
  → LogExportResult(...)  bọc try/catch RIÊNG              ← SAU
```

Log ném thì mất một dòng nhật ký, cursor vẫn tiến. Nhật ký là thứ phụ, **không được phép chặn tiến độ
gửi** — đúng kể cả sau khi cột `Length32` đã sửa. Việc sửa cột vẫn nên làm (ba bảng dùng chung hai
chiều ⇒ **Hiếu xử**), nhưng không còn chặn SV-12.

### Khi nào ghi cursor

| Kết quả | `LastTimeRun` | `LastDataId` | `SerialNbr` | `NextTimeRun` |
|---|---|---|---|---|
| không có dòng mới | giữ | giữ | giữ | tiến |
| mapping lỗi · HTTP lỗi | giữ | giữ | giữ | tiến để retry |
| **một trang HTTP 200** — nhánh **cursor** | max watermark của trang | row ID cuối trang | +1 | **không đụng** |
| 🔴 **một trang HTTP 200** — nhánh **all** | `GETDATE()` | **KHÔNG ghi** | +1 | **không đụng** |
| 🔴 **hết budget** (20 trang / 150s) | như hai dòng trên, lấy trang cuối đã gửi | như trên | (đã +1 từng trang) | tiến — tick sau tiếp tục |
| vòng lặp xong, hết dữ liệu | như hai dòng trên | như trên | (đã +1 từng trang) | tiến |

Hai dòng giữa là chỗ dễ làm sai nhất: **chỉ nhánh cursor ghi `LastDataId`**. Nhánh all ghi `LastTimeRun`
để giữ mốc *"đã giao hàng tới đây"*, nhưng khoá phân trang của nó chỉ nằm trong bộ nhớ — xem §6.

Hết budget **không phải lỗi**. Ghi log mức Information kèm số trang đã gửi và số dòng còn tồn, để
vận hành đọc log không tưởng là hệ thống nghẽn.

### Lần đầu bật cờ trên subscription CHƯA GỬI lần nào

Chỉ áp khi `LastTimeRun == null`: gửi dữ liệu **phát sinh sau khi bật**, **không** backfill lịch sử —
`GETDATE()` làm mốc khởi đầu.

⚠️ **Đừng đọc mục này thành "mỗi lần bật cờ đều lấy `GETDATE()`".** Subscription đã gửi ít nhất một lần
(kể cả bằng nhánh all) thì `LastTimeRun != null` và phải **giữ nguyên** — xem ca *bật → tắt → bật lại*
ở V10. Resume sau Pause cũng đi đúng quy tắc đó, không phải ngoại lệ.

### 🔴 Resume sau Pause — CÓ backfill *(chốt 20/09)*

`ChangeStateAsync` (`:377`) **giữ nguyên** cursor khi Resume. Dữ liệu phát sinh trong lúc Pause được
gửi bù đủ, không bỏ.

Ví dụ thật, gói 107 pause một tuần:

| Số | Giá trị |
|---|---|
| tồn đọng | ~1.728 dòng (`TmsIncident` staging) |
| số trang @50 dòng | 35 trang |
| budget | 20 trang/lần chạy |
| `continuous` 30s | **2 tick ≈ 1 phút** là hết |
| `daily` 08:00 | **2 ngày** |

Ghi con số này vào tài liệu vận hành. Resume một subscription `daily` bị pause lâu thì hai ngày đầu
sẽ thấy lưu lượng cao bất thường — đó là hành vi **đúng**, không phải sự cố.

### 🔴 Tin "dòng bị xóa" — HOÃN *(chốt 19/09)*

Đợt này **không** gửi tin xóa cho đối tác. Lý do: chưa thỏa thuận được field nào mang dấu hiệu đó —
gửi khi họ chưa biết thì field rơi mất lúc ánh xạ, đối tác nhận về bản ghi **trông y như bình
thường**, tệ hơn không gửi. **Hệ quả chấp nhận**: đối tác giữ nguyên dòng đã bị gỡ — đúng bằng hiện
trạng hôm nay. Vì vậy **V5 lọc ở cả hai nhánh**, và **không** dựng alias `__operation`.

---

## 6. Khuôn SQL và bảo đảm giao hàng

Mỗi `QueryPacketNNN` tự biết bảng/cột nguồn, nhưng phải trả hai alias nội bộ thống nhất — cả hai
**không bao giờ** xuất hiện trong JSON gửi đối tác:

```text
__watermark = COALESCE(t.UpdateTime, t.CreateTime, <domainTime>)   ← mốc CSDL đổi dòng
__rowid     = khoá ổn định để phân trang khi trùng thời gian
```

**Query JOIN nhiều bảng** — `__watermark` là mốc lớn nhất của mọi nguồn đóng góp vào dòng:

```sql
CROSS APPLY
(
    SELECT MAX(sourceTimes.ChangeTime) AS ChangeTime
    FROM
    (
        VALUES
            (COALESCE(zs.UpdateTime, zs.CreateTime)),
            (COALESCE(z.UpdateTime, z.CreateTime)),
            (COALESCE(ts.UpdateTime, ts.CreateTime))
    ) sourceTimes(ChangeTime)
) changeInfo
```

### Chọn nhánh trong C#

Không viết `WHERE @onlyNew = 0 OR ...` nếu làm SQL Server bỏ index seek. Bốn ca, hai nhánh:

| Policy | Cờ tắt | Cờ bật |
|---|---|---|
| `FullOrChanged` (101·102·105·108) | query **all** | query **cursor** |
| `AlwaysIncremental` (103·104·107·109) | query **cursor** | query **cursor** |
| `NotReady` (110) | báo `NotReady`, không gửi | báo `NotReady`, không gửi |
| `Disabled` (106 · 111) | bỏ qua | bỏ qua |

Cả bốn ca đều đi qua **cùng một** vòng lặp phân trang; chỉ câu query bên trong là khác.

### Query cursor

```sql
SELECT ..., changeInfo.ChangeTime AS __watermark, main.ID AS __rowid
FROM ...
WHERE main.IsDelete IS NULL
  AND (
        changeInfo.ChangeTime > @lastTime
     OR (changeInfo.ChangeTime = @lastTime AND main.ID > @lastDataId)
  )
ORDER BY changeInfo.ChangeTime, main.ID
```

Cursor keyset (`>` kèm nhánh bằng-nhau-so-ID) là thứ gỡ **L1**. Dùng `>=` trơn thì `TOP N` đọc lại
mãi mốc cũ.

🔴 **`ORDER BY` bắt buộc ASC, không được bỏ.** `Extract` lấy max watermark bằng cách quét **từ cuối
danh sách lùi về đầu** rồi `break` ở dòng đầu tiên có đủ cặp `__watermark`/`__rowid` — nó **tin** rằng
dòng cuối là dòng lớn nhất. Bỏ `ORDER BY`, hoặc đổi sang `DESC`, thì cursor **nhảy hoặc lùi mà không
báo lỗi**. Xem mục *Hiện trạng* điều 4.

### Query all

`WHERE main.IsDelete IS NULL ORDER BY <business-key>`, phân trang keyset trên chính khoá nghiệp vụ đó.

🔴 **Page cursor của nhánh all chỉ sống trong bộ nhớ một lần chạy** — tuyệt đối **không** ghi vào
`LastDataId`. Nhánh all chụp lại toàn bộ mỗi lần chạy; ghi page cursor xuống DB sẽ khiến lần chạy sau
bắt đầu từ giữa ảnh chụp và **không bao giờ** chụp lại phần đầu.

🔴 **Nhưng nhánh all VẪN ghi `LastTimeRun` = `SELECT GETDATE()`.** Chốt có chủ ý, không phải kế thừa tình
cờ. Hôm nay mã làm đúng điều này một cách ngẫu nhiên: `DataOutboundService.cs:301` trả
`isSnapshot ? (MaxWatermark ?? exportedAt) : MaxWatermark`, mà `Extract` chỉ tính `MaxWatermark` khi
`!isSnapshot` ⇒ gói snapshot luôn rơi xuống `exportedAt`.

| Vì sao phải ghi | Vì sao phải là giờ CSDL |
|---|---|
| Đó là mốc *"đã giao hàng tới đây"*. Thiếu nó thì lúc bật cờ trở lại, cursor không có điểm bắt đầu đúng — mất đúng khoảng từ ảnh chụp cuối tới lúc bật *(xem ca **bật → tắt → bật lại** ở §5)* | `LastTimeRun` sẽ được đem so với `COALESCE(UpdateTime, CreateTime)` của CSDL. Dùng `DateTime.Now` của Worker mà lệch giờ là mất dữ liệu hoặc gửi trùng |

Hệ quả: snapshot lớn hơn budget (20 trang × 50 = 1.000 dòng) **không thể hoàn tất** trong một lần
chạy, và phần dư bị bỏ chứ không nối tiếp.

🔴 **Hai gói `FullOrChanged` hiện VƯỢT budget** — không phải "đều nằm trong" như bản trước ghi:

| Gói | Số dòng query ra hôm nay | Số trang @50 | Trong budget 20 trang? |
|---|---|---|---|
| 101 | ≈ **449.000** *(JOIN `TmsTrafficStatistic` nhân dòng)* | ~8.980 | ❌ vượt ~450 lần |
| 105 | trọn `TollTransactionOut` — **chưa đo** | ? | ❌ chưa biết, phải đo trước khi bật |
| 102 | 1 (`CctvDevice`) | 1 | ✅ |
| 108 | 69 (`VmsCurrent`) | 2 | ✅ |

⇒ **101 và 105 phải sửa query trước**: 101 chọn latest/aggregate, 105 đo rồi quyết. Phân trang không
cứu được một gói ra 449.000 dòng. Vẫn phải **ghi warning khi snapshot không hoàn tất**.

Giá trị cursor **luôn** truyền parameter SqlSugar; không nối dữ liệu vào chuỗi SQL.

### Vòng lặp — mã giả

```text
claim lease:  NextTimeRun = leaseExpiry            // DataOutboundService.cs:120
policy = ResolvePolicy(packetCode)                 // §7, không đọc comment trong mã
if policy in (NotReady, Disabled):  báo và thoát

useCursor = policy == AlwaysIncremental || IsChangedRowsOnly(sub)
page      = 0
t0        = now

loop:
    rows = useCursor ? QueryCursorPage(lastTime, lastDataId, pageSize)
                     : QueryAllPage(memoryPageKey, pageSize)
    if rows.Count == 0:                 break            // hết dữ liệu
    if !Map(rows):                      break            // mapping lỗi → giữ cursor
    if !Post(rows):                     break            // HTTP != 200 → giữ cursor

    if useCursor:  CommitPage(rows.MaxWatermark, rows.MaxRowId)   // KHÔNG đụng NextTimeRun
    else:          memoryPageKey = rows.LastBusinessKey           // chỉ trong bộ nhớ

    try:   LogExportResult(...)        // SAU commit trang, try/catch RIÊNG
    catch: ghi warning, KHÔNG rethrow  // log ném không được chặn vòng lặp

    page++
    if page >= maxPagesPerRun:                      break   // phanh 1
    if now - t0 >= leaseSeconds * 0.5:              break   // phanh 2

finally:
    NextTimeRun = ComputeNextTimeRun(sub, now)     // nhả lease, đúng một lần
```

`Map` lỗi và `Post` lỗi đều **giữ cursor** và thoát — không tiến nửa bước.

### Bảo đảm giao hàng: at-least-once

HTTP 200 và commit cursor là **hai bước rời nhau**. Worker chết đúng giữa hai bước ⇒ tick sau gửi lại
trang đó. Đây là **at-least-once**, và không có cách nào biến nó thành exactly-once khi transport là
HTTP và cursor nằm ở DB khác phía. **Không hứa exactly-once trong tài liệu hay báo cáo.**

Commit theo **từng trang** (V11) thu hẹp cửa sổ trùng: mất lease giữa backfill 35 trang thì phần gửi
lại nhiều nhất là **1 trang = 50 dòng**, không phải cả 1.728 dòng.

### 🔴 `Idempotency-Key` — HOÃN *(chốt 20/09)*

Chưa thỏa thuận được với đối tác nên **đợt này không gửi** header này. Ghi thiết kế lại đây để lần sau
mở lại là làm được ngay:

```text
Idempotency-Key = subscriptionId + "|" + packetCode + "|" + maxWatermark + "|" + maxRowId
```

**Không** dùng `SerialNbr` làm khoá: nó tăng cả khi retry, nên hai lần gửi cùng một trang sẽ mang hai
khoá khác nhau — đúng thứ cần tránh.

**Hệ quả chấp nhận có ý thức**: đối tác **không có cách lọc bản trùng**. Kết hợp với Resume-backfill
(§5), một lần mất lease giữa backfill sẽ để lại tối đa 50 dòng trùng ở phía họ. Mở lại việc này khi
có kênh thỏa thuận.

---

## 7. Ma trận 11 gói

Comment `Snapshot`/`Incremental` trong mã **không phải** nguồn sự thật nghiệp vụ —
`PacketMetadataResolver.ResolveFilterMode` xếp 108 là Incremental trong khi query thật là full
snapshot. Thêm policy tường minh đặt cạnh `PacketMetadataResolver`, viện dẫn
`../doc/02-mapping-goi-tin-101-111.md`.

| Gói | Policy | Page size | Bảng chính / row ID | Nguồn `ChangeTime` | Trạng thái |
|---|---|---|---|---|---|
| 101 | FullOrChanged | 50 | `TmsZoneStatus` / `ZoneId` | ZoneStatus + Zone + bản thống kê được chọn | sửa chọn latest/aggregate trước, tránh nhân dòng · 299 dòng staging |
| 102 | FullOrChanged | 50 | `CctvDevice` / `ID` | CctvDevice + Equipment | join IP chỉ là tạm; code đang `NULL AS snapshot` · 1 dòng staging |
| 103 | AlwaysIncremental | 50 | `TmsTrafficData` / `ID` | audit time; `DetectTime` chỉ fallback | **L1 · L3** |
| 104 | AlwaysIncremental | 50 | `TmsWeather` / `ID` | audit time; `TimeDetect` chỉ fallback | **L3** · registry có alias `["104_rfidData"] = QueryPacket105` |
| 105 | FullOrChanged | 50 | giao dịch / `ID` | Transaction In/Out + VehicleRegistration | code đang thiếu nguồn In |
| 106 | 🔴 **Disabled** | — | — | — | **nguồn KHÔNG tồn tại** *(đo 20/09, xem mục HIỆN TRẠNG)*: 4/11 trường tải trọng không có cột, 3 trạm cân khai rồi nhưng 0 dòng dữ liệu, không bảng nào trong CSDL chứa tải trọng. ⇒ **gỡ `QueryPacket106` khỏi registry** (V13), không sửa cursor cho nó |
| 107 | AlwaysIncremental | 50 | `TmsIncident` / `ID` | `COALESCE(i.UpdateTime, i.CreateTime, i.StartDate)` | **L4 · L5** · 1.728 dòng staging |
| 108 | FullOrChanged | 50 | VMS / `EquipmentId` | VmsCurrent + Equipment | staging **69 dòng / 61 `EquipmentId`** ⇒ chọn dòng mới nhất mỗi thiết bị trước |
| 109 | AlwaysIncremental | 50 | giao dịch / `ID` | Transaction In/Out + Lane + Station | **L1 · L3** · thiếu In, `tollPrice = NULL` |
| 110 | 🔴 **NotReady** | — | — | — | **Hai lý do độc lập:** ① chưa có `messageId` bền vững · ② **L7** — bộ lọc `State` hỏng, 97% sự cố đã đóng vẫn lọt. ⇒ dừng, không mở rộng query hiện tại. Khi làm thật phải theo khuôn `TrafficInfoService.cs:36-40`: lọc bằng **giá trị enum số** `TmsIncidentStateEnum` (loại `0` Skipped · `1` Registration · `4` Finished · `5` Canceled), **không** so chuỗi chữ |
| 111 | Disabled | — | — | — | tài liệu ghi `skip` |

**4 `FullOrChanged` + 4 `AlwaysIncremental` + 1 `NotReady` (110) + 2 `Disabled` (106 · 111) = 11.**

Hai gói `Disabled` khác nhau về lý do, ghi rõ để đừng gộp: **111** vì tài liệu ghi `skip`; **106** vì
**nguồn dữ liệu không tồn tại**. Ngày nào có bảng WIM thật thì 106 quay lại thành `NotReady` rồi mới
`AlwaysIncremental` — không tự bật lại bằng query cũ.

Gói 107 phải đổi `ISNULL(i.UpdateTime, i.StartDate)` → `COALESCE(i.UpdateTime, i.CreateTime,
i.StartDate)` ở **cả ba chỗ** `:391` `:395` `:396`. Không còn chỗ nào "giữ khuôn `ISNULL` sẵn có".

Handler nào chưa đủ điều kiện phải báo rõ `NotReady`, **không gửi payload sai**.

---

## 8. Kiểm thử

Tệp: `tests/ShareData/Services/DataOutboundServiceTests.cs` (hoặc lớp test DataOutbound hiện hữu;
không tạo suite trùng).

| Ca | Kỳ vọng |
|---|---|
| cờ bật, query 0 dòng | không gọi sender; `Success + NoNewData`; cursor giữ nguyên |
| cờ bật, có insert · có update | gửi; thành công mới tiến cặp cursor |
| 🔴 dòng vừa INSERT, `UpdateTime = NULL` *(§4)* | **vẫn bắt được** bằng `CreateTime` |
| 🔴 gói 107 dòng mới *(L4)* | bắt bằng `CreateTime`, **không** rơi xuống `StartDate` |
| 🔴 dòng bị xóa mềm *(V5)* | **không gửi** — cả nhánh all lẫn nhánh cursor |
| 🔴 100 dòng cùng timestamp, page size 50 *(L1)* | trang 1 lấy 1–50; trang 2 lấy 51–100, **không lặp lại** |
| insert trễ nhưng domain time cũ *(L3)* | vẫn bắt bằng `CreateTime` |
| HTTP lỗi | không tiến cả hai cursor |
| cờ tắt + AlwaysIncremental | vẫn lấy sau cursor, không dump lịch sử |
| 🔴 `daily` + cờ bật *(§1)* | `NextTimeRun` = đúng giờ hẹn kế tiếp; **không** poll 5s; cờ không đổi `NextTimeRun` |
| 🔴 `continuous` + cờ tắt *(V11)* | phân trang tới hết, **không** kẹt ở 50 dòng đầu |
| 🔴 300 dòng tồn, budget 20 trang *(V11)* | một lần chạy tiêu hết 300 dòng |
| 🔴 1.100 dòng tồn, budget 20 trang *(V11)* | tick 1 lấy 1.000 và nhả lease; tick 2 lấy phần dư, **không lặp lại** |
| 🔴 HTTP lỗi ở trang 4 *(V11)* | 3 trang đầu đã commit; cursor nằm ở cuối trang 3; `NextTimeRun` tiến để retry |
| 🔴 cursor gửi trong request body API *(V9)* | bị **bỏ qua**; entity lưu cursor do Worker đặt, không dump lịch sử |
| 🔴 Resume sau Pause *(R17 · §5)* | cursor **giữ nguyên**; backfill đủ phần tồn đọng |
| 🔴 bật cờ khi `LastTimeRun == null` *(V10)* | cursor = giờ CSDL lúc bật; **không** backfill lịch sử |
| 🔴 **bật → tắt → bật lại** *(V10)* | ảnh chụp cuối `09:01:00`, dòng đổi `09:01:20`, bật cờ `09:01:40` ⇒ dòng đó **phải được gửi**. `LastTimeRun` **giữ nguyên** `09:01:00`, **không** dựng lại bằng `GETDATE()` |
| 🔴 nhánh all chạy xong *(§6)* | `LastTimeRun` **được ghi** = giờ CSDL · `LastDataId` **không** bị chạm |
| 🔴 nhánh all hết budget giữa snapshot *(§6)* | page cursor **không** ghi vào `LastDataId`; lần chạy sau chụp lại từ đầu; có warning |
| gói 110 | `NotReady` rõ ràng · **không** chạy query incident thay thế |
| 🔴 gói 106 · 111 *(V13)* | `Disabled` rõ ràng · **không** gọi được handler nào · registry **không còn** `QueryPacket106` |
| 🔴 `LogExportResult` ném sau khi POST 200 *(§5)* | cursor **vẫn tiến**; chỉ mất dòng nhật ký; **không** gửi lại lô đó ở tick sau |
| 🔴 `LastTimeRun == null` + cờ bật *(L6 · V10)* | **không** quét từ 1900; báo lỗi cấu hình và dừng subscription |
| 🔴 query cursor bị bỏ `ORDER BY` *(§6)* | test phải bắt được cursor lấy sai dòng — chứng minh `ORDER BY ASC` là ràng buộc, không phải trang trí |
| 🔴 gói 101 sau khi chọn latest/aggregate *(L5)* | ra **299 dòng**, không phải ~449.000 |

```text
dotnet build src/Services/ShareData/ShareDataWorker/ShareDataWorker.csproj
dotnet build src/TAC_WebAPI/TAC_WebAPI.csproj
dotnet test tests/test.csproj --filter "FullyQualifiedName~Services.DataOutbound"
```

Phải build **cả hai** project: V9 · V10 sửa `ShareDataSubscriptionCommandHandler` thuộc WebAPI, V1
sửa entity mà **cả hai** phía cùng tham chiếu.

Trước `dotnet test`, bắt buộc kiểm connection string test chỉ trỏ local (`localhost`, `127.0.0.1`,
`(localdb)`, `.`). Thấy IP xa như `10.10.8.30` thì **huỷ test và báo**; không chạy.

---

## 9. Không làm

- Không NATS · CDC · trigger SQL · physical-delete tracking · `LastPayloadHash`.
- Không thêm `NextDataCheckTime` hay bất kỳ đồng hồ thứ hai nào — dùng lại `NextTimeRun`.
- Không dùng `Mode = Event` làm cờ nữa — đã có `SendOnNewData`. `null` và `false` **cùng nghĩa**.
- 🔴 Không để cờ tham gia tính `NextTimeRun` dưới bất kỳ hình thức nào (§1).
- 🔴 Không hứa **exactly-once** ở tài liệu, log hay báo cáo — cơ chế là at-least-once (§6).
- 🔴 Không đổi `ColumnDescription` của `SendOnNewData` (`ShareDataSubscription.cs:65`) — code Hiếu
  vừa sửa tay, rule 5.
- 🔴 Không reset cursor khi **Resume** — Resume phải backfill (§5). Chỉ khởi tạo cursor ở đúng hai
  chỗ của V10.
- 🔴 Không ghi page cursor của nhánh all xuống `LastDataId` (§6).
- 🔴 Không để `LogExportResult` — hay bất kỳ câu ghi nhật ký nào — chắn được đường tiến cursor (§5).
- 🔴 Không dùng giá trị mồi `1900-01-01` cho cursor (L6); thiếu cursor thì **dừng**, không dump lịch sử.
- 🔴 Không bỏ `ORDER BY` khỏi query cursor để "tối ưu" — `Extract` dựa vào thứ tự ASC để lấy max (§6).
- 🔴 Không bật gói 101 và 105 trước khi sửa khối lượng query (L5) — phân trang không cứu được 449.000 dòng.
- 🔴 Không đẻ cờ cho **chiều NHẬN** — bên nhận có dữ liệu mới là do đối tác đẩy sang, mình không dò
  (việc **SV-13** đã bỏ khỏi kế hoạch).
- 🔴 Không dùng `UpdateTime` **đứng trần** làm `__watermark` (§4); không tự chế contract xóa mềm (§5).
- Không soạn/chạy DDL thủ công và **không sinh tệp `.sql`** cho `LastDataId` — CodeFirst, rule 7.
- Không tự làm gói 110/111 bằng dữ liệu gần đúng; không coi comment `SNAPSHOT` là bằng chứng nghiệp vụ.
- 🔴 Không sửa, không giữ, không "tạm dùng" `QueryPacket106` — **gỡ hẳn** (V13). Gói 106 chỉ mở lại khi
  CSDL có bảng WIM thật; đừng lấy `TmsTrafficData` làm nguồn cân xe lần nữa.

## Tự kiểm trước khi báo xong

| Kiểm | Kỳ vọng |
|---|---|
| `grep -rn "SendOnNewData" src/` | entity `:65` + Add `:98` + Update `:152` + **đúng 1** helper ở Worker. Ngoài bốn chỗ đó, **không** nơi nào đọc trực tiếp property *(V8)* |
| `grep -rn "LastDataId" src/` | property trong entity + `@lastDataId` trong extraction + chỗ persist. **Không** có tệp `.sql` nào *(V1 · rule 7)* |
| `grep -rn ">= @lastTime" src/` | **0** — đối chiếu đúng 5 chỗ `:267` `:301` `:357` `:395` `:459` đã thay bằng cursor kép *(L1)* |
| `grep -rn "ISNULL(i.UpdateTime, i.StartDate)" src/` | **0** *(L4)* |
| Soi **từng** câu query — all lẫn cursor | **mọi** câu có `IsDelete IS NULL` trong `WHERE` *(L2)* |
| Soi từng `__watermark` mới dựng | **không có** `UpdateTime` đứng trần — phải trong `COALESCE` *(§4)* |
| `grep -c "@lastDataId" .../DataOutboundExtractionProcess.cs` | **> 0** (trước: 0) *(T1)* |
| `grep -rnE "\blastId\b\|@lastId\b\|MaxLastId\b\|\.LastId\b" src/` | **0** — đã gộp hết về **một** tên `LastDataId`/`lastDataId` *(V2)* |
| `grep -rn "EventPollIntervalSeconds" src/` | **0** *(T4)* |
| `grep -rn "__operation" src/` | **0** — tin xóa đã hoãn *(§5)* |
| `grep -rn "QueryPacket106" src/` | **0** — đã gỡ cả hàm lẫn đăng ký trong registry *(V13)* |
| `grep -rn "'FINISHED'" src/` | **0** — bộ lọc `State` bằng chuỗi chữ của gói 110 không còn. Nếu 110 vẫn `NotReady` thì cả query cũng không còn *(L7)* |
| `grep -rn "Idempotency-Key" src/` | **0** — đã hoãn *(§6)* |
| Soi nơi ghi cursor | commit trang **không** chứa `NextTimeRun`; `NextTimeRun` chỉ được set ở `finally` *(V11)* |
| Soi Add · Update handler | `LastTimeRun` · `LastDataId` **không** nhận giá trị từ command *(V9)* |
| Soi `ChangeStateAsync` | Resume **không** chạm cursor *(V10)* |
| Soi vị trí `LogExportResult` | nằm **sau** commit trang và có `try/catch` **riêng**; không còn chắn đường trả cursor *(§5)* |
| Soi **từng** query cursor | có `ORDER BY <watermark>, <rowid>` **ASC**; không câu nào thiếu hoặc dùng `DESC` *(§6)* |
| `grep -rn "SqlDateTimeFloor" src/` | **0** — đã bỏ giá trị mồi 1900 *(L6)* |
| Soi query gói 101 | có chọn latest/aggregate cho `TmsTrafficStatistic`; đếm dòng trả về ≈ 299, **không** ~449.000 *(L5)* |
| Đo số dòng `TollTransactionOut` | có con số thật trước khi bật gói 105 *(L5)* |

## Báo cáo khi xong

1. **L1** — xác nhận không còn `>= @lastTime` đứng một mình; kết quả ca *100 dòng cùng timestamp* và
   ca *1.100 dòng / budget 20 trang*.
2. **L2** — số dòng `IsDelete IS NOT NULL` ở các bảng nguồn = lượng dữ liệu đối tác **thôi nhận** ngay
   lần chạy đầu. Con số lớn bất thường thì báo để vận hành khỏi tưởng hệ thống chết.
3. **L3 · L4 · §4** — liệt kê biểu thức `__watermark` của **từng** gói trong 8 gói active, chứng minh
   không gói nào dùng `UpdateTime` trần và gói 107 đã có `CreateTime`.
4. **L5 · V11 · V12** — page size và budget thực tế đang đọc từ khoá cấu hình nào; log "hết budget"
   trông ra sao.
5. **T1 · V1** — `LastDataId` đã lên DB qua CodeFirst ở môi trường nào; xác nhận **không** có tệp
   `.sql` nào được sinh và **đã báo Hiếu**.
6. **V9 · V10** — kết quả ca cursor-trong-request-body và ca Resume-backfill.
7. Policy thực tế của từng gói 101–111 (**4 + 4 + 1 + 2**), handler nào còn `NotReady`; số test trước/sau.
   Xác nhận riêng: **`QueryPacket106` đã gỡ hẳn** (V13) và gói 106 báo `Disabled`, không phải `NotReady`.
8. 🔴 Nhắc lại hai mục còn HOÃN để lần sau mở lại: **tin dòng bị xóa** (§5) và **`Idempotency-Key`**
   (§6), kèm hệ quả đang chấp nhận.
