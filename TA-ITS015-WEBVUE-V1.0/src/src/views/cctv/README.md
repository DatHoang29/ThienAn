# Module CCTV

Hệ thống quản lý và giám sát camera (CCTV) tích hợp với nền tảng VMS (Nx Witness).
Module gồm 3 phân hệ con: **Quản lý thiết bị (equipments)**, **Quản lý template bố cục (templates)**
và **Dashboard giám sát trực tiếp + xem lại (dashboard)**.

> Tài liệu này mô tả tổng quan kiến trúc cấp module. Chi tiết từng phân hệ xem README riêng:
> - [`equipments/README.md`](./equipments/README.md)
> - [`templates/README.md`](./templates/README.md)
> - [`dashboard/README.md`](./dashboard/README.md)

---

## 1. Tổng quan

| Hạng mục | Mô tả |
| --- | --- |
| Framework | Vue 3 (`<script setup lang="ts">`) + TypeScript |
| UI | Element Plus, vxe-table (bảng dữ liệu), GridStack (lưới kéo–thả) |
| State | Pinia (`/@/stores/cctvDashboard.ts`) |
| Giao tiếp API | Client sinh tự động từ Swagger tại `/@/api-services/cctv` |
| VMS | Nx Witness — stream WebRTC, footage (xem lại), snapshot, điều khiển PTZ |
| Đa ngôn ngữ | i18n key `lz.(button\|label\|message).cctv(Equipment\|Template\|Dashboard).*` |
| Kiểm thử | Vitest + fast-check + jsdom |

## 2. Cấu trúc thư mục

```
views/cctv/
├── README.md                ← tài liệu này (tổng quan module)
├── equipments/              ← phân hệ Quản lý thiết bị
│   ├── index.vue
│   └── __tests__/
├── templates/               ← phân hệ Quản lý template bố cục
│   ├── index.vue
│   ├── utils/templateLayout.ts   (logic layout thuần, có unit test)
│   ├── utils/__tests__/
│   └── MANUAL_TEST_CHECKLIST.md
└── dashboard/               ← phân hệ Dashboard giám sát + xem lại
    ├── index.vue            (orchestrator chính)
    ├── types.ts             (kiểu dữ liệu + cấu hình grid)
    ├── services/            (tầng gọi API + phiên Nx)
    ├── composables/         (logic editor camera tái sử dụng)
    ├── component/           (các thành phần con: grid, tile, playback, PTZ…)
    ├── constants/           (hằng số z-index lớp điều khiển)
    ├── utils/               (hàm thuần thuần + unit test)
    ├── styles/              (token SCSS)
    └── __tests__/
```

## 3. Quan hệ giữa các phân hệ

```
            ┌────────────────────┐
            │   equipments       │  Đồng bộ & quản lý thiết bị từ VMS
            │  (CctvDeviceApi)   │  → nguồn camera cho dashboard
            └─────────┬──────────┘
                      │ deviceId / equipmentId
                      ▼
   ┌────────────────────┐      tham chiếu      ┌────────────────────┐
   │     templates      │ ───────────────────► │     dashboard      │
   │  (bố cục lưới)     │   "Áp dụng template" │  (bố cục camera     │
   │  (TemplateApi)     │ ◄─────────────────── │   thực tế + xem)    │
   └────────────────────┘   "Xuất template"    └────────────────────┘
```

- **equipments**: đồng bộ danh sách thiết bị từ VMS (`syncdevice`), bật/tắt, xóa. Là nguồn camera.
- **templates**: định nghĩa bố cục lưới (vị trí/kích thước các ô) tái sử dụng. Có thể tạo từ
  dashboard ("Xuất sang template") và áp dụng ngược lại vào dashboard.
- **dashboard**: gắn camera thực tế vào bố cục, xem trực tiếp (live), xem lại (playback), điều khiển PTZ.

## 4. Tầng API (`/@/api-services/cctv`)

Client được sinh tự động (Swagger Codegen), **không sửa tay**. Các API class chính:

| API class | Mục đích |
| --- | --- |
| `CctvDeviceApi` | Danh sách / đồng bộ / bật-tắt / xóa thiết bị |
| `DashboardApi` | CRUD dashboard, tag, copy |
| `DashboardItemApi` | CRUD các ô camera trong dashboard |
| `TemplateApi` | CRUD template bố cục |
| `CctvMediaApi` | URL stream WebRTC, footage, snapshot, tải video, auth token Nx |
| `CctvPtzApi` | Điều khiển PTZ (di chuyển, zoom, preset) |

Tầng `dashboard/services/cctvDashboardService.ts` bọc các API trên thành hàm nghiệp vụ
đã chuẩn hóa dữ liệu (mapping), là **điểm truy cập API duy nhất** cho UI dashboard.

## 5. Store (`/@/stores/cctvDashboard.ts`)

Pinia store cache dữ liệu dùng chung để tránh gọi lại API:

| State | Ý nghĩa |
| --- | --- |
| `dashboards` | Danh sách dashboard |
| `dashboardTags` | Danh sách tag |
| `equipments` | Danh sách thiết bị (cache deviceview) |
| `dashboardDetails` | Cache chi tiết từng dashboard theo id |

> Lưu ý: dữ liệu lấy từ store khi load thường; nút **Tải lại** dùng `{ force: true }` để bỏ cache và gọi API mới.

## 6. Tích hợp Nx VMS — điểm cần lưu ý quan trọng

- URL WebRTC (live/playback) và tải video do BE trả về trỏ tới **Cctv Proxy Service** kèm `ticket` có hạn;
  FE dùng thẳng URL đó, KHÔNG cần đăng nhập/cookie Nx (tài khoản Nx chỉ nằm ở BE). Ticket hết hạn thì gọi lại API lấy URL mới.
- Các thao tác này định danh camera bằng **`deviceId` của Nx**, KHÔNG phải `id` nội bộ.
- **Thiết bị có `deviceId = null` nghĩa là chưa được Nx hỗ trợ/đồng bộ.** Khi đó UI phải hiển thị
  ảnh "không khả dụng" và **không gọi API media** (tránh lỗi `notFoundByDeviceId`).
  Hành vi này được xử lý ở `CameraTile.vue` (computed `hasStreamSource`).
- Để media hoạt động, thiết bị cần được đồng bộ qua nút **"Đồng bộ dữ liệu"** ở trang equipments.

## 7. Đa ngôn ngữ (i18n)

- Toàn bộ chuỗi hiển thị dùng key i18n, **không hardcode** trong template/script.
- Quy ước key theo chuẩn chung của dự án: `lz.<button|label|message>.<module><Entity>.<key>`
  ví dụ `lz.button.cctvEquipment.syncData`, `lz.message.cctvTemplate.confirmDelete`.
- Nút chung (xác nhận/hủy/xóa) tái dùng key `lz.button.base.*`, không định nghĩa lại.
- File ngôn ngữ: `/@/i18n/lang/its/cctv/vi-vn.json` và `/@/i18n/lang/its/cctv/en-us.json` (phải đồng bộ key + tham số `{..}`). Key dùng chung `lz.*.base.*` nằm ở `/@/i18n/lang/vi-vn.json`.

## 8. Kiểm thử

- Logic thuần tách khỏi `.vue` để unit-test (ví dụ `templates/utils/templateLayout.ts`,
  `dashboard/utils/cameraControlUtils.ts`).
- Chạy: `pnpm test` (toàn bộ) hoặc `pnpm test src/views/cctv/<module>` (theo phân hệ).
- Tương tác DOM/stream không cover được bằng unit test → dùng checklist thủ công
  (xem `templates/MANUAL_TEST_CHECKLIST.md`).

## 9. Quy ước chung khi phát triển

- Không sửa file trong `api-services/cctv` (sinh tự động) — bọc logic ở tầng `services`.
- Comment bằng tiếng Việt, ngắn gọn, giải thích "tại sao" hơn là "làm gì".
- Không để lại `console.*` debug trong code chính thức.
- Tên biến/hàm theo camelCase; key i18n theo đúng chuẩn ở mục 7.


## 10. Những feat chưa thực hiện

- Giới hạn hiện tại của speed playback hiện tại là 1 - 2s sai số từ x8 trở lên
- Hệ PTZ chưa thực hiện được vì chưa có thiết bị để test và chuẩn hóa đầu ra
- Chưa có lọc type Camera vì BE chưa hỗ trợ
- Chưa có thumbnail cho equipment ở trang quản lý vì BE chưa hỗ trợ
- Đây là tài liệu V1 - V2 sẽ phát triển thêm tính năng như dual playback, xem dynamic grid layout. 