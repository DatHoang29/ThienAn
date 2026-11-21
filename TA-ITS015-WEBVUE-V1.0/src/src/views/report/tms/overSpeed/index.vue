<template>
	<div class="rpt-acs-container">
		<!-- Thẻ tìm kiếm thu gọn / kéo đổi chiều cao được như các trang danh sách khác (GridSearchPanel). -->
		<GridSearchPanel id="tms-rpt-over-speed-search">
			<template #filters>
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-position="left" style="flex: 1 1 0%" label-width="auto">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.base.fromDate')">
							<DatePicker v-model="state.queryParams.fromDate" dateFormat="dd/mm/yy" showTime showSeconds
									size="small"
									class="ta-input-size"
									style="width: 100%;"
									:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.label.base.fromDate') })" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.base.toDate')">
							<DatePicker v-model="state.queryParams.toDate" dateFormat="dd/mm/yy" showTime showSeconds
									size="small"
									class="ta-input-size"
									style="width: 100%;"
									:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.label.base.toDate') })" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.tmsRptOverSpeed.zone')">
							<el-select v-model="state.queryParams.zoneIdList" filterable multiple collapse-tags collapse-tags-tooltip clearable class="w100"
									:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.label.tmsRptOverSpeed.zone') })">
								<el-option v-for="item in state.zoneList" :key="item.id" :label="item.name" :value="item.id" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.tmsTrafficStatistic.camera')">
							<el-select v-model="state.queryParams.equipmentIdList" filterable multiple collapse-tags collapse-tags-tooltip clearable class="w100"
									:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.label.tmsTrafficStatistic.camera') })">
								<el-option v-for="item in state.cameraList" :key="item.id" :label="item.name" :value="item.id" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.direction')">
							<el-select v-model="state.queryParams.direction" clearable class="w100"
									:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.direction') })">
								<el-option v-for="item in state.directionList" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.tmsRptOverSpeed.vehicleSize')">
							<el-select v-model="state.queryParams.vehicleSize" clearable class="w100"
									:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.label.tmsRptOverSpeed.vehicleSize') })">
								<el-option v-for="item in state.vehicleSizeList" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.licensePlate')">
							<el-input v-model="state.queryParams.licensePlate" clearable :maxlength="20"
									:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.tms.licensePlate') })"
									@keyup.enter="handleQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.tmsRptOverSpeed.minExceed')">
							<el-input-number v-model="state.queryParams.minExceedKmh" :min="0" :max="300" :step="5" controls-position="right" class="w100"
									:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.label.tmsRptOverSpeed.minExceed') })" />
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>

			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />

			<el-row>
				<el-col>
					<SearchButton :auth-search="'tmsRptOverSpeed:view'" @search="handleQuery" @reset="resetQuery" :loading="state.isProcessing"
								:title-search="$t('lz.button.acs.reportCreate')" />
				</el-col>
			</el-row>
			</template>

		<el-card class="full-table" shadow="hover">
			<ReportViewer
				ref="reportViewerRef"
				:pdf-source="state.pdf"
				:loading="state.isProcessing"
				:format-options="getConfigDataByCode(BaseConfigTypeEnum.RptExportFormat)"
				:default-format="state.format"
				:export-file-name="reportFileName"
				export-auth="tmsRptOverSpeed:export"
				print-auth="tmsRptOverSpeed:print"
				@export="handleExport"
			/>
		</el-card>
		</GridSearchPanel>
	</div>
</template>

<script lang="ts" setup name="tmsRptOverSpeed">
import GridSearchPanel from '/@/components/gridPage/GridSearchPanel.vue';
import { onMounted, reactive, ref, defineAsyncComponent, computed, watch } from 'vue';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { useDateFormat, useNow } from '@vueuse/core';
import { saveAs } from 'file-saver';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum, SysConfigTypeCodeEnum } from '/@/types/enums';
import DatePicker from 'primevue/datepicker';
import { OverSpeedRenderInput, TmsOverSpeedReportApi } from '/@/api-services/tmsReport';
import { TmsEquipmentApi, TmsEquipmentTypeApi, TmsZoneApi } from '/@/api-services/tms';
import { vehicleCountField } from '/@/views/bigScreen/trafficInfo/component/vehicleCountField';

/** Trần khoảng thời gian — khớp kiểm tra của backend (OverSpeedReportService.MaxRangeDays). */
const MAX_RANGE_DAYS = 31;

const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const ReportViewer = defineAsyncComponent(() => import('/@/components/advanced/report-viewer.vue'));
const reportViewerRef = ref<InstanceType<typeof ReportViewer>>();
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const { t: $t } = useI18n();
const now = useNow();

const state = reactive({
	isProcessing: false,
	pdf: null as any,
	format: 'pdf',
	// Id dữ liệu của lần xem gần nhất: export dùng lại để file tải về khớp đúng bản đang xem.
	reportDataId: null as string | null,
	queryParams: {
		fromDate: null as any,
		toDate: null as any,
		zoneIdList: [] as string[],
		equipmentIdList: [] as string[],
		direction: undefined as string | undefined,
		vehicleSize: undefined as string | undefined,
		licensePlate: undefined as string | undefined,
		minExceedKmh: undefined as number | undefined,
	},
	zoneList: [] as any[],
	cameraList: [] as any[],
	directionList: [] as any[],
	vehicleSizeList: [] as any[],
});

// Đổi bộ lọc sau khi xem thì bỏ cache: export phải dựng lại theo bộ lọc mới thay vì trả bản cũ.
watch(() => state.queryParams, () => (state.reportDataId = null), { deep: true });

onMounted(async () => {
	resetQuery();
	if (Object.keys(useBaseConfig().configList ?? {}).length === 0) await useBaseConfig().setConfigDataList();
	state.directionList = getConfigDataByCode(SysConfigTypeCodeEnum.TmsDirectionConfigCode) ?? [];
	// Backend chỉ lọc được cỡ short/medium/long (cột vehicleSize của VDS); nhãn lấy theo cấu hình loại xe.
	const configured = getConfigDataByCode(SysConfigTypeCodeEnum.TmsVehicleTypeConfigCode) ?? [];
	state.vehicleSizeList = ['short', 'medium', 'long'].map((code) => {
		const item = configured.find((item: any) => vehicleCountField(item) === code);
		return { code, name: item?.name ?? { short: 'S', medium: 'M', long: 'L' }[code] };
	});

	const [types, equipment, zones] = await Promise.all([
		getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet(),
		getAPI(TmsEquipmentApi).apiTmsTmsequipmentListGet(),
		getAPI(TmsZoneApi).apiTmsTmszoneListGet(),
	]);
	const typeIds = new Set((types.data.result ?? []).filter((item: any) => /VDS/i.test(String(item.code ?? ''))).map((item: any) => String(item.id)));
	state.cameraList = (equipment.data.result ?? []).filter((item: any) => typeIds.has(String(item.equipmentTypeId)));
	state.zoneList = (zones.data.result ?? []).filter((item: any) => item.type === 'zone');
});

function defaultRange() {
	const start = new Date(now.value);
	start.setHours(0, 0, 0, 0);
	const end = new Date(now.value);
	end.setHours(23, 59, 59, 999);
	return { start, end };
}

const reportFileName = computed(() => 'OverSpeedReport_' + useDateFormat(useNow(), 'YYYYMMDD_HHmmss').value);

/** Kiểm tra khoảng thời gian trước khi gọi API — backend vẫn kiểm lại. */
function validRange(): boolean {
	const { fromDate, toDate } = state.queryParams;
	const from = fromDate instanceof Date ? fromDate.getTime() : NaN;
	const to = toDate instanceof Date ? toDate.getTime() : NaN;
	if (!Number.isFinite(from) || !Number.isFinite(to) || from >= to || to - from > MAX_RANGE_DAYS * 86400000) {
		ElMessage.warning($t('lz.message.tmsRptOverSpeed.timeRangeInvalid', { days: MAX_RANGE_DAYS }));
		return false;
	}
	return true;
}

function buildInput(): OverSpeedRenderInput {
	const q = state.queryParams;
	return {
		reportDataId: state.reportDataId,
		fromDate: q.fromDate,
		toDate: q.toDate,
		zoneIdList: q.zoneIdList.length ? q.zoneIdList : null,
		equipmentIdList: q.equipmentIdList.length ? q.equipmentIdList : null,
		direction: q.direction || null,
		vehicleSize: q.vehicleSize || null,
		licensePlate: q.licensePlate?.trim() || null,
		minExceedKmh: q.minExceedKmh ?? null,
	};
}

const handleQuery = async () => {
	if (!validRange()) return;
	try {
		state.isProcessing = true;
		state.pdf = null;
		state.reportDataId = generateDataId();
		const response = await getAPI(TmsOverSpeedReportApi).apiTmsreportTmsoverspeedreportRenderlistPost(buildInput(), { responseType: 'blob' });
		state.pdf = URL.createObjectURL(new Blob([response.data as any]));
	} catch (error) {
		// Interceptor axios đã hiện thông báo lỗi (kể cả khi body là blob).
		console.error('handleQuery(...)', error);
	} finally {
		state.isProcessing = false;
	}
};

const handleExport = async (format: string) => {
	if (!validRange()) {
		reportViewerRef.value?.setExporting(false);
		return;
	}
	state.format = format;
	try {
		const response = await getAPI(TmsOverSpeedReportApi).apiTmsreportTmsoverspeedreportExportlistPost({ ...buildInput(), format }, { responseType: 'blob' });
		saveAs(new Blob([response.data as any], { type: response.headers['content-type'] }), `${reportFileName.value}.${format}`);
		ElMessage.success($t('lz.message.base.exportSuccess'));
	} catch (error: any) {
		if (error?.response?.status !== 0) console.error('Error exporting file:', error);
	} finally {
		reportViewerRef.value?.setExporting(false);
	}
};

const generateDataId = () => {
	const dateStr = useDateFormat(useNow(), 'YYYYMMDD');
	const guidStr = 'xxxxxxxxxxxx4xxxyxxxxxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
		const r = (Math.random() * 16) | 0,
			v = c === 'x' ? r : (r & 0x3) | 0x8;
		return v.toString(16);
	});
	return `${dateStr.value}_${guidStr}`;
};

const resetQuery = () => {
	const { start, end } = defaultRange();
	Object.assign(state.queryParams, {
		fromDate: start,
		toDate: end,
		zoneIdList: [],
		equipmentIdList: [],
		direction: undefined,
		vehicleSize: undefined,
		licensePlate: undefined,
		minExceedKmh: undefined,
	});
	state.reportDataId = null;
};
</script>

<style lang="scss" scoped>
.rpt-acs-container {
	height: 100%;
	display: flex;
	flex-direction: column;
}
.full-table {
	flex: 1;
	display: flex;
	flex-direction: column;
	overflow: hidden;
}
</style>
