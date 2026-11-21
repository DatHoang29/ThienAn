<template>
	<div class="equipment-mapping-grid">
		<!-- <div class="mb10" style="display: flex; align-items: center; gap: 8px; flex-wrap: wrap">
			<el-button type="primary" icon="ele-Plus" size="small" @click="onAddMapping(currentZoneId)" v-auth="'tmsZoneEquipment:add'">
				{{ $t('lz.button.base.add') }}
			</el-button>
			<el-button type="danger" icon="ele-Delete" size="small" @click="onBatchDelete" :disabled="!selectedRows.length" v-auth="'tmsZoneEquipment:delete'">
				{{ $t('lz.button.base.batchDelete') }} ({{ selectedRows.length }})
			</el-button>
		</div> -->

		<div class="zone-sections">
			<div v-for="zone in allZones" :key="zone.id" class="zone-section mb10">
				<div class="zone-section-header" @click="toggleZone(zone.id)">
					<el-icon size="14" style="margin-right: 4px">
						<ele-ArrowRight v-if="!expandedZones.has(zone.id)" />
						<ele-ArrowDown v-else />
					</el-icon>
					<el-tag :type="zone.id === props.zoneId ? 'info' : 'warning'" size="default">
						{{ zone.name }} | Km {{ zone.fromKmNumber }}+{{ zone.metNumber ?? zone.fromMetNumber }} → Km {{ zone.toKmNumber }}+{{ zone.toMetNumber }}
					</el-tag>
					<el-tag type="success" size="default" style="margin-left: 4px">
						{{ $t('lz.label.tmsZone.equipmentCount', { count: (zoneEquipmentMap[zone.id] || []).length }) }}
					</el-tag>
				</div>

				<div v-show="expandedZones.has(zone.id)" class="zone-section-body">
					<div class="zone-section-toolbar">
						<el-button type="primary" icon="ele-Plus" size="small" @click.stop="onAddMapping(zone.id)" v-auth="'tmsZoneEquipment:add'">
							{{ $t('lz.button.base.add') }}
						</el-button>
						<el-button type="danger" icon="ele-Delete" size="small" @click.stop="onDeleteByZone(zone.id)" :disabled="!getZoneSelectedCount(zone.id)" v-auth="'tmsZoneEquipment:delete'">
							{{ $t('lz.button.base.batchDelete') }} ({{ getZoneSelectedCount(zone.id) }})
						</el-button>
					</div>
					<vxe-grid
						:ref="(el: any) => setGridRef(zone.id, el)"
						class="xGrid-style"
						v-bind="gridOptionsMap[zone.id]"
						:max-height="550"
						@checkbox-change="(e: any) => onCheckboxChange(zone.id, e)"
						@checkbox-all="(e: any) => onCheckboxChange(zone.id, e)"
					>
						<template #kmMet_default="{ row }">
							Km {{ row.equipmentKmNumber }} + {{ row.equipmentMetNumber }}
						</template>
						<template #lane_default="{ row }">
							{{ getConfigItemByCode(BaseConfigTypeEnum.TmsLaneType, row.laneId)?.name }}
						</template>
						<template #status_default="{ row }">
							<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, row.status)" />
						</template>
						<template #empty>
							<el-empty :image-size="80" />
						</template>
						<template #row_buttons="{ row }">
							<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
								<el-button icon="ele-Edit" text type="primary" v-auth="'tmsZoneEquipment:update'" @click="onEdit(row)"></el-button>
							</el-tooltip>
							<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
								<el-button icon="ele-Delete" text type="danger" v-auth="'tmsZoneEquipment:delete'" @click="onDelete(row)"></el-button>
							</el-tooltip>
						</template>
					</vxe-grid>
				</div>
			</div>

			<el-empty v-if="!allZones.length" :image-size="120" />
		</div>

		<AddMappingDialog
			ref="addMappingDialogRef"
            :title="mappingTitle"
			:zone-id="currentAddZoneId"
			:equipment-list="equipmentList"
			:existing-equipment-ids="existingEquipmentIds"
			@handleQuery="onChanged"
		/>
	</div>
</template>

<script lang="ts" setup name="equipmentMappingGrid">
import { ref, computed, watch, defineAsyncComponent, nextTick } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { getAPI } from '/@/utils/axios-utils';
import { TmsZoneEquipmentApi } from '/@/api-services/tms';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;

const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
const AddMappingDialog = defineAsyncComponent(() => import('./addMappingDialog.vue'));

const { t: $t } = useI18n();

const props = defineProps<{
	zoneId: string | null;
	zoneData: any | null;
	zoneTreeData: any[];
	equipmentList: any[];
}>();
const emit = defineEmits<{ changed: [] }>();

const mappingTitle = ref('');
const addMappingDialogRef = ref();
const selectedRows = ref<any[]>([]);
const expandedZones = ref<Set<string>>(new Set());
const zoneEquipmentMap = ref<Record<string, any[]>>({});
const gridRefs = ref<Record<string, any>>({});
const currentAddZoneId = ref<string | null>(null);

const childZones = computed(() => {
	if (!props.zoneData?.id || !props.zoneTreeData?.length) return [];
	const findChildren = (nodes: any[], parentId: string): any[] => {
		const result: any[] = [];
		for (const node of nodes) {
			if (node.pid === parentId) {
				result.push(node);
				result.push(...findChildren(nodes, node.id));
			}
		}
		return result;
	};
	return findChildren(props.zoneTreeData, props.zoneData.id);
});

const allZones = computed(() => {
	if (!props.zoneData) return [];
	return [props.zoneData, ...childZones.value];
});

const existingEquipmentIds = computed(() => {
	const ids: string[] = [];
	for (const list of Object.values(zoneEquipmentMap.value)) {
		for (const r of list as any[]) {
			if (r.equipmentId) ids.push(r.equipmentId);
		}
	}
	return ids;
});

const currentZoneId = computed(() => props.zoneId);

function setGridRef(zoneId: string, el: any) {
	if (el) gridRefs.value[zoneId] = el;
}

function toggleZone(zoneId: string) {
	if (expandedZones.value.has(zoneId)) {
		expandedZones.value.delete(zoneId);
	} else {
		expandedZones.value.add(zoneId);
	}
	expandedZones.value = new Set(expandedZones.value);
}

const gridOptionsMap = ref<Record<string, any>>({});

watch(allZones, (newZones) => {
	const newMap: Record<string, any> = {};
	for (const zone of newZones) {
		if (gridOptionsMap.value[zone.id]) {
			newMap[zone.id] = gridOptionsMap.value[zone.id];
		} else {
			newMap[zone.id] = useVxeTable<any>(
				{
					id: `tmsZoneEquipment_${zone.id}`,
					name: 'ZoneEquipment',
					columns: [
						{ type: 'checkbox', width: 50 },
						{ field: 'equipmentCode', title: $t('lz.entity.base.code'), minWidth: 120, sortable: true, showOverflow: 'tooltip' },
						{ field: 'equipmentName', title: $t('lz.entity.base.name'), minWidth: 150, sortable: true, showOverflow: 'tooltip' },
						{ field: 'kmMet', title: $t('lz.label.base.position'), minWidth: 120, slots: { default: 'kmMet_default' } },
						{ field: 'laneId', title: $t('lz.entity.tms.lane'), minWidth: 100, slots: { default: 'lane_default' } },
						{ field: 'status', title: $t('lz.entity.base.status'), width: 110, sortable: true, showOverflow: 'tooltip', slots: { default: 'status_default' } },
						{ field: 'remark', title: $t('lz.entity.base.remark'), minWidth: 150, showOverflow: 'tooltip' },
						{ title: $t('lz.label.base.action'), width: 100, fixed: 'right', slots: { default: 'row_buttons' } },
					],
				},
				{
					data: [],
					rowConfig: { keyField: 'id' },
					checkboxConfig: { range: false },
					proxyConfig: { autoLoad: false },
					pagerConfig: { enabled: false },
					toolbarConfig: { enabled: false },
				}
			);
		}
	}
	gridOptionsMap.value = newMap;
}, { immediate: true });


function getZoneSelectedCount(zoneId: string): number {
	return selectedRows.value.filter((r: any) => r.zoneId === zoneId).length;
}

function onCheckboxChange(zoneId: string) {
	const grid = gridRefs.value[zoneId];
	if (!grid) return;
	const records = grid.getCheckboxRecords() ?? [];
	const others = selectedRows.value.filter((r: any) => r.zoneId !== zoneId);
	selectedRows.value = [...others, ...records];
}

watch(() => props.zoneId, (newVal) => {
	if (newVal) {
		refresh();
	} else {
		zoneEquipmentMap.value = {};
	}
}, { immediate: true });

/* Vừa thêm/sửa/xoá liên kết: nạp lại lưới và báo tab cha đếm lại huy hiệu số thiết bị trên cây zone. */
function onChanged() {
	refresh();
	emit('changed');
}

async function refresh() {
	if (!props.zoneId) return;
	const newMap: Record<string, any[]> = {};
	const zones = allZones.value;
	await Promise.all(zones.map(async (zone: any) => {
		try {
			const res = await getAPI(TmsZoneEquipmentApi).apiTmsTmszoneequipmentListGet(zone.id);
			newMap[zone.id] = (res.data.result ?? []).map((r: any) => ({ ...r, zoneId: zone.id }));
		} catch (error) {
			console.error(error);
			newMap[zone.id] = [];
		}
	}));
	zoneEquipmentMap.value = newMap;
	selectedRows.value = [];
	expandedZones.value = new Set(zones.map((z: any) => z.id));
	await nextTick();
	for (const zone of zones) {
		const grid = gridRefs.value[zone.id];
		if (grid) grid.loadData(newMap[zone.id] || []);
	}
}

function onAddMapping(zoneId: string) {
    mappingTitle.value = $t('lz.label.base.titleAdd', { name: $t('lz.entity.tms.mapping') });
	currentAddZoneId.value = zoneId;
	nextTick(() => addMappingDialogRef.value?.open());
}

function onEdit(row: any) {
    mappingTitle.value = $t('lz.label.base.titleEdit', { name: $t('lz.entity.tms.mapping') });
	currentAddZoneId.value = row.zoneId;
	nextTick(() => addMappingDialogRef.value?.open(row));
}

async function onDelete(row: any) {
	ElMessageBox.confirm(
		$t('lz.message.base.confirmDelete', { name: row.equipmentName }),
		$t('lz.message.base.warning'),
		{ confirmButtonText: $t('lz.button.base.confirm'), cancelButtonText: $t('lz.button.base.cancel'), type: 'warning' }
	).then(async () => {
		await getAPI(TmsZoneEquipmentApi).apiTmsTmszoneequipmentDeletePost({ id: row.id });
		ElMessage.success($t('lz.message.base.deleteSuccess'));
		onChanged();
	}).catch(() => {});
}

async function onDeleteByZone(zoneId: string) {
	const rows = selectedRows.value.filter((r: any) => r.zoneId === zoneId);
	if (!rows.length) return;
	ElMessageBox.confirm(
		$t('lz.message.base.confirmBatchDelete', { total: rows.length }),
		$t('lz.message.base.warning'),
		{ confirmButtonText: $t('lz.button.base.confirm'), cancelButtonText: $t('lz.button.base.cancel'), type: 'warning' }
	).then(async () => {
		const ids = rows.map((r: any) => ({ id: r.id }));
		await getAPI(TmsZoneEquipmentApi).apiTmsTmszoneequipmentBatchdeletePost(ids);
		ElMessage.success($t('lz.message.base.deleteSuccess'));
		onChanged();
	}).catch(() => {});
}

async function onBatchDelete() {
	if (!selectedRows.value.length) return;
	ElMessageBox.confirm(
		$t('lz.message.base.confirmBatchDelete', { total: selectedRows.value.length }),
		$t('lz.message.base.warning'),
		{ confirmButtonText: $t('lz.button.base.confirm'), cancelButtonText: $t('lz.button.base.cancel'), type: 'warning' }
	).then(async () => {
		const ids = selectedRows.value.map((r: any) => ({ id: r.id }));
		await getAPI(TmsZoneEquipmentApi).apiTmsTmszoneequipmentBatchdeletePost(ids);
		ElMessage.success($t('lz.message.base.deleteSuccess'));
		onChanged();
	}).catch(() => {});
}

defineExpose({ refresh });
</script>

<style scoped>
.equipment-mapping-grid {
	display: flex;
	flex-direction: column;
	flex: 1;
	min-height: 0;
	overflow: auto;
}
.zone-sections {
	flex: 1;
	overflow: auto;
}
.zone-section {
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 4px;
	overflow: hidden;
	height: stretch;
}
.zone-section-header {
	display: flex;
	align-items: center;
	padding: 6px 10px;
	background: var(--el-fill-color-light);
	cursor: pointer;
	user-select: none;
	gap: 4px;
}
.zone-section-header:hover {
	background: var(--el-fill-color);
}
.zone-section-body {
	padding: 6px;
}
.zone-section-toolbar {
	display: flex;
	gap: 6px;
	margin-bottom: 6px;
}
</style>
