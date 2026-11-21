<template>
    <div class="event-rule-container">
        <!-- Thanh công cụ: bộ lọc + chú giải + hành động -->
        <el-card shadow="never" class="toolbar-card" :body-style="{ padding: '10px 12px' }">
            <div class="toolbar">
                <div class="toolbar-left">
                    <el-radio-group v-model="state.viewMode">
                        <el-radio-button value="byEquipment">{{ $t('lz.label.tmsEquipmentEventRule.viewByEquipment') }}</el-radio-button>
                        <el-radio-button value="matrix">{{ $t('lz.label.tmsEquipmentEventRule.viewMatrix') }}</el-radio-button>
                    </el-radio-group>
                    <el-input v-model="state.search" :placeholder="$t('lz.label.tmsEquipmentEventRule.searchEvent')" clearable prefix-icon="ele-Search" class="search-input" />
                    <el-select
                        v-model="state.selectedTypeIds"
                        multiple
                        collapse-tags
                        collapse-tags-tooltip
                        clearable
                        filterable
                        :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.equipmentType') })"
                        class="type-filter"
                    >
                        <el-option v-for="eq in state.equipmentTypes" :key="eq.id" :label="eq.name" :value="eq.id" />
                    </el-select>
                    <el-checkbox v-model="state.onlyConfigured">{{ $t('lz.label.tmsEquipmentEventRule.onlyConfigured') }}</el-checkbox>
                </div>
                <div class="toolbar-right">
                    <el-tooltip :content="$t('lz.label.tmsEquipmentEventRule.legend')" placement="bottom" :show-after="200">
                        <div class="legend">
                            <span class="legend-item"><i class="mark mark--before mark--on mark--static">▲</i>{{ $t('lz.label.tmsEquipmentEventRule.before') }}</span>
                            <span class="legend-item"><i class="mark mark--after mark--on mark--static">▼</i>{{ $t('lz.label.tmsEquipmentEventRule.after') }}</span>
                        </div>
                    </el-tooltip>
                    <el-badge :value="changeCount" :hidden="changeCount === 0" type="warning">
                        <el-button icon="ele-Refresh" @click="reload">{{ $t('lz.button.base.refresh') }}</el-button>
                    </el-badge>
                    <el-button :disabled="changeCount === 0" @click="resetChanges">{{ $t('lz.button.base.reset') }}</el-button>
                    <el-button type="primary" icon="ele-Select" :loading="state.saving" :disabled="changeCount === 0" v-auth="'tmsEquipmentEventRule:update'" @click="save">
                        {{ $t('lz.button.base.save') }}
                        <span v-if="changeCount > 0">&nbsp;({{ changeCount }})</span>
                    </el-button>
                </div>
            </div>
        </el-card>

        <!-- Chế độ theo thiết bị: chọn 1 loại thiết bị bên trái, cấu hình các loại sự kiện bên phải -->
        <el-card v-if="state.viewMode === 'byEquipment'" shadow="never" class="matrix-card" v-loading="state.loading">
            <div v-if="visibleEquipmentTypes.length" class="split">
                <aside class="split-side">
                    <div
                        v-for="eq in visibleEquipmentTypes"
                        :key="eq.id"
                        class="side-item"
                        :class="{ 'side-item--active': activeEquipment?.id === eq.id }"
                        :title="eq.name"
                        @click="state.activeEquipmentId = eq.id"
                    >
                        <span class="side-name">{{ eq.name }}</span>
                        <span class="count-pill" :class="{ 'count-pill--on': colCount(eq.id) > 0 }">{{ colCount(eq.id) }}</span>
                    </div>
                </aside>
                <section class="split-main" v-if="activeEquipment">
                    <div class="main-head">
                        <div class="main-title">
                            {{ activeEquipment.name }}
                            <span class="main-sub">{{ colCount(activeEquipment.id) }} / {{ state.eventTypes.length * 2 }}</span>
                        </div>
                        <div class="bulk-actions">
                            <el-button size="small" plain type="primary" @click="bulkCol(activeEquipment.id, 'both')">{{ $t('lz.label.tmsEquipmentEventRule.setAllBoth') }}</el-button>
                            <el-button size="small" plain @click="bulkCol(activeEquipment.id, 'before')">▲ {{ $t('lz.label.tmsEquipmentEventRule.setAllBefore') }}</el-button>
                            <el-button size="small" plain @click="bulkCol(activeEquipment.id, 'after')">▼ {{ $t('lz.label.tmsEquipmentEventRule.setAllAfter') }}</el-button>
                            <el-button size="small" plain type="danger" @click="bulkCol(activeEquipment.id, 'none')">{{ $t('lz.label.tmsEquipmentEventRule.clearAll') }}</el-button>
                        </div>
                    </div>
                    <div v-if="filteredEventTypes.length" class="event-list">
                        <div v-for="ev in filteredEventTypes" :key="ev.id" class="event-row" :class="{ 'event-row--active': isCellActive(ev.id, activeEquipment.id) }">
                            <span class="event-row-name" :title="ev.name">{{ ev.name }}</span>
                            <div class="cell-marks">
                                <button
                                    type="button"
                                    class="mark mark--before mark--wide"
                                    :class="{ 'mark--on': getCell(ev.id, activeEquipment.id).before, 'mark--dirty': isCellDirty(ev.id, activeEquipment.id, 'before') }"
                                    @click="toggleCell(ev.id, activeEquipment.id, 'before')"
                                >▲ {{ $t('lz.label.tmsEquipmentEventRule.before') }}</button>
                                <button
                                    type="button"
                                    class="mark mark--after mark--wide"
                                    :class="{ 'mark--on': getCell(ev.id, activeEquipment.id).after, 'mark--dirty': isCellDirty(ev.id, activeEquipment.id, 'after') }"
                                    @click="toggleCell(ev.id, activeEquipment.id, 'after')"
                                >▼ {{ $t('lz.label.tmsEquipmentEventRule.after') }}</button>
                            </div>
                        </div>
                    </div>
                    <el-empty v-else :image-size="100" :description="$t('lz.label.base.noData')" />
                </section>
            </div>
            <el-empty v-else :image-size="140" :description="$t('lz.label.base.noData')" />
        </el-card>

        <!-- Ma trận cấu hình: hàng = loại sự kiện, cột = loại thiết bị -->
        <el-card v-else shadow="never" class="matrix-card" v-loading="state.loading">
            <div v-if="filteredEventTypes.length && visibleEquipmentTypes.length" class="matrix-scroll">
                <!-- table-layout: fixed + colgroup: tiêu đề và ô luôn thẳng cột, kể cả khi tên loại thiết bị dài -->
                <table class="rule-matrix" :style="{ minWidth: `calc(${MATRIX_EVENT_COL_WIDTH}px + ${visibleEquipmentTypes.length} * ${MATRIX_EQUIP_COL_WIDTH}px)` }">
                    <colgroup>
                        <col :style="{ width: `${MATRIX_EVENT_COL_WIDTH}px` }" />
                        <col v-for="eq in visibleEquipmentTypes" :key="eq.id" :style="{ width: `${MATRIX_EQUIP_COL_WIDTH}px` }" />
                    </colgroup>
                    <thead>
                        <tr>
                            <th class="col-event sticky-col sticky-head">{{ $t('lz.entity.tms.eventType') }}</th>
                            <th v-for="eq in visibleEquipmentTypes" :key="eq.id" class="col-equip sticky-head">
                                <el-dropdown trigger="click" @command="(m: BulkMode) => bulkCol(eq.id, m)">
                                    <div class="head-trigger" :title="eq.name">
                                        <span class="head-name">{{ eq.name }}</span>
                                        <span class="count-pill" :class="{ 'count-pill--on': colCount(eq.id) > 0 }">{{ colCount(eq.id) }}</span>
                                    </div>
                                    <template #dropdown>
                                        <el-dropdown-menu>
                                            <el-dropdown-item command="both">{{ $t('lz.label.tmsEquipmentEventRule.setAllBoth') }}</el-dropdown-item>
                                            <el-dropdown-item command="before">{{ $t('lz.label.tmsEquipmentEventRule.setAllBefore') }}</el-dropdown-item>
                                            <el-dropdown-item command="after">{{ $t('lz.label.tmsEquipmentEventRule.setAllAfter') }}</el-dropdown-item>
                                            <el-dropdown-item command="none" divided>{{ $t('lz.label.tmsEquipmentEventRule.clearAll') }}</el-dropdown-item>
                                        </el-dropdown-menu>
                                    </template>
                                </el-dropdown>
                            </th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="ev in filteredEventTypes" :key="ev.id">
                            <td class="col-event sticky-col">
                                <el-dropdown trigger="click" class="row-dropdown" @command="(m: BulkMode) => bulkRow(ev.id, m)">
                                    <div class="event-cell" :title="ev.name">
                                        <span class="event-name">{{ ev.name }}</span>
                                        <span class="count-pill" :class="{ 'count-pill--on': rowCount(ev.id) > 0 }">{{ rowCount(ev.id) }}</span>
                                    </div>
                                    <template #dropdown>
                                        <el-dropdown-menu>
                                            <el-dropdown-item command="both">{{ $t('lz.label.tmsEquipmentEventRule.setAllBoth') }}</el-dropdown-item>
                                            <el-dropdown-item command="before">{{ $t('lz.label.tmsEquipmentEventRule.setAllBefore') }}</el-dropdown-item>
                                            <el-dropdown-item command="after">{{ $t('lz.label.tmsEquipmentEventRule.setAllAfter') }}</el-dropdown-item>
                                            <el-dropdown-item command="none" divided>{{ $t('lz.label.tmsEquipmentEventRule.clearAll') }}</el-dropdown-item>
                                        </el-dropdown-menu>
                                    </template>
                                </el-dropdown>
                            </td>
                            <td v-for="eq in visibleEquipmentTypes" :key="eq.id" class="cell">
                                <div class="cell-marks">
                                    <button
                                        type="button"
                                        class="mark mark--before"
                                        :class="{ 'mark--on': getCell(ev.id, eq.id).before, 'mark--dirty': isCellDirty(ev.id, eq.id, 'before') }"
                                        :title="$t('lz.label.tmsEquipmentEventRule.before')"
                                        @click="toggleCell(ev.id, eq.id, 'before')"
                                    >▲</button>
                                    <button
                                        type="button"
                                        class="mark mark--after"
                                        :class="{ 'mark--on': getCell(ev.id, eq.id).after, 'mark--dirty': isCellDirty(ev.id, eq.id, 'after') }"
                                        :title="$t('lz.label.tmsEquipmentEventRule.after')"
                                        @click="toggleCell(ev.id, eq.id, 'after')"
                                    >▼</button>
                                </div>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
            <el-empty v-else :image-size="140" :description="$t('lz.label.base.noData')" />
        </el-card>
    </div>
</template>

<script lang="ts" setup name="tmsEquipmentEventRule">
import { computed, onMounted, reactive } from 'vue';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { TmsEquipmentEventRuleApi, TmsEventTypeApi, TmsEquipmentTypeApi } from '/@/api-services/tms';
import type { EquipmentEventRuleOutput, AddEquipmentEventRuleInput } from '/@/api-services/tms';
import { TmsEquipmentEventRuleProcessType } from '/@/types/enums/tmsEnum';
import { StatusTypeEnum } from '/@/types/enums/baseEnum';

const { t: $t } = useI18n();

/** Trạng thái 1 ô ma trận (1 loại sự kiện x 1 loại thiết bị) */
interface CellState {
    before: boolean;
    after: boolean;
    beforeRuleId?: string | null;
    afterRuleId?: string | null;
}

const state = reactive({
    loading: false,
    saving: false,
    search: '',
    onlyConfigured: false, // chỉ hiện loại sự kiện đã có cấu hình
    viewMode: 'byEquipment' as 'byEquipment' | 'matrix', // chế độ hiển thị
    activeEquipmentId: '' as string, // loại thiết bị đang chọn ở chế độ "theo thiết bị"
    eventTypes: [] as any[], // hàng
    equipmentTypes: [] as any[], // cột: TẤT CẢ loại thiết bị (động, không cố định VMS/LCS)
    selectedTypeIds: [] as string[], // lọc cột hiển thị; rỗng = hiển thị tất cả
    matrix: {} as Record<string, CellState>, // trạng thái đang chỉnh
    original: {} as Record<string, CellState>, // snapshot gốc để so diff
});

// Các loại thiết bị hiển thị làm cột theo bộ lọc (rỗng -> tất cả)
const visibleEquipmentTypes = computed(() => {
    if (!state.selectedTypeIds.length) return state.equipmentTypes;
    return state.equipmentTypes.filter((eq: any) => state.selectedTypeIds.includes(eq.id));
});

// Độ rộng cột ở chế độ ma trận (px)
const MATRIX_EVENT_COL_WIDTH = 260;
const MATRIX_EQUIP_COL_WIDTH = 150;

// Loại thiết bị đang chọn (mặc định: cái đầu tiên đang hiển thị)
const activeEquipment = computed(() => visibleEquipmentTypes.value.find((eq: any) => eq.id === state.activeEquipmentId) ?? visibleEquipmentTypes.value[0]);

// Khóa ô ma trận
const cellKey = (eventTypeId: string, equipmentTypeId: string) => `${eventTypeId}__${equipmentTypeId}`;

// Lấy ô ma trận (đã được khởi tạo sẵn khi load)
const getCell = (eventTypeId: string, equipmentTypeId: string): CellState => {
    const key = cellKey(eventTypeId, equipmentTypeId);
    if (!state.matrix[key]) state.matrix[key] = { before: false, after: false };
    return state.matrix[key];
};

// Chuẩn hóa chuỗi: thường hóa + bỏ dấu tiếng Việt để tìm kiếm không phân biệt hoa/thường và dấu
// (vd gõ "chay" vẫn khớp "Cháy", "tai nan" khớp "Tai nạn")
const normalizeText = (value: any) =>
    (value ?? '')
        .toString()
        .toLowerCase()
        .normalize('NFD')
        .replace(/[\u0300-\u036f]/g, '')
        .replace(/đ/g, 'd')
        .trim();

// Lọc loại sự kiện theo ô tìm kiếm (không phân biệt dấu/hoa thường)
const filteredEventTypes = computed(() => {
    const keyword = normalizeText(state.search);
    return state.eventTypes.filter((ev: any) => {
        if (keyword && !normalizeText(ev.name).includes(keyword)) return false;
        if (state.onlyConfigured && rowCount(ev.id) === 0) return false;
        return true;
    });
});

// Ô có cấu hình (trước hoặc sau)
const isCellActive = (eventTypeId: string, equipmentTypeId: string) => {
    const cell = getCell(eventTypeId, equipmentTypeId);
    return cell.before || cell.after;
};

// Số ô đang bật theo hàng (loại sự kiện) / theo cột (loại thiết bị) — mỗi ô tối đa 2 (Trước + Sau)
const rowCount = (eventTypeId: string) =>
    state.equipmentTypes.reduce((n: number, eq: any) => {
        const c = getCell(eventTypeId, eq.id);
        return n + (c.before ? 1 : 0) + (c.after ? 1 : 0);
    }, 0);
const colCount = (equipmentTypeId: string) =>
    state.eventTypes.reduce((n: number, ev: any) => {
        const c = getCell(ev.id, equipmentTypeId);
        return n + (c.before ? 1 : 0) + (c.after ? 1 : 0);
    }, 0);

// Ô đã khác snapshot gốc (chưa lưu)
const isCellDirty = (eventTypeId: string, equipmentTypeId: string, which: 'before' | 'after') => {
    const key = cellKey(eventTypeId, equipmentTypeId);
    return !!state.matrix[key]?.[which] !== !!state.original[key]?.[which];
};

type BulkMode = 'both' | 'before' | 'after' | 'none';
const applyMode = (cell: CellState, mode: BulkMode) => {
    cell.before = mode === 'both' || mode === 'before';
    cell.after = mode === 'both' || mode === 'after';
};
// Thiết lập hàng loạt cho 1 hàng (tất cả loại thiết bị đang hiển thị)
const bulkRow = (eventTypeId: string, mode: BulkMode) => {
    visibleEquipmentTypes.value.forEach((eq: any) => applyMode(getCell(eventTypeId, eq.id), mode));
};
// Thiết lập hàng loạt cho 1 cột (tất cả loại sự kiện đang hiển thị sau lọc)
const bulkCol = (equipmentTypeId: string, mode: BulkMode) => {
    filteredEventTypes.value.forEach((ev: any) => applyMode(getCell(ev.id, equipmentTypeId), mode));
};

// Bật/tắt 1 loại xử lý trong ô
const toggleCell = (eventTypeId: string, equipmentTypeId: string, which: 'before' | 'after') => {
    const cell = getCell(eventTypeId, equipmentTypeId);
    cell[which] = !cell[which];
};

// So sánh matrix hiện tại với snapshot gốc -> danh sách thêm/xóa
const buildDiff = () => {
    const adds: AddEquipmentEventRuleInput[] = [];
    const deletes: { id: string }[] = [];

    state.eventTypes.forEach((ev: any) => {
        state.equipmentTypes.forEach((eq: any) => {
            const key = cellKey(ev.id, eq.id);
            const cur = state.matrix[key] ?? { before: false, after: false };
            const orig = state.original[key] ?? { before: false, after: false };

            // Loại xử lý: Trước
            if (cur.before && !orig.before) {
                adds.push({ eventTypeId: ev.id, equipmentTypeId: eq.id, processType: TmsEquipmentEventRuleProcessType.Before, status: StatusTypeEnum.Enable });
            } else if (!cur.before && orig.before && orig.beforeRuleId) {
                deletes.push({ id: orig.beforeRuleId });
            }

            // Loại xử lý: Sau
            if (cur.after && !orig.after) {
                adds.push({ eventTypeId: ev.id, equipmentTypeId: eq.id, processType: TmsEquipmentEventRuleProcessType.After, status: StatusTypeEnum.Enable });
            } else if (!cur.after && orig.after && orig.afterRuleId) {
                deletes.push({ id: orig.afterRuleId });
            }
        });
    });

    return { adds, deletes };
};

// Tổng số thay đổi chưa lưu
const changeCount = computed(() => {
    const { adds, deletes } = buildDiff();
    return adds.length + deletes.length;
});

// Nạp toàn bộ loại thiết bị (động) làm cột. Không cố định VMS/LCS -> có loại mới là tự có cột mới.
const loadEquipmentTypes = async () => {
    const res = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet();
    state.equipmentTypes = res.data.result ?? [];
};

// Nạp loại sự kiện làm hàng
const loadEventTypes = async () => {
    const res = await getAPI(TmsEventTypeApi).apiTmsTmseventtypeListGet();
    state.eventTypes = (res.data.result ?? []).filter((item: any) => item.code !== 'unknown');
};

// Nạp rule hiện có và dựng lại ma trận + snapshot gốc
const loadRules = async () => {
    const res = await getAPI(TmsEquipmentEventRuleApi).apiTmsTmsequipmenteventruleListGet();
    const rules = (res.data.result ?? []) as EquipmentEventRuleOutput[];

    // Khởi tạo toàn bộ ô rỗng để phản ứng ổn định
    const matrix: Record<string, CellState> = {};
    state.eventTypes.forEach((ev: any) => {
        state.equipmentTypes.forEach((eq: any) => {
            matrix[cellKey(ev.id, eq.id)] = { before: false, after: false };
        });
    });

    // Áp rule vào ma trận
    rules.forEach((rule) => {
        if (!rule.eventTypeId || !rule.equipmentTypeId) return;
        const key = cellKey(rule.eventTypeId, rule.equipmentTypeId);
        if (!matrix[key]) matrix[key] = { before: false, after: false };
        if (rule.processType === TmsEquipmentEventRuleProcessType.Before) {
            matrix[key].before = true;
            matrix[key].beforeRuleId = rule.id;
        } else if (rule.processType === TmsEquipmentEventRuleProcessType.After) {
            matrix[key].after = true;
            matrix[key].afterRuleId = rule.id;
        }
    });

    state.matrix = matrix;
    state.original = JSON.parse(JSON.stringify(matrix));
};

// Tải toàn bộ dữ liệu
const loadAll = async () => {
    state.loading = true;
    try {
        await Promise.all([loadEventTypes(), loadEquipmentTypes()]);
        await loadRules();
    } catch (error) {
        console.error('loadAll equipmentEventRule', error);
    } finally {
        state.loading = false;
    }
};

// Tải lại
const reload = () => loadAll();

// Hủy thay đổi -> quay lại snapshot gốc
const resetChanges = () => {
    state.matrix = JSON.parse(JSON.stringify(state.original));
};

// Lưu: thêm rule mới, xóa rule bỏ chọn (batch), rồi tải lại
const save = async () => {
    const { adds, deletes } = buildDiff();
    if (!adds.length && !deletes.length) {
        ElMessage.info($t('lz.label.tmsEquipmentEventRule.noChange'));
        return;
    }

    state.saving = true;
    try {
        if (adds.length) {
            await Promise.all(adds.map((rule) => getAPI(TmsEquipmentEventRuleApi).apiTmsTmsequipmenteventruleAddPost(rule)));
        }
        if (deletes.length) {
            await getAPI(TmsEquipmentEventRuleApi).apiTmsTmsequipmenteventruleBatchdeletePost(deletes);
        }
        ElMessage.success($t('lz.message.base.saveSuccess'));
        await loadRules();
    } catch (error) {
        console.error('save equipmentEventRule', error);
    } finally {
        state.saving = false;
    }
};

onMounted(loadAll);
</script>

<style scoped lang="scss">
.event-rule-container {
    display: flex;
    flex-direction: column;
    height: 100%;
    gap: 8px;
}

.toolbar-card {
    flex: 0 0 auto;
}

.toolbar {
    display: flex;
    align-items: center;
    justify-content: space-between;
    flex-wrap: wrap;
    gap: 10px;

    .toolbar-left,
    .toolbar-right {
        display: flex;
        align-items: center;
        gap: 10px;
        flex-wrap: wrap;
    }

    .toolbar-left {
        flex: 1 1 auto;
        min-width: 0;
    }

    .toolbar-right {
        flex: 0 0 auto;
    }

    .search-input {
        width: 220px;
    }

    .type-filter {
        width: 240px;
    }

    :deep(.el-select__wrapper),
    :deep(.el-input__wrapper) {
        min-height: 32px;
    }

    :deep(.el-checkbox) {
        height: 32px;
        margin-right: 0;
    }
}

.legend {
    display: flex;
    align-items: center;
    gap: 12px;
    height: 32px;
    padding: 0 12px 0 0;
    margin-right: 2px;
    border-right: 1px solid var(--el-border-color-lighter);
    font-size: 12px;
    white-space: nowrap;
    color: var(--el-text-color-secondary);
    cursor: help;

    .legend-item {
        display: inline-flex;
        align-items: center;
        gap: 4px;
    }
}

.matrix-card {
    flex: 1 1 0%;
    overflow: hidden;

    :deep(.el-card__body) {
        height: 100%;
        padding: 0;
    }
}

/* Chế độ theo thiết bị: danh sách thiết bị | danh sách sự kiện */
.split {
    display: flex;
    height: 100%;
}

.split-side {
    flex: 0 0 clamp(200px, 22%, 260px);
    overflow: auto;
    padding: 10px 8px;
    border-right: 1px solid var(--el-border-color-lighter);
    background: var(--el-fill-color-extra-light);
}

.side-item {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 8px;
    min-height: 36px;
    padding: 6px 10px;
    margin-bottom: 2px;
    font-size: 13px;
    border-radius: 6px;
    border-left: 3px solid transparent;
    cursor: pointer;
    transition: background-color 0.15s ease;

    &:hover {
        background: var(--el-fill-color);
    }

    &--active {
        background: var(--el-color-primary-light-9) !important;
        border-left-color: var(--el-color-primary);
        color: var(--el-color-primary);
        font-weight: 600;
    }
}

.side-name {
    flex: 1 1 auto;
    min-width: 0;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.split-main {
    flex: 1 1 0%;
    min-width: 0;
    display: flex;
    flex-direction: column;
}

.main-head {
    flex: 0 0 auto;
    display: flex;
    align-items: center;
    justify-content: space-between;
    flex-wrap: wrap;
    gap: 8px 16px;
    min-height: 52px;
    padding: 8px 20px;
    border-bottom: 1px solid var(--el-border-color-lighter);

    .main-title {
        min-width: 0;
        font-size: 15px;
        font-weight: 600;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
    }

    .bulk-actions {
        display: flex;
        align-items: center;
        flex-wrap: wrap;
        gap: 6px;

        .el-button {
            margin: 0;
        }
    }

    .main-sub {
        margin-left: 8px;
        font-size: 12px;
        font-weight: 400;
        color: var(--el-text-color-secondary);
    }
}

.event-list {
    flex: 1 1 0%;
    overflow: auto;
    padding: 6px 20px 16px;
}

.event-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 16px;
    max-width: 960px;
    min-height: 44px;
    padding: 6px 10px;
    border-bottom: 1px solid var(--el-border-color-extra-light);
    border-left: 3px solid transparent;

    &:hover {
        background: var(--el-fill-color-light);
    }

    &--active {
        border-left-color: var(--el-color-primary);
    }
}

.event-row-name {
    flex: 1 1 auto;
    min-width: 0;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

/* Màn hình hẹp: danh sách thiết bị xếp trên, sự kiện bên dưới */
@media (max-width: 900px) {
    .split {
        flex-direction: column;
    }

    .split-side {
        flex: 0 0 auto;
        max-height: 160px;
        border-right: none;
        border-bottom: 1px solid var(--el-border-color-lighter);
    }

    .event-row {
        max-width: none;
    }
}

.matrix-scroll {
    height: 100%;
    overflow: auto;
}

.rule-matrix {
    width: 100%;
    table-layout: fixed;
    border-collapse: separate;
    border-spacing: 0;
    font-size: 13px;

    th,
    td {
        border-bottom: 1px solid var(--el-border-color-extra-light);
        padding: 4px 8px;
        text-align: center;
        vertical-align: middle;
    }

    .sticky-head {
        position: sticky;
        top: 0;
        z-index: 2;
        background: var(--el-fill-color-light);
        font-weight: 600;
        color: var(--el-text-color-primary);
        border-bottom: 1px solid var(--el-border-color-lighter);
    }

    .sticky-col {
        position: sticky;
        left: 0;
        z-index: 1;
        background: var(--el-bg-color);
        border-right: 1px solid var(--el-border-color-lighter);
    }

    th.sticky-col.sticky-head {
        z-index: 3;
        background: var(--el-fill-color-light);
    }

    .col-event {
        padding: 6px 12px;
        text-align: left;
    }

    .col-equip {
        padding: 8px 10px;
        line-height: 1.3;
    }

    td.cell {
        padding: 6px 10px;
    }

    /* Kẻ sọc nhẹ để dò theo hàng */
    tbody tr:nth-child(even) td:not(.sticky-col) {
        background: color-mix(in srgb, var(--el-fill-color-light) 45%, transparent);
    }

    tbody tr:hover td {
        background: color-mix(in srgb, var(--el-color-primary) 8%, var(--el-bg-color));
    }
}

.head-trigger,
.event-cell {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 8px;
    cursor: pointer;
    outline: none;
}

.head-trigger {
    justify-content: center;
    min-width: 0;
}

/* Tên cột dài: cho xuống tối đa 2 dòng thay vì bị cắt/đè */
.head-trigger .head-name {
    min-width: 0;
    white-space: normal;
    word-break: break-word;
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    text-align: center;
}

.head-name,
.event-name {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.event-name {
    font-weight: 500;
    color: var(--el-text-color-primary);
}

.row-dropdown {
    display: block;
    width: 100%;
}

/* Số cấu hình đang bật của hàng/cột */
.count-pill {
    flex: 0 0 auto;
    min-width: 18px;
    height: 18px;
    line-height: 18px;
    padding: 0 5px;
    border-radius: 9px;
    font-size: 11px;
    font-weight: 500;
    color: var(--el-text-color-secondary);
    background: var(--el-fill-color);

    &--on {
        color: var(--el-color-primary);
        background: var(--el-color-primary-light-9);
    }
}

.cell-marks {
    display: inline-flex;
    flex: 0 0 auto;
    gap: 6px;
}

/* Nút bật/tắt gọn: ▲ Trước (primary), ▼ Sau (warning) */
.mark {
    position: relative;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 26px;
    height: 24px;
    padding: 0;
    font-size: 10px;
    font-style: normal;
    line-height: 1;
    border-radius: 5px;
    border: 1px solid var(--el-border-color-lighter);
    background: var(--el-bg-color);
    color: var(--el-text-color-placeholder);
    cursor: pointer;
    user-select: none;
    transition: background-color 0.15s ease, color 0.15s ease, border-color 0.15s ease, transform 0.1s ease;

    &:hover {
        transform: translateY(-1px);
    }

    &--before:hover {
        border-color: var(--el-color-primary);
        color: var(--el-color-primary);
    }

    &--after:hover {
        border-color: var(--el-color-warning);
        color: var(--el-color-warning);
    }

    &--before.mark--on {
        background: var(--el-color-primary);
        border-color: var(--el-color-primary);
        color: #fff;
    }

    &--after.mark--on {
        background: var(--el-color-warning);
        border-color: var(--el-color-warning);
        color: #fff;
    }

    /* Chấm báo thay đổi chưa lưu */
    &--dirty::after {
        content: '';
        position: absolute;
        top: -3px;
        right: -3px;
        width: 7px;
        height: 7px;
        border-radius: 50%;
        background: var(--el-color-danger);
        border: 1px solid var(--el-bg-color);
    }

    &--wide {
        width: auto;
        min-width: 84px;
        height: 28px;
        padding: 0 12px;
        gap: 4px;
        font-size: 12px;
        white-space: nowrap;
    }

    &--static {
        width: 18px;
        height: 16px;
        font-size: 9px;
        cursor: help;
        pointer-events: none;
    }
}
</style>
