<template>
	<div class="videowall-wall-permission-container">
		<GridSearchPanel id="videowall-wall-permission-search">
			<template #filters>
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true"
				label-width="auto" style="flex: 1 1 0%" label-position="left">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.videoWall.permissionCodeName')">
							<el-input v-model="state.queryParams.keyword" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.label.videoWall.permissionCodeName') })" clearable
								@keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.videoWall.user')">
							<el-select v-model="state.queryParams.userId" :placeholder="$t('lz.label.videoWall.selectUser')" filterable clearable class="w100"
								@clear="state.queryParams.userId = undefined">
								<el-option v-for="u in state.users" :key="u.id!" :label="userLabel(u)" :value="u.id!" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.videoWall.org')">
							<el-cascader v-model="state.queryParams.orgId" :options="state.orgTree"
								:props="{ checkStrictly: true, emitPath: false, value: 'id', label: 'name', children: 'children' }"
								filterable clearable :placeholder="$t('lz.label.videoWall.selectOrg')" class="w100" @clear="state.queryParams.orgId = undefined">
								<template #default="{ node, data }">
									<span>{{ data.name }}</span>
									<span v-if="!node.isLeaf"> ({{ data.children.length }}) </span>
								</template>
							</el-cascader>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.status')">
							<el-select v-model="state.queryParams.status" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.base.status') })" clearable class="w100" @clear="state.queryParams.status = undefined">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code" :label="item.name" :value="parseInt(item.code)" />
							</el-select>
						</el-form-item>
					</el-col>
					<!-- Phân quyền gắn theo phiên bản bố cục: mặc định chỉ xem quyền đang có hiệu lực -->
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.videoWall.layoutFilter')">
							<el-checkbox v-model="state.onlyCurrentLayout" @change="handleQuery">{{ $t('lz.label.videoWall.onlyCurrentLayout') }}</el-checkbox>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>

			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />

			<el-row>
				<SearchButton :auth-search="'videoWallPermission:page'" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
			</el-row>
			</template>

		<el-card class="full-table" shadow="hover">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents"
				@checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">
				<template #toolbar_tools>
					<TableHeader @add="handleAdd" :show-delete="state.showBatchDelete" :auth-add="'videoWallPermission:add'"
						:auth-delete="'videoWallPermission:delete'" @refresh="handleQuery" @custom-column="handleCustom"
						@delete="handleBatchDelete">
					</TableHeader>
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_actors="{ row }">
					<!-- Nhiều đối tượng không đủ chỗ trong cột ⇒ hover hiện toàn bộ danh sách -->
					<el-tooltip placement="top" :show-after="300" :disabled="!actorCount(row)">
						<template #content>
							<div class="wp-actor-tip">
								<template v-if="row.userIds?.length">
									<div class="wp-actor-tip-title">{{ $t('lz.entity.videoWall.user') }} ({{ row.userIds.length }})</div>
									<div v-for="id in row.userIds" :key="`tu-${id}`">• {{ userName(id) }}</div>
								</template>
								<template v-if="row.orgIds?.length">
									<div class="wp-actor-tip-title">{{ $t('lz.entity.videoWall.org') }} ({{ row.orgIds.length }})</div>
									<div v-for="id in row.orgIds" :key="`to-${id}`">• {{ orgName(id) }}</div>
								</template>
							</div>
						</template>
						<!-- Mỗi bản ghi chỉ 1 loại đối tượng: nhãn loại + tên nối nhau (tối đa MAX_ACTOR_NAMES) + "+N"; đủ danh sách ở tooltip -->
						<div v-if="actorCount(row)" class="wp-actor" :class="row.userIds?.length ? 'is-user' : 'is-org'">
							<span class="wp-actor-type">
								<el-icon><ele-User v-if="row.userIds?.length" /><ele-OfficeBuilding v-else /></el-icon>
								{{ row.userIds?.length ? $t('lz.entity.videoWall.user') : $t('lz.entity.videoWall.org') }}
							</span>
							<span class="wp-actor-names">{{ actorSummary(row).text }}</span>
							<span v-if="actorSummary(row).rest > 0" class="wp-actor-more">+{{ actorSummary(row).rest }}</span>
						</div>
						<span v-else class="wp-actor-none">—</span>
					</el-tooltip>
				</template>
				<template #row_area="{ row }">
					<WallMiniGrid :config="row.config" :is-full-access="row.isFullAccess" :blocks="state.layouts.find((l) => l.id === row.wallTopologyId)?.blocks" />
				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<!-- Bố cục của quyền: tên bố cục (xanh = bố cục đang dùng, xám = bố cục khác — quyền không có hiệu lực) -->
				<template #row_layout="{ row }">
					<el-tooltip :content="$t('lz.message.videoWall.permissionOtherLayout')" placement="top" :disabled="!isOldLayout(row)" :show-after="300">
						<el-tag size="small" :type="isOldLayout(row) ? 'info' : 'success'" disable-transitions>{{ layoutName(row) }}</el-tag>
					</el-tooltip>
				</template>
				<!-- Dùng / Không dùng — độc lập với bố cục; dữ liệu cũ chưa có trạng thái coi như Dùng -->
				<template #row_status="{ row }">
					<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, row.status ?? StatusEnum.NUMBER_1)" />
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
						<el-button icon="ele-CopyDocument" text type="warning" v-auth="'videoWallPermission:add'" @click="handleCopy(row)"> </el-button>
					</el-tooltip>
					<!-- Quyền của bố cục khác vẫn sửa được: hộp sửa vẽ lưới theo bố cục của quyền -->
					<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
						<el-button icon="ele-Edit" text type="primary" v-auth="'videoWallPermission:update'" @click="handleEdit(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger" v-auth="'videoWallPermission:delete'" @click="handleDelete(row)"> </el-button>
					</el-tooltip>
				</template>
			</vxe-grid>
		</el-card>
		</GridSearchPanel>

		<EditWallPermission ref="editWallPermissionRef" :title="state.title" :operateType="state.operateType" :screens="state.screens"
			:controllers="vwCtrl.controllers" :users="state.users" :org-tree="state.orgTree" @handleQuery="handleQuery" />
	</div>
</template>

<script lang="ts" setup name="videoWallPermission">
import { computed, onMounted, reactive, ref, defineAsyncComponent } from 'vue';
import GridSearchPanel from '/@/components/gridPage/GridSearchPanel.vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { Local } from '/@/utils/storage';
import EditWallPermission from './component/editWallPermission.vue';
import WallMiniGrid from './component/wallMiniGrid.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import { getAPI } from '/@/utils/axios-utils';
import { VwWallPermissionApi, VwScreenApi, VwWallTopologyApi } from '/@/api-services/videoWall';
import { VwPageWallPermissionInput, VwPageWallPermissionOutput, VwScreenOutput, VwWallTopologyOutput } from '/@/api-services/videoWall';
import { SysOrgApi, SysUserApi, SysOrg, UserOutput } from '/@/api-services/system';
import { OperateType } from '/@/types/constants/operate';
import { useVideoWallController } from '/@/stores/its/videoWallController';
import { useVideoWallTopology } from '/@/stores/its/videoWallTopology';
import { StatusEnum } from '/@/api-services/videoWall';
import { BaseConfigTypeEnum } from '/@/types/enums/baseEnum';
import { useBaseConfig } from '/@/stores/baseConfig';

const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
const { t: $t } = useI18n();
const vwCtrl = useVideoWallController();
const { getConfigDataByCode, getConfigItemByCode } = useBaseConfig();
const xGrid = ref<VxeGridInstance>();
const editWallPermissionRef = ref<InstanceType<typeof EditWallPermission>>();

/** Số bản ghi tối đa nạp một lần qua `/page` (giống các trang VideoWall khác). */
const PAGE_ALL = 500;
/** Bố cục tường (khối) — lưới chọn ô theo bố cục đã lưu ở trang Màn hình, khớp BE kiểm ô. */
const topology = useVideoWallTopology();

const state = reactive({
	queryParams: {
		keyword: undefined as any,
		userId: undefined as any,
		orgId: undefined as any,
		status: undefined as any,
	},
	/** Chỉ hiện quyền thuộc bố cục đang dùng (quyền có hiệu lực). Bỏ chọn ⇒ hiện cả quyền của phiên bản cũ. */
	onlyCurrentLayout: true,
	localPageParam: {
		pageSize: 50 as number,
		defaultSort: { field: 'createTime', order: 'desc', descStr: 'desc' } as any,
	},
	title: '',
	operateType: 'add' as OperateType,
	showBatchDelete: false,
	users: [] as UserOutput[],
	orgTree: [] as SysOrg[],
	screens: [] as VwScreenOutput[],
	/** Danh sách bố cục — hiển thị tên bố cục của từng quyền. */
	layouts: [] as VwWallTopologyOutput[],
});
const localPageParamKey = 'localPageParam:videoWallPermission';

const userMap = computed(() => new Map(state.users.map((u) => [u.id ?? '', u])));
const orgMap = computed(() => {
	const map = new Map<string, SysOrg>();
	const walk = (nodes?: SysOrg[] | null) => {
		for (const n of nodes ?? []) {
			if (n.id) map.set(n.id, n);
			walk(n.children);
		}
	};
	walk(state.orgTree);
	return map;
});
const userLabel = (u: UserOutput) => (u.realName || u.name ? `${u.realName || u.name} (${u.username})` : u.username || u.id || '');
const userName = (id: string) => {
	const u = userMap.value.get(id);
	return u ? userLabel(u) : id;
};
const orgName = (id: string) => orgMap.value.get(id)?.name || id;
const actorCount = (row: VwPageWallPermissionOutput) => (row.userIds?.length ?? 0) + (row.orgIds?.length ?? 0);
/**
 * Quyền cấp trên phiên bản bố cục khác bố cục đang dùng ⇒ không có hiệu lực, chỉ xem / nhân bản / xoá.
 * Chưa có WallTopologyId (BE / migration chưa cập nhật) ⇒ không coi là bố cục cũ, tránh gắn nhãn sai cả bảng.
 */
const isOldLayout = (row: VwPageWallPermissionOutput) => !!topology.topology?.id && !!row.wallTopologyId && row.wallTopologyId !== topology.topology.id;

/** Tên bố cục của quyền "BC07 — Bố cục …"; không tra được (dữ liệu cũ chưa gắn bố cục) ⇒ bố cục đang dùng. */
const layoutName = (row: VwPageWallPermissionOutput) => {
	const l = state.layouts.find((x) => x.id === (row.wallTopologyId || topology.topology?.id));
	return l ? `${l.code} — ${l.name}` : row.wallTopologyId || '';
};
const loadLayouts = async () => {
	const res = await getAPI(VwWallTopologyApi).apiVideowallVwwalltopologyListGet();
	state.layouts = res.data.result ?? [];
};

/** Số tên đối tượng nối nhau trong ô; phần còn lại gộp "+N", xem đủ ở tooltip. */
const MAX_ACTOR_NAMES = 3;
const actorSummary = (row: VwPageWallPermissionOutput) => {
	const names = row.userIds?.length
		? row.userIds.map((id) => {
				const u = userMap.value.get(id);
				return u ? u.realName || u.name || u.username || id : id;
			})
		: (row.orgIds ?? []).map(orgName);
	return { text: names.slice(0, MAX_ACTOR_NAMES).join(', '), rest: Math.max(0, names.length - MAX_ACTOR_NAMES) };
};
const areaCountLabel = (row: VwPageWallPermissionOutput) => {
	const total = topology.totalScreens;
	if (row.isFullAccess) return `${total}/${total}`;
	try {
		const cells = row.config ? (JSON.parse(row.config) as any[]) : [];
		return `${cells?.length ?? 0}/${total}`;
	} catch {
		return $t('lz.label.videoWall.configError');
	}
};

const options = useVxeTable<VwPageWallPermissionOutput>(
	{
		id: 'videoWallPermission',
		name: 'VideoWallPermission',
		columns: [
			{ type: 'checkbox', fixed: 'left', width: 45 },
			{ field: 'code', title: $t('lz.entity.base.code'), minWidth: 120, showOverflow: 'tooltip', sortable: true },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 160, showOverflow: 'tooltip', sortable: true },
			{ field: 'userIds', title: $t('lz.entity.videoWall.actor'), minWidth: 240, showOverflow: false, slots: { default: 'row_actors' } },
			{ field: 'wallTopologyId', title: $t('lz.label.videoWall.layoutFilter'), width: 200, align: 'center', showOverflow: 'tooltip', slots: { default: 'row_layout' } },
			{ field: 'config', title: $t('lz.entity.videoWall.grantedArea'), minWidth: 200, slots: { default: 'row_area' } },
			{ field: 'isFullAccess', title: $t('lz.entity.videoWall.screenCount'), width: 110, formatter: ({ row }: any) => areaCountLabel(row) },
			{ field: 'status', title: $t('lz.entity.base.status'), width: 110, align: 'center', sortable: true, slots: { default: 'row_status' } },
			{ field: '', title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 130, showOverflow: true, className: 'wp-col-action', slots: { default: 'row_buttons' } },
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
		// Quyền của bố cục cũ không có hiệu lực ⇒ làm mờ cả dòng
		rowClassName: ({ row }: any) => (isOldLayout(row) ? 'wp-row-old-layout' : ''),
	}
);

// Page initialization
onMounted(async () => {
	state.localPageParam = Local.get(localPageParamKey) || state.localPageParam;
	await Promise.all([loadUsers(), loadOrgTree(), loadScreens(), loadLayouts(), vwCtrl.load().catch(() => {}), topology.load(true)]);
});

// Danh mục phục vụ ô chọn và hiển thị tên
const loadUsers = async () => {
	const res = await getAPI(SysUserApi).apiSystemSysuserListPost({});
	state.users = res.data.result ?? [];
};
const loadOrgTree = async () => {
	const res = await getAPI(SysOrgApi).apiSystemSysorgListGet('0');
	state.orgTree = res.data.result ?? [];
};
const loadScreens = async () => {
	const res = await getAPI(VwScreenApi).apiVideowallVwscreenPagePost({ page: 1, pageSize: PAGE_ALL });
	state.screens = res.data.result?.records ?? [];
};

// Query API
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const sortField = sort?.field || state.localPageParam.defaultSort.field;
	const sortOrder = sort?.order || state.localPageParam.defaultSort.order;
	// Lọc theo bố cục đang dùng cần ID bố cục ⇒ chờ nạp (dùng lại nếu đã có)
	await topology.load();
	const params = Object.assign(state.queryParams, {
		page: page.currentPage,
		pageSize: page.pageSize,
		field: sortField,
		order: sortOrder,
		descStr: 'desc',
		wallTopologyId: state.onlyCurrentLayout && !topology.topology?.isDefault ? topology.topology?.id : undefined,
	}) as VwPageWallPermissionInput;
	return await getAPI(VwWallPermissionApi).apiVideowallVwwallpermissionPagePost(params);
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
	state.queryParams.keyword = undefined;
	state.queryParams.userId = undefined;
	state.queryParams.orgId = undefined;
	state.queryParams.status = undefined;
	state.onlyCurrentLayout = true;
	await xGrid.value?.commitProxy('reload');
};

/**
 * Nạp lại bố cục tường + vị trí màn hình + bộ điều khiển trước khi mở dialog.
 * Trang được giữ sống khi chuyển tab ⇒ bố cục / màn có thể vừa đổi ở trang Màn hình;
 * dialog cũng lọc ô đã cấp theo bố cục hiện tại nên phải nạp xong mới mở.
 */
const refreshWallData = () => Promise.all([loadScreens(), loadLayouts(), topology.load(true), vwCtrl.load(true).catch(() => {})]);

// Open add dialog
const handleAdd = async () => {
	await refreshWallData();
	state.title = $t('lz.label.base.titleAdd', { name: $t('lz.entity.videoWall.wallPermission') });
	state.operateType = 'add';
	editWallPermissionRef.value?.openDialog({ isFullAccess: false });
};

// Open copy dialog — giữ vùng được cấp và đối tượng, bỏ mã để nhập mã mới
const handleCopy = async (row: any) => {
	await refreshWallData();
	state.title = $t('lz.label.base.titleCopy', { name: $t('lz.entity.videoWall.wallPermission') });
	state.operateType = 'copy';
	editWallPermissionRef.value?.openDialog({ ...row, code: null });
};

// Open edit dialog
const handleEdit = async (row: any) => {
	await refreshWallData();
	state.title = $t('lz.label.base.titleEdit', { name: $t('lz.entity.videoWall.wallPermission') });
	state.operateType = 'edit';
	editWallPermissionRef.value?.openDialog(row);
};

// Delete
const handleDelete = (row: any) => {
	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.name || row.code }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(VwWallPermissionApi).apiVideowallVwwallpermissionDeletePost({ id: row.id });
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
			await getAPI(VwWallPermissionApi).apiVideowallVwwallpermissionBatchdeletePost(selectRecords.filter((l) => l.id).map((l) => ({ id: l.id! })));
			handleQuery();
			ElMessage.success($t('lz.message.base.deleteSuccess'));
		})
		.catch(() => {});
};

// Custom column
function handleCustom() {
	xGrid.value?.openCustom();
}
// Get selected records
function getCheckedRecords() {
	return (xGrid.value?.getCheckboxRecords() as VwPageWallPermissionOutput[]) ?? [];
}

function selectChangeEvent() {
	state.showBatchDelete = getCheckedRecords().length > 0;
}

// Table events
const gridEvents: VxeGridListeners<VwPageWallPermissionOutput> = {
	async pageChange({ pageSize }) {
		state.localPageParam.pageSize = pageSize;
	},
	async sortChange({ field, order }) {
		state.localPageParam.defaultSort = { field: field, order: order!, descStr: 'desc' };
	},
};
</script>

<style scoped lang="scss">
.wp-actor-tip {
	max-height: 320px;
	overflow-y: auto;
	line-height: 1.6;
}
.wp-actor-tip-title {
	font-weight: 600;
	& + div {
		margin-top: 2px;
	}
	&:not(:first-child) {
		margin-top: 6px;
	}
}
/* Cột đối tượng: [icon Loại] tên A, tên B, tên C  +N */
.wp-actor {
	display: flex;
	align-items: center;
	gap: 8px;
	min-width: 0;
	.wp-actor-type {
		flex-shrink: 0;
		display: inline-flex;
		align-items: center;
		gap: 4px;
		height: 20px;
		padding: 0 6px;
		border-radius: 4px;
		font-size: 12px;
		color: var(--el-color-primary);
		background: var(--el-color-primary-light-9);
	}
	&.is-org .wp-actor-type {
		color: var(--el-color-success);
		background: var(--el-color-success-light-9);
	}
	.wp-actor-names {
		min-width: 0;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}
	.wp-actor-more {
		flex-shrink: 0;
		padding: 0 6px;
		border-radius: 10px;
		font-size: 12px;
		line-height: 18px;
		color: var(--el-text-color-secondary);
		background: var(--el-fill-color);
	}
}
.wp-actor-none {
	color: var(--el-text-color-placeholder);
}
/* Dòng quyền thuộc bố cục cũ: mờ đi (trừ cột hành động để vẫn bấm Nhân bản / Xoá rõ ràng) */
:deep(.wp-row-old-layout .vxe-body--column:not(.wp-col-action):not(.col--checkbox) .vxe-cell) {
	opacity: 0.55;
}
</style>
