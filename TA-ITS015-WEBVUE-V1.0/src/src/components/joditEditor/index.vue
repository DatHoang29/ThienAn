<template>
    <div class="editor-container">
        <textarea ref="areaRef"></textarea>

        <input ref="fileInputRef" type="file" accept="image/*" multiple style="display: none"
            @change="handleFileChange" />
    </div>
</template>

<script setup lang="ts" name="joditEditor">
import { onMounted, onBeforeUnmount, ref, watch } from 'vue';
import { Jodit } from 'jodit';
import { protectMailTags } from './protectedTags';
import 'jodit/esm/plugins/all.js';
import 'jodit/es2021/jodit.min.css';

import { ElMessage } from 'element-plus';


interface Props {
    disable?: boolean;
    placeholder?: string;
    mode?: string;
    height?: string;
    width?: string;
    /** Allow the user to drag the editor's bottom corner to resize vertically. */
    resizable?: boolean;
    minHeight?: number | string;
    getHtml?: string;
    getText?: string;
    allowUploadImage?: boolean;
    /** The consuming screen uploads the file and returns the image URL. */
    uploadImage?: (file: File) => Promise<string>;
    textDropMime?: string;
    /** Keep inserted tag codes noneditable; deletion removes the whole token. */
    protectInsertedTags?: boolean;
    /** Tool names declared by the consuming screen; use 'customUploadImage' for the upload picker. */
    buttons?: string[];
    excludeTools?: string[];
    /** Enabled registered plugin names (kebab-case). Required plugin dependencies are included automatically. */
    plugins?: string[];
}

const props = withDefaults(defineProps<Props>(), {
    disable: false,
    placeholder: 'Please enter content...',
    mode: 'default',
    height: '310px',
    resizable: false,
    getHtml: '',
    getText: '',
    allowUploadImage: true,
    textDropMime: '',
    protectInsertedTags: false,
    buttons: () => [],
    excludeTools: () => [],
    plugins: () => [],
});

const emit = defineEmits<{
    (e: 'update:getHtml', value: string): void;
    (e: 'update:getText', value: string): void;
}>();

const areaRef = ref<HTMLTextAreaElement | null>(null);
const fileInputRef = ref<HTMLInputElement | null>(null);

let editor: Jodit | null = null;
let destroyed = false;

let dropArea: HTMLElement | null = null;
let tagProtection: ReturnType<typeof protectMailTags> | null = null;

const getEditorText = (): string => {
    if (!editor) return '';

    return editor.editor?.innerText ?? '';
};

const handleChange = (): void => {
    if (!editor || destroyed) return;

    const html = editor.value ?? '';
    const text = getEditorText();

    emit('update:getHtml', html);
    emit('update:getText', text);
};

const isExternalTextDrag = (event: DragEvent): boolean =>
    !!props.textDropMime && !!event.dataTransfer &&
    Array.from(event.dataTransfer.types).includes(props.textDropMime);

const onExternalTextDragOver = (event: DragEvent): void => {
    if (!isExternalTextDrag(event)) return;
    event.preventDefault();
    event.stopImmediatePropagation();
    if (event.dataTransfer) {
        event.dataTransfer.dropEffect = editor && !destroyed && !props.disable && editor.isEditorMode() ? 'copy' : 'none';
    }
};

const onExternalTextDrop = (event: DragEvent): void => {
    if (!isExternalTextDrag(event)) return;
    event.preventDefault();
    event.stopImmediatePropagation();
    if (!editor || destroyed || props.disable || !editor.isEditorMode()) return;
    const text = event.dataTransfer?.getData(props.textDropMime) ?? '';
    if (!text) return;

    editor.focus();
    if (!editor.s.insertCursorAtPoint(event.clientX, event.clientY)) return;
    tagProtection?.expandSelection();
    editor.s.insertNode(tagProtection?.createTag(text) ?? editor.editor.ownerDocument.createTextNode(text));
    editor.synchronizeValues();
};

const uploadImage = async (file: File): Promise<void> => {
    if (!editor || destroyed || props.disable || !props.allowUploadImage || !props.uploadImage) return;

    if (!file.type.startsWith('image/')) {
        ElMessage.warning('Please select an image file!');
        return;
    }

    try {
        const url = await props.uploadImage(file);
        console.log("url", url);
        if (!url) throw new Error('Upload did not return an image URL.');
        editor?.s.insertImage(url, null, null);
        editor?.synchronizeValues();
    } catch (error) {
        console.error('Jodit upload image error:', error);
        ElMessage.error('Upload failed!');
    }
};

const openImagePicker = (): void => {
    if (!editor || props.disable || !props.allowUploadImage || !props.uploadImage) return;

    fileInputRef.value?.click();
};

const handleFileChange = async (event: Event): Promise<void> => {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);

    if (!files.length) return;

    if (editor) {
        editor.s.restore();
    }

    for (const file of files) {
        await uploadImage(file);
    }

    input.value = '';
};

const getExcludedTools = (): string[] => [...new Set([
    ...props.excludeTools,
    ...(!props.allowUploadImage ? ['image', 'customUploadImage'] : []),
    ...(!props.uploadImage ? ['customUploadImage'] : []),
])];

const getToolbarButtons = (): typeof Jodit.defaultOptions.buttons =>
    props.buttons.map((button) => {
        if (button !== 'customUploadImage') return button;
        return {
            name: 'customUploadImage',
            icon: 'image',
            tooltip: 'Upload image',
            exec: () => {
                if (!editor || props.disable || !props.allowUploadImage || !props.uploadImage) return false;
                editor.s.save();
                openImagePicker();
                return false;
            },
        };
    });

const refreshToolbar = (): void => {
    if (!editor || destroyed) return;
    const buttons = getToolbarButtons();
    const removed = getExcludedTools();
    editor.o.buttons = buttons;
    editor.o.removeButtons = removed;
    editor.toolbar.removeButtons = removed;
    editor.toolbar.build(buttons);
};


const getEnabledPlugins = (): string[] => {
    const enabled = new Set<string>();
    const include = (name: string): void => {
        const normalized = name.trim().replace(/([a-z0-9])([A-Z])/g, '$1-$2').toLowerCase();
        if (!normalized || enabled.has(normalized)) return;
        const plugin = Jodit.plugins.get(normalized);
        if (!plugin) {
            console.warn(`Jodit plugin is not registered: ${normalized}`);
            return;
        }
        enabled.add(normalized);
        const dependencies = (plugin as { requires?: string[] }).requires ?? [];
        dependencies.forEach(include);
    };
    props.plugins.forEach(include);
    if (props.resizable) include('resize-handler');
    return [...enabled];
};

const destroyEditor = (): void => {
    destroyed = true;
    dropArea?.removeEventListener('dragover', onExternalTextDragOver, true);
    dropArea?.removeEventListener('drop', onExternalTextDrop, true);
    dropArea = null;
    tagProtection?.destroy();
    tagProtection = null;
    const instance = editor;
    editor = null;
    if (instance) {
        instance.events.off('change', handleChange);
        instance.destruct();
    }
};

const createEditor = (html: string = props.getHtml): void => {
    if (!areaRef.value) return;

    destroyed = false;

    editor = Jodit.make(areaRef.value, {
        height: props.height,
        width: props.width,
        ...(props.minHeight !== undefined ? { minHeight: props.minHeight } : {}),
        ...(props.resizable ? { allowResizeY: true, allowResizeX: false } : {}),
        placeholder: props.placeholder,

        readonly: props.disable,
        disabled: props.disable,

        language: 'en',
        safeMode: true,
        safePluginsList: getEnabledPlugins(),

        toolbar: true,
        toolbarAdaptive: false,

        buttons: getToolbarButtons(),
        removeButtons: getExcludedTools(),

        uploader: {
            insertImageAsBase64URI: false,
        },

        enableDragAndDropFileToEditor: false,

        showCharsCounter: false,
        showWordsCounter: false,
        showXPathInStatusbar: false,

        askBeforePasteHTML: false,

        toolbarButtonSize:
            props.mode === 'simple' ? 'small' : 'middle',
    });

    editor.value = html ?? '';
    if (props.protectInsertedTags) tagProtection = protectMailTags(editor);

    editor.events.on('change', handleChange);
    dropArea = editor.editor;
    dropArea.addEventListener('dragover', onExternalTextDragOver, true);
    dropArea.addEventListener('drop', onExternalTextDrop, true);
};

onMounted(() => createEditor());

// Plugin initialization happens at creation time; changing this list resets undo history.
watch(
    () => `${props.protectInsertedTags}:${getEnabledPlugins().sort().join(',')}`,
    () => {
        if (!editor || destroyed) return;
        const html = editor.value;
        destroyEditor();
        createEditor(html);
    }
);

watch(
    () => props.getHtml,
    (value) => {
        if (!editor || destroyed) return;

        const newValue = value ?? '';

        if (editor.value !== newValue) {
            if (tagProtection) tagProtection.setContent(newValue);
            else editor.value = newValue;
        }
    }
);

watch(
    () => props.disable,
    (value) => {
        if (!editor || destroyed) return;

        editor.setReadOnly(value);
        editor.setDisabled(value);
    }
);

watch(
    [() => props.buttons, () => props.excludeTools, () => props.allowUploadImage, () => props.uploadImage],
    refreshToolbar,
    { deep: true }
);

const insertText = (text: string): boolean => {
    if (!editor || destroyed || props.disable) return false;

    try {
        editor.s.restore();
        editor.focus();

        // Create a text node to prevent HTML injection
        tagProtection?.expandSelection();
        const textNode = tagProtection?.createTag(text) ?? editor.editor.ownerDocument.createTextNode(text);

        // Insert at cursor position
        editor.s.insertNode(textNode);

        editor.synchronizeValues();

        return true;
    } catch (error) {
        console.error('Jodit insertText error:', error);
        return false;
    }
};

defineExpose({
    insertText,

    getHtml: (): string => editor?.value ?? '',

    getText: (): string => getEditorText(),

    focus: (): void => {
        editor?.focus();
    },

    getEditor: (): Jodit | null => editor,
});

onBeforeUnmount(destroyEditor);
</script>

<style scoped>
.editor-container {
    width: 100%;
    min-width: 0;
}

.editor-container :deep(.jodit-container) {
    width: 100%;
}

/* theme/app.scss reset `* { padding: 0 }` removes the list gutter, so ol/ul markers get clipped.
   Restore the browser default so the editor matches how mail clients render the HTML. */
.editor-container :deep(.jodit-wysiwyg ol),
.editor-container :deep(.jodit-wysiwyg ul) {
    padding-left: 40px;
}

.editor-container :deep(.jodit-wysiwyg .mail-tag-token) {
    display: inline-block;
    padding: 0 5px;
    border: 1px solid var(--el-color-primary-light-5);
    border-radius: 4px;
    background: var(--el-color-primary-light-9);
    color: var(--el-color-primary);
    white-space: nowrap;
    cursor: default;
}
</style>
