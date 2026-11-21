import { computed } from 'vue';
import { defineStore } from 'pinia';
import { useRequestOldRoutes } from '/@/stores/requestOldRoutes';
import { findMenuPage } from '/@/utils/menuTabs';
import { getAPI } from '/@/utils/axios-utils';
import { VwWallTopologyApi } from '/@/api-services/videoWall';
import type { VwWallBlockOutput, VwWallTopologyOutput } from '/@/api-services/videoWall';
import { VideoWallEnums } from '/@/types/enums';

/**
 * Bố cục tường Video Wall (API `vwwalltopology/current`): tường gồm 1..n khối xếp ngang theo `blockNo`,
 * mỗi khối có lưới `cols × rows` riêng. Vị trí màn hình (blockNo, gridRow, gridCol) và cửa sổ kịch bản
 * (blockNo, x, y, w, h) đều tính TRONG khối — 1 ô = `panelW × panelH` pixel logic.
 */

/** Nhãn khối A, B, C… theo blockNo (0-based) — khớp BE `VwWallLayoutService.BlockLabel`. */
export const blockLabel = (blockNo?: number | null): string => (blockNo != null && blockNo >= 0 && blockNo < 26 ? String.fromCharCode(65 + blockNo) : String(blockNo ?? ''));

/** Nhãn ô dạng "A · H1 · C2"; tường 1 khối thì bỏ phần khối. */
export const cellLabel = (blockNo: number | null | undefined, row: number | null | undefined, col: number | null | undefined, multiBlock = true): string =>
	`${multiBlock ? `${blockLabel(blockNo ?? 0)} · ` : ''}H${(row ?? 0) + 1} · C${(col ?? 0) + 1}`;

/**
 * Tỉ lệ ô (rộng:cao) cho phép chọn — khớp BE `VwWallProfile.CellRatios` (VwWallTopology.CellRatio, VwScreen.CellRatio).
 * Chỉ quyết định HÌNH DẠNG ô khi vẽ; toạ độ cửa sổ vẫn tính theo `panelW × panelH` pixel logic cho mọi tỉ lệ.
 */
export const CELL_RATIOS = ['16:9', '3:4'] as const;
export const DEFAULT_CELL_RATIO = '16:9';

/** Tách "16:9" ⇒ { rw: 16, rh: 9 }; sai định dạng ⇒ undefined. */
export const cellRatioParts = (ratio?: string | null): { rw: number; rh: number } | undefined => {
	const m = /^\s*(\d+)\s*:\s*(\d+)\s*$/.exec(ratio ?? '');
	return m && +m[1] > 0 && +m[2] > 0 ? { rw: +m[1], rh: +m[2] } : undefined;
};

/** Màn khai báo tỉ lệ khác tỉ lệ ô của bố cục ⇒ cảnh báo trên lưới. Màn chưa khai báo tỉ lệ thì bỏ qua. */
export const isCellRatioMismatch = (screenRatio?: string | null, wallRatio?: string | null) => !!screenRatio && !!wallRatio && screenRatio.trim() !== wallRatio.trim();

/** Component của trang Bố cục tường trong menu BE. */
const WALL_TOPOLOGY_PAGE_COMPONENT = 'videoWall/wallTopology/layoutTab';

/**
 * Trang Bố cục tường trong menu BE (menu riêng hoặc tab của menu nhóm); không được cấp menu ⇒ undefined (ẩn liên kết).
 * Mở bằng `openMenuPage(router, target)`.
 */
export function useWallTopologyPagePath() {
	return computed(() => findMenuPage(useRequestOldRoutes().requestOldRoutes as any[], WALL_TOPOLOGY_PAGE_COMPONENT));
}

/** Bố cục khi BE chưa trả về (lỗi mạng): 1 khối mặc định như BE. */
const DEFAULT_BLOCKS: VwWallBlockOutput[] = [
	{ blockNo: 0, cols: VideoWallEnums.WALL_COLS, rows: VideoWallEnums.WALL_ROWS, screenCount: VideoWallEnums.WALL_COLS * VideoWallEnums.WALL_ROWS, offsetCol: 0 },
];

let pending: Promise<void> | null = null;

export const useVideoWallTopology = defineStore('videoWallTopology', {
	state: () => ({
		topology: null as VwWallTopologyOutput | null,
		loaded: false,
		loading: false,
	}),

	getters: {
		/** Các khối, sắp theo blockNo (trái → phải). */
		blocks(state): VwWallBlockOutput[] {
			const list = state.topology?.blocks ?? [];
			return list.length ? [...list].sort((a, b) => (a.blockNo ?? 0) - (b.blockNo ?? 0)) : DEFAULT_BLOCKS;
		},
		/** Chiều rộng 1 ô (pixel logic). */
		panelW(state): number {
			return state.topology?.screenWidth || VideoWallEnums.PANEL_WIDTH_PX;
		},
		/** Chiều cao 1 ô (pixel logic). */
		panelH(state): number {
			return state.topology?.screenHeight || VideoWallEnums.PANEL_HEIGHT_PX;
		},
		/** Tỉ lệ ô (hình dạng ô khi vẽ), VD "16:9". */
		cellRatio(state): string {
			return state.topology?.cellRatio || DEFAULT_CELL_RATIO;
		},
		totalScreens(): number {
			return this.blocks.reduce((t, b) => t + (b.cols ?? 0) * (b.rows ?? 0), 0);
		},
		isMultiBlock(): boolean {
			return this.blocks.length > 1;
		},
	},

	actions: {
		/**
		 * Nạp bố cục tường.
		 * - Mặc định dùng lại dữ liệu đã nạp; nhiều trang gọi cùng lúc chỉ phát 1 request.
		 * - `force = true`: nạp lại (gọi sau khi lưu bố cục).
		 */
		async load(force = false): Promise<void> {
			if (!force && this.loaded) return;
			if (pending) {
				if (!force) return pending;
				// Request đang chạy có thể phát trước khi lưu bố cục ⇒ đợi xong rồi nạp lại, không dùng kết quả cũ
				await pending.catch(() => {});
				if (pending) return pending;
			}
			this.loading = true;
			pending = getAPI(VwWallTopologyApi)
				.apiVideowallVwwalltopologyCurrentGet()
				.then((res) => {
					this.topology = res.data.result ?? null;
					this.loaded = true;
				})
				.finally(() => {
					this.loading = false;
					pending = null;
				});
			return pending;
		},

		block(blockNo?: number | null): VwWallBlockOutput | undefined {
			return this.blocks.find((b) => b.blockNo === (blockNo ?? 0));
		},

		/** Ô (hàng, cột) có nằm trong lưới của khối không. */
		isCellInside(blockNo: number | null | undefined, row: number | null | undefined, col: number | null | undefined): boolean {
			const b = this.block(blockNo);
			return !!b && row != null && col != null && row >= 0 && col >= 0 && row < (b.rows ?? 0) && col < (b.cols ?? 0);
		},

		/** Kích thước khối theo pixel logic. */
		blockSize(blockNo?: number | null): { w: number; h: number } {
			const b = this.block(blockNo);
			return { w: (b?.cols ?? 0) * this.panelW, h: (b?.rows ?? 0) * this.panelH };
		},
	},
});
