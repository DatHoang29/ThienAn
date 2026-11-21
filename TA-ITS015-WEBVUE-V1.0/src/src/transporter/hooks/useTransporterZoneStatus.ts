import { onMounted, onUnmounted } from 'vue';
import mittBus from '/@/utils/mitt';
import { TransporterEvent } from '/@/transporter/constants/transporterEvent';

/**
 * Một thay đổi điều kiện giao thông của zone đã được chuẩn hoá từ payload NATS.
 */
export interface ZoneStatusUpdate {
    zoneId: string;
    condition: string;
    updateTime: string;
}

const pick = (obj: any, ...keys: string[]): any => {
    if (!obj) return undefined;
    for (const key of keys) {
        if (obj[key] !== undefined && obj[key] !== null) return obj[key];
    }
    return undefined;
};

/**
 * Chuẩn hoá payload `ta.its.data.tms.zone` thành danh sách thay đổi condition.
 * Chấp nhận envelope broadcaster (`Data`/`data`) và trường PascalCase/camelCase.
 */
export const normalizeZoneStatusUpdates = (payload: any): ZoneStatusUpdate[] => {
    const envelope = pick(payload, 'Data', 'data', 'BroadcastObject', 'broadcastObject') ?? payload;
    const result = pick(envelope, 'Result', 'result', 'Items', 'items') ?? envelope;
    const records = Array.isArray(result) ? result : [result];

    return records.reduce<ZoneStatusUpdate[]>((updates, record) => {
        const zoneStatus = pick(record, 'ZoneStatus', 'zoneStatus') ?? record;
        const zoneId = pick(zoneStatus, 'ZoneId', 'zoneId', 'Id', 'id');
        const condition = pick(zoneStatus, 'Condition', 'condition', 'ZoneStatusCondition', 'zoneStatusCondition');
        const updateTime = pick(zoneStatus, 'UpdateTime', 'updateTime');

        if (zoneId == null || condition == null) return updates;

        updates.push({
            zoneId: String(zoneId),
            condition: String(condition),
            // Không dùng giờ nhận để làm mới bản tin thiếu thời gian nguồn.
            updateTime: updateTime == null ? '' : String(updateTime),
        });
        return updates;
    }, []);
};

/**
 * Lắng nghe thay đổi condition zone realtime qua `ta.its.data.tms.zone`.
 * Hook tự đăng ký/gỡ mittBus listener theo lifecycle của component.
 */
export function useTransporterZoneStatus(handler: (updates: ZoneStatusUpdate[]) => void, options: { immediate?: boolean } = {}) {
    const listener = (payload: any) => {
        const updates = normalizeZoneStatusUpdates(payload);
        if (updates.length) handler(updates);
    };

    onMounted(() => {
        mittBus.on(TransporterEvent.DataTmsZone as any, listener);
        if (options.immediate) handler([]);
    });

    onUnmounted(() => {
        mittBus.off(TransporterEvent.DataTmsZone as any, listener);
    });
}
