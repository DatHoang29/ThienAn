<template>
	<el-dialog
		v-model="visible"
		width="min(1260px, 90vw)"
		destroy-on-close
		draggable
		:close-on-click-modal="!isSubmitting"
		:close-on-press-escape="!isSubmitting"
		:show-close="!isSubmitting"
		class="sql-diff-dialog"
		@closed="handleDialogClosed"
	>
		<template #header>
			<div class="diff-dialog-title">
				<el-icon class="mr5"><ele-Edit /></el-icon>
				<span>{{ dialogTitle }}</span>
			</div>
		</template>

		<el-alert v-if="props.locked" :title="$t('lz.label.sharedataDataSource.partnerSqlLockedBySubscription')" type="warning" :closable="false" show-icon class="mb10" />
		<div class="diff-container">
			<div class="diff-header-row">
				<div class="diff-header-col">
					<span class="header-badge std-badge">{{ $t('lz.label.sharedataDataSource.standardColumnTitle') }}</span>
				</div>
				<div class="diff-header-col">
					<span class="header-badge partner-badge">{{ $t('lz.label.sharedataDataSource.partnerColumnTitle') }}</span>
				</div>
			</div>
			<div ref="monacoDiffContainerRef" class="diff-editor-box" />

			<div v-if="fieldComparison.rows.length" class="diff-fields-section">
				<div class="diff-fields-header">
					<span class="diff-fields-title">{{ $t('lz.label.sharedataDataSource.diffFieldsTitle') }}</span>
					<span class="diff-legend-group">
						<span class="diff-legend-item">
							<span class="cmp-st same">=</span>
							<span>{{ fieldComparison.sameCount }}</span>
						</span>
						<span v-if="fieldComparison.newCount" class="diff-legend-item">
							<span class="cmp-st new">+</span>
							<span>{{ fieldComparison.newCount }}</span>
						</span>
						<span v-if="fieldComparison.missingCount" class="diff-legend-item">
							<span class="cmp-st missing">-</span>
							<span>{{ fieldComparison.missingCount }}</span>
						</span>
						<span v-if="fieldComparison.diffCount" class="diff-legend-item">
							<span class="cmp-st diff">≠</span>
							<span>{{ fieldComparison.diffCount }}</span>
						</span>
					</span>
					<el-checkbox v-model="onlyDiff" size="small">
						{{ $t('lz.label.sharedataDataSource.onlyShowDiffRows') }}
					</el-checkbox>
				</div>
				<el-table :data="filteredFieldRows" size="small" border max-height="220" class="diff-fields-table">
					<el-table-column :label="$t('lz.entity.sharedataDataSource.aliasFieldKey')" min-width="150" header-align="center">
						<template #default="{ row }">
							<span class="diff-field-key">{{ row.key }}</span>
							<span v-if="row.label && row.label !== row.key" class="diff-field-label"> · {{ row.label }}</span>
						</template>
					</el-table-column>
					<el-table-column :label="$t('lz.label.sharedataDataSource.standardColumnTitle')" min-width="160" header-align="center">
						<template #default="{ row }">
							<span v-if="row.common">{{ formatFieldSpec(row.common) }}</span>
							<span v-else class="diff-field-empty">—</span>
						</template>
					</el-table-column>
					<el-table-column :label="$t('lz.label.sharedataDataSource.partnerColumnTitle')" min-width="160" header-align="center">
						<template #default="{ row }">
							<span v-if="row.partner">{{ formatFieldSpec(row.partner) }}</span>
							<span v-else class="diff-field-empty">—</span>
						</template>
					</el-table-column>
					<el-table-column :label="$t('lz.label.sharedataDataSource.fieldCompareColumn')" min-width="140" align="center" header-align="center">
						<template #default="{ row }">
							<span class="diff-cell-compare">
								<span class="cmp-st" :class="row.state">{{ stateSymbol(row.state) }}</span>
								<span>{{ $t(`lz.label.sharedataDataSource.compareState_${row.state}`) }}</span>
							</span>
						</template>
					</el-table-column>
				</el-table>
			</div>
		</div>

		<template #footer>
			<span class="dialog-footer">
				<el-button :disabled="isSubmitting" @click="visible = false">{{ $t('lz.button.base.cancel') }}</el-button>
				<el-button type="warning" plain icon="ele-RefreshLeft" :loading="submittingMode === 'standard'" :disabled="isSubmitting || props.locked" @click="handleUseStandard">
					{{ $t('lz.button.sharedataDataSource.takeAllStandard') }}
				</el-button>
				<el-button type="primary" icon="ele-FolderChecked" :loading="submittingMode === 'merged'" :disabled="isSubmitting || props.locked" @click="handleSaveMerged">
					{{ $t('lz.button.sharedataDataSource.saveMerged') }}
				</el-button>
			</span>
		</template>
	</el-dialog>
</template>

<script lang="ts" setup name="sharedataSqlDiffDialog">
import { computed, nextTick, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage, ElMessageBox } from 'element-plus';
import { getAPI } from '/@/utils/axios-utils';
import { ShareDataPacketApi } from '/@/api-services/shareData/apis/share-data-packet-api';
import type { SqlFieldItem } from '/@/api-services/shareData/models';
import { compareFields, type FieldCompareState } from '/@/utils/sharedata/fieldCompare';

const { t: $t } = useI18n();

interface Props {
	modelValue: boolean;
	partnerId?: string | null;
	packetCode?: string | null;
	partnerName?: string | null;
	/** Mẫu chung hiện tại, hiện ở cột trái và chỉ đọc. */
	standardSql?: string | null;
	/** SQL riêng của đối tác, hiện ở cột phải và sửa được để trộn từ mẫu chung. */
	customSql?: string | null;
	commonFields?: SqlFieldItem[] | null;
	partnerFields?: SqlFieldItem[] | null;
	/** Đối tác có đăng ký gửi đang chạy: BE chặn lưu / đặt lại SQL riêng (ESH-2306), chỉ cho xem. */
	locked?: boolean;
}
const props = defineProps<Props>();

interface Emits {
	(e: 'update:modelValue', val: boolean): void;
	(e: 'success'): void;
}
const emit = defineEmits<Emits>();

const onlyDiff = ref(false);

const visible = computed({
	get: () => props.modelValue,
	set: (val: boolean) => emit('update:modelValue', val),
});

const dialogTitle = computed(() =>
	$t('lz.title.sharedataDataSource.sqlDiff', {
		packet: props.packetCode || '',
		partner: props.partnerName || '',
	})
);

const monacoDiffContainerRef = ref<HTMLDivElement | null>(null);
const submittingMode = ref<'standard' | 'merged' | null>(null);
const isSubmitting = computed(() => submittingMode.value !== null);

let diffEditorInstance: any = null;
let originalModelInstance: any = null;
let modifiedModelInstance: any = null;
let initializing = false;

const initDiffEditor = async () => {
	if (!monacoDiffContainerRef.value || diffEditorInstance || initializing) return;
	initializing = true;
	try {
		const monaco = await import('monaco-editor');
		const isDark = document.documentElement.classList.contains('dark');

		diffEditorInstance = monaco.editor.createDiffEditor(monacoDiffContainerRef.value, {
			theme: isDark ? 'vs-dark' : 'vs',
			readOnly: false,
			originalEditable: false,
			renderSideBySide: true,
			renderMarginRevertIcon: true,
			fontSize: 13,
			lineNumbers: 'on',
			minimap: { enabled: false },
			automaticLayout: true,
			scrollBeyondLastLine: false,
			renderOverviewRuler: false,
			scrollbar: { verticalScrollbarSize: 10, horizontalScrollbarSize: 10, useShadows: false },
		});

		originalModelInstance = monaco.editor.createModel(props.standardSql || '', 'sql');
		modifiedModelInstance = monaco.editor.createModel(props.customSql || '', 'sql');
		diffEditorInstance.setModel({ original: originalModelInstance, modified: modifiedModelInstance });
	} catch (e) {
		console.error('Failed to initialize Monaco diff editor:', e);
	} finally {
		initializing = false;
	}
};

const disposeDiffEditor = () => {
	if (diffEditorInstance) {
		diffEditorInstance.dispose();
		diffEditorInstance = null;
	}
	if (originalModelInstance) {
		originalModelInstance.dispose();
		originalModelInstance = null;
	}
	if (modifiedModelInstance) {
		modifiedModelInstance.dispose();
		modifiedModelInstance = null;
	}
};

const handleDialogClosed = () => {
	disposeDiffEditor();
};

watch(
	() => props.modelValue,
	(isOpen) => {
		if (isOpen) {
			nextTick(() => {
				initDiffEditor();
			});
		} else {
			disposeDiffEditor();
		}
	}
);

/** Nội dung đang ở cột phải (đã trộn hoặc chưa sửa). */
const getMergedText = () => (modifiedModelInstance ? modifiedModelInstance.getValue() : props.customSql);

const handleUseStandard = async () => {
	if (props.locked || isSubmitting.value) return;
	try {
		await ElMessageBox.confirm($t('lz.confirm.sharedataDataSource.takeAllStandard'), $t('lz.label.base.notification'), {
			type: 'warning',
			confirmButtonText: $t('lz.button.base.confirm'),
			cancelButtonText: $t('lz.button.base.cancel'),
		});
	} catch {
		return;
	}

	submittingMode.value = 'standard';
	try {
		await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketResetpartnerextractsqlPost({
			partnerId: props.partnerId,
			packetCode: props.packetCode,
		});
		ElMessage.success($t('lz.message.base.saveSuccess'));
		emit('success');
	} catch {
		// Handled by global exception handler
	} finally {
		submittingMode.value = null;
	}
};

const handleSaveMerged = async () => {
	if (props.locked || isSubmitting.value) return;
	submittingMode.value = 'merged';
	try {
		await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketUpdatepartnerextractsqlPost({
			partnerId: props.partnerId,
			packetCode: props.packetCode,
			extractSql: getMergedText(),
		});
		ElMessage.success($t('lz.message.base.saveSuccess'));
		emit('success');
	} catch {
		// Handled by global exception handler
	} finally {
		submittingMode.value = null;
	}
};

const fieldComparison = computed(() => compareFields(props.commonFields, props.partnerFields));
const filteredFieldRows = computed(() => (onlyDiff.value ? fieldComparison.value.rows.filter((r) => r.state !== 'same') : fieldComparison.value.rows));

const formatFieldSpec = (f: SqlFieldItem) => {
	const parts = [f.type || 'string'];
	if (f.dbType) parts.push(`(${f.dbType})`);
	if (f.required) parts.push($t('lz.label.sharedataDataSource.required'));
	return parts.join(' ');
};

const stateSymbol = (s: FieldCompareState) => (s === 'same' ? '=' : s === 'new' ? '+' : s === 'missing' ? '-' : '≠');
const stateTagType = (s: FieldCompareState) => (s === 'same' ? 'success' : s === 'new' ? 'primary' : s === 'missing' ? 'danger' : 'warning');
</script>

<!-- Hộp được teleport ra body nên style scoped không với tới; chỉ khóa theo class riêng của hộp này. -->
<style lang="scss">
/* CSS toàn cục đặt .el-dialog là absolute nên hộp lệch lên trên khi nội dung đổi cao; đưa về luồng flex của overlay để luôn nằm giữa.
   Dịch sang phải (left: 40px) để tạo khoảng cách thoáng với viền menu bên trái (sidebar), tránh bị dính sát mép menu. */
.el-overlay .el-dialog.sql-diff-dialog {
	position: relative;
	left: 40px;
}
</style>

<style scoped lang="scss">
.diff-dialog-title {
	display: flex;
	align-items: center;
	color: #fff;
}

.diff-container {
	display: flex;
	flex-direction: column;
	width: 100%;
	gap: 6px;
}

.diff-header-row {
	display: flex;
	width: 100%;
}

.diff-header-col {
	flex: 1;
	padding: 4px 8px;

	.header-badge {
		font-size: 12px;
		font-weight: 600;
		padding: 2px 8px;
		border-radius: 4px;
	}

	.std-badge {
		background: var(--el-color-info-light-9);
		color: var(--el-color-info);
		border: 1px solid var(--el-color-info-light-7);
	}

	.partner-badge {
		background: var(--el-color-primary-light-9);
		color: var(--el-color-primary);
		border: 1px solid var(--el-color-primary-light-7);
	}
}

.diff-editor-box {
	width: 100%;
	height: min(460px, 48vh);
	border: 1px solid var(--el-border-color);
	border-radius: var(--el-border-radius-base);
	overflow: hidden;
}

.diff-fields-section {
	margin-top: 8px;
	display: flex;
	flex-direction: column;
	gap: 6px;
}

.diff-fields-header {
	display: flex;
	align-items: center;
	gap: 12px;
	flex-wrap: wrap;

	.diff-fields-title {
		font-size: 13px;
		font-weight: 600;
		color: var(--el-text-color-primary);
	}
}

.diff-legend-group {
	display: inline-flex;
	align-items: center;
	gap: 10px;
}

.diff-legend-item {
	display: inline-flex;
	align-items: center;
	gap: 4px;
	font-size: 12px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

.diff-cell-compare {
	display: inline-flex;
	align-items: center;
	gap: 6px;
	font-size: 12px;
	font-weight: 500;
}

/* Ký hiệu so với mẫu: vòng tròn màu đồng bộ toàn hệ thống */
.cmp-st {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 20px;
	height: 20px;
	border-radius: 50%;
	border: 1px solid;
	font-weight: 700;
	font-size: 13px;
	line-height: 1;

	&.same {
		color: var(--el-color-success);
		background: var(--el-color-success-light-9);
		border-color: var(--el-color-success-light-5);
	}

	&.new {
		color: var(--el-color-primary);
		background: var(--el-color-primary-light-9);
		border-color: var(--el-color-primary-light-5);
	}

	&.missing {
		color: var(--el-color-danger);
		background: var(--el-color-danger-light-9);
		border-color: var(--el-color-danger-light-5);
	}

	&.diff {
		color: var(--el-color-warning-dark-2);
		background: var(--el-color-warning-light-9);
		border-color: var(--el-color-warning-light-5);
	}
}

.diff-field-key {
	font-family: Consolas, Monaco, monospace;
	font-weight: 600;
}

.diff-field-label,
.diff-field-empty {
	color: var(--el-text-color-secondary);
}
</style>
