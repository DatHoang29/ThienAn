<template>
	<!--
		Màu ô tô bằng `cell-style` để vxe gán thẳng vào <td>. KHÔNG tô lên thẻ con bên trong:
		vxe bọc nội dung ô trong .vxe-cell / .vxe-cell--label, thẻ con sẽ co theo chữ nên khối màu
		không phủ kín ô — nhìn như mất màu.
	-->
	<vxe-table
		:data="rows"
		:cell-style="cellStyle"
		size="mini"
		border
		auto-resize
		:column-config="{ resizable: false }"
		:row-config="{ isHover: true, height: 28 }"
		:scroll-x="{ enabled: true }"
		class="matrix-table"
	>
		<vxe-column
			field="zoneName"
			:title="$t('lz.label.tmsTrafficMatrix.section')"
			fixed="left"
			min-width="220"
			align="right"
			header-align="center"
			show-overflow="tooltip"
		>
			<template #default="{ row }">
				<span :title="rowTitle(row)">{{ row.zoneName }}</span>
			</template>
		</vxe-column>

		<!-- 24 cột giờ -->
		<vxe-column
			v-for="hour in props.hours"
			:key="hour"
			:field="`h${hour}`"
			:title="String(hour).padStart(2, '0')"
			width="44"
			align="center"
		>
			<template #default="{ row, column }">
				<span :title="cellTitle(row[column.field])">{{ cellText(row[column.field]) }}</span>
			</template>
		</vxe-column>

		<vxe-column
			field="summary"
			:title="isSpeed ? $t('lz.label.tmsTrafficMatrix.dayAverage') : $t('lz.label.tmsTrafficMatrix.dayTotal')"
			fixed="right"
			width="84"
			align="center"
		>
			<template #default="{ row }">
				<span class="matrix-summary">{{ summaryText(row) }}</span>
			</template>
		</vxe-column>

		<template #empty>
			<el-empty :image-size="80" :description="$t('lz.label.base.noData')" />
		</template>
	</vxe-table>
</template>

<script lang="ts" setup name="tmsTrafficMatrixTable">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { TrafficMatrixBand, TrafficMatrixCellOutput, TrafficMatrixRowOutput } from '/@/api-services/tms';
import { TrafficMatrixMetric } from '/@/views/tms/trafficMatrix/metric';

const { t: $t } = useI18n();

interface Props {
	/* Các đoạn tuyến của một hướng, đã được BE sắp theo lý trình tăng dần */
	rowList: Array<TrafficMatrixRowOutput>;
	/* Danh sách giờ làm tiêu đề cột, BE trả về 0..23 */
	hours: Array<number>;
	/* Dải màu tương ứng chế độ xem đang chọn */
	bands: Array<TrafficMatrixBand>;
	metric: TrafficMatrixMetric;
}
const props = withDefaults(defineProps<Props>(), {
	metric: 'speed',
});

const isSpeed = computed(() => props.metric === 'speed');

/* Chỉ cột giờ mới tô màu; các cột khác (tên đoạn, ô tổng) giữ nguyên nền bảng */
const hourFieldPattern = /^h\d+$/;

/* Trải mảng cells thành các field h0..h23 để vxe-column đọc trực tiếp qua column.field,
   khỏi phải dò tìm theo giờ ở mỗi lần render ô. */
const rows = computed(() =>
	(props.rowList ?? []).map((row) => {
		const flat: any = { ...row };
		(row.cells ?? []).forEach((cell) => (flat[`h${cell.hour}`] = cell));
		return flat;
	})
);

const bandColor = (index?: number | null) => {
	if (index === undefined || index === null || index < 0) return '';
	return props.bands?.[index]?.color ?? '';
};

/* vxe gọi hàm này cho MỌI ô rồi gán kết quả vào style của <td> */
const cellStyle = ({ row, column }: any) => {
	const field = column?.field ?? '';
	if (!hourFieldPattern.test(field)) return {};

	const cell = row?.[field] as TrafficMatrixCellOutput | undefined;
	if (!cell) return {};

	const color = bandColor(isSpeed.value ? cell.speedBandIndex : cell.volumeBandIndex);
	return color ? { backgroundColor: color, color: 'var(--el-text-color-primary)' } : {};
};

/* Vận tốc không có mẫu thì để trống, KHÔNG hiển thị 0 — 0 km/h là xe đứng yên,
   khác hoàn toàn với việc không đo được. Lưu lượng thì 0 là số thật. */
const cellText = (cell?: TrafficMatrixCellOutput) => {
	if (!cell) return isSpeed.value ? '-' : 0;
	if (isSpeed.value) return cell.avgSpeed ?? '-';
	return cell.vehicleCount ?? 0;
};

const cellTitle = (cell?: TrafficMatrixCellOutput) => {
	if (!cell) return $t('lz.label.base.noData');

	const speed =
		cell.avgSpeed === null || cell.avgSpeed === undefined
			? $t('lz.label.tmsTrafficMatrix.noSpeedSample')
			: `${cell.avgSpeed} ${$t('lz.label.tmsTrafficMatrix.speedUnit')}`;

	return [
		`${$t('lz.label.base.hour')} ${String(cell.hour).padStart(2, '0')}`,
		`${$t('lz.label.tmsTrafficMatrix.metricSpeed')}: ${speed}`,
		`${$t('lz.label.tmsTrafficMatrix.metricVolume')}: ${cell.vehicleCount ?? 0}`,
		`${$t('lz.label.tmsTrafficMatrix.sampleCount')}: ${cell.speedSampleCount ?? 0}`,
	].join('\n');
};

const rowTitle = (row: TrafficMatrixRowOutput) => {
	const lines = [row.zoneName ?? ''];

	if (row.fromKmNumber !== null && row.fromKmNumber !== undefined) {
		const from = `Km${row.fromKmNumber}+${row.fromMetNumber ?? 0}`;
		const to =
			row.toKmNumber === null || row.toKmNumber === undefined ? '' : ` - Km${row.toKmNumber}+${row.toMetNumber ?? 0}`;
		lines.push(`${$t('lz.entity.tms.roadPosition')}: ${from}${to}`);
	}

	if (row.maxSpeed !== null && row.maxSpeed !== undefined)
		lines.push(`${$t('lz.label.tmsTrafficMatrix.maxSpeed')}: ${row.maxSpeed} ${$t('lz.label.tmsTrafficMatrix.speedUnit')}`);

	return lines.join('\n');
};

/* Ô tổng KHÔNG tô màu: tổng lưu lượng cả ngày nằm ở thang giá trị khác hẳn lưu lượng
   một giờ, dùng chung dải màu sẽ cho ra màu sai lệch về mặt nghĩa. */
const summaryText = (row: TrafficMatrixRowOutput) => {
	if (!isSpeed.value) return row.dayVehicleCount ?? 0;
	return row.dayAvgSpeed ?? '-';
};
</script>

<style lang="scss" scoped>
.matrix-table {
	:deep(.vxe-header--column) {
		font-weight: 600;
	}

	/* Ô giờ hẹp nên bỏ padding ngang để số 3 chữ số không bị cắt */
	:deep(.vxe-body--column) {
		padding-left: 0;
		padding-right: 0;
	}
}

.matrix-summary {
	font-weight: 600;
}
</style>
