# Kế hoạch gộp code điều khiển VMS từ `vmsInfo.vue` vào `vmsModal.vue`

> **Dành cho người thực thi:** Plan dùng cú pháp checkbox (`- [ ]`) để theo dõi tiến độ. Thực hiện tuần tự từng Task, build/kiểm thử sau mỗi Phase.

**Mục tiêu:** Đưa kiến trúc điều khiển VMS hiện đại của `vmsInfo.vue` (API hợp nhất `VmsControlApi` + preview base64 từ BE + lập lịch run/stop + modal độ ưu tiên) sang `vmsModal.vue`, đồng thời **giữ nguyên** nghiệp vụ sự cố của `vmsModal.vue` (gợi ý thiết bị theo vị trí sự cố, câu mặc định theo loại sự cố, cập nhật trạng thái công việc). Sau khi gộp, sắp xếp lại code theo các `#SECTION` thống nhất để dễ đọc.

**Phạm vi:**
- File ĐÍCH (sửa chính): `src/src/views/modal/vms/vmsModal.vue`
- File NGUỒN (tham chiếu, copy logic): `src/src/views/bigScreen/trafficInfo/component/device/vmsInfo.vue`
- KHÔNG sửa file trong `src/src/api-services/**` (auto-generated).

**Tech stack:** Vue 3.5 `<script setup lang="ts">` / Element Plus / Pinia / Axios client tự sinh từ Swagger.

---

## 0. Kết quả nghiên cứu (đã xác minh trong code)

### 0.1. Hai điểm vào (entry point) của `vmsModal` sau khi gộp

`vmsModal.vue` sau khi gộp phải phục vụ được **2 luồng gọi khác nhau**:

| Luồng | File gọi | Cách gọi `openDialog` | Sự kiện nghe | Có `itemId`? | Có `incidentInfo`? |
|-------|----------|------------------------|--------------|--------------|---------------------|
| **Sự cố** | `views/modal/incident/incidentPopup.vue` → `showVmsModal()` | `openDialog({ incidentInfo, actionType: VmsActionType.Text, workExecuteId, workExecuteRemark, eventTypeCode })` | `@onMeSubmitSuccess` | Không | **Có** |
| **Click icon VMS** | `views/bigScreen/trafficInfo/component/equipmentModal.vue` → `loadModalInfo()` | `openDialog({ itemId, ... })` | `@closed / @closeAll / @closeOthers` | **Có** | Không |

> Hiện `equipmentModal.vue` import nhầm tên: `import VmsModal from './device/vmsInfo.vue'` (biến tên `VmsModal` nhưng trỏ tới **vmsInfo**). Đây là lý do click icon đang mở `vmsInfo`.

**Hệ quả thiết kế:** `openDialog(data)` của vmsModal gộp phải **tự nhận biết chế độ** (mode):
- Nếu `data.incidentInfo` có giá trị → **chế độ Sự cố**: set câu mặc định theo loại sự cố + load thiết bị gợi ý + gán `incidentId` khi submit + emit `onMeSubmitSuccess`.
- Nếu `data.itemId` có giá trị → **chế độ Thiết bị**: tự tick đúng thiết bị vừa click + emit `closed/closeAll/closeOthers`.

### 0.2. BE đã sẵn sàng cho sự cố

Model `src/src/api-services/vms/models/add-control-input.ts` (`AddControlInput`) **đã có sẵn**:
`controlType, actionType, content1, content2, color, backColor, font, fontSize, brightness, priority, url, size, **incidentId**, equipmentList, timeStart, timeEnd, scheduleList`.

→ API hợp nhất `VmsControlApi.apiVmsVmscontrolAddPost` xử lý được cả luồng sự cố. **Không cần BE bổ sung field.** Chỉ cần gán `state.addControlProcess.incidentId = state.incidentId` trước khi submit ở chế độ sự cố.

### 0.3. `vmsPriority.vue` đã khớp hợp đồng của vmsInfo

`views/modal/vms/vmsPriority.vue`:
- `openDialog(currentList, addControlInput: AddControlInput, baseUrl, listEquipment): boolean` — trả `true` nếu có VMS priority thấp hơn cần xác nhận (mở dialog), `false` nếu không vướng.
- emit `handleConfirm(ids: string[])`.

→ Trùng khớp `checkHasHigherPriority()` và `handlePriorityConfirm()` của vmsInfo. Dùng lại nguyên trạng, **không cần sửa** `vmsPriority.vue`.

### 0.4. Quyết định container & sự kiện

- **Giữ `vmsModal` là `el-dialog`** (đúng như `incidentPopup` đang dùng) làm luồng chính. Component tự quản lý hiển thị qua `state.isShowDialog`.
- **Phát đồng thời cả 2 nhóm sự kiện** để tương thích 2 caller: `onMeSubmitSuccess` (sự cố) và `closed/closeAll/closeOthers` (thiết bị). Caller nào không nghe thì sự kiện tự bỏ qua — không gây lỗi.
- Với luồng click icon: sau khi gộp, đổi import trong `equipmentModal.vue` sang `vmsModal.vue` **là bước tùy chọn cuối** (Phase G), chỉ làm khi luồng sự cố đã chạy ổn. Lưu ý `equipmentModal` nhúng component trong khung kéo-thả; nếu vmsModal là `el-dialog` thì nó sẽ bung overlay riêng — chấp nhận được, nhưng cần xác nhận UX với người dùng trước khi đổi.

---

## 1. So sánh hiện trạng hai file

| Tiêu chí | `vmsInfo.vue` (nguồn) | `vmsModal.vue` (đích) |
|----------|----------------------|------------------------|
| Container | Panel (tab Info + Control), emit `closed/closeOthers/closeAll` | `el-dialog`, emit `onMeSubmitSuccess` |
| API điều khiển | `VmsControlApi.apiVmsVmscontrolAddPost(AddControlInput)` (hợp nhất) | `VmsCurrentApi` + `VmsProcessApi` (Add/Update rời rạc) |
| Preview | Gọi BE `apiVmsVmscontrolPreviewPost` → base64, debounce + watch | Dựng bằng CSS phía client |
| Loại điều khiển | Text/Message/Phrase/Image/Template/Time/Schedule (enum mới) | Lẫn enum cũ: ImageTemplate/Timer/Scheduling + mới |
| Lập lịch | `submitrun`/`submitstop`, `isRunning`, `loadListVmsRunning` | `draggableImages` thủ công, không có run/stop |
| Độ ưu tiên | `VmsPriorityModal` (gọn) | `ElMessageBox.confirm` lặp lại nhiều lần (rối) |
| Chọn thiết bị | Checkbox nhóm theo hướng (`equipmentListGroupByDirection`) | `recommendedEquipmentGroups` (gợi ý theo sự cố) |
| Nghiệp vụ sự cố | Không có | **Có** (incidentInfo, eventTypeCode, workExecute) |

**Kết luận:** `vmsInfo.vue` có lõi điều khiển tốt hơn. Lấy lõi đó làm nền, ghép thêm nghiệp vụ sự cố của `vmsModal.vue`.

---

## 2. Phần GIỮ LẠI từ `vmsModal.vue` (nghiệp vụ sự cố)

Những phần này là giá trị riêng của modal, **không có** ở `vmsInfo.vue`, bắt buộc giữ:

1. `onDialogOpened(row)` — nhận `workExecuteId`, `workExecuteRemark`, `eventTypeCode`, `incidentInfo`, `incidentId`, `actionType`.
2. `setDefaultVmsMessage(actionType)` — set câu mặc định row1/row2 theo loại sự cố (incident/accident/fire/FA/trafficJam) lấy từ `SysTerminologyApi` qua `VmsMessageForIncidentEnum`.
3. `loadRecommendedEquipmentList()` — gợi ý thiết bị VMS theo `kmNumber/metNumber` + hướng của sự cố + bán kính cấu hình (`operation_config` small/large range), gọi `apiTmsTmsequipmentListbypositionanddirectionPost`.
4. `recommendedEquipmentGroups` (computed) — nhóm thiết bị gợi ý theo hướng + tự tick `isRecommended`.
5. `onSubmitData()` — cập nhật trạng thái công việc sự cố qua `TmsWorkExecuteApi.apiTmsTmsworkexecuteUpdatestatePost`.
6. `emit('onMeSubmitSuccess')` sau khi submit thành công (để màn cha refresh).
7. Container `el-dialog` + `defineExpose({ openDialog })` (cách parent gọi modal giữ nguyên).

---

## 3. Phần MANG SANG từ `vmsInfo.vue` (lõi điều khiển)

Copy/điều chỉnh các khối sau (thay container panel → dùng trong `el-dialog`):

1. **State chuẩn hoá:** `ruleForm`, `inputDisplay`, `listData`, `addControlProcess (AddControlInput)`, `scheduleImageList`, `preview`, `previewTemplateJson`, `listVmsRunningSchedule`, `listInformationProcess`.
2. **Load data:** `loadDataControl`, `loadImgageAndTemplateData`, `loadVmsFontSizeConfig`, `loadListVmsRunning`, `loadProcessData`, `displayInformationProcess`.
3. **Chuyển đổi hiển thị theo loại:** `onVmsActionlTypeChange`, `toggleControlDisplay`, `resetControlToDefault`, `bindVmsDataToSelect`.
4. **Message/Phrase:** `onMessageSelectChange`, `onWordList1SelectChange`, `onWordList2SelectChange`.
5. **Định dạng chữ:** `formatRowCase`, `formatTemplateElementCase`, `updatePreview`.
6. **Ảnh:** `handleImageUpload`, `uploadImage`, `beforeUpload`, `updateSelectedImageUrl`.
7. **Template:** `updateSelectedTemplateUrl`, `syncTemplateJson`, `textElements`/`imageElements` (computed), `handleImageTemplateSelectChange`.
8. **Timer/Schedule:** `handleTimerChange`, `handleSheduleChange`, `removeAt`, `onMove`, `moveItem`, `submitrun`, `submitstop`, `isRunning`.
9. **Submit hợp nhất:** `prepareDataSubmit`, `getListEquipment`, `checkHasHigherPriority`, `handlePriorityConfirm`, `processOnSubmit`.
10. **Preview API:** `isApiRenderType`, `buildPreviewInput`, `fetchPreview`, `schedulePreview` (debounce) + `watch` + huỷ ở `onUnmounted`.
11. Component con `VmsPriorityModal`.

> Lưu ý loại bỏ những thứ không cần: drag/resize template thủ công (`startDrag/onDrag/startResize/onResize`...) nếu UI modal không dùng; phần `previewTemplate`/`previewSchedule` dư thừa.

---

## 4. Cấu trúc `#SECTION` đích (sắp xếp lại cho dễ đọc)

Áp dụng cho **cả template và script** của `vmsModal.vue` sau khi gộp:

```
TEMPLATE
  └─ el-dialog
      1. Action Type selector
      2. SELECT vùng chọn (image/template/message/phrase)
      3. CONTENT inputs (row1/row2 + word list + template text elements)
      4. TIME controls (Time)
      5. STYLE controls (font/color/background/fontSize)
      6. BRIGHTNESS + PRIORITY
      7. PREVIEW (base64 từ API / image / schedule draggable)
      8. RECOMMENDED EQUIPMENT (nhóm theo hướng - nghiệp vụ sự cố)
      9. FOOTER (cancel / confirm / run / stop)

SCRIPT (theo thứ tự #SECTION)
  #SECTION: IMPORTS & SETUP
  #SECTION: STATE
  #SECTION: INCIDENT (nghiệp vụ giữ lại)
      - onDialogOpened, setDefaultVmsMessage, loadRecommendedEquipmentList,
        recommendedEquipmentGroups, onSubmitData
  #SECTION: LOAD DATA
      - loadDataControl, loadImgageAndTemplateData, loadVmsFontSizeConfig,
        loadProcessData, loadListVmsRunning, displayInformationProcess
  #SECTION: DIALOG METHODS (openDialog, cancel)
  #SECTION: CONTROL DISPLAY (onVmsActionlTypeChange, toggleControlDisplay, reset, bind)
  #SECTION: MESSAGE & WORD
  #SECTION: TEXT FORMAT & PREVIEW HELPERS
  #SECTION: IMAGE
  #SECTION: TEMPLATE
  #SECTION: SELECT CHANGE ROUTER (handleImageTemplateSelectChange)
  #SECTION: TIMER
  #SECTION: SCHEDULING (handleSheduleChange, removeAt, moveItem, submitrun, submitstop, isRunning)
  #SECTION: SUBMIT (prepareDataSubmit, getListEquipment, checkHasHigherPriority,
                    handlePriorityConfirm, processOnSubmit, submit)
  #SECTION: PREVIEW QUA API (isApiRenderType, buildPreviewInput, fetchPreview, schedulePreview, watch)
  #SECTION: LIFECYCLE (onMounted, onUnmounted, defineExpose)
```

---

## 5. Các bước thực hiện

### Phase A — Chuẩn bị & xác minh phụ thuộc

- [ ] **A1. Xác minh API có sẵn (không tạo file api-services):** Kiểm tra trong `src/src/api-services/vms` đã export `VmsControlApi`, `AddControlInput`, `ControlScheduleInput`, `ControlEquipmentInput`, `PreviewInput`, `ProcessStatusOutput`, `InformationProcessOutput`. (vmsInfo.vue đang dùng → đã có.) Nếu thiếu → báo lại, chờ regen Swagger, KHÔNG tự tạo.
- [ ] **A2. Xác minh `VmsPriorityModal`:** đường dẫn `/@/views/modal/vms/vmsPriority.vue` tồn tại và signature `openDialog(listSelected, editForm, baseUrl, equipmentList)` + emit `handle-confirm`.
- [ ] **A3. Xác minh enum:** `VmsActionType` (Text/Message/Phrase/Image/Template/Time/Schedule), `VmsMessageForIncidentEnum`, `TmsEventType`, `TmsIncidentConfigCodeEnum`, `TmsWorkExecuteStateEnum`.
- [ ] **A4. Sao lưu file đích:** ghi chú lại danh sách parent đang gọi `vmsModal.vue` để bảo đảm `openDialog(row)` + emit `onMeSubmitSuccess` không đổi hợp đồng.

### Phase B — Dựng khung script mới

- [ ] **B1.** Tạo phần IMPORTS hợp nhất: gộp import của cả 2 file, loại trùng. Thêm `VmsControlApi`, các model control, `VmsPriorityModal`, `debounce` (lodash-es); giữ `SysTerminologyApi`, `TmsWorkExecuteApi`, `TmsEquipmentTypeApi` cho nghiệp vụ sự cố.
- [ ] **B2.** Thay `state` cũ bằng `state` chuẩn của vmsInfo, **bổ sung** các field sự cố: `incidentInfo`, `incidentId`, `eventTypeCode`, `workExecuteId`, `workExecuteRemark`, `recommendedEquipmentList`, `selectedRecommendedEquipmentList`, `vmsMessageDefaultList`, `directionList`.
- [ ] **B3.** Khai báo `priorityModalRef`, `ruleFormRef`, `selectedTemplate`, `mapStore`, `getConfigDataByCode`, `getOpConfigItemByCode`, `getOpConfigListByGroupCode`.

### Phase C — Mang lõi điều khiển sang

- [ ] **C1.** Copy các hàm #SECTION CONTROL DISPLAY / MESSAGE & WORD / TEXT FORMAT / IMAGE / TEMPLATE / SELECT ROUTER / TIMER / SCHEDULING từ vmsInfo, chỉnh tham chiếu state cho khớp.
- [ ] **C2.** Copy #SECTION PREVIEW QUA API (`isApiRenderType`, `buildPreviewInput`, `fetchPreview`, `schedulePreview`, `watch`, huỷ ở `onUnmounted`).
- [ ] **C3.** Copy #SECTION SUBMIT: `prepareDataSubmit`, `getListEquipment`, `checkHasHigherPriority`, `handlePriorityConfirm`, `processOnSubmit`, `submitrun`, `submitstop`, `isRunning`.
- [ ] **C4.** Thay toàn bộ `processOnSubmit` cũ của vmsModal (lặp code `VmsCurrentApi`/`VmsProcessApi` + confirm thủ công) bằng `processOnSubmit` mới gọi `VmsControlApi`.

### Phase D — Ghép nghiệp vụ sự cố + nhận biết chế độ

- [ ] **D1.** Đưa `onDialogOpened`, `setDefaultVmsMessage`, `loadRecommendedEquipmentList`, `recommendedEquipmentGroups`, `onSubmitData`, `loadDefaultData` (terminology + direction) vào #SECTION INCIDENT.
- [ ] **D2. `openDialog(data)` nhận biết 2 chế độ:**

  ```ts
  const openDialog = async (data: any) => {
      state.isShowDialog = true;
      ruleFormRef.value?.resetFields();
      state.scheduleImageList = [];

      // Phân biệt chế độ theo dữ liệu đầu vào
      state.mode = data?.incidentInfo ? 'incident' : 'device';
      state.itemId = data?.itemId ?? null;

      // Load dữ liệu dùng chung
      await loadDataInfo();      // baseUrl + equipment + group by direction
      await loadCurrentData();
      await loadDataControl();   // control type, color/font/size, message/phrase, image/template
      await loadProcessData();

      const actionType = data?.actionType ?? VmsActionType.Text;
      state.ruleForm.controlType = actionType;
      toggleControlDisplay();

      if (state.mode === 'incident') {
          // Nghiệp vụ sự cố
          state.incidentInfo     = data.incidentInfo;
          state.incidentId       = data.incidentInfo?.id ?? null;
          state.eventTypeCode    = data.eventTypeCode;
          state.workExecuteId    = data.workExecuteId;
          state.workExecuteRemark= data.workExecuteRemark;
          await loadDefaultData();          // terminology câu mặc định
          setDefaultVmsMessage(actionType); // set row1/row2 theo loại sự cố
          await loadRecommendedEquipmentList(); // tự tick thiết bị gợi ý
      } else {
          // Chế độ thiết bị: tự tick đúng VMS vừa click
          state.incidentInfo = null;
          state.incidentId = null;
          if (state.itemId) state.listVmsSelected = [state.itemId];
      }

      schedulePreview();
  };
  ```

- [ ] **D3. Thống nhất nguồn thiết bị submit.** Dùng MỘT mảng `state.listVmsSelected` cho `getListEquipment()`:
  - Chế độ sự cố: đồng bộ `listVmsSelected` ← `selectedRecommendedEquipmentList` (có thể dùng `watch` hoặc gán trong `submit`).
  - Chế độ thiết bị: `listVmsSelected = [itemId]` (đã set ở D2).
  - `getListEquipment()` luôn map từ `state.listVmsSelected`.
- [ ] **D4. `submit()` hợp nhất:**

  ```ts
  const submit = async () => {
      await new Promise(r => ruleFormRef.value?.validate(() => r(null)));

      // Đồng bộ thiết bị đã chọn (chế độ sự cố lấy từ recommended)
      if (state.mode === 'incident')
          state.listVmsSelected = [...state.selectedRecommendedEquipmentList];

      // Chế độ sự cố: cập nhật trạng thái công việc trước
      if (state.mode === 'incident') {
          const ok = await onSubmitData();
          if (!ok) { ElMessage.error($t('lz.message.base.processUnSuccess')); return; }
      }

      prepareDataSubmit();
      state.addControlProcess.incidentId = state.incidentId; // gán liên kết sự cố (null nếu device)

      const listSelected = state.listData.currentList.filter((x:any) => state.listVmsSelected.includes(x.equipmentId));
      const needPriority = checkHasHigherPriority(listSelected, state.addControlProcess);
      if (!needPriority) {
          await processOnSubmit();
          emitDone();   // emit('onMeSubmitSuccess') + emit('closed')
          cancel();
      }
      // nếu vướng ưu tiên → VmsPriorityModal xử lý qua handlePriorityConfirm
  };
  ```

- [ ] **D5.** `handlePriorityConfirm()` sau khi `processOnSubmit()` cũng gọi `emitDone()`.
- [ ] **D6.** Tạo helper `emitDone()` phát cả `onMeSubmitSuccess` (cho incident) lẫn `closed` (cho device) để tương thích 2 caller.

### Phase E — Template

- [ ] **E1.** Dựng lại template trong `el-dialog` theo cấu trúc #SECTION mục 4, tái dùng markup control của vmsInfo (đã có sẵn các vùng select/content/style/preview).
- [ ] **E2. Vùng chọn thiết bị theo chế độ** (cả hai đều bind `state.listVmsSelected` qua nhóm theo hướng `equipmentListGroupByDirection`):
  - Chế độ **sự cố**: hiển thị danh sách **gợi ý** (`recommendedEquipmentGroups`) + tự tick `selectedRecommendedEquipmentList`.
  - Chế độ **thiết bị**: hiển thị toàn bộ VMS nhóm theo hướng, tick sẵn thiết bị vừa click.
  - Có thể dùng `v-if="state.mode === 'incident'"` để chọn block hiển thị phù hợp.
- [ ] **E3.** Footer: nút `cancel`/`confirm`; với `controlType === Schedule` hiển thị `run`/`stop` (`submitrun`/`submitstop`) như vmsInfo.
- [ ] **E4.** Thêm `<VmsPriorityModal ref="priorityModalRef" @handle-confirm="handlePriorityConfirm" />` (import từ `/@/views/modal/vms/vmsPriority.vue`).

### Phase F — Dọn dẹp & kiểm thử

- [ ] **F1.** Xoá code chết: `draggableImages` cũ, `editProcessForm`/`editCurrentForm`, `TransporterSocketCluster` (chưa import — gây lỗi), preview CSS cũ, các hàm trùng, toàn bộ `processOnSubmit` cũ dùng `VmsCurrentApi`/`VmsProcessApi`.
- [ ] **F2.** Chạy `pnpm lint-fix` cho file, sửa lỗi TS chặn build.
- [ ] **F3.** `get_diagnostics` trên `vmsModal.vue` → không còn lỗi compile.
- [ ] **F4. Kiểm thử luồng SỰ CỐ** (người dùng chạy `pnpm dev`): mở từ `incidentPopup` → câu mặc định đúng loại sự cố, thiết bị gợi ý tự tick, preview base64, các loại điều khiển, run/stop schedule, modal ưu tiên, sau submit cập nhật work execute + reload sự cố (`onMeSubmitSuccess`).

### Phase G — (Tùy chọn) Chuyển luồng click icon sang dùng `vmsModal`

> Chỉ làm sau khi Phase F xanh. Cần xác nhận UX với người dùng vì `vmsModal` là `el-dialog` còn `vmsInfo` là panel trong khung kéo-thả.

- [ ] **G1.** Trong `equipmentModal.vue` đổi `import VmsModal from './device/vmsInfo.vue'` → `import VmsModal from '/@/views/modal/vms/vmsModal.vue'`.
- [ ] **G2.** Giữ template `<VmsModal @closed @closeAll @closeOthers>` — nhờ `emitDone()` + các emit `closeAll/closeOthers` đã bổ sung, hợp đồng vẫn khớp.
- [ ] **G3.** Kiểm thử: click icon VMS → mở vmsModal ở chế độ thiết bị, tick sẵn đúng VMS, điều khiển + submit hoạt động, đóng đồng bộ.
- [ ] **G4.** Nếu UX el-dialog không phù hợp khung floating → cân nhắc giữ `vmsInfo` cho luồng click icon và chỉ dùng `vmsModal` cho sự cố (chấp nhận 2 component nhưng dùng chung phần lõi sau này tách thành composable).

---

## 6. Rủi ro & lưu ý

- **Payload BE đã thống nhất:** bản mới lưu qua `VmsControlApi.apiVmsVmscontrolAddPost`. Model `AddControlInput` **đã có `incidentId`** → luồng sự cố lưu được liên kết. Chỉ cần nhớ gán `addControlProcess.incidentId` ở chế độ sự cố (device để `null`).
- **`getListEquipment`:** luôn map từ `state.listVmsSelected`; ở chế độ sự cố nhớ đồng bộ từ `selectedRecommendedEquipmentList` trước khi submit để không gửi rỗng.
- **Hai nhóm sự kiện:** dùng `emitDone()` phát cả `onMeSubmitSuccess` và `closed` để tương thích cả `incidentPopup` lẫn `equipmentModal`.
- **`onUnmounted`:** bắt buộc `schedulePreview.cancel()` để tránh set state sau khi đóng dialog.
- **Hợp đồng với parent:** giữ nguyên tên `openDialog` và event `onMeSubmitSuccess` để `incidentPopup` không vỡ.
- **`TransporterSocketCluster`:** code cũ của vmsModal dùng biến này nhưng chưa import (lỗi runtime tiềm ẩn) — xoá khi thay `processOnSubmit`.
- **UX khung kéo-thả (Phase G):** `vmsModal` là `el-dialog`; nếu nhúng trong `equipmentModal` cần xác nhận trải nghiệm trước khi đổi import.

---

## 7. Tiêu chí hoàn thành (Definition of Done)

- [ ] `vmsModal.vue` dùng `VmsControlApi` cho mọi loại điều khiển, có preview base64.
- [ ] Giữ đủ nghiệp vụ sự cố: câu mặc định theo loại sự cố, gợi ý thiết bị, cập nhật trạng thái công việc, emit refresh.
- [ ] Có lập lịch run/stop và modal độ ưu tiên.
- [ ] Code sắp xếp theo các `#SECTION` ở mục 4, không còn code chết.
- [ ] `get_diagnostics` sạch lỗi; `pnpm lint-fix` không báo lỗi chặn build.

---

## 8. Cân nhắc cấu trúc 2 tab (Info + Control) trong modal gộp

> Mục tiêu người dùng: modal gộp **giữ 2 tab Info + Control của `vmsInfo`** + **nghiệp vụ sự cố**. Điểm vênh lớn nhất: tab Info gắn với **một** thiết bị (`itemId`), còn sự cố không có thiết bị cụ thể (nhiều thiết bị gợi ý).

### 8.1. Phụ thuộc `itemId` của tab Info (đã xác minh trong code)
- Tab Info hiển thị `state.vmsData` = `equipmentList.find(u => u.id === state.itemId)` và ảnh `state.vmsCurrent.url` = `currentList.find(x => x.equipmentId === state.itemId)`.
- `loadDataInfo()`, `loadCurrentData()`, `loadProcessData()`, `displayInformationProcess()` đều đọc `state.itemId`.
- → Chế độ sự cố (`incidentInfo`, không `itemId`) sẽ làm tab Info rỗng nếu bê nguyên.

### 8.2. Quyết định cần chốt

- [ ] **Q1. Tab Info ở chế độ sự cố** (chọn 1):
  - (A) Ẩn tab Info, chỉ hiện tab Control. *(đơn giản nhất, khuyến nghị)*
  - (B) Tab Info hiển thị thông tin sự cố (`incidentInfo`).
  - (C) Tab Info đổ thông tin của thiết bị đang tick chọn trong danh sách gợi ý.
- [ ] **Q2. Tab mặc định:** device → `info`; incident → `control`. Đặt `state.activeTab` theo `state.mode` trong `openDialog`.
- [ ] **Q3. Layout tab trong `el-dialog`:** đưa `el-tabs` vào body; phần header tên/vị trí thiết bị của `vmsInfo` chỉ hiện ở device mode (incident mode hiển thị tiêu đề sự cố hoặc ẩn).
- [ ] **Q4. Guard `itemId` null:** thêm kiểm tra `if (state.itemId)` trong `loadDataInfo/loadCurrentData/loadProcessData/displayInformationProcess` để không crash ở incident mode.

### 8.3. Bổ sung vào Phase E (template) & Phase D (openDialog)

- [ ] **E0. Khung tab:** dựng `el-tabs v-model="state.activeTab"` trong body `el-dialog` với 2 `el-tab-pane`: `info`, `control`. Bọc tab Info bằng `v-if="state.mode === 'device'"` (nếu chọn Q1-A).
- [ ] **D7.** Trong `openDialog`: `state.activeTab = state.mode === 'incident' ? 'control' : 'info'`.

### 8.4. State bổ sung
```ts
state.mode = 'device' as 'device' | 'incident';
state.activeTab = 'info' as 'info' | 'control';
```

> Các phần còn lại (tab Control, preview API, schedule, priority, submit hợp nhất) dùng chung cho cả 2 chế độ — không phụ thuộc `itemId`, nên không cần tách.
