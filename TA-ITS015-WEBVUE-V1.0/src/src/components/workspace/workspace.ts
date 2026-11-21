import { nextTick, onActivated, onBeforeUnmount, onDeactivated, onMounted, watch } from 'vue';
import type { InjectionKey, Ref } from 'vue';

/** Báo cho WorkspacePage bên trong biết đã có vỏ trang bên ngoài (trang tab). */
export const WORKSPACE_PAGE_KEY: InjectionKey<boolean> = Symbol('workspacePage');

// Workspace state and controller
export interface WorkspaceState<Row> {
	tab: string;
	rows: Row[];
	selectedKey: string;
	loading: boolean;
	saving: boolean;
	page: number;
	pageSize: number;
	total: number;
	pendingUpdates: boolean;
	lastLoaded: Date | null;
	dataRevision?: number;
}

export type WorkspaceSort = { field: string; order: 'asc' | 'desc' };

export interface WorkspaceController<Row> {
	state: WorkspaceState<Row>;
	selected: Ref<Row | null>;
	error: Ref<string>;
	optionsError: Ref<string>;
	selectRow: (row: Row | null, force?: boolean) => Promise<void>;
	load: (preferredKey?: string) => Promise<unknown>;
	reset: () => Promise<void>;
	move: (direction: -1 | 1) => Promise<void>;
	changeSort: (field?: string, order?: 'asc' | 'desc' | null) => Promise<void>;
	changePage: (page: number) => Promise<void>;
	reloadOptions: (force?: boolean) => Promise<void>;
	refresh: () => Promise<void>;
}

// Evidence loading contract
export interface EvidenceImage {
	key: string;
	title?: string;
	titleKey?: string;
	titleParams?: Record<string, number>;
	url?: string;
	statusKey?: string;
}

export interface EvidenceStage {
	value: number;
	labelKey: string;
}

export interface EvidenceLoadOptions {
	stage: number;
	signal: AbortSignal;
	baseUrl?: string;
	createObjectURL: (blob: Blob) => string | undefined;
}

export type EvidenceLoader<RecordType extends object> = (record: RecordType, options: EvidenceLoadOptions) => Promise<EvidenceImage[]>;

// Active-tab keyboard ownership

interface KeyboardState {
	tab: string;
	loading: boolean;
	saving: boolean;
	selectedKey: string;
}

export function useWorkspaceKeyboard(workspace: Ref<HTMLElement | undefined>, tab: string, state: KeyboardState, active: Ref<boolean>, handleKeydown: (event: KeyboardEvent) => void) {
	let focusFrame = 0;
	let listening = false;
	const isActive = () => listening && active.value && state.tab === tab && !!workspace.value?.isConnected;
	const isReady = () => isActive() && !state.loading && !state.saving && !!state.selectedKey;
	const hasOpenDialog = () => Array.from(document.querySelectorAll('[role="dialog"], [role="alertdialog"]')).some((dialog) => dialog.getClientRects().length);

	async function focusWorkspace() {
		await nextTick();
		if (!isReady()) return;
		if (focusFrame) cancelAnimationFrame(focusFrame);
		focusFrame = requestAnimationFrame(() => {
			focusFrame = 0;
			if (!isReady() || hasOpenDialog()) return;
			if (document.activeElement?.closest('input, textarea, select, [role="combobox"], [role="dialog"], [role="alertdialog"], [contenteditable="true"]')) return;
			workspace.value?.focus({ preventScroll: true });
		});
	}

	function onKeydown(event: KeyboardEvent) {
		if (!isReady() || event.defaultPrevented || !['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight'].includes(event.key)) return;
		const target = event.target as HTMLElement | null;
		if (!target) return;
		if (!workspace.value?.contains(target)) {
			if (target !== document.body && target !== document.documentElement) return;
			if (hasOpenDialog()) return;
		}
		handleKeydown(event);
		if (event.defaultPrevented) event.stopPropagation();
	}

	function bindKeyboard() {
		listening = true;
		document.addEventListener('keydown', onKeydown, true);
		void focusWorkspace();
	}

	function unbindKeyboard() {
		listening = false;
		document.removeEventListener('keydown', onKeydown, true);
		if (focusFrame) cancelAnimationFrame(focusFrame);
		focusFrame = 0;
	}

	watch(
		() => !state.loading && !state.saving && !!state.selectedKey && state.tab === tab && active.value,
		(ready) => {
			if (ready) void focusWorkspace();
		},
		{ flush: 'post' }
	);
	onMounted(bindKeyboard);
	onActivated(bindKeyboard);
	onDeactivated(unbindKeyboard);
	onBeforeUnmount(unbindKeyboard);
}
