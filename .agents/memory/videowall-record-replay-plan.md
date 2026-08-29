---
name: videowall-record-replay-plan
description: VideoWall WPF tool — Record/Replay mechanism and offline test harness implemented & verified
metadata:
  type: project
---

Chế độ **Ghi / Phát lại (Record/Replay)** cho công cụ `Module.VideoWall.WPF` đã **hoàn thành và kiểm thử toàn diện ngày 2026-08-29**.

**Đã triển khai:**
- **Lõi Record/Replay**: `VwDeviceIoMode` (Live, Record, Replay), `VwTape`, `VwTapeEntry`, `VwTapeStore` (hỗ trợ streaming JSONL append, save/load JSON và export log), `VwReplayHandler` (DelegatingHandler tự động tee ghi khi Record, bắt request và trả canned responses khi Replay, log Warning khi missing entry), `VwDirectClientFactory` gộp pipeline handler `VwDirectDigestHandler -> VwReplayHandler -> HttpClientHandler`.
- **UI WPF**: Row 1 MainWindow thêm RadioButtons chọn Live/Record/Replay, chọn file Tape, hiển thị cảnh báo banner vàng khi đang ở chế độ Replay, nút "Lưu thành tape" từ ActivityLog.
- **Harness & Fixture**: `SampleData/sample-tape.json` chứa specs DS-C30S-S11, test suite `tests/Modules/VideoWall/Wpf/VwReplayHandlerTests.cs` (5 bài test kiểm tra probe, add window, 404 missing entry, guardrail max windows, tape serialization). Toàn bộ 65 bài test WPF chạy xanh.
- **Tài liệu** (`DocBusinessThienAn/HữuNghị-ChiLăng/VideoWall/`, đã gom & chia khu 2026-08-29):
  - `README.md` — chỉ mục "chia khu": Khu 1 reference thiết bị/ISAPI, Khu 2 công cụ WPF.
  - `videowall-record-replay.md` — **tài liệu chính** (đã gộp bản tổng hợp cũ + runbook cũ thành 1 file). 5 phần: A bối cảnh & quyết định · B cơ chế & thiết kế ("tape là gì", 3 chế độ, handler chain) · C runbook vận hành · D bảng phạm vi offline vs tại chỗ · E kiểm thử tự động.
  - `videowall-test-2ngay.md` — checklist tick-box đi test 2 ngày tại TCB (Ngày 1 / Ngày 2 / "thế nào là đủ" 6 tiêu chí).
  - `videowall_plan.md`, `transcript-videowall-28082026.md` — bản gốc lịch sử, không sửa.

