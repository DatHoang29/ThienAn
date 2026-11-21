<template>
	<TrafficVolumeReport :report-type="TmsTrafficDataReportTypeEnum.TrafficMonthReportByShift" />
</template>

<script lang="ts" setup name="tmsRptTrafficVolumeMonthByShift">
// Author: KienLT
// Description: Tab menu "Báo cáo lưu lượng theo ca trong tháng" — khai ở SysMenu loại `tab` với component
// `report/tms/trafficVolume/tabs/monthByShift`; nội dung dùng chung component/trafficVolumeReport.vue.
// Created date: 13/11/2026
import TrafficVolumeReport from '../component/trafficVolumeReport.vue';
import { TmsTrafficDataReportTypeEnum } from '/@/types/enums';
</script>
