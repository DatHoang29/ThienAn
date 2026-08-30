---
name: videowall-record-replay-plan
description: VideoWall WPF tool — Live mode + continuous auto-log + Scenarios architecture
metadata:
  type: project
---

Công cụ `Module.VideoWall.WPF` đã được **đơn giản hoá**: bỏ Record/Replay/Tape, tập trung vào chế độ **Trực tiếp (Live) + Tự động ghi Log liên tục ra file + Kịch bản**:

**Đã triển khai:**
- **Live & Direct Pipeline**: `VwDirectClientFactory` thiết lập chuỗi kết nối trực tiếp `VwDirectDigestHandler -> SocketsHttpHandler`, hỗ trợ xác thực Digest Auth tự động cho thiết bị phần cứng (DS-C30S-S11). Bỏ hoàn toàn Backend Mode, chỉ dùng Direct Mode.
- **Tự động ghi Log liên tục (Auto Session Logging)**: Trong `MainViewModel`, mỗi thao tác gửi/nhận hay sự kiện được tự động append vào file `%LOCALAPPDATA%\Module.VideoWall.WPF\Logs\session_{yyyyMMdd_HHmmss}.jsonl`. Nút "Xuất log" giữ nguyên để xuất snapshot gộp khi cần.
- **Local Storage**: `VwLocalScreenStore` và `VwLocalSceneStore` lưu cấu hình màn hình và kịch bản độc lập theo `deviceKey` tại local.
- **Kịch bản (Scenario)**: Tab Kịch bản cho phép cấu hình chuỗi gọi nhiều API liên tiếp, chỉnh `DelayBetweenStepsMs`, lưu kịch bản, và "▶ Chạy tiếp từ bước #N (Resume)". Bộ kiểm thử lỗi hỗ trợ công tắc "Gửi thật để xem mã lỗi thiết bị".
- **Khung Response Động**: Khung Response toàn màn hình tự động hiển thị ở Tab 3..13 ISAPI và tự động ẩn ở Tab 1 & Tab 2, thanh `GridSplitter` chính và phụ kéo thả trơn tru không sinh mảng trắng.
- **Tài liệu**: Danh mục tài liệu chuẩn và hướng dẫn kiểm thử được quản lý duy nhất tại [`DocBusinessThienAn/HữuNghị-ChiLăng/VideoWall/README.md`](../DocBusinessThienAn/HữuNghị-ChiLăng/VideoWall/README.md).

