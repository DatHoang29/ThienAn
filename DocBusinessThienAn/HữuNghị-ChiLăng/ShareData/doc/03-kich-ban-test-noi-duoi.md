# Kịch bản test nối đuôi ShareData (chỉ thao tác trên giao diện)

Làm từ trên xuống, đánh dấu từng ô. Mỗi bước: **Làm** → **Thấy**. Thấy khác thì dừng, chụp màn hình gửi lại.

- Đối tác **Đạt** (mã `Đ`), gói **`108_vmsInfo`**. Dùng sẵn hồ sơ ánh xạ `D_108VMSINFO_OUT` và đăng ký **gửi** gói 108 chu kỳ **30 giây** của Đạt. Đăng ký gửi chu kỳ 1 ngày và đăng ký nhận của Đạt để nguyên, tắt.
- Câu mẫu gói 108 là câu chụp toàn bộ. Kịch bản tạo cấu hình riêng cho Đạt và viết câu nối đuôi trên đó; mẫu chung không đổi.
- Sếp chốt 02/11/2026 dùng **PA1: một chuỗi ghép** `thời gian_khóa` ở cột `__lastKey`. Mục 0 đến 5 kiểm theo dạng này, cũng là dạng câu mẫu của 7 gói nối đuôi (101, 103, 104, 106, 107, 109, 111). Dạng 2 cột đã bỏ ngày 03/11/2026; câu riêng còn `@lastTime` trong CSDL sửa hết bằng tệp D2 ở bước 0.1.
- Trong lúc test, Đạt chuyển sang **kênh File**: mỗi lần gửi được lưu thành một tệp bằng chứng, không cần máy nhận. Bước dọn trả lại như cũ.
- Dữ liệu nguồn `VmsCurrent` trên DB dev (đếm ngày 01/11): **72** dòng; **27** dòng có `UpdateTime`, mới nhất `21/10/2026 07:58:45` khóa `1175a668-79af-440d-a32d-2ba040ba8bf8`; **45** dòng chưa có `UpdateTime` (đều có `CreateTime` ngày 19/08/2026). Cột mốc lấy `CreateTime` khi `UpdateTime` trống nên gửi đủ cả 72 dòng. Hai dòng VMS006 và VMS005 **trùng giờ** `12/08/2026 08:46:07.230`, đứng thứ 5 và 6.
- Xem kết quả ở **Lịch sử chia sẻ → Nhật ký truyền nhận** (cột *Số bản ghi*). Trang **Mốc gửi** mới làm khung, chưa khai menu và không bàn giao nên kịch bản không dùng; mốc kiểm qua số bản ghi: lượt sau không gửi lặp nghĩa là mốc đã tiến.
- Đăng ký chạy 30 giây một lượt: "chờ một lượt" là chờ khoảng 1 phút.

Cấu hình cũ của Đạt để trả lại ở bước dọn:

| Kênh | Địa chỉ | Cổng | Endpoint |
|:--|:--|:--|:--|
| API | `localhost` | `9999` | (trống) |

---

## 0. Chuẩn bị

- [ ] **0.1** Đã chạy các tệp seed theo `sql/README.md`, gồm tệp **D2** `sharedata-sua-cau-rieng-moc-hai-cot-ShareDataSql.sql` (sửa câu riêng còn `@lastTime`, xem dòng `[ĐÃ SỬA]` in ra). Chạy WebAPI, Worker và web bản mới nhất. Mở web, tải lại hẳn trang (Cmd+Shift+R).
- [ ] **0.2** **Cấu hình** → sửa đối tác **Đạt**: kênh kết nối **File**, để trống *Thư mục*, định dạng JSON → Lưu → **Kết nối**. Mọi đăng ký của Đạt để **tắt**.
- [ ] **0.3** **Cấu hình gói tin** → gói `108_vmsInfo` → **Tạo cấu hình riêng +** → Đối tác **Đạt**, Sao chép từ **Mẫu chung** → xác nhận. Đạt đã có cấu hình riêng thì mở thẳng tab Đạt.
  **Thấy:** tự mở tab Đạt, khung soạn có câu `SELECT e.Code AS equipmentCode, … FROM VmsCurrent v …`.
- [ ] **0.4** Xóa hết khung soạn (Cmd+A, Delete), dán câu dưới → **Lưu & phân tích**.
  **Thấy:** lưu thành công; bảng **3. Tham số điều kiện** có `@pageSize` (ô số, điền sẵn 100) và `@lastKey` (*theo key*, ô ngày hiện *Gửi toàn bộ dữ liệu*); không có `@lastTime`.
  ```sql
  SELECT TOP (@pageSize)
      CONVERT(varchar(23), COALESCE(v.UpdateTime, v.CreateTime), 121) + '_' + v.ID AS __lastKey,
      e.Code AS equipmentCode,
      v.Name AS vmsName,
      e.KmNumber AS locationKm,
      e.MetNumber AS locationMet,
      e.DirectionId AS direction,
      e.LaneId AS laneId,
      v.RowData AS displayContent,
      v.Url AS displayImageUrl,
      v.Size AS displaySize,
      v.Priority AS priority,
      v.ExecutedDate AS executedTime
  FROM VmsCurrent v
  LEFT JOIN TmsEquipment e ON v.EquipmentId = e.ID
  WHERE CONVERT(varchar(23), COALESCE(v.UpdateTime, v.CreateTime), 121) + '_' + v.ID > @lastKey
  ORDER BY __lastKey ASC
  ```
- [ ] **0.5** `@pageSize` = **5**; `@lastKey` để trống ô ngày → **Lưu & phân tích**.
  **Thấy:** lưu thành công. Ô giá trị của `@lastKey` là ô chọn ngày giờ, không có ô gõ tay. Trang này chỉ đặt Mốc bắt đầu: kể cả khi đã có mốc, tab Đạt vẫn không hiện mốc đang chạy và không có nút sửa; đã có mốc thì ô bị khóa (xem 2.1). Rê chuột vào **Quy chuẩn mốc** của câu sai thì mục chưa đạt có đoạn SQL gợi ý, popup có một nút **Sửa câu**. Trên tiêu đề mục 3, dải **Quy chuẩn mốc** 3/3 xanh; màn này không còn bảng mốc. Cột **Nguồn giá trị** là ô chọn, tự điền theo tên biến: `@lastKey` là **Nối tiếp lần trước theo key**, biến khác là **Giá trị nhập**; riêng `@pageSize` bị khóa. Bấm thanh tiêu đề mục 3 để thu gọn bảng, dải **Quy chuẩn mốc** vẫn hiện. Cả 45 dòng chưa có `UpdateTime` đều được gửi theo `CreateTime`.

---

## 1. Gửi dữ liệu có sẵn, chia trang, dòng trùng giờ ở ranh giới trang

`@pageSize` = 5 làm ranh giới trang 1 và trang 2 rơi đúng giữa hai dòng trùng giờ VMS006 / VMS005.

- [ ] **1.1** **Cấu hình** → bật đăng ký gửi gói 108 chu kỳ 30 giây của Đạt (**Bật/Tắt**). Chờ một lượt.
  **Thấy:**
  - **Nhật ký truyền nhận**: 15 lần gửi thành công, *Số bản ghi* **5** (14 lần) rồi **2** (đủ 72 dòng; lần cuối là 1 thay vì 2 nghĩa là mất dòng trùng giờ).
  - **Lịch sử chia sẻ → Nhật ký cấu hình**: không có dòng "Câu truy vấn nối đuôi có rủi ro".
- [ ] **1.2** Chờ thêm một lượt. **Thấy:** không có lần gửi mới (không gửi lặp).

## 2. Khóa khi đăng ký gửi đang bật (đăng ký vẫn bật)

- [ ] **2.1** Chọn lại gói `108_vmsInfo` → tab Đạt.
  **Thấy:** **Lưu & phân tích** mờ, có biểu tượng khóa ghi "Đang có đăng ký gửi bật sử dụng (Đạt)…". Ô giá trị của `@lastKey` bị khóa, có biểu tượng khóa và chữ "Đã có mốc, gửi tiếp", không hiện giá trị mốc. Tiêu đề cột **So mẫu chung** nằm trên một dòng.

## 5. Câu sai bị chặn khi lưu

- [ ] **5.0** Tắt đăng ký gửi. Chọn lại gói → tab Đạt.

Mỗi ca: xóa hết khung soạn (Cmd+A, Delete), dán câu của ca → **Lưu & phân tích** → xem thông báo → chọn lại gói, hộp hỏi bỏ thay đổi → **Xác nhận**. Câu mỗi ca chỉ khác câu ở bước 0.4 đúng dòng có chú thích `-- sai`. Đạt đã có mốc từ mục 1 nhưng câu sai bị từ chối trước, nên không hiện hộp xác nhận đổi công thức.

- [ ] **5.1** `WHERE` chỉ so `ID`, không so đúng cột mốc.
  **Thấy:** "Biểu thức so với biến lastKey trong WHERE phải viết giống hệt biểu thức của cột [__lastKey] ở SELECT; viết khác thì bỏ sót hoặc gửi lặp dòng."
  ```sql
  SELECT TOP (@pageSize)
      CONVERT(varchar(23), COALESCE(v.UpdateTime, v.CreateTime), 121) + '_' + v.ID AS __lastKey,
      e.Code AS equipmentCode,
      v.Name AS vmsName,
      e.KmNumber AS locationKm,
      e.MetNumber AS locationMet,
      e.DirectionId AS direction,
      e.LaneId AS laneId,
      v.RowData AS displayContent,
      v.Url AS displayImageUrl,
      v.Size AS displaySize,
      v.Priority AS priority,
      v.ExecutedDate AS executedTime
  FROM VmsCurrent v
  LEFT JOIN TmsEquipment e ON v.EquipmentId = e.ID
  WHERE v.ID > @lastKey  -- sai: chỉ so ID
  ORDER BY __lastKey ASC
  ```
- [ ] **5.2** `WHERE` dùng `v.CreateTime`, khác biểu thức cột mốc ở `SELECT`.
  **Thấy:** Lỗi như 5.1.
  ```sql
  SELECT TOP (@pageSize)
      CONVERT(varchar(23), COALESCE(v.UpdateTime, v.CreateTime), 121) + '_' + v.ID AS __lastKey,
      e.Code AS equipmentCode,
      v.Name AS vmsName,
      e.KmNumber AS locationKm,
      e.MetNumber AS locationMet,
      e.DirectionId AS direction,
      e.LaneId AS laneId,
      v.RowData AS displayContent,
      v.Url AS displayImageUrl,
      v.Size AS displaySize,
      v.Priority AS priority,
      v.ExecutedDate AS executedTime
  FROM VmsCurrent v
  LEFT JOIN TmsEquipment e ON v.EquipmentId = e.ID
  WHERE CONVERT(varchar(23), v.CreateTime, 121) + '_' + v.ID > @lastKey  -- sai: khác biểu thức cột mốc
  ORDER BY __lastKey ASC
  ```
- [ ] **5.3** `ORDER BY` không theo cột mốc.
  **Thấy:** "Truy vấn đồng bộ nối đuôi theo khóa phải sắp xếp ORDER BY ngoài cùng theo cột [__lastKey], nếu không có thể bỏ sót dòng."
  ```sql
  SELECT TOP (@pageSize)
      CONVERT(varchar(23), COALESCE(v.UpdateTime, v.CreateTime), 121) + '_' + v.ID AS __lastKey,
      e.Code AS equipmentCode,
      v.Name AS vmsName,
      e.KmNumber AS locationKm,
      e.MetNumber AS locationMet,
      e.DirectionId AS direction,
      e.LaneId AS laneId,
      v.RowData AS displayContent,
      v.Url AS displayImageUrl,
      v.Size AS displaySize,
      v.Priority AS priority,
      v.ExecutedDate AS executedTime
  FROM VmsCurrent v
  LEFT JOIN TmsEquipment e ON v.EquipmentId = e.ID
  WHERE CONVERT(varchar(23), COALESCE(v.UpdateTime, v.CreateTime), 121) + '_' + v.ID > @lastKey
  ORDER BY v.ID ASC  -- sai: không sắp theo cột mốc
  ```
- [ ] **5.4** Sắp giảm dần.
  **Thấy:** "Truy vấn đồng bộ nối đuôi (Incremental) bắt buộc sắp xếp tăng dần (ASC); không cho phép sắp xếp giảm dần (DESC)…"
  ```sql
  SELECT TOP (@pageSize)
      CONVERT(varchar(23), COALESCE(v.UpdateTime, v.CreateTime), 121) + '_' + v.ID AS __lastKey,
      e.Code AS equipmentCode,
      v.Name AS vmsName,
      e.KmNumber AS locationKm,
      e.MetNumber AS locationMet,
      e.DirectionId AS direction,
      e.LaneId AS laneId,
      v.RowData AS displayContent,
      v.Url AS displayImageUrl,
      v.Size AS displaySize,
      v.Priority AS priority,
      v.ExecutedDate AS executedTime
  FROM VmsCurrent v
  LEFT JOIN TmsEquipment e ON v.EquipmentId = e.ID
  WHERE CONVERT(varchar(23), COALESCE(v.UpdateTime, v.CreateTime), 121) + '_' + v.ID > @lastKey
  ORDER BY __lastKey DESC  -- sai: giảm dần
  ```
- [ ] **5.5** Cột mốc dùng `v.UpdateTime` trơn, không có `COALESCE`.
  **Thấy:** "Cột mốc dùng UpdateTime trơn nên dòng mới thêm (chưa có UpdateTime) không được gửi. Dùng COALESCE(UpdateTime, CreateTime) ở cả cột mốc và WHERE." Rê chuột vào **Quy chuẩn mốc**: mục *Có cột mốc* đỏ, ghi "Dòng mới thêm chưa có UpdateTime nên không được gửi"; bấm **Sửa câu** thì câu về `COALESCE`, 3/3 xanh.
  ```sql
  SELECT TOP (@pageSize)
      CONVERT(varchar(23), v.UpdateTime, 121) + '_' + v.ID AS __lastKey,  -- sai: UpdateTime trơn
      e.Code AS equipmentCode,
      v.Name AS vmsName,
      e.KmNumber AS locationKm,
      e.MetNumber AS locationMet,
      e.DirectionId AS direction,
      e.LaneId AS laneId,
      v.RowData AS displayContent,
      v.Url AS displayImageUrl,
      v.Size AS displaySize,
      v.Priority AS priority,
      v.ExecutedDate AS executedTime
  FROM VmsCurrent v
  LEFT JOIN TmsEquipment e ON v.EquipmentId = e.ID
  WHERE CONVERT(varchar(23), v.UpdateTime, 121) + '_' + v.ID > @lastKey  -- sai: UpdateTime trơn
  ORDER BY __lastKey ASC
  ```
- [ ] **5.6** Cột mốc chỉ có ngày giờ, không ghép khóa.
  **Thấy:** "Cột mốc chỉ có ngày giờ nên sót các dòng trùng giờ. Ghép thêm khóa…". **Sửa câu** ghép lại đủ `COALESCE(v.UpdateTime, v.CreateTime)` và `'_' + v.ID`, 3/3 xanh.
  ```sql
  SELECT TOP (@pageSize)
      v.UpdateTime AS __lastKey,  -- sai: chỉ có ngày giờ
      e.Code AS equipmentCode,
      v.Name AS vmsName,
      e.KmNumber AS locationKm,
      e.MetNumber AS locationMet,
      e.DirectionId AS direction,
      e.LaneId AS laneId,
      v.RowData AS displayContent,
      v.Url AS displayImageUrl,
      v.Size AS displaySize,
      v.Priority AS priority,
      v.ExecutedDate AS executedTime
  FROM VmsCurrent v
  LEFT JOIN TmsEquipment e ON v.EquipmentId = e.ID
  WHERE v.UpdateTime > @lastKey  -- sai: chỉ có ngày giờ
  ORDER BY __lastKey ASC
  ```

**Thấy chung:** chọn lại gói thì câu và bảng tham số vẫn là bản cũ.

## 6. Cách viết khác cho dòng chưa có thời gian: câu hai nhóm mốc *(tùy chọn)*

Câu ở các mục trên đã gửi đủ 72 dòng nhờ `COALESCE` nên không cần cách này; mục này chỉ để kiểm tính năng nhóm mốc (một câu nhiều mốc, mỗi mốc một tên). Câu hai nhóm tách riêng hai mốc: nhóm `upd` theo `UpdateTime`, nhóm `new` là các dòng `UpdateTime` còn trống theo `CreateTime` (cả 45 dòng đều có `CreateTime`).

- [ ] **6.1** Đăng ký gửi đang tắt. Tab Đạt: xóa hết khung soạn, dán câu dưới → **Lưu & phân tích**.
  **Thấy:** hộp xác nhận "Câu truy vấn đổi công thức cột mốc nối đuôi… (Đạt)…" → **Xác nhận**. Sau đó:
  - Lưu thành công.
  - Bảng tham số có `@lastKey_upd`, `@lastKey_new` (*theo key*), **đã chọn sẵn nguồn**; không còn `@lastKey`.

  ```sql
  SELECT TOP (@pageSize)
      u.__lastKey_upd, u.__lastKey_new,
      u.equipmentCode, u.vmsName, u.locationKm, u.locationMet, u.direction, u.laneId,
      u.displayContent, u.displayImageUrl, u.displaySize, u.priority, u.executedTime
  FROM (
      SELECT
          CONVERT(varchar(23), v.UpdateTime, 121) + '_' + v.ID AS sortKey,
          CONVERT(varchar(23), v.UpdateTime, 121) + '_' + v.ID AS __lastKey_upd,
          CAST(NULL AS NVARCHAR(100)) AS __lastKey_new,
          e.Code AS equipmentCode, v.Name AS vmsName, e.KmNumber AS locationKm, e.MetNumber AS locationMet,
          e.DirectionId AS direction, e.LaneId AS laneId, v.RowData AS displayContent, v.Url AS displayImageUrl,
          v.Size AS displaySize, v.Priority AS priority, v.ExecutedDate AS executedTime
      FROM VmsCurrent v
      LEFT JOIN TmsEquipment e ON v.EquipmentId = e.ID
      WHERE CONVERT(varchar(23), v.UpdateTime, 121) + '_' + v.ID > @lastKey_upd
      UNION ALL
      SELECT
          CONVERT(varchar(23), v.CreateTime, 121) + '_' + v.ID, NULL, CONVERT(varchar(23), v.CreateTime, 121) + '_' + v.ID,
          e.Code, v.Name, e.KmNumber, e.MetNumber, e.DirectionId, e.LaneId, v.RowData, v.Url,
          v.Size, v.Priority, v.ExecutedDate
      FROM VmsCurrent v
      LEFT JOIN TmsEquipment e ON v.EquipmentId = e.ID
      WHERE v.UpdateTime IS NULL
        AND CONVERT(varchar(23), v.CreateTime, 121) + '_' + v.ID > @lastKey_new
  ) u
  ORDER BY u.sortKey ASC
  ```
- [ ] **6.2** Bảng tham số: `@pageSize` = **20** → **Lưu & phân tích**. Bật đăng ký gửi, chờ một lượt.
  **Thấy:** gửi **72** bản ghi (27 + 45), *Số bản ghi* **20, 20, 20, 12**.
- [ ] **6.3** Chờ thêm một lượt. **Thấy:** không gửi lặp.

## 7. Gửi lỗi thì giữ mốc *(tùy chọn, làm sau mục 6)*

Bước 7.2 xóa mốc của Đạt bằng cách đổi công thức từ câu hai nhóm (mục 6) về câu ở bước 0.4, vì màn hình không có nút Đặt lại. Bỏ qua mục 6 thì bỏ qua cả mục này.

Cấu hình cũ của Đạt (API `localhost:9999`) không có gì nghe trên máy, nên dùng luôn để thử gửi lỗi.

- [ ] **7.1** Tắt đăng ký gửi. **Cấu hình** → sửa Đạt về kênh **API**, địa chỉ `localhost`, cổng `9999`, endpoint để trống → Lưu → **Kết nối**.
- [ ] **7.2** Tab Đạt: xóa hết khung soạn, dán lại câu ở bước 0.4 → **Lưu & phân tích** → hộp xác nhận đổi công thức → **Xác nhận** (mốc của Đạt bị xóa). Bật đăng ký gửi, chờ một lượt.
  **Thấy:** **Cảnh báo & lỗi** có lỗi không kết nối được tới `localhost:9999`; *Nhật ký truyền nhận* có lần gửi lỗi.
- [ ] **7.3** Tắt đăng ký gửi. Sửa Đạt về kênh **File** như bước 0.2 → Lưu → **Kết nối**. Bật đăng ký gửi, chờ một lượt.
  **Thấy:** gửi đủ 72 bản ghi: lần gửi lỗi không làm mốc tiến.

## 8. Mẫu gợi ý select-incremental (không lưu)

- [ ] **8.1** Chọn một gói không bị khóa → khung soạn SQL: Cmd+A, Delete, gõ `SEL`, chọn **select-incremental**.
  **Thấy:** chèn khung câu `SELECT TOP (@pageSize) t.Column AS aliasName, CONVERT(varchar(23), COALESCE(t.UpdateTime, t.CreateTime), 121) + '_' + t.ID AS __lastKey FROM TableName t WHERE CONVERT(varchar(23), COALESCE(t.UpdateTime, t.CreateTime), 121) + '_' + t.ID > @lastKey ORDER BY __lastKey ASC`; không còn `@lastTime`, `__lastTime`; phím Tab nhảy qua các ô cần đổi.
- [ ] **8.2** Chọn lại gói, hộp hỏi bỏ thay đổi → **Xác nhận**.

## 9. Câu mẫu gói nối đuôi có sẵn bộ biến (không lưu; làm sau khi chạy tệp seed)

- [ ] **9.1** Mở lần lượt gói `103_vdsData`, `104_weatherData`, `106_wimData`, `107_incidentData`, `109_etcData` → tab **Mẫu chung**.
  **Thấy:** khung soạn là câu một chuỗi ghép, không còn `@lastTime`; bảng **3. Tham số điều kiện** có `@lastKey` nguồn Nối tiếp lần trước theo key, không còn `@lastTime`; dải **Quy chuẩn mốc** 3/3 xanh. DB đã có bộ biến cũ thì còn dòng `@pageSize` như đang lưu, câu mẫu dùng `TOP 100` nên dòng này không tác dụng.

## 10. Dọn: trả Đạt về như cũ

- [ ] **10.1** **Cấu hình** → tắt đăng ký gửi gói 108 của Đạt (đăng ký có sẵn, không xóa).
- [ ] **10.2** **Cấu hình gói tin** → gói 108 → tab Đạt → **Dùng lại mẫu chung** → xác nhận. Mốc của Đạt vẫn còn trong CSDL nhưng không ảnh hưởng, vì câu mẫu gói 108 là câu chụp toàn bộ; làm lại kịch bản thì bước 0.4 hiện hộp xác nhận đổi công thức → **Xác nhận**.
- [ ] **10.3** **Cấu hình** → sửa Đạt về kênh **API**, `localhost`, cổng `9999`, endpoint trống (bảng ở đầu tệp) → Lưu.

## 11. Nhiều Worker trên máy chủ *(làm khi triển khai)*

Mục tiêu: khởi động nhiều dịch vụ trên máy chủ mà vẫn chạy đúng, chưa cần đo tải. Cách cài xem MasterPlan mục SV-5.

- [ ] **11.1** Cài 4 dịch vụ `SendOnly` (`A101` đến `A104`) và 1 dịch vụ `ReceiveOnly`, cùng CSDL. Khởi động cả 5.
  **Thấy:** cả 5 dịch vụ chạy, không tự dừng; log không có lỗi kết nối hay lỗi vai trò.
- [ ] **11.2** Bật đăng ký gửi của 4 đối tác, chờ vài lượt.
  **Thấy:** `ShareDataInboundPacket` có gói của đủ 4 đối tác, tất cả `Done`; dữ liệu mỗi đối tác đủ, không trùng, không lẫn nhau; **Cảnh báo & lỗi** không có `ESH-15xx` mới.
- [ ] **11.3** Dừng rồi khởi động lại một dịch vụ gửi. **Thấy:** gửi tiếp từ mốc, không gửi lại từ đầu.

---

## Không làm được trên giao diện (đã có test tự động)

| Tình huống | Vì sao không làm tay được |
|:--|:--|
| Xem mốc đang chạy, Đặt mốc, Đặt lại | Làm ở trang **Mốc gửi**, chưa khai menu và không bàn giao |
| Câu riêng còn `@lastTime` sửa bằng nút **Sửa câu** | Sau tệp D2, DB dev không còn câu dạng này để thử |
| Dữ liệu nguồn mới hoặc vừa sửa thì được gửi tiếp | `VmsCurrent` chỉ đổi khi gửi nội dung xuống biển VMS thật; không làm trong lúc test |
| Tắt đăng ký rồi bấm Đặt mốc / Đặt lại đúng lúc Worker đang gửi dở | Kênh File gửi xong ngay, không kịp bấm |
| Câu bị sửa thẳng trong CSDL (cảnh báo ESH-1306, ESH-1308) | Phải sửa ngoài màn hình |
