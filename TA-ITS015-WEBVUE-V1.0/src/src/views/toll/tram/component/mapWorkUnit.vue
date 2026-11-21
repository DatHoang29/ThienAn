<template>
	<div class="toll-map-work-unit-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="600px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Link /> </el-icon>
					<span> {{ $t('lz.label.toll.mapWorkUnit') }} </span>
				</div>
			</template>
			<el-form :model="state.editForm" ref="editFormRef" label-width="auto" label-position="left" v-loading="state.isLoading">
				<el-row :gutter="10">
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.toll.stationId')">
							<el-input v-model="state.editForm.stationId" disabled />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.name')">
							<el-input v-model="state.editForm.name" disabled />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.toll.workUnit')" prop="refId">
							<el-select
								v-model="state.editForm.refId"
								filterable
								clearable
								class="w100"
								:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.toll.workUnit') })"
								@clear="state.editForm.refId = undefined"
							>
								<el-option v-for="item in state.workUnitList" :key="item.id" :label="`${item.code} - ${item.name}`" :value="item.id" :disabled="!!item.mappedStation">
									<span>{{ item.code }} - {{ item.name }}</span>
									<span v-if="item.mappedStation" style="float: right; color: var(--el-text-color-secondary); font-size: 12px">
										{{ $t('lz.label.toll.workUnitMappedTo', { name: item.mappedStation }) }}
									</span>
								</el-option>
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
			<template #footer>
				<span class="dialog-footer">
					<el-button @click="cancel">{{ $t('lz.button.base.cancel') }}</el-button>
					<el-button type="primary" @click="submit" :disabled="state.isProcessing">{{ $t('lz.button.base.confirm') }}</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup>
import { reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';

import { getAPI } from '/@/utils/axios-utils';
import { TramApi, TramOutput, MapWorkUnitTramInput } from '/@/api-services/toll';
import { TmsWorkUnitApi } from '/@/api-services/tms';

const WORK_UNIT_TYPE_TOLL = 'toll';

const { t: $t } = useI18n();
const emits = defineEmits(['handleQuery']);

const editFormRef = ref();
const state = reactive({
	isShowDialog: false,
	isLoading: false,
	isProcessing: false,
	editForm: {} as { id?: string; stationId?: string | null; name?: string | null; refId?: string | null },
	workUnitList: [] as { id: string; code?: string | null; name?: string | null; mappedStation?: string | null }[],
});

const loadWorkUnitList = async (stationId?: string) => {
	state.isLoading = true;
	try {
		const [rsWorkUnit, rsStation] = await Promise.all([
			getAPI(TmsWorkUnitApi).apiTmsTmsworkunitListGet(undefined, undefined, WORK_UNIT_TYPE_TOLL),
			getAPI(TramApi).apiTollTramListGet(),
		]);
		const stations = (rsStation.data.result ?? []) as TramOutput[];
		const mappedBy = new Map<string, string>();
		stations.filter((s) => s.refId && s.id !== stationId).forEach((s) => mappedBy.set(s.refId!, s.name ?? s.stationId ?? ''));

		state.workUnitList = ((rsWorkUnit.data.result ?? []) as any[]).map((wu) => ({
			id: wu.id,
			code: wu.code,
			name: wu.name,
			mappedStation: mappedBy.get(wu.id) ?? null,
		}));
	} finally {
		state.isLoading = false;
	}
};

const openDialog = async (row: TramOutput) => {
	editFormRef.value?.resetFields();
	state.editForm = { id: row.id ?? undefined, stationId: row.stationId, name: row.name, refId: row.refId ?? undefined };
	state.isShowDialog = true;
	await loadWorkUnitList(row.id ?? undefined);
};

const cancel = () => {
	state.isShowDialog = false;
};

const submit = async () => {
	try {
		state.isProcessing = true;
		await getAPI(TramApi).apiTollTramMapworkunitPost({ id: state.editForm.id!, refId: state.editForm.refId ?? null } as MapWorkUnitTramInput);
		ElMessage.success($t('lz.message.base.processSuccess'));
		emits('handleQuery');
		state.isShowDialog = false;
	} catch (error) {
	} finally {
		state.isProcessing = false;
	}
};

defineExpose({ openDialog });
</script>
