<template>
	<!-- Form thông tin kịch bản — phần trên của panel cấu hình bên trái; nút Lưu / Bỏ qua nằm trên header trang -->
	<div class="scene-form">
		<div class="scene-form-header">
			<el-icon class="scene-form-icon"><ele-Document /></el-icon>
			<span class="scene-form-title">{{ $t('lz.label.videoWall.sceneInfo') }}</span>
		</div>
		<!-- Kịch bản phủ ngoài vùng được phân quyền: chỉ sửa được cửa sổ trong vùng, không sửa thông tin -->
		<el-text v-if="infoReadonly" class="scene-form-readonly" type="warning" size="small">
			<el-icon><ele-Lock /></el-icon>
			{{ $t('lz.label.videoWall.sceneOutsideReadonly') }}
		</el-text>
		<el-form label-position="top" autocomplete="off" @submit.prevent>
			<el-form-item :label="$t('lz.entity.base.code')" required>
				<el-input
					:model-value="sceneCode"
					@update:model-value="emit('update:sceneCode', $event)"
					:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.code') })"
					maxlength="64"
					:disabled="infoReadonly || codeReadonly"
					:clearable="!codeReadonly"
				/>
			</el-form-item>
			<el-form-item :label="$t('lz.entity.base.name')" required>
				<el-input
					:model-value="sceneName"
					@update:model-value="emit('update:sceneName', $event)"
					:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.name') })"
					maxlength="64"
					:disabled="infoReadonly"
					clearable
				/>
			</el-form-item>
			<el-form-item :label="$t('lz.entity.base.remark')">
				<el-input
					:model-value="sceneRemark"
					@update:model-value="emit('update:sceneRemark', $event)"
					:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.remark') })"
					type="textarea"
					:rows="2"
					maxlength="256"
					:disabled="infoReadonly"
					show-word-limit
					clearable
				/>
			</el-form-item>
		</el-form>
	</div>
</template>

<script lang="ts" setup name="videoWallSceneToolbar">
import { useI18n } from 'vue-i18n';
import type { VwControllerOutput } from '/@/api-services/videoWall';

const { t: $t } = useI18n();

interface Props {
	sceneCode: string;
	sceneName: string;
	/** Ghi chú kịch bản (VwScene.Remark) */
	sceneRemark?: string;
	/** Khoá mã / tên / ghi chú (kịch bản phủ ngoài vùng được phân quyền); nút Lưu vẫn dùng cho thay đổi cửa sổ. */
	infoReadonly?: boolean;
	/** Khoá riêng ô mã: mã chỉ nhập khi tạo mới, kịch bản đã lưu thì không cho sửa. */
	codeReadonly?: boolean;
	controllerId?: string;
	controllers?: VwControllerOutput[];
}
withDefaults(defineProps<Props>(), {
	sceneRemark: '',
	infoReadonly: false,
	codeReadonly: false,
	controllerId: '',
	controllers: () => [],
});
const emit = defineEmits<{
	(e: 'update:sceneCode', value: string): void;
	(e: 'update:sceneName', value: string): void;
	(e: 'update:sceneRemark', value: string): void;
	(e: 'update:controllerId', value: string): void;
}>();
</script>

<style scoped>
.scene-form {
	padding: 12px 14px 4px;
}

.scene-form-header {
	display: flex;
	align-items: center;
	gap: 8px;
	margin-bottom: 10px;
}

.scene-form-icon {
	color: var(--el-color-primary);
}

.scene-form-title {
	font-size: 15px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

.scene-form-readonly {
	display: block;
	margin-bottom: 8px;
}

.scene-form :deep(.el-form-item) {
	margin-bottom: 12px;
}

.scene-form :deep(.el-form-item__label) {
	margin-bottom: 4px;
	line-height: 20px;
	font-size: 14px;
	font-weight: 500;
}
</style>
