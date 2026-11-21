<template>
	<div class="rpt-acs-container">
		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-position="left" style="flex: 1 1 0%" label-width="auto">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.base.fromDate')">
							<DatePicker
								v-model="state.queryParams.fromDate"
								dateFormat="dd/mm/yy"
								showTime
								showSeconds
								size="small"
								class="ta-input-size"
								style="width: 100%"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.label.base.fromDate') })"
							/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.base.toDate')">
							<DatePicker
								v-model="state.queryParams.toDate"
								dateFormat="dd/mm/yy"
								showTime
								showSeconds
								size="small"
								class="ta-input-size"
								style="width: 100%"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.label.base.toDate') })"
							/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.fmsEquipmentHistory.equipmentType')">
							<el-select
								v-model="state.queryParams.equipmentTypeId"
								filterable
								:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.label.fmsEquipmentHistory.equipmentType') })"
								clearable
								class="w100"
							>
								<el-option v-for="item in state.equipmentTypeList" :key="item.id" :label="item.name" :value="item.id" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.fmsEquipmentHistory.equipmentId')">
							<el-select
								v-model="state.queryParams.equipmentId"
								:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.fmsEquipmentHistory.equipmentId') })"
								filterable
								clearable
								class="w100"
							>
								<el-option
									v-for="item in state.equipmentFilterList"
									:key="item.id"
									:label="`${item.name} (${item?.kmNumber ?? 0}Km +${item.metNumber ?? 0})`"
									:value="item.id"
								/>
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.base.status')">
							<el-select
								v-model="state.queryParams.state"
								:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.label.base.status') })"
								clearable
								class="w100"
							>
								<el-option v-for="item in state.stateList" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tmsSignallog.executeBy')">
							<el-input
								v-model="state.queryParams.executeBy"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.tmsSignallog.executeBy') })"
								clearable
								@keyup.enter.native="handleQuery"
								class="w100"
							/>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>

			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />

			<el-row>
				<el-col>
					<SearchButton
						:auth-search="'lcsRptOperationLogHistory:view'"
						@search="handleQuery"
						@reset="resetQuery"
						:loading="state.isProcessing"
						:title-search="$t('lz.button.acs.reportCreate')"
					/>
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
				export-auth="lcsRptOperationLogHistory:export"
				print-auth="lcsRptOperationLogHistory:print"
				@export="handleExport"
			/>
		</el-card>
	</div>
</template>

<script lang="ts" setup name="lcsRptOperationLogHistory">
/**
 * Author: ChuongLP
 * Description: Báo cáo lịch sử điều khiển thiết bị LCS
 * Created date: 08/10/2026
 */
import { onMounted, reactive, ref, defineAsyncComponent, computed, watch } from 'vue';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { useDateFormat, useNow } from '@vueuse/core';
import { saveAs } from 'file-saver';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum, TmsEquipmentType } from '/@/types/enums';
import DatePicker from 'primevue/datepicker';
import { VmsOperationLogHistoryReportApi } from '/@/api-services/vmsReport';
import { TmsEquipmentApi, TmsEquipmentTypeApi } from '/@/api-services/tms';

const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;

const { t: $t } = useI18n();
const now = useNow();

// Danh sách mã loại thiết bị được phép lọc trên báo cáo (chỉ LCS)
const ALLOWED_EQUIPMENT_TYPES = [TmsEquipmentType.LCS];

// Module key phân hệ
const MODULE_KEY = 'LCS';

// Report viewer ref
const ReportViewer = defineAsyncComponent(() => import('/@/components/advanced/report-viewer.vue'));
const reportViewerRef = ref<InstanceType<typeof ReportViewer>>();

// State definition
const state = reactive({
	isProcessing: false,
	pdf: null as any,
	queryParams: {
		moduleKey: MODULE_KEY as string | null,
		equipmentTypeId: null as any,
		equipmentId: null as any,
		state: null as any,
		executeBy: null as string | null,
		fromDate: dateForSearch().startTime as any,
		toDate: dateForSearch().endTime as any,
		reportDataId: null as any,
		format: 'pdf' as any,
	},
	stateList: [] as any,
	equipmentList: [] as any,
	equipmentFilterList: [] as any,
	equipmentTypeList: [] as any,
});

// Page initialization
onMounted(async () => {
	// Set default values
	resetQuery();
	const typeResponse = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet();
	// Chỉ lấy loại thiết bị là LCS
	state.equipmentTypeList = (typeResponse.data.result ?? []).filter((item: any) =>
		item.code && ALLOWED_EQUIPMENT_TYPES.some((allowedCode) => allowedCode.toLowerCase() === item.code.toLowerCase())
	);
	if (state.equipmentTypeList.length === 1) {
		state.queryParams.equipmentTypeId = state.equipmentTypeList[0].id;
	}

	const allowedTypeIds = new Set(state.equipmentTypeList.map((type: any) => type.id));

	const equipResponse = await getAPI(TmsEquipmentApi).apiTmsTmsequipmentListGet();
	// Chỉ lấy các thiết bị tương ứng với loại thiết bị LCS
	state.equipmentList = (equipResponse.data.result ?? [])
		.filter((equip: any) => allowedTypeIds.has(equip.equipmentTypeId))
		.sort((a: any, b: any) => a.name.localeCompare(b.name));
	state.equipmentFilterList = state.equipmentList;
	// Trạng thái thao tác điều khiển thiết bị
	state.stateList = getConfigDataByCode(BaseConfigTypeEnum.TmsActionType);
});

// Lọc danh sách thiết bị theo loại thiết bị được chọn
watch(
	() => state.queryParams.equipmentTypeId,
	(newTypeId) => {
		if (newTypeId) {
			state.equipmentFilterList = state.equipmentList.filter((equip: any) => equip.equipmentTypeId === newTypeId);
			// Nếu thiết bị hiện tại không thuộc loại thiết bị mới chọn thì reset
			if (state.queryParams.equipmentId && !state.equipmentFilterList.some((equip: any) => equip.id === state.queryParams.equipmentId)) {
				state.queryParams.equipmentId = null;
			}
		} else {
			state.equipmentFilterList = state.equipmentList;
		}
	}
);

// Calculate default date range for search (last 3 days)
function dateForSearch(): { startTime: any; endTime: any } {
	const start = new Date(new Date(now.value));
	start.setDate(start.getDate() - 3);
	start.setHours(0, 0, 0, 0);

	const end = new Date(new Date(now.value).setHours(23, 59, 59, 999));
	return {
		startTime: start as any,
		endTime: end as any,
	};
}

// Tên file báo cáo khi export
const reportFileName = computed(() => {
	return 'LcsOperationLogHistory_' + useDateFormat(useNow(), 'YYYYMMDD_HHmmss').value;
});

// Query operation
const handleQuery = async () => {
	try {
		state.isProcessing = true;
		state.pdf = null;
		state.queryParams.reportDataId = generateDataId();
		const response = await getAPI(VmsOperationLogHistoryReportApi).apiVmsreportVmsoperationloghistoryreportRenderlistPost(
			state.queryParams,
			{ responseType: 'blob' }
		);
		state.pdf = URL.createObjectURL(new Blob([response.data as any]));
		reportViewerRef.value?.setZoomTo150();
	} catch (error) {
		console.error('handleQuery(...)', error);
	} finally {
		state.isProcessing = false;
	}
};

// Export operation (called when exporting to non-PDF formats)
const handleExport = async (format: string) => {
	try {
		state.queryParams.format = format;
		const response = await getAPI(VmsOperationLogHistoryReportApi).apiVmsreportVmsoperationloghistoryreportExportlistPost(
			state.queryParams,
			{ responseType: 'blob' }
		);

		// Get the filename from the Content-Disposition header
		const filename = `${reportFileName.value}.${format}`;
		// Use FileSaver to save the file
		saveAs(new Blob([response.data as any], { type: response.headers['content-type'] }), filename);

		ElMessage.success($t('lz.message.base.exportSuccess'));
	} catch (error: any) {
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

// Generate unique report data ID
const generateDataId = () => {
	const dateStr = useDateFormat(useNow(), 'YYYYMMDD');
	const guidStr = 'xxxxxxxxxxxx4xxxyxxxxxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
		const r = (Math.random() * 16) | 0,
			v = c === 'x' ? r : (r & 0x3) | 0x8;
		return v.toString(16);
	});

	return `${dateStr.value}_${guidStr}`;
};

// Reset operation
const resetQuery = async () => {
	state.queryParams.fromDate = dateForSearch().startTime;
	state.queryParams.toDate = dateForSearch().endTime;
	state.queryParams.equipmentTypeId = state.equipmentTypeList?.length === 1 ? state.equipmentTypeList[0].id : null;
	state.queryParams.equipmentId = null;
	state.queryParams.state = null;
	state.queryParams.executeBy = null;
	state.queryParams.moduleKey = MODULE_KEY;
	state.equipmentFilterList = state.equipmentList;
};
</script>

<style lang="scss" scoped>
.rpt-acs-container {
	height: 100%;
	display: flex;
	flex-direction: column;
}
.el-header {
	height: 30px;
}
.full-table {
	flex: 1;
	display: flex;
	flex-direction: column;
	overflow: hidden;
}
</style>
