# Bộ Câu hỏi & Trả lời phỏng vấn / Báo cáo PM (Chị T): Luồng Gửi Dữ liệu (Outbound)

> **Mục đích:** Tài liệu "phao cứu sinh" (Cheat Sheet) tổng hợp toàn bộ các câu hỏi từ tổng quan đến chi tiết kỹ thuật mà PM (Chị T) hoặc các bên liên quan có thể hỏi về **Luồng Gửi dữ liệu (ShareData Outbound)** của dự án Cao tốc Hữu Nghị – Chi Lăng.  
> **Căn cứ kỹ thuật:** Dựa trên hiện trạng mã nguồn thực tế và Cơ sở dữ liệu Staging (`10.10.8.30:1433`).

---

## Mục lục tra cứu nhanh

- [Phần 1: Nhóm câu hỏi Tổng quan & Kiến trúc](#phần-1-nhóm-câu-hỏi-tổng-quan--kiến-trúc)
  - [Q1. Luồng Gửi hoạt động như thế nào? Bức tranh tổng thể 1 phút?](#q1-luồng-gửi-hoạt-động-như-thế-nào-bức-tranh-tổng-thể-1-phút)
  - [Q2. Có những cơ chế kích hoạt gửi nào? Có gửi tức thì khi có tai nạn/sự cố không?](#q2-có-những-cơ-chế-kích-hoạt-gửi-nào-có-gửi-tức-thì-khi-có-tai-nạnsự-cố-không)
  - [Q3. Hệ thống gửi dữ liệu cho ai và có bao nhiêu loại gói tin?](#q3-hệ-thống-gửi-dữ-liệu-cho-ai-và-có-bao-nhiêu-loại-gói-tin)
- [Phần 2: Nhóm câu hỏi Cấu hình & Vận hành trên UI](#phần-2-nhóm-câu-hỏi-cấu-hình--vận-hành-trên-ui)
  - [Q4. Để một lượt gửi diễn ra thành công, cần thỏa mãn những điều kiện tiên quyết nào?](#q4-để-một-lượt-gửi-diễn-ra-thành-công-cần-thỏa-mãn-những-điều-kiện-tiên-quyết-nào)
  - [Q5. Tại sao đã tạo Đăng ký chia sẻ (Subscription) rồi mà hệ thống vẫn không gửi dữ liệu?](#q5-tại-sao-đã-tạo-đăng-ký-chia-sẻ-subscription-rồi-mà-hệ-thống-vẫn-không-gửi-dữ-liệu)
  - [Q6. Hồ sơ ánh xạ (Mapping) đóng vai trò gì?](#q6-hồ-sơ-ánh-xạ-mapping-đóng-vai-trò-gì)
  - [Q7. Bộ mã quy đổi (CodeSet) dùng để làm gì?](#q7-bộ-mã-quy-đổi-codeset-dùng-để-làm-gì)
- [Phần 3: Nhóm câu hỏi Kỹ thuật & Xử lý Dữ liệu](#phần-3-nhóm-câu-hỏi-kỹ-thuật--xử-lý-dữ-liệu)
  - [Q8. Dữ liệu được trích xuất từ bảng nào trong CSDL?](#q8-dữ-liệu-được-trích-xuất-từ-bảng-nào-trong-csdl)
  - [Q9. Khác biệt giữa Gói gửi toàn bộ (Snapshot) và Gói gửi nối đuôi (Biến động)?](#q9-khác-biệt-giữa-gói-gửi-toàn-bộ-snapshot-và-gói-gửi-nối-đuôi-biến-động)
  - [Q10. Cơ chế nối đuôi (LastSend) hoạt động ra sao? Làm sao chống trùng dữ liệu?](#q10-cơ-chế-nối-đuôi-lastsend-hoạt-động-ra-sao-làm-sao-chống-trùng-dữ-liệu)
  - [Q11. Ví dụ cụ thể: Lần 1 gửi A, lần 2 trong CSDL vẫn là A thì có gửi không?](#q11-ví-dụ-cụ-thể-lần-1-gửi-dữ-liệu-a-lần-2-trong-csdl-vẫn-là-a-không-có-dữ-liệu-mới-thì-hệ-thống-có-gửi-không)
  - [Q12. Gói tin gửi đi có cấu trúc như thế nào qua HTTP?](#q12-gói-tin-gửi-đi-có-cấu-trúc-như-thế-nào-qua-http)
- [Phần 4: Nhóm câu hỏi Xử lý Sự cố & Độ tin cậy](#phần-4-nhóm-câu-hỏi-xử-lý-sự-cố--độ-tin-cậy)
  - [Q13. Nếu mạng rớt hoặc máy chủ đối tác trả về HTTP 500 thì sao? Có bị mất dữ liệu không?](#q13-nếu-mạng-rớt-hoặc-máy-chủ-đối-tác-trả-về-http-500-thì-sao-có-bị-mất-dữ-liệu-không)
  - [Q14. Khi đối tác phản ánh "không nhận được dữ liệu", mình kiểm tra ở đâu để đối soát?](#q14-khi-đối-tác-phản-ánh-không-nhận-được-dữ-liệu-mình-kiểm-tra-ở-đâu-để-đối-soát)
  - [Q15. Các mã cảnh báo sự cố ESH thường gặp ở chiều gửi là gì?](#q15-các-mã-cảnh-báo-sự-cố-esh-thường-gặp-ở-chiều-gửi-là-gì)
- [Phần 5: Hiện trạng Dữ liệu Staging & Kịch bản Test/Demo](#phần-5-hiện-trạng-dữ-liệu-staging--kịch-bản-testdemo)
  - [Q16. Hiện tại trên Staging đã có data test những gói nào rồi?](#q16-hiện-tại-trên-staging-đã-có-data-test-những-gói-nào-rồi)
  - [Q17. Những gói nào chưa có data hoặc đang gặp vướng mắc? Hướng xử lý?](#q17-những-gói-nào-chưa-có-data-hoặc-đang-gặp-vướng-mắc-hướng-xử-lý)
  - [Q18. Bây giờ muốn demo luồng gửi cho sếp hoặc đối tác xem ngay thì làm thế nào?](#q18-bây-giờ-muốn-demo-luồng-gửi-cho-sếp-hoặc-đối-tác-xem-ngay-thì-làm-thế-nào)

---

## Phần 1: Nhóm câu hỏi Tổng quan & Kiến trúc

### Q1. Luồng Gửi hoạt động như thế nào? Bức tranh tổng thể 1 phút?
* **Trả lời trọng tâm:**  
  Luồng Gửi (Outbound Pipeline) là dịch vụ ngầm (`ShareDataWorker`) tự động vận hành theo 3 chặng tuần tự:
  1. **Chặng 1 - Trích xuất (Extract):** Quét CSDL `DEV_ITS10`, lọc dữ liệu mới phát sinh theo mốc thời gian gửi lần trước (`LastSend`).
  2. **Chặng 2 - Ánh xạ (Map):** Biến đổi dữ liệu thô nội bộ sang đúng cấu trúc JSON đối tác yêu cầu dựa vào Hồ sơ ánh xạ (`TargetShapeJson`) và quy đổi bảng mã (`CodeSet`).
  3. **Chặng 3 - Vận chuyển (Transport):** Đẩy dữ liệu qua giao thức **HTTP POST** tới REST API của đối tác, đồng thời lưu 1 bản sao file JSON tại thư mục server `E:\IIS_WebPool\ITS\ITS015\Services\TA-ShareData-Service\sharedata` để làm bằng chứng kiểm toán.

---

### Q2. Có những cơ chế kích hoạt gửi nào? Có gửi tức thì khi có tai nạn/sự cố không?
* **Trả lời trọng tâm:**  
  Hệ thống hỗ trợ song song **2 cơ chế kích hoạt**:
  1. **Theo lịch định kỳ (Scheduled):** Cấu hình theo chu kỳ giây (Interval, ví dụ: 60s/lần) hoặc theo biểu thức Cron (ví dụ: 09:00 hàng ngày). Dành cho dữ liệu thống kê, quan trắc thời tiết, lưu lượng xe.
  2. **Theo sự kiện dữ liệu mới (Event-driven):** Bật cờ `SendOnNewData` trong Đăng ký chia sẻ. Cơ chế **SQL Change Tracking** bắt ngay biến động bảng nghiệp vụ $\rightarrow$ bắn tín hiệu qua **NATS Message Broker** $\rightarrow$ Worker kích hoạt lượt gửi ngay lập tức (Near real-time), đặc biệt quan trọng với **Gói 107 (Sự cố giao thông)** và **Gói 110 (Cảnh báo WP)**.

---

### Q3. Hệ thống gửi dữ liệu cho ai và có bao nhiêu loại gói tin?
* **Trả lời trọng tâm:**  
  - **Đối tác:** Bất kỳ đơn vị bên ngoài nào được khai báo trong bảng `ShareDataPartner` (ví dụ: Cục Đường bộ Việt Nam - DRVN, Cảnh sát Giao thông, Trung tâm điều hành tuyến lân cận...).
  - **Gói tin:** Chuẩn hóa gồm **10 loại gói tin (từ 101 đến 110)**:
    - *Nhóm hiện trạng (Snapshot):* 101 (Giao thông chung), 102 (Camera CCTV), 105 (Nhận dạng RFID), 108 (Biển báo VMS), 110 (Cảnh báo WP).
    - *Nhóm biến động (Nối đuôi):* 103 (Dò xe VDS), 104 (Thời tiết), 106 (Cân tải trọng WIM), 107 (Sự cố giao thông), 109 (Thu phí ETC).

---

## Phần 2: Nhóm câu hỏi Cấu hình & Vận hành trên UI

### Q4. Để một lượt gửi diễn ra thành công, cần thỏa mãn những điều kiện tiên quyết nào?
* **Trả lời trọng tâm:**  
  Bắt buộc thỏa mãn **4 điều kiện**:
  1. **Đối tác:** Trạng thái kích hoạt (`Enable`) và kết nối đang mở (`Connected`).
  2. **Đăng ký chia sẻ:** Trạng thái `Hoạt động` (Active), cấu hình đúng lịch gửi hoặc bật cờ sự kiện.
  3. **Hồ sơ ánh xạ:** Bắt buộc có **đúng 1 hồ sơ đang BẬT ("Đang dùng")** cho bộ ba `(Đối tác × Gói tin × Chiều Gửi)`.
  4. **Kỳ kích hoạt:** Đến hạn lịch gửi hoặc có bản ghi mới trong CSDL.

---

### Q5. Tại sao đã tạo Đăng ký chia sẻ (Subscription) rồi mà hệ thống vẫn không gửi dữ liệu?
* **Trả lời trọng tâm:**  
  Đây là tình huống thực tế thường gặp nhất do thiếu mắt xích phụ thuộc:
  1. **Chưa BẬT hồ sơ ánh xạ:** Đã tạo Đăng ký nhưng chưa có hồ sơ ánh xạ ở trạng thái "Đang dùng" $\rightarrow$ Hệ thống tự động chặn gửi và ghi cảnh báo **`ESH-1304`** ở màn hình Cảnh báo lỗi.
  2. **Chưa mở kết nối Đối tác:** Thẻ đối tác chưa bấm "Kết nối" (phiên chưa `Connected`).
  3. **CSDL không có dữ liệu mới:** Kỳ quét đã chạy nhưng trong khoảng thời gian đó không phát sinh bản ghi mới nào (`rawRows = 0`) $\rightarrow$ Hệ thống dừng ở Bước 1, không gọi HTTP, ghi 1 dòng log phẳng `Success (0 bản ghi)`.

---

### Q6. Hồ sơ ánh xạ (Mapping) đóng vai trò gì?
* **Trả lời trọng tâm:**  
  - Hồ sơ ánh xạ quyết định **hình dạng dữ liệu đầu ra**. Vì mỗi đối tác (DRVN, bên thứ 3) yêu cầu định dạng JSON khác nhau nên hệ thống dùng cấu trúc `TargetShapeJson` để:
    - Cắt gọt trường cần gửi, đặt tên thuộc tính theo chuẩn của đối tác.
    - Định dạng kiểu dữ liệu (số nguyên, chuỗi, ngày giờ ISO 8601).
    - Tính toán tổng hợp in-memory (COUNT làn xe, AVG vận tốc...).
  - Chỉ có **1 hồ sơ được phép BẬT** tại một thời điểm cho mỗi cặp `(Đối tác, Gói tin)`.

---

### Q7. Bộ mã quy đổi (CodeSet) dùng để làm gì?
* **Trả lời trọng tâm:**  
  Dùng để ánh xạ giá trị enum/danh mục 2 chiều:
  - CSDL nội bộ lưu mã số ngắn (ví dụ: Loại sự cố tai nạn = `1`, ùn tắc = `2`).
  - Đối tác yêu cầu chuỗi tiếng Anh chuẩn (ví dụ: `ACCIDENT`, `CONGESTION`).
  - Bộ mã sẽ tự động đổi giá trị trước khi đóng gói JSON gửi đi. Nếu giá trị nội bộ bị null, hệ thống sẽ điền giá trị mặc định (`defaultPartnerValue`).

---

## Phần 3: Nhóm câu hỏi Kỹ thuật & Xử lý Dữ liệu

### Q8. Dữ liệu được trích xuất từ bảng nào trong CSDL?
* **Trả lời trọng tâm:**  
  Toàn bộ dữ liệu chiều Gửi được đọc trực tiếp từ CSDL vận hành chính **`DEV_ITS10`**:
  - Gói 101: `TmsZoneStatus`, `TmsZone`, `TmsTrafficStatistic`
  - Gói 102: `CctvDevice`, `TmsEquipment`
  - Gói 103: `TmsTrafficData`, `TmsEquipment`
  - Gói 104: `TmsWeather`
  - Gói 105: `TollTransactionOut`, `TmsVehicleRegistration`
  - Gói 106: `TmsTrafficData`
  - Gói 107: `TmsIncident`, `TmsEventType`
  - Gói 108: `VmsCurrent`, `TmsEquipment`
  - Gói 109: `TollTransactionOut`, `TollLane`, `TollStation`
  - Gói 110: `TmsIncident`, `VmsCurrent`, `TmsEquipment`

---

### Q9. Khác biệt giữa Gói gửi toàn bộ (Snapshot) và Gói gửi nối đuôi (Biến động)?
* **Trả lời trọng tâm:**  
  - **Gói Snapshot (101, 102, 105, 108, 110):** Mỗi lần đến lịch gửi, hệ thống lấy toàn bộ ảnh chụp trạng thái hiện tại của thiết bị, biển báo, hiện trạng khu vực (tối đa 100 bản ghi mới nhất). Không lọc theo mốc cũ, **tuyệt đối không tạo/cập nhật bảng `ShareDataLastSend`**.
  - **Gói Biến động / Nối đuôi (103, 104, 106, 107, 109):** Dữ liệu phát sinh liên tục theo dòng sự kiện (xe chạy qua cảm biến, thời tiết từng phút, sự cố). Hệ thống quét dữ liệu tăng dần qua con trỏ kép `(LastTime, LastKey)` lưu trong bảng `ShareDataLastSend`. Phân trang tối đa **100 bản ghi/trang**.

---

### Q10. Cơ chế nối đuôi (LastSend) hoạt động ra sao? Làm sao chống trùng dữ liệu?
* **Trả lời trọng tâm:**  
  - Bảng `ShareDataLastSend` lưu giữ mốc con trỏ kép `(LastTime, LastKey)` độc lập cho từng cặp `(PartnerCode, PacketCode)`.
  - **Mốc thời gian (`LastTime`):** `Watermark = COALESCE(UpdateTime, CreateTime, [EventTime])`.
  - **Mốc khóa (`LastKey`):** Khóa định danh `ID` (`__rowid`) của bản ghi cuối cùng trong trang (dùng để phân định rạch ròi khi nhiều bản ghi có cùng một tích tắc thời gian).
  - **Câu lệnh SQL lọc:**
    ```sql
    WHERE @lastTime IS NULL
       OR Watermark > @lastTime
       OR (Watermark = @lastTime AND ID > @lastKey)
    ORDER BY Watermark ASC, ID ASC
    ```
  - **Chỉ khi đối tác phản hồi HTTP 200..299 thành công**, hệ thống mới commit cập nhật mốc `LastTime` và `LastKey` mới vào bảng `ShareDataLastSend`. Nhờ cơ chế con trỏ đơn điệu này, hệ thống đảm bảo **không bao giờ gửi trùng bản ghi cũ**.

---

### Q11. Ví dụ cụ thể: Lần 1 gửi dữ liệu A, lần 2 trong CSDL vẫn là A (không có dữ liệu mới) thì hệ thống có gửi không?
* **Trả lời trọng tâm:**  
  Tùy thuộc vào chính sách của gói tin:
  1. **Nếu là GÓI NỐI ĐUÔI (Biến động - 103, 104, 106, 107, 109):**
     - **$\rightarrow$ TUYỆT ĐỐI KHÔNG GỬI!**
     - **Giải thích:** Lần 1 đã gửi A xong thì mốc `LastTime = T_A`, `LastKey = ID_A`. Sang lần 2, câu query áp điều kiện lọc `Watermark > T_A OR (Watermark = T_A AND ID > ID_A)` $\rightarrow$ Bản ghi A bị loại bỏ, kết quả trả về `0 bản ghi` (`RawRows.Count == 0`). Hệ thống **dừng ngay tại Chặng 1, không gọi HTTP sang đối tác**, chỉ ghi 1 dòng log phẳng `Success (0 bản ghi, NoNewData)`.
     - *Ngoại lệ:* Nếu bản ghi A được **UPDATE** trong CSDL (cột `UpdateTime` của A tăng lên thành $T_{A\_mới} > T_A$), kỳ quét sau hệ thống sẽ bắt được A như một bản ghi cập nhật mới và gửi đi.
  2. **Nếu là GÓI SNAPSHOT (Hiện trạng - 101, 102, 105, 108, 110):**
     - **$\rightarrow$ VẪN GỬI BÌNH THƯỜNG!**
     - **Giải thích:** Gói Snapshot không dùng mốc `LastSend`, mỗi chu kỳ là một bản chụp tức thời gửi sang để đối tác duy trì giám sát "nhịp tim" và hiện trạng tuyến đường mới nhất, ngay cả khi nội dung trạng thái không thay đổi.

---

### Q12. Gói tin gửi đi có cấu trúc như thế nào qua HTTP?
* **Trả lời trọng tâm:**  
  - **Phương thức:** `HTTP POST`.
  - **Header định danh:**
    - `PartnerCode`: Mã định danh đối tác (ví dụ: `DOITAC_HNCL`).
    - `PacketCode`: Mã gói tin (ví dụ: `101_commonData`).
    - `SerialNbr`: Số thứ tự gói tin tự tăng (ví dụ: `1042`).
    - `Content-Type`: `application/json; charset=utf-8`.
  - **Body:** Chuỗi JSON thuần túy (Plain JSON) tuân thủ đúng khuôn mẫu đã cấu hình trong hồ sơ ánh xạ (không bọc phong bì PDU phức tạp).

---

## Phần 4: Nhóm câu hỏi Xử lý Sự cố & Độ tin cậy

### Q13. Nếu mạng rớt hoặc máy chủ đối tác trả về HTTP 500 thì sao? Có bị mất dữ liệu không?
* **Trả lời trọng tâm:**  
  - **Hoàn toàn KHÔNG mất dữ liệu.**
  - **Cơ chế xử lý:** Khi gặp lỗi Timeout hoặc HTTP 4xx/5xx:
    1. Hệ thống đánh dấu phiên gửi là `Thất bại` (Failed) và ghi chi tiết mã lỗi HTTP vào dòng con Bước 2 (Transport).
    2. Bắn mã cảnh báo `ESH-1302` (Lỗi kết nối) hoặc `ESH-1303` (Đối tác từ chối).
    3. **Quan trọng nhất:** Hệ thống **giữ nguyên mốc `LastSend` cũ**. Đến chu kỳ lịch tiếp theo, Worker sẽ tự động quét lại đúng các bản ghi chưa gửi thành công để gửi lại (Cơ chế tự động Retry).

---

### Q14. Khi đối tác phản ánh "không nhận được dữ liệu", mình kiểm tra ở đâu để đối soát?
* **Trả lời trọng tâm:**  
  Quy trình kiểm tra 3 bước nhanh chóng:
  1. **Bước 1 - Màn hình Nhật ký (Tab Truyền nhận):** Lọc theo mã đối tác và gói tin xem có phiên gửi nào không. Nếu có: xem Bước 2 (Vận chuyển) thành công hay thất bại, mã HTTP phản hồi là bao nhiêu.
  2. **Bước 2 - Màn hình Cảnh báo lỗi:** Kiểm tra xem có cảnh báo mã `ESH-1302` (mạng chết), `ESH-1304` (chưa bật mapping), hay `ESH-1305` (JSON lỗi) không.
  3. **Bước 3 - Kiểm tra tệp JSON trên Server:** Mở thư mục backup `E:\IIS_WebPool\ITS\ITS015\Services\TA-ShareData-Service\sharedata` trên máy `10.10.8.30`. Tìm file JSON theo ngày giờ để làm bằng chứng xác thực gói tin đã được sinh ra và gửi đi.

---

### Q15. Các mã cảnh báo sự cố ESH thường gặp ở chiều gửi là gì?
* **Trả lời trọng tâm:**  
  | Mã lỗi | Ý nghĩa | Cách xử lý nhanh |
  |---|---|---|
  | **ESH-1301** | Chưa đăng ký hoặc đối tác bị khóa | Kiểm tra trạng thái Đối tác (`Enable`) và Đăng ký (`Active`) |
  | **ESH-1302** | Kết nối mạng thất bại (Timeout/Từ chối) | Kiểm tra IP/Port và đường truyền tới máy chủ đối tác |
  | **ESH-1303** | Đối tác trả về HTTP lỗi (4xx / 5xx) | Báo đối tác kiểm tra API tiếp nhận của họ |
  | **ESH-1304** | Chưa bật Hồ sơ ánh xạ (`Mapping`) | Vào màn Ánh xạ dữ liệu, chọn hồ sơ và bấm **BẬT "Đang dùng"** |
  | **ESH-1305** | Cấu hình `TargetShapeJson` sai cú pháp JSON | Kiểm tra lại dấu ngoặc và cấu trúc JSON trong hồ sơ |
  | **ESH-1306** | Dữ liệu nguồn thiếu trường bắt buộc | Kiểm tra dữ liệu CSDL nguồn thiếu cột nghiệp vụ |

---

## Phần 5: Hiện trạng Dữ liệu Staging & Kịch bản Test/Demo

### Q16. Hiện tại trên Staging đã có data test những gói nào rồi?
* **Trả lời trọng tâm:**  
  Hiện tại trên CSDL `DEV_ITS10` (máy `10.10.8.30`) đã có sẵn dữ liệu thực tế lớn cho **5 gói tin**:
  - **Gói 101 (Giao thông chung):** Có hơn 448k bản ghi thống kê xe và 299 trạng thái khu vực.
  - **Gói 103 (Thiết bị dò xe VDS):** Có hơn 342k bản ghi dữ liệu đo đếm lưu lượng xe.
  - **Gói 104 (Trạm thời tiết):** Có 20 bản ghi quan trắc thời tiết Campbell.
  - **Gói 107 (Sự cố giao thông):** Có hơn 1.9k bản ghi sự cố và loại sự cố.
  - **Gói 108 (Biển báo điện tử VMS):** Có 71 bản ghi trạng thái hiển thị biển báo.
  $\rightarrow$ *Có thể bật Đăng ký gửi để test hoặc demo ngay lập tức 5 gói này!*

---

### Q17. Những gói nào chưa có data hoặc đang gặp vướng mắc? Hướng xử lý?
* **Trả lời trọng tâm:**  
  Có **5 gói tin** cần lưu ý:
  1. **Gói 102 (CCTV):** Bảng `CctvDevice` chỉ có 1 camera mẫu, chưa có dữ liệu ảnh snapshot thực tế $\rightarrow$ *Hướng xử lý:* Thêm URL ảnh mẫu vào DB nếu cần test.
  2. **Gói 105 (RFID):** Bảng `TollTransactionOut` có data xe nhưng danh mục trường đang bị lệch ID $\rightarrow$ *Hướng xử lý:* Cần rà soát và cấu hình lại trường trong màn Ánh xạ.
  3. **Gói 106 (WIM Cân xe):** Đang trỏ vào bảng VDS chung, chưa có bảng cân xe chuyên biệt $\rightarrow$ *Hướng xử lý:* Chờ cập nhật bảng cân xe hoặc test luồng đo đếm chung.
  4. **Gói 109 (ETC):** Có dữ liệu xe qua trạm nhưng cột giá cước `tollPrice` bị null $\rightarrow$ *Hướng xử lý:* Cần bổ sung giá trị cước mẫu vào bảng giao dịch trạm.
  5. **Gói 110 (Cảnh báo WP):** Dữ liệu sự cố cũ trên DB đều đã đóng (`State = '4'`), gói 110 chỉ lọc sự cố đang mở $\rightarrow$ *Hướng xử lý:* Chỉ cần tạo 1 sự cố mới ở trạng thái đang mở (`State = '1'`) là test được ngay.

---

### Q18. Bây giờ muốn demo luồng gửi cho sếp hoặc đối tác xem ngay thì làm thế nào?
* **Trả lời trọng tâm:**  
  Quy trình demo thực tế trong 3 phút:
  1. **Chuẩn bị đầu nhận:** Dùng công cụ Mock Endpoint trực tuyến (như `Webhook.site` hoặc Postman Mock) để lấy 1 URL tiếp nhận.
  2. **Cấu hình trên giao diện:**
     - Vào màn **Chia sẻ dữ liệu** $\rightarrow$ Sửa địa chỉ Endpoint của đối tác thành URL Webhook trên $\rightarrow$ Bấm **Kết nối**.
     - Vào màn **Ánh xạ dữ liệu** $\rightarrow$ Bật "Đang dùng" hồ sơ của **Gói 101 hoặc Gói 107**.
     - Vào màn **Chia sẻ dữ liệu** $\rightarrow$ Bật Đăng ký gửi (chọn chu kỳ 30 giây hoặc bấm gửi theo sự kiện).
  3. **Xem kết quả:**
     - Mở trang `Webhook.site`: Thấy ngay payload JSON gửi sang đầy đủ headers và cấu trúc dữ liệu.
     - Mở màn hình **Nhật ký**: Thấy phiên gửi `Success` gồm dòng Cha và 2 dòng Con chi tiết.
     - Vào thư mục `E:\IIS_WebPool\...\sharedata` trên server: Mở file JSON lưu đối soát.

---

## Bảng Cheat Sheet: Tóm tắt trả lời siêu tốc trong 15 giây

| Từ khóa câu hỏi | Điểm mấu chốt cần trả lời ngay |
|---|---|
| **Luồng đi như thế nào?** | DB `DEV_ITS10` $\rightarrow$ Trích xuất (LastSend) $\rightarrow$ Map (TargetShapeJson + CodeSet) $\rightarrow$ HTTP POST + Backup Disk $\rightarrow$ Đối tác. |
| **Kích hoạt khi nào?** | 2 chế độ: Theo lịch hẹn (Cron/Interval) HOẶC Theo sự kiện tức thì (SQL Change Tracking + NATS). |
| **Tại sao không gửi?** | Thiếu 1 trong 4 điều kiện: Chưa mở kết nối đối tác, Đăng ký chưa Active, **chưa BẬT hồ sơ ánh xạ (ESH-1304)**, hoặc CSDL không có dữ liệu mới. |
| **Mất mạng có mất data không?** | Không mất. Giữ nguyên mốc `LastSend`, ghi log lỗi ESH-1302, kỳ sau tự động retry. |
| **Chống trùng dữ liệu thế nào?** | Dùng mốc nối đuôi `LastSend` theo cặp `(Đối tác, Gói tin)`. Chỉ gửi bản ghi mới hơn lần trước. |
| **Đối tác kêu không nhận được thì làm sao?** | Kiểm tra màn hình **Nhật ký** (tab Truyền nhận), màn hình **Cảnh báo lỗi** (mã ESH), và mở file JSON lưu ở thư mục `E:\IIS_WebPool\...\sharedata`. |
| **Gói nào test được ngay?** | 5 gói đã có data thật: **101 (Giao thông), 103 (VDS), 104 (Thời tiết), 107 (Sự cố), 108 (VMS)**. |

