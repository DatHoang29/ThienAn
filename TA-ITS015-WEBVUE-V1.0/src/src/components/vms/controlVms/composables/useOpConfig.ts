import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseOpConfigGroupCodeEnum } from '/@/types/enums/baseEnum';

/**
 * Đọc cấu hình vận hành (SysOpConfig) cho luồng điều khiển VMS/LCS.
 *
 * VÌ SAO CẦN FILE NÀY:
 *  1. baseConfig.getOpConfigItemByCode() gọi checkOperationConfigList() -> setOpConfigList() nhưng
 *     KHÔNG await. Lần đọc đầu tiên sau khi mở app luôn trả rỗng, nên mọi giá trị đều rơi về fallback
 *     mà không có dấu hiệu gì. Với phạm vi gợi ý thiết bị, hậu quả là hệ thống dùng 4000m mặc định
 *     thay vì cấu hình thật -> gợi ý sai tập thiết bị.
 *     -> ensureOpConfigLoaded() phải được await TRƯỚC khi đọc.
 *  2. Một số item cấu hình thực tế được đặt ở nhóm khác 'operation_config' (ví dụ 'VMS Config').
 *     -> readOpConfigValue() ưu tiên nhóm chuẩn, không thấy thì quét toàn bộ nhóm theo mã.
 */

/**
 * Đảm bảo danh sách SysOpConfig đã được nạp xong. Gọi nhiều lần vẫn an toàn:
 * đã có dữ liệu thì không gọi lại API.
 */
export const ensureOpConfigLoaded = async (): Promise<void> => {
	const baseConfig = useBaseConfig();
	if (!baseConfig.opConfigList || Object.keys(baseConfig.opConfigList).length === 0) {
		try {
			await baseConfig.setOpConfigList();
		} catch (error) {
			console.error('ensureOpConfigLoaded', error);
		}
	}
};

/**
 * Đọc value cấu hình theo mã, KHÔNG phụ thuộc nhóm.
 * Ưu tiên nhóm 'operation_config', không thấy thì quét toàn bộ nhóm.
 * Trả undefined khi không có cấu hình hoặc value rỗng.
 */
export const readOpConfigValue = (code: string): any => {
	const baseConfig = useBaseConfig();

	const hasValue = (v: any) => v !== undefined && v !== null && v !== '';

	const primary = baseConfig.getOpConfigItemByCode(BaseOpConfigGroupCodeEnum.OperationConfig, code)?.value;
	if (hasValue(primary)) return primary;

	const all = baseConfig.getOpConfigList() || {};
	for (const groupKey of Object.keys(all)) {
		const found = (all[groupKey] || []).find((item: any) => item?.code === code);
		if (found && hasValue(found.value)) return found.value;
	}
	return undefined;
};

/**
 * Đọc cấu hình dạng số nguyên. Trả fallback khi chưa cấu hình / không parse được.
 * positiveOnly = true: giá trị <= 0 cũng coi là không hợp lệ (dùng cho phạm vi, độ dài...).
 */
export const readOpConfigInt = (code: string, fallback: number, positiveOnly = true): number => {
	const parsed = parseInt(readOpConfigValue(code));
	if (isNaN(parsed)) return fallback;
	if (positiveOnly && parsed <= 0) return fallback;
	return parsed;
};

/**
 * Đọc cấu hình dạng số nguyên có thể null (vị trí đầu/cuối tuyến: 0 là giá trị hợp lệ).
 */
export const readOpConfigIntOrNull = (code: string): number | null => {
	const parsed = parseInt(readOpConfigValue(code));
	return isNaN(parsed) ? null : parsed;
};
