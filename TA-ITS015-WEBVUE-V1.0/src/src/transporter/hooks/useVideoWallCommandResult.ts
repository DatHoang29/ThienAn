import { onMounted, onUnmounted } from 'vue';
import mittBus from '/@/utils/mitt';
import { TransporterEvent } from '/@/transporter/constants/transporterEvent';

/**
 * Description: Kết quả lệnh VideoWall do service ITS.VideoWall phát lên NATS `ta.its.data.videowall`
 *              (VwCommandResponseEnvelope). Service serialize PascalCase ⇒ đọc bất chấp hoa/thường.
 */
export interface VideoWallCommandResult<T = any> {
	messageId: string;
	action: string;
	success: boolean;
	errorMessage: string;
	data: T;
}

const pick = (obj: any, ...keys: string[]): any => {
	if (!obj) return undefined;
	for (const k of keys) if (obj[k] !== undefined && obj[k] !== null) return obj[k];
	return undefined;
};

/** Bọc data con sang camelCase (đệ quy) để trang dùng thống nhất. */
const camel = (value: any): any => {
	if (Array.isArray(value)) return value.map(camel);
	if (!value || typeof value !== 'object') return value;
	return Object.fromEntries(Object.entries(value).map(([k, v]) => [k.charAt(0).toLowerCase() + k.slice(1), camel(v)]));
};

/** Thời gian giữ kết quả trong bộ đệm (kết quả có thể về TRƯỚC khi API trả MessageId). */
const BUFFER_MS = 120_000;

/**
 * Description: Chờ kết quả lệnh VideoWall theo MessageId (BE trả về khi nhận lệnh — VwDeviceSetupAcceptedOutput).
 *              Lắng nghe suốt thời gian component sống và đệm kết quả gần đây ⇒ không lỡ kết quả về sớm.
 *              Quá `timeoutMs` ⇒ reject (service tắt / NATS pubsub không giữ lệnh khi service offline).
 *
 * @example
 * const { waitFor } = useVideoWallCommandResult();
 * const res = await api.apply(...);                       // { accepted, messageId }
 * const result = await waitFor(res.data.result.messageId); // VideoWallCommandResult
 */
export function useVideoWallCommandResult() {
	const buffer = new Map<string, { result: VideoWallCommandResult; at: number }>();
	const waiters = new Map<string, (result: VideoWallCommandResult) => void>();

	const listener = (raw: any) => {
		const messageId = String(pick(raw, 'MessageId', 'messageId') ?? '');
		if (!messageId) return; // gói trạng thái (heartbeat) — không phải phản hồi lệnh
		const result: VideoWallCommandResult = {
			messageId,
			action: String(pick(raw, 'Action', 'action') ?? ''),
			success: Boolean(pick(raw, 'Success', 'success') ?? false),
			errorMessage: String(pick(raw, 'ErrorMessage', 'errorMessage') ?? ''),
			data: camel(pick(raw, 'Data', 'data')),
		};
		const waiter = waiters.get(messageId);
		if (waiter) {
			waiters.delete(messageId);
			waiter(result);
			return;
		}
		const now = Date.now();
		for (const [k, v] of buffer) if (now - v.at > BUFFER_MS) buffer.delete(k);
		buffer.set(messageId, { result, at: now });
	};

	onMounted(() => mittBus.on(TransporterEvent.DataVideoWall as any, listener));
	onUnmounted(() => {
		mittBus.off(TransporterEvent.DataVideoWall as any, listener);
		waiters.clear();
	});

	const waitFor = <T = any>(messageId: string, timeoutMs = 30_000): Promise<VideoWallCommandResult<T>> =>
		new Promise((resolve, reject) => {
			const buffered = buffer.get(messageId);
			if (buffered) {
				buffer.delete(messageId);
				return resolve(buffered.result);
			}
			const timer = setTimeout(() => {
				waiters.delete(messageId);
				reject(new Error('timeout'));
			}, timeoutMs);
			waiters.set(messageId, (result) => {
				clearTimeout(timer);
				resolve(result);
			});
		});

	return { waitFor };
}
