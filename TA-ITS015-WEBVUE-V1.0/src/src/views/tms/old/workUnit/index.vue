<template>
	<div class="workunit-container">
		<el-card shadow="hover"
			:body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" label-position="left" :inlineMessage="true"	label-width="auto" style="flex: 1 1 0%">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.code')" prop="code">
							<el-input v-model="state.queryParams.code" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.base.code')})" @keyup.enter.native="handleQuery" :maxlength="EntityFieldLength.Length64" show-word-limit clearable class="w100"/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.name')" prop="name">
							<el-input v-model="state.queryParams.name" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.base.name')})" @keyup.enter.native="handleQuery" :maxlength="EntityFieldLength.Length64" show-word-limit clearable class="w100"/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.type')" prop="type">
							<el-select v-model="state.queryParams.type" :placeholder="$t('lz.label.base.pleaseSelect', {input: $t('lz.entity.base.type')})" clearable class="w100">
								<el-option v-for="item in state.unitTypeList" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.phone')" prop="phone">
							<el-input v-model="state.queryParams.phone" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.base.phone')})" @keyup.enter.native="handleQuery" :maxlength="EntityFieldLength.Length32" show-word-limit clearable class="w100"/>
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
				<SearchButton authSearch="tmsWorkUnit:list" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
			</el-row>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGridWorkUnit" class="xGrid-style" v-bind="options" v-on="gridEvents" @checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">
				<template #toolbar_tools>
					<TableHeader @add="handleAdd" :show-delete="state.showBatchDelete" :auth-add="'tmsWorkUnit:add'" :auth-delete="'tmsWorkUnit:delete'" @refresh="handleQuery" @custom-column="handleCustom" @delete="handleBatchDelete">
					</TableHeader>
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_type="{ row }">
					{{ state.unitTypeList?.find((item:any) => item.code == row.type)?.name }}
				</template>
				<template #row_status="{ row }">
					<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, row.status)" />
				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
						<el-button icon="ele-CopyDocument" text type="warning" v-auth="'tmsWorkUnit:add'" @click="handleCopy(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
						<el-button icon="ele-Edit" text type="primary" v-auth="'tmsWorkUnit:update'" @click="handleEdit(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger" v-auth="'tmsWorkUnit:delete'" @click="handleDelete(row)"> </el-button>
					</el-tooltip>
				</template>
				<template #header_name>
					{{ $t('lz.label.tms.tmsWorkUnit') }}
				</template>
			</vxe-grid>
		</el-card>

		<editWorkUnitComp ref="editWorkUnitCompRef" :title="state.title" :operateType="state.operateType" @handleQuery="handleQuery" />
		
	</div>
</template>

<script lang="ts" setup name="tmsWorkUnit">
	import { onMounted, reactive, ref, defineAsyncComponent } from 'vue';
	import { ElMessageBox, ElMessage } from 'element-plus';
	import { useI18n } from 'vue-i18n';
	import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
	import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
	import { Local } from '/@/utils/storage';
	import { getAPI } from '/@/utils/axios-utils';
	import { OperateType } from '/@/types/constants/operate';
	import ModifyRecord from '/@/components/table/modifyRecord.vue';

	import { TmsWorkUnitApi } from '/@/api-services/tms';
	import { PageTmsWorkUnitInput, PageTmsWorkUnitOutput } from '/@/api-services/tms';

	import editWorkUnitComp from '/@/views/tms/workUnit/component/editWorkUnit.vue';

	import { useBaseConfig } from '/@/stores/baseConfig';
	import { BaseConfigTypeEnum } from '/@/types/enums';

	const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
	const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
	const getConfigItemByCode = useBaseConfig().getConfigItemByCode;

	const editWorkUnitCompRef = ref<InstanceType<typeof editWorkUnitComp>>();
	const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
	const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
	const xGridWorkUnit = ref<VxeGridInstance>();
	const { t: $t } = useI18n();
	
	import { EntityFieldLength } from '/@/types/enums/baseEnum';
	
	const state = reactive({
		queryParams: {
			code: undefined,
			name: undefined,
			type: undefined,
			phone: undefined,
			status: undefined
		},
		localPageParam: {
			pageSize: 50 as number,
			defaultSort: {} as any,
		},
		visible: false,
		title: '',
		operateType: 'add' as OperateType,
		showBatchDelete: false,
		unitTypeList: [] as any,
		statusList: [] as any
	});

	const options = useVxeTable<PageTmsWorkUnitOutput>(
		{
			id: 'tmsWorkUnitGrid',
			name: 'TmsWorkUnit',
			columns: [
				// Fields
				{ type: 'checkbox', fixed: 'left', width: 45 },
				{ field: 'code', title: $t('lz.entity.base.code'), minWidth: 100, showOverflow: 'tooltip', sortable: true },

				{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 120, showOverflow: 'tooltip', sortable: true },
				{ field: 'type', title: $t('lz.entity.base.type'), minWidth: 100, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_type' } },
				{ field: 'phone', title: $t('lz.entity.base.phone'), minWidth: 100, showOverflow: 'tooltip', sortable: true },
				{ field: 'address', title: $t('lz.entity.base.address'), minWidth: 200, showOverflow: 'tooltip', sortable: true },
				{ field: 'info', title: $t('lz.entity.base.info'), minWidth: 200, showOverflow: 'tooltip', sortable: true },
				{ field: 'status', title: $t('lz.entity.base.status'), minWidth: 100, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_status' }},
				// Actions
				{ title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
				{ title: $t('lz.label.base.action'), fixed: 'right', width: 100, showOverflow: true, slots: { default: 'row_buttons' } },
			],
		},
		{
			// Proxy configuration
			proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
			toolbarConfig: { export: false },
		}
	);

	onMounted(async () => {
		state.unitTypeList = getConfigDataByCode(BaseConfigTypeEnum.TmsWorkUnitType);
		state.statusList = getConfigDataByCode(BaseConfigTypeEnum.SysStatusType);

		state.localPageParam = Local.get(localPageParamKey) || state.localPageParam;
		await handleQuery();
	});

	// Query API
	const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
		const params = Object.assign(
			state.queryParams, 
			{
				page: page.currentPage,
				pageSize: page.pageSize ?? state.localPageParam.pageSize,
				field: sort?.order ? sort?.field : undefined,
				order: sort?.order ? sort?.order : undefined,
				descStr: 'desc', // để OrderBuilder so 'desc' == 'desc' => DESC - ChuongLP - 31-07-2026
			}
		) as PageTmsWorkUnitInput;
		
		return await getAPI(TmsWorkUnitApi).apiTmsTmsworkunitPagePost(params);
	};

	// Query operation
	const handleQuery = async () => {
		options.loading = true;
		await xGridWorkUnit.value?.commitProxy('reload') ;

		options.loading = false;
		selectChangeEvent();
	};

	// Reset operation
	const resetQuery = async () => {
		state.queryParams.code = undefined;
		state.queryParams.name = undefined;
		state.queryParams.type = undefined;
		state.queryParams.phone = undefined;
		state.queryParams.status = undefined;
		await xGridWorkUnit.value?.commitProxy('reload');
	};

	// Open add dialog
	const handleAdd = () => {
		state.title = $t('lz.label.base.titleAdd', {name: $t('lz.label.tms.tmsWorkUnit')});
		state.operateType = 'add';
		editWorkUnitCompRef.value?.openDialog({ status: 1, orderNo: 100 });
	};

	// Open copy dialog
	const handleCopy = (row: any) => {
		state.title = $t('lz.label.base.titleCopy', {name: $t('lz.label.tms.tmsWorkUnit')});
		state.operateType = 'copy';

		editWorkUnitCompRef.value?.openDialog(row);
	};

	// Open edit dialog
	const handleEdit = (row: any) => {
		state.title = $t('lz.label.base.titleEdit', {name: $t('lz.label.tms.tmsWorkUnit')});
		state.operateType = 'edit';

		editWorkUnitCompRef.value?.openDialog(row);
	};

	// Delete
	const handleDelete = (row: any) => {
		ElMessageBox.confirm($t('lz.message.base.confirmDelete', {name:row.name}), $t('lz.label.base.confirm'), {
			confirmButtonText: $t('lz.button.base.confirm'),
			cancelButtonText: $t('lz.button.base.cancel'),
			type: 'warning',
		})
		.then(async () => {
			try{
				await getAPI(TmsWorkUnitApi).apiTmsTmsworkunitDeletePost({ id: row.id });
				handleQuery();
				ElMessage.success($t('lz.message.base.processSuccess'));
			}
			catch(error){
				ElMessage.success($t('lz.message.base.processUnSuccess'));
			}
		})
		.catch(() => { });
	};

	//Batch delete
	const handleBatchDelete = async () => {
		const selectRecords = getCheckedRecords();

		ElMessageBox.confirm($t('lz.message.base.confirmBatchDelete', { total: selectRecords.length }), $t('lz.label.base.confirm'), {
			confirmButtonText: $t('lz.button.base.confirm'),
			cancelButtonText: $t('lz.button.base.cancel'),
			type: 'warning',
		})
		.then(async () => {
			try{
				await getAPI(TmsWorkUnitApi).apiTmsTmsworkunitBatchdeletePost(selectRecords.filter(l => l.id));
				handleQuery();
				ElMessage.success($t('lz.message.base.processSuccess'));
			}
			catch(error){
				ElMessage.success($t('lz.message.base.processUnSuccess'));
			}
		})
		.catch(() => { });
	}

	// Local storage parameters
	const localPageParamKey = 'localPageParam:tmsWorkUnit';

	// Custom column
	function handleCustom() {
		xGridWorkUnit.value?.openCustom();
	}

	// Get selected records
	function getCheckedRecords() {
		return xGridWorkUnit.value?.getCheckboxRecords() ?? [];
	}

	function selectChangeEvent() {
		state.showBatchDelete = getCheckedRecords().length > 0;
	}

	// Table events
	const gridEvents: VxeGridListeners<PageTmsWorkUnitOutput> = {
		// Only valid when pager-config is configured, this event will be triggered when the page changes
		async pageChange({ pageSize }) {
			state.localPageParam.pageSize = pageSize;
			Local.set(localPageParamKey, state.localPageParam);
		},
		// This event will be triggered when the sorting condition changes
		async sortChange({ field, order }) {
			state.localPageParam.defaultSort = { field: field, order: order! };
			Local.set(localPageParamKey, state.localPageParam);
		},
	};
</script>
