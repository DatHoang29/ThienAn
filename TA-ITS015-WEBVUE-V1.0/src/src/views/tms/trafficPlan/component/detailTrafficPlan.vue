<template>
	<el-dialog v-model="state.isShowDialog" :title="$t('lz.label.tms.shippingAddress')" width="800">
		<template #header>
			<div style="color: #fff">
				<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle">
					<ele-View />
				</el-icon>
				<span>{{ props.title }}</span>
			</div>
		</template>
		<table :model="state.dataList" class="table-data w100" compact>
			<tr class="title">
				<th colspan="4">
					<p>{{ $t('lz.entity.base.code') }}: {{ state.dataList.code }}</p>
				</th>
			</tr>
			<tr class="alt">
				<th>{{ $t('lz.entity.base.name') }}</th>
				<td>{{ state.dataList.name }}</td>
				<th>{{ $t('lz.entity.tms.eventType') }}</th>
				<td>{{ eventTypeName }}</td>
			</tr>
			<tr>
				<th>{{ $t('lz.entity.base.remark') }}</th>
				<td>{{ state.dataList.remark }}</td>
				<th>{{ $t('lz.entity.base.status') }}</th>
				<td>
					<el-tag v-if="state.dataList.status === 1" type="success">{{ $t('lz.label.base.enable') }}</el-tag>
					<el-tag v-else type="danger">{{ $t('lz.label.base.disable') }}</el-tag>
				</td>
			</tr>
		</table>
		
		<el-card shadow="hover" class="work-contact-card mt10" style="width: 100%; margin-bottom: 10px;">
			<template #header>				
				<h3> {{ $t('lz.label.tmsTrafficPlanWorkDetail.addNewJob') }}</h3>
			</template>
			<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto" label-position="left" style="padding-top: 10px;">
				<el-row :gutter="10" >
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.tmsWorkDetail.workDetail')" prop="workDetailId"
							:rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.tmsWorkDetail.workDetail')}), trigger: 'change' }]">
							<el-select v-model="state.ruleForm.workDetailId" 
								:placeholder="$t('lz.label.base.pleaseSelect',{name: $t('lz.entity.tmsWorkDetail.workDetail')})"
								filterable 
								clearable 
								class="w100" 
								@change="getWorkMode">
								<el-option v-for="item in state.dataWorkDetailList" :key="item.id" :label="item.name" :value="item.id" />
							</el-select>
							
						</el-form-item>
					</el-col>	
				</el-row>
				<el-row :gutter="10" >
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.tmsWorkExecute.executeOrg')" prop="executeOrgId"
							:rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.tmsWorkExecute.executeOrg')}), trigger: 'change' }]">
							<el-cascader
								:options="filteredOrgData"
								:props="{ checkStrictly: true, emitPath: false, value: 'id', label: 'name', disabled: (data: any) => data.status !== (StatusTypeEnum.Enable as number)}"
								:placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.system.org')})"
								clearable
								class="w100"
								v-model="state.ruleForm.executeOrgId"
							>
								<template #default="{ node, data }">
									<span>{{ data.name }}</span>
									<span v-if="!node.isLeaf"> ({{ data.children.length }}) </span>
								</template>
							</el-cascader>
						</el-form-item>
					</el-col>	
				</el-row>
				
				<!-- <el-row v-if="showWorkDetailIdRow" :gutter="10">
					<el-col :xs="12" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="$t('lz.entity.tmsTrafficPlan.trafficPlan')">
							<el-input v-model="state.ruleForm.trafficPlanId" class="w100" readonly />
						</el-form-item>
					</el-col>
				</el-row> -->
				<el-row :gutter="10">
					<el-col :xs="12" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.tms.state')" prop="state" 
							:rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.tms.state')}), trigger: 'change' }]">
							<el-select v-model="state.ruleForm.state" clearable class="w100">
								<el-option v-for="item in state.dataWorkStateList" :key="item.code" :label="item.name" :value="item.code"
									:disabled="item.code === '2' && state.queryParams.workdetail !== '2'"
								/>
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="12" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.tmsWorkExecute.step')">
									<!-- Replace el-select with el-input-number -->
							<el-input-number
								v-model="state.ruleForm.step"
								:min="1"
								:max="99"
								class="w100"/>
						</el-form-item>
					</el-col>
				</el-row>
				<el-button type="primary" :loading="state.loading" :disabled="state.loading" @click="submit">
					<el-icon v-if="!state.loading"><Plus /></el-icon>{{ $t('lz.button.base.add') }}
				</el-button>
			</el-form>
		</el-card>
		<el-card>
      <template #header >
        <div style="display: flex; flex-direction: row; justify-content: space-between">
          <h3>{{ $t('lz.label.tmsTrafficPlanWorkDetail.workDetailList') }}</h3>
          <el-button type="primary" @click="handleQuery"><el-icon><Refresh /></el-icon>{{ $t('lz.button.base.refresh') }}</el-button>
        </div>
        
      </template>
      	<div class="grid-container">
			<vxe-grid ref="xGrid"
				class="xGrid-style"
				v-bind="options"
				:row-config="{ useKey: true }"
				:scroll-y="{ enabled: false }"
				:edit-config="{ trigger: 'click', mode: 'cell' }"
				:row-drag="{ trigger: 'row', sort: true }"
				@checkbox-change="selectChangeEvent"
				@checkbox-all="selectChangeEvent"
				>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #orderNo_edit="{ row }">
					<!-- ChỉnhSửa: chỉ gọi API khi blur (rời ô). min=1 để chặn số âm và số 0 -->
					<vxe-input
						v-model="row.step"
						type="number"
						:min="1"
						:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.tmsWorkExecute.step') })"
						@focus="trackInitialValue(row)"
						@blur="handleEditEnd(row)"
					/>
					</template>
				<template #row_status="{ row }">
					<el-select v-model="row.state" :placeholder="$t('lz.label.base.pleaseSelectStatus')" @change="updateOrderNo(row)">
						<el-option
						v-for="type in state.dataWorkStateList"
						:key="type.code"
						:label="type.name"
						:value="type.code"
						:disabled="type.code === '2' && getWorkModeByDetailId(row.workDetailId) !== '2'"
						>
						</el-option>
					</el-select>
				</template>
				<template #row_workDetailId="{ row }">
					<el-select v-model="row.workDetailId" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tmsWorkDetail.workDetail') })"
						 @change="handleRowWorkDetailChange(row)">
						<el-option
						v-for="type in state.dataWorkDetailList"
						:key="type.id"
						:label="type.name"
						:value="type.id"
						>
						</el-option>
					</el-select>
				</template>
				<template #row_executeOrgId="{ row }">
					<el-form-item >
						<el-form-item >
							<el-cascader
								:options="filteredOrgData"
								:props="{ checkStrictly: false, emitPath: false, value: 'id', label: 'name', children: 'children' }"
								:placeholder="$t('lz.label.base.pleaseSelectItem')"
								class="w100"
								@change="updateOrderNo(row)"
								v-model="row.executeOrgId">
								<template #default="{ node, data }">
								<span>{{ data.name }}</span>
								<span v-if="!node.isLeaf"> ({{ data.children.length }}) </span>
								</template>
							</el-cascader>
						</el-form-item>
					</el-form-item>
				</template>

				
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger" v-auth="'tmsTrafficPlan:delete'"
							@click="handleDelete(row)"> </el-button>
					</el-tooltip>
				</template>
				<template #header_name>
					{{ $t('lz.entity.base.name') }}
				</template>
			</vxe-grid>
	  	</div>
		
    </el-card>
		<template #footer>
			<span class="dialog-footer">
				<el-button type="danger" @click="state.isShowDialog = false">{{ $t('lz.button.base.close') }}</el-button>
			</span>
		</template>
	</el-dialog>
</template>

<script lang="ts" setup>
// Import libraries
import { reactive, computed, onMounted,ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { VxeGridInstance, VxeGridPropTypes , VXETable } from 'vxe-table';
import { ElMessage,ElMessageBox,ElSelect, ElOption, } from 'element-plus';
import { Refresh, Plus } from '@element-plus/icons-vue';
import { OperateType } from '/@/types/constants';
// Import API and models
import { SysConfigDataApi } from '/@/api-services/cfgSystem';
import { SysOrgApi,} from '/@/api-services/system';
import { TmsEventTypeApi, TmsWorkDetailApi,TmsTrafficPlanWorkDetailApi, PageTmsTrafficPlanWorkDetailInput} from '/@/api-services/tms';
import { PageTmsTrafficPlanOutput,PageTmsTrafficPlanWorkDetailOutput,UpdateTmsTrafficPlanWorkDetailInput } from '/@/api-services/tms';
import { StatusTypeEnum } from '/@/types/enums';
// const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));


// Declare params
interface Props {
	operateType: OperateType;
	title: string;
}
const xGrid = ref<VxeGridInstance>();
const emits = defineEmits(['handleQueryApi','handleQuery']);
const ruleFormRef = ref();
const { t: $t } = useI18n();
const props = defineProps<Props>();
const state = reactive({
	isShowDialog: false,
	showBatchDelete: false,
	loading: false,
	dataWorkDetailList: [] as any,
	dataOrgList: [] as any,
	TypeList: [] as any,
	dataWorkStateList: [] as any,
	ruleForm: {} as UpdateTmsTrafficPlanWorkDetailInput,
	dataList: {} as PageTmsTrafficPlanOutput,
	initialValues: {} as any,
	queryParams: {
		name: undefined as any,
		code: undefined as any,
		pid: undefined as any,
		workdetail: undefined as any,
	},
});
const options = useVxeTable<PageTmsTrafficPlanWorkDetailOutput>(
	{
		id: 'tmsTrafficPlan',
		name: `${$t('lz.entity.tmsTrafficPlan.trafficPlan')}`,
		height: '300px',
		columns: [
		{
			field: 'step',
			title: $t('lz.entity.tmsWorkExecute.step'),
			minWidth: 15,
			showOverflow: 'tooltip',
			sortable: false,
			editRender: {
			name: 'input',
			props: {
				type: 'number',
				min: 0,
				placeholder: `${$t('lz.label.base.pleaseSelectNumber')}`
			},
			},
			slots: { edit: 'orderNo_edit' },
		},
		{ 
			field: 'workDetailId', 
			title: `${$t('lz.entity.tmsWorkDetail.workDetail')}`, 
			minWidth: 140, 
			formatter: ({ cellValue }: any) => state.dataWorkDetailList.find((u: any) => u.id == cellValue)?.name, 
			showOverflow: 'tooltip' ,
			slots: {edit: 'row_workDetailId'},
			editRender: {
			name: 'select', // Use a select component for editing the state
			props: {
				options: state.dataWorkDetailList.map(type => ({
				value: type.id,
				label: type.name
				})),
				placeholder: `${$t('lz.label.base.pleaseSelectStatus')}`
			},
			events: {
				change: (params) => updateOrderNo(params.row) // Function to handle state update
			}
			}
		},
		{
			field: 'executeOrgId',
			title: `${$t('lz.label.tmsTrafficPlanWorkDetail.executeOrg')}`,
			minWidth: 140,
			formatter: ({ cellValue }: any) => {
				// Hàm đệ quy tìm kiếm tổ chức theo ID
				const findOrgById = (list: any[], id: any): any => {
				for (const org of list) {
					if (org.id === id) {
					return org.name;
					}
					if (org.children && org.children.length > 0) {
					const result = findOrgById(org.children, id);
					if (result) {
						return result;
					}
					}
				}
				return null;
				};

				// Tìm kiếm tổ chức
				const orgName = findOrgById(state.dataOrgList, cellValue);
				return orgName || ''; // Trả về tên tổ chức hoặc chuỗi rỗng
			},
			showOverflow: 'tooltip',
			slots: {edit: 'row_executeOrgId'},
			editRender: {
				name: 'ElCascader', // Sử dụng 'el-cascader' làm trình chỉnh sửa
				options: 'dataOrgList' as any, // Danh sách kiểu cha-con từ state
				props: {
				valueKey: 'id',
				labelKey: 'name',
				childrenKey: 'children',
				clearable: true,
				placeholder: `${$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.system.org') })}` ,
				},
				events: {
				change: ({ row, value }: any) => {
					row.executeOrgId = value; // Cập nhật giá trị trong hàng
				},
				},
			},
		},


		{
			field: 'state', 
			title: $t('lz.entity.tms.state'), 
			width: 100,
			showOverflow: 'tooltip', 
			formatter: ({ cellValue }: any) => state.dataWorkStateList.find((u: any) => u.code == cellValue)?.name, 
			slots: {edit: 'row_status'},
			editRender: {
			name: 'select', // Use a select component for editing the state
			props: {
				options: state.dataWorkStateList.map(type => ({
				value: type.id,
				label: type.name
				})),
				placeholder: $t('lz.label.base.pleaseSelectState')
			},
			events: {
				change: (params) => updateOrderNo(params.row) // Function to handle state update
			}
			}
		},
		{ 
			title: $t('lz.label.base.action'), 
			fixed: 'right', 
			width: 80, 
			showOverflow: true, 
			slots: { default: 'row_buttons' } 
		},
		],
	},
	{
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		toolbarConfig: { export: false },

	}
);

const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
    const params = {
        page: page.currentPage,
        pageSize: page.pageSize,
        field: sort.field,
        order: sort.order,
        descStr: 'desc',
        trafficPlanId: state.dataList.id,
    } as PageTmsTrafficPlanWorkDetailInput;
    return await getAPI(TmsTrafficPlanWorkDetailApi).apiTmsTmstrafficplanworkdetailPagePost(params);
};


// Lấy workMode của công việc theo workDetailId (dùng cho từng dòng trong grid)
const getWorkModeByDetailId = (workDetailId: any) => {
    return state.dataWorkDetailList.find((item: any) => item.id === workDetailId)?.workMode;
};


// Page initialization
onMounted(async () => {
	const response = await getAPI(TmsWorkDetailApi).apiTmsTmsworkdetailListGet();
	state.dataWorkDetailList = response.data.result;
	let resDicData = await getAPI(TmsEventTypeApi).apiTmsTmseventtypeListGet();
	state.TypeList = resDicData.data.result;
	const workdetail = await getAPI(SysOrgApi).apiSystemSysorgListGet("0");
	state.dataOrgList = workdetail.data.result;
	const dataWorkStateList = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet('tms_work_state');
	state.dataWorkStateList = dataWorkStateList.data.result;
	await handleQuery();

});
const filteredOrgData = computed(() => {
return state.dataOrgList.filter((item:any) => item.id !== state.queryParams.pid);
});
// Computed property to get event type name
const eventTypeName = computed(() => {
	const eventType = state.TypeList.find((type: any) => type.id === state.dataList.eventTypeId);
	return eventType ? eventType.name : '';
});
const trackInitialValue = (row: any) => {
	state.initialValues[row.id] = row.step;
};

// Handle input blur event (end of editing)
// ChỉnhSửa: chỉ gọi API khi rời ô, giá trị hợp lệ (số nguyên >= 1) và có thay đổi thật
const handleEditEnd = async (row: any) => {
	const step = Math.floor(Number(row.step));

	// Chặn số âm / số 0 / rỗng / không phải số: khôi phục giá trị cũ, không gọi API
	if (!Number.isFinite(step) || step < 1) {
		row.step = state.initialValues[row.id];
		ElMessage.warning($t('lz.validation.base.greaterThanOrEqual', { PropertyName: $t('lz.entity.tmsWorkExecute.step'), ComparisonValue: 1 }));
		return;
	}

	// Chuẩn hoá về số nguyên
	row.step = step;

	// Chỉ gọi API khi giá trị thực sự thay đổi
	if (state.initialValues[row.id] !== row.step) {
		await updateOrderNo(row);
	}
};

const updateOrderNo = async (row: any) => {
	const updateData = row;

	await getAPI(TmsTrafficPlanWorkDetailApi).apiTmsTmstrafficplanworkdetailUpdatePost(updateData);
	await handleQuery();
	ElMessage.success($t('lz.message.base.actionSuccess'));
};

const getWorkMode = (selectedId: any) => {
	const selectedItem = state.dataWorkDetailList.find(
		(item:any) => item.id === selectedId
	);

	if (selectedItem) {
		state.queryParams.workdetail = selectedItem.workMode; // Lưu workMode vào biến
		console.log('Selected Work Mode:', state.queryParams.workdetail); // In ra console để kiểm tra
	} else {
		state.queryParams.workdetail = null; // Reset nếu không tìm thấy
	}
	// Khi chọn lại Công việc chi tiết thì xóa trường Tình trạng để chọn lại
	state.ruleForm.state = undefined;
	ruleFormRef.value?.clearValidate('state');
};

// Cập nhật khi đổi công việc chi tiết trên từng dòng trong lưới
const handleRowWorkDetailChange = (row: any) => {
	// Nếu tình trạng đang là Auto ('2') nhưng công việc chi tiết mới không hỗ trợ Auto thì chuyển về Manual ('1')
	if (row.state === '2' && getWorkModeByDetailId(row.workDetailId) !== '2') {
		row.state = '1';
	}
	updateOrderNo(row);
};

// Open dialog
const openDialog = (row: any) => {
	state.dataList = JSON.parse(JSON.stringify(row.row));
	state.ruleForm = {
		trafficPlanId: row.row.id, // Thiết lập giá trị mặc định cho workDetailId
		step: row.step ?? 1, // ChỉnhSửa: mặc định Bước = 1, tránh thêm mới với step rỗng
		state: undefined,
	};
	state.queryParams.workdetail = null;
	state.loading = false;
	state.isShowDialog = true;
	ruleFormRef.value?.clearValidate();
	handleQuery();
};
const handleQuery = async () => {
	options.loading = true;
	await xGrid.value?.commitProxy('reload') ;
	options.loading = false;
	selectChangeEvent();
	};
	function getCheckedRecords() {
		return xGrid.value?.getCheckboxRecords() ?? [];
	}
	function selectChangeEvent() {
		state.showBatchDelete = getCheckedRecords().length > 0;
	}
	const submit = () => {
		if (state.loading) return;
		ruleFormRef.value?.validate(async (valid: boolean) => {
			if (!valid) return;
			state.loading = true;
			try {
				await getAPI(TmsTrafficPlanWorkDetailApi).apiTmsTmstrafficplanworkdetailAddPost(state.ruleForm);
				ElMessage.success($t('lz.message.base.actionSuccess'));
				await handleQuery();
			} catch (error) {
				console.error(error);
			} finally {
				state.loading = false;
			}
		});
	};
	
	// Delete
const handleDelete = (row: any) => {
	const workDetailName = state.dataWorkDetailList.find((u: any) => u.id == row.workDetailId)?.name ?? ''; // ChuongLP - 30-07-2026 Thêm xử lý để lấy được tên của work

	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: workDetailName }), $t('lz.message.base.warning'), {
		confirmButtonText:  $t('lz.button.base.confirm'),
		cancelButtonText:  $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(TmsTrafficPlanWorkDetailApi).apiTmsTmstrafficplanworkdetailDeletePost({ id: row.id });
			handleQuery();
			ElMessage.success($t('lz.message.base.deleteSuccess'));
		})
		.catch(() => { });
};

// Export object
defineExpose({ openDialog });
</script>

<style scoped>
.grid-container {
	padding: 16px;
	box-sizing: border-box;
	max-height: 100%;
	overflow: auto;
}

.xGrid-style {
	width: 100%;
	height: 250px!important;
	max-height: 300px; /* Adjust as necessary */
}
.table-data {
	border-collapse: collapse;  
}

.table-data td, th {
	/* padding: 15px 10px; */
	text-align: left;
	border-color: #E6E8EB !important;
	border-left: thin solid;
	border-top: thin solid;
	border-right: thin solid;
}

.table-data tr:last-child td, th {
	border-bottom: thin solid;
}

.table-data tr.title th {
	text-align: center;
	/* font-size: large; */
}

.table-data tr.alt {
	background-color: #F0F2F5;
}
</style>
