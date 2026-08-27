# Kịch bản test toàn trình — Công cụ WPF kiểm thử ISAPI VideoWall

> Viết theo đúng UI thật đang có trên đĩa (`Module.VideoWall.WPF/Views/MainWindow.xaml`, xác nhận lúc viết tài liệu này). Công cụ hoạt động thuần **kết nối trực tiếp thiết bị phần cứng thật (Direct Mode)**, phủ đủ **116/116 API** theo danh mục chuẩn `ISAPI-Videowall-Controller/VideoWall_ISAPI_API_List.md` (11 nhóm, đúng số thứ tự 9.7.1 → 9.7.11). Vào thẳng `MainWindow` mà không cần đăng nhập.

---

## Chuẩn bị (2 tiến trình, đúng thứ tự)

```
1. MockServer:  cd scripts/VwMockServerRunner && dotnet run
                → nghe 127.0.0.1:18080-18083, account admin / Password123!
                (Dùng khi giả lập hoặc test các lệnh nguy hiểm; khi cắm thiết bị thật thì bỏ qua MockServer)
2. WPF Tool:    cd src/Modules/VideoWall/Module.VideoWall.WPF && dotnet run
```

Mở app → vào thẳng `MainWindow`:
- **Thanh tiêu đề & Kết nối (trên cùng)**: IP, Port, Tài khoản, Mật khẩu, Tường số, nút "Kết nối (Ping)" và "Khảo sát (Probe)".
- **Khung "Kết quả khảo sát (Probe)"**: LUÔN hiển thị (không còn thu gọn được), nằm NGAY DƯỚI khung Test API, chứa 2 bảng: Tường, Cổng ra / Kênh vào (bảng "Lệch CSDL ↔ thiết bị" đã bỏ — Direct Mode không có CSDL nào để so sánh).
- **Khung kiểm thử 11 nhóm API ISAPI (trung tâm)**: 11 `TabItem` theo đúng thứ tự 9.7.1 → 9.7.11, chia đôi mỗi tab:
  - Cột trái: Danh sách API trong nhóm (Method badge màu sắc: GET xanh dương, POST xanh lá, PUT cam, DELETE đỏ).
  - Cột giữa: Form nhập tham số động (Path params, Body fields, Dropdown enum).
  - Cột phải: Khung "Phản hồi từ thiết bị (Response)" cố định.
- **Thanh thực thi ISAPI chung** (trên khung 11 tab): Method badge + ô **Endpoint sửa tay được** (gõ trực tiếp nếu cần chỉnh nhanh, không bắt buộc chọn từ danh sách) + nút "Gửi ISAPI".
- **Khung Logs (dưới cùng)**: Bảng nhật ký hoạt động cố định, GridSplitter kéo dãn.

**Lưu ý khi chuyển tab**: mỗi tab tự nhớ lựa chọn API của riêng nó — chuyển qua tab khác rồi quay lại, lựa chọn cũ vẫn còn nguyên; chuyển tab không tự đổi Endpoint đang hiện, chỉ đổi khi bấm chọn 1 API cụ thể trong tab đó.

**Ký hiệu mức độ dùng trong các bảng dưới đây** (theo đúng `VideoWall_ISAPI_API_List.md`):
🟢 Dùng chính · ⚪ Tùy chọn/mở rộng · ⚫ Ngoài phạm vi (stream mạng, dự án chỉ dùng HDMI)

**Ký hiệu ghi chú kiểm thử:**
✅ đã có form tham số đầy đủ · 📝 chỉ path-param (không cần form) · 🚧 form còn thiếu field so với tài liệu gốc (Advanced/nới lỏng, xem `videowall_plan` mục H nếu cần biết chi tiết) · 🧵 body dạng danh sách (List) — form hiện chỉ hỗ trợ gửi **đúng 1 phần tử** · 🔴 **GHI/XOÁ THẬT — chỉ test trên MockServer trừ khi chủ ý** · ⛔ **KHÔNG build form chi tiết** (ngoài phạm vi, gọi thô qua Endpoint nếu cần)

---

## Bước 1 — Kết nối & Khảo sát thiết bị (Direct Mode)

1. Nhập thông số thiết bị trên thanh kết nối:
   - IP: `127.0.0.1` (hoặc IP thật ngoài hiện trường)
   - Port: `18080` (hoặc `80` cho thiết bị thật)
   - Tài khoản: `admin` · Mật khẩu: `Password123!`
   - Tường số: để trống để tự dò, hoặc nhập số cụ thể
2. Bấm **"Kết nối (Ping)"** → thanh trạng thái báo kết nối thành công/thất bại.
3. Bấm **"Khảo sát (Probe)"** → tự động đọc cấu hình thiết bị (`9.7.4.8`, `9.7.5.2`, `9.7.5.5`, `9.7.5.6`). Trước khi bấm, dòng trạng thái bên phải hiện **"Chưa khảo sát."** — nghĩa là chưa từng chạy Probe cho phiên/thiết bị đang chọn, không phải lỗi. Nên Probe trước khi gửi bất kỳ lệnh GHI nào để chắc chắn đang thao tác đúng tường/cổng thật.
4. Xem kết quả ở khung "Kết quả khảo sát" (không cần mở gì cả, luôn hiển thị sẵn):
   - Bảng **Tường**: ID tường, Tên tường.
   - Bảng **Cổng ra / Kênh vào**: OutputId, số cửa sổ, kiểu toạ độ, kênh vào, loại cổng.
5. (Tuỳ chọn) Kéo thanh chia mỏng giữa khung Test API và khung Probe để đổi chiều cao 2 khối theo ý.

---

## Bước 2 — Kiểm thử theo Tab (11 nhóm, 116 API)

### Tab 1 — Board (9.7.1) — 4 API

| Mục | Method | Tên API | Mức | Ghi chú |
|---|---|---|---|---|
| 9.7.1.1 | PUT | Set parameters of a specified sub-board | ⚪ | ✅🧵 `BoardID` path, `slotNo`+`fullFrameEnable` body |
| 9.7.1.2 | GET | Get sub-board capability | ⚪ | 📝 không tham số |
| 9.7.1.3 | PUT | Set parameters of all sub-boards | ⚪ | ✅🧵 field giống 9.7.1.1, gửi 1 phần tử |
| 9.7.1.4 | GET | Get capability of the status of all sub-boards | 🟢 | 📝 không tham số |

### Tab 2 — Decoding (9.7.2) — 16 API

| Mục | Method | Tên API | Mức | Ghi chú |
|---|---|---|---|---|
| 9.7.2.1 | GET | Get decoding device status | 🟢 | 📝 |
| 9.7.2.2 | GET | Get network pre-monitor parameters of a video wall | ⚪ | 📝 `videoWallID` |
| 9.7.2.3 | PUT | Set network pre-monitor parameters of a video wall | ⚪ | ✅ resolution/frameRate/bitRate... |
| 9.7.2.4 | GET | Get sub window configuration capability | ⚪ | 📝 `videoWallID/VWMWID/VWSWID` |
| 9.7.2.5 | PUT | Start dynamic decoding | 🟢 | 📝 `videoWallID/VWMWID/VWSWID` |
| 9.7.2.6 | GET | Get decoding status of all sub windows of a specific window | 🟢 | 📝 |
| 9.7.2.7 | PUT | Stop dynamic decoding | 🟢 | 📝 |
| 9.7.2.8 | GET | Get decoding status of all sub-windows of all windows | 🟢 | 📝 `videoWallID` — API giám sát lõi, poll 3-5s |
| 9.7.2.9 | GET | Get sub-board stream exporting configurations | ⚪ | 📝 |
| 9.7.2.10 | PUT | Set sub-board stream exporting configurations | ⚪ | ✅ body JSON (`enabled`), không phải XML |
| 9.7.2.11 | GET | Get capability of default decoding delay parameters | ⚪ | 📝 |
| 9.7.2.12 | GET | Get default decoding delay parameters | ⚪ | 📝 |
| 9.7.2.13 | PUT | Set default decoding delay parameters | ⚪ | ✅ body JSON (`defaultDecodeDelayParam` enum) |
| 9.7.2.14 | GET | Get network pre-monitoring parameters of all video walls | ⚪ | 📝 |
| 9.7.2.15 | PUT | Set network pre-monitoring parameters of all video walls | ⚪ | ✅🧵 field giống 9.7.2.3, gửi 1 phần tử |
| 9.7.2.16 | GET | Get capability of network pre-monitoring parameters of video wall | ⚪ | 📝 |

### Tab 3 — Output (9.7.3) — 9 API

| Mục | Method | Tên API | Mức | Ghi chú |
|---|---|---|---|---|
| 9.7.3.1 | GET | Get the audio output channels' parameters | ⚪ | 📝 |
| 9.7.3.2 | PUT | Set parameters of all audio output channels | ⚪ | ✅🧵 `sourceType` enum chưa đủ (⚠️ đọc GET thật trước khi khoá cứng) |
| 9.7.3.3 | PUT | Set parameters of all video outputs | ⚪ | ✅🧵 nhiều field, `PortInBoard` để ReadOnly |
| 9.7.3.4 | GET | Get basic parameters of all video outputs | 🟢 | 📝 |
| 9.7.3.5 | GET | Get parameters of a specific video output | 🟢 | 📝 `channelID` |
| 9.7.3.6 | PUT | Set parameters of a specific video output | 🟢 | ✅ `channelID`, `id`, `portType` dropdown |
| 9.7.3.7 | GET | Get the capability of a specific video output | 🟢 | 📝 `channelID` |
| 9.7.3.8 | PUT | Set parameters of all video output channels | ⚪ | ✅ body đơn (KHÔNG phải List dù tên "all" — đúng theo tài liệu gốc, không phải lỗi) |
| 9.7.3.9 | GET | Get the configuration capability of all video output channels | ⚪ | 📝 |

### Tab 4 — Input (9.7.4) — 34 API

⚠️ `channelID` thật KHÔNG phải số nhỏ 1..N — là ID 32-bit đóng gói (`Type<<24 | BoardID<<16 | PortID`). Nên đọc `9.7.4.8` trước để lấy `channelID` thật rồi mới test các API path-param bên dưới.

| Mục | Method | Tên API | Mức | Ghi chú |
|---|---|---|---|---|
| 9.7.4.1 | GET | Get the audio capabilities | ⚪ | 📝 |
| 9.7.4.2 | GET | Get capability set of adding signal source group | ⚪ | 📝 |
| 9.7.4.3 | POST | Get signal source groups | ⚪ | 📝 POST nhưng không có body |
| 9.7.4.4 | GET | Get capability of editing signal source group | ⚪ | 📝 |
| 9.7.4.5 | GET | Get capability of no signal parameters of signal source | ⚪ | 📝 |
| 9.7.4.6 | GET | Get no signal parameters of signal source | ⚪ | 📝 |
| 9.7.4.7 | GET | Get video capabilities | ⚪ | 📝 |
| 9.7.4.8 | GET | Get parameters of all video input channels | 🟢 | 📝 chạy trước tiên để lấy `channelID` thật |
| 9.7.4.9 | PUT | Set parameters of all video input channels | ⚪ | ✅🧵 field giống 9.7.4.10, gửi 1 phần tử |
| 9.7.4.10 | PUT | Set parameters of a specified signal source | 🟢 | ✅ đổi tên nguồn ("Cổng 3" → "Dashboard GT") |
| 9.7.4.11 | GET | Get parameters of a specific signal source | ⚪ | 📝 `channelID` |
| 9.7.4.12 | GET | Get color parameters of a specific signal source | ⚪ | 📝 `channelID` |
| 9.7.4.13 | PUT | Set color parameters of a specified signal source | ⚪ | ✅ brightness/contrast/saturation/hue/sharpness 0-100 |
| 9.7.4.14 | GET | Get the color configuration capability of a signal source | ⚪ | 📝 `channelID` |
| 9.7.4.15 | GET | Get picture cropping parameters of a specific signal source | 🟢 | 📝 `channelID` |
| 9.7.4.16 | PUT | Set picture cropping parameters of a specified signal source | 🟢 | ✅ BẮT BUỘC cho composite window toàn tường |
| 9.7.4.17 | GET | Get the capability of configuring picture cropping parameters | ⚪ | 📝 `channelID` |
| 9.7.4.18 | GET | Get captured pictures | 🟢 | 📝 `channelID` — ⚠️ Response là ảnh JPEG nhị phân, không phải text |
| 9.7.4.19 | GET | Get the capability of configuring image position adjustment parameters | ⚪ | 📝 `channelID` |
| 9.7.4.20 | PUT | Set the custom resolution of a specified signal source | ⚪ | ✅ `id`/`imageWidth`/`imageHeight` bắt buộc — sai bội số alignment sẽ lỗi |
| 9.7.4.21 | GET | Get the capability of customizing the resolution of a specified signal source | ⚪ | 📝 `channelID` |
| 9.7.4.22 | GET | Get the OSD configuration capability of a signal source | ⚪ | 📝 `channelID` |
| 9.7.4.23 | GET | Get the video input capability | ⚪ | 📝 |
| 9.7.4.24 | GET | Get splicing configuration of all signal resources | ⚪ | 📝 |
| 9.7.4.25 | PUT | Set jointing parameters of a specified signal source | ⚪ | ✅🚧 danh sách kênh ghép nhập dạng chuỗi cách nhau dấu phẩy (Advanced) |
| 9.7.4.26 | GET | Get splicing parameters of a signal source | ⚪ | 📝 `channelID` |
| 9.7.4.27 | GET | Get signal source splicing capability | ⚪ | 📝 |
| 9.7.4.28 | GET | Get all video streams' parameters | ⚫ | 📝 ngoài phạm vi (stream mạng) |
| 9.7.4.29 | PUT | Set all video stream parameters | ⚫ | ⛔ ngoài phạm vi — gọi thô qua Endpoint nếu thật sự cần |
| 9.7.4.30 | PUT | Set parameters of a specific video stream | ⚫ | ⛔ ngoài phạm vi |
| 9.7.4.31 | DELETE | Delete parameters of a specific video stream | ⚫ | 🔴 ngoài phạm vi + xoá thật |
| 9.7.4.32 | GET | Get parameters of a specified video stream | ⚫ | 📝 ngoài phạm vi |
| 9.7.4.33 | GET | Get video stream capability | ⚫ | 📝 ngoài phạm vi |
| 9.7.4.34 | GET | Get capability of searching for network input source parameters | ⚫ | 📝 ngoài phạm vi |

### Tab 5 — Video Wall (9.7.5) — 6 API, toàn bộ 🟢 Dùng chính

| Mục | Method | Tên API | Ghi chú |
|---|---|---|---|
| 9.7.5.1 | GET | Get the capability of video wall controller | 📝 gọi ĐẦU TIÊN khi kết nối máy mới |
| 9.7.5.2 | GET | Get parameters of all video walls | 📝 ⚠️ Copy `<WallOutputList>`/`<WallWindowList>` từ Response để dùng cho 9.7.5.3 |
| 9.7.5.3 | PUT | Set parameters of a specific video wall | ✅ Form Hybrid: field phẳng + 2 ô dán XML thô `WallOutputList`/`WallWindowList` (bắt buộc tick xác nhận) |
| 9.7.5.4 | GET | Get parameters of a specific video wall | 📝 `videoWallID` |
| 9.7.5.5 | GET | Get linked screen parameters of all outputs | 📝 nguồn sự thật map SCR-xx ↔ ô lưới ↔ channelID |
| 9.7.5.6 | GET | Get video wall capabilities | 📝 maxWallNums, maxWindowNums, baseOutputSize |

### Tab 6 — Plan (9.7.6) — 4 API, toàn bộ ⚪ (nhóm mới)

| Mục | Method | Tên API | Ghi chú |
|---|---|---|---|
| 9.7.6.1 | POST | Add a plan | ✅🚧 cấu trúc lồng sâu (`ActTimeDetail`/`PlanDetailList`) — kiểm tra kỹ Request XML trong log trước khi tin |
| 9.7.6.2 | GET | Get configuration capability of a specific plan | 📝 `videoWallID`, `planTemplateID` |
| 9.7.6.3 | GET | Get plan configuration capability | 📝 `videoWallID` |
| 9.7.6.4 | GET | Get the current plan | 📝 `videoWallID` |

### Tab 7 — Scene (9.7.7) — 9 API, toàn bộ ⚪ (nhóm mới)

⚠️ Thiết bị chỉ lưu `id`+`name` cho scene — bố cục thật nằm ở CSDL backend, không sửa được qua API này.

| Mục | Method | Tên API | Ghi chú |
|---|---|---|---|
| 9.7.7.1 | GET | Get all scenes' parameters | 📝 `videoWallID` |
| 9.7.7.2 | PUT | Set parameters of a specific scene | ✅ chỉ `id`+`name` |
| 9.7.7.3 | PUT | Switch to a specific scene | 🔴📝 KÍCH HOẠT scene thật, đổi ngay layout đang chiếu — chỉ test khi chủ ý. Lỗi `inSceneSwitchingPleaseDoNotOperate` = đang chuyển dở, thử lại sau |
| 9.7.7.4 | PUT | Save the current scene | 🔴📝 **GHI ĐÈ scene thật** bằng bố cục đang chạy. Lưu ý: thiết bị KHÔNG lưu được virtual-LED/ảnh nền qua lệnh này (`isSupportSaveSceneVirLed/BaseMap=false` đã đo thật) — CSDL backend phải tự bù phần đó |
| 9.7.7.5 | GET | Get scene configuration capability | 📝 `videoWallID` — maxSceneNums |
| 9.7.7.6 | GET | Get the current scene | 📝 `videoWallID` |
| 9.7.7.7 | GET | Get scene control parameters capability | 📝 |
| 9.7.7.8 | GET | Get scene control parameters | 📝 |
| 9.7.7.9 | PUT | Set scene control parameters | ✅ body JSON, không path param |

### Tab 8 — Screen (9.7.8) — 1 API, 🟢 Dùng chính

| Mục | Method | Tên API | Ghi chú |
|---|---|---|---|
| 9.7.8.1 | PUT | Close all screens | 🔴 **TẮT TẤT CẢ MÀN HÌNH** — chỉ test trên MockServer, tuyệt đối không gửi lên thiết bị thật đang chiếu. Không có API bật-lại/bật-từng-màn (cần RS-232/485 riêng) |

### Tab 9 — Text (LED) (9.7.9) — 8 API, toàn bộ ⚪ (nhóm mới)

| Mục | Method | Tên API | Ghi chú |
|---|---|---|---|
| 9.7.9.1 | PUT | Set parameters of all virtual LEDs | ✅🧵🚧 nhiều enum (font/màu/định dạng giờ), gửi 1 phần tử |
| 9.7.9.2 | GET | Get parameters of all virtual LEDs | 📝 `videoWallID` |
| 9.7.9.3 | POST | Add all virtual LEDs | ✅🚧 body đơn (không phải List dù tên "all"), có thêm field clock/weather khi chọn `ledType` tương ứng |
| 9.7.9.4 | GET | Get parameters of a specified LED | 📝 `videoWallID`/`SubtitlesID` |
| 9.7.9.5 | DELETE | Delete a specific virtual LED | 🔴 xoá thật — chỉ test MockServer trừ khi chủ ý |
| 9.7.9.6 | PUT | Set parameters of a specific virtual LED | ✅🚧 |
| 9.7.9.7 | GET | Get the virtual LED configuration capability | 📝 |
| 9.7.9.8 | GET | Get configuration capability of all virtual LEDs | 📝 `videoWallID` |

### Tab 10 — Wallpaper (9.7.10) — 8 API, toàn bộ ⚪ (nhóm mới)

⚠️ Phần lớn field của `BaseMap` chỉ mang tính metadata đọc-được, KHÔNG có cơ chế upload ảnh qua chương API này — thực tế chỉ `name` chắc chắn có tác dụng khi PUT.

| Mục | Method | Tên API | Ghi chú |
|---|---|---|---|
| 9.7.10.1 | GET | Get configuration capability of background picture window | 📝 `videoWallID`/`mapFileID` |
| 9.7.10.2 | GET | Get the capability of all background pictures | 📝 `videoWallID` |
| 9.7.10.3 | PUT | Set parameters of all background pictures | ✅🧵 chỉ `name` chắc chắn có tác dụng, gửi 1 phần tử |
| 9.7.10.4 | DELETE | Delete a specific background picture | 🔴 xoá thật |
| 9.7.10.5 | PUT | Set parameters of a specific background picture | ✅ giống 9.7.10.3 |
| 9.7.10.6 | GET | Get parameters of a background picture | 📝 `mapFileID` |
| 9.7.10.7 | GET | Get the background picture configuration capability | 📝 |
| 9.7.10.8 | GET | Get configuration of all background pictures | 📝 query `isGetBaseMapFile` |

### Tab 11 — Window (9.7.11) — 17 API

| Mục | Method | Tên API | Mức | Ghi chú |
|---|---|---|---|---|
| 9.7.11.1 | GET | Get LED or LCD areas | ⚪ | 📝 `videoWallID` |
| 9.7.11.2 | GET | Get all windows' parameters | 🟢 | 📝 vẽ lưới khi load |
| 9.7.11.3 | DELETE | Delete all windows | 🟢 | 🔴 xoá TẤT CẢ window của 1 wall — chỉ test MockServer trừ khi chủ ý |
| 9.7.11.4 | POST | Add a window | 🟢 | ✅ Form DTO chuyên dụng — `Rect` hệ uniformCoordinate, bảng `SubWindowList` thêm/xoá được |
| 9.7.11.5 | GET | Get parameters configuration of a specific window | 🟢 | 📝 `VWMWID` |
| 9.7.11.6 | PUT | Set parameters of a specific window | 🟢 | ✅ Form DTO chuyên dụng — move/resize/đổi nguồn |
| 9.7.11.7 | DELETE | Delete a specific window | 🟢 | 🔴 xoá 1 window |
| 9.7.11.8 | PUT | Bottom the window | 🟢 | 📝 đưa xuống đáy z-order |
| 9.7.11.9 | GET | Get single configuration capabilities of sub-windows | ⚪ | 📝 |
| 9.7.11.10 | GET | Get parameters of decoding delay | ⚪ | 📝 |
| 9.7.11.11 | GET | Get decoding delay capability | ⚪ | 📝 |
| 9.7.11.12 | GET | Get the configuration capability of full-frame-rate fluent video mode | ⚪ | 📝 |
| 9.7.11.13 | PUT | Top the window | 🟢 | 📝 đưa lên đỉnh z-order |
| 9.7.11.14 | GET | Get the window configuration capability of the video wall | 🟢 | 📝 lỗi đa client: `multipleVideowallClientConflict` |
| 9.7.11.15 | GET | Get the parameters configuration capability of sub-stream in multi-screen mode | ⚪ | 📝 |
| 9.7.11.16 | GET | Get the configuration parameters of the stream type for streaming | ⚪ | 📝 |
| 9.7.11.17 | GET | Get the pre-editing capability of video wall | ⚪ | 📝 |

---

## Bước 3 — Khung "Logs" & Xem chi tiết gói tin

1. Khung Logs nằm cố định ở đáy cửa sổ, luôn hiển thị, có GridSplitter điều chỉnh chiều cao.
2. Mỗi thao tác Ping / Probe / Gửi ISAPI đều ghi lại 1 bản ghi (Thời gian, Nhóm, Mức độ, Chi tiết).
3. Bấm đúp vào 1 dòng log hoặc bấm "Xem chi tiết" → mở `ActivityDetailWindow` xem toàn bộ Header HTTP, URL Endpoint, Request XML/JSON gửi đi và Response nhận về — dùng để xác nhận đúng field/đúng cấu trúc List-wrapper trước khi tin tưởng 1 API mới.

---

## Checklist tổng hợp

**An toàn — có thể test trên thiết bị thật:**
- [ ] Vào app trực tiếp, không yêu cầu đăng nhập; Ping & Probe thành công
- [ ] Khung Probe hiển thị đúng 2 bảng (Tường, Cổng ra/Kênh vào), không còn bảng Lệch CSDL
- [ ] Cả 11 tab hiển thị đúng danh sách API, chuyển tab không làm mất lựa chọn đã chọn ở tab khác
- [ ] Toàn bộ API GET (📝, phần lớn trong 116 API) gửi được, Response hợp lệ
- [ ] Endpoint sửa tay được — gõ thử 1 giá trị rồi gửi ngay, không bị ghi đè
- [ ] Tab 5 (Video Wall): làm đủ chu trình 9.7.5.2 → copy XML → 9.7.5.3 (tick xác nhận) → 9.7.5.4 xác nhận đã đổi
- [ ] Tab 11 (Window): 9.7.11.4 thêm window (kèm SubWindows) → 9.7.11.6 sửa → 9.7.11.8/.13 đổi z-order → 9.7.11.7 xoá đúng 1 window

**Chỉ test trên MockServer (ghi/xoá thật, có thể phá cấu hình đang chạy):**
- [ ] `9.7.7.3` activate scene, `9.7.7.4` saveData (ghi đè scene)
- [ ] `9.7.8.1` closeAll (tắt tất cả màn)
- [ ] `9.7.9.5` xoá virtual LED, `9.7.10.4` xoá ảnh nền, `9.7.4.31` xoá stream
- [ ] `9.7.11.3` xoá tất cả window của 1 wall

**Cần đối chiếu kỹ Request XML trong log trước khi tin (field còn nới lỏng/Advanced):**
- [ ] `9.7.6.1` Add a plan (cấu trúc lồng sâu)
- [ ] `9.7.4.25` join/tách nguồn (danh sách kênh dạng chuỗi)
- [ ] `9.7.9.1`/`.3`/`.6` virtual LED (nhiều enum màu/font/định dạng giờ)
- [ ] 7 API dạng List-wrapper (`9.7.1.3`, `9.7.2.15`, `9.7.3.2`, `9.7.3.3`, `9.7.4.9`, `9.7.9.1`, `9.7.10.3`) — xác nhận Request XML có bọc đúng thẻ `<XxxList>` bên ngoài, không gửi thiếu

**Ngoài phạm vi dự án — không cần test kỹ:**
- [ ] `9.7.4.28`–`.34` (stream mạng) — biết là gọi được (GET) là đủ, 2 API PUT stream (`.29`/`.30`) không có form chi tiết, có thể bỏ qua
