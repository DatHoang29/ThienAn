import { onMounted, onUnmounted, ref } from 'vue';
import type { AxiosResponse } from 'axios';
import { ElMessage, ElNotification } from 'element-plus';
import mittBus from '/@/utils/mitt';

/**
 * Description: Composable dùng chung cho các nút "lấy dữ liệu thủ công" có phản hồi kết quả qua NATS.
 * Áp dụng cho mọi luồng: FE gọi REST (BE trả RequestId) -> BE publish Type="request" -> dịch vụ xử lý
 * -> dịch vụ publish Type="result" kèm RequestId -> FE (subscribe trực tiếp) đối chiếu và hiển thị.
 *
 * Dùng chung 1 kênh 2 chiều nên chỉ xử lý gói Type="result" và đúng RequestId của mình.
 * Có đệm kết quả về sớm (race-safe) + đồng hồ an toàn chống treo loading.
 *
 * Toll dùng hook riêng (useManualFetchToll) cho tương thích cũ; module mới nên dùng hook chung này.
 * Author: ChuongLP
 * Created date: 14/09/2026
 */

interface ManualFetchResult {
	type: string;
	requestId: string;
	success: boolean;
	message: string;
}

/** Các chuỗi thông báo (đã dịch sẵn ở view) hiển thị theo từng trạng thái. */
export interface ManualFetchMessages {
	sent: string;
	successTitle: string;
	failedTitle: string;
	timeout: string;
}

/** Đọc field bất chấp PascalCase/camelCase. */
const pick = (obj: any, ...keys: string[]): any => {
	if (!obj) return undefined;
	for (const k of keys) {
		if (obj[k] !== undefined && obj[k] !== null) return obj[k];
	}
	return undefined;
};

const normalizeResult = (raw: any): ManualFetchResult => ({
	type: String(pick(raw, 'Type', 'type') ?? ''),
	requestId: String(pick(raw, 'RequestId', 'requestId') ?? ''),
	success: Boolean(pick(raw, 'Success', 'success') ?? false),
	message: String(pick(raw, 'Message', 'message') ?? ''),
});

interface Options {
	/** mittBus event tương ứng subject kết quả (vd TransporterEvent.FmsControl). */
	onSuccess?: () => void;
	timeoutMs?: number;
	messages: ManualFetchMessages;
	/** Có hiển thị nội dung chi tiết (message) trong popup không. Mặc định false = chỉ hiện tiêu đề. */
	showDetail?: boolean;
}

export function useManualFetchNats(event: string, options: Options) {
	const isFetching = ref(false);
	const msg = options.messages;

	let pendingRequestId: string | null = null;
	let timer: ReturnType<typeof setTimeout> | null = null;
	const recentResults = new Map<string, { result: ManualFetchResult; at: number }>();

	const clearPending = () => {
		pendingRequestId = null;
		isFetching.value = false;
		if (timer) {
			clearTimeout(timer);
			timer = null;
		}
	};

	const armTimeout = () => {
		if (timer) clearTimeout(timer);
		const timeoutMs = options.timeoutMs ?? 120000;
		timer = setTimeout(() => {
			if (isFetching.value) {
				clearPending();
				ElMessage.warning(msg.timeout);
			}
		}, timeoutMs);
	};

	const settle = (result: ManualFetchResult) => {
		clearPending();
		if (result.success) {
			ElNotification.success({
				title: msg.successTitle,
				message: options.showDetail ? result.message || msg.successTitle : '',
				duration: 4000,
			});
			options.onSuccess?.();
		} else {
			ElNotification.error({
				title: msg.failedTitle,
				message: options.showDetail ? result.message || msg.failedTitle : '',
				duration: 6000,
			});
		}
	};

	const listener = (raw: any) => {
		try {
			const result = normalizeResult(raw);
			if (result.type.toLowerCase() !== 'result') return;
			if (!result.requestId) return;

			if (pendingRequestId && result.requestId === pendingRequestId) {
				settle(result);
				return;
			}

			// Kết quả về TRƯỚC khi trigger kịp gán pendingRequestId -> đệm lại để trigger nhặt.
			recentResults.set(result.requestId, { result, at: Date.now() });
			for (const [key, val] of recentResults) {
				if (Date.now() - val.at > 60000) recentResults.delete(key);
			}
		} catch {
			// Không ném ra ngoài để tránh làm đứt vòng lắng nghe NATS dùng chung.
		}
	};

	onMounted(() => {
		mittBus.on(event as any, listener);
	});

	onUnmounted(() => {
		mittBus.off(event as any, listener);
		if (timer) clearTimeout(timer);
		recentResults.clear();
	});

	const trigger = async (apiCall: () => Promise<AxiosResponse<{ result?: { requestId?: string | null } | null }>>) => {
		if (isFetching.value) return;
		isFetching.value = true;
		pendingRequestId = null;
		armTimeout();
		try {
			const res = await apiCall();
			const requestId = res.data?.result?.requestId;
			if (!requestId) {
				clearPending();
				ElMessage.error(msg.failedTitle);
				return;
			}

			ElMessage.info(msg.sent);

			// Kết quả có thể đã về trước dòng này -> nhặt từ đệm.
			const buffered = recentResults.get(requestId);
			if (buffered) {
				recentResults.delete(requestId);
				settle(buffered.result);
				return;
			}

			pendingRequestId = requestId;
		} catch {
			clearPending();
		}
	};

	return { isFetching, trigger };
}
