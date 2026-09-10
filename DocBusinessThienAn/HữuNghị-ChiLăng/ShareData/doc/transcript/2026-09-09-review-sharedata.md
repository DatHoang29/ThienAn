---
tier: A
read: full
source: ../../_source/audio/2026-09-09-review-1.m4a, ../../_source/audio/2026-09-09-review-2.m4a
date: 2026-09-09
model: Gemini Native Audio Multimodal + User Meeting Notes
status: verified
---

# Review ShareData — Rà soát & Sửa đổi toàn diện luồng DataPublicationService

**Ngôn ngữ:** Tiếng Việt  
**Tổng thời lượng:** ~61 phút (Phần 1: 35:36, Phần 2: 25:41)  
**File nguồn:** `_source/audio/2026-09-09-review-1.m4a`, `_source/audio/2026-09-09-review-2.m4a`  
**Chủ đề:** Đối chiếu hiện trạng CSDL/Code thực tế (`ShareDataSubscription`, `ShareDataScheduleDto`, `ShareDataActivityLog`) và chốt **6 điểm sửa đổi cốt lõi** cho luồng ShareData sau buổi họp ngày 09/09/2026.

---

## 1. Bảng đối chiếu 6 điểm sửa đổi: Hiện trạng File Gốc vs Quyết định chốt

| STT | Điểm yêu cầu (Notepad++) | Hiện trạng File Gốc (Code & CSDL cũ) | Vấn đề phát sinh | Quyết định chốt sửa lại sau cuộc họp |
|:---:|---|---|---|---|
| **1 & 3** | **Bổ sung xử lý theo giờ, theo time (Cụ thể: 9h sáng hàng ngày)** | `ShareDataScheduleDto` chỉ có `continuous` (chạy theo `IntervalSeconds`) và `daily` (khung `StartTime` + `DurationMinutes`). Bảng `ShareDataSubscription` chỉ có cột `IntervalSeconds`. | **Chưa có case chạy đúng 1 mốc giờ cố định trong ngày.** Khi người dùng muốn chốt số liệu đúng 9h00 sáng hàng ngày để gửi cho đối tác thì hệ thống không hỗ trợ cấu hình mốc giờ điểm này. | **Bổ sung case chạy theo mốc giờ cụ thể hàng ngày (Daily Fixed Time):** Cấu hình mốc giờ cố định (ví dụ: `09:00:00 AM`), đúng giờ này background job tự động quét dữ liệu của 24h trước và xuất bản. |
| **2** | **Log xử lý xóa theo đầu ngày** | Entity `ShareDataActivityLog`: Khai báo rõ *"Bảng CHỈ GHI THÊM — không sửa, không xóa qua API"*. Mỗi lần gửi/nhận/xuất file đều ghi 1 dòng log `TRANSFER`. | Khi hệ thống chạy dài ngày, số lượng bản ghi log phình to cực nhanh, làm chậm các câu truy vấn tra cứu và tốn dung lượng CSDL. | **Bổ sung background job dọn dẹp log đầu ngày:** Vào đầu mỗi ngày (00:00), hệ thống tự động quét và xóa sạch các log gửi thành công cũ (`Status = SUCCESS`), chỉ lưu lại log lỗi (`FAILED`) hoặc log gần nhất để đối soát. |
| **4** | **Bỏ cấu hình sự kiện, một lần; Thêm switch/checkbox tự động gửi khi có dữ liệu mới** | `ShareDataSubscription.Mode` hỗ trợ 3 giá trị: `SINGLE` (gửi 1 lần), `EVENT` (gửi theo sự kiện), `PERIODIC` (gửi định kỳ). Kèm theo 2 cột phức tạp: `EventSourceId` và `DebounceSec`. | Khái niệm gửi theo sự kiện (`EVENT`) và gửi 1 lần (`SINGLE`) quá rườm rà, dễ gây lỗi cấu hình và thực tế nghiệp vụ chia sẻ dữ liệu giao thông không ai dùng đến. | **BỎ HẲN** chế độ `EVENT` và `SINGLE`. Thay thế bằng 1 control toggle/switch trực quan: **`[x] Tự động gửi khi có dữ liệu mới`** (khi có bản ghi mới phát sinh thì service tự động bắt và đẩy đi ngay). |
| **5** | **Bỏ bớt field ưu tiên (Priority)** | Bảng `ShareDataSubscription` có cột `Priority` (kiểu int, 0..10), dự định xây dựng hàng đợi ưu tiên (Priority Queue). | Việc phân cấp ưu tiên từ 0 đến 10 là over-engineering, gây phức tạp hóa luồng xử lý và tạo nút thắt cổ chai (bottleneck) không cần thiết. | **BỎ HOÀN TOÀN** field `Priority` trên giao diện và backend. Mọi gói tin đến lịch hoặc có dữ liệu mới sẽ được đẩy đi theo hàng đợi bất đồng bộ tiêu chuẩn. |
| **6** | **Ẩn bớt những field không cần** | DTO `ShareDataAddSubscriptionInput` kế thừa trực tiếp entity `ShareDataSubscription`, phơi bày hàng loạt trường kỹ thuật: `DebounceSec`, `EventSourceId`, `SerialNbr`, `Guaranteed`, `Persistent`, `RejectReason`, `CancelReason`... | Người dùng vận hành bị rối mắt trước quá nhiều trường thông tin nội bộ không phục vụ cấu hình nghiệp vụ thông thường. | **Ẩn toàn bộ các field thừa/nội bộ.** Giao diện cấu hình chỉ giữ lại các trường thiết yếu: (1) Đối tác, (2) Gói dữ liệu/Mapping, (3) Lựa chọn gửi: Switch tự động khi có data mới HOẶC Chọn mốc giờ (9h sáng) / Chu kỳ, (4) Trạng thái Bật/Tắt. |

---

## 2. Chi tiết giải pháp kỹ thuật theo từng điểm

### 2.1. Lập lịch theo giờ cố định hàng ngày (Mục 1 & 3)
- Cập nhật `ShareDataScheduleDto`: Bổ sung thuộc tính `DailyAtTime` (định dạng `HH:mm:ss`, ví dụ `"09:00:00"`).
- Cơ chế thực thi: Worker kiểm tra mốc giờ hàng ngày; khi đồng hồ hệ thống chạm mốc `DailyAtTime`, service kích hoạt câu query trích xuất dữ liệu của chu kỳ 24h trước đó để đóng gói gửi đi.

### 2.2. Cơ chế xóa log đầu ngày (Mục 2)
- Xây dựng một cron job chạy vào `00:05` hàng ngày:
  ```sql
  -- Dọn dẹp log truyền nhận thành công của các ngày trước đó
  DELETE FROM ShareDataActivityLog 
  WHERE LogType = 'TRANSFER' 
    AND Status = 'SUCCESS' 
    AND OccurredAt < CAST(GETDATE() AS DATE);
  ```
- Việc dọn dẹp này giúp bảng `ShareDataActivityLog` luôn giữ kích thước tối ưu, truy vấn phân trang trên UI phản hồi tức thì.

### 2.3. Tinh giản chế độ gửi & Thêm switch dữ liệu mới (Mục 4)
- **Loại bỏ:** Enum / Radio button cho `EVENT` và `SINGLE`.
- **Cấu trúc mới trên UI:**
  - **Lựa chọn 1 (Switch):** `[x] Tự động gửi khi có dữ liệu mới phát sinh` (Auto-push on new record).
  - **Lựa chọn 2 (Lập lịch):**
    - `(•) Theo giờ cố định hàng ngày:` Nhập mốc giờ (ví dụ `09:00 AM`).
    - `(•) Theo chu kỳ lặp lại:` Nhập số phút (ví dụ mỗi `15 phút`).

### 2.4. Bỏ field Priority & Tinh gọn DTO (Mục 5 & 6)
- Bỏ trường `Priority` khỏi form tạo/sửa đăng ký.
- Cấu hình chỉ hiển thị các trường nghiệp vụ thực tế, các trường hệ thống (`SerialNbr`, `State`, `RequestedAt`) để backend tự động quản lý ngầm.

---

## 3. Các quyết định kỹ thuật khác đã chốt trong cuộc họp

1. **Phương thức xuất bản dữ liệu:**
   - Kết hợp song song: Vẫn ghi file ra ổ cứng/UNC path để làm bằng chứng đối soát lịch sử; đồng thời gọi HTTP POST trực tiếp tới endpoint đối tác.
2. **Cơ chế ánh xạ trường (Mapping via SQL Alias):**
   - Không sửa đổi schema CSDL nội bộ. Sử dụng câu truy vấn SQL với **SQL Alias** (hoặc bảng cấu hình mapping key-value) để biến đổi tên cột nội bộ thành tên trường chuẩn của từng đối tác (`DB Column` ↔ `Partner Field`).
3. **Chuẩn hóa Socket Message Wrapper (`H2 data`):**
   - Đối với các luồng truyền dữ liệu qua Socket / API bên Khang: Đóng gói tin theo cấu trúc vỏ bọc chuẩn: `Type`, `Topic`, `Timestamp`, và `Payload` (`H2 data`).

---

## 4. Biên bản diễn biến cuộc họp theo thời gian

### Phần A — Luồng dữ liệu, Cấu hình gói tin & Mapping đối tác (Audio 1 — 35:36)
- **[00:00 - 05:00] Rà soát luồng dữ liệu:** Bàn về việc ghi file của Đạt; thống nhất giữ lại ghi file để lưu vết đối soát, không bỏ hoàn toàn.
- **[05:00 - 12:00] Cấu trúc gói tin:** Rà soát các field trong gói tin; yêu cầu tách bạch rõ tên trường nội bộ và tên trường đối tác.
- **[12:00 - 20:00] Ánh xạ dữ liệu (Mapping):** Chốt dùng câu query SQL với Alias để map linh hoạt cho từng đối tác mà không cần sửa code C#.
- **[20:00 - 28:00] Chuẩn hóa trường:** Quy ước rõ đơn vị đo (km, mét), mã trạng thái thiết bị và `ZoneID`.
- **[28:00 - 35:36] Lịch sử và kiểm thử:** Cơ chế ghi log và bàn giao dữ liệu test trên môi trường staging.

### Phần B — Rà soát luồng kích hoạt, Tinh gọn UI & Socket Wrapper (Audio 2 — 25:41)
- **[00:00 - 07:00] Bổ sung chạy theo giờ:** Nhận diện hệ thống chưa có case cấu hình theo mốc giờ cụ thể hàng ngày (ví dụ 9h sáng hàng ngày) -> Yêu cầu bổ sung ngay.
- **[07:00 - 14:00] Bỏ chế độ sự kiện & Bỏ field ưu tiên:** Thống nhất bỏ cấu hình sự kiện phức tạp và gửi một lần; bỏ field `Priority` để tránh over-engineer hàng đợi; thêm switch tự động gửi khi có data mới.
- **[14:00 - 20:00] Ẩn các field thừa:** Tinh gọn form cấu hình giao diện, ẩn các trường nội bộ không cần thiết.
- **[20:00 - 25:41] Socket Message Wrapper:** Thống nhất vỏ bọc gói tin `H2 data` cho các kết nối socket bên ngoài.
