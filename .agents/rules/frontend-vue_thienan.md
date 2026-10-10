---
name: frontend-vue
version: 1.0.0
priority: P1
trigger: model_decision
description: Frontend Vue 3 standards, Element Plus, VxeTable, SCSS Dark/Light theme, CSS-first, and TypeScript conventions.
---

# 💻 Quy Chuẩn Frontend Vue 3 / TypeScript — Thiên Ân

> **Thuộc hệ thống SSOT `.agents/rules/`**. Quy định kiến trúc Vue 3, Element Plus, VxeTable, SCSS Dark/Light theme, TypeScript conventions và an toàn phát triển giao diện.

---

## 📁 1. Cấu Trúc Thư Mục & Component

```text
src/views/<module>/<feature>/
├── index.vue                   # Màn hình danh sách chính (VxeTable / Search Form)
└── component/
    ├── edit<Feature>.vue       # Dialog / Drawer Thêm mới & Cập nhật
    └── detail<Feature>.vue     # Modal / Drawer Xem chi tiết bản ghi
```

### API Services (Swagger Auto-Generated — CẤM TỰ SỬA TAY)
* Toàn bộ mã nguồn trong `src/api-services/` được sinh tự động bởi OpenAPI / NSwag từ Backend WebAPI.
* ⛔ **TUYỆT ĐỐI CẤM** tự tạo file API thủ công hoặc sửa tay vào `src/api-services/`.
* Khi Backend cập nhật API hoặc DTO: Chạy script generate đồng bộ để cập nhật tự động.

---

## 🎨 2. Quy Chuẩn CSS / SCSS & Theme Dark/Light (P0 Safeguards)

1. **CẤM Hardcode Mã Màu (`#fff`, `#000`, `#1578a3`, `red`)**:
   - BẮT BUỘC sử dụng CSS variables của Element Plus để đảm bảo tương thích 100% giữa Light Theme và Dark Theme:
     - Màu chủ đạo: `var(--el-color-primary)`, `var(--el-color-success)`, `var(--el-color-warning)`, `var(--el-color-danger)`.
     - Màu chữ: `var(--el-text-color-primary)`, `var(--el-text-color-regular)`, `var(--el-text-color-secondary)`.
     - Màu nền & viền: `var(--el-bg-color)`, `var(--el-fill-color-light)`, `var(--el-border-color)`.
2. **CẤM Lạm Dụng `!important` Trong CSS/SCSS (Rule 20.6)**:
   - ⛔ TUYỆT ĐỐI KHÔNG sử dụng `!important` bừa bãi trong CSS/SCSS (đặc biệt trong scoped styles và component).
   - *Lý do*: Phá vỡ CSS cascade chain, khiến trình duyệt tính toán lại cây kiểu gây giật lag và khó bảo trì.
   - *Giải pháp thay thế chuẩn*: Nâng cao độ ưu tiên tự nhiên (specificity) bằng lồng selector (ví dụ `.parent .child`), dùng class định danh cụ thể hoặc pseudo-classes.
3. **Quy Chuẩn Comment Trong SCSS**:
   - BẮT BUỘC dùng block comment dạng `/* comment */`.
   - ⛔ TUYỆT ĐỐI KHÔNG dùng comment một dòng `//` trong khối SCSS (gây lỗi phân tích cú pháp khi Vite build).
4. **Không Để Thừa Chấm Phẩy (`;;`)**: Đảm bảo cú pháp CSS sạch.

---

## 📐 3. Nguyên Tắc Sửa Giao Diện & Responsive (CSS-First)

1. **CSS-First (Rule 20.7)**:
   - Mọi lỗi hiển thị (placeholder bị che, icon che khuất, vỡ dòng, co rúm nút, tràn màn hình...) BẮT BUỘC xử lý bằng CSS/SCSS (Flexbox, Grid, Container Queries `@container`, Media Queries `@media`, CSS variables).
2. **CẤM Xóa Props / Thuộc Tính Template (Rule 19.27)**:
   - ⛔ TUYỆT ĐỐI KHÔNG tự ý gỡ bỏ các thuộc tính chuẩn của Element Plus (`show-word-limit`, `:maxlength`, `clearable`, `filterable`, `:body-style`...) để "né" việc căn chỉnh CSS.
   - Nếu không gian quá hẹp không thể hiển thị vừa cả nội dung và bộ đếm/nút, BẮT BUỘC hỏi ý kiến người dùng trước.
3. **Tách Biệt Logic và UI (Rule 19.29)**:
   - Khi được giao sửa giao diện, chỉ can thiệp vào CSS/SCSS và template layout. Không được chạm vào hàm tính toán nghiệp vụ hoặc call API nếu không có yêu cầu.

---

## 🪟 4. Chuẩn Modal / Dialog Thêm, Sửa & Kéo Thả (Draggable)

* **Tiêu đề Dialog**: Rõ ràng, có icon nhận diện ngữ cảnh, hỗ trợ đa ngôn ngữ `$t(...)`.
* **Draggable Dialog & Tránh Giật Toạ Độ (Bug #32 Resolution)**:
  - Khi sử dụng `<el-dialog draggable ref="dialogRef">`, việc reset vị trí khi mở/đóng hoặc thay đổi nội dung BẮT BUỘC sử dụng phương thức chuẩn của Element Plus:
    ```ts
    dialogRef.value?.resetPosition?.();
    ```
  - ⛔ **TUYỆT ĐỐI CẤM** can thiệp trực tiếp bằng tay vào `dialogEl.style.transform = ...`, vì sẽ làm lệch (desync) trạng thái đóng gói nội bộ của hook `useDraggable`, gây hiện tượng dialog bị giật/snap lại vị trí cũ khi kéo chuột.
  - Căn chỉnh vị trí top an toàn bằng CSS selector lồng `:deep(.el-overlay .el-dialog.mp-dialog)` mà không dùng `!important`.

---

## 🌐 5. Đa Ngôn Ngữ (i18n Scope Rules)

* **Phạm vi sửa file i18n**: Phía Frontend, lập trình viên **ĐƯỢC PHÉP** thêm/sửa key bản dịch trong `src/i18n/lang/vi-vn.json` và `en-us.json` khi phát triển giao diện.
  *(Lưu ý: Quy tắc cấm sửa tay file tĩnh chỉ áp dụng cho Backend WebAPI `src/TAC_WebAPI/Resources/*.json` do các file đó được sinh tự động từ CSDL `SysTerminology`).*
* **Ưu tiên lấy bản dịch**: Luôn ưu tiên dùng key bản dịch chuẩn từ backend resource hoặc file i18n (`src/i18n/lang/vi-vn.json` và `en-us.json`).
* **CẤM hardcode chuỗi text**: Mọi nhãn, thông báo, tiêu đề cột bảng BẮT BUỘC bọc qua `$t('lz.label....')`.

---

## 🧩 6. Quy Chuẩn Code TypeScript / JavaScript

1. **CẤM Sử Dụng Toán Tử `!!` (No Double Negation - Rule 20.8)**:
   - ⛔ TUYỆT ĐỐI CẤM viết cú pháp `!!` (như `!!startTime.value`, `!!row?.id`).
   - *Thay thế chuẩn*: Sử dụng hàm ép kiểu tường minh `Boolean(value)` hoặc guard clause tường minh:
     ```ts
     // ❌ CẤM:
     const hasId = !!row?.id;
     // ✅ CHUẨN:
     const hasId = Boolean(row?.id);
     const isNotEmpty = val != null && val !== '';
     ```
2. **Ngắt Dòng Thuộc Tính Template (Rule 20.9)**:
   - Khi component có từ 3 props/events trở lên, ngắt mỗi thuộc tính thành một dòng riêng biệt, thụt đầu dòng rõ ràng để dễ review Git diff.

---

## 🛑 7. Quy Tắc Vận Hành Client Dev (P0 Safeguard)

* **CẤM TỰ Ý CHẠY `npm run build` / `pnpm build` (Rule 19.13)**:
  - Phía Frontend, môi trường phát triển đã có Vite Dev Server chạy nền với tính năng Hot-Module Replacement (Hot-Reload / HMR).
  - Sau khi sửa code Vue/SCSS, AI **TUYỆT ĐỐI KHÔNG tự chạy build**, tránh gây treo máy và lãng phí thời gian.
  - Chỉ chạy build khi người dùng yêu cầu trực tiếp.
