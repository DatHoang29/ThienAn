<template>
	<el-dialog :model-value="modelValue" width="760px" @update:model-value="onVisibleChange">
		<template #header>
			<div style="color: #fff">
				<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle">
					<ele-VideoCamera />
				</el-icon>
				<span> {{ title }} </span>
			</div>
		</template>
		<el-form label-position="top">
			<div class="camera-picker-toolbar">
				<el-input :model-value="equipmentQuery" :placeholder="t('lz.label.cctvDashboard.searchCameraByInfo')" clearable @update:model-value="onEquipmentQueryChange">
					<template #prefix>
						<el-icon><ele-Search /></el-icon>
					</template>
				</el-input>
				<el-select :model-value="equipmentStatusFilter" :placeholder="t('lz.entity.base.status')" @update:model-value="onEquipmentStatusFilterChange">
					<el-option :label="t('lz.label.cctvDashboard.all')" value="all" />
					<el-option :label="t('lz.label.cctvDashboard.online')" value="1" />
					<el-option :label="t('lz.label.cctvDashboard.offline')" value="0" />
				</el-select>
			</div>
			<el-table v-loading="equipmentLoading" :data="equipments" height="300" highlight-current-row @current-change="onSelectEquipment">
				<el-table-column width="56">
					<template #default="{ row }">
						<el-radio :model-value="selectedEquipmentId" :label="row.ID" @change="() => onSelectEquipment(row)">&nbsp;</el-radio>
					</template>
				</el-table-column>
				<el-table-column :label="t('lz.label.cctvDashboard.cameraName')" min-width="180">
					<template #default="{ row }">
						<div class="equipment-name">
							<strong>{{ row.Name || row.Code || row.ID }}</strong>
							<span>{{ row.Code || row.ID }}</span>
						</div>
					</template>
				</el-table-column>
				<el-table-column prop="Ip" label="IP" width="140" />
				<el-table-column :label="t('lz.entity.base.status')" width="140">
					<template #default="{ row }">
						<el-tag :type="row.Status === 1 ? 'success' : 'danger'" size="small">
							{{ row.Status === 1 ? t('lz.label.cctvDashboard.online') : t('lz.label.cctvDashboard.offline') }}
						</el-tag>
					</template>
				</el-table-column>
				<el-table-column prop="Remark" :label="t('lz.label.base.location')" min-width="150" />
			</el-table>
			<el-form-item v-if="mode === 'add'" :label="gridSizeLabel">
				<div class="grid-size-inputs">
					<el-input-number
						:model-value="newCameraW"
						:min="minGridW"
						:max="maxGridW"
						controls-position="right"
						@update:model-value="onNewCameraWChange"
					/>
				</div>
			</el-form-item>
		</el-form>
		<template #footer>
			<el-button @click="onVisibleChange(false)">{{ cancelText }}</el-button>
			<el-button type="primary" :loading="loading" :disabled="!selectedEquipmentId || loading" @click="$emit('submit')">
				{{ submitText }}
			</el-button>
		</template>
	</el-dialog>
</template>

<script setup lang="ts">
import { useI18n } from 'vue-i18n';

const { t } = useI18n();

type ITmsEquipment = {
	ID: string;
	Code?: string;
	Name?: string;
	Ip?: string;
	Status?: number;
	Remark?: string;
};

const props = defineProps<{
	modelValue: boolean;
	title: string;
	submitText: string;
	cancelText: string;
	gridSizeLabel: string;
	loading: boolean;
	equipmentQuery: string;
	equipmentStatusFilter: string;
	equipmentLoading: boolean;
	equipments: ITmsEquipment[];
	selectedEquipmentId: string;
	mode: 'add' | 'replace';
	newCameraW: number;
	minGridW: number;
	maxGridW: number;
}>();

const emit = defineEmits<{
	(e: 'update:modelValue', value: boolean): void;
	(e: 'update:selectedEquipmentId', value: string): void;
	(e: 'update:equipmentQuery', value: string): void;
	(e: 'update:equipmentStatusFilter', value: string): void;
	(e: 'update:newCameraW', value: number): void;
	(e: 'submit'): void;
}>();

const onVisibleChange = (value: boolean) => {
	emit('update:modelValue', value);
};

const onSelectEquipment = (equipment?: ITmsEquipment) => {
	emit('update:selectedEquipmentId', equipment?.ID || '');
};

const onEquipmentQueryChange = (value: string | number) => {
	emit('update:equipmentQuery', String(value || ''));
};

const onEquipmentStatusFilterChange = (value: string | number | boolean) => {
	emit('update:equipmentStatusFilter', String(value || 'all'));
};

const onNewCameraWChange = (value?: number) => {
	emit('update:newCameraW', value || props.minGridW);
};
</script>

<style scoped lang="scss">
.grid-size-inputs {
	display: grid;
	grid-template-columns: minmax(0, 1fr);
	gap: 12px;
	width: 100%;
}

.camera-picker-toolbar {
	display: grid;
	grid-template-columns: minmax(0, 1fr) 180px;
	gap: 12px;
	margin-bottom: 14px;
}

.equipment-name {
	display: grid;
	gap: 3px;

	strong {
		color: #101828;
		font-size: 14px;
	}

	span {
		color: #667085;
		font-size: 12px;
	}
}

@media (max-width: 760px) {
	.camera-picker-toolbar {
		grid-template-columns: 1fr;
	}
}
</style>
