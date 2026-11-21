<template>
	<WorkspacePage>
	<div class="current-content-container">
		<!-- SearchBar: begin -->
		<GridSearchPanel id="vms-current-content-search">
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
				<SearchButton authSearch="vmsCurrentContent:page" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
			</el-row>
				</template>
		<!-- SearchBar: end -->

		<!-- GridTable: begin -->
		<el-card class="full-table" shadow="hover">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
				<template #toolbar_buttons>
					<GridSummary :summary="summary" />
				</template>
				<template #toolbar_tools>
					<TableHeader :show-add="false" :show-delete="false" @refresh="handleQuery" @custom-column="handleCustom" />
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>

				<!-- IP: merge từ danh sách thiết bị VMS -->
				<template #row_ip="{ row }">
					{{ state.equipmentMap[row.equipmentId]?.ip || '-' }}
				</template>

				<!-- Ảnh hiện tại của thiết bị (cột Url). Tự đổi realtime khi nhận NATS. -->
				<template #row_currentPreview="{ row }">
					<div class="panel-img">
						<el-image :src="buildUrl(row.url)" :preview-src-list="[buildUrl(row.url)]" :preview-teleported="true" fit="contain" hide-on-click-modal>
							<template #error><img :src="noImageUrl" /></template>
						</el-image>
					</div>
				</template>
			</vxe-grid>
		</el-card>
		</GridSearchPanel>
		<!-- GridTable: end -->
	</div>
	</WorkspacePage>
</template>

<script lang="ts" setup name="vmsCurrentContent">
import { onMounted, onActivated, reactive, ref, defineAsyncComponent } from 'vue';
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

// Hook NATS dùng chung VMS/LCS: nghe cập nhật nội dung realtime
import { useTransporterVms, getVmsEquipmentId, normalizeVmsRecord } from '/@/transporter/hooks/useTransporterVms';
import { TmsEquipmentType } from '/@/types/enums';

const { t: $t } = useI18n();
const mapStore = useMapStore();

const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));

const xGrid = ref<VxeGridInstance>();
const { summary, track } = useGridSummary();

const noImageUrl = `${import.meta.env.BASE_URL}Upload/noimage.jpg`;
// Ghép URL ảnh theo cách chung của VMS; rỗng -> noimage
const buildUrl = (u?: string) => (u ? `${mapStore.baseUrl}/${u}` : noImageUrl);

// Code loại thiết bị được phép hiển thị ở trang này
const ALLOWED_TYPE_CODES = [TmsEquipmentType.Vms, TmsEquipmentType.LCS];

const state = reactive({
	queryParams: { code: undefined as any, name: undefined as any, equipmentTypeId: undefined as any },
	localPageParam: {
		pageSize: 50 as number,
		defaultSort: { field: 'createTime', order: 'desc', descStr: 'desc' } as any,
	},
	typeList: [] as any[], // danh sách loại thiết bị VMS/LCS (từ TmsEquipmentType)
	equipmentMap: {} as Record<string, any>, // equipmentId -> { ip, equipmentTypeId, ... }
});

const options = useVxeTable<PageVmsDefaultCurrentOutput>(
	{
		id: 'vmsCurrentContentGrid',
		name: 'vmsCurrentContentGrid',
		columns: [
			{ field: 'code', title: $t('lz.entity.base.code'), minWidth: 140, showOverflow: 'tooltip', sortable: true },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 180, showOverflow: 'tooltip', sortable: true },
			{ field: 'ip', title: $t('lz.entity.vms.ip'), width: 160, showOverflow: 'tooltip', slots: { default: 'row_ip' } },
			{ field: 'url', title: $t('lz.label.base.preview'), minWidth: 240, slots: { default: 'row_currentPreview' } },
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

onMounted(async () => {
	// Đảm bảo có baseUrl để ghép URL ảnh preview
	if (!mapStore.baseUrl) {
		try {
			await mapStore.fetchSystemSysconfigFileserverGet();
		} catch (e) {
			console.error('Error fetching file server:', e);
		}
	}

	// 1) Nạp danh sách loại thiết bị, chỉ giữ VMS/LCS còn hiệu lực (IsDelete == null)
	try {
		const resType = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet();
		state.typeList = (resType.data?.result ?? []).filter((t: any) => t.isDelete == null && ALLOWED_TYPE_CODES.includes(t.code));
	} catch (e) {
		console.error('Error loading equipment type list:', e);
	}

	// 2) Nạp thiết bị thuộc các loại trên -> equipmentMap (chỉ thiết bị isDelete == null)
	//    Lưu ý: API tra theo CODE của loại thiết bị nên phải truyền t.code (không phải t.id).
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

// Query API (tái sử dụng endpoint PageDefaultContent — trả về full record VmsCurrent gồm cả url)
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
};

const resetQuery = async () => {
	state.queryParams.code = undefined;
	state.queryParams.name = undefined;
	state.queryParams.equipmentTypeId = undefined;
	await xGrid.value?.commitProxy('reload');
};

function handleCustom() {
	xGrid.value?.openCustom();
}

// Realtime: khi nhận cập nhật VMS/LCS từ NATS -> đổi ảnh của đúng dòng đang hiển thị
useTransporterVms((payload) => {
	const id = getVmsEquipmentId(payload.data);
	if (!id) return;

	const rows = (xGrid.value?.getData() as any[]) ?? [];
	const row = rows.find((r: any) => String(r.equipmentId) === id);
	if (!row) return; // thiết bị không nằm trong trang/bộ lọc hiện tại -> bỏ qua

	const rec = normalizeVmsRecord(payload.data);
	if (rec.url !== undefined) row.url = rec.url; // cập nhật tại chỗ -> el-image đổi ảnh ngay
	xGrid.value?.updateData();
});

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
