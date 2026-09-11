---
name: videowall-outstanding-issues-20260911
description: Punch list of unresolved bugs/gaps found while reviewing VideoWall permission CRUD + NATS device-access redesign, as of 2026-09-11 — check status before assuming fixed
metadata:
  type: project
---

Danh sách các vấn đề CHƯA XỬ LÝ tại thời điểm 2026-09-11 (cuối phiên review), phát hiện khi rà
soát [[videowall-userarea-permission-model]] và [[videowall-nats-device-access-redesign]]. Kiểm
tra lại code thật trước khi tin memory này còn đúng — có thể đã được sửa ở phiên sau.

1. **`VwUserAreaPermissionCommandHandler.HandleAsync(VwUpdateUserAreaPermissionInput)` không
   duplicate-check actor** (`Module.VideoWall/Controllers/UserAreaPermission/Commands/`) — Update
   cho phép đổi `UserId`/`OrgId` của 1 bản ghi sang actor đã có bản ghi khác đang active, không
   báo lỗi trùng → có thể tồn tại ≥2 bản ghi active cùng actor, `GetFirstAsync` (đọc, không
   `OrderBy`) lấy bản ghi không xác định → cấp/thu hồi quyền sai lệch âm thầm.
2. **`UserId`/`OrgId` không được `.Trim()` trước khi so sánh/lưu** (cùng file, `HandleAsync(Add)`)
   — chuỗi toàn khoảng trắng (`" "`) lọt qua check "có UserId" nhưng không khớp
   `IsNullOrEmpty`/`string.Empty` dùng ở nơi khác (duplicate-check nhánh else, fallback đọc ở
   `VwUserAreaAccessService`) → tạo được bản ghi "ma": không bị phát hiện trùng, không bao giờ
   được áp dụng khi enforce quyền.
3. **`VwUserAreaPermissionController` chưa có trong checklist gỡ `[AllowAnonymous]` trước
   production** ở `VwOrgAccessService.cs` (docstring dòng ~29-42, hiện liệt kê đủ 10 controller,
   thiếu controller này) — phần ĐỌC (Page/GetList/GetById) hiện hoàn toàn public.
4. **Checklist gỡ `[AllowAnonymous]` ở `VwDeviceController.cs`/`VwDeviceSetupController.cs` đã lỗi
   thời** — vẫn ghi phải "mở lại `_orgAccess`" nhưng logic ISAPI (và `_orgAccess` liên quan) đã
   chuyển hẳn sang Worker (`ITS.VideoWall`) theo redesign NATS. Chưa rõ phân quyền theo org cho 29
   endpoint (wizard + diagnostic) này còn được enforce ở đâu sau khi chuyển — **cần làm rõ trước
   khi thật sự gỡ `[AllowAnonymous]` ở production**, rủi ro hở quyền nếu bỏ qua.
5. **`IVwDeviceProxyDispatcher` DI registration chưa verify bằng build+run thật** — Worker resolve
   qua `sp.GetService<IVwDeviceProxyDispatcher>()` (nullable, service-locator trong
   `VwCommandConsumer`). Nếu Furion không tự convention-scan đúng 4 class mới trong
   `ITS.VideoWall/Consumer/DeviceProxy/`, dispatcher sẽ luôn `null` → cả 23 action diagnostic sẽ
   luôn báo "Unsupported action" dù code đọc tĩnh hoàn toàn đúng. Chỉ đọc code không loại trừ được
   rủi ro này — cần chạy thử thật.
6. **`ITS.VideoWall.Worker/appsettings.json` thiếu khai subject `.scene` trong `Nats.Streams`**
   (WebAPI's `DataTransporter.json` có, Worker thiếu) — không gây lỗi runtime (fallback pubsub mặc
   định trùng khớp) nhưng nên bổ sung cho tường minh.
7. **Tính năng "heartbeat định kỳ" (Worker tự poll trạng thái thiết bị mỗi N giây, ghi log/CSDL,
   bắn NATS cho FE) mô tả trong biên bản họp CHƯA được implement** — đã grep xác nhận (2 lần,
   cách nhau) không có `PeriodicTimer`/heartbeat/polling nào trong `src/Services/VideoWall/`, và
   không có bảng CSDL nào cho trạng thái runtime (chỉ có `VwController.Status` — cờ Enable/Disable
   admin set tay, và `VwEventTriggerLog` — log append-only theo hành động, không phải theo định
   kỳ thời gian). Nếu người dùng hỏi lại "tính năng heartbeat sao rồi", đây vẫn là tính năng MỚI
   hoàn toàn cần thiết kế từ đầu, chưa có gì để sửa.
