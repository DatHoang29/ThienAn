<template>
    <!-- Chỉ render khi thực sự có sự cố đang hiển thị (cur != null), tránh hiện card rỗng khi danh sách vừa bị co lại -->
    <div 
        v-if="state.incidentList.length > 0 && cur" 
        class="incident-notification-container"
        :style="{
            '--border-tac-color': state.incidentList.length > 0 ? 'var(--el-color-danger)' : 'var(--el-color-primary)'
        }"
    >
        <el-tooltip
            placement="bottom-start"
            effect="dark"
            :offset="10"
            :show-after="120"
            popper-class="incident-tip-popper"
        >
            <template #content>
                <div class="itip">
                    <div class="itip__head">
                        <div class="itip__icon-wrap">
                            <img class="itip__icon" :src="getIncidentIcon(cur?.eventTypeId)">
                        </div>
                        <div class="itip__head-main">
                            <div class="itip__title">{{ eventTypeName }}</div>
                            <div class="itip__km">{{ kmText }}</div>
                        </div>
                        <span v-if="statusName" class="itip__status">{{ statusName }}</span>
                    </div>

                    <div class="itip__rows">
                        <div class="itip__row" v-if="locationName">
                            <el-icon class="itip__row-ic"><MapLocation /></el-icon>
                            <span class="itip__row-lb">{{ $t('lz.label.tms.zone') }}</span>
                            <span class="itip__row-val">{{ locationName }}</span>
                        </div>
                        <div class="itip__row" v-if="directionName">
                            <el-icon class="itip__row-ic"><Guide /></el-icon>
                            <span class="itip__row-lb">{{ $t('lz.entity.tms.direction') }}</span>
                            <span class="itip__row-val">{{ directionName }}</span>
                        </div>
                        <div class="itip__row" v-if="cur?.startDate">
                            <el-icon class="itip__row-ic"><Clock /></el-icon>
                            <span class="itip__row-lb">{{ $t('lz.label.base.dateTime') }}</span>
                            <span class="itip__row-val">{{ useDateFormat(cur?.startDate, DateTimeFormatEnum.DateTimeDefault) }}</span>
                        </div>
                        <div class="itip__row" v-if="cur?.remark">
                            <el-icon class="itip__row-ic"><Document /></el-icon>
                            <span class="itip__row-lb">{{ $t('lz.entity.base.remark') }}</span>
                            <span class="itip__row-val">{{ cur?.remark }}</span>
                        </div>
                    </div>
                </div>
            </template>

            <transition name="fade-slide" mode="out-in">
                <div
                    :key="cur?.id ?? state.currentIndex"
                    class="incident-item display-flex--horizontal flex-gap--10px align-items--center"
                    @mouseenter="handleMouseEnter"
                    @mouseleave="handleMouseLeave"
                    @click="handleClickIncident"
                >
                    <div class="incident-icon-wrap">
                        <span class="incident-icon-pulse"></span>
                        <img
                            class="incident-icon"
                            :src="getIncidentIcon(cur?.eventTypeId)"
                        >
                    </div>
                    <div class="incident-info">
                        <div class="incident-location text-uppercase text-bold">
                            {{ kmText }}
                            <span v-if="directionName">- {{ directionName }}</span>
                        </div>
                        <div class="incident-name text-uppercase">
                            {{ eventTypeName }}
                            <span v-if="statusName" class="text-none">- {{ statusName }}</span>
                        </div>
                        <div class="incident-zone" v-if="locationName">
                            <el-icon><MapLocation /></el-icon>
                            <span>{{ locationName }}</span>
                        </div>
                    </div>
                    <!-- Nhãn thứ hạng ưu tiên: TOP n (n = thứ tự trong danh sách ưu tiên nhất, không phải tổng số sự cố) -->
                    <div class="incident-rank">
                        <span class="incident-rank__tag">TOP</span>
                        <span class="incident-rank__num">{{ state.currentIndex + 1 }}</span>
                    </div>
                </div>
            </transition>
        </el-tooltip>
    </div>
</template>

<script lang="ts" setup>
    import { onMounted, onUnmounted, reactive, watch, computed } from 'vue';
    import { MapLocation, Guide, Clock, Document } from '@element-plus/icons-vue';
    import { getAPI } from '/@/utils/axios-utils';
    import { TmsIncidentApi } from '/@/api-services/tms';
    import { useI18n } from 'vue-i18n';

    import { useIncidentMonitor } from '/@/stores/its/incidentMonitor';
    import { DateTimeFormatEnum, UploadAssetItemsEnum } from '/@/types/enums/baseEnum';
    import { useItsMediaServerStore } from '/@/stores/its/itsMediaServer';

    import { useCommonVarsAndFuncs } from '/@/stores/its/commonVarsAndFuncs';
    import { useUserInfo } from '/@/stores/userInfo';

    import { useMapStore } from '/@/stores/its/mapStore';
    import { useBaseConfig } from '/@/stores/baseConfig';
    import { BaseConfigTypeEnum } from '/@/types/enums';
    import { useTransporterIncident } from '/@/transporter/hooks/useTransporterIncident';
import { useDateFormat } from '@vueuse/core';
    const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
    const mapStore = useMapStore();

    const commonVarsAndFuncs = useCommonVarsAndFuncs();
    const userInfoStore = useUserInfo();

    const incidentMonitor = useIncidentMonitor();

    const { t: $t } = useI18n();
    const emit = defineEmits(['hasIncident']);
    const state = reactive({
        lastestIncident: null as any,
        incidentList: [] as any,
        currentIndex: 0 as number,
        eventTypeList: [] as any,
        baseUrl: '' as any,
        flipInterval: null as any,
    });

    // Số thứ tự request tăng dần: NATS có thể bắn liên tiếp nhiều event => nhiều request song song.
    // Chỉ nhận kết quả của request mới nhất, tránh response về muộn ghi đè danh sách mới bằng danh sách cũ.
    let fetchSeq = 0;

    // Đồng bộ lại vị trí đang hiển thị sau khi danh sách được làm mới.
    // Sự cố đang hiển thị có thể đã kết thúc/bị huỷ nên không còn trong danh sách trả về:
    // nếu giữ nguyên currentIndex cũ (vượt quá độ dài mới) thì cur = null và card sẽ hiển thị
    // nội dung rỗng kiểu "0Km +0". Vì vậy phải giữ đúng bản ghi cũ nếu còn, ngược lại clamp lại.
    const syncCurrentIndex = (previousId?: string | null) => {
        if (state.incidentList.length < 1) {
            state.currentIndex = 0;
            return;
        }

        const keepIndex = previousId ? state.incidentList.findIndex((item: any) => item.id === previousId) : -1;
        state.currentIndex = keepIndex >= 0 ? keepIndex : Math.min(state.currentIndex, state.incidentList.length - 1);
    };

    const getLatestIncident = async() =>{
        const seq = ++fetchSeq;
        // Ghi nhớ sự cố đang hiển thị trước khi làm mới để giữ đúng vị trí nếu nó vẫn còn
        const previousId = state.incidentList[state.currentIndex]?.id;

        let response = await getAPI(TmsIncidentApi).apiTmsTmsincidentLatestincidentPost({count: 5});
        if (seq !== fetchSeq) return; // đã có request mới hơn => bỏ kết quả này

        state.incidentList = response.data.result || [];
        state.lastestIncident = state.incidentList.at(0) ?? null;
        syncCurrentIndex(previousId);

        emit('hasIncident', !!state.lastestIncident);

        // Địa chỉ file server không đổi theo từng sự cố => chỉ lấy 1 lần
        if(!state.baseUrl)
        {
            // Địa chỉ file server dùng chung (chỉ gọi API 1 lần cho toàn app)
            state.baseUrl = await useItsMediaServerStore().ensureLoaded();
        }
    }

    // Sự cố đang hiển thị + các thông tin dẫn xuất (dùng cho card & tooltip)
    const cur = computed<any>(() => state.incidentList[state.currentIndex] ?? null);
    // Không trả về "0Km +0" khi chưa có sự cố: tránh hiển thị nội dung rỗng lúc danh sách vừa thay đổi
    const kmText = computed(() => (cur.value ? (cur.value.kmNumber ?? 0) + 'Km +' + (cur.value.metNumber ?? 0) : ''));
    const eventTypeName = computed(() => cur.value?.eventTypeName ?? (getEventType(cur.value?.eventTypeId)?.name ?? cur.value?.name));
    const statusName = computed(() => cur.value?.state != null ? getConfigItemByCode(BaseConfigTypeEnum.TmsIncidentStatus, cur.value?.state + '')?.name : '');
    const directionName = computed(() => cur.value?.influenceScope != null ? getConfigItemByCode(BaseConfigTypeEnum.TmsDirectionType, cur.value?.influenceScope + '')?.name : '');
    const locationName = computed(() => cur.value?.location != null ? getConfigItemByCode(BaseConfigTypeEnum.TmsZoneLocation, cur.value?.location + '')?.name : '');

    const getEventType = (evenTypeId: string | null) => {
        if(!evenTypeId)
            return null;

        return state.eventTypeList?.find((item: any) => item.id == evenTypeId);
    };

    const getIncidentIcon = (eventTypeId: string | null) => {
        const eventType = getEventType(eventTypeId);

        return eventType?.url ? commonVarsAndFuncs.getFullUrl(eventType.url) : commonVarsAndFuncs.getFullUrl(UploadAssetItemsEnum.WarningIcon);
    };

    const hasIncidentPermission = () => {
        const authBtnList = userInfoStore.userInfos?.authBtnList || [];

        return authBtnList.includes('tmsIncident:list')
            || authBtnList.includes('tmsIncident:view')
            || authBtnList.includes('tmsIncident:popup')
            || authBtnList.includes('tmsIncident');
    };

    const formatDateTime = (dateString: string) => {
        if (!dateString) return '';
        const date = new Date(dateString);
        return date.toLocaleString('vi-VN', {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit'
        });
    };

    const startFlipAnimation = () => {
        if (state.flipInterval) {
            clearInterval(state.flipInterval);
        }
        state.flipInterval = setInterval(() => {
            if (state.incidentList.length > 0) {
                state.currentIndex = (state.currentIndex + 1) % state.incidentList.length;
            }
        }, 5000);
    };

    const stopFlipAnimation = () => {
        if (state.flipInterval) {
            clearInterval(state.flipInterval);
            state.flipInterval = null;
        }
    };

    const handleMouseEnter = () => {
        stopFlipAnimation();
    };

    const handleMouseLeave = () => {
        if (state.incidentList.length > 0) {
            startFlipAnimation();
        }
    };

    const handleClickIncident = async () => {
        if(await incidentMonitor.userCanViewData() === false)
            return;
        const currentIncidentId = state.incidentList[state.currentIndex]?.id;
        if(currentIncidentId)
            incidentMonitor.selectedIncidentId = currentIncidentId;

        incidentMonitor.isShowIncidentPopup = true;
    };

    // Lắng nghe sự cố realtime qua mittBus (NATS) => thay cho watch store cũ
    useTransporterIncident(() => {
        getLatestIncident();
    }, { immediate: true });

    watch(() => state.incidentList, (newList) => {
        if (newList && newList.length > 0) {
            startFlipAnimation();
        } else {
            stopFlipAnimation();
        }
    });

    watch(() => state.incidentList.length, (count) => {
        const borderColor = count > 0 ? 'var(--el-color-danger)' : 'var(--el-color-primary)';
        document.documentElement.style.setProperty('--border-tac-color', borderColor);
        // Bật/tắt animation viền cảnh báo toàn app chỉ khi có sự cố
        document.documentElement.classList.toggle('has-incident', count > 0);
    }, { immediate: true });

    onMounted(async() => {
        if(state.eventTypeList && state.eventTypeList.length < 1)
        {
            if(mapStore.eventTypeList && mapStore.eventTypeList.length < 1)
            {
                await  mapStore.fetchEventTypeData()              
            }
            state.eventTypeList = [...mapStore.eventTypeList]
        }

        getLatestIncident();
    });

    onUnmounted(() => {
        stopFlipAnimation();
    });
</script>

<style lang="scss" scoped>
.incident-notification-container {
    position: relative;
    width: 300px;
    min-width: 300px;
    max-width: 300px;
    box-sizing: border-box;
    margin-top: 5px;
}

@keyframes incidentPulse {
    0% { box-shadow: 0 0 0 0 color-mix(in srgb, var(--el-color-danger) 55%, transparent); }
    70% { box-shadow: 0 0 0 8px rgba(255, 255, 255, 0); }
    100% { box-shadow: 0 0 0 0 rgba(255, 255, 255, 0); }
}

.incident-item {
    position: relative;
    display: flex;
    align-items: center;
    gap: 10px;
    /* Cao 46px để lọt thanh topBar (50px) mà không bị cắt viền */
    height: 46px;
    padding: 0 10px;
    cursor: pointer;
    width: 100%;
    box-sizing: border-box;
    border-radius: 14px;
    /* Nền hồng đỏ nhạt chuyển dần, nhấn mạnh trạng thái cảnh báo */
    background: linear-gradient(135deg,
        color-mix(in srgb, var(--el-color-danger) 7%, #fff),
        color-mix(in srgb, var(--el-color-danger) 17%, #fff));
    border: 2px solid color-mix(in srgb, var(--el-color-danger) 55%, transparent);
    box-shadow: 0 8px 24px -14px color-mix(in srgb, var(--el-color-danger) 60%, transparent),
                0 1px 2px rgba(15, 30, 55, 0.06);
    transition: transform 0.2s ease, box-shadow 0.2s ease, border-color 0.2s ease;

    &:hover {
        transform: translateY(-1px);
        border-color: var(--el-color-danger);
        box-shadow: 0 12px 30px -12px color-mix(in srgb, var(--el-color-danger) 70%, transparent),
                    0 1px 2px rgba(15, 30, 55, 0.08);
    }
}

.incident-icon-wrap {
    position: relative;
    flex-shrink: 0;
    display: flex;
    align-items: center;
    justify-content: center;
    width: 34px;
    height: 34px;

    /* Chấm "live" nhấp nháy góc trên phải icon */
    .incident-icon-pulse {
        position: absolute;
        top: -2px;
        right: -3px;
        width: 9px;
        height: 9px;
        border-radius: 50%;
        background: var(--el-color-danger);
        animation: incidentPulse 1.6s infinite;
    }
}

.incident-icon {
    width: 28px;
    height: 28px;
    object-fit: contain;
}

/* Nhãn thứ hạng ưu tiên (TOP n) - không phải tổng số sự cố */
.incident-rank {
    flex-shrink: 0;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    line-height: 1;
    min-width: 34px;
    padding: 5px 7px;
    border-radius: 9px;
    background: linear-gradient(160deg,
        color-mix(in srgb, var(--el-color-danger) 85%, #fff),
        color-mix(in srgb, var(--el-color-danger) 90%, #000));
    box-shadow: 0 4px 10px -4px color-mix(in srgb, var(--el-color-danger) 70%, transparent);

    .incident-rank__tag {
        font-size: 8px;
        font-weight: 700;
        letter-spacing: 0.8px;
        color: rgba(255, 255, 255, 0.85);
    }

    .incident-rank__num {
        margin-top: 1px;
        font-size: 17px;
        font-weight: 800;
        color: #fff;
    }
}

.incident-info {
    display: flex;
    flex-direction: column;
    gap: 0;
    flex: 1;
    min-width: 0;
}

.incident-location {
    font-size: 15px;
    font-weight: 800;
    line-height: 1.15;
    letter-spacing: 0.2px;
    color: color-mix(in srgb, var(--el-color-danger) 92%, #000);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.incident-name {
    font-size: 12px;
    line-height: 1.15;
    color: var(--el-text-color-primary);
    font-weight: 700;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;

    .text-none {
        color: var(--el-text-color-primary);
        font-weight: 700;
        text-transform: uppercase;
    }
}

/* Dòng khu vực (tuyến chính/phụ) trên card */
.incident-zone {
    display: flex;
    align-items: center;
    gap: 4px;
    font-size: 10px;
    line-height: 1.15;
    color: var(--el-text-color-secondary);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;

    .el-icon {
        font-size: 12px;
        color: color-mix(in srgb, var(--el-color-primary) 70%, transparent);
        flex-shrink: 0;
    }
}

// Fade-slide animation - smooth and simple
.fade-slide-enter-active,
.fade-slide-leave-active {
    transition: all 0.3s ease;
}

.fade-slide-enter-from {
    opacity: 0;
    transform: translateY(10px);
}

.fade-slide-leave-to {
    opacity: 0;
    transform: translateY(-10px);
}

.fade-slide-enter-to,
.fade-slide-leave-from {
    opacity: 1;
    transform: translateY(0);
}
</style>

<!-- Style global cho tooltip (el-tooltip teleport ra body nên không dùng scoped) -->
<style lang="scss">
.incident-tip-popper.el-popper {
    --itip-danger: var(--el-color-danger);
    width: 300px;
    max-width: 300px;
    padding: 0 !important;
    border-radius: 12px !important;
    border: 1px solid color-mix(in srgb, var(--itip-danger) 40%, transparent) !important;
    background: linear-gradient(180deg, rgba(24, 30, 42, 0.98), rgba(16, 20, 30, 0.98)) !important;
    box-shadow: 0 18px 40px -12px rgba(0, 0, 0, 0.6),
                inset 0 1px 0 rgba(255, 255, 255, 0.06) !important;
    overflow: hidden;

    .itip {
        display: flex;
        flex-direction: column;
    }

    /* Header: icon + tiêu đề + trạng thái */
    .itip__head {
        display: flex;
        align-items: center;
        gap: 10px;
        padding: 12px 14px;
        background: linear-gradient(135deg,
            color-mix(in srgb, var(--itip-danger) 30%, transparent),
            color-mix(in srgb, var(--itip-danger) 6%, transparent));
        border-bottom: 1px solid rgba(255, 255, 255, 0.08);
    }

    .itip__icon-wrap {
        flex-shrink: 0;
        width: 34px;
        height: 34px;
        border-radius: 9px;
        display: flex;
        align-items: center;
        justify-content: center;
        background: rgba(255, 255, 255, 0.1);
        border: 1px solid color-mix(in srgb, var(--itip-danger) 35%, transparent);
    }

    .itip__icon {
        width: 22px;
        height: 22px;
        object-fit: contain;
    }

    .itip__head-main {
        flex: 1;
        min-width: 0;
    }

    .itip__title {
        font-size: 13px;
        font-weight: 700;
        color: #fff;
        text-transform: uppercase;
        letter-spacing: 0.3px;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
    }

    .itip__km {
        font-size: 12px;
        font-weight: 600;
        color: color-mix(in srgb, var(--itip-danger) 45%, #fff);
        margin-top: 2px;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
    }

    .itip__status {
        flex-shrink: 0;
        align-self: flex-start;
        font-size: 10px;
        font-weight: 700;
        letter-spacing: 0.4px;
        padding: 3px 8px;
        border-radius: 999px;
        color: #fff;
        background: color-mix(in srgb, var(--itip-danger) 70%, transparent);
        border: 1px solid color-mix(in srgb, var(--itip-danger) 55%, transparent);
        white-space: nowrap;
    }

    /* Body: các dòng thông tin */
    .itip__rows {
        display: flex;
        flex-direction: column;
        padding: 8px 14px 12px;
        gap: 8px;
    }

    .itip__row {
        display: flex;
        align-items: center;
        gap: 8px;
        font-size: 12px;
        line-height: 1.4;
        color: rgba(255, 255, 255, 0.92);
    }

    .itip__row-ic {
        flex-shrink: 0;
        font-size: 14px;
        color: color-mix(in srgb, var(--itip-danger) 45%, #fff);
    }

    .itip__row-lb {
        flex-shrink: 0;
        min-width: 74px;
        color: rgba(255, 255, 255, 0.55);
    }

    .itip__row-val {
        flex: 1;
        min-width: 0;
        font-weight: 600;
        word-break: break-word;
    }

    .el-popper__arrow::before {
        background: rgba(24, 30, 42, 0.98) !important;
        border-color: color-mix(in srgb, var(--itip-danger) 40%, transparent) !important;
    }
}
</style>