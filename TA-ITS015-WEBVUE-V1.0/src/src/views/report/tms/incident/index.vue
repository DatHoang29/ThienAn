<template>
	<div class="rpt-acs-container">
		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true"	label-position="left" style="flex: 1 1 0%" label-width="auto" >
				<el-row :gutter="10">
					
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.base.fromDate')">
							<DatePicker v-model="state.queryParams.fromDate" dateFormat="dd/mm/yy" showTime showSeconds
									size="small" 
									class="ta-input-size" 
									style="width: 100%;"
									:placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.label.base.fromDate')})"/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.base.toDate')">
							<DatePicker v-model="state.queryParams.toDate" dateFormat="dd/mm/yy" showTime showSeconds
									size="small" 
									class="ta-input-size" 
									:placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.label.base.toDate')})"/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tmsIncident.eventTypeId')">
							<el-select v-model="state.queryParams.eventTypeId" :placeholder="$t('lz.label.base.pleaseSelect', {input: $t('lz.entity.tmsIncident.eventTypeId')})" :maxlength="EntityFieldLength.Length128" show-word-limit clearable>
								<el-option v-for="item in state.eventTypeList" :label="item.name" :value="item.id" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tmsIncident.source')">
							<el-select v-model="state.queryParams.source" :placeholder="$t('lz.label.base.pleaseSelect', {input: $t('lz.entity.tmsIncident.source')})" clearable>
								<el-option v-for="item in state.sourceDetectList" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					
				</el-row>
			</el-form>

			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />

			<el-row>
				<el-col>
					<SearchButton :auth-search="'tmsRptIncident:view'" @search="handleQuery" @reset="resetQuery" :loading="state.isProcessing"
								:title-search="$t('lz.button.acs.reportCreate')" />
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
				export-auth="tmsRptIncident:export"
				print-auth="tmsRptIncident:print"
				@export="handleExport"
			/>
		</el-card>
	</div>
</template>

<script lang="ts" setup name="tmsRptIncident">
import { onMounted, reactive, ref, defineAsyncComponent, computed } from 'vue';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { useDateFormat, useNow } from '@vueuse/core';
import { saveAs } from 'file-saver';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { TmsIncidentReportApi } from '/@/api-services/tmsReport';
import { TmsEventTypeApi } from '/@/api-services/tms';
import  DatePicker from 'primevue/datepicker';

import { EntityFieldLength } from '/@/types/enums/baseEnum';

const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
// Report viewer ref
const ReportViewer = defineAsyncComponent(() => import('/@/components/advanced/report-viewer.vue'));
const reportViewerRef = ref<InstanceType<typeof ReportViewer>>();

const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const getOpConfigItemByCode = useBaseConfig().getOpConfigItemByCode;
const { t: $t } = useI18n();
const now = useNow();

const state = reactive({
	isProcessing: false,
	pdf: null as any,
	exporting: false,
	queryParams: {
		eventTypeId: '' as any,
		source: '' as any,
		fromDate: dateForSearch().startTime as any,
		toDate: dateForSearch().endTime as any,
		reportDataId: null as any,
		format: 'pdf' as any,
	} , 
	sourceDetectList: [] as any,
	eventTypeList: [] as any,
});

// Page initialization
onMounted(async () => {
	// Set default values 
	resetQuery();
	let response = await getAPI(TmsEventTypeApi).apiTmsTmseventtypeListGet();
	state.eventTypeList = response.data.result;

	state.sourceDetectList = getConfigDataByCode(BaseConfigTypeEnum.TmsSourceDetectIncident);

});
function dateForSearch(): { startTime: any; endTime: any } {
	const start = new Date(new Date(now.value));
	// start.setMonth(start.getMonth() - 1);
	start.setDate(start.getDate() - 3)
	start.setHours(0, 0, 0, 0);

	const end = new Date(new Date(now.value).setHours(23, 59, 59, 999));
	return {
		startTime: start as any,
		endTime: end as any
	};
};
//Tên file báo cáo khi export
const reportFileName = computed(() => {
	return 'IncidentReport_' + useDateFormat(useNow(), 'YYYYMMDD_HHmmss').value;
});
// Query operation
const handleQuery = async () => {
	try{
		state.isProcessing = true;
		state.pdf = null;
		
		// state.queryParams.fromDate = dateForSearch().startTime;
		// state.queryParams.endTime = dateForSearch().endTime;
		state.queryParams.reportDataId = generateDataId();
		const response = await getAPI(TmsIncidentReportApi).apiTmsreportTmsincidentreportRenderlistPost(
			state.queryParams,
			{ responseType: 'blob' }
		);
		const pdf = new Blob([response.data as any], { type: 'application/pdf' });
        if ((await pdf.slice(0, 5).text()) !== '%PDF-') throw new Error($t('lz.message.tmsIncident.reportLoadFailed'));
        state.pdf = URL.createObjectURL(pdf);

	}
	catch(error){
		console.error('handleQuery(...)', error);
        ElMessage.error($t('lz.message.tmsIncident.reportLoadFailed'));
	}
	finally{
		state.isProcessing = false;
	}
};

// Export operation
const handleExport = async (format: string) => {

	state.exporting = true;
	state.queryParams.format = format;
	try {
		const response = await getAPI(TmsIncidentReportApi).apiTmsreportTmsincidentreportExportlistPost(
			state.queryParams,
			{ responseType: 'blob' }
		);

		// Get the filename from the Content-Disposition header
		let filename = `${reportFileName.value}.${format}`; // Use the selected format for the filename
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
			reportViewerRef.value?.setExporting(false);
	}
};

// // const handlePrint = () => {
// // 	const printScale = getOpConfigItemByCode(BaseOpConfigTypeEnum.CommonConfig, BaseOpConfigItemTypeEnum.ReportPrintDpi);
// // 	pdfRef.value?.print(printScale.value,'',true);
// // };

// const handlePrint = async () => {
// 	//api export pdf
// 	state.queryParams.format = 'pdf';
// 	const response = await getAPI(TmsIncidentReportApi).apiTmsreportTmsincidentreportExportlistPost(
// 		state.queryParams,
// 		{ responseType: 'blob' }
// 	);
// 	const blob = new Blob([response.data as any], { type: response.headers['content-type'] });
// 	const url = URL.createObjectURL(blob);
// 	const printWindow = window.open(url, '_blank');
// 	printWindow?.print();
// 	URL.revokeObjectURL(url);	
// };

const generateDataId = () => {
	const dateStr = useDateFormat(useNow(), 'YYYYMMDD'); // yyyyMMdd
	const guidStr = 'xxxxxxxxxxxx4xxxyxxxxxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
		const r = Math.random() * 16 | 0, v = c === 'x' ? r : (r & 0x3 | 0x8);
		return v.toString(16);
	});

	return `${dateStr.value}_${guidStr}`;
}; 

// Reset operation
const resetQuery = async () => {
	state.queryParams.fromDate = dateForSearch().startTime; //00:00:00 - first day of month
	state.queryParams.toDate = dateForSearch().endTime; //23:59:59 - last day of month
	state.queryParams.eventTypeId = '';
	state.queryParams.source = '';
};
</script>
<style lang="scss" scoped>
.rpt-acs-container {
    height: 100%;
    display: flex;
    flex-direction: column;
}
.el-header{
	height: 30px;
}
.full-table {
	flex: 1;
	display: flex;
	flex-direction: column;
	overflow: hidden;
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
