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
			width="600px"
			append-to-body
			class="sd-set-cursor-dialog"
		>
			<template #header>
				<div style="color: var(--el-color-white)">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle">
						<ele-Edit />
					</el-icon>
					<span>{{ $t('lz.label.sharedataSqlVariable.setCursorDialogTitle', { partner: state.partnerName }) }}</span>
				</div>
			</template>

			<el-form :disabled="state.isProcessing" label-width="auto" label-position="left">
				<!-- Mỗi nhóm mốc một khối ô nhập: nhóm mặc định (__lastKey) và các nhóm có tên của câu gộp nhiều nguồn -->
				<div v-for="section in state.sections" :key="`${state.partnerId}_${section.group ?? ''}`" class="sd-cursor-section">
					<div v-if="state.sections.length > 1 || section.group" class="sd-cursor-section-title">
						{{ section.group ? $t('lz.label.sharedataSqlVariable.setCursorGroupTitle', { group: section.group }) : $t('lz.label.sharedataSqlVariable.setCursorDefaultGroup') }}
					</div>
					<el-form-item :label="$t('lz.label.sharedataSqlVariable.setCursorKey', { column: cursorColumn('__lastKey', section) })" required>
						<cursor-value-input v-if="state.supportsTime" v-model="section.lastKey" supports-time />
						<div v-else class="sd-cursor-no-time">{{ $t('lz.label.sharedataSqlVariable.setCursorNoTime') }}</div>
						<div v-if="section.current" class="sd-cursor-current">{{ $t('lz.label.sharedataSqlVariable.setCursorCurrent', { value: section.current }) }}</div>
					</el-form-item>
				</div>
				<div class="sd-cursor-hint">{{ $t('lz.label.sharedataSqlVariable.setCursorHint') }}</div>
			</el-form>

			<template #footer>
				<span class="dialog-footer">
					<el-button :disabled="state.isProcessing" @click="cancel">{{ $t('lz.button.base.cancel') }}</el-button>
					<el-button type="primary" :disabled="state.isProcessing || !canSave" @click="submit">{{ $t('lz.button.base.confirm') }}</el-button>
				</span>
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
import { type CursorSection, cursorColumn, cursorTimeOf, cursorTimeValue, hasTimeKeyFormula, isCursorSectionFilled } from '/@/utils/sharedata/cursorSections';
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
	supportsTime: false,
	sections: [] as DialogSection[],
	snapshot: '',
});

const isChanged = (section: DialogSection) => isCursorSectionFilled(section) && section.lastKey !== section.initial;
const canSave = computed(() => state.sections.some(isChanged));

const loadSupportsTime = async (packetCode: string, partnerId: string) => {
	try {
		const res = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketGetpartnerextractsqlGet(partnerId, packetCode);
		return hasTimeKeyFormula(res.data?.result?.customSql || res.data?.result?.standardSql);
	} catch {
		// Lỗi đã được interceptor báo; không đọc được câu thì chưa chọn được thời điểm
		return false;
	}
};

const openDialog = async (packetCode: string, partner: { partnerId: string; partnerName: string; sections: CursorSection[] }) => {
	state.supportsTime = await loadSupportsTime(packetCode, partner.partnerId);
	state.packetCode = packetCode;
	state.partnerId = partner.partnerId;
	state.partnerName = partner.partnerName;
	// Điền sẵn ngày giờ của mốc đang chạy để chỉnh từ đó; nhóm không đổi thì giữ nguyên mốc
	state.sections = partner.sections.map((section) => {
		const time = state.supportsTime ? cursorTimeOf(section.lastKey) : '';
		const initial = time ? cursorTimeValue(time) : '';
		return { group: section.group, lastKey: initial, current: section.lastKey, initial };
	});
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

defineExpose({ openDialog });
</script>

<style scoped lang="scss">
.sd-cursor-section + .sd-cursor-section {
	margin-top: 8px;
	padding-top: 8px;
	border-top: 1px dashed var(--el-border-color);
}

.sd-cursor-section-title {
	font-size: 12px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	margin-bottom: 6px;
}

.sd-cursor-no-time {
	font-size: 12px;
	color: var(--el-color-warning);
	line-height: 1.45;
}

.sd-cursor-current {
	width: 100%;
	margin-top: 2px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
	line-height: 1.45;
	word-break: break-all;
}

.sd-cursor-hint {
	font-size: 12px;
	color: var(--el-text-color-secondary);
	line-height: 1.45;
	margin-top: 4px;
}
</style>
