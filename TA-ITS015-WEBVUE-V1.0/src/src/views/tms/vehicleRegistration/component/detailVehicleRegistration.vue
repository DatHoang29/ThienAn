<template>
	<el-dialog v-model="state.isShowDialog" :title="$t('lz.label.tms.tmsVehicleRegistration')" width="800">
		<template #header>
			<div style="color: #fff">
				<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-View /></el-icon>
				<span> {{ props.title }} </span>
			</div>
		</template>
		<table :model="state.dataList" class="table-data w100">
			<tr class="title">
				<th colspan="4">
					<p>{{ state.dataList.licensePlate }}</p>
				</th>
			</tr>
			<tr class="alt">
				<th>{{ $t('lz.entity.tmsVehicleRegistration.brand') }}</th>
				<td>{{ state.dataList.brand }}</td>
				<th>{{ $t('lz.entity.tmsVehicleRegistration.chassisNumber') }}</th>
				<td>{{ state.dataList.chassisNumber }}</td>
			</tr>
			<tr>
				<th>{{ $t('lz.entity.tmsVehicleRegistration.inspectionStamp') }}</th>
				<td>{{ state.dataList.inspectionStamp }}</td>
				<th>{{ $t('lz.entity.tmsVehicleRegistration.expirationDate') }}</th>
				<td>{{ state.dataList.expirationDate ? useDateFormat(state.dataList.expirationDate, DateTimeFormatEnum.DateDefault) : null }}</td>
			</tr>
			<tr class="alt">
				<th>{{ $t('lz.entity.tms.vehicleType') }}</th>
				<td>{{ state.dataList.vehicleType }}</td>
				<th>{{ $t('lz.entity.tmsVehicleRegistration.engineNumber') }}</th>
				<td>{{ state.dataList.engineNumber }}</td>
			</tr>
			<tr class="title">
				<th colspan="4">
				</th>
			</tr>
			<tr class="alt">
				<th>{{ $t('lz.entity.tmsVehicleRegistration.owner') }}</th>
				<td>{{ state.dataList.owner }}</td>
				<th>{{ $t('lz.entity.base.address') }}</th>
				<td>{{ state.dataList.address }}</td>
			</tr>
			<tr>
				<th>{{ $t('lz.entity.tmsVehicleRegistration.overallDimensions') }}</th>
				<td>{{ state.dataList.overallDimensions }}</td>
				<th>{{ $t('lz.entity.tmsVehicleRegistration.containerDimensions') }}</th>
				<td>{{ state.dataList.containerDimensions }}</td>
			</tr>
			<tr class="alt">
				<th>{{ $t('lz.entity.tmsVehicleRegistration.seat') }}</th>
				<td>{{ state.dataList.seat }}</td>
				<th>{{ $t('lz.entity.tmsVehicleRegistration.dateRegistrations') }}</th>
				<td>{{ state.dataList.dateRegistrations ? useDateFormat(state.dataList.dateRegistrations, DateTimeFormatEnum.DateDefault) : null }}</td>
			</tr>
		</table>
		<template #footer>
			<span class="dialog-footer">
				<el-button type="danger" @click="state.isShowDialog = false">{{ $t('lz.button.base.close') }}</el-button>
			</span>
		</template>
	</el-dialog>
</template>
  
<script lang="ts" setup name="detailVehicleRegistration">
	import { reactive, onMounted } from 'vue';
	import { useI18n } from 'vue-i18n';
	import { useDateFormat } from '@vueuse/core';

	import { useBaseConfig } from '/@/stores/baseConfig';
	import { BaseConfigTypeEnum } from '/@/types/enums';
	
	const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
	
	import { DateTimeFormatEnum } from '/@/types/enums/baseEnum';

	import { PageTmsVehicleRegistrationOutput } from '/@/api-services/tms';
	import { OperateType } from '/@/types/constants';

	interface Props {
		operateType: OperateType;
		title: string;
	}
	const { t: $t } = useI18n();
	const props = defineProps<Props>();
  	const state = reactive({
		isShowDialog: false,
		dataTypeList: [] as any,
		dataList: {} as PageTmsVehicleRegistrationOutput
	});

	onMounted(async () => {
		state.dataTypeList = getConfigDataByCode(BaseConfigTypeEnum.TmsVehicleRegistrationType);
	});

	const openDialog = (row: any) => {
		state.dataList = JSON.parse(JSON.stringify(row));
		state.isShowDialog = true;
	};

	defineExpose({ openDialog });
</script>

<style scoped>
	.table-data {
		border-collapse: collapse;  
	}

	.table-data td, th{
		padding: 15px 10px;
		text-align: left;
		border-color: #E6E8EB !important;
		border-left: thin solid;
		border-top: thin solid;
		border-right: thin solid;
	}

	.table-data tr:last-child td, th{
		border-bottom: thin solid;
	}

	.table-data tr.title th{
		text-align: center;
		font-size: large;
	}

	.table-data tr.alt{
		background-color: #F0F2F5;
	}
</style>