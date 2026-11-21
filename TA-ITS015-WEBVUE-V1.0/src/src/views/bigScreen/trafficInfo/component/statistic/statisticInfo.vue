<template>
    <el-alert v-if="state.error" :title="state.error" type="warning" :closable="false" />
    <div class="stat-wrap">
        <!-- Bảng phân loại xe của zone 1 -->
        <div v-for="(group, groupIndex) in STATISTIC_ZONES" :key="group.id" class="stat-card stat-table-card" :style="{ order: groupIndex === 0 ? 0 : groupIndex + 2 }">
            <!-- Thẻ thống kê lưu lượng theo hướng: icon + tiêu đề tĩnh + tên hướng (dữ liệu cấu hình) dạng pill màu theo hướng
                 đẩy về bên phải, giữ nguyên chữ hoa/thường như khi nhập. Màu pill trùng màu đường lưu lượng / kim đồng hồ cùng hướng. -->
            <div class="stat-card-title stat-card-title--dir" :style="{ '--dir-color': ZONE_HEX[groupIndex % ZONE_HEX.length] }">
                <el-icon class="stat-title-icon"><Van /></el-icon>
                <span class="dir-label">{{ $t('lz.label.zoneInfo.volumeByDirection') }}</span>
                <span class="dir-badge" :title="group.name">
                    <span class="dir-dot"></span>
                    <span class="dir-name">{{ group.name }}</span>
                </span>
            </div>
            <table class="stat-veh-table">
                <thead>
                    <tr>
                        <th class="text-left">{{ $t('lz.label.zoneInfo.vehicleType') }}</th>
                        <th>{{ $t('lz.label.zoneInfo.hourCount') }}</th>
                        <th>{{ $t('lz.label.zoneInfo.dayCount') }}</th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="veh in state.vehicleTypes" :key="group.id + veh.code">
                        <td class="text-left">
                            <!-- Hiển thị ảnh minh hoạ loại xe (S/M/L); tooltip là tên loại xe, thiếu ảnh thì hiển thị tên -->
                            <el-tooltip v-if="getVehicleImage(veh)" :content="veh.name" placement="top" :show-after="150">
                                <img class="veh-img" :src="getVehicleImage(veh)" :alt="veh.name" />
                            </el-tooltip>
                            <span v-else>{{ veh.name }}</span>
                        </td>
                        <td>{{ getCell(groupIndex, veh.code, 'hour') }}</td>
                        <td>{{ getCell(groupIndex, veh.code, 'day') }}</td>
                    </tr>
                    <tr class="stat-veh-total">
                        <td class="text-left">{{ $t('lz.label.zoneInfo.vehicleCount') }}</td>
                        <td>{{ getTotal(groupIndex, 'hour') }}</td>
                        <td>{{ getTotal(groupIndex, 'day') }}</td>
                    </tr>
                </tbody>
            </table>
        </div>

        <!-- Biểu đồ lưu lượng theo giờ -->
        <div class="stat-card stat-chart-card" style="order: 1">
            <div class="stat-card-title"><el-icon class="stat-title-icon"><TrendCharts /></el-icon>{{ $t('lz.label.zoneInfo.volumeTabTitle') }}</div>
            <scEcharts class="stat-chart" height="100%" :option="volumeOption" />
        </div>

        <!-- Tốc độ trung bình 2 hướng -->
        <div class="stat-card stat-chart-card" style="order: 2">
            <div class="stat-card-title"><el-icon class="stat-title-icon"><Odometer /></el-icon>{{ $t('lz.label.zoneInfo.speedTabTitle') }}</div>
            <scEcharts class="stat-chart" height="100%" :option="speedOption" />
        </div>

    </div>
</template>

<script setup lang="ts" name="statisticInfo">
import { reactive, computed, onMounted, onUnmounted, onActivated, onDeactivated, defineAsyncComponent } from 'vue';
import { useI18n } from 'vue-i18n';
import { TrendCharts, Odometer, Van } from '@element-plus/icons-vue';
import { useThrottleFn } from '@vueuse/core';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { getAPI } from '/@/utils/axios-utils';
import { VdsDashboardApi } from '/@/api-services/vds';
import { useVdsSse } from '/@/transporter/hooks/useVdsSse';
import vehicleShortImage from '/@/assets/vehicle/S.png';
import vehicleMediumImage from '/@/assets/vehicle/M.png';
import vehicleLongImage from '/@/assets/vehicle/L.png';
import { vehicleCountField } from '../vehicleCountField';

const scEcharts = defineAsyncComponent(() => import('/@/components/scEcharts/index.vue'));

/**
 * Ảnh minh hoạ loại xe: cột `value` của cấu hình loại xe chứa tên file (S.png / M.png / L.png),
 * import tĩnh để Vite bundle được đường dẫn thật khi build.
 */
const VEHICLE_IMAGE_MAP: Record<string, string> = {
    's.png': vehicleShortImage,
    'm.png': vehicleMediumImage,
    'l.png': vehicleLongImage,
};

/** Thứ tự dòng trong bảng phân loại xe: xe ngắn (S) -> trung (M) -> dài (L). */
const VEHICLE_DISPLAY_ORDER = ['s.png', 'm.png', 'l.png'];

const { t: $t } = useI18n();
const baseConfig = useBaseConfig();
const getConfigDataByCode = baseConfig.getConfigDataByCode;


/**
 * Zone dùng để thống kê theo từng hướng, lấy từ cấu hình vận hành.
 */
interface StatisticZoneConfig {
    id: string;
    name: string;
}

interface StatisticCell {
    hour: number;
    day: number;
}

const STATISTIC_ZONES = reactive<StatisticZoneConfig[]>([
    { id: '2', name: 'Hướng 2' },
    { id: '1', name: 'Hướng 1' },
]);

/** Màu của 2 hướng/zone, dùng chung cho bảng, biểu đồ lưu lượng và đồng hồ tốc độ. */
const ZONE_HEX = ['#409eff', '#e6a23c'];

const state = reactive({
    // Nhóm hướng lấy từ snapshot VDS theo cấu hình thiết bị đếm.
    directions: [] as any[],
    vehicleTypes: [] as any[],
    hours: [] as string[],
    cellData: {} as Record<number, Record<string, StatisticCell>>,
    volumeSeries: [] as { name: string; data: number[] }[],
    avgSpeed: [] as (number | null)[],
    error: '',
    totals: [] as { hour: number | null; day: number | null }[],
});

const pick = (value: any, ...keys: string[]): any => {
    if (!value || typeof value !== 'object') return undefined;

    for (const key of keys) {
        if (value[key] !== undefined && value[key] !== null) return value[key];
    }

    return undefined;
};

const normalizeCode = (value: any): string => String(value ?? '').trim().toLowerCase();

const toNumber = (value: any): number => {
    const numberValue = Number(value);
    return Number.isFinite(numberValue) ? numberValue : 0;
};



const hexToRgba = (hex: string, alpha: number) => {
    const h = hex.replace('#', '');
    const r = parseInt(h.substring(0, 2), 16);
    const g = parseInt(h.substring(2, 4), 16);
    const b = parseInt(h.substring(4, 6), 16);
    return `rgba(${r}, ${g}, ${b}, ${alpha})`;
};

/** Chuẩn hoá tên file ảnh ở cột `value` của cấu hình loại xe. */
const getVehicleImageKey = (vehicleType: any): string => String(vehicleType?.value ?? '').trim().toLowerCase();

/** Thứ tự hiển thị trong bảng phân loại xe: S -> M -> L, không phụ thuộc thứ tự trả về của cấu hình. */
const getVehicleOrder = (vehicleType: any): number => {
    const orderIndex = VEHICLE_DISPLAY_ORDER.indexOf(getVehicleImageKey(vehicleType));
    // Loại xe chưa gán ảnh S/M/L thì xếp cuối bảng, giữ nguyên thứ tự gốc giữa chúng.
    return orderIndex < 0 ? VEHICLE_DISPLAY_ORDER.length : orderIndex;
};

const configureVehicleTypes = () => {
    const configuredTypes = getConfigDataByCode(BaseConfigTypeEnum.TmsVehicleTypeConfigCode) || [];
    const vehicleTypes = configuredTypes.length
        ? configuredTypes.map((vehicleType: any) => ({
            ...vehicleType,
            code: String(vehicleType.code),
        }))
        : [
            // Mã fallback theo nhóm S/M/L mà VDS lưu vào TmsTrafficData.Type.
            { code: '1', name: 'Xe < 6m', value: 'S.png' },
            { code: '3', name: 'Xe 6 - 12m', value: 'M.png' },
            { code: '4', name: 'Xe > 12m', value: 'L.png' },
        ];

    // Sort trên mảng mới (map/fallback) nên không làm đổi thứ tự dữ liệu trong store cấu hình.
    state.vehicleTypes = vehicleTypes.sort((a: any, b: any) => getVehicleOrder(a) - getVehicleOrder(b));
};

/** Lấy ảnh loại xe theo tên file đã cấu hình ở cột `value`; không khớp thì trả về '' để hiển thị tên. */
const getVehicleImage = (vehicleType: any): string => VEHICLE_IMAGE_MAP[getVehicleImageKey(vehicleType)] ?? '';

const normalizeAxisKey = (value: any): string => String(value ?? '').trim().replace(/\s*h$/i, '');

/**
 * Cộng các series loại xe của từng zone theo đúng mốc xAxis, sau đó tạo 2 series hướng.
 * Zone thứ i luôn tương ứng hướng thứ i (xem loadStatisticZones) nên không cần dò theo tên.
 */
const applyVolumeChartData = (charts: any[]) => {
    const firstAxisData = pick(pick(charts.find(Boolean), 'xAxis', 'XAxis') || {}, 'data', 'Data');
    const axisKeys = Array.isArray(firstAxisData) ? firstAxisData.map(normalizeAxisKey) : [];

    state.hours = axisKeys.map((axisKey) => `${axisKey}h`);
    const directionData = state.directions.map(() => Array.from({ length: axisKeys.length }, () => 0));

    charts.forEach((chart, zoneIndex) => {
        const seriesData = pick(chart, 'series', 'Series');
        const rawAxisData = pick(pick(chart, 'xAxis', 'XAxis') || {}, 'data', 'Data');

        if (!Array.isArray(seriesData) || !Array.isArray(rawAxisData) || !directionData[zoneIndex]) return;

        rawAxisData.map(normalizeAxisKey).forEach((axisKey, axisIndex) => {
            const outputAxisIndex = axisKeys.indexOf(axisKey);
            if (outputAxisIndex < 0) return;

            directionData[zoneIndex][outputAxisIndex] += seriesData.reduce((sum: number, series: any) => {
                const data = pick(series, 'data', 'Data');
                return sum + (Array.isArray(data) ? toNumber(data[axisIndex]) : 0);
            }, 0);
        });
    });

    state.volumeSeries = state.directions.map((direction, directionIndex) => ({
        name: String(direction.name ?? direction.code),
        data: directionData[directionIndex] ?? [],
    }));
};

const getCell = (index: number, code: string, type: 'hour' | 'day') => state.cellData[index]?.[code]?.[type] ?? '—';
const getTotal = (index: number, type: 'hour' | 'day') => state.totals[index]?.[type] ?? '—';

/**
 * Cỡ chữ dùng chung cho 2 chart của khối thống kê.
 * Khối này hiển thị trên bigScreen nên chữ phải đủ lớn để đọc từ xa; gom về một chỗ
 * để 2 chart không bị lệch cỡ chữ khi tinh chỉnh.
 */
/** Màu chữ/lưới của chart trên nền thẻ tối. */
const CHART_COLOR = {
    text: '#9fb3cc',
    legend: '#c9d6e8',
    axisLine: 'rgba(255, 255, 255, 0.18)',
    splitLine: 'rgba(255, 255, 255, 0.07)',
};

/** Tooltip tối đồng bộ panel. */
const DARK_TOOLTIP = {
    backgroundColor: 'rgba(14, 24, 42, 0.95)',
    borderColor: 'rgba(64, 158, 255, 0.45)',
    textStyle: { color: '#e8f2ff', fontSize: 14 },
};

const CHART_FONT = {
    legend: 15,
    axisLabel: 14,
    /* Đồng hồ tốc độ có 2 cái nằm cạnh nhau nên nhãn mốc nhỏ hơn chart lưu lượng để không đè nhau */
    gaugeAxisLabel: 12,
    gaugeTitle: 14,
    gaugeValue: 20,
};

// Chart hiển thị đúng 2 hướng; mỗi hướng là tổng các series loại xe của zone tương ứng.
const volumeOption = computed(() => ({
    color: ZONE_HEX,
    tooltip: { trigger: 'axis', ...DARK_TOOLTIP },
    legend: {
        data: state.volumeSeries.map((series) => series.name),
        top: 2,
        textStyle: { color: CHART_COLOR.legend, fontSize: CHART_FONT.legend },
        itemWidth: 18,
        itemHeight: 11,
    },
    /* Chừa thêm chỗ cho nhãn trục và legend đã tăng cỡ chữ, tránh bị cắt chữ */
    grid: { top: 40, left: 52, right: 16, bottom: 30 },
    xAxis: {
        type: 'category',
        boundaryGap: false,
        data: state.hours,
        axisLabel: { color: CHART_COLOR.text, fontSize: CHART_FONT.axisLabel },
        axisLine: { lineStyle: { color: CHART_COLOR.axisLine } },
    },
    yAxis: {
        type: 'value',
        axisLabel: { color: CHART_COLOR.text, fontSize: CHART_FONT.axisLabel },
        splitLine: { lineStyle: { color: CHART_COLOR.splitLine } },
    },
    series: state.volumeSeries.map((series, index) => {
        const color = ZONE_HEX[index % ZONE_HEX.length];
        return {
            name: series.name,
            type: 'line',
            smooth: true,
            showSymbol: false,
            lineStyle: { width: 2.5 },
            areaStyle: {
                opacity: 0.9,
                color: {
                    type: 'linear',
                    x: 0, y: 0, x2: 0, y2: 1,
                    colorStops: [
                        { offset: 0, color: hexToRgba(color, 0.35) },
                        { offset: 1, color: hexToRgba(color, 0.02) },
                    ],
                },
            },
            data: series.data,
        };
    }),
}));

const SPEED_ZONE_COLORS: [number, string][] = [
    [40 / 120, '#66ccff'],
    [90 / 120, '#ff9966'],
    [1, '#ff3300'],
];

const speedOption = computed(() => {
    return {
        series: state.directions.map((direction, index) => ({
            type: 'gauge',
            /**
             * Thu nhỏ đồng hồ và đẩy 2 tâm xa nhau hơn: bán kính + khoảng cách nhãn + bề rộng chữ
             * phải nhỏ hơn nửa khoảng cách giữa 2 tâm, nếu không nhãn số của 2 đồng hồ sẽ đè nhau.
             */
            center: [`${(index + 0.5) * 100 / state.directions.length}%`, '52%'],
            radius: '66%',
            min: 0,
            max: 120,
            /* 6 mốc (0/20/.../120) thay vì 8 mốc để giảm số nhãn phải vẽ */
            splitNumber: 6,
            startAngle: 210,
            endAngle: -30,
            axisLine: { lineStyle: { width: 10, color: SPEED_ZONE_COLORS } },
            /**
             * distance âm = đẩy vạch/nhãn ra ngoài vành: vạch cách vành 6-10px, nhãn số cách 24px
             * để nhãn không dính vành mà 2 đồng hồ vẫn không đè nhãn của nhau.
             */
            axisTick: { distance: -6, length: 5, lineStyle: { color: '#fff', width: 1 } },
            splitLine: { distance: -10, length: 9, lineStyle: { color: '#fff', width: 2 } },
            axisLabel: { color: CHART_COLOR.text, distance: -24, fontSize: CHART_FONT.gaugeAxisLabel },
            pointer: { width: 4, length: '55%', itemStyle: { color: ZONE_HEX[index % ZONE_HEX.length] } },
            anchor: { show: true, size: 7, itemStyle: { color: ZONE_HEX[index % ZONE_HEX.length] } },
            title: { offsetCenter: [0, '88%'], color: CHART_COLOR.legend, fontSize: CHART_FONT.gaugeTitle, fontWeight: 600 },
            detail: {
                valueAnimation: true,
                offsetCenter: [0, '46%'],
                fontSize: CHART_FONT.gaugeValue,
                fontWeight: 'bolder',
                color: ZONE_HEX[index % ZONE_HEX.length],
                formatter: (value: number) => state.avgSpeed[index] == null ? '—' : `${value} Km/h`,
            },
            data: [{ value: state.avgSpeed[index] ?? 0, name: direction.name }],
        })),
    };
});

let latestRequestId = 0;

let disposed = false;
let active = true;
let poll: ReturnType<typeof setInterval> | undefined;
const refreshStatistics = async () => {
    if (disposed || !active) return;
    const requestId = ++latestRequestId;
    try {
        const response = await getAPI(VdsDashboardApi).apiVdsVdsdashboardCenterinfoGet();
        if (disposed || !active || requestId !== latestRequestId) return;
        const groups = response.data.result?.directions;
        if (!Array.isArray(groups)) throw new Error('Phản hồi thống kê VDS thiếu danh sách directions');
        if (!groups.length) {
            state.directions = [];
            // Giữ khung bảng khi snapshot chưa có nhóm hướng; số liệu được xóa bên dưới.
            // Không suy ra số đếm từ biểu đồ hoặc hiển thị lại số liệu cũ.
            state.cellData = {};
            state.totals = [];
            state.hours = [];
            state.volumeSeries = [];
            state.avgSpeed = [];
            state.error = 'API VDS trả về danh sách hướng rỗng. Kiểm tra các mục vds_count_direction_equipment_<hướng> đang bật trong nhóm vds_config và đúng tenant của tài khoản. Chưa có số liệu đếm theo hướng.';
            return;
        }
        const ordered = [...groups].sort((a, b) => {
            const rank = (key?: string | null) => key === '2' ? 0 : key === '1' ? 1 : 2;
            return rank(a.directionKey) - rank(b.directionKey);
        });
        state.directions = ordered.map(g => ({ code: g.directionKey, name: g.name || g.directionKey }));
        STATISTIC_ZONES.splice(0, STATISTIC_ZONES.length, ...ordered.map(g => ({ id: g.directionKey || '', name: g.name || g.directionKey || '' })));
        state.cellData = {};
        state.totals = ordered.map(g => ({ hour: g.last1h?.total ?? null, day: g.last24h?.total ?? null }));
        ordered.forEach((g, index) => {
            state.cellData[index] = {};
            state.vehicleTypes.forEach(v => {
                // Swagger đặt tên reserved word _short/_long; JSON thực tế vẫn là short/long.
                const field = vehicleCountField(v);
                state.cellData[index][v.code] = { hour: field ? (g.last1h as any)?.[field] : undefined, day: field ? (g.last24h as any)?.[field] : undefined };
            });
        });
        applyVolumeChartData(ordered.map(g => g.chart));
        state.avgSpeed = ordered.map(g => g.speed?.last1hAvgSpeed ?? null);
        state.error = '';
    } catch (error) {
        if (disposed || !active || requestId !== latestRequestId) return;
        const message = 'Không tải được thống kê VDS. Kiểm tra kết nối và cấu hình nhóm đếm.';
        if (state.error !== message) console.error('statisticInfo.vue: refreshStatistics', error);
        state.error = message;
    }
};
const startPoll = () => { if (!poll) poll = setInterval(() => { void refreshStatistics(); }, 60000); };
const stopPoll = () => { if (poll) clearInterval(poll); poll = undefined; };
onMounted(() => { configureVehicleTypes(); void refreshStatistics(); startPoll(); });
onActivated(() => { if (active) return; active = true; void refreshStatistics(); startPoll(); });
onDeactivated(() => { active = false; latestRequestId++; stopPoll(); });
const refresh = useThrottleFn(() => refreshStatistics(), 1500);
useVdsSse(['count'], refresh);
onUnmounted(() => { disposed = true; active = false; latestRequestId++; stopPoll(); });
</script>

<style scoped lang="scss">
@import '/@/theme/its/trafficInfo.scss';

.stat-wrap {
    flex: 1;
    min-height: 200px;
    width: 100%;
    display: grid;
    grid-template-columns: 0.95fr 1.5fr 1.15fr 0.95fr;
    gap: 10px;
    padding-top: 2px;
}

/* Thẻ kính tối - đồng bộ khung danh sách sự cố / camera ở tab Giám sát */
.stat-card {
    position: relative;
    background: linear-gradient(160deg, rgba(20, 34, 58, 0.82) 0%, rgba(10, 18, 32, 0.8) 100%);
    border: 1px solid color-mix(in srgb, var(--el-color-primary) 40%, transparent);
    border-radius: 10px;
    box-shadow:
        0 8px 24px -14px rgba(0, 0, 0, 0.7),
        0 0 16px -6px color-mix(in srgb, var(--el-color-primary) 55%, transparent),
        inset 0 1px 0 rgba(255, 255, 255, 0.08);
    padding: 6px 10px 8px;
    display: flex;
    flex-direction: column;
    min-height: 0;
    overflow: hidden;
}

/* Tiêu đề thẻ: cùng kiểu tiêu đề khu Giám sát (chữ sáng, đường kẻ gradient mờ dần) */
.stat-card-title {
    position: relative;
    display: flex;
    align-items: center;
    gap: 8px;
    color: #e8f2ff;
    font-size: 13px;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.8px;
    padding: 2px 2px 7px;
    margin-bottom: 4px;
    flex: 0 0 auto;

    &::after {
        content: '';
        position: absolute;
        left: 0;
        right: 0;
        bottom: 0;
        height: 1px;
        background: linear-gradient(90deg, color-mix(in srgb, var(--el-color-primary) 70%, transparent) 0%, color-mix(in srgb, var(--el-color-primary) 20%, transparent) 45%, transparent 100%);
    }

    /* Tiêu đề thẻ lưu lượng theo hướng: icon + tiêu đề tĩnh (cỡ như tiêu đề thẻ khác) + pill tên hướng (dữ liệu) ở bên phải.
       Thẻ hẹp thì tiêu đề tĩnh cắt "…" trước, tên hướng luôn đọc được. */
    &--dir {
        gap: 8px;

        .dir-label {
            flex: 1;
            min-width: 0;
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
        }

        .dir-badge {
            flex: none;
            max-width: 55%;
            margin-left: auto;
            display: inline-flex;
            align-items: center;
            gap: 7px;
            padding: 3px 12px 3px 9px;
            border-radius: 999px;
            background: color-mix(in srgb, var(--dir-color) 20%, transparent);
            box-shadow: inset 0 0 0 1px color-mix(in srgb, var(--dir-color) 55%, transparent), 0 0 14px -6px var(--dir-color);
        }

        .dir-dot {
            background: var(--dir-color);
            color: var(--dir-color);
        }

        .dir-name {
            min-width: 0;
            font-size: 14px;
            font-weight: 700;
            letter-spacing: 0.2px;
            text-transform: none;
            color: #fff;
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
        }
    }

    /* Chấm màu hướng (bảng phân loại xe) */
    .dir-dot {
        width: 10px;
        height: 10px;
        border-radius: 50%;
        flex-shrink: 0;
        box-shadow: 0 0 8px currentColor;
    }

    /* Icon badge (thẻ biểu đồ) - cùng kiểu icon tiêu đề khu Giám sát */
    .stat-title-icon {
        flex: none;
        width: 24px;
        height: 24px;
        border-radius: 7px;
        font-size: 14px;
        color: var(--el-color-primary-light-3);
        background: color-mix(in srgb, var(--el-color-primary) 20%, transparent);
        box-shadow: inset 0 0 0 1px color-mix(in srgb, var(--el-color-primary) 40%, transparent);
    }
}

.stat-chart-card .stat-chart {
    flex: 1;
    /* Cỡ chữ trong chart đã tăng nên cần thêm chiều cao tối thiểu để nhãn không bị chồng */
    min-height: 180px;
    width: 100%;
}

/* Bảng phân loại xe - giãn đầy chiều cao card, tránh dư trắng dưới */
.stat-table-card {
    justify-content: flex-start;
}

.stat-veh-table {
    width: 100%;
    flex: 1;
    height: 100%;
    border-collapse: collapse;
    color: #e8f2ff;
    font-size: 15px;
    font-variant-numeric: tabular-nums;

    thead th {
        color: rgba(232, 242, 255, 0.5);
        font-weight: 600;
        font-size: 12px;
        text-transform: uppercase;
        letter-spacing: 0.4px;
        padding: 4px 6px 8px;
        text-align: center;
    }

    tbody td {
        padding: 7px 6px;
        text-align: center;
        font-weight: 600;
        border-bottom: 1px solid rgba(255, 255, 255, 0.06);
    }

    tbody tr:nth-child(odd) {
        background: rgba(255, 255, 255, 0.03);
    }

    .text-left {
        text-align: left;
    }

    /* Ảnh loại xe (S/M/L) là hình đen: đặt trên nền sáng bo tròn để nổi trên thẻ tối */
    .veh-img {
        display: block;
        height: 38px;
        width: auto;
        max-width: 100%;
        object-fit: contain;
        padding: 2px 8px;
        border-radius: 7px;
        background: rgba(255, 255, 255, 0.92);
        box-shadow: 0 2px 8px -4px rgba(0, 0, 0, 0.6);
    }

    .stat-veh-total td {
        border-top: 1px solid color-mix(in srgb, var(--el-color-primary) 45%, transparent);
        border-bottom: none;
        color: var(--el-color-primary-light-3);
        font-weight: 800;
        font-size: 17px;
        background: color-mix(in srgb, var(--el-color-primary) 14%, transparent);
    }
}
</style>
