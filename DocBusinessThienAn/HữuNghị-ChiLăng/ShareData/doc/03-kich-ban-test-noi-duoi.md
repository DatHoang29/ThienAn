# Kịch bản test nối đuôi ShareData

Làm từ trên xuống. Mỗi bước: **Làm** → **Thấy**. Thấy khác thì dừng, chụp màn hình.

- **Môi trường:** CSDL `DEV_ITS015`. WebAPI, Worker chạy bản mới nhất (có phần sửa 08–09/11/2026). FE tải lại hẳn trang (Cmd+Shift+R).
- **Dữ liệu test:** đối tác PC08, gói `101_commonData`, kênh Dịch vụ WebAPI gửi về API nhận của chính máy chủ DEV. Bảng `TmsZoneStatus` có 299 dòng (35 dòng chưa có `UpdateTime`), nhiều dòng trùng giờ.
- **Chu kỳ:** đăng ký gửi 30 giây một lượt, mỗi lượt gửi 1 trang `@pageSize` dòng.
- **Xem kết quả:** **Lịch sử chia sẻ → Nhật ký truyền nhận** (cột *Số bản ghi*). Mốc xem ở khối **4. Mốc đang chạy**, tab **Cấu hình gói tin**.

## 0. Chuẩn bị

- [ ] **0.1** Đã chạy seed theo `sql/README.md`: số 2 (câu mẫu), 13 (menu), 14 (đối tác), 15 (bộ mã), 16 (ánh xạ) và tệp `sharedata-dich-thuat-SysTerminology.sql`. **Thấy:** tab **Bộ mã Chuẩn hóa** có 9 bộ mã.
- [ ] **0.2** Tab **Cấu hình** → PC08 → **Ngắt kết nối** → kênh **Dịch vụ WebAPI**: địa chỉ `https://` + IP máy chủ DEV, cổng WebAPI (không phải cổng giao diện), endpoint `/api/sharedata/sharedatainbound/receive`, bật **Bỏ qua kiểm tra chứng chỉ SSL/TLS** → Lưu → **Kết nối**. **Thấy:** ô Loại kênh kết nối chỉ có Dịch vụ WebAPI, Máy chủ FTP, Hàng đợi tin Kafka.
- [ ] **0.3** PC08 → **Gửi đi**: có đăng ký gói 101 (30 giây) thì để **tắt**; chưa có thì tạo rồi để tắt. **Thấy:** hồ sơ ánh xạ `PC08_101COMMONDATA_OUT`.
- [ ] **0.4** **Cấu hình gói tin** → gói 101 → khối 4: dòng PC08 có mốc thì bấm **↺ Đặt lại** → xác nhận.
- [ ] **0.5** **Tạo cấu hình riêng +** → PC08, sao chép Mẫu chung. Xóa khung soạn, dán **câu gốc** dưới đây → **Lưu & phân tích**. **Thấy:**
  - bảng tham số có `@pageSize` (100) và `@lastKey` (*Theo Mốc đang chạy*);
  - dải **Quy chuẩn mốc** 3/3 xanh.
- [ ] **0.6** `@pageSize` = **21** → **Lưu & phân tích**. **Thấy:** khối 4 dòng PC08 ghi "Chưa có, lượt gửi tới lấy toàn bộ dữ liệu".

**Câu gốc** (các ca ở mục 3 sửa từ câu này; `K` là biểu thức cột mốc ở dòng 2):

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

## 1. Gửi có chia trang

- [ ] **1.1** Bật đăng ký gửi, chờ khoảng 8 phút (15 lượt). **Thấy:**
  - 15 lần gửi thành công: 14 lần 21 dòng, 1 lần 5 dòng, đủ 299;
  - không có cảnh báo;
  - tệp JSON: `laneId` ra `ALL_LANES`/`LANE_1`/`LANE_2`, `trafficCondition` ra `FREE_FLOW`/`NORMAL`/`SLOW`/`CONGESTED`.
- [ ] **1.2** Chờ thêm 1 lượt. **Thấy:** không gửi thêm.

## 2. Khóa khi đăng ký đang bật

- [ ] **2.1** Mở lại gói 101 → tab PC08. **Thấy:**
  - **Lưu & phân tích** mờ, có biểu tượng khóa;
  - ô `@lastKey` ghi "Đã có mốc, gửi tiếp";
  - khối 4 hiện mốc `yyyy-MM-dd HH:mm:ss.fff_<ID>`; **Đặt mốc**, **Đặt lại** mờ.
- [ ] **2.2** Tab **Bộ mã Chuẩn hóa** → sửa `lane_code`. **Thấy:** báo vàng bộ mã đang dùng, bảng quy đổi bị khóa.

## 3. Câu sai bị chặn khi lưu

Tắt đăng ký trước. Mỗi ca:
1. Sửa câu gốc như cột *Sửa*.
2. Bấm **Lưu & phân tích**: phải thấy câu báo ở cột *Thấy*, câu không được lưu.
3. Chọn lại gói → bỏ thay đổi.

| Ca | Sửa | Thấy (đầu câu báo) |
|---|---|---|
| 3.1 | `WHERE zs.ID > @lastKey` | "Biểu thức so với biến lastKey trong WHERE phải viết giống hệt…" |
| 3.2 | `WHERE` dùng `CONVERT(varchar(23), zs.CreateTime, 121) + '_' + zs.ID` | Như 3.1 |
| 3.3 | `ORDER BY zs.ID ASC` | "…phải sắp xếp ORDER BY ngoài cùng theo cột [__lastKey]…" |
| 3.4 | `ORDER BY __lastKey DESC` | "…bắt buộc sắp xếp tăng dần (ASC)…" |
| 3.5 | Bỏ `COALESCE`, chỉ còn `zs.UpdateTime` (dòng 2 và `WHERE`) | "Cột mốc dùng UpdateTime trơn…"; bấm **Sửa câu** thì về 3/3 |
| 3.6 | Dòng 2: `zs.UpdateTime AS __lastKey`; `WHERE zs.UpdateTime > @lastKey` | "Cột mốc chỉ có ngày giờ…"; **Sửa câu** về 3/3 |
| 3.7 | `WHERE K >= @lastKey` | "…phải dùng dấu >…" (dải vẫn 3/3) |
| 3.8 | `varchar(23) … 121` đổi thành `varchar(10) … 103` (dòng 2 và `WHERE`) | "…ghép ngày giờ sai định dạng…" |
| 3.9 | Xóa dòng `WHERE` | "Câu có cột mốc nhưng không dùng biến mốc…" |
| 3.10 | Dòng 2: `zs.ID AS __lastKey`; `WHERE zs.ID > @lastKey` | "Cột mốc phải viết đúng công thức…"; **Sửa câu** về 3/3 |
| 3.11 | `K` thay bằng `CAST(z.FromKmNumber AS varchar(10))` (dòng 2 và `WHERE`) | Như 3.10 |
| 3.12 | `COALESCE(zs.UpdateTime, zs.CreateTime)` thay bằng `zs.DetectWay` (dòng 2 và `WHERE`) | Như 3.10; **Sửa câu** ra `COALESCE(zs.DetectWay, zs.CreateTime)` |
| 3.13 | `'_' + zs.ID` thay bằng `'_' + z.Name` (dòng 2 và `WHERE`) | Như 3.10 |
| 3.14 | Bỏ `TOP (@pageSize)`, thêm cuối câu `OFFSET 1 ROWS FETCH NEXT @pageSize ROWS ONLY` | "…không được dùng OFFSET…" |
| 3.15 | Câu A bên dưới | "…không dùng chung một mốc…" |
| 3.16 | Câu B bên dưới | "Câu con có TOP phải sắp…" |

```sql
-- Câu A (3.15): hai nguồn dùng chung @lastKey
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

-- Câu B (3.16): câu con có TOP sắp theo cột khác cột mốc
SELECT T.__lastKey, T.zoneId
FROM (
    SELECT TOP (@pageSize) CONVERT(varchar(23), COALESCE(zs.UpdateTime, zs.CreateTime), 121) + '_' + zs.ID AS __lastKey, zs.ZoneId AS zoneId
    FROM dbo.TmsZoneStatus zs
    WHERE CONVERT(varchar(23), COALESCE(zs.UpdateTime, zs.CreateTime), 121) + '_' + zs.ID > @lastKey
    ORDER BY zs.AverageSpeed
) T
ORDER BY T.__lastKey ASC
```

## 4. Câu hai nhóm mốc *(tùy chọn)*

- [ ] **4.1** Đăng ký tắt. Tab PC08: dán câu dưới → **Lưu & phân tích**. Hộp xác nhận báo thiếu 7 trường ánh xạ và bỏ mốc PC08 → **Xác nhận**. **Thấy:** bảng tham số có `@lastKey_upd`, `@lastKey_new` (*Theo Mốc đang chạy*), không còn `@lastKey`.
- [ ] **4.2** `@pageSize` = **20** → Lưu → bật đăng ký, chờ khoảng 8 phút. **Thấy:** đủ 299 dòng (14 lần 20 dòng, 1 lần 19 dòng).
- [ ] **4.3** Chờ thêm 1 lượt. **Thấy:** không gửi thêm.

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

## 5. Gửi lỗi thì giữ mốc *(tùy chọn)*

- [ ] **5.1** Tắt đăng ký. PC08 → **Ngắt kết nối** → kênh **API**, `localhost`, cổng `9999` → Lưu → **Kết nối**.
- [ ] **5.2** Bỏ mốc PC08:
  - vừa làm mục 4: dán lại câu gốc, `@pageSize` = 21, Lưu, **Xác nhận**;
  - không làm mục 4: khối 4 → **Đặt lại**.

  Bật đăng ký, chờ 1 lượt. **Thấy:** **Cảnh báo & lỗi** báo không kết nối được `localhost:9999`.
- [ ] **5.3** Tắt đăng ký. PC08 về cấu hình bước 0.2 → **Kết nối**. Bật, chờ khoảng 8 phút. **Thấy:** đủ 299 dòng (lần gửi lỗi không làm mốc tiến).

## 6. Mẫu gợi ý

- [ ] **6.1** Khung soạn: Cmd+A, Delete, gõ `SEL` → chọn **select-incremental**. **Thấy:** câu một chuỗi ghép có `COALESCE(... UpdateTime, ... CreateTime)`, không có `@lastTime`. Chọn lại gói → bỏ thay đổi.

## 7. Câu mẫu các gói khác

- [ ] **7.1** Mở gói 103, 104, 106, 107, 109 → tab **Mẫu chung**. **Thấy:** `@lastKey` *Theo Mốc đang chạy*, **Quy chuẩn mốc** 3/3 xanh.

## 8. Khối 4. Mốc đang chạy *(làm khi PC08 còn cấu hình riêng)*

- [ ] **8.1** Tab **Mẫu chung**: khối 4 không có PC08. Tab PC08: khối 4 có 1 dòng PC08, mốc nguyên chuỗi.
- [ ] **8.2** Đăng ký đang bật. **Thấy:** **Đặt mốc**, **Đặt lại** mờ.
- [ ] **8.3** Tắt đăng ký, tải lại trang → **Đặt mốc**:
  - chọn giờ **sớm hơn** mốc. **Thấy:** "Gửi lại dữ liệu từ … tới …";
  - chọn giờ **muộn hơn**. **Thấy:** "Bỏ qua dữ liệu từ … tới …";
  - **Xác nhận**. **Thấy:** "Đã cập nhật mốc gửi dữ liệu cho đối tác.", mốc thành `yyyy-MM-dd HH:mm:ss.000_`.
- [ ] **8.4** **Đặt lại** → xác nhận. **Thấy:** "Đã bỏ Mốc đang chạy."; dòng PC08 ghi "Chưa có, lượt gửi tới lấy …".
- [ ] **8.5** Bật đăng ký, chờ 1 lượt. **Thấy:** gửi lại từ Mốc bắt đầu; tải lại trang thì mốc đã tiến.
- [ ] **8.6** Tab **Mẫu chung**, bảng tham số. **Thấy:** ô `@lastKey` trống ghi "Đối tác chưa có mốc: gửi toàn bộ". Không lưu.

## 9. Dọn

- [ ] **9.1** Tắt đăng ký gửi gói 101 của PC08.
- [ ] **9.2** Tab PC08 → **Dùng lại mẫu chung** (cùng công thức cột mốc nên mốc giữ nguyên). Làm lại từ đầu thì Đặt lại mốc trước (bước 0.4).
- [ ] **9.3** PC08 giữ cấu hình bước 0.2 tới khi có địa chỉ nhận thật.

## 10. Nhiều Worker trên máy chủ *(khi triển khai)*

Theo `bangiao/HUONG-DAN-CLONE-WORKER.md`.

- [ ] **10.1** Tạo một dịch vụ `ShareDataWorker-A101`. **Thấy:** sau 1 phút vẫn `RUNNING`.
- [ ] **10.2** Tạo đủ 4 dịch vụ chỉ gửi và 1 dịch vụ chỉ nhận. **Thấy:** cả 5 chạy ổn; đủ 4 đối tác có lượt gửi thành công, dữ liệu không lẫn nhau.
- [ ] **10.3** Khởi động lại một dịch vụ gửi. **Thấy:** gửi tiếp từ mốc, không gửi lại từ đầu.

## Không test tay được (đã có test tự động)

| Ca | Vì sao không test tay |
|---|---|
| Câu riêng còn `@lastTime`, sửa bằng nút **Sửa câu** | `DEV_ITS015` không có câu riêng dạng này |
| Dữ liệu nguồn mới hoặc vừa sửa thì được gửi tiếp | Không có dịch vụ nào cập nhật `TmsZoneStatus` trên `DEV_ITS015` |
| Đặt mốc, Đặt lại đúng lúc Worker đang gửi | Một lượt gửi xong quá nhanh, không kịp bấm |
| Câu bị sửa thẳng trong CSDL (ESH-1306, ESH-1308) | Phải sửa ngoài màn hình |
| `JOIN` nhân dòng (ESH-1309) | Dữ liệu test không có dạng một cha nhiều con |
| Dòng trùng giờ có ID dạng GUID, chữ hoa lẫn thường | Cần dữ liệu cố ý trùng giờ tới mili giây |
| Xóa rồi tạo lại đối tác, gói cùng mã | Phải xóa cấu hình đang dùng |
| API từ chối đổi mã đối tác | Giao diện đã khóa ô mã |
