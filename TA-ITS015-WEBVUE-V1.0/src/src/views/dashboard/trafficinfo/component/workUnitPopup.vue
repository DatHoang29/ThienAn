<template>
	<div class="simple-popup" v-if="state.showMe">
		<div class="simple-popup--container pos-relative">
			<el-button type="danger" @click="state.showMe = false" class="button-close pos-absolute top-right margin-5px" />
			<div class="panel-body">
				<el-tabs class="padding-10px h100">
					<el-tab-pane class="overflow-auto" style="max-height: 95%;" v-loading="state.isLoading" :element-loading-text="$t('fe.base.text.loading')" :label="$t('fe.base.text.summary')">
						<div v-if="state.workUnitInfo">
							<table class="table-data">
								<tr style="border-bottom: thin solid 2px;">
									<td colspan="2">
										<h3 class="text-uppercase">{{ state.workUnitInfo?.name }}</h3>
										<br>
									</td>
								</tr>
								<tr class="border-bottom--dashed">
									<th>{{ $t('entity.tmsWorkUnit.Type') }}:</th>
									<td>{{ state.workUnitTypeList?.find(et => et.id == state.workUnitInfo?.equipmentTypeId)?.name }}</td>
								</tr>
								<tr class="border-bottom--dashed">
									<th>{{ $t('entity.base.phone') }}:</th>
									<td>{{ state.workUnitInfo?.phone }}</td>
								</tr>
								<tr class="border-bottom--dashed">
									<th>{{ $t('entity.base.address') }}:</th>
									<td>{{ state.workUnitInfo?.address }}</td>
								</tr>
								<tr class="border-bottom--dashed">
									<th>{{ $t('entity.base.info') }}:</th>
									<td>{{ state.workUnitInfo?.info }}</td>
								</tr>
								<tr class="border-bottom--dashed">
									<th>{{ $t('entity.base.status') }}:</th>
									<td>
										<el-tag :type="(state.workUnitInfo?.status == 1 ? 'success' : 'danger')">{{ state.statusList?.find(wu => wu.code == state.workUnitInfo?.status)?.name }}
										</el-tag>
									</td>
								</tr>
								<tr class="border-bottom--dashed">
									<th style="width: 120px;">{{ $t('entity.base.remark') }}:</th>
									<td>{{ state.workUnitInfo?.remark }}</td>
								</tr>
							</table>
						</div>
						<div v-else>
							<el-empty :image-size="100" />
						</div>
					</el-tab-pane>
					<el-tab-pane :label="$t('fe.base.text.extend')">
						<div v-if="state.workUnitInfo">

						</div>
						<div v-else>
							<el-empty :image-size="100" />
						</div>
					</el-tab-pane>
				</el-tabs>
			</div>
		</div>
	</div>
</template>
<script lang="ts" setup>
	import { reactive, onMounted } from 'vue';
	import { getAPI } from '/@/utils/axios-utils';

	import { TmsWorkContactApi, TmsWorkUnitApi } from '/@/api-services/tms';
	import { SysConfigDataApi } from '/@/api-services/cfgSystem';

    const state = reactive({
		title: 'Title',
		showMe: false,
		isLoading: false,
		statusList: [],
		workUnitInfo: [],
		workUnitTypeList: []
    });

	// Prepare the necessary data for program
	const loadData = async() => {
		let response = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet('status_type');
		state.statusList = response.data.result ? response.data.result : [{code: '0', name: 'Disable'}, {code: '1', name: 'Enable'}];

		response = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet('work_unit_type');
		state.workUnitTypeList = response.data.result;
	}

	// DOM has completed loading, bind data to the DOM if needed
	onMounted(() => {
		loadData();
	});
	
	// Show or hide the popup
	const toggleWorkUnitPopup = async (isShow: Boolean, data: any) => {
		// Show or hide instantly
		state.showMe = isShow;

		if(data?.objectId){
			const response = await getAPI(TmsWorkContactApi).apiTmsTmsworkcontactSinglePost({id: data.objectId});

			state.workUnitInfo = response.data.result;
		}
	};

	// Export toggleWorkUnitPopup
	defineExpose({toggleWorkUnitPopup});
</script>
<style scoped>
	/* Table */
	.table-data{
		width: 100%;
		max-height: 100%;
		max-width: 100%;
	}

	.table-data th,
	.table-data td{
		text-align: left;
		font-weight: normal;
		padding: 5px;
	}

	.table-data th{
		text-align: right;
	}

	.table-data .border-bottom--dashed td,
	.table-data .border-bottom--dashed th{
		border-bottom: thin dashed #E5E5E5;
	}

    /* Simple Popup */
	.simple-popup{
		position: absolute;
		top: 0px;
		left: 0px;
		display: flex;
		justify-content: center;
		align-items: center;
		z-index: 1080 !important;
		background-color: rgba(0, 0, 0, 0.5);
		width: 100%;
		height: 100%;
	}

	.simple-popup--container{
		display: flex;
		flex-direction: column;
		overflow: hidden;
		width: fit-content;
		height: 80%;
		min-width: 40%;
		min-height: 80%;
		max-width: 80%;
		max-height: 80%;
		background-color: white;
		border-radius: 5px;
	}

	.simple-popup--container .panel-header{
		height: 40px;
		max-height: 40px;
	}

	.simple-popup--container .panel-body{
		flex-grow: 1;
	}

    /* Button close */
	.button-close{
		position: absolute;
		top: 5px;
		right: 5px;
	}

	.button-close::before{
		display: block;
		position: absolute;
		left: 0;
		top: 0;
		width: 100%;
		height: 100%;
		display: flex;
		justify-content: center;
		align-items: center;
		content: '\00d7';
		font-weight: lighter;
		font-size: 20px;
	}

	/* Position */
	.pos-relative{
		position: relative;
	}

	.pos-absolute{
		position: absolute
	}

	/* Style */
	.top-right{
		top: 0px;
		right: 0px;
		z-index: 1080 !important;
	}

	.flex-inline{
		display: flex;
		gap: 10px;
	}

	.text-uppercase{
		text-transform: uppercase;
	}

	.padding-5px{
		padding: 5px;
	}

	.padding-10px{
		padding: 10px;
	}

	.margin-5px{
		margin: 5px;
	}

	.overflow-auto{
		overflow: auto;
	}
</style>