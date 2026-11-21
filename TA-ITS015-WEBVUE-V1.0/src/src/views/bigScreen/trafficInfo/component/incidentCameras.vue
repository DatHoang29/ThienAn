<template>
    <div v-if="state.selectedIncident">
        <el-tabs>
            <!-- <el-tab-pane v-for="cam in state.cameraList" :label="cam.name + ' (' + (cam.kmNumber ?? 0) + 'Km ' + (cam.metNumber ?? 0) + ')'" @tab-change="(tabName) => {console.log(tabName);}">
                <div class="display-flex--vertical flex-gap--10px">
                    <div class="display-flex--horizontal flex-gap--10px">
                        <strong class="text-uppercase">{{ state.selectedIncident?.eventTypeName ?? (getEventType(state.selectedIncident?.eventTypeId)?.name ?? state.selectedIncident?.name) }}</strong>
                        <strong>({{ (state.selectedIncident?.kmNumber ?? 0) + 'Km +' + (state.selectedIncident?.metNumber ?? 0) }})</strong>
                    </div>
                    <div>
                        <CctvCameraView ref="liveCamCompRef" :equipment="cam" :style="{width: `${state.camProperties.width}px`, height: `${state.camProperties.height}px`}" />
                    </div>
                </div>
            </el-tab-pane> -->
            <el-tab-pane v-if="state.cameraList[0]" :label="state.cameraList[0].name + ' (' + (state.cameraList[0].kmNumber ?? 0) + 'Km ' + (state.cameraList[0].metNumber ?? 0) + ')'" @tab-change="(tabName) => {console.log(tabName);}">
                <div class="display-flex--vertical flex-gap--10px">
                    <div>
                        <CctvCameraView ref="liveCamCompRef" :equipment="state.cameraList[0]" :style="{width: `${state.camProperties.width}px`, height: `${state.camProperties.height}px`}" />
                    </div>
                    <div class="display-flex--horizontal flex-gap--10px">
                        <strong class="text-uppercase">{{ state.selectedIncident?.eventTypeName ?? (getEventType(state.selectedIncident?.eventTypeId)?.name ?? state.selectedIncident?.name) }}</strong>
                        <strong>({{ (state.selectedIncident?.kmNumber ?? 0) + 'Km +' + (state.selectedIncident?.metNumber ?? 0) }})</strong>
                    </div>
                </div>
            </el-tab-pane>
            <el-tab-pane v-else>
                <el-empty :image-size="200" />
            </el-tab-pane>
        </el-tabs>
    </div>
    <div v-else>
        <el-tabs>
            <el-tab-pane>
                <el-card shadow="hover" class="w100">
                    <el-empty :image-size="100" />
                </el-card>
            </el-tab-pane>
        </el-tabs>
    </div>
</template>

<script lang="ts" setup>
    import { onMounted, reactive, ref } from 'vue';
    import { getAPI } from '/@/utils/axios-utils';
    import { TmsEquipmentApi, TmsEquipmentTypeApi, TmsEventTypeApi, TmsIncidentApi } from '/@/api-services/tms';
    import { useI18n } from 'vue-i18n';
    import { useCommonVarsAndFuncs } from '/@/stores/its/commonVarsAndFuncs';
    const commonVarsAndFuncs = useCommonVarsAndFuncs();

    const { t: $t } = useI18n();

    // Khung view camera dùng chung: tự chọn phát qua CCTV (PTZ + playback) hoặc RTSP.
    import CctvCameraView from '/@/components/cctv/CctvCameraView.vue';
    const liveCamCompRef = ref<InstanceType<typeof CctvCameraView>>();

    const state = reactive({
        isLoading: false,
        selectedIncident: [],
        incidentInfo: [],
        eventTypeList: [],
        cameraList: [],
        camProperties: {width: 400, height: 300}
    });
    
    const getEventType = (eventTypeId: any) => {
        if(!eventTypeId)
            return null;

        return state.eventTypeList?.find(item => item.id == eventTypeId);
    }

    const showAllCameras = async(incidentId: string = '') => {
        let response = incidentId ? await getAPI(TmsIncidentApi).apiTmsTmsincidentSinglePost({id: incidentId}) : await getAPI(TmsIncidentApi).apiTmsTmsincidentLatestincidentPost();
        state.selectedIncident = response.data.result;

        if((state.selectedIncident ?? null) == null)
            return;

        response = await getAPI(TmsEventTypeApi).apiTmsTmseventtypeListGet();
        state.eventTypeList = response.data.result;

        if(state.selectedIncident?.equipmentId){
            response = await getAPI(TmsEquipmentApi).apiTmsTmsequipmentListbyidsGet(state.selectedIncident?.equipmentId);
            const equipmentList = response.data.result;

            response = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet();
            const equipmentTypeList = response.data.result;

            state.cameraList = equipmentList.filter(equip => equipmentTypeList?.some(equipType => equipType.id == equip.equipmentTypeId && commonVarsAndFuncs.isCCTVCamera(equipType.code, 'CCTV')));
        }

        state.incidentInfo = [];
        if(state.selectedIncident?.name)
            state.incidentInfo.push(state.selectedIncident?.name);

        if(state.selectedIncident?.remark)
            state.incidentInfo.push(state.selectedIncident?.remark);

        if(state.selectedIncident?.description)
            state.incidentInfo.push(state.selectedIncident?.description);

        if(state.selectedIncident?.kmNumber)
            state.incidentInfo.push((state.selectedIncident?.kmNumber ?? 0) + 'Km +' + (state.selectedIncident?.metNumber ?? 0));
    }

    onMounted(() => {
        showAllCameras();
    });

    defineExpose({showAllCameras});
</script>

<style scoped>
    /* Style Css */
    .incident-notification{
        display: flex;
        align-items: center;
        gap: 10px;
        text-align: left;
    }

    .incident-notification .img{
        max-width: 30px;
        max-height: 30px;
    }

    .incident-notification .info {
        font-weight: bold;
        width: fit-content;
        text-transform: uppercase;
    }

    .flex-inline{
        display: flex;
        flex-wrap: wrap;
        gap: 5px;
    }

    .text-capitalize{
        text-transform: capitalize !important;
    }

    .padding-5px{
        padding: 5px;
    }
</style>