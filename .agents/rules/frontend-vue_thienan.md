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

### 🏷️ Quy Chuẩn Đặt Tên Tệp Component (`camelCase` Bắt Buộc - CẤM `PascalCase`) (Rule 20.14)
* **Tên tệp `.vue`**:
  - Tên tất cả các tệp component con trong thư mục `src/views/<module>/<feature>/component/` và `src/components/` **BẮT BUỘC viết theo chuẩn `camelCase` (chữ cái đầu viết thường)**.
  - ✅ **Ví dụ chuẩn**:
    - `visualQueryBuilder.vue`
    - `editPacket.vue`, `editPacketField.vue`, `editPartner.vue`, `editSubscription.vue`
    - `activityDetailDialog.vue`, `alertDetailDrawer.vue`, `subscriptionTable.vue`
  - ⛔ **TUYỆT ĐỐI CẤM** đặt tên tệp component viết hoa chữ cái đầu (PascalCase) như:
    - ❌ `VisualQueryBuilder.vue` $\rightarrow$ Đổi thành `visualQueryBuilder.vue`.
    - ❌ `EditPacket.vue` $\rightarrow$ Đổi thành `editPacket.vue`.
    - ❌ `SubscriptionTable.vue` $\rightarrow$ Đổi thành `subscriptionTable.vue`.
* **Thuộc tính name trong `<script lang="ts" setup name="...">`**:
  - Đồng bộ viết theo `camelCase` (ví dụ `name="visualQueryBuilder"` hoặc `name="sharedataVisualQueryBuilder"`), không viết hoa chữ cái đầu.
* **Quy ước Import & Sử dụng trong Template**:
  - Khi import trong script có thể khai báo định danh PascalCase (`const VisualQueryBuilder = defineAsyncComponent(...)`) để gắn vào template (`<VisualQueryBuilder ... />`) theo quy ước Vue, nhưng đường dẫn tệp `import(...)` **BẮT BUỘC trỏ chính xác đến tên tệp `camelCase`**.

### API Services (Swagger Auto-Generated — CẤM TỰ SỬA TAY & BẮT BUỘC DÙNG `getAPI`)
* Toàn bộ mã nguồn trong `src/api-services/` được sinh tự động bởi OpenAPI / Swagger Codegen (`pnpm run build-api` hoặc `pwsh api_build/build.ps1`) từ Backend WebAPI.
* ⛔ **TUYỆT ĐỐI CẤM** tự tạo file API thủ công hoặc sửa tay vào `src/api-services/`.
* ⛔ **TUYỆT ĐỐI CẤM** gọi trực tiếp `axiosInstance.get/post(...)` hoặc `axios.get/post(...)` và CẤM hardcode chuỗi URL (`${serveConfig.basePath}/api/...`) trong các component/view `.vue`.
* ✅ **BẮT BUỘC**: 100% các cuộc gọi API từ Frontend phải thông qua wrapper chuẩn: `getAPI(XxxApi)` từ `/@/utils/axios-utils`.
* Khi Backend cập nhật API hoặc DTO: Chạy script generate đồng bộ (`build-api`) để cập nhật tự động các client class typed.

---

## 🎨 2. Quy Chuẩn CSS / SCSS & Theme Dark/Light

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
6. **Vị Trí Đặt Code CSS/SCSS (Rule 20.16)**:
   - Code CSS mới đặt ở cuối cùng khối `<style>`; chỉnh sửa CSS hiện có thì sửa in-place tại chỗ. Chi tiết xem mục 6.5.

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
* **CẤM hardcode text mẫu, chuỗi giao diện & thông báo lỗi fallback — 100% đưa vào i18n (Rule 20.15)**:
  - ⛔ **CẤM** viết chuỗi tiếng Việt/tiếng Anh trực tiếp trong template, placeholder, `:label`, `:title`, `:description`, tooltip, nhãn nút bấm, nhãn mẫu mặc định (như `Mẫu mặc định`, `Đã nạp ...`, `Đã sao chép ...`, `Trực quan`, `Mã nguồn SQL`, `Thêm cột`, `Sắp xếp`...), hoặc trong script (`ElMessage.success('Đã sao chép...')`).
  - ⛔ **CẤM ĐẶC BIỆT**: Tuyệt đối không hardcode chuỗi thông báo lỗi fallback trong các khối `try-catch`, hàm kiểm tra hoặc `ElMessage` (ví dụ cấm viết: `ElMessage.error(err?.message || 'Lỗi khi lưu cấu hình SQL.')`, `state.error = result?.message || 'Cú pháp SQL không hợp lệ.'`).
  - ✅ **BẮT BUỘC**:
    - 100% text hiển thị cho người dùng phải được định nghĩa bằng key trong `src/src/i18n/lang/vi-vn.json` (tiếng Việt thuần túy) và `src/src/i18n/lang/en-us.json` (tiếng Anh thuần túy).
    - Gọi qua `$t(...)` (`const { t: $t } = useI18n()`) làm giá trị fallback an toàn:
      ```ts
      // ❌ CẤM:
      ElMessage.error(err?.response?.data?.message || err?.message || 'Lỗi khi lưu cấu hình SQL.');

      // ✅ BẮT BUỘC:
      ElMessage.error(err?.response?.data?.message || err?.message || $t('lz.message.sharedataDataSource.saveSqlFailed'));
      ```
    - Mọi màn hình/component mới hoặc sửa đổi đều phải có bài test Playwright quét tĩnh (Static Code Scan) bảo đảm không chứa text thô cứng nhắc.
* **CẤM viết nhãn UI song ngữ lai tiếng Anh trong ngoặc (Rule 20.13)**:
  - ⛔ **CẤM** viết nhãn, tiêu đề tab/card, nút bấm, placeholder, option label trên HTML/Vue theo kiểu song ngữ lai tiếng Việt kèm tiếng Anh trong ngoặc (như `Trực quan (Builder)`, `Mã nguồn SQL (Raw)`, `Cột trích xuất (SELECT)`, `Điều kiện lọc (WHERE)`, `Sắp xếp (ORDER BY)`, `Tăng dần (ASC)`, `Giảm dần (DESC)`).
  - 📌 **Quy tắc chú giải tiếng Anh trong ngoặc `[Tiếng Việt] ([English])` CHỈ ÁP DỤNG cho tài liệu kỹ thuật Markdown (`.md`)** (như Spec, Architecture, MasterPlan, Q&A) nhằm hỗ trợ lập trình viên đối chiếu thuật ngữ nghiệp vụ.
  - ✅ **BẮT BUỘC trên giao diện HTML/Vue**: Ngôn ngữ nào ra ngôn ngữ đó qua hệ thống i18n:
    - Trong tệp `src/src/i18n/lang/vi-vn.json` (và giá trị fallback trong template): Dùng tiếng Việt chuẩn mực, thuần túy (ví dụ: `Trực quan`, `Mã nguồn SQL`, `Cột trích xuất:`, `Điều kiện lọc:`, `Sắp xếp:`, `Tăng dần`, `Giảm dần`).
    - Trong tệp `src/src/i18n/lang/en-us.json`: Dùng tiếng Anh chuẩn xác, tự nhiên (ví dụ: `Visual Builder`, `Raw SQL`, `Columns:`, `Filter Conditions:`, `Order By:`, `Ascending`, `Descending`).
* **Quy chuẩn vị trí đặt key i18n mới (Rule 20.19)**:
  - Key mới chèn vào dòng cuối cùng của `vi-vn.json` và `en-us.json`; chỉnh sửa key hiện có thì sửa in-place cùng hàng. Chi tiết xem mục 6.5.

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

4. **Khai Báo `defineProps` Trực Tiếp Trong Component — CẤM Tạo `types.ts` Lẻ Cho Component (Rule 20.17)**:
   - **Quy chuẩn khai báo Props**:
     - Toàn bộ định nghĩa interface Props (`interface Props { ... }`) và cú pháp `defineProps<Props>()` hoặc `const props = withDefaults(defineProps<Props>(), { ... })` **BẮT BUỘC khai báo trực tiếp bên trong khối `<script setup lang="ts">` của component `.vue`**.
     - ⛔ **TUYỆT ĐỐI CẤM** tạo file `types.ts` riêng lẻ chỉ để chứa props hoặc vài kiểu dữ liệu cục bộ của 1 component con làm rác cấu trúc thư mục.
     - Không tạo các file `types.ts` (hoặc `props.ts`) riêng lẻ trong thư mục component chỉ để chứa vài kiểu dữ liệu hoặc interface props của 1 component (ví dụ: cấm tạo `components/<name>/types.ts` hoặc `component/types.ts` chỉ để định nghĩa Props).
     - Không tách lẻ file kiểu gây phân mảnh mã nguồn, rác cây thư mục và phải import gián tiếp không cần thiết.
   - **Tái sử dụng kiểu dữ liệu (Re-exporting Types)**:
     - Nếu component khác hoặc view cha cần tái sử dụng type/enum liên quan đến component con, hãy export trực tiếp type đó từ `<script setup lang="ts">` của component (ví dụ: `export type ScheduleKind = ...;` ngay trong component `.vue`), và phía component sử dụng chỉ cần import cú pháp chuẩn: `import ComponentName, { type ScheduleKind } from './componentName.vue'`.

5. **Vị Trí Đặt Code Mới (CSS, Script, Key i18n) (Rule 20.16, 20.18, 20.19)**:
   - **Nguyên tắc chung**: Code mới thêm vào BẮT BUỘC đặt ở dòng cuối cùng của khối tương ứng (move dòng cuối hết); chỉnh sửa thành phần đã có thì BẮT BUỘC giữ nguyên vị trí tại chỗ (in-place cùng hàng), cấm di dời lung tung làm xáo trộn Git diff.
   - **CSS / SCSS (Rule 20.16)**: Mọi block selector/class CSS mới thêm vào BẮT BUỘC đặt ở cuối cùng của khối `<style>` (tận dụng thứ tự cascade tự nhiên, không cần `!important`). Chỉnh sửa thuộc tính trong selector đã có thì giữ nguyên vị trí tại chỗ khối selector đó.
   - **Script Trong `<script setup>` (Rule 20.18)**: Mọi hàm, computed property, helper function mới thêm vào BẮT BUỘC đặt ở cuối cùng của khối `<script setup>` (ngay trước `defineExpose` hoặc trước thẻ đóng `</script>`). ⛔ **TUYỆT ĐỐI CẤM** chèn code mới vào giữa các khối logic hiện có (như nhét vào giữa khai báo `state = reactive(...)` và các hàm mở form). Sửa hàm/biến có sẵn thì giữ nguyên in-place cùng hàng.
   - **Key i18n JSON (Rule 20.19)**: Khi bổ sung key bản dịch mới vào `src/src/i18n/lang/vi-vn.json` và `en-us.json`, **BẮT BUỘC chỉ chèn key mới vào DÒNG CUỐI CÙNG của tệp JSON** (ngay trước dấu đóng `}`). ⛔ **TUYỆT ĐỐI CẤM** di chuyển, tái sắp xếp, xáo trộn hoặc xóa các key đã tồn tại từ trước trong file. Chỉnh sửa key đã có (sửa câu từ, thêm `\n`) thì sửa tại chỗ (in-place cùng hàng).

6. **TypeScript Clean & Tối Giản — CẤM Ép Kiểu Rườm Rà Cho Khởi Tạo State (Rule 20.20)**:
   - **Bối cảnh & Quy chuẩn khởi tạo**: Khi khai báo các thuộc tính trong `reactive({ ... })`, `ref(...)` hoặc biến cục bộ, viết trực diện, tối giản, ngắn gọn theo chuẩn JavaScript/TypeScript:
     ```ts
     // ✅ CHUẨN CLEAN CODE:
     const state = reactive({
         apiSyncKind: null,
         lookbackDays: null,
         syncKind: 1,
         cursorColumn: '',
         isProcessing: false,
     });
     ```
   - ⛔ **TUYỆT ĐỐI CẤM** viết ép kiểu rườm rà, thừa thãi (Over-engineered type assertion) khi TypeScript không hề báo lỗi:
     - ❌ CẤM: `apiSyncKind: null as number | null,` $\rightarrow$ ✅ CHUẨN: `apiSyncKind: null,`
     - ❌ CẤM: `lookbackDays: null as number | null,` $\rightarrow$ ✅ CHUẨN: `lookbackDays: null,`
     - ❌ CẤM: `syncKind: 1 as number | null,` $\rightarrow$ ✅ CHUẨN: `syncKind: 1,`
     - ❌ CẤM: `cursorColumn: '' as string,` $\rightarrow$ ✅ CHUẨN: `cursorColumn: '',`
     - ❌ CẤM: `error: '' as string,` $\rightarrow$ ✅ CHUẨN: `error: '',`
   - *Nguyên tắc*: Trừ khi TypeScript thực sự báo lỗi kiểu bắt buộc (strict compiler error) không thể suy diễn được, luôn ưu tiên cú pháp đơn giản, sạch sẽ nhất (Clean Code), không lạm dụng `as Type | null` làm rác code và gây khó đọc.

7. **Bắt Buộc Kiểm Tra Kiểu (Type Safety) — Tránh Tuyệt Đối Lỗi Đỏ Trong IDE / TS Plugin (Rule 20.21)**:
   - **Mục tiêu**: Đảm bảo 100% code mới hoặc code được chỉnh sửa không phát sinh bất kỳ lỗi đỏ nào từ TypeScript Language Server / IDE (như `ts-plugin(2367)`, `ts(2322)`, `ts(2339)`).
   - **Các quy tắc kiểm tra kiểu bắt buộc**:
     1. **Tránh lỗi `no overlap` (`ts-plugin(2367)`) do so sánh sai kiểu**:
        - Khi một biến/thuộc tính mang kiểu enum số (như `status?: StatusEnum | undefined`): **BẮT BUỘC chỉ so sánh với enum hoặc số tương ứng** (`status === StatusEnum.NUMBER_0` hoặc `status === 0`).
        - ⛔ **TUYỆT ĐỐI CẤM** so sánh biến enum số với chuỗi (`status === '0'` hay `status === 'Disable'`) hoặc boolean (`status === false`) vì hai kiểu không có điểm chung (no overlap), sẽ gây lỗi gạch chân đỏ `ts(2367)` trong VS Code.
     2. **Tránh lỗi thuộc tính không tồn tại (`ts(2339)`) trên DTO**:
        - Nếu truy cập một trường phụ mở rộng không nằm trong DTO Swagger backend (ví dụ: `syncKind` trong `ShareDataUpdatePacketInput`), **BẮT BUỘC ép kiểu cục bộ an toàn**: `(state.editForm as any).syncKind` để tránh báo đỏ.
     3. **Tự kiểm tra trước khi bàn giao (Pre-handoff Type Audit)**:
        - Trước khi hoàn tất mọi thay đổi code TypeScript / Vue, AI phải tự rà soát kiểu dữ liệu của các biểu thức điều kiện `if (...)`, `===`, toán tử ba ngôi `? :` để bảo đảm kiểu dữ liệu hai vế tương thích (overlap) 100%, tuyệt đối không để sót lỗi gạch chân đỏ trong IDE của lập trình viên.

---

## 🛑 7. Quy Tắc Vận Hành Client Dev

* **CẤM TỰ Ý CHẠY `npm run build` / `pnpm build` (Rule 19.13)**: Phía Frontend đã có Vite Dev Server chạy nền tích hợp sẵn Hot-Reload (HMR). Sau khi sửa code FE, AI ⛔ **TUYỆT ĐỐI KHÔNG tự chạy build** (tránh treo máy và lãng phí thời gian); chỉ build khi người dùng yêu cầu trực tiếp.

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

### 2. Bốn Điều Cấm Kỵ Cốt Lõi Khi Viết Component
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

## 🧪 9. Quy Chuẩn Bắt Buộc Viết Test Sau Khi Fix Lặt Vặt (Zero Regression - Rule 19.67)

Chi tiết quy trình, hình thức kiểm thử (quét tĩnh CSS/template, đo `getBoundingClientRect`) và lệnh thực thi: xem [testing-rules_thienan.md](./testing-rules_thienan.md) §3.



