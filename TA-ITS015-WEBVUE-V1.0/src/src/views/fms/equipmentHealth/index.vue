<template>
	<div class="fms-equipment-health-container">
		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%" label-position="left">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="5" :xl="5">
						<el-form-item :label="$t('lz.entity.base.code')" prop="code">
							<el-input v-model="state.queryParams.code" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.code') })" clearable @keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="5" :xl="5">
						<el-form-item :label="$t('lz.entity.base.name')" prop="name">
							<el-input v-model="state.queryParams.name" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.name') })" clearable @keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="5">
						<el-form-item :label="$t('lz.entity.tms.equipmentType')" prop="equipmentTypeId">
							<el-select v-model="state.queryParams.equipmentTypeId" filterable clearable class="w100" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.equipmentType') })">
								<el-option v-for="item in activeTypeList" :key="item.id" :label="item.name" :value="item.id" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="8" :xl="9">
						<el-form-item :label="$t('lz.entity.fmsHealth.healthStatus')" prop="healthStatus">
							<el-select v-model="state.queryParams.healthStatus" clearable class="w100" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.fmsHealth.healthStatus') })">
								<el-option :label="$t('lz.label.fmsHealth.good')" value="good" />
								<el-option :label="$t('lz.label.fmsHealth.warning')" value="warning" />
								<el-option :label="$t('lz.label.fmsHealth.critical')" value="critical" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>

			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />

			<el-row>
				<SearchButton :auth-search="'fmsEquipmentHealth:page'" @search="handleQuery" @reset="resetQuery" :loading="options.loading"  />
			</el-row>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
				<!-- Thống kê nhanh tình trạng sức khỏe toàn bộ thiết bị -->
				<template #toolbar_buttons>
					<div class="health-stats">
						<el-tag type="info" effect="plain">{{ $t('lz.label.fmsHealth.total') }}: {{ state.stats.total }}</el-tag>
						<el-tag type="success">{{ $t('lz.label.fmsHealth.good') }}: {{ state.stats.good }}</el-tag>
						<el-tag type="warning">{{ $t('lz.label.fmsHealth.warning') }}: {{ state.stats.warning }}</el-tag>
						<el-tag type="danger">{{ $t('lz.label.fmsHealth.critical') }}: {{ state.stats.critical }}</el-tag>
					</div>
				</template>
				<template #toolbar_tools>
					<TableHeader @refresh="handleQuery" @custom-column="handleCustom" />
				</template>

				<template #empty>
					<el-empty :image-size="200" />
				</template>

				<template #row_km_met="{ row }"> Km {{ row.kmNumber }} + {{ row.metNumber }} </template>

				<template #row_temperature="{ row }">
					<span :style="{ color: metricColor('temperature', row.temperature), fontWeight: 600 }">{{ row.temperature }} °C</span>
				</template>
				<template #row_fan="{ row }">
					<span :style="{ color: metricColor('fanSpeed', row.fanSpeed), fontWeight: 600 }">{{ row.fanSpeed }} RPM</span>
				</template>
				<template #row_power="{ row }">
					<span :style="{ color: metricColor('power', row.power), fontWeight: 600 }">{{ row.power }} V</span>
				</template>
				<template #row_health="{ row }">
					<el-tag :type="healthTagType(row.healthStatus)">{{ healthLabel(row.healthStatus) }}</el-tag>
				</template>
			</vxe-grid>
		</el-card>
	</div>
</template>

<script lang="ts" setup name="fmsEquipmentHealth">
import { computed, onMounted, reactive, ref, defineAsyncComponent } from 'vue';
import { useI18n } from 'vue-i18n';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { getAPI } from '/@/utils/axios-utils';
import { TmsEquipmentApi, TmsEquipmentTypeApi } from '/@/api-services/tms';
import { useDateFormat } from '@vueuse/core';
import { DATE_TIME_FORMAT } from '/@/types/formats';

const { t: $t } = useI18n();

const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));

const xGrid = ref<VxeGridInstance>();

// Ngưỡng cảnh báo cho từng số liệu (mock). warn = bắt đầu cảnh báo, crit = nguy cấp.
const THRESHOLD = {
	temperature: { warn: 55, crit: 70 }, // °C: càng cao càng xấu
	fanSpeed: { warn: 4500, crit: 5500 }, // RPM: quạt quay quá nhanh -> đang gắng làm mát
	powerLow: 200, // V: dưới ngưỡng này là bất thường
	powerHigh: 240, // V: trên ngưỡng này là bất thường
};

// Kiểu 1 dòng dữ liệu sức khỏe thiết bị
interface HealthRow {
	id: string;
	code: string;
	name: string;
	equipmentTypeId: string;
	kmNumber: number | null;
	metNumber: number | null;
	temperature: number;
	fanSpeed: number;
	power: number;
	healthStatus: 'good' | 'warning' | 'critical';
	lastUpdate: Date;
}

const state = reactive({
	queryParams: {
		code: '' as string,
		name: '' as string,
		equipmentTypeId: undefined as string | undefined,
		healthStatus: undefined as string | undefined,
	},
	typeList: [] as any[],
	allRows: [] as HealthRow[], // Toàn bộ dòng mock sinh từ danh sách thiết bị
	stats: { total: 0, good: 0, warning: 0, critical: 0 },
});

// Danh sách loại thiết bị cho bộ lọc: chỉ hiển thị loại chưa bị xóa (isDelete == null).
// typeList gốc vẫn giữ đầy đủ để formatter hiển thị được tên loại đã xóa (nếu có).
const activeTypeList = computed(() => state.typeList.filter((item) => item.isDelete == null));

// ===== Helpers dữ liệu giả lập =====
const randInt = (min: number, max: number) => Math.floor(Math.random() * (max - min + 1)) + min;

// Suy ra tình trạng sức khỏe tổng hợp từ các số liệu theo ngưỡng
const deriveHealth = (temperature: number, fanSpeed: number, power: number): HealthRow['healthStatus'] => {
	const critical = temperature >= THRESHOLD.temperature.crit || fanSpeed >= THRESHOLD.fanSpeed.crit || power < THRESHOLD.powerLow || power > THRESHOLD.powerHigh;
	if (critical) return 'critical';
	const warning = temperature >= THRESHOLD.temperature.warn || fanSpeed >= THRESHOLD.fanSpeed.warn;
	if (warning) return 'warning';
	return 'good';
};

// Sinh dữ liệu sức khỏe random cho toàn bộ thiết bị lấy từ TmsEquipment
const buildHealthRows = (equipments: any[]): HealthRow[] => {
	return equipments.map((e) => {
		const temperature = randInt(30, 80);
		const fanSpeed = randInt(2000, 6000);
		const power = randInt(190, 245);
		return {
			id: e.id,
			code: e.code,
			name: e.name,
			equipmentTypeId: e.equipmentTypeId,
			kmNumber: e.kmNumber,
			metNumber: e.metNumber,
			temperature,
			fanSpeed,
			power,
			healthStatus: deriveHealth(temperature, fanSpeed, power),
			lastUpdate: new Date(Date.now() - randInt(0, 3600) * 1000),
		};
	});
};

// Cập nhật thống kê nhanh trên toolbar
const refreshStats = () => {
	state.stats.total = state.allRows.length;
	state.stats.good = state.allRows.filter((r) => r.healthStatus === 'good').length;
	state.stats.warning = state.allRows.filter((r) => r.healthStatus === 'warning').length;
	state.stats.critical = state.allRows.filter((r) => r.healthStatus === 'critical').length;
};

// ===== Hiển thị =====
const metricColor = (metric: 'temperature' | 'fanSpeed' | 'power', value: number) => {
	let level: HealthRow['healthStatus'] = 'good';
	if (metric === 'temperature') level = value >= THRESHOLD.temperature.crit ? 'critical' : value >= THRESHOLD.temperature.warn ? 'warning' : 'good';
	else if (metric === 'fanSpeed') level = value >= THRESHOLD.fanSpeed.crit ? 'critical' : value >= THRESHOLD.fanSpeed.warn ? 'warning' : 'good';
	else level = value < THRESHOLD.powerLow || value > THRESHOLD.powerHigh ? 'critical' : 'good';

	if (level === 'critical') return 'var(--el-color-danger)';
	if (level === 'warning') return 'var(--el-color-warning)';
	return 'var(--el-color-success)';
};

const healthTagType = (status: string) => (status === 'critical' ? 'danger' : status === 'warning' ? 'warning' : 'success');
const healthLabel = (status: string) => (status === 'critical' ? $t('lz.label.fmsHealth.critical') : status === 'warning' ? $t('lz.label.fmsHealth.warning') : $t('lz.label.fmsHealth.good'));

// Table parameter configuration
const options = useVxeTable<HealthRow>(
	{
		id: 'fmsEquipmentHealth',
		name: 'Equipment Health',
		columns: [
			{ type: 'seq', title: $t('lz.label.base.idx'), width: 70, fixed: 'left' },
			{ field: 'code', title: $t('lz.entity.base.code'), minWidth: 130, fixed: 'left', showOverflow: 'tooltip', sortable: true },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 150, showOverflow: 'tooltip', sortable: true },
			{ field: 'equipmentTypeId', title: $t('lz.entity.tms.equipmentType'), minWidth: 140, showOverflow: 'tooltip', formatter: ({ cellValue }: any) => state.typeList.find((u) => u.id == cellValue)?.name ?? '' },
			{ field: 'kmNumber', title: $t('lz.entity.tms.kmMetNumber'), minWidth: 110, showOverflow: 'tooltip', slots: { default: 'row_km_met' } },
			{ field: 'temperature', title: $t('lz.entity.fmsHealth.temperature'), minWidth: 110, sortable: true, slots: { default: 'row_temperature' } },
			{ field: 'fanSpeed', title: $t('lz.entity.fmsHealth.fanSpeed'), minWidth: 120, sortable: true, slots: { default: 'row_fan' } },
			{ field: 'power', title: $t('lz.entity.fmsHealth.power'), minWidth: 110, sortable: true, slots: { default: 'row_power' } },
			{ field: 'healthStatus', title: $t('lz.entity.fmsHealth.healthStatus'), width: 170, sortable: true, slots: { default: 'row_health' } },
			{ field: 'lastUpdate', title: $t('lz.entity.fmsHealth.lastUpdate'), minWidth: 160, sortable: true, showOverflow: 'tooltip', formatter: ({ cellValue }: any) => useDateFormat(cellValue, DATE_TIME_FORMAT)?.value },
		],
	},
	{
		proxyConfig: { autoLoad: false, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		sortConfig: { defaultSort: { field: 'healthStatus', order: 'desc' } },
		pagerConfig: { pageSize: 50 },
		toolbarConfig: { export: false },
	}
);

// Page initialization
onMounted(async () => {
	// Lấy danh mục loại thiết bị để hiển thị tên loại
	const resType = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet(undefined, undefined, undefined, undefined, undefined, true as any);
	state.typeList = resType.data.result ?? [];

	// Lấy tạm danh sách thiết bị từ TmsEquipment rồi fill random số liệu sức khỏe.
	// Chỉ lấy thiết bị chưa bị xóa (IsDelete == null) để loại bỏ dữ liệu rác.
	const resEquip = await getAPI(TmsEquipmentApi).apiTmsTmsequipmentListGet();
	const equipments = (resEquip.data.result ?? []).filter((e) => e.isDelete == null);
	state.allRows = buildHealthRows(equipments);
	refreshStats();

	await handleQuery();
});

// Query "API" (mock): lọc + sắp xếp + phân trang phía FE trên state.allRows
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const { code, name, equipmentTypeId, healthStatus } = state.queryParams;

	let filtered = state.allRows.filter((r) => {
		if (code && !r.code?.toLowerCase().includes(code.toLowerCase())) return false;
		if (name && !r.name?.toLowerCase().includes(name.toLowerCase())) return false;
		if (equipmentTypeId && r.equipmentTypeId !== equipmentTypeId) return false;
		if (healthStatus && r.healthStatus !== healthStatus) return false;
		return true;
	});

	// Sắp xếp. healthStatus sắp theo mức độ nghiêm trọng để cảnh báo nổi lên đầu
	const field = sort.field || 'healthStatus';
	const order = sort.order || 'desc';
	const severity: Record<string, number> = { good: 0, warning: 1, critical: 2 };
	filtered = filtered.sort((a: any, b: any) => {
		const va = field === 'healthStatus' ? severity[a.healthStatus] : a[field];
		const vb = field === 'healthStatus' ? severity[b.healthStatus] : b[field];
		if (va === vb) return 0;
		const res = va > vb ? 1 : -1;
		return order === 'desc' ? -res : res;
	});

	const total = filtered.length;
	const start = (page.currentPage - 1) * page.pageSize;
	const records = filtered.slice(start, start + page.pageSize);

	await new Promise((resolve) => setTimeout(resolve, 120)); // giả lập độ trễ mạng
	return { data: { result: { records, total }, message: '' } };
};

// Query operation
const handleQuery = async () => {
	options.loading = true;
	await xGrid.value?.commitProxy('reload');
	options.loading = false;
};

// Reset operation
const resetQuery = async () => {
	state.queryParams.code = '';
	state.queryParams.name = '';
	state.queryParams.equipmentTypeId = undefined;
	state.queryParams.healthStatus = undefined;
	await xGrid.value?.commitProxy('reload');
};

function handleCustom() {
	xGrid.value?.openCustom();
}

const gridEvents: VxeGridListeners<HealthRow> = {};
</script>

<style scoped lang="scss">
.health-stats {
	display: flex;
	align-items: center;
	gap: 8px;
}
</style>
