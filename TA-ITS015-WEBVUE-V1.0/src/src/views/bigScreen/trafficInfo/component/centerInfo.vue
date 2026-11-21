<template>
  <!-- Thu gọn 2 bước:
       1) compact: panel thấp lại, danh sách ~2 dòng mới nhất, camera giữ nguyên kích thước nổi góc phải bản đồ;
       2) minimized: chỉ còn thanh trạng thái (sự kiện mới nhất | số liệu | bản quyền).
       Nội dung không unmount ở cả 2 bước ⇒ luồng camera không phải tải lại. -->
  <div class="visualizing-container-content-center line-center" :class="{ 'collapsed': isCollapsed, 'minimized': isMinimized, 'is-dragging': dragHeight != null }"
       :style="{ background: 'linear-gradient(180deg, #2a415f 0%, #1d3350 100%)', height: dragHeight != null ? dragHeight + 'px' : undefined }">
    <DetailIncident ref="detailIncidentRef" :title="$t('lz.label.base.titleView', { name: $t('lz.label.tmsMapLocation.event') })" operate-type="view" />
    <VdsEventDetail ref="vdsEventDetailRef" />
    <!-- Tay cầm dính mép trên panel: ▲ | 3 chấm mức hiển thị (bấm để nhảy thẳng) | ▼.
         Nút không dùng được thì mờ đi chứ không ẩn ⇒ tay cầm không bị nhảy vị trí. -->
    <div
      class="collapse-handle"
      :class="{ 'is-active': handleActive || dragHeight != null }"
      @pointerdown="onHandlePointerDown"
      @click.capture="onHandleClickCapture"
    >
      <button type="button" class="collapse-handle__btn" :class="{ 'is-disabled': collapseLevel === 0 }" @click="changeCollapseLevel(-1)">
        <el-icon><ArrowUp /></el-icon>
      </button>
      <span class="collapse-handle__dots">
        <span
          v-for="level in [0, 1, 2]"
          :key="level"
          class="collapse-handle__dot"
          :class="{ active: collapseLevel === level }"
          @click="collapseLevel = level as 0 | 1 | 2"
        />
      </span>
      <button type="button" class="collapse-handle__btn" :class="{ 'is-disabled': collapseLevel === 2 }" @click="changeCollapseLevel(1)">
        <el-icon><ArrowDown /></el-icon>
      </button>
    </div>

    <!-- Thanh trạng thái khi thu gọn hết -->
    <div v-if="isMinimized" class="mini-bar">
      <!-- Trái: sự kiện/sự cố mới nhất, bấm để mở lại chế độ compact -->
      <div class="mini-bar__latest" @click="collapseLevel = 1">
        <transition name="mini-fade" mode="out-in">
          <div v-if="latestRow" :key="latestRow._rowKey" class="mini-latest">
            <span class="kind-chip" :class="'kind-chip--' + latestRow._group">
              <span v-if="latestRow._kind === 'live'" class="kind-chip__live" />
              {{ latestRow._group === 'event' ? $t('lz.label.tms.event') : $t('lz.label.tms.tmsIncident') }}
            </span>
            <span class="mini-latest__name" :title="latestRow.name">{{ latestRow.name }}</span>
            <span class="mini-latest__meta">
              {{ $t('lz.entity.tms.km') }} {{ latestRow.kmNumber ?? 0 }} + {{ latestRow.metNumber ?? 0 }}
              · {{ useDateFormat(latestRow._group === 'event' ? new Date(latestRow._time) : latestRow.startDate, 'HH:mm:ss') }}
            </span>
          </div>
        </transition>
      </div>

      <!-- Giữa: số liệu tổng hợp -->
      <div class="compact-stats compact-stats--bar">
        <div class="compact-stat">
          <span class="compact-stat__value">{{ statistics.total }}</span>
          <span class="compact-stat__label">{{ $t('lz.label.tmsIncident.totalIncident') }}</span>
        </div>
        <div class="compact-stat compact-stat--warn">
          <span class="compact-stat__value">{{ statistics.executing }}</span>
          <span class="compact-stat__label">{{ $t('lz.label.tmsIncident.inProgressIncident') }}</span>
        </div>
        <div class="compact-stat compact-stat--new">
          <span class="compact-stat__value">{{ statistics.new }}</span>
          <span class="compact-stat__label">{{ $t('lz.label.tmsIncident.registrationIncident') }}</span>
        </div>
      </div>

      <!-- Phải: bản quyền -->
      <div class="mini-bar__footer">
        <Footer />
      </div>
    </div>

    <div class="visualizing-container-content-center-bottom">
      <!-- Tab dạng segmented gọn, đặt phía trên (ẩn khi thu gọn: compact chỉ hiện tab Giám sát) -->
      <div v-show="!isCollapsed" class="center-tabstrip">
        <button
          v-for="tab in centerTabs"
          :key="tab.key"
          type="button"
          class="center-tab"
          :class="{ active: state.activeCenterTab === tab.key }"
          @click="state.activeCenterTab = tab.key"
        >
          <el-icon class="center-tab-icon"><component :is="tab.icon" /></el-icon>
          <span class="center-tab-label">{{ tab.label }}</span>
        </button>
      </div>

      <!-- Tab: Giám sát (danh sách sự cố + camera cạnh nhau) -->
      <div v-show="isCollapsed || state.activeCenterTab === 'monitor'" class="center-view monitor-view">
        <div class="monitor-flex">
          <!-- Danh sách sự kiện sự cố -->
          <div class="incidents-list">
        <div class="visualizing-container-title">
          <div class="title-left">
            <el-icon class="title-icon title-icon--danger"><WarningFilled /></el-icon>
            <span class="color-white">{{ $t('lz.label.zoneInfo.incidentList') }}</span>
            <span class="title-divider" aria-hidden="true" />
            <el-checkbox-group v-model="selectedGroups" class="incident-group-filter">
              <el-checkbox label="event" class="filter-chip filter-chip--event">{{ $t('lz.label.tmsMapLocation.event') }}</el-checkbox>
              <el-checkbox label="incident" class="filter-chip filter-chip--incident">{{ $t('lz.label.tms.tmsIncident') }}</el-checkbox>
            </el-checkbox-group>
            <span class="title-divider" aria-hidden="true" />
            <!-- Bỏ chọn: lịch sử HISTORY_DAYS ngày gần nhất của cả sự kiện lẫn sự cố, mọi trạng thái. -->
            <el-checkbox v-model="showOngoing" class="incident-ongoing-filter filter-chip filter-chip--live" :title="$t('lz.label.tmsIncident.ongoingHint', { days: HISTORY_DAYS })">
              {{ $t('lz.label.tmsIncident.ongoing') }}
            </el-checkbox>
            <span class="title-divider" aria-hidden="true" />
            <!--refresh button-->
            <el-tooltip :content="$t('lz.button.base.refresh')" placement="top">
              <el-button
                circle
                size="small"
                icon="ele-Refresh"
                class="refresh-btn"
                @click="handleRefreshIncidents"
              />
            </el-tooltip>
          </div>
          <div class="title-right">
            <!-- Thu gọn: số liệu tổng hợp hiển thị gọn ngay trên tiêu đề danh sách -->
            <div v-if="isCollapsed" class="compact-stats">
              <!-- Màn hẹp (≤1680px, vd zoom 125%) CSS ẩn chữ của pill ⇒ rê chuột vào mới hiện tên chỉ số -->
              <el-tooltip :content="$t('lz.label.tmsIncident.totalIncident')" placement="top" :disabled="!isCompactStatLabelHidden">
                <div class="compact-stat">
                  <span class="compact-stat__value">{{ statistics.total }}</span>
                  <span class="compact-stat__label">{{ $t('lz.label.tmsIncident.totalIncident') }}</span>
                </div>
              </el-tooltip>
              <el-tooltip :content="$t('lz.label.tmsIncident.inProgressIncident')" placement="top" :disabled="!isCompactStatLabelHidden">
                <div class="compact-stat compact-stat--warn">
                  <span class="compact-stat__value">{{ statistics.executing }}</span>
                  <span class="compact-stat__label">{{ $t('lz.label.tmsIncident.inProgressIncident') }}</span>
                </div>
              </el-tooltip>
              <el-tooltip :content="$t('lz.label.tmsIncident.registrationIncident')" placement="top" :disabled="!isCompactStatLabelHidden">
                <div class="compact-stat compact-stat--new">
                  <span class="compact-stat__value">{{ statistics.new }}</span>
                  <span class="compact-stat__label">{{ $t('lz.label.tmsIncident.registrationIncident') }}</span>
                </div>
              </el-tooltip>
            </div>
            <incidentButtonsComp @onSubmitSuccess="handleQuery" />
          </div>
        </div>
        <!-- <hr class="visualizing-container-title-colorful" /> -->        
         <div class="incidents-grid-wrap" @mouseenter="freezeLiveRows" @mouseleave="releaseLiveRows">
          <vxe-grid ref="xGridIncident" class="xGrid-style" v-bind="options" :show-header="!isCollapsed" :data="tableRows" :row-class-name="rowClassName" :style="{padding: '0px', color: '#1d3350'}" >
          <!-- Một bảng chung: dòng sự cố (TmsIncident) và dòng sự kiện VDS trực tiếp (SSE), phân biệt bằng cột Loại + viền màu. -->
          <template #empty>
            <!-- Trạng thái rỗng (nội dung trung tính, không khẳng định tình trạng tuyến):
                 - bỏ chọn cả Sự kiện lẫn Sự cố ⇒ nhắc chọn bộ lọc;
                 - đang diễn ra ⇒ "chưa ghi nhận", icon check xanh + vòng sóng lan như radar đang giám sát;
                 - lịch sử ⇒ không có dữ liệu trong N ngày.
                 Thu gọn (compact) hiển thị 1 dòng ngang (icon + tiêu đề + mô tả), căn giữa thân bảng. -->
            <div class="board-empty" :class="{ 'board-empty--compact': isCollapsed, 'board-empty--live': emptyState.live }">
              <span class="board-empty__icon">
                <template v-if="emptyState.live">
                  <span class="board-empty__wave" />
                  <span class="board-empty__wave board-empty__wave--late" />
                </template>
                <el-icon><component :is="emptyState.icon" /></el-icon>
              </span>
              <span class="board-empty__text">
                <span class="board-empty__title">{{ emptyState.title }}</span>
                <span v-if="emptyState.desc" class="board-empty__desc">{{ emptyState.desc }}</span>
              </span>
            </div>
          </template>
          <template #row_kind="{ row }">
            <span class="kind-chip" :class="'kind-chip--' + row._group">
              <span v-if="row._kind === 'live'" class="kind-chip__live" />
              {{ row._group === 'event' ? $t('lz.label.tms.event') : $t('lz.label.tms.tmsIncident') }}
            </span>
          </template>
          <template #row_name="{ row }">
            <!-- span (không phải p) để chữ là nội dung inline của ô ⇒ vxe tự cắt '…' khi dài. -->
            <span v-if="row._group !== 'event'">{{ row.name }}</span>
            <span v-else>{{ row.name }}<span v-if="liveEventSpeed(row) != null" class="live-event-meta"> · {{ liveEventSpeed(row) }} {{ $t('lz.label.tmsTrafficMatrix.speedUnit') }}</span><span v-if="(row.count ?? 0) > 1" class="live-event-meta"> · ×{{ row.count }}</span></span>
          </template>
          <template #row_eventTypeId="{ row }">
              <span class="event-type-cell">
                <span class="event-type-cell__icon">
                  <img :src="media.eventTypeIcon(findIncidentEventType(row, state.eventTypeList))" alt="" loading="lazy" @error="media.onEventTypeIconError" />
                </span>
                <span class="event-type-cell__text">{{ incidentEventTypeDisplay(row, state.eventTypeList) }}</span>
              </span>
          </template>
          <template #row_location="{ row }">
            <p>
              <span>{{ (state.locationList?.find((item: any) => item.code == row.location)?.name ?? row.locationName ?? '') + ' ' }}</span>
              <span>{{ $t('lz.entity.tms.km') }} {{ (row.kmNumber ?? 0) }} + {{ row.metNumber ?? 0 }}</span>
              <span v-if="row.influenceScope != null" class="location-direction">
                ({{ getConfigItemByCode(BaseConfigTypeEnum.TmsDirectionType, row.influenceScope + '')?.name }})
              </span>
            </p>
          </template>
          <template #row_startDate="{ row }">
            <p>{{ useDateFormat(row._group === 'event' ? new Date(row._time) : row.startDate, DateTimeFormatEnum.DateTimeDefault) }}</p>
          </template>

          <template #row_state="{ row }">
            <TagInfo v-if="row._group !== 'event'" :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsIncidentStatus, row.state)" />
            <span v-else class="live-event-meta">—</span>
          </template>
          <template #row_actions="{ row }">
            <!-- Sự kiện VDS (trực tiếp hoặc lịch sử) không có luồng xử lý: chỉ xem chi tiết và xem camera khu vực. -->
            <div v-if="row._group === 'event'" class="display-flex--horizontal flex-justify--center flex-gap--5px action-icons">
              <el-tooltip :content="$t('lz.button.base.view')" placement="top">
                <el-button circle size="small" type="primary" icon="ele-View" v-auth="'tmsIncident:view'" @click="openLiveEventDetail(row)" />
              </el-tooltip>
              <el-tooltip :content="$t('lz.label.tmsIncident.liveCamera')" placement="top">
                <el-button circle size="small" type="info" icon="ele-VideoCamera" v-auth="'tmsIncident:view'" @click="showEventCameras(row)" />
              </el-tooltip>
            </div>
            <!-- Một khối nút dùng chung cho cả sự cố và sự kiện, đúng như luồng TMS ở dev.
                 Không tách theo eventGroup: sự kiện đi cùng luồng xác nhận với sự cố.
                 isIncidentInAccountScope chỉ áp cho nút Hoàn thành (giống dev); các nút còn lại
                 để showActionButton + phân quyền BE quyết định, còn showIncidentPopup vẫn chặn
                 ở tầng cuối nếu hồ sơ không thuộc phạm vi tài khoản. -->
            <div v-else class="display-flex--horizontal flex-justify--center flex-gap--5px action-icons" style="overflow-x: auto;">
              <el-tooltip :content="$t('lz.button.base.view')" placement="top">
                <el-button circle size="small" type="primary" icon="ele-View" v-auth="'tmsIncident:view'" @click="detailIncidentRef?.openDialog(row)" />
              </el-tooltip>

              <el-tooltip :content="$t('lz.button.tmsWorkExecute.executed')" placement="top" v-if="showActionButton(row.state, TmsIncidentStateEnum.Execute, row.id)">
                <el-button circle size="small" type="success" icon="ele-VideoPlay" v-auth="'tmsIncident:view'" @click="showIncidentPopup(row.id, TmsIncidentStateEnum.Execute)" />
              </el-tooltip>

              <el-tooltip :content="$t('lz.button.tmsIncident.verify')" placement="top" v-if="showActionButton(row.state, TmsIncidentStateEnum.Verified)">
                <el-button circle size="small" type="primary" icon="ele-Select" v-auth="'tmsIncident:view'" @click="showIncidentPopup(row.id, TmsIncidentStateEnum.Verified)" />
              </el-tooltip>

              <el-tooltip :content="$t('lz.button.tmsIncident.skip')" placement="top" v-if="showActionButton(row.state, TmsIncidentStateEnum.Skipped)">
                <el-button circle size="small" type="warning" icon="ele-DArrowRight" v-auth="'tmsIncident:view'" @click="showIncidentPopup(row.id, TmsIncidentStateEnum.Skipped)" />
              </el-tooltip>

              <el-tooltip :content="$t('lz.button.tmsIncident.finish')" placement="top" v-if="showActionButton(row.state, TmsIncidentStateEnum.Finished) && isIncidentInAccountScope(row.id)">
                <el-button circle size="small" type="warning" icon="ele-CircleCheck" v-auth="'tmsIncident:view'"  @click="showIncidentPopup(row.id, TmsIncidentStateEnum.Finished)" />
              </el-tooltip>

              <el-tooltip :content="$t('lz.button.base.cancel')" placement="top" v-if="showActionButton(row.state, TmsIncidentStateEnum.Canceled)">
                <el-button circle size="small" type="danger" icon="ele-CircleClose" v-auth="'tmsIncident:view'" @click="showIncidentPopup(row.id, TmsIncidentStateEnum.Canceled)" />
              </el-tooltip>

              <el-tooltip :content="$t('lz.button.tmsIncident.report')" placement="top" v-if="showActionButton(row.state, TmsIncidentStateEnum.Report)">
                <el-button circle size="small" type="info" icon="ele-Document" v-auth="'tmsIncident:view'" @click="openReportPage(row)" />
              </el-tooltip>

              <el-tooltip :content="$t('lz.label.tmsIncident.liveCamera')" placement="top" v-if="showActionButton(row.state, TmsIncidentStateEnum.LiveCam)">
                <el-button circle size="small" type="info" icon="ele-VideoCamera" v-auth="'tmsIncident:view'" @click="showAllCameras(row)" />
              </el-tooltip>

              <el-tooltip :content="$t('lz.button.tmsIncident.authorize')" placement="top" v-if="showActionButton(row.state, TmsIncidentStateEnum.InProgress, row.id)">
                <el-button circle size="small" type="primary" icon="ele-DocumentChecked" v-auth="'tmsIncident:view'" @click="showIncidentPopup(row.id, TmsIncidentStateEnum.InProgress)" />
              </el-tooltip>
            </div>
          </template>
        </vxe-grid>
         </div>
          </div>
          <!-- Camera trực tiếp -->
          <div class="camera-list">
            <div class="visualizing-container-title">
              <div v-if="showActivityFeed" class="title-left">
                <el-icon class="title-icon"><List /></el-icon>
                <span class="color-white">{{ $t('lz.label.tmsActivityFeed.title') }}</span>
              </div>
              <div v-else class="title-left">
                <el-icon class="title-icon"><VideoCamera /></el-icon>
                <span class="color-white">{{ $t('lz.label.tmsIncident.liveCamera') }}</span>
                <span class="subtitle-name" :title="state.selectedIncident?.name">{{ state.selectedIncident?.name }}</span>
              </div>
              <!-- Có camera của sự cố: cho đổi qua lại giữa video và hoạt động gần đây (người theo dõi hoạt động
                   không phải là người xử lý sự cố). Sự cố mới / bấm nút camera của dòng khác ⇒ tự quay về video. -->
              <div v-if="hasCameras" class="title-right">
                <el-tooltip :content="state.feedPinned ? $t('lz.label.tmsIncident.liveCamera') : $t('lz.label.tmsActivityFeed.title')" placement="top">
                  <el-button text class="list-button" @click="toggleFeed">
                    <el-icon><VideoCamera v-if="state.feedPinned" /><List v-else /></el-icon>
                  </el-button>
                </el-tooltip>
              </div>
            </div>
           <!-- <hr class="visualizing-container-title-colorful" /> -->
           <el-tabs 
               :style="{backgroundColor: 'transparent', borderRadius: '3px', padding:'0px', marginTop: '0px', width: '100%', overflow: 'visible'}"
                v-model="state.activeCamTab" 
                class="custom-tabs tab-live-cam"               
                @tab-click="handleLiveCamTabClick"     
                v-if="state.cameraList?.length > 0"
                v-show="!state.feedPinned"
                type="border-card"
              >
                <el-tab-pane v-for="(cam, idx) in state.cameraList" :label="commonVarsAndFuncs.formatCameraName(cam.name, cam.kmNumber, cam.metNumber)" 
                            :name="'camTab' + idx" class="live-cam-parent">
                     <CctvCameraView :ref="el => liveCamRefs[idx as any] = (el as InstanceType<typeof CctvCameraView>)" :equipment="cam" :auto-start="false" />
                 </el-tab-pane>
              </el-tabs>

              <!-- Đang tải camera của dòng vừa chọn: chưa chuyển sang bảng hoạt động để khung không nháy. -->
              <div v-else-if="state.cameraList === null" class="camera-empty">
                <el-icon class="camera-empty__icon"><VideoCamera /></el-icon>
                <span>{{ $t('lz.label.base.loading') }}</span>
              </div>

              <!-- Không có camera: hiện hoạt động gần đây (lịch sử lọc xe; sự kiện thiết bị sau này). -->
              <template v-else>
                <div v-if="state.selectedIncident?.name" class="camera-none-note" :title="state.selectedIncident?.name">
                  <el-icon><VideoCamera /></el-icon>
                  <span>{{ $t('lz.label.tmsActivityFeed.noCameraFor', { name: state.selectedIncident?.name }) }}</span>
                </div>
                <ActivityFeed />
              </template>
              <!-- Người dùng tự chuyển sang hoạt động gần đây trong khi sự cố đang chọn vẫn có camera (tab camera chỉ ẩn). -->
              <ActivityFeed v-if="hasCameras && state.feedPinned" />
          </div>
        </div>
      </div>

      <!-- Tab: Tổng quan - thống kê phân loại xe + lưu lượng + tốc độ theo 2 hướng -->
      <div v-show="!isCollapsed && state.activeCenterTab === 'overview'" class="center-view">
        <StatisticInfo />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { incidentEventTypeDisplay, findIncidentEventType } from '/@/utils/incidentEventTypeDisplay';
import { useItsMediaServerStore } from '/@/stores/its/itsMediaServer';
import { ref, reactive, onMounted, onUnmounted, watch, nextTick, computed, markRaw } from 'vue';
import { logSseDiagnostic } from '/@/utils/fetch-utils';
import { defineAsyncComponent } from 'vue';
import { WarningFilled, VideoCamera, Monitor, DataAnalysis, List, ArrowUp, ArrowDown, CircleCheckFilled, Calendar, Filter } from '@element-plus/icons-vue';
import { VxeGridInstance, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { getAPI } from '/@/utils/axios-utils';
import { TmsIncidentApi, TmsEquipmentApi, TmsZoneEquipmentApi } from '/@/api-services/tms';
import type { PageTmsIncidentOutput } from '/@/api-services/tms';
import type { TmsIncidentMonitorInput } from '/@/api-services/tms';
// Khung view camera dùng chung: tự chọn phát qua CCTV (PTZ + playback) hoặc RTSP.
import CctvCameraView from '/@/components/cctv/CctvCameraView.vue';

import { VxeUI } from 'vxe-table';
import { useI18n } from 'vue-i18n';
import { useDateFormat, useMediaQuery } from '@vueuse/core';

import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { TmsIncidentStateEnum } from '/@/types/enums';
import { DateTimeFormatEnum } from '/@/types/enums/baseEnum';

// Begin - Add by Ngọc Thắng
import { TmsIncidentActionEnum, TmsIncidentConfigCodeEnum, TmsReportRouteEnum } from '/@/types/enums/tmsEnum';

import { useCommonVarsAndFuncs } from '/@/stores/its/commonVarsAndFuncs';
import { useIncidentMonitor } from '/@/stores/its/incidentMonitor';
import { useMapStore } from '/@/stores/its/mapStore';
import { useIncidentSnapshot } from '/@/transporter/hooks/useIncidentSnapshot';
import { useVdsEventSnapshot } from '/@/transporter/hooks/useVdsEventSnapshot';
import type { EventSnapshotItemOutput, PageTrafficDataOutput } from '/@/api-services/vds';
import { VdsTrafficDataApi } from '/@/api-services/vds';
const VdsEventDetail = defineAsyncComponent(() => import('/@/components/vds/VdsEventDetail.vue'));
import { vdsTimestamp } from '/@/utils/vdsTimestamp';
import { useTmsStore } from '/@/stores/tmsStore';
import { ElMessage } from 'element-plus';

const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const getOpConfigItemByCode = useBaseConfig().getOpConfigItemByCode;
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
const incidentButtonsComp = defineAsyncComponent(() => import('./incidentButtons.vue'));
const StatisticInfo = defineAsyncComponent(() => import('./statistic/statisticInfo.vue'));
const ActivityFeed = defineAsyncComponent(() => import('./activityFeed.vue'));
const Footer = defineAsyncComponent(() => import('/@/layout/footer/index.vue'));

const incidentMonitor = useIncidentMonitor();
const commonVarsAndFuncs = useCommonVarsAndFuncs();


// End - Add By Ngọc Thắng
const { t: $t } = useI18n();
const liveCamRefs = ref<InstanceType<typeof CctvCameraView>[]>([]);
VxeUI.setTheme('dark')
/** 0 = mở rộng, 1 = compact (camera + ~2 dòng), 2 = thu gọn hết (chỉ thanh trạng thái). */
const collapseLevel = ref<0 | 1 | 2>(1); // mặc định: compact (camera + ~2 dòng)
const isCollapsed = computed(() => collapseLevel.value === 1);
/* Khớp @media (max-width: 1680px) trong style: lúc đó chữ của pill số liệu bị ẩn ⇒ bật tooltip thay thế. */
const isCompactStatLabelHidden = useMediaQuery('(max-width: 1680px)');
const isMinimized = computed(() => collapseLevel.value === 2);
const xGridIncident = ref<VxeGridInstance>();
const mapStore = useMapStore();
const media = useItsMediaServerStore();

const state = reactive({
  //Begin - Add by Ngọc Thắng
  disableButton: false,
  eventTypeList: [] as any,
  incidentList: [] as any,
  cameraList: [] as any,
  // Người dùng chủ động xem "Hoạt động gần đây" dù sự cố đang chọn có camera; sự cố mới / chọn dòng khác thì bỏ.
  feedPinned: false,
  sourceDetectList: [] as any,
	locationList: [] as any,
	laneList: [] as any,
	zoneList: [] as any,
	incidentStatusList: [] as any,
  selectedIncident: {} as any,
  activeCamTab: null,
  equipmentList: [] as any,
  // Tab đang hiển thị ở khu vực center (monitor | overview | ...)
  activeCenterTab: 'monitor' as string,
  //End - Add by Ngọc Thắng
});

// Cấu hình các tab của khu vực center.
// Mỗi tab là 1 loại nội dung khác nhau; thêm tab mới = thêm 1 entry + 1 <div v-show> tương ứng.
const centerTabs = computed(() => [
  { key: 'monitor', label: $t('lz.label.zoneInfo.incidentList'), icon: markRaw(WarningFilled) },
  // { key: 'events', label: 'Danh sách sự kiện', icon: markRaw(WarningFilled) },
  { key: 'overview', label: $t('lz.label.base.summary'), icon: markRaw(DataAnalysis) },
]);

// Khung camera chuyển sang bảng hoạt động gần đây khi đã tải xong mà không có camera nào.
const hasCameras = computed(() => (state.cameraList?.length ?? 0) > 0);
const showActivityFeed = computed(() => (Array.isArray(state.cameraList) && state.cameraList.length === 0) || (hasCameras.value && state.feedPinned));

const statistics = reactive({
  total: 0,
  executing: 0,
  new: 0
});

const changeCollapseLevel = (step: 1 | -1) => {
  collapseLevel.value = Math.min(2, Math.max(0, collapseLevel.value + step)) as 0 | 1 | 2;
};

// Header bảng ẩn/hiện + chiều cao panel đổi cùng lúc ⇒ ép vxe tính lại sau khi panel đổi xong (có transition 0.3s)
/**
 * Kéo tay cầm lên/xuống: panel cao theo con trỏ, thả ra thì bám về mức gần nhất.
 * Chiều cao mỗi mức phải khớp SCSS: $centerInfoHeight / $compactHeight / $miniHeight.
 */
const LEVEL_HEIGHTS = [300, 162, 52] as const;
const DRAG_THRESHOLD = 5;
/** Kéo tối thiểu bấy nhiêu px là sang mức kế tiếp theo hướng kéo. */
const SNAP_DISTANCE = 24;
const dragHeight = ref<number | null>(null);
let dragStartY = 0;
let dragStartHeight = 0;
let dragMoved = false;
let suppressHandleClick = false;

const onHandlePointerMove = (e: PointerEvent) => {
  const dy = e.clientY - dragStartY;
  if (!dragMoved && Math.abs(dy) < DRAG_THRESHOLD) return;
  dragMoved = true;
  dragHeight.value = Math.min(LEVEL_HEIGHTS[0], Math.max(LEVEL_HEIGHTS[2], dragStartHeight - dy));
};

const onHandlePointerUp = () => {
  window.removeEventListener('pointermove', onHandlePointerMove);
  window.removeEventListener('pointerup', onHandlePointerUp);
  window.removeEventListener('pointercancel', onHandlePointerUp);
  if (!dragMoved || dragHeight.value == null) return;
  // Thả ra: bám về mức có chiều cao gần nhất; nếu vẫn là mức cũ nhưng đã kéo đủ SNAP_DISTANCE thì sang 1 mức theo hướng kéo
  // (không bắt phải kéo quá nửa khoảng cách giữa 2 mức). Chặn click phát sinh ngay sau khi kéo.
  const height = dragHeight.value;
  const startLevel = collapseLevel.value;
  let target = LEVEL_HEIGHTS.reduce((best, h, i) => (Math.abs(h - height) < Math.abs(LEVEL_HEIGHTS[best] - height) ? i : best), 0);
  const delta = height - dragStartHeight; // > 0: kéo lên (mở rộng)
  if (target === startLevel && Math.abs(delta) >= SNAP_DISTANCE) target = startLevel + (delta > 0 ? -1 : 1);
  collapseLevel.value = Math.min(2, Math.max(0, target)) as 0 | 1 | 2;
  dragHeight.value = null;
  suppressHandleClick = true;
  setTimeout(() => (suppressHandleClick = false), 0);
};

const onHandlePointerDown = (e: PointerEvent) => {
  if (e.button !== 0) return;
  // Chặn bôi chọn chữ / kéo ảnh mặc định của trình duyệt khi nắm kéo (không ảnh hưởng click)
  e.preventDefault();
  dragStartY = e.clientY;
  dragStartHeight = LEVEL_HEIGHTS[collapseLevel.value];
  dragMoved = false;
  window.addEventListener('pointermove', onHandlePointerMove);
  window.addEventListener('pointerup', onHandlePointerUp);
  window.addEventListener('pointercancel', onHandlePointerUp);
};

const onHandleClickCapture = (e: MouseEvent) => {
  if (!suppressHandleClick) return;
  e.stopPropagation();
  e.preventDefault();
  suppressHandleClick = false;
};

onUnmounted(() => onHandlePointerUp());

// Tay cầm mặc định mờ; vừa đổi mức thì hiện rõ thêm chốc lát để người dùng thấy phản hồi
const handleActive = ref(false);
let handleActiveTimer: ReturnType<typeof setTimeout> | undefined;
onUnmounted(() => clearTimeout(handleActiveTimer));

watch(collapseLevel, () => {
  handleActive.value = true;
  clearTimeout(handleActiveTimer);
  handleActiveTimer = setTimeout(() => (handleActive.value = false), 1500);
  nextTick(() => xGridIncident.value?.recalculate());
  setTimeout(() => xGridIncident.value?.recalculate(), 320);
});

// Table options
// Id riêng (không dùng chung 'tmsIncidentGrid' với dashboard/trang sự cố cũ): bảng này có thêm cột Loại, dùng chung
// thì cấu hình cột lưu trong localStorage của ba bảng đè lẫn nhau.
const options = useVxeTable<PageTmsIncidentOutput>({
    id: 'bigScreenEventIncidentGrid',
    name: 'TmsIncident',
    columns: [
      // { type: 'checkbox', fixed: 'left', width: 30 },
      { field: '_group', title: $t('lz.entity.base.type'), width: 96, slots: { default: 'row_kind' } },
      { field: 'name', title: $t('lz.entity.base.name'), minWidth: 260, showOverflow: 'tooltip', slots: { default: 'row_name' } },
      { field: 'eventTypeId', title: $t('lz.entity.tmsIncident.eventTypeId'), width: 120, showOverflow: 'tooltip' , slots: { default: 'row_eventTypeId' }},
      { field: 'state', title: $t('lz.entity.base.status'), width: 120, showOverflow: 'tooltip', slots: { default: 'row_state' }},
      { field: 'location', title: $t('lz.label.base.position'), width: 280, showOverflow: 'tooltip', slots: { default: 'row_location' }},
      { field: 'startDate', title: $t('lz.entity.tmsIncident.startDate'), width: 170, showOverflow: 'tooltip', slots: { default: 'row_startDate' } },
      { field: 'Actions', title: $t('lz.label.base.action'), width: 130, showOverflow: 'tooltip', slots: { default: 'row_actions' } }
    ],
  }, 
  {
    // Sọc xen kẽ NHẠT (trắng / xanh xám) chung cho sự kiện lẫn sự cố: sự kiện chèn/hết hạn làm sọc dịch một hàng,
    // nên độ chênh giữ thấp để không nháy cả bảng; loại dòng phân biệt bằng vạch màu bên trái + chip Loại.
    stripe: true,
    // Cấu hình chung bật scrollToTopOnChange: dữ liệu sự kiện đổi vài giây một lần sẽ kéo bảng về đầu liên tục.
    scrollY: { scrollToTopOnChange: false },
    // Khoá dòng ổn định qua mỗi lần SSE đổi dữ liệu; dòng "đang xem camera" tô bằng rowClassName (row--selected)
    // vì current row của vxe mất mỗi lần nạp lại.
    rowConfig: {
      keyField: '_rowKey',
      isCurrent: false,
      isHover: true
    },

    pagerConfig: { enabled: false },
    // Tắt proxy: lưới hiển thị danh sách GỘP (sự cố + sự kiện trực tiếp) qua :data; proxy sẽ ghi đè :data.
    proxyConfig: { enabled: false },

  }
);

// EDIT BY NGỌC THẮNG - 04/03/2025
const DetailIncident = defineAsyncComponent(() => import('/@/views/tms/incident/component/detailIncident.vue'));
const detailIncidentRef = ref<InstanceType<typeof DetailIncident>>();
const selectedGroups = ref(['event', 'incident']);
const incidentStreamQuery = ref<TmsIncidentMonitorInput>({ openIncidentDaysLimit: 7, closedIncidentDaysLimit: 1, includeEvents: true, includeIncidents: true, order: 'desc' });
let pendingStreamList: any;
let listRequestVersion = 0;
let disposed = false;
onUnmounted(() => { disposed = true; ++listRequestVersion; });
let latestListRequest: Promise<any> | undefined;
const listQueryKey = (value: any) => JSON.stringify(Object.entries(value).filter(([,v]) => v != null && v !== '').sort(([a],[b]) => a.localeCompare(b)));
watch(selectedGroups, () => { void handleQuery(); }, { deep: true });
/**
 * Ô "Đang diễn ra": có chọn = sự kiện trực tiếp + sự cố đang theo dõi (monitorlist, SSE) như trước;
 * bỏ chọn = lịch sử HISTORY_DAYS ngày gần nhất của cả sự kiện lẫn sự cố, mọi trạng thái (nạp một lần, nút làm mới để nạp lại).
 */
const showOngoing = ref(true);
watch(showOngoing, () => { void handleQuery(); });
/**
 * Ô "Sự cố" = MỌI hồ sơ TmsIncident (cả loại nhóm event lẫn incident — đã vào TmsIncident là sự cố);
 * ô "Sự kiện" = sự kiện VDS (luồng riêng, không qua monitorlist).
 */
const showIncidents = () => selectedGroups.value.includes('incident');
useIncidentSnapshot(() => showOngoing.value ? incidentStreamQuery.value : null, async snapshot => {
  if (!showOngoing.value) return;
  if (snapshot.includeEvents !== showIncidents() || snapshot.includeIncidents !== showIncidents()) return;
  if (listQueryKey(snapshot.clientQuery) !== listQueryKey(incidentStreamQuery.value)) return;
  pendingStreamList = { list: snapshot, key: listQueryKey(snapshot.clientQuery) };
  try { await loadIncidents(); }
  finally { pendingStreamList = undefined; }
});

// ===== Sự kiện VDS trực tiếp (SSE api/vds/vdsEvent/Stream) — chỉ khi tick "Sự kiện" và "Đang diễn ra".
// Không truyền notify ⇒ không popup.
const { items: liveEvents } = useVdsEventSnapshot(() => showOngoing.value && selectedGroups.value.includes('event') ? {} : null);

/** Sự kiện đổi liên tục (vd quá tốc độ: mỗi xe một dòng) ⇒ chỉ giữ số dòng mới nhất này; sự cố không giới hạn. */
const EVENT_ROW_LIMIT = 50;

/** Chế độ lịch sử: số dòng mới nhất mỗi loại (bảng không phân trang). */
const HISTORY_ROW_LIMIT = 200;
// KienLT - 09/11/2026: số ngày lịch sử lấy theo cấu hình open_incident_days_limit (như chế độ Đang diễn ra) thay cho 3 ngày cố định.
/** Thiếu/sai cấu hình ⇒ 7 ngày (cùng mặc định của monitorlist); BE chặn tối đa 120 ngày. */
const HISTORY_DEFAULT_DAYS = 7;
const HISTORY_MAX_DAYS = 120;
/** Sự kiện VDS đọc QuestDB thô: API api/vds/vdsTrafficData/page chỉ nhận cửa sổ tối đa 60 ngày. */
const VDS_HISTORY_MAX_DAYS = 60;
const HISTORY_DAYS = computed(() => {
  const days = parseInt(getOpConfigItemByCode('operation_config', TmsIncidentConfigCodeEnum.OpenIncidentDaysLimit)?.value);
  return Number.isFinite(days) && days >= 1 ? Math.min(days, HISTORY_MAX_DAYS) : HISTORY_DEFAULT_DAYS;
});

type BoardRow = Record<string, any> & {
  /** incident = hồ sơ TmsIncident (có luồng xử lý); live = sự kiện VDS trực tiếp; history = sự kiện VDS lịch sử (chỉ xem). */
  _kind: 'incident' | 'live' | 'history';
  /** Nhóm hiển thị ở cột Loại: sự kiện (VDS) / sự cố (TmsIncident). */
  _group: 'event' | 'incident';
  _rowKey: string;
  /** Mốc sắp xếp chung (ms). */
  _time: number;
};

const timeOf = (value: unknown): number => {
  const time = value ? new Date(value as any).getTime() : NaN;
  return Number.isFinite(time) ? time : 0;
};

const liveEventName = (item: EventSnapshotItemOutput) =>
  [item.eventTypeName, item.licensePlate || item.equipmentName].filter(Boolean).join(' · ');

/** Tốc độ hiển thị của dòng sự kiện: hành trình dùng tốc độ trung bình, còn lại tốc độ tức thời. */
const liveEventSpeed = (row: any): number | null => {
  const speed = row?.source === 'journey' ? row?.avgSpeedKmh : row?.speed;
  return typeof speed === 'number' && Number.isFinite(speed) ? Math.round(speed * 10) / 10 : null;
};

// Mọi hồ sơ TmsIncident đều là SỰ CỐ, kể cả loại thuộc nhóm event (vd hồ sơ VDS tạo trước 19/10, có cái đang xử lý
// dở) — đủ luồng xử lý. Không trùng dòng sự kiện trực tiếp vì sự kiện trực tiếp chỉ sống vài chục giây.
// Lưu ý: startDate của sự cố là giờ server không kèm múi giờ ⇒ trình duyệt hiểu theo múi giờ máy; đúng khi máy
// vận hành cùng múi giờ server (như cách cột thời gian của bảng này vẫn hiển thị trước đây).
const incidentRows = computed<BoardRow[]>(() => (state.incidentList ?? [])
  .map((row: any) => ({
    ...row,
    _kind: 'incident',
    _group: 'incident',
    _rowKey: 'i:' + row.id,
    _time: timeOf(row.startDate),
  })));

// Chữ ký các dòng sự kiện đang hiển thị: SSE và bộ hết hạn 250ms thay mảng liên tục, kể cả khi 50 dòng hiển thị không
// đổi (vd sự kiện ngoài top 50 hết hạn). Trả lại đúng mảng cũ thì Vue không báo đổi ⇒ lưới không nạp lại vô ích.
// ===== Sự kiện VDS lịch sử (bỏ chọn "Đang diễn ra"): bản ghi thô QuestDB qua api/vds/vdsTrafficData/page (chỉ bản ghi
// có sự kiện), quy về cùng dạng với sự kiện trực tiếp để dùng chung cột, chi tiết (VdsEventDetail) và camera khu vực.
const historyEvents = ref<PageTrafficDataOutput[]>([]);

/** Tên loại theo danh mục TMS; 'unknown' kèm tên AID gốc (cùng cách trang Sự kiện - Sự cố). */
const historyEventTypeName = (row: PageTrafficDataOutput) => {
  const code = String(row.eventType ?? '').trim();
  const findType = (value: string) => (state.eventTypeList ?? []).find((item: any) => String(item?.code ?? '').toLowerCase() === value.toLowerCase());
  if (code.toLowerCase() === 'unknown') {
    const unknown = findType('unknown')?.name || $t('lz.label.base.unknown');
    return row.aidEventType ? `${unknown} (${row.aidEventType})` : unknown;
  }
  return findType(code)?.name || code;
};

const historyEventItem = (row: PageTrafficDataOutput): EventSnapshotItemOutput => {
  const equipment = (state.equipmentList ?? []).find((item: any) => item?.id === row.equipmentId);
  const location = equipment?.location ?? null;
  // Giữ chuỗi giờ gốc (thêm Z nếu thiếu): khoá ảnh QuestDB cần đúng độ chính xác micro giây.
  const detectTime = row.detectTime ? vdsTimestamp(row.detectTime as unknown as string) : undefined;
  return {
    key: `${row.vendorEventId}@${String(row.detectTime)}`,
    source: 'vds',
    vendorEventId: row.vendorEventId as unknown as string,
    detectTime: detectTime as unknown as Date,
    eventTypeCode: row.eventType,
    eventTypeName: historyEventTypeName(row),
    aidEventType: row.aidEventType,
    equipmentId: row.equipmentId,
    equipmentName: equipment?.name ?? null,
    location,
    locationName: location ? getConfigItemByCode(BaseConfigTypeEnum.TmsZoneLocation, String(location))?.name ?? null : null,
    influenceScope: row.direction,
    influenceScopeName: row.direction != null ? getConfigItemByCode(BaseConfigTypeEnum.TmsDirectionType, row.direction + '')?.name ?? null : null,
    kmNumber: row.kmNumber ?? equipment?.kmNumber ?? null,
    metNumber: row.metNumber ?? equipment?.metNumber ?? null,
    lane: row.lane,
    licensePlate: row.licensePlate,
    plateColor: row.plateColor,
    speed: row.speed,
    speedMin: row.speedMin,
    speedMax: row.speedMax,
    count: 1,
    firstDetectTime: detectTime as unknown as Date,
    lastDetectTime: detectTime as unknown as Date,
  };
};

let eventRowsSignature = '';
let eventRowsCache: BoardRow[] = [];
const eventRows = computed<BoardRow[]>(() => {
  const ongoing = showOngoing.value;
  const source = ongoing ? liveEvents.value : historyEvents.value.map(historyEventItem);
  const rows: BoardRow[] = source
    .filter(item => !!item.key)
    .map(item => ({
      ...item,
      _item: item,
      _kind: ongoing ? 'live' as const : 'history' as const,
      _group: 'event' as const,
      _rowKey: (ongoing ? 'e:' : 'h:') + item.key,
      // Giờ VDS là UTC (thiếu hậu tố thì vdsTimestamp thêm Z), giờ sự cố là giờ server ⇒ đều quy về epoch để so.
      _time: item.lastDetectTime ? timeOf(vdsTimestamp(item.lastDetectTime as unknown as string)) : 0,
      name: liveEventName(item),
    }))
    .sort((a, b) => b._time - a._time)
    .slice(0, ongoing ? EVENT_ROW_LIMIT : HISTORY_ROW_LIMIT);
  const signature = rows.map(row => `${row._rowKey}|${row._time}|${row.count ?? ''}|${liveEventSpeed(row) ?? ''}|${row.name}`).join(',');
  if (signature === eventRowsSignature) return eventRowsCache;
  eventRowsSignature = signature;
  return (eventRowsCache = rows);
});

// Đang rê chuột trên bảng ⇒ giữ nguyên các dòng sự kiện đang thấy (không chèn/xoá dòng dưới con trỏ, tránh bấm nhầm
// nút của dòng khác); rời chuột thì áp bản mới nhất. Dòng sự cố vẫn cập nhật bình thường.
const frozenEventRows = ref<BoardRow[] | null>(null);
const freezeLiveRows = () => { frozenEventRows.value = eventRows.value; };
const releaseLiveRows = () => { frozenEventRows.value = null; };

/** Một danh sách, mới nhất trước. Sort ổn định ⇒ sự cố cùng mốc giữ thứ tự BE. */
const tableRows = computed<BoardRow[]>(() =>
  [...incidentRows.value, ...(frozenEventRows.value ?? eventRows.value)].sort((a, b) => b._time - a._time));

/** Nội dung trạng thái rỗng của bảng theo bộ lọc hiện tại. */
const emptyState = computed(() => {
  if (selectedGroups.value.length === 0) {
    return { icon: markRaw(Filter), title: $t('lz.label.tmsIncident.emptyNoFilter'), desc: '', live: false };
  }
  if (showOngoing.value) {
    return { icon: markRaw(CircleCheckFilled), title: $t('lz.label.tmsIncident.emptyOngoingTitle'), desc: $t('lz.label.tmsIncident.emptyOngoingDesc'), live: true };
  }
  return { icon: markRaw(Calendar), title: $t('lz.label.base.noData'), desc: $t('lz.label.tmsIncident.emptyHistoryDesc', { days: HISTORY_DAYS.value }), live: false };
});

/** Dòng mới nhất (sự kiện hoặc sự cố) cho thanh trạng thái khi thu gọn hết. */
const latestRow = computed<BoardRow | undefined>(() => tableRows.value[0]);

/** Dòng đang phát camera ở khung bên phải. */
const selectedRowKey = ref<string>();
const rowClassName = ({ row }: { row: BoardRow }) => [
  'row-group--' + row._group,
  row._kind === 'live' ? 'row-kind--live' : '',
  row._rowKey === selectedRowKey.value ? 'row--selected' : '',
].filter(Boolean).join(' ');

const vdsEventDetailRef = ref<InstanceType<typeof VdsEventDetail>>();
const openLiveEventDetail = (row: any) => vdsEventDetailRef.value?.open(row?._item ?? row);

/**
 * Nạp dữ liệu bảng — thay cho proxy của lưới. Đang diễn ra: sự cố theo monitorlist (REST, hoặc snapshot SSE đang chờ),
 * sự kiện tự đến qua SSE. Lịch sử: nạp một lần cả sự cố lẫn sự kiện HISTORY_DAYS ngày gần nhất.
 */
const loadIncidents = async () => {
  try {
    if (showOngoing.value) {
      historyEvents.value = [];
      await handleQueryApi({} as VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams);
    } else {
      await loadHistory();
    }
  }
  catch (error) { console.error('[centerInfo] loadIncidents', error); }
};

let historyRequest = 0;
/** Lịch sử HISTORY_DAYS ngày gần nhất: sự cố mọi trạng thái + sự kiện VDS, mỗi loại tối đa HISTORY_ROW_LIMIT dòng mới nhất. */
const loadHistory = async () => {
  const request = ++historyRequest;
  ++listRequestVersion; // bỏ kết quả monitorlist còn đang chờ của chế độ "Đang diễn ra"
  const to = new Date();
  const dayMs = 24 * 60 * 60 * 1000;
  const from = new Date(to.getTime() - HISTORY_DAYS.value * dayMs);
  const eventFrom = new Date(to.getTime() - Math.min(HISTORY_DAYS.value, VDS_HISTORY_MAX_DAYS) * dayMs);
  const wantIncidents = selectedGroups.value.includes('incident');
  const wantEvents = selectedGroups.value.includes('event');
  // Sự cố lưu giờ server không múi giờ ⇒ gửi chuỗi giờ địa phương như trang Sự kiện - Sự cố; VDS nhận Date (UTC).
  const serverTime = (value: Date) => useDateFormat(value, DateTimeFormatEnum.DateTimeValidInput).value as unknown as Date;
  const [incidents, events] = await Promise.all([
    wantIncidents
      ? getAPI(TmsIncidentApi).apiTmsTmsincidentPagePost({
          page: 1, pageSize: HISTORY_ROW_LIMIT, field: 'startDate', order: 'desc', descStr: 'desc',
          fromDate: serverTime(from), toDate: serverTime(to), includeEvents: true, includeIncidents: true,
        }).then(res => res?.data?.result?.records ?? [])
          .catch(error => { console.error('[centerInfo] loadHistory incidents', error); return []; })
      : Promise.resolve([]),
    wantEvents
      ? getAPI(VdsTrafficDataApi).apiVdsVdstrafficdataPagePost({
          page: 1, pageSize: HISTORY_ROW_LIMIT, field: 'detectTime', order: 'desc', descStr: 'desc',
          fromDate: eventFrom, toDate: to, eventsOnly: true,
        }).then(res => res?.data?.result?.records ?? [])
          .catch(error => { console.error('[centerInfo] loadHistory events', error); return []; })
      : Promise.resolve([]),
  ]);
  if (request !== historyRequest || showOngoing.value || disposed) return;
  state.incidentList = incidents;
  historyEvents.value = events;
  // Cùng công thức BE (TmsIncidentMonitorOutput): mới = Đăng ký; đang xử lý = Đã xác thực + Đang xử lý.
  statistics.total = incidents.length;
  statistics.new = incidents.filter((row: any) => row?.state == TmsIncidentStateEnum.Registration).length;
  statistics.executing = incidents.filter((row: any) => row?.state == TmsIncidentStateEnum.Verified || row?.state == TmsIncidentStateEnum.InProgress).length;
};
const handleQueryApi = async (sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {

  let closedIncidentDaysLimit = getOpConfigItemByCode('operation_config', TmsIncidentConfigCodeEnum.ClosedIncidentDaysLimit)?.value;
  let openIncidentDaysLimit = getOpConfigItemByCode('operation_config', TmsIncidentConfigCodeEnum.OpenIncidentDaysLimit)?.value; 

  if(!closedIncidentDaysLimit || isNaN(parseInt(closedIncidentDaysLimit)))
    closedIncidentDaysLimit = 1; // Default 1 day

  if(!openIncidentDaysLimit || isNaN(parseInt(openIncidentDaysLimit)))
    openIncidentDaysLimit = 7; // Default 7 day

  const params = Object.assign({
    closedIncidentDaysLimit: Number(closedIncidentDaysLimit),
    openIncidentDaysLimit: Number(openIncidentDaysLimit),
    field: sort.field,
    order: sort.order || 'desc',
    includeEvents: showIncidents(),
    includeIncidents: showIncidents()
  });

  const queryChanged = listQueryKey(incidentStreamQuery.value) !== listQueryKey(params);
  if (JSON.stringify(incidentStreamQuery.value) !== JSON.stringify(params)) incidentStreamQuery.value = params;
  const version = ++listRequestVersion;
  const streamList = pendingStreamList?.key === listQueryKey(params) ? pendingStreamList.list : undefined;
  const connectionId = streamList ? undefined : 'sse-' + Array.from(crypto.getRandomValues(new Uint8Array(16)), byte => byte.toString(16).padStart(2, '0')).join('');
  logSseDiagnostic('incident-list-query', { connectionId, source: streamList ? 'snapshot' : 'rest', queryChanged,
    configReady: Object.keys(useBaseConfig().opConfigList ?? {}).length > 0 });
  const request = streamList ? Promise.resolve({ data: { result: streamList } }) : getAPI(TmsIncidentApi).apiTmsTmsincidentMonitorlistPost(params,
    { headers: { 'X-SSE-Diagnostic-Id': connectionId! } });
  latestListRequest = request;
  let data;
  try { data = await request; }
  catch (error) { if (version !== listRequestVersion) return latestListRequest; throw error; }
  if (version !== listRequestVersion) return latestListRequest;
  if (disposed) return { data: { result: { items: [] } } };
  const result = data?.data?.result;
  state.incidentList = result?.items ?? [];
  autoOpenNewIncident(state.incidentList);

  const summary = result?.statistics;
  statistics.total = summary?.total ?? result?.items?.length ?? 0;
  statistics.executing = summary?.executing ?? 0;
  statistics.new = summary?.new ?? summary?._new ?? 0;

	return  data;
};

const handleRefreshIncidents = () => {
  void handleQuery();
}

// Tourist Stay Duration
const initVisualizingContentCenterBottom = () => {
  // const myChart = echarts.init(visualizingContentCenterBottom.value);
  // const option = {
  //   tooltip: {
  //     trigger: 'axis',
  //     axisPointer: {
  //       type: 'shadow',
  //     },
  //   },
  //   grid: {
  //     top: 26,
  //     right: 10,
  //     bottom: 66,
  //     left: 45,
  //   },
  //   xAxis: {
  //     type: 'value',
  //     axisLabel: {
  //       color: '#16cfd0',
  //     },
  //     splitLine: {
  //       show: true,
  //       lineStyle: {
  //         color: 'rgba(22, 207, 208, 0.3)',
  //       },
  //     },
  //   },
  //   yAxis: {
  //     type: 'category',
  //     axisLabel: {
  //       color: '#16cfd0',
  //     },
  //   },
  //   series: [
  //     {
  //       name: 'Completed',
  //       type: 'bar',
  //       stack: 'total',
  //       label: {
  //         show: true,
  //       },
  //       emphasis: {
  //         focus: 'series',
  //       },
  //       barWidth: 12,
  //       itemStyle: {
  //         label: {
  //           show: true,
  //         },
  //         labelLine: {
  //           show: false,
  //         },
  //         color: new echarts.graphic.LinearGradient(0, 0, 1, 0, [
  //           { offset: 0, color: 'rgba(7,165,255,0.2)' },
  //           { offset: 1, color: 'rgba(7,165,255,1)' },
  //         ]),
  //       },
  //     },
  //     {
  //       name: 'In Progress',
  //       type: 'bar',
  //       stack: 'total',
  //       label: {
  //         show: true,
  //       },
  //       emphasis: {
  //         focus: 'series',
  //       },
  //       barWidth: 12,
  //       itemStyle: {
  //         label: {
  //           show: true,
  //         },
  //         labelLine: {
  //           show: false,
  //         },
  //         color: new echarts.graphic.LinearGradient(0, 0, 1, 0, [
  //           { offset: 0, color: 'rgba(41,244,236,0)' },
  //           { offset: 1, color: 'rgba(41,244,236,1)' },
  //         ]),
  //       },
  //     },
  //   ],
  //   dataset: {
  //     source: [
  //       { status: 'Signed', value1: 33, value2: 93 },
  //       { status: 'In Delivery', value1: 53, value2: 32 },
  //       { status: 'Shipped', value1: 78, value2: 65 },
  //       { status: 'In Procurement', value1: 12, value2: 35 },
  //       { status: 'Order Received', value1: 90, value2: 52 },
  //     ],
  //   },
  // };
  // myChart.setOption(option);
};

// Begin - Add by Ngọc Thắng
const loadDefaultData = async() => {
    state.sourceDetectList = getConfigDataByCode(BaseConfigTypeEnum.TmsSourceDetectIncident);
		state.locationList = getConfigDataByCode(BaseConfigTypeEnum.TmsZoneLocation);
		state.incidentStatusList = getConfigDataByCode(BaseConfigTypeEnum.TmsIncidentStatus);

		// TODO: LaneList is temporarily DirectionList => by direction_config_type not tms_lane
		state.laneList = getConfigDataByCode(BaseConfigTypeEnum.TmsDirectionType);

    if(state.eventTypeList && state.eventTypeList.length < 1)
    {
      if(mapStore.eventTypeList && mapStore.eventTypeList.length < 1)
      {        
           await  mapStore.fetchEventTypeData();        
      }   
      state.eventTypeList = [...mapStore.eventTypeList]
    }

    if(state.equipmentList && state.equipmentList.length < 1)
    {
      if(mapStore.equipmentList && mapStore.equipmentList.length < 1)
      {        
           await  mapStore.fetchEquipmentList();        
      }   
      state.equipmentList = [...mapStore.equipmentList]
    }
};

const showActionButton = (curIncidentState: string, newIncidentState: string, incidentId: string = '') => {


  switch(curIncidentState){
    case TmsIncidentStateEnum.Canceled: // Đã hủy
      return false;

    case TmsIncidentStateEnum.Finished: // Đã kết thúc
      return false;

    case TmsIncidentStateEnum.Skipped: // Đã bỏ qua
      return false;

    case TmsIncidentStateEnum.Registration: // Mới được đăng ký
      if(newIncidentState == TmsIncidentStateEnum.Verified)
        return incidentMonitor.allAllowedActionByAccount?.includes(TmsIncidentActionEnum.Verify);

      // Đã bỏ nhánh Skipped: thao tác "Bỏ qua" thực hiện trong popup, không cần nút riêng ở lưới ====== ChuongLP - 29-07-2026 
      // if(newIncidentState == TmsIncidentStateEnum.Skipped)
      //   return incidentMonitor.allAllowedActionByAccount?.includes(TmsIncidentActionEnum.Skip);

      if(newIncidentState == TmsIncidentStateEnum.LiveCam)
        return true;

      break;

    case TmsIncidentStateEnum.Verified: // Đã được xác thực sự cố
      // if(newIncidentState == TmsIncidentStateEnum.Canceled)
      //   return incidentMonitor.allAllowedActionByAccount?.includes(TmsIncidentActionEnum.Cancel);

      // if(newIncidentState == TmsIncidentStateEnum.Finished)
      //   return incidentMonitor.allAllowedActionByAccount?.includes(TmsIncidentActionEnum.Finish);

      // Nút "Phê duyệt" (áp dụng kịch bản) mở popup để xử lý ====== ChuongLP - 29-07-2026
      // KienLT 08/10/2026: không có mã thao tác riêng cho phê duyệt, nên chỉ hiện khi sự cố nằm trong danh sách
      // popup của tài khoản (ListByAccount: chỉ nhóm giám sát nhận sự cố đã xác thực). Trước đây trả true cho mọi
      // tài khoản ⇒ admin ngoài nhóm giám sát thấy nút nhưng bấm vào bị showIncidentPopup chặn "không có quyền".
      if (newIncidentState == TmsIncidentStateEnum.InProgress)
        return isIncidentInAccountScope(incidentId);
      
      if(newIncidentState == TmsIncidentStateEnum.LiveCam)
        return true;

      break;
    
    case TmsIncidentStateEnum.InProgress: // Đã được áp kịch bản chi tiết
      // console.log("showActionButton", curIncidentState, newIncidentState, incidentId);
      // console.log("allAllowedActionByAccount", incidentMonitor.allAllowedActionByAccount);
      // console.log("allIncidentByAccount0", TmsIncidentActionEnum.Cancel)
      // console.log("allIncidentByAccount1", incidentMonitor.allAllowedActionByAccount?.includes(TmsIncidentActionEnum.Cancel));
      // console.log("allIncidentByAccount2", incidentMonitor.allAllowedActionByAccount?.includes(TmsIncidentActionEnum.Finish));
      // console.log("allIncidentByAccount3", incidentMonitor.allAllowedActionByAccount?.includes(TmsIncidentActionEnum.Report));
      // console.log("allIncidentByAccount4", incidentMonitor.allAllowedActionByAccount?.includes(TmsIncidentActionEnum.Detail));


      // if(newIncidentState == TmsIncidentStateEnum.Canceled){
    
      //   return incidentMonitor.allAllowedActionByAccount?.includes(TmsIncidentActionEnum.Cancel);
      // }

      if(newIncidentState == TmsIncidentStateEnum.Finished)
        return incidentMonitor.allAllowedActionByAccount?.includes(TmsIncidentActionEnum.Finish);

      if(newIncidentState == TmsIncidentStateEnum.Report)
        return incidentMonitor.allAllowedActionByAccount?.includes(TmsIncidentActionEnum.Report);

      if(newIncidentState == TmsIncidentStateEnum.Detail)
        return incidentMonitor.allAllowedActionByAccount?.includes(TmsIncidentActionEnum.Detail);

      if(newIncidentState == TmsIncidentStateEnum.Execute){
        if(incidentMonitor.allIncidentByAccount?.some(item => item?.incident?.id == incidentId && item?.incident?.state == TmsIncidentStateEnum.InProgress && item?.hasWorkExecute == true))
          return incidentMonitor.allAllowedActionByAccount?.includes(TmsIncidentActionEnum.Execute);

        return false;
      }

      if(newIncidentState == TmsIncidentStateEnum.LiveCam)
        return true;

      break;

    default:
      return false;
  }
}

// Sự cố có thuộc danh sách mà tài khoản hiện tại được phép xử lý không
// (đây chính là dữ liệu popup dùng => đảm bảo lưới và popup đồng nhất)
const isIncidentInAccountScope = (incidentId: string) =>
  incidentMonitor.allIncidentByAccount?.some((item: any) => item?.incident?.id === incidentId) ?? false;
  

// targetState: trạng thái đích ứng với nút được bấm (Execute/Verified/InProgress...).
// Popup tự chọn tab theo danh sách chứa incident (forceActiveSelectedIncidentTab),
// nên targetState hiện chỉ mang ý nghĩa ngữ cảnh của nút; nhận vào để chữ ký khớp
// template và tránh "tham số chết". Chỉ mở popup cho incident thuộc phạm vi tài khoản
// (popup chỉ có dữ liệu allIncidentByAccount) — nút đã được ẩn khi ngoài phạm vi,
// guard này là lớp bảo vệ cuối.
const showIncidentPopup = (incidentId: string, _targetState?: string) => {
  if (!isIncidentInAccountScope(incidentId)) {
    ElMessage.warning($t('lz.exception.base.noPermission'));
    return;
  }
  incidentMonitor.selectedIncidentId = incidentId as any;
  if (!incidentMonitor.isShowIncidentPopup)
    incidentMonitor.isShowIncidentPopup = true;
};

const openReportPage = (row: any) =>{
    window.open(TmsReportRouteEnum.IncidentReportDetail + row.id, '_blank');
};

// Reload the incident-list
const handleQuery = async () => {
  options.loading = true;
  try { await loadIncidents(); }
  finally { options.loading = false; }
};

/** Tăng mỗi lần đổi dòng xem camera: lượt tra camera về muộn của dòng trước không được ghi đè dòng sau. */
let cameraRequest = 0;

/** Bật camera đầu tiên của danh sách vừa nạp, tắt các camera khác. */
const startFirstCamera = async (request: number) => {
  await nextTick();
  if (request !== cameraRequest) return;
  // Bỏ ô ref thừa của danh sách camera dài hơn trước đó (tab đã gỡ để lại null).
  liveCamRefs.value.length = state.cameraList?.length ?? 0;
  state.activeCamTab = 'camTab0' as any;
  await nextTick();
  if (request !== cameraRequest) return;
  liveCamRefs.value?.forEach(cam => cam?.stop?.());
  liveCamRefs.value?.[0]?.start?.();
};

/**
 * Camera XEM ĐƯỢC của khu vực có sự kiện. Camera VDS phát hiện sự kiện thường không phát được (không có luồng),
 * nên lấy camera CCTV/PTZ cùng vùng — cùng quy tắc service VDS chọn camera cho sự cố: vùng theo vị trí
 * (tuyến + km + hướng), thiếu thì vùng gắn với chính camera VDS.
 */
const resolveEventCameras = async (row: any): Promise<any[]> => {
  try {
    await commonVarsAndFuncs.getCctvEquipmentTypeList();
    const tmsStore = useTmsStore();
    let zoneIds: string[] = [];
    if (row?.location && row?.kmNumber != null) {
      const zones = await tmsStore.getZonesByPosition({
        location: row.location,
        kmNumber: row.kmNumber,
        metNumber: row.metNumber ?? 0,
        directionId: row.influenceScope ?? undefined,
        isExcludeParent: true,
      });
      zoneIds = (zones ?? []).map((zone: any) => zone?.id).filter(Boolean);
    }
    if (!zoneIds.length && row?.equipmentId) {
      const res = await getAPI(TmsZoneEquipmentApi).apiTmsTmszoneequipmentListGet(undefined, row.equipmentId);
      zoneIds = (res?.data?.result ?? []).map((link: any) => link?.zoneId).filter(Boolean);
    }
    const equipments = await tmsStore.getEquipmentByZones(zoneIds);
    const seen = new Set<string>();
    return (equipments ?? []).filter((equipment: any) => {
      const id = String(equipment?.id ?? '');
      if (!id || seen.has(id) || !commonVarsAndFuncs.isCCTVEquipmentType(equipment?.equipmentTypeId)) return false;
      seen.add(id);
      return true;
    });
  } catch (error) {
    console.error('[centerInfo] resolveEventCameras', error);
    return [];
  }
};

/** Đổi khung phải giữa video sự cố và hoạt động gần đây. Ẩn video thì dừng luồng; hiện lại thì bật camera của tab đang chọn. */
const toggleFeed = async () => {
  state.feedPinned = !state.feedPinned;
  if (state.feedPinned) {
    liveCamRefs.value?.forEach(cam => cam?.stop?.());
    return;
  }
  await nextTick();
  const active = Number(String(state.activeCamTab ?? 'camTab0').replace('camTab', '')) || 0;
  liveCamRefs.value?.[active]?.start?.();
};

/** Id sự cố đã thấy ở các lượt nạp "Đang diễn ra"; null = chưa nạp lần nào (lượt đầu do getLatestIncident lo). */
let knownIncidentIds: Set<string> | null = null;
/**
 * Sự cố MỚI xuất hiện trong danh sách đang diễn ra ⇒ tự mở camera của sự cố đó (kể cả khi người dùng đang xem hoạt động
 * gần đây). Chỉ cộng dồn id đã thấy, không thu hẹp: bỏ tick rồi tick lại ô lọc không làm sự cố cũ thành "mới".
 */
const autoOpenNewIncident = (items: any[]) => {
  const fresh = knownIncidentIds ? items.filter(item => item?.id && !knownIncidentIds!.has(String(item.id))) : [];
  knownIncidentIds ??= new Set<string>();
  items.forEach(item => { if (item?.id) knownIncidentIds!.add(String(item.id)); });
  if (!fresh.length) return;
  const newest = fresh.reduce((a, b) => (timeOf(b?.startDate) > timeOf(a?.startDate) ? b : a));
  void showAllCameras(newest);
};

/** Nút camera của dòng sự kiện trực tiếp: nạp camera khu vực vào khung "Video trực tiếp". */
const showEventCameras = async (row: any) => {
  const request = ++cameraRequest;
  state.feedPinned = false;
  selectedRowKey.value = row?._rowKey;
  state.selectedIncident = { name: row?.name };
  state.cameraList = null;
  const cameras = await resolveEventCameras(row);
  if (request !== cameraRequest) return;
  state.cameraList = cameras;
  await startFirstCamera(request);
};

const showAllCameras = async (incident: any) => {
  const request = ++cameraRequest;
  state.feedPinned = false;
  selectedRowKey.value = incident?.id ? 'i:' + incident.id : undefined;
  state.selectedIncident = incident;
  state.cameraList = null;

  console.log('[centerInfo] showAllCameras - incident:', incident?.id, '| equipmentId:', incident?.equipmentId);

  if ((incident?.equipmentId?.length ?? 0) == 0) {
    console.warn('[centerInfo] showAllCameras - equipmentId rỗng, bỏ qua');
    // Kết thúc trạng thái đang tải; để null thì khung kẹt ở "Đang tải" mãi.
    state.cameraList = [];
    return;
  }

  // Chuyển đổi id: incident.equipmentId có thể lưu id của zoneEquipment (dữ liệu cũ)
  // => map về equipmentId tương ứng trước khi gọi listbyids.
  let resolvedIds = String(incident.equipmentId).split(',').map((x: string) => x.trim()).filter((x: string) => x.length > 0);
  try {
    const zeRes = await getAPI(TmsZoneEquipmentApi).apiTmsTmszoneequipmentListGet();
    const zeMap: Record<string, string> = {};
    (zeRes?.data?.result ?? []).forEach((ze: any) => {
      if (ze?.id && ze?.equipmentId) zeMap[String(ze.id).trim().toLowerCase()] = String(ze.equipmentId);
    });
    resolvedIds = resolvedIds.map((id: string) => zeMap[id.toLowerCase()] ?? id);
    console.log('[centerInfo] showAllCameras - resolvedIds:', resolvedIds);
  } catch (error) {
    console.error('[centerInfo] showAllCameras - load zoneEquipment mapping error:', error);
  }

  // Lấy đúng danh sách thiết bị (camera) liên quan theo equipmentId (đã map) qua listbyids.
  try {
    const res = await getAPI(TmsEquipmentApi).apiTmsTmsequipmentListbyidsGet(resolvedIds.join(','));
    if (request !== cameraRequest) return;
    state.cameraList = res?.data?.result ?? [];
    console.log('[centerInfo] showAllCameras - listbyids trả về:', state.cameraList?.length, 'camera', state.cameraList);
  } catch (error) {
    console.error('showAllCameras - listbyids error, fallback lọc từ danh sách sẵn có:', error);
    if (request !== cameraRequest) return;
    // Fallback: lọc từ danh sách thiết bị đã tải (chuẩn hoá id: bỏ khoảng trắng, không phân biệt hoa/thường)
    const equipmentIds = String(incident.equipmentId)
      .split(',')
      .map((id: string) => id.trim().toLowerCase())
      .filter((id: string) => id.length > 0);
    state.cameraList = (state.equipmentList ?? []).filter((equip: any) => equipmentIds.includes(String(equip.id).trim().toLowerCase()));
    console.log('[centerInfo] showAllCameras - fallback lọc được:', state.cameraList?.length, 'camera');
  }

  // Đợi DOM render danh sách camera, chọn tab đầu và chỉ bật camera đầu tiên.
  await startFirstCamera(request);
}

// Monitoring changes in the incident system => WATCH-SOCKET
const getLatestIncident = async() =>{
    let response = await getAPI(TmsIncidentApi).apiTmsTmsincidentLatestincidentPost();
    state.selectedIncident = response.data.result as any;

    console.log('[centerInfo] getLatestIncident - result:', state.selectedIncident?.length, state.selectedIncident);

    //Lấy danh sách camera liên quan đến sự cố đầu tiên
    if(state.selectedIncident.length > 0){
      console.log('[centerInfo] getLatestIncident - sự cố đầu tiên equipmentId:', state.selectedIncident[0]?.equipmentId);
      showAllCameras(state.selectedIncident[0]);
    } else {
      console.warn('[centerInfo] getLatestIncident - không có sự cố nào');
    }
}

// Lắng nghe sự cố realtime qua mittBus (NATS) => thay cho watch store cũ
// Snapshot updates the table without restarting the selected camera.

// End - Add by Ngọc Thắng

// Add a ref to track the previous tab
const previousTab = ref(null);


// SonTH: Handle live cam tab click
const handleLiveCamTabClick = (pane: any, _ev?: Event) => {
  // Danh sách camera đổi độ dài theo dòng đang xem ⇒ có thể còn ô ref null; bỏ qua thay vì ném lỗi giữa chừng.
  const active = Number(pane.index);
  //Tắt tất cả camera khác
  liveCamRefs.value.forEach((cam, index) => { if (index !== active) cam?.stop?.(); });

  //Bật camera được chọn
  liveCamRefs.value[active]?.start?.();
};

onMounted(async () => {
  // Thay autoLoad của proxy cũ: nạp danh sách sự cố ngay (kèm biểu tượng đang tải), không chờ danh mục.
  void handleQuery();
  // Begin - Add by Ngọc Thắng
  await loadDefaultData();
  await getLatestIncident();
  // End - Add by Ngọc Thắng

  // await initVisualizingContentCenterTop(); COMMENT BY NGỌC THẮNG
  await initVisualizingContentCenterBottom();


});
</script>

<style scoped lang="scss">
.incident-group-filter { display: flex; gap: 6px; flex-wrap: wrap; }
.incident-group-filter :deep(.el-checkbox) { margin-right: 0; color: var(--el-color-white); }
/* Chữ các ô tick trên nền tiêu đề tối: luôn trắng, kể cả khi đã chọn (Element Plus mặc định tô màu primary).
   Không dùng --el-text-color-primary: ở giao diện sáng biến này là màu chữ đậm gần đen. */
.incident-group-filter :deep(.el-checkbox__label),
.incident-ongoing-filter :deep(.el-checkbox__label) { color: var(--el-color-white) !important; }
.incident-ongoing-filter { margin-right: 0; }
/* Tiêu đề danh sách sự cố (nhiều ô lọc) được xuống dòng khi màn hẹp. */
.incidents-list .visualizing-container-title .title-left { flex-wrap: wrap; }
/* Tiêu đề khung camera LUÔN một dòng: tên sự cố dài cắt "…" (title vẫn hiện đủ khi rê chuột).
   Nếu cho xuống dòng, tên bị đẩy xuống dòng 2 và chiếm chỗ của video (khung chỉ cao ~300px). */
.camera-list .visualizing-container-title .title-left { flex-wrap: nowrap; min-width: 0; }
.camera-list .visualizing-container-title .subtitle-name { flex: 1 1 auto; min-width: 0; }
.camera-list .visualizing-container-title .title-left > .color-white { flex: none; white-space: nowrap; }
$lrWidth: 288px;
$centerInfoHeight: 300px;
/* Thu gọn: đủ tiêu đề + ~2 dòng bảng (không header); camera giữ cao gần bằng khi mở rộng */
$compactHeight: 162px;
$compactCameraHeight: 270px;
/* Thu gọn hết: thanh trạng thái */
$miniHeight: 52px;
$titleWidth: 240px;
$titleSolidBackground: var(--el-color-primary);


.line-center{
    border-top: 2px solid var(--border-tac-color);
    border-image: linear-gradient(to right, transparent, var(--border-tac-color) 25%, var(--border-tac-color) 75%, transparent) 1;
    box-shadow: 0 -6px 28px -10px color-mix(in srgb, var(--border-tac-color) 55%, transparent);
}

.visualizing-container-content-center {
  width: 100%;
  height: $centerInfoHeight;
  position: absolute;
  bottom: 0;
  left: 0;
  background: linear-gradient(to right, rgba(22, 207, 208, 0.1), rgba(22, 207, 208, 0.3));
  z-index: 1000;
  transition: transform 0.3s ease, background-color 0.3s ease;
  padding: 10px 0 0 0;

  /* Thu gọn (compact): panel thấp lại thay vì trượt mất; danh sách ~2 dòng mới nhất, camera nổi góc phải */
  &.collapsed {
    height: $compactHeight;
    padding-top: 4px;

    .visualizing-container-content-center-bottom {
      height: 100%;
      bottom: 0;
      /* Lề hẹp để dành chỗ cho bảng (bản quyền chỉ hiện ở thanh thu gọn hết) */
      padding: 2px 6px 6px;
    }

    /* Tiêu đề danh sách luôn 1 dòng để bảng giữ đủ ~2 dòng; màn hẹp thì bỏ chữ của pill số liệu */
    .incidents-list .visualizing-container-title .title-left {
      flex-wrap: nowrap;
      overflow: hidden;
    }

    @media (max-width: 1680px) {
      .compact-stats .compact-stat .compact-stat__label {
        display: none;
      }
    }

    /* Lồng dưới -bottom để thắng specificity của rule mở rộng (.monitor-flex .camera-list) khai báo phía sau */
    .visualizing-container-content-center-bottom .monitor-flex {
      position: relative;
      gap: 6px;

      /* Khung danh sách: bớt đệm, tiêu đề thấp lại (icon/chip/nút nhỏ hơn) ⇒ bảng có thêm chỗ cho đủ 2 dòng */
      .incidents-list {
        padding: 0 6px 3px;

        .visualizing-container-title {
          margin-bottom: 2px;
          padding: 2px 2px 3px;

          .title-left {
            gap: 8px;
          }

          .title-icon {
            width: 22px;
            height: 22px;
            font-size: 13px;
            border-radius: 6px;
          }

          .filter-chip {
            height: 22px;
          }

          .title-divider {
            height: 14px;
          }
        }

        /* Số liệu tổng hợp trên tiêu đề: pill thấp hơn */
        .compact-stats .compact-stat {
          padding: 0 9px;

          .compact-stat__value {
            font-size: 15px;
          }
        }

        /* Nút Thêm sự cố gọn lại cho cùng chiều cao chip */
        .title-right :deep(.el-button) {
          height: 26px;
        }

        /* Dòng bảng thấp hơn khi thu gọn ⇒ hiển thị trọn 2 dòng */
        .xGrid-style {
          --vxe-ui-table-row-height-default: 34px;
          --vxe-ui-table-row-height-medium: 34px;
          --vxe-ui-table-row-height-small: 34px;
          --vxe-ui-table-row-height-mini: 34px;
        }
      }

      /* Camera giữ đúng kích thước như khi mở rộng, neo đáy-phải và tràn lên trên bản đồ */
      .camera-list {
        position: absolute;
        right: 0;
        bottom: 0;
        width: calc(28% - 6px);
        height: $compactCameraHeight;
        padding: 2px 6px 6px;
        z-index: 2;
        background: linear-gradient(160deg, rgba(29, 51, 80, 0.97) 0%, rgba(14, 24, 42, 0.97) 100%);
        box-shadow:
          0 -10px 30px -12px rgba(0, 0, 0, 0.55),
          0 0 18px -6px color-mix(in srgb, var(--el-color-primary) 60%, transparent),
          inset 0 1px 0 rgba(255, 255, 255, 0.08);
      }
    }
  }

  /* Bảng sự cố chiếm phần còn lại dưới tiêu đề (mở rộng giữ tỉ lệ cũ 82%) */
  .incidents-grid-wrap {
    height: 82%;
    min-height: 0;
  }

  &.collapsed .incidents-grid-wrap {
    height: auto;
    flex: 1;
  }

  /* Thu gọn hết: chỉ còn thanh trạng thái; nội dung ẩn bằng display (vẫn mount ⇒ camera không tải lại) */
  &.minimized {
    height: $miniHeight;
    padding-top: 0;

    .visualizing-container-content-center-bottom {
      display: none;
    }
  }

  /* Đang kéo tay cầm: tắt transition để panel bám theo con trỏ, không chọn chữ */
  &.is-dragging {
    transition: none !important;
    user-select: none;
  }

  .mini-bar {
    /* Neo đỉnh với chiều cao cố định: khi kéo panel cao lên, thanh không trôi vào giữa */
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    height: $miniHeight;
    display: grid;
    grid-template-columns: minmax(0, 1fr) auto minmax(0, 1fr);
    align-items: center;
    gap: 16px;
    padding: 0 16px;

    .mini-bar__latest {
      min-width: 0;
      height: 34px;
      display: flex;
      align-items: center;
      padding: 0 10px;
      border-radius: 8px;
      cursor: pointer;
      transition: background-color 0.2s ease;

      &:hover {
        background: rgba(255, 255, 255, 0.08);
      }
    }

    .mini-latest {
      min-width: 0;
      display: flex;
      align-items: center;
      gap: 8px;

      .mini-latest__name {
        min-width: 0;
        color: #e8f2ff;
        font-size: 13px;
        font-weight: 700;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
      }

      .mini-latest__meta {
        flex: none;
        color: rgba(232, 242, 255, 0.6);
        font-size: 12px;
        white-space: nowrap;
      }
    }

    .mini-bar__footer {
      justify-self: end;
      min-width: 0;
      font-size: 11px;
      white-space: nowrap;
      overflow: hidden;

      :deep(.layout-footer) {
        padding-top: 0;
        margin-bottom: 0;
        background: none;

        &::before {
          display: none;
        }
      }

      :deep(.layout-footer-warp) {
        color: rgba(232, 242, 255, 0.5);
      }
    }

    /* Số liệu ở thanh trạng thái: to hơn bản compact một chút */
    .compact-stats--bar {
      margin-right: 0;
      gap: 10px;

      .compact-stat {
        padding: 3px 14px;

        .compact-stat__value {
          font-size: 20px;
        }

        .compact-stat__label {
          font-size: 12px;
        }
      }
    }

    @media (max-width: 1440px) {
      grid-template-columns: minmax(0, 1fr) auto;

      .mini-bar__footer {
        display: none;
      }
    }
  }

  .mini-fade-enter-active,
  .mini-fade-leave-active {
    transition: opacity 0.25s ease, transform 0.25s ease;
  }

  .mini-fade-enter-from {
    opacity: 0;
    transform: translateY(6px);
  }

  .mini-fade-leave-to {
    opacity: 0;
    transform: translateY(-6px);
  }

  .visualizing-container-content-center-bottom {
    width: 100%;
    position: relative;
    height: $centerInfoHeight;
    bottom: 15px;
    display: flex;
    flex-direction: column;
    padding: 6px 10px 0;
    transition: opacity 0.3s ease;

    /* Tab nổi tách riêng phía trên mép panel: mặc định mờ + chỉ icon,
       hover mới rõ và hiện tên -> gọn gàng, không chiếm không gian nội dung */
    .center-tabstrip {
      position: absolute;
      left: 8px;
      top: 0;
      transform: translateY(-100%);
      z-index: 1001;
      display: inline-flex;
      gap: 3px;
      padding: 0px;
      border-radius: 10px 10px 0 0;
      background: rgba(10, 18, 34, 0.5);
      backdrop-filter: blur(8px);
      border: 1px solid rgba(255, 255, 255, 0.12);
      border-bottom: none;
      opacity: 0.42;
      transition: opacity 0.25s ease, background 0.25s ease;

      &:hover {
        opacity: 1;
        background: rgba(10, 18, 34, 0.86);
      }
    }

    .center-tab {
      display: inline-flex;
      align-items: center;
      padding: 6px 9px;
      border: none;
      outline: none;
      background: transparent;
      border-radius: 7px;
      cursor: pointer;
      white-space: nowrap;
      color: rgba(255, 255, 255, 0.85);
      font-size: 12px;
      font-weight: 600;
      letter-spacing: 0.2px;
      transition: background-color 0.2s ease, color 0.2s ease;

      .center-tab-icon {
        font-size: 16px;
        flex: 0 0 auto;
      }

      .center-tab-label {
        max-width: 0;
        opacity: 0;
        overflow: hidden;
        white-space: nowrap;
        transition: max-width 0.25s ease, opacity 0.2s ease, margin-left 0.25s ease;
      }

      &:hover {
        background: rgba(255, 255, 255, 0.12);
      }

      &.active {
        background: var(--el-color-primary);
        color: #fff;
      }
    }

    /* Hover vào cả strip mới bung nhãn */
    .center-tabstrip:hover .center-tab-label {
      max-width: 160px;
      opacity: 1;
      margin-left: 6px;
    }

    /* Vùng nội dung của tab đang chọn */
    .center-view {
      flex: 1;
      min-height: 0;
      width: 100%;
      display: flex;
      flex-direction: column;
      overflow: visible;
    }

    /* Tab Giám sát: danh sách sự cố + camera cạnh nhau (như cũ) */
    .monitor-flex {
      flex: 1;
      min-height: 0;
      display: flex;
      gap: 10px;
      width: 100%;

      .incidents-list {
        width: 72%;
        min-width: 0;
        display: flex;
        flex-direction: column;
        background: linear-gradient(160deg, rgba(20, 34, 58, 0.72) 0%, rgba(10, 18, 32, 0.68) 100%);
        border: 1px solid color-mix(in srgb, var(--el-color-primary) 40%, transparent);
        border-radius: 10px;
        padding: 2px 8px 6px;
        box-shadow:
          0 8px 24px -14px rgba(0, 0, 0, 0.7),
          0 0 16px -6px color-mix(in srgb, var(--el-color-primary) 55%, transparent),
          inset 0 1px 0 rgba(255, 255, 255, 0.08);
      }

      .camera-list {
        width: 28%;
        min-width: 0;
        display: flex;
        flex-direction: column;
        overflow: visible;
        background: linear-gradient(160deg, rgba(20, 34, 58, 0.72) 0%, rgba(10, 18, 32, 0.68) 100%);
        border: 1px solid color-mix(in srgb, var(--el-color-primary) 40%, transparent);
        border-radius: 10px;
        padding: 2px 8px 8px;
        box-shadow:
          0 8px 24px -14px rgba(0, 0, 0, 0.7),
          0 0 16px -6px color-mix(in srgb, var(--el-color-primary) 55%, transparent),
          inset 0 1px 0 rgba(255, 255, 255, 0.08);

        /* Trạng thái rỗng: khung nét đứt tinh tế, đồng bộ tông primary */
        .camera-empty {
          flex: 1;
          min-height: 160px;
          display: flex;
          flex-direction: column;
          align-items: center;
          justify-content: center;
          gap: 10px;
          margin-top: 4px;
          border: 1px dashed color-mix(in srgb, var(--el-color-primary) 28%, transparent);
          border-radius: 8px;
          background: color-mix(in srgb, var(--el-color-primary) 3%, transparent);
          color: var(--el-text-color-secondary);

          .camera-empty__icon {
            font-size: 36px;
            color: color-mix(in srgb, var(--el-color-primary) 55%, transparent);
          }

          span {
            font-size: 12px;
            letter-spacing: 0.3px;
          }
        }

        /* Dòng chọn không có camera: ghi chú một dòng phía trên bảng hoạt động gần đây */
        .camera-none-note {
          display: flex;
          align-items: center;
          gap: 6px;
          margin-top: 4px;
          padding: 4px 8px;
          font-size: 12px;
          color: var(--el-text-color-secondary);
          border-radius: 6px;
          background: color-mix(in srgb, var(--el-color-warning) 10%, transparent);

          span {
            min-width: 0;
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
          }
        }
      }
    }

    /* Empty-state cho tab nội dung khác */
    .tab-placeholder {
      flex: 1;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 12px;
      color: var(--el-text-color-secondary);

      .placeholder-icon {
        font-size: 42px;
        opacity: 0.45;
      }

      span {
        font-size: 14px;
        letter-spacing: 0.5px;
      }
    }
  }

  /* Tay cầm dạng tab dính mép trên panel, cùng tông nền panel */
  .collapse-handle {
    position: absolute;
    top: 0;
    left: 50%;
    transform: translate(-50%, -100%);
    z-index: 1001;
    display: flex;
    align-items: center;
    gap: 2px;
    height: 24px;
    padding: 0 4px;
    border-radius: 10px 10px 0 0;
    background: linear-gradient(180deg, #34507a 0%, #2a415f 100%);
    border: 1px solid rgba(255, 255, 255, 0.14);
    border-bottom: none;
    box-shadow: 0 -6px 16px -8px rgba(0, 0, 0, 0.5);
    /* Vùng nắm vô hình rộng hơn tab (trên + 2 bên) để dễ bắt khi kéo */
    &::before {
      content: '';
      position: absolute;
      inset: -12px -24px 0;
    }

    /* Nút + chấm nằm trên vùng nắm vô hình, nếu không ::before (absolute) sẽ đè và nuốt click */
    .collapse-handle__btn,
    .collapse-handle__dots {
      position: relative;
      z-index: 1;
    }

    /* Nắm kéo lên/xuống (cả cảm ứng) */
    cursor: ns-resize;
    touch-action: none;
    user-select: none;
    /* Mặc định mờ, không giành sự chú ý trên bản đồ; hover / đang thao tác / vừa đổi mức mới rõ */
    opacity: 0.35;
    transition: opacity 0.3s ease, box-shadow 0.3s ease;

    &:hover,
    &:has(:focus-visible), /* chỉ focus bằng bàn phím; click chuột giữ focus nên không dùng :focus-within */
    &.is-active {
      opacity: 1;
      box-shadow: 0 -6px 18px -6px color-mix(in srgb, var(--el-color-primary) 55%, transparent);
    }

    .collapse-handle__btn {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 28px;
      height: 20px;
      padding: 0;
      border: none;
      outline: none;
      border-radius: 6px;
      background: transparent;
      color: #c9d6e8;
      font-size: 14px;
      cursor: pointer;
      transition: background-color 0.2s ease, color 0.2s ease;

      &:hover:not(.is-disabled) {
        background: rgba(255, 255, 255, 0.12);
        color: #fff;
      }

      /* Không dùng thuộc tính disabled: Chrome nuốt sự kiện chuột trên nút disabled ⇒ nắm đúng chỗ đó không kéo được */
      &.is-disabled {
        opacity: 0.3;
        cursor: default;
      }
    }

    .collapse-handle__dots {
      display: inline-flex;
      align-items: center;
      gap: 4px;
      padding: 0 4px;
    }

    .collapse-handle__dot {
      width: 6px;
      height: 6px;
      border-radius: 999px;
      background: rgba(201, 214, 232, 0.4);
      cursor: pointer;
      transition: width 0.25s ease, background-color 0.25s ease;

      &:hover {
        background: rgba(255, 255, 255, 0.8);
      }

      &.active {
        width: 16px;
        background: var(--el-color-primary-light-3);
        box-shadow: 0 0 6px color-mix(in srgb, var(--el-color-primary) 70%, transparent);
      }
    }
  }

  /* Số liệu tổng hợp dạng pill trên tiêu đề danh sách khi thu gọn */
  .compact-stats {
    display: flex;
    align-items: center;
    gap: 6px;
    margin-right: 12px;

    .compact-stat {
      --stat-color: #ffd35c;
      display: inline-flex;
      align-items: baseline;
      gap: 6px;
      padding: 2px 10px;
      border-radius: 999px;
      background: rgba(255, 255, 255, 0.06);
      border: 1px solid color-mix(in srgb, var(--stat-color) 35%, transparent);
      white-space: nowrap;

      &--warn { --stat-color: var(--el-color-danger-light-3); }
      &--new { --stat-color: var(--el-color-primary-light-3); }

      .compact-stat__value {
        font-size: 17px;
        font-weight: 800;
        line-height: 1.2;
        color: var(--stat-color);
      }

      .compact-stat__label {
        font-size: 11px;
        font-weight: 600;
        letter-spacing: 0.2px;
        color: rgba(232, 242, 255, 0.82);
      }
    }
  }

  .footer-wrapper {
    position: absolute;
    left: 20px;
    bottom: 5px;
    z-index: 10;

    :deep(.footer-container) {
      justify-content: flex-start !important;
    }

    :deep(.footer-flex) {
      justify-content: flex-start !important;
    }
  }


  &.solid-background {
    background: $titleSolidBackground!important;
  }
}

.visualizing-container-title {
  margin: 0 0 6px;
  width: 100%;
  font-size: 13px;
  color: #e8f2ff;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.8px;
  opacity: 1;
  padding: 6px 4px;
  /* Đường kẻ đáy dạng gradient mờ dần (thay viền phẳng) - vẽ bằng ::after bên dưới */
  position: relative;
  display: flex;
  justify-content: space-between;
  align-items: center;

  &::after {
    content: '';
    position: absolute;
    left: 0;
    right: 0;
    bottom: 0;
    height: 1px;
    background: linear-gradient(90deg, color-mix(in srgb, var(--el-color-primary) 70%, transparent) 0%, color-mix(in srgb, var(--el-color-primary) 20%, transparent) 45%, transparent 100%);
  }

  /* Bộ lọc dạng chip bật/tắt (thay checkbox): tắt = viền mờ chữ nhạt; bật = nền tint + viền + chấm màu theo loại */
  .filter-chip {
    --chip-color: var(--el-color-primary);
    height: 24px;
    margin-right: 0;
    padding: 0 10px 0 8px;
    border-radius: 999px;
    border: 1px solid rgba(255, 255, 255, 0.16);
    background: rgba(255, 255, 255, 0.04);
    transition: background-color 0.2s ease, border-color 0.2s ease, box-shadow 0.2s ease;

    &:hover {
      border-color: color-mix(in srgb, var(--chip-color) 60%, transparent);
      background: rgba(255, 255, 255, 0.08);
    }

    /* Ẩn ô vuông checkbox, thay bằng chấm màu phía trước nhãn */
    :deep(.el-checkbox__input) {
      position: absolute;
      opacity: 0;
      pointer-events: none;
    }

    :deep(.el-checkbox__label) {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      padding-left: 0;
      font-size: 11.5px;
      font-weight: 700;
      letter-spacing: 0.5px;
      color: rgba(232, 242, 255, 0.55) !important;

      &::before {
        content: '';
        width: 7px;
        height: 7px;
        border-radius: 50%;
        background: transparent;
        box-shadow: inset 0 0 0 1.5px rgba(232, 242, 255, 0.45);
        transition: background-color 0.2s ease, box-shadow 0.2s ease;
      }
    }

    &.is-checked {
      border-color: color-mix(in srgb, var(--chip-color) 70%, transparent);
      background: color-mix(in srgb, var(--chip-color) 18%, transparent);
      box-shadow: 0 0 12px -4px color-mix(in srgb, var(--chip-color) 70%, transparent);

      :deep(.el-checkbox__label) {
        color: #fff !important;

        &::before {
          background: var(--chip-color);
          box-shadow: 0 0 6px var(--chip-color);
        }
      }
    }

    &--event { --chip-color: var(--el-color-warning); }
    &--incident { --chip-color: var(--el-color-danger); }
    &--live { --chip-color: var(--el-color-success); }

    /* Đang diễn ra: chấm nhấp nháy như tín hiệu trực tiếp */
    &--live.is-checked :deep(.el-checkbox__label)::before {
      animation: kind-live-pulse 1.4s ease-in-out infinite;
    }
  }

  .title-left {
    display: flex;
    align-items: center;
    gap: 12px;
    min-width: 0;

    /* Icon tiêu đề dạng badge vuông bo tròn, nền tint theo màu ngữ nghĩa */
    .title-icon {
      --title-icon-color: var(--el-color-primary);
      flex: none;
      width: 26px;
      height: 26px;
      border-radius: 8px;
      font-size: 15px;
      padding: 0 !important;
      color: color-mix(in srgb, var(--title-icon-color) 70%, #fff) !important;
      background: color-mix(in srgb, var(--title-icon-color) 20%, transparent);
      box-shadow: inset 0 0 0 1px color-mix(in srgb, var(--title-icon-color) 40%, transparent), 0 4px 12px -6px var(--title-icon-color);
      margin-right: -2px; /* icon sát chữ tiêu đề, các cụm sau mới cách xa */
    }

    .title-icon--danger {
      --title-icon-color: var(--el-color-danger);
    }

    /* Vạch ngăn dọc giữa tiêu đề / nhóm lọc / Đang diễn ra / nút làm mới */
    .title-divider {
      flex: none;
      width: 1px;
      height: 18px;
      background: rgba(255, 255, 255, 0.14);
    }

    /* Icon cảnh báo cho danh sách sự cố */
    .title-icon--warn {
      --title-icon-color: var(--el-color-warning);
    }

    /* Nút refresh tròn, tông primary nhạt, tinh tế và đồng bộ với toolbar */
    /* Nút làm mới CHỈ ICON: không khung/nền (nút tô primary chìm vào nền tối cùng tông), icon xám trắng; hover icon
       sáng xanh và xoay. Cùng kiểu nút làm mới của "Hoạt động gần đây" (activityFeed.vue). */
    .refresh-btn {
      width: 22px;
      height: 22px;
      padding: 0;
      border: none;
      background: transparent !important;
      box-shadow: none;
      color: #c9d6e8;
      transition: color 0.2s ease;

      :deep(.el-icon) {
        font-size: 15px;
      }

      &:hover,
      &:focus-visible {
        color: var(--el-color-primary-light-3);
      }

      &:hover :deep(.el-icon) {
        transform: rotate(90deg);
        transition: transform 0.3s ease;
      }
    }

    .subtitle-name {
      color: #8ea6c8;
      font-weight: 500;
      font-size: 12px;
      text-transform: none;
      letter-spacing: 0;
      // max-width: 320px;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    i {
      padding-right: 5px;
      color: orange;
    }
  }

  .title-right {
    display: flex;
    align-items: center;

    .list-button {
      color: var(--el-color-primary-light-3);
      padding: 0;

      &:hover {
        color: #ffffff;
      }
    }
  }

  /* // &::after {
  //   content: '';
  //   position: absolute;
  //   left: 0;
  //   bottom: 0;
  //   width: 1px;
  //   height: 10px;
  //   background: linear-gradient(to top, #ffffff, rgba(255, 255, 255, 0.5));
  // } */
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


.float-switch {
		position: fixed;
		bottom: 20px;
		right: 20px;
		z-index: 1200;
		padding: 10px;
		border-radius: 20px;
		background: rgba(0, 0, 0, 0.2);
		backdrop-filter: blur(8px);
		
		:deep(.el-switch) {
			--el-switch-on-color: #13ce66;
			--el-switch-off-color: #8b8b8b;
		}
	}


	.visualizing-container {
		.visualizing-container-content-center {
			/* // Existing gradient background
			//background: linear-gradient(to top, rgba(0, 0, 0, 1), rgba(7, 82, 53, 0.7)); // Bottom gradient */

      background: #fff; // Bottom gradient
			transition: background-color 0.3s ease;

			&.solid-background {
				/* // Solid background when switch is active */
				background: $titleSolidBackground!important;

			}

			.footer-wrapper {
				position: absolute;
				left: 20px;
				bottom: 5px;
				z-index: 10;

				:deep(.footer-container) {
					justify-content: flex-start !important;
				}

				:deep(.footer-flex) {
					justify-content: flex-start !important;
				}
			}

			&.collapsed {
				/* // ... existing collapsed styles ... */
				
				.footer-wrapper {
					display: block;
				}
			}
		}

		/* // Add styles for side footer */
		.side-footer {
			position: fixed;
			left: 5px;
			z-index: 1000;
			color: white;
			opacity: 1;
            top:25px;
			transition: all 0.3s ease;
			
			.collapsed & {
				opacity: 1;
				transform: translateX(0);
			}

			:deep(.footer-container) {
				justify-content: flex-start !important; // Force left alignment
				padding-left: 0 !important;
			}

			:deep(.footer-flex) {
				justify-content: flex-start !important;
				margin-left: 0 !important;
			}
		}
	}

	.visualizing-container {
		.visualizing-container-content-center {
			transition: transform 0.3s ease, height 0.3s ease;
			transform-origin: bottom;

		}
	}

	:deep(.el-button) {
		transition: all 0.3s ease;
	}

  /* ================================================================
     Lưới sự cố: sọc caro TRẮNG / XANH ĐẬM, chữ tương phản ngược nền từng hàng.
     - Hàng lẻ (nền trắng)      -> chữ xanh đậm
     - Hàng chẵn (nền xanh đậm) -> chữ trắng
     - Hover: luôn về nền TRẮNG -> chữ xanh
     Biến màu gom về 1 chỗ cho dễ chỉnh.
     ================================================================ */
  $grid-navy: #1d3350;      /* xanh đậm */
  $grid-white: #ffffff;     /* trắng */
  $grid-current: #dbe7fb;   /* hàng đang chọn: xanh rất nhạt */
  $grid-line: rgba(120, 140, 170, 0.35);
  $kind-event: var(--el-color-warning);     /* sự kiện: cam (cùng màu icon sự kiện trên bản đồ) */
  $kind-incident: var(--el-color-danger);  /* sự cố: đỏ */
  $grid-stripe: #eef3fa;    /* hàng sọc (sự kiện lẫn sự cố): xanh xám rất nhạt */

  /* Nhãn cột Loại: nền đặc + chữ trắng ⇒ đọc được trên cả hàng trắng lẫn hàng sọc xanh đậm */
  .kind-chip {
    display: inline-flex;
    align-items: center;
    gap: 4px;
    padding: 1px 8px;
    border-radius: 10px;
    font-size: 12px;
    font-weight: 600;
    line-height: 18px;
    white-space: nowrap;
    color: var(--el-color-white) !important;
    &--event { background-color: $kind-event; }
    &--incident { background-color: $kind-incident; }
  }
  /* Chấm nhấp nháy: dòng sự kiện VDS trực tiếp (đổi liên tục, không có luồng xử lý) */
  .kind-chip__live {
    width: 6px;
    height: 6px;
    border-radius: 50%;
    background-color: var(--el-color-white);
    animation: kind-live-pulse 1.4s ease-in-out infinite;
  }
  @keyframes kind-live-pulse {
    0%, 100% { opacity: 1; }
    50% { opacity: 0.25; }
  }
  .live-event-meta { opacity: 0.75; }

  /* Cột loại sự cố: icon tròn theo danh mục + tên (tự cắt '…' khi dài) */
  .event-type-cell {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    max-width: 100%;
    vertical-align: middle;
  }
  .event-type-cell__icon {
    flex: 0 0 auto;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 26px;
    height: 26px;
    border-radius: 50%;
    background: color-mix(in srgb, var(--el-color-primary) 14%, transparent);
    img { width: 19px; height: 19px; object-fit: contain; }
  }
  .event-type-cell__text {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  /* ===== Trạng thái rỗng của bảng: icon tròn phát sáng + vòng sóng lan, tiêu đề + mô tả ===== */
  .board-empty {
    /* Đang diễn ra: xanh lá (như bản trước); lọc rỗng / lịch sử: primary */
    --empty-color: var(--el-color-primary);

    &--live {
      --empty-color: var(--el-color-success);
    }
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 12px;
    padding: 12px 0;

    .board-empty__icon {
      position: relative;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 56px;
      height: 56px;
      border-radius: 50%;
      font-size: 28px;
      color: var(--empty-color);
      background: radial-gradient(circle, color-mix(in srgb, var(--empty-color) 22%, transparent) 0%, color-mix(in srgb, var(--empty-color) 6%, transparent) 70%);
      box-shadow: inset 0 0 0 1px color-mix(in srgb, var(--empty-color) 45%, transparent), 0 0 24px -6px var(--empty-color);
    }

    /* 2 vòng sóng lan lệch pha như tín hiệu radar "đang giám sát" */
    .board-empty__wave {
      position: absolute;
      inset: 0;
      border-radius: 50%;
      border: 1px solid var(--empty-color);
      opacity: 0;
      animation: board-empty-wave 2.8s ease-out infinite;

      &--late {
        animation-delay: 1.4s;
      }
    }

    .board-empty__text {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 3px;
      text-align: center;
    }

    .board-empty__title {
      font-size: 15px;
      font-weight: 700;
      letter-spacing: 0.3px;
      color: $grid-navy;
    }

    .board-empty__desc {
      font-size: 12.5px;
      color: color-mix(in srgb, #{$grid-navy} 60%, transparent);
    }

    /* Thu gọn: 1 dòng ngang (icon nhỏ + tiêu đề + mô tả), bỏ vòng sóng để vừa ~70px */
    &--compact {
      flex-direction: row;
      gap: 10px;
      padding: 0;

      .board-empty__icon {
        width: 28px;
        height: 28px;
        font-size: 16px;
      }

      .board-empty__wave {
        display: none;
      }

      .board-empty__text {
        flex-direction: row;
        align-items: baseline;
        gap: 8px;
      }

      .board-empty__title {
        font-size: 13.5px;
      }

      .board-empty__text {
        white-space: nowrap;
      }
    }
  }

  /* Ô rỗng của vxe: nội dung mặc định chỉ rộng 50% ⇒ cho rộng tự nhiên để 1 dòng không bị bẻ */
  .xGrid-style :deep(.vxe-table--empty-content) {
    width: auto;
  }

  /* Thu gọn ẩn header bảng nhưng lớp rỗng của vxe có thể còn giữ offset cũ ⇒ phủ kín thân bảng và căn giữa */
  .visualizing-container-content-center.collapsed .xGrid-style :deep(.vxe-table--empty-placeholder) {
    top: 0 !important;
    height: 100% !important;
    min-height: 0;
  }

  @keyframes board-empty-wave {
    0% { transform: scale(1); opacity: 0.55; }
    100% { transform: scale(1.9); opacity: 0; }
  }

  .xGrid-style {
    --vxe-ui-layout-background-color: #{$grid-white}!important; /* Nền cơ sở: trắng */
    --vxe-ui-font-color: #{$grid-navy}!important;               /* Chữ mặc định: xanh đậm */
    --vxe-ui-table-header-background-color: #{$grid-navy}!important; /* Header: xanh đậm */
    --vxe-ui-table-header-font-color: #ffffff!important;             /* Chữ header: trắng */
    --vxe-ui-table-border-color: #{$grid-line}!important;            /* Viền lưới */

    /* Header giữ chữ trắng (ghi đè rule global tô header theo màu primary) */
    :deep(.vxe-table--header),
    :deep(.vxe-header--row),
    :deep(.vxe-header--column),
    :deep(.vxe-table--header .vxe-cell) {
      color: #ffffff !important;
    }

    /* Ô trong suốt để lộ nền của HÀNG (đặt màu nền ở cấp hàng) */
    :deep(.vxe-body--column) {
      background-color: transparent !important;
      background-image: none !important;
    }

    /* Hàng lẻ: nền TRẮNG, chữ XANH ĐẬM */
    :deep(.vxe-body--row) {
      background-color: $grid-white !important;
    }
    :deep(.vxe-body--row .vxe-cell),
    :deep(.vxe-body--row .vxe-cell p) {
      color: $grid-navy !important;
    }

    /* Hàng chẵn (sọc): nền xanh xám rất nhạt, chữ vẫn XANH ĐẬM — sọc nhạt để dòng chèn liên tục không nháy */
    :deep(.vxe-body--row.row--stripe) {
      background-color: $grid-stripe !important;
    }

    /* Hàng đang chọn: nền xanh nhạt, chữ xanh đậm (dễ đọc) */
    :deep(.vxe-body--row.row--current) {
      background-color: $grid-current !important;
    }
    :deep(.vxe-body--row.row--current .vxe-cell),
    :deep(.vxe-body--row.row--current .vxe-cell p) {
      color: $grid-navy !important;
    }


    /* Dòng đang phát camera ở khung bên phải (giữ được qua mỗi lần SSE nạp lại) */
    :deep(.vxe-body--row.row--selected) {
      background-color: $grid-current !important;
    }
    :deep(.vxe-body--row.row--selected .vxe-cell),
    :deep(.vxe-body--row.row--selected .vxe-cell p) {
      color: $grid-navy !important;
    }

    /* Viền màu bên trái theo nhóm: sự kiện (cam) / sự cố (đỏ) — không bị nền sọc/hover đè vì là box-shadow */
    :deep(.vxe-body--row.row-group--event > .vxe-body--column:first-child) {
      box-shadow: inset 4px 0 0 $kind-event;
    }
    :deep(.vxe-body--row.row-group--incident > .vxe-body--column:first-child) {
      box-shadow: inset 4px 0 0 $kind-incident;
    }

    /* Hover: LUÔN về nền TRẮNG, chữ XANH (đặt cuối để thắng cả sọc lẫn current) */
    :deep(.vxe-body--row:hover),
    :deep(.vxe-body--row.row--hover),
    :deep(.vxe-body--row.row--stripe:hover),
    :deep(.vxe-body--row.row--stripe.row--hover),
    :deep(.vxe-body--row.row--current:hover),
    :deep(.vxe-body--row.row--current.row--hover) {
      background-color: $grid-white !important;
    }
    :deep(.vxe-body--row:hover .vxe-cell),
    :deep(.vxe-body--row.row--hover .vxe-cell),
    :deep(.vxe-body--row.row--stripe:hover .vxe-cell),
    :deep(.vxe-body--row.row--stripe.row--hover .vxe-cell),
    :deep(.vxe-body--row:hover .vxe-cell p),
    :deep(.vxe-body--row.row--hover .vxe-cell p),
    :deep(.vxe-body--row.row--stripe:hover .vxe-cell p),
    :deep(.vxe-body--row.row--stripe.row--hover .vxe-cell p) {
      color: $grid-navy !important;
    }
  }

  .custom-tabs {
    width: 100%;
    
    :deep(.el-tabs__header) {
      margin: 0;
      padding: 0;
      position: relative;
      width: 100%;
    }
    
    :deep(.el-tabs__nav-wrap) {
      padding: 0;
      overflow: visible !important;
      position: relative;
      width: 100%;
    }
    
    :deep(.el-tabs__nav-scroll) {
      overflow: hidden !important;
      width: 100%;
    }
    
    :deep(.el-tabs__nav) {
      float: none !important;
      white-space: nowrap;
      display: inline-block;
    }
    
    :deep(.el-tabs__item) {
      color: #8ea6c8 !important;
      padding: 8px 12px;
      border-radius: 4px 4px 0 0;
      transition: all 0.3s ease;
      font-size: 13px;
      display: inline-block;
      
      &.is-active {
        color: #ffffff !important;
        background-color: var(--el-color-primary) !important;
        font-weight: bold;
      }

      &:hover {
        background-color: color-mix(in srgb, var(--el-color-primary) 22%, transparent);
      }
    }

    :deep(.el-tabs__nav-wrap::after) {
      background-color: transparent;
    }

    :deep(.el-tabs__active-bar) {
      background-color: var(--el-color-primary);
      height: 3px;
    }

  }

  .camera-card {
    .el-card__body {  
      padding: 3px!important;
    }
  }

  .live-cam-parent {
    display: contents;
    width: 100% !important;
    min-height: 180px;
    height: 200px;
    align-items: center;
    margin: 0 !important;
    padding: 0 35px !important; 
  }



  thead {
    background-color: var(--el-color-primary-light-5); 
  }
  .vxe-cell .el-button{
    padding: 5px !important;
    margin: 0px !important;
  }

  /* Mô tả hướng ảnh hưởng trong cột Vị trí - kế thừa màu chữ của hàng (tương phản theo nền),
     làm mờ nhẹ để phân cấp phụ; tránh cố định 1 màu gây chìm trên hàng trắng/xanh. */
  :deep(.location-direction) {
    color: inherit;
    opacity: 0.75;
    // font-size: 12px;
    // font-style: italic;
  }

  /* Nút thao tác sự cố: chỉ icon, tròn, kích thước đồng đều */
  :deep(.action-icons) {
    .el-button.is-circle {
      width: 26px !important;
      height: 26px !important;
      padding: 0 !important;
      margin: 0 !important;
    }
    .el-button + .el-button {
      margin-left: 0 !important;
    }
  }

  .border-traffic-info{
    background: linear-gradient(135deg, color-mix(in srgb, var(--el-color-primary) 88%, #000) 0%, var(--el-color-primary) 100%);
    padding: 10px 8px 8px;
    border-radius: 10px;
    box-shadow: 0 8px 22px -12px color-mix(in srgb, var(--el-color-primary) 60%, transparent), inset 0 1px 0 rgba(255, 255, 255, 0.12);
  }

  .title-center{
    width:100%;
    text-align:center;
    padding: 4px;
    overflow: hidden;
    
    p {
      margin: 0;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      font-size: 12px;
      max-width: 100%;
    }
    
    span {
      white-space: nowrap;
    }
  }

  .camera-title {
    flex-shrink: 0;
    margin: 2px 0 !important;
    min-height: auto;
  }

  :deep(.vxe-grid--pager-wrapper){
    display: none!important;
  }

/* Global styles for tab navigation buttons - MUST be clickable */
.tab-live-cam .el-tabs__header {
  position: relative !important;
  overflow: visible !important;
  padding: 0 !important;
  margin: 0 !important;
  width: 100% !important;
}

.tab-live-cam .el-tabs__nav-wrap {
  margin: 0 !important;
  padding: 0 35px !important;
  overflow: visible !important;
  position: relative !important;
}

.tab-live-cam .el-tabs__nav-prev,
.tab-live-cam .el-tabs__nav-next {
  position: absolute !important;
  top: 0 !important;
  bottom: 0 !important;
  cursor: pointer !important;
  z-index: 1002 !important;
  width: 32px !important;
  height: 40px !important;
  display: flex !important;
  visibility: visible !important;
  opacity: 1 !important;
  align-items: center !important;
  justify-content: center !important;
  background: #ffffff !important;
  border: 2px solid var(--el-color-primary) !important;
  box-shadow: 0 3px 12px rgba(0, 0, 0, 0.3) !important;
  transition: all 0.3s !important;
  font-size: 18px !important;
  color: var(--el-color-primary) !important;
  pointer-events: all !important;
  font-weight: bold !important;
  line-height: 1 !important;
  margin: 0 !important;
}

.tab-live-cam .el-tabs__nav-prev:hover,
.tab-live-cam .el-tabs__nav-next:hover {
  color: #ffffff !important;
  background: var(--el-color-primary) !important;
  border-color: var(--el-color-primary) !important;
  box-shadow: 0 5px 18px rgba(0, 0, 0, 0.4) !important;
  transform: scale(1.08) !important;
}

.tab-live-cam .el-tabs__nav-prev.is-disabled,
.tab-live-cam .el-tabs__nav-next.is-disabled {
  opacity: 0.25 !important;
  cursor: not-allowed !important;
  border-color: #dcdfe6 !important;
  color: #c0c4cc !important;
  background: #f5f7fa !important;
  pointer-events: none !important;
}

.tab-live-cam .el-tabs__nav-prev {
  left: 0 !important;
  border-radius: 0 8px 8px 0 !important;
}

.tab-live-cam .el-tabs__nav-next {
  right: 0 !important;
  border-radius: 8px 0 0 8px !important;
}

.tab-live-cam .el-tabs__nav-prev i,
.tab-live-cam .el-tabs__nav-next i {
  font-size: 16px !important;
  font-weight: bold !important;
  line-height: 1 !important;
}

.tab-live-cam .el-tabs__content {
  padding: 0 !important;
  margin: 0 !important;
}

.tab-live-cam {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: visible;
  width: 100%;
  position: relative;

  /* Border-card sang tông tối neon (mặc định EP là nền sáng) */
  &.el-tabs--border-card {
    background: transparent;
    border-color: color-mix(in srgb, var(--el-color-primary) 30%, transparent);
    box-shadow: none;
  }

  :deep(.el-tabs__header.is-top) {
    background: rgba(10, 18, 32, 0.55);
    border-bottom-color: color-mix(in srgb, var(--el-color-primary) 25%, transparent);
  }

  :deep(.el-tabs__content) {
    flex: 1;
    min-height: 160px;
    overflow: hidden;
    padding: 0;
    margin: 0;
  }

  /* Đảm bảo khung video luôn có kích thước để canvas hiển thị */
  :deep(.el-tab-pane) {
    height: 100%;
    min-height: 160px;
  }

  :deep(.live-cam) {
    min-height: 160px;
  }
}
</style>
