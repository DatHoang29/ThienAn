---
tier: A
read: full
source: ../../_source/audio/2026-09-09-review-1.m4a, ../../_source/audio/2026-09-09-review-2.m4a
date: 2026-09-09
model: Gemini Whisper ASR + Synthesis
status: verified
---

# Review ShareData — Luồng gửi nhận, Cấu hình gói tin, Mapping đối tác & Chu kỳ gửi

**Ngôn ngữ:** Tiếng Việt  
**Tổng thời lượng:** ~61 phút (Phần 1: 35:36, Phần 2: 25:41)  
**File nguồn:** `_source/audio/2026-09-09-review-1.m4a`, `_source/audio/2026-09-09-review-2.m4a`  
**Chủ đề:** Rà soát toàn diện kiến trúc phân hệ **ShareData (DataPublicationService)**: luồng xử lý file/API, cơ chế cấu hình gói tin động, ánh xạ cột CSDL sang đối tác (SQL Alias), chu kỳ gửi (Interval/Cron/Event), chuẩn hóa Socket Message Wrapper (`H2 data`) và tối giản hàng đợi ưu tiên.

---

## 1. Tóm tắt nội dung toàn diện (Executive Summary)

Toàn bộ buổi họp ngày 09/09/2026 tập trung làm rõ và chốt phương án kỹ thuật cho dịch vụ chia sẻ dữ liệu ra bên ngoài:

1. **Phương thức xuất bản dữ liệu (Ghi file vs Gửi API):**
   - Rà soát luồng trước đây: Ghi file xuống ổ đĩa, service khác quét log pull file đi.
   - Quyết định: Kết hợp song song. Giữ việc ghi file làm tầng backup/lưu trữ đối soát lịch sử; đồng thời bổ sung module bắn trực tiếp qua API đối tác khi có dữ liệu mới phát sinh.
2. **Cấu hình gói tin & Cơ chế ánh xạ (Dynamic Field Mapping):**
   - Mỗi đối tác (Partner A, Partner B, Cục Đường bộ...) có định dạng và tên trường (field name) khác nhau.
   - Quyết định: **Không sửa đổi schema DB nội bộ**. Áp dụng câu truy vấn SQL (Query/View) với **SQL Alias** tương ứng hoặc bảng mapping cấu hình (`DB Column` ↔ `Partner Field Name`).
   - Xử lý chuẩn hóa kiểu dữ liệu: định dạng ngày giờ, đơn vị đo vị trí (km vs mét), mã hóa trạng thái thiết bị / khu vực (`0/1` hoặc `true/false`, string enum).
3. **Chu kỳ gửi & Xử lý sự kiện khẩn cấp (Incident / Event-driven):**
   - Hỗ trợ 3 chế độ gửi:
     - **Theo chu kỳ (Interval):** Quét gửi 5 phút, 15 phút/lần cho số liệu lưu lượng, thời tiết thông thường.
     - **Theo khung giờ cố định (Cron):** Chốt số liệu theo ca/ngày (ví dụ 6h00 sáng, 22h00 đêm).
     - **Theo sự kiện thời gian thực (Real-time Event):** Khi có sự cố giao thông (cháy xe, tai nạn, chướng ngại vật), luồng sự kiện lập tức bắn gói tin đi ngay, không chờ chu kỳ quét định kỳ.
4. **Chuẩn hóa vỏ bọc gói tin (Socket / Message Wrapper):**
   - Thống nhất cấu trúc wrapper gửi qua Socket / API bên Khang và đối tác ngoài:
     - Gồm các trường: `Type` (loại gói), `Topic` (chủ đề), `Timestamp` (thời điểm), và `Payload` (`H2 data`).
     - Cho phép bên nhận định tuyến đúng handler xử lý trước khi giải mã chi tiết payload.
5. **Đơn giản hóa hàng đợi ưu tiên (Priority Queue - KISS principle):**
   - Nhóm thống nhất không over-engineer hệ thống hàng đợi quá phức tạp.
   - Phân cấp cơ bản: Gói sự cố (High - gửi ngay lập tức) > Gói định kỳ (Normal - gửi theo lịch trình), tránh tạo thêm nút thắt cổ chai (bottleneck) không cần thiết.
6. **Quản lý lịch sử và lỗi (Log & Retry):**
   - Ghi nhận chi tiết lịch sử mỗi lần xuất bản (`PublicationHistory`): thời gian, đối tác, mã gói, mã phản hồi HTTP, thành công/thất bại.
   - Có cơ chế quét lại các bản ghi lỗi để gửi bù (retry).

---

## 2. Điểm cốt lõi & Quyết định kỹ thuật (Key Takeaways)

- **Quy tắc cô lập Entity & DB:**
  - Không tự ý sửa đổi các entity dùng chung (`Esh*`) do WebAPI sở hữu.
  - Sử dụng DTO độc lập và dùng SQL Alias trong câu query để tạo cấu trúc JSON đúng theo đặc tả từng đối tác.
- **Tách biệt phân hệ VideoWall:**
  - Các nội dung bàn về phân quyền màn hình VideoWall theo User/Tổ chức, dựng cây Zone bằng `SqlSugar.ToTree()`, và ma trận layout được tách riêng sang phân hệ **VideoWall**.
- **Đơn giản hóa vận hành:**
  - Trên giao diện cấu hình, ẩn các option rườm rà chưa sử dụng trong giai đoạn 1, cung cấp giá trị mặc định hợp lý để giảm thiểu lỗi người dùng.

---

## 3. Nội dung thảo luận chi tiết (Detailed Transcript)

### Phần A — Luồng dữ liệu, Cấu hình gói tin & Mapping đối tác (Audio 1 — 35:36)

- **[00:00 - 05:00] Rà soát luồng dữ liệu hiện tại:**
  - Thảo luận luồng ghi file của Đạt: Ghi file xuống hệ thống để API đọc log và lấy file lên.
  - Phân tích việc giảm bớt khâu trung gian, chốt giữ ghi file để lưu vết và phục vụ kiểm toán đối soát.
- **[05:00 - 12:00] Cấu trúc gói tin và định nghĩa trường:**
  - Thảo luận bảng cấu hình gói tin: Mã gói tin, tên gói tin, danh sách các field.
  - Các thuộc tính field: Tên trường gửi đi, kiểu dữ liệu, định dạng format, giá trị default.
- **[12:00 - 20:00] Ánh xạ cột DB sang trường gói tin (Field Mapping):**
  - Tranh luận phương án hard-code class DTO vs câu query SQL với Alias.
  - Chốt phương án câu query SQL linh hoạt: Tên cột DB được alias trực tiếp thành tên trường của đối tác.
- **[20:00 - 28:00] Xử lý tham số vị trí, Zone và trạng thái:**
  - Chuẩn hóa các trường `ZoneID`, `StationID`, `Location`, cự ly `Kilometer`.
  - Quy ước rõ ràng đơn vị đo mét và kilomet để tránh nhầm lẫn giữa các hệ thống tích hợp.
- **[28:00 - 35:36] Kiểm soát lịch sử gửi (Log) và kế hoạch kiểm thử:**
  - Cơ chế ghi log `SendLog` / `PublicationHistory`.
  - Xử lý ngoại lệ khi gửi thất bại và quét gửi bù.

### Phần B — Chu kỳ gửi, Sự kiện khẩn cấp, Socket Wrapper & Hàng đợi (Audio 2 — 25:41)

- **[00:00 - 07:00] Chu kỳ và lịch trình gửi:**
  - Phân loại: Gửi liên tục (stream), theo khoảng thời gian (interval 5-15 phút), hay theo khung giờ cố định (cron).
- **[07:00 - 14:00] Xử lý sự kiện khẩn cấp (Incident / Event-driven):**
  - Cơ chế bắt sự kiện tai nạn, cháy nổ, kích hoạt bắn tin khẩn cấp ngay tức thì.
  - Thảo luận Priority Queue: Thống nhất giữ đơn giản, gói tin sự cố ưu tiên bắn trước.
- **[14:00 - 20:00] Tinh gọn cấu hình giao diện:**
  - Ẩn bớt các option nâng cao chưa dùng đến, giữ lại cấu hình cần thiết: đối tác, chu kỳ, danh sách gói tin.
- **[20:00 - 25:41] Chuẩn hóa Socket Message Wrapper:**
  - Thống nhất vỏ bọc gói tin `H2 data` cho các dịch vụ kết nối socket/API ngoài.
  - Định hướng tích hợp và chuẩn bị cho các đợt đối soát dữ liệu tiếp theo.
