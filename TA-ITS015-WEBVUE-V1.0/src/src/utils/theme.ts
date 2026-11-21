import { ElMessage } from 'element-plus';
import { ACTIVE_DESIGN, DESIGN_PRESETS, DesignPreset } from '/@/theme/presets/designPreset';
import { Local } from '/@/utils/storage';

/**
 * Color conversion functions
 * @method hexToRgb Convert hex color to rgb color
 * @method rgbToHex Convert rgb color to Hex color
 * @method getDarkColor Darken color value
 * @method getLightColor Lighten color value
 */
export function useChangeColor() {
	// str Color value string
	const hexToRgb = (str: string): any => {
		let hexs: any = '';
		let reg = /^\#?[0-9A-Fa-f]{6}$/;
		if (!reg.test(str)) {
			ElMessage.warning('Input error: invalid hex');
			return '';
		}
		str = str.replace('#', '');
		hexs = str.match(/../g);
		for (let i = 0; i < 3; i++) hexs[i] = parseInt(hexs[i], 16);
		return hexs;
	};
	// r represents red | g represents green | b represents blue
	const rgbToHex = (r: any, g: any, b: any): string => {
		let reg = /^\d{1,3}$/;
		if (!reg.test(r) || !reg.test(g) || !reg.test(b)) {
			ElMessage.warning('Input error: invalid rgb color value');
			return '';
		}
		let hexs = [r.toString(16), g.toString(16), b.toString(16)];
		for (let i = 0; i < 3; i++) if (hexs[i].length == 1) hexs[i] = `0${hexs[i]}`;
		return `#${hexs.join('')}`;
	};
	// color Color value string | level Degree of lightening, limited to between 0-1
	const getDarkColor = (color: string, level: number): string => {
		let reg = /^\#?[0-9A-Fa-f]{6}$/;
		if (!reg.test(color)) {
			ElMessage.warning('Input error: invalid hex color value');
			return '';
		}
		let rgb = useChangeColor().hexToRgb(color);
		for (let i = 0; i < 3; i++) rgb[i] = Math.floor(rgb[i] * (1 - level));
		return useChangeColor().rgbToHex(rgb[0], rgb[1], rgb[2]);
	};
	// color Color value string | level Degree of darkening, limited to between 0-1
	const getLightColor = (color: string, level: number): string => {
		let reg = /^\#?[0-9A-Fa-f]{6}$/;
		if (!reg.test(color)) {
			ElMessage.warning('Input error: invalid hex color value');
			return '';
		}
		let rgb = useChangeColor().hexToRgb(color);
		for (let i = 0; i < 3; i++) rgb[i] = Math.floor((255 - rgb[i]) * level + rgb[i]);
		return useChangeColor().rgbToHex(rgb[0], rgb[1], rgb[2]);
	};
	return {
		hexToRgb,
		rgbToHex,
		getDarkColor,
		getLightColor,
	};
}

/**
 * Sinh đầy đủ thang biến CSS cho một tông màu Element Plus.
 * Tạo --el-color-{name}, --el-color-{name}-dark-2 và --el-color-{name}-light-1..9 từ 1 màu hex gốc.
 * @param name Tên tông màu: 'primary' | 'success' | 'warning' | 'danger' | 'info'
 * @param color Màu hex gốc (vd: '#1677FF')
 */
export function setElColorRamp(name: string, color: string) {
	if (!color) return;
	const { getDarkColor, getLightColor } = useChangeColor();
	const root = document.documentElement.style;
	root.setProperty(`--el-color-${name}`, color);
	root.setProperty(`--el-color-${name}-dark-2`, getDarkColor(color, 0.1));
	for (let i = 1; i <= 9; i++) {
		root.setProperty(`--el-color-${name}-light-${i}`, getLightColor(color, i / 10));
	}
}

/**
 * Áp tông màu chủ đạo (primary) toàn cục: thang màu Element Plus + biến VxeTable liên quan.
 * Là nguồn duy nhất để đồng bộ giữa lúc khởi động app và khi người dùng đổi màu trong Settings.
 * @param primary Màu hex chủ đạo (vd: '#1677FF')
 */
export function setPrimaryTheme(primary: string) {
	if (!primary) return;
	setElColorRamp('primary', primary);
	const { getLightColor } = useChangeColor();
	const root = document.documentElement.style;
	root.setProperty('--vxe-ui-font-primary-color', primary);
	root.setProperty('--vxe-ui-font-primary-lighten-color', getLightColor(primary, 0.3));
}

/**
 * Bộ màu ngữ nghĩa (semantic) sống động hơn mặc định Element Plus.
 * Mặc định EP hơi trầm/olive (success #67C23A, warning #E6A23C, danger #F56C6C).
 * Bộ này rực rỡ, tương phản cao — hợp phòng điều hành giao thông.
 */
export const VIBRANT_SEMANTIC: Record<string, string> = {
	success: '#17C964', // xanh lá rực (emerald)
	warning: '#FF9500', // cam hổ phách rực
	danger: '#FF3B30', // đỏ cảnh báo rực
};

/**
 * Áp bộ màu ngữ nghĩa rực rỡ (success/warning/danger) cho toàn app.
 * Sinh đầy đủ thang light/dark để mọi thành phần Element Plus đồng bộ.
 * @param colors Map tên tông màu -> hex (mặc định VIBRANT_SEMANTIC)
 */
export function setSemanticTheme(colors: Record<string, string> = VIBRANT_SEMANTIC) {
	Object.entries(colors).forEach(([name, color]) => setElColorRamp(name, color));
}

/**
 * Áp các biến CSS màu bố cục (sidebar/top bar/menu) từ themeConfig.
 * Cần thiết để nền sidebar, chữ menu... áp dụng ngay lúc khởi động app kể cả khi
 * cache themeConfigStyle cũ không còn (settings.vue chỉ init 1 lần theo cờ 'frequency').
 * @param cfg themeConfig hiện hành
 */
export function applyLayoutColorVars(cfg: any) {
	if (!cfg) return;
	const { getLightColor } = useChangeColor();
	const root = document.documentElement.style;
	const setVar = (name: string, value?: string) => {
		if (value) root.setProperty(name, value);
	};
	setVar('--next-bg-menuBar', cfg.menuBar);
	// getLightColor chỉ nhận hex; bọc try để tránh lỗi nếu là giá trị 'var(...)'
	try {
		if (/^#?[0-9A-Fa-f]{6}$/.test(cfg.menuBar || '')) {
			root.setProperty('--next-bg-menuBar-light-1', getLightColor(cfg.menuBar, 0.05));
		}
	} catch (e) {
		/* noop */
	}
	setVar('--next-bg-menuBarColor', cfg.menuBarColor);
	setVar('--next-bg-menuBarActiveColor', cfg.menuBarActiveColor);
	setVar('--next-bg-topBar', cfg.topBar);
	setVar('--next-bg-topBarColor', cfg.topBarColor);
	setVar('--next-bg-columnsMenuBar', cfg.columnsMenuBar);
	setVar('--next-bg-columnsMenuBarColor', cfg.columnsMenuBarColor);
}
/**
 * Áp DESIGN PRESET đang bật (theme/presets/designPreset.ts) lên app:
 *  1. Gắn class preset vào <html> để phần CSS tương ứng có hiệu lực
 *  2. Ghi đè màu/font của themeConfig theo preset (preset là nguồn quyết định)
 *  3. Sinh lại thang màu primary + các biến bố cục
 *
 * Preset 'original' không ghi đè gì → giao diện nguyên bản.
 * @param cfg themeConfig hiện hành (sẽ được ghi đè tại chỗ nếu preset có khai báo)
 */
export function applyDesignPreset(cfg: any) {
	const preset = DESIGN_PRESETS[ACTIVE_DESIGN];
	if (!preset) return;

	// 1. Gắn class preset vào <html>, xoá class của các preset khác
	const html = document.documentElement;
	Object.values(DESIGN_PRESETS).forEach((p) => html.classList.remove(p.htmlClass));
	html.classList.add(preset.htmlClass);

	// 2. Xác định có nên ghi đè màu/font theo preset không:
	//    - switchedPreset      : đổi hẳn preset (ACTIVE_DESIGN) -> luôn áp màu của preset mới.
	//    - presetColorsChanged : cùng preset nhưng bảng màu trong designPreset.ts đổi (deploy).
	//    - userCustomized      : người dùng đã TỰ chỉnh màu trong drawer "Cấu hình trang".
	//    => Deploy chỉ đổi màu preset sẽ KHÔNG ghi đè màu người dùng đã tự chọn; chỉ áp cho
	//       người dùng chưa từng tự chỉnh. Đổi hẳn preset thì đặt lại baseline cho tất cả.
	const colorKeys: Array<keyof DesignPreset> = ['primary', 'menuBar', 'menuBarColor', 'menuBarActiveColor', 'topBar', 'topBarColor', 'globalFont'];
	const signature = colorKeys.map((k) => `${String(k)}=${preset[k] ?? ''}`).join('|');
	const switchedPreset = Local.get('appliedDesign') !== ACTIVE_DESIGN;
	const presetColorsChanged = Local.get('appliedDesignSig') !== signature;
	const userCustomized = Local.get('themeUserCustomized') === true;
	// Người dùng CHƯA tự chỉnh màu -> LUÔN áp màu theo preset code (nguồn sự thật) mỗi lần load,
	// KHÔNG phụ thuộc signature cache. Trước đây chỉ áp khi signature đổi (presetColorsChanged),
	// nên khi cache themeConfig/themeConfigStyle trong localStorage bị cũ/lệch (sau deploy, Ctrl+F5)
	// thì cache cũ thắng -> mất/sai màu nền sidebar + màu chữ. Đổi hẳn preset (switchedPreset) vẫn
	// re-baseline cho tất cả. Chỉ tài khoản đã tự chỉnh màu mới được giữ lựa chọn của họ.
	const shouldOverride = switchedPreset || !userCustomized;

	if (cfg && shouldOverride) {
		colorKeys.forEach((k) => {
			const value = preset[k];
			if (value) cfg[k] = value as string;
		});
	}

	// Đổi hẳn preset -> đặt lại baseline: xoá cờ tự chỉnh của người dùng.
	if (switchedPreset) Local.remove('themeUserCustomized');

	// Cập nhật dấu preset đã áp để lần load sau không lặp lại việc so sánh/ghi đè.
	// LƯU Ý: KHÔNG xoá 'themeConfigStyle' ở đây — việc xoá khiến lần load kế tiếp gán
	// cssText = null làm mất sạch biến màu inline (mất màu sidebar/primary).
	if (switchedPreset || presetColorsChanged) {
		Local.set('appliedDesign', ACTIVE_DESIGN);
		Local.set('appliedDesignSig', signature);
	}

	// 3. Áp thang màu + biến bố cục — LUÔN chạy mỗi lần load (không phụ thuộc cache),
	//    đảm bảo --next-bg-menuBar... luôn có giá trị nên nền sidebar không bị mất màu.
	setPrimaryTheme(cfg?.primary);
	applyLayoutColorVars(cfg);
}
