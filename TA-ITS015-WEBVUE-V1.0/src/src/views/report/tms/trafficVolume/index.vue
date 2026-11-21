<template>
	<!-- Báo cáo lưu lượng chạy trên ĐÚNG khung tab menu chung (views/tabPage): mỗi loại báo cáo là một tab menu. -->
	<TabPage />
</template>

<script lang="ts" setup name="tmsRptTrafficVolume">
// Author: KienLT
// Description: Trang "Báo cáo lưu lượng giao thông" dạng tab menu. Mỗi loại báo cáo là một component trong ./tabs, khai ở
// SysMenu loại `tab` (con của menu này) để phân quyền/sắp thứ tự/đổi tên như mọi tab menu khác.
// Môi trường CHƯA khai các tab đó trong menu ⇒ dựng tab tạm từ danh mục loại báo cáo (traffic_volume_report_type) để
// trang vẫn dùng được y như trước; khi menu đã có tab thì cấu hình menu được ưu tiên.
// Created date: 13/11/2026
import { watch } from 'vue';
import { useRoute } from 'vue-router';
import TabPage from '/@/views/tabPage/index.vue';
import { useMenuTabs } from '/@/stores/menuTabs';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum, TmsTrafficDataReportTypeEnum } from '/@/types/enums';
import type { MenuOutput } from '/@/api-services/system/models/menu-output';

/** Loại báo cáo → component tab. */
const TAB_COMPONENTS: Record<string, string> = {
	[TmsTrafficDataReportTypeEnum.TrafficDayReportByShift]: 'report/tms/trafficVolume/tabs/dayByShift',
	[TmsTrafficDataReportTypeEnum.TraffiDayReportByTime]: 'report/tms/trafficVolume/tabs/dayByTime',
	[TmsTrafficDataReportTypeEnum.TrafficMonthReportByTime]: 'report/tms/trafficVolume/tabs/monthByTime',
	[TmsTrafficDataReportTypeEnum.TrafficMonthReportByShift]: 'report/tms/trafficVolume/tabs/monthByShift',
};

const route = useRoute();
const menuTabs = useMenuTabs();
const menuId = String(route.meta.menuId ?? '');
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;

/** Tab tạm dựng từ danh mục loại báo cáo, cùng dạng dữ liệu tab của menu để khung tab chung đọc được. */
const fallbackTabs = (): MenuOutput[] =>
	((getConfigDataByCode(BaseConfigTypeEnum.TmsTrafficVolumeReportType) ?? []) as any[])
		.filter((item) => TAB_COMPONENTS[String(item.code ?? '').trim().toLowerCase()])
		.map((item, index) => ({
			id: `${menuId}:${item.code}`,
			type: 'tab',
			name: String(item.code),
			status: 1,
			orderNo: (index + 1) * 10,
			component: TAB_COMPONENTS[String(item.code).trim().toLowerCase()],
			meta: { title: String(item.name ?? item.code) },
		})) as MenuOutput[];

// Chạy ngay (trước khi khung tab dựng) và chạy lại khi menu được nạp lại (setMenuTabs thay cả bảng tab).
watch(
	() => menuTabs.tabsByMenuId[menuId],
	(configured) => {
		if (!menuId || configured?.length) return;
		const tabs = fallbackTabs();
		if (tabs.length) menuTabs.tabsByMenuId[menuId] = tabs;
	},
	{ immediate: true }
);
</script>
