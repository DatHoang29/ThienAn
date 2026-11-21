import { onMounted, onUnmounted } from 'vue';
import mittBus from '/@/utils/mitt';
import { TransporterEvent } from '/@/transporter/constants/transporterEvent';

/**
 * Description: Hook dùng CHUNG cho VMS và LCS. Lắng nghe sự kiện cập nhật nội dung
 *              (hình ảnh / lịch phát) realtime qua mittBus (NATS transporter, subject
 *              'ta.its.data.vms' -> TransporterEvent.DataVms). Nhờ đó UI đổi hình ngay
 *              khi có lịch phát mới mà KHÔNG cần load lại trang.
 *
 * Luồng: BE ItsDataTransporter.BroadcastData(VmsUpdateImage, { ActionType, Data })
 *        -> transporterNats.handleVmsData -> mittBus.emit(TransporterEvent.DataVms)
 *        -> hook này gọi handler(payload đã chuẩn hoá).
 *
 * Author: (auto) - 20260710
 */

// Chuẩn hoá payload nhận từ BE. BE gửi { ActionType, Data } (PascalCase),
// nhưng để an toàn ta chấp nhận cả camelCase.
export interface VmsBroadcastPayload {
    actionType: string; // ví dụ: 'vms-success'
    data: any; // 1 bản ghi VmsCurrent (equipmentId, url, rowData, priority, brightness, ...)
    raw: any; // payload gốc, phòng khi cần đọc field khác
}

/**
 * Description: Đọc field bất chấp PascalCase/camelCase (BE có thể serialize theo 1 trong 2).
 */
const pick = (obj: any, ...keys: string[]): any => {
    if (!obj) return undefined;
    for (const k of keys) {
        if (obj[k] !== undefined && obj[k] !== null) return obj[k];
    }
    return undefined;
};

/**
 * Description: Chuẩn hoá payload thô từ NATS về dạng { actionType, data, raw }.
 */
export const normalizeVmsPayload = (raw: any): VmsBroadcastPayload => {
    const actionType = pick(raw, 'ActionType', 'actionType') ?? '';
    const data = pick(raw, 'Data', 'data') ?? raw ?? null;
    return { actionType, data, raw };
};

/**
 * Description: Lấy equipmentId từ 1 bản ghi VmsCurrent (chịu được PascalCase/camelCase).
 */
export const getVmsEquipmentId = (data: any): string | undefined => {
    const id = pick(data, 'equipmentId', 'EquipmentId', 'equipmentID', 'EquipmentID');
    return id != null ? String(id) : undefined;
};

/**
 * Description: Chuẩn hoá 1 bản ghi VmsCurrent về camelCase (viết thường chữ đầu) cho TẤT CẢ
 *              trường top-level. Vì BE có thể gửi PascalCase (Url, EquipmentId...) trong khi
 *              FE (markerIcons, preview) đọc camelCase (url, equipmentId...). Nếu không chuẩn
 *              hoá, gộp vào vmsCurrentList sẽ khiến bản đồ đọc .url = undefined -> ảnh sai.
 */
export const normalizeVmsRecord = (data: any): any => {
    if (!data || typeof data !== 'object') return data;
    const out: Record<string, any> = {};
    for (const key of Object.keys(data)) {
        const camel = key.charAt(0).toLowerCase() + key.slice(1);
        // Ưu tiên giữ giá trị đã có ở dạng camelCase nếu trùng
        if (out[camel] === undefined || out[camel] === null) {
            out[camel] = data[key];
        }
    }
    return out;
};

/**
 * Description: Upsert 1 payload VMS/LCS vào mảng vmsCurrentList theo equipmentId.
 *              Dùng được cả trong Pinia store (không phụ thuộc lifecycle của component).
 *              Trả về MẢNG MỚI để đảm bảo Vue reactivity re-render marker/preview.
 * @param list Mảng vmsCurrentList hiện tại.
 * @param payload Payload đã chuẩn hoá (hoặc raw — sẽ tự normalize).
 */
export const applyVmsCurrentToList = (list: any[], payload: VmsBroadcastPayload | any): any[] => {
    const p: VmsBroadcastPayload = payload?.raw !== undefined ? payload : normalizeVmsPayload(payload);
    const data = p.data;
    const equipmentId = getVmsEquipmentId(data);
    if (!equipmentId) return list;

    const next = Array.isArray(list) ? [...list] : [];
    // Chuẩn hoá toàn bộ record về camelCase để bản đồ/preview đọc đúng (url, equipmentId...)
    const record = { ...normalizeVmsRecord(data), equipmentId };

    const idx = next.findIndex((x: any) => getVmsEquipmentId(x) === equipmentId);
    if (idx !== -1) {
        next[idx] = { ...next[idx], ...record };
    } else {
        next.push(record);
    }
    return next;
};

export interface UseTransporterVmsOptions {
    /** Chỉ nhận payload có equipmentId nằm trong danh sách này (lọc theo thiết bị đang mở). */
    equipmentIds?: () => Array<string | number> | undefined;
    /** Gọi handler ngay khi mounted (giống watch { immediate: true }). */
    immediate?: boolean;
}

/**
 * Description: Đăng ký lắng nghe realtime cập nhật nội dung VMS/LCS.
 *              Tự động on() khi mounted và off() khi unmounted.
 * @param handler Hàm xử lý khi có cập nhật. Nhận payload đã chuẩn hoá { actionType, data, raw }.
 * @param options equipmentIds: hàm trả về danh sách id cần quan tâm (lọc); immediate: gọi ngay.
 *
 * @example
 * // Trong component (ví dụ vmsInfo.vue / lcsInfo.vue)
 * useTransporterVms((payload) => {
 *     const id = getVmsEquipmentId(payload.data);
 *     if (id === state.itemId) state.vmsCurrent = payload.data;
 * });
 */
export function useTransporterVms(handler: (payload: VmsBroadcastPayload) => void, options: UseTransporterVmsOptions = {}) {
    const listener = (raw: any) => {
        const payload = normalizeVmsPayload(raw);

        // Lọc theo thiết bị quan tâm (nếu có)
        const filterIds = options.equipmentIds?.();
        if (filterIds && filterIds.length) {
            const id = getVmsEquipmentId(payload.data);
            const allow = filterIds.some((x) => String(x) === id);
            if (!allow) return;
        }

        handler(payload);
    };

    onMounted(() => {
        mittBus.on(TransporterEvent.DataVms as any, listener);
        if (options.immediate) handler(normalizeVmsPayload(null));
    });

    onUnmounted(() => {
        mittBus.off(TransporterEvent.DataVms as any, listener);
    });
}
