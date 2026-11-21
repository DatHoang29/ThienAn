
<template>
	<div class="mail-tag-bar">
		<el-tooltip v-for="tag in inlineTags" :key="tag.code!" placement="top" :show-after="300">
			<template #content>
				<div><b>{{ tag.code }}</b></div>
				<div v-if="tag.description">{{ tag.description }}</div>
				<div v-if="tag.sample" class="tip-sample">VD: {{ tag.sample }}</div>
			</template>
			<el-tag size="small" effect="plain" class="tag-chip" :draggable="!disabled"
				@dragstart="onDragStart($event, tag.code!)" @click="!doubleClickInsert && insert(tag.code!)"
				@dblclick="doubleClickInsert && insert(tag.code!)">
				{{ tag.label || tag.code }}
			</el-tag>
		</el-tooltip>
		<span class="hint">{{ $t('lz.label.emailSchedule.tagHint') }}</span>
	</div>
</template>

<script lang="ts" setup name="mailTagBar">
import { computed, nextTick, onMounted, ref } from 'vue';
import type { MailTagInfo } from '/@/api-services/mail';
import { loadMailTags, MAIL_TAG_MIME } from './mailTemplateCache';

const props = defineProps<{
	/** Ref của el-input đích (input hoặc textarea). */
	target?: any;
	/** Giá trị hiện tại của ô đích. */
	modelValue?: string | null;
	disabled?: boolean;
	/** Enable double-click insertion for the subject without inserting twice. */
	doubleClickInsert?: boolean;
}>();

const emits = defineEmits<{
	(e: 'update:modelValue', value: string): void;
}>();

const tags = ref<MailTagInfo[]>([]);
// Keep the exact placeholder code supplied by the Tags API.
const inlineTags = computed(() => tags.value.filter((x) => x.code));

onMounted(async () => {
	try {
		tags.value = await loadMailTags();
	} catch (error) { }
});

/** Phần tử DOM thật của el-input (ElInput expose cả input lẫn textarea). */
const nativeEl = (): HTMLInputElement | HTMLTextAreaElement | undefined =>
	props.target?.textarea ?? props.target?.input;

const insert = (code: string) => {
	if (props.disabled) return;
	if (props.target?.insertText) { props.target.insertText(code); return; }
	const current = props.modelValue ?? '';
	const el = nativeEl();

	// selectionStart vẫn giữ vị trí con trỏ sau khi ô mất focus (bấm chip làm ô mất focus).
	// Ô chưa từng được focus thì trình duyệt trả vị trí cuối chuỗi → chèn vào cuối.
	const start = el?.selectionStart ?? current.length;
	const end = el?.selectionEnd ?? current.length;

	emits('update:modelValue', current.slice(0, start) + code + current.slice(end));

	nextTick(() => {
		if (!el) return;
		el.focus();
		const pos = start + code.length;
		el.setSelectionRange(pos, pos);
	});
};

const onDragStart = (e: DragEvent, code: string) => {
	if (props.disabled || !e.dataTransfer) { e.preventDefault(); return; }
	e.dataTransfer.setData(MAIL_TAG_MIME, code);
	e.dataTransfer.setData('text/plain', code);
	e.dataTransfer.effectAllowed = 'copy';
};
</script>

<style scoped lang="scss">
.mail-tag-bar {
	display: flex;
	flex-wrap: wrap;
	align-items: center;
	gap: 4px;
}

.tag-chip {
	cursor: grab;
	user-select: none;

	&:active {
		cursor: grabbing;
	}
}

.tip-sample {
	margin-top: 2px;
	opacity: 0.8;
}

.hint {
	margin-left: 4px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}
</style>
