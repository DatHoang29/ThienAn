<template>
	<WorkspacePage>
	<div class="sharedata-codeset-container">
		<GridSearchPanel id="sharedata-codeset-search">
			<template #filters>
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true"
				label-width="auto" style="flex: 1 1 0%" label-position="left">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.code')" prop="code">
							<el-input v-model="state.queryParams.code" 
							:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.code') })"
							clearable
								@keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.name')" prop="name">
							<el-input v-model="state.queryParams.name" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.name') })" clearable
								@keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>

			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />

			<el-row>
				<SearchButton :auth-search="'eshCanonical:page'" @search="handleQuery" @reset="resetQuery"
					:loading="options.loading" />
			</el-row>
			</template>

		<el-card class="full-table" shadow="hover">
			<vxe-grid ref="xGrid" height="100%" class="xGrid-style" v-bind="options" v-on="gridEvents"
				@checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">
				<template #toolbar_buttons>
					<GridSummary :summary="summary" />
				</template>
				<template #toolbar_tools>
					<TableHeader @add="handleAdd" :show-delete="state.showBatchDelete" :auth-add="'eshCanonical:add'"
						:auth-delete="'eshCanonical:delete'" @refresh="handleQuery" @custom-column="handleCustom"
						@delete="handleBatchDelete">
					</TableHeader>
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>

				<template #row_scope="{ row }">
					<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.ShareScope, row.scope)" />
				</template>

				<template #row_value_count="{ row }">
					<el-tag size="small" type="info">{{ row.valueCount ?? row.values?.length ?? 0 }}</el-tag>
				</template>

				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>

				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
						<el-button icon="ele-CopyDocument" text type="warning" v-auth="'eshCanonical:add'"
							@click="handleCopy(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
						<el-button icon="ele-Edit" text type="primary" v-auth="'eshCanonical:update'"
							@click="handleEdit(row)"> </el-button>
					</el-tooltip>
					<!-- Không còn chặn xóa tại FE: hai cột đếm usedBy* đã bỏ khỏi API.
						 Backend vẫn kiểm bộ mã đang được phễu lọc dùng và trả lỗi khi xóa. -->
					<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger"
							v-auth="'eshCanonical:delete'" @click="handleDelete(row)"> </el-button>
					</el-tooltip>
				</template>
			</vxe-grid>
		</el-card>
		</GridSearchPanel>

		<EditCodeSet ref="editCodeSetRef" :title="state.title" :operateType="state.operateType"
			@handleQuery="() => handleQuery(true)" />
	</div>
	</WorkspacePage>
</template>

<script lang="ts" setup name="eshCanonical">
import { nextTick, onActivated, onBeforeUnmount, onMounted, reactive, ref, defineAsyncComponent } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import WorkspacePage from '/@/components/workspace/WorkspacePage.vue';
import GridSearchPanel from '/@/components/gridPage/GridSearchPanel.vue';
import GridSummary from '/@/components/gridPage/GridSummary.vue';
import { useGridSummary } from '/@/components/gridPage/useGridSummary';
import { useGridPager } from '/@/components/gridPage/useGridPager';
import { optimisticRemoveGridRows, silentSyncGrid } from '/@/utils/sharedata/gridSync';
import { Local } from '/@/utils/storage';
import EditCodeSet from './component/editCodeSet.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import { getAPI } from '/@/utils/axios-utils';
import { ShareDataCodeSetApi } from '/@/api-services/shareData';
import { ShareDataPageCodeSetInput, ShareDataPageCodeSetOutput } from '/@/api-services/shareData';
import { OperateType } from '/@/types/constants/operate';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums/baseEnum';

const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;

const { t: $t } = useI18n();
const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));

const xGrid = ref<VxeGridInstance>();
const { summary, track } = useGridSummary();
const editCodeSetRef = ref<InstanceType<typeof EditCodeSet>>();
const state = reactive({
	queryParams: {
		name: undefined as unknown as string,
		code: undefined as unknown as string,
	},
	localPageParam: {
		pageSize: 50 as number,
		defaultSort: { field: 'createTime', order: 'desc', descStr: 'desc' } as any,
	},
	visible: false,
	title: '',
	operateType: 'add' as OperateType,
	showBatchDelete: false,
});
// Local storage parameters
const localPageParamKey = 'localPageParam:eshCanonical';
// Table parameter configuration
const options = useVxeTable<ShareDataPageCodeSetOutput>(
	{
		id: 'eshCanonical',
		name: 'CodeSet',
		columns: [
			{ type: 'checkbox', fixed: 'left', width: 45 },
			{ field: 'code', title: $t('lz.entity.base.code'), minWidth: 150, align: 'left', headerAlign: 'center', showOverflow: 'tooltip', sortable: true },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 200, align: 'left', headerAlign: 'center', showOverflow: 'tooltip', sortable: true },
			{ field: 'valueCount', title: $t('lz.entity.sharedataCodeSet.valueCount'), width: 100, slots: { default: 'row_value_count' } },
			{ field: '', title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 130, showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	{
		height: '100%',
		autoResize: true,
		// Proxy configuration
		proxyConfig: { autoLoad: false, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		// Sorting configuration
		sortConfig: { defaultSort: state.localPageParam.defaultSort },
		// Pagination configuration
		pagerConfig: { pageSize: state.localPageParam.pageSize },
		// Toolbar configuration
		toolbarConfig: { export: false },
		tooltipConfig: { popupClassName: 'code-set-tooltip' },
	}
);

const onWindowResize = () => {
	xGrid.value?.recalculate(true);
};

// Page initialization
onMounted(async () => {
	window.addEventListener('resize', onWindowResize);
	state.localPageParam = Local.get(localPageParamKey) || state.localPageParam;
	await handleQuery();
	await nextTick();
	xGrid.value?.recalculate(true);
});

onBeforeUnmount(() => {
	window.removeEventListener('resize', onWindowResize);
});

// Query API
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	// Nếu sort.field bị null/undefined, lấy giá trị defaultSort
	const sortField = sort?.field || state.localPageParam.defaultSort.field;
	const sortOrder = sort?.order || state.localPageParam.defaultSort.order;

	const params = Object.assign(state.queryParams, { page: page.currentPage, pageSize: page.pageSize, field: sortField, order: sortOrder, descStr: 'desc' }) as ShareDataPageCodeSetInput;
	return track(getAPI(ShareDataCodeSetApi).apiSharedataSharedatacodesetPagePost(params));
};

// Query operation
const handleQuery = async (silent: boolean | unknown = false) => {
	if (silent === true) {
		await silentSyncGrid({
			grid: xGrid.value,
			queryApi: handleQueryApi,
			defaultSort: state.localPageParam.defaultSort,
			selectChangeEvent,
		});
		return;
	}
	options.loading = true;
	try {
		await xGrid.value?.commitProxy('reload');
	} finally {
		options.loading = false;
	}
	selectChangeEvent();
};

// Reset operation
const resetQuery = async () => {
	state.queryParams.name = undefined as unknown as string;
	state.queryParams.code = undefined as unknown as string;
	await xGrid.value?.commitProxy('reload');
};

// Open add dialog
const handleAdd = () => {
	state.title = $t('lz.label.base.titleAdd', { name: $t('lz.label.sharedataCodeSet.addTitleEntity') });
	state.operateType = 'add';
	editCodeSetRef.value?.openDialog({ scope: 1, values: [] });
};

// Open copy dialog
const handleCopy = (row: any) => {
	state.title = $t('lz.label.base.titleCopy', { name: $t('lz.label.sharedataCodeSet.addTitleEntity') });
	state.operateType = 'copy';
	editCodeSetRef.value?.openDialog(row);
};

// Open edit dialog
const handleEdit = (row: any) => {
	state.title = $t('lz.label.base.titleEdit', { name: $t('lz.label.sharedataCodeSet.addTitleEntity') });
	state.operateType = 'edit';
	editCodeSetRef.value?.openDialog(row);
};

// Delete
const handleDelete = (row: any) => {
	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.name }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			// Cập nhật lạc quan trên bảng và đồng bộ ngầm để không nháy loading.
			const { rollback } = optimisticRemoveGridRows({
				grid: xGrid.value,
				predicate: (r: any) => (row.id ? r.id === row.id : r === row),
				summary,
				selectChangeEvent,
			});
			try {
				await getAPI(ShareDataCodeSetApi).apiSharedataSharedatacodesetDeletePost({ id: row.id });
				ElMessage.success($t('lz.message.base.deleteSuccess'));
				await handleQuery(true);
			} catch {
				await rollback();
			}
		})
		.catch(() => { });
};

//Batch delete
const handleBatchDelete = async () => {
	const selectRecords = getCheckedRecords();
	const deleteIds = selectRecords.filter((l) => l.id).map((l) => l.id);
	const deleteIdSet = new Set(deleteIds);
	ElMessageBox.confirm($t('lz.message.base.confirmBatchDelete', { total: selectRecords.length }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			const { rollback } = optimisticRemoveGridRows({
				grid: xGrid.value,
				predicate: (r: any) => (r.id ? deleteIdSet.has(r.id) : selectRecords.includes(r)),
				summary,
				selectChangeEvent,
			});
			try {
				await getAPI(ShareDataCodeSetApi).apiSharedataSharedatacodesetBatchdeletePost(deleteIds.map((id) => ({ id })));
				ElMessage.success($t('lz.message.base.deleteSuccess'));
				await handleQuery(true);
			} catch {
				await rollback();
			}
		})
		.catch(() => { });
};

// Custom column
function handleCustom() {
	xGrid.value?.openCustom();
}
// Get selected records
function getCheckedRecords() {
	return xGrid.value?.getCheckboxRecords() ?? [];
}

function selectChangeEvent() {
	state.showBatchDelete = getCheckedRecords().length > 0;
}

// Table events
const gridEvents: VxeGridListeners<ShareDataPageCodeSetOutput> = {
	// Only valid when pager-config is configured, this event will be triggered when the page changes
	async pageChange({ pageSize }) {
		state.localPageParam.pageSize = pageSize;
		Local.set(localPageParamKey, state.localPageParam);
	},
	// This event will be triggered when the sorting condition changes
	async sortChange({ field, order }) {
		state.localPageParam.defaultSort = { field: field, order: order!, descStr: 'desc' };
		Local.set(localPageParamKey, state.localPageParam);
	},
};

useGridPager(xGrid, options);

// KeepAlive also activates on initial mount; the grid proxy already loads then.
let activated = false;
onActivated(async () => {
	if (!activated) {
		activated = true;
		return;
	}
	await xGrid.value?.commitProxy('query');
	await nextTick();
	xGrid.value?.recalculate(true);
});
</script>

<style scoped lang="scss">
/* Chuẩn hóa VXE-Grid Loading & Element Loading — 8-dots spinner bự vừa vặn */
:deep(.vxe-table--loading) {
	.vxe-loading--default-icon {
		display: inline-block;
		width: 50px;
		height: 50px;
		font-size: 0;
		color: transparent;
		background-color: var(--el-color-primary, #409eff);
		mask-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 50 50'%3E%3Ccircle cx='25' cy='6' r='3.4' fill='white' opacity='1'/%3E%3Ccircle cx='38.44' cy='11.56' r='3.4' fill='white' opacity='0.875'/%3E%3Ccircle cx='44' cy='25' r='3.4' fill='white' opacity='0.75'/%3E%3Ccircle cx='38.44' cy='38.44' r='3.4' fill='white' opacity='0.625'/%3E%3Ccircle cx='25' cy='44' r='3.4' fill='white' opacity='0.5'/%3E%3Ccircle cx='11.56' cy='38.44' r='3.4' fill='white' opacity='0.375'/%3E%3Ccircle cx='6' cy='25' r='3.4' fill='white' opacity='0.25'/%3E%3Ccircle cx='11.56' cy='11.56' r='3.4' fill='white' opacity='0.125'/%3E%3C/svg%3E");
		-webkit-mask-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 50 50'%3E%3Ccircle cx='25' cy='6' r='3.4' fill='white' opacity='1'/%3E%3Ccircle cx='38.44' cy='11.56' r='3.4' fill='white' opacity='0.875'/%3E%3Ccircle cx='44' cy='25' r='3.4' fill='white' opacity='0.75'/%3E%3Ccircle cx='38.44' cy='38.44' r='3.4' fill='white' opacity='0.625'/%3E%3Ccircle cx='25' cy='44' r='3.4' fill='white' opacity='0.5'/%3E%3Ccircle cx='11.56' cy='38.44' r='3.4' fill='white' opacity='0.375'/%3E%3Ccircle cx='6' cy='25' r='3.4' fill='white' opacity='0.25'/%3E%3Ccircle cx='11.56' cy='11.56' r='3.4' fill='white' opacity='0.125'/%3E%3C/svg%3E");
		mask-repeat: no-repeat;
		-webkit-mask-repeat: no-repeat;
		mask-position: center;
		-webkit-mask-position: center;
		mask-size: contain;
		-webkit-mask-size: contain;
		animation: sharedata-roll 0.9s linear infinite;
		transform-origin: center;
		will-change: transform;

		&::before {
			display: none;
		}
	}
	.vxe-loading--text {
		font-size: 14px;
		font-weight: 500;
		color: var(--el-color-primary, #409eff);
		padding-top: 10px;
	}
}

:deep(.el-loading-mask) {
	background-color: rgba(255, 255, 255, 0.78);
	backdrop-filter: blur(1px);
	border-radius: inherit;
	transition: opacity 0.25s ease;
	z-index: 100;
}

:deep(.el-loading-spinner) {
	top: 50%;
	transform: translateY(-50%);
	margin-top: 0;
	width: 100%;
	text-align: center;
	position: absolute;

	.circular {
		display: inline-block;
		width: 50px;
		height: 50px;
		background-color: var(--el-color-primary, #409eff);
		mask-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 50 50'%3E%3Ccircle cx='25' cy='6' r='3.4' fill='white' opacity='1'/%3E%3Ccircle cx='38.44' cy='11.56' r='3.4' fill='white' opacity='0.875'/%3E%3Ccircle cx='44' cy='25' r='3.4' fill='white' opacity='0.75'/%3E%3Ccircle cx='38.44' cy='38.44' r='3.4' fill='white' opacity='0.625'/%3E%3Ccircle cx='25' cy='44' r='3.4' fill='white' opacity='0.5'/%3E%3Ccircle cx='11.56' cy='38.44' r='3.4' fill='white' opacity='0.375'/%3E%3Ccircle cx='6' cy='25' r='3.4' fill='white' opacity='0.25'/%3E%3Ccircle cx='11.56' cy='11.56' r='3.4' fill='white' opacity='0.125'/%3E%3C/svg%3E");
		-webkit-mask-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 50 50'%3E%3Ccircle cx='25' cy='6' r='3.4' fill='white' opacity='1'/%3E%3Ccircle cx='38.44' cy='11.56' r='3.4' fill='white' opacity='0.875'/%3E%3Ccircle cx='44' cy='25' r='3.4' fill='white' opacity='0.75'/%3E%3Ccircle cx='38.44' cy='38.44' r='3.4' fill='white' opacity='0.625'/%3E%3Ccircle cx='25' cy='44' r='3.4' fill='white' opacity='0.5'/%3E%3Ccircle cx='11.56' cy='38.44' r='3.4' fill='white' opacity='0.375'/%3E%3Ccircle cx='6' cy='25' r='3.4' fill='white' opacity='0.25'/%3E%3Ccircle cx='11.56' cy='11.56' r='3.4' fill='white' opacity='0.125'/%3E%3C/svg%3E");
		mask-repeat: no-repeat;
		-webkit-mask-repeat: no-repeat;
		mask-position: center;
		-webkit-mask-position: center;
		mask-size: contain;
		-webkit-mask-size: contain;
		animation: sharedata-roll 0.9s linear infinite;
		transform-origin: center;
		will-change: transform;

		.path {
			display: none;
		}
	}

	.el-loading-text {
		color: var(--el-color-primary, #409eff);
		font-size: 14px;
		font-weight: 500;
		letter-spacing: 0.3px;
		margin: 10px 0 0;
		line-height: 1.4;
	}

	&:not(:has(.el-loading-text))::after {
		content: 'Đang tải...';
		display: block;
		color: var(--el-color-primary, #409eff);
		font-size: 14px;
		font-weight: 500;
		letter-spacing: 0.3px;
		margin-top: 10px;
		line-height: 1.4;
	}
}

@keyframes sharedata-roll {
	from {
		transform: rotate(0deg);
	}
	to {
		transform: rotate(360deg);
	}
}

.sharedata-codeset-container {
	display: flex;
	flex-direction: column;
	height: 100%;
}

:deep(.full-table) {
	flex: 1 1 0%;
	min-height: 0;
	display: flex;
	flex-direction: column;

	.el-card__body {
		height: 100%;
		flex: 1 1 0%;
		display: flex;
		flex-direction: column;
		min-height: 0;
		padding: 5px 5px 8px 5px;

		.xGrid-style {
			flex: 1 1 0%;
			min-height: 0;
			height: 100%;
			width: 100%;
			display: flex;
			flex-direction: column;
			overflow: hidden;

			.vxe-grid--layout-body-wrapper,
			.vxe-grid--layout-body-content-wrapper {
				flex: 1 1 0%;
				min-height: 0;
				height: 100%;
				display: flex;
				flex-direction: column;
				overflow: hidden;
			}

			.vxe-grid--toolbar-wrapper {
				flex-shrink: 0;
			}

			.vxe-grid--table-container {
				flex: 1 1 0%;
				min-height: 0;
				height: 100%;
				display: flex;
				flex-direction: column;
				overflow: hidden;

				.vxe-grid--table-wrapper {
					flex: 1 1 0%;
					min-height: 0;
					height: 100%;
					display: flex;
					flex-direction: column;
					overflow: hidden;
				}
			}

			.vxe-table {
				flex: 1 1 0%;
				min-height: 0;
				height: 100%;
				display: flex;
				flex-direction: column;

				.vxe-table--render-wrapper,
				.vxe-table--main-wrapper {
					flex: 1 1 0% !important;
					min-height: 0 !important;
					height: 100% !important;
					display: flex !important;
					flex-direction: column !important;
				}

				.vxe-table--body-wrapper.body--wrapper {
					flex: 1 1 0% !important;
					min-height: 0 !important;
					height: 100% !important;
					overflow-y: auto;
					overflow-x: auto;
				}

				&.is--empty .vxe-table--empty-placeholder {
					height: 100% !important;
					min-height: 200px;
					display: flex;
					align-items: center;
					justify-content: center;
				}
			}

			.vxe-grid--pager-wrapper {
				flex-shrink: 0;
				margin-top: auto;
			}

			.vxe-pager {
				background: transparent;
			}
		}
	}
}
</style>

<style lang="scss">
/* Tooltips render in <body>: styled here, not in the scoped block */
.vxe-tooltip--wrapper.code-set-tooltip .vxe-tooltip--content {
	box-sizing: border-box;
	max-width: 28vw;
	overflow-wrap: anywhere;
}
</style>
