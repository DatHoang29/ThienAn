<template>
    <InfoPanel
        v-model:active-tab="state.activeTab"
        :tabs="tabs"
        :device-name="state.data?.code"
        :device-location="deviceLocation"
        :status-variant="statusVariant"
        :show-footer="state.activeTab === 'controlTab'"
        @closed="emits('closed')"
        @close-others="emits('closeOthers')"
        @close-all="emits('closeAll')"
    >
        <!-- Info -->
        <template #infoTab>
            <div class="content-text">
                <div class="border-traffic-info">
                    <h3 class="section-title">{{ $t('lz.label.vms.deviceSumary') }}</h3>
                    <div class="background-summary-device">
                        <span :style="{ color: '#000' }"> {{ $t('lz.label.base.status') }}: </span>
                        <div class="status-indicator">
                            <TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsEquipmentState, state.data?.equipmentState)" />
                        </div>
                    </div>
                </div>

                <div class="border-traffic-info mt10">
                    <h3 class="section-title">{{ $t('lz.label.vms.deviceInformation') }}</h3>
                    <div class="info-grid">
                        <div class="info-item">
                            <span class="info-k">{{ $t('lz.entity.base.code') }}</span>
                            <span class="info-v">{{ state.data?.code || '-' }}</span>
                        </div>
                        <div class="info-item">
                            <span class="info-k">{{ $t('lz.entity.base.name') }}</span>
                            <span class="info-v">{{ state.data?.name || '-' }}</span>
                        </div>
                        <div class="info-item">
                            <span class="info-k">{{ $t('lz.entity.base.type') }}</span>
                            <span class="info-v">{{ state.data?.type || '-' }}</span>
                        </div>
                        <div class="info-item">
                            <span class="info-k">{{ $t('lz.entity.tms.location') }}</span>
                            <span class="info-v">Km{{ state.data?.kmNumber ?? '-' }}+{{ state.data?.metNumber ?? '-' }}</span>
                        </div>
                        <div class="info-item">
                            <span class="info-k">{{ $t('lz.entity.vms.ip') }}</span>
                            <span class="info-v">{{ state.data?.ip || '-' }}</span>
                        </div>
                        <div class="info-item">
                            <span class="info-k">{{ $t('lz.entity.vms.port') }}</span>
                            <span class="info-v">{{ state.data?.port || '-' }}</span>
                        </div>
                    </div>
                </div>

                <!-- Preview ảnh LCS hiện tại (tự đổi realtime qua NATS) -->
                <div class="border-traffic-info mt10">
                    <h3 class="section-title">{{ $t('lz.label.base.preview') }}</h3>
                    <div class="lcs-preview">
                        <el-image
                            :src="lcsImageUrl"
                            :preview-src-list="[lcsImageUrl]"
                            :preview-teleported="true"
                            fit="contain"
                            hide-on-click-modal
                           
                            class="lcs-preview-img"
                        >
                            <template #error>
                                <img :src="noImageUrl" class="lcs-preview-img" :alt="$t('lz.label.base.preview')" />
                            </template>
                        </el-image>
                    </div>
                </div>

                <!-- Lấy hình thực tế đang hiển thị trên thiết bị (đối chiếu với nội dung hiện tại) -->
                <div class="border-traffic-info mt10">
                    <div class="real-image-action">
                        <el-button type="primary" :loading="state.realImageLoading" @click="fetchRealImage">
                            {{ $t('lz.button.vms.readDeviceImage') }}
                        </el-button>
                    </div>
                    <div v-if="state.realImageUrl" class="lcs-preview mt10">
                        <el-image
                            :src="state.realImageUrl"
                            :preview-src-list="[state.realImageUrl]"
                            :preview-teleported="true"
                            fit="contain"
                            hide-on-click-modal
                            class="lcs-preview-img"
                        />
                    </div>
                </div>
            </div>
        </template>

        <!-- Status tab: giao diện trạng thái thiết bị (mock, bổ sung tính năng thật sau) -->
        <template #statusTab>
            <deviceStatusTab :item-id="state.itemId" :device-name="state.data?.name" device-type="lcs" />
        </template>

        <!-- Control tab: uỷ quyền toàn bộ điều khiển ảnh cho component dùng chung controlVms (deviceType = LCS) -->
        <template #controlTab>
            <div class="content-text">
                <div class="lcs-control-wrapper">
                    <controlVms ref="controlRef" mode="manual" :device-type="VmsDeviceType.Lcs" @submit-success="onControlSuccess" />
                </div>
            </div>
        </template>

        <!-- Footer chỉ hiện ở tab control -->
        <template #footer>
            <!-- Đặt lại mặc định: đẩy sang trái, tách khỏi nhóm Hủy/Xác nhận bên phải -->
            <el-button class="btn-default-content" :icon="RefreshLeft" style="margin-right: auto" @click="applyDefaultContent">{{ $t('lz.button.vms.defaultContent') }}</el-button>
            <el-button @click="cancel">{{ $t('lz.button.base.cancel') }}</el-button>
            <el-button type="primary" class="btn-confirm" :loading="isSubmitting" @click="submit">
                {{ $t('lz.button.base.confirm') }}<span v-if="selectedCount" class="confirm-count">{{ selectedCount }}</span>
            </el-button>
        </template>
    </InfoPanel>
</template>

<script setup lang="ts" name="lcsInfoModal">
import { reactive, computed, defineAsyncComponent, ref, nextTick, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { BaseConfigTypeEnum, VmsActionType, VmsDeviceType } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import { useMapStore } from '/@/stores/its/mapStore';
import { ElMessage } from 'element-plus';
import { RefreshLeft } from '@element-plus/icons-vue';
import { getAPI } from '/@/utils/axios-utils';
import { VmsCurrentApi, EquipmentTelemetryApi } from '/@/api-services/vms';
import { auth } from '/@/utils/authFunction';
import InfoPanel from '/@/components/bigScreen/floatingModal/InfoPanel.vue';
// Component điều khiển thiết bị dùng chung (control + preview + chọn thiết bị + check ưu tiên)
import controlVms from '/@/components/vms/controlVms/index.vue';
// Tab trạng thái thiết bị dùng chung (mock UI)
import deviceStatusTab from '/@/components/vms/deviceStatus/index.vue';
// Hook NATS dùng chung VMS/LCS
import { useTransporterVms, getVmsEquipmentId, normalizeVmsRecord, applyVmsCurrentToList } from '/@/transporter/hooks/useTransporterVms';

const { t: $t } = useI18n();
const mapStore = useMapStore();
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
const emits = defineEmits(['activated', 'closed', 'closeOthers', 'closeAll', 'controlTabChange']);

// ref tới component điều khiển dùng chung
const controlRef = ref();
// Số thiết bị đang chọn trong form điều khiển (hiện trên nút Xác nhận)
const selectedCount = computed(() => controlRef.value?.selectedCount ?? 0);
const isSubmitting = ref(false);

const state = reactive({
    activeTab: 'infoTab',
    data: null as any,
    itemId: '' as any,
    vmsCurrent: {} as any, // nội dung LCS đang hiển thị (lấy từ bảng VmsCurrent)
    realImageUrl: '' as string, // hình thực tế đọc từ thiết bị (base64)
    realImageLoading: false as boolean,
});

// Đọc hình thực tế đang hiển thị trên thiết bị LCS để đối chiếu với DB.
const fetchRealImage = async () => {
    if (!state.itemId) return;
    state.realImageLoading = true;
    try {
        const res = await getAPI(EquipmentTelemetryApi).apiVmsEquipmenttelemetryReadimagePost({ equipmentId: String(state.itemId) });
        const result = res.data?.result;
        if (result?.succeeded && result.base64) {
            state.realImageUrl = result.base64.startsWith('data:') ? result.base64 : `data:${result.contentType || 'image/bmp'};base64,${result.base64}`;
        } else {
            state.realImageUrl = '';
            ElMessage.warning(result?.message || $t('lz.message.vms.readDeviceImageFailed'));
        }
    } catch (e) {
        console.error('Error reading real device image:', e);
        ElMessage.error($t('lz.message.vms.readDeviceImageFailed'));
    } finally {
        state.realImageLoading = false;
    }
};

// Ảnh mặc định khi chưa có/không tải được nội dung
const noImageUrl = `${import.meta.env.BASE_URL}Upload/noimage.jpg`;
// URL ảnh hiện tại của LCS (fallback noimage). Đổi state.vmsCurrent -> ảnh tự cập nhật.
const lcsImageUrl = computed(() => (state.vmsCurrent?.url ? `${mapStore.baseUrl}/${state.vmsCurrent.url}` : noImageUrl));

// Nghe NATS: chỉ xử lý tin của ĐÚNG thiết bị đang mở (equipmentIds).
// Mỗi dialog là 1 instance riêng nên khi mở nhiều dialog, mỗi cái chỉ đổi ảnh của mình.
useTransporterVms((payload) => {
    // Guard: data chưa được nạp (chưa gọi openInfo) thì bỏ qua
    if (!state.data) return;
    // Upsert vào currentList -> priority check + default content luôn dùng dữ liệu mới
    state.data.currentList = applyVmsCurrentToList(state.data.currentList ?? [], payload);

    // lcsInfo luôn ở mode manual (không có defineProps trong component này) —
    // chỉ cần kiểm tra đúng thiết bị đang mở để đổi preview ngay không cần reload.
    if (getVmsEquipmentId(payload.data) === String(state.itemId)) {
        state.vmsCurrent = { ...state.vmsCurrent, ...normalizeVmsRecord(payload.data) };
    }
});

// Quyền điều khiển thiết bị trên bản đồ (dùng chung VMS/LCS). Prefix theo tên định danh menu bản đồ tmsTrafficInfo.
// Tài khoản KHÔNG có quyền này chỉ được xem, không thấy tab "Điều khiển".
const DEVICE_CONTROL_AUTH = 'tmsTrafficInfo:control';
const canControl = computed(() => auth(DEVICE_CONTROL_AUTH));

// Tab "Điều khiển" chỉ hiển thị khi tài khoản có quyền điều khiển.
const tabs = computed(() => {
    const list = [
        { label: $t('lz.label.base.info'), name: 'infoTab' },
        { label: $t('lz.label.base.status'), name: 'statusTab' },
    ];
    if (canControl.value) list.push({ label: $t('lz.label.base.control'), name: 'controlTab' });
    return list;
});

// Báo khung cha (equipmentModal) nới / trả chiều cao khi vào / rời tab "Điều khiển" để thấy danh sách thiết bị.
watch(() => state.activeTab, (tab) => emits('controlTabChange', tab === 'controlTab'));

const deviceLocation = computed(() => `Km ${state.data?.kmNumber ?? ''} + ${state.data?.metNumber ?? ''}`);

// Tô màu header theo trạng thái thiết bị (active/inactive/fault)
const statusVariant = computed(() => getConfigItemByCode(BaseConfigTypeEnum.TmsEquipmentState, state.data?.equipmentState)?.value || '');

// Nạp nội dung LCS hiện tại (từ bảng VmsCurrent) để hiển thị ảnh ở tab thông tin.
const loadCurrentData = async () => {
    const res = await getAPI(VmsCurrentApi).apiVmsVmscurrentListGet();
    state.vmsCurrent = res.data.result?.find((x: any) => x.equipmentId == state.itemId) ?? {};
};

// Mở panel: nạp thông tin + uỷ quyền tab control cho controlVms (tab dùng v-show nên component luôn mounted).
const openInfo = async (data: any) => {
    state.data = data;
    state.itemId = data?.id ?? data?.itemId;
    state.activeTab = 'infoTab';
    state.realImageUrl = ''; // reset hình thực tế của thiết bị trước đó

    // Đảm bảo có đường dẫn file server để ghép URL ảnh
    if (!mapStore.baseUrl) {
        try {
            await mapStore.fetchSystemSysconfigFileserverGet();
        } catch (e) {
            console.error('Error fetching file server:', e);
        }
    }
    await loadCurrentData();

    // Tab control: chỉ khởi tạo khi có quyền điều khiển (không quyền -> tab bị ẩn, không cần mount).
    if (canControl.value) {
        await nextTick();
        controlRef.value?.open({ itemId: state.itemId, actionType: VmsActionType.Image, vmsData: data });
    }
};

// Bấm Xác nhận: gọi submit của controlVms. Việc đóng panel do controlVms phát 'submit-success' đảm nhiệm
// (không đóng ngay để tránh huỷ modal cảnh báo độ ưu tiên).
const submit = async () => {
    if (isSubmitting.value) return;
    isSubmitting.value = true;
    try {
        await controlRef.value?.submit();
    } catch (error) {
        console.error('Error submitting LCS control:', error);
    } finally {
        isSubmitting.value = false;
    }
};

// Gọi component con áp dụng nội dung mặc định (tab control)

const applyDefaultContent = () => controlRef.value?.applyDefaultContent();

// Đóng panel
const cancel = () => emits('closed');

// Component con submit thành công -> đóng panel
const onControlSuccess = () => cancel();

defineExpose({ openInfo });
</script>

<style scoped lang="scss">
@import '/@/theme/its/trafficInfo.scss';

.content-text {
    width: 100%;
    white-space: normal;
    word-wrap: break-word;
}

.lcs-control-wrapper {
    width: 100%;
    /* .modal-content có padding 10px: khung nội dung ghim sát mép trên vùng cuộn */
    --vc-sticky-top: -10px;
}

@import '/@/components/vms/controlVms/footerButtons.scss';

/* Tab header (Thông tin / Trạng thái / Điều khiển): chữ trắng cho nổi - chỉ áp cho panel này.
   Theme chung (trafficInfo.scss) đặt màu bằng !important nên thêm .el-tabs__nav để độ ưu tiên cao hơn. */
:deep(.header-tabs .el-tabs__nav .el-tabs__item) {
    color: rgba(255, 255, 255, 0.85) !important;
}
:deep(.header-tabs .el-tabs__nav .el-tabs__item:hover),
:deep(.header-tabs .el-tabs__nav .el-tabs__item.is-active) {
    color: #ffffff !important;
}


/* Nút lấy hình thực tế từ thiết bị */
.real-image-action {
    display: flex;
    justify-content: center;
}

/* Khung ảnh preview LCS */
.lcs-preview {
    display: flex;
    justify-content: center;
    padding: 4px;
    border-radius: 8px;
    background: var(--el-fill-color-light);
    border: 1px solid var(--el-border-color-light);

    .lcs-preview-img {
        width: 100%;
        max-width: 250px;
        max-height: 120px;
        object-fit: contain;
        border-radius: 6px;
        cursor: zoom-in;
    }
}
</style>
