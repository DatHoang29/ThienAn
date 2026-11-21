<template>
	<div class="vds-segment-container">
		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="query" :show-message="false" :inline-message="true" label-width="auto" label-position="left" style="flex: 1 1 0%">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6"><el-form-item label="Tên đoạn"><el-input v-model="query.keyword" class="ta-input-size" size="small" clearable maxlength="128" @keyup.enter="loadSegments" /></el-form-item></el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6"><el-form-item label="Camera đầu"><el-select v-model="query.fromEquipmentId" class="ta-input-size" size="small" filterable clearable placeholder="Tất cả"><el-option v-for="camera in cameras" :key="camera.id" :value="camera.id" :label="cameraLabel(camera)" /></el-select></el-form-item></el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6"><el-form-item label="Camera cuối"><el-select v-model="query.toEquipmentId" class="ta-input-size" size="small" filterable clearable placeholder="Tất cả"><el-option v-for="camera in cameras" :key="camera.id" :value="camera.id" :label="cameraLabel(camera)" /></el-select></el-form-item></el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6"><el-form-item label="Chiều"><el-select v-model="query.direction" class="ta-input-size" size="small" clearable placeholder="Tất cả"><el-option v-for="direction in directions" :key="direction.code" :value="direction.code" :label="direction.name" /></el-select></el-form-item></el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6"><el-form-item label="Trạng thái"><el-select v-model="query.status" class="ta-input-size" size="small" clearable placeholder="Tất cả"><el-option label="Hoạt động" value="1" /><el-option label="Tắt" value="0" /></el-select></el-form-item></el-col>
				</el-row>
			</el-form>
			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />
			<el-row><SearchButton authSearch="vdsSegment:list" :loading="loading" @search="loadSegments" @reset="resetQuery" /></el-row>
		</el-card>
		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<el-alert v-if="error" :title="error" type="error" :closable="false" class="mb5" />
			<vxe-grid ref="grid" class="xGrid-style" v-bind="options" :data="filteredRows">
				<template #toolbar_tools><TableHeader :auth-add="'vdsSegment:add'" :show-delete="false" @add="openSegment()" @refresh="loadSegments" @custom-column="grid?.openCustom()" /></template>
				<template #empty><el-empty :image-size="160" description="Chưa khai báo đoạn camera" /></template>
				<template #from="{ row }">{{ cameraName(row.fromEquipmentId) }}</template>
				<template #to="{ row }">{{ cameraName(row.toEquipmentId) }}</template>
				<template #direction="{ row }">{{ directionName(row.direction) }}</template>
				<template #status="{ row }"><el-tag :type="row.status === '1' ? 'success' : 'info'">{{ row.status === '1' ? 'Hoạt động' : 'Tắt' }}</el-tag></template>
				<template #row_record="{ row }"><ModifyRecord :data="{ ...row, id: row.id ?? null }" /></template>
				<template #actions="{ row }">
					<el-tooltip content="Xem" placement="top"><el-button v-auth="'vdsSegment:view'" text type="primary" icon="ele-View" aria-label="Xem" @click="openSegment(row, 'view')" /></el-tooltip>
					<el-tooltip content="Sao chép" placement="top"><el-button v-auth="'vdsSegment:add'" text type="warning" icon="ele-CopyDocument" aria-label="Sao chép" @click="openSegment({ ...row, id: undefined })" /></el-tooltip>
					<el-tooltip content="Sửa" placement="top"><el-button v-auth="'vdsSegment:update'" text type="primary" icon="ele-Edit" @click="openSegment(row)" /></el-tooltip>
					<el-tooltip content="Xóa" placement="top"><el-button v-auth="'vdsSegment:delete'" text type="danger" icon="ele-Delete" :disabled="!!deletingId" :loading="deletingId === row.id" @click="deleteSegment(row)" /></el-tooltip>
				</template>
			</vxe-grid>
		</el-card>
		<EditSegment ref="editRef" :title="editTitle" :cameras="cameras" :directions="directions" @saved="loadSegments" />
	</div>
</template>

<script setup lang="ts" name="vdsSegment">
import { computed, onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import type { VxeGridInstance } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { VdsSegmentApi } from '/@/api-services/vds';
import type { SegmentOutput } from '/@/api-services/vds';
import { getAPI } from '/@/utils/axios-utils';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useI18n } from 'vue-i18n';
import TableHeader from '/@/components/advanced/table-header-operation.vue';
import SearchButton from '/@/components/advanced/search-button-operation.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import { cameraLabel, loadVdsCameras, type VdsCameraOption } from '../catalog';
import EditSegment from './component/editSegment.vue';

const grid = ref<VxeGridInstance>();
const { t: $t } = useI18n();
const editTitle = ref('');
function openSegment(row?: SegmentOutput, mode: 'edit' | 'view' = 'edit') {
    editTitle.value = $t(mode === 'view' ? 'lz.label.vdsSegment.detail' : row?.id ? 'lz.label.vdsSegment.edit' : 'lz.label.vdsSegment.add');
    editRef.value?.openDialog(row, mode);
}
const editRef = ref<InstanceType<typeof EditSegment>>();
const rows = ref<SegmentOutput[]>([]);
const cameras = ref<VdsCameraOption[]>([]);
const directions = ref<{ code: string; name: string }[]>([]);
const query = reactive({ keyword: '', fromEquipmentId: '', toEquipmentId: '', direction: '', status: '' });
const appliedQuery = ref({ ...query });
const loading = ref(false);
const deletingId = ref('');
const error = ref('');
const cameraMap = computed(() => new Map(cameras.value.map(camera => [camera.id, camera])));
const cameraName = (id?: string | null) => cameraMap.value.has(id ?? '') ? cameraLabel(cameraMap.value.get(id ?? '')!) : id || '-';
const directionName = (code?: string | null) => directions.value.find(item => item.code === code)?.name ?? code ?? '-';
const filteredRows = computed(() => rows.value.filter(row => {
	const filter = appliedQuery.value;
	if (filter.status && row.status !== filter.status) return false;
	if (filter.fromEquipmentId && row.fromEquipmentId !== filter.fromEquipmentId) return false;
	if (filter.toEquipmentId && row.toEquipmentId !== filter.toEquipmentId) return false;
	if (filter.direction && row.direction !== filter.direction) return false;
	const term = filter.keyword.trim().toLocaleLowerCase('vi');
	return !term || (row.name ?? '').toLocaleLowerCase('vi').includes(term);
}));

const options = useVxeTable<SegmentOutput>({
	id: 'vdsSegmentGrid', name: 'vdsSegmentGrid',
	columns: [
		{ field: 'name', title: 'Tên đoạn', minWidth: 180, showOverflow: 'tooltip' },
		{ field: 'fromEquipmentId', title: 'Camera đầu (A)', minWidth: 220, showOverflow: 'tooltip', slots: { default: 'from' } },
		{ field: 'toEquipmentId', title: 'Camera cuối (B)', minWidth: 220, showOverflow: 'tooltip', slots: { default: 'to' } },
		{ field: 'direction', title: 'Chiều', width: 130, slots: { default: 'direction' } },
		{ field: 'distanceMeter', title: 'Khoảng cách (m)', width: 155 },
		{ field: 'status', title: 'Trạng thái', width: 115, slots: { default: 'status' } },
		{ field: '', title: $t('lz.label.base.audit'), width: 100, slots: { default: 'row_record' } },
		{ title: $t('lz.label.base.action'), width: 180, fixed: 'right', slots: { default: 'actions' } },
	],
}, { proxyConfig: { enabled: false }, pagerConfig: { enabled: false }, toolbarConfig: { export: false } });

async function loadSegments() {
	loading.value = true;
	error.value = '';
	try {
		const response = await getAPI(VdsSegmentApi).apiVdsVdssegmentListGet();
		if (response.data.succeeded === false) throw new Error(response.data.message || 'Không tải được danh sách đoạn camera.');
		rows.value = response.data.result ?? [];
		appliedQuery.value = { ...query };
	} catch (cause) {
		error.value = cause instanceof Error ? cause.message : 'Không tải được danh sách đoạn camera.';
	} finally { loading.value = false; }
}

function resetQuery() { Object.assign(query, { keyword: '', fromEquipmentId: '', toEquipmentId: '', direction: '', status: '' }); void loadSegments(); }

async function deleteSegment(row: SegmentOutput) {
	if (!row.id || deletingId.value) return;
	try { await ElMessageBox.confirm(`Xóa đoạn ${row.name || row.id}?`, 'Xác nhận', { type: 'warning' }); }
	catch { return; }
	deletingId.value = row.id;
	try {
		const response = await getAPI(VdsSegmentApi).apiVdsVdssegmentDeletePost({ id: row.id });
		if (response.data.succeeded === false || response.data.result?.saved !== true) throw new Error(response.data.message || 'Không xóa được đoạn camera.');
		ElMessage.success('Đã xóa đoạn camera.');
		if (response.data.result.applied !== true) {
			const transports = Object.values(response.data.result.refresh ?? {});
			if (transports.length && transports.every(Boolean)) ElMessage.info('Đã gửi yêu cầu cập nhật. Chưa xác nhận Worker đã áp dụng.');
			else ElMessage.warning('Đã xóa trong DB nhưng chưa gửi được yêu cầu cập nhật Worker.');
		}
		await loadSegments();
	} catch (cause) { error.value = cause instanceof Error ? cause.message : 'Không xóa được đoạn camera.'; }
	finally { deletingId.value = ''; }
}

onMounted(async () => {
	try {
		const baseConfig = useBaseConfig();
		if (Object.keys(baseConfig.configList ?? {}).length === 0) await baseConfig.setConfigDataList();
		directions.value = (baseConfig.getConfigDataByCode(BaseConfigTypeEnum.TmsDirectionType) ?? [])
			.map(item => ({ code: String(item.code), name: item.name }));
		cameras.value = await loadVdsCameras();
	} catch { error.value = 'Không tải được danh sách camera VDS hoặc chiều lưu thông.'; }
	await loadSegments();
});
</script>
