import { defineStore } from 'pinia';
import type { DashboardCCTVDetailOutput, DashboardCCTVOutput, DashboardCameraInfo } from '/@/views/cctv/dashboard/types';

export const useCctvDashboardStore = defineStore('cctvDashboard', {
	state: () => ({
		dashboards: [] as DashboardCCTVOutput[],
		dashboardTags: [] as string[],
		equipments: [] as DashboardCameraInfo[],
		dashboardDetails: {} as Record<string, DashboardCCTVDetailOutput>,
	}),
	actions: {
		setDashboards(dashboards: DashboardCCTVOutput[]) {
			this.dashboards = dashboards;
		},
		setDashboardTags(tags: string[]) {
			this.dashboardTags = tags;
		},
		setEquipments(equipments: DashboardCameraInfo[]) {
			this.equipments = equipments;
		},
		setDashboardDetail(dashboard: DashboardCCTVDetailOutput) {
			this.dashboardDetails = {
				...this.dashboardDetails,
				[dashboard.id]: dashboard,
			};
		},
		removeDashboardDetail(id: string) {
			const nextDetails = { ...this.dashboardDetails };
			delete nextDetails[id];
			this.dashboardDetails = nextDetails;
		},
		clearDashboardDetails() {
			this.dashboardDetails = {};
		},
	},
});
