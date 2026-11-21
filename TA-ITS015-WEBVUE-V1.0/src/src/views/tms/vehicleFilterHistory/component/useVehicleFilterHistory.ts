import { computed, reactive } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';
import { useDateFormat } from '@vueuse/core';
import { vdsTimestamp } from '/@/utils/vdsTimestamp';
import { useCommonVarsAndFuncs } from '/@/stores/its/commonVarsAndFuncs';
import { useMapStore } from '/@/stores/its/mapStore';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { DateTimeFormatEnum } from '/@/types/enums/baseEnum';

// KienLT - 09/11/2026: phần dùng chung của 2 tab menu "Theo lượt" (index.vue) và "Theo chuyến" (trip/index.vue).

/** Chỉ camera VDS mới ghi lượt lọc xe, nên ô lọc Camera chỉ liệt kê đúng loại thiết bị này. */
const VDS_EQUIPMENT_TYPE_CODE = 'VDS';
/** API lịch sử lọc xe (cả lượt lẫn chuyến) chỉ nhận khoảng thời gian tối đa 60 ngày. */
const MAX_RANGE_DAYS = 60;
const DEFAULT_RANGE_DAYS = 3;

export interface VehicleFilterQuery {
	licensePlate?: string;
	direction?: string;
	type?: string;
	equipmentId?: string;
	fromDate?: string;
	toDate?: string;
}

/** Bộ lọc, danh mục cấu hình và cách hiển thị camera; mỗi tab gọi một lần (bộ lọc riêng từng tab). */
export function useVehicleFilterHistory() {
	const { t: $t } = useI18n();
	const commonVarsAndFuncs = useCommonVarsAndFuncs();
	const mapStore = useMapStore();
	const baseConfig = useBaseConfig();

	const defaultRange = () => ({
		fromDate: commonVarsAndFuncs.getISODatetimeFromDays(-DEFAULT_RANGE_DAYS),
		toDate: commonVarsAndFuncs.getISODatetimeFromDays(0),
	});

	const queryParams = reactive<VehicleFilterQuery>({ ...defaultRange() });

	const lists = reactive({
		locationList: [] as any[],
		laneList: [] as any[],
		directionList: [] as any[],
		filterTypeList: [] as any[],
	});

	const loadConfigLists = () => {
		lists.locationList = baseConfig.getConfigDataByCode(BaseConfigTypeEnum.TmsLocationConfigType) ?? [];
		lists.filterTypeList = baseConfig.getConfigDataByCode(BaseConfigTypeEnum.TmsVehicleFilterType) ?? [];
		lists.directionList = baseConfig.getConfigDataByCode(BaseConfigTypeEnum.TmsDirectionType) ?? [];
		lists.laneList = baseConfig.getConfigDataByCode(BaseConfigTypeEnum.TmsLaneType) ?? [];
	};

	/* ------------------------------ Camera: tra tên + lý trình ------------------------------ */

	/** Lý trình dạng "Km X+YYY"; dùng chung cho cột Camera, ô lọc và Lộ trình. */
	const formatKm = (km?: number | null, met?: number | null) => `Km ${km ?? 0}+${String(met ?? 0).padStart(3, '0')}`;

	/** "Tên camera (Km X+YYY)"; thiết bị chưa khai báo lý trình thì chỉ hiện tên. */
	const cameraLabel = (equipment: any) => {
		const name = equipment?.name || equipment?.code || String(equipment?.id ?? '');
		if (equipment?.kmNumber == null && equipment?.metNumber == null) return name;
		return `${name} (${formatKm(equipment.kmNumber, equipment.metNumber)})`;
	};

	/** Tra theo toàn bộ thiết bị (không chỉ VDS) để dòng cũ/thiết bị đã đổi loại vẫn hiện được tên. */
	const equipmentById = computed(() => {
		const map = new Map<string, any>();
		for (const equipment of mapStore.equipmentList ?? []) {
			if (equipment?.id != null) map.set(String(equipment.id), equipment);
		}
		return map;
	});

	/** Id không tra được thì rút gọn 8 ký tự đầu để cột không bị dài. */
	const shortId = (id: string) => (id.length > 8 ? `${id.slice(0, 8)}…` : id);

	const cameraLabelById = (equipmentId?: string | null) => {
		if (!equipmentId) return '—';
		const equipment = equipmentById.value.get(String(equipmentId));
		return equipment ? cameraLabel(equipment) : shortId(String(equipmentId));
	};

	/** Lộ trình camera theo thời gian: "VDS-01 (Km 10+200) → VDS-03 (Km 13+500) → …" (bỏ camera lặp liền kề). */
	const formatRoute = (equipmentIds?: Array<string> | null) => {
		const ids = (equipmentIds ?? []).filter((id, index, all) => !!id && id !== all[index - 1]);
		return ids.length ? ids.map((id) => cameraLabelById(id)).join(' → ') : '—';
	};

	const vdsCameraList = computed(() => {
		const vdsTypeIds = new Set(
			(mapStore.equipmentTypeList ?? [])
				.filter((equipmentType: any) => String(equipmentType?.code ?? '').trim().toUpperCase() === VDS_EQUIPMENT_TYPE_CODE)
				.map((equipmentType: any) => String(equipmentType.id))
		);
		return (mapStore.equipmentList ?? [])
			.filter((equipment: any) => equipment?.id != null && vdsTypeIds.has(String(equipment.equipmentTypeId)))
			.sort((first: any, second: any) => cameraLabel(first).localeCompare(cameraLabel(second), 'vi'));
	});

	/** Dùng lại danh sách thiết bị của mapStore; chỉ gọi API khi store chưa nạp. */
	const loadEquipmentOptions = async () => {
		try {
			await Promise.all([
				(mapStore.equipmentList?.length ?? 0) === 0 ? mapStore.fetchEquipmentList() : Promise.resolve(),
				(mapStore.equipmentTypeList?.length ?? 0) === 0 ? mapStore.fetchEquipmentTypeList() : Promise.resolve(),
			]);
		} catch (error) {
			console.error('Không tải được danh sách camera VDS.', error);
		}
	};

	/* ------------------------------ Định dạng chung ------------------------------ */

	/** Thời gian từ API là UTC (có thể thiếu hậu tố Z) → hiển thị giờ địa phương. */
	const formatUtcTime = (value?: string | Date | null) =>
		value ? useDateFormat(vdsTimestamp(value as string | Date), DateTimeFormatEnum.DateTimeDefault).value : '';

	const getFilterTypeItem = (code?: string | null) =>
		code ? baseConfig.getConfigItemByCode(BaseConfigTypeEnum.TmsVehicleFilterType, code) : null;

	const findName = (list: any[], code?: string | number | null) => list?.find((item: any) => item.code == code)?.name;

	/* ------------------------------ Bộ lọc ------------------------------ */

	/** Bộ lọc gửi API; ném lỗi nếu khoảng thời gian không hợp lệ (tối đa 60 ngày). */
	const buildFilterParams = () => {
		const params = {
			licensePlate: queryParams.licensePlate || undefined,
			direction: queryParams.direction || undefined,
			type: queryParams.type || undefined,
			equipmentId: queryParams.equipmentId || undefined,
			fromDate: new Date(queryParams.fromDate ?? ''),
			toDate: new Date(queryParams.toDate ?? ''),
		};
		if (
			!queryParams.fromDate ||
			!queryParams.toDate ||
			!Number.isFinite(params.fromDate.getTime()) ||
			!Number.isFinite(params.toDate.getTime()) ||
			params.toDate <= params.fromDate ||
			params.toDate.getTime() - params.fromDate.getTime() > MAX_RANGE_DAYS * 86400000
		) {
			ElMessage.error($t('lz.validation.tmsVehicleFilterHistory.timeRange'));
			throw new Error($t('lz.validation.tmsVehicleFilterHistory.timeRange'));
		}
		return params;
	};

	const resetFilters = () => {
		Object.assign(queryParams, { licensePlate: undefined, direction: undefined, type: undefined, equipmentId: undefined, ...defaultRange() });
	};

	return {
		queryParams,
		lists,
		vdsCameraList,
		loadConfigLists,
		loadEquipmentOptions,
		cameraLabel,
		cameraLabelById,
		formatRoute,
		formatUtcTime,
		getFilterTypeItem,
		findName,
		buildFilterParams,
		resetFilters,
	};
}

export type VehicleFilterHistoryContext = ReturnType<typeof useVehicleFilterHistory>;
