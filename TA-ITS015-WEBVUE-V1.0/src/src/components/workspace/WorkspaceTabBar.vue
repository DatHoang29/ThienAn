<template>
	<nav ref="navRef" class="workspace-tabbar tab-page-navigation" :aria-label="ariaLabel">
		<el-tabs :model-value="modelValue" class="management-tabs workspace-tabs" :before-leave="beforeLeave" @update:model-value="onUpdate" @tab-click="onTabClick">
			<el-tab-pane v-for="tab in tabs" :key="tab.key" :name="tab.key" :disabled="disabled">
				<template #label>
					<span class="management-tab-label workspace-tab-label">
						<SvgIcon v-if="tab.icon" :name="tab.icon" :size="16" aria-hidden="true" />
						<span>{{ tab.label }}</span>
					</span>
				</template>
			</el-tab-pane>
		</el-tabs>
		<!-- Bấm tab: 2 luồng sáng chạy từ đỉnh tab theo viền xuống cạnh dưới header; đổi key để chạy lại mỗi lần bấm. -->
		<svg v-if="trace" :key="tracePulse" class="tab-trace" :style="{ left: `${trace.x}px`, top: `${trace.y}px` }" :width="trace.w" :height="trace.h" aria-hidden="true">
			<path v-for="streak in trace.streaks" :key="streak.d" class="tab-trace-streak" :d="streak.d" :style="{ '--len': `${streak.len}px` }" />
		</svg>
	</nav>
</template>

<script setup lang="ts" name="workspaceTabBar">
// Author: KienLT
// Description: Thanh tab của trang có tab (tách từ views/tabPage để trang tự điều phối tab như Zone, Sự kiện - Sự cố
// dùng CHUNG một kiểu dáng với các trang *Mngt). Đặt trong header `.workspace-header`; vỏ trang phải có class
// `workspace-ui tab-page` để nhận kiểu dáng header + tab bên dưới.
// Created date: 13/11/2026
import { nextTick, onActivated, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import type { TabsPaneContext } from 'element-plus';
import SvgIcon from '/@/components/svgIcon/index.vue';

interface WorkspaceTab {
	key: string;
	label: string;
	icon?: string;
}
interface Props {
	modelValue: string;
	tabs: WorkspaceTab[];
	ariaLabel?: string;
	/** Chặn rời tab (vd còn thay đổi chưa lưu): trả false / Promise bị từ chối thì giữ nguyên tab. */
	beforeLeave?: (next: string | number, previous: string | number) => boolean | Promise<unknown>;
	/** Khoá toàn bộ tab (vd tab hiện tại đang lưu/đang tải). */
	disabled?: boolean;
}
const props = defineProps<Props>();
const emit = defineEmits<{ 'update:modelValue': [value: string] }>();
const onUpdate = (value: string | number) => emit('update:modelValue', String(value));

// Luồng sáng chạy theo viền tab active: tọa độ theo header (để chạy hết cạnh dưới), đặt SVG tương đối với <nav>.
const navRef = ref<HTMLElement>();
const trace = ref<{ x: number; y: number; w: number; h: number; streaks: { d: string; len: number }[] } | null>(null);
const tracePulse = ref(0);
function measureTrace() {
	const nav = navRef.value;
	const header = nav?.closest('.workspace-header');
	const tab = nav?.querySelector<HTMLElement>('.el-tabs__item.is-active');
	if (!nav || !header || !tab) {
		trace.value = null;
		return;
	}
	const n = nav.getBoundingClientRect();
	const h = header.getBoundingClientRect();
	const t = tab.getBoundingClientRect();
	if (!h.width || !t.width) {
		trace.value = null;
		return;
	}
	const r = 8;
	const top = t.top - h.top + 0.5;
	const left = t.left - h.left + 0.5;
	const right = t.right - h.left - 0.5;
	const mid = (left + right) / 2;
	const base = h.height - 0.5;
	const corner = (Math.PI * r) / 2;
	const side = Math.max(base - top - r, 0);
	trace.value = {
		x: h.left - n.left,
		y: h.top - n.top,
		w: h.width,
		h: h.height,
		streaks: [
			{ d: `M${mid} ${top}H${left + r}A${r} ${r} 0 0 0 ${left} ${top + r}V${base}H0`, len: mid - left - r + corner + side + left },
			{ d: `M${mid} ${top}H${right - r}A${r} ${r} 0 0 1 ${right} ${top + r}V${base}H${h.width}`, len: right - r - mid + corner + side + h.width - right },
		],
	};
}
async function playTrace() {
	await nextTick();
	measureTrace();
	tracePulse.value++;
}
watch(() => props.modelValue, playTrace);
// tab-click phát ra trước khi đổi tab, nên chỉ cần tự chạy lại khi bấm vào chính tab đang chọn.
function onTabClick(pane: TabsPaneContext) {
	if (pane.paneName === props.modelValue) playTrace();
}
let resizeObserver: ResizeObserver | undefined;
onMounted(() => {
	playTrace();
	const header = navRef.value?.closest('.workspace-header');
	const nav = navRef.value?.querySelector('.el-tabs__nav');
	if (typeof ResizeObserver === 'undefined' || !header) return;
	resizeObserver = new ResizeObserver(() => measureTrace());
	resizeObserver.observe(header);
	if (nav) resizeObserver.observe(nav);
});
onActivated(measureTrace);
onBeforeUnmount(() => resizeObserver?.disconnect());
</script>

<style lang="scss">
/* KHÔNG scoped: header và vỏ trang nằm ngoài component này. Mọi rule gắn dưới `.workspace-ui.tab-page`. */
.workspace-ui.tab-page {
	gap: 4px;
	padding: 4px 6px;

	.workspace-header {
		min-height: 42px;
		padding: 0 10px;
		gap: 10px;
		flex-wrap: nowrap;
		flex-direction: row;
		border: 1px solid var(--workspace-border);
		border-bottom-color: var(--workspace-primary);
		border-radius: 10px 10px 0 0;
		background: var(--workspace-surface);
	}

	.workspace-heading {
		flex: 0 1 auto;
		min-width: 0;
		max-width: 45%;
		gap: 7px;
		padding: 6px 0;
	}

	.workspace-heading > .el-icon {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		width: 26px;
		height: 26px;
		border-radius: 6px;
		background: var(--workspace-primary-soft);
		color: var(--workspace-primary);
	}

	.workspace-heading h1 {
		font-size: 16px;
		line-height: 1.35;
		letter-spacing: -0.2px;
	}

	.tab-page-navigation {
		position: relative;
		flex: 1 1 0;
		align-self: stretch;
		align-items: flex-end;
		min-width: 0;
		margin-left: 0;
		padding: 2px 0 0 8px;
		border: 0;
		border-radius: 0;
		background: transparent;

		&::before {
			position: absolute;
			top: 10px;
			bottom: 10px;
			left: 0;
			width: 1px;
			background: var(--workspace-border);
			content: '';
		}
	}

	/* Tab gắn vào cạnh dưới header; tab active che đường viền để nối liền với nội dung. */
	.workspace-tabs {
		width: max-content;
		min-width: 0;
		max-width: 100%;
		margin-bottom: -1px;
	}

	.workspace-tabs > .el-tabs__header {
		margin: 0;
	}

	.workspace-tabs .el-tabs__nav-wrap {
		margin-bottom: 0;
	}

	.workspace-tabs .el-tabs__nav {
		display: flex;
		align-items: flex-end;
		gap: 4px;
	}

	.workspace-tabs .el-tabs__active-bar {
		display: none;
	}

	/* Chừa chỗ phía trên để bóng của tab active không bị overflow cắt. */
	.workspace-tabs .el-tabs__nav-scroll {
		padding: 4px 4px 0;
	}

	.workspace-tabs.el-tabs--top > .el-tabs__header .el-tabs__item {
		position: relative;
		/* Mọi tab cùng chiều cao: đổi trạng thái chỉ đổi màu, không nhảy hình. */
		height: 36px;
		padding: 0 16px;
		border: 1px solid transparent;
		border-bottom: 0;
		border-radius: 8px 8px 0 0;
		background-color: transparent;
		background-image: none;
		color: var(--workspace-muted);
		font-size: 13px;
		font-weight: 600;
		line-height: 36px;
		box-shadow: none;
		transition:
			color 180ms ease,
			background-color 180ms ease,
			border-color 180ms ease,
			box-shadow 180ms ease;

		/* Thanh chỉ báo thẳng ở cạnh trên, không cong theo góc bo. */
		&::before {
			position: absolute;
			top: -1px;
			left: 12px;
			right: 12px;
			height: 3px;
			border-radius: 0 0 3px 3px;
			background: var(--workspace-primary);
			opacity: 0;
			transform: scaleX(0);
			/* Đường cong vượt nhẹ để thanh bật giãn lại sau mỗi lần nhấn. */
			transition:
				transform 340ms cubic-bezier(0.34, 1.56, 0.64, 1),
				opacity 180ms ease,
				box-shadow 340ms ease;
			content: '';
		}
	}

	/* Ripple khi bấm: lúc nhấn reset tức thì, lúc thả lan rộng rồi mờ dần, nên bấm liên tục vẫn chạy lại. */
	.workspace-tabs .el-tabs__item::after {
		position: absolute;
		inset: 0;
		border-radius: inherit;
		background: radial-gradient(circle, var(--workspace-primary) 0%, transparent 70%) center / 260% 260% no-repeat;
		opacity: 0;
		pointer-events: none;
		transition:
			background-size 500ms cubic-bezier(0.2, 0.8, 0.2, 1),
			opacity 500ms ease;
		content: '';
	}

	.workspace-tabs .el-tabs__item:active::after {
		background-size: 0% 0%;
		opacity: 0.22;
		transition: none;
	}

	/* Chữ lún nhẹ khi nhấn. */
	.workspace-tab-label {
		position: relative;
		z-index: 1;
		transition: transform 120ms ease;
	}

	.workspace-tabs .el-tabs__item:active .workspace-tab-label {
		transform: scale(0.95);
	}

	/* Hover: "tab mờ" cùng hình dáng tab active (viền + nền nhạt) để không thành khối rời. */
	.workspace-tabs.el-tabs--top > .el-tabs__header .el-tabs__item:not(.is-active):hover {
		border-color: color-mix(in srgb, var(--workspace-primary) 28%, transparent);
		background-color: color-mix(in srgb, var(--workspace-primary-soft) 70%, transparent);
		color: var(--workspace-primary);

		&::before {
			opacity: 0.45;
			transform: scaleX(0.4);
		}
	}

	.workspace-tabs.el-tabs--top > .el-tabs__header .el-tabs__item.is-active {
		border-color: var(--workspace-primary);
		background-color: var(--workspace-surface);
		/* Primary nhạt ở trên, về nền trắng ở chân tab để vẫn nối liền với nội dung. */
		background-image: linear-gradient(180deg, color-mix(in srgb, var(--workspace-primary) 24%, var(--workspace-surface)) 0%, var(--workspace-surface) 90%);
		color: var(--workspace-primary);
		box-shadow: 0 -2px 6px rgba(30, 52, 90, 0.08);

		&::before {
			opacity: 1;
			transform: scaleX(1);
		}
	}

	/* Nhấn: thanh trên co lại và sáng lên; thả ra thì bật giãn về đủ độ dài. */
	.workspace-tabs.el-tabs--top > .el-tabs__header .el-tabs__item.is-top:active::before {
		opacity: 1;
		transform: scaleX(0.55);
		box-shadow: 0 0 8px var(--workspace-primary);
		transition:
			transform 90ms ease-out,
			opacity 90ms ease-out,
			box-shadow 90ms ease-out;
	}

	.workspace-tabs .el-tabs__item:focus-visible {
		outline: 2px solid var(--workspace-primary);
		outline-offset: -3px;
	}

	.workspace-tab-label {
		gap: 6px;
	}

	.workspace-tab-label .svg-icon {
		opacity: 0.7;
		transition: opacity 160ms ease;
	}

	.el-tabs__item:hover .workspace-tab-label .svg-icon,
	.el-tabs__item.is-active .workspace-tab-label .svg-icon {
		opacity: 1;
	}
}

.tab-trace {
	position: absolute;
	z-index: 3;
	overflow: visible;
	pointer-events: none;
}

/* Luồng sáng: một đoạn nét ngắn trượt dọc đường viền bằng stroke-dashoffset. */
.tab-trace-streak {
	--streak: 56px;
	fill: none;
	stroke: var(--workspace-primary);
	stroke-width: 2;
	stroke-linecap: round;
	stroke-dasharray: var(--streak) var(--len);
	stroke-dashoffset: var(--streak);
	opacity: 0;
	filter: drop-shadow(0 0 3px var(--workspace-primary));
	animation: tab-trace-run 1000ms cubic-bezier(0.45, 0, 0.25, 1) both;
}

@keyframes tab-trace-run {
	0% {
		stroke-dashoffset: var(--streak);
		opacity: 0;
	}
	12% {
		opacity: 0.7;
	}
	85% {
		opacity: 0.7;
	}
	100% {
		stroke-dashoffset: calc(var(--len) * -1);
		opacity: 0;
	}
}

@media (max-width: 680px) {
	.workspace-ui.tab-page {
		.workspace-header {
			align-items: stretch;
			flex-direction: column;
			gap: 4px;
			padding: 6px 8px 0;
		}

		.workspace-heading {
			max-width: none;
			padding: 0;
		}

		.workspace-heading h1 {
			font-size: 16px;
		}

		.tab-page-navigation {
			width: 100%;
			max-width: 100%;
			margin-left: 0;
			padding: 2px 0 0;

			&::before {
				display: none;
			}
		}

		.workspace-tabs {
			width: 100%;
		}

		.workspace-tabs .el-tabs__item {
			flex: 1 1 0;
			justify-content: center;
		}
	}
}

@media (pointer: coarse) {
	.workspace-ui.tab-page .workspace-tabs.el-tabs--top > .el-tabs__header .el-tabs__item,
	.workspace-ui.tab-page .workspace-tabs.el-tabs--top > .el-tabs__header .el-tabs__item.is-active {
		height: 42px;
		line-height: 42px;
	}
}

@media (prefers-reduced-motion: reduce) {
	.workspace-ui.tab-page .workspace-tabs .el-tabs__item,
	.workspace-ui.tab-page .workspace-tabs .el-tabs__item::before,
	.workspace-ui.tab-page .workspace-tabs .el-tabs__item::after,
	.workspace-ui.tab-page .workspace-tab-label,
	.workspace-ui.tab-page .workspace-tab-label .svg-icon {
		transition: none;
	}

	.tab-trace {
		display: none;
	}
}
</style>
