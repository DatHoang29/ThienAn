
<template>
	<div class="mail-addr" :class="{ 'is-focus': state.focused, 'is-disabled': disabled }" @click="focusInput">
		<el-tag v-for="(email, i) in state.chips" :key="email" size="small" closable :disable-transitions="true"
			type="info" class="mail-addr__tag" :title="email" @close="remove(i)" @dblclick.stop="edit(i)">
			{{ email }}
		</el-tag>
		<input ref="inputRef" v-model="state.draft" class="mail-addr__input" type="text" autocomplete="off"
			:disabled="disabled" :placeholder="state.chips.length ? '' : placeholder"
			@input="emitValue" @keydown="onKeydown" @paste="onPaste" @focus="onFocus" @blur="onBlur" />
		<!-- Gợi ý từ sổ địa chỉ (RptEmailAddress). mousedown.prevent để bấm chọn không làm ô nhập mất focus -->
		<ul v-if="suggestions.length" class="mail-addr__suggest" @mousedown.prevent>
			<li v-for="(email, i) in suggestions" :key="email" :class="{ 'is-active': i === state.active }"
				@mouseenter="state.active = i" @click.stop="pick(email)">
				{{ email }}
			</li>
		</ul>
	</div>
</template>

<script lang="ts" setup name="mailAddressInput">
import { computed, reactive, ref, watch } from 'vue';
import { emailRegex } from '/@/types/validators';
import { useMailReportCatalog } from '/@/hooks/useMailReportCatalog';

const props = defineProps<{
	modelValue?: string | null;
	placeholder?: string;
	disabled?: boolean;
}>();

const emits = defineEmits<{
	(e: 'update:modelValue', value: string): void;
	(e: 'blur'): void;
}>();

const SEPARATORS = /[;,\s]+/;
const COMMIT_KEYS = new Set(['Enter', ',', ';', ' ']);

const state = reactive({
	chips: [] as string[],
	draft: '',
	focused: false,
	/** Sổ địa chỉ để gợi ý — nạp khi ô nhập được focus lần đầu. */
	book: [] as string[],
	/** Dòng gợi ý đang chọn bằng phím mũi tên; -1 = chưa chọn. */
	active: -1,
});

const { loadMailAddresses, filterMailAddresses } = useMailReportCatalog();

/** Gợi ý khi đang gõ: địa chỉ chứa phần đã gõ, bỏ các địa chỉ đã có trong ô. */
const suggestions = computed(() =>
	state.focused && state.draft.trim() ? filterMailAddresses(state.book, state.draft, state.chips) : []
);

watch(() => state.draft, () => (state.active = -1));

const onFocus = async () => {
	state.focused = true;
	state.book = await loadMailAddresses();
};
const inputRef = ref<HTMLInputElement>();

const isValid = (email: string) => emailRegex.test(email);
const split = (text?: string | null) => (text ?? '').split(SEPARATORS).map((x) => x.trim()).filter(Boolean);

let lastEmitted: string | null = null;

const emitValue = () => {
	const value = [...state.chips, ...split(state.draft)].join('; ');
	lastEmitted = value;
	emits('update:modelValue', value);
};
const absorb = (text: string) => {
	const keys = new Set(state.chips.map((x) => x.toLowerCase()));
	const rest: string[] = [];

	split(text).forEach((x) => {
		if (!isValid(x)) return void rest.push(x);
		if (keys.has(x.toLowerCase())) return;
		keys.add(x.toLowerCase());
		state.chips.push(x);
	});

	return rest.join('; ');
};

watch(
	() => props.modelValue,
	(value) => {
		if ((value ?? '') === lastEmitted) return;
		state.chips = [];
		state.draft = absorb(value ?? '');
		lastEmitted = value ?? '';
	},
	{ immediate: true }
);

const commitDraft = () => {
	state.draft = absorb(state.draft);
	emitValue();
};

const remove = (index: number) => {
	state.chips.splice(index, 1);
	emitValue();
};

const edit = (index: number) => {
	if (props.disabled) return;
	const [email] = state.chips.splice(index, 1);
	state.draft = [state.draft.trim(), email].filter(Boolean).join('; ');
	emitValue();
	focusInput();
};

/** Chọn một gợi ý: thành chip, xoá phần đang gõ. */
const pick = (email: string) => {
	state.draft = absorb(email);
	state.active = -1;
	emitValue();
	focusInput();
};

const onKeydown = (e: KeyboardEvent) => {
	if (e.isComposing) return; 

	const list = suggestions.value;
	if (list.length) {
		if (e.key === 'ArrowDown') {
			e.preventDefault();
			state.active = (state.active + 1) % list.length;
			return;
		}
		if (e.key === 'ArrowUp') {
			e.preventDefault();
			state.active = state.active <= 0 ? list.length - 1 : state.active - 1;
			return;
		}
		if ((e.key === 'Enter' || e.key === 'Tab') && state.active >= 0) {
			e.preventDefault();
			pick(list[state.active]);
			return;
		}
	}

	const draft = state.draft.trim();

	if (COMMIT_KEYS.has(e.key) || (e.key === 'Tab' && draft)) {
		if (e.key !== 'Tab' || isValid(draft)) e.preventDefault();
		if (isValid(draft)) commitDraft();
		return;
	}

	if (e.key === 'Backspace' && !state.draft && state.chips.length) {
		e.preventDefault();
		edit(state.chips.length - 1);
	}
};

const onPaste = (e: ClipboardEvent) => {
	const text = e.clipboardData?.getData('text') ?? '';
	if (!SEPARATORS.test(text.trim())) return; 
	e.preventDefault();
	state.draft = absorb(`${state.draft} ${text}`);
	emitValue();
};

const onBlur = () => {
	state.focused = false;
	commitDraft();
	emits('blur');
};

const focusInput = () => inputRef.value?.focus();

defineExpose({ focus: focusInput });
</script>

<style scoped lang="scss">
.mail-addr {
	position: relative;
	display: flex;
	flex-wrap: wrap;
	align-items: center;
	gap: 4px;
	width: 100%;
	min-height: 32px;
	padding: 3px 8px;
	border-radius: var(--el-border-radius-base);
	box-shadow: 0 0 0 1px var(--el-border-color) inset;
	background: var(--el-fill-color-blank);
	cursor: text;
	transition: box-shadow 0.2s;

	&:hover {
		box-shadow: 0 0 0 1px var(--el-border-color-hover) inset;
	}

	&.is-focus {
		box-shadow: 0 0 0 1px var(--el-color-primary) inset;
	}

	&.is-disabled {
		background: var(--el-disabled-bg-color);
		cursor: not-allowed;
	}
}

:global(.el-form-item.is-error .mail-addr) {
	box-shadow: 0 0 0 1px var(--el-color-danger) inset;
}

.mail-addr__tag {
	max-width: 100%;
	cursor: default;
}

.mail-addr__suggest {
	position: absolute;
	top: calc(100% + 4px);
	left: 0;
	right: 0;
	z-index: 10;
	max-height: 240px;
	margin: 0;
	padding: 4px 0;
	overflow-y: auto;
	list-style: none;
	background: var(--el-bg-color-overlay);
	border: 1px solid var(--el-border-color-light);
	border-radius: var(--el-border-radius-base);
	box-shadow: var(--el-box-shadow-light);

	li {
		padding: 0 12px;
		line-height: 30px;
		font-size: 13px;
		color: var(--el-text-color-regular);
		cursor: pointer;
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;

		&.is-active {
			background: var(--el-fill-color-light);
			color: var(--el-color-primary);
		}
	}
}

.mail-addr__input {
	flex: 1;
	min-width: 140px;
	height: 24px;
	border: none;
	outline: none;
	background: transparent;
	font-size: 13px;
	color: var(--el-text-color-regular);

	&::placeholder {
		color: var(--el-text-color-placeholder);
	}
}
</style>
