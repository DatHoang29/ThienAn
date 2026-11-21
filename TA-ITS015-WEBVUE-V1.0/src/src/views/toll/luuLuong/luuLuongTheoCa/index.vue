<template>
	<div class="rpt-its-container">
		<el-card shadow="hover"
			:body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true"
				label-position="left" style="flex: 1 1 0%" label-width="auto">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.base.day')" required>
							<DatePicker v-model="state.queryParams.ngay" dateFormat="dd/mm/yy" showTimes showSeconds
								size="small" class="ta-input-size"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.label.base.day') })" />
						</el-form-item>
					</el-col>

					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.tmsTrafficInfoReport.shift')" required>
							<el-select v-model="state.queryParams.ca" class="w100"
								:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.label.tmsTrafficInfoReport.shift') })">
								<!-- Tên ca lấy trực tiếp từ dữ liệu nghiệp vụ (TollShift.Name), không còn
								     dịch qua key `lz.param.{groupCode}.{code}` của op_config. -->
								<el-option v-for="item in state.listData.caList" :key="item.code" :label="item.name"
									:value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>

					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.station')" required>
							<el-select v-model="state.queryParams.tram" filterable class="w100"
								:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.station') })">
								<!-- Bind theo `code` (mã trạm) cho khớp 5 trang báo cáo còn lại; trước đây
								     bind `item.id` nên gửi lên báo cáo id bản ghi cấu hình, không phải mã trạm. -->
								<el-option v-for="item in state.listData.tramList" :key="item.code" :label="item.name"
									:value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>

					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.direction')" required>
							<el-select v-model="state.queryParams.huong" filterable class="w100"
								:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.direction') })">
								<el-option v-for="item in state.listData.huongList" :key="item.code" :label="item.name"
									:value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>

			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />

			<el-row>
				<el-col>
					<SearchButton :auth-search="'itsBcLLTheoCa:view'" @search="handleQuery" @reset="resetQuery"
						:loading="state.isProcessing" :title-search="$t('lz.button.acs.reportCreate')" />
				</el-col>
			</el-row>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<ReportViewer ref="reportViewerRef" :pdf-source="state.pdf" :loading="state.isProcessing"
				:format-options="getConfigDataByCode(BaseConfigTypeEnum.RptExportFormat)"
				:default-format="state.queryParams.format" :export-file-name="reportFileName"
				export-auth="itsBcLLTheoCa:export" print-auth="itsBcLLTheoCa:print" @export="handleExport" />
		</el-card>
	</div>
</template>

<script lang="ts" setup name="itsBcLLTheoCa">
import { onMounted, reactive, ref, defineAsyncComponent, computed } from 'vue';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { useDateFormat, useNow } from '@vueuse/core';
import { saveAs } from 'file-saver';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum, TollDirectionEnum } from '/@/types/enums';
import DatePicker from 'primevue/datepicker';
import { LLTCApi } from '/@/api-services/tollReport';
import { useTollCatalog } from '/@/hooks/useTollCatalog';

const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const { loadTollStations, loadTollShifts } = useTollCatalog();

const { t: $t } = useI18n();

// Report viewer ref
const ReportViewer = defineAsyncComponent(() => import('/@/components/advanced/report-viewer.vue'));
const reportViewerRef = ref<InstanceType<typeof ReportViewer>>();

// State definition
const state = reactive({
	isProcessing: false,
	pdf: null as any,
	queryParams: {
		ngay: null,
		ca: null,
		tram: null,
		huong: TollDirectionEnum.Out, // Mặc định hướng ra
		reportDataId: null as any,
		format: 'pdf' as any,
	} as any,
	listData: {
		tramList: [] as any,
		huongList: [] as any,
		caList: [] as any,
	},
});

// Trạm mặc định: trạm đầu tiên trong danh sách
const defaultTram = () => {
	return state.listData.tramList?.[0]?.code ?? null;
};

// Ca mặc định: ca đầu tiên trong danh sách (BE không trả số liệu khi không truyền ca)
const defaultCa = () => {
	return state.listData.caList?.[0]?.code ?? null;
};

// Page initialization
onMounted(async () => {
	// Danh sách trạm lấy từ API nghiệp vụ (/api/toll/tram/list), không còn khai báo tay
	// trong op_config `toll_station_list`.
	const rsTram = await loadTollStations();
	state.listData.tramList = rsTram as [];

	const rsDirection = getConfigDataByCode(BaseConfigTypeEnum.TollDirectionList);
	state.listData.huongList = rsDirection;

	// Danh sách ca làm việc lấy từ API nghiệp vụ (/api/toll/calamviec/list),
	// không còn khai báo tay trong op_config `DayWorkShift`.
	state.listData.caList = await loadTollShifts();

	resetQuery();
});

// Hướng mặc định: hướng ra (xe ra)
const defaultHuong = () => {
	const item = state.listData.huongList?.find((x: any) => x.code?.toString() === TollDirectionEnum.Out);
	return item ? item.code : TollDirectionEnum.Out;
};

//Tên file báo cáo khi export
const reportFileName = computed(() => {
	return 'LuuLuongTheoCa_' + useDateFormat(useNow(), 'YYYYMMDD_HHmmss').value;
});

// Validate query params
const validateQuery = (): boolean => {
	if (!state.queryParams.ngay) {
		state.queryParams.ngay = new Date(new Date(useNow().value).setHours(0, 0, 0, 0));
	}
	if (!state.queryParams.ca) {
		state.queryParams.ca = defaultCa();
	}
	if (!state.queryParams.ca) {
		ElMessage.warning($t('lz.validation.base.required', { PropertyName: $t('lz.label.tmsTrafficInfoReport.shift') }));
		return false;
	}
	if (!state.queryParams.tram) {
		state.queryParams.tram = defaultTram();
	}
	if (!state.queryParams.tram) {
		ElMessage.warning($t('lz.validation.base.required', { PropertyName: $t('lz.entity.tms.station') }));
		return false;
	}
	if (!state.queryParams.huong) {
		state.queryParams.huong = defaultHuong();
	}
	if (!state.queryParams.huong) {
		ElMessage.warning($t('lz.validation.base.required', { PropertyName: $t('lz.entity.tms.direction') }));
		return false;
	}
	return true;
};

// Query operation
const handleQuery = async () => {
	if (!validateQuery()) return;
	try {
		state.isProcessing = true;
		state.pdf = null;
		state.queryParams.reportDataId = generateDataId();
		const response = await getAPI(LLTCApi).apiTollreportLltcRenderPost(state.queryParams, { responseType: 'blob' });
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
	if (!validateQuery()) return;
	try {
		state.queryParams.format = format;

		// API Export cũng chính là API gửi mail: có sendMail + recipients thì BE gửi kèm file
		// vừa kết xuất. Người nhận lấy từ tham số cấu hình `toll_report_auto_mail`
		// (BaseConfigTypeEnum.TollMailAuto) — email ở cột Giá trị (value), rỗng thì lấy Mã (code).
		const recipients = getConfigDataByCode(BaseConfigTypeEnum.TollMailAuto)
			.map((x: any) => x.value || x.code)
			.filter((x: any) => !!x)
			.join('; ');

		const response = await getAPI(LLTCApi).apiTollreportLltcExportPost(
			{ ...state.queryParams, sendMail: !!recipients, recipients },
			{ responseType: 'blob' }
		);

		let filename = `${reportFileName.value}.${format}`;
		saveAs(new Blob([response.data as any], { type: response.headers['content-type'] }), filename);

		// Lưu ý: BE cố tình nuốt lỗi gửi mail để không làm hỏng việc tải file, nên thông báo này
		// chỉ khẳng định đã gửi yêu cầu; lỗi SMTP (nếu có) nằm trong log server.
		ElMessage.success(recipients ? `Đã tải về và gửi email tới: ${recipients}` : $t('lz.message.base.exportSuccess'));
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
	const dateStr = useDateFormat(useNow(), 'YYYYMMDD'); // yyyyMMdd
	const guidStr = 'xxxxxxxxxxxx4xxxyxxxxxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
		const r = (Math.random() * 16) | 0,
			v = c === 'x' ? r : (r & 0x3) | 0x8;
		return v.toString(16);
	});

	return `${dateStr.value}_${guidStr}`;
};

// Reset operation
const resetQuery = async () => {
	state.queryParams.ngay = new Date(new Date(useNow().value).setHours(0, 0, 0, 0));
	state.queryParams.ca = defaultCa();
	state.queryParams.tram = defaultTram();
	state.queryParams.huong = defaultHuong();
	state.queryParams.reportDataId = null;
};
</script>
<style lang="scss" scoped>
.rpt-its-container {
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
