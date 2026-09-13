# Prompt: Đổi subject NATS VideoWall (Frontend) — scene → data (không phải status)

> Ngày lập: 2026-09-12, **sửa lại 2026-09-13**. FE hiện đang nghe subject CŨ
> `ta.its.data.videowall.scene` — đây là **bug thật đang tồn tại**: BE/Worker đã đổi cách publish
> từ lâu (không còn dùng `.scene` nữa), nên FE hiện tại **không nhận được bất kỳ cập nhật realtime
> nào cho VideoWall cả**. Bản đầu của prompt này (2026-09-12) định đổi đích sang `.status` — SAI,
> đã đính chính lại theo quyết định kiến trúc mới nhất (xem
> `videowall-eliminate-worker-to-be-nats-reply-prompt.md`, mục "ĐÍNH CHÍNH quan trọng"): subject
> `Status` bị bỏ hoàn toàn, `Data` thay thế nó làm kênh Worker→FE duy nhất.
>
> **Đổi đích cuối cùng**: `ta.its.data.videowall.scene` → **`ta.its.data.videowall.data`**.
>
> **Phụ thuộc**: phải làm ĐỒNG THỜI với `videowall-eliminate-worker-to-be-nats-reply-prompt.md`
> (phần BE/Worker) — 2 bên phải cùng nói chuyện qua đúng 1 subject `Data`, không thể deploy lệch
> pha (FE đổi trước mà BE chưa đổi, hoặc ngược lại, đều mất realtime).

## Bối cảnh

FE chỉ subscribe đúng 1 trong 2 subject VideoWall còn tồn tại sau khi dọn kiến trúc — subject
`Data` (Worker→FE trực tiếp, KHÔNG qua BE). Subject còn lại (`Control`) là nội bộ BE→Worker, FE
không đụng tới.

**Payload trên `Data` giờ đa dạng hơn trước** (không chỉ còn mỗi thông báo scene) — mọi sự kiện
(scene activated, cảnh báo phần cứng `HardwareOutOfSync`, heartbeat định kỳ, kết quả DeviceSetup
Probe/SyncSources...) đều đi chung 1 subject này, phân biệt bằng field **`EventType`** mới (xem
prompt kiến trúc lớn để biết đầy đủ danh sách `EventType` — ví dụ `"SceneActivated"`,
`"HardwareOutOfSync"`, `"DeviceHeartbeat"`, `"DeviceProbeCompleted"`). FE PHẢI đọc `EventType` để
rẽ nhánh xử lý đúng, không còn suy đoán qua sự có/vắng mặt của field khác như trước.

---

## File cần sửa — Frontend (`TA-ITS015-WEBVUE-V1.0`, branch `dev`)

### 1. `src/public/Configuration/Nats.subjects.json`
Dòng 44 (object có `"subject": "ta.its.data.videowall.scene"`) — đổi giá trị thành
`"ta.its.data.videowall.data"`. Giữ nguyên `mode: "pubsub"`, `allowSubscribeOnFe: true`.

### 2. `src/src/transporter/constants/transporterEvent.ts`
Dòng 38: `DataVideoWallScene: 'ta.its.data.videowall.scene'` → `'ta.its.data.videowall.data'`.

**Không cần đổi** tên biến `DataVideoWallScene`/`NatsSubject.DataVideoWallScene`/event key nội bộ
`'nats:data:videowall:scene'` (dòng 12) — đây chỉ là tên gọi nội bộ trong code FE (Pub/Sub bus nội
bộ `mittBus`), không phải chuỗi wire NATS thật gửi lên server. Có thể cân nhắc đổi tên biến sang
`DataVideoWall` (bỏ hậu tố `Scene`) cho đúng ngữ nghĩa mới (subject giờ mang nhiều loại sự kiện,
không riêng scene) — KHÔNG bắt buộc, tuỳ người thực thi, không ảnh hưởng chức năng.

### 3. Xử lý `EventType` trong handler nhận message (MỚI — không có trong bản gốc 2026-09-12)
Tại nơi xử lý message nhận từ subject này (`transporterNats.ts` case
`NatsSubject.DataVideoWallScene`, và/hoặc trong `useTransporterVideoWallScene.ts`) — thêm bước
đọc field `EventType` trong payload trước khi xử lý tiếp, rẽ nhánh theo từng loại sự kiện thay vì
giả định payload luôn là thông báo scene activated như trước. Tối thiểu cần xử lý đúng
`"SceneActivated"` (giữ hành vi cũ, không đổi UI logic đã có) — các `EventType` khác
(`HardwareOutOfSync`, `DeviceHeartbeat`, `DeviceProbeCompleted`...) có thể tạm thời log/bỏ qua nếu
FE chưa có UI tương ứng, miễn không bị crash khi nhận phải payload lạ.

### Không cần sửa gì thêm
- `src/src/views/videoWall/monitor/index.vue`, `useTransporterVideoWallScene.ts` (phần logic xử
  lý SceneActivated hiện có) — chỉ tham chiếu qua `TransporterEvent.DataVideoWallScene` (tên nội
  bộ, không đổi), giữ nguyên hành vi cho đúng `EventType = "SceneActivated"`.
- `src/src/views/videoWall/itsIntegration/index.vue`, `schedule/index.vue` — nếu có tham chiếu
  tương tự qua tên nội bộ, cũng không cần sửa (chỉ cần grep lại để chắc chắn không có nơi nào
  hardcode literal string `'ta.its.data.videowall.scene'` ngoài 2 file trên).

---

## Verification (Frontend)

1. Grep toàn bộ `src/src/` và `src/public/`: `ta\.its\.data\.videowall\.scene` — phải trả về
   **0 kết quả** sau khi sửa.
2. Grep `ta\.its\.data\.videowall\.data` — phải xuất hiện đúng ở 2 vị trí liệt kê trên (mục 1-2).
3. Xác nhận handler đọc `EventType` trước khi xử lý (mục 3) — không throw/crash nếu nhận payload
   với `EventType` lạ/chưa biết.
4. **Phải deploy cùng lúc với phần BE + Worker**
   (`videowall-eliminate-worker-to-be-nats-reply-prompt.md`). Test thật trên Monitor page: BE
   kích hoạt 1 scene (`ActiveVwScene`), xác nhận FE tự động cập nhật UI đúng như mong đợi (không
   còn nghe được nếu 2 bên lệch subject).
