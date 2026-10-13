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

---

## 🏛️ 8. Quy Chuẩn Viết Component Dialog / Drawer & Cấm Tự Chế Entrypoint / Local i18n

Để đảm bảo 100% component đồng bộ tuyệt đối về trải nghiệm, cấu trúc và khả năng bảo trì với toàn bộ codebase dự án, mọi lập trình viên và AI Agent **BẮT BUỘC** tuân thủ các quy tắc sau:

### 1. Khung Mẫu (Skeleton) Bắt Buộc Cho Component Dialog / Drawer
Mọi dialog form thêm/sửa/xem trước (`component/edit<Feature>.vue`, `component/<feature>Dialog.vue`) **BẮT BUỘC** tuân theo cấu trúc chuẩn:

```vue
<template>
	<div class="<tên-chức-năng>-container">
		<el-dialog
			ref="dialogRef"
			v-model="state.isShowDialog"
			draggable
			:close-on-click-modal="false"
			:show-close="!state.isProcessing"
			:close-on-press-escape="!state.isProcessing"
			width="750px"
		>
			<template #header>
				<div style="color: var(--el-color-white)">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle">
						<ele-Edit />
					</el-icon>
					<span>{{ props.title }}</span>
				</div>
			</template>

			<el-form
				ref="editFormRef"
				:model="state.editForm"
				:disabled="state.isProcessing"
				label-width="auto"
				label-position="left"
			>
				<el-row :gutter="10">
					<el-col :xs="24" :sm="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.code')" prop="code">
							<el-input v-model="state.editForm.code" clearable />
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>

			<template #footer>
				<span class="dialog-footer">
					<el-button @click="cancel" :disabled="state.isProcessing">
						{{ $t('lz.button.base.cancel') }}
					</el-button>
					<el-button type="primary" :disabled="state.isProcessing" @click="submit">
						{{ $t('lz.button.base.confirm') }}
					</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="esh<ComponentName>">
import { reactive, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import type { OperateType } from '/@/types/constants';

const { t: $t } = useI18n(); // Luôn dùng Global i18n
const dialogRef = ref();
const editFormRef = ref();

interface Props {
	operateType?: OperateType;
	title: string;
}
const props = defineProps<Props>();
const emits = defineEmits(['handleQuery']);

const state = reactive({
	isShowDialog: false, // ⛔ BẮT BUỘC là isShowDialog, CẤM đặt isShow
	isProcessing: false,
	editForm: {} as any,
});

const openDialog = (row: any) => {
	editFormRef.value?.resetFields();
	state.editForm = JSON.parse(JSON.stringify(row));
	state.isShowDialog = true;
	dialogRef.value?.resetPosition?.();
};

const closeDialog = () => {
	emits('handleQuery');
	state.isShowDialog = false;
};

const cancel = () => {
	state.isShowDialog = false;
};

const submit = async () => {
	// Xử lý lưu và gọi closeDialog()
};

defineExpose({ openDialog });
</script>
```

### 2. Bốn Điều Cấm Kỵ Cốt Lõi Khi Viết Component (P0 Safeguards)
1. ⛔ **CẤM Tự Chế Tệp HTML & CSS Rời Rạc Ngoài Luồng Ứng Dụng**:
   - Tuyệt đối không tạo file `.html` tĩnh (như `*prototype.html`) hoặc file `.css` ngoài component để làm bản thử nghiệm ad-hoc. Mọi tính năng đều phải được tích hợp chuẩn chỉnh vào luồng SFC Vue, layout và router của hệ thống.
2. ⛔ **CẤM Dùng i18n Local Scope (`useScope: 'local'`) & Tệp Messages Riêng**:
   - Toàn bộ dự án sử dụng Single Source of Truth: `const { t: $t } = useI18n()`.
   - Tất cả key bản dịch BẮT BUỘC đặt tập trung tại `src/src/i18n/lang/vi-vn.json` và `en-us.json` theo đúng cấu trúc tiền tố (`lz.entity.*`, `lz.label.*`, `lz.button.*`, `lz.message.*`, `lz.placeholder.*`).
   - Tuyệt đối không tạo tệp `*Messages.ts` riêng hoặc dùng `useI18n({ useScope: 'local' })`.
3. ⛔ **CẤM Tự Sáng Tác Naming State & Footer Lạ Lẫm**:
   - Biến cờ modal BẮT BUỘC là `state.isShowDialog` (không dùng `state.isShow`).
   - Cụm nút chân trang BẮT BUỘC là `<span class="dialog-footer">` chứa `cancel()` và `submit()`. Không đóng modal bằng cách gán inline `@click="state.isShow = false"`.
4. ⛔ **CẤM Lạm Dụng Custom CSS Grid Phá Vỡ Hệ Thống Giao Diện Element Plus**:
   - Giao diện form và bố cục modal BẮT BUỘC dùng `<el-row :gutter="10">`, `<el-col :xs="24" :sm="..." class="mb20">`, các class tiện ích toàn cục (`w100`, `mb5`, `mb20`, `mr5`, `ml10`, `rs-note`).
   - Không tự chế các class bố cục dị biệt (`.step-pill`, `.prototype-main-grid`) gây lệch tông giao diện so với các màn hình chuẩn khác của hệ thống.

