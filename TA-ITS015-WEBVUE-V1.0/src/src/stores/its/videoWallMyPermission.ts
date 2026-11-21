import { defineStore } from 'pinia';
import { i18n } from '/@/i18n';
import { getAPI } from '/@/utils/axios-utils';
import { VwWallPermissionApi } from '/@/api-services/videoWall';
import type { VwGridCell } from '/@/api-services/videoWall';
import { useVideoWallTopology } from '/@/stores/its/videoWallTopology';

/**
 * Quyền vùng màn hình Video Wall của người dùng đang đăng nhập (API `GetMy`).
 *
 * - `full` : toàn quyền (SuperAdmin, bản ghi IsFullAccess).
 * - `area` : chỉ thao tác trong các ô được cấp.
 * - `none` : chưa được cấp vùng ⇒ không thao tác được.
 *
 * Đây chỉ là lớp hỗ trợ giao diện (làm mờ ô, khoá cửa sổ); BE (`VwPermissionService`) mới là nơi quyết định.
 * Tính ô giống BE: ô (trong khối) = floor(x / panelW), floor(y / panelH) — kích thước ô lấy từ bố cục tường.
 */
export type VwWallAccessType = 'full' | 'area' | 'none';

let pending: Promise<void> | null = null;

export const useVideoWallMyPermission = defineStore('videoWallMyPermission', {
	state: () => ({
		accessType: 'none' as VwWallAccessType,
		allowedCells: [] as VwGridCell[],
		source: null as string | null,
		/** SuperAdmin: được thao tác cấp cả tường (VD Đặt kịch bản mặc định). */
		isSuperAdmin: false,
		loaded: false,
		loading: false,
	}),

	getters: {
		isFull(state): boolean {
			return state.accessType === 'full';
		},
		isNone(state): boolean {
			return state.accessType === 'none';
		},
		/** Tập ô được cấp, khoá "block,col,row". */
		allowedSet(state): Set<string> {
			return new Set(state.allowedCells.map((c) => `${c.block ?? 0},${c.col},${c.row}`));
		},
	},

	actions: {
		/**
		 * Nạp quyền của tài khoản hiện tại.
		 * - Mặc định dùng lại dữ liệu đã nạp; `force = true` để nạp lại (VD mỗi lần vào trang Kịch bản).
		 * - Lỗi mạng ⇒ coi như `none` (an toàn), BE vẫn là nơi chặn cuối cùng.
		 */
		async load(force = false): Promise<void> {
			if (!force && this.loaded) return;
			if (pending) return pending;
			this.loading = true;
			pending = getAPI(VwWallPermissionApi)
				.apiVideowallVwwallpermissionGetmyGet()
				.then((res) => {
					const r = res.data.result;
					const type = (r?.accessType as VwWallAccessType) || (r?.isFullAccess ? 'full' : r?.allowedCells?.length ? 'area' : 'none');
					this.accessType = type;
					this.allowedCells = r?.allowedCells ?? [];
					this.source = r?.source ?? null;
					this.isSuperAdmin = !!r?.isSuperAdmin;
					this.loaded = true;
				})
				.catch(() => {
					this.accessType = 'none';
					this.allowedCells = [];
					this.source = null;
					this.isSuperAdmin = false;
				})
				.finally(() => {
					this.loading = false;
					pending = null;
				});
			return pending;
		},

		/** Ô (col,row) của khối có được phép thao tác không. */
		isCellAllowed(block: number, col: number, row: number): boolean {
			if (this.accessType === 'full') return true;
			if (this.accessType === 'none') return false;
			return this.allowedSet.has(`${block},${col},${row}`);
		},

		/** Cửa sổ (toạ độ logic trong khối) có nằm trọn trong vùng được cấp không — cùng công thức với BE. */
		isRectAllowed(block: number, x: number, y: number, w: number, h: number): boolean {
			if (this.accessType === 'full') return true;
			if (this.accessType === 'none' || !(w > 0) || !(h > 0)) return false;
			const topology = useVideoWallTopology();
			const pw = topology.panelW;
			const ph = topology.panelH;
			const colStart = Math.floor(x / pw);
			const colEnd = Math.floor((x + w - 1) / pw);
			const rowStart = Math.floor(y / ph);
			const rowEnd = Math.floor((y + h - 1) / ph);
			for (let c = colStart; c <= colEnd; c++) for (let r = rowStart; r <= rowEnd; r++) if (!this.allowedSet.has(`${block},${c},${r}`)) return false;
			return true;
		},

		/** Nội dung thông báo khi bị chặn. */
		deniedMessage(): string {
			return this.accessType === 'none'
				? i18n.global.t('lz.message.videoWall.wallPermissionNotAssigned')
				: i18n.global.t('lz.message.videoWall.positionOutsideArea');
		},
	},
});
