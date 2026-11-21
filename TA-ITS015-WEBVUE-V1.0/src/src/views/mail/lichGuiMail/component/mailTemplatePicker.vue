<!--
	Dropdown chọn mẫu thư phía trên editor của dialog lịch gửi.

	Danh sách mẫu đang bật và nội dung lấy từ API RptEmailTemplate/List.
	Chọn một mẫu để soạn nội dung riêng cho lịch gửi.
-->
<template>
	<div class="mail-tpl-picker">
		<div v-if="state.loading && !state.templates.length" class="hint">
			<el-icon class="is-loading"><ele-Loading /></el-icon>
		</div>
		<el-empty v-else-if="!state.templates.length" :image-size="50"
			:description="$t('lz.label.emailSchedule.noTemplate')" />

		<el-select v-else :model-value="modelValue" :disabled="disabled || state.loading" :loading="state.loading"
			filterable class="w100" :aria-label="$t('lz.entity.emailSchedule.template')"
			:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.emailSchedule.template') })"
			@change="(code) => { const tpl = findTemplate(code); if (tpl) choose(tpl); }">
			<el-option v-for="tpl in state.templates" :key="tpl.code!" :value="tpl.code!" :label="tpl.name || tpl.code!" />
		</el-select>
	</div>
</template>

<script lang="ts" setup name="mailTemplatePicker">
import { onMounted, reactive } from 'vue';
import type { RptEmailTemplateOutput } from '/@/api-services/mail';
import { loadMailTemplates } from './mailTemplateCache';

const props = defineProps<{
	modelValue?: string | null;
	disabled?: boolean;
}>();

const emits = defineEmits<{
	(e: 'update:modelValue', code: string): void;
	/** Người dùng CHỌN một mẫu khác mẫu đang chọn. Trả kèm nội dung để cha nạp vào editor. */
	(e: 'change', tpl: RptEmailTemplateOutput, previous?: RptEmailTemplateOutput): void;
	/** Danh sách mẫu đã nạp xong. */
	(e: 'loaded', templates: RptEmailTemplateOutput[]): void;
}>();

const state = reactive({
	loading: false,
	templates: [] as RptEmailTemplateOutput[],
});

const choose = (tpl: RptEmailTemplateOutput) => {
	if (props.disabled || !tpl.code || tpl.code === props.modelValue) return;

	const previous = state.templates.find((x) => x.code === props.modelValue);
	emits('update:modelValue', tpl.code);
	emits('change', tpl, previous);
};

const load = async () => {
	try {
		state.loading = true;
		state.templates = await loadMailTemplates();
		emits('loaded', state.templates);
	} catch (error) {
	} finally {
		state.loading = false;
	}
};

/** Tra mẫu và nội dung theo mã. */
const findTemplate = (code?: string | null) => state.templates.find((x) => x.code === code);

onMounted(load);

defineExpose({ findTemplate, reload: load });
</script>

<style scoped lang="scss">
.mail-tpl-picker {
	width: 100%;
}

.hint {
	font-size: 12px;
	color: var(--el-text-color-secondary);
}
</style>
