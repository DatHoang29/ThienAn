# Kịch bản test nối đuôi ShareData (chỉ thao tác trên giao diện)

Làm từ trên xuống, đánh dấu từng ô. Mỗi bước: **Làm** → **Thấy**. Thấy khác thì dừng, chụp màn hình gửi lại.

- Đối tác **Đạt** (mã `Đ`), gói **`108_vmsInfo`**. Dùng sẵn hồ sơ ánh xạ `D_108VMSINFO_OUT` và đăng ký **gửi** gói 108 chu kỳ **30 giây** của Đạt. Đăng ký gửi chu kỳ 1 ngày và đăng ký nhận của Đạt để nguyên, tắt.
- Câu mẫu gói 108 là câu chụp toàn bộ. Kịch bản tạo cấu hình riêng cho Đạt và viết câu nối đuôi trên đó; mẫu chung không đổi.
- Trong lúc test, Đạt chuyển sang **kênh File**: mỗi lần gửi được lưu thành một tệp bằng chứng, không cần máy nhận. Bước dọn trả lại như cũ.
- Dữ liệu nguồn `VmsCurrent` trên DB dev (đếm ngày 01/11): **72** dòng; **27** dòng có `UpdateTime`, mới nhất `21/10/2026 07:58:45` khóa `1175a668-79af-440d-a32d-2ba040ba8bf8`; **45** dòng chưa có `UpdateTime`. Hai dòng VMS006 và VMS005 **trùng giờ** `12/08/2026 08:46:07.230`, đứng thứ 5 và 6.
- Xem kết quả ở **Lịch sử chia sẻ → Nhật ký truyền nhận** (cột *Số bản ghi*) và dải **Mốc đang chạy** ở tab Đạt. Dải mốc không tự làm mới: chọn lại gói 108 để xem mốc mới.
- Đăng ký chạy 30 giây một lượt: "chờ một lượt" là chờ khoảng 1 phút.

Cấu hình cũ của Đạt để trả lại ở bước dọn:

| Kênh | Địa chỉ | Cổng | Endpoint |
|:--|:--|:--|:--|
| API | `localhost` | `9999` | (trống) |

---

## 0. Chuẩn bị

- [ ] **0.1** Chạy WebAPI, Worker và web bản mới nhất. Mở web, tải lại hẳn trang (Cmd+Shift+R).
- [ ] **0.2** **Cấu hình** → sửa đối tác **Đạt**: kênh kết nối **File**, để trống *Thư mục*, định dạng JSON → Lưu → **Kết nối**. Mọi đăng ký của Đạt để **tắt**.
- [ ] **0.3** **Cấu hình gói tin** → gói `108_vmsInfo` → **Tạo cấu hình riêng +** → Đối tác **Đạt**, Sao chép từ **Mẫu chung** → xác nhận.
  **Thấy:** tự mở tab Đạt, khung soạn có câu `SELECT e.Code AS equipmentCode, … FROM VmsCurrent v …`.
- [ ] **0.4** Xóa hết khung soạn (Cmd+A, Delete), dán câu dưới → **Lưu & phân tích**.
  **Thấy:** "Sửa các dòng tham số đang báo lỗi trước khi lưu."; bảng **3. Tham số điều kiện** có `@pageSize` (ô giá trị trống, viền đỏ), `@lastTime` (*theo thời gian*), `@lastKey` (*theo key*).
  ```sql
  SELECT TOP (@pageSize)
      v.UpdateTime AS __lastTime,
      v.ID AS __lastKey,
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
  WHERE v.UpdateTime >= ISNULL(@lastTime, '17530101')
    AND (@lastTime IS NULL OR v.UpdateTime > @lastTime OR v.ID > @lastKey)
  ORDER BY v.UpdateTime ASC, v.ID ASC
  ```
- [ ] **0.5** `@pageSize` = **5**; `@lastTime` để **Toàn bộ** → **Lưu & phân tích**.
  **Thấy:** lưu thành công, không có cảnh báo vàng. Lưu ý: 45 dòng chưa có `UpdateTime` không được gửi và màn hình không báo; Worker ghi rủi ro vào nhật ký cấu hình khi chạy (xem bước 1.1). Mục 6 xử lý các dòng này.

---

## 1. Gửi dữ liệu có sẵn, chia trang, dòng trùng giờ ở ranh giới trang

`@pageSize` = 5 làm ranh giới trang 1 và trang 2 rơi đúng giữa hai dòng trùng giờ VMS006 / VMS005.

- [ ] **1.1** **Cấu hình** → bật đăng ký gửi gói 108 chu kỳ 30 giây của Đạt (**Bật/Tắt**). Chờ một lượt.
  **Thấy:**
  - **Nhật ký truyền nhận**: 6 lần gửi thành công, *Số bản ghi* **5, 5, 5, 5, 5, 2** (đủ 27 dòng; lần cuối là 1 thay vì 2 nghĩa là mất dòng trùng giờ).
  - Tab Đạt, dải *Mốc đang chạy*: `21/10/2026 07:58:45 · 1175a668…`.
  - **Lịch sử chia sẻ → Nhật ký cấu hình**: một dòng "Câu truy vấn nối đuôi có rủi ro", nêu cột `__lastTime` cho phép trống. Các lượt sau không ghi thêm.
- [ ] **1.2** Chờ thêm một lượt. **Thấy:** không có lần gửi mới (không gửi lặp).

## 2. Khóa khi đăng ký gửi đang bật (đăng ký vẫn bật)

- [ ] **2.1** Chọn lại gói `108_vmsInfo` → tab Đạt.
  **Thấy:** **Lưu & phân tích** mờ, có biểu tượng khóa ghi "Đang có đăng ký gửi bật sử dụng (Đạt)…"; **✎ Đặt mốc** và **↺ Đặt lại** đều mờ.

## 3. Đặt mốc (tắt đăng ký trước)

- [ ] **3.1** **Cấu hình** → tắt đăng ký gửi. Chọn lại gói → tab Đạt → **✎ Đặt mốc**: *Mốc thời gian* = `15/10/2026 11:01:30`, *Mốc khóa* để trống → **Xác nhận**.
  **Thấy:** "Đã cập nhật mốc gửi dữ liệu cho đối tác."
- [ ] **3.2** Bật đăng ký gửi, chờ một lượt.
  **Thấy:** gửi đúng **2** bản ghi (VMS-N-02 và "Cảnh báo sự cố Km25"); mốc về `21/10/2026 07:58:45 · 1175a668…`.

## 4. Đặt lại

- [ ] **4.1** **Cấu hình** → tắt đăng ký gửi. Chọn lại gói → tab Đạt → **↺ Đặt lại** → xác nhận.
  **Thấy:** "Đã đặt lại mốc đã lưu."; dải mốc trống.
- [ ] **4.2** Bật đăng ký gửi, chờ một lượt.
  **Thấy:** gửi lại toàn bộ, *Số bản ghi* **5, 5, 5, 5, 5, 2**.

## 5. Câu sai bị chặn khi lưu

- [ ] **5.0** Tắt đăng ký gửi. Chọn lại gói → tab Đạt.

Mỗi dòng: sửa trong khung soạn → **Lưu & phân tích** → xem thông báo → chọn lại gói, hộp hỏi bỏ thay đổi → **Xác nhận**.

| | Sửa | Thấy |
|:--|:--|:--|
| ☐ **5.1** | Trong `WHERE`, xóa ` OR v.ID > @lastKey` | Lỗi đỏ, không lưu |
| ☐ **5.2** | Thay `AND (@lastTime IS NULL OR v.UpdateTime > @lastTime OR v.ID > @lastKey)` bằng `AND v.ID > @lastKey` | Lỗi "WHERE phải so cả cặp mốc…" |
| ☐ **5.3** | `ORDER BY v.ID ASC, v.UpdateTime ASC` | Lỗi "…phải sắp xếp ORDER BY ngoài cùng theo cột [__lastTime] rồi [__lastKey]…" |
| ☐ **5.4** | `ORDER BY v.UpdateTime DESC, v.ID DESC` | Lỗi "…không cho phép sắp xếp giảm dần (DESC)…" |
| ☐ **5.5** | Không sửa câu. Bảng tham số: `@lastKey` chọn nguồn **Nối tiếp lần trước (theo thời gian)** | Lỗi "WHERE phải so cả cặp mốc…" |

**Thấy chung:** chọn lại gói thì câu và bảng tham số vẫn là bản cũ (`@lastKey` vẫn *theo key*).

## 5b. Câu một cột: chuỗi ghép (PA1 trong biên bản)

Gộp thời gian và khóa vào một cột `__lastKey`. Dòng chưa có `UpdateTime` ghép ra NULL nên không được gửi: đúng là **27** dòng, không phải 72.

- [ ] **5b.1** Đăng ký gửi đang tắt. Tab Đạt: xóa hết khung soạn, dán câu dưới → **Lưu & phân tích**.
  **Thấy:** hộp xác nhận "Câu truy vấn đổi công thức cột mốc nối đuôi… (Đạt)…" → **Xác nhận**. Sau đó:
  - Lưu thành công.
  - Bảng tham số có `@pageSize` = 5 và `@lastKey` (*theo key*); không còn `@lastTime`. Dải **Quy chuẩn mốc** 3/3 xanh.

  ```sql
  SELECT TOP (@pageSize)
      CONVERT(varchar(23), v.UpdateTime, 121) + '_' + v.ID AS __lastKey,
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
  WHERE CONVERT(varchar(23), v.UpdateTime, 121) + '_' + v.ID > @lastKey
  ORDER BY __lastKey ASC
  ```
- [ ] **5b.2** **Cấu hình** → bật đăng ký gửi, chờ một lượt.
  **Thấy:** *Số bản ghi* **5, 5, 5, 5, 5, 2** (đủ 27 dòng, hai dòng trùng giờ VMS006 / VMS005 không mất). Chọn lại gói → tab Đạt: dải mốc `2026-10-21 07:58:45.487_1175a668-79af-440d-a32d-2ba040ba8bf8`.
- [ ] **5b.3** Chờ thêm một lượt. **Thấy:** không có lần gửi mới.
- [ ] **5b.4** **Cấu hình** → tắt đăng ký gửi, sang mục 6.

## 6. Dòng chưa có thời gian: câu hai nhóm mốc

Câu hiện tại không bao giờ gửi 45 dòng chưa có `UpdateTime`. Câu hai nhóm gửi thêm các dòng đó theo `CreateTime` (cả 45 dòng đều có `CreateTime`).

- [ ] **6.1** Đăng ký gửi đang tắt. Tab Đạt: xóa hết khung soạn, dán câu dưới → **Lưu & phân tích**.
  **Thấy:** hộp xác nhận "Câu truy vấn đổi công thức cột mốc nối đuôi… (Đạt)…" → **Xác nhận**. Sau đó:
  - Lưu thành công.
  - Bảng tham số có `@lastTime_upd`, `@lastTime_new` (*theo thời gian*), `@lastKey_upd`, `@lastKey_new` (*theo key*), **đã chọn sẵn nguồn**; không còn `@lastTime`, `@lastKey`.
  - Mở **✎ Đặt mốc**: có hai khối *Nhóm mốc upd* và *Nhóm mốc new* (xem rồi đóng).

  ```sql
  SELECT TOP (@pageSize)
      u.__lastTime_upd, u.__lastKey_upd, u.__lastTime_new, u.__lastKey_new,
      u.equipmentCode, u.vmsName, u.locationKm, u.locationMet, u.direction, u.laneId,
      u.displayContent, u.displayImageUrl, u.displaySize, u.priority, u.executedTime
  FROM (
      SELECT
          v.UpdateTime AS sortTime, v.ID AS sortKey,
          v.UpdateTime AS __lastTime_upd, v.ID AS __lastKey_upd,
          CAST(NULL AS DATETIME) AS __lastTime_new, CAST(NULL AS NVARCHAR(64)) AS __lastKey_new,
          e.Code AS equipmentCode, v.Name AS vmsName, e.KmNumber AS locationKm, e.MetNumber AS locationMet,
          e.DirectionId AS direction, e.LaneId AS laneId, v.RowData AS displayContent, v.Url AS displayImageUrl,
          v.Size AS displaySize, v.Priority AS priority, v.ExecutedDate AS executedTime
      FROM VmsCurrent v
      LEFT JOIN TmsEquipment e ON v.EquipmentId = e.ID
      WHERE v.UpdateTime >= ISNULL(@lastTime_upd, '17530101')
        AND (@lastTime_upd IS NULL OR v.UpdateTime > @lastTime_upd OR v.ID > @lastKey_upd)
      UNION ALL
      SELECT
          v.CreateTime, v.ID, NULL, NULL, v.CreateTime, v.ID,
          e.Code, v.Name, e.KmNumber, e.MetNumber, e.DirectionId, e.LaneId, v.RowData, v.Url,
          v.Size, v.Priority, v.ExecutedDate
      FROM VmsCurrent v
      LEFT JOIN TmsEquipment e ON v.EquipmentId = e.ID
      WHERE v.UpdateTime IS NULL
        AND v.CreateTime >= ISNULL(@lastTime_new, '17530101')
        AND (@lastTime_new IS NULL OR v.CreateTime > @lastTime_new OR v.ID > @lastKey_new)
  ) u
  ORDER BY u.sortTime ASC, u.sortKey ASC
  ```
- [ ] **6.2** Bảng tham số: `@pageSize` = **20** → **Lưu & phân tích**. Bật đăng ký gửi, chờ một lượt.
  **Thấy:** gửi **72** bản ghi (27 + 45), *Số bản ghi* **20, 20, 20, 12**; dải mốc hiện `upd: 21/10/2026 07:58:45 · 1175a668…; new: … · …`.
- [ ] **6.3** Chờ thêm một lượt. **Thấy:** không gửi lặp.

## 7. Gửi lỗi thì giữ mốc *(tùy chọn)*

Cấu hình cũ của Đạt (API `localhost:9999`) không có gì nghe trên máy, nên dùng luôn để thử gửi lỗi.

- [ ] **7.1** Tắt đăng ký gửi. **Cấu hình** → sửa Đạt về kênh **API**, địa chỉ `localhost`, cổng `9999`, endpoint để trống → Lưu → **Kết nối**.
- [ ] **7.2** Chọn lại gói → tab Đạt → **↺ Đặt lại** → xác nhận. Bật đăng ký gửi, chờ một lượt.
  **Thấy:** **Cảnh báo & lỗi** có lỗi không kết nối được tới `localhost:9999`; *Nhật ký truyền nhận* có lần gửi lỗi; dải mốc **vẫn trống**.
- [ ] **7.3** Tắt đăng ký gửi. Sửa Đạt về kênh **File** như bước 0.2 → Lưu → **Kết nối**. Bật đăng ký gửi, chờ một lượt.
  **Thấy:** gửi đủ 72 bản ghi, mốc tiến như bước 6.2.

## 8. Mẫu gợi ý select-incremental (không lưu)

- [ ] **8.1** Chọn một gói không bị khóa → khung soạn SQL: Cmd+A, Delete, gõ `SEL`, chọn **select-incremental**.
  **Thấy:** chèn khung câu `SELECT TOP (@pageSize)` có `__lastTime`, `__lastKey`, `WHERE … >= ISNULL(@lastTime, '17530101') AND (… > @lastTime OR … > @lastKey)`, `ORDER BY` hai cột; phím Tab nhảy qua các ô cần đổi.
- [ ] **8.2** Chọn lại gói, hộp hỏi bỏ thay đổi → **Xác nhận**.

## 9. Gói 103–109 có sẵn bộ biến (không lưu)

- [ ] **9.1** Mở lần lượt gói `103_vdsData`, `104_weatherData`, `106_wimData`, `107_incidentData`, `109_etcData` → tab **Mẫu chung**.
  **Thấy:** bảng **3. Tham số điều kiện** có 3 dòng `@pageSize` (Giá trị nhập, 100), `@lastTime` (theo thời gian, Toàn bộ), `@lastKey` (theo key); dải **Quy chuẩn mốc** 4/4 xanh.

## 10. Dọn: trả Đạt về như cũ

- [ ] **10.1** **Cấu hình** → tắt đăng ký gửi gói 108 của Đạt (đăng ký có sẵn, không xóa).
- [ ] **10.2** **Cấu hình gói tin** → gói 108 → tab Đạt → **↺ Đặt lại** → xác nhận → **Dùng lại mẫu chung** → xác nhận.
- [ ] **10.3** **Cấu hình** → sửa Đạt về kênh **API**, `localhost`, cổng `9999`, endpoint trống (bảng ở đầu tệp) → Lưu.

---

## Không làm được trên giao diện (đã có test tự động)

| Tình huống | Vì sao không làm tay được |
|:--|:--|
| Dữ liệu nguồn mới hoặc vừa sửa thì được gửi tiếp | `VmsCurrent` chỉ đổi khi gửi nội dung xuống biển VMS thật; không làm trong lúc test |
| Tắt đăng ký rồi bấm Đặt mốc / Đặt lại đúng lúc Worker đang gửi dở | Kênh File gửi xong ngay, không kịp bấm |
| Câu bị sửa thẳng trong CSDL (cảnh báo ESH-1306, ESH-1308) | Phải sửa ngoài màn hình |
