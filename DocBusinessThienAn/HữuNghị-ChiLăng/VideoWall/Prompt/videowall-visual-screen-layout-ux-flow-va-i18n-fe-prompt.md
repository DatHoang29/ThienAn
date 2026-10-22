# VideoWall (FE) — Tối Ưu UX Luồng Chia Vùng, Component Bố Cục Trực Quan Ma Trận, Multi-User Permission & Chuẩn Hóa i18n

**Tệp prompt:** `DocBusinessThienAn/HữuNghị-ChiLăng/VideoWall/Prompt/videowall-visual-screen-layout-ux-flow-va-i18n-fe-prompt.md`  
**Phân hệ:** VideoWall (`TA-ITS015-WEBVUE-V1.0/src/src/views/videoWall`)  
**Ngày lập:** 22/10/2026  
**Căn cứ kỹ thuật:** Biên bản cuộc họp kỹ thuật toàn diện ngày 22/10/2026 giữa Anh Sơn (Tech Lead) và Đạt (Dev) từ tệp ghi âm gốc `videowall.m4a` (~45:10).  
**Trạng thái:** ⏳ Chờ thực thi (Chỉ chạy khi có yêu cầu).

---

## 1. Bối Cảnh & Các Điểm Nghẽn Giao Diện (Từ Cuộc Họp 22/10/2026)

Trong cuộc họp review chi tiết, Anh Sơn đã trực tiếp chỉ ra 10 vấn đề bất cập về UX/UI trên các màn hình phân hệ VideoWall:

1. **Luồng "Chia vùng" màn hình bị khựng và thiếu bước chọn mẫu:**
   - Hiện trạng: Bấm nút "Chia vùng" lập tức bắt người dùng kéo chuột quét tự do trên màn hình ma trận mà không biết phải chia mấy vùng. Khi muốn đổi kiểu chia (1-1, 2x2, 3x3) thì không có menu chọn, phải hủy thao tác làm lại từ đầu.
   - Chỉ đạo: Bấm "Chia vùng" $\rightarrow$ Mở popup/dialog chọn chế độ chia trước (1-1 [toàn màn hình], 2x2 [4 vùng], 3x3 [9 vùng], Tùy chỉnh [Custom]). Sau khi chọn mới áp dụng lên màn hình mục tiêu. Cho phép lưu cấu hình tùy chỉnh thành template tái sử dụng.
2. **Gốc tọa độ không chuẩn và thiếu thống nhất ký hiệu:**
   - Hiện trạng: Gốc tọa độ hiển thị nhảy lung tung khi nhập số 1 hay 10; ký hiệu chỗ ghi `H1C1`, chỗ ghi `1-1`, chỗ ghi `CD`.
   - Chỉ đạo: Chuẩn hóa gốc tọa độ Top-Left theo dữ liệu Display thực tế, đồng bộ ký hiệu rõ ràng `Hàng 1 - Cột 1` (`H1-C1`). Tách component trực quan ma trận dùng chung (`ScreenLayoutRenderer.vue`).
3. **Phân quyền ma trận đơn lẻ từng người và lệch tỷ lệ cột:**
   - Hiện trạng: Bảng phân quyền hiển thị cột đối tượng quá to trong khi lưới ma trận bị co cụm tí hon; mỗi dòng chỉ gán được 1 người dùng.
   - Chỉ đạo: Cân đối lại tỷ lệ cột; hỗ trợ chọn nhiều người dùng (`el-select multiple`) hoặc chọn theo Tổ chức, thiết lập 1 lần áp dụng cho cả nhóm.
4. **Dashboard thống kê trạng thái bị phân mảnh ("bị lé mắt"):**
   - Hiện trạng: Danh sách nằm bên trái nhưng số đếm trạng thái lại thảy tít sang góc phải màn hình, khiến mắt người dùng phải liếc ngang rất mỏi.
   - Chỉ đạo: Tách bạch rõ 2 khối thống kê thành 2 cột cân đối: Khối Bộ điều khiển (Tổng | Hoạt động | Mất kết nối) và Khối Màn hình (Tổng | Bật | Tắt).
5. **Thanh chỉ số tóm tắt & bổ sung tiêu đề rõ ràng:**
   - Hiện trạng: Đầu trang trống trải; trạng thái ghi mỗi chữ "Bình thường" trơ trọi; người dùng phải tự đếm thủ công từng ô màn hình đang dùng.
   - Chỉ đạo: Bổ sung Header tiêu đề trang; thanh tóm tắt số liệu (Tổng số input đang phát, Số màn hình đang dùng, Số màn hình còn trống: "Còn 2 màn hình chưa sử dụng"); nhãn ngữ cảnh rõ ràng: "Kịch bản: Bình thường", "Trạng thái: Đang hoạt động".
6. **Form zíc zắc hình chữ L và thứ tự phím Tab bị nhảy:**
   - Hiện trạng: Phím Tab nhảy lung tung không theo thứ tự tự nhiên; trường Độ phân giải bị đặt chung với Trạng thái; trường Bộ điều khiển bị chèn vào giữa Loại nguồn tín hiệu và Cổng tín hiệu.
   - Chỉ đạo: Luồng nhìn và Tab index từ trên xuống dưới, từ trái sang phải; dùng `<el-divider>` ngăn cách rõ ràng; gom cụm Nguồn tín hiệu và Cổng tín hiệu liền nhau.
7. **Lỗi màu sắc Solid Blue Overlay Bug & tương phản mờ nhạt:**
   - Hiện trạng: Các dòng bảng/cây danh mục bị phủ một lớp màu xanh đặc quánh (`bị phủ màu xanh / mù màu`), che mất chữ và mất hiệu ứng sọc vằn zebra; giao diện kịch bản ám màu xám xịt từ nền đến màn hình; font chữ nhỏ.
   - Chỉ đạo: Xóa bỏ triệt để class gây đè màu xanh đặc; tăng độ tương phản phân định rõ vùng thao tác; tăng font-size các nhãn thông số.
8. **Layout Quản lý kịch bản mất cân đối, trường "Thứ tự lớp" khó hiểu:**
   - Hiện trạng: Thông tin kịch bản kéo dài full-width trong khi thuộc tính cửa sổ bị dồn ép tí hon; trường "Thứ tự lớp" (z-index) không có hướng dẫn khiến người dùng phải mò mẫm; nút Thêm mới đặt xa danh sách; nút Lưu không cố định.
   - Chỉ đạo: Cân đối bố cục phần dưới; thêm tooltip cho "Thứ tự lớp" ("Giá trị càng lớn, cửa sổ hiển thị càng nổi lên trên các lớp khác") kèm nút Đưa lên trên/xuống dưới; đưa nút Thêm mới về cạnh kịch bản; đặt nút Lưu ở thanh cố định (Sticky Action Bar); thêm nút thu gọn panel (Splitter / Collapsible).
9. **Highlight liên kết trực quan & Khóa nguồn tín hiệu đã sử dụng:**
   - Hiện trạng: Chọn kịch bản/nguồn tín hiệu ở danh sách nhưng trên màn hình không có tín hiệu phản hồi tương ứng; không rào lại nguồn đã dùng khiến người dùng kéo trùng sinh lỗi.
   - Chỉ đạo: Click danh sách thì cửa sổ trên màn hình sáng lên (Active Highlight) và ngược lại; rào lại (disable / gray out) nguồn tín hiệu đã sử dụng trong danh sách nguồn kèm nhãn "Đang sử dụng".
10. **Chuẩn hóa thuật ngữ & 100% i18n với tiền tố `vw_`:**
    - Đổi "Topology" $\rightarrow$ Sơ đồ kết nối / Bố cục; "Controller" $\rightarrow$ Bộ điều khiển.
    - 100% qua `$t(...)`, tiền tố đồng bộ `vw_` trong `vi-vn.json` và `en-us.json`.

---

## 2. Danh Sách Tệp Cần Chỉnh Sửa & Tái Cấu Trúc

### 2.1. Component Dùng Chung & Layout Ma Trận
- `TA-ITS015-WEBVUE-V1.0/src/src/views/videoWall/components/ScreenLayoutRenderer.vue` (Tạo mới hoặc nâng cấp từ `wallMiniGrid.vue`): Component hiển thị ma trận màn hình dựa trên dữ liệu Display, hỗ trợ chế độ xem, chế độ chọn ô, chế độ chia vùng và highlight liên kết.
- `TA-ITS015-WEBVUE-V1.0/src/src/views/videoWall/components/ScreenDivisionDialog.vue` (Tạo mới): Dialog chọn chế độ chia vùng (1-1, 2x2, 3x3, Custom) kèm chức năng lưu mẫu template.

### 2.2. Màn Hình Phân Quyền Vùng (`wallPermission`)
- `TA-ITS015-WEBVUE-V1.0/src/src/views/videoWall/wallPermission/index.vue`: Cân đối tỷ lệ cột bảng, nhúng `ScreenLayoutRenderer`.
- `TA-ITS015-WEBVUE-V1.0/src/src/views/videoWall/wallPermission/component/editWallPermission.vue`: Form hỗ trợ chọn nhiều người dùng (`el-select multiple` có search) hoặc chọn theo Tổ chức, gọi API `BatchAssign`.

### 2.3. Màn Hình Dashboard & Bộ Điều Khiển (`dashboard`, `controllers`)
- `TA-ITS015-WEBVUE-V1.0/src/src/views/videoWall/dashboard/index.vue`: Tách 2 cột thống kê trực quan (Bộ điều khiển vs Màn hình), đặt thẳng hàng với danh sách.
- `TA-ITS015-WEBVUE-V1.0/src/src/views/videoWall/controllers/index.vue`: Điều chỉnh tỷ lệ độ rộng cột bảng (`min-width`/flex), sửa lỗi Solid Blue Overlay Bug.
- `TA-ITS015-WEBVUE-V1.0/src/src/views/videoWall/controllers/component/editController.vue`: Bổ sung chọn thiết bị từ `Equipment`, ẩn bắt buộc nhập IP/Password khi đã chọn `EquipmentId`.

### 2.4. Màn Hình Nguồn Tín Hiệu (`sources`)
- `TA-ITS015-WEBVUE-V1.0/src/src/views/videoWall/sources/component/editSource.vue`: Sắp xếp lại thứ tự trường: Loại nguồn tín hiệu và Cổng tín hiệu vào nằm cạnh nhau; Bộ điều khiển đưa về khối thiết bị; điều chỉnh Tab index.

### 2.5. Màn Hình Kịch Bản (`scenes`)
- `TA-ITS015-WEBVUE-V1.0/src/src/views/videoWall/scenes/index.vue`:
  - Bổ sung Header tiêu đề và thanh thống kê tóm tắt (Tổng số input đang phát, Số màn hình đang dùng, Số màn hình trống).
  - Nút "Thêm mới" đưa về gần danh sách kịch bản; nút "Lưu" đặt cố định trên Sticky Action Bar.
  - Tích hợp thanh trượt/nút thu gọn panel (Splitter / Collapsible).
  - Tương tác 2 chiều: Click danh sách thì cửa sổ trên canvas sáng lên và ngược lại.
  - Disable / gray out nguồn tín hiệu đã sử dụng trong danh sách nguồn (`source.isUsed`).
- `TA-ITS015-WEBVUE-V1.0/src/src/views/videoWall/scenes/component/sceneCanvas.vue`: Tối ưu hóa độ tương phản màu sắc màn hình, nhãn tọa độ `H1-C1`.
- `TA-ITS015-WEBVUE-V1.0/src/src/views/videoWall/scenes/component/editWindowProperties.vue`: Cân đối layout thuộc tính cửa sổ, bổ sung tooltip giải thích rõ ràng cho trường "Thứ tự lớp" (z-index) kèm nút "Lên trên cùng" / "Xuống dưới cùng".

### 2.6. Ngôn Ngữ & Dịch Thuật (i18n)
- `TA-ITS015-WEBVUE-V1.0/src/src/i18n/lang/vi-vn.json` (hoặc `its/videowall/vi-vn.json` theo Rule 20.29): Chuẩn hóa toàn bộ khóa với tiền tố `vw_`, đổi Topology $\rightarrow$ Bố cục màn hình / Sơ đồ kết nối; Controller $\rightarrow$ Bộ điều khiển.
- `TA-ITS015-WEBVUE-V1.0/src/src/i18n/lang/en-us.json` (hoặc `its/videowall/en-us.json`): Đồng bộ tương ứng.

---

## 3. Đặc Tả Chi Tiết Thay Đổi Giao Diện & Code Mẫu

### 3.1. Luồng Chia Vùng Màn Hình (`ScreenDivisionDialog.vue`)

Khi người dùng bấm nút "Chia vùng" trên toolbar của ma trận màn hình:
1. Không kích hoạt kéo chuột ngay lập tức.
2. Mở dialog `ScreenDivisionDialog`:
   - Hiển thị 4 thẻ lựa chọn trực quan có icon ma trận minh họa:
     - **1-1:** Toàn màn hình đơn (1 cửa sổ phủ kín).
     - **2x2:** Chia 4 vùng đều nhau (2 hàng × 2 cột).
     - **3x3:** Chia 9 vùng đều nhau (3 hàng × 3 cột).
     - **Tùy chỉnh (Custom):** Cho phép nhập số hàng × số cột hoặc kích hoạt chế độ kéo thả chia lưới tự do.
   - Nút "Lưu thành mẫu tùy chỉnh": Cho phép người dùng đặt tên mẫu và lưu vào danh sách template dùng lại.
3. Sau khi bấm "Áp dụng", con trỏ chuột chuyển sang biểu tượng trỏ mục tiêu để người dùng click vào màn hình muốn áp dụng kiểu chia này.

### 3.2. Cân Đối Tỷ Lệ Cột & Phân Quyền Multi-User (`editWallPermission.vue`)

```html
<template>
  <el-dialog v-model="state.isShowDialog" :title="dialogTitle" width="1100px" :close-on-click-modal="false">
    <el-row :gutter="20">
      <!-- Cột trái: Cấu hình đối tượng phân quyền (chiếm 8/24) -->
      <el-col :span="8">
        <el-card shadow="never" class="permission-sidebar-card">
          <template #header>
            <span class="font-bold">{{ $t('vw_permission_target_header') }}</span>
          </template>
          
          <el-form :model="state.ruleForm" label-position="top">
            <!-- Chế độ gán: Theo danh sách người dùng hoặc theo Phòng ban/Tổ chức -->
            <el-form-item :label="$t('vw_permission_assign_type')">
              <el-radio-group v-model="state.assignType">
                <el-radio-button label="users">{{ $t('vw_permission_type_users') }}</el-radio-button>
                <el-radio-button label="org">{{ $t('vw_permission_type_org') }}</el-radio-button>
              </el-radio-group>
            </el-form-item>

            <!-- Chọn nhiều người dùng (Multi-select) -->
            <el-form-item v-if="state.assignType === 'users'" :label="$t('vw_permission_select_users')">
              <el-select
                v-model="state.selectedUserIds"
                multiple
                filterable
                collapse-tags
                collapse-tags-tooltip
                :placeholder="$t('vw_permission_select_users_placeholder')"
                class="w100"
              >
                <el-option
                  v-for="user in state.userList"
                  :key="user.id"
                  :label="`${user.account} - ${user.realName}`"
                  :value="user.id"
                />
              </el-select>
            </el-form-item>

            <!-- Chọn Tổ chức -->
            <el-form-item v-else :label="$t('vw_permission_select_org')">
              <el-tree-select
                v-model="state.ruleForm.orgId"
                :data="state.orgTreeData"
                check-strictly
                class="w100"
                :placeholder="$t('vw_permission_select_org_placeholder')"
              />
            </el-form-item>

            <!-- Quyền truy cập toàn bộ -->
            <el-form-item>
              <el-checkbox v-model="state.ruleForm.isFullAccess">
                {{ $t('vw_permission_full_access') }}
              </el-checkbox>
            </el-form-item>
          </el-form>
        </el-card>
      </el-col>

      <!-- Cột phải: Lưới ma trận trực quan (chiếm 16/24) -->
      <el-col :span="16">
        <el-card shadow="never" class="permission-grid-card">
          <template #header>
            <div class="flex justify-between items-center">
              <span class="font-bold">{{ $t('vw_permission_grid_header') }}</span>
              <span class="text-sm text-gray-500">{{ $t('vw_permission_grid_hint') }}</span>
            </div>
          </template>

          <!-- Component Lưới ma trận chuẩn Top-Left H1-C1 -->
          <ScreenLayoutRenderer
            v-model:selectedCells="state.selectedCells"
            :disabled="state.ruleForm.isFullAccess"
            :rows="4"
            :cols="8"
            mode="select"
          />
        </el-card>
      </el-col>
    </el-row>
  </el-dialog>
</template>
```

### 3.3. Sửa Lỗi Màu Sắc Solid Blue Overlay Bug & Khôi Phục Zebra Stripes

Trong `TA-ITS015-WEBVUE-V1.0/src/src/views/videoWall`:
- Kiểm tra các file stylesheet scoped (`.scss` hoặc `.vue`) và rà soát các selector toàn cục:
  ```scss
  /* ⛔ CẤM selector ghi đè màu nền toàn bộ dòng của bảng bằng màu xanh đặc */
  /* TRƯỚC (Bị lỗi phủ màu xanh): */
  :deep(.el-table__row) {
    background-color: #2b579a !important; /* ❌ Lỗi Solid Blue Overlay */
  }
  
  /* SAU (Đúng chuẩn Thiên Ân, giữ sọc vằn zebra và hover êm dịu): */
  .vw-table-container {
    :deep(.el-table--striped .el-table__body tr.el-table__row--striped td.el-table__cell) {
      background-color: var(--el-fill-color-lighter);
    }
    :deep(.el-table__body tr:hover > td.el-table__cell) {
      background-color: var(--el-fill-color-light);
    }
  }
  ```
- Tuân thủ tuyệt đối **Rule 20.6**: ⛔ CẤM `!important` trong CSS/SCSS; sử dụng biến CSS hệ thống (`--el-fill-color-*`).

### 3.4. Bổ Sung Tooltip "Thứ Tự Lớp" (Z-Index) & Nút Trực Quan

Trong `editWindowProperties.vue`:
```html
<el-form-item :label="$t('vw_window_zindex')">
  <div class="flex items-center gap-2">
    <el-input-number
      v-model="state.ruleForm.zIndex"
      :min="1"
      :max="100"
      controls-position="right"
    />
    <el-tooltip
      :content="$t('vw_window_zindex_tooltip')"
      placement="top"
    >
      <el-icon class="cursor-pointer text-gray-400"><QuestionFilled /></el-icon>
    </el-tooltip>
    
    <!-- Nút trực quan đưa lên/xuống nhanh -->
    <el-button-group class="ml-2">
      <el-button size="small" @click="bringToFront">{{ $t('vw_btn_bring_to_front') }}</el-button>
      <el-button size="small" @click="sendToBack">{{ $t('vw_btn_send_to_back') }}</el-button>
    </el-button-group>
  </div>
</el-form-item>
```
Nội dung i18n của `vw_window_zindex_tooltip`:
- `vi-vn.json`: `"Thứ tự xếp lớp hiển thị: Giá trị càng lớn, cửa sổ càng nổi lên trên các cửa sổ khác."`
- `en-us.json`: `"Display layer order: Larger values bring the window on top of other windows."`

### 3.5. Highlight Liên Kết Trực Quan & Khóa Nguồn Tín Hiệu Đã Dùng

Trong `scenes/index.vue`:
1. **Highlight 2 chiều:**
   - Khi hover hoặc click vào một kịch bản hoặc một cửa sổ trong danh sách bên trái:
     `activeWindowId.value = window.id`
   - Trên Canvas `sceneCanvas.vue`: Cửa sổ có `window.id === activeWindowId` được kích hoạt class `.is-active-highlight` (viền sáng xanh dương nổi bật, bóng đổ nhẹ).
   - Khi click trực tiếp vào một cửa sổ trên canvas: Danh sách bên trái tự động scroll và focus vào dòng tương ứng.
2. **Khóa nguồn tín hiệu đã sử dụng:**
   - Trong danh sách các nguồn tín hiệu (Input Sources):
     ```html
     <div
       v-for="source in sourceList"
       :key="source.id"
       :class="['source-item', { 'is-disabled-in-use': source.isUsed }]"
       :draggable="!source.isUsed"
     >
       <span class="source-name">{{ source.name }}</span>
       <el-tag v-if="source.isUsed" size="small" type="info">
         {{ $t('vw_source_in_use') }}
       </el-tag>
     </div>
     ```
   - CSS cho `.is-disabled-in-use`: Màu chữ mờ nhạt (`opacity: 0.6`), nền xám nhạt, con trỏ chuột `cursor: not-allowed`, không cho kéo thả.

### 3.6. Bố Trí Thanh Thao Tác Cố Định (Sticky Action Bar) & Nút Thêm Mới

Trong `scenes/index.vue`:
- Nút "Thêm mới kịch bản" được đặt ngay trên đỉnh danh sách kịch bản (không để tít xa góc phải màn hình).
- Nút "Lưu kịch bản" được đặt trong thanh điều khiển cố định (Sticky Action Bar) ở chân hoặc đỉnh màn hình làm việc:
  ```scss
  .vw-sticky-action-bar {
    position: sticky;
    bottom: 0;
    z-index: 20;
    background-color: var(--el-bg-color);
    padding: 12px 24px;
    border-top: 1px solid var(--el-border-color-light);
    display: flex;
    justify-content: flex-end;
    gap: 12px;
  }
  ```

---

## 4. Tiêu Chí Nghiệm Thu (Acceptance Criteria)

1. **Luồng Chia Vùng:**
   - [ ] Bấm nút "Chia vùng" mở dialog chọn chế độ (1-1, 2x2, 3x3, Custom) trước khi người dùng thao tác trên màn hình.
   - [ ] Hỗ trợ lưu cấu hình tùy chỉnh thành template để dùng lại cho các màn hình khác.
2. **Phân Quyền Ma Trận:**
   - [ ] Form gán quyền cho phép chọn nhiều người dùng (`el-select multiple`) hoặc theo Tổ chức.
   - [ ] Tỷ lệ cột giữa sidebar đối tượng (8/24) và khung lưới ma trận (16/24) cân đối, không đè bẹp lưới.
3. **Dashboard & Thống Kê:**
   - [ ] Thống kê trạng thái hiển thị rõ ràng 2 khối (Bộ điều khiển vs Màn hình), đặt thẳng hàng với danh sách, hết lỗi "lé mắt".
   - [ ] Bổ sung thanh tóm tắt số lượng input đang phát, số màn hình đang dùng, số màn hình còn trống ("Còn N màn hình chưa sử dụng").
4. **Form & Tab Index:**
   - [ ] Bấm phím Tab trong các form di chuyển mượt mà từ trên xuống dưới, từ trái sang phải.
   - [ ] Các khối thông tin có `<el-divider>` phân cách rõ ràng.
   - [ ] Cụm Loại nguồn tín hiệu và Cổng tín hiệu vào nằm cạnh nhau, không bị kẹp Bộ điều khiển vào giữa.
5. **Khắc Phục Lỗi Màu Sắc & Tương Phản:**
   - [ ] Đã dọn sạch hoàn toàn lỗi Solid Blue Overlay Bug; bảng dữ liệu và danh mục khôi phục đúng sọc vằn zebra và độ tương phản sáng rõ.
   - [ ] Font size các nhãn thông số dễ đọc, phù hợp người vận hành.
6. **Kịch Bản & Cửa Sổ:**
   - [ ] Nút "Thêm mới" nằm cạnh danh sách kịch bản; nút "Lưu" nằm ở Sticky Action Bar luôn thấy khi cuộn.
   - [ ] Trường "Thứ tự lớp" có tooltip hướng dẫn và nút Đưa lên trên / Đưa xuống dưới.
   - [ ] Hỗ trợ thu gọn panel (Splitter / Collapse) để phóng to khung ma trận.
   - [ ] Highlight liên kết 2 chiều giữa danh sách và cửa sổ màn hình; rào lại (disable/gray out) nguồn tín hiệu đã sử dụng.
7. **Quy Chuẩn Thiên Ân (P0 Safeguards):**
   - [ ] 100% text, placeholder, tooltip qua `$t(...)` với tiền tố `vw_` đồng bộ cả `vi-vn.json` và `en-us.json` (Rule 20.15, Rule 20.29).
   - [ ] ⛔ CẤM `!important` trong CSS/SCSS (Rule 20.6).
   - [ ] ⛔ CẤM toán tử phủ định kép `!!` (Rule 20.8).
   - [ ] Bấm ra ngoài dialog thì đóng; form bẩn hỏi xác nhận trước khi đóng (Rule 20.26).
   - [ ] ⛔ CẤM tự ý chạy `npm run build` sau khi sửa FE (Rule 19.13).

