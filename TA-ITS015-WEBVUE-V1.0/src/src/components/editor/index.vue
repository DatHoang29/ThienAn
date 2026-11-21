<template>
	<div class="editor-container">
		<Toolbar :editor="editorRef" :mode="mode" :defaultConfig="toolbarConfig"  />
		<Editor
			:mode="mode"
			:defaultConfig="state.editorConfig"
			:style="{ height }"
			v-model="state.editorVal"
			@onCreated="handleCreated"
			@onChange="handleChange"
		/>
	</div>
</template>

<script setup lang="ts" name="wngEditor">
// https://www.wangeditor.com/v5/for-frame.html#vue3
import '@wangeditor/editor/dist/css/style.css';
import { reactive, shallowRef, watch, onBeforeUnmount, computed } from 'vue';
import { Toolbar, Editor } from '@wangeditor/editor-for-vue';
import { ElMessage } from 'element-plus';
import { getAPI } from '/@/utils/axios-utils';
import { SysFileApi } from '/@/api-services/system';
import { IDomEditor, IToolbarConfig, i18nChangeLanguage } from '@wangeditor/editor';


i18nChangeLanguage('en');

// Define the values passed from the parent component
const props = defineProps({
	// Whether to disable
	disable: {
		type: Boolean,
		default: () => false,
	},
	// Default placeholder for the content box
	placeholder: {
		type: String,
		default: () => 'Please enter content...',
	},
	// https://www.wangeditor.com/v5/getting-started.html#mode-%E6%A8%A1%E5%BC%8F
	// Mode, optional <default|simple>, default is default
	mode: {
		type: String,
		default: () => 'default',
	},
	// Height
	height: {
		type: String,
		default: () => '310px',
	},
	// Two-way binding for getting editor.getHtml()
	getHtml: String,
	// Two-way binding for getting editor.getText()
	getText: String,
	allowUploadImage: {
		type: Boolean,
		default: true
	}
});

// Define the values/events passed from the child component to the parent
const emit = defineEmits(['update:getHtml', 'update:getText']);

// Define variable content
const editorRef = shallowRef();
const state = reactive({
	editorConfig: {
		placeholder: props.placeholder,
		// Menu configuration
		MENU_CONF: {
			uploadImage: {
				fieldName: 'file',
				customUpload(file, insertFn) {
					// console.log('customUpload', file);
					const uploadFun = async () => {
						const rps = await getAPI(SysFileApi).apiSysFileUploadFilePostForm(file);
						if (rps.data.type == 'success' && rps.data.result != null) {
							insertFn(rps.data.result.url, rps.data.result.name, rps.data.result.url);
						} else {
							ElMessage.error('Upload failed!');
						}
					};
					uploadFun();
				},
			},
			insertImage: {
				checkImage(src: string, alt: string, href: string): boolean | string | undefined {
					if (src.indexOf('http') !== 0) {
						return 'Image URL must start with http/https';
					}
					return true;
				},
			},
		},
	},
	editorVal: props.getHtml,
});

const toolbarConfig = computed<Partial<IToolbarConfig>>(() => ({
	excludeKeys: [
		'group-video',
		'insertVideo',
		'uploadVideo',
		'codeBlock',
		'fullScreen',
		"deleteTable",
		"tableHeader",
		"tableFullWidth",
		"todo",
		"netImage",
		...(props.allowUploadImage
			? []
			: [
				'group-image',
				'insertImage',
				'uploadImage'
			])
	]
}));

// Editor callback function
const handleCreated = (editor: IDomEditor) => {
	editorRef.value = editor;
	if (props.disable) editor.disable();
};
// When the editor content changes
const handleChange = (editor: IDomEditor) => {
	emit('update:getHtml', editor.getHtml());
	emit('update:getText', editor.getText());
};
// Insert external tag text using the editor selection and undo history.
const insertText = (text: string): boolean => {
	const editor = editorRef.value as IDomEditor | undefined;
	if (!editor || props.disable || editor.isDisabled()) return false;
	editor.restoreSelection();
	if (!editor.selection) editor.focus(true);
	else editor.focus();
	editor.insertText(text);
	return true;
};
defineExpose({ insertText });

// When the page is destroyed
onBeforeUnmount(() => {
	const editor = editorRef.value;
	if (editor == null) return;
	editor.destroy();
});
// Watch for changes in the disable state
// https://gitee.com/lyt-top/vue-next-admin/issues/I4LM7I
watch(
	() => props.disable,
	(bool) => {
		const editor = editorRef.value;
		if (editor == null) return;
		bool ? editor.disable() : editor.enable();
	},
	{
		deep: true,
	}
);
// Watch for changes in two-way binding values for reflection
watch(
	() => props.getHtml,
	(val) => {
		state.editorVal = val;
	},
	{
		deep: true,
	}
);
</script>
