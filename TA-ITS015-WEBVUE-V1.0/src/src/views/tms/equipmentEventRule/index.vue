<template>
    <div class="event-rule-container">
        <!-- Bộ lọc: thu gọn/mở rộng và kéo đổi chiều cao bằng PanelSplitter -->
        <el-card v-show="searchPanel.visible" :id="searchPanel.id" :ref="searchPanel.setPanel" :style="searchPanel.panelStyle" shadow="never" class="toolbar-card" :body-style="{ padding: '10px 12px' }">
            <div class="toolbar">
                <div class="toolbar-left">
                    <el-input v-model="state.search" :placeholder="$t('lz.label.tmsEquipmentEventRule.searchEvent')" clearable prefix-icon="ele-Search" class="search-input" />
                    <el-checkbox v-model="state.onlyConfigured">{{ $t('lz.label.tmsEquipmentEventRule.onlyConfigured') }}</el-checkbox>
                </div>
                <el-button type="primary" plain icon="ele-Plus" @click="openAddDialog">{{ $t('lz.label.tmsEquipmentEventRule.addEquipmentType') }}</el-button>
            </div>
        </el-card>
        <PanelSplitter side="left" orientation="horizontal" :style="searchPanel.splitterStyle" :controls="searchPanel.id" :label="$t('lz.button.base.search')" :resize-hint="$t('lz.label.base.resizePanel', { name: $t('lz.button.base.search') })" :expand-label="$t('lz.label.base.extend')" :collapse-label="$t('lz.button.base.collapse')" :expanded="searchPanel.visible" :width="searchPanel.size" :min="searchPanel.min" :max="searchPanel.max" @resize="searchPanel.resize" @step="searchPanel.step" />

        <!-- Thanh hành động luôn hiển thị (kể cả khi thu gọn bộ lọc): chú giải + gợi ý kéo chọn + làm mới / đặt lại / lưu -->
        <div class="action-bar">
            <div class="legend-wrap">
                <el-tooltip :content="$t('lz.label.tmsEquipmentEventRule.legend')" placement="bottom" :show-after="200">
                    <div class="legend">
                        <span class="legend-item"><i class="legend-box legend-box--before">▲</i>{{ $t('lz.label.tmsEquipmentEventRule.before') }}</span>
                        <span class="legend-item"><i class="legend-box legend-box--after">▼</i>{{ $t('lz.label.tmsEquipmentEventRule.after') }}</span>
                    </div>
                </el-tooltip>
                <span class="drag-hint"><el-icon><ele-Pointer /></el-icon>{{ $t('lz.label.tmsEquipmentEventRule.dragHint') }}</span>
            </div>
            <div class="action-buttons">
                <el-badge :value="changeCount" :hidden="changeCount === 0" type="warning">
                    <el-button icon="ele-Refresh" @click="reload">{{ $t('lz.button.base.refresh') }}</el-button>
                </el-badge>
                <el-button :disabled="changeCount === 0 && !state.removedTypeIds.length" @click="resetChanges">{{ $t('lz.button.base.reset') }}</el-button>
                <el-button type="primary" icon="ele-Select" :loading="state.saving" :disabled="changeCount === 0" v-auth="'tmsEquipmentEventRule:update'" @click="save">
                    {{ $t('lz.button.base.save') }}
                    <span v-if="changeCount > 0">&nbsp;({{ changeCount }})</span>
                </el-button>
            </div>
        </div>

        <!-- Ma trận cấu hình: hàng = loại sự kiện, cột = loại thiết bị (mỗi loại 2 ô con: ▲ Trước | ▼ Sau) -->
        <el-card shadow="never" class="matrix-card" v-loading="state.loading">
            <div v-if="filteredEventTypes.length && visibleEquipmentTypes.length" ref="scrollRef" class="matrix-scroll">
                <!-- table-layout: fixed + colgroup: tiêu đề và ô luôn thẳng cột, kể cả khi tên loại thiết bị dài -->
                <table class="rule-matrix" :class="{ 'is-dragging': drag.active }" :style="{ minWidth: `${MATRIX_EVENT_COL_WIDTH + subColumns.length * MATRIX_SUB_COL_WIDTH}px` }">
                    <colgroup>
                        <col :style="{ width: `${MATRIX_EVENT_COL_WIDTH}px` }" />
                        <col v-for="sub in subColumns" :key="sub.key" :style="{ width: `${MATRIX_SUB_COL_WIDTH}px` }" />
                    </colgroup>
                    <thead>
                        <tr class="head-row">
                            <th rowspan="2" class="col-event sticky-col sticky-head">{{ $t('lz.entity.tms.eventType') }}</th>
                            <th v-for="eq in visibleEquipmentTypes" :key="eq.id" colspan="2" class="col-equip sticky-head group-end">
                                <div class="equip-head">
                                    <el-dropdown trigger="click" class="equip-dropdown" @command="(m: BulkMode) => bulkCol(eq.id, m)">
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
                                    <button type="button" class="head-remove" :title="$t('lz.label.tmsEquipmentEventRule.removeEquipmentType')" @click="removeEquipmentType(eq)">
                                        <el-icon><ele-Close /></el-icon>
                                    </button>
                                </div>
                            </th>
                        </tr>
                        <tr class="sub-head-row">
                            <th
                                v-for="sub in subColumns"
                                :key="sub.key"
                                class="sub-head sticky-head"
                                :class="[`sub-head--${sub.which}`, { 'group-end': sub.which === 'after' }]"
                                :title="$t('lz.label.tmsEquipmentEventRule.toggleColumn')"
                                @click="toggleSubColumn(sub)"
                            >
                                {{ sub.which === 'before' ? '▲' : '▼' }} {{ sub.which === 'before' ? $t('lz.label.tmsEquipmentEventRule.before') : $t('lz.label.tmsEquipmentEventRule.after') }}
                            </th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="(ev, r) in filteredEventTypes" :key="ev.id" :class="{ 'row-active': drag.active && inRowRange(r) }">
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
                            <!-- Ô con: bấm để bật/tắt, giữ chuột kéo để chọn cả vùng (giá trị đích = ngược với ô bắt đầu kéo) -->
                            <td
                                v-for="(sub, c) in subColumns"
                                :key="sub.key"
                                class="sub-cell"
                                :class="subCellClass(ev.id, sub, r, c)"
                                :data-r="r"
                                :data-c="c"
                                :title="`${ev.name} · ${sub.eqName} · ${sub.which === 'before' ? $t('lz.label.tmsEquipmentEventRule.before') : $t('lz.label.tmsEquipmentEventRule.after')}`"
                                @pointerdown="startDrag(r, c, $event)"
                            >
                                <span class="mark-box">{{ sub.which === 'before' ? '▲' : '▼' }}</span>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
            <el-empty v-else-if="!visibleEquipmentTypes.length && !state.loading" :image-size="140" :description="$t('lz.label.tmsEquipmentEventRule.noEquipmentType')">
                <el-button type="primary" icon="ele-Plus" @click="openAddDialog">{{ $t('lz.label.tmsEquipmentEventRule.addEquipmentType') }}</el-button>
            </el-empty>
            <el-empty v-else :image-size="140" :description="$t('lz.label.base.noData')" />
        </el-card>

        <!-- Thêm loại thiết bị vào ma trận (chỉ liệt kê loại chưa có) -->
        <el-dialog v-model="state.addDialogVisible" :title="$t('lz.label.tmsEquipmentEventRule.addEquipmentTypeTitle')" width="480px" append-to-body>
            <template v-if="hiddenEquipmentTypes.length">
                <el-input v-model="state.addSearch" clearable prefix-icon="ele-Search" :placeholder="$t('lz.button.base.search')" class="mb10" />
                <el-checkbox-group v-model="state.addSelection" class="add-type-list">
                    <el-checkbox v-for="eq in addCandidates" :key="eq.id" :value="eq.id" :label="eq.name" border />
                </el-checkbox-group>
                <el-empty v-if="!addCandidates.length" :image-size="80" :description="$t('lz.label.base.noData')" />
            </template>
            <el-empty v-else :image-size="100" :description="$t('lz.label.tmsEquipmentEventRule.allEquipmentTypesAdded')" />
            <template #footer>
                <el-button @click="state.addDialogVisible = false">{{ $t('lz.button.base.cancel') }}</el-button>
                <el-button type="primary" :disabled="!state.addSelection.length" @click="confirmAddTypes">
                    {{ $t('lz.label.tmsEquipmentEventRule.addEquipmentType') }}
                    <span v-if="state.addSelection.length">&nbsp;({{ state.addSelection.length }})</span>
                </el-button>
            </template>
        </el-dialog>
    </div>
</template>

<script lang="ts" setup name="tmsEquipmentEventRule">
import { computed, onBeforeUnmount, onMounted, reactive, ref } from 'vue';
import PanelSplitter from '/@/components/workspace/PanelSplitter.vue';
import { useSearchPanel } from '/@/components/workspace/useSearchPanel';
import { ElMessage, ElMessageBox } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { TmsEquipmentEventRuleApi, TmsEventTypeApi, TmsEquipmentTypeApi } from '/@/api-services/tms';
import type { EquipmentEventRuleOutput, AddEquipmentEventRuleInput } from '/@/api-services/tms';
import { TmsEquipmentEventRuleProcessType } from '/@/types/enums/tmsEnum';
import { StatusTypeEnum } from '/@/types/enums/baseEnum';

const { t: $t } = useI18n();

// Thu gọn/mở rộng và kéo đổi chiều cao vùng bộ lọc.
const searchPanel = useSearchPanel('equipment-event-rule-search');

/** Trạng thái 1 ô ma trận (1 loại sự kiện x 1 loại thiết bị) */
interface CellState {
    before: boolean;
    after: boolean;
    beforeRuleId?: string | null;
    afterRuleId?: string | null;
}

type Which = 'before' | 'after';

/** 1 cột con của ma trận: loại thiết bị x (Trước | Sau) */
interface SubColumn {
    key: string;
    eqId: string;
    eqName: string;
    which: Which;
}

// Loại thiết bị người dùng chủ động thêm vào ma trận (dù chưa cấu hình ô nào) - ghi nhớ trên trình duyệt
const ADDED_TYPES_STORAGE_KEY = 'tmsEquipmentEventRuleAddedTypes';
const readAddedTypes = (): string[] => {
    try {
        const value = JSON.parse(localStorage.getItem(ADDED_TYPES_STORAGE_KEY) ?? '[]');
        return Array.isArray(value) ? value : [];
    } catch {
        return [];
    }
};
const saveAddedTypes = () => {
    try {
        localStorage.setItem(ADDED_TYPES_STORAGE_KEY, JSON.stringify(state.addedTypeIds));
    } catch {
        // bỏ qua: không lưu được thì lần sau chỉ hiện loại đã có cấu hình
    }
};

const state = reactive({
    loading: false,
    saving: false,
    search: '',
    onlyConfigured: false, // chỉ hiện loại sự kiện đã có cấu hình
    eventTypes: [] as any[], // hàng
    equipmentTypes: [] as any[], // TẤT CẢ loại thiết bị (động, không cố định VMS/LCS)
    addedTypeIds: readAddedTypes() as string[], // loại thiết bị được thêm thủ công vào ma trận
    removedTypeIds: [] as string[], // loại thiết bị bị bỏ khỏi ma trận trong phiên (chờ Lưu)
    matrix: {} as Record<string, CellState>, // trạng thái đang chỉnh
    original: {} as Record<string, CellState>, // snapshot gốc để so diff
    addDialogVisible: false,
    addSearch: '',
    addSelection: [] as string[],
});

// Độ rộng cột ở ma trận (px): cột loại sự kiện + mỗi cột con Trước/Sau
const MATRIX_EVENT_COL_WIDTH = 260;
const MATRIX_SUB_COL_WIDTH = 68;

// Khóa ô ma trận
const cellKey = (eventTypeId: string, equipmentTypeId: string) => `${eventTypeId}__${equipmentTypeId}`;

// Lấy ô ma trận (đã được khởi tạo sẵn khi load)
const getCell = (eventTypeId: string, equipmentTypeId: string): CellState => {
    const key = cellKey(eventTypeId, equipmentTypeId);
    if (!state.matrix[key]) state.matrix[key] = { before: false, after: false };
    return state.matrix[key];
};

// Loại thiết bị có ít nhất 1 ô bật trong bảng trạng thái cho trước
const hasConfig = (source: Record<string, CellState>, equipmentTypeId: string) =>
    state.eventTypes.some((ev: any) => {
        const cell = source[cellKey(ev.id, equipmentTypeId)];
        return !!cell && (cell.before || cell.after);
    });

// Cột của ma trận = loại thiết bị đã có cấu hình (đã lưu hoặc đang chỉnh) + loại được thêm thủ công,
// trừ loại vừa bị bỏ trong phiên. Loại không cần quản lý thì không chiếm chỗ trên ma trận.
const visibleEquipmentTypes = computed(() =>
    state.equipmentTypes.filter(
        (eq: any) => !state.removedTypeIds.includes(eq.id) && (state.addedTypeIds.includes(eq.id) || hasConfig(state.original, eq.id) || hasConfig(state.matrix, eq.id))
    )
);

// Loại thiết bị chưa có trên ma trận (ứng viên của nút "Thêm loại thiết bị")
const hiddenEquipmentTypes = computed(() => {
    const visibleIds = new Set(visibleEquipmentTypes.value.map((eq: any) => eq.id));
    return state.equipmentTypes.filter((eq: any) => !visibleIds.has(eq.id));
});

// Cột con phẳng (Trước, Sau của từng loại thiết bị) - dùng làm chỉ số cột khi kéo chọn vùng
const subColumns = computed<SubColumn[]>(() =>
    visibleEquipmentTypes.value.flatMap((eq: any) =>
        (['before', 'after'] as Which[]).map((which) => ({ key: `${eq.id}__${which}`, eqId: eq.id, eqName: eq.name, which }))
    )
);

// Chuẩn hóa chuỗi: thường hóa + bỏ dấu tiếng Việt để tìm kiếm không phân biệt hoa/thường và dấu
// (vd gõ "chay" vẫn khớp "Cháy", "tai nan" khớp "Tai nạn")
const normalizeText = (value: any) =>
    (value ?? '')
        .toString()
        .toLowerCase()
        .normalize('NFD')
        .replace(/[̀-ͯ]/g, '')
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
const isCellDirty = (eventTypeId: string, equipmentTypeId: string, which: Which) => {
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

// Bấm tiêu đề cột con (▲ Trước / ▼ Sau): đã bật hết -> tắt hết, ngược lại -> bật hết
const toggleSubColumn = (sub: SubColumn) => {
    const rows = filteredEventTypes.value;
    const allOn = rows.length > 0 && rows.every((ev: any) => getCell(ev.id, sub.eqId)[sub.which]);
    rows.forEach((ev: any) => (getCell(ev.id, sub.eqId)[sub.which] = !allOn));
};

/* ---------- Kéo chuột chọn vùng ---------- */
// Giữ chuột tại 1 ô con rồi kéo: vùng chữ nhật (hàng x cột con) được tô xem trước, thả chuột thì áp dụng.
// Giá trị áp dụng = ngược với ô bắt đầu (bắt đầu ở ô đang tắt -> bật cả vùng, ô đang bật -> tắt cả vùng).
// Bấm 1 ô không kéo = vùng 1x1 = bật/tắt ô đó. Esc để hủy khi đang kéo.
const scrollRef = ref<HTMLElement>();
const drag = reactive({ active: false, r0: 0, c0: 0, r1: 0, c1: 0, value: true });

const inRowRange = (r: number) => r >= Math.min(drag.r0, drag.r1) && r <= Math.max(drag.r0, drag.r1);
const inSelection = (r: number, c: number) => drag.active && inRowRange(r) && c >= Math.min(drag.c0, drag.c1) && c <= Math.max(drag.c0, drag.c1);

const subCellClass = (eventTypeId: string, sub: SubColumn, r: number, c: number) => {
    const selecting = inSelection(r, c);
    return [
        `sub-cell--${sub.which}`,
        {
            'is-on': getCell(eventTypeId, sub.eqId)[sub.which],
            'is-dirty': isCellDirty(eventTypeId, sub.eqId, sub.which),
            'is-selecting': selecting,
            'preview-on': selecting && drag.value,
            'preview-off': selecting && !drag.value,
            'group-end': sub.which === 'after',
        },
    ];
};

const startDrag = (r: number, c: number, e: PointerEvent) => {
    if (e.button !== 0) return;
    e.preventDefault(); // không bôi đen chữ khi kéo
    const ev = filteredEventTypes.value[r];
    const sub = subColumns.value[c];
    if (!ev || !sub) return;
    Object.assign(drag, { active: true, r0: r, c0: c, r1: r, c1: c, value: !getCell(ev.id, sub.eqId)[sub.which] });
    window.addEventListener('pointermove', onDragMove);
    window.addEventListener('pointerup', endDrag);
    window.addEventListener('keydown', onDragKey);
};

// Tự cuộn khi kéo sát mép vùng ma trận (bỏ qua phần tiêu đề và cột loại sự kiện đang dính)
const autoScroll = (e: PointerEvent) => {
    const el = scrollRef.value;
    if (!el) return;
    const rect = el.getBoundingClientRect();
    const head = (el.querySelector('thead') as HTMLElement | null)?.offsetHeight ?? 0;
    const edge = 36;
    const step = 18;
    if (e.clientY > rect.bottom - edge) el.scrollTop += step;
    else if (e.clientY < rect.top + head + edge) el.scrollTop -= step;
    if (e.clientX > rect.right - edge) el.scrollLeft += step;
    else if (e.clientX < rect.left + MATRIX_EVENT_COL_WIDTH + edge) el.scrollLeft -= step;
};

const onDragMove = (e: PointerEvent) => {
    if (!drag.active) return;
    const target = (document.elementFromPoint(e.clientX, e.clientY) as HTMLElement | null)?.closest('td[data-c]') as HTMLElement | null;
    if (target) {
        drag.r1 = Number(target.dataset.r);
        drag.c1 = Number(target.dataset.c);
    }
    autoScroll(e);
};

const stopDrag = () => {
    drag.active = false;
    window.removeEventListener('pointermove', onDragMove);
    window.removeEventListener('pointerup', endDrag);
    window.removeEventListener('keydown', onDragKey);
};

const endDrag = () => {
    if (!drag.active) return;
    const rows = filteredEventTypes.value;
    const cols = subColumns.value;
    for (let r = Math.min(drag.r0, drag.r1); r <= Math.max(drag.r0, drag.r1); r++) {
        for (let c = Math.min(drag.c0, drag.c1); c <= Math.max(drag.c0, drag.c1); c++) {
            const ev = rows[r];
            const sub = cols[c];
            if (ev && sub) getCell(ev.id, sub.eqId)[sub.which] = drag.value;
        }
    }
    stopDrag();
};

const onDragKey = (e: KeyboardEvent) => {
    if (e.key === 'Escape') stopDrag();
};

onBeforeUnmount(stopDrag);

/* ---------- Thêm / bỏ loại thiết bị trên ma trận ---------- */
const addCandidates = computed(() => {
    const keyword = normalizeText(state.addSearch);
    return hiddenEquipmentTypes.value.filter((eq: any) => !keyword || normalizeText(eq.name).includes(keyword));
});

const openAddDialog = () => {
    state.addSearch = '';
    state.addSelection = [];
    state.addDialogVisible = true;
};

const confirmAddTypes = () => {
    state.addSelection.forEach((id) => {
        if (!state.addedTypeIds.includes(id)) state.addedTypeIds.push(id);
    });
    state.removedTypeIds = state.removedTypeIds.filter((id) => !state.addSelection.includes(id));
    saveAddedTypes();
    state.addDialogVisible = false;
};

// Bỏ loại thiết bị khỏi ma trận: xóa mọi ô của loại đó (với TẤT CẢ loại sự kiện, không chỉ đang lọc) -> xóa rule khi Lưu
const removeEquipmentType = async (eq: any) => {
    if (colCount(eq.id) > 0) {
        try {
            await ElMessageBox.confirm($t('lz.label.tmsEquipmentEventRule.removeEquipmentTypeConfirm', { name: eq.name }), $t('lz.label.tmsEquipmentEventRule.removeEquipmentType'), {
                type: 'warning',
            });
        } catch {
            return;
        }
    }
    state.eventTypes.forEach((ev: any) => applyMode(getCell(ev.id, eq.id), 'none'));
    state.addedTypeIds = state.addedTypeIds.filter((id) => id !== eq.id);
    if (!state.removedTypeIds.includes(eq.id)) state.removedTypeIds.push(eq.id);
    saveAddedTypes();
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

// Nạp toàn bộ loại thiết bị (động). Không cố định VMS/LCS -> có loại mới là có thể thêm vào ma trận.
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
    state.removedTypeIds = [];
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

// Hủy thay đổi -> quay lại snapshot gốc (loại thiết bị vừa bỏ cũng hiện lại)
const resetChanges = () => {
    state.matrix = JSON.parse(JSON.stringify(state.original));
    state.removedTypeIds = [];
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

    .toolbar-left {
        display: flex;
        align-items: center;
        gap: 10px;
        flex-wrap: wrap;
        min-width: 0;
    }

    .search-input {
        width: 260px;
    }

    :deep(.el-input__wrapper) {
        min-height: 32px;
    }

    :deep(.el-checkbox) {
        height: 32px;
        margin-right: 0;
    }
}

/* Thanh hành động: chú giải + gợi ý bên trái, nút bên phải */
.action-bar {
    flex: 0 0 auto;
    display: flex;
    align-items: center;
    justify-content: space-between;
    flex-wrap: wrap;
    gap: 10px;
    padding: 0 2px;

    .legend-wrap {
        display: flex;
        align-items: center;
        flex-wrap: wrap;
        gap: 4px 16px;
        min-width: 0;
    }

    .action-buttons {
        display: flex;
        align-items: center;
        gap: 8px;
    }
}

.legend {
    display: flex;
    align-items: center;
    gap: 12px;
    height: 32px;
    padding: 0 6px;
    font-size: 12px;
    white-space: nowrap;
    color: var(--el-text-color-secondary);
    cursor: help;

    .legend-item {
        display: inline-flex;
        align-items: center;
        gap: 6px;
    }
}

.legend-box {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 20px;
    height: 18px;
    border-radius: 4px;
    font-size: 9px;
    font-style: normal;
    color: #fff;

    &--before {
        background: var(--el-color-primary);
    }

    &--after {
        background: var(--el-color-warning);
    }
}

.drag-hint {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    font-size: 12px;
    color: var(--el-text-color-secondary);
}

.matrix-card {
    flex: 1 1 0%;
    overflow: hidden;

    :deep(.el-card__body) {
        height: 100%;
        padding: 0;
    }
}

.matrix-scroll {
    height: 100%;
    overflow: auto;
}

/* ---------- Ma trận ---------- */
.rule-matrix {
    --head-row-height: 52px;
    width: 100%;
    table-layout: fixed;
    border-collapse: separate;
    border-spacing: 0;
    font-size: 13px;

    th,
    td {
        border-bottom: 1px solid var(--el-border-color-extra-light);
        padding: 0;
        text-align: center;
        vertical-align: middle;
    }

    /* Vạch phân cách giữa các loại thiết bị (sau cột con "Sau") */
    .group-end {
        border-right: 1px solid var(--el-border-color-lighter);
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

    .head-row th {
        height: var(--head-row-height);
        box-sizing: border-box;
    }

    /* Hàng tiêu đề thứ 2 (▲ Trước | ▼ Sau) dính ngay dưới hàng tên loại thiết bị */
    .sub-head-row th {
        top: var(--head-row-height);
        height: 30px;
        font-size: 12px;
        font-weight: 500;
        cursor: pointer;
        user-select: none;
        transition: background-color 0.15s ease;
    }

    .sub-head--before {
        color: var(--el-color-primary);

        &:hover {
            background: var(--el-color-primary-light-9);
        }
    }

    .sub-head--after {
        color: var(--el-color-warning-dark-2);

        &:hover {
            background: var(--el-color-warning-light-9);
        }
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
        padding: 4px 6px 4px 10px;
        line-height: 1.3;
    }

    /* Kẻ sọc nhẹ + làm nổi hàng đang trỏ / đang kéo để dò theo hàng */
    tbody tr:nth-child(even) td:not(.sticky-col) {
        background: color-mix(in srgb, var(--el-fill-color-light) 45%, transparent);
    }

    tbody tr:hover td,
    tbody tr.row-active td {
        background: color-mix(in srgb, var(--el-color-primary) 6%, var(--el-bg-color));
    }

    &.is-dragging {
        cursor: crosshair;
    }
}

.equip-head {
    display: flex;
    align-items: center;
    gap: 4px;
}

.equip-dropdown {
    flex: 1 1 auto;
    min-width: 0;
}

.head-remove {
    flex: 0 0 auto;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 20px;
    height: 20px;
    padding: 0;
    border: none;
    border-radius: 4px;
    background: transparent;
    color: var(--el-text-color-placeholder);
    cursor: pointer;
    opacity: 0;
    transition: opacity 0.15s ease, color 0.15s ease, background-color 0.15s ease;

    &:hover {
        color: var(--el-color-danger);
        background: var(--el-color-danger-light-9);
    }

    &:focus-visible {
        opacity: 1;
        outline: 2px solid var(--el-color-primary);
    }
}

/* Nút bỏ loại thiết bị chỉ hiện khi trỏ vào tiêu đề cột -> tiêu đề gọn gàng */
.col-equip:hover .head-remove {
    opacity: 1;
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

/* ---------- Ô con Trước / Sau: cả ô là vùng bấm & kéo ---------- */
.sub-cell {
    position: relative;
    height: 40px;
    cursor: pointer;
    user-select: none;
    touch-action: none; /* kéo chọn trên màn cảm ứng không cuộn trang */

    .mark-box {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        width: 30px;
        height: 24px;
        border-radius: 5px;
        border: 1px dashed var(--el-border-color);
        font-size: 10px;
        line-height: 1;
        color: transparent;
        background: var(--el-bg-color);
        transition: background-color 0.12s ease, border-color 0.12s ease, color 0.12s ease;
    }

    &:hover .mark-box {
        border-style: solid;
    }

    &--before:hover .mark-box {
        border-color: var(--el-color-primary);
        color: var(--el-color-primary-light-5);
    }

    &--after:hover .mark-box {
        border-color: var(--el-color-warning);
        color: var(--el-color-warning-light-5);
    }

    /* Đang bật */
    &--before.is-on .mark-box {
        border: 1px solid var(--el-color-primary);
        background: var(--el-color-primary);
        color: #fff;
    }

    &--after.is-on .mark-box {
        border: 1px solid var(--el-color-warning);
        background: var(--el-color-warning);
        color: #fff;
    }

    /* Đang kéo chọn: nền vùng chọn + xem trước kết quả khi thả chuột */
    &.is-selecting {
        background: color-mix(in srgb, var(--el-color-primary) 14%, var(--el-bg-color)) !important;
    }

    &--before.preview-on .mark-box {
        border: 1px solid var(--el-color-primary);
        background: var(--el-color-primary-light-5);
        color: #fff;
    }

    &--after.preview-on .mark-box {
        border: 1px solid var(--el-color-warning);
        background: var(--el-color-warning-light-5);
        color: #fff;
    }

    &.preview-off .mark-box {
        border: 1px dashed var(--el-color-danger-light-5);
        background: var(--el-bg-color);
        color: var(--el-color-danger-light-5);
    }

    /* Chấm báo thay đổi chưa lưu */
    &.is-dirty::after {
        content: '';
        position: absolute;
        top: 6px;
        right: calc(50% - 19px);
        width: 7px;
        height: 7px;
        border-radius: 50%;
        background: var(--el-color-danger);
        border: 1px solid var(--el-bg-color);
    }
}

/* Danh sách loại thiết bị trong dialog thêm */
.add-type-list {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 8px;
    max-height: 360px;
    overflow-y: auto;

    :deep(.el-checkbox) {
        margin-right: 0;
        width: 100%;
        overflow: hidden;
    }

    :deep(.el-checkbox__label) {
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
    }
}
</style>
