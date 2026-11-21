<template>
	<div class="detail-incident-container">
		<el-dialog v-model="state.isShowDialog" width="800" draggable append-to-body class="incident-detail-dialog">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-View /></el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>

			<div class="incident-detail">
				<!-- Tiêu đề sự kiện: tên, mã, loại sự kiện và trạng thái -->
				<section class="incident-detail__hero">
					<div class="incident-detail__hero-main">
						<div class="incident-detail__name">{{ state.incidentInfo.name || '—' }}</div>
						<div v-if="state.incidentInfo.code" class="incident-detail__code">
							<span>{{ state.incidentInfo.code }}</span>
							<el-tooltip :content="$t('lz.button.base.copy')" placement="top">
								<i class="fa fa-clone incident-detail__copy" @click="copyText(state.incidentInfo.code)"></i>
							</el-tooltip>
						</div>
						<div class="incident-detail__tags">
							<el-tag v-if="view.eventType" effect="plain" round>
								<i class="fa fa-exclamation-triangle"></i>&nbsp;{{ view.eventType }}
							</el-tag>
						</div>
					</div>
					<div class="incident-detail__status">
						<span class="incident-detail__label">{{ $t('lz.entity.base.status') }}</span>
						<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsIncidentStatus, state.incidentInfo.state)" />
					</div>
				</section>

				<!-- Số liệu chính -->
				<section class="incident-detail__stats">
					<div class="incident-detail__stat">
						<div class="incident-detail__stat-icon is-primary"><i class="fa fa-map-marker"></i></div>
						<div>
							<div class="incident-detail__label">{{ $t('lz.label.base.position') }}</div>
							<div class="incident-detail__stat-value">{{ view.position || '—' }}</div>
						</div>
					</div>
					<div class="incident-detail__stat">
						<div class="incident-detail__stat-icon is-success"><i class="fa fa-clock-o"></i></div>
						<div>
							<div class="incident-detail__label">{{ $t('lz.entity.tmsIncident.startDate') }}</div>
							<div class="incident-detail__stat-value">{{ view.time || '—' }}</div>
							<div v-if="view.date" class="incident-detail__stat-sub">{{ view.date }}</div>
						</div>
					</div>
					<div class="incident-detail__stat">
						<div class="incident-detail__stat-icon is-danger"><i class="fa fa-users"></i></div>
						<div>
							<div class="incident-detail__label">{{ $t('lz.entity.tmsIncident.injuredNumber') }}</div>
							<div class="incident-detail__stat-value">{{ view.injured }}</div>
						</div>
					</div>
					<div class="incident-detail__stat">
						<div class="incident-detail__stat-icon is-warning"><i class="fa fa-car"></i></div>
						<div>
							<div class="incident-detail__label">{{ $t('lz.entity.tmsIncident.vehicleNumber') }}</div>
							<div class="incident-detail__stat-value">{{ view.vehicles }}</div>
						</div>
					</div>
				</section>

				<!-- Vị trí & phạm vi ảnh hưởng -->
				<section class="incident-detail__section">
					<div class="incident-detail__section-title"><i class="fa fa-road"></i>{{ $t('lz.label.tms.incidentScope') }}</div>
					<div class="incident-detail__grid">
						<div class="incident-detail__field">
							<div class="incident-detail__label">{{ $t('lz.label.base.location') }}</div>
							<div class="incident-detail__value" :class="{ 'is-empty': !view.location }">{{ view.location || '—' }}</div>
						</div>
						<div class="incident-detail__field">
							<div class="incident-detail__label">{{ $t('lz.entity.tmsIncident.influenceScope') }}</div>
							<div class="incident-detail__value" :class="{ 'is-empty': !view.scope }">{{ view.scope || '—' }}</div>
						</div>
						<div class="incident-detail__field">
							<div class="incident-detail__label">{{ $t('lz.entity.tmsIncident.zoneId') }}</div>
							<div v-if="state.zoneList.length" class="incident-detail__value">
								<div v-for="item in state.zoneList" :key="item.id">
									<span class="incident-detail__strong">{{ item.name }}</span>
									<span class="incident-detail__muted">
										· {{ commonVarsAndFuncs.formatZonePosition(item.fromKmNumber, item.fromMetNumber, item.toKmNumber, item.toMetNumber) }}
									</span>
								</div>
							</div>
							<div v-else class="incident-detail__value is-empty">—</div>
						</div>
						<div class="incident-detail__field">
							<div class="incident-detail__label">{{ $t('lz.label.base.camera') }}</div>
							<div v-if="state.cameraList.length" class="incident-detail__value incident-detail__chips">
								<el-tag v-for="item in state.cameraList" :key="item.id" type="info" size="small">
									<i class="fa fa-video-camera"></i>&nbsp;{{ item.name }}
								</el-tag>
							</div>
							<div v-else class="incident-detail__value is-empty">—</div>
						</div>
					</div>
				</section>

				<!-- Thông tin bổ sung -->
				<section class="incident-detail__section">
					<div class="incident-detail__section-title"><i class="fa fa-info-circle"></i>{{ $t('lz.label.base.otherInfo') }}</div>
					<div class="incident-detail__grid">
						<div class="incident-detail__field">
							<div class="incident-detail__label">{{ $t('lz.entity.tmsIncident.source') }}</div>
							<div class="incident-detail__value" :class="{ 'is-empty': !view.source }">{{ view.source || '—' }}</div>
						</div>
						<div class="incident-detail__field">
							<div class="incident-detail__label">{{ $t('lz.entity.base.createBy') }}</div>
							<div class="incident-detail__value" :class="{ 'is-empty': !state.incidentInfo.createdBy }">
								<template v-if="state.incidentInfo.createdBy">
									<i class="fa fa-user-circle-o incident-detail__muted"></i>&nbsp;{{ state.incidentInfo.createdBy }}
								</template>
								<template v-else>—</template>
							</div>
						</div>
						<div class="incident-detail__field is-full">
							<div class="incident-detail__label">{{ $t('lz.entity.base.info') }}</div>
							<div class="incident-detail__value is-multiline" :class="{ 'is-empty': !state.incidentInfo.description }">{{ state.incidentInfo.description || '—' }}</div>
						</div>
						<div class="incident-detail__field is-full">
							<div class="incident-detail__label">{{ $t('lz.entity.base.remark') }}</div>
							<div class="incident-detail__value is-multiline" :class="{ 'is-empty': !state.incidentInfo.remark }">{{ state.incidentInfo.remark || '—' }}</div>
						</div>
					</div>
				</section>

				<IncidentVehicleInfo :metadata="state.incidentInfo.metadata" />
			</div>

			<template #footer>
				<span class="dialog-footer">
					<el-button type="danger" @click="state.isShowDialog = false">{{ $t('lz.button.base.close') }}</el-button>
				</span>
			</template>
		</el-dialog>
	</div>

	<cameraPopupComp ref="cameraPopupCompRef"/>
</template>
  
<script lang="ts" setup>
import { incidentEventTypeDisplay } from '/@/utils/incidentEventTypeDisplay';
	import IncidentVehicleInfo from './incidentVehicleInfo.vue';
import { reactive, onMounted, defineAsyncComponent, ref, computed } from 'vue';
	import commonFunction from '/@/utils/commonFunction';
	import { useI18n } from 'vue-i18n';
	import { useDateFormat } from '@vueuse/core';

	import { PageTmsIncidentOutput } from '/@/api-services/tms';
	import { useTmsStore } from '/@/stores/tmsStore';
	const tmsStore = useTmsStore();

	import { useCommonVarsAndFuncs } from '/@/stores/its/commonVarsAndFuncs';
	const commonVarsAndFuncs = useCommonVarsAndFuncs();

	// IMPORTANCE: pnpm install @cycjimmy/jsmpeg-player
	const cameraPopupComp = defineAsyncComponent(() => import('/@/views/modal/common/cameraPopup.vue'));
    const cameraPopupCompRef = ref<InstanceType<typeof cameraPopupComp>>();

	import { useBaseConfig } from '/@/stores/baseConfig';
	import { BaseConfigTypeEnum } from '/@/types/enums';
	const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
	const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
	const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));

	import { DateTimeFormatEnum } from '/@/types/enums/baseEnum';

	interface Props {
		operateType: OperateType;
		title: string;
	}

	const { t: $t } = useI18n();
	const props = defineProps<Props>();
  	const state = reactive({
		isShowDialog: false,
		dataList: {} as PageTmsIncidentOutput,
        incidentInfo: {} as PageTmsIncidentOutput,
		sourceDetectList: [] as any,
		eventTypeList: [] as any,
		locationList: [] as any,
		laneList: [] as any,
		zoneList: [] as any,
		incidentStatusList: [] as any,
		imageList: [] as any,
		cameraList: [] as any,
	});

	const { copyText } = commonFunction();

	// Giá trị hiển thị đã tra cứu sẵn từ danh mục, để template gọn
	const view = computed(() => {
		const info = state.incidentInfo;
		const hasPosition = info.kmNumber != null || info.metNumber != null;
		const count = (v: any) => (v === null || v === undefined || v === '' ? '—' : v);
		return {
			eventType: incidentEventTypeDisplay(info, state.eventTypeList),
			location: state.locationList?.find((item: any) => item.code == info.location)?.name,
			scope: state.laneList?.find((item: any) => item.code == info.influenceScope)?.name,
			source: state.sourceDetectList?.find((item: any) => item.code == info.source)?.name,
			position: hasPosition ? commonVarsAndFuncs.formatPosition(info.kmNumber, info.metNumber) : null,
			date: info.startDate ? useDateFormat(info.startDate, DateTimeFormatEnum.DateDefault).value : null,
			time: info.startDate ? useDateFormat(info.startDate, DateTimeFormatEnum.TimeDefault).value : null,
			injured: count(info.injuredNumber),
			vehicles: count(info.vehicleNumber),
		};
	});

	onMounted(async () => {
		state.sourceDetectList = getConfigDataByCode(BaseConfigTypeEnum.TmsSourceDetectIncident);
		state.locationList = getConfigDataByCode(BaseConfigTypeEnum.TmsZoneLocation);
		state.incidentStatusList = getConfigDataByCode(BaseConfigTypeEnum.TmsIncidentStatus);
		
		// TODO: LaneList is temporarily DirectionList => by direction_config_type not tms_lane
		state.laneList = getConfigDataByCode(BaseConfigTypeEnum.TmsDirectionType);

		state.eventTypeList = await tmsStore.getEventTypes();
	});

	let detailRequest = 0;
	const openDialog = async (row: any) => {
        const request = ++detailRequest;
        state.zoneList = [];
        state.cameraList = [];
		state.isShowDialog = true;

		if(!row)
			return;

		state.incidentInfo = JSON.parse(JSON.stringify(row));
		if(state.incidentInfo?.zoneId){
			//zoneId: "id01,id02,id03..."
			const zones = await tmsStore.getZonesByIds(state.incidentInfo.zoneId);
			if (request !== detailRequest) return;
            state.zoneList = zones;
		}

		if(state.incidentInfo?.equipmentId){
			//equipmentId: "id01,id02,id03..."
			const equipment = await tmsStore.getEquipmentByIds(state.incidentInfo.equipmentId);
			if (request !== detailRequest) return;
            state.cameraList = equipment;
		}
	};

	defineExpose({openDialog});
</script>

<style lang="scss" scoped>
	.incident-detail {
		display: flex;
		flex-direction: column;
		gap: 16px;
		color: var(--el-text-color-primary);
	}

	/* Khối tiêu đề */
	.incident-detail__hero {
		display: flex;
		align-items: flex-start;
		justify-content: space-between;
		gap: 16px;
		padding: 16px 18px;
		border: 1px solid var(--el-border-color-lighter);
		border-left: 4px solid var(--el-color-primary);
		border-radius: 8px;
		background: var(--el-fill-color-light);
	}

	.incident-detail__hero-main {
		min-width: 0;
		display: flex;
		flex-direction: column;
		gap: 6px;
	}

	.incident-detail__name {
		font-size: 17px;
		font-weight: 600;
		line-height: 1.4;
	}

	.incident-detail__code {
		display: flex;
		align-items: center;
		gap: 8px;
		font-family: Consolas, Menlo, monospace;
		font-size: 12px;
		color: var(--el-text-color-secondary);
		word-break: break-all;
	}

	.incident-detail__copy {
		cursor: pointer;
		color: var(--el-text-color-placeholder);
		transition: color 0.2s;

		&:hover {
			color: var(--el-color-primary);
		}
	}

	.incident-detail__tags {
		display: flex;
		flex-wrap: wrap;
		gap: 6px;
		margin-top: 2px;
	}

	.incident-detail__status {
		flex-shrink: 0;
		display: flex;
		flex-direction: column;
		align-items: flex-end;
		gap: 6px;
	}

	/* Thẻ số liệu */
	.incident-detail__stats {
		display: grid;
		grid-template-columns: repeat(4, minmax(0, 1fr));
		gap: 12px;
	}

	.incident-detail__stat {
		display: flex;
		align-items: center;
		gap: 12px;
		padding: 12px 14px;
		border: 1px solid var(--el-border-color-lighter);
		border-radius: 8px;
		background: var(--el-bg-color);
	}

	.incident-detail__stat-icon {
		flex-shrink: 0;
		width: 36px;
		height: 36px;
		display: flex;
		align-items: center;
		justify-content: center;
		border-radius: 50%;
		font-size: 16px;

		&.is-primary {
			color: var(--el-color-primary);
			background: var(--el-color-primary-light-9);
		}
		&.is-success {
			color: var(--el-color-success);
			background: var(--el-color-success-light-9);
		}
		&.is-danger {
			color: var(--el-color-danger);
			background: var(--el-color-danger-light-9);
		}
		&.is-warning {
			color: var(--el-color-warning);
			background: var(--el-color-warning-light-9);
		}
	}

	.incident-detail__stat-value {
		font-size: 16px;
		font-weight: 600;
		line-height: 1.3;
	}

	.incident-detail__stat-sub {
		font-size: 12px;
		color: var(--el-text-color-secondary);
	}

	/* Nhóm thông tin */
	.incident-detail__section {
		border: 1px solid var(--el-border-color-lighter);
		border-radius: 8px;
		overflow: hidden;
	}

	.incident-detail__section-title {
		display: flex;
		align-items: center;
		gap: 8px;
		padding: 10px 16px;
		font-weight: 600;
		font-size: 13px;
		background: var(--el-fill-color-light);
		border-bottom: 1px solid var(--el-border-color-lighter);

		i {
			color: var(--el-color-primary);
		}
	}

	.incident-detail__grid {
		display: grid;
		grid-template-columns: repeat(2, minmax(0, 1fr));
		gap: 14px 24px;
		padding: 14px 16px;
	}

	.incident-detail__field.is-full {
		grid-column: 1 / -1;
	}

	.incident-detail__label {
		font-size: 12px;
		color: var(--el-text-color-secondary);
		margin-bottom: 4px;
	}

	.incident-detail__value {
		font-size: 14px;
		line-height: 1.6;
		word-break: break-word;

		&.is-empty {
			color: var(--el-text-color-placeholder);
		}
		&.is-multiline {
			white-space: pre-line;
		}
	}

	.incident-detail__chips {
		display: flex;
		flex-wrap: wrap;
		gap: 6px;
	}

	.incident-detail__strong {
		font-weight: 500;
	}

	.incident-detail__muted {
		color: var(--el-text-color-secondary);
	}

	@media (max-width: 768px) {
		.incident-detail__stats {
			grid-template-columns: repeat(2, minmax(0, 1fr));
		}
		.incident-detail__grid {
			grid-template-columns: minmax(0, 1fr);
		}
		.incident-detail__hero {
			flex-direction: column;
		}
		.incident-detail__status {
			align-items: flex-start;
		}
	}

	/* Bảng thông tin phương tiện bên trong không cần margin riêng */
	:deep(.incident-vehicle-info) {
		margin: 0;
	}
</style>
