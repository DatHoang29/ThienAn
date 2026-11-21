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
                            <span class="info-k">{{ $t('lz.entity.base.ipAddress') }}</span>
                            <span class="info-v">{{ state.data?.ip || '-' }}</span>
                        </div>
                        <div class="info-item">
                            <span class="info-k">{{ $t('lz.entity.base.port') }}</span>
                            <span class="info-v">{{ state.data?.port || '-' }}</span>
                        </div>
                    </div>
                </div>
            </div>
        </template>

        <!-- VDS Data -->
        <template #vdsDataTab>
            <el-select v-if="state.rows.length > 1" v-model="state.selectedLane" aria-label="Làn xe">
                <el-option v-for="(row, index) in state.rows" :key="index" :value="index" :label="`Làn ${row.data?.lane || '—'}`" />
            </el-select>
            <el-alert v-if="state.error || state.stale" :title="state.error || 'Dữ liệu đã cũ hoặc chưa có số liệu mới'" type="warning" :closable="false" />
            <div class="content-text">
                <div class="border-content-info mt10">
                    <h3 class="section-title">{{ $t('lz.label.tms.vdsData') }} </h3>
                    <div class="info-grid">
                        <div class="info-item">
                            <span class="info-k">{{ $t('lz.entity.tmsTrafficAnalysis.fromTime') }}</span>
                            <span class="info-v">{{ state.tmsTrafficAnalysis?.fromTime ? useDateFormat(state.tmsTrafficAnalysis.fromTime as Date, DATE_TIME_FORMAT).value : '—' }}</span>
                        </div>
                        <div class="info-item">
                            <span class="info-k">{{ $t('lz.entity.tmsTrafficAnalysis.toTime') }}</span>
                            <span class="info-v">{{ state.tmsTrafficAnalysis?.toTime ? useDateFormat(state.tmsTrafficAnalysis.toTime as Date, DATE_TIME_FORMAT).value : '—' }}</span>
                        </div>
                        <div class="info-item">
                            <span class="info-k">{{ $t('lz.entity.tmsTrafficStatistic.totalVehicleNumber') }}</span>
                            <span class="info-v">{{ state.tmsTrafficAnalysis?.totalVehicle ?? '—' }}</span>
                        </div>
                        <div class="info-item">
                            <span class="info-k">{{ $t('lz.entity.tmsTrafficStatistic.avgSpeed') }}</span>
                            <span class="info-v">{{ state.tmsTrafficAnalysis?.avgSpeed ?? '—' }}</span>
                        </div>
                        <div class="info-item">
                            <span class="info-k">{{ $t('lz.entity.tmsTrafficStatistic.spaceOccupy') }}</span>
                            <span class="info-v">{{ state.tmsTrafficAnalysis?.spaceOccupy == null ? '—' : `${state.tmsTrafficAnalysis.spaceOccupy}%` }}</span>
                        </div>
                        <div class="info-item">
                            <span class="info-k">{{ $t('lz.entity.tmsTrafficAnalysis.timeOccupy') }}</span>
                            <span class="info-v">{{ state.tmsTrafficAnalysis?.timeOccupy == null ? '—' : `${state.tmsTrafficAnalysis.timeOccupy}%` }}</span>
                        </div>
                        <div class="info-item">
                            <span class="info-k">{{ $t('lz.entity.tmsTrafficAnalysis.sVehicleNumber') }}</span>
                            <span class="info-v">{{ state.tmsTrafficAnalysis?.shortVehicle ?? '—' }}</span>
                        </div>
                        <div class="info-item">
                            <span class="info-k">{{ $t('lz.entity.tmsTrafficAnalysis.mVehicleNumber') }}</span>
                            <span class="info-v">{{ state.tmsTrafficAnalysis?.mediumVehicle ?? '—' }}</span>
                        </div>
                        <div class="info-item">
                            <span class="info-k">{{ $t('lz.entity.tmsTrafficAnalysis.lVehicleNumber') }}</span>
                            <span class="info-v">{{ state.tmsTrafficAnalysis?.longVehicle ?? '—' }}</span>
                        </div>
                        <div class="info-item">
                            <!-- <span class="info-k">{{ $t('lz.entity.tmsTrafficAnalysis.oVehicleNumber') }}</span>
                            <span class="info-v">{{ state.tmsTrafficAnalysis?.oVehicleNumber ?? '—' }}</span> -->
                        </div>
                    </div>
                </div>
            </div>
        </template>
    </InfoPanel>
</template>

<script setup lang="ts" name="vdsInfoModal">
import { reactive, watch, computed, onMounted, onActivated, onDeactivated, defineAsyncComponent } from 'vue';
import { useI18n } from 'vue-i18n';
import { SysConfigDataApi } from '/@/api-services/cfgSystem';
import { getAPI } from '/@/utils/axios-utils';
import { TmsEquipmentTypeApi } from '/@/api-services/tms';
import { VdsTrafficStatisticApi, type TrafficStatisticOutput, type StatisticSnapshotRowOutput } from '/@/api-services/vds';
import { useTransporterVdsCount } from '/@/transporter/hooks/useTransporterVdsCount';
import { useThrottleFn } from '@vueuse/core';
import { onUnmounted } from 'vue';
import { useDateFormat } from '@vueuse/core';
import { DATE_TIME_FORMAT } from '/@/types/formats';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import InfoPanel from '/@/components/bigScreen/floatingModal/InfoPanel.vue';

const { t: $t } = useI18n();
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));

const emits = defineEmits(['activated', 'closed', 'closeOthers', 'closeAll']);

const state = reactive({
    activeTab: 'infoTab',
    data: null as any,
    equipmentTypeList: [] as any,
    equipStateList: [] as any,
    error: '',
    stale: false,
    rows: [] as StatisticSnapshotRowOutput[],
    selectedLane: 0,
    tmsTrafficAnalysis: {} as TrafficStatisticOutput,
});

const tabs = computed(() => [
    { label: $t('lz.label.base.info'), name: 'infoTab' },
    { label: $t('lz.label.tms.vdsData'), name: 'vdsDataTab' },
]);

const deviceLocation = computed(() => `Km ${state.data?.kmNumber ?? ''} + ${state.data?.metNumber ?? ''}`);

// Tô màu header theo trạng thái thiết bị (active/inactive/fault) - map qua value của config equipment_state
const statusVariant = computed(() => state.equipStateList.find((item: any) => item.code == state.data?.equipmentState)?.value || '');

let requestId = 0;
let disposed = false;
let active = true;
let poll: ReturnType<typeof setInterval> | undefined;
const refreshStatistic = async () => {
    if (disposed || !active || state.activeTab !== 'vdsDataTab' || !state.data?.id) return;
    const current = ++requestId;
    try {
        const toDate = new Date();
        const res = await getAPI(VdsTrafficStatisticApi).apiVdsVdstrafficstatisticSnapshotPost({
            equipmentIds: [state.data.id], fromDate: new Date(toDate.getTime() - 86400000), toDate,
        });
        if (disposed || !active || current !== requestId) return;
        state.rows = (res.data.result?.rows ?? []).filter(row => row.data?.equipmentId === state.data.id);
        const previousLane = state.tmsTrafficAnalysis.lane;
        state.selectedLane = Math.max(0, state.rows.findIndex(row => row.data?.lane === previousLane));
        const row = state.rows[state.selectedLane];
        state.tmsTrafficAnalysis = row?.data ?? {};
        state.stale = row?.stale ?? true;
        state.error = '';
    } catch (error) {
        if (!disposed && active && current === requestId) state.error = 'Không tải được thống kê thiết bị VDS';
        console.error('vdsInfo.vue: refreshStatistic', error);
    }
};
watch(() => state.selectedLane, (index) => {
    state.tmsTrafficAnalysis = state.rows[index]?.data ?? {};
    state.stale = state.rows[index]?.stale ?? true;
});
watch(() => [state.activeTab, state.data?.id], () => { void refreshStatistic(); });
const refresh = useThrottleFn(() => refreshStatistic(), 1500);
// Luồng count đã được bigscreen mở và chia sẻ; tránh tạo SSE thứ sáu đến cùng host API.
useTransporterVdsCount(refresh);
const startPoll = () => { if (!poll) poll = setInterval(() => { void refreshStatistic(); }, 60000); };
const stopPoll = () => { if (poll) clearInterval(poll); poll = undefined; };
onActivated(() => { if (active) return; active = true; void refreshStatistic(); startPoll(); });
onDeactivated(() => { active = false; requestId++; stopPoll(); });
onUnmounted(() => { disposed = true; active = false; requestId++; stopPoll(); });

onMounted(async () => {
    startPoll();
    const res = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet('equipment_state');
    state.equipStateList = res.data.result;

    const res2 = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet();
    state.equipmentTypeList = res2.data.result;
});

// Dialog methods
const openInfo = (data: any) => {
    requestId++;
    state.tmsTrafficAnalysis = {};
    state.rows = [];
    state.error = '';
    state.data = data;
    state.activeTab = 'infoTab';
};

defineExpose({ openInfo });
</script>

<style scoped lang="scss">
@import '/@/theme/its/trafficInfo.scss';
</style>
