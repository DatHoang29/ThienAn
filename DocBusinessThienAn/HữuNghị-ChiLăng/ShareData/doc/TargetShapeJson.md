# Bộ khung đầu ra `TargetShapeJson` — tài liệu bàn giao cho service

Phiên bản: 2026-09-18 · Phạm vi: `ShareDataMapping.TargetShapeJson` (hồ sơ ánh xạ / phễu lọc)

Tài liệu này mô tả **toàn bộ ký hiệu** trong bộ khung và cách service phải đọc bộ khung cho
**ba chiều**: Gửi (Outbound = 0), Nhận (Inbound = 1) và Cả hai (Both = 2).

---

## 1. Dữ liệu đầu vào của service

Một hồ sơ ánh xạ (`ShareDataMapping`) gồm các cột service cần:

| Cột | Ý nghĩa |
| --- | --- |
| `PartnerId` | Đối tác áp dụng hồ sơ |
| `DatatypeId` | **ID gói tin** (`ShareDataPacket.ID`, dạng GUID) — không phải mã gói tin |
| `Direction` | 0 = Gửi, 1 = Nhận, 2 = Cả hai |
| `PartnerSchemaJson` | JSON mẫu đối tác cung cấp, giữ nguyên văn, **chỉ để tham khảo** |
| `TargetShapeJson` | **Bộ khung** — thứ service đọc để chạy |
| `IsActive` | Mỗi (đối tác × gói tin × chiều) chỉ có một hồ sơ được bật |

Trường dữ liệu của gói tin nằm ở `ShareDataPacketField` (lọc theo `DatatypeId`), khoá tra là
`AliasFieldKey` — đúng chuỗi mà bộ khung ghi trong `$field`. `ShareDataPacketField.Type` là
kiểu nội bộ của trường, dùng cho chiều nhận.

---

## 2. Nguyên tắc chung của bộ khung

Bộ khung là **bản sao cấu trúc JSON của đối tác**: giữ nguyên mọi khoá, mọi cấp lồng nhau,
mọi mảng. Chỉ khác ở chỗ mỗi **lá** (giá trị nguyên thuỷ) được thay bằng một ký hiệu mô tả
giá trị đó lấy từ đâu và biến đổi thế nào.

Hai hệ quả quan trọng:

- **Luật gắn theo VỊ TRÍ, không theo trường.** Cùng một trường gói tin dùng ở hai khoá khác
  nhau thì mỗi khoá khai luật riêng — đối tác đòi cùng giá trị ở hai chỗ với hai đơn vị khác
  nhau vẫn làm được.
- **Một bộ khung dùng cho cả hai chiều.** Chiều gửi đọc bộ khung từ trong ra ngoài (điền giá
  trị vào khuôn), chiều nhận đọc từ ngoài vào trong (dùng khuôn làm bản đồ để bóc dữ liệu).

---

## 3. Bảng ký hiệu đầy đủ


### 3.2. `null` — khoá chưa ánh xạ

```json
"cameraName": null
```

Người dùng không chọn trường và cũng không gõ giá trị. **Gửi:** gửi `null` cho khoá đó (giữ
khoá, không bỏ khoá). **Nhận:** bỏ qua, không ghi gì vào bản ghi nội bộ.

### 3.3. `$field` — lấy giá trị từ trường gói tin

```json
"avgSpeed": { "$field": "averageSpeed" }
```

Giá trị của `$field` là `ShareDataPacketField.AliasFieldKey`. Đây là ký hiệu chính, chiếm đa số.

### 3.4. `$extend` — luật quy đổi của riêng khoá đó

Chỉ xuất hiện kèm `$field`, và chỉ khi có ít nhất một luật. Sáu khoá, tất cả đều không bắt buộc:

| Khoá | Ý nghĩa | Áp ở chiều |
| --- | --- | --- |
| `codeSet` | `ShareDataCodeSet.Code` — bảng quy đổi giá trị, xem mục 6 | cả hai |
| `targetType` | Kiểu dữ liệu **phía đối tác** (`string`, `int`, `dateTime`, `bool`…) | cả hai |
| `dateFormat` | Khuôn ngày **phía đối tác**, VD `dd/MM/yyyy HH:mm:ss` | cả hai |
| `numberFormat` | Khuôn số **phía đối tác**, VD `0.#` | chỉ khi GỬI |
| `defaultPartnerValue` | Giá trị gửi cho đối tác khi dữ liệu nội bộ rỗng | chỉ khi GỬI |
| `defaultSourceValue` | Giá trị lưu nội bộ khi đối tác không gửi giá trị | chỉ khi NHẬN |

```json
"measuredAt": {
  "$field": "dataTime",
  "$extend": { "targetType": "dateTime", "dateFormat": "dd/MM/yyyy HH:mm:ss" }
},
"trafficCond": {
  "$field": "trafficCondition",
  "$extend": { "codeSet": "TRAFFIC_COND" }
},
"deviceState": {
  "$field": "state",
  "$extend": { "defaultPartnerValue": "on", "defaultSourceValue": "1" }
}
```

**Phân biệt hai lớp mặc định.** `$extend.defaultPartnerValue` / `defaultSourceValue` **không
giống** hai trường cùng tên trong bộ mã, dù tên trùng nhau:

| | Mặc định của **bộ mã** | Mặc định của **lá** (`$extend`) |
| --- | --- | --- |
| Kích hoạt khi | Giá trị **có** nhưng **không khớp dòng nào** trong bảng quy đổi | Giá trị **rỗng / không có** (nội bộ null khi gửi, đối tác không gửi khoá khi nhận) |
| Phạm vi | Thuộc bộ mã — mọi khoá, mọi hồ sơ dùng bộ mã đó đều chung | Thuộc đúng một khoá trong một hồ sơ |
| Trả lời câu hỏi | "Giá trị lạ thì quy về đâu?" | "Thiếu dữ liệu thì điền gì?" |

Tên trùng nhau là có chủ ý: ở cả hai nơi, `defaultPartnerValue` là giá trị theo chuẩn đối tác
nên chỉ dùng khi GỬI, `defaultSourceValue` là giá trị nội bộ nên chỉ dùng khi NHẬN.

Trong thực tế một khoá chỉ có **một** nguồn mặc định: FE khoá hai ô mặc định của lá ngay khi
người dùng chọn bộ mã (giá trị rỗng cũng không khớp dòng nào nên rơi luôn vào mặc định của bộ
mã). Nếu gặp hồ sơ cũ có cả hai, **ưu tiên mặc định của bộ mã**.

**Quy ước cốt lõi:** `targetType`, `dateFormat`, `numberFormat` **mô tả phía đối tác**, không
phụ thuộc chiều. Chiều gửi thì ép dữ liệu nội bộ sang kiểu/khuôn đó; chiều nhận thì dùng chính
khuôn đó để **đọc** chuỗi đối tác rồi ép về kiểu của trường gói tin
(`ShareDataPacketField.Type`). Nhờ vậy hồ sơ Cả hai chỉ cần một bộ giá trị.

Khoá `expression` còn được đọc/ghi trong code cũ nhưng **FE không sinh ra nữa** — service coi
như không có, nếu gặp thì bỏ qua và ghi log.

### 3.5. `$meta` — giá trị hệ thống, service tự sinh

```json
"dataTime":    { "$meta": "Now" },
"seq":         { "$meta": "Serial" },
"packetCode":  { "$meta": "PacketCode",  "$value": "101_commonData" },
"partnerCode": { "$meta": "PartnerCode", "$value": "PARTNER2" }
```

Bốn giá trị hợp lệ, **không bao giờ kèm `$extend`**:

| `$meta` | Nội dung | `$value` |
| --- | --- | --- |
| `Now` | Thời điểm gửi gói tin | không có — service tự sinh lúc gửi |
| `Serial` | Số thứ tự gói tin | không có — service tự sinh lúc gửi |
| `PacketCode` | Mã gói tin (`ShareDataPacket.Code`) | có, FE ghi sẵn lúc lưu hồ sơ |
| `PartnerCode` | Mã đối tác (`ShareDataPartner.Code`) | có, FE ghi sẵn lúc lưu hồ sơ |

`$value` là giá trị FE đã biết ngay lúc lưu hồ sơ, service dùng luôn cho nhanh. Nếu service
muốn tự tra lại từ CSDL thì vẫn được — `$meta` là nguồn sự thật, `$value` chỉ là bản chụp.

**Ràng buộc:** hồ sơ có chiều gửi (Outbound hoặc Both) **bắt buộc khai đủ cả 4 khoá Meta**;
BE chặn ở cả Add lẫn Update, lỗi `ESH-2108`.

### 3.6. `$exclude` — di sản, không còn dùng

```json
"debugInfo": { "$exclude": true }
```

Hồ sơ cũ có thể còn ký hiệu này, nghĩa là **không gửi khoá đó** (bỏ hẳn khoá khỏi payload).
FE hiện không sinh ra nữa. Service nên vẫn hiểu để đọc được hồ sơ cũ.

### 3.7. Mảng

Mảng trong bộ khung là mảng JSON thật, **không có ký hiệu riêng**. Service tự quyết theo nội dung:

- **Mảng có `$field` (hoặc `$meta`) bên trong** → đây là mảng **nhân theo bản ghi**. FE chỉ giữ
  **một phần tử duy nhất làm khuôn**, service lặp khuôn đó cho từng bản ghi dữ liệu.
- **Mảng không có `$field` nào bên trong** → mảng hằng, giữ nguyên toàn bộ phần tử.

```json
"data": [
  {
    "segmentCode": { "$field": "zoneId" },
    "avgSpeed":    { "$field": "averageSpeed" }
  }
],
"supportedTypes": [1, 2, 3]
```

---

## 4. Chiều GỬI (Outbound = 0)

### 4.1. Thứ tự áp luật cho MỘT lá `$field`

```
giá trị nội bộ
  → quy đổi bộ mã (sourceValue → partnerValue)
  → nếu rỗng thì lấy defaultPartnerValue
  → ép kiểu theo targetType + định dạng theo dateFormat / numberFormat
```

Hai điểm phải giữ đúng:

1. **Đã qua bộ mã thì KHÔNG ép kiểu nữa** — giá trị bộ mã trả ra đã là giá trị cuối cùng.
2. **Mặc định áp SAU bộ mã**, không phải trước. Giá trị nội bộ rỗng thì bộ mã không khớp gì cả,
   lúc đó mới lấy mặc định.

`numberFormat` chỉ dùng ở chiều này (làm tròn / hiển thị, VD `0.#`).

### 4.2. Nhân bản ghi

Xét bộ khung trước khi dựng payload:

- **Khuôn CÓ mảng chứa lá đã ánh xạ** → gộp mọi bản ghi vào **một gói tin**: mảng đó nhân theo
  từng bản ghi, các lá nằm ngoài mảng lấy bản ghi đầu tiên.
- **Khuôn KHÔNG có mảng như vậy** → **mỗi bản ghi thành một gói tin riêng**.

### 4.3. Các ký hiệu khác

`$meta` → sinh giá trị (Now = thời điểm gửi, Serial = số thứ tự, PacketCode/PartnerCode lấy
`$value` hoặc tra lại). Hằng số → ghi nguyên. `null` → gửi `null`. `$exclude` → bỏ khoá.

---

## 5. Chiều NHẬN (Inbound = 1)

Bộ khung lúc này là **bản đồ để đọc payload đối tác gửi lên**. Đi song song hai cây: cây bộ
khung và cây payload nhận được, khớp theo tên khoá và chỉ số mảng.

### 5.1. Thứ tự áp luật cho MỘT lá `$field`

```
giá trị đối tác gửi lên
  → quy đổi bộ mã NGƯỢC (partnerValue → sourceValue)
  → nếu rỗng thì lấy defaultSourceValue
  → parse theo dateFormat (nếu targetType là ngày) rồi ép về kiểu của ShareDataPacketField.Type
  → ghi vào trường AliasFieldKey của bản ghi nội bộ
```

Cụ thể với ngày: `DateTime.TryParseExact(value, dateFormat, CultureInfo.InvariantCulture,
DateTimeStyles.None, out var dt)`. **Bắt buộc `InvariantCulture`**, nếu để culture của máy chủ
thì `dd/MM` và `MM/dd` hiểu khác nhau tuỳ máy. Parse hỏng thì thử tiếp kiểu ISO/round-trip;
vẫn hỏng thì coi như rỗng, áp `defaultSourceValue` nếu có, còn không thì ghi cảnh báo và bỏ
qua khoá đó thay vì làm hỏng cả gói tin.

`numberFormat` **không dùng để parse** — parse số theo `InvariantCulture`, dấu chấm thập phân.

Múi giờ: khuôn không mang offset (VD `dd/MM/yyyy HH:mm:ss`) thì phải có quy ước cố định
(mặc định `+07:00` hoặc quy về UTC); khuôn ISO có offset thì giữ nguyên offset rồi quy đổi.

### 5.2. Các ký hiệu khác

| Ký hiệu | Chiều nhận xử lý |
| --- | --- |
| Hằng số | Bỏ qua. Nếu muốn chặt chẽ có thể đối chiếu: payload khác hằng số đã khai thì ghi cảnh báo |
| `null` | Bỏ qua, không ghi gì |
| `$meta` | Bỏ qua khi bóc dữ liệu. Có thể đối chiếu `PacketCode` / `PartnerCode` với `$value` để xác thực gói tin đến đúng hồ sơ |
| `$exclude` | Bỏ qua |
| Mảng có `$field` bên trong | Đây là **danh sách bản ghi**: payload có bao nhiêu phần tử thì sinh bấy nhiêu bản ghi nội bộ |
| Mảng hằng | Bỏ qua |

### 5.3. Điểm cần biết trước khi code

Bộ mã tra ngược theo `partnerValue`. BE **chưa chặn** hai dòng có cùng `partnerValue` trong một
bộ mã, nên service phải tự xử lý: lấy dòng đầu tiên và ghi cảnh báo, hoặc coi là lỗi cấu hình.
Tương tự, hai khoá đối tác khác nhau cùng trỏ vào một `AliasFieldKey` là hợp lệ khi gửi nhưng
mơ hồ khi nhận — service nên ghi cảnh báo, lấy giá trị cuối cùng hoặc giá trị không rỗng.

---

## 6. Chiều CẢ HAI (Both = 2)

Dùng **đúng một bộ khung** cho cả hai chiều, không có bộ khung riêng cho từng chiều.

- Cấu trúc, `$field`, `$meta`, hằng số, mảng: giống hệt, không phụ thuộc chiều.
- `codeSet`: một bảng dùng cho cả hai, tra xuôi khi gửi, tra ngược khi nhận.
- `targetType` / `dateFormat`: mô tả phía đối tác nên dùng chung cho cả hai chiều.
- `numberFormat`: chỉ có nghĩa khi gửi.
- **Mặc định tách đôi:** `defaultPartnerValue` áp khi gửi, `defaultSourceValue` áp khi nhận.
  Hồ sơ Both thường khai cả hai vì hai chiều điền vào hai phía khác nhau — VD `deviceState`
  gửi đi mặc định `"on"` (chuẩn đối tác) nhưng nhận về mặc định `1` (giá trị nội bộ).
- Ràng buộc 4 khoá Meta vẫn áp dụng, vì hồ sơ Both có gửi đi.

Một hạn chế cần biết: nếu đối tác **nhận** một khuôn ngày nhưng **gửi trả** khuôn khác thì
một `dateFormat` không diễn đạt đủ. Hiện chưa hỗ trợ; khi gặp thì tách thành hai hồ sơ một
chiều, hoặc báo lại để bổ sung khoá `dateFormat` riêng cho chiều nhận.

---

## 7. Bộ mã quy đổi (`ShareDataCodeSet`)

`$extend.codeSet` chứa `ShareDataCodeSet.Code`. Bảng quy đổi nằm trong cột `ValuesJson`:

```json
{
  "values": [
    { "sourceValue": "1", "partnerValue": "on",  "displayName": "Bật" },
    { "sourceValue": "2", "partnerValue": "off", "displayName": "Tắt" }
  ],
  "defaultSourceValue": "1",
  "defaultPartnerValue": "on"
}
```

- **Gửi:** khớp `sourceValue` → lấy `partnerValue`. Không khớp dòng nào → lấy
  `defaultPartnerValue`. Vẫn không có → giữ nguyên giá trị gốc và ghi cảnh báo.
- **Nhận:** khớp `partnerValue` → lấy `sourceValue`. Không khớp → lấy `defaultSourceValue`.
  Vẫn không có → giữ nguyên và ghi cảnh báo.
- So khớp **bỏ qua hoa thường và khoảng trắng thừa** (FE và BE đều kiểm tra trùng theo quy
  ước này).
- Hồ sơ đã chọn bộ mã thì FE **khoá** hai ô mặc định của lá — mặc định lúc đó lấy theo bộ mã.
  Nếu gặp hồ sơ cũ có cả `codeSet` lẫn `defaultPartnerValue`/`defaultSourceValue`, ưu tiên
  mặc định của bộ mã.
- Dữ liệu cũ có thể còn `ValuesJson` là **mảng thuần** (`[{...},{...}]`) thay vì object như
  trên; lúc đó coi như không có giá trị mặc định.

---

## 8. Những gì BE đã kiểm trước khi lưu hồ sơ

Service có thể tin các điều kiện sau đã đúng:

| Kiểm tra | Mã lỗi |
| --- | --- |
| Hồ sơ có chiều gửi phải khai đủ 4 khoá Meta | `ESH-2108` |
| `$field` không được rỗng | `mappingFieldKeyRequired` |
| Mã bộ mã khai trong `$extend.codeSet` phải tồn tại | `ESH-2104` |
| Mỗi (đối tác × gói tin × chiều) chỉ một hồ sơ được bật | `ESH-2101` / `ESH-2107` |
| Bộ mã: không trùng `sourceValue`, không trùng `partnerValue` | `ESH-2203` / `ESH-2205` |

Chưa kiểm (service tự phòng): trường bắt buộc của gói tin chưa được ánh xạ; hai khoá đối tác
cùng trỏ một trường nội bộ; chuỗi `dateFormat` có hợp lệ với .NET hay không.

---

## 9. Ví dụ đầy đủ

### 9.1. JSON mẫu đối tác (`PartnerSchemaJson`)

```json
{
  "header": {
    "source": "ITS-TMS",
    "dataTime": "2026-09-14T09:30:00+07:00",
    "partnerCode": "PARTNER2",
    "packetCode": "101_commonData",
    "serial": "126156"
  },
  "data": [
    { "zoneId": 1001, "averageSpeed": 45.5, "trafficCondition": "Normal" }
  ]
}
```

### 9.2. Bộ khung (`TargetShapeJson`)

```json
{
  "header": {
    "source": "ITS-TMS",
    "dataTime":    { "$meta": "Now" },
    "partnerCode": { "$meta": "PartnerCode", "$value": "PARTNER2" },
    "packetCode":  { "$meta": "PacketCode",  "$value": "101_commonData" },
    "serial":      { "$meta": "Serial" }
  },
  "data": [
    {
      "zoneId":       { "$field": "zoneId" },
      "averageSpeed": { "$field": "averageSpeed",
                        "$extend": { "targetType": "int", "numberFormat": "0.#" } },
      "trafficCondition": { "$field": "trafficCondition",
                            "$extend": { "codeSet": "TRAFFIC_COND",
                                         "targetType": "string" } }
    }
  ]
}
```

### 9.3. Chiều gửi — hai bản ghi nội bộ

| zoneId | averageSpeed | trafficCondition |
| --- | --- | --- |
| 1001 | 45.5 | 2 |
| 1002 | 61.25 | 1 |

Khuôn có mảng chứa lá ánh xạ → gộp thành **một gói tin**, mảng `data` nhân theo bản ghi:

```json
{
  "header": {
    "source": "ITS-TMS",
    "dataTime": "2026-09-18T14:05:31+07:00",
    "partnerCode": "PARTNER2",
    "packetCode": "101_commonData",
    "serial": "1287"
  },
  "data": [
    { "zoneId": 1001, "averageSpeed": 46,  "trafficCondition": "Congested" },
    { "zoneId": 1002, "averageSpeed": 61,  "trafficCondition": "Normal" }
  ]
}
```

`trafficCondition` đi qua bộ mã (`2 → "Congested"`, `1 → "Normal"`) nên **không** ép kiểu nữa.
`averageSpeed` không có bộ mã nên ép theo `targetType: int` và `numberFormat: 0.#`.

### 9.4. Chiều nhận — payload đối tác gửi lên

```json
{
  "header": { "source": "ITS-TMS", "dataTime": "18/09/2026 14:05:31",
              "partnerCode": "PARTNER2", "packetCode": "101_commonData", "serial": "9981" },
  "data": [ { "zoneId": 1001, "averageSpeed": "46", "trafficCondition": "Congested" } ]
}
```

Bản ghi nội bộ suy ra:

| Trường | Giá trị | Bước đã áp |
| --- | --- | --- |
| `zoneId` | 1001 | lấy thẳng, ép theo kiểu của trường |
| `averageSpeed` | 46 | parse số theo InvariantCulture, ép về kiểu trường |
| `trafficCondition` | 2 | bộ mã ngược `"Congested" → 2` |

`header.source` là hằng số nên bỏ qua; ba khoá `$meta` bỏ qua khi bóc dữ liệu, có thể đối
chiếu `packetCode` / `partnerCode` với `$value` để chắc chắn gói tin đến đúng hồ sơ.

---

## 10. Cạm bẫy đã gặp

- **Chuỗi `dateFormat` sai chuẩn .NET.** Hồ sơ hiện có đang lưu `dd/MM/YYYY HH:MM:ss` — sai:
  `YYYY` không hợp lệ (phải `yyyy`), `MM` ở phần giờ nghĩa là **tháng** chứ không phải phút
  (phải `mm`). Đúng phải là `dd/MM/yyyy HH:mm:ss`.
- **`DatatypeId` là ID gói tin (GUID), không phải mã.** Danh mục `shareData_type` cũ đã bỏ,
  mọi nơi đều dùng `ShareDataPacket.ID`.
- **Đừng suy "khoá vắng mặt nghĩa là bỏ".** Bộ khung luôn giữ đủ khoá; bỏ khoá được ghi tường
  minh bằng `$exclude` (di sản), còn khoá chưa ánh xạ thì ghi `null`.
- **`$meta` không bao giờ đi kèm `$extend`.** Gặp trường hợp ngược lại là dữ liệu hỏng.
