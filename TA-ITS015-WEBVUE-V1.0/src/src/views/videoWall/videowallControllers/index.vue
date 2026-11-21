<template>
	<WorkspacePage>
	<div class="videowall-controller-container">
		<GridSearchPanel id="videowall-controller-search">
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
						<el-form-item :label="$t('lz.entity.videoWall.model')" prop="model">
							<el-select v-model="state.queryParams.model" :placeholder="$t('lz.label.videoWall.selectModel')" clearable class="w100">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.VideoWallControllerModel)" :key="item.code" :label="item.name" :value="item.code" />
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
				<SearchButton :auth-search="'videoWallController:page'" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
			</el-row>
			</template>

		<el-card class="full-table" shadow="hover">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents"
				@checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">
				<template #toolbar_buttons>
					<GridSummary :summary="summary" />
				</template>
				<template #toolbar_tools>
					<TableHeader @add="handleAdd" :show-delete="state.showBatchDelete" :auth-add="'videoWallController:add'"
						:auth-delete="'videoWallController:delete'" @refresh="handleQuery" @custom-column="handleCustom"
						@delete="handleBatchDelete">
					</TableHeader>
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_model="{ row }">
					<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.VideoWallControllerModel, row.model)" />
				</template>
				<template #row_color="{ row }">
					<span class="vw-color-cell"><i :style="{ background: vwColorVar(row.colorHex) }" />{{ vwColorVar(row.colorHex, '').toUpperCase() }}</span>
				</template>
				<template #row_status="{ row }">
					<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, row.status)" />
				</template>
				<template #row_role="{ row }">
					<el-tag :type="roleOf(row).type" effect="plain">{{ roleOf(row).label }}</el-tag>
				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #row_buttons="{ row }">
					<!-- Bộ phân phối: khai báo cổng ra cấp tín hiệu cho bộ hiển thị nào -->
					<el-tooltip v-if="row.role === VW_ROLE.Distribution" :content="$t('lz.label.videoWall.feedsButton')" placement="top" :auto-close="1000">
						<el-button icon="ele-Connection" text type="success" v-auth="'videoWallOutput:update'" @click="feedsRef?.openDialog(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
						<el-button icon="ele-CopyDocument" text type="warning" v-auth="'videoWallController:add'" @click="handleCopy(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
						<el-button icon="ele-Edit" text type="primary" v-auth="'videoWallController:update'" @click="handleEdit(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger" v-auth="'videoWallController:delete'" @click="handleDelete(row)"> </el-button>
					</el-tooltip>
				</template>
			</vxe-grid>
		</el-card>
		</GridSearchPanel>

		<EditController ref="editControllerRef" :title="state.title" :operateType="state.operateType" @handleQuery="handleQuery" />
		<FeedsDialog ref="feedsRef" />
	</div>
	</WorkspacePage>
</template>

<script lang="ts" setup name="videoWallController">
import { onActivated, onMounted, reactive, ref, defineAsyncComponent } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { Local } from '/@/utils/storage';
import EditController from './component/editController.vue';
import FeedsDialog from './component/feedsDialog.vue';
import { VW_ROLE } from '/@/stores/its/videoWallController';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import { getAPI } from '/@/utils/axios-utils';
import { VwControllerApi } from '/@/api-services/videoWall';
import { VwPageControllerOutput } from '/@/api-services/videoWall';
import { OperateType } from '/@/types/constants/operate';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import { useVideoWallController, vwColorVar, VW_DEFAULT_CTRL_COLOR } from '/@/stores/its/videoWallController';
import WorkspacePage from '/@/components/workspace/WorkspacePage.vue';
import GridSearchPanel from '/@/components/gridPage/GridSearchPanel.vue';
import GridSummary from '/@/components/gridPage/GridSummary.vue';
import { useGridSummary } from '/@/components/gridPage/useGridSummary';


const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;

const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
const { t: $t } = useI18n();
const vwCtrl = useVideoWallController();
const xGrid = ref<VxeGridInstance>();
// Bảng cây không phân trang (API list) ⇒ tự ghi tổng số / giờ tải thay cho track()
const { summary } = useGridSummary();
const editControllerRef = ref<InstanceType<typeof EditController>>();
const feedsRef = ref<InstanceType<typeof FeedsDialog>>();
const state = reactive({
	queryParams: {
		name: undefined as unknown as string,
		model: undefined as unknown as string,
		status: undefined as unknown as any,
	},
	localPageParam: {
		pageSize: 50 as number,
		defaultSort: { field: 'code', order: 'asc', descStr: 'desc' } as any,
	},
	title: '',
	operateType: 'add' as OperateType,
	/** Danh sách đang hiển thị (dùng suy ra vai trò trung tâm / con). */
	rows: [] as Array<{ id?: string | null; parentControllerId?: string | null }>,
	showBatchDelete: false,
});
const localPageParamKey = 'localPageParam:videoWallController';
const options = useVxeTable<VwPageControllerOutput>(
	{
		id: 'videoWallController',
		name: 'VideoWallController',
		columns: [
			{ type: 'checkbox', fixed: 'left', width: 45 },
			// Cây cha–con: bộ trung tâm ở gốc, các bộ con lồng bên dưới (theo ParentControllerId)
			{ field: 'code', title: $t('lz.entity.base.code'), minWidth: 160, showOverflow: 'tooltip', treeNode: true, align: 'left', headerAlign: 'center' },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 160, showOverflow: 'tooltip' },
			{ field: 'parentControllerId', title: $t('lz.entity.videoWall.role'), width: 120, slots: { default: 'row_role' } },
			{ field: 'model', title: $t('lz.entity.videoWall.model'), minWidth: 170, showOverflow: 'tooltip', sortable: false, slots: { default: 'row_model' } },
			{ field: 'ip', title: $t('lz.entity.base.ipAddress'), width: 140, showOverflow: 'tooltip', sortable: true },
			{ field: 'port', title: $t('lz.entity.base.port'), width: 90, showOverflow: 'tooltip', sortable: false },
			// { field: 'account', title: $t('lz.entity.base.account'), width: 120, showOverflow: 'tooltip' },
			// { field: 'chassis', title: 'Chassis', width: 90, showOverflow: 'tooltip' },
			// { field: 'originCol', title: $t('lz.entity.videoWall.coverage'), width: 150, showOverflow: 'tooltip', formatter: ({ row }: any) => coverageLabel(row) },
			{ field: 'colorHex', title: $t('lz.entity.videoWall.color'), width: 110, slots: { default: 'row_color' } },
			{ field: 'status', title: $t('lz.entity.base.status'), width: 110, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_status' } },
			{ field: '', title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 160, showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	{
		// Khung đổi kích thước (thu / mở menu, đổi tab) ⇒ tính lại độ rộng cột, cột Hành động luôn sát mép phải
		autoResize: true,
		// Hiển thị dạng cây ⇒ nạp toàn bộ (không phân trang) rồi vxe-table tự dựng cây từ danh sách phẳng
		proxyConfig: { autoLoad: true, ajax: { query: ({ sort }) => handleQueryApi(sort) } },
		pagerConfig: { enabled: false },
		rowConfig: { keyField: 'id', height: 66 },
		treeConfig: { transform: true, rowField: 'id', parentField: 'parentControllerId', expandAll: true, reserve: true },
		// Bảng cây: stripe của vxe-table đánh dấu theo chỉ số trong từng cấp nên tô sai (gần như cả bảng cùng màu)
		// ⇒ tắt và tự tô xen kẽ theo thứ tự dòng đang hiển thị, giống các bảng khác.
		stripe: false,
		rowClassName: ({ row }: { row: VwPageControllerOutput }) => {
			const visible = xGrid.value?.getTableData().visibleData ?? [];
			return visible.indexOf(row) % 2 === 1 ? 'vw-row-stripe' : '';
		},
		// Có checkbox chọn tất cả; chọn bộ trung tâm thì tự chọn kèm các bộ con (giống trang Menu)
		checkboxConfig: { range: false },
		toolbarConfig: { export: false },
	}
);

// Page initialization
onMounted(() => {
	state.localPageParam = Local.get(localPageParamKey) || state.localPageParam;
});

// Quay lại tab / menu (KeepAlive): tải lại bảng để nhận thay đổi từ tab khác.
// onActivated cũng chạy ngay sau lần mount đầu, khi đó dữ liệu vừa tải nên bỏ qua.
let activated = false;
onActivated(async () => {
	if (!activated) {
		activated = true;
		return;
	}
	await xGrid.value?.commitProxy('query');
});

// Query API — lấy toàn bộ (API list); có chọn cột sắp xếp thì BE sắp (Field / Order), không thì sắp theo mã;
// vxe-table dựng cây theo parentControllerId, giữ thứ tự này trong từng cấp
const handleQueryApi = async (sort?: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const q = state.queryParams;
	// Field / Order chưa có trong client sinh tự động ⇒ gửi kèm qua options.params
	const res = await getAPI(VwControllerApi).apiVideowallVwcontrollerListGet(undefined, q.name || undefined, q.model || undefined, undefined, undefined, q.status ?? undefined, {
		params: sort?.order ? { Field: sort.field, Order: sort.order } : {},
	});
	const list = sort?.order ? (res.data.result ?? []) : (res.data.result ?? []).slice().sort((a, b) => (a.code ?? '').localeCompare(b.code ?? ''));
	state.rows = list;
	summary.total = list.length;
	summary.lastLoaded = new Date();
	return { ...res, data: { ...res.data, result: list } };
};

// Vai trò (VwController.role): tên lấy từ cấu hình videowall_controller_role
const ROLE_TAG: Record<string, 'primary' | 'success' | 'info'> = { distribution: 'primary', display: 'success', standalone: 'info' };
const roleOf = (row: VwPageControllerOutput): { label: string; type: 'primary' | 'success' | 'info' } => {
	const item = row.role ? (getConfigItemByCode(BaseConfigTypeEnum.VideoWallControllerRole, row.role) as { name?: string } | undefined) : undefined;
	return { label: item?.name || row.role || '—', type: ROLE_TAG[row.role ?? ''] ?? 'info' };
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
	state.queryParams.model = undefined as unknown as string;
	state.queryParams.status = undefined as unknown as any;
	await xGrid.value?.commitProxy('reload');
};

// Open add dialog — mặc định theo model duy nhất đang dùng (DS-C66S-S12, 4U, 6 khe vào/ra).
const handleAdd = () => {
	state.title = $t('lz.label.base.titleAdd', { name: $t('lz.entity.videoWall.controller') });
	state.operateType = 'add';
	editControllerRef.value?.openDialog({
		status: 1,
		model: 'DS-C66S-S12',
		chassis: '4U',
		colorHex: VW_DEFAULT_CTRL_COLOR,
		inputSlotsNumber: 6,
		slotsNumber: 6,
	});
};

// Open copy dialog
const handleCopy = (row: any) => {
	state.title = $t('lz.label.base.titleCopy', { name: $t('lz.entity.videoWall.controller') });
	state.operateType = 'copy';
	editControllerRef.value?.openDialog(row);
};

// Open edit dialog
const handleEdit = (row: any) => {
	state.title = $t('lz.label.base.titleEdit', { name: $t('lz.entity.videoWall.controller') });
	state.operateType = 'edit';
	editControllerRef.value?.openDialog(row);
};

// Delete
const handleDelete = (row: any) => {
	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.name }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(VwControllerApi).apiVideowallVwcontrollerDeletePost({ id: row.id });
			handleQuery();
			vwCtrl.load(true).catch(() => {});
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
			await getAPI(VwControllerApi).apiVideowallVwcontrollerBatchdeletePost(selectRecords.filter((l) => l.id).map((l) => ({ id: l.id! })));
			handleQuery();
			vwCtrl.load(true).catch(() => {});
			ElMessage.success($t('lz.message.base.deleteSuccess'));
		})
		.catch(() => {});
};

// // Vùng phủ trên lưới tường, hiển thị 1-based cho người dùng.
// const coverageLabel = (row: VwPageControllerOutput) => {
// 	if (row.originCol == null || row.coverCols == null) return '';
// 	const colFrom = row.originCol + 1;
// 	const colTo = row.originCol + row.coverCols;
// 	const rowFrom = (row.originRow ?? 0) + 1;
// 	const rowTo = (row.originRow ?? 0) + (row.coverRows ?? 0);
// 	return $t('lz.label.videoWall.coverageRange', { colFrom, colTo, rowFrom, rowTo });
// };

// Custom column
function handleCustom() {
	xGrid.value?.openCustom();
}
// Get selected records
function getCheckedRecords() {
	return (xGrid.value?.getCheckboxRecords() as VwPageControllerOutput[]) ?? [];
}

function selectChangeEvent() {
	state.showBatchDelete = getCheckedRecords().length > 0;
}

// Table events
const gridEvents: VxeGridListeners<VwPageControllerOutput> = {
	async pageChange({ pageSize }) {
		state.localPageParam.pageSize = pageSize;
	},
	async sortChange({ field, order }) {
		state.localPageParam.defaultSort = { field: field, order: order!, descStr: 'desc' };
	},
};
</script>

<style scoped>
.vw-color-cell {
	display: inline-flex;
	align-items: center;
	gap: 6px;
	font-size: 12px;
	font-variant-numeric: tabular-nums;
	i {
		width: 16px;
		height: 16px;
		border-radius: 4px;
		box-shadow: inset 0 0 0 1px rgba(0, 0, 0, 0.15);
	}
}
/* Màu xen kẽ tự tô cho bảng cây (xem rowClassName) — cùng biến màu với stripe mặc định của vxe-table */
.xGrid-style :deep(.vxe-body--row.vw-row-stripe) {
	background-color: var(--vxe-ui-table-row-striped-background-color);
}
</style>
