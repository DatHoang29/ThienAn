import { defineStore } from 'pinia'; // Này là import pinia => Cơ chế quản lý biến toàn cục toàn hệ thống

// Này là export ra một biến toàn cục cụ thể
export const useMonitorVms = defineStore('monitorVms', {
	state: () => ({
		url: null, // Cho nay anh dat mot bien de luu thoi gian ma socket update lai gia tri
		eqipmentId: null as string | null, // cho nay anh dat mot bien de luu gia tri ma socket lay ra duoc => nay tuy thuoc vaof ben trong backend chu cho socket gui ra gif nhe
		listUrl: [],
		isVmsUpdate: false
	})
});

