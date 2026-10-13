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

### API Services (Swagger Auto-Generated — CẤM TỰ SỬA TAY & BẮT BUỘC DÙNG `getAPI`)
* Toàn bộ mã nguồn trong `src/api-services/` được sinh tự động bởi OpenAPI / Swagger Codegen (`pnpm run build-api` hoặc `pwsh api_build/build.ps1`) từ Backend WebAPI.
* ⛔ **TUYỆT ĐỐI CẤM** tự tạo file API thủ công hoặc sửa tay vào `src/api-services/`.
* ⛔ **TUYỆT ĐỐI CẤM** gọi trực tiếp `axiosInstance.get/post(...)` hoặc `axios.get/post(...)` và CẤM hardcode chuỗi URL (`${serveConfig.basePath}/api/...`) trong các component/view `.vue`.
* ✅ **BẮT BUỘC**: 100% các cuộc gọi API từ Frontend phải thông qua wrapper chuẩn: `getAPI(XxxApi)` từ `/@/utils/axios-utils`.
* Khi Backend cập nhật API hoặc DTO: Chạy script generate đồng bộ (`build-api`) để cập nhật tự động các client class typed.

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
5. **Quy Chuẩn Phạm Vi CSS (Component-Scoped First — CẤM Sửa Theme Toàn Dự Án Cho Module Con) (Rule 20.11)**:
   - Mọi tuỳ biến CSS, override giao diện bảng (VxeTable), form, dialog cho một màn hình/phân hệ nghiệp vụ cụ thể (như ShareData, TMS, VMS, VideoWall...) **BẮT BUỘC viết trong khối `<style scoped>` nội bộ của chính component/view đó** (hoặc tệp style riêng thuộc phân hệ).
   - ⛔ **TUYỆT ĐỐI CẤM** nhồi nhét selector hoặc class của component con/nghiệp vụ cụ thể vào các tệp theme/preset dùng chung toàn dự án (như `depthBlueAmber.scss`, `theme.scss`, `app.scss`).
   - *Lý do*: Sửa vào theme chung sẽ gây hiệu ứng lây lan (side-effects), làm thay đổi và vỡ giao diện ngoài ý muốn của các phân hệ khác trên toàn dự án.

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
* **CẤM hardcode chuỗi text & thông báo lỗi fallback (P0 Safeguard)**:
  - ⛔ **CẤM** viết chuỗi tiếng Việt/tiếng Anh trực tiếp trong template, placeholder, `:label`, `:title`, `:description`.
  - ⛔ **CẤM ĐẶC BIỆT**: Tuyệt đối không hardcode chuỗi thông báo lỗi fallback trong các khối `try-catch`, hàm kiểm tra hoặc `ElMessage` (ví dụ cấm viết: `ElMessage.error(err?.message || 'Lỗi khi lưu cấu hình SQL.')`, `state.error = result?.message || 'Cú pháp SQL không hợp lệ.'`).
  - ✅ **BẮT BUỘC**:
    - Khi có thông báo mới, **BẮT BUỘC** bổ sung ngay key tương ứng vào cả 2 file `src/src/i18n/lang/vi-vn.json` và `src/src/i18n/lang/en-us.json` (theo quy ước tiền tố `lz.message.<feature>.<actionFailed>`).
    - Gọi qua `$t(...)` làm giá trị fallback an toàn:
      ```ts
      // ❌ CẤM:
      ElMessage.error(err?.response?.data?.message || err?.message || 'Lỗi khi lưu cấu hình SQL.');

      // ✅ BẮT BUỘC:
      ElMessage.error(err?.response?.data?.message || err?.message || $t('lz.message.sharedataDataSource.saveSqlFailed'));
      ```

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

3. **Type Safety Cho Tham Số Modal/Dialog Nhận DTO Backend (Rule 20.10)**:
   - **Bối cảnh & Lỗi thường gặp**: Toàn bộ các model/DTO sinh từ Swagger / C# backend (`api-services/**`) đều có các thuộc tính dạng nullable/optional (`id?: string | null; code?: string | null | undefined; name?: string | null | undefined;`).
   - ⛔ **TUYỆT ĐỐI CẤM** khai báo chữ ký hàm mở modal/dialog (`openDialog`, `openDrawer`, `initData`) với kiểu đối tượng con non-nullable cứng nhắc (như `openDialog(row: { code: string; name: string })`). Việc này sẽ luôn kích hoạt lỗi compile TypeScript nghiêm ngặt:
     > `Argument of type '{ id?: ...; code?: string | null | undefined; ... }' is not assignable to parameter of type '{ code: string; name: string; }'. Types of property 'code' are incompatible. Type 'string | null | undefined' is not assignable to type 'string'.`
   - ✅ **QUY CHUẨN BẮT BUỘC**:
     - Trong component con, khai báo tham số nhận vào cho phép nullable/optional (`string | null | undefined`) hoặc dùng `any` theo đúng khung mẫu skeleton tại Mục 8:
       ```ts
       // ✅ Cách 1: Khai báo interface cho phép nullable kèm index signature
       export interface OpenDialogParam {
           id?: string | null;
           code?: string | null;
           name?: string | null;
           [key: string]: any;
       }
       const openDialog = (row: OpenDialogParam | any) => { ... };

       // ✅ Cách 2: Dùng row: any theo đúng khung mẫu chuẩn Thiên Ân
       const openDialog = (row: any) => { ... };
       ```
     - Khi gán dữ liệu vào state form nội bộ, **BẮT BUỘC dùng null-coalescing (`?? ''`)** để fallback an toàn:
       ```ts
       state.form.packetCode = row.code ?? '';
       state.form.packetName = row.name ?? '';
       ```
     - ⛔ **CẤM** ép kiểu mù quáng bằng `row.code as string` hoặc dùng toán tử non-null assertion `row.code!` để né lỗi type, vì sẽ gây crash ứng dụng runtime khi dữ liệu thực tế là `null` hoặc `undefined`.

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

---

### 3. Quy Chuẩn Ngắt Dòng Cho Thông Báo, Tooltip & Gợi Ý Nhiều Câu (Dấu Chấm '.') (Rule 20.12)
* Khi một chuỗi thông báo, tooltip (`el-tooltip`), gợi ý (`hint` / `guide`) hoặc ghi chú giải thích chứa từ 2 câu trở lên (ngăn cách bằng dấu chấm `.`):
  - ⛔ **CẤM** để dính liền các câu trên một dòng đơn lẻ tạo thành một khối văn bản dài dằng dặc gây mỏi mắt và khó đọc cho người dùng.
  - ✅ **BẮT BUỘC ngắt dòng (xuống dòng)** sau mỗi dấu chấm câu `.` để phân tách rõ ràng các ý (nguyên nhân/hiện trạng $\rightarrow$ hành động tiếp theo cần làm).
  - **Trong tệp i18n (`vi-vn.json`, `en-us.json`)**: Chèn ký tự xuống dòng `\n` ngay sau dấu chấm `.`, ví dụ:
    ```json
    "lz.label.sharedataSubscription.mappingNoneHint": "Chưa có hồ sơ ánh xạ cho gói tin và chiều này.\nVào màn Ánh xạ dữ liệu tạo hồ sơ rồi bật lên mới bật được đăng ký."
    ```
  - **Trong template Vue / `el-tooltip`**:
    - Sử dụng slot `<template #content><div style="white-space: pre-line">{{ ... }}</div></template>` hoặc cấu hình CSS `white-space: pre-line;` để trình duyệt diễn giải đúng ký tự `\n` ngắt dòng.

---

## 🧪 9. Quy Chuẩn Bắt Buộc Viết Test Sau Khi Fix Lặt Vặt (Zero Regression - Rule 18 / 19.67 - P0)

Mọi yêu cầu sửa/chỉnh lỗi dù là lặt vặt nhất ở Frontend (như căn chỉnh CSS, font chữ, màu sắc, khoảng cách padding/margin, chống giật layout modal/tab, placeholder, nhãn i18n, validate form...) **BẮT BUỘC** thực hiện quy trình sau:

1. **Viết/Bổ sung Test Ngay Sau Khi Sửa**:
   - Thêm test case tương ứng vào bộ kiểm thử Playwright tập trung tại `tests/FE/views/<module>/<feature>.test.ts` (Rule P0.2).
   - ⛔ **CẤM** kết thúc lượt làm việc hoặc báo "đã sửa xong" mà không bổ sung bài test xác minh.
2. **Hình thức kiểm thử**:
   - **Quét tĩnh mã nguồn (Static Code / CSS Scan)**: Kiểm tra file `.vue` không có `!important`, có đầy đủ CSS styles/classes yêu cầu, không dùng toán tử `!!`, chuẩn hóa font-family, có key i18n đầy đủ trong cả `vi-vn.json` và `en-us.json`.
   - **Kiểm thử E2E / Render DOM**: Mở component/dialog, đo kích thước phần tử (`getBoundingClientRect`), kiểm tra không bị co giật/layout shift khi chuyển tab hoặc đổi trạng thái.
3. **Thực Thi & Dọn Dẹp (Rule P0.15)**:
   - Chạy test Playwright để chứng minh bài test pass 100%: `npx playwright test views/<module>/<feature>.test.ts --project=chromium`.
   - Sau khi test pass, **tự động xóa bỏ các thư mục tạm (`test-results/`)** để giữ sạch Working Tree.



