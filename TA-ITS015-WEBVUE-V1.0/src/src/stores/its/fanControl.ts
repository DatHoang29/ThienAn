/**
 * Description: Điều khiển hệ thống quạt (quạt phản lực / Jet fan) trên bản đồ giám sát.
 *   - Nhận diện thiết bị quạt theo mã modal của loại thiết bị ('fan').
 *   - Giữ trạng thái chạy (thuận / nghịch / dừng), tần số đặt và tần số đo (Hz) của từng quạt.
 *   - Hover: đọc số liệu (`readTelemetry`) — quạt chưa có dữ liệu thì sinh ngẫu nhiên,
 *     đang chạy thì tần số dao động nhẹ quanh giá trị đặt cho giống số đo thật.
 *
 *   HIỆN ĐANG GIẢ LẬP: chưa có API/biến tần thật. Khi có, thay `dispatchFanCommand` (gửi lệnh) và
 *   `readTelemetry` (đọc số đo) bằng lời gọi API; modal + bản đồ giữ nguyên.
 * Author: ChuongLP
 * Created date: 10/11/2026
 */
import { defineStore } from 'pinia';
import { getEquipmentModalCode, simulateDelay } from '/@/stores/its/deviceControlUtils';
import { TmsEquipmentModalCode } from '/@/types/enums';

/** forward = chạy thuận, reverse = chạy nghịch (đảo chiều gió), stop = dừng. */
export type FanMode = 'forward' | 'reverse' | 'stop';
export type FanCommand = 'forward' | 'reverse' | 'stop';

export interface FanState {
    mode: FanMode;
    /** Tần số đặt cho biến tần (Hz). */
    setpoint: number;
    /** Tần số đo được gần nhất (Hz) — 0 khi dừng. */
    frequency: number;
}

export interface FanCommandDefinition {
    command: FanCommand;
    variant: 'success' | 'warning' | 'danger';
    labelKey: string;
}

export const FAN_COMMANDS: FanCommandDefinition[] = [
    { command: 'forward', variant: 'success', labelKey: 'lz.button.fan.forward' },
    { command: 'reverse', variant: 'warning', labelKey: 'lz.button.fan.reverse' },
    { command: 'stop', variant: 'danger', labelKey: 'lz.button.fan.stop' },
];

/** Dải tần số biến tần (Hz) và mức mặc định khi khởi chạy. */
export const FAN_FREQUENCY_MIN = 0;
export const FAN_FREQUENCY_MAX = 50;
export const FAN_FREQUENCY_STEP = 0.5;
export const FAN_FREQUENCY_DEFAULT = 50;

const SIMULATE_DELAY_MS = 800;
/** Biên độ dao động tần số đo quanh giá trị đặt khi đang chạy (Hz). */
const FREQUENCY_JITTER_HZ = 0.4;

/** Quạt = loại thiết bị được gán mã modal 'fan' (cấu hình tms_equipment_modal_type). */
export const isFanDevice = (deviceId: string) => getEquipmentModalCode(deviceId) === TmsEquipmentModalCode.Fan;

const round1 = (value: number) => Math.round(value * 10) / 10;
const clampFrequency = (value: number) => Math.min(FAN_FREQUENCY_MAX, Math.max(FAN_FREQUENCY_MIN, Number(value) || 0));

/** Tần số đo khi đang chạy: dao động nhẹ quanh giá trị đặt, không vượt dải biến tần. */
const measureFrequency = (state: FanState) =>
    state.mode === 'stop' ? 0 : round1(clampFrequency(state.setpoint + (Math.random() * 2 - 1) * FREQUENCY_JITTER_HZ));

/** Trạng thái ngẫu nhiên cho quạt chưa có dữ liệu: đa số đang chạy thuận, tần số 30–50 Hz. */
const createRandomState = (): FanState => {
    const roll = Math.random();
    const mode: FanMode = roll < 0.6 ? 'forward' : roll < 0.8 ? 'reverse' : 'stop';
    const setpoint = Math.round((30 + Math.random() * 20) / FAN_FREQUENCY_STEP) * FAN_FREQUENCY_STEP;
    const state: FanState = { mode, setpoint, frequency: 0 };
    state.frequency = measureFrequency(state);
    return state;
};

/**
 * Gửi lệnh xuống biến tần. HIỆN GIẢ LẬP: chờ rồi coi như thành công.
 * TODO: thay bằng API điều khiển quạt khi BE/service hỗ trợ.
 */
const dispatchFanCommand = async (_fanId: string, _payload: { mode?: FanMode; setpoint?: number }): Promise<void> => {
    await simulateDelay(SIMULATE_DELAY_MS);
};

export const useFanControlStore = defineStore('fanControl', {
    state: () => ({
        fans: {} as Record<string, FanState>,
        /** Đang giả lập (chưa kết nối thiết bị thật). */
        simulate: true,
    }),
    actions: {
        /** Chỉ đọc (không sinh dữ liệu) — dùng trong render/computed để không ghi state lúc render. */
        getState(fanId?: string | null): FanState | undefined {
            return fanId ? this.fans[String(fanId)] : undefined;
        },

        /**
         * Đọc số liệu mới nhất (gọi khi hover / mở modal): chưa có thì sinh ngẫu nhiên,
         * có rồi thì cập nhật tần số đo (dao động quanh giá trị đặt).
         */
        readTelemetry(fanId: string): FanState {
            const id = String(fanId);
            const current = this.fans[id];
            if (!current) {
                this.fans[id] = createRandomState();
            } else {
                current.frequency = measureFrequency(current);
            }
            return { ...this.fans[id] };
        },

        /** Chạy thuận / chạy nghịch / dừng. Chạy từ trạng thái dừng mà chưa đặt tần số thì dùng mức mặc định. */
        async sendCommand(fanId: string, command: FanCommand) {
            const id = String(fanId);
            const current = this.fans[id] ?? createRandomState();
            const setpoint = current.setpoint > 0 ? current.setpoint : FAN_FREQUENCY_DEFAULT;

            await dispatchFanCommand(id, { mode: command, setpoint });

            const next: FanState = { mode: command, setpoint, frequency: 0 };
            next.frequency = measureFrequency(next);
            this.fans[id] = next;
        },

        /** Đặt tần số biến tần (Hz); quạt đang dừng chỉ lưu giá trị đặt, chạy lại sẽ dùng. */
        async setFrequency(fanId: string, hz: number) {
            const id = String(fanId);
            const setpoint = clampFrequency(hz);
            const current = this.fans[id] ?? createRandomState();

            await dispatchFanCommand(id, { setpoint });

            const next: FanState = { ...current, setpoint };
            next.frequency = measureFrequency(next);
            this.fans[id] = next;
        },
    },
});
