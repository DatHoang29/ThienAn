<template>
	<!-- Danh sách kịch bản (mọi bố cục): tra cứu, bật / tắt, đặt mặc định, nhân bản, xoá; mở chỉnh sửa ở tab Kịch bản hiển thị -->
	<div class="sml">
		<GridSearchPanel id="videowall-scene-list-search">
			<template #filters>
			<el-form :model="state.queryParams" :show-message="false" :inline-message="true" label-width="auto" style="flex: 1 1 0%" label-position="left">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.videoWall.codeOrName')">
							<el-input v-model="state.queryParams.name" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.label.videoWall.codeOrName') })" clearable
								@keyup.enter="handleQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<!-- Mặc định bố cục đang dùng; chọn "Tất cả bố cục" để xem cả kịch bản của bố cục khác -->
						<el-form-item :label="$t('lz.entity.videoWall.layout')">
							<el-select v-model="state.queryParams.wallTopologyId" filterable class="w100" @change="handleQuery">
								<el-option :label="$t('lz.label.videoWall.allLayouts')" :value="ALL_LAYOUTS" />
								<el-option v-for="l in state.layouts" :key="l.id!" :label="`${l.code} — ${l.name}`" :value="l.id!">
									<span>{{ `${l.code} — ${l.name}` }}</span>
									<el-tag v-if="l.id === currentId" size="small" type="success" class="ml5">{{ $t('lz.label.videoWall.currentLayout') }}</el-tag>
								</el-option>
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.status')">
							<el-select v-model="state.queryParams.status" clearable class="w100" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.base.status') })"
								@change="handleQuery">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code" :label="item.name" :value="parseInt(item.code)" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />
			<el-row>
				<SearchButton :auth-search="'videoWallScene:page'" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
			</el-row>
			</template>

		<el-card class="full-table" shadow="hover">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" @checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">
				<template #toolbar_tools>
					<!-- Thêm mới ⇒ sang tab Kịch bản hiển thị với kịch bản mới (giống nút Sửa) -->
					<TableHeader :auth-add="'videoWallScene:add'" @add="emit('create')" :show-delete="state.showBatchDelete" :auth-delete="'videoWallScene:delete'" @refresh="handleQuery"
						@custom-column="xGrid?.openCustom()" @delete="emit('batch-delete', getCheckedRecords())" />
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_name="{ row }">
					<span class="sml-name">
						<el-icon v-if="row.isDefault === DefaultEnum.NUMBER_1" class="sml-star" :title="$t('lz.label.base.isDefault')"><ele-StarFilled /></el-icon>
						<span>{{ row.name }}</span>
					</span>
				</template>
				<template #row_layout="{ row }">
					<span class="sml-layout">
						<span class="sml-layout__name">{{ layoutLabel(row.wallTopologyId) }}</span>
						<el-tag v-if="row.wallTopologyId === currentId" size="small" type="success" effect="light" round>{{ $t('lz.label.videoWall.currentLayout') }}</el-tag>
						<el-tag v-else size="small" type="info" effect="plain" round>{{ $t('lz.label.videoWall.oldLayout') }}</el-tag>
					</span>
				</template>
				<template #row_windows="{ row }">
					{{ state.windowCounts[row.id] ?? 0 }}
				</template>
				<template #row_serving="{ row }">
					<span v-if="row.id === state.servingId" class="sml-live"><i />{{ $t('lz.label.videoWall.sceneServing') }}</span>
					<span v-else class="sml-muted">—</span>
				</template>
				<template #row_status="{ row }">
					<!-- Kịch bản bố cục khác chỉ tắt được, không bật lại (BE chặn: toạ độ cửa sổ không khớp tường) -->
					<el-switch :model-value="row.status === StatusEnum.NUMBER_1" :loading="state.toggling === row.id" v-auth="'videoWallScene:update'"
						:disabled="row.status !== StatusEnum.NUMBER_1 && row.wallTopologyId !== currentId" @change="(on: any) => toggleStatus(row, !!on)" />
				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<!-- Thứ tự / màu nút giống các trang quản lý khác: thao tác riêng (xanh lá) · Sao chép (cam) · Sửa (xanh dương) · Xoá (đỏ) -->
				<template #row_buttons="{ row }">
					<el-tooltip v-if="canSetDefault" :content="$t('lz.button.videoWall.setDefault')" placement="top" :auto-close="1000">
						<el-button icon="ele-Star" text type="success" v-auth="'videoWallScene:update'"
							:disabled="row.isDefault === DefaultEnum.NUMBER_1 || row.wallTopologyId !== currentId" @click="emit('set-default', row)" />
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
						<el-button icon="ele-CopyDocument" text type="warning" v-auth="'videoWallScene:add'" @click="emit('duplicate', row)" />
					</el-tooltip>
					<el-tooltip :content="$t('lz.label.videoWall.openInEditor')" placement="top" :auto-close="1000">
						<el-button icon="ele-Edit" text type="primary" @click="emit('open', row)" />
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger" v-auth="'videoWallScene:delete'" @click="emit('delete', row)" />
					</el-tooltip>
				</template>
			</vxe-grid>
		</el-card>
		</GridSearchPanel>
	</div>
</template>

<script lang="ts" setup name="videoWallSceneManageList">
import { computed, defineAsyncComponent, onMounted, reactive, ref } from 'vue';
import GridSearchPanel from '/@/components/gridPage/GridSearchPanel.vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { useI18n } from 'vue-i18n';
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { getAPI } from '/@/utils/axios-utils';
import { DefaultEnum, StatusEnum, VwSceneApi, VwWallTopologyApi, VwWindowSceneApi } from '/@/api-services/videoWall';
import type { VwPageSceneInput, VwPageSceneOutput, VwWallTopologyOutput } from '/@/api-services/videoWall';
import { fetchActiveScene } from '/@/utils/videoWall/sceneStart';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';

const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const { t: $t } = useI18n();

interface Props {
	/** ID bố cục đang dùng — lọc mặc định, nhãn "Dùng" / "Bố cục cũ". */
	currentTopologyId?: string | null;
	/** Hiện nút "Đặt mặc định" — chỉ SuperAdmin. */
	canSetDefault?: boolean;
}
const props = withDefaults(defineProps<Props>(), { currentTopologyId: null, canSetDefault: false });
const emit = defineEmits<{
	(e: 'open', scene: VwPageSceneOutput): void;
	/** Thêm kịch bản mới ⇒ trang cha sang tab soạn. */
	(e: 'create'): void;
	(e: 'duplicate', scene: VwPageSceneOutput): void;
	(e: 'set-default', scene: VwPageSceneOutput): void;
	(e: 'delete', scene: VwPageSceneOutput): void;
	/** Xoá nhiều kịch bản đang tick chọn. */
	(e: 'batch-delete', scenes: VwPageSceneOutput[]): void;
	/** Bật / tắt kịch bản ⇒ trang cha nạp lại danh sách của ô chọn kịch bản. */
	(e: 'changed'): void;
}>();

/** Số cửa sổ tối đa nạp một lần để đếm cửa sổ theo kịch bản. */
const WINDOW_PAGE_ALL = 5000;
/** Giá trị lọc "Tất cả bố cục" — chuỗi rỗng bị el-select coi là chưa chọn. */
const ALL_LAYOUTS = '__all__';

const xGrid = ref<VxeGridInstance>();
const state = reactive({
	queryParams: {
		name: '',
		/** Gán sau khi nạp danh sách bố cục (mặc định bố cục đang dùng). */
		wallTopologyId: '',
		status: undefined as StatusEnum | undefined,
	},
	/** Đang tick chọn ít nhất 1 dòng ⇒ hiện nút Xoá nhiều. */
	showBatchDelete: false,
	layouts: [] as VwWallTopologyOutput[],
	/** Số cửa sổ theo ID kịch bản. */
	windowCounts: {} as Record<string, number>,
	/** Kịch bản BE đang phát lên tường. */
	servingId: '',
	/** Kịch bản đang bật / tắt (hiện loading trên công tắc). */
	toggling: '',
});

const options = useVxeTable<VwPageSceneOutput>(
	{
		id: 'videoWallSceneManage',
		name: 'VideoWallSceneManage',
		columns: [
			{ type: 'checkbox', fixed: 'left', width: 45 },
			// Cột ngắn cố định độ rộng; chỉ Tên + Bố cục giãn theo màn hình
			{ field: 'code', title: $t('lz.entity.base.code'), width: 160, showOverflow: 'tooltip', sortable: true },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 200, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_name' } },
			{ field: 'wallTopologyId', title: $t('lz.entity.videoWall.layout'), minWidth: 280, showOverflow: 'tooltip', slots: { default: 'row_layout' } },
			{ field: 'windows', title: $t('lz.label.videoWall.sourceTotal'), width: 100, align: 'center', slots: { default: 'row_windows' } },
			{ field: 'activeScene', title: $t('lz.label.videoWall.sceneServing'), width: 120, align: 'center', slots: { default: 'row_serving' } },
			{ field: 'status', title: $t('lz.entity.base.status'), width: 100, align: 'center', sortable: true, slots: { default: 'row_status' } },
			{ field: '', title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 170, showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	{
		// Khung đổi kích thước (thu / mở menu, đổi tab) ⇒ tính lại độ rộng cột, cột Hành động luôn sát mép phải
		autoResize: true,
		// Nạp sau khi có danh sách bố cục (lọc mặc định theo bố cục đang dùng)
		proxyConfig: { autoLoad: false, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		sortConfig: { defaultSort: { field: 'orderNo', order: 'asc' } },
		pagerConfig: { pageSize: 50 },
		toolbarConfig: { export: false },
		rowConfig: { keyField: 'id', height: 56 },
	}
);

/** Bố cục đang dùng: theo trạng thái Dùng trong danh sách bố cục (đọc thẳng DB), chưa nạp thì theo trang cha. */
const currentId = computed(() => state.layouts.find((l) => l.status === StatusEnum.NUMBER_1)?.id ?? props.currentTopologyId ?? '');

onMounted(async () => {
	try {
		const res = await getAPI(VwWallTopologyApi).apiVideowallVwwalltopologyListGet();
		state.layouts = res.data.result ?? [];
	} finally {
		state.queryParams.wallTopologyId = currentId.value || ALL_LAYOUTS;
		await handleQuery();
	}
});

const layoutLabel = (id?: string | null) => {
	const l = state.layouts.find((x) => x.id === id);
	return l ? `${l.code} — ${l.name}` : '—';
};

/** Nạp trang kịch bản + đếm cửa sổ + kịch bản đang phát (song song). */
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params: VwPageSceneInput = {
		name: state.queryParams.name.trim() || undefined,
		wallTopologyId: state.queryParams.wallTopologyId && state.queryParams.wallTopologyId !== ALL_LAYOUTS ? state.queryParams.wallTopologyId : undefined,
		status: state.queryParams.status,
		page: page.currentPage,
		pageSize: page.pageSize,
		field: sort?.field || 'orderNo',
		order: sort?.order || 'asc',
		descStr: 'desc',
	};
	const [res, windowRes, active] = await Promise.all([
		getAPI(VwSceneApi).apiVideowallVwscenePagePost(params),
		getAPI(VwWindowSceneApi)
			.apiVideowallVwwindowscenePagePost({ page: 1, pageSize: WINDOW_PAGE_ALL })
			.catch(() => null),
		fetchActiveScene(),
	]);
	const counts: Record<string, number> = {};
	for (const w of windowRes?.data.result?.records ?? []) if (w.sceneId) counts[w.sceneId] = (counts[w.sceneId] ?? 0) + 1;
	state.windowCounts = counts;
	state.servingId = active?.id ?? '';
	return res;
};

const handleQuery = async () => {
	options.loading = true;
	await xGrid.value?.commitProxy('reload');
	options.loading = false;
	selectChangeEvent();
};

function getCheckedRecords() {
	return (xGrid.value?.getCheckboxRecords() as VwPageSceneOutput[]) ?? [];
}

function selectChangeEvent() {
	state.showBatchDelete = getCheckedRecords().length > 0;
}

const resetQuery = async () => {
	state.queryParams.name = '';
	state.queryParams.wallTopologyId = currentId.value || ALL_LAYOUTS;
	state.queryParams.status = undefined;
	await handleQuery();
};

/** Bật / tắt kịch bản (hỏi trước). Tắt ⇒ không hiện ở Giám sát & Điều khiển, lập lịch / quy tắc sự kiện không kích hoạt được. */
const toggleStatus = async (row: VwPageSceneOutput, on: boolean) => {
	try {
		await ElMessageBox.confirm(
			$t(on ? 'lz.message.videoWall.confirmEnableScene' : 'lz.message.videoWall.confirmDisableScene', { name: row.name }),
			$t('lz.label.base.warning'),
			{ confirmButtonText: $t('lz.button.base.confirm'), cancelButtonText: $t('lz.button.base.cancel'), type: 'warning' }
		);
	} catch {
		return;
	}
	state.toggling = row.id ?? '';
	try {
		const status = on ? StatusEnum.NUMBER_1 : StatusEnum.NUMBER_0;
		await getAPI(VwSceneApi).apiVideowallVwsceneUpdatePost({ ...row, status });
		row.status = status;
		ElMessage.success($t('lz.message.base.saveSuccess'));
		emit('changed');
	} finally {
		state.toggling = '';
	}
};

defineExpose({
	/** Nạp lại bảng (sau khi thêm / sửa / xoá ở tab Kịch bản hiển thị). */
	reload: handleQuery,
});
</script>

<style scoped lang="scss">
.sml {
	height: 100%;
	display: flex;
	flex-direction: column;
}
.sml-name {
	display: inline-flex;
	align-items: center;
	gap: 6px;
	min-width: 0;
}
.sml-star {
	flex-shrink: 0;
	color: var(--el-color-warning);
}
.sml-layout {
	display: inline-flex;
	align-items: center;
	gap: 6px;
	min-width: 0;
	&__name {
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}
}
.sml-live {
	display: inline-flex;
	align-items: center;
	gap: 6px;
	padding: 2px 10px;
	border-radius: 999px;
	font-size: 12px;
	font-weight: 600;
	color: #fff;
	background: var(--el-color-success);
	i {
		width: 6px;
		height: 6px;
		border-radius: 50%;
		background: #fff;
	}
}
.sml-muted {
	color: var(--el-text-color-placeholder);
}
</style>
