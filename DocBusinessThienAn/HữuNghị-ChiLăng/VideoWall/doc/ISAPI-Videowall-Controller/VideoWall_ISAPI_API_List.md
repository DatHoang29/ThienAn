# VideoWall ISAPI API List

> Chuyen doi tu VideoWall_ISAPI_API_List.xlsx

## Tổng quan

| DANH MỤC API ISAPI — QUẢN LÝ DECODING & VIDEO WALL (Hikvision DS-C66S) |  |  |  |  |
| --- | --- | --- | --- | --- |
| Nguồn: ISAPI Controller – Videowall Controller (chương 9.7, tr.332–499). Số trang = trang PDF gốc. |  |  |  |  |
| Kiến trúc dự án: 4 controller độc lập (4 IP, digest auth) • mọi nguồn qua HDMI input • scene lưu trong DB BE • 1 window không vượt ranh giới controller. |  |  |  |  |
|  |  |  |  |  |
| Sheet / Nhóm | Mục tài liệu | Trang | Số API | Ghi chú cho dự án |
| Board Management | 9.7.1 | 332 | 4 | Health-check board — phát hiện board output lỗi kéo theo nhóm màn |
| Decoding Management | 9.7.2 | 336 | 16 | Poll windows/status (9.7.2.8) là API giám sát lõi của màn Monitor |
| Output Channel Management | 9.7.3 | 360 | 9 | 32 output = tổng 4 máy; panel "Bật/Tắt" mang nghĩa kết nối logic |
| Signal Source Management | 9.7.4 | 373 | 34 | Chỉ dùng nhóm inputs (HDMI). Nhóm streaming/channels: ngoài phạm vi. cutOff bắt buộc cho composite window |
| Video Wall Management | 9.7.5 | 418 | 6 | Capability gọi đầu tiên; outputs (9.7.5.5) là bảng map lưới |
| Video Wall Plan Management | 9.7.6 | 437 | 4 | Tùy chọn — lịch tự động; chứa openScreen/closeScreen (workaround bật/tắt màn) |
| Video Wall Scene Management | 9.7.7 | 440 | 9 | Tùy chọn — scene chính thức nằm ở DB BE (thiết bị chỉ lưu id+name, không sửa được bố cục qua API) |
| Video Wall Screen Management | 9.7.8 | 446 | 1 | Chỉ có closeAll — không có bật từng màn/mở tất cả |
| Video Wall Text Management (Virtual LED) | 9.7.9 | 446 | 8 | Chữ chạy — giai đoạn mở rộng |
| Video Wall Wallpaper Management (Base Map) | 9.7.10 | 463 | 8 | Ảnh nền — giai đoạn mở rộng |
| Video Wall Window Management | 9.7.11 | 469 | 17 | Nhóm lõi kéo-thả: 9.7.11.2/4/6/7 + top/bottom |
|  |  |  |  |  |
| TỔNG |  |  | 116 |  |
|  |  |  |  |  |
| Chú giải màu (các sheet nhóm): |  |  |  |  |
| Xanh lá = Dùng chính cho dự án  •  Trắng = Tùy chọn/mở rộng  •  Xám = Ngoài phạm vi (đã chốt nguồn chỉ qua HDMI) |  |  |  |  |
|  |  |  |  |  |
| Get/Set parameters là lấy/thay đổi cấu hình hiện tại của các module |  |  |  |  |
| System.Xml.XmlElement |  |  |  |  |
| System.Xml.XmlElement |  |  |  |  |

## 1. Board

| 9.7.1  Board Management |  |  |  |  |  |  |
| --- | --- | --- | --- | --- | --- | --- |
| Quản lý sub-board (board cắm trong khung máy, bo mạch của controller) |  |  |  |  |  |  |
|  |  |  |  |  |  |  |
| Mục | Tên API (tài liệu) | Method | URL | Trang | Công dụng / Ghi chú dự án | Mức dùng |
| 9.7.1.1 | Set parameters of a specified sub-board | PUT | /ISAPI/System/Board/<BoardID>/config | 332 | Cấu hình 1 sub-board theo BoardID | Tùy chọn / mở rộng |
| 9.7.1.2 | Get sub-board capability | GET | /ISAPI/System/Board/capabilities | 332 | Khai báo cấu hình hiện tại sub-board | Tùy chọn / mở rộng |
| 9.7.1.3 | Set parameters of all sub-boards | PUT | /ISAPI/System/Board/config | 333 | Cấu hình tất cả sub-board | Tùy chọn / mở rộng |
| 9.7.1.4 | Get capability of the status of all sub-boards | GET | /ISAPI/System/Board/status/capabilities | 334 | Kiểm tra capability trạng thái của các subboard | Dùng chính |

## 2. Decoding

| 9.7.2  Decoding Management |  |  |  |  |  |  |
| --- | --- | --- | --- | --- | --- | --- |
| Quản lý giải mã: trạng thái decode, start/stop, pre-monitor, decode delay |  |  |  |  |  |  |
| Quản lý việc phát hình lên từng subwindows/window |  |  |  |  |  |  |
| Mục | Tên API (tài liệu) | Method | URL | Trang | Công dụng / Ghi chú dự án | Mức dùng |
| 9.7.2.1 | Get decoding device status | GET | /ISAPI/DisplayDev/decoingDevice/status?format=json | 336 | Trạng thái thiết bị giải mã, Health status của thiết bị decode (controller) | Dùng chính |
| 9.7.2.2 | Get network pre-monitor parameters of a video wall | GET | /ISAPI/DisplayDev/VideoWall/<videoWallID>/nPreMonitor | 343 | Lấy cấu hình luồng preview qua mạng của videowall | Tùy chọn / mở rộng |
| 9.7.2.3 | Set network pre-monitor parameters of a video wall | PUT | /ISAPI/DisplayDev/VideoWall/<videoWallID>/nPreMonitor | 343 | Thay đổi cấu hình luồng preview qua mạng của videowall — nhúng preview videoWall vào web | Tùy chọn / mở rộng |
| 9.7.2.4 | Get sub window configuration capability | GET | /ISAPI/DisplayDev/VideoWall/<videoWallID>/windows/<VWMWID>/sub/<VWSWID>/param/capabilities | 344 | Khai báo khả năng (capabilities) của subwindow | Tùy chọn / mở rộng |
| 9.7.2.5 | Start dynamic decoding | PUT | /ISAPI/DisplayDev/VideoWall/<videoWallID>/windows/<VWMWID>/sub/<VWSWID>/start | 345 | Bắt đầu giải mã động — phát nguồn vào 1 sub-window | Dùng chính |
| 9.7.2.6 | Get decoding status of all sub windows of a specific window | GET | /ISAPI/DisplayDev/VideoWall/<videoWallID>/windows/<VWMWID>/sub/<VWSWID>/status | 347 | Khai báo trạng thái decode các sub-window của 1 window — drill-down khi click khối lỗi | Dùng chính |
| 9.7.2.7 | Stop dynamic decoding | PUT | /ISAPI/DisplayDev/VideoWall/<videoWallID>/windows/<VWMWID>/sub/<VWSWID>/stop | 351 | Dừng giải mã 1 sub-window (window còn, ngừng hình) | Dùng chính |
| 9.7.2.8 | Get decoding status of all sub-windows of all windows | GET | /ISAPI/DisplayDev/VideoWall/<videoWallID>/windows/status | 352 | Khai báo Trạng thái decode TẤT CẢ window — poll 3–5s để tô màu lưới Monitor (lỗi camera/nguồn hiện gián tiếp ở đây) | Dùng chính |
| 9.7.2.9 | Get sub-board stream exporting configurations | GET | /ISAPI/DisplayDev/VideoWall/DecodeMgr/BoardStreamExportCfg?format=json | 356 | Khai báo trạng thái stream từ các sub-board hiện đang được bật hay tắt.  (/*opt, boolean, whether to enable exporting sub-board stream*/) | Tùy chọn / mở rộng |
| 9.7.2.10 | Set sub-board stream exporting configurations | PUT | /ISAPI/DisplayDev/VideoWall/DecodeMgr/BoardStreamExportCfg?format=json | 356 | Thay đổi cấu hình của sub-board stream (true/falsse - request ) | Tùy chọn / mở rộng |
| 9.7.2.11 | Get capability of default decoding delay parameters | GET | /ISAPI/DisplayDev/VideoWall/DecodeMgr/DefaultDecodeDelayParams/capabilities?format=json | 356 | Lấy các giá trị mà thiết bị cho phép cấu hình đối với tham số Default Decode Delay. | Tùy chọn / mở rộng |
| 9.7.2.12 | Get default decoding delay parameters | GET | /ISAPI/DisplayDev/VideoWall/DecodeMgr/DefaultDecodeDelayParams?format=json | 357 | Lấy giá trị default decoding delay | Tùy chọn / mở rộng |
| 9.7.2.13 | Set default decoding delay parameters | PUT | /ISAPI/DisplayDev/VideoWall/DecodeMgr/DefaultDecodeDelayParams?format=json | 357 | Thay đổi giá trị default decoding delay | Tùy chọn / mở rộng |
| 9.7.2.14 | Get network pre-monitoring parameters of all video walls | GET | /ISAPI/DisplayDev/VideoWall/nPreMonitor | 357 | Lấy cấu hình hiện tại của chức năng xem trước qua mạng cho tất cả các Video Wall. | Tùy chọn / mở rộng |
| 9.7.2.15 | Set network pre-monitoring parameters of all video walls | PUT | /ISAPI/DisplayDev/VideoWall/nPreMonitor | 358 | Thay đổi cấu hình hiện tại của chức năng xem trước qua mạng cho tất cả các Video Wall. | Tùy chọn / mở rộng |
| 9.7.2.16 | Get capability of network pre-monitoring parameters of video wall | GET | /ISAPI/DisplayDev/VideoWall/nPreMonitor/capabilities | 359 | Kiểm tra khả năng của network pre-monitoring parameters of video wall | Tùy chọn / mở rộng |

## 3. Output Channel

| 9.7.3  Output Channel Management |  |  |  |  |  |  |
| --- | --- | --- | --- | --- | --- | --- |
| Quản lý kênh đầu ra (video/audio) — 32 màn hình |  |  |  |  |  |  |
|  |  |  |  |  |  |  |
| Mục | Tên API (tài liệu) | Method | URL | Trang | Công dụng / Ghi chú dự án | Mức dùng |
| 9.7.3.1 | Get the audio output channels' parameters | GET | /ISAPI/DisplayDev/Audio/outputs/channels | 360 | System.Xml.XmlElement | Tùy chọn / mở rộng |
| 9.7.3.2 | Set parameters of all audio output channels | PUT | /ISAPI/DisplayDev/Audio/outputs/channels | 361 | System.Xml.XmlElement | Tùy chọn / mở rộng |
| 9.7.3.3 | Set parameters of all video outputs | PUT | /ISAPI/DisplayDev/Video/outputs/channels | 362 | System.Xml.XmlElement | Tùy chọn / mở rộng |
| 9.7.3.4 | Get basic parameters of all video outputs | GET | /ISAPI/DisplayDev/Video/outputs/channels | 364 | Lấy câu hình hiện tại của các kênh video output | Dùng chính |
| 9.7.3.5 | Get parameters of a specific video output | GET | /ISAPI/DisplayDev/Video/outputs/channels/<channelID> | 366 | Lấy cấu hình chi tiết 1 output (1 screen) | Dùng chính |
| 9.7.3.6 | Set parameters of a specific video output | PUT | /ISAPI/DisplayDev/Video/outputs/channels/<channelID> | 367 | Cấu hình 1 output | Dùng chính |
| 9.7.3.7 | Get the capability of a specific video output | GET | /ISAPI/DisplayDev/Video/outputs/channels/<channelID>/capabilities | 369 | Khai báo năng lực 1 output — độ phân giải hỗ trợ | Dùng chính |
| 9.7.3.8 | Set parameters of all video output channels | PUT | /ISAPI/DisplayDev/Video/outputs/channels/all | 371 | Áp cấu hình cho toàn bộ kênh output một lần | Tùy chọn / mở rộng |
| 9.7.3.9 | Get the configuration capability of all video output channels | GET | /ISAPI/DisplayDev/Video/outputs/channels/capabilities | 372 | Năng lực cấu hình toàn bộ output | Tùy chọn / mở rộng |

## 4. Signal Source

| 9.7.4  Signal Source Management |  |  |  |  |  |  |
| --- | --- | --- | --- | --- | --- | --- |
| Quản lý nguồn tín hiệu: input HDMI/DVI/SDI, stream mạng, crop, màu, ghép nguồn |  |  |  |  |  |  |
|  |  |  |  |  |  |  |
| Mục | Tên API (tài liệu) | Method | URL | Trang | Công dụng / Ghi chú dự án | Mức dùng |
| 9.7.4.1 | Get the audio capabilities | GET | /ISAPI/DisplayDev/Audio/capabilities | 373 | Năng lực audio | Tùy chọn / mở rộng |
| 9.7.4.2 | Get capability set of adding signal source group | GET | /ISAPI/DisplayDev/SignalSource/AddSignalSourceGroup/capabilities?format=json | 374 | Năng lực thêm nhóm nguồn | Tùy chọn / mở rộng |
| 9.7.4.3 | Get signal source groups | POST | /ISAPI/DisplayDev/SignalSource/GetSignalSourceGroup?format=json | 374 | Lấy các nhóm nguồn tín hiệu (JSON, method POST) | Tùy chọn / mở rộng |
| 9.7.4.4 | Get capability of editing signal source group | GET | /ISAPI/DisplayDev/SignalSource/ModifySignalSourceGroup/capabilities?format=json | 375 | Năng lực sửa nhóm nguồn | Tùy chọn / mở rộng |
| 9.7.4.5 | Get capability of no signal parameters of signal source | GET | /ISAPI/DisplayDev/SignalSource/SignalSourceNoSignalParams/capabilities?format=json | 375 | Năng lực tham số "mất tín hiệu" | Tùy chọn / mở rộng |
| 9.7.4.6 | Get no signal parameters of signal source | GET | /ISAPI/DisplayDev/SignalSource/SignalSourceNoSignalParams?format=json | 376 | Đọc tham số hiển thị khi nguồn mất tín hiệu | Tùy chọn / mở rộng |
| 9.7.4.7 | Get video capabilities | GET | /ISAPI/DisplayDev/Video/capabilities | 376 | Năng lực video (input/stream) của máy | Tùy chọn / mở rộng |
| 9.7.4.8 | Get parameters of all video input channels | GET | /ISAPI/DisplayDev/Video/inputs/channels | 378 | Khai báo cấu hình của tất cả input channel — panel "Nguồn tín hiệu" (mọi nguồn qua HDMI theo kiến trúc đã chốt) | Dùng chính |
| 9.7.4.9 | Set parameters of all video input channels | PUT | /ISAPI/DisplayDev/Video/inputs/channels | 381 | Cấu hình tất cả input | Tùy chọn / mở rộng |
| 9.7.4.10 | Set parameters of a specified signal source | PUT | /ISAPI/DisplayDev/Video/inputs/channels/<channelID> | 383 | Cấu hình 1 nguồn — đổi tên ("Cổng 3" → "Dashboard GT") | Dùng chính |
| 9.7.4.11 | Get parameters of a specific signal source | GET | /ISAPI/DisplayDev/Video/inputs/channels/<channelID> | 385 | Chi tiết 1 nguồn | Tùy chọn / mở rộng |
| 9.7.4.12 | Get color parameters of a specific signal source | GET | /ISAPI/DisplayDev/Video/inputs/channels/<channelID>/color | 387 | Đọc tham số màu 1 nguồn | Tùy chọn / mở rộng |
| 9.7.4.13 | Set color parameters of a specified signal source | PUT | /ISAPI/DisplayDev/Video/inputs/channels/<channelID>/color | 388 | Chỉnh màu 1 nguồn (sáng/tương phản...) | Tùy chọn / mở rộng |
| 9.7.4.14 | Get the color configuration capability of a signal source | GET | /ISAPI/DisplayDev/Video/inputs/channels/<channelID>/color/capabilities | 389 | Năng lực chỉnh màu | Tùy chọn / mở rộng |
| 9.7.4.15 | Get picture cropping parameters of a specific signal source | GET | /ISAPI/DisplayDev/Video/inputs/channels/<channelID>/cutOff | 389 | Đọc tham số CROP (cắt hình) 1 nguồn | Dùng chính |
| 9.7.4.16 | Set picture cropping parameters of a specified signal source | PUT | /ISAPI/DisplayDev/Video/inputs/channels/<channelID>/cutOff | 390 | Đặt CROP 1 nguồn — BẮT BUỘC cho composite window toàn tường (mỗi máy crop 1 phần khung) | Dùng chính |
| 9.7.4.17 | Get the capability of configuring picture cropping parameters of a signal source | GET | /ISAPI/DisplayDev/Video/inputs/channels/<channelID>/cutOff/capabilities | 390 | Năng lực crop | Tùy chọn / mở rộng |
| 9.7.4.18 | Get captured pictures | GET | /ISAPI/DisplayDev/Video/inputs/channels/<channelID>/picture | 391 | Chụp snapshot 1 nguồn — thumbnail preview trên panel nguồn | Dùng chính |
| 9.7.4.19 | Get the capability of configuring image position adjustment parameters of a signal source | GET | /ISAPI/DisplayDev/Video/inputs/channels/<channelID>/position/capabilities | 391 | Năng lực chỉnh vị trí hình | Tùy chọn / mở rộng |
| 9.7.4.20 | Set the custom resolution of a specified signal source | PUT | /ISAPI/DisplayDev/Video/inputs/channels/<channelID>/resolution | 392 | Đặt độ phân giải tùy chỉnh cho 1 nguồn | Tùy chọn / mở rộng |
| 9.7.4.21 | Get the capability of customizing the resolution of a specified signal source | GET | /ISAPI/DisplayDev/Video/inputs/channels/<channelID>/resolution/capabilities | 393 | Năng lực độ phân giải tùy chỉnh | Tùy chọn / mở rộng |
| 9.7.4.22 | Get the OSD configuration capability of a signal source | GET | /ISAPI/DisplayDev/Video/inputs/channels/<channelID>/text/capabilities | 394 | Năng lực OSD của nguồn | Tùy chọn / mở rộng |
| 9.7.4.23 | Get the video input capability | GET | /ISAPI/DisplayDev/Video/inputs/channels/capabilities | 395 | Năng lực input tổng | Tùy chọn / mở rộng |
| 9.7.4.24 | Get splicing configuration of all signal resources | GET | /ISAPI/DisplayDev/Video/inputs/joinSignal | 397 | Đọc cấu hình ghép (splicing) tất cả nguồn | Tùy chọn / mở rộng |
| 9.7.4.25 | Set jointing parameters of a specified signal source | PUT | /ISAPI/DisplayDev/Video/inputs/joinSignal/<channelID> | 398 | Đặt tham số ghép nhiều input thành 1 nguồn lớn | Tùy chọn / mở rộng |
| 9.7.4.26 | Get splicing parameters of a signal source | GET | /ISAPI/DisplayDev/Video/inputs/joinSignal/<channelID> | 400 | Đọc tham số ghép 1 nguồn | Tùy chọn / mở rộng |
| 9.7.4.27 | Get signal source splicing capability | GET | /ISAPI/DisplayDev/Video/inputs/joinSignal/capabilities | 401 | Năng lực ghép nguồn | Tùy chọn / mở rộng |
| 9.7.4.28 | Get all video streams' parameters | GET | /ISAPI/DisplayDev/Video/streaming/channels | 402 | Danh sách stream mạng (RTSP/camera IP) — NGOÀI PHẠM VI dự án (đã chốt mọi nguồn qua HDMI) | Ngoài phạm vi (nguồn chỉ HDMI) |
| 9.7.4.29 | Set all video stream parameters | PUT | /ISAPI/DisplayDev/Video/streaming/channels | 405 | Cấu hình tất cả stream mạng — ngoài phạm vi | Ngoài phạm vi (nguồn chỉ HDMI) |
| 9.7.4.30 | Set parameters of a specific video stream | PUT | /ISAPI/DisplayDev/Video/streaming/channels/<channelID> | 408 | Cấu hình 1 stream mạng — ngoài phạm vi | Ngoài phạm vi (nguồn chỉ HDMI) |
| 9.7.4.31 | Delete parameters of a specific video stream | DELETE | /ISAPI/DisplayDev/Video/streaming/channels/<channelID> | 411 | Xóa 1 stream mạng — ngoài phạm vi | Ngoài phạm vi (nguồn chỉ HDMI) |
| 9.7.4.32 | Get parameters of a specified video stream | GET | /ISAPI/DisplayDev/Video/streaming/channels/<channelID> | 412 | Chi tiết 1 stream mạng — ngoài phạm vi | Ngoài phạm vi (nguồn chỉ HDMI) |
| 9.7.4.33 | Get video stream capability | GET | /ISAPI/DisplayDev/Video/streaming/channels/capabilities | 415 | Năng lực stream mạng — ngoài phạm vi | Ngoài phạm vi (nguồn chỉ HDMI) |
| 9.7.4.34 | Get capability of searching for network input source parameters | GET | /ISAPI/DisplayDev/Video/streaming/channels/search/capabilities | 418 | Năng lực tìm kiếm nguồn mạng — ngoài phạm vi | Ngoài phạm vi (nguồn chỉ HDMI) |

## 5. VideoWall

| 9.7.5  Video Wall Management |  |  |  |  |  |  |
| --- | --- | --- | --- | --- | --- | --- |
| Quản lý tường màn hình: capability, cấu hình wall, gán output |  |  |  |  |  |  |
|  |  |  |  |  |  |  |
| Mục | Tên API (tài liệu) | Method | URL | Trang | Công dụng / Ghi chú dự án | Mức dùng |
| 9.7.5.1 | Get the capability of video wall controller | GET | /ISAPI/DisplayDev/capabilities | 418 | Capability toàn thiết bị — gọi ĐẦU TIÊN khi BE kết nối máy | Dùng chính |
| 9.7.5.2 | Get parameters of all video walls | GET | /ISAPI/DisplayDev/VideoWall | 422 | Cấu hình tất cả wall (lưới hàng×cột) — mỗi controller là 1 wall riêng biệt | Dùng chính |
| 9.7.5.3 | Set parameters of a specific video wall | PUT | /ISAPI/DisplayDev/VideoWall/<videoWallID> | 427 | Sửa 1 wall: đổi bố cục lưới, gán output vào ô — dùng khâu setup | Dùng chính |
| 9.7.5.4 | Get parameters of a specific video wall | GET | /ISAPI/DisplayDev/VideoWall/<videoWallID> | 432 | Đọc cấu hình 1 wall | Dùng chính |
| 9.7.5.5 | Get linked screen parameters of all outputs | GET | /ISAPI/DisplayDev/VideoWall/<videoWallID>/outputs | 433 | Output đã gán lên wall + vị trí ô — nguồn sự thật map SCR-xx ↔ ô lưới ↔ channelID | Dùng chính |
| 9.7.5.6 | Get video wall capabilities | GET | /ISAPI/DisplayDev/VideoWall/capabilities | 434 | Capability video wall: maxWallNums, maxWindowNums, baseOutputSize (=1920), isSupportRoam/Scene... | Dùng chính |

## 6. Plan

| 9.7.6  Video Wall Plan Management |  |  |  |  |  |  |
| --- | --- | --- | --- | --- | --- | --- |
| Quản lý plan — lịch trình tự động (đổi scene, bật/tắt màn theo giờ) |  |  |  |  |  |  |
|  |  |  |  |  |  |  |
| Mục | Tên API (tài liệu) | Method | URL | Trang | Công dụng / Ghi chú dự án | Mức dùng |
| 9.7.6.1 | Add a plan | POST | /ISAPI/DisplayDev/VideoWall/<videoWallID>/plan | 437 | Thêm plan — lịch tự động (activateScene / closeScreen / openScreen / switchBaseMap theo giờ) | Tùy chọn / mở rộng |
| 9.7.6.2 | Get configuration capability of a specific plan | GET | /ISAPI/DisplayDev/VideoWall/<videoWallID>/plan/<planTemplateID>/capabilities | 438 | Năng lực 1 plan template | Tùy chọn / mở rộng |
| 9.7.6.3 | Get plan configuration capability | GET | /ISAPI/DisplayDev/VideoWall/<videoWallID>/plan/capabilities | 439 | Năng lực plan (số plan tối đa) | Tùy chọn / mở rộng |
| 9.7.6.4 | Get the current plan | GET | /ISAPI/DisplayDev/VideoWall/<videoWallID>/plan/isRunning | 440 | Plan đang chạy | Tùy chọn / mở rộng |

## 7. Scene

| 9.7.7  Video Wall Scene Management |  |  |  |  |  |  |
| --- | --- | --- | --- | --- | --- | --- |
| Quản lý scene — kịch bản bố cục lưu trên thiết bị |  |  |  |  |  |  |
|  |  |  |  |  |  |  |
| Mục | Tên API (tài liệu) | Method | URL | Trang | Công dụng / Ghi chú dự án | Mức dùng |
| 9.7.7.1 | Get all scenes' parameters | GET | /ISAPI/DisplayDev/VideoWall/<videoWallID>/scene | 440 | Danh sách scene — CHỈ trả id + name, KHÔNG có bố cục (lý do dùng DB làm nguồn sự thật) | Tùy chọn / mở rộng |
| 9.7.7.2 | Set parameters of a specific scene | PUT | /ISAPI/DisplayDev/VideoWall/<videoWallID>/scene/<SID> | 441 | Sửa scene — chỉ sửa được id/name, không sửa được bố cục | Tùy chọn / mở rộng |
| 9.7.7.3 | Switch to a specific scene | PUT | /ISAPI/DisplayDev/VideoWall/<videoWallID>/scene/<SID>/activate | 441 | KÍCH HOẠT scene — chỉ gọi từ hành động "đưa lên tường" ở màn Monitor. Lỗi: inSceneSwitchingPleaseDoNotOperate | Tùy chọn / mở rộng |
| 9.7.7.4 | Save the current scene | PUT | /ISAPI/DisplayDev/VideoWall/<videoWallID>/scene/<SID>/saveData | 442 | Lưu bố cục ĐANG CHẠY trên tường vào scene — lệnh duy nhất ghi đè scene; nút "Áp dụng" không được gọi | Tùy chọn / mở rộng |
| 9.7.7.5 | Get scene configuration capability | GET | /ISAPI/DisplayDev/VideoWall/<videoWallID>/scene/capabilities | 443 | Capability scene: maxSceneNums, tên 1–10 ký tự, isSupportSceneInfo/Copy/Import/Export (desc chứa URL API mở rộng) | Tùy chọn / mở rộng |
| 9.7.7.6 | Get the current scene | GET | /ISAPI/DisplayDev/VideoWall/<videoWallID>/scene/isRunning | 444 | Scene đang chạy — highlight trên FE | Tùy chọn / mở rộng |
| 9.7.7.7 | Get scene control parameters capability | GET | /ISAPI/DisplayDev/VideoWallScene/SceneControlParams/capabilities?format=json | 444 | Năng lực tham số điều khiển scene | Tùy chọn / mở rộng |
| 9.7.7.8 | Get scene control parameters | GET | /ISAPI/DisplayDev/VideoWallScene/SceneControlParams?format=json | 445 | Đọc tham số điều khiển scene (JSON) | Tùy chọn / mở rộng |
| 9.7.7.9 | Set scene control parameters | PUT | /ISAPI/DisplayDev/VideoWallScene/SceneControlParams?format=json | 445 | Đặt tham số điều khiển scene (hành vi chuyển cảnh) | Tùy chọn / mở rộng |

## 8. Screen

| 9.7.8  Video Wall Screen Management |  |  |  |  |  |  |
| --- | --- | --- | --- | --- | --- | --- |
| Điều khiển màn hình (chỉ có lệnh tắt tất cả) |  |  |  |  |  |  |
|  |  |  |  |  |  |  |
| Mục | Tên API (tài liệu) | Method | URL | Trang | Công dụng / Ghi chú dự án | Mức dùng |
| 9.7.8.1 | Close all screens | PUT | /ISAPI/DisplayDev/ScreenCtrl/closeAll | 446 | TẮT tất cả màn hình — nút "Tắt tất cả". LƯU Ý: không có API mở-tất-cả hay bật/tắt từng màn (cần RS-232/485 riêng) | Dùng chính |

## 9. Text (LED)

| 9.7.9  Video Wall Text Management (Virtual LED) |  |  |  |  |  |  |
| --- | --- | --- | --- | --- | --- | --- |
| Chữ chạy / phụ đề ảo trên tường |  |  |  |  |  |  |
|  |  |  |  |  |  |  |
| Mục | Tên API (tài liệu) | Method | URL | Trang | Công dụng / Ghi chú dự án | Mức dùng |
| 9.7.9.1 | Set parameters of all virtual LEDs | PUT | /ISAPI/DisplayDev/VideoWall/<videoWallID>/virtualLED | 446 | Cấu hình tất cả virtual LED (chữ chạy) | Tùy chọn / mở rộng |
| 9.7.9.2 | Get parameters of all virtual LEDs | GET | /ISAPI/DisplayDev/VideoWall/<videoWallID>/virtualLED | 448 | Đọc tất cả virtual LED | Tùy chọn / mở rộng |
| 9.7.9.3 | Add all virtual LEDs | POST | /ISAPI/DisplayDev/VideoWall/<videoWallID>/virtualLED | 451 | Thêm virtual LED | Tùy chọn / mở rộng |
| 9.7.9.4 | Get parameters of a specified LED | GET | /ISAPI/DisplayDev/VideoWall/<videoWallID>/virtualLED/<SubtitlesID> | 453 | Đọc 1 virtual LED | Tùy chọn / mở rộng |
| 9.7.9.5 | Delete a specific virtual LED | DELETE | /ISAPI/DisplayDev/VideoWall/<videoWallID>/virtualLED/<SubtitlesID> | 456 | Xóa 1 virtual LED | Tùy chọn / mở rộng |
| 9.7.9.6 | Set parameters of a specific virtual LED | PUT | /ISAPI/DisplayDev/VideoWall/<videoWallID>/virtualLED/<SubtitlesID> | 456 | Sửa 1 virtual LED | Tùy chọn / mở rộng |
| 9.7.9.7 | Get the virtual LED configuration capability | GET | /ISAPI/DisplayDev/VideoWall/<videoWallID>/virtualLED/<SubtitlesID>/capabilities | 459 | Năng lực 1 virtual LED | Tùy chọn / mở rộng |
| 9.7.9.8 | Get configuration capability of all virtual LEDs | GET | /ISAPI/DisplayDev/VideoWall/<videoWallID>/virtualLED/capabilities | 461 | Năng lực tất cả virtual LED | Tùy chọn / mở rộng |

## 10. Wallpaper

| 9.7.10  Video Wall Wallpaper Management (Base Map) |  |  |  |  |  |  |
| --- | --- | --- | --- | --- | --- | --- |
| Ảnh nền tường |  |  |  |  |  |  |
|  |  |  |  |  |  |  |
| Mục | Tên API (tài liệu) | Method | URL | Trang | Công dụng / Ghi chú dự án | Mức dùng |
| 9.7.10.1 | Get configuration capability of background picture window | GET | /ISAPI/DisplayDev/VideoWall/<videoWallID>/baseMap/<mapFileID>/capabilities | 463 | Năng lực cửa sổ ảnh nền theo file | Tùy chọn / mở rộng |
| 9.7.10.2 | Get the capability of all background pictures | GET | /ISAPI/DisplayDev/VideoWall/<videoWallID>/baseMap/capabilities | 464 | Năng lực tất cả ảnh nền | Tùy chọn / mở rộng |
| 9.7.10.3 | Set parameters of all background pictures | PUT | /ISAPI/DisplayDev/VideoWall/baseMap | 465 | Cấu hình tất cả ảnh nền | Tùy chọn / mở rộng |
| 9.7.10.4 | Delete a specific background picture | DELETE | /ISAPI/DisplayDev/VideoWall/baseMap/<mapFileID> | 465 | Xóa 1 ảnh nền | Tùy chọn / mở rộng |
| 9.7.10.5 | Set parameters of a specific background picture | PUT | /ISAPI/DisplayDev/VideoWall/baseMap/<mapFileID> | 466 | Cấu hình 1 ảnh nền | Tùy chọn / mở rộng |
| 9.7.10.6 | Get parameters of a background picture | GET | /ISAPI/DisplayDev/VideoWall/baseMap/<mapFileID> | 467 | Đọc 1 ảnh nền | Tùy chọn / mở rộng |
| 9.7.10.7 | Get the background picture configuration capability | GET | /ISAPI/DisplayDev/VideoWall/baseMap/capabilities | 467 | Năng lực ảnh nền | Tùy chọn / mở rộng |
| 9.7.10.8 | Get configuration of all background pictures | GET | /ISAPI/DisplayDev/VideoWall/baseMap?isGetBaseMapFile=<isGetBaseMapFile> | 468 | Đọc cấu hình tất cả ảnh nền (kèm file nếu isGetBaseMapFile) | Tùy chọn / mở rộng |

## 11. Window

| 9.7.11  Video Wall Window Management |  |  |  |  |  |  |
| --- | --- | --- | --- | --- | --- | --- |
| Quản lý cửa sổ hiển thị — lõi thao tác kéo thả |  |  |  |  |  |  |
|  |  |  |  |  |  |  |
| Mục | Tên API (tài liệu) | Method | URL | Trang | Công dụng / Ghi chú dự án | Mức dùng |
| 9.7.11.1 | Get LED or LCD areas | GET | /ISAPI/DisplayDev/VideoWall/<videoWallID>/ledArea | 469 | Đọc vùng LED/LCD của tường hỗn hợp | Tùy chọn / mở rộng |
| 9.7.11.2 | Get all windows' parameters | GET | /ISAPI/DisplayDev/VideoWall/<videoWallID>/windows | 470 | Lấy TẤT CẢ window — vẽ lưới khi load, re-sync sau Áp dụng (gọi 4 máy rồi BE gộp) | Dùng chính |
| 9.7.11.3 | Delete all windows | DELETE | /ISAPI/DisplayDev/VideoWall/<videoWallID>/windows | 474 | Xóa tất cả window — clear vùng của 1 máy | Dùng chính |
| 9.7.11.4 | Add a window | POST | /ISAPI/DisplayDev/VideoWall/<videoWallID>/windows | 475 | MỞ window mới — Rect theo hệ uniformCoordinate (1 ô = 1920). Lỗi cần bắt: windowsAmountExceedLimitInSingleOutput/Screen | Dùng chính |
| 9.7.11.5 | Get parameters configuration of a specific window | GET | /ISAPI/DisplayDev/VideoWall/<videoWallID>/windows/<VWMWID> | 480 | Đọc 1 window — drill-down/refresh đơn lẻ | Dùng chính |
| 9.7.11.6 | Set parameters of a specific window | PUT | /ISAPI/DisplayDev/VideoWall/<videoWallID>/windows/<VWMWID> | 484 | SỬA window: move/resize/đổi nguồn — nút "Áp dụng" thao tác khẩn cấp (không đụng scene) | Dùng chính |
| 9.7.11.7 | Delete a specific window | DELETE | /ISAPI/DisplayDev/VideoWall/<videoWallID>/windows/<VWMWID> | 489 | Xóa 1 window | Dùng chính |
| 9.7.11.8 | Bottom the window | PUT | /ISAPI/DisplayDev/VideoWall/<videoWallID>/windows/<VWMWID>/bottom | 490 | Đưa window xuống DƯỚI CÙNG (z-order — không set số tùy ý) | Dùng chính |
| 9.7.11.9 | Get single configuration capabilities of sub-windows | GET | /ISAPI/DisplayDev/VideoWall/<videoWallID>/windows/<VWMWID>/sub/<VWSWID>/capabilities | 490 | Năng lực sub-window (chế độ chia 1/4/9/16...) | Tùy chọn / mở rộng |
| 9.7.11.10 | Get parameters of decoding delay | GET | /ISAPI/DisplayDev/VideoWall/<videoWallID>/windows/<VWMWID>/sub/<VWSWID>/decodeDelay | 494 | Đọc decode delay của sub-window | Tùy chọn / mở rộng |
| 9.7.11.11 | Get decoding delay capability | GET | /ISAPI/DisplayDev/VideoWall/<videoWallID>/windows/<VWMWID>/sub/<VWSWID>/decodeDelay/capabilities | 494 | Năng lực decode delay | Tùy chọn / mở rộng |
| 9.7.11.12 | Get the configuration capability of full-frame-rate fluent video mode | GET | /ISAPI/DisplayDev/VideoWall/<videoWallID>/windows/<VWMWID>/sub/<VWSWID>/fullFrame/capabilities | 494 | Năng lực chế độ video mượt full-frame-rate | Tùy chọn / mở rộng |
| 9.7.11.13 | Top the window | PUT | /ISAPI/DisplayDev/VideoWall/<videoWallID>/windows/<VWMWID>/top | 495 | Đưa window lên TRÊN CÙNG (z-order) | Dùng chính |
| 9.7.11.14 | Get the window configuration capability of the video wall | GET | /ISAPI/DisplayDev/VideoWall/<videoWallID>/windows/capabilities | 495 | Capability windows: windowMode, isSupportWinTopBottom. Lỗi đa client: multipleVideowallClientConflict 0x4000A4F8 | Dùng chính |
| 9.7.11.15 | Get the parameters configuration capability of sub-stream in multi-screen mode | GET | /ISAPI/DisplayDev/VideoWall/<videoWallID>/windows/subSteam/capabilities?format=json | 497 | Năng lực sub-stream chế độ nhiều màn (JSON) | Tùy chọn / mở rộng |
| 9.7.11.16 | Get the configuration parameters of the stream type for streaming when the number of windows | GET | /ISAPI/DisplayDev/VideoWall/<videoWallID>/windows/subSteam?format=json | 498 | Đọc cấu hình loại stream khi số window lớn (auto main/sub) | Tùy chọn / mở rộng |
| 9.7.11.17 | Get the pre-editing capability of video wall | GET | /ISAPI/DisplayDev/VideoWall/preEdit/capabilities?format=json | 498 | Năng lực pre-editing của tường (JSON) | Tùy chọn / mở rộng |


