<template>
	<div class="hk-hau-kiem-container">
		<splitpanes class="default-theme" horizontal>
			<pane size="9" min-size="3" max-size="40" class="el-card">
				<el-card shadow="hover" style="height: 100%" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', alignItems: 'start' }">
					<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%" @submit.prevent="handleQuery()">
						<el-row :gutter="10">
							<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
								<el-form-item :label="$t('lz.entity.base.startTime')" prop="startTime" label-position="left">
									<DatePicker
										v-model="state.queryParams.startTime"
										dateFormat="dd/mm/yy"
										size="small"
										class="ta-input-size"
										showTime
										hourFormat="24"
										showSeconds
										:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.startTime') })"
									/>
								</el-form-item>
							</el-col>
							<el-col class="mb5" :xs="24" :sm="8" :md="6" :lg="6" :xl="6">
								<el-form-item :label="$t('lz.entity.tms.licensePlate')" prop="plate" label-position="left">
									<el-input v-model="state.queryParams.plate" clearable :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.tms.licensePlate') })" />
								</el-form-item>
							</el-col>
							<el-col class="mb5" :xs="24" :sm="8" :md="6" :lg="6" :xl="6">
								<el-form-item :label="$t('lz.entity.toll.tagId')" prop="tagId" label-position="left">
									<el-input v-model="state.queryParams.tagId" clearable :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.toll.tagId') })" />
								</el-form-item>
							</el-col>
						</el-row>

						<el-row :gutter="10">
							<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
								<el-form-item :label="$t('lz.entity.base.endTime')" prop="endTime" label-position="left">
									<DatePicker
										v-model="state.queryParams.endTime"
										dateFormat="dd/mm/yy"
										size="small"
										class="ta-input-size"
										showTime
										hourFormat="24"
										showSeconds
										:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.endTime') })"
									/>
								</el-form-item>
							</el-col>
							<el-col class="mb5" :xs="24" :sm="8" :md="6" :lg="6" :xl="6">
								<el-form-item :label="$t('lz.entity.toll.transactionId')" prop="transactionId" label-position="left">
									<el-input v-model="state.queryParams.transactionId" clearable :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.toll.transactionId') })" />
								</el-form-item>
							</el-col>
							<el-col class="mb5" :xs="24" :sm="8" :md="6" :lg="6" :xl="6">
								<el-form-item :label="$t('lz.entity.tms.station')" prop="stationId" label-position="left">
									<el-select v-model="state.queryParams.stationId" filterable clearable class="w100" @clear="state.queryParams.stationId = undefined" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.station') })">
										<el-option v-for="item in state.stationList" :key="item.stationId" :label="item.name" :value="item.stationId" />
									</el-select>
								</el-form-item>
							</el-col>
							<el-col class="mb5" :xs="24" :sm="8" :md="6" :lg="6" :xl="6">
								<el-form-item :label="$t('lz.entity.tms.direction')" prop="direction" label-position="left">
									<el-select v-model="state.queryParams.direction" filterable clearable class="w100" @clear="state.queryParams.direction = undefined" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.direction') })">
										<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TollDirectionList)" :key="item.code" :label="item.name" :value="parseInt(item.code)" />
									</el-select>
								</el-form-item>
							</el-col>
						</el-row>
					</el-form>

					<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />

					<div class="el-form-group">
						<SearchButton :auth-search="'tollHauKiem:list'" :show-reset="true" :title-search="$t('lz.button.base.search')" @search="handleQuery(false)" @reset="handleReset" />
						<el-button type="warning" icon="ele-Download" :loading="isFetching" class="mt5" @click="handleManualFetch">
							{{ $t('lz.button.toll.manualFetch') }}
						</el-button>
					</div>
				</el-card>
			</pane>

			<pane size="87">
				<splitpanes class="default-theme" style="height: 100%">
					<pane size="100" style="display: flex">
						<LeftListComp ref="leftListRef" :query-params="state.queryParams" @select-item="handleSelectItemLeft" @query="handleQueryLeft" />
					</pane>
				</splitpanes>
			</pane>
		</splitpanes>
	</div>
</template>

<script lang="ts" setup name="tollHauKiem">
import { onMounted, reactive, ref, defineAsyncComponent } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';
import { Splitpanes, Pane } from 'splitpanes';
import 'splitpanes/dist/splitpanes.css';
import DatePicker from 'primevue/datepicker';
import { getAPI } from '/@/utils/axios-utils';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { HaukiemTransactionApi, TramApi } from '/@/api-services/toll';
import type { TramOutput } from '/@/api-services/toll/models';
import { useManualFetchToll } from '/@/transporter/hooks/useManualFetchToll';
import { useTollWorkerStatus } from '/@/transporter/hooks/useTollWorkerStatus';
import LeftListComp from './component/listLeft.vue';

const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const leftListRef = ref<InstanceType<typeof LeftListComp>>();

const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const { t: $t } = useI18n();

const state = reactive({
	// UI state
	enableBtnEditPlate: true,
	selectRow: {} as any,
	editBienSoXe: '',
	autoSwitch: {
		currentIndex: -1,
	},
	// Tham số tìm kiếm truyền xuống LeftListComp
	queryParams: {
		startTime: undefined as any,
		endTime: undefined as any,
		plate: undefined as any,
		tagId: undefined as any,
		transactionId: undefined as any,
		userId: undefined as any,
		stationId: undefined as any,
		direction: undefined as any,
	},
	// Danh sách trạm lấy từ DB (bảng TollStation)
	stationList: [] as TramOutput[],
});

// Lấy danh sách trạm từ DB thay vì cấu hình
const loadStations = async () => {
	try {
		const res = await getAPI(TramApi).apiTollTramListGet();
		state.stationList = res.data?.result ?? [];
	} catch {
		state.stationList = [];
	}
};

onMounted(() => {
	loadStations();
	const { startTime, endTime } = getDefaultTimeRange();
	state.queryParams.startTime = startTime;
	state.queryParams.endTime = endTime;
});

// Khoảng thời gian mặc định: 00:00:00 – 23:59:59 của ngày hiện tại
const getDefaultTimeRange = () => ({
	startTime: new Date(new Date().setHours(0, 0, 0, 0)),
	endTime: new Date(new Date().setHours(23, 59, 59, 999)),
});

// Validate khoảng thời gian — dùng chung cho handleQuery và handleManualFetch
const validateTimeRange = (): boolean => {
	if (state.queryParams.startTime == null) {
		ElMessage.warning($t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.startTime') }));
		return false;
	}
	if (state.queryParams.endTime == null) {
		ElMessage.warning($t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.endTime') }));
		return false;
	}
	if (new Date(state.queryParams.startTime).getTime() > new Date(state.queryParams.endTime).getTime()) {
		ElMessage.warning($t('lz.validation.toll.greaterThanStartTime', { EndTime: $t('lz.entity.base.endTime'), StartTime: $t('lz.entity.base.startTime') }));
		return false;
	}
	return true;
};

const handleQuery = async (isSetToOldRow = false) => {
	if (!validateTimeRange()) {
		leftListRef.value?.clearTableData();
		return;
	}

	leftListRef.value?.setQueryParams(state.queryParams);
	await leftListRef.value?.handleQuery(!isSetToOldRow);
	state.autoSwitch.currentIndex = 0;

	if (isSetToOldRow) {
		const { plateEdit } = state.selectRow;
		leftListRef.value?.setRowIndex();
		state.editBienSoXe = plateEdit;
	} else {
		leftListRef.value?.selectRow(0, true);
	}
};

// Grid con đã tải xong
const handleQueryLeft = () => {
	state.enableBtnEditPlate = (leftListRef.value?.getTableLength() ?? 0) == 0;
};

// Chọn 1 dòng trên grid con
const handleSelectItemLeft = (row: any, index: number) => {
	state.selectRow = row ?? {};
	state.autoSwitch.currentIndex = index;
	state.editBienSoXe = row?.plateEdit;
};

const handleReset = async () => {
	const { startTime, endTime } = getDefaultTimeRange();
	state.queryParams.startTime = startTime;
	state.queryParams.endTime = endTime;
	state.queryParams.plate = undefined;
	state.queryParams.tagId = undefined;
	state.queryParams.transactionId = undefined;
	state.queryParams.userId = undefined;
	state.queryParams.stationId = undefined;
	state.queryParams.direction = undefined;
	state.editBienSoXe = '';
	state.enableBtnEditPlate = (leftListRef.value?.getTableLength() ?? 0) == 0;

	leftListRef.value?.setQueryParams(state.queryParams);
	await leftListRef.value?.handleQuery(true);
};

// Trạng thái Online/Offline của ITS.Toll.Worker (theo heartbeat NATS mỗi 30s)
const { status: workerStatus, statusTagType: workerStatusTagType } = useTollWorkerStatus();

// Lấy dữ liệu giao dịch (xe vào + xe ra) thủ công từ TCS (qua NATS); nhận kết quả -> nạp lại lưới
const { isFetching, trigger } = useManualFetchToll({ onSuccess: () => handleQuery(true) });
const handleManualFetch = () => {
	if (!validateTimeRange()) return;
	trigger(() =>
		getAPI(HaukiemTransactionApi).apiTollHaukiemtransactionManualfetchPost({
			fromDate: state.queryParams.startTime,
			toDate: state.queryParams.endTime,
		})
	);
};
</script>

<style lang="scss" scoped>
.hk-hau-kiem-container {
	height: 100%;
}
</style>
