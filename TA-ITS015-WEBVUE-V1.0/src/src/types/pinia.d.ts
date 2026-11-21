/**
 * Pinia type definitions
 */

// User information
declare interface UserInfos<T = any> {
	authBtnList: string[];
	photo: string;
	roles: string[];
	time: number;
	userName: string;
	[key: string]: T;
}
declare interface UserInfosState {
	userInfos: UserInfos;
	constList: T[];
	dictList: T;
	dictListInt: T;
}
declare interface VMS {
	themeVms: {
		url: string;
		listVms: list<string>;

	};

}
// Keep alive route list
declare interface KeepAliveNamesState {
	keepAliveNames: string[];
	cachedViews: string[];
}

// Original routes returned by the backend (unprocessed)
declare interface RequestOldRoutesState {
	requestOldRoutes: string[];
}

// TagsView route list
declare interface TagsViewRoutesState<T = any> {
	tagsViewRoutes: T[];
	isTagsViewCurrenFull: Boolean;
}

// Route list
declare interface RoutesListState<T = any> {
	routesList: T[];
	isColumnsMenuHover: Boolean;
	isColumnsNavHover: Boolean;
}

// Layout configuration
declare interface ThemeConfigState {
	themeConfig: {
		isDrawer: boolean;
		primary: string;
		topBar: string;
		topBarColor: string;
		isTopBarColorGradual: boolean;
		isTopBarSameThemeColor: boolean;
		menuBar: string;
		menuBarColor: string;
		menuBarActiveColor: string;
		isMenuBarColorGradual: boolean;
		columnsMenuBar: string;
		columnsMenuBarColor: string;
		isColumnsMenuBarColorGradual: boolean;
		isColumnsMenuHoverPreload: boolean;
		columnsLogoHeight: number;
		columnsMenuWidth: number;
		columnsMenuHeight: number;
		isCollapse: boolean;
		isUniqueOpened: boolean;
		isFixedHeader: boolean;
		isFixedHeaderChange: boolean;
		isClassicSplitMenu: boolean;
		isLockScreen: boolean;
		lockScreenTime: number;
		isShowLogo: boolean;
		isShowLogoChange: boolean;
		isBreadcrumb: boolean;
		isTagsview: boolean;
		isBreadcrumbIcon: boolean;
		isTagsviewIcon: boolean;
		isCacheTagsView: boolean;
		isSortableTagsView: boolean;
		isShareTagsView: boolean;
		isFooter: boolean;
		isGrayscale: boolean;
		isInvert: boolean;
		isIsDark: boolean;
		isWatermark: boolean;
		watermarkText: string;
		tagsStyle: string;
		animation: string;
		columnsAsideStyle: string;
		columnsAsideLayout: string;
		layout: string;
		isRequestRoutes: boolean;
		globalTitle: string;
		globalViceTitle: string;
		globalViceTitleMsg: string;
		copyright: string;
		globalI18n: string;
		globalComponentSize: string;
		globalFont: string;
		loginLogo: string;
		logoUrl: string; // System logo address
		icp: string; // Icp registration number
		icpUrl: string; // Icp address
		numberBreadcrumb: number;
	};
}
