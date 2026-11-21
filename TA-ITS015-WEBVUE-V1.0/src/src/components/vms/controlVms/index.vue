<!--
  Component dùng chung điều khiển thiết bị VMS (control + preview + chọn thiết bị).
  - mode = 'manual'   : điều khiển thủ công (mở từ thiết bị VMS trên bản đồ) -> chọn thiết bị group theo hướng.
  - mode = 'incident' : điều khiển theo sự cố -> danh sách thiết bị gợi ý theo vị trí/hướng sự cố.
  Lưu ý: nút Xác nhận/Hủy KHÔNG nằm ở đây mà ở component cha; cha gọi submit() qua ref.
-->
<template>
  <div class="vms-control">
    <!-- vc-form: phạm vi áp tông tối (biến màu). Modal ưu tiên / sự cố nằm NGOÀI form nên không bị ảnh hưởng. -->
    <!-- label-width CỐ ĐỊNH (không dùng "auto"): "auto" đo độ rộng nhãn bằng getComputedStyle nên khi form nằm trong tab/panel đang ẩn
         (display:none) phép đo ra NaN -> các nhãn lệch hàng sau khi chuyển tab rồi quay lại. 132px đủ cho nhãn dài nhất (vi/en). -->
    <el-form :model="state.ruleForm" ref="ruleFormRef" label-width="132px" label-position="left" class="vc-form">
      <!-- ============ Preview (đưa lên đầu: mô phỏng biển LED là trọng tâm của form) ============ -->
      <div v-if="state.inputDisplay.preview || state.inputDisplay.previewImage || state.inputDisplay.previewSchedule" class="border-content-info vc-section vc-preview" :class="{ 'is-sticky': !state.inputDisplay.previewSchedule }">
        <!-- Preview text/message/phrase/template (ảnh base64 từ API) hoặc ảnh (Image / Time) -->
        <template v-if="state.inputDisplay.preview || state.inputDisplay.previewImage">
          <div class="vc-preview-head">
            <span class="vc-section-title">{{ $t('lz.label.base.preview') }}</span>
            <div class="vc-preview-chips">
              <!-- Chế độ xem: mô phỏng điểm LED (phóng to) hoặc ảnh gốc như trước (kích thước thật, không hiệu ứng) -->
              <div class="vc-view-toggle" role="group">
                <button type="button" class="vc-view-btn" :class="{ 'is-active': previewMode === 'pixel' }" @click="setPreviewMode('pixel')">
                  {{ $t('lz.label.vmsControl.previewPixel') }}
                </button>
                <button type="button" class="vc-view-btn" :class="{ 'is-active': previewMode === 'image' }" @click="setPreviewMode('image')">
                  {{ $t('lz.label.vmsControl.previewImage') }}
                </button>
              </div>
              <span v-if="state.inputDisplay.brightnessSlider" class="vc-chip">
                <el-icon><Sunny /></el-icon>{{ state.ruleForm.brightness }}%
              </span>
            </div>
          </div>
          <div class="led-board" :class="{ 'is-plain': previewMode === 'image', 'is-square': props.deviceType === VmsDeviceType.Lcs }" v-loading="state.preview.loading" element-loading-background="rgba(5, 10, 20, 0.6)">
            <transition name="led-swap" mode="out-in">
              <!-- Vừa mở form thủ công: hiện ẢNH ĐANG CHẠY trên biển (VmsCurrent.Url, đúng với mọi loại điều khiển).
                   Người dùng sửa nội dung / đổi loại điều khiển -> chuyển sang preview render như bình thường. -->
              <div v-if="currentContentUrl" key="current-content" class="led-screen">
                <img :src="currentContentUrl" :alt="$t('lz.label.vms.templateImage')" class="led-image" @error="state.preview.showCurrent = false" />
              </div>
              <div v-else-if="state.inputDisplay.preview && state.preview.src" :key="state.preview.src" class="led-screen">
                <img :src="state.preview.src" :alt="$t('lz.label.vms.templateImage')" class="led-image" />
              </div>
              <!-- Ảnh thư viện không qua API render nên mô phỏng độ sáng bằng CSS filter -->
              <div v-else-if="state.inputDisplay.previewImage && state.ruleForm.urlImage" :key="previewImageUrl" class="led-screen">
                <img :src="previewImageUrl" :alt="$t('lz.label.vms.templateImage')" class="led-image" :style="{ filter: previewImageFilter }" />
              </div>
              <div v-else key="empty" class="led-empty">
                <span v-if="state.inputDisplay.preview && state.preview.error" class="led-error">{{ state.preview.error }}</span>
              </div>
            </transition>
          </div>
        </template>

        <!-- Preview lập lịch (Schedule) + nút Run/Stop -->
        <el-row v-if="state.inputDisplay.previewSchedule">
          <el-card class="preview w100">
            <div class="schedule-card-header">
              <span class="schedule-card-title">{{ $t('lz.label.base.preview') }}</span>
              <div class="schedule-card-actions">
                <el-button v-if="!state.isRunningSchedule && state.isLoadingSchedule" type="primary" size="small" loading>{{ $t('lz.button.base.run') }}</el-button>
                <el-button v-else-if="!state.isRunningSchedule" type="primary" size="small" @click="submitrun">{{ $t('lz.button.base.run') }}</el-button>
                <el-button v-else-if="state.isRunningSchedule && state.isLoadingSchedule" type="danger" size="small" loading>{{ $t('lz.button.base.stop') }}</el-button>
                <el-button v-else type="danger" size="small" @click="submitstop">{{ $t('lz.button.base.stop') }}</el-button>
              </div>
            </div>
            <draggable tag="ul" :list="state.scheduleImageList" handle=".handle" class="draggable-container" :animation="150" ghost-class="sortable-ghost" chosen-class="sortable-chosen" :force-fallback="true" @move="onMove" :delay="100" drag-class="sortable-drag" :sort="true" itemKey="id" lockAxis="y" :fallbackOnBody="true">
              <template #item="{ element, index }">
                <li class="list-group-item">
                  <div class="schedule-item-row">
                    <div class="reorder-buttons horizontal-buttons" v-if="!state.isLoadingSchedule">
                      <i class="fa fa-arrow-up" @click.stop="moveItem(index, index - 1)" :class="{ disabled: index === 0 }" style="cursor: pointer; margin: 0 2px; color: #409eff" :title="$t('lz.label.vms.moveUp')" />
                      <i class="fa fa-arrow-down" @click.stop="moveItem(index, index + 1)" :class="{ disabled: index === state.scheduleImageList.length - 1 }" style="cursor: pointer; margin: 0 2px; color: #409eff" :title="$t('lz.label.vms.moveDown')" />
                    </div>
                    <img :src="mapStore.baseUrl + '/' + element.preview" :alt="$t('lz.label.vms.templateImage')" class="schedule-item-thumb" />
                    <div class="schedule-ttl-wrap">
                      <el-input-number v-model="element.timeToLive" :min="3" :max="999" controls-position="right" size="small" style="width: 88px" />
                    </div>
                    <i v-if="!state.isLoadingSchedule" class="fa fa-times" @click.stop="removeAt(index)" style="cursor: pointer; color: #dc3545; font-size: 1.2em; margin-left: 4px" :title="$t('lz.label.vms.remove')" />
                  </div>
                </li>
              </template>
            </draggable>
          </el-card>
        </el-row>
      </div>

      <!-- ============ Vùng điều khiển theo loại (control type) ============ -->
      <div class="border-vms-info vc-section vc-controls" :class="{ mt10: state.inputDisplay.preview || state.inputDisplay.previewImage || state.inputDisplay.previewSchedule }">
        <el-row :gutter="10">
          <!-- Loại điều khiển (ẩn với LCS vì chỉ hỗ trợ Image) -->
          <el-col v-if="props.deviceType !== VmsDeviceType.Lcs" :span="24" class="mb5">
            <el-form-item :label="$t('lz.label.vms.controlType')">
              <el-select v-model="state.ruleForm.controlType" @change="onVmsActionlTypeChange" class="w100">
                <el-option v-for="item in state.listData.controlTypeList" :key="item.code" :label="item.name" :value="item.code?.trim()" />
              </el-select>
            </el-form-item>
          </el-col>

          <!-- Chọn ảnh / template -->
          <el-col v-if="state.inputDisplay.imageSelect || state.inputDisplay.templateSelect" :span="24" class="mb5">
            <el-form-item :label="state.inputDisplay.imageSelect ? $t('lz.label.base.image') : $t('lz.label.base.template')">
              <el-select
                v-model="state.ruleForm.imgTemplate"
                popper-class="simple-img_select"
                :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.label.base.image') })"
                filterable clearable class="w100"
                @change="handleImageTemplateSelectChange"
              >
                <el-option value="" v-if="state.inputDisplay.imageSelect">
                  <el-upload class="image-uploader" :auto-upload="false" :show-file-list="false" :on-change="handleImageUpload" :before-upload="beforeUpload">
                    <el-button>{{ $t('lz.button.base.upload') }} {{ $t('lz.label.base.image') }}</el-button>
                  </el-upload>
                </el-option>
                <el-option v-for="item in state.listData.imageList" v-if="state.inputDisplay.imageSelect" :key="item.id" :label="item.name" :value="item.id">
					<img :src="mapStore.baseUrl + '/' + item.url" :alt="$t('lz.label.vms.templateImage')" class="img-option-thumb" />
					<span class="img-option-name">{{ item.name }} ({{ item.size }}px)</span>
				</el-option>
				<el-option v-for="item in state.listData.templateList" v-if="!state.inputDisplay.imageSelect" :key="item.id" :label="item.name" :value="item.id">
					<img :src="mapStore.baseUrl + '/' + item.preview" :alt="$t('lz.label.vms.templateImage')" class="img-option-thumb" />
					<span class="img-option-name">{{ item.name }}</span>
				</el-option>
              </el-select>
            </el-form-item>
          </el-col>

          <!-- Sửa nội dung từng ô text của template -->
          <template v-if="state.ruleForm.controlType === VmsActionType.Template && textElements.length">
            <el-col v-for="el in textElements" :key="el.id" :span="24" class="mb5">
              <el-form-item :label="el.label || el.id">
                <div class="input-with-case-btns">
                  <el-input v-model="el.content" :placeholder="el.label || el.id" :maxlength="contentMaxLength" show-word-limit clearable @input="syncTemplateJson" />
                  <div class="case-btn-group">
                    <el-tooltip :auto-close="1000" :content="$t('lz.label.base.upperCase')" placement="top"><el-button size="small" class="case-btn" @click="formatTemplateElementCase(el, 'upper')">AA</el-button></el-tooltip>
                    <el-tooltip :auto-close="1000" :content="$t('lz.label.base.titleCase')" placement="top"><el-button size="small" class="case-btn" @click="formatTemplateElementCase(el, 'title')">Ab</el-button></el-tooltip>
                    <el-tooltip :auto-close="1000" :content="$t('lz.label.base.lowerCase')" placement="top"><el-button size="small" class="case-btn" @click="formatTemplateElementCase(el, 'lower')">bc</el-button></el-tooltip>
                  </div>
                </div>
              </el-form-item>
            </el-col>
          </template>

          <!-- Chọn message -->
          <el-col v-if="state.inputDisplay.messageSelect" :span="24" class="mb5">
            <el-form-item :label="$t('lz.entity.vms.messageSelect')">
              <el-select v-model="state.selectedMessage" @change="onMessageSelectChange" class="w100">
                <el-option v-for="item in state.listData.messageList" :key="item.code" :label="item.name" :value="item.code?.trim()" />
              </el-select>
            </el-form-item>
          </el-col>

          <!-- Nội dung 1 -->
          <el-col v-if="state.inputDisplay.content1Input" :span="24" class="mb5">
            <el-form-item :label="$t('lz.entity.vmsMessage.content1')">
              <div class="input-with-case-btns">
                <el-input v-model="state.ruleForm.row1" :placeholder="$t('lz.entity.vmsMessage.content1')" :maxlength="contentMaxLength" show-word-limit clearable @input="updatePreview" />
                <div class="case-btn-group">
                  <el-tooltip :auto-close="1000" :content="$t('lz.label.base.upperCase')" placement="top"><el-button size="small" class="case-btn" @click="formatRowCase('row1', 'upper')">AA</el-button></el-tooltip>
                  <el-tooltip :auto-close="1000" :content="$t('lz.label.base.titleCase')" placement="top"><el-button size="small" class="case-btn" @click="formatRowCase('row1', 'title')">Ab</el-button></el-tooltip>
                  <el-tooltip :auto-close="1000" :content="$t('lz.label.base.lowerCase')" placement="top"><el-button size="small" class="case-btn" @click="formatRowCase('row1', 'lower')">bc</el-button></el-tooltip>
                </div>
              </div>
            </el-form-item>
          </el-col>

          <!-- Nội dung 2 -->
          <el-col v-if="state.inputDisplay.content2Input" :span="24" class="mb5">
        <el-form-item :label="$t('lz.entity.vmsMessage.content2')">
              <div class="input-with-case-btns">
                <el-input v-model="state.ruleForm.row2" :placeholder="$t('lz.entity.vmsMessage.content2')" :maxlength="contentMaxLength" show-word-limit clearable @input="updatePreview" />
                <div class="case-btn-group">
                  <el-tooltip :auto-close="1000" :content="$t('lz.label.base.upperCase')" placement="top"><el-button size="small" class="case-btn" @click="formatRowCase('row2', 'upper')">AA</el-button></el-tooltip>
                  <el-tooltip :auto-close="1000" :content="$t('lz.label.base.titleCase')" placement="top"><el-button size="small" class="case-btn" @click="formatRowCase('row2', 'title')">Ab</el-button></el-tooltip>
                  <el-tooltip :auto-close="1000" :content="$t('lz.label.base.lowerCase')" placement="top"><el-button size="small" class="case-btn" @click="formatRowCase('row2', 'lower')">bc</el-button></el-tooltip>
                </div>
              </div>
            </el-form-item>
          </el-col>

          <!-- Cụm từ 1 -->
          <el-col v-if="state.inputDisplay.phrase1Select" :span="24" class="mb5">
            <el-form-item :label="$t('lz.entity.vmsMessage.content1')">
              <el-select v-model="state.ruleForm.wordList1" @change="onWordList1SelectChange" filterable multiple allow-create default-first-option clearable :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.vmsMessage.content1') })">
                <el-option v-for="item in state.listData.phraseList" :key="item.code" :label="item.content" :value="item.code" />
              </el-select>
            </el-form-item>
          </el-col>

          <!-- Cụm từ 2 -->
          <el-col v-if="state.inputDisplay.phrase2Select" :span="24" class="mb5">
            <el-form-item :label="$t('lz.entity.vmsMessage.content2')">
              <el-select v-model="state.ruleForm.wordList2" @change="onWordList2SelectChange" filterable multiple allow-create default-first-option clearable :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.vmsMessage.content2') })">
                <el-option v-for="item in state.listData.phraseList" :key="item.code" :label="item.content" :value="item.code" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>

        <!-- Thời gian (Time) -->
        <el-row :gutter="10" v-if="state.inputDisplay.timeStartPicker || state.inputDisplay.timeEndPicker" class="mb5">
          <el-col :span="12">
            <el-form-item :label="$t('lz.label.vmsMassage.startTime')">
              <el-time-picker v-model="state.ruleForm.startTime" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.label.vmsMassage.startTime') })" format="HH:mm" class="w100" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="$t('lz.label.vmsMassage.endTime')">
              <el-time-picker v-model="state.ruleForm.endTime" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.label.vmsMassage.endTime') })" format="HH:mm" class="w100" />
            </el-form-item>
          </el-col>
        </el-row>

        <!-- Font / màu / nền / cỡ chữ -->
        <el-row :gutter="10" v-if="state.inputDisplay.fontSelect || state.inputDisplay.colorSelect || state.inputDisplay.backgroundSelect">
          <el-col :span="12" class="mb5">
            <el-form-item :label="$t('lz.label.vmsMessage.textColor')">
              <el-select v-model="state.ruleForm.textColor" @change="updatePreview">
                <template #prefix><span class="vc-swatch" :style="{ background: swatchColor(state.ruleForm.textColor) }" /></template>
                <el-option v-for="color in state.listData.fontColorList" :key="color.code" :value="color.code" :label="color.name">
                  <span class="vc-swatch" :style="{ background: swatchColor(color.code) }" />{{ color.name }}
                </el-option>
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12" class="mb5">
            <el-form-item :label="$t('lz.label.vmsMessage.backgroundColor')">
              <el-select v-model="state.ruleForm.bgColor" @change="updatePreview">
                <template #prefix><span class="vc-swatch" :style="{ background: swatchColor(state.ruleForm.bgColor) }" /></template>
                <el-option v-for="color in state.listData.bgColorList" :key="color.code" :value="color.code" :label="color.name">
                  <span class="vc-swatch" :style="{ background: swatchColor(color.code) }" />{{ color.name }}
                </el-option>
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12" class="mb5">
            <el-form-item :label="$t('lz.label.vmsMessage.font')">
              <el-select v-model="state.ruleForm.selectedFont" @change="updatePreview"><el-option v-for="font in state.listData.fontList" :key="font.code" :value="font.code" :label="font.name" /></el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12" class="mb5">
            <el-form-item :label="$t('lz.label.vmsMessage.fontSize')">
              <el-select v-model="state.ruleForm.fontSize" @change="updatePreview"><el-option v-for="size in state.listData.fontSizeList" :key="size.code" :value="size.code" :label="size.name" /></el-select>
            </el-form-item>
          </el-col>
          <!-- Cảnh báo màu chữ trùng màu nền (không đọc được nội dung trên bảng) -->
          <el-col v-if="state.showWarning" :span="24" class="mb5">
            <el-alert :title="$t('lz.message.vmsControl.sameColorAlert')" type="warning" :closable="false" show-icon />
          </el-col>
        </el-row>

        <!-- Độ sáng + Độ ưu tiên -->
        <!-- Chia 12/12 giống hàng màu/font phía trên để nhãn cột phải thẳng hàng -->
        <el-row :gutter="10" v-if="state.inputDisplay.brightnessSlider || state.inputDisplay.priorityInput">
          <el-col :span="12" class="mb5">
            <el-form-item :label="$t('lz.label.vmsMessage.brightness')">
              <div class="vc-brightness">
                <el-slider v-model="state.ruleForm.brightness" :min="0" :max="100" :step="1" @input="updatePreview" />
                <span class="vc-brightness-value">{{ state.ruleForm.brightness }}%</span>
              </div>
            </el-form-item>
          </el-col>
          <el-col :span="6" class="mb5">
            <el-form-item :label="$t('lz.label.vmsMessage.priority')"><el-input-number v-model="state.ruleForm.priority" :min="1" :max="100" controls-position="right" class="w100" /></el-form-item>
          </el-col>
        </el-row>
      </div>

      <!-- ============ Chọn thiết bị: SƠ ĐỒ TUYẾN theo hướng (manual & incident dùng chung) ============
           Mỗi hướng là 1 làn đường; thiết bị đặt đúng vị trí Km từ đầu tuyến -> cuối tuyến (cấu hình route start/end).
           Bấm vào thiết bị để chọn / bỏ chọn; rê chuột để xem nội dung đang hiển thị, lập lịch, trạng thái gửi. -->
      <div class="border-vms-info vc-section mt10 route-panel" :class="mode === 'manual' ? 'manual-equipment-panel' : 'recommended-equipment-panel'">
        <div class="equipment-list-header">
          <span class="schedule-card-title">{{ mode === 'manual' ? $t('lz.label.vmsControl.equipmentList') : $t('lz.label.vmsControl.recommendedList') }}</span>
          <span class="vc-count" :class="{ 'is-active': selectedCount > 0 }">{{ selectedCount }}/{{ totalEquipmentCount }}</span>
        </div>

        <!-- mode incident: phạm vi gợi ý (mở đầu = small, kéo slider mở rộng tới large) + "Toàn tuyến" -->
        <div v-if="mode !== 'manual'" class="recommended-toolbar">
          <span>{{ $t('lz.label.vmsControl.suggestionRange') }}: {{ (state.expandRange / 1000).toFixed(1) }} km</span>
          <el-slider
            v-if="hasExpandRange"
            v-model="state.expandRange"
            :min="rangeConfig.small"
            :max="rangeConfig.large"
            :step="500"
            :format-tooltip="formatRangeTooltip"
            style="width: 180px"
            @change="reloadSuggestion"
          />
          <el-checkbox v-model="state.isWholeRoute" @change="reloadSuggestion">{{ $t('lz.label.vmsControl.wholeRoute') }}</el-checkbox>
        </div>

        <div v-if="mode !== 'manual' && state.isProcessing" class="route-loading" v-loading="true" :element-loading-text="$t('lz.label.base.loading')" element-loading-background="transparent" />
        <div v-else-if="routeView.lanes.length" class="route-map">
          <!-- Trục lý trình: đầu tuyến (trái) -> cuối tuyến (phải) -->
          <div class="route-axis">
            <span v-for="tick in routeView.ticks" :key="tick.pct" class="route-tick" :class="{ 'is-end': tick.isEnd }" :style="{ left: routeLeft(tick.pct) }">{{ tick.label }}</span>
          </div>

          <div v-for="(lane, laneIndex) in routeView.lanes" :key="lane.name" class="route-lane" :style="{ animationDelay: laneIndex * 0.06 + 's' }">
            <div class="route-lane-head">
              <el-checkbox
                :model-value="lane.selectedCount === lane.items.length"
                :indeterminate="lane.selectedCount > 0 && lane.selectedCount < lane.items.length"
                @change="toggleLane(lane)"
              >
                <span class="direction-title-text">{{ lane.name }}</span>
              </el-checkbox>
              <span class="direction-badge">{{ lane.selectedCount }}/{{ lane.items.length }}</span>
            </div>

            <div class="route-track" :style="{ height: laneHeight(lane) }">
              <div class="route-road" />
              <el-tooltip v-for="m in lane.items" :key="m.item.id" placement="top" :show-after="120" :hide-after="0" :offset="10" popper-class="vc-route-tip">
                <template #content>
                  <div class="route-tip">
                    <div class="route-tip-head">
                      <span class="route-tip-name">{{ m.item.name }}</span>
                      <span class="route-tip-km">{{ formatKm(m.pos) }}</span>
                    </div>
                    <div class="route-tip-sub">{{ lane.name }}</div>
                    <template v-if="currentImageOf(m.item.id)">
                      <div class="route-tip-label">{{ $t('lz.label.vmsControl.currentContent') }}</div>
                      <img :src="currentImageOf(m.item.id)" :alt="m.item.name" class="route-tip-img" />
                    </template>
                    <div v-if="isRunning(m.item.id)" class="route-tip-row is-running">
                      <el-icon class="is-loading"><Loading /></el-icon>{{ $t('lz.label.vmsProcess.scheduleRunning') }}
                    </div>
                    <div v-if="sendStatusOf(m.item.id)" class="route-tip-row" :class="'is-' + sendStatusOf(m.item.id)">{{ sendStatusLabel(m.item.id) }}</div>
                    <div class="route-tip-hint">
                      {{ isEquipmentSelected(m.item.id) ? $t('lz.label.vmsControl.selected') : $t('lz.label.vmsControl.notSelected') }} · {{ $t('lz.label.vmsControl.clickToToggle') }}
                    </div>
                  </div>
                </template>
                <button
                  type="button"
                  class="route-marker"
                  :class="markerClass(m.item.id)"
                  :style="{ left: routeLeft(m.pct), '--tier': m.tier }"
                  :aria-pressed="isEquipmentSelected(m.item.id)"
                  :aria-label="m.item.name"
                  @click="toggleEquipment(m.item.id)"
                >
                  <span class="route-chip">
                    <el-icon v-if="sendStatusOf(m.item.id) === 'pending'" class="is-loading"><Loading /></el-icon>
                    <el-icon v-else-if="sendStatusOf(m.item.id) === 'success'"><CircleCheckFilled /></el-icon>
                    <el-icon v-else-if="sendStatusOf(m.item.id) === 'fail'"><CircleCloseFilled /></el-icon>
                    {{ m.item.name }}
                  </span>
                  <span class="route-stem" />
                  <span class="route-pin" />
                </button>
              </el-tooltip>
            </div>
          </div>
        </div>
        <p v-else class="route-empty">{{ $t('lz.label.base.noData') }}</p>
      </div>
    </el-form>

    <!-- Modal độ ưu tiên (gom vào component dùng chung) -->
    <VmsPriorityModal ref="priorityModalRef" @handle-confirm="handlePriorityConfirm" />
    <!-- Modal phân xử điều khiển sự cố (Append / Override visual strike-through) -->
    <VmsIncidentModal ref="incidentModalRef" @handle-confirm="handleIncidentConfirm" />
  </div>
</template>


<script lang="ts" setup name="vmsControl">
import { ref, reactive, onMounted, nextTick, computed, watch } from 'vue';
import { getAPI, axiosInstance, serveConfig } from '/@/utils/axios-utils';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Loading, Sunny, CircleCheckFilled, CircleCloseFilled } from '@element-plus/icons-vue';
import { useTransporterVms, getVmsEquipmentId } from '/@/transporter/hooks/useTransporterVms';
import { Local } from '/@/utils/storage';
import { useI18n } from 'vue-i18n';
import draggable from 'vuedraggable';

import { TmsEquipmentApi, TmsEquipmentTypeApi, TmsWorkExecuteApi } from '/@/api-services/tms';
import { VmsMessageApi, VmsImageApi, VmsTemplateApi, VmsProcessApi, VmsCurrentApi, VmsControlApi } from '/@/api-services/vms';
import { AddControlInput, ControlScheduleInput, ControlEquipmentInput, ProcessStatusOutput, InformationProcessOutput } from '/@/api-services/vms';
import { SysConfigDataApi, SysTerminologyApi } from '/@/api-services/cfgSystem';
import { VmsActionType, VmsDeviceType, VmsImageType, VmsMessageForIncidentEnum } from '/@/types/enums/vmsEnum';
import { TmsEquipmentType, TmsIncidentConfigCodeEnum } from '/@/types/enums/tmsEnum';
import { BaseConfigTypeEnum, SysConfigTypeCodeEnum } from '/@/types/enums/baseEnum';
import { TmsWorkExecuteStateEnum } from '/@/types/enums';
import { useMapStore } from '/@/stores/its/mapStore';
import { useBaseConfig } from '/@/stores/baseConfig';
import VmsPriorityModal from './vmsPriority.vue';
import VmsIncidentModal from './vmsIncidentModal.vue';

// Logic xử lý được tách sang composables/ (state + data + submit vẫn giữ ở đây)
import { useVmsManual } from './composables/useVmsManual';
import { useVmsControlDisplay } from './composables/useVmsControlDisplay';
import { useVmsContent } from './composables/useVmsContent';
import { useVmsPreview } from './composables/useVmsPreview';
import { useVmsIncident } from './composables/useVmsIncident';
import { useVmsValidation } from './composables/useVmsValidation';
// Đọc cấu hình vận hành (SysOpConfig) an toàn: phải await ensureOpConfigLoaded trước khi đọc
import { ensureOpConfigLoaded, readOpConfigInt, readOpConfigIntOrNull } from './composables/useOpConfig';

/**
 * ============================================================================
 *  BẢN ĐỒ LUỒNG (đọc theo thứ tự này để hiểu component)
 * ============================================================================
 *  ĐIỂM VÀO:
 *   - onMounted()      : tự chạy khi mount → ensureDefaultData() (tải dữ liệu nền) + directionList
 *   - open(ctx)        : CHA gọi khi mở form → khởi tạo theo mode
 *   - submit()         : CHA gọi khi bấm "Xác nhận" (nút nằm ở cha)
 *
 *  PHÂN BỔ CODE:
 *   - index.vue          : state + load data (API) + submit + open/onMounted + wiring
 *   - useVmsManual       : group thiết bị theo hướng, bind select, đổ lại tiến trình (manual)
 *   - useVmsIncident     : group gợi ý theo hướng, câu mặc định (incident)
 *   - useVmsControlDisplay: ẩn/hiện input theo loại, đổi loại, chọn message/phrase
 *   - useVmsContent      : định dạng chữ, ảnh, template, schedule list
 *   - useVmsPreview      : preview qua API (base64) + watch/debounce
 * ============================================================================
 */

//============ #SECTION: KHAI BÁO CHUNG (props / emits / helper / ref) =============================
const { t: $t, locale } = useI18n();
const mapStore = useMapStore();
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
// Cấu hình vận hành (SysOpConfig) đọc qua helper useOpConfig, không lấy trực tiếp từ store nữa
// vì store trả rỗng ở lần đọc đầu (xem giải thích trong useOpConfig.ts).

// mode là prop; ngữ cảnh (incident / itemId) truyền qua open(ctx)
// deviceType: phân loại thiết bị điều khiển (VMS mặc định | LCS). LCS chỉ hỗ trợ điều khiển bằng hình ảnh.
interface Props {
	mode: 'manual' | 'incident';
	deviceType?: VmsDeviceType;
}
const props = withDefaults(defineProps<Props>(), { deviceType: VmsDeviceType.Vms });

interface Emits {
	(e: 'submitSuccess'): void; // cha lắng nghe để refresh + đóng
	(e: 'validityChange', valid: boolean): void; // cha lắng nghe để disable nút Xác nhận khi dữ liệu vi phạm
}
const emit = defineEmits<Emits>();

const ruleFormRef = ref();
const priorityModalRef = ref<InstanceType<typeof VmsPriorityModal>>();
const incidentModalRef = ref<InstanceType<typeof VmsIncidentModal>>();
const selectedTemplate = ref<any>(null);
const activeVmsActionType = ref(VmsActionType.Text);

//============ #SECTION: STATE =============================
const state = reactive<any>({
	isProcessing: false,

	// ===== ngữ cảnh sự cố (incident) =====
	incidentId: null,
	incidentInfo: null,
	eventTypeCode: null,
	vmsMessageDefaultList: [],
	recommendedEquipmentList: [],
	selectedRecommendedEquipmentList: [],
	expandRange: 0, // Phạm vi gợi ý user chọn (mét). Mặc định = small; kéo tối đa = large.
	isWholeRoute: false, // Toàn tuyến (dùng vị trí đầu/cuối tuyến) - chỉ thêm ứng viên để user tự tick

	// ===== ngữ cảnh thủ công (manual) =====
	itemId: '' as any,
	listVmsSelected: [] as any,
	vmsCurrent: {} as any,
	isLoading: false,
	vmsWordTemplateAll: [] as any,
	vmsTextTemplateAll: [] as any,

	// ===== chung =====
	directionList: [],
	equipmentList: [],
	selectedMessage: null as any,
	vmsData: {} as any,
	addControlProcess: {} as AddControlInput,
	scheduleImageList: [] as any,
	previewTemplateJson: '' as string,
	isRunningSchedule: false,
	isLoadingSchedule: false,
	listVmsRunningSchedule: [] as Array<ProcessStatusOutput>,
	listInformationProcess: {} as Array<InformationProcessOutput>,
	showWarning: false,
	// showCurrent: đang hiện ảnh nội dung hiện tại của thiết bị (VmsCurrent.Url) thay cho ảnh render từ form
	preview: { src: '' as string, loading: false, error: '' as string, showCurrent: false },
	inputDisplay: {
		content1Input: false,
		content2Input: false,
		phrase1Select: false,
		phrase2Select: false,
		templateSelect: false,
		imageSelect: false,
		messageSelect: false,
		fontSelect: false,
		fontSizeSelect: false,
		colorSelect: false,
		backgroundSelect: false,
		brightnessSlider: true,
		priorityInput: true,
		preview: false,
		previewImage: false,
		previewSchedule: false,
		timeStartPicker: false,
		timeEndPicker: false,
	},
	listData: {
		controlTypeList: [] as any,
		messageList: [] as any,
		phraseList: [] as any,
		msgTemplateAll: [] as any,
		imageList: [] as any,
		templateList: [] as any,
		fontList: [] as any,
		fontSizeList: [] as any,
		fontColorList: [] as any,
		bgColorList: [] as any,
		currentList: [] as any,
		equipmentListGroupByDirection: [] as any, // mode manual: group theo hướng
	},
	ruleForm: {
		controlType: VmsActionType.Text as any,
		wordList1: [] as any,
		wordList2: [] as any,
		imgTemplate: '',
		row1: '',
		row2: '',
		row3: '',
		selectedFont: 'Arial',
		fontSize: 14,
		textColor: 'White',
		bgColor: 'Black',
		brightness: 100,
		priority: 0,
		playMode: 'override',
		timeToLive: 30,
		startTime: '',
		endTime: '',
		urlImage: '' as any,
		// dùng cho nghiệp vụ sự cố
		workExecuteId: null as any,
		workExecuteRemark: null as any,
	} as any,
});

// Danh sách sự cố khác đang chiếm giữ các thiết bị được chọn
const occupiedIncidents = ref<any[]>([]);
const isDeviceUnderIncident = computed(() => occupiedIncidents.value.length > 0);

const checkOccupiedIncidents = async (selectedIds: string[]) => {
	if (!selectedIds || selectedIds.length === 0) {
		occupiedIncidents.value = [];
		return;
	}
	try {
		const res = await axiosInstance.post(`${serveConfig.basePath}/api/tms/TmsIncidentControl/GetByEquipment`, {
			equipmentIds: selectedIds,
			excludeIncidentId: state.incidentId || '',
			// Chỉ tính sự cố còn đang xử lý; bản ghi sót của sự cố đã kết thúc không được bật hộp chọn Ghi đè/Nối tiếp
			onlyActiveIncident: true,
		});
		occupiedIncidents.value = res.data?.result || [];
	} catch {
		occupiedIncidents.value = [];
	}
};

watch(
	() => (props.mode === 'manual' ? state.listVmsSelected : state.selectedRecommendedEquipmentList),
	(newIds) => {
		checkOccupiedIncidents(newIds || []);
	},
	{ deep: true }
);

//============ #SECTION: WIRING COMPOSABLES =============================
// deps dùng chung truyền xuống composable (state là nguồn duy nhất).
const deps = { state, props, $t, locale, mapStore, getConfigDataByCode, selectedTemplate } as any;

// Kiểm tra thiết bị đang chạy lập lịch (giữ ở index vì dùng ở nhiều nơi + template).
const isRunning = (id: any) => {
	return Array.isArray(state.listVmsRunningSchedule) && state.listVmsRunningSchedule.some((process: ProcessStatusOutput) => process.vmsId === id && process.status);
};

// URL ảnh preview cho loại Image: base64 (data:) dùng trực tiếp, còn lại nối baseUrl (ảnh thư viện là path tương đối).
const previewImageUrl = computed(() => {
	const url = state.ruleForm.urlImage;
	if (!url) return '';
	return String(url).startsWith('data:') ? url : `${mapStore.baseUrl}/${url}`;
});

// Khởi tạo composable theo thứ tự phụ thuộc: manual → display → content → preview → incident.
const { buildEquipmentGroupByDirection, bindVmsDataToSelect, displayInformationProcess } = useVmsManual({ ...deps, isRunning });
const { onMessageSelectChange, onWordList1SelectChange, onWordList2SelectChange, toggleControlDisplay, resetControlToDefault, onVmsActionlTypeChange } = useVmsControlDisplay({
	...deps,
	bindVmsDataToSelect,
	displayInformationProcess,
});

// Khởi tạo composable content (định dạng chữ, ảnh, template, schedule list) + preview (base64).
const {
	updatePreview,
	formatRowCase,
	formatTemplateElementCase,
	handleImageUpload,
	beforeUpload,
	updateSelectedImageUrl,
	updateSelectedTemplateUrl,
	syncTemplateJson,
	textElements,
	handleImageTemplateSelectChange,
	handleTimerChange,
	handleSheduleChange,
	removeAt,
	onMove,
	moveItem,
} = useVmsContent(deps);
const { schedulePreview, fetchPreview  } = useVmsPreview(deps);
const { recommendedEquipmentGroups, setDefaultVmsMessage, applyEventDefaultContent } = useVmsIncident(deps);

//============ #SECTION: HIỂN THỊ (chỉ phục vụ giao diện, không ảnh hưởng dữ liệu gửi) =============================
// Mã màu cấu hình là tên màu CSS (White/Black/Red...) => dùng trực tiếp làm chấm màu; mã lạ thì trình duyệt bỏ qua.
const swatchColor = (code: any) => (code ? String(code).trim() : 'transparent');

// Chế độ xem khung nội dung: 'pixel' = mô phỏng điểm LED phóng to, 'image' = ảnh gốc kích thước thật như trước.
// Chỉ ảnh hưởng hiển thị; nhớ theo trình duyệt của người dùng.
type PreviewMode = 'pixel' | 'image';
const PREVIEW_MODE_KEY = 'vmsControlPreviewMode';
const readPreviewMode = (): PreviewMode => {
	try {
		return Local.get(PREVIEW_MODE_KEY) === 'image' ? 'image' : 'pixel';
	} catch {
		return 'pixel';
	}
};
const previewMode = ref<PreviewMode>(readPreviewMode());
const setPreviewMode = (mode: PreviewMode) => {
	previewMode.value = mode;
	try {
		Local.set(PREVIEW_MODE_KEY, mode);
	} catch {
		// Trình duyệt chặn localStorage: vẫn đổi chế độ trong phiên hiện tại
	}
};

// Ảnh thư viện không qua API render => mô phỏng độ sáng bằng CSS (giữ tối thiểu 35% để vẫn nhìn được nội dung).
const previewImageFilter = computed(() => `brightness(${0.35 + (0.65 * (state.ruleForm.brightness ?? 100)) / 100})`);

// Số thiết bị đã chọn / tổng số thiết bị đang hiển thị (cha dùng selectedCount cho nút Xác nhận).
const selectedCount = computed(() => ((props.mode === 'manual' ? state.listVmsSelected : state.selectedRecommendedEquipmentList) ?? []).length);
//============ #SECTION: TRẠNG THÁI GỬI THEO TỪNG THIẾT BỊ =============================
// Khi bấm Xác nhận: thiết bị được chọn chuyển 'pending', rồi 'success' / 'fail' theo socket vms-success / vms-fail
// mà BE phát cho TỪNG thiết bị sau khi gửi (ta.its.data.vms). Chỉ nhận socket của thiết bị đang chờ trong lần gửi này.
type SendStatus = 'pending' | 'success' | 'fail';
const sendStatus = reactive<Record<string, SendStatus>>({});
const SEND_SETTLE_MS = 1500; // chờ thêm socket đến muộn sau khi API đã trả về
const SEND_SHOW_MS = 700; // giữ dấu ✓ để người dùng kịp thấy trước khi form đóng

useTransporterVms((payload) => {
	const id = getVmsEquipmentId(payload.data);
	if (!id || sendStatus[id] !== 'pending') return;
	if (payload.actionType === 'vms-success') sendStatus[id] = 'success';
	else if (payload.actionType === 'vms-fail') sendStatus[id] = 'fail';
});

const clearSendStatus = () => Object.keys(sendStatus).forEach((key) => delete sendStatus[key]);
const sendStatusOf = (id: any): SendStatus | undefined => sendStatus[String(id)];
const sendStatusLabel = (id: any) => {
	const s = sendStatusOf(id);
	if (s === 'pending') return $t('lz.label.vmsControl.sending');
	if (s === 'success') return $t('lz.label.vmsControl.sendSuccess');
	if (s === 'fail') return $t('lz.label.vmsControl.sendFail');
	return '';
};

// Đợi các thiết bị còn 'pending' nhận socket, tối đa SEND_SETTLE_MS.
const waitSendSettled = async (ids: string[]) => {
	const deadline = Date.now() + SEND_SETTLE_MS;
	while (Date.now() < deadline && ids.some((id) => sendStatus[id] === 'pending')) {
		await new Promise((r) => setTimeout(r, 100));
	}
};

const equipmentNameOf = (id: string) => {
	const all: any[] = [
		...(state.equipmentList ?? []),
		...(state.recommendedEquipmentList ?? []),
		...Object.values(state.listData.equipmentListGroupByDirection ?? {}).flat(),
	];
	return all.find((e: any) => String(e?.id) === id)?.name ?? id;
};

const totalEquipmentCount = computed(() => {
	const groups = props.mode === 'manual' ? state.listData.equipmentListGroupByDirection : recommendedEquipmentGroups.value;
	return Object.values(groups ?? {}).reduce((sum: number, list: any) => sum + (Array.isArray(list) ? list.length : 0), 0);
});

//============ #SECTION: SƠ ĐỒ TUYẾN (chọn thiết bị theo vị trí Km trên từng hướng) =============================
// Danh sách thiết bị chọn theo mode: manual -> listVmsSelected, incident -> selectedRecommendedEquipmentList.
const selectedIdList = (): any[] => (props.mode === 'manual' ? state.listVmsSelected : state.selectedRecommendedEquipmentList) ?? [];
const setSelectedIdList = (ids: any[]) => {
	if (props.mode === 'manual') state.listVmsSelected = ids;
	else state.selectedRecommendedEquipmentList = ids;
};
const isEquipmentSelected = (id: any) => selectedIdList().includes(id);
const toggleEquipment = (id: any) => {
	const current = selectedIdList();
	setSelectedIdList(current.includes(id) ? current.filter((x: any) => x !== id) : [...current, id]);
};
// Chọn cả hướng: đã chọn đủ thì bỏ hết, ngược lại chọn thêm toàn bộ thiết bị của hướng.
const toggleLane = (lane: any) => {
	const laneIds = lane.items.map((m: any) => m.item.id);
	const current = selectedIdList();
	const allSelected = laneIds.every((id: any) => current.includes(id));
	setSelectedIdList(allSelected ? current.filter((id: any) => !laneIds.includes(id)) : Array.from(new Set([...current, ...laneIds])));
};

const ROUTE_TIER_GAP_PCT = 12; // khoảng cách tối thiểu (% chiều dài tuyến) để 2 nhãn cùng tầng không chồng nhau
const ROUTE_MAX_TIERS = 3; // tối đa 3 tầng nhãn mỗi làn, quá thì dùng lại tầng trống sớm nhất
const ROUTE_TIER_HEIGHT = 24; // px mỗi tầng nhãn
const ROUTE_EDGE_INSET = 36; // px chừa 2 đầu để nhãn ở đầu/cuối tuyến không bị cắt

const positionOf = (e: any) => (Number(e?.kmNumber) || 0) * 1000 + (Number(e?.metNumber) || 0);
const formatKm = (p: number) => `Km${Math.floor(p / 1000)}+${Math.round(p % 1000)}`;
const routeLeft = (pct: number) => `calc(${ROUTE_EDGE_INSET}px + (100% - ${ROUTE_EDGE_INSET * 2}px) * ${pct / 100})`;
const laneHeight = (lane: any) => `${24 + lane.tiers * ROUTE_TIER_HEIGHT}px`;

// Dựng sơ đồ: trục từ đầu tuyến -> cuối tuyến (cấu hình vị trí đầu/cuối), chưa cấu hình thì lấy theo
// thiết bị xa nhất 2 phía. Mỗi hướng là 1 làn; nhãn thiết bị gần nhau được xếp lên tầng khác để không chồng.
const routeView = computed(() => {
	const groups: Record<string, any[]> = (props.mode === 'manual' ? state.listData.equipmentListGroupByDirection : recommendedEquipmentGroups.value) ?? {};
	const entries = Object.entries(groups).filter(([, list]) => Array.isArray(list) && list.length > 0);
	const positions = entries.flatMap(([, list]) => list.map(positionOf));
	if (!positions.length) return { lanes: [] as any[], ticks: [] as any[] };

	const cfgStart = readOpConfigIntOrNull(TmsIncidentConfigCodeEnum.RouteStartPosition);
	const cfgEnd = readOpConfigIntOrNull(TmsIncidentConfigCodeEnum.RouteEndPosition);
	let start: number;
	let end: number;
	if (cfgStart != null && cfgEnd != null && cfgStart !== cfgEnd) {
		start = cfgStart;
		end = cfgEnd;
	} else {
		const lo = Math.min(...positions);
		const hi = Math.max(...positions);
		const pad = Math.max((hi - lo) * 0.05, 500);
		start = Math.max(0, lo - pad);
		end = hi + pad;
	}
	const toPct = (p: number) => Math.min(100, Math.max(0, ((p - start) / (end - start)) * 100));

	const selected = new Set(selectedIdList());
	const lanes = entries.map(([name, list]) => {
		const tierLastPct: number[] = [];
		const items = list
			.map((item: any) => ({ item, pos: positionOf(item), pct: toPct(positionOf(item)) }))
			.sort((a: any, b: any) => a.pct - b.pct)
			.map((m: any) => {
				let tier = tierLastPct.findIndex((last) => m.pct - last >= ROUTE_TIER_GAP_PCT);
				if (tier === -1) tier = tierLastPct.length < ROUTE_MAX_TIERS ? tierLastPct.length : tierLastPct.indexOf(Math.min(...tierLastPct));
				tierLastPct[tier] = m.pct;
				return { ...m, tier };
			})
			// Vẽ tầng cao trước, tầng thấp sau: nhãn tầng dưới nằm ĐÈ lên cọc của tầng trên (cùng vị trí Km)
			.sort((a: any, b: any) => b.tier - a.tier);
		return { name, items, tiers: Math.max(1, tierLastPct.length), selectedCount: items.filter((m: any) => selected.has(m.item.id)).length };
	});

	const ticks = [0, 25, 50, 75, 100].map((t) => ({ pct: t, label: formatKm(Math.round(start + ((end - start) * t) / 100)), isEnd: t === 0 || t === 100 }));
	return { lanes, ticks };
});

const markerClass = (id: any) => {
	const status = sendStatusOf(id);
	return { 'is-selected': isEquipmentSelected(id), 'is-running': isRunning(id), ['is-send-' + status]: !!status };
};

// Ảnh nội dung đang hiển thị trên biển (VmsCurrent) cho phần chi tiết khi rê chuột.
const currentImageOf = (id: any) => {
	const url = state.listData.currentList?.find((x: any) => x.equipmentId == id)?.url;
	return url ? `${mapStore.baseUrl}/${url}` : '';
};

// Ảnh nội dung hiện tại của thiết bị đang mở (chỉ mode manual), hiển thị ở khung preview khi vừa mở form.
// Dùng Url thay vì RowData: mọi loại điều khiển gửi thành công đều ghi Url (ảnh thật trên biển), còn RowData
// chỉ có khi gửi dạng chữ (Image/Template/Lập lịch/Hẹn giờ -> RowData null => render ra ảnh đen).
const currentContentUrl = computed(() => (props.mode === 'manual' && state.preview.showCurrent ? currentImageOf(state.itemId) : ''));

// Người dùng thay đổi bất kỳ nội dung / loại điều khiển nào -> bỏ ảnh hiện tại, quay về preview render từ form.
watch(
	() => [
		state.ruleForm.controlType,
		state.ruleForm.row1,
		state.ruleForm.row2,
		state.ruleForm.textColor,
		state.ruleForm.bgColor,
		state.ruleForm.selectedFont,
		state.ruleForm.fontSize,
		state.ruleForm.brightness,
		state.ruleForm.urlImage,
		state.ruleForm.imgTemplate,
		state.ruleForm.wordList1,
		state.ruleForm.wordList2,
		state.selectedMessage,
		state.previewTemplateJson,
	],
	() => {
		state.preview.showCurrent = false;
	}
);

// Ràng buộc dữ liệu trước khi gửi ra thiết bị (độ dài, màu trùng, cỡ chữ). contentMaxLength dùng cho :maxlength ở template.
const { contentMaxLength, isContentValid, validateContentBeforeSubmit } = useVmsValidation(deps);

// Phát cờ hợp lệ cho component cha để disable nút Xác nhận khi dữ liệu vi phạm ràng buộc.
watch(isContentValid, (valid) => emit('validityChange', valid), { immediate: true });

//============ #SECTION: LOAD DATA (giữ ở index) =============================

// Nạp toàn bộ dữ liệu cho điều khiển: loại điều khiển, terminology câu mặc định, màu/font/cỡ chữ,
// message/phrase, thiết bị VMS, ảnh, template, danh sách hiện tại (current).
const loadDefaultData = async () => {
	// Màu nền / màu chữ / cỡ chữ / font lấy từ config (đồng bộ, có ngay)
	state.listData.bgColorList = getConfigDataByCode(BaseConfigTypeEnum.VmsBackgroundColor);
	state.listData.fontColorList = getConfigDataByCode(BaseConfigTypeEnum.VmsFontColor);
	state.listData.fontSizeList = getConfigDataByCode(BaseConfigTypeEnum.VmsFontSize);
	state.listData.fontList = getConfigDataByCode(BaseConfigTypeEnum.VmsFontName);

	// ===== ƯU TIÊN 1: DANH SÁCH THIẾT BỊ (để phần chọn thiết bị hiện sớm nhất) =====
	const equipmentType = props.deviceType === VmsDeviceType.Lcs ? TmsEquipmentType.LCS : TmsEquipmentType.Vms;
	let response = await getAPI(TmsEquipmentApi).apiTmsTmsequipmentGetequipmenttypebyequipmentidVmstypeGet(equipmentType);
	state.equipmentList = response.data?.result ?? [];
	// Build nhóm theo hướng NGAY -> danh sách thiết bị (mode manual) render liền, không chờ các API còn lại
	if (props.mode === 'manual') buildEquipmentGroupByDirection();

	// ===== ƯU TIÊN 2: THÔNG TIN ĐIỀU KHIỂN (loại điều khiển) =====
	response = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet(BaseConfigTypeEnum.VmsActionType);
	state.listData.controlTypeList = response.data.result;
	// LCS chỉ hỗ trợ điều khiển bằng hình ảnh -> chỉ giữ loại Image
	if (props.deviceType === VmsDeviceType.Lcs) {
		state.listData.controlTypeList = (state.listData.controlTypeList ?? []).filter((item: any) => item.code?.trim() === VmsActionType.Image);
	}

	// (Nội dung hiện tại KHÔNG nạp ở đây vì hàm này chỉ chạy 1 lần -> xem loadCurrentList, gọi mỗi lần open)

	// ===== ƯU TIÊN 4: MESSAGE / PHRASE =====
	response = await getAPI(VmsMessageApi).apiVmsVmsmessageListPost({ lang: locale.value, sortKey: 'orderNo', sortOrder: 'desc' });
	state.listData.msgTemplateAll = response.data.result;
	state.listData.messageList = state.listData.msgTemplateAll.filter((item: any) => item.type == '1');
	state.listData.phraseList = state.listData.msgTemplateAll.filter((item: any) => item.type == '0');

	// ===== ƯU TIÊN 5: TERMINOLOGY CÂU MẶC ĐỊNH (nghiệp vụ sự cố) =====
	response = await getAPI(SysTerminologyApi).apiCfgsystemSysterminologyListbyparaPost({
		language: locale.value,
		codeList: [
			VmsMessageForIncidentEnum.VMS_INCIDENT_MESSAGE_ROW1, VmsMessageForIncidentEnum.VMS_INCIDENT_MESSAGE_ROW2,
			VmsMessageForIncidentEnum.VMS_ACCIDENT_MESSAGE_ROW1, VmsMessageForIncidentEnum.VMS_ACCIDENT_MESSAGE_ROW2,
			VmsMessageForIncidentEnum.VMS_FA_MESSAGE_ROW1, VmsMessageForIncidentEnum.VMS_FA_MESSAGE_ROW2,
			VmsMessageForIncidentEnum.VMS_FIRE_MESSAGE_ROW1, VmsMessageForIncidentEnum.VMS_FIRE_MESSAGE_ROW2,
			VmsMessageForIncidentEnum.VMS_TRAFFIC_JAM_MESSAGE_ROW1, VmsMessageForIncidentEnum.VMS_TRAFFIC_JAM_MESSAGE_ROW2,
		],
	});
	state.vmsMessageDefaultList = response.data?.result;

	// ===== CUỐI CÙNG: DỮ LIỆU PHỤC VỤ PREVIEW ẢNH (ảnh thư viện + template) =====
	// Lấy id loại thiết bị (VMS/LCS) từ code -> chỉ nạp ảnh thuộc đúng loại thiết bị đang điều khiển
	const equipTypeRes = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet('', equipmentType);
	const equipmentTypeId = equipTypeRes.data?.result?.length ? equipTypeRes.data.result[0].id : undefined;
	response = await getAPI(VmsImageApi).apiVmsVmsimageListPost({ type: VmsImageType.Image, equipmentTypeId });
	state.listData.imageList = response.data.result ?? [];
	response = await getAPI(VmsTemplateApi).apiVmsVmstemplateListGet();
	state.listData.templateList = response.data.result ?? [];
};

// Đảm bảo loadDefaultData chỉ chạy 1 lần và mọi nơi cùng chờ chung 1 promise.
// Tránh race: open() có thể chạy khi onMounted mới set controlTypeList nhưng chưa set equipmentList.
// Lỗi thì bỏ promise đã cache để lần mở sau nạp lại, tránh form hỏng vĩnh viễn tới khi F5.
let defaultDataPromise: Promise<void> | null = null;
const ensureDefaultData = () => {
	if (!defaultDataPromise) {
		defaultDataPromise = loadDefaultData().catch((error) => {
			defaultDataPromise = null;
			throw error;
		});
	}
	return defaultDataPromise;
};

// Nội dung hiện tại của tất cả thiết bị (đổ nội dung hiện tại / mặc định + check độ ưu tiên).
// Nạp lại MỖI LẦN open: component được giữ mount giữa các lần mở panel nên dữ liệu cũ sẽ lệch.
const loadCurrentList = async () => {
	const response = await getAPI(VmsCurrentApi).apiVmsVmscurrentListGet();
	state.listData.currentList = response.data.result ?? [];
};

// Kiểm tra tiến trình lập lịch đang chạy của các VMS.
const loadListVmsRunning = async () => {
	try {
		const res = await getAPI(VmsProcessApi).apiVmsVmsprocessCheckvmsrunningscheduleGet();
		state.listVmsRunningSchedule = res.data.result as Array<ProcessStatusOutput>;
		// mode manual: cập nhật trạng thái chạy của thiết bị đang mở
		if (props.mode === 'manual' && state.itemId) state.isRunningSchedule = isRunning(state.itemId);
	} catch (error) {
		console.error(error);
	}
};

// Đổ lại nội dung thiết lập trước đó + thông tin tiến trình đang chạy (mode manual).
const loadProcessData = async () => {
	// Đổ lại NỘI DUNG HIỆN TẠI của thiết bị lên form/preview theo loại điều khiển đang mở.
	if (state.vmsCurrent) {
		const controlType = state.ruleForm.controlType;

		if (
			controlType == VmsActionType.Text ||
			controlType == VmsActionType.Message ||
			controlType == VmsActionType.Phrase
		) {
			// LƯU Ý: JSON rowData từ BE có thể là PascalCase (Row1/Row2) hoặc camelCase (row1/row2)
			// -> đọc chịu được cả hai kiểu, nếu chỉ đọc chữ thường sẽ rỗng và preview không hiện nội dung hiện tại.
			let rowData: any = {};
			try {
				rowData = JSON.parse(state.vmsCurrent.rowData) || {};
			} catch {
				rowData = {};
			}
			state.ruleForm.row1 = rowData.Row1 ?? rowData.row1 ?? '';
			state.ruleForm.row2 = rowData.Row2 ?? rowData.row2 ?? '';
			state.ruleForm.row3 = rowData.Row3 ?? rowData.row3 ?? '';
		} else if (controlType == VmsActionType.Image) {
			// LCS và VMS loại ảnh: đổ ảnh hiện tại (url) lên preview khi mở form.
			if (state.vmsCurrent.url) state.ruleForm.urlImage = state.vmsCurrent.url;
		}
	}

	// Lấy thông tin tiến trình để đổ lại theo từng loại điều khiển
	try {
		await loadListVmsRunning();
		const responseInfo = await getAPI(VmsProcessApi).apiVmsVmsprocessGetinformationvmsGet();
		state.listInformationProcess = responseInfo?.data?.result as Array<InformationProcessOutput>;
	} catch (error) {
		console.error(error);
	}
};

// Phạm vi gợi ý mặc định (mét) khi SysOpConfig chưa cấu hình.
// PHẢI trùng với EquipmentSuggestionConst.DefaultSmallRangeMeters ở backend, nếu lệch thì luồng
// thủ công và luồng tự động sẽ chọn ra hai tập thiết bị khác nhau.
const DEFAULT_SMALL_RANGE = 4000;

// Đọc cấu hình phạm vi small/large (mét) từ SysOpConfig.
//  - small: phạm vi TÍCH SẴN.
//  - large: phạm vi GỢI Ý thêm (hiển thị, không tích). Chưa cấu hình -> 0 tức không có phạm vi mở rộng.
// LƯU Ý: chỉ đúng sau khi đã await ensureOpConfigLoaded() (xem useOpConfig.ts).
const readRangeConfig = () => {
	const small = readOpConfigInt(TmsIncidentConfigCodeEnum.EquipmentSuggestionSmallRange, DEFAULT_SMALL_RANGE);
	const large = readOpConfigInt(TmsIncidentConfigCodeEnum.EquipmentSuggestionLargeRange, 0, false);
	return { small, large };
};

// Bound cho slider chọn phạm vi (min=small, max=large).
const rangeConfig = computed(() => readRangeConfig());

// Có phạm vi mở rộng để kéo slider hay không (large phải lớn hơn small mới có ý nghĩa).
const hasExpandRange = computed(() => rangeConfig.value.large > rangeConfig.value.small);

// Hiển thị tooltip slider theo km.
const formatRangeTooltip = (v: number) => `${(v / 1000).toFixed(1)} km`;

// Nạp danh sách thiết bị gợi ý theo sự cố qua API ListEquipmentSuggestion.
// - Lần mở đầu: ứng viên = phạm vi small (bằng đúng phạm vi tích sẵn).
//   Kéo slider -> ứng viên mở rộng tới giá trị slider (tối đa large); "Toàn tuyến" -> thêm theo vị trí đầu/cuối tuyến.
// - Tick tự động (isRecommended từ BE) luôn chỉ theo small, nên phần mở rộng chỉ hiện ra để user tự tick.
// - preserveSelection=false (lần mở đầu): tick theo đúng isRecommended.
//   preserveSelection=true (kéo slider / bật Toàn tuyến): GIỮ NGUYÊN lựa chọn user, chỉ lọc bỏ id không còn trong danh sách.
const loadRecommendedEquipmentList = async (preserveSelection = false) => {
	if (!state.incidentInfo) return;

	state.isProcessing = true;
	try {
		// Cấu hình vận hành phải có TRƯỚC khi đọc phạm vi, nếu không sẽ âm thầm dùng giá trị mặc định.
		await ensureOpConfigLoaded();

		// Loại thiết bị theo deviceType (VMS/LCS)
		const equipmentTypeCode = props.deviceType === VmsDeviceType.Lcs ? TmsEquipmentType.LCS : TmsEquipmentType.Vms;
		let response = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet('', equipmentTypeCode);
		const equipmentTypeId = response.data?.result?.length > 0 ? response.data?.result[0].id : null;
		
		const incidentDirection = state.incidentInfo?.influenceScope ?? null;

		// small       : phạm vi TÍCH SẴN (BE quyết định isRecommended theo phạm vi này).
		// chosenRange : phạm vi HIỂN THỊ ứng viên = giá trị slider, luôn kẹp trong [small, max(small, large)].
		//   Lần mở đầu open() đặt expandRange = small -> chỉ hiện thiết bị gần, đúng bằng phạm vi tích sẵn.
		//   Người dùng kéo slider mới mở rộng dần tới large; phần mở rộng chỉ GỢI Ý, không tích sẵn.
		const { small, large } = readRangeConfig();
		const maxRange = Math.max(small, large);
		const requestedRange = state.expandRange > 0 ? state.expandRange : small;
		const chosenRange = Math.min(Math.max(requestedRange, small), maxRange);

		// Cấu hình vị trí đầu/cuối tuyến (đơn vị mét = km*1000+met) phục vụ "Toàn tuyến"
		const routeStartPosition = readOpConfigIntOrNull(TmsIncidentConfigCodeEnum.RouteStartPosition);
		const routeEndPosition = readOpConfigIntOrNull(TmsIncidentConfigCodeEnum.RouteEndPosition);

		// Lựa chọn user hiện tại (dùng khi reload để không mất tick)
		const previousSelected = [...(state.selectedRecommendedEquipmentList ?? [])];

		response = await getAPI(TmsEquipmentApi).apiTmsTmsequipmentListequipmentsuggestionPost({
			kmNumber: state.incidentInfo.kmNumber,
			metNumber: state.incidentInfo.metNumber,
			suggestionSmallRange: small,
			suggestionLongRange: chosenRange,
			directionId: incidentDirection,
			equipmentTypeId: equipmentTypeId,
			// Loại sự kiện để BE tra ProcessType (Before/After) trong TmsEquipmentEventRule
			eventTypeId: state.incidentInfo?.eventTypeId ?? null,
			// Tuyến của sự cố (tms_zone_location) để BE chỉ tích chọn sẵn thiết bị đúng tuyến
			location: state.incidentInfo?.location ?? null,
			// Mở rộng ứng viên khi phạm vi hiển thị lớn hơn phạm vi tích sẵn + toàn tuyến (có thể đồng thời)
			isExpand: chosenRange > small,
			isWholeRoute: state.isWholeRoute,
			routeStartPosition: routeStartPosition,
			routeEndPosition: routeEndPosition,
		});

		state.recommendedEquipmentList = response.data.result ?? [];
		const availableIds = new Set(state.recommendedEquipmentList.map((x: any) => x?.id ?? x?.equipmentId));

		// Danh sách thiết bị nằm trong phạm vi đang chọn (BE trả isRecommended = true khi trong bán kính).
		const recommendedIds =
			state.recommendedEquipmentList
				?.filter((item: any) => item?.isRecommended)
				?.map((item: any) => item?.id ?? item?.equipmentId)
				?.filter((item: any) => item != null) ?? [];

		if (preserveSelection) {
			// Kéo phạm vi / bật "Toàn tuyến": giữ tick cũ còn hợp lệ + BỔ SUNG thiết bị mới vào bán kính.
			const kept = previousSelected.filter((id: any) => availableIds.has(id));
			state.selectedRecommendedEquipmentList = Array.from(new Set([...kept, ...recommendedIds]));
		} else {
			// Lần mở đầu: tick tự động các thiết bị nằm trong bán kính.
			state.selectedRecommendedEquipmentList = recommendedIds;
		}
	} catch (error) {
		console.error('loadRecommendedEquipmentList', error);
	} finally {
		state.isProcessing = false;
	}
};

// Manual: lấy TẤT CẢ thiết bị NẰM TRONG TUYẾN theo cấu hình vị trí đầu/cuối (lọc theo vị trí).
// ĐỘC LẬP với incident: lọc client-side trên state.equipmentList; KHÔNG có mở rộng/toàn tuyến.
const filterManualEquipmentByConfig = () => {
	const list = state.equipmentList ?? [];

	// Vị trí đầu/cuối tuyến (đơn vị mét = km*1000+met)
	const routeStart = readOpConfigIntOrNull(TmsIncidentConfigCodeEnum.RouteStartPosition);
	const routeEnd = readOpConfigIntOrNull(TmsIncidentConfigCodeEnum.RouteEndPosition);
	const hasRoute = routeStart != null && routeEnd != null;
	const lo = hasRoute ? Math.min(routeStart, routeEnd) : 0;
	const hi = hasRoute ? Math.max(routeStart, routeEnd) : 0;

	// Thiết bị có vị trí nằm trong tuyến; luôn giữ thiết bị đang mở. Chưa cấu hình tuyến -> lấy tất cả.
	const filtered = hasRoute
		? list.filter((e: any) => {
				const p = (e.kmNumber ?? 0) * 1000 + (e.metNumber ?? 0);
				return (p >= lo && p <= hi) || e.id === state.itemId;
			})
		: list;

	buildEquipmentGroupByDirection(filtered);

	// Luôn giữ thiết bị đang mở được chọn.
	if (state.itemId && !(state.listVmsSelected ?? []).includes(state.itemId)) {
		state.listVmsSelected = [...(state.listVmsSelected ?? []), state.itemId];
	}
};

// Nạp lại danh sách gợi ý khi đổi phạm vi/toàn tuyến (chỉ dùng cho incident).
const reloadSuggestion = () => loadRecommendedEquipmentList(true);

// Điểm rẽ nhánh nạp thiết bị theo mode.
const loadEquipmentOrRecommend = async () => {
	if (props.mode === 'manual') {
		// Lọc theo vị trí đầu/cuối tuyến -> phải có cấu hình trước, nếu không sẽ lấy tất cả thiết bị.
		await ensureOpConfigLoaded();
		filterManualEquipmentByConfig();
	} else {
		await loadRecommendedEquipmentList();
	}
};

//============ #SECTION: SUBMIT (giữ ở index) =============================

// Cập nhật trạng thái công việc sự cố khi submit (chỉ mode incident).
const onSubmitData = async () => {
	if (props.mode === 'incident' && state.ruleForm.workExecuteId) {
		await getAPI(TmsWorkExecuteApi).apiTmsTmsworkexecuteUpdatestatePost({
			id: state.ruleForm.workExecuteId,
			state: TmsWorkExecuteStateEnum.Executed,
			remark: state.ruleForm.workExecuteRemark,
		});
	}
	return true;
};

// Chuẩn bị payload AddControlInput theo loại điều khiển.
const prepareDataSubmit = () => {
	const controlType = state.ruleForm.controlType;

	switch (controlType) {
		case VmsActionType.Image:
			state.addControlProcess.url = state.ruleForm.urlImage;
			break;
		case VmsActionType.Template:
			syncTemplateJson();
			state.addControlProcess.url = state.ruleForm.urlImage;
			(state.addControlProcess as any).jsonContent = state.previewTemplateJson;
			{
				const w = Number(state.vmsData?.width),
					h = Number(state.vmsData?.height);
				state.addControlProcess.size = w > 0 && h > 0 ? `${w}x${h}` : '384x64';
			}
			break;
		case VmsActionType.Text:
		case VmsActionType.Phrase:
		case VmsActionType.Message:
			state.addControlProcess.content1 = state.ruleForm.row1?.trim();
			state.addControlProcess.content2 = state.ruleForm.row2?.trim();
			state.addControlProcess.color = state.ruleForm.textColor;
			state.addControlProcess.backColor = state.ruleForm.bgColor;
			state.addControlProcess.font = state.ruleForm.selectedFont;
			break;
		case VmsActionType.Time: {
			const imageUrl = state.listData.templateList?.find((item: any) => item.id == state.ruleForm.imgTemplate)?.preview;
			state.addControlProcess.url = imageUrl || '';
			state.addControlProcess.timeStart = state.ruleForm.startTime as any;
			state.addControlProcess.timeEnd = state.ruleForm.endTime as any;
			break;
		}
		case VmsActionType.Schedule:
			if (!state.addControlProcess.actionType || state.addControlProcess.actionType == '') state.addControlProcess.actionType = 'start';
			if (state.addControlProcess.actionType == 'stop') break;
			state.addControlProcess.scheduleList = [] as Array<ControlScheduleInput>;
			state.scheduleImageList.forEach((item: any) => {
				state.addControlProcess.scheduleList?.push({ url: item.preview, timeToLive: item.timeToLive || 5 } as ControlScheduleInput);
			});
			break;
	}

	state.addControlProcess.controlType = controlType;
	state.addControlProcess.brightness = state.ruleForm.brightness;
	state.addControlProcess.priority = state.ruleForm.priority;
	(state.addControlProcess as any).playMode = state.ruleForm.playMode || 'override';
	(state.addControlProcess as any).timeToLive = Number(state.ruleForm.timeToLive) || 30;
	(state.addControlProcess as any).deviceType = props.deviceType;  // phân loại thiết bị cho BE (VMS/LCS)

};

// Lấy danh sách thiết bị theo mode: manual -> listVmsSelected, incident -> selectedRecommendedEquipmentList.
const getListEquipment = () => {
	const source = props.mode === 'manual' ? state.listVmsSelected : state.selectedRecommendedEquipmentList;
	state.addControlProcess.equipmentList = (source ?? []).map((id: any) => ({ ID: id as string }) as ControlEquipmentInput);
};

// Kiểm tra độ ưu tiên qua VmsPriorityModal.
const checkHasHigherPriority = (listSelected: any, editForm: any): boolean => {
	const hasHigherPriority = priorityModalRef.value?.openDialog(listSelected, editForm, mapStore.baseUrl, state.equipmentList, state.preview.src);
	return hasHigherPriority == true;
};

// Gửi điều khiển xuống BE và theo dõi kết quả từng thiết bị (sendStatus).
// Trả về false khi có thiết bị gửi lỗi: form giữ nguyên để người dùng thấy biển nào lỗi và gửi lại.
const processOnSubmit = async (): Promise<boolean> => {
	getListEquipment();
	const ids: string[] = (state.addControlProcess.equipmentList ?? []).map((x: any) => String(x.ID));
	clearSendStatus();
	ids.forEach((id) => (sendStatus[id] = 'pending'));
	try {
		await getAPI(VmsControlApi).apiVmsVmscontrolAddPost(state.addControlProcess);
	} catch (e) {
		clearSendStatus();
		throw e;
	}

	await waitSendSettled(ids);
	// Không nhận được socket (vd lệnh nối tiếp chỉ xếp vào playlist) -> bỏ trạng thái, không coi là lỗi
	ids.forEach((id) => {
		if (sendStatus[id] === 'pending') delete sendStatus[id];
	});

	const failedIds = ids.filter((id) => sendStatus[id] === 'fail');
	if (failedIds.length) {
		ElMessage({
			type: 'error',
			showClose: true,
			duration: 6000,
			message: $t('lz.message.vmsControl.sendPartialFail', { count: failedIds.length, total: ids.length, names: failedIds.map(equipmentNameOf).join(', ') }),
		});
		return false;
	}

	// Giữ dấu ✓ thêm chút để người dùng kịp thấy trước khi form đóng
	if (ids.some((id) => sendStatus[id] === 'success')) await new Promise((r) => setTimeout(r, SEND_SHOW_MS));
	return true;
};

// Xử lý khi xác nhận độ ưu tiên (chọn lọc thiết bị cần ghi đè).
const handlePriorityConfirm = async (listConfirm: string[]) => {
	if (listConfirm.length > 0) {
		if (props.mode === 'manual') {
			state.listVmsSelected = state.listVmsSelected.filter((x: any) => listConfirm.includes(x));
		} else {
			state.selectedRecommendedEquipmentList = state.selectedRecommendedEquipmentList.filter((x: any) => listConfirm.includes(x));
		}
		if (!(await processOnSubmit())) return;
		emit('submitSuccess');
		ElMessage.success($t('lz.message.base.processSuccess'));
	}
};

// Xử lý khi xác nhận trên Modal Sự cố (VmsIncidentModal)
const handleIncidentConfirm = async (payload: { isOverride: boolean; playMode: 'override' | 'append'; timeToLive: number }) => {
	state.ruleForm.playMode = payload.playMode;
	state.ruleForm.timeToLive = payload.timeToLive;

	prepareDataSubmit();
	if (props.mode === 'incident') state.addControlProcess.incidentId = state.incidentId;
	(state.addControlProcess as any).playMode = payload.playMode;
	(state.addControlProcess as any).timeToLive = payload.timeToLive;

	// Nghiệp vụ sự cố: cập nhật trạng thái công việc trước
	if (props.mode === 'incident') {
		const ok = await onSubmitData();
		if (!ok) {
			ElMessage.error($t('lz.message.base.processUnSuccess'));
			return;
		}
	}

	// Nếu chọn Ghi đè (Override), kiểm tra độ ưu tiên nếu cần
	if (payload.isOverride) {
		const selectedIds = props.mode === 'manual' ? state.listVmsSelected : state.selectedRecommendedEquipmentList;
		const listSelected = state.listData.currentList.filter((x: any) => selectedIds.includes(x.equipmentId));
		const needPriority = checkHasHigherPriority(listSelected, state.addControlProcess);
		if (needPriority) return; // Đợi người dùng xác nhận trên VmsPriorityModal
	}

	if (!(await processOnSubmit())) return;
	emit('submitSuccess');
	ElMessage.success($t('lz.message.base.processSuccess'));
};

// Hàm submit chính - cha gọi qua ref khi bấm nút Xác nhận (nút nằm ở component cha).
const submit = async (): Promise<boolean> => {
	const valid = await ruleFormRef.value?.validate().catch(() => false);
	if (!valid) return false;
	await nextTick();

	// Ràng buộc dữ liệu điều khiển trước khi gửi (độ dài nội dung, màu chữ trùng nền, cỡ chữ).
	if (!validateContentBeforeSubmit()) return false;

	// Bắt buộc chọn ít nhất 1 thiết bị trước khi điều khiển.
	const selectedIds = props.mode === 'manual' ? state.listVmsSelected : state.selectedRecommendedEquipmentList;
	if (!selectedIds || selectedIds.length === 0) {
		ElMessage.warning($t('lz.message.vmsControl.noEquipmentSelected'));
		return false;
	}

	prepareDataSubmit();
	if (props.mode === 'incident') state.addControlProcess.incidentId = state.incidentId;

	// KIỂM TRA SỰ CỐ: Nếu các thiết bị đang có sự cố hoặc có playlist đang append -> mở VmsIncidentModal
	// LCS không có lập lịch / playlist -> không hỏi Ghi đè/Nối tiếp, gửi đè thẳng.
	const hasIncidentConflict = props.deviceType !== VmsDeviceType.Lcs && await incidentModalRef.value?.openDialog({
		selectedEquipmentIds: selectedIds,
		equipmentList: state.equipmentList,
		newImageUrl: state.preview.src || state.ruleForm.urlImage || '',
		newTimeToLive: state.ruleForm.timeToLive || 10,
		incidentId: state.incidentId,
		baseUrl: mapStore.baseUrl,
		hasOccupiedIncidents: isDeviceUnderIncident.value,
	});

	// Nếu có sự cố -> Modal VmsIncidentModal đã mở, trả về false để dừng luồng submit chờ xác nhận
	if (hasIncidentConflict) return false;

	// Thiết bị không phục vụ sự cố khác -> luôn Ghi đè (mục 5.1). Không để cấu hình mặc định 'append' của sự kiện
	// âm thầm gửi nối tiếp khi người dùng không được hỏi.
	state.ruleForm.playMode = 'override';
	(state.addControlProcess as any).playMode = 'override';

	// Form xác nhận trước khi điều khiển VMS/LCS khi không có sự cố
	try {
		await ElMessageBox.confirm($t('lz.message.vmsControl.confirmControl'), $t('lz.message.base.warning'), {
			confirmButtonText: $t('lz.button.base.confirm'),
			cancelButtonText: $t('lz.button.base.cancel'),
			type: 'warning',
		});
	} catch {
		return false; // người dùng bấm Huỷ hoặc đóng hộp thoại
	}

	// Nghiệp vụ sự cố: cập nhật trạng thái công việc trước
	if (props.mode === 'incident') {
		const ok = await onSubmitData();
		if (!ok) {
			ElMessage.error($t('lz.message.base.processUnSuccess'));
			return false;
		}
	}

	// Kiểm tra độ ưu tiên nếu ghi đè
	const isOverride = state.ruleForm.playMode !== 'append';
	const listSelected = state.listData.currentList.filter((x: any) => selectedIds.includes(x.equipmentId));
	const needPriority = isOverride && props.deviceType !== VmsDeviceType.Lcs && checkHasHigherPriority(listSelected, state.addControlProcess);

	if (needPriority) return false;

	if (!(await processOnSubmit())) return false;
	emit('submitSuccess');
	ElMessage.success($t('lz.message.base.processSuccess'));
	return true;
};

//============ #SECTION: RUN / STOP SCHEDULING (giữ ở index) =============================

const submitrun = async () => {
	// Ràng buộc dữ liệu (lập lịch phải có ít nhất 1 ảnh) trước khi chạy.
	if (!validateContentBeforeSubmit()) return;
	state.isLoadingSchedule = true;
	state.addControlProcess.actionType = 'start';
	prepareDataSubmit();
	getListEquipment();
	try {
		await getAPI(VmsControlApi).apiVmsVmscontrolAddPost(state.addControlProcess);
		await loadListVmsRunning();
		state.isLoadingSchedule = false;
	} catch (error) {
		console.error('Error occurred while running VMS Schedule Process: ', error);
	}
};

const submitstop = async () => {
	state.isLoadingSchedule = true;
	state.addControlProcess.actionType = 'stop';
	prepareDataSubmit();
	getListEquipment();
	try {
		await getAPI(VmsControlApi).apiVmsVmscontrolAddPost(state.addControlProcess);
		await loadListVmsRunning();
		state.isLoadingSchedule = false;
		state.addControlProcess.actionType = '';
	} catch (error) {
		console.error('Error occurred while stoping VMS Schedule Process: ', error);
	}
};

//============ #SECTION: LIFECYCLE / OPEN / EXPOSE =============================
// Xử lý khi mounted: nạp dữ liệu nền + hướng, đảm bảo có dữ liệu mặc định.
onMounted(async () => {
	if (!mapStore.baseUrl) { // chưa có baseUrl thì nạp config trước
		try {
			await mapStore.fetchSystemSysconfigFileserverGet();
		} catch (e) {
			console.error(e);
		}
	}
	state.directionList = getConfigDataByCode(SysConfigTypeCodeEnum.TmsDirectionConfigCode) ?? [];  // nạp hướng
	await ensureDefaultData(); // nạp dữ liệu mặc định (font, color, message, phrase, template, image)
});

/**
 * Khởi tạo control theo ngữ cảnh. Cha gọi qua ref khi mở dialog/panel.
 * ctx (manual):   { itemId, vmsData?, actionType? }
 * ctx (incident): { incidentInfo, incidentId?, eventTypeCode?, workExecuteId?, workExecuteRemark?, vmsData?, actionType? }
 */
// Không phủ "Đang tải" lên cả form: phần nào có dữ liệu trước thì hiện trước (preview, nội dung, thiết bị...).
const open = async (ctx: any = {}) => {
	clearSendStatus(); // trạng thái gửi của lần mở trước không còn ý nghĩa
	await initForm(ctx);
};

const initForm = async (ctx: any) => {
	state.preview.showCurrent = false;
	ruleFormRef.value?.resetFields();
	state.scheduleImageList = [];

	// Đảm bảo đã nạp dữ liệu nền + hướng + cấu hình vận hành (phạm vi gợi ý, vị trí tuyến) + nội dung hiện tại mới nhất
	if (!state.directionList?.length) state.directionList = getConfigDataByCode(SysConfigTypeCodeEnum.TmsDirectionConfigCode) ?? [];
	await Promise.all([ensureDefaultData(), ensureOpConfigLoaded(), loadCurrentList()]);

	// Loại điều khiển ban đầu
	const actionType = ctx.actionType ?? VmsActionType.Text;
	state.ruleForm.controlType = actionType;
	activeVmsActionType.value = actionType;

	// Khởi tạo chế độ phát & TTL
	state.ruleForm.playMode = ctx.defaultPlayMode || 'override';
	state.ruleForm.timeToLive = ctx.timeToLive != null ? ctx.timeToLive : 30;

	if (props.mode === 'manual') {
		// ===== ngữ cảnh thủ công =====
		state.itemId = ctx.itemId;
		state.listVmsSelected = ctx.itemId ? [ctx.itemId] : [];
		state.vmsData = ctx.vmsData ?? state.equipmentList.find((u: any) => u.id === ctx.itemId) ?? {};
		state.vmsCurrent = state.listData.currentList?.find((x: any) => x.equipmentId == state.itemId);
		await loadEquipmentOrRecommend();
		await loadProcessData();
		await checkOccupiedIncidents(state.listVmsSelected);
	} else {
		// ===== ngữ cảnh sự cố =====
		state.incidentInfo = ctx.incidentInfo;
		state.incidentId = ctx.incidentInfo?.id ?? ctx.incidentId;
		state.eventTypeCode = ctx.eventTypeCode;
		state.ruleForm.workExecuteId = ctx.workExecuteId;
		state.ruleForm.workExecuteRemark = ctx.workExecuteRemark;
		state.vmsData = ctx.vmsData ?? {};
		// Slider bắt đầu ở small: lần mở đầu chỉ hiện thiết bị trong phạm vi tích sẵn.
		// Kéo slider lên mới lấy thêm ứng viên tới large (hiện ra nhưng không tích).
		state.expandRange = readRangeConfig().small;
		state.isWholeRoute = false; // reset toàn tuyến mỗi lần mở
		setDefaultVmsMessage(actionType); // fallback: câu mặc định theo terminology (nếu không có VmsEventDefault)
		await loadEquipmentOrRecommend();
		// Tự động fill nội dung mặc định theo sự kiện sự cố (VmsEventDefault + token động), silent -> không toast khi trống
		await applyEventDefaultContent(true);
		await checkOccupiedIncidents(state.selectedRecommendedEquipmentList);
	}

	toggleControlDisplay();
	fetchPreview();	// render preview NGAY lần đầu mở (không chờ debounce)
	schedulePreview();

	// Manual: hiện ảnh đang chạy trên biển. Chờ nextTick để watch "người dùng sửa nội dung" ở trên xử lý xong
	// các thay đổi do chính initForm gán (controlType, row1/row2...) rồi mới bật, tránh bị tắt ngay.
	if (props.mode === 'manual') {
		await nextTick();
		state.preview.showCurrent = !!currentImageOf(state.itemId);
	}
};


// Nguồn nội dung mặc định = nội dung hiện tại của THIẾT BỊ ĐANG ĐIỀU KHIỂN:
//  - manual   : thiết bị đang mở (state.vmsCurrent)
//  - incident : thiết bị đầu tiên đang được TICK CHỌN (không dùng thiết bị gợi ý)
const defaultContentSource = computed(() => {
	if (props.mode === 'manual') return state.vmsCurrent || null;
	const id = state.selectedRecommendedEquipmentList?.[0];
	return id ? state.listData.currentList?.find((x: any) => x.equipmentId == id) || null : null;
});

// Parse DefaultRowData (không phân biệt hoa/thường), null -> ''.
const parseDefaultRowData = (raw: any) => {
	try {
		const obj = typeof raw === 'string' ? JSON.parse(raw) : (raw || {});
		return { row1: obj?.Row1 ?? obj?.row1 ?? '', row2: obj?.Row2 ?? obj?.row2 ?? '' };
	} catch {
		return { row1: '', row2: '' };
	}
};

// Fill nội dung mặc định theo loại điều khiển hiện tại (GHI ĐÈ nội dung đang nhập).
//  - incident: lấy nội dung mặc định theo sự kiện sự cố (VmsEventDefault) + thay token động từ metadata.
//  - manual  : lấy nội dung hiện tại của thiết bị đang điều khiển (DefaultRowData/DefaultUrl).
const applyDefaultContent = async () => {
	state.preview.showCurrent = false;
	if (props.mode === 'incident') {
		const applied = await applyEventDefaultContent();
		if (applied) updatePreview();
		return;
	}

	const source = defaultContentSource.value;
	const type = state.ruleForm.controlType;

	if (type === VmsActionType.Image) {
		if (!source?.defaultUrl) return ElMessage.info($t('lz.label.base.noData'));
		state.ruleForm.imgTemplate = '';           // bỏ chọn ảnh thư viện
		state.ruleForm.urlImage = source.defaultUrl;
	} else if ([VmsActionType.Text, VmsActionType.Message, VmsActionType.Phrase].includes(type)) {
		if (!source?.defaultRowData) return ElMessage.info($t('lz.label.base.noData'));
		const { row1, row2 } = parseDefaultRowData(source.defaultRowData);
		state.ruleForm.row1 = row1;
		state.ruleForm.row2 = row2;
		updatePreview();
	} else {
		ElMessage.info($t('lz.label.base.noData'));  // Template/Time/Schedule không có nội dung mặc định
	}
};



defineExpose({ open, submit, submitrun, submitstop, applyDefaultContent, selectedCount }); // cha gọi qua ref để mở form + submit + run/stop scheduling + applyDefaultContent
</script>

<style lang="scss">
/* KHÔNG scoped: dropdown el-select teleport ra body + phần tử __list do element-plus render
   nên style scoped không với tới. Đồng bộ giao diện thẻ với LCS (lcs-img-select). */
/* Khung tooltip chi tiết thiết bị trên sơ đồ tuyến (teleport ra body nên không scoped) */
.el-popper.vc-route-tip {
	padding: 8px 10px;
	color: #e6edf7;
	background: #1d3150 !important;
	border: 1px solid rgba(96, 140, 255, 0.35) !important;
	border-radius: 10px;
	box-shadow: 0 12px 28px -10px rgba(0, 0, 0, 0.85);

	.el-popper__arrow::before {
		background: #1d3150 !important;
		border-color: rgba(96, 140, 255, 0.35) !important;
	}
}

.simple-img_select {
	max-height: 50vh;
	overflow-y: auto;

	.el-select-dropdown__list {
		display: grid;
		grid-template-columns: repeat(2, 1fr); /* ảnh VMS bản ngang -> 2 cột */
		gap: 8px;
		padding: 10px;
	}

	.el-select-dropdown__item {
		height: auto;
		padding: 10px 8px;
		display: flex;
		flex-direction: column; /* ảnh trên, tên dưới */
		align-items: center;
		gap: 8px;
		line-height: normal;
		border: 1px solid var(--el-border-color-lighter);
		border-radius: 8px;

		&.is-selected,
		&:hover {
			border-color: var(--el-color-primary);
			background: var(--el-color-primary-light-9);
		}

		/* Option upload chiếm trọn hàng đầu (nhận diện qua el-upload bên trong,
		   KHÔNG dùng :first-child vì ở chế độ Template option đầu là template card) */
		&:has(.image-uploader) {
			grid-column: 1 / -1;
			flex-direction: row;
			justify-content: center;
		}
	}

	.img-option-thumb {
		width: 100%;
		max-height: 64px;       /* ảnh ngang 384x64 */
		object-fit: contain;    /* không crop */
		background: var(--el-fill-color-light);
		border-radius: 6px;
		padding: 4px;
	}

	.img-option-name {
		font-size: 12px;
		color: var(--el-text-color-regular);
		text-align: center;
		white-space: normal;
		word-break: break-word;
		width: 100%;
	}
}
</style>


<style scoped>
.vms-control {
	width: 100%;
}

/* ============ Tông tối "control console" (đồng bộ theme màn hình lớn trafficInfo.scss) ============
   Ghi đè biến Element Plus trong phạm vi .vc-form => input/select/slider/checkbox tự đổi màu, không cần :deep từng component.
   Dropdown (teleport ra body) giữ theme chung. */
.vc-form {
	--vc-surface-1: #1d3150;
	--vc-surface-2: #182a45;
	--vc-input: #243b5e;
	--vc-line: rgba(125, 165, 255, 0.32);
	--vc-text: #f3f7fd;
	--vc-muted: #c3d2e8;
	--vc-label: #ffffff;
	--vc-accent: #22d3ee;
	--vc-warn: #f59e0b;
	--vc-ease: cubic-bezier(0.22, 1, 0.36, 1);

	--el-fill-color-blank: var(--vc-input);
	--el-bg-color: var(--vc-input);
	--el-card-bg-color: transparent;
	--el-text-color-primary: var(--vc-text);
	--el-text-color-regular: #eef3fb;
	--el-text-color-secondary: var(--vc-muted);
	--el-text-color-placeholder: #93a7c6;
	--el-border-color: rgba(125, 165, 255, 0.45);
	--el-border-color-light: rgba(125, 165, 255, 0.36);
	--el-border-color-lighter: rgba(125, 165, 255, 0.26);
	--el-border-color-hover: rgba(140, 180, 255, 0.75);
	--el-fill-color-light: rgba(125, 165, 255, 0.14);
	--el-fill-color: rgba(125, 165, 255, 0.2);
	--el-disabled-bg-color: rgba(125, 165, 255, 0.08);
	color: var(--vc-text);
}

/* !important: khi form nằm trong el-dialog (luồng sự cố vmsModal/lcsModal), preset theme
   (depthBlueAmber.scss: html.design-... .el-dialog .el-form-item__label) có độ ưu tiên cao hơn
   và tô label xám #475569 trên nền tối. Form thủ công (InfoPanel) không nằm trong el-dialog nên không bị. */
.vc-form :deep(.el-form-item__label) {
	color: var(--vc-label) !important;
	font-size: 13px;
	font-weight: 500;
	/* Nhãn động (tên ô của template tuỳ ý) dài hơn cột thì xuống dòng thay vì tràn vào ô nhập */
	overflow-wrap: anywhere;
}

/* Khối nền kính tối thay cho khối trắng viền xanh 3px cũ */
.border-vms-info,
.border-content-info {
	width: 100%;
	padding: 10px 12px;
	border-radius: 12px;
	border: 1px solid var(--vc-line);
	background: linear-gradient(160deg, var(--vc-surface-1) 0%, var(--vc-surface-2) 100%);
	box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.06), 0 10px 24px -16px rgba(0, 0, 0, 0.85);
}

.vc-section-title,
.schedule-card-title {
	font-size: 11.5px;
	font-weight: 700;
	letter-spacing: 0.08em;
	text-transform: uppercase;
	color: #ffffff;
}

/* ---------- Khung nội dung ghim ở đầu vùng cuộn ----------
   Cuộn xuống danh sách thiết bị vẫn thấy nội dung sẽ gửi. Không ghim khi lập lịch (danh sách ảnh dài).
   --vc-sticky-top: vỏ chứa có padding thì truyền giá trị âm để khung dính sát mép trên (vd InfoPanel -10px). */
.vc-preview.is-sticky {
	position: sticky;
	top: var(--vc-sticky-top, 0px);
	z-index: 5;
	/* Nền đặc: nội dung cuộn bên dưới không lộ qua */
	background: linear-gradient(160deg, #1f3454 0%, #192c48 100%);
	box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.06), 0 12px 20px -12px rgba(0, 0, 0, 0.95);
}

/* ---------- Mô phỏng biển LED ---------- */
.vc-preview-head {
	display: flex;
	align-items: center;
	justify-content: space-between;
	margin-bottom: 8px;
}
.vc-preview-chips {
	display: flex;
	gap: 6px;
}
.vc-chip {
	display: inline-flex;
	align-items: center;
	gap: 4px;
	padding: 2px 8px;
	border-radius: 999px;
	font-size: 11.5px;
	font-weight: 600;
	color: var(--vc-accent);
	background: color-mix(in srgb, var(--vc-accent) 12%, transparent);
	border: 1px solid color-mix(in srgb, var(--vc-accent) 30%, transparent);
	font-variant-numeric: tabular-nums;
}

/* --led-h: chiều cao khung nội dung, dùng chung cho cả 2 chế độ xem để khung không nhảy khi chuyển.
   VMS (biển ngang ~384x64): 96px => phóng ~1.5 lần, đủ đọc mà không chiếm chỗ danh sách thiết bị.
   LCS (biển vuông): 150px. */
.led-board {
	--led-h: 96px;
	position: relative;
	display: flex;
	align-items: center;
	justify-content: center;
	min-height: calc(var(--led-h) + 16px);
	padding: 8px;
	border-radius: 10px;
	background: radial-gradient(ellipse at center, #0b1220 0%, #04070d 100%);
	border: 1px solid rgba(255, 255, 255, 0.06);
	box-shadow: inset 0 0 0 3px #070b13, inset 0 0 24px rgba(0, 0, 0, 0.9), 0 0 0 1px rgba(96, 140, 255, 0.12);
}
.led-board.is-square {
	--led-h: 150px;
}
/* Điểm LED: phóng nội dung theo CHIỀU CAO cố định (--led-h), chiều rộng theo tỉ lệ ảnh.
   led-screen ôm sát ảnh nên lưới điểm LED chỉ phủ đúng vùng nội dung, không tràn ra 2 bên. */
.led-screen {
	position: relative;
	display: flex;
	justify-content: center;
	width: fit-content;
	max-width: 100%;
	overflow: hidden;
	border-radius: 2px;
}
.led-image {
	display: block;
	width: auto;
	height: var(--led-h);
	max-width: 100%;
	object-fit: contain;
	image-rendering: pixelated; /* giữ nét điểm ảnh như bóng LED thật khi phóng to */
}
/* Lưới điểm LED phủ lên nội dung */
.led-screen::after {
	content: '';
	position: absolute;
	inset: 0;
	pointer-events: none;
	background-image: radial-gradient(circle, transparent 52%, rgba(0, 0, 0, 0.38) 56%);
	background-size: 3px 3px;
}
/* Vệt quét sáng chạy ngang một lần khi nội dung đổi */
.led-screen::before {
	content: '';
	position: absolute;
	top: 0;
	bottom: 0;
	left: -40%;
	width: 40%;
	z-index: 1;
	pointer-events: none;
	background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.18), transparent);
	animation: led-sweep 0.9s var(--vc-ease) 0.1s both;
}
/* Chế độ "Ảnh gốc": hiển thị như trước - kích thước thật, ảnh mượt, không lưới điểm / vệt quét */
/* Ảnh gốc: kích thước thật, chỉ thu nhỏ khi ảnh lớn hơn khung (không cao hơn chế độ điểm LED) */
.led-board.is-plain .led-image {
	width: auto;
	height: auto;
	max-width: 100%;
	max-height: var(--led-h);
	image-rendering: auto;
}
.led-board.is-plain .led-screen::after,
.led-board.is-plain .led-screen::before {
	display: none;
}

/* Nút chuyển chế độ xem (segmented nhỏ) */
.vc-view-toggle {
	display: inline-flex;
	padding: 2px;
	border-radius: 999px;
	background: rgba(96, 140, 255, 0.1);
	border: 1px solid var(--vc-line);
}
.vc-view-btn {
	padding: 2px 10px;
	border: none;
	border-radius: 999px;
	background: transparent;
	color: var(--vc-muted);
	font-size: 11.5px;
	font-weight: 600;
	line-height: 1.5;
	cursor: pointer;
	transition: background-color 0.2s, color 0.2s;
}
.vc-view-btn:hover {
	color: var(--vc-text);
}
.vc-view-btn.is-active {
	color: #06212a;
	background: var(--vc-accent);
}
.vc-view-btn:focus-visible {
	outline: 2px solid var(--vc-accent);
	outline-offset: 1px;
}

.led-empty {
	width: 100%;
	min-height: 64px;
	display: flex;
	align-items: center;
	justify-content: center;
}
.led-error {
	color: #f87171;
	font-size: 12.5px;
}
.led-swap-enter-active,
.led-swap-leave-active {
	transition: opacity 0.2s ease, transform 0.2s ease;
}
.led-swap-enter-from,
.led-swap-leave-to {
	opacity: 0;
	transform: scale(0.98);
}

/* ---------- Chấm màu + độ sáng ---------- */
.vc-swatch {
	display: inline-block;
	width: 12px;
	height: 12px;
	margin-right: 8px;
	border-radius: 50%;
	vertical-align: -1px;
	box-shadow: 0 0 0 1px rgba(148, 163, 184, 0.55);
}
.vc-brightness {
	display: flex;
	align-items: center;
	gap: 12px;
	width: 100%;
}
.vc-brightness .el-slider {
	flex: 1;
}
.vc-brightness-value {
	min-width: 40px;
	text-align: right;
	font-size: 12.5px;
	font-weight: 600;
	color: var(--vc-accent);
	font-variant-numeric: tabular-nums;
}

/* ---------- Số thiết bị đã chọn ---------- */
.vc-count {
	font-size: 11.5px;
	font-weight: 700;
	padding: 1px 8px;
	border-radius: 999px;
	color: var(--vc-muted);
	background: rgba(125, 165, 255, 0.22);
	font-variant-numeric: tabular-nums;
	transition: color 0.2s, background-color 0.2s;
}
.vc-count.is-active {
	color: #06212a;
	background: var(--vc-accent);
}

@keyframes vc-rise {
	from {
		opacity: 0;
		transform: translateY(6px);
	}
	to {
		opacity: 1;
		transform: none;
	}
}
@keyframes led-sweep {
	from {
		left: -40%;
	}
	to {
		left: 110%;
	}
}
@keyframes vc-pop {
	0% {
		transform: scale(0.6);
	}
	60% {
		transform: scale(1.15);
	}
	100% {
		transform: scale(1);
	}
}

@media (prefers-reduced-motion: reduce) {
	.led-screen::before {
		animation: none;
	}
	.led-swap-enter-active,
	.led-swap-leave-active {
		transition: none;
	}
}

.preview {
	border: 1px solid var(--vc-line);
	overflow: hidden;
}

/* Case transformation buttons */
.input-with-case-btns {
	display: flex;
	align-items: center;
	gap: 6px;
	width: 100%;
}
.input-with-case-btns .el-input {
	flex: 1;
}
.case-btn-group {
	display: flex;
	gap: 3px;
	flex-shrink: 0;
}
.case-btn {
	width: 28px !important;
	height: 28px !important;
	padding: 0 !important;
	font-size: 11px !important;
	font-weight: bold !important;
	/* Nút phụ trên nền tối: nền trong, chữ sáng, hover mới tô primary */
	background-color: rgba(125, 165, 255, 0.18);
	border-color: rgba(140, 180, 255, 0.45);
	color: #e3ecfb;
	transition: background-color 0.2s, color 0.2s, border-color 0.2s;
}
.case-btn:hover {
	background-color: var(--el-color-primary);
	border-color: var(--el-color-primary);
	color: #fff;
}

:deep(.el-form-item__content) {
	justify-content: flex-start;
}

.simple-img_select li {
	display: flex;
	align-items: center;
	gap: 10px;
	padding: 10px !important;
	height: auto !important;
	max-height: 70px;
	border-bottom: thin dashed #e5e5e5;
}
.simple-img_select li img {
	max-height: 50px !important;
	max-width: 100% !important;
}

/* Khối header lập lịch + thumbnail */
.schedule-card-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding: 6px 4px 10px;
	border-bottom: 1px solid var(--el-border-color-lighter);
	margin-bottom: 8px;
}
.schedule-card-actions {
	display: flex;
	gap: 6px;
}
.schedule-item-row {
	display: flex;
	align-items: center;
	gap: 8px;
	width: 100%;
}
.schedule-item-thumb {
	flex: 1;
	min-width: 0; /* cho ảnh co lại được, tránh đẩy ô timeToLive + nút xoá tràn ra ngoài card */
	max-height: 72px;
	object-fit: contain;
	border-radius: 4px;
	background: var(--el-fill-color-light);
}
.schedule-ttl-wrap {
	display: flex;
	align-items: center;
	gap: 4px;
	flex-shrink: 0;
}
.uploaded-image {
	width: 100%;
	height: auto;
}

.draggable-container {
	list-style: none;
	padding: 0;
	margin: 0;
	touch-action: none;
	position: relative;
}
.list-group-item {
	margin-bottom: 8px;
	border: 1px solid var(--el-border-color);
	border-radius: 4px;
	padding: 8px;
	background-color: var(--el-bg-color);
	position: relative;
	z-index: 1;
	touch-action: none;
}
.list-group-item.sortable-ghost {
	opacity: 0.5;
	background: rgba(34, 211, 238, 0.18);
}
.list-group-item.sortable-chosen {
	background: #17294a;
	z-index: 10;
	box-shadow: 0 0 10px rgba(0, 0, 0, 0.2);
}

.reorder-buttons {
	display: flex;
	margin: 0 5px;
}
.horizontal-buttons {
	flex-direction: row;
	align-items: center;
	margin-right: 8px;
}
.reorder-buttons i {
	padding: 3px;
	transition: all 0.2s ease;
	border-radius: 3px;
}
.reorder-buttons i:hover {
	background-color: rgba(96, 140, 255, 0.18);
	transform: scale(1.2);
}
.reorder-buttons i.disabled {
	opacity: 0.3;
	cursor: not-allowed;
}

/* ============ Sơ đồ tuyến chọn thiết bị (manual & incident) ============ */
.equipment-list-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	margin-bottom: 6px;
}

.recommended-toolbar {
	display: flex;
	align-items: center;
	justify-content: flex-end;
	gap: 12px;
	flex-wrap: wrap;
	margin-bottom: 6px;
	font-size: 12.5px;
	color: var(--vc-label);
}

.route-loading {
	min-height: 90px;
}
.route-empty {
	margin: 12px 0 4px;
	text-align: center;
	color: var(--vc-muted);
	font-size: 12.5px;
}

.route-map {
	position: relative;
	padding-top: 2px;
}

/* Trục lý trình */
.route-axis {
	position: relative;
	height: 18px;
	margin-bottom: 2px;
	border-bottom: 1px dashed var(--el-border-color-lighter);
}
.route-tick {
	position: absolute;
	top: 0;
	transform: translateX(-50%);
	font-size: 10.5px;
	color: var(--vc-muted);
	white-space: nowrap;
	font-variant-numeric: tabular-nums;
}
.route-tick.is-end {
	color: var(--vc-label);
	font-weight: 600;
}

/* Mỗi hướng là 1 làn */
.route-lane {
	padding-top: 6px;
	animation: vc-rise 0.32s var(--vc-ease) both;
}
.route-lane + .route-lane {
	margin-top: 4px;
	border-top: 1px solid var(--el-border-color-lighter);
}
.route-lane-head {
	display: flex;
	align-items: center;
	justify-content: space-between;
}
.route-lane-head .el-checkbox {
	height: 22px;
}

.direction-title-text {
	font-weight: 600;
	font-size: 13px;
	color: var(--el-text-color-primary);
}
.direction-badge {
	font-size: 11px;
	font-weight: 600;
	padding: 1px 7px;
	border-radius: 10px;
	background-color: rgba(125, 165, 255, 0.28);
	color: #e0ebff;
	font-variant-numeric: tabular-nums;
}

.route-track {
	position: relative;
}
/* Mặt đường + vạch kẻ giữa */
.route-road {
	position: absolute;
	left: 0;
	right: 0;
	bottom: 0;
	height: 14px;
	border-radius: 7px;
	background: linear-gradient(180deg, #2a4266 0%, #223858 100%);
	border: 1px solid rgba(140, 180, 255, 0.5);
	box-shadow: inset 0 1px 3px rgba(0, 0, 0, 0.6);
}
.route-road::after {
	content: '';
	position: absolute;
	left: 8px;
	right: 8px;
	top: 50%;
	height: 2px;
	transform: translateY(-50%);
	background: repeating-linear-gradient(90deg, rgba(241, 245, 249, 0.7) 0 10px, transparent 10px 20px);
}

/* Thiết bị: nhãn (chip) - cọc - chấm trên mặt đường. Tầng nhãn = --tier */
.route-marker {
	position: absolute;
	bottom: 2px;
	transform: translateX(-50%);
	display: flex;
	flex-direction: column;
	align-items: center;
	padding: 0;
	border: none;
	background: none;
	cursor: pointer;
	z-index: 1;
	/* Chỉ NHÃN nhận chuột: cọc + chấm của thiết bị tầng trên chạy xuyên qua nhãn tầng dưới khi 2 thiết bị
	   cùng/ gần vị trí, nếu cả nút nhận chuột thì không bấm được thiết bị bên dưới. */
	pointer-events: none;
}
.route-marker .route-chip {
	pointer-events: auto;
}
.route-marker:hover,
.route-marker:focus-visible {
	z-index: 3;
}
.route-chip {
	display: inline-flex;
	align-items: center;
	gap: 4px;
	padding: 2px 9px;
	border-radius: 999px;
	font-size: 11.5px;
	font-weight: 600;
	line-height: 1.5;
	white-space: nowrap;
	color: var(--vc-text);
	background: #2b4670;
	border: 1px solid rgba(150, 188, 255, 0.65);
	transition: transform 0.18s var(--vc-ease), background-color 0.18s, color 0.18s, box-shadow 0.18s, border-color 0.18s;
}
.route-stem {
	width: 1px;
	height: calc(4px + var(--tier, 0) * 24px);
	background: rgba(160, 195, 255, 0.7);
	transition: background-color 0.18s;
}
.route-pin {
	position: relative;
	width: 10px;
	height: 10px;
	border-radius: 50%;
	background: #223858;
	border: 2px solid #b4cbef;
	transition: border-color 0.18s, background-color 0.18s, transform 0.18s;
}

.route-marker:hover .route-chip,
.route-marker:focus-visible .route-chip {
	transform: translateY(-2px);
	border-color: var(--vc-accent);
	box-shadow: 0 6px 14px -6px rgba(34, 211, 238, 0.7);
}
.route-marker:focus-visible {
	outline: none;
}
.route-marker:hover .route-pin {
	transform: scale(1.15);
}

/* Đang chọn */
.route-marker.is-selected .route-chip {
	color: #06212a;
	background: var(--vc-accent);
	border-color: var(--vc-accent);
	box-shadow: 0 0 12px -4px var(--vc-accent);
}
.route-marker.is-selected .route-stem {
	background: var(--vc-accent);
}
.route-marker.is-selected .route-pin {
	background: var(--vc-accent);
	border-color: #e0fbff;
	animation: vc-pop 0.25s var(--vc-ease);
}

/* Đang chạy lập lịch: chấm đỏ nhịp nhẹ trên cọc (chi tiết xem khi rê chuột) */
.route-marker.is-running .route-pin::after {
	content: '';
	position: absolute;
	inset: -5px;
	border-radius: 50%;
	border: 2px solid rgba(239, 68, 68, 0.75);
	animation: route-pulse 1.6s ease-out infinite;
}

/* Trạng thái gửi */
.route-marker.is-send-pending .route-chip .el-icon {
	color: inherit;
}
.route-marker.is-send-success .route-chip {
	color: #052e16;
	background: #22c55e;
	border-color: #22c55e;
	box-shadow: 0 0 12px -4px #22c55e;
}
.route-marker.is-send-success .route-pin,
.route-marker.is-send-success .route-stem {
	background: #22c55e;
	border-color: #bbf7d0;
}
.route-marker.is-send-fail .route-chip {
	color: #fff;
	background: #dc2626;
	border-color: #ef4444;
	box-shadow: 0 0 12px -4px #ef4444;
}
.route-marker.is-send-fail .route-pin,
.route-marker.is-send-fail .route-stem {
	background: #ef4444;
	border-color: #fecaca;
}
.route-chip .is-loading {
	animation: rotating 1s linear infinite;
}

/* Nội dung chi tiết khi rê chuột (tooltip teleport ra body, khung ngoài style ở khối không scoped) */
.route-tip {
	min-width: 180px;
	max-width: 260px;
	display: flex;
	flex-direction: column;
	gap: 4px;
}
.route-tip-head {
	display: flex;
	align-items: baseline;
	justify-content: space-between;
	gap: 10px;
}
.route-tip-name {
	font-weight: 700;
	font-size: 13px;
	color: #f1f5ff;
}
.route-tip-km,
.route-tip-sub {
	font-size: 11.5px;
	color: #a9bddb;
	font-variant-numeric: tabular-nums;
}
.route-tip-label {
	margin-top: 2px;
	font-size: 10.5px;
	font-weight: 700;
	letter-spacing: 0.06em;
	text-transform: uppercase;
	color: #8ea6c8;
}
.route-tip-img {
	display: block;
	width: 100%;
	max-height: 90px;
	object-fit: contain;
	border-radius: 4px;
	background: #04070d;
	image-rendering: pixelated;
}
.route-tip-row {
	display: flex;
	align-items: center;
	gap: 5px;
	font-size: 12px;
	font-weight: 600;
}
.route-tip-row.is-running {
	color: #f87171;
}
.route-tip-row.is-running .is-loading {
	animation: rotating 2s linear infinite;
}
.route-tip-row.is-pending {
	color: #22d3ee;
}
.route-tip-row.is-success {
	color: #4ade80;
}
.route-tip-row.is-fail {
	color: #f87171;
}
.route-tip-hint {
	margin-top: 2px;
	padding-top: 4px;
	border-top: 1px solid rgba(96, 140, 255, 0.18);
	font-size: 11px;
	color: #8ea6c8;
}

@keyframes route-pulse {
	from {
		transform: scale(0.6);
		opacity: 1;
	}
	to {
		transform: scale(1.6);
		opacity: 0;
	}
}

@media (prefers-reduced-motion: reduce) {
	.route-lane,
	.route-marker.is-selected .route-pin,
	.route-marker.is-running .route-pin::after {
		animation: none;
	}
	.route-chip,
	.route-pin {
		transition: none;
	}
}

@keyframes rotating {
	from {
		transform: rotate(0deg);
	}
	to {
		transform: rotate(360deg);
	}
}
</style>
