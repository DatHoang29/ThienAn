<template>
	<!--
		Chọn kịch bản Video Wall: thẻ chọn (icon · nhãn nhỏ · tên đậm · mũi tên) + panel danh sách có tìm kiếm
		(cùng kiểu "Thay đổi bản đồ" của trang Thiết lập vị trí bản đồ). Dùng ở trang Giám sát & Điều khiển và Quản lý kịch bản.
		Chỉ báo lựa chọn qua `select`; trang cha quyết định có chuyển kịch bản không (VD hỏi bỏ thay đổi chưa lưu).
	-->
	<el-popover v-model:visible="open" placement="bottom-start" :width="420" :offset="8" :show-arrow="false" trigger="click"
		popper-class="vw-scene-popover" :disabled="isDisabled" @show="onShow">
		<template #reference>
			<button type="button" class="scene-picker" :class="{ 'is-disabled': isDisabled, 'is-open': open, 'is-block': block }" :disabled="isDisabled">
				<span class="scene-picker__icon" :class="{ 'is-live': isServing(activeScene) }">
					<el-icon><ele-Grid /></el-icon>
				</span>
				<span class="scene-picker__text">
					<span class="scene-picker__label">{{ label }}</span>
					<span class="scene-picker__name" :class="{ 'is-placeholder': !activeScene }">{{ triggerText }}</span>
				</span>
				<el-icon class="scene-picker__arrow"><ele-ArrowDown /></el-icon>
			</button>
		</template>

		<div class="sp-panel">
			<div class="sp-panel__head">
				<div class="sp-panel__title-row">
					<span class="sp-panel__title-icon"><el-icon><ele-Film /></el-icon></span>
					<span class="sp-panel__title">{{ $t('lz.label.videoWall.sceneSwitchTitle') }}</span>
					<span class="sp-panel__summary">{{ $t('lz.label.videoWall.sceneCountN', { count: scenes.length }) }}</span>
				</div>
				<el-input v-model="keyword" clearable prefix-icon="ele-Search" class="sp-panel__search"
					:placeholder="$t('lz.label.videoWall.sceneSearchPlaceholder')" />
			</div>
			<div ref="listRef" class="sp-panel__body">
				<button v-for="scene in filteredScenes" :key="scene.id!" type="button" class="sp-item" :class="{ 'is-active': scene.id === modelValue }"
					:title="scene.name || scene.code || ''" @click="onSelect(scene)">
					<span class="sp-item__icon" :class="{ 'is-live': isServing(scene) }">
						<el-icon><ele-Film /></el-icon>
					</span>
					<span class="sp-item__text">
						<span class="sp-item__name">{{ scene.name || scene.code }}</span>
						<span class="sp-item__meta">{{ scene.code }}</span>
					</span>
					<span v-if="isServing(scene)" class="sp-item__live"><i />{{ $t('lz.label.videoWall.sceneServing') }}</span>
					<span v-if="isDefaultOf(scene)" class="sp-item__tag"><el-icon><ele-StarFilled /></el-icon>{{ $t('lz.label.base.isDefault') }}</span>
					<span v-if="isOld?.(scene)" class="sp-item__tag is-muted" :title="$t('lz.message.videoWall.sceneOldLayoutReadonly')">
						{{ $t('lz.label.videoWall.oldLayout') }}
					</span>
					<span class="sp-item__check">
						<el-icon v-if="scene.id === modelValue"><ele-CircleCheckFilled /></el-icon>
					</span>
				</button>
				<div v-if="!filteredScenes.length" class="sp-empty">
					<el-icon class="sp-empty__icon"><ele-Search /></el-icon>
					<span>{{ $t('lz.label.base.noData') }}</span>
				</div>
			</div>
		</div>
	</el-popover>
</template>

<script lang="ts" setup name="vwScenePicker">
import { computed, nextTick, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { DefaultEnum } from '/@/api-services/videoWall';
import type { VwSceneOutput } from '/@/api-services/videoWall';

interface Props {
	scenes: VwSceneOutput[];
	/** ID kịch bản đang chọn ('' = chưa chọn). */
	modelValue?: string;
	/** Nhãn nhỏ phía trên tên (VD "Chọn kịch bản"). */
	label: string;
	/** Chữ hiện khi chưa chọn; bỏ trống ⇒ "Chọn kịch bản" / "Chưa có kịch bản". */
	placeholder?: string;
	/** Kịch bản BE đang phát ⇒ icon xanh + nhãn "Đang phát" trong danh sách. */
	servingSceneId?: string;
	disabled?: boolean;
	/** Thẻ chọn giãn hết chiều ngang (cột hẹp); mặc định rộng cố định 280px. */
	block?: boolean;
	/** Kịch bản thuộc bố cục cũ ⇒ nhãn "Bố cục cũ". */
	isOld?: (scene: VwSceneOutput) => boolean;
	/** Kịch bản mặc định; mặc định theo cờ IsDefault. */
	isDefault?: (scene: VwSceneOutput) => boolean;
}
const props = withDefaults(defineProps<Props>(), { modelValue: '', placeholder: '', servingSceneId: '', disabled: false, block: false });
const emit = defineEmits<{
	(e: 'update:modelValue', id: string): void;
	(e: 'select', scene: VwSceneOutput): void;
}>();

const { t: $t } = useI18n();

const open = ref(false);
const keyword = ref('');
const listRef = ref<HTMLElement | null>(null);

const activeScene = computed(() => (props.modelValue ? props.scenes.find((s) => s.id === props.modelValue) : undefined));
const isDisabled = computed(() => props.disabled || !props.scenes.length);
const isServing = (scene?: VwSceneOutput) => !!scene?.id && scene.id === props.servingSceneId;
const isDefaultOf = (scene: VwSceneOutput) => (props.isDefault ? props.isDefault(scene) : scene.isDefault === DefaultEnum.NUMBER_1);

const triggerText = computed(() => {
	if (activeScene.value) return activeScene.value.name || activeScene.value.code;
	if (props.placeholder) return props.placeholder;
	return props.scenes.length ? $t('lz.label.videoWall.selectScene') : $t('lz.label.videoWall.noScene');
});

/** Bỏ dấu tiếng Việt để tìm "binh thuong" ra "Bình thường". */
const normalize = (text: unknown) =>
	String(text ?? '')
		.toLowerCase()
		.normalize('NFD')
		.replace(/[̀-ͯ]/g, '')
		.replace(/đ/g, 'd');

const filteredScenes = computed(() => {
	const key = normalize(keyword.value.trim());
	if (!key) return props.scenes;
	return props.scenes.filter((s) => normalize(s.name).includes(key) || normalize(s.code).includes(key));
});

/** Mở panel ⇒ xoá từ khoá cũ và cuộn tới kịch bản đang chọn. */
function onShow() {
	keyword.value = '';
	nextTick(() => {
		const list = listRef.value;
		const active = list?.querySelector<HTMLElement>('.sp-item.is-active');
		if (list && active) list.scrollTop = active.offsetTop - list.clientHeight / 2 + active.offsetHeight / 2;
	});
}

function onSelect(scene: VwSceneOutput) {
	open.value = false;
	if (!scene.id || scene.id === props.modelValue) return;
	emit('update:modelValue', scene.id);
	emit('select', scene);
}
</script>

<style scoped lang="scss">
/* ── Thẻ chọn kịch bản ── */
.scene-picker {
	display: flex;
	align-items: center;
	gap: 10px;
	width: 280px;
	padding: 4px 10px 4px 4px;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 10px;
	background: var(--el-bg-color);
	color: var(--el-text-color-primary);
	text-align: left;
	cursor: pointer;
	transition: border-color 0.15s, box-shadow 0.15s, background-color 0.15s;

	&.is-block {
		flex: 1;
		width: 100%;
		min-width: 0;
	}

	&:hover:not(.is-disabled),
	&.is-open {
		border-color: var(--el-color-primary-light-5);
		box-shadow: 0 0 0 3px var(--el-color-primary-light-9);
	}

	&.is-disabled {
		cursor: not-allowed;
		opacity: 0.6;
	}
}

.scene-picker__icon {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 34px;
	height: 34px;
	border-radius: 9px;
	font-size: 18px;
	flex-shrink: 0;
	color: var(--el-color-primary);
	background: var(--el-color-primary-light-9);

	&.is-live {
		color: #fff;
		background: linear-gradient(135deg, var(--el-color-success) 0%, var(--el-color-success-dark-2, #529b2e) 100%);
		box-shadow: 0 2px 8px rgba(var(--el-color-success-rgb), 0.35);
	}
}

.scene-picker__text {
	flex: 1;
	min-width: 0;
	display: flex;
	flex-direction: column;
	gap: 1px;
}

.scene-picker__label {
	font-size: 11px;
	line-height: 14px;
	color: var(--el-text-color-secondary);
}

.scene-picker__name {
	font-size: 14px;
	font-weight: 600;
	line-height: 1.3;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;

	&.is-placeholder {
		font-weight: 500;
		color: var(--el-text-color-placeholder);
	}
}

.scene-picker__arrow {
	color: var(--el-text-color-secondary);
	font-size: 14px;
	flex-shrink: 0;
	transition: transform 0.2s;

	.is-open & {
		transform: rotate(180deg);
	}
}

/* ── Panel danh sách kịch bản (trong popover) ── */
.sp-panel {
	display: flex;
	flex-direction: column;
	max-height: min(560px, 75vh);
	background: var(--el-bg-color-overlay);
	color: var(--el-text-color-primary);
}

.sp-panel__head {
	display: flex;
	flex-direction: column;
	gap: 12px;
	padding: 16px 16px 12px;
	border-bottom: 1px solid var(--el-border-color-lighter);
	background: linear-gradient(180deg, var(--el-fill-color-lighter) 0%, var(--el-bg-color-overlay) 100%);
}

.sp-panel__title-row {
	display: flex;
	align-items: center;
	gap: 8px;
}

.sp-panel__title-icon {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 36px;
	height: 36px;
	border-radius: 10px;
	font-size: 19px;
	flex-shrink: 0;
	color: var(--el-color-primary);
	background: var(--el-color-primary-light-9);
}

.sp-panel__title {
	flex: 1;
	min-width: 0;
	font-size: 15px;
	font-weight: 600;
}

.sp-panel__summary {
	padding: 3px 10px;
	border-radius: 999px;
	background: var(--el-fill-color-light);
	color: var(--el-text-color-regular);
	font-size: 13px;
	font-weight: 600;
	white-space: nowrap;
}

.sp-panel__search {
	:deep(.el-input__wrapper) {
		min-height: 36px;
		border-radius: 8px;
	}
}

.sp-panel__body {
	flex: 1;
	min-height: 0;
	overflow-y: auto;
	padding: 10px 12px 12px;
	display: flex;
	flex-direction: column;
	gap: 6px;

	&::-webkit-scrollbar {
		width: 6px;
	}
	&::-webkit-scrollbar-thumb {
		background: rgba(148, 163, 184, 0.45);
		border-radius: 999px;
	}
}

.sp-item {
	display: flex;
	align-items: center;
	gap: 12px;
	width: 100%;
	min-height: 60px;
	padding: 8px 12px 8px 8px;
	border: 1px solid transparent;
	border-radius: 12px;
	background: transparent;
	color: var(--el-text-color-primary);
	text-align: left;
	cursor: pointer;
	flex-shrink: 0;
	transition: all 0.15s ease;

	&:hover {
		background: var(--el-fill-color-light);
	}

	&.is-active {
		border-color: var(--el-color-primary-light-5);
		background: var(--el-color-primary-light-9);
	}
}

.sp-item__icon {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 42px;
	height: 42px;
	border-radius: 12px;
	font-size: 20px;
	flex-shrink: 0;
	color: var(--el-color-primary);
	background: var(--el-color-primary-light-9);

	&.is-live {
		color: var(--el-color-success);
		background: var(--el-color-success-light-9);
	}

	.sp-item.is-active & {
		color: #fff;
		background: var(--el-color-primary);
	}
}

.sp-item__text {
	flex: 1;
	min-width: 0;
	display: flex;
	flex-direction: column;
	gap: 3px;
}

.sp-item__name {
	font-size: 14px;
	font-weight: 600;
	line-height: 1.3;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;

	.sp-item.is-active & {
		font-weight: 700;
	}
}

.sp-item__meta {
	font-size: 12px;
	color: var(--el-text-color-secondary);
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.sp-item__live {
	display: inline-flex;
	align-items: center;
	gap: 5px;
	padding: 2px 9px;
	border-radius: 999px;
	background: var(--el-color-success);
	color: #fff;
	font-size: 12px;
	font-weight: 600;
	white-space: nowrap;
	flex-shrink: 0;

	i {
		width: 6px;
		height: 6px;
		border-radius: 50%;
		background: #fff;
		animation: sp-pulse 1.6s ease-in-out infinite;
	}
}

.sp-item__tag {
	display: inline-flex;
	align-items: center;
	gap: 4px;
	padding: 2px 10px;
	border-radius: 999px;
	border: 1px solid var(--el-color-warning-light-7);
	background: var(--el-color-warning-light-9);
	color: var(--el-color-warning-dark-2);
	font-size: 12px;
	font-weight: 600;
	white-space: nowrap;
	flex-shrink: 0;

	/* Bố cục cũ: xám, không gây chú ý như "Mặc định" */
	&.is-muted {
		border-color: var(--el-border-color);
		background: var(--el-fill-color-light);
		color: var(--el-text-color-secondary);
	}
}

.sp-item__check {
	display: inline-flex;
	width: 22px;
	justify-content: center;
	font-size: 22px;
	color: var(--el-color-primary);
	flex-shrink: 0;
}

.sp-empty {
	display: flex;
	flex-direction: column;
	align-items: center;
	gap: 6px;
	padding: 24px 0;
	font-size: 14px;
	color: var(--el-text-color-secondary);
}

.sp-empty__icon {
	font-size: 22px;
}

@keyframes sp-pulse {
	0%,
	100% {
		opacity: 1;
	}
	50% {
		opacity: 0.35;
	}
}
</style>

<style lang="scss">
/* Bỏ padding mặc định của popover để panel tràn viền (giống popover chọn bản đồ) */
.el-popover.el-popper.vw-scene-popover {
	padding: 0;
	border-radius: 14px;
	border: 1px solid var(--el-border-color-lighter);
	box-shadow: 0 12px 32px rgba(15, 23, 42, 0.16);
	overflow: hidden;
}
</style>
