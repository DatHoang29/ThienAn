# WebVue Frontend Architecture & Index Steering Instructions

## 1. Quick Index & Architecture Reference
Trước khi tìm kiếm hay quét file cho Frontend, **LUÔN LUÔN** tham khảo các file index và quy chuẩn sẵn có tại thư mục `.kiro/steering/`:
- **Cấu trúc Thư mục & Views**: Đọc [structure.md](.kiro/steering/structure.md) để biết ngay vị trí các module views, components, store (Pinia), router, utils.
- **Quy chuẩn Code & Giao diện**: Đọc [coding-rules.md](.kiro/steering/coding-rules.md) để tuân thủ conventions về Vue 3, VxeTable, Element Plus, i18n, icons, naming props/events.
- **Tech Stack & Libraries**: Đọc [tech.md](.kiro/steering/tech.md) (Vue 3 Script Setup, Vite, TypeScript/JavaScript, VxeTable, Pinia).
- **Nghiệp vụ sản phẩm**: Đọc [product.md](.kiro/steering/product.md).

## 2. Nguyên tắc đọc và sửa code hiệu quả (Tối ưu phản hồi)
- **Không duyệt quét thư mục mù (Do not blindly scan directories)**: Dựa vào [structure.md](.kiro/steering/structure.md) để định vị trực tiếp component, view hoặc API client cần sửa.
- **Đọc theo chỉ số dòng (Line slicing)**: Sử dụng `view_file` với `StartLine` và `EndLine` cho từng template block, script block hoặc style block cần sửa, tránh đọc tràn lan các file vue lớn.
- **Tuân thủ chuẩn Vue 3 + VxeTable**:
  - Tận dụng `vxe-grid` / `vxe-table` theo các patterns đã cấu hình.
  - Sử dụng đa ngôn ngữ qua i18n thay vì hardcode text.
