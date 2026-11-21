<template>
	<WorkspacePage>
	<div class="default-content-container">
		<!-- SearchBar: begin -->
		<GridSearchPanel id="vms-default-content-search">
				<template #filters>
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%" label-position="left">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="24" :md="8" :lg="8" :xl="8">
						<el-form-item :label="$t('lz.entity.base.code')" prop="code">
							<el-input v-model="state.queryParams.code" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.code') })" @keyup.enter.native="handleQuery" :maxlength="EntityFieldLength.Length64" show-word-limit clearable class="w100" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="24" :md="8" :lg="8" :xl="8">
						<el-form-item :label="$t('lz.entity.base.name')" prop="name">
							<el-input v-model="state.queryParams.name" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.name') })" @keyup.enter.native="handleQuery" :maxlength="EntityFieldLength.Length64" show-word-limit clearable class="w100" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="24" :md="8" :lg="8" :xl="8">
						<el-form-item :label="$t('lz.entity.tms.equipmentType')" prop="equipmentTypeId">
							<el-select v-model="state.queryParams.equipmentTypeId" filterable clearable class="w100"
								:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.equipmentType') })">
								<el-option v-for="item in state.typeList" :key="item.id" :label="item.name" :value="item.id" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />
			<el-row>
				<SearchButton authSearch="vmsDefaultContent:page" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
			</el-row>
				</template>
		<!-- SearchBar: end -->

		<!-- GridTable: begin -->
		<el-card class="full-table" shadow="hover">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents" @checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">
				<template #toolbar_buttons>
					<GridSummary :summary="summary" />
				</template>
				<template #toolbar_tools>
					<TableHeader :show-add="false" :show-delete="state.showBatchDelete" :auth-delete="'vmsDefaultContent:delete'" @refresh="handleQuery" @custom-column="handleCustom" @delete="handleBatchDelete" />
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>

				<!-- IP: merge từ danh sách thiết bị VMS -->
				<template #row_ip="{ row }">
					{{ state.equipmentMap[row.equipmentId]?.ip || '-' }}
				</template>

				<!-- Nội dung mặc định (parse JSON DefaultRowData) -->
				<template #row_defaultContent="{ row }">
					<template v-if="row.defaultRowData">
						<div>{{ parseDefaultRowData(row.defaultRowData).row1 }}</div>
						<div>{{ parseDefaultRowData(row.defaultRowData).row2 }}</div>
					</template>
					<span v-else>-</span>
				</template>

				<!-- Ảnh preview nội dung mặc định (dùng URL chung của VMS) -->
				<template #row_defaultPreview="{ row }">
					<div class="panel-img">
						<el-image :src="buildUrl(row.defaultUrl)" :preview-src-list="[buildUrl(row.defaultUrl)]" :preview-teleported="true" fit="contain" hide-on-click-modal>
							<template #error><img :src="noImageUrl" /></template>
						</el-image>
					</div>
				</template>

				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
						<el-button icon="ele-Edit" text type="primary" v-auth="'vmsDefaultContent:update'" @click="handleEdit(row)" />
					</el-tooltip>
					<!-- <el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger" v-auth="'vmsDefaultContent:delete'" @click="handleDelete(row)" />
					</el-tooltip> -->
				</template>
			</vxe-grid>
		</el-card>
		</GridSearchPanel>
		<!-- GridTable: end -->

		<!-- Dialog thêm/sửa nội dung mặc định -->
		<editDefaultContent ref="editRef" :equipment-map="state.equipmentMap" :equipment-type-list="state.typeList" @handleQuery="handleQuery" />
	</div>
	</WorkspacePage>
</template>

<script lang="ts" setup name="vmsDefaultContent">
import { onMounted, onActivated, reactive, ref, defineAsyncComponent } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import WorkspacePage from '/@/components/workspace/WorkspacePage.vue';
import GridSearchPanel from '/@/components/gridPage/GridSearchPanel.vue';
import GridSummary from '/@/components/gridPage/GridSummary.vue';
import { useGridSummary } from '/@/components/gridPage/useGridSummary';
import { useGridPager } from '/@/components/gridPage/useGridPager';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { getAPI } from '/@/utils/axios-utils';
import { VmsCurrentApi } from '/@/api-services/vms';
import { PageVmsDefaultCurrentInput, PageVmsDefaultCurrentOutput } from '/@/api-services/vms';
import { TmsEquipmentApi, TmsEquipmentTypeApi } from '/@/api-services/tms';
import { EntityFieldLength } from '/@/types/enums/baseEnum';
import { useMapStore } from '/@/stores/its/mapStore';

import editDefaultContent from '/@/views/vms/defaultContent/component/editDefaultContent.vue';
import { TmsEquipmentType } from '/@/types/enums';

const { t: $t } = useI18n();
const mapStore = useMapStore();

const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));

const xGrid = ref<VxeGridInstance>();
const { summary, track } = useGridSummary();
const editRef = ref<InstanceType<typeof editDefaultContent>>();

const noImageUrl = `${import.meta.env.BASE_URL}Upload/noimage.jpg`;
// Ghép URL ảnh theo cách chung của VMS; rỗng -> noimage
const buildUrl = (u?: string) => (u ? `${mapStore.baseUrl}/${u}` : noImageUrl);

// Parse DefaultRowData (chịu được PascalCase/camelCase), null -> ''
const parseDefaultRowData = (raw: any) => {
	try {
		const obj = typeof raw === 'string' ? JSON.parse(raw) : raw || {};
		return { row1: obj?.Row1 ?? obj?.row1 ?? '', row2: obj?.Row2 ?? obj?.row2 ?? '' };
	} catch {
		return { row1: '', row2: '' };
	}
};

// Code loại thiết bị được phép hiển thị ở trang này
const ALLOWED_TYPE_CODES = [TmsEquipmentType.Vms, TmsEquipmentType.LCS];

const state = reactive({
	queryParams: { code: undefined as any, name: undefined as any, equipmentTypeId: undefined as any },
	localPageParam: {
		pageSize: 50 as number,
		defaultSort: { field: 'createTime', order: 'desc', descStr: 'desc' } as any,
	},
	showBatchDelete: false,
	typeList: [] as any[], // danh sách loại thiết bị VMS/LCS (từ TmsEquipmentType)
	equipmentMap: {} as Record<string, any>, // equipmentId -> { ip, kmNumber, metNumber, width, height }
});

const options = useVxeTable<PageVmsDefaultCurrentOutput>(
	{
		id: 'vmsDefaultContentGrid',
		name: 'vmsDefaultContentGrid',
		columns: [
			{ type: 'checkbox', fixed: 'left', width: 45 },
			{ field: 'code', title: $t('lz.entity.base.code'), minWidth: 140, showOverflow: 'tooltip', sortable: true },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 180, showOverflow: 'tooltip', sortable: true },
			{ field: 'ip', title: $t('lz.entity.vms.ip'), width: 150, showOverflow: 'tooltip', slots: { default: 'row_ip' } },
			{ field: 'defaultRowData', title: $t('lz.entity.vmsCurrent.defaultRowData'), minWidth: 200, showOverflow: 'tooltip', slots: { default: 'row_defaultContent' } },
			{ field: 'defaultUrl', title: $t('lz.label.base.preview'), minWidth: 200, slots: { default: 'row_defaultPreview' } },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 110, slots: { default: 'row_buttons' } },
		],
	},
	{
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		sortConfig: { defaultSort: state.localPageParam.defaultSort },
		pagerConfig: { pageSize: state.localPageParam.pageSize },
		toolbarConfig: { export: false },
		rowConfig: { height: 120 },
	}
);

// Thu gọn thanh phân trang theo chiều rộng bảng.
useGridPager(xGrid, options);

// Quay lại tab/menu (KeepAlive): tải lại trang hiện tại để nhận thay đổi từ tab khác.
// onActivated cũng chạy ngay sau lần mount đầu, khi đó dữ liệu vừa tải nên bỏ qua.
let activated = false;
onActivated(async () => {
	if (!activated) {
		activated = true;
		return;
	}
	await xGrid.value?.commitProxy('query');
});

// Hàm này dùng để lấy danh sách thiết bị VMS 1 lần -> map equipmentId để hiển thị IP + lấy size cho preview
onMounted(async () => {
	// Đảm bảo có baseUrl để ghép URL ảnh preview
	if (!mapStore.baseUrl) {
		try {
			await mapStore.fetchSystemSysconfigFileserverGet();
		} catch (e) {
			console.error('Error fetching file server:', e);
		}
	}

	// Nạp danh sách thiết bị VMS + LCS -> map equipmentId để hiển thị IP + lấy size cho preview
	// 1) Nạp danh sách loại thiết bị, chỉ giữ VMS/LCS còn hiệu lực (IsDelete == null)
		try {
			const resType = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet();
			state.typeList = (resType.data?.result ?? []).filter((t: any) => t.isDelete == null && ALLOWED_TYPE_CODES.includes(t.code));
		} catch (e) {
			console.error('Error loading equipment type list:', e);
		}

		// 2) Nạp thiết bị thuộc các loại trên -> equipmentMap (chỉ thiết bị isDelete == null)
		try {
			const results = await Promise.all(
				state.typeList.map((t: any) => getAPI(TmsEquipmentApi).apiTmsTmsequipmentGetequipmenttypebyequipmentidVmstypeGet(t.code))
			);
			results.forEach((res: any) => {
				(res.data?.result ?? [])
					.filter((e: any) => e.isDelete == null)
					.forEach((e: any) => (state.equipmentMap[e.id] = e));
			});
		} catch (e) {
			console.error('Error loading equipment list:', e);
		}

	await handleQuery();
});

// Query API
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	// Loại thiết bị -> danh sách equipmentId; '-1' để grid rỗng khi loại đó chưa có thiết bị nào
	let equipmentIds: string[] | undefined = undefined;
	if (state.queryParams.equipmentTypeId) {
		equipmentIds = Object.values(state.equipmentMap)
			.filter((e: any) => e.equipmentTypeId === state.queryParams.equipmentTypeId)
			.map((e: any) => e.id);
		if (equipmentIds.length === 0) equipmentIds = ['-1'];
	}

	const params = {
		code: state.queryParams.code,
		name: state.queryParams.name,
		equipmentIds,
		page: page.currentPage,
		pageSize: page.pageSize,
		field: sort.field,
		order: sort.order,
		descStr: 'desc',
	} as PageVmsDefaultCurrentInput;

	return track(getAPI(VmsCurrentApi).apiVmsVmscurrentPagedefaultcontentPost(params));
};

const handleQuery = async () => {
	options.loading = true;
	await xGrid.value?.commitProxy('reload');
	options.loading = false;
	selectChangeEvent();
};

const resetQuery = async () => {
	state.queryParams.code = undefined;
	state.queryParams.name = undefined;
	state.queryParams.equipmentTypeId = undefined;
	await xGrid.value?.commitProxy('reload');
};

// Sửa nội dung mặc định
const handleEdit = (row: any) => {
	editRef.value?.openDialog(row);
};

// Xóa (clear) nội dung mặc định
const handleDelete = (row: any) => {
	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.name }), $t('lz.label.base.confirm'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			try {
				await getAPI(VmsCurrentApi).apiVmsVmscurrentDeletedefaultcontentPost({ id: row.id });
				handleQuery();
				ElMessage.success($t('lz.message.base.processSuccess'));
			} catch (error) {
				ElMessage.error($t('lz.message.base.processUnSuccess'));
			}
		})
		.catch(() => {});
};

function handleCustom() {
	xGrid.value?.openCustom();
}

function getCheckedRecords() {
	return xGrid.value?.getCheckboxRecords() ?? [];
}

function selectChangeEvent() {
	state.showBatchDelete = getCheckedRecords().length > 0;
}

// Xóa hàng loạt (lặp từng dòng vì BE xóa theo 1 ID)
const handleBatchDelete = async () => {
	const selectRecords = getCheckedRecords(); // lấy danh sách các dòng đã check

	// lấy danh sách tên các thiết bị đã chọn xóa
	const equipmentName = selectRecords.map((l: any) => l.name).join(', ');

	ElMessageBox.confirm($t('lz.message.base.confirmBatchDelete', { names: equipmentName }), $t('lz.label.base.confirm'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			try {
				await Promise.all(selectRecords.filter((l: any) => l.id).map((l: any) => getAPI(VmsCurrentApi).apiVmsVmscurrentDeletedefaultcontentPost({ id: l.id })));
				handleQuery();
				ElMessage.success($t('lz.message.base.processSuccess'));
			} catch (error) {
				ElMessage.error($t('lz.message.base.processUnSuccess'));
			}
		})
		.catch(() => {});
};

// Hàm này dùng để lấy danh sách thiết bị VMS 1 lần -> map equipmentId để hiển thị IP + lấy size cho preview
const gridEvents: VxeGridListeners<PageVmsDefaultCurrentOutput> = {
	async pageChange({ pageSize }) {
		state.localPageParam.pageSize = pageSize;
	},
	async sortChange({ field, order }) {
		state.localPageParam.defaultSort = { field: field, order: order! };
	},
};
</script>

<style scoped>
.panel-img :deep(img) {
	max-width: 100%;
	max-height: 110px;
	padding: 6px;
}
</style>
