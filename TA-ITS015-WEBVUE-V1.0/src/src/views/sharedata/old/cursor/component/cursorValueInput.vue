<template>
	<div class="cvi-wrap">
		<!-- Câu chưa đúng quy chuẩn mốc: khóa ô ngày, sửa câu tại Quy chuẩn mốc -->
		<template v-if="props.invalid">
			<el-date-picker type="datetime" size="small" class="cvi-picker" disabled :placeholder="$t('lz.placeholder.sharedataSqlVariable.cursorNeedsFix')" />
		</template>

		<!-- Đối tác đã có mốc gửi: Mốc lần đầu không còn tác dụng nên khóa ô, chữ gợi ý nói rõ trạng thái -->
		<template v-else-if="props.hasCursor">
			<el-date-picker type="datetime" size="small" class="cvi-picker" disabled prefix-icon="ele-Lock" :placeholder="$t('lz.placeholder.sharedataSqlVariable.cursorHasSaved')" />
		</template>

		<template v-else-if="customValue">
			<span class="cvi-preview cvi-muted" :title="props.modelValue ?? ''">{{ $t('lz.label.sharedataSqlVariable.cursorWillSave', { value: props.modelValue }) }}</span>
			<el-button link size="small" icon="ele-Close" class="cvi-clear" :title="$t('lz.label.sharedataSqlVariable.cursorClearStart')" @click="emit('update:modelValue', '')" />
		</template>
		<!-- Mốc lần đầu đã chọn có chữ "Từ" đứng trước nên không cần nhãn riêng; để trống thì chữ gợi ý nói là gửi toàn bộ -->
		<template v-else-if="props.supportsTime">
			<span v-if="props.allowEmpty && pickedTime" class="cvi-from">{{ $t('lz.label.sharedataSqlVariable.cursorStartFrom') }}</span>
			<el-date-picker
				:model-value="pickedTime"
				type="datetime"
				size="small"
				class="cvi-picker"
				popper-class="cvi-date-popper"
				format="DD/MM/YYYY HH:mm:ss"
				value-format="YYYY-MM-DD HH:mm:ss"
				:shortcuts="timeShortcuts"
				:placeholder="props.allowEmpty ? $t('lz.placeholder.sharedataSqlVariable.cursorStartPlaceholder') : $t('lz.placeholder.sharedataSqlVariable.cursorPickTime')"
				@update:model-value="onPickTime"
			/>
		</template>
		<span v-else class="cvi-start-text">{{ $t('lz.label.sharedataSqlVariable.cursorModeStart') }}</span>
	</div>
</template>

<script lang="ts" setup name="sharedataCursorValueInput">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { cursorTimeOf, cursorTimeValue } from '/@/utils/sharedata/cursorSections';

const { t: $t } = useI18n();

const props = defineProps<{
	/** Mốc bắt đầu (bảng tham số) hoặc mốc mới (hộp Đặt mốc). */
	modelValue?: string | null;
	/** Được để trống: trống là gửi từ đầu. */
	allowEmpty?: boolean;
	/** Câu ghép cột mốc theo kiểu 121: chọn được ngày giờ. */
	supportsTime?: boolean;
	/** Câu chưa đúng quy chuẩn mốc: chưa chọn được ngày. */
	invalid?: boolean;
	/** Đối tác đang mở đã có mốc gửi: Mốc lần đầu không còn tác dụng nên khóa ô. */
	hasCursor?: boolean;
}>();

const emit = defineEmits(['update:modelValue']);

const pickedTime = computed(() => {
	const time = cursorTimeOf(props.modelValue);
	return time && props.modelValue === cursorTimeValue(time) ? time : '';
});

/** Giá trị đã lưu không theo dạng chọn ngày giờ: chỉ hiện để xem và xóa được. */
const customValue = computed(() => Boolean(props.modelValue) && !(props.supportsTime && pickedTime.value));

const onPickTime = (value: string | null) => emit('update:modelValue', value ? cursorTimeValue(value) : '');

const startOfDay = (daysOffset = 0, firstOfMonth = false) => {
	const d = new Date();
	if (firstOfMonth) d.setDate(1);
	else if (daysOffset) d.setDate(d.getDate() + daysOffset);
	d.setHours(0, 0, 0, 0);
	return d;
};

const timeShortcuts = computed(() => [
	{ text: $t('lz.label.sharedataSqlVariable.shortcutTodayStart'), value: () => startOfDay(0) },
	{ text: $t('lz.label.sharedataSqlVariable.shortcutYesterdayStart'), value: () => startOfDay(-1) },
	{ text: $t('lz.label.sharedataSqlVariable.shortcutLast7Days'), value: () => startOfDay(-7) },
	{ text: $t('lz.label.sharedataSqlVariable.shortcutMonthStart'), value: () => startOfDay(0, true) },
]);
</script>

<style scoped lang="scss">
.cvi-wrap {
	display: flex;
	flex-wrap: nowrap;
	align-items: center;
	gap: 4px;
	width: 100%;
	min-width: 0;
}

:deep(.cvi-picker.el-date-editor) {
	flex: 1 1 auto;
	width: auto;
	min-width: 0;
	height: 28px;
	overflow: visible;
	--el-input-height: 28px;

	&.el-input {
		height: 28px;
	}

	.el-input__wrapper {
		height: 28px;
		min-height: 28px;
		padding: 1px 6px;
		box-sizing: border-box;
	}

	.el-input__inner {
		font-size: 11.5px;
		height: 26px;
		line-height: 26px;
	}
}

.cvi-start-text {
	flex-shrink: 0;
	font-size: 12px;
	color: var(--el-text-color-regular);
}

.cvi-muted {
	flex: 1 1 0;
	min-width: 0;
	font-size: 12px;
	color: var(--el-text-color-secondary);
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}

.cvi-clear {
	flex-shrink: 0;
}

.cvi-from {
	flex-shrink: 0;
	font-size: 12px;
	color: var(--el-text-color-regular);
}

/* Bảng chọn ngày gọn: cột chọn nhanh hẹp, ô ngày 30 x 26 px, lề và tiêu đề nhỏ lại */
:global(.cvi-date-popper .el-date-picker.has-sidebar.has-time) {
	width: 352px;
}

:global(.cvi-date-popper .el-picker-panel__sidebar) {
	width: 120px;
	padding-top: 4px;
}

:global(.cvi-date-popper .el-picker-panel__sidebar + .el-picker-panel__body) {
	margin-left: 120px;
}

:global(.cvi-date-popper .el-picker-panel__shortcut) {
	white-space: nowrap;
	font-size: 12px;
	line-height: 26px;
	padding-left: 10px;
}

:global(.cvi-date-popper .el-date-picker__time-header) {
	padding: 6px 4px 4px;
}

:global(.cvi-date-popper .el-date-picker__header) {
	padding: 8px 8px 0;
}

:global(.cvi-date-popper .el-date-picker__header-label) {
	font-size: 14px;
}

:global(.cvi-date-popper .el-date-picker .el-picker-panel__content) {
	width: 210px;
	margin: 6px 11px 8px;
}

:global(.cvi-date-popper .el-date-table th) {
	padding: 2px 0 4px;
}

:global(.cvi-date-popper .el-date-table td) {
	width: 30px;
	height: 26px;
	padding: 1px 0;
}

:global(.cvi-date-popper .el-date-table td .el-date-table-cell) {
	height: 24px;
	padding: 1px 0;
}

:global(.cvi-date-popper .el-date-table td .el-date-table-cell .el-date-table-cell__text) {
	width: 22px;
	height: 22px;
	line-height: 22px;
}

:global(.cvi-date-popper .el-picker-panel__footer) {
	padding: 4px 8px;
}
</style>
