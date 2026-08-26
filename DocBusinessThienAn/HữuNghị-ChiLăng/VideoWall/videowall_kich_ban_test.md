# Kịch bản test toàn trình — Công cụ WPF kiểm thử ISAPI VideoWall

> Viết theo đúng UI thật đang có trên đĩa (`Module.VideoWall.WPF/Views/MainWindow.xaml`, xác nhận lúc viết tài liệu này). Công cụ hoạt động thuần **kết nối trực tiếp thiết bị phần cứng thật (Direct Mode)** và kiểm thử toàn diện **31 API "Dùng chính"** theo danh mục chuẩn `ISAPI-Videowall-Controller/00B-api-list-full.md`. Giao diện được thiết kế dạng form nhập tham số theo metadata, tự động sinh Request Body XML chuẩn Hikvision ISAPI (không phải gõ XML tay). Vào thẳng `MainWindow` mà không cần đăng nhập.

---

## Chuẩn bị (2 tiến trình, đúng thứ tự)

```
1. MockServer:  cd scripts/VwMockServerRunner && dotnet run
                → nghe 127.0.0.1:18080-18083, account admin / Password123!
                (Dùng khi giả lập hoặc test các lệnh nguy hiểm; khi cắm thiết bị thật thì bỏ qua MockServer)
2. WPF Tool:    cd src/Modules/VideoWall/Module.VideoWall.WPF && dotnet run
```

Mở app → vào thẳng `MainWindow` với giao diện Direct Mode chuyên dụng:
- **Thanh tiêu đề & Kết nối (trên cùng)**: IP, Port, Tài khoản, Mật khẩu, Tường số, nút "Kết nối (Ping)" và "Khảo sát (Probe)".
- **Khung kết quả khảo sát (Probe)**: Expander thu gọn/mở rộng chứa 3 bảng: Tường, Cổng ra / Kênh vào, Lệch CSDL.
- **Khung kiểm thử 7 nhóm API ISAPI (trung tâm)**: Gồm 7 TabItem tương ứng 7 nhóm nghiệp vụ, chia đôi:
  - Cột trái: Danh sách API trong nhóm (Method badge màu sắc: GET xanh dương, POST xanh lá, PUT cam, DELETE đỏ).
  - Cột giữa: Form nhập tham số động (Path params, Body fields, Dropdown enum, Expander nâng cao).
  - Cột phải: Khung "Phản hồi từ thiết bị (Response)" cố định hiển thị XML phản hồi.
- **Khung Logs (dưới cùng)**: Bảng nhật ký hoạt động cố định có GridSplitter kéo dãn.

---

## Bước 1 — Kết nối & Khảo sát thiết bị (Direct Mode)

1. Nhập thông số thiết bị trên thanh kết nối:
   - IP: `127.0.0.1` (hoặc IP thật ngoài hiện trường, ví dụ `10.10.8.30`)
   - Port: `18080` (hoặc `80` cho thiết bị thật)
   - Tài khoản: `admin`
   - Mật khẩu: `Password123!`
   - Tường số: `1` (hoặc để trống để client tự dò)
2. Bấm **"Kết nối (Ping)"** → kiểm tra khả năng kết nối trực tiếp, thanh trạng thái hiển thị "Kết nối thành công tới thiết bị ...".
3. Bấm **"Khảo sát (Probe)"** → tự động đọc cấu hình thiết bị (`9.7.4.8`, `9.7.5.2`, `9.7.5.5`, `9.7.5.6`).
4. Mở Expander **"Kết quả khảo sát (Probe)"** → kiểm tra dữ liệu 3 bảng:
   - Bảng **Tường**: ID tường, Tên tường.
   - Bảng **Cổng ra / Kênh vào**: OutputId, số cửa sổ, kiểu toạ độ, kênh vào, loại cổng (HDMI/DVI).
   - Bảng **Lệch CSDL ↔ thiết bị**: danh sách điểm lệch nếu có.

---

## Bước 2 — Kiểm thử 7 nhóm API ISAPI theo Tab

### Tab 1 — `1. Board` (9.7.1)
- **9.7.1.4 (GET)**: *Get capability of the status of all sub-boards*
  - Chọn API → Form thông báo không cần tham số.
  - Bấm **"Gửi ISAPI"** → Khung Response trả về XML chứa năng lực sub-board (`<BoardStatusCap>`).

### Tab 2 — `2. Giải mã (Decoding)` (9.7.2)
- **9.7.2.1 (GET)**: *Get decoding device status* → Bấm "Gửi ISAPI" → Nhận trạng thái giải mã.
- **9.7.2.5 (PUT)**: *Start dynamic decoding*
  - Nhập Path params: `videoWallID` (1), `VWMWID` (1), `VWSWID` (1).
  - Bấm **"Gửi ISAPI"** → Thiết bị bắt đầu giải mã luồng.
- **9.7.2.6 (GET)**: *Get decoding status of all sub windows of a specific window* → Nhập `videoWallID`, `VWMWID`, `VWSWID` → Bấm gửi.
- **9.7.2.7 (PUT)**: *Stop dynamic decoding* → Nhập `videoWallID`, `VWMWID`, `VWSWID` → Bấm gửi → Dừng giải mã.
- **9.7.2.8 (GET)**: *Get decoding status of all sub-windows of all windows* → Nhập `videoWallID` → Bấm gửi.

### Tab 3 — `3. Cổng ra (Output)` (9.7.3)
- **9.7.3.4 (GET)**: *Get basic parameters of all video outputs* → Bấm gửi → Danh sách tất cả cổng ra.
- **9.7.3.5 (GET)**: *Get parameters of a specific video output* → Nhập `channelID` (1) → Bấm gửi.
- **9.7.3.6 (PUT)**: *Set parameters of a specific video output*
  - Nhập `channelID` (1).
  - Form Body: `id` = 1, `portType` = chọn từ dropdown (`HDMI` / `DVI` / `VGA` / `BNC` / `YPbPr` / `SDI`).
  - Bấm **"Gửi ISAPI"** → Form tự động dựng XML `<VideoOutputChannel version="2.0">` và gửi xuống.
- **9.7.3.7 (GET)**: *Get the capability of a specific video output* → Nhập `channelID` (1) → Bấm gửi.

### Tab 4 — `4. Nguồn tín hiệu (Input)` (9.7.4)
- **9.7.4.8 (GET)**: *Get parameters of all video input channels* → Bấm gửi → Danh sách tất cả kênh vào.
- **9.7.4.10 (PUT)**: *Set parameters of a specified signal source*
  - Nhập `channelID` (1), `id` = 1, `name` = `Dashboard GT`, `RelateScreenServer` = `true`.
  - Mở Expander "Tham số nâng cao": `resolution` (1920*1080), `frameRate` (60), `inputPortType` (HDMI).
  - Bấm **"Gửi ISAPI"** → Tự động dựng XML `<VideoInputChannel>` với nhóm lồng `<OutputResolution>` chuẩn xác.
- **9.7.4.15 (GET)**: *Get picture cropping parameters of a specific signal source* → Nhập `channelID` → Bấm gửi.
- **9.7.4.16 (PUT)**: *Set picture cropping parameters of a specified signal source*
  - Nhập `channelID`, `leftCutOff` (0), `rightCutOff` (0), `topCutOff` (0), `bottomCutOff` (0).
  - Bấm **"Gửi ISAPI"** → Gửi XML `<InputCutOff>`.
- **9.7.4.18 (GET)**: *Get captured pictures* → Nhập `channelID` → Bấm gửi.

### Tab 5 — `5. Tường ghép (Video Wall)` (9.7.5)
- **9.7.5.1 (GET)**: *Get the capability of video wall controller* → Bấm gửi.
- **9.7.5.2 (GET)**: *Get parameters of all video walls* → Bấm gửi.
  - ⚠️ **QUAN TRỌNG:** Copy 2 đoạn XML `<WallOutputList>...</WallOutputList>` và `<WallWindowList>...</WallWindowList>` từ kết quả Response để phục vụ cho lệnh PUT 9.7.5.3.
- **9.7.5.3 (PUT)**: *Set parameters of a specific video wall (Form Hybrid An Toàn)*
  - Nhập `videoWallID` (1), `id` (1), `name` (VideoWall 1), `backgroudColor` (black), `autoSwitchMainSub` (false).
  - Dán đoạn XML `WallOutputList` đã copy từ GET 9.7.5.2 vào ô trái.
  - Dán đoạn XML `WallWindowList` đã copy từ GET 9.7.5.2 vào ô phải.
  - Tick chọn CheckBox bắt buộc: *"Tôi đã dán đúng WallOutputList/WallWindowList từ GET 9.7.5.2 (hoặc cố ý để trống)"*.
  - Bấm **"Gửi ISAPI"** → Form ghép toàn bộ field phẳng và 2 khối XML thô gửi xuống thiết bị an toàn.
- **9.7.5.4 (GET)**: *Get parameters of a specific video wall* → Nhập `videoWallID` (1) → Bấm gửi.
- **9.7.5.5 (GET)**: *Get linked screen parameters of all outputs* → Nhập `videoWallID` (1) → Bấm gửi.
- **9.7.5.6 (GET)**: *Get video wall capabilities* → Bấm gửi.

### Tab 6 — `6. Màn hình (Screen)` (9.7.8)
- **9.7.8.1 (PUT)**: *Close all screens*
  - ⛔ **CẢNH BÁO:** Nằm trong 3 lệnh cấm ngoài chủ đích — **CHỈ TEST TRÊN MOCKSERVER**, tuyệt đối không gửi lên thiết bị thật đang chiếu.
  - Form Body: `VideoWallID` (1), `OutputID` (để trống hoặc nhập số).
  - Bấm **"Gửi ISAPI"** → Gửi XML `<ScreenCtrl>`.

### Tab 7 — `7. Cửa sổ (Window)` (9.7.11)
- **9.7.11.2 (GET)**: *Get all windows parameters* → Nhập `videoWallID` (1) → Bấm gửi.
- **9.7.11.3 (DELETE)**: *Delete all windows* → Nhập `videoWallID` (1) → Bấm gửi (⚠️ Chỉ test MockServer).
- **9.7.11.4 (POST)**: *Add a window (Form DTO Chuyên Dụng)*
  - Nhập `videoWallID` (1).
  - Thông số toạ độ: `WndOperateMode` (uniformCoordinate), `X` (0), `Y` (0), `Width` (1920), `Height` (1080), `WindowMode` (0).
  - Bảng `SubWindowList`: có sẵn dòng ID 1, SignalMode "video input", VideoInputChannelID "1". Bấm "+ Thêm SubWindow" hoặc "Xoá" để điều chỉnh.
  - Bấm **"Gửi ISAPI"** → Tự động serialize DTO `VwISAPIWindowRequest` sang XML và POST lên thiết bị.
- **9.7.11.5 (GET)**: *Get parameters configuration of a specific window* → Nhập `videoWallID` (1), `VWMWID` (1) → Bấm gửi.
- **9.7.11.6 (PUT)**: *Set parameters of a specific window (Form DTO Chuyên Dụng)*
  - Nhập `videoWallID` (1), `VWMWID` (1).
  - Điều chỉnh toạ độ X, Y, W, H, Mode và danh sách SubWindows.
  - Bấm **"Gửi ISAPI"** → Serialize DTO có thẻ `<id>` và PUT cập nhật vị trí cửa sổ.
- **9.7.11.7 (DELETE)**: *Delete a specific window* → Nhập `videoWallID` (1), `VWMWID` (1) → Bấm gửi → Xoá đúng cửa sổ chỉ định.
- **9.7.11.8 (PUT)**: *Bottom the window* → Nhập `videoWallID` (1), `VWMWID` (1) → Bấm gửi → Đưa cửa sổ xuống đáy.
- **9.7.11.13 (PUT)**: *Top the window* → Nhập `videoWallID` (1), `VWMWID` (1) → Bấm gửi → Đưa cửa sổ lên đỉnh.
- **9.7.11.14 (GET)**: *Get the window configuration capability of the video wall* → Nhập `videoWallID` (1) → Bấm gửi.

---

## Bước 3 — Khung "Logs" & Xem chi tiết gói tin

1. Khung Logs nằm cố định ở đáy cửa sổ, luôn hiển thị và có GridSplitter điều chỉnh chiều cao.
2. Mỗi thao tác Ping / Probe / Gửi ISAPI đều ghi lại 1 bản ghi rõ ràng (Thời gian, Nhóm, Mức độ, Chi tiết).
3. Bấm đúp vào 1 dòng log hoặc bấm nút **"Xem chi tiết"** → Mở cửa sổ `ActivityDetailWindow` xem toàn bộ Header HTTP, URL Endpoint, Request XML gửi đi và Response XML nhận về.

---

## Checklist tổng hợp

- [ ] Vào app trực tiếp, không yêu cầu đăng nhập
- [ ] Ping & Probe kết nối trực tiếp thiết bị thật (hoặc MockServer)
- [ ] Mở Expander Probe xem kết quả khảo sát 3 bảng: Tường, Cổng ra/vào, Lệch CSDL
- [ ] Tab 1 (Board): Gửi 9.7.1.4 thành công
- [ ] Tab 2 (Decoding): Gửi 9.7.2.1, 9.7.2.5, 9.7.2.6, 9.7.2.7, 9.7.2.8 thành công
- [ ] Tab 3 (Output): Gửi 9.7.3.4, 9.7.3.5, 9.7.3.6 (chọn HDMI/DVI từ dropdown), 9.7.3.7 thành công
- [ ] Tab 4 (Input): Gửi 9.7.4.8, 9.7.4.10 (tự sinh nhóm OutputResolution), 9.7.4.15, 9.7.4.16, 9.7.4.18 thành công
- [ ] Tab 5 (Video Wall): Gửi 9.7.5.1, 9.7.5.2 (lấy XML mẫu), 9.7.5.3 (form hybrid + tick xác nhận), 9.7.5.4, 9.7.5.5, 9.7.5.6 thành công
- [ ] Tab 6 (Screen): Gửi 9.7.8.1 thành công trên MockServer (không gửi thiết bị thật đang chạy)
- [ ] Tab 7 (Window): Gửi 9.7.11.2, 9.7.11.3, 9.7.11.4 (thêm window + SubWindows), 9.7.11.5, 9.7.11.6 (sửa window), 9.7.11.7, 9.7.11.8, 9.7.11.13, 9.7.11.14 thành công
- [ ] Xem chi tiết gói tin Request/Response XML đầy đủ trong popup Xem chi tiết log
