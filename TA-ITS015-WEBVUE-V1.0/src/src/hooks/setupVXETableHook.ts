import App from '/@/App.vue';
// VXETable
import VxeUITable from 'vxe-table';
import VXETablePluginElement from 'vxe-table-plugin-element';
import VXETablePluginExportXLSX from 'vxe-table-plugin-export-xlsx';
// import 'vxe-table/lib/style.css'; // Already imported in theme/vxe.scss
import 'vxe-table-plugin-element/dist/style.css';
// import { createI18n } from 'vue-i18n';
import enUS from 'vxe-pc-ui/lib/language/en-US'
// import viVN from 'vxe-pc-ui/lib/language/vi-VN'
import viVNTable from '../i18n/lang/vxetable/vi-vn.json';

// Vxe UI component library
import VxeUI, { VxeComponentSizeType } from 'vxe-pc-ui';
import ExcelJS from 'exceljs';
import { useThemeConfig } from '/@/stores/themeConfig';
// import json from '../utils/locale';
import { Local } from '../utils/storage';
const themeConfig = useThemeConfig();

const vxeSize: VxeComponentSizeType = useThemeConfig().themeConfig.globalComponentSize == 'small' ? 'large' : themeConfig.themeConfig.globalComponentSize == 'default' ? 'large' : 'large';

export const setupVXETable = (app: App) => {
	// Load plugins
	VxeUITable.use(VXETablePluginElement);
	VxeUITable.use(VXETablePluginExportXLSX, { ExcelJS: ExcelJS });

	// SonTH: Set language for VxeUI
	VxeUI.setI18n('en-US', enUS);
	VxeUI.setI18n('vi-VN', viVNTable);

	// SonTH: Set default language
	var lang = Local.get('themeConfig')?.globalI18n || 'en-US';
	VxeUI.setLanguage(lang);

	// VXETable global configuration https://vxetable.cn/v4.7/#/table/start/global
	VxeUITable.setConfig({
		theme: 'light', // Theme color
		size: vxeSize, // Global size mini, small, medium/default
		// i18n: (key, args) => i18n.global.t(key, args),
		// zIndex: 999, // Global zIndex starting value, if the project's z-index style value is too large, it needs to be set larger to avoid being covered
		// version: 0, // Version number, useful for some features with data caching, increasing the version number can be used to reset data
		// loadingText: 'Loading...', // Global loading prompt content, if null, no text will be displayed
		table: {
			border: true, // Whether to have borders
			stripe: true, // Whether to have zebra stripes
			// keepSource: false,// Keep the original value state, relied on by some functions, such as edit state, restore data, etc. (enabling affects performance, depending on the amount of data)
			showOverflow: true, // Set to show ellipsis when all content is too long (if it is a fixed column, it is recommended to set this value to improve rendering speed)
			size: vxeSize, // Table size [medium, small, mini]
			autoResize: true, // Automatically listen for changes in the parent element to recalculate the table (may be used in scenarios where the parent element may change dynamically, show/hide containers, column width anomalies, etc.)
			round: false, // Whether to have rounded borders
			emptyText: 'No data available', //
			columnConfig: {
				isCurrent: true, // When the mouse clicks the column header, whether to highlight the current column
				isHover: true, // When the mouse hovers over the column header, whether to highlight the current header
				resizable: true, // Whether each column enables column width adjustment
				minWidth: 70, // Minimum width of each column (auto, px, %)
			},
			rowConfig: {
				isCurrent: false, // When the mouse clicks the row, whether to highlight the current row
				isHover: true, // When the mouse hovers over the row, whether to highlight the current row
			},
			radioConfig: {
				strict: true, // Strict mode, cannot be deselected after selection
				highlight: true, // Highlight selected row
			},
			checkboxConfig: {
				strict: true, // Strict mode, when data is empty or all disabled, the header checkbox is disabled
				highlight: true, // Highlight checked row
				range: true, // Enable checkbox range selection function (after enabling, you can select or deselect specified rows by sliding the mouse within the checkbox column) ***Global configuration seems ineffective***
				trigger: 'default', // Trigger method (note: when multiple functions overlap, they will be triggered simultaneously) default (default), cell (click cell to trigger), row (click row to trigger)
			},
			exportConfig: {
				remote: false, // Whether to export from the server
				types: ['xlsx', 'csv', 'html', 'xml', 'txt'], // Optional file type list
				modes: ['current', 'selected', 'all'], // Output data method
			},
			printConfig: { modes: ['current', 'selected', 'all'] }, // Output data method
			tooltipConfig: {
				showAll: false, // Enable tooltips for all cells
				enterable: true, // Whether the mouse can enter the tooltip
				theme: 'dark'
			},
			customConfig: {
				// Global settings seem ineffective, need to be set separately
				storage: {
					// Whether to enable localStorage to save, will keep the column operation state locally (needs an id)
					visible: true, // Enable show/hide column state
					resizable: true, // Enable column width state
				},
			},
			scrollX: {
				enabled: true, // Whether to enable horizontal virtual scrolling by default
				gt: 30, // Automatically start horizontal virtual scrolling when greater than specified columns
			},
			scrollY: {
				scrollToTopOnChange: true, // When the data source is changed, automatically scroll the vertical scrollbar to the top
				enabled: true, // Whether to enable vertical virtual scrolling by default
				gt: 50, // Automatically trigger to enable virtual scrolling when data exceeds specified quantity
			},
		},
		grid: {
			size: vxeSize,
			toolbarConfig: {
				size: vxeSize, // Size medium, small, mini
				perfect: false, // Matching style
			},
			zoomConfig: { escRestore: true }, // Whether to allow restoration by pressing the ESC key
		},
		pager: {
			size: vxeSize,
			background: true, // With background color
			perfect: false, // Matching style
			autoHidden: false, // Automatically hide when there is only one page
			pageSize: 50, // Page size
			pagerCount: 10, // Number of page number buttons to display
			pageSizes: [50, 100, 200, 500], // Page size options list
			layouts: ['PrevJump', 'PrevPage', 'JumpNumber', 'NextPage', 'NextJump', 'Sizes', 'FullJump', 'PageCount', 'Total'], // Custom layout
		},
	});
	// Install UI components
	app.use(VxeUI);
	app.use(VxeUITable);
	// Mount internal objects to the vue instance, for example:
	// app.config.globalProperties.$XModal = VxeUI.modal
	// app.config.globalProperties.$XPrint = VxeUI.print
	// app.config.globalProperties.$XSaveFile = VxeUI.saveFile
	// app.config.globalProperties.$XReadFile = VxeUI.readFile
};
