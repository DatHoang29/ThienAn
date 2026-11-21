<template>
	<el-card shadow="hover" class="image-view-container">
		<div class="display-flex--horizontal flex-nowrap flex-gap--5px">
			<div class="text-center flex-1">
				<h4 class="text-uppercase bg-color-primary padding-5px">{{ $t('lz.label.tollHauKiem.imagePlateIn') }}</h4>
				<b v-if="state.strList[0]" style="color: var(--el-color-danger)">{{ state.strList[0] }} - {{ state.plateIn }}</b>
				<div class="image-container">
					<el-image
						class="el-image"
						:src="state.srcList[0]"
						:zoom-rate="1.2"
						:max-scale="7"
						:min-scale="0.2"
						:preview-src-list="state.previewSrcList"
						:initial-index="0"
						fit="fill"
						show-progress
						@error="handleImageError(0)"
					>
						<template #error>
							<img :src="noImage" style="width: 100%; height: 100%" />
						</template>
					</el-image>
				</div>
			</div>
			<div class="text-center flex-1">
				<h4 class="text-uppercase bg-color-primary padding-5px">{{ $t('lz.label.tollHauKiem.imagePlateOut') }}</h4>
				<b v-if="state.strList[1]" style="color: var(--el-color-danger)">{{ state.strList[1] }} - {{ state.plateOut }}</b>
				<div class="image-container">
					<el-image
						class="el-image"
						:src="state.srcList[1]"
						:zoom-rate="1.2"
						:max-scale="7"
						:min-scale="0.2"
						:preview-src-list="state.previewSrcList"
						:initial-index="1"
						fit="fill"
						show-progress
						@error="handleImageError(1)"
					>
						<template #error>
							<img :src="noImage" style="width: 100%; height: 100%" />
						</template>
					</el-image>
				</div>
			</div>
			<div class="text-center flex-1">
				<h4 class="text-uppercase bg-color-primary padding-5px">{{ $t('lz.label.tollHauKiem.imageVehicleIn') }}</h4>
				<b v-if="state.strList[2]" style="color: var(--el-color-danger)">{{ state.strList[2] }}</b>
				<div class="image-container">
					<el-image
						class="el-image"
						:src="state.srcList[2]"
						:zoom-rate="1.2"
						:max-scale="7"
						:min-scale="0.2"
						:preview-src-list="state.previewSrcList"
						:initial-index="2"
						fit="fill"
						show-progress
						@error="handleImageError(2)"
					>
						<template #error>
							<img :src="noImage" style="width: 100%; height: 100%; object-fit: cover" />
						</template>
					</el-image>
				</div>
			</div>
			<div class="text-center flex-1">
				<h4 class="text-uppercase bg-color-primary padding-5px">{{ $t('lz.label.tollHauKiem.imageVehicleOut') }}</h4>
				<b v-if="state.strList[3]" style="color: var(--el-color-danger)">{{ state.strList[3] }} - {{ state.price }}</b>
				<div class="image-container">
					<el-image
						class="el-image"
						:src="state.srcList[3]"
						:zoom-rate="1.2"
						:max-scale="7"
						:min-scale="0.2"
						:preview-src-list="state.previewSrcList"
						show-progress
						:initial-index="3"
						fit="fill"
						@error="handleImageError(3)"
					>
						<template #error>
							<img :src="noImage" style="width: 100%; height: 100%" />
						</template>
					</el-image>
				</div>
			</div>
		</div>
	</el-card>
</template>

<script lang="ts" setup name="tollHauKiem">
import { reactive, onMounted } from 'vue';
import { useDateFormat } from '@vueuse/core';
import noImage from '/@/assets/img/camera_unavailable.jpg';
import { SysConfigApi } from '/@/api-services/system';
import { getAPI } from '/@/utils/axios-utils';
import { ListTransactionOutPut, HaukiemTransactionApi } from '/@/api-services/toll';
import { DATE_TIME_FORMAT } from '/@/types/formats';

const state = reactive({
	srcList: [noImage, noImage, noImage, noImage] as any,
	strList: ['', '', '', ''] as any,
	previewSrcList: [] as string[],
	baseUrl: '' as any,
	plateIn: '' as string,
	plateOut: '' as string,
	price: undefined as any,
});

defineEmits(['checkPostBike']);

onMounted(async () => {
	const rs = await getAPI(SysConfigApi).apiSystemSysconfigFileserverGet('absolute');
	state.baseUrl = rs.data.result;
	// Initialize preview list with noImage
	state.previewSrcList = [noImage, noImage, noImage, noImage];
});

// Set image list
const setImageList = async (row: any) => {
	const item = { ...row } as ListTransactionOutPut;

	const { imageLane, imageLaneIn, imagePlate, imagePlateIn } = item;
	let images = [
		resolveImageUrl(imagePlateIn as string, state.baseUrl),
		resolveImageUrl(imagePlate as string, state.baseUrl),
		resolveImageUrl(imageLaneIn as string, state.baseUrl),
		resolveImageUrl(imageLane as string, state.baseUrl),
	];
	// Assign list image to show picture
	state.srcList = images;

	// Show info about time in, time out, plate in, plate out
	const timeIn = formatDateTime(item.transactionDatetimeIn);
	const timeOut = formatDateTime(item.transactionDatetime);
	state.strList = [timeIn, timeOut, timeIn, timeOut];

	state.plateIn = item.plate as string;
	state.plateOut = item.plateEdit as string;
	state.price = item.price != null ? Number(item.price).toLocaleString('vi-VN') : '';

	// Show image preview
	state.previewSrcList = images;

	// Gọi API lấy ảnh hậu kiểm (base64) từ TCS
	if (imagePlateIn || imagePlate || imageLaneIn || imageLane) {
		try {
			const res = await getAPI(HaukiemTransactionApi).apiTollHaukiemtransactionImagePost({
				imagePlateIn,
				imagePlate,
				imageLaneIn,
				imageLane,
			});
			const imgData = res.data?.result;
			if (imgData) {
				images = [
					imgData.imagePlateIn || resolveImageUrl(imagePlateIn as string, state.baseUrl),
					imgData.imagePlate || resolveImageUrl(imagePlate as string, state.baseUrl),
					imgData.imageLaneIn || resolveImageUrl(imageLaneIn as string, state.baseUrl),
					imgData.imageLane || resolveImageUrl(imageLane as string, state.baseUrl),
				];
				state.srcList = images;
				state.previewSrcList = images;
			}
		} catch (error) {
			console.error('Error fetching transaction images:', error);
		}
	}
};

// Format thời gian, bỏ qua giá trị không hợp lệ (năm <= 1900)
const formatDateTime = (value: any): string => {
	if (!value || new Date(value).getFullYear() <= 1900) return '';
	return useDateFormat(value, DATE_TIME_FORMAT).value;
};

// Hàm xử lý chuỗi đường dẫn hợp lệ
const resolveImageUrl = (path: string | null, baseUrl: string): string => {
	if (!path) return noImage;
	return `${baseUrl}${path}`;
};

// Handle image load errors
const handleImageError = (index: number) => {
	// Update both the displayed image and preview image
	state.srcList[index] = noImage;
	state.previewSrcList[index] = noImage;
};

defineExpose({
	setImageList,
	noImage,
});
</script>

<style scoped>
.image-view-container {
	width: 100%;
}

.image-container {
	overflow: hidden;
	height: calc(100% - 80px);
}

.el-image {
	height: 100%;
	width: 100%;
}

.image-placeholder {
	display: flex;
	justify-content: center;
	align-items: center;
	height: 100%;
	background-color: var(--el-fill-color-light);
	color: var(--el-text-color-secondary);
	font-size: 14px;
}

.padding-10px {
	padding: 10px;
}

.text-uppercase {
	text-transform: capitalize;
	color: var(--el-color-white);
}

.text-center {
	text-align: center;
}

.display-flex--horizontal {
	display: flex;
	flex-direction: row;
}

.flex-nowrap {
	flex-wrap: nowrap;
}

.flex-gap--5px {
	gap: 5px;
}

.flex-1 {
	flex: 1;
}
</style>
