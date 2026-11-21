// Giới hạn nhập liệu dùng chung cho các màn ShareData (đăng ký, ánh xạ, nguồn dữ liệu, nhật ký lỗi).
//
// Giá trị đọc từ SysOpConfig nhóm `sharedata_config` qua getOpConfigItemByCode của baseConfig,
// để quản trị đổi min/max/default mà không phải sửa code. Thiếu dòng hoặc giá trị sai thì dùng
// SHAREDATA_FE_CONFIG_FALLBACK (types/constants/sharedataFeConfig.ts).

import { computed } from 'vue';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseOpConfigGroupCodeEnum, SharedataFeConfigCodeEnum } from '/@/types/enums';
import { SHAREDATA_FE_CONFIG_FALLBACK } from '/@/types/constants';

/** Khoảng giá trị của một ô nhập số. */
export interface SharedataRange {
	min: number;
	max: number;
	default: number;
}

/** Khoảng ngày tra cứu của các màn nhật ký. */
export interface SharedataDayRange {
	defaultDays: number;
	maxDays: number;
}

/**
 * Description: Lấy giới hạn nhập liệu ShareData theo cấu hình hệ thống.
 *              Trả về computed vì danh sách cấu hình được nạp bất đồng bộ sau khi đăng nhập.
 *
 * @example
 * const feConfig = useSharedataFeConfig();
 * <el-input-number :min="feConfig.subscriptionInterval.min" :max="feConfig.subscriptionInterval.max" />
 */
export function useSharedataFeConfig() {
	const getOpConfigItemByCode = useBaseConfig().getOpConfigItemByCode;

	/** Giá trị trên DB phải là số nguyên không âm; còn lại coi như chưa cấu hình. */
	const readLimit = (code: SharedataFeConfigCodeEnum): number => {
		const raw = getOpConfigItemByCode(BaseOpConfigGroupCodeEnum.SharedataConfig, code)?.value;
		const value = raw == null || String(raw).trim() === '' ? NaN : Number(raw);
		return Number.isInteger(value) && value >= 0 ? value : SHAREDATA_FE_CONFIG_FALLBACK[code];
	};

	/** Bộ min/max/default lệch nhau (VD max < min) thì dùng nguyên bộ dự phòng, không trộn với giá trị DB. */
	const readRange = (minCode: SharedataFeConfigCodeEnum, maxCode: SharedataFeConfigCodeEnum, defaultCode: SharedataFeConfigCodeEnum): SharedataRange => {
		const range = { min: readLimit(minCode), max: readLimit(maxCode), default: readLimit(defaultCode) };
		if (range.min <= range.default && range.default <= range.max) return range;
		return { min: SHAREDATA_FE_CONFIG_FALLBACK[minCode], max: SHAREDATA_FE_CONFIG_FALLBACK[maxCode], default: SHAREDATA_FE_CONFIG_FALLBACK[defaultCode] };
	};

	const readDayRange = (): SharedataDayRange => {
		const range = { defaultDays: readLimit(SharedataFeConfigCodeEnum.HistoryRangeDefaultDays), maxDays: readLimit(SharedataFeConfigCodeEnum.HistoryRangeMaxDays) };
		if (range.defaultDays >= 1 && range.defaultDays <= range.maxDays) return range;
		return {
			defaultDays: SHAREDATA_FE_CONFIG_FALLBACK[SharedataFeConfigCodeEnum.HistoryRangeDefaultDays],
			maxDays: SHAREDATA_FE_CONFIG_FALLBACK[SharedataFeConfigCodeEnum.HistoryRangeMaxDays],
		};
	};

	const readPositiveLimit = (code: SharedataFeConfigCodeEnum): number => {
		const value = readLimit(code);
		return value >= 1 ? value : SHAREDATA_FE_CONFIG_FALLBACK[code];
	};

	return computed(() => ({
		subscriptionInterval: readRange(SharedataFeConfigCodeEnum.SubscriptionIntervalSecMin, SharedataFeConfigCodeEnum.SubscriptionIntervalSecMax, SharedataFeConfigCodeEnum.SubscriptionIntervalSecDefault),
		subscriptionDebounce: readRange(SharedataFeConfigCodeEnum.SubscriptionDebounceSecMin, SharedataFeConfigCodeEnum.SubscriptionDebounceSecMax, SharedataFeConfigCodeEnum.SubscriptionDebounceSecDefault),
		subscriptionPriority: readRange(SharedataFeConfigCodeEnum.SubscriptionPriorityMin, SharedataFeConfigCodeEnum.SubscriptionPriorityMax, SharedataFeConfigCodeEnum.SubscriptionPriorityDefault),
		partnerHeartbeat: readRange(SharedataFeConfigCodeEnum.PartnerHeartbeatSecMin, SharedataFeConfigCodeEnum.PartnerHeartbeatSecMax, SharedataFeConfigCodeEnum.PartnerHeartbeatSecDefault),
		partnerResponseTimeout: readRange(SharedataFeConfigCodeEnum.PartnerResponseTimeoutSecMin, SharedataFeConfigCodeEnum.PartnerResponseTimeoutSecMax, SharedataFeConfigCodeEnum.PartnerResponseTimeoutSecDefault),
		partnerDatagramSize: readRange(SharedataFeConfigCodeEnum.PartnerDatagramSizeMin, SharedataFeConfigCodeEnum.PartnerDatagramSizeMax, SharedataFeConfigCodeEnum.PartnerDatagramSizeDefault),
		mappingSampleTop: readRange(SharedataFeConfigCodeEnum.MappingSampleTopMin, SharedataFeConfigCodeEnum.MappingSampleTopMax, SharedataFeConfigCodeEnum.MappingSampleTopDefault),
		historyRange: readDayRange(),
		sqlSampleTop: readRange(SharedataFeConfigCodeEnum.SqlSampleTopMin, SharedataFeConfigCodeEnum.SqlSampleTopMax, SharedataFeConfigCodeEnum.SqlSampleTopDefault),
		logListDefaultTop: readPositiveLimit(SharedataFeConfigCodeEnum.LogListDefaultTop),
	}));
}
