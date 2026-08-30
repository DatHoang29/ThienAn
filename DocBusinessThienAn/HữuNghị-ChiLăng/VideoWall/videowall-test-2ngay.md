# Checklist test 2 ngày tại TCB (nội bộ)

> Bản nội bộ — mang theo khi đi hiện trường. Bối cảnh & cơ chế: [`videowall-record-replay.md`](videowall-record-replay.md).
> Thiết bị: **Hikvision DS-C30S-S11** (1 controller, 12 màn). **Không đụng phần cứng.**

## Nguyên tắc

- Tương tác qua IP (ISAPI/HTTP). Không mở/tháo/đổi phần cứng, không đổi dây genlock.
- Log **tự động ghi liên tục ngay khi mở app, không cần bật gì** — mọi request/response tự vào file `%LOCALAPPDATA%\Module.VideoWall.WPF\Logs\session_*.jsonl`. ⚠️ Không có nút "chế độ Ghi" để bật/tắt — nó luôn chạy sẵn.
- Chụp màn hình từng bước: màn tường · web quản lý controller · màn công cụ · (khi lỗi) đèn báo controller.
- 1 case khó quá **~1 giờ** không xong → **skip**, ghi lại "chưa xong + dữ liệu kèm theo", làm việc khác. ⚠️ Về nhà xử tiếp bằng cách **đọc lại file log JSONL đã lưu** — KHÔNG có công cụ "Phát lại (Replay)" tự chạy lại request cũ, phải tự tay làm lại theo đúng thông số đã ghi trong log.
- Bình thường → chụp là đủ. Lỗi → lấy log chi tiết + mã lỗi.

---

## NGÀY 1 — luồng chính + case khó

### 1. Kết nối & khảo sát
- [ ] Cắm mạng/PC vào controller; nhập IP / Port `80` / Account `admin` / Password.
- [ ] Bấm **Ping** → "kết nối thành công". Chụp.
- [ ] Bấm **Probe** → đọc được: capabilities (maxWindow / maxScene), danh sách wall, outputs (12 màn), input channels. Chụp.
- [ ] Ghi lại: IP, port, serial port, vị trí cuộn dây (hỏi người quản lý khu vực).

### 2. Backup — BẮT BUỘC, làm trước khi thay đổi gì
- [ ] **Tải file cấu hình từ web quản lý** của controller (Maintenance/Security → Backup) — đây là bản backup CHÍNH, đầy đủ nhất (cơ chế gốc của hãng). Sau khi test xong, phục hồi lại bằng đúng file này qua web quản lý.
- [ ] Clone kịch bản đang chạy ra bản riêng để test (không đụng bản gốc).

### 3. Bố cục scene — chạy DryRun trước, rồi đẩy thật
- [ ] **Scene KHÔNG chồng**: mỗi màn 1 cửa sổ phủ kín, lưới 4×3 (xác nhận cách xếp khi tới nơi). Đẩy thật → nhìn tường → chụp.
- [ ] **Scene CHỒNG cửa sổ**: 2 nguồn xếp lớp — 1 to làm nền (ZIndex 1), 1 nhỏ đè lên (ZIndex 2). Đẩy → nhìn → chụp.
- [ ] **Chia nhỏ 1 cửa sổ**: 1 nguồn chia 4 / 6 / 9 ô.
- [ ] **Canh size khi 2 trigger**: dựng tình huống 2 vùng tranh nhau → canh 1 nhỏ 1 lớn.
- [ ] **Activate scene**: tạo 2–3 scene, chuyển qua lại, xem tường đổi đúng. Chụp mỗi lần.

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
- [ ] 4. Có **file log JSONL tự động** (không phải công cụ riêng — luôn ghi sẵn) + ảnh cho mọi bước.
- [ ] 5. Cấu hình khách **trả về nguyên trạng**, có xác nhận của khách.
- [ ] 6. Case nào > 1 giờ không ra → đã ghi "chưa xong + dữ liệu kèm theo" (không cố đấm).
