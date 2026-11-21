<!--
  Tab "Trạng thái" dùng chung cho form điều khiển thiết bị VMS / LCS.
  Dữ liệu lấy từ bảng lịch sử telemetry (VmsEquipmentTelemetry) qua API:
   - Hiện tại: bản ghi mới nhất (dòng đầu tiên), hiển thị các thông số đọc được đã gom nhóm.
   - Lịch sử ghi nhận: các bản ghi trước đó, cho chọn số dòng muốn xem.
  Cột Config của mỗi bản ghi là JSON gom nhóm: { environment:{...}, display:{...}, cabinet:{...} }.
-->
<template>
    <div class="chart-container" v-loading="state.loading">
        <div class="content-text">
            <!-- Thanh công cụ: xem lịch sử chi tiết (chuyển sang trang quản lý) + làm mới -->
            <div class="status-toolbar">
                <el-button size="small" type="primary" link :icon="Tickets" @click="goToHistory">{{ $t('lz.button.vmsDeviceStatus.viewHistory') }}</el-button>
                <el-button size="small" :icon="Refresh" @click="fetchData">{{ $t('lz.button.base.refresh') }}</el-button>
            </div>

            <!-- ===== Hiện tại (bản ghi mới nhất) ===== -->
            <div class="border-content-info">
                <h3 class="section-title">
                    {{ $t('lz.label.vmsDeviceStatus.current') }}
                    <span v-if="state.current" class="current-meta">
                        <el-tag :type="state.current.succeeded ? 'success' : 'info'" size="small" effect="dark">
                            {{ state.current.succeeded ? $t('lz.label.vmsDeviceStatus.online') : $t('lz.label.vmsDeviceStatus.offline') }}
                        </el-tag>
                        <span class="update-time" v-if="state.current.collectedTime">({{ formatTime(state.current.collectedTime) }})</span>
                    </span>
                </h3>

                <div v-if="!state.current" class="no-data">{{ $t('lz.label.base.noData') }}</div>
                <div v-else class="info-grid">
                    <div v-for="meta in visibleCurrentFields" :key="meta.key" class="info-item">
                        <span class="info-k">{{ $t(meta.labelKey) }}</span>
                        <span class="info-v">
                            <el-tag v-if="meta.type === 'door'" :type="isTruthy(currentStats[meta.key]) ? 'danger' : 'success'" size="small" effect="dark">
                                {{ isTruthy(currentStats[meta.key]) ? $t('lz.label.vmsDeviceStatus.doorOpen') : $t('lz.label.vmsDeviceStatus.doorClosed') }}
                            </el-tag>
                            <el-tag v-else-if="meta.type === 'power'" :type="isTruthy(currentStats[meta.key]) ? 'success' : 'info'" size="small" effect="dark">
                                {{ isTruthy(currentStats[meta.key]) ? $t('lz.label.vmsDeviceStatus.online') : $t('lz.label.vmsDeviceStatus.offline') }}
                            </el-tag>
                            <template v-else>{{ formatValue(currentStats[meta.key], meta.unit) }}</template>
                        </span>
                    </div>
                </div>
            </div>

            <!-- ===== Lịch sử ghi nhận ===== -->
            <div class="border-content-info mt10">
                <h3 class="section-title">{{ $t('lz.label.vmsDeviceStatus.history') }}</h3>
                <div class="history-table-wrap">
                    <table class="history-table">
                        <thead>
                            <tr>
                                <th>{{ $t('lz.entity.vmsDeviceStatus.recordTime') }}</th>
                                <th>{{ $t('lz.entity.vmsDeviceStatus.temperatureIn') }}</th>
                                <th>{{ $t('lz.entity.vmsDeviceStatus.humidityIn') }}</th>
                                <th>{{ $t('lz.entity.vmsDeviceStatus.door') }}</th>
                                <th>{{ $t('lz.entity.vmsDeviceStatus.powerStatus') }}</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr v-for="(row, idx) in historyRows" :key="idx">
                                <td class="col-time">{{ formatTime(row.collectedTime) }}</td>
                                <td>{{ formatValue(row.stats.temperatureIn, '°C') }}</td>
                                <td>{{ formatValue(row.stats.humidityIn, '%') }}</td>
                                <td>
                                    <span :style="{ color: isTruthy(row.stats.doorOpen) ? 'var(--el-color-danger)' : 'var(--el-color-success)' }">
                                        {{ isTruthy(row.stats.doorOpen) ? $t('lz.label.vmsDeviceStatus.doorOpen') : $t('lz.label.vmsDeviceStatus.doorClosed') }}
                                    </span>
                                </td>
                                <td>
                                    <span :style="{ color: isTruthy(row.stats.powerStatus) ? 'var(--el-color-success)' : 'var(--el-text-color-secondary)' }">
                                        {{ isTruthy(row.stats.powerStatus) ? $t('lz.label.vmsDeviceStatus.online') : $t('lz.label.vmsDeviceStatus.offline') }}
                                    </span>
                                </td>
                            </tr>
                            <tr v-if="!historyRows.length">
                                <td colspan="5" class="no-data">{{ $t('lz.label.base.noData') }}</td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts" name="deviceStatusTab">
import { reactive, computed, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { Refresh, Tickets } from '@element-plus/icons-vue';
import { getAPI } from '/@/utils/axios-utils';
import { EquipmentTelemetryApi } from '/@/api-services/vms';

const { t: $t } = useI18n();
const router = useRouter();

// Ngữ cảnh thiết bị
interface Props {
    itemId?: string | number;
    deviceName?: string;
    deviceType?: string; // 'vms' | 'lcs'
}
const props = defineProps<Props>();

// Số dòng lịch sử hiển thị cố định trong tab (xem đầy đủ ở trang quản lý)
const HISTORY_LIMIT = 5;

// Đường dẫn trang quản lý lịch sử telemetry.
// LƯU Ý: chỉnh cho khớp path của menu bạn thêm cho views/vms/telemetry/index.vue.
const TELEMETRY_ROUTE_PATH = '/vms/monitorMngt';

// Metadata các thông số hiển thị ở khung "Hiện tại" (key khớp IntgVmsStatType trả từ BE).
const CURRENT_FIELDS: { key: string; labelKey: string; unit?: string; type?: 'door' | 'power' }[] = [
    { key: 'temperatureIn', labelKey: 'lz.entity.vmsDeviceStatus.temperatureIn', unit: '°C' },
    { key: 'temperatureOut', labelKey: 'lz.entity.vmsDeviceStatus.temperatureOut', unit: '°C' },
    { key: 'humidityIn', labelKey: 'lz.entity.vmsDeviceStatus.humidityIn', unit: '%' },
    { key: 'humidityOut', labelKey: 'lz.entity.vmsDeviceStatus.humidityOut', unit: '%' },
    { key: 'windSpeed', labelKey: 'lz.entity.vmsDeviceStatus.windSpeed', unit: 'm/s' },
    { key: 'windDirection', labelKey: 'lz.entity.vmsDeviceStatus.windDirection' },
    { key: 'gradient', labelKey: 'lz.entity.vmsDeviceStatus.gradient' },
    { key: 'brightness', labelKey: 'lz.entity.vmsDeviceStatus.brightness' },
    { key: 'pixelErrorCount', labelKey: 'lz.entity.vmsDeviceStatus.pixelErrorCount' },
    { key: 'firmwareVersion', labelKey: 'lz.entity.vmsDeviceStatus.firmwareVersion' },
    { key: 'doorOpen', labelKey: 'lz.entity.vmsDeviceStatus.door', type: 'door' },
    { key: 'powerStatus', labelKey: 'lz.entity.vmsDeviceStatus.powerStatus', type: 'power' },
];

const state = reactive({
    loading: false,
    current: null as any, // bản ghi mới nhất
    records: [] as any[], // toàn bộ bản ghi lấy về (mới -> cũ)
});

// Chuyển sang trang quản lý lịch sử telemetry, lọc sẵn theo thiết bị đang xem.
const goToHistory = () => {
    router.push({
        path: TELEMETRY_ROUTE_PATH,
        query: { equipmentCode: state.current?.equipmentCode ?? undefined },
    });
};

// Gom các nhóm trong Config JSON thành 1 map phẳng { statKey: value }
const flattenConfig = (configStr?: string | null): Record<string, any> => {
    if (!configStr) return {};
    try {
        const obj = JSON.parse(configStr);
        const flat: Record<string, any> = {};
        Object.values(obj || {}).forEach((group: any) => {
            if (group && typeof group === 'object') Object.assign(flat, group);
        });
        return flat;
    } catch {
        return {};
    }
};

// Thông số của bản ghi hiện tại (đã phẳng)
const currentStats = computed<Record<string, any>>(() => flattenConfig(state.current?.config));

// Chỉ hiển thị field có giá trị trong bản ghi hiện tại
const visibleCurrentFields = computed(() => CURRENT_FIELDS.filter((m) => currentStats.value[m.key] !== undefined && currentStats.value[m.key] !== null));

// Lịch sử = các bản ghi trước bản ghi mới nhất, kèm map thông số phẳng
const historyRows = computed(() =>
    state.records.slice(1).map((r) => ({
        collectedTime: r.collectedTime,
        stats: flattenConfig(r.config),
    }))
);

// Giá trị dạng "1"/"true"/1 => bật/mở
const isTruthy = (v: any) => v === 1 || v === '1' || v === true || v === 'true';

const formatValue = (v: any, unit?: string) => {
    if (v === undefined || v === null || v === '') return '-';
    return unit ? `${v} ${unit}` : `${v}`;
};

const formatTime = (d?: string | Date | null) => {
    if (!d) return '-';
    const date = new Date(d);
    return isNaN(date.getTime()) ? '-' : date.toLocaleString('vi-VN');
};

// Lấy lịch sử telemetry của thiết bị: mới nhất làm "hiện tại", còn lại là lịch sử.
const fetchData = async () => {
    if (!props.itemId) {
        state.current = null;
        state.records = [];
        return;
    }
    state.loading = true;
    try {
        const res = await getAPI(EquipmentTelemetryApi).apiVmsEquipmenttelemetryPagePost({
            equipmentId: String(props.itemId),
            page: 1,
            pageSize: HISTORY_LIMIT + 1, // +1: 1 bản ghi "hiện tại" + tối đa 5 dòng lịch sử
            field: 'collectedTime',
            order: 'desc',
            descStr: 'desc',
        } as any);

        const records = res.data?.result?.records ?? [];
        state.records = records;
        state.current = records[0] ?? null;
    } catch (e) {
        console.error('Error loading device telemetry:', e);
        state.current = null;
        state.records = [];
    } finally {
        state.loading = false;
    }
};

// itemId được gán sau khi mở panel (reactive) -> nạp ngay khi có
watch(() => props.itemId, fetchData, { immediate: true });
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

/* Thanh công cụ trên cùng tab trạng thái */
.status-toolbar {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 8px;
}

.section-title .current-meta {
    margin-left: 8px;
    font-weight: 400;
}

.update-time {
    margin-left: 6px;
    font-size: 11px;
    color: var(--el-text-color-secondary);
}

/* Bảng lịch sử ghi nhận */
.history-table-wrap {
    max-height: 200px;
    overflow: auto;
}

.history-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 12px;

    th,
    td {
        padding: 6px 8px;
        text-align: center;
        border-bottom: 1px solid var(--el-border-color-light);
        white-space: nowrap;
    }

    thead th {
        position: sticky;
        top: 0;
        background: var(--el-color-primary);
        color: #fff;
        font-weight: 600;
        z-index: 1;
    }

    tbody tr:hover td {
        background: color-mix(in srgb, var(--el-color-primary) 28%, transparent);
        color: #fff;
    }

    .col-time {
        font-variant-numeric: Inter-UI, Menlo, Monaco, Consolas, 'Courier New', monospace;
    }
}

.no-data {
    padding: 16px;
    text-align: center;
    color: var(--el-text-color-secondary);
}
</style>
