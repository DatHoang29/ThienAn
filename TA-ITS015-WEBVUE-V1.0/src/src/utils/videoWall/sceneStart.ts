import { getAPI } from '/@/utils/axios-utils';
import { VwSceneApi, DefaultEnum, StatusEnum } from '/@/api-services/videoWall';
import type { VwSceneOutput } from '/@/api-services/videoWall';

/**
 * Kịch bản đang phát: `GET /vwscene/active`.
 * - KHÔNG truyền tham số (client Swagger sẽ đẩy thành `?input=[object Object]`).
 * - Chưa có kịch bản active thì BE trả lỗi nghiệp vụ ⇒ `customCatch` để không hiện toast, trả `undefined`.
 */
export async function fetchActiveScene(): Promise<VwSceneOutput | undefined> {
	try {
		const res = await getAPI(VwSceneApi).apiVideowallVwsceneActiveGet(undefined, { customCatch: true } as any);
		return res.data.result?.id ? (res.data.result as VwSceneOutput) : undefined;
	} catch {
		return undefined;
	}
}

/**
 * Kịch bản mở sẵn khi vào trang — quy tắc chung của Giám sát & điều khiển và Quản lý kịch bản:
 * kịch bản BE đang phát (có trong danh sách) → kịch bản mặc định đang bật → kịch bản đang bật đầu tiên → kịch bản đầu tiên.
 * `scenes` đã sắp theo orderNo; rỗng ⇒ `undefined`.
 */
export const pickStartScene = (scenes: VwSceneOutput[], active?: VwSceneOutput): VwSceneOutput | undefined =>
	(active?.id ? scenes.find((s) => s.id === active.id) : undefined) ??
	scenes.find((s) => s.isDefault === DefaultEnum.NUMBER_1 && s.status === StatusEnum.NUMBER_1) ??
	scenes.find((s) => s.status === StatusEnum.NUMBER_1) ??
	scenes[0];
