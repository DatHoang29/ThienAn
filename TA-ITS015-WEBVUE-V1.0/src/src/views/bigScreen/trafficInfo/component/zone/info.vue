<template>
	<div class="content-text">
		<div class="zone-col">
			<!--Tổng quan-->
			<div class="border-content-info">
				<h3 class="section-title">{{ $t('lz.label.zoneInfo.infoTabTitle') }}</h3>
				<div class="info-grid">
					<div class="info-item">
						<span class="info-k">{{ $t('lz.entity.base.name') }}</span>
						<span class="info-v">{{ state.zoneInfo?.name || '-' }}</span>
					</div>
					<div class="info-item">
						<!-- <span class="info-k">{{ $t('lz.entity.base.type') }}</span>
						<span class="info-v">{{ state.zoneTypeName || '-' }}</span> -->
					</div>
					<div class="info-item info-item--full">
						<span class="info-k">{{ $t('lz.label.base.position') }}</span>
						<span class="info-v">
							Km{{ state.zoneInfo?.fromKmNumber ?? 0 }}+{{ state.zoneInfo?.fromMetNumber ?? 0 }}
							- Km{{ state.zoneInfo?.toKmNumber ?? 0 }}+{{ state.zoneInfo?.toMetNumber ?? 0 }}
						</span>
					</div>
					<div class="info-item info-item--full">
						<span class="info-k">{{ $t('lz.label.base.camera') }}</span>
						<span class="info-v">
							<div v-if="state.cameraList?.length > 0" class="camera-btn-list">
								<el-button v-for="cam in state.cameraList" :key="cam.id" size="small" round type="primary" @click="showCameraPopup(cam)">{{ commonVarsAndFuncs.formatCameraName(cam.name, cam.kmNumber, cam.metNumber) }}</el-button>
							</div>
							<span v-else class="zone-empty">{{ $t('lz.label.base.noData') }}</span>
						</span>
					</div>
				</div>
			</div>

			<!--Thông tin giao thông-->
			<!-- <div class="border-content-info">
				<h3 class="section-title">{{ $t('lz.label.zoneInfo.trafficSummary') }}</h3>
				<div class="info-grid">
					<div class="info-item">
						<span class="info-k">{{ $t('lz.label.zoneInfo.vehicleCount') }}</span>
						<span class="info-v">{{ commonVarsAndFuncs.getRandomNumber(105, 505) }}</span>
					</div>
					<div class="info-item">
						<span class="info-k">{{ $t('lz.label.zoneInfo.averageSpeed') }}</span>
						<span class="info-v">{{ commonVarsAndFuncs.getRandomNumber(25, 85) }} Km/h</span>
					</div>
					
					<div class="info-item">
						<span class="info-k">{{ $t('lz.label.zoneInfo.timestamp') }}</span>
						<span class="info-v">{{ useDateFormat(new Date(), DateTimeFormatEnum.DateTimeDefault) }}</span>
					</div>
					<div class="info-item">
						<span class="info-k">{{ $t('lz.label.zoneInfo.intervalTime') }}</span>
						<span class="info-v">{{ commonVarsAndFuncs.getRandomNumber(25, 85) }}</span>
					</div>
				</div>
			</div> -->
		</div>

		<!--Danh sách sự kiện sự cố-->
		<div class="border-content-info mt10">
			<div class="zone-incident-head">
				<h3 class="section-title zone-incident-title">{{ $t('lz.label.zoneInfo.incidentList') }}</h3>
				<incidentButtonsComp :zoneId="state.zoneInfo?.id" :kmNumber="state.midKmNumber" :metNumber="state.midMetNumber" :influenceScope="state.zoneInfo?.laneId?.toString()" @onSubmitSuccess="handleQuery" />
			</div>
			<div class="zone-incident-list">
				<p v-if="(state.incidentList?.length ?? 0) == 0" class="zone-empty">{{ $t('lz.label.base.noData') }}</p>
				<div v-for="incident in state.incidentList" :key="incident.id" class="zone-incident-item">
					<TagInfo class="zi-state" :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsIncidentStatus, incident.state)" />
					<span class="zi-name">{{ incident.name }}</span>
					<span class="zi-pos">{{ incident?.kmNumber ?? 0 }}Km +{{ incident?.metNumber ?? 0 }}</span>
				</div>
			</div>
		</div>
	</div>
	<cameraPopupComp ref="cameraPopupCompRef"/>
</template>

<script lang="ts" setup>
	const cameraPopupComp = defineAsyncComponent(() => import('/@/views/modal/common/cameraPopup.vue'));
	const cameraPopupCompRef = ref<InstanceType<typeof cameraPopupComp>>();

	import { onMounted, reactive, ref, watch } from 'vue';
	import { getAPI } from '/@/utils/axios-utils';
	import { useI18n } from 'vue-i18n';
	import { useDateFormat } from '@vueuse/core';
	import { defineAsyncComponent } from 'vue';
	import { DateTimeFormatEnum } from '/@/types/enums/baseEnum';
    import { ElMessage } from 'element-plus';
	import { TmsIncidentStateEnum } from '/@/types/enums';

	import { useBaseConfig } from '/@/stores/baseConfig';
	import { BaseConfigTypeEnum } from '/@/types/enums';

	const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
	const getConfigItemByCode = useBaseConfig().getConfigItemByCode;

	const { t: $t } = useI18n();

	const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));

    import { useCommonVarsAndFuncs } from '/@/stores/its/commonVarsAndFuncs';
	import { TmsEquipmentApi, TmsEquipmentTypeApi, TmsZoneApi } from '/@/api-services/tms';
	const commonVarsAndFuncs = useCommonVarsAndFuncs();

	const incidentButtonsComp = defineAsyncComponent(() => import('/@/views/bigScreen/trafficInfo/component/incidentButtons.vue'));

	const props = defineProps({
		zoneInfo: {
			type: Object,
			default: null
		},
		incidentList: {
			type: Array,
			default: null
		},
		equipmentList: {
			type: Array,
			default: null
		}
	});

	// Declare local viewbag
	const state = reactive({
		isProcessing: false,
		midKmNumber: 0,
		midMetNumber: 0,
		zoneInfo: {} as any,
		zoneTypeName: '',
        incidentList: [] as any,
		zoneTypeList: [] as any,
		equipmentList: [] as any,
		equipmentTypeList: [] as any,
		cameraList: [] as any
	});

	let cameraRequestId = 0;

	// Page initialization
	onMounted(() => {
		state.zoneTypeList = getConfigDataByCode(BaseConfigTypeEnum.TmsZoneType);
	});

	// Dùng đúng tiêu chí của editIncident.vue: EquipmentType có code chứa "CCTV" là camera.
	// Không lọc qua ModuleId vì liên kết module của type không đồng nhất với dữ liệu đang hiển thị.
	const loadEquipmentTypes = async () => {
		if ((state.equipmentTypeList?.length ?? 0) > 0)
			return state.equipmentTypeList;

		const response = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet();
		state.equipmentTypeList = response.data.result ?? [];
		return state.equipmentTypeList;
	};

	const isCCTVCamera = (equipmentTypeId: string | null | undefined) => {
		const equipmentTypeCode = state.equipmentTypeList?.find((item: any) => item.id == equipmentTypeId)?.code;
		return equipmentTypeCode ? commonVarsAndFuncs.isCCTVCamera(equipmentTypeCode, 'CCTV,PTZ') : false;
	};

	const loadCameraList = async (zoneId: string | null | undefined) => {
		const requestId = ++cameraRequestId;
		state.cameraList = [];

		if (!zoneId)
			return;

		try {
			await loadEquipmentTypes();

			// Zone modal nhận ID của polygon được click (có thể là zone cha), trong khi
			// TmsZoneEquipment thường được map tại zone con. Lấy cả root + descendants
			// để dùng cùng phạm vi thiết bị với luồng chọn zone của editIncident.vue.
			const zoneTreeResponse = await getAPI(TmsZoneApi).apiTmsTmszoneGettreewithchildGet(zoneId, undefined, undefined, false);
			const zoneTreeResult = zoneTreeResponse.data.result as any;
			const zones = Array.isArray(zoneTreeResult) ? zoneTreeResult : zoneTreeResult?.value ?? [];
			const zoneIds = [...new Set(zones.map((zone: any) => zone.id).filter(Boolean))] as string[];

			const response = await getAPI(TmsEquipmentApi).apiTmsTmsequipmentListbyzonelistPost({
				zoneIds: zoneIds.length > 0 ? zoneIds : [zoneId]
			});

			// Bỏ kết quả cũ nếu người dùng đã chuyển sang zone khác trước khi API trả về.
			if (requestId != cameraRequestId || zoneId != props.zoneInfo?.id)
				return;

			const cameraList = response.data.result?.filter((item: any) => isCCTVCamera(item.equipmentTypeId)) ?? [];
			state.cameraList = cameraList.filter((camera: any, index: number, items: any[]) => index == items.findIndex(item => item.id == camera.id));
		}
		catch (error) {
			if (requestId == cameraRequestId)
				state.cameraList = [];
			console.error('zone/info.vue: loadCameraList(...)', error);
		}
	};

	const showCameraPopup = (cameraInfo: any) => {
		if(cameraInfo)
			cameraPopupCompRef.value?.openDialog(cameraInfo);
		else
			ElMessage.error($t('lz.label.base.noData'));
	};

	// Đồng bộ thông tin hiển thị; không gọi lại API camera khi incident/equipment prop thay đổi.
	watch(() => [props.zoneInfo, props.incidentList, props.equipmentList], () => {
		state.zoneInfo = props.zoneInfo;
		state.incidentList = props.incidentList?.filter(item =>
			(item.state == TmsIncidentStateEnum.Verified || item.state == TmsIncidentStateEnum.InProgress)
			&& item.zoneId?.includes(props.zoneInfo?.id)
		) ?? [];
		state.equipmentList = props.equipmentList ?? [];

		if (!state.zoneInfo)
			return;

		state.zoneTypeName = state.zoneInfo.type;
		const fromPosition = (state.zoneInfo.fromKmNumber ?? 0) * 1000 + (state.zoneInfo.fromMetNumber ?? 0);
		const toPosition = (state.zoneInfo.toKmNumber ?? 0) * 1000 + (state.zoneInfo.toMetNumber ?? 0);

		if (fromPosition < toPosition) {
			state.midKmNumber = state.zoneInfo.fromKmNumber ?? 0;
			state.midMetNumber = (state.zoneInfo.fromMetNumber ?? 0) + 1;
		}
		else if (fromPosition > toPosition) {
			state.midKmNumber = state.zoneInfo.toKmNumber ?? 0;
			state.midMetNumber = (state.zoneInfo.toMetNumber ?? 0) + 1;
		}
	}, { immediate: true });

	// Chỉ tải camera khi zone đổi; `immediate` bảo đảm vẫn nạp khi prop đã có trước lúc mount.
	watch(() => props.zoneInfo?.id, loadCameraList, { immediate: true });
</script>
<style scoped lang="scss">
    @import '/@/theme/its/trafficInfo.scss';

    /* 2 khối thông tin cạnh nhau */
    .zone-2col {
        display: grid;
        grid-template-columns: repeat(2, minmax(0, 1fr));
        gap: 10px;
    }

    /* Danh sách camera dạng nút */
    .camera-btn-list {
        display: flex;
        flex-wrap: wrap;
        gap: 5px;

        .el-button {
            margin: 0;
        }
    }

    .zone-empty {
        color: var(--el-text-color-secondary);
        font-size: 12px;
    }

    /* Tiêu đề + nút thêm sự cố trên cùng 1 hàng */
    .zone-incident-head {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 10px;
        margin-bottom: 8px;

        .zone-incident-title {
            margin-bottom: 0;
        }
    }

    /* Danh sách sự cố */
    .zone-incident-list {
        display: flex;
        flex-direction: column;
        gap: 6px;

        .zone-incident-item {
            display: flex;
            align-items: center;
            gap: 10px;
            padding: 6px 10px;
            border-radius: 8px;
            background: color-mix(in srgb, var(--el-color-danger) 5%, #fff);
            border: 1px solid rgba(20, 40, 70, 0.06);
            font-size: 12.5px;

            /* zi-state giờ là TagInfo (el-tag) — màu/nội dung lấy theo config TmsIncidentStatus,
               chỉ chỉnh kích thước/khoảng cách để khớp layout hàng ngang cũ */
            :deep(.zi-state) {
                flex-shrink: 0;
                font-weight: 700;
                font-size: 11px;
                text-transform: uppercase;
                letter-spacing: 0.3px;
            }

            .zi-name {
                flex: 1;
                min-width: 0;
                font-weight: 600;
                color: var(--el-text-color-primary);
                white-space: nowrap;
                overflow: hidden;
                text-overflow: ellipsis;
            }

            .zi-pos {
                flex-shrink: 0;
                color: var(--el-text-color-secondary);
            }
        }
    }
</style>
