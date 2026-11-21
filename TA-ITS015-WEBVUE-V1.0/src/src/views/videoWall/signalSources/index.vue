<template>
	<WorkspacePage>
	<div class="videowall-source-container">
		<GridSearchPanel id="videowall-source-search">
			<template #filters>
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true"
				label-width="auto" style="flex: 1 1 0%" label-position="left">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.videoWall.codeOrName')" prop="name">
							<el-input v-model="state.queryParams.name" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.label.videoWall.codeOrName') })" clearable
								@keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.videoWall.sourceType')" prop="sourceType">
							<el-select v-model="state.queryParams.sourceType" :placeholder="$t('lz.label.videoWall.selectSourceType')" clearable class="w100">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.VideoWallSourceType)" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.videoWall.controller')" prop="controllerId">
							<!-- Cây bộ điều khiển cha → con; lọc thì chọn được cả bộ cha (checkStrictly) — như trang Màn hình -->
							<el-cascader v-model="state.queryParams.controllerId" :options="controllerTree" :props="{ ...CONTROLLER_CASCADER_PROPS, checkStrictly: true }"
								:placeholder="$t('lz.label.videoWall.selectController')" filterable clearable class="w100" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.status')" prop="status">
							<el-select v-model="state.queryParams.status" filterable clearable class="w100" @clear="state.queryParams.status = undefined"
								:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.base.status') })">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code" :label="item.name" :value="parseInt(item.code)" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>

			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />

			<el-row>
				<SearchButton :auth-search="'videoWallSource:page'" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
			</el-row>
			</template>

		<el-card class="full-table" shadow="hover">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents"
				@checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">
				<template #toolbar_buttons>
					<GridSummary :summary="summary" />
				</template>
				<template #toolbar_tools>
					<TableHeader @add="handleAdd" :show-delete="state.showBatchDelete" :auth-add="'videoWallSource:add'"
						:auth-delete="'videoWallSource:delete'" @refresh="handleQuery" @custom-column="handleCustom"
						@delete="handleBatchDelete">
					</TableHeader>
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_source_type="{ row }">
					<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.VideoWallSourceType, row.sourceType)" />
				</template>
				<template #row_controller="{ row }">
					<!-- Cùng dạng với ô lọc bộ điều khiển: "Ctrl X — Tên" -->
					{{ vwCtrl.find(row.controllerId) ? `${vwCtrl.label(row.controllerId)} — ${vwCtrl.find(row.controllerId)?.name ?? ''}` : vwCtrl.label(row.controllerId, '—') }}
				</template>
				<template #row_endpoint="{ row }">
					{{ row.sourceType === VideoWallSourceTypeEnum.LocalSignal || row.sourceType === VideoWallSourceTypeEnum.CascadeLink ? (row.inputPort != null ? $t('lz.label.videoWall.inputPortN', { port: row.inputPort }) : '') : row.url }}
				</template>
				<template #row_signal_status="{ row }">
					<el-tag v-if="row.signalStatus" size="small" :type="signalStatusTag(row.signalStatus)">{{ signalStatusLabel(row.signalStatus) }}</el-tag>
				</template>
				<template #row_status="{ row }">
					<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, row.status)" />
				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
						<el-button icon="ele-CopyDocument" text type="warning" v-auth="'videoWallSource:add'" @click="handleCopy(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
						<el-button icon="ele-Edit" text type="primary" v-auth="'videoWallSource:update'" @click="handleEdit(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger" v-auth="'videoWallSource:delete'" @click="handleDelete(row)"> </el-button>
					</el-tooltip>
				</template>
			</vxe-grid>
		</el-card>
		</GridSearchPanel>

		<EditSource ref="editSourceRef" :title="state.title" :operateType="state.operateType" :controllers="vwCtrl.controllers" @handleQuery="handleQuery" />
	</div>
	</WorkspacePage>
</template>

<script lang="ts" setup name="videoWallSource">
import { computed, onActivated, onMounted, reactive, ref, defineAsyncComponent } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { Local } from '/@/utils/storage';
import EditSource from './component/editSource.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import { getAPI } from '/@/utils/axios-utils';
import { VwSourceApi } from '/@/api-services/videoWall';
import { VwPageSourceInput, VwPageSourceOutput } from '/@/api-services/videoWall';
import { useVideoWallController } from '/@/stores/its/videoWallController';
import { buildControllerTree, CONTROLLER_CASCADER_PROPS } from '/@/utils/videoWall/controllerTree';
import { OperateType } from '/@/types/constants/operate';
import { BaseConfigTypeEnum, VideoWallSourceTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import WorkspacePage from '/@/components/workspace/WorkspacePage.vue';
import GridSearchPanel from '/@/components/gridPage/GridSearchPanel.vue';
import GridSummary from '/@/components/gridPage/GridSummary.vue';
import { useGridSummary } from '/@/components/gridPage/useGridSummary';
import { useGridPager } from '/@/components/gridPage/useGridPager';


const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;

const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
const { t: $t } = useI18n();
const vwCtrl = useVideoWallController();
const controllerTree = computed(() => buildControllerTree(vwCtrl.controllers));
const xGrid = ref<VxeGridInstance>();
const { summary, track } = useGridSummary();
const editSourceRef = ref<InstanceType<typeof EditSource>>();
const state = reactive({
	queryParams: {
		name: undefined as unknown as string,
		sourceType: undefined as unknown as string,
		controllerId: undefined as unknown as string,
		status: undefined as unknown as any,
	},
	localPageParam: {
		pageSize: 50 as number,
		defaultSort: { field: 'orderNo', order: 'asc', descStr: 'desc' } as any,
	},
	title: '',
	operateType: 'add' as OperateType,
	showBatchDelete: false,
});
// Local storage parameters
const localPageParamKey = 'localPageParam:videoWallSource';
// Table parameter configuration
const options = useVxeTable<VwPageSourceOutput>(
	{
		id: 'videoWallSource',
		name: 'VideoWallSource',
		columns: [
			{ type: 'checkbox', fixed: 'left', width: 45 },
			{ field: 'code', title: $t('lz.entity.base.code'), minWidth: 120, showOverflow: 'tooltip', sortable: true },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 160, showOverflow: 'tooltip', sortable: true },
			{ field: 'sourceType', title: $t('lz.entity.videoWall.sourceType'), width: 140, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_source_type' } },
			{
				field: 'signalType',
				title: $t('lz.entity.videoWall.signalType'),
				width: 130,
				showOverflow: 'tooltip',
				formatter: ({ cellValue }: any) => getConfigItemByCode(BaseConfigTypeEnum.VideoWallSignalType, cellValue)?.name ?? cellValue ?? '',
			},
			{ field: 'controllerId', title: $t('lz.entity.videoWall.controller'), width: 220, showOverflow: 'tooltip', slots: { default: 'row_controller' } },
			{ field: 'url', title: $t('lz.entity.videoWall.endpoint'), minWidth: 240, showOverflow: 'tooltip', slots: { default: 'row_endpoint' } },
			//TODO: Ẩn cột tín hiệu tạm thời, vì chưa có dữ liệu tín hiệu từ thiết bị => check lại đang lệch typeCheck giữa FE, BE, Service, DB 
			// { field: 'signalStatus', title: 'Tín hiệu', width: 110, showOverflow: 'tooltip', slots: { default: 'row_signal_status' } },
			{ field: 'status', title: $t('lz.entity.base.status'), width: 110, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_status' } },
			{ field: 'orderNo', title: $t('lz.entity.base.orderNo'), width: 90, sortable: true },
			{ field: '', title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 130, showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	{
		// Khung đổi kích thước (thu / mở menu, đổi tab) ⇒ tính lại độ rộng cột, cột Hành động luôn sát mép phải
		autoResize: true,
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		sortConfig: { defaultSort: state.localPageParam.defaultSort },
		pagerConfig: { pageSize: state.localPageParam.pageSize },
		toolbarConfig: { export: false },
		rowConfig: { height: 66 },
	}
);

// Thu gọn thanh phân trang theo chiều rộng bảng.
useGridPager(xGrid, options);

// Page initialization
onMounted(async () => {
	state.localPageParam = Local.get(localPageParamKey) || state.localPageParam;
	await vwCtrl.load();
});

// Quay lại tab / menu (KeepAlive): tải lại bảng + bộ điều khiển (có thể đã đổi ở tab khác).
// onActivated cũng chạy ngay sau lần mount đầu, khi đó dữ liệu vừa tải nên bỏ qua.
let activated = false;
onActivated(async () => {
	if (!activated) {
		activated = true;
		return;
	}
	await Promise.all([xGrid.value?.commitProxy('query'), vwCtrl.load(true).catch(() => {})]);
});

// Query API
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const sortField = sort?.field || state.localPageParam.defaultSort.field;
	const sortOrder = sort?.order || state.localPageParam.defaultSort.order;
	const params = Object.assign(state.queryParams, { page: page.currentPage, pageSize: page.pageSize, field: sortField, order: sortOrder, descStr: 'desc' }) as VwPageSourceInput;
	return track(getAPI(VwSourceApi).apiVideowallVwsourcePagePost(params));
};

// Query operation
const handleQuery = async () => {
	options.loading = true;
	await xGrid.value?.commitProxy('reload');
	options.loading = false;
	selectChangeEvent();
};

// Reset operation
const resetQuery = async () => {
	state.queryParams.name = undefined as unknown as string;
	state.queryParams.sourceType = undefined as unknown as string;
	state.queryParams.controllerId = undefined as unknown as string;
	state.queryParams.status = undefined as unknown as any;
	await xGrid.value?.commitProxy('reload');
};

// Open add dialog
const handleAdd = () => {
	state.title = $t('lz.label.base.titleAdd', { name: $t('lz.entity.videoWall.source') });
	state.operateType = 'add';
	editSourceRef.value?.openDialog({ status: 1, orderNo: 100 });
};

// Open copy dialog
const handleCopy = (row: any) => {
	state.title = $t('lz.label.base.titleCopy', { name: $t('lz.entity.videoWall.source') });
	state.operateType = 'copy';
	editSourceRef.value?.openDialog(row);
};

// Open edit dialog
const handleEdit = (row: any) => {
	state.title = $t('lz.label.base.titleEdit', { name: $t('lz.entity.videoWall.source') });
	state.operateType = 'edit';
	editSourceRef.value?.openDialog(row);
};

// Delete
const handleDelete = (row: any) => {
	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.name }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(VwSourceApi).apiVideowallVwsourceDeletePost({ id: row.id });
			handleQuery();
			ElMessage.success($t('lz.message.base.deleteSuccess'));
		})
		.catch(() => {});
};

// Batch delete
const handleBatchDelete = async () => {
	const selectRecords = getCheckedRecords();
	ElMessageBox.confirm($t('lz.message.base.confirmBatchDelete', { total: selectRecords.length }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(VwSourceApi).apiVideowallVwsourceBatchdeletePost(selectRecords.filter((l) => l.id).map((l) => ({ id: l.id! })));
			handleQuery();
			ElMessage.success($t('lz.message.base.deleteSuccess'));
		})
		.catch(() => {});
};

const signalStatusLabel = (value: string) => ({ Signal: $t('lz.label.videoWall.signal'), NoSignal: $t('lz.label.videoWall.noSignal'), Abnormal: $t('lz.label.videoWall.abnormal') } as Record<string, string>)[value] ?? value;
const signalStatusTag = (value: string) => (value === 'Signal' ? 'success' : value === 'Abnormal' ? 'warning' : 'danger');

// Custom column
function handleCustom() {
	xGrid.value?.openCustom();
}
// Get selected records
function getCheckedRecords() {
	return (xGrid.value?.getCheckboxRecords() as VwPageSourceOutput[]) ?? [];
}

function selectChangeEvent() {
	state.showBatchDelete = getCheckedRecords().length > 0;
}

// Table events
const gridEvents: VxeGridListeners<VwPageSourceOutput> = {
	async pageChange({ pageSize }) {
		state.localPageParam.pageSize = pageSize;
	},
	async sortChange({ field, order }) {
		state.localPageParam.defaultSort = { field: field, order: order!, descStr: 'desc' };
	},
};
</script>
