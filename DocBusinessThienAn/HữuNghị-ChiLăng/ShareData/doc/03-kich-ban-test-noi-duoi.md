# Kịch bản test nối đuôi ShareData (chỉ thao tác trên giao diện)

Làm từ trên xuống, đánh dấu từng ô. Mỗi bước: **Làm** → **Thấy**. Thấy khác thì dừng, chụp màn hình gửi lại.

- CSDL máy chủ mới `10.10.12.80/DEV_ITS015` (cập nhật 06/11/2026). Chạy WebAPI, Worker bản mới nhất trỏ vào CSDL này (Worker: `DefaultConnection` = `DEV_ITS015`, `InboundConnection` = `DEV_ITS015_Inbound`).
- Đối tác **Phòng Cảnh sát giao thông (PC08)** (mã `PC08`), gói **`101_commonData`**. Dùng sẵn hồ sơ ánh xạ `PC08_101COMMONDATA_OUT` (đã gắn bộ mã chuẩn `lane_code` cho `laneId` và `traffic_condition` cho `trafficCondition`). Trên `DEV_ITS015` PC08 đã có sẵn đăng ký **gửi** gói 101 chu kỳ **30 giây** đang tắt: bước 0.3 giữ đăng ký này (chưa có mới tạo).
- Câu mẫu gói 101 là câu nối đuôi một chuỗi ghép (PA1, sếp chốt 02/11/2026) `thời gian_khóa` ở cột `__lastKey`. Kịch bản tạo cấu hình riêng cho PC08 và viết câu trên đó; mẫu chung không đổi.
- PC08 chưa có địa chỉ nhận nên trong lúc test dùng kênh **Xuất tệp tin** (File): mỗi lần gửi được lưu thành một tệp bằng chứng, không cần máy nhận.
- Dữ liệu nguồn `TmsZoneStatus` trên `DEV_ITS015` (đếm ngày 06/11/2026): **299** dòng; **264** dòng có `UpdateTime`, mới nhất `22/10/2026 17:11:28.537`; **35** dòng chưa có `UpdateTime` (đều có `CreateTime`). Cột mốc lấy `CreateTime` khi `UpdateTime` trống nên gửi đủ cả 299 dòng. Câu JOIN không nhân dòng (299 giá trị cột mốc khác nhau).
- Giá trị nguồn đi qua **Bộ mã Chuẩn hóa** trên `DEV_ITS015`: `TmsZone.LaneId` có `0, 1, 2` (quy đổi ra `ALL_LANES, LANE_1, LANE_2`) cùng các dòng trống hoặc `L1` (rơi về mặc định `LANE_1`); `TmsZoneStatus.Condition` có `GOOD, NORMAL, SLOW, conges` (quy đổi không phân biệt hoa thường ra `FREE_FLOW, NORMAL, SLOW, CONGESTED`) cùng các dòng trống hoặc dữ liệu thử cũ `OLD, NEW` (rơi về mặc định `NORMAL`, vẫn gửi đủ 299 dòng và không sinh cảnh báo lỗi).
- Dòng **trùng giờ**: 139 dòng đầu cùng giờ `03/06/2026 11:16:10.730`; ba cặp trùng giờ đứng thứ **231–232** (`19/08/2026 14:57:16.920`), 255–256, 291–292.
- Xem kết quả ở **Theo dõi chia sẻ → Lịch sử chia sẻ → Nhật ký truyền nhận** (cột *Số bản ghi*). Lượt sau không gửi lặp nghĩa là mốc đã tiến. Xem mốc trực tiếp ở khối **4. Mốc đang chạy** của tab **Cấu hình gói tin** (dưới bảng tham số; tab Mẫu chung liệt kê đối tác dùng mẫu chung, tab đối tác chỉ có đối tác đó), kiểm ở mục 10. Mốc hiện nguyên chuỗi như trong CSDL, ví dụ `2026-10-22 17:11:28.537_<ID>`.
- Màn hình dạng tab: **Chia sẻ dữ liệu → Cấu hình chia sẻ** gồm các tab *Cấu hình, Cấu hình gói tin, Ánh xạ dữ liệu, Bộ mã Chuẩn hóa, Nguồn sự kiện* (không còn tab *Mốc gửi*: chạy tệp seed menu số 13 để gỡ); **Theo dõi chia sẻ** gồm *Lịch sử chia sẻ, Cảnh báo & lỗi*.
- Đăng ký chạy 30 giây một lượt; mỗi lượt gửi đúng 1 trang dữ liệu (theo `@pageSize`; đã gỡ bỏ cơ chế tự động vét nhiều trang `outbound_max_pages_per_run`). Để gửi hết 299 dòng với `@pageSize = 21`, hệ thống sẽ gửi lần lượt qua 15 chu kỳ 30 giây (khoảng 7-8 phút); nếu muốn gửi hết trong 1 lượt thì đặt `@pageSize >= 300`. "Chờ một lượt" là chờ khoảng 1 phút.

---

## 0. Chuẩn bị

- [ ] **0.1** Trên `DEV_ITS015` đã chạy các tệp seed theo `sql/README.md` (câu mẫu 2, đối tác 14, bộ mã chuẩn hóa 15, ánh xạ 16; 11 gói tin đã có sẵn trên `DEV_ITS015`); gói 101 ở tab **Mẫu chung** đã lưu sẵn danh sách **13** trường dữ liệu. Mở web, tải lại hẳn trang (Cmd+Shift+R); mở tab **Bộ mã Chuẩn hóa** thấy **Tổng cộng: 9** bộ mã chuẩn (có `lane_code`, `traffic_condition`; không còn dòng `condition` cũ).
- [ ] **0.2** Tab **Cấu hình** → dòng **PC08**: đang ở trạng thái đã kết nối thì bấm **Ngắt kết nối** trước, vì lúc đang kết nối các ô kết nối bị khóa. Bấm sửa PC08 rồi điền:
  - **Địa chỉ (IP/host)**: `127.0.0.1`; **Cổng**: `8080`. Hai ô này bắt buộc nhập nhưng kênh tệp không dùng tới; PC08 tạo bằng tệp seed 14 nên đang để trống cả hai.
  - **Loại kênh kết nối**: **Xuất tệp tin** (kênh File); **Thư mục lưu trữ** để trống; **Định dạng**: JSON.

  → Lưu → **Kết nối**.
- [ ] **0.3** Tab **Cấu hình** → PC08 → **Gửi đi**: nếu đã có dòng `101_commonData` (Định kỳ 30 giây) thì giữ ở trạng thái **tắt**; chưa có mới bấm **Tạo đăng ký gửi**: gói `101_commonData`, chế độ **Định kỳ** 30 giây → Lưu, để **tắt**.
  **Thấy:** cột *Hồ sơ ánh xạ* hiện `PC08_101COMMONDATA_OUT`.
- [ ] **0.3b** Tab **Cấu hình gói tin** → gói `101_commonData` → tab **Mẫu chung** (PC08 chưa có cấu hình riêng; có rồi thì mở tab PC08) → khối **4. Mốc đang chạy**: nếu dòng **PC08** đang có mốc (trên `DEV_ITS015` đang có mốc đặt bằng tay `2026-10-22 00:00:00.000_`), bấm **↺ Đặt lại** → hộp hỏi "Bỏ Mốc đang chạy của <tên đối tác PC08>? Lượt gửi tới gửi lại …" → xác nhận.
  **Thấy:** "Đã bỏ Mốc đang chạy."; dòng PC08 ghi "Chưa có, lượt gửi tới lấy …" (bắt buộc đặt lại để bước 0.5–0.6 mở ô Mốc bắt đầu và mục 1 gửi đủ 299 dòng từ đầu).
- [ ] **0.4** Tab **Cấu hình gói tin** → gói `101_commonData` → **Tạo cấu hình riêng +** → Đối tác **PC08**, Sao chép từ **Mẫu chung** → xác nhận. PC08 đã có cấu hình riêng thì mở thẳng tab PC08.
  **Thấy:** tự mở tab PC08, khung soạn có câu `SELECT TOP (@pageSize) CONVERT(varchar(23), COALESCE(zs.UpdateTime, zs.CreateTime), 121) + '_' + zs.ID AS __lastKey, … FROM dbo.TmsZoneStatus zs …`.
- [ ] **0.5** Xóa hết khung soạn (Cmd+A, Delete), dán câu dưới (giống câu mẫu) → **Lưu & phân tích**.
  **Thấy:** lưu thành công; bảng **3. Tham số điều kiện** có `@pageSize` (ô số, điền sẵn 100) và `@lastKey` (*theo key*, ô ngày hiện *Gửi toàn bộ dữ liệu*); không có `@lastTime`.
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
- [ ] **0.6** `@pageSize` = **21**; `@lastKey` để trống ô ngày → **Lưu & phân tích**.
  **Thấy:** lưu thành công. Ô giá trị của `@lastKey` là ô chọn ngày giờ, không có ô gõ tay. Bảng tham số chỉ đặt Mốc bắt đầu: đã có mốc thì ô bị khóa (xem 2.1). khối **4. Mốc đang chạy** tab PC08 có một dòng PC08 ghi "Chưa có, lượt gửi tới lấy toàn bộ dữ liệu". Trên tiêu đề mục 3, dải **Quy chuẩn mốc** 3/3 xanh. Cột **Nguồn giá trị**: `@lastKey` là **Theo Mốc đang chạy**, `@pageSize` là **Giá trị nhập**, cả hai ô nguồn đều bị khóa. Bấm thanh tiêu đề mục 3 để thu gọn bảng, dải **Quy chuẩn mốc** vẫn hiện.

---

## 1. Gửi dữ liệu có sẵn, chia trang, dòng trùng giờ ở ranh giới trang

`@pageSize` = 21 làm trang 11 kết thúc ở dòng 231, dòng 232 cùng giờ sang trang 12; nhóm 139 dòng đầu cùng giờ trải qua 7 trang.

- [ ] **1.1** Tab **Cấu hình** → PC08 → bật đăng ký gửi gói 101 (**Bật/Tắt**). Chờ một lượt.
  **Thấy:**
  - **Nhật ký truyền nhận**: 15 lần gửi thành công, *Số bản ghi* **21** (14 lần) rồi **5** (đủ 299 dòng; tổng thiếu dòng nào nghĩa là mất dòng trùng giờ).
  - **Lịch sử chia sẻ → Nhật ký cấu hình**: không có dòng "Câu truy vấn nối đuôi có rủi ro".
  - Thư mục bằng chứng có 15 tệp JSON dạng `header` + `data` của hồ sơ `PC08_101COMMONDATA_OUT`. Mở kiểm tra 1 tệp JSON thấy trong mảng `data`: `laneId` đã chuẩn hóa thành `ALL_LANES`, `LANE_1`, `LANE_2` (không còn `"0"`, `"1"`, `"2"`, `"L1"` hay `null`); `trafficCondition` đã chuẩn hóa thành `FREE_FLOW`, `NORMAL`, `SLOW`, `CONGESTED` (dòng nguồn trống hoặc `OLD`, `NEW` ra mặc định `NORMAL`).
  - **Cảnh báo & lỗi**: không có cảnh báo hay lỗi mới.
- [ ] **1.2** Chờ thêm một lượt. **Thấy:** không có lần gửi mới (không gửi lặp).

## 2. Khóa khi đăng ký gửi đang bật (đăng ký vẫn bật)

- [ ] **2.1** Tab **Cấu hình gói tin** → chọn lại gói `101_commonData` → tab PC08.
  **Thấy:** **Lưu & phân tích** mờ, có biểu tượng khóa ghi "Đang có đăng ký gửi bật sử dụng (PC08)…". Ô giá trị của `@lastKey` bị khóa, có chữ "Đã có mốc, gửi tiếp", không hiện giá trị mốc. khối **4. Mốc đang chạy** dòng PC08 hiện mốc đang chạy dạng `yyyy-MM-dd HH:mm:ss.fff_<ID>`; **✎ Đặt mốc**, **↺ Đặt lại** mờ.
- [ ] **2.2** Tab **Bộ mã Chuẩn hóa** → bấm nút **Sửa** (`✎`) ở dòng `lane_code` (hoặc `traffic_condition`).
  **Thấy:** hiện thông báo vàng "Bộ mã đang được hồ sơ ánh xạ có đăng ký bật sử dụng (PC08_101COMMONDATA_OUT). Bảng quy đổi và giá trị mặc định bị khóa; tạm dừng đăng ký trước khi sửa.", bảng quy đổi và giá trị mặc định bị khóa mờ. Đóng hộp thoại.

## 3. Câu sai bị chặn khi lưu

- [ ] **3.0** Tắt đăng ký gửi. Chọn lại gói `101_commonData` → tab PC08.

Mỗi ca: xóa hết khung soạn (Cmd+A, Delete), dán câu của ca → **Lưu & phân tích** → xem thông báo → chọn lại gói, hộp hỏi bỏ thay đổi → **Xác nhận**. Câu mỗi ca chỉ khác câu ở bước 0.5 đúng dòng có chú thích `-- sai`. PC08 đã có mốc từ mục 1 nhưng câu sai bị từ chối trước, nên không hiện hộp xác nhận đổi công thức.

- [ ] **3.1** `WHERE` chỉ so `ID`, không so đúng cột mốc.
  **Thấy:** "Biểu thức so với biến lastKey trong WHERE phải viết giống hệt biểu thức của cột [__lastKey] ở SELECT; viết khác thì bỏ sót hoặc gửi lặp dòng."
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
  WHERE zs.ID > @lastKey  -- sai: chỉ so ID
  ORDER BY __lastKey ASC
  ```
- [ ] **3.2** `WHERE` dùng `zs.CreateTime`, khác biểu thức cột mốc ở `SELECT`.
  **Thấy:** Lỗi như 3.1.
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
  WHERE CONVERT(varchar(23), zs.CreateTime, 121) + '_' + zs.ID > @lastKey  -- sai: khác biểu thức cột mốc
  ORDER BY __lastKey ASC
  ```
- [ ] **3.3** `ORDER BY` không theo cột mốc.
  **Thấy:** "Truy vấn đồng bộ nối đuôi theo khóa phải sắp xếp ORDER BY ngoài cùng theo cột [__lastKey], nếu không có thể bỏ sót dòng."
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
  ORDER BY zs.ID ASC  -- sai: không sắp theo cột mốc
  ```
- [ ] **3.4** Sắp giảm dần.
  **Thấy:** "Truy vấn đồng bộ nối đuôi (Incremental) bắt buộc sắp xếp tăng dần (ASC); không cho phép sắp xếp giảm dần (DESC)…"
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
  ORDER BY __lastKey DESC  -- sai: giảm dần
  ```
- [ ] **3.5** Cột mốc dùng `zs.UpdateTime` trơn, không có `COALESCE`.
  **Thấy:** "Cột mốc dùng UpdateTime trơn nên dòng mới thêm (chưa có UpdateTime) không được gửi. Dùng COALESCE(UpdateTime, CreateTime) ở cả cột mốc và WHERE." Rê chuột vào **Quy chuẩn mốc**: mục *Có cột mốc* đỏ, ghi "Dòng mới thêm chưa có UpdateTime nên không được gửi"; bấm **Sửa câu** thì câu về `COALESCE`, 3/3 xanh.
  ```sql
  SELECT TOP (@pageSize)
      CONVERT(varchar(23), zs.UpdateTime, 121) + '_' + zs.ID AS __lastKey,  -- sai: UpdateTime trơn
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
  WHERE CONVERT(varchar(23), zs.UpdateTime, 121) + '_' + zs.ID > @lastKey  -- sai: UpdateTime trơn
  ORDER BY __lastKey ASC
  ```
- [ ] **3.6** Cột mốc chỉ có ngày giờ, không ghép khóa.
  **Thấy:** "Cột mốc chỉ có ngày giờ nên sót các dòng trùng giờ. Ghép thêm khóa…". **Sửa câu** ghép lại đủ `COALESCE(zs.UpdateTime, zs.CreateTime)` và `'_' + zs.ID`, 3/3 xanh.
  ```sql
  SELECT TOP (@pageSize)
      zs.UpdateTime AS __lastKey,  -- sai: chỉ có ngày giờ
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
  WHERE zs.UpdateTime > @lastKey  -- sai: chỉ có ngày giờ
  ORDER BY __lastKey ASC
  ```
- [ ] **3.7** So cột mốc bằng `>=`.
  **Thấy:** "So cột mốc với biến mốc phải dùng dấu >; dùng >= thì dòng cuối bị gửi lặp mỗi lượt." Dải **Quy chuẩn mốc** vẫn 3/3, vì lỗi này chỉ máy chủ báo khi lưu.
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
  WHERE CONVERT(varchar(23), COALESCE(zs.UpdateTime, zs.CreateTime), 121) + '_' + zs.ID >= @lastKey  -- sai: dùng >=
  ORDER BY __lastKey ASC
  ```
- [ ] **3.8** Ghép ngày giờ sai định dạng.
  **Thấy:** "Cột mốc ghép ngày giờ sai định dạng nên sắp sai thứ tự, có thể bỏ sót dòng. Dùng CONVERT(varchar(23), <cột thời gian>, 121)."
  ```sql
  SELECT TOP (@pageSize)
      CONVERT(varchar(10), COALESCE(zs.UpdateTime, zs.CreateTime), 103) + '_' + zs.ID AS __lastKey,  -- sai: ngày trước tháng
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
  WHERE CONVERT(varchar(10), COALESCE(zs.UpdateTime, zs.CreateTime), 103) + '_' + zs.ID > @lastKey  -- sai: ngày trước tháng
  ORDER BY __lastKey ASC
  ```
- [ ] **3.9** Có cột mốc nhưng không dùng biến mốc.
  **Thấy:** "Câu có cột mốc nhưng không dùng biến mốc (nguồn Theo Mốc đang chạy) nên lượt nào cũng gửi lại cùng dữ liệu…"
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
  ORDER BY __lastKey ASC  -- sai: đã bỏ WHERE so với @lastKey
  ```
- [ ] **3.10** Cột mốc là cột chuỗi trơn.
  **Thấy:** "Cột mốc phải viết đúng công thức CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID, phần thời gian phải có CreateTime; viết khác thì có thể bỏ sót dòng." Rê chuột vào **Quy chuẩn mốc**: mục *Có cột mốc* đỏ; bấm **Sửa câu** thì câu về đúng công thức, 3/3 xanh.
  ```sql
  SELECT TOP (@pageSize)
      zs.ID AS __lastKey,  -- sai: cột chuỗi trơn
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
  WHERE zs.ID > @lastKey  -- sai: cột chuỗi trơn
  ORDER BY __lastKey ASC
  ```
- [ ] **3.11** Ép số sang chuỗi làm cột mốc.
  **Thấy:** Lỗi như 3.10. **Sửa câu** ghép thời gian trước và giữ phần ép kiểu.
  ```sql
  SELECT TOP (@pageSize)
      CAST(z.FromKmNumber AS varchar(10)) AS __lastKey,  -- sai: ép số sang chuỗi
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
  WHERE CAST(z.FromKmNumber AS varchar(10)) > @lastKey  -- sai: ép số sang chuỗi
  ORDER BY __lastKey ASC
  ```
- [ ] **3.12** Phần thời gian không có `CreateTime`.
  **Thấy:** Lỗi như 3.10. **Sửa câu** đổi thành `COALESCE(zs.DetectWay, zs.CreateTime)`.
  ```sql
  SELECT TOP (@pageSize)
      CONVERT(varchar(23), zs.DetectWay, 121) + '_' + zs.ID AS __lastKey,  -- sai: không có CreateTime
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
  WHERE CONVERT(varchar(23), zs.DetectWay, 121) + '_' + zs.ID > @lastKey  -- sai: không có CreateTime
  ORDER BY __lastKey ASC
  ```

- [ ] **3.13** Phần khóa là cột có thể trống.
  **Thấy:** Lỗi như 3.10. **Sửa câu** đổi phần khóa thành `zs.ID`.
  ```sql
  SELECT TOP (@pageSize)
      CONVERT(varchar(23), COALESCE(zs.UpdateTime, zs.CreateTime), 121) + '_' + z.Name AS __lastKey,  -- sai: tên đoạn đường có thể trống
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
  WHERE CONVERT(varchar(23), COALESCE(zs.UpdateTime, zs.CreateTime), 121) + '_' + z.Name > @lastKey  -- sai: tên đoạn đường có thể trống
  ORDER BY __lastKey ASC
  ```
- [ ] **3.14** Câu dùng `OFFSET`.
  **Thấy:** "Câu nối đuôi không được dùng OFFSET…", câu không được lưu.
  ```sql
  SELECT
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
  OFFSET 1 ROWS FETCH NEXT @pageSize ROWS ONLY  -- sai: dòng bị bỏ qua không bao giờ được gửi
  ```
- [ ] **3.15** Gộp hai nguồn bằng `UNION` dùng chung `@lastKey`.
  **Thấy:** "Câu gộp nhiều nguồn không dùng chung một mốc…", câu không được lưu.
  ```sql
  SELECT TOP (@pageSize) u.__lastKey, u.zoneId
  FROM (
      SELECT CONVERT(varchar(23), COALESCE(zs.UpdateTime, zs.CreateTime), 121) + '_' + zs.ID AS __lastKey, zs.ZoneId AS zoneId
      FROM dbo.TmsZoneStatus zs
      WHERE CONVERT(varchar(23), COALESCE(zs.UpdateTime, zs.CreateTime), 121) + '_' + zs.ID > @lastKey
      UNION ALL  -- sai: hai nguồn dùng chung một mốc
      SELECT CONVERT(varchar(23), COALESCE(e.UpdateTime, e.CreateTime), 121) + '_' + e.ID, e.Code
      FROM TmsEquipment e
      WHERE CONVERT(varchar(23), COALESCE(e.UpdateTime, e.CreateTime), 121) + '_' + e.ID > @lastKey
  ) u
  ORDER BY u.__lastKey ASC
  ```
- [ ] **3.16** Câu con có `TOP` sắp theo cột khác cột mốc (dạng mẫu của sếp).
  **Thấy:** "Câu con có TOP phải sắp tăng dần theo cột mốc…", câu không được lưu.
  ```sql
  SELECT T.__lastKey, T.zoneId
  FROM (
      SELECT TOP (@pageSize) CONVERT(varchar(23), COALESCE(zs.UpdateTime, zs.CreateTime), 121) + '_' + zs.ID AS __lastKey, zs.ZoneId AS zoneId
      FROM dbo.TmsZoneStatus zs
      WHERE CONVERT(varchar(23), COALESCE(zs.UpdateTime, zs.CreateTime), 121) + '_' + zs.ID > @lastKey
      ORDER BY zs.AverageSpeed  -- sai: câu con chọn dòng theo cột khác cột mốc
  ) T
  ORDER BY T.__lastKey ASC
  ```

**Thấy chung:** chọn lại gói thì câu và bảng tham số vẫn là bản cũ.

## 4. Cách viết khác cho dòng chưa có thời gian: câu hai nhóm mốc *(tùy chọn)*

Câu ở các mục trên đã gửi đủ 299 dòng nhờ `COALESCE` nên không cần cách này; mục này chỉ để kiểm tính năng nhóm mốc (một câu nhiều mốc, mỗi mốc một tên). Câu hai nhóm tách riêng hai mốc: nhóm `upd` theo `UpdateTime`, nhóm `new` là các dòng `UpdateTime` còn trống theo `CreateTime` (cả 35 dòng đều có `CreateTime`).

- [ ] **4.1** Đăng ký gửi đang tắt. Tab PC08: xóa hết khung soạn, dán câu dưới → **Lưu & phân tích**.
  **Thấy:** một hộp xác nhận gộp cả cảnh báo thiếu 7 trường đang dùng trong `PC08_101COMMONDATA_OUT` (`fromLocationKm`, `fromLocationMet`, `toLocationKm`, `toLocationMet`, `laneId`, `speedLimit`, `vehicleCount`) và khung vàng "Câu truy vấn đổi công thức hoặc bảng nguồn của cột mốc: bỏ Mốc đang chạy của 1 đối tác (…), lượt chạy tới gửi lại dữ liệu từ Mốc bắt đầu." → **Xác nhận**. Sau đó:
  - Lưu thành công.
  - Bảng tham số có `@lastKey_upd`, `@lastKey_new` (*theo key*), **đã chọn sẵn nguồn**; không còn `@lastKey`.

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
- [ ] **4.2** Bảng tham số: `@pageSize` = **20** → **Lưu & phân tích**. Bật đăng ký gửi, chờ một lượt.
  **Thấy:** gửi **299** bản ghi (264 + 35), *Số bản ghi* **20** (14 lần) rồi **19**.
- [ ] **4.3** Chờ thêm một lượt. **Thấy:** không gửi lặp.

## 5. Gửi lỗi thì giữ mốc *(tùy chọn)*

Kênh API `localhost:9999` không có gì nghe trên máy, nên dùng để thử gửi lỗi.

- [ ] **5.1** Tắt đăng ký gửi. Tab **Cấu hình** → dòng PC08 → **Ngắt kết nối** → sửa PC08: **Loại kênh kết nối** API, **Địa chỉ (IP/host)** `localhost`, **Cổng** `9999`, endpoint để trống → Lưu → **Kết nối**.
- [ ] **5.2** Xóa mốc đang chạy của PC08 trước khi thử:
  - Nếu vừa làm mục 4: ở tab PC08 xóa hết khung soạn, dán lại câu ở bước 0.5 → `@pageSize` = **21** → **Lưu & phân tích** → hộp xác nhận đổi công thức hoặc bảng nguồn của cột mốc → **Xác nhận** (mốc của PC08 tự bị xóa).
  - Nếu bỏ qua mục 4: tab **Cấu hình gói tin** → gói `101_commonData` → tab PC08 → khối **4. Mốc đang chạy** → bấm **↺ Đặt lại** ở dòng PC08 → xác nhận.
  Sau đó bật đăng ký gửi, chờ một lượt.
  **Thấy:** **Cảnh báo & lỗi** có lỗi không kết nối được tới `localhost:9999`; *Nhật ký truyền nhận* có lần gửi lỗi.
- [ ] **5.3** Tắt đăng ký gửi. Dòng PC08 → **Ngắt kết nối** → sửa về **Xuất tệp tin** như bước 0.2 (địa chỉ, cổng để nguyên) → Lưu → **Kết nối**. Bật đăng ký gửi, chờ một lượt.
  **Thấy:** gửi đủ 299 bản ghi: lần gửi lỗi không làm mốc tiến.

## 6. Mẫu gợi ý select-incremental (không lưu)

- [ ] **6.1** Chọn một gói không bị khóa → khung soạn SQL: Cmd+A, Delete, gõ `SEL`, chọn **select-incremental**.
  **Thấy:** chèn khung câu `SELECT TOP (@pageSize) t.Column AS aliasName, CONVERT(varchar(23), COALESCE(t.UpdateTime, t.CreateTime), 121) + '_' + t.ID AS __lastKey FROM TableName t WHERE CONVERT(varchar(23), COALESCE(t.UpdateTime, t.CreateTime), 121) + '_' + t.ID > @lastKey ORDER BY __lastKey ASC`; không còn `@lastTime`, `__lastTime`; phím Tab nhảy qua các ô cần đổi.
- [ ] **6.2** Chọn lại gói, hộp hỏi bỏ thay đổi → **Xác nhận**.

## 7. Câu mẫu gói nối đuôi có sẵn bộ biến (không lưu; làm sau khi chạy tệp seed)

- [ ] **7.1** Mở lần lượt gói `103_vdsData`, `104_weatherData`, `106_wimData`, `107_incidentData`, `109_etcData` → tab **Mẫu chung**.
  **Thấy:** khung soạn là câu một chuỗi ghép, không còn `@lastTime`; bảng **3. Tham số điều kiện** có `@lastKey` nguồn Theo Mốc đang chạy, không còn `@lastTime`; dải **Quy chuẩn mốc** 3/3 xanh. DB đã có bộ biến cũ thì còn dòng `@pageSize` như đang lưu, câu mẫu dùng `TOP 100` nên dòng này không tác dụng.

## 8. Dọn

- [ ] **8.1** Tab **Cấu hình** → tắt đăng ký gửi gói 101 của PC08 (giữ đăng ký để dùng thật sau này).
- [ ] **8.2** Tab **Cấu hình gói tin** → gói `101_commonData` → tab PC08 → **Dùng lại mẫu chung** → xác nhận. Câu mẫu gói 101 cùng công thức cột mốc với câu riêng ở bước 0.5 nên mốc của PC08 vẫn dùng tiếp, không gửi lại từ đầu. Làm lại kịch bản từ đầu thì đặt lại mốc ở khối **4. Mốc đang chạy** (bước 0.3b) trước.
- [ ] **8.3** Giữ PC08 ở kênh **Xuất tệp tin** cho tới khi có địa chỉ nhận thật; có địa chỉ thì ở tab **Cấu hình** bấm **Ngắt kết nối**, sửa sang kênh API với địa chỉ, cổng thật rồi **Kết nối** lại.

## 9. Nhiều Worker trên máy chủ *(làm khi triển khai)*

Mục tiêu: khởi động nhiều dịch vụ trên máy chủ mà vẫn chạy đúng, chưa cần đo tải. Các bước clone ở `bangiao/HUONG-DAN-CLONE-WORKER.md`.

- [ ] **9.0** Tạo và khởi động thử một dịch vụ `ShareDataWorker-A101`.
  **Thấy:** `sc.exe query ShareDataWorker-A101` báo `RUNNING`, sau 1 phút vẫn `RUNNING`. Nếu không chạy được thì dừng, chưa tạo các dịch vụ còn lại.

- [ ] **9.1** Tạo và khởi động đủ 4 dịch vụ `SendOnly` (`A101` đến `A104`) và 1 dịch vụ `ReceiveOnly`, cùng CSDL.
  **Thấy:** cả 5 dịch vụ chạy, không tự dừng; log không có lỗi kết nối hay lỗi vai trò.
- [ ] **9.2** Bật đăng ký gửi của 4 đối tác, chờ vài lượt.
  **Thấy:** **Lịch sử chia sẻ → Nhật ký truyền nhận** có lượt gửi thành công của đủ 4 đối tác; **Cảnh báo & lỗi** không có cảnh báo mới; dữ liệu mỗi đối tác đủ, không lẫn nhau.
- [ ] **9.3** Dừng rồi khởi động lại một dịch vụ gửi. **Thấy:** gửi tiếp từ mốc, không gửi lại từ đầu.

## 10. Khối 4 Mốc đang chạy *(làm trước mục 8, khi PC08 còn cấu hình riêng gói 101)*

- [ ] **10.1** Tab **Cấu hình gói tin** → gói `101_commonData` → tab **Mẫu chung**. **Thấy:** khối **4. Mốc đang chạy của đối tác dùng mẫu chung** không có dòng PC08 (PC08 đang có cấu hình riêng). Chuyển sang tab PC08. **Thấy:** khối **4. Mốc đang chạy của <tên đối tác PC08>** có đúng một dòng PC08, cột *Mốc đang chạy* là nguyên chuỗi như trong CSDL, ví dụ `2026-10-22 17:11:28.537_<ID>`.
- [ ] **10.2** Đăng ký gửi của PC08 đang bật. **Thấy:** **✎ Đặt mốc** và **↺ Đặt lại** mờ, rê chuột báo phải tắt đăng ký trước.
- [ ] **10.3** Tắt đăng ký, tải lại trang, **✎ Đặt mốc**. **Thấy:** hộp **Đặt Mốc đang chạy — <tên đối tác PC08>**, dòng "Mốc đang chạy: <chuỗi mốc>". Chọn ngày giờ **sớm hơn** mốc. **Thấy:** chữ đỏ "Gửi lại dữ liệu từ … tới …: đối tác nhận trùng phần này.". Chọn ngày giờ **muộn hơn**. **Thấy:** "Bỏ qua dữ liệu từ … tới …: phần này không bao giờ được gửi.". Chọn lại một ngày giờ bất kỳ → **Xác nhận**.
  **Thấy:** "Đã cập nhật mốc gửi dữ liệu cho đối tác."; cột mốc thành `yyyy-MM-dd HH:mm:ss.000_` theo giờ đã chọn (phần sau `_` trống).
- [ ] **10.4** **↺ Đặt lại** → hộp hỏi "Bỏ Mốc đang chạy của <tên đối tác PC08>? Lượt gửi tới gửi lại …, phần đã gửi sẽ bị gửi trùng." → xác nhận. **Thấy:** "Đã bỏ Mốc đang chạy."; dòng PC08 ghi "Chưa có, lượt gửi tới lấy …" theo Mốc bắt đầu của câu.
- [ ] **10.5** Bật lại đăng ký, chờ một lượt. **Thấy:** gửi lại từ **Mốc bắt đầu**; tải lại trang thì mốc đã tiến.
- [ ] **10.6** Tab **Mẫu chung**, bảng **3. Tham số điều kiện**. **Thấy:** ô giá trị của `@lastKey` để trống ghi "Đối tác chưa có mốc: gửi toàn bộ"; chọn ngày giờ thì phía trên ô ghi "Đối tác chưa có mốc: từ". Tab PC08 vẫn ghi như mục 0.6. Không lưu, tải lại trang để bỏ thay đổi.

---

## Không làm được trên giao diện (đã có test tự động)

| Tình huống | Vì sao không làm tay được |
|:--|:--|
| Câu riêng còn `@lastTime` sửa bằng nút **Sửa câu** | `DEV_ITS015` không có câu riêng dạng này để thử |
| Dữ liệu nguồn mới hoặc vừa sửa thì được gửi tiếp | `TmsZoneStatus` trên `DEV_ITS015` không có dịch vụ nào cập nhật (mới nhất 22/10/2026); không sửa tay dữ liệu nguồn trong lúc test |
| Tắt đăng ký rồi bấm Đặt mốc / Đặt lại đúng lúc Worker đang gửi dở | Kênh File gửi xong ngay, không kịp bấm |
| Câu bị sửa thẳng trong CSDL (cảnh báo ESH-1306, ESH-1308) | Phải sửa ngoài màn hình |
| `JOIN` nhân dòng làm trùng giá trị cột mốc (cảnh báo ESH-1309) | Cần dữ liệu một cha nhiều con; bảng trong kịch bản không có |
| Xóa rồi tạo lại đối tác, gói tin cùng mã thì gửi lại từ đầu | Làm được nhưng phải xóa cấu hình đang dùng để test |
| API từ chối đổi mã đối tác | Giao diện đã khóa ô mã |
