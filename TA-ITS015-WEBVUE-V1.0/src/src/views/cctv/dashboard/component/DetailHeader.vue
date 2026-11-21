<template>
	<div class="page-heading detail-heading">
		<div>
			<el-button plain type="primary" @click="$emit('back')">
				<el-icon><ele-ArrowLeft /></el-icon>
				{{ t('lz.button.cctvDashboard.backToDashboards') }}
			</el-button>
			<div class="heading-title">
				<div class="editable-meta-row">
					<template v-if="editingField !== 'name'">
						<h2>{{ dashboard?.name }}</h2>
						<el-button class="meta-edit-btn" text @click="$emit('start-meta-edit', 'name')">
							<el-icon><ele-Edit /></el-icon>
						</el-button>
					</template>
					<template v-else>
						<div class="inline-edit-group">
							<el-input :model-value="metaDraft.name" size="large" class="inline-edit-input"
								@update:model-value="updateMeta('name', String($event || ''))"
								@keyup.enter="$emit('save-meta-field', 'name')" />
							<div class="inline-edit-actions">
								<el-button class="meta-action-btn" type="primary" circle size="small"
									:loading="savingField === 'name'" @click="$emit('save-meta-field', 'name')">
									<el-icon><ele-Check /></el-icon>
								</el-button>
								<el-button class="meta-action-btn" circle size="small"
									@click="$emit('cancel-meta-edit')">
									<el-icon><ele-Close /></el-icon>
								</el-button>
							</div>
						</div>
					</template>
				</div>
				<div class="editable-meta-row description-row">
					<template v-if="editingField !== 'description'">
						<p>{{ dashboard?.description || t('lz.label.cctvDashboard.detailSubtitle') }}</p>
						<el-button class="meta-edit-btn" text @click="$emit('start-meta-edit', 'description')">
							<el-icon><ele-Edit /></el-icon>
						</el-button>
					</template>
					<template v-else>
						<div class="inline-edit-group description-edit-group">
							<el-input :model-value="metaDraft.description" type="textarea" :rows="3"
								class="inline-edit-input"
								@update:model-value="updateMeta('description', String($event || ''))"
								@keyup.ctrl.enter="$emit('save-meta-field', 'description')" />
							<div class="inline-edit-actions">
								<el-button class="meta-action-btn" type="primary" circle size="small"
									:loading="savingField === 'description'"
									@click="$emit('save-meta-field', 'description')">
									<el-icon><ele-Check /></el-icon>
								</el-button>
								<el-button class="meta-action-btn" circle size="small"
									@click="$emit('cancel-meta-edit')">
									<el-icon><ele-Close /></el-icon>
								</el-button>
							</div>
						</div>
					</template>
				</div>
			</div>
		</div>
		<div class="heading-actions">
			<el-button @click="$emit('open-add-camera')">
				<el-icon><ele-Plus /></el-icon>
				{{ t('lz.button.cctvDashboard.addCamera') }}
			</el-button>
			<el-button v-if="editMode" @click="$emit('cancel-edit-mode')">
				<el-icon><ele-Close /></el-icon>
				{{ t('lz.button.cctvDashboard.cancelEdit') }}
			</el-button>
			<el-button :type="editMode ? 'primary' : 'default'" @click="$emit('toggle-edit-mode')">
				<el-icon><ele-Edit /></el-icon>
				{{ editMode ? t('lz.button.cctvDashboard.saveLayout') : t('lz.button.cctvDashboard.editLayout') }}
			</el-button>
			<!-- <el-select :model-value="preset" size="default" placeholder="Chọn mẫu" @update:model-value="onPresetChange">
				<el-option label="Blank" value="" />
				<el-option v-for="template in templates" :key="template.ID" :label="template.Name || template.Code || template.ID" :value="template.ID" />
			</el-select> -->
			<CustomSelect v-model="presetModel" :options="templateOptions" size="default" class="template-select"
				popper-class="template-select-dropdown" :label="t('lz.label.cctvDashboard.template')">
				<template #option="{ option }">
					<CameraTemplatePreview v-if="isTemplateOption(option)" :template="option.data" size="sm" />
					<span v-else>{{ option.label }}</span>
				</template>
			</CustomSelect>
			<el-button @click="$emit('enter-fullscreen')" style="margin-left: 0px;">
				<el-icon><ele-FullScreen /></el-icon>
				{{ t('lz.button.cctvDashboard.fullscreen') }}
			</el-button>
			<el-dropdown trigger="click" @command="onMoreCommand">
				<el-button plain circle>
					<el-icon><ele-CaretBottom /></el-icon>
				</el-button>
				<template #dropdown>
					<el-dropdown-menu>
						<el-dropdown-item command="duplicate"> <el-icon><ele-CopyDocument /></el-icon> {{
							t('lz.button.cctvDashboard.duplicate') }}</el-dropdown-item>
						<el-dropdown-item command="create-template"> <el-icon><ele-DocumentAdd /></el-icon> {{
							t('lz.button.cctvDashboard.exportTemplate') }}</el-dropdown-item>
						<el-dropdown-item divided command="delete"> <el-icon><ele-Delete /></el-icon> {{
							t('lz.button.cctvDashboard.deleteDashboard') }}</el-dropdown-item>
					</el-dropdown-menu>
				</template>
			</el-dropdown>

		</div>
	</div>
</template>

<script setup lang="ts">
import type { DashboardCCTVOutput, ICctvDashboardTemplate } from '/@/views/cctv/dashboard/types';
import { useI18n } from 'vue-i18n';
import { computed, toRefs } from 'vue';
import CustomSelect from './CustomSelect.vue';
import CameraTemplatePreview from './CameraTemplatePreview.vue';

const { t } = useI18n();

type EditableField = 'name' | 'description' | 'slug' | '';

const props = defineProps<{
	dashboard?: DashboardCCTVOutput & { description?: string };
	editingField: EditableField;
	savingField: EditableField;
	metaDraft: { name: string; description: string; slug: string };
	editMode: boolean;
	preset: string;
	templates: ICctvDashboardTemplate[];
}>();

const { dashboard, editingField, savingField, metaDraft, editMode, templates } = toRefs(props);

type TemplateSelectOption = {
	label: string;
	value: string;
	data?: ICctvDashboardTemplate;
};

const emit = defineEmits<{
	(e: 'back'): void;
	(e: 'start-meta-edit', field: Exclude<EditableField, ''>): void;
	(e: 'save-meta-field', field: Exclude<EditableField, ''>): void;
	(e: 'cancel-meta-edit'): void;
	(e: 'update:meta-draft', value: { name: string; description: string; slug: string }): void;
	(e: 'open-add-camera'): void;
	(e: 'toggle-edit-mode'): void;
	(e: 'update:preset', value: string): void;
	(e: 'more-command', value: string): void;
	(e: 'enter-fullscreen'): void;
	(e: 'cancel-edit-mode'): void;
}>();

const updateMeta = (field: 'name' | 'description', value: string) => {
	emit('update:meta-draft', {
		...metaDraft.value,
		[field]: value,
	});
};

const presetModel = computed({
	get: () => props.preset,
	set: (value: string | number) => emit('update:preset', String(value || '')),
});

const templateOptions = computed<TemplateSelectOption[]>(() =>
	templates.value.map((template) => ({
		label: template.Name || template.ID,
		value: template.ID,
		data: template,
	}))
);

const isTemplateOption = (option: unknown): option is TemplateSelectOption & { data: ICctvDashboardTemplate } => {
	if (!option || typeof option !== 'object') return false;
	const candidate = option as TemplateSelectOption;
	return Boolean(candidate.data);
};

const onMoreCommand = (value: string | number | object) => {
	if (typeof value !== 'string') return;
	emit('more-command', value);
};
</script>

<style scoped lang="scss">
.page-heading {
	display: flex;
	align-items: flex-end;
	justify-content: space-between;
	gap: 24px;

	h2 {
		margin: 0;
		font-size: 32px;
		line-height: 38px;
	}

	p {
		margin: 4px 0 0;
		color: #667085;
	}
}

.detail-heading {
	align-items: flex-start;
}

.heading-title {
	margin-top: 1vh;
	display: grid;
	gap: 6px;
}

.editable-meta-row {
	display: flex;
	align-items: center;
	gap: 8px;
	min-width: 0;

	.meta-edit-btn {
		opacity: 0;
		transition: opacity 0.15s ease;
		padding: 0;
	}

	&:hover .meta-edit-btn,
	&:focus-within .meta-edit-btn {
		opacity: 1;
	}
}

.inline-edit-group {
	display: flex;
	align-items: center;
	gap: 8px;
	flex-wrap: wrap;
	width: 100%;
}

.inline-edit-actions {
	display: flex;
	align-items: center;
	gap: 6px;
	margin-left: auto;
	flex-shrink: 0;
}

.meta-action-btn {
	width: 26px;
	height: 26px;
	padding: 0;
	min-height: 26px;
	border-radius: 999px;
}

.meta-action-btn :deep(svg) {
	width: 12px;
	height: 12px;
}

.inline-edit-input {
	min-width: 260px;
	max-width: 100%;
	flex: 1 1 260px;
}

.description-row p {
	margin: 0;
	color: #667085;
	line-height: 1.5;
	max-width: 900px;
}

.description-edit-group .inline-edit-input {
	min-width: 360px;
}

.heading-actions {
	display: flex;
	align-items: center;
	justify-content: flex-end;
	gap: 12px;
	flex-wrap: wrap;

	:deep(.el-select) {
		width: 160px;
	}

	/* Element Plus tự thêm margin-left:12px giữa 2 el-button liền kề, khiến khoảng cách
	   giữa các nút (24px) không đồng đều với nút↔select (12px). Bỏ để 'gap' quyết định. */
	:deep(.el-button + .el-button) {
		margin-left: 0;
	}
}

.template-select {
	width: 280px;
	min-width: 280px;
	flex: 0 0 280px;
}

.template-select :deep(.el-select) {
	width: 100%;
}

@media (max-width: 760px) {
	.page-heading {
		align-items: flex-start;
		flex-direction: column;
	}

	.heading-actions,
	.heading-actions :deep(.el-select),
	.heading-actions :deep(.el-button) {
		width: 100%;
	}
}
</style>
