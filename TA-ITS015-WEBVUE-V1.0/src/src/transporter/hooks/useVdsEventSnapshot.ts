/** Nhận snapshot sự kiện VDS qua SSE (SharedWorker), ngừng khi rời màn hình; snapshot đầu tiên không phát cảnh báo. */
import { ref, onMounted, onActivated, onDeactivated, onUnmounted, watch } from 'vue';
import { fetchSseApi, buildFetchHeaders } from '/@/utils/fetch-utils';
import { VdsEventState, vdsEventPrincipal } from '/@/utils/vds-event-state';
import { VdsEventApiAxiosParamCreator } from '/@/api-services/vds';
import type { EventSnapshotItemOutput, EventSnapshotOutput } from '/@/api-services/vds';

/** Bộ lọc của api/vds/vdsEvent/Stream (cùng tham số với apiVdsVdseventStreamGet). */
export type VdsEventQuery = { zoneId?: string; equipmentId?: string; direction?: string };

const REFRESH_SECONDS = 3;

export function useVdsEventSnapshot(query: () => VdsEventQuery | null = () => ({}), notify?: (items: EventSnapshotItemOutput[]) => void) {
	const items = ref<EventSnapshotItemOutput[]>([]);
	let state = new VdsEventState(),
		abort: AbortController | undefined;
	let stopWatch: (() => void) | undefined, timer: ReturnType<typeof setInterval> | undefined;
	let active = false,
		auth = '',
		principal = '',
		previousQuery = '';
	const identity = () => JSON.stringify(buildFetchHeaders());
	const warn = (current: AbortController, error: unknown) => {
		if (!current.signal.aborted) console.warn('[VDS event SSE]', error);
	};
	const connect = (encoded: string) => {
		abort?.abort();
		items.value = [];
		if (encoded === 'null') {
			state = new VdsEventState();
			return;
		}
		const current = (abort = new AbortController());
		const filter: VdsEventQuery = JSON.parse(encoded);
		void VdsEventApiAxiosParamCreator()
			.apiVdsVdseventStreamGet(REFRESH_SECONDS, filter.zoneId, filter.equipmentId, filter.direction)
			.then((request) => {
				if (current.signal.aborted) return;
				return fetchSseApi<EventSnapshotOutput>(request.url, {
					event: 'vdsevent',
					shareAcrossTabs: true,
					signal: current.signal,
					reconnect: true,
					idleTimeout: 60000,
					retryServerErrors: true,
					customCatch: true,
					onResult: (data) => {
						if (current.signal.aborted || !Array.isArray(data?.items)) return;
						const fresh = state.accept(data, performance.now());
						items.value = [...state.items];
						if (fresh.length) notify?.(fresh);
					},
					onError: (error) => warn(current, error),
				}).done;
			})
			.catch((error) => warn(current, error));
	};
	const start = () => {
		if (active) return;
		active = true;
		auth = identity();
		principal = vdsEventPrincipal(buildFetchHeaders());
		// Mỗi lần mở lại màn dùng baseline mới, không thông báo tồn đọng.
		state = new VdsEventState();
		stopWatch = watch(
			() => JSON.stringify(query()),
			(encoded) => {
				if (encoded !== previousQuery) state = new VdsEventState();
				previousQuery = encoded;
				connect(encoded);
			},
			{ immediate: true }
		);
		timer = setInterval(() => {
			if (auth !== identity()) {
				auth = identity();
				const nextPrincipal = vdsEventPrincipal(buildFetchHeaders());
				if (principal !== nextPrincipal) state = new VdsEventState();
				principal = nextPrincipal;
				connect(previousQuery);
			}
			const remaining = state.expire(performance.now());
			if (remaining.length !== items.value.length) items.value = [...remaining];
		}, 250);
	};
	const stop = () => {
		active = false;
		stopWatch?.();
		abort?.abort();
		clearInterval(timer);
		items.value = [];
	};
	onMounted(start);
	onActivated(start);
	onDeactivated(stop);
	onUnmounted(stop);
	return { items };
}
