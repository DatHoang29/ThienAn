<template>
    <InfoPanel
        v-model:active-tab="state.activeTab"
        :tabs="tabs"
        :device-name="state.data?.name"
        :device-location="state.data?.remark"
        @closed="emits('closed')"
        @close-others="emits('closeOthers')"
        @close-all="emits('closeAll')"
    >
        <!-- Info -->
        <template #infoTab>
            <div class="content-text">
                <!-- <div class="border-content-info">
                    <h3 class="section-title">{{ $t('lz.label.base.summary') }}</h3>
                    <div class="background-summary-device">
                        <span>{{ $t('lz.label.base.status') }}:</span>
                        <div class="status-indicator">
                            <TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, state.data?.status)" />
                        </div>
                    </div>
                </div> -->

                <div class="border-content-info">
                    <h3 class="section-title">{{ $t('lz.label.base.info') }}</h3>
                    <div class="info-grid">
                        <div class="info-item">
                            <span class="info-k">{{ $t('lz.entity.base.code') }}</span>
                            <span class="info-v">{{ state.data?.code || '-' }}</span>
                        </div>
                        <div class="info-item">
                            <span class="info-k">{{ $t('lz.entity.base.name') }}</span>
                            <span class="info-v">{{ state.data?.name || '-' }}</span>
                        </div>
                        <div class="info-item info-item--full">
                            <span class="info-k">{{ $t('lz.label.base.location') }}</span>
                            <span class="info-v">{{ state.data?.remark || '-' }}</span>
                        </div>
                        <div class="info-item info-item--full">
                            <span class="info-k">{{ $t('lz.entity.base.desc') }}</span>
                            <span class="info-v">{{ state.data?.info || '-' }}</span>
                        </div>
                    </div>
                </div>
            </div>
        </template>

        <!-- Statistic -->
        <template #statisticTab>
            <div class="border-info-cus">
                <table class="stats-table">
                    <tr>
                        <th>{{ $t('lz.label.base.vehicle') }}</th>
                        <th style="text-align: right;">{{ $t('lz.label.base.count') }}</th>
                    </tr>
                    <tr>
                        <td>{{ $t('lz.label.unitInfo.totalVehicle') }}</td>
                        <td>{{ generateRandomCount() }}</td>
                    </tr>
                    <tr>
                        <td>{{ $t('lz.label.unitInfo.passedVehicleCount') }}</td>
                        <td>{{ generateRandomCount() }}</td>
                    </tr>
                    <tr>
                        <td>{{ $t('lz.label.unitInfo.deniedVehicleCount') }}</td>
                        <td>{{ generateRandomCount() }}</td>
                    </tr>
                </table>
            </div>
        </template>
    </InfoPanel>
</template>

<script setup lang="ts" name="wosInfoModal">
import { reactive, computed, defineAsyncComponent } from 'vue';
import { useI18n } from 'vue-i18n';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import InfoPanel from '/@/components/bigScreen/floatingModal/InfoPanel.vue';

const { t: $t } = useI18n();
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));

const emits = defineEmits(['activated', 'closed', 'closeOthers', 'closeAll']);

const state = reactive({
    activeTab: 'infoTab',
    data: null as any,
});

const tabs = computed(() => [
    { label: $t('lz.label.base.info'), name: 'infoTab' },
    { label: $t('lz.label.base.statistic'), name: 'statisticTab' },
]);

const generateRandomCount = () => {
    return Math.floor(Math.random() * 100) + 1;
};

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
