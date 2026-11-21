import { defineStore } from 'pinia';
import { fetchSseApi, type SseConnection } from '/@/utils/fetch-utils';

/**
 * Author: Kiro
 * Description: Nguồn giờ server DÙNG CHUNG cho toàn ứng dụng.
 *
 *   Chỉ lưu ĐỘ LỆCH `serverOffsetMs = serverTime - clientNow`, KHÔNG lưu một mốc giờ tự tăng.
 *   Nhờ vậy mọi nơi tính "giờ server hiện tại" bằng `Date.now() + offset` — luôn theo nhịp đồng
 *   hồ máy trạm giữa hai snapshot SSE, không bị trôi và không cần đồng hồ riêng.
 *
 *   Kết nối SSE `/api/system/sysconfig/servertime` được chia sẻ giữa các tab (SharedWorker) và tự
 *   kết nối lại. Nhiều component cùng gọi `connect()` chỉ mở một luồng; `disconnect()` đếm tham
 *   chiếu nên tab/màn hình nào đóng cũng không làm gãy nơi còn lại.
 *
 *   Vì sao là store chứ không lấy biến trong serverTime.vue: component đó chỉ mount khi có topbar
 *   (màn hình big screen full-screen không có), và biến trong đó là ref cục bộ chỉ để hiển thị.
 */
export const useServerTime = defineStore('serverTime', {
	state: () => ({
		/** Độ lệch (ms): serverTime - clientNow. 0 khi chưa có dữ liệu (coi như giờ máy trạm). */
		serverOffsetMs: 0,
		/** Lệch múi giờ server (phút) do BE trả về; phục vụ hiển thị theo giờ server. */
		serverTimeZoneMinutes: 0,
		/** Đã nhận ít nhất một mốc giờ server hợp lệ chưa. */
		synced: false,
	}),
	actions: {
		/**
		 * Giờ server hiện tại (ms epoch), suy ra từ đồng hồ máy trạm + offset.
		 *
		 * PHẢI là action (method), KHÔNG dùng getter: getter Pinia được Vue cache như `computed`,
		 * chỉ tính lại khi dependency reactive đổi. `Date.now()` không reactive nên getter sẽ trả
		 * mãi giá trị cũ ⇒ đồng hồ đứng. Method thì chạy lại mỗi lần gọi.
		 */
		serverNowMs(): number {
			return Date.now() + this.serverOffsetMs;
		},
		/** Giờ server hiện tại dạng Date (ms epoch UTC). Dùng để so hạn hiển thị. */
		serverNow(): Date {
			return new Date(Date.now() + this.serverOffsetMs);
		},
		/**
		 * Cập nhật offset từ một mốc giờ server. Bỏ qua giá trị không hợp lệ.
		 *
		 * Chống GIẬT đồng hồ: giữa các snapshot (server gửi ~5s/lần) client vẫn tự đếm mượt bằng
		 * `Date.now() + offset`. Mỗi snapshot mới lại tính offset tức thời, nhưng jitter mạng khiến
		 * offset lệch vài trăm ms mỗi lần ⇒ đồng hồ nhảy tới/lui. Vì vậy chỉ ghi đè offset khi lệch
		 * ĐÁNG KỂ (mặc định > 2s, tức thực sự sai giờ / mất đồng bộ); lệch nhỏ do độ trễ thì giữ
		 * offset cũ để đồng hồ chạy liền mạch — đúng như hành vi "đếm +1s" của bản cũ.
		 */
		applyServerTime(serverTime: unknown, serverTimeZone?: unknown) {
			const parsed = new Date(serverTime as any).getTime();
			if (Number.isNaN(parsed)) return;
			const nextOffset = parsed - Date.now();
			if (!this.synced || Math.abs(nextOffset - this.serverOffsetMs) > 2000) {
				this.serverOffsetMs = nextOffset;
			}
			if (typeof serverTimeZone === 'number' && Number.isFinite(serverTimeZone)) {
				this.serverTimeZoneMinutes = serverTimeZone;
			}
			this.synced = true;
		},
		/**
		 * Mở kết nối SSE giờ server (đếm tham chiếu). Trả hàm dừng tương ứng — gọi khi component
		 * unmount. An toàn khi gọi nhiều lần: chỉ luồng đầu tiên thực sự kết nối.
		 */
		connect(): () => void {
			refCount += 1;
			if (!connection) {
				connection = fetchSseApi<{ serverTime?: string; serverTimeZone?: number }>('/api/system/sysconfig/servertime', {
					shareAcrossTabs: true,
					reconnect: true,
					idleTimeout: 60 * 1000,
					customCatch: true,
					onResult: (data) => this.applyServerTime(data?.serverTime, data?.serverTimeZone),
					onResultError: (error) => console.warn('[serverTime] parse error', error),
				});
			}
			let stopped = false;
			return () => {
				if (stopped) return;
				stopped = true;
				this.disconnect();
			};
		},
		/** Giảm tham chiếu; đóng luồng khi không còn nơi dùng. */
		disconnect() {
			refCount = Math.max(0, refCount - 1);
			if (refCount === 0 && connection) {
				connection.close();
				connection = undefined;
			}
		},
	},
});

// Kết nối và bộ đếm tham chiếu để ngoài state Pinia — không cần reactive và không serialize.
let connection: SseConnection | undefined;
let refCount = 0;
