<template>
	<TrafficVolumeReport :report-type="TmsTrafficDataReportTypeEnum.TraffiDayReportByTime" />
</template>

<script lang="ts" setup name="tmsRptTrafficVolumeDayByTime">
// Author: KienLT
// Description: Tab menu "Báo cáo lưu lượng theo khoảng thời gian trong ngày" — khai ở SysMenu loại `tab` với component
// `report/tms/trafficVolume/tabs/dayByTime`; nội dung dùng chung component/trafficVolumeReport.vue.
// Created date: 13/11/2026
import TrafficVolumeReport from '../component/trafficVolumeReport.vue';
import { TmsTrafficDataReportTypeEnum } from '/@/types/enums';
</script>
