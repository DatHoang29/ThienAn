/**
 * Description: Điều khiển hệ thống Barrier (barrier + đèn tín hiệu barrier) trên bản đồ giám sát.
 *   - Nhận diện thiết bị thuộc hệ Barrier theo mã modal của loại thiết bị ('barrier' / 'barrier_light').
 *   - Ghép cặp barrier ↔ đèn tín hiệu đứng cạnh nhau trong cùng tầng của block.
 *   - Lưu trạng thái barrier (mở/đóng) + đèn (đỏ/xanh) để bản đồ và modal hiển thị.
 *
 *   HIỆN ĐANG GIẢ LẬP: WebAPI chưa có endpoint điều khiển barrier. Khi có, chỉ cần thay thân hàm
 *   `dispatchBarrierCommand` bằng lời gọi API (BE đẩy lệnh xuống service Barrier qua NATS
 *   `ta.its.control.barrier`, action `open` / `close`); phần còn lại (modal, bản đồ) giữ nguyên.
 * Author: ChuongLP
 * Created date: 10/11/2026
 */
import { defineStore } from 'pinia';
import { getEquipmentModalCode, simulateDelay } from '/@/stores/its/deviceControlUtils';
import { TmsEquipmentModalCode } from '/@/types/enums';

export type BarrierArmState = 'open' | 'closed';
export type BarrierLightState = 'red' | 'green';
export type BarrierDeviceKind = 'barrier' | 'light';
export type BarrierCommand = 'closeRed' | 'red' | 'openGreen' | 'green' | 'open' | 'close';

/** Một hệ barrier: barrier + đèn tín hiệu đi kèm (đèn có thể tích hợp trên chính barrier). */
export interface BarrierSystem {
    /** Thiết bị barrier (null = chỉ có đèn, các lệnh mở/đóng bị khoá). */
    barrierId: string | null;
    /** Thiết bị đèn tín hiệu; không có đèn rời thì dùng chính barrierId (đèn tích hợp). */
    lightId: string | null;
}

export interface BarrierCommandDefinition {
    command: BarrierCommand;
    /** Trạng thái barrier sau lệnh (bỏ trống = không đổi barrier). */
    arm?: BarrierArmState;
    /** Trạng thái đèn sau lệnh (bỏ trống = không đổi đèn). */
    light?: BarrierLightState;
    /** Màu nút: danger = đỏ, success = xanh, default = trung tính. */
    variant: 'danger' | 'success' | 'default';
    labelKey: string;
}

/** Danh sách lệnh — thứ tự = thứ tự nút trên form (2 cột: lệnh kèm barrier | lệnh chỉ đèn). */
export const BARRIER_COMMANDS: BarrierCommandDefinition[] = [
    { command: 'closeRed', arm: 'closed', light: 'red', variant: 'danger', labelKey: 'lz.button.barrier.closeRed' },
    { command: 'red', light: 'red', variant: 'danger', labelKey: 'lz.button.barrier.red' },
    { command: 'openGreen', arm: 'open', light: 'green', variant: 'success', labelKey: 'lz.button.barrier.openGreen' },
    { command: 'green', light: 'green', variant: 'success', labelKey: 'lz.button.barrier.green' },
    { command: 'open', arm: 'open', variant: 'default', labelKey: 'lz.button.barrier.open' },
    { command: 'close', arm: 'closed', variant: 'default', labelKey: 'lz.button.barrier.close' },
];

/** Thời gian giả lập thiết bị phản hồi (ms). */
const SIMULATE_DELAY_MS = 800;

// #region Nhận diện thiết bị

/**
 * 'barrier' | 'light' (đèn tín hiệu) | null (không thuộc hệ barrier).
 * Theo mã modal gán cho loại thiết bị (cấu hình tms_equipment_modal_type): 'barrier' / 'barrier_light'.
 */
export const getBarrierDeviceKind = (deviceId: string): BarrierDeviceKind | null => {
    const modalCode = getEquipmentModalCode(deviceId);
    if (modalCode === TmsEquipmentModalCode.BarrierLight) return 'light';
    if (modalCode === TmsEquipmentModalCode.Barrier) return 'barrier';
    return null;
};

/** Thiết bị loại `kind` gần `index` nhất trong danh sách cùng tầng (bằng khoảng cách thì lấy bên trái). */
const findNearestPeer = (peers: string[], index: number, kind: BarrierDeviceKind): string | null => {
    let best: string | null = null;
    let bestDistance = Infinity;
    peers.forEach((peerId, peerIndex) => {
        if (peerIndex === index || getBarrierDeviceKind(peerId) !== kind) return;
        const distance = Math.abs(peerIndex - index);
        if (distance < bestDistance) {
            best = peerId;
            bestDistance = distance;
        }
    });
    return best;
};

/**
 * Xác định hệ barrier của thiết bị được bấm.
 * @param deviceId thiết bị được bấm
 * @param peers danh sách thiết bị cùng tầng block theo thứ tự hiển thị (thiết bị đặt lẻ = không có)
 * @returns null nếu thiết bị không thuộc hệ barrier (mở modal thiết bị thường)
 */
export const resolveBarrierSystem = (deviceId: string, peers?: string[]): BarrierSystem | null => {
    const id = String(deviceId);
    const kind = getBarrierDeviceKind(id);
    if (!kind) return null;

    const list = Array.isArray(peers) ? peers.map(String) : [];
    const index = list.indexOf(id);

    if (kind === 'barrier') {
        // Không có đèn rời cạnh bên → coi như đèn tích hợp trên chính barrier
        const lightId = index >= 0 ? findNearestPeer(list, index, 'light') : null;
        return { barrierId: id, lightId: lightId ?? id };
    }

    const barrierId = index >= 0 ? findNearestPeer(list, index, 'barrier') : null;
    // Đèn tín hiệu không có barrier cạnh bên (vd đèn giao thông đặt lẻ) → không phải hệ barrier, mở modal thiết bị thường
    if (!barrierId) return null;
    return { barrierId, lightId: id };
};

// #endregion

// #region Gửi lệnh

/**
 * Gửi lệnh xuống thiết bị. HIỆN GIẢ LẬP: chờ SIMULATE_DELAY_MS rồi coi như thành công.
 * TODO: thay bằng API WebAPI khi có endpoint điều khiển barrier (lệnh đèn tín hiệu cần bổ sung ở service).
 */
const dispatchBarrierCommand = async (_system: BarrierSystem, _command: BarrierCommand): Promise<void> => {
    await simulateDelay(SIMULATE_DELAY_MS);
};

// #endregion

export const useBarrierControlStore = defineStore('barrierControl', {
    state: () => ({
        /** Trạng thái barrier theo id thiết bị; chưa có = chưa xác định. */
        arms: {} as Record<string, BarrierArmState>,
        /** Trạng thái đèn theo id thiết bị đèn (hoặc id barrier nếu đèn tích hợp). */
        lights: {} as Record<string, BarrierLightState>,
        /** Đang giả lập (chưa kết nối thiết bị thật). */
        simulate: true,
    }),
    actions: {
        getArmState(deviceId?: string | null): BarrierArmState | undefined {
            return deviceId ? this.arms[String(deviceId)] : undefined;
        },
        getLightState(deviceId?: string | null): BarrierLightState | undefined {
            return deviceId ? this.lights[String(deviceId)] : undefined;
        },
        /** Lệnh có thực hiện được với hệ này không (thiếu barrier thì khoá lệnh mở/đóng). */
        canRun(system: BarrierSystem | null, definition: BarrierCommandDefinition): boolean {
            if (!system) return false;
            if (definition.arm && !system.barrierId) return false;
            if (definition.light && !system.lightId) return false;
            return true;
        },
        /** Gửi lệnh rồi cập nhật trạng thái (bản đồ + modal đổi theo). Lỗi ném ra cho UI báo. */
        async sendCommand(system: BarrierSystem, command: BarrierCommand) {
            const definition = BARRIER_COMMANDS.find((item) => item.command === command);
            if (!definition || !this.canRun(system, definition)) throw new Error(`Barrier command not allowed: ${command}`);

            await dispatchBarrierCommand(system, command);

            if (definition.arm && system.barrierId) this.arms[system.barrierId] = definition.arm;
            if (definition.light && system.lightId) this.lights[system.lightId] = definition.light;
        },
    },
});
