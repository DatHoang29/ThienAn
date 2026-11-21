<template>
	<!-- Gốc trang nhận style chung .layout-parent > div:first-child (flex column) -> bố cục trái/phải đặt ở div con -->
	<div class="equipment-state-chart-page" v-loading="options.loading">
	<div ref="layoutRef" class="equipment-state-chart-container" :class="{ 'type-collapsed': !typeVisible }" :style="layoutStyle">
		<section id="equipment-state-type-panel" class="type-pane" :aria-label="$t('lz.label.tmsEquipmentStateChart.equipmentTypeName')">
			<el-card shadow="hover">
				<div v-show="typeVisible" class="type-grid">
					<vxe-grid ref="xEquipmentType" class="xGrid-style" v-bind="options" v-on="gridEvents" @cell-click="onEquipmentTypeChange">
						<template #empty>
							<el-empty :image-size="200" />
						</template>

						<template #row_icon="{ row }">
							<div v-if="row.defaultIcon" class="div-img">
								<img :src="commonVarsAndFuncs.getFullUrl(row.defaultIcon)" />
							</div>
						</template>

						<template #row_rate="{ row }">
							<div class="div-progress">
								<el-progress :stroke-width="15" :percentage="row?.rate ?? 0" :color="row?.color" />
							</div>
						</template>

						<template #row_evaluation="{ row }">
							<el-button :color="row?.colorLabel" class="text-uppercase text-color--white" style="padding: 10px !important">
								{{ evaluationText(row) }}
							</el-button>
						</template>
					</vxe-grid>
				</div>

				<!-- Chế độ thu gọn: chỉ còn cột biểu tượng, hover để xem nhanh số lượng / tỉ lệ -->
				<div v-show="!typeVisible" class="type-rail">
					<el-tooltip v-for="row in state.rateList" :key="row.id" placement="right" effect="light" :show-after="80" popper-class="equipment-type-rail-tip">
						<template #content>
							<div class="rail-tip">
								<div class="rail-tip__name">{{ row.name }}</div>
								<div class="rail-tip__rate" :style="{ color: row.color }">{{ row.rate ?? 0 }}%</div>
								<el-progress :stroke-width="6" :percentage="row.rate ?? 0" :color="row.color" :show-text="false" />
								<div v-if="row.total != null" class="rail-tip__count">
									{{ $t('lz.label.tmsEquipmentStateChart.activeCount', { active: row.activeCount ?? 0, total: row.total }) }}
								</div>
								<span class="rail-tip__tag" :style="{ background: row.colorLabel }">{{ evaluationText(row) }}</span>
							</div>
						</template>
						<button
							type="button"
							class="type-rail__item"
							:class="{ active: row.id == state.equipmentTypeId }"
							:style="{ '--rate-color': row.color || 'var(--el-color-info)' }"
							:aria-label="`${row.name} ${row.rate ?? 0}%`"
							@click="selectEquipmentType(row)"
						>
							<img v-if="row.defaultIcon" :src="commonVarsAndFuncs.getFullUrl(row.defaultIcon)" alt="" />
							<span v-else class="type-rail__fallback">{{ row.name?.charAt(0) }}</span>
							<span class="type-rail__rate">{{ row.rate ?? 0 }}%</span>
						</button>
					</el-tooltip>
				</div>
			</el-card>
		</section>

		<PanelSplitter
			side="left"
			:label="$t('lz.label.tmsEquipmentStateChart.equipmentTypeName')"
			controls="equipment-state-type-panel"
			:width="typeVisible ? typeSize : 0"
			:min="typeMin"
			:max="typeMax"
			:expanded="typeVisible"
			:expand-label="$t('lz.label.base.extend')"
			:collapse-label="$t('lz.button.base.collapse')"
			:resize-hint="$t('lz.label.base.resizePanel', { name: $t('lz.label.tmsEquipmentStateChart.equipmentTypeName') })"
			@resize="resizeType"
			@step="stepType"
		/>

		<section class="chart-pane">
			<el-card shadow="hover">
				<div class="chart-header">
					<div class="chart-header__title">
						<img v-if="selectedType?.defaultIcon" :src="commonVarsAndFuncs.getFullUrl(selectedType.defaultIcon)" alt="" />
						<span>{{ selectedType?.name ?? state.equipmentTypeList?.find((item) => item.id == state.equipmentTypeId)?.name }}</span>
						<span v-if="selectedType" class="chart-header__tag" :style="{ background: selectedType.colorLabel }">{{ evaluationText(selectedType) }}</span>
					</div>
					<el-button type="primary" plain size="small" @click="goEquipmentPage">
						{{ $t('lz.label.tmsEquipmentStateChart.openEquipmentList') }}
						<el-icon class="el-icon--right"><TopRight /></el-icon>
					</el-button>
				</div>

				<!-- Thẻ tổng quan: tổng thiết bị + số lượng theo từng trạng thái (lấy từ dữ liệu biểu đồ tròn) -->
				<div class="kpi-row">
					<div class="kpi-card" :style="{ '--kpi-color': selectedType?.color || 'var(--el-color-primary)' }">
						<div class="kpi-card__label">{{ $t('lz.label.tmsEquipmentStateChart.total') }}</div>
						<div class="kpi-card__value">{{ stateSummary.total }}</div>
						<div class="kpi-card__sub">{{ $t('lz.label.tmsEquipmentStateChart.activityRate') }}: {{ selectedType?.rate ?? 0 }}%</div>
					</div>
					<div v-for="item in stateSummary.items" :key="item.name" class="kpi-card" :style="{ '--kpi-color': item.color || 'var(--el-color-info)' }">
						<div class="kpi-card__label">{{ item.name }}</div>
						<div class="kpi-card__value">{{ item.value }}</div>
						<div class="kpi-card__sub">{{ stateSummary.total ? Math.round((item.value * 100) / stateSummary.total) : 0 }}%</div>
					</div>
				</div>

				<div class="chart-grid">
					<div class="chart-panel chart-panel--pie">
						<pieChartComp ref="pieChartCompRef" :dataChangedCount="state.dataChangedCount" :chartData="state.pieChartData" @onChartMounted="onPieChartMounted" />
					</div>
					<div class="chart-panel chart-panel--line">
						<lineChartComp ref="lineChartCompRef" :dataChangedCount="state.dataChangedCount" :chartData="state.lineChartData" @onChartMounted="onLineChartMounted" />
					</div>
					<div class="chart-panel chart-panel--bar" :style="{ height: `${barChartHeight}px` }">
						<barChartComp ref="barChartCompRef" :dataChangedCount="state.dataChangedCount" :chartData="state.barChartData" @onChartMounted="onBarChartMounted" />
					</div>
				</div>
			</el-card>
		</section>
	</div>
	</div>
</template>

<script lang="ts" setup>

	import { onMounted, reactive, ref, watch, onUnmounted, computed, nextTick } from 'vue';
	import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
	import { useI18n } from 'vue-i18n';
	import { TopRight } from '@element-plus/icons-vue';

	import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
	import { getAPI } from '/@/utils/axios-utils';
	import mittBus from '/@/utils/mitt';
	import { TransporterEvent } from '/@/transporter/constants/transporterEvent';
	import { useResizeObserver, useThrottleFn } from '@vueuse/core';

	import PanelSplitter from '/@/components/workspace/PanelSplitter.vue';

	import { useCommonVarsAndFuncs } from '/@/stores/its/commonVarsAndFuncs';
	const commonVarsAndFuncs = useCommonVarsAndFuncs();

	import { PageValidEquipmentRateInput, PageValidEquipmentRateOutput, TmsEquipmentTypeApi, TmsEquipmentTypeChartApi } from '/@/api-services/tms';

	const {t:$t} = useI18n();

	const xEquipmentType = ref<VxeGridInstance>();

	import pieChartComp from '/@/views/fms/equipmentChart/component/pieChart.vue';
	const pieChartCompRef = ref<InstanceType<typeof pieChartComp>>();

	import barChartComp from '/@/views/fms/equipmentChart/component/barChart.vue';
	const barChartCompRef = ref<InstanceType<typeof barChartComp>>();

	import lineChartComp from '/@/views/fms/equipmentChart/component/lineChart.vue';
	import { FmsEquipmentHistoryChartApi } from '/@/api-services/fms';
	const lineChartCompRef = ref<InstanceType<typeof lineChartComp>>();

	// total / activeCount: BE bổ sung (PageValidEquipmentRateOutput), có thể chưa có trong model swagger cũ
	type EquipmentRateRow = PageValidEquipmentRateOutput & { total?: number; activeCount?: number };

	const state = reactive({
		title: null,
		equipmentTypeList: [] as any,
		rateList: [] as EquipmentRateRow[],
		equipmentTypeId: null as any,
		defaultEquipmentTypeId: null as any,
		dataChangedCount: 0,
		chartMountedCount: 0,
		pieChartData: {} as any,
		lineChartData: {} as any,
		barChartData: {} as any,
		localPageParam: {
			pageSize: 10000 as number,
			defaultSort: { field: 'orderNo', order: 'asc', descStr: 'desc' } as any,
		},
	});

	/* ---- Panel danh sách loại thiết bị (PanelSplitter): mở rộng / thu gọn còn cột biểu tượng ---- */
	const TYPE_RAIL_WIDTH = 92;
	const layoutRef = ref<HTMLElement>();
	const layoutWidth = ref(0);
	const typeVisible = ref(true);
	const typeWidth = ref<number>();
	const typeMin = 380;
	const typeMax = computed(() => Math.max(typeMin, Math.min((layoutWidth.value || 1600) * 0.55, (layoutWidth.value || 1600) - 520)));
	const typeNormal = computed(() => Math.min(Math.max(typeMin, (layoutWidth.value || 1600) * 0.33), typeMax.value));
	const typeSize = computed(() => Math.min(Math.max(typeMin, typeWidth.value ?? typeNormal.value), typeMax.value));
	// Gán trực tiếp grid-template-columns (không qua biến CSS) để bố cục luôn là trái | splitter | phải
	const layoutStyle = computed(() => ({ gridTemplateColumns: `${typeVisible.value ? typeSize.value : TYPE_RAIL_WIDTH}px 28px minmax(0, 1fr)` }));

	useResizeObserver(layoutRef, () => {
		layoutWidth.value = layoutRef.value?.clientWidth ?? 0;
	});

	const resizeType = (size: number) => {
		typeVisible.value = size > 0;
		if (size > 0) typeWidth.value = size;
	};

	const stepType = (direction: 'grow' | 'shrink') => {
		const normal = typeNormal.value;
		if (direction === 'grow') resizeType(!typeVisible.value || typeSize.value < normal - 1 ? normal : typeMax.value);
		else if (typeVisible.value) resizeType(typeSize.value > normal + 1 ? normal : 0);
	};

	// Lưới bị ẩn (v-show) khi thu gọn -> tính lại kích thước cột khi mở lại
	watch(typeVisible, async (visible) => {
		if (!visible) return;
		await nextTick();
		xEquipmentType.value?.recalculate();
	});

	const selectedType = computed(() => state.rateList.find((item) => item.id == state.equipmentTypeId));

	const evaluationText = (row: EquipmentRateRow) => (row?.rate == 100 ? $t('lz.label.tmsEquipmentStateChart.good') : $t('lz.label.tmsEquipmentStateChart.bad'));

	// Tổng hợp số lượng theo trạng thái từ dữ liệu biểu đồ tròn (cùng cách map với pieChart.vue)
	const stateSummary = computed(() => {
		const raw: any[] = Array.isArray(state.pieChartData) ? state.pieChartData : [];
		const items = raw.map((item: any) => ({
			name: item?.name ?? item?.Name ?? '',
			value: Number(item?.value ?? item?.Value ?? 0),
			color: item?.itemStyle?.color ?? item?.ItemStyle?.Color ?? item?.ItemStyle?.color,
		}));
		return { items, total: items.reduce((sum, item) => sum + item.value, 0) };
	});

	// Chiều cao biểu đồ cột co giãn theo số thiết bị (mỗi dòng ~44px) để không bị dồn khi nhiều thiết bị
	const barChartHeight = computed(() => {
		const yAxis = state.barChartData?.yAxis;
		const count = (Array.isArray(yAxis) ? yAxis[0]?.data : yAxis?.data)?.length ?? 0;
		return Math.max(360, count * 44 + 120);
	});

	onMounted(async() => {
	let response = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet();
	state.equipmentTypeList = response.data.result;
	mittBus.on(TransporterEvent.StatusFms as any, handleNatsStatusForChart);  //
	});

	onUnmounted(() => {
	mittBus.off(TransporterEvent.StatusFms as any, handleNatsStatusForChart); //
	});

	const goEquipmentPage = () => {
		window.open('#tms/equipment?Type=' + state.equipmentTypeId, '_blank');
	};

	const options = useVxeTable<PageValidEquipmentRateOutput>(
		{
			id: 'EquipmentTypeGrid',
			name: 'EquipmentTypeGrid',
			columns: [
				{ field: '', title: $t('lz.entity.base.icon'), minWidth: 80, showOverflow: 'tooltip', slots: { default: 'row_icon' } },
				{ field: 'name', title: $t('lz.label.tmsEquipmentStateChart.equipmentTypeName'), minWidth: 80, showOverflow: 'tooltip' },
				{ field: '', title: $t('lz.label.tmsEquipmentStateChart.activityRate'), minWidth: 160, showOverflow: 'tooltip', slots: { default: 'row_rate' } },
				{ field: '', title: '', minWidth: 100, showOverflow: 'tooltip', slots: { default: 'row_evaluation' } },
			],
		},
		{
			proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQuery(page, sort) } },
			rowConfig: { isCurrent: true, isHover: true, height: 80 },
			toolbarConfig: { export: false },
			pagerConfig: { autoHidden: true }
		}
	);

	const handleQuery = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
		const params = Object.assign(
			{
				page: page.currentPage,
				pageSize: page.pageSize,
				field: sort.field,
				order: sort.order
			}
		) as PageValidEquipmentRateInput;

		const response = await getAPI(TmsEquipmentTypeChartApi).apiTmsTmsequipmenttypechartValidequipmentratelistPost(params);

		state.rateList = (response.data.result?.records ?? []) as EquipmentRateRow[];

		if(response.data.result?.records?.length > 0){
        // Bổ sung điều kiện kiểm tra !state.equipmentTypeId
        // Để không ghi đè thiết bị người dùng đang chọn khi có NATS gửi về
        if (!state.equipmentTypeId) {
            state.equipmentTypeId = response.data.result.records[0].id;
            state.defaultEquipmentTypeId = response.data.result.records[0].id;
        }
    }

		return response;
	};

	const onEquipmentTypeChange = async(scope: any) => {
		if(scope && state.equipmentTypeId != scope.row.id){
			state.equipmentTypeId = scope.row.id;

			await loadEquipmentStateChart();
			await loadInvalidEquipmentHistoryChart();
			await loadInvalidEquipmentDetailHistoryChart();

			state.dataChangedCount++;
		}
	};

	// Chọn loại thiết bị từ cột biểu tượng (chế độ thu gọn): đồng bộ dòng đang chọn của lưới
	const selectEquipmentType = (row: EquipmentRateRow) => {
		xEquipmentType.value?.setCurrentRow(row);
		onEquipmentTypeChange({ row });
	};

	const loadEquipmentStateChart = async(equipmentTypeId: string = '') => {
		let response = await getAPI(TmsEquipmentTypeChartApi).apiTmsTmsequipmenttypechartEquipmentstatechartPost({equipmentTypeId: (equipmentTypeId ? equipmentTypeId: state.equipmentTypeId)});
		state.pieChartData = response.data.result?.seriesData;
	}

	const loadInvalidEquipmentHistoryChart = async(equipmentTypeId: string = '') => {
		let response = await getAPI(FmsEquipmentHistoryChartApi).apiFmsFmsequipmenthistorychartInvalidequipmenthistorychartPost({equipmentTypeId: (equipmentTypeId ? equipmentTypeId: state.equipmentTypeId)});
		state.lineChartData = response.data.result;
	}

	const loadInvalidEquipmentDetailHistoryChart = async(equipmentTypeId: string = '') => {
		let response = await getAPI(FmsEquipmentHistoryChartApi).apiFmsFmsequipmenthistorychartInvalidequipmenthistorydetailchartPost({equipmentTypeId: (equipmentTypeId ? equipmentTypeId: state.equipmentTypeId)});
		state.barChartData = response.data.result;
	}

	const onPieChartMounted = () => {
		state.chartMountedCount++;
	}

	const onLineChartMounted = () => {
		state.chartMountedCount++;
	}

	const onBarChartMounted = () => {
		state.chartMountedCount++;
	}

	const gridEvents: VxeGridListeners<PageValidEquipmentRateOutput> = {
		async pageChange({ pageSize }) {
			state.localPageParam.pageSize = pageSize;
		},
		async sortChange({ field, order }) {
			state.localPageParam.defaultSort = { field: field, order: order! };
		},
	};

	watch(() => [state.chartMountedCount, state.defaultEquipmentTypeId], async() => {
		if(state.chartMountedCount >= 3 && state.defaultEquipmentTypeId){
			state.chartMountedCount = 0;

			const equipmentTypeId = state.defaultEquipmentTypeId;

			await loadEquipmentStateChart(equipmentTypeId);
			await loadInvalidEquipmentHistoryChart(equipmentTypeId);
			await loadInvalidEquipmentDetailHistoryChart(equipmentTypeId);

			state.dataChangedCount++;
		}
	});

	const refreshAllCharts = async () => {
		if (!state.equipmentTypeId) return;
		try {
			await xEquipmentType.value?.commitProxy('query');

			await loadEquipmentStateChart();
			await loadInvalidEquipmentHistoryChart();
			await loadInvalidEquipmentDetailHistoryChart();

			state.dataChangedCount++;

			console.log('[equipmentStateChart] Đã refresh chart từ NATS.');
		} catch (error) {
			console.error('[equipmentStateChart] Lỗi refresh chart:', error);
		}
	};

	const throttledRefresh = useThrottleFn(refreshAllCharts, 5000);

// Xử lý khi nhận NATS event
const handleNatsStatusForChart = (payload: any) => {
  if (!payload) return;
  throttledRefresh();  // đã được lọc ở tầng client, gọi thẳng
};

</script>

<style lang="scss" scoped>
	.equipment-state-chart-page {
		height: calc(100vh - 84px);   /* vùng khả dụng: viewport trừ header + tab layout */
		display: flex;
		flex-direction: column;
		min-height: 0;
	}

	.equipment-state-chart-container {
		flex: 1;
		display: grid;
		grid-template-columns: 35% 28px minmax(0, 1fr);   /* mặc định; độ rộng thực tế gán qua :style */
		grid-template-rows: minmax(0, 1fr);
		min-height: 0;

		.type-pane,
		.chart-pane {
			min-width: 0;
			min-height: 0;
		}

		/* Card lấp đầy pane, chuyển sang flex column */
		:deep(.el-card) {
			height: 100%;
			display: flex;
			flex-direction: column;
		}

		/* Thân card là vùng cuộn: cả 2 pane đều có thanh cuộn khi tràn */
		:deep(.el-card__body) {
			flex: 1;
			min-height: 0;
			overflow-y: auto;
		}

		/* vxe-grid dùng height: 'auto' (cao theo phần tử cha) -> .type-grid phải lấp đầy thân card */
		.type-pane :deep(.el-card__body) {
			display: flex;
			flex-direction: column;
		}

		.type-grid {
			flex: 1;
			min-height: 0;
		}

		&.type-collapsed .type-pane :deep(.el-card__body) {
			padding: 8px 6px;
		}
	}

	.div-img img{
		margin: auto;
		width: 100%;
		max-width: 280px;
		max-height: 80px;
		padding: 10px;
	}

	.div-progress .el-progress--line {
		max-width: 200px;
		margin: auto;
	}

	/* ---- Cột biểu tượng khi thu gọn ---- */
	.type-rail {
		display: flex;
		flex-direction: column;
		gap: 6px;
	}

	.type-rail__item {
		position: relative;
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 4px;
		width: 100%;
		padding: 8px 4px 6px;
		border: 1px solid var(--el-border-color-lighter);
		border-radius: 8px;
		background: var(--el-bg-color);
		cursor: pointer;
		transition: background-color 0.15s, border-color 0.15s, box-shadow 0.15s;

		/* Vạch màu trạng thái bên trái: nhìn nhanh tốt / cảnh báo / lỗi */
		&::before {
			content: '';
			position: absolute;
			left: -1px;
			top: 8px;
			bottom: 8px;
			width: 4px;
			border-radius: 0 4px 4px 0;
			background: var(--rate-color);
		}

		img {
			width: 48px;
			height: 40px;
			object-fit: contain;
		}

		&:hover {
			border-color: var(--el-color-primary-light-5);
			background: var(--el-color-primary-light-9);
		}

		&.active {
			border-color: var(--el-color-primary);
			background: var(--el-color-primary-light-9);
			box-shadow: 0 0 0 1px var(--el-color-primary) inset;
		}

		&:focus-visible {
			outline: 2px solid var(--el-color-primary);
			outline-offset: 1px;
		}
	}

	.type-rail__fallback {
		display: grid;
		place-items: center;
		width: 40px;
		height: 40px;
		border-radius: 50%;
		background: var(--el-fill-color);
		font-weight: 700;
		color: var(--el-text-color-regular);
	}

	.type-rail__rate {
		font-size: 12px;
		font-weight: 700;
		line-height: 1;
		color: var(--rate-color);
	}

	.rail-tip {
		display: flex;
		flex-direction: column;
		gap: 6px;
		min-width: 180px;

		.rail-tip__name {
			font-weight: 600;
			font-size: 14px;
			color: var(--el-text-color-primary);
		}

		.rail-tip__rate {
			font-size: 22px;
			font-weight: 700;
			line-height: 1;
		}

		.rail-tip__count {
			font-size: 12px;
			color: var(--el-text-color-regular);
		}

		.rail-tip__tag {
			align-self: flex-start;
			padding: 2px 8px;
			border-radius: 4px;
			font-size: 11px;
			font-weight: 600;
			text-transform: uppercase;
			color: #fff;
		}
	}

	/* ---- Khu vực biểu đồ ---- */
	.chart-header {
		position: sticky;
		top: 0;
		z-index: 10;
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 12px;
		margin: -4px 0 12px;
		padding: 10px 14px;
		border-radius: 8px;
		background: var(--el-color-primary);
		color: #fff;

		.chart-header__title {
			display: flex;
			align-items: center;
			gap: 10px;
			min-width: 0;
			font-size: 16px;
			font-weight: 600;

			img {
				width: 32px;
				height: 28px;
				object-fit: contain;
				padding: 2px;
				border-radius: 4px;
				background: #fff;
			}

			span {
				white-space: nowrap;
				overflow: hidden;
				text-overflow: ellipsis;
			}
		}

		.chart-header__tag {
			flex: none;
			padding: 2px 8px;
			border-radius: 4px;
			font-size: 11px;
			text-transform: uppercase;
			border: 1px solid rgba(255, 255, 255, 0.6);
		}
	}

	.kpi-row {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
		gap: 10px;
		margin-bottom: 12px;
	}

	.kpi-card {
		position: relative;
		padding: 10px 12px 10px 16px;
		border: 1px solid var(--el-border-color-lighter);
		border-radius: 8px;
		background: var(--el-bg-color);
		overflow: hidden;

		&::before {
			content: '';
			position: absolute;
			left: 0;
			top: 0;
			bottom: 0;
			width: 4px;
			background: var(--kpi-color);
		}

		.kpi-card__label {
			font-size: 12px;
			color: var(--el-text-color-secondary);
			white-space: nowrap;
			overflow: hidden;
			text-overflow: ellipsis;
		}

		.kpi-card__value {
			margin-top: 2px;
			font-size: 24px;
			font-weight: 700;
			line-height: 1.2;
			color: var(--el-text-color-primary);
			font-variant-numeric: tabular-nums;
		}

		.kpi-card__sub {
			font-size: 12px;
			color: var(--el-text-color-secondary);
		}
	}

	.chart-grid {
		display: grid;
		/* Tròn | Đường luôn chia trái - phải (như bố cục cũ), cột ở hàng dưới chiếm hết chiều ngang */
		grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
		gap: 12px;
	}

	.chart-panel {
		min-width: 0;
		padding: 8px;
		border: 1px solid var(--el-border-color-lighter);
		border-radius: 8px;
		background: var(--el-bg-color);
	}

	.chart-panel--pie,
	.chart-panel--line {
		height: 360px;
	}

	.chart-panel--bar {
		grid-column: 1 / -1;
	}
</style>
