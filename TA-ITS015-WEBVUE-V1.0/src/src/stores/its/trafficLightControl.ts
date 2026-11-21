/**
 * Description: Điều khiển đèn tín hiệu giao thông (SS) trên bản đồ giám sát.
 *   - Nhận diện thiết bị theo mã modal 'traffic_light' của loại thiết bị.
 *   - Mỗi đèn điều khiển ĐỘC LẬP (không ghép với barrier hay đèn khác).
 *   - Hiện chỉ hỗ trợ 2 màu: đỏ / xanh.
 *   - Lưu màu đèn hiện tại để bản đồ (icon) và modal hiển thị.
 *
 *   HIỆN ĐANG GIẢ LẬP: WebAPI chưa có endpoint điều khiển đèn. Khi có, chỉ cần thay thân hàm
 *   `dispatchTrafficLightCommand` bằng lời gọi API (BE đẩy lệnh `set-color` xuống service TrafficLight qua NATS
 *   `ta.its.control.trafficlight`; màu Red/Green); phần còn lại (modal, bản đồ) giữ nguyên.
 * Created date: 13/11/2026
 */
import { defineStore } from 'pinia';
import { getEquipmentModalCode, simulateDelay } from '/@/stores/its/deviceControlUtils';
import { TmsEquipmentModalCode } from '/@/types/enums';

export type TrafficLightColor = 'red' | 'green';

export interface TrafficLightCommandDefinition {
    command: TrafficLightColor;
    /** Màu nút: danger = đỏ, success = xanh. */
    variant: 'danger' | 'success';
    labelKey: string;
}

/** Danh sách lệnh — thứ tự = thứ tự nút trên form. */
export const TRAFFIC_LIGHT_COMMANDS: TrafficLightCommandDefinition[] = [
    { command: 'red', variant: 'danger', labelKey: 'lz.button.trafficLight.red' },
    { command: 'green', variant: 'success', labelKey: 'lz.button.trafficLight.green' },
];

/** Thời gian giả lập thiết bị phản hồi (ms). */
const SIMULATE_DELAY_MS = 800;

/** true nếu thiết bị là đèn tín hiệu giao thông SS (theo mã modal của loại thiết bị). */
export const isTrafficLightDevice = (deviceId: string): boolean => getEquipmentModalCode(deviceId) === TmsEquipmentModalCode.TrafficLight;

/**
 * Gửi lệnh xuống thiết bị. HIỆN GIẢ LẬP: chờ SIMULATE_DELAY_MS rồi coi như thành công.
 * TODO: thay bằng API WebAPI khi có endpoint điều khiển đèn tín hiệu.
 */
const dispatchTrafficLightCommand = async (_deviceId: string, _color: TrafficLightColor): Promise<void> => {
    await simulateDelay(SIMULATE_DELAY_MS);
};

export const useTrafficLightControlStore = defineStore('trafficLightControl', {
    state: () => ({
        /** Màu đèn theo id thiết bị; chưa có = chưa xác định. */
        lights: {} as Record<string, TrafficLightColor>,
        /** Đang giả lập (chưa kết nối thiết bị thật). */
        simulate: true,
    }),
    actions: {
        getLightState(deviceId?: string | null): TrafficLightColor | undefined {
            return deviceId ? this.lights[String(deviceId)] : undefined;
        },
        /** Gửi lệnh rồi cập nhật màu (bản đồ + modal đổi theo). Lỗi ném ra cho UI báo. */
        async sendCommand(deviceId: string, color: TrafficLightColor) {
            if (!TRAFFIC_LIGHT_COMMANDS.some((item) => item.command === color)) throw new Error(`Traffic light command not allowed: ${color}`);
            await dispatchTrafficLightCommand(String(deviceId), color);
            this.lights[String(deviceId)] = color;
        },
    },
});
