import type { Jodit } from 'jodit';

const TAG_SELECTOR = 'span[data-mail-tag]';

/** Protect inserted tokens while keeping their original code in the saved HTML. */
export function protectMailTags(editor: Jodit) {
    const root = editor.editor;
    const doc = root.ownerDocument;
    let restoring = false;
    let disposed = false;
    let deletingToken = false;
    let locked = new Map<string, string>();
    let acceptedHtml = '';

    const tags = (container: ParentNode): HTMLSpanElement[] =>
        Array.from(container.querySelectorAll<HTMLSpanElement>(TAG_SELECTOR));

    const hydrate = (): void => {
        tags(root).forEach((tag) => {
            tag.contentEditable = 'false';
            tag.draggable = false;
            tag.classList.add('mail-tag-token');
        });
    };

    const preservesTags = (container: ParentNode): boolean => {
        const current = tags(container);
        return [...locked].every(([id, code]) => {
            const matches = current.filter((tag) => tag.dataset.mailTag === id);
            // Removing a whole token is valid; a remaining token must keep its code intact.
            return matches.every((tag) => tag.textContent === code && tag.contentEditable === 'false');
        });
    };

    const parse = (html: string): DocumentFragment => {
        const template = doc.createElement('template');
        template.innerHTML = html;
        return template.content;
    };

    const restore = (): void => {
        if (restoring || disposed) return;
        restoring = true;
        try {
            editor.setNativeEditorValue(acceptedHtml);
        } finally {
            restoring = false;
        }
    };

    const beforeGetValue = (): string | undefined => {
        if (disposed || restoring || preservesTags(root)) return;
        restore();
        return acceptedHtml;
    };

    const beforeSetNativeValue = (data: { value: string }): void => {
        if (!disposed && !restoring && !preservesTags(parse(data.value))) {
            data.value = acceptedHtml;
        }
    };

    const afterGetValue = (data: { value: string }): void => {
        if (disposed || restoring) return;
        const content = parse(data.value);
        if (!preservesTags(content)) {
            data.value = acceptedHtml;
            restore();
            return;
        }
        acceptedHtml = data.value;
        tags(content).forEach((tag) => {
            locked.set(tag.dataset.mailTag!, tag.textContent ?? '');
        });
    };

    /** Any range touching a token includes that token in full. */
    const expandSelection = (direction?: 'backward' | 'forward'): boolean => {
        const selection = doc.getSelection();
        if (!selection?.rangeCount) return false;
        const range = selection.getRangeAt(0).cloneRange();
        if (!root.contains(range.commonAncestorContainer)) return false;
        const affected = tags(root).filter((tag) => {
            if (!range.collapsed || tag.contains(range.startContainer)) return range.intersectsNode(tag);
            if (!direction) return false;
            const edge = doc.createRange();
            edge.selectNode(tag);
            edge.collapse(direction === 'forward');
            const order = range.compareBoundaryPoints(Range.START_TO_START, edge);
            if (direction === 'backward' ? order < 0 : order > 0) return false;
            const between = doc.createRange();
            if (direction === 'backward') {
                between.setStartAfter(tag);
                between.setEnd(range.startContainer, range.startOffset);
            } else {
                between.setStart(range.startContainer, range.startOffset);
                between.setEndBefore(tag);
            }
            return !between.cloneContents().querySelector('br, p, div, li, tr, td') &&
                between.toString().replace(/[\u200B\uFEFF]/g, '') === '';
        });
        if (!affected.length) return false;
        const first = doc.createRange();
        first.selectNode(affected[0]);
        const last = doc.createRange();
        last.selectNode(affected[affected.length - 1]);
        if (range.compareBoundaryPoints(Range.START_TO_START, first) > 0) range.setStartBefore(affected[0]);
        if (range.compareBoundaryPoints(Range.END_TO_END, last) < 0) range.setEndAfter(affected[affected.length - 1]);
        selection.removeAllRanges();
        selection.addRange(range);
        return true;
    };

    const deleteSelectedToken = (direction?: 'backward' | 'forward'): boolean => {
        if (deletingToken || editor.o.readonly || editor.o.disabled || !expandSelection(direction)) return false;
        deletingToken = true;
        try {
            editor.execCommand('delete');
            editor.synchronizeValues();
        } finally {
            deletingToken = false;
        }
        return true;
    };

    const stop = (event: Event): void => {
        event.preventDefault();
        event.stopImmediatePropagation();
    };
    const onBeforeInput = (event: InputEvent): void => {
        const direction = event.inputType.endsWith('Backward') ? 'backward' :
            event.inputType.endsWith('Forward') ? 'forward' : undefined;
        if (event.inputType.startsWith('delete')) {
            if (deleteSelectedToken(direction)) stop(event);
        } else {
            expandSelection();
        }
    };
    const onKeyDown = (event: KeyboardEvent): void => {
        const direction = event.key === 'Backspace' ? 'backward' : event.key === 'Delete' ? 'forward' : undefined;
        if (direction && deleteSelectedToken(direction)) stop(event);
        else if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'x') expandSelection();
    };
    const onReplaceSelection = (event: Event): void => {
        expandSelection();
    };
    const onDragStart = (event: DragEvent): void => {
        const target = event.target instanceof Element ? event.target : null;
        if (target?.closest(TAG_SELECTOR) || expandSelection()) stop(event);
    };

    hydrate();
    acceptedHtml = editor.value;
    tags(root).forEach((tag) => locked.set(tag.dataset.mailTag!, tag.textContent ?? ''));
    editor.e.on('beforeGetValueFromEditor', beforeGetValue);
    editor.e.on('afterGetValueFromEditor', afterGetValue);
    editor.e.on('beforeSetNativeEditorValue', beforeSetNativeValue);
    root.addEventListener('beforeinput', onBeforeInput, true);
    root.addEventListener('keydown', onKeyDown, true);
    root.addEventListener('cut', onReplaceSelection, true);
    root.addEventListener('paste', onReplaceSelection, true);
    root.addEventListener('dragstart', onDragStart, true);

    // Covers commands/history and DOM edits that bypass native beforeinput.
    const observer = new MutationObserver(() => {
        if (disposed || restoring) return;
        if (!preservesTags(root)) {
            restore();
            editor.synchronizeValues();
        }
    });
    observer.observe(root, { subtree: true, childList: true, characterData: true,
        attributes: true, attributeFilter: ['contenteditable', 'data-mail-tag'] });

    return {
        createTag(text: string): HTMLSpanElement {
            const tag = doc.createElement('span');
            tag.dataset.mailTag = globalThis.crypto?.randomUUID?.() ??
                `mail-tag-${Date.now()}-${Math.random().toString(36).slice(2)}`;
            tag.contentEditable = 'false';
            tag.draggable = false;
            tag.className = 'mail-tag-token';
            tag.textContent = text;
            return tag;
        },
        expandSelection,
        /** An explicit parent update (loading/changing a template) establishes a new document. */
        setContent(html: string): void {
            locked.clear();
            acceptedHtml = html;
            restoring = true;
            try {
                editor.value = html;
                hydrate();
            } finally {
                restoring = false;
            }
            afterGetValue({ value: editor.value });
        },
        destroy(): void {
            disposed = true;
            observer.disconnect();
            root.removeEventListener('beforeinput', onBeforeInput, true);
            root.removeEventListener('keydown', onKeyDown, true);
            root.removeEventListener('cut', onReplaceSelection, true);
            root.removeEventListener('paste', onReplaceSelection, true);
            root.removeEventListener('dragstart', onDragStart, true);
            editor.e.off('beforeGetValueFromEditor', beforeGetValue);
            editor.e.off('afterGetValueFromEditor', afterGetValue);
            editor.e.off('beforeSetNativeEditorValue', beforeSetNativeValue);
            locked.clear();
        },
    };
}
