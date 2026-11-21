<template>
	<div class="zone-tab">
		<!-- Tìm kiếm của tab "Gán thiết bị" -->
		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', alignItems: 'start' }">
			<el-form :model="query" label-position="left" label-width="auto" style="flex: 1 1 0%">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.tms.zone')">
							<el-input v-model="query.keyword" clearable :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.label.tmsZone.keyword') })" @keyup.enter.native="applyQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.location')">
							<el-select v-model="query.location" clearable class="w100" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tms.location')})">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TmsZoneLocation)" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.direction')">
							<el-select v-model="query.laneId" clearable class="w100" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tms.direction')})">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TmsDirectionType)" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item>
							<el-checkbox v-model="query.onlyEmpty">{{ $t('lz.label.tmsZone.onlyEmpty') }}</el-checkbox>
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
			<div class="split-left">
				<zoneTreePanel :data="treeData" :current-key="state.currentId" @node-click="onNodeClick">
					<template #badge="{ data }">
						<el-tag v-if="countOf(data.id) > 0" size="small" type="success" effect="plain" round>{{ countOf(data.id) }}</el-tag>
						<el-tooltip v-else :content="$t('lz.label.tmsZone.noEquipment')" placement="top">
							<el-icon class="warn-icon"><ele-WarningFilled /></el-icon>
						</el-tooltip>
					</template>
				</zoneTreePanel>
			</div>

			<div class="split-right" v-loading="state.loading">
				<template v-if="currentZone">
					<div class="right-head">
						<div>
							<div class="right-title">{{ currentZone.name || currentZone.code }}</div>
							<div class="right-sub">{{ kmText(currentZone) }} · {{ directionName(currentZone.laneId) }}</div>
						</div>
						<span class="spacer" />
						<el-button type="warning" icon="ele-Connection" v-auth="'tmsZoneEquipment:add'" @click="onAutoMapZone">{{ $t('lz.button.tmsZone.autoMapZone') }}</el-button>
						<el-button type="warning" plain icon="ele-Connection" v-auth="'tmsZoneEquipment:add'" @click="onAutoMapAll">{{ $t('lz.button.tms.autoMapAll') }}</el-button>
						<el-button icon="ele-Refresh" @click="reloadMappings">{{ $t('lz.button.base.refresh') }}</el-button>
					</div>
					<equipmentMappingGrid
						ref="mappingGridRef"
						:zone-id="currentZone.id"
						:zone-data="currentZone"
						:zone-tree-data="state.zones"
						:equipment-list="state.equipmentList"
						@changed="loadCounts"
					/>
				</template>
				<el-empty v-else :image-size="150" :description="$t('lz.message.tmsZone.selectZoneToMap')" />
			</div>
		</div>

		<autoMapPreviewDialog ref="autoMapPreviewDialogRef" @handleConfirm="reloadMappings" />
	</div>
</template>

<script lang="ts" setup name="tmsZoneMappingTab">
// Author: KienLT
// Description: Tab "Gán thiết bị" — cây zone (trái, huy hiệu số thiết bị đã gán) + thiết bị của zone đang chọn (phải).
// Bấm zone cha: lưới phải hiện zone đó và mọi zone con (equipmentMappingGrid tự gom).
// Created date: 08/10/2026
import { computed, defineAsyncComponent, inject, onActivated, onMounted, reactive, ref, watch } from 'vue';
import { useZoneTabNavigation } from '../utils/zoneTabNavigation';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { TmsEquipmentApi, TmsZoneApi, TmsZoneEquipmentApi } from '/@/api-services/tms';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import zoneTreePanel from './zoneTreePanel.vue';
import equipmentMappingGrid from './equipmentMappingGrid.vue';
import autoMapPreviewDialog from './autoMapPreviewDialog.vue';
import { buildZoneTree, flattenZoneTree } from '../utils/zoneTreeUtils';
import { formatKm, zoneSpan } from '/@/services/tms/zone-speed';

const { getConfigDataByCode, getConfigItemByCode } = useBaseConfig();
const { t: $t } = useI18n();
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));

interface Props {
	/** Zone chọn sẵn khi mở tab từ nút trên dòng của tab Danh sách. */
	focusZoneId?: string | null;
}
const props = defineProps<Props>();
// Điều phối giữa các tab (utils/zoneTabNavigation): zone chọn sẵn từ tab Danh sách, nạp lại khi zone đổi.
const tabNavigation = useZoneTabNavigation().state;
/** Bản danh sách zone đã nạp; lệch với tabNavigation.version ⇒ zone vừa bị sửa ở tab Danh sách. */
let loadedVersion = tabNavigation.version ?? 0;

const mappingGridRef = ref<InstanceType<typeof equipmentMappingGrid>>();
const autoMapPreviewDialogRef = ref<InstanceType<typeof autoMapPreviewDialog>>();

const emptyQuery = () => ({ keyword: '', location: null as string | null, laneId: null as string | null, onlyEmpty: false });
const query = reactive(emptyQuery());
/** Bộ lọc ĐÃ áp (bấm Tìm) — tách khỏi form để gõ chưa xong không lọc lại cây. */
const applied = reactive(emptyQuery());

const state = reactive({
	loading: false,
	zones: [] as any[],
	equipmentList: [] as any[],
	counts: {} as Record<string, number>,
	currentId: null as string | null,
});

const countOf = (zoneId: string) => state.counts[zoneId] ?? 0;
const directionName = (code: any) =>
	code == null || code === '' ? $t('lz.label.tmsZone.noDirection') : getConfigItemByCode(BaseConfigTypeEnum.TmsDirectionType, String(code))?.name ?? $t('lz.label.tmsZone.directionCode', { code });
const kmText = (zone: any) => {
	const span = zoneSpan(zone);
	return span ? `${formatKm(span[0])} – ${formatKm(span[1])}` : $t('lz.label.tmsZone.noKmRange');
};

const treeData = computed(() => {
	const keyword = applied.keyword.trim().toLowerCase();
	const filtered = !keyword && !applied.location && !applied.laneId && !applied.onlyEmpty;
	const match = filtered ? null : new Set(state.zones.filter(z =>
		(!keyword || `${z.name ?? ''} ${z.code ?? ''}`.toLowerCase().includes(keyword))
		&& (!applied.location || z.location === applied.location)
		&& (!applied.laneId || String(z.laneId ?? '') === applied.laneId)
		&& (!applied.onlyEmpty || countOf(z.id) === 0)
	).map(z => z.id));
	return buildZoneTree(state.zones, match, z => {
		const span = zoneSpan(z);
		return { kmText: span ? `${formatKm(span[0])}–${formatKm(span[1])}` : '' };
	});
});

const currentZone = computed(() => state.zones.find(z => z.id === state.currentId) ?? null);

const onNodeClick = (node: any) => { state.currentId = node.id; };

const applyQuery = () => Object.assign(applied, { ...query });
const resetQuery = () => { Object.assign(query, emptyQuery()); applyQuery(); };

/**
 * Đếm thiết bị đã gán theo zone: đọc trang API zone-equipment (không lọc zone) tới hết — không giả định server
 * cho trang lớn tuỳ ý. Trần 50 trang × 500 để không lặp vô hạn nếu API trả sai cờ.
 */
const loadCounts = async () => {
	try {
		const counts: Record<string, number> = {};
		for (let page = 1; page <= 50; page++) {
			const response = await getAPI(TmsZoneEquipmentApi).apiTmsTmszoneequipmentPagePost(undefined, undefined, undefined, undefined, page, 500);
			const result: any = response.data.result ?? {};
			for (const row of result.records ?? []) {
				if (row.zoneId) counts[row.zoneId] = (counts[row.zoneId] ?? 0) + 1;
			}
			if (!result.hasNextPage) break;
		}
		state.counts = counts;
	} catch (error) {
		console.error('zoneMappingTab.vue: loadCounts(...)', error);
	}
};

const load = async () => {
	state.loading = true;
	try {
		const [zonesResponse, equipmentResponse] = await Promise.all([
			getAPI(TmsZoneApi).apiTmsTmszoneGettreeGet(),
			getAPI(TmsEquipmentApi).apiTmsTmsequipmentListGet(),
			loadCounts(),
		]);
		state.zones = flattenZoneTree((zonesResponse.data.result as any[]) ?? []);
		state.equipmentList = equipmentResponse.data.result ?? [];
		const wantedZoneId = props.focusZoneId ?? tabNavigation.focus.mapping?.zoneId;
		if (wantedZoneId) state.currentId = wantedZoneId;
		if (state.currentId && !state.zones.some(z => z.id === state.currentId)) state.currentId = null;
	} catch (error) {
		console.error('zoneMappingTab.vue: load(...)', error);
	} finally {
		state.loading = false;
	}
};

const reloadMappings = async () => {
	await loadCounts();
	mappingGridRef.value?.refresh();
};

const onAutoMapZone = async () => {
	if (!currentZone.value?.id) {
		ElMessage.warning($t('lz.message.tmsZone.pleaseSelectZone'));
		return;
	}
	try {
		const res = await getAPI(TmsZoneEquipmentApi).apiTmsTmszoneequipmentAutomapPost({ zoneId: currentZone.value.id });
		if (!res.data.result?.length) {
			ElMessage.info($t('lz.message.tmsZone.noMatchingEquipment'));
			return;
		}
		// Bản xem trước CHỈ gồm thiết bị chưa gán (BE đã loại thiết bị có sẵn) ⇒ không được xoá liên kết cũ của zone,
		// nếu không các thiết bị đang gán sẽ mất, chỉ còn lại thiết bị vừa thêm.
		autoMapPreviewDialogRef.value?.open(res.data.result, currentZone.value.id, false);
	} catch (error) {
		console.error(error);
	}
};

const onAutoMapAll = async () => {
	try {
		const res = await getAPI(TmsZoneEquipmentApi).apiTmsTmszoneequipmentAutomapallPost({ clearExistingMappings: false });
		if (!res.data.result?.length) {
			ElMessage.info($t('lz.message.tmsZone.noMatchingEquipment'));
			return;
		}
		autoMapPreviewDialogRef.value?.open(res.data.result, undefined, false);
	} catch (error) {
		console.error(error);
	}
};

/** Mở tab từ nút "Gán thiết bị" của tab Danh sách: chọn sẵn zone đó. */
const focus = (zoneId: string | null) => { if (zoneId) state.currentId = zoneId; };

onMounted(load);
// Nút "Gán thiết bị" trên dòng của tab Danh sách: chọn sẵn zone đó (tab đã dựng từ trước thì prop/onMounted không chạy lại).
watch(() => tabNavigation.focus.mapping?.nonce, () => focus(tabNavigation.focus.mapping?.zoneId ?? null));
// KeepAlive giữ tab khi ẩn ⇒ mở lại mà zone đã đổi ở tab Danh sách thì nạp lại.
onActivated(() => {
	if (tabNavigation.version === loadedVersion) return;
	loadedVersion = tabNavigation.version;
	void load();
});
defineExpose({ load, focus });
</script>

<style scoped lang="scss">
@import './zoneTab.scss';

.warn-icon {
	color: var(--el-color-warning);
}
</style>
