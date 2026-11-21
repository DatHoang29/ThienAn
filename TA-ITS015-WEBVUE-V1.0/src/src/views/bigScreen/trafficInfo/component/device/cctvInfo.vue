<template>
    <InfoPanel
        v-model:active-tab="state.activeTab"
        :tabs="tabs"
        :device-name="state.data?.code"
        :device-location="deviceLocation"
        :status-variant="statusVariant"
        @closed="emits('closed')"
        @close-others="emits('closeOthers')"
        @close-all="emits('closeAll')"
    >
        <!-- Info -->
        <template #infoTab>
            <div class="content-text">
                <div class="border-content-info">
                    <h3 class="section-title"> {{ $t('lz.label.vms.deviceSumary') }} </h3>
                    <div class="background-summary-device">
                        <span :style="{ color: '#000' }"> {{ $t('lz.label.base.status') }}: </span>
                        <div class="status-indicator">
                            <TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsEquipmentState, state.data?.equipmentState)" />
                        </div>
                    </div>
                </div>

                <div class="border-content-info mt10">
                    <h3 class="section-title">{{ $t('lz.label.vms.deviceInformation') }} </h3>
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
                            <span class="info-v">{{ state.equipmentTypeList.find((item: any) => item.id == state.data?.equipmentTypeId)?.name || '-' }}</span>
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
            </div>
        </template>

        <!-- Live Cam: ưu tiên phát qua hệ thống CCTV (PTZ + playback), tự rơi về RTSP nếu CCTV không hỗ trợ IP -->
        <template #liveCamTab>
            <div class="live-cam-tab border-info-cus">
                <CctvCameraView ref="liveCamCompRef" :equipment="state.data" :active="state.activeTab === 'liveCamTab'" />
            </div>
        </template>
    </InfoPanel>
</template>

<script setup lang="ts" name="cctvInfoModal">
import { reactive, ref, watch, computed, onMounted, defineAsyncComponent } from 'vue';
import { useI18n } from 'vue-i18n';
import CctvCameraView from '/@/components/cctv/CctvCameraView.vue';
import { SysConfigDataApi } from '/@/api-services/cfgSystem';
import { getAPI } from '/@/utils/axios-utils';
import { TmsEquipmentTypeApi } from '/@/api-services/tms';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import InfoPanel from '/@/components/bigScreen/floatingModal/InfoPanel.vue';

const { t: $t } = useI18n();
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
const liveCamCompRef = ref<InstanceType<typeof CctvCameraView>>();

const emits = defineEmits(['activated', 'closed', 'closeOthers', 'closeAll']);

const state = reactive({
    activeTab: 'liveCamTab',
    data: null as any,
    isCamPlaying: false,
    equipmentTypeList: [] as any,
    equipStateList: [] as any,
});

const tabs = computed(() => [
    { label: $t('lz.label.base.info'), name: 'infoTab' },
    { label: $t('lz.entity.tms.liveCamera'), name: 'liveCamTab' },
]);

const deviceLocation = computed(() => `Km ${state.data?.kmNumber ?? ''} + ${state.data?.metNumber ?? ''}`);

// Tô màu header theo trạng thái thiết bị (active/inactive/fault) - map qua value của config equipment_state
const statusVariant = computed(() => getConfigItemByCode(BaseConfigTypeEnum.TmsEquipmentState, state.data?.equipmentState)?.value || '');

// Chuyển tab: chỉ phát khi đang ở tab camera (CctvCameraView tự dọn kết nối khi bị dừng),
// tránh treo WebRTC/RTSP khi người dùng đứng ở tab thông tin.
watch(
    () => state.activeTab,
    (newVal) => {
        if (newVal === 'liveCamTab') {
            liveCamCompRef.value?.start();
            state.isCamPlaying = true;
        } else if (state.isCamPlaying) {
            liveCamCompRef.value?.stop();
            state.isCamPlaying = false;
        }
    }
);

onMounted(async () => {
    const res = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet('equipment_state');
    state.equipStateList = res.data.result;

    const res2 = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet();
    state.equipmentTypeList = res2.data.result;
});

// Dialog methods
const openInfo = (data: any) => {
    state.data = data;
    state.activeTab = 'liveCamTab';
    state.isCamPlaying = true;
};

defineExpose({ openInfo });
</script>

<style scoped lang="scss">
@import '/@/theme/its/trafficInfo.scss';
</style>
