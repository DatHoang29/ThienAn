<template>
	<div class="simple-popup" v-if="state.showMe">
		<div class="simple-popup--container pos-relative">
			<el-button type="danger" @click="state.showMe = false"
				class="button-close pos-absolute top-right margin-5px" />
			<div class="panel-body">
				<el-tabs class="padding-10px h100">
					<el-tab-pane class="overflow-auto" style="max-height: 95%;" v-loading="state.isLoading"
						:element-loading-text="$t('Loading...')" :label="$t('Incidents')">
						<div v-if="state.incident">
							<table class="table-data">
								<tr style="border-bottom: thin solid 2px;">
									<td colspan="2">
										<h3 class="text-uppercase">{{ state.incident?.title }}</h3>
										<br>
									</td>
								</tr>
								<tr class="border-bottom--dashed" v-for="(name, index) in state.incident?.names"
									:key="index">
									<th style="width: 130px;">{{ $t('Incident') }} {{ index + 1 }}:</th>
									<td>{{ name }}</td>
								</tr>


							</table>
						</div>
						<div v-else>
							<el-empty :image-size="100" />
						</div>
					</el-tab-pane>
					<el-tab-pane :label="$t('Traffic Static')">

					</el-tab-pane>
				</el-tabs>
			</div>
		</div>
	</div>
</template>
<script lang="ts" setup>
import { onMounted, reactive } from 'vue';
import { TmsTrafficInfoApi, TmsZoneApi } from '/@/api-services/tms';
import { getAPI } from '/@/utils/axios-utils';

// Declare the reactive state
const state = reactive({
	showMe: false,
	incident: [],
	ZoneListData: [],
	TrafficStatic: [],
});

// Create your necessary methods
const loadData = async () => {
	state.incident = {};
};

// Load your data as soon as vue completes the creation
loadData();

// Bind your data to the DOM when the DOM has been created
onMounted(async () => {
	let response = await getAPI(TmsZoneApi).apiTmsTmszoneListGet();
	state.ZoneListData = response.data.result;

});

// Show or hide the popup
// Do something when the popup is show
const toggleMyPopup = async (isShow: Boolean, data: any) => {
	// Show or hide the popup
	state.showMe = isShow;

	try {
		// Fetch the incident zone data
		const incidentZoneData = await getAPI(TmsTrafficInfoApi).apiTmsTmstrafficinfoCheckincidentPost();
		// Log incidentZoneData for debugging
		if (Array.isArray(incidentZoneData.data.result)) {
			// Collect all incident names matching the zoneId
			const matchingNames = [];
			// Iterate over the array to find matching zoneIds
			incidentZoneData.data.result.forEach((incident: any) => {
				if (incident.zoneId === data.zoneId) {
					matchingNames.push(incident.name); // Assuming `name` is the property to display
				}
			});
			const Zonename = state.ZoneListData.find(item => item.id == data.zoneId);

			// Update yourData based on the result
			if (matchingNames.length > 0) {
				state.incident = {
					title: `Incidents found for ${Zonename.name}`,
					names: matchingNames // Join names into a single string
				};
			} else {
				state.incident = {
					title: `No incidents found for  ${Zonename.name}`,
					names: []
				};
			}
		} else {
			console.log('incidentZoneData is not an array:', incidentZoneData);
			state.incident = {
				title: `Unexpected data format received`,
				names: []
			};
		}

	} catch (error) {
		console.error('Error fetching incident zone data:', error);
	}

	// Show data for debugging purposes
};





// Export toggleSimplePopup
defineExpose({ toggleMyPopup });
</script>
<style scoped>
/* Table */
.table-data {
	width: 100%;
	max-height: 100%;
	max-width: 100%;
}

.table-data th,
.table-data td {
	text-align: left;
	font-weight: normal;
	padding: 5px;
}

.table-data th {
	text-align: right;
}

.table-data .border-bottom--dashed td,
.table-data .border-bottom--dashed th {
	border-bottom: thin dashed #E5E5E5;
}

/* Canvas */
#livecam-container {
	background-color: black !important;
	display: flex;
	align-items: center;
	justify-content: center;
}

#livecam-container canvas {
	width: 100%;
	height: auto;
	max-width: 100%;
	max-height: 100%;
}

/* Simple Popup */
.simple-popup {
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

.simple-popup--container {
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

.simple-popup--container .panel-header {
	height: 40px;
	max-height: 40px;
}

.simple-popup--container .panel-body {
	flex-grow: 1;
}

/* Button close */
.button-close {
	position: absolute;
	top: 5px;
	right: 5px;
}

.button-close::before {
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
.pos-relative {
	position: relative;
}

.pos-absolute {
	position: absolute
}

/* Style */
.top-right {
	top: 0px;
	right: 0px;
	z-index: 1080 !important;
}

.flex-inline {
	display: flex;
	gap: 10px;
}

.text-uppercase {
	text-transform: uppercase;
}

.padding-5px {
	padding: 5px;
}

.padding-10px {
	padding: 10px;
}

.margin-5px {
	margin: 5px;
}

.overflow-auto {
	overflow: auto;
}
</style>