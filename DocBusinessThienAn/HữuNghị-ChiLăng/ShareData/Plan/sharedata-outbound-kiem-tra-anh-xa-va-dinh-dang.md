# Luồng GỬI ShareData — luồng chuẩn, nguồn cấu hình, và đang kẹt ở đâu

> **Tài liệu sống.** Cập nhật lần cuối: **17/09/2026**.
> Chỉ nói về **luồng xử lý**. Việc lặt vặt tách sang `../Prompt/`.
> Đọc từ trên xuống một lượt là hiểu trọn luồng — không cần mở tệp nào khác.

---

# 0. LUỒNG CHUẨN — đi từ đầu tới cuối

Dấu đứng trước mô tả trạng thái của **chính việc ghi trên dòng đó**:
🟥 chưa có trong mã · 🟧 có nhưng chạy sai · ✅ có và chạy đúng

```
┌─ A · CHỌN VIỆC ────────────────────────────────────────────────────────┐
│  worker thức mỗi 5 giây                                                │
│    → quét đăng ký đến hạn   (chiều Gửi · đối tác đang bật & kết nối)   │
│    → nhận lease             (đẩy NextTimeRun ra xa, tối thiểu 5 phút)  │
│    → tra GÓI TIN                                                       │
│    → tra PHỄU LỌC           ✅ có truy vấn                              │
│       └ không tìm thấy      ✅ NGẮT, ghi ESH-1304, không gửi            │
│    → dựng ngữ cảnh          (mang theo suốt 3 bước)                    │
└───────────────────────────────────┬────────────────────────────────────┘
                                    ▼
┌─ B · BƯỚC 1 — LẤY DỮ LIỆU ─────────────────────────────────────────────┐
│  mã gói → tra ra MỘT HÀM C# → chạy CÂU SQL CỨNG nằm trong hàm đó       │
│                                                                        │
│  RA:  danh sách dòng thô                                               │
│       khoá mỗi dòng  ==  bí danh sau AS  ← mấu chốt của cả luồng       │
│                                                                        │
│  không dòng nào → DỪNG, không gửi                                      │
└───────────────────────────────────┬────────────────────────────────────┘
                                    ▼
┌─ C · BƯỚC 2 — ÁNH XẠ ── nguồn: ShareDataMapping.TargetShapeJson ───────┐
│  ✅ bộ khung rỗng / hỏng cú pháp → NGẮT, ghi ESH-1206                   │
│  ✅ đối chiếu: tên bộ khung gọi mà dòng thô không có → ghi ESH-1205     │
│  🟥 phép gộp cấp tập dòng  (đếm · tổng · trung bình · min · max)        │
│                                                                        │
│      với TỪNG TRƯỜNG, đúng thứ tự:                                     │
│                                                                        │
│         giá trị thô                                                    │
│             │                                                          │
│             ├─ ✅  rỗng → lấy GIÁ TRỊ MẶC ĐỊNH   $extend.defaultValue   │
│             ├─ ✅  BIỂU THỨC                     $extend.expression     │
│             ├─ ✅  BỘ MÃ quy đổi                 $extend.codeSet        │
│             ├─ ✅  ÉP KIỂU                       $extend.targetType     │
│             └─ ✅  ĐỊNH DẠNG đầu ra              $extend.format         │
│                                                                        │
│  trường BẮT BUỘC mà rỗng → NGẮT CẢ LÔ  ✅ $extend.required              │
│  đóng gói JSON  ✅ không phong bì · không mã băm                        │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │  thất bại → DỪNG, KHÔNG sang D
                                    ▼
┌─ D · BƯỚC 3 — XUẤT BẢN ────────────────────────────────────────────────┐
│  ghi tệp xuống đĩa      bắt buộc   · lỗi → ghi cảnh báo rồi dừng       │
│  gửi HTTP tới đối tác   tuỳ chọn   · lỗi → CHỈ cảnh báo, vẫn tính OK   │
│  ghi nhật ký kết quả  +  ghi lại mốc cho lần chạy sau                  │
└────────────────────────────────────────────────────────────────────────┘
```

## Giai đoạn A — Chọn việc để làm

| # | Làm gì | Chi tiết |
|---|---|---|
| A1 | Worker thức dậy | mỗi **5 giây** một lần |
| A2 | Quét đăng ký đến hạn | `Direction = Gửi`, `State = Active`, đối tác đang bật và đang kết nối, `NextTimeRun` đã tới |
| A3 | Không có đăng ký nào | ghi nhật ký rồi ngủ tiếp |
| A4 | **Nhận lease** từng đăng ký | đẩy `NextTimeRun` ra xa (tối thiểu 5 phút) bằng cập nhật có điều kiện, để worker khác không giành mất |
| A5 | Tra **gói tin** | theo `DatatypeId` của đăng ký → `ShareDataPacket` · không thấy → ghi lỗi, dừng |
| A6 | Tra **phễu lọc** | theo đối tác + mã gói + chiều Gửi → `ShareDataMapping` · ✅ không thấy thì **ngắt, ghi `ESH-1304`, không gửi** |
| A7 | Dựng ngữ cảnh | gói tin + đối tác + đăng ký + phễu lọc, mang theo suốt ba bước |

## Giai đoạn B — BƯỚC 1: Lấy dữ liệu

| # | Làm gì | Chi tiết |
|---|---|---|
| B1 | Tra bảng đăng ký truy vấn | theo mã gói → ra **một hàm C#** · không có → ném lỗi |
| B2 | **Chạy câu SQL cứng trong hàm đó** | ✅ câu lệnh nằm trong mã, **không** trong CSDL |
| B3 | Nhận danh sách dòng thô | **khoá mỗi dòng = bí danh sau `AS`** của câu SQL |
| B4 | Không có dòng nào | ghi nhật ký "không có dữ liệu mới", **dừng, không gửi** |
| B5 | Dò mốc gia tăng | với gói kiểu tăng dần, đọc `__watermark` và `__rowid` để lần sau lấy tiếp |

**Ra khỏi bước 1:** danh sách dòng thô + mốc gia tăng. Không đụng gì tới cấu hình ánh xạ.

## Giai đoạn C — BƯỚC 2: Ánh xạ

Nguồn cấu hình: **`ShareDataMapping.TargetShapeJson`** — xem mục 1.

| # | Làm gì | Chi tiết |
|---|---|---|
| C1 | ✅ **Chặn bộ khung hỏng** | rỗng hoặc sai cú pháp JSON → ghi `ESH-1206`, huỷ kết xuất |
| C2 | ✅ **Đối chiếu tên** | xem giải thích ngay dưới bảng |
| C3 | Nạp bộ mã | gom mã bộ khai trong bộ khung → đọc `ShareDataCodeSet` |
| C4 | 🟥 **Phép gộp cấp tập dòng** | đếm, tổng, trung bình, nhỏ nhất, lớn nhất — chạy **trước** khi vào từng trường · **khoan làm**, xem mục 2.3 |
| C5 | Đi theo cây bộ khung | khoá nào có `$field` thì lấy dữ liệu, khoá nào là hằng số thì giữ nguyên, `$exclude` thì bỏ khỏi bản tin |

### C2 — "đối chiếu tên" là đối chiếu cái gì với cái gì

Có **hai danh sách tên trường**, sinh ra từ hai chỗ khác nhau:

| Danh sách | Từ đâu ra | Gói 101 có |
|---|---|---|
| **Khoá dòng thô** | bí danh sau `AS` của câu SQL, tức thứ CSDL **thật sự trả về** | **13** tên |
| **Tên bộ khung gọi** | mọi giá trị `$field` trong `TargetShapeJson`, tức thứ phễu lọc **đòi hỏi** | **16** tên (17 lượt — `dataTime` buộc 2 lần) |

Phép kiểm chỉ là: **lấy danh sách thứ hai, dò xem từng tên có nằm trong danh sách thứ nhất không.**

Hai kiểu lệch, **chỉ một kiểu là vấn đề**:

| Dòng thô có | Bộ khung gọi | Kết quả | Cảnh báo |
|---|---|---|---|
| `A · B · C · D` | `A · B · C` | chỉ `A·B·C` — **`D` bỏ hoàn toàn** | 🔇 **không** |
| `A · B` | `A · B · C` | `A·B` ánh xạ · **`"c": null`** | 🔔 **`ESH-1205`** |

Chỉ **một chiều** đáng báo: *bộ khung đòi thứ dòng thô không có*. Dòng thô thừa cột là chuyện của câu
truy vấn, không phải lỗi cấu hình.

Cảnh báo được sinh ra từ sự lệch pha giữa hai tập trên. Sự lệch này là bản chất của dữ liệu từ câu
truy vấn, không phải lỗi cấu hình.

**Gói 101 hôm nay:**

| | Số | Tên |
|---|---|---|
| Bộ khung gọi · dòng thô **không có** | **6** | `ESH-1205` báo đúng 6 tên này, 1 lần/5 phút |
| Dòng thô có · bộ khung **không gọi** | **3** | `vehicleCount` · `fromLocationMet` · `averageSpeed` → bỏ qua im lặng |

> 💡 Chốt: "truy vấn có gì lấy đó", không khai bí danh giữ chỗ.

Với **từng trường**, đúng thứ tự này:

| # | Bước | Khoá `$extend` | Trạng thái |
|---|---|---|---|
| C6 | Lấy giá trị thô theo tên | `$field` | ✅ |
| C7 | Rỗng thì lấy **giá trị mặc định** | `defaultValue` | ✅ |
| C8 | **Biểu thức** | `expression` | ✅ |
| C9 | **Bộ mã quy đổi** | `codeSet` | ✅ |
| C10 | **Ép kiểu** | `targetType` | ✅ |
| C11 | **Định dạng đầu ra** | `format` | ✅ · không khai → ISO-8601 |
| C12 | Trường bắt buộc mà rỗng | `required` | ✅ **ngắt cả lô**, ghi `ESH-1202` |
| C13 | Đóng gói JSON | — | ✅ **không phong bì**, không mã băm |

**Ra khỏi bước 2:** mảng byte nội dung cuối, hoặc cờ thất bại kèm lý do.
**Thất bại thì KHÔNG sang bước 3.**

## Giai đoạn D — BƯỚC 3: Xuất bản

| # | Làm gì | Chi tiết |
|---|---|---|
| D1 | **Ghi tệp** xuống đĩa | bắt buộc · đường dẫn `Out/{đối tác}/{yyyyMM}/{ddHH}/{gói}/{tên}.json` · lỗi → ghi cảnh báo, dừng |
| D2 | **Gửi HTTP** | chỉ khi đối tác có khai địa chỉ · lỗi **chỉ cảnh báo**, tệp đã ghi vẫn tính thành công |
| D3 | Ghi nhật ký kết quả | số dòng, kích thước, đường dẫn tệp, mã phễu lọc |
| D4 | Ghi lại mốc | cập nhật `LastTimeRun`, tăng số thứ tự, tính `NextTimeRun` cho lần sau |

## Ví dụ đi trọn một trường

Trường `trafficCondition`, gói 101, theo cấu hình thật:

| Bước | Giá trị |
|---|---|
| B3 — dòng thô | cột `zs.Condition` → `"normal"` |
| C6 — bộ khung gọi `{"$field":"trafficCondition"}` | `"normal"` |
| C7 — mặc định `$extend.defaultValue = "Default"` | có giá trị rồi → giữ `"normal"` |
| C8 — biểu thức | không khai → giữ `"normal"` |
| C9 — bộ mã `$extend.codeSet = "condition"` | `"normal"` → `"0"` |
| C10 — ép kiểu `$extend.targetType = "string"` | `"0"` đã là chuỗi → giữ nguyên |
| C11 — định dạng | không khai → giữ nguyên |
| D1 — ghi tệp | nằm trong `data[].trafficInfo[].trafficCondition` |

> ⚠️ Dữ liệu thật có **5 dòng `conges`** không nằm trong bộ mã, và bộ mã **không có bản ghi mặc định**
> ⇒ 5 dòng đó nhả nguyên `"conges"` kèm cảnh báo. Xem **P2** mục 3.

---

# 1. Nguồn cấu hình

## 1.1 Đã chốt: worker đọc DUY NHẤT `ShareDataMapping.TargetShapeJson`

Mọi cấu hình cấp trường nằm trong **`$extend`**, bên trong cột `TargetShapeJson` của bảng
`ShareDataMapping`.

### Hai bảng, đừng lẫn

| | `ShareDataMapping` | `ShareDataPacketField` |
|---|---|---|
| Chứa `TargetShapeJson` (và `$extend` bên trong) | ✅ **có** | ❌ không |
| Khoá | `PartnerId` + `DatatypeId` + `Direction` | `DatatypeId` |
| Phạm vi | **riêng từng đối tác, từng chiều** | **dùng chung mọi đối tác** |
| Cột chính | `TargetShapeJson` · `PartnerSchemaJson` · `Direction` · `IsActive` · `Format` | `AliasFieldKey` · `Type` · `IsRequired` · `GroupName` · `Status` |
| Worker đọc lúc chạy? | ✅ **duy nhất nguồn này** | ❌ **không** |

`ShareDataPacketField` là **danh mục lúc thiết kế**, phục vụ giao diện CRUD gói tin/cột. Giao diện lấy
danh mục từ đó rồi **sinh ra** `TargetShapeJson`. Worker chỉ đọc kết quả đã sinh — đúng nguyên tắc
*"worker không CRUD, chỉ lấy ra xử lý"*.

### Hệ quả: `$extend` đổi PHẠM VI cấu hình

| | Cũ — `FieldsJson` | Mới — `$extend` trong `ShareDataMapping` |
|---|---|---|
| Khai một lần, áp cho ai | **mọi đối tác** | **riêng từng đối tác** |
| Thêm đối tác mới | thừa hưởng sẵn | **phải khai lại từ đầu** |
| Khai sót | không xảy ra được | sót ở phễu lọc nào thì **chỉ đối tác đó** mất ép kiểu / mất lớp chặn |

Hiện chỉ có **một phễu lọc chiều Gửi đang sống** nên chưa lộ. Thêm đối tác thứ hai là thấy ngay.

## 1.2 Bảng khoá `$extend` — đối chiếu `FieldsJson` cũ

| `FieldsJson` (đã bỏ) | `TargetShapeJson` | Worker đọc? |
|---|---|---|
| `fieldKey` | `$field` | ✅ |
| `dataType` | `$extend.targetType` | ✅ |
| `codeSet` | `$extend.codeSet` | ✅ |
| `expression` | `$extend.expression` | ✅ |
| *(mới)* | `$extend.defaultValue` | ✅ |
| `isRequired` | `$extend.required` | ✅ |
| *(mới)* | `$extend.format` | ✅ |
| `internalOnly` | `$exclude: true` | ✅ |
| `noSource` | *bỏ hẳn* — **không dựng lại dưới dạng nào** | — |
| `orderNo` · `columnName` · `unit` · `isKey` | *không dựng lại ở chiều GỬI* | — |

> Trong mã còn vế `fieldMeta?.Required` và `fieldMeta?.DataType`. Đó là **tàn dư luôn `null`**
> (xem **P3** mục 3), giữ lại chỉ để không đổi hành vi lúc gỡ — **không** phải nguồn cấu hình song song.

## 1.3 Nội dung gửi đi

| | Cách cũ | Cách mới |
|---|---|---|
| Vỏ ngoài | phong bì PDU 9 khoá (`pduType`, `sender`, `destination`, `timestamp`, `hash`, `payload`…) | **bỏ hết** — nội dung = y hệt kết quả ánh xạ |
| Mã băm | có tính | **bỏ** |
| Định dạng | JSON + XML | **chỉ JSON** |
| Rẽ nhánh theo phiên bản gói | không có | vẫn không có (đúng chốt) |

---

# 2. Biến đổi một trường

## 2.1 Thứ tự năm bước

| | Thứ tự |
|---|---|
| Cách cũ (trước 17/09/2026) | biểu thức → bộ mã → rỗng lấy mặc định → ép kiểu |
| **Hiện tại** (đã chốt theo wireframe) | **rỗng lấy mặc định → biểu thức → bộ mã → ép kiểu → định dạng** ✅ |

**Vì sao theo wireframe:** đây là bản duy nhất giải thích được lý do — *"thứ tự này phải giữ nguyên
vì chiều nhận đảo lại đúng bốn bước"*. Lệch thứ tự thì chiều nhận **không khôi phục được giá trị gốc**.

> 💡 **Đảo vị trí thôi là chưa đủ.** Bộ tính biểu thức đọc giá trị **từ dòng dữ liệu**, không đọc từ
> biến trung gian. Bản sửa 17/09 dựng **một bản sao dòng dữ liệu** và ghi đè khoá của trường đang xử
> lý bằng giá trị hiện tại (đã qua bước mặc định), nhờ vậy biểu thức nhìn thấy đúng giá trị.
>
> Test `Transform_WhenRawValueEmpty_AppliesDefaultValueBeforeExpression_Test` chốt hành vi này: giá
> trị thô rỗng, mặc định `"50"`, biểu thức `speed + 10` → kết quả `60`.

## 2.2 Trường thiếu ra `null` — đã chốt, KHÔNG đổi thành `""` / `0`

Khoá vẫn **có mặt** trong JSON khi giá trị `null` — `PayloadJsonOptions` chỉ đặt
`PropertyNamingPolicy = CamelCase`, **không** có `DefaultIgnoreCondition`. Đối tác kiểm *"có khoá
không"* thì vẫn qua.

### Đã cân nhắc và bác bỏ: đổi `null` thành giá trị rỗng theo kiểu

| Trường | `0` nghĩa là gì | `null` nghĩa là gì |
|---|---|---|
| `laneCount` | đường có **không làn** | chưa biết |
| `averageSpeed` | xe **đứng yên, đang tắc** | chưa đo được |
| `shoulderWidth` | **không có** vai đường | chưa khảo sát |

`0` và `""` là **giá trị thật** trong ITS. Đổi *"không có dữ liệu"* thành `0` là nói với đối tác một
điều sai, mà sai im lặng — riêng `averageSpeed = 0` còn là tín hiệu tắc đường.

**Muốn giá trị cụ thể thì khai tường minh `$extend.defaultValue`**, theo từng trường từng đối tác:

```jsonc
"routeName": {
  "$field": "routeName",
  "$extend": { "targetType": "string", "defaultValue": "" }
}
```

> ⚠️ **Đã có sẵn một đường ngầm biến `null` thành giá trị cụ thể**, qua bộ mã chứ không qua kiểu:
> `MapCode` — nếu bộ mã có bản ghi `isDefault = true` thì giá trị `null` **cũng bị thay** bằng giá trị
> mặc định của đối tác. Bộ `condition` không có `isDefault` nên chưa dính; bộ `pave` thì **có**
> (`2 → "off"`). Cần nhớ khi đọc kết quả.

## 2.3 Phép gộp cấp tập dòng — C3, **khoan làm**

Năm phép trên **toàn tập dòng thô**: **đếm · tổng · trung bình · nhỏ nhất · lớn nhất**. Vị trí trong
luồng: **chạy TRƯỚC khi vào xử lý từng trường**, kết quả thành giá trị dùng chung cho mọi dòng (hoặc
cho khối `header`).

**Vì sao nặng** — đo trên mã hiện tại:

| Vướng | Chi tiết |
|---|---|
| Bộ tính biểu thức nhận **một dòng** | `TryEvaluate(expression, rowDict, ...)` — muốn gộp thì phải nhận **tập dòng**, đổi chữ ký cả bước |
| `RenderShapeNode` cũng theo **một dòng** | chữ ký có `rowDict`, không có đường nhìn thấy các dòng khác |
| Đã có sẵn một nửa | `RenderShapeAggregate` **nhận `allRowDicts`** — chỗ bám hợp lý nhất, hiện chỉ dùng cho khối `$each`/`$as` |
| Chưa có cú pháp | `$extend` chưa có khoá nào cho phép gộp; cần chốt tên (vd `$agg: "sum"`) |

**Hướng gợi ý khi làm**: bám vào `RenderShapeAggregate` đã có `allRowDicts`, tính sẵn bảng giá trị gộp
**một lần trước vòng lặp dòng**, rồi bơm vào `rowDict` như biến hệ thống ⇒ **không phải đổi chữ ký**
`RenderShapeNode` hay bộ tính biểu thức.

**Chưa chốt**: tên khoá cú pháp, và phạm vi gộp (toàn lô hay theo nhóm `GroupName`).

---

# 3. Đang kẹt ở đâu

## 3.1 Bốn phát hiện đo ngày 17/09 🟥

Đo trên `mssql_dev` = `DEV_ITS10` (`localhost:14333`), theo **bộ khung đối tác mới**.
**Không sửa cái nào trong đợt 17/09** — đây là lỗi dữ liệu/cấu hình hoặc nợ kỹ thuật.

| | Nội dung | Bằng chứng |
|---|---|---|
| **P3** | `LoadPacketFields(db, packet.Code)` truyền `"101_commonData"`, lọc `DatatypeId == "101_commonData"` (dự phòng `"101"`). CSDL lưu `DatatypeId = "PKT101"` — là **ID** gói tin, **không** phải Code ⇒ **nạp về 0 dòng**, `fieldsDict` rỗng, **`fieldMeta` LUÔN `null`** | truy vấn trực tiếp `WHERE DatatypeId IN ('101_commonData','101')` → **`[]`** |
| **P1** | `fromLocationMet: 0` và `averageSpeed: 45.5` trong bộ khung là **số cứng**, không có `$field` ⇒ mọi dòng gửi hằng số, dữ liệu thật bị bỏ | `RenderShapeNode` `case JsonValueKind.Number` trả literal |
| **P2** | Bộ mã `condition` chỉ có `normal→"0"` · `slow→"1"`, **cả hai `isDefault: false`**. Dữ liệu thật có **`conges` 5 dòng** không khớp ⇒ nhả nguyên `"conges"` + cảnh báo mỗi chu kỳ | `TmsZoneStatus.Condition`: normal 126 · slow 21 · **conges 5** |
| **P4** | `defaultValue: "Default"` **không bao giờ chạy** — cả **152/152 dòng** đều có `Condition` | như trên |

🔴 **P3 là cái nặng nhất.** Nó làm `fieldMeta?.Required`, `fieldMeta?.DataType`,
`fieldMeta?.CodeSetCode` **vô hiệu toàn bộ**.

**P3 KHÔNG sửa bằng cách truyền `packet.ID`** — làm vậy là **hồi sinh** một nguồn mà chốt bảo không
đọc. P3 là **bằng chứng đường nạp đó nên XOÁ**. Và vì `fieldMeta` luôn `null`, gỡ cả đường
**không đổi một hành vi nào hôm nay** — đây là lúc rẻ nhất để làm.

### Đường nạp thừa còn trong mã

| Chỗ | Hàm / thành phần | Việc |
|---|---|---|
| `DataOutbound/Extraction/DataOutboundExtractionProcess.cs` | `Extract` | gọi `LoadPacketFields(db, packet.Code)` — **truy vấn CSDL mỗi chu kỳ, trả 0 dòng** |
| `DataOutbound/Extraction/DataOutboundExtractionProcess.Fields.cs` | `LoadPacketFields` | cả tệp này chỉ phục vụ đường nạp đó |
| **`ShareDataWorker.Core/Models/DataOutbound/DataOutboundExtractionResult.cs`** | `PacketFields` | `List<PacketFieldDto>` — mang xuyên suốt ba tầng |
| `DataOutbound/Mapping/DataMappingProcess.cs` | `Map` | lặp `extraction.PacketFields` gom `neededCodeSets`; rồi truyền vào `Transform` làm `declaredFields` |
| `DataOutbound/Mapping/DataMappingProcess.cs` | `Transform` · `RenderShapeNode` | **26 chỗ** dùng `fieldMeta` / `fieldsDict` / `declaredFields` |

## 3.2 Bộ khung thật dùng `$extend` rất mỏng 🔴

Phễu lọc chiều Gửi đang sống (`TEST_101COMMONDATA_OUT`, gói `101_commonData`):

| Đo | Kết quả |
|---|---|
| Số trường bộ khung gọi | **16** |
| Số trường **có** `$extend` | 🔴 **1 / 16** — chỉ `trafficCondition` |
| Khoá đang dùng | `targetType: "string"` · `codeSet: "condition"` · `defaultValue: "Default"` |
| `required` · `format` · `expression` | **0 / 16** |

**Kẹt ở đâu:** cơ chế đã đủ, **thiếu dữ liệu**. Mọi trường trừ `trafficCondition` đang đi qua không ép
kiểu, không lớp chặn, không quy đổi. Đây là **việc điền dữ liệu, thuộc bên giao diện/module** — nguồn
để điền đã có sẵn trong `ShareDataPacketField.Type` và `.IsRequired`.

Ghi nhận thêm từ bộ khung thật:

- **Không dùng `$each`.** `data` là mảng chứa **một object khuôn**, nên engine nở N bản ghi dưới
  **một** header.
- **Cùng một trường dùng ở hai khoá**: `dataTime` xuất hiện cả ở `header` lẫn trong `trafficInfo`.
  Đúng thiết kế *"luật gắn theo vị trí, không theo trường"* — nhưng nghĩa là `$extend.format` phải
  **khai ở cả hai chỗ**, khai một chỗ thì chỗ kia vẫn ISO-8601.

## 3.3 Thiếu giá trị hệ thống

Tầng ánh xạ **không có khái niệm** thời điểm hiện tại, số thứ tự bản tin, mã đối tác. Bộ khung hiện
khai `header.requestId`, `messageType`, `version`, `source` là **hằng số**.

**Kẹt ở đâu:** khoá nào đối tác đòi thời điểm gửi hoặc số hiệu bản tin thì hiện không điền được.

## 3.4 Header bị nhân bản — cần đo lại

Tệp kết xuất cũ (08/09) có `payload` là mảng **299 phần tử**, mỗi phần tử `{header, data}` kèm đúng
một dòng ⇒ header nhân ra 299 bản, tệp phình 198 KB.

**Nhiều khả năng đã hết** vì bộ khung hiện tại không dùng `$each`. Theo test
`Transform_RecordTemplateArray_...`, đường này sinh **một header và nở N bản ghi data**.
Vẫn phải **đo lại** ở lần chạy tới.

## 3.5 Chưa có "gửi khi có dữ liệu mới" 🔴

### Đã chốt gì

Anh Sơn, họp **09/09**:

> *"Cái gửi theo 'sự kiện' với 'gửi 1 lần' đó **bỏ đi**! Thay vào đó thêm một cái **Switch**:
> `Tự động gửi khi có dữ liệu mới`. Bật: cứ khi nào DB phát sinh bản ghi mới là đẩy gói tin đi ngay.
> Tắt: chỉ gửi theo lịch trình định kỳ."*

Làm rõ **16/09**: phát hiện bản ghi mới **dựa vào `UpdateTime` / `CreateTime`**.

### Hiện trạng mã — chưa làm gì cả

| Thứ | Trạng thái |
|---|---|
| Cờ "gửi khi có dữ liệu mới" | **KHÔNG tồn tại** — không cột nào |
| `SubMode.Event` · `SubMode.Single` | **vẫn còn** trong enum, dù đã chốt bỏ |
| `Mode = Event` trong bộ lập lịch | chỉ trả `now + 5 giây` — **poll nhanh hơn**, không phải theo dữ liệu |
| `EventSourceId`, `DebounceSec` | còn cột, **worker không đọc lần nào** |

### Cờ này chỉ có nghĩa với gói ẢNH CHỤP

| Loại gói | Gói nào | Hành vi hiện tại | Cần cờ không |
|---|---|---|---|
| **Tăng dần** | 103, 104, 106, 107, 109 | truy vấn đã lọc theo mốc · không có dòng mới thì **đã dừng** | ❌ **đã đúng sẵn** |
| **Ảnh chụp** | 101, 102, 105, 108, 110, 111 | truy vấn trả **toàn bộ** mỗi lần ⇒ **luôn gửi**, kể cả không đổi gì | 🔴 **cần** |

Gói 101 đang gửi lại **toàn bộ dòng mỗi chu kỳ**, dù dữ liệu không đổi.

**Vướng kỹ thuật:** câu truy vấn gói ảnh chụp **không select `__watermark`**, bước trích xuất cũng bỏ
qua phần dò mốc. Muốn làm thì phải thêm `MAX(UpdateTime)` làm `__watermark` rồi so với `LastTimeRun`.

| # | Việc | Thuộc ai |
|---|---|---|
| a | Thêm cột cờ vào `ShareDataSubscription` | **đụng lược đồ — người khác** |
| b | Thêm `MAX(UpdateTime)` làm `__watermark` vào 6 câu truy vấn ảnh chụp | worker |
| c | Bước trích xuất tính mốc cho **cả** gói ảnh chụp | worker |
| d | Cờ bật mà mốc **không mới hơn** `LastTimeRun` → **bỏ qua, không gửi** | worker |
| e | Dọn tàn dư `SubMode.Event`, `SubMode.Single`, `EventSourceId`, `DebounceSec` | **chờ chốt** — rule 19.6 |

Việc a chặn việc d. Việc b và c làm được ngay, và **tự nó đã có ích**: có mốc thì nhật ký biết dữ liệu
tính tới thời điểm nào.

## 3.6 Chưa chạy từ 15/09

Nhật ký hoạt động dừng ở 15/09. Cấu hình đường dẫn xuất và địa chỉ đối tác **đã sửa**, nên chạy lại
được — nhưng **chưa có tệp mốc nào theo hành vi mới** để so.

---

# 4. Việc còn lại

| # | Việc | Gỡ mục | Đổi hành vi | Trạng thái |
|---|---|---|---|---|
| # | Việc | Gỡ mục | Đổi hành vi | Trạng thái |
|---|---|---|---|---|
| **6b** | **Gỡ đường nạp `ShareDataPacketField`** — bỏ `LoadPacketFields`, `PacketFields`, `declaredFields`, `fieldsDict`, `fieldMeta`, nhánh phẳng | 3.1 · P3 | không¹ | 🔴 **chưa có prompt** |
| 7 | **Phép gộp cấp tập dòng** | 2.3 | có | **khoan làm** |
| 9 | **Gửi khi có dữ liệu mới** | 3.5 | có | chờ — **cột cờ thuộc người khác** |
| — | **Điền `$extend`** cho 15 trường còn lại | 3.2 | có | **bên giao diện/module lo** |
| — | **Chốt 6 trường đặc tả đòi mà CSDL chưa có** | 5 | có | chờ — quyết định nghiệp vụ |

¹ Không đổi hành vi **hôm nay**, vì theo P3 `fieldMeta` đã luôn `null`. Nhưng vỡ **19 lời gọi
`Transform(`** và **43 chỗ dùng `PacketFieldDto`**, nằm ở **hai** tệp kiểm thử:

| Tệp | `Transform(` | `PacketFieldDto` |
|---|---|---|
| `tests/ShareData/Services/DataOutboundServiceTests.cs` | 13 | 36 |
| `tests/ShareData/Services/DataOutboundMappingTests.cs` | 6 | 7 |

> ✅ **Vừa xong 17/09** — xem mục 7: đổi tên `DataOutbound` · gỡ 6 dòng `NULL AS`.
> Sau khi gỡ `NULL AS`, `ESH-1205` bắt đầu báo đúng 6 tên đó, khoảng **12 bản ghi/giờ**
> (`AlertThrottleInterval = 5 phút`). **Báo đúng, không phải lỗi mới** — khoảng trống có thật,
> trước bị `NULL AS` che đi. Dữ liệu gửi đi **không đổi**, 6 trường vẫn ra `null`.

## Việc 6 — xong một nửa 🟡

| Nửa | Trạng thái |
|---|---|
| Thôi đọc `ShareDataTable.FieldsJson` | ✅ xong 17/09 |
| **Bộ khung thành nguồn DUY NHẤT** | 🔴 **chưa** — mới đổi nguồn phụ này lấy nguồn phụ khác |

## Số đo tham chiếu — `mssql_dev` (local), 17/09/2026

| Đo gì | Kết quả |
|---|---|
| Lược đồ local so với mã của Hiếu | 🟩 **đã đồng bộ** — `ShareDataPacketField.GroupName` và `ShareDataPacket.Status` đều có |
| Phễu lọc **đang sống** | **1 chiều Gửi** (`TEST_101COMMONDATA_OUT`, `Direction = 0`) · 1 chiều Nhận · 2 bản ghi đã xoá mềm |
| Gói tin có phễu lọc | **chỉ `101_commonData`** |
| `ShareDataPacketField` theo gói | 101→**18** · 107→16 · 109→15 · 104→15 · 108→13 · 106→12 · 105→11 · 102→11 · 103→11 · 110→10 |
| `PKT111` | 🔴 **0 dòng** — chưa khai trường nào |
| `ShareDataMapping.Format` | **cột đã tồn tại**, giá trị `null` |

---

# 5. Chưa chốt

| Vấn đề | Lựa chọn |
|---|---|
| **Sáu trường đặc tả đòi mà CSDL chưa có** (`routeName` · `roadType` · `roadAuthority` · `pavementType` · `laneCount` · `shoulderWidth`) | bỏ khỏi bộ khung, hay bổ sung cột thật vào CSDL. Chừng nào chưa chốt thì `ESH-1205` còn báo, và **báo đúng**. |
| **Bộ mã `laneId`** | `FieldsJson` cũ khai `codeSet: "laneId"`, bộ khung **không** khai ⇒ hiện **mất hẳn quy đổi**. Bù `$extend.codeSet` vào bộ khung; hay chấp nhận mất |
| **Bộ mã mặc định cấp gói tin ở đâu** | thêm cột vào `ShareDataPacketField`; hay chấp nhận khai lặp từng đối tác; hay thêm cột **chỉ làm gợi ý** cho giao diện, worker vẫn chỉ đọc `$extend` |
| **Chia nhỏ gói khi vượt kích thước tối đa** | chuẩn ISO yêu cầu, mã không có — có làm không |
| **Hợp đồng luồng GỬI đang nằm HAI NƠI** — cố ý hay sót? | Đợt 17/09 chuyển `IDataOutboundExtractionProcess` · `DataOutboundExtractionResult` · `DataMappingResult` sang **`ShareDataWorker.Core`**, nhưng `IDataOutboundSender` · `DataOutboundSendResult` **vẫn ở worker**. Cùng là hợp đồng giữa ba tầng — chuyển nốt cho nhất quán, hay giữ nguyên? |

---

# 6. Ngoài phạm vi

- Không đụng luồng nhận.
- Không đổi câu truy vấn hardcode — đã chốt giữ trong mã.
- **Không đổi `DataOutboundRestSender`** — thân bản tin 7 khoá khớp **đúng 1-1** với thực thể bên
  nhận, là hợp đồng nội bộ đã thống nhất. *"Bỏ cái vỏ"* nhắm vào phong bì PDU, **đã bỏ rồi**.
- Không xoá mã trở thành không dùng (rule 19.6).
- **Không sửa P1 · P2 · P4** — lỗi dữ liệu, bên cấu hình lo.

---

# 7. Nhật ký thay đổi

| Ngày | Việc | Kết quả |
|---|---|---|
| 17/09 | **Việc 3** — đảo giá trị mặc định lên trước biểu thức | ✅ · kèm bản sao dòng dữ liệu để biểu thức thấy giá trị mặc định · 79 pass |
| 17/09 | **Việc 8** — ngắt khi không tìm thấy phễu lọc | ✅ `ESH-1304 MappingNotFound` · 80 pass |
| 17/09 | Commit `fa60436d` (Văn Hiếu) **xoá thực thể `ShareDataTable`** | worker gãy **8 lỗi `CS0246`** · cùng đợt thêm CRUD `ShareDataPacket`/`ShareDataPacketField`, cột `GroupName` và `Status` |
| 17/09 | **Gỡ cụm mã chết** — `BuildQuery`, `GetFullTableName`, 2 hàm `ResolveIncremental*`, 2 forwarder | ✅ `PacketQueryBuilder` → **`PacketJsonParser`** (chỉ còn `ParseFields` + `ParseCodeValues`) · **bản dịch xanh** |
| 17/09 | **Việc 6 (nửa đầu)** — `LoadPacketFields` đọc `ShareDataPacketField` thay `FieldsJson` | 🟡 nửa sau chưa làm · và theo **P3** đường này trả 0 dòng |
| 17/09 | **Việc 4** — đọc `$extend.required` + `$extend.format` | ✅ cờ bắt buộc thành *hoặc–hoặc* |
| 17/09 | **Việc 5** — bỏ rào `if (!hasCodeSet)`, thêm nhánh `datetime` + `FormatDateTime` | ✅ · ⚠️ **không đổi đầu ra hôm nay**: `trafficCondition` sau bộ mã vốn đã là chuỗi, và chưa trường nào khai `targetType: "datetime"` |
| 17/09 | **Việc 2** — đối chiếu tên bộ khung với khoá dòng thô | ✅ `ESH-1205 ShapeFieldNotInRawRow`, gom một lần mỗi chu kỳ |
| 17/09 | **Việc 1** — bộ khung rỗng/hỏng cú pháp thì ngắt | ✅ `ESH-1206 ShapeInvalid` · nhánh phẳng giữ nguyên, chỉ không với tới được |
| 17/09 | **Bù 6 bí danh `NULL AS`** vào `QueryPacket101` | 17/09 thêm → 17/09 **gỡ lại** theo chốt "có gì lấy đó" |
| 17/09 | **Gỡ lại 6 dòng `NULL AS`** | ✅ `QueryPacket101` còn **13 bí danh có nguồn thật** · tệp kiểm thử **không phải sửa dòng nào** · `ESH-1205` nay báo đúng 6 tên thiếu (~12 bản ghi/giờ) — **báo đúng**. `NULL AS snapshot` gói 102 và `CAST(NULL AS …)` gói phí **giữ nguyên**, ngoài phạm vi |
| 17/09 | **Đổi tên `DataPublication` → `DataOutbound`** — 242 lượt / 27 tệp / 13 tệp đổi tên, trải **2 dự án** | ✅ **XONG** · đối xứng với `DataInbound` · rename **đụng cả luồng NHẬN** (4 lời gọi thật trong `DataInboundService`) · còn đúng **1** chỗ giữ tên cũ **cố ý**: chú thích trong `ShareDataMapping.cs` — entity dùng chung, **cần báo chủ sở hữu** |

## Đối chiếu tên tệp qua các lần tái cấu trúc

Tên **hiện hành** là cột phải. Hai cột trái chỉ để tra khi đọc tài liệu cũ hoặc lịch sử git.

| Đời 1 | Đời 2 | **Đời 3 — HIỆN HÀNH** |
|---|---|---|
| `Orchestration/DataPublicationService.cs` | `DataPublicationService.cs` | **`DataOutboundService.cs`** |
| `Orchestration/DataPublicationService.Validation.cs` | tách 3 | **`PacketMetadataResolver` · `PacketJsonParser` · `SqlInjectionGuard`** *(tên trung tính, chỉ đổi namespace)* |
| `Orchestration/DataPublicationService.Schedule.cs` | `Scheduling/DataPublicationScheduler.cs` | **`Scheduling/DataOutboundScheduler.cs`** |
| `DataPublicationContext.cs` | `Context/DataPublicationContext.cs` | **`Context/DataOutboundContext.cs`** |
| `DataExtractionProcess*` | `DataPublicationExtractionProcess*` | **`DataOutboundExtractionProcess*`** |
| `ExtractionResult` | `DataPublicationExtractionResult` | **`DataOutboundExtractionResult`** |
| `FileExportSender` / `RestPacketSender` | `DataPublicationFileSender` / `DataPublicationRestSender` | **`DataOutboundFileSender` / `DataOutboundRestSender`** |
| `IPacketSender` / `SendResult` | `IDataPublicationSender` / `DataPublicationSendResult` | **`IDataOutboundSender` / `DataOutboundSendResult`** |
| — | `DataPublicationWorker` / `IDataPublicationService` | **`DataOutboundWorker` / `IDataOutboundService`** |

Thư mục: `Infrastructure/Services/DataPublication/` → **`Infrastructure/Services/DataOutbound/`**.

### Đợt 17/09 còn DỜI CHỖ, không chỉ đổi tên

Bốn thứ đổi cả **dự án** hoặc **thư mục** — tra ở đây nếu tìm không thấy:

| Kiểu | Trước | **Vị trí thật hiện nay** |
|---|---|---|
| `DataOutboundExtractionResult` | `DataOutbound/Extraction/` | **`ShareDataWorker.Core/Models/DataOutbound/`** |
| `DataMappingResult` | `DataOutbound/Mapping/` | **`ShareDataWorker.Core/Models/DataOutbound/`** |
| `IDataOutboundExtractionProcess` | `DataOutbound/Extraction/` | **`ShareDataWorker.Core/Interfaces/DataOutbound/`** |
| `PacketJsonParser` | `DataOutbound/Extraction/` | **`DataOutbound/Mapping/`** *(vẫn trong worker)* |

> ⚠️ Còn **đúng một** chỗ giữ tên cũ, **cố ý**: `Module.ShareData.Core/Entities/ShareDataMapping.cs`
> — chú thích nhắc `DataPublicationService`. Entity dùng chung do WebAPI sở hữu, **không tự sửa**;
> cần báo chủ sở hữu.

🔴 **Tài liệu này KHÔNG ghi số dòng mã.** Cấu trúc đã đổi nhiều lần. Tìm theo **tên hàm**.

## Ba lần đo sai đã được thay

Ghi lại để không đo lại theo cách cũ:

| Từng kết luận | Thật ra |
|---|---|
| *"Gói 101 khai 0 trường `codeSet` trong `FieldsJson`"* | khai **hai**: `laneId` và `trafficCondition` |
| *"`isRequired` bắt buộc cho `zoneId` và `zoneStatusId`"* | `FieldsJson` khai `zoneId` + **`dataTime`**; `zoneStatusId` không có trong `FieldsJson` |
| *"Chuyển sang `ShareDataPacketField` thì không mất gì"* | **không áp dụng** — đã chốt worker không đọc bảng đó |

## Kiểm chứng khi chạy lại

```
dotnet test tests/test.csproj --filter "FullyQualifiedName~Services.DataOutbound"
```

🔴 Trước khi chạy: xác nhận connection string trong `tests` trỏ **local** (`127.0.0.1,14333`).
Thấy `10.10.8.30` → **huỷ ngay và báo lại**.

**Soi tệp kết xuất thật** trong `sharedata/send`:

| Kiểm | Kỳ vọng |
|---|---|
| 6 trường giữ chỗ | `null`, **không** sinh `ESH-1205` |
| `vehicleCount` · `fromLocationMet` · `averageSpeed` | không có trong đầu ra, **không** cảnh báo |
| `fromLocationMet` = `0`, `averageSpeed` = `45.5` ở **mọi dòng** | đúng **P1**, không phải lỗi mới |
| 5 dòng `conges` | nhả nguyên `"conges"` — đúng **P2** |
| Số phần tử `payload` | đo lại, kỳ vọng **một** header |




