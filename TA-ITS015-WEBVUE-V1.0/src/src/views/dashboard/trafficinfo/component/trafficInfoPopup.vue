<template>
	<div class="simple-popup" v-if="state.showMe">
		<div class="simple-popup--container position-relative" :style="props.popupStyle">
			<div class="panel-header display-flex--horizontal padding-10px">
				<incidentButtonsComp :zoneId="state.zoneInfo?.id" dropdownSize="small" />
				<el-button type="danger" @click="state.showMe = false" class="button-close" />
			</div>
			<div class="panel-body" ref="popupBodyRef" style="margin-top: -20px;">
				<el-alert v-if="state.trafficError" :title="state.trafficError" type="warning" :closable="false" />
			<el-tabs v-model="curActiveTab" class="padding-10px h100" @tab-change="onTabChange">
					<el-tab-pane name="summaryTab" :label="$t('fe.base.text.summary')">
						<div v-if="state.isLoading" style="text-align: center;">
							<el-button type="primary" loading text>{{ $t('fe.base.text.loading') }}</el-button>
							<br><br>
						</div>
						<div v-else class="display-flex--horizontal flex-align--top">
							<div class="flex-1">
								<table class="table-data table-border--dashed width-full">
									<caption class="text-left">{{ $t('Zone Info') }}</caption>
									<tr>
										<td>{{ $t('Name') }}</td>
										<td><strong>{{ state.zoneInfo?.name }}</strong></td>
									</tr>
									<tr>
										<td>{{ $t('Position') }}</td>
										<td>
											<p v-if="state.zoneInfo">
												<span>
													({{ (state.zoneInfo?.fromKmNumber ?? 0).toString().concat(' Km', '+',
														(state.zoneInfo?.fromMetNumber ?? 0))}}
												</span>
												<span>-</span>
												<span>
													{{ (state.zoneInfo?.toKmNumber ?? 0).toString().concat(' Km', '+',
														(state.zoneInfo?.toMetNumber ?? 0))}})
												</span>
											</p>
										</td>
									</tr>
									<tr>
										<td>{{ $t('Zone Type') }}</td>
										<td>
											{{ state.zoneTypeList?.find(item => item.code == state.zoneInfo.type)?.name
											}}
										</td>
									</tr>
									<tr>
										<td>{{ $t('Camera') }}</td>
										<td>
											<template v-if="state.zoneInfo" v-for="i in 5">
												<el-button :icon="VideoCamera" type="primary"
													@click="showCameraPopup('Camera')">Camera {{ i }}</el-button>
												<template v-if="i % 3 == 0"><br><br></template>
											</template>
										</td>
									</tr>
								</table>
							</div>
							<div class="flex-1">
								<table class="table-data table-border--dashed width-full">
									<caption class="text-left">{{ $t('Traffic summary') }}</caption>
									<tr>
										<td>{{ $t('Vehicle Count') }}</td>
										<td>123</td>
									</tr>
									<tr>
										<td>{{ $t('Average Speed') }}</td>
										<td>123 Km/h</td>
									</tr>
									<tr>
										<td>{{ $t('Interval (sec)') }}</td>
										<td>60</td>
									</tr>
									<tr>
										<td>{{ $t('Traffic Timestamp') }}</td>
										<td>{{ useDateFormat(new Date(), DateTimeFormatEnum.DateTimeDefault) }}</td>
									</tr>
								</table>
								<table class="table-data table-border--dashed width-full">
									<caption class="text-left">{{ state.incident?.title }}</caption>

									<tr v-for="(name, index) in state.incident?.names" :key="index">
										<th style="width: 130px;">{{ $t('Incident') }} {{ index + 1 }}:</th>
										<td>{{ name }}</td>
									</tr>

								</table>
							</div>
						</div>

					</el-tab-pane>
					<el-tab-pane name="volumeTab" :label="$t('fe.tmsTrafficData.title.volumeChart')">
						<div v-if="state.isLoading" style="text-align: center;">
							<el-button type="primary" loading text>{{ $t('fe.base.text.loading') }}</el-button>
							<br><br>
						</div>
						<div v-else class="display-flex--horizontal flex-align--top">
							<div class="flex-1">
								<lineChartComp :chartOptions="lineChartOptions" />
							</div>
							<div class="flex-1">
								<div v-if="state.directionList?.length" class="display-flex--horizontal"
									style="gap: 20px; justify-content: center;">
									<table class="table-data table-border" v-for="dir in state.directionList">
										<tr class="header">
											<td colspan="100">{{ dir.DirectionName }}</td>
										</tr>
										<tr class="caption text-center">
											<th>{{ $t('fe.tmsTrafficData.title.type') }}</th>
											<th>{{ $t('fe.tmsTrafficData.title.oneHour') }}</th>
											<th>{{ $t('fe.tmsTrafficData.title.oneDay') }}</th>
										</tr>
										<tr v-for="(vt, idx) in state.vehicleTypeList"
											:class="idx % 2 != 0 ? 'alt' : ''">
											<td>
												<el-tooltip placement="right">
													<template #content>{{ vt.name }}</template>
													<img v-if="vt.value" :src="vt.value" class="icon-vehicle" />
													<p v-else>{{ vt.name?.substring(0, 9) + (vt.name?.length > 10 ?
														'...' : '') }}</p>
												</el-tooltip>
											</td>
											<td class="text-center">
												{{ getVehicleCountForOneHour(dir.DirectionCode, '', vt.code) }}
											</td>
											<td class="text-center">
												{{ getVehicleCountForOneDay(dir.DirectionCode, '', vt.code) }}
											</td>
										</tr>
										<tr class="footer">
											<th>{{ $t('fe.tmsTrafficData.title.total') }}</th>
											<th class="text-center">{{ getVehicleCountForOneHour(dir.DirectionCode) }}
											</th>
											<th class="text-center">{{ getVehicleCountForOneDay(dir.DirectionCode) }}
											</th>
										</tr>
									</table>
								</div>
								<div v-else>
									<el-empty :image-size="100" />
								</div>
							</div>
						</div>
					</el-tab-pane>
					<el-tab-pane :label="$t('fe.tmsTrafficData.title.trafficStatus')">
						<div v-if="state.directionList?.length" class="flex-inline"
							style="gap: 30px; justify-content: center;">
							<div class="display-flex--horizontal">
								<div v-for="dir in state.directionList">
									<gaugeChartComp :value="getTrafficStatus(dir.DirectionCode)" />
									<p class="text-uppercase text-center">{{ dir.DirectionName }}</p>
									<p class="text-center">{{ getTrafficStatus(dir.DirectionCode) }}Km/h</p>
								</div>
							</div>
						</div>
						<div v-else>
							<el-empty :image-size="100" />
						</div>
					</el-tab-pane>
				</el-tabs>
			</div>
		</div>
	</div>
	<cameraPopupComp ref="cameraPopupCompRef" />
</template>

<script lang="ts" setup>
const cameraPopupComp = defineAsyncComponent(() => import('/@/views/modal/common/cameraPopup.vue'));
const cameraPopupCompRef = ref<InstanceType<typeof cameraPopupComp>>();

// Import libraries
import { onMounted, reactive, ref } from 'vue';
import { getAPI } from '/@/utils/axios-utils';
import { useI18n } from 'vue-i18n';
import { useDateFormat } from '@vueuse/core';
import { VideoCamera } from '@element-plus/icons-vue'

// Import API and models
import { TmsIncidentApi, TmsZoneApi, TmsTrafficInfoApi } from '/@/api-services/tms';
import { VdsTrafficDataApi } from '/@/api-services/vds';
import { vdsTimestamp } from '/@/utils/vdsTimestamp';
import { vehicleCountField } from '/@/views/bigScreen/trafficInfo/component/vehicleCountField';
import { SysConfigDataApi } from '/@/api-services/cfgSystem';

// Import components
import gaugeChartComp from './charts/gaugeChart.vue';

// Importance: pnpm install echarts vue-echarts
import lineChartComp from './charts/lineChart.vue';

// Incident component
// Do not use asynchronous for importing this component
import { defineAsyncComponent } from 'vue';
import { DateTimeFormatEnum, UploadAssetItemsEnum } from '/@/types/enums/baseEnum';
import { ElMessage } from 'element-plus';

const incidentButtonsComp = defineAsyncComponent(() => import('./incidentButtons.vue'));

const { t: $t } = useI18n();

const curActiveTab = ref('summaryTab');

const props = defineProps({
	popupStyle: {
		type: String,
		default: 'width: 60%'
	}
});

// Declare local viewbag
const state = reactive({
	title: 'Title',
	showMe: false,
	isLoading: false,
	incident: [],
	zoneInfo: [],
	ZoneListData: [],
	zoneTypeList: [],
	statusList: [],
	directionList: [], // Mảng chứa danh sách hướng direction
	vehicleTypeList: [], // Mảng chứa danh sách phân loại xe, ex: xe dài dưới 6m, dài từ 6m đến 12m, trên 12m
	hourListForToDay: [], // Mảng chứa danh sách thời gian của ngày hiện tại tính từ 00:00:000 đến hiện tại
	trafficError: '',
	trafficDataForHourAgo: [], // Mảng chứa danh sách dữ liệu traffic của giờ vừa qua tính từ thời điểm hiện tại
	trafficDataForToDay: [], // Đối tượng chứa danh sách dữ liệu traffic của ngày hiện tại tính từ 00:00:00 đến thời điểm hiện tại
	trafficDataAverage: [] // Mảng chứa danh sách dữ liệu tốc độ trung bình speed-average mới nhất
});

const lineChartOptions = ref({
	title: {
		text: ''
	},
	tooltip: {
		trigger: 'axis'
	},
	grid: {
		bottom: '10%',
	},
	legend: {
		data: ['id_1', 'id_2'],
		formatter: function (id) { return id == 'id_1' ? 'Name 1' : 'Name 2'; }
	},
	xAxis: {
		type: 'category',
		name: 'X-Name',
		data: ['x1', 'x2', 'x3']
	},
	yAxis: {
		type: 'value',
		name: 'Y-Name',
		data: [1, 2, 3, 4]
	},
	series: [
		{
			name: 'id_1',
			type: 'line',
			data: [5, 10, 15, 20]
		},
		{
			name: 'id_2',
			type: 'line',
			data: [4, 16, 20, 15]
		}
	]
});

// Page initialization
onMounted(async () => {
	let response = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet('status_type');
	state.statusList = response.data.result ? response.data.result : [{ code: '0', name: 'Disable' }, { code: '1', name: 'Enable' }];

	response = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet('tms_zone_type');
	state.zoneTypeList = response.data.result;

	response = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet('vehicle_type');
	state.vehicleTypeList = response.data.result;

	response = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet('direction_config_type');
	state.directionList = response.data.result?.map(item => ({ DirectionCode: item.code, DirectionName: item.name }));

	response = await getAPI(TmsZoneApi).apiTmsTmszoneListGet();
	state.ZoneListData = response.data.result;

});

// Get the vehicle count in the past hour
const getVehicleCountForOneHour = function (direction: String = '', lane: String = '', vehicleType: String = '') {
	if (isNullOrEmpty(state.trafficDataForHourAgo))
		return state.trafficError ? '—' : 0;

	lane = lane.trim().toLowerCase();
	direction = direction.trim().toLowerCase();
	vehicleType = vehicleCountField(state.vehicleTypeList.find(item => String(item.code) === String(vehicleType)) ?? { code: vehicleType }) ?? vehicleType.trim().toLowerCase();

	let result = state.trafficDataForHourAgo;

	if (direction.length > 0) {
		result = result.filter(item => item.direction != null && item.direction.trim().toLowerCase() == direction);
	}

	if (lane.length > 0) {
		result = result.filter(item => item.lane != null && item.lane.trim().toLowerCase() == lane);
	}

	if (vehicleType.length > 0) {
		result = result.filter(item => item.vehicleType != null && item.vehicleType.trim().toLowerCase() == vehicleType);
	}

	return result.length > 0 ? result.reduce((calc, item) => { return calc + (item.vehicleCount ?? 0); }, 0) : 0;
};

// Get the vehicle count in the past day
const getVehicleCountForOneDay = function (direction: String = '', lane: String = '', vehicleType: String = '') {
	if (isNullOrEmpty(state.trafficDataForToDay))
		return state.trafficError ? '—' : 0;

	lane = lane.trim().toLowerCase();
	direction = direction.trim().toLowerCase();
	vehicleType = vehicleCountField(state.vehicleTypeList.find(item => String(item.code) === String(vehicleType)) ?? { code: vehicleType }) ?? vehicleType.trim().toLowerCase();

	let result = state.trafficDataForToDay;

	if (direction.length > 0) {
		result = result.filter(item => item.direction != null && item.direction.trim().toLowerCase() == direction);
	}

	if (lane.length > 0) {
		result = result.filter(item => item.lane != null && item.lane.trim().toLowerCase() == lane);
	}

	if (vehicleType.length > 0) {
		result = result.filter(item => item.vehicleType != null && item.vehicleType.trim().toLowerCase() == vehicleType);
	}

	return result.length > 0 ? result.reduce((calc, item) => { return calc + (item.vehicleCount ?? 0); }, 0) : 0;
};

// Load traffic data by zoneId
let trafficRequestId = 0;
const loadTrafficDataByZone = async () => {
	const zoneId = state.zoneInfo?.id;

	if (!zoneId)
		return;

	// Display loading status
	state.isLoading = true;

 const requestId = ++trafficRequestId;
 state.trafficDataForToDay = []; state.trafficDataForHourAgo = []; state.trafficDataAverage = []; state.trafficError = '';
 try {
  const toDate = new Date();
  const fromDate = new Date(toDate); fromDate.setHours(0, 0, 0, 0);
  const api = getAPI(VdsTrafficDataApi);
  const [today, hour, average, incidents] = await Promise.all([
   api.apiVdsVdstrafficdataVolumebyhourPost({ zoneId, fromDate, toDate }),
   api.apiVdsVdstrafficdataVolumebyhourPost({ zoneId, fromDate: new Date(toDate.getTime() - 3600000), toDate }),
   api.apiVdsVdstrafficdataAveragePost({ zoneId, fromDate: new Date(toDate.getTime() - 3600000), toDate }),
   getAPI(TmsIncidentApi).apiTmsTmsincidentListPost({ zoneId }),
  ]);
  if (requestId !== trafficRequestId || state.zoneInfo?.id !== zoneId) return;
  const rows = response => (response.data.result ?? []).map(row => ({ ...row,
   vehicleType: row.vehicleSize,
   detectHour: row.bucketTime ? new Date(vdsTimestamp(row.bucketTime)).getHours() : row.detectHour,
  }));
  state.trafficDataForToDay = rows(today);
  state.trafficDataForHourAgo = rows(hour);
  state.trafficDataAverage = (average.data.result ?? []).map(row => ({ ...row, averageSpeed: row.avgSpeed }));
  state.incidentList = incidents.data.result ?? [];
  state.trafficError = '';
 } catch (error) {
  if (requestId === trafficRequestId) {
   state.trafficDataForToDay = []; state.trafficDataForHourAgo = []; state.trafficDataAverage = [];
   state.trafficError = 'Không tải được dữ liệu giao thông VDS';
  }
  console.error('[trafficInfoPopup] VDS query failed', error);
 } finally { if (requestId === trafficRequestId) state.isLoading = false; }

};

// Check null or empty
const isNullOrEmpty = (input: any) => {
	if (input === undefined || input == null)
		return true;

	const type = typeof input;

	if (type.toLowerCase() == 'string')
		return input.trim().length == 0;

	if (type.toLowerCase() == 'object' && Array.isArray(input))
		return input.length == 0;

	return false;
};

// Update the directionList
const updateDirectionList = () => {
	// Combine the directionList with the actualDirectionList
	if (state.trafficDataForToDay?.length > 0) {
		let actualDirectionList = state.trafficDataForToDay?.map(item => item.direction) // Trích xuất Direction
			.filter((value, index, self) => self.indexOf(value) === index); // Distinct Direction

		actualDirectionList = actualDirectionList.map(aDirCode => {
			const dirObj = state.directionList?.find(dir => dir.DirectionCode == aDirCode);
			return { DirectionCode: aDirCode, DirectionName: (dirObj?.DirectionName ? dirObj.DirectionName : 'Dir-' + aDirCode) };
		});

		state.directionList?.forEach(dir => {
			if (!actualDirectionList?.find(aDir => aDir.DirectionCode == dir.DirectionCode)) {
				actualDirectionList.push({ DirectionCode: dir.DirectionCode, DirectionName: dir.DirectionName });
			}
		});

		// Update the directionList
		state.directionList = actualDirectionList;
	}
};

// Generate data for yAxis of vehicle-count chart
const buildDataForYAxisChart = () => {
	if (isNullOrEmpty(state.trafficDataForToDay) || isNullOrEmpty(state.directionList) || isNullOrEmpty(state.hourListForToDay))
		return null;

	// Generate hourly data for each direction
	const resultList = [];
	state.directionList?.forEach(dir => {
		const dataList = [];
		state.hourListForToDay?.forEach(hour => {
			const vehicleCount = state.trafficDataForToDay?.filter(td => td.direction == dir.DirectionCode && td.detectHour == hour)?.reduce((calc, td) => { return calc + (td.vehicleCount ?? 0); }, 0);

			dataList.push({ Hour: hour, VehicleCount: vehicleCount });
		});

		resultList.push({ DirectionCode: dir.DirectionCode, DirectionName: dir.DirectionName, Data: dataList });
	});

	return resultList;
};

// Generate data for xAxis of vehicle-count chart
const buildDataForXAxisChart = () => {
	if (isNullOrEmpty(state.trafficDataForToDay))
		return null;

	state.hourListForToDay = state.trafficDataForToDay?.map(item => item.detectHour) // Trích xuất DetectHour
		.filter((value, index, self) => self.indexOf(value) === index); // Distinct DetectHour

	return state.hourListForToDay;
};

// Get the status of the traffic
const getTrafficStatus = function (direction: String = '', lane: String = '') {
	if (isNullOrEmpty(state.trafficDataAverage))
		return state.trafficError ? '—' : 0;

	direction = direction.trim().toLowerCase();
	lane = lane.trim().toLowerCase();

	let result = state.trafficDataAverage;

	const zone = state.zoneInfo?.id ?? '';
	if (zone.length > 0) {
		result = result.filter(item => item.zone != null && item.zone.trim().toLowerCase() == zone);
	}

	if (direction.length > 0) {
		result = result.filter(item => item.direction != null && item.direction.trim().toLowerCase() == direction);
	}

	if (lane.length > 0) {
		result = result.filter(item => item.lane != null && item.lane.trim().toLowerCase() == lane);
	}

	const valid = result.filter(item => item.averageSpeed != null && item.totalVehicle > 0);
	const count = valid.reduce((sum, item) => sum + item.totalVehicle, 0);
	return count ? Math.round(valid.reduce((sum, item) => sum + item.averageSpeed * item.totalVehicle, 0) / count) : '—';
};

// Reset to default for the SpeedChart
const resetSpeedChartToDefault = () => {
	// Set titles for x-axis and y-axis of the SpeedChart
	lineChartOptions.value.xAxis.name = $t('fe.tmsTrafficData.title.volumeChartXName');
	lineChartOptions.value.yAxis.name = $t('fe.tmsTrafficData.title.volumeChartYName');

	// Set data to empty for the SpeedChart
	lineChartOptions.value.legend.data = [];
	lineChartOptions.value.xAxis.data = [];
	lineChartOptions.value.series?.forEach(ser => { ser.data = []; });
};

// Binding data to VolumeChart
const bindingDataToVolumeChart = () => {
	const zoneId = state.zoneInfo?.id ?? '';
	if (!zoneId)
		return;

	const xAxisData = buildDataForXAxisChart();
	const yAxisData = buildDataForYAxisChart();

	if (isNullOrEmpty(xAxisData) || isNullOrEmpty(yAxisData))
		return;

	// Set data for x-axis
	lineChartOptions.value.xAxis.data = xAxisData;

	// Set data for y-axis
	lineChartOptions.value.legend.data = yAxisData?.map(item => item.DirectionCode);
	lineChartOptions.value.legend.formatter = (dirCode) => {
		const dirObj = state.directionList?.find(dir => dir.DirectionCode == dirCode);
		return dirObj ? dirObj.DirectionName : dirCode;
	};

	lineChartOptions.value.series = yAxisData?.map(item => ({
		type: 'line',
		name: item.DirectionCode,
		data: item.Data?.map(record => record.VehicleCount)
	}));
};

const onTabChange = (activeTabName: string) => {
};

// Show or hide the popup
const toggleTrafficDataPopup = async (isShow: Boolean, data: any) => {
	// Show or hide instantly
	state.showMe = isShow;
	// Proceed to load the data
	let response = await getAPI(TmsZoneApi).apiTmsTmszoneListzonebyidsGet((data?.objectId ?? ''));
	state.zoneInfo = response.data.result ? response.data.result[0] : null;
	response = await getAPI(TmsTrafficInfoApi).apiTmsTmstrafficinfoCheckincidentPost();
	const incidentZoneData = response.data.result;

	if (Array.isArray(incidentZoneData)) {
		// Collect all incident names matching the zoneId
		const matchingNames = [];
		// Iterate over the array to find matching zoneIds
		incidentZoneData.forEach((incident: any) => {
			if (incident.zoneId === data.objectId) {
				matchingNames.push(incident.name); // Assuming `name` is the property to display
			}
		});
		const Zonename = state.ZoneListData.find(item => item.id == data.objectId);
		// Update yourData based on the result
		if (matchingNames.length > 0) {
			state.incident = {
				title: `Incidents found for ${Zonename.name}`,
				names: matchingNames // Join names into a single string
			};
		} else {
			state.incident = {
				title: `No incidents found for  ${Zonename.name}`,
				names: []
			};
		}
	} else {
		state.incident = {
			title: `Unexpected data format received`,
			names: []
		};
	}
	resetSpeedChartToDefault();

	if (state.zoneInfo) {
		// Wait for data loading to complete
		await loadTrafficDataByZone();

		// Update direction list
		updateDirectionList();

		// Start binding data to volume chart
		bindingDataToVolumeChart();


	}

};

const showCameraPopup = (cameraId: string) => {
	// DEMO
	cameraPopupCompRef.value?.openDialog(cameraId, 'Camera ' + cameraId, 1, 20);
	return;

	const camera = state.cameraList?.find(item => item.id == cameraId);

	if (camera)
		cameraPopupCompRef.value.openDialog(camera.id, camera.name, camera.kmNumber, camera.metNumber);
	else
		ElMessage.error($t('fe.base.text.noData'));
};

// Export toggleTrafficDataPopup
defineExpose({ toggleTrafficDataPopup });
</script>
<style lang="scss" scoped>
$_gray: #E5E5E5;
$_border_color: $_gray;

/* Display */
.display-flex {

	&--vertical,
	&--horizontal {
		display: flex;
		gap: 10px;
	}

	&--vertical {
		flex-direction: column;
	}

	&--horizontal {
		flex-direction: row;
		align-items: center;
	}
}

.flex {
	&-1 {
		flex: 1;
	}

	&-2 {
		flex: 2;
	}

	&-grow {
		&--1 {
			flex-grow: 1;
		}
	}

	&-justify {
		&--space-between {
			justify-content: space-between;
		}
	}

	&-align {
		&--top {
			align-items: start;
		}

		&--center {
			align-items: center;
		}
	}
}

/* Table */
.table-data {
	border-collapse: collapse;

	caption {
		text-transform: uppercase;
		font-weight: bold;
		border-bottom: thin solid $_border_color;
	}

	th,
	td {
		padding: 5px;
	}
}

.table {
	&-border {
		border: thin solid $_border_color;

		tr:not(:last-child) th,
		td {
			border-bottom: thin solid $_border_color;
		}

		&--dashed tr:not(:last-child) {

			th,
			td {
				border-bottom: thin dashed $_border_color;
			}
		}
	}
}

/* Canvas */
#livecam-container {
	background-color: black !important;
	display: flex;
	align-items: center;
	justify-content: center;

	canvas {
		width: 100%;
		height: auto;
		max-width: 100%;
		max-height: 100%;
	}
}

/* Simple Popup */
.simple-popup {
	pointer-events: auto;
	position: absolute;
	top: 0px;
	left: 0px;
	display: flex;
	justify-content: center;
	align-items: center;
	z-index: 1080 !important;
	background-color: rgba(0, 0, 0, 0.5);
	width: 100%;
	height: 100%;

	&--container {
		display: flex;
		flex-direction: column;
		overflow: hidden;
		width: fit-content;
		height: 80%;
		min-width: 40%;
		min-height: 80%;
		max-width: 80%;
		max-height: 80%;
		background-color: white;
		border-radius: 5px;
	}

	&.panel-header {
		height: 40px;
		max-height: 40px;
	}

	&.panel-body {
		flex-grow: 1;
	}
}

/* Button close */
.button-close {
	position: absolute;
	top: 5px;
	right: 5px;

	&::before {
		display: block;
		position: absolute;
		left: 0;
		top: 0;
		width: 100%;
		height: 100%;
		display: flex;
		justify-content: center;
		align-items: center;
		content: '\00d7';
		font-weight: lighter;
		font-size: 20px;
	}
}

/* Position */
.position {
	&-relative {
		position: relative;
	}

	&-absolute {
		position: absolute
	}
}

/* Style */
.bg {
	&-gray {
		background-color: $_gray;
	}

	&-white {
		background-color: white;
	}
}

.text {
	&-left {
		text-align: left;
	}

	&-center {
		text-align: center;
	}

	&-right {
		text-align: right;
	}

	&-uppercase {
		text-transform: uppercase;
	}
}

.padding {
	&-5px {
		padding: 5px;
	}

	&-10px {
		padding: 10px;
	}

	&-15px {
		padding: 15px;
	}
}

.width {
	&-fit {
		width: fit-content
	}

	&-full {
		width: 100%;
	}
}

/* Other */
.icon-vehicle {
	max-width: 80px;
	max-height: 35px;
}
</style>