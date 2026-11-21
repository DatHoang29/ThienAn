<template>
	<el-dialog v-model="state.isShowDialog" :title="$t('lz.entity.tms.autoMapPreview')" width="80%" draggable :close-on-click-modal="false" top="5vh" destroy-on-close>
		<template #header>
			<div style="color: #fff">
				<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-Connection /></el-icon>
				<span>{{ $t('lz.label.base.preview') }}</span>
			</div>
		</template>
		<div class="mb10">
			<el-alert type="info" :closable="false" show-icon :title="$t('lz.message.tmsZone.autoMapInfo')" />
		</div>
		<div class="mb10" style="display: flex; align-items: center; gap: 12px">
			<el-button @click="onSelectAll">{{ $t('lz.button.base.selectAll') }}</el-button>
			<el-button @click="onDeselectAll">{{ $t('lz.button.base.deselectAll') }}</el-button>
			<span>{{ $t('lz.label.base.selectedCount', { count: selectedRows.length, total: previewData.length }) }}</span>
		</div>
		<vxe-grid ref="xGrid" v-bind="gridOptions" :data="previewData" :max-height="500" @checkbox-change="onCheckboxChange" @checkbox-all="onCheckboxChange">
			<template #zone_default="{ row }">
				{{ row.zoneCode }} - {{ row.zoneName }}
			</template>
			<template #kmMet_default="{ row }">
				Km {{ row.kmNumber }} + {{ row.metNumber }}
			</template>
			<template #lane_default="{ row }">
				{{ getConfigItemByCode(BaseConfigTypeEnum.TmsLaneType, row.laneId)?.name }}
			</template>
			<template #matchReason_default="{ row }">
				<el-tag size="small" type="success">{{ row.matchReason }}</el-tag>
			</template>
			<template #empty>
				<el-empty :image-size="120" />
			</template>
		</vxe-grid>
		<template #footer>
			<el-button @click="state.isShowDialog = false">{{ $t('lz.button.base.cancel') }}</el-button>
			<el-button type="primary" @click="onConfirm" :disabled="!selectedRows.length">
				{{ $t('lz.button.base.confirm') }} ({{ selectedRows.length }})
			</el-button>
		</template>
	</el-dialog>
</template>

<script lang="ts" setup name="autoMapPreviewDialog">
import { reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { TmsZoneEquipmentApi } from '/@/api-services/tms';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;

const { t: $t } = useI18n();

const emit = defineEmits(['handleConfirm']);

const xGrid = ref();
const previewData = ref<any[]>([]);
const selectedRows = ref<any[]>([]);
const currentZoneId = ref<string | null>(null);
const clearExisting = ref(false);

const state = reactive({
	isShowDialog: false,
});

const gridOptions = reactive({
	border: true,
	stripe: true,
	showOverflow: true,
	columnConfig: { resizable: true },
	checkboxConfig: { range: false },
	columns: [
		{ type: 'checkbox', width: 50 },
		{ field: 'zoneCode', title: $t('lz.label.tms.zone'), minWidth: 150, slots: { default: 'zone_default' } },
		{ field: 'equipmentCode', title: $t('lz.entity.base.code'), minWidth: 120 },
		{ field: 'equipmentName', title: $t('lz.entity.tms.equipment'), minWidth: 150 },
		{ field: 'kmMet', title: $t('lz.label.base.position'), minWidth: 120, slots: { default: 'kmMet_default' } },
		{ field: 'laneId', title: $t('lz.entity.tms.lane'), minWidth: 100, slots: { default: 'lane_default' } },
		{ field: 'matchReason', title: $t('lz.entity.base.remark'), minWidth: 360, showOverflow: 'tooltip', slots: { default: 'matchReason_default' } },
	],
});

function open(data: any[], zoneId?: string, shouldClear?: boolean) {
	previewData.value = data;
	currentZoneId.value = zoneId ?? null;
	clearExisting.value = shouldClear ?? false;
	selectedRows.value = [];
	state.isShowDialog = true;
}

function onCheckboxChange() {
	selectedRows.value = xGrid.value?.getCheckboxRecords() ?? [];
}

function onSelectAll() {
	xGrid.value?.setAllCheckboxRow(true);
	selectedRows.value = [...previewData.value];
}

function onDeselectAll() {
	xGrid.value?.clearCheckboxRow();
	selectedRows.value = [];
}

async function onConfirm() {
	const mappings = selectedRows.value.map((row: any) => ({
		zoneId: row.zoneId,
		equipmentId: row.equipmentId,
		type: '',
		status: 1,
		laneId: row.laneId,
		remark: '',
	}));

	try {
		await getAPI(TmsZoneEquipmentApi).apiTmsTmszoneequipmentConfirmmappingPost({
			mappings,
			zoneId: currentZoneId.value ?? undefined,
			clearExistingMappings: clearExisting.value,
		});
		ElMessage.success($t('lz.message.base.saveSuccess'));
		state.isShowDialog = false;
		emit('handleConfirm');
	} catch (error) {
		console.error(error);
	}
}

defineExpose({ open });
</script>
