<template>
	<section class="evidence-panel workspace-media-panel" :aria-label="$t('lz.entity.base.image')" aria-keyshortcuts="ArrowLeft ArrowRight">
		<div class="section-heading evidence-toolbar">
			<h3>
				<el-icon><Picture /></el-icon>
				{{ $t('lz.entity.base.image') }}
			</h3>
			<div class="evidence-tools">
				<el-button :icon="Refresh" text :disabled="loading" :title="$t('lz.button.base.refresh')" :aria-label="$t('lz.button.base.refresh')" @click="loadImages" />
				<el-button :icon="Download" text :disabled="!currentImage?.url" :title="$t('lz.button.base.download')" :aria-label="$t('lz.button.base.download')" @click="downloadImage" />
			</div>
		</div>
		<div v-if="stages.length > 0" class="media-stages" :aria-label="$t('lz.label.base.captureStage')">
			<button v-for="item in stages" :key="item.value" type="button" :class="{ active: stage === item.value }" :aria-pressed="stage === item.value" @click="stage = item.value">
				{{ item.label }}
			</button>
		</div>
		<div v-else class="media-stages" :aria-label="$t('lz.entity.base.type')">
			<button v-for="(item, index) in images" :key="item.key" type="button" :class="{ active: imageIndex === index }" :aria-pressed="imageIndex === index" @click="imageIndex = index">
				{{ item.title }}
			</button>
		</div>
		<div class="evidence-media">
			<div v-loading="loading" class="evidence-frame" :aria-busy="loading">
				<el-image
					v-if="currentImage?.url"
					:key="currentImage.url"
					:src="currentImage.url"
					:alt="currentImage.title"
					:preview-src-list="previewUrls"
					:initial-index="previewIndex"
					preview-teleported
					fit="contain"
					:zoom-rate="1.2"
				>
					<template #error>
						<div class="evidence-empty">
							<el-icon :size="32"><Picture /></el-icon>
							<span>{{ $t('lz.message.base.errorFetchingData') }}</span>
						</div>
					</template>
				</el-image>
				<div v-else class="evidence-empty">
					<el-icon :size="38"><Picture /></el-icon>
					<strong>
						{{ loading ? $t('lz.label.base.loading') : currentImage?.status || $t('lz.label.base.noImage') }}
					</strong>
				</div>
			</div>
			<div v-if="thumbnails && images.length > 1" class="camera-thumbnails" :aria-label="$t('lz.label.base.pleaseSelect', { input: $t('lz.label.base.camera') })">
				<button
					v-for="(item, index) in images"
					:key="item.key"
					type="button"
					:class="{ active: imageIndex === index }"
					:aria-pressed="imageIndex === index"
					:title="item.title"
					:aria-label="$t('lz.label.base.imageNumber', { index: index + 1, name: item.title })"
					@click="imageIndex = index"
				>
					<span class="camera-preview">
						<img v-if="item.url" :src="item.url" :alt="item.title" loading="lazy" />
						<el-icon v-else><Picture /></el-icon>
					</span>
					<span class="camera-name">{{ item.title }}</span>
				</button>
			</div>
		</div>
	</section>
</template>

<script setup lang="ts" name="workspaceMediaPanel" generic="RecordType extends object">
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';
import { Picture, Refresh, Download } from '@element-plus/icons-vue';
import { saveAs } from 'file-saver';
import type { EvidenceImage, EvidenceLoader, EvidenceStage } from './workspace';

interface Props {
	record: RecordType;
	fileName?: string;
	recordKey: string;
	version: number;
	load: EvidenceLoader<RecordType>;
	initialImages?: EvidenceImage[];
	stages?: EvidenceStage[];
	thumbnails?: boolean;
	baseUrl?: string;
}

const props = withDefaults(defineProps<Props>(), { initialImages: () => [], stages: () => [], thumbnails: false, fileName: 'evidence' });

// Component state
const { t: $t } = useI18n();
const stage = ref(props.stages[0]?.value ?? 1);
const imageIndex = ref(0);
const loading = ref(false);
const sourceImages = ref<EvidenceImage[]>([]);
let requestId = 0;
let abort: AbortController | undefined;
const objectUrls = new Set<string>();

// Translated captions and preview data
const images = computed(() =>
	sourceImages.value.map((image) => ({
		...image,
		title: image.title || (image.titleKey ? $t(image.titleKey, image.titleParams ?? {}) : ''),
		status: image.statusKey ? $t(image.statusKey) : '',
	}))
);
const stages = computed(() => props.stages.map((item) => ({ ...item, label: $t(item.labelKey) })));
const currentImage = computed(() => images.value[imageIndex.value]);
const previewUrls = computed(() => images.value.flatMap((item) => (item.url ? [item.url] : [])));
const previewIndex = computed(() => Math.max(0, previewUrls.value.indexOf(currentImage.value?.url ?? '')));

// Image navigation
function navigateEvidence(direction: -1 | 1): boolean {
	if (stages.value.length) {
		const index = stages.value.findIndex((item) => item.value === stage.value);
		const nextIndex = Math.max(0, Math.min(stages.value.length - 1, index + direction));
		stage.value = stages.value[nextIndex].value;
		return true;
	}
	if (images.value.length < 2) return false;
	imageIndex.value = Math.max(0, Math.min(images.value.length - 1, imageIndex.value + direction));
	return true;
}

// Loading and object URL ownership
function releaseObjectURL(url: string) {
	if (objectUrls.delete(url)) URL.revokeObjectURL(url);
}

function clearImages() {
	objectUrls.forEach(releaseObjectURL);
	sourceImages.value = [...props.initialImages];
}

async function loadImages() {
	const request = ++requestId;
	abort?.abort();
	abort = new AbortController();
	const signal = abort.signal;
	clearImages();
	imageIndex.value = 0;
	loading.value = true;
	const createdUrls: string[] = [];
	let published = false;
	try {
		const results = await props.load(
			{ ...props.record },
			{
				stage: stage.value,
				signal,
				baseUrl: props.baseUrl,
				createObjectURL(blob) {
					if (request !== requestId || signal.aborted) return undefined;
					const url = URL.createObjectURL(blob);
					createdUrls.push(url);
					objectUrls.add(url);
					return url;
				},
			}
		);
		if (request !== requestId) return;
		sourceImages.value = results;
		published = true;
	} catch {
		if (request === requestId)
			sourceImages.value = [
				{
					key: 'error',
					titleKey: 'lz.entity.base.image',
					statusKey: 'lz.message.base.errorFetchingData',
				},
			];
	} finally {
		if (!published) createdUrls.forEach(releaseObjectURL);
		if (request === requestId) loading.value = false;
	}
}

// Download
function downloadImage() {
	if (!currentImage.value?.url) return;
	try {
		saveAs(currentImage.value.url, `${props.fileName.replace(/[^a-zA-Z0-9_-]/g, '_')}_${imageIndex.value + 1}.jpg`);
	} catch {
		ElMessage.error($t('lz.message.base.failedToDownloadFile'));
	}
}

// Watchers and lifecycle
watch(
	() => [props.recordKey, props.version, stage.value],
	() => {
		void loadImages();
	},
	{ immediate: true }
);
onBeforeUnmount(() => {
	++requestId;
	abort?.abort();
	clearImages();
});

defineExpose({ navigateEvidence });
</script>

<style lang="scss">
.workspace-media-panel {
	flex-shrink: 0;
}
.workspace-media-panel .section-heading {
	display: flex;
	justify-content: space-between;
	gap: 8px;
	align-items: center;
	margin-bottom: 4px;
}
.workspace-media-panel .section-heading h3 {
	display: flex;
	gap: 6px;
	align-items: center;
	font-size: calc(13px + var(--workspace-panel-font-increment, 0px));
	font-weight: 650;
}
.workspace-media-panel .section-heading .el-button {
	font-size: calc(11px + var(--workspace-panel-font-increment, 0px));
	min-height: 24px;
	padding: 4px;
}
.workspace-media-panel .evidence-tools {
	display: flex;
	gap: 4px;
}
.workspace-media-panel .evidence-tools .el-button {
	width: 24px;
}
.workspace-media-panel {
	margin-bottom: 8px;
}
.workspace-media-panel .media-stages {
	display: flex;
	gap: 4px;
	padding: 3px;
	border-radius: 7px;
	background: var(--workspace-heading-bg, var(--el-fill-color-light, #f5f7fa));
	border: 1px solid var(--workspace-border-strong, var(--el-border-color, #dcdfe6));
	margin-bottom: 6px;
}
.workspace-media-panel .media-stages button {
	flex: 1;
	min-width: 0;
	border: 1px solid transparent;
	background: transparent;
	color: var(--workspace-muted, var(--el-text-color-secondary, #909399));
	font-size: calc(12px + var(--workspace-panel-font-increment, 0px));
	border-radius: 5px;
	padding: 3px;
	min-height: 22px;
}
.workspace-media-panel .media-stages button.active {
	background: var(--workspace-primary, var(--el-color-primary, #409eff));
	border-color: var(--workspace-primary, var(--el-color-primary, #409eff));
	color: var(--workspace-surface, var(--el-bg-color, #fff));
	font-weight: 650;
}
.workspace-media-panel .media-stages button:not(.active):hover {
	background: var(--workspace-surface, var(--el-bg-color, #fff));
	color: var(--workspace-primary-dark, var(--el-color-primary-dark-2, #337ecc));
}
.workspace-media-panel .evidence-media {
	--workspace-evidence-height: clamp(170px, 28vh, 210px);
	display: grid;
	grid-template-columns: minmax(0, 1fr);
	gap: 6px;
}
.workspace-media-panel .evidence-frame {
	width: 100%;
	min-width: 0;
	height: var(--workspace-evidence-height);
	background: #172536;
	border-radius: 6px;
	overflow: hidden;
	position: relative;
}
.workspace-media-panel .evidence-frame .el-image {
	width: 100%;
	height: 100%;
	display: block;
}
.workspace-media-panel .evidence-frame .el-loading-mask {
	background: rgba(23, 37, 54, 0.85);
}
.workspace-media-panel .evidence-empty {
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	gap: 8px;
	width: 100%;
	height: 100%;
	padding: 20px;
	color: #aabed5;
	text-align: center;
}
.workspace-media-panel .evidence-empty strong {
	font-size: calc(13px + var(--workspace-panel-font-increment, 0px));
	color: #dce6f3;
}
.workspace-media-panel .evidence-empty span {
	font-size: calc(11px + var(--workspace-panel-font-increment, 0px));
}
.workspace-media-panel .camera-thumbnails {
	display: flex;
	min-width: 0;
	gap: 5px;
	overflow-x: auto;
	overflow-y: hidden;
	scrollbar-width: thin;
	padding-bottom: 2px;
}
.workspace-media-panel .camera-thumbnails button {
	display: inline-flex;
	align-items: center;
	gap: 5px;
	max-width: 140px;
	height: 30px;
	flex-shrink: 0;
	border: 1px solid var(--workspace-border, var(--el-border-color, #dcdfe6));
	border-radius: 5px;
	padding: 3px 6px 3px 3px;
	background: var(--workspace-subtle, var(--el-fill-color-light, #f5f7fa));
	color: var(--workspace-muted, var(--el-text-color-secondary, #909399));
	font-family: inherit;
	line-height: 1.2;
	cursor: pointer;
}
.workspace-media-panel .camera-thumbnails button:hover {
	border-color: var(--workspace-primary, var(--el-color-primary, #409eff));
	color: var(--workspace-primary, var(--el-color-primary, #409eff));
}
.workspace-media-panel .camera-thumbnails button.active {
	border-color: var(--workspace-primary, var(--el-color-primary, #409eff));
	color: var(--workspace-primary, var(--el-color-primary, #409eff));
	background: var(--workspace-primary-soft, var(--el-color-primary-light-9, #ecf5ff));
}
.workspace-media-panel .camera-preview {
	display: flex;
	align-items: center;
	justify-content: center;
	width: 30px;
	height: 22px;
	flex-shrink: 0;
	border-radius: 3px;
	overflow: hidden;
	background: var(--workspace-heading-bg, var(--el-fill-color, #f0f2f5));
}
.workspace-media-panel .camera-preview img {
	width: 100%;
	height: 100%;
	object-fit: cover;
}
.workspace-media-panel .camera-preview .el-icon {
	font-size: 15px;
}
.workspace-media-panel .camera-name {
	min-width: 0;
	font-size: calc(11px + var(--workspace-panel-font-increment, 0px));
	overflow: hidden;
	white-space: nowrap;
	text-overflow: ellipsis;
}
.workspace-media-panel {
	--workspace-panel-font-increment: var(--detail-font-increment, 0px);
	--workspace-evidence-height: clamp(170px, 28vh, 210px);
	min-width: 0;
}
.workspace-media-panel h3 {
	margin: 0;
	font-size: 13px;
}
</style>
