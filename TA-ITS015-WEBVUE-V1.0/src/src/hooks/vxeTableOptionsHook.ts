import { dayjs } from 'element-plus';
import { reactive } from 'vue';
import { VxeGridProps, VxeGridPropTypes } from 'vxe-table';

/**
 * @param {String} id Unique identifier for the table, automatically generated if empty
 * @param {String} name Table name, related to the exported file name
 * @param {VxeGridPropTypes.Columns<any>} columns Column configuration
 * @param {boolean} enableExport Whether to show the export button
 * @param {boolean} remoteExport Whether to use server-side export
 * @param {Function} searchCallback Search callback method
 * @param {Function} queryAllCallback Query all callback method
 * @param {Function} remoteExportCallback Server-side export callback method
 */
interface iVxeOption {
	id?: string;
	name?: string;
	columns: VxeGridPropTypes.Columns<any>;
	enableExport?: boolean;
	remoteExport?: boolean;
	searchCallback: Function;
	queryAllCallback?: Function;
	remoteExportCallback?: Function;
}

/**
 * Vxe Table Parameterized Hook
 * @param {iVxeOption} opt Parameters
 * @param {VxeGridProps<RowVO>} extras Parameters to override
 * @returns
 */
export const useVxeTable = <T>(opt: iVxeOption, extras?: VxeGridProps<RowVO>) => {
	// Default parameters
	opt = Object.assign({ enableExport: true, remoteExport: false }, opt);
	// Create tableId, fixed table id allows for recording adjusted column widths, which remain effective upon refresh
	opt.id = opt.id ? opt.id : String(new Date().getTime());
	// console.log(opt);
	const options = reactive<VxeGridProps>({
		id: opt.id,
		height: 'auto',
		autoResize: true,
		size: 'mini',
		loading: false,
		align: 'center', // Automatically monitor changes in the parent element to recalculate the table (may be used in scenarios where the parent element may have dynamic changes, hidden containers, abnormal column widths, etc.)
		// data: [] as Array<T>,
		columns: opt.columns,
		toolbarConfig: {
			size: 'small',
			slots: { buttons: 'toolbar_buttons', tools: 'toolbar_tools' },
			refresh: {
				queryMethod: () => opt.searchCallback(),
			},
			export: opt.enableExport,
			print: true,
			zoom: true,
			custom: true,
		},
		checkboxConfig: { range: true },
		sortConfig: { trigger: 'cell', remote: true },
		exportConfig: {
			remote: opt.remoteExport, // Set to use server-side export
			filename: `${opt.name}_export_${dayjs().format('YYMMDDHHmmss')}`,
			exportMethod: ({ options }) => {
				if (opt.remoteExportCallback) opt.remoteExportCallback(options);
			}, // Server-side export method
		},
		printConfig: { sheetName: '' },
		proxyConfig: {
			enabled: true,
			autoLoad: false,
			sort: true,
			props: {
				list: 'data.result', // When not paginated
				result: 'data.result.items', // When paginated
				total: 'data.result.total',
				message: 'data.message',
			},
			ajax: {
				query: () => Promise.resolve(), // Required to avoid errors
				queryAll: opt.queryAllCallback,
			},
		},
		customConfig: {
			storage: {
				// Whether to enable localStorage local saving, which will retain column operation status locally (requires an id)
				visible: true, // Enable show/hide column status
				resizable: true, // Enable column width status
			},
		},
	});
	return extras ? Object.assign(options, extras) : options;
};
