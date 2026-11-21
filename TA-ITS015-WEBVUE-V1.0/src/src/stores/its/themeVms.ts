import { defineStore } from 'pinia';

export const useThemeVms = defineStore('themeVms', {
	state: (): VMS => ({
		themeVms: {
			// Whether to enable the layout configuration drawer
			url: '',

			/**
			 * Global theme
			 */
			// Default primary theme color
			listVms: '', // Carmine red: #F03F24 // Delphinium blue: #0F59A4 // Mint green: #207F4C
		},
	}),
	actions: {
		setThemeConfig(data: VMS) {
			this.themeVms = data.themeVms;
		},
		
	},
});
