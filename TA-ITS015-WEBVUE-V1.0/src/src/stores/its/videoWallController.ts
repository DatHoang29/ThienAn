
import { defineStore } from 'pinia';
import { i18n } from '/@/i18n';
import { getAPI } from '/@/utils/axios-utils';
import { VwControllerApi } from '/@/api-services/videoWall';
import type { VwControllerOutput } from '/@/api-services/videoWall';

export const VW_NO_COLOR = 'var(--el-border-color)';

/** Màu mặc định cho bộ điều khiển mới. */
export const VW_DEFAULT_CTRL_COLOR = '#3F51B5';

/** Vai trò bộ điều khiển (VwController.role, SysConfigData videowall_controller_role). */
export const VW_ROLE = { Distribution: 'distribution', Display: 'display', Standalone: 'standalone' } as const;

/**
 * 10 màu được phép chọn cho bộ điều khiển (chỉ chọn trong danh sách này):
 * không dùng màu hệ thống primary / success / warning / danger / info, tránh đỏ / xanh lá / xám / vàng / cam —
 * trùng màu trạng thái lỗi / hoạt động / mất kết nối / cảnh báo (viền ô màn hình ăn theo màu bộ điều khiển).
 */
export const VW_CTRL_COLOR_PRESETS = ['#3F51B5', '#1A237E', '#0277BD', '#0097A7', '#00838F', '#5E35B1', '#8E44AD', '#D81B60', '#AD1457', '#8D6E63'];

/** Màu CSS của bộ điều khiển (ColorHex = mã màu chọn trong VW_CTRL_COLOR_PRESETS, VD "#3F51B5"). */
export function vwColorVar(color?: string | null, fallback = VW_NO_COLOR): string {
	return color?.trim() || fallback;
}

/** Màu nền nhạt (CSS) pha từ màu bộ điều khiển. */
export function vwColorBgVar(color?: string | null, fallback = VW_NO_COLOR): string {
	const value = color?.trim();
	return value ? `color-mix(in srgb, ${value} 20%, var(--el-bg-color))` : fallback;
}

/** Thuộc tính el-tag tô theo màu bộ điều khiển. */
export function vwColorTag(color?: string | null): { color?: string; style?: string } {
	const value = color?.trim();
	return value ? { color: value, style: 'border-color: transparent; color: #fff' } : {};
}

/** Mã bộ điều khiển để hiển thị: đúng mã đã lưu (mặc định theo mã thiết bị tham chiếu), mọi nơi trong Video Wall như nhau. */
export function vwCtrlCode(code?: string | null): string {
	return code?.trim() ?? '';
}

/** Cổng ra để hiển thị: "OUT 3" — đúng nhãn in trên mặt sau bộ điều khiển (OUT 1, OUT 2…) để đối chiếu khi đấu dây. */
export function vwPortLabel(port?: number | null): string {
	return port ? `OUT ${port}` : '';
}

/** Số bản ghi tối đa nạp một lần qua `/page`. */
const PAGE_ALL = 500;

// Giữ ngoài state: Promise không cần reactive.
let pending: Promise<void> | null = null;
/** Số thứ tự lượt nạp — bỏ kết quả của lượt cũ về muộn hơn lượt mới. */
let requestSeq = 0;

export const useVideoWallController = defineStore('videoWallController', {
	state: () => ({
		/** Toàn bộ bộ điều khiển, sắp theo mã. */
		controllers: [] as VwControllerOutput[],
		loaded: false,
		loading: false,
	}),

	getters: {
		/** Tra nhanh id → bộ điều khiển. */
		byId(state): Map<string, VwControllerOutput> {
			const map = new Map<string, VwControllerOutput>();
			for (const c of state.controllers) if (c.id) map.set(c.id, c);
			return map;
		},
	},

	actions: {
		/**
		 * Nạp danh sách bộ điều khiển.
		 * - Mặc định dùng lại dữ liệu đã nạp; nhiều trang gọi cùng lúc chỉ phát 1 request.
		 * - `force = true`: nạp lại (gọi sau khi thêm / sửa / xoá bộ điều khiển).
		 */
		async load(force = false): Promise<VwControllerOutput[]> {
			if (!force) {
				if (this.loaded) return this.controllers;
				if (pending) {
					await pending;
					return this.controllers;
				}
			}

			const seq = ++requestSeq;
			this.loading = true;
			const request = getAPI(VwControllerApi)
				.apiVideowallVwcontrollerPagePost({ page: 1, pageSize: PAGE_ALL, field: 'code', order: 'asc' })
				.then((res) => {
					if (seq !== requestSeq) return;
					this.controllers = res.data.result?.records ?? [];
					this.loaded = true;
				})
				.finally(() => {
					if (seq !== requestSeq) return;
					this.loading = false;
					pending = null;
				});
			pending = request;
			await request;
			return this.controllers;
		},

		/** Bộ điều khiển theo id; `undefined` nếu rỗng hoặc bản ghi không còn. */
		find(id?: string | null): VwControllerOutput | undefined {
			return id ? this.byId.get(id) : undefined;
		},

		/**
		 * Mã bộ điều khiển để hiển thị (xem vwCtrlCode).
		 * - `id` rỗng → `emptyText` (VD nguồn "Dùng chung", kịch bản "Toàn tường").
		 * - Chưa nạp xong danh sách → `emptyText` (tránh nháy nhãn sai lúc đang tải).
		 * - Đã nạp mà không tra được (bộ điều khiển đã bị xoá) → "(Bộ điều khiển đã xoá)": vẫn báo dữ liệu sai nhưng không lộ GUID thô.
		 */
		label(id?: string | null, emptyText = ''): string {
			if (!id) return emptyText;
			const ctrl = this.find(id);
			if (ctrl) return vwCtrlCode(ctrl.code) || ctrl.name || '';
			return this.loaded ? i18n.global.t('lz.label.videoWall.controllerMissing') : emptyText;
		},

		/** "C2 · OUT 3" — bộ điều khiển + cổng ra của 1 màn; thiếu cổng thì chỉ còn bộ điều khiển. */
		portText(id?: string | null, port?: number | null): string {
			const ctrl = this.label(id);
			const out = vwPortLabel(port);
			return ctrl && out ? `${ctrl} · ${out}` : ctrl || out;
		},

		/** Màu đậm (CSS) của bộ điều khiển theo id — tô viền ô màn hình / cổng. */
		colorOf(id?: string | null): string {
			return vwColorVar(this.find(id)?.colorHex);
		},

		/** Màu nền nhạt (CSS) của bộ điều khiển theo id. */
		bgColorOf(id?: string | null): string {
			return vwColorBgVar(this.find(id)?.colorHex);
		},
	},
});
