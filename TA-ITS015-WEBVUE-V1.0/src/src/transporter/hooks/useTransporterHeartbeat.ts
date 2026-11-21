import { onMounted, onUnmounted } from 'vue';
import mittBus from '/@/utils/mitt';
import { TransporterEvent } from '/@/transporter/constants/transporterEvent';

/**
 * Một nhịp heartbeat nhận được từ NATS cho một phân hệ dịch vụ.
 */
export interface HeartbeatBeat {
	/** Subject thực của gói, vd "ta.its.heartbeat.toll". */
	subject: string;
	/** Payload gốc (có thể là { data: "ok" } hoặc envelope WResult của FMS). */
	data: any;
	/** Thời điểm FE nhận gói (epoch ms). */
	receivedAt: number;
}

const pick = (obj: any, ...keys: string[]): any => {
	if (!obj) return undefined;
	for (const key of keys) {
		if (obj[key] !== undefined && obj[key] !== null) return obj[key];
	}
	return undefined;
};

/**
 * Rút trạng thái "ok" từ payload heartbeat, chấp nhận nhiều dạng gói:
 *  - Toll (dịch vụ tầng Services): { data: "ok" }
 *  - FMS (DeviceMonitor): WResult<HeartbeatResponse> => Result[].Data.Status = "Ok"
 * Trả về chuỗi trạng thái nếu tìm được, ngược lại undefined (vẫn coi là còn sống vì có gói đến).
 */
export const extractHeartbeatStatus = (data: any): string | undefined => {
	if (data == null) return undefined;
	if (typeof data === 'string') return data;

	// Dạng { data: "ok" }
	const direct = pick(data, 'data', 'Data');
	if (typeof direct === 'string') return direct;

	// Dạng envelope FMS: Result[].Data.Status
	const result = pick(data, 'Result', 'result');
	const records = Array.isArray(result) ? result : result ? [result] : [];
	for (const record of records) {
		const inner = pick(record, 'Data', 'data');
		const status = pick(inner, 'Status', 'status');
		if (typeof status === 'string') return status;
	}
	return undefined;
};

/**
 * Lắng nghe mọi heartbeat dịch vụ realtime qua NATS transporter.
 * Hook tự đăng ký/gỡ mittBus listener theo lifecycle của component.
 */
export function useTransporterHeartbeat(handler: (beat: HeartbeatBeat) => void) {
	const listener = (payload: any) => {
		if (!payload || typeof payload.subject !== 'string') return;
		// Log chặng "mittBus → hook": thấy dòng này mà thẻ không xanh thì lỗi ở dashboard (subject không khớp cấu hình).
		console.log(`[Heartbeat] hook → ${payload.subject}`, extractHeartbeatStatus(payload.data) ?? payload.data);
		handler({
			subject: payload.subject,
			data: payload.data,
			receivedAt: typeof payload.receivedAt === 'number' ? payload.receivedAt : Date.now(),
		});
	};

	onMounted(() => {
		mittBus.on(TransporterEvent.Heartbeat as any, listener);
	});

	onUnmounted(() => {
		mittBus.off(TransporterEvent.Heartbeat as any, listener);
	});
}
