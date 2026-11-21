<template>
	<div class="cctv-home-page" :class="{ 'is-fullscreen': state.fullscreen }" v-auth="'cctvDashboardHome:view'">
		<template v-if="!state.fullscreen">
			<div class="cctv-home-layout">
				<aside class="cctv-sidebar">
					<section class="sidebar-section dashboard-section">
						<div class="sidebar-section-header">
							<div>
								<h3>{{ $t('lz.label.cctvDashboard.title') }}</h3>
								<p>{{ $t('lz.label.cctvDashboard.dashboardCount', { count: filteredDashboards.length })
								}}</p>
							</div>
							<div class="sidebar-header-actions">
								<el-tooltip :content="$t('lz.button.cctvDashboard.refreshDashboards')" placement="top">
									<el-button circle text :loading="state.loadingDashboards"
										@click="handleRefreshDashboards">
										<el-icon><ele-Refresh /></el-icon>
									</el-button>
								</el-tooltip>
								<el-tooltip :content="$t('lz.button.cctvDashboard.addDashboard')" placement="top">
									<el-button circle type="primary" @click="handleAddDashboard"
										v-auth="'cctvDashboardHome:add'">
										<el-icon><ele-Plus /></el-icon>
									</el-button>
								</el-tooltip>
							</div>
						</div>

						<div class="dashboard-tools">
							<el-input v-model="state.dashboardQuery"
								:placeholder="$t('lz.label.cctvDashboard.searchDashboard')" clearable>
								<template #prefix>
									<el-icon><ele-Search /></el-icon>
								</template>
							</el-input>
							<el-popover placement="bottom-end" :width="220" trigger="click">
								<template #reference>
									<el-badge :value="state.dashboardFilterTags.length || ''"
										:hidden="!state.dashboardFilterTags.length" type="primary">
										<el-button circle text class="filter-btn">
											<el-icon><ele-Filter /></el-icon>
										</el-button>
									</el-badge>
								</template>
								<div class="dashboard-tag-filter">
									<div class="dashboard-tag-filter-header">
										<span>{{ $t('lz.label.cctvDashboard.filterByTag') }}</span>
										<el-button v-if="state.dashboardFilterTags.length" link type="primary"
											size="small" @click="state.dashboardFilterTags = []">{{
												$t('lz.button.cctvDashboard.clearFilter') }}</el-button>
									</div>
									<el-checkbox-group v-model="state.dashboardFilterTags"
										class="dashboard-tag-filter-list">
										<el-checkbox v-for="tag in availableDashboardTags" :key="tag" :label="tag"
											:value="tag">
											{{ tag }}
										</el-checkbox>
									</el-checkbox-group>
									<el-empty v-if="!availableDashboardTags.length"
										:description="$t('lz.label.cctvDashboard.noTags')" :image-size="40" />
								</div>
							</el-popover>
						</div>

						<div v-loading="state.loadingDashboards" class="dashboard-list">
							<article v-for="dashboard in filteredDashboards" :key="dashboard.id" role="button"
								tabindex="0" class="dashboard-row"
								:class="{ 'is-active': dashboard.id === state.selectedDashboardId }"
								@click="handleSelectDashboard(dashboard.id)"
								@keydown.enter.prevent="handleSelectDashboard(dashboard.id)"
								@keydown.space.prevent="handleSelectDashboard(dashboard.id)">
								<el-tooltip :content="dashboard.remark || ''" placement="right"
									:disabled="!dashboard.remark" :show-after="350">
									<span class="dashboard-row-main">
										<strong>{{ dashboard.name }}</strong>
										<span v-if="dashboard.tags?.length" class="dashboard-row-tags">
											<el-tag v-for="tag in dashboard.tags.slice(0, 3)" :key="tag" size="small">{{
												tag
											}}</el-tag>
										</span>
									</span>
								</el-tooltip>
								<el-dropdown trigger="click"
									@command="(command: any) => handleDashboardRowCommand(command, dashboard)"
									@click.stop>
									<el-button circle text :aria-label="$t('lz.label.cctvDashboard.dashboardActions')">
										<i class="fa fa-ellipsis-v"></i>
									</el-button>
									<template #dropdown>
										<el-dropdown-menu>
											<el-dropdown-item command="duplicate" v-auth="'cctvDashboardHome:add'">
												<el-icon><ele-CopyDocument /></el-icon>
												{{ $t('lz.button.cctvDashboard.duplicate') }}
											</el-dropdown-item>
											<el-dropdown-item divided command="delete" v-auth="'cctvDashboardHome:delete'">
												<el-icon><ele-Delete /></el-icon>
												{{ $t('lz.button.cctvDashboard.deleteDashboard') }}
											</el-dropdown-item>
										</el-dropdown-menu>
									</template>
								</el-dropdown>
							</article>

							<el-empty v-if="!state.loadingDashboards && !filteredDashboards.length"
								:description="$t('lz.label.cctvDashboard.noMatchingDashboard')" :image-size="90" />
						</div>
					</section>

					<section class="sidebar-section camera-section">
						<CameraEquipmentPanel :equipments="filteredEquipments" :query="state.equipmentQuery"
							:loading="state.equipmentLoading" :refreshing="state.equipmentLoading"
							:adding-camera-id="editorState.addingCameraId"
							:dragging-over-grid="editorState.draggingOverGrid" :is-added="isEquipmentAdded"
							:selected-type="state.equipmentTypeFilter" :equipment-types="availableEquipmentTypes"
							@update:query="state.equipmentQuery = $event"
							@update:selected-type="state.equipmentTypeFilter = $event" @add="handleAddCameraFromPanel"
							@drag-start="handleStartManualDrag" @refresh="handleRefreshEquipments" />
					</section>
				</aside>

				<main class="cctv-workspace">
					<section v-if="!activeDashboard && !state.loadingDashboard" class="empty-guide">
						<div class="empty-guide-content">
							<el-icon><ele-VideoCamera /></el-icon>
							<h2>{{ $t('lz.label.cctvDashboard.emptyTitle') }}</h2>
							<p>{{ $t('lz.label.cctvDashboard.emptyDescription') }}</p>
							<div class="empty-guide-steps">
								<span>{{ $t('lz.label.cctvDashboard.emptyStepSelect') }}</span>
								<span>{{ $t('lz.label.cctvDashboard.emptyStepDrag') }}</span>
								<span>{{ $t('lz.label.cctvDashboard.emptyStepSave') }}</span>
							</div>
							<el-button type="primary" @click="handleAddDashboard" v-auth="'cctvDashboardHome:add'">
								<el-icon color="white"><ele-Plus /></el-icon>
								{{ $t('lz.button.cctvDashboard.createDashboard') }}
							</el-button>
						</div>
					</section>

					<section v-else-if="state.loadingDashboard" class="workspace-loading">
						<el-skeleton :rows="8" animated />
					</section>

					<section v-else class="dashboard-detail">
						<header class="detail-header">
							<div class="detail-copy">
								<div class="dashboard-field-row">
									<input v-model="info.name" class="dashboard-name-input"
										:placeholder="$t('lz.label.cctvDashboard.dashboardName')"
										@keydown.enter.prevent="handleSaveDashboardInfo"
										@keydown.esc.prevent="handleCancelDashboardInfo" />
									<div v-if="isDashboardNameDirty" class="dashboard-name-actions"
										v-auth="'cctvDashboardHome:update'">
										<el-button size="small" type="primary" :loading="state.savingInfo"
											@click="handleSaveDashboardInfo">
											<el-icon><ele-Check /></el-icon>
											{{ $t('lz.button.base.save') }}
										</el-button>
										<el-button size="small" :disabled="state.savingInfo"
											@click="handleCancelDashboardInfo">
											{{ $t('lz.button.base.cancel') }}
										</el-button>
									</div>
								</div>
							</div>

							<div class="detail-actions">
								<CustomSelect v-model="info.templateId" :options="templateOptions" size="default"
									class="template-select" popper-class="template-select-dropdown" clearable
									:label="$t('lz.label.cctvDashboard.template')"
									@update:model-value="handleTemplateChange">
									<template #option="{ option }">
										<CameraTemplatePreview v-if="isTemplateOption(option)" :template="option.data"
											size="sm" />
										<span v-else>{{ option.label }}</span>
									</template>
								</CustomSelect>
								<el-button :type="editorState.editMode ? 'primary' : 'default'" :loading="state.saving"
									@click="handleToggleEditMode" v-auth="'cctvDashboardHome:update'">
									<el-icon><ele-Edit /></el-icon>
									{{ editorState.editMode ? $t('lz.button.cctvDashboard.saveLayout') :
										$t('lz.button.cctvDashboard.editLayout') }}
								</el-button>
								<el-button @click="enterFullscreen">
									<el-icon><ele-FullScreen /></el-icon>
									{{ $t('lz.button.cctvDashboard.fullscreen') }}
								</el-button>
								<el-dropdown trigger="click" @command="handleDetailMoreCommand">
									<el-button circle plain :aria-label="$t('lz.label.cctvDashboard.moreActions')">
										<i class="fa fa-ellipsis-v no-margin-right"></i>
									</el-button>
									<template #dropdown>
										<el-dropdown-menu>
											<el-dropdown-item command="edit-info" v-auth="'cctvDashboardHome:update'">
												<el-icon><ele-Edit /></el-icon>
												{{ $t('lz.button.cctvDashboard.editInfo') }}
											</el-dropdown-item>
											<el-dropdown-item command="duplicate" v-auth="'cctvDashboardHome:add'">
												<el-icon><ele-CopyDocument /></el-icon>
												{{ $t('lz.button.cctvDashboard.duplicate') }}
											</el-dropdown-item>
											<el-dropdown-item command="export-template" v-auth="'cctvDashboardHome:update'">
												<el-icon><ele-DocumentAdd /></el-icon>
												{{ $t('lz.button.cctvDashboard.exportTemplate') }}
											</el-dropdown-item>
											<el-dropdown-item divided command="delete" v-auth="'cctvDashboardHome:delete'">
												<el-icon><ele-Delete /></el-icon>
												{{ $t('lz.button.cctvDashboard.deleteDashboard') }}
											</el-dropdown-item>
										</el-dropdown-menu>
									</template>
								</el-dropdown>
							</div>
						</header>

						<section ref="gridDropRef" class="grid-pane"
							:class="{ 'is-drag-over': editorState.draggingOverGrid }"
							@dragenter.prevent="onGridDragOver" @dragover.prevent="onGridDragOver"
							@dragleave="onGridDragLeave" @drop="onGridDrop">
							<div v-if="editorState.draggingCameraId" class="grid-drop-overlay" :style="gridOverlayStyle"
								@dragenter.prevent="onGridDragOver" @dragover.prevent="onGridDragOver"
								@dragleave="onGridDragLeave" @drop="onGridDrop">
								<div v-if="editorState.dragGhost" class="grid-drop-ghost" :style="gridGhostStyle">
									<el-icon><ele-VideoCamera /></el-icon>
								</div>
							</div>
							<CameraGrid ref="cameraGridRef" :cameras="editorState.cameras"
								:edit-mode="editorState.editMode" :selected-slot-id="editorState.selectedSlotId"
								@detail="handleOpenCameraDetail" @remove="handleRemoveCamera"
								@add-slot="setSelectedSlotForAdd" @select="handleSelectCamera"
								@layout-change="updateCameraLayout" @layout-dirty="state.layoutDirty = true" />
						</section>
					</section>
					<el-dialog v-model="state.dashboardInfoDialogVisible" width="520px" append-to-body destroy-on-close
						class="dashboard-info-dialog" @closed="handleDashboardInfoDialogClosed">
						<template #header>
							<div style="color: #fff">
								<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle">
									<ele-Edit />
								</el-icon>
								<span> {{ dashboardInfoDialogTitle }} </span>
							</div>
						</template>
						<el-form label-position="top" class="dashboard-info-form" @submit.prevent>
							<el-form-item :label="$t('lz.label.cctvDashboard.dashboardName')" required>
								<el-input v-model="info.name"
									:placeholder="$t('lz.label.cctvDashboard.dashboardNamePlaceholder')"
									@keydown.enter.prevent="handleSaveDashboardInfoDialog" />
							</el-form-item>
							<el-form-item :label="$t('lz.label.cctvDashboard.tag')">
								<div class="dashboard-tags-editor">
									<el-tag v-for="tag in info.tags" :key="tag" closable size="small"
										@close="removeTag(tag)">
										{{ tag }}
									</el-tag>
									<el-autocomplete v-model="state.tagInput" class="tag-input" size="small"
										:placeholder="$t('lz.label.cctvDashboard.addTag')" clearable value-key="value"
										:fetch-suggestions="queryDashboardTagSuggestions"
										@select="handleTagSuggestionSelect" @keydown="onTagKeydown"
										@blur="commitTagInput" />
								</div>
							</el-form-item>
							<el-form-item :label="$t('lz.entity.base.remark')">
								<el-input v-model="info.remark" type="textarea" :rows="3"
									:placeholder="$t('lz.label.cctvDashboard.remarkPlaceholder')" />
							</el-form-item>
						</el-form>
						<template #footer>
							<el-button :disabled="state.savingInfo" @click="handleCancelDashboardInfoDialog">{{
								$t('lz.button.base.cancel') }}</el-button>
							<el-button type="primary" :loading="state.savingInfo"
								@click="handleSaveDashboardInfoDialog">
								{{ $t('lz.button.base.save') }}
							</el-button>
						</template>
					</el-dialog>

					<!-- Camera Info Dialog -->
					<el-dialog v-model="state.cameraInfoDialogVisible" width="460px" append-to-body destroy-on-close
						class="camera-info-dialog">
						<template #header>
							<div style="color: #fff">
								<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle">
									<ele-InfoFilled />
								</el-icon>
								<span> {{ $t('lz.label.cctvDashboard.cameraInfo') }} </span>
							</div>
						</template>
						<div v-if="state.cameraInfoData" class="camera-info-dialog-body">
							<el-descriptions :column="1" border>
								<el-descriptions-item :label="$t('lz.entity.base.name')">{{ state.cameraInfoData.name ||
									'--'
								}}</el-descriptions-item>
								<el-descriptions-item label="DeviceId">{{ state.cameraInfoData.deviceId || '--'
								}}</el-descriptions-item>
								<el-descriptions-item label="IP">{{ state.cameraInfoData.ip || '--'
								}}</el-descriptions-item>
								<el-descriptions-item :label="$t('lz.label.base.location')">{{
									state.cameraInfoData.location || '--'
								}}</el-descriptions-item>
								<el-descriptions-item :label="$t('lz.entity.base.status')">{{
									state.cameraInfoData.status || '--'
								}}</el-descriptions-item>
								<el-descriptions-item :label="$t('lz.label.cctvDashboard.gridPosition')">
									x: {{ state.cameraInfoData.x }}, y: {{ state.cameraInfoData.y }}, w: {{
										state.cameraInfoData.w }},
									h: {{ state.cameraInfoData.h }}
								</el-descriptions-item>
							</el-descriptions>
						</div>
						<template #footer>
							<el-button type="primary" @click="state.cameraInfoDialogVisible = false">{{
								$t('lz.button.base.close') }}</el-button>
						</template>
					</el-dialog>
				</main>
			</div>
		</template>

		<div v-else ref="fullscreenRef">
			<FullscreenCameraStage ref="fullscreenStageRef" :cameras="editorState.cameras"
				:focused-camera="focusedFullscreenCamera" :focus-mode="state.focusedFullscreenMode" :edit-mode="false"
				@detail="handleOpenCameraDetail" @remove="handleRemoveCamera" @add-slot="setSelectedSlotForAdd"
				@select="handleFocusFullscreenCamera" @replace-focus-camera="handleReplaceFullscreenCamera"
				@focus-settings="handleOpenFullscreenCameraSettings" @focus-mode="handleSetFullscreenFocusMode"
				@camera-action="handleFullscreenCameraAction" @clear-focus="clearFullscreenFocus"
				@layout-change="updateCameraLayout" @layout-dirty="state.layoutDirty = true" />
		</div>

		<div v-if="editorState.manualDragging && editorState.dragPointer" class="camera-drag-cursor"
			:style="dragCursorStyle">
			<el-icon><ele-VideoCamera /></el-icon>
		</div>
	</div>
</template>

<script setup lang="ts" name="cctvDashboardHome">
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue';
import { onBeforeRouteLeave, useRoute } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import { saveAs } from 'file-saver';
import { useI18n } from 'vue-i18n';
import CameraEquipmentPanel from './component/CameraEquipmentPanel.vue';
import CameraGrid from './component/CameraGrid.vue';
import CameraTemplatePreview from './component/CameraTemplatePreview.vue';
import CustomSelect from './component/CustomSelect.vue';
import FullscreenCameraStage from './component/FullscreenCameraStage.vue';
import { useCctvDashboardStore } from '/@/stores/cctvDashboard';
import {
	createCctvDashboard,
	createCctvDashboardTemplate,
	deleteCctvDashboard,
	duplicateCctvDashboard,
	getCctvDashboardDetail,
	getCctvSnapshotUrl,
	loadCctvDashboardList,
	loadCctvDashboardTags,
	loadCctvDashboardTemplates,
	loadCctvEquipments,
	saveCctvDashboardItems,
	updateCctvDashboard,
} from './services/cctvDashboardService';
import { applyCctvDashboardConfigFromStore } from './services/cctvDashboardConfig';
import { canUseLiveCameraControls, canUseCameraPlayback } from './utils/cameraControlUtils';
import { getCameraSourceId, getEquipmentId, useCameraDashboardEditor } from './composables/useCameraDashboardEditor';
import { normalizeCameraGridItem, type DashboardCCTVDetailOutput, type DashboardCCTVOutput, type DashboardCameraInfo, type ICctvDashboardTemplate, type VideoMonitoringItem } from '/@/views/cctv/dashboard/types';

const { t: $t } = useI18n();
const route = useRoute();
const cctvDashboardStore = useCctvDashboardStore();
const equipments = ref<DashboardCameraInfo[]>([]);
const cameraGridRef = ref<InstanceType<typeof CameraGrid> & { getCurrentLayout?: () => VideoMonitoringItem[] }>();
const fullscreenStageRef = ref<InstanceType<typeof FullscreenCameraStage> & { getCurrentLayout?: () => VideoMonitoringItem[] }>();
const fullscreenRef = ref<HTMLElement>();
const layoutBaselineSignature = ref('');
const draftDashboardIdPrefix = 'draft-dashboard-';

const {
	gridDropRef,
	state: editorState,
	gridGhostStyle,
	gridOverlayStyle,
	dragCursorStyle,
	isEquipmentAdded,
	applyTemplateLayout,
	clearTemplatePlaceholders,
	setSelectedSlotForAdd,
	addCameraFromPanel,
	removeCameraLocal,
	updateCameraLayout,
	startManualDrag,
	onGridDragOver,
	onGridDragLeave,
	onGridDrop,
} = useCameraDashboardEditor(equipments);

const info = reactive({
	name: '',
	remark: '',
	tags: [] as string[],
	templateId: '',
});

const dashboardInfoSnapshot = reactive({
	name: '',
	remark: '',
	tags: [] as string[],
});

const originalInfo = reactive({
	name: '',
	remark: '',
	tagsSignature: '',
});

const state = reactive({
	dashboardQuery: '',
	dashboardFilter: 'all',
	dashboardFilterTags: [] as string[],
	equipmentQuery: '',
	equipmentTypeFilter: '',
	tagInput: '',
	dashboardTags: [] as string[],
	selectedDashboardId: '',
	selectedCameraId: '',
	focusedFullscreenCameraId: '',
	focusedFullscreenMode: 'view' as 'view' | 'ptz' | 'playback',
	fullscreen: false,
	layoutDirty: false,
	skipUnsavedGuard: false,
	loadingDashboards: false,
	loadingDashboard: false,
	equipmentLoading: false,
	saving: false,
	savingInfo: false,
	dashboardInfoDialogVisible: false,
	dashboardInfoDialogMode: 'edit' as 'create' | 'edit',
	dashboardInfoDialogIntent: 'info' as 'info' | 'save-layout',
	dashboardInfoDialogSaved: false,
	dashboards: [] as DashboardCCTVOutput[],
	templates: [] as ICctvDashboardTemplate[],
	activeDashboard: null as DashboardCCTVDetailOutput | null,
	cameraInfoDialogVisible: false,
	cameraInfoData: null as VideoMonitoringItem | null,
});

type TemplateSelectOption = {
	label: string;
	value: string;
	data: ICctvDashboardTemplate | null;
};

const toSafeString = (value: unknown, fallback = '') => {
	try {
		if (value === undefined || value === null) return fallback;
		if (typeof value === 'string') return value;
		if (typeof value === 'number' || typeof value === 'boolean') return String(value);
		if (value instanceof Date) return value.toISOString();
		if (typeof value === 'object') {
			const source = value as Record<string, unknown>;
			const primitiveValue = source.id ?? source.ID ?? source.Id ?? source.value ?? source.Value ?? source.code ?? source.Code ?? source.name ?? source.Name ?? source.url ?? source.Url;
			if (primitiveValue !== value) return toSafeString(primitiveValue, fallback);
		}
		return fallback;
	} catch {
		return fallback;
	}
};

const toSafeNumber = (value: unknown, fallback = 0) => {
	try {
		const nextValue = Number(value);
		return Number.isFinite(nextValue) ? nextValue : fallback;
	} catch {
		return fallback;
	}
};

const getErrorMessage = (error: unknown, fallback = $t('lz.message.cctvDashboard.genericError')) => {
	if (error instanceof Error) return error.message || fallback;
	const message = toSafeString((error as { message?: unknown })?.message);
	return message || toSafeString(error, fallback);
};

const openExternalUrl = (url: string, fileName?: string) => {
	const anchor = document.createElement('a');
	anchor.href = url;
	anchor.rel = 'noopener noreferrer';
	if (fileName) {
		anchor.download = fileName;
	} else {
		anchor.target = '_blank';
	}
	document.body.appendChild(anchor);
	anchor.click();
	anchor.remove();
};

const showFullscreenMessage = (type: 'success' | 'warning' | 'error', message: string) => {
	ElMessage({
		type,
		message,
		customClass: 'cctv-fullscreen-message',
	});
};

const getTagsSignature = (tags: string[]) => tags.map((tag) => tag.trim()).filter(Boolean).sort().join('|');
const isDraftDashboardId = (id: string) => id.startsWith(draftDashboardIdPrefix);
const isDraftDashboard = (dashboard: DashboardCCTVOutput | DashboardCCTVDetailOutput | null | undefined) => Boolean(dashboard?.id && isDraftDashboardId(dashboard.id));

const getCamerasSignature = (cameras: VideoMonitoringItem[]) =>
	cameras
		.map((camera) =>
			[
				toSafeString(camera.id),
				toSafeString(getCameraSourceId(camera)),
				toSafeNumber(camera.x),
				toSafeNumber(camera.y),
				toSafeNumber(camera.w),
				toSafeNumber(camera.h),
				camera.isPlaceholder ? 1 : 0,
			].join(':')
		)
		.join('|');

const normalizeDashboardCameras = (cameras: VideoMonitoringItem[] | undefined): VideoMonitoringItem[] =>
	(cameras || []).map((camera, index) => {
		const rawStatus = toSafeString(camera?.status, 'offline');
		return normalizeCameraGridItem({
			...camera,
			id: toSafeString(camera?.id, `dashboard-camera-${index + 1}`),
			sourceCameraId: toSafeString(camera?.sourceCameraId),
			deviceId: toSafeString(camera?.deviceId),
			ip: toSafeString(camera?.ip),
			name: toSafeString(camera?.name, 'Camera'),
			location: toSafeString(camera?.location),
			status: (['online', 'warning', 'offline'].includes(rawStatus) ? rawStatus : 'offline') as VideoMonitoringItem['status'],
			aspect: toSafeString(camera?.aspect, '16:9'),
			videoUrl: toSafeString(camera?.videoUrl),
			x: toSafeNumber(camera?.x, 0),
			y: toSafeNumber(camera?.y, 0),
			w: toSafeNumber(camera?.w, 2),
			h: toSafeNumber(camera?.h, 2),
			isPlaceholder: Boolean(camera?.isPlaceholder),
		});
	});

const activeDashboard = computed(() => state.activeDashboard);

const filteredDashboards = computed(() => {
	const query = state.dashboardQuery.trim().toLowerCase();
	const filterTags = state.dashboardFilterTags;
	return state.dashboards
		.filter((dashboard) => {
			// Filter by tags
			if (filterTags.length > 0) {
				const dashboardTags = dashboard.tags || [];
				if (!filterTags.some((tag) => dashboardTags.includes(tag))) return false;
			}
			if (!query) return true;
			return `${dashboard.name} ${dashboard.remark} ${dashboard.tags?.join(' ')}`.toLowerCase().includes(query);
		})
		.sort((first, second) => second.updatedAt.localeCompare(first.updatedAt));
});

const filteredEquipments = computed(() => {
	const query = state.equipmentQuery.trim().toLowerCase();
	const typeFilter = state.equipmentTypeFilter;
	return equipments.value.filter((equipment) => {
		const id = getEquipmentId(equipment);
		if (!id) return false;
		if (typeFilter && equipment.equipmentTypeName !== typeFilter) return false;
		if (!query) return true;
		return [equipment.name, equipment.code, id, equipment.ip, equipment.location].map((value) => toSafeString(value).toLowerCase()).join(' ').includes(query);
	});
});

const availableEquipmentTypes = computed(() => {
	const types = new Set<string>();
	equipments.value.forEach((equipment) => {
		const typeName = equipment.equipmentTypeName;
		if (typeName) types.add(typeName);
	});
	return [...types].sort((a, b) => a.localeCompare(b));
});

const templateOptions = computed<TemplateSelectOption[]>(() => [
	{ label: 'Blank', value: '', data: null },
	...state.templates.map((template) => ({
		label: template.Name || template.ID,
		value: template.ID,
		data: template,
	})),
]);

const focusedFullscreenCamera = computed(() => {
	if (!state.focusedFullscreenCameraId) return undefined;
	return editorState.cameras.find((camera) => camera.id === state.focusedFullscreenCameraId && !camera.isPlaceholder);
});

const isDashboardNameDirty = computed(() => info.name.trim() !== originalInfo.name);
const dashboardInfoDialogTitle = computed(() =>
	$t(state.dashboardInfoDialogMode === 'create' ? 'lz.label.cctvDashboard.createTitle' : 'lz.label.cctvDashboard.editInfoTitle')
);

const availableDashboardTags = computed(() => {
	const tags = new Set<string>();
	state.dashboardTags.forEach((tag) => {
		const value = tag.trim();
		if (value) tags.add(value);
	});
	state.dashboards.forEach((dashboard) => {
		(dashboard.tags || []).forEach((tag) => {
			const value = tag.trim();
			if (value) tags.add(value);
		});
	});
	info.tags.forEach((tag) => {
		const value = tag.trim();
		if (value) tags.add(value);
	});
	return [...tags].sort((first, second) => first.localeCompare(second));
});

const isTemplateOption = (option: unknown): option is TemplateSelectOption & { data: ICctvDashboardTemplate } => {
	if (!option || typeof option !== 'object') return false;
	return Boolean((option as TemplateSelectOption).data);
};

const syncDashboardForm = (dashboard: DashboardCCTVOutput) => {
	info.name = dashboard.name;
	info.remark = dashboard.remark || '';
	info.tags = dashboard.tags ? [...dashboard.tags] : [];
	info.templateId = '';
	originalInfo.name = dashboard.name;
	originalInfo.remark = dashboard.remark || '';
	originalInfo.tagsSignature = getTagsSignature(dashboard.tags || []);
};

const snapshotDashboardInfo = () => {
	dashboardInfoSnapshot.name = info.name;
	dashboardInfoSnapshot.remark = info.remark;
	dashboardInfoSnapshot.tags = [...info.tags];
};

const restoreDashboardInfoSnapshot = () => {
	info.name = dashboardInfoSnapshot.name;
	info.remark = dashboardInfoSnapshot.remark;
	info.tags = [...dashboardInfoSnapshot.tags];
	state.tagInput = '';
};

const openDashboardInfoDialog = (mode: 'create' | 'edit' = 'edit', intent: 'info' | 'save-layout' = 'info') => {
	state.dashboardInfoDialogMode = mode;
	state.dashboardInfoDialogIntent = intent;
	state.dashboardInfoDialogSaved = false;
	snapshotDashboardInfo();
	state.dashboardInfoDialogVisible = true;
};

const handleCancelDashboardInfoDialog = () => {
	restoreDashboardInfoSnapshot();
	state.dashboardInfoDialogVisible = false;
};

const handleDashboardInfoDialogClosed = () => {
	if (!state.dashboardInfoDialogSaved) restoreDashboardInfoSnapshot();
	state.dashboardInfoDialogSaved = false;
	state.dashboardInfoDialogIntent = 'info';
	state.tagInput = '';
};

const markLayoutClean = () => {
	layoutBaselineSignature.value = getCamerasSignature(editorState.cameras);
	state.layoutDirty = false;
};

const resetDashboardViewState = () => {
	editorState.editMode = false;
	editorState.selectedSlotId = '';
	editorState.draggingCameraId = '';
	editorState.draggingOverGrid = false;
	editorState.dragGhost = null;
	editorState.manualDragging = false;
	state.selectedCameraId = '';
	state.focusedFullscreenCameraId = '';
	state.focusedFullscreenMode = 'view';
	state.fullscreen = false;
	state.dashboardInfoDialogVisible = false;
	state.dashboardInfoDialogSaved = false;
	state.dashboardInfoDialogIntent = 'info';
	state.tagInput = '';
};

const syncGridLayout = () => {
	const layout = state.fullscreen ? fullscreenStageRef.value?.getCurrentLayout?.() : cameraGridRef.value?.getCurrentLayout?.();
	if (!layout) return editorState.cameras;
	updateCameraLayout(layout);
	return layout;
};

const confirmLeaveDirtyLayout = async () => {
	if (!state.layoutDirty) return true;
	try {
		await ElMessageBox.confirm($t('lz.message.cctvDashboard.unsavedLayoutConfirm'), $t('lz.label.cctvDashboard.unsavedChanges'), {
			type: 'warning',
			confirmButtonText: $t('lz.button.cctvDashboard.continue'),
			cancelButtonText: $t('lz.button.cctvDashboard.stay'),
		});
		return true;
	} catch {
		return false;
	}
};

const replaceBrowserQuerySilently = (mutate: (query: URLSearchParams) => void) => {
	const url = new URL(window.location.href);
	url.search = '';
	const currentHash = url.hash || `#${route.path || '/cctv/dashboard/'}`;
	const hashText = currentHash.startsWith('#') ? currentHash.slice(1) : currentHash;
	const queryIndex = hashText.indexOf('?');
	const hashPath = queryIndex >= 0 ? hashText.slice(0, queryIndex) : hashText;
	const hashQueryText = queryIndex >= 0 ? hashText.slice(queryIndex + 1) : '';
	const hashQuery = new URLSearchParams(hashQueryText);
	mutate(hashQuery);
	const nextHashQuery = hashQuery.toString();
	const nextHash = `#${hashPath}${nextHashQuery ? `?${nextHashQuery}` : ''}`;
	const nextUrl = `${url.pathname}${nextHash}`;
	window.history.replaceState(window.history.state, document.title, nextUrl);
};

const syncSelectedDashboardUrl = (dashboardId: string) => {
	replaceBrowserQuerySilently((query) => {
		query.set('id', dashboardId);
		query.delete('tagsViewName');
	});
};

const clearSelectedDashboardUrl = () => {
	replaceBrowserQuerySilently((query) => {
		query.delete('id');
		query.delete('tagsViewName');
	});
};

/* Dashboard list: begin ======================== */

const loadDashboards = async (options: { force?: boolean } = {}) => {
	if (!options.force && cctvDashboardStore.dashboards.length) {
		state.dashboards = cctvDashboardStore.dashboards;
		return;
	}
	state.loadingDashboards = true;
	try {
		const dashboards = await loadCctvDashboardList();
		const draftDashboard = state.activeDashboard && isDraftDashboard(state.activeDashboard) ? state.activeDashboard : null;
		state.dashboards = draftDashboard ? [draftDashboard, ...dashboards] : dashboards;
		cctvDashboardStore.setDashboards(state.dashboards);
	} finally {
		state.loadingDashboards = false;
	}
};

const loadDashboardTags = async (options: { force?: boolean } = {}) => {
	if (!options.force && cctvDashboardStore.dashboardTags.length) {
		state.dashboardTags = cctvDashboardStore.dashboardTags;
		return;
	}
	state.dashboardTags = await loadCctvDashboardTags();
	cctvDashboardStore.setDashboardTags(state.dashboardTags);
};

const handleSelectDashboard = async (id: string, options: { skipConfirm?: boolean; syncRoute?: boolean; force?: boolean } = {}) => {
	if (!id || (id === state.selectedDashboardId && !options.force)) return;
	if (!options.skipConfirm && !(await confirmLeaveDirtyLayout())) return;
	state.loadingDashboard = true;
	resetDashboardViewState();
	try {
		const cachedDashboard = !options.force ? cctvDashboardStore.dashboardDetails[id] : undefined;
		// Tái dùng danh sách thiết bị đã cache trong store, tránh gọi lại deviceview mỗi lần mở dashboard
		const cachedDevices = cctvDashboardStore.equipments.length ? cctvDashboardStore.equipments : undefined;
		const dashboard = cachedDashboard || (await getCctvDashboardDetail(id, cachedDevices));
		const cameras = normalizeDashboardCameras(dashboard.cameras);
		state.selectedDashboardId = dashboard.id;
		state.activeDashboard = { ...dashboard, cameras };
		editorState.cameras = cameras;
		resetDashboardViewState();
		syncDashboardForm(dashboard);
		cctvDashboardStore.setDashboardDetail({ ...dashboard, cameras });
		// Cập nhật title trình duyệt
		const appName = window.__env__?.VITE_APP_NAME || '';
		const dashboardTitle = dashboard.name || 'CCTV Dashboard';
		document.title = appName ? `${appName} | ${dashboardTitle}` : dashboardTitle;
		await nextTick();
		markLayoutClean();
		if (options.syncRoute !== false) syncSelectedDashboardUrl(dashboard.id);
	} catch (error) {
		ElMessage.error(getErrorMessage(error, $t('lz.message.cctvDashboard.loadDashboardFailed')));
	} finally {
		state.loadingDashboard = false;
	}
};

const clearSelectedDashboard = () => {
	state.selectedDashboardId = '';
	state.activeDashboard = null;
	editorState.cameras = [];
	info.name = '';
	info.remark = '';
	info.tags = [];
	info.templateId = '';
	resetDashboardViewState();
	markLayoutClean();
	clearSelectedDashboardUrl();
};

const handleRefreshDashboards = async () => {
	if (state.selectedDashboardId && !(await confirmLeaveDirtyLayout())) return;
	await Promise.all([loadDashboards({ force: true }), loadDashboardTags({ force: true })]);
	if (state.selectedDashboardId && !isDraftDashboardId(state.selectedDashboardId)) {
		await handleSelectDashboard(state.selectedDashboardId, { skipConfirm: true, syncRoute: false, force: true });
	}
	ElMessage.success($t('lz.message.cctvDashboard.refreshDashboardSuccess'));
};

const handleAddDashboard = async () => {
	if (state.saving) return;
	if (!(await confirmLeaveDirtyLayout())) return;
	const draftId = `${draftDashboardIdPrefix}${Date.now()}`;
	const createdAt = new Date().toISOString();
	const draftDashboard: DashboardCCTVDetailOutput = {
		id: draftId,
		name: '',
		remark: '',
		tags: [],
		createdAt,
		updatedAt: createdAt,
		cameraCount: 0,
		cameras: [],
	};
	state.selectedDashboardId = draftId;
	state.activeDashboard = draftDashboard;
	state.dashboards = [draftDashboard, ...state.dashboards.filter((dashboard) => !isDraftDashboardId(dashboard.id))];
	cctvDashboardStore.setDashboards(state.dashboards);
	cctvDashboardStore.setDashboardDetail(draftDashboard);
	editorState.cameras = [];
	resetDashboardViewState();
	syncDashboardForm(draftDashboard);
	await nextTick();
	markLayoutClean();
	clearSelectedDashboardUrl();
	ElMessage.info($t('lz.message.cctvDashboard.createDashboardHint'));
};

const handleDuplicateDashboard = async (dashboard: DashboardCCTVOutput) => {
	if (isDraftDashboard(dashboard)) {
		ElMessage.warning($t('lz.message.cctvDashboard.saveBeforeDuplicate'));
		return;
	}
	let duplicateName: string;
	try {
		const { value } = await ElMessageBox.prompt(
			$t('lz.message.cctvDashboard.confirmDuplicate'),
			$t('lz.button.cctvDashboard.duplicate'),
			{
				confirmButtonText: $t('lz.button.base.confirm'),
				cancelButtonText: $t('lz.button.base.cancel'),
				inputValue: `${dashboard.name} (Copy)`,
				inputPlaceholder: $t('lz.label.cctvDashboard.dashboardNamePlaceholder'),
				inputValidator: (val: string) => !!val?.trim() || $t('lz.message.cctvDashboard.duplicateNameRequired'),
			}
		);
		duplicateName = value.trim();
	} catch {
		return;
	}
	state.saving = true;
	try {
		const duplicated = await duplicateCctvDashboard(dashboard, duplicateName);
		const sourceCameras =
			dashboard.id === state.activeDashboard?.id
				? syncGridLayout().filter((camera) => !camera.isPlaceholder)
				: normalizeDashboardCameras((await getCctvDashboardDetail(dashboard.id, cctvDashboardStore.equipments.length ? cctvDashboardStore.equipments : undefined)).cameras).filter((camera) => !camera.isPlaceholder);
		await saveCctvDashboardItems(duplicated.id, sourceCameras, { omitIds: true });
		cctvDashboardStore.removeDashboardDetail(duplicated.id);
		await loadDashboards({ force: true });
		await handleSelectDashboard(duplicated.id, { skipConfirm: true, force: true });
		ElMessage.success($t('lz.message.cctvDashboard.duplicateSuccess', { name: duplicated.name }));
	} catch (error) {
		ElMessage.error(getErrorMessage(error));
	} finally {
		state.saving = false;
	}
};

const handleDeleteDashboard = async (dashboard: DashboardCCTVOutput) => {
	if (isDraftDashboard(dashboard)) {
		cctvDashboardStore.removeDashboardDetail(dashboard.id);
		state.dashboards = state.dashboards.filter((item) => item.id !== dashboard.id);
		cctvDashboardStore.setDashboards(state.dashboards);
		if (dashboard.id === state.selectedDashboardId) clearSelectedDashboard();
		ElMessage.success($t('lz.message.cctvDashboard.cancelDraftSuccess'));
		return;
	}
	try {
		await ElMessageBox.confirm($t('lz.message.cctvDashboard.confirmDelete', { name: dashboard.name }), $t('lz.button.cctvDashboard.deleteDashboard'), {
			type: 'warning',
			confirmButtonText: $t('lz.button.base.delete'),
			cancelButtonText: $t('lz.button.base.cancel'),
		});
	} catch {
		return;
	}
	state.saving = true;
	try {
		await deleteCctvDashboard(dashboard.id);
		cctvDashboardStore.removeDashboardDetail(dashboard.id);
		await loadDashboards({ force: true });
		if (dashboard.id === state.selectedDashboardId) clearSelectedDashboard();
		ElMessage.success($t('lz.message.cctvDashboard.deleteSuccess'));
	} catch (error) {
		ElMessage.error(getErrorMessage(error));
	} finally {
		state.saving = false;
	}
};

const handleDashboardRowCommand = (command: string | number | object, dashboard: DashboardCCTVOutput) => {
	if (command === 'duplicate') handleDuplicateDashboard(dashboard);
	if (command === 'delete') handleDeleteDashboard(dashboard);
};

/* Dashboard list: end ======================== */

/* Dashboard detail: begin ======================== */

type TagSuggestion = {
	value: string;
};

const ensureDashboardPersisted = async (cameras: VideoMonitoringItem[] = editorState.cameras) => {
	const dashboard = state.activeDashboard;
	if (!dashboard) return null;
	const name = info.name.trim();
	if (!name) {
		openDashboardInfoDialog('create');
		return null;
	}
	if (!isDraftDashboard(dashboard)) return dashboard;
	const created = await createCctvDashboard(name, info.remark.trim(), [...info.tags]);
	const createdDetail: DashboardCCTVDetailOutput = {
		...created,
		cameraCount: cameras.filter((camera) => !camera.isPlaceholder).length,
		cameras,
	};
	state.selectedDashboardId = created.id;
	state.activeDashboard = createdDetail;
	state.dashboards = [created, ...state.dashboards.filter((item) => item.id !== dashboard.id)];
	cctvDashboardStore.removeDashboardDetail(dashboard.id);
	cctvDashboardStore.setDashboardDetail(createdDetail);
	cctvDashboardStore.setDashboards(state.dashboards);
	syncDashboardForm(created);
	syncSelectedDashboardUrl(created.id);
	return createdDetail;
};

const queryDashboardTagSuggestions = (query: string, callback: (items: TagSuggestion[]) => void) => {
	const normalizedQuery = query.trim().toLowerCase();
	const selectedTags = new Set(info.tags.map((tag) => tag.toLowerCase()));
	const options = availableDashboardTags.value
		.filter((tag) => !selectedTags.has(tag.toLowerCase()))
		.filter((tag) => !normalizedQuery || tag.toLowerCase().includes(normalizedQuery))
		.map((tag) => ({ value: tag }));
	callback(options);
};

const commitTagInput = () => {
	const value = state.tagInput.trim();
	if (value && !info.tags.includes(value)) {
		info.tags.push(value);
	}
	state.tagInput = '';
};

const handleTagSuggestionSelect = (item: TagSuggestion) => {
	state.tagInput = item.value;
	commitTagInput();
};

const onTagKeydown = (event: KeyboardEvent) => {
	if (event.key === 'Enter' || event.key === ',') {
		event.preventDefault();
		commitTagInput();
	}
};

const removeTag = (tag: string) => {
	info.tags = info.tags.filter((item) => item !== tag);
};

const handleSaveDashboardInfo = async () => {
	const dashboard = state.activeDashboard;
	if (!dashboard || state.savingInfo) return;
	if (!info.name.trim()) {
		openDashboardInfoDialog(isDraftDashboard(dashboard) ? 'create' : 'edit');
		return;
	}
	state.savingInfo = true;
	try {
		if (isDraftDashboard(dashboard)) {
			openDashboardInfoDialog('create');
			return;
		}
		const updated = await updateCctvDashboard({
			...dashboard,
			name: info.name.trim(),
			remark: info.remark.trim(),
			tags: [...info.tags],
		});
		state.activeDashboard = { ...dashboard, ...updated, cameras: editorState.cameras };
		cctvDashboardStore.setDashboardDetail({ ...dashboard, ...updated, cameras: editorState.cameras });
		syncDashboardForm(updated);
		await loadDashboards({ force: true });
		ElMessage.success($t('lz.message.cctvDashboard.saveInfoSuccess'));
	} catch (error) {
		ElMessage.error(getErrorMessage(error));
	} finally {
		state.savingInfo = false;
	}
};

const handleCancelDashboardInfo = () => {
	info.name = originalInfo.name;
};

const handleSaveDashboardInfoDialog = async () => {
	const dashboard = state.activeDashboard;
	if (!dashboard || state.savingInfo) return;
	commitTagInput();
	if (!info.name.trim()) {
		ElMessage.warning($t('lz.message.cctvDashboard.dashboardNameRequired'));
		return;
	}
	const shouldSaveLayout = state.dashboardInfoDialogIntent === 'save-layout';
	state.savingInfo = true;
	try {
		if (isDraftDashboard(dashboard)) {
			const syncedCameras = shouldSaveLayout ? syncGridLayout() : editorState.cameras;
			const created = await ensureDashboardPersisted(syncedCameras);
			if (!created) return;
			if (shouldSaveLayout) {
				const realCameras = syncedCameras.filter((camera) => !camera.isPlaceholder);
				await saveCctvDashboardItems(created.id, realCameras);
				state.activeDashboard = { ...created, cameras: syncedCameras };
				cctvDashboardStore.setDashboardDetail({ ...created, cameras: syncedCameras });
				markLayoutClean();
				editorState.editMode = false;
			}
			await Promise.all([loadDashboards({ force: true }), loadDashboardTags({ force: true })]);
			ElMessage.success($t(shouldSaveLayout ? 'lz.message.cctvDashboard.createAndSaveSuccess' : 'lz.message.cctvDashboard.createSuccess'));
		} else {
			const updated = await updateCctvDashboard({
				...dashboard,
				name: info.name.trim(),
				remark: info.remark.trim(),
				tags: [...info.tags],
			});
			state.activeDashboard = { ...dashboard, ...updated, cameras: editorState.cameras };
			cctvDashboardStore.setDashboardDetail({ ...dashboard, ...updated, cameras: editorState.cameras });
			syncDashboardForm(updated);
			await Promise.all([loadDashboards({ force: true }), loadDashboardTags({ force: true })]);
			ElMessage.success($t('lz.message.cctvDashboard.saveInfoSuccess'));
		}
		state.dashboardInfoDialogSaved = true;
		state.dashboardInfoDialogVisible = false;
	} catch (error) {
		ElMessage.error(getErrorMessage(error));
	} finally {
		state.savingInfo = false;
	}
};

let suppressTemplateChange = false;

const handleTemplateChange = (value: string | number) => {
	// Bỏ qua khi đang tự reset selector về rỗng (tránh vô tình clear placeholder vừa dựng).
	if (suppressTemplateChange) return;
	const templateId = toSafeString(value);
	if (!templateId) {
		clearTemplatePlaceholders();
		state.layoutDirty = getCamerasSignature(editorState.cameras) !== layoutBaselineSignature.value;
		return;
	}
	const template = state.templates.find((item) => item.ID === templateId);
	if (!template) return;
	applyTemplateLayout(template);
	state.layoutDirty = true;
	editorState.editMode = true;
	// Reset selector về rỗng để có thể CHỌN LẠI cùng một template (el-select không phát sự kiện
	// khi chọn lại đúng option đang chọn). Chọn lại sẽ dựng lại các ô trống, giữ camera ở vị trí cũ.
	suppressTemplateChange = true;
	nextTick(() => {
		info.templateId = '';
		nextTick(() => {
			suppressTemplateChange = false;
		});
	});
};

const handleSaveLayout = async () => {
	const dashboard = state.activeDashboard;
	if (!dashboard || state.saving) return false;
	if (isDraftDashboard(dashboard)) {
		openDashboardInfoDialog('create', 'save-layout');
		return false;
	}
	if (!info.name.trim()) {
		openDashboardInfoDialog('edit', 'save-layout');
		return false;
	}
	state.saving = true;
	try {
		const syncedCameras = syncGridLayout();
		const persistedDashboard = await ensureDashboardPersisted(syncedCameras);
		if (!persistedDashboard) {
			return false;
		}
		const updated = isDraftDashboard(dashboard)
			? persistedDashboard
			: await updateCctvDashboard({
				...dashboard,
				name: info.name.trim(),
				remark: info.remark.trim(),
				tags: [...info.tags],
			});
		const realCameras = syncedCameras.filter((camera) => !camera.isPlaceholder);
		await saveCctvDashboardItems(updated.id, realCameras);
		state.activeDashboard = { ...updated, cameras: syncedCameras };
		cctvDashboardStore.setDashboardDetail({ ...updated, cameras: syncedCameras });
		syncDashboardForm(updated);
		markLayoutClean();
		await loadDashboards({ force: true });
		ElMessage.success($t('lz.message.cctvDashboard.saveDashboardSuccess'));
		return true;
	} catch (error) {
		ElMessage.error(getErrorMessage(error));
		return false;
	} finally {
		state.saving = false;
	}
};

const handleToggleEditMode = async () => {
	if (!editorState.editMode) {
		editorState.editMode = true;
		return;
	}
	const saved = await handleSaveLayout();
	if (saved) editorState.editMode = false;
};

const handleDetailMoreCommand = (command: string | number | object) => {
	const dashboard = state.activeDashboard;
	if (!dashboard) return;
	if (command === 'edit-info') openDashboardInfoDialog(isDraftDashboard(dashboard) ? 'create' : 'edit');
	if (command === 'duplicate') handleDuplicateDashboard(dashboard);
	if (command === 'export-template') handleExportDashboardTemplate();
	if (command === 'delete') handleDeleteDashboard(dashboard);
};

const handleExportDashboardTemplate = async () => {
	const dashboard = state.activeDashboard;
	if (!dashboard) return;
	if (isDraftDashboard(dashboard)) {
		ElMessage.warning($t('lz.message.cctvDashboard.saveBeforeExportTemplate'));
		return;
	}

	let templateName = '';
	try {
		const { value } = await ElMessageBox.prompt($t('lz.message.cctvDashboard.exportTemplatePrompt'), $t('lz.button.cctvDashboard.exportTemplate'), {
			confirmButtonText: $t('lz.button.base.export'),
			cancelButtonText: $t('lz.button.base.cancel'),
			inputValue: $t('lz.label.cctvDashboard.templateDefaultName', { name: info.name.trim() || dashboard.name }),
			inputPlaceholder: $t('lz.label.cctvTemplate.templateNamePlaceholder'),
			inputValidator: (value) => Boolean(value.trim()) || $t('lz.message.cctvTemplate.templateNameRequired'),
		});
		templateName = value.trim();
	} catch (error) {
		if (error === 'cancel' || error === 'close') return;
		ElMessage.error(getErrorMessage(error));
		return;
	}

	try {
		const realCameras = syncGridLayout().filter((camera) => !camera.isPlaceholder);
		await createCctvDashboardTemplate(templateName, info.remark, realCameras);
		state.templates = await loadCctvDashboardTemplates();
		ElMessage.success($t('lz.message.cctvDashboard.exportTemplateSuccess', { count: realCameras.length, name: templateName }));
	} catch (error) {
		ElMessage.error(getErrorMessage(error));
	}
};

/* Dashboard detail: end ======================== */

/* Camera editor: begin ======================== */

const loadEquipments = async (options: { force?: boolean } = {}) => {
	if (!options.force && cctvDashboardStore.equipments.length) {
		equipments.value = cctvDashboardStore.equipments;
		return;
	}
	state.equipmentLoading = true;
	try {
		equipments.value = await loadCctvEquipments();
		cctvDashboardStore.setEquipments(equipments.value);
	} finally {
		state.equipmentLoading = false;
	}
};

const handleRefreshEquipments = async () => {
	await loadEquipments({ force: true });
	ElMessage.success($t('lz.message.cctvDashboard.refreshCameraListSuccess'));
};

const loadTemplates = async () => {
	state.templates = await loadCctvDashboardTemplates();
};

const ensureLayoutEditMode = () => {
	if (!editorState.editMode) editorState.editMode = true;
};

const handleStartManualDrag = (equipmentId: string, event: MouseEvent) => {
	if (!state.activeDashboard) {
		ElMessage.warning($t('lz.message.cctvDashboard.selectDashboardFirst'));
		return;
	}
	ensureLayoutEditMode();
	startManualDrag(equipmentId, event);
};

const handleAddCameraFromPanel = (equipmentId: string) => {
	if (!state.activeDashboard) {
		ElMessage.warning($t('lz.message.cctvDashboard.selectDashboardFirst'));
		return;
	}
	ensureLayoutEditMode();
	addCameraFromPanel(equipmentId);
};

const handleRemoveCamera = async (cameraId: string) => {
	const camera = editorState.cameras.find((item) => item.id === cameraId);
	if (!camera) return;
	try {
		await ElMessageBox.confirm(
			camera.isPlaceholder ? $t('lz.message.cctvDashboard.confirmRemovePlaceholder') : $t('lz.message.cctvDashboard.confirmRemoveCamera', { name: camera.name || cameraId }),
			camera.isPlaceholder ? $t('lz.button.cctvDashboard.removePlaceholder') : $t('lz.button.cctvDashboard.removeCamera'),
			{
				type: 'warning',
				confirmButtonText: $t('lz.button.base.delete'),
				cancelButtonText: $t('lz.button.base.cancel'),
			}
		);
		removeCameraLocal(cameraId);
		state.layoutDirty = true;
		editorState.editMode = true;
		if (state.focusedFullscreenCameraId === cameraId) clearFullscreenFocus();
		if (state.selectedCameraId === cameraId) state.selectedCameraId = '';
	} catch (error) {
		if (error !== 'cancel' && error !== 'close') ElMessage.error(getErrorMessage(error));
	}
};

const handleSelectCamera = (id: string) => {
	const camera = editorState.cameras.find((item) => item.id === id && !item.isPlaceholder);
	if (camera) state.selectedCameraId = id;
};

const handleOpenCameraDetail = (id: string) => {
	const camera = editorState.cameras.find((item) => item.id === id && !item.isPlaceholder);
	if (camera) {
		state.selectedCameraId = id;
		state.cameraInfoDialogVisible = true;
		state.cameraInfoData = camera;
	}
};

/* Camera editor: end ======================== */

/* Fullscreen: begin ======================== */

const enterFullscreen = async () => {
	if (!state.activeDashboard) return;
	state.fullscreen = true;
	await nextTick();
	try {
		await fullscreenRef.value?.requestFullscreen?.();
	} catch {
		// CSS fixed fallback keeps fullscreen usable when browser fullscreen is blocked.
	}
};

const exitFullscreen = () => {
	state.fullscreen = false;
	state.focusedFullscreenCameraId = '';
	state.focusedFullscreenMode = 'view';
};

const cleanupFullscreenState = () => {
	state.fullscreen = false;
	state.focusedFullscreenCameraId = '';
	state.focusedFullscreenMode = 'view';
	if (document.fullscreenElement) document.exitFullscreen().catch(() => undefined);
};

const onFullscreenChange = () => {
	if (!document.fullscreenElement && state.fullscreen) exitFullscreen();
};

const handleFocusFullscreenCamera = (id: string) => {
	if (!state.fullscreen) return;
	const camera = editorState.cameras.find((item) => item.id === id && !item.isPlaceholder);
	if (!camera) return;
	state.focusedFullscreenCameraId = id;
	state.focusedFullscreenMode = 'view';
};

const handleReplaceFullscreenCamera = (id: string) => {
	if (!state.fullscreen) return;
	const camera = editorState.cameras.find((item) => item.id === id && !item.isPlaceholder);
	if (!camera) return;
	state.focusedFullscreenCameraId = id;
};

const handleOpenFullscreenCameraSettings = (id: string) => {
	if (!state.fullscreen) return;
	const camera = editorState.cameras.find((item) => item.id === id && !item.isPlaceholder);
	if (!camera) return;
	state.focusedFullscreenCameraId = id;
	state.focusedFullscreenMode = 'ptz';
};

const handleSetFullscreenFocusMode = (mode: 'view' | 'ptz' | 'playback') => {
	state.focusedFullscreenMode = mode;
};

const handleFullscreenCameraInfo = (camera: VideoMonitoringItem) => {
	ElMessageBox.alert(
		[
			`${$t('lz.entity.base.name')}: ${camera.name || '--'}`,
			`DeviceId: ${camera.deviceId || '--'}`,
			`IP: ${camera.ip || '--'}`,
			`${$t('lz.label.base.location')}: ${camera.location || '--'}`,
			`${$t('lz.entity.base.status')}: ${camera.status || '--'}`,
		].join('\n'),
		$t('lz.label.cctvDashboard.cameraInfo'),
		{
			confirmButtonText: $t('lz.button.base.close'),
			customClass: 'cctv-camera-info-dialog',
			modalClass: 'cctv-camera-info-modal',
			zIndex: 10060,
		}
	);
};

const handleFullscreenCameraScreenshot = async (camera: VideoMonitoringItem) => {
	if (!camera.deviceId) {
		showFullscreenMessage('warning', $t('lz.message.cctvDashboard.cameraDeviceIdNotFound'));
		return;
	}
	try {
		const url = await getCctvSnapshotUrl(camera, {
			size: '1920x1080',
			format: 'jpg',
		});
		saveAs(url, `snapshot-${new Date().toLocaleString('en-GB')}-${camera.name}-${camera.ip}.jpg`);
		showFullscreenMessage('success', $t('lz.message.cctvDashboard.snapshotSuccess'));
	} catch (error) {
		showFullscreenMessage('error', getErrorMessage(error, $t('lz.message.cctvDashboard.snapshotFailed')));
	}
};

const handleFullscreenCameraAction = (action: 'ptz' | 'playback' | 'screenshot' | 'info', id: string) => {
	const camera = editorState.cameras.find((item) => item.id === id && !item.isPlaceholder);
	if (!camera) return;
	if (action === 'info') {
		handleFullscreenCameraInfo(camera);
		return;
	}
	if (action === 'screenshot') {
		// Snapshot cần thiết bị online + Device ID + URL luồng
		if (!canUseLiveCameraControls(camera)) return;
		handleFullscreenCameraScreenshot(camera);
		return;
	}
	// PTZ cần điều khiển live; Playback chỉ cần Device ID
	if (action === 'ptz' && !canUseLiveCameraControls(camera)) return;
	if (action === 'playback' && !canUseCameraPlayback(camera)) return;
	state.focusedFullscreenCameraId = id;
	state.focusedFullscreenMode = action;
};

const clearFullscreenFocus = () => {
	state.focusedFullscreenCameraId = '';
	state.focusedFullscreenMode = 'view';
};

/* Fullscreen: end ======================== */

watch(
	() => route.query.id,
	(value) => {
		const id = toSafeString(value);
		if (!id) return;
		if (id !== state.selectedDashboardId) handleSelectDashboard(id, { skipConfirm: true, syncRoute: false });
	}
);

watch(
	() => getCamerasSignature(editorState.cameras),
	(value) => {
		if (state.loadingDashboard) return;
		state.layoutDirty = value !== layoutBaselineSignature.value;
	}
);

onMounted(async () => {
	// Set title mặc định
	document.title = `${window.__env__?.VITE_APP_NAME || ''} | CCTV Dashboard`.replace(/^\s*\|\s*/, '');
	// Áp dụng cấu hình lưới dashboard từ DB (SysOpConfig) trước khi render lưới
	await applyCctvDashboardConfigFromStore();
	await Promise.all([loadDashboards(), loadDashboardTags(), loadEquipments(), loadTemplates()]);
	document.addEventListener('fullscreenchange', onFullscreenChange);
	const routeDashboardId = toSafeString(route.query.id);
	if (routeDashboardId) await handleSelectDashboard(routeDashboardId, { skipConfirm: true, syncRoute: false });
});

onBeforeUnmount(() => {
	document.removeEventListener('fullscreenchange', onFullscreenChange);
	cleanupFullscreenState();
});

onBeforeRouteLeave(async () => {
	if (state.skipUnsavedGuard) {
		cleanupFullscreenState();
		return;
	}
	if (!(await confirmLeaveDirtyLayout())) return false;
	cleanupFullscreenState();
});
</script>

<style scoped lang="scss">
.cctv-home-page {
	height: 100vh;
	min-height: 0;
	background: var(--el-bg-color, #ffffff);
	color: var(--el-text-color-primary, #303133);
	overflow: hidden;
}

.cctv-home-layout {
	display: grid;
	grid-template-columns: minmax(280px, 2fr) minmax(0, 10fr);
	gap: 16px;
	height: 100%;
	min-height: 0;
	padding: 16px;
	box-sizing: border-box;
	overflow: hidden;
}

.cctv-sidebar {
	min-width: 0;
	min-height: 0;
	display: grid;
	grid-template-rows: minmax(220px, 0.95fr) minmax(260px, 1fr);
	gap: 12px;
	max-height: 100%;
	position: sticky;
	top: 16px;
}

.sidebar-section {
	min-width: 0;
	min-height: 0;
	background: var(--el-bg-color, #ffffff);
	border: 1px solid var(--el-border-color-light, #e4e7ec);
	border-radius: var(--el-border-radius-base, 8px);
	padding: 12px;
	display: grid;
	align-content: start;
	gap: 10px;
}

.sidebar-section-header {
	display: flex;
	align-items: flex-start;
	justify-content: space-between;
	gap: 10px;

	h3 {
		margin: 0;
		font-size: 16px;
		line-height: 22px;
	}

	p {
		margin: 2px 0 0;
		color: var(--el-text-color-secondary, #667085);
		font-size: 12px;
	}
}

.sidebar-header-actions {
	display: flex;
	align-items: center;
	gap: 6px;
	flex: 0 0 auto;
}

.dashboard-tools {
	display: flex;
	align-items: center;
	gap: 8px;

	.el-input {
		flex: 1;
	}

	.filter-btn {
		flex-shrink: 0;
	}
}

.dashboard-tag-filter {
	display: flex;
	flex-direction: column;
	gap: 8px;
}

.dashboard-tag-filter-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	font-size: 13px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

.dashboard-tag-filter-list {
	display: flex;
	flex-direction: column;
	gap: 4px;
	max-height: 200px;
	overflow-y: auto;
}

.dashboard-list {
	min-height: 0;
	overflow-y: auto;
	overflow-x: hidden;
	display: grid;
	align-content: start;
	gap: 8px;
	padding-right: 4px;
}

.dashboard-row {
	width: 100%;
	border: 1px solid var(--el-border-color-light, #e4e7ec);
	border-radius: var(--el-border-radius-base, 8px);
	background: var(--el-bg-color, #ffffff);
	display: grid;
	grid-template-columns: minmax(0, 1fr) auto;
	gap: 8px;
	align-items: center;
	text-align: left;
	padding: 10px;
	cursor: pointer;
	min-width: 0;
	min-height: 58px;
}

.dashboard-row :deep(.el-tooltip__trigger) {
	min-width: 0;
	display: block;
}

.dashboard-row .fa,
.detail-actions .fa {
	color: var(--el-text-color-regular);
}

.dashboard-row:hover .fa,
.detail-actions .el-button:hover .fa {
	color: var(--el-color-primary);
}

.dashboard-row:hover {
	border-color: var(--el-border-color, #98a2b3);
	background: var(--el-fill-color-light, #f9fafb);
}

.dashboard-row.is-active {
	border-color: var(--el-color-primary);
	background: var(--el-color-primary-light-9);
	box-shadow: inset 3px 0 0 var(--el-color-primary);
}

.dashboard-row-main {
	min-width: 0;
	display: grid;
	gap: 6px;

	strong,
	>span:not(.dashboard-row-tags) {
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	strong {
		color: var(--el-text-color-primary, #303133);
		font-size: 14px;
	}

	>span:not(.dashboard-row-tags) {
		color: var(--el-text-color-secondary, #909399);
		font-size: 12px;
	}
}

.dashboard-row-tags {
	display: flex;
	flex-wrap: wrap;
	gap: 4px;
	max-height: 24px;
	overflow: hidden;
}

.camera-section {
	overflow: hidden;
}

.camera-section :deep(.camera-list-panel) {
	max-height: none;
	height: 100%;
}

.cctv-workspace {
	min-width: 0;
	min-height: 0;
	display: grid;
	overflow: hidden;
}

.empty-guide,
.workspace-loading,
.dashboard-detail {
	min-width: 0;
	background: var(--el-bg-color, #ffffff);
	border: 1px solid var(--el-border-color-light, #e4e7ec);
	border-radius: var(--el-border-radius-base, 8px);
}

.empty-guide {
	display: grid;
	place-items: center;
	height: 100%;
	min-height: 0;
	padding: 24px;
}

.empty-guide-content {
	display: grid;
	justify-items: center;
	gap: 14px;
	max-width: 560px;
	text-align: center;

	:deep(.el-icon) {
		font-size: 44px;
		// color: var(--el-color-primary);
	}

	h2 {
		margin: 0;
		font-size: 28px;
		line-height: 36px;
	}

	p {
		margin: 0;
		color: var(--el-text-color-secondary, #909399);
		line-height: 22px;
	}
}

.empty-guide-steps {
	display: flex;
	flex-wrap: wrap;
	justify-content: center;
	gap: 8px;

	span {
		border: 1px solid var(--el-border-color-light, #e4e7ec);
		border-radius: 999px;
		padding: 6px 10px;
		color: var(--el-text-color-regular, #606266);
		font-size: 12px;
		background: var(--el-fill-color-light, #f9fafb);
	}
}

.workspace-loading {
	padding: 24px;
}

.dashboard-detail {
	height: 100%;
	min-height: 0;
	padding: 16px;
	display: flex;
	flex-direction: column;
	gap: 14px;
	overflow: hidden;
	box-sizing: border-box;
}

.detail-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 16px;
	flex: 0 0 auto;
	border-bottom: 1px solid var(--el-border-color-lighter, #eaecf0);
	padding-bottom: 14px;
}

.detail-copy {
	flex: 1 1 auto;
	min-width: 0;
}

.dashboard-field-row {
	min-width: 0;
}

.dashboard-field-row:first-child {
	display: flex;
	align-items: center;
	gap: 10px;
}

.dashboard-name-actions {
	flex: 0 0 auto;
	display: flex;
	align-items: center;
	gap: 6px;
}

.dashboard-name-input {
	width: 100%;
	min-width: 0;
	border: none;
	outline: none;
	background: transparent;
	padding: 0;
}

.dashboard-name-input {
	font-size: 28px;
	font-weight: 650;
	line-height: 36px;
	color: var(--el-text-color-primary, #303133);
}

.dashboard-name-input:focus {
	border-bottom: 2px solid var(--el-color-primary);
}

.dashboard-tags-editor {
	display: flex;
	align-items: center;
	flex-wrap: wrap;
	gap: 6px;
	flex: 1 1 260px;
	min-width: 0;

	:deep(.el-tag) {
		flex: 0 0 auto;
		max-width: 140px;
	}
}

.tag-input {
	flex: 0 0 108px;
	width: 108px;
	min-width: 96px;
	vertical-align: top;

	:deep(.el-input__wrapper) {
		min-height: 24px;
	}

	:deep(.el-input__inner) {
		font-size: 12px;
	}
}

.dashboard-info-form {
	display: grid;
	gap: 2px;
}

.dashboard-info-form .dashboard-tags-editor {
	min-height: 32px;
	padding: 4px 0;
}

.detail-actions {
	display: flex;
	align-items: center;
	justify-content: flex-end;
	gap: 8px;
	flex-wrap: wrap;
}

.template-select {
	width: 240px;
	min-width: 220px;
}

.grid-pane {
	min-width: 0;
	min-height: 0;
	flex: 1 1 0;
	display: flex;
	align-items: center;
	justify-content: center;
	position: relative;
	overflow: hidden;
	border: none;
	border-radius: 0;
	transition: border-color 0.2s ease, background-color 0.2s ease;
}

.grid-pane :deep(.camera-grid-wrap) {
	height: 100%;
	max-height: 100%;
}

.grid-pane.is-drag-over {
	border: 2px dashed var(--el-color-primary);
	border-radius: var(--el-border-radius-base, 10px);
	background: var(--el-color-primary-light-9, rgba(46, 144, 250, 0.06));
}

.grid-drop-overlay {
	position: absolute;
	z-index: 20;
}

.grid-drop-ghost {
	position: absolute;
	border: 2px dashed var(--el-color-primary);
	background: var(--el-color-primary-light-9, rgba(46, 144, 250, 0.12));
	border-radius: var(--el-border-radius-base, 8px);
	box-sizing: border-box;
	pointer-events: none;
	display: grid;
	place-items: center;
	color: var(--el-color-primary);

	:deep(.el-icon) {
		width: 52px;
		height: 52px;
		border-radius: 999px;
		display: inline-grid;
		place-items: center;
		background: var(--el-color-primary-light-9, #e0f2fe);
		box-shadow: inset 0 0 0 1px var(--el-color-primary-light-7, #bae6fd);
		font-size: 24px;
	}
}

.camera-drag-cursor {
	position: fixed;
	width: 56px;
	height: 56px;
	margin-left: -28px;
	margin-top: -28px;
	border: 1px solid var(--el-color-primary-light-7, #bae6fd);
	border-radius: 999px;
	background: var(--el-color-primary-light-9, #e0f2fe);
	box-sizing: border-box;
	pointer-events: none;
	z-index: 10000;
	display: grid;
	place-items: center;
	color: var(--el-color-primary);
	box-shadow: 0 10px 24px rgba(15, 23, 42, 0.18);

	:deep(.el-icon) {
		font-size: 24px;
	}
}

.is-fullscreen {
	display: block;
	position: fixed;
	inset: 0;
	z-index: 9999;
	min-height: 100vh;
	background: #000000;
}

:global(.cctv-camera-info-modal) {
	z-index: 10060 !important;
}

:global(.cctv-camera-info-dialog) {
	z-index: 10061 !important;
}

:global(.el-message.cctv-fullscreen-message) {
	z-index: 10090 !important;
}

:global(.el-message) {
	z-index: 10100 !important;
}

.w100 {
	width: 100%;
}

@media (max-width: 1024px) {
	.cctv-home-page {
		height: auto;
		min-height: 100vh;
		overflow: visible;
	}

	.cctv-home-layout {
		grid-template-columns: 1fr;
		height: auto;
		min-height: 100vh;
		overflow: visible;
	}

	.cctv-sidebar {
		position: static;
		max-height: none;
		grid-template-rows: none;
	}

	.detail-header {
		display: grid;
	}

	.detail-actions {
		justify-content: flex-start;
	}

	.dashboard-detail {
		height: auto;
		min-height: 620px;
		overflow: visible;
	}

	.grid-pane {
		min-height: 360px;
		align-items: center;
	}
}

.detail-actions {
	:deep(.el-button + .el-button) {
		margin-left: 0;
	}
}

.no-margin-right {
	margin-right: 0 !important;
}


</style>
