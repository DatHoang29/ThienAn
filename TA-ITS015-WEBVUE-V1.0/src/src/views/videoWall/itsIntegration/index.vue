<template>
	<WorkspacePage>
	<div class="videowall-its-container">
		<GridSearchPanel id="videowall-its-integration-search">
			<template #filters>
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true"
				label-width="auto" style="flex: 1 1 0%" label-position="left">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.videoWall.eventType')" prop="eventTypeId">
							<el-select v-model="state.queryParams.eventTypeId" :placeholder="$t('lz.label.videoWall.selectEventType')" filterable clearable class="w100">
								<el-option v-for="evt in state.eventTypes" :key="evt.id!" :label="evt.name!" :value="evt.id!" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.videoWall.eventSource')" prop="eventSource">
							<el-select v-model="state.queryParams.eventSource" :placeholder="$t('lz.label.videoWall.allSources')" clearable class="w100">
								<el-option v-for="src in getConfigDataByCode(BaseConfigTypeEnum.VideoWallEventSource)" :key="src.code" :label="src.name" :value="src.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.priority')" prop="priority">
							<el-select v-model="state.queryParams.priority" :placeholder="$t('lz.label.base.all')" clearable class="w100">
								<el-option v-for="p in getConfigDataByCode(BaseConfigTypeEnum.VideoWallEventPriority)" :key="p.code" :label="p.name" :value="p.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.status')" prop="status">
							<el-select v-model="state.queryParams.status" filterable clearable class="w100" @clear="state.queryParams.status = undefined"
								:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.base.status') })">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code" :label="item.name" :value="parseInt(item.code)" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>

			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />

			<el-row>
				<SearchButton @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
			</el-row>
			</template>

		<el-card class="full-table" shadow="hover">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents"
				@checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">
				<template #toolbar_buttons>
					<GridSummary :summary="summary" />
				</template>
				<template #toolbar_tools>
					<TableHeader @add="handleAdd" :show-delete="state.showBatchDelete" :auth-add="'videoWallItsIntegration:add'"
						:auth-delete="'videoWallItsIntegration:delete'" @refresh="handleQuery" @custom-column="handleCustom"
						@delete="handleBatchDelete">
					</TableHeader>
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<!-- Bản ghi lưu id loại sự kiện, hiển thị tên tra từ danh mục TMS -->
				<template #row_event_type="{ row }">
					<el-tag :type="eventTagType(row.eventTypeId)" size="small">{{ eventTypeName(row.eventTypeId) }}</el-tag>
				</template>
				<template #row_event_source="{ row }">
					{{ eventSourceName(row.eventSource) }}
				</template>
				<template #row_priority="{ row }">
					<!-- Màu theo danh mục videowall_event_priority (TagType / StyleSetting trong DB) -->
					<TagInfo v-if="priorityItem(row.priority)" :config-item="priorityItem(row.priority)" size="small" />
					<span v-else>{{ priorityName(row.priority) }}</span>
				</template>
				<template #row_scene="{ row }">
					{{ sceneName(row.targetSceneId) }}
					<el-tag v-if="isLocked(row)" size="small" type="info" class="ml5">
						{{ sceneOf(row.targetSceneId) ? $t('lz.label.videoWall.oldLayout') : $t('lz.label.videoWall.sceneDeleted') }}
					</el-tag>
				</template>
				<template #row_status="{ row }">
					<!-- Kịch bản đích thuộc bố cục cũ / đã xoá ⇒ không cho bật lại (còn đang bật thì vẫn tắt được) -->
					<el-tooltip :disabled="!isLocked(row)" :content="$t('lz.message.videoWall.targetSceneLocked')" placement="top">
						<span>
							<el-switch v-model="row.status" :active-value="StatusTypeEnum.Enable" :inactive-value="StatusTypeEnum.Disable" size="small"
								:disabled="isLocked(row) && row.status !== StatusTypeEnum.Enable" v-auth="'videoWallItsIntegration:update'" @change="onToggle(row)" />
						</span>
					</el-tooltip>
				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.videoWall.testRun')" placement="top" :auto-close="1000">
						<el-button icon="ele-VideoPlay" text type="success" v-auth="'videoWallItsIntegration:update'" @click="handleTest(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
						<el-button icon="ele-CopyDocument" text type="warning" v-auth="'videoWallItsIntegration:add'" @click="handleCopy(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
						<el-button icon="ele-Edit" text type="primary" v-auth="'videoWallItsIntegration:update'" @click="handleEdit(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger" v-auth="'videoWallItsIntegration:delete'" @click="handleDelete(row)"> </el-button>
					</el-tooltip>
				</template>
			</vxe-grid>
		</el-card>
		</GridSearchPanel>

		<EditEventRule ref="editEventRuleRef" :title="state.title" :operateType="state.operateType" :scenes="state.scenes"
			:eventTypes="selectableEventTypes" @handleQuery="handleQuery" />
	</div>
	</WorkspacePage>
</template>

<script lang="ts" setup name="videoWallItsIntegration">
import { onActivated, onMounted, reactive, ref, defineAsyncComponent, computed } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { Local } from '/@/utils/storage';
import EditEventRule from './component/editEventRule.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import { getAPI } from '/@/utils/axios-utils';
import { VwEventRuleApi, VwSceneApi } from '/@/api-services/videoWall';
import { VwPageEventRuleInput, VwPageEventRuleOutput, VwSceneOutput } from '/@/api-services/videoWall';
import { TmsEventTypeApi } from '/@/api-services/tms';
import type { EventTypeOutput } from '/@/api-services/tms';
import { OperateType } from '/@/types/constants/operate';
import { BaseConfigTypeEnum, StatusTypeEnum } from '/@/types/enums/baseEnum';
import { useBaseConfig } from '/@/stores/baseConfig';
import { useVideoWallTopology } from '/@/stores/its/videoWallTopology';
import WorkspacePage from '/@/components/workspace/WorkspacePage.vue';
import GridSearchPanel from '/@/components/gridPage/GridSearchPanel.vue';
import GridSummary from '/@/components/gridPage/GridSummary.vue';
import { useGridSummary } from '/@/components/gridPage/useGridSummary';
import { useGridPager } from '/@/components/gridPage/useGridPager';

const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const topology = useVideoWallTopology();

/** Giá trị mặc định khi thêm quy tắc mới (mã trong danh mục videowall_event_source / videowall_event_priority). */
const DEFAULT_EVENT_SOURCE = 'ITS_EVENT';
const DEFAULT_EVENT_PRIORITY = 'NORMAL';

const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
const { t: $t } = useI18n();
const xGrid = ref<VxeGridInstance>();
const { summary, track } = useGridSummary();
const editEventRuleRef = ref<InstanceType<typeof EditEventRule>>();
const selectableEventTypes = computed(() => state.eventTypes.filter(item => item.code !== 'unknown'));

const state = reactive({
	queryParams: {
		eventTypeId: undefined as unknown as string,
		eventSource: undefined as unknown as string,
		priority: undefined as unknown as string,
		status: undefined as unknown as any,
	},
	localPageParam: {
		pageSize: 50 as number,
		// Sắp theo mã ưu tiên (thứ tự chữ cái: CRITICAL, HIGH, LOW, NORMAL). BE chọn quy tắc theo GetPriorityRank
		// (CRITICAL > HIGH > NORMAL > LOW) nên thứ tự này chưa khớp hoàn toàn khi có mức LOW.
		defaultSort: { field: 'priority', order: 'asc', descStr: 'desc' } as any,
	},
	title: '',
	operateType: 'add' as OperateType,
	showBatchDelete: false,
	/** Toàn bộ kịch bản chưa xoá (mọi trạng thái) — để hiện tên và nhận biết kịch bản bố cục cũ. */
	scenes: [] as VwSceneOutput[],
	scenesLoaded: false,
	/** Danh mục loại sự kiện TMS — hiển thị `name`, lưu `id` (VwEventRule.EventTypeId). */
	eventTypes: [] as EventTypeOutput[],
});
// Local storage parameters
const localPageParamKey = 'localPageParam:videoWallItsIntegration';
// Table parameter configuration
const options = useVxeTable<VwPageEventRuleOutput>(
	{
		id: 'videoWallItsIntegration',
		name: 'VideoWallItsIntegration',
		columns: [
			{ type: 'checkbox', fixed: 'left', width: 45 },
			// Cột ngắn (mã, tag, công tắc, nút) cố định độ rộng; chỉ Loại sự kiện + Kịch bản hiển thị giãn theo màn hình
			{ field: 'code', title: $t('lz.entity.base.code'), width: 180, showOverflow: 'tooltip', sortable: true },
			{ field: 'eventTypeId', title: $t('lz.entity.videoWall.eventType'), minWidth: 200, showOverflow: 'tooltip',sortable: true, slots: { default: 'row_event_type' } },
			{ field: 'eventSource', title: $t('lz.entity.videoWall.eventSource'), width: 150, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_event_source' } },
			{ field: 'priority', title: $t('lz.entity.base.priority'), width: 110, align: 'center', sortable: true, slots: { default: 'row_priority' } },
			{ field: 'targetSceneId', title: $t('lz.entity.videoWall.targetScene'), minWidth: 240, showOverflow: 'tooltip', slots: { default: 'row_scene' } },
			// { field: 'remark', title: $t('lz.entity.base.remark'), minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'status', title: $t('lz.entity.base.status'), width: 100, align: 'center', sortable: true, slots: { default: 'row_status' } },
			{ field: '', title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 150, showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	{
		// Khung đổi kích thước (thu / mở menu, đổi tab) ⇒ tính lại độ rộng cột, cột Hành động luôn sát mép phải
		autoResize: true,
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		sortConfig: { defaultSort: state.localPageParam.defaultSort },
		pagerConfig: { pageSize: state.localPageParam.pageSize },
		toolbarConfig: { export: false },
		rowConfig: { height: 66 },
	}
);

// Thu gọn thanh phân trang theo chiều rộng bảng.
useGridPager(xGrid, options);

// Page initialization
onMounted(async () => {
	state.localPageParam = Local.get(localPageParamKey) || state.localPageParam;
	await Promise.all([loadScenes(), loadEventTypes()]);
});
// Quay lại tab / menu (KeepAlive): tải lại bảng + kịch bản / bố cục (có thể đã đổi ở trang khác).
// onActivated cũng chạy ngay sau lần mount đầu, khi đó dữ liệu vừa tải nên bỏ qua.
let firstActivation = true;
onActivated(async () => {
	if (firstActivation) {
		firstActivation = false;
		return;
	}
	await Promise.all([xGrid.value?.commitProxy('query'), loadScenes()]);
});

/** Nạp lại kịch bản (mọi trạng thái) + bố cục đang dùng — gọi khi vào trang, làm mới bảng và trước khi mở dialog. */
const loadScenes = async () => {
	const [res] = await Promise.all([
		getAPI(VwSceneApi).apiVideowallVwscenePagePost({ page: 1, pageSize: 500, field: 'orderNo', order: 'asc' }),
		topology.load(true).catch(() => {}),
	]);
	state.scenes = (res.data.result?.records ?? []) as VwSceneOutput[];
	state.scenesLoaded = true;
};

const sceneOf = (sceneId?: string | null) => state.scenes.find((s) => s.id === sceneId);
/** Kịch bản đích thuộc bố cục cũ hoặc đã bị xoá ⇒ quy tắc không được bật. */
const isLocked = (row: VwPageEventRuleOutput) => {
	if (!state.scenesLoaded || !row.targetSceneId) return false;
	const scene = sceneOf(row.targetSceneId);
	return !scene || (!!topology.topology?.id && scene.wallTopologyId !== topology.topology.id);
};

// Lỗi thì để rỗng, cột vẫn hiển thị id thô.
const loadEventTypes = async () => {
	try {
		const res = await getAPI(TmsEventTypeApi).apiTmsTmseventtypeListGet();
		state.eventTypes = res.data.result ?? [];
	} catch {
		state.eventTypes = [];
	}
};

// Query API
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const sortField = sort?.field || state.localPageParam.defaultSort.field;
	const sortOrder = sort?.order || state.localPageParam.defaultSort.order;
	const params = Object.assign(state.queryParams, { page: page.currentPage, pageSize: page.pageSize, field: sortField, order: sortOrder, descStr: 'desc' }) as VwPageEventRuleInput;
	return track(getAPI(VwEventRuleApi).apiVideowallVweventrulePagePost(params));
};

// Query operation
const handleQuery = async () => {
	options.loading = true;
	await Promise.all([xGrid.value?.commitProxy('reload'), loadScenes()]);
	options.loading = false;
	selectChangeEvent();
};

// Reset operation
const resetQuery = async () => {
	state.queryParams.eventTypeId = undefined as unknown as string;
	state.queryParams.eventSource = undefined as unknown as string;
	state.queryParams.priority = undefined as unknown as string;
	state.queryParams.status = undefined as unknown as any;
	await xGrid.value?.commitProxy('reload');
};

// Open add dialog
// Mở dialog luôn nạp lại kịch bản + bố cục để dropdown đúng dữ liệu mới nhất
const handleAdd = async () => {
	await loadScenes();
	state.title = $t('lz.label.base.titleAdd', { name: $t('lz.entity.videoWall.rule') });
	state.operateType = 'add';
	editEventRuleRef.value?.openDialog({ status: StatusTypeEnum.Enable, eventSource: DEFAULT_EVENT_SOURCE, priority: DEFAULT_EVENT_PRIORITY });
};

// Open copy dialog
const handleCopy = async (row: any) => {
	await loadScenes();
	state.title = $t('lz.label.base.titleCopy', { name: $t('lz.entity.videoWall.rule') });
	state.operateType = 'copy';
	editEventRuleRef.value?.openDialog(row);
};

// Open edit dialog
const handleEdit = async (row: any) => {
	await loadScenes();
	state.title = $t('lz.label.base.titleEdit', { name: $t('lz.entity.videoWall.rule') });
	state.operateType = 'edit';
	editEventRuleRef.value?.openDialog(row);
};

// Delete
const handleDelete = (row: any) => {
	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.code }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(VwEventRuleApi).apiVideowallVweventruleDeletePost({ id: row.id });
			handleQuery();
			ElMessage.success($t('lz.message.base.deleteSuccess'));
		})
		.catch(() => {});
};

// Batch delete
const handleBatchDelete = async () => {
	const selectRecords = getCheckedRecords();
	ElMessageBox.confirm($t('lz.message.base.confirmBatchDelete', { total: selectRecords.length }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(VwEventRuleApi).apiVideowallVweventruleBatchdeletePost(selectRecords.filter((l) => l.id).map((l) => ({ id: l.id! })));
			handleQuery();
			ElMessage.success($t('lz.message.base.deleteSuccess'));
		})
		.catch(() => {});
};

// Bật/tắt quy tắc ngay trên lưới; lỗi thì trả switch về trạng thái cũ.
const onToggle = async (row: VwPageEventRuleOutput) => {
	const enabled = row.status === (StatusTypeEnum.Enable as any);
	try {
		await getAPI(VwEventRuleApi).apiVideowallVweventruleUpdatePost(row);
		ElMessage.success($t('lz.message.base.saveSuccess'));
	} catch {
		row.status = (enabled ? StatusTypeEnum.Disable : StatusTypeEnum.Enable) as any;
	}
};

// Chạy thử: chỉ gửi eventTypeId để BE tự duyệt VwEventRule chọn kịch bản (và ghi VwEventTriggerLog).
// Không gửi kèm code — BE sẽ bỏ qua quy tắc. Monitor nhận NATS và chuyển theo kịch bản BE chọn.
const handleTest = (row: VwPageEventRuleOutput) => {
	if (row.status !== (StatusTypeEnum.Enable as any)) {
		ElMessage.warning($t('lz.message.videoWall.ruleDisabledCannotTest'));
		return;
	}
	ElMessageBox.confirm($t('lz.message.videoWall.confirmSimulateEvent', { name: eventTypeName(row.eventTypeId) }), $t('lz.button.videoWall.testRun'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'info',
	})
		.then(async () => {
			await getAPI(VwSceneApi).apiVideowallVwsceneActivePost({ eventTypeId: row.eventTypeId });
			ElMessage.success($t('lz.message.videoWall.eventSent', { name: eventTypeName(row.eventTypeId) }));
		})
		.catch(() => {});
};

// Hiển thị
const sceneName = (sceneId?: string | null) => sceneOf(sceneId)?.name || '—';
const eventTypeName = (id?: string | null) => (id ? state.eventTypes.find((e) => e.id === id)?.name || id : '—');
const eventSourceName = (code?: string | null) => (code ? getConfigItemByCode(BaseConfigTypeEnum.VideoWallEventSource, code)?.name || code : '—');
const priorityItem = (code?: string | null) => (code ? getConfigItemByCode(BaseConfigTypeEnum.VideoWallEventPriority, code) : undefined);
const priorityName = (code?: string | null) => (code ? priorityItem(code)?.name || code : '—');
/** Màu tag suy theo `code` của loại sự kiện. */
const eventTagType = (id?: string | null): any => {
	const code = (state.eventTypes.find((e) => e.id === id)?.code || '').toUpperCase();
	switch (code) {
		case 'ACCIDENT':
		case 'EMERGENCY':
			return 'danger';
		case 'CONGESTION':
			return 'warning';
		case 'WEATHER_ALERT':
			return 'info';
		default:
			return 'primary';
	}
};

// Custom column
function handleCustom() {
	xGrid.value?.openCustom();
}
// Get selected records
function getCheckedRecords() {
	return (xGrid.value?.getCheckboxRecords() as VwPageEventRuleOutput[]) ?? [];
}

function selectChangeEvent() {
	state.showBatchDelete = getCheckedRecords().length > 0;
}

// Table events
const gridEvents: VxeGridListeners<VwPageEventRuleOutput> = {
	async pageChange({ pageSize }) {
		state.localPageParam.pageSize = pageSize;
	},
	async sortChange({ field, order }) {
		state.localPageParam.defaultSort = { field: field, order: order!, descStr: 'desc' };
	},
};
</script>
