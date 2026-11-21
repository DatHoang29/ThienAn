<template>
	<div class="cvi-wrap">
		<!-- Câu chưa đúng quy chuẩn mốc: khóa ô, sửa câu tại Quy chuẩn mốc -->
		<template v-if="props.invalid">
			<el-input
				size="small"
				class="cvi-input"
				disabled
				:placeholder="$t('lz.placeholder.sharedataSqlVariable.cursorNeedsFix')"
			/>
		</template>

		<!-- Đối tác đã có mốc gửi: Mốc lần đầu không còn tác dụng nên khóa ô, chữ gợi ý nói rõ trạng thái -->
		<template v-else-if="props.hasCursor">
			<el-input
				size="small"
				class="cvi-input"
				disabled
				prefix-icon="ele-Lock"
				:placeholder="$t('lz.placeholder.sharedataSqlVariable.cursorHasSaved')"
			/>
		</template>

		<!-- Mốc gõ tay: ngày giờ viết liền 8 tới 17 chữ số, hoặc đủ 17 chữ số kèm _<đuôi> (đuôi được để trống), sai luật thì báo ngay dưới ô.
		     Mốc bắt đầu đã nhập có chữ "Từ" đứng trước nên không cần nhãn riêng; tab Mẫu chung: chữ nói rõ chỉ áp cho đối tác chưa có mốc, nhãn đứng riêng một dòng trên ô -->
		<template v-else>
			<span v-if="props.allowEmpty && props.modelValue" class="cvi-from" :class="{ 'is-common': props.commonScope }">
				{{ $t(props.commonScope ? 'lz.label.sharedataSqlVariable.cursorStartFromCommon' : 'lz.label.sharedataSqlVariable.cursorStartFrom') }}
			</span>
			<el-input
				:model-value="props.modelValue ?? ''"
				size="small"
				class="cvi-input"
				:class="{ 'is-error': Boolean(errorText) }"
				:maxlength="CURSOR_VALUE_MAX_LENGTH"
				:disabled="props.disabled"
				:placeholder="$t('lz.placeholder.sharedataSqlVariable.cursorValue')"
				@update:model-value="(value: string) => emit('update:modelValue', value)"
			/>
			<div v-if="errorText" class="cvi-error">{{ errorText }}</div>
		</template>
	</div>
</template>

<script lang="ts" setup name="sharedataCursorValueInput">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { CURSOR_VALUE_MAX_LENGTH, isValidCursorValue } from '/@/utils/sharedata/cursorSections';

const { t: $t } = useI18n();

const props = defineProps<{
	/** Mốc bắt đầu (bảng tham số) hoặc mốc mới (hộp Đặt mốc). */
	modelValue?: string | null;
	/** Được để trống: trống là gửi từ đầu. */
	allowEmpty?: boolean;
	/** Câu chưa đúng quy chuẩn mốc: chưa nhập được mốc. */
	invalid?: boolean;
	/** Đối tác đang mở đã có mốc gửi: Mốc lần đầu không còn tác dụng nên khóa ô. */
	hasCursor?: boolean;
	/** Tab Mẫu chung: Mốc bắt đầu chỉ áp cho đối tác chưa có mốc, chữ trước ô nói rõ điều đó. */
	commonScope?: boolean;
	/** Khoá ô nhập mốc khi phạm vi đang có đăng ký gửi chạy. */
	disabled?: boolean;
}>();

const emit = defineEmits(['update:modelValue']);

/** Mốc đã nhập sai luật thì báo ngay dưới ô; ô trống không báo. */
const errorText = computed(() => {
	const value = (props.modelValue ?? '').trim();
	return value && !isValidCursorValue(value) ? $t('lz.validation.sharedataSqlVariable.cursorValueInvalid') : '';
});

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

.cvi-input {
	flex: 1 1 0;
	width: auto;
	min-width: 0;
	--el-input-height: 28px;

	:deep(.el-input__inner) {
		font-size: 11.5px;
	}

	/* Ô đã khóa: bấm không hiện con trỏ nhập, không bôi chọn chữ; biểu tượng cấm vẫn hiện ở khung ô */
	&.is-disabled :deep(.el-input__inner) {
		pointer-events: none;
		caret-color: transparent;
		user-select: none;
	}

	/* Mốc sai luật: viền đỏ như các ô báo lỗi khác */
	&.is-error :deep(.el-input__wrapper) {
		box-shadow: 0 0 0 1px var(--el-color-danger) inset;
	}
}

.cvi-from {
	flex-shrink: 0;
	font-size: 12px;
	color: var(--el-text-color-regular);

	/* Nhãn dài của tab Mẫu chung đứng riêng một dòng để ô nhập không bị ép hẹp */
	&.is-common {
		flex-basis: 100%;
		line-height: 1.4;
	}
}

/* Nhãn tab Mẫu chung và câu báo sai luật đứng riêng một dòng */
.cvi-wrap:has(.cvi-from.is-common),
.cvi-wrap:has(.cvi-error) {
	flex-wrap: wrap;
}

/* Câu báo mốc sai luật đứng riêng một dòng dưới ô */
.cvi-error {
	flex-basis: 100%;
	font-size: 12px;
	line-height: 1.4;
	color: var(--el-color-danger);
	white-space: normal;
	word-break: normal;
	overflow-wrap: anywhere;
}
</style>
