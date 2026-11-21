<template>
	<div class="sys-server-container">
		<el-row :gutter="8">
			<el-col :md="12" :sm="24">
				<el-card shadow="hover" :header="$t('lz.label.sysInfo.systemInfo')">
					<table class="sysInfo_table">
						<tbody>
							<tr>
								<td class="sysInfo_td">{{ $t('lz.label.sysInfo.hostName') }}:</td>
								<td class="sysInfo_td">{{ state.machineBaseInfo.hostName }}</td>
							</tr>
							<tr>
								<td class="sysInfo_td">{{ $t('lz.label.sysInfo.operatingSystem') }}:</td>
								<td class="sysInfo_td">{{ state.machineBaseInfo.systemOs }}</td>
							</tr>
							<tr>
								<td class="sysInfo_td">{{ $t('lz.label.sysInfo.systemArchitecture') }}:</td>
								<td class="sysInfo_td">{{ state.machineBaseInfo.osArchitecture }}</td>
							</tr>
							<tr>
								<td class="sysInfo_td">{{ $t('lz.label.sysInfo.cpuCores') }}:</td>
								<td class="sysInfo_td">{{ state.machineBaseInfo.processorCount }}</td>
							</tr>
							<tr>
								<td class="sysInfo_td">{{ $t('lz.label.sysInfo.runtime') }}:</td>
								<td class="sysInfo_td">{{ state.machineBaseInfo.sysRunTime }}</td>
							</tr>
							<tr>
								<td class="sysInfo_td">{{ $t('lz.label.sysInfo.internalIpAddress') }}:</td>
								<td class="sysInfo_td">{{ state.machineBaseInfo.localIp }}</td>
							</tr>
							<tr>
								<td class="sysInfo_td">{{ $t('lz.label.sysInfo.framework') }}:</td>
								<td class="sysInfo_td">{{ state.machineBaseInfo.frameworkDescription }}</td>
							</tr>
						</tbody>
					</table>
				</el-card>
			</el-col>
			<el-col :md="12" :sm="24">
				<el-card shadow="hover" :header="$t('lz.label.sysInfo.usageInfo')">
					<el-row>
						<el-col :xs="12" :sm="12" :md="12" :lg="12" :xl="12" style="text-align: center">
							<el-progress
								type="dashboard"
								:percentage="parseInt(state.machineUseInfo.ramRate == undefined ? 0 : state.machineUseInfo.ramRate.substr(0, state.machineUseInfo.ramRate.length - 1))"
								:color="'var(--el-color-primary)'"
							>
								<template #default>
									<span>{{ state.machineUseInfo.ramRate }}<br /></span>
									<span style="font-size: 10px">
										{{ $t('lz.label.sysInfo.used') }}: {{ state.machineUseInfo.usedRam }}<br />
										{{ $t('lz.label.sysInfo.remaining') }}: {{ state.machineUseInfo.freeRam }}<br />
										{{ $t('lz.label.sysInfo.memoryUsageRate') }}
									</span>
								</template>
							</el-progress>
						</el-col>
						<el-col :xs="12" :sm="12" :md="12" :lg="12" :xl="12" style="text-align: center">
							<el-progress
								v-for="(item, index) in state.machineUseInfo.cpuRates"
								:key="index"
								type="dashboard"
								:percentage="parseInt(item == undefined ? 0 : item.substr(0, item.length - 1))"
								:color="'var(--el-color-primary)'"
							>
								<template #default>
									<span>{{ item }}<br /></span>
									<span style="font-size: 10px">{{ $t('lz.label.sysInfo.cpuUsageRate') }}<br />{{ $t('lz.label.sysInfo.core') }} {{ index + 1 }}</span>
								</template>
							</el-progress>
						</el-col>
					</el-row>

					<el-row>
						<table class="sysInfo_table">
							<tbody>
								<tr>
									<td class="sysInfo_td">{{ $t('lz.label.sysInfo.startupTime') }}:</td>
									<td class="sysInfo_td">{{ state.machineUseInfo.startTime }}</td>
								</tr>
								<!-- <tr>
									<td class="sysInfo_td">Runtime:</td>
									<td class="sysInfo_td">{{ state.machineUseInfo.runTime }}</td>
								</tr> -->
								<tr>
									<td class="sysInfo_td">{{ $t('lz.label.sysInfo.websiteDirectory') }}:</td>
									<td class="sysInfo_td">{{ state.machineBaseInfo.wwwroot }}</td>
								</tr>
								<tr>
									<td class="sysInfo_td">{{ $t('lz.label.sysInfo.developmentEnvironment') }}:</td>
									<td class="sysInfo_td">{{ state.machineBaseInfo.environment }}</td>
								</tr>
								<tr>
									<td class="sysInfo_td">{{ $t('lz.label.sysInfo.environmentVariables') }}:</td>
									<td class="sysInfo_td">{{ state.machineBaseInfo.stage }}</td>
								</tr>
							</tbody>
						</table>
					</el-row>
				</el-card>
			</el-col>
		</el-row>

		<el-row :gutter="8">
			<el-col :md="24" :sm="24">
				<el-card shadow="hover" :header="$t('lz.label.sysInfo.diskInfo')" style="margin-top: 5px">
					<el-row>
						<el-col
							:span="4"
							:xs="(24 / state.machineDiskInfo.length) * 2"
							:sm="24 / state.machineDiskInfo.length"
							:md="24 / state.machineDiskInfo.length"
							:lg="24 / state.machineDiskInfo.length"
							:xl="24 / state.machineDiskInfo.length"
							v-for="d in state.machineDiskInfo"
							:key="d.diskName"
							style="text-align: center"
						>
							<el-progress type="circle" :percentage="d.availablePercent" :color="'var(--el-color-primary)'">
								<template #default>
									<span>{{ d.availablePercent }}%<br /></span>
									<span style="font-size: 10px">
										{{ $t('lz.label.sysInfo.used') }}: {{ d.used }}GB<br />
										{{ $t('lz.label.sysInfo.remaining') }}: {{ d.availableFreeSpace }}GB<br />
										{{ d.diskName }}
									</span>
								</template>
							</el-progress>
						</el-col>
					</el-row>
				</el-card>
			</el-col>
		</el-row>
	</div>
</template>

<script lang="ts" setup name="sysServer">
import { onActivated, onDeactivated, onMounted, reactive } from 'vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysServerApi } from '/@/api-services/system';

const state = reactive({
	machineBaseInfo: [] as any,
	machineUseInfo: [] as any,
	machineDiskInfo: [] as any,
	assemblyInfo: [] as any,
	timer: null as any,
});

onMounted(async () => {
	loadMachineBaseInfo();
	loadMachineUseInfo();
	loadMachineDiskInfo();
});

// Server Configuration Information
const loadMachineBaseInfo = async () => {
	// var res = await getAPI(SysServerApi).apiSystemSysserverServerbaseGet();
	// state.machineBaseInfo = res.data.result;
};

// Server Memory Information
const loadMachineUseInfo = async () => {
	// var res = await getAPI(SysServerApi).apiSystemSysserverServerusedGet();
	// state.machineUseInfo = res.data.result;
};

// Server Disk Information
const loadMachineDiskInfo = async () => {
	var res = await getAPI(SysServerApi).apiSystemSysserverServerdiskGet();
	state.machineDiskInfo = res.data.result;
};

// Real-time Memory Refresh
const refreshData = () => {
	loadMachineUseInfo();
};

onActivated(() => {
	state.timer = setInterval(() => {
		refreshData();
	}, 10000);
});

onDeactivated(() => {
	clearInterval(state.timer);
});
</script>

<style lang="scss" scoped>
.sysInfo_table {
	width: 100%;
	min-height: 40px;
	line-height: 40px;
	text-align: center;
}

.sysInfo_td {
	border-bottom: 1px solid #e8e8e8;
	min-width: 100px;
}
</style>