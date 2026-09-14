# So sánh: FE (Vue, TA-ITS015-WEBVUE-V1.0) có đáp ứng đủ như WPF (ITS.VideoWall.WPF) chưa

> Ngày lập: 2026-09-14. Khảo sát bằng Explore agent, đọc thật `src/src/views/videoWall/**` +
> `services/videoWall/**` + `api-services/videoWall/**` bên FE, đối chiếu với bộ tính năng WPF đã
> khảo sát đầy đủ trong cùng phiên làm việc (công cụ chẩn đoán thao tác ISAPI trực tiếp cho kỹ sư
> field).

## Kết luận chung
FE khá đầy đủ cho vận hành thường ngày (thậm chí một số chỗ trực quan hơn WPF), nhưng còn 4 khoảng
trống thật cần lưu ý.

## FE ĐÃ CÓ (một số chỗ TỐT HƠN WPF)

- **Controllers/Sources/Screens/Outputs**: CRUD đầy đủ + "Output Map" (gán màn hình lên port bằng
  click trực quan) — tương đương "Screen assignment CRUD" của WPF.
- **Scenes**: trình soạn scene 3 cột — canvas kéo-thả (thêm/di chuyển/resize/xoá cửa sổ, khoá ô, kéo
  nguồn vào cell) — **trực quan hơn WPF** (WPF chủ yếu form nhập tay + vài template cứng).
- **Monitor**: xem trước 1 scene, nút "Apply" đẩy lên chạy thật, thanh trạng thái hiện scene đang
  chạy + số màn ON — tương đương "Activate scene" của WPF.
- **Schedule** (CRUD, có Cron) và **ITS Integration/EventRule** (CRUD + nút "Test" tự kích hoạt) —
  tương đương `VwSchedule`/`VwEventRule`.
- **Dashboard**: thẻ tổng quan trạng thái online/offline từng controller.
- Có subscribe NATS thật để tự động cập nhật Monitor khi scene đổi (không phải polling).

## ❌ Khoảng trống #1 — Không có nhóm DeviceSetup (Ping/Probe/SyncSources/ResetCircuitBreaker/ISAPI passthrough)

Grep toàn bộ mã nguồn FE cho `Probe|SyncSources|SetupScene|ResetCircuitBreaker|CircuitBreaker|
DeviceSetup` → **0 kết quả** trong code VideoWall. FE chỉ gọi CRUD chuẩn + `vwscene/active`/
`activeregions`. Không có cách nào từ FE để: Ping kiểm tra controller còn sống, chạy Probe khám phá
năng lực/tường mới, chạy SyncSources để tự map input channel, hay Reset Circuit Breaker khi controller
bị khoá do đăng nhập sai — tất cả các thao tác này hiện chỉ làm được qua WPF.

## ❌ Khoảng trống #2 — Subject NATS FE lắng nghe có vẻ SAI/CŨ, real-time có thể đang không hoạt động

FE lắng nghe `NatsSubject.DataVideoWallScene = "ta.its.data.videowall.scene"` — nhưng subject THẬT
hiện tại bên Worker/BE (đã chốt tên trong phiên làm việc backend) là `VwSubjects.Data =
"ta.its.data.videowall"` (KHÔNG có hậu tố `.scene`). Nếu đúng vậy, FE đang subscribe nhầm kênh —
tính năng "Monitor tự chuyển scene khi có người đổi ở nơi khác" có thể đang không nhận được gì. Đây
chính là lý do prompt `videowall-subject-rename-fe-prompt.md` (đã viết từ trước, còn đang chờ, CHƯA
thực thi) tồn tại — nên ưu tiên làm trước để chốt lại tính năng real-time.

## ❌ Khoảng trống #3 — Trang Schedule của FE hiện không có tác dụng thật

Đối chiếu với `Vw_Entities_WritePath_Audit_2026-09-14.md` (cùng thư mục doc): `VwSchedule` có CRUD
đầy đủ ở BE nhưng KHÔNG có bất kỳ scheduler/job nào đọc bảng này để tự kích hoạt scene đúng giờ. FE
có UI Cron đầy đủ, người dùng tạo lịch được, nhưng lịch đó sẽ không bao giờ tự chạy — cần làm phần
"động cơ" thực thi ở BE trước thì trang Schedule của FE mới thật sự có tác dụng.

## ❌ Khoảng trống #4 — Không có hiển thị phân quyền theo Ô LƯỚI (chỉ có theo Controller)

FE chỉ có `useVideoWallScope` (phạm vi theo CONTROLLER: "all"/"assigned" + danh sách controller được
phép) — làm canvas Scene readonly nếu ngoài phạm vi. Không có hiển thị/áp theo TỪNG Ô LƯỚI như
`VwWallPermission.Config` đã thiết kế (Tầng 3). Gap này đã biết từ trước, prompt
`videowall-fe-grid-permission-visualization-prompt.md` đã viết sẵn, còn đang chờ thực thi.

## Việc cần làm tiếp theo (chưa quyết định, cần người dùng chọn)

1. Ưu tiên thực thi 2 prompt FE cũ đang chờ: đổi subject NATS + hiển thị phân quyền ô lưới.
2. Có viết thêm prompt cho FE gọi được nhóm DeviceSetup (Ping/Probe/SyncSources/ResetCircuitBreaker)
   không?
3. Có làm phần scheduler cho `VwSchedule` trước khi tiếp tục đầu tư UI Schedule không?
