/**
 * mitt event type definition
 *
 * @method openSettingsDrawer Open the layout settings dialog
 * @method restoreDefault Column layout, data display when mouse enters and exits
 * @method setSendColumnsChildren Column layout, menu data is passed to the menu under navMenu when the mouse enters and exits
 * @method setSendClassicChildren Classic layout, when the split menu is turned on, the menu data is passed to the menu under navMenu
 * @method getBreadcrumbIndexSetFilterRoutes Layout settings dialog, when the split menu is turned on, the menu data is passed to the menu under navMenu
 * @method layoutMobileResize When the browser window changes, it is used to adapt the mobile interface display
 * @method openOrCloseSortable Layout settings dialog, turn on TagsView drag
 * @method openShareTagsView Layout settings dialog, turn on TagsView sharing
 * @method onTagsViewRefreshRouterView tagsview refresh interface
 * @method onCurrentContextmenuClick tagsview right-click menu item click
 */
declare type MittType<T = any> = {
	openSettingsDrawer?: string;
	restoreDefault?: string;
	setSendColumnsChildren: T;
	setSendClassicChildren: T;
	getBreadcrumbIndexSetFilterRoutes?: string;
	layoutMobileResize: T;
	openOrCloseSortable?: string;
	openShareTagsView?: string;
	onTagsViewRefreshRouterView?: T;
	onCurrentContextmenuClick?: T;
};

// mitt parameter type definition
declare type LayoutMobileResize = {
	layout: string;
	clientWidth: number;
};

// mitt parameter menu type
declare type MittMenu = {
	children: RouteRecordRaw[];
	item?: RouteItem;
};


