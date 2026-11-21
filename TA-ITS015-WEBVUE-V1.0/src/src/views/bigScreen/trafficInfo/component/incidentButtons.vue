<template>
    <el-dropdown
        split-button
        type="primary"
        :size="props.dropdownSize"
        class="incident-add-btn"
        @click="showEditIncidentComp(null)"
        trigger="click"
        popper-class="incident-add-popper"
    >
        <span class="incident-add-btn__label">
            <el-icon class="incident-add-btn__icon"><Plus /></el-icon>
            <span>{{ $t('lz.button.tmsIncident.addIncident') }}</span>
        </span>
        <template #dropdown>
            <el-dropdown-menu class="incident-dropdown-menu">
                <el-dropdown-item
                    v-for="item in state.eventTypeList"
                    :key="item.id"
                    @click="showEditIncidentComp(item.id)"
                >
                    <span class="incident-add-popper__icon">
                        <img :src="media.eventTypeIcon(item)" alt="" @error="media.onEventTypeIconError" />
                    </span>
                    <span>{{ $t('lz.button.tmsIncident.addIncident', { eventType: item.name }) }}</span>
                </el-dropdown-item>
            </el-dropdown-menu>
        </template>
    </el-dropdown>
</template>

<script lang="ts" setup name="incidentButtons">
	import { onMounted, reactive } from 'vue';
	import { useI18n } from 'vue-i18n';
	import { Plus } from '@element-plus/icons-vue';
	import { useMapStore } from '/@/stores/its/mapStore';
	import { useItsMediaServerStore } from '/@/stores/its/itsMediaServer';
	import { useBaseConfig } from '/@/stores/baseConfig';
	import { useIncidentMonitor } from '/@/stores/its/incidentMonitor';
	import { BaseOpConfigCodeEnum, BaseOpConfigGroupCodeEnum, StatusTypeEnum } from '/@/types/enums/baseEnum';

	const { t: $t } = useI18n();
	const mapStore = useMapStore();
	const media = useItsMediaServerStore();
	const incidentMonitor = useIncidentMonitor();
	const getOpConfigItemByCode = useBaseConfig().getOpConfigItemByCode;

	interface Emits {
		(e: 'onSubmitSuccess'): void;
	}
	const emit = defineEmits<Emits>();

	const props = defineProps({
		dropdownSize: {
			type: String,
			default: 'small',
		},
		zoneId: String,
		equipmentId: String,
		kmNumber: Number,
		metNumber: Number,
		influenceScope: String,
	});

	const state = reactive({
		eventTypeList: [] as any[],
	});

	// Mở dialog tạo nhanh sự cố với loại sự cố đã chọn
	const showEditIncidentComp = (eventTypeId: string | null = null) => {
		const row = {
			eventTypeId: eventTypeId,
			zoneId: props.zoneId,
			equipmentId: props.equipmentId,
			kmNumber: props.kmNumber,
			metNumber: props.metNumber,
			influenceScope: props.influenceScope,
			source: 'user',
			startDate: Date.now(),
		};

		const title = $t('lz.label.base.titleAdd', { name: $t('lz.label.tms.tmsIncident') });
		incidentMonitor.setIncidentDialogState(true, title, false, row);
	};

	// Nạp danh sách loại sự cố cho dropdown tạo nhanh, lọc theo cấu hình opConfig
	const loadDefaultData = async () => {
		if (state.eventTypeList.length > 0) return;

		if (mapStore.eventTypeList && mapStore.eventTypeList.length < 1) {
			await mapStore.fetchEventTypeData();
		}

		const allTypes = [...mapStore.eventTypeList];

		// Cấu hình danh sách sự cố cho phép đăng ký nhanh, nội dung là các Code loại sự cố cách nhau bởi ","
		const quickRegisterConfig = getOpConfigItemByCode(BaseOpConfigGroupCodeEnum.ItsOperationConfig, BaseOpConfigCodeEnum.EventQuickRegister)?.value;
		const codes = (quickRegisterConfig ?? '')
			.split(',')
			.map((c: string) => c.trim())
			.filter((c: string) => c);

		if (codes.length > 0) {
			// Chỉ hiển thị các loại sự cố được cấu hình và đang hoạt động, giữ đúng thứ tự trong cấu hình
			const typeMap = new Map(allTypes.map((e: any) => [e.code, e]));
			state.eventTypeList = codes.map((c) => typeMap.get(c)).filter((e) => !!e && e.status === StatusTypeEnum.Enable);
		} else {
			// Không cấu hình → hiển thị toàn bộ các loại đang hoạt động
			state.eventTypeList = allTypes.filter((e: any) => e.status === StatusTypeEnum.Enable);
		}
	};

	// Xử lý khi tạo sự cố thành công
	const handleOnSubmitSuccess = () => {
		emit('onSubmitSuccess');
	};

	onMounted(() => {
		loadDefaultData();
	});
</script>

<style lang="scss">
.display-flex--horizontal {
    display: flex;
    align-items: center;
}
.el-popper {
    z-index: 4000 !important;
}
.incident-dropdown-menu {
    z-index: 4000 !important;
}

/* Nút thêm sự cố: cụm split-button liền khối, tông primary trầm.
   Style KHÔNG scoped -> dùng selector thường, không dùng :deep. */
.incident-add-btn {
    .el-button-group {
        display: inline-flex;
        border-radius: 6px;
        box-shadow: 0 2px 8px -2px color-mix(in srgb, var(--el-color-primary) 55%, transparent);
    }

    .el-button-group > .el-button {
        border: none !important;
        font-weight: 600;
        letter-spacing: 0.2px;
        background: color-mix(in srgb, var(--el-color-primary) 90%, #0f172a);
        transition: background-color 0.2s ease;

        &:hover,
        &:focus-visible {
            background: var(--el-color-primary);
        }
    }

    /* Nút chính: bo trái */
    .el-button-group > .el-button:first-child {
        border-radius: 6px 0 0 6px !important;
        padding-left: 12px;
        padding-right: 12px;
    }

    /* Nút mũi tên: bo phải, nền đậm hơn 1 bậc + vạch ngăn mảnh */
    .el-button-group > .el-dropdown__caret-button {
        border-radius: 0 6px 6px 0 !important;
        width: 28px;
        padding: 0;
        background: color-mix(in srgb, var(--el-color-primary) 78%, #0f172a);
        box-shadow: inset 1px 0 0 rgba(255, 255, 255, 0.22);

        &::before {
            display: none;
        }
    }

    .incident-add-btn__label {
        display: inline-flex;
        align-items: center;
        gap: 5px;
    }

    .incident-add-btn__icon {
        font-size: 14px;
    }
}

/* Menu xổ: thẻ trắng bo góc, item thoáng, icon tròn tông primary */
.el-popper.incident-add-popper {
    border: 1px solid #e2e8f0 !important;
    border-radius: 8px;
    box-shadow: 0 12px 28px -8px rgba(15, 23, 42, 0.28) !important;
    overflow: hidden;

    .el-popper__arrow {
        display: none;
    }

    .el-dropdown-menu {
        padding: 4px;
        min-width: 180px;
    }

    .el-dropdown-menu__item {
        gap: 8px;
        padding: 7px 12px 7px 8px;
        border-radius: 6px;
        font-size: 13px;
        color: #1e293b;
        line-height: 20px;

        .incident-add-popper__icon {
            flex: 0 0 auto;
            display: inline-flex;
            align-items: center;
            justify-content: center;
            width: 28px;
            height: 28px;
            border-radius: 50%;
            background: color-mix(in srgb, var(--el-color-primary) 12%, #ffffff);
            transition: background-color 0.2s ease, box-shadow 0.2s ease;

            img {
                width: 20px;
                height: 20px;
                object-fit: contain;
            }
        }

        &:not(.is-disabled):hover,
        &:not(.is-disabled):focus {
            background-color: color-mix(in srgb, var(--el-color-primary) 8%, #ffffff);
            color: var(--el-color-primary);

            .incident-add-popper__icon {
                background: color-mix(in srgb, var(--el-color-primary) 22%, #ffffff);
                box-shadow: 0 0 0 1px color-mix(in srgb, var(--el-color-primary) 40%, transparent);
            }
        }
    }
}
</style>
