import { defineStore } from 'pinia';

/**
 * Route cache list
 * @methods setCacheKeepAlive Set the route names to be cached (Tagsview enabled)
 * @methods addCachedView Add the route names to be cached (Tagsview disabled)
 * @methods delCachedView Delete the route names to be cached (Tagsview disabled)
 * @methods delOthersCachedViews Right-click menu `Close Others`, delete the route names to be cached (Tagsview disabled)
 * @methods delAllCachedViews Right-click menu `Close All`, delete the route names to be cached (Tagsview disabled)
 */
export const useKeepALiveNames = defineStore('keepALiveNames', {
	state: (): KeepAliveNamesState => ({
		keepAliveNames: [],
		cachedViews: [],
	}),
	actions: {
		async setCacheKeepAlive(data: Array<string>) {
			this.keepAliveNames = data;
		},
		async addCachedView(view: any) {
			if (view.meta.isKeepAlive) this.cachedViews?.push(view.name);
		},
		async delCachedView(view: any) {
			const index = this.cachedViews.indexOf(view.name);
			index > -1 && this.cachedViews.splice(index, 1);
		},
		async delOthersCachedViews(view: any) {
			if (view.meta.isKeepAlive) this.cachedViews = [view.name];
			else this.cachedViews = [];
		},
		async delAllCachedViews() {
			this.cachedViews = [];
		},
	},
});
