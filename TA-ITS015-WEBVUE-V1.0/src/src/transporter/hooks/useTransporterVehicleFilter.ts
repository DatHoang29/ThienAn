import { onMounted, onUnmounted } from 'vue';
import mittBus from '/@/utils/mitt';
import { TransporterEvent } from '/@/transporter/constants/transporterEvent';

/**
 * Dữ liệu nhận từ subject `ta.its.data.vds.filter`.
 *
 * Payload VDS có thể thay đổi giữa PascalCase/camelCase và có thể được bọc
 * trong `Data`, `Result` hoặc `BroadcastObject`, vì vậy hook chỉ chuẩn hoá
 * phần envelope; các field nghiệp vụ khác vẫn được giữ nguyên.
 *
 * Lưu ý: BE gửi `filterType` và `alarm` dưới dạng CHUỖI ('0' | '1'), và gửi
 * `detectTime` là giờ local KHÔNG có offset — đừng so sánh bằng số và đừng
 * coi `detectTime` là UTC.
 */
export interface VehicleAlertData {
    historyId?: string;
    filterId?: string;
    plate?: string;
    /** '0' = danh sách cho phép, '1' = danh sách chặn (TmsVehicleFilter.Type). */
    filterType?: string;
    /** '0' = không cảnh báo, '1' = cảnh báo. */
    alarm?: string;
    /**
     * true ⇒ hiện thông báo; false ⇒ chỉ là dòng lịch sử mới (cảnh báo tắt hoặc còn trong cửa chống dội),
     * dùng cho bảng hoạt động gần đây. Bản tin cũ không có trường này ⇒ coi như true.
     */
    notify?: boolean;
    equipmentId?: string;
    equipmentName?: string;
    /** Lý trình camera bắt được xe. */
    km?: number;
    met?: number;
    /** Id vùng của lượt xe. */
    zone?: string;
    /** Giờ local, không offset. */
    detectTime?: string;
    zoneId?: string;
    [key: string]: unknown;
}

export type VehicleAlertPayload = VehicleAlertData | VehicleAlertData[];

export interface UseTransporterVehicleAlertOptions {
    /** Gọi handler ngay khi component mounted. */
    immediate?: boolean;
    /** Chỉ nhận bản tin của zone này; bản tin không có zoneId vẫn được nhận. */
    zoneId?: string | number | (() => string | number | null | undefined);
}

const pick = (value: any, ...keys: string[]): any => {
    if (!value || typeof value !== 'object') return undefined;

    for (const key of keys) {
        if (value[key] !== undefined && value[key] !== null) return value[key];
    }

    return undefined;
};

const parseJson = (value: any): any => {
    if (typeof value !== 'string') return value;

    try {
        return JSON.parse(value);
    } catch {
        return value;
    }
};

/** Bỏ các lớp envelope thường gặp của broadcaster/VDS. */
export const normalizeVehicleAlertData = (payload: any): VehicleAlertPayload => {
    let current = parseJson(payload);

    // Cho phép tối đa vài lớp bọc để không làm mất payload nếu BE đổi envelope.
    for (let i = 0; i < 4; i++) {
        const nested = pick(current, 'Data', 'data', 'BroadcastObject', 'broadcastObject');
        if (nested !== undefined && nested !== current) {
            current = parseJson(nested);
            continue;
        }

        const result = pick(current, 'Result', 'result', 'Items', 'items');
        if (result !== undefined && result !== current) {
            current = parseJson(result);
            continue;
        }

        break;
    }

    if (Array.isArray(current)) return current as VehicleAlertData[];
    if (current && typeof current === 'object') return current as VehicleAlertData;

    return { value: current };
};

const getZoneId = (data: VehicleAlertPayload): string | undefined => {
    const records = Array.isArray(data) ? data : [data];
    const zoneId = records
        .map((record) => pick(record, 'ZoneId', 'zoneId', 'ZoneID', 'zoneID', 'Zone', 'zone'))
        .find((value) => value !== undefined && value !== null && String(value).trim() !== '');

    return zoneId == null ? undefined : String(zoneId);
};

const resolveZoneId = (
    zoneId: UseTransporterVehicleAlertOptions['zoneId']
): string | undefined => {
    const value = typeof zoneId === 'function' ? zoneId() : zoneId;
    return value == null || String(value).trim() === '' ? undefined : String(value);
};

/**
 * Lắng nghe cảnh báo phương tiện lọc (`ta.its.data.vds.filter`) qua mittBus/NATS.
 * Hook tự đăng ký và huỷ listener theo lifecycle của component.
 */
export function useTransporterVehicleAlert(
    handler: (data: VehicleAlertPayload) => void,
    options: UseTransporterVehicleAlertOptions = {}
) {
    const listener = (raw: any) => {
        const data = normalizeVehicleAlertData(raw);
        const configuredZoneId = resolveZoneId(options.zoneId);
        const incomingZoneId = getZoneId(data);

        if (
            configuredZoneId &&
            incomingZoneId &&
            configuredZoneId !== incomingZoneId
        ) {
            return;
        }

        handler(data);
    };

    onMounted(() => {
        mittBus.on(TransporterEvent.VehicleAlertData as any, listener);
        if (options.immediate) handler({});
    });

    onUnmounted(() => {
        mittBus.off(TransporterEvent.VehicleAlertData as any, listener);
    });
}
