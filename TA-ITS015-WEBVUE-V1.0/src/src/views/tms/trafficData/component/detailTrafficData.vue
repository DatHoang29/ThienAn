<template>
	<el-dialog v-model="state.isShowDialog" :title="$t('lz.label.base.detail')" width="800">
		<template #header>
			<div style="color: #fff">
				<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-View /></el-icon>
				<span> {{ props.title }} </span>
			</div>
		</template>
		<table :model="state.dataList" class="table-data w100">
			<tr class="alt">
				<th>{{ $t('lz.entity.tms.licensePlate') }}</th>
				<td><b class="text-h2 text-upper">{{ state.dataList.licensePlate }}</b></td>
				<th>{{ $t('lz.entity.tms.length') }}</th>
				<td>
					<el-tag  size="large" type="primary">
						<b>{{ state.dataList.lengthMm }} (mm)</b>
					</el-tag>
				</td>
			</tr>
			<tr>
				<th>{{ $t('lz.entity.tmsTrafficInfo.equipmentId') }}</th>
				<td>{{ state.equipmentList?.find((item: any) => item.id == state.dataList.equipmentId)?.name }}</td>
				<th>{{ $t('lz.entity.tms.direction') }}</th>
				<td>
					{{ state.directionList?.find((item: any) => item.code == state.dataList.direction)?.name }}
				</td>
			</tr>
			<tr>
				<th>{{ $t('lz.entity.tms.vehicleSize') }}</th>
				<td>{{ state.vehicleTypeList?.find((item: any) => item.code?.trim().toLowerCase() == state.dataList.vehicleSize?.trim().toLowerCase())?.name ?? state.dataList.vehicleSize }}</td>
				<th>{{ $t('lz.entity.tms.lane') }}</th>
				<td>{{ state.laneList?.find((item: any) => item.code == state.dataList.lane)?.name }}</td>
			</tr>
			<tr :class="(state.dataList.vehicleSize ? '' : 'alt')">
				<th>{{ $t('lz.entity.base.image') }}</th>
				<td colspan="3">
					<div class="panel-img">
						<img v-if="state.imageUrl" :src="state.imageUrl" />
						<span v-else>{{ state.imageLoading ? $t('lz.label.base.loadingImage') : $t('lz.label.base.noImage') }}</span>
					</div>
				</td>
			</tr>
		</table>		
		<template #footer>
			<span class="dialog-footer">
				<el-button type="danger" @click="state.isShowDialog = false">{{ $t('lz.button.base.cancel') }}</el-button>
			</span>
		</template>
	</el-dialog>
</template>
  
<script lang="ts" setup>
	import { reactive, onMounted, onUnmounted, watch } from 'vue';
	import { useI18n } from 'vue-i18n';
	import { getAPI } from '/@/utils/axios-utils';

	import { TmsEquipmentApi } from '/@/api-services/tms';
	import { PageTrafficDataOutput, VdsMediaApi } from '/@/api-services/vds';
	import { vdsTimestamp } from '/@/utils/vdsTimestamp';

	import { useBaseConfig } from '/@/stores/baseConfig';
	import { BaseConfigTypeEnum } from '/@/types/enums';

	const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
	
	interface Props {
		operateType: OperateType;
		title: string;
	}

	const { t: $t } = useI18n();
	const props = defineProps<Props>();
  	const state = reactive({
		isShowDialog: false,
		dataList: {} as PageTrafficDataOutput,
		imageUrl: '' as string,
		imageLoading: false,
		equipmentList: [] as any,
		directionList: [] as any,
		vehicleTypeList: [] as any,
		laneList: [] as any,
	});


	// Page initialization
	onMounted(async () => {
		let response = await getAPI(TmsEquipmentApi).apiTmsTmsequipmentListGet();
		state.equipmentList = response.data.result;
		
		state.directionList = getConfigDataByCode(BaseConfigTypeEnum.TmsDirectionType);
		state.laneList = getConfigDataByCode(BaseConfigTypeEnum.TmsLaneType);
		state.vehicleTypeList = getConfigDataByCode(BaseConfigTypeEnum.TmsVehicleTypeConfigCode);
	});

	let imageRequestId = 0;
	const loadImage = async (row: PageTrafficDataOutput, requestId: number) => {
		try {
			if (row.vendorEventId == null || row.vendorEventId <= 0 || !row.detectTime) return;
			const detectTime = vdsTimestamp(row.detectTime);
			if (!detectTime) return;
			const response = await getAPI(VdsMediaApi).apiVdsVdsmediaFileGet(row.vendorEventId, detectTime as unknown as Date, 'full', { responseType: 'blob' });
			const blob = response.data;
			const contentType = String((response.headers as any)?.['content-type'] ?? blob?.type ?? '').toLowerCase();
			if (!(blob instanceof Blob) || blob.size === 0 || contentType.includes('json') || contentType.includes('problem')) return;
			const signature = new Uint8Array(await blob.slice(0, 12).arrayBuffer());
			const isImage = (signature[0] === 0xff && signature[1] === 0xd8 && signature[2] === 0xff)
				|| (signature[0] === 0x89 && signature[1] === 0x50 && signature[2] === 0x4e && signature[3] === 0x47)
				|| (signature[0] === 0x47 && signature[1] === 0x49 && signature[2] === 0x46)
				|| (signature[0] === 0x52 && signature[1] === 0x49 && signature[2] === 0x46 && signature[8] === 0x57 && signature[9] === 0x45 && signature[10] === 0x42 && signature[11] === 0x50);
			if (!isImage) return;
			const imageUrl = await new Promise<string>((resolve, reject) => {
				const reader = new FileReader();
				reader.onload = () => resolve(String(reader.result));
				reader.onerror = () => reject(reader.error);
				reader.readAsDataURL(blob);
			});
			if (requestId === imageRequestId && state.isShowDialog) state.imageUrl = imageUrl;
		} catch {
			// A record can legitimately have no media file.
		} finally {
			if (requestId === imageRequestId) state.imageLoading = false;
		}
	};

	// Open dialog
	const openDialog = (row: PageTrafficDataOutput) => {
		const requestId = ++imageRequestId;
		state.dataList = row;
		state.imageUrl = '';
		state.imageLoading = true;
		state.isShowDialog = true;
		void loadImage(row, requestId);
	};
	watch(() => state.isShowDialog, visible => {
		if (visible) return;
		++imageRequestId;
		state.imageUrl = '';
		state.imageLoading = false;
	}, { flush: 'sync' });
	onUnmounted(() => { ++imageRequestId; });

	// Export object
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

	.text-h1{
		font-size: large;
	}

	.text-upper{
		text-transform: uppercase;
	}

	.text-h2{
		font-size: 14px;
	}

	.panel-img img{
		max-width: 100%;
		max-height: 150px;
		padding: 10px;
	}
</style>
