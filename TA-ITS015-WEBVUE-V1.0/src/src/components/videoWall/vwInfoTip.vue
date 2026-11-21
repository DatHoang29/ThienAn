<template>
	<!--
		Thẻ thông tin trong tooltip của Video Wall (ô màn hình, cửa sổ kịch bản…):
		  tiêu đề = chấm trạng thái / ô màu + tên chính + tên phụ; thân = các dòng icon · nhãn · giá trị (căn phải).
		Dùng trong `<template #content>` của el-tooltip, kèm `popper-class="vw-info-tip"` để có khung tối bo góc.
	-->
	<div class="vw-tip">
		<div class="vw-tip-head">
			<i v-if="swatch" class="vw-tip-swatch vw-tip-swatch--lg" :style="{ backgroundColor: swatch }" />
			<i v-else-if="dot" class="vw-tip-dot" :class="`is-${dot}`" />
			<span class="vw-tip-title">{{ title || '—' }}</span>
			<span v-if="subtitle && subtitle !== title" class="vw-tip-sub">{{ subtitle }}</span>
		</div>
		<div v-if="visibleRows.length" class="vw-tip-body">
			<div v-for="(row, i) in visibleRows" :key="i" class="vw-tip-row">
				<el-icon v-if="row.icon"><component :is="row.icon" /></el-icon>
				<span class="vw-tip-label">{{ row.label }}</span>
				<span class="vw-tip-value" :class="row.tone ? `is-${row.tone}` : undefined">
					<i v-if="row.swatch" class="vw-tip-swatch" :style="{ backgroundColor: row.swatch }" />
					{{ row.value || '—' }}
				</span>
			</div>
		</div>
	</div>
</template>

<script lang="ts">
export interface VwTipRow {
	/** Tên icon đăng ký toàn cục, vd 'ele-Location'. */
	icon?: string;
	label: string;
	value?: string | number | null;
	/** Ô màu nhỏ trước giá trị (vd màu bộ điều khiển). */
	swatch?: string;
	/** Màu chữ giá trị: success = xanh lá, warning = cam, muted = xám. */
	tone?: 'success' | 'warning' | 'muted';
	/** false ⇒ ẩn dòng (tiện viết điều kiện ngay trong mảng). */
	show?: boolean;
}
</script>

<script lang="ts" setup name="vwInfoTip">
import { computed } from 'vue';

interface Props {
	title?: string | null;
	subtitle?: string | null;
	/** Chấm trạng thái trước tiêu đề: on = xanh có quầng, off = xám, warn = cam. */
	dot?: 'on' | 'off' | 'warn';
	/** Ô màu trước tiêu đề (thay cho chấm) — vd màu loại nguồn của cửa sổ. */
	swatch?: string;
	rows?: VwTipRow[];
}
const props = withDefaults(defineProps<Props>(), { rows: () => [] });

const visibleRows = computed(() => props.rows.filter((r) => r.show !== false));
</script>

<style lang="scss">
/*
 * Popper gắn vào body nên style không scoped, khoá theo popper-class "vw-info-tip".
 * Sao theo tooltip thiết bị trên bản đồ (.item-info-popover — views/bigScreen/trafficInfo/index.vue):
 * kính tối chuyển sắc, viền + quầng sáng theo màu nhấn (--vw-tip-accent = primary), dải màu nhấn ở tiêu đề.
 * Kèm .is-dark để mạnh hơn .el-popper.is-dark (nền xám mặc định của Element).
 */
.el-popper.is-dark.vw-info-tip,
.el-popper.vw-info-tip {
	--vw-tip-accent: var(--el-color-primary);
	padding: 0;
	border-radius: 12px;
	border: 1px solid color-mix(in srgb, var(--vw-tip-accent) 65%, transparent);
	background: linear-gradient(180deg, rgba(20, 28, 44, 0.98), rgba(10, 16, 28, 0.98));
	box-shadow: 0 18px 44px -12px rgba(0, 0, 0, 0.72), 0 0 24px -4px color-mix(in srgb, var(--vw-tip-accent) 65%, transparent),
		inset 0 1px 0 rgba(255, 255, 255, 0.1);
	overflow: hidden;

	.el-popper__arrow::before {
		background: rgba(24, 30, 42, 0.98);
		border-color: color-mix(in srgb, var(--vw-tip-accent) 40%, transparent);
	}
}

.vw-tip {
	min-width: 220px;
	max-width: 320px;
	color: rgba(255, 255, 255, 0.92);
	font-size: 12px;
	line-height: 1.4;

	/* Tiêu đề: dải màu nhấn nhạt dần */
	.vw-tip-head {
		display: flex;
		align-items: center;
		gap: 8px;
		padding: 10px 14px;
		background: linear-gradient(
			135deg,
			color-mix(in srgb, var(--vw-tip-accent) 28%, transparent),
			color-mix(in srgb, var(--vw-tip-accent) 6%, transparent)
		);
		border-bottom: 1px solid rgba(255, 255, 255, 0.08);
	}

	/* Chấm trạng thái phát sáng: on = xanh lá, off = xám, warn = cam */
	.vw-tip-dot {
		flex-shrink: 0;
		width: 8px;
		height: 8px;
		border-radius: 50%;
		background: var(--el-color-info);
		box-shadow: 0 0 8px color-mix(in srgb, var(--el-color-info) 70%, transparent);

		&.is-on {
			background: var(--el-color-success);
			box-shadow: 0 0 8px color-mix(in srgb, var(--el-color-success) 70%, transparent);
		}

		&.is-warn {
			background: var(--el-color-warning);
			box-shadow: 0 0 8px color-mix(in srgb, var(--el-color-warning) 70%, transparent);
		}
	}

	/* Ô màu: trong dòng là ô vuông nhỏ; ở tiêu đề là chấm tròn phát sáng như chấm trạng thái */
	.vw-tip-swatch {
		flex-shrink: 0;
		width: 8px;
		height: 8px;
		border-radius: 2px;

		&--lg {
			border-radius: 50%;
			box-shadow: 0 0 8px rgba(255, 255, 255, 0.25);
		}
	}

	.vw-tip-title {
		font-size: 13px;
		font-weight: 700;
		color: #fff;
		text-transform: uppercase;
		letter-spacing: 0.3px;
		white-space: nowrap;
	}

	.vw-tip-sub {
		min-width: 0;
		color: rgba(255, 255, 255, 0.55);
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.vw-tip-body {
		display: flex;
		flex-direction: column;
		gap: 7px;
		padding: 8px 14px 12px;
	}

	.vw-tip-row {
		display: flex;
		align-items: center;
		gap: 8px;

		.el-icon {
			flex-shrink: 0;
			font-size: 14px;
			color: color-mix(in srgb, var(--vw-tip-accent) 45%, #fff);
		}
	}

	.vw-tip-label {
		flex-shrink: 0;
		min-width: 68px;
		color: rgba(255, 255, 255, 0.55);
		white-space: nowrap;
	}

	.vw-tip-value {
		display: inline-flex;
		align-items: center;
		justify-content: flex-end;
		gap: 6px;
		flex: 1;
		min-width: 0;
		padding-left: 12px;
		font-weight: 600;
		white-space: nowrap;

		&.is-success {
			color: color-mix(in srgb, var(--el-color-success) 55%, #fff);
		}

		&.is-warning {
			color: color-mix(in srgb, var(--el-color-warning) 55%, #fff);
		}

		&.is-muted {
			color: color-mix(in srgb, var(--el-color-info) 45%, #fff);
		}
	}
}
</style>
