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

* **Thư mục module** (tích hợp từ Kiro FE, 20/10/2026): `views/tms/`, `views/vms/`, `views/fms/`, `views/system/`, `views/bigScreen/` (màn hình toàn màn hình), `views/login/`; thư mục chức năng viết `camelCase` (`dutySchedule`, `equipmentType`). Đường dẫn rút gọn `/@/` trỏ tới `src/src/` (`import { getAPI } from '/@/utils/axios-utils'`).
* **Mẫu `<script setup>`**: `<script lang="ts" setup name="tmsEquip">`, `const { t: $t } = useI18n();`. Tên component đặt bằng `name` (plugin `vite-plugin-vue-setup-extend`) theo dạng `<module><ChứcNăng>` camelCase (`tmsEquip`, `tmsEditEquip`, `vmsImage`).
* **Nạp component chậm**: component nâng cao dùng `defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'))` (cùng `search-button-operation.vue`, `tag-info.vue`).
* **Props/Emits/State**: khai `interface Props` + `defineProps<Props>()` (cần mặc định thì `withDefaults`); `interface Emits { (e: 'handleQuery'): void }` + `defineEmits<Emits>()`; mở hàm cho cha bằng `defineExpose({ openDialog })`; trạng thái gom trong `const state = reactive({ isShowDialog, ruleForm, queryParams, loading, ... })`.

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

### API Services (Swagger Auto-Generated — CẤM TẠO SERVICE RIÊNG, CẤM RAW AXIOS & BẮT BUỘC DÙNG `getAPI`) (Rule 20.28)
* Toàn bộ mã nguồn trong `src/api-services/` được sinh tự động bởi OpenAPI / Swagger Codegen (`pnpm run build-api` hoặc `pwsh api_build/build.ps1`) từ Backend WebAPI.
* **BE thêm / sửa API, DTO hoặc enum thì BẮT BUỘC chạy `pnpm run build-api` (27/10/2026)**: chạy khi WebAPI đang chạy đúng bản BE mới (lệnh đọc Swagger của WebAPI rồi ghi đè toàn bộ `src/api-services/`), xem `git diff` và chỉ giữ thay đổi của phân hệ mình (phân hệ khác lệch thì báo người phụ trách, không commit thay). Lệnh không tự chạy khi `dev` / `build`. Viết tay (mục dưới) chỉ là tạm khi chưa chạy được; lần sinh kế tiếp thay hẳn phần viết tay. Đã gặp 27/10/2026: `models/index.ts` thiếu dòng export (`IsRequired`) làm màn Ánh xạ trắng trang; còn sót model của API đã bỏ (`packet-write-sql-item`…).
* ⛔ **TUYỆT ĐỐI CẤM** tự ý tạo thêm tầng service riêng lẻ (như `packetSqlService.ts`, `xxxService.ts`) trong `views/` hoặc `services/` làm phân mảnh kiến trúc. BẮT BUỘC tra cứu cách các view/component khác trong cùng phân hệ gọi API để bám sát và đồng nhất 100%.
* ⛔ **TUYỆT ĐỐI CẤM** gọi trực tiếp `axiosInstance.get/post(...)` hoặc `axios.get/post(...)` và CẤM hardcode chuỗi URL (`${serveConfig.basePath}/api/...`) trong các component/view `.vue`.
* ✅ **BẮT BUỘC**: 100% các cuộc gọi API từ Frontend phải thông qua wrapper chuẩn: `getAPI(XxxApi)` từ `/@/utils/axios-utils`.
* Khi Backend cập nhật API hoặc DTO: Chạy script generate đồng bộ (`build-api`) để cập nhật tự động các client class typed.
* **API hoặc kiểu dữ liệu chưa kịp sinh**: Bổ sung method chuẩn Swagger vào class `XxxApi` tại `api-services/<module>/apis/` (gồm 4 khối: `AxiosParamCreator`, `Fp`, `Factory`, `Class`) để component gọi thông qua `getAPI(XxxApi)`. Tuyệt đối không lách bằng cách gọi thô `axiosInstance` hay tạo file service ngoài luồng.
* **Hàm viết tay phải giống hệt hàm Swagger sinh (bổ sung 25/10/2026)**: ưu tiên chạy `build-api`; chỉ viết tay khi chưa sinh được, và khi đó ở **cả 4 khối** (`AxiosParamCreator`, `Fp`, `Factory`, `Class`) mỗi hàm BẮT BUỘC có:
  - Khối chú thích ngay phía trên, đúng mẫu Swagger: `@summary <mô tả>`, một dòng `@param` cho **từng** tham số (kể cả `[body]` và `[options]`), `@throws {RequiredError}` (khối `Class` thêm `@memberof <Lớp>Api`).
  - Kiểu thật của tham số: `body?: <TênInput>` lấy từ `api-services/<module>/models/` (chưa có model thì thêm tệp model và dòng `export` ở `models/index.ts`), ⛔ **CẤM** `body?: any`. Tham số truy vấn ghi đúng kiểu (`packetCode?: string`).
  - Tên tham số query (`localVarQueryParameter['…']`) viết **đúng như tên thuộc tính DTO C#** (PascalCase: `PacketCode`, `PartnerId`, `ID`) giống mã Swagger sinh ra; ⛔ **CẤM** tự đổi sang camelCase khi thêm tay (27/10/2026: `getsqlvariables` / `getwritesqls` / `getsqldetail` từng lệch kiểu).
* **Kiểm kết quả gọi API (27/10/2026)**: kiểu `WResult<T>` sinh từ Swagger chỉ có `code`, `message`, `succeeded`, `result`, `time` — ⛔ **CẤM** viết `res.data?.type === 'success'` (không có trường `type`, TS báo lỗi; với API trả `void` còn thành `Property 'type' does not exist on type 'never'`). Interceptor `axios-utils` đã ném lỗi khi `code !== 200`, nên: API có dữ liệu → kiểm `res.data?.result`; API trả `void` → chỉ `await`, chạy tới dòng sau là thành công. Model sinh sẵn vừa đổi trên đĩa mà VS Code vẫn báo thiếu trường thì chạy **TypeScript: Restart TS Server** trước khi sửa.
  - Đã gặp 25/10/2026: 11 hàm SQL/biến của `ShareDataPacketApi` (`...UpdatepartnerextractsqlPost`, `...GetsqlvariablesGet`, `...UpdatesqlvariablesPost`…) viết tay không chú thích và `body?: any`, nằm lẫn với hàm Swagger có chú thích đầy đủ.
* **Tên hàm sinh tự động**: `api<Module><ChứcNăng><Hành động><Method>` (`apiTmsTmsequipmentPagePost`, `apiTmsTmsequipmentAddPost`, `apiTmsTmsequipmentDeletePost`). Ví dụ gọi: `await getAPI(TmsEquipmentApi).apiTmsTmsequipmentPagePost({ page: 1, pageSize: 50, field: 'createTime', order: 'desc' } as PageEquipmentInput)`.

---

## 🎨 2. Quy Chuẩn CSS / SCSS & Theme Dark/Light

1. **CẤM Hardcode Mã Màu (`#fff`, `#000`, `#1578a3`, `red`)**:
   - BẮT BUỘC sử dụng CSS variables của Element Plus để đảm bảo tương thích 100% giữa Light Theme và Dark Theme:
     - Màu chủ đạo: `var(--el-color-primary)`, `var(--el-color-success)`, `var(--el-color-warning)`, `var(--el-color-danger)`.
     - Màu chữ: `var(--el-text-color-primary)`, `var(--el-text-color-regular)`, `var(--el-text-color-secondary)`.
     - Màu nền & viền: `var(--el-bg-color)`, `var(--el-fill-color-light)`, `var(--el-border-color)`, `var(--el-border-color-light)`.
     - Biến thể (theo Kiro FE): `var(--el-color-primary-light-3)` … `-light-9` (nhạt dần), `var(--el-color-primary-dark-2)` (đậm hơn). Nghĩa màu: primary = thao tác chính/link; success = hoạt động/thành công; warning = cảnh báo/chờ xử lý; danger = lỗi/xóa/ngừng; info = thông tin phụ.
     - Nút, thẻ, thông báo của Element dùng prop `type="primary|success|warning|danger|info"` (`<el-tag type="success">`), ⛔ không `style="background: #1578a3"`.
   - Lớp tiện ích có sẵn: `w100`, `mb5`, `mb20`; responsive bằng điểm ngắt của `el-col` (`:xs="24" :sm="12" :md="8" :lg="6" :xl="6"`); style riêng của trang ưu tiên `scoped`.
2. **CẤM Lạm Dụng `!important` Trong CSS/SCSS (Rule 20.6)**:
   - ⛔ TUYỆT ĐỐI KHÔNG sử dụng `!important` bừa bãi trong CSS/SCSS (đặc biệt trong scoped styles và component).
   - *Lý do*: Phá vỡ CSS cascade chain, khiến trình duyệt tính toán lại cây kiểu gây giật lag và khó bảo trì.
   - *Giải pháp thay thế chuẩn*: Nâng cao độ ưu tiên tự nhiên (specificity) bằng lồng selector (ví dụ `.parent .child`), dùng class định danh cụ thể hoặc pseudo-classes.
3. **Quy Chuẩn Comment Trong SCSS**:
   - BẮT BUỘC dùng block comment dạng `/* comment */`.
   - ⛔ TUYỆT ĐỐI KHÔNG dùng comment một dòng `//` trong khối SCSS (gây lỗi phân tích cú pháp khi Vite build).
4. **Không Để Thừa Chấm Phẩy (`;;`)**: Đảm bảo cú pháp CSS sạch.
5. **Quy Chuẩn Phạm Vi CSS (Component-Scoped First — CẤM TUYỆT ĐỐI Viết / Sửa CSS Global) (Rule 20.22)**:
   - **Bối cảnh & Nguy cơ**: Thư mục CSS global (`src/src/theme/`, bao gồm `app.scss`, `index.scss`, `vxeTable.scss`, `element-plus.scss`, `presets/depthBlueAmber.scss`...) là cấu hình nền tảng toàn dự án. Mọi can thiệp CSS, override component (bảng, form, dialog, màu tiêu đề...) của một phân hệ nghiệp vụ nếu đưa ra file global sẽ làm rò rỉ selector (leaking styles), phá vỡ giao diện của các phân hệ khác trên toàn dự án.
   - **Các quy định bắt buộc (P0 Safeguards)**:
     1. ⛔ **TOÀN BỘ THƯ MỤC `src/src/theme/` LÀ VÙNG CẤM ĐỊA (Restricted Area)**: AI CẤM TUYỆT ĐỐI tạo mới, sửa đổi bất kỳ tệp nào trong `src/src/theme/` trừ khi và chỉ khi **người dùng có yêu cầu rõ ràng bằng lời**. Nếu người dùng không yêu cầu trực tiếp, 100% style phải đóng gói trong phân hệ.
     2. ⛔ **CẤM NGỤY BIỆN "UTILITY CLASS DÙNG CHUNG" (No Scope Creep)**: Nghiêm cấm việc tự ý biến một nhu cầu style phát sinh từ phân hệ (ví dụ: muốn `el-table` có màu header giống `vxe-table`) thành class tiện ích dùng chung đặt ở file global (như `.el-table.el-table-xgrid-header`). Bất kỳ style nào phục vụ cho màn hình nào thì thuộc sở hữu của màn hình đó.
     3. ⛔ **CẤM GHI ĐÈ FILE PRESET SCSS**: Nghiêm cấm nhảy vào các file preset (`depthBlueAmber.scss`...) viết khối override cho component con. Khi cần ăn theo màu theme, BẮT BUỘC dùng CSS Variables hệ thống (`var(--el-color-primary)`, `var(--el-color-primary-light-8)`, `var(--el-text-color-primary)`...) kết hợp với `:deep(...)` bên trong `<style scoped>`.
     4. ✅ **Đóng gói nội bộ phân hệ**: Mọi style của phân hệ BẮT BUỘC viết trong `<style scoped lang="scss">` của chính component/view đó hoặc đặt trong thư mục `src/src/views/<phân-hệ>/style/` (ví dụ: `src/src/views/sharedata/style/loading.scss`).
     5. ✅ **Import tại component / view**: Tại các view / component chính cần sử dụng, import nội bộ bằng cú pháp `@import '../style/<tên-style>.scss';` hoặc `@import './style/<tên-style>.scss';` trong khối `<style lang="scss">`.
     6. ✅ **Scope selector an toàn**: Các selector trong file style phân hệ vẫn phải được bọc prefix theo class cha định danh của phân hệ (ví dụ: `.eshare-sharing`, `.esh-datasource-container`, `[class*='sharedata-']`) để cô lập hoàn toàn phạm vi ảnh hưởng.
     7. 🔍 **Tự kiểm tra trước khi bàn giao (Pre-handoff Diff Gate)**: Trước khi hoàn tất, AI phải kiểm tra `git status`/`git diff`. Nếu thấy xuất hiện thay đổi trong `src/src/theme/` mà người dùng không yêu cầu, BẮT BUỘC hoàn nguyên (`git checkout`) ngay lập tức và chuyển style về `<style scoped>` nội bộ.
6. **Vị Trí Đặt Code CSS/SCSS (Rule 20.16)**:
   - Code CSS mới đặt ở cuối cùng khối `<style>`; chỉnh sửa CSS hiện có thì sửa in-place tại chỗ. Chi tiết xem mục 6.5.
8. **Định dạng nội dung menu/popper bị đưa ra `<body>` (bổ sung 20/10/2026)**:
   - `el-dropdown`, `el-select`, `el-autocomplete`, `el-popover`, `el-tooltip` mặc định đưa phần nổi (menu, gợi ý, popover) ra thẳng `<body>`. Trong `<style scoped>`, `:deep(.lớp-menu) .x` biên dịch thành `[data-v-xxx] .lớp-menu .x` — cần phần tử cha mang `data-v-xxx` — nên **không khớp** khi menu đã ở `<body>`.
   - ✅ Viết class scoped thường đặt trên phần tử nằm trong slot của chính component (`.vqb-suggest-item { … }`, `.lớp-menu .x { … }`): phần tử trong slot vẫn mang `data-v-xxx` dù đã bị đưa ra ngoài. Hoặc dùng `popper-class="<tên-riêng>"` kèm một khối `<style lang="scss">` không scoped, mọi selector bắt đầu bằng tên riêng đó (tiền lệ `views/bigScreen/trafficInfo/component/incidentNotification.vue`).
   - ⛔ Không dùng `:deep(.lớp-của-menu)` cho phần nổi. Đã gặp: menu "Chèn biến hệ thống" ở `sharedata/dataSource/component/queryBuilder.vue` hiện dính chữ `@lastTimeMốc…` vì CSS không áp.

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
* **Bấm Ra Ngoài Thì Đóng — Form Có Thay Đổi Chưa Lưu Phải Hỏi Trước Khi Đóng (Rule 20.26, đổi 20/10/2026)**:
  - *Vì sao đổi*: người dùng phản ánh hộp thoại "bấm ra ngoài không tự tắt, phải bấm dấu X". Bản cũ cấm hẳn việc đóng khi bấm ra ngoài để chống mất dữ liệu đang nhập; bản mới giữ mục đích đó bằng bước hỏi xác nhận, chỉ khi thật sự có dữ liệu chưa lưu.
  - **Hộp chỉ để xem** (`<el-drawer>`/`<el-dialog>` chi tiết nhật ký, cảnh báo, xem trước): để mặc định, bấm ra ngoài hoặc Esc là đóng.
  - **`ElMessageBox.confirm` / `prompt`**: để mặc định (bấm ra ngoài = Hủy, không mất dữ liệu).
  - **Form thêm/sửa**:
    - `:close-on-click-modal="!state.isProcessing"`, `:close-on-press-escape="!state.isProcessing"`, `:show-close="!state.isProcessing"`, `:before-close="beforeClose"`.
    - Lúc mở form chụp lại dữ liệu ban đầu (`JSON.stringify` của `ruleForm` sau khi nạp xong). `beforeClose(done)`: dữ liệu hiện tại khác bản chụp → `ElMessageBox.confirm($t('lz.message.base.confirmDiscardChanges'), …)`, đồng ý thì `done()`, hủy thì giữ hộp; giống bản chụp → `done()` ngay.
    - Nút "Bỏ qua"/"Hủy" gọi cùng `beforeClose`, không gán thẳng `isShowDialog = false`. Lưu thành công thì đóng thẳng, không hỏi.
    - Tiền lệ trong dự án: `views/mail/mauMail/component/editMau.vue` (hỏi khi có thay đổi chưa lưu — câu chữ trong tệp đó đang viết cứng, ⛔ không chép, dùng khóa i18n), `views/vds/config/component/editVdsConfig.vue` (chặn đóng khi đang lưu).
  - Mã cũ ở phân hệ khác giữ nguyên tới khi sửa tới màn đó (Rule 19.89).
* **Bảng đối chiếu trình bày modal — mọi modal phải nhìn giống nhau** (tích hợp từ Kiro FE trong `TA-ITS015-WEBVUE-V1.0/.kiro/steering/coding-rules.md`; mẫu tham chiếu `views/tms/workContact/component/editWorkContact.vue`; khung đầy đủ ở §8):

  | Hạng mục | Chuẩn | ⛔ Không dùng |
  |---|---|---|
  | Tiêu đề | slot `#header` với icon `ele-Edit` + `{{ props.title }}` | thuộc tính `:title="props.title"` |
  | Thuộc tính dialog | `draggable` + `:close-on-click-modal="!state.isProcessing"` + `:before-close="beforeClose"` + `width="700px"` (Rule 20.26 bản 20/10/2026; Kiro gốc ghi `:close-on-click-modal="false"`) | `align-center` |
  | Nhãn form | `label-width="auto"` + `label-position="left"` | `label-position="right"`, `label-width="120px"` |
  | Khoảng cách cột | `<el-row :gutter="10">` | `:gutter="16"` |
  | Cột | `<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">`; textarea/trường dài dùng cột 24 ở mọi điểm ngắt | `:span="12"` (thiếu responsive, thiếu `mb20`) |
  | Kiểm tra dữ liệu | `:rules="[...]"` đặt ngay trên `el-form-item` | object `rules` riêng rồi `:rules="rules"` trên `el-form` |
  | Chân dialog | `<span class="dialog-footer">` bọc 2 nút | đặt nút thẳng trong slot `#footer` |
  | Nút xác nhận | `:disabled="state.isProcessing"` | `:loading="state.loading"` |
  | Độ dài trường | `EntityFieldLength.Length64/128/256` cho cả `:maxlength` lẫn thông điệp | số viết cứng (`maxlength="256"`) |

  Thông điệp kiểm tra dùng khóa chung: `$t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.code') })`, `$t('lz.validation.base.maxlength', { PropertyName, MaxLength: EntityFieldLength.Length64 })`; placeholder `$t('lz.label.base.pleaseType', { input: ... })`; tiêu đề `$t('lz.label.base.titleAdd' | 'titleEdit', { name: ... })`.

* **Lỗi giao diện đã gặp — kiểm lại trước khi báo xong (bổ sung 22/10/2026, từ hộp rà soát SQL riêng ShareData)**:
  - **Tiêu đề hộp thoại bị đen / hộp lệch góc**: thanh tiêu đề dùng nền primary nên chữ BẮT BUỘC trắng. ⛔ Không dùng `:title`, không dùng `align-center` (xung đột với `.el-dialog { position: absolute }` ở `theme/element.scss`, hộp bị đẩy sát góc trái trên). Luôn theo bảng trên: slot `#header` (icon + chữ trắng, `color: #fff`) + `draggable`; hộp đang lưu thì khóa `close-on-click-modal` / `close-on-press-escape` / `show-close`. Áp cho cả hộp xác nhận, hộp rà soát và hộp xem, không chỉ form thêm/sửa.
  - **Hộp cao thay đổi sau khi mở (Monaco, bảng, chip nạp muộn) bị lệch khỏi giữa màn hình**: `.el-dialog` toàn cục là `position: absolute` nên không tự canh giữa lại khi cao thêm. Hộp loại này thêm class riêng và đưa về luồng flex của overlay bằng khối `<style lang="scss">` (không scoped, vì hộp được teleport ra body): `.el-overlay .el-dialog.<class-riêng> { position: relative; }`. Chỉ khóa theo class riêng, không đụng `.el-dialog` chung.
  - **Hộp mở chậm vài trăm ms**: không `await` API phụ (đếm cột, kiểm tra, thống kê) trước khi bật `visible`. Mở hộp ngay với dữ liệu đã có, phần phụ nạp xong mới điền (kiểm tra còn đúng đối tượng trước khi gán). Hộp nạp bằng `defineAsyncComponent` thì nạp sẵn chunk lúc rảnh như Monaco.
  - **Nhãn / nút trong một hàng bị rớt dòng**: hàng thanh công cụ (nút chuyển, tab, nút hành động) dùng `flex-wrap: wrap`; phần nhãn giữ `flex: 0 0 auto` + `white-space: nowrap` (kể cả `.el-radio-button__inner`). ⛔ Không đặt `flex: 1 1 0` + `overflow: hidden` cho cột chứa nhãn, vì khi cột bên cạnh dài ra nhãn bị ép co và xuống dòng. Hàng chật thì để cả nhóm nút xuống hàng dưới.
  - **Monaco trong hộp thoại**: thanh cuộn nhỏ (`scrollbar: { verticalScrollbarSize: 10, horizontalScrollbarSize: 10, useShadows: false }`), diff editor tắt `renderOverviewRuler`; mặc định của Monaco làm thanh cuộn to, lệch tông với các màn khác.
  - **Monaco nạp chậm lần đầu**: `import('monaco-editor')` nặng vài MB, nếu chỉ nạp khi mở tab thì người dùng thấy ô soạn thảo trống vài trăm ms. Trang chứa trình soạn SQL phải nạp sẵn lúc rảnh (`requestIdleCallback`, có `setTimeout` dự phòng) trong `onMounted`, chỉ với người dùng có quyền dùng tab đó; các component con vẫn `import()` như cũ và nhận lại module đã nạp.
  - **Kiểm tra khi sửa giao diện**: sau khi sửa phải đối chiếu hộp thoại / thanh công cụ vừa làm với một hộp có sẵn của phân hệ (vd `editPacket.vue`, `activityDetailDialog.vue`) về tiêu đề, vị trí, kích thước thanh cuộn, thay vì chỉ kiểm tra mã.

---

## 🌐 5. Đa Ngôn Ngữ (i18n Scope Rules)

* **Cấu trúc khóa** (tích hợp từ Kiro FE): `lz.entity.{base|<module>|<module><ChứcNăng>}.<trường>` (`lz.entity.base.code`, `lz.entity.tms.equipmentType`, `lz.entity.tmsEquipment.vendor`); `lz.label.*` (nhãn: `lz.label.base.pleaseType`, `lz.label.tms.zoneInfo`); `lz.validation.base.*` (`required`, `maxLength`); `lz.message.base.*` (`saveSuccess`, `confirmDelete`); `lz.button.base.*` (`confirm`, `cancel`); giá trị cấu hình `lz.param.<typeCode>.<code>` (`lz.param.status_type.1`). Phân cấp `base` → `<module>` → `<module><ChứcNăng>` ở §10.3. Trong script: `ElMessage.success($t('lz.message.base.saveSuccess'))`.

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
  - Key mới chèn vào dòng cuối cùng của `vi-vn.json` và `en-us.json` (hoặc file phân hệ `its/<phân-hệ>/{vi-vn,en-us}.json`); chỉnh sửa key hiện có thì sửa in-place cùng hàng. Chi tiết xem mục 6.5.

* **Tách Tệp Locale Theo Phân Hệ & Thoát Ký Tự `@` (Vue-i18n Escape & Subsystem Locale Modularization) (Rule 20.29)**:
  1. **Tách tệp locale riêng theo từng phân hệ (`src/src/i18n/lang/its/<phân-hệ>/{vi-vn,en-us}.json`)**:
     - ⛔ **CẤM** tiếp tục nhồi nhét hàng nghìn khóa dịch đặc thù của phân hệ nghiệp vụ vào hai tệp global khổng lồ `src/src/i18n/lang/vi-vn.json` và `en-us.json` (gây phình to file, chậm tải và xung đột Git khi nhiều dev cùng sửa).
     - ✅ **BẮT BUỘC**: Tách toàn bộ khóa của từng phân hệ nghiệp vụ ra tệp riêng: `src/src/i18n/lang/its/<phân-hệ>/vi-vn.json` và `en-us.json` (ví dụ `its/sharedata/`, `its/tms/`...).
     - **Tự động nạp động (Auto-discovery) qua `import.meta.glob`**: Trong [`src/src/utils/locale.ts`](/@/utils/locale.ts), sử dụng cơ chế nạp động tự động của Vite:
       ```ts
       const enUSIts = import.meta.glob<Record<string, string>>('../i18n/lang/its/**/en-us.json', { eager: true, import: 'default' });
       const viVNIts = import.meta.glob<Record<string, string>>('../i18n/lang/its/**/vi-vn.json', { eager: true, import: 'default' });
       const mergeIts = (files: Record<string, Record<string, string>>) => Object.assign({}, ...Object.values(files));
       ```
       ⛔ **CẤM** import thủ công từng file phân hệ (`import enUSSharedata...`) làm `locale.ts` liên tục bị sửa và xung đột Git. Thêm phân hệ mới chỉ cần tạo thư mục `src/src/i18n/lang/its/<phân-hệ>/`, hệ thống sẽ tự động quét và nạp.
     - **Đồng bộ song ngữ**: Số lượng và danh sách khóa giữa `vi-vn.json` và `en-us.json` trong thư mục phân hệ BẮT BUỘC bằng nhau 100% (keys diff = []).
  2. **Thoát ký tự `@` trần trong chuỗi bản dịch (Vue-i18n Linked Format Escape `{'@'}`)**:
     - ⛔ **TUYỆT ĐỐI CẤM** để ký tự `@` trần đi liền trước chữ cái hoặc tên biến trong chuỗi i18n (ví dụ cấm: `Tham số [@{0}]`, `@lastTime`, `@pageSize`, `TOP (@pageSize)`).
     - *Lý do*: Vue-i18n mặc định coi `@:` hoặc `@<word>` là cú pháp liên kết chuỗi (linked message). Khi gặp `@` không hợp lệ, Vue-i18n sẽ ném lỗi runtime nghiêm trọng: `SyntaxError: Invalid linked format` hoặc làm crash trang khi render.
     - ✅ **BẮT BUỘC**: Bọc escape bằng cú pháp chuẩn vue-i18n: `{'@'}` (ví dụ: `Tham số [{'@'}{0}]`, `{'@'}lastTime`, `{'@'}pageSize`, `TOP ({'@'}pageSize)`).
  3. **Cơ chế nạp CSDL `SysTerminology` & Cập nhật seed SQL Idempotent an toàn**:
     - `getTerminologyList(lang)` ưu tiên nạp từ bảng CSDL `SysTerminology` (qua IndexedDB), chỉ lấy JSON khi DB chưa có khóa đó. Do đó: nếu chỉ sửa JSON mà CSDL đã có bản ghi cũ thì giao diện sẽ **không đổi**.
     - Khi chỉnh sửa giá trị bản dịch trong script seed SQL:
       - ⛔ **CẤM** ghi đè làm mất các bản dịch mà quản trị viên đã tùy chỉnh trên môi trường vận hành.
       - ✅ **BẮT BUỘC**: Viết khối `UPDATE dbo.SysTerminology SET Value = N'<mới>' WHERE Name = N'...' AND Code = N'...' AND Lang = N'...' AND Value = N'<giá trị cũ>';` trước lệnh `MERGE ... WHEN NOT MATCHED THEN INSERT`.
       - Sau khi chạy script SQL trên CSDL, trên giao diện webapp cần bấm nút 🔄 tại avatar góc phải trên để làm mới cache ngôn ngữ.
  4. **Kiểm thử Playwright đọc từ điển i18n tĩnh**:
     - Trong các tệp test E2E/Static Scan, nếu kiểm tra các khóa của phân hệ, BẮT BUỘC đọc từ `i18n/lang/its/<phân-hệ>/vi-vn.json` (hoặc merge cùng `vi-vn.json` gốc), không được chỉ assert trên file gốc `vi-vn.json` vì các khóa phân hệ đã được tách ra.

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
   - Chỉ áp cho thẻ mới viết hoặc thẻ có thêm thuộc tính; thẻ cũ không đổi nội dung thì giữ nguyên dòng và thụt lề cũ (Rule 19.89, [backend-dotnet_thienan.md](./backend-dotnet_thienan.md) §4.21).
   - **Thẻ không có nội dung viết tự đóng (Rule 20.30)**: Thẻ HTML hoặc component không có nội dung bên trong (không chữ, không thẻ con, không slot) BẮT BUỘC viết dạng tự đóng `<div ... />`; ⛔ **CẤM** viết cặp mở – đóng rỗng `<div ...></div>`. Áp dụng cho cả thẻ HTML thường (`div`, `span`, `i`...) và component trong `<template>` của tệp `.vue`. Chỉ áp cho thẻ mới viết hoặc thẻ có sửa thuộc tính; thẻ cũ không đổi nội dung thì giữ nguyên (Rule 19.89).
     ```vue
     <!-- ❌ CẤM -->
     <div
         ref="monacoContainerRef"
         class="sql-monaco-editor"
         :style="{ height: `${rawEditorHeight}px` }"
     ></div>

     <!-- ✅ CHUẨN -->
     <div
         ref="monacoContainerRef"
         class="sql-monaco-editor"
         :style="{ height: `${rawEditorHeight}px` }"
     />
     ```

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
   - **Mục tiêu**: Đảm bảo 100% code mới hoặc code được chỉnh sửa không phát sinh bất kỳ lỗi đỏ nào từ TypeScript Language Server / IDE (như `ts-plugin(7006)`, `ts-plugin(2367)`, `ts(2322)`, `ts(2339)`).
   - **Các quy tắc kiểm tra kiểu bắt buộc**:
     1. **Bắt buộc khai báo đúng kiểu tham số sự kiện / callback (`ts-plugin(7006)`) — CẤM dùng `:any`**:
        - Khi viết hàm xử lý sự kiện (event handler), lắng nghe DOM listener (`addEventListener`), hoặc callback thư viện thứ 3 (Monaco editor, resize drag handle, window event, v.v.): **BẮT BUỘC khai báo đúng kiểu sự kiện chuẩn của DOM hoặc interface sự kiện chính xác của thư viện** (ví dụ: `(e: MouseEvent) => ...`, `(e: KeyboardEvent) => ...`, `(e: Event) => ...` hoặc `(e: editor.IContentSizeChangedEvent)` từ `monaco-editor`).
        - ⛔ **TUYỆT ĐỐI CẤM dùng `:any`** (như `(e: any) => ...`) để lách lỗi kiểu. Bắt buộc tra cứu và dùng đúng kiểu sự kiện thực tế.
        - ⛔ **TUYỆT ĐỐI CẤM để trần** `(e) => { ... }` hoặc `const handleDrag = (e) => { ... }` khiến TypeScript báo lỗi gạch chân đỏ: `Parameter 'e' implicitly has an 'any' type.ts-plugin(7006)`.
     2. **Tránh lỗi `no overlap` (`ts-plugin(2367)`) do so sánh sai kiểu**:
        - Khi một biến/thuộc tính mang kiểu enum số (như `status?: StatusEnum | undefined`): **BẮT BUỘC chỉ so sánh với enum hoặc số tương ứng** (`status === StatusEnum.NUMBER_0` hoặc `status === 0`).
        - ⛔ **TUYỆT ĐỐI CẤM** so sánh biến enum số với chuỗi (`status === '0'` hay `status === 'Disable'`) hoặc boolean (`status === false`) vì hai kiểu không có điểm chung (no overlap), sẽ gây lỗi gạch chân đỏ `ts(2367)` trong VS Code.
     3. **Tránh lỗi thuộc tính không tồn tại (`ts(2339)`) trên DTO**:
        - Nếu truy cập một trường phụ mở rộng không nằm trong DTO Swagger backend (ví dụ: `syncKind` trong `ShareDataUpdatePacketInput`), **BẮT BUỘC ép kiểu cục bộ an toàn**: `(state.editForm as any).syncKind` để tránh báo đỏ.
     4. **Tự kiểm tra trước khi bàn giao (Pre-handoff Type Audit)**:
        - Trước khi hoàn tất mọi thay đổi code TypeScript / Vue, AI phải tự rà soát kiểu dữ liệu của các biểu thức điều kiện `if (...)`, `===`, toán tử ba ngôi `? :`, cũng như khai báo kiểu cho mọi tham số sự kiện/callback để bảo đảm zero lỗi đỏ trong IDE của lập trình viên.

8. **Đóng Gói CSS Phân Hệ Nội Bộ — CẤM TUYỆT ĐỐI Viết / Sửa CSS Global (Rule 20.22)**:
   - Chi tiết quy định cấm đụng vào thư mục `src/src/theme/`, cấm ngụy biện class dùng chung và hướng dẫn đóng gói tại `<style scoped>` / `src/src/views/<phân-hệ>/style/`: xem mục 2.5.

9. **Bỏ Là Bỏ Hết Các Liên Đới (Cascading Cleanup - Rule 19.68)**:
   - Khi gỡ bỏ một nút bấm, component hoặc tính năng UI: **BẮT BUỘC gỡ bỏ triệt để toàn bộ biến state/ref, hàm xử lý (handlers), watcher, computed, component/dialog liên quan, SCSS riêng của nó**, và cập nhật bài test tương ứng.
   - ⛔ **TUYỆT ĐỐI CẤM** để lại mã chết (dead code / zombie code) rải rác trong file `.vue`.

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
			:close-on-click-modal="!state.isProcessing"
			:show-close="!state.isProcessing"
			:close-on-press-escape="!state.isProcessing"
			:before-close="beforeClose"
			width="700px"
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
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
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

### 4. Nội Dung Tooltip Chỉ Chứa Chữ, Không Chứa Icon (bổ sung 27/10/2026)
* Nội dung `el-tooltip` (và `title`, `aria-label`) chỉ là chữ mô tả thao tác, ví dụ `Chỉnh sửa`, `Thu gọn`.
  - ⛔ **CẤM** đưa ký hiệu / icon vào nội dung tooltip (`✎`, `⤢`, `⇄`, `＋`, emoji...). Icon thuộc về nút bấm, không thuộc về chữ giải thích.
  - Khóa i18n có ký hiệu ở đầu để làm nhãn nút (vd. `"✎ Chỉnh sửa"`) thì **không dùng lại** cho tooltip; tạo khóa riêng chỉ có chữ (vd. `editFieldsTooltip: "Chỉnh sửa"`).
  - Nút chỉ có icon (tiết kiệm bề rộng) bắt buộc kèm tooltip chữ và `aria-label` cùng nội dung.

---

## 🧪 9. Ưu Tiên Sửa Code Trước — Chỉ Viết Test Khi Có Yêu Cầu (Rule 19.67 - Fix-First & On-Demand Testing)

Khi sửa lỗi hoặc điều chỉnh giao diện (UI/UX, CSS, font chữ, alignment...), tập trung sửa dứt điểm code trước để người dùng review ngay. ⛔ CẤM tự ý viết thêm bài test cho mỗi fix nhỏ làm chậm tiến độ. Chỉ viết test khi người dùng yêu cầu rõ ràng. Chi tiết quy chuẩn: xem [testing-rules_thienan.md](./testing-rules_thienan.md) §3.

---

## 📊 10. Quy Chuẩn VxeTable (`useVxeTable`), Dữ Liệu Cấu Hình (`BaseConfigTypeEnum`) & Phân Cấp i18n (Rule 20.25, bổ sung 19/10/2026)

### 10.1. VxeTable Grid Pattern
- BẮT BUỘC khởi tạo grid thông qua hook `useVxeTable`:
  ```typescript
  import { VxeGridInstance } from 'vxe-table';
  import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';

  const xGrid = ref<VxeGridInstance>();

  const options = useVxeTable<PageEquipmentOutput>(
      {
          id: 'tmsEquipGrid',      // Bắt buộc có ID duy nhất để lưu cấu hình ẩn/hiện cột của người dùng
          name: 'Equipment_Export', // Tiền tố tên tệp khi xuất Excel
          columns: [
              { type: 'checkbox', fixed: 'left', width: 45 },
              { field: 'code', title: $t('lz.entity.base.code'), minWidth: 140, sortable: true },
              { field: 'name', title: $t('lz.entity.base.name'), minWidth: 180 },
              { field: 'status', title: $t('lz.entity.base.status'), width: 120, slots: { default: 'row_status' } },
              { title: $t('lz.label.base.action'), fixed: 'right', width: 140, slots: { default: 'row_buttons' } },
          ],
      },
      {
          proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
          sortConfig: { defaultSort: { field: 'createTime', order: 'desc' } },
          pagerConfig: { pageSize: 50 },
      }
  );
  ```

- Hook `useVxeTable` tự đọc kết quả API: phân trang lấy `data.result.records` (dòng) + `data.result.total` (tổng); không phân trang lấy `data.result` (Kiro FE).

- **Tiêu đề cột bảng căn giữa (bổ sung 22/10/2026)**: mọi bảng, kể cả bảng nhỏ trong tab/hộp thoại, BẮT BUỘC căn giữa tiêu đề cột; chỉ phần thân ô mới tuỳ kiểu dữ liệu.
  - `el-table-column`: luôn có `header-align="center"`. Cột trạng thái (thẻ `el-tag`), thời gian, số, thao tác thì thêm `align="center"` (căn cả thân ô, tiêu đề tự theo); cột tên/mô tả dài giữ thân ô căn trái.
  - `vxe-table` / `useVxeTable`: mỗi cột khai `headerAlign: 'center'`; thân ô dùng `align: 'center'` cho trạng thái / thời gian / số / thao tác, cột văn bản dài để trái.
  - ⛔ Không để tiêu đề lệch trái trong khi thân ô cùng cột căn giữa, và ngược lại.

### 10.2. Quản Lý Dữ Liệu Cấu Hình (Pinia `BaseConfigStore`)
- ⛔ **CẤM** hardcode chuỗi mã loại cấu hình trực tiếp (như `'status_type'`, `'direction_type'`).
- BẮT BUỘC sử dụng enum `BaseConfigTypeEnum` từ `/@/types/enums`:
  ```typescript
  import { BaseConfigTypeEnum } from '/@/types/enums';
  import { useBaseConfig } from '/@/stores/baseConfig';

  const { getConfigDataByCode, getConfigItemByCode } = useBaseConfig();

  // 1. Lấy danh sách options cho dropdown / radio
  const statusOptions = computed(() => getConfigDataByCode(BaseConfigTypeEnum.SysStatusType));

  // 2. Lấy 1 item để hiển thị tên trên bảng hoặc TagInfo
  const statusItem = getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, row.status);
  ```
- Khi bind vào form có trường dữ liệu là số, bắt buộc dùng `parseInt(item.code)` vì trường code của SysConfig luôn là string.
- Danh sách (dropdown, radio) dùng `getConfigDataByCode()`; một mục (formatter cột VxeTable, `TagInfo`) dùng `getConfigItemByCode()` — ví dụ cột `formatter: ({ cellValue }) => getConfigItemByCode(BaseConfigTypeEnum.TmsDirectionType, cellValue)?.name`, slot `<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, row.status)" />`; cấu hình vận hành (`SysOpConfig`) dùng `getOpConfigListByGroupCode()`. Backend thêm loại cấu hình mới thì thêm giá trị vào `BaseConfigTypeEnum` (Kiro FE).

### 10.3. Phân Cấp Namespace i18n
1. **`base` (`lz.*.base.*`)**: Dùng chung cho toàn hệ thống (code, name, status, confirmDelete, saveSuccess...). BẮT BUỘC kiểm tra `base` trước khi tạo mới key.
2. **`{module}` (`lz.*.{module}.*`)**: Dùng chung cho toàn module (nhiều màn hình cùng dùng).
3. **`{module}{Feature}` (`lz.*.{module}{Feature}.*`)**: Dành riêng cho 1 màn hình cụ thể.
- 100% text trên UI phải đưa vào i18n đồng bộ cả `vi-vn.json` và `en-us.json`. ⛔ **CẤM** viết song ngữ lai.

### 10.4. Định Dạng Ngày Tháng Trên Bộ Lọc & Form (`el-date-picker` Bắt Buộc `format="DD/MM/YYYY"`) (Rule 20.27, bổ sung 21/10/2026)
- **Bối cảnh & Mục tiêu**:
  - Element Plus `<el-date-picker>` mặc định hiển thị ngày tháng theo định dạng quốc tế `YYYY-MM-DD` (hoặc `YYYY-MM-DD HH:mm:ss`) nếu thiếu thuộc tính `format`. Thói quen chuẩn của người dùng Việt Nam và hệ thống Thiên Ân là `ngày/tháng/năm` (`DD/MM/YYYY`).
- **Quy định bắt buộc**:
  1. **Bộ lọc / Input chỉ có ngày (`type="date"` hoặc `type="daterange"`)**:
     - ✅ BẮT BUỘC khai báo thuộc tính `format="DD/MM/YYYY"`.
     - Giữ `value-format="YYYY-MM-DD"` (để truyền giá trị chuẩn ISO cho Backend WebAPI).
     - Ví dụ chuẩn:
       ```vue
       <el-date-picker
           v-model="state.dateRange"
           type="daterange"
           format="DD/MM/YYYY"
           value-format="YYYY-MM-DD"
           :start-placeholder="$t('lz.placeholder.sharedataHistory.dateFrom')"
           :end-placeholder="$t('lz.placeholder.sharedataHistory.dateTo')"
           class="w100"
       />
       ```
  2. **Bộ lọc / Input có ngày và giờ (`type="datetime"` hoặc `type="datetimerange"`)**:
     - ✅ BẮT BUỘC khai báo thuộc tính `format="DD/MM/YYYY HH:mm:ss"` (hoặc `format="DD/MM/YYYY HH:mm"` nếu form không dùng giây).
     - Giữ `value-format="YYYY-MM-DD HH:mm:ss"` (hoặc `value-format="YYYY-MM-DD HH:mm"`).
     - Ví dụ chuẩn:
       ```vue
       <el-date-picker
           v-model="state.dateRange"
           type="datetimerange"
           format="DD/MM/YYYY HH:mm:ss"
           value-format="YYYY-MM-DD HH:mm:ss"
           :start-placeholder="$t('lz.entity.base.startTime')"
           :end-placeholder="$t('lz.entity.base.endTime')"
           class="w100"
       />
       ```
  3. ⛔ **TUYỆT ĐỐI CẤM** bỏ quên thuộc tính `format` trên `el-date-picker` dẫn tới hiển thị năm trước ngày sau (`YYYY-MM-DD`) gây nhầm lẫn cho người dùng.
  4. **Bảng dữ liệu hiển thị (VxeTable / el-table)**: BẮT BUỘC dùng formatter hiển thị thống nhất qua `DateTimeFormatEnum.DateTimeDefault` (`DD/MM/YYYY HH:mm:ss`) hoặc `DD/MM/YYYY`.

---

## 🧱 11. Store, Kiểu Dữ Liệu, Sự Kiện Thời Gian Thực, Trang CRUD, Lưu Trữ & Phân Quyền (tích hợp từ Kiro FE, 20/10/2026)

> Nguồn: bộ rule Kiro FE `TA-ITS015-WEBVUE-V1.0/.kiro/steering/` (`coding-rules.md`, `structure.md`, `tech.md`, `product.md`) và bản tóm tắt `DocBusinessThienAn/Rules_FE_BE.md` — đã tích hợp đủ vào tệp này; các bản sao `DocBusinessThienAn/KiroR/` và `DocBusinessThienAn/Rules_FE_BE.md` đã xóa để bảo đảm SSOT tập trung tại `.agents/rules/`. Rule nào tệp này đã quy định chặt hơn thì theo tệp này.

### 11.1. Pinia
- Viết store kiểu **Options API**: `defineStore('baseConfig', { state: () => ({ ... }), actions: { ... } })`, ⛔ không dùng kiểu Setup API.
- Store chính: `themeConfig.ts` (giao diện, bố cục, ngôn ngữ), `userInfo.ts` (đăng nhập, người dùng, quyền), `baseConfig.ts` (dữ liệu cấu hình, §10.2), `routesList.ts` (route động từ backend).

### 11.2. Kiểu & Enum
- `types/enums/baseEnum.ts`: mọi enum (`BaseConfigTypeEnum`, `StatusTypeEnum`, `DateTimeFormatEnum`…); `types/constants/operate.ts`: `OperateType = 'add' | 'edit' | 'copy' | 'view' | 'other'`; `types/constants/natsEvent.ts`: hằng sự kiện NATS.

### 11.3. Sự Kiện Thời Gian Thực (NATS qua `mittBus`)
```typescript
import mittBus from '/@/utils/mitt';
import { NatsEvent } from '/@/types/constants/natsEvent';

onMounted(() => mittBus.on(NatsEvent.StatusFms as any, handleNatsDeviceStatus));
onUnmounted(() => mittBus.off(NatsEvent.StatusFms as any, handleNatsDeviceStatus));
```
- Đăng ký ở `onMounted` thì **bắt buộc** hủy ở `onUnmounted` (tránh xử lý trùng khi vào lại màn hình).

### 11.4. Trang CRUD Chuẩn
- **`index.vue`**: form tìm kiếm (`el-card` + `el-form` + `el-row`/`el-col`); `vxe-grid` có toolbar (thêm/xóa/làm mới/chọn cột); nút trên từng dòng (sao chép/sửa/xóa) gắn `v-auth`; `ref` tới component sửa; các hàm `handleQuery`, `resetQuery`, `handleAdd`, `handleEdit`, `handleDelete`, `handleBatchDelete`.
- **`component/edit<ChứcNăng>.vue`**: `el-dialog` + `el-form` có kiểm tra (thông điệp i18n); `openDialog(row)` mở cho cha qua `defineExpose`; `submit()` gọi thêm/sửa qua `getAPI(...)`; xong thì `emit('handleQuery')` để cha tải lại.
- **Thao tác chuẩn**: thêm `state.operateType = 'add'; editRef.value?.openDialog({ status: 1, orderNo: 100 })`; sửa `state.operateType = 'edit'; editRef.value?.openDialog(row)`; xóa có xác nhận `ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.name }), …)` (kèm `closeOnClickModal: false`, §4) → `getAPI(Api).api…DeletePost({ id: row.id })` → `handleQuery()` → `ElMessage.success($t('lz.message.base.deleteSuccess'))`.

### 11.5. Lưu Trữ Trình Duyệt
- `import { Local, Session } from '/@/utils/storage'`: `Local.set/get` (localStorage, lâu dài), `Session.set/get` (sessionStorage, tạm thời; token đi qua cookie). ⛔ Không gọi thẳng `localStorage`/`sessionStorage`.

### 11.6. Phân Quyền
- Mức route: `meta.roles: ['admin', 'common']`.
- Mức nút: `v-auth="'<featureId>:<action>'"` (`tmsEquip:page`, `tmsEquip:add`, `tmsEquip:update`, `tmsEquip:delete`).

### 11.7. Công Cụ & Thư Viện
- Vite 5, Vue 3.5 (Composition API, `<script setup>`), TypeScript strict, Pinia 3, Vue Router 5; Element Plus 2 (chính), PrimeVue 4 (bổ sung), VxeTable 4 (xuất Excel), WangEditor 5; bản đồ/biểu đồ: Leaflet 1.9 + Leaflet-Geoman, MapLibre GL 5, OpenLayers 10, ECharts 6 (+ echarts-gl); thời gian thực: SignalR, MQTT 5, NATS WebSocket; HTTP: Axios + client sinh từ Swagger.
- Định dạng: Prettier (tab, độ rộng 2, `printWidth` 200, nháy đơn, dấu phẩy cuối kiểu es5); ESLint với `vue-eslint-parser` + `@typescript-eslint`.
- Lệnh (thư mục `src/`): `pnpm install`, `pnpm dev` (cổng mặc định 8888), `pnpm lint-fix`; bản production loại `console`/`debugger`. ⛔ Không tự chạy `pnpm build` (Rule 19.13, §7). Node.js 18+ khuyến nghị, dùng pnpm.
- **Cài thêm thư viện không được đổi vị trí các dòng đang có trong `package.json`** (bổ sung 23/10/2026): `pnpm add` tự xếp lại `dependencies` theo thứ tự chữ cái nên kéo theo diff ở những thư viện không liên quan (đã gặp: `jodit`, `jodit-vue` từ cuối danh sách bị chuyển lên giữa). Sau mỗi lần `pnpm add`/`pnpm remove`, đọc `git diff src/package.json` và trả các dòng cũ về đúng chỗ, chỉ giữ dòng thư viện mới thêm hoặc gỡ. ⛔ Không chạy `pnpm dedupe`/`pnpm up` kèm khi chỉ được yêu cầu cài một thư viện.

### 11.8. Bố Cục Mã Nguồn & Route
- `src/src/`: `api-services/<module>/` (sinh tự động), `components/` (dùng chung: `leafletMap/`, `table/`, `editor/`, `livecam/`, `scEcharts/`), `directive/`, `hooks/`, `i18n/`, `indexedDb/` (Dexie), `layout/`, `router/`, `stores/` (store riêng ITS ở `stores/its/`), `theme/`, `transporter/`, `types/`, `utils/` (`axios-utils.ts`, `storage.ts`, `locale.ts`), `views/`. Ngoài `src/src/`: `public/` (`config.js`), `api_build/` (script sinh API), `.env*` (⛔ không commit `.env.development`).
- Route lấy từ backend qua `SysMenuApi.apiSystemSysmenuLoginmenutreeGet`; đường dẫn `component` của menu ánh xạ vào `views/**`, nên đặt màn hình mới đúng thư mục khai trên menu. Route tĩnh (đăng nhập, toàn màn hình) ở `router/route.ts`; `router/backEnd.ts` đang dùng, `frontEnd.ts` không dùng.
- Component dùng chung đặt ở `src/components/`; component riêng của chức năng đặt cạnh view (`views/<module>/<chứcNăng>/component/`).




