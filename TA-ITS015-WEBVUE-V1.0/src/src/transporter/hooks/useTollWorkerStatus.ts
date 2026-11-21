import { computed, onMounted, onUnmounted, ref } from 'vue';
import { useTransporterHeartbeat } from '/@/transporter/hooks/useTransporterHeartbeat';

/**
 * Theo dõi trạng thái Online/Offline của ITS.Toll.Worker qua heartbeat NATS.
 *
 * Worker phát gói { data: "ok" } lên kênh "ta.its.heartbeat.toll" mỗi HeartbeatIntervalSeconds (mặc định 30s).
 * Ngưỡng offline = 2.5 × interval = 75s.
 *
 * Dùng trong màn hình toll để hiển thị badge trạng thái kế bên nút "Lấy dữ liệu".
 * Author: ChuongLP
 * Created date: 30/09/2026
 */

const TOLL_HEARTBEAT_SUBJECT = 'ta.its.heartbeat.toll';
/** Ngưỡng coi là offline: 2.5 × 30s interval = 75 000 ms. */
const OFFLINE_THRESHOLD_MS = 75_000;
/** Tần suất tự kiểm tra lại (giây) khi không có gói mới đến — cập nhật badge sau khi hết hạn. */
const CHECK_INTERVAL_MS = 10_000;

export type TollWorkerStatus = 'online' | 'offline' | 'unknown';

export function useTollWorkerStatus() {
	const lastSeenAt = ref<number | null>(null);
	let checkTimer: ReturnType<typeof setInterval> | null = null;

	const status = computed<TollWorkerStatus>(() => {
		if (lastSeenAt.value === null) return 'unknown';
		return Date.now() - lastSeenAt.value <= OFFLINE_THRESHOLD_MS ? 'online' : 'offline';
	});

	// El-tag type tương ứng với trạng thái
	const statusTagType = computed(() => {
		if (status.value === 'online') return 'success';
		if (status.value === 'offline') return 'danger';
		return 'info';
	});

	// Lắng nghe heartbeat qua hook dùng chung
	useTransporterHeartbeat((beat) => {
		if (beat.subject !== TOLL_HEARTBEAT_SUBJECT) return;
		lastSeenAt.value = beat.receivedAt;
	});

	// Kiểm tra định kỳ để cập nhật badge khi hết hạn (không cần gói mới đến)
	onMounted(() => {
		checkTimer = setInterval(() => {
			// computed tự tính lại khi đọc, interval chỉ cần "tick" để trigger re-render
			lastSeenAt.value = lastSeenAt.value;
		}, CHECK_INTERVAL_MS);
	});

	onUnmounted(() => {
		if (checkTimer) { clearInterval(checkTimer); checkTimer = null; }
	});

	return { status, statusTagType, lastSeenAt };
}
