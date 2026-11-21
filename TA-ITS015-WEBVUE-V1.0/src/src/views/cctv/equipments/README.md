# Phân hệ Quản lý Thiết bị CCTV (equipments)

Quản lý danh sách thiết bị camera đồng bộ từ VMS (Nx Witness): tra cứu, đồng bộ, bật/tắt, xóa.
Đây là **nguồn thiết bị** để gắn vào dashboard.

## 1. Cấu trúc

```
equipments/
├── index.vue                       ← trang quản lý thiết bị
└── __tests__/
    └── cctvEquipments.test.ts      ← unit/integration test
```

## 2. Tính năng

| Tính năng | Mô tả |
| --- | --- |
| Tìm kiếm / lọc | Theo trạng thái (Status), tên thiết bị, địa chỉ IP (lọc phía client) |
| Đồng bộ dữ liệu | Gọi VMS đồng bộ thiết bị (`syncdevice`), hiển thị tổng kết thêm/sửa/xóa/bỏ qua/lỗi |
| Bật / Tắt | Bật hoặc tắt một thiết bị (`active`) |
| Xem chi tiết | Dialog thông tin: Device ID, IP, trạng thái thiết bị, lần đồng bộ cuối, ngày tạo/cập nhật |
| Xóa | Xóa đơn lẻ hoặc xóa hàng loạt (theo checkbox đã chọn) |

## 3. Luồng dữ liệu

```
onMounted → handleQuery()
  └─ getAPI(CctvDeviceApi).apiCctvCctvdeviceDevicelistGet(deviceId, ip, name)
     → lọc/sắp xếp phía client (filterDevices) → đổ vào vxe-grid (options.data)

Đồng bộ:  handleSync → apiCctvCctvdeviceSyncdevicePost → handleQuery()
Bật/Tắt:  handleToggleActive → apiCctvCctvdeviceActivePost → handleQuery()
Xóa:      handleDelete / handleBatchDelete → apiCctvCctvdeviceDeletePost → handleQuery()
```

- Bảng dùng `vxe-grid` qua hook `useVxeTable`. API trả về **danh sách không phân trang**, nên
  `proxyConfig.enabled = false` và `pagerConfig.enabled = false`, dữ liệu nạp thủ công vào `options.data`.

## 4. Các hàm chính (`index.vue`)

| Hàm | Vai trò |
| --- | --- |
| `handleQuery()` | Gọi API danh sách, lọc, đổ vào grid |
| `resetQuery()` | Xóa bộ lọc rồi truy vấn lại |
| `handleSync()` | Đồng bộ thiết bị từ VMS, hiển thị tổng kết |
| `handleToggleActive(row)` | Bật/tắt thiết bị (kèm hộp xác nhận) |
| `handleDelete(row)` | Xóa 1 thiết bị (kèm xác nhận) |
| `handleBatchDelete()` | Xóa nhiều thiết bị đã chọn |
| `handleDetail(row)` | Mở dialog chi tiết |
| `filterDevices(devices)` | Lọc theo tên/IP/status + sắp theo lần đồng bộ cuối |
| `assertApiSucceeded(res, msg)` | Kiểm tra response chuẩn (code/succeeded) và ném lỗi nếu thất bại |
| `getDeviceKey(row)` | Lấy khóa định danh thiết bị (`deviceId || id`) |

## 5. Trạng thái thiết bị (deviceState)

| Giá trị | Hiển thị |
| --- | --- |
| `1` | Online (success) |
| `0` | Offline (danger) |
| khác | N/A (info) |

Cột **Status** dùng `BaseConfigTypeEnum.SysStatusType` (cấu hình hệ thống), render qua `TagInfo`.

## 6. Điểm cần lưu ý

- **Device ID là khóa cho thao tác media về sau.** Thiết bị thiếu `deviceId` không thể bật/tắt
  hay dùng cho stream — code đã cảnh báo (`deviceIdNotFound`) trước khi gọi API.
- Đồng bộ là thao tác phía VMS: kết quả phụ thuộc tình trạng kết nối Nx; thông báo tổng kết
  cảnh báo (warning) nếu có bản ghi lỗi (`totalFailed > 0`).
- Lọc thực hiện phía client trên dữ liệu đã tải; với dữ liệu rất lớn cần cân nhắc lọc phía server.
- Toàn bộ chuỗi dùng i18n `lz.*.cctvEquipment.*`; nút xác nhận/hủy dùng `lz.button.base.*`.

## 7. Kiểm thử

```bash
pnpm test src/views/cctv/equipments
```

File `__tests__/cctvEquipments.test.ts` kiểm thử các hàm thuần (lọc, kiểm tra response, lấy khóa thiết bị…).

## 8. Phụ thuộc

- `CctvDeviceApi` (`/@/api-services/cctv`)
- `useVxeTable` (`/@/hooks/useVxeTableOptionsHook`)
- `useBaseConfig` (`/@/stores/baseConfig`) — danh mục Status
- Component dùng chung: `SearchButton`, `TagInfo`, `ModifyRecord`
