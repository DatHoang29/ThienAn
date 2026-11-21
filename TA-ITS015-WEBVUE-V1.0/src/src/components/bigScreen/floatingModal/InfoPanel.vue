<template>
    <div class="background-modal-container">
        <!-- Header: tabs + title + dropdown close (dùng chung) -->
        <div class="modal-header" style="padding: 0;">
            <el-tabs
                v-if="tabs.length"
                v-model="activeTabProxy"
                class="header-tabs"
                :style="computedTabsStyle"
                @tab-change="onTabChange"
            >
                <el-tab-pane v-for="tab in tabs" :key="tab.name" :label="tab.label" :name="tab.name" />
            </el-tabs>

            <!-- Vùng tiêu đề: cho phép override bằng slot #title, mặc định dùng prop title/subtitle -->
            <slot name="title">
                <div v-if="title || subtitle" class="main-title">
                    {{ title }}<template v-if="subtitle"> - {{ subtitle }}</template>
                </div>
            </slot>

            <!-- Khối thông tin thiết bị trên header (tên + vị trí), dùng chung - override bằng slot #header-device-info -->
            <slot name="header-device-info">
                <div v-if="deviceName || deviceLocation" class="header-device-info" :class="statusVariant ? 'header-device-info--' + statusVariant : ''">
                    <div class="device-name">{{ deviceName }}</div>
                    <div v-if="deviceLocation" class="device-location">
                        <el-icon class="location-icon"><Location /></el-icon>
                        {{ deviceLocation }}
                    </div>
                </div>
            </slot>

            <!-- Thông tin bổ sung trên header -->
            <slot name="header-extra" />

            <div class="close-actions">
                <el-dropdown trigger="hover" class="close-dropdown" @command="handleCloseCommand">
                    <!-- Click X = đóng luôn; vẫn giữ hover để xổ menu đóng khác/đóng tất cả -->
                    <div class="close-btn" @click.stop="handleCloseCommand('close')">
                        <el-icon><Close /></el-icon>
                    </div>
                    <template #dropdown>
                        <el-dropdown-menu>
                            <el-dropdown-item command="close">{{ $t('lz.button.base.close') }}</el-dropdown-item>
                            <el-dropdown-item command="closeOthers">{{ $t('lz.button.base.closeOther') }}</el-dropdown-item>
                            <el-dropdown-item command="closeAll">{{ $t('lz.button.base.closeAll') }}</el-dropdown-item>
                        </el-dropdown-menu>
                    </template>
                </el-dropdown>
            </div>
        </div>

        <!-- Content: mỗi tab có 1 slot theo tên tab (dùng v-show để component con luôn mounted) -->
        <div class="modal-content" @mousedown.stop>
            <template v-if="tabs.length">
                <div v-for="tab in tabs" v-show="activeTabProxy === tab.name" :key="tab.name" class="tab-pane-content">
                    <slot :name="tab.name" />
                </div>
            </template>
            <slot v-else />
        </div>

        <!-- Footer tuỳ chọn: chứa thông tin hoặc nút chức năng -->
        <div v-if="showFooter" class="modal-footer" @mousedown.stop>
            <slot name="footer" />
        </div>
    </div>
</template>

<script setup lang="ts" name="infoPanel">
import { computed, useSlots } from 'vue';
import { Close, Location } from '@element-plus/icons-vue';
import { ElTabs, ElTabPane, ElDropdown, ElDropdownMenu, ElDropdownItem, ElIcon } from 'element-plus';
import { useI18n } from 'vue-i18n';

interface TabItem {
    label: string;
    name: string;
}

const props = withDefaults(
    defineProps<{
        /** Danh sách tab hiển thị trên header. Rỗng => chỉ hiển thị slot default */
        tabs?: TabItem[];
        /** Tab đang chọn (dùng v-model:activeTab) */
        activeTab?: string;
        /** Tiêu đề chính (vd: tên thiết bị/đơn vị) */
        title?: string;
        /** Dòng phụ (vd: Km12+300) */
        subtitle?: string;
        /** Tên thiết bị hiển thị ở khối header-device-info (góc phải) */
        deviceName?: string;
        /** Vị trí thiết bị hiển thị ở khối header-device-info (vd: Km12+300) */
        deviceLocation?: string;
        /** Tô màu header-device-info theo trạng thái thiết bị: 'active' | 'inactive' | 'fault' (rỗng = mặc định primary) */
        statusVariant?: string;
        /** Bật/tắt footer */
        showFooter?: boolean;
        /** style bổ sung cho thanh tab */
        tabsStyle?: string;
    }>(),
    {
        tabs: () => [],
        activeTab: '',
        title: '',
        subtitle: '',
        deviceName: '',
        deviceLocation: '',
        statusVariant: '',
        showFooter: false,
        tabsStyle: 'margin-left: 10px;',
    }
);

const slots = useSlots();

// Có khối header-device-info hay không (qua props hoặc slot) => chừa khoảng cho thanh tab
const hasDeviceInfo = computed(() => !!(props.deviceName || props.deviceLocation || slots['header-device-info']));
const computedTabsStyle = computed(() => `${props.tabsStyle}${hasDeviceInfo.value ? ' padding-right: 210px;' : ''}`);

const emit = defineEmits<{
    (e: 'update:activeTab', name: string): void;
    (e: 'tabChange', name: string): void;
    (e: 'closed'): void;
    (e: 'closeOthers'): void;
    (e: 'closeAll'): void;
}>();

const { t: $t } = useI18n();

const activeTabProxy = computed<string>({
    get: () => props.activeTab || props.tabs[0]?.name || '',
    set: (v) => emit('update:activeTab', v),
});

const onTabChange = (name: any) => emit('tabChange', name as string);

// Xử lý menu đóng (dùng chung cho mọi panel)
const handleCloseCommand = (command: string) => {
    switch (command) {
        case 'close':
            emit('closed');
            break;
        case 'closeOthers':
            emit('closeOthers');
            break;
        case 'closeAll':
            emit('closeAll');
            break;
    }
};
</script>

<style scoped lang="scss">
@import '/@/theme/its/trafficInfo.scss';

.tab-pane-content {
    height: 100%;
}

/* Nút đóng (dropdown) định vị góc phải header - dùng chung */
.close-actions {
    position: absolute;
    right: 10px;
    top: 10px;
    z-index: 2;
    display: flex;
    align-items: center;

    .close-dropdown .close-btn {
        cursor: pointer;
        padding: 4px;
        border-radius: 6px;
        transition: background-color 0.25s, box-shadow 0.25s, transform 0.15s;

        .el-icon {
            font-size: 16px;
        }

        &:hover {
            background-color: color-mix(in srgb, var(--el-color-danger) 18%, transparent);
            box-shadow: 0 0 12px -2px color-mix(in srgb, var(--el-color-danger) 70%, transparent);
            transform: scale(1.08);
        }
    }
}

/* Khối thông tin thiết bị trên header (dùng chung) */
.header-device-info {
    position: absolute;
    right: 38px;
    top: 50%;
    transform: translateY(-50%);
    z-index: 2;
    display: flex;
    flex-direction: column;
    align-items: flex-end;
    background: linear-gradient(135deg, var(--el-color-primary), var(--el-color-primary-dark-2));
    border-radius: 8px;
    padding: 3px 10px;
    border: 1px solid color-mix(in srgb, var(--el-color-primary-light-3) 55%, transparent);
    border-left: 3px solid var(--el-color-primary-light-3);
    box-shadow: 0 0 16px -3px color-mix(in srgb, var(--el-color-primary) 75%, transparent);
    min-width: 90px;
    max-width: 160px;

    /* Tô màu theo trạng thái thiết bị (active/inactive/fault) */
    &.header-device-info--active {
        background: linear-gradient(135deg, var(--el-color-success), color-mix(in srgb, var(--el-color-success) 65%, #000));
        border-color: color-mix(in srgb, var(--el-color-success) 55%, transparent);
        border-left-color: var(--el-color-success);
        box-shadow: 0 0 16px -3px color-mix(in srgb, var(--el-color-success) 75%, transparent);
    }

    &.header-device-info--inactive {
        background: linear-gradient(135deg, var(--el-color-info), color-mix(in srgb, var(--el-color-info) 65%, #000));
        border-color: color-mix(in srgb, var(--el-color-info) 55%, transparent);
        border-left-color: var(--el-color-info);
        box-shadow: 0 0 16px -3px color-mix(in srgb, var(--el-color-info) 70%, transparent);
    }

    &.header-device-info--fault {
        background: linear-gradient(135deg, var(--el-color-danger), color-mix(in srgb, var(--el-color-danger) 65%, #000));
        border-color: color-mix(in srgb, var(--el-color-danger) 55%, transparent);
        border-left-color: var(--el-color-danger);
        box-shadow: 0 0 16px -3px color-mix(in srgb, var(--el-color-danger) 75%, transparent);
    }

    .device-name {
        color: #fff;
        font-size: 13px;
        font-weight: 700;
        line-height: 1.3;
        letter-spacing: 0.5px;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
        max-width: 150px;
        text-shadow: 0 1px 2px rgba(0, 0, 0, 0.4);
    }

    .device-location {
        display: flex;
        align-items: center;
        gap: 3px;
        color: rgba(255, 255, 255, 0.85);
        font-size: 11px;
        font-weight: 400;
        letter-spacing: 0.3px;
        margin-top: 1px;

        .location-icon {
            font-size: 11px;
            color: rgba(255, 255, 255, 0.7);
        }
    }
}

.modal-footer {
    flex: 0 0 auto;
    padding: 6px 12px;
    /* Đồng bộ tông tối với thân modal (background-modal-container: $deep-1/$deep-2)
       thay vì nền trắng cũ gây lệch màu. Viền + chữ theo tông primary/sáng. */
    border-top: $hairline;
    background: color-mix(in srgb, var(--el-color-primary) 10%, $deep-1);
    color: $text-strong;
    backdrop-filter: blur(6px);
    display: flex;
    justify-content: flex-end;
    align-items: center;
    gap: 10px;
}
</style>
