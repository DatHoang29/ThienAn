import { onMounted, onUnmounted } from 'vue';
import mittBus from '/@/utils/mitt';
import { TransporterEvent } from '/@/transporter/constants/transporterEvent';

/**
 * Một thay đổi trạng thái thiết bị đã được chuẩn hoá từ payload NATS FMS.
 */
export interface EquipmentStatusUpdate {
    equipmentId: string;
    equipmentState: unknown;
}

const pick = (obj: any, ...keys: string[]): any => {
    if (!obj) return undefined;
    for (const key of keys) {
        if (obj[key] !== undefined && obj[key] !== null) return obj[key];
    }
    return undefined;
};

/**
 * Chuẩn hoá payload `ta.its.status.fms` thành các thay đổi trạng thái thiết bị.
 * Hỗ trợ payload FMS hiện tại (`Result[].Info.ReferenceId`, `Result[].Data.Status`)
 * và cả envelope PascalCase/camelCase của broadcaster.
 */
export const normalizeEquipmentStatusUpdates = (payload: any): EquipmentStatusUpdate[] => {
    const envelope = pick(payload, 'Data', 'data', 'BroadcastObject', 'broadcastObject') ?? payload;
    const result = pick(envelope, 'Result', 'result') ?? envelope;
    const records = Array.isArray(result) ? result : [result];

    return records.reduce<EquipmentStatusUpdate[]>((updates, record) => {
        const info = pick(record, 'Info', 'info') ?? record;
        const data = pick(record, 'Data', 'data') ?? record;
        const equipmentId = pick(info, 'ReferenceId', 'referenceId', 'EquipmentId', 'equipmentId');
        const equipmentState = pick(data, 'Status', 'status', 'EquipmentState', 'equipmentState');

        if (equipmentId == null || equipmentState == null) return updates;

        updates.push({
            equipmentId: String(equipmentId),
            equipmentState,
        });
        return updates;
    }, []);
};

/**
 * Lắng nghe trạng thái thiết bị FMS realtime qua NATS transporter.
 * Hook tự đăng ký/gỡ mittBus listener theo lifecycle của component.
 */
export function useTransporterStatusFms(handler: (updates: EquipmentStatusUpdate[]) => void, options: { immediate?: boolean } = {}) {
    const listener = (payload: any) => {
        const updates = normalizeEquipmentStatusUpdates(payload);
        if (updates.length) handler(updates);
    };

    onMounted(() => {
        mittBus.on(TransporterEvent.StatusFms as any, listener);
        if (options.immediate) handler([]);
    });

    onUnmounted(() => {
        mittBus.off(TransporterEvent.StatusFms as any, listener);
    });
}
