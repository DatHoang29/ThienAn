<template>
  <div class="vms-incident-modal-container">
    <el-dialog
      v-model="state.isShowDialog"
      draggable
      :close-on-click-modal="false"
      width="850px"
      style="z-index: 1000;"
      body-class="custom-incident-dialog-body"
      class="vms-incident-dialog"
    >
      <template #header>
        <div class="dialog-header-wrap">
          <el-icon size="20" color="#e6a23c" style="margin-right: 6px; vertical-align: middle;">
            <ele-WarningFilled />
          </el-icon>
          <span class="dialog-title-text">{{ $t('lz.label.vms.incidentModalTitle') }}</span>
        </div>
      </template>

      <div class="incident-modal-content">
        <!-- 1. Thanh thông báo & Hướng dẫn -->
        <div class="alert-info-box">
          <el-alert
            :title="$t('lz.label.vms.incidentModalAlertTitle')"
            type="warning"
            :closable="false"
            show-icon
          >
            <template #default>
              <div class="alert-desc">
                {{ $t('lz.label.vms.incidentModalAlertDesc') }}
              </div>
            </template>
          </el-alert>
        </div>

        <!-- 2. Ô tích chọn GHI ĐÈ (Mặc định không tích = NỐI TIẾP) -->
        <div class="override-control-box">
          <el-checkbox
            v-model="state.isOverride"
            size="large"
            class="override-checkbox"
          >
            <span class="override-checkbox-label">
              {{ $t('lz.label.vms.overrideCheckboxPrompt') }}
            </span>
          </el-checkbox>
          <el-tooltip
            :content="$t('lz.label.vms.overrideCheckboxTooltip')"
            placement="top"
            :auto-close="3000"
          >
            <el-icon class="ml5 info-icon"><ele-QuestionFilled /></el-icon>
          </el-tooltip>
        </div>

        <!-- 3. Tiêu đề + Giới hạn số lượng danh sách hình ảnh -->
        <div class="playlist-meta-bar">
          <div class="meta-left">
            <el-icon class="mr5"><ele-PictureFilled /></el-icon>
            <span class="meta-title">
              {{ $t('lz.label.vms.playlistTitle') }}
            </span>
            <span class="meta-limit">
              ({{ $t('lz.label.vms.maxLimit') }}: <strong>{{ state.maxLimit }}</strong> {{ $t('lz.label.vms.pages') }})
            </span>
          </div>

          <div class="meta-right">
            <template v-if="!state.isOverride">
              <el-tag type="info" size="small" effect="plain" class="mr5">
                {{ $t('lz.label.vms.currentPages') }}: {{ maxCurrentCount }}
              </el-tag>
              <span class="meta-arrow">➔</span>
              <el-tag
                :type="maxCurrentCount + 1 > state.maxLimit ? 'warning' : 'success'"
                size="small"
                effect="dark"
                class="ml5"
              >
                {{ $t('lz.label.vms.afterAppend') }}: {{ maxCurrentCount + 1 }} / {{ state.maxLimit }} {{ $t('lz.label.vms.pages') }}
              </el-tag>
            </template>
            <template v-else>
              <el-tag type="danger" size="small" effect="dark">
                {{ $t('lz.label.vms.overrideSummary', { count: maxCurrentCount, max: state.maxLimit }) }}
              </el-tag>
            </template>
          </div>
        </div>

        <!-- Cảnh báo nếu danh sách vượt quá giới hạn tối đa -->
        <div v-if="!state.isOverride && maxCurrentCount >= state.maxLimit" class="limit-warning-bar mt5 mb5">
          <el-alert
            :title="$t('lz.label.vms.limitWarning', { max: state.maxLimit })"
            type="error"
            :closable="false"
            show-icon
          />
        </div>

        <!-- 4. Danh sách các thiết bị & hình ảnh playlist (Scrollable) -->
        <div class="scrollable-playlist-area">
          <div
            v-for="equip in state.equipmentPlaylists"
            :key="equip.equipmentId"
            class="equipment-playlist-section"
          >
            <!-- Header tên biển -->
            <div class="equipment-header-row">
              <div class="equipment-title">
                <span class="equipment-code">{{ equip.code || equip.equipmentId }}</span>
                <span v-if="equip.location" class="equipment-location">({{ equip.location }})</span>
              </div>
              <div class="equipment-status-badge">
                <el-tag v-if="state.isOverride" type="danger" size="small">
                  {{ $t('lz.label.vms.statusOverride') }}
                </el-tag>
                <el-tag v-else type="success" size="small">
                  {{ $t('lz.label.vms.statusAppend', { total: equip.items.length + 1 }) }}
                </el-tag>
              </div>
            </div>

            <!-- Chuỗi các ảnh Playlist Cards -->
            <div class="playlist-cards-track">
              <!-- A. Các hình ảnh hiện tại đang phát (Read-only) -->
              <div
                v-for="(item, idx) in equip.items"
                :key="item.id || idx"
                class="playlist-card existing-item"
                :class="{ 'is-overridden': state.isOverride }"
              >
                <div class="card-header-bar">
                  <span class="page-number-tag">{{ $t('lz.label.vms.page') }} {{ idx + 1 }}</span>
                  <span v-if="state.isOverride" class="override-strike-text">
                    {{ $t('lz.label.vms.willBeRemoved') }}
                  </span>
                </div>

                <div class="card-thumb-wrapper">
                  <img
                    :src="buildImageUrl(item.url)"
                    :alt="`Page ${idx + 1}`"
                    class="playlist-thumb-img"
                    @error="onImageError"
                  />
                  <!-- Hiệu ứng gạch chéo đỏ khi Ghi đè -->
                  <div v-if="state.isOverride" class="strikethrough-overlay">
                    <div class="strikethrough-line-diag1"></div>
                    <div class="strikethrough-line-diag2"></div>
                    <el-icon class="strikethrough-cross-icon"><ele-CloseBold /></el-icon>
                  </div>
                </div>

                <!-- Thời gian hiển thị kế bên ảnh (Read-only) -->
                <div class="card-footer-bar">
                  <span class="ttl-display" :class="{ 'ttl-struck': state.isOverride }">
                    <el-icon class="mr2"><ele-Clock /></el-icon>
                    {{ item.timeToLive || 10 }}s
                  </span>
                </div>
              </div>

              <!-- Mũi tên kết nối thứ tự phát luân phiên nếu ở chế độ Append -->
              <div v-if="!state.isOverride" class="flow-arrow-separator">
                <el-icon size="18" color="#409eff"><ele-Right /></el-icon>
              </div>

              <!-- B. Hình ảnh sự cố MỚI (Nối tiếp ngay sau các hình cũ) -->
              <div
                class="playlist-card new-item"
                :class="{ 'is-sole-override': state.isOverride }"
              >
                <div class="card-header-bar new-card-header">
                  <span class="page-number-tag new-badge">
                    {{ state.isOverride ? $t('lz.label.vms.soleOverridePage') : `${$t('lz.label.vms.page')} ${equip.items.length + 1} (${$t('lz.label.vms.new')})` }}
                  </span>
                </div>

                <div class="card-thumb-wrapper new-thumb-wrapper">
                  <img
                    :src="buildImageUrl(state.newImageUrl)"
                    alt="New Content"
                    class="playlist-thumb-img"
                    @error="onImageError"
                  />
                </div>

                <!-- Thời gian hiển thị của hình mới -->
                <div class="card-footer-bar new-footer-bar">
                  <div class="ttl-input-group">
                    <el-icon class="mr2" color="#409eff"><ele-Clock /></el-icon>
                    <el-input-number
                      v-model="state.newTimeToLive"
                      :min="1" :max="100"
                      :step="1"
                      controls-position="right"
                      size="small"
                      style="width: 82px"
                    />
                    <span class="ml3 unit-text">s</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <template #footer>
        <div class="dialog-footer-actions">
          <el-button @click="handleCancel">
            {{ $t('lz.button.base.cancel') }}
          </el-button>
          <el-button type="primary" :loading="state.submitting" @click="handleConfirm">
            {{ state.isOverride ? $t('lz.button.vms.confirmOverride') : $t('lz.button.vms.confirmAppend') }}
          </el-button>
        </div>
      </template>
    </el-dialog>
  </div>
</template>

<script lang="ts" setup name="VmsIncidentModal">
import { computed, reactive, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { axiosInstance, serveConfig } from '/@/utils/axios-utils';

const { t: $t } = useI18n();

const emits = defineEmits<{
  (e: 'handle-confirm', payload: { isOverride: boolean; playMode: 'override' | 'append'; timeToLive: number }): void;
  (e: 'handle-cancel'): void;
}>();

const NO_IMAGE_PATH = 'Upload/NoImage.bmp';

interface PlaylistItem {
  id?: string;
  url: string;
  timeToLive: number;
  incidentId?: string;
  isIncident?: boolean;
  orderNo?: number;
}

interface EquipmentPlaylist {
  equipmentId: string;
  code?: string;
  location?: string;
  items: PlaylistItem[];
}

const state = reactive({
  isShowDialog: false,
  submitting: false,
  isOverride: false, // MẶC ĐỊNH LÀ FALSE (NỐI TIẾP / APPEND)
  maxLimit: 5,
  newImageUrl: '',
  newTimeToLive: 10,
  baseUrl: '',
  incidentId: '',
  equipmentPlaylists: [] as EquipmentPlaylist[],
});

// Tính số lượng ảnh hiện tại lớn nhất trong số các thiết bị được chọn
const maxCurrentCount = computed(() => {
  if (!state.equipmentPlaylists.length) return 0;
  return Math.max(...state.equipmentPlaylists.map(e => e.items.length), 0);
});

// URL hình ảnh
const noImageUrl = computed(() => `${state.baseUrl}/${NO_IMAGE_PATH}`);
const buildImageUrl = (url?: string) => {
  if (!url) return noImageUrl.value;
  if (url.startsWith('data:') || url.startsWith('http')) return url;
  return `${state.baseUrl}/${url}`;
};

const onImageError = (e: Event) => {
  const target = e.target as HTMLImageElement;
  if (target.src !== noImageUrl.value) target.src = noImageUrl.value;
};

/**
 * Mở Dialog phân xử điều khiển sự cố
 * Trả về true nếu có thiết bị đang có nội dung sự cố (dialog mở ra chờ xác nhận)
 * Trả về false nếu không có thiết bị nào có nội dung/sự cố (tiến hành gửi thẳng)
 */
const openDialog = async (params: {
  selectedEquipmentIds: string[];
  equipmentList: any[];
  newImageUrl: string;
  newTimeToLive: number;
  incidentId?: string;
  baseUrl: string;
  hasOccupiedIncidents?: boolean;
}): Promise<boolean> => {
  state.baseUrl = params.baseUrl;
  state.newImageUrl = params.newImageUrl;
  state.newTimeToLive = params.newTimeToLive && params.newTimeToLive > 0 ? params.newTimeToLive : 10;
  state.incidentId = params.incidentId || '';
  state.isOverride = false; // Luôn mặc định là Append (Nối tiếp)
  state.equipmentPlaylists = [];

  if (!params.selectedEquipmentIds || params.selectedEquipmentIds.length === 0) {
    return false;
  }

  try {
    // 1. Gọi API lấy danh sách playlist của các thiết bị được chọn
    const res = await axiosInstance.post(
      `${serveConfig.basePath}/api/vms/VmsProcess/GetActivePlaylistByEquipment`,
      { equipmentIds: params.selectedEquipmentIds }
    );

    const apiPlaylists: Array<{ equipmentId: string; maxPlaylistItems: number; items: PlaylistItem[] }> = res.data?.result || [];
    if (apiPlaylists.length > 0 && apiPlaylists[0].maxPlaylistItems) {
      state.maxLimit = apiPlaylists[0].maxPlaylistItems;
    }

    // 2. Map thông tin thiết bị (tên, km, mét)
    const playlists: EquipmentPlaylist[] = [];
    for (const pl of apiPlaylists) {
      if (pl.items && pl.items.length > 0) {
        const eqInfo = params.equipmentList.find((item: any) => item.id === pl.equipmentId);
        playlists.push({
          equipmentId: pl.equipmentId,
          code: eqInfo?.code || pl.equipmentId,
          location: eqInfo?.kmNumber != null ? `Km${eqInfo.kmNumber}+${eqInfo.metNumber || 0}` : '',
          items: pl.items,
        });
      }
    }

    // 3. Chỉ hỏi Ghi đè/Nối tiếp khi biển đang phát nội dung của sự cố KHÁC (lịch thường / ảnh tĩnh thì gửi thẳng)
    const hasOtherIncidentContent = playlists.some((pl) =>
      pl.items.some((it: any) => it.isIncident && (!state.incidentId || it.incidentId !== state.incidentId))
    );
    if (!hasOtherIncidentContent && !params.hasOccupiedIncidents) {
      return false;
    }

    // Nếu không có playlist từ API nhưng có occupiedIncidents, tạo item giả định từ equipmentList
    if (playlists.length === 0 && params.hasOccupiedIncidents) {
      for (const eqId of params.selectedEquipmentIds) {
        const eqInfo = params.equipmentList.find((item: any) => item.id === eqId);
        playlists.push({
          equipmentId: eqId,
          code: eqInfo?.code || eqId,
          location: eqInfo?.kmNumber != null ? `Km${eqInfo.kmNumber}+${eqInfo.metNumber || 0}` : '',
          items: [{ url: NO_IMAGE_PATH, timeToLive: 10, isIncident: true, orderNo: 1 }],
        });
      }
    }

    state.equipmentPlaylists = playlists;
    state.isShowDialog = true;
    return true;
  } catch (error) {
    console.error('Failed to get active playlist:', error);
    // Nếu API lỗi nhưng có sự cố chiếm giữ, mở modal fallback
    if (params.hasOccupiedIncidents) {
      state.isShowDialog = true;
      return true;
    }
    return false;
  }
};

const handleCancel = () => {
  state.isShowDialog = false;
  emits('handle-cancel');
};

const handleConfirm = () => {
  state.isShowDialog = false;
  emits('handle-confirm', {
    isOverride: state.isOverride,
    playMode: state.isOverride ? 'override' : 'append',
    timeToLive: state.newTimeToLive,
  });
};

defineExpose({
  openDialog,
});
</script>

<style scoped lang="scss">
.vms-incident-modal-container {
  :deep(.custom-incident-dialog-body) {
    padding: 16px 20px;
    background-color: #1e1e2d;
    color: #e4e7ed;
  }

  :deep(.el-dialog__header) {
    background-color: #1a1a27;
    margin-right: 0;
    padding: 14px 20px;
    border-bottom: 1px solid #2d2d3f;
  }

  :deep(.el-dialog__footer) {
    background-color: #1a1a27;
    padding: 12px 20px;
    border-top: 1px solid #2d2d3f;
  }
}

.dialog-header-wrap {
  display: flex;
  align-items: center;
}

.dialog-title-text {
  font-size: 16px;
  font-weight: 600;
  color: #ffffff;
}

.alert-info-box {
  margin-bottom: 12px;
  :deep(.el-alert) {
    background-color: rgba(230, 162, 60, 0.12);
    border: 1px solid rgba(230, 162, 60, 0.35);
  }
  .alert-desc {
    font-size: 13px;
    line-height: 1.5;
    color: #e6a23c;
    margin-top: 3px;
  }
}

.override-control-box {
  background-color: #27273a;
  padding: 10px 16px;
  border-radius: 6px;
  border: 1px solid #36364d;
  display: flex;
  align-items: center;
  margin-bottom: 12px;

  .override-checkbox {
    margin-right: 4px;
    :deep(.el-checkbox__label) {
      font-size: 14px;
      font-weight: 600;
      color: #ff4d4f;
    }
  }

  .info-icon {
    color: #909399;
    cursor: pointer;
    &:hover {
      color: #409eff;
    }
  }
}

.playlist-meta-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 8px 14px;
  background-color: #232334;
  border-radius: 6px;
  border-left: 4px solid #409eff;
  margin-bottom: 12px;

  .meta-left {
    display: flex;
    align-items: center;
    .meta-title {
      font-weight: 600;
      color: #ffffff;
      font-size: 14px;
    }
    .meta-limit {
      margin-left: 8px;
      color: #909399;
      font-size: 13px;
      strong {
        color: #e6a23c;
      }
    }
  }

  .meta-right {
    display: flex;
    align-items: center;
    .meta-arrow {
      color: #909399;
      font-weight: bold;
    }
  }
}

.scrollable-playlist-area {
  max-height: 400px;
  overflow-y: auto;
  padding-right: 4px;

  &::-webkit-scrollbar {
    width: 6px;
  }
  &::-webkit-scrollbar-thumb {
    background: #3f3f5a;
    border-radius: 3px;
  }
}

.equipment-playlist-section {
  background-color: #27273a;
  border-radius: 8px;
  padding: 12px 14px;
  margin-bottom: 12px;
  border: 1px solid #36364d;

  .equipment-header-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 10px;
    padding-bottom: 6px;
    border-bottom: 1px dashed #3f3f5a;

    .equipment-code {
      font-weight: 600;
      color: #409eff;
      font-size: 14px;
    }
    .equipment-location {
      margin-left: 6px;
      color: #a0a0b8;
      font-size: 13px;
    }
  }
}

.playlist-cards-track {
  display: flex;
  align-items: center;
  gap: 12px;
  overflow-x: auto;
  padding-bottom: 6px;

  &::-webkit-scrollbar {
    height: 6px;
  }
  &::-webkit-scrollbar-thumb {
    background: #4a4a68;
    border-radius: 3px;
  }
}

.playlist-card {
  flex: 0 0 160px;
  background-color: #1a1a27;
  border: 1px solid #36364d;
  border-radius: 6px;
  padding: 8px;
  transition: all 0.25s ease;
  position: relative;

  .card-header-bar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 6px;
    font-size: 12px;

    .page-number-tag {
      font-weight: 600;
      color: #cfd3dc;
    }
    .override-strike-text {
      color: #f56c6c;
      font-size: 11px;
      font-weight: 600;
    }
  }

  .card-thumb-wrapper {
    position: relative;
    width: 100%;
    height: 75px;
    background-color: #000;
    border-radius: 4px;
    overflow: hidden;
    display: flex;
    align-items: center;
    justify-content: center;

    .playlist-thumb-img {
      max-width: 100%;
      max-height: 100%;
      object-fit: contain;
      transition: filter 0.25s ease, opacity 0.25s ease;
    }
  }

  .card-footer-bar {
    display: flex;
    align-items: center;
    justify-content: center;
    margin-top: 6px;
    font-size: 12px;
    color: #909399;

    .ttl-display {
      display: inline-flex;
      align-items: center;
      background-color: #27273a;
      padding: 2px 8px;
      border-radius: 4px;
      font-weight: 500;
      color: #a8abb2;

      &.ttl-struck {
        text-decoration: line-through;
        color: #f56c6c;
        background-color: rgba(245, 108, 108, 0.1);
      }
    }
  }

  /* HIỆU ỨNG GẠCH BỎ KHI TÍCH GHI ĐÈ */
  &.is-overridden {
    border-color: #f56c6c;
    background-color: rgba(245, 108, 108, 0.04);

    .playlist-thumb-img {
      filter: grayscale(100%);
      opacity: 0.3;
    }

    .strikethrough-overlay {
      position: absolute;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      background: rgba(245, 108, 108, 0.2);
      display: flex;
      align-items: center;
      justify-content: center;
      pointer-events: none;

      .strikethrough-line-diag1 {
        position: absolute;
        top: 50%;
        left: 0;
        width: 100%;
        height: 3px;
        background-color: #f56c6c;
        transform: rotate(25deg);
        box-shadow: 0 0 6px rgba(245, 108, 108, 0.8);
      }
      .strikethrough-line-diag2 {
        position: absolute;
        top: 50%;
        left: 0;
        width: 100%;
        height: 3px;
        background-color: #f56c6c;
        transform: rotate(-25deg);
        box-shadow: 0 0 6px rgba(245, 108, 108, 0.8);
      }

      .strikethrough-cross-icon {
        color: #ffffff;
        font-size: 26px;
        z-index: 2;
        background-color: rgba(245, 108, 108, 0.85);
        border-radius: 50%;
        padding: 4px;
      }
    }
  }

  /* THẺ HÌNH MỚI NỐI TIẾP */
  &.new-item {
    border: 2px solid #409eff;
    background-color: rgba(64, 158, 255, 0.06);
    box-shadow: 0 0 10px rgba(64, 158, 255, 0.2);

    .new-badge {
      color: #409eff;
      font-weight: bold;
    }

    .new-thumb-wrapper {
      border: 1px solid #409eff;
    }

    .new-footer-bar {
      .ttl-input-group {
        display: flex;
        align-items: center;
        justify-content: center;

        .unit-text {
          font-size: 12px;
          color: #cfd3dc;
        }
      }
    }

    &.is-sole-override {
      border-color: #67c23a;
      background-color: rgba(103, 194, 58, 0.08);
      box-shadow: 0 0 12px rgba(103, 194, 58, 0.3);

      .new-badge {
        color: #67c23a;
      }
      .new-thumb-wrapper {
        border-color: #67c23a;
      }
    }
  }
}

.flow-arrow-separator {
  flex: 0 0 24px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.dialog-footer-actions {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
}
</style>
