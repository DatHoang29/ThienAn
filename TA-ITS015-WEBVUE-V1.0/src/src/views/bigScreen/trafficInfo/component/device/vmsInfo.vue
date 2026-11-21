<template>
    <InfoPanel
        v-model:active-tab="state.activeTab"
        :tabs="tabs"
        :device-name="state.vmsData?.name"
        :device-location="deviceLocation"
        :status-variant="statusVariant"
        :show-footer="state.activeTab === 'control'"
        @closed="emits('closed')"
        @close-others="emits('closeOthers')"
        @close-all="emits('closeAll')"
    >
        <!-- Info tab -->
        <template #info>
            <div class="chart-container">
                <div class="content-text">
                    <div class="border-content-info">
                        <h3 class="section-title">{{ $t('lz.label.vms.deviceInformation') }}</h3>
                        <div class="info-grid">
                            <div class="info-item">
                                <span class="info-k">{{ $t('lz.entity.base.code') }}</span>
                                <span class="info-v">{{ state.vmsData?.code || '-' }}</span>
                            </div>
                            <div class="info-item">
                                <span class="info-k">{{ $t('lz.entity.base.name') }}</span>
                                <span class="info-v">{{ state.vmsData?.name || '-' }}</span>
                            </div>
                            <div class="info-item">
                                <span class="info-k">{{ $t('lz.entity.vms.ip') }}</span>
                                <span class="info-v">{{ state.vmsData?.ip || '-' }}</span>
                            </div>
                            <div class="info-item">
                                <span class="info-k">{{ $t('lz.entity.vms.port') }}</span>
                                <span class="info-v">{{ state.vmsData?.port || '-' }}</span>
                            </div>
                            <div class="info-item info-item--full">
                                <span class="info-k">{{ $t('lz.entity.tms.location') }}</span>
                                <span class="info-v">Km{{ state.vmsData?.kmNumber ?? '-' }}+{{ state.vmsData?.metNumber ?? '-' }}</span>
                            </div>
                        </div>
                    </div>

                    <div class="border-content-info mt10">
                        <h3 class="section-title">{{ $t('lz.label.base.preview') }}</h3>
                        <div class="vms-preview">
                            <!-- Bấm vào ảnh để xem preview lớn; ảnh lỗi/không có -> noimage.jpg -->
                            <el-image
                                :src="vmsImageUrl"
                                :preview-src-list="[vmsImageUrl]"
                                :preview-teleported="true"
                                :zoom-rate="1.2"
                                :max-scale="7"
                                :min-scale="0.2"
                                fit="contain"
                                hide-on-click-modal
                                class="vms-preview-img"
                            >
                                <template #error>
                                    <img :src="noImageUrl" class="vms-preview-img" :alt="$t('lz.label.vms.templateImage')" />
                                </template>
                            </el-image>
                        </div>
                    </div>

                    <!-- Lấy hình thực tế đang hiển thị trên thiết bị (đối chiếu với nội dung hiện tại) -->
                    <div class="border-content-info mt10">
                        <div class="real-image-action">
                            <el-button type="primary" :loading="state.realImageLoading" @click="fetchRealImage">
                                {{ $t('lz.button.vms.readDeviceImage') }}
                            </el-button>
                        </div>
                        <div v-if="state.realImageUrl" class="vms-preview mt10">
                            <el-image
                                :src="state.realImageUrl"
                                :preview-src-list="[state.realImageUrl]"
                                :preview-teleported="true"
                                :zoom-rate="1.2"
                                :max-scale="7"
                                :min-scale="0.2"
                                fit="contain"
                                hide-on-click-modal
                                class="vms-preview-img"
                            />
                        </div>
                    </div>
                </div>
            </div>
        </template>

        <!-- Status tab: giao diện trạng thái thiết bị (mock, bổ sung tính năng thật sau) -->
        <template #status>
            <deviceStatusTab :item-id="state.itemId" :device-name="state.vmsData?.name" device-type="vms" />
        </template>

        <!-- Control tab: uỷ quyền toàn bộ điều khiển cho component dùng chung controlVms -->
        <template #control>
            <div class="chart-container">
                <div class="content-text">
                    <div class="vms-control-wrapper">
                        <controlVms ref="controlRef" mode="manual" @submit-success="onControlSuccess" />
                    </div>
                </div>
            </div>
        </template>

        <!-- Footer chỉ hiện ở tab control -->
        <template #footer>
            <!-- Đặt lại mặc định: đẩy sang trái, tách khỏi nhóm Hủy/Xác nhận bên phải -->
            <el-button class="btn-default-content" :icon="RefreshLeft" style="margin-right: auto" @click="applyDefaultContent">{{ $t('lz.button.vms.defaultContent') }}</el-button>
            <el-button @click="cancel">{{ $t('lz.button.base.cancel') }}</el-button>
            <el-button type="primary" class="btn-confirm" @click="handleSubmit">
                {{ $t('lz.button.base.confirm') }}<span v-if="selectedCount" class="confirm-count">{{ selectedCount }}</span>
            </el-button>
        </template>
    </InfoPanel>
</template>

<script setup lang="ts" name="vmsInfoModal">
import { reactive, ref, computed, onMounted, nextTick, watch } from 'vue';
import { ElMessage } from 'element-plus';
import { RefreshLeft } from '@element-plus/icons-vue';
import { getAPI } from '/@/utils/axios-utils';
import { TmsEquipmentApi } from '/@/api-services/tms';
import { VmsCurrentApi, EquipmentTelemetryApi } from '/@/api-services/vms/api';
import { TmsEquipmentType } from '/@/types/enums/tmsEnum';
import { SysConfigTypeCodeEnum } from '/@/types/enums/baseEnum';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useI18n } from 'vue-i18n';
import { useItsMediaServerStore } from '/@/stores/its/itsMediaServer';
import { useMapStore } from '/@/stores/its/mapStore';
import { useBaseConfig } from '/@/stores/baseConfig';
import { auth } from '/@/utils/authFunction';

// Component điều khiển VMS dùng chung (control + preview + chọn thiết bị)
import controlVms from '/@/components/vms/controlVms/index.vue';
// Tab trạng thái thiết bị dùng chung (mock UI)
import deviceStatusTab from '/@/components/vms/deviceStatus/index.vue';
// Panel dùng chung (header + tabs + footer)
import InfoPanel from '/@/components/bigScreen/floatingModal/InfoPanel.vue';
import { useTransporterVms, getVmsEquipmentId, normalizeVmsRecord } from '/@/transporter/hooks/useTransporterVms';

useTransporterVms((payload) => {
    if (getVmsEquipmentId(payload.data) === String(state.itemId)) {
        state.vmsCurrent = { ...state.vmsCurrent, ...normalizeVmsRecord(payload.data) };
    }
});

const { t: $t } = useI18n();
const mapStore = useMapStore();
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;

// ref tới component điều khiển dùng chung
const controlRef = ref();
// Số thiết bị đang chọn trong form điều khiển (hiện trên nút Xác nhận)
const selectedCount = computed(() => controlRef.value?.selectedCount ?? 0);
const modalId = ref('');
const emits = defineEmits(['activated', 'closed', 'closeOthers', 'closeAll', 'controlTabChange']);

const state = reactive<any>({
    activeTab: 'info',
    data: {} as any,
    itemId: '' as any,
    vmsData: {} as any,
    vmsCurrent: {} as any,
    baseUrl: '' as string,
    directionList: [] as any,
    listVmsSelected: [] as any,
    listData: {
        equipmentList: [] as any, // danh sách thiết bị VMS
        equipmentListGroupByDirection: [] as any, // nhóm theo hướng
        currentList: [] as any, // nội dung hiện tại của VMS
    },
    realImageUrl: '' as string, // hình thực tế đọc từ thiết bị (base64)
    realImageLoading: false as boolean,
});

// Đọc hình thực tế đang hiển thị trên thiết bị (gọi trực tiếp thiết bị qua tích hợp) để đối chiếu với DB.
const fetchRealImage = async () => {
    if (!state.itemId) return;
    state.realImageLoading = true;
    try {
        const res = await getAPI(EquipmentTelemetryApi).apiVmsEquipmenttelemetryReadimagePost({ equipmentId: String(state.itemId) });
        const result = res.data?.result;
        if (result?.succeeded && result.base64) {
            // BE trả base64 thô -> ghép data URI để hiển thị; nếu đã là data URI thì dùng nguyên.
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

// Quyền điều khiển thiết bị trên bản đồ (dùng chung VMS/LCS). Prefix theo tên định danh menu bản đồ tmsTrafficInfo.
// Tài khoản KHÔNG có quyền này chỉ được xem, không thấy tab "Điều khiển".
const DEVICE_CONTROL_AUTH = 'tmsTrafficInfo:control';
const canControl = computed(() => auth(DEVICE_CONTROL_AUTH));

// Tab "Điều khiển" chỉ hiển thị khi tài khoản có quyền điều khiển.
const tabs = computed(() => {
    const list = [
        { label: $t('lz.label.base.info'), name: 'info' },
        { label: $t('lz.label.base.status'), name: 'status' },
    ];
    if (canControl.value) list.push({ label: $t('lz.label.base.control'), name: 'control' });
    return list;
});

// Báo khung cha (equipmentModal) nới / trả chiều cao khi vào / rời tab "Điều khiển" để thấy danh sách thiết bị.
watch(() => state.activeTab, (tab) => emits('controlTabChange', tab === 'control'));

const deviceLocation = computed(() => `Km${state.vmsData?.kmNumber || '-'}+${state.vmsData?.metNumber || '-'}`);

// Tô màu header theo trạng thái thiết bị (active/inactive/fault)
const statusVariant = computed(() => getConfigItemByCode(BaseConfigTypeEnum.TmsEquipmentState, state.vmsData?.equipmentState)?.value || '');

// Ảnh mặc định khi không có/không tải được nội dung VMS
const noImageUrl = `${import.meta.env.BASE_URL}Upload/noimage.jpg`;
// URL ảnh nội dung hiện tại của VMS (fallback noimage nếu chưa có)
const vmsImageUrl = computed(() => (state.vmsCurrent?.url ? `${mapStore.baseUrl}/${state.vmsCurrent.url}` : noImageUrl));

//============ #SECTION: LOAD DATA (tab info) =============================

// Nạp thông tin cơ bản cho tab info: base url + danh sách thiết bị + thiết bị đang mở.
const loadDataInfo = async () => {
    // Đường dẫn server lấy hình
    // Địa chỉ file server dùng chung (chỉ gọi API 1 lần cho toàn app)
    state.baseUrl = await useItsMediaServerStore().ensureLoaded();

    // Lấy danh sách thiết bị
    await loadEquipmentData();

    if (state.itemId) {
        state.listVmsSelected = [state.itemId];
        state.vmsData = state.listData.equipmentList.find((u: any) => u.id === state.itemId);
    }
};

// Lấy danh sách thiết bị VMS và group theo hướng (sort theo tên trong từng nhóm).
const loadEquipmentData = async () => {
    const response = await getAPI(TmsEquipmentApi).apiTmsTmsequipmentGetequipmenttypebyequipmentidVmstypeGet(TmsEquipmentType.Vms);
    state.listData.equipmentList = response?.data?.result ?? null;

    const groupByDirection = state.listData.equipmentList.reduce((accumulator: any, currentItem: any) => {
        const valueDirection = currentItem['directionId'];
        if (!valueDirection) return accumulator; // thiếu directionId thì bỏ qua
        const nameDirection = state.directionList.find((item: any) => item.code == valueDirection)?.name;
        if (nameDirection && !accumulator[nameDirection]) {
            accumulator[nameDirection] = [];
        }
        accumulator[nameDirection].push(currentItem);
        return accumulator;
    }, {});

    // Sort by name (asc) cho từng nhóm hướng
    const sortKey = 'name';
    const sortDirection = 'asc';
    for (const key in groupByDirection) {
        if (Object.prototype.hasOwnProperty.call(groupByDirection, key) && Array.isArray(groupByDirection[key])) {
            groupByDirection[key].sort((a: any, b: any) => {
                const valueA = a[sortKey].toUpperCase();
                const valueB = b[sortKey].toUpperCase();
                let comparison = 0;
                if (valueA < valueB) comparison = -1;
                else if (valueA > valueB) comparison = 1;
                return sortDirection === 'desc' ? comparison * -1 : comparison;
            });
        }
    }
    state.listData.equipmentListGroupByDirection = groupByDirection;
};

// Lấy nội dung hiện tại của VMS đang mở để hiển thị preview tab info.
const loadCurrentData = async () => {
    const listVmsCurrent = await getAPI(VmsCurrentApi).apiVmsVmscurrentListGet();
    state.listData.currentList = listVmsCurrent.data.result;
    state.vmsCurrent = state.listData.currentList?.find((x: any) => x.equipmentId == state.itemId);
};

//============ #SECTION: DIALOG METHODS =============================

// Mở panel: nạp dữ liệu tab info + uỷ quyền tab control cho controlVms.
const openDialog = async (data: any) => {
    if (data) {
        state.data = data;
        state.itemId = data.itemId;
        state.realImageUrl = ''; // reset hình thực tế của thiết bị trước đó

        // Tab info
        await loadDataInfo();
        await loadCurrentData();

        // Tab control: chỉ khởi tạo khi có quyền điều khiển (không quyền -> tab bị ẩn, không cần mount).
        if (canControl.value) {
            await nextTick();
            controlRef.value?.open({ itemId: data.itemId, vmsData: state.vmsData });
        }
    }
};

const isSubmitting = ref(false); // Trạng thái submit để disable nút xác nhận

// Hàm xử lý submit: gọi submit của component con controlVms, sau đó đóng panel.
const handleSubmit = async () => {
    if (isSubmitting.value) return; // Tránh submit nhiều lần
    isSubmitting.value = true;
    try {
        await controlRef.value?.submit();
        // KHÔNG đóng panel ở đây: việc đóng do controlVms phát 'submit-success' (onControlSuccess) đảm nhiệm.
        // Nếu đóng ngay sẽ huỷ modal cảnh báo độ ưu tiên (destroy-on-close) trước khi người dùng kịp xác nhận.
    } catch (error) {
        console.error('Error submitting control:', error);
    } finally {
        isSubmitting.value = false;
    }
}

// Gọi component con áp dụng nội dung mặc định (tab control)
const applyDefaultContent = () => controlRef.value?.applyDefaultContent(); 

// Đóng panel
const cancel = () => {
    emits('closed', modalId.value);
};

// Component con submit thành công -> đóng panel
const onControlSuccess = () => {
    cancel();
};

onMounted(async () => {
    // Kiểm tra đường dẫn lấy hình
    if (mapStore.baseUrl == '' || mapStore.baseUrl == null || mapStore.baseUrl == undefined) {
        try {
            await mapStore.fetchSystemSysconfigFileserverGet();
        } catch (error) {
            console.error('Error initializing map fetch file server:', error);
        }
    }

    // Danh sách hướng (phục vụ group thiết bị theo hướng)
    state.directionList = getConfigDataByCode(SysConfigTypeCodeEnum.TmsDirectionConfigCode);
});

// Export cho component cha sử dụng
defineExpose({ openDialog, itemId: modalId });
</script>

<style scoped lang="scss">
@import '/@/theme/its/trafficInfo.scss';

.chart-container {
    padding: 0;
    width: 100%;

    .content-text {
        width: 100%;
        white-space: normal;
        word-wrap: break-word;
    }
}

.vms-control-wrapper {
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

/* Khung ảnh preview VMS */
.vms-preview {
    display: flex;
    justify-content: center;
    padding: 4px;
    border-radius: 8px;
    background: color-mix(in srgb, var(--el-color-primary) 4%, #f4f8fc);
    border: 1px solid rgba(20, 40, 70, 0.07);

    .vms-preview-img {
        width: 100%;
        max-width: 340px;
        max-height: 230px;
        border-radius: 6px;
        cursor: zoom-in;
        object-fit: contain;
    }
}
</style>
