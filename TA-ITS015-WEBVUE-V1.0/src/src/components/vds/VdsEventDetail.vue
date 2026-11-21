<template>
	<el-dialog v-model="state.isShowDialog" @close="close" width="700px" draggable :close-on-click-modal="false" append-to-body>
		<template #header>
			<div style="color: var(--el-color-white)">
				<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-Edit /></el-icon>
				<span>{{ item?.eventTypeName }}</span>
			</div>
		</template>
		<template v-if="item">
			<el-descriptions :column="2" border>
				<el-descriptions-item :label="t('lz.label.base.camera')">{{ item.equipmentName ?? item.equipmentId }}</el-descriptions-item>
				<el-descriptions-item :label="t('lz.label.base.position')"
					>{{ item.locationName }} {{ t('lz.entity.tms.km') }} {{ item.kmNumber ?? '—' }} + {{ item.metNumber ?? '—' }} {{ item.influenceScopeName }}</el-descriptions-item
				>
				<el-descriptions-item :label="t('lz.entity.tms.licensePlate')">{{ item.licensePlate ?? '—' }}</el-descriptions-item>
				<el-descriptions-item :label="t(item.source === 'journey' ? 'lz.entity.tms.averageSpeed' : 'lz.entity.vdsEvent.speed')"
					>{{ item.source === 'journey' ? item.avgSpeedKmh : item.speed }} {{ t('lz.label.tmsTrafficMatrix.speedUnit') }}</el-descriptions-item
				>
				<el-descriptions-item :label="t('lz.entity.vdsEvent.limit')">{{ item.speedMin ?? '—' }} / {{ item.speedMax ?? '—' }} {{ t('lz.label.tmsTrafficMatrix.speedUnit') }}</el-descriptions-item>
				<el-descriptions-item :label="t('lz.entity.vdsEvent.detected')">{{
					item.lastDetectTime ? useDateFormat(vdsTimestamp(item.lastDetectTime), DateTimeFormatEnum.DateTimeDefault).value : '—'
				}}</el-descriptions-item>
				<el-descriptions-item :label="t('lz.entity.vds.aidEvent')">{{ item.aidEventType ?? '—' }}</el-descriptions-item>
				<el-descriptions-item :label="t('lz.entity.vdsEvent.count')">{{ item.count }}</el-descriptions-item>
				<template v-if="item.source === 'journey'">
					<el-descriptions-item :label="t('lz.entity.tms.fromCamera')">{{ item.fromEquipmentName ?? item.fromEquipmentId }}</el-descriptions-item>
					<el-descriptions-item :label="t('lz.label.vdsEvent.journey')"
						>{{ item.distanceMeter }} {{ t('lz.label.base.meterUnit') }} / {{ item.travelSeconds }} {{ t('lz.label.tmsServiceMonitor.unitSecond') }}</el-descriptions-item
					>
				</template>
			</el-descriptions>
			<div v-loading="loading" class="event-image">
				<el-image v-if="image" :src="image" :preview-src-list="[image]" fit="contain" /><el-text v-else>{{ t('lz.label.base.noImage') }}</el-text>
			</div>
		</template>
		<template #footer>
			<span class="dialog-footer"
				><el-button @click="state.isShowDialog = false">{{ t('lz.button.base.close') }}</el-button></span
			>
		</template>
	</el-dialog>
</template>
<script setup lang="ts" name="vdsEventDetail">
/** Chi tiết sự kiện không có luồng xử lý sự cố. */
import { reactive, toRefs, onUnmounted } from 'vue';
import { useDateFormat } from '@vueuse/core';
import { DateTimeFormatEnum } from '/@/types/enums/baseEnum';
import { vdsTimestamp } from '/@/utils/vdsTimestamp';
import { auth } from '/@/utils/authFunction';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { VdsMediaApi } from '/@/api-services/vds';
import type { EventSnapshotItemOutput } from '/@/api-services/vds';
const { t } = useI18n();
const state = reactive({
	isShowDialog: false,
	item: undefined as EventSnapshotItemOutput | undefined,
	image: '',
	loading: false,
});
const { item, image, loading } = toRefs(state);
let generation = 0;
function release() {
	if (image.value.startsWith('blob:')) URL.revokeObjectURL(image.value);
	image.value = '';
}
async function open(value: EventSnapshotItemOutput) {
	if (!auth('tmsIncident:view')) return;
	const current = ++generation;
	release();
	item.value = { ...value };
	state.isShowDialog = true;
	loading.value = false;
	if (value.vendorEventId && value.detectTime) {
		loading.value = true;
		try {
			// Không chuyển eventId sang Number hay detectTime qua Date: phải giữ nguyên khóa QuestDB (eventId có thể
			// vượt 2^53, detectTime tới micro giây). Client sinh ra chỉ gọi toISOString khi tham số là Date, nên
			// truyền CHUỖI gốc thì được dùng nguyên văn — cùng cách trafficEventHistoryDetailModal đang làm.
			const response = await getAPI(VdsMediaApi).apiVdsVdsmediaFileGet(value.vendorEventId as unknown as number, value.detectTime as unknown as Date, 'full', { responseType: 'blob' });
			if (current === generation) image.value = URL.createObjectURL(response.data);
		} catch {
			/* Không có ảnh vẫn xem được thông tin sự kiện. */
		} finally {
			if (current === generation) loading.value = false;
		}
	}
}
function close() {
	++generation;
	release();
	loading.value = false;
}
onUnmounted(close);
defineExpose({ open });
</script>
<style scoped>
.event-image {
	min-height: 80px;
	margin-top: 12px;
	text-align: center;
}
.event-image :deep(img) {
	max-height: 360px;
}
</style>
