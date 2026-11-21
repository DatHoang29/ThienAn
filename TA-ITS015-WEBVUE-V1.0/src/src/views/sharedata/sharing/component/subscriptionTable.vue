<template>
	<div class="sub-table">
		<el-tabs v-model="directionTab" class="sub-direction-tabs">
			<el-tab-pane name="outbound" :label="$t('lz.label.sharedataSubscription.outbound')" />
			<el-tab-pane name="inbound" :label="$t('lz.label.sharedataSubscription.inbound')" />
		</el-tabs>
		<!-- Grid needs its own parent: vxe resolves height 100% from the parent, which would otherwise include the tabs -->
		<div class="sub-table__grid">
		<vxe-grid ref="xGrid" height="100%" class="xGrid-style" v-bind="options">
			<template #toolbar_tools>
				<el-button v-if="canOperate" type="primary" size="small" icon="ele-Plus" v-auth="'sharedataSharing:add'"
					@click="emit('add', activeTab)">
					{{ activeTab === SharedataDirectionEnum.Outbound ? $t('lz.button.sharedataSubscription.createOutbound') : $t('lz.button.sharedataSubscription.createInbound') }}
				</el-button>
			</template>

			<template #empty>
				<el-empty :description="$t('lz.label.sharedataSubscription.noSubscription')" :image-size="80" />
			</template>

			<template #row_datatype="{ row }">
				<span>{{ packetNameOf(packetList, row.datatypeId) }}</span>
			</template>

			<template #row_mode="{ row }">
				<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SharedataSubMode, row.mode)" />
				<span v-if="row.schedule" class="sub-table__sched">{{ scheduleSummary(row.schedule) }}</span>
			</template>

			<template #row_state="{ row }">
				<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SharedataSubState, row.state)" />
			</template>

			<!-- Tình trạng hồ sơ ánh xạ: backend SUY SỐNG mỗi lần nạp lưới theo
			     (đối tác × gói tin × chiều), không đọc cột nào trong CSDL — nên luôn khớp với
			     hồ sơ mà worker thực dùng. Ba trạng thái cần ba hành động khác nhau, xem tooltip. -->
			<template #row_mapping="{ row }">
				<el-tooltip placement="top" popper-class="subscription-table-tooltip">
					<template #content>
						<div style="white-space: pre-line">{{ mappingHint(row) }}</div>
					</template>
					<span class="sub-table__mapping">
						<el-tag :type="mappingTagType(row)" size="small" effect="plain">{{ mappingStateText(row) }}</el-tag>
						<span v-if="row.mappingName || row.mappingCode" class="sub-table__mapping-name">
							{{ row.mappingName || row.mappingCode }}
						</span>
					</span>
				</el-tooltip>
			</template>

			<template #row_toggle="{ row }">
				<el-tooltip placement="top" popper-class="subscription-table-tooltip">
					<template #content>
						<div style="white-space: pre-line">{{ toggleTitle(row) }}</div>
					</template>
					<el-switch :model-value="row.state === SharedataSubStateEnum.Active"
						:disabled="!canOperate || !canToggle(row)"
						@change="(v: string | number | boolean) => emit('toggle', row, Boolean(v))" />
				</el-tooltip>
			</template>
			<template #row_record="{ row }">
				<ModifyRecord :data="row" />
			</template>
			<template #row_buttons="{ row }">
				<!-- BE chặn sửa / xoá đăng ký đang chạy: khoá nút, tooltip nhắc tạm dừng trước -->
				<el-tooltip
					:content="isRunning(row) ? $t('lz.label.sharedataSubscription.pauseBeforeEdit') : $t('lz.button.base.edit')"
					placement="top"
					popper-class="subscription-table-tooltip"
				>
					<span v-if="canOperate && isAlive(row)">
						<el-button size="small" type="primary" text icon="ele-Edit" :disabled="isRunning(row)"
							:aria-label="$t('lz.button.base.edit')" @click="emit('edit', row)"></el-button>
					</span>
				</el-tooltip>
				<el-tooltip
					:content="isRunning(row) ? $t('lz.label.sharedataSubscription.pauseBeforeEdit') : $t('lz.button.base.delete')"
					placement="top"
					popper-class="subscription-table-tooltip"
				>
					<span v-if="canOperate && isAlive(row)">
						<el-button size="small" type="danger" text icon="ele-Delete" :disabled="isRunning(row)"
							:aria-label="$t('lz.button.base.delete')" @click="emit('cancel', row)"></el-button>
					</span>
				</el-tooltip>
				<span v-if="!canOperate || !isAlive(row)" class="sub-table__muted">—</span>
			</template>
		</vxe-grid>
		</div>
	</div>
</template>

<script lang="ts" setup name="sharedataSubscriptionTable">
import { computed, defineAsyncComponent, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import type { VxeGridInstance } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { BaseConfigTypeEnum, SharedataDirectionEnum, SharedataSubMappingStateEnum, SharedataSubStateEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import { ShareDataPacketApi } from '/@/api-services/shareData';
import type { ShareDataScheduleDto, ShareDataSubscriptionOutput } from '/@/api-services/shareData';
import { getAPI } from '/@/utils/axios-utils';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import { packetNameOf } from '/@/utils/sharedata/labelHelper';

const { t: $t } = useI18n();

const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;

const ALIVE_STATES: number[] = [SharedataSubStateEnum.Pending, SharedataSubStateEnum.Active, SharedataSubStateEnum.Paused];

/**
 * Tên gói tin lấy thẳng từ danh sách gói tin (ShareDataPacket).
 * DatatypeId của đăng ký lưu ID gói tin nên tra theo ID; không còn dùng danh mục shareData_type.
 */
const packetList = ref<any[]>([]);


const onWindowResize = () => {
	xGrid.value?.recalculate(true);
};

onMounted(async () => {
	window.addEventListener('resize', onWindowResize);
	try {
		const res = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketListGet();
		packetList.value = res.data.result ?? [];
	} catch {
		packetList.value = [];
	}
	await nextTick();
	xGrid.value?.recalculate(true);
});

onBeforeUnmount(() => {
	window.removeEventListener('resize', onWindowResize);
});

interface Props {
	subscriptions: ShareDataSubscriptionOutput[];
	canOperate: boolean;
	partnerConnected?: boolean;
}
const props = defineProps<Props>();

interface Emits {
	(e: 'add', direction: number): void;
	(e: 'edit', row: ShareDataSubscriptionOutput): void;
	(e: 'cancel', row: ShareDataSubscriptionOutput): void;
	(e: 'toggle', row: ShareDataSubscriptionOutput, enabled: boolean): void;
	(e: 'preview', row: ShareDataSubscriptionOutput): void;
	(e: 'export', row: ShareDataSubscriptionOutput): void;
}
const emit = defineEmits<Emits>();

function isAlive(row: any): boolean {
	return ALIVE_STATES.includes(row.state);
}

function isRunning(row: any): boolean {
	return row.state === SharedataSubStateEnum.Active;
}

/* ---- Tình trạng hồ sơ ánh xạ ---- */

/** Đang tắt cũng coi như chưa dùng được: worker chỉ lấy hồ sơ đang bật. */
const isMappingReady = (row: any) => Number(row?.mappingState) === SharedataSubMappingStateEnum.Ready;

const mappingStateText = (row: any) => {
	switch (Number(row?.mappingState)) {
		case SharedataSubMappingStateEnum.Ready:
			return $t('lz.label.sharedataSubscription.mappingReady');
		case SharedataSubMappingStateEnum.Disabled:
			return $t('lz.label.sharedataSubscription.mappingDisabled');
		default:
			return $t('lz.label.sharedataSubscription.mappingNone');
	}
};

const mappingTagType = (row: any): 'success' | 'warning' | 'danger' => {
	switch (Number(row?.mappingState)) {
		case SharedataSubMappingStateEnum.Ready:
			return 'success';
		case SharedataSubMappingStateEnum.Disabled:
			return 'warning';
		default:
			return 'danger';
	}
};

/** Nói luôn việc phải làm, vì hai trạng thái chưa dùng được cần hai hành động khác nhau. */
const mappingHint = (row: any) => {
	switch (Number(row?.mappingState)) {
		case SharedataSubMappingStateEnum.Ready:
			return $t('lz.label.sharedataSubscription.mappingReadyHint');
		case SharedataSubMappingStateEnum.Disabled:
			return $t('lz.label.sharedataSubscription.mappingDisabledHint');
		default:
			return $t('lz.label.sharedataSubscription.mappingNoneHint');
	}
};

/** Cùng đối tác, chiều và gói tin chỉ được một đăng ký chạy: có cái khác đang chạy thì không bật thêm cái này. */
function hasOtherActive(row: any): boolean {
	if (row.state === SharedataSubStateEnum.Active) return false;
	return props.subscriptions.some(
		(s) => s.id !== row.id && s.direction === row.direction && s.datatypeId === row.datatypeId && s.state === SharedataSubStateEnum.Active
	);
}

function canToggle(row: any): boolean {
	// Tạm dừng đăng ký đang chạy: BE không đặt điều kiện.
	if (row.state === SharedataSubStateEnum.Active) return true;
	if (row.state !== SharedataSubStateEnum.Paused && row.state !== SharedataSubStateEnum.Pending) return false;
	// Bật lại (Resume) — khớp BE: đối tác đã kết nối, hồ sơ ánh xạ đang bật, không có đăng ký trùng đang chạy.
	// Khoá sẵn công tắc để người dùng không bấm vào chỗ chắc chắn lỗi.
	if (!isMappingReady(row)) return false;
	if (hasOtherActive(row)) return false;
	return Boolean(props.partnerConnected);
}

function toggleTitle(row: any): string {
	if (row.state === SharedataSubStateEnum.Active) {
		return activeTab.value === SharedataDirectionEnum.Inbound ? $t('lz.label.sharedataSubscription.toggleCollect') : $t('lz.label.sharedataSubscription.toggleSend');
	}
	if (!isMappingReady(row)) return mappingHint(row);
	if (hasOtherActive(row)) return $t('lz.label.sharedataSubscription.duplicateActiveHint');
	if (!props.partnerConnected) {
		return $t('lz.label.sharedataSubscription.needConnectPartnerFirst');
	}
	return activeTab.value === SharedataDirectionEnum.Inbound ? $t('lz.label.sharedataSubscription.toggleCollect') : $t('lz.label.sharedataSubscription.toggleSend');
}

const xGrid = ref<VxeGridInstance>();
const activeTab = ref<number>(SharedataDirectionEnum.Outbound);
const directionTab = computed<string>({
	get: () => (activeTab.value === SharedataDirectionEnum.Inbound ? 'inbound' : 'outbound'),
	set: (v) => {
		activeTab.value = v === 'inbound' ? SharedataDirectionEnum.Inbound : SharedataDirectionEnum.Outbound;
	},
});

const rows = computed(() => props.subscriptions.filter((s) => s.direction === (activeTab.value as any)));

const options = useVxeTable<ShareDataSubscriptionOutput>(
	{
		id: 'sharedataSubscription',
		name: 'ShareDataSubscription',
		columns: [
			{
				field: 'datatypeId',
				title: $t('lz.entity.sharedataSubscription.datatypeId'),
				minWidth: 220,
				align: 'left',
				headerAlign: 'center',
				showOverflow: 'tooltip',
				slots: { default: 'row_datatype' },
				formatter: ({ cellValue }: any) => packetNameOf(packetList.value, cellValue),
			},
			{ field: 'mode', title: $t('lz.entity.sharedataSubscription.mode'), minWidth: 120, align: 'left', headerAlign: 'center', slots: { default: 'row_mode' } },
			{
				field: 'mappingState',
				title: $t('lz.entity.sharedataSubscription.mappingState'),
				minWidth: 190,
				align: 'left',
				headerAlign: 'center',
				showOverflow: 'tooltip',
				slots: { default: 'row_mapping' },
			},
			{ title: $t('lz.label.sharedataSubscription.toggle'), width: 90, align: 'center', slots: { default: 'row_toggle' } },
			{ field: 'state', title: $t('lz.entity.sharedataSubscription.state'), width: 110, slots: { default: 'row_state' } },
			{ field: '', title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 100, slots: { default: 'row_buttons' } },
		],
	},
	{
		height: '100%',
		autoResize: true,
		data: [],
		proxyConfig: { enabled: false },
		pagerConfig: { enabled: false },
		checkboxConfig: { range: false },
		toolbarConfig: { export: false, print: false, zoom: false, custom: false, slots: { tools: 'toolbar_tools' } },
		tooltipConfig: { popupClassName: 'subscription-table-tooltip' },
	}
);

watch(
	rows,
	async (val) => {
		options.data = (val as ShareDataSubscriptionOutput[]).slice();
		await nextTick();
		xGrid.value?.recalculate(true);
	},
	{ immediate: true, deep: true }
);

watch(packetList, () => {
	if (rows.value?.length) {
		options.data = (rows.value as ShareDataSubscriptionOutput[]).slice();
		nextTick().then(() => xGrid.value?.recalculate(true));
	}
});

function scheduleSummary(sc: ShareDataScheduleDto): string {
	// Bản ghi mới dùng key `intervalSeconds`; bản ghi cũ còn `updateDelaySec` (không còn trong DTO).
	const interval = sc?.intervalSeconds ?? (sc as any)?.updateDelaySec;
	if (sc.kind === 'continuous') return ` ${interval}s`;
	return ` ${(sc.daysOfWeek ?? []).join('/')}`;
}
</script>

<style lang="scss">
/* Tooltips render in <body>: styled here, not in the scoped block */
.el-popper.subscription-table-tooltip {
	box-sizing: border-box;
	width: max-content;
	max-width: 28vw;
	line-height: 1.5;
	white-space: pre-line;
	overflow-wrap: anywhere;
}

.vxe-tooltip--wrapper.subscription-table-tooltip .vxe-tooltip--content {
	box-sizing: border-box;
	max-width: 28vw;
	overflow-wrap: anywhere;
}
</style>

<style scoped lang="scss">
/* Bọc ngoài phải cao 100% để `height: '100%'` của vxe-grid có mốc tính. */
.sub-table {
	height: 100%;
	display: flex;
	flex-direction: column;
	min-height: 0;
}

.sub-table__grid {
	flex: 1 1 0%;
	min-height: 0;
	display: flex;
	flex-direction: column;
}

.sub-direction-tabs {
	flex-shrink: 0;
	margin-bottom: 8px;

	:deep(.el-tabs__header) {
		margin: 0;
	}

	:deep(.el-tabs__nav-wrap) {
		margin-bottom: 0;

		&::after {
			height: 1px;
			background-color: var(--el-border-color-lighter);
		}
	}

	:deep(.el-tabs__content) {
		display: none;
		padding: 0;
	}

	:deep(.el-tabs__item) {
		padding: 0 8px;
		height: 32px;
		line-height: 32px;
		font-size: 13px;

		&.is-active {
			font-weight: 600;
			color: var(--el-color-primary);
		}
	}

	:deep(.el-tabs__active-bar) {
		background-color: var(--el-color-primary);
		height: 2px;
	}
}

.sub-table :deep(.vxe-grid) {
	flex: 1 1 0%;
	min-height: 0;
	height: 100%;
	width: 100%;
	display: flex;
	flex-direction: column;
	overflow: hidden;

	.vxe-grid--layout-body-wrapper,
	.vxe-grid--layout-body-content-wrapper {
		flex: 1 1 0%;
		min-height: 0;
		height: 100%;
		display: flex;
		flex-direction: column;
		overflow: hidden;
	}

	.vxe-grid--toolbar-wrapper {
		flex-shrink: 0;
	}

	.vxe-toolbar {
		padding: 4px 0;
	}

	.vxe-grid--table-container {
		flex: 1 1 0%;
		min-height: 0;
		height: 100%;
		display: flex;
		flex-direction: column;
		overflow: hidden;

		.vxe-grid--table-wrapper {
			flex: 1 1 0%;
			min-height: 0;
			height: 100%;
			display: flex;
			flex-direction: column;
			overflow: hidden;
		}
	}

	.vxe-table {
		flex: 1 1 0%;
		min-height: 0;
		height: 100%;
		display: flex;
		flex-direction: column;

		.vxe-table--render-wrapper,
		.vxe-table--main-wrapper {
			flex: 1 1 0% !important;
			min-height: 0 !important;
			height: 100% !important;
			display: flex !important;
			flex-direction: column !important;
		}

		.vxe-table--body-wrapper.body--wrapper {
			flex: 1 1 0% !important;
			min-height: 0 !important;
			height: 100% !important;
			overflow-y: auto;
			overflow-x: auto;
		}

		&.is--empty .vxe-table--empty-placeholder {
			height: 100% !important;
			min-height: 200px;
			display: flex;
			align-items: center;
			justify-content: center;
		}
	}
}

.sub-table__sched {
	margin-left: 4px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.sub-table__muted {
	color: var(--el-text-color-secondary);
}

/* Cột hồ sơ ánh xạ: tag trạng thái trước, tên hồ sơ theo sau và co lại khi cột hẹp. */
.sub-table__mapping {
	display: inline-flex;
	align-items: center;
	gap: 6px;
	max-width: 100%;
	overflow: hidden;
}

.sub-table__mapping-name {
	font-size: 12px;
	color: var(--el-text-color-secondary);
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

:deep(.vxe-cell.c--tooltip) {
	.cursor-default {
		display: inline-flex;
		align-items: center;
		gap: 4px;
	}
}
</style>
