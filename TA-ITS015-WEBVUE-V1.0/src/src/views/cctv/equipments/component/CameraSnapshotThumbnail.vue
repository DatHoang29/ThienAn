<template>
	<div class="camera-snapshot-thumbnail" :class="{ 'has-image': !!resolvedUrl }">
		<div class="thumbnail-image-wrap" v-if="resolvedUrl">
			<img :src="resolvedUrl" :alt="cameraName" class="thumbnail-image" @error="onImageError" />
			<div class="thumbnail-overlay">
				<el-tooltip :content="$t('lz.label.cctvEquipment.viewSnapshot')" placement="top">
					<el-button circle size="small" type="primary" @click.stop="handleView">
						<el-icon><ele-View /></el-icon>
					</el-button>
				</el-tooltip>
			</div>
		</div>
		<div class="thumbnail-placeholder" v-else>
			<el-icon :size="24"><ele-VideoCamera /></el-icon>
			<span>{{ $t('lz.label.cctvEquipment.noSnapshot') }}</span>
		</div>
		<div class="thumbnail-time" v-if="formattedTime">
			<el-icon :size="12"><ele-Clock /></el-icon>
			<span>{{ formattedTime }}</span>
		</div>

		<!-- Preview Dialog -->
		<el-dialog v-model="previewVisible" width="720px" append-to-body destroy-on-close
			:close-on-click-modal="true" class="snapshot-preview-dialog">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle">
						<ele-Picture />
					</el-icon>
					<span> {{ dialogTitle }} </span>
				</div>
			</template>
			<div class="preview-content">
				<img :src="resolvedUrl" :alt="cameraName" class="preview-image" />
			</div>
			<template #footer>
				<span class="preview-time" v-if="formattedTime">
					<el-icon :size="14"><ele-Clock /></el-icon>
					{{ formattedTime }}
				</span>
				<el-button @click="previewVisible = false">{{ $t('lz.button.base.close') }}</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { dayjs } from 'element-plus';
import { getAPI } from '/@/utils/axios-utils';
import { useItsMediaServerStore } from '/@/stores/its/itsMediaServer';

const { t: $t } = useI18n();

const props = defineProps<{
	snapshotUrl?: string | null;
	snapshotTime?: string | Date | null;
	cameraName?: string;
	cacheKey?: number;
}>();

const previewVisible = ref(false);
const imageError = ref(false);
const fileServerUrl = ref('');

// Load file server base URL
onMounted(async () => {
	try {
		// Địa chỉ file server dùng chung (chỉ gọi API 1 lần cho toàn app)
		fileServerUrl.value = await useItsMediaServerStore().ensureLoaded();
	} catch {
		fileServerUrl.value = '';
	}
});

const resolvedUrl = computed(() => {
	if (imageError.value) return '';
	const url = props.snapshotUrl || '';
	if (!url) return '';
	let fullUrl = '';
	// Nếu đã là URL đầy đủ thì dùng trực tiếp
	if (url.startsWith('http://') || url.startsWith('https://')) {
		fullUrl = url;
	} else {
		// Ghép với file server base URL
		const baseUrl = (fileServerUrl.value || '').replace(/\/+$/, '');
		if (!baseUrl) return '';
		fullUrl = `${baseUrl}${url.startsWith('/') ? '' : '/'}${url}`;
	}
	// Thêm t=timestamp để tránh cache ảnh cũ
	const separator = fullUrl.includes('?') ? '&' : '?';
	return `${fullUrl}${separator}t=${props.cacheKey || Date.now()}`;
});

const formattedTime = computed(() => {
	if (!props.snapshotTime) return '';
	const time = dayjs(props.snapshotTime);
	return time.isValid() ? time.format('DD/MM/YYYY HH:mm:ss') : '';
});

const dialogTitle = computed(() => {
	return `${$t('lz.label.cctvEquipment.snapshotPreview')} - ${props.cameraName || ''}`;
});

const handleView = () => {
	previewVisible.value = true;
};

const onImageError = () => {
	imageError.value = true;
};

// Reset lỗi ảnh khi cacheKey thay đổi (sync mới)
watch(() => props.cacheKey, () => {
	imageError.value = false;
});
</script>

<style scoped lang="scss">
.camera-snapshot-thumbnail {
	position: relative;
	width: 80px;
	height: 50px;
	border-radius: 4px;
	overflow: hidden;
	border: 1px solid var(--el-border-color-light, #e4e7ec);
	background: var(--el-fill-color-light, #f9fafb);
	flex-shrink: 0;
}

.thumbnail-image-wrap {
	width: 100%;
	height: 100%;
	position: relative;
}

.thumbnail-image {
	width: 100%;
	height: 100%;
	object-fit: cover;
	display: block;
}

.thumbnail-overlay {
	position: absolute;
	inset: 0;
	display: flex;
	align-items: center;
	justify-content: center;
	background: rgba(0, 0, 0, 0.4);
	opacity: 0;
	transition: opacity 0.2s ease;
}

.thumbnail-image-wrap:hover .thumbnail-overlay {
	opacity: 1;
}

.thumbnail-placeholder {
	width: 100%;
	height: 100%;
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	gap: 2px;
	color: var(--el-text-color-secondary, #909399);

	span {
		font-size: 9px;
		line-height: 1.2;
	}
}

.thumbnail-time {
	position: absolute;
	bottom: 0;
	left: 0;
	right: 0;
	background: rgba(0, 0, 0, 0.65);
	color: #ffffff;
	font-size: 9px;
	padding: 1px 3px;
	display: flex;
	align-items: center;
	gap: 2px;
	line-height: 1.3;
}

.preview-content {
	display: flex;
	align-items: center;
	justify-content: center;
	background: #000000;
	border-radius: 4px;
	overflow: hidden;
}

.preview-image {
	max-width: 100%;
	max-height: 480px;
	object-fit: contain;
}

.preview-time {
	display: inline-flex;
	align-items: center;
	gap: 4px;
	color: var(--el-text-color-secondary);
	font-size: 13px;
	margin-right: auto;
}

:global(.snapshot-preview-dialog .el-dialog__footer) {
	display: flex;
	align-items: center;
}


</style>
