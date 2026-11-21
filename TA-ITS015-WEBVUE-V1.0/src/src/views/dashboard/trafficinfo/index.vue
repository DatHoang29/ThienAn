<template>
  <div class="das-traffic-container">
    <!-- TopBar styled header -->
     <div class="header">
      <el-row type="flex" justify="space-between" align="middle">
        <!-- Left: Logo -->
        <el-col :span="8" class="horizontal-layout">
          <img src="" alt="Logo" class="logo" />
          <incidentNotificationComp ref="incidentNotificationCompRef" />
        </el-col>
       
            
        <!-- Right: Weather, Home, Account -->
        <el-col :span="8" class="right-content">
          <!-- Weather -->


          <!-- Home -->
          <div class="home-info">
            <el-button type="primary" @click="navigateToHome">{{ $t('Home') }}</el-button>
          </div>
          
          <!-- Account -->
          <span class="el-dropdown-link">
              <el-avatar icon="ele-User" />
            </span>
        </el-col>
      </el-row>
    </div>
    
    <!-- Wrapper to hold both divs side by side -->
    <div class="container">
      <!-- Left side div -->
      <div class="left-container">
        <el-form :model="state.ruleForm" ref="ruleFormRef" :show-message="false" :inlineMessage="true" label-width="auto">
          <!-- <div class="map-container">
            <div id="map" style="height: 500px;">
              <simplePopupComp ref="simplePopupCompRef"/>
              <myPopupComp ref="myPopupCompRef"/>
              <button @click.prevent ="refreshMapData" 
            style="position: absolute; bottom: 20px; left: 20px; padding: 10px 15px; font-size: 16px; color: white; background-color: rgba(0, 123, 255, 0.7); border: none; border-radius: 4px; cursor: pointer; transition: background-color 0.3s ease;">
              Refresh Map Data
              </button>
            </div>

          </div> -->
          <simpleMapComp ref="simpleMapCompRef"/>
          <div style="display: flex; flex-direction: row; justify-content: space-around;  flex: 1 1 0%;">
            <el-card shadow="hover" style="margin-top: 5px;">
              <div>
                <incidentButtonsComp ref="incidentButtonsCompRef" />
                <br>
              </div>
              <vxe-grid ref="xGridIncident" class="xGrid-style" v-bind="options" v-on="gridEvents" @checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">
                <template #empty>
                  <el-empty :image-size="200" />
                </template>
                <!-- Grid row templates -->
                <template #row_eventTypeId="{ row }">
                  <el-text>{{ incidentEventTypeDisplay(row, state.eventTypeList) }}</el-text>
                </template>
                <template #row_index="{row, rowIndex}">
                  {{ (rowIndex + 1) }}
                </template>
                <template #row_name="{ row }">
                  <el-text tag="b">{{ row.code }}</el-text>
                  <br>
                  <el-text>{{ row.name }}</el-text>
                </template>
                <template #row_position="{ row }">
                  <el-text>{{ state.locationList ? state.locationList.find((item: any) => item.code == row.location)?.name + ' ' : '' }}</el-text>
                  <el-text>Km {{ (row.kmNumber ?? 0) }} + {{ row.metNumber }}</el-text>
                </template>
                <template #row_state="{ row }">
                  <el-tag :type="getStateStyle(row.state)">{{ state.incidentStatusList?.find(item => item.code == row.state)?.name ?? '' }}</el-tag>
                </template>
                <template #row_camera="{ row }">
                  <el-tooltip :content="$t('Camera')" placement="top">
                    <el-button v-if="checkIncidentHasCamera(row.id)" type="primary" plain icon="ele-VideoCameraFilled" v-auth="'tmsIncident:view'"></el-button>
                  </el-tooltip>
                </template>
                <template #row_startDate="{ row }">
                  <el-text v-if="row.startDate != null">
                    {{ useDateFormat(row.startDate, 'DD/MM/YYYY') }}
                  </el-text>
                  <br>
                  <el-text v-if="row.startDate != null">
                    {{ useDateFormat(row.startDate, 'HH:mm:ss') }}
                  </el-text>
                </template>
                <template #row_record="{ row }">
                  <ModifyRecord :data="row" />
                </template>
                <template #row_buttons="{ row }">
                  <!-- <el-tooltip :content="$t('State')" placement="top">
                    <el-button icon="ele-Delete" text type="danger" :disabled="['0', '4', '5'].includes(row.state)" v-auth="'tmsIncident:delete'" @click="handleUpdateState(row)"> </el-button>
                  </el-tooltip> -->
                  <el-tooltip :content="$t('Verified')" placement="top">
                    <el-button icon="ele-Right" v-if="showActionButton(row.state, TmsIncidentStateEnum.Verified)" text type="primary" v-auth="'tmsIncident:view'" @click="updateIncidentState(row.id, TmsIncidentStateEnum.Verified)"></el-button>
                  </el-tooltip>
                  <el-tooltip :content="$t('Skipped')" placement="top">
<el-button icon="ele-MuteNotification" v-if="showActionButton(row.state, TmsIncidentStateEnum.Skipped)" text type="danger" v-auth="'tmsIncident:view'" @click="updateIncidentState(row.id, TmsIncidentStateEnum.Skipped)"></el-button>
                  </el-tooltip>
                  <el-tooltip :content="$t('Finished')" placement="top">
                    <el-button icon="ele-Select" v-if="showActionButton(row.state, TmsIncidentStateEnum.Finished)" text type="primary" v-auth="'tmsIncident:view'" @click="updateIncidentState(row.id, TmsIncidentStateEnum.Finished)">
                    </el-button>
                  </el-tooltip>
                  <el-tooltip :content="$t('Canceled')" placement="top">
                    <el-button icon="ele-CloseBold" v-if="showActionButton(row.state, TmsIncidentStateEnum.Canceled)" text type="danger" v-auth="'tmsIncident:view'" @click="updateIncidentState(row.id, TmsIncidentStateEnum.Canceled)"></el-button>
                  </el-tooltip>
                </template>
                <template #header_name>
                  {{ $t('Incident') }}
                </template>
              </vxe-grid>

              <!-- Modal for editing/removing markers and polygons -->
              
            </el-card>
            <!-- Additional cards or content -->
            <el-card shadow="hover" class="main-card">
              <div class="card-content">
                <el-tabs>
                  <el-tab-pane label="Cam XYZ">
                    <el-card shadow="hover" class="card-item" style="width:415px">
                      <el-empty :image-size="100" /> <!-- Adjust image size -->
                    </el-card>
                    <p class="card-text">Details for Cam XYZ</p>
                  </el-tab-pane>
                  <el-tab-pane label="Cam ABC">
                    <el-card shadow="hover" class="card-item" style="width:415px">
                      <el-empty :image-size="100" /> <!-- Adjust image size -->
                    </el-card>
                    <p class="card-text">Details for Cam ABC</p>
                  </el-tab-pane>
                </el-tabs>
              </div>
            </el-card>
          </div>
        </el-form>
      </div>
      
      <!-- Right side div -->
      <div class="right-container">
        <el-card shadow="hover" class="card-item" v-for="i in 4" :key="i">
          <el-empty :image-size="70" />
        </el-card>
      </div>
    </div>
  </div>
</template>
<script lang="ts" setup>
import { incidentEventTypeDisplay } from '/@/utils/incidentEventTypeDisplay';
import { ref, computed, reactive, onMounted, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useDateFormat } from '@vueuse/core'
import Image from '/@/assets/performance.png';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import '@geoman-io/leaflet-geoman-free';
import '@geoman-io/leaflet-geoman-free/dist/leaflet-geoman.css';
import { getAPI } from '/@/utils/axios-utils';
import { SysConfigDataApi } from '/@/api-services/cfgSystem';
import { TmsMapApi, TmsMapLocationApi, TmsEquipmentApi, TmsEquipmentTypeApi, TmsZoneApi,TmsIncidentApi,TmsEventTypeApi,TmsTrafficInfoApi } from '/@/api-services/tms';
import { VmsCurrentApi } from '/@/api-services/vms';
import { UpdateTmsMapLocationInput,PageTmsIncidentOutput,PageTmsIncidentInput } from '/@/api-services/tms';
import { TmsEquipmentType,TmsTrafficInfoMap } from '/@/types/enums/tmsEnum';
import { ElMessageBox,ElMessage, ElForm } from 'element-plus';
import { useBroadcastChannel } from '@vueuse/core'

import simpleMapComp from './component/simpleMap.vue';
const asimpleMapCompRef = ref(null)

// Import the myPopup component
// import myPopupComp from './component/simplePopupZone.vue';

// Creare the reference to the myPopup component
const myPopupCompRef = ref(null);

interface Props {
  title: String,
  menuData: Array<any>,
}
const xGridIncident = ref<VxeGridInstance>();
const simplePopupCompRef = ref(null);
const { t: $t } = useI18n();
const props = defineProps<Props>();
const ruleFormRef = ref<InstanceType<typeof ElForm>>();
const state = reactive({
  queryParams: {
    name: undefined,
    code: undefined,
    mapId: undefined,
    equipmentType: undefined,
    equipmentId: undefined,
    pid: undefined,
  },
  localPageParam: {
		pageSize: 50 as number,
		defaultSort: { field: 'orderNo', order: 'asc', descStr: 'desc' },
	},
  id: '',
  showWorkDetailIdRow: false,
  MapList: [] as any[],
  EquipmentList: [] as any[],
  VmsCurrentList: [] as any[],
  EquipmentTypeList: [] as any[],
  ZonesList: [] as any[],
  IncidentList: [] as any[],
  IncidentWithZones: [] as any[],
  sourceDetectList: [] as any,
	eventTypeList: [] as any,
	locationList: [] as any,
	laneList: [] as any,
	zoneList: [] as any,
	incidentStatusList: [] as any,
  map: null as L.Map | null,
  markersLayer: L.layerGroup() as L.LayerGroup<L.Marker>,
  polygonsLayer: L.layerGroup() as L.LayerGroup<L.Polygon>,
  tileLayer: null as L.TileLayer | null,
  imageOverlay: null as L.ImageOverlay | null,
  ruleForm: {} as UpdateTmsMapLocationInput,
  geoJsonData: {
    type: "FeatureCollection",
    features: [] as GeoJSON.Feature[],
  },
  editDialogVisible: false,
  editFeature: {
    id: '',
    status: '',
    strokeColor: '#000000',
    strokeWidth: 2,
    strokeOpacity: 1,
    fillColor: '#ff0000',
    fillOpacity: 1,
    type: '',
    layer: null as L.Layer | null,
  },
});

const options = useVxeTable<PageTmsIncidentOutput>(
		{
			id: 'tmsIncidentGrid',
			name: 'TmsIncident',
			columns: [
				// Fields
				// { type: 'checkbox', fixed: 'left', width: 30 },
				{ title: $t('Idx'), minWidth: 40, slots: { default: 'row_index' } },
				{ field: 'name', title: $t('Name'), minWidth: 300, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_name' } },
				{ field: 'eventTypeId', title: $t('EventType'), minWidth: 100, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_eventTypeId' } },
				{ field: 'startDate', title: $t('StartDate'), minWidth: 100, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_startDate' } },
				{ field: 'createdBy', title: $t('CreatedBy'), minWidth: 80, showOverflow: 'tooltip', sortable: true },
				{ field: 'location', title: $t('Position'), minWidth: 200, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_position' } },
				{ field: 'state', title: $t('State'), fixed: 'right', minWidth: 100, showOverflow: 'tooltip', slots: { default: 'row_state' } },
				{ title: $t('Cam'), fixed: 'right', minWidth: 60, showOverflow: 'tooltip', slots: { default: 'row_camera' } },
				{ title: $t('Actions'), fixed: 'right', width: 140, showOverflow: true, slots: { default: 'row_buttons' } },
			]
		},
		// vxeGrid configuration parameters (any parameter can be overridden here), refer to the official vxe-table documentation
		{
			// Proxy configuration
			proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
			// Sorting configuration
			// sortConfig: { defaultSort: Local.get(localPageParamKey)?.defaultSort || state.localPageParam.defaultSort },
			// // Pagination configuration
			// pagerConfig: { pageSize: Local.get(localPageParamKey)?.pageSize || state.localPageParam.pageSize },
			// Toolbar configuration
			toolbarConfig: { export: false },
			rowConfig: {
				height: 60
			}
		}
	);
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params = Object.assign(state.queryParams, { page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order, descStr: 'desc' }) as PageTmsIncidentInput;
	return await getAPI(TmsIncidentApi).apiTmsTmsincidentPagePost(params);
};
// Query operation
	const handleQuery = async () => {
		options.loading = true;
		await xGridIncident.value?.commitProxy('reload') ;

		options.loading = false;
	};
// Initialize the map and load data
onMounted(async () => {
  try {
    // Fetch Map List data
    const mapData = await getAPI(TmsMapApi).apiTmsTmsmapListGet();
    state.MapList = mapData?.data?.result || [];

    // Fetch Equipment Type List data
    const equipmentTypeData = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet();
    state.EquipmentTypeList = equipmentTypeData?.data?.result || [];
 // Fetch Incident List data
    const incidentData = await getAPI(TmsIncidentApi).apiTmsTmsincidentListPost();
    state.IncidentList = incidentData?.data?.result || [];

    const incidentZoneData = await getAPI(TmsTrafficInfoApi).apiTmsTmstrafficinfoCheckincidentexistsPost();
    state.IncidentWithZones = incidentZoneData?.data?.result || [];
    // Fetch Zones List data
    const zoneData = await getAPI(TmsZoneApi).apiTmsTmszoneGettreeGet();
    state.ZonesList = zoneData?.data?.result || [];

    let response = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet('tms_source_detect');
		state.sourceDetectList = response.data.result;

		response = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet('tms_location');
		state.locationList = response.data.result;

		response = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet('tms_lane');
		state.laneList = response.data.result;

		response = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet('tms_incident_status');
		state.incidentStatusList = response.data.result;

		response = await getAPI(TmsEventTypeApi).apiTmsTmseventtypeListGet();
		state.eventTypeList = response.data.result;
    
    response = await getAPI(VmsCurrentApi).apiVmsVmscurrentListGet();
		state.VmsCurrentList = response.data.result;

    // Initialize map and set watchers
   
  } catch (error) {
    console.error('Error fetching data:', error);
  }
});

const filteredMenuData = computed(() => {
  return state.ZonesList.filter(item => item.id !== state.queryParams.pid);
});
  // ============= TRAFFIC - INCIDENT - CAMLIVE - WEATHER
  import { defineAsyncComponent } from 'vue';

  const incidentButtonsComp = defineAsyncComponent(() => import('./component/incidentButtons.vue'));
  const incidentButtonsCompRef = ref<InstanceType<typeof incidentButtonsComp>>();

  const incidentNotificationComp = defineAsyncComponent(() => import('./component/incidentNotification.vue'));
  const incidentNotificationCompRef = ref<InstanceType<typeof incidentNotificationComp>>();

  const workUnitPopupComp = defineAsyncComponent(() => import('./component/workUnitPopup.vue'));
  const workUnitPopupCompRef = ref<InstanceType<typeof workUnitPopupComp>>();

  import trafficInfoPopupComp from './component/trafficInfoPopup.vue';
  const trafficInfoPopupCompRef = ref<InstanceType<typeof trafficInfoPopupComp>>();

  //==============================
  import { TmsIncidentStateEnum } from '/@/types/enums';

  const updateIncidentState = async(incidentId: string, incidentState: string) => {
    ElMessageBox.confirm(
        $t('fe.base.message.confirmProcess'),
        $t('fe.base.text.confirm'),
        {
            confirmButtonText: $t('fe.base.button.confirm'),
            cancelButtonText: $t('fe.base.button.cancel'),
            type: 'warning',
        }
    )
    .then(async() => {
        state.disableButton = true;

        await getAPI(TmsIncidentApi).apiTmsTmsincidentUpdatestatePost({Id: incidentId, State: incidentState});

        handleQuery();

        ElMessage.success($t('fe.base.message.processCompleted'))
    })
    .catch((error) => {
      ElMessage.success(error);
    })
    .finally(() => { 
      state.disableButton = false; 
    });
  }

  const showActionButton = (curIncidentState: string, newIncidentState: string) => {
    switch(curIncidentState){
      case TmsIncidentStateEnum.Canceled: // Đã hủy
        return false;

      case TmsIncidentStateEnum.Finished: // Đã kết thúc
        return false;

      case TmsIncidentStateEnum.Skipped: // Đã bỏ qua
        return false;

      case TmsIncidentStateEnum.Registration: // Mới được đăng ký
        return newIncidentState == TmsIncidentStateEnum.Verified || newIncidentState == TmsIncidentStateEnum.Skipped;

      case TmsIncidentStateEnum.Verified: // Đã được xác thực sự cố
        return newIncidentState == TmsIncidentStateEnum.Skipped || newIncidentState == TmsIncidentStateEnum.Canceled || newIncidentState == TmsIncidentStateEnum.Finished;
      
      case TmsIncidentStateEnum.InProgress: // Đã được áp kịch bản chi tiết
        return newIncidentState == TmsIncidentStateEnum.Canceled || newIncidentState == TmsIncidentStateEnum.Finished;

      default:
        return false;
    }
  }

  const getStateStyle = (incidentState: string) => {
    switch(incidentState){
      case TmsIncidentStateEnum.Canceled: // Đã hủy
        return 'danger';

      case TmsIncidentStateEnum.Finished: // Đã kết thúc
        return 'success';

      case TmsIncidentStateEnum.Registration: // Mới được đăng ký
        return 'info';

      case TmsIncidentStateEnum.Skipped: // Đã bỏ qua
        return 'warning';

      case TmsIncidentStateEnum.Verified: // Đã được xác thực sự cố
        return 'primary';

      case TmsIncidentStateEnum.InProgress: // Đã được áp kịch bản chi tiết
        return 'primary';

      default:
        return 'primary';
    }
  }

  const checkIncidentHasCamera = (incidentId: string) => {
    return true;
  }
</script>

<style scoped>
.map-container {
  position: relative;
  width: 100%;
  height: 500px;
}
.container {
  display: flex;
  flex-direction: row; /* Arrange children side by side */
  justify-content: space-between; /* Space out the left and right containers */
  align-items: flex-start; /* Align items to the start of the cross axis (top) */
}

.left-container {
  flex: 3; /* Larger width for the left container */
  margin-right: 5px;
}

.right-container {
  flex: 1; /* Smaller width for the right container */
  display: flex;
  flex-direction: column; /* Arrange items in a column */
}
card-container {
  display: flex;
  gap: 20px; /* Space between cards */
}
.main-card {
  margin-top: 5px;
  width: 30%; /* Đảm bảo thẻ chiếm toàn bộ chiều rộng của phần tử chứa */
  max-width: 500px; /* Hoặc chiều rộng tối đa bạn muốn */
}

.card-content {
  margin-top: 5px;
  display: flex;
  flex-direction: column; /* Đặt hướng của các phần tử con thành dọc */
  align-items: center; /* Canh giữa các phần tử con theo chiều ngang */
  text-align: center; /* Canh giữa văn bản trong thẻ */
}

.card-item {
  width: 100%;
  margin-top: 5px;
 /* Đảm bảo thẻ con chiếm toàn bộ chiều rộng của phần tử chứa */
}

.card-text {
  font-size: 18px; /* Điều chỉnh kích thước chữ */
  margin-top: 10px; /* Thêm khoảng cách phía trên văn bản */
}
.xGrid-style {
  height: 338px; /* Hoặc giá trị phù hợp với kích thước mong muốn */
  overflow: auto; /* Để hiển thị thanh cuộn nếu cần */
}
.header {
  padding: 10px 20px;
  background-color: #f5f5f5;
}

.logo {
  height: 50px;
}

.center-content {
  text-align: center;
}

.right-content {
  display: flex;
  align-items: center;
  justify-content: flex-end;
}

.weather-info {
  margin-right: 20px;
}

.el-dropdown-link {
  cursor: pointer;
}
.horizontal-layout {
  display: flex;
  align-items: center;
}

.weather-info {
  display: flex;
  align-items: center;
  margin-left: 10px; /* Optional: Add space between avatar and text */
}

.info-text {
  margin-left: 10px; /* Optional: Add space between avatar and text */
}
</style>
