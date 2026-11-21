<template>
	<div class="sharedata-cursor-container">
		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px' }">
			<el-form class="sd-filter-form" label-width="auto" label-position="left">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.sharedataCursor.packet')">
							<el-select
								v-model="state.packetId"
								class="w100 sd-cursor-packet"
								filterable
								:disabled="options.loading"
								:placeholder="$t('lz.placeholder.sharedataCursor.packet')"
								@change="handleQuery"
							>
								<el-option v-for="item in state.packets" :key="item.id" :label="codeNameLabel(item)" :value="item.id" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5 sd-cursor-note" :xs="24" :sm="12" :md="16" :lg="18" :xl="18">
						{{ $t('lz.label.sharedataSqlVariable.partnerCursorDrawerHint') }}
					</el-col>
				</el-row>
			</el-form>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options">
				<template #toolbar_tools>
					<TableHeader :show-add="false" :show-delete="false" @refresh="handleQuery" @custom-column="handleCustom" />
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>

				<template #row_sqlMode="{ row }">
					<el-tag size="small" :type="row.isCustom ? 'warning' : 'info'" effect="plain">
						{{ $t(row.isCustom ? 'lz.label.sharedataDataSource.tagCustomSql' : 'lz.label.sharedataDataSource.tagSharedSql') }}
					</el-tag>
				</template>

				<template #row_cursor="{ row }">
					<span v-if="row.savedValue" class="sd-cursor-saved">{{ row.savedValue }}</span>
					<span v-else class="sd-cursor-none">{{ $t('lz.label.sharedataCursor.noSaved') }}</span>
				</template>

				<template #row_buttons="{ row }">
					<el-tooltip :content="setTooltip(row)" placement="top">
						<span class="sd-cursor-btn-wrapper">
							<el-button text type="primary" size="small" class="sd-cursor-set-btn" :disabled="row.sections.length === 0 || isLocked(row)" @click="openSetCursor(row)">
								✎ {{ $t('lz.button.sharedataSqlVariable.setCursor') }}
							</el-button>
						</span>
					</el-tooltip>
					<el-tooltip :content="resetTooltip(row)" placement="top">
						<span class="sd-cursor-btn-wrapper">
							<el-button
								text
								type="primary"
								size="small"
								class="sd-cursor-reset-btn"
								:disabled="!row.savedValue || isLocked(row)"
								:loading="state.resettingPartnerId === row.partnerId"
								@click="handleReset(row)"
							>
								↺ {{ $t('lz.button.sharedataSqlVariable.resetCursor') }}
							</el-button>
						</span>
					</el-tooltip>
				</template>
			</vxe-grid>
		</el-card>

		<SetCursorDialog ref="setCursorRef" @handleQuery="handleQuery" />
	</div>
</template>

<script lang="ts" setup name="sharedataCursor">
import { computed, defineAsyncComponent, onMounted, reactive, ref } from 'vue';
import SetCursorDialog from './component/setCursorDialog.vue';
import { useI18n } from 'vue-i18n';
import { ElMessage, ElMessageBox } from 'element-plus';
import type { VxeGridInstance } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { getAPI } from '/@/utils/axios-utils';
import { ShareDataPacketApi } from '/@/api-services/shareData';
import type { ShareDataPagePacketOutput } from '/@/api-services/shareData';
import { SharedataDirectionEnum } from '/@/types/enums';
import { codeNameLabel } from '/@/utils/sharedata/labelHelper';
import { listActiveSubscriptions } from '/@/utils/sharedata/packetUsage';
import { type CursorSection, buildCursorSections, cursorSavedText } from '/@/utils/sharedata/cursorSections';

const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));

/* Giữ khớp ShareDataConst.KindBuiltin (BE) */
const KIND_BUILTIN = 'Builtin';
/* Danh mục gói tin nhỏ nên nạp một lượt cho ô chọn */
const PACKET_PAGE_SIZE = 500;

interface PartnerCursorRow {
	partnerId: string;
	partnerCode?: string | null;
	partnerName?: string | null;
	isCustom: boolean;
	savedValue: string | null;
	sections: CursorSection[];
}

const { t: $t } = useI18n();
const xGrid = ref<VxeGridInstance>();
const setCursorRef = ref();

const state = reactive({
	packets: [] as ShareDataPagePacketOutput[],
	packetId: undefined as string | undefined,
	lockedPartnerIds: [] as string[],
	resettingPartnerId: null as string | null,
});

const currentPacket = computed(() => state.packets.find((p) => p.id === state.packetId));

const partnerLabel = (row: PartnerCursorRow) => [row.partnerCode, row.partnerName].filter(Boolean).join(' — ');

const options = useVxeTable<PartnerCursorRow>(
	{
		id: 'sharedataCursor',
		name: 'SendCursor',
		columns: [
			{ field: 'partnerCode', title: $t('lz.label.sharedataDataSource.partnerSqlColPartner'), minWidth: 220, headerAlign: 'center', align: 'left', showOverflow: 'tooltip', formatter: ({ row }: any) => partnerLabel(row) },
			{ field: 'isCustom', title: $t('lz.label.sharedataSqlVariable.colSqlMode'), width: 130, headerAlign: 'center', align: 'center', slots: { default: 'row_sqlMode' } },
			{ field: 'savedValue', title: $t('lz.label.sharedataSqlVariable.colPartnerCursor'), minWidth: 360, headerAlign: 'center', align: 'left', slots: { default: 'row_cursor' } },
			{ title: $t('lz.label.sharedataSqlVariable.colAction'), fixed: 'right', width: 190, headerAlign: 'center', align: 'center', showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	{
		proxyConfig: { autoLoad: false, ajax: { query: () => handleQueryApi() } },
		pagerConfig: { enabled: false },
		toolbarConfig: { export: false },
		rowConfig: { keyField: 'partnerId' },
	}
);

/** Đối tác có đăng ký gửi đang bật: BE chặn đặt mốc, đặt lại mốc của họ (ESH-2306). Không tải được thì không khóa; BE vẫn chặn khi lưu. */
const loadLockedPartnerIds = async (packet: ShareDataPagePacketOutput): Promise<string[]> => {
	try {
		const subs = await listActiveSubscriptions(packet.id!, packet.code);
		return [...new Set(subs.filter((s) => Number(s.direction) === SharedataDirectionEnum.Outbound || Number(s.direction) === SharedataDirectionEnum.Both).map((s) => s.partnerId!))];
	} catch {
		return [];
	}
};

const loadPartnerCursor = async (packetCode: string, partner: { partnerId?: string | null; partnerCode?: string | null; partnerName?: string | null; isCustom?: boolean }): Promise<PartnerCursorRow> => {
	let sections: CursorSection[] = [];
	try {
		const res = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketGetsqlvariablesGet(packetCode, partner.partnerId!);
		const items = res.data?.result?.items ?? [];
		const builtinItems = items.filter((i: any) => i.valueKind === KIND_BUILTIN);
		sections = buildCursorSections(builtinItems.map((i: any) => ({ code: i.code ?? '', source: i.value ?? '', savedValue: i.savedValue })));
	} catch {
		// Lỗi đã được interceptor báo; dòng đối tác này hiện như chưa có mốc
	}
	return {
		partnerId: partner.partnerId!,
		partnerCode: partner.partnerCode,
		partnerName: partner.partnerName,
		isCustom: Boolean(partner.isCustom),
		savedValue: cursorSavedText(sections),
		sections,
	};
};

const handleQueryApi = async (): Promise<{ data: { result: PartnerCursorRow[] } }> => {
	const packet = currentPacket.value;
	if (!packet?.code) return { data: { result: [] } };
	const [partnersRes, lockedPartnerIds] = await Promise.all([getAPI(ShareDataPacketApi).apiSharedataSharedatapacketGetpartnersqllistGet(packet.code), loadLockedPartnerIds(packet)]);
	const partners = (partnersRes.data.result ?? []).filter((p) => Boolean(p.partnerId));
	const rows = await Promise.all(partners.map((p) => loadPartnerCursor(packet.code!, p)));
	state.lockedPartnerIds = lockedPartnerIds;
	return { data: { result: rows } };
};

const handleQuery = async () => {
	options.loading = true;
	try {
		await xGrid.value?.commitProxy('reload');
	} finally {
		options.loading = false;
	}
};

const handleCustom = () => xGrid.value?.openCustom();

const isLocked = (row: PartnerCursorRow) => state.lockedPartnerIds.includes(row.partnerId);

const setTooltip = (row: PartnerCursorRow) => {
	if (isLocked(row)) return $t('lz.label.sharedataDataSource.cursorLockedBySubscription');
	return $t(row.sections.length > 0 ? 'lz.tooltip.sharedataSqlVariable.setCursorBtn' : 'lz.tooltip.sharedataSqlVariable.setDisabledNoCursorVars');
};

const resetTooltip = (row: PartnerCursorRow) => {
	if (isLocked(row)) return $t('lz.label.sharedataDataSource.cursorLockedBySubscription');
	return $t(row.savedValue ? 'lz.tooltip.sharedataCursor.resetBtn' : 'lz.tooltip.sharedataSqlVariable.resetDisabledNoSaved');
};

const openSetCursor = (row: PartnerCursorRow) => {
	setCursorRef.value?.openDialog(currentPacket.value!.code, {
		partnerId: row.partnerId,
		partnerName: row.partnerName || row.partnerCode || row.partnerId,
		sections: row.sections,
	});
};

const handleReset = async (row: PartnerCursorRow) => {
	try {
		await ElMessageBox.confirm($t('lz.confirm.sharedataSqlVariable.resetSavedValue'), $t('lz.label.base.notification'), {
			type: 'warning',
			confirmButtonText: $t('lz.button.base.confirm'),
			cancelButtonText: $t('lz.button.base.cancel'),
		});
	} catch {
		return;
	}
	state.resettingPartnerId = row.partnerId;
	try {
		await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketResetlastsendPost({ packetCode: currentPacket.value!.code!, partnerId: row.partnerId });
		ElMessage.success($t('lz.message.sharedataSqlVariable.resetSavedSuccess'));
		await handleQuery();
	} catch {
		// Lỗi đã được interceptor báo
	} finally {
		state.resettingPartnerId = null;
	}
};

onMounted(async () => {
	const res = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketPagePost({ page: 1, pageSize: PACKET_PAGE_SIZE, field: 'orderNo', order: 'asc', descStr: 'desc' });
	state.packets = res.data.result?.records ?? [];
	state.packetId = state.packets[0]?.id ?? undefined;
	await handleQuery();
});
</script>

<style scoped lang="scss">
.sharedata-cursor-container {
	display: flex;
	flex-direction: column;
	height: 100%;
}

.full-table {
	flex: 1;
	min-height: 0;

	:deep(.el-card__body) {
		height: 100%;
		padding: 5px;
	}
}

.sd-cursor-note {
	display: flex;
	align-items: center;
	font-size: 12px;
	line-height: 1.45;
	color: var(--el-text-color-secondary);
}

.sd-cursor-saved {
	font-family: var(--el-font-family-mono, monospace);
	font-size: 12px;
	word-break: break-all;
}

.sd-cursor-none {
	color: var(--el-text-color-secondary);
}

.sd-cursor-btn-wrapper {
	display: inline-block;

	& + & {
		margin-left: 4px;
	}
}
</style>
