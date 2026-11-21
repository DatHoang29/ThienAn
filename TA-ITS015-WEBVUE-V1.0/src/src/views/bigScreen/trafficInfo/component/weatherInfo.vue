<template>
    <!-- <div class="visualizing-container-head-right-text-box">
        <div class="display-flex--horizontal flex-gap--10px" :style="{ color: props.textColor || 'var(--next-bg-topBarColor)', fontWeight: 'bold'}">
            <img style="max-height: 25px;" :src="commonVarsAndFuncs.getFullUrl(UploadFoldersEnum.Weather + (state.currentWeatherData?.type?.toLowerCase().concat('.png') ?? 'weather.png'))">
            <p class="text-capitalize">{{ state.currentWeatherData?.shortDescription ?? '' }}</p>
            <p>{{ state.currentWeatherData?.temperature ?? '' }} &deg;C</p>
        </div>
    </div> -->
    <!-- Nút trang chủ + tài khoản, nằm trong thanh công cụ kính mờ góc phải header (index.vue) -->
    <div class="head-right-buttons">
        <button type="button" class="head-icon-btn" @click="handleHome">
            <el-icon><HomeFilled /></el-icon>
        </button>
        <span class="head-divider" aria-hidden="true" />
        <UserInfo class="head-user" :style="{ color: props.textColor || 'var(--next-bg-topBarColor)' }" />
    </div>
</template>

<script lang="ts" setup>
    import { reactive,defineAsyncComponent, onMounted } from 'vue';
    import { HomeFilled } from '@element-plus/icons-vue';
    import { getAPI } from '/@/utils/axios-utils';
    import { TmsWeatherApi } from '/@/api-services/tms';
    import { useRouter } from 'vue-router';
    import { useItsMediaServerStore } from '/@/stores/its/itsMediaServer';
    import { UploadFoldersEnum } from '/@/types/enums/baseEnum';

    import { useCommonVarsAndFuncs } from '/@/stores/its/commonVarsAndFuncs';
    const commonVarsAndFuncs = useCommonVarsAndFuncs();
    const UserInfo = defineAsyncComponent(() => import('/@/layout/navBars/topBar/userInfo.vue'));
    const props = defineProps<{
        textColor?: {
            type: String,
            default: ''
        };
    }>();

    const router = useRouter();
    const state = reactive({
        currentWeatherData: [] as any,
        baseUrl: '' as string,

    });

    onMounted(async() => {
        let response = await getAPI(TmsWeatherApi).apiTmsTmsweatherDetailGet();
        state.currentWeatherData = response.data.result;
        // Địa chỉ file server dùng chung (chỉ gọi API 1 lần cho toàn app)
        state.baseUrl = await useItsMediaServerStore().ensureLoaded();
    });

    const handleHome = () => {
        router.push('/');
    };
</script>

<style scoped lang="scss">
.head-right-buttons {
    display: flex;
    align-items: center;
    gap: 4px;
}

/* Nút icon tròn trong suốt, hover sáng nhẹ - cùng kiểu chuông thông báo */
.head-icon-btn {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 32px;
    height: 32px;
    padding: 0;
    border: none;
    outline: none;
    border-radius: 50%;
    background: transparent;
    color: #e8f2ff;
    font-size: 17px;
    cursor: pointer;
    transition: background-color 0.2s ease, color 0.2s ease;

    &:hover,
    &:focus-visible {
        background: rgba(255, 255, 255, 0.14);
        color: #fff;
    }
}

.head-divider {
    width: 1px;
    height: 20px;
    margin: 0 4px;
    background: rgba(255, 255, 255, 0.18);
}

/* Tài khoản: avatar + tên dạng chip, hover nền sáng nhẹ */
.head-user {
    :deep(.layout-navbars-breadcrumb-user-link) {
        display: inline-flex;
        align-items: center;
        height: 32px;
        padding: 0 10px 0 3px;
        border-radius: 999px;
        color: #e8f2ff;
        font-size: 13px;
        font-weight: 600;
        cursor: pointer;
        transition: background-color 0.2s ease;

        &:hover {
            background: rgba(255, 255, 255, 0.12);
        }
    }

    /* Avatar: bỏ viền trắng dày (style inline của userInfo), thay bằng vòng sáng mảnh màu primary */
    :deep(.layout-navbars-breadcrumb-user-link-photo) {
        width: 26px !important;
        height: 26px !important;
        border: none !important;
        font-size: 13px !important;
        font-weight: 700;
        color: #fff;
        background: linear-gradient(135deg, var(--el-color-primary-light-3), var(--el-color-primary));
        box-shadow: 0 0 0 2px rgba(255, 255, 255, 0.25);
        margin-right: 8px !important;
    }

    :deep(.el-icon--right) {
        margin-left: 6px;
        font-size: 12px;
        opacity: 0.7;
    }
}
</style>
