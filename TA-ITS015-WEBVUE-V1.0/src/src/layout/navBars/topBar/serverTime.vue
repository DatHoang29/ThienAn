<template>
    <div class="server-time" style="color: var(--next-bg-topBarColor);">
      {{ formattedServerTime }}
    </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue';
import { useIntervalFn } from '@vueuse/core';
import { useThemeConfig } from '/@/stores/themeConfig';
import { useServerTime } from '/@/stores/serverTimeStore';
import { storeToRefs } from 'pinia';

// Define variable content
const storesThemeConfig = useThemeConfig();
const { themeConfig } = storeToRefs(storesThemeConfig);

// Nguồn giờ server dùng chung (SSE offset). Component chỉ hiển thị, không tự quản lý kết nối.
const serverTimeStore = useServerTime();
const formattedServerTime = ref('');

const daysOfWeek = {
  'vi-VN': ['Chủ Nhật', 'Thứ Hai', 'Thứ Ba', 'Thứ Tư', 'Thứ Năm', 'Thứ Sáu', 'Thứ Bảy'],
  'en-US': ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']
};

let disconnect;

const updateFormattedTime = () => {
    // Giờ server hiện tại + lệch múi giờ server ⇒ hiển thị đúng theo giờ server.
    const adjustedTime = new Date(serverTimeStore.serverNowMs() + serverTimeStore.serverTimeZoneMinutes * 60000);

    const lang = themeConfig.value.globalI18n;
    const dayName = daysOfWeek[lang][adjustedTime.getUTCDay()];

    const day = adjustedTime.getUTCDate().toString().padStart(2, '0');
    const month = (adjustedTime.getUTCMonth() + 1).toString().padStart(2, '0');
    const year = adjustedTime.getUTCFullYear();
    const hours = adjustedTime.getUTCHours().toString().padStart(2, '0');
    const minutes = adjustedTime.getUTCMinutes().toString().padStart(2, '0');
    const seconds = adjustedTime.getUTCSeconds().toString().padStart(2, '0');

    formattedServerTime.value = `${dayName}, ${day}/${month}/${year} ${hours}:${minutes}:${seconds}`;
};

onMounted(() => {
    disconnect = serverTimeStore.connect();
    // Nhích đồng hồ mỗi giây; giá trị luôn suy ra từ Date.now()+offset nên không trôi.
    useIntervalFn(updateFormattedTime, 1000, { immediateCallback: true });
});

onUnmounted(() => disconnect?.());
</script>

<style>
.server-time {
    font-family: 'VT323', monospace;
    font-size: 16px;
    font-weight: bold;
    color: var(--el-color-primary);
}   
@import url('/@/assets/VT323-Regular.ttf');
</style>
