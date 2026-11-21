import { h, onMounted, onUnmounted, ref } from 'vue';
import type { AxiosResponse } from 'axios';
import { ElMessage, ElMessageBox, ElNotification } from 'element-plus';
import { useI18n } from 'vue-i18n';
import mittBus from '/@/utils/mitt';
import { TransporterEvent } from '/@/transporter/constants/transporterEvent';
import type { WResultManualFetchTollOutput } from '/@/api-services/toll';
import { useTollCatalog } from '/@/hooks/useTollCatalog';

// ─── Kiểu dữ liệu nhận từ Service ────────────────────────────────────────────

/**
 * Kết quả từng DataType (phản ánh TollControlResultItem phía Service).
 * Service serialize PascalCase → đọc bất chấp hoa/thường qua helper pick().
 */
interface TollControlResultItem {
	dataType: string;
	ok: boolean;
	message: string;
	received: number;
	inserted: number;
	updated: number;
	unchanged: number;
	failed: number;
}

/**
 * Kết quả tổng hợp (Type="result") Service phát về sau khi xử lý xong.
 * items[] chứa chi tiết từng DataType — dùng để hiển thị lỗi cụ thể thay vì chỉ message tổng hợp.
 */
interface TollControlResult {
	type: string;
	requestId: string;
	success: boolean;
	message: string;
	items: TollControlResultItem[];
	received: number;
	inserted: number;
	updated: number;
	unchanged: number;
	failed: number;
}

// ─── Helper đọc field bất chấp PascalCase/camelCase ──────────────────────────
const pick = (obj: any, ...keys: string[]): any => {
	if (!obj) return undefined;
	for (const k of keys) {
		if (obj[k] !== undefined && obj[k] !== null) return obj[k];
	}
	return undefined;
};

const normalizeItem = (raw: any): TollControlResultItem => ({
	dataType: String(pick(raw, 'DataType', 'dataType') ?? ''),
	ok: Boolean(pick(raw, 'Ok', 'ok') ?? false),
	message: String(pick(raw, 'Message', 'message') ?? ''),
	received: Number(pick(raw, 'Received', 'received') ?? 0),
	inserted: Number(pick(raw, 'Inserted', 'inserted') ?? 0),
	updated: Number(pick(raw, 'Updated', 'updated') ?? 0),
	unchanged: Number(pick(raw, 'Unchanged', 'unchanged') ?? 0),
	failed: Number(pick(raw, 'Failed', 'failed') ?? 0),
});

const normalizeResult = (raw: any): TollControlResult => ({
	type: String(pick(raw, 'Type', 'type') ?? ''),
	requestId: String(pick(raw, 'RequestId', 'requestId') ?? ''),
	success: Boolean(pick(raw, 'Success', 'success') ?? false),
	message: String(pick(raw, 'Message', 'message') ?? ''),
	items: ((pick(raw, 'Items', 'items') as any[]) ?? []).map(normalizeItem),
	received: Number(pick(raw, 'Received', 'received') ?? 0),
	inserted: Number(pick(raw, 'Inserted', 'inserted') ?? 0),
	updated: Number(pick(raw, 'Updated', 'updated') ?? 0),
	unchanged: Number(pick(raw, 'Unchanged', 'unchanged') ?? 0),
	failed: Number(pick(raw, 'Failed', 'failed') ?? 0),
});

// ─── Composable ───────────────────────────────────────────────────────────────

/**
 * Luồng đầy đủ:
 *   1. Bấm nút → gọi REST API tương ứng của màn hình (BE trả về RequestId ngay).
 *   2. BE publish lệnh lên NATS ta.its.control.toll (Type="request").
 *   3. Service ITS.Toll.Worker nhận → phát ngay Type="ack" (Worker xác nhận đang online + đã tiếp nhận).
 *   4. Service gọi lại API TCS, lưu DB, rồi publish Type="result" kèm RequestId và Items[].
 *   5. FE nhận ack → huỷ cảnh báo "có thể offline".
 *      FE nhận result → hiển thị ElNotification thành công hoặc lỗi chi tiết theo từng DataType.
 *
 * Xử lý lỗi:
 *   - Timeout gọi API TCS / lỗi HTTP → BE tự gửi result với success=false, items[].message chứa lý do.
 *   - Worker offline → không nhận ack sau ackTimeoutMs → cảnh báo sớm; vẫn chờ tới timeoutMs.
 *   - Quá timeoutMs vẫn chưa có result → warning timeout.
 *
 * Chiến lược 2 giai đoạn:
 *   - Giai đoạn 1 (chưa có ack): chờ tối đa timeoutMs (mặc định 15s).
 *     Nếu hết giờ → Worker offline hoặc mạng NATS có vấn đề.
 *   - Giai đoạn 2 (đã nhận ack): Worker đang xử lý → tự động kéo dài thêm extendedTimeoutMs (mặc định 40s).
 *     Đủ để BE chờ TCS phản hồi (TimeoutSeconds 30s từ SysOpConfig) + overhead.
 *     Nếu hết giờ này → Worker xử lý bất thường (treo, crash sau ack).
 *
 * @param options.onSuccess         Hàm gọi khi lấy dữ liệu thành công (thường là nạp lại lưới).
 * @param options.timeoutMs         Chờ tối đa trước khi có ack (mặc định 15 000 ms = 15s).
 * @param options.extendedTimeoutMs Kéo dài thêm sau khi nhận ack (mặc định 40 000 ms = 40s).
 * @param options.ackTimeoutMs      Cảnh báo offline nếu chưa nhận ack (mặc định 8 000 ms = 8s).
 */
export function useManualFetchToll(options: { onSuccess?: () => void; timeoutMs?: number; extendedTimeoutMs?: number; ackTimeoutMs?: number } = {}) {
	const { t: $t } = useI18n();
	const { clearTollCatalogCache } = useTollCatalog();
	const isFetching = ref(false);

	let pendingRequestId: string | null = null;
	let resultTimer: ReturnType<typeof setTimeout> | null = null;
	let ackTimer: ReturnType<typeof setTimeout> | null = null;

	// ── Quản lý timer ─────────────────────────────────────────────────────────

	const clearAckTimer = () => {
		if (ackTimer) { clearTimeout(ackTimer); ackTimer = null; }
	};

	const clearPending = () => {
		pendingRequestId = null;
		isFetching.value = false;
		if (resultTimer) { clearTimeout(resultTimer); resultTimer = null; }
		clearAckTimer();
	};

	/**
	 * Bật đồng hồ an toàn tổng thể NGAY khi bắt đầu (trước cả lời gọi REST API), bao trọn cả:
	 *   - lời gọi REST bị treo (BE/mạng không phản hồi), và
	 *   - trường hợp Service xử lý lâu hoặc không bao giờ phát kết quả về.
	 * Hết giờ mà vẫn đang chờ → bỏ trạng thái loading để FE không bị kẹt.
	 */
	const armResultTimeout = () => {
		if (resultTimer) clearTimeout(resultTimer);
		const ms = options.timeoutMs ?? 15_000;
		resultTimer = setTimeout(() => {
			if (isFetching.value) {
				clearPending();
				ElMessage.warning($t('lz.message.toll.fetchTimeout'));
			}
		}, ms);
	};

	/**
	 * Kéo dài thời gian chờ result sau khi nhận được ack.
	 * Worker đã xác nhận đang xử lý → reset timer để đủ thời gian chờ BE gọi TCS xong
	 * (TCS timeout 30s từ SysOpConfig + overhead 10s = 40s).
	 */
	const extendResultTimeout = () => {
		if (resultTimer) clearTimeout(resultTimer);
		const ms = options.extendedTimeoutMs ?? 40_000;
		resultTimer = setTimeout(() => {
			if (isFetching.value) {
				clearPending();
				ElMessage.warning($t('lz.message.toll.fetchTimeout'));
			}
		}, ms);
	};

	/**
	 * Bật đồng hồ chờ ACK sau khi đã có RequestId. Nếu Worker không phát ack trong thời gian này
	 * → hiển thị cảnh báo "có thể offline" nhưng KHÔNG huỷ chờ result.
	 * Ack đến trước khi hết giờ → huỷ timer này và kéo dài timer result sang giai đoạn 2.
	 */
	const armAckTimeout = () => {
		clearAckTimer();
		const ms = options.ackTimeoutMs ?? 8_000;
		ackTimer = setTimeout(() => {
			ackTimer = null;
			if (isFetching.value) {
				// Cảnh báo nhưng không clearPending — vẫn tiếp tục chờ result.
				ElMessage.warning($t('lz.message.toll.fetchAckTimeout'));
			}
		}, ms);
	};

	// ── Đệm kết quả đến sớm ───────────────────────────────────────────────────
	// Service có thể xử lý và phát result trước khi trigger() kịp gán pendingRequestId.
	// Không có đệm → kết quả đến sớm bị bỏ qua → FE chờ tới timeout dù Service đã thành công.
	const recentResults = new Map<string, { result: TollControlResult; at: number }>();

	/**
	 * Chuẩn hóa thông báo lỗi từ Service sang thông báo ngắn gọn, dễ hiểu (đã dịch i18n)
	 * và lược bỏ toàn bộ thông tin nhạy cảm như IP, Port, URL kỹ thuật.
	 */
	const formatFriendlyError = (rawMessage: string): string => {
		if (!rawMessage) return $t('lz.message.toll.fetchFailedMessage');

		const lower = rawMessage.toLowerCase();

		// Lỗi mất kết nối / từ chối kết nối tới máy chủ đích (TCS)
		if (
			lower.includes('actively refused') ||
			lower.includes('no connection could be made') ||
			lower.includes('connection refused') ||
			lower.includes('failed to respond') ||
			lower.includes('unable to connect') ||
			lower.includes('network is unreachable') ||
			lower.includes('no such host') ||
			lower.includes('name or service not known') ||
			lower.includes('error occurred while sending the request') ||
			lower.includes('socket')
		) {
			return $t('lz.message.toll.fetchConnectionError');
		}

		// Lỗi quá thời gian chờ phản hồi (timeout)
		if (lower.includes('timeout') || lower.includes('timed out') || lower.includes('canceled')) {
			return $t('lz.message.toll.fetchTimeout');
		}

		// Lược bỏ thông tin IP:Port, ngoặc chứa IP:Port và URL kỹ thuật
		const cleaned = rawMessage
			.replace(/\(\s*(?:https?:\/\/)?\d{1,3}(?:\.\d{1,3}){3}(?::\d+)?[^)]*\)/gi, '')
			.replace(/https?:\/\/[^\s)]+/gi, '')
			.replace(/\b\d{1,3}(?:\.\d{1,3}){3}(?::\d+)?\b/g, '')
			.replace(/\s{2,}/g, ' ')
			.trim();

		// Nếu chuỗi còn lại vẫn là thông báo lỗi kỹ thuật tiếng Anh phổ biến thì trả về thông báo chuẩn tiếng Việt
		if (!cleaned || /[a-z]{4,}/i.test(cleaned) && !/[àáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđ]/i.test(cleaned)) {
			return $t('lz.message.toll.fetchFailedMessage');
		}

		return cleaned;
	};

	// ── Hiển thị kết quả ──────────────────────────────────────────────────────
	const settle = (result: TollControlResult) => {
		clearPending(); // huỷ cả 2 timer + reset isFetching

		if (result.success) {
			// Dữ liệu danh mục vừa được lấy lại → xoá cache FE để các trang báo cáo / lịch gửi mail nạp lại.
			clearTollCatalogCache();
			ElNotification.success({
				title: $t('lz.message.toll.fetchSuccessTitle'),
				duration: 4000,
			});
			options.onSuccess?.();
		} else {
			const failedItems = result.items.filter((i) => !i.ok);
			const friendlyMessages = Array.from(
				new Set(
					failedItems.length > 0
						? failedItems.map((i) => formatFriendlyError(i.message))
						: [formatFriendlyError(result.message)]
				)
			);

			const msgNode =
				friendlyMessages.length > 1
					? h(
							'ul',
							{ style: 'margin:0;padding:0;list-style:none;font-size:12px;line-height:1.8' },
							friendlyMessages.map((msg) => h('li', { style: 'margin-bottom:2px;word-break:break-word' }, `• ${msg}`))
						)
					: friendlyMessages[0] || $t('lz.message.toll.fetchFailedMessage');

			ElNotification.error({
				title: $t('lz.message.toll.fetchFailedTitle'),
				message: msgNode,
				duration: 6_000,
			});
		}
	};

	// ── Listener NATS ─────────────────────────────────────────────────────────
	const listener = (raw: any) => {
		try {
			const type = String(pick(raw, 'Type', 'type') ?? '')
				.trim()
				.toLowerCase();
			const requestId = String(pick(raw, 'RequestId', 'requestId') ?? '');

			if (!type || !requestId) return;

			// Type="ack": Worker xác nhận đã tiếp nhận request → huỷ cảnh báo offline + kéo dài timer result.
			if (type === 'ack') {
				if (pendingRequestId && requestId === pendingRequestId) {
					clearAckTimer();
					// Giai đoạn 2: Worker đang xử lý, reset timer dài hơn để đợi TCS phản hồi.
					extendResultTimeout();
				}
				return; // ack không phải kết quả cuối → dừng tại đây
			}

			// Bỏ qua mọi gói không phải kết quả (vd gói "request" do BE phát trên cùng kênh).
			if (type !== 'result') return;

			const result = normalizeResult(raw);
			if (!result.requestId) return;

			// Khớp yêu cầu đang chờ → xử lý ngay.
			if (pendingRequestId && result.requestId === pendingRequestId) {
				settle(result);
				return;
			}

			// Kết quả về TRƯỚC khi trigger() kịp gán pendingRequestId → đệm lại.
			recentResults.set(result.requestId, { result, at: Date.now() });
			// Dọn các mục quá hạn (> 60s) tránh rò bộ nhớ.
			for (const [key, val] of recentResults) {
				if (Date.now() - val.at > 60_000) recentResults.delete(key);
			}
		} catch {
			// Tuyệt đối KHÔNG ném ra ngoài: tránh làm đứt vòng lắng nghe NATS dùng chung.
		}
	};

	onMounted(() => {
		mittBus.on(TransporterEvent.TollControl as any, listener);
	});

	onUnmounted(() => {
		mittBus.off(TransporterEvent.TollControl as any, listener);
		clearPending();
		recentResults.clear();
	});

	// ── API công khai ─────────────────────────────────────────────────────────

	/**
	 * Gọi API lấy dữ liệu thủ công của màn hình rồi chờ kết quả qua NATS.
	 * @param apiCall Hàm gọi REST trả về WResultManualFetchTollOutput (chứa RequestId).
	 */
	const trigger = async (apiCall: () => Promise<AxiosResponse<WResultManualFetchTollOutput>>) => {
		if (isFetching.value) return;

		// Xác nhận trước khi lấy dữ liệu thủ công.
		try {
			await ElMessageBox.confirm($t('lz.message.toll.fetchConfirm'), $t('lz.message.toll.fetchConfirmTitle'), {
				confirmButtonText: $t('lz.button.base.confirm'),
				cancelButtonText: $t('lz.button.base.cancel'),
				type: 'warning',
			});
		} catch {
			return; // người dùng bấm Hủy
		}

		isFetching.value = true;
		pendingRequestId = null;

		// Bật đồng hồ an toàn NGAY từ đầu — bao trọn cả trường hợp lời gọi REST bị treo.
		armResultTimeout();

		try {
			const res = await apiCall();
			const requestId = res.data?.result?.requestId;

			if (!requestId) {
				// Không lấy được RequestId → không thể đối chiếu kết quả, dừng ngay.
				clearPending();
				ElMessage.error($t('lz.message.toll.fetchFailedTitle'));
				return;
			}

			ElMessage.info($t('lz.message.toll.fetchSent'));

			// Kết quả có thể đã về TRƯỚC dòng này (Service nhanh hơn vòng REST) → nhặt từ đệm.
			const buffered = recentResults.get(requestId);
			if (buffered) {
				recentResults.delete(requestId);
				settle(buffered.result);
				return;
			}

			// Chưa có result → gán pendingRequestId và bật đồng hồ chờ ack.
			pendingRequestId = requestId;
			armAckTimeout();
		} catch {
			// Lỗi gọi REST (axios interceptor thường đã hiển thị) → bỏ loading ngay.
			clearPending();
		}
	};

	return { isFetching, trigger };
}
