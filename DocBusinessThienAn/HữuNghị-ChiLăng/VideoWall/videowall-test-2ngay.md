# Checklist test 2 ngày tại TCB (Hiện trường)

> Bản nội bộ — mang theo khi đi hiện trường tại trạm điều hành TCB (Hữu Nghị – Chi Lăng).
> Thiết bị: **Hikvision DS-C30S-S11** (1 controller, 12 màn ghép 4×3). **Không đụng phần cứng.**

---

## 🎯 Bối Cảnh & Kiến Trúc 2 Tầng (WPF ↔ Thiết Bị)

- **Phạm vi trực tiếp (Direct Mode Only)**: Ứng dụng `Module.VideoWall.WPF` kết nối thẳng tới IP bộ điều khiển qua HTTP/ISAPI + Digest Auth. Không qua Backend trung gian, không chạm vào dây cáp phần cứng hay genlock của khách hàng.
- **Tự động ghi log ra file (Auto Session Logging)**: Mọi thao tác gửi request và dữ liệu phản hồi XML/JSON từ thiết bị đều tự động ghi nối tiếp vào file `%LOCALAPPDATA%\Module.VideoWall.WPF\Logs\session_{yyyyMMdd_HHmmss}.jsonl` để làm căn cứ đối chiếu và phân tích sau này.
- **Quy tắc 1 giờ**: Nếu gặp một case khó quá **~1 giờ** không giải quyết được → **Skip**, ghi chú lại "chưa xong + dữ liệu gửi/nhận", chuyển sang làm việc khác. Về nhà mở file log JSONL để phân tích chi tiết.

---

## 💡 Phân Định Rõ Ràng Giữa Tab 1 và Tab 2

| Tab | Tên chức năng | Mục đích & Nghiệp vụ |
|---|---|---|
| **Tab 1** | **Thiết lập Scene** | **Bố cục hiển thị tĩnh (Scene)**: Dựng các ô camera trên tường 12 màn hình (Lưới $4 \times 3$, Xếp lớp Picture-in-Picture) và đẩy xuống phần cứng (`🚀 ĐẨY XUỐNG THIẾT BỊ`). |
| **Tab 2** | **Kịch bản (Scenario)** | **Chuỗi API kiểm thử tự động (Automation Sequence)**: Chạy hàng loạt lệnh API liên tiếp có độ trễ (`DelayBetweenStepsMs = 400ms`), kiểm thử tranh vùng và bộ kiểm thử 3 trường hợp lỗi biên (có công tắc gửi thật). |

---

## NGÀY 1 — luồng chính + case khó

### 1. Kết nối & Khảo sát thiết bị (1-Click)
- [ ] Cắm mạng/PC vào controller; nhập IP / Port `80` / Account `admin` / Password tại thanh trên cùng.
- [ ] Bấm **🔍 Kết nối & Khảo sát** (Nút chính duy nhất) → Kỳ vọng:
  - Kiểm tra kết nối mạng + Digest Auth thành công.
  - Tự động đọc thông số phần cứng: `capabilities` (maxWindow = 16, maxScene = 128), danh sách tường (`WallNo = 1`), 12 cổng ra màn hình (Outputs) và danh sách camera (Inputs).
  - Tự động mở khóa (Unlock) toàn bộ các Tab làm việc bên dưới và nạp dữ liệu sang Tab 1. Chụp ảnh màn hình.
- [ ] Ghi lại: IP, port, serial port, vị trí cuộn dây (hỏi người quản lý khu vực).

### 2. Backup — BẮT BUỘC, làm trước khi thay đổi gì
- [ ] **Tải file cấu hình từ web quản lý** của controller (Maintenance/Security → Backup) — đây là bản backup CHÍNH, đầy đủ nhất (cơ chế gốc của hãng). Sau khi test xong, phục hồi lại bằng đúng file này qua web quản lý.
- [ ] Clone kịch bản đang chạy ra bản riêng để test (không đụng bản gốc).

### 3. Bố cục scene (Bố cục các ô camera trên tường) — chạy DryRun trước, rồi đẩy thật
> 💡 *Ghi nhớ nhanh*: 
> - **Scene**: Là "bức tranh toàn cảnh" lưu lại vị trí các ô camera trên tường tại 1 thời điểm.
> - **DryRun (Chạy thử)**: Diễn tập an toàn, kiểm tra tính toán toạ độ và log mà chưa gửi lệnh đổi màn hình thật.
> - **Đẩy thật**: Gửi lệnh ISAPI xuống thiết bị để xoá cũ, tạo mới các ô camera và kích hoạt Scene lên tường thật.

- [ ] **Scene KHÔNG chồng** (Tab 1, Chế độ A): Mỗi màn hình chiếu 1 camera độc lập phủ kín 100%, lưới 4×3 (xác nhận cách xếp khi tới nơi). Bật DryRun kiểm tra trước → Tắt DryRun bấm Đẩy thật → nhìn tường màn hình đổi đúng → chụp ảnh.
- [ ] **Scene CHỒNG cửa sổ** (Tab 1, Chế độ B): 2 nguồn camera xếp lớp — 1 ô to làm nền (ZIndex 1), 1 ô nhỏ đè lên góc (ZIndex 2, Picture-in-Picture). Đẩy thật → nhìn tường → chụp ảnh.
- [ ] **Chia nhỏ 1 cửa sổ**: 1 nguồn chia 4 / 6 / 9 ô camera.
- [ ] **Canh size khi 2 trigger** (Tab 2): Dựng tình huống 2 vùng tranh nhau → canh 1 nhỏ 1 lớn.
- [ ] **Activate scene**: Tạo 2–3 scene khác nhau, bấm nút kích hoạt chuyển qua lại giữa các scene, xem tường đổi tức thì trong 1 giây. Chụp ảnh mỗi lần.

### 4. Case lỗi — làm sớm (dễ bị vướng)
- [ ] **Sai số màn**: khai 21 màn khi thiết bị chỉ 12 → thiết bị **phải báo lỗi**. Chụp mã lỗi, ghi vào file.
- [ ] **SID ngoài dải**: SID 999 (> 128) → phải bị chặn. Nếu muốn thấy mã lỗi thật của thiết bị thì tick bật checkbox **"Gửi thật để xem mã lỗi thiết bị"** ở Tab 2 trước khi chạy Bộ kiểm thử lỗi.
- [ ] **Mất kết nối giữa chừng**: rút dây / tắt nguồn mạng khi đang chạy kịch bản → xem công cụ báo gì, cắm lại dây và bấm **"▶ Chạy tiếp từ bước #N"** để kiểm tra tính năng Resume.

### 5. Cuối ngày
- [ ] Đóng gói: file log JSONL (`session_*.jsonl`, tự động sẵn) + toàn bộ ảnh chụp (đặt tên theo ngày). Có thể bấm nút **"Xuất log"** trên đầu bảng Logs để lưu thêm 1 bản `.json` gộp dễ đọc.
- [ ] Nếu đã đổi cấu hình → khôi phục về trạng thái ban đầu (từ file backup web quản lý).

---

## NGÀY 2 — rà soát + bàn giao

- [ ] Xử lý lại các case Ngày 1 bị lỗi / bị skip (dùng dữ liệu đã ghi + log).
- [ ] Kiểm tra thêm các API/luồng chưa chạm (nếu còn giờ).
- [ ] Test cáp / thiết bị mang theo (nếu có kế hoạch).
- [ ] **Khôi phục cấu hình gốc** (từ file backup Ngày 1 qua web quản lý).
- [ ] **Bàn giao**: xác nhận tường hiển thị đúng như trước khi đội tới. Có người khách xác nhận.
- [ ] Đóng gói bằng chứng Ngày 2, gộp với Ngày 1.

---

## THẾ NÀO LÀ ĐỦ (định nghĩa "xong" cho 2 ngày)

Không cần test hết mọi thứ — đủ khi:

- [ ] 1. Kết nối + Probe + Backup + Restore **chạy được trên thiết bị thật**.
- [ ] 2. Mỗi loại bố cục **ít nhất 1 lần đẩy thật, thấy tường đổi đúng, có ảnh**: scene không chồng / scene chồng / activate.
- [ ] 3. **Ít nhất 2 case lỗi** (sai số màn + SID sai) có **mã lỗi thật của thiết bị** ghi ra file.
- [ ] 4. Có **file log JSONL tự động lưu toàn bộ dữ liệu gửi/nhận** + ảnh chụp cho mọi bước.
- [ ] 5. Cấu hình khách **trả về nguyên trạng**, có xác nhận của khách.
- [ ] 6. Case nào > 1 giờ không ra → đã ghi "chưa xong + dữ liệu kèm theo" (không cố đấm).
