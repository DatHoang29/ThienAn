<template>
    <InfoPanel
        v-model:active-tab="state.activeTab"
        :tabs="tabs"
        :title="state.data?.name"
        :subtitle="state.data?.remark"
        @closed="emits('closed')"
        @close-others="emits('closeOthers')"
        @close-all="emits('closeAll')"
    >
        <!-- Info -->
        <template #infoTab>
            <div class="content-text">
                <div class="info-section">
                    <h3 class="section-title">Toll Summary</h3>
                    Status:
                    <div class="status-indicator">
                        <span :class="state.data?.status == 1 ? 'green' : 'red'">
                            {{ state.data?.status == 1 ? 'Open' : 'Close' }}
                        </span>
                    </div>
                </div>

                <div class="info-section">
                    <h3 class="section-title">Toll Information</h3>
                    <table class="info-table">
                        <tr>
                            <td class="label">Code</td>
                            <td class="value">{{ state.data?.code || '-' }}</td>
                            <td class="label">Name</td>
                            <td class="value">{{ state.data?.name || '-' }}</td>
                        </tr>
                        <tr>
                            <td class="label">Location</td>
                            <td class="value">{{ state.data?.remark || '-' }}</td>
                        </tr>
                        <tr>
                            <td class="label">Info</td>
                            <td class="value">{{ state.data?.info || '-' }}</td>
                        </tr>
                    </table>
                </div>
            </div>
        </template>
    </InfoPanel>
</template>

<script setup lang="ts" name="unitInfoModal">
import { reactive, computed } from 'vue';
import { useI18n } from 'vue-i18n';
import InfoPanel from '/@/components/bigScreen/floatingModal/InfoPanel.vue';

const { t: $t } = useI18n();

const emits = defineEmits(['activated', 'closed', 'closeOthers', 'closeAll']);

const state = reactive({
    activeTab: 'infoTab',
    data: null as any,
});

const tabs = computed(() => [{ label: 'Info', name: 'infoTab' }]);

// Dialog methods
const openInfo = (data: any) => {
    state.data = data;
    state.activeTab = 'infoTab';
};

defineExpose({ openInfo });
</script>

<style scoped lang="scss">
@import '/@/theme/its/trafficInfo.scss';
</style>
