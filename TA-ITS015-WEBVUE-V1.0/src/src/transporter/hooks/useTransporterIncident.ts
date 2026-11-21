import { onMounted, onUnmounted } from 'vue';
import mittBus from '/@/utils/mitt';
import { TransporterEvent } from '/@/transporter/constants/transporterEvent';

/**
 * Description: Lắng nghe sự kiện sự cố (incident) realtime qua mittBus (NATS transporter).
 *              Thay thế cách watch store incidentMonitor.broadcastCode/broadcastDateTime cũ.
 *              Tự động đăng ký khi mounted và hủy đăng ký khi unmounted.
 * @param handler Hàm xử lý khi nhận broadcast incident. Tham số `data` là payload
 *                từ NATS (broadcastCode, broadcastObject, broadcastMessage, broadcastDateTime).
 * @param options.immediate Gọi handler ngay khi mounted (tương đương watch { immediate: true }).
 */
export function useTransporterIncident(handler: (data?: any) => void, options: { immediate?: boolean } = {}) {
    const listener = (raw: any) => {
        let data = raw;
        for (let i = 0; i < 4; i++) {
            if (typeof data === 'string') { try { data = JSON.parse(data); } catch { break; } }
            const nested = data?.data ?? data?.Data;
            if (nested == null) break;
            data = nested;
        }
        handler(data);
    };
    const reconnect = (status: { type?: string }) => { if (status?.type === 'reconnect') handler(); };

    onMounted(() => {
        mittBus.on(TransporterEvent.DataIncident as any, listener);
        mittBus.on('event-nats-status' as any, reconnect);
        if (options.immediate) handler();
    });

    onUnmounted(() => {
        mittBus.off(TransporterEvent.DataIncident as any, listener);
        mittBus.off('event-nats-status' as any, reconnect);
    });
}
