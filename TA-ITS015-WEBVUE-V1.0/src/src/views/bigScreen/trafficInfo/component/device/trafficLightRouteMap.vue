<template>
    <div class="tl-route">
        <div class="tl-route__header">
            <span class="tl-route__title">{{ $t('lz.label.trafficLight.equipmentList') }}</span>
            <span class="tl-route__count" :class="{ 'is-active': selectedIds.length > 0 }">{{ selectedIds.length }}/{{ totalCount }}</span>
        </div>

        <div v-if="view.lanes.length" class="tl-route__map">
            <!-- Trục lý trình: đầu tuyến (trái) -> cuối tuyến (phải) -->
            <div class="tl-route__axis">
                <span v-for="tick in view.ticks" :key="tick.pct" class="tl-route__tick" :class="{ 'is-end': tick.isEnd }" :style="{ left: leftOf(tick.pct) }">{{ tick.label }}</span>
            </div>

            <div v-for="lane in view.lanes" :key="lane.key" class="tl-route__lane">
                <div class="tl-route__lane-head">
                    <el-checkbox
                        :model-value="lane.selectedCount === lane.items.length"
                        :indeterminate="lane.selectedCount > 0 && lane.selectedCount < lane.items.length"
                        @change="toggleLane(lane)"
                    >
                        <span class="tl-route__lane-title">{{ lane.name }}</span>
                    </el-checkbox>
                    <span class="tl-route__badge">{{ lane.selectedCount }}/{{ lane.items.length }}</span>
                </div>

                <div class="tl-route__track" :style="{ height: laneHeight(lane) }">
                    <div class="tl-route__road" />
                    <button
                        v-for="m in lane.items"
                        :key="m.item.id"
                        type="button"
                        class="tl-route__marker"
                        :class="markerClass(m.item)"
                        :style="{ left: leftOf(m.pct), '--tier': m.tier }"
                        :aria-pressed="isSelected(m.item.id)"
                        :aria-label="m.item.name"
                        :title="`${m.item.name} · ${formatKm(m.pos)}`"
                        @click="toggle(m.item.id)"
                    >
                        <span class="tl-route__chip">{{ m.item.name }}</span>
                        <span class="tl-route__stem" />
                        <span class="tl-route__pin" />
                    </button>
                </div>
            </div>
        </div>
        <p v-else class="tl-route__empty">{{ $t('lz.label.base.noData') }}</p>
        <p v-if="view.lanes.length" class="tl-route__hint">{{ $t('lz.label.trafficLight.clickToToggle') }}</p>
    </div>
</template>

<script setup lang="ts" name="trafficLightRouteMap">
import { computed } from 'vue';

/** 1 đèn trên sơ đồ: position = km*1000+m (mét); color = màu đèn đã điều khiển (chưa có = chưa xác định). */
export interface TrafficLightMapItem {
    id: string;
    name: string;
    position: number;
    color?: 'red' | 'green';
}
export interface TrafficLightMapLane {
    key: string;
    name: string;
    items: TrafficLightMapItem[];
}

const props = defineProps<{
    lanes: TrafficLightMapLane[];
    modelValue: string[];
    /** Đầu/cuối tuyến (mét); thiếu hoặc đầu >= cuối thì tự lấy theo đèn xa nhất mỗi phía. */
    start?: number | null;
    end?: number | null;
}>();
const emit = defineEmits<{ (event: 'update:modelValue', value: string[]): void }>();

// Cùng thuật toán xếp nhãn như sơ đồ tuyến của modal VMS (controlVms): nhãn gần nhau xếp lên tầng khác để không chồng.
const TIER_GAP_PCT = 12;
const MAX_TIERS = 3;
const TIER_HEIGHT = 24;
const EDGE_INSET = 36;

const selectedIds = computed(() => props.modelValue || []);
const totalCount = computed(() => props.lanes.reduce((total, lane) => total + lane.items.length, 0));

const formatKm = (position: number) => `Km${Math.floor(position / 1000)}+${Math.round(position % 1000)}`;
const leftOf = (pct: number) => `calc(${EDGE_INSET}px + (100% - ${EDGE_INSET * 2}px) * ${pct / 100})`;
const laneHeight = (lane: { tiers: number }) => `${24 + lane.tiers * TIER_HEIGHT}px`;

const view = computed(() => {
    const lanesWithItems = props.lanes.filter((lane) => lane.items.length > 0);
    const positions = lanesWithItems.flatMap((lane) => lane.items.map((item) => item.position));
    if (!positions.length) return { lanes: [] as any[], ticks: [] as any[] };

    let start: number;
    let end: number;
    if (props.start != null && props.end != null && props.end > props.start) {
        start = props.start;
        end = props.end;
    } else {
        const lo = Math.min(...positions);
        const hi = Math.max(...positions);
        const pad = Math.max((hi - lo) * 0.05, 500);
        start = Math.max(0, lo - pad);
        end = hi + pad;
    }
    const toPct = (position: number) => Math.min(100, Math.max(0, ((position - start) / (end - start)) * 100));

    const selected = new Set(selectedIds.value);
    const lanes = lanesWithItems.map((lane) => {
        const tierLastPct: number[] = [];
        const items = lane.items
            .map((item) => ({ item, pos: item.position, pct: toPct(item.position) }))
            .sort((a, b) => a.pct - b.pct)
            .map((m) => {
                let tier = tierLastPct.findIndex((last) => m.pct - last >= TIER_GAP_PCT);
                if (tier === -1) tier = tierLastPct.length < MAX_TIERS ? tierLastPct.length : tierLastPct.indexOf(Math.min(...tierLastPct));
                tierLastPct[tier] = m.pct;
                return { ...m, tier };
            })
            // Vẽ tầng cao trước, tầng thấp sau: nhãn tầng dưới nằm đè lên cọc của tầng trên
            .sort((a, b) => b.tier - a.tier);
        return { key: lane.key, name: lane.name, items, tiers: Math.max(1, tierLastPct.length), selectedCount: items.filter((m) => selected.has(m.item.id)).length };
    });

    const ticks = [0, 25, 50, 75, 100].map((t) => ({ pct: t, label: formatKm(Math.round(start + ((end - start) * t) / 100)), isEnd: t === 0 || t === 100 }));
    return { lanes, ticks };
});

const isSelected = (id: string) => selectedIds.value.includes(id);
const toggle = (id: string) => {
    emit('update:modelValue', isSelected(id) ? selectedIds.value.filter((item) => item !== id) : [...selectedIds.value, id]);
};
// Chọn cả hầm: đã chọn đủ thì bỏ hết, ngược lại chọn thêm toàn bộ đèn của hầm.
const toggleLane = (lane: { items: { item: TrafficLightMapItem }[] }) => {
    const laneIds = lane.items.map((m) => m.item.id);
    const allSelected = laneIds.every((id) => isSelected(id));
    emit('update:modelValue', allSelected ? selectedIds.value.filter((id) => !laneIds.includes(id)) : Array.from(new Set([...selectedIds.value, ...laneIds])));
};

const markerClass = (item: TrafficLightMapItem) => ({
    'is-selected': isSelected(item.id),
    [`is-${item.color}`]: !!item.color,
});
</script>

<style scoped lang="scss">
/* Tông tối đồng bộ modal (InfoPanel): chữ trắng, nền chip/đường xanh đậm như sơ đồ tuyến của VMS */
$tl-text: #ffffff;
$tl-text-soft: #e8f2ff;
$tl-line: rgba(125, 165, 255, 0.4);

.tl-route__header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 6px;
}
.tl-route__title {
    color: $tl-text;
    font-size: 12px;
    font-weight: 700;
    letter-spacing: 0.04em;
    text-transform: uppercase;
}
.tl-route__count {
    padding: 1px 8px;
    border-radius: 999px;
    background: rgba(125, 165, 255, 0.28);
    color: $tl-text;
    font-size: 11.5px;
    font-weight: 700;
    font-variant-numeric: tabular-nums;
}
.tl-route__count.is-active {
    background: var(--el-color-primary);
    color: #fff;
}

.tl-route__empty {
    margin: 12px 0 4px;
    text-align: center;
    color: $tl-text;
    font-size: 12.5px;
}
.tl-route__hint {
    margin: 8px 0 0;
    color: $tl-text-soft;
    font-size: 11.5px;
}

/* Trục lý trình */
.tl-route__map { position: relative; padding-top: 2px; }
.tl-route__axis {
    position: relative;
    height: 18px;
    margin-bottom: 2px;
    border-bottom: 1px dashed $tl-line;
}
.tl-route__tick {
    position: absolute;
    top: 0;
    transform: translateX(-50%);
    color: $tl-text-soft;
    font-size: 10.5px;
    white-space: nowrap;
    font-variant-numeric: tabular-nums;
}
.tl-route__tick.is-end { color: $tl-text; font-weight: 700; }

/* Mỗi hầm là 1 làn */
.tl-route__lane { padding-top: 6px; }
.tl-route__lane + .tl-route__lane { margin-top: 4px; border-top: 1px solid $tl-line; }
.tl-route__lane-head { display: flex; align-items: center; justify-content: space-between; }
.tl-route__lane-head .el-checkbox { height: 22px; }
.tl-route__lane-title { color: $tl-text; font-size: 13px; font-weight: 600; }
.tl-route__badge {
    padding: 1px 7px;
    border-radius: 10px;
    background: rgba(125, 165, 255, 0.28);
    color: $tl-text;
    font-size: 11px;
    font-weight: 600;
    font-variant-numeric: tabular-nums;
}

.tl-route__track { position: relative; }
.tl-route__road {
    position: absolute;
    right: 0;
    bottom: 0;
    left: 0;
    height: 14px;
    border: 1px solid rgba(140, 180, 255, 0.5);
    border-radius: 7px;
    background: linear-gradient(180deg, #2a4266 0%, #223858 100%);
    box-shadow: inset 0 1px 3px rgba(0, 0, 0, 0.6);
}
.tl-route__road::after {
    content: '';
    position: absolute;
    top: 50%;
    right: 8px;
    left: 8px;
    height: 2px;
    transform: translateY(-50%);
    background: repeating-linear-gradient(90deg, rgba(241, 245, 249, 0.7) 0 10px, transparent 10px 20px);
}

/* Đèn: nhãn (chip) - cọc - chấm trên mặt đường. Tầng nhãn = --tier. Chấm tô theo màu đèn đã điều khiển. */
.tl-route__marker {
    position: absolute;
    bottom: 2px;
    z-index: 1;
    display: flex;
    flex-direction: column;
    align-items: center;
    padding: 0;
    transform: translateX(-50%);
    border: none;
    background: none;
    cursor: pointer;
    /* Chỉ nhãn nhận chuột để không che đèn nằm dưới khi 2 đèn gần nhau */
    pointer-events: none;
}
.tl-route__marker .tl-route__chip { pointer-events: auto; }
.tl-route__marker:hover,
.tl-route__marker:focus-visible { z-index: 3; outline: none; }

.tl-route__chip {
    padding: 2px 9px;
    border: 1px solid rgba(150, 188, 255, 0.65);
    border-radius: 999px;
    background: #2b4670;
    color: $tl-text;
    font-size: 11.5px;
    font-weight: 600;
    line-height: 1.5;
    white-space: nowrap;
    transition: transform 0.18s, background-color 0.18s, color 0.18s, border-color 0.18s;
}
.tl-route__stem {
    width: 1px;
    height: calc(4px + var(--tier, 0) * 24px);
    background: rgba(160, 195, 255, 0.7);
}
.tl-route__pin {
    width: 10px;
    height: 10px;
    border: 2px solid #b4cbef;
    border-radius: 50%;
    background: #223858;
    transition: transform 0.18s, background-color 0.18s, border-color 0.18s;
}

.tl-route__marker:hover .tl-route__chip,
.tl-route__marker:focus-visible .tl-route__chip { transform: translateY(-2px); border-color: #22d3ee; }
.tl-route__marker:hover .tl-route__pin { transform: scale(1.15); }

/* Màu đèn đã điều khiển */
.tl-route__marker.is-red .tl-route__pin { border-color: #fecaca; background: var(--el-color-danger); box-shadow: 0 0 6px var(--el-color-danger); }
.tl-route__marker.is-green .tl-route__pin { border-color: #bbf7d0; background: var(--el-color-success); box-shadow: 0 0 6px var(--el-color-success); }

/* Đang chọn */
.tl-route__marker.is-selected .tl-route__chip { border-color: #22d3ee; background: #22d3ee; color: #06212a; }
.tl-route__marker.is-selected .tl-route__stem { background: #22d3ee; }
</style>
