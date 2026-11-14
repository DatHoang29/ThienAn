# Kịch bản tạo đối tác mới ShareData (đối tác Kien)

Làm lần lượt từ trên xuống. Mỗi bước có hai phần: **Làm** là thao tác, **Thấy** là kết quả phải ra. Nếu thấy khác thì dừng lại và chụp màn hình.

- **Môi trường:** Máy chủ DEV `10.10.12.80` (giao diện web `https://10.10.12.80:12205`, WebAPI `https://10.10.12.80:12204`), CSDL `DEV_ITS015` (và `DEV_ITS015_Inbound`). WebAPI và Worker chạy bản mới nhất. FE tải lại hẳn trang bằng `Cmd+Shift+R`.
- **Dữ liệu test:** đối tác `KIEN`, gói `101_commonData`, chiều **Gửi đi**, **câu SQL riêng của Kien**. Kênh **Dịch vụ WebAPI** gửi ngược về API nhận của chính máy chủ DEV (`https://10.10.12.80:12204/api/sharedata/sharedatainbound/receive`); địa chỉ này giữ nguyên suốt kịch bản. Bảng `TmsZoneStatus` có 299 dòng (35 dòng chưa có `UpdateTime`), nhiều dòng trùng giờ.
- **Chu kỳ:** đăng ký gửi 30 giây một lượt, mỗi lượt gửi 1 trang `@pageSize` dòng.
- **Thứ tự bắt buộc:** Đối tác → Kết nối → Gói tin (Mẫu chung) → Đăng ký gửi (tự ở Tạm dừng) → Câu SQL riêng → Bộ mã chuẩn hóa → Hồ sơ ánh xạ (bật) → Bật đăng ký → Mốc đang chạy. Lý do: phải có đăng ký gửi gói `101_commonData` thì Kien mới xuất hiện trong hộp **Tạo cấu hình riêng +** ở màn **Cấu hình gói tin** và trong danh sách gói tin ở màn **Ánh xạ dữ liệu** (xem thêm [CHUC-NANG-SHAREDATA.md](../DacTa/CHUC-NANG-SHAREDATA.md) §1.1).
- **Xem kết quả:** **Lịch sử chia sẻ → Nhật ký truyền nhận** (cột *Số bản ghi*), màn **Cảnh báo lỗi**. Mốc xem ở khối **4. Mốc đang chạy**, màn **Cấu hình gói tin**, tab **Kien**.

> 📌 Bộ giá trị của Kien (`free`, `normal`, `slow`, `jam`) và JSON mẫu ở bước 6.2 là dữ liệu đặt ra để chạy được toàn bộ các bước. Có khuôn JSON và bộ mã thật của đối tác thì thay vào hai bước này; các bước còn lại giữ nguyên.

---

## 1. Tạo đối tác mới & Kết nối (màn Chia sẻ dữ liệu)

- [ ] **1.1** Vào màn **Chia sẻ dữ liệu**. Ở cột danh sách đối tác bên trái bấm **Thêm**.
- [ ] **1.2** Tab **Thông tin chung**: nhập:
  - Mã đối tác: `KIEN`
  - Tên đối tác: `Kien`
  - Ghi chú: `Đối tác thử nghiệm`
- [ ] **1.3** Tab **Cấu hình kết nối**: điền thông tin kết nối rồi bấm **Xác nhận**:
  - Địa chỉ (IP/host): `https://10.10.12.80`
  - Cổng: `12204` *(cổng WebAPI của máy chủ DEV, không phải cổng giao diện web `12205`)*
  - Endpoint: `/api/sharedata/sharedatainbound/receive`
  - Loại kênh kết nối: **Dịch vụ WebAPI**
  - Bật **Bỏ qua kiểm tra chứng chỉ SSL/TLS**

  **Thấy:**
  - Kien xuất hiện trong danh sách đối tác.
  - Ô Loại kênh kết nối chỉ có 3 lựa chọn: *Dịch vụ WebAPI*, *Máy chủ FTP*, *Hàng đợi tin Kafka*.
  - Dòng **Địa chỉ đầy đủ** tự ghép đúng: `https://10.10.12.80:12204/api/sharedata/sharedatainbound/receive`.
- [ ] **1.4** Kiểm chặn: bấm **Thêm** một đối tác nữa cũng mã `KIEN`. **Thấy:** báo trùng mã, không lưu được. Đóng hộp thoại.
- [ ] **1.5** Chọn đối tác **Kien** rồi bấm **Kết nối**. **Thấy:** trạng thái đổi sang đã kết nối, thẻ **Phiên hoạt động** tăng 1, nút **Kết nối** ẩn đi (chỉ còn **Ngắt kết nối**).

---

## 2. Tạo/cấu hình Gói tin & Mẫu chung (màn Cấu hình gói tin)

> Nếu môi trường đã có sẵn gói `101_commonData` thì mở gói đó kiểm tra lại các bước dưới đây; nếu làm mới từ đầu (hoặc tạo gói mới), thao tác từ bước 2.1.

- [ ] **2.1** Vào màn **Cấu hình gói tin**. Ở bảng **Gói tin** bên trái, bấm **Thêm** (hoặc chọn gói `101_commonData` nếu đã có):
  - **Mã**: `101_commonData` *(hoặc mã gói mới, ví dụ `kien_101_data`, tối đa 128 ký tự)*
  - **Tên**: `Thông tin chung / luồng giao thông`
  - **Loại gói (thứ tự)**: `101 · Thông tin chung / luồng giao thông`
  - **Trạng thái**: **Hoạt động**
  - Bấm **Xác nhận**.
- [ ] **2.2** Chọn gói vừa tạo ở bảng bên trái → mở tab **Gửi đi · lấy dữ liệu gửi đối tác** → phạm vi **Mẫu chung**.
- [ ] **2.3** Ở khối **1. Câu truy vấn mẫu chung**, dán câu SQL chuẩn dưới đây rồi bấm **Lưu & phân tích**:

  ```sql
  SELECT TOP (@pageSize)
      CONVERT(varchar(23), COALESCE(zs.UpdateTime, zs.CreateTime), 121) + '_' + zs.ID AS __lastKey,
      zs.ID AS zoneStatusId,
      zs.ZoneId AS zoneId,
      z.Name AS zoneName,
      z.FromKmNumber AS fromLocationKm,
      z.FromMetNumber AS fromLocationMet,
      z.ToKmNumber AS toLocationKm,
      z.ToMetNumber AS toLocationMet,
      z.LaneId AS laneId,
      CAST(zs.AverageSpeed AS DECIMAL(18, 2)) AS averageSpeed,
      zs.Condition AS trafficCondition,
      COALESCE(zs.UpdateTime, zs.CreateTime) AS dataTime,
      z.MaxSpeed AS speedLimit,
      ts.TotalVehicleNumber AS vehicleCount
  FROM dbo.TmsZoneStatus zs
  LEFT JOIN dbo.TmsZone z ON zs.ZoneId = z.ID
  LEFT JOIN dbo.TmsTrafficStatistic ts ON zs.ZoneId = ts.ZoneId
  WHERE CONVERT(varchar(23), COALESCE(zs.UpdateTime, zs.CreateTime), 121) + '_' + zs.ID > @lastKey
  ORDER BY __lastKey ASC
  ```

  **Thấy:**
  - Khối **2. Trường dữ liệu** tự động phân tích ra 13 trường từ câu SQL (bỏ qua cột kỹ thuật `__lastKey`) kèm kiểu tự đoán.
  - Khối **3. Tham số điều kiện** tự trích `@pageSize` (*Giá trị nhập*, mặc định `100`) và `@lastKey` (*Theo Mốc đang chạy*, ô **Mốc bắt đầu** trống = *Gửi toàn bộ dữ liệu*).
  - Góc phải hiện huy hiệu **Quy chuẩn mốc: 3/3** màu xanh.
- [ ] **2.4** *(Tùy chọn chỉnh thông tin trường)* Ở khối **2. Trường dữ liệu**, bấm biểu tượng bút sửa (`✎`) ở cuối một dòng trường bất kỳ để mở hộp thoại **Chỉnh sửa trường gói tin**: đặt **Tên hiển thị**, **Nhóm**, **Kiểu**, tích **Bắt buộc**, **Ghi chú** → bấm **Lưu**.

---

## 3. Tạo đăng ký gửi (màn Chia sẻ dữ liệu → Kien → tab Gửi đi)

Tạo đăng ký gửi ngay sau khi có đối tác và gói tin để hệ thống ghi nhận Kien sử dụng gói `101_commonData` (nhờ vậy màn **Cấu hình gói tin** mới liệt kê Kien khi bấm **Tạo cấu hình riêng +**, và màn **Ánh xạ dữ liệu** mới cho chọn gói `101_commonData`).

- [ ] **3.1** Vào màn **Chia sẻ dữ liệu** → chọn **Kien** → tab **Gửi đi** → bấm **Tạo đăng ký gửi** và điền:
  - Loại dữ liệu (gói tin): `101_commonData`
  - Kiểu lịch: **Liên tục**, Chu kỳ (giây): **30** *(công tắc Gửi ngay khi có dữ liệu mới để tắt)*
- [ ] **3.2** Bấm **Đăng ký**. **Thấy:**
  - Vì Kien chưa có hồ sơ ánh xạ đang bật, hệ thống tự đặt đăng ký ở trạng thái **Tạm dừng** (chưa chạy gửi, không sinh cảnh báo `ESH-1304`).
  - Cột **Hồ sơ ánh xạ** hiện nhãn đỏ **Chưa có hồ sơ**, cột **Mapping áp dụng** để trống (`—`).
  - Công tắc **Bật/Tắt** bị khóa; rê chuột vào thấy nhắc vào màn Ánh xạ dữ liệu tạo và bật hồ sơ trước.
- [ ] **3.3** Kiểm chặn: tạo thêm một đăng ký gửi gói `101_commonData` nữa cho Kien. **Thấy:** bị từ chối vì đã có đăng ký đang sống.

---

## 4. Tạo câu SQL riêng của Kien (màn Cấu hình gói tin)

- [ ] **4.1** Mở gói `101_commonData`, tab **Mẫu chung**. **Thấy:** bảng tham số có `@pageSize` và `@lastKey` (*Theo Mốc đang chạy*), dải **Quy chuẩn mốc** 3/3 xanh; khối **4. Mốc đang chạy của đối tác dùng mẫu chung** có dòng `KIEN — Kien` (vì Kien đã đăng ký gói 101 nhưng chưa có câu SQL riêng).
- [ ] **4.2** Bấm **Tạo cấu hình riêng +** → chọn Đối tác `KIEN — Kien (đang dùng theo mẫu)`, Sao chép từ **Mẫu chung** → bấm **Tạo & mở tab**. Xóa khung soạn, dán **câu gốc** dưới đây → **Lưu & phân tích**. **Thấy:**
  - Đang ở phạm vi đối tác **KIEN Kien**;
  - Bảng tham số có `@pageSize` (`100`) và `@lastKey` (*Theo Mốc đang chạy*);
  - Dải **Quy chuẩn mốc** 3/3 xanh.
- [ ] **4.3** Đổi `@pageSize` = **21** → **Lưu & phân tích**. **Thấy:** khối **4. Mốc đang chạy của Kien** ghi *"Chưa có, lượt gửi tới lấy toàn bộ dữ liệu"*.
- [ ] **4.4** Chuyển phạm vi về **Mẫu chung**. **Thấy:** khối 4 không còn dòng Kien, vì Kien đã có câu SQL riêng. Chuyển lại sang tab **Kien**.

**Câu gốc** (các ca ở mục 9 sửa từ câu này; `K` là biểu thức cột mốc ở dòng 2):

```sql
SELECT TOP (@pageSize)
    CONVERT(varchar(23), COALESCE(zs.UpdateTime, zs.CreateTime), 121) + '_' + zs.ID AS __lastKey,
    zs.ID AS zoneStatusId,
    zs.ZoneId AS zoneId,
    z.Name AS zoneName,
    z.FromKmNumber AS fromLocationKm,
    z.FromMetNumber AS fromLocationMet,
    z.ToKmNumber AS toLocationKm,
    z.ToMetNumber AS toLocationMet,
    z.LaneId AS laneId,
    CAST(zs.AverageSpeed AS DECIMAL(18, 2)) AS averageSpeed,
    zs.Condition AS trafficCondition,
    COALESCE(zs.UpdateTime, zs.CreateTime) AS dataTime,
    z.MaxSpeed AS speedLimit,
    ts.TotalVehicleNumber AS vehicleCount
FROM dbo.TmsZoneStatus zs
LEFT JOIN dbo.TmsZone z ON zs.ZoneId = z.ID
LEFT JOIN dbo.TmsTrafficStatistic ts ON zs.ZoneId = ts.ZoneId
WHERE CONVERT(varchar(23), COALESCE(zs.UpdateTime, zs.CreateTime), 121) + '_' + zs.ID > @lastKey
ORDER BY __lastKey ASC
```

---

## 5. Tạo bộ mã chuẩn hóa từ đầu (màn Bộ mã)

Tạo mới từ đầu cả hai bộ mã dùng cho Kien: **làn xe** (`kien_lane_code`) và **tình trạng giao thông** (`kien_traffic_condition`) để không phụ thuộc dữ liệu có sẵn.

- [ ] **5.1** Vào màn **Bộ mã** → bấm **Thêm**. Tạo bộ mã 1 — **Làn xe**:
  - Mã: `kien_lane_code`
  - Tên: `Mã làn đường (Kien)`
  - Thêm 4 dòng quy đổi:

    | Giá trị nội bộ | Giá trị đối tác | Tên hiển thị |
    |---|---|---|
    | `0` | `ALL_LANES` | Tất cả các làn |
    | `1` | `LANE_1` | Làn 1 |
    | `2` | `LANE_2` | Làn 2 |
    | `3` | `LANE_3` | Làn 3 |

  - Dòng mặc định: nội bộ `1`, đối tác `LANE_1`.
  - Bấm **Xác nhận** (Lưu). **Thấy:** lưới có bộ mã `kien_lane_code`, cột *Số giá trị* là 4.
- [ ] **5.2** Bấm **Thêm**. Tạo bộ mã 2 — **Tình trạng giao thông**:
  - Mã: `kien_traffic_condition`
  - Tên: `Tình trạng giao thông (Kien)`
  - Thêm 4 dòng quy đổi:

    | Giá trị nội bộ | Giá trị đối tác | Tên hiển thị |
    |---|---|---|
    | `GOOD` | `free` | Thông thoáng |
    | `NORMAL` | `normal` | Bình thường |
    | `SLOW` | `slow` | Di chuyển chậm |
    | `CONGES` | `jam` | Ùn tắc |

  - Dòng mặc định: nội bộ `NORMAL`, đối tác `normal`.
  - Bấm **Xác nhận** (Lưu). **Thấy:** lưới có bộ mã `kien_traffic_condition`, cột *Số giá trị* là 4.
- [ ] **5.3** Mở lại bộ mã `kien_traffic_condition`, ở khối **Thử quy đổi**:
  - Gõ `SLOW`. **Thấy:** *Khớp bảng quy đổi*, ra `slow`;
  - Gõ `ABC`. **Thấy:** *Không khớp, dùng giá trị mặc định*, ra `normal`.
- [ ] **5.4** Kiểm chặn: thêm một dòng nội bộ `SLOW` thứ hai. **Thấy:** cảnh báo trùng Giá trị nội bộ. Xóa dòng đó đi.
- [ ] **5.5** Kiểm chặn: thử lưu một bộ mã không có dòng quy đổi nào. **Thấy:** không lưu được.

---

## 6. Tạo hồ sơ ánh xạ từ đầu (màn Ánh xạ dữ liệu)

- [ ] **6.1** Vào màn **Ánh xạ dữ liệu** → bấm **Thêm**. Ở tab **Thông tin chung**:
  - Khi chưa chọn đối tác, ô **Gói tin** bị khóa.
  - Chọn Đối tác: **Kien** (Chiều mặc định là **Gửi đi**).
  - Chọn Gói tin: **101_commonData**.
  - Nhập Tên hồ sơ: `Kien 101 gửi đi`.
  - **Thấy:** Mã hồ sơ tự sinh `KIEN_101COMMONDATA_OUT`, bị khóa không sửa được.
- [ ] **6.2** Chuyển sang tab **Ánh xạ**: dán JSON mẫu của Kien vào ô **JSON đối tác** rồi bấm **Phân tích**:

  ```json
  {
    "header": { "sentAt": "2026-11-12T10:00:00", "seq": 1, "packet": "101_commonData", "sender": "KIEN" },
    "items": [
      { "segmentId": "Z01", "segmentName": "Km10-Km12", "lane": "LANE_1",
        "speed": 52.5, "status": "slow", "time": "2026-11-12T10:00:00", "count": 120 }
    ]
  }
  ```

  **Thấy:** cây vẽ lại đúng cấu trúc JSON, `header` có nhãn *nhóm*.
- [ ] **6.3** Gắn trường hệ thống cho từng khóa và mở thiết lập luật (biểu tượng bánh răng ở cột phải của lá) để khai luật quy đổi:

  | Khóa của Kien | Hệ thống | Luật (trong thiết lập lá) |
  |---|---|---|
  | `header.sentAt` | `Now` *(Meta)* | — |
  | `header.seq` | `Serial` *(Meta)* | — |
  | `header.packet` | `PacketCode` *(Meta)* | — |
  | `header.sender` | `PartnerCode` *(Meta)* | — |
  | `items[].segmentId` | `zoneId` | — |
  | `items[].segmentName` | `zoneName` | — |
  | `items[].lane` | `laneId` | Bộ mã `kien_lane_code` |
  | `items[].speed` | `averageSpeed` | Kiểu dữ liệu: `number` |
  | `items[].status` | `trafficCondition` | Bộ mã `kien_traffic_condition` |
  | `items[].time` | `dataTime` | Kiểu dữ liệu: `datetime`, Định dạng ngày: `yyyy-MM-dd'T'HH:mm:ss` |
  | `items[].count` | `vehicleCount` | Kiểu dữ liệu: `number` |

  - Mảng `items` phải mang nhãn **nhân theo bản ghi**.
  - Có thể bấm **Tự động ánh xạ** trước rồi sửa lại những khóa chưa khớp.
  - **Thấy:** khóa đã chọn bộ mã thì hai ô mặc định bị khóa (*"Bộ mã đã lo giá trị mặc định"*).
  - **Thấy:** cuối tab có khối *"Gói tin còn N trường đối tác không yêu cầu"*.
- [ ] **6.4** Kiểm chặn: bỏ khóa `PartnerCode` rồi bấm **Xác nhận** (Lưu). **Thấy:** không lưu được vì thiếu giá trị hệ thống (*"Hồ sơ có chiều gửi phải khai đủ 4 giá trị hệ thống"*). Gắn lại `PartnerCode`.
- [ ] **6.5** Chuyển sang tab **Dữ liệu gửi đi**: đặt **Số bản ghi** = `2` rồi bấm **Lấy dữ liệu mẫu** (hệ thống lấy 2 bản ghi thật từ CSDL theo câu SQL riêng của Kien và tự dựng payload; nếu sửa luật thì bấm thêm **Dựng payload gửi đi**).
  - **Thấy:** bảng **Bản ghi thật của hệ thống** hiện 2 dòng; khung **Payload đối tác sẽ nhận** ra 1 gói tin JSON, mảng `items` có 2 phần tử.
  - **Thấy:** `header.sender` = `"KIEN"`, `items[].lane` quy đổi sang `ALL_LANES` / `LANE_1` / `LANE_2`, `items[].status` quy đổi sang `free` / `normal` / `slow` / `jam`.
  - **Thấy:** vì định dạng `yyyy-MM-dd'T'HH:mm:ss` có ký tự `'T'`, bản xem trước trên trình duyệt hiện dòng nhắc vàng *"Định dạng của dataTime chưa được hỗ trợ đầy đủ trong bản xem trước. Cần đối chiếu kết quả backend"* (khi Worker gửi thật ở mục 7 sẽ định dạng đúng).
- [ ] **6.6** Quay lại tab **Thông tin chung**, giữ công tắc **Đang dùng** ở trạng thái **Bật** rồi bấm **Xác nhận** (Lưu). **Thấy:** lưới có hồ sơ `KIEN_101COMMONDATA_OUT` đang bật.
- [ ] **6.7** Quay lại màn **Chia sẻ dữ liệu** → chọn **Kien** → tab **Gửi đi**. **Thấy:** cột **Hồ sơ ánh xạ** đổi sang nhãn xanh **Đang áp dụng**, cột **Mapping áp dụng** hiện `Kien 101 gửi đi`, và công tắc **Bật/Tắt** đã mở khóa.

---

## 7. Bật đăng ký & Kiểm tra gửi có chia trang

- [ ] **7.1** Bật công tắc đăng ký gửi của Kien, chờ khoảng 8 phút (15 lượt). **Thấy:**
  - Ở **Lịch sử chia sẻ → Nhật ký truyền nhận**: có 15 lượt chiều **Gửi đi** báo **Thành công** (14 lượt 21 bản ghi, 1 lượt 5 bản ghi, tổng cộng đủ 299 bản ghi). *(Lưu ý: vì gửi vòng về chính API nhận của máy chủ DEV trong khi Kien chưa tạo đăng ký chiều Nhận về, nhật ký sẽ có thêm các dòng chiều **Nhận về** báo "Chưa có đăng ký nhận cho loại gói tin này" — đây là kết quả đúng của đầu nhận).*
  - Ở màn **Cảnh báo lỗi**: không có cảnh báo lỗi gửi hay `ESH-1304` mới cho Kien.
  - Mở chi tiết payload gửi thật: `sender` = `KIEN`, `lane` ra `ALL_LANES` / `LANE_1` / `LANE_2`, `status` ra `free` / `normal` / `slow` / `jam`, `time` đúng định dạng `yyyy-MM-ddTHH:mm:ss`.
- [ ] **7.2** Chờ thêm 1 lượt. **Thấy:** không gửi thêm.

---

## 8. Mốc đang chạy, các kiểu đặt mốc

Làm trên tab **Kien** của gói 101, khối **4. Mốc đang chạy của Kien**.

- [ ] **8.1** Khóa khi đăng ký đang bật. Mở lại gói 101 → chọn phạm vi đối tác **Kien**. **Thấy:**
  - **Lưu & phân tích** và **Dùng lại mẫu chung** mờ (bị khóa), có thông báo vàng *"Đang có đăng ký gửi bật sử dụng (Kien)..."*;
  - Ô `@lastKey` ở khối 3 ghi *"Đã có mốc, gửi tiếp"*;
  - Khối 4 có 1 dòng `KIEN — Kien`, mốc nguyên chuỗi `yyyy-MM-dd HH:mm:ss.fff_<ID>`; hai nút biểu tượng **Đặt mốc** và **Đặt lại** ở cột Thao tác bị khóa (mờ).
- [ ] **8.2** Khóa ở các màn khác khi đăng ký đang bật. **Thấy:**
  - Hồ sơ ánh xạ `KIEN_101COMMONDATA_OUT` không sửa, xóa hay tắt được;
  - Sửa `kien_traffic_condition` thì báo vàng bộ mã đang dùng, bảng quy đổi bị khóa.
- [ ] **8.3** Đặt mốc **sớm hơn**. Tắt đăng ký, tải lại trang → **Đặt mốc** → chọn giờ sớm hơn mốc hiện tại. **Thấy:** *"Gửi lại dữ liệu từ … tới …"*. Bấm Hủy.
- [ ] **8.4** Đặt mốc **muộn hơn**. **Đặt mốc** → chọn giờ muộn hơn mốc hiện tại. **Thấy:** *"Bỏ qua dữ liệu từ … tới …"*. Bấm Hủy.
- [ ] **8.5** Đặt mốc rồi gửi lại. **Đặt mốc** → chọn giờ sớm hơn → **Xác nhận**. **Thấy:**
  - *"Đã cập nhật mốc gửi dữ liệu cho đối tác."*;
  - Mốc thành `yyyy-MM-dd HH:mm:ss.000_`.

  Bật đăng ký, chờ tới khi hết dữ liệu. **Thấy:** chỉ gửi lại các dòng có cột mốc sau giờ đã chọn; tải lại trang thì mốc đã tiến.
- [ ] **8.6** **Đặt lại**. Tắt đăng ký → **Đặt lại** → xác nhận. **Thấy:** *"Đã bỏ Mốc đang chạy."*; dòng Kien ghi *"Chưa có, lượt gửi tới lấy …"*.

  Bật đăng ký, chờ khoảng 8 phút. **Thấy:** gửi lại đủ 299 dòng như bước 7.1.
- [ ] **8.7** Tab **Mẫu chung**, bảng tham số. **Thấy:** ô `@lastKey` trống, ghi *"Đối tác chưa có mốc: gửi toàn bộ"*. Không lưu.

---

## 9. Câu SQL riêng sai bị chặn khi lưu

Tắt đăng ký trước. Mỗi ca:
1. Trên tab Kien, sửa câu gốc như cột *Sửa*.
2. Bấm **Lưu & phân tích**: phải thấy câu báo ở cột *Thấy*, câu không được lưu.
3. Chọn lại gói → bỏ thay đổi.

| Ca | Sửa | Thấy (đầu câu báo) |
|---|---|---|
| 9.1 | `WHERE zs.ID > @lastKey` | "Biểu thức so với biến lastKey trong WHERE phải viết giống hệt…" |
| 9.2 | `WHERE` dùng `CONVERT(varchar(23), zs.CreateTime, 121) + '_' + zs.ID` | Như 9.1 |
| 9.3 | `ORDER BY zs.ID ASC` | "…phải sắp xếp ORDER BY ngoài cùng theo cột [__lastKey]…" |
| 9.4 | `ORDER BY __lastKey DESC` | "…bắt buộc sắp xếp tăng dần (ASC)…" |
| 9.5 | Bỏ `COALESCE`, chỉ còn `zs.UpdateTime` (dòng 2 và `WHERE`) | "Cột mốc dùng UpdateTime trơn…"; bấm **Sửa câu** thì về 3/3 |
| 9.6 | Dòng 2: `zs.UpdateTime AS __lastKey`; `WHERE zs.UpdateTime > @lastKey` | "Cột mốc chỉ có ngày giờ…"; **Sửa câu** về 3/3 |
| 9.7 | `WHERE K >= @lastKey` | "…phải dùng dấu >…" (dải vẫn 3/3) |
| 9.8 | `varchar(23) … 121` đổi thành `varchar(10) … 103` (dòng 2 và `WHERE`) | "…ghép ngày giờ sai định dạng…" |
| 9.9 | Xóa dòng `WHERE` | "Câu có cột mốc nhưng không dùng biến mốc…" |
| 9.10 | Dòng 2: `zs.ID AS __lastKey`; `WHERE zs.ID > @lastKey` | "Cột mốc phải viết đúng công thức…"; **Sửa câu** về 3/3 |
| 9.11 | `K` thay bằng `CAST(z.FromKmNumber AS varchar(10))` (dòng 2 và `WHERE`) | Như 9.10 |
| 9.12 | `COALESCE(zs.UpdateTime, zs.CreateTime)` thay bằng `zs.DetectWay` (dòng 2 và `WHERE`) | Như 9.10; **Sửa câu** ra `COALESCE(zs.DetectWay, zs.CreateTime)` |
| 9.13 | `'_' + zs.ID` thay bằng `'_' + z.Name` (dòng 2 và `WHERE`) | Như 9.10 |
| 9.14 | Bỏ `TOP (@pageSize)`, thêm cuối câu `OFFSET 1 ROWS FETCH NEXT @pageSize ROWS ONLY` | "…không được dùng OFFSET…" |
| 9.15 | Câu A bên dưới | "…không dùng chung một mốc…" |
| 9.16 | Câu B bên dưới | "Câu con có TOP phải sắp…" |

```sql
-- Câu A (9.15): hai nguồn dùng chung @lastKey
SELECT TOP (@pageSize) u.__lastKey, u.zoneId
FROM (
    SELECT CONVERT(varchar(23), COALESCE(zs.UpdateTime, zs.CreateTime), 121) + '_' + zs.ID AS __lastKey, zs.ZoneId AS zoneId
    FROM dbo.TmsZoneStatus zs
    WHERE CONVERT(varchar(23), COALESCE(zs.UpdateTime, zs.CreateTime), 121) + '_' + zs.ID > @lastKey
    UNION ALL
    SELECT CONVERT(varchar(23), COALESCE(e.UpdateTime, e.CreateTime), 121) + '_' + e.ID, e.Code
    FROM TmsEquipment e
    WHERE CONVERT(varchar(23), COALESCE(e.UpdateTime, e.CreateTime), 121) + '_' + e.ID > @lastKey
) u
ORDER BY u.__lastKey ASC

-- Câu B (9.16): câu con có TOP sắp theo cột khác cột mốc
SELECT T.__lastKey, T.zoneId
FROM (
    SELECT TOP (@pageSize) CONVERT(varchar(23), COALESCE(zs.UpdateTime, zs.CreateTime), 121) + '_' + zs.ID AS __lastKey, zs.ZoneId AS zoneId
    FROM dbo.TmsZoneStatus zs
    WHERE CONVERT(varchar(23), COALESCE(zs.UpdateTime, zs.CreateTime), 121) + '_' + zs.ID > @lastKey
    ORDER BY zs.AverageSpeed
) T
ORDER BY T.__lastKey ASC
```

---

## 10. Câu riêng hai nhóm mốc *(tùy chọn)*

- [ ] **10.1** Đăng ký tắt. Tab Kien: dán câu dưới → **Lưu & phân tích**. Hộp xác nhận báo thiếu trường ánh xạ (`laneId`, `vehicleCount` không có trong câu) và bỏ mốc Kien → **Xác nhận**. **Thấy:** bảng tham số có `@lastKey_upd`, `@lastKey_new` (*Theo Mốc đang chạy*), không còn `@lastKey`.
- [ ] **10.2** `@pageSize` = **20** → Lưu → bật đăng ký, chờ khoảng 8 phút. **Thấy:** đủ 299 dòng (14 lần 20 dòng, 1 lần 19 dòng).
- [ ] **10.3** Chờ thêm 1 lượt. **Thấy:** không gửi thêm.
- [ ] **10.4** Tắt đăng ký. Khối 4. **Thấy:** dòng Kien có mốc của cả hai nhóm.
- [ ] **10.5** Trả về câu một mốc: dán lại câu gốc, `@pageSize` = 21, **Lưu & phân tích**, **Xác nhận**. **Thấy:** bảng tham số về `@pageSize`, `@lastKey`; dòng Kien ghi *"Chưa có, lượt gửi tới lấy toàn bộ dữ liệu"*.

```sql
SELECT TOP (@pageSize)
    u.__lastKey_upd, u.__lastKey_new,
    u.zoneStatusId, u.zoneId, u.zoneName, u.averageSpeed, u.trafficCondition, u.dataTime
FROM (
    SELECT
        CONVERT(varchar(23), zs.UpdateTime, 121) + '_' + zs.ID AS sortKey,
        CONVERT(varchar(23), zs.UpdateTime, 121) + '_' + zs.ID AS __lastKey_upd,
        CAST(NULL AS NVARCHAR(100)) AS __lastKey_new,
        zs.ID AS zoneStatusId, zs.ZoneId AS zoneId, z.Name AS zoneName,
        CAST(zs.AverageSpeed AS DECIMAL(18, 2)) AS averageSpeed, zs.Condition AS trafficCondition, zs.UpdateTime AS dataTime
    FROM dbo.TmsZoneStatus zs
    LEFT JOIN dbo.TmsZone z ON zs.ZoneId = z.ID
    WHERE CONVERT(varchar(23), zs.UpdateTime, 121) + '_' + zs.ID > @lastKey_upd
    UNION ALL
    SELECT
        CONVERT(varchar(23), zs.CreateTime, 121) + '_' + zs.ID, NULL, CONVERT(varchar(23), zs.CreateTime, 121) + '_' + zs.ID,
        zs.ID, zs.ZoneId, z.Name, CAST(zs.AverageSpeed AS DECIMAL(18, 2)), zs.Condition, zs.CreateTime
    FROM dbo.TmsZoneStatus zs
    LEFT JOIN dbo.TmsZone z ON zs.ZoneId = z.ID
    WHERE zs.UpdateTime IS NULL
      AND CONVERT(varchar(23), zs.CreateTime, 121) + '_' + zs.ID > @lastKey_new
) u
ORDER BY u.sortKey ASC
```

---

## 11. Dùng lại mẫu chung

- [ ] **11.1** Đăng ký tắt, Kien đã có mốc (chạy lại bước 7.1 nếu vừa làm 10.5). Tab Kien → **Dùng lại mẫu chung**. **Thấy:** tab Kien mất, mốc Kien giữ nguyên vì câu riêng và Mẫu chung cùng công thức cột mốc.
- [ ] **11.2** Tab **Mẫu chung** → khối 4. **Thấy:** có dòng Kien với mốc cũ.
- [ ] **11.3** Bật đăng ký, chờ 1 lượt. **Thấy:** không gửi lại từ đầu.

---

## 12. Dọn

- [ ] **12.1** Tắt đăng ký → tắt **Đang dùng** của hồ sơ ánh xạ → **Ngắt kết nối**. **Thấy:** khi ngắt, hệ thống báo số đăng ký gửi đã tự tắt.
- [ ] **12.2** Làm lại kịch bản từ đầu thì **Đặt lại** mốc Kien trước.
- [ ] **12.3** Muốn xóa hẳn thì xóa lần lượt: đăng ký → hồ sơ ánh xạ → bộ mã `kien_lane_code`, `kien_traffic_condition` → đối tác. **Thấy:** xóa đối tác khi còn kết nối thì bị chặn với câu *"Ngắt kết nối trước khi xoá"*.
