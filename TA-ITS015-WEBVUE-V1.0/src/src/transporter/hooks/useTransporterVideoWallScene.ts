import { onMounted, onUnmounted } from 'vue';
import mittBus from '/@/utils/mitt';
import { TransporterEvent } from '/@/transporter/constants/transporterEvent';

/**
 * Description: Payload đã chuẩn hoá của sự kiện kích hoạt kịch bản VideoWall.
 */
export interface VideoWallScenePayload {
	/** Id kịch bản — có thể rỗng nếu BE chỉ gửi `Code`. */
	sceneId: string;
	/** Mã kịch bản — khoá chính mà BE dùng trong `VwActiveSceneInput`. */
	code: string;
	/** Tên kịch bản (nếu BE gửi kèm). */
	name: string;
	/** Loại sự kiện ITS đã kích hoạt kịch bản (nếu có). */
	eventTypeId: string;
	/** Thời điểm kích hoạt dạng ISO (nếu BE gửi kèm). */
	triggeredAt?: string;
	/** Payload gốc, phòng khi cần đọc field khác. */
	raw: any;
}

/** Đọc field bất chấp PascalCase/camelCase. */
const pick = (obj: any, ...keys: string[]): any => {
	if (!obj) return undefined;
	for (const k of keys) {
		if (obj[k] !== undefined && obj[k] !== null && obj[k] !== '') return obj[k];
	}
	return undefined;
};

const asString = (value: unknown): string => (value === undefined || value === null ? '' : String(value));

const asIso = (value: unknown): string | undefined => {
	if (!value) return undefined;
	const d = value instanceof Date ? value : new Date(String(value));
	return Number.isNaN(d.getTime()) ? undefined : d.toISOString();
};

/**
 * Description: Tìm giá trị của một trong các khoá `keys`, dò theo chiều sâu tối đa `maxDepth` lớp.
 *
 * VÌ SAO PHẢI DÒ SÂU: BE bọc dữ liệu trong một lớp envelope và tên lớp này không cố định —
 * đã gặp `{ ActiveValue: { SceneId } }`, subject VMS thì dùng `{ ActionType, Data }`, và có
 * thể còn `ActiveScene`/`Payload`. Bóc theo danh sách tên vỏ biết trước thì mỗi lần BE đổi vỏ
 * là FE đứng im không đổi kịch bản; dò theo khoá cần tìm thì không phụ thuộc tên vỏ nữa.
 */
const deepPick = (source: any, keys: string[], maxDepth = 4): any => {
	if (!source || typeof source !== 'object') return undefined;
	const direct = pick(source, ...keys);
	if (direct !== undefined) return direct;
	if (maxDepth <= 0) return undefined;
	for (const value of Object.values(source)) {
		if (value && typeof value === 'object') {
			const found = deepPick(value, keys, maxDepth - 1);
			if (found !== undefined) return found;
		}
	}
	return undefined;
};

/**
 * Description: Chuẩn hoá payload thô từ NATS thành {@link VideoWallScenePayload}.
 *              Gọi hàm này trong handler của {@link useTransporterVideoWallScene}.
 *
 * Chỉ cần payload có `SceneId` (hoặc `Code`) ở BẤT KỲ lớp nào là dùng được — xem
 * {@link deepPick}. `Id` chỉ được đọc khi không có `SceneId`, để tránh nhặt nhầm id của
 * lớp vỏ (VD bản ghi log trigger cũng có `Id`).
 *
 * Đã kiểm chứng với payload thực tế của BE:
 *   { "ActiveValue": { "SceneId": "bbbbbbbb-0000-4000-8000-000000000002" } }
 *
 * @example
 * const payload = normalizeVideoWallScenePayload(data);
 * if (!payload.sceneId && !payload.code) return;   // không tra được kịch bản
 */
export const normalizeVideoWallScenePayload = (raw: any): VideoWallScenePayload => {
	const sceneId = deepPick(raw, ['SceneId', 'sceneId']) ?? deepPick(raw, ['Id', 'id']);
	return {
		sceneId: asString(sceneId),
		code: asString(deepPick(raw, ['Code', 'code', 'SceneCode', 'sceneCode'])),
		name: asString(deepPick(raw, ['Name', 'name', 'SceneName', 'sceneName'])),
		eventTypeId: asString(deepPick(raw, ['EventTypeId', 'eventTypeId'])),
		triggeredAt: asIso(deepPick(raw, ['TriggeredAt', 'triggeredAt'])),
		raw,
	};
};

/**
 * Description: Lắng nghe realtime "BE vừa kích hoạt kịch bản VideoWall" qua mittBus (NATS transporter).
 *              Tự động đăng ký khi mounted và hủy đăng ký khi unmounted.
 *
 *              Luồng đầy đủ:
 *                1. Ai đó gọi `POST /api/videowall/vwscene/active` (FE bấm áp dụng, lịch chạy,
 *                   hoặc hệ thống ITS đẩy sự kiện) → BE `ActiveVwScene` → `MessBus.InvokeAsync`.
 *                2. BE publish lên NATS subject `ta.its.data.videowall.scene`.
 *                3. `transporterNats` nhận, emit `TransporterEvent.DataVideoWallScene` lên mittBus.
 *                4. Hook này gọi handler → trang Monitor chuyển sang kịch bản mới.
 *
 * @param handler Hàm xử lý khi có kịch bản mới được kích hoạt. Tham số `data` là payload
 *                THÔ từ NATS; dùng {@link normalizeVideoWallScenePayload} để bóc SceneId/Code.
 * @param options.immediate Gọi handler ngay khi mounted (tương đương watch { immediate: true }).
 *                          Lúc đó `data` là `undefined` nên handler phải tự chịu được.
 */
export function useTransporterVideoWallScene(handler: (data?: any) => void, options: { immediate?: boolean } = {}) {
	const listener = (data: any) => {
		// Log chặng "mittBus → component": thấy dòng này là toàn tuyến NATS đã thông,
		// lỗi (nếu còn) nằm trong handler của trang.
		handler(data);
	};

	onMounted(() => {
		mittBus.on(TransporterEvent.DataVideoWallScene as any, listener);
		if (options.immediate) handler();
	});

	onUnmounted(() => {
		mittBus.off(TransporterEvent.DataVideoWallScene as any, listener);
	});
}
