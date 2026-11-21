<template>
    <InfoPanel
        v-model:active-tab="state.activeTab"
        :tabs="tabs"
        :device-name="state.data?.name"
        :device-location="state.data?.remark"
        @closed="emits('closed')"
        @close-others="emits('closeOthers')"
        @close-all="emits('closeAll')"
    >
        <!-- Info -->
        <template #infoTab>
            <div class="content-text">
                <!-- <div class="border-traffic-info">
                    <h3 class="section-title">{{ $t('lz.label.base.summary') }}</h3>
                    <div class="background-summary-device">
                        <span>{{ $t('lz.label.base.status') }}:</span>
                        <div class="status-indicator">
                            <TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, state.data?.status)" />
                        </div>
                    </div>
                </div> -->

                <div class="border-traffic-info">
                    <h3 class="section-title">{{ $t('lz.label.base.info') }}</h3>
                    <div class="info-grid">
                        <div class="info-item">
                            <span class="info-k">{{ $t('lz.entity.base.code') }}</span>
                            <span class="info-v">{{ state.data?.code || '-' }}</span>
                        </div>
                        <div class="info-item">
                            <span class="info-k">{{ $t('lz.entity.base.name') }}</span>
                            <span class="info-v">{{ state.data?.name || '-' }}</span>
                        </div>
                        <div class="info-item info-item--full">
                            <span class="info-k">{{ $t('lz.label.base.location') }}</span>
                            <span class="info-v">{{ state.data?.remark || '-' }}</span>
                        </div>
                        <div class="info-item info-item--full">
                            <span class="info-k">{{ $t('lz.entity.base.desc') }}</span>
                            <span class="info-v">{{ state.data?.info || '-' }}</span>
                        </div>
                    </div>
                </div>
            </div>
        </template>

        <!-- Statistic -->
        <template #statisticTab>
            <div class="lane-statistics">
                <div v-for="lane in state.statistics" :key="lane.laneNo" class="border-traffic-info">
                    <h3 class="lane-title">Làn {{ lane.laneNo }}</h3>
                    <table class="stats-table">
                        <tr>
                            <th>{{ $t('lz.entity.tmsVehicleRegistration.vehicleType') }}</th>
                            <th>{{ $t('lz.label.base.count') }}</th>
                        </tr>
                        <tr v-for="row in lane.rows" :key="row.code">
                            <td>{{ row.name }}</td>
                            <td>{{ row.count }}</td>
                        </tr>
                        <tr class="stats-total-row">
                            <td>{{ $t('lz.label.base.total') }}</td>
                            <td>{{ lane.total }}</td>
                        </tr>
                    </table>
                </div>
            </div>
        </template>
    </InfoPanel>
</template>

<script setup lang="ts" name="tollInfoModal">
import { reactive, computed, onMounted, defineAsyncComponent } from 'vue';
import { useI18n } from 'vue-i18n';
import { useCommonVarsAndFuncs } from '/@/stores/its/commonVarsAndFuncs';
import { SysConfigTypeCodeEnum } from '/@/types/enums/baseEnum';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import InfoPanel from '/@/components/bigScreen/floatingModal/InfoPanel.vue';

const { t: $t } = useI18n();
const commonVarsAndFuncs = useCommonVarsAndFuncs();
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));

const emits = defineEmits(['activated', 'closed', 'closeOthers', 'closeAll']);

// ─────────────────────────────────────────────────────────────────────────────
// CẤU HÌNH DỮ LIỆU MẪU (có thể tùy chỉnh)
// Trạm thu phí chưa có dữ liệu thực tế -> sinh dữ liệu mẫu để hiển thị.
// Chỉ cần chỉnh các thông số dưới đây là thay đổi được cách sinh số ngẫu nhiên.
// ─────────────────────────────────────────────────────────────────────────────
const SAMPLE_CONFIG = {
    laneCount: 4, // Số làn thu phí hiển thị
    vehicleTypeCount: 5, // Số loại xe mẫu (Xe loại 1 .. Xe loại 5)
    baseCount: 120, // Lưu lượng cơ sở của loại NHIỀU nhất (Xe loại 1)
    stepRatio: 0.65, // Hệ số giảm dần: mỗi loại sau ≈ 65% loại trước -> loại 5 ít nhất
    jitter: 0.25, // Biên độ dao động ngẫu nhiên quanh giá trị cơ sở (±25%)
};

const state = reactive({
    activeTab: 'infoTab',
    data: null as any,
    vehicleTypeList: [] as any,
    statistics: [] as Array<{ laneNo: number; total: number; rows: Array<{ code: string; name: string; count: number }> }>,
});

const tabs = computed(() => [
    { label: $t('lz.label.base.info'), name: 'infoTab' },
    { label: $t('lz.label.base.statistic'), name: 'statisticTab' },
]);

onMounted(async () => {
    state.vehicleTypeList = await commonVarsAndFuncs.getSysConfigDataList(SysConfigTypeCodeEnum.TmsVehicleTypeConfigCode);
});

// Danh sách loại xe dùng để thống kê: ưu tiên cấu hình thực tế, nếu chưa có thì dùng mẫu "Xe loại 1..N".
const getVehicleTypesForStats = () => {
    if (Array.isArray(state.vehicleTypeList) && state.vehicleTypeList.length) {
        return state.vehicleTypeList.map((t: any) => ({ code: String(t.code), name: t.name }));
    }
    return Array.from({ length: SAMPLE_CONFIG.vehicleTypeCount }, (_, i) => ({
        code: `sample_${i + 1}`,
        name: `Xe loại ${i + 1}`,
    }));
};

// Sinh lưu lượng ngẫu nhiên theo THỨ HẠNG loại xe:
// rankIndex = 0 (Xe loại 1) nhiều nhất, càng về sau càng ít dần (giảm theo stepRatio).
const randomCountByRank = (rankIndex: number) => {
    const base = SAMPLE_CONFIG.baseCount * Math.pow(SAMPLE_CONFIG.stepRatio, rankIndex);
    const delta = base * SAMPLE_CONFIG.jitter * (Math.random() * 2 - 1); // dao động ±jitter
    return Math.max(0, Math.round(base + delta));
};

// Sinh toàn bộ bảng thống kê mẫu (ổn định giữa các lần render, chỉ đổi khi gọi lại).
const buildSampleStatistics = () => {
    const vehicleTypes = getVehicleTypesForStats();
    state.statistics = Array.from({ length: SAMPLE_CONFIG.laneCount }, (_, laneIdx) => {
        const rows = vehicleTypes.map((type, rankIdx) => ({
            code: type.code,
            name: type.name,
            count: randomCountByRank(rankIdx),
        }));
        const total = rows.reduce((sum, r) => sum + r.count, 0);
        return { laneNo: laneIdx + 1, total, rows };
    });
};

// Dialog methods
const openInfo = (data: any) => {
    state.data = data;
    state.activeTab = 'infoTab';
    buildSampleStatistics(); // Sinh lại dữ liệu mẫu mỗi lần mở panel
};

defineExpose({ openInfo });
</script>

<style scoped lang="scss">
@import '/@/theme/its/trafficInfo.scss';

/* Dòng tổng cộng của từng làn: làm nổi bật so với các dòng loại xe */
.stats-total-row {
    font-weight: 700;
    td {
        color: var(--el-color-primary);
        border-top: 1px solid color-mix(in srgb, var(--el-color-primary) 40%, transparent);
    }
}
</style>
