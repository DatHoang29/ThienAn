# GridPage

Khung bố cục mỏng cho các trang tự giữ `vxe-grid` của mình (trang CRUD kiểu cũ: `useVxeTable`, `TableHeader`, `SearchButton`).

| Component | Vị trí | Trách nhiệm |
| --- | --- | --- |
| `WorkspacePage.vue` | `components/workspace/` | Header trang và slot `tabs`; khi nằm trong một `WorkspacePage` khác thì chỉ render nội dung. |
| `GridSearchPanel.vue` | `components/gridPage/` | Thẻ tìm kiếm phía trên, thanh chia ngang (thu gọn/kéo/phím mũi tên) và slot nội dung. Prop duy nhất: `id` của thẻ tìm kiếm. |
| `GridSummary.vue` + `useGridSummary.ts` | `components/gridPage/` | "Tổng cộng: N · Cập nhật lần cuối: giờ" như toolbar của `DataWorkspace`. Đặt `<GridSummary :summary="summary" />` trong slot `toolbar_buttons` của vxe-grid và bọc API trang: `return track(getAPI(XxxApi).apiXxxPagePost(params))`. |
| `useGridPager.ts` | `components/gridPage/` | Thu gọn thanh phân trang theo chiều rộng bảng, cùng ngưỡng `DataWorkspace`: ≥ 900px đầy đủ, 640–899px bỏ nhảy nhanh/tổng, < 640px chỉ trước/sau + ô tới trang + số trang + cỡ trang. Gọi `useGridPager(xGrid, options)` ngay sau `useVxeTable`. `GridSearchPanel` thêm: bảng < 600px thì "Tổng cộng" một hàng, nút thao tác xuống hàng dưới. |
| `GridSidePanel.vue` | `components/gridPage/` | Vùng bên trái (slot `side`, ví dụ cây đơn vị), thanh chia dọc và slot nội dung; thay cho `splitpanes`. Props: `id`, `label`, `width` (mặc định 240). Dưới 600px vùng bên trái chuyển lên trên. |

Trang chức năng tự khai báo form tìm kiếm, `<vxe-grid>`, `useVxeTable<Row>(...)`, cột, sự kiện, tham số truy vấn, API và thao tác dòng trong cùng file Vue. Khung không nhận controller của bảng và không chuyển tiếp slot của bảng.

Khác `components/workspace/DataWorkspace`: không có tìm kiếm tự động, không có panel chi tiết, không bao bảng. Bấm Enter / nút Tìm kiếm như trang gốc. `DataWorkspace` giữ cho các màn giám sát như `tms/incidentNew`.

Áp dụng đầu tiên cho người dùng và tài khoản. Các trang hiện nằm tại `views/system/user` và `views/system/account`; `views/tabPage/index.vue` là khung nhóm tab dùng chung cho mọi phân hệ. Bản nguyên bản lưu trong `views/system/old` và `old.zip`. Các tiện ích cấp thấp (`PanelSplitter`, `useSearchPanel`) dùng lại từ `components/workspace/`.

```vue
<div class="feature-container">
	<GridSearchPanel id="feature-search">
		<template #filters>
			<!-- Form tìm kiếm + el-divider + SearchButton của trang gốc, giữ nguyên. -->
		</template>
		<el-card class="full-table" shadow="hover">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents" />
		</el-card>
	</GridSearchPanel>
</div>
```

- Slot `filters` nhận `{ visible, hide }`; `hide()` thu gọn thẻ tìm kiếm.
- `GridSearchPanel` tự lấp đầy chiều cao (`flex: 1`) và áp lại style `.full-table`, nên dùng được cả khi trang là menu riêng, nằm trong tab, hay nằm trong `splitpanes`.
- Không dùng cho trang đã chuyển sang `DataWorkspace`.
