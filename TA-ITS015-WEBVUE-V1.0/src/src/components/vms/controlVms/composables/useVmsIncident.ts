import { computed } from 'vue';
import { ElMessage } from 'element-plus';
import { getAPI } from '/@/utils/axios-utils';
import { VmsEventDefaultApi } from '/@/api-services/vms';
import { TmsEquipmentTypeApi } from '/@/api-services/tms';
import { VmsActionType, VmsMessageForIncidentEnum, VmsDeviceType } from '/@/types/enums/vmsEnum';
import { TmsEventType, TmsEquipmentType } from '/@/types/enums/tmsEnum';
import { BaseConfigTypeEnum } from '/@/types/enums/baseEnum';
import type { VmsControlDeps } from './types';

/**
 * Logic biến đổi dữ liệu cho mode incident:
 * - group danh sách thiết bị gợi ý theo hướng
 * - chọn câu mặc định theo loại sự cố
 * Lưu ý: hàm gọi API loadRecommendedEquipmentList giữ ở index (nhóm data).
 */
export function useVmsIncident(deps: VmsControlDeps) {
	const { state, props, $t, getConfigDataByCode } = deps;
	const deviceType = (props as any).deviceType ?? VmsDeviceType.Vms;

	// Group recommendedEquipmentList theo directionName để UI hiển thị gọn.
	const recommendedEquipmentGroups = computed(() => {
		const directionMap = new Map((state.directionList ?? []).map((item: any) => [String(item.code), item.name]));
		const groups: Record<string, any[]> = {};

		(state.recommendedEquipmentList ?? []).forEach((item: any) => {
			const equipment = state.equipmentList?.find((equipmentItem: any) => equipmentItem.id === item.id || equipmentItem.id === item.equipmentId);
			const directionId = item.directionId ?? equipment?.directionId;
			const directionName = item.directionName ?? equipment?.directionName ?? (directionId != null ? directionMap.get(String(directionId)) : null) ?? $t('lz.label.base.noData');

			if (!groups[directionName]) groups[directionName] = [];
			groups[directionName].push(item);
		});

		Object.keys(groups).forEach((key) => {
			groups[key].sort((left: any, right: any) => (left?.name ?? '').localeCompare(right?.name ?? ''));
		});

		return groups;
	});

	// Nếu actionType là Text, chọn câu mặc định theo eventTypeCode: incident/accident/fire/traffic jam.
	const setDefaultVmsMessage = (actionType: string) => {
		let messageDefault1 = null;
		let messageDefault2 = null;

		if (actionType != VmsActionType.Text) return;

		const eventTypeCode = state.eventTypeCode?.toLowerCase() ?? '';

		if (eventTypeCode == TmsEventType.Incident || eventTypeCode.includes('incident')) {
			messageDefault1 = state.vmsMessageDefaultList?.find((item: any) => item.code == VmsMessageForIncidentEnum.VMS_INCIDENT_MESSAGE_ROW1)?.value;
			messageDefault2 = state.vmsMessageDefaultList?.find((item: any) => item.code == VmsMessageForIncidentEnum.VMS_INCIDENT_MESSAGE_ROW2)?.value;
		} else if (eventTypeCode == TmsEventType.Accident || eventTypeCode.includes('accident')) {
			messageDefault1 = state.vmsMessageDefaultList?.find((item: any) => item.code == VmsMessageForIncidentEnum.VMS_ACCIDENT_MESSAGE_ROW1)?.value;
			messageDefault2 = state.vmsMessageDefaultList?.find((item: any) => item.code == VmsMessageForIncidentEnum.VMS_ACCIDENT_MESSAGE_ROW2)?.value;
		} else if (eventTypeCode == TmsEventType.Fire || state.eventTypeCode == TmsEventType.FA || eventTypeCode.includes('fire') || eventTypeCode.includes('fa')) {
			messageDefault1 = state.vmsMessageDefaultList?.find((item: any) => item.code == VmsMessageForIncidentEnum.VMS_FIRE_MESSAGE_ROW1)?.value;
			messageDefault2 = state.vmsMessageDefaultList?.find((item: any) => item.code == VmsMessageForIncidentEnum.VMS_FIRE_MESSAGE_ROW2)?.value;
		} else if (eventTypeCode == TmsEventType.TrafficJam || eventTypeCode.includes('traffic')) {
			messageDefault1 = state.vmsMessageDefaultList?.find((item: any) => item.code == VmsMessageForIncidentEnum.VMS_TRAFFIC_JAM_MESSAGE_ROW1)?.value;
			messageDefault2 = state.vmsMessageDefaultList?.find((item: any) => item.code == VmsMessageForIncidentEnum.VMS_TRAFFIC_JAM_MESSAGE_ROW2)?.value;
		}

		if (messageDefault1 || messageDefault2) {
			state.ruleForm.row1 = messageDefault1;
			state.ruleForm.row2 = messageDefault2;
		}
	};

	// trả vẽ các hàm để component cha gọi khi cần thiết từ index.vue (controlVms).
	// Parse chuỗi JSON an toàn (lỗi -> object rỗng).
	const parseJsonSafe = (raw: any): Record<string, any> => {
		try {
			return typeof raw === 'string' ? JSON.parse(raw) || {} : raw || {};
		} catch {
			return {};
		}
	};

	// Thay token động ({VehiclePlate}, {VehicleSpeed}...) trong chuỗi bằng giá trị từ metadata sự cố.
	// key = code bỏ dấu {}; thiếu key -> thay chuỗi rỗng.
	const replaceDynamicTokens = (text: string, metadataObj: Record<string, any>, tokenList: any[]): string => {
		if (!text) return text ?? '';
		let result = text;
		(tokenList ?? []).forEach((token: any) => {
			const code = token?.code;
			if (!code) return;
			const key = String(code).replace(/[{}]/g, '');
			const value = metadataObj?.[key];
			result = result.split(code).join(value != null ? String(value) : '');
		});
		return result;
	};

	// Bấm "Nội dung mặc định" ở luồng sự cố (incident):
	//  - Lấy nội dung mặc định theo loại sự kiện (VmsEventDefault) + loại thiết bị (VMS/LCS).
	//  - Text/Message/Phrase: đổ row1/row2 (đã thay token động từ metadata) + màu/nền/font/cỡ chữ từ DefaultConfig.
	//  - Image (LCS): đổ đường dẫn ảnh mặc định.
	// Trả về true nếu đã áp dụng (để index gọi updatePreview()).
	// silent = true: dùng cho auto-fill khi mở dialog -> không hiện toast "không có dữ liệu".
	const applyEventDefaultContent = async (silent = false): Promise<boolean> => {
		const eventTypeId = state.incidentInfo?.eventTypeId;
		if (!eventTypeId) {
			if (!silent) ElMessage.info($t('lz.label.base.noData'));
			return false;
		}

		try {
			// Loại thiết bị tương ứng deviceType (VMS/LCS)
			const equipmentTypeCode = deviceType === VmsDeviceType.Lcs ? TmsEquipmentType.LCS : TmsEquipmentType.Vms;
			const equipmentTypeRes = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet('', equipmentTypeCode);
			const equipmentTypeId = equipmentTypeRes.data?.result?.length ? equipmentTypeRes.data.result[0].id : null;

			// Nội dung mặc định theo loại sự kiện sự cố
			const eventDefaultRes = await getAPI(VmsEventDefaultApi).apiVmsVmseventdefaultListGet(undefined, undefined, undefined, eventTypeId);
			const eventDefaultList = eventDefaultRes.data?.result ?? [];

			// Lọc theo loại thiết bị, lấy bản ghi đầu tiên
			const record = eventDefaultList.find((item: any) => !equipmentTypeId || item.equipmentTypeId === equipmentTypeId);
			if (!record) {
				if (!silent) ElMessage.info($t('lz.label.base.noData'));
				return false;
			}

			const cfg = parseJsonSafe(record.defaultConfig);
			const type = state.ruleForm.controlType;

			if (type === VmsActionType.Image) {
				if (!record.url) {
					if (!silent) ElMessage.info($t('lz.label.base.noData'));
					return false;
				}
				state.ruleForm.imgTemplate = ''; // bỏ chọn ảnh thư viện
				state.ruleForm.urlImage = record.url;
				return true;
			}

			if ([VmsActionType.Text, VmsActionType.Message, VmsActionType.Phrase].includes(type)) {
				const metadataObj = parseJsonSafe(state.incidentInfo?.metadata);
				const tokenList = getConfigDataByCode(BaseConfigTypeEnum.VmsContentDefault) ?? [];

				const rowData = parseJsonSafe(record.rowData);
				const row1 = rowData?.Row1 ?? rowData?.row1 ?? cfg?.Row1 ?? '';
				const row2 = rowData?.Row2 ?? rowData?.row2 ?? cfg?.Row2 ?? '';

				state.ruleForm.row1 = replaceDynamicTokens(row1, metadataObj, tokenList);
				state.ruleForm.row2 = replaceDynamicTokens(row2, metadataObj, tokenList);

				// Màu/nền/font/cỡ chữ từ DefaultConfig (chỉ set khi có giá trị, tránh ghi đè bằng rỗng)
				if (cfg?.Color) state.ruleForm.textColor = cfg.Color;
				if (cfg?.BackColor) state.ruleForm.bgColor = cfg.BackColor;
				if (cfg?.Font) state.ruleForm.selectedFont = cfg.Font;
				const fontSize = parseInt(cfg?.FontSize);
				if (!isNaN(fontSize)) state.ruleForm.fontSize = fontSize;

				// Chế độ phát & thời gian hiển thị mặc định từ cấu hình sự cố
				if (record.defaultPlayMode) state.ruleForm.playMode = record.defaultPlayMode;
				if (record.timeToLive != null && record.timeToLive > 0) state.ruleForm.timeToLive = record.timeToLive;

				return true;
			}

			// Loại điều khiển khác (Template/Time/Schedule) không có nội dung mặc định theo sự kiện
			if (!silent) ElMessage.info($t('lz.label.base.noData'));
			return false;
		} catch (error) {
			console.error('applyEventDefaultContent', error);
			return false;
		}
	};

	return { recommendedEquipmentGroups, setDefaultVmsMessage, applyEventDefaultContent };
}
