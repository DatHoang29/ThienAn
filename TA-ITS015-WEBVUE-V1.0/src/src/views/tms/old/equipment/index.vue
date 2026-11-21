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
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.equipmentType')" prop="equipmentTypeId">
							<el-select v-model="state.queryParams.equipmentTypeId" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tms.equipmentType')})" filterable clearable class="w100">
								<el-option v-for="item in selectableTypeList" :key="item.id" :label="item.name" :value="item.id" />
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
					<span v-if="row.kmNumber != null || row.metNumber != null">Km {{ row.kmNumber ?? '' }} + {{ row.metNumber ?? '' }}</span>
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
				<template #row_equip_type="{ row }">
					<span v-if="state.typeList.find((item: any) => item.id === row.equipmentTypeId)">
						{{ state.typeList.find((item: any) => item.id === row.equipmentTypeId)?.name }}
						<span
							v-if="state.typeList.find((item: any) => item.id === row.equipmentTypeId)?.isDelete != null"
							style="color: var(--el-color-danger); margin-left: 5px;">
							({{ $t('lz.label.base.deleted') }})
						</span>
					</span>
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
import { onMounted, reactive, ref, defineAsyncComponent, nextTick, onUnmounted, computed } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { Local } from '/@/utils/storage';
import EditEquipment from './component/editEquipment.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import { getAPI } from '/@/utils/axios-utils';
import { TmsEquipmentApi, TmsEquipmentTypeApi } from '/@/api-services/tms';
import { PageEquipmentInput, PageEquipmentOutput } from '/@/api-services/tms';
import { OperateType } from '/@/types/constants/operate';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import mittBus from '/@/utils/mitt';
import { TransporterEvent } from '/@/transporter/constants/transporterEvent';
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
			{ field: 'equipmentTypeId', title: $t('lz.entity.tms.equipmentType'), minWidth: 150, sortable:true, showOverflow: 'tooltip', slots: { default: 'row_equip_type' } },
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
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		// Pagination configuration
		pagerConfig: { pageSize:  state.localPageParam.pageSize },
		// Toolbar configuration
		toolbarConfig: { export: false },
	}
);

// Page initialization
onMounted(async () => {
	state.localPageParam = Local.get(localPageParamKey) || state.localPageParam;
	// Lắng nghe trạng thái thiết bị realtime từ NATS.
	mittBus.on(TransporterEvent.StatusFms as any, handleNatsDeviceStatus);
	let resDicData = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet(undefined, undefined, undefined, undefined,undefined, true as any);
	state.typeList = resDicData.data.result;

	// Nếu điều hướng từ trang biểu đồ có kèm loại thiết bị => lọc sẵn - ChuongLP - 30/07/2026
    const typeIdFromQuery = route.query.Type as string;
    if (typeIdFromQuery) {
        state.queryParams.equipmentTypeId = typeIdFromQuery;
    }

	await handleQuery();

});

//Xử lý khi rời khỏi trang 
onUnmounted(() => {
	mittBus.off(TransporterEvent.StatusFms as any, handleNatsDeviceStatus);
});

// Query API
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params = Object.assign(state.queryParams, {
		page: page.currentPage,
		pageSize: page.pageSize,
		field: sort?.order ? sort?.field : undefined,
		order: sort?.order ? sort?.order : undefined,
		descStr: 'desc',
	}) as PageEquipmentInput;
	return await getAPI(TmsEquipmentApi).apiTmsTmsequipmentPagePost(params);
};

// Query operation
const handleQuery = async () => {
	options.loading = true;
	await xGrid.value?.commitProxy('reload') ;
	options.loading = false;
	selectChangeEvent();
};

// Chỉ các loại thiết bị chưa xóa dùng cho ô lọc. state.typeList vẫn giữ cả loại đã xóa để cột trong bảng tra tên.
const selectableTypeList = computed(() => state.typeList.filter((item: any) => item.isDelete == null));

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
	await xGrid.value?.commitProxy('reload');
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

// Lấy data hiện tại của vxe-grid.
const getCurrentGridRows = (): any[] => {
	const tableData = xGrid.value?.getTableData?.();
	return tableData?.fullData ?? tableData?.tableData ?? [];
};

// so sanh 2 giá trị, bỏ qua khoảng trắng và không phân biệt hoa thường
const sameValue = (a: any, b: any) => {
	if (a === undefined || a === null || b === undefined || b === null) return false;
	return String(a).trim().toLowerCase() === String(b).trim().toLowerCase();
};

// Tìm thiết bị tương ứng trên bảng dựa vào thông tin từ NATS message
const findEquipmentRowByNatsItem = (item: any) => {
	const rows = getCurrentGridRows();

	const info = item?.Info ?? item?.info ?? {};
	const referenceId = info.ReferenceId ?? info.referenceId;
	const code = info.Code ?? info.code;
	const ip = info.Ip ?? info.ip;

	return rows.find((row: any) =>
		sameValue(row.id, referenceId) ||
		sameValue(row.equipmentId, referenceId) ||
		sameValue(row.code, code) ||
		sameValue(row.ip, ip)
	);
};

// Cập nhật trực tiếp trạng thái thiết bị trên bảng khi nhận được message từ NATS
const updateEquipmentRowsByNatsStatus = async (payload: any) => {
	const results = payload?.Result ?? payload?.result ?? [];

	if (!Array.isArray(results) || results.length === 0) return;

	let updatedCount = 0;

	results.forEach((item: any) => {
		const row = findEquipmentRowByNatsItem(item);
		if (!row) return;

		const data = item?.Data ?? item?.data ?? {};	// Thông tin trạng thái thiết bị từ NATS message
		const status = data.Status ?? data.status;		// Trạng thái thiết bị (có thể là code hoặc giá trị hiển thị tùy theo message)
		const rawStatus = data.RawStatus ?? data.rawStatus;

		row.equipmentState = status;
		row.rawDeviceStatus = rawStatus;
		row.lastDeviceStatusTime = payload?.Time ?? payload?.time ?? new Date().toISOString();

		updatedCount++;
	});

	if (updatedCount > 0) {
		await nextTick(); // Đảm bảo DOM đã cập nhật trước khi gọi updateData
		await xGrid.value?.updateData?.(); // Cập nhật lại bảng để hiển thị trạng thái mới
		console.log(`[tmsEquip] Đã cập nhật trực tiếp ${updatedCount} thiết bị từ NATS.`);
	}
};

// handler — bỏ check packetType, gọi thẳng vì subject đã đúng
const handleNatsDeviceStatus = async (payload: any) => {
  if (!payload) return;
  await updateEquipmentRowsByNatsStatus(payload);
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
	async pageChange({ pageSize }) {
		state.localPageParam.pageSize = pageSize;
	},
	// This event will be triggered when the sorting condition changes
	async sortChange({ field, order }) {
		state.localPageParam.defaultSort = { field: field, order: order!, descStr: 'desc' };
	},
};
</script>
