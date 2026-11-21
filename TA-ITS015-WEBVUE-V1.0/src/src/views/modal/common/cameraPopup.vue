<template>
    <!-- SonTH - 24/07/2026: append-to-body vì popup này được mở từ trong popup xử lý sự cố, mà popup
         đó có draggable (transform) nên neo position: fixed của phần tử con vào chính nó, làm popup
         camera bị lệch vị trí và kẹt trong stacking context của nó. -->
    <el-dialog v-model="state.isShowDialog" :z-index="ZIndexHighestEnum.Max" append-to-body style="max-width: 680px; max-height: 460px; background-color: black;" @closed="handleOnClosed" draggable overflow destroy-on-close>
        <template #header>
            <div class="display-flex--horizontal flex-gap--5px">
                <div style="color: #fff">
                    <span>{{ commonVarsAndFuncs.formatCameraName(state.cameraInfo?.name, state.cameraInfo?.kmNumber, state.cameraInfo?.metNumber) }}</span>
                </div>
            </div>
        </template>
        <div id="livecam-container">
            <CctvCameraView ref="liveCamCompRef" :equipment="state.cameraInfo" :style="{width: '640px', height:'360px'}" />
        </div>
    </el-dialog>
</template>

<script lang="ts" setup>
    import { reactive, ref } from 'vue';
    import { useI18n } from 'vue-i18n';

    // Khung view camera dùng chung: tự chọn phát qua CCTV (PTZ + playback) hoặc RTSP.
    import CctvCameraView from '/@/components/cctv/CctvCameraView.vue';
    import { ZIndexHighestEnum } from '/@/types/enums/baseEnum';
    const liveCamCompRef = ref<InstanceType<typeof CctvCameraView>>();

    import { useCommonVarsAndFuncs } from '/@/stores/its/commonVarsAndFuncs';
    const commonVarsAndFuncs = useCommonVarsAndFuncs();

    const { t: $t } = useI18n();

    const state = reactive({
        isShowDialog: false,
        cameraInfo: {}
    });

    // Handle the event when clicking a tab
	const handleOnClosed = () => {
        liveCamCompRef.value?.stop();
	};

    // Open dialog function
    const openDialog = (cameraInfo: any) => {
        state.isShowDialog = true;
        state.cameraInfo = cameraInfo;
    };

    defineExpose({openDialog});
</script>

<style scoped>
/* Canvas */
#livecam-container{
    background-color: black !important;
    display: flex;
    align-items: center;
    justify-content: center;
}

#livecam-container canvas{
    width: 100%;
    height: auto;
    max-width: 100%;
    max-height: 100%;
}
</style>