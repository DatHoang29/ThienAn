<template>
	<el-dialog v-model="state.isShowDialog" width="700px" draggable :close-on-click-modal="false" destroy-on-close>
		<template #header>
			<div style="color: #fff">
				<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-Edit /></el-icon>
				<span>{{ props.title }}</span>
			</div>
		</template>
		<el-form ref="editFormRef" :model="state.editForm" label-width="auto" label-position="left">
			<el-row :gutter="10">
				<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
					<el-form-item :label="$t('lz.entity.tms.equipment')" prop="equipmentId" :rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.tms.equipment') }), trigger: 'change' }]">
						<el-select v-model="state.editForm.equipmentId" filterable :disabled="state.isEdit" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.equipment') })" class="w100">
							<el-option v-for="item in availableEquipment" :key="item.id" :label="`${item.code ?? ''} - ${item.name ?? ''} (Km ${item.kmNumber ?? 0}+${item.metNumber ?? 0})`" :value="item.id" />
						</el-select>
					</el-form-item>
				</el-col>
				<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
					<el-form-item :label="$t('lz.entity.tms.lane')">
						<el-select v-model="state.editForm.laneId" filterable clearable :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.lane') })" class="w100">
							<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TmsLaneType)" :key="item.code" :label="item.name" :value="item.code" />
						</el-select>
					</el-form-item>
				</el-col>
				<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
					<el-form-item :label="$t('lz.entity.base.status')">
						<el-radio-group v-model="state.editForm.status">
							<el-radio v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code" :value="parseInt(item.code)">{{ item.name }}</el-radio>
						</el-radio-group>
					</el-form-item>
				</el-col>
				<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
					<el-form-item :label="$t('lz.entity.base.remark')">
						<el-input v-model="state.editForm.remark" type="textarea" :rows="4" :maxlength="EntityFieldLength.Length1024" show-word-limit :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.remark') })" />
					</el-form-item>
				</el-col>
			</el-row>
		</el-form>
		<template #footer>
            <span class="dialog-footer">
			<el-button @click="state.isShowDialog = false">{{ $t('lz.button.base.cancel') }}</el-button>
			<el-button type="primary" :disabled="state.isProcessing" @click="onSubmit">{{ $t('lz.button.base.confirm') }}</el-button>
            </span>
		</template>
	</el-dialog>
</template>

<script lang="ts" setup name="addMappingDialog">
import { reactive, ref, computed } from 'vue';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { TmsZoneEquipmentApi } from '/@/api-services/tms';
import { BaseConfigTypeEnum, EntityFieldLength } from '/@/types/enums/baseEnum';
import { useBaseConfig } from '/@/stores/baseConfig';
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;

const { t: $t } = useI18n();

const props = defineProps<{
	title: string;
	zoneId: string | null;
	equipmentList: any[];
	existingEquipmentIds: string[];
}>();

const emit = defineEmits(['handleQuery']);

const editFormRef = ref();

const availableEquipment = computed(() => {
		const list = props.equipmentList ?? [];
		if (state.isEdit && state.editForm.equipmentId) {
			return list.filter((e: any) => !props.existingEquipmentIds.includes(e.id) || e.id === state.editForm.equipmentId);
		}
		return list.filter((e: any) => !props.existingEquipmentIds.includes(e.id));
	});

const state = reactive({
	isShowDialog: false,
    isProcessing: false,
	isEdit: false,
	editForm: {
		id: '' as string,
		zoneId: '' as string,
		equipmentId: '' as string,
		type: '' as string,
		laneId: '' as string,
		status: 1 as number,
		remark: '' as string,
	},
});


function open(row?: any) {
    editFormRef.value?.resetFields();
	state.isEdit = !!row;
	state.editForm = {
		id: row?.id ?? '',
		zoneId: props.zoneId ?? '',
		equipmentId: row?.equipmentId ?? '',
		type: row?.type ?? '',
		laneId: row?.laneId ?? '',
		status: row?.status ?? 1,
		remark: row?.remark ?? '',
	};
	state.isShowDialog = true;
}

async function onSubmit() {
    if (state.isProcessing || !(await editFormRef.value?.validate().catch(() => false))) return;
    state.isProcessing = true;
	try {
		if (state.isEdit) {
			await getAPI(TmsZoneEquipmentApi).apiTmsTmszoneequipmentUpdatePost(state.editForm);
		} else {
			state.editForm.zoneId = props.zoneId ?? '';
			await getAPI(TmsZoneEquipmentApi).apiTmsTmszoneequipmentAddPost(state.editForm);
		}
		ElMessage.success($t('lz.message.base.saveSuccess'));
		state.isShowDialog = false;
		emit('handleQuery');
	} catch (error) {
		console.error(error);
	} finally { state.isProcessing = false; }
}

defineExpose({ open });
</script>
