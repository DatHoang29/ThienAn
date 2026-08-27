# Kịch bản test toàn trình — Công cụ WPF kiểm thử ISAPI VideoWall

> Viết theo đúng UI thật đang có trên đĩa (`Module.VideoWall.WPF/Views/MainWindow.xaml`, xác nhận lúc viết tài liệu này). Công cụ hoạt động thuần **kết nối trực tiếp thiết bị phần cứng thật (Direct Mode)**, phủ đủ **116/116 API** theo danh mục chuẩn `ISAPI-Videowall-Controller/VideoWall_ISAPI_API_List.md` (11 nhóm, đúng số thứ tự 9.7.1 → 9.7.11). Vào thẳng `MainWindow` mà không cần đăng nhập.

---

## Chuẩn bị (2 tiến trình, đúng thứ tự)

MockServer: cd scripts/VwMockServerRunner && dotnet run → nghe 127.0.0.1:18080-18083, account admin / Password123! (Dùng khi giả lập hoặc test các lệnh nguy hiểm; khi cắm thiết bị thật thì bỏ qua MockServer)
WPF Tool: cd src/Modules/VideoWall/Module.VideoWall.WPF && dotnet run


Mở app → vào thẳng `MainWindow`:
- **Thanh tiêu đề & Kết nối (trên cùng)**: IP, Port, Tài khoản, Mật khẩu, Tường số, nút "Kết nối (Ping)" và "Khảo sát (Probe)".
- **Khung "Kết quả khảo sát (Probe)"**: LUÔN hiển thị (không còn thu gọn được), nằm NGAY DƯỚI khung Test API, chứa 2 bảng: Tường, Cổng ra / Kênh vào (bảng "Lệch CSDL ↔ thiết bị" đã bỏ — Direct Mode không có CSDL nào để so sánh).
- **Khung kiểm thử 11 nhóm API ISAPI (trung tâm)**: 11 `TabItem` theo đúng thứ tự 9.7.1 → 9.7.11, chia đôi mỗi tab:
  - Cột trái: Danh sách API trong nhóm (Method badge màu sắc: GET xanh dương, POST xanh lá, PUT cam, DELETE đỏ). Tên API trong danh sách hiển thị **bằng tiếng Việt**.
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
| 9.7.1.1 | PUT | Cấu hình tham số của 1 sub-board được chỉ định | ⚪ | ✅🧵 `BoardID` path, `slotNo`+`fullFrameEnable` body |
| 9.7.1.2 | GET | Lấy khả năng của sub-board | ⚪ | 📝 không tham số |
| 9.7.1.3 | PUT | Cấu hình tham số của tất cả sub-board | ⚪ | ✅🧵 field giống 9.7.1.1, gửi 1 phần tử |
| 9.7.1.4 | GET | Lấy khả năng trạng thái của tất cả sub-board | 🟢 | 📝 không tham số |

### Tab 2 — Decoding (9.7.2) — 16 API

| Mục | Method | Tên API | Mức | Ghi chú |
|---|---|---|---|---|
| 9.7.2.1 | GET | Lấy trạng thái thiết bị giải mã | 🟢 | 📝 |
| 9.7.2.2 | GET | Lấy tham số pre-monitor mạng của 1 tường ghép | ⚪ | 📝 `videoWallID` |
| 9.7.2.3 | PUT | Cấu hình tham số pre-monitor mạng của 1 tường ghép | ⚪ | ✅ resolution/frameRate/bitRate... |
| 9.7.2.4 | GET | Lấy khả năng cấu hình cửa sổ con | ⚪ | 📝 `videoWallID/VWMWID/VWSWID` |
| 9.7.2.5 | PUT | Bắt đầu giải mã động | 🟢 | 📝 `videoWallID/VWMWID/VWSWID` |
| 9.7.2.6 | GET | Lấy trạng thái giải mã của tất cả cửa sổ con thuộc 1 cửa sổ cụ thể | 🟢 | 📝 |
| 9.7.2.7 | PUT | Dừng giải mã động | 🟢 | 📝 |
| 9.7.2.8 | GET | Lấy trạng thái giải mã của tất cả cửa sổ con thuộc tất cả cửa sổ | 🟢 | 📝 `videoWallID` — API giám sát lõi, poll 3-5s |
| 9.7.2.9 | GET | Lấy cấu hình xuất stream của sub-board | ⚪ | 📝 |
| 9.7.2.10 | PUT | Cấu hình xuất stream của sub-board | ⚪ | ✅ body JSON (`enabled`), không phải XML |
| 9.7.2.11 | GET | Lấy khả năng tham số độ trễ giải mã mặc định | ⚪ | 📝 |
| 9.7.2.12 | GET | Lấy tham số độ trễ giải mã mặc định | ⚪ | 📝 |
| 9.7.2.13 | PUT | Cấu hình tham số độ trễ giải mã mặc định | ⚪ | ✅ body JSON (`defaultDecodeDelayParam` enum) |
| 9.7.2.14 | GET | Lấy tham số pre-monitor mạng của tất cả tường ghép | ⚪ | 📝 |
| 9.7.2.15 | PUT | Cấu hình tham số pre-monitor mạng của tất cả tường ghép | ⚪ | ✅🧵 field giống 9.7.2.3, gửi 1 phần tử |
| 9.7.2.16 | GET | Lấy khả năng tham số pre-monitor mạng của tường ghép | ⚪ | 📝 |

### Tab 3 — Output (9.7.3) — 9 API

| Mục | Method | Tên API | Mức | Ghi chú |
|---|---|---|---|---|
| 9.7.3.1 | GET | Lấy tham số của các kênh xuất audio | ⚪ | 📝 |
| 9.7.3.2 | PUT | Cấu hình tham số của tất cả kênh xuất audio | ⚪ | ✅🧵 `sourceType` enum chưa đủ (⚠️ đọc GET thật trước khi khoá cứng) |
| 9.7.3.3 | PUT | Cấu hình tham số của tất cả cổng xuất video | ⚪ | ✅🧵 nhiều field, `PortInBoard` để ReadOnly |
| 9.7.3.4 | GET | Lấy tham số cơ bản của tất cả cổng xuất video | 🟢 | 📝 |
| 9.7.3.5 | GET | Lấy tham số của 1 cổng xuất video cụ thể | 🟢 | 📝 `channelID` |
| 9.7.3.6 | PUT | Cấu hình tham số của 1 cổng xuất video cụ thể | 🟢 | ✅ `channelID`, `id`, `portType` dropdown |
| 9.7.3.7 | GET | Lấy khả năng của 1 cổng xuất video cụ thể | 🟢 | 📝 `channelID` |
| 9.7.3.8 | PUT | Cấu hình tham số của tất cả kênh xuất video | ⚪ | ✅ body đơn (KHÔNG phải List dù tên "all" — đúng theo tài liệu gốc, không phải lỗi) |
| 9.7.3.9 | GET | Lấy khả năng cấu hình của tất cả kênh xuất video | ⚪ | 📝 |

### Tab 4 — Input (9.7.4) — 34 API

⚠️ `channelID` thật KHÔNG phải số nhỏ 1..N — là ID 32-bit đóng gói (`Type<<24 | BoardID<<16 | PortID`). Nên đọc `9.7.4.8` trước để lấy `channelID` thật rồi mới test các API path-param bên dưới.

| Mục | Method | Tên API | Mức | Ghi chú |
|---|---|---|---|---|
| 9.7.4.1 | GET | Lấy khả năng audio | ⚪ | 📝 |
| 9.7.4.2 | GET | Lấy khả năng thêm nhóm nguồn tín hiệu | ⚪ | 📝 |
| 9.7.4.3 | POST | Lấy danh sách nhóm nguồn tín hiệu | ⚪ | 📝 POST nhưng không có body |
| 9.7.4.4 | GET | Lấy khả năng sửa nhóm nguồn tín hiệu | ⚪ | 📝 |
| 9.7.4.5 | GET | Lấy khả năng tham số khi mất tín hiệu của nguồn tín hiệu | ⚪ | 📝 |
| 9.7.4.6 | GET | Lấy tham số khi mất tín hiệu của nguồn tín hiệu | ⚪ | 📝 |
| 9.7.4.7 | GET | Lấy khả năng video | ⚪ | 📝 |
| 9.7.4.8 | GET | Lấy tham số của tất cả kênh vào video | 🟢 | 📝 chạy trước tiên để lấy `channelID` thật |
| 9.7.4.9 | PUT | Cấu hình tham số của tất cả kênh vào video | ⚪ | ✅🧵 field giống 9.7.4.10, gửi 1 phần tử |
| 9.7.4.10 | PUT | Cấu hình tham số của 1 nguồn tín hiệu được chỉ định | 🟢 | ✅ đổi tên nguồn ("Cổng 3" → "Dashboard GT") |
| 9.7.4.11 | GET | Lấy tham số của 1 nguồn tín hiệu cụ thể | ⚪ | 📝 `channelID` |
| 9.7.4.12 | GET | Lấy tham số màu của 1 nguồn tín hiệu cụ thể | ⚪ | 📝 `channelID` |
| 9.7.4.13 | PUT | Cấu hình tham số màu của 1 nguồn tín hiệu được chỉ định | ⚪ | ✅ brightness/contrast/saturation/hue/sharpness 0-100 |
| 9.7.4.14 | GET | Lấy khả năng cấu hình màu của nguồn tín hiệu | ⚪ | 📝 `channelID` |
| 9.7.4.15 | GET | Lấy tham số cắt ảnh của 1 nguồn tín hiệu cụ thể | 🟢 | 📝 `channelID` |
| 9.7.4.16 | PUT | Cấu hình tham số cắt ảnh của 1 nguồn tín hiệu được chỉ định | 🟢 | ✅ BẮT BUỘC cho composite window toàn tường |
| 9.7.4.17 | GET | Lấy khả năng cấu hình tham số cắt ảnh của nguồn tín hiệu | ⚪ | 📝 `channelID` |
| 9.7.4.18 | GET | Lấy ảnh đã chụp | 🟢 | 📝 `channelID` — ⚠️ Response là ảnh JPEG nhị phân, không phải text |
| 9.7.4.19 | GET | Lấy khả năng cấu hình tham số chỉnh vị trí ảnh của nguồn tín hiệu | ⚪ | 📝 `channelID` |
| 9.7.4.20 | PUT | Cấu hình độ phân giải tuỳ chỉnh của 1 nguồn tín hiệu được chỉ định | ⚪ | ✅ `id`/`imageWidth`/`imageHeight` bắt buộc — sai bội số alignment sẽ lỗi |
| 9.7.4.21 | GET | Lấy khả năng tuỳ chỉnh độ phân giải của 1 nguồn tín hiệu được chỉ định | ⚪ | 📝 `channelID` |
| 9.7.4.22 | GET | Lấy khả năng cấu hình OSD của nguồn tín hiệu | ⚪ | 📝 `channelID` |
| 9.7.4.23 | GET | Lấy khả năng kênh vào video | ⚪ | 📝 |
| 9.7.4.24 | GET | Lấy cấu hình ghép của tất cả nguồn tín hiệu | ⚪ | 📝 |
| 9.7.4.25 | PUT | Cấu hình tham số ghép của 1 nguồn tín hiệu được chỉ định | ⚪ | ✅🚧 danh sách kênh ghép nhập dạng chuỗi cách nhau dấu phẩy (Advanced) |
| 9.7.4.26 | GET | Lấy tham số ghép của nguồn tín hiệu | ⚪ | 📝 `channelID` |
| 9.7.4.27 | GET | Lấy khả năng ghép của nguồn tín hiệu | ⚪ | 📝 |
| 9.7.4.28 | GET | Lấy tham số của tất cả stream video | ⚫ | 📝 ngoài phạm vi (stream mạng) |
| 9.7.4.29 | PUT | Cấu hình tham số của tất cả stream video | ⚫ | ⛔ ngoài phạm vi — gọi thô qua Endpoint nếu thật sự cần |
| 9.7.4.30 | PUT | Cấu hình tham số của 1 stream video cụ thể | ⚫ | ⛔ ngoài phạm vi |
| 9.7.4.31 | DELETE | Xoá tham số của 1 stream video cụ thể | ⚫ | 🔴 ngoài phạm vi + xoá thật |
| 9.7.4.32 | GET | Lấy tham số của 1 stream video được chỉ định | ⚫ | 📝 ngoài phạm vi |
| 9.7.4.33 | GET | Lấy khả năng stream video | ⚫ | 📝 ngoài phạm vi |
| 9.7.4.34 | GET | Lấy khả năng tìm tham số nguồn tín hiệu mạng | ⚫ | 📝 ngoài phạm vi |

### Tab 5 — Video Wall (9.7.5) — 6 API, toàn bộ 🟢 Dùng chính

| Mục | Method | Tên API | Ghi chú |
|---|---|---|---|
| 9.7.5.1 | GET | Lấy khả năng của bộ điều khiển tường ghép | 📝 gọi ĐẦU TIÊN khi kết nối máy mới |
| 9.7.5.2 | GET | Lấy tham số của tất cả tường ghép | 📝 Response trả về danh sách tất cả tường, mỗi tường có sẵn `<WallOutputList>`/`<WallWindowList>` lồng bên trong — dùng để đối chiếu hoặc nạp trực tiếp cho 9.7.5.3 |
| 9.7.5.3 | PUT | Cấu hình tham số của 1 tường ghép cụ thể | ✅ Form Hybrid — nhập bảng "Danh sách cổng ra"/"Danh sách cửa sổ tường" (tự sinh XML `WallOutputList`/`WallWindowList`), hoặc dán XML rồi bấm "Nạp từ XML thô bên dưới" để đổ lên bảng. Có 2 nút "💾 Lưu làm cấu hình gốc" / "↩ Khôi phục cấu hình gốc" để lưu/trả lại đúng cấu hình đang chạy trước khi test (không còn checkbox xác nhận) |
| 9.7.5.4 | GET | Lấy tham số của 1 tường ghép cụ thể | 📝 `videoWallID` |
| 9.7.5.5 | GET | Lấy tham số màn hình liên kết của tất cả cổng xuất | 📝 nguồn sự thật map SCR-xx ↔ ô lưới ↔ channelID |
| 9.7.5.6 | GET | Lấy khả năng của tường ghép | 📝 maxWallNums, maxWindowNums, baseOutputSize |

### Tab 6 — Plan (9.7.6) — 4 API, toàn bộ ⚪ (nhóm mới)

| Mục | Method | Tên API | Ghi chú |
|---|---|---|---|
| 9.7.6.1 | POST | Thêm 1 plan | ✅🚧 cấu trúc lồng sâu (`ActTimeDetail`/`PlanDetailList`) — kiểm tra kỹ Request XML trong log trước khi tin |
| 9.7.6.2 | GET | Lấy khả năng cấu hình của 1 plan cụ thể | 📝 `videoWallID`, `planTemplateID` |
| 9.7.6.3 | GET | Lấy khả năng cấu hình plan | 📝 `videoWallID` |
| 9.7.6.4 | GET | Lấy plan hiện tại | 📝 `videoWallID` |

### Tab 7 — Scene (9.7.7) — 9 API, toàn bộ ⚪ (nhóm mới)

⚠️ Thiết bị chỉ lưu `id`+`name` cho scene — bố cục thật nằm ở CSDL backend, không sửa được qua API này.

| Mục | Method | Tên API | Ghi chú |
|---|---|---|---|
| 9.7.7.1 | GET | Lấy tham số của tất cả scene | 📝 `videoWallID` |
| 9.7.7.2 | PUT | Cấu hình tham số của 1 scene cụ thể | ✅ chỉ `id`+`name` |
| 9.7.7.3 | PUT | Chuyển sang 1 scene cụ thể | 🔴📝 KÍCH HOẠT scene thật, đổi ngay layout đang chiếu — chỉ test khi chủ ý. Lỗi `inSceneSwitchingPleaseDoNotOperate` = đang chuyển dở, thử lại sau |
| 9.7.7.4 | PUT | Lưu scene hiện tại | 🔴📝 **GHI ĐÈ scene thật** bằng bố cục đang chạy. Lưu ý: thiết bị KHÔNG lưu được virtual-LED/ảnh nền qua lệnh này (`isSupportSaveSceneVirLed/BaseMap=false` đã đo thật) — CSDL backend phải tự bù phần đó |
| 9.7.7.5 | GET | Lấy khả năng cấu hình scene | 📝 `videoWallID` — maxSceneNums |
| 9.7.7.6 | GET | Lấy scene hiện tại | 📝 `videoWallID` |
| 9.7.7.7 | GET | Lấy khả năng tham số điều khiển scene | 📝 |
| 9.7.7.8 | GET | Lấy tham số điều khiển scene | 📝 |
| 9.7.7.9 | PUT | Cấu hình tham số điều khiển scene | ✅ body JSON, không path param |

### Tab 8 — Screen (9.7.8) — 1 API, 🟢 Dùng chính

| Mục | Method | Tên API | Ghi chú |
|---|---|---|---|
| 9.7.8.1 | PUT | Đóng tất cả màn hình | 🔴 **TẮT TẤT CẢ MÀN HÌNH** — chỉ test trên MockServer, tuyệt đối không gửi lên thiết bị thật đang chiếu. Không có API bật-lại/bật-từng-màn (cần RS-232/485 riêng) |

### Tab 9 — Text (LED) (9.7.9) — 8 API, toàn bộ ⚪ (nhóm mới)

| Mục | Method | Tên API | Ghi chú |
|---|---|---|---|
| 9.7.9.1 | PUT | Cấu hình tham số của tất cả LED ảo | ✅🧵🚧 nhiều enum (font/màu/định dạng giờ), gửi 1 phần tử |
| 9.7.9.2 | GET | Lấy tham số của tất cả LED ảo | 📝 `videoWallID` |
| 9.7.9.3 | POST | Thêm tất cả LED ảo | ✅🚧 body đơn (không phải List dù tên "all"), có thêm field clock/weather khi chọn `ledType` tương ứng |
| 9.7.9.4 | GET | Lấy tham số của 1 LED được chỉ định | 📝 `videoWallID`/`SubtitlesID` |
| 9.7.9.5 | DELETE | Xoá 1 LED ảo cụ thể | 🔴 xoá thật — chỉ test MockServer trừ khi chủ ý |
| 9.7.9.6 | PUT | Cấu hình tham số của 1 LED ảo cụ thể | ✅🚧 |
| 9.7.9.7 | GET | Lấy khả năng cấu hình LED ảo | 📝 |
| 9.7.9.8 | GET | Lấy khả năng cấu hình của tất cả LED ảo | 📝 `videoWallID` |

### Tab 10 — Wallpaper (9.7.10) — 8 API, toàn bộ ⚪ (nhóm mới)

⚠️ Phần lớn field của `BaseMap` chỉ mang tính metadata đọc-được, KHÔNG có cơ chế upload ảnh qua chương API này — thực tế chỉ `name` chắc chắn có tác dụng khi PUT.

| Mục | Method | Tên API | Ghi chú |
|---|---|---|---|
| 9.7.10.1 | GET | Lấy khả năng cấu hình cửa sổ ảnh nền | 📝 `videoWallID`/`mapFileID` |
| 9.7.10.2 | GET | Lấy khả năng của tất cả ảnh nền | 📝 `videoWallID` |
| 9.7.10.3 | PUT | Cấu hình tham số của tất cả ảnh nền | ✅🧵 chỉ `name` chắc chắn có tác dụng, gửi 1 phần tử |
| 9.7.10.4 | DELETE | Xoá 1 ảnh nền cụ thể | 🔴 xoá thật |
| 9.7.10.5 | PUT | Cấu hình tham số của 1 ảnh nền cụ thể | ✅ giống 9.7.10.3 |
| 9.7.10.6 | GET | Lấy tham số của 1 ảnh nền | 📝 `mapFileID` |
| 9.7.10.7 | GET | Lấy khả năng cấu hình ảnh nền | 📝 |
| 9.7.10.8 | GET | Lấy cấu hình của tất cả ảnh nền | 📝 query `isGetBaseMapFile` |

### Tab 11 — Window (9.7.11) — 17 API

| Mục | Method | Tên API | Mức | Ghi chú |
|---|---|---|---|---|
| 9.7.11.1 | GET | Lấy vùng LED hoặc LCD | ⚪ | 📝 `videoWallID` |
| 9.7.11.2 | GET | Lấy tham số của tất cả cửa sổ | 🟢 | 📝 vẽ lưới khi load |
| 9.7.11.3 | DELETE | Xoá tất cả cửa sổ | 🟢 | 🔴 xoá TẤT CẢ window của 1 wall — chỉ test MockServer trừ khi chủ ý |
| 9.7.11.4 | POST | Thêm 1 cửa sổ | 🟢 | ✅ Form DTO chuyên dụng — `Rect` hệ uniformCoordinate, bảng `SubWindowList` thêm/xoá được |
| 9.7.11.5 | GET | Lấy cấu hình tham số của 1 cửa sổ cụ thể | 🟢 | 📝 `VWMWID` |
| 9.7.11.6 | PUT | Cấu hình tham số của 1 cửa sổ cụ thể | 🟢 | ✅ Form DTO chuyên dụng — move/resize/đổi nguồn |
| 9.7.11.7 | DELETE | Xoá 1 cửa sổ cụ thể | 🟢 | 🔴 xoá 1 window |
| 9.7.11.8 | PUT | Hạ cửa sổ xuống dưới cùng | 🟢 | 📝 đưa xuống đáy z-order |
| 9.7.11.9 | GET | Lấy khả năng cấu hình đơn lẻ của cửa sổ con | ⚪ | 📝 |
| 9.7.11.10 | GET | Lấy tham số độ trễ giải mã | ⚪ | 📝 |
| 9.7.11.11 | GET | Lấy khả năng độ trễ giải mã | ⚪ | 📝 |
| 9.7.11.12 | GET | Lấy khả năng cấu hình chế độ video mượt full-frame-rate | ⚪ | 📝 |
| 9.7.11.13 | PUT | Đưa cửa sổ lên trên cùng | 🟢 | 📝 đưa lên đỉnh z-order |
| 9.7.11.14 | GET | Lấy khả năng cấu hình cửa sổ của tường ghép | 🟢 | 📝 lỗi đa client: `multipleVideowallClientConflict` |
| 9.7.11.15 | GET | Lấy khả năng cấu hình tham số sub-stream ở chế độ đa màn hình | ⚪ | 📝 |
| 9.7.11.16 | GET | Lấy tham số cấu hình loại stream khi có nhiều cửa sổ | ⚪ | 📝 |
| 9.7.11.17 | GET | Lấy khả năng pre-editing của tường ghép | ⚪ | 📝 |

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
- [ ] Cả 11 tab hiển thị đúng danh sách API bằng tiếng Việt, chuyển tab không làm mất lựa chọn đã chọn ở tab khác
- [ ] Toàn bộ API GET (📝, phần lớn trong 116 API) gửi được, Response hợp lệ
- [ ] Endpoint sửa tay được — gõ thử 1 giá trị rồi gửi ngay, không bị ghi đè
- [ ] Tab 5 (Video Wall): làm đủ chu trình 9.7.5.2 → nhập/nạp bảng Output+Window ở 9.7.5.3 → thử "Lưu làm cấu hình gốc" và "Khôi phục cấu hình gốc" → 9.7.5.4 xác nhận đã đổi
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
