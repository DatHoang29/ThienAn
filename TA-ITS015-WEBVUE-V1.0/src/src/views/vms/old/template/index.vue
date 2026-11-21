<template>
	<div class="vms-template-container">
		<el-card shadow="hover"
			:body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true"
				label-width="auto" style="flex: 1 1 0%" label-position="left">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.code')" prop="code">
							<el-input v-model="state.queryParams.code" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.code')})" clearable
								@keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.name')" prop="name">
							<el-input v-model="state.queryParams.name" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.name')})" clearable
								@keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.lang')" prop="lang">
							<el-select v-model="state.queryParams.lang" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.base.lang')})" clearable>
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysLanguageType)" :key="item.code" :label="item.name"
									:value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.status')" prop="status" >
							<el-select v-model="state.queryParams.status" filterable clearable class="w100" @clear="state.queryParams.status = undefined" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.base.status')})" >
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code" :label="item.name" :value="parseInt(item.code)" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>

			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />

			<el-row>
				<SearchButton :auth-search="'vmsTemplate:page'" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
			</el-row>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents"
				@checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">

				<template #toolbar_tools>
					<TableHeader @add="handleAdd" :show-delete="state.showBatchDelete" :auth-add="'vmsTemplate:add'"
						:auth-delete="'vmsTemplate:delete'" @refresh="handleQuery" @custom-column="handleCustom"
						@delete="handleBatchDelete">
					</TableHeader>
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_status="{ row }">
					<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, row.status)" />
				</template>
				<template #row_image="{ row }">

					<div class="panel-img">
						<el-image :src="state.baseUrl + '/' + row.preview" alt="Cannot Preview" lazy hide-on-click-modal
							:preview-src-list="[state.baseUrl + '/' + row.preview]" :initial-index="0" fit="scale-down"
							preview-teleported></el-image>
					</div>
				</template>
				<template #row_size="{ row }">
					{{ getConfigItemByCode(BaseConfigTypeEnum.VmsImageSize, row.size)?.name }}
				</template>
				<template #row_lang="{ row }">
					{{ getConfigItemByCode(BaseConfigTypeEnum.SysLanguageType, row.lang)?.name }}


				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />

				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
						<el-button icon="ele-CopyDocument" text type="warning" v-auth="'vmsTemplate:add'"
							@click="handleCopy(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
						<el-button icon="ele-Edit" text type="primary" v-auth="'vmsTemplate:update'"
							@click="handleEdit(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger" v-auth="'vmsTemplate:delete'"
							@click="handleDelete(row)"> </el-button>
					</el-tooltip>

				</template>
				<template #header_name>
				</template>
			</vxe-grid>
		</el-card>

		<EditTemplate ref="editTemplateRef" :title="state.title" :operateType="state.operateType"
			@handleQuery="handleQuery" />
	</div>
</template>

<script lang="ts" setup name="vmsTemplate">
import { onMounted, reactive, ref, defineAsyncComponent } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import EditTemplate from './component/editTemplate.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import { getAPI } from '/@/utils/axios-utils';
import { VmsTemplateApi } from '/@/api-services/vms';
import { PageTemplateInput, PageTemplateOutput } from '/@/api-services/vms';
import { OperateType } from '/@/types/constants/operate';
import { useItsMediaServerStore } from '/@/stores/its/itsMediaServer';
import { BaseConfigTypeEnum } from '/@/types/enums/baseEnum';
import { useBaseConfig } from '/@/stores/baseConfig';
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));

const { t: $t } = useI18n();
const xGrid = ref<VxeGridInstance>();
const editTemplateRef = ref<InstanceType<typeof EditTemplate>>();
const state = reactive({
	queryParams: {
		code: undefined,
		name: undefined,	
		lang: undefined,
		status: undefined,

	},
	localPageParam: {
		pageSize: 50 as number,
		defaultSort: { field: 'orderNo', order: 'asc', descStr: 'desc' } as any,
	},
	visible: false,
	title: '',
	operateType: 'add' as OperateType,
	showBatchDelete: false,
	baseUrl: '' as any,

});
// Table parameter configuration
const options = useVxeTable<PageTemplateOutput>(
	{
		id: 'vmsTemplateGrid',
		name: 'VmsTemplate',
		columns: [
			{ type: 'checkbox', fixed: 'left', width: 45 },

			// Fields
			{ field: 'code', title: $t('lz.entity.base.code'), minWidth: 150, showOverflow: 'tooltip', sortable: true, },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 200, showOverflow: 'tooltip', sortable: true, },
			{ field: 'size', title: $t('lz.entity.vmsTemplate.size'), minWidth: 100, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_size' } },
			{ field: 'preview', title: $t('lz.entity.vms.previewImage'), minWidth: 300, showOverflow: 'tooltip', sortable: false, slots: { default: 'row_image' } },
			{ field: 'lang', title: $t('lz.entity.base.lang'), minWidth: 50, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_lang' } },
			{ field: 'status', title: $t('lz.entity.base.status'), width: 110, showOverflow: 'tooltip',sortable: true, slots: { default: 'row_status' } },
			// Action fields
			{ field: '', title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 130, showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	// vxeGrid configuration parameters (any parameter can be overridden here), refer to the official vxe-table documentation
	{
		// Proxy configuration
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		//Sorting configuration
		sortConfig: { defaultSort: state.localPageParam.defaultSort },
		// Pagination configuration
		pagerConfig: { pageSize: state.localPageParam.pageSize },
		//Toolbar configuration
		toolbarConfig: { export: false },
		rowConfig: {
			height: 120
		}
	}
);

// Page initialization
onMounted(async () => {
	state.localPageParam = state.localPageParam;
	// Địa chỉ file server dùng chung (chỉ gọi API 1 lần cho toàn app)
	state.baseUrl = await useItsMediaServerStore().ensureLoaded();

	await handleQuery();

});

// Query API
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params = Object.assign(state.queryParams, { page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order, descStr: 'desc' }) as PageTemplateInput;
	return await getAPI(VmsTemplateApi).apiVmsVmstemplatePagePost(params);
};

// Query operation
const handleQuery = async () => {
	options.loading = true;
	await xGrid.value?.commitProxy('reload') ;
	options.loading = false;
	selectChangeEvent();
};

// Reset operation
const resetQuery = async () => {
	state.queryParams.name = undefined;
	state.queryParams.lang = undefined;
	state.queryParams.code = undefined;
	state.queryParams.status = undefined;
	await xGrid.value?.commitProxy('reload');
};

// Open add dialog
const handleAdd = () => {
	state.title = $t('lz.label.base.titleAdd',{name: $t('lz.label.vms.template')});
	state.operateType = 'add';
	editTemplateRef.value?.openDialog({ status: 1});

};

// Open copy dialog
const handleCopy = (row: any) => {
	state.title = $t('lz.label.base.titleCopy',{name: $t('lz.label.vms.template')});
	state.operateType = 'copy';
	editTemplateRef.value?.openDialog(row);
};

// Open edit dialog
const handleEdit = (row: any) => {
	state.title = $t('lz.label.base.titleEdit',{name: $t('lz.label.vms.template')});
	state.operateType = 'edit';
	editTemplateRef.value?.openDialog(row);
};


// Delete
const handleDelete = (row: any) => {
	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.name }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(VmsTemplateApi).apiVmsVmstemplateDeletePost({ id: row.id });
			await handleQuery();
			ElMessage.success($t('lz.message.base.deleteSuccess'));
		})
		.catch(() => { });
};

//Batch delete
const handleBatchDelete = async () => {
	const selectRecords = getCheckedRecords();
	ElMessageBox.confirm($t('lz.message.base.confirmBatchDelete', { total: selectRecords.length }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(VmsTemplateApi).apiVmsVmstemplateBatchdeletePost(selectRecords.filter(l => l.id));
			await handleQuery();
			ElMessage.success($t('lz.message.base.deleteSuccess'));

		})
		.catch(() => { });
}

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
const gridEvents: VxeGridListeners<PageTemplateOutput> = {
	// Only valid when pager-config is configured, this event will be triggered when the page changes
	async pageChange({ pageSize }) {
		state.localPageParam.pageSize = pageSize;
	},
	// This event will be triggered when the sorting condition changes
	async sortChange({ field, order }) {
		state.localPageParam.defaultSort = { field: field, order: order!, descStr: 'desc' };
	},
};
</script>
<style scoped>
.panel-img .el-image {
	width: 90%;
	height: 100px;
	padding: 10px;
}
</style>