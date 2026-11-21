<template>
	<WorkspacePage>
	<div class="event-default-container">
		<!-- SearchBar: begin -->
		<GridSearchPanel id="vms-event-default-content-search">
				<template #filters>
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%" label-position="left">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="24" :md="12" :lg="12" :xl="12">
						<el-form-item :label="$t('lz.entity.vmsEventDefault.eventTypeId')" prop="eventTypeId">
							<el-select v-model="state.queryParams.eventTypeId" filterable clearable class="w100" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.vmsEventDefault.eventTypeId') })">
								<el-option v-for="item in state.eventTypeList" :key="item.id" :label="item.name" :value="item.id" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />
			<el-row>
				<SearchButton :auth-search="'vmsEventDefaultContent:page'" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
			</el-row>
				</template>
		<!-- SearchBar: end -->

		<!-- GridTable: begin -->
		<el-card class="full-table" shadow="hover">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents" @checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">
				<template #toolbar_buttons>
					<GridSummary :summary="summary" />
				</template>
				<template #toolbar_tools>
					<TableHeader :auth-add="'vmsEventDefaultContent:add'" :show-delete="state.showBatchDelete" :auth-delete="'vmsEventDefaultContent:delete'" @add="handleAdd" @refresh="handleQuery" @custom-column="handleCustom" @delete="handleBatchDelete" />
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>

				<!-- Loại sự kiện sự cố -->
				<template #row_eventType="{ row }">
					{{ state.eventTypeMap[row.eventTypeId]?.name || '-' }}
				</template>

				<!-- Loại thiết bị (VMS/LCS) -->
				<template #row_equipmentType="{ row }">
					<el-tag size="small" :type="state.equipmentTypeMap[row.equipmentTypeId]?.code === TmsEquipmentType.LCS ? 'warning' : 'primary'">
						{{ state.equipmentTypeMap[row.equipmentTypeId]?.name || '-' }}
					</el-tag>
				</template>

				<!-- Nội dung (parse JSON RowData) -->
				<template #row_content="{ row }">
					<template v-if="row.rowData">
						<div>{{ parseRowData(row.rowData).row1 }}</div>
						<div>{{ parseRowData(row.rowData).row2 }}</div>
					</template>
					<span v-else>-</span>
				</template>

				<!-- Ảnh preview -->
				<template #row_preview="{ row }">
					<div class="panel-img">
						<el-image :src="buildUrl(row.url)" :preview-src-list="[buildUrl(row.url)]" :preview-teleported="true" fit="contain" hide-on-click-modal>
							<template #error><img :src="noImageUrl" /></template>
						</el-image>
					</div>
				</template>

				<!-- Chế độ phát -->
				<template #row_playMode="{ row }">
					<el-tag size="small" :type="row.defaultPlayMode === 'append' ? 'success' : 'info'">
						{{ row.defaultPlayMode === 'append' ? $t('lz.label.vms.playModeAppend') : $t('lz.label.vms.playModeOverride') }}
					</el-tag>
				</template>

				<!-- Thời gian hiển thị -->
				<template #row_ttl="{ row }">
					<span>{{ row.timeToLive || 30 }}s</span>
				</template>

				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
						<el-button icon="ele-Edit" text type="primary" v-auth="'vmsEventDefaultContent:update'" @click="handleEdit(row)" />
					</el-tooltip>
					<!-- <el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger" v-auth="'vmsEventDefaultContent:delete'" @click="handleDelete(row)" />
					</el-tooltip> -->
				</template>
			</vxe-grid>
		</el-card>
		</GridSearchPanel>
		<!-- GridTable: end -->

		<!-- Dialog thêm/sửa -->
		<editEventDefaultContent ref="editRef" :event-type-list="selectableEventTypes" :equipment-type-list="state.equipmentTypeList" @handleQuery="handleQuery" />
	</div>
	</WorkspacePage>
</template>

<script lang="ts" setup name="vmsEventDefaultContent">
import { onMounted, onActivated, reactive, ref, defineAsyncComponent, computed } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import WorkspacePage from '/@/components/workspace/WorkspacePage.vue';
import GridSearchPanel from '/@/components/gridPage/GridSearchPanel.vue';
import GridSummary from '/@/components/gridPage/GridSummary.vue';
import { useGridSummary } from '/@/components/gridPage/useGridSummary';
import { useGridPager } from '/@/components/gridPage/useGridPager';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { getAPI } from '/@/utils/axios-utils';
import { VmsEventDefaultApi } from '/@/api-services/vms';
import { PageVmsEventDefaultInput, PageVmsEventDefaultOutput } from '/@/api-services/vms';
import { TmsEventTypeApi, TmsEquipmentTypeApi } from '/@/api-services/tms';
import { useMapStore } from '/@/stores/its/mapStore';

// Code loại thiết bị được phép xử lý ở trang này
const ALLOWED_TYPE_CODES = [TmsEquipmentType.Vms, TmsEquipmentType.LCS];

import editEventDefaultContent from '/@/views/vms/eventDefaultContent/component/editEventDefaultContent.vue';
import { TmsEquipmentType } from '/@/types/enums';

const { t: $t } = useI18n();
const mapStore = useMapStore();

const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));

const xGrid = ref<VxeGridInstance>();
const { summary, track } = useGridSummary();
const editRef = ref<InstanceType<typeof editEventDefaultContent>>();

const noImageUrl = `${import.meta.env.BASE_URL}Upload/noimage.jpg`;
const buildUrl = (u?: string) => (u ? `${mapStore.baseUrl}/${u}` : noImageUrl);

// Parse RowData (chịu được PascalCase/camelCase)
const parseRowData = (raw: any) => {
	try {
		const obj = typeof raw === 'string' ? JSON.parse(raw) : raw || {};
		return { row1: obj?.Row1 ?? obj?.row1 ?? '', row2: obj?.Row2 ?? obj?.row2 ?? '' };
	} catch {
		return { row1: '', row2: '' };
	}
};

const selectableEventTypes = computed(() => state.eventTypeList.filter((item: any) => item.code !== 'unknown'));

const state = reactive({
	queryParams: { eventTypeId: undefined as any },
	localPageParam: {
		pageSize: 50 as number,
		defaultSort: { field: 'createTime', order: 'desc', descStr: 'desc' } as any,
	},
	showBatchDelete: false,
	eventTypeList: [] as any[], // danh sách loại sự kiện sự cố (TmsEventType)
	eventTypeMap: {} as Record<string, any>, // eventTypeId -> event type (để hiển thị tên trong grid)
	equipmentTypeList: [] as any[], // danh sách loại thiết bị VMS/LCS (TmsEquipmentType)
	equipmentTypeMap: {} as Record<string, any>, // equipmentTypeId -> equipment type (để hiển thị tên/code trong grid)
});

const options = useVxeTable<PageVmsEventDefaultOutput>(
	{
		id: 'vmsEventDefaultContentGrid',
		name: 'vmsEventDefaultContentGrid',
		columns: [
			{ type: 'checkbox', fixed: 'left', width: 45 },
			{ field: 'eventTypeId', title: $t('lz.entity.vmsEventDefault.eventTypeId'), minWidth: 180, showOverflow: 'tooltip', slots: { default: 'row_eventType' } },
			{ field: 'equipmentTypeId', title: $t('lz.entity.vmsEventDefault.equipmentTypeId'), width: 110, slots: { default: 'row_equipmentType' } },
			{ field: 'rowData', title: $t('lz.entity.vmsEventDefault.rowData'), minWidth: 220, showOverflow: 'tooltip', slots: { default: 'row_content' } },
			{ field: 'defaultPlayMode', title: $t('lz.label.vms.playMode'), width: 130, slots: { default: 'row_playMode' } },
			{ field: 'timeToLive', title: $t('lz.label.vms.timeToLive'), width: 130, slots: { default: 'row_ttl' } },
			{ field: 'url', title: $t('lz.label.base.preview'), minWidth: 200, slots: { default: 'row_preview' } },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 110, slots: { default: 'row_buttons' } },
		],
	},
	{
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		sortConfig: { defaultSort: state.localPageParam.defaultSort },
		pagerConfig: { pageSize: state.localPageParam.pageSize },
		toolbarConfig: { export: false },
		rowConfig: { height: 120 },
	}
);

// Thu gọn thanh phân trang theo chiều rộng bảng.
useGridPager(xGrid, options);

// Quay lại tab/menu (KeepAlive): tải lại trang hiện tại để nhận thay đổi từ tab khác.
// onActivated cũng chạy ngay sau lần mount đầu, khi đó dữ liệu vừa tải nên bỏ qua.
let activated = false;
onActivated(async () => {
	if (!activated) {
		activated = true;
		return;
	}
	await xGrid.value?.commitProxy('query');
});

onMounted(async () => {
	// Đảm bảo có baseUrl để ghép URL ảnh preview
	if (!mapStore.baseUrl) {
		try {
			await mapStore.fetchSystemSysconfigFileserverGet();
		} catch (e) {
			console.error('Error fetching file server:', e);
		}
	}

	// Nạp danh sách loại sự kiện sự cố 1 lần -> map eventTypeId để hiển thị tên
	try {
		const res = await getAPI(TmsEventTypeApi).apiTmsTmseventtypeListGet();
		state.eventTypeList = res.data?.result ?? [];
		state.eventTypeList.forEach((i: any) => (state.eventTypeMap[i.id] = i));
	} catch (e) {
		console.error('Error loading event type list:', e);
	}

	// Nạp danh sách loại thiết bị, chỉ giữ VMS/LCS còn hiệu lực (IsDelete == null)
	try {
		const resType = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet();
		state.equipmentTypeList = (resType.data?.result ?? []).filter((t: any) => t.isDelete == null && ALLOWED_TYPE_CODES.includes(t.code));
		state.equipmentTypeList.forEach((i: any) => (state.equipmentTypeMap[i.id] = i));
	} catch (e) {
		console.error('Error loading equipment type list:', e);
	}

	await handleQuery();
});

// Query API
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params = {
		eventTypeId: state.queryParams.eventTypeId,
		page: page.currentPage,
		pageSize: page.pageSize,
		field: sort.field,
		order: sort.order,
		descStr: 'desc',
	} as PageVmsEventDefaultInput;

	return track(getAPI(VmsEventDefaultApi).apiVmsVmseventdefaultPagePost(params));
};

const handleQuery = async () => {
	options.loading = true;
	await xGrid.value?.commitProxy('reload');
	options.loading = false;
	selectChangeEvent();
};

const resetQuery = async () => {
	state.queryParams.eventTypeId = undefined;
	await xGrid.value?.commitProxy('reload');
};

// Thêm mới
const handleAdd = () => {
	editRef.value?.openDialog(null);
};

// Sửa
const handleEdit = (row: any) => {
	editRef.value?.openDialog(row);
};

// Xóa
const handleDelete = (row: any) => {
	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: state.eventTypeMap[row.eventTypeId]?.name || row.id }), $t('lz.label.base.confirm'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			try {
				await getAPI(VmsEventDefaultApi).apiVmsVmseventdefaultDeletePost({ id: row.id });
				handleQuery();
				ElMessage.success($t('lz.message.base.processSuccess'));
			} catch (error) {
				ElMessage.error($t('lz.message.base.processUnSuccess'));
			}
		})
		.catch(() => {});
};

function handleCustom() {
	xGrid.value?.openCustom();
}

function getCheckedRecords() {
	return xGrid.value?.getCheckboxRecords() ?? [];
}

function selectChangeEvent() {
	state.showBatchDelete = getCheckedRecords().length > 0;
}

// Xóa hàng loạt (lặp từng dòng vì BE xóa theo 1 ID)
const handleBatchDelete = async () => {
	const selectRecords = getCheckedRecords();
	ElMessageBox.confirm($t('lz.message.base.confirmBatchDelete', { total: selectRecords.length }), $t('lz.label.base.confirm'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			try {
				await Promise.all(selectRecords.filter((l: any) => l.id).map((l: any) => getAPI(VmsEventDefaultApi).apiVmsVmseventdefaultDeletePost({ id: l.id })));
				handleQuery();
				ElMessage.success($t('lz.message.base.processSuccess'));
			} catch (error) {
				ElMessage.error($t('lz.message.base.processUnSuccess'));
			}
		})
		.catch(() => {});
};

const gridEvents: VxeGridListeners<PageVmsEventDefaultOutput> = {
	async pageChange({ pageSize }) {
		state.localPageParam.pageSize = pageSize;
	},
	async sortChange({ field, order }) {
		state.localPageParam.defaultSort = { field: field, order: order! };
	},
};
</script>

<style scoped>
.panel-img :deep(img) {
	max-width: 100%;
	max-height: 110px;
	padding: 6px;
}
</style>
