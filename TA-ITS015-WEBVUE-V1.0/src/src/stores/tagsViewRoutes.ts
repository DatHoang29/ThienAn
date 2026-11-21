import { defineStore } from 'pinia';
import { Session } from '/@/utils/storage';

/**
 * TagsView route list
 * @methods setTagsViewRoutes Set the TagsView route list
 * @methods setCurrenFullscreen Set the boolean state when enabling/disabling full screen
 */
export const useTagsViewRoutes = defineStore('tagsViewRoutes', {
	state: (): TagsViewRoutesState => ({
		tagsViewRoutes: [],
		isTagsViewCurrenFull: false,
	}),
	actions: {
		async setTagsViewRoutes(data: Array<string>) {
			this.tagsViewRoutes = data;
		},
		setCurrenFullscreen(bool: Boolean) {
			Session.set('isTagsViewCurrenFull', bool);
			this.isTagsViewCurrenFull = bool;
		},
	},
});
