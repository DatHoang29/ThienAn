# Giám sát sự kiện và sự cố

Hai chức năng Sự kiện và Sự cố chạy trực tiếp từ menu hoặc được ghép thành tab qua `tms/incidentNew/index`. Mỗi chức năng tự quản lý giao diện, bộ lọc, truy vấn và trạng thái.

```text
incidentNew/
├── index.vue                         # Ghép chức năng thành tab theo menu
├── README.md
├── event/
│   ├── EventManage.vue               # Trang Sự kiện
│   ├── useEventMonitor.ts            # State, quyền xem, truy vấn và realtime VDS
│   ├── eventModel.ts                 # Kiểu dữ liệu, bộ lọc và tham số VDS
│   └── component/
│       ├── detailEvent.vue
│       └── createIncident.vue        # Tạo sự cố từ sự kiện (dialog trong slot dialogs)
├── incident/
│   ├── IncidentManage.vue            # Trang Sự cố
│   ├── useIncidentMonitor.ts         # State, quyền, truy vấn và xử lý sự cố TMS
│   ├── incidentModel.ts              # Kiểu dữ liệu, bộ lọc và tham số TMS
│   ├── incident.scss                 # Style riêng: biểu đồ, trạng thái, thao tác
│   └── component/
│       ├── detailIncident.vue
│       ├── overviewChart.vue
│       └── incidentHistory.vue
└── shared/
    ├── monitorFormat.ts              # Hàm thuần định dạng biển số/lý trình
    └── monitor.scss                  # Style trình bày chung
```

- `index.vue` lấy tab từ `menuTabs.tabsByMenuId[route.meta.menuId]`, dùng chung `visibleMenuTabs` và `resolveViewModule` với `system/userMngt`. Tên, biểu tượng, thứ tự, trạng thái, quyền và chính sách cache lấy từ menu CSDL; không tự thêm tab khi thiếu cấu hình. Component được tải khi chọn tab. Cache cũ bị hủy khi tab bị bỏ/thu hồi hoặc đổi component/chính sách cache.
- Mỗi chức năng tạo `useEventMonitor()` hoặc `useIncidentMonitor()`, tự quản lý bộ lọc, phân trang, dữ liệu và ảnh. Không có `setTab` hay nhánh chuyển nghiệp vụ bằng `state.tab`. `isKeepAlive` khác `false` giữ trạng thái khi chuyển tab; `false` hủy component khi rời tab. Tab đang tải/lưu chặn chuyển tab.
- Cả hai tab Sự kiện và Sự cố tự tải lại grid theo `tmsStore.incidentRevision` khi NATS báo thay đổi hoặc kết nối lại, không hiện thông báo yêu cầu làm mới thủ công. Store gom các bản tin trong 300 ms và hủy cache trước khi thông báo. Giữ truy vấn đã áp dụng, sort, trang và dòng đang chọn; thay đổi đến khi đang tải/lưu hoặc tab đang cache được gom để tải lại khi sẵn sàng.
- `EventManage.vue` và `IncidentManage.vue` truyền tiêu đề đã dịch, cột, ID bảng, thứ tự sắp xếp và các slot cho `DataWorkspace.vue` qua `monitor.workspace`. Workspace giữ bố cục và sự kiện bảng; slot `detail` nhận `visible`/`active`, các slot ô nhận dữ liệu VXE. Mỗi tab tự nối phím trái/phải tới component chi tiết của nó.
- `detailEvent.vue` quản lý chi tiết phương tiện, tải hai ảnh VDS `full`/`vehicle`, giữ khóa thời gian UTC có microsecond và mở dialog xem/in phương tiện.
- Tạo sự cố từ sự kiện: bản tin chưa có sự cố (`TmsIncident.EventId = VendorEventId`) thì nút "Tạo sự cố" mở `createIncident.vue` (đặt ở slot `dialogs` của `EventManage`, nối qua `monitor.bindCreateDialog`). Nút chỉ bấm được khi loại đã ánh xạ của sự kiện thuộc nhóm sự cố (`monitor.canCreateIncident`; nhóm sự kiện / `unknown` thì khoá). Loại sự cố cố định theo loại của sự kiện (BE đọc lại từ bản tin), người dùng chọn **hình thức**: `process` (Xử lý — Đăng ký rồi theo luồng cũ: có kịch bản tự động thì đếm ngược, không thì thủ công) hoặc `record` (Ghi nhận — tạo thẳng ở Hoàn thành/Bỏ qua/Huỷ, không chạy kịch bản/thiết bị). Sự kiện cũ hơn `createIncidentAutomationMaxAgeMinutes` thì mặc định Ghi nhận. Dialog gửi khóa bản tin + hình thức (+ trạng thái ghi nhận) + mô tả/ghi chú tới `tmsIncident/CreateFromVdsEvent`; BE tự đọc vị trí/camera/ảnh từ bản tin gốc. BE báo đang có sự cố mở cùng loại/vị trí ⇒ hỏi "Mở sự cố đang có" hoặc "Vẫn tạo mới" (`force`). Xong thì mở sự cố bên tab Sự cố.
- `IncidentManage.vue` quản lý đăng ký/sửa sự cố, dialog chỉnh sửa, biểu đồ và icon loại sự kiện. `detailIncident.vue` quản lý các giai đoạn ảnh TMS, trạng thái, báo cáo, hủy/bỏ qua và mở lịch sử. Lịch sử và biểu đồ nằm cùng thư mục sự cố.
- `event/` không import nghiệp vụ từ `incident/` và ngược lại. `shared/` chỉ chứa hàm định dạng thuần và style chung; không truy vấn API, giữ state hoặc kiểm tra quyền. Không đưa các file nghiệp vụ vào thư viện component.
- `SearchFilterPanel.vue` nhận ngày mặc định từ tab; `MediaPanel.vue` nhận hàm tải, giai đoạn và tùy chọn thumbnail từ tab, xử lý hủy yêu cầu/ảnh đến muộn và thu hồi blob URL.
- Các component chung nằm ở `src/src/components/workspace/`: `DataWorkspace.vue`, `SearchFilterPanel.vue`, `MediaPanel.vue`, `PanelSplitter.vue`, `WorkspaceEmpty.vue`. Chúng không import từ view, API hay store TMS. CSS khung trang, bộ lọc và ảnh nằm cùng component; `workspace.ts` chứa hợp đồng dữ liệu chung và hàm quản lý bàn phím. Hướng dẫn dùng ở `docs/ui/workspace-components.md`.
- Hai bảng lưu cấu hình riêng bằng ID `tmsIncidentReviewGrid_event` và `tmsIncidentReviewGrid_incident`.
- Component quản lý mỗi tab chỉ có một phần tử gốc `DataWorkspace`; dialog chỉnh sửa đặt trong slot `dialogs` để nhận đúng chiều rộng từ router layout.
- Ngày giờ dùng `useDateFormat` và `DateTimeFormatEnum`; timestamp VDS vẫn được chuẩn hóa UTC bằng `vdsTimestamp` trước khi hiển thị. Placeholder nhập/chọn dùng các key chung `lz.label.base.pleaseType` và `lz.label.base.pleaseSelect`. Code dùng cấu hình Prettier chung của dự án, có dòng trống giữa các hàm.
- Component con đặt trong `component/`, tên tệp theo quy ước `detailEvent.vue`, `detailIncident.vue`, `incidentHistory.vue`, `overviewChart.vue`. Đường dẫn hai tab và ID bảng giữ nguyên.
- Hai chức năng dùng `handleQuery` / `resetQuery`; Sự cố có `handleAdd` / `handleEdit`. `EventMonitor` và `IncidentMonitor` là hợp đồng riêng xuất từ composable tương ứng; component chi tiết chỉ dùng hợp đồng của chức năng mình.
- Lịch sử giữ trạng thái, dữ liệu suy ra, hàm hỗ trợ, truy vấn và lifecycle ngay trong `script setup`; không bọc thêm composable chỉ dùng trong chính component.
- Khóa dịch theo thứ tự **base → tms → chức năng riêng**: dùng `lz.entity.base.*`, `lz.label.base.*`, `lz.button.base.*`, `lz.message.base.*`, `lz.validation.base.*` cho nội dung chung; dùng `lz.entity.tms.*` / `lz.label.tms.*` cho thuật ngữ giao thông (biển số, loại/cỡ xe, tốc độ, làn, lý trình, VDS); chỉ giữ `tmsIncident.*` cho trạng thái, quy trình, lịch sử xử lý và thao tác đặc thù sự cố. Ví dụ biển số dùng `lz.entity.tms.licensePlate`. Đơn vị `mm`, `km/h` đặt tại giao diện; chữ in hoa do CSS xử lý.
- Hai khóa `lz.label.tmsIncident.reviewEvent` và `lz.label.tmsIncident.reviewIncident` giữ dưới dạng liên kết tới khóa chuẩn để tương thích tiêu đề tab đã lưu trong menu CSDL. Cấu hình mới dùng `lz.label.tms.event` và `lz.label.tms.tmsIncident`.
- `eventModel.ts` chứa kiểu dữ liệu, bộ lọc, khóa bản ghi và tham số VDS; `incidentModel.ts` chứa phần TMS, bao gồm tham số danh sách/thống kê. Composable từng chức năng tự điều phối truy vấn, quyền, chọn bản ghi, yêu cầu cạnh tranh và adapter `workspace`. Hủy/bỏ qua và quyền cập nhật chỉ nằm trong chức năng Sự cố.
- Style nền lấy từ `theme/workspace.scss`; component chung tự mang CSS bố cục. Hai trang tự import `shared/monitor.scss`; Sự cố import thêm `incident.scss`. CSS không phụ thuộc việc tải trang tab cha trước. Hướng dẫn áp dụng: `docs/ui/workspace-style.md` ở gốc repository.

Kiểm tra hợp đồng API và điều hướng từ thư mục `src`:

```powershell
node --test tests/traffic-monitor-api.test.mjs tests/menu-tabs.test.mjs
```

Các kiểm tra dùng dữ liệu/API giả lập, không gọi backend thật.

## Chạy thành chức năng riêng

Menu có `type = menu` có thể trỏ trực tiếp đến:

| Chức năng | `component`                               | Quyền nghiệp vụ                            |
| --------- | ----------------------------------------- | ------------------------------------------ |
| Sự kiện   | `tms/incidentNew/event/EventManage`       | Các quyền `tmsTrafficAnalysis:*` tương ứng |
| Sự cố     | `tms/incidentNew/incident/IncidentManage` | Các quyền `tmsIncident:*` tương ứng        |

Hai component không yêu cầu props, slot tab hay store `menuTabs` để khởi tạo. Khi mở trực tiếp, tiêu đề mặc định là khóa dịch của chức năng. Khi ghép tab, trang cha vẫn truyền `title`, `tabLabel` và slot `tabs` theo cấu hình CSDL. `DataWorkspace` tự quản lý trạng thái active và lifecycle bàn phím trong cả hai trường hợp.

Hai chức năng dùng chung dịch vụ danh mục/cache/realtime `tmsStore` của ứng dụng; mỗi chức năng có bộ lọc, phân trang, yêu cầu, lựa chọn và lifecycle riêng. Sự kiện không tải trang Sự cố, danh sách sự cố hay danh mục loại sự cố. ID bảng, định dạng thời gian và hợp đồng API giữ nguyên. Thay đổi frontend không tự tạo hoặc đổi menu CSDL.

## Cấu hình menu CSDL

Menu cha có `type = menu`, component `tms/incidentNew/index`. Hai node con có `type = tab`, `pid` là ID menu cha:

| Trường             | Sự kiện                             | Sự cố                                     |
| ------------------ | ----------------------------------- | ----------------------------------------- |
| `name`             | `tmsIncidentNewEventTab`            | `tmsIncidentNewIncidentTab`               |
| `meta.title`       | `lz.label.tms.event`                | `lz.label.tms.tmsIncident`                |
| `component`        | `tms/incidentNew/event/EventManage` | `tms/incidentNew/incident/IncidentManage` |
| `meta.icon`        | `ele-Bell`                          | `ele-Warning`                             |
| `orderNo`          | 100                                 | 200                                       |
| `status`           | 1                                   | 1                                         |
| `meta.isHide`      | false                               | false                                     |
| `meta.isKeepAlive` | true                                | true                                      |

Tên hiển thị có thể là khóa dịch hoặc văn bản trực tiếp. Tab không tạo route/sidebar/tagsView riêng. Cấp menu cha, node tab và các node `btn` của nghiệp vụ tương ứng cho vai trò; giữ quyền `tmsTrafficAnalysis:page` / `tmsIncident:page` và quyền thao tác trên node `btn`. Frontend kiểm tra thêm `permission` nếu backend trả trường này trên tab. Tải lại cây menu sau khi đổi cấu hình/quyền. Thay đổi frontend không tự ghi dữ liệu CSDL.
