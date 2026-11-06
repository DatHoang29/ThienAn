# Kịch bản test nối đuôi ShareData (chỉ thao tác trên giao diện)

Làm từ trên xuống, đánh dấu từng ô. Mỗi bước: **Làm** → **Thấy**. Thấy khác thì dừng, chụp màn hình gửi lại.

- CSDL máy chủ mới `10.10.12.80/DEV_ITS015` (cập nhật 06/11/2026). Chạy WebAPI, Worker bản mới nhất trỏ vào CSDL này (Worker: `DefaultConnection` = `DEV_ITS015`, `InboundConnection` = `DEV_ITS015_Inbound`).
- Đối tác **Phòng Cảnh sát giao thông (PC08)** (mã `PC08`), gói **`101_commonData`**. Dùng sẵn hồ sơ ánh xạ `PC08_101COMMONDATA_OUT`. PC08 chưa có đăng ký nào: bước 0.3 tạo đăng ký **gửi** gói 101 chu kỳ **30 giây**.
- Câu mẫu gói 101 là câu nối đuôi một chuỗi ghép (PA1, sếp chốt 02/11/2026) `thời gian_khóa` ở cột `__lastKey`. Kịch bản tạo cấu hình riêng cho PC08 và viết câu trên đó; mẫu chung không đổi.
- PC08 chưa có địa chỉ nhận nên trong lúc test dùng **kênh File**: mỗi lần gửi được lưu thành một tệp bằng chứng, không cần máy nhận.
- Dữ liệu nguồn `TmsZoneStatus` trên `DEV_ITS015` (đếm ngày 06/11/2026): **299** dòng; **264** dòng có `UpdateTime`, mới nhất `22/10/2026 17:11:28.537`; **35** dòng chưa có `UpdateTime` (đều có `CreateTime`). Cột mốc lấy `CreateTime` khi `UpdateTime` trống nên gửi đủ cả 299 dòng. Câu JOIN không nhân dòng (299 giá trị cột mốc khác nhau).
- Dòng **trùng giờ**: 139 dòng đầu cùng giờ `03/06/2026 11:16:10.730`; ba cặp trùng giờ đứng thứ **231–232** (`19/08/2026 14:57:16.920`), 255–256, 291–292.
- Xem kết quả ở **Theo dõi chia sẻ → Lịch sử chia sẻ → Nhật ký truyền nhận** (cột *Số bản ghi*). Lượt sau không gửi lặp nghĩa là mốc đã tiến. Xem mốc trực tiếp ở tab **Mốc gửi**, kiểm ở mục 12.
- Màn hình dạng tab: **Chia sẻ dữ liệu → Cấu hình chia sẻ** gồm các tab *Cấu hình, Cấu hình gói tin, Ánh xạ dữ liệu, Bộ mã Chuẩn hóa, Mốc gửi, Nguồn sự kiện*; **Theo dõi chia sẻ** gồm *Lịch sử chia sẻ, Cảnh báo & lỗi*.
- Đăng ký chạy 30 giây một lượt; mỗi lượt gửi tối đa 20 trang (`outbound_max_pages_per_run`). "Chờ một lượt" là chờ khoảng 1 phút.

---

## 0. Chuẩn bị

- [ ] **0.1** Trên `DEV_ITS015` đã chạy các tệp seed theo `sql/README.md` (gói tin, câu mẫu, đối tác 14, ánh xạ 15). Gói 101 đã **Dò cột trả về** → **Lưu** ở tab Mẫu chung (danh sách trường không trống). Mở web, tải lại hẳn trang (Cmd+Shift+R).
- [ ] **0.2** Tab **Cấu hình** → sửa đối tác **PC08**: kênh kết nối **File**, để trống *Thư mục*, định dạng JSON → Lưu → **Kết nối**.
- [ ] **0.3** Tab **Cấu hình** → PC08 → **Gửi đi** → **Tạo đăng ký gửi**: gói `101_commonData`, chế độ **Định kỳ** 30 giây → Lưu. Để **tắt**.
  **Thấy:** cột *Hồ sơ ánh xạ* hiện `PC08_101COMMONDATA_OUT`.
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
  **Thấy:** lưu thành công. Ô giá trị của `@lastKey` là ô chọn ngày giờ, không có ô gõ tay. Trang này chỉ đặt Mốc bắt đầu: đã có mốc thì ô bị khóa (xem 2.1). Trên tiêu đề mục 3, dải **Quy chuẩn mốc** 3/3 xanh. Cột **Nguồn giá trị**: `@lastKey` là **Nối tiếp lần trước theo key**, `@pageSize` là **Giá trị nhập** và bị khóa. Bấm thanh tiêu đề mục 3 để thu gọn bảng, dải **Quy chuẩn mốc** vẫn hiện.

---

## 1. Gửi dữ liệu có sẵn, chia trang, dòng trùng giờ ở ranh giới trang

`@pageSize` = 21 làm trang 11 kết thúc ở dòng 231, dòng 232 cùng giờ sang trang 12; nhóm 139 dòng đầu cùng giờ trải qua 7 trang.

- [ ] **1.1** Tab **Cấu hình** → PC08 → bật đăng ký gửi gói 101 (**Bật/Tắt**). Chờ một lượt.
  **Thấy:**
  - **Nhật ký truyền nhận**: 15 lần gửi thành công, *Số bản ghi* **21** (14 lần) rồi **5** (đủ 299 dòng; tổng thiếu dòng nào nghĩa là mất dòng trùng giờ).
  - **Lịch sử chia sẻ → Nhật ký cấu hình**: không có dòng "Câu truy vấn nối đuôi có rủi ro".
  - Thư mục bằng chứng có 15 tệp JSON dạng `header` + `data` của hồ sơ `PC08_101COMMONDATA_OUT`.
- [ ] **1.2** Chờ thêm một lượt. **Thấy:** không có lần gửi mới (không gửi lặp).

## 2. Khóa khi đăng ký gửi đang bật (đăng ký vẫn bật)

- [ ] **2.1** Tab **Cấu hình gói tin** → chọn lại gói `101_commonData` → tab PC08.
  **Thấy:** **Lưu & phân tích** mờ, có biểu tượng khóa ghi "Đang có đăng ký gửi bật sử dụng (PC08)…". Ô giá trị của `@lastKey` bị khóa, có chữ "Đã có mốc, gửi tiếp", không hiện giá trị mốc.

## 5. Câu sai bị chặn khi lưu

- [ ] **5.0** Tắt đăng ký gửi. Chọn lại gói `101_commonData` → tab PC08.

Mỗi ca: xóa hết khung soạn (Cmd+A, Delete), dán câu của ca → **Lưu & phân tích** → xem thông báo → chọn lại gói, hộp hỏi bỏ thay đổi → **Xác nhận**. Câu mỗi ca chỉ khác câu ở bước 0.5 đúng dòng có chú thích `-- sai`. PC08 đã có mốc từ mục 1 nhưng câu sai bị từ chối trước, nên không hiện hộp xác nhận đổi công thức.

- [ ] **5.1** `WHERE` chỉ so `ID`, không so đúng cột mốc.
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
- [ ] **5.2** `WHERE` dùng `zs.CreateTime`, khác biểu thức cột mốc ở `SELECT`.
  **Thấy:** Lỗi như 5.1.
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
- [ ] **5.3** `ORDER BY` không theo cột mốc.
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
- [ ] **5.4** Sắp giảm dần.
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
- [ ] **5.5** Cột mốc dùng `zs.UpdateTime` trơn, không có `COALESCE`.
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
- [ ] **5.6** Cột mốc chỉ có ngày giờ, không ghép khóa.
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
- [ ] **5.7** So cột mốc bằng `>=`.
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
- [ ] **5.8** Ghép ngày giờ sai định dạng.
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
- [ ] **5.9** Có cột mốc nhưng không dùng biến mốc.
  **Thấy:** "Câu có cột mốc nhưng không dùng biến mốc (nguồn Nối tiếp lần trước theo key) nên lượt nào cũng gửi lại cùng dữ liệu…"
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
- [ ] **5.10** Cột mốc là cột chuỗi trơn.
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
- [ ] **5.11** Ép số sang chuỗi làm cột mốc.
  **Thấy:** Lỗi như 5.10. **Sửa câu** ghép thời gian trước và giữ phần ép kiểu.
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
- [ ] **5.12** Phần thời gian không có `CreateTime`.
  **Thấy:** Lỗi như 5.10. **Sửa câu** đổi thành `COALESCE(zs.DetectWay, zs.CreateTime)`.
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

- [ ] **5.13** Phần khóa là cột có thể trống.
  **Thấy:** Lỗi như 5.10. **Sửa câu** đổi phần khóa thành `zs.ID`.
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
- [ ] **5.14** Câu dùng `OFFSET`.
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
- [ ] **5.15** Gộp hai nguồn bằng `UNION` dùng chung `@lastKey`.
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
- [ ] **5.16** Câu con có `TOP` sắp theo cột khác cột mốc (dạng mẫu của sếp).
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

## 6. Cách viết khác cho dòng chưa có thời gian: câu hai nhóm mốc *(tùy chọn)*

Câu ở các mục trên đã gửi đủ 299 dòng nhờ `COALESCE` nên không cần cách này; mục này chỉ để kiểm tính năng nhóm mốc (một câu nhiều mốc, mỗi mốc một tên). Câu hai nhóm tách riêng hai mốc: nhóm `upd` theo `UpdateTime`, nhóm `new` là các dòng `UpdateTime` còn trống theo `CreateTime` (cả 35 dòng đều có `CreateTime`).

- [ ] **6.1** Đăng ký gửi đang tắt. Tab PC08: xóa hết khung soạn, dán câu dưới → **Lưu & phân tích**.
  **Thấy:** hộp xác nhận "Câu truy vấn đổi công thức cột mốc nối đuôi… (PC08)…" → **Xác nhận**. Sau đó:
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
- [ ] **6.2** Bảng tham số: `@pageSize` = **20** → **Lưu & phân tích**. Bật đăng ký gửi, chờ một lượt.
  **Thấy:** gửi **299** bản ghi (264 + 35), *Số bản ghi* **20** (14 lần) rồi **19**.
- [ ] **6.3** Chờ thêm một lượt. **Thấy:** không gửi lặp.

## 7. Gửi lỗi thì giữ mốc *(tùy chọn, làm sau mục 6)*

Bước 7.2 xóa mốc của PC08 bằng cách đổi công thức từ câu hai nhóm (mục 6) về câu ở bước 0.5, vì màn hình không có nút Đặt lại. Bỏ qua mục 6 thì bỏ qua cả mục này.

Kênh API `localhost:9999` không có gì nghe trên máy, nên dùng để thử gửi lỗi.

- [ ] **7.1** Tắt đăng ký gửi. Tab **Cấu hình** → sửa PC08 sang kênh **API**, địa chỉ `localhost`, cổng `9999`, endpoint để trống → Lưu → **Kết nối**.
- [ ] **7.2** Tab PC08: xóa hết khung soạn, dán lại câu ở bước 0.5 → **Lưu & phân tích** → hộp xác nhận đổi công thức → **Xác nhận** (mốc của PC08 bị xóa). Bật đăng ký gửi, chờ một lượt.
  **Thấy:** **Cảnh báo & lỗi** có lỗi không kết nối được tới `localhost:9999`; *Nhật ký truyền nhận* có lần gửi lỗi.
- [ ] **7.3** Tắt đăng ký gửi. Sửa PC08 về kênh **File** như bước 0.2 → Lưu → **Kết nối**. Bật đăng ký gửi, chờ một lượt.
  **Thấy:** gửi đủ 299 bản ghi: lần gửi lỗi không làm mốc tiến.

## 8. Mẫu gợi ý select-incremental (không lưu)

- [ ] **8.1** Chọn một gói không bị khóa → khung soạn SQL: Cmd+A, Delete, gõ `SEL`, chọn **select-incremental**.
  **Thấy:** chèn khung câu `SELECT TOP (@pageSize) t.Column AS aliasName, CONVERT(varchar(23), COALESCE(t.UpdateTime, t.CreateTime), 121) + '_' + t.ID AS __lastKey FROM TableName t WHERE CONVERT(varchar(23), COALESCE(t.UpdateTime, t.CreateTime), 121) + '_' + t.ID > @lastKey ORDER BY __lastKey ASC`; không còn `@lastTime`, `__lastTime`; phím Tab nhảy qua các ô cần đổi.
- [ ] **8.2** Chọn lại gói, hộp hỏi bỏ thay đổi → **Xác nhận**.

## 9. Câu mẫu gói nối đuôi có sẵn bộ biến (không lưu; làm sau khi chạy tệp seed)

- [ ] **9.1** Mở lần lượt gói `103_vdsData`, `104_weatherData`, `106_wimData`, `107_incidentData`, `109_etcData` → tab **Mẫu chung**.
  **Thấy:** khung soạn là câu một chuỗi ghép, không còn `@lastTime`; bảng **3. Tham số điều kiện** có `@lastKey` nguồn Nối tiếp lần trước theo key, không còn `@lastTime`; dải **Quy chuẩn mốc** 3/3 xanh. DB đã có bộ biến cũ thì còn dòng `@pageSize` như đang lưu, câu mẫu dùng `TOP 100` nên dòng này không tác dụng.

## 10. Dọn

- [ ] **10.1** Tab **Cấu hình** → tắt đăng ký gửi gói 101 của PC08 (giữ đăng ký để dùng thật sau này).
- [ ] **10.2** Tab **Cấu hình gói tin** → gói `101_commonData` → tab PC08 → **Dùng lại mẫu chung** → xác nhận. Câu mẫu gói 101 cùng công thức cột mốc với câu riêng ở bước 0.5 nên mốc của PC08 vẫn dùng tiếp, không gửi lại từ đầu. Làm lại kịch bản từ đầu thì đặt lại mốc ở tab **Mốc gửi** (mục 12.4) trước.
- [ ] **10.3** Giữ PC08 ở kênh **File** cho tới khi có địa chỉ nhận thật; có địa chỉ thì sửa sang kênh **API** ở tab **Cấu hình**.

## 11. Nhiều Worker trên máy chủ *(làm khi triển khai)*

Mục tiêu: khởi động nhiều dịch vụ trên máy chủ mà vẫn chạy đúng, chưa cần đo tải. Các bước clone ở `bangiao/HUONG-DAN-CLONE-WORKER.md`.

- [ ] **11.0** Tạo và khởi động thử một dịch vụ `ShareDataWorker-A101`.
  **Thấy:** `sc.exe query ShareDataWorker-A101` báo `RUNNING`, sau 1 phút vẫn `RUNNING`. Nếu không chạy được thì dừng, chưa tạo các dịch vụ còn lại.

- [ ] **11.1** Tạo và khởi động đủ 4 dịch vụ `SendOnly` (`A101` đến `A104`) và 1 dịch vụ `ReceiveOnly`, cùng CSDL.
  **Thấy:** cả 5 dịch vụ chạy, không tự dừng; log không có lỗi kết nối hay lỗi vai trò.
- [ ] **11.2** Bật đăng ký gửi của 4 đối tác, chờ vài lượt.
  **Thấy:** **Lịch sử chia sẻ → Nhật ký truyền nhận** có lượt gửi thành công của đủ 4 đối tác; **Cảnh báo & lỗi** không có cảnh báo mới; dữ liệu mỗi đối tác đủ, không lẫn nhau.
- [ ] **11.3** Dừng rồi khởi động lại một dịch vụ gửi. **Thấy:** gửi tiếp từ mốc, không gửi lại từ đầu.

## 12. Trang Mốc gửi *(làm trước mục 10, khi PC08 còn cấu hình riêng gói 101)*

- [ ] **12.1** Mở **Cấu hình chia sẻ**. **Thấy:** có tab **Mốc gửi** (tệp seed 13 đã chạy trên `DEV_ITS015`).
- [ ] **12.2** Mở tab **Mốc gửi**, chọn gói `101_commonData` khi đăng ký gửi của PC08 đang bật.
  **Thấy:** dòng PC08 có nhãn **Cấu hình riêng**, hiện mốc đang chạy; **✎ Đặt mốc** và **↺ Đặt lại** mờ, rê chuột báo phải tắt đăng ký trước.
- [ ] **12.3** Tắt đăng ký, bấm làm mới, **✎ Đặt mốc**, chọn một ngày giờ → **Xác nhận**.
  **Thấy:** "Đã cập nhật mốc gửi dữ liệu cho đối tác."; cột mốc thành `<giờ đã chọn>.000_`.
- [ ] **12.4** **↺ Đặt lại** → xác nhận. **Thấy:** "Đã đặt lại mốc đã lưu."; dòng PC08 ghi "Chưa có mốc".
- [ ] **12.5** Bật lại đăng ký, chờ một lượt. **Thấy:** gửi lại từ **Mốc lần đầu**, bấm làm mới thì mốc đã tiến.

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
