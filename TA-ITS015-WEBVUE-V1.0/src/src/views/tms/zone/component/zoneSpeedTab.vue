<template>
	<div class="zone-tab">
		<!-- Tìm kiếm của tab "Tốc độ min/max" -->
		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', alignItems: 'start' }">
			<el-form :model="query" label-position="left" label-width="auto" style="flex: 1 1 0%">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<!-- Nhánh cha: chỉ liệt kê zone CÓ con; chọn nhánh ⇒ lọc về các zone con cháu của nhánh -->
						<el-form-item :label="$t('lz.entity.tmsZone.pid')">
							<el-tree-select
								v-model="query.parentIds"
								:data="parentTree"
								node-key="id"
								:props="{ label: 'name', children: 'children' }"
								multiple
								check-strictly
								filterable
								collapse-tags
								collapse-tags-tooltip
								:max-collapse-tags="1"
								:render-after-expand="false"
								clearable
								class="w100"
								:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tmsZone.pid') })"
							/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.tms.zone')">
							<el-input v-model="query.keyword" clearable :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.label.tmsZone.keyword') })" @keyup.enter.native="applyQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.direction')">
							<el-select v-model="query.direction" clearable class="w100" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.direction') })">
								<el-option v-for="item in directionOptions" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.location')">
							<el-select v-model="query.location" clearable class="w100" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.location') })">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TmsZoneLocation)" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.km')">
							<div class="km-range">
								<el-input-number v-model="query.fromKm" :min="0" :controls="false" :value-on-clear="null" :placeholder="$t('lz.label.base.from')" />
								<span>–</span>
								<el-input-number v-model="query.toKm" :min="0" :controls="false" :value-on-clear="null" :placeholder="$t('lz.label.base.to')" />
							</div>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="16" :lg="18" :xl="18">
						<el-form-item>
							<el-checkbox v-model="query.onlyVdsCamera">{{ $t('lz.label.tmsZone.onlyVdsCamera') }}</el-checkbox>
							<el-checkbox v-model="query.onlyOverride">{{ $t('lz.label.tmsZone.onlyOverride') }}</el-checkbox>
							<el-checkbox v-model="query.onlyDirty">{{ $t('lz.label.tmsZone.onlyDirty') }}</el-checkbox>
							<el-checkbox v-model="query.onlyInvalid">{{ $t('lz.label.tmsZone.onlyInvalid') }}</el-checkbox>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />
			<el-row>
				<SearchButton :auth-search="'tmsZone:page'" @search="applyQuery" @reset="resetQuery" :loading="state.loading" />
			</el-row>
		</el-card>

		<div class="zone-split">
			<!-- Cây zone: tick zone cha = chọn nhanh mọi zone con; bấm tên = thu phạm vi bên phải -->
			<div class="split-left">
				<zoneTreePanel
					:data="treeData"
					checkable
					:checked-keys="treeCheckedKeys"
					:current-key="state.scopeId"
					:dimmed-ids="pureParentIds"
					@update:checked-keys="onTreeCheck"
					@node-click="onNodeClick"
				>
					<template #badge="{ data }">
						<template v-if="!pureParentIds.has(data.id) && rowById.get(data.id)">
							<span
								class="speed-chip"
								:class="{ 'is-default': badgeOf(data.id).source === 'direction', 'is-invalid': !badgeOf(data.id).valid }"
								:style="chipStyle(data.id)"
							>{{ badgeOf(data.id).valid ? `${badgeOf(data.id).min}–${badgeOf(data.id).max}` : '⚠' }}</span>
							<i v-if="isDirty(rowById.get(data.id)!)" class="dirty-dot" :title="$t('lz.label.tmsZone.onlyDirty')" />
						</template>
					</template>
				</zoneTreePanel>
			</div>

			<div class="split-right" v-loading="state.loading || state.isProcessing" :element-loading-text="state.isProcessing ? $t('lz.label.base.saving') : undefined">
				<!-- Phạm vi + mặc định theo hướng (chỉ xem; sửa ở trang cấu hình VDS) -->
				<div class="right-head">
					<div>
						<div class="right-title">
							{{ scopeZone ? scopeZone.name || scopeZone.code : $t('lz.label.tmsZone.allZones') }}
							<el-button v-if="scopeZone" plain type="primary" icon="ele-Back" @click="state.scopeId = null">{{ $t('lz.button.base.viewAll') }}</el-button>
						</div>
						<div class="right-sub">{{ $t('lz.label.tmsZone.zoneCount', { count: scopeRows.length }) }}</div>
					</div>
					<span class="spacer" />
					<div class="defaults">
						<span class="defaults-title">{{ $t('lz.label.tmsZone.directionDefaults') }}:</span>
						<template v-if="state.directionDefaults.length">
							<el-tag v-for="d in state.directionDefaults" :key="d.direction" type="info" effect="plain" round size="small">
								{{ directionLabel(d.direction) }}: <b>{{ d.minSpeed }}–{{ d.maxSpeed }}</b>
							</el-tag>
						</template>
						<el-tag v-else type="danger" effect="plain" size="small">{{ $t('lz.label.tmsZone.noDirectionDefaults') }}</el-tag>
						<el-tooltip :content="$t('lz.label.tmsZone.directionDefaultsHint')" placement="top">
							<el-icon class="hint-icon"><ele-QuestionFilled /></el-icon>
						</el-tooltip>
					</div>
				</div>

				<!-- Sơ đồ dải Km: khung thu gọn (giống trang bản đồ) để nhường chỗ cho bảng khi không cần xem -->
				<Fieldset
					toggleable
					:collapsed="state.stripCollapsed"
					:legend="`${$t('lz.label.tmsZone.speedStrip')} (${$t('lz.label.tmsZone.zoneCount', { count: stripItems.length })})`"
					class="strip-fieldset"
					@update:collapsed="onStripCollapsed"
				>
					<speedStrip :items="stripItems" :direction-label="directionLabel" @toggle="toggleSelect" />
				</Fieldset>

				<!-- Lưu/bỏ thay đổi đặt NGAY TRÊN bảng để thấy được mà không phải cuộn xuống cuối -->
				<div class="save-bar">
					<span v-if="dirtyIds.length" class="dirty-text">
						{{ $t('lz.label.tmsZone.dirtyCount', { count: dirtyIds.length }) }}
						<template v-if="dirtyInvalidCount"> · <span class="text-danger">{{ $t('lz.label.tmsZone.dirtyInvalidCount', { count: dirtyInvalidCount }) }}</span></template>
					</span>
					<span v-else class="text-muted">{{ $t('lz.label.base.noChanges') }}</span>
					<span class="spacer" />
					<el-button :disabled="!dirtyIds.length || state.isProcessing" @click="discard">{{ $t('lz.button.base.discardChanges') }}</el-button>
					<el-button type="primary" :disabled="!dirtyIds.length || state.isProcessing" v-auth="'tmsZone:update'" @click="save">
						{{ $t('lz.button.tmsZone.saveChanges', { count: dirtyIds.length }) }}
					</el-button>
				</div>

				<div class="grid-wrap">
					<vxe-grid ref="gridRef" class="xGrid-style zone-speed-grid" v-bind="gridOptions" :data="scopeRows" :row-class-name="rowClass" @checkbox-change="onGridCheck" @checkbox-all="onGridCheck">
						<!-- Áp hàng loạt cho các zone đã chọn -->
						<template #toolbar_buttons>
							<div class="bulk-bar" :class="{ 'is-active': selectedCount > 0 }">
								<span>{{ $t('lz.label.tmsZone.selectedZones', { count: selectedCount }) }}</span>
								<template v-if="selectedCount">
									<el-divider direction="vertical" />
									<span>{{ $t('lz.entity.tmsZone.minSpeed') }}</span>
									<el-input-number v-model="bulk.min" class="bulk-input" :min="1" :max="ZONE_SPEED_MAX_KMH" :step="5" :precision="0" :value-on-clear="null"
										:disabled="bulk.clearMin || state.isProcessing" :placeholder="$t('lz.label.base.keepCurrent')" />
									<el-checkbox v-model="bulk.clearMin" :disabled="state.isProcessing">{{ $t('lz.label.tmsZone.resetToDefault') }}</el-checkbox>
									<span>{{ $t('lz.entity.tmsZone.maxSpeed') }}</span>
									<el-input-number v-model="bulk.max" class="bulk-input" :min="1" :max="ZONE_SPEED_MAX_KMH" :step="5" :precision="0" :value-on-clear="null"
										:disabled="bulk.clearMax || state.isProcessing" :placeholder="$t('lz.label.base.keepCurrent')" />
									<el-checkbox v-model="bulk.clearMax" :disabled="state.isProcessing">{{ $t('lz.label.tmsZone.resetToDefault') }}</el-checkbox>
									<el-button type="primary" :disabled="!bulkHasAction || state.isProcessing" @click="applyBulk">
										{{ $t('lz.button.tmsZone.applyToSelected', { count: selectedCount }) }}
									</el-button>
								</template>
							</div>
						</template>
						<template #toolbar_tools>
							<TableHeader :show-add="false" :show-delete="false" :loading="state.loading" @refresh="load" @custom-column="gridRef?.openCustom()">
								<template #prefix>
									<el-button v-if="selectedCount" link @click="clearSelection">{{ $t('lz.button.base.deselectAll') }}</el-button>
								</template>
							</TableHeader>
						</template>
						<template #empty>
							<el-empty :image-size="80" :description="$t('lz.label.base.noData')" />
						</template>

						<template #col_zone="{ row }">
							<div class="zone-name">{{ row.name || row.code }}</div>
							<div class="zone-code">{{ row.code }}</div>
						</template>
						<template #col_km="{ row }">{{ kmText(row) }}</template>
						<template #col_dir="{ row }">{{ directionLabel(row.laneId) }}</template>
						<template #col_cam="{ row }">
							<template v-if="row.vdsCameras?.length">
								<el-tooltip v-for="cam in row.vdsCameras" :key="cam.id" placement="top" :content="cam.isPrimary ? $t('lz.label.tmsZone.primaryCamera') : $t('lz.label.tmsZone.secondaryCamera')">
									<el-tag size="small" :type="cam.isPrimary ? 'primary' : 'info'" class="mr5">{{ cam.code || cam.name }}</el-tag>
								</el-tooltip>
							</template>
							<span v-else class="text-muted">—</span>
						</template>
						<template #col_min="{ row }">
							<el-input-number :model-value="valueOf(row, 'min') ?? undefined" class="w100" size="small" :min="1" :max="ZONE_SPEED_MAX_KMH" :precision="0" :controls="false"
								:value-on-clear="null" :placeholder="placeholderOf(row, 'min')" :disabled="state.isProcessing" :class="{ 'is-changed': changed(row, 'min') }"
								@update:model-value="(v: number | null) => setDraft(row, 'min', v)" />
						</template>
						<template #col_max="{ row }">
							<el-input-number :model-value="valueOf(row, 'max') ?? undefined" class="w100" size="small" :min="1" :max="ZONE_SPEED_MAX_KMH" :precision="0" :controls="false"
								:value-on-clear="null" :placeholder="placeholderOf(row, 'max')" :disabled="state.isProcessing" :class="{ 'is-changed': changed(row, 'max') }"
								@update:model-value="(v: number | null) => setDraft(row, 'max', v)" />
						</template>
						<template #col_eff_header>
							<el-tooltip placement="top" :content="$t('lz.label.tmsZone.effectiveSpeedHint')">
								<span>{{ $t('lz.entity.tmsZone.effectiveSpeed') }} <el-icon><ele-QuestionFilled /></el-icon></span>
							</el-tooltip>
						</template>
						<template #col_eff="{ row }">
							<el-tooltip placement="top" :content="effTooltip(row)">
								<div class="eff-cell">
									<template v-for="eff in displayOf(row)" :key="eff.direction ?? '—'">
										<span v-if="eff.valid" class="eff-value"><small v-if="displayOf(row).length > 1">{{ directionLabel(eff.direction) }}: </small>{{ eff.min }}–{{ eff.max }}</span>
										<el-tag v-else size="small" type="danger" effect="plain">{{ $t('lz.label.tmsZone.unusable') }}</el-tag>
									</template>
									<small v-if="!pushesAid(row)" class="text-muted">{{ $t('lz.label.tmsZone.notPushedToAid') }}</small>
								</div>
							</el-tooltip>
						</template>
						<template #col_source="{ row }">
							<span class="source" :class="`source--${displayOf(row)[0].source}`">{{ $t(SPEED_SOURCE_I18N[displayOf(row)[0].source]) }}</span>
						</template>
						<template #col_note="{ row }">
							<span v-if="!row.vdsCameras?.length" class="text-warning">{{ $t('lz.label.tmsZone.noVdsCamera') }}</span>
							<span v-else-if="!hasPrimaryCamera(row)" class="text-muted">{{ $t('lz.label.tmsZone.notPrimaryZone') }}</span>
						</template>
					</vxe-grid>
				</div>
			</div>
		</div>
	</div>
</template>

<script lang="ts" setup name="tmsZoneSpeedTab">
// Author: KienLT
// Description: Tab "Tốc độ min/max" — cây zone (trái, tick để chọn, bấm để thu phạm vi) + dải Km, áp hàng loạt,
// bảng sửa trực tiếp (vxe-grid) và lưu theo lô (phải).
// Created date: 08/10/2026
//
// Quy tắc nghiệp vụ (chốt 08/10/2026):
//  - Zone cha CHỈ để chọn nhanh các zone con; ngưỡng không kế thừa từ cha.
//  - Mặc định theo hướng chỉ HIỂN THỊ; sửa ở trang cấu hình VDS.
//  - Thay đổi gom lại, lưu MỘT lần qua batchUpdateSpeed ⇒ service VDS nạp lại + đẩy AID một lần cho cả lô.
//  - Cột Hiệu lực = ngưỡng ĐẨY XUỐNG AID theo hướng từng camera vùng chính (ForCamera); zone không là vùng chính
//    của camera nào thì hiện ngưỡng zone (ForZone). V_min tính chậm ở tooltip (MinSpeedOfZone).
import { computed, defineAsyncComponent, inject, nextTick, onActivated, onMounted, reactive, ref, watch } from 'vue';
import { onBeforeRouteLeave } from 'vue-router';
import { useZoneTabNavigation } from '../utils/zoneTabNavigation';
import { ElMessage, ElMessageBox } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { VxeGridInstance } from 'vxe-table';
import Fieldset from 'primevue/fieldset';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { Local } from '/@/utils/storage';
import zoneTreePanel from './zoneTreePanel.vue';
import speedStrip, { type StripItem } from './speedStrip.vue';
import { buildZoneTree, childrenMap, descendantIds } from '../utils/zoneTreeUtils';
import {
	SPEED_SOURCE_I18N, ZONE_SPEED_MAX_KMH, aidSpeeds, batchUpdateZoneSpeed, directionOfZone, effectiveSpeed, formatKm, getZoneSpeedConfigList, slowMinOfZone, speedBinOf, toKmh, zoneSpan,
	type AidSpeed, type DirectionSpeedDefault, type EffectiveSpeed, type ZoneSpeedCamera, type ZoneSpeedRow,
} from '/@/services/tms/zone-speed';

const { t: $t } = useI18n();
const { getConfigDataByCode, getConfigItemByCode } = useBaseConfig();
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));

type Field = 'min' | 'max';
interface Draft { min: number | null; max: number | null }

/** Nhớ trạng thái thu gọn sơ đồ dải Km giữa các lần mở trang. */
const STRIP_COLLAPSED_KEY = 'tmsZoneSpeed:stripCollapsed';

/** BE nguyên tử theo TỪNG lô; thực tế ~110 zone nên chỉ một lô. */
const SAVE_CHUNK_SIZE = 500;

const gridRef = ref<VxeGridInstance>();

const state = reactive({
	loading: false,
	isProcessing: false,
	zones: [] as ZoneSpeedRow[],
	directionDefaults: [] as DirectionSpeedDefault[],
	/** Zone đang được bấm ở cây: thu phạm vi bảng/dải về zone đó + con cháu. null = mọi zone theo bộ lọc. */
	scopeId: null as string | null,
	/** Khung sơ đồ dải Km đang thu gọn. */
	stripCollapsed: Local.get(STRIP_COLLAPSED_KEY) === true,
});

const emptyQuery = () => ({
	/** Zone cha (nhánh) được chọn; rỗng = mọi nhánh. */
	parentIds: [] as string[],
	keyword: '',
	direction: null as string | null,
	location: null as string | null,
	fromKm: null as number | null,
	toKm: null as number | null,
	onlyVdsCamera: false,
	onlyOverride: false,
	onlyDirty: false,
	onlyInvalid: false,
});
const query = reactive(emptyQuery());
/** Bộ lọc ĐÃ áp (bấm Tìm) — tách khỏi form để chọn chưa xong không lọc lại. */
const applied = reactive(emptyQuery());
// Sao chép mảng để bản đã áp không bị đổi theo khi người dùng còn đang chọn trên form.
const applyQuery = () => Object.assign(applied, { ...query, parentIds: [...query.parentIds] });
const resetQuery = () => { Object.assign(query, emptyQuery()); applyQuery(); };

const bulk = reactive({ min: null as number | null, max: null as number | null, clearMin: false, clearMax: false });
const drafts = reactive<Record<string, Draft>>({});
const selected = ref(new Set<string>());

const gridOptions = useVxeTable<ZoneSpeedRow>(
	{
		id: 'tmsZoneSpeed',
		name: 'Zone Speed',
		columns: [
			{ type: 'checkbox', width: 44, fixed: 'left' },
			{ field: 'name', title: $t('lz.label.tms.zone'), minWidth: 190, align: 'left', headerAlign: 'center', slots: { default: 'col_zone' } },
			{ title: $t('lz.entity.tms.km'), minWidth: 150, slots: { default: 'col_km' } },
			{ field: 'laneId', title: $t('lz.entity.tms.direction'), width: 110, slots: { default: 'col_dir' } },
			{ title: $t('lz.entity.tmsZone.vdsCamera'), minWidth: 140, slots: { default: 'col_cam' } },
			{ field: 'minSpeed', title: $t('lz.entity.tmsZone.minSpeed'), width: 130, showHeaderOverflow: 'tooltip', slots: { default: 'col_min' } },
			{ field: 'maxSpeed', title: $t('lz.entity.tmsZone.maxSpeed'), width: 130, showHeaderOverflow: 'tooltip', slots: { default: 'col_max' } },
			{ title: $t('lz.entity.tmsZone.effectiveSpeed'), width: 150, slots: { default: 'col_eff', header: 'col_eff_header' } },
			{ title: $t('lz.entity.tmsZone.speedSource'), width: 160, slots: { default: 'col_source' } },
			{ title: $t('lz.label.base.remark'), minWidth: 170, align: 'left', showOverflow: 'tooltip', slots: { default: 'col_note' } },
		],
	},
	{
		rowConfig: { keyField: 'id', isHover: true },
		checkboxConfig: { range: false, reserve: false, highlight: true },
		proxyConfig: { enabled: false },
		pagerConfig: { enabled: false },
		sortConfig: { remote: false },
		toolbarConfig: { export: false },
	}
);

// ─────────────── Danh mục ───────────────

const defaultsMap = computed(() => new Map(state.directionDefaults.map(d => [String(d.direction), d])));
const directionOptions = computed(() => getConfigDataByCode(BaseConfigTypeEnum.TmsDirectionType) ?? []);
const directionLabel = (code: string | null | undefined) => {
	if (code == null || code === '') return $t('lz.label.tmsZone.noDirection');
	return getConfigItemByCode(BaseConfigTypeEnum.TmsDirectionType, String(code))?.name ?? $t('lz.label.tmsZone.directionCode', { code });
};

const children = computed(() => childrenMap(state.zones));
const rowById = computed(() => new Map(state.zones.map(z => [z.id, z])));

/** Zone cha THUẦN (có con, không camera, không khai ngưỡng): chỉ để chọn nhanh, không vào bảng/dải. */
const isPureParent = (zone: ZoneSpeedRow) =>
	(children.value.get(zone.id)?.length ?? 0) > 0 && !zone.vdsCameras?.length && toKmh(zone.minSpeed) == null && toKmh(zone.maxSpeed) == null;
const pureParentIds = computed(() => new Set(state.zones.filter(isPureParent).map(z => z.id)));

// ─────────────── Giá trị & nháp ───────────────

const original = (row: ZoneSpeedRow, field: Field) => toKmh(field === 'min' ? row.minSpeed : row.maxSpeed);
const valueOf = (row: ZoneSpeedRow, field: Field) => (drafts[row.id] ? drafts[row.id][field] : original(row, field));
const changed = (row: ZoneSpeedRow, field: Field) => !!drafts[row.id] && drafts[row.id][field] !== original(row, field);
const isDirty = (row: ZoneSpeedRow) => changed(row, 'min') || changed(row, 'max');

const setDraft = (row: ZoneSpeedRow, field: Field, value: number | null | undefined) => {
	if (state.isProcessing) return; // payload đã chụp; sửa lúc này sẽ lẫn với kết quả lưu
	const normalized = value == null ? null : Math.round(value);
	const draft = drafts[row.id] ?? { min: original(row, 'min'), max: original(row, 'max') };
	draft[field] = normalized;
	// Quay về đúng giá trị gốc ⇒ bỏ nháp để zone không bị tính là "có thay đổi".
	if (draft.min === original(row, 'min') && draft.max === original(row, 'max')) delete drafts[row.id];
	else drafts[row.id] = draft;
};

/** Ngưỡng ZONE theo hướng zone (ForZone). */
const effOf = (row: ZoneSpeedRow): EffectiveSpeed => effectiveSpeed(row, valueOf(row, 'min'), valueOf(row, 'max'), defaultsMap.value);
/** Ngưỡng đẩy xuống AID theo từng hướng camera vùng chính (ForCamera). Rỗng ⇒ zone không đẩy AID. */
const aidOf = (row: ZoneSpeedRow): AidSpeed[] => aidSpeeds(row, valueOf(row, 'min'), valueOf(row, 'max'), defaultsMap.value);
const pushesAid = (row: ZoneSpeedRow) => aidOf(row).length > 0;
const hasPrimaryCamera = (row: ZoneSpeedRow) => (row.vdsCameras ?? []).some((c: ZoneSpeedCamera) => c.isPrimary);

/** Cặp hiển thị: ngưỡng AID nếu zone là vùng chính của camera (thứ thật sự xuống thiết bị), không thì ngưỡng zone. */
const displayOf = (row: ZoneSpeedRow): AidSpeed[] => {
	const aid = aidOf(row);
	return aid.length ? aid : [{ ...effOf(row), cameras: [] }];
};
const displayValid = (row: ZoneSpeedRow) => displayOf(row).every(e => e.valid);
/** Đại diện một cặp cho huy hiệu/dải: có cặp lệch thì lấy cặp lệch để không giấu lỗi. */
const representative = (row: ZoneSpeedRow) => {
	const shown = displayOf(row);
	return shown.find(e => !e.valid) ?? shown[0];
};

const speedText = (eff: EffectiveSpeed) => {
	if (eff.valid) return `${eff.min}–${eff.max} km/h`;
	if (eff.min == null || eff.max == null)
		return eff.direction ? $t('lz.message.tmsZone.missingSpeed', { direction: directionLabel(eff.direction) }) : $t('lz.message.tmsZone.missingSpeedNoDirection');
	return $t('lz.message.tmsZone.minGreaterThanMax', { min: eff.min, max: eff.max });
};

const effTooltip = (row: ZoneSpeedRow) => {
	const parts = aidOf(row).map(e => $t('lz.label.tmsZone.aidSpeedLine', { cameras: e.cameras.join(', '), direction: directionLabel(e.direction), value: speedText(e) }));
	if (!parts.length) parts.push($t('lz.label.tmsZone.notPrimaryZone'));
	const vmin = slowMinOfZone(row, valueOf(row, 'min'), defaultsMap.value);
	parts.push($t('lz.label.tmsZone.slowMinLine', { value: vmin != null ? `${vmin} km/h` : $t('lz.label.tmsZone.sourceNone') }));
	return parts.join(' · ');
};

const placeholderOf = (row: ZoneSpeedRow, field: Field) => {
	const direction = directionOfZone(row);
	const fallback = direction ? defaultsMap.value.get(direction) : undefined;
	return fallback ? $t('lz.label.tmsZone.defaultValue', { value: field === 'min' ? fallback.minSpeed : fallback.maxSpeed }) : '—';
};

const kmText = (row: ZoneSpeedRow) => {
	const span = zoneSpan(row);
	return span ? `${formatKm(span[0])} – ${formatKm(span[1])}` : '—';
};

const badgeOf = (id: string) => representative(rowById.value.get(id)!);
const chipStyle = (id: string) => {
	const bin = speedBinOf(badgeOf(id));
	return bin ? { background: bin.color, color: bin.ink } : {};
};

// ─────────────── Lọc & phạm vi ───────────────

const visibleRows = computed(() => state.zones.filter(z => !isPureParent(z)));

/** Cây chỉ gồm zone CÓ con — nguồn của ô chọn nhánh cha. */
const parentTree = computed(() => buildZoneTree(state.zones, new Set(state.zones.filter(z => children.value.has(z.id)).map(z => z.id))));

/** Nhánh đã chọn ⇒ tập zone thuộc nhánh (chính nó + con cháu); null = không lọc theo nhánh. */
const branchIds = computed(() => {
	if (!applied.parentIds.length) return null;
	return new Set(applied.parentIds.flatMap(id => [id, ...descendantIds(id, children.value)]));
});

const filteredRows = computed(() => {
	const keyword = applied.keyword.trim().toLowerCase();
	const fromM = applied.fromKm != null ? applied.fromKm * 1000 : null;
	const toM = applied.toKm != null ? applied.toKm * 1000 + 999 : null;
	const branch = branchIds.value;
	return visibleRows.value
		.filter(z => !branch || branch.has(z.id))
		.filter(z => !applied.direction || directionOfZone(z) === applied.direction)
		.filter(z => !applied.location || z.location === applied.location)
		.filter(z => {
			if (fromM == null && toM == null) return true;
			const span = zoneSpan(z);
			if (!span) return false;
			return (toM == null || span[0] <= toM) && (fromM == null || span[1] >= fromM);
		})
		.filter(z => !keyword || `${z.name ?? ''} ${z.code ?? ''}`.toLowerCase().includes(keyword))
		.filter(z => !applied.onlyVdsCamera || (z.vdsCameras?.length ?? 0) > 0)
		.filter(z => !applied.onlyOverride || valueOf(z, 'min') != null || valueOf(z, 'max') != null)
		.filter(z => !applied.onlyDirty || isDirty(z))
		.filter(z => !applied.onlyInvalid || !displayValid(z))
		.sort((a, b) => (directionOfZone(a) ?? '~').localeCompare(directionOfZone(b) ?? '~') || (zoneSpan(a)?.[0] ?? Infinity) - (zoneSpan(b)?.[0] ?? Infinity));
});

const treeData = computed(() =>
	buildZoneTree(state.zones, new Set(filteredRows.value.map(z => z.id)), z => {
		const span = zoneSpan(z);
		return { kmText: span ? `${formatKm(span[0])}–${formatKm(span[1])}` : '' };
	})
);

const scopeZone = computed(() => (state.scopeId ? rowById.value.get(state.scopeId) ?? null : null));
const scopeRows = computed(() => {
	if (!state.scopeId) return filteredRows.value;
	const scope = new Set([state.scopeId, ...descendantIds(state.scopeId, children.value)]);
	return filteredRows.value.filter(z => scope.has(z.id));
});

const stripItems = computed<StripItem[]>(() =>
	scopeRows.value.flatMap(row => {
		const span = zoneSpan(row);
		if (!span) return [];
		return [{ row, span, eff: representative(row), selected: selected.value.has(row.id), dirty: isDirty(row), direction: directionOfZone(row) }];
	})
);

const onNodeClick = (node: { id: string }) => { state.scopeId = node.id; };

const onStripCollapsed = (val: boolean) => {
	state.stripCollapsed = val;
	Local.set(STRIP_COLLAPSED_KEY, val);
};

// ─────────────── Chọn (một tập dùng chung cho cây, bảng, dải) ───────────────

const selectedCount = computed(() => selected.value.size);
const replaceSelection = (next: Set<string>) => { selected.value = next; };
const toggleSelect = (id: string) => {
	const next = new Set(selected.value);
	next.has(id) ? next.delete(id) : next.add(id);
	replaceSelection(next);
};
const clearSelection = () => replaceSelection(new Set());

/** Cây chỉ nhận khoá zone thường (có thể chọn); el-tree tự suy trạng thái tick của zone cha. */
const treeCheckedKeys = computed(() => [...selected.value]);

/** Tick trên cây: thay phần chọn của các zone ĐANG HIỆN trong cây; zone bị bộ lọc ẩn giữ nguyên trạng thái chọn. */
const onTreeCheck = (keys: string[]) => {
	const visibleInTree = new Set(filteredRows.value.map(z => z.id));
	const checked = new Set(keys);
	const next = new Set([...selected.value].filter(id => !visibleInTree.has(id)));
	visibleInTree.forEach(id => { if (checked.has(id)) next.add(id); });
	replaceSelection(next);
};

/** Tick trên bảng: thay phần chọn của các dòng đang hiện trong bảng. */
const onGridCheck = () => {
	const checked = new Set((gridRef.value?.getCheckboxRecords() ?? []).map((r: any) => r.id));
	const next = new Set(selected.value);
	scopeRows.value.forEach(r => (checked.has(r.id) ? next.add(r.id) : next.delete(r.id)));
	replaceSelection(next);
};

// Đồng bộ tập chọn ⇒ checkbox của bảng (setCheckboxRow không phát sự kiện checkbox-change nên không vòng lặp).
watch([selected, scopeRows], async () => {
	await nextTick();
	const grid = gridRef.value;
	if (!grid) return;
	await grid.clearCheckboxRow();
	const rows = scopeRows.value.filter(r => selected.value.has(r.id));
	if (rows.length) await grid.setCheckboxRow(rows, true);
});

// ─────────────── Áp hàng loạt ───────────────

const bulkHasAction = computed(() => bulk.min != null || bulk.max != null || bulk.clearMin || bulk.clearMax);

const applyBulk = () => {
	if (!bulkHasAction.value || state.isProcessing) return;
	const minValue = bulk.clearMin ? null : bulk.min;
	const maxValue = bulk.clearMax ? null : bulk.max;
	if (minValue != null && maxValue != null && minValue > maxValue) {
		ElMessage.error($t('lz.validation.tmsZone.minNotGreaterThanMax'));
		return;
	}
	let count = 0;
	for (const zone of state.zones) {
		if (!selected.value.has(zone.id)) continue;
		// Ô để trống = GIỮ NGUYÊN giá trị hiện tại của từng zone; tick "Về mặc định" = xoá giá trị zone.
		if (bulk.clearMin || bulk.min != null) setDraft(zone, 'min', minValue);
		if (bulk.clearMax || bulk.max != null) setDraft(zone, 'max', maxValue);
		count++;
	}
	ElMessage.success($t('lz.message.tmsZone.bulkApplied', { count }));
};

// ─────────────── Lưu ───────────────

const dirtyRows = computed(() => state.zones.filter(isDirty));
const dirtyIds = computed(() => dirtyRows.value.map(z => z.id));
const dirtyInvalidCount = computed(() => dirtyRows.value.filter(z => !displayValid(z)).length);

const rowClass = ({ row }: { row: ZoneSpeedRow }) => (isDirty(row) ? 'row-dirty' : '');

const discard = () => { Object.keys(drafts).forEach(id => delete drafts[id]); };

const save = async () => {
	const rows = dirtyRows.value;
	if (!rows.length) return;

	// Zone tự khai cả hai mà Min > Max: chặn (BE cũng chặn). Ghép với mặc định hướng mà lệch: chỉ cảnh báo.
	const ownConflict = rows.filter(z => {
		const min = valueOf(z, 'min');
		const max = valueOf(z, 'max');
		return min != null && max != null && min > max;
	});
	if (ownConflict.length) {
		const names = ownConflict.slice(0, 3).map(z => z.name || z.code).join(', ') + (ownConflict.length > 3 ? '…' : '');
		ElMessage.error($t('lz.message.tmsZone.zonesMinGreaterThanMax', { count: ownConflict.length, names }));
		return;
	}
	if (dirtyInvalidCount.value) {
		try {
			await ElMessageBox.confirm($t('lz.message.tmsZone.confirmSaveInvalid', { count: dirtyInvalidCount.value }), $t('lz.message.base.warning'), {
				type: 'warning',
				confirmButtonText: $t('lz.button.tmsZone.saveAnyway'),
				cancelButtonText: $t('lz.button.tmsZone.review'),
			});
		} catch {
			return;
		}
	}

	// Chụp payload TRƯỚC await; sau mỗi lô chỉ xoá nháp đã được xác nhận lưu VÀ còn nguyên giá trị đã gửi.
	const items = rows.map(z => ({ id: z.id, minSpeed: valueOf(z, 'min'), maxSpeed: valueOf(z, 'max') }));
	const releaseSaved = (saved: typeof items) => {
		for (const item of saved) {
			const draft = drafts[item.id];
			if (draft && draft.min === item.minSpeed && draft.max === item.maxSpeed) delete drafts[item.id];
		}
	};

	state.isProcessing = true;
	let savedCount = 0;
	try {
		for (let i = 0; i < items.length; i += SAVE_CHUNK_SIZE) {
			const chunk = items.slice(i, i + SAVE_CHUNK_SIZE);
			await batchUpdateZoneSpeed(chunk);
			savedCount += chunk.length;
			releaseSaved(chunk);
		}
		ElMessage.success($t('lz.message.tmsZone.speedSaved', { count: savedCount }));
	} catch (error) {
		console.error('zoneSpeedTab.vue: save(...)', error);
		// Lỗi của lô đầu đã được interceptor axios báo; chỉ báo thêm khi lưu được một phần.
		if (savedCount > 0) ElMessage.error($t('lz.message.tmsZone.speedPartialSaved', { saved: savedCount, total: items.length }));
	} finally {
		state.isProcessing = false;
	}
	if (savedCount > 0) await load();
};

// ─────────────── Nạp ───────────────

/** Zone cần chọn sẵn nhưng dữ liệu chưa về (tab vừa mở lần đầu) ⇒ áp sau khi load xong. */
let pendingFocusId: string | null = null;

const load = async () => {
	state.loading = true;
	try {
		const data = await getZoneSpeedConfigList();
		state.zones = data.zones;
		state.directionDefaults = data.directionDefaults;
		// Bỏ nháp/chọn/phạm vi của zone không còn tồn tại.
		const ids = new Set(data.zones.map(z => z.id));
		Object.keys(drafts).forEach(id => { if (!ids.has(id)) delete drafts[id]; });
		replaceSelection(new Set([...selected.value].filter(id => ids.has(id))));
		if (state.scopeId && !ids.has(state.scopeId)) state.scopeId = null;
		if (pendingFocusId) {
			const id = pendingFocusId;
			pendingFocusId = null;
			focus(id);
		}
	} catch (error) {
		console.error('zoneSpeedTab.vue: load(...)', error);
	} finally {
		state.loading = false;
	}
};

/** Còn thay đổi chưa lưu ⇒ hỏi trước khi rời tab. Trả true nếu được phép rời. */
const confirmLeave = async () => {
	if (!dirtyIds.value.length) return true;
	try {
		await ElMessageBox.confirm($t('lz.message.tmsZone.confirmDiscard', { count: dirtyIds.value.length }), $t('lz.label.base.unsavedChange'), {
			type: 'warning',
			confirmButtonText: $t('lz.button.base.discardChanges'),
			cancelButtonText: $t('lz.button.tmsZone.stay'),
		});
		discard();
		return true;
	} catch {
		return false;
	}
};

/** Mở tab từ nút "Tốc độ" của tab Danh sách: thu phạm vi về zone đó và chọn sẵn nó (nếu là zone sửa được). */
const focus = (zoneId: string | null) => {
	if (!zoneId) return;
	if (!state.zones.length) {
		pendingFocusId = zoneId;
		return;
	}
	state.scopeId = zoneId;
	const zone = rowById.value.get(zoneId);
	if (zone && !isPureParent(zone)) replaceSelection(new Set([zoneId]));
};

// Điều phối giữa các tab (utils/zoneTabNavigation): zone chọn sẵn từ tab Danh sách, nạp lại khi zone đổi.
const tabNavigation = useZoneTabNavigation().state;
let loadedVersion = tabNavigation.version ?? 0;
onMounted(async () => {
	await load();
	// Tab được mở lần đầu từ nút "Tốc độ" của tab Danh sách: áp zone chọn sẵn.
	focus(tabNavigation.focus.speed?.zoneId ?? null);
});
watch(() => tabNavigation.focus.speed?.nonce, () => focus(tabNavigation.focus.speed?.zoneId ?? null));
// KeepAlive giữ tab khi ẩn ⇒ mở lại mà zone đã đổi ở tab Danh sách thì nạp lại (không nạp đè khi còn thay đổi chưa lưu).
onActivated(() => {
	if (tabNavigation.version === loadedVersion || dirtyIds.value.length) return;
	loadedVersion = tabNavigation.version;
	void load();
});
// Rời trang khi còn thay đổi chưa lưu ⇒ hỏi (trước đây do tms/zone/index.vue làm).
onBeforeRouteLeave(async () => await confirmLeave());
defineExpose({ load, confirmLeave, focus, hasUnsaved: () => dirtyIds.value.length > 0 });
</script>

<style scoped lang="scss">
/* Bảng tốc độ đã có cuộn bên trong; tránh cuộn thừa do làm tròn chiều cao. */
.zone-speed-grid :deep(.vxe-grid--layout-body-content-wrapper) {
	overflow: hidden;
}

@import './zoneTab.scss';

.km-range {
	display: flex;
	align-items: center;
	gap: 6px;
	width: 100%;

	:deep(.el-input-number) {
		flex: 1;
	}
}

.defaults {
	display: flex;
	flex-wrap: wrap;
	align-items: center;
	gap: 6px;
	font-size: 12px;

	.defaults-title {
		color: var(--el-text-color-secondary);
	}

	.hint-icon {
		color: var(--el-text-color-secondary);
		cursor: help;
	}
}

.grid-wrap {
	flex: 1;
	min-height: 280px;
	/* Dòng đã tick: vxe mặc định tô vàng cam (#fff3e0) lệch tông trang ⇒ dùng tông primary như phần còn lại. */
	--vxe-ui-table-row-checkbox-checked-background-color: var(--el-color-primary-light-9);
	--vxe-ui-table-row-hover-checkbox-checked-background-color: var(--el-color-primary-light-8);
}

.bulk-bar {
	display: flex;
	flex-wrap: wrap;
	align-items: center;
	gap: 8px;
	padding: 2px 0;
	font-size: 13px;

	.bulk-input {
		width: 115px;
	}
}

/* Sơ đồ dải Km trong khung thu gọn — cùng kiểu Fieldset của trang bản đồ, màu theo biến Element để theo theme. */
:deep(.strip-fieldset) {
	flex: 0 0 auto;
	margin: 0;
	/* Aura đặt padding đáy 1.125rem cho fieldset ⇒ khi thu gọn còn khoảng trống thừa dưới tiêu đề. */
	padding: 0 0 4px;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 6px;
	background: var(--el-bg-color);

	.p-fieldset-legend {
		margin: 2px 4px;
		padding: 0;
		border: 1px solid var(--el-border-color-lighter);
		border-radius: 4px;
		background: var(--el-fill-color-light);
		font-size: 13px;
		font-weight: 600;
		color: var(--el-text-color-primary);
	}

	.p-fieldset-toggle-button {
		display: inline-flex;
		align-items: center;
		gap: 6px;
		padding: 3px 10px 3px 8px;
		color: var(--el-text-color-primary);
	}

	.p-fieldset-toggle-icon {
		color: var(--el-text-color-secondary);
	}

	.p-fieldset-content {
		padding: 4px 10px 4px;
	}
}

.save-bar {
	display: flex;
	align-items: center;
	gap: 8px;
	padding: 6px 10px;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 6px;
	background: var(--el-fill-color-lighter);

	.dirty-text {
		font-weight: 500;
		color: var(--el-text-color-primary);
	}
}

.speed-chip {
	display: inline-block;
	min-width: 44px;
	padding: 0 6px;
	border-radius: 9px;
	font-size: 11px;
	font-weight: 600;
	line-height: 18px;
	text-align: center;

	&.is-default {
		outline: 1.5px dashed var(--el-text-color-secondary);
		outline-offset: -1.5px;
	}

	&.is-invalid {
		background: repeating-linear-gradient(135deg, var(--el-fill-color-darker) 0 4px, var(--el-fill-color-light) 4px 8px);
		color: var(--el-color-danger);
	}
}

.dirty-dot {
	display: inline-block;
	width: 7px;
	height: 7px;
	border-radius: 50%;
	background: var(--el-color-warning);
}

.zone-name {
	font-weight: 500;
	line-height: 1.3;
}

.zone-code {
	font-size: 11px;
	color: var(--el-text-color-secondary);
}

.eff-cell {
	display: flex;
	flex-direction: column;
	align-items: center;
	gap: 2px;
}

.eff-value {
	font-weight: 600;
}

.source {
	font-size: 12px;

	&--zone {
		color: var(--el-color-primary);
	}
	&--mixed {
		color: var(--el-color-primary-light-3);
	}
	&--direction {
		color: var(--el-text-color-secondary);
	}
	&--none {
		color: var(--el-color-danger);
	}
}

.text-muted {
	color: var(--el-text-color-secondary);
	font-size: 12px;
}
.text-warning {
	color: var(--el-color-warning-dark-2);
	font-size: 12px;
}
.text-danger {
	color: var(--el-color-danger);
}

:deep(.is-changed .el-input__wrapper) {
	box-shadow: 0 0 0 1px var(--el-color-warning) inset;
	background: var(--el-color-warning-light-9);
}

:deep(.row-dirty > td:first-child) {
	box-shadow: 3px 0 0 var(--el-color-warning) inset;
}
</style>
