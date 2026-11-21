<template>
	<div class="sharedata-set-cursor-container">
		<el-dialog
			ref="dialogRef"
			v-model="state.isShowDialog"
			draggable
			:close-on-click-modal="!state.isProcessing"
			:show-close="!state.isProcessing"
			:close-on-press-escape="!state.isProcessing"
			:before-close="beforeClose"
			width="620px"
			append-to-body
			class="partner-setup-dialog sd-set-cursor-dialog"
		>
			<template #header>
				<div class="scd-header">
					<el-icon size="16" style="margin-right: 6px; display: inline; vertical-align: middle">
						<ele-Edit />
					</el-icon>
					<span>{{ $t('lz.label.sharedataSqlVariable.setCursorDialogTitle', { partner: state.partnerName }) }}</span>
				</div>
			</template>

			<el-form :disabled="state.isProcessing" size="small" label-position="left" label-width="170px" class="scd-form">
				<!-- Khối từng nhóm mốc (hỗ trợ câu UNION / nhiều nguồn) -->
				<div
					v-for="section in state.sections"
					:key="`${state.partnerId}_${section.group ?? ''}`"
					class="sd-cursor-section"
				>
					<!-- Tiêu đề nhóm nếu câu có nhiều mốc (UNION) -->
					<div v-if="state.sections.length > 1 || section.group" class="scd-group-header">
						<span class="scd-group-title">
							{{ section.group ? $t('lz.label.sharedataSqlVariable.setCursorGroupTitle', { group: section.group }) : $t('lz.label.sharedataSqlVariable.setCursorDefaultGroup') }}
						</span>
						<el-tag size="small" type="primary" effect="plain" class="scd-col-tag">
							{{ cursorColumn('__lastKey', section) }}
						</el-tag>
					</div>

					<!-- 1. Mốc đang chạy -->
					<el-form-item
						class="sd-cursor-current"
						:label="`${$t('lz.label.sharedataCursor.currentLabel')}:`"
					>
						<div v-if="section.current" class="scd-current-val">
							<el-tag size="small" type="success" effect="light" class="scd-tag-mono">
								<el-icon style="vertical-align: -1px; margin-right: 4px;"><ele-Timer /></el-icon>
								{{ section.current }}
							</el-tag>
						</div>
						<div v-else class="scd-no-cursor">
							<el-tag size="small" type="info" effect="light">{{ $t('lz.label.sharedataCursor.noSaved') }}</el-tag>
							<span class="scd-sub-text"> — {{ noneNextText(section.start) }}</span>
						</div>
					</el-form-item>

					<!-- 2. Mốc mới cần đặt: ô chữ điền sẵn mốc đang chạy, sai luật thì báo ngay dưới ô -->
					<el-form-item :label="`${$t('lz.label.sharedataCursor.newCursorLabel')}:`" required class="scd-new-cursor-item">
						<div class="scd-input-container">
							<cursor-value-input v-model="section.lastKey" />
						</div>

						<!-- Cảnh báo hệ quả khi thay đổi mốc -->
						<div
							v-if="effectText(section)"
							class="sd-cursor-effect scd-effect-alert"
							:class="isEarlierEffect(section) ? 'is-warning' : 'is-info'"
						>
							<el-icon class="scd-effect-icon">
								<ele-Warning v-if="isEarlierEffect(section)" />
								<ele-InfoFilled v-else />
							</el-icon>
							<span>{{ effectText(section) }}</span>
						</div>
					</el-form-item>
				</div>

				<!-- Ghi chú nguyên lý mốc đồng bộ chuẩn alert.info của ShareData -->
				<div class="alert info">
					{{ $t('lz.label.sharedataSqlVariable.setCursorHint') }}
				</div>
			</el-form>

			<template #footer>
				<div class="scd-dialog-footer">
					<div class="scd-footer-right">
						<el-button :disabled="state.isProcessing" @click="cancel">{{ $t('lz.button.base.cancel') }}</el-button>
						<el-button type="primary" :disabled="state.isProcessing || !canSave" :loading="state.isProcessing" @click="submit">
							{{ $t('lz.button.base.confirm') }}
						</el-button>
					</div>
				</div>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="sharedataSetCursorDialog">
import { computed, reactive, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage, ElMessageBox } from 'element-plus';
import { getAPI } from '/@/utils/axios-utils';
import { ShareDataPacketApi } from '/@/api-services/shareData';
import { type CursorSection, cursorColumn, isCursorSectionFilled, isValidCursorValue } from '/@/utils/sharedata/cursorSections';
import CursorValueInput from './cursorValueInput.vue';

const { t: $t } = useI18n();
const dialogRef = ref();
const emits = defineEmits(['handleQuery']);

interface DialogSection extends CursorSection {
	/** Mốc đang chạy của nhóm. */
	current: string;
	/** Mốc điền sẵn khi mở hộp: chưa đổi thì không gửi. */
	initial: string;
}

const state = reactive({
	isShowDialog: false,
	isProcessing: false,
	packetCode: '',
	partnerId: '',
	partnerName: '',
	sections: [] as DialogSection[],
	snapshot: '',
});

const isChanged = (section: DialogSection) => isCursorSectionFilled(section) && section.lastKey.trim() !== section.initial;
/** Có nhóm đổi mốc và mọi ô đã nhập đều đúng luật mới Xác nhận được; ô trống hoặc chưa đổi thì không gửi. */
const canSave = computed(() => state.sections.some(isChanged) && state.sections.every((section) => !isCursorSectionFilled(section) || isValidCursorValue(section.lastKey)));

const openDialog = (packetCode: string, partner: { partnerId: string; partnerName: string; sections: CursorSection[] }) => {
	state.packetCode = packetCode;
	state.partnerId = partner.partnerId;
	state.partnerName = partner.partnerName;
	// Điền sẵn nguyên chuỗi mốc đang chạy để sửa từ đó; nhóm không đổi thì giữ nguyên mốc
	state.sections = partner.sections.map((section) => ({ group: section.group, lastKey: section.lastKey, current: section.lastKey, initial: section.lastKey, start: section.start ?? '' }));
	state.snapshot = JSON.stringify(state.sections);
	state.isShowDialog = true;
	dialogRef.value?.resetPosition?.();
};

const beforeClose = async (done: () => void) => {
	if (JSON.stringify(state.sections) !== state.snapshot) {
		try {
			await ElMessageBox.confirm($t('lz.message.base.confirmDiscardChanges'), $t('lz.label.base.notification'), {
				type: 'warning',
				confirmButtonText: $t('lz.button.base.confirm'),
				cancelButtonText: $t('lz.button.base.cancel'),
			});
		} catch {
			return;
		}
	}
	done();
};

const cancel = () => beforeClose(() => (state.isShowDialog = false));

const submit = async () => {
	if (!canSave.value) return;
	let savedCount = 0;
	state.isProcessing = true;
	try {
		// Mỗi nhóm mốc gửi một lần (nhóm có tên kèm cursorKey)
		for (const section of state.sections.filter(isChanged)) {
			await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketSetlastsendPost({
				packetCode: state.packetCode,
				partnerId: state.partnerId,
				lastKey: section.lastKey.trim(),
				...(section.group ? { cursorKey: section.group } : {}),
			});
			savedCount++;
		}
		ElMessage.success($t('lz.message.sharedataSqlVariable.setCursorSuccess'));
		state.isShowDialog = false;
		emits('handleQuery');
	} catch {
		// Lỗi đã được interceptor báo; nhóm trước đã lưu thì nạp lại bảng để thấy đúng mốc hiện tại
		if (savedCount > 0) emits('handleQuery');
	} finally {
		state.isProcessing = false;
	}
};

/** Ghi chú lượt gửi tới khi chưa có mốc: Mốc bắt đầu của câu SQL (nguyên chuỗi), trống là gửi toàn bộ. */
const noneNextText = (start?: string) => {
	const from = start ? $t('lz.label.sharedataCursor.resendFrom', { value: start }) : $t('lz.label.sharedataCursor.resendAll');
	return $t('lz.label.sharedataCursor.noneNext', { from });
};

/** Hệ quả khi dời mốc đang chạy (so chuỗi): lùi thì phần đã gửi bị gửi lại, tiến thì phần chưa gửi bị bỏ qua; chưa có mốc đang chạy, chưa đổi hoặc mốc sai luật thì không báo. */
const effectText = (section: DialogSection) => {
	const next = (section.lastKey || '').trim();
	if (!section.current || !isChanged(section) || !isValidCursorValue(next) || next === section.current) return '';
	const earlier = next < section.current;
	const from = earlier ? next : section.current;
	const to = earlier ? section.current : next;
	return $t(earlier ? 'lz.label.sharedataCursor.effectResend' : 'lz.label.sharedataCursor.effectSkip', { from, to });
};

const isEarlierEffect = (section: DialogSection) => {
	const next = (section.lastKey || '').trim();
	return Boolean(section.current && isChanged(section) && next < section.current);
};

defineExpose({ openDialog });
</script>

<style scoped lang="scss">
.scd-header {
	display: flex;
	align-items: center;
	font-weight: 600;
	font-size: 15px;
	color: #fff;
}

.scd-form {
	padding: 6px 0 0;

	:deep(.el-form-item) {
		margin-bottom: 14px;
		align-items: flex-start;

		.el-form-item__label {
			font-size: 13px;
			font-weight: 500;
			color: var(--el-text-color-primary, #303133);
			line-height: 32px;
			padding-right: 12px;
			white-space: nowrap;
		}

		.el-form-item__content {
			line-height: 32px;
			display: flex;
			align-items: center;
			flex-wrap: nowrap;
			min-height: 32px;
		}
	}
}

.sd-cursor-section + .sd-cursor-section {
	margin-top: 12px;
	padding-top: 12px;
	border-top: 1px dashed var(--el-border-color-lighter);
}

.scd-group-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding: 4px 0 8px;
	margin-bottom: 8px;
	border-bottom: 1px dashed var(--el-border-color-lighter);

	.scd-group-title {
		font-size: 13px;
		font-weight: 600;
		color: var(--el-text-color-primary);
	}

	.scd-col-tag {
		font-family: Consolas, Monaco, monospace;
		font-size: 11px;
	}
}

.scd-current-val,
.scd-no-cursor {
	display: inline-flex;
	align-items: center;
	flex-wrap: nowrap;
	white-space: nowrap;
	gap: 6px;
	min-height: 32px;
}

/* Mốc đang chạy dài (ngày giờ kèm ID dạng GUID) xuống dòng trong khung, không đẩy hộp thoại cuộn ngang */
.scd-current-val {
	max-width: 100%;
	min-width: 0;
	white-space: normal;
}

.scd-tag-mono {
	font-family: Consolas, Monaco, monospace;
	font-size: 12px;
	height: auto;
	max-width: 100%;
	padding: 4px 8px;
	line-height: 1.5;
	white-space: normal;
	word-break: break-all;

	:deep(.el-tag__content) {
		white-space: normal;
		word-break: break-all;
	}
}

.scd-sub-text {
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.scd-new-cursor-item {
	:deep(.el-form-item__content) {
		flex-direction: column;
		align-items: stretch;
	}
}

.scd-input-container {
	width: 100%;

	:deep(.cvi-wrap) {
		width: 100%;
	}

	:deep(.cvi-input) {
		--el-input-height: 32px;

		.el-input__inner {
			font-size: 12.5px;
		}
	}
}

.scd-effect-alert {
	display: flex;
	align-items: center;
	gap: 8px;
	margin-top: 8px;
	padding: 6px 12px;
	border-radius: 4px;
	font-size: 12.5px;
	line-height: 1.4;
	width: 100%;

	&.is-warning {
		background: #fdf6ec;
		border: 1px solid #faecd8;
		color: #e6a23c;

		.scd-effect-icon {
			color: #e6a23c;
		}
	}

	&.is-info {
		background: #eaf1f8;
		border: 1px solid #bcd3e8;
		color: #1d4f7c;

		.scd-effect-icon {
			color: #1d4f7c;
		}
	}

	.scd-effect-icon {
		font-size: 15px;
		flex-shrink: 0;
	}
}

.alert.info {
	border: 1px solid #bcd3e8;
	background: #eaf1f8;
	color: #1d4f7c;
	border-radius: 4px;
	padding: 8px 12px;
	font-size: 13px;
	line-height: 1.5;
	margin-top: 10px;
	margin-bottom: 4px;
	white-space: pre-line;
	cursor: help;
	transition: background-color 0.2s ease, border-color 0.2s ease;

	&:hover {
		background: #e2eef8;
		border-color: #a4c4e0;
	}
}

.scd-dialog-footer {
	display: flex;
	align-items: center;
	justify-content: space-between;
	width: 100%;

	.scd-footer-right {
		display: inline-flex;
		align-items: center;
		gap: 12px;
		margin-left: auto;
	}
}
</style>
