/**
 * =============================================================================
 * CÔNG TẮC CHỌN GIAO DIỆN (DESIGN PRESET)
 * =============================================================================
 *
 * CÁCH DÙNG: chỉ cần sửa DUY NHẤT giá trị của ACTIVE_DESIGN bên dưới rồi F5 lại trang.
 *
 *   'original'        → Giao diện NGUYÊN BẢN của hệ thống (không đổi gì cả)
 *   'depthBlueAmber'  → Giao diện MẪU 1: Depth Blue + Amber Focus
 *                       (sidebar navy tối, primary xanh cobalt, header bảng slate,
 *                        dòng chọn vàng hổ phách, bỏ kẻ dọc)
 *
 * Mỗi preset gồm 2 phần, hoàn toàn tách biệt nên sửa preset này không ảnh hưởng preset kia:
 *   - Màu/font (JS): khai báo trong DESIGN_PRESETS ở file này
 *   - CSS bố cục   : theme/presets/original.scss  và  theme/presets/depthBlueAmber.scss
 *
 * Cách hoạt động: preset đang bật sẽ gắn class tương ứng vào thẻ <html>
 * (vd: <html class="design-depth-blue-amber">), toàn bộ CSS của preset được
 * bọc trong class đó nên preset không bật sẽ hoàn toàn "im lặng".
 * =============================================================================
 */

/** ⬇⬇⬇ ĐỔI DÒNG NÀY ĐỂ CHỌN GIAO DIỆN ⬇⬇⬇ */
export const ACTIVE_DESIGN: DesignPresetName = 'depthBlueAmber';
/** ⬆⬆⬆ ĐỔI DÒNG NÀY ĐỂ CHỌN GIAO DIỆN ⬆⬆⬆ */

export type DesignPresetName = 'original' | 'depthBlueAmber';

/** Cấu hình màu/font của một preset. Để undefined = giữ nguyên giá trị gốc của hệ thống. */
export interface DesignPreset {
	/** Class gắn vào <html> để kích hoạt phần CSS của preset */
	htmlClass: string;
	/** Màu chủ đạo (sinh ra --el-color-primary + thang light/dark) */
	primary?: string;
	/** Nền sidebar */
	menuBar?: string;
	/** Chữ menu sidebar */
	menuBarColor?: string;
	/** Nền item menu đang chọn */
	menuBarActiveColor?: string;
	/** Nền top bar */
	topBar?: string;
	/** Chữ top bar */
	topBarColor?: string;
	/** Font toàn cục: 'Inter' | 'Space Grotesk' | 'system' */
	globalFont?: string;
	/** Bật highlight dòng đang chọn của bảng (VxeTable rowConfig.isCurrent) */
	tableCurrentRow?: boolean;
}

export const DESIGN_PRESETS: Record<DesignPresetName, DesignPreset> = {
	/**
	 * NGUYÊN BẢN — các giá trị dưới đây trùng khớp mặc định gốc trong stores/themeConfig.ts.
	 * Chúng chỉ được áp lại khi bạn CHUYỂN từ preset khác về 'original' (để dọn màu của
	 * preset cũ còn nằm trong localStorage). Khi đã ở 'original' thì hệ thống không ghi đè,
	 * nên màu bạn tự chọn trong Cấu hình trang vẫn được giữ như trước đây.
	 */
	original: {
		htmlClass: 'design-original',
		primary: '#1677FF',
		menuBar: '#FFFFFF',
		menuBarColor: '#000000',
		menuBarActiveColor: 'var(--el-color-primary-light-7)',
		topBar: '#FFFFFF',
		topBarColor: '#000000',
		globalFont: 'Inter',
		tableCurrentRow: false,
	},

	/** MẪU 1 — Depth Blue + Amber Focus */
	depthBlueAmber: {
		htmlClass: 'design-depth-blue-amber',
		// Xanh Cobalt (dải design: #0066FF – #0066FF). Muốn tươi hơn: đổi '#0066FF'
		primary: '#1E56A0',
		menuBar: '#0F172A', // Dark Navy
		menuBarColor: '#94A3B8', // Slate Gray
		menuBarActiveColor: '#2563EB', // Royal Blue (item active)
		topBar: '#FFFFFF',
		topBarColor: '#0F172A',
		globalFont: 'Inter',
		tableCurrentRow: true,
	},
};

/** Preset đang được bật */
export const activePreset = (): DesignPreset => DESIGN_PRESETS[ACTIVE_DESIGN];

/** Có đang bật preset khác 'original' hay không */
export const isCustomDesign = (): boolean => ACTIVE_DESIGN !== 'original';
