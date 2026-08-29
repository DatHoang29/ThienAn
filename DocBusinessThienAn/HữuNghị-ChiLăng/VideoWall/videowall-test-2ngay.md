# Checklist test 2 ngày tại TCB (nội bộ)

> Bản nội bộ — mang theo khi đi hiện trường. Bối cảnh & cơ chế: [`videowall-record-replay.md`](videowall-record-replay.md).
> Thiết bị: **Hikvision DS-C30S-S11** (1 controller, 12 màn). **Không đụng phần cứng.**

## Nguyên tắc

- Tương tác qua IP (ISAPI/HTTP). Không mở/tháo/đổi phần cứng, không đổi dây genlock.
- Bật **chế độ Ghi (Record)** ngay từ đầu → mọi request/response tự vào file tape.
- Chụp màn hình từng bước: màn tường · web quản lý controller · màn công cụ · (khi lỗi) đèn báo controller.
- 1 case khó quá **~1 giờ** không xong → **skip**, ghi lại "chưa xong + dữ liệu kèm theo", làm việc khác. Về nhà **Phát lại (Replay)** xử tiếp.
- Bình thường → chụp là đủ. Lỗi → lấy log chi tiết + mã lỗi.

---

## NGÀY 1 — luồng chính + case khó

### 1. Kết nối & khảo sát
- [ ] Cắm mạng/PC vào controller; nhập IP / Port `80` / Account `admin` / Password.
- [ ] Bấm **Ping** → "kết nối thành công". Chụp.
- [ ] Bấm **Probe** → đọc được: capabilities (maxWindow / maxScene), danh sách wall, outputs (12 màn), input channels. Chụp.
- [ ] Ghi lại: IP, port, serial port, vị trí cuộn dây (hỏi người quản lý khu vực).

### 2. Backup — BẮT BUỘC, làm trước khi thay đổi gì
- [ ] Tải file cấu hình từ **web quản lý** của controller (Maintenance/Security → Backup).
- [ ] Bấm **"Chụp lại mặc định"** trong công cụ (probe cấu hình hiện tại).
- [ ] Clone kịch bản đang chạy ra bản riêng để test (không đụng bản gốc).

### 3. Bố cục scene — chạy DryRun trước, rồi đẩy thật
- [ ] **Scene KHÔNG chồng**: mỗi màn 1 cửa sổ phủ kín, lưới 4×3 (xác nhận cách xếp khi tới nơi). Đẩy thật → nhìn tường → chụp.
- [ ] **Scene CHỒNG cửa sổ**: 2 nguồn xếp lớp — 1 to làm nền (ZIndex 1), 1 nhỏ đè lên (ZIndex 2). Đẩy → nhìn → chụp.
- [ ] **Chia nhỏ 1 cửa sổ**: 1 nguồn chia 4 / 6 / 9 ô.
- [ ] **Canh size khi 2 trigger**: dựng tình huống 2 vùng tranh nhau → canh 1 nhỏ 1 lớn.
- [ ] **Activate scene**: tạo 2–3 scene, chuyển qua lại, xem tường đổi đúng. Chụp mỗi lần.

### 4. Case lỗi — làm sớm (dễ bị vướng)
- [ ] **Sai số màn**: khai 21 màn khi thiết bị chỉ 12 → thiết bị **phải báo lỗi**. Chụp mã lỗi, ghi vào file.
- [ ] **SID ngoài dải**: SID 999 (> 128) → phải bị chặn (client hoặc thiết bị).
- [ ] **Mất kết nối giữa chừng**: rút dây / tắt nguồn mạng khi đang chạy kịch bản → xem công cụ báo gì, thử **"Chạy tiếp từ bước lỗi"**.

### 5. Cuối ngày
- [ ] Đóng gói: file tape + log JSON + toàn bộ ảnh chụp (đặt tên theo ngày).
- [ ] Nếu đã đổi cấu hình → khôi phục về trạng thái ban đầu.

---

## NGÀY 2 — rà soát + bàn giao

- [ ] Xử lý lại các case Ngày 1 bị lỗi / bị skip (dùng dữ liệu đã ghi + log).
- [ ] Kiểm tra thêm các API/luồng chưa chạm (nếu còn giờ).
- [ ] Test cáp / thiết bị mang theo (nếu có kế hoạch).
- [ ] **Khôi phục cấu hình gốc** (từ file backup Ngày 1) + bấm "Khôi phục mặc định".
- [ ] **Bàn giao**: xác nhận tường hiển thị đúng như trước khi đội tới. Có người khách xác nhận.
- [ ] Đóng gói bằng chứng Ngày 2, gộp với Ngày 1.

---

## THẾ NÀO LÀ ĐỦ (định nghĩa "xong" cho 2 ngày)

Không cần test hết mọi thứ — đủ khi:

- [ ] 1. Kết nối + Probe + Backup + Restore **chạy được trên thiết bị thật**.
- [ ] 2. Mỗi loại bố cục **ít nhất 1 lần đẩy thật, thấy tường đổi đúng, có ảnh**: scene không chồng / scene chồng / activate.
- [ ] 3. **Ít nhất 2 case lỗi** (sai số màn + SID sai) có **mã lỗi thật của thiết bị** ghi ra file.
- [ ] 4. Có **1 file tape (Ghi)** + log + ảnh cho mọi bước.
- [ ] 5. Cấu hình khách **trả về nguyên trạng**, có xác nhận của khách.
- [ ] 6. Case nào > 1 giờ không ra → đã ghi "chưa xong + dữ liệu kèm theo" (không cố đấm).
