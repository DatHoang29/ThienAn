# Giám sát sự kiện và sự cố

Component menu: `tms/incidentNew/index`. Cấu trúc theo `system/userMngt`: mỗi tab có một component quản lý chứa giao diện, bộ lọc, cấu hình cột và cách hiển thị dòng.

```text
incidentNew/
├── index.vue                         # Chọn tab theo cấu hình menu
├── event/
│   ├── EventManage.vue                # Bộ lọc, cột và nội dung dòng phương tiện VDS
│   └── component/detailEvent.vue    # Chi tiết, tải ảnh VDS và dialog phương tiện
├── incident/
│   ├── IncidentManage.vue             # Tab sự cố: quy trình TMS
│   └── component/
│       ├── detailIncident.vue        # Chi tiết, tải ảnh TMS, trạng thái và báo cáo
│       ├── overviewChart.vue          # Thống kê, biểu đồ của tab sự cố
│       └── incidentHistory.vue        # Giao diện và truy vấn lịch sử xử lý
└── internal/                         # Nghiệp vụ nội bộ incidentNew
    ├── useTrafficMonitor.ts          # Trạng thái, truy vấn và phân trang
    ├── trafficMonitorModel.ts        # Kiểu dữ liệu, chuyển đổi và tham số API
    ├── trafficMonitor.scss           # Bố cục đặc thù của trang
    └── README.md                     # Tài liệu module
```

- `index.vue` lấy tab từ `menuTabs.tabsByMenuId[route.meta.menuId]`, dùng chung `visibleMenuTabs` và `resolveViewModule` với `system/userMngt`. Tên, biểu tượng, thứ tự, trạng thái, quyền và chính sách cache lấy từ menu CSDL; không tự thêm tab khi thiếu cấu hình. Component được tải khi chọn tab. Cache cũ bị hủy khi tab bị bỏ/thu hồi hoặc đổi component/chính sách cache.
- Mỗi component tab tạo `useTrafficMonitor('event')` hoặc `useTrafficMonitor('incident')`, giữ riêng bộ lọc, phân trang, dữ liệu và ảnh. `isKeepAlive` khác `false` giữ trạng thái khi chuyển tab; `false` hủy component khi rời tab. Tab đang tải/lưu chặn chuyển tab.
- Tab Sự cố tự tải lại grid theo `tmsStore.incidentRevision` khi NATS báo thay đổi hoặc kết nối lại. Store gom các bản tin trong 300 ms và hủy cache trước khi thông báo. Giữ truy vấn đã áp dụng, sort, trang và dòng đang chọn; thay đổi đến khi đang tải/lưu hoặc tab đang cache được gom để tải lại khi sẵn sàng. Tab Sự kiện giữ cách thông báo cập nhật riêng.
- `EventManage.vue` và `IncidentManage.vue` truyền tiêu đề đã dịch, cột, ID bảng, thứ tự sắp xếp và các slot cho `DataWorkspace.vue` qua `monitor.workspace`. Workspace giữ bố cục và sự kiện bảng; slot `detail` nhận `visible`/`active`, các slot ô nhận dữ liệu VXE. Mỗi tab tự nối phím trái/phải tới component chi tiết của nó.
- `detailEvent.vue` quản lý chi tiết phương tiện, tải hai ảnh VDS `full`/`vehicle`, giữ khóa thời gian UTC có microsecond và mở dialog xem/in phương tiện.
- `IncidentManage.vue` quản lý đăng ký/sửa sự cố, dialog chỉnh sửa, biểu đồ và icon loại sự kiện. `detailIncident.vue` quản lý các giai đoạn ảnh TMS, trạng thái, báo cáo, hủy/bỏ qua và mở lịch sử. Lịch sử và biểu đồ nằm cùng thư mục sự cố.
- Gốc `incidentNew` chỉ có `index.vue`. `internal/` giữ truy vấn API, trạng thái/quyền nghiệp vụ, bộ chuyển tham số VDS/TMS và CSS chi tiết/biểu đồ của màn này. Không đưa các file nghiệp vụ vào thư viện component.
- `SearchFilterPanel.vue` nhận ngày mặc định từ tab; `MediaPanel.vue` nhận hàm tải, giai đoạn và tùy chọn thumbnail từ tab, xử lý hủy yêu cầu/ảnh đến muộn và thu hồi blob URL.
- Bốn component chung nằm ở `src/src/components/workspace/`: `DataWorkspace.vue`, `SearchFilterPanel.vue`, `MediaPanel.vue`, `PanelSplitter.vue`, `WorkspaceEmpty.vue`. Chúng không import từ view, API hay store TMS. CSS khung trang, bộ lọc và ảnh nằm cùng component; `workspace.ts` chứa hợp đồng dữ liệu chung và hàm quản lý bàn phím. Hướng dẫn dùng ở `docs/ui/workspace-components.md`.
- Hai bảng lưu cấu hình riêng bằng ID `tmsIncidentReviewGrid_event` và `tmsIncidentReviewGrid_incident`.
- Component quản lý mỗi tab chỉ có một phần tử gốc `DataWorkspace`; dialog chỉnh sửa đặt trong slot `dialogs` để nhận đúng chiều rộng từ router layout.
- Ngày giờ dùng `useDateFormat` và `DateTimeFormatEnum`; timestamp VDS vẫn được chuẩn hóa UTC bằng `vdsTimestamp` trước khi hiển thị. Placeholder nhập/chọn dùng các key chung `lz.label.base.pleaseType` và `lz.label.base.pleaseSelect`. Code dùng cấu hình Prettier chung của dự án, có dòng trống giữa các hàm.
- Component con đặt trong `component/`, tên tệp theo quy ước `detailEvent.vue`, `detailIncident.vue`, `incidentHistory.vue`, `overviewChart.vue`. Đường dẫn hai tab và ID bảng giữ nguyên.
- Hai tab dùng `handleQuery` / `resetQuery`; tab sự cố tách `handleAdd` / `handleEdit`. `TrafficMonitor` là hợp đồng kiểu dữ liệu xuất từ composable hiện có, không tạo lớp dùng chung hoặc tệp mới.
- Lịch sử giữ trạng thái, dữ liệu suy ra, hàm hỗ trợ, truy vấn và lifecycle ngay trong `script setup`; không bọc thêm composable chỉ dùng trong chính component.
- Ưu tiên các khóa dịch `lz.button.base.*`, `lz.message.base.*`, `lz.entity.base.*` và khóa nghiệp vụ TMS đã có. Các khóa `review*` chỉ giữ cho nội dung riêng của màn giám sát; bỏ khóa trùng nghĩa và placeholder cũ không còn tham chiếu. Tên các khóa còn dùng và ID cấu hình bảng không thay đổi.
- `trafficMonitorModel.ts` giữ kiểu dữ liệu và các bộ chuyển tham số VDS/TMS dùng cho danh sách/thống kê. `useTrafficMonitor.ts` điều phối dữ liệu nghiệp vụ: truy vấn danh sách, quyền, thông tin bản ghi đang chọn, cập nhật trạng thái, xử lý yêu cầu cạnh tranh và tạo adapter `workspace` cho bảng dùng chung. Component riêng từng tab sử dụng hợp đồng này; trạng thái tìm kiếm, chuyển tab và API giữ hành vi hiện có.
- Style nền lấy từ `theme/workspace.scss`; component chung tự mang CSS bố cục. `trafficMonitor.scss` chỉ giữ nội dung đặc thù của trang. Hướng dẫn áp dụng: `docs/ui/workspace-style.md` ở gốc repository.

Kiểm tra hợp đồng API và điều hướng từ thư mục `src`:

```powershell
node --test tests/traffic-monitor-api.test.mjs tests/menu-tabs.test.mjs
```

Các kiểm tra dùng dữ liệu/API giả lập, không gọi backend thật.

## Cấu hình menu CSDL

Menu cha có `type = menu`, component `tms/incidentNew/index`. Hai node con có `type = tab`, `pid` là ID menu cha:

| Trường | Sự kiện | Sự cố |
| --- | --- | --- |
| `name` | `tmsIncidentNewEventTab` | `tmsIncidentNewIncidentTab` |
| `meta.title` | `lz.label.tmsIncident.reviewEvent` | `lz.label.tmsIncident.reviewIncident` |
| `component` | `tms/incidentNew/event/EventManage` | `tms/incidentNew/incident/IncidentManage` |
| `meta.icon` | `ele-Bell` | `ele-Warning` |
| `orderNo` | 100 | 200 |
| `status` | 1 | 1 |
| `meta.isHide` | false | false |
| `meta.isKeepAlive` | true | true |

Tên hiển thị có thể là khóa dịch hoặc văn bản trực tiếp. Tab không tạo route/sidebar/tagsView riêng. Cấp menu cha, node tab và các node `btn` của nghiệp vụ tương ứng cho vai trò; giữ quyền `tmsTrafficAnalysis:page` / `tmsIncident:page` và quyền thao tác trên node `btn`. Frontend kiểm tra thêm `permission` nếu backend trả trường này trên tab. Tải lại cây menu sau khi đổi cấu hình/quyền. Thay đổi frontend không tự ghi dữ liệu CSDL.
