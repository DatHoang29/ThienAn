<template>
	<div class="sharedata-mapping-container">
		<el-card shadow="hover"
			:body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form class="sd-filter-form" :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true"
				label-width="auto" style="flex: 1 1 0%" label-position="left">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.sharedataMapping.code')" prop="code">
							<el-input v-model="state.queryParams.code" :placeholder="$t('lz.label.sharedataMapping.codePlaceholder')" clearable
								@keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.sharedataMapping.name')" prop="name">
							<el-input v-model="state.queryParams.name" :placeholder="$t('lz.label.sharedataMapping.namePlaceholder')" clearable
								@keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.sharedataMapping.partner')" prop="partnerId">
							<el-select v-model="state.queryParams.partnerId" :placeholder="$t('lz.label.sharedataMapping.partnerId')" filterable
								clearable class="w100">
								<el-option v-for="item in state.partnerList" :key="item.id" :label="codeNameLabel(item)"
									:value="item.id" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.sharedataMapping.datatype')" prop="datatypeId">
							<el-select v-model="state.queryParams.datatypeId" :placeholder="$t('lz.label.sharedataMapping.datatypeId')" filterable
								clearable class="w100">
								<el-option v-for="item in state.packetList" :key="item.id"
									:label="codeNameLabel(item)" :value="item.id" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.sharedataMapping.direction')" prop="direction">
							<el-select v-model="state.queryParams.direction" :placeholder="$t('lz.label.sharedataMapping.direction')" clearable
								class="w100">
								<el-option v-for="item in DIRECTION_LIST" :key="item.code" :label="item.name"
									:value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.sharedataMapping.isActive')" prop="isActive">
							<el-select v-model="state.queryParams.isActive" :placeholder="$t('lz.label.sharedataMapping.isActiveAll')" clearable class="w100">
								<el-option :label="$t('lz.label.sharedataMapping.enabled')" :value="true" />
								<el-option :label="$t('lz.label.sharedataMapping.disabled')" :value="false" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>

			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />

			<el-row>
				<SearchButton :auth-search="'eshMapping:page'" @search="handleQuery" @reset="resetQuery"
					:loading="options.loading" />
			</el-row>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents"
				@checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">
				<template #toolbar_tools>
					<TableHeader @add="handleAdd" :show-delete="state.showBatchDelete" :auth-add="'eshMapping:add'"
						:auth-delete="'eshMapping:delete'" @refresh="handleQuery" @custom-column="handleCustom"
						@delete="handleBatchDelete">
					</TableHeader>
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_partner="{ row }">{{ partnerNameOf(state.partnerList, row.partnerId) || $t('lz.label.sharedataMapping.sharedProfile') }}</template>
				<template #row_datatype="{ row }">{{ packetNameOf(state.packetList, row.datatypeId) }}</template>

				<template #row_direction="{ row }">
					<TagInfo :config-item="getTagItem(DIRECTION_LIST, row.direction)" />
				</template>


				<template #row_item_count="{ row }">
					<el-tag size="small" type="info">{{ row.mappedFieldCount ?? 0 }}</el-tag>
				</template>

				<!-- Bật/tắt tại chỗ: backend tự tắt các hồ sơ trùng cặp đối tác + gói tin. -->
				<template #row_active="{ row }">
					<el-tooltip
						:content="mappingSwitchHint(row)"
						:disabled="!isMappingRowLocked(row)"
						placement="top"
					>
						<span class="inline-block" :style="{ cursor: isMappingRowLocked(row) ? 'not-allowed' : 'default' }">
							<el-switch
								:model-value="Boolean(row.isActive)"
								:disabled="isMappingRowLocked(row)"
								v-auth="'eshMapping:update'"
								@change="(val: any) => handleSetActive(row, Boolean(val))"
							/>
						</span>
					</el-tooltip>
				</template>

				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>

				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
						<el-button icon="ele-CopyDocument" text type="warning" v-auth="'eshMapping:add'"
							@click="handleCopy(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="mappingActionHint(row, 'edit')" placement="top" :auto-close="1000">
						<span class="inline-block" :style="{ cursor: isMappingRowLocked(row) ? 'not-allowed' : 'default' }">
							<el-button icon="ele-Edit" text type="primary" :disabled="isMappingRowLocked(row)" v-auth="'eshMapping:update'"
								@click="handleEdit(row)"> </el-button>
						</span>
					</el-tooltip>
					<el-tooltip :content="mappingActionHint(row, 'delete')" placement="top" :auto-close="1000">
						<span class="inline-block" :style="{ cursor: isMappingRowLocked(row) ? 'not-allowed' : 'default' }">
							<el-button icon="ele-Delete" text type="danger" :disabled="isMappingRowLocked(row)" v-auth="'eshMapping:delete'"
								@click="handleDelete(row)"> </el-button>
						</span>
					</el-tooltip>
				</template>
			</vxe-grid>
		</el-card>

		<EditMapping ref="editMappingRef" :title="state.title" :operateType="state.operateType"
			:partnerList="state.partnerList" @handleQuery="handleQuery" />
	</div>
</template>

<script lang="ts" setup name="eshMapping">
import { computed, onMounted, reactive, ref, defineAsyncComponent } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { Local } from '/@/utils/storage';
import EditMapping from './component/editMapping.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import { getAPI } from '/@/utils/axios-utils';
import {
	ShareDataMappingApi,
	ShareDataPacketApi,
	ShareDataPartnerApi,
	ShareDataSubscriptionApi,
	SubSubscriptionState,
	Direction,
	ShareDataPageMappingInput,
	ShareDataPageMappingOutput,
	ShareDataSubscriptionOutput,
} from '/@/api-services/shareData';
import { OperateType } from '/@/types/constants/operate';
import { codeNameLabel, partnerNameOf, packetNameOf } from '/@/utils/sharedata/labelHelper';
const { t: $t } = useI18n();

interface MappingUsageCheckTarget {
	id?: string | null;
	isActive?: boolean | null;
	partnerId?: string | null;
	datatypeId?: string | null;
	direction?: number | null;
}

/**
 * Lấy danh sách toàn bộ các đăng ký chia sẻ đang Active trong hệ thống.
 * Trả về mảng rỗng nếu có lỗi hoặc không có đăng ký nào.
 */
const getActiveSubscriptions = async (): Promise<ShareDataSubscriptionOutput[]> => {
	const res = await getAPI(ShareDataSubscriptionApi).apiSharedataSharedatasubscriptionListGet(
		undefined,
		undefined,
		undefined,
		SubSubscriptionState.NUMBER_1
	);
	const list = res.data.result;
	if (!Array.isArray(list)) throw new Error('Invalid active subscriptions response');
	return list;
};

/**
 * Khớp chuẩn quy tắc Backend (MappingCommandHandler.HasActiveSubscriptionAsync):
 * Hồ sơ ánh xạ chỉ bị coi là "đang dùng" khi:
 * 1. Hồ sơ đang BẬT (isActive === true)
 * 2. Đã có partnerId và datatypeId
 * 3. Tồn tại ít nhất 1 Subscription đang Active (State = 1) có:
 *    - Cùng partnerId
 *    - Cùng datatypeId
 *    - Cùng chiều (hoặc hồ sơ ánh xạ khai chiều Cả hai - Both = 2)
 */
const isMappingInUse = (
	mapping: MappingUsageCheckTarget,
	subs: ShareDataSubscriptionOutput[]
): boolean => {
	if (mapping.isActive !== true) return false;
	if (!mapping.partnerId || !mapping.datatypeId) return false;

	return subs.some((sub) => {
		if (sub.partnerId !== mapping.partnerId) return false;
		if (sub.datatypeId !== mapping.datatypeId) return false;
		return mapping.direction === Direction.NUMBER_2 || sub.direction === mapping.direction;
	});
};

// Chiều dữ liệu — BaseEnums.Direction: Outbound = 0, Inbound = 1, Both = 2.
// TODO: chuyển sang bảng cấu hình dùng chung khi danh mục được seed.
const DIRECTION_LIST = computed(() => [
	{ code: 0, name: $t('lz.label.sharedataMapping.outbound'), tagType: 'primary' },
	{ code: 1, name: $t('lz.label.sharedataMapping.inbound'), tagType: 'success' },
	{ code: 2, name: $t('lz.label.sharedataMapping.both'), tagType: 'warning' },
]);

/** Tra một dòng nhãn để đưa vào TagInfo; trả undefined nếu giá trị không có trong danh mục. */
const getTagItem = (list: any[], value: any) => list.find((i) => i.code === Number(value));
const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));

const xGrid = ref<VxeGridInstance>();
const editMappingRef = ref<InstanceType<typeof EditMapping>>();
const activeSubscriptions = ref<ShareDataSubscriptionOutput[]>([]);

const isMappingRowLocked = (row: any): boolean => {
	if (!row) return false;
	return isMappingInUse(row, activeSubscriptions.value);
};

const mappingActionHint = (row: any, action: 'edit' | 'delete'): string => {
	if (isMappingRowLocked(row)) {
		return action === 'edit'
			? $t('lz.label.sharedataMapping.mappingEditLocked')
			: $t('lz.label.sharedataMapping.mappingDeleteLocked');
	}
	return action === 'edit'
		? $t('lz.button.base.edit')
		: $t('lz.button.base.delete');
};

const mappingSwitchHint = (row: any): string => {
	return isMappingRowLocked(row)
		? $t('lz.label.sharedataMapping.mappingSwitchLocked')
		: '';
};

const state = reactive({
	queryParams: {
		code: undefined as unknown as string,
		name: undefined as unknown as string,
		partnerId: undefined as unknown as string,
		datatypeId: undefined as unknown as string,
		direction: undefined as unknown as number,
		isActive: undefined as unknown as boolean,
	},
	localPageParam: {
		pageSize: 50 as number,
		defaultSort: { field: 'createTime', order: 'desc', descStr: 'desc' } as any,
	},
	visible: false,
	title: '',
	operateType: 'add' as OperateType,
	showBatchDelete: false,
	partnerList: [] as any,
	/** Danh sách gói tin — DatatypeId của hồ sơ lưu ID gói tin (VD PKT101), không phải mã. */
	packetList: [] as any,
});
// Local storage parameters
const localPageParamKey = 'localPageParam:eshMapping';
const options = useVxeTable<ShareDataPageMappingOutput>(
	{
		id: 'eshMapping',
		name: 'Mapping',
		columns: [
			{ type: 'checkbox', fixed: 'left', width: 45, checkMethod: ({ row }) => !isMappingRowLocked(row) },
			{ field: 'code', title: $t('lz.entity.base.code'), minWidth: 150, showOverflow: 'tooltip', sortable: true },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 180, showOverflow: 'tooltip', sortable: true },
			{ field: 'partnerId', title: $t('lz.entity.sharedataMapping.partner'), width: 210, showOverflow: 'tooltip', slots: { default: 'row_partner' } },
			{ field: 'datatypeId', title: $t('lz.entity.sharedataMapping.datatype'), minWidth: 220, showOverflow: 'tooltip', slots: { default: 'row_datatype' } },
			{ field: 'direction', title: $t('lz.entity.sharedataMapping.direction'), width: 100, sortable: true, slots: { default: 'row_direction' } },
			{ field: 'mappedFieldCount', title: $t('lz.entity.sharedataMapping.mappedFieldCount'), width: 100, slots: { default: 'row_item_count' } },
			{ field: 'isActive', title: $t('lz.entity.sharedataMapping.isActive'), width: 100, slots: { default: 'row_active' } },
			{ field: '', title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 130, showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	{
		// Proxy configuration
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		// Sorting configuration
		sortConfig: { defaultSort: state.localPageParam.defaultSort },
		// Pagination configuration
		pagerConfig: { pageSize: state.localPageParam.pageSize },
		// Toolbar configuration
		toolbarConfig: { export: false },
	}
);

// Page initialization
onMounted(async () => {
	state.localPageParam = Local.get(localPageParamKey) || state.localPageParam;
	const resPartner = await getAPI(ShareDataPartnerApi).apiSharedataSharedatapartnerListGet(
		undefined, undefined, undefined, undefined, undefined, true as any
	);
	state.partnerList = resPartner.data.result;

	const resPacket = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketListGet();
	state.packetList = resPacket.data.result ?? [];

	await handleQuery();
});

// Query API
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const sortField = sort?.field || state.localPageParam.defaultSort.field;
	const sortOrder = sort?.order || state.localPageParam.defaultSort.order;

	const params = Object.assign(state.queryParams, { page: page.currentPage, pageSize: page.pageSize, field: sortField, order: sortOrder, descStr: 'desc' }) as ShareDataPageMappingInput;
	const [mappingRes, subs] = await Promise.all([
		getAPI(ShareDataMappingApi).apiSharedataSharedatamappingPagePost(params),
		getActiveSubscriptions(),
	]);
	activeSubscriptions.value = subs;
	return mappingRes;
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
	state.queryParams.code = undefined as unknown as string;
	state.queryParams.name = undefined as unknown as string;
	state.queryParams.partnerId = undefined as unknown as string;
	state.queryParams.datatypeId = undefined as unknown as string;
	state.queryParams.direction = undefined as unknown as number;
	state.queryParams.isActive = undefined as unknown as boolean;
	await xGrid.value?.commitProxy('reload');
};

// Open add dialog
const handleAdd = () => {
	state.title = $t('lz.label.base.titleAdd', { name: $t('lz.label.sharedataMapping.addTitleEntity') });
	state.operateType = 'add';
	editMappingRef.value?.openDialog({ direction: 0, format: 0, isActive: true });
};

// Open copy dialog
const handleCopy = (row: any) => {
	state.title = $t('lz.label.base.titleCopy', { name: $t('lz.label.sharedataMapping.addTitleEntity') });
	state.operateType = 'copy';
	editMappingRef.value?.openDialog(row);
};

// Open edit dialog
const handleEdit = (row: any) => {
	if (isMappingRowLocked(row)) {
		ElMessage.warning($t('lz.label.sharedataMapping.mappingEditLocked'));
		return;
	}
	state.title = $t('lz.label.base.titleEdit', { name: $t('lz.label.sharedataMapping.addTitleEntity') });
	state.operateType = 'edit';
	editMappingRef.value?.openDialog(row);
};

/** Bật/tắt hồ sơ. Backend tự tắt các hồ sơ trùng cặp đối tác + gói tin. */
const handleSetActive = async (row: any, value: boolean) => {
	if (isMappingRowLocked(row)) {
		ElMessage.warning($t('lz.label.sharedataMapping.mappingSwitchLocked'));
		return;
	}
	try {
		await getAPI(ShareDataMappingApi).apiSharedataSharedatamappingSetactivePost({ id: row.id, isActive: value });
		ElMessage.success($t('lz.message.base.saveSuccess'));
	} finally {
		handleQuery();
	}
};

// Delete
const handleDelete = (row: any) => {
	if (isMappingRowLocked(row)) {
		ElMessage.warning($t('lz.label.sharedataMapping.mappingDeleteLocked'));
		return;
	}
	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.name }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(ShareDataMappingApi).apiSharedataSharedatamappingDeletePost({ id: row.id });
			handleQuery();
			ElMessage.success($t('lz.message.base.deleteSuccess'));
		})
		.catch(() => { });
};

//Batch delete
const handleBatchDelete = async () => {
	const selectRecords = getCheckedRecords().filter((r) => !isMappingRowLocked(r));
	if (!selectRecords.length) {
		ElMessage.warning($t('lz.label.sharedataMapping.mappingDeleteLocked'));
		return;
	}
	ElMessageBox.confirm($t('lz.message.base.confirmBatchDelete', { total: selectRecords.length }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(ShareDataMappingApi).apiSharedataSharedatamappingBatchdeletePost(selectRecords.filter((l) => l.id).map((l) => ({ id: l.id })));
			handleQuery();
			ElMessage.success($t('lz.message.base.deleteSuccess'));
		})
		.catch(() => { });
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
const gridEvents: VxeGridListeners<ShareDataPageMappingOutput> = {
	// Only valid when pager-config is configured, this event will be triggered when the page changes
	async pageChange({ pageSize }) {
		state.localPageParam.pageSize = pageSize;
		Local.set(localPageParamKey, state.localPageParam);
	},
	// This event will be triggered when the sorting condition changes
	async sortChange({ field, order }) {
		state.localPageParam.defaultSort = { field: field, order: order!, descStr: 'desc' };
		Local.set(localPageParamKey, state.localPageParam);
	},
};
</script>

<style scoped lang="scss">
/* Bộ lọc: cột không co nhỏ hơn nội dung (nhãn + ô), ô nhập / ô chọn rộng tối thiểu đủ chữ gợi ý khoảng 14 ký tự; thiếu chỗ thì ô lọc tự xuống hàng thay vì bị cắt chữ */
.sd-filter-form {
	:deep(.el-col) {
		min-width: max-content;
	}

	:deep(.el-form-item__content > .el-input) {
		min-width: calc(14ch + 24px);
	}

	:deep(.el-form-item__content > .el-select) {
		min-width: calc(14ch + 48px);
	}
}

/* Chuẩn hóa VXE-Grid Loading & Element Loading — 8-dots spinner bự vừa vặn */
:deep(.vxe-table--loading) {
	.vxe-loading--default-icon {
		display: inline-block;
		width: 50px;
		height: 50px;
		font-size: 0;
		color: transparent;
		background-color: var(--el-color-primary, #409eff);
		mask-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 50 50'%3E%3Ccircle cx='25' cy='6' r='3.4' fill='white' opacity='1'/%3E%3Ccircle cx='38.44' cy='11.56' r='3.4' fill='white' opacity='0.875'/%3E%3Ccircle cx='44' cy='25' r='3.4' fill='white' opacity='0.75'/%3E%3Ccircle cx='38.44' cy='38.44' r='3.4' fill='white' opacity='0.625'/%3E%3Ccircle cx='25' cy='44' r='3.4' fill='white' opacity='0.5'/%3E%3Ccircle cx='11.56' cy='38.44' r='3.4' fill='white' opacity='0.375'/%3E%3Ccircle cx='6' cy='25' r='3.4' fill='white' opacity='0.25'/%3E%3Ccircle cx='11.56' cy='11.56' r='3.4' fill='white' opacity='0.125'/%3E%3C/svg%3E");
		-webkit-mask-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 50 50'%3E%3Ccircle cx='25' cy='6' r='3.4' fill='white' opacity='1'/%3E%3Ccircle cx='38.44' cy='11.56' r='3.4' fill='white' opacity='0.875'/%3E%3Ccircle cx='44' cy='25' r='3.4' fill='white' opacity='0.75'/%3E%3Ccircle cx='38.44' cy='38.44' r='3.4' fill='white' opacity='0.625'/%3E%3Ccircle cx='25' cy='44' r='3.4' fill='white' opacity='0.5'/%3E%3Ccircle cx='11.56' cy='38.44' r='3.4' fill='white' opacity='0.375'/%3E%3Ccircle cx='6' cy='25' r='3.4' fill='white' opacity='0.25'/%3E%3Ccircle cx='11.56' cy='11.56' r='3.4' fill='white' opacity='0.125'/%3E%3C/svg%3E");
		mask-repeat: no-repeat;
		-webkit-mask-repeat: no-repeat;
		mask-position: center;
		-webkit-mask-position: center;
		mask-size: contain;
		-webkit-mask-size: contain;
		animation: sharedata-roll 0.9s linear infinite;
		transform-origin: center;
		will-change: transform;

		&::before {
			display: none;
		}
	}
	.vxe-loading--text {
		font-size: 14px;
		font-weight: 500;
		color: var(--el-color-primary, #409eff);
		padding-top: 10px;
	}
}

:deep(.el-loading-mask) {
	background-color: rgba(255, 255, 255, 0.78);
	backdrop-filter: blur(1px);
	border-radius: inherit;
	transition: opacity 0.25s ease;
	z-index: 100;
}

:deep(.el-loading-spinner) {
	top: 50%;
	transform: translateY(-50%);
	margin-top: 0;
	width: 100%;
	text-align: center;
	position: absolute;

	.circular {
		display: inline-block;
		width: 50px;
		height: 50px;
		background-color: var(--el-color-primary, #409eff);
		mask-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 50 50'%3E%3Ccircle cx='25' cy='6' r='3.4' fill='white' opacity='1'/%3E%3Ccircle cx='38.44' cy='11.56' r='3.4' fill='white' opacity='0.875'/%3E%3Ccircle cx='44' cy='25' r='3.4' fill='white' opacity='0.75'/%3E%3Ccircle cx='38.44' cy='38.44' r='3.4' fill='white' opacity='0.625'/%3E%3Ccircle cx='25' cy='44' r='3.4' fill='white' opacity='0.5'/%3E%3Ccircle cx='11.56' cy='38.44' r='3.4' fill='white' opacity='0.375'/%3E%3Ccircle cx='6' cy='25' r='3.4' fill='white' opacity='0.25'/%3E%3Ccircle cx='11.56' cy='11.56' r='3.4' fill='white' opacity='0.125'/%3E%3C/svg%3E");
		-webkit-mask-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 50 50'%3E%3Ccircle cx='25' cy='6' r='3.4' fill='white' opacity='1'/%3E%3Ccircle cx='38.44' cy='11.56' r='3.4' fill='white' opacity='0.875'/%3E%3Ccircle cx='44' cy='25' r='3.4' fill='white' opacity='0.75'/%3E%3Ccircle cx='38.44' cy='38.44' r='3.4' fill='white' opacity='0.625'/%3E%3Ccircle cx='25' cy='44' r='3.4' fill='white' opacity='0.5'/%3E%3Ccircle cx='11.56' cy='38.44' r='3.4' fill='white' opacity='0.375'/%3E%3Ccircle cx='6' cy='25' r='3.4' fill='white' opacity='0.25'/%3E%3Ccircle cx='11.56' cy='11.56' r='3.4' fill='white' opacity='0.125'/%3E%3C/svg%3E");
		mask-repeat: no-repeat;
		-webkit-mask-repeat: no-repeat;
		mask-position: center;
		-webkit-mask-position: center;
		mask-size: contain;
		-webkit-mask-size: contain;
		animation: sharedata-roll 0.9s linear infinite;
		transform-origin: center;
		will-change: transform;

		.path {
			display: none;
		}
	}

	.el-loading-text {
		color: var(--el-color-primary, #409eff);
		font-size: 14px;
		font-weight: 500;
		letter-spacing: 0.3px;
		margin: 10px 0 0;
		line-height: 1.4;
	}

	&:not(:has(.el-loading-text))::after {
		content: 'Đang tải...';
		display: block;
		color: var(--el-color-primary, #409eff);
		font-size: 14px;
		font-weight: 500;
		letter-spacing: 0.3px;
		margin-top: 10px;
		line-height: 1.4;
	}
}

@keyframes sharedata-roll {
	from {
		transform: rotate(0deg);
	}
	to {
		transform: rotate(360deg);
	}
}
</style>
