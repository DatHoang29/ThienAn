<!--
  Màn GIÁM SÁT TELEMETRY THIẾT BỊ (FMS) - dạng THẺ theo từng thiết bị.
  - Chia tab theo LOẠI thiết bị (chỉ hiện loại đang có thiết bị) + tab "Tất cả".
  - Mỗi thiết bị 1 thẻ: mini-tab Tài nguyên/Uptime/Môi trường, thanh tiến trình, trạng thái kết nối.
  - Số cột thẻ cấu hình trong code (CARD_COLUMNS = 4 hoặc 5).
  - Tự động làm mới định kỳ (REFRESH_SECONDS).
  Author: ChuongLP
-->
<template>
	<div class="telemetry-container">
		<!-- Khung chung (GridSearchPanel): thẻ tìm kiếm cố định phía trên + thanh chia thu gọn/kéo, nội dung lấp đầy phần còn lại -->
		<GridSearchPanel id="fms-telemetry-search">
			<template #filters>
			<div class="toolbar">
				<el-input
					v-model="state.keyword"
					clearable
					class="kw-input"
					:prefix-icon="'ele-Search'"
					:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.code') + ' / ' + $t('lz.entity.base.name') })"
				/>
				<div class="toolbar-right">
					<!-- Chọn loại thiết bị cần giám sát (lưu SysConfigData fms_telemetry_equipment_type, BE lọc theo danh sách này) -->
					<el-button type="primary" plain icon="ele-Plus" @click="openTypeDialog">
						{{ $t('lz.label.fmsTelemetry.addEquipmentType') }}
						<span v-if="state.monitoredConfigs.length">&nbsp;({{ state.monitoredConfigs.length }})</span>
					</el-button>
					<el-button v-auth="'fmsTelemetry:refresh'" icon="ele-Refresh" :loading="state.loading" @click="loadDevices(true)">{{ $t('lz.button.base.refresh') }}</el-button>
				</div>
			</div>
			</template>

		<!-- Nội dung: danh sách thẻ cuộn bên trong, không đẩy mất thẻ tìm kiếm -->
		<el-card class="content-card" shadow="hover" v-loading="state.loading && !state.loaded">
			<div class="content-head">
				<div class="content-title">{{ $t('lz.label.vmsDeviceStatus.deviceList') }}</div>
				<div class="content-info">
					{{ $t('lz.label.vmsDeviceStatus.showingDevices', { count: filteredDevices.length }) }}
					<span class="dot-sep">•</span>
					{{ $t('lz.label.vmsDeviceStatus.autoRefresh') }}: {{ REFRESH_SECONDS }}s
					<span class="dot-sep">•</span>
					<span :class="{ 'monitor-all': !state.monitoredConfigs.length }">
						{{ state.monitoredConfigs.length ? $t('lz.label.fmsTelemetry.monitoringCount', { count: state.monitoredConfigs.length }) : $t('lz.label.fmsTelemetry.monitoringAll') }}
					</span>
				</div>
			</div>
			
			<el-tabs v-model="state.activeType" class="type-tabs">
				<el-tab-pane :label="`${$t('lz.label.base.all')} (${filteredAll.length})`" name="__all__">
					<div v-if="filteredAll.length" class="card-grid" :style="{ '--cols': CARD_COLUMNS }">
						<deviceCard v-for="dev in filteredAll" :key="dev.equipmentId" :row="dev" :type-name="typeName(dev.equipmentTypeId)" :type-icon="typeIcon(dev.equipmentTypeId)" @detail="handleViewHistory" />
					</div>
					<el-empty v-else :image-size="140" :description="$t('lz.label.base.noData')" />
				</el-tab-pane>

				<el-tab-pane v-for="grp in typeGroups" :key="grp.id" :label="`${grp.name} (${grp.count})`" :name="grp.id">
					<div v-if="devicesOfType(grp.id).length" class="card-grid" :style="{ '--cols': CARD_COLUMNS }">
						<deviceCard v-for="dev in devicesOfType(grp.id)" :key="dev.equipmentId" :row="dev" :type-name="grp.name" :type-icon="grp.icon" @detail="handleViewHistory" />
					</div>
					<el-empty v-else :image-size="140" :description="$t('lz.label.base.noData')" />
				</el-tab-pane>
			</el-tabs>
		</el-card>
		</GridSearchPanel>

		<!-- Chọn loại thiết bị giám sát: tick = giám sát, bỏ tick = bỏ giám sát; bỏ hết = hiển thị toàn bộ -->
		<el-dialog v-model="state.typeDialogVisible" :title="$t('lz.label.fmsTelemetry.equipmentTypeTitle')" width="560px" append-to-body>
			<el-alert :title="$t('lz.label.fmsTelemetry.equipmentTypeHint')" type="info" :closable="false" show-icon class="mb10" />
			<el-input v-model="state.typeSearch" clearable prefix-icon="ele-Search" :placeholder="$t('lz.button.base.search')" class="mb10" />
			<el-checkbox-group v-model="state.typeSelection" class="type-check-list">
				<el-checkbox v-for="item in typeCandidates" :key="item.id" :value="item.id" border>
					<span class="type-check-label" :title="item.name">{{ item.name }}</span>
				</el-checkbox>
			</el-checkbox-group>
			<el-empty v-if="!typeCandidates.length" :image-size="80" :description="$t('lz.label.base.noData')" />
			<template #footer>
				<el-button @click="state.typeDialogVisible = false">{{ $t('lz.button.base.cancel') }}</el-button>
				<el-button type="primary" :loading="state.typeSaving" @click="saveMonitoredTypes">{{ $t('lz.button.base.save') }}</el-button>
			</template>
		</el-dialog>

		<deviceTelemetryHistoryComp ref="historyRef" :title="$t('lz.label.vmsDeviceStatus.historyTitle')" />
	</div>
</template>

<script lang="ts" setup name="fmsTelemetry">
import { computed, onMounted, onUnmounted, reactive, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';

import { FmsEquipmentTelemetryApi } from '/@/api-services/fms';
import { PageEquipmentLatestInput } from '/@/api-services/fms';
import { TmsEquipmentTypeApi } from '/@/api-services/tms';
import { SysConfigDataApi, SysConfigTypeApi } from '/@/api-services/cfgSystem';
import type { AddSysConfigDataInput, SysConfigDataOutput } from '/@/api-services/cfgSystem';
import { ElMessage } from 'element-plus';
import { BaseConfigTypeEnum } from '/@/types/enums/baseEnum';

import GridSearchPanel from '/@/components/gridPage/GridSearchPanel.vue';
import deviceCard from '/@/views/fms/telemetry/component/deviceCard.vue';
import deviceTelemetryHistoryComp from '/@/views/fms/telemetry/component/deviceTelemetryHistory.vue';

const { t: $t } = useI18n();

/** Số cột thẻ trên 1 hàng (đổi 4 hoặc 5 tùy nhu cầu). */
const CARD_COLUMNS = 4;
/** Chu kỳ tự động làm mới (giây). */
const REFRESH_SECONDS = 30;

const historyRef = ref<InstanceType<typeof deviceTelemetryHistoryComp>>();

const state = reactive({
	loading: false,
	loaded: false,
	keyword: '' as string,
	activeType: '__all__' as string,
	devices: [] as any[], // toàn bộ thiết bị + telemetry mới nhất
	typeList: [] as any[], // danh mục loại thiết bị (kèm loại đã xóa để map tên)
	monitoredConfigs: [] as SysConfigDataOutput[], // loại thiết bị đang giám sát (SysConfigData fms_telemetry_equipment_type)
	typeDialogVisible: false,
	typeSaving: false,
	typeSearch: '' as string,
	typeSelection: [] as string[], // ID loại thiết bị đang tick trong dialog
});

let refreshTimer: any = null;

// ===== Map loại thiết bị -> tên / icon =====
const typeName = (id: any) => state.typeList.find((u) => u.id == id)?.name ?? '';
const typeIcon = (id: any) => state.typeList.find((u) => u.id == id)?.defaultIcon ?? '';

// ===== Lọc theo từ khóa (mã hoặc tên) =====
const matchKeyword = (dev: any) => {
	const kw = state.keyword.trim().toLowerCase();
	if (!kw) return true;
	return (dev.equipmentCode ?? '').toLowerCase().includes(kw) || (dev.equipmentName ?? '').toLowerCase().includes(kw);
};

const filteredDevices = computed(() => state.devices.filter(matchKeyword));
const filteredAll = computed(() => filteredDevices.value);

// ===== Nhóm theo loại thiết bị (chỉ loại đang có thiết bị) =====
const typeGroups = computed(() => {
	const map = new Map<string, { id: string; name: string; icon: string; count: number }>();
	for (const dev of filteredDevices.value) {
		const id = dev.equipmentTypeId ?? '__none__';
		if (!map.has(id)) {
			map.set(id, { id, name: typeName(id) || $t('lz.label.vmsDeviceStatus.unknown'), icon: typeIcon(id), count: 0 });
		}
		map.get(id)!.count++;
	}
	return Array.from(map.values()).sort((a, b) => a.name.localeCompare(b.name));
});

const devicesOfType = (id: string) => filteredDevices.value.filter((d) => (d.equipmentTypeId ?? '__none__') === id);

// ===== Nạp dữ liệu =====
const loadTypeList = async () => {
	const resType = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet(undefined, undefined, undefined, undefined, undefined, true as any);
	state.typeList = resType.data.result ?? [];
};

const loadDevices = async (showLoading = false) => {
	if (showLoading) state.loading = true;
	try {
		const params = { page: 1, pageSize: 1000, field: 'equipmentCode', order: 'asc', descStr: 'desc' } as PageEquipmentLatestInput;
		const res = await getAPI(FmsEquipmentTelemetryApi).apiFmsFmsequipmenttelemetryPageequipmentPost(params);
		state.devices = res.data.result?.records ?? [];
		state.loaded = true;
		// Nếu tab loại đang chọn không còn thiết bị -> quay về "Tất cả".
		if (state.activeType !== '__all__' && !typeGroups.value.some((g) => g.id === state.activeType)) {
			state.activeType = '__all__';
		}
	} finally {
		if (showLoading) state.loading = false;
	}
};

// ===== Loại thiết bị giám sát (SysConfigData: Code = Value = ID loại thiết bị) =====
const MONITORED_TYPE_CODE = BaseConfigTypeEnum.FmsTelemetryEquipmentType;
const monitoredTypeId = (c: SysConfigDataOutput) => c.value || c.code || '';

const loadMonitoredTypes = async () => {
	const res = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataListGet(undefined, undefined, undefined, undefined, MONITORED_TYPE_CODE);
	state.monitoredConfigs = (res.data.result ?? []).filter((c) => c.configTypeCode === MONITORED_TYPE_CODE && monitoredTypeId(c));
};

// Ứng viên trong dialog: loại thiết bị chưa xóa, lọc theo ô tìm kiếm (không dấu)
const normalize = (v: any) => (v ?? '').toString().toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/đ/g, 'd');
const typeCandidates = computed(() => {
	const kw = normalize(state.typeSearch).trim();
	return state.typeList.filter((t) => t.isDelete == null && (!kw || normalize(t.name).includes(kw)));
});

const openTypeDialog = async () => {
	await loadMonitoredTypes();
	state.typeSearch = '';
	state.typeSelection = state.monitoredConfigs.map(monitoredTypeId);
	state.typeDialogVisible = true;
};

// Lưu: thêm dòng cho loại mới tick, xóa dòng của loại bị bỏ tick, rồi tải lại danh sách thiết bị
const saveMonitoredTypes = async () => {
	const current = new Map(state.monitoredConfigs.map((c) => [monitoredTypeId(c), c]));
	const selected = new Set(state.typeSelection);
	const addIds = [...selected].filter((id) => !current.has(id));
	const removeRows = [...current.entries()].filter(([id]) => !selected.has(id)).map(([, c]) => ({ id: c.configDataId ?? c.id }));

	state.typeSaving = true;
	try {
		if (addIds.length) {
			// Lấy toàn bộ loại cấu hình rồi so khớp mã ở FE (không truyền tham số lọc: BE cũ lọc theo code bị lỗi null)
			const typeRes = await getAPI(SysConfigTypeApi).apiCfgsystemSysconfigtypeListGet();
			const configTypeId = (typeRes.data.result ?? []).find((t: any) => t.code === MONITORED_TYPE_CODE)?.id;
			if (!configTypeId) {
				ElMessage.error($t('lz.label.fmsTelemetry.configTypeMissing'));
				return;
			}
			await Promise.all(
				addIds.map((id, idx) =>
					getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataAddPost({
						configTypeId,
						code: id,
						value: id,
						name: typeName(id) || id,
						orderNo: (state.monitoredConfigs.length + idx + 1) * 10,
						status: 1,
					} as AddSysConfigDataInput)
				)
			);
		}
		if (removeRows.length) await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataBatchdeletePost(removeRows);

		ElMessage.success($t('lz.message.base.saveSuccess'));
		state.typeDialogVisible = false;
		await loadMonitoredTypes();
		await loadDevices(true);
	} finally {
		state.typeSaving = false;
	}
};

const handleViewHistory = (row: any) => {
	historyRef.value?.openDialog({
		...row,
		equipmentTypeName: typeName(row.equipmentTypeId),
		equipmentTypeIcon: typeIcon(row.equipmentTypeId),
	});
};

onMounted(async () => {
	state.loading = true;
	try {
		await loadTypeList();
		// Cấu hình loại giám sát chỉ để hiển thị thông tin; lỗi đọc cấu hình không chặn tải thiết bị.
		await loadMonitoredTypes().catch(() => (state.monitoredConfigs = []));
		await loadDevices(false);
	} finally {
		state.loading = false;
	}
	// Tự động làm mới nền (không hiện spinner để tránh nháy).
	refreshTimer = setInterval(() => loadDevices(false), REFRESH_SECONDS * 1000);
});

onUnmounted(() => {
	if (refreshTimer) clearInterval(refreshTimer);
});
</script>

<style scoped>
.telemetry-container {
	width: 100%;
	height: 100%;
}

.toolbar {
	flex: 1 1 auto;
	display: flex;
	align-items: center;
	gap: 10px;
	padding: 3px 7px 8px;
}
.kw-input {
	max-width: 340px;
}
.toolbar-right {
	margin-left: auto;
}

/* Thẻ nội dung lấp đầy phần còn lại dưới thẻ tìm kiếm; chỉ vùng thẻ thiết bị cuộn (tiêu đề + tab loại đứng yên) */
.content-card {
	flex: 1 1 0%;
	display: flex;
	flex-direction: column;
	min-height: 0;
}
.content-card :deep(.el-card__body) {
	flex: 1 1 auto;
	display: flex;
	flex-direction: column;
	min-height: 0;
}
.content-head {
	display: flex;
	align-items: baseline;
	justify-content: space-between;
	margin-bottom: 6px;
}
.content-title {
	font-size: 15px;
	font-weight: 700;
	color: var(--el-text-color-primary);
}
.content-info {
	font-size: 12px;
	color: var(--el-text-color-secondary);
}
.dot-sep {
	margin: 0 4px;
}
.monitor-all {
	color: var(--el-color-warning);
}

/* Danh sách loại thiết bị trong dialog chọn giám sát */
.type-check-list {
	display: grid;
	grid-template-columns: repeat(2, minmax(0, 1fr));
	gap: 8px;
	max-height: 360px;
	overflow-y: auto;
}
.type-check-list :deep(.el-checkbox) {
	margin-right: 0;
	width: 100%;
	overflow: hidden;
}
.type-check-label {
	display: inline-block;
	max-width: 190px;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
	vertical-align: middle;
}

.card-grid {
	display: grid;
	grid-template-columns: repeat(var(--cols, 4), minmax(0, 1fr));
	gap: 12px;
	align-items: stretch;
}
@media (max-width: 1400px) {
	.card-grid {
		grid-template-columns: repeat(3, minmax(0, 1fr));
	}
}
@media (max-width: 1000px) {
	.card-grid {
		grid-template-columns: repeat(2, minmax(0, 1fr));
	}
}
@media (max-width: 640px) {
	.card-grid {
		grid-template-columns: 1fr;
	}
}

.type-tabs {
	flex: 1 1 auto;
	display: flex;
	flex-direction: column;
	min-height: 0;
}
:deep(.type-tabs .el-tabs__content) {
	flex: 1 1 auto;
	min-height: 0;
	overflow-y: auto;
	padding: 2px 4px 4px 0; /* chừa chỗ bóng hover của thẻ khi cuộn */
}
</style>
