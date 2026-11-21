import { dayjs } from 'element-plus';
import { reactive } from 'vue';
import { VxeGridProps, VxeGridPropTypes, VxeComponentSizeType } from 'vxe-table';
import { useThemeConfig } from '/@/stores/themeConfig';
import { activePreset } from '/@/theme/presets/designPreset';
import { merge } from 'lodash-es';
import { fa } from 'element-plus/es/locale';

const vxeSize: VxeComponentSizeType = useThemeConfig().themeConfig.globalComponentSize == 'small' ? 'mini' : useThemeConfig().themeConfig.globalComponentSize == 'default' ? 'small' : 'medium';
/**
 * @param {String} id Unique identifier for the table, automatically generated if empty
 * @param {String} name Table name, related to the exported file name
 * @param {VxeGridPropTypes.Columns<any>} columns Column configuration
 */
interface iVxeOption {
	id?: string;
	name?: string;
	columns: VxeGridPropTypes.Columns<any>;
}

/**
 * Vxe Table Parameterized Hook
 * @param {iVxeOption} opt Parameters
 * @param {VxeGridProps<RowVO>} extras Parameters to override
 * @returns
 */
export const useVxeTable = <T>(opt: iVxeOption, extras?: VxeGridProps<T>) => {
	// Default parameters
	opt = Object.assign({ enableExport: true, remoteExport: false }, opt);
	// Create tableId, fixed table id allows for recording adjusted column widths, which remain effective upon refresh
	opt.id = opt.id ? opt.id : String(new Date().getTime());
	// console.log(opt);
	const options = reactive<VxeGridProps>({
		id: opt.id,
		height: 'auto',
		autoResize: true,
		size: vxeSize,
		loading: false,
		align: 'center', // Automatically monitor changes in the parent element to recalculate the table (may be used in scenarios where the parent element may have dynamic changes, hidden containers, abnormal column widths, etc.)
		// data: [] as Array<T>,
		columns: opt.columns,
		toolbarConfig: {
			size: vxeSize,
			slots: { buttons: 'toolbar_buttons', tools: 'toolbar_tools' },

			export: false,
			print: false,
			zoom: false,
			custom: false,
		},
		columnConfig: {
			isCurrent: false,
			resizable: true,
		},
		// Design preset: chỉ bật highlight dòng đang chọn (nền vàng hổ phách) khi preset yêu cầu.
		// Preset 'original' không thêm rowConfig → giữ nguyên hành vi gốc của bảng.
		...(activePreset().tableCurrentRow ? { rowConfig: { isCurrent: true, isHover: true } } : {}),
		checkboxConfig: {
			range: true,
			reserve: true,
			highlight: false,
		},

		sortConfig: { trigger: 'cell', remote: true },
		exportConfig: {
			remote: false, // Set to use server-side export
			filename: `${opt.name}_${dayjs().format('YYMMDDHHmmss')}`,
		},
		pagerConfig: {
			enabled: true,
			size: vxeSize,
			pageSize: 50,
			pageSizes: [50, 100, 200, 500]
		}, // Pagination
		printConfig: { sheetName: '' },
		proxyConfig: {
			enabled: true,
			autoLoad: false,
			sort: true,
			props: {
				list: 'data.result', // When not paginated
				result: 'data.result.records', // When paginated
				total: 'data.result.total',
				message: 'data.message',
			},
		},
		customConfig: {
			// immediate: false — chỉ áp dụng thay đổi khi ấn Xác nhận, không live-update ngay.
			// Với immediate: true (default), mỗi lần toggle trong dialog gọi saveCustomStore ngay,
			// nhưng cancelCustom KHÔNG gọi saveCustomStore để undo → localStorage bị lệch,
			// khiến các cột đã ẩn bật lại sau khi ấn Hủy hoặc reload trang.
			immediate: false,
			storage: {
				// Whether to enable localStorage local saving, which will retain column operation status locally (requires an id)
				visible: true, // Enable show/hide column status
				resizable: true, // Enable column width status
			},
		},
	});
	return extras ? merge(options, extras) : options;
};
