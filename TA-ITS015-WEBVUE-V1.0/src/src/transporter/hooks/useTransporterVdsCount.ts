import { useVdsSse } from './useVdsSse';
import { onMounted, onUnmounted } from 'vue';
import mittBus from '/@/utils/mitt';
import { TransporterEvent } from '/@/transporter/constants/transporterEvent';

/**
 * Dữ liệu nhận từ subject `ta.its.data.vds.traffic`.
 *
 * Payload VDS có thể thay đổi giữa PascalCase/camelCase và có thể được bọc
 * trong `Data`, `Result` hoặc `BroadcastObject`, vì vậy hook chỉ chuẩn hoá
 * phần envelope; các field nghiệp vụ khác vẫn được giữ nguyên.
 */
export interface VdsCountData {
    zoneId?: string;
    equipmentId?: string;
    [key: string]: unknown;
}

export type VdsCountPayload = VdsCountData | VdsCountData[];

export interface UseTransporterVdsCountOptions {
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
export const normalizeVdsCountData = (payload: any): VdsCountPayload => {
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

    if (Array.isArray(current)) return current as VdsCountData[];
    if (current && typeof current === 'object') return current as VdsCountData;

    return { value: current };
};

const getZoneId = (data: VdsCountPayload): string | undefined => {
    const records = Array.isArray(data) ? data : [data];
    const zoneId = records
        .map((record) => pick(record, 'ZoneId', 'zoneId', 'ZoneID', 'zoneID', 'Zone', 'zone'))
        .find((value) => value !== undefined && value !== null && String(value).trim() !== '');

    return zoneId == null ? undefined : String(zoneId);
};

const resolveZoneId = (
    zoneId: UseTransporterVdsCountOptions['zoneId']
): string | undefined => {
    const value = typeof zoneId === 'function' ? zoneId() : zoneId;
    return value == null || String(value).trim() === '' ? undefined : String(value);
};

/**
 * SSE count/statistic kích hoạt tải lại biểu đồ; NATS chỉ còn dữ liệu traffic.
 * Hook tự đăng ký và huỷ listener theo lifecycle của component.
 */
export function useTransporterVdsCount(
    handler: (data: VdsCountPayload) => void | Promise<void>,
    options: UseTransporterVdsCountOptions = {}
) {
    const listener = (raw: any) => {
        const data = normalizeVdsCountData(raw);
        const configuredZoneId = resolveZoneId(options.zoneId);
        const records = Array.isArray(data) ? data : [data];
        if (configuredZoneId && !records.some(record => {
            const incoming = getZoneId(record);
            return !incoming || incoming.toLowerCase() === configuredZoneId.toLowerCase();
        })) return;

        handler(data);
    };

    const reconnect = (status: { type?: string }) => {
        if (status.type === 'reconnect') handler({});
    };
    // Count gửi snapshot định kỳ kể cả khi không có xe mới. Một nhịp này tải lại
    // cả số đếm/lưu lượng/tốc độ qua API hiện có; không cần thêm socket statistic.
    // Giữ chỗ cho REST trên HTTP/1.1 (bigscreen còn SSE giờ, zone, danh sách và icon).
    useVdsSse(['count'], () => handler({}));
    const events = [TransporterEvent.VdsTrafficData];
    onMounted(() => {
        events.forEach(event => mittBus.on(event as any, listener));
        mittBus.on('event-nats-status' as any, reconnect);
        if (options.immediate) handler({});
    });

    onUnmounted(() => {
        events.forEach(event => mittBus.off(event as any, listener));
        mittBus.off('event-nats-status' as any, reconnect);
    });
}
