<template>
	<div class="vw-output-list" v-loading="state.loading">
		<!-- Lọc tại chỗ (API trả đủ mọi cổng, không phân trang) -->
		<div class="vw-output-list-bar">
			<el-input v-model="filter.keyword" class="vw-output-list-input" clearable :placeholder="$t('lz.label.videoWall.codeOrName')" />
			<el-cascader v-model="filter.controllerId" class="vw-output-list-input" :options="controllerTree" :props="{ ...CONTROLLER_CASCADER_PROPS, checkStrictly: true }"
				:placeholder="$t('lz.label.videoWall.selectController')" filterable clearable />
			<el-select v-model="filter.deviceState" class="vw-output-list-select" clearable :placeholder="$t('lz.entity.videoWall.deviceState')">
				<el-option v-for="s in DEVICE_STATES" :key="s.value" :value="s.value" :label="$t(s.label)" />
			</el-select>
			<span class="vw-output-list-count">{{ rows.length }} / {{ state.outputs.length }}</span>
			<span v-if="lastSynced" class="vw-output-list-count">· {{ $t('lz.label.videoWall.lastSynced') }}: {{ lastSynced }}</span>
			<el-button class="ml-auto" icon="ele-Refresh" @click="reload">{{ $t('lz.button.base.refresh') }}</el-button>
			<el-button type="primary" icon="ele-Download" :loading="state.syncing" v-auth="'videoWallOutput:update'" @click="syncFromDevice">
				{{ $t('lz.button.videoWall.updateOutputs') }}
			</el-button>
		</div>

		<el-card class="full-table" shadow="hover">
			<vxe-grid class="xGrid-style" :data="rows" :columns="columns" height="auto" border="inner" stripe auto-resize
				:row-config="{ isHover: true }" :sort-config="{ trigger: 'cell' }" :column-config="{ resizable: true }">
				<template #row_controller="{ row }">
					<span class="vw-output-list-ctrl" :style="{ borderLeftColor: vwCtrl.colorOf(row.controllerId) }">{{ vwCtrl.label(row.controllerId) }}</span>
				</template>
				<!-- Tên: tên đặt trên web, không có thì tên thiết bị. Dòng phụ: khe cắm card + cổng trên card -->
				<template #row_name="{ row }">
					<div>{{ row.displayName || row.name }}</div>
					<div class="vw-output-list-muted">{{ portHint(row) }}</div>
				</template>
				<template #row_position="{ row }">
					<span v-if="row.gridRow != null && row.gridCol != null">{{ cellLabel(row.blockNo, row.gridRow, row.gridCol, topology.isMultiBlock) }}</span>
					<span v-else class="vw-output-list-muted">—</span>
				</template>
				<template #row_screen="{ row }">
					<template v-if="row.screenCode">
						<div>{{ row.screenCode }}</div>
						<div class="vw-output-list-muted">{{ row.serialNumber }}</div>
					</template>
					<span v-else class="vw-output-list-muted">{{ $t('lz.label.videoWall.noScreen') }}</span>
				</template>
				<template #row_screen_state="{ row }">
					<el-tag v-if="row.screenState" size="small" :type="screenStateTag(row.screenState)">{{ row.screenState }}</el-tag>
				</template>
				<template #row_device_state="{ row }">
					<el-tag size="small" :type="deviceStateOf(row.deviceState).tag">{{ $t(deviceStateOf(row.deviceState).label) }}</el-tag>
				</template>
				<template #row_status="{ row }">
					<el-tag size="small" :type="row.status === 1 ? 'success' : 'info'">{{ row.status === 1 ? $t('lz.label.videoWall.outputInUse') : $t('lz.label.videoWall.outputNotInUse') }}</el-tag>
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
						<el-button icon="ele-Edit" text type="primary" v-auth="'videoWallOutput:update'" @click="openEdit(row)" />
					</el-tooltip>
				</template>
				<template #empty>
					<el-empty :image-size="120" :description="$t('lz.message.videoWall.outputNotSynced')" />
				</template>
			</vxe-grid>
		</el-card>

		<!-- Chỉ sửa trường web quản lý; phần còn lại do thiết bị đồng bộ -->
		<el-dialog v-model="state.editVisible" :title="editing ? `${$t('lz.button.base.edit')} · ${editing.name}` : ''" width="460px" draggable>
			<el-form label-position="top">
				<el-form-item :label="$t('lz.label.videoWall.outputDisplayName')">
					<el-input v-model="edit.displayName" maxlength="64" clearable :placeholder="editing?.name ?? ''" />
				</el-form-item>
				<el-form-item :label="$t('lz.entity.base.remark')">
					<el-input v-model="edit.remark" type="textarea" :rows="3" maxlength="256" show-word-limit />
				</el-form-item>
				<el-form-item :label="$t('lz.entity.base.status')">
					<el-switch v-model="edit.status" :active-value="1" :inactive-value="0"
						:active-text="$t('lz.label.videoWall.outputInUse')" :inactive-text="$t('lz.label.videoWall.outputNotInUse')" />
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.editVisible = false">{{ $t('lz.button.base.cancel') }}</el-button>
				<el-button type="primary" :loading="state.saving" @click="save">{{ $t('lz.button.base.save') }}</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="videoWallControllerOutputList">
import { computed, onMounted, reactive } from 'vue';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import type { VxeGridPropTypes } from 'vxe-table';
import { getAPI } from '/@/utils/axios-utils';
import { VwControllerOutputApi, VwDeviceSetupApi } from '/@/api-services/videoWall';
import type { VwControllerOutputOutput } from '/@/api-services/videoWall';
import { useVideoWallController } from '/@/stores/its/videoWallController';
import { cellLabel, useVideoWallTopology } from '/@/stores/its/videoWallTopology';
import { buildControllerTree, CONTROLLER_CASCADER_PROPS } from '/@/utils/videoWall/controllerTree';
import { formatDate } from '/@/utils/formatTime';

/** VwOutputDeviceState: 1 có trên thiết bị, 2 không thấy ở lần đồng bộ gần nhất, 3 dựng tay (chưa đồng bộ). */
const DEVICE_STATES = [
	{ value: 1, label: 'lz.label.videoWall.outputPresent', tag: 'success' },
	{ value: 2, label: 'lz.label.videoWall.outputNotFound', tag: 'danger' },
	{ value: 3, label: 'lz.label.videoWall.outputSeed', tag: 'info' },
] as const;
const deviceStateOf = (v?: number | null) => DEVICE_STATES.find((s) => s.value === v) ?? DEVICE_STATES[2];
const screenStateTag = (s: string) => (s === 'Online' ? 'success' : s === 'Warning' ? 'warning' : 'danger');

const { t: $t } = useI18n();
const vwCtrl = useVideoWallController();
const topology = useVideoWallTopology();
const controllerTree = computed(() => buildControllerTree(vwCtrl.controllers));

/** Worker đồng bộ bất đồng bộ (NATS) ⇒ nạp lại sau khoảng này. */
const SYNC_RELOAD_MS = 4000;

const state = reactive({
	loading: false,
	syncing: false,
	saving: false,
	editVisible: false,
	editingId: '',
	outputs: [] as VwControllerOutputOutput[],
});
const filter = reactive({ keyword: '', controllerId: undefined as string | undefined, deviceState: undefined as number | undefined });
const edit = reactive({ displayName: '' as string, remark: '' as string, status: 1 as number });
const editing = computed(() => state.outputs.find((o) => o.id === state.editingId));

/** Chọn bộ cha ⇒ gồm cả cổng của các bộ con. */
const controllerIds = computed(() => {
	const id = filter.controllerId;
	if (!id) return null;
	return new Set([id, ...vwCtrl.controllers.filter((c) => c.parentControllerId === id).map((c) => c.id!)]);
});

const rows = computed(() => {
	const kw = filter.keyword.trim().toLowerCase();
	return state.outputs.filter((o) => {
		if (controllerIds.value && !controllerIds.value.has(o.controllerId ?? '')) return false;
		if (filter.deviceState != null && o.deviceState !== filter.deviceState) return false;
		if (!kw) return true;
		return [o.code, o.name, o.displayName, o.screenCode, o.serialNumber].some((v) => v?.toLowerCase().includes(kw));
	});
});

const columns: VxeGridPropTypes.Columns<VwControllerOutputOutput> = [
	// { type: 'seq', width: 55, fixed: 'left' },
	{ field: 'controllerCode', title: $t('lz.entity.videoWall.controller'), minWidth: 170, sortable: true, slots: { default: 'row_controller' } },
	{ field: 'name', title: $t('lz.entity.videoWall.outputPort'), minWidth: 170, sortable: true, slots: { default: 'row_name' } },
	{ field: 'deviceOutputId', title: 'Output ID', width: 110, sortable: true },
	{ field: 'portType', title: $t('lz.entity.videoWall.portType'), width: 95 },
	{ field: 'resolution', title: $t('lz.entity.videoWall.resolution'), width: 140 },
	{ field: 'gridRow', title: $t('lz.entity.videoWall.position'), titlePrefix: { content: $t('lz.label.videoWall.cellLabelHint') }, width: 110, sortable: true, slots: { default: 'row_position' } },
	{ field: 'screenCode', title: $t('lz.entity.videoWall.screen'), minWidth: 170, sortable: true, slots: { default: 'row_screen' } },
	{ field: 'screenState', title: $t('lz.entity.videoWall.connection'), width: 105, slots: { default: 'row_screen_state' } },
	// { field: 'temperature', title: $t('lz.entity.videoWall.temperature'), width: 95, sortable: true, formatter: ({ cellValue }) => (cellValue ? `${cellValue} °C` : '') },
	// { field: 'deviceState', title: $t('lz.entity.videoWall.deviceState'), width: 120, sortable: true, slots: { default: 'row_device_state' } },
	{ field: 'lastSyncedAt', title: $t('lz.label.videoWall.lastSynced'), width: 145, sortable: true, formatter: ({ cellValue }) => (cellValue ? formatDate(new Date(cellValue), 'dd/mm/YYYY HH:MM') : '') },
	{ field: 'status', title: $t('lz.entity.base.status'), width: 110, sortable: true, slots: { default: 'row_status' } },
	{ field: 'remark', title: $t('lz.entity.base.remark'), minWidth: 160, showOverflow: 'tooltip' },
	{ title: $t('lz.label.base.action'), fixed: 'right', width: 80, align: 'center', slots: { default: 'row_buttons' } },
];

/** "Output 7-1" ⇒ "Khe 7 · Cổng 1 · HDMI"; có tên đặt trên web thì kèm tên thiết bị để còn đối chiếu. */
const portHint = (o: VwControllerOutputOutput) => {
	const slot = o.boardNo != null && o.portNo != null ? $t('lz.label.videoWall.slotPort', { slot: o.boardNo, port: o.portNo }) : '';
	return [o.displayName ? o.name : '', slot, o.portType].filter(Boolean).join(' · ');
};

const lastSynced = computed(() => {
	const times = state.outputs.map((o) => o.lastSyncedAt).filter(Boolean).map((d) => new Date(d as any).getTime());
	return times.length ? formatDate(new Date(Math.max(...times)), 'dd/mm/YYYY HH:MM') : '';
});

/** Theo bộ con (mã), trong mỗi bộ theo hàng rồi cột. */
const sortOutputs = (list: VwControllerOutputOutput[]) =>
	list.sort((a, b) => (a.controllerCode ?? '').localeCompare(b.controllerCode ?? '') || (a.localRow ?? 0) - (b.localRow ?? 0) || (a.localCol ?? 0) - (b.localCol ?? 0));

const reload = async () => {
	state.loading = true;
	try {
		await Promise.all([vwCtrl.load(), topology.load(true)]);
		const res = await getAPI(VwControllerOutputApi).apiVideowallVwcontrolleroutputListGet();
		// Bỏ cổng của bộ trung tâm: đó là dây nối trung tâm → bộ con, không phải cổng ra màn hình
		state.outputs = sortOutputs((res.data.result ?? []).filter((o) => !o.isMaster));
	} finally {
		state.loading = false;
	}
};

/** Gửi lệnh đồng bộ cổng ra mọi bộ điều khiển rồi nạp lại. */
const syncFromDevice = async () => {
	state.syncing = true;
	try {
		const res = await getAPI(VwDeviceSetupApi).apiVideowallVwdevicesetupSyncoutputsPost({});
		if (!res.data.result?.accepted) return ElMessage.warning($t('lz.message.videoWall.commandRejected'));
		ElMessage.success($t('lz.message.videoWall.syncOutputsSent'));
		await new Promise((r) => setTimeout(r, SYNC_RELOAD_MS));
		await reload();
	} finally {
		state.syncing = false;
	}
};

const openEdit = (o: VwControllerOutputOutput) => {
	state.editingId = o.id ?? '';
	edit.displayName = o.displayName ?? '';
	edit.remark = o.remark ?? '';
	edit.status = (o.status as number) ?? 1;
	state.editVisible = true;
};

const save = async () => {
	if (!editing.value?.id) return;
	state.saving = true;
	try {
		await getAPI(VwControllerOutputApi).apiVideowallVwcontrolleroutputUpdatePost({
			id: editing.value.id,
			displayName: edit.displayName || null,
			remark: edit.remark || null,
			status: edit.status as any,
		});
		ElMessage.success($t('lz.message.base.saveSuccess'));
		state.editVisible = false;
		await reload();
	} finally {
		state.saving = false;
	}
};

onMounted(reload);
</script>

<style scoped lang="scss">
.vw-output-list {
	height: 100%;
	display: flex;
	flex-direction: column;
	gap: 8px;
	min-height: 0;
	> .full-table {
		flex: 1;
		min-height: 0;
		:deep(.el-card__body) {
			height: 100%;
			box-sizing: border-box;
		}
	}
}
.vw-output-list-bar {
	display: flex;
	align-items: center;
	flex-wrap: wrap;
	gap: 8px;
}
.vw-output-list-input {
	width: 220px;
}
.vw-output-list-select {
	width: 170px;
}
.vw-output-list-count {
	font-size: 13px;
	color: var(--el-text-color-secondary);
}
.ml-auto {
	margin-left: auto;
}
.vw-output-list-muted {
	font-size: 12px;
	color: var(--el-text-color-secondary);
}
.vw-output-list-ctrl {
	padding-left: 6px;
	border-left: 3px solid transparent;
}
</style>
