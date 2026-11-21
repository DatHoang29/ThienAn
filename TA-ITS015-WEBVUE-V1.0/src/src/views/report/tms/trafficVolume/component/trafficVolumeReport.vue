<template>
	<!-- Một loại báo cáo lưu lượng = một tab menu (views/tabPage); loại do tab truyền vào qua prop reportType. -->
	<WorkspacePage>
	<div class="rpt-traffic-volume-container">
		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true"	label-width="auto" style="flex: 1 1 0%">
				<el-row :gutter="10">
					<el-col v-show="state.isDayReport" class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.tmsTrafficInfoReport.shift')">
							<el-select v-model="state.queryParams.workShiftCode" :placeholder="$t('lz.label.base.pleaseSelect', {input: $t('lz.label.tmsTrafficInfoReport.shift')})">
								<el-option v-for="item in state.workShiftList" :label="$t(`lz.param.${item.groupCode}.${item.code}`,item.name)" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col v-show="state.isDayReport" class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.base.date')">
							<el-date-picker
								v-model="state.queryParams.valueDate"
								type="date"
								:format="DateTimeFormatEnum.DateDefault"
								:placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.label.base.date')})"
								:clearable="false"
								class="w100"
							/>
						</el-form-item>
					</el-col>
					<el-col v-show="state.isMonthReport" class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.base.month')">
							<el-select v-model="state.queryParams.month" :placeholder="$t('lz.label.base.month')" class="width-100pc">
								<el-option
									v-for="month in [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12]"
									:label="`${$t('lz.label.base.month')} ${month.toString().padStart(2, '0')}`"
									:value="month"
								/>
							</el-select>
						</el-form-item>
					</el-col>
					<el-col v-show="state.isMonthReport" class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.base.year')">
							<el-select v-model="state.queryParams.year" :placeholder="$t('lz.label.base.year')" class="width-100pc">
								<el-option
									v-for="year in yearOptions"
									:key="year"
									:label="`${$t('lz.label.base.year')} ${year}`"
									:value="year"
								/>
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>

			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />

			<el-row>
				<el-col>
					<SearchButton authSearch="tmsRptTrafficVolume:list" @search="handleQuery" @reset="resetQuery" :loading="state.isProcessing" />
				</el-col>
			</el-row>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<ReportViewer
				ref="reportViewerRef"
				:pdf-source="state.pdf"
				:loading="state.isProcessing"
				:format-options="getConfigDataByCode(BaseConfigTypeEnum.RptExportFormat)"
				:default-format="state.queryParams.format"
				:export-file-name="reportFileName"
				export-auth="tmsRptTrafficVolume:export"
				print-auth="tmsRptTrafficVolume:print"
				@export="handleExport"
			/>
		</el-card>
	</div>
	</WorkspacePage>
</template>

<script lang="ts" setup name="tmsRptTrafficVolumeReport">
// Author: KienLT
// Description: Nội dung báo cáo lưu lượng giao thông cho một loại báo cáo (prop reportType). Tách từ trang index cũ để mỗi
// loại báo cáo là một tab menu (thư mục tabs) dùng chung phần lọc, kiểm tra điều kiện và khung xem báo cáo này.
// Created date: 13/11/2026
import { onMounted, reactive, ref, defineAsyncComponent, computed } from 'vue';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { useDateFormat, useNow } from '@vueuse/core';

import { saveAs } from 'file-saver';

import { DateTimeFormatEnum, BaseOpConfigCodeEnum } from '/@/types/enums/baseEnum';
import { TmsTrafficDataReportApi } from '/@/api-services/tmsReport';
import { TmsTrafficDataReportTypeEnum } from '/@/types/enums';

import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';

const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const getOpConfigListByGroupCode = useBaseConfig().getOpConfigListByGroupCode;

import WorkspacePage from '/@/components/workspace/WorkspacePage.vue';

/** Mã loại báo cáo (SysConfigData nhóm traffic_volume_report_type) — mỗi tab menu truyền một mã. */
const props = defineProps<{ reportType: string }>();

const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const { t: $t } = useI18n();
const now = useNow();
// Report viewer ref
const ReportViewer = defineAsyncComponent(() => import('/@/components/advanced/report-viewer.vue'));
const reportViewerRef = ref<InstanceType<typeof ReportViewer>>();


const state = reactive({
	isProcessing: false,
	exporting: false,
	isDayReport: true,
	isMonthReport: false,
	pdfData: null,
	exportFormat: null,
	pdf: null as any,
	// queryParams: {
	// 	workShift: null,
	// 	valueDate: null,
	// 	month: null,
	// 	year: null,
	// 	reportType: null,
	// },
	queryParams: {
		workShiftCode: null as any,
		valueDate: null as any,
		month: null as any,
		year: null as any,
		reportType: null as any,
		reportDataId: null as any,
		format: 'pdf' as any,
		workShiftName: null as any,
		workShiftValue: null as any,
	} , 
	workShiftList: [] as any,
});
/** Năm bắt đầu có dữ liệu báo cáo. Danh sách năm chạy từ đây tới năm hiện tại để báo cáo cũ không bị mất khỏi ô chọn theo thời gian. */
const REPORT_START_YEAR = 2024;
const yearOptions = computed(() => {
	const currentYear = now.value.getFullYear();
	// Đồng hồ máy sai lùi về trước năm bắt đầu vẫn còn ít nhất một năm để chọn.
	return Array.from({ length: Math.max(currentYear - REPORT_START_YEAR, 0) + 1 }, (_, index) => REPORT_START_YEAR + index);
});

//Tên file báo cáo khi export
const reportFileName = computed(() => {
	return 'TrafficVolumeReport_' + useDateFormat(useNow(), 'YYYYMMDD_HHmmss').value;
});
// Page initialization
onMounted(async () => {
	state.queryParams.reportType = props.reportType;
	// Nạp danh sách ca / khoảng thời gian của loại này rồi đặt điều kiện mặc định.
	await handleReportTypeChange();
});

// Query operation
const handleQuery = async () => {
	try{
		let response = null;
		const workShiftInfo = state.workShiftList?.find((item:any) => item.code == state.queryParams.workShiftCode) ?? null as any;

		// Thiếu điều kiện bắt buộc ⇒ báo rõ trường còn thiếu (trước đây thoát im lặng, người dùng tưởng nút Tìm kiếm hỏng).
		const missingField = !state.queryParams.reportType ? $t('lz.entity.report.type')
			: isSelectedDayReport()
				? (workShiftInfo == null ? $t('lz.label.tmsTrafficInfoReport.shift') : !state.queryParams.valueDate ? $t('lz.label.base.date') : null)
				: (!state.queryParams.month ? $t('lz.label.base.month') : !state.queryParams.year ? $t('lz.label.base.year') : null);
		if (missingField) {
			ElMessage.warning($t('lz.validation.base.required', { PropertyName: missingField }));
			return;
		}
		
		state.isProcessing = true;
		state.pdf = null;

		state.queryParams.reportDataId = generateDataId();
		state.queryParams.workShiftName = workShiftInfo?.name;
		state.queryParams.workShiftValue = workShiftInfo?.value;
		// state.queryParams.valueDate = commonVarsAndFuncs.getISODatetimeString(state.queryParams.valueDate);

		if(isSelectedDayReport()){
			response = await getAPI(TmsTrafficDataReportApi).apiTmsreportTmstrafficdatareportRenderlistbydayPost(
				state.queryParams,
				{ responseType: 'blob' }
			);
		}
		else{
			response = await getAPI(TmsTrafficDataReportApi).apiTmsreportTmstrafficdatareportRenderlistbymonthPost(
				state.queryParams,
				{ responseType: 'blob' }
			);
		}

		state.pdf = URL.createObjectURL(new Blob([response.data as any]));

	}
	catch(error){
		console.error('handleQuery(...)', error);
	}
	finally{
		state.isProcessing = false;
	}
};

// Export operation
const handleExport = async (format: string) => {
	try {
		let response = null;
		state.exporting = true;
		state.queryParams.format = format;
		const workShiftInfo = state.workShiftList?.find(item => item.code == state.queryParams.workShiftCode) ?? null as any;
		state.queryParams.workShiftName = workShiftInfo?.name;
		state.queryParams.workShiftValue = workShiftInfo?.value;
		if(isSelectedDayReport()){
			response = await getAPI(TmsTrafficDataReportApi).apiTmsreportTmstrafficdatareportExportlistbydayPost(
				state.queryParams,
				{ responseType: 'blob' }
			);
		}
		else{
			response = await getAPI(TmsTrafficDataReportApi).apiTmsreportTmstrafficdatareportExportlistbymonthPost(
				state.queryParams,
				{ responseType: 'blob' }
			);
		}

		// Get the filename from the Content-Disposition header
		let filename = `${reportFileName.value}.${state.queryParams.format}`; // Use the selected format for the filename
		// Use FileSaver to save the file
		saveAs(new Blob([response.data as any], { type: response.headers['content-type'] }), filename);

		ElMessage.success($t('lz.message.base.exportSuccess'));
	} catch (error) {
		if (error?.response?.status === 0) {
		return;
		} else {
		console.error('Error exporting file:', error);
		ElMessage.error($t('lz.message.base.exportFailed'));
		}
	} finally {
		state.exporting = false;
		reportViewerRef.value?.setExporting(false);
	}
};

// const handlePrint = async () => {
// 	//api export pdf
// 	state.queryParams.format = 'pdf';
	
// 	let response = null;
// 	const workShiftInfo = state.workShiftList?.find(item => item.code == state.queryParams.workShiftCode) ?? null as any;
// 	state.queryParams.workShiftName = workShiftInfo?.name;
// 	state.queryParams.workShiftValue = workShiftInfo?.value;
// 	if(isSelectedDayReport()){
// 		response = await getAPI(TmsTrafficDataReportApi).apiTmsreportTmstrafficdatareportExportlistbydayPost(
// 			state.queryParams,
// 			{ responseType: 'blob' }
// 		);
// 	}
// 	else{
// 		response = await getAPI(TmsTrafficDataReportApi).apiTmsreportTmstrafficdatareportExportlistbymonthPost(
// 			state.queryParams,
// 			{ responseType: 'blob' }
// 		);
// 	}
// 	const blob = new Blob([response.data as any], { type: response.headers['content-type'] });
// 	const url = URL.createObjectURL(blob);
// 	const printWindow = window.open(url, '_blank');
// 	printWindow?.print();
// 	URL.revokeObjectURL(url);	
// };


//generate unique report data ID
const generateDataId = () => {
	const dateStr = useDateFormat(useNow(), 'YYYYMMDD'); // yyyyMMdd
	const guidStr = 'xxxxxxxxxxxx4xxxyxxxxxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
		const r = Math.random() * 16 | 0, v = c === 'x' ? r : (r & 0x3 | 0x8);
		return v.toString(16);
	});

	return `${dateStr.value}_${guidStr}`;
};

// Handle report type change
const handleReportTypeChange = async() => {
	if(state.queryParams.reportType?.trim().toLowerCase() == TmsTrafficDataReportTypeEnum.TraffiDayReportByTime.trim().toLowerCase()){
		state.isDayReport = true;
		state.isMonthReport = false;

		// state.workShiftList = await commonVarsAndFuncs.getOperationConfigList(BaseOpConfigCodeEnum.DayWorkTime);

		state.workShiftList = await getOpConfigListByGroupCode(BaseOpConfigCodeEnum.DayWorkTime);
		
	}
	else if(state.queryParams.reportType?.trim().toLowerCase() == TmsTrafficDataReportTypeEnum.TrafficDayReportByShift.trim().toLowerCase()){
		state.isDayReport = true;
		state.isMonthReport = false;

		// state.workShiftList = await commonVarsAndFuncs.getOperationConfigList(BaseOpConfigCodeEnum.DayWorkShift);

		state.workShiftList = await getOpConfigListByGroupCode(BaseOpConfigCodeEnum.DayWorkShift);
	}
	else {
		state.isDayReport = false;
		state.isMonthReport = true;

		// state.workShiftList = await commonVarsAndFuncs.getOperationConfigList(BaseOpConfigCodeEnum.DayWorkShift);

		state.workShiftList = await getOpConfigListByGroupCode(BaseOpConfigCodeEnum.DayWorkShift);
	}

	setDefaultData();
};

// Set default data for query parameters
const setDefaultData = async() => {
	state.queryParams.workShiftCode = state.workShiftList?.[0]?.code ?? null;
	state.queryParams.valueDate = new Date(new Date(now.value));

	state.queryParams.month = new Date().getMonth() + 1;
	state.queryParams.year = new Date().getFullYear();
};

const isSelectedDayReport = () => {
	return state.queryParams.reportType?.trim().toLowerCase() == TmsTrafficDataReportTypeEnum.TraffiDayReportByTime.trim().toLowerCase()
		|| state.queryParams.reportType?.trim().toLowerCase() == TmsTrafficDataReportTypeEnum.TrafficDayReportByShift.trim().toLowerCase();
};

// Reset operation
const resetQuery = async () => {
	// Giữ loại báo cáo (tab) đang chọn; chỉ đưa điều kiện về mặc định và bỏ bản xem trước.
	state.pdf = null;
	state.pdfData = null;
	await setDefaultData();
};
</script>
<style lang="scss" scoped>
.rpt-incident-container {
    height: 100%;
    display: flex;
    flex-direction: column;
  }
  
  .full-table {
    flex: 1;
    display: flex;
    flex-direction: column;
    overflow: hidden;
    min-height: 0;
  }

  /* Trang nằm trong WorkspacePage nên không còn khớp rule chung `.layout-parent > div:first-child .full-table`
     (theme/element.scss) — rule đó cho thân thẻ cao 100%. Thiếu nó, khung báo cáo chỉ cao bằng nội dung nên PDF
     hiện ra còn một dải thấp. Khai lại đúng phần đó cho thẻ báo cáo. */
  .full-table :deep(.el-card__body) {
    height: 100%;
    display: flex;
    flex-direction: column;
    padding: 5px 5px 8px 5px;
  }
  
  .pdf-container {
    flex: 1;
    overflow: auto;
  }
  
  :deep(.vue-office-pdf) {
    width: 100%;
    height: 100%;
  }
  
  .export-container {
    display: flex;
    align-items: center; /* Align items vertically in the center */
  }
  
  .export-select {
    width: 120px; /* Set a fixed width for the dropdown */
    margin-right: 10px; /* Add some space between the dropdown and the button */
  }
</style>
