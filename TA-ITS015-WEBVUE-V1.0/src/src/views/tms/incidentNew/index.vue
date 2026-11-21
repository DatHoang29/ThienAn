<template>
	<!-- Giữ cho môi trường mà menu còn trỏ `tms/incidentNew/index`: vẫn là khung tab menu chung. Cấu hình chuẩn: menu trỏ thẳng `tabPage/index`. -->
	<TabPage />
</template>

<script lang="ts" setup name="tmsIncidentNew">
// Author: KienLT
// Description: Trang Sự kiện - Sự cố là trang tab menu (views/tabPage); tab Sự kiện / Sự cố khai ở SysMenu loại `tab`.
// Điều phối giữa hai tab nằm ở shared/incidentNavigation.ts nên file này không còn logic riêng.
// Created date: 21/10/2026 — KienLT 13/11/2026: dùng khung tab menu chung.
import TabPage from '/@/views/tabPage/index.vue';
</script>
