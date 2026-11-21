import { defineStore } from 'pinia';

export const useThemeConfig = defineStore('themeConfig', {
	state: (): ThemeConfigState => ({
		themeConfig: {
			// Whether to enable the layout configuration drawer
			isDrawer: false,

			/**
			 * Global theme
			 */
			// Default primary theme color
			// Tông chủ đạo sống động hơn (azure) thay cho teal trầm cũ (#1578a3) để tổng thể rực rỡ hơn
			primary: '#1677FF', // Old teal: #1578a3 // Carmine red: #F03F24 // Delphinium blue: #0F59A4 // Mint green: #207F4C
			// Whether to enable dark mode
			isIsDark: false,

			/**
			 * Top bar settings
			 */
			// Default top bar navigation background color
			topBar: '#FFFFFF',
			// Default top bar navigation font color
			topBarColor: '#000000',
			// Whether to enable top bar background color gradient
			isTopBarColorGradual: false,

			/**
			 * Menu settings
			 */
			// Default menu navigation background color
			menuBar: '#FFFFFF',
			// Default menu navigation font color
			menuBarColor: '#000000',
			// Default menu highlight background color
			menuBarActiveColor: 'var(--el-color-primary-light-7)',
			// Whether to enable menu background color gradient
			isMenuBarColorGradual: false,

			/**
			 * Column settings
			 */
			// Default column menu background color
			columnsMenuBar: '#2C3A49',
			// Default column menu font color
			columnsMenuBarColor: '#F0F0F0',
			// Whether to enable column menu background color gradient
			isColumnsMenuBarColorGradual: false,
			// Whether to enable column menu mouse hover preload (preview menu)
			isColumnsMenuHoverPreload: false,
			// Column Logo height (px)
			columnsLogoHeight: 50,
			// Column menu width (px)
			columnsMenuWidth: 70,
			// Column menu height (px)
			columnsMenuHeight: 50,

			/**
			 * Interface settings
			 */
			// Whether to enable horizontal menu folding effect
			isCollapse: false,
			// Whether to enable menu accordion effect
			isUniqueOpened: true,
			// Whether to enable fixed Header
			isFixedHeader: true,
			// Initialization variable, used to update the height of the menu el-scrollbar, do not delete
			isFixedHeaderChange: false,
			// Whether to enable classic layout split menu (only effective for classic layout)
			isClassicSplitMenu: false,
			// Whether to enable automatic screen lock
			isLockScreen: false,
			// Enable automatic screen lock countdown (s/second)
			lockScreenTime: 300,

			/**
			 * Interface display
			 */
			// Whether to enable sidebar Logo
			isShowLogo: true,
			// Initialization variable, used to update the height of el-scrollbar, do not delete
			isShowLogoChange: false,
			// Whether to enable Breadcrumb, forced classic, horizontal layout does not display
			isBreadcrumb: false,
			// Whether to enable Tagsview
			isTagsview: true,
			// Whether to enable Breadcrumb icon
			isBreadcrumbIcon: false,
			// Number of Breadcrumb
			numberBreadcrumb: 3,
			// Whether to enable Tagsview icon
			isTagsviewIcon: true,
			// Whether to enable TagsView cache
			isCacheTagsView: true,
			// Whether to enable TagsView drag
			isSortableTagsView: true,
			// Whether to enable TagsView sharing -- Shared details page: tagsView will only appear once; Non-shared details page: tagsView will appear multiple times
			isShareTagsView: true,
			// Whether to enable Footer bottom copyright information
			isFooter: true,
			// Whether to enable grayscale mode
			isGrayscale: false,
			// Whether to enable color weakness mode
			isInvert: false,
			// Whether to enable watermark
			isWatermark: false,
			// Watermark text
			watermarkText: '',

			/**
			 * Other settings
			 */
			// Tagsview style: optional values "<tags-style-one|tags-style-four|tags-style-five>", default tags-style-five
			// The defined value is the same as the class in `/src/layout/navBars/tagsView/tagsView.vue`
			tagsStyle: 'tags-style-one',
			// Main page switching animation: Animate.css
			animation: 'fade',
			// Column highlight style: optional values "<columns-round|columns-card>", default columns-round
			columnsAsideStyle: 'columns-round',
			// Column layout style: optional values "<columns-horizontal|columns-vertical>", default columns-horizontal
			columnsAsideLayout: 'columns-vertical',

			/**
			 * Layout switching
			 * Note: For demonstration purposes, when switching layouts, the colors will be restored to the default, code location: /@/layout/navBars/topBar/settings.vue
			 * In the `initSetLayoutChange(set layout switching, reset theme style)` method
			 */
			// Layout switching: optional values "<defaults|classic|transverse|columns>", default defaults
			layout: 'defaults',

			/**
			 * Backend control routing
			 */
			// Whether to enable backend control routing
			isRequestRoutes: true,

			/**
			 * Global website title / subtitle
			 */
			// Website main title (menu navigation, browser current page title)
			globalTitle: 'TAC',
			// Website subtitle (top text on login page)
			globalViceTitle: 'TAC',
			// Website subtitle (top text on login page)
			globalViceTitleMsg: 'TAC',
			// Copyright and filing text
			copyright: 'Copyright © 2024-TAC.',
			// Default initial language, optional values "<vi-vn|en-US>", default vi-VN
			globalI18n: 'vi-VN',
			// Default global component size, optional values "<large|'default'|small>", default 'large'
			globalComponentSize: 'small',
			// Default global font, optional values "<Inter|Space Grotesk|system>", default 'Space Grotesk'
			globalFont: 'Inter',
			// System logo address
			logoUrl: '',
			// Login logo is same as system logo
			loginLogo: false,
			// Icp filing number
			icp: '',
			// Icp address
			icpUrl: 'https://tacorp.vn',
		},
	}),
	actions: {
		setThemeConfig(data: ThemeConfigState) {
			this.themeConfig = data.themeConfig;
		},
	},
});
