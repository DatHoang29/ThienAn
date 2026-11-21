<template>
	<div class="visualizing-traffic-info">
		<!-- Map -->
		<LeafletMap  
			ref="mapRef" 
			:selected-map-id="selectedMapId" 
			:show-location-editor="false" 
			:filters="activeFilters"			
			@item-action="handleItemAction" 
			@return-geo-json-data="handleGetGeoJsonDataFromLeaflet"
			@zone-status-change="handleZoneStatusChange"
			@bearing-change="handleBearingChange"
			@rotation-lock-change="state.rotationLocked = $event"
			/>
		<!-- Control Map Button-->
		<ControlMap 
			:map="map" 
			:mapId="selectedMapId"
			:geo-json-data="state.geoJsonDataFromLeaflet"
			 :active-filters="activeFilters"
			@mapId="handleMapIdChange" 
			@zoomIn="handleZoomIn"
			@zoomOut="handleZoomOut"
			@refreshMap="handleRefreshMap"
			@filterChange="handleFilterChange"
			@rotateReset="handleRotateReset"
		/>
		<!-- Cụm điều khiển góc trên bên trái: la bàn + chú thích màu sắc bản đồ -->
		<div class="map-left-controls">
			<!-- La bàn: bấm đảo Bắc ⇄ Nam, kéo ngang xoay tự do, chuột phải về góc cấu hình -->
			<MapCompassControl
				:bearing="state.mapBearing"
				:locked="state.rotationLocked"
				tooltip-placement="right"
				@set-bearing="handleSetBearing"
				@rotate-by-delta="handleRotateByDelta"
				@reset="handleRotateReset"
			/>
			<span class="map-left-controls__divider"></span>
			<el-popover placement="right-start" :width="260" trigger="click" popper-class="map-legend-popover">
				<template #reference>
					<div>
						<el-tooltip :content="$t('lz.label.tmsMapLocation.mapLegend')" placement="right">
							<el-button circle>
								<el-icon><ele-InfoFilled /></el-icon>
							</el-button>
						</el-tooltip>
					</div>
				</template>
				<MapLegend />
			</el-popover>
		</div>
		<!-- Container -->
		<div class="visualizing-container">
			<!-- Header -->
			<div class="visualizing-container-head">
				<!-- Left header -->
				<div class="visualizing-container-head-left">
					<!-- Mặc định hiện logo; có sự cố thì nhường chỗ cho thẻ thông báo sự cố (IncidentNotificationComp luôn mount để còn lắng nghe sự cố).
					     Đặt ngoài cụm text-box để căn giữa header độc lập, không bị lệch theo offset của thẻ sự cố. -->
					<transition name="head-logo-fade">
						<div v-if="!hasIncident" class="head-logo">
							<img :src="headLogoUrl" :alt="themeConfig.globalTitle" />
						</div>
					</transition>
					<div class="visualizing-container-head-left-text">
						<div class="visualizing-container-head-left-text-box">
							<div class="display-flex--horizontal flex-gap--10px mt25">
								<!-- Begin - Add By Ngọc Thắng -->
								<IncidentNotificationComp @has-incident="onHasIncident" />
								<!-- End - Add By Ngọc Thắng -->
							</div>
						</div>
					</div>
				</div>
				<!-- Center header -->
				<div class="visualizing-container-head-center">
					<div class="visualizing-container-head-center-box">
						<div class="visualizing-container-head-center-maintitle">  {{ $t('lz.label.tms.statusTraffic') }}</div>
						<div class="visualizing-container-head-center-subtitle"> {{ $t(trafficStatusLabelKey) }}</div>
					</div>
				</div>
				<!-- Right header -->
				<div class="visualizing-container-head-right">
					<div class="visualizing-container-head-right-text">
						<!-- Cụm công cụ góc phải gom thành 1 thanh kính mờ: đồng hồ | chuông · trang chủ | tài khoản -->
						<div class="head-toolbar">
							<div class="head-toolbar__clock">
								<el-icon class="head-toolbar__clock-icon"><Clock /></el-icon>
								<ServerTime />
							</div>
							<span class="head-toolbar__divider" aria-hidden="true" />
							<!-- SonTH: Chuông thông báo (danh sách chưa đọc) — đặt trước cụm Home+User để giữ user ở góc phải -->
							<NotiBell dark class="head-toolbar__bell" />
							<WeatherComp :text-color="'var(--el-color-white)'" />
						</div>
					</div>
				</div>
			</div>

			<!-- Center Info Panel -->
			<CenterInfoPanel/>

			<!-- Tab Modal -->
			<EquipmentModal 
				v-for="modal in activeEquipmentModals"
				:key="modal.id"
				ref="equipmentModalRefs"
				@activated="handleModalActivated"
				@closed="handleModalClosed"
				@closeOthers="handleCloseOthers"
				@closeAll="handleCloseAll"
			/>		
			<UnitModal 
				v-for="modal in activeUnitModals"
				:key="modal.id"
				ref="unitModalRefs"
				@activated="handleModalActivated"
				@closed="handleModalClosed"
				@closeOthers="handleCloseOthers"
				@closeAll="handleCloseAll"
			/>		
			<!-- Zone Modal -->
			<ZoneModal
				v-for="modal in activeZoneModals"
				:key="modal.id"
				ref="zoneModalRefs"
				@activated="handleModalActivated"
				@closed="handleModalClosed"
				@closeOthers="handleCloseOthers"
				@closeAll="handleCloseAll"
			/>		
		</div>

		<!-- Add popover when hover on item-->
		 <!-- TODO: Tách ra component-->
		<el-popover
			v-model:visible="statePopover.showPopover"
			:virtual-ref="statePopover.virtualRef"
			virtual-triggering
			trigger="hover"
			placement="bottom"
			:width="250"
			popper-class="item-info-popover is-dark"

		>		
			<template #default>
				<div class="mtip" :class="'mtip--' + (statePopover.hoverItem?.action || 'default')">
					<!-- Header: chấm trạng thái + tên -->
					<div class="mtip__head">
						<span
							class="mtip__dot"
							:class="statePopover.hoverItem?.incident
								? 'is-bad'
								: (statePopover.hoverItem?.equipment
									? 'is-' + (statePopover.hoverItem?.equipmentStateValue || 'inactive')
									: 'is-ok')"
						></span>
						<div class="mtip__title">{{ statePopover.hoverItem?.name || 'Unknown' }}</div>
					</div>

					<div class="mtip__rows">
						<!-- Hover cho zone -->
						<template v-if="statePopover.hoverItem?.location">
							<div class="mtip__row">
								<el-icon class="mtip__row-ic"><LocationFilled /></el-icon>
								<span class="mtip__lb">{{ $t('lz.label.tms.location') }}</span>
								<span class="mtip__val">{{ statePopover.hoverItem?.location }}</span>
							</div>
							<div class="mtip__row">
								<el-icon class="mtip__row-ic"><Flag /></el-icon>
								<span class="mtip__lb">{{ $t('lz.label.base.status') }}</span>
								<span class="mtip__val">
									{{ hoverZoneLive?.status ?? statePopover.hoverItem?.status }}
									<span
										v-if="hoverZoneLive?.averageSpeed != null"
										class="mtip__speed"
										:class="{ 'is-held': hoverZoneLive.speedHeld }"
										:title="hoverZoneLive.speedHeld ? $t('lz.label.zoneInfo.avgSpeedHeld') : $t('lz.entity.tmsTrafficStatistic.avgSpeed')"
									>· {{ hoverZoneLive.averageSpeed }} km/h</span>
								</span>
							</div>
							<!-- Ngưỡng tốc độ của zone (TmsZone.MinSpeed/MaxSpeed), dạng biển báo -->
							<div class="mtip__row" v-if="statePopover.hoverItem?.minSpeed != null || statePopover.hoverItem?.maxSpeed != null">
								<el-icon class="mtip__row-ic"><Odometer /></el-icon>
								<span class="mtip__lb">{{ $t('lz.label.zoneInfo.speedRange') }}</span>
								<span class="mtip__val mtip__signs">
									<span
										v-if="statePopover.hoverItem?.minSpeed != null"
										class="mtip__sign mtip__sign--min"
										:title="$t('lz.label.zoneInfo.minSpeed')"
									>{{ statePopover.hoverItem.minSpeed }}</span>
									<span
										v-if="statePopover.hoverItem?.maxSpeed != null"
										class="mtip__sign mtip__sign--max"
										:title="$t('lz.label.zoneInfo.maxSpeed')"
									>{{ statePopover.hoverItem.maxSpeed }}</span>
								</span>
							</div>
						</template>

						<!-- Hover cho equipment -->
						<template v-if="statePopover.hoverItem?.equipment">
							<div class="mtip__row" v-if="statePopover.hoverItem?.equipmentTypeName">
								<el-icon class="mtip__row-ic"><Monitor /></el-icon>
								<span class="mtip__lb">{{ $t('lz.entity.tms.equipmentType') }}</span>
								<span class="mtip__val">{{ statePopover.hoverItem?.equipmentTypeName }}</span>
							</div>
							<div class="mtip__row">
								<el-icon class="mtip__row-ic"><LocationFilled /></el-icon>
								<span class="mtip__lb">{{ $t('lz.label.base.location') }}</span>
								<span class="mtip__val">Km{{ statePopover.hoverItem?.locationKm }}+{{ statePopover.hoverItem?.locationMet }}</span>
							</div>
							<div class="mtip__row">
								<el-icon class="mtip__row-ic"><CircleCheck /></el-icon>
								<span class="mtip__lb">{{ $t('lz.label.base.status') }}</span>
								<span class="mtip__val" :class="'is-' + (statePopover.hoverItem?.equipmentStateValue || 'inactive')">
									{{ getConfigItemByCode(BaseConfigTypeEnum.TmsEquipmentState, statePopover.hoverItem?.equipmentState)?.name || '' }}
								</span>
							</div>
							<!-- Quạt: tần số biến tần + hướng chạy (giả lập, đổi mỗi lần hover) -->
							<template v-if="statePopover.hoverItem?.fan">
								<div class="mtip__row">
									<el-icon class="mtip__row-ic"><Odometer /></el-icon>
									<span class="mtip__lb">{{ $t('lz.label.fan.frequency') }}</span>
									<span class="mtip__val">{{ statePopover.hoverItem.fan.frequency }}</span>
								</div>
								<div class="mtip__row">
									<el-icon class="mtip__row-ic"><Guide /></el-icon>
									<span class="mtip__lb">{{ $t('lz.label.fan.direction') }}</span>
									<span class="mtip__val" :class="statePopover.hoverItem.fan.running ? 'is-active' : 'is-inactive'">{{ statePopover.hoverItem.fan.direction }}</span>
								</div>
							</template>
						</template>

						<!-- Hover cho unit -->
						<template v-if="statePopover.hoverItem?.unit">
							<div class="mtip__row">
								<el-icon class="mtip__row-ic"><Phone /></el-icon>
								<span class="mtip__lb">{{ $t('lz.label.base.contact') }}</span>
								<span class="mtip__val">{{ statePopover.hoverItem?.contact }}</span>
							</div>
							<div class="mtip__row">
								<el-icon class="mtip__row-ic"><InfoFilled /></el-icon>
								<span class="mtip__lb">{{ $t('lz.label.base.info') }}</span>
								<span class="mtip__val">{{ statePopover.hoverItem?.info }}</span>
							</div>
						</template>

						<!-- Hover cho incident trên zone -->
						<template v-if="statePopover.hoverItem?.incident">
							<div class="mtip__row">
								<el-icon class="mtip__row-ic"><WarningFilled /></el-icon>
								<span class="mtip__lb">{{ $t('lz.entity.tmsEvent.eventType') }}</span>
								<span class="mtip__val">{{ statePopover.hoverItem?.eventTypeName }}</span>
							</div>
							<div class="mtip__row" v-if="statePopover.hoverItem?.zoneLocationName || (statePopover.hoverItem?.zoneName && statePopover.hoverItem?.zoneName !== '-')">
								<el-icon class="mtip__row-ic"><MapLocation /></el-icon>
								<span class="mtip__lb">{{ $t('lz.label.tms.zone') }}</span>
								<span class="mtip__val">{{ statePopover.hoverItem?.zoneLocationName || statePopover.hoverItem?.zoneName }}</span>
							</div>

							<div class="mtip__row">
								<el-icon class="mtip__row-ic"><LocationFilled /></el-icon>
								<span class="mtip__lb">{{ $t('lz.label.tms.location') }}</span>
								<span class="mtip__val">Km{{ statePopover.hoverItem?.locationKm }}+{{ statePopover.hoverItem?.locationMet }}</span>
							</div>
							<div class="mtip__row" v-if="statePopover.hoverItem?.direction">
								<el-icon class="mtip__row-ic"><Guide /></el-icon>
								<span class="mtip__lb">{{ $t('lz.entity.tms.direction') }}</span>
								<span class="mtip__val">{{ statePopover.hoverItem?.direction }}</span>
							</div>
							<div class="mtip__row" v-if="statePopover.hoverItem?.startDate">
								<el-icon class="mtip__row-ic"><Clock /></el-icon>
								<span class="mtip__lb">{{ $t('lz.label.base.startDate') }}</span>
								<span class="mtip__val">{{ useDateFormat(statePopover.hoverItem?.startDate, DateTimeFormatEnum.DateTimeDefault) }}</span>
							</div>
							<div class="mtip__row" v-if="statePopover.hoverItem?.trafficPlanName">
								<el-icon class="mtip__row-ic"><Document /></el-icon>
								<span class="mtip__lb">{{ $t('lz.entity.tmsTrafficPlan.name') }}</span>
								<span class="mtip__val">{{ statePopover.hoverItem?.trafficPlanName }}</span>
							</div>
							<div class="mtip__row" v-if="statePopover.hoverItem?.state">
								<el-icon class="mtip__row-ic"><Flag /></el-icon>
								<span class="mtip__lb">{{ $t('lz.entity.tmsIncident.state') }}</span>
								<span class="mtip__val is-bad">{{ statePopover.hoverItem?.state }}</span>
							</div>
						</template>
					</div>
				</div>
			</template>
		</el-popover>
		<editIncidentComp ref="editIncidentCompRef" />
	</div>
</template>

<script setup lang="ts" name="trafficInfo">
import { reactive, onMounted, onUnmounted, ref, computed, defineAsyncComponent, nextTick } from 'vue';
import { LocationFilled, MapLocation, Guide, Clock, Document, Monitor, Phone, InfoFilled, WarningFilled, Flag, CircleCheck, Odometer } from '@element-plus/icons-vue';
import { NextLoading } from '/@/utils/loading';
import ServerTime from '/@/layout/navBars/topBar/serverTime.vue';
import L from "leaflet";
import 'leaflet/dist/leaflet.css';
import '@geoman-io/leaflet-geoman-free';
import '@geoman-io/leaflet-geoman-free/dist/leaflet-geoman.css';
import CenterInfoPanel from './component/centerInfo.vue';
import ControlMap from './component/controlMap.vue';
import LeafletMap from '../mapSample/components/LeafletMap.vue';
import MapLegend from '../mapSample/components/MapLegend.vue';
import MapCompassControl from '../mapSample/components/MapCompassControl.vue';
import UnitModal from './component/unitModal.vue'
import ZoneModal from './component/zoneModal.vue'
import EquipmentModal from './component/equipmentModal.vue'
import { ElPopover } from 'element-plus'
import { useI18n } from 'vue-i18n'
import { useDateFormat, useMouse } from '@vueuse/core'
import { useThemeConfig } from '/@/stores/themeConfig';
import defaultLogo from '/@/assets/logo.png';
import { useBaseConfig } from '/@/stores/baseConfig';
import { useIncidentMonitor } from '/@/stores/its/incidentMonitor';
import { useMapStore } from '/@/stores/its/mapStore';
import { isFanDevice, useFanControlStore } from '/@/stores/its/fanControl';
import { useTransporterVdsZone } from '/@/transporter/hooks/useTransporterVdsZone';
import { BaseConfigTypeEnum, BaseOpConfigGroupCodeEnum, DateTimeFormatEnum, TmsTrafficInfoConfigCodeEnum, TmsZoneConditionEnum } from '/@/types/enums';

const incidentMonitor = useIncidentMonitor();
const mapStore = useMapStore();

const baseConfig = useBaseConfig();
const getConfigItemByCode = baseConfig.getConfigItemByCode;
const getConfigItemName = baseConfig.getConfigItemName;
// Logo đầu trang: logo hệ thống cấu hình từ backend (sysLogo), chưa có thì dùng logo mặc định
const themeConfig = useThemeConfig().themeConfig;
const headLogoUrl = computed(() => themeConfig.logoUrl || defaultLogo);
// Có sự cố đang hiển thị ở đầu trang ⇒ ẩn logo
const hasIncident = ref(false);
const onHasIncident = (value: boolean) => {
	hasIncident.value = !!value;
};
const getConfigDataByCode = baseConfig.getConfigDataByCode;


// Define variable content
const visualizingMap = ref();
const map = ref<L.Map | null>(null);

// Add new ref for selected map ID
const selectedMapId = ref<string>('');
const mapRef = ref<InstanceType<typeof LeafletMap> | null>(null)

// SSE zone báo hiệu cập nhật. LeafletMap tự gọi API
// zoneStatus và đổi style zone hiện có, không render/reload lại toàn bộ bản đồ.
useTransporterVdsZone(() => {
  return mapRef.value?.refreshZoneStatus();
});

// Khai báo để dịch thuật
const { t: $t } = useI18n();

const state = reactive({
  geoJsonDataFromLeaflet: [] as any, // Khai báo biến để lấy dữ liệu geoJsonData - Add by Tuan
  /** Góc xoay hiện tại của bản đồ (độ), đồng bộ từ LeafletMap để vẽ kim la bàn. */
  mapBearing: 0 as number,
  /** Bản đồ đang khóa xoay (config lockRotation) → la bàn chỉ hiển thị. */
  rotationLocked: false as boolean
})

interface TrafficZoneStatus {
  zoneId?: string | null;
  condition?: string | null;
  /** TmsZoneStatus.AverageSpeed (chuỗi, vd "62.5") — trung bình cửa sổ trượt do worker VDS ghi. */
  averageSpeed?: string | null;
  updateTime?: Date | string | null;
  stale?: boolean;
}

// Ngưỡng tính theo SỐ ZONE (không theo %): chỉ vài zone có camera VDS nên tỉ lệ trên tổng zone luôn rất nhỏ.
const DEFAULT_TRAFFIC_ZONE_THRESHOLD = 3;
const trafficZoneStatuses = ref<TrafficZoneStatus[]>([]);
const slowZoneThreshold = ref(DEFAULT_TRAFFIC_ZONE_THRESHOLD);
const congestionZoneThreshold = ref(DEFAULT_TRAFFIC_ZONE_THRESHOLD);
let isTrafficInfoMounted = false;

const readTrafficZoneThreshold = (code: TmsTrafficInfoConfigCodeEnum): number => {
  const rawValue = baseConfig.getOpConfigItemByCode(BaseOpConfigGroupCodeEnum.ItsOperationConfig, code)?.value;
  const parsedValue = Number(rawValue);
  return rawValue !== undefined && rawValue !== null && rawValue !== '' && Number.isInteger(parsedValue) && parsedValue >= 1
    ? parsedValue
    : DEFAULT_TRAFFIC_ZONE_THRESHOLD;
};

const loadTrafficZoneThresholds = async () => {
  try {
    if (!baseConfig.opConfigList || Object.keys(baseConfig.opConfigList).length === 0) {
      await baseConfig.setOpConfigList();
    }
    slowZoneThreshold.value = readTrafficZoneThreshold(TmsTrafficInfoConfigCodeEnum.SlowZoneThreshold);
    congestionZoneThreshold.value = readTrafficZoneThreshold(TmsTrafficInfoConfigCodeEnum.CongestionZoneThreshold);
  } catch {
    slowZoneThreshold.value = DEFAULT_TRAFFIC_ZONE_THRESHOLD;
    congestionZoneThreshold.value = DEFAULT_TRAFFIC_ZONE_THRESHOLD;
  }
};

const handleZoneStatusChange = (statuses: TrafficZoneStatus[]) => {
  trafficZoneStatuses.value = Array.isArray(statuses) ? statuses : [];
};

// Dòng quá hạn đã được bản đồ hạ về normal theo giờ server (useZoneStatus.expireZoneStatuses, chạy mỗi giây)
// nên ở đây chỉ đếm theo condition. Zone nghẽn cũng tính là "ít nhất chậm".
const trafficStatusLabelKey = computed(() => {
  let congestedCount = 0;
  let slowOrWorseCount = 0;

  trafficZoneStatuses.value.forEach((zoneStatus) => {
    if (zoneStatus.stale) return;
    const condition = String(zoneStatus.condition ?? '').trim().toLowerCase();
    if (condition === TmsZoneConditionEnum.Congested) {
      congestedCount += 1;
      slowOrWorseCount += 1;
    } else if (condition === TmsZoneConditionEnum.Slow) {
      slowOrWorseCount += 1;
    }
  });

  // Tắc nghẽn ưu tiên khi đồng thời đạt cả hai ngưỡng.
  if (congestedCount >= congestionZoneThreshold.value) return 'lz.label.base.blockage';
  if (slowOrWorseCount >= slowZoneThreshold.value) return 'lz.label.base.slow';
  return 'lz.label.base.clear';
});



// Begin - Add By Ngọc Thắng
const WeatherComp = defineAsyncComponent(() => import('./component/weatherInfo.vue'));
const IncidentNotificationComp = defineAsyncComponent(() => import('./component/incidentNotification.vue'));
const NotiBell = defineAsyncComponent(() => import('/@/views/notification/component/notiBell.vue'));

import editIncidentComp from '/@/views/tms/incident/component/editIncident.vue';
const editIncidentCompRef = ref<InstanceType<typeof editIncidentComp>>();
// End - Add By Ngọc Thắng

/**
 * Khai báo key lưu trữ storage
 */
const FILTER_STORAGE_KEY = 'traffic-map-active-filters-by-map'

// Lưu toàn bộ filter theo mapId
const filtersByMap = ref<Record<string, Record<string, any>>>({})

// Khai báo biến để filter dữ liệu - Add by Tuan
const activeFilters = ref<Record<string, any>>({})


// Lấy cầu hình tham số trạng thái sự kiện sự cố
const listIncidentStatus = getConfigDataByCode(BaseConfigTypeEnum.TmsIncidentStatus);


/**
 * Hàm lưu filter storage
 */
const saveFiltersToStorage = (data: Record<string, Record<string, any>>) => {
  localStorage.setItem(FILTER_STORAGE_KEY, JSON.stringify(data))
}


/**
 * Hàm load filter from storage
 */
const loadFiltersFromStorage = () => {
  try {
    const raw = localStorage.getItem(FILTER_STORAGE_KEY)
    if (!raw) return {}

    const parsed = JSON.parse(raw)
    if (!parsed || typeof parsed !== 'object') return {}

    // Bộ lọc Khu vực đã bỏ: xoá khoá zone cũ (vd zone: []) để zone luôn hiển thị
    Object.values(parsed).forEach((filters: any) => {
      if (filters && typeof filters === 'object') delete filters.zone
    })
    return parsed
  } catch (error) {
    console.error('loadFiltersFromStorage error:', error)
    return {}
  }
}

onMounted(async () => {
  isTrafficInfoMounted = true;
  NextLoading.done();
  await loadTrafficZoneThresholds();
  if (!isTrafficInfoMounted) return;

  const savedFiltersByMap = loadFiltersFromStorage()
  filtersByMap.value = savedFiltersByMap

  if (selectedMapId.value) {
    activeFilters.value = JSON.parse(
      JSON.stringify(filtersByMap.value[selectedMapId.value] || {})
    )

  }
});

onUnmounted(() => {
	isTrafficInfoMounted = false;
	if (map.value) {
		map.value.remove();
	}
});


/* 
	Begin of header functions
*/


/* 
	End of button functions
*/


/* 
	Begin of map functions
*/
// Xử lý lấy map default khi load trang, ưu tiên map có isDefault = 1, nếu không có thì để trống và chờ người dùng chọn
const handleMapIdChange = async (mapId: string) => {
  // Sau khi thay đổi map, đóng tất cả modal đang mở để tránh lỗi dữ liệu không đồng bộ giữa các map
  handleCloseAll();

  // reset filter
  activeFilters.value = {};
  delete filtersByMap.value[mapId];
  saveFiltersToStorage(filtersByMap.value);

  // reset mapId để component con render lại sạch
  selectedMapId.value = '';
  await nextTick();

  selectedMapId.value = mapId;
};


// Handle zoom in
const handleZoomIn = () => {
	mapRef.value?.zoomIn();
};

// Handle zoom out
const handleZoomOut = () => {
	mapRef.value?.zoomOut();
};

// Handle refresh map
const handleRefreshMap = () => {
  mapRef.value?.handleRefreshTrigger()

  if (selectedMapId.value) {
    const currentId = selectedMapId.value
    const currentFilters = JSON.parse(
      JSON.stringify(filtersByMap.value[currentId] || {})
    )

    selectedMapId.value = ''
    setTimeout(() => {
      selectedMapId.value = currentId
      activeFilters.value = currentFilters
    }, 150)
  }
}

// Handle rotate by delta (drag on rotate button)
const handleRotateByDelta = (delta: number) => {
	mapRef.value?.rotateByDelta?.(delta);
};

// Handle rotate reset (click on rotate button)
const handleRotateReset = () => {
	mapRef.value?.rotateReset?.();
};

// Bấm la bàn: xoay bản đồ về hướng tuyệt đối (0 = Bắc, 180 = Nam)
const handleSetBearing = (bearing: number) => {
	mapRef.value?.setBearing?.(bearing);
};

// Đồng bộ góc xoay hiện tại của bản đồ để la bàn vẽ đúng hướng kim
const handleBearingChange = (bearing: number) => {
	state.mapBearing = bearing;
};

// Initialize map
const initMap = () => {
	map.value = L.map(visualizingMap.value, {
		zoomControl: false
	}).setView([10.8231, 106.6297], 11);

	L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
		attribution: 'Tac Map Server',
	}).addTo(map.value as L.Map);

	map.value.on('zoom', () => {
		const currentZoom = map.value?.getZoom() || 13;
		// Scale ranges from 0.5 (zoomed out) to 1.5 (zoomed in)
		const scale = Math.max(0.5, Math.min(1.5, currentZoom / 13));
		if (visualizingMap.value) {
			visualizingMap.value.style.setProperty('--incident-icon-scale', scale.toString());
		}
	});

	// Trigger initially
	setTimeout(() => {
		map.value?.fire('zoom');
	}, 100);
};

/* 
	End of map functions
*/



// Handle item action to open modal
const handleItemAction = (action: IMapItemAction) => {	
	switch (action.action) {
		case 'click':
			handleItemClick({id: action.itemId, itemGroup: action.itemGroup, name: ''}, action);
			break;
		case 'mouseover':
			handleMouseOver(action)
			break;
		case 'mouseout':
			statePopover.showPopover = false
			break
		case 'dblclick':
			break;
		case 'rightclick':
			break;
	}	
};

/*
	Begin of mouse hover functions
*/


// Add reactive state for popover
const fanStore = useFanControlStore();

const statePopover = reactive({
	showPopover: false,
	virtualRef: { getBoundingClientRect: () => ({}) } as any,
	hoverItem: null as any
})

/** Tốc độ (km/h) dương làm tròn về số nguyên; null/0/không phải số ⇒ null (không hiển thị). */
const toSpeedKmh = (value: unknown): number | null => {
	if (value == null || value === '') return null;
	const speed = Number(value);
	return Number.isFinite(speed) && speed > 0 ? Math.round(speed) : null;
};

// Trạng thái + tốc độ TB REALTIME của zone đang hover, lấy từ snapshot SSE (cùng nguồn tô màu polygon) —
// zone.zoneStatus trong mapStore chỉ nạp một lần khi mở bản đồ nên sẽ cũ. Computed để popover tự cập nhật
// khi snapshot mới về trong lúc đang hover.
// Tốc độ có hạn RIÊNG, dài hơn hạn trạng thái: BE (ZoneSpeedFreshness) đã tự bỏ số khi worker ngừng cập nhật
// quá cửa sổ tính TB, nên ở đây còn số là còn dùng được — trạng thái quá hạn thì chỉ làm mờ (số giữ lại).
const hoverZoneLive = computed(() => {
	const item = statePopover.hoverItem;
	if (item?.action !== 'zone') return null;
	const live = trafficZoneStatuses.value.find((row) => String(row?.zoneId) === String(item.id));
	if (!live) return null;
	return {
		status: live.condition ? getConfigItemName(BaseConfigTypeEnum.TmsZoneStatusColor, live.condition) : null,
		averageSpeed: toSpeedKmh(live.averageSpeed),
		speedHeld: !!live.stale,
	};
});

// Add by Tuan
// Add handleMouseOver function
const handleMouseOver = (action: IMapItemAction) => {
	//console.log("handleMouseOver - action", action)
	const geoJsonData = mapRef.value?.handleGetGeoJsonDataFromLeaflet();
	// Latutide và longitude		
	const itemFromGeoData = geoJsonData?.features?.find((feature:any) => feature?.properties.id ==  action.itemId)			
	const coordinatesArray = itemFromGeoData?.geometry?.coordinates;
	const itemData = action?.itemData || {}
	const incidentData = itemData?.incidentData || {}
	const coordinates = action?.geometry?.coordinates || []
	// Check itemGroup
	switch(action.itemGroup){
		case 'zone':
			const zoneList = mapRef.value?.handleGetZoneListData()			
			const zone = zoneList?.find(zone => zone.id == action.itemId)
			if(zone)
			{
				const zoneCondition = zone?.zoneStatus?.condition ??
									  zone?.additionalInfo?.zoneStatusCondition ??
									  'normal'
				// Update hover item info
				statePopover.hoverItem = {
					id: action.itemId,
					code: zone.code,
					name: zone.name || 'Unknown',
					action: action.itemGroup,
					location: `Km${zone.fromKmNumber}+${zone.fromMetNumber} - Km${zone.toKmNumber}+${zone.toMetNumber}`,
					// Ngưỡng khai riêng cho zone; zone chưa khai thì không hiện biển tương ứng.
					minSpeed: toSpeedKmh(zone.minSpeed),
					maxSpeed: toSpeedKmh(zone.maxSpeed),
				}
				statePopover.hoverItem['status'] = getConfigItemName(BaseConfigTypeEnum.TmsZoneStatusColor, zoneCondition)
			}
			break;
		case 'device':
			const equipmentList = mapRef.value?.handleEquipmentListData()
			const device = equipmentList?.find(equipment => equipment.id == action.itemId) || itemData
			const equipmentTypeId = device?.equipmentTypeId ?? itemData?.equipmentTypeId;
			const sourceCoordinates = coordinatesArray || coordinates;
			// Loại thiết bị tương ứng
			const deviceType = mapStore.equipmentTypeList?.find((t: any) => t?.id == equipmentTypeId);
			// Trạng thái thiết bị (active/inactive/fault) map qua value config equipment_state
			const equipmentState = device?.equipmentState ?? itemData?.equipmentState;
			const deviceStateValue = getConfigItemByCode(BaseConfigTypeEnum.TmsEquipmentState, equipmentState)?.value || 'inactive';

			// Dùng itemData làm fallback để hover vẫn hoạt động với mọi loại thiết bị trong block,
			// kể cả khi equipmentList chưa đồng bộ đầy đủ tại thời điểm hover.
			statePopover.hoverItem = {
				id: action.itemId,
				name: device?.name || itemData?.name || 'Unknown',
				action: action.itemGroup,
				equipment: true,
				equipmentTypeName: deviceType?.name || itemData?.equipmentTypeName || '',
				equipmentState,
				equipmentStateValue: deviceStateValue,
				locationKm: device?.kmNumber ?? itemData?.kmNumber,
				locationMet: device?.metNumber ?? itemData?.metNumber,
				latitude: sourceCoordinates?.[0],
				longitude: sourceCoordinates?.[1]
			}
			// Quạt: đọc tần số + hướng (đang giả lập: lần đầu sinh ngẫu nhiên, sau đó dao động quanh giá trị đặt)
			if (isFanDevice(String(action.itemId))) {
				const fan = fanStore.readTelemetry(String(action.itemId));
				statePopover.hoverItem.fan = {
					frequency: `${fan.frequency.toFixed(1)} Hz`,
					running: fan.mode !== 'stop',
					direction: fan.mode === 'forward'
						? $t('lz.label.fan.directionForward')
						: fan.mode === 'reverse' ? $t('lz.label.fan.directionReverse') : $t('lz.label.fan.directionNone'),
				}
			}
			break;
		case 'unit':
			const unitList = mapRef.value?.handleGetUnitListData();
			const unitListCopy: any[] | undefined = JSON.parse(JSON.stringify(unitList));
			const unit = unitListCopy?.find(unit => unit.id == action.itemId)
			//console.log(unit);
			// Update hover item info
			statePopover.hoverItem = {
				id: action.itemId,
				name: unit.name || 'Unknown',
				action: action.itemGroup,
				unit: true,
				contact: unit?.phone || '-',
				info: unit?.info || '-',
				latitude: coordinatesArray[0],
				longitude: coordinatesArray[1]
			}
			break;

		case 'incident':
			// Khu vực tuyến chính/phụ: lấy theo zone.location (TmsZoneLocation)
			const incidentZone = mapStore.zoneList?.find((z: any) => String(z?.id) === String(itemData?.zoneId));
			const zoneLocationName = incidentZone?.location != null
				? (getConfigItemByCode(BaseConfigTypeEnum.TmsZoneLocation, incidentZone.location + '')?.name || '')
				: '';

			// Hướng: incidentData.direction là mã (code) => map qua TmsDirectionType để ra tên
			const directionName = (incidentData?.direction != null && incidentData?.direction !== '')
				? (getConfigItemByCode(BaseConfigTypeEnum.TmsDirectionType, incidentData.direction + '')?.name || incidentData.direction)
				: '';

			statePopover.hoverItem = {
				id: action.itemId,
				name: itemData?.eventTypeName || $t('lz.label.tmsMapLocation.event'),
				action: 'incident',
				incident: true,

				zoneLocationName: zoneLocationName,
				zoneName: itemData?.zoneName || '-',
				eventTypeName: itemData?.eventTypeName || incidentData?.eventTypeName || '-',
				incidentId: incidentData?.incidentId || '-',
				trafficPlanName: incidentData?.trafficPlanName || '',
				locationKm: incidentData?.kmNumber,
				locationMet: incidentData?.metNumber,
				direction: directionName,
				startDate:
				incidentData?.startDate &&
				incidentData.startDate !== '0001-01-01T00:00:00'
					? incidentData.startDate
					: '-',

				state: listIncidentStatus.find((s : any) => s.code == incidentData.state)?.name || '-',

				latitude: coordinates[1],
				longitude: coordinates[0]
			}
      		break;

		default:
			// Update hover item info
			statePopover.hoverItem = {
				id: action.itemId,
				name: action.itemGroup || 'Unknown',
				action: action.itemGroup
			}
			break;
	}

	//remove popover class
	document.querySelector('.item-info-popover')?.classList.remove('is-light')
	// Hide any existing popover
	statePopover.showPopover = false
	// Update virtual ref with current mouse position
	const { x, y } = useMouse()
	statePopover.virtualRef = {
		getBoundingClientRect: () => ({
			width: 40,
			height: 40,
			top: y.value || 0,
			right: x.value || 0,
			bottom: y.value || 0,
			left: x.value || 0,
		})
	}

	// Show popover
	statePopover.showPopover = true
}

/*
	End of mouse hover functions
*/

/**
 * Filter dữ liệu trên map	
 */
const handleFilterChange = (filters: any) => {
  if (!selectedMapId.value) return

  const clonedFilters = JSON.parse(JSON.stringify(filters))

  activeFilters.value = clonedFilters
  filtersByMap.value[selectedMapId.value] = clonedFilters

  saveFiltersToStorage(filtersByMap.value)
}

const handleGetGeoJsonDataFromLeaflet = (value:any) => {
  if(value){
    state.geoJsonDataFromLeaflet = [...value];   
  } 
}


/*
	Begin of mouse click functions 
*/

import { IMapItemAction } from '/@/types/interfaces';
import { getConfig } from 'vxe-table';


interface Item {
    id: string;
    name: string | null;
	itemGroup: string;
}

interface ActiveModal {
    id: string;
    itemId: string;
    name: string;
}

// Define modal refs
const unitModalRefs = ref<any[]>([])
const zoneModalRefs = ref<any[]>([])
const equipmentModalRefs = ref<any[]>([])

const activeUnitModals = ref<ActiveModal[]>([])
const activeZoneModals = ref<ActiveModal[]>([])
const activeEquipmentModals = ref<ActiveModal[]>([])

const activeItemId = ref<string | null>(null)

// Handle modal activated
const handleModalActivated = (itemId: string) => {
    // Deactivate ALL other modals
    unitModalRefs.value.forEach((modalRef) => {
        if (modalRef.itemId !== itemId) {
            modalRef.setActive(false)
        }
    })
    zoneModalRefs.value.forEach((modalRef) => {
        if (modalRef.itemId !== itemId) {
            modalRef.setActive(false)
        }
    })
	equipmentModalRefs.value.forEach((modalRef) => {
        if (modalRef.itemId !== itemId) {
            modalRef.setActive(false)
        }
    })
    activeItemId.value = itemId
}

// Function to open a modal
const openModal = (modalRefs: any[], activeModals: ActiveModal[], item: Item, data: any) => {
    const existingModalIndex = activeModals.findIndex(
        modal => modal.itemId === item.id
    )

    // Deactivate all modals first
    modalRefs.forEach((modalRef) => {
        modalRef.setActive(false)
    })

    if (existingModalIndex >= 0) {
        // Activate existing modal
        nextTick(() => {
            modalRefs[existingModalIndex].openDialog(data)
            modalRefs[existingModalIndex].setModalId(item.id)
        })
    } else {
        // Create new modal
        activeModals.push({
            id: `modal-${Date.now()}`,
            itemId: item.id,
            name: item.name || ''
        })
        nextTick(() => {
            const lastIndex = modalRefs.length - 1
            modalRefs[lastIndex].openDialog(data)
            modalRefs[lastIndex].setModalId(item.id)
        })
    }
    activeItemId.value = item.id
}

// Open a modal based on itemGroup
const handleItemClick = async (item: Item, data: any) => {
    console.log("[DEBUG] handleItemClick - item.itemGroup:", item.itemGroup, "item.id:", item.id)
    switch (item.itemGroup) {
        case 'zone':
            openModal(zoneModalRefs.value, activeZoneModals.value, item, data)
            break
        case 'unit':
            openModal(unitModalRefs.value, activeUnitModals.value, item, data)
            break
        case 'device':
            openModal(equipmentModalRefs.value, activeEquipmentModals.value, item, data)
            break
        case 'incident':
            if (await incidentMonitor.userCanViewData() === false) return;
            if (item.id) incidentMonitor.selectedIncidentId = item.id;
            incidentMonitor.isShowIncidentPopup = true;
            break
        default:
            console.warn(`[DEBUG] Unknown action: ${item.itemGroup}`)
    }
}

// Close a modal
const handleModalClosed = (itemId: string) => {
    const index = activeUnitModals.value.findIndex(modal => modal.itemId === itemId)
    if (index > -1) {
        activeUnitModals.value.splice(index, 1)
    }
	const indexEquipment = activeEquipmentModals.value.findIndex(modal => modal.itemId === itemId)
	if (indexEquipment > -1) {
		activeEquipmentModals.value.splice(indexEquipment, 1)
	}
	const indexZone = activeZoneModals.value.findIndex(modal => modal.itemId === itemId)
	if (indexZone > -1) {
		activeZoneModals.value.splice(indexZone, 1)
	}

    if (activeItemId.value === itemId) {
        activeItemId.value = null
    }
}

// Update handleCloseOthers and handleCloseAll to handle both modal types
const handleCloseOthers = (currentModalId: string) => {
    [unitModalRefs, zoneModalRefs, equipmentModalRefs].forEach(modalRefs => {
        modalRefs.value.forEach((modalRef) => {
            if (modalRef.itemId !== currentModalId) {
                modalRef.cancel()
            }
        })
    })
    activeUnitModals.value = activeUnitModals.value.filter(
        modal => modal.itemId === currentModalId
    )
    activeZoneModals.value = activeZoneModals.value.filter(
        modal => modal.itemId === currentModalId
    )
	activeEquipmentModals.value = activeEquipmentModals.value.filter(
        modal => modal.itemId === currentModalId
    )
}

const handleCloseAll = () => {
    [unitModalRefs, zoneModalRefs, equipmentModalRefs].forEach(modalRefs => {
        modalRefs.value.forEach((modalRef) => {
            modalRef.cancel()
        })
    })
    activeUnitModals.value = []
    activeZoneModals.value = []
	activeEquipmentModals.value = []
    activeItemId.value = null
}

/*
	End of mouse click functions
*/



</script>

<style scoped lang="scss">
// Width of the left and right charts
$lrWidth: 288px;
// Height of the center info
$centerInfoHeight: 270px;
// Width of the title
$titleWidth: 240px;
// Border color
// $border-color: #27e28b;
// $border-danger-color: #ff0000;
// :root{
	
// }


/* Neon "thở" cho dải accent đáy header */
@keyframes headAccentPulse {
	0%, 100% {
		opacity: 0.75;
		filter: drop-shadow(0 0 5px color-mix(in srgb, var(--border-tac-color) 70%, transparent));
	}
	50% {
		opacity: 1;
		filter: drop-shadow(0 0 12px color-mix(in srgb, var(--border-tac-color) 95%, transparent));
	}
}

/* Neon "thở" nhẹ cho tiêu đề trung tâm */
@keyframes headTitleGlow {
	0%, 100% {
		text-shadow: 0 0 10px color-mix(in srgb, var(--border-tac-color) 70%, transparent), 0 1px 3px rgba(0, 0, 0, 0.5);
	}
	50% {
		text-shadow: 0 0 22px color-mix(in srgb, var(--border-tac-color) 100%, transparent), 0 0 34px color-mix(in srgb, var(--border-tac-color) 55%, transparent), 0 1px 3px rgba(0, 0, 0, 0.5);
	}
}

.visualizing-traffic-info {
	border: none; /* Remove the existing border */
	box-sizing: border-box;
	height: 100%;
	width: 100%;
	position: relative;
	:deep(.BMap_cpyCtrl) {
		display: none;
	}
	:deep(.anchorBL) {
		display: none;
	}

	/* Cụm điều khiển góc trên bên trái (la bàn + chú thích): dưới header 60px.
	   Một khung nổi có viền, cùng phong cách với cụm nút bên phải (controlMap) */
	.map-left-controls {
		opacity: 0.55;
		transition: opacity 0.2s ease;

		&:hover,
		&:focus-within {
			opacity: 1;
		}

		@media (hover: none) {
			opacity: 1;
		}

		@media (prefers-reduced-motion: reduce) {
			transition: none;
		}

		position: absolute;
		top: 80px;
		left: 20px;
		z-index: 1200;
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 5px;
		padding: 5px;
		border: 1px solid var(--el-border-color-light);
		border-radius: 12px;
		background: color-mix(in srgb, var(--el-bg-color-overlay) 92%, transparent);
		backdrop-filter: blur(6px);
		box-shadow: 0 6px 18px rgba(15, 23, 42, 0.18), 0 1px 3px rgba(15, 23, 42, 0.1);

		:deep(.map-compass__rose) {
			max-width: 22px;
			max-height: 22px;
		}

		.map-left-controls__divider {
			display: block;
			width: 22px;
			height: 1px;
			background: var(--el-border-color);
		}

		/* Nút la bàn nằm trong component con nên không nhận scope-id của trang này:
		   dùng :deep để la bàn cùng cỡ/kiểu với nút chú thích bên dưới. */
		.el-button,
		:deep(.map-compass) {
			width: 32px;
			height: 32px;
			margin: 0 !important;
			border-color: transparent;
			background: transparent;
			color: var(--el-text-color-regular);
			font-size: 16px;
			transition: background-color 0.2s, color 0.2s, border-color 0.2s, transform 0.2s;

			&:hover,
			&:focus-visible {
				border-color: var(--el-color-primary-light-5);
				background: var(--el-color-primary-light-9);
				color: var(--el-color-primary);
				transform: scale(1.06);
			}
		}
	}

	.visualizing-container {
		.visualizing-container-head::before {
			content: "";
			position: absolute;
			inset: 0;
			background: var(--el-color-primary); /* color below */
			opacity: 0;
			z-index: -1;
			transition: opacity 0.3s ease;
			}

		.visualizing-container-head:hover::before {
			opacity: 0.14; /* subtle tint on hover */
		}

		/* Dải accent đáy header (fade 2 mép), phát sáng neon "thở" theo màu trạng thái */
		.visualizing-container-head::after {
			content: "";
			position: absolute;
			left: 0;
			right: 0;
			bottom: 0;
			height: 2px;
			z-index: 1;
			background: linear-gradient(to right,
				transparent 0%,
				var(--border-tac-color) 20%,
				var(--border-tac-color) 80%,
				transparent 100%);
			filter: drop-shadow(0 0 6px color-mix(in srgb, var(--border-tac-color) 90%, transparent));
			animation: headAccentPulse 3s ease-in-out infinite;
		}
		
		.visualizing-container-head {
			height: 60px;
			width: 100%;
			position: absolute;
			top: 0;
			left: 0;
			display: flex;
			align-items: center;
			color: #ffffff;
			/* Kính TỐI neon: nền deep-navy trong, viền primary phát sáng, chiều sâu nhiều lớp */
			background:
				radial-gradient(120% 180% at 50% 0%, color-mix(in srgb, var(--border-tac-color) 26%, transparent) 0%, transparent 60%),
				linear-gradient(180deg, rgba(6, 12, 24, 0.72) 0%, rgba(6, 12, 24, 0.46) 100%),
				linear-gradient(to right,
					transparent 0%,
					color-mix(in srgb, var(--border-tac-color) 30%, transparent) 18%,
					color-mix(in srgb, var(--border-tac-color) 30%, transparent) 82%,
					transparent 100%);
			z-index: 500;
			backdrop-filter: blur(14px) saturate(150%);
			box-shadow:
				inset 0 1px 0 rgba(255, 255, 255, 0.2),
				inset 0 -1px 0 color-mix(in srgb, var(--border-tac-color) 45%, transparent),
				0 10px 40px -8px color-mix(in srgb, var(--border-tac-color) 75%, transparent);
			.visualizing-container-head-left {
				/* Logo mặc định: badge sáng để logo màu tối không chìm trên nền header tối; căn giữa theo chiều dọc */
				.head-logo {
					position: absolute;
					z-index: 2;
					left: 30px;
					top: 50%;
					transform: translateY(-50%);
					display: flex;
					align-items: center;
					height: 52px;
					padding: 5px 16px;
					box-sizing: border-box;
					border-radius: 10px;
					background: linear-gradient(180deg, #ffffff 0%, #eef3fa 100%);
					border: 1px solid rgba(255, 255, 255, 0.7);
					box-shadow: 0 6px 18px -6px rgba(0, 0, 0, 0.55), 0 0 0 1px color-mix(in srgb, var(--el-color-primary) 25%, transparent);

					img {
						display: block;
						height: 100%;
						max-width: 260px;
						object-fit: contain;
					}
				}

				.head-logo-fade-enter-active,
				.head-logo-fade-leave-active {
					transition: opacity 0.3s ease;
				}

				.head-logo-fade-enter-from,
				.head-logo-fade-leave-to {
					opacity: 0;
				}
				flex: 1;
				position: relative;
				height: 60px;
				.visualizing-container-head-left-text {
					position: relative;
					z-index: 1;
					font-size: 12px;
					// opacity: 0.6;
					width: 93%;
					padding-left: 15px;
					top: 50%;
					transform: translateY(-100%);
					.visualizing-container-head-left-text-box {

						width: 100%;
						margin-top: 10px;
						position: relative;
						padding-left: 15px;
						// &::after {
						// 	content: '';
						// 	height: 20px;
						// 	width: 100%;
						// 	position: absolute;
						// 	background: linear-gradient(to right, rgba(22, 207, 208, 0.1), rgba(22, 207, 208, 0.3));
						// 	top: -1px;
						// 	left: 0;
						// 	transform: skew(30deg);
						// }
					}
				}
				// &::before,
				// &::after {
				// 	content: '';
				// 	height: 3px;
				// 	background: linear-gradient(-90deg, #16cfd0, transparent);
				// 	position: absolute;
				// 	width: 100%;
				// }
				&::before {
					top: 1px;
					right: 33px;
					opacity: 0.8;
				}
				&::after {
					top: 41px;
					right: -2px;
				}
			}
			.visualizing-container-head-center {
				height: 60px;
				font-size: 18px;
				text-align: center;
				display: flex;
				flex-direction: column;
				position: relative;
				padding: 0 60px;
				background: transparent; /* Remove the radial gradient */
				
				.visualizing-container-head-center-box {
					margin: auto;
					position: relative;
					z-index: 1;
					
					.visualizing-container-head-center-maintitle {
						position: relative;
						color: var(--el-color-white);
						font-weight: 700;
						font-size: 21px;
						margin-bottom: 12px;
						letter-spacing: 2px;
						text-shadow: 0 0 16px color-mix(in srgb, var(--border-tac-color) 90%, transparent), 0 1px 3px rgba(0, 0, 0, 0.4);
						/* Neon "thở" tạo điểm nhấn sống động */
						animation: headTitleGlow 3.4s ease-in-out infinite;

						/* Remove the decorative pseudo elements */
						&::before, &::after {
							display: none;
						}
					}
					
					.visualizing-container-head-center-subtitle {
						font-size: 12px;
						margin-bottom: 6px;
						font-weight: 500;
						text-transform: uppercase;
						letter-spacing: 3px;
						color: rgba(255, 255, 255, 0.82);
						position: relative;
						text-shadow: 0 0 10px color-mix(in srgb, var(--border-tac-color) 55%, transparent);
						/* Remove the decorative pseudo elements */
						&::before, &::after {
							display: none;
						}
					}
					
					/* Remove other decorative elements */
					&::before, &::after {
						display: none;
					}
				}
				&::after {
					content: '';
					width: 200%; /* Độ dài đường kẻ */
					height: 5px; /* Độ dày ở giữa */
					position: absolute;
					top: 50%;
					left: 50%;
					transform: translate(-50%, -50%);
					background: var(--border-tac-color);
					filter: drop-shadow(0 0 8px color-mix(in srgb, var(--border-tac-color) 100%, transparent)) drop-shadow(0 0 16px color-mix(in srgb, var(--border-tac-color) 55%, transparent));
					
					/* Tạo hiệu ứng thu nhỏ mượt mà */
					mask-image: linear-gradient(to right, transparent, var(--border-tac-color) 20%, var(--border-tac-color) 80%, transparent ),
								radial-gradient(circle, var(--border-tac-color) 40%, transparent 70%)!important;
					-webkit-mask-image: linear-gradient(to right, transparent, var(--border-tac-color) 20%, var(--border-tac-color) 80%, transparent),
										radial-gradient(circle, var(--border-tac-color) 40%, transparent 70%);
				}
				/* Remove bottom decorative elements */
				// &::before, &::after {
				// 	display: none;
				// }
			}
			.visualizing-container-head-right {
				flex: 1;
				position: relative;
				height: 60px;
				margin-top: 5px;
				.visualizing-container-head-right-text {
					position: relative;
					z-index: 1;
					font-size: 12px;
					width: 93%;
					float: right;
					text-align: right;
					padding-right: 15px;
					/* Căn giữa header (trừ margin-top 5px của khối cha) cho thẳng hàng logo bên trái */
					top: calc(50% - 5px);
					transform: translateY(-50%);
					display: flex;
					justify-content: flex-end;
					align-items: center;
					gap: 10px;
					.visualizing-container-head-right-text-box {
						width: 100%;
						position: relative;
						padding-right: 15px;
						&::after {
							content: '';
							height: 20px;
							width: 100%;
							position: absolute;
							background: linear-gradient(to left, rgba(22, 207, 208, 0.1), rgba(22, 207, 208, 0.3));
							top: -1px;
							right: 0;
							transform: skew(-30deg);
						}
					}
					.head-right-buttons {
						float: none;
						display: flex;
						gap: 5px;
					}
					/* Thanh công cụ góc phải: 1 khối kính mờ bo tròn gom đồng hồ, chuông, trang chủ, tài khoản */
					.head-toolbar {
						display: flex;
						align-items: center;
						gap: 4px;
						height: 42px;
						padding: 0 5px 0 14px;
						border-radius: 999px;
						background: linear-gradient(180deg, rgba(255, 255, 255, 0.1) 0%, rgba(255, 255, 255, 0.04) 100%), rgba(10, 18, 34, 0.42);
						backdrop-filter: blur(10px);
						border: 1px solid rgba(255, 255, 255, 0.14);
						box-shadow: 0 8px 22px -12px rgba(0, 0, 0, 0.6), inset 0 1px 0 rgba(255, 255, 255, 0.12);
					}

					/* Đồng hồ: icon + giờ server, chữ số đều (tabular) để giây nhảy không làm rung bố cục */
					.head-toolbar__clock {
						display: inline-flex;
						align-items: center;
						gap: 8px;
						padding-right: 6px;
						color: #e8f2ff;

						.head-toolbar__clock-icon {
							font-size: 16px;
							color: var(--el-color-primary-light-3);
						}

						:deep(.server-time) {
							font-family: inherit;
							font-size: 14px;
							font-weight: 600;
							letter-spacing: 0.3px;
							font-variant-numeric: tabular-nums;
							color: #e8f2ff !important;
							white-space: nowrap;
						}
					}

					.head-toolbar__divider {
						width: 1px;
						height: 20px;
						margin: 0 4px;
						background: rgba(255, 255, 255, 0.18);
					}

					/* Chuông: nút tròn cùng cỡ nút trang chủ */
					.head-toolbar__bell {
						:deep(.noti-bell-icon) {
							display: inline-flex;
							align-items: center;
							justify-content: center;
							width: 32px;
							height: 32px;
							padding: 0;
							border-radius: 50%;
							font-size: 17px;
							color: #e8f2ff;
						}
					}
				}
				// &::before,
				// &::after {
				// 	content: '';
				// 	height: 3px;
				// 	background: linear-gradient(90deg, #16cfd0, transparent);
				// 	position: absolute;
				// 	width: 100%;
				// }
				&::before {
					top: 1px;
					left: 33px;
					opacity: 0.8;
				}
				&::after {
					top: 41px;
					left: -2px;
				}
			}
		}
		.visualizing-container-title {
			max-width: $titleWidth;
			font-size: 14px;
			color: rgb(235, 255, 255);
			font-weight: bold;
			opacity: 1;
			padding: 0 5px;
			border-bottom: 1px solid #ffffff;
			border-image: linear-gradient(to right, #ffffff, rgba(22, 207, 208, 0.02)) 1 10;
			position: relative;
			i {
				padding-right: 5px;
				color: orange;
			}
			// &::after {
			// 	content: '';
			// 	position: absolute;
			// 	left: 0;
			// 	bottom: 0;
			// 	width: 1px;
			// 	height: 10px;
			// 	background: linear-gradient(to top, #ffffff, rgba(255, 255, 255, 0.5));
			// }
		}
		.visualizing-container-title-colorful {
			max-width: $titleWidth;
			border: 0;
			padding: 1px;
			background: linear-gradient(135deg, red, orange, green, #16cfd0, purple);
			--mask-image: repeating-linear-gradient(135deg, #000 0px, #000 1px, transparent 1px, transparent 4px);
			-webkit-mask-image: var(--mask-image);
			mask-image: var(--mask-image);
		}
	}

	// Add styles for the map container if necessary
    .leaflet-container {
        position: absolute; // Ensure the map is positioned absolutely
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        z-index: 0; // Set a lower z-index to place it behind other elements
    }

    // Gradient overlay
    &::before,
    &::after {
        display: none;
    }
}

/* Style popover hover map nằm ở block global bên dưới (teleport ra body) */
</style>

<!-- Popover hover teleport ra body => cần style global (neon glass) -->
<style lang="scss">
.item-info-popover.el-popper {
	--mtip-accent: var(--el-color-primary);
	min-width: 220px;
	max-width: 300px;
	padding: 0 !important;
	border-radius: 12px !important;
	border: 1px solid color-mix(in srgb, var(--mtip-accent) 65%, transparent) !important;
	background: linear-gradient(180deg, rgba(20, 28, 44, 0.98), rgba(10, 16, 28, 0.98)) !important;
	box-shadow: 0 18px 44px -12px rgba(0, 0, 0, 0.72),
				0 0 24px -4px color-mix(in srgb, var(--mtip-accent) 65%, transparent),
				inset 0 1px 0 rgba(255, 255, 255, 0.1) !important;
	overflow: hidden;

	/* Sự cố dùng tông đỏ cảnh báo, còn lại tông primary */
	&.mtip--incident {
		--mtip-accent: var(--el-color-danger);
	}

	.el-popper__arrow::before {
		background: rgba(24, 30, 42, 0.98) !important;
		border-color: color-mix(in srgb, var(--mtip-accent) 40%, transparent) !important;
	}

	.mtip__head {
		display: flex;
		align-items: center;
		gap: 8px;
		padding: 10px 14px;
		background: linear-gradient(135deg,
			color-mix(in srgb, var(--mtip-accent) 28%, transparent),
			color-mix(in srgb, var(--mtip-accent) 6%, transparent));
		border-bottom: 1px solid rgba(255, 255, 255, 0.08);
	}

	.mtip__dot {
		flex-shrink: 0;
		width: 8px;
		height: 8px;
		border-radius: 50%;
		background: var(--mtip-accent);
		box-shadow: 0 0 8px color-mix(in srgb, var(--mtip-accent) 70%, transparent);

		&.is-ok,
		&.is-active {
			background: var(--el-color-success);
			box-shadow: 0 0 8px color-mix(in srgb, var(--el-color-success) 70%, transparent);
		}

		&.is-bad,
		&.is-fault {
			background: var(--el-color-danger);
			box-shadow: 0 0 8px color-mix(in srgb, var(--el-color-danger) 70%, transparent);
		}

		&.is-inactive {
			background: var(--el-color-info);
			box-shadow: 0 0 8px color-mix(in srgb, var(--el-color-info) 70%, transparent);
		}
	}

	.mtip__title {
		flex: 1;
		min-width: 0;
		font-size: 13px;
		font-weight: 700;
		color: #fff;
		text-transform: uppercase;
		letter-spacing: 0.3px;
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}

	.mtip__rows {
		display: flex;
		flex-direction: column;
		padding: 8px 14px 12px;
		gap: 7px;
	}

	.mtip__row {
		display: flex;
		align-items: center;
		gap: 8px;
		font-size: 12px;
		line-height: 1.4;
		color: rgba(255, 255, 255, 0.92);
	}

	.mtip__row-ic {
		flex-shrink: 0;
		font-size: 14px;
		color: color-mix(in srgb, var(--mtip-accent) 45%, #fff);
	}

	.mtip__lb {
		flex-shrink: 0;
		min-width: 68px;
		color: rgba(255, 255, 255, 0.55);
	}

	.mtip__val {
		flex: 1;
		min-width: 0;
		text-align: right;
		font-weight: 600;
		word-break: break-word;

		&.is-ok,
		&.is-active {
			color: color-mix(in srgb, var(--el-color-success) 55%, #fff);
		}

		&.is-bad,
		&.is-fault {
			color: color-mix(in srgb, var(--el-color-danger) 45%, #fff);
		}

		&.is-inactive {
			color: color-mix(in srgb, var(--el-color-info) 45%, #fff);
		}
	}

	.mtip__speed {
		white-space: nowrap;
		color: color-mix(in srgb, var(--mtip-accent) 45%, #fff);

		/* Số giữ lại từ lần cập nhật cuối (trạng thái đã quá hạn) */
		&.is-held {
			opacity: 0.55;
			font-style: italic;
		}
	}

	/* Biển báo tốc độ: min = biển xanh chữ trắng, max = biển trắng viền đỏ chữ đen */
	.mtip__signs {
		display: flex;
		justify-content: flex-end;
		gap: 8px;
	}

	.mtip__sign {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		box-sizing: border-box;
		width: 34px;
		height: 34px;
		border-radius: 50%;
		font-size: 13px;
		font-weight: 800;
		line-height: 1;
		font-variant-numeric: tabular-nums;
		box-shadow: 0 2px 6px rgba(0, 0, 0, 0.45);

		&--min {
			background: #1565c0;
			border: 2px solid #fff;
			color: #fff;
		}

		&--max {
			background: #fff;
			border: 4px solid #d32f2f;
			color: #111;
		}
	}
}
</style>
