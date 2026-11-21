import { defineStore } from 'pinia';

/**
 * Original routes returned by the backend (unprocessed)
 * @methods setCacheKeepAlive Sets the original route data from the interface
 */
export const useRequestOldRoutes = defineStore('requestOldRoutes', {
	state: (): RequestOldRoutesState => ({
		requestOldRoutes: [],
	}),
	actions: {
		async setRequestOldRoutes(routes: Array<string>) {
			this.requestOldRoutes = routes;
		},
	},
});


/**
 * Recursively searches through the routes and returns routes that match the defaultMenu IDs.
 * @param routes - The array of routes to search through.
 * @param defaultMenu - An array of IDs to match against the routes.
 * @returns An array of routes that match the defaultMenu IDs.
 */
export function searchRoutes(routes: any[], defaultMenu: string[]): any[] {
    let matchedRoutes: any[] = [];
    routes.forEach(route => {
        // Check if the current route's ID is in the defaultMenu
        if (defaultMenu.includes(route.id)) {
            matchedRoutes.push(route);
        }
        // If the route has children, recursively search through them
        if (route.children && route.children.length > 0) {
            matchedRoutes = matchedRoutes.concat(searchRoutes(route.children, defaultMenu));
        }
    });

    return matchedRoutes;
}