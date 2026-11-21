/**
 * Description: Hàm dùng chung cho các store điều khiển theo hệ thiết bị (barrier, fan, ...).
 *   Hệ của thiết bị lấy theo mã modal gán cho loại thiết bị (TmsEquipmentType.modalCode).
 * Author: ChuongLP
 * Created date: 10/11/2026
 */
import { useMapStore } from '/@/stores/its/mapStore';
import { isCctvCameraEquipment } from '/@/components/cctv/cctvComponent.service';
import { TmsEquipmentModalCode } from '/@/types/enums';

/** Bỏ dấu tiếng Việt + chữ thường để so khớp tên/mã loại thiết bị ("Quạt phản lực" → "quat phan luc"). */
export const normalizeText = (value: unknown) =>
    String(value ?? '')
        .normalize('NFD')
        .replace(/[̀-ͯ]/g, '')
        .replace(/đ/gi, 'd')
        .toLowerCase();

/** Loại thiết bị của 1 thiết bị (theo mapStore đã nạp sẵn). */
export const getEquipmentTypeOf = (deviceId: string) => {
    const mapStore = useMapStore();
    const equipment = mapStore.equipmentList.find((item: any) => String(item.id) === String(deviceId));
    return mapStore.equipmentTypeList.find((item: any) => String(item?.id) === String(equipment?.equipmentTypeId));
};

/** Chuỗi "mã tên" đã chuẩn hoá của loại thiết bị, rỗng nếu không tìm thấy. */
export const getEquipmentTypeText = (deviceId: string) => {
    const type = getEquipmentTypeOf(deviceId);
    return type ? normalizeText(`${type.code ?? ''} ${type.name ?? ''}`) : '';
};

// #region Mã modal thiết bị
// Author: KienLT
// Created date: 11/11/2026
// Description: Modal mở trên bigscreen lấy theo TmsEquipmentType.modalCode (danh mục cấu hình
//   BaseConfigTypeEnum.TmsEquipmentModalType) thay cho việc đoán theo mã/tên loại ở từng nơi.

const LEGACY_LIGHT_PATTERN = /den tin hieu|signal|traffic[\s_-]*light/;
const LEGACY_BARRIER_PATTERN = /barrier|barie/;
// Đèn tín hiệu giao thông độc lập (SS...): không thuộc hệ barrier nên không được ghép cặp với barrier cùng tầng block.
const LEGACY_TRAFFIC_LIGHT_PATTERN = /giao thong|traffic|\bss\b/;
const LEGACY_FAN_PATTERN = /\bfan\b|jetfan|jet[\s_-]*fan|quat/;

/**
 * DỰ PHÒNG cho loại thiết bị CHƯA gán modalCode (DB chưa chạy script gán sẵn, BE chưa trả cột mới):
 * giữ nguyên luật đoán cũ theo mã/tên loại để hành vi không đổi. Thứ tự ưu tiên như code cũ:
 * camera → đèn tín hiệu → barrier → quạt → VMS/LCS/VDS.
 * TODO: xoá hàm này khi mọi môi trường đã gán modalCode cho loại thiết bị.
 */
const resolveLegacyModalCode = (type: any): string => {
    if (isCctvCameraEquipment({ equipmentTypeId: type?.id, equipmentTypeCode: type?.code })) return TmsEquipmentModalCode.Cctv;
    const text = normalizeText(`${type?.code ?? ''} ${type?.name ?? ''}`);
    if (LEGACY_LIGHT_PATTERN.test(text)) {
        // Đèn giao thông thường (không nhắc tới barrier) → đèn độc lập; còn lại giữ như cũ là đèn đi kèm barrier
        if (!LEGACY_BARRIER_PATTERN.test(text) && LEGACY_TRAFFIC_LIGHT_PATTERN.test(text)) return TmsEquipmentModalCode.TrafficLight;
        return TmsEquipmentModalCode.BarrierLight;
    }
    if (LEGACY_BARRIER_PATTERN.test(text)) return TmsEquipmentModalCode.Barrier;
    if (LEGACY_FAN_PATTERN.test(text)) return TmsEquipmentModalCode.Fan;
    switch (type?.code) {
        case 'Equipment_Type_VMS':
        case 'VMS':
            return TmsEquipmentModalCode.Vms;
        case 'LCS':
            return TmsEquipmentModalCode.Lcs;
        case 'VDS':
            return TmsEquipmentModalCode.Vds;
        default:
            return TmsEquipmentModalCode.Device;
    }
};

/** Mã modal của 1 loại thiết bị (bản ghi TmsEquipmentType); không có loại ⇒ 'device'. */
export const resolveEquipmentModalCode = (type: any): string => {
    if (!type) return TmsEquipmentModalCode.Device;
    const code = String(type.modalCode ?? '').trim().toLowerCase();
    // Loại thiết bị mã TRAFFIC_LIGHT (đèn tín hiệu giao thông SS) luôn là đèn độc lập: script gán sẵn modalCode theo tên
    // từng đặt nhầm 'barrier_light' cho loại này làm đèn SS bị ghép với barrier. Mã khác (vd 'traffic_light', 'device') vẫn được tôn trọng.
    if (String(type.code ?? '').trim().toLowerCase() === 'traffic_light' && (!code || code === TmsEquipmentModalCode.BarrierLight)) {
        return TmsEquipmentModalCode.TrafficLight;
    }
    return code || resolveLegacyModalCode(type);
};

/** Mã modal của 1 thiết bị (theo loại thiết bị trong mapStore đã nạp sẵn). */
export const getEquipmentModalCode = (deviceId: string) => resolveEquipmentModalCode(getEquipmentTypeOf(deviceId));

// #endregion

/** Tên thiết bị theo id ('-' nếu không có). */
export const getEquipmentName = (deviceId?: string | null) => {
    if (!deviceId) return '-';
    return useMapStore().equipmentList.find((item: any) => String(item.id) === String(deviceId))?.name || '-';
};

/** Chờ giả lập thiết bị phản hồi. */
export const simulateDelay = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms));
