# Bộ Câu hỏi & Trả lời phỏng vấn / Báo cáo PM (Chị T): Luồng Gửi (Outbound) & Luồng Nhận (Inbound) Dữ liệu

> **Quyết định hiện hành đã chốt 14/10/2026:** **105 mặc định Snapshot** theo [mapping `All/update`](../doc/02-mapping-goi-tin-101-111.md), setting gói cho chọn Snapshot/Incremental và Worker dùng setting đã lưu; không có TOP cứng. **110 được tạo/cấu hình bình thường với bảng/dữ liệu mô phỏng**. Viết sẵn code Kafka producer, CSV/FTP, mã hóa, cleanup để dùng theo cấu hình và chạy Playwright sau sửa FE. Danh sách nhóm gói bên dưới là mặc định, không thay setting; hiện trạng đo/rà soát cũ không thay quyết định mới. Theo dõi triển khai tại [MasterPlan](../Plan/Sharedata_MasterPlan.md).

> **Mục đích:** Tài liệu "phao cứu sinh" (Cheat Sheet) tổng hợp toàn bộ các câu hỏi từ tổng quan đến chi tiết kỹ thuật mà PM (Chị T) hoặc các bên liên quan có thể hỏi về **Luồng Gửi dữ liệu (ShareData Outbound)** và **Luồng Nhận dữ liệu (ShareData Inbound, Phần 6)** của dự án Cao tốc Hữu Nghị – Chi Lăng.  
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
- [Phần 6: Luồng Nhận dữ liệu (Inbound)](#phần-6-luồng-nhận-dữ-liệu-inbound)
  - [Q19. Luồng Nhận hoạt động như thế nào? Bức tranh tổng thể 1 phút?](#q19-luồng-nhận-hoạt-động-như-thế-nào-bức-tranh-tổng-thể-1-phút)
  - [Q20. Endpoint nhận kiểm tra những gì và khi nào báo lỗi cho đối tác?](#q20-endpoint-nhận-kiểm-tra-những-gì-và-khi-nào-báo-lỗi-cho-đối-tác)
  - [Q21. Gói tin nhận về có những trạng thái nào?](#q21-gói-tin-nhận-về-có-những-trạng-thái-nào)
  - [Q22. Điều kiện để Worker xử lý gói? Vì sao gói nằm im ở Pending?](#q22-điều-kiện-để-worker-xử-lý-gói-vì-sao-gói-nằm-im-ở-pending)
  - [Q23. Thứ tự, lô và cơ chế chống xử lý trùng của Worker?](#q23-thứ-tự-lô-và-cơ-chế-chống-xử-lý-trùng-của-worker)
  - [Q24. Ghi dữ liệu vào CSDL đích có an toàn không? Lỗi giữa chừng thì sao?](#q24-ghi-dữ-liệu-vào-csdl-đích-có-an-toàn-không-lỗi-giữa-chừng-thì-sao)
  - [Q25. Ánh xạ ngược và bộ mã (CodeSet) chiều Nhận hoạt động ra sao?](#q25-ánh-xạ-ngược-và-bộ-mã-codeset-chiều-nhận-hoạt-động-ra-sao)
  - [Q26. Đối tác nói "đã gửi nhưng trong DB không có", kiểm tra ở đâu?](#q26-đối-tác-nói-đã-gửi-nhưng-trong-db-không-có-kiểm-tra-ở-đâu)
  - [Q27. Các mã cảnh báo ESH chiều Nhận là gì?](#q27-các-mã-cảnh-báo-esh-chiều-nhận-là-gì)
  - [Q28. Gói bị Failed hoặc NoSubscription có tự xử lý lại không?](#q28-gói-bị-failed-hoặc-nosubscription-có-tự-xử-lý-lại-không)

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
  2. **Theo sự kiện dữ liệu mới (Event-driven):** Bật cờ `SendOnNewData` trong Đăng ký chia sẻ. Cơ chế **SQL Change Tracking** bắt ngay biến động bảng nghiệp vụ $\rightarrow$ bắn tín hiệu qua **NATS Message Broker** $\rightarrow$ Worker kích hoạt lượt gửi ngay lập tức (Near real-time), đặc biệt quan trọng với **Gói 107 (Sự cố giao thông)**.

---

### Q3. Hệ thống gửi dữ liệu cho ai và có bao nhiêu loại gói tin?
* **Trả lời trọng tâm:**  
  - **Đối tác:** Bất kỳ đơn vị bên ngoài nào được khai báo trong bảng `ShareDataPartner` (ví dụ: Cục Đường bộ Việt Nam - DRVN, Cảnh sát Giao thông, Trung tâm điều hành tuyến lân cận...).
  - **Gói tin:** Chuẩn hóa gồm **10 loại gói tin (từ 101 đến 110)**:
    - *Nhóm hiện trạng (Snapshot):* 101 (Giao thông chung), 102 (Camera CCTV), 105 (Nhận dạng RFID), 108 (Biển báo VMS), 110 (chưa triển khai).
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
  Toàn bộ dữ liệu chiều Gửi được trích xuất trực tiếp từ CSDL vận hành chính **`DEV_ITS10`**. Dưới đây là chi tiết bảng chính (`FROM`), các bảng phụ liên kết (`LEFT JOIN ... ON ...`) và mục đích nghiệp vụ của từng gói tin:

  | Gói tin | Bảng chính (FROM) | Bảng liên kết (LEFT JOIN ... ON ...) | Mục đích nghiệp vụ của phép JOIN |
  | :--- | :--- | :--- | :--- |
  | **101** (Lưu lượng - Snapshot) | `TmsZoneStatus zs` | 1. `TmsZone z` ON `zs.ZoneId = z.ID`<br/>2. `TmsTrafficStatistic ts` ON `zs.ZoneId = ts.ZoneId` | • Lấy tên đoạn đường (`zoneName`), lý trình từ–đến Km/Met, giới hạn tốc độ (`speedLimit`).<br/>• Lấy tổng lưu lượng xe đếm được qua khu vực (`vehicleCount`). |
  | **102** (Camera CCTV - Snapshot) | `CctvDevice c` | `TmsEquipment e` ON `c.Ip = e.Ip` | • Nối qua IP camera để lấy mã quản lý thiết bị định danh (`cameraCode`), lý trình lắp đặt trên tuyến (`locationKm`, `locationMet`) và hướng tuyến (`direction`). |
  | **103** (Dò xe VDS - Nối đuôi) | `TmsTrafficData td` | `TmsEquipment e` ON `td.EquipmentId = e.ID` | • Lấy vị trí lý trình Km/Met nơi đặt cảm biến dò xe ghi nhận phương tiện đi qua. |
  | **104** (Thời tiết - Nối đuôi) | `TmsWeather w` | *(Không JOIN - Bảng đơn)* | • Toàn bộ thông số thời tiết (nhiệt độ, độ ẩm, tốc độ/hướng gió, lượng mưa, tầm nhìn) đã có sẵn đầy đủ trên `TmsWeather`. |
  | **105** (Định danh RFID - Snapshot) | `TollTransactionIn i`<br/>*(Xe vào trạm)* | `TmsVehicleRegistration vr`<br/>ON `COALESCE(i.PlateEdit, i.PlateLpr) = vr.LicensePlate` | • Nối qua biển số xe nhận diện (ưu tiên biển soát vé đã chỉnh sửa `PlateEdit`, sau đó đến biển tự động `PlateLpr`) để lấy thông tin đăng ký: Hãng xe (`vehicleBrand`), Chủ xe (`vehicleOwner`). |
  | **106** (Cân tải trọng WIM - Nối đuôi) | `TmsTrafficData td` | *(Không JOIN - Bảng đơn)* | • Trích xuất 7 trường đo đếm kích thước & tốc độ xe có sẵn (`detectTime`, `lane`, `locationCode`, `speed`, `height`, `width`, `length`).<br/>*(4 trường tải trọng trả null do chưa có bảng trạm cân riêng)*. |
  | **107** (Sự cố giao thông - Nối đuôi) | `TmsIncident i` | `TmsEventType et` ON `i.EventTypeId = et.ID` | • Lấy tên phân loại sự cố chuẩn hóa (`eventTypeName` - ví dụ: Tai nạn, Ùn tắc, Cháy xe, Đất đá sạt lở...). |
  | **108** (Biển báo VMS - Snapshot) | `VmsCurrent v` | `TmsEquipment e` ON `v.EquipmentId = e.ID` | • Lấy mã thiết bị biển báo (`equipmentCode`), vị trí lý trình lắp đặt (`locationKm`, `locationMet`), hướng tuyến và làn đường gắn biển. |
  | **109** (Thu phí ETC - Nối đuôi) | `TollTransactionOut t`<br/>*(Xe ra trạm)* | 1. `TollLane l` ON `t.LaneId = l.LaneId`<br/>2. `TollStation s` ON `t.StationId = s.StationId` | • Lấy tên làn thu phí (`laneName`).<br/>• Lấy tên trạm thu phí (`stationName`). |
  | **110** (Cảnh báo WP - NotReady) | *Chưa query* | *(Handler trả về rỗng theo policy NotReady)* | • Nghiệp vụ cảnh báo phát hành đang chờ chuẩn hóa mô hình từ đối tác (khi hoàn thiện sẽ kết hợp `TmsIncident` với `VmsCurrent` / `TmsEquipment`). |

---

### Q9. Khác biệt giữa Gói gửi toàn bộ (Snapshot) và Gói gửi nối đuôi (Biến động)?
* **Trả lời trọng tâm:**  
  - **Khi setting là Snapshot:** mỗi lần đến lịch gửi lấy toàn bộ theo SQL cấu hình, không ép TOP cứng và không dùng LastSend để lọc dữ liệu. Mặc định 101, 102, **105**, 108 là Snapshot; **105 theo mapping `All/update`, đã chốt**. Người dùng được chọn Incremental và Worker phải dùng setting/cursor tương ứng. **110 được tạo/cấu hình bình thường**, bổ sung bảng/dữ liệu mô phỏng và test thông luồng; trạng thái triển khai được ghi riêng trong MasterPlan.
  - **Gói Biến động / Nối đuôi (103, 104, 106, 107, 109):** Dữ liệu phát sinh liên tục theo dòng sự kiện (xe chạy qua cảm biến, thời tiết từng phút, sự cố). Hệ thống quét dữ liệu tăng dần qua con trỏ kép `(LastTime, LastKey)` lưu trong bảng `ShareDataLastSend`. Phân trang tối đa **100 bản ghi/trang**.

---

### Q10. Cơ chế nối đuôi (LastSend) hoạt động ra sao? Làm sao chống trùng dữ liệu?
* **Trả lời trọng tâm:**  
  - Bảng `ShareDataLastSend` lưu giữ mốc con trỏ kép `(LastTime, LastKey)` độc lập cho từng cặp `(PartnerCode, PacketCode)`.
  - **Mốc thời gian (`LastTime`):** `Watermark = COALESCE(UpdateTime, CreateTime, [EventTime])`.
  - **Mốc khóa (`LastKey`):** Khóa định danh `ID` (`__lastKey`) của bản ghi cuối cùng trong trang (dùng để phân định rạch ròi khi nhiều bản ghi có cùng một tích tắc thời gian).
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
    2. Bắn mã cảnh báo `ESH-1402` (mọi lỗi gửi HTTP); file JSON vẫn được xuất, phiên ghi Fail, mốc giữ nguyên để gửi lại.
    3. **Quan trọng nhất:** Hệ thống **giữ nguyên mốc `LastSend` cũ**. Đến chu kỳ lịch tiếp theo, Worker sẽ tự động quét lại đúng các bản ghi chưa gửi thành công để gửi lại (Cơ chế tự động Retry).

---

### Q14. Khi đối tác phản ánh "không nhận được dữ liệu", mình kiểm tra ở đâu để đối soát?
* **Trả lời trọng tâm:**  
  Quy trình kiểm tra 3 bước nhanh chóng:
  1. **Bước 1 - Màn hình Nhật ký (Tab Truyền nhận):** Lọc theo mã đối tác và gói tin xem có phiên gửi nào không. Nếu có: xem Bước 2 (Vận chuyển) thành công hay thất bại, mã HTTP phản hồi là bao nhiêu.
  2. **Bước 2 - Màn hình Cảnh báo lỗi:** Kiểm tra xem có cảnh báo mã `ESH-1402` (gửi HTTP lỗi), `ESH-1304` (chưa bật mapping), hay `ESH-1202` / `ESH-1206` (thiếu trường bắt buộc / shape sai) không.
  3. **Bước 3 - Kiểm tra tệp JSON trên Server:** Mở thư mục backup `E:\IIS_WebPool\ITS\ITS015\Services\TA-ShareData-Service\sharedata` trên máy `10.10.8.30`. Tìm file JSON theo ngày giờ để làm bằng chứng xác thực gói tin đã được sinh ra và gửi đi.

---

### Q15. Các mã cảnh báo sự cố ESH thường gặp ở chiều gửi là gì?
* **Trả lời trọng tâm:**  
  | Mã lỗi | Ý nghĩa | Cách xử lý nhanh |
  |---|---|---|
  | **ESH-1201** | Bộ mã quy đổi khai trong metadata không có trong CSDL | Khai báo bộ mã tương ứng trong bảng `ShareDataCodeSet` |
  | **ESH-1202** | Thiếu hoặc null trường bắt buộc → hủy kết xuất lượt gửi | Bổ sung dữ liệu nguồn trong CSDL hoặc kiểm tra danh mục trường bắt buộc |
  | **ESH-1203** | Biểu thức tính toán lỗi → giữ giá trị gốc, vẫn gửi | Kiểm tra lại cú pháp biểu thức trong cấu hình trường/mapping |
  | **ESH-1204** | Số nguyên bị nâng thành decimal sau quy đổi đơn vị | Kiểm tra lại công thức quy đổi đơn vị và kiểu trường đích |
  | **ESH-1205** | *(Không còn dùng)* Trường trong shape không có trong dòng thô | Mã cũ không còn sử dụng trong hệ thống |
  | **ESH-1206** | `TargetShapeJson` rỗng hoặc sai cú pháp JSON → hủy kết xuất | Chuẩn hóa và kiểm tra lại cú pháp JSON trong hồ sơ ánh xạ |
  | **ESH-1301** | Không có cấu hình gói tin cho `DatatypeId` | Khai báo cấu hình gói tin trong danh mục `ShareDataPacket` |
  | **ESH-1302** | Lỗi truy vấn dữ liệu nguồn từ CSDL | Kiểm tra câu truy vấn SQL và tình trạng kết nối CSDL `DEV_ITS10` |
  | **ESH-1303** | Mất lease phân tán, node Worker khác đang nhận đăng ký | Bình thường trong cụm multi-worker; kiểm tra tải nếu mất lease liên tục |
  | **ESH-1304** | Không có hồ sơ ánh xạ chiều gửi đang bật ("Đang dùng") → hủy | Vào màn Ánh xạ dữ liệu, chọn hồ sơ của gói tin và bấm **BẬT "Đang dùng"** |
  | **ESH-1305** | Khóa `__lastKey` dài hơn độ rộng cột `LastKey` → hủy trước khi gửi | Kiểm tra kiểu dữ liệu khóa chính và độ rộng cột `LastKey` bảng `ShareDataLastSend` |
  | **ESH-1401** | Lỗi ghi file kết xuất JSON ra ổ đĩa server | Kiểm tra quyền ghi thư mục và dung lượng trống trên ổ đĩa server |
  | **ESH-1402** | Mọi lỗi gửi HTTP tới đối tác (4xx/5xx/timeout/exception) | Kiểm tra mạng và máy chủ đối tác; file JSON vẫn được xuất, hệ thống giữ mốc tự retry kỳ sau |

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
  2. **Gói 105 (RFID):** Bảng `TollTransactionIn` có data xe nhưng danh mục trường đang bị lệch ID $\rightarrow$ *Hướng xử lý:* Cần rà soát và cấu hình lại trường trong màn Ánh xạ.
  3. **Gói 106 (WIM Cân xe):** Đang trỏ vào bảng VDS chung, chưa có bảng cân xe chuyên biệt $\rightarrow$ *Hướng xử lý:* Chờ cập nhật bảng cân xe hoặc test luồng đo đếm chung.
  4. **Gói 109 (ETC):** Có dữ liệu xe qua trạm nhưng cột giá cước `tollPrice` bị null $\rightarrow$ *Hướng xử lý:* Cần bổ sung giá trị cước mẫu vào bảng giao dịch trạm.
  5. **Gói 110 (Cảnh báo WP):** Chưa triển khai (handler trả rỗng). Tạo sự cố mới không giúp test được; cần triển khai handler trước.

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

## Phần 6: Luồng Nhận dữ liệu (Inbound)

> **Căn cứ:** đã đối chiếu mã `ShareDataInboundController`, `InboundCommandHandler` (WebAPI) và `DataInboundService` (Worker). Mục nào ghi **[Cần dev xác nhận]** là chưa đọc tận mã hoặc cần chốt ý đồ thiết kế.

### Q19. Luồng Nhận hoạt động như thế nào? Bức tranh tổng thể 1 phút?
* **Trả lời trọng tâm:**  
  Luồng Nhận tách làm **2 tầng bất đồng bộ**:
  1. **Tầng WebAPI (nhận và lưu thô):** đối tác gọi `HTTP POST .../Receive` kèm 3 header `PartnerCode`, `PacketCode`, `SerialNbr` và body JSON. WebAPI kiểm tra đối tác, tra đăng ký, **lưu nguyên văn body** vào bảng `ShareDataInboundPacket` (CSDL `DEV_ITS10_Inbound`) rồi trả về ngay. Không bóc tách gì ở bước này.
  2. **Tầng Worker (`DataInboundService`):** định kỳ quét các gói `Pending`, tra cấu hình, bóc tách JSON, ánh xạ ngược (CodeSet), chạy các câu ghi SQL vào bảng đích trong **1 transaction**, rồi đánh dấu `Done` hoặc `Failed`.
  - Dữ liệu nhận về **chỉ ghi vào `DEV_ITS10_Inbound`**, không ghi vào CSDL vận hành chính `DEV_ITS10`.

---

### Q20. Endpoint nhận kiểm tra những gì và khi nào báo lỗi cho đối tác?
* **Trả lời trọng tâm:**  
  - **Xác thực:** endpoint để `[AllowAnonymous]` (không đăng nhập, không token). Đối tác chỉ được nhận diện bằng header `PartnerCode`. Chưa thấy giới hạn IP hay kích thước body **[Cần dev xác nhận]**.
  - **Header:** `SerialNbr` là số nguyên, **tùy chọn**; giá trị không phải số bị bỏ qua lặng lẽ thành rỗng. `PartnerCode` và `PacketCode` được cắt khoảng trắng đầu/cuối.
  - **Đối tác sai (báo lỗi cho đối tác):** không tìm thấy mã đối tác (kể cả thiếu header) → lỗi `DataNotExist`; đối tác đang bị khóa → lỗi `PartnerDisabled`. Gói **không** được lưu.
  - **Chưa có đăng ký (KHÔNG báo lỗi):** không tìm thấy gói tin, hoặc chưa có đăng ký chiều Nhận, hoặc đăng ký đã hết hiệu lực → gói vẫn được **lưu** với trạng thái `NoSubscription` kèm lý do trong `ErrorMessage`, ghi một dòng nhật ký lỗi, và handler **không ném lỗi** nên đối tác coi như gửi thành công. Mã HTTP cụ thể: **[Cần dev xác nhận]** (mặc định 200 khi không có ngoại lệ).
  - **Có đăng ký hợp lệ:** gói lưu với `Pending`. Đăng ký "hợp lệ" ở bước nhận gồm trạng thái `Active`, `Paused`, `Pending`.
  - **Dữ liệu lưu kèm:** `ByteSize` (số byte UTF-8 của body) và `ReceiveLogId` (id dòng nhật ký cha, để Worker cập nhật tiếp).
  - **Lỗi khi lưu CSDL:** ghi nhật ký thất bại rồi ném lỗi lại cho đối tác.

---

### Q21. Gói tin nhận về có những trạng thái nào?
* **Trả lời trọng tâm:**  

  | Giá trị | Trạng thái | Ai đặt, khi nào |
  |---|---|---|
  | 0 | `Pending` | WebAPI khi nhận gói có đăng ký hợp lệ; Worker khi trả gói kẹt về hàng đợi |
  | 1 | `Processing` | Worker khi "chiếm chỗ" gói để xử lý |
  | 2 | `Done` | Worker sau khi ghi CSDL đích thành công |
  | 3 | `Failed` | Worker khi lỗi (JSON không đọc được, đối tác/gói không hợp lệ, thiếu câu ghi, lỗi SQL) |
  | 4 | `NoSubscription` | WebAPI khi nhận gói nhưng chưa có đăng ký hợp lệ; Worker cũng đặt khi đăng ký đã hết hiệu lực lúc xử lý |

---

### Q22. Điều kiện để Worker xử lý gói? Vì sao gói nằm im ở Pending?
* **Trả lời trọng tâm:**  
  Với mỗi nhóm (đối tác × loại gói), Worker kiểm tra lần lượt:
  1. **Đối tác** tồn tại và đang `Enable` → nếu không: gói `Failed`, `ESH-1507`.
  2. **Gói tin** có trong danh mục (`ShareDataPacket`) → nếu không: `Failed`, `ESH-1506`.
  3. **Đăng ký chiều Nhận** (bản mới nhất) có và còn hiệu lực → nếu không: `NoSubscription`, `ESH-1508`.
  4. **Đăng ký phải `Active`.** Nếu là `Paused` hoặc `Pending`: Worker **bỏ qua nhóm, giữ gói ở `Pending`, không báo lỗi**. Đây là lý do thường gặp nhất khiến "gói nằm im".
  5. **Có câu ghi** (`WriteSql`) cho gói tin trong bảng `ShareDataPacketSql` (đang bật, chạy theo `OrderNo`) → nếu không: `Failed`, `ESH-1509`.
  6. **Hồ sơ ánh xạ** chiều Nhận hoặc Hai chiều đang bật: **không bắt buộc**. Nếu thiếu, Worker vẫn xử lý bằng cách tìm mảng đối tượng phẳng trong JSON (có test "không có mapping vẫn thành công"). Chiều Nhận **không** báo `ESH-1304` như chiều Gửi.
  7. **Chuỗi kết nối** `InboundConnection` phải được cấu hình, nếu thiếu thì Worker **bỏ qua toàn bộ luồng Nhận** và chỉ ghi log lỗi.
  - Lưu ý tên bảng: câu ghi nằm ở bảng `ShareDataPacketSql` (cột `WriteSql`), không phải `ShareDataPacketWrite` như Spec đang ghi.

---

### Q23. Thứ tự, lô và cơ chế chống xử lý trùng của Worker?
* **Trả lời trọng tâm:**  
  - **Gom nhóm:** các gói `Pending` được gom theo (`PartnerCode`, `PacketCode`); mỗi nhóm dùng chung một bộ cấu hình.
  - **Thứ tự:** trong nhóm xử lý theo `SerialNbr` tăng dần, rồi `CreateTime`.
  - **Lô:** tối đa **50 gói / nhóm / lượt** (`MaxPacketsPerRun`).
  - **Chống xử lý trùng:** trước khi xử lý, Worker "chiếm chỗ" bằng một lệnh cập nhật có điều kiện `Pending → Processing`; chỉ tiến trình nào chiếm được mới xử lý.
  - **Gói kẹt:** gói nằm ở `Processing` quá **10 phút** (`StuckProcessingTimeout`) được tự trả về `Pending` ở đầu mỗi lượt.
  - **Một gói lỗi không chặn gói sau:** gói `Failed` được bỏ qua và Worker xử lý tiếp gói kế tiếp.
  - **`SerialNbr` của đăng ký:** sau mỗi lượt, đăng ký Nhận được cập nhật `SerialNbr` = số lớn nhất của các gói **xử lý thành công**; giá trị này **chỉ tăng, không lùi**.
  - Chưa thấy cơ chế phát hiện gói **trùng `SerialNbr`** hay **bỏ sót số thứ tự** **[Cần dev xác nhận]**.

---

### Q24. Ghi dữ liệu vào CSDL đích có an toàn không? Lỗi giữa chừng thì sao?
* **Trả lời trọng tâm:**  
  - Toàn bộ các câu ghi của **một gói** chạy trong **một transaction**, mỗi câu nhận cả mảng bản ghi dưới dạng tham số JSON (kiểu `MERGE` / `OPENJSON`).
  - Câu lệnh bất kỳ lỗi → **rollback toàn bộ gói**, gói chuyển `Failed`, cảnh báo `ESH-1505`, thông điệp nêu rõ câu ghi nào lỗi.
  - Gói không có bản ghi nào sau khi bóc tách → không ghi gì nhưng vẫn đánh dấu `Done`.
  - Thành công → gói `Done`, cập nhật dòng nhật ký cha sang thành công và thêm dòng con Bước 2 (Bóc tách & Lưu trữ).

---

### Q25. Ánh xạ ngược và bộ mã (CodeSet) chiều Nhận hoạt động ra sao?
* **Trả lời trọng tâm:**  
  - **Có hồ sơ ánh xạ:** Worker đọc khuôn JSON chiều Nhận để biết từng trường (`fieldKey`) nằm ở đâu trong JSON đối tác gửi.
  - **CodeSet:** chỉ lấy các bộ mã khai trong `$extend.codeSet` của hồ sơ ánh xạ, tra ngược `partnerValue → sourceValue` (ví dụ `"CONGESTION"` → `2`).
  - **Không có hồ sơ:** lấy trực tiếp các thuộc tính của từng đối tượng trong mảng JSON phẳng, không quy đổi mã.
  - **Định dạng:** chỉ chấp nhận JSON. Nội dung bắt đầu bằng `<` (XML) hoặc JSON hỏng → gói `Failed`, `ESH-1503`.

---

### Q26. Đối tác nói "đã gửi nhưng trong DB không có", kiểm tra ở đâu?
* **Trả lời trọng tâm:**  
  1. **Bảng `ShareDataInboundPacket`** (CSDL `DEV_ITS10_Inbound`): tìm theo `PartnerCode`, `PacketCode`, `SerialNbr`. Xem `ProcessState`, `ProcessedAt`, `ErrorMessage`.
     - Không có dòng nào → gói chưa tới WebAPI, hoặc WebAPI đã báo lỗi đối tác (`DataNotExist`/`PartnerDisabled`).
     - `NoSubscription` → chưa có đăng ký Nhận hợp lệ.
     - `Pending` lâu → đăng ký đang `Paused`/`Pending`, hoặc Worker chưa chạy/chưa cấu hình `InboundConnection`.
     - `Failed` → đọc `ErrorMessage`.
  2. **Màn hình Nhật ký (Tab Truyền nhận, chiều RCV):** xem dòng cha và các dòng con.
  3. **Màn hình Cảnh báo lỗi:** tìm mã `ESH-15xx` (Q27).
  4. **Bảng đích** trong CSDL `DEV_ITS10_Inbound` (ví dụ `TmsIncident`, `TmsWeather`...).

---

### Q27. Các mã cảnh báo ESH chiều Nhận là gì?
* **Trả lời trọng tâm:**  

  | Mã lỗi | Ý nghĩa | Cách xử lý nhanh |
  |---|---|---|
  | **ESH-1503** | Không đọc được nội dung gói (JSON hỏng, XML) | Yêu cầu đối tác gửi lại JSON hợp lệ |
  | **ESH-1504** | Không xác định được khóa đối chiếu, hoặc khóa khớp nhiều bản ghi | Kiểm tra câu ghi và dữ liệu gói **[Cần dev xác nhận nơi phát sinh]** |
  | **ESH-1505** | Lỗi ghi CSDL nhận (đã rollback) | Đọc `ErrorMessage` để biết câu ghi nào lỗi |
  | **ESH-1506** | Không có cấu hình gói tin cho `PacketCode` | Khai báo gói tin hoặc sửa `PacketCode` đối tác gửi |
  | **ESH-1507** | Đối tác không tồn tại hoặc đang tắt | Kiểm tra màn hình Đối tác |
  | **ESH-1508** | Chưa có đăng ký Nhận hợp lệ | Tạo/duyệt đăng ký chiều Nhận |
  | **ESH-1509** | Gói tin chưa khai câu ghi chiều Nhận | Khai `WriteSql` trong `ShareDataPacketSql` |
  | ESH-1501 / 1502 | Chưa cấu hình URL API nhận / gọi API hộp gửi của đối tác lỗi | Mô tả kiểu "kéo", trong khi luồng đang mô tả là "đẩy" **[Cần dev xác nhận còn dùng không]** |

  > Lưu ý: lỗi ghi CSDL chiều Nhận là **`ESH-1505`**, không phải `ESH-1307` như trong Spec.

---

### Q28. Gói bị Failed hoặc NoSubscription có tự xử lý lại không?
* **Trả lời trọng tâm:**  
  - **Không.** Worker chỉ lấy các gói ở `Pending`. `Failed` và `NoSubscription` là trạng thái cuối.
  - **Hệ quả quan trọng:** nếu đối tác gửi dữ liệu **trước khi** có đăng ký (gói thành `NoSubscription`), việc tạo đăng ký sau đó **không** làm Worker xử lý lại các gói đó. Tương tự, gói `Failed` do dữ liệu hoặc cấu hình sai sẽ không tự chạy lại sau khi sửa cấu hình.
  - **Cách xử lý lại:** hiện chưa thấy chức năng "xử lý lại" trong giao diện; thao tác khả dĩ là đưa gói về `Pending` bằng tay trong CSDL rồi để Worker chạy lại. **[Cần dev xác nhận có nên làm vậy và có tính năng chính thức không]**.
  - Đối tác nhận phản hồi thành công kể cả khi gói bị `NoSubscription` (xem Q20), nên họ sẽ không tự gửi lại. Đây là điểm cần quyết định khi hoàn thiện luồng Nhận.

---

## Bảng Cheat Sheet: Tóm tắt trả lời siêu tốc trong 15 giây

| Từ khóa câu hỏi | Điểm mấu chốt cần trả lời ngay |
|---|---|
| **Luồng đi như thế nào?** | DB `DEV_ITS10` $\rightarrow$ Trích xuất (LastSend) $\rightarrow$ Map (TargetShapeJson + CodeSet) $\rightarrow$ HTTP POST + Backup Disk $\rightarrow$ Đối tác. |
| **Kích hoạt khi nào?** | 2 chế độ: Theo lịch hẹn (Cron/Interval) HOẶC Theo sự kiện tức thì (SQL Change Tracking + NATS). |
| **Tại sao không gửi?** | Thiếu 1 trong 4 điều kiện: Chưa mở kết nối đối tác, Đăng ký chưa Active, **chưa BẬT hồ sơ ánh xạ (ESH-1304)**, hoặc CSDL không có dữ liệu mới. |
| **Mất mạng có mất data không?** | Không mất. Giữ nguyên mốc `LastSend`, ghi log lỗi ESH-1402, kỳ sau tự động retry. |
| **Chống trùng dữ liệu thế nào?** | Dùng mốc nối đuôi `LastSend` theo cặp `(Đối tác, Gói tin)`. Chỉ gửi bản ghi mới hơn lần trước. |
| **Đối tác kêu không nhận được thì làm sao?** | Kiểm tra màn hình **Nhật ký** (tab Truyền nhận), màn hình **Cảnh báo lỗi** (mã ESH), và mở file JSON lưu ở thư mục `E:\IIS_WebPool\...\sharedata`. |
| **Gói nào test được ngay?** | 5 gói đã có data thật: **101 (Giao thông), 103 (VDS), 104 (Thời tiết), 107 (Sự cố), 108 (VMS)**. |
| **Luồng Nhận đi như thế nào?** | Đối tác `POST Receive` (header PartnerCode/PacketCode/SerialNbr) $\rightarrow$ WebAPI lưu thô vào `ShareDataInboundPacket` (Pending) $\rightarrow$ Worker bóc tách + CodeSet ngược $\rightarrow$ ghi `DEV_ITS10_Inbound` trong 1 transaction $\rightarrow$ Done/Failed. |
| **Gói nhận về nằm im ở Pending?** | Đăng ký Nhận đang `Paused`/`Pending` (Worker giữ lại, không báo lỗi), hoặc Worker chưa chạy / thiếu `InboundConnection`. |
| **Gửi trước khi có đăng ký thì sao?** | Gói lưu `NoSubscription`, đối tác vẫn nhận phản hồi thành công, **không tự xử lý lại** khi đăng ký được tạo sau. |
| **Lỗi ghi DB chiều Nhận?** | Rollback cả gói, `Failed`, mã **ESH-1505** (không phải 1307). |

