<template>
    <div class="timeline-incident-container">
        <el-card>
            <template #header>
                <div class="card-header">
                    <h2>{{ $t('lz.label.base.info') }}</h2>
                </div>
            </template>
            <div style="max-height: 90vh; padding: 20px; padding-bottom: 60px; overflow-y: auto">
                <el-timeline v-if="state.workExecList" class="timeline">
                    <el-timeline-item v-for="we in state.workExecList" :timestamp="useDateFormat(we.executeDate ?? we.createTime, DateTimeFormatEnum.DateTimeDefaultRevert)" placement="top">
                        <el-card>
                            <div class="display-flex--vertical flex-gap--10px">
                                <h4>{{ we.workName }}</h4>
                                <i v-if="!we?.isIncident" class="text-color--mute">[{{ state.incidentInfo.name }}]</i>
                                <p v-if="!we?.occurred" class="display-flex--horizontal flex-gap--10px">
                                    <span>{{ $t('lz.entity.base.status') }}: </span>
                                    <TagInfo :config-item="(we?.isIncident ? getConfigItemByCode(BaseConfigTypeEnum.TmsIncidentStatus, we.state) : getConfigItemByCode(BaseConfigTypeEnum.TmsWorkExecuteStatus, we.state))" />
                                </p>
                                <p v-if="we.executeBy?.length > 0" class="display-flex--horizontal flex-gap--10px">
                                    <span>{{$t('lz.entity.tmsWorkExecute.executeDate')}}:</span>
                                    <span>{{ we.executeDate ? useDateFormat(we.executeDate, DateTimeFormatEnum.DateTimeDefaultRevert) : null }}</span>
                                </p>
                                <p v-if="we.executeBy?.length > 0" class="display-flex--horizontal flex-gap--10px">
                                    <span>{{$t('lz.entity.tmsWorkExecute.executeBy')}}:</span>
                                    <span>{{ we.executeBy }}</span>
                                </p>

                                <p v-if="we.remark" class="display-flex--horizontal flex-gap--10px">
                                    <span>{{$t('lz.entity.base.remark')}}:</span>
                                    <span>{{ we.remark }}</span>
                                </p>
                                <p v-if="we.description" class="display-flex--horizontal flex-gap--10px">
                                    <span>{{$t('lz.entity.base.info')}}:</span>
                                    <span>{{ we.description }}</span>     
                                </p>
                                <div v-if="we.workContactList?.length > 0" class="display-flex--horizontal flex-gap--10px">
                                    <span>{{ $t('lz.label.tmsWorkExecute.workContact') }}:</span>
                                    <p>{{ we.workContactList.map(wc => wc.contactName + ' (' + wc.contactPhone + ')').join(', ') }}</p>
                                </div>

                                <div v-if="we?.isIncident && we.state == TmsIncidentStateEnum.Registration" class="display-flex--vertical flex-gap--10px">

                                    <p class="display-flex--horizontal flex-gap--10px">
                                        <span>{{ $t('lz.entity.tms.eventType') }}:</span>
                                        <span>{{ incidentEventTypeDisplay(state.incidentInfo, state.eventTypeList) }}</span>
                                    </p>

                                    <p class="display-flex--horizontal flex-gap--10px">
                                        <span>{{ $t('lz.label.base.location') }}:</span>
                                        <span>{{ state.locationList?.find(item => item.code == state.incidentInfo.location)?.name }}</span>
                                    </p>

                                    <p class="display-flex--horizontal flex-gap--10px">
                                        <span>{{ $t('lz.entity.tmsIncident.influenceScope') }}:</span>
                                        <span>{{ state.laneList?.find(item => item.code == state.incidentInfo.influenceScope)?.name }}</span>
                                    </p>

                                    <p class="display-flex--horizontal flex-gap--10px">
                                        <span>{{ $t('lz.entity.tmsIncident.zoneId') }}:</span>
                                        <span>
                                            {{ state.zoneList?.map(item => item.name).join(', ') }}
                                        </span>
                                    </p>    

                                    <p class="display-flex--horizontal flex-gap--10px">
                                        <span>{{ $t('lz.label.base.position') }}:</span>
                                        <span>
                                            {{ commonVarsAndFuncs.formatPosition(state.incidentInfo.kmNumber, state.incidentInfo.metNumber) }}
                                        </span>
                                    </p>

                                    <p class="display-flex--horizontal flex-gap--10px">
                                        <span>{{ $t('lz.label.base.camera') }}:</span>
                                        <span>
                                            {{ state.cameraList?.map(item => item.name).join(', ') }}
                                        </span>
                                    </p>    

                                    <p class="display-flex--horizontal flex-gap--10px">
                                        <span>{{ $t('lz.entity.tmsIncident.source') }}:</span>
                                        <span>{{ state.sourceDetectList?.find(item => item.code == state.incidentInfo.source)?.name }}</span>
                                    </p>

                                    <p class="display-flex--horizontal flex-gap--10px">
                                        <span>{{ $t('lz.entity.tmsIncident.injuredNumber') }}:</span>
                                        <span>{{ state.incidentInfo.injuredNumber }}</span>
                                    </p>

                                    <p class="display-flex--horizontal flex-gap--10px">
                                        <span>{{ $t('lz.entity.tmsIncident.vehicleNumber') }}:</span>
                                        <span>{{ state.incidentInfo.vehicleNumber }}</span>
                                    </p>

                                    <p class="display-flex--horizontal flex-gap--10px">
                                        <span>{{ $t('lz.entity.base.info') }}:</span>
                                        <span>{{ state.incidentInfo.description }}</span>
                                    </p>

                                    <p class="display-flex--horizontal flex-gap--10px">
                                        <span>{{ $t('lz.entity.base.remark') }}:</span>
                                        <span>{{ state.incidentInfo.remark }}</span>
                                    </p>
                                </div>
                            </div>
                        </el-card>
                    </el-timeline-item>
                </el-timeline>
            </div>
        </el-card>
    </div>
</template>
  
<script lang="ts" setup>
import { incidentEventTypeDisplay } from '/@/utils/incidentEventTypeDisplay';
	import { reactive, onMounted, defineAsyncComponent } from 'vue';
	import { useI18n } from 'vue-i18n';
	import { getAPI } from '/@/utils/axios-utils';
    import { useRoute } from 'vue-router';
	import { useDateFormat } from '@vueuse/core';

	import { TmsIncidentApi } from '/@/api-services/tms';
	import { useTmsStore } from '/@/stores/tmsStore';
	const tmsStore = useTmsStore();
    import { TmsIncidentStateEnum } from '/@/types/enums';
    import { DateTimeFormatEnum } from '/@/types/enums/baseEnum';

    import { useCommonVarsAndFuncs } from '/@/stores/its/commonVarsAndFuncs';
	const commonVarsAndFuncs = useCommonVarsAndFuncs();

    import { useBaseConfig } from '/@/stores/baseConfig';
	import { BaseConfigTypeEnum } from '/@/types/enums';
	const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
	const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
	const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));

	const route = useRoute();
    const { t: $t } = useI18n();
  	
    const state = reactive({
        incidentTimeline: [],

		incidentInfo: [],
        workExecList: [],
        workContactList: [],
		sourceDetectList: [],
		eventTypeList: [],
		locationList: [],
		laneList: [],
		zoneList: [],
		cameraList: [],
	});

    const loadIncidentData = async() => {
        const incidentId = route.query?.incidentId;

        let response = await (incidentId?.length > 0 ? getAPI(TmsIncidentApi).apiTmsTmsincidentSinglePost({id: incidentId}) : null);
        state.incidentInfo = response.data.result;

        if(!state.incidentInfo)
            return;

        state.workExecList = await tmsStore.getIncidentHistory(String(incidentId), state.incidentInfo.state);

        if(state.incidentInfo.state == TmsIncidentStateEnum.Finished || state.incidentInfo.state == TmsIncidentStateEnum.Skipped || state.incidentInfo.state == TmsIncidentStateEnum.Canceled){
            state.workExecList.unshift({
                workName: state.incidentInfo.name,
                createTime: state.incidentInfo.createdDate,
                executeDate: state.incidentInfo.finishedDate,
                executeBy: state.incidentInfo.finishedBy,
                state: state.incidentInfo.state, // Finished | Canceled | Skipped
                isIncident: true
            });
        }

        if(state.incidentInfo.approvedBy?.length > 0){
            state.workExecList.push({
                workName: state.incidentInfo.name,
                createTime: state.incidentInfo.createdDate,
                executeDate: state.incidentInfo.approvedDate,
                executeBy: state.incidentInfo.approvedBy,
                state: TmsIncidentStateEnum.InProgress,
                isIncident: true
            });
        }

        if(state.incidentInfo.validatedBy?.length > 0){
            state.workExecList.push({
                workName: state.incidentInfo.name,
                createTime: state.incidentInfo.createdDate,
                executeDate: state.incidentInfo.validatedDate,
                executeBy: state.incidentInfo.validatedBy,
                state: TmsIncidentStateEnum.Verified,
                isIncident: true
            });
        }

        // KienLT - 26/10/2026: Đăng ký = lúc ghi vào hệ thống (createdDate), không phải lúc xảy ra (startDate) — sự cố
        // tạo từ sự kiện cũ trước đây hiện giờ đăng ký bằng giờ sự kiện. Thiếu createdDate (dữ liệu cũ) thì dùng startDate.
        state.workExecList.push({
            workName: state.incidentInfo.name,
            createTime: state.incidentInfo.createdDate,
            executeDate: state.incidentInfo.createdDate ?? state.incidentInfo.startDate,
            executeBy: state.incidentInfo.createdBy,
            state: TmsIncidentStateEnum.Registration,
            isIncident: true
        });

        // Xảy ra sớm hơn lúc đăng ký quá 1 phút ⇒ thêm mốc "Xảy ra" (cùng luật với nhật ký ở tab Sự cố).
        const occurredAt = Date.parse(String(state.incidentInfo.startDate ?? ''));
        const registeredAt = Date.parse(String(state.incidentInfo.createdDate ?? ''));
        if (Number.isFinite(occurredAt) && Number.isFinite(registeredAt) && registeredAt - occurredAt > 60_000) {
            state.workExecList.push({
                workName: $t('lz.label.tmsIncident.historyOccurred'),
                executeDate: state.incidentInfo.startDate,
                isIncident: true,
                occurred: true
            });
        }

        state.sourceDetectList = getConfigDataByCode(BaseConfigTypeEnum.TmsSourceDetectIncident);
		state.locationList = getConfigDataByCode(BaseConfigTypeEnum.TmsLocationType);
		state.incidentStatusList = getConfigDataByCode(BaseConfigTypeEnum.TmsIncidentStatus);
		
		// TODO: LaneList is temporarily DirectionList => by direction_config_type not tms_lane
		state.laneList = getConfigDataByCode(BaseConfigTypeEnum.TmsDirectionType);

		state.eventTypeList = await tmsStore.getEventTypes();

        if(state.incidentInfo.zoneId){
			//zoneId: "id01,id02,id03..."
			state.zoneList = await tmsStore.getZonesByIds(state.incidentInfo.zoneId);
		}

		if(state.incidentInfo.equipmentId){
			//equipmentId: "id01,id02,id03..."
			state.cameraList = await tmsStore.getEquipmentByIds(state.incidentInfo.equipmentId);
		}
    };

	onMounted(async () => {
        loadIncidentData();
	});

</script>
