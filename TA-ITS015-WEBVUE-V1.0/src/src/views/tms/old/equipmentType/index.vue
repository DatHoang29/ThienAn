<template>
	<div class="tms-equiptype-container">
		<el-card shadow="hover"
			:body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" label-position="left" :show-message="false" :inlineMessage="true"
				label-width="auto" style="flex: 1 1 0%">
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
						<el-form-item :label="$t('lz.entity.tms.module')" prop="moduleId">
							<el-select v-model="state.queryParams.moduleId" filterable clearable class="w100" @clear="state.queryParams.moduleId = undefined" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tms.module')})">
								<el-option 
									v-for="item in state.moduleList" 
									:key="item.id" 
									:label="item.isDelete != null ? `${item.name} (${$t('lz.label.base.deleted')})` : item.name" 
									:value="item.id" 
								>
									<span :style="{ color: item.isDelete != null ? 'var(--el-text-color-secondary)' : 'inherit', textDecoration: item.isDelete != null ? 'line-through' : 'none' }">
										{{ item.isDelete != null ? `${item.name} (${$t('lz.label.base.deleted')})` : item.name }}
									</span>
								</el-option>
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
				<SearchButton :auth-search="'tmsEquipType:page'" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
			</el-row>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGrid" class="xGrid-style custom-grid" v-bind="options"
				@checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent" :row-config="{ height: 75 }">
				<template #icon_cell="{ row, column }">
					<div class="panel-img">
						<img v-if="row[column.field]" :src="state.baseUrl+'/'+row[column.field]" />
					</div>
				</template>
				<template #toolbar_tools>
					<TableHeader @add="handleAdd" :show-delete="state.showBatchDelete" :auth-add="'tmsEquipType:add'"
						:auth-delete="'tmsEquipType:delete'" @refresh="handleQuery" @custom-column="handleCustom"
						@delete="handleBatchDelete">
					</TableHeader>
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_module="{ row }">
					<span v-if="state.moduleList.find((item: any) => item.id === row.moduleId)">
						{{ state.moduleList.find((item: any) => item.id === row.moduleId)?.name }}
						<span 
							v-if="state.moduleList.find((item: any) => item.id === row.moduleId)?.isDelete != null" 
							style="color: var(--el-color-danger); margin-left: 5px;">
							({{ $t('lz.label.base.deleted') }})
						</span>
					</span>
				</template>
				<template #row_status="{ row }">
					<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, row.status)" />
				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
						<el-button icon="ele-CopyDocument" text type="warning" v-auth="'tmsEquipType:add'"
							@click="handleCopy(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
						<el-button icon="ele-Edit" text type="primary" v-auth="'tmsEquipType:update'"
							@click="handleEdit(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger" v-auth="'tmsEquipType:delete'"
							@click="handleDelete(row)"> </el-button>
					</el-tooltip>

				</template>
				<template #header_name>
					
				</template>
			</vxe-grid>
		</el-card>

		<EditEquipmentType ref="editEquipTypeRef" :title="state.title" :operateType="state.operateType" @handleQuery="handleQuery" />
	</div>
</template>

<script lang="ts" setup name="tmsEquipType">
import { computed, onMounted, reactive, ref, defineAsyncComponent } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { VxeGridInstance, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { Local } from '/@/utils/storage';
import EditEquipmentType from '/@/views/tms/equipmentType/component/editEquipmentType.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import { getAPI } from '/@/utils/axios-utils';
import { TmsEquipmentTypeApi, TmsModulesApi} from '/@/api-services/tms';
import { PageEquipmentTypesInput, PageEquipmentTypesOutput } from '/@/api-services/tms';
import { OperateType } from '/@/types/constants/operate';
import { useItsMediaServerStore } from '/@/stores/its/itsMediaServer';
import { BaseConfigTypeEnum, StatusTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;


const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
const { t: $t } = useI18n();
const xGrid = ref<VxeGridInstance>();

const editEquipTypeRef = ref<InstanceType<typeof EditEquipmentType>>();
const state = reactive({
	queryParams: {
		name: undefined,
		moduleId: undefined,
		code: undefined,
		status: undefined,
	},
	localPageParam: {
		pageSize: 50 as number,
		defaultSort: {} as any,
	},
	visible: false,
	title: '',
	operateType: 'add' as OperateType,
	showBatchDelete: false,
	moduleList: [] as any[],
	baseUrl: '' as string,

});


// Local storage parameters
const localPageParamKey = 'localPageParam:tmsEquipType';

// Table parameter configuration
const options = useVxeTable<PageEquipmentTypesOutput>(
	{
		id: 'tmsEquipType',
		name: 'EquipmentType',
		columns: [
			{ type: 'checkbox', fixed: 'left', width: 45,},
			{ field: 'code', title: $t('lz.entity.base.code'), minWidth: 200, showOverflow: 'tooltip', sortable: true },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 200, showOverflow: 'tooltip', sortable: true,		},
			{ field: 'moduleId', title: $t('lz.entity.tms.module'), minWidth: 100, slots: { default: 'row_module' }, showOverflow: 'tooltip', sortable: true },
			{ field: 'size', title: $t('lz.entity.base.size'), minWidth: 90, showOverflow: 'tooltip', sortable: true },
			{ field: 'defaultIcon', title: $t('lz.entity.tmsEquipmentType.defaultIcon'), minWidth: 150,  showOverflow: 'tooltip', sortable: false,slots: { default: 'icon_cell' },},
			{ field: 'activeIcon', title: $t('lz.entity.tmsEquipmentType.activeIcon'), minWidth: 150, showOverflow: 'tooltip', sortable: false, slots: { default: 'icon_cell' },},
			{ field: 'inactiveIcon', title: $t('lz.entity.tmsEquipmentType.inactiveIcon'), minWidth: 150, showOverflow: 'tooltip', sortable: false, slots: { default: 'icon_cell' },},
			{ field: 'faultIcon', title: $t('lz.entity.tmsEquipmentType.faultIcon'), minWidth: 150, showOverflow: 'tooltip', sortable: false, slots: { default: 'icon_cell' },},
			{ field: 'status', title: $t('lz.entity.base.status'), width: 110, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_status' } },
			{ field: '', title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 100, showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	{
		// Proxy configuration
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		
		// Toolbar configuration
		toolbarConfig: { export: false },
	}
);

// Page initialization
onMounted(async () => {
	state.localPageParam = Local.get(localPageParamKey) || state.localPageParam;
	let resDicData = await getAPI(TmsModulesApi).apiTmsTmsmodulesListGet();
	state.moduleList = resDicData.data.result;
	// Địa chỉ file server dùng chung (chỉ gọi API 1 lần cho toàn app)
	state.baseUrl = await useItsMediaServerStore().ensureLoaded();
	await handleQuery();

	
});

// Query API
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params = Object.assign(state.queryParams, { page: page.currentPage, pageSize: page.pageSize, field: sort?.order ? sort?.field : undefined, order: sort?.order ? sort?.order : undefined, descStr: 'desc' }) as PageEquipmentTypesInput;
	return await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypePagePost(params);
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
	state.queryParams.moduleId = undefined;
	state.queryParams.name = undefined;
	state.queryParams.code = undefined;
	state.queryParams.status = undefined;
	await xGrid.value?.commitProxy('reload');
};

// Open add dialog
const handleAdd = () => {
	state.title = $t('lz.label.base.titleAdd',{name: $t('lz.entity.tms.equipmentType')});
	state.operateType = 'add';
	editEquipTypeRef.value?.openDialog({ status: 1, orderNo: 100 });
};

// Open copy dialog
const handleCopy = (row: any) => {
	state.title = $t('lz.label.base.titleCopy',{name: $t('lz.entity.tms.equipmentType')});
	state.operateType = 'copy';
	editEquipTypeRef.value?.openDialog(row);
};

// Open edit dialog
const handleEdit = (row: any) => {
	state.title = $t('lz.label.base.titleEdit',{name: $t('lz.entity.tms.equipmentType')});
	state.operateType = 'edit';
	editEquipTypeRef.value?.openDialog(row);
};

// Delete
const handleDelete = (row: any) => {
	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.name }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeDeletePost({ id: row.id });
			handleQuery();
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
			await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeBatchdeletePost(selectRecords.filter(l => l.id));
			handleQuery();
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
// Row select change event


</script>
<style scoped>
.panel-img img{
		max-width: 80%;
		max-height: 70px;
		padding: 3px;
	}
</style>