---
name: videowall-nats-device-access-redesign
description: VideoWall Worker now uses TransportManager (matching VDS pattern) not Furion/ItsDataTransporter; ALL WebAPI device-ISAPI access (wizard + 23 diagnostic endpoints) now goes through NATS request-reply, removing backward ProjectReference
metadata:
  type: project
---

Trong repo `TA-ITS015-WEBAPI-V1.0`, module VideoWall (`Module.VideoWall` = WebAPI,
`src/Services/VideoWall/ITS.VideoWall` + `ITS.VideoWall.Worker` = Worker Service riêng) đã trải
qua 2 lượt sửa kiến trúc NATS lớn trong phiên 2026-09-11, cả 2 đã implement và đã review kỹ:

1. **Đồng bộ transport với VDS**: Worker đổi từ `ItsDataTransporter` (API tĩnh, kéo theo Furion +
   `Modules.DataTransporter.ITS`) sang `TransportManager` (`Services.Shared.Runtime`, nhẹ, không
   Furion) — đúng pattern `ITS.VDS.Core` đã dùng. Đồng thời đổi tên subject
   `ta.its.data.videowall.cmd`/`.telemetry` → `.request`/`.response` (dễ hiểu hơn theo yêu cầu
   người dùng) ở CẢ 2 phía (WebAPI `ItsDataTransporterChannel.VideoWallRequest/Response`, Worker
   `VwSubjects.Request/Response`). Subject `.scene` (`VwSubjects.SceneData`, broadcast 1 chiều
   xuống FE) GIỮ NGUYÊN tên — không phải cặp request/response nên không đổi.

2. **NATS-hoá toàn bộ truy cập thiết bị từ WebAPI** (việc lớn hơn nhiều lần ước tính ban đầu —
   xem [[feedback-verify-root-cause-before-fix]]): WebAPI trước đó có `ProjectReference` NGƯỢC
   tới Worker (`Module.VideoWall.csproj` → `ITS.VideoWall.csproj`) chỉ để DI 4 class ISAPI cụ thể
   — nguyên nhân là 2 nhóm code gọi thẳng thiết bị không qua NATS: (a) Device Setup wizard (6
   thao tác: Ping/Probe/SyncSources/SetupScene/SendPassthrough/ResetCircuitBreaker), (b)
   "Diagnostic proxy" 23 endpoint (`VwDeviceController`, cố ý `[AllowAnonymous]` để test qua
   Swagger/Postman tại hiện trường — xem docstring gốc). Người dùng xác nhận: cả 2 nhóm chuyển
   hết qua NATS, vì việc chẩn đoán hiện trường trực tiếp sẽ chuyển sang dùng **WPF client riêng**
   thay cho Swagger vào WebAPI.

   Kết quả: thêm hạ tầng request-reply mới (`IVwNatsRequestClient`/`IVwNatsReplySink` +
   `VwNatsRequestClient`, singleton dùng chung — registry theo `MessageId`, KHÔNG tạo subscription
   NATS thứ 2, chỉ nhận reply qua `VwTelemetryConsumer` gọi vào) + 29 `VwCommandActions` mới (6
   DeviceSetup* + 23 DeviceProxy*) + dispatcher ở Worker (`ITS.VideoWall/Consumer/DeviceProxy/`).
   Sau đó **xoá hẳn** `ProjectReference` ngược + 5 dòng DI ISAPI cũ khỏi `Module.VideoWall` — đã
   verify bằng review code thật (không chỉ đọc plan), không còn tham chiếu nào.

**Why:** Lý do tách Service khỏi WebAPI (deadlock/thread-starvation khi thiết bị treo) đã chốt từ
biên bản họp 11/09; lý do dùng `TransportManager` thay `ItsDataTransporter` là vì lợi ích dual
fan-out NATS+SocketCluster của `ItsDataTransporter` không dùng tới (SocketCluster đang tắt ở mọi
config) — chỉ tốn phụ thuộc Furion không cần thiết cho 1 process Worker độc lập.

**How to apply:** Coi 2 việc trên là ĐÃ XONG và ĐÃ ĐÚNG (đã review kỹ, không bug nghiêm trọng ở
các điểm rủi ro cao: race condition, memory leak, double-subscription, nhầm tên action). Plan chi
tiết: `DocBusinessThienAn/HữuNghị-ChiLăng/Plan/videowall-worker-nats-sync-plan.md` (việc 1) và
`videowall-device-nats-full-plan.md` (việc 2). Còn vài điểm CHƯA verify được bằng đọc code tĩnh —
xem [[videowall-outstanding-issues-20260911]].
