<template>
	<div class="mail-subject-input" @focusout="onBlur">
		<JoiEditor ref="editorRef" :get-html="editorHtml" @update:getHtml="onHtmlChange"
			:plugins="subjectPlugins" :disable="disabled" :placeholder="placeholder" height="50px"
			resizable :min-height="50"
			:allow-upload-image="false" :text-drop-mime="MAIL_TAG_MIME" :protect-inserted-tags="true" />
		<span class="subject-count">{{ acceptedText.length }} / {{ maxlength }}</span>
	</div>
</template>

<script setup lang="ts" name="mailSubjectInput">
import { onMounted, ref, watch } from 'vue';
import JoiEditor from '/@/components/joditEditor/index.vue';
import { loadMailTags, MAIL_TAG_MIME } from './mailTemplateCache';

const props = withDefaults(defineProps<{
	modelValue?: string | null;
	disabled?: boolean;
	placeholder?: string;
	maxlength?: number;
}>(), { modelValue: '', disabled: false, placeholder: '', maxlength: 256 });
const emit = defineEmits<{
	(e: 'update:modelValue', value: string): void;
	(e: 'blur'): void;
}>();

const editorRef = ref<InstanceType<typeof JoiEditor>>();
const editorHtml = ref('');
const acceptedText = ref('');
let acceptedHtml = '';
let restoring = false;
let codes: string[] = [];
const subjectPlugins = [
	'enter', 'backspace', 'delete-command', 'hotkeys', 'placeholder', 'wrap-nodes',
	'dtd', 'clean-html', 'clipboard', 'paste', 'redo-undo',
];

/** Render saved plain text safely and rebuild protected tokens from the Tags API. */
const toEditorHtml = (text: string): string => {
	const container = document.createElement('div');
	if (!codes.length) {
		container.textContent = text;
		return container.innerHTML;
	}
	const pattern = new RegExp(codes.map((code) => code.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')).join('|'), 'g');
	let position = 0;
	for (const match of text.matchAll(pattern)) {
		container.append(document.createTextNode(text.slice(position, match.index)));
		const token = document.createElement('span');
		token.dataset.mailTag = globalThis.crypto?.randomUUID?.() ??
			`subject-tag-${Date.now()}-${Math.random().toString(36).slice(2)}`;
		token.contentEditable = 'false';
		token.className = 'mail-tag-token';
		token.textContent = match[0];
		container.append(token);
		position = match.index! + match[0].length;
	}
	container.append(document.createTextNode(text.slice(position)));
	return container.innerHTML;
};

const setSubject = (text: string): void => {
	acceptedText.value = text;
	acceptedHtml = toEditorHtml(text);
	editorHtml.value = acceptedHtml;
};

/** The API receives token codes as plain text, never editor markup. */
const onHtmlChange = (html: string): void => {
	if (restoring) return;
	const text = (editorRef.value?.getText() ?? '').replace(/[\u200B\uFEFF]/g, '').replace(/[\r\n]+/g, ' ');
	if (text.length > props.maxlength) {
		const editor = editorRef.value?.getEditor();
		if (!editor) return;
		restoring = true;
		try {
			editor.value = acceptedHtml;
			editor.synchronizeValues();
		} finally {
			restoring = false;
		}
		return;
	}
	acceptedHtml = html;
	editorHtml.value = html;
	acceptedText.value = text;
	if (text !== (props.modelValue ?? '')) emit('update:modelValue', text);
};

watch(() => props.modelValue, (value) => {
	const text = value ?? '';
	if (text !== acceptedText.value) setSubject(text);
}, { immediate: true });

onMounted(async () => {
	try {
		codes = [...new Set((await loadMailTags()).map((tag) => tag.code).filter((code): code is string => !!code))]
			.sort((left, right) => right.length - left.length);
		setSubject(props.modelValue ?? '');
	} catch {
		// Keep the subject editable when the shared tag request fails.
	}
});

const onBlur = (): void => {
	editorRef.value?.getEditor()?.s.save();
	emit('blur');
};

// MailTagBar uses the same insertion API as the content editor.
defineExpose({ insertText: (text: string) => editorRef.value?.insertText(text) ?? false });
</script>

<style scoped>
.mail-subject-input {
	width: 100%;
	min-width: 0;
}
.mail-subject-input :deep(.jodit-toolbar__box),
.mail-subject-input :deep(.jodit-status-bar) {
	display: none;
}
.mail-subject-input :deep(.jodit-wysiwyg) {
	white-space: pre-wrap;
	overflow-wrap: anywhere;
}
.subject-count {
	display: block;
	text-align: right;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}
</style>
