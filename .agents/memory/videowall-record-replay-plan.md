---
name: videowall-record-replay-plan
description: VideoWall WPF tool — Live mode + continuous auto-log + Scenarios architecture
metadata:
  type: project
---

Công cụ `Module.VideoWall.WPF` đã được **đơn giản hoá**: bỏ Record/Replay/Tape, tập trung vào chế độ **Trực tiếp (Live) + Tự động ghi Log liên tục ra file + Kịch bản**:

**Đã triển khai:**
- **Live & Direct Pipeline**: `VwDirectClientFactory` thiết lập chuỗi kết nối trực tiếp `VwDirectDigestHandler -> HttpClientHandler`, hỗ trợ xác thực Digest Auth tự động cho thiết bị phần cứng (DS-C30S-S11).
- **Tự động ghi Log liên tục (Auto Session Logging)**: Trong `MainViewModel`, mỗi thao tác gửi/nhận hay sự kiện được tự động append vào file `%LOCALAPPDATA%\Module.VideoWall.WPF\Logs\session_{yyyyMMdd_HHmmss}.jsonl`. Nút "Xuất log" giữ nguyên để xuất snapshot gộp khi cần.
- **Kịch bản (Scenario)**: Tab Kịch bản cho phép cấu hình chuỗi gọi nhiều API liên tiếp, chỉnh `DelayBetweenStepsMs`, lưu kịch bản, và "Chạy tiếp từ bước N (Resume)".
- **UI Responsive**: Header kết nối Row 1 và thanh công cụ Kịch bản được bọc trong `WrapPanel` tự động ngắt dòng khi cửa sổ co nhỏ. Tab Thiết lập Scene phân tách dọc Hàng 2 (Dựng cửa sổ) và Hàng 3 (Cửa sổ đã lưu) với `GridSplitter`.
- **Tài liệu**:
  - `videowall-record-replay.md` — kiến trúc Live + auto-log + Kịch bản.
  - `videowall-test-local.md` — chạy test tự động và MockServer riêng `scripts/VwMockServerRunner`.
  - `videowall-test-2ngay.md` — checklist kiểm thử 2 ngày tại hiện trường.

