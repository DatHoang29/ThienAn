<template>
	<div class="zone-tab">
		<!-- Tìm kiếm của tab "Danh sách khu vực" -->
		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" label-position="left" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.name')" prop="name">
							<el-input v-model="state.queryParams.name" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.name')})" clearable @keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.code')" prop="code">
							<el-input v-model="state.queryParams.code" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.code')})" clearable @keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.type')" prop="type">
							<el-select v-model="state.queryParams.type" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.base.type')})" clearable @clear="state.queryParams.type = undefined">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TmsZoneType)" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.status')" prop="status">
							<el-select v-model="state.queryParams.status" filterable clearable class="w100" @clear="state.queryParams.status = undefined"
								:placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.base.status')})">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code" :label="item.name" :value="parseInt(item.code)" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.location')">
							<el-select v-model="state.queryParams.location" clearable class="w100" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tms.location')})">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TmsZoneLocation)" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.direction')">
							<el-select v-model="state.queryParams.laneId" clearable class="w100" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tms.direction')})">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TmsDirectionType)" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="10" :lg="6" :xl="6" class="mb5">
						<el-form-item :label="$t('lz.entity.tms.fromKmNumber')">
							<el-row :gutter="20">
								<el-col :span="12"><el-input-number v-model="state.queryParams.fromKmNumber" :min="0" :placeholder="$t('lz.entity.tms.kmNumber')" class="w100" /></el-col>
								<el-col :span="12"><el-input-number v-model="state.queryParams.fromMetNumber" :min="0" :max="999" :placeholder="$t('lz.entity.tms.metNumber')" class="w100" /></el-col>
							</el-row>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="10" :lg="6" :xl="6" class="mb5">
						<el-form-item :label="$t('lz.entity.tms.toKmNumber')">
							<el-row :gutter="20">
								<el-col :span="12"><el-input-number v-model="state.queryParams.toKmNumber" :min="0" :placeholder="$t('lz.entity.tms.kmNumber')" class="w100" /></el-col>
								<el-col :span="12"><el-input-number v-model="state.queryParams.toMetNumber" :min="0" :max="999" :placeholder="$t('lz.entity.tms.metNumber')" class="w100" /></el-col>
							</el-row>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />
			<el-row>
				<SearchButton :auth-search="'tmsZone:page'" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
			</el-row>
		</el-card>

		<!-- Cùng khung chiều cao với tab Gán thiết bị/Tốc độ (zoneTab.scss): bảng height 'auto' cần khung cha có chiều cao cố định. -->
		<div class="zone-split">
		<el-card class="full-table" shadow="hover" style="flex: 1; min-width: 0">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents" @checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">
				<template #toolbar_buttons>
					<el-button-group style="padding-left: 12px">
						<el-button type="primary" icon="ele-Expand" @click="handleExpand"> {{ $t('lz.button.base.expandAll') }} </el-button>
						<el-button type="primary" icon="ele-Fold" @click="handleFold"> {{ $t('lz.button.base.collapseAll') }} </el-button>
					</el-button-group>
				</template>
				<template #toolbar_tools>
					<TableHeader @add="handleAdd" :show-delete="state.showBatchDelete" :auth-add="'tmsZone:add'" :auth-delete="'tmsZone:delete'"
						@refresh="handleQuery" @custom-column="handleCustom" @delete="handleBatchDelete" />
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_location="{ row }">
					{{ getConfigItemByCode(BaseConfigTypeEnum.TmsZoneLocation, row.location)?.name }}
				</template>
				<template #row_from_location="{ row }">
					<span v-if="row.fromKmNumber != null || row.fromMetNumber != null">{{$t('lz.entity.tms.km')}} {{ row.fromKmNumber ?? '' }} + {{ row.fromMetNumber ?? '' }}</span>
				</template>
				<template #row_to_location="{ row }">
					<span v-if="row.toKmNumber != null || row.toMetNumber != null">{{$t('lz.entity.tms.km')}} {{ row.toKmNumber ?? '' }} + {{ row.toMetNumber ?? '' }}</span>
				</template>
				<template #row_type="{ row }">
					{{ getConfigItemByCode(BaseConfigTypeEnum.TmsZoneType, row.type)?.name }}
				</template>
				<template #row_status="{ row }">
					<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, row.status)" />
				</template>
				<template #row_lane="{ row }">
					{{ getConfigItemByCode(BaseConfigTypeEnum.TmsDirectionType, row.laneId)?.name }}
				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.label.tmsZone.tabMapping')" placement="top" :auto-close="1000">
						<el-button icon="ele-Connection" text type="success" v-auth="'tmsZoneEquipment:view'" @click="navigate('mapping', row.id)"></el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.label.tmsZone.tabSpeed')" placement="top" :auto-close="1000">
						<el-button icon="ele-Odometer" text type="warning" v-auth="'tmsZone:update'" @click="navigate('speed', row.id)"></el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
						<el-button icon="ele-CopyDocument" text type="info" v-auth="'tmsZone:add'" @click="handleCopy(row)"></el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
						<el-button icon="ele-Edit" text type="primary" v-auth="'tmsZone:update'" @click="handleEdit(row)"></el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger" v-auth="'tmsZone:delete'" @click="handleDelete(row)"></el-button>
					</el-tooltip>
				</template>
			</vxe-grid>
		</el-card>
		</div>

		<EditZone ref="editZoneRef" :title="state.title" :zone-data="state.zoneData" :operateType="state.operateType" @handleQuery="onSaved" />
	</div>
</template>

<script lang="ts" setup name="tmsZoneTreeTab">
// Author: KienLT
// Description: Tab "Danh sách khu vực" — lưới cây zone + tìm kiếm riêng (tách từ index.vue).
// Created date: 08/10/2026
//
// Tuyến (location) và Hướng (laneId) lọc ở FE (API GetTree chưa nhận 2 tham số này): giữ zone khớp và tổ tiên
// của nó để cây vẫn đúng phân cấp.
import { reactive, ref, defineAsyncComponent, inject } from 'vue';
import { useZoneTabNavigation } from '../utils/zoneTabNavigation';
import { ElMessageBox, ElMessage } from 'element-plus';
import { VxeGridInstance, VxeGridListeners } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { useI18n } from 'vue-i18n';
import EditZone from './editZone.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import { TmsZoneTypes } from '/@/types/enums/tmsEnum';
import { getAPI } from '/@/utils/axios-utils';
import { TmsZoneApi } from '/@/api-services/tms';
import { OperateType } from '/@/types/constants';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import { buildZoneTree, childrenMap, descendantIds, flattenZoneTree, type FlatZone } from '../utils/zoneTreeUtils';

const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;

const { t: $t } = useI18n();
const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));

interface Emits {
	(e: 'navigate', tab: 'mapping' | 'speed', zoneId: string): void;
	/** Có thêm/sửa/xoá zone ⇒ các tab khác cần nạp lại. */
	(e: 'changed'): void;
}
const emits = defineEmits<Emits>();
// Tab menu của khung tab chung: điều phối sang tab khác đi qua utils/zoneTabNavigation.
const tabNavigation = useZoneTabNavigation();
const navigate = (kind: 'mapping' | 'speed', zoneId: string) => {
	emits('navigate', kind, zoneId);
	tabNavigation.open(kind, zoneId);
};

const xGrid = ref<VxeGridInstance>();
const editZoneRef = ref<InstanceType<typeof EditZone>>();

const emptyQuery = () => ({
	name: undefined as string | undefined,
	code: undefined as string | undefined,
	type: undefined as any,
	status: undefined as any,
	location: undefined as string | undefined,
	laneId: undefined as string | undefined,
	fromKmNumber: undefined as any,
	fromMetNumber: undefined as any,
	toKmNumber: undefined as any,
	toMetNumber: undefined as any,
});

const state = reactive({
	/** Cây ĐẦY ĐỦ (không lọc) — cho cascader "khu vực cha" trong dialog sửa. */
	zoneData: [] as any[],
	queryParams: emptyQuery(),
	title: '',
	operateType: 'add' as OperateType,
	showBatchDelete: false,
});

const options = useVxeTable<TmsZoneApi>(
	{
		id: 'tmsZone',
		name: 'Zone Information',
		columns: [
			{ type: 'checkbox', width: 40, fixed: 'left' },
			{ field: 'code', title: $t('lz.entity.base.code'), minWidth: 180, showOverflow: 'tooltip', sortable: true },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 180, showOverflow: 'tooltip', treeNode: true, align: 'left', sortable: true, headerAlign: 'center' },
			{ field: 'type', title: $t('lz.entity.base.type'), minWidth: 100, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_type' } },
			{ field: 'location', title: $t('lz.entity.tms.location'), minWidth: 120, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_location' } },
			{ field: 'fromKmNumber', title: $t('lz.entity.tms.fromKmNumber'), minWidth: 120, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_from_location' } },
			{ field: 'toKmNumber', title: $t('lz.entity.tms.toKmNumber'), minWidth: 120, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_to_location' } },
			{ field: 'laneId', title: $t('lz.entity.tms.direction'), width: 100, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_lane' } },
			// Trống = dùng mặc định theo hướng (VdsConfig vds_route_{hướng}_min/max_speed).
			{ field: 'minSpeed', title: $t('lz.entity.tmsZone.minSpeed'), minWidth: 120, align: 'center', showOverflow: 'tooltip', sortable: true },
			{ field: 'maxSpeed', title: $t('lz.entity.tmsZone.maxSpeed'), minWidth: 120, align: 'center', showOverflow: 'tooltip', sortable: true },
			{ field: 'status', title: $t('lz.entity.base.status'), width: 110, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_status' } },
			{ field: '', title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 190, showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	{
		rowConfig: { keyField: 'id' },
		stripe: false,
		checkboxConfig: { range: false },
		proxyConfig: { autoLoad: true, ajax: { query: () => handleQueryApi() } },
		sortConfig: { remote: false, defaultSort: { field: 'orderNo', order: 'asc' } as any },
		pagerConfig: { enabled: false },
		toolbarConfig: { export: false },
		treeConfig: { expandAll: false, reserve: true },
	}
);

const serverFilterEmpty = () => {
	const q = state.queryParams;
	return [q.name, q.code, q.type, q.status, q.fromKmNumber, q.fromMetNumber, q.toKmNumber, q.toMetNumber].every(v => v == null || v === '');
};

/** Lọc Tuyến/Hướng ở FE trên cây trả về, giữ tổ tiên của zone khớp. */
const applyClientFilter = (tree: any[]) => {
	const { location, laneId } = state.queryParams;
	if (!location && !laneId) return tree;
	const flat = flattenZoneTree(tree);
	const match = new Set(flat.filter(z => (!location || z.location === location) && (!laneId || String(z.laneId ?? '') === laneId)).map(z => z.id));
	return buildZoneTree(flat, match);
};

const handleQueryApi = async () => {
	const p: any = { ...state.queryParams };
	Object.keys(p).forEach(key => { if (p[key] === null || p[key] === '') p[key] = undefined; });
	const response = await getAPI(TmsZoneApi).apiTmsTmszoneGettreeGet(p.name, p.code, p.type, p.fromKmNumber, p.fromMetNumber, p.toKmNumber, p.toMetNumber, p.status);
	const tree = (response.data.result as any[]) ?? [];
	if (serverFilterEmpty()) state.zoneData = tree;
	(response.data as any).result = applyClientFilter(tree);
	return response;
};

const handleQuery = async () => {
	options.loading = true;
	try {
		const response = await handleQueryApi();
		xGrid.value?.loadData((response.data.result as any) ?? []);
	} finally {
		options.loading = false;
	}
};

const resetQuery = async () => {
	state.queryParams = emptyQuery();
	await handleQuery();
};

const onSaved = async () => {
	await handleQuery();
	emits('changed');
	tabNavigation.markChanged();
};

const handleAdd = () => {
	state.title = $t('lz.label.base.titleAdd', { name: $t('lz.label.tms.zone') });
	state.operateType = 'add';
	editZoneRef.value?.openDialog({ type: TmsZoneTypes.Zone, isHide: false, isKeepAlive: true, isAffix: false, isIframe: false, status: 1, orderNo: 100 });
};

const handleCopy = (row: any) => {
	state.title = $t('lz.label.base.titleCopy', { name: $t('lz.label.tms.zone') });
	state.operateType = 'copy';
	editZoneRef.value?.openDialog(row);
};

const handleEdit = (row: any) => {
	state.title = $t('lz.label.base.titleEdit', { name: $t('lz.label.tms.zone') });
	state.operateType = 'edit';
	editZoneRef.value?.openDialog(row);
};

/**
 * Zone con cháu (ngoài các zone đang chọn) mà BE sẽ xoá theo. Đọc cây đầy đủ — không theo bộ lọc đang áp trên lưới,
 * vì zone con có thể đang bị lọc ẩn. Lỗi đọc cây ⇒ coi như không có con (BE vẫn xoá dây chuyền).
 */
const descendantsOf = async (ids: string[]): Promise<FlatZone[]> => {
	try {
		const response = await getAPI(TmsZoneApi).apiTmsTmszoneGettreeGet();
		const zones = flattenZoneTree((response.data.result ?? []) as any[]) as FlatZone[];
		const byId = new Map(zones.map(z => [z.id, z]));
		const children = childrenMap(zones);
		const selected = new Set(ids);
		const result = new Map<string, FlatZone>();
		ids.forEach(id => descendantIds(id, children).forEach(d => { if (!selected.has(d) && byId.has(d)) result.set(d, byId.get(d)!); }));
		return [...result.values()];
	} catch {
		return [];
	}
};

/** Tên vài zone con cho hộp xác nhận (tối đa 5, còn lại "…"). */
const zoneNamesText = (zones: FlatZone[]) => zones.slice(0, 5).map(z => z.name || z.code).join(', ') + (zones.length > 5 ? ', …' : '');

const handleDelete = async (row: any) => {
	// Xoá zone cha ⇒ BE xoá luôn mọi zone con cháu: báo rõ số lượng trước khi xác nhận.
	const children = await descendantsOf([row.id]);
	const message = children.length
		? $t('lz.message.tmsZone.confirmDeleteWithChildren', { name: row.name, count: children.length, names: zoneNamesText(children) })
		: $t('lz.message.base.confirmDelete', { name: row.name });
	ElMessageBox.confirm(message, $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(TmsZoneApi).apiTmsTmszoneDeletePost({ id: row.id });
			ElMessage.success($t('lz.message.base.deleteSuccess'));
			await onSaved();
		})
		.catch(() => {});
};

const getCheckedRecords = () => xGrid.value?.getCheckboxRecords() ?? [];

const handleBatchDelete = async () => {
	const selectRecords = getCheckedRecords();
	const children = await descendantsOf(selectRecords.map((l: any) => l.id).filter(Boolean));
	const message = children.length
		? $t('lz.message.tmsZone.confirmBatchDeleteWithChildren', { total: selectRecords.length, count: children.length, names: zoneNamesText(children) })
		: $t('lz.message.base.confirmBatchDelete', { total: selectRecords.length });
	ElMessageBox.confirm(message, $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(TmsZoneApi).apiTmsTmszoneBatchdeletePost(selectRecords.filter((l: any) => l.id));
			ElMessage.success($t('lz.message.base.deleteSuccess'));
			await onSaved();
		})
		.catch(() => {});
};

const handleCustom = () => xGrid.value?.openCustom();
const selectChangeEvent = () => { state.showBatchDelete = getCheckedRecords().length > 0; };

const gridEvents: VxeGridListeners<TmsZoneApi> = {
	async proxyQuery() {
		if (serverFilterEmpty() && !state.queryParams.location && !state.queryParams.laneId)
			state.zoneData = (xGrid.value?.getTableData().tableData as any[]) ?? state.zoneData;
	},
};

const handleExpand = () => xGrid.value?.setAllTreeExpand(true);
const handleFold = () => xGrid.value?.clearTreeExpand();

defineExpose({ reload: handleQuery });
</script>

<style scoped lang="scss">
@import './zoneTab.scss';
</style>
