# Phân hệ Dashboard Giám sát CCTV (dashboard)

Dashboard giám sát camera: gắn camera vào bố cục lưới, xem **trực tiếp (live)** qua WebRTC,
**xem lại (playback)** footage theo dòng thời gian, **điều khiển PTZ**, chụp **snapshot** và
xuất video. Hỗ trợ chế độ **toàn màn hình** và áp dụng **template bố cục**.

## 1. Cấu trúc

```
dashboard/
├── index.vue                     ← orchestrator: list + detail + editor + fullscreen
├── types.ts                      ← kiểu dữ liệu + cấu hình lưới + chuẩn hóa ô
├── services/
│   └── cctvDashboardService.ts   ← tầng API nghiệp vụ (mapping dữ liệu)
├── composables/
│   └── useCameraDashboardEditor.ts  ← logic editor camera (thêm/xóa/kéo–thả/áp template)
├── component/                    ← các thành phần con (xem mục 5)
├── constants/cameraControlLayers.ts ← thứ tự z-index các lớp điều khiển
├── utils/cameraControlUtils.ts   ← hàm thuần (tốc độ playback, tooltip…)
├── styles/cameraControlTokens.scss
└── __tests__/
```

**Tài liệu chuyên sâu kèm theo** (giữ nguyên, nên đọc khi sửa phần liên quan):
- `PLAYBACK_TIME_SYNC.md` — cơ chế đồng bộ đồng hồ playback theo tiến độ video.
- `CCTV_TYPE_MAPPING.md` / `CCTV_TYPES_CLEANUP.md` — ánh xạ & dọn dẹp kiểu dữ liệu.

## 2. Kiến trúc tổng thể

```
                       index.vue (orchestrator)
        ┌───────────────────┼─────────────────────────┐
        ▼                   ▼                         ▼
  Sidebar             Workspace                 Fullscreen
  - DashboardCard     - DetailHeader            - FullscreenCameraStage
  - CameraEquipment   - CameraGrid (GridStack)  - FullscreenFocusLayout
    Panel               └─ CameraTile (stream)    (PTZ / Playback / Info)
                        - PlaybackBar
                        - PtzControlPanel / PtzMovePad

  Logic editor camera  → composables/useCameraDashboardEditor.ts
  Gọi API + mapping    → services/cctvDashboardService.ts
  Cache dữ liệu        → /@/stores/cctvDashboard.ts
```

## 3. Luồng chính

**Khởi tạo:**
```
onMounted → Promise.all(loadDashboards, loadDashboardTags, loadEquipments, loadTemplates)
          → nếu URL có ?id → handleSelectDashboard(id)
```

**Chọn / mở dashboard:**
```
handleSelectDashboard(id)
  → getCctvDashboardDetail(id, cachedDevices)   // tái dùng thiết bị từ store (tránh N+1)
  → normalizeDashboardCameras() → editorState.cameras → CameraGrid render
```

**Xem trực tiếp (CameraTile):**
```
resolveStreamUrl() → getCctvWebRtcStreamUrl(deviceId) → WebSocket signaling
  → RTCPeerConnection → attachRemoteStream() → <video>
```

**Xem lại (PlaybackBar):**
```
loadPeriods() → loadCctvRecordingPeriods(footage)   // các khoảng có dữ liệu
seek/togglePause/cycleSpeed → emit('playback', {fromDate, speed})
  → CameraTile phát lại stream từ mốc đó; đồng hồ suy ra từ tiến độ video (xem PLAYBACK_TIME_SYNC.md)
```

**Lưu bố cục:**
```
handleSaveLayout → syncGridLayout() → ensureDashboardPersisted()
  → saveCctvDashboardItems(dashboardId, cameras)
```

## 4. `services/cctvDashboardService.ts` — các hàm chính

| Nhóm | Hàm | Mục đích |
| --- | --- | --- |
| Danh sách | `loadCctvEquipments`, `loadCctvDashboardList`, `loadCctvDashboardTags`, `loadCctvDashboardTemplates` | Tải dữ liệu nguồn |
| Chi tiết | `getCctvDashboardDetail(id, cachedDevices?)` | Tải dashboard + items + map camera (tái dùng devices cache) |
| CRUD dashboard | `createCctvDashboard`, `updateCctvDashboard`, `deleteCctvDashboard`, `duplicateCctvDashboard` | Quản lý dashboard |
| Items | `saveCctvDashboardItems`, `createCctvDashboardItems` | Lưu các ô camera |
| Template | `createCctvDashboardTemplate` | Xuất bố cục hiện tại thành template |
| Media | `getCctvWebRtcStreamUrl`, `getCctvPlaybackStreamUrl`, `loadCctvRecordingPeriods`, `getCctvMediaDownloadUrl`, `getCctvSnapshotUrl` | Stream / footage / tải video / snapshot |
| PTZ | `moveCctvPtz`, `stopCctvPtz`, `loadCctvPtzPresets`, `activateCctvPtzPreset`, `deleteCctvPtzPreset` | Điều khiển PTZ + preset |

> Các hàm media định danh bằng `camera.deviceId` (Nx). URL stream/tải video trả về đi qua Cctv Proxy (kèm ticket), FE không cần phiên/cookie Nx.

## 5. Thành phần con (`component/`)

| Component | Vai trò |
| --- | --- |
| `CameraGrid.vue` | Lưới camera bằng GridStack (kéo–thả/resize, ép vuông, đồng bộ layout) |
| `CameraTile.vue` | Một ô camera: resolve URL, phát WebRTC/native, báo tiến độ, ảnh "không khả dụng" |
| `PlaybackBar.vue` | Dòng thời gian xem lại: tải footage, seek, play/pause, đổi tốc độ, xuất video, snapshot |
| `PtzControlPanel.vue` / `PtzMovePad.vue` | Bảng điều khiển PTZ + bàn di chuyển/zoom |
| `PtzPresetCard.vue` | Thẻ preset PTZ |
| `FullscreenFocusLayout.vue` / `FullscreenCameraStage.vue` | Bố cục & sân khấu camera toàn màn hình |
| `DetailHeader.vue` | Tiêu đề + hành động dashboard (lưu, sửa, template, fullscreen…) |
| `CameraEquipmentPanel.vue` | Danh sách thiết bị để kéo/thêm vào lưới (có nút **Tải lại**) |
| `CameraPickerDialog.vue` | Hộp chọn camera cho ô trống |
| `CameraInfoPanel.vue` | Thông tin chi tiết camera |
| `CameraTemplatePreview.vue` | Xem trước bố cục template |
| `DashboardCard.vue` | Thẻ dashboard ở sidebar |
| `CustomSelect.vue` / `MetricCard.vue` | Tiện ích UI dùng lại |

## 6. `composables/useCameraDashboardEditor.ts`

Đóng gói logic chỉnh sửa bố cục camera (tách khỏi `index.vue`):

- `addCameraFromPanel`, `addCameraToGrid`, `removeCameraLocal` — thêm/xóa camera trong lưới.
- `startManualDrag` — kéo camera từ panel thả vào lưới (có ghost + tính vị trí).
- `applyTemplateLayout(template)` — áp bố cục template lên camera hiện có (sinh placeholder cho ô thừa).
- Va chạm: `canPlaceCameraAt`, `getNextCameraPosition`, `getNearestCameraPosition`.
- Helper: `getEquipmentId`, `getCameraSourceId`.

## 7. Kiểu dữ liệu & cấu hình (`types.ts`)

- `VideoMonitoringItem` — ô camera trong lưới (`id, sourceCameraId, deviceId, x, y, w, h, isPlaceholder…`).
- `DashboardCCTVOutput` / `DashboardCCTVDetailOutput` — dashboard và bản chi tiết kèm camera.
- `dashboardGridConfig` — cấu hình lưới (6×6, margin, kích thước ô mặc định…).
- `normalizeCameraGridItem(item)` — ép ô về **vuông** và nằm trong lưới (clamp x/y/w/h).
- `ICctvDashboardTemplate` / `ICctvDashboardTemplateSlot` — kiểu template phía UI.

### Cấu hình lưới lấy từ DB (SysOpConfig — nhóm `cctv_config`)

`dashboardGridConfig` trong `types.ts` chỉ là **giá trị mặc định/dự phòng**. Khi mở dashboard,
`onMounted` gọi `applyCctvDashboardConfigFromStore()` (`services/cctvDashboardConfig.ts`) để
ghi đè bằng giá trị cấu hình vận hành trong DB, đọc theo chuẩn
`useBaseConfig().getOpConfigItemByCode('cctv_config', code)`:

| Mã (code) trong DB | Áp vào | Ghi chú |
| --- | --- | --- |
| `cctv_dashboard_grid_v1_w` | `column` (+ `maxItemsPerRow`) | Số cột |
| `cctv_dashboard_grid_v1_h` | `row` (+ `minRow`, `maxRow`) | Số hàng |
| `cctv_dashboard_grid_v1_margin` | `margin` | Khoảng cách ô |
| `cctv_dashboard_grid_v1_float` | `float` | "1"/"true" → true |
| `cctv_dashboard_grid_v1_animate` | `animate` | "1"/"true" → true |
| `cctv_dashboard_grid_v1_defaultW` | `defaultW` (+ `defaultH`) | Ô vuông nên cao = rộng |
| `cctv_dashboard_grid_v1_minW` | `minW` (+ `minH`) | Ô vuông nên cao = rộng |
| `cctv_vendor` | vendor gọi API media | `getCctvVendor()` đọc trực tiếp khi gọi |

> Vì lưới là ô vuông, các chiều cao (`minH`, `defaultH`) và `minRow/maxRow/maxItemsPerRow`
> được suy ra từ giá trị tương ứng, không cần khai báo riêng trong DB. Key nào DB thiếu thì giữ mặc định.

## 8. Điểm cần lưu ý quan trọng

- **Camera không có `deviceId` → không hỗ trợ media.** `CameraTile` (computed `hasStreamSource`) sẽ
  hiển thị ảnh "không khả dụng" và **không gọi API stream**, tránh lỗi `notFoundByDeviceId`.
- **Vô hiệu hóa điều khiển theo trạng thái thiết bị** (`utils/cameraControlUtils.ts`):
  - **PTZ + Snapshot** chỉ bật khi camera **online + có `deviceId` + có URL luồng**
    (`canUseLiveCameraControls`). Thiếu một trong ba → nút bị disable kèm tooltip lý do.
  - **Playback** chỉ cần **`deviceId`** (`canUseCameraPlayback`): camera offline hoặc không có URL
    live vẫn xem lại được footage đã ghi; chỉ disable khi không có `deviceId`. Không có footage trong
    khoảng thời gian thì `PlaybackBar` báo "không có dữ liệu" (không disable trước).
  - Áp dụng ở `FullscreenFocusLayout` (header + panel toggle) và `CameraTile` (overlay fullscreen grid),
    kèm guard trong handler `handleFullscreenCameraAction` (defense-in-depth).
- **Phiên Nx**: nếu stream lỗi auth, `CameraTile` tự thử lại với phiên mới (`forceAuthRefresh`).
- **Đồng bộ đồng hồ playback**: Nx truyền media qua WebRTC, thời gian footage không nằm trong kênh
  signaling → vị trí phát được suy ra từ tiến độ render `<video>`. Xem `PLAYBACK_TIME_SYNC.md` trước khi sửa.
- **GridStack**: cùng lưu ý như module templates (xem `templates/README.md` mục 5) — `removeDOM`,
  va chạm khi resize, cờ chống vòng lặp `change`.
- **Cache thiết bị (N+1)**: `getCctvDashboardDetail` nhận `cachedDevices` từ store để **không**
  gọi lại `deviceview` mỗi lần mở dashboard. Nút "Tải lại danh sách camera" mới dùng `{ force: true }`.
- **z-index lớp điều khiển**: dùng hằng số `CameraControlLayer` (`constants/cameraControlLayers.ts`),
  không hardcode z-index rời rạc.
- File `component/log.txt` là dữ liệu tạm trong quá trình phát triển, không phải mã nguồn.

## 9. Kiểm thử

```bash
pnpm test src/views/cctv/dashboard
```

- `utils/__tests__/*.property.test.ts` — property test cho hàm thuần (tốc độ playback, chọn camera, format…).
- `component/__tests__/*` — test mount component (CameraTile, FullscreenFocusLayout, export snapshot…).
- Tương tác stream/PTZ thực tế cần môi trường Nx → kiểm thử thủ công.

## 10. Phụ thuộc

- `gridstack` (^12), `element-plus`, `file-saver` (lưu snapshot/video)
- API: `DashboardApi`, `DashboardItemApi`, `CctvMediaApi`, `CctvPtzApi`, `CctvDeviceApi`, `TemplateApi`
- Store: `/@/stores/cctvDashboard.ts`
- Trình duyệt: WebRTC (`RTCPeerConnection`), Fullscreen API, `requestVideoFrameCallback`
