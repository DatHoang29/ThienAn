<template>
    <div v-if="state.isProcessing" style="text-align: center; padding-top: 20px;">
        <div v-loading="state.isProcessing" :element-loading-text="$t('lz.label.base.loading')">
        </div>
    </div>

    <!-- KienLT 09/10/2026: trước chỉ ghi tên hướng ⇒ mở nhiều popup dễ nhầm vùng. Ghi rõ khu vực + lý trình + hướng,
         và số là trung bình 1 giờ qua (last1h.avgSpeed của VdsDashboard/ZoneInfo), không phải tức thời. -->
    <div v-if="state.zoneInfo" class="border-content-info speed-zone">
        <h3 class="section-title">{{ $t('lz.label.zoneInfo.avgSpeedLastHour') }}</h3>
        <div class="speed-zone__head">
            <span class="speed-zone__name">{{ state.zoneInfo.name || state.zoneInfo.code }}</span>
            <span class="speed-zone__km">
                Km{{ state.zoneInfo.fromKmNumber ?? 0 }}+{{ state.zoneInfo.fromMetNumber ?? 0 }}
                → Km{{ state.zoneInfo.toKmNumber ?? 0 }}+{{ state.zoneInfo.toMetNumber ?? 0 }}
            </span>
            <el-tag v-if="zoneDirection" size="small" effect="dark" round class="speed-zone__dir">
                {{ $t('lz.entity.tms.direction') }}: {{ zoneDirection.name }}
            </el-tag>
        </div>
        <div class="speed-zone__gauge bg-color--white">
            <gaugeChartComp :zoneInfo="state.zoneInfo" :requestTime="state.requestTime" :directionCode="zoneDirection?.code ?? String(state.zoneInfo.laneId ?? 'zone')" />
        </div>
    </div>
    <div v-else>
        <p>{{ $t('lz.label.base.noData') }}</p>
    </div>
</template>

<script setup>

import { reactive, onMounted, watch, computed } from 'vue';

import { useI18n } from 'vue-i18n';
const { t: $t } = useI18n();

import { useCommonVarsAndFuncs } from '/@/stores/its/commonVarsAndFuncs';
const commonVarsAndFuncs = useCommonVarsAndFuncs();

import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';

const getConfigDataByCode = useBaseConfig().getConfigDataByCode;

import { SysConfigTypeCodeEnum } from '/@/types/enums/baseEnum';

import gaugeChartComp from './charts/gaugeChart.vue';

const state = reactive({
    isProcessing: false,
    requestTime: null,
    zoneInfo: null,
    directionList: []
});

const props = defineProps({
    requestTime: {
        type: Number,
        default: null
    },
    zoneInfo: {
        type: Object,
        default: null
    }
});

onMounted(async() => {
    state.directionList = await commonVarsAndFuncs.getSysConfigDataList(SysConfigTypeCodeEnum.TmsDirectionConfigCode);
    state.directionList = getConfigDataByCode(BaseConfigTypeEnum.TmsDirectionType);
});

/** Hướng của vùng (laneId ↔ mã cấu hình hướng); không khớp ⇒ ẩn nhãn hướng, đồng hồ vẫn hiện. */
const zoneDirection = computed(() => state.directionList?.find(dir => dir.code == state.zoneInfo?.laneId) ?? null);

watch(() => props.requestTime, async() => {
    state.requestTime = props.requestTime;
    state.zoneInfo = props.zoneInfo;
});
</script>
<style scoped lang="scss">
    @import '/@/theme/its/trafficInfo.scss';

    .speed-zone {
        padding: 10px 12px;
    }

    .speed-zone__head {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: 4px 10px;
        margin-bottom: 8px;
    }

    .speed-zone__name {
        color: $text-strong;
        font-size: 15px;
        font-weight: 700;
    }

    .speed-zone__km {
        color: $text-muted;
        font-size: 12px;
        font-variant-numeric: tabular-nums;
    }

    .speed-zone__dir {
        margin-left: auto;
    }

    .speed-zone__gauge {
        border-radius: 6px;
        padding: 6px 0;
    }
</style>
