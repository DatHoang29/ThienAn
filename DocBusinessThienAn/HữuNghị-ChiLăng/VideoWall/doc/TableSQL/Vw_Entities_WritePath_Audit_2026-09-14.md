# Audit: 12 Entity VideoWall — Vai trò & Write Path

> Ngày lập: 2026-09-14. Kiểm tra toàn bộ 12 entity trong
> `src/Modules/VideoWall/Module.VideoWall.Core/Entities/` — vai trò từng entity là gì, và có write
> path (Insert/Update) thật ở đâu (BE `Module.VideoWall` và/hoặc Worker `ITS.VideoWall`). Thực hiện
> bằng 2 Explore agent grep toàn bộ codebase, không suy đoán.

## Bảng tổng hợp

| Entity | Vai trò | Ghi từ đâu |
|---|---|---|
| `VwController` | Bộ điều khiển vật lý (Hikvision DS-C66S). Có `Role` ("center"/"sub"), IP/tài khoản Digest, `OrgId` sở hữu, `ActiveSceneId`/`ActiveSceneAt` (đang chạy scene nào), 5 cột năng lực (chỉ ở bộ trung tâm). | CRUD web + Worker (Probe/ActivateScene/Heartbeat) — dùng nhiều nhất. |
| `VwScreen` | 1 màn hình LED vật lý trong tường, gắn 1 output port của controller, có toạ độ lưới (GridCol/GridRow) + trạng thái tín hiệu. | CRUD web + Worker (Heartbeat). |
| `VwSource` | 1 nguồn tín hiệu đầu vào (camera HDMI vật lý hoặc luồng IP), có SignalNo/SignalStatus khớp input channel thật. | CRUD web + Worker (Probe/SyncSources/Heartbeat). |
| `VwSlotPort` | 1 cổng output vật lý trên card mở rộng của controller (thuộc slot nào, cổng số mấy). | CRUD web + Worker (`ProbeCore`). |
| `VwControllerSlot` | 1 slot/card cắm trên khung controller (card Input hoặc Output) — tồn kho cấp CARD, khác `VwSlotPort` (tồn kho cấp CỔNG). | CHỈ từ Worker (`ProbeCore`), không có CRUD web. Trước đó từng là gap đã fix xong, giờ ghi đúng. |
| `VwScene` | 1 kịch bản/bố cục hiển thị trên tường, cờ `ActiveScene` đánh dấu đang chạy, gắn `ControllerId` sở hữu (rỗng = kịch bản toàn tường). | CRUD web + Worker (flip cờ `ActiveScene` lúc activate/đồng bộ). |
| `VwWindowScene` | 1 cửa sổ cụ thể BÊN TRONG 1 Scene — toạ độ (X,Y,W,H), đang chiếu nguồn nào, `DeviceWindowId` khớp ID thật trên thiết bị. | CRUD web + Worker (gán `DeviceWindowId` sau khi tạo cửa sổ trên thiết bị). |
| `VwWallTopology` | Cấu hình tổng thể 1 tường (số hàng/cột lưới, kích thước panel) — cơ sở tính toạ độ cho Screen/WindowScene/phân quyền Tầng 3. | CRUD web only (upsert đúng 1 dòng duy nhất). |
| `VwEventRule` | Quy tắc "khi sự kiện loại X xảy ra thì tự động kích hoạt Scene Y" — tự động hoá theo sự kiện. | CRUD web only. |
| `VwSchedule` | Lịch phát Scene tự động theo giờ — tự động hoá theo thời gian. | CRUD web only — **xem cảnh báo bên dưới**. |
| `VwEventTriggerLog` | Nhật ký audit APPEND-ONLY — ghi từng bước ISAPI đã chạy (DeviceSetup) hoặc lịch sử kích hoạt (EventRule/Schedule/thao tác tay). KHÔNG phản ánh trạng thái hiện tại, chỉ để xem lại/debug. | Insert-only cả 2 phía (BE + Worker) — ĐÚNG thiết kế, xác nhận KHÔNG có `Update` nào đụng bảng này. |
| `VwWallPermission` | Bảng phân quyền Tầng 3 (theo User hoặc Org) — quy định User/Org được thao tác đúng ô lưới nào trên tường (dùng `VwWallTopology` làm hệ quy chiếu). | CRUD web only (giới hạn SuperAdmin). |

## Kết luận chung

**12/12 entity đều có write path thật** — không có entity nào bị bỏ hoang hoàn toàn (viết ra rồi
không ai ghi).

## ⚠️ Cảnh báo: `VwSchedule` — ghi được, đọc lại được, nhưng KHÔNG AI THỰC THI

Đây không phải "entity chết vì không ghi được", mà là dạng khác: có đủ CRUD (Add/Update/Delete qua
API, kể cả FE đã có UI Cron), nhưng đã grep toàn bộ repo — **không có Hangfire job / BackgroundService
/ IHostedService / timer nào đọc bảng `VwSchedule` rồi tự kích hoạt scene đúng giờ đã đặt**. Worker
chỉ đăng ký đúng 2 HostedService (`VwCommandConsumer`, `VwDeviceHeartbeatService`), không đụng tới
`VwSchedule`. Doc comment trong `ScheduleViewModel.cs` (WPF) cũng tự ghi nhận: *"Tab Lịch: CRUD thuần
trên VwSchedule"*.

**Hệ quả**: người dùng có thể tạo lịch qua giao diện (cả BE lẫn FE), nhưng lịch đó không bao giờ tự
chạy. Tính năng "lên lịch chiếu scene tự động" hiện chưa hoàn thiện — thiếu hẳn phần "động cơ"
(scheduler engine) đọc và thực thi `VwSchedule` đúng giờ.

Muốn hoàn thiện tính năng này cần quyết định trước: chạy bằng cơ chế nào (Hangfire đã có sẵn trong dự
án, hay BackgroundService tự viết), tần suất poll, xử lý múi giờ, xử lý trùng giờ nhiều lịch — đây là
việc làm MỚI (thêm engine), không phải fix bug nhỏ.
