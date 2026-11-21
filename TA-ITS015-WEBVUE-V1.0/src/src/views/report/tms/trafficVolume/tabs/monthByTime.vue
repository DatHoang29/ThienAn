<template>
	<TrafficVolumeReport :report-type="TmsTrafficDataReportTypeEnum.TrafficMonthReportByTime" />
</template>

<script lang="ts" setup name="tmsRptTrafficVolumeMonthByTime">
// Author: KienLT
// Description: Tab menu "Báo cáo lưu lượng theo khoảng thời gian trong tháng" — khai ở SysMenu loại `tab` với component
// `report/tms/trafficVolume/tabs/monthByTime`; nội dung dùng chung component/trafficVolumeReport.vue.
// Created date: 13/11/2026
import TrafficVolumeReport from '../component/trafficVolumeReport.vue';
import { TmsTrafficDataReportTypeEnum } from '/@/types/enums';
</script>
