<template>
    <!-- SonTH: FAB xử lý sự kiện/ sự cố -->
    <!-- Floating Action Button -->
    <transition name="fab-fade">
        <el-tooltip
            v-if="!incidentMonitor.isShowIncidentPopup && state.totalIncidentCount > 0"
            content="Xử lý sự kiện/ sự cố"
            placement="left"
            effect="dark"
            :auto-close="1000"
        >
            <div 
                :class="['incident-fab', { 'fab-blink': state.shouldBlink }]"
                @click="openIncidentDialog"
            >
                <div class="fab-content">
                    <el-icon class="fab-icon" :size="24">
                        <WarningFilled />
                    </el-icon>
                    <el-badge :value="state.totalIncidentCount" :max="99" class="fab-badge" />
                </div>
            </div>
        </el-tooltip>
    </transition>

    <!-- SonTH - 24/07/2026: append-to-body để dialog và dropdown/tooltip bên trong nó cùng nằm ở
         body. el-dialog mặc định không teleport nên render tại chỗ, còn el-select teleport ra body;
         hai stacking context khác nhau khiến dialog có thể đè lên dropdown của chính nó.
         Style của component này không scoped nên vẫn áp dụng bình thường sau khi teleport. -->
    <el-dialog 
    v-model="incidentMonitor.isShowIncidentPopup" 
    :width="state.dialogWidth" 
    class="incidents-dialog" 
    append-to-body
    @open="handleOpen" 
    @close="handleClose" 
    :close-on-click-modal="false"
    :close-on-press-escape="!state.isProcessing"
    :show-close="!state.isProcessing"
    :draggable="!state.isProcessing"
     destroy-on-close
     style="border: 5px solid var(--el-color-primary);width:90%">
        <template #header>
            <div style="color: #fff">
                <span> {{ $t('lz.label.tmsIncident.incidentTitle') }} </span>
            </div>
        </template>
        <div class="incident-dialog-content" :aria-busy="state.isProcessing"
            v-loading="state.isProcessing" :element-loading-text="$t('lz.label.base.loading')"
            @wheel.capture="blockProcessingScroll" @touchmove.capture="blockProcessingScroll">
        <div class="incident-dialog-panels" :inert="state.isProcessing || undefined">
        <el-tabs v-model="state.activeTab" style="padding:10px 10px 0 10px">
             <!-- SECTION: tab xác thực -->
            <el-tab-pane v-if="state.registrationIncidentList?.length > 0" name="registrationIncidentTab">
                <template #label>
                    <div class="tab-label">
                        <span class="margin-x--5px">{{ $t('lz.label.tmsIncident.registrationTitle') }}</span>
                        <el-badge :value="state.registrationIncidentList?.length" class="ml-2" type="warning" />
                    </div>
                </template>
                
                <div class="registration-panel">
                    <!-- Loading indicator -->
                    <!-- <div v-if="state.isProcessing" class="loading-overlay">
                        <el-icon class="loading-icon"><Loading /></el-icon>
                        <p>{{ $t('lz.label.base.loading') }}</p>
                    </div> -->

                    <!-- Scrollable incident list -->
                    <div class="incident-list-container">
                        <!-- SECTION: Sect-123 Xử lý danh sách xác nhận -->
                        <div class="incident-grid">
                            <el-card 
                                v-for="iwd in state.registrationIncidentList" 
                                :key="iwd.incident.id"
                                :class="['incident-card', {'is-selected': iwd.incident.id == incidentMonitor.selectedIncidentId}]"
                                shadow="hover"
                            >
                                <!-- Header with formatted information -->
                                <div class="incident-content">
                                    <div class="incident-content-group">
                                        <div class="incident-main-info">
                                            <div class="incident-image-container" style="border: 1px solid var(--el-color-primary);">
                                                <el-image style="height: 50%;width: 130px;padding: 10px"
                                                    class="incident-icon" 
                                                    :src="getIncidentIcon(iwd.incident?.eventTypeId)"
                                                    fit="contain"
                                                />
                                                <div class="incident-image-text">
                                                    <div class="incident-label">
                                                    {{ iwd.incident?.eventTypeName ?? (getEventType(iwd.incident?.eventTypeId)?.name ?? '') }}
                                                    </div>
                                                    <div class="incident-time">
                                                        {{ useDateFormat(iwd.incident?.startDate, DateTimeFormatEnum.DateTimeDefault) }}
                                                        <br>
                                                        {{ commonVarsAndFuncs.formatPosition(iwd.incident?.kmNumber, iwd.incident?.metNumber) }}
                                                    </div>
                                                </div>
                                               
                                            </div>
                                        </div>

                                        <!-- Incident details in grid layout -->
                                        <div class="incident-details-grid">
                                            <div class="detail-row">
                                                <div class="detail-label">{{ $t('lz.entity.base.code') }}</div>
                                                <div class="detail-value">{{ iwd.incident?.code }}</div>
                                            </div>
                                            <div class="detail-row">
                                                <div class="detail-label">{{ $t('lz.entity.base.name') }}</div>
                                                <div class="detail-value">{{ iwd.incident?.name }}</div>
                                            </div>
                                            <div class="detail-row">
                                                <div class="detail-label">{{ $t('lz.entity.tmsIncident.influenceScope') }}</div>
                                                <div class="detail-value display-flex--horizontal flex-gap--5px">
                                                   <TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsZoneLocation, iwd.incident?.location)" />
                                                    <TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsDirectionType,iwd.incident?.influenceScope)" />
                                                </div>
                                            </div>
                                            <div class="detail-row">
                                                <div class="detail-label">{{ $t('lz.label.base.camera') }}</div>
                                                <div class="detail-value camera-list">
                                                    <template v-if="iwd.incident?.equipmentId?.length > 0 && state.equipmentList?.length > 0">
                                                        <el-button  round
                                                            v-for="equip in getIncidentCameras(iwd.incident)"
                                                            :key="equip.id"
                                                            size="small" 
                                                            @click="showCameraPopup(equip)"
                                                            :color="getEquipmentStateColor(equip.equipmentState)"
                                                            style="color: white;border-radius: 10px;"
                                                        >
                                                            {{ commonVarsAndFuncs.formatCameraName(equip.name, equip.kmNumber, equip.metNumber) }}
                                                        </el-button>
                                                    </template>
                                                </div>
                                            </div>
                                        </div>

                                    </div>
                                    <!-- Action buttons -->
                                    <div class="incident-actions">
                                        <!-- SonTH - 24/07/2026: sự cố đang chờ tự động thực thi thì khóa thao tác thủ công
                                             để người trực không bấm đè lên luồng tự động. Muốn can thiệp tay thì dùng
                                             "chuyển thủ công" trên banner cảnh báo. -->
                                        <el-tag v-if="isAutomationPending(iwd)" type="warning" effect="dark" size="small" class="automation-pending-tag">
                                            {{ $t('lz.label.tmsIncidentAutomation.autoExecuteIn') }} {{ automationStore.formatRemaining(iwd) }}
                                        </el-tag>
                                        <el-button 
                                            size="small"
                                            :disabled="state.isProcessing || isAutomationPending(iwd)" 
                                            type="danger" 
                                            @click="skipIncident(iwd.incident?.id)"
                                            class="action-btn"
                                        >
                                            {{ $t('lz.button.tmsIncident.skip') }}
                                        </el-button>
                                        <el-button 
                                            size="small"
                                            :disabled="state.isProcessing || isAutomationPending(iwd)" 
                                            type="success" 
                                            @click="verifyIncident(iwd.incident?.id)"
                                            class="action-btn"
                                        >
                                            {{ $t('lz.button.tmsIncident.verify') }}
                                        </el-button>
                                    </div>
                                </div>
                            </el-card>
                        </div>
                    </div>

                    <!-- Static footer with batch actions -->
                    <div class="registration-footer">
                            <el-button 
                                :disabled="state.isProcessing || !state.registrationIncidentList?.length" 
                                type="danger" 
                                @click="skipAllIncidents"
                            >
                                {{ $t('lz.button.tmsIncident.skipAll') }}
                            </el-button>
                            <el-button class="ml10" 
                                :disabled="state.isProcessing || !state.registrationIncidentList?.length" 
                                type="success" 
                                @click="verifyAllIncidents"
                            >
                                {{ $t('lz.button.tmsIncident.verifyAll') }}
                            </el-button>
                    </div>
                </div>
            </el-tab-pane>
            <!-- SECTION: tab xác nhận & chọn kịch bản -->
            <el-tab-pane v-if="state.verifiedIncidentList?.length > 0" name="verifiedIncidentTab">
                <template #label>
                    <div class="tab-label">
                        <span class="margin-x--5px">{{ $t('lz.label.tmsIncident.verifiedTitle') }}</span>
                        <el-badge :value="state.verifiedIncidentList?.length" class="ml-2" type="warning" />
                    </div>
                </template>
                <div class="verified-panel">
                    <!-- Loading indicator
                    <div v-if="state.isProcessing" class="loading-overlay">
                        <el-icon class="loading-icon"><Loading /></el-icon>
                        <p>{{ $t('lz.label.base.loading') }}</p>
                    </div> -->

                    <!-- Scrollable incident list -->
                    <div class="incident-list-container">
                        <div class="incident-grid">
                            <el-card 
                                v-for="iwd in state.verifiedIncidentList" 
                                :key="iwd.incident.id"
                                :class="['incident-card', {'is-selected': iwd.incident.id == incidentMonitor.selectedIncidentId}]"
                                shadow="hover"
                            >
                                <div class="incident-content">
                                    <!-- Header with Image and Basic Info -->
                                    <div class="incident-header">
                                        <el-image 
                                            class="incident-image" 
                                            :src="getIncidentIcon(iwd.incident?.eventTypeId)"
                                            fit="contain"
                                        />
                                        <div class="incident-title text-uppercase">
                                            <h3>{{ iwd.incident?.eventTypeName ?? (getEventType(iwd.incident?.eventTypeId)?.name ?? iwd.incident?.name) }}</h3>
                                            <p class="incident-datetime">
                                                <i class="far fa-clock mr-2"></i>
                                                {{ useDateFormat(iwd.incident?.startDate, DateTimeFormatEnum.DateTimeDefault) }}
                                            </p>
                                            <p class="incident-location">
                                                <i class="fas fa-map-marker-alt mr-2"></i>
                                                {{ commonVarsAndFuncs.formatPosition(iwd.incident?.kmNumber, iwd.incident?.metNumber) }}
                                            </p>
                                        </div>
                                    </div>

                                    <!-- Details -->
                                    <div class="incident-details">
                                        <el-descriptions :column="1" border>
                                            <el-descriptions-item :label="$t('lz.entity.base.code')" label-width="120px">
                                                {{ iwd.incident?.code }}
                                            </el-descriptions-item>
                                            <el-descriptions-item :label="$t('lz.entity.base.name')">
                                                {{ iwd.incident?.name }}
                                            </el-descriptions-item>
                                            <el-descriptions-item :label="$t('lz.entity.tmsIncident.influenceScope')" class="display-flex--horizontal flex-gap--5px flex-align--center">
                                                <div class="detail-value display-flex--horizontal flex-gap--5px">
                                                   <TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsZoneLocation, iwd.incident?.location)" />
                                                    <TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsDirectionType,iwd.incident?.influenceScope)" />
                                                </div>
                                            </el-descriptions-item>
                                            <el-descriptions-item :label="$t('lz.label.base.camera')">
                                                <div class="camera-buttons">
                                                    <template v-if="iwd.incident?.equipmentId?.length > 0 && state.equipmentList?.length > 0">
                                                        <el-button round
                                                            v-for="equip in getIncidentCameras(iwd.incident)"
                                                            :key="equip.id"
                                                            size="small" 
                                                            @click="showCameraPopup(equip)"
                                                            :color="getEquipmentStateColor(equip.equipmentState)"
                                                            style="color: white;"
                                                        >
                                                            {{ commonVarsAndFuncs.formatCameraName(equip.name, equip.kmNumber, equip.metNumber) }}
                                                        </el-button>
                                                    </template>
                                                </div>
                                            </el-descriptions-item>
                                            <el-descriptions-item :label="$t('lz.entity.tmsIncident.trafficPlanId')">
                                                <!-- popper-class: dropdown được teleport ra body với z-index ~2000,
                                                     phải nâng lên trên overlay của popup này (xem style cuối file) -->
                                                <el-select 
                                                    v-model="state.selectedTrafficPlan[iwd.incident?.id]" 
                                                    class="w-100"
                                                   
                                                    :placeholder="$t('lz.label.base.pleaseSelect', {input: $t('lz.entity.tmsIncident.trafficPlanId')})"
                                                >
                                                    <el-option 
                                                        v-for="tp in state.trafficPlanList?.filter((item:any) => item.eventTypeId == iwd.incident?.eventTypeId)" 
                                                        :key="tp.id"
                                                        :label="tp.name" 
                                                        :value="tp.id" 
                                                        :disabled="tp.hasWorkDetailMapped != true" 
                                                    />
                                                </el-select>
                                            </el-descriptions-item>
                                        </el-descriptions>
                                    </div>

                                    <!-- Actions -->
                                    <div class="incident-actions">
                                        <!-- SonTH - 24/07/2026: khóa thao tác thủ công khi đang chờ tự động thực thi -->
                                        <el-tag v-if="isAutomationPending(iwd)" type="warning" effect="dark" size="small" class="automation-pending-tag">
                                            {{ $t('lz.label.tmsIncidentAutomation.autoExecuteIn') }} {{ automationStore.formatRemaining(iwd) }}
                                        </el-tag>
                                        <el-button-group>
                                            <el-button 
                                                :disabled="state.isProcessing || isAutomationPending(iwd)" 
                                                type="danger" 
                                                @click="cancelIncident(iwd.incident?.id)"
                                                class="mr5"
                                            >
                                                {{ $t('lz.button.base.cancel') }}
                                            </el-button>
                                            <el-button 
                                                :disabled="state.isProcessing || isAutomationPending(iwd)" 
                                                type="primary" 
                                                @click="applyIncidentTrafficPlan(iwd.incident?.id)"
                                            >
                                                {{ $t('lz.button.tmsIncident.authorize') }}
                                            </el-button>
                                        </el-button-group>
                                    </div>
                                </div>
                            </el-card>
                        </div>
                    </div>

                    <!-- Static footer with batch actions -->
                    <div class="verified-footer">
                        <el-button-group>
                            <el-button 
                                :disabled="state.isProcessing || !state.verifiedIncidentList?.length" 
                                type="danger" 
                                @click="cancelAllIncidents"
                            >
                                {{ $t('lz.button.tmsIncident.cancelAll') }}
                            </el-button>
                        </el-button-group>
                    </div>
                </div>
            </el-tab-pane>
            <!-- SECTION: tab xử lý kịch bản -->
            <el-tab-pane v-if="state.inProgressIncidentList?.length > 0" name="inProgressIncidentTab">
                <template #label>
                    <div class="tab-label">
                        <span class="margin-x--5px">{{ $t('lz.label.tmsIncident.inProgressTitle') }}</span>
                        <el-badge :value="state.inProgressIncidentList?.length" class="ml-2" type="warning" />
                    </div>
                </template>
                <div class="inprogress-panel">
						<!-- Timeline sidebar -->
						<div class="tl-sidebar">
							<div class="tl-track">
								<div class="tl-line"></div>
								<el-tooltip v-for="iwd in state.inProgressIncidentList" :key="iwd.incident.id" :content="iwd.incident?.name" placement="top" :auto-close="1000">
									<div :class="['tl-node', { 'tl-node--active': iwd.incident.id == activeTimelineId }]" @click="scrollToIncident(iwd.incident.id)">
										<div class="tl-dot">
											<el-image :src="getIncidentIcon(iwd.incident?.eventTypeId)" fit="contain" class="tl-dot-icon" />
										</div>
										<div class="tl-info">
											<div class="tl-time">{{ useDateFormat(iwd.incident?.startDate, DateTimeFormatEnum.DateTimeDefault) }}</div>
											<div class="tl-loc">{{ commonVarsAndFuncs.formatPosition(iwd.incident?.kmNumber, iwd.incident?.metNumber) }}</div>
										</div>
									</div>
								</el-tooltip>
							</div>
						</div>
						<!-- Main incident content -->
						<div class="inprogress-content">
							<div class="display-flex--vertical flex-gap--20px">
								<el-card v-for="iwd in state.inProgressIncidentList" :id="'incident-card-' + iwd.incident.id" :class="'confirmed-incident-card ' + (iwd.incident.id == incidentMonitor.selectedIncidentId ? ' selected-incident' : '')" shadow="hover">
                                <!-- Confirmed Incident Info -->
                                <div class="confirmed-incident-container">
                                    <!-- Icon and Event Type Section -->
                                    <div class="incident-icon-section">
                                        <div class="icon-wrapper">
                                            <el-image 
                                                class="incident-icon-image" 
                                                fit="contain" 
                                                :src="getIncidentIcon(iwd.incident?.eventTypeId)" 
                                            />
                                            <div class="icon-label-section">
                                                <div class="icon-event-label">
                                                    <b>
                                                        {{ iwd.incident?.eventTypeName ?? (getEventType(iwd.incident?.eventTypeId)?.name ?? iwd.incident?.name) }}
                                                    </b>
                                                </div>
                                                <div class="icon-time-info">
                                                    <i class="el-icon-time"></i>
                                                    {{ useDateFormat(iwd.incident?.startDate, DateTimeFormatEnum.DateTimeDefault) }}
                                                </div>
                                                <div class="icon-location-info">
                                                    <i class="el-icon-location"></i>
                                                    {{ commonVarsAndFuncs.formatPosition(iwd.incident?.kmNumber, iwd.incident?.metNumber) }}
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                    
                                    <!-- Incident Details Section -->
                                    <div class="incident-details-section mb10">
                                        <div class="incident-header-title">
                                            <h3>{{ iwd.incident?.name }}</h3>
                                            <div class="incident-header-tags">
                                                <el-tag type="danger" class="mr5" size="large">{{ iwd.incident?.eventTypeName }}</el-tag>
                                                <el-tag type="primary" size="large">{{ useDateFormat(iwd.incident?.startDate, DateTimeFormatEnum.DateTimeDefault) }}</el-tag>
                                            </div>
                                        </div>
                                        <el-alert v-if="isAutomationFailed(iwd)" class="automation-failure-notice" type="error" :closable="false" show-icon
                                            :title="$t('lz.label.tmsIncidentAutomation.failedNeedsManual')"
                                            :description="formatAutomationReason(iwd.automation?.errorCode, iwd.automation?.errorMessage)" />
                                        
                                        <div class="incident-info-grid">
                                            <div class="info-item">
                                                <div class="info-label">
                                                    <i class="el-icon-tickets"></i>
                                                    {{ $t('lz.entity.base.code') }}
                                                </div>
                                                <div class="info-value">{{ iwd.incident?.code }}</div>
                                            </div>
                                            
                                            <div class="info-item">
                                                <div class="info-label">
                                                    <i class="el-icon-document"></i>
                                                    {{ $t('lz.entity.tms.location') }}
                                                </div>
                                                <div class="info-value">
                                                    {{ commonVarsAndFuncs.formatPosition(iwd.incident?.kmNumber, iwd.incident?.metNumber) }}
                                                </div>
                                            </div>
                                            
                                            <div class="info-item">
                                                <div class="info-label">
                                                    <i class="el-icon-guide"></i>
                                                    {{ $t('lz.entity.tmsIncident.influenceScope') }}
                                                </div>
                                                <div class="info-value display-flex--horizontal flex-gap--5px flex-align--center">
                                                    <TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsZoneLocation, iwd.incident?.location)" />
                                                    <TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsDirectionType,iwd.incident?.influenceScope)" />
                                                </div>
                                            </div>
                                            
                                            <div class="info-item">
                                                <div class="info-label">
                                                    <i class="el-icon-s-flag"></i>
                                                    {{ $t('lz.entity.tmsIncident.trafficPlanId') }}
                                                </div>
                                                <div class="info-value">
                                                    {{ state.trafficPlanList?.find(item => item.id == iwd.incident?.trafficPlanId)?.name ?? '-' }}
                                                </div>
                                            </div>
                                            
                                            <div class="info-item info-item-full">
                                                <div class="info-label">
                                                    <i class="el-icon-video-camera"></i>
                                                    {{ $t('lz.label.base.camera') }}
                                                </div>
                                                <div class="info-value camera-buttons-wrapper">
                                                    <template v-if="iwd.incident?.equipmentId?.length > 0 && state.equipmentList?.length > 0">
                                                        <el-button round
                                                            v-for="equip in getIncidentCameras(iwd.incident)"
                                                            :key="equip.id"
                                                            size="small" 
                                                            @click="showCameraPopup(equip)"
                                                            :color="getEquipmentStateColor(equip.equipmentState)"
                                                            style="color: white;"
                                                        >
                                                            <i class="el-icon-video-camera"></i>
                                                            {{ commonVarsAndFuncs.formatCameraName(equip.name, equip.kmNumber, equip.metNumber) }}
                                                        </el-button>
                                                    </template>
                                                    <span v-else class="text-muted">-</span>
                                                </div>
                                            </div>
                                            
                                            <!-- <div class="info-item">
                                                <div class="info-label">
                                                    <i class="el-icon-time"></i>
                                                    {{ $t('lz.entity.tmsIncident.startDate') }}
                                                </div>
                                                <div class="info-value">
                                                    <el-tag type="warning">{{ useDateFormat(iwd.incident?.startDate, DateTimeFormatEnum.DateTimeDefault) }}</el-tag>
                                                </div>
                                            </div> -->
                                        </div>
                                    </div>
                                </div>
                                <!-- Traffic Plan Work Detail Table -->
                                <div v-if="iwd.workExecuteList?.length > 0" class="work-detail-table-container">
                                    <div class="table-header mb3">
                                        <!-- <h4>{{ $t('lz.label.tmsIncident.workDetailTitle') }}</h4> -->
                                        <el-button 
                                            size="small" 
                                            type="primary" 
                                            :icon="'ele-Refresh'" 
                                            @click="refreshWorkExecuteList(iwd.incident.id)"
                                            :disabled="state.isProcessing"
                                        >
                                            {{ $t('lz.button.base.refresh') }}
                                        </el-button>
                                    </div>
                                    <table class="table-data bg-color--white table-border w100">
                                        <tr class="text-center">
                                            <th class="width-80px text-center">{{ $t('lz.entity.tmsWorkExecute.step') }}</th>
                                            <th class="width-300px text-center">{{ $t('lz.entity.tmsWorkExecute.workName') }}</th>
                                            <th class="width-500px text-center">{{ $t('lz.label.tmsTrafficPlanWorkDetail.contactList') }}</th>
                                            <th class="width-300px text-center">{{ $t('lz.entity.base.remark') }}</th>
                                            <!-- <th class="text-center">{{ $t('lz.label.tmsWorkExecute.autoTitle') }}</th> -->
                                            <th class="width-300px text-center">{{ $t('lz.label.base.action') }}</th>

                                        </tr>
                                        <el-tooltip 
                                            v-for="(we, idx) in iwd.workExecuteList"
                                            :key="we.id"
                                            :disabled="isPreviousStepsCompleted(iwd.workExecuteList, we.step)"
                                            :content="$t('lz.message.tmsWorkExecute.completePreviousSteps')"
                                            placement="top"
                                           
                                        >
                                            <tr 
                                                :class="[
                                                    idx % 2 == 0 ? 'bg-color--white' : 'bg-color--light-gray',
                                                    !isPreviousStepsCompleted(iwd.workExecuteList, we.step) ? 'row-disabled' : ''
                                                ]">
                                                <td class="text-center"><strong>{{ $t('lz.entity.tmsWorkExecute.step') + ' ' + we.step }}</strong></td>
                                                <td class="text-left">
                                                    <h4>{{ we.workName }}</h4>
                                                    <p v-if="we.description" class="text-italic mt10">{{ we.description }}</p>
                                                    <p v-if="isAutomationHandOverWork(we)" class="automation-failed-reason mt5">{{ formatAutomationReason(null, we.remark) }}</p>
                                                </td>
                                                <td class="text-center">
                                                    <table class="table-data table-border" v-if="we.workContactList?.length > 0" style="min-width: 200px; margin: 0 auto;">
                                                        <tr v-for="wc in we.workContactList">
                                                            <td>{{ wc.contactName }}</td>
                                                            <td>
                                                                {{ wc.contactPhone }}
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                                <td class="text-center">
                                                    <el-input
                                                        v-model="state.workExecuteRemarkList[we.id]"
                                                        type="textarea"
                                                        :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.base.remark')})"
                                                        class="w100"
                                                        :autosize="{ minRows: 2, maxRows: 8 }"
                                                        :disabled="!isPreviousStepsCompleted(iwd.workExecuteList, we.step)"
                                                    />
                                                </td>
                                                 <!-- <td class="text-center">
                                                    <div class="display-flex--horizontal flex-gap--5px flex-justify--center">
                                                        <el-button v-if="we.workMode == TmsWorkModeEnum.Auto" :disabled ="state.isProcessing || !isPreviousStepsCompleted(iwd.workExecuteList, we.step)" size="medium" @click="autoWorkExecute(we.id, we.controlType, iwd.incident.id)">{{ $t('lz.button.tmsWorkExecute.auto') }}</el-button>
                                                    </div>
                                                </td> -->
                                                <td class="text-center">
                                                    <div class="display-flex--horizontal flex-gap--5px flex-justify--center">
                                                        <el-button :disabled ="state.isProcessing || !isPreviousStepsCompleted(iwd.workExecuteList, we.step)" type="success" :plain="we.controlType" size="medium" @click="verifyWorkExecute(we.id, we.controlType, iwd.incident.id)" class="width-80px">{{ $t('lz.button.tmsWorkExecute.executed') }}</el-button>
                                                        <el-button :disabled ="state.isProcessing || !isPreviousStepsCompleted(iwd.workExecuteList, we.step)" type="danger" size="medium" @click="skipWorkExecute(we.id)" class="width-80px">{{ $t('lz.button.tmsIncident.skip') }}</el-button>
                                                    </div>
                                                </td>
                                            </tr>
                                        </el-tooltip>
                                    </table>
                                </div>
                            </el-card>
							</div>
						</div>
					</div>
				</el-tab-pane>
        </el-tabs>

        <div v-if="state.allProcessingIncident?.length > 0" class="leader-incident-list">
            <div v-for="(iwd, idx) in state.allProcessingIncident" class="pd10 mb20" :style="(idx % 2 != 0 ? 'background-color: var(--light-gray)' : 'background-color: white')">
                <h3 class="pb10 pt10" :style="(idx % 2 != 0 ? 'background-color: var(--light-gray)' : 'background-color: white')">
                    {{ (idx + 1) }}. {{ iwd.incident?.name }} ({{ commonVarsAndFuncs.formatPosition(iwd.incident?.kmNumber, iwd.incident?.metNumber) }})
                </h3>
                <el-alert v-if="isAutomationFailed(iwd)" class="automation-failure-notice mb10" type="error" :closable="false" show-icon
                    :title="$t('lz.label.tmsIncidentAutomation.failedNeedsManual')"
                    :description="formatAutomationReason(iwd.automation?.errorCode, iwd.automation?.errorMessage)" />
                <div class="display-flex--horizontal flex-gap--20px flex-align--stretch">
                    <div class="sticky-image width-200px">
                        <div class="position-sticky" style="top: 20px;">
                            <el-card>
                                <el-image class="width-200px height-100px" fit="contain" :src="getIncidentIcon(iwd.incident?.eventTypeId)"  style="margin-left: -8px"/>
                                <div class="text-center">
                                    <h3 class="text-uppercase">{{ iwd.incident?.name }}</h3>
                                    <p><strong>{{ useDateFormat(iwd.incident?.startDate, DateTimeFormatEnum.DateTimeDefault) }}</strong></p>
                                    <p><strong>{{ commonVarsAndFuncs.formatPosition(iwd.incident?.kmNumber, iwd.incident?.metNumber) }}</strong></p>
                                </div>
                            </el-card>
                        </div>
                    </div>
                    <div v-if="iwd.workExecuteList?.length > 0" class="flex-grow--1">
                        <table class="table-data table-border w100">
                            <tr class="text-left">
                                <th class="width-80px">{{ $t('lz.entity.tmsWorkExecute.step') }}</th>
                                <th class="width-300px">{{ $t('lz.entity.tmsWorkExecute.executeOrg') }}</th>
                                <th>{{ $t('lz.entity.base.info') }}</th>
                            </tr>
                            <tr v-for="(we, idx) in iwd.workExecuteList" :class="idx % 2 == 0 ? 'bg-color--white' : 'bg-color--light-gray'">
                                <td><strong>{{ $t('lz.entity.tmsWorkExecute.step') + ' ' + we.step }}</strong></td>
                                <td class="width-150px">
                                    {{ getOrganizationName(we.executeOrg) }}
                                </td>
                                <td>
                                    <p class="text-italic text-mute mb10">{{ iwd.incident?.name }}</p>
                                    <div class="display-flex--horizontal flex-gap--5px flex-align--center">

                                        <TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsWorkExecuteStatus, we.state)" />

                                        <h4>{{ we.workName }}</h4>
                                    </div>
                                    <p v-if="we.description" class="mt10">{{ we.description }}</p>
                                    <p v-if="isAutomationHandOverWork(we)" class="automation-failed-reason mt5">{{ formatAutomationReason(null, we.remark) }}</p>
                                </td>
                            </tr>
                        </table>
                    </div>
                    <div class="sticky-actions">
                        <div class="position-sticky" style="top: 20px;">
                            <el-card>
                                <el-button :disabled ="state.isProcessing" type="primary" size="large" style="width: 80px;" @click="finishedIncident(iwd.incident?.id)">{{ $t('lz.button.tmsIncident.finish') }}</el-button>
                                <br><br>
                                <a target="_blank" :disabled ="state.isProcessing" :href="`${TmsReportRouteEnum.IncidentReportDetail}${iwd.incident?.id}`">
                                    <el-button type="success" size="large" style="width: 80px;">{{ $t('lz.button.tmsIncident.report') }}</el-button>
                                </a>
                            </el-card>
                        </div>
                    </div>
                </div>
            </div>
        </div>
        </div>
        </div>
    </el-dialog>

    <cameraPopupComp ref="cameraPopupCompRef"/>
    <vmsModalComp ref="vmsModalCompRef" @onMeSubmitSuccess="onVmsModalSubmitSuccess"/>
    <lcsModalComp ref="lcsModalCompRef" @onMeSubmitSuccess="onVmsModalSubmitSuccess"/>
</template>

<script lang="ts" setup>
    import { onMounted, onUnmounted, reactive, ref, defineAsyncComponent, watch, getCurrentInstance, computed, nextTick } from 'vue';
    import { Local, Session } from '/@/utils/storage';
    import { useI18n } from 'vue-i18n';
    import { useRoute } from 'vue-router';
    import { WarningFilled } from '@element-plus/icons-vue';
    import mittBus from '/@/utils/mitt';

    const { t: $t } = useI18n();

    import { TmsControlTypeEnum, TmsIncidentStateEnum, TmsReportRouteEnum, TmsWorkExecuteStateEnum, TmsWorkModeEnum, IncidentBroadcastObjectEnum } from '/@/types/enums';
    import { getAPI } from '/@/utils/axios-utils';
    import { TmsEquipmentTypeApi, TmsEquipmentApi, TmsEventTypeApi, TmsIncidentApi, TmsTrafficPlanApi, TmsWorkExecuteApi, TmsZoneEquipmentApi } from '/@/api-services/tms';
    import { ElMessage, ElMessageBox, ElNotification } from 'element-plus';
    import { AlertTypeEnum, UploadAssetItemsEnum, DateTimeFormatEnum, ZIndexHighestEnum } from '/@/types/enums/baseEnum';
    import { useDateFormat } from '@vueuse/core';
    import { VmsActionType } from '/@/types/enums/vmsEnum';

    import { useIncidentMonitor } from '/@/stores/its/incidentMonitor';
    const incidentMonitor = useIncidentMonitor();

    // SonTH - 24/07/2026: nơi chứa nhánh tự động hóa tách ra từ ListByAccount, dùng cho banner cảnh báo toàn cục
    import { useIncidentAutomation, AutomationRunStatus, AUTOMATION_SYSTEM_ACTOR } from '/@/stores/its/incidentAutomation';
    const automationStore = useIncidentAutomation();

    import { useTransporterIncident } from '/@/transporter/hooks/useTransporterIncident';

    import { useCommonVarsAndFuncs } from '/@/stores/its/commonVarsAndFuncs';
    const commonVarsAndFuncs = useCommonVarsAndFuncs();

    import { useBaseConfig } from '/@/stores/baseConfig';
	import { BaseConfigTypeEnum } from '/@/types/enums';
	const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
	const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
	const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));

    // - Begin - Add by KHOAND

    import { VmsCurrentApi } from '/@/api-services/vms';
    
    // - End - Add by KHOAND

    // Add by TuanHA
    import { useMapStore } from '/@/stores/its/mapStore';
    const mapStore = useMapStore();

    const { appContext } = getCurrentInstance()!;
    const route = useRoute();

    // IMPORTANCE: pnpm install @cycjimmy/jsmpeg-player
    const cameraPopupComp = defineAsyncComponent(() => import('/@/views/modal/common/cameraPopup.vue'));
    const cameraPopupCompRef = ref<InstanceType<typeof cameraPopupComp>>();

    const vmsModalComp = defineAsyncComponent(() => import('/@/views/modal/vms/vmsModal.vue'));
    const vmsModalCompRef = ref<InstanceType<typeof vmsModalComp>>();

    // Modal điều khiển LCS theo sự cố (song song vmsModal, deviceType = LCS)
    const lcsModalComp = defineAsyncComponent(() => import('/@/views/modal/vms/lcsModal.vue'));
    const lcsModalCompRef = ref<InstanceType<typeof lcsModalComp>>();

    const state = reactive<any>({
        isProcessing: false,
        dialogWidth: '60%',
        activeTab: null,
        incidentInfo: null,
        userOrgList: [],
        eventTypeList: [],
        equipmentTypeList: [],
        trafficPlanList: [],
        equipmentList: [],
        directionList: [],
        incidentList: [],
        stateColorList: [],
        // Map: incidentId -> danh sách camera liên quan (fetch theo equipmentId qua API listbyids)
        incidentCamerasMap: {},
        // -------------------------- Mornitor: NnGsgt
        registrationIncidentList: [],
        // -------------------------- Mornitor: NvGsgt
        verifiedIncidentList: [],
        selectedTrafficPlan: {}, // IMPORTANCE: NOT ARRAY => RECOMMEND
        // -------------------------- Monitor Or Employee: NvGsgt, NvHtd
        inProgressIncidentList: [],
        workExecuteRemarkList: {}, // IMPORTANCE: NOT ARRAY => RECOMMEND
        // -------------------------- Leader: TcVhh
        allProcessingIncident: [],
        // -------------------------- FAB State
        shouldBlink: false,
        blinkTimeout: null as any,
        totalIncidentCount: 0,
        // -------------------------- Initial Load Tracking
        hasInitialLoaded: false
    });

    // Common Functions ==============================================

    // Handle updating the incident state
    const updateIncidentState = async(incidentId: string, newState: string, nextActiveTab: string = '') => {
        if (state.isProcessing) return;
        // Validate all incident status cases
        if(!incidentId || !newState){
            alertWarning($t('lz.message.base.invalidData'), AlertTypeEnum.Warning);

            return;
        }

        let messageConfirm = null;
        if(newState == TmsIncidentStateEnum.Verified)
            messageConfirm = incidentId.trim().toLowerCase() == 'all' ? $t('lz.message.tmsIncident.verifyIncidentAllConfirm') : $t('lz.message.tmsIncident.verifyIncidentConfirm');
        else if(newState == TmsIncidentStateEnum.InProgress)
            messageConfirm = $t('lz.message.tmsIncident.authIncidentConfirm');
        else if(newState == TmsIncidentStateEnum.Finished)
            messageConfirm = $t('lz.message.tmsIncident.finishIncidentConfirm');
        else if(newState == TmsIncidentStateEnum.Skipped)
            messageConfirm = incidentId.trim().toLowerCase() == 'all' ? $t('lz.message.tmsIncident.skipIncidentAllConfirm') : $t('lz.message.tmsIncident.skipIncidentConfirm');
        else if(newState == TmsIncidentStateEnum.Canceled)
            messageConfirm = incidentId.trim().toLowerCase() == 'all' ? $t('lz.message.tmsIncident.cancelIncidentAllConfirm') : $t('lz.message.tmsIncident.cancelIncidentConfirm');

        let ownsProcessing = false;
        return ElMessageBox.confirm(
            messageConfirm,
            $t('lz.message.base.confirmProcess'),
            {
                showCancelButton: true,
                confirmButtonText: $t('lz.button.base.confirm'),
			    cancelButtonText: $t('lz.button.base.cancel'),
                type: 'warning',
                customStyle: {
                    'z-index': ZIndexHighestEnum.Max
                },
                customClass: 'z-index-highest--max',
            },
            appContext
        )
        .then(async() => {
            if (state.isProcessing) return;
            ownsProcessing = true;
            // Disable the controls
            setControlsToLoading(true);

            if(incidentId.trim().toLowerCase() == 'all'){
                let keyValueList = null;
                if(newState == TmsIncidentStateEnum.Verified || newState == TmsIncidentStateEnum.Skipped)
                    keyValueList = state.registrationIncidentList?.map(item => ({Id: item.incident?.id, State: newState}));
                else if(newState == TmsIncidentStateEnum.Canceled)
                    keyValueList = state.verifiedIncidentList?.map(item => ({Id: item.incident?.id, State: newState}));

                // Only batch update for states that are Verified
                if(keyValueList?.length > 0)
                    await getAPI(TmsIncidentApi).apiTmsTmsincidentBatchupdatestatePost(keyValueList);
            }
            else{
                let response = null;

                if(newState == TmsIncidentStateEnum.Verified){
                    response = await getAPI(TmsIncidentApi).apiTmsTmsincidentUpdatestatePost({id: incidentId, State: newState});
                }
                else if(newState == TmsIncidentStateEnum.InProgress){
                    // Validate that the traffic plan must have a value
                    if(!state.selectedTrafficPlan[incidentId]){
                        alertMessage($t('lz.message.tmsIncident.trafficPlanIsRequired'), AlertTypeEnum.Warning);

                        return;
                    }

                    response = await getAPI(TmsIncidentApi).apiTmsTmsincidentUpdatestatePost({id: incidentId, state: newState, trafficPlanId: state.selectedTrafficPlan[incidentId]});
                }
                else if(newState == TmsIncidentStateEnum.Finished){
                    response = await getAPI(TmsIncidentApi).apiTmsTmsincidentUpdatestatePost({id: incidentId, State: newState});
                    // - Begin - Add by KHOAND

                    try {
                            const result = await ElMessageBox.confirm(
                                "Độ ưu tiên hiện tại thấp hơn độ ưu tiên đã tồn tại. Bạn có muốn tiếp tục cập nhật không?",
                                $t('lz.message.base.confirmProcess'),
                                {
                                    confirmButtonText: $t('lz.button.base.yes'),
                                    cancelButtonText: $t('lz.button.base.no'),
                                    type: "warning",
                                }
                            );
                            // Nếu người dùng bấm "Đồng ý", result sẽ là "confirm"
                            if (result === "confirm") {
                                await getAPI(VmsCurrentApi).apiVmsVmscurrentRestartIncidentidPost(incidentId);
                                ElMessage.success($t('lz.message.base.processSuccess'));
                            }
                    } catch (error) {
                        // Người dùng bấm "Hủy" hoặc có lỗi xảy ra
                        ElMessage.success($t('lz.message.base.processUnSuccess'));
                    }

                    // - End - Add by KHOAND
                }
                else{
                    // Skipped | Canceled
                    response = await getAPI(TmsIncidentApi).apiTmsTmsincidentUpdatestatePost({Id: incidentId, State: newState});
                }

                // Reload all incident data => IMPORTANCE: IMPORTANCE: MUST BE CALLED HERE
                await loadIncidentsByAccount(); // Need to use the keyword "await" here

                if(response?.data.result == true){
                    if(nextActiveTab == 'verifiedIncidentTab'){
                        if(state.verifiedIncidentList?.length > 0)
                            state.activeTab = nextActiveTab;
                        else if(state.registrationIncidentList?.length > 0)
                            state.activeTab = 'registrationIncidentTab';
                        else 
                            setDefaultActiveTab();
                    }
                    else if(nextActiveTab == 'inProgressIncidentTab'){
                        if(state.inProgressIncidentList?.length > 0)
                            state.activeTab = nextActiveTab;
                        else if(state.registrationIncidentList?.length > 0)
                            state.activeTab = 'registrationIncidentTab';
                        else 
                            setDefaultActiveTab();
                    }
                    
                    ElMessage({message: $t('lz.message.base.processSuccess'), type: 'success'});

                    // Open a new report window 
                    if(newState == TmsIncidentStateEnum.Finished)
                        window.open(TmsReportRouteEnum.IncidentReportDetail + incidentId, '_blank');
                }
                else
                    ElMessage({message: $t('lz.message.base.processUnSuccess'), type: 'error'});
            }
        })
        .catch((error) => {
            console.error('incidentPopup.vue: updateIncidentState(...)', error);
        })
        .finally(() => {
            // Enable the controls
            if (ownsProcessing) setControlsToLoading(false);
        });
    };

    // Xóa toàn bộ dữ liệu sự cố + reset bộ đếm (ẩn FAB) khi không còn sự cố nào
    const resetIncidentData = () => {
        state.incidentList = null;
        state.registrationIncidentList = [];
        state.verifiedIncidentList = [];
        state.inProgressIncidentList = [];
        state.allProcessingIncident = [];
        state.totalIncidentCount = 0;
        state.shouldBlink = false;
        incidentMonitor.allIncidentByAccount = [];
        if(state.blinkTimeout) {
            clearTimeout(state.blinkTimeout);
            state.blinkTimeout = null;
        }
    };

    // Retrieve all incident data by account
    // This function will be used multiple times in various places
    let incidentRefreshInFlight: Promise<void> | null = null;
    let incidentRefreshQueued = false;
    let incidentRefreshOpen = false;
    let incidentRefreshDisposed = false;
    let incidentRefreshFailures = 0;
    let incidentRefreshTimer: ReturnType<typeof setTimeout> | undefined;
    const incidentAuthKey = () => String(Local.get('access-token') ?? Session.get('token') ?? '');
    const loadIncidentsByAccount = (autoOpen: boolean = false): Promise<void> => {
        incidentRefreshQueued = true;
        incidentRefreshOpen ||= autoOpen;
        if (incidentRefreshInFlight) return incidentRefreshInFlight;
        incidentRefreshInFlight = (async () => {
            while (incidentRefreshQueued && !incidentRefreshDisposed) {
                incidentRefreshQueued = false;
                const open = incidentRefreshOpen;
                incidentRefreshOpen = false;
                await fetchIncidentsByAccount(open);
            }
        })().finally(() => { incidentRefreshInFlight = null; });
        return incidentRefreshInFlight;
    };
    const fetchIncidentsByAccount = async(autoOpen: boolean = false) => {
        const authKey = incidentAuthKey();
        const isCurrentAccount = () => !incidentRefreshDisposed && !!authKey && authKey === incidentAuthKey();
        try{
            console.log('[IncidentPopup] loadIncidentsByAccount called with autoOpen:', autoOpen);
            
            if(!isUserLoggedIn()) {
                resetIncidentData();
                automationStore.clear();
                return;
            }

            await loadDefaultData();
            let userCanView = await incidentMonitor.userCanViewData(); // Must use the keyword "await" here
           // console.log('[IncidentPopup] userCanView:', userCanView);
            
            if (!isCurrentAccount()) return;
            if(!userCanView){
                // Không còn sự cố nào để xử lý => xóa dữ liệu cũ + reset count để ẩn FAB
                resetIncidentData();
                incidentMonitor.isShowIncidentPopup = false;
                // SonTH - 24/07/2026: dọn luôn banner tự động, nếu không nó sẽ treo lại trên màn hình
                automationStore.clear();
               // console.log('[IncidentPopup] User cannot view incidents, dialog closed');
                return;
            }

            // Retrieve all allowed actions related to the incident by account
            let response = await getAPI(TmsIncidentApi).apiTmsTmsincidentListallowedactionPost();
            if (!isCurrentAccount()) return;
            incidentMonitor.allAllowedActionByAccount = response.data.result;

            response = await getAPI(TmsIncidentApi).apiTmsTmsincidentListbyaccountPost();
            if (!isCurrentAccount()) return;
            incidentRefreshFailures = 0;
            const previousIds = (incidentMonitor.allIncidentByAccount ?? []).map((x: any) => x.incident?.id).join(',');
            state.incidentList = response.data.result;

            state.registrationIncidentList = state.incidentList?.registrationIncidentList;
            state.verifiedIncidentList = state.incidentList?.verifiedIncidentList;
            state.inProgressIncidentList = state.incidentList?.inProgressIncidentList;
            state.allProcessingIncident = state.incidentList?.allProcessingIncident;

            // Author: SonTH - 24/07/2026
            // Tách nhánh tự động hóa từ CÙNG response này sang store để banner cảnh báo toàn cục hiển thị.
            // Luồng thủ công phía trên không đổi; banner không tự gọi API nào để tránh có hai nguồn dữ liệu.
            automationStore.syncFromIncidentList(state.incidentList);

            // Prefetch camera liên quan cho tất cả sự cố (theo equipmentId) => hiển thị nút camera đúng
            await buildIncidentCamerasMap();
            if (!isCurrentAccount()) return;

            // Calculate total incident count - include all types
            state.totalIncidentCount = 
                (state.registrationIncidentList?.length || 0) +
                (state.verifiedIncidentList?.length || 0) +
                (state.inProgressIncidentList?.length || 0) +
                (state.allProcessingIncident?.length || 0);

            // console.log('[IncidentPopup] Total incident count:', state.totalIncidentCount, {
            //     registration: state.registrationIncidentList?.length || 0,
            //     verified: state.verifiedIncidentList?.length || 0,
            //     inProgress: state.inProgressIncidentList?.length || 0,
            //     allProcessing: state.allProcessingIncident?.length || 0
            // });

            // Load all incidents into a global variable
            incidentMonitor.allIncidentByAccount = [];
            state.registrationIncidentList?.forEach(item => {
                item.hasWorkExecute = false;
                incidentMonitor.allIncidentByAccount.push(item);
            });

            state.verifiedIncidentList?.forEach(item => {
                item.hasWorkExecute = false;
                incidentMonitor.allIncidentByAccount.push(item);
            });

            state.inProgressIncidentList?.forEach(item => {
                item.hasWorkExecute = true;
                incidentMonitor.allIncidentByAccount.push(item);
            });

            state.allProcessingIncident?.forEach(item => {
                item.hasWorkExecute = false;
                incidentMonitor.allIncidentByAccount.push(item);
            });

            // Only auto-open if explicitly requested (first time load) and has incidents
            if(autoOpen && state.totalIncidentCount > 0) {
                console.log('[IncidentPopup] Opening dialog - autoOpen=true and has incidents');
                incidentMonitor.isShowIncidentPopup = true;
            } else if(!autoOpen && state.totalIncidentCount > 0) {
                console.log('[IncidentPopup] Not auto-opening, triggering blink animation');
                // Trigger blink animation when not auto-opening
                if(!incidentMonitor.isShowIncidentPopup && previousIds !== incidentMonitor.allIncidentByAccount.map((x: any) => x.incident?.id).join(',')) {
                    triggerBlinkAnimation();
                }
            } else {
                console.log('[IncidentPopup] Not opening dialog - no incidents or conditions not met');
            }

            // Second: Activate the default tab if the current tab has no data
            if(!checkIncidentExistsByActiveTab())
                setDefaultActiveTab();
        }
        catch(error){
            incidentRefreshFailures++;
            console.error('incidentPopup.vue: loadIncidentsByAccount(...)', error);
        }
    };

    const forceActiveSelectedIncidentTab = () => {
        if(state.registrationIncidentList?.some(item => item?.incident?.id == incidentMonitor.selectedIncidentId)){
            
            if(state.registrationIncidentList?.length > 1){

                const incidentIndex = state.registrationIncidentList?.findIndex(item => item?.incident?.id == incidentMonitor.selectedIncidentId);

                const selectedList = state.registrationIncidentList?.slice(incidentIndex, incidentIndex + 1);

                if(selectedList?.length > 0){
                    state.registrationIncidentList?.splice(incidentIndex, 1);
                    state.registrationIncidentList?.unshift(selectedList[0]);
                }
            }

            state.activeTab = 'registrationIncidentTab';

            return;
        }

        if(state.verifiedIncidentList?.some(item => item?.incident?.id == incidentMonitor.selectedIncidentId)){
            
            if(state.verifiedIncidentList?.length > 1){
                const incidentIndex = state.verifiedIncidentList?.findIndex(item => item?.incident?.id == incidentMonitor.selectedIncidentId);
                
                const selectedList = state.verifiedIncidentList?.slice(incidentIndex, incidentIndex + 1);

                if(selectedList?.length > 0){
                    state.verifiedIncidentList?.splice(incidentIndex, 1);
                    state.verifiedIncidentList?.unshift(selectedList[0]);
                }
            }

            state.activeTab = 'verifiedIncidentTab';
            
            return;
        }

        if(state.inProgressIncidentList?.some(item => item?.incident?.id == incidentMonitor.selectedIncidentId)){

            if(state.inProgressIncidentList?.length > 1){
                const incidentIndex = state.inProgressIncidentList?.findIndex(item => item?.incident?.id == incidentMonitor.selectedIncidentId);

                const selectedList = state.inProgressIncidentList?.slice(incidentIndex, incidentIndex + 1);

                if(selectedList?.length > 0){
                    state.inProgressIncidentList?.splice(incidentIndex, 1);
                    state.inProgressIncidentList?.unshift(selectedList[0]);
                }
            }

            state.activeTab = 'inProgressIncidentTab';
            
            return;
        }
    }

    // Retrieve the default data 
    // Just call this function only once
    const loadDefaultData = async() => {
        try{
            let response = null;
			// TODO: LaneList is temporarily DirectionList => by direction_config_type not tms_lane
            if( state.directionList && state.directionList.length < 1)
			    state.directionList = getConfigDataByCode(BaseConfigTypeEnum.TmsDirectionType);

            // Get all traffic-plan
            if(state.trafficPlanList && state.trafficPlanList.length < 1){
                response = await getAPI(TmsTrafficPlanApi).apiTmsTmstrafficplanListvalidtrafficplansPost();
                state.trafficPlanList = response.data.result;
            }

            // Get all event-type
            if(state.eventTypeList && state.eventTypeList.length < 1){

                if(mapStore.eventTypeList && mapStore.eventTypeList.length < 1)
                {
                    response = await getAPI(TmsEventTypeApi).apiTmsTmseventtypeListGet();
                    state.eventTypeList = response.data.result;
                    //console.log("incident pop-up  state.eventTypeList again",  state.eventTypeList)
                }
                else
                {
                    state.eventTypeList = [...mapStore.eventTypeList]
                    //console.log("incident pop-up  state.eventTypeList map store",  state.eventTypeList)
                }
            }

            // Get all equipment-type
            if(state.equipmentTypeList && state.equipmentTypeList.length < 1)
            {
                if(mapStore.equipmentTypeList && mapStore.equipmentTypeList.length < 1)
                {
                    response = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet();
                    state.equipmentTypeList = response.data.result;
                    //console.log("incident pop-up  state.equipmentTypeList again",  state.equipmentTypeList)
                }
                else
                {
                    state.equipmentTypeList = [...mapStore.equipmentTypeList]
                    //console.log("incident pop-up  state.equipmentTypeList map store",  state.equipmentTypeList)
                }
            }

            // Get all cameras
            if(state.equipmentList && state.equipmentList.length < 1){

                if(mapStore.equipmentList && mapStore.equipmentList.length < 1)
                {
                    response = await getAPI(TmsEquipmentApi).apiTmsTmsequipmentListGet();
                    state.equipmentList = response.data.result;
                }
                else
                {
                    state.equipmentList = [...mapStore.equipmentList]
                }
            }

            if(state.stateColorList && state.stateColorList.length < 1)
			    state.stateColorList = getConfigDataByCode(BaseConfigTypeEnum.TmsEquipmentStateColor);
        }
        catch(error){
            console.error('incidentPopup.vue: loadDefaultData(...)', error);
        }
    };

    // Show camera function
	const showCameraPopup = (cameraInfo: any) => {
		if(cameraInfo)
			cameraPopupCompRef.value?.openDialog(cameraInfo);
		else
			ElMessage.error($t('lz.label.base.noData'));
	};

    // Lấy danh sách camera liên quan của 1 sự cố (đã prefetch trong incidentCamerasMap).
    // Fallback: lọc từ equipmentList đã tải (trường hợp map chưa build kịp).
    const getIncidentCameras = (incident: any) => {
        const incidentId = incident?.id;
        if (incidentId && state.incidentCamerasMap && state.incidentCamerasMap[incidentId]) {
            return state.incidentCamerasMap[incidentId];
        }

        const equipmentId = incident?.equipmentId;
        if (!equipmentId || String(equipmentId).length === 0) return [];

        const ids = String(equipmentId)
            .split(',')
            .map((x: string) => x.trim().toLowerCase())
            .filter((x: string) => x.length > 0);

        return (state.equipmentList ?? []).filter((equip: any) => ids.includes(String(equip.id).trim().toLowerCase()));
    };

    // Prefetch danh sách camera cho TẤT CẢ sự cố theo equipmentId.
    // equipmentId của sự cố có thể không nằm trong equipmentList của map (bị giới hạn phạm vi),
    // nên phải gọi API listbyids để lấy đúng thiết bị theo id.
    const buildIncidentCamerasMap = async () => {
        const allIncidents = [
            ...(state.registrationIncidentList ?? []),
            ...(state.verifiedIncidentList ?? []),
            ...(state.inProgressIncidentList ?? []),
            ...(state.allProcessingIncident ?? []),
        ];

        console.log('[IncidentPopup] buildIncidentCamerasMap - list counts:', {
            registration: state.registrationIncidentList?.length ?? 0,
            verified: state.verifiedIncidentList?.length ?? 0,
            inProgress: state.inProgressIncidentList?.length ?? 0,
            allProcessing: state.allProcessingIncident?.length ?? 0,
            totalIncidents: allIncidents.length,
        });

        // Lấy TOÀN BỘ thiết bị 1 lần rồi map theo id (không gọi API từng sự cố)
        let allEquipment: any[] = [];
        try {
            const res = await getAPI(TmsEquipmentApi).apiTmsTmsequipmentListGet();
            allEquipment = res?.data?.result ?? [];
            // Cập nhật lại equipmentList đầy đủ (mapStore có thể bị giới hạn phạm vi -> thiếu camera)
            if (allEquipment.length > 0) {
                state.equipmentList = allEquipment;
            }
        } catch (error) {
            console.error('[IncidentPopup] buildIncidentCamerasMap - load all equipment error:', error);
            allEquipment = state.equipmentList ?? [];
        }

        // Dictionary id -> equipment (chuẩn hoá id: trim + lowercase)
        const equipmentById: Record<string, any> = {};
        allEquipment.forEach((e: any) => {
            equipmentById[String(e.id).trim().toLowerCase()] = e;
        });

        // Bảng chuyển đổi zoneEquipmentId -> equipmentId
        // (dữ liệu cũ: incident.equipmentId lưu id của TmsZoneEquipment, cần map về id thiết bị)
        const zoneEqToEquipId: Record<string, string> = {};
        try {
            const zeRes = await getAPI(TmsZoneEquipmentApi).apiTmsTmszoneequipmentListGet();
            (zeRes?.data?.result ?? []).forEach((ze: any) => {
                if (ze?.id && ze?.equipmentId) {
                    zoneEqToEquipId[String(ze.id).trim().toLowerCase()] = String(ze.equipmentId).trim().toLowerCase();
                }
            });
        } catch (error) {
            console.error('[IncidentPopup] buildIncidentCamerasMap - load zoneEquipment mapping error:', error);
        }

        console.log('[IncidentPopup] buildIncidentCamerasMap - tổng thiết bị:', allEquipment.length, '| zoneEquipment mapping:', Object.keys(zoneEqToEquipId).length);

        // Giải id: nếu là id thiết bị -> dùng luôn; nếu là id zoneEquipment -> chuyển về equipmentId tương ứng
        const resolveEquipment = (rawId: string) => {
            const id = String(rawId).trim().toLowerCase();
            if (equipmentById[id]) return equipmentById[id];
            const mappedEquipId = zoneEqToEquipId[id];
            if (mappedEquipId && equipmentById[mappedEquipId]) return equipmentById[mappedEquipId];
            return null;
        };

        // Build map incidentId -> cameras[] bằng cách map từ list đã tải
        const map: Record<string, any[]> = {};
        allIncidents.forEach((iwd: any) => {
            const inc = iwd?.incident;
            if (!inc?.id) return;

            const eid = inc.equipmentId;
            if (!eid || String(eid).length === 0) {
                map[inc.id] = [];
                return;
            }

            const ids = String(eid)
                .split(',')
                .map((x: string) => x.trim())
                .filter((x: string) => x.length > 0);

            map[inc.id] = ids.map((id: string) => resolveEquipment(id)).filter(Boolean);
        });

        state.incidentCamerasMap = map;

        console.log('[IncidentPopup] buildIncidentCamerasMap - DONE. map:', map);
    };

    /**
     * Author: SonTH
     * Description: Sự cố này có đang chờ luồng tự động thực thi hay không.
     *              Dùng để khóa thao tác thủ công, tránh người trực và automation cùng tác động
     *              lên một sự cố. Chỉ khóa ở trạng thái đang đếm ngược (WarningPending); khi automation
     *              đã chạy xong thì sự cố quay về luồng thủ công bình thường.
     * Created date: 24/07/2026
     */
    const isAutomationPending = (iwd: any) => iwd?.automation?.status == AutomationRunStatus.WarningPending;

    // KienLT - 16/10/2026: run tự động gần nhất của sự cố đã lỗi và BE đã bàn giao cho người trực (bước lỗi về Chờ).
    const isAutomationFailed = (iwd: any) => iwd?.automation?.status == AutomationRunStatus.Failed;
    const isAutomationHandOverWork = (we: any) =>
        we?.executionSource == 'Automatic' && we?.state == TmsWorkExecuteStateEnum.Waiting && !!we?.remark;
    // Đổi mã lỗi kỹ thuật của luồng tự động sang câu dễ đọc (dịch đúng một lần ở FE); không nhận ra thì hiện nguyên văn.
    // BE: ErrorMessage của UnexpectedError là thông điệp gốc; Remark bước bàn giao có tiền tố "AutoHandOver: ".
    const formatAutomationReason = (errorCode?: string | null, message?: string | null) => {
        const text = String(message ?? '')
            .replace(/^AutoHandOver:\s*/, '')
            .replace(/^AutoDeviceControlFailed:\s*/, '')
            .trim();
        const occupied = text.match(/IncidentControlOccupied:([^;]*)/);
        if (occupied) return $t('lz.message.tmsIncidentAutomation.reasonOccupied', { devices: occupied[1].split(',').join(', ') });
        if (errorCode == 'StuckTimeout') return $t('lz.message.tmsIncidentAutomation.reasonStuck');
        if (errorCode == 'UnexpectedError') return $t('lz.message.tmsIncidentAutomation.reasonUnexpected', { detail: text });
        return text || $t('lz.message.tmsIncidentAutomation.reasonUnknown');
    };

    // Determine if the incident list has data
    const hasNoIncident = () => {
        return (state.registrationIncidentList?.length ?? 0) == 0
            && (state.verifiedIncidentList?.length ?? 0) == 0
            && (state.inProgressIncidentList?.length ?? 0) == 0
            && (state.allProcessingIncident?.length ?? 0) == 0;
    };

    function getEventType(eventTypeId: any){
        if(!eventTypeId)
            return null;

        return state.eventTypeList?.find(item => item.id == eventTypeId);
    }

    function getIncidentIcon(eventTypeId: any){
        const eventType = getEventType(eventTypeId);

        return eventType?.url ? commonVarsAndFuncs.getFullUrl(eventType.url) : commonVarsAndFuncs.getFullUrl(UploadAssetItemsEnum.AttentionIcon);
    }

    // Get the organization name by organization id
    const getOrganizationName = (organiztionId: string) => {
        return commonVarsAndFuncs.userOrgList?.find(item => item.orgId == organiztionId)?.orgName;
    };

    // Verify Incident ===================================================
    
    const verifyIncident = async(incidentId: string) => {
        await updateIncidentState(incidentId, TmsIncidentStateEnum.Verified, 'verifiedIncidentTab');
    };

    const verifyAllIncidents = async() => {
        await updateIncidentState('All', TmsIncidentStateEnum.Verified, 'verifiedIncidentTab');
    };

    const skipIncident = async(incidentId: string) => {
        await updateIncidentState(incidentId, TmsIncidentStateEnum.Skipped, 'registrationIncidentTab');
    };

    const skipAllIncidents = async() => {
        await updateIncidentState('All', TmsIncidentStateEnum.Skipped, 'registrationIncidentTab');
    };

    // InProgess Incident (Map Trafficplan) ===================================

    const applyIncidentTrafficPlan = async(incidentId: string) => {
        await updateIncidentState(incidentId, TmsIncidentStateEnum.InProgress, 'inProgressIncidentTab');
    };

    const cancelIncident = async(incidentId: string) => {
        await updateIncidentState(incidentId, TmsIncidentStateEnum.Canceled, 'verifiedIncidentTab');
    };

    const cancelAllIncidents = async() => {
        await updateIncidentState('All', TmsIncidentStateEnum.Canceled, 'verifiedIncidentTab');
    };

    // WorkExecute Incidents ====================================================
    const updateWorkExecute = async(workExecId: string, newState: string) => {
        if (state.isProcessing) return;
        if(!workExecId || !newState){
            alertMessage($t('lz.message.base.invalidData'), AlertTypeEnum.Warning);

            return;
        }

        let messageConfirm = null;

        if(newState == TmsWorkExecuteStateEnum.Executed)
            messageConfirm = $t('lz.message.tmsWorkExecute.executeWorkConfirm');
        else if(newState == TmsWorkExecuteStateEnum.Skipped)
            messageConfirm = $t('lz.message.tmsWorkExecute.skipWorkConfirm');

        let ownsProcessing = false;
        return ElMessageBox.confirm(
            messageConfirm,
            $t('lz.message.base.confirmProcess'),
            {
                showCancelButton: true,
                confirmButtonText: $t('lz.button.base.confirm'),
                cancelButtonText: $t('lz.button.base.cancel'),
                type: 'warning',
                customStyle: {
                    'z-index': ZIndexHighestEnum.Max
                },
                customClass: 'z-index-highest--max',
            },
            appContext
        )
        .then(async() => {
            if (state.isProcessing) return;
            ownsProcessing = true;
            // Disable the controls
            setControlsToLoading(true);

            const response = await getAPI(TmsWorkExecuteApi).apiTmsTmsworkexecuteUpdatestatePost({id: workExecId, state: newState, remark: state.workExecuteRemarkList[workExecId]});

            // Reload all incident data => IMPORTANCE: MUST BE CALLED HERE
            await loadIncidentsByAccount(); // Need to use the keyword "await" here

            if(response?.data.result == true)
                ElMessage({message: $t('lz.message.base.processSuccess'), type: 'success'});
            else
                ElMessage({message: $t('lz.message.base.processUnSuccess'), type: 'error'});
        })
        .catch((error) => {
            console.log('incidentPopup.vue: updateWorkExecute(...)', error);
        })
        .finally(() => {
            // Enable the controls
            if (ownsProcessing) setControlsToLoading(false);
        });
    };

    const verifyWorkExecute = async(workExecuteId: string, controlType: string, incidentId: string) => {
        // VMS -> modal điều khiển VMS (mặc định Text); LCS -> modal điều khiển LCS (chỉ hình ảnh)
        if(controlType == TmsControlTypeEnum.Vms){
            showVmsModal(workExecuteId, incidentId);
        }
        else if(controlType == TmsControlTypeEnum.Lcs){
            showLcsModal(workExecuteId, incidentId);
        }
        else{
            await updateWorkExecute(workExecuteId, TmsWorkExecuteStateEnum.Executed);
        }
    }

    const skipWorkExecute = async(incidentId: string) => {
        await updateWorkExecute(incidentId, TmsWorkExecuteStateEnum.Skipped);
    }

    const showVmsModal = (workExecuteId: string, incidentId: string) => {
        let incidentInfo = null;
        state.inProgressIncidentList?.forEach(item => {
            if(item.incident?.id == incidentId){
                incidentInfo = item.incident;
            }
        });

        const eventTypeCode = state.eventTypeList?.find(item => item.id == incidentInfo?.eventTypeId)?.code;
        const workExecuteRemark = workExecuteId ? state.workExecuteRemarkList[workExecuteId] : null;

        vmsModalCompRef.value?.openDialog({incidentInfo: incidentInfo, actionType: VmsActionType.Text, workExecuteId: workExecuteId, workExecuteRemark: workExecuteRemark, eventTypeCode: eventTypeCode});
    };

    // Mở modal điều khiển LCS theo sự cố (LCS chỉ hỗ trợ hình ảnh -> actionType = Image)
    const showLcsModal = (workExecuteId: string, incidentId: string) => {
        let incidentInfo = null;
        state.inProgressIncidentList?.forEach(item => {
            if(item.incident?.id == incidentId){
                incidentInfo = item.incident;
            }
        });

        const eventTypeCode = state.eventTypeList?.find(item => item.id == incidentInfo?.eventTypeId)?.code;
        const workExecuteRemark = workExecuteId ? state.workExecuteRemarkList[workExecuteId] : null;

        lcsModalCompRef.value?.openDialog({incidentInfo: incidentInfo, actionType: VmsActionType.Image, workExecuteId: workExecuteId, workExecuteRemark: workExecuteRemark, eventTypeCode: eventTypeCode});
    };

    // Reload all incident data when the vmsModal is closed
    const onVmsModalSubmitSuccess = () => {
        loadIncidentsByAccount();
    };

    // Refresh work execute list for specific incident
    const refreshWorkExecuteList = async(incidentId: string) => {
        if(!incidentId || state.isProcessing) return;
        
        setControlsToLoading(true);
        
        try {
            await loadIncidentsByAccount();
            ElMessage.success($t('lz.message.base.refreshSuccess'));
        } catch(error) {
            console.error('incidentPopup.vue: refreshWorkExecuteList(...)', error);
            ElMessage.error($t('lz.message.base.refreshFailed'));
        } finally {
            setControlsToLoading(false);
        }
    };

    // Auto WorkExecute
    const autoWorkExecute = async(workExecuteId: string, controlType: string, incidentId: string) => {
        ElMessageBox.alert("Chức năng đang phát triển");
    }

    // Finished Incidents ======================================================
    const finishedIncident = async(incidentId: string) => {
        await updateIncidentState(incidentId, TmsIncidentStateEnum.Finished);
    };

    // One HTTP fallback for the shared popup/banner; no SSE is added to the management list.
    const refreshOnFocus = () => { if (!document.hidden) void loadIncidentsByAccount(false); };
    const scheduleIncidentRefresh = () => {
        if (incidentRefreshDisposed) return;
        const base = automationStore.hasPendingRuns ? 3000 : 10000;
        const delay = Math.min(60000, base * 2 ** Math.min(incidentRefreshFailures, 3));
        incidentRefreshTimer = setTimeout(async () => {
            if (!document.hidden) await loadIncidentsByAccount(false);
            scheduleIncidentRefresh();
        }, delay);
    };

    // General Functions ======================================================

    // Handle when component is mounted to the DOM
    onMounted(async() => {
        incidentRefreshDisposed = false;
        scheduleIncidentRefresh();
        window.addEventListener('focus', refreshOnFocus);
        window.addEventListener('online', refreshOnFocus);
        document.addEventListener('visibilitychange', refreshOnFocus);
        
        // Auto-load incidents on initial page load (when component first mounts)
        if (!state.hasInitialLoaded) {
            console.log('[IncidentPopup] Initial mount - calling loadIncidentsByAccount(true)');
            state.hasInitialLoaded = true;
            // Reset collapsed state on page refresh/login
            incidentMonitor.isCollapsed = false;
            console.log('[IncidentPopup] Initial load - isCollapsed reset to false');
            await loadIncidentsByAccount(true);
        }
        
        // Listen for page load event - auto open dialog (backup mechanism)
        (mittBus as any).on('loadIncidentsOnPageLoad', async(data: any) => {
            console.log('[IncidentPopup] Received loadIncidentsOnPageLoad event', data);
            // Skip if already loaded
            if (state.hasInitialLoaded) {
                console.log('[IncidentPopup] Already loaded initially, skipping');
                return;
            }
            state.hasInitialLoaded = true;
            await loadIncidentsByAccount(data?.autoOpen ?? true);
        });
        
        // Listen for route change event - respect collapsed state
        (mittBus as any).on('loadIncidentsOnRouteChange', async(data: any) => {
            console.log('[IncidentPopup] Received loadIncidentsOnRouteChange event', data);
            // If collapsed, just show FAB and blink. If not collapsed, keep dialog open
            const shouldOpen = !incidentMonitor.isCollapsed;
            console.log('[IncidentPopup] isCollapsed:', incidentMonitor.isCollapsed, 'shouldOpen:', shouldOpen);
            await loadIncidentsByAccount(shouldOpen);
        });
    });

    // Clean up event listeners on unmount
    onUnmounted(() => {
        incidentRefreshDisposed = true;
        clearTimeout(incidentRefreshTimer);
        window.removeEventListener('focus', refreshOnFocus);
        window.removeEventListener('online', refreshOnFocus);
        document.removeEventListener('visibilitychange', refreshOnFocus);
        (mittBus as any).off('loadIncidentsOnPageLoad');
        (mittBus as any).off('loadIncidentsOnRouteChange');
        if (scrollObserver) scrollObserver.disconnect();
    });

    // Monitor to ensure data changes are always detected
    // This is necessary because the data is loaded asynchronously outside the onMounted
    watch(() => [state.registrationIncidentList, state.verifiedIncidentList, state.inProgressIncidentList, state.allProcessingIncident], () => {
        // Update total count - include all types
        state.totalIncidentCount = 
            (state.registrationIncidentList?.length || 0) +
            (state.verifiedIncidentList?.length || 0) +
            (state.inProgressIncidentList?.length || 0) +
            (state.allProcessingIncident?.length || 0);
        
        // Hide the modal if there are no incidents
        if(!isUserLoggedIn() || hasNoIncident()) {
            incidentMonitor.isShowIncidentPopup = false;
            state.shouldBlink = false;
        }
    }, true);

    // Monitor the active tab to change dialog width
    watch(() => state.activeTab, () => {
        if(state.activeTab == 'inProgressIncidentTab' || !state.activeTab)
            state.dialogWidth = '90%';
        else
            state.dialogWidth = '60%';
    }, true);

    // Lắng nghe sự cố realtime qua mittBus (NATS) => thay cho watch store cũ
    useTransporterIncident(async (data) => {
        // Sự kiện từ luồng tự động (AutomationRun) → chỉ refresh data, KHÔNG mở lại modal
        // nếu người dùng đã thu gọn xuống FAB. Tránh quấy rầy khi automation đang chạy nền.
        const isAutoEvent = data?.broadcastObject == IncidentBroadcastObjectEnum.AutomationRun;

        // Event và incident đều dùng hàng chờ TMS theo quyền.
        if(!isUserLoggedIn())
            return;

        // Load dữ liệu mới nhất trước, chưa mở modal
        await loadIncidentsByAccount(false);
        console.log('[IncidentPopup] Nats update - đã tải lại danh sách sự cố', {
            dangKy: state.registrationIncidentList?.length ?? 0,
            daXacNhan: state.verifiedIncidentList?.length ?? 0,
            tong: state.totalIncidentCount ?? 0,
            code: data?.code ?? data?.broadcastCode,
        });

        // Chỉ tự mở lại modal khi:
        // 1. Có sự cố ở trạng thái cần xử lý ngay (tạo mới, xác thực)
        // 2. KHÔNG phải sự kiện từ luồng tự động
        // Trừ trạng thái "đang xử lý" — không cần quấy rầy người trực khi chỉ có update tiến trình
        const hasUrgentIncidents = (state.registrationIncidentList?.length > 0) || (state.verifiedIncidentList?.length > 0);
        if(hasUrgentIncidents && !incidentMonitor.isShowIncidentPopup && !isAutoEvent) {
            incidentMonitor.isCollapsed = false;
            incidentMonitor.isShowIncidentPopup = true;
            console.log('[IncidentPopup] Nats update - auto-opening dialog (has registration/verified incidents)');
        } else if(!hasUrgentIncidents || isAutoEvent) {
            // Chỉ có inProgress/allProcessing hoặc là sự kiện auto → nhấp nháy FAB, không mở modal
            if(!incidentMonitor.isShowIncidentPopup && state.totalIncidentCount > 0) {
                triggerBlinkAnimation();
            }
            console.log('[IncidentPopup] Nats update - keeping FAB (isAutoEvent:', isAutoEvent, ')');
        } else {
            console.log('[IncidentPopup] Nats update - dialog already open, data refreshed');
        }

        // Notification the broadcastMessage
        const notificationKey = broadcastNotificationKey(data);
        if (notificationKey) {
            const message = data.broadcastMessage;
            const isAutomationActor = message.Account == AUTOMATION_SYSTEM_ACTOR;
            ElNotification({
                title: $t('lz.label.base.info'),
                dangerouslyUseHTMLString: true,
                message: $t(notificationKey, {
                    Account: isAutomationActor ? $t('lz.label.tmsIncidentAutomation.systemActor') : message.Account,
                    // Bản tin luồng tự động chỉ mang IncidentId ⇒ tra tên trong danh sách vừa nạp.
                    Incident: message.Incident ?? incidentNameOf(message.IncidentId),
                    Work: message.Work,
                }),
                type: message.RunStatus == AutomationRunStatus.Failed ? 'warning' : undefined,
                customClass: 'incident-broadcast-notification--small',
                // zIndex: ZIndexHighestEnum.Max,
                duration: 5 * 1000,
            });
        }
    });

    const incidentNameOf = (incidentId?: string) =>
        incidentMonitor.allIncidentByAccount?.find((item: any) => item?.incident?.id === incidentId)?.incident?.name ?? '';

    /**
     * Key i18n của thông báo nổi cho một bản tin kênh sự cố, hoặc null nếu không bật thông báo.
     *
     * Luồng tự động làm nhiều bước liền nhau (xác thực, gán kịch bản, từng công việc...) — mỗi bước một bản tin, có lúc
     * 6-8 thông báo chồng nhau. Nên với luồng tự động chỉ báo lúc BẮT ĐẦU (lập lịch, WarningPending) và lúc KẾT THÚC
     * (hệ thống đóng sự cố) hoặc THẤT BẠI (Failed). Các bước giữa vẫn làm mới popup ở trên, chỉ không bật thông báo.
     * Huỷ lượt tự động luôn đi kèm một thao tác tay (bỏ qua / huỷ / kết thúc / tự gán kịch bản) đã có thông báo riêng.
     */
    const broadcastNotificationKey = (data: any): string | null => {
        const message = data?.broadcastMessage;
        if (!message?.Message) return null;
        if (data?.broadcastObject == IncidentBroadcastObjectEnum.AutomationRun) {
            if (message.RunStatus == AutomationRunStatus.WarningPending) return 'lz.message.tmsIncidentAutomation.runStartedNotify';
            if (message.RunStatus == AutomationRunStatus.Failed) return 'lz.message.tmsIncidentAutomation.runFailedNotify';
            return null;
        }
        if (message.Account == AUTOMATION_SYSTEM_ACTOR && message.Message != 'lz.message.tmsIncident.finishedNotify') return null;
        return message.Message;
    };

    watch(() => incidentMonitor.selectedIncidentId, () => {
        if(incidentMonitor.selectedIncidentId?.length > 0)
            forceActiveSelectedIncidentTab();
    }, true);

    const getEquipmentStateColor = (equipState: string) => {
        let stateGroup = '';
        switch(equipState?.toString()){
            case '0': // InActive
            case '4': // Cannot Connected
                stateGroup = 'inactive';
                break;

            case '1': // Active
                stateGroup = 'active';
                break;

            case '2': // Error
            case '7': // Pixel dead
                stateGroup = 'error';
                break;

            case '3': // Fault
            case '5': // Power fault	fault
                stateGroup = 'fault';
                break;
        }

        const config = stateGroup ? getConfigItemByCode(BaseConfigTypeEnum.TmsEquipmentStateColor, stateGroup) : null;
        
        return config?.value ?? '#909399';
    };

    // Check if all previous steps (lower step numbers) are completed (Executed or Skipped)
    const isPreviousStepsCompleted = (workExecuteList: any[], currentStep: number) => {
        if (!workExecuteList || workExecuteList.length === 0) return true;
        
        // Find all steps with lower step numbers than current step
        const previousSteps = workExecuteList.filter(we => we.step < currentStep);
        
        // If there are no previous steps, this step can be executed
        if (previousSteps.length === 0) return true;
        
        // Check if all previous steps are either Executed or Skipped
        return previousSteps.every(we => 
            we.state === TmsWorkExecuteStateEnum.Executed || 
            we.state === TmsWorkExecuteStateEnum.Skipped
        );
    };

    // active = success, inactive = info, error = warning, fault = danger

    // Handle on dialog is closed
    const handleClose = () => {
        incidentMonitor.selectedIncidentId = null; // Reset incident info
        incidentMonitor.isShowIncidentPopup = false; // => IMPORTANCE
        incidentMonitor.isCollapsed = true; // Set collapsed state when user closes dialog
        console.log('[IncidentPopup] Dialog closed by user, isCollapsed set to true');
        state.shouldBlink = false; // Stop blinking when dialog is closed
        if(state.blinkTimeout) {
            clearTimeout(state.blinkTimeout);
            state.blinkTimeout = null;
        }
    };

    const handleOpen = async() => {
        await loadDefaultData(); // Must use the keyword "await" here
        state.shouldBlink = false; // Stop blinking when dialog is opened
        if(state.blinkTimeout) {
            clearTimeout(state.blinkTimeout);
            state.blinkTimeout = null;
        }
    }

    // Local tracking for timeline active state (does NOT reorder the list)
    const activeTimelineId = ref<string | null>(null);

    // Scroll to a specific incident card and highlight timeline node
    const scrollToIncident = (incidentId: string) => {
        const el = document.getElementById('incident-card-' + incidentId);
        if (el) {
            el.scrollIntoView({ behavior: 'smooth', block: 'center' });
            activeTimelineId.value = incidentId;
        }
    };

    // Sync timeline active node when main content scrolls
    const inprogressContentRef = ref<HTMLElement | null>(null);
    let scrollObserver: IntersectionObserver | null = null;

    const setupScrollObserver = () => {
        if (scrollObserver) scrollObserver.disconnect();

        const container = document.querySelector('.inprogress-content');
        if (!container) return;

        scrollObserver = new IntersectionObserver(
            (entries) => {
                for (const entry of entries) {
                    if (entry.isIntersecting) {
                        const id = entry.target.id.replace('incident-card-', '');
                        if (id) activeTimelineId.value = id;
                    }
                }
            },
            { root: container, rootMargin: '-40% 0px -40% 0px', threshold: 0 }
        );

        container.querySelectorAll('[id^="incident-card-"]').forEach((el) => {
            scrollObserver!.observe(el);
        });
    };

    // Watch list changes to re-setup observer
    watch(() => state.inProgressIncidentList, () => {
        if (state.inProgressIncidentList?.length > 0) {
            nextTick(() => setupScrollObserver());
        }
    }, { deep: true });

    // Open incident dialog from FAB
    const openIncidentDialog = () => {
        incidentMonitor.isShowIncidentPopup = true;
        incidentMonitor.isCollapsed = false; // Clear collapsed state when user opens dialog
        console.log('[IncidentPopup] Dialog opened from FAB, isCollapsed set to false');
    };

    // Trigger blink animation
    const triggerBlinkAnimation = () => {
        state.shouldBlink = true;
        
        // Clear existing timeout if any
        if(state.blinkTimeout) {
            clearTimeout(state.blinkTimeout);
        }
        
        // Stop blinking after 3 seconds
        state.blinkTimeout = setTimeout(() => {
            state.shouldBlink = false;
        }, 3000);
    };

    // Check if the user has logged in
    const isUserLoggedIn = () => {
	    if((route.path?.replaceAll('/', '').length ?? 0) == 0 || route.path?.includes('login'))
		    return false;

        return Local.get('access-token')?.length > 0 || Session.get('token')?.length > 0;
    }

    // // Check if the current user can access the data
    // const userCanViewData = async() => {
    //     let response = await getAPI(TmsIncidentApi).apiTmsTmsincidentCheckincidentPost();

    //     return response.data.result;
    // }

    // Show hide the processing status
    const blockProcessingScroll = (event: Event) => {
        if (state.isProcessing) event.preventDefault();
    };

    const setControlsToLoading = (isProcessing: boolean) => {
        state.isProcessing = isProcessing;
    }

    // Check incident data by active tab
    const checkIncidentExistsByActiveTab = () => {
        if(state.activeTab == 'inProgressIncidentTab')
            return state.inProgressIncidentList?.length > 0;

        if(state.activeTab == 'verifiedIncidentTab')
            return state.verifiedIncidentList?.length > 0;

        if(state.activeTab == 'registrationIncidentTab')
            return state.registrationIncidentList?.length > 0;

        return false;
    };

    // Set active for incident tab
    const setDefaultActiveTab = () => {
        
        // First
        if(state.inProgressIncidentList?.length > 0){
            state.activeTab = 'inProgressIncidentTab';
            state.dialogWidth = '90%';
        }
        // Second
        else if(state.verifiedIncidentList?.length > 0){
            state.activeTab = 'verifiedIncidentTab';
            state.dialogWidth = '60%';
        }
        // Last
        else if(state.registrationIncidentList?.length > 0){
            state.activeTab = 'registrationIncidentTab';
            state.dialogWidth = '60%';
        }
        else if(state.allProcessingIncident?.length > 0){
            state.activeTab = null;
            state.dialogWidth = '90%';
        }
        else
            incidentMonitor.isShowIncidentPopup = false;
    };

    // Alert function
    const alertMessage = (msgContent: string, msgType: string) => {
        let msgTitle = $t('lz.label.base.info');

        switch(msgType){
            case AlertTypeEnum.Confirm:
                msgTitle = $t('lz.label.base.confirm');
                break;

            case AlertTypeEnum.Info:
                msgTitle = $t('lz.label.base.info');
                break;

            case AlertTypeEnum.Error:
                msgTitle = $t('lz.label.base.error');
                break;

            case AlertTypeEnum.Warning:
                msgTitle = $t('lz.label.base.warning');
                break;
        }

        ElMessageBox.alert(
            msgContent,
            msgTitle,
            {
                confirmButtonText: $t('lz.button.base.confirm'),
                customStyle: {
                    'z-index': ZIndexHighestEnum.Max
                },
                customClass: 'z-index-highest--max',
                type: msgType
            },
            appContext
        );
    }

defineExpose({loadIncidentsByAccount});
</script>

<!-- IMPORTANCE: Do not use scoped keyword for .el-dialog__body (ele-dialog) -->
<style lang="scss">

    /* El-dialog */
    .incidents-dialog .el-dialog__body {
        overflow: hidden !important;
    }

    .incidents-dialog .incident-dialog-content {
        position: relative;
        isolation: isolate;
    }

    .incidents-dialog .incident-dialog-panels {
        position: relative;
        z-index: 0;
    }

    .incidents-dialog .leader-incident-list {
        max-height: 80vh;
        overflow: auto;
        overscroll-behavior: contain;
    }

    .incidents-dialog .selected-incident{
        background-color: #e6f7ff;
        border: 2px dashed #33beff;
    }

    .registration-panel {
        position: relative;
        height: auto;
        max-height: calc(100vh - 250px); /* Adjust based on your header/footer heights */
        display: flex;
        flex-direction: column;

        .incident-list-container {
            flex: 1;
            overflow-y: auto;
            padding: 10px 5px 20px 5px;
            
            /* Custom scrollbar styling */
            &::-webkit-scrollbar {
                width: 8px;
            }
            
            &::-webkit-scrollbar-track {
                background: #f1f1f1;
                border-radius: 4px;
            }
            
            &::-webkit-scrollbar-thumb {
                background: #888;
                border-radius: 4px;
                
                &:hover {
                    background: #555;
                }
            }
        }

        .incident-grid {
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(500px, 1fr));
            gap: 20px;
            margin-bottom: 20px;
        }

        .incident-card {
            transition: all 0.3s ease;
            border: 3px solid var(--el-color-primary);
            border-radius: 8px;
            
            &.is-selected {
                border: 3px dashed var(--el-color-success);
                box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2);
            }

            &:hover {
                transform: translateY(-3px);
                box-shadow: 0 4px 15px rgba(0, 0, 0, 0.15);
            }

            .incident-content {
                display: flex;
                flex-direction: column;
            }
            .incident-content-group{
                display: flex;
                flex-direction: row;
                gap: 5px;
            }

            .incident-main-info {
                /* display: flex; */
                padding-bottom: 20px;
                /* Giữ khối bên trái không bị co khi khối bên phải (mã/tên) được phép co/xuống dòng */
                flex-shrink: 0;
            }

            .incident-image-container {
                display: flex;
                flex-direction: column;
                align-items: center;
                background-color: #fff;
                border-radius: 5px;
                /* padding: 10px 15px; */
                width: 100%;
            }
            .incident-image-text {
                background-color:var(--el-color-primary);
                color:white;
                height:50%;
                width: 100%;
                padding-bottom:10px;
            }
            .incident-icon {

                width: 70px;
                height: 70px;
                object-fit: contain;
            }

            .incident-label {
                font-weight: bold;
                font-family: 'Roboto', sans-serif;
                text-transform: uppercase;
                color: var(--el-color-warning);
                margin-top: 10px;
                text-align: center;
            }

            .incident-time {
                color: var(--el-color-white);
                font-size: 12px;
                text-align: center;
                margin-top: 5px;
                padding: 5px;
            }

            .incident-details-grid {
                border-radius: 4px;
                padding: 5px;
                margin-bottom: 15px;
                /* flex: 1 + min-width: 0 để cột chi tiết co lại theo card thay vì tràn và bị cắt chữ */
                flex: 1;
                min-width: 0;
            }

            .detail-row {
                display: flex;
                margin-bottom: 8px;
                padding: 4px 0;
            }

            .detail-label {
                font-weight: bold;
                width: 80px;
                flex-shrink: 0;
                color: var(--el-color-primary);
            }

            .detail-value {
                flex: 1;
                min-width: 0;
                /* Mã/tên dài không có khoảng trắng vẫn phải xuống dòng */
                white-space: normal;
                overflow-wrap: anywhere;
                word-break: break-word;
            }

            .camera-list {
                display: flex;
                justify-content: flex-center;
                flex-wrap: wrap;
                gap: 5px;
            }

            .incident-actions {
                display: flex;
                justify-content: center;
                gap: 10px;
                margin-top: 10px;
            }

            .action-btn {
                min-width: 100px;
            }
        }

        .registration-footer {
            position: sticky;
            bottom: 0;
            left: 0;
            right: 0;
            background: white;
            padding: 16px 20px;
            border-top: 1px solid var(--el-border-color-lighter);
            display: flex;
            justify-content: center;
            box-shadow: 0 -2px 10px rgba(0, 0, 0, 0.05);
            z-index: 1;
        }

        .tab-label {
            display: flex;
            align-items: center;
            gap: 8px;
        }
    }

        /* InProgress Panel with Timeline Sidebar */
    .inprogress-panel {
        display: flex;
        max-height: calc(100vh - 250px);

        .tl-sidebar {
            flex-shrink: 0;
            width: 44px;
            overflow-y: auto;
            overflow-x: hidden;
            padding: 8px 4px;
            border-right: 1px solid #ebeef5;
            transition: width 0.3s ease;

            &:hover {
                width: 140px;
            }

            &::-webkit-scrollbar {
                width: 0;
            }
        }

        .tl-track {
            position: relative;
            display: flex;
            flex-direction: column;
            align-items: center;
        }

        .tl-line {
            position: absolute;
            top: 18px;
            bottom: 18px;
            left: 50%;
            width: 2px;
            transform: translateX(-50%);
            background: #dcdfe6;
        }

        .tl-node {
            position: relative;
            z-index: 1;
            display: flex;
            flex-direction: column;
            align-items: center;
            padding: 10px 0;
            cursor: pointer;
            width: 100%;

            &:hover .tl-dot {
                box-shadow: 0 0 0 3px rgba(64, 158, 255, 0.2);
                transform: scale(1.12);
            }

            &:hover .tl-loc {
                color: var(--el-color-primary);
            }
        }

        .tl-node--active .tl-dot {
            border-color: var(--el-color-primary);
            box-shadow: 0 0 0 3px rgba(64, 158, 255, 0.25);
        }

        .tl-dot {
            width: 30px;
            height: 30px;
            border-radius: 50%;
            border: 2px solid #dcdfe6;
            background: #fff;
            display: flex;
            align-items: center;
            justify-content: center;
            flex-shrink: 0;
            transition: all 0.25s ease;
        }

        .tl-dot-icon {
            width: 22px;
            height: 22px;
        }

        .tl-info {
            text-align: center;
            min-width: 0;
            margin-top: 2px;
            max-height: 0;
            overflow: hidden;
            opacity: 0;
            transition: max-height 0.3s ease, opacity 0.25s ease, margin-top 0.3s ease;
        }

        .tl-sidebar:hover .tl-info {
            max-height: 40px;
            opacity: 1;
            margin-top: 4px;
            background: white;
            border: 1px solid var(--el-border-color-lighter);
            padding: 3px;
        }

        .tl-time {
            font-size: 10px;
            color: #909399;
            line-height: 1.3;
        }

        .tl-loc {
            font-size: 10px;
            color: #303133;
            font-weight: 600;
            line-height: 1.3;
            transition: color 0.2s;
        }

        .inprogress-content {
            flex: 1;
            overflow-y: auto;
            padding: 10px 5px 20px 10px;
        }
    }

    @keyframes rotate {
        from { transform: rotate(0deg); }
        to { transform: rotate(360deg); }
    }

    /* Update dialog styles to ensure proper scrolling */
    .incidents-dialog {
        .el-dialog__body {
            padding: 0 !important;
            overflow: hidden !important;
        }
        
        .el-tabs__content {
            overflow: hidden !important;
        }
    }

    /* Confirmed Incident Info Styles */
    .confirmed-incident-card {
        transition: all 0.3s ease!important;
        border: 3px solid var(--el-color-primary)!important;
        border-radius: 8px!important;
        padding: 10px;
        /* border-radius: 12px; */
        background: linear-gradient(135deg, #ffffff 0%, #f8f9fa 100%);
        /* transition: all 0.3s ease; */
        /* border: 2px solid #e8eaed; */

        &:hover {
            transform: translateY(-2px);
            box-shadow: 0 8px 24px rgba(0, 0, 0, 0.12);
            border-color: var(--el-color-primary);
        }

        &.selected-incident {
            background: linear-gradient(135deg, #e6f7ff 0%, #f0f9ff 100%);
            border: 3px dashed var(--el-color-success)!important;
            box-shadow: 0 6px 20px rgba(64, 158, 255, 0.2);
        }
    }

    .confirmed-incident-container {
        display: flex;
        gap: 24px;
        align-items: stretch;
    }

    /* Icon Section Styles */
    .incident-icon-section {
        flex-shrink: 0;
        
        .icon-wrapper {
            display: flex;
            flex-direction: column;
            width: 200px;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
            border: 2px solid var(--el-color-primary);
            background: white;
            transition: all 0.3s ease;

            &:hover {
                box-shadow: 0 6px 16px rgba(0, 0, 0, 0.12);
                transform: scale(1.02);
            }
        }

        .incident-icon-image {
            height: 120px;
            width: 100%;
            background: linear-gradient(135deg, #f5f7fa 0%, #ffffff 100%);
            padding: 15px;
        }

        .icon-label-section {
            background: linear-gradient(135deg, var(--el-color-primary) 0%, var(--el-color-primary-light-3) 100%);
            color: white;
            padding: 16px 12px;
            text-align: center;
        }

        .icon-event-label {
            font-weight: 700;
            font-size: 15px;
            font-family: 'Roboto', sans-serif;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            margin-bottom: 10px;
            line-height: 1.4;
            text-shadow: 0 1px 2px rgba(0, 0, 0, 0.1);
        }

        .icon-time-info,
        .icon-location-info {
            font-size: 13px;
            font-weight: 500;
            margin: 6px 0;
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 6px;

            i {
                font-size: 14px;
            }
        }
    }

    /* Details Section Styles */
    .incident-details-section {
        flex: 1;
        min-width: 0;
        display: flex;
        flex-direction: column;
        gap: 16px;
    }

    .incident-header-title {
        display: flex;
        flex-wrap: wrap;
        gap: 10px 16px;
        justify-content: space-between;
        align-items: flex-start;
        padding-bottom: 12px;
        border-bottom: 2px solid #e8eaed;

        h3 {
            flex: 1 1 320px;
            min-width: 0;
            margin: 0;
            font-size: 20px;
            font-weight: 700;
            color: var(--el-color-primary);
            line-height: 1.4;
            overflow-wrap: anywhere;
        }

        .incident-header-tags {
            display: flex;
            flex-wrap: wrap;
            align-items: center;
            gap: 6px;
            max-width: 100%;
        }

        .el-tag {
            font-weight: 600;
            padding: 8px 16px;
            font-size: 13px;
        }
    }

    .incident-info-grid {
        display: grid;
        grid-template-columns: repeat(2, 1fr);
        gap: 10px 10px;
        
        .info-item {
            display: flex;
            flex-direction: column;
            gap: 6px;
            padding: 12px;
            background: white;
            border-radius: 8px;
            border: 1px solid #e8eaed;
            transition: all 0.2s ease;

            &:hover {
                border-color: var(--el-color-primary-light-5);
                box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
            }

            &.info-item-full {
                grid-column: 1 / -1;
            }
        }

        .info-label {
            font-size: 13px;
            font-weight: 600;
            color: #606266;
            display: flex;
            align-items: center;
            gap: 6px;
            text-transform: uppercase;
            letter-spacing: 0.3px;

            i {
                color: var(--el-color-primary);
                font-size: 14px;
            }
        }

        .info-value {
            font-size: 14px;
            font-weight: 500;
            color: #303133;
            line-height: 1.6;

            &.camera-buttons-wrapper {
                display: flex;
                flex-wrap: wrap;
                gap: 8px;
                margin-top: 4px;
            }

            .text-muted {
                color: #909399;
                font-style: italic;
            }

            .el-tag {
                font-weight: 500;
            }

            .el-button {
                font-weight: 500;
                
                i {
                    margin-right: 4px;
                }
            }
        }
    }

    .verified-panel {
        position: relative;
        height: auto;
        max-height: calc(100vh - 250px);
        display: flex;
        flex-direction: column;

        .incident-list-container {
            
            flex: 1;
            overflow-y: auto;
            padding: 10px 5px 10px 5px;
            
            &::-webkit-scrollbar {
                width: 8px;
            }
            
            &::-webkit-scrollbar-track {
                background: #f1f1f1;
                border-radius: 4px;
            }
            
            &::-webkit-scrollbar-thumb {
                background: #888;
                border-radius: 4px;
                
                &:hover {
                    background: #555;
                }
            }
        }

        .incident-grid {
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(500px, 1fr));
            gap: 20px;
            margin-bottom: 20px;
        }

        .incident-card {
            transition: all 0.3s ease;
            border: 3px solid var(--el-color-primary);
            border-radius: 8px;            
            &.is-selected {
                border: 3px dashed var(--el-color-success);
                transform: translateY(-2px);
                box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
            }

            &:hover {
                transform: translateY(-2px);
            }

            .incident-content {
                display: flex;
                flex-direction: column;
                gap: 16px;
            }
        }

        .incident-header {
            display: flex;
            gap: 16px;

            .incident-image {
                width: 120px;
                height: 120px;
                border-radius: 8px;
                object-fit: cover;
            }

            .incident-title {
                flex: 1;
                
                h3 {
                    margin: 0 0 8px;
                    color: var(--el-color-primary);
                    font-size: 18px;
                }

                .incident-datetime,
                .incident-location {
                    color: var(--el-text-color-secondary);
                    margin: 4px 0;
                    font-size: 14px;
                }
            }
        }

        .incident-details {
            .camera-buttons {
                display: flex;
                flex-wrap: wrap;
                gap: 4px;
                justify-content: flex-end;
            }

            .el-select {
                width: 100%;
            }
        }

        .incident-actions {
            display: flex;
            justify-content: flex-end;
            padding-top: 16px;
            border-top: 1px solid var(--el-border-color-lighter);
        }

        .verified-footer {
            position: sticky;
            bottom: 0;
            left: 0;
            right: 0;
            background: white;
            padding: 16px 20px;
            border-top: 1px solid var(--el-border-color-lighter);
            display: flex;
            justify-content: flex-end;
            box-shadow: 0 -2px 10px rgba(0, 0, 0, 0.05);
            z-index: 1;
        }

        /* Work detail table styles */
        .work-detail-table-container {
            margin-top: 16px;
            
            .table-header {
                display: flex;
                justify-content: space-between;
                align-items: center;
                padding: 12px 16px;
                background: linear-gradient(to right, var(--el-color-primary-light-9), var(--el-color-primary-light-8));
                border: 1px solid var(--el-border-color-lighter);
                border-bottom: none;
                border-radius: 4px 4px 0 0;
                
                h4 {
                    margin: 0;
                    color: var(--el-color-primary);
                    font-size: 16px;
                    font-weight: 600;
                }
            }
            
            .table-data {
                border-radius: 0 0 4px 4px;
            }
        }

        /* Disabled row style for work execute table */
        .row-disabled {
            opacity: 0.5;
            background-color: #f5f5f5 !important;
            cursor: not-allowed;
            
            td {
                color: #999;
            }
            
            .el-input, .el-button {
                pointer-events: none;
            }
        }
    }

    /* Floating Action Button Styles */
    .incident-fab {
        position: fixed;
        bottom: 30px;
        right: 30px;
        width: 60px;
        height: 60px;
        border-radius: 50%;
        background: linear-gradient(135deg, var(--el-color-warning) 0%, var(--el-color-warning-light-3) 100%);
        box-shadow: 0 6px 20px rgba(230, 162, 60, 0.4);
        display: flex;
        align-items: center;
        justify-content: center;
        cursor: pointer;
        transition: all 0.3s ease;
        z-index: 2000;
        border: 3px solid white;
        opacity: 0.5; /* Default 50% transparent */

        &:hover {
            transform: translateY(-4px) scale(1.05);
            box-shadow: 0 10px 30px rgba(230, 162, 60, 0.6);
            background: linear-gradient(135deg, var(--el-color-warning-dark-2) 0%, var(--el-color-warning) 100%);
            opacity: 1; /* 100% visible on hover */
        }

        &:active {
            transform: translateY(-2px) scale(1.02);
        }

        .fab-content {
            position: relative;
            display: flex;
            align-items: center;
            justify-content: center;
            color: white;

            .fab-icon {
                font-size: 32px;
                animation: pulse 2s ease-in-out infinite;
            }

            .fab-badge {
                position: absolute;
                top: -8px;
                right: -8px;
                
                :deep(.el-badge__content) {
                    background-color: var(--el-color-danger);
                    border: 2px solid white;
                    font-weight: 700;
                    font-size: 12px;
                    padding: 4px 6px;
                    border-radius: 12px;
                    line-height: 1;
                }
            }
        }

        &.fab-blink {
            animation: blink-border 0.8s ease-in-out infinite, shake 0.5s ease-in-out infinite;
            opacity: 1; /* Show 100% when blinking */
        }
    }

    /* FAB Fade Transition */
    .fab-fade-enter-active,
    .fab-fade-leave-active {
        transition: all 0.3s ease;
    }

    .fab-fade-enter-from {
        opacity: 0;
        transform: scale(0) translateY(20px);
    }

    .fab-fade-leave-to {
        opacity: 0;
        transform: scale(0.8) translateY(20px);
    }

    /* Blink Animation */
    @keyframes blink-border {
        0%, 100% {
            box-shadow: 0 6px 20px rgba(230, 162, 60, 0.4), 0 0 0 0 rgba(245, 108, 108, 0.7);
        }
        50% {
            box-shadow: 0 6px 20px rgba(230, 162, 60, 0.4), 0 0 0 10px rgba(245, 108, 108, 0);
        }
    }

    /* Shake Animation */
    @keyframes shake {
        0%, 100% { transform: translateX(0); }
        10%, 30%, 50%, 70%, 90% { transform: translateX(-3px); }
        20%, 40%, 60%, 80% { transform: translateX(3px); }
    }

    /* Pulse Animation */
    @keyframes pulse {
        0%, 100% {
            transform: scale(1);
        }
        50% {
            transform: scale(1.1);
        }
    }

    /* SonTH - 24/07/2026: nhãn đếm ngược khi sự cố đang chờ luồng tự động thực thi */
    .automation-pending-tag {
        align-self: center;
        font-variant-numeric: tabular-nums;
    }

    .automation-failure-notice.el-alert {
        align-items: flex-start;
        gap: 10px;
        padding: 10px 12px;
        border: 1px solid var(--el-color-danger-light-8);
        border-left: 3px solid var(--el-color-danger);
        border-radius: 6px;
        text-align: left;

        .el-alert__icon {
            flex: 0 0 18px;
            width: 18px;
            margin: 2px 0 0;
            font-size: 18px;
        }

        .el-alert__content {
            min-width: 0;
            padding: 0;
        }

        .el-alert__title {
            font-size: 13px;
            font-weight: 600;
            line-height: 1.5;
            word-break: normal;
            overflow-wrap: break-word;
        }

        .el-alert__description {
            margin: 4px 0 0;
            color: var(--el-text-color-regular);
            font-size: 12px;
            line-height: 1.6;
            word-break: normal;
            overflow-wrap: anywhere;
        }
    }

    /* KienLT - 16/10/2026: lý do bước tự động bị lỗi, đã trả về Chờ cho người trực làm tay */
    .automation-failed-reason {
        color: var(--el-color-danger);
        font-size: 13px;
    }

    /* Thu nhỏ riêng notification broadcast, không ảnh hưởng các notification khác. */
    .incident-broadcast-notification--small {
        --el-notification-width: min(280px, calc(100vw - 32px));
        --el-notification-padding: 10px 24px 10px 10px;
        --el-notification-icon-size: 20px;
        --el-notification-group-margin-left: 8px;
        --el-notification-group-margin-right: 4px;
        --el-notification-title-font-size: 14px;
        --el-notification-content-font-size: 12px;
        --el-notification-close-font-size: 14px;

        .el-notification__content {
            margin-top: 2px;
            line-height: 18px;
        }

        .el-notification__closeBtn {
            top: 12px;
            right: 10px;
        }
    }

    /* SonTH - 24/07/2026: popup này KHÔNG tự đặt z-index. Mốc khởi tạo của Element Plus
       đã nâng lên 20000 trong main.ts nên popup vượt sẵn các lớp tĩnh của bản đồ, đồng thời
       hộp xác nhận và dropdown mở từ trong popup vẫn nằm trên nó theo thứ tự mở.
       Xem theme/element.scss mục "Thứ tự lớp phủ". */
</style>
