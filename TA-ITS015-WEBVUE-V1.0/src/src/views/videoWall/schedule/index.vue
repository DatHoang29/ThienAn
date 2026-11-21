<template>
	<WorkspacePage>
	<div class="videowall-schedule-container">
		<GridSearchPanel id="videowall-schedule-search">
			<template #filters>
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true"
				label-width="auto" style="flex: 1 1 0%" label-position="left">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.videoWall.codeOrName')" prop="name">
							<el-input v-model="state.queryParams.name" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.label.videoWall.codeOrName') })" clearable
								@keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.videoWall.targetScene')" prop="targetSceneId">
							<el-select v-model="state.queryParams.targetSceneId" :placeholder="$t('lz.label.videoWall.selectScene')" filterable clearable class="w100">
								<el-option v-for="s in state.scenes" :key="s.id!" :label="`${s.code} — ${s.name}`" :value="s.id!" />
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
					<TableHeader @add="handleAdd" :show-delete="state.showBatchDelete" :auth-add="'videoWallSchedule:add'"
						:auth-delete="'videoWallSchedule:delete'" @refresh="handleQuery" @custom-column="handleCustom"
						@delete="handleBatchDelete">
					</TableHeader>
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_type="{ row }">
					<el-tag size="small" :type="scheduleTypeOf(row.scheduleType)?.tag">{{ scheduleTypeOf(row.scheduleType)?.label ?? row.scheduleType }}</el-tag>
				</template>
				<template #row_time="{ row }">
					{{ formatScheduleTime(row) }}
				</template>
				<template #row_action="{ row }">
					{{ actionLabel(row.action) }}
				</template>
				<template #row_status="{ row }">
					<!-- Kịch bản đích thuộc bố cục cũ / đã xoá ⇒ không cho bật lại (còn đang bật thì vẫn tắt được) -->
					<el-tooltip :disabled="!isLocked(row)" :content="$t('lz.message.videoWall.targetSceneLocked')" placement="top">
						<span>
							<el-switch v-model="row.status" :active-value="StatusTypeEnum.Enable" :inactive-value="StatusTypeEnum.Disable" size="small"
								:disabled="isLocked(row) && row.status !== StatusTypeEnum.Enable" v-auth="'videoWallSchedule:update'" @change="onToggle(row)" />
						</span>
					</el-tooltip>
				</template>
				<template #row_next_run="{ row }">
					<span v-if="row.status === StatusTypeEnum.Enable">{{ formatDateTime(row.nextRun) || '—' }}</span>
					<span v-else style="color: var(--el-text-color-placeholder)">{{ $t('lz.label.videoWall.disabled') }}</span>
				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.videoWall.testRun')" placement="top" :auto-close="1000">
						<el-button icon="ele-VideoPlay" text type="success" v-auth="'videoWallSchedule:update'" @click="handleTest(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
						<el-button icon="ele-CopyDocument" text type="warning" v-auth="'videoWallSchedule:add'" @click="handleCopy(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
						<el-button icon="ele-Edit" text type="primary" v-auth="'videoWallSchedule:update'" @click="handleEdit(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger" v-auth="'videoWallSchedule:delete'" @click="handleDelete(row)"> </el-button>
					</el-tooltip>
				</template>
			</vxe-grid>
		</el-card>
		</GridSearchPanel>

		<EditSchedule ref="editScheduleRef" :title="state.title" :operateType="state.operateType" :scenes="state.scenes" @handleQuery="handleQuery" />
	</div>
	</WorkspacePage>
</template>

<script lang="ts" setup name="videoWallSchedule">
import { onActivated, onMounted, reactive, ref, defineAsyncComponent } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { Local } from '/@/utils/storage';
import EditSchedule from './component/editSchedule.vue';
import { SCHEDULE_TYPES, SCHEDULE_ACTION_LABELS, WEEKDAY_OPTIONS } from './component/scheduleOptions';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import { getAPI } from '/@/utils/axios-utils';
import { VwScheduleApi, VwSceneApi } from '/@/api-services/videoWall';
import { VwPageScheduleInput, VwPageScheduleOutput, VwSceneOutput } from '/@/api-services/videoWall';
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
const topology = useVideoWallTopology();

const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const { t: $t } = useI18n();
const xGrid = ref<VxeGridInstance>();
const { summary, track } = useGridSummary();
const editScheduleRef = ref<InstanceType<typeof EditSchedule>>();
const state = reactive({
	queryParams: {
		name: undefined as unknown as string,
		targetSceneId: undefined as unknown as string,
		status: undefined as unknown as any,
	},
	localPageParam: {
		pageSize: 50 as number,
		defaultSort: { field: 'createTime', order: 'desc', descStr: 'desc' } as any,
	},
	title: '',
	operateType: 'add' as OperateType,
	showBatchDelete: false,
	/** Toàn bộ kịch bản chưa xoá (mọi trạng thái) — để hiện tên và nhận biết kịch bản bố cục cũ. */
	scenes: [] as VwSceneOutput[],
	scenesLoaded: false,
});
// Local storage parameters
const localPageParamKey = 'localPageParam:videoWallSchedule';
// Table parameter configuration
const options = useVxeTable<VwPageScheduleOutput>(
	{
		id: 'videoWallSchedule',
		name: 'VideoWallSchedule',
		columns: [
			{ type: 'checkbox', fixed: 'left', width: 45 },
			// Cột ngắn cố định độ rộng; chỉ Tên + Kịch bản hiển thị giãn theo màn hình. Trạng thái đứng ngay trước Thông tin
			{ field: 'code', title: $t('lz.entity.base.code'), width: 160, showOverflow: 'tooltip', sortable: true },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 180, showOverflow: 'tooltip', sortable: true },
			{ field: 'scheduleType', title: $t('lz.entity.videoWall.scheduleType'), width: 120, align: 'center', sortable: true, slots: { default: 'row_type' } },
			{ field: 'time', title: $t('lz.entity.videoWall.time'), width: 180, showOverflow: 'tooltip', slots: { default: 'row_time' } },
			{ field: 'action', title: $t('lz.entity.videoWall.executeAction'), minWidth: 220, showOverflow: 'tooltip', slots: { default: 'row_action' } },
			{ field: 'nextRun', title: $t('lz.entity.videoWall.nextRun'), width: 160, align: 'center', sortable: true, slots: { default: 'row_next_run' } },
			{ field: 'lastRun', title: $t('lz.entity.videoWall.lastRun'), width: 160, align: 'center', sortable: true, formatter: ({ cellValue }: any) => formatDateTime(cellValue) },
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
	await loadScenes();
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

/** Lịch "Chuyển kịch bản" có kịch bản đích thuộc bố cục cũ hoặc đã bị xoá ⇒ không được bật. */
const isLocked = (row: VwPageScheduleOutput) => {
	if (!state.scenesLoaded || row.action !== 'activate_scene' || !row.targetSceneId) return false;
	const scene = state.scenes.find((s) => s.id === row.targetSceneId);
	return !scene || (!!topology.topology?.id && scene.wallTopologyId !== topology.topology.id);
};

// Query API
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const sortField = sort?.field || state.localPageParam.defaultSort.field;
	const sortOrder = sort?.order || state.localPageParam.defaultSort.order;
	const params = Object.assign(state.queryParams, { page: page.currentPage, pageSize: page.pageSize, field: sortField, order: sortOrder, descStr: 'desc' }) as VwPageScheduleInput;
	return track(getAPI(VwScheduleApi).apiVideowallVwschedulePagePost(params));
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
	state.queryParams.name = undefined as unknown as string;
	state.queryParams.targetSceneId = undefined as unknown as string;
	state.queryParams.status = undefined as unknown as any;
	await xGrid.value?.commitProxy('reload');
};

// Open add dialog — mở dialog luôn nạp lại kịch bản + bố cục để dropdown đúng dữ liệu mới nhất
const handleAdd = async () => {
	await loadScenes();
	state.title = $t('lz.label.base.titleAdd', { name: $t('lz.entity.videoWall.schedule') });
	state.operateType = 'add';
	// Giờ chạy mặc định = giờ hiện tại (HH:mm)
	editScheduleRef.value?.openDialog({ status: StatusTypeEnum.Enable, scheduleType: 1, time: new Date().toTimeString().slice(0, 5), weekdays: '1,2,3,4,5', action: 'activate_scene' });
};

// Open copy dialog
const handleCopy = async (row: any) => {
	await loadScenes();
	state.title = $t('lz.label.base.titleCopy', { name: $t('lz.entity.videoWall.schedule') });
	state.operateType = 'copy';
	editScheduleRef.value?.openDialog(row);
};

// Open edit dialog
const handleEdit = async (row: any) => {
	await loadScenes();
	state.title = $t('lz.label.base.titleEdit', { name: $t('lz.entity.videoWall.schedule') });
	state.operateType = 'edit';
	editScheduleRef.value?.openDialog(row);
};

// Delete
const handleDelete = (row: any) => {
	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.name }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(VwScheduleApi).apiVideowallVwscheduleDeletePost({ id: row.id });
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
			await getAPI(VwScheduleApi).apiVideowallVwscheduleBatchdeletePost(selectRecords.filter((l) => l.id).map((l) => ({ id: l.id! })));
			handleQuery();
			ElMessage.success($t('lz.message.base.deleteSuccess'));
		})
		.catch(() => {});
};

// Bật/tắt lịch ngay trên lưới; BE đăng ký / gỡ job Hangfire và tính lại lần chạy tiếp theo.
const onToggle = async (row: VwPageScheduleOutput) => {
	const enabled = row.status === (StatusTypeEnum.Enable as any);
	try {
		await getAPI(VwScheduleApi).apiVideowallVwscheduleUpdatePost(row);
		ElMessage.success($t('lz.message.base.saveSuccess'));
		handleQuery();
	} catch {
		row.status = (enabled ? StatusTypeEnum.Disable : StatusTypeEnum.Enable) as any;
	}
};

// Chạy thử: BE thực hiện hành động của lịch ngay (POST vwschedule/runnow) và ghi lastRun.
const handleTest = (row: VwPageScheduleOutput) => {
	if (row.action !== 'activate_scene') {
		ElMessage.warning($t('lz.message.videoWall.testOnlyActivateScene'));
		return;
	}
	if (isLocked(row)) {
		ElMessage.warning($t('lz.message.videoWall.targetSceneLocked'));
		return;
	}
	const scene = state.scenes.find((s) => s.id === row.targetSceneId);
	if (!scene?.code) {
		ElMessage.warning($t('lz.message.videoWall.targetSceneMissing'));
		return;
	}
	ElMessageBox.confirm($t('lz.message.videoWall.confirmSwitchScene', { name: scene.name }), $t('lz.button.videoWall.testRun'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'info',
	})
		.then(async () => {
			await getAPI(VwScheduleApi).apiVideowallVwscheduleRunnowPost({ id: row.id! });
			ElMessage.success($t('lz.message.videoWall.switchedToScene', { name: scene.name }));
			handleQuery();
		})
		.catch(() => {});
};

// Hiển thị
const scheduleTypeOf = (value?: number | null) => SCHEDULE_TYPES.find((t) => t.value === value);
const actionLabel = (value?: string | null) => SCHEDULE_ACTION_LABELS.find((a) => a.value === value)?.label ?? value ?? '';
const formatDateTime = (value?: Date | string | null) => {
	if (!value) return '';
	const d = new Date(value);
	return Number.isNaN(d.getTime()) ? '' : d.toLocaleString('vi-VN');
};
const formatScheduleTime = (row: VwPageScheduleOutput) => {
	switch (row.scheduleType) {
		case 1:
			return $t('lz.label.videoWall.dailyAt', { time: row.time ?? '' });
		case 2: {
			const days = (row.weekdays ?? '')
				.split(',')
				.map((d) => WEEKDAY_OPTIONS.find((o) => String(o.value) === d.trim())?.label)
				.filter(Boolean)
				.join(', ');
			return `${row.time ?? ''} (${days})`;
		}
		case 3:
			// OnceDate lưu dạng "YYYY-MM-DD HH:mm" -> hiển thị "DD/MM/YYYY HH:mm"
			return `${(row.onceDate ?? '').replace(/^(\d{4})-(\d{2})-(\d{2})/, '$3/$2/$1')} ${row.time ?? ''}`.trim();
		default:
			return '';
	}
};

// Custom column
function handleCustom() {
	xGrid.value?.openCustom();
}
// Get selected records
function getCheckedRecords() {
	return (xGrid.value?.getCheckboxRecords() as VwPageScheduleOutput[]) ?? [];
}

function selectChangeEvent() {
	state.showBatchDelete = getCheckedRecords().length > 0;
}

// Table events
const gridEvents: VxeGridListeners<VwPageScheduleOutput> = {
	async pageChange({ pageSize }) {
		state.localPageParam.pageSize = pageSize;
	},
	async sortChange({ field, order }) {
		state.localPageParam.defaultSort = { field: field, order: order!, descStr: 'desc' };
	},
};
</script>
