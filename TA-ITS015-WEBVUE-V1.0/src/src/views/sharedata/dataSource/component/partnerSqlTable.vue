<template>
	<div class="partner-sql-table">
		<div class="partner-sql-toolbar">
			<el-radio-group v-model="filter" size="small" class="partner-sql-filter">
				<el-radio-button v-for="item in filters" :key="item.value" :value="item.value">{{ item.label }} ({{ item.count }})</el-radio-button>
			</el-radio-group>
			<!-- Chọn nhiều đối tác có SQL riêng rồi đưa về dùng mẫu chung một lượt -->
			<el-button size="small" type="warning" plain icon="ele-RefreshLeft" :disabled="selected.length === 0" :loading="props.resetting" @click="emit('resetMany', selected)">
				{{ $t('lz.button.sharedataDataSource.resetManyToStandard') }} ({{ selected.length }})
			</el-button>
		</div>

		<div class="partner-sql-grid">
			<el-table
				ref="tableRef"
				v-loading="props.loading"
				:element-loading-text="$t('lz.label.base.loading')"
				:data="rows"
				size="small"
				border
				style="width: 100%"
				:empty-text="$t('lz.label.sharedataDataSource.noPartnerSql')"
				@selection-change="handleSelectionChange"
			>
				<!-- Chỉ đối tác đang có SQL riêng mới chọn được; ô chọn ở tiêu đề chọn tất cả các dòng chọn được đang hiện -->
				<el-table-column type="selection" width="46" align="center" :selectable="isSelectable" />
				<el-table-column :label="$t('lz.label.sharedataDataSource.partnerSqlColPartner')" min-width="260" header-align="center" show-overflow-tooltip>
					<template #default="{ row }">
						<!-- Tên và mã trên một dòng, không ngắt; quá dài thì cắt "…" và có tooltip -->
						<span class="partner-name">{{ row.partnerName || row.partnerCode }}</span>
						<span v-if="row.partnerName && row.partnerCode" class="partner-sub"> - {{ row.partnerCode }}</span>
					</template>
				</el-table-column>
				<el-table-column :label="$t('lz.label.sharedataDataSource.partnerSqlColStatus')" width="150" align="center">
					<template #default="{ row }">
						<el-tag size="small" effect="plain" :type="statusMeta[statusOf(row)].type">
							{{ $t(statusMeta[statusOf(row)].label) }}
						</el-tag>
					</template>
				</el-table-column>
				<el-table-column :label="$t('lz.label.sharedataDataSource.partnerSqlColUpdated')" min-width="240" align="center">
					<template #default="{ row }">
						<span v-if="row.updateTime" class="partner-time">{{ formatTime(row.updateTime) }}</span>
						<span v-else class="partner-empty">{{ $t('lz.label.sharedataDataSource.partnerSqlUpdatedEmpty') }}</span>
					</template>
				</el-table-column>
				<el-table-column :label="$t('lz.label.base.action')" width="120" align="center">
					<template #default="{ row }">
						<!-- Nút chỉ có biểu tượng theo quy ước các bảng khác; tên thao tác hiện ở tooltip -->
						<div class="partner-actions">
							<template v-if="row.isCustom">
								<el-tooltip :content="$t('lz.button.sharedataDataSource.openRow')" placement="top">
									<el-button icon="ele-Edit" text type="primary" :aria-label="$t('lz.button.sharedataDataSource.openRow')" @click="emit('open', row)" />
								</el-tooltip>
								<el-tooltip :content="$t('lz.button.sharedataDataSource.compareWithStandard')" placement="top">
									<el-button icon="ele-Switch" text type="primary" :aria-label="$t('lz.button.sharedataDataSource.compareWithStandard')" @click="emit('compare', row)" />
								</el-tooltip>
							</template>
							<el-tooltip v-else :content="$t('lz.button.sharedataDataSource.createPartnerSqlRow')" placement="top">
								<el-button icon="ele-DocumentAdd" text type="primary" :aria-label="$t('lz.button.sharedataDataSource.createPartnerSqlRow')" @click="emit('create', row)" />
							</el-tooltip>
						</div>
					</template>
				</el-table-column>
			</el-table>
		</div>
	</div>
</template>

<script lang="ts" setup name="sharedataPartnerSqlTable">
import { computed, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { dayjs } from 'element-plus';

const { t: $t } = useI18n();

interface PartnerSqlRow {
	partnerId: string;
	partnerCode?: string;
	partnerName: string;
	updateTime?: string | null;
	isCustom: boolean;
}
interface Props {
	items: PartnerSqlRow[];
	/** Đang đưa nhiều đối tác về mẫu chung: nút hàng loạt xoay và không bấm tiếp được. */
	resetting?: boolean;
	/** Đang tải danh sách đối tác: bảng hiện hiệu ứng v-loading. */
	loading?: boolean;
}
const props = defineProps<Props>();

interface Emits {
	(e: 'open', row: PartnerSqlRow): void;
	(e: 'create', row: PartnerSqlRow): void;
	(e: 'compare', row: PartnerSqlRow): void;
	/** Đưa các đối tác đã chọn về dùng mẫu chung (xóa SQL riêng của họ). */
	(e: 'resetMany', rows: PartnerSqlRow[]): void;
}
const emit = defineEmits<Emits>();

const tableRef = ref();
const selected = ref<PartnerSqlRow[]>([]);
const isSelectable = (row: PartnerSqlRow) => row.isCustom;
const handleSelectionChange = (rows: PartnerSqlRow[]) => {
	selected.value = rows;
};
// Danh sách tải lại (sau khi đưa về mẫu chung, đổi gói tin...) thì bỏ chọn cũ
watch(
	() => props.items,
	() => tableRef.value?.clearSelection()
);

type SqlStatus = 'custom' | 'shared';

/** Thẻ trạng thái theo quy ước chung: el-tag small, effect plain, màu theo type, chỉ có chữ. */
const statusMeta = {
	custom: { type: 'primary', label: 'lz.label.sharedataDataSource.tagCustomSql' },
	shared: { type: 'info', label: 'lz.label.sharedataDataSource.tagSharedSql' },
} as const;

const statusOf = (row: PartnerSqlRow): SqlStatus => (row.isCustom ? 'custom' : 'shared');

const filter = ref<'all' | SqlStatus>('all');

const filters = computed(() => [
	{ value: 'all', label: $t('lz.label.sharedataDataSource.filterAll'), count: props.items.length },
	{ value: 'custom', label: $t('lz.label.sharedataDataSource.tagCustomSql'), count: props.items.filter((p) => p.isCustom).length },
	{ value: 'shared', label: $t('lz.label.sharedataDataSource.tagSharedSql'), count: props.items.filter((p) => !p.isCustom).length },
]);

/** SQL riêng xếp trước để thấy ngay các đối tác đã tách khỏi mẫu chung; cùng nhóm thì theo tên. */
const rows = computed(() => {
	const list = props.items.filter((p) => filter.value === 'all' || statusOf(p) === filter.value);
	return list.slice().sort((a, b) => {
		if (a.isCustom !== b.isCustom) return a.isCustom ? -1 : 1;
		return (a.partnerName || '').localeCompare(b.partnerName || '', 'vi');
	});
});

const formatTime = (value?: string | null) => (value ? dayjs(value).format('DD/MM/YYYY HH:mm:ss') : '');
</script>

<style lang="scss" scoped>
.partner-sql-table {
	display: flex;
	flex-direction: column;
	flex: 1;
	min-height: 0;

	.partner-sql-toolbar {
		display: flex;
		align-items: center;
		justify-content: space-between;
		flex-wrap: wrap;
		gap: 8px;
		margin-bottom: 8px;
		flex-shrink: 0;
	}

	.partner-sql-grid {
		flex: 1;
		min-height: 0;
		overflow: auto;
	}

	.partner-name {
		font-weight: 600;
	}

	.partner-sub {
		font-size: 12px;
		color: var(--el-text-color-secondary);
	}

	.partner-time {
		font-variant-numeric: tabular-nums;
	}

	/* Ô chưa có dữ liệu: chữ mờ giải thích vì sao trống, tránh để ô trắng. */
	.partner-empty {
		font-size: 12px;
		color: var(--el-text-color-placeholder);
	}

	/* Căn giữa cụm nút; bỏ lề trái mặc định giữa hai el-button liền kề để cụm không bị lệch. */
	.partner-actions {
		display: flex;
		align-items: center;
		justify-content: center;

		.el-button + .el-button {
			margin-left: 0;
		}
	}

	/* Tiêu đề cùng màu bảng vxe "Danh sách trường": nền xanh đậm theo màu chủ đạo, chữ trắng; ô chọn ở tiêu đề viền trắng */
	:deep(.el-table__header-wrapper th.el-table__cell) {
		background-image: linear-gradient(180deg, color-mix(in srgb, var(--el-color-primary) 78%, #0f172a) 0%, var(--el-color-primary) 100%);
		color: #ffffff;
		font-weight: 600;
		border-right-color: rgba(255, 255, 255, 0.22);
	}

	:deep(.el-table__header-wrapper .el-checkbox__inner) {
		background-color: transparent;
		border-color: #ffffff;
	}

	:deep(.el-table__header-wrapper .el-checkbox__input.is-checked .el-checkbox__inner),
	:deep(.el-table__header-wrapper .el-checkbox__input.is-indeterminate .el-checkbox__inner) {
		background-color: #ffffff;
		border-color: #ffffff;
	}

	:deep(.el-table__header-wrapper .el-checkbox__input.is-checked .el-checkbox__inner::after) {
		border-color: var(--el-color-primary);
	}

	:deep(.el-table__header-wrapper .el-checkbox__input.is-indeterminate .el-checkbox__inner::before) {
		background-color: var(--el-color-primary);
	}
}
</style>
