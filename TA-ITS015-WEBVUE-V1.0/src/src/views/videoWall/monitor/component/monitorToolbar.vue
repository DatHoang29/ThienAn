<template>
	<div class="monitor-toolbar">
		<!-- Tiêu đề trang (như các trang Video Wall khác) + toàn màn hình -->
		<div class="monitor-toolbar-header">
			<span class="monitor-toolbar-title">
				<el-icon :size="18"><ele-Monitor /></el-icon>
				<span>{{ $t('lz.label.videoWall.monitorTitle') }}</span>
			</span>
			<el-button class="video-wall-btn" round @click="emit('fullscreen')">
				<el-icon :size="16">
					<ele-FullScreen />
				</el-icon>
				<span>{{ $t('lz.button.videoWall.fullscreen') }}</span>
			</el-button>
		</div>
		<!-- Hàng công cụ: chọn kịch bản + Đặt lại / Áp dụng bên trái (cùng chiều cao 44px), thông tin kịch bản đang xem bên phải -->
		<div class="monitor-toolbar-row">
			<div class="monitor-toolbar-left">
				<!-- Chọn kịch bản: components/videoWall/scenePicker.vue (dùng chung với trang Quản lý kịch bản).
				     Toàn màn hình: popper gắn vào body nằm ngoài vùng fullscreen nên không hiện ⇒ khoá. -->
				<ScenePicker :scenes="scenes" :model-value="modelValue" :serving-scene-id="servingSceneId" :disabled="applying || isFullscreen"
					:label="$t('lz.label.videoWall.selectedScene')" @select="onSelect" />
				<el-button class="monitor-toolbar-btn" @click="emit('reset')" :disabled="!hasChanges || applying">
					<el-icon><ele-RefreshLeft /></el-icon> {{ $t('lz.button.base.reset') }}
				</el-button>
				<!-- Áp dụng là thao tác duy nhất gửi lệnh xuống tường (kích hoạt kịch bản cho cả
				     phòng điều hành) ⇒ phải có quyền. Reset chỉ thao tác cục bộ nên không gắn. -->
				<el-button class="monitor-toolbar-btn" type="primary" v-auth="'videoWallMonitor:apply'" :disabled="!canApply" :loading="applying" @click="emit('apply')">
					<el-icon><ele-Check /></el-icon> {{ $t('lz.button.videoWall.apply') }}
				</el-button>
			</div>

			<div class="monitor-toolbar-center" />

			<div class="monitor-toolbar-right">
				<!-- Thông tin kịch bản đang xem: dải ngang, mỗi mục = icon · nhãn · giá trị (tên · thuộc tính · trạng thái) -->
				<div v-if="activeScene" class="monitor-toolbar-meta">
					<div v-if="activeScene.name" class="meta-item">
						<span class="meta-item__icon"><el-icon><ele-Film /></el-icon></span>
						<span class="meta-item__label">{{ $t('lz.label.videoWall.sceneNameDisplay') }}</span>
						<span class="meta-item__value" :title="activeScene.name">{{ activeScene.name }}</span>
					</div>
					<div v-if="activeScene.isDefault === DefaultEnum.NUMBER_1" class="meta-item">
						<span class="meta-item__icon is-warning"><el-icon><ele-StarFilled /></el-icon></span>
						<span class="meta-item__label">{{ $t('lz.label.videoWall.sceneAttribute') }}</span>
						<el-tag size="small" type="warning" effect="light" round>{{ $t('lz.label.base.isDefault') }}</el-tag>
					</div>
					<div class="meta-item">
						<!-- Icon phát sóng xanh khi tường đang phát đúng kịch bản này; xem trước ⇒ icon mắt xám -->
						<span class="meta-item__icon" :class="isServing(activeScene) ? 'is-success' : 'is-info'">
							<el-icon><ele-VideoPlay v-if="isServing(activeScene)" /><ele-View v-else /></el-icon>
						</span>
						<span class="meta-item__label">{{ $t('lz.label.base.status') }}</span>
						<el-tag v-if="isServing(activeScene)" size="small" type="success" effect="light" round>
							<span class="monitor-toolbar-live" />{{ $t('lz.label.videoWall.sceneServing') }}
						</el-tag>
						<el-tag v-else size="small" type="info" effect="light" round>{{ $t('lz.label.videoWall.scenePreview') }}</el-tag>
					</div>
				</div>
			</div>
		</div>
	</div>
</template>

<script lang="ts" setup name="videoWallMonitorToolbar">
import { computed } from 'vue';
import { DefaultEnum } from '/@/api-services/videoWall';
import type { VwSceneOutput } from '/@/api-services/videoWall';
import ScenePicker from '/@/components/videoWall/scenePicker.vue';

interface Props {
	scenes: VwSceneOutput[];
	modelValue: string;
	/** Kịch bản BE đang phát trên tường ⇒ gắn nhãn "Đang phát" / "Xem trước". */
	servingSceneId?: string;
	hasChanges: boolean;
	/** Bật nút "Áp dụng": có kịch bản đang chọn — kể cả kịch bản đang phát (áp dụng lại sau khi sửa). */
	canApply?: boolean;
	/** Đang gọi API kích hoạt kịch bản — chặn bấm lặp và khoá chọn kịch bản. */
	applying?: boolean;
	/** Đang toàn màn hình — popper gắn vào body nằm ngoài vùng fullscreen nên không hiện ⇒ khoá chọn kịch bản. */
	isFullscreen?: boolean;
}
const props = withDefaults(defineProps<Props>(), {
	servingSceneId: '',
	canApply: false,
	applying: false,
	isFullscreen: false,
});
const emit = defineEmits<{
	(e: 'changeScene', sceneId: string): void;
	(e: 'apply'): void;
	(e: 'reset'): void;
	(e: 'screenCmd', cmd: string): void;
	(e: 'fullscreen'): void;
	(e: 'update:modelValue', val: string): void;
}>();

const activeScene = computed(() => props.scenes.find((s) => s.id === props.modelValue));
const isServing = (scene?: VwSceneOutput) => !!scene?.id && scene.id === props.servingSceneId;

function onSelect(scene: VwSceneOutput) {
	emit('update:modelValue', scene.id!);
	emit('changeScene', scene.id!);
}
</script>

<style scoped lang="scss">
/* Khung trắng bọc tiêu đề + thanh công cụ như các trang khác */
.monitor-toolbar {
	display: flex;
	flex-direction: column;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 10px;
	background: var(--el-bg-color);
	box-shadow: 0 1px 4px rgba(15, 23, 42, 0.04);
	flex-shrink: 0;
}

/* Dòng tiêu đề — cùng cỡ chữ với .scene-editor-title của trang Quản lý kịch bản hiển thị */
.monitor-toolbar-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 16px;
	padding: 8px 16px;
	border-bottom: 1px solid var(--el-border-color-light);
}

.monitor-toolbar-title {
	display: inline-flex;
	align-items: center;
	gap: 8px;
	font-size: 18px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	white-space: nowrap;
}

.monitor-toolbar-row {
	display: flex;
	align-items: center;
	gap: 12px;
	padding: 12px 16px;
}

.monitor-toolbar-left,
.monitor-toolbar-right {
	display: flex;
	align-items: center;
	gap: 8px;
	min-width: 0;
	flex-shrink: 0;
}

/* Thiếu chỗ ⇒ dải thông tin bên phải co (tên kịch bản cắt "…"), cụm chọn kịch bản + nút giữ nguyên */
.monitor-toolbar-right {
	flex-shrink: 1;
}

/* Nút Đặt lại / Áp dụng cao bằng ô chọn kịch bản và dải thông tin (44px) */
.monitor-toolbar-btn {
	height: 44px;
	padding: 0 18px;
	border-radius: 10px;
	font-size: 14px;
	font-weight: 600;
	.el-icon {
		margin-right: 4px;
		font-size: 16px;
	}
}

.monitor-toolbar-center {
	flex: 1;
	display: flex;
	align-items: center;
	justify-content: center;
	flex-wrap: wrap;
	gap: 8px;
	min-width: 0;
}

/* Element Plus tự thêm margin-left:12px giữa 2 el-button liền kề ⇒ để 'gap' quyết định khoảng cách */
.monitor-toolbar :deep(.el-button + .el-button) {
	margin-left: 0;
}

/* ── Dải thông tin kịch bản: nền xám nhạt, các mục nằm ngang ngăn bằng vạch dọc ── */
.monitor-toolbar-meta {
	display: flex;
	align-items: center;
	min-width: 0;
	height: 44px;
	padding: 0 6px;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 10px;
	background: var(--el-fill-color-lighter);
	overflow: hidden;
}

.meta-item {
	display: inline-flex;
	align-items: center;
	gap: 8px;
	min-width: 0;
	padding: 0 12px;

	& + & {
		border-left: 1px solid var(--el-border-color);
	}

	/* Mục tên kịch bản được co lại trước khi thiếu chỗ, các mục tag giữ nguyên */
	&:first-child {
		flex-shrink: 1;
	}

	&:not(:first-child) {
		flex-shrink: 0;
	}
}

.meta-item__icon {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 24px;
	height: 24px;
	border-radius: 7px;
	font-size: 14px;
	flex-shrink: 0;
	color: var(--el-color-primary);
	background: var(--el-color-primary-light-9);

	&.is-warning {
		color: var(--el-color-warning);
		background: var(--el-color-warning-light-9);
	}

	&.is-success {
		color: var(--el-color-success);
		background: var(--el-color-success-light-9);
	}

	&.is-info {
		color: var(--el-text-color-secondary);
		background: var(--el-fill-color);
	}
}

.meta-item__label {
	font-size: 12px;
	color: var(--el-text-color-secondary);
	white-space: nowrap;
	font-weight: 600;
}

.meta-item__value {
	max-width: 220px;
	font-size: 13px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

/* Chấm "đang phát" */
.monitor-toolbar-live {
	display: inline-block;
	width: 6px;
	height: 6px;
	margin-right: 4px;
	border-radius: 50%;
	background: currentColor;
	vertical-align: middle;
	animation: sp-pulse 1.6s ease-in-out infinite;
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

