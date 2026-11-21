<template>
	<!-- Chọn kịch bản: dropdown đầu cột cấu hình bên trái + thao tác trên kịch bản đang mở -->
	<div class="scene-select">
		<div class="scene-select-row">
			<!-- Cùng ô chọn với trang Giám sát & Điều khiển: components/videoWall/scenePicker.vue -->
			<ScenePicker :scenes="scenes" :model-value="activeSceneId" block :label="$t('lz.label.videoWall.selectedScene')"
				:placeholder="isNew ? $t('lz.label.videoWall.unsavedNewScene') : ''" :is-old="isOldLayout" :is-default="isDefaultScene" @select="(scene) => emit('select', scene)" />
			<!-- Thêm mới cạnh ô chọn: thao tác trên danh sách kịch bản -->
			<el-button type="primary" plain icon="ele-Plus" class="scene-select-add" :title="$t('lz.button.base.add')" v-auth="'videoWallScene:add'" @click="emit('create')" />
		</div>

		<!-- Kịch bản đang mở (dòng dưới ô chọn): mã + tag bên trái · nhóm nút thao tác căn phải (mặc định · sao chép · thông tin · xoá) -->
		<div v-if="activeScene" class="scene-select-meta">
			<span v-if="activeScene.code" class="scene-select-code" :title="activeScene.code">{{ activeScene.code }}</span>
			<el-tag v-if="isDefaultScene(activeScene)" size="small" type="warning" effect="light" round class="scene-select-tag">
				{{ $t('lz.label.base.isDefault') }}
			</el-tag>
			<!-- Kịch bản dựng trên phiên bản bố cục khác: chỉ xem, không áp dụng được -->
			<el-tag v-if="isOldLayout(activeScene)" size="small" type="info" effect="plain" class="scene-select-tag" :title="$t('lz.message.videoWall.sceneOldLayoutReadonly')">
				{{ $t('lz.label.videoWall.oldLayout') }}
			</el-tag>
			<span class="scene-select-actions">
				<!-- Đã là mặc định ⇒ sao vàng đặc, không bấm được -->
				<el-tooltip v-if="canSetDefault" :content="isDefaultScene(activeScene) ? $t('lz.label.base.isDefault') : $t('lz.button.videoWall.setDefault')" placement="top">
					<span v-auth="'videoWallScene:update'">
						<el-button text circle size="small" class="scene-select-btn" :class="{ 'is-default': isDefaultScene(activeScene) }"
							:icon="isDefaultScene(activeScene) ? 'ele-StarFilled' : 'ele-Star'" :disabled="isDefaultScene(activeScene) || isOldLayout(activeScene)"
							@click="emit('set-default', activeScene)" />
					</span>
				</el-tooltip>
				<el-tooltip :content="$t('lz.button.base.copy')" placement="top">
					<el-button text circle size="small" class="scene-select-btn" icon="ele-CopyDocument" v-auth="'videoWallScene:add'" @click="emit('duplicate', activeScene)" />
				</el-tooltip>
				<span class="scene-select-btn"><ModifyRecord :data="activeScene" icon-only :width="420" /></span>
				<el-tooltip :content="$t('lz.button.base.delete')" placement="top">
					<el-button text circle size="small" type="danger" class="scene-select-btn" icon="ele-Delete" v-auth="'videoWallScene:delete'"
						@click="emit('delete', activeScene)" />
				</el-tooltip>
			</span>
		</div>
	</div>
</template>

<script lang="ts" setup name="videoWallSceneList">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { DefaultEnum } from '/@/api-services/videoWall';
import type { VwSceneOutput } from '/@/api-services/videoWall';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import ScenePicker from '/@/components/videoWall/scenePicker.vue';

const { t: $t } = useI18n();

interface Props {
	scenes: VwSceneOutput[];
	/** Kịch bản đang mở ('' khi chưa chọn hoặc đang tạo mới). */
	activeSceneId: string;
	/** Đang soạn kịch bản mới chưa lưu ⇒ ô chọn hiện nhãn "Kịch bản mới (chưa lưu)". */
	isNew?: boolean;
	/** Nhãn bộ điều khiển sở hữu, theo `controllerId` — chỉ để hiển thị. */
	controllerLabels?: Record<string, string>;
	/** Hiện mục "Đặt mặc định" — chỉ SuperAdmin (cài đặt của cả tường). */
	canSetDefault?: boolean;
	/** ID bố cục đang dùng — kịch bản thuộc bố cục khác gắn nhãn "Bố cục cũ". */
	currentTopologyId?: string | null;
}
const props = withDefaults(defineProps<Props>(), {
	isNew: false,
	controllerLabels: () => ({}),
	canSetDefault: false,
	currentTopologyId: null,
});
const isOldLayout = (scene: VwSceneOutput) => !!props.currentTopologyId && scene.wallTopologyId !== props.currentTopologyId;
/** Kịch bản bố cục cũ không áp dụng được ⇒ không coi là mặc định (BE cũng bỏ cờ / chặn đặt mặc định). */
const isDefaultScene = (scene: VwSceneOutput) => scene.isDefault === DefaultEnum.NUMBER_1 && !isOldLayout(scene);
const emit = defineEmits<{
	(e: 'select', scene: VwSceneOutput): void;
	(e: 'duplicate', scene: VwSceneOutput): void;
	(e: 'delete', scene: VwSceneOutput): void;
	(e: 'set-default', scene: VwSceneOutput): void;
	(e: 'create'): void;
}>();

const activeScene = computed(() => (props.activeSceneId ? props.scenes.find((s) => s.id === props.activeSceneId) : undefined));
</script>

<style scoped>
.scene-select {
	display: flex;
	flex-direction: column;
	gap: 6px;
	padding: 12px 14px 4px;
	min-width: 0;
}

.scene-select-row {
	display: flex;
	align-items: center;
	gap: 6px;
}

.scene-select-meta {
	display: flex;
	align-items: center;
	gap: 6px;
	min-width: 0;
}

.scene-select-actions {
	margin-left: auto;
	display: inline-flex;
	align-items: center;
	gap: 2px;
	flex-shrink: 0;
}

/* Nút thao tác: cùng ô 24px, các nút liền nhau không có khoảng thừa của el-button */
.scene-select-btn {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 24px;
	height: 24px;
	margin: 0 !important;
	font-size: 14px;
}

.scene-select-btn :deep(.el-icon) {
	margin: 0;
}

/* Đã là mặc định: sao vàng đặc, vẫn rõ dù nút bị khoá */
.scene-select-btn.is-default.is-disabled {
	color: var(--el-color-warning);
}

.scene-select-tag {
	flex-shrink: 0;
}

.scene-select-code {
	min-width: 0;
	font-size: 13px;
	color: var(--el-text-color-secondary);
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

/* Nút thêm cao bằng thẻ chọn kịch bản */
.scene-select-add {
	flex-shrink: 0;
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 44px;
	height: 44px;
	padding: 0;
	border-radius: 10px;
	font-size: 16px;
}

/* Nút chỉ có icon: bỏ lề / span rỗng mặc định của el-button để dấu cộng nằm đúng tâm */
.scene-select-add :deep(.el-icon) {
	margin: 0;
}
.scene-select-add :deep(> span:empty) {
	display: none;
}
</style>

