<template>
	<div class="videowall-output-container">
		<!-- Danh sách màn (tài sản). Sơ đồ cổng ra là tab menu riêng (menu "Màn hình" dùng /tabPage/index) -->
			<div class="vw-screens-list">
		<GridSearchPanel id="videowall-screen-search">
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
						<el-form-item :label="$t('lz.label.videoWall.installState')" prop="installed">
							<el-select v-model="state.queryParams.installed" clearable class="w100" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.label.videoWall.installState') })">
								<el-option :value="true" :label="$t('lz.label.videoWall.screenInstalled')" />
								<el-option :value="false" :label="$t('lz.label.videoWall.screenInStock')" />
							</el-select>
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
				<SearchButton :auth-search="'videoWallOutput:page'" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
			</el-row>
			</template>

		<el-card class="full-table" shadow="hover">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents"
				@checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">
				<template #toolbar_tools>
					<TableHeader @add="handleAdd" :show-delete="state.showBatchDelete" :auth-add="'videoWallOutput:add'"
						:auth-delete="'videoWallOutput:delete'" @refresh="handleQuery" @custom-column="handleCustom"
						@delete="handleBatchDelete">
						<template #prefix>
							<el-button type="primary" icon="ele-Download" :loading="state.syncing" v-auth="'videoWallOutput:update'" @click="syncScreens">
								{{ $t('lz.button.videoWall.syncScreens') }}
							</el-button>
						</template>
					</TableHeader>
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_position="{ row }">
					<span v-if="row.gridRow != null && row.gridCol != null">{{ cellLabel(row.blockNo, row.gridRow, row.gridCol, topology.isMultiBlock) }}</span>
					<span v-else style="color: var(--el-text-color-placeholder)">{{ $t('lz.label.videoWall.notPlaced') }}</span>
				</template>
				
				<template #row_screen_state="{ row }">
					<el-tag v-if="row.screenState" size="small" :type="screenStateTag(row.screenState)">{{ screenStateLabel(row.screenState) }}</el-tag>
					<span v-else style="color: var(--el-text-color-placeholder)">{{ $t('lz.label.videoWall.undetermined') }}</span>
				</template>
				<template #row_status="{ row }">
					<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, row.status)" />
				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
						<el-button icon="ele-CopyDocument" text type="warning" v-auth="'videoWallOutput:add'" @click="handleCopy(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
						<el-button icon="ele-Edit" text type="primary" v-auth="'videoWallOutput:update'" @click="handleEdit(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger" v-auth="'videoWallOutput:delete'" @click="handleDelete(row)"> </el-button>
					</el-tooltip>
				</template>
			</vxe-grid>
		</el-card>
		</GridSearchPanel>
			</div>

		<EditOutput ref="editOutputRef" :title="state.title" :operateType="state.operateType" @handleQuery="handleQuery" />
	</div>
</template>

<script lang="ts" setup name="videoWallOutput">
import { onMounted, reactive, ref, defineAsyncComponent } from 'vue';
import GridSearchPanel from '/@/components/gridPage/GridSearchPanel.vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { Local } from '/@/utils/storage';
import EditOutput from './component/editOutput.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import { getAPI } from '/@/utils/axios-utils';
import { VwDeviceSetupApi, VwScreenApi } from '/@/api-services/videoWall';
import { VwPageScreenInput, VwPageScreenOutput } from '/@/api-services/videoWall';
import { useVideoWallController } from '/@/stores/its/videoWallController';
import { cellLabel, useVideoWallTopology } from '/@/stores/its/videoWallTopology';
import { OperateType } from '/@/types/constants/operate';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';


const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;

const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
const { t: $t } = useI18n();
const vwCtrl = useVideoWallController();
const topology = useVideoWallTopology();
const xGrid = ref<VxeGridInstance>();
const editOutputRef = ref<InstanceType<typeof EditOutput>>();
/** Worker đồng bộ bất đồng bộ (NATS) ⇒ nạp lại sau khoảng này. */
const SYNC_RELOAD_MS = 4000;
const state = reactive({
	syncing: false,
	queryParams: {
		name: undefined as unknown as string,
		installed: undefined as boolean | undefined,
		panelType: undefined as unknown as string,
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
const localPageParamKey = 'localPageParam:videoWallOutput';
// Table parameter configuration
const options = useVxeTable<VwPageScreenOutput>(
	{
		id: 'videoWallOutput',
		name: 'VideoWallOutput',
		columns: [
			{ type: 'checkbox', fixed: 'left', width: 45 },
			{ field: 'code', title: $t('lz.entity.base.code'), minWidth: 120, showOverflow: 'tooltip', sortable: true },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 160, showOverflow: 'tooltip', sortable: true },
			{ field: 'serialNumber', title: $t('lz.entity.videoWall.serialNumber'), minWidth: 130, showOverflow: 'tooltip', sortable: true },
			{ field: 'outputName', title: $t('lz.entity.videoWall.outputPort'), width: 120, showOverflow: 'tooltip' },
			{ field: 'gridRow', title: $t('lz.entity.videoWall.position'), titlePrefix: { content: $t('lz.label.videoWall.cellLabelHint') }, width: 110, showOverflow: 'tooltip', slots: { default: 'row_position' } },
			// { field: 'panelType', title: $t('lz.entity.videoWall.panelType'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_panel_type' } },
			{ field: 'panelSize', title: $t('lz.entity.videoWall.panelSize'), width: 110, sortable: true },
			{ field: 'screenState', title: $t('lz.entity.videoWall.connection'), width: 120, showOverflow: 'tooltip', slots: { default: 'row_screen_state' } },
			{ field: 'temperature', title: $t('lz.entity.videoWall.temperature'), width: 100, sortable: true, formatter: ({ cellValue }: any) => (cellValue ? `${cellValue} °C` : '') },
			{ field: 'runTimeHours', title: $t('lz.entity.videoWall.runTimeHours'), width: 110, sortable: true },
			// Độ sáng (đèn nền 0-100) theo lần đồng bộ / chỉnh gần nhất — chỉnh ở tab Bố cục màn
			{ field: 'brightness', title: $t('lz.label.videoWall.brightness'), width: 100 },
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

// Page initialization
onMounted(async () => {
	state.localPageParam = Local.get(localPageParamKey) || state.localPageParam;
	await Promise.all([vwCtrl.load(), topology.load()]);
});

// Query API
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const sortField = sort?.field || state.localPageParam.defaultSort.field;
	const sortOrder = sort?.order || state.localPageParam.defaultSort.order;
	const params = Object.assign(state.queryParams, { page: page.currentPage, pageSize: page.pageSize, field: sortField, order: sortOrder, descStr: 'desc' }) as VwPageScreenInput;
	return await getAPI(VwScreenApi).apiVideowallVwscreenPagePost(params);
};

// Query operation
const handleQuery = async () => {
	options.loading = true;
	await xGrid.value?.commitProxy('reload');
	options.loading = false;
	selectChangeEvent();
};

/** Đồng bộ màn đang cắm từ các bộ con (cần đã có cổng ra) rồi nạp lại. Worker chạy bất đồng bộ (NATS). */
const syncScreens = async () => {
	state.syncing = true;
	try {
		const res = await getAPI(VwDeviceSetupApi).apiVideowallVwdevicesetupSyncscreensPost({});
		if (!res.data.result?.accepted) return ElMessage.warning($t('lz.message.videoWall.commandRejected'));
		ElMessage.success($t('lz.message.videoWall.syncScreensSent'));
		// Giống Đồng bộ cổng ra: gửi lệnh rồi nạp lại sau thời gian cố định, không chờ service trả kết quả
		// (12/11/2026: server có thể chỉ chạy FE + BE — chờ MessageId làm nút quay tới 120 s)
		await new Promise((r) => setTimeout(r, SYNC_RELOAD_MS));
		await handleQuery();
	} finally {
		state.syncing = false;
	}
};

// Reset operation
const resetQuery = async () => {
	state.queryParams.name = undefined as unknown as string;
	state.queryParams.installed = undefined;
	state.queryParams.panelType = undefined as unknown as string;
	state.queryParams.status = undefined as unknown as any;
	await xGrid.value?.commitProxy('reload');
};

// Open add dialog — mặc định theo màn hình mẫu của tường (chưa khai thì panel 55" 4K đang dùng).
const handleAdd = async () => {
	state.title = $t('lz.label.base.titleAdd', { name: $t('lz.entity.videoWall.outputScreen') });
	state.operateType = 'add';
	const t = topology.topology;
	editOutputRef.value?.openDialog({
		status: 1,
		panelModel: t?.defaultPanelModel ?? null,
		panelType: t?.defaultPanelType || 'LCD',
		panelSize: t?.defaultPanelSize ?? 55,
		resolution: t?.defaultResolution || '3840x2160@30Hz',
		// Màn tạo tay = màn trong kho; cắm vào cổng nào do đồng bộ thiết bị ghi nhận
		orderNo: 100,
	});
};

// Open copy dialog
const handleCopy = async (row: any) => {
	state.title = $t('lz.label.base.titleCopy', { name: $t('lz.entity.videoWall.outputScreen') });
	state.operateType = 'copy';
	// Bản sao là màn trong kho: không kế thừa cổng đang cắm / serial
	editOutputRef.value?.openDialog({ ...row, controllerOutputId: null, serialNumber: null, outputName: null, controllerId: null, blockNo: null, gridRow: null, gridCol: null });
};

// Open edit dialog
const handleEdit = (row: any) => {
	state.title = $t('lz.label.base.titleEdit', { name: $t('lz.entity.videoWall.outputScreen') });
	state.operateType = 'edit';
	editOutputRef.value?.openDialog(row);
};

// Delete
const handleDelete = (row: any) => {
	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.name }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(VwScreenApi).apiVideowallVwscreenDeletePost({ id: row.id });
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
			await getAPI(VwScreenApi).apiVideowallVwscreenBatchdeletePost(selectRecords.filter((l) => l.id).map((l) => ({ id: l.id! })));
			handleQuery();
			ElMessage.success($t('lz.message.base.deleteSuccess'));
		})
		.catch(() => {});
};

// Trạng thái kết nối do Worker ghi: Online / Offline / Warning.
const screenStateLabel = (value: string) => ({ Online: $t('lz.label.videoWall.online'), Offline: $t('lz.label.videoWall.offline'), Warning: $t('lz.label.base.warning') } as Record<string, string>)[value] ?? value;
const screenStateTag = (value: string) => (value === 'Online' ? 'success' : value === 'Warning' ? 'warning' : 'danger');

// Custom column
function handleCustom() {
	xGrid.value?.openCustom();
}
// Get selected records
function getCheckedRecords() {
	return (xGrid.value?.getCheckboxRecords() as VwPageScreenOutput[]) ?? [];
}

function selectChangeEvent() {
	state.showBatchDelete = getCheckedRecords().length > 0;
}

// Table events
const gridEvents: VxeGridListeners<VwPageScreenOutput> = {
	async pageChange({ pageSize }) {
		state.localPageParam.pageSize = pageSize;
	},
	async sortChange({ field, order }) {
		state.localPageParam.defaultSort = { field: field, order: order!, descStr: 'desc' };
	},
};
</script>

<style scoped lang="scss">
.vw-screens-list {
	height: 100%;
	display: flex;
	flex-direction: column;
}
</style>
