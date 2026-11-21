<template>
	<div class="cctv-equipment-container">
		<!-- Search / Filter -->
		<el-card shadow="hover"
			:body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inline-message="true"
				label-width="auto" style="flex: 1 1 0%" label-position="left">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.status')" prop="status">
							<el-select v-model="state.queryParams.status" filterable clearable class="w100"
								@clear="state.queryParams.status = undefined"
								:placeholder="$t('lz.label.base.pleaseSelectStatus')">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)"
									:key="item.code" :label="item.name" :value="parseInt(item.code)" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.cctvEquipment.deviceName')" prop="name">
							<el-input v-model="state.queryParams.name"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.label.cctvEquipment.deviceName').toLowerCase() })"
								clearable @keyup.enter="handleQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.ipAddress')" prop="ip">
							<el-input v-model="state.queryParams.ip"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.ipAddress').toLowerCase() })"
								clearable @keyup.enter="handleQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.cctvEquipment.equipmentType')" prop="equipmentType">
							<el-select v-model="state.queryParams.equipmentType" filterable clearable class="w100"
								@clear="state.queryParams.equipmentType = ''"
								:placeholder="$t('lz.label.cctvEquipment.selectEquipmentType')">
								<el-option v-for="item in state.equipmentTypes" :key="item.id" :label="item.name"
									:value="item.name" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>

			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />

			<el-row>
				<SearchButton :auth-search="'cctvEquipments:view'" @search="handleQuery" @reset="resetQuery"
					:loading="options.loading" />
			</el-row>
		</el-card>

		<!-- Table -->
		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGrid" class="xGrid-style cctv-white-grid" v-bind="options" v-on="gridEvents"
				@checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">

				<template #toolbar_tools>
					<el-row wrap justify="end" class="lt-sm:w-200px">
						<el-button type="success" icon="ele-Refresh" @click="handleSync" :loading="state.syncing"
							v-auth="'cctvEquipments:sync'">
							{{ $t('lz.button.cctvEquipment.syncData') }}
						</el-button>
						<el-button type="danger" v-show="state.showBatchDelete" icon="ele-Delete"
							@click="handleBatchDelete" v-auth="'cctvEquipments:delete'">
							{{ $t('lz.button.base.delete') }}
						</el-button>
						<el-button icon="ele-Refresh" @click="handleQuery">
							{{ $t('lz.button.base.refresh') }}
						</el-button>
						<el-button size="small" @click="handleCustom" icon="ele-Setting">
							{{ $t('lz.button.base.columnSetting') }}
						</el-button>
					</el-row>
				</template>

				<template #row_status="{ row }">
					<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, row.status)" />
				</template>

				<template #row_snapshot="{ row }">
					<CameraSnapshotThumbnail :snapshot-url="row.snapshotUrl" :snapshot-time="row.snapshotTime"
						:camera-name="row.name" :cache-key="state.snapshotCacheKey" />
				</template>

				<template #row_deviceState="{ row }">
					<el-tag :type="row.deviceState === 1 ? 'success' : row.deviceState === 0 ? 'danger' : 'info'"
						effect="plain">
						{{ getDeviceStateLabel(row.deviceState) }}
					</el-tag>
				</template>

				<template #row_lastSyncTime="{ row }">
					{{ row.lastSyncTime ? formatDate(row.lastSyncTime) : '' }}
				</template>

				<template #row_createTime="{ row }">
					{{ row.createTime ? formatDate(row.createTime) : '' }}
				</template>

				<template #row_updateTime="{ row }">
					{{ row.updateTime ? formatDate(row.updateTime) : '' }}
				</template>

				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>

				<template #empty>
					<el-empty :image-size="200" />
				</template>

				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.detail')" placement="top" :auto-close="1000">
						<el-button icon="ele-View" text type="primary" @click="handleDetail(row)"
							v-auth="'cctvEquipments:view'"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.cctvEquipment.toggleActive')" placement="top"
						:auto-close="1000">
						<el-button icon="ele-Switch" text type="warning" @click="handleToggleActive(row)"
							v-auth="'cctvEquipments:active'"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger" @click="handleDelete(row)"
							v-auth="'cctvEquipments:delete'"> </el-button>
					</el-tooltip>
				</template>
			</vxe-grid>
		</el-card>

		<!-- Detail Dialog -->
		<el-dialog v-model="state.detailVisible" width="600px" draggable :close-on-click-modal="false">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle">
						<ele-View />
					</el-icon>
					<span> {{ $t('lz.label.cctvEquipment.deviceDetail') }} </span>
				</div>
			</template>
			<el-descriptions :column="1" border v-if="state.detailRow">
				<el-descriptions-item :label="$t('lz.entity.base.id')">{{ state.detailRow.id }}</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.label.cctvEquipment.deviceId')">{{ state.detailRow.deviceId
				}}</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.entity.base.name')">{{ state.detailRow.name
				}}</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.entity.base.ipAddress')">{{ state.detailRow.ip
				}}</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.label.cctvEquipment.deviceState')">
					<el-tag
						:type="state.detailRow.deviceState === 1 ? 'success' : state.detailRow.deviceState === 0 ? 'danger' : 'info'"
						effect="plain">
						{{ getDeviceStateLabel(state.detailRow.deviceState) }}
					</el-tag>
				</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.entity.base.status')">
					<TagInfo v-if="state.detailRow?.status"
						:config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, state.detailRow?.status.toString())" />
					<TagInfo v-else :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, '0')" />

				</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.label.cctvEquipment.lastSyncTime')">{{ state.detailRow.lastSyncTime
					?
					formatDate(state.detailRow.lastSyncTime) : '' }}</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.entity.base.createTime')">{{ state.detailRow.createTime ?
					formatDate(state.detailRow.createTime) :
					'' }}</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.entity.base.updateTime')">{{ state.detailRow.updateTime ?
					formatDate(state.detailRow.updateTime) : '' }}</el-descriptions-item>
			</el-descriptions>
			<template #footer>
				<el-button @click="state.detailVisible = false">{{ $t('lz.button.base.close') }}</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="cctvEquipments">
import { onMounted, reactive, ref, defineAsyncComponent } from 'vue';
import { ElMessageBox, ElMessage, dayjs } from 'element-plus';
import { VxeGridInstance, VxeGridListeners } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { getAPI } from '/@/utils/axios-utils';
import { CctvDeviceApi } from '/@/api-services/cctv';
import { CctvDeviceOutput } from '/@/api-services/cctv';
import type { CctvDeviceViewOutput } from '/@/api-services/cctv';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import { useI18n } from 'vue-i18n';

const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
const CameraSnapshotThumbnail = defineAsyncComponent(() => import('./component/CameraSnapshotThumbnail.vue'));
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const { t: $t } = useI18n();

const xGrid = ref<VxeGridInstance>();

const state = reactive({
	queryParams: {
		status: undefined as unknown as any,
		name: undefined as unknown as string,
		ip: undefined as unknown as string,
		equipmentType: undefined as unknown as string,
	},
	showBatchDelete: false,
	syncing: false,
	detailVisible: false,
	detailRow: null as CctvDeviceOutput | null,
	equipmentTypes: [] as { id: string; name: string }[],
	snapshotCacheKey: Date.now(),
});

// Format date helper
const formatDate = (date: any) => {
	return date ? dayjs(date).format('DD/MM/YYYY HH:mm:ss') : '';
};

const getDeviceStateLabel = (deviceState?: number | null) => {
	if (deviceState === 1) return $t('lz.label.cctvEquipment.online');
	if (deviceState === 0) return $t('lz.label.cctvEquipment.offline');
	return $t('lz.label.base.unknown');
};

const getTimeValue = (date: any) => {
	return date ? dayjs(date).valueOf() : 0;
};

const normalizeText = (value: any) => {
	return String(value ?? '').trim().toLowerCase();
};

const matchesText = (value: any, keyword: any) => {
	const normalizedKeyword = normalizeText(keyword);
	if (!normalizedKeyword) return true;
	return normalizeText(value).includes(normalizedKeyword);
};

const filterDevices = (devices: CctvDeviceOutput[]) => {
	const { name, ip, status, equipmentType } = state.queryParams;
	// Nếu có filter type thì dùng mapping từ deviceview
	return devices
		.filter((item) => matchesText(item.name, name))
		.filter((item) => matchesText(item.ip, ip))
		.filter((item) => status === undefined || status === null || String(item.status) === String(status))
		.filter((item) => {
			if (!equipmentType) return true;
			// Match bằng deviceId trong deviceViewMap
			const deviceId = item.deviceId || item.id || '';
			const viewInfo = deviceViewMap.get(deviceId);
			return viewInfo?.equipmentTypeName === equipmentType;
		})
		.sort((a, b) => getTimeValue(b.lastSyncTime) - getTimeValue(a.lastSyncTime));
};

// Map deviceId -> CctvDeviceViewOutput để tra cứu equipmentTypeName
const deviceViewMap = new Map<string, CctvDeviceViewOutput>();

const loadEquipmentTypes = async () => {
	try {
		const res = await getAPI(CctvDeviceApi).apiCctvCctvdeviceDeviceviewGet(undefined, undefined);
		const devices = (res.data as any)?.result ?? [];
		const typeMap = new Map<string, string>();
		deviceViewMap.clear();
		(devices as CctvDeviceViewOutput[]).forEach((device) => {
			if (device.deviceId) deviceViewMap.set(device.deviceId, device);
			if (device.id) deviceViewMap.set(device.id, device);
			if (device.equipmentTypeId && device.equipmentTypeName) {
				typeMap.set(device.equipmentTypeId, device.equipmentTypeName);
			}
		});
		state.equipmentTypes = [...typeMap.entries()].map(([id, name]) => ({ id, name }));
	} catch {
		state.equipmentTypes = [];
	}
};

const getDeviceKey = (row: CctvDeviceOutput) => {
	return row.deviceId || row.id || '';
};

const getErrorMessage = (e: any) => {
	return e?.response?.data?.message || e?.message || $t('lz.message.cctvEquipment.unknownError');
};

const isCancelAction = (e: any) => {
	return e === 'cancel' || e === 'close';
};

const assertApiSucceeded = (res: any, fallbackMessage: string) => {
	const data = res?.data;
	if (data?.succeeded === false || (data?.code !== undefined && data.code !== 200)) {
		throw new Error(data?.message || fallbackMessage);
	}
};

// Table configuration - API trả về list (không phân trang), load data thủ công
const options = useVxeTable<CctvDeviceOutput>(
	{
		id: 'cctvEquipments',
		name: 'CCTV_Devices',
		columns: [
			{ type: 'checkbox', fixed: 'left', width: 45 },
			{ field: 'snapshotUrl', title: $t('lz.label.cctvEquipment.thumbnail'), width: 100, showOverflow: false, slots: { default: 'row_snapshot' } },
			{ field: 'deviceId', title: $t('lz.label.cctvEquipment.deviceId'), minWidth: 200, showOverflow: 'tooltip', sortable: true },
			{ field: 'name', title: $t('lz.label.cctvEquipment.deviceName'), minWidth: 200, showOverflow: 'tooltip', sortable: true },
			{ field: 'ip', title: $t('lz.entity.base.ipAddress'), minWidth: 140, showOverflow: 'tooltip', sortable: true },
			{ field: 'deviceState', title: $t('lz.label.cctvEquipment.deviceState'), width: 120, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_deviceState' } },
			{ field: 'status', title: $t('lz.entity.base.status'), width: 110, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_status' } },
			{ field: 'lastSyncTime', title: $t('lz.label.cctvEquipment.lastSyncTime'), minWidth: 170, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_lastSyncTime' } },
			{ field: '', title: $t('lz.label.base.detail'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 130, showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	{
		proxyConfig: { enabled: false },
		pagerConfig: { enabled: false },
		toolbarConfig: { export: false },
		sortConfig: { remote: false, trigger: 'cell' },
		rowConfig: { height: 64 },
		data: [],
	}
);

// Page initialization
onMounted(async () => {
	await loadEquipmentTypes();
	await handleQuery();
});

// Query operation - gọi API trực tiếp và set data vào grid
const handleQuery = async () => {
	options.loading = true;
	try {
		const { name, ip } = state.queryParams;
		const res = await getAPI(CctvDeviceApi).apiCctvCctvdeviceDevicelistGet(
			undefined,        // deviceId
			ip || undefined,  // ip
			name || undefined // name
		);
		assertApiSucceeded(res, $t('lz.message.cctvEquipment.loadListError'));
		options.data = filterDevices(res.data?.result ?? []);
	} catch (e: any) {
		ElMessage.error($t('lz.message.cctvEquipment.loadListErrorWithReason', { error: getErrorMessage(e) }));
		options.data = [];
	} finally {
		options.loading = false;
		selectChangeEvent();
	}
};

// Reset operation
const resetQuery = async () => {
	state.queryParams.status = undefined as unknown as any;
	state.queryParams.name = undefined as unknown as string;
	state.queryParams.ip = undefined as unknown as string;
	state.queryParams.equipmentType = undefined as unknown as string;
	await handleQuery();
};

// Sync devices from NxVMS
const handleSync = async () => {
	try {
		state.syncing = true;
		const res = await getAPI(CctvDeviceApi).apiCctvCctvdeviceSyncdevicePost({});
		assertApiSucceeded(res, $t('lz.message.cctvEquipment.syncFailed'));
		// Chỉ cần biết đồng bộ thành công, không hiển thị chi tiết số liệu
		ElMessage.success($t('lz.message.cctvEquipment.syncSuccess'));
		state.snapshotCacheKey = Date.now();
		await handleQuery();
	} catch (e: any) {
		ElMessage.error($t('lz.message.cctvEquipment.syncFailedWithReason', { error: getErrorMessage(e) }));
	} finally {
		state.syncing = false;
	}
};

// Toggle active status
const handleToggleActive = (row: CctvDeviceOutput) => {
	const deviceId = getDeviceKey(row);
	if (!deviceId) {
		ElMessage.warning($t('lz.message.cctvEquipment.deviceIdNotFound'));
		return;
	}
	const isDeactivating = row.status === 1;
	const action = $t(isDeactivating ? 'lz.button.cctvEquipment.deactivate' : 'lz.button.cctvEquipment.activate');
	ElMessageBox.confirm($t('lz.message.cctvEquipment.confirmToggleActive', { action: action.toLowerCase(), name: row.name }), $t('lz.label.base.confirm'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			const res = await getAPI(CctvDeviceApi).apiCctvCctvdeviceActivePost({ deviceId, ip: row.ip || undefined });
			assertApiSucceeded(res, $t('lz.message.cctvEquipment.toggleActiveFailed', { action: action.toLowerCase() }));
			ElMessage.success($t('lz.message.cctvEquipment.toggleActiveSuccess', { action: action.toLowerCase() }));
			await handleQuery();
		})
		.catch((e) => {
			if (!isCancelAction(e)) ElMessage.error($t('lz.message.cctvEquipment.toggleActiveFailedWithReason', { action, error: getErrorMessage(e) }));
		});
};

// Delete single
const handleDelete = (row: CctvDeviceOutput) => {
	const deviceId = getDeviceKey(row);
	if (!deviceId) {
		ElMessage.warning($t('lz.message.cctvEquipment.deviceIdNotFound'));
		return;
	}
	ElMessageBox.confirm($t('lz.message.cctvEquipment.confirmDelete', { name: row.name }), $t('lz.label.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			const res = await getAPI(CctvDeviceApi).apiCctvCctvdeviceDeletePost({ deviceId, ip: row.ip || undefined });
			assertApiSucceeded(res, $t('lz.message.cctvEquipment.deleteFailed'));
			ElMessage.success($t('lz.message.cctvEquipment.deleteSuccess'));
			await handleQuery();
		})
		.catch((e) => {
			if (!isCancelAction(e)) ElMessage.error($t('lz.message.cctvEquipment.deleteFailedWithReason', { error: getErrorMessage(e) }));
		});
};

// Batch delete
const handleBatchDelete = async () => {
	const selectRecords = getCheckedRecords();
	const invalidRecords = selectRecords.filter((record) => !getDeviceKey(record));
	if (invalidRecords.length > 0) {
		ElMessage.warning($t('lz.message.cctvEquipment.missingDeviceIdCount', { count: invalidRecords.length }));
		return;
	}
	ElMessageBox.confirm($t('lz.message.cctvEquipment.confirmBatchDelete', { count: selectRecords.length }), $t('lz.label.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			for (const record of selectRecords) {
				const res = await getAPI(CctvDeviceApi).apiCctvCctvdeviceDeletePost({ deviceId: getDeviceKey(record), ip: record.ip || undefined });
				assertApiSucceeded(res, $t('lz.message.cctvEquipment.deleteFailed'));
			}
			ElMessage.success($t('lz.message.base.deleteSuccess'));
			await handleQuery();
		})
		.catch((e) => {
			if (!isCancelAction(e)) ElMessage.error($t('lz.message.cctvEquipment.batchDeletePartialOrFailed', { error: getErrorMessage(e) }));
		});
};

// View detail
const handleDetail = (row: CctvDeviceOutput) => {
	state.detailRow = row;
	state.detailVisible = true;
};

// Custom column
function handleCustom() {
	xGrid.value?.openCustom();
}

// Get selected records
function getCheckedRecords(): CctvDeviceOutput[] {
	return xGrid.value?.getCheckboxRecords() ?? [];
}

function selectChangeEvent() {
	state.showBatchDelete = getCheckedRecords().length > 0;
}

// Table events
const gridEvents: VxeGridListeners<CctvDeviceOutput> = {};
</script>

<style scoped>
/* :deep(.cctv-white-grid) {
	--vxe-ui-table-header-background-color: var(--el-color-white);
	--vxe-ui-table-footer-background-color: var(--el-color-white);
	--vxe-ui-table-row-striped-background-color: var(--el-color-white);
} */

/* :deep(.cctv-white-grid .vxe-table--body-wrapper),
:deep(.cctv-white-grid .vxe-table--main-wrapper),
:deep(.cctv-white-grid .vxe-table--header-wrapper),
:deep(.cctv-white-grid .vxe-table--footer-wrapper),
:deep(.cctv-white-grid .vxe-body--row),
:deep(.cctv-white-grid .vxe-body--column),
:deep(.cctv-white-grid .vxe-header--column) {
	background-color: var(--el-color-white) !important;
} */

</style>
