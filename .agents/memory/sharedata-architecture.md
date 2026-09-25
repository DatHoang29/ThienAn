---
type: project
created: 2026-09-25
updated: 2026-09-25
---

# ShareData Architecture & Workers

## 1. Tách biệt 2 Worker
- `DataChangePollingWorker`: Polling định kỳ SQL Server Change Tracking (mỗi 1s). Khi phát hiện bảng nguồn thay đổi, ánh xạ sang PacketCode và xuất bản vào NATS subject duy nhất `ta.its.event.sharedata.newdata` với payload:
  `{ "PacketCode": "103", "Type": "103", "Version": 12345, "TriggeredAt": "2026-09-25T..." }`.
- `DataNatsConsumerWorker`: Đăng ký NATS subject `ta.its.event.sharedata.newdata`, parse PacketCode/Type và gọi `DataOutboundService.ProcessTriggerSubscriptions` để kích hoạt xuất bản các subscription có `SendOnNewData = true`.

## 2. Race condition & Trễ dữ liệu (Đã kiểm thử toàn trình)
- Kịch bản: Đợt 1 chỉ thấy Xe A và Xe B (mốc checkpoint dừng tại Xe B). Trong lúc đợt 1 đang chạy hoặc vừa xong, Xe C mới được commit vào CSDL.
- Kết quả kiểm chứng (Test: `ChangeTracking_WhenNewDataCommittedAfterFirstBatch_SubsequentRunPicksUpRemainingData_Test`):
  - Lượt 1: Gửi Xe A và B, Checkpoint LastTime dừng ở Xe B.
  - Lượt 2: Quét tiếp nối đuôi từ cursor của Xe B, gửi tiếp đúng Xe C (RecordCount = 1). Không trùng lặp A, B và không sót bản ghi C.
