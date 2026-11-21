<template>
	<!-- Giữ cho môi trường mà menu còn trỏ `tms/zone/index`: vẫn là khung tab menu chung. Cấu hình chuẩn: menu trỏ thẳng `tabPage/index`. -->
	<TabPage />
</template>

<script lang="ts" setup name="tmsZone">
// Author: KienLT
// Description: Trang Zone là trang tab menu (views/tabPage); ba tab (Danh sách khu vực, Gán thiết bị, Tốc độ min/max) khai ở
// SysMenu loại `tab`. Điều phối giữa các tab nằm ở utils/zoneTabNavigation.ts nên file này chỉ còn đọc link cũ
// ?tab=list|mapping|speed lúc mở trang.
// Created date: 08/10/2026 — KienLT 26/10/2026: tab theo menu — KienLT 13/11/2026: dùng khung tab menu chung.
import { onMounted } from 'vue';
import { useRoute } from 'vue-router';
import TabPage from '/@/views/tabPage/index.vue';
import { useZoneTabNavigation, ZONE_TAB_COMPONENTS } from './utils/zoneTabNavigation';

const route = useRoute();
const navigation = useZoneTabNavigation();
onMounted(() => {
	const wanted = String(route.query.tab ?? '');
	if (wanted in ZONE_TAB_COMPONENTS) navigation.select(wanted as keyof typeof ZONE_TAB_COMPONENTS);
});
</script>
