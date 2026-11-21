import type { VwScreenOutput } from '/@/api-services/videoWall';
import type { VwTipRow } from '/@/components/videoWall/vwInfoTip.vue';

/**
 * Trạng thái KẾT NỐI của màn — Worker ghi vào `ScreenState` (Online / Offline / Warning).
 * Đây là nguồn DUY NHẤT cho "Đang bật / Đang tắt" ở mọi nơi (dashboard, bố cục màn hình, tooltip…).
 * Không nhầm với `Status` (cờ cấu hình Dùng / Không dùng do người dùng đặt).
 */
export const isScreenOnline = (s: Pick<VwScreenOutput, 'screenState'>) => String(s.screenState ?? '') === 'Online';

/** Dòng "Trạng thái" trong tooltip màn (VwInfoTip) — cùng cách đọc với dashboard. */
export const screenStateTipRow = (s: Pick<VwScreenOutput, 'screenState'>, t: (key: string) => string): VwTipRow => ({
	icon: 'ele-SwitchButton',
	label: t('lz.entity.base.status'),
	value: isScreenOnline(s) ? t('lz.label.videoWall.screenOn') : t('lz.label.videoWall.screenOff'),
	tone: isScreenOnline(s) ? 'success' : 'muted',
});
