<template>
	<div class="tms-traffic-matrix-container">
		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%" label-position="left">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="5" :xl="5">
						<el-form-item :label="$t('lz.label.base.date')" prop="valueDate">
							<el-date-picker
								v-model="state.queryParams.valueDate"
								type="date"
								:clearable="false"
								:disabled-date="disabledFutureDate"
								:format="DateTimeFormatEnum.DateDefault"
								:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.label.base.date') })"
								class="w100"
								@keyup.enter.native="handleQuery"
							/>
						</el-form-item>
					</el-col>
					<!-- Loại khu vực quyết định cái gì được dùng làm HÀNG của ma trận. Lấy từ danh mục
					     tms_zone_type chứ không cố định chuỗi, vì mỗi dự án khai mã khác nhau. -->
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="5" :xl="5">
						<el-form-item :label="$t('lz.label.tmsTrafficMatrix.zoneType')" prop="zoneType">
							<el-select v-model="state.queryParams.zoneType" filterable class="w100" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.label.tmsTrafficMatrix.zoneType') })" @change="handleZoneTypeChange">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TmsZoneType)" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="5" :xl="5">
						<el-form-item :label="$t('lz.label.tmsTrafficMatrix.section')" prop="zoneId">
							<el-select v-model="state.queryParams.zoneId" filterable clearable class="w100" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.label.tmsTrafficMatrix.section') })">
								<el-option v-for="item in state.zoneList" :key="item.id" :label="item.name" :value="item.id" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="4" :xl="4">
						<el-form-item :label="$t('lz.entity.tms.vehicleType')" prop="vehicleType">
							<el-select v-model="state.queryParams.vehicleType" filterable clearable class="w100" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.vehicleType') })">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TmsVehicleTypeConfigCode)" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<!-- Đổi chế độ xem KHÔNG gọi lại API: BE trả cả vận tốc và lưu lượng trong một lần.
					     Danh sách và nhãn lấy từ cấu hình tham số tms_traffic_matrix_metric. -->
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="5" :xl="5">
						<el-form-item :label="$t('lz.label.tmsTrafficMatrix.metric')" prop="metric">
							<el-radio-group v-model="state.metric" size="small">
								<el-radio-button v-for="item in metricList" :key="item.code" :label="item.code">{{ item.name }}</el-radio-button>
							</el-radio-group>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>

			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />
			<el-row>
				<SearchButton :auth-search="'tmsTrafficMatrix:view'" @search="handleQuery" @reset="resetQuery" :loading="state.loading" />
			</el-row>
		</el-card>

		<el-card class="matrix-body" shadow="hover" style="margin-top: 5px" v-loading="state.loading">
			<!-- Thang màu: nhãn và màu đều lấy từ BE để web và báo cáo không lệch nhau -->
			<div class="matrix-legend" v-if="activeBands.length">
				<div class="matrix-legend-item" v-for="(band, index) in activeBands" :key="index">
					<div class="matrix-legend-label">{{ bandLabel(band) }}</div>
					<div class="matrix-legend-color" :style="{ backgroundColor: band.color ?? '' }"></div>
				</div>
			</div>

			<el-collapse v-model="state.expandedDirections" v-if="state.matrix?.groups?.length">
				<el-collapse-item v-for="group in state.matrix.groups" :key="groupKey(group)" :name="groupKey(group)">
					<template #title>
						<span class="matrix-group-title">
							{{ group.directionName || $t('lz.label.tmsTrafficMatrix.unknownDirection') }}
						</span>
					</template>
					<MatrixTable :row-list="group.rows ?? []" :hours="state.matrix.hours ?? []" :bands="activeBands" :metric="state.metric" />
				</el-collapse-item>
			</el-collapse>

			<el-empty v-else :image-size="200" :description="$t('lz.label.base.noData')" />
		</el-card>
	</div>
</template>

<script lang="ts" setup name="tmsTrafficMatrix">
import { computed, defineAsyncComponent, onMounted, reactive } from 'vue';
import { useI18n } from 'vue-i18n';
import { useDateFormat, useNow } from '@vueuse/core';
import { getAPI } from '/@/utils/axios-utils';
import { TmsTrafficMatrixApi } from '/@/api-services/tms';
import { TrafficMatrixBand, TrafficMatrixGroupOutput, TrafficMatrixInput, TrafficMatrixOutput } from '/@/api-services/tms';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { DateTimeFormatEnum } from '/@/types/enums/baseEnum';
import { useBaseConfig } from '/@/stores/baseConfig';
import { TrafficMatrixMetric, TrafficMatrixMetricEnum } from '/@/views/tms/trafficMatrix/metric';

const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const MatrixTable = defineAsyncComponent(() => import('/@/views/tms/trafficMatrix/component/matrixTable.vue'));

const { t: $t } = useI18n();
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const now = useNow();
let matrixRequestId = 0;

const state = reactive({
	loading: false,
	queryParams: {
		valueDate: new Date(new Date(now.value).setHours(0, 0, 0, 0)) as any,
		zoneType: undefined as any,
		zoneId: undefined as any,
		vehicleType: undefined as any,
	},
	metric: TrafficMatrixMetricEnum.Speed as TrafficMatrixMetric,
	matrix: null as TrafficMatrixOutput | null,
	zoneList: [] as any,
	expandedDirections: [] as string[],
});

onMounted(async () => {
	state.metric = metricList.value[0]?.code ?? TrafficMatrixMetricEnum.Speed;
	/* KHÔNG đoán loại khu vực hay tuyến ở FE. Gọi lần đầu để trống, BE lấy theo cấu hình hoạt
	   động rồi echo lại trong response; FE đồng bộ theo đúng giá trị đó. */
	await handleQuery();
});

/* Không cho chọn ngày ở tương lai: chưa có xe chạy thì ma trận chắc chắn rỗng */
const disabledFutureDate = (time: Date) => time.getTime() > Date.now();

/* Danh sách chế độ xem lấy từ cấu hình tham số. Chưa cấu hình thì vẫn phải cho đổi chế độ, nếu
   không trang mất hẳn một nửa chức năng — nên lùi về hai mã hợp đồng với nhãn từ i18n. */
const metricList = computed(() => {
	const list = getConfigDataByCode(BaseConfigTypeEnum.TmsTrafficMatrixMetric) ?? [];
	if (list.length) return list;

	return [
		{ code: TrafficMatrixMetricEnum.Speed, name: $t('lz.label.tmsTrafficMatrix.metricSpeed'), value: '' },
		{ code: TrafficMatrixMetricEnum.Volume, name: $t('lz.label.tmsTrafficMatrix.metricVolume'), value: '' },
	] as any;
});

/* Đơn vị đo của chế độ đang xem, lấy từ Value của bản ghi cấu hình */
const metricUnit = computed(() => {
	const item = metricList.value.find((entry: any) => entry.code === state.metric);
	return item?.value || $t(isSpeed.value ? 'lz.label.tmsTrafficMatrix.speedUnit' : 'lz.label.tmsTrafficMatrix.volumeUnit');
});

/* Danh sách đoạn tuyến cho ô chọn.
   Lấy từ chính các HÀNG mà BE trả về, không gọi TmsZoneApi nữa: BE đã giới hạn phạm vi theo
   cấu hình tuyến nên danh sách này luôn khớp đúng những gì đang xem. Gọi API zone riêng sẽ ra
   cả đoạn của tuyến khác — đúng cái đang gây nhiễu.
   Chỉ nạp lại khi ĐANG XEM TOÀN PHẠM VI, vì lúc đã chọn một đoạn thì BE chỉ trả về đoạn đó. */
const captureSectionList = () => {
	if (state.queryParams.zoneId) return;

	state.zoneList = (state.matrix?.groups?.[0]?.rows ?? []).map((row) => ({
		id: row.zoneId,
		name: row.zoneName,
	}));
};

/* Đổi loại khu vực thì lựa chọn đoạn tuyến cũ không còn đúng nữa, phải xoá kèm theo,
   kẻo gửi lên một zoneId không thuộc loại đang xem. */
const handleZoneTypeChange = async () => {
	state.queryParams.zoneId = undefined;
	await handleQuery();
};

const handleQuery = async () => {
	const requestId = ++matrixRequestId;
	state.loading = true;
	try {
		const params = {
			valueDate: state.queryParams.valueDate,
			zoneType: state.queryParams.zoneType,
			zoneId: state.queryParams.zoneId,
			vehicleType: state.queryParams.vehicleType,
		} as TrafficMatrixInput;

		const res = await getAPI(TmsTrafficMatrixApi).apiTmsTmstrafficmatrixHourlymatrixPost(params);
		if (requestId !== matrixRequestId) return;
		state.matrix = res.data.result ?? null;
		/* Đồng bộ ô chọn với loại khu vực BE đã thực dùng (lần đầu vào trang chưa chọn gì) */
		if (!state.queryParams.zoneType && state.matrix?.zoneType) state.queryParams.zoneType = state.matrix.zoneType;
		captureSectionList();
		/* Mở sẵn mọi hướng: người dùng vào là thấy số ngay, không phải bấm thêm */
		state.expandedDirections = (state.matrix?.groups ?? []).map((group) => groupKey(group));
	} catch (error) {
		if (requestId !== matrixRequestId) return;
		state.matrix = null;
		state.expandedDirections = [];
	} finally {
		if (requestId === matrixRequestId) state.loading = false;
	}
};

const resetQuery = async () => {
	state.queryParams.valueDate = new Date(new Date(now.value).setHours(0, 0, 0, 0));
	state.queryParams.zoneType = undefined;
	state.queryParams.zoneId = undefined;
	state.queryParams.vehicleType = undefined;
	state.metric = metricList.value[0]?.code ?? TrafficMatrixMetricEnum.Speed;
	await handleQuery();
};

const isSpeed = computed(() => state.metric === TrafficMatrixMetricEnum.Speed);

const activeBands = computed<Array<TrafficMatrixBand>>(() =>
	(isSpeed.value ? state.matrix?.speedBands : state.matrix?.volumeBands) ?? []
);

/* Hướng chưa khai có directionCode rỗng, dùng chuỗi cố định làm khoá cho el-collapse */
const groupKey = (group: TrafficMatrixGroupOutput) => group.directionCode ?? 'unknown';

/* Nhãn chú giải: ưu tiên tên đã khai trong cấu hình tham số để người vận hành sửa được chữ trên
   thanh màu; thiếu tên thì tự dựng từ khoảng giá trị + đơn vị. */
const bandLabel = (band: TrafficMatrixBand) => {
	if (band.name) return band.name;

	const unit = metricUnit.value;
	if (band.from === null || band.from === undefined) return `${band.to}${unit} ${$t('lz.label.tmsTrafficMatrix.under')}`;
	if (band.to === null || band.to === undefined) return `${band.from}${unit} ${$t('lz.label.tmsTrafficMatrix.over')}`;
	return `${band.from}~${band.to}${unit}`;
};
</script>

<style lang="scss" scoped>
.tms-traffic-matrix-container {
	height: 100%;
	display: flex;
	flex-direction: column;
}

.matrix-body {
	flex: 1;
	display: flex;
	flex-direction: column;
	overflow: auto;
}

.matrix-legend {
	display: flex;
	flex-wrap: wrap;
	margin-bottom: 10px;
	border: 1px solid var(--el-border-color-light);
	border-radius: var(--el-border-radius-base);
	overflow: hidden;
}

.matrix-legend-item {
	flex: 1 1 0;
	min-width: 92px;
}

.matrix-legend-label {
	padding: 4px 6px;
	font-size: 12px;
	text-align: center;
	white-space: nowrap;
	color: var(--el-text-color-regular);
	background-color: var(--el-fill-color-light);
	border-right: 1px solid var(--el-border-color-lighter);
}

.matrix-legend-color {
	height: 14px;
}

.matrix-group-title {
	font-weight: 600;
	color: var(--el-text-color-primary);
}
</style>
