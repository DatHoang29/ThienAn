# Prompt: Đổi tên subject NATS VideoWall (Frontend) — scene → status

> Ngày lập: 2026-09-12. Phần FRONTEND của việc đổi tên subject NATS. **Có 1 file riêng cho
> Backend + Worker**: `videowall-subject-rename-be-worker-prompt.md` (cùng thư mục) — phải làm
> ĐỒNG THỜI với file đó, không thể deploy riêng lẻ (subject cũ/mới không nói chuyện được với
> nhau, FE sẽ mất real-time nếu chỉ đổi 1 phía).

## Bối cảnh
FE chỉ subscribe đúng 1 trong 3 subject VideoWall — subject `scene` (BE→FE, thông báo trạng thái
scene đang chiếu để tự cập nhật UI). 2 subject còn lại (`request`/`response`, đổi tên thành
`control`/`data`) là nội bộ BE↔Worker, FE không đụng tới, không cần sửa gì cho 2 cái đó.

**Đổi**: `ta.its.data.videowall.scene` → `ta.its.data.videowall.status`.

---

## File cần sửa — Frontend (`TA-ITS015-WEBVUE-V1.0`, branch `dev`)

### 1. `src/public/Configuration/Nats.subjects.json`
Dòng 44 (object có `"subject": "ta.its.data.videowall.scene"`) — đổi giá trị thành
`"ta.its.data.videowall.status"`. Giữ nguyên `mode: "pubsub"`, `allowSubscribeOnFe: true`.

### 2. `src/src/transporter/constants/transporterEvent.ts`
Dòng 38: `DataVideoWallScene: 'ta.its.data.videowall.scene'` → `'ta.its.data.videowall.status'`.

**Không cần đổi** tên biến `DataVideoWallScene`/`NatsSubject.DataVideoWallScene`/event key nội bộ
`'nats:data:videowall:scene'` (dòng 12) — đây chỉ là tên gọi nội bộ trong code FE (Pub/Sub bus nội
bộ `mittBus`), không phải chuỗi wire NATS thật gửi lên server, đổi thêm chỉ tốn công không cần
thiết và không ảnh hưởng gì tới việc đổi subject wire.

### Không cần sửa gì thêm
- `src/src/views/videoWall/monitor/index.vue`, `useTransporterVideoWallScene.ts` — chỉ tham
  chiếu qua `TransporterEvent.DataVideoWallScene` (tên nội bộ, không đổi) nên không cần sửa.
- `src/src/views/videoWall/itsIntegration/index.vue`, `schedule/index.vue` — nếu có tham chiếu
  tương tự qua tên nội bộ, cũng không cần sửa (chỉ cần grep lại để chắc chắn không có nơi nào
  hardcode literal string `'ta.its.data.videowall.scene'` ngoài 2 file trên).

---

## Verification (Frontend)

1. Grep toàn bộ `src/src/` và `src/public/`: `ta\.its\.data\.videowall\.scene` — phải trả về
   **0 kết quả** sau khi sửa.
2. Grep `ta\.its\.data\.videowall\.status` — phải xuất hiện đúng ở 2 vị trí liệt kê trên.
3. **Phải deploy cùng lúc với phần BE + Worker** (`videowall-subject-rename-be-worker-prompt.md`).
   Test thật trên Monitor page: BE kích hoạt 1 scene (`ActiveVwScene`), xác nhận FE tự động cập
   nhật UI đúng như trước khi đổi tên (không còn nghe được nếu chỉ đổi 1 phía).
