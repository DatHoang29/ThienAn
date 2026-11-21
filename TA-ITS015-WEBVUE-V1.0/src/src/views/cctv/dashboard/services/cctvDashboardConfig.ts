/**
 * cctvDashboardConfig.ts
 *
 * Đọc cấu hình vận hành CCTV từ bảng SysOpConfig (nhóm `cctv_config`) trong DB và áp dụng
 * vào cấu hình mặc định phía FE, thay cho giá trị fix cứng.
 *
 * Lấy giá trị theo chuẩn chung của dự án: `useBaseConfig().getOpConfigItemByCode(groupCode, code)`
 * (giống các service khác). Nếu DB thiếu key nào thì giữ nguyên giá trị mặc định hiện tại.
 */
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseOpConfigCodeEnum } from '/@/types/enums/baseEnum';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { dashboardGridConfig } from '/@/views/cctv/dashboard/types';

/** Mã (code) các tham số cấu hình CCTV trong nhóm `cctv_config` */
export const CctvConfigCode = {
	GridColumn: 'cctv_dashboard_grid_v1_w', // Số cột lưới dashboard
	GridRow: 'cctv_dashboard_grid_v1_h', // Số hàng lưới dashboard
	GridMargin: 'cctv_dashboard_grid_v1_margin', // Khoảng cách giữa các ô (px)
	GridFloat: 'cctv_dashboard_grid_v1_float', // Cho phép ô "nổi" (không tự dồn lên)
	GridAnimate: 'cctv_dashboard_grid_v1_animate', // Bật hiệu ứng chuyển động
	GridDefaultW: 'cctv_dashboard_grid_v1_defaultW', // Kích thước ô mặc định khi thêm
	GridMinW: 'cctv_dashboard_grid_v1_minW', // Kích thước ô tối thiểu
	Vendor: 'cctv_vendor', // Phần mềm VMS tích hợp (vd: nxvms)
	StreamDefault: 'cctv_dashboard_stream_default', // Đường truyền mặc định focus (primary/secondary)
} as const;

/** Đảm bảo danh sách op config đã được tải (tải nếu rỗng) */
const ensureOpConfigLoaded = async () => {
	const base = useBaseConfig();
	if (!base.opConfigList || Object.keys(base.opConfigList).length === 0) {
		await base.setOpConfigList();
	}
	return base;
};

/** Đọc giá trị số theo code, trả về fallback nếu không có/không hợp lệ */
const readNumberConfig = (code: string, fallback: number): number => {
	const value = useBaseConfig().getOpConfigItemByCode(BaseOpConfigCodeEnum.CctvConfig, code)?.value;
	const parsed = Number(value);
	return value !== undefined && value !== null && value !== '' && Number.isFinite(parsed) ? parsed : fallback;
};

/** Đọc giá trị boolean theo code ("1"/"true" => true), trả về fallback nếu không có */
const readBooleanConfig = (code: string, fallback: boolean): boolean => {
	const value = useBaseConfig().getOpConfigItemByCode(BaseOpConfigCodeEnum.CctvConfig, code)?.value;
	if (value === undefined || value === null || value === '') return fallback;
	const text = String(value).trim().toLowerCase();
	return text === '1' || text === 'true';
};

/**
 * Lấy vendor VMS từ cấu hình DB (vd: "nxvms"). Trả về undefined nếu chưa cấu hình
 * để tầng API dùng giá trị mặc định của backend.
 */
export const getCctvVendor = (): string | undefined => {
	const value = useBaseConfig().getOpConfigItemByCode(BaseOpConfigCodeEnum.CctvConfig, CctvConfigCode.Vendor)?.value;
	const text = value === undefined || value === null ? '' : String(value).trim();
	return text || undefined;
};

/**
 * Lấy đường truyền mặc định cho focus mode từ cấu hình DB (primary/secondary).
 * Trả về 'primary' nếu chưa cấu hình.
 */
export const getCctvDefaultStream = (): 'primary' | 'secondary' => {
	const value = useBaseConfig().getOpConfigItemByCode(BaseOpConfigCodeEnum.CctvConfig, CctvConfigCode.StreamDefault)?.value;
	const text = value === undefined || value === null ? '' : String(value).trim().toLowerCase();
	return text === 'secondary' ? 'secondary' : 'primary';
};

/**
 * Lấy số cột/hàng lưới từ cấu hình DB (dùng chung cho dashboard và editor template,
 * để bố cục template khớp với lưới dashboard). Trả về mặc định nếu DB chưa cấu hình.
 */
export const loadCctvGridDimensions = async (): Promise<{ columns: number; rows: number }> => {
	await ensureOpConfigLoaded();
	return {
		columns: readNumberConfig(CctvConfigCode.GridColumn, dashboardGridConfig.column),
		rows: readNumberConfig(CctvConfigCode.GridRow, dashboardGridConfig.row),
	};
};

/**
 * Áp dụng cấu hình lưới dashboard từ DB vào `dashboardGridConfig`.
 * Gọi một lần khi khởi tạo trang dashboard (trước khi render lưới).
 * Lưới camera là ô vuông nên các chiều cao suy ra từ chiều rộng tương ứng;
 * số hàng tối thiểu/tối đa = số hàng; số ô tối đa mỗi hàng = số cột.
 */
export const applyCctvDashboardConfigFromStore = async (): Promise<void> => {
	await ensureOpConfigLoaded();

	const column = readNumberConfig(CctvConfigCode.GridColumn, dashboardGridConfig.column);
	const row = readNumberConfig(CctvConfigCode.GridRow, dashboardGridConfig.row);
	const minW = readNumberConfig(CctvConfigCode.GridMinW, dashboardGridConfig.minW);
	const defaultW = readNumberConfig(CctvConfigCode.GridDefaultW, dashboardGridConfig.defaultW);

	dashboardGridConfig.column = column;
	dashboardGridConfig.row = row;
	dashboardGridConfig.minRow = row;
	dashboardGridConfig.maxRow = row;
	dashboardGridConfig.margin = readNumberConfig(CctvConfigCode.GridMargin, dashboardGridConfig.margin);
	dashboardGridConfig.float = readBooleanConfig(CctvConfigCode.GridFloat, dashboardGridConfig.float);
	dashboardGridConfig.animate = readBooleanConfig(CctvConfigCode.GridAnimate, dashboardGridConfig.animate);
	dashboardGridConfig.minW = minW;
	dashboardGridConfig.minH = minW;
	dashboardGridConfig.defaultW = defaultW;
	dashboardGridConfig.defaultH = defaultW;
	dashboardGridConfig.maxItemsPerRow = column;
};

/**
 * Lấy danh sách đường truyền camera từ SysConfig (cctv_stream_options).
 * Trả về mảng {code, name} - ví dụ: [{code: 'primary', name: 'Luồng chính'}, {code: 'secondary', name: 'Luồng phụ'}]
 */
export const getCctvStreamOptions = (): { code: string; name: string }[] => {
	const items = useBaseConfig().getConfigDataByCode(BaseConfigTypeEnum.CctvStreamOptions);
	if (!Array.isArray(items) || items.length === 0) {
		// Fallback nếu chưa cấu hình
		return [
			{ code: 'primary', name: 'Luồng chính' },
			{ code: 'secondary', name: 'Luồng phụ' },
		];
	}
	return items.filter((item: any) => item.code).map((item: any) => ({
		code: String(item.code).trim(),
		name: String(item.name || item.code).trim(),
	}));
};
