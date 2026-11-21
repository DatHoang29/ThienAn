# Phân hệ Quản lý Template Bố cục (templates)

Quản lý các **template bố cục lưới** (vị trí/kích thước các ô camera) để tái sử dụng cho dashboard.
Trình chỉnh sửa bố cục dùng **GridStack** (kéo–thả, thay đổi kích thước) trên lưới chuẩn 6×6.

## 1. Cấu trúc

```
templates/
├── index.vue                          ← trang danh sách + dialog editor bố cục
├── utils/
│   ├── templateLayout.ts              ← LOGIC LAYOUT THUẦN (testable)
│   └── __tests__/templateLayout.property.test.ts
└── MANUAL_TEST_CHECKLIST.md           ← checklist test thủ công (tương tác GridStack)
```

## 2. Tính năng

| Tính năng | Mô tả |
| --- | --- |
| Danh sách + lọc | Theo tên template và trạng thái; mỗi dòng có **preview bố cục** thu nhỏ |
| Thêm mới | Dialog với editor GridStack; chọn nhanh preset số lượng (1/4/6/9/16) |
| Thêm / xóa item | Thêm ô vào vị trí trống đầu tiên; xóa từng ô (nút × khi hover) hoặc xóa tất cả |
| Kéo–thả / resize | Di chuyển và đổi kích thước ô; ô luôn được ép về **hình vuông**, không đè nhau |
| Chỉnh sửa | Sửa tên, ghi chú, trạng thái và cả bố cục của template đã lưu |
| Tạo bản sao | Nhân bản template có sẵn (prefill tên `… - Copy`), cho sửa toàn bộ rồi lưu mới |
| Copy bố cục | Trong dialog, nạp bố cục từ một template khác để chỉnh tiếp |

## 3. Kiến trúc: tách logic thuần ra `utils/templateLayout.ts`

Để test được và tránh phụ thuộc DOM/GridStack, toàn bộ thuật toán bố cục nằm ở module thuần:

| Hàm | Vai trò |
| --- | --- |
| `isOverlapping(a, b)` | Hai hình chữ nhật có giao nhau không (AABB) |
| `isWithinGrid(rect)` | Rect có nằm trọn trong lưới không |
| `canPlaceRect(others, x, y, w, h)` | Có thể đặt rect tại vị trí (trong lưới + không đè) |
| `findFreePosition(existing, w, h)` | Tìm ô trống đầu tiên theo thứ tự đọc (trên→dưới, trái→phải) |
| `generateEqualGrid(count)` | Sinh bố cục lưới đều cho N camera (clamp `1..MAX_SLOTS=36`) |
| `resolveSquareSize(rect, others)` | Tính cạnh vuông hợp lệ sau resize (collision-aware) |
| `renumberByReadingOrder(slots)` | Đánh số lại `slotNo = 1..N` theo thứ tự đọc |
| `isLayoutValid(slots)` | Kiểm tra toàn bộ bố cục (trong lưới + không đè) |

Hằng số mặc định: `GRID_COLUMNS = 6`, `GRID_ROWS = 6`, `MAX_SLOTS = 36`.
`index.vue` import các hàm này và chỉ giữ phần điều khiển GridStack/DOM.

> **Số cột/hàng lấy từ DB:** các hàm trên nhận `cols/rows` qua tham số (mặc định 6×6).
> Khi `onMounted`, `index.vue` gọi `loadCctvGridDimensions()` (`dashboard/services/cctvDashboardConfig.ts`)
> để lấy số cột/hàng từ **cùng cấu hình DB với dashboard** (`cctv_dashboard_grid_v1_w` / `_h`),
> lưu vào ref `gridColumns` / `gridRows` và truyền vào các hàm + khởi tạo GridStack.
> Nhờ đó bố cục template **luôn khớp lưới dashboard**; đổi số hàng/cột trong DB là editor template tự theo.

## 4. Vòng đời GridStack trong dialog (`index.vue`)

```
@opened  → handleDialogOpened → initEditorGrid()   (khởi tạo GridStack, nạp items)
thao tác → change/dragstop/resizestop → renumberAndSync() / enforceSquareResize()
@close   → handleDialogClose → destroyEditorGrid()  (hủy grid, gỡ listener)
submit   → renumberAndSync() → apiCctvTemplateAdd/UpdatePost
```

Các hàm điều khiển chính:

| Hàm | Vai trò |
| --- | --- |
| `initEditorGrid()` | Khởi tạo GridStack 6×6; ghi đè `GridStack.renderCB` để render HTML (số ô + nút xóa) |
| `loadItemsToGrid(items)` | Xóa sạch và dựng lại widget (dùng `removeAll(true)` + `batchUpdate`) |
| `renumberAndSync()` | Đồng bộ vị trí về state + đánh số lại theo thứ tự đọc |
| `enforceSquareResize()` | Ép ô về vuông, có kiểm tra va chạm (`resolveSquareSize`) |
| `handleAddSlot()` | Thêm 1 ô 1×1 vào vị trí trống (`findFreePosition`) |
| `removeWidgetByUid(uid)` | Xóa 1 ô theo uid (delegation từ nút ×) |
| `applyPreset(count)` | Dựng lại bố cục theo số lượng chọn nhanh |
| `handleCopyFromTemplate(id)` | Nạp bố cục từ template khác |
| `handleDuplicate(row)` | Mở dialog tạo bản sao (id rỗng để lưu thành template mới) |

## 5. Điểm cần lưu ý (GridStack v12) — đã xử lý

- `removeAll(removeDOM)` và `removeWidget(el, removeDOM)` mặc định `removeDOM=true`; **phải truyền `true`**
  để xóa cả DOM (truyền `false` sẽ để lại phần tử trên giao diện gây "ô ma").
- `GridStack.renderCB` mặc định dùng `textContent` (chống XSS) → cần override sang `innerHTML`
  để hiển thị nội dung HTML (số ô + nút ×). Nội dung do code tự sinh, không chứa input người dùng.
- Cờ `isProgrammaticUpdate` chặn xử lý sự kiện `change` khi đang cập nhật grid bằng code (tránh vòng lặp).
- `uid` của widget tách biệt với `slotNo` hiển thị; `slotNo` được đánh lại theo vị trí sau mỗi thay đổi
  nên luôn liền mạch `1..N` dù xóa ô ở giữa.

## 6. Mô hình dữ liệu

- API: `TemplateApi` (`apiCctvTemplateTemplatelistGet`, `…AddPost`, `…UpdatePost`, `…DeletePost`).
- Model: `TemplateOutput { id, name, remark, status, items: TemplateSlot[], createTime, updateTime }`.
- `TemplateSlot { slotNo, x, y, w, h }` map 1–1 với node GridStack `{ id, x, y, w, h }`.

## 7. Kiểm thử

```bash
pnpm test src/views/cctv/templates     # unit + property test (templateLayout)
```

- Logic thuần: `templateLayout.property.test.ts` (unit + property-based với fast-check).
- Tương tác DOM/kéo–thả: theo `MANUAL_TEST_CHECKLIST.md` (10 nhóm case).

## 8. Phụ thuộc

- `gridstack` (^12) + CSS `gridstack/dist/gridstack.min.css`
- `TemplateApi` (`/@/api-services/cctv`)
- `useVxeTable`, `useBaseConfig`, component `SearchButton` / `TagInfo`
