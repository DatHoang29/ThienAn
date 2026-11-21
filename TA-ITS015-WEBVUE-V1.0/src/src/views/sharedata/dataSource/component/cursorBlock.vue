<template>
	<!-- Khối 4: mốc đang chạy theo từng đối tác trong phạm vi tab đang mở (tab Mẫu chung: mọi đối tác dùng câu mẫu chung, mỗi đối tác một dòng; tab đối tác: đúng đối tác đó).
	     Worker tự ghi mốc sau mỗi lượt gửi thành công; ở đây xem, Đặt mốc, Đặt lại. Mốc bắt đầu của câu SQL nằm ở khối 3 ngay trên. -->
	<div class="sd-cursor-block">
		<div class="scb-card" :class="{ 'is-scope-locked': scopeLocked }">
			<div class="scb-header" :class="{ 'is-collapsed': collapsed }">
				<div class="scb-header-left" @click="toggleCollapsed">
					<span class="scb-title">{{ title }}</span>
					<span class="scb-badge-count">{{ scopePartners.length }}</span>
				</div>
				<el-tooltip :content="collapsed ? $t('lz.button.sharedataDataSource.sqlEditorExpandBtn') : $t('lz.button.sharedataDataSource.sqlEditorCollapseBtn')" placement="top">
					<el-button
						text
						size="small"
						type="primary"
						class="scb-collapse-btn"
						:icon="collapsed ? 'ele-ArrowDown' : 'ele-ArrowUp'"
						:aria-label="collapsed ? $t('lz.button.sharedataDataSource.sqlEditorExpandBtn') : $t('lz.button.sharedataDataSource.sqlEditorCollapseBtn')"
						@click="toggleCollapsed"
					/>
				</el-tooltip>
			</div>

			<el-collapse-transition>
				<div v-show="!collapsed">
					<el-table
						:data="state.rows"
						size="small"
						border
						style="width: 100%"
						class="scb-table"
						row-key="partnerId"
						:row-class-name="({ row }: { row: PartnerCursorRow }) => (isLocked(row) ? 'is-row-locked' : '')"
						:empty-text="$t('lz.label.sharedataCursor.emptyCommon')"
						:tooltip-options="{ popperClass: 'cursor-block-tooltip' }"
					>
						<el-table-column :label="$t('lz.label.sharedataDataSource.partnerSqlColPartner')" min-width="100" header-align="center" show-overflow-tooltip>
							<template #default="{ row }">{{ partnerLabel(row) }}</template>
						</el-table-column>

						<el-table-column :label="$t('lz.label.sharedataSqlVariable.colPartnerCursor')" min-width="180" header-align="center">
							<template #default="{ row }">
								<!-- Mỗi nhóm mốc một dòng: có mốc thì hiện nguyên chuỗi mốc đang lưu;
								     chưa có thì nói lượt gửi tới lấy từ đâu theo Mốc bắt đầu của câu SQL ở khối 3 -->
								<div v-if="row.sections.length > 0" class="sd-cursor-saved">
									<div v-for="line in cursorLines(row)" :key="line.group ?? ''" class="sd-cursor-line">
										<span v-if="line.group" class="sd-cursor-group">{{ line.group }}:</span>
										<span class="sd-cursor-value">{{ line.text }}</span>
									</div>
								</div>
								<span v-else>{{ $t('lz.label.sharedataCursor.noSaved') }}</span>
							</template>
						</el-table-column>

						<el-table-column :label="$t('lz.label.sharedataSqlVariable.colAction')" width="100" header-align="center" align="center">
							<template #default="{ row }">
								<!-- Nút icon có tooltip theo quy ước chung các bảng khác trong phân hệ -->
								<div class="sd-cursor-actions">
									<el-tooltip
										:content="setTooltip(row)"
										placement="top"
										popper-class="cursor-block-tooltip"
									>
										<span class="sd-cursor-btn-wrapper">
											<el-button
												text
												type="primary"
												size="small"
												icon="ele-Edit"
												class="sd-cursor-set-btn"
												:disabled="row.sections.length === 0 || isLocked(row)"
												:aria-label="$t('lz.button.sharedataSqlVariable.setCursor')"
												@click="openSetCursor(row)"
											/>
										</span>
									</el-tooltip>
									<el-tooltip
										:content="resetTooltip(row)"
										placement="top"
										popper-class="cursor-block-tooltip"
									>
										<span class="sd-cursor-btn-wrapper">
											<el-button
												text
												type="primary"
												size="small"
												icon="ele-RefreshLeft"
												class="sd-cursor-reset-btn"
												:disabled="!row.savedValue || isLocked(row)"
												:loading="state.resettingPartnerId === row.partnerId"
												:aria-label="$t('lz.button.sharedataSqlVariable.resetCursor')"
												@click="handleReset(row)"
											/>
										</span>
									</el-tooltip>
								</div>
							</template>
						</el-table-column>
					</el-table>
				</div>
			</el-collapse-transition>
		</div>

		<SetCursorDialog ref="setCursorRef" @handleQuery="onCursorChanged" />
	</div>
</template>

<script lang="ts" setup name="sharedataCursorBlock">
import { computed, reactive, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage, ElMessageBox } from 'element-plus';
import { getAPI } from '/@/utils/axios-utils';
import { ShareDataPacketApi } from '/@/api-services/shareData';
import { Local } from '/@/utils/storage';
import { type CursorSection, buildCursorSections, cursorSavedText } from '/@/utils/sharedata/cursorSections';
import SetCursorDialog from './setCursorDialog.vue';

/* Giữ khớp ShareDataConst.KindBuiltin (BE) */
const KIND_BUILTIN = 'Builtin';
/* Thu gọn khối như khối 3, nhớ trạng thái giữa các lần mở */
const CURSOR_COLLAPSED_KEY = 'sharedataDataSource:cursorCollapsed';

interface PartnerItem {
	partnerId: string;
	partnerCode?: string | null;
	partnerName?: string | null;
	isCustom: boolean;
}

interface PartnerCursorRow {
	partnerId: string;
	partnerCode?: string | null;
	partnerName?: string | null;
	savedValue: string | null;
	sections: CursorSection[];
}

interface CursorLine {
	group: string | null;
	/** Nguyên chuỗi mốc đang lưu; chưa có mốc thì câu nói lượt gửi tới lấy từ đâu. */
	text: string;
}

const props = defineProps<{
	packetCode?: string | null;
	/** Tab đối tác đang mở; trống là tab Mẫu chung. */
	partnerId?: string | null;
	partners: PartnerItem[];
	/** Đối tác có đăng ký gửi đang bật: không Đặt mốc, Đặt lại được (ESH-2306). */
	lockedPartnerIds: string[];
}>();

interface Emits {
	/** Vừa Đặt mốc hoặc Đặt lại: khối 3 khóa hay mở ô Mốc bắt đầu theo mốc mới. */
	(e: 'cursor-change'): void;
}
const emit = defineEmits<Emits>();

const { t: $t } = useI18n();
const setCursorRef = ref();
const collapsed = ref(Local.get(CURSOR_COLLAPSED_KEY) === true);
const state = reactive({
	rows: [] as PartnerCursorRow[],
	resettingPartnerId: null as string | null,
	loading: false,
});
let loadVersion = 0;

/** Đối tác thuộc phạm vi tab: tab Mẫu chung là các đối tác dùng câu mẫu chung, tab đối tác là đúng đối tác đó. */
const scopePartners = computed(() =>
	props.partnerId ? props.partners.filter((p) => p.partnerId === props.partnerId) : props.partners.filter((p) => p.partnerId && !p.isCustom)
);

const title = computed(() => {
	if (!props.partnerId) return $t('lz.label.sharedataCursor.blockTitleCommon');
	const partner = props.partners.find((p) => p.partnerId === props.partnerId);
	return $t('lz.label.sharedataCursor.blockTitlePartner', { partner: partner?.partnerName || partner?.partnerCode || props.partnerId });
});

const toggleCollapsed = () => {
	collapsed.value = !collapsed.value;
	Local.set(CURSOR_COLLAPSED_KEY, collapsed.value);
};

const loadRow = async (packetCode: string, partner: PartnerItem): Promise<PartnerCursorRow> => {
	let sections: CursorSection[] = [];
	try {
		const res = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketGetsqlvariablesGet(packetCode, partner.partnerId);
		const items = (res.data?.result?.items ?? []).filter((i: any) => i.valueKind === KIND_BUILTIN);
		sections = buildCursorSections(items.map((i: any) => ({ code: i.code ?? '', source: i.value ?? '', savedValue: i.savedValue, defaultValue: i.defaultValue })));
	} catch {
		// Lỗi đã được interceptor báo; dòng của đối tác này hiện như chưa có mốc
	}
	return { partnerId: partner.partnerId, partnerCode: partner.partnerCode, partnerName: partner.partnerName, savedValue: cursorSavedText(sections), sections };
};

/** Nạp lại mốc đang chạy của mọi đối tác trong phạm vi tab; lượt nạp cũ về sau lượt mới thì bỏ. */
const reload = async () => {
	const version = ++loadVersion;
	const packetCode = props.packetCode;
	const partners = scopePartners.value;
	if (!packetCode || partners.length === 0) {
		state.rows = [];
		state.loading = false;
		return;
	}
	state.loading = true;
	try {
		const rows = await Promise.all(partners.map((p) => loadRow(packetCode, p)));
		if (version === loadVersion) state.rows = rows;
	} finally {
		if (version === loadVersion) state.loading = false;
	}
};

watch(() => [props.packetCode, props.partnerId, scopePartners.value.map((p) => p.partnerId).join(',')], reload, { immediate: true });

/** Mốc vừa đổi (Đặt mốc, Đặt lại): nạp lại bảng và báo trang cha để khối 3 khóa hay mở ô Mốc bắt đầu. */
const onCursorChanged = async () => {
	await reload();
	emit('cursor-change');
};

const partnerLabel = (row: Pick<PartnerItem, 'partnerCode' | 'partnerName'>) => [row.partnerCode, row.partnerName].filter(Boolean).join(' — ');

const isLocked = (row: PartnerCursorRow) => props.lockedPartnerIds.includes(row.partnerId);

/** Cả khối mờ khi mọi đối tác trong bảng đều có đăng ký gửi đang bật; chỉ một số đối tác thì chỉ dòng của đối tác đó mờ. */
const scopeLocked = computed(() => scopePartners.value.length > 0 && scopePartners.value.every((p) => props.lockedPartnerIds.includes(p.partnerId)));

const setTooltip = (row: PartnerCursorRow) => {
	if (isLocked(row)) return $t('lz.label.sharedataDataSource.cursorLockedBySubscription');
	return $t(row.sections.length > 0 ? 'lz.tooltip.sharedataSqlVariable.setCursorBtn' : 'lz.tooltip.sharedataSqlVariable.setDisabledNoCursorVars');
};

const resetTooltip = (row: PartnerCursorRow) => {
	if (isLocked(row)) return $t('lz.label.sharedataDataSource.cursorLockedBySubscription');
	return $t(row.savedValue ? 'lz.tooltip.sharedataCursor.resetBtn' : 'lz.tooltip.sharedataSqlVariable.resetDisabledNoSaved');
};

const openSetCursor = (row: PartnerCursorRow) => {
	if (!props.packetCode) return;
	setCursorRef.value?.openDialog(props.packetCode, {
		partnerId: row.partnerId,
		partnerName: row.partnerName || row.partnerCode || row.partnerId,
		sections: row.sections,
	});
};

const handleReset = async (row: PartnerCursorRow) => {
	if (!props.packetCode) return;
	try {
		const partner = row.partnerName || row.partnerCode || row.partnerId;
		await ElMessageBox.confirm($t('lz.confirm.sharedataCursor.resetRunning', { partner, from: resetFromText(row) }), $t('lz.label.base.notification'), {
			type: 'warning',
			confirmButtonText: $t('lz.button.base.confirm'),
			cancelButtonText: $t('lz.button.base.cancel'),
		});
	} catch {
		return;
	}
	const prevSavedValue = row.savedValue;
	const prevLastKeys = row.sections.map((s) => s.lastKey);
	row.savedValue = null;
	row.sections.forEach((s) => {
		s.lastKey = '';
	});
	state.resettingPartnerId = row.partnerId;
	try {
		await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketResetlastsendPost({ packetCode: props.packetCode, partnerId: row.partnerId });
		ElMessage.success($t('lz.message.sharedataSqlVariable.resetSavedSuccess'));
		await onCursorChanged();
	} catch {
		row.savedValue = prevSavedValue;
		row.sections.forEach((s, i) => {
			s.lastKey = prevLastKeys[i] ?? '';
		});
		await reload();
	} finally {
		state.resettingPartnerId = null;
	}
};

/** Nhóm mốc chưa có mốc đang chạy thì lượt gửi tới lấy từ đâu: Mốc bắt đầu của câu SQL (nguyên chuỗi), trống là toàn bộ dữ liệu. */
const startFromText = (section: CursorSection) => {
	const start = section.start ?? '';
	if (!start) return $t('lz.label.sharedataCursor.resendAll');
	return $t('lz.label.sharedataCursor.resendFrom', { value: start });
};

/** Mốc đang chạy từng nhóm mốc: nguyên chuỗi mốc đang lưu; chưa có thì lượt gửi tới lấy từ đâu. */
const cursorLines = (row: PartnerCursorRow): CursorLine[] =>
	row.sections.map((section) => ({
		group: section.group,
		text: section.lastKey || $t('lz.label.sharedataCursor.noneNext', { from: startFromText(section) }),
	}));

/** Bỏ mốc đang chạy thì lượt gửi tới gửi lại từ đâu, theo từng nhóm mốc. */
const resetFromText = (row: PartnerCursorRow) => {
	const parts = row.sections.map((section) => (section.group ? `${section.group}: ${startFromText(section)}` : startFromText(section)));
	return parts.length > 0 ? parts.join('; ') : $t('lz.label.sharedataCursor.resendAll');
};

const loading = computed(() => state.loading);

defineExpose({ reload, loading });
</script>

<style lang="scss">
/* Tooltips render in <body>, so this block is not scoped */
.el-popper.cursor-block-tooltip {
	box-sizing: border-box;
	width: max-content;
	max-width: 28vw;
	line-height: 1.5;
	white-space: pre-line;
	overflow-wrap: anywhere;
}
</style>

<style scoped lang="scss">
.scb-card {
	display: flex;
	flex-direction: column;
	gap: 6px;
	padding: 6px 8px;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 6px;
	background: var(--el-bg-color);
	box-sizing: border-box;
	overflow: hidden;

	&.is-scope-locked {
		background: var(--el-fill-color-lighter);

		.scb-table :deep(.el-table__body tr > td.el-table__cell) {
			background-color: var(--el-fill-color-light);
			color: var(--el-text-color-secondary);
		}
	}
}

.scb-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding-bottom: 6px;
	border-bottom: 1px solid var(--el-border-color-lighter);

	&.is-collapsed {
		padding-bottom: 0;
		border-bottom: none;
	}

	.scb-header-left {
		display: flex;
		flex: 1;
		align-items: center;
		gap: 6px;
		cursor: pointer;
		user-select: none;
	}

	.scb-title {
		font-size: 13px;
		font-weight: 600;
		color: var(--el-text-color-primary);
	}

	.scb-badge-count {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		padding: 1px 7px;
		border-radius: 10px;
		font-size: 12px;
		font-weight: 500;
		background: var(--el-fill-color-darker);
		color: var(--el-text-color-regular);
		line-height: 1.2;
	}
}

/* Bảng cùng kiểu với bảng tham số ở khối 3: tiêu đề nền xanh chữ trắng, ô gọn, rê chuột đổi nền dòng */
.scb-table {
	width: 100%;
	height: auto;

	:deep(.el-table__inner-wrapper) {
		height: auto;
	}

	:deep(.el-table__body-wrapper) {
		height: auto;
	}

	:deep(.el-scrollbar) {
		height: auto;
	}

	:deep(.el-scrollbar__wrap) {
		height: auto;
	}

	:deep(.el-table__header-wrapper th.el-table__cell) {
		background-image: linear-gradient(180deg, color-mix(in srgb, var(--el-color-primary) 78%, #0f172a) 0%, var(--el-color-primary) 100%);
		color: #ffffff;
		font-weight: 600;
		font-size: 12px;
		height: 32px;
		padding: 3px 0;
		border-right-color: rgba(255, 255, 255, 0.22);
	}

	:deep(.el-table__row) {
		transition: background-color 0.15s ease;

		&:hover > td.el-table__cell {
			background-color: var(--el-fill-color-lighter);
		}

		&.is-row-locked > td.el-table__cell {
			background-color: var(--el-fill-color-light);
			color: var(--el-text-color-secondary);
		}
	}

	:deep(.el-table__cell) {
		padding: 5px 0;
	}

	:deep(.el-table__empty-block) {
		min-height: 0;
	}

	:deep(.el-table__empty-text) {
		width: auto;
		line-height: 1.5;
		padding: 10px 12px;
		font-size: 12px;
	}
}

/* Chữ trong bảng cùng một màu, một cỡ (màu chữ mặc định của bảng); xuống dòng ở khoảng trắng,
   chuỗi mốc dài (phần đuôi GUID) được bẻ giữa chừng; nhờ vậy bảng không cuộn ngang */
.sd-cursor-line {
	word-break: normal;
	overflow-wrap: anywhere;
}

/* Hai nút icon trên một hàng, không xuống dòng, căn giữa ô thao tác */
.sd-cursor-actions {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	gap: 4px;
	white-space: nowrap;

	.el-button + .el-button {
		margin-left: 0;
	}
}

.sd-cursor-btn-wrapper {
	display: inline-flex;
}

/* Mỗi nhóm mốc một dòng: tên nhóm, rồi chuỗi mốc hoặc lượt gửi tới lấy từ đâu */
.sd-cursor-line {
	display: flex;
	flex-wrap: wrap;
	align-items: baseline;
	gap: 0 6px;
	line-height: 1.6;
}

.sd-cursor-group {
	font-weight: 600;
}
</style>
