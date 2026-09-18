# Luồng GỬI ShareData — luồng chuẩn, nguồn cấu hình, và đang kẹt ở đâu

> **Tài liệu sống.** Cập nhật lần cuối: **18/09/2026**.
> Chỉ nói về **luồng xử lý**. Việc lặt vặt tách sang `../Prompt/`.
> Đọc từ trên xuống một lượt là hiểu trọn luồng — không cần mở tệp nào khác.

---

## Chú giải dấu

Dùng thống nhất trong cả tài liệu. Dấu đứng trước mô tả trạng thái của **chính việc ghi trên dòng đó**:

| Dấu | Nghĩa |
|---|---|
| 🔴 | **chưa có trong mã** |
| 🟠 | **có nhưng chạy sai** |
| 🟡 | **có, chạy được, nhưng chờ chốt** — hoặc đang đề xuất bỏ |
| 🟢 | **có và chạy đúng** |
| ⚠️ | **nhấn mạnh** — không phải trạng thái |

---

# 0. LUỒNG CHUẨN — đi từ đầu tới cuối

```
┌─ A · CHỌN VIỆC ────────────────────────────────────────────────────────┐
│  worker thức mỗi 5 giây                                                │
│    → quét đăng ký đến hạn   (chiều Gửi · đối tác đang bật & kết nối)   │
│    → nhận lease             (đẩy NextTimeRun ra xa, tối thiểu 5 phút)  │
│    → tra GÓI TIN                                                       │
│    → tra PHỄU LỌC           🟢 có truy vấn                              │
│       └ không tìm thấy      🟢 NGẮT, ghi ESH-1304, không gửi            │
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
│  🟢 bộ khung rỗng / hỏng cú pháp → NGẮT, ghi ESH-1206                   │
│  🟢 tên bộ khung gọi mà dòng thô không có → trường đó ra null, im lặng  │
│  🔴 phép gộp cấp tập dòng  (đếm · tổng · trung bình · min · max)        │
│                                                                        │
│      với TỪNG TRƯỜNG, đúng thứ tự:                                     │
│                                                                        │
│         giá trị thô                                                    │
│             │                                                          │
│             ├─ 🟢  rỗng → lấy GIÁ TRỊ MẶC ĐỊNH   $extend.defaultValue   │
│             ├─ 🟢  BIỂU THỨC                     $extend.expression     │
│             ├─ 🟢  BỘ MÃ quy đổi                 $extend.codeSet        │
│             ├─ 🟢  ÉP KIỂU                       $extend.targetType     │
│             └─ 🟢  ĐỊNH DẠNG đầu ra              $extend.format         │
│                                                                        │
│  trường BẮT BUỘC mà rỗng → NGẮT CẢ LÔ  🟢 $extend.required              │
│  đóng gói JSON  🟢 không phong bì · không mã băm                        │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │  thất bại → DỪNG, KHÔNG sang D
                                    ▼
┌─ D · BƯỚC 3 — XUẤT BẢN ────────────────────────────────────────────────┐
│  HAI kênh KHÔNG ngang hàng                                             │
│                                                                        │
│    gửi API tới đối tác   LUÔN gọi, kể cả khi thiếu EndPointApiUrl      │
│                          → GIAO HÀNG · QUYẾT TRẠNG THÁI                │
│                          thành/bại đều GHI LỊCH SỬ · lỗi → ESH-1402    │
│                                                                        │
│    ghi tệp xuống đĩa     🟢 CHỈ ở Dev/Debug/Test                       │
│                             hoặc bật NasStorage:EnableFileExport       │
│                          → bản lưu để xem · KHÔNG ghi log, kể cả lỗi   │
│                          → KHÔNG ảnh hưởng trạng thái                  │
│                                                                        │
│  trạng thái lô = kết quả gửi API, một mình                             │
│                                                                        │
│  ghi nhật ký kết quả  +  ghi lại mốc (chỉ khi thành công)              │
│  tính lần chạy kế tiếp:                                                │
│      Mode=Event   🟠 now+5s  (poll nhanh, KHÔNG kiểm dữ liệu mới)      │
│      kind=daily   🟢 theo giờ · thứ · biên ngày                        │
│      continuous   🟢 now+Interval, kẹp trong khung giờ                 │
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
| A6 | Tra **phễu lọc** | theo đối tác + mã gói + chiều Gửi → `ShareDataMapping` · 🟢 không thấy thì **ngắt, ghi `ESH-1304`, không gửi** |
| A7 | Dựng ngữ cảnh | gói tin + đối tác + đăng ký + phễu lọc, mang theo suốt ba bước |

## Giai đoạn B — BƯỚC 1: Lấy dữ liệu

| # | Làm gì | Chi tiết |
|---|---|---|
| B1 | Tra bảng đăng ký truy vấn | theo mã gói → ra **một hàm C#** · không có → ném lỗi |
| B2 | **Chạy câu SQL cứng trong hàm đó** | 🟢 câu lệnh nằm trong mã, **không** trong CSDL |
| B3 | Nhận danh sách dòng thô | **khoá mỗi dòng = bí danh sau `AS`** của câu SQL |
| B4 | Không có dòng nào | ghi nhật ký "không có dữ liệu mới", **dừng, không gửi** |
| B5 | Dò mốc gia tăng | với gói kiểu tăng dần, đọc `__watermark` và `__rowid` để lần sau lấy tiếp |

**Ra khỏi bước 1:** danh sách dòng thô + mốc gia tăng. Không đụng gì tới cấu hình ánh xạ.

## Giai đoạn C — BƯỚC 2: Ánh xạ

Nguồn cấu hình: **`ShareDataMapping.TargetShapeJson`** — xem mục 1.

| # | Làm gì | Chi tiết |
|---|---|---|
| C1 | 🟢 **Chặn bộ khung hỏng** | rỗng hoặc sai cú pháp JSON → ghi `ESH-1206`, huỷ kết xuất |
| C2 | 🟢 **Đối chiếu tên** | xem giải thích ngay dưới bảng |
| C3 | Nạp bộ mã | gom mã bộ khai trong bộ khung → đọc `ShareDataCodeSet` |
| C4 | 🔴 **Phép gộp cấp tập dòng** | đếm, tổng, trung bình, nhỏ nhất, lớn nhất — chạy **trước** khi vào từng trường · **khoan làm**, xem mục 2.3 |
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
| `A · B` | `A · B · C` | `A·B` ánh xạ · **`"c": null`** | 🔇 **không** — chốt 18/09, xem dưới |

### 🟢 Cả hai kiểu lệch đều im lặng — chốt 18/09

Trước 18/09, kiểu lệch thứ hai còn ghi thêm cảnh báo `ESH-1205`. **Đã bỏ.** Lý do:

Rà **cả 6 bản ghi họp**: **không buổi nào yêu cầu cảnh báo này** — nó do người viết tài liệu tự thêm.
Và chốt **09/09** đã xử đúng vấn đề đó bằng cách **phòng ở giao diện**, không phải báo lúc chạy:

> *"Không cho người dùng gõ tay tên trường nữa! Load danh sách các trường từ CSDL lên cho người ta
> chọn bằng **Dropdown**, vừa chuẩn xác vừa không sợ sai hoa thường hay sai chính tả."*

Nặng hơn: sau khi bỏ 6 dòng `NULL AS` theo chốt *"truy vấn có gì lấy đó"*, cảnh báo **bắn đúng 6 tên
mỗi 5 phút, mãi mãi** — là 6 trường *"đặc tả đòi nhưng CSDL chưa có"*, đang chờ **quyết định nghiệp
vụ** ở mục 5. Tức là **không ai tắt được bằng cách sửa mã**. Báo động lặp vô nghĩa làm người đọc mất
tin vào mọi cảnh báo khác.

**Hành vi hiện tại:** trường bộ khung gọi mà dòng thô không có → **ra `null`, im lặng**. Hết.

⚠️ Hằng số `ShapeFieldNotInRawRow` **vẫn nằm trong `ShareDataAlertCode.cs`** nhưng **không nơi nào
dùng** — giữ theo rule 19.6.

**Gói 101 hôm nay:**

| | Số | Tên |
|---|---|---|
| Bộ khung gọi · dòng thô **không có** | **6** | ra `null` im lặng — `routeName` · `roadType` · `roadAuthority` · `pavementType` · `laneCount` · `shoulderWidth`, xem mục 5 |
| Dòng thô có · bộ khung **không gọi** | **3** | `vehicleCount` · `fromLocationMet` · `averageSpeed` → bỏ qua im lặng |

> 💡 Chốt: "truy vấn có gì lấy đó", không khai bí danh giữ chỗ.

Với **từng trường**, đúng thứ tự này:

| # | Bước | Khoá `$extend` | Trạng thái |
|---|---|---|---|
| C6 | Lấy giá trị thô theo tên | `$field` | 🟢 |
| C7 | Rỗng thì lấy **giá trị mặc định** | `defaultValue` | 🟢 |
| C8 | **Biểu thức** | `expression` | 🟢 |
| C9 | **Bộ mã quy đổi** | `codeSet` | 🟢 |
| C10 | **Ép kiểu** | `targetType` | 🟢 |
| C11 | **Định dạng đầu ra** | `format` | 🟢 · không khai → ISO-8601 |
| C12 | Trường bắt buộc mà rỗng | `required` | 🟢 **ngắt cả lô**, ghi `ESH-1202` |
| C13 | Đóng gói JSON | — | 🟢 **không phong bì**, không mã băm |

**Ra khỏi bước 2:** mảng byte nội dung cuối, hoặc cờ thất bại kèm lý do.
**Thất bại thì KHÔNG sang bước 3.**

## Giai đoạn D — BƯỚC 3: Xuất bản

**Hai kênh KHÔNG ngang hàng.** Gửi API là **giao hàng** — nó quyết trạng thái. Ghi tệp là **bản lưu để
xem ở máy dev** — im lặng, hỏng cũng không sao.

| # | Làm gì | Chi tiết |
|---|---|---|
| D1 | **Ghi tệp** xuống đĩa | 🟢 **chỉ chạy khi `ShouldWriteFile()` cho phép** — xem bảng dưới · đường dẫn `Out/{đối tác}/{yyyyMM}/{ddHH}/{gói}/{tên}.json` · 🔇 **không ghi log gì, kể cả khi lỗi** · **không ảnh hưởng trạng thái** |
| D2 | **Gửi API** | **LUÔN gọi**, không hỏi có `EndPointApiUrl` hay không · thành công hay thất bại đều **ghi lịch sử** · lỗi → `ESH-1402` (Warning) |
| D3 | Quyết trạng thái | **= kết quả gửi API, một mình** · `LogExportResult` đúng **một lần** |
| D4 | Ghi lại mốc | **chỉ khi thành công**: cập nhật `LastTimeRun`, tăng số thứ tự, tính `NextTimeRun` |

### 🟢 Ghi tệp phụ thuộc MÔI TRƯỜNG — thêm ngày 18/09

`DataOutboundFileSender.ShouldWriteFile()` quyết theo thứ tự:

| # | Điều kiện | Kết quả |
|---|---|---|
| 1 | `NasStorage:EnableFileExport` có khai trong cấu hình | theo đúng giá trị đó — **đè mọi thứ dưới** |
| 2 | Môi trường = `Staging` · `Production` · `Prod` | **KHÔNG ghi tệp** |
| 3 | Môi trường = `Development` · `Dev` · `Debug` · `Test` · `Testing` | ghi tệp |
| 4 | Không xác định được môi trường | bản `DEBUG` → ghi · bản `RELEASE` → **không** ghi |

**Chốt 18/09 (anh Đạt) — hai vế:**

> *"Ghi file chỉ xử lý lưu ở môi trường development, nói chung là local; staging/production không ghi
> file. Và **ghi file thì không cần log gì hết**. Còn ở **bước gửi HTTP thất bại hoặc thành công đều
> phải ghi log hết**."*
>
> *"Không khai endpoint **kệ nó** nha, **cứ gọi HTTP gọi luôn**; khi gọi HTTP nó trả mã lỗi gì đó
> **ghi log**."*

⇒ **Thiếu `EndPointApiUrl` không còn là trường hợp đặc biệt.** Worker cứ gọi; URL thiếu đường dẫn sẽ
nhận **404 / từ chối kết nối** và ghi đúng lỗi thật nhận được. Không rẽ nhánh, không bỏ qua đăng ký.

⚠️ Bản 17/09 của tài liệu này từng ghi *"không có kênh nào thì bỏ qua đăng ký"* (mục **D0**) — **đã
bỏ**, đó là cách hiểu sai. Prompt tương ứng cũng đã xoá và viết lại.

### Tính lần chạy kế tiếp

| Chế độ | Hành vi | Trạng thái |
|---|---|---|
| `Mode = Event` | `now + 5 giây` rồi **gửi bình thường** | 🟠 **sai** — phải kiểm dữ liệu mới, xem 3.5 |
| `ScheduleJson.kind = "daily"` | theo giờ · thứ · biên ngày | 🟢 |
| mặc định (`continuous`) | `now + IntervalSeconds`, **kẹp vào `[StartTime, EndTime]`** | 🟢 **xong 18/09** (việc S1) · ca qua đêm `StartTime > EndTime` **bỏ ngỏ có chủ đích**, xem 3.7 |

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
| Chứa `TargetShapeJson` (và `$extend` bên trong) | 🟢 **có** | ❌ không |
| Khoá | `PartnerId` + `DatatypeId` + `Direction` | `DatatypeId` |
| Phạm vi | **riêng từng đối tác, từng chiều** | **dùng chung mọi đối tác** |
| Cột chính | `TargetShapeJson` · `PartnerSchemaJson` · `Direction` · `IsActive` · `Format` | `AliasFieldKey` · `Type` · `IsRequired` · `GroupName` · `Status` |
| Worker đọc lúc chạy? | 🟢 **duy nhất nguồn này** | ❌ **không** |

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
| `fieldKey` | `$field` | 🟢 |
| `dataType` | `$extend.targetType` | 🟢 |
| `codeSet` | `$extend.codeSet` | 🟢 |
| `expression` | ~~`$extend.expression`~~ | 🚫 **ĐÃ CHẾT** — §3.4 bản bàn giao: *"FE không sinh ra nữa, service coi như không có, gặp thì bỏ qua và ghi log"* |
| *(mới)* | ~~`$extend.defaultValue`~~ | 🔴 **KHÔNG TỒN TẠI** — tên thật là `defaultPartnerValue` (gửi) / `defaultSourceValue` (nhận). Mã đang đọc sai tên ⇒ **mặc định cấp lá không chạy** |
| `isRequired` | `$extend.required` | ⚠️ **không có trong bảng 6 khoá của §3.4** — xem mục 5 |
| *(mới)* | ~~`$extend.format`~~ | 🔴 **KHÔNG TỒN TẠI** — tên thật là `dateFormat` (2 chiều) / `numberFormat` (chỉ GỬI). Mã đang đọc sai tên ⇒ **hai khoá bị bỏ qua âm thầm** |

### 🔴 Bảng khoá `$extend` ĐÚNG — bản bàn giao §3.4

Đúng **sáu** khoá, tất cả đều không bắt buộc:

| Khoá | Ý nghĩa | Áp ở chiều | Worker đọc? |
|---|---|---|---|
| `codeSet` | `ShareDataCodeSet.Code` | cả hai | 🟢 |
| `targetType` | Kiểu **phía đối tác** (`string`·`int`·`dateTime`·`bool`…) | cả hai | 🟢 |
| `dateFormat` | Khuôn ngày **phía đối tác**, vd `dd/MM/yyyy HH:mm:ss` | cả hai | 🔴 **0** |
| `numberFormat` | Khuôn số **phía đối tác**, vd `0.#` | **chỉ GỬI** | 🔴 **0** |
| `defaultPartnerValue` | Giá trị gửi đối tác khi dữ liệu nội bộ rỗng | **chỉ GỬI** | 🔴 **0** |
| `defaultSourceValue` | Giá trị lưu nội bộ khi đối tác không gửi | **chỉ NHẬN** | 🔴 **0** |

⚠️ **HAI lớp mặc định trùng tên nhau** (§3.4) — đừng lẫn:

| | Mặc định của **bộ mã** | Mặc định của **lá** (`$extend`) |
|---|---|---|
| Kích hoạt khi | giá trị **có** nhưng **không khớp dòng nào** | giá trị **rỗng / không có** |
| Phạm vi | cả bộ mã, mọi hồ sơ dùng chung | đúng một khoá trong một hồ sơ |

Hồ sơ có **cả hai** ⇒ 🔴 **ưu tiên mặc định của bộ mã**.

🔴 **`targetType` · `dateFormat` · `numberFormat` mô tả PHÍA ĐỐI TÁC**, không phụ thuộc chiều. Gửi thì
ép sang khuôn đó; nhận thì dùng chính khuôn đó để **đọc** chuỗi đối tác rồi ép về
`ShareDataPacketField.Type`. Nhờ vậy hồ sơ *Cả hai* chỉ cần một bộ giá trị.

📎 Việc **N1** sẽ sửa bốn khoá worker chưa đọc. Xem mục 4.
| `internalOnly` | ~~`$exclude: true`~~ | 🚫 **đã bãi bỏ ở chiều gửi** — xem dưới |
| `noSource` | *bỏ hẳn* — **không dựng lại dưới dạng nào** | — |
| `orderNo` · `columnName` · `unit` · `isKey` | *không dựng lại ở chiều GỬI* | — |

### 🚫 `$exclude` — đã bãi bỏ ở chiều gửi

**Chốt 16/09** (review frontend), Anh Sơn:
> *"Khi người dùng đã chủ động đưa một trường vào mẫu cấu hình để gửi đi, việc tồn tại tùy chọn
> **'Bỏ khóa này khi gửi đi'** là mâu thuẫn và dư thừa — nếu không muốn gửi thì ngay từ đầu không
> thêm trường đó vào danh sách. **Rà soát và bỏ tùy chọn này ở chiều gửi.**"*

`TargetShapeJson` thật **không có khoá này** ở đâu cả. Giao diện **không được chào** tuỳ chọn đó nữa.

⚠️ Mã vẫn còn đọc `$exclude` ở **3 chỗ** trong `Mapping/DataMappingProcess.cs` — **giữ nguyên** theo
rule 19.6. Đọc mã thấy nó thì đừng tưởng là tính năng đang dùng.

## 1.3 Nội dung gửi đi

| | Cách cũ | Cách mới |
|---|---|---|
| Vỏ ngoài — **lớp 1** | phong bì PDU 9 khoá (`pduType`, `sender`, `destination`, `timestamp`, `hash`, `payload`…) | 🟢 **đã bỏ hết** |
| Vỏ ngoài — **lớp 2** | `httpPayload` 7 khoá trong `DataOutboundRestSender`: `partnerCode` · `datatypeId` · `packetVersion` · `serialNbr` · `pduType` · `format` · **`rawContent`** (chứa cả cục JSON đã ánh xạ dưới dạng **chuỗi**) | 🔴 **VẪN ĐANG GỬI** — sẽ bỏ ở **SV-1**, xem mục 4 |
| Mã băm | có tính | **bỏ** |
| Định dạng | JSON + XML | **chỉ JSON** |
| Rẽ nhánh theo phiên bản gói | không có | vẫn không có (đúng chốt) |

### Thân bản tin sau **SV-1** — luôn là MẢNG bản ghi

🔴 **Chốt anh Đạt 18/09: thân HTTP luôn là mảng bản ghi, không có ngoại lệ.**

Phải chốt vì `FinalBytes` hôm nay **có hai dạng** tuỳ bộ khung — xem `DataMappingProcess.Map`,
nhánh `ShapeHasRepeatBlock`:

| Bộ khung | `FinalBytes` **hôm nay** | Thân HTTP sau SV-1 |
|---|---|---|
| **Có khối lặp** — 🟢 **gói 101 đang dùng** | **object** `{ "header": {…}, "data": [ …N bản ghi… ] }` | **bóc lớp**: chỉ gửi mảng `data` |
| **Phẳng** — không khối lặp nào | **mảng** `[ {…}, {…} ]` | gửi **thẳng** |

⚠️ **"Khối lặp" KHÔNG đồng nghĩa với `$each`.** `ShapeHasRepeatBlock` trả `true` khi gặp **một trong hai**:

| Dạng | Ví dụ |
|---|---|
| `$each: true` | khai tường minh |
| **mảng khuôn bản ghi** — mảng mà phần tử có `$field` | `"data": [ { "zoneId": {"$field":"zoneId"}, … } ]` |

🔴 **Bộ khung thật của gói 101 KHÔNG có `$each` ở đâu cả** — nó vào nhánh gộp qua **dạng thứ hai**
(`data` là mảng chứa **một object khuôn**). Đừng tìm `$each` trong bộ khung rồi kết luận nhầm là nhánh
phẳng.

**Đầu ra gói 101 hôm nay** (`RenderShapeAggregate` chạy **một lần** cho cả tập dòng ⇒ **một** header):

```json
{ "header": { "requestId": "REQ-20260914-001", "messageType": "TrafficData", "version": "1.0",
              "source": "ITS-TMS", "dataTime": "…" },
  "data": [ { "zoneId": "…", "trafficInfo": [ { "trafficCondition": "0", "dataTime": "…" } ], … },
            { … } ] }
```

**Sau SV-1 — chỉ còn phần trong `data`:**

```json
[ { "zoneId": "…", "trafficInfo": [ … ], … }, { … } ]
```

⚠️ **`header` bị bỏ** (`requestId`, `messageType`, `version`, `source`, `dataTime`). Đây là **chọn có
chủ đích** của anh Đạt, đổi lấy việc bên nhận luôn parse đúng **một** dạng. **Đừng tưởng là lỗi.**
Nếu sau này đối tác cần mấy trường đó thì phải đưa **vào trong từng bản ghi**, không dựng lại vỏ.

⚠️ **Chưa chốt:** bóc `data` đặt ở **tầng gửi** hay **tầng ánh xạ** — xem mục 5.

📌 Tệp kết xuất cũ ngày 08/09 (trong `sharedata/send/Out/Tesst/…`) có **`header` lặp lại ở mọi bản
ghi** — đó là **engine cũ trước khi có `RenderShapeAggregate`**. Đừng lấy tệp đó làm mẫu đối chiếu.

---

# 2. Biến đổi một trường

## 2.1 Thứ tự năm bước

🔴 **Chốt lại 18/09 theo bản bàn giao của Hiếu** (`TargetShapeJson.md` §4.1) — **khác** thứ tự tài liệu
này ghi trước đó:

| | Thứ tự |
|---|---|
| Trước 17/09 | biểu thức → bộ mã → rỗng lấy mặc định → ép kiểu |
| 17/09 (theo wireframe) | rỗng lấy mặc định → biểu thức → bộ mã → ép kiểu → định dạng |
| **ĐÚNG — bản bàn giao 18/09** | **bộ mã → rỗng thì `defaultPartnerValue` → ép kiểu + định dạng** 🔴 **mã chưa theo** |

Hai điều bản bàn giao nhấn, mã hiện tại **đều chưa có**:

1. 🔴 **Mặc định áp SAU bộ mã, không phải trước.** *"Giá trị nội bộ rỗng thì bộ mã không khớp gì cả,
   lúc đó mới lấy mặc định."*
2. 🔴 **Đã qua bộ mã thì KHÔNG ép kiểu nữa** — giá trị bộ mã trả ra **là giá trị cuối cùng**.

⚠️ Lý lẽ cũ *"thứ tự này phải giữ vì chiều nhận đảo lại đúng bốn bước"* **không còn đứng** — §5.1 bản
bàn giao mô tả chiều nhận là *"bộ mã ngược → rỗng thì `defaultSourceValue` → parse theo `dateFormat`"*,
tức **vẫn đối xứng** với thứ tự mới.

📎 Việc **N1** sẽ sửa. Xem mục 4.

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

### 🔴 HAI loại "giá trị mặc định" — đừng lẫn

Đây là chỗ dễ nhầm nhất của cả luồng. **Hai cơ chế khác nhau, chạy ở hai bước khác nhau:**

| | `$extend.defaultValue` | `CodeSet.isDefault` |
|---|---|---|
| Khai ở đâu | trong **bộ khung** (`TargetShapeJson`) | trong **bộ mã** (`ShareDataCodeSet.ValuesJson`) |
| Kích hoạt khi | **giá trị thô rỗng** | giá trị **không khớp mã nào** trong bộ |
| Chạy ở bước | **C7** — *trước* bộ mã | **C9** — *bên trong* bộ mã |
| Phạm vi | theo từng trường, từng đối tác | theo từng bộ mã, dùng chung |

**Chốt 11/09:**
> *"Nếu dữ liệu không nằm trong CodeSet: hỗ trợ giá trị mặc định (`DefaultValue`) **hoặc giữ nguyên
> giá trị gốc**."*

⇒ Không khớp mã mà bộ mã **không** khai `isDefault` thì **giữ nguyên giá trị gốc** — **đúng thiết kế**,
không phải lỗi. Đó chính là ca `conges` ở **P2** mục 3.

**Chốt 16/09** còn yêu cầu thêm: giá trị mặc định của bộ mã phải **phân chiều gửi / nhận**
(*"dữ liệu mặc định thì xem coi là dữ liệu chiều nào"*) — **chưa làm**, xem mục 5.

⚠️ Hệ quả cần nhớ khi đọc kết quả: nếu bộ mã **có** `isDefault`, thì giá trị `null` đi vào `MapCode`
**cũng bị thay** bằng giá trị mặc định của đối tác. Bộ `condition` không có nên chưa dính;
bộ `pave` thì **có** (`2 → "off"`).

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

## 2.4 Bộ mã — hai cấu trúc `ValuesJson`, mặc định tách theo chiều

### Cấu trúc MỚI (sắp dùng)

```json
{
  "values": [
    { "sourceValue": "1", "partnerValue": "on",  "displayName": "display" },
    { "sourceValue": "2", "partnerValue": "off", "displayName": "display" }
  ],
  "defaultSourceValue": "4",
  "defaultPartnerValue": "false"
}
```

### Cấu trúc CŨ (đang nằm trong CSDL)

Gốc là **mảng trần**, mặc định nằm **trong từng dòng**:

```json
[ { "sourceValue": "normal", "partnerValue": "0", "displayName": "", "isDefault": false }, … ]
```

### Ngữ nghĩa mặc định — chốt 18/09

| Chiều | Khớp một dòng `values[]` | **Không khớp dòng nào** |
|---|---|---|
| **GỬI** (source → partner) | xuất `partnerValue` | xuất **`defaultSourceValue`** |
| **NHẬN** (partner → source) | xuất `sourceValue` | xuất **`defaultPartnerValue`** |

⚠️ **Chiều GỬI dùng `defaultSourceValue`, KHÔNG phải `defaultPartnerValue`.** Điều này **ngược trực
giác** — *"gửi đi thì mặc định phải là giá trị phía đối tác chứ?"*. Nhưng đây là chốt của anh Đạt:
*"default src value là giá trị default bên mình gửi đi"*. Ai đọc mã thấy lạ thì **hỏi, đừng tự sửa**.

### Thứ tự ưu tiên khi không khớp

| # | Nguồn | Dùng cho |
|---|---|---|
| 1 | `defaultSourceValue` (chiều GỬI) | cấu trúc **mới** |
| 2 | dòng có `isDefault == true` | cấu trúc **cũ** — tương thích ngược |
| 3 | trả **nguyên giá trị thô** + cảnh báo | không khai mặc định nào |

⚠️ `ParseCodeValues` hiện **chỉ đọc gốc mảng** — gặp gốc object thì trả **rỗng**, và `MapCode` với
danh sách rỗng sẽ **trả nguyên giá trị thô, không cảnh báo**. Nghĩa là ngày dữ liệu di trú mà mã chưa
sửa thì **bộ mã tắt âm thầm**. Xem mục 4.

---

# 3. Đang kẹt ở đâu

## 3.1 Bốn phát hiện đo ngày 17/09 🔴

Đo trên `mssql_dev` = `DEV_ITS10` (`localhost:14333`), theo **bộ khung đối tác mới**.
**Không sửa cái nào trong đợt 17/09** — đây là lỗi dữ liệu/cấu hình hoặc nợ kỹ thuật.

| | Nội dung | Bằng chứng |
|---|---|---|
| **P3** | 🟢 **Đã xử lý (18/09/2026)**: đường nạp `ShareDataPacketField` đã gỡ bỏ hoàn toàn khỏi luồng GỬI (`LoadPacketFields`, `PacketFields`, `declaredFields`, `fieldsDict`, `fieldMeta` không còn tồn tại). Trước đó: `LoadPacketFields(db, packet.Code)` lọc `DatatypeId` theo Code nên luôn trả 0 dòng, `fieldMeta` luôn `null`. | Đã gỡ trọn vẹn; 0 grep match trong `DataOutbound` |
| **P1** | `fromLocationMet: 0` và `averageSpeed: 45.5` trong bộ khung là **số cứng**, không có `$field` ⇒ mọi dòng gửi hằng số, dữ liệu thật bị bỏ | `RenderShapeNode` `case JsonValueKind.Number` trả literal |
| **P2** | Bộ mã `condition` chỉ có `normal→"0"` · `slow→"1"`, **cả hai `isDefault: false`**. Dữ liệu thật có **`conges` 5 dòng** không khớp ⇒ nhả nguyên `"conges"` + cảnh báo mỗi chu kỳ. 🟢 **ĐÚNG THIẾT KẾ** theo chốt 11/09 — thiếu ở **cấu hình bộ mã**, không phải lỗi mã. `conges` sẽ nhận **`defaultSourceValue`** khi bộ mã `condition` chuyển sang cấu trúc mới và có khai khoá đó | `TmsZoneStatus.Condition`: normal 126 · slow 21 · **conges 5** |
| **P4** | `defaultValue: "Default"` **không bao giờ chạy** — cả **152/152 dòng** đều có `Condition` | như trên |

🟢 **P3 đã được xử lý trọn vẹn (18/09/2026):** Đường nạp `ShareDataPacketField` đã được gỡ bỏ hoàn toàn khỏi luồng GỬI đúng theo chốt kiến trúc. `TargetShapeJson` (và cấu hình `$extend`) trở thành nguồn cấu hình DUY NHẤT. Các cấu trúc `LoadPacketFields`, `PacketFields`, `declaredFields`, `fieldsDict`, `fieldMeta` và nhánh phẳng cũ lặp trên `declaredFields` đã được xoá sạch khỏi mã nguồn luồng Gửi.

## 3.2 Bộ khung thật dùng `$extend` rất mỏng ⚠️

Phễu lọc chiều Gửi đang sống (`TEST_101COMMONDATA_OUT`, gói `101_commonData`):

| Đo | Kết quả |
|---|---|
| Số trường bộ khung gọi | **16** |
| Số trường **có** `$extend` | ⚠️ **1 / 16** — chỉ `trafficCondition` |
| Khoá đang dùng | `targetType: "string"` · `codeSet: "condition"` · `defaultValue: "Default"` |
| `required` · `format` · `expression` | **0 / 16** |

**Kẹt ở đâu:** cơ chế đã đủ, **thiếu dữ liệu**. Mọi trường trừ `trafficCondition` đang đi qua không ép
kiểu, không lớp chặn, không quy đổi. Đây là **việc điền dữ liệu, thuộc bên giao diện/module** — nguồn
để điền đã có sẵn trong `ShareDataPacketField.Type` và `.IsRequired`.

Ghi nhận thêm từ bộ khung thật:

- **Không dùng `$each`.** `data` là mảng chứa **một object khuôn**, nên engine nở N bản ghi dưới
  **một** header. ⚠️ Nhưng vẫn **đi nhánh gộp** (`RenderShapeAggregate`): `ShapeHasRepeatBlock` nhận
  mảng khuôn bản ghi là khối lặp, không cần `$each`. Vì vậy đầu ra gói 101 là **object**
  `{header, data:[…]}`, **không phải** mảng bản ghi — đây là chỗ **SV-1** sẽ bóc. Xem mục 1.3.
- **Cùng một trường dùng ở hai khoá**: `dataTime` xuất hiện cả ở `header` lẫn trong `trafficInfo`.
  Đúng thiết kế *"luật gắn theo vị trí, không theo trường"* — nhưng nghĩa là `$extend.format` phải
  **khai ở cả hai chỗ**, khai một chỗ thì chỗ kia vẫn ISO-8601.

## 3.3 Thiếu trường Meta hệ thống 🔴

Tầng ánh xạ **không có khái niệm** thời điểm hiện tại, số thứ tự bản tin, mã đối tác. Bộ khung hiện
khai `header.requestId`, `messageType`, `version`, `source` là **hằng số**.

**Chốt 16/09** (review frontend) — đã có tên quy ước và đã giao việc:
> *"Các trường dữ liệu mang tính đặc thù hệ thống như thời gian hiện tại (`meta.now` / `DateTime.Now`),
> mã yêu cầu (`request_id`), thông tin chứng thực… **phải được xử lý tự động trong code Backend**.
> Trên giao diện, gom vào nhóm riêng **'Trường Meta hệ thống'** để người dùng chọn nhanh."*

Đầu việc giao **Đạt (BE)**: *"Đảm bảo Backend **tự động inject** giá trị thời gian hệ thống vào các
trường meta khi gửi/nhận."*

**Kẹt ở đâu:** khoá nào đối tác đòi thời điểm gửi hoặc số hiệu bản tin thì hiện **không điền được**.

⚠️ Liên quan **mã đối tác**: chốt 16/09 (định danh đối tác) yêu cầu `partnerCode` đi **trong body/
metadata** của gói tin — *"gán cái mã A101 đó vào trong cái trường define cái đối tác, để lúc em nhận
em mới biết được gói này của A101"*. Hiện bộ khung không có chỗ nào sinh giá trị này.

### 🔴 Cú pháp đã có trên staging, worker chưa hiểu (đo 18/09)

> **Hết chờ CÚ PHÁP — nhưng còn chờ Hiếu chốt CÁCH GIẢI.** Xem 6 câu ở cuối mục này.

Tra `mssql_staging` (**nguồn sự thật**) thì cú pháp meta **đã được khai rồi**:

```json
"header": {
  "source": "ITS-TMS",
  "dataTime":    { "$meta": "Now" },
  "partnerCode": { "$meta": "PartnerCode", "$value": "Test" },
  "packetCode":  { "$meta": "PacketCode",  "$value": "101_commonData" },
  "serial":      { "$meta": "Serial" }
}
```

| Khoá staging dùng | Worker hỗ trợ |
|---|---|
| `$meta` (`Now` · `PartnerCode` · `PacketCode` · `Serial`) | 🔴 **0 dòng mã** |
| `$value` | 🔴 **0 dòng mã** |

🔴 **Đây là lỗi ĐANG CHẠY, không phải việc chờ.** `RenderShapeAggregate` chỉ xử lý node có `$field`;
node không có thì nó duyệt từng thuộc tính rồi **nhả nguyên văn** ⇒ đối tác đang nhận
`"dataTime": {"$meta":"Now"}` thay vì mốc thời gian thật.

⚠️ **`$value` KHÔNG phải nguồn giá trị** — nó chỉ là giá trị giao diện hiển thị cho người khai xem
trước. Worker phải đọc `$meta` và tự giải. Lấy `$value` làm nguồn là **đặt giá trị cứng**, đúng thứ
chốt 16/09 cấm: token phải được **worker giải lúc gửi**, nếu không mọi lô sẽ mang **cùng một mốc thời
gian** — đúng lỗi **P1** đang mắc với `fromLocationMet: 0` và `averageSpeed: 45.5`.

⚠️ Tầng render **không thấy `DataOutboundContext`** — `Transform`, `RenderShapeAggregate`,
`RenderShapeNode` chỉ nhận dòng dữ liệu + bộ khung + bộ mã. Phải luồn giá trị meta xuống, bằng **tham
số tuỳ chọn đặt CUỐI** để **19 lời gọi `Transform(`** trong test không phải sửa.

### 🟢 ĐÃ CHỐT — bản bàn giao `TargetShapeJson.md` (Hiếu, 18/09)

Sáu câu từng treo ở đây **đã có đáp án**:

| # | Câu hỏi | Đáp án | Mục |
|---|---|---|---|
| **1** | `$value` để làm gì? | **Bản chụp** FE ghi sẵn lúc lưu hồ sơ. `$meta` là **nguồn sự thật**; service dùng `$value` cho nhanh **hoặc** tra lại — cả hai đều hợp lệ | §3.5 |
| **2** | Danh sách token đầy đủ? | Đúng **4**: `Now` · `Serial` · `PacketCode` · `PartnerCode`. 🔴 **`$meta` không bao giờ kèm `$extend`** — gặp là dữ liệu hỏng | §3.5 · §10 |
| **3** | Meta ở `header` hay xuống `data`? | **Ở `header`, giữ nguyên.** `header` **không phải vỏ của mình** — nó là khuôn đối tác. Hiếu: *"nó chỉ là tên của json bọc ngoài thôi"* | §2 · §9.3 |
| **4** | `numberFormat`/`dateFormat` thay `format`? | **Thay hẳn.** Không có khoá `format`. `dateFormat` 2 chiều · `numberFormat` chỉ GỬI. `expression` **đã chết** | §3.4 |
| **5** | `DatatypeId` GUID hay mã? | **`ShareDataPacket.ID` (GUID)**. Danh mục `shareData_type` cũ đã bỏ | §1 · §10 |
| **6** | `PartnerCode` là mã của ai? | **`ShareDataPartner.Code` của hồ sơ** ⇒ `ctx.Partner.Code` | §3.5 |

🔴 **`Now` và `Serial` KHÔNG có `$value`** — service **tự sinh lúc gửi**. Anh Đạt nhấn 18/09:
*"`dataTime`/`Now` và `seq`/`Serial` là dữ liệu mình tự sinh gửi qua."* Gặp `$value` ở hai token này
là dữ liệu hỏng.

#### 🔴 Câu 6 — hai đầu hiểu ngược nhau

| Đầu | Hiểu `PartnerCode` là | Bằng chứng |
|---|---|---|
| **Gửi** | mã **bên nhận** | `DataOutboundRestSender`: `partnerCode = partner.Code` — `ctx.Partner` là đối tác mà đăng ký này gửi TỚI |
| **Nhận** | mã **bên gửi** | `DataInboundService.ResolveContextAsync` tra `ShareDataPartner` theo mã trong gói, rồi lấy **đăng ký chiều NHẬN của đối tác đó** |

Chốt 16/09 đứng về phía bên nhận: *"gán cái mã A101 đó vào trong cái trường define cái đối tác, để lúc
em nhận em mới biết được **gói này của A101**"* — tức mã **bên gửi**.

⚠️ Nếu đúng là mã bên gửi thì giá trị phải lấy từ **`ShareData:SelfPartnerCode`** (**SV-4**), không phải
`ctx.Partner.Code`. Nhưng SV-4 thuộc **kỳ cuối** ⇒ **M1 cố ý giữ `ctx.Partner.Code`**, đúng y giá trị vỏ
`httpPayload` đang gửi hôm nay, nên **không phải bước lùi**. Ngữ nghĩa thật chốt ở kỳ cuối cùng **SV-3**.

📎 Prompt: `../Prompt/sharedata-worker-giai-meta-prompt.md` — ⏸ **đang hoãn**

## 3.4 Header bị nhân bản — cần đo lại

Tệp kết xuất cũ (08/09) có `payload` là mảng **299 phần tử**, mỗi phần tử `{header, data}` kèm đúng
một dòng ⇒ header nhân ra 299 bản, tệp phình 198 KB.

**Nhiều khả năng đã hết** vì bộ khung hiện tại không dùng `$each`. Theo test
`Transform_RecordTemplateArray_...`, đường này sinh **một header và nở N bản ghi data**.
Vẫn phải **đo lại** ở lần chạy tới.

**Tiêu chí nghiệm thu — chốt 09/09, Anh Sơn:**
> *"Câu truy vấn chạy ra 291 dòng thì khi mapping xong xuất ra kết quả cũng phải **đủ 291 dòng, không
> được thiếu hay duplicate**."*

⇒ Đếm số bản ghi trong `data[]` phải **bằng đúng** số dòng thô câu truy vấn trả về.

## 3.7 Lịch gửi — đã kẹp khung giờ `StartTime`/`EndTime` cho chu kỳ lặp 🟢

**Chốt 09/09 + 16/09 — hai chế độ:**

| Chế độ | Cấu hình | Ví dụ |
|---|---|---|
| **Chu kỳ lặp** | mỗi N giây/phút, **có thể giới hạn khung giờ** | mỗi 30 giây, chỉ trong 06:00–22:00 |
| **Theo ngày** | chọn **ngày trong tuần** + **mốc giờ cố định** | đúng 09:00 mỗi ngày, gửi tổng hợp hôm trước |

Đã **bỏ** hẳn *"gửi theo sự kiện"* và *"gửi 1 lần"* (09/09 — *"Gửi một lần để làm cái gì? Test thì
bấm nút test trên giao diện là xong"*).

### CSDL và mã nguồn đã đồng bộ

`ShareDataSubscription.ScheduleJson` của **cả hai** đăng ký đang sống (đo 18/09):

```json
{"Kind":"continuous","IntervalSeconds":30,
 "StartTime":"06:00","EndTime":"22:00",
 "DaysOfWeek":null,"StartDate":null,"EndDate":null,"DurationMinutes":null}
```

`DataOutboundScheduler.ComputeNextTimeRun` đọc khoá nào:

| Khoá | Đọc ở nhánh nào |
|---|---|
| `kind` | luôn |
| `startTime` · `daysOfWeek` · `startDate` · `endDate` | trong nhánh `daily` |
| `IntervalSeconds` | nhánh mặc định (`continuous`) |
| **`StartTime`** · **`EndTime`** | 🟢 **nhánh `continuous`** — kẹp kết quả vào khung `[StartTime, EndTime]` |
| `DurationMinutes` | ⏸ chưa chốt nghĩa, tạm chưa đọc |

**Quy tắc kẹp khung giờ cho `continuous` (đã xong 18/09):**

| Kết quả rơi vào | Trả về |
|---|---|
| **trong** khung `[StartTime, EndTime]` | giữ nguyên (`now + interval`) |
| **trước** `StartTime` | `StartTime` **hôm nay** |
| **sau** `EndTime` | `StartTime` **ngày mai** |
| `StartTime`/`EndTime` **không khai** hoặc sai định dạng | giữ nguyên (`now + interval`) — hành vi cũ |
| Khung qua đêm `StartTime > EndTime` | giữ nguyên (`now + interval`) — tạm bỏ qua để tránh rủi ro lệch lịch |

## 3.5 "Gửi khi có dữ liệu mới" — khả thi bằng polling + cursor 🟠

### Nguồn đã chốt và điều chỉnh 18/09

Anh Sơn, họp **09/09**:

> *"Bật: cứ khi nào DB phát sinh bản ghi mới là đẩy gói tin đi ngay. Tắt: chỉ gửi theo lịch trình
> định kỳ."*

Biên bản **16/09** làm rõ cơ chế phát hiện dựa `CreateTime`/`UpdateTime`. Sau khi đọc đủ 7 transcript
và tài liệu mapping 101–111, không có nguồn nào chốt mô hình "6 snapshot + MAX watermark + gửi lại
toàn bộ". Đó là giả định của code/prompt cũ, nay **bỏ**.

**Ngữ nghĩa S2 mới:**

- Không dùng NATS/CDC trong đợt này; cờ bật dùng polling theo `IntervalSeconds`.
- Cờ bật gửi **các dòng mới · cập nhật · soft-delete** sau cursor đã giao thành công.
- Cờ tắt chạy theo lịch; phạm vi all hay tăng dần vẫn theo policy nghiệp vụ của từng gói.
- Cờ thật do Hiếu/WebAPI cung cấp; worker đọc ở đúng một adapter, không dùng `Mode = Event` lâu dài.
- Không dùng `LastPayloadHash`.

### Ba policy theo tài liệu mapping

| Policy | Gói | Cờ tắt | Cờ bật |
|---|---|---|---|
| `FullOrChanged` | 101 · 102 · 105 · 108 | lấy all theo lịch | lấy phần mới/cập nhật/xoá |
| `AlwaysIncremental` | 103 · 104 · 106 · 107 · 109 | lấy sau cursor theo lịch | lấy sau cursor theo polling |
| `NotReady` / `Disabled` | 110 / 111 | 110 chưa đủ contract · 111 ghi `skip` | không tự chế dữ liệu |

Hardcode hiện tại có sai khác rõ với tài liệu: 102 trả `snapshot = NULL`; 105 bỏ nguồn In; 109 bỏ
nguồn In và hardcode giá `NULL`; 110 thiếu Weather/outbox; 111 đáng lẽ `skip` nhưng query Incident.
Không dùng comment `SNAPSHOT/INCREMENTAL` làm business truth.

### Chuẩn cursor chung

Mỗi query tự tính nguồn thay đổi nhưng trả alias thống nhất:

```text
__watermark = thời điểm thay đổi DB của dòng
__rowid     = khoá ổn định
__operation = upsert | delete
```

Ưu tiên audit time, không lấy thời gian nghiệp vụ làm cursor nếu có audit:

```sql
COALESCE(IsDelete, UpdateTime, CreateTime, <domainTime>)
```

Query JOIN phải lấy thời gian lớn nhất của mọi bảng đóng góp vào output. Cursor là cặp
`(LastTimeRun, LastDataId)`:

```sql
WHERE ChangeTime > @lastTime
   OR (ChangeTime = @lastTime AND ID > @lastId)
ORDER BY ChangeTime, ID
```

Không dùng `>= @lastTime` một mình; nó lặp/kẹt khi nhiều dòng cùng timestamp và có `TOP N`.

### Soft delete

- Query all: `WHERE main.IsDelete IS NULL`.
- Query changed: không lọc delete trước; dùng `IsDelete` làm `__watermark` và trả
  `__operation = 'delete'`.
- Xoá bảng join nhưng bản ghi chính còn sống là `upsert` của bản ghi chính.
- Nếu batch có delete mà `TargetShapeJson` không bind `$field: "__operation"`, huỷ batch và giữ
  cursor; không gửi delete như upsert.

### Field và persist

Không tính cờ do Hiếu cung cấp, S2 thêm đúng một field `ShareDataSubscription.LastDataId`; dùng lại
`LastTimeRun`. `ExecuteExportForSubscription` đã trả `LastId`, nhưng caller đang bỏ nó — phải nối vào
`TryPersistExportResult` và lưu cặp cursor trong cùng update có lease.

| Kết quả | Cursor | `SerialNbr` | `NextTimeRun` |
|---|---|---|---|
| không có dòng mới | giữ | giữ | tiến |
| mapping/HTTP lỗi | giữ | giữ | tiến để retry |
| HTTP thành công | lưu max watermark + row ID tương ứng | +1 | tiến |

### Lịch không NATS

- Gỡ hardcode `Mode == Event → 5s` và `EventPollIntervalSeconds`.
- Cờ bật: poll `now + IntervalSeconds`; đây là nhịp kiểm tra, không phải gửi vô điều kiện.
- Cờ tắt: giữ continuous/daily hiện hữu.
- Lần đầu bật: API khởi tạo `LastTimeRun` bằng thời điểm bật và `LastDataId = null`; không dùng năm
  1900 rồi backfill toàn bộ lịch sử.

Chi tiết thực thi và test: `../Prompt/sharedata-event-gui-khi-co-du-lieu-moi-prompt.md`.

## 3.6 Chưa chạy từ 15/09

Nhật ký hoạt động dừng ở 15/09. Cấu hình đường dẫn xuất và địa chỉ đối tác **đã sửa**, nên chạy lại
được — nhưng **chưa có tệp mốc nào theo hành vi mới** để so.

---

# 4. Việc còn lại

| # | Việc | Gỡ mục | Đổi hành vi | Trạng thái |
|---|---|---|---|---|
| **A1** | **Lỗi ghi tệp không được chặn gửi API** — bỏ `return` sớm; `Failed` chỉ khi **không kênh nào** thành công | 0 · D | **có** | 🟢 **xong 18/09** · ⚠️ **A2 thay bảng quyết định của nó** |
| **A2** | **HTTP quyết trạng thái · ghi tệp im lặng** — luôn gọi HTTP kể cả thiếu `EndPointApiUrl`; bỏ `ESH-1401`; `isSuccess = apiOk` | 0 · D | **có** | 🟢 **xong 18/09** |
| **6b** | **Gỡ đường nạp `ShareDataPacketField`** — bỏ `LoadPacketFields`, `PacketFields`, `declaredFields`, `fieldsDict`, `fieldMeta`, nhánh phẳng | 3.1 · P3 | không³ | 🟢 **xong 18/09** · `TargetShapeJson` thành nguồn cấu hình DUY NHẤT |
| **C1** | **Bộ mã đọc được cấu trúc `ValuesJson` mới** + chuyển `PacketJsonParser` sang `Core/Common/Parsing` | 2.4 | không¹ | 🟢 **xong 18/09** |
| **S1** | **Khung giờ `StartTime`/`EndTime` cho chế độ `continuous`** — kẹp lần chạy tiếp vào khung [StartTime, EndTime], hết gửi 24/7 | 3.7 | **có** | 🟢 **xong 18/09** |
| **S2** | ⚠️ **Cờ "gửi khi có dữ liệu mới"** — polling + cursor `(LastTimeRun, LastDataId)` theo từng gói | 3.5 | **có** | ⏸ **chờ cột cờ thật từ Hiếu/WebAPI** · = **SV-12** bên MasterPlan |
| **M1** | 🔴 **Worker giải `$meta`** — `Now` · `Serial` · `PacketCode` · `PartnerCode`. Staging **đã khai**, worker **0 dòng** ⇒ đang nhả token thô cho đối tác | 3.3 | **có** | ⏳ **prompt sẵn sàng** — `worker-giai-meta` · kỳ 1 |
| **N1** | 🔴 **Đồng bộ tầng ánh xạ với bản bàn giao** — 4 khoá `$extend` worker đọc sai tên + đổi thứ tự biến đổi + qua bộ mã thì không ép kiểu | 1.2 · 2.1 | **có** | ⏳ **prompt sẵn sàng** — `dong-bo-extend-theo-ban-giao` |
| **SV-1** | 🔴 **Bỏ vỏ `httpPayload` 7 khoá** — gửi thẳng `FinalBytes`, **giữ cả `header` lẫn `data`** | 1.3 | **có** | ⏳ **prompt sẵn sàng** — `bo-vo-httppayload` · **kỳ 1**, chạy sau M1⁵ |
| **M2** | **Log 2 bước cha–con** — 1 log cha + Step 1 *trích xuất* → Step 2 *xử lý & gửi* | — | có | ⏸ **chờ module ShareData** cấu trúc bảng trước, worker xử sau |
| **M3** | **Bảng mã lỗi trong cấu hình hệ thống** — nay `ESH-*` hardcode trong `ShareDataAlertCode.cs` | — | không | 🟡 **chưa chốt lưu ở đâu** — xem mục 5 |
| **M4** | **Format số thập phân** — `CoerceDataType` hiện **chỉ đổi kiểu, không định dạng**; tham số `format` **không tới nhánh số**, chỉ vào `FormatDateTime`. ⚠️ Staging đã khai **`$extend.numberFormat`** và **`$extend.dateFormat`**, worker chỉ biết `format` ⇒ **cả hai đang bị bỏ qua âm thầm** | 1.2 | có | 🔴 chốt **11/09**, chưa làm² |
| 7 | **Phép gộp cấp tập dòng** | 2.3 | có | **khoan làm** |
| 9 | **Gửi khi có dữ liệu mới** | 3.5 | có | chờ — **cột cờ thuộc người khác** |
| **C2** | **Bỏ hẳn `ESH-1205`** — gỡ khối đối chiếu tên bộ khung ↔ khoá dòng thô; trường thiếu ra `null` im lặng | 0 · C · 2.2 | không⁴ | 🟢 **xong 18/09** |
| — | **Điền `$extend`** cho 15 trường còn lại | 3.2 | có | **bên giao diện/module lo** |
| — | **Chốt 6 trường đặc tả đòi mà CSDL chưa có** | 5 | có | chờ — quyết định nghiệp vụ |

### 🔢 Prompt S2

`event-gui-khi-co-du-lieu-moi` đã được viết lại theo polling + cursor từng gói và đang **chờ cột cờ
thật từ Hiếu/WebAPI**. Không thực thi bằng `Mode = Event` tạm thời.

🟢 Năm việc **A2 · C1 · C2 · S1 · 6b** đã **xong 18/09**, prompt đã xoá theo quy ước Auto-Cleanup.
S2 dùng pipeline/lease hiện hữu và `TargetShapeJson` của **6b**, nhưng scheduler khi cờ bật dùng
`IntervalSeconds` làm nhịp thăm dò; không còn giả định `Event` là lịch gửi giống Periodic.

¹ C1 **không đổi hành vi hôm nay** — CSDL còn cấu trúc cũ, nhánh cũ giữ nguyên. Nó là **chặn trước**.

⁴ C2 **không đổi đầu ra một byte** — 6 trường vẫn ra `null` y như cũ. Chỉ thôi ghi cảnh báo kèm theo.

⁵ 🔴 **SV-1 — hướng đã chốt, chỉ còn chặn kỹ thuật.** Anh Sơn (biên bản định danh đối tác, `01:48`):
*"Bỏ cái vỏ, chỉ lấy ở trong thôi… cấu trúc dữ liệu trả về cho đã rồi tự nhiên ngồi làm thêm cấu trúc
ở ngoài, xong rồi lại phải đi thống nhất ở bên ngoài nữa."* và `02:20`: *"mấy thông tin version mấy
thông tin gì không có liên quan gì hết"* — trong khi vỏ hiện tại mang đúng `packetVersion` và `pduType`.
Anh Đạt chốt 18/09: *"bỏ, chỉ tận dụng phần rawJSON — đẩy cục đó qua."*

**Thân bản tin đích — LUÔN là mảng bản ghi:**

```json
[ { "zoneId": "Z01", "trafficCondition": "0", "dataTime": "2026-09-18T10:00:00" }, { "zoneId": "Z02", … } ]
```

Anh Đạt làm rõ 18/09: *"chỉ lấy nội dung cục `data:{ }` bên trong, gửi qua HTTP"* — tức
`DataOutboundContext` là **đồ nội bộ**, không lên đường truyền; thân HTTP = đúng phần dữ liệu.

🔴 **`FinalBytes` hôm nay có HAI dạng**, phải quy về một:

| Bộ khung | `FinalBytes` | Thân HTTP |
|---|---|---|
| **Có khối lặp** — 🟢 **gói 101 đang dùng** | object `{ "header": {…}, "data": [ …N… ] }` | **bóc lớp**, chỉ gửi mảng `data` — **bỏ `header`** |
| Phẳng — không khối lặp | mảng `[ {...}, {...} ]` | gửi **thẳng** |

⚠️ **Khối lặp ≠ `$each`.** Gói 101 **không có `$each`** ở đâu cả, nhưng vẫn vào nhánh gộp vì
`"data": [ { …có `$field`… } ]` là **mảng khuôn bản ghi**. Bỏ `header` là **chọn có chủ đích** để bên
nhận chỉ phải parse một dạng. Chi tiết và ví dụ đầu ra thật: mục **1.3**.

**Hai điều kiện tiên quyết:**

| # | Điều kiện | Trạng thái |
|---|---|---|
| 1 | **M1 xong** — worker giải được `$meta` | ⏸ prompt đang hoãn, chờ Hiếu |
| 2 | **4 khoá meta đã chuyển từ `header` xuống khuôn bản ghi trong `data`** | 🔴 **chưa** — staging vẫn để ở `header` |

Điều kiện 2 là **sửa cấu hình bộ khung, không phải sửa mã** — thuộc bên giao diện/module. Chạy SV-1 khi
4 khoá còn ở `header` thì **mất sạch định danh đối tác**. Không có đường tắt: anh Sơn đã gạt phương án
header HTTP — *"không truyền thông tin đối tác vào header HTTP nếu cấu trúc gói tin qua socket/broker
yêu cầu dữ liệu nằm trọn trong payload"*.

⚠️ Cục JSON là **mảng dòng** ⇒ 4 khoá meta **lặp ở mọi dòng**. Bên nhận đọc dòng đầu — có cần kiểm các
dòng còn lại đồng nhất không thì **chưa chốt**, xem mục 5.

📌 Phép bóc `data` đặt ở **tầng gửi** (chốt 18/09) — tệp kết xuất local giữ nguyên `{header, data}`.

🔗 **Phải đổi cùng nhịp với chiều NHẬN** (`SV-1b` bên MasterPlan): `DataInboundService.Parse.cs:52-64`
đang **bắt buộc** khoá `payload`. Đổi một chiều là gãy luồng. Chiều nhận thuộc **Hiếu**.

² ⚠️ **Cú pháp meta phải là TOKEN worker giải, không phải giá trị cứng.** Đặt cứng trong
`TargetShapeJson` thì mọi lô mang **cùng một mốc thời gian**, đúng lỗi **P1** đang mắc với
`fromLocationMet: 0`.

🔴 **Cú pháp đã có trên staging từ 18/09** — `{"$meta":"Now"}`, `{"$meta":"PartnerCode"}`… Hết chờ.
Nhưng nó đi kèm khoá `$value` (vd `{"$meta":"PartnerCode","$value":"Test"}`): **`$value` chính là cái
bẫy "giá trị cứng"** mà chốt trên cảnh báo. Worker **đọc `$meta`, bỏ qua `$value`**. Xem mục 3.3.

² Cụ thể: `"decimal" or "number"` → `Convert.ToDecimal(...)` — **đổi kiểu, không làm tròn**.
`averageSpeed = 45.6789` ra nguyên `45.6789`; muốn `45.68` thì **hiện không có cách nào**.
🔎 Ghi nhận kèm: mã `ESH-1204 IntegerPromoted` (*"số nguyên bị nâng lên decimal sau quy đổi đơn vị"*)
**đã khai nhưng 0 chỗ dùng** — người viết trước đã lường trước, chưa nối dây. Giữ, không xoá (rule 19.6).

³ Không đổi hành vi **hôm nay**, vì theo P3 `fieldMeta` đã luôn `null`. Nhưng vỡ **19 lời gọi
`Transform(`** và **43 chỗ dùng `PacketFieldDto`**, nằm ở **hai** tệp kiểm thử:

| Tệp | `Transform(` | `PacketFieldDto` |
|---|---|---|
| `tests/ShareData/Services/DataOutboundServiceTests.cs` | 13 | 36 |
| `tests/ShareData/Services/DataOutboundMappingTests.cs` | 6 | 7 |

> 🟢 **Vừa xong 17/09** — xem mục 7: đổi tên `DataOutbound` · gỡ 6 dòng `NULL AS`.
> Khoảng trống 6 trường là **có thật**, trước bị `NULL AS` che đi. Dữ liệu gửi đi **không đổi**,
> 6 trường vẫn ra `null` — và từ **18/09 thì im lặng**, không còn cảnh báo kèm theo.

## Số đo tham chiếu — `mssql_dev` (local), 17/09/2026

| Đo gì | Kết quả |
|---|---|
| Lược đồ local so với mã của Hiếu | 🟩 **đã đồng bộ** — `ShareDataPacketField.GroupName` và `ShareDataPacket.Status` đều có |
| Phễu lọc **đang sống** | **1 chiều Gửi** (`TEST_101COMMONDATA_OUT`, `Direction = 0`) · 1 chiều Nhận · 2 bản ghi đã xoá mềm |
| Gói tin có phễu lọc | **chỉ `101_commonData`** |
| `ShareDataPacketField` theo gói | 101→**18** · 107→16 · 109→15 · 104→15 · 108→13 · 106→12 · 105→11 · 102→11 · 103→11 · 110→10 |
| `PKT111` | ⚠️ **0 dòng** — chưa khai trường nào |
| `ShareDataMapping.Format` | **cột đã tồn tại**, giá trị `null` |

---

# 5. Chưa chốt

| Vấn đề | Lựa chọn |
|---|---|
| **Sáu trường đặc tả đòi mà CSDL chưa có** (`routeName` · `roadType` · `roadAuthority` · `pavementType` · `laneCount` · `shoulderWidth`) | bỏ khỏi bộ khung, hay bổ sung cột thật vào CSDL. **Đây là quyết định nghiệp vụ, không phải việc của mã** — dù chốt cách nào thì hôm nay chúng vẫn ra `null` im lặng |
| **Bộ mã `laneId`** | Tầng 1 (`fieldMeta.CodeSetCode`) đã bỏ hẳn khỏi mã nguồn luồng GỬI, chỉ còn `$extend.codeSet`. Bộ khung phễu lọc đang sống không khai khoá này ⇒ mất quy đổi (hoặc cần bổ sung `$extend.codeSet: "laneId"` vào bộ khung trên giao diện/CSDL). |
| **Bộ mã mặc định cấp gói tin ở đâu** | thêm cột vào `ShareDataPacketField`; hay chấp nhận khai lặp từng đối tác; hay thêm cột **chỉ làm gợi ý** cho giao diện, worker vẫn chỉ đọc `$extend` |
| **Chia nhỏ gói khi vượt kích thước tối đa** | chuẩn ISO yêu cầu, mã không có — có làm không |
| ~~**Hợp đồng luồng GỬI đang nằm HAI NƠI**~~ | 🟢 **ĐÃ XONG 18/09** — Đã chuyển toàn bộ `IDataOutboundSender`, `DataOutboundSendResult`, và `DataOutboundContext` sang **`ShareDataWorker.Core`**; toàn bộ hợp đồng và models của 3 bước Outbound nay nằm trọn vẹn tại Core |
| **Giá trị mặc định của bộ mã — phân chiều gửi/nhận** | Chốt **16/09**: *"dữ liệu mặc định thì xem coi là dữ liệu chiều nào"*. Hiện `CodeValueDto.IsDefault` **không phân chiều** — một giá trị dùng chung cả hai. Tách chiều, hay chấp nhận dùng chung? |
| **Cờ chỉ-gửi / chỉ-nhận + clone service** | Chốt **16/09** để test tải đa đối tác: clone worker thành A1…A4 *chỉ gửi* + 1 instance *chỉ nhận*, dùng chung CSDL; không khai gì thì mặc định hai chiều. **Hoãn có chủ đích** — làm **luồng 1-1 cho đúng trước đã**. Chốt lại thời điểm sau |
| **Bảng mã lỗi lưu ở đâu** | Tra 18/09: **`SysConfig`** có tiền lệ thật đang chạy (`Code`/`Value`/`GroupCode`, 6 dòng nhóm `WebConfig`) · **`SysDictType` + `SysDictData`** là bảng từ điển 2 cấp đúng vai trò nhưng **rỗng hoàn toàn, chưa ai dùng**. Chọn cái nào? |
| **`MaxLen` cho kiểu String — có làm không** | Chốt 11/09 có nhắc, nhưng tra 18/09 thì **chưa có gì đứng sau**: không cột lưu trong `ShareDataPacketField`, không khoá `$extend`, không mã kiểm tra, **không đối tác nào đòi**. Rủi ro độ dài có thật nhưng ở **chiều NHẬN** (cảnh báo 4000 ký tự bị cắt, 16/09). Có làm ở chiều gửi không, và cho trường nào? |
| ~~**Giới hạn khung giờ cho chế độ chu kỳ**~~ | 🟢 **ĐÃ XONG 18/09** (việc **S1**) — `continuous` nay kẹp vào `[StartTime, EndTime]`. Còn **một vế bỏ ngỏ có chủ đích**: ca qua đêm `StartTime > EndTime`. Xem mục 3.7 |
| **`partnerCode` lặp ở mọi dòng — bên nhận kiểm tới đâu?** | Sau **SV-1**, cục JSON là mảng dòng và `partnerCode` (qua M1) sẽ lặp ở từng dòng. Bên nhận đọc **dòng đầu** — nhưng có kiểm các dòng còn lại cùng giá trị không, và lệch thì xử sao: bỏ cả lô, hay lấy dòng đầu rồi ghi cảnh báo? Chiều nhận thuộc **Hiếu**, cần chốt cùng lúc với SV-1 |
| ~~**`DatatypeId` hai môi trường khác kiểu**~~ | 🟢 **ĐÃ CÓ ĐÁP ÁN** — bản bàn giao §1+§10: `DatatypeId` là **`ShareDataPacket.ID` (GUID)**, danh mục `shareData_type` cũ đã bỏ. ⚠️ `ResolveActivePacket` hiện **chỉ tra theo `Code`/enum/OrderNo, không có nhánh tra theo ID** — nhưng nhật ký staging 17–18/09 **không hề có** `ESH-1301`/`ESH-1304`, nên **chưa kết luận được là gãy**. Cần quan sát thêm khi worker chạy lại trên staging |
| 🔴 **`$extend.required` còn dùng không?** | Không có trong bảng **6 khoá** §3.4 bản bàn giao, nhưng worker đang đọc (`DataMappingProcess.cs`) và việc 4 ngày 17/09 đã làm. §8 xếp *"trường bắt buộc chưa được ánh xạ"* vào nhóm **BE chưa kiểm, service tự phòng** — tức nguồn phải là `ShareDataPacketField.IsRequired`, mà đường nạp đó **đã bị 6b gỡ hết** 18/09. ⇒ Cờ bắt buộc hiện **không có nguồn nào cấp dữ liệu**. FE còn sinh `required` không, hay bỏ hẳn `ESH-1202`? |
| **Ba kiểu dữ liệu thành mã chết sau 6b — xoá hay giữ?** | `PacketJsonParser.ParseFields` **không còn ai gọi**; `PacketQueryResult` **0 nơi dùng**; `PacketFieldDto` chỉ còn hai thứ chết trên dùng. Giữ theo rule 19.6 nhưng cần chốt: dọn hẳn, hay để dành cho luồng NHẬN dùng sau? ⚠️ Kèm theo: chú thích XML `DataInboundService.cs:22` ghi *"Dùng lại các hàm quy ước chung của luồng gửi (ParseFields, ReadShape…)"* — **nay sai sự thật**, luồng nhận không còn gọi `ParseFields` |

---

# 6. Ngoài phạm vi

- Không đụng luồng nhận.
- Không đổi câu truy vấn hardcode — đã chốt giữ trong mã.
- ~~**Không đổi THÂN BẢN TIN của `DataOutboundRestSender`**~~ — 🔴 **DÒNG NÀY ĐÃ SAI, gỡ 18/09.**
  Bản cũ ghi 7 khoá là *"hợp đồng nội bộ đã thống nhất"* và cho rằng *"bỏ cái vỏ"* chỉ nhắm vào phong
  bì PDU. **Không đúng** — đó là diễn giải của người viết, không phải chốt. Thân bản tin **phải bỏ vỏ**,
  xem mục 4 **SV-1**.
- Không xoá mã trở thành không dùng (rule 19.6).
- **Không sửa P1 · P2 · P4** — lỗi dữ liệu, bên cấu hình lo.

---

# 7. Nhật ký thay đổi

## Nguồn chốt — quyết định nào từ buổi họp nào

Tra ở đây trước khi hỏi lại. Bản ghi đầy đủ ở `../doc/transcript/`.

| Quyết định | Buổi họp | Mục trong tài liệu này |
|---|---|---|
| Gửi thẳng payload qua API là kênh giao hàng · tệp là lưu vết local | **09/09** review sharedata | 0 · D · mục 4 A1 |
| Bí danh sau `AS` là hợp đồng giữa truy vấn và bộ khung | **09/09** | 0 · B3 · C2 |
| Bỏ *"gửi theo sự kiện"* và *"gửi 1 lần"* · thêm *"gửi khi có dữ liệu mới"* | **09/09** + 16/09 | 3.5 |
| Lịch gửi 2 chế độ: chu kỳ lặp · theo ngày + mốc giờ cố định | **09/09** + 16/09 | 3.7 |
| Chu kỳ lặp **có thể giới hạn khung giờ** (`StartTime`/`EndTime`) | **16/09** review frontend | 3.7 · mục 4 S1 |
| So khớp **không phân biệt hoa/thường** (`OrdinalIgnoreCase`) | **09/09** | 0 · C2 |
| *"Vào bao nhiêu dòng, ra đúng bấy nhiêu dòng"* | **09/09** | 3.4 |
| `DataType` chuẩn hoá: String (+`MaxLen`) · Number · DateTime (+format) · Boolean | **11/09** script | 1.2 · mục 4 M5 |
| Bộ mã: không khớp thì dùng `DefaultValue` **hoặc giữ nguyên giá trị gốc** | **11/09** | 2.2 · 3.1 P2 |
| Phép gộp: `SUM` · `AVG` · `MIN` · `MAX` · `COUNT` | **11/09** | 2.3 |
| Format: ngày giờ **và số thập phân** | **11/09** | 1.2 · mục 4 M4 |
| Pipeline 3 process · ngữ cảnh xuyên suốt · cô lập lỗi tại Mapping | **16/09** refactor worker | 0 |
| **Bỏ phong bì PDU** · **bỏ rẽ nhánh version** | **16/09** định danh đối tác | 1.3 |
| `partnerCode` đi **trong body/metadata** của gói tin | **16/09** định danh đối tác | 3.3 · mục 4 M1 |
| Cờ chỉ-gửi / chỉ-nhận + clone service test đa đối tác | **16/09** định danh đối tác | mục 5 — **đã hoãn** |
| Trường Meta hệ thống `meta.now` · `request_id`, Backend tự inject | **16/09** review frontend | 3.3 · mục 4 M1 |
| Nút **"Tự động ánh xạ"** — giao diện sinh `TargetShapeJson` | **16/09** review frontend | 1.1 |
| **Bỏ** tuỳ chọn *"Bỏ khoá này khi gửi đi"* (`$exclude`) ở chiều gửi | **16/09** review frontend | 1.2 |
| `Default Value` cho bộ mã, **phân chiều gửi/nhận** | **16/09** sửa UI | 2.2 · 2.4 |
| Checkbox *"gửi ngay khi có dữ liệu mới"* ánh xạ sang `Mode = Event/Periodic` | **MasterPlan mục A** (FE đã xong) | 3.5 · mục 4 S2 |
| Ngữ nghĩa mặc định bộ mã theo chiều (`ValuesJson` mới · chiều GỬI dùng `defaultSourceValue`) | **anh Đạt chốt 18/09** | 2.4 · mục 4 C1 |
| Ghi tệp **chỉ ở local/dev** và **không ghi log gì**; gửi HTTP **thành hay bại đều ghi log** | **anh Đạt chốt 18/09** | 0 · D1 · D2 |
| Không khai `EndPointApiUrl` → **cứ gọi HTTP, nó trả mã lỗi gì thì ghi log nấy** | **anh Đạt chốt 18/09** | 0 · D2 · mục 4 A2 |
| **Bỏ hẳn `ESH-1205`** — trường bộ khung gọi mà dòng thô không có thì ra `null` **im lặng** | **anh Đạt chốt 18/09** *(cảnh báo này vốn không có gốc từ buổi họp nào — người viết tài liệu tự thêm)* | 0 · C · 2.2 · mục 4 C2 |
| Log 2 bước cha–con | **16/09** sửa UI | mục 4 M2 |
| Bảng mã lỗi khai trong cấu hình hệ thống | **16/09** sửa UI | mục 4 M3 |

## Đã làm

| Ngày | Việc | Kết quả |
|---|---|---|
| 17/09 | **Việc 3** — đảo giá trị mặc định lên trước biểu thức | 🟢 · kèm bản sao dòng dữ liệu để biểu thức thấy giá trị mặc định · 79 pass |
| 17/09 | **Việc 8** — ngắt khi không tìm thấy phễu lọc | 🟢 `ESH-1304 MappingNotFound` · 80 pass |
| 17/09 | Commit `fa60436d` (Văn Hiếu) **xoá thực thể `ShareDataTable`** | worker gãy **8 lỗi `CS0246`** · cùng đợt thêm CRUD `ShareDataPacket`/`ShareDataPacketField`, cột `GroupName` và `Status` |
| 17/09 | **Gỡ cụm mã chết** — `BuildQuery`, `GetFullTableName`, 2 hàm `ResolveIncremental*`, 2 forwarder | 🟢 `PacketQueryBuilder` → **`PacketJsonParser`** (chỉ còn `ParseFields` + `ParseCodeValues`) · **bản dịch xanh** |
| 17/09 | **Việc 6 (nửa đầu)** — `LoadPacketFields` đọc `ShareDataPacketField` thay `FieldsJson` | ⚠️ **18/09 gỡ hẳn đường này** (việc 6b) — khi đó nửa sau chưa làm · và theo **P3** đường này trả 0 dòng |
| 17/09 | **Việc 4** — đọc `$extend.required` + `$extend.format` | 🟢 cờ bắt buộc thành *hoặc–hoặc* |
| 17/09 | **Việc 5** — bỏ rào `if (!hasCodeSet)`, thêm nhánh `datetime` + `FormatDateTime` | 🟢 · ⚠️ **không đổi đầu ra hôm nay**: `trafficCondition` sau bộ mã vốn đã là chuỗi, và chưa trường nào khai `targetType: "datetime"` |
| 17/09 | **Việc 2** — đối chiếu tên bộ khung với khoá dòng thô | `ESH-1205 ShapeFieldNotInRawRow`, gom một lần mỗi chu kỳ · ⚠️ **18/09 GỠ LẠI** — rà 6 bản ghi họp thì cảnh báo này **không có gốc từ chốt nào**, do người viết tài liệu tự thêm. Anh Đạt chốt bỏ hẳn |
| 17/09 | **Việc 1** — bộ khung rỗng/hỏng cú pháp thì ngắt | 🟢 `ESH-1206 ShapeInvalid` · nhánh phẳng giữ nguyên, chỉ không với tới được |
| 17/09 | **Bù 6 bí danh `NULL AS`** vào `QueryPacket101` | 17/09 thêm → 17/09 **gỡ lại** theo chốt "có gì lấy đó" |
| 17/09 | **Gỡ lại 6 dòng `NULL AS`** | 🟢 `QueryPacket101` còn **13 bí danh có nguồn thật** · tệp kiểm thử **không phải sửa dòng nào** · 6 tên thiếu lộ ra — khoảng trống **có thật**, trước bị `NULL AS` che (⚠️ cảnh báo kèm theo đã gỡ 18/09). `NULL AS snapshot` gói 102 và `CAST(NULL AS …)` gói phí **giữ nguyên**, ngoài phạm vi |
| 17/09 | **Đổi tên `DataPublication` → `DataOutbound`** — 242 lượt / 27 tệp / 13 tệp đổi tên, trải **2 dự án** | 🟢 **XONG** · đối xứng với `DataInbound` · rename **đụng cả luồng NHẬN** (4 lời gọi thật trong `DataInboundService`) · còn đúng **1** chỗ giữ tên cũ **cố ý**: chú thích trong `ShareDataMapping.cs` — entity dùng chung, **cần báo chủ sở hữu** |
| 18/09 | **Gỡ phụ thuộc chặn — lỗi ghi tệp không còn huỷ lô; `Failed` chỉ khi không kênh nào thành công** | 🟢 Sửa Step 4 Transport trong `DataOutboundService`: ghi tệp hỏng báo `ESH-1401` mức Error nhưng không huỷ lô; tiếp tục gửi API; lô xuất bản thành công nếu tệp hoặc API thành công · Thêm 3 test kiểm thử trọn ma trận lỗi transport · ⚠️ **cuối 18/09 bị A2 thay**: `ESH-1401` bỏ hẳn, ghi tệp im lặng, trạng thái do HTTP quyết một mình |
| 18/09 | **Bộ mã đọc được cả hai cấu trúc `ValuesJson`; chiều GỬI dùng `defaultSourceValue` khi không khớp** | 🟢 Chuyển `PacketJsonParser` sang `ShareDataWorker.Core/Common/Parsing` dùng chung hai chiều; thêm `CodeSetDto` và `ParseCodeSet`; `MapCode` ưu tiên `directionDefault` → `IsDefault` → giá trị thô; gỡ 2 forwarder ở `DataOutboundService` · 119 pass |
| 18/09 | **Chế độ `continuous` nay tôn trọng `StartTime`/`EndTime`; trước đó gửi 24/7** | 🟢 Khung giờ `[StartTime, EndTime]` được kẹp khi tính `NextTimeRun` cho `continuous`; ca qua đêm `StartTime > EndTime` được bỏ qua (giữ nguyên candidate) để tránh rủi ro lệch lịch · Đã thêm 7 test kiểm thử khung giờ · 126 pass |
| 18/09 | **Việc 6b — Gỡ đường nạp `ShareDataPacketField`; `TargetShapeJson` thành nguồn cấu hình DUY NHẤT đúng như chốt** | 🟢 Xoá `DataOutboundExtractionProcess.Fields.cs`; bỏ `PacketFields` khỏi `DataOutboundExtractionResult`; bỏ `LoadPacketFields` forwarder; bỏ `declaredFields`, `fieldsDict`, 26 chỗ `fieldMeta`, và nhánh phẳng cũ trong `DataMappingProcess`; 125 pass, 1 skipped · 0 grep match |
| 18/09 | **Việc A2 — HTTP quyết trạng thái · ghi tệp im lặng** | 🟢 Gửi HTTP luôn thực thi và quyết định trạng thái duy nhất của lô xuất bản; ghi tệp im lặng hoàn toàn, không ghi ESH-1401; thiếu `EndPointApiUrl` vẫn gửi và ghi nhận lỗi thật; cập nhật 3 test + 1 test đối tác null |
| 18/09 | **Việc C2 — Bỏ hẳn `ESH-1205`, trường thiếu ra `null` im lặng** | 🟢 Bỏ khối đối chiếu và cảnh báo ESH-1205 tại `DataMappingProcess`; hằng số giữ theo Rule 19.6; viết lại 2 test khẳng định trường thiếu ra `null` và trường thừa bị bỏ qua · 126 pass |
| 18/09 | **Chuẩn hoá vị trí `PacketMetadataResolver` & gộp `SqlInjectionGuard`** | 🟢 Chuyển `PacketMetadataResolver` sang `ShareDataWorker.Core/Common/Resolvers/`; tích hợp `ValidateExpression` trực tiếp vào `DataMappingProcess.Expression.cs` và xoá file thừa `SqlInjectionGuard.cs`; 126 pass |
| 18/09 | **Chuẩn hoá hợp đồng luồng GỬI về `ShareDataWorker.Core`** | 🟢 Chuyển `IDataOutboundSender`, `DataOutboundSendResult`, `DataOutboundContext` sang `ShareDataWorker.Core`; xoá thư mục `Context/` thừa; `Transport/` chỉ còn 2 implementation File & REST; 126 pass |
| 18/09 | **Đối chiếu 30 tệp staged** (2.714 thêm / 2.645 xoá) | 🟢 **Không tìm thấy lỗi.** Cả 5 việc A2 · C1 · C2 · S1 · 6b đều qua **toàn bộ** phép tự kiểm của chính prompt: `hasApiEndpoint` 0 · `missingInRaw` 0 · `LoadPacketFields`/`PacketFields`/`declaredFields`/`fieldsDict`/`fieldMeta` **0/0/0/0/0** · `FileWriteFailed` và `ShapeFieldNotInRawRow` mỗi mã **chỉ còn hằng số** (đúng rule 19.6) · `DataOutboundRestSender` khớp đủ A1·A2·A3 |
| 18/09 | **Phát hiện kèm khi đối chiếu** | ⚠️ 3 kiểu dữ liệu thành **mã chết** sau 6b (`ParseFields`, `PacketQueryResult`, `PacketFieldDto`) + chú thích `DataInboundService.cs:22` **sai sự thật** → đưa vào mục 5 chờ chốt · ⚠️ mục 6 từng ghi sai rằng thân bản tin *"không đổi"* → **đã gỡ**, nay là việc **SV-1** |

## Đối chiếu tên tệp qua các lần tái cấu trúc

Tên **hiện hành** là cột phải. Hai cột trái chỉ để tra khi đọc tài liệu cũ hoặc lịch sử git.

| Đời 1 | Đời 2 | **Đời 3 — HIỆN HÀNH** |
|---|---|---|
| `Orchestration/DataPublicationService.cs` | `DataPublicationService.cs` | **`DataOutboundService.cs`** |
| `Orchestration/DataPublicationService.Validation.cs` | tách 3 | **`PacketMetadataResolver` (Core/Resolvers) · `PacketJsonParser` (Core/Parsing) · `DataMappingProcess.ValidateExpression`** |
| `Orchestration/DataPublicationService.Schedule.cs` | `Scheduling/DataPublicationScheduler.cs` | **`Scheduling/DataOutboundScheduler.cs`** |
| `DataPublicationContext.cs` | `Context/DataPublicationContext.cs` | **`ShareDataWorker.Core/Models/DataOutbound/DataOutboundContext.cs`** |
| `DataExtractionProcess*` | `DataPublicationExtractionProcess*` | **`DataOutboundExtractionProcess*`** |
| `ExtractionResult` | `DataPublicationExtractionResult` | **`ShareDataWorker.Core/Models/DataOutbound/DataOutboundExtractionResult`** |
| `FileExportSender` / `RestPacketSender` | `DataPublicationFileSender` / `DataPublicationRestSender` | **`DataOutboundFileSender` / `DataOutboundRestSender`** |
| `IPacketSender` / `SendResult` | `IDataPublicationSender` / `DataPublicationSendResult` | **`ShareDataWorker.Core/Interfaces/DataOutbound/IDataOutboundSender` / `ShareDataWorker.Core/Models/DataOutbound/DataOutboundSendResult`** |
| — | `DataPublicationWorker` / `IDataPublicationService` | **`DataOutboundWorker` / `IDataOutboundService`** |

Thư mục: `Infrastructure/Services/DataPublication/` → **`Infrastructure/Services/DataOutbound/`**.

### Đợt 17/09 & 18/09 còn DỜI CHỖ, không chỉ đổi tên

Các thành phần đổi cả **dự án** hoặc **thư mục** — tra ở đây nếu tìm không thấy:

| Kiểu | Trước | **Vị trí thật hiện nay** |
|---|---|---|
| `DataOutboundExtractionResult` | `DataOutbound/Extraction/` | **`ShareDataWorker.Core/Models/DataOutbound/`** |
| `DataMappingResult` | `DataOutbound/Mapping/` | **`ShareDataWorker.Core/Models/DataOutbound/`** |
| `DataOutboundSendResult` | `DataOutbound/Transport/` | **`ShareDataWorker.Core/Models/DataOutbound/`** |
| `DataOutboundContext` | `DataOutbound/Context/` | **`ShareDataWorker.Core/Models/DataOutbound/`** |
| `IDataOutboundExtractionProcess` | `DataOutbound/Extraction/` | **`ShareDataWorker.Core/Interfaces/DataOutbound/`** |
| `IDataOutboundSender` | `DataOutbound/Transport/` | **`ShareDataWorker.Core/Interfaces/DataOutbound/`** |
| `PacketJsonParser` | `DataOutbound/Mapping/` | **`ShareDataWorker.Core/Common/Parsing/`** *(dùng chung hai chiều)* |
| `PacketMetadataResolver` | `DataOutbound/Extraction/` | **`ShareDataWorker.Core/Common/Resolvers/`** *(pure domain metadata resolver)* |
| `SqlInjectionGuard` | `DataOutbound/Extraction/` | **Tích hợp trực tiếp vào `DataMappingProcess.Expression.cs`** *(xoá file thừa)* |

> ⚠️ Còn **đúng một** chỗ giữ tên cũ, **cố ý**: `Module.ShareData.Core/Entities/ShareDataMapping.cs`
> — chú thích nhắc `DataPublicationService`. Entity dùng chung do WebAPI sở hữu, **không tự sửa**;
> cần báo chủ sở hữu.

⚠️ **Tài liệu này KHÔNG ghi số dòng mã.** Cấu trúc đã đổi nhiều lần. Tìm theo **tên hàm**.

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

⚠️ Trước khi chạy: xác nhận connection string trong `tests` trỏ **local** (`127.0.0.1,14333`).
Thấy `10.10.8.30` → **huỷ ngay và báo lại**.

**Soi tệp kết xuất thật** trong `sharedata/send`:

| Kiểm | Kỳ vọng |
|---|---|
| 6 trường giữ chỗ | `null`, **không sinh cảnh báo nào** |
| `vehicleCount` · `fromLocationMet` · `averageSpeed` | không có trong đầu ra, **không** cảnh báo |
| `fromLocationMet` = `0`, `averageSpeed` = `45.5` ở **mọi dòng** | đúng **P1**, không phải lỗi mới |
| 5 dòng `conges` | nhả nguyên `"conges"` — đúng **P2** |
| Số phần tử `payload` | đo lại, kỳ vọng **một** header |




