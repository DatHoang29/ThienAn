<template>
	<div class="tms-equipment-container">
		<el-card shadow="hover"
			:body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true"
				label-width="auto" style="flex: 1 1 0%" label-position="left" >
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item  :label="$t('lz.entity.base.code')" prop="code">
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
					<!-- <el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.port')" prop="port">
							<el-input v-model="state.queryParams.port" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.port')})" clearable
								@keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col> -->
					<!-- <el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.ipAddress')" prop="ip">
							<el-input v-model="state.queryParams.ip" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.ipAddress')})" clearable
								@keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col> -->
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.equipmentType')" prop="equipmentTypeId">
							<el-select v-model="state.queryParams.equipmentTypeId" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tms.equipmentType')})" filterable clearable class="w100">
								<el-option v-for="item in state.typeList" :key="item.id" :label="item.isDelete != null ? `${item.name}` : item.name" :value="item.id"
								>
									<span :style="{ color: item.isDelete != null ? 'var(--el-text-color-secondary)' : 'inherit' , textDecoration: item.isDelete != null ? 'line-through' : 'none' }">
										{{ item.isDelete != null ? `${item.name}` : item.name }}
									</span>
								</el-option>
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.direction')" prop="directionId">
							<el-select v-model="state.queryParams.directionId" clearable :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tms.direction')})"  class="w100">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TmsDirectionType)" :key="item.code" :label="item.name" :value="item.code" />


							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.location')" prop="location">
							<el-select v-model="state.queryParams.location" clearable :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tms.location')})"  class="w100">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TmsZoneLocation)" :key="item.code" :label="item.name" :value="item.code" />

							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.equipmentState')" prop="equipmentState">
							<el-select v-model="state.queryParams.equipmentState" clearable :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tms.equipmentState')})"  class="w100">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TmsEquipmentState)" :key="item.code" :label="item.name" :value="item.code" />
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
				<SearchButton :auth-search="'tmsEquip:page'" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
			</el-row>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents"
				@checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">
				 <template #row_km_met="{ row }">
					Km {{ row.kmNumber }} + {{ row.metNumber }}
				</template>
				<template #toolbar_tools>
					<TableHeader @add="handleAdd" :show-delete="state.showBatchDelete" :auth-add="'tmsEquip:add'"
						:auth-delete="'tmsEquip:delete'" @refresh="handleQuery" @custom-column="handleCustom"
						@delete="handleBatchDelete">
					</TableHeader>
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_equip_state="{ row }">
					<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsEquipmentState, row.equipmentState)" />
				</template>

				<template #row_status="{ row }">
					<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, row.status)" />
				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />

				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
						<el-button icon="ele-CopyDocument" text type="warning" v-auth="'tmsEquip:add'"
							@click="handleCopy(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
						<el-button icon="ele-Edit" text type="primary" v-auth="'tmsEquip:update'"
							@click="handleEdit(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger" v-auth="'tmsEquip:delete'"
							@click="handleDelete(row)"> </el-button>
					</el-tooltip>

				</template>
			</vxe-grid>
		</el-card>

		<EditEquipment ref="editEquipRef" :title="state.title" :operateType="state.operateType" @handleQuery="handleQuery" />
	</div>
</template>

<script lang="ts" setup name="tmsEquip">
import { onMounted, onActivated, onDeactivated, reactive, ref, defineAsyncComponent, onUnmounted } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { VxeGridInstance, VxeGridListeners } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { Local } from '/@/utils/storage';
import EditEquipment from './component/editEquipment.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import { getAPI } from '/@/utils/axios-utils';
import { TmsEquipmentApi, TmsEquipmentTypeApi } from '/@/api-services/tms';
import { PageEquipmentOutput } from '/@/api-services/tms';
import { OperateType } from '/@/types/constants/operate';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import { SSEDemoApiAxiosParamCreator } from '/@/api-services/vds';
import { buildFetchHeaders, fetchSseApi } from '/@/utils/fetch-utils';
import { useRoute } from 'vue-router';

const route = useRoute();
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;

const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
const { t: $t } = useI18n();
const xGrid = ref<VxeGridInstance>();
const editEquipRef = ref<InstanceType<typeof EditEquipment>>();
const state = reactive({
	queryParams: {
		code:undefined as unknown as string,
		name: undefined as unknown as string,
		ip: undefined as unknown as string,
		port: undefined as unknown as string,
		equipmentTypeId: undefined as unknown as string,
		status: undefined as unknown as any,
		directionId: undefined as unknown as string,
		equipmentState: undefined as unknown as string,
		location: undefined as unknown as string,
	},
	localPageParam: {
		pageSize: 50 as number,
		defaultSort: {} as any,
	},
	visible: false,
	title: '',
	operateType: 'add' as OperateType,
	showBatchDelete: false,
	typeList: [] as any
});
// Local storage parameters
const localPageParamKey = 'localPageParam:tmsEquip';
// Table parameter configuration
const options = useVxeTable<PageEquipmentOutput>(
	{
		id: 'tmsEquip',
		name: 'Equipment',
		columns: [
			{ type: 'checkbox', fixed: 'left', width: 45 },
			{ field: 'code', title: $t('lz.entity.base.code'), minWidth: 150, showOverflow: 'tooltip', sortable: true },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 150, showOverflow: 'tooltip', sortable: true,},
			{ field: 'equipmentTypeId', title: $t('lz.entity.tms.equipmentType'), minWidth: 150, sortable:true, formatter: ({ cellValue }: any) => {
				const type = state.typeList.find((u: any) => u.id == cellValue);
				return type ? (type.isDelete != null ? `${type.name} (Đã xóa)` : type.name) : '';}, showOverflow: 'tooltip' },
			{ field: 'equipmentState', title: $t('lz.entity.tms.equipmentState'), width: 150, sortable:true, showOverflow: 'tooltip', slots: { default: 'row_equip_state' } },
			{ field: 'directionId', title: $t('lz.entity.tms.direction'), minWidth: 80, showOverflow: 'tooltip', sortable: true, formatter: ({ cellValue }: any) => getConfigItemByCode(BaseConfigTypeEnum.TmsDirectionType, cellValue)?.name },
			{ field: 'location', title: $t('lz.entity.tms.location'), minWidth: 80, showOverflow: 'tooltip', sortable: true,  formatter: ({ cellValue }: any) => getConfigItemByCode(BaseConfigTypeEnum.TmsZoneLocation, cellValue)?.name },
			{ field: 'kmNumber', title: $t('lz.entity.tms.kmMetNumber'), minWidth: 100, showOverflow: 'tooltip', slots: { default: 'row_km_met' } },

			{ field: 'status', title: $t('lz.entity.base.status'), width: 110, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_status' } },
			{ field: '', title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 100, showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	// vxeGrid configuration parameters (any parameter can be overridden here), refer to the official vxe-table documentation
	{
		// Proxy configuration
		proxyConfig: { enabled: false },
		data: [],
		rowConfig: { keyField: 'id' },
		// Sorting configuration
		sortConfig: { remote: true },
		// Pagination configuration
		pagerConfig: { currentPage: 1, pageSize: state.localPageParam.pageSize, total: 0 },
		// Toolbar configuration
		toolbarConfig: { export: false },
	}
);

// Abort cả kết nối đang mở và request đang được chuẩn bị khi rời trang.
let sseAbort: AbortController | null = null;
let equipmentSnapshot: PageEquipmentOutput[] = [];
let currentPage = 1;
let latestPageRequest = 0;
let lastStreamIds = '';
let latestStates = new Map<string, { equipmentState?: number | null }>();
let disposed = false;
let pageActive = true;
const authScope = () => JSON.stringify(buildFetchHeaders());
let currentScope = authScope();
let authTimer: ReturnType<typeof setInterval> | undefined;
const startEquipmentSse = async () => {
    if (disposed || !pageActive) return;
    if (sseAbort && !sseAbort.signal.aborted) return;
    const controller = new AbortController();
    sseAbort = controller;
	try {
		const request = await SSEDemoApiAxiosParamCreator().apiVdsSsedemoGet();
		if (disposed || !pageActive || controller.signal.aborted) return;
		const connection = fetchSseApi<EquipmentSnapshot>(request.url, {
			event: 'snapshot',
			shareAcrossTabs: true,
			signal: controller.signal,
			reconnect: true,
			onError: error => { console.warn('[equipmentSSE] Stream reconnecting:', error); },
			onResult: handleSseDeviceStatus,
		});
		await connection.done;
	} catch (error) {
		if (!controller.signal.aborted) console.error('[equipmentSSE] Stream failed:', error);
	}
};

// Page initialization
onMounted(async () => {
	state.localPageParam = Local.get(localPageParamKey) || state.localPageParam;
	options.loading = true;

	// Nếu điều hướng từ trang biểu đồ có kèm loại thiết bị => lọc sẵn - ChuongLP - 30/07/2026
    const typeIdFromQuery = route.query.Type as string;
    if (typeIdFromQuery) {
        state.queryParams.equipmentTypeId = typeIdFromQuery;
    }
	try {
		const resDicData = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet(undefined, undefined, undefined, undefined, undefined, true as any);
		state.typeList = resDicData.data.result;
	} catch (error) { console.error('[equipmentSSE] Equipment types failed:', error); }
	if (disposed) return;
	await handleQuery().catch(error => console.error('[equipmentSSE] Equipment page failed:', error));
	if (disposed) return;
	void startEquipmentSse();
	authTimer = setInterval(() => {
		if (!pageActive) return;
		const scope = authScope();
		if (scope === currentScope) return;
		currentScope = scope;
		sseAbort?.abort();
		lastStreamIds = '';
		latestStates.clear();
		equipmentSnapshot = [];
		options.data = [];
		void loadPage().catch(error => console.error('[equipmentSSE] Equipment page failed after login change:', error));
		void startEquipmentSse();
	}, 1000);
	

});

//Xử lý khi rời khỏi trang 
onUnmounted(() => {
	disposed = true;
	pageActive = false;
	latestPageRequest++;
	sseAbort?.abort();
	if (authTimer) clearInterval(authTimer);
});
onActivated(() => {
	pageActive = true;
	const scope = authScope();
	if (scope !== currentScope) {
		currentScope = scope;
		sseAbort?.abort();
		lastStreamIds = '';
		latestStates.clear();
		equipmentSnapshot = [];
		options.data = [];
		void loadPage().catch(error => console.error('[equipmentSSE] Equipment page failed after login change:', error));
	}
	if (!sseAbort || sseAbort.signal.aborted) void startEquipmentSse();
});
onDeactivated(() => { pageActive = false; sseAbort?.abort(); });

// Page owns complete equipment fields and server-side filters. SSEDemo only supplies ID/state.
const loadPage = async () => {
    if (disposed) return;
    const request = ++latestPageRequest;
    options.loading = true;
    try {
        const sort = state.localPageParam.defaultSort;
        const response = await getAPI(TmsEquipmentApi).apiTmsTmsequipmentPagePost({
            ...state.queryParams,
            page: currentPage,
            pageSize: state.localPageParam.pageSize,
            field: sort?.order ? sort?.field : undefined,
            order: sort?.order ? sort?.order : undefined,
            descStr: 'desc',
        });
        if (disposed || request !== latestPageRequest) return;
        equipmentSnapshot = (response.data.result?.records ?? []).map(row => {
            const update = row.id ? latestStates.get(row.id) : undefined;
            return update ? { ...row, equipmentState: update.equipmentState } : row;
        });
        options.data = equipmentSnapshot;
        options.pagerConfig = { ...options.pagerConfig, currentPage, pageSize: state.localPageParam.pageSize, total: response.data.result?.total ?? 0 };
        state.showBatchDelete = false;
        void xGrid.value?.clearCheckboxRow();
    } finally {
        if (request === latestPageRequest) options.loading = false;
    }
};
const handleQuery = async () => { currentPage = 1; await loadPage(); };

// Reset operation
const resetQuery = async () => {
	state.queryParams.code = undefined as unknown as string;
	state.queryParams.name = undefined as unknown as string;
	state.queryParams.port = undefined as unknown as string;
	state.queryParams.location = undefined as unknown as string;
	state.queryParams.equipmentTypeId = undefined as unknown as string;
	state.queryParams.directionId = undefined as unknown as string;
	state.queryParams.equipmentState = undefined as unknown as string;
	state.queryParams.ip = undefined as unknown as string;
	state.queryParams.status = undefined as unknown as string;
	await handleQuery();
};

// Open add dialog
const handleAdd = () => {
	state.title = $t('lz.label.base.titleAdd',{name: $t('lz.entity.tms.equipment')});
	state.operateType = 'add';
	editEquipRef.value?.openDialog({ status: 1, orderNo: 100, equipmentState: 1 });
};

// Open copy dialog
const handleCopy = (row: any) => {
	state.title = $t('lz.label.base.titleCopy',{name: $t('lz.entity.tms.equipment')});
	state.operateType = 'copy';
	editEquipRef.value?.openDialog(row);
};

// Open edit dialog
const handleEdit = (row: any) => {
	state.title = $t('lz.label.base.titleEdit',{name: $t('lz.entity.tms.equipment')});
	state.operateType = 'edit';
	editEquipRef.value?.openDialog(row);
};


// Delete
const handleDelete = (row: any) => {
	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.name }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(TmsEquipmentApi).apiTmsTmsequipmentDeletePost({ id: row.id });
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
			await getAPI(TmsEquipmentApi).apiTmsTmsequipmentBatchdeletePost(selectRecords.filter(l => l.id));
			handleQuery();
			ElMessage.success($t('lz.message.base.deleteSuccess'));

		})
		.catch(() => { });
}

interface EquipmentSnapshot {
	timestamp: string;
	total: number;
	data: { id?: string; equipmentState?: number | null }[];
}

// Wrapper/lỗi/loading do fetchSseApi xử lý; màn hình chỉ cập nhật snapshot.
// Giữ ID độc lập: nhiều thiết bị có thể trùng code.
const handleSseDeviceStatus = (snapshot: EquipmentSnapshot): void => {
	if (sseAbort?.signal.aborted) return;
	const rows = (Array.isArray(snapshot?.data) ? snapshot.data : []).filter(row => row.id);
	const streamIds = rows.map(row => row.id).sort().join('|');
	const changedMembership = lastStreamIds !== '' && streamIds !== lastStreamIds;
	lastStreamIds = streamIds;
	const states = new Map<string, { equipmentState?: number | null }>(rows.map(row => [row.id!, row]));
	latestStates = states;
	let changedFilteredState = false;
	equipmentSnapshot = equipmentSnapshot.map(row => {
		const update = row.id ? states.get(row.id) : undefined;
		if (!update) return row;
		if (state.queryParams.equipmentState != null && update.equipmentState !== row.equipmentState) changedFilteredState = true;
		return { ...row, equipmentState: update.equipmentState, lastDeviceStatusTime: snapshot.timestamp };
	});
	options.data = equipmentSnapshot;
	if (changedMembership || changedFilteredState) void loadPage().catch(error => console.error('[equipmentSSE] Equipment page refresh failed:', error));
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
const gridEvents: VxeGridListeners<PageEquipmentOutput> = {
	// Only valid when pager-config is configured, this event will be triggered when the page changes
	async pageChange({ currentPage: page, pageSize }) {
		currentPage = page;
		state.localPageParam.pageSize = pageSize;
		await loadPage();
	},
	// This event will be triggered when the sorting condition changes
	async sortChange({ field, order }) {
		state.localPageParam.defaultSort = { field: field, order: order!, descStr: 'desc' };
		await loadPage();
	},
};
</script>
