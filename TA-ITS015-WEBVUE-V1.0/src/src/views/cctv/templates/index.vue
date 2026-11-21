<template>
	<div class="cctv-template-container">
		<!-- Search / Filter -->
		<el-card shadow="hover"
			:body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inline-message="true"
				label-width="auto" style="flex: 1 1 0%" label-position="left">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.status')" prop="status">
							<el-select v-model="state.queryParams.status" filterable clearable class="w100"
								@clear="state.queryParams.status = undefined"
								:placeholder="$t('lz.label.base.pleaseSelectStatus')">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)"
									:key="item.code" :label="item.name" :value="parseInt(item.code)" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.cctvTemplate.templateName')" prop="name">
							<el-input v-model="state.queryParams.name"
								:placeholder="$t('lz.label.cctvTemplate.templateNamePlaceholder')" clearable
								@keyup.enter="handleQuery" />
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>

			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />

			<el-row>
				<SearchButton :auth-search="'cctvTemplates:view'" @search="handleQuery" @reset="resetQuery"
					:loading="options.loading" />
			</el-row>
		</el-card>

		<!-- Table -->
		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents"
				@checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">

				<template #toolbar_tools>
					<TableHeader @add="handleAdd" :show-delete="state.showBatchDelete" :auth-add="'cctvTemplates:add'"
						:auth-delete="'cctvTemplates:delete'" @refresh="handleQuery" @custom-column="handleCustom"
						@delete="handleBatchDelete">
					</TableHeader>
				</template>

				<!-- Slot hiển thị preview grid -->
				<template #row_preview="{ row }">
					<div class="template-grid-preview" :style="getGridStyle()">
						<div v-for="item in (row.items || [])" :key="item.slotNo" class="template-grid-preview__item"
							:style="getItemStyle(item)">
							{{ item.slotNo }}
						</div>
					</div>
				</template>

				<template #row_status="{ row }">
					<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, row.status)" />
				</template>

				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>

				<template #empty>
					<el-empty :image-size="200" />
				</template>

				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
						<el-button icon="ele-Edit" text type="primary" @click="handleEdit(row)"
							v-auth="'cctvTemplates:update'"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.cctvTemplate.duplicate')" placement="top" :auto-close="1000">
						<el-button icon="ele-CopyDocument" text type="success" @click="handleDuplicate(row)"
							v-auth="'cctvTemplates:add'"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger" @click="handleDelete(row)"
							v-auth="'cctvTemplates:delete'"> </el-button>
					</el-tooltip>
				</template>
			</vxe-grid>
		</el-card>

		<!-- Dialog thêm mới / chỉnh sửa / tạo bản sao (component tách riêng) -->
		<TemplateEditorDialog ref="editorDialogRef" :grid-columns="gridColumns" :grid-rows="gridRows"
			@saved="handleTemplateSaved" />
	</div>
</template>

<script lang="ts" setup name="cctvTemplates">
import { onMounted, reactive, ref, defineAsyncComponent } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { VxeGridInstance, VxeGridListeners } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { getAPI } from '/@/utils/axios-utils';
import { TemplateApi } from '/@/api-services/cctv';
import { TemplateCctvOutput as TemplateOutput, TemplateSlot } from '/@/api-services/cctv/models';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import { useI18n } from 'vue-i18n';
import { GRID_COLUMNS, GRID_ROWS } from '/@/views/cctv/templates/utils/templateLayout';
import {
	getFullGridStyle,
	prioritizeTemplateById,
	sortTemplatesByCreateTimeDesc,
} from '/@/views/cctv/templates/utils/templatePresentation';
import { loadCctvGridDimensions } from '/@/views/cctv/dashboard/services/cctvDashboardConfig';
import TemplateEditorDialog from './component/TemplateEditorDialog.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';

const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const { t: $t } = useI18n();

// Số cột/hàng lưới lấy từ cấu hình DB (cùng nguồn với lưới dashboard), truyền xuống dialog editor.
// Khởi tạo bằng giá trị mặc định, ghi đè từ DB khi onMounted.
const gridColumns = ref(GRID_COLUMNS);
const gridRows = ref(GRID_ROWS);

// --- Refs ---
const xGrid = ref<VxeGridInstance>();
const editorDialogRef = ref<InstanceType<typeof TemplateEditorDialog>>();

// --- State ---
const state = reactive({
	queryParams: {
		status: undefined as unknown as any,
		name: undefined as unknown as string,
	},
	showBatchDelete: false,
});

// --- Helpers ---

/** Lấy message lỗi từ response */
const getErrorMessage = (e: any) => {
	return e?.response?.data?.message || e?.message || $t('lz.message.cctvTemplate.unknownError');
};

/** Kiểm tra action bị hủy */
const isCancelAction = (e: any) => {
	return e === 'cancel' || e === 'close';
};

/** Kiểm tra API response thành công */
const assertApiSucceeded = (res: any, fallbackMessage: string) => {
	const data = res?.data;
	if (data?.succeeded === false || (data?.code !== undefined && data.code !== 200)) {
		throw new Error(data?.message || fallbackMessage);
	}
};

/** Tính grid style theo toàn bộ lưới cấu hình, kể cả các ô trống */
const getGridStyle = () => getFullGridStyle(gridColumns.value, gridRows.value);

/** Tính style cho từng item trong preview table */
const getItemStyle = (item: TemplateSlot) => ({
	gridColumn: `${(item.x ?? 0) + 1} / span ${Math.max(item.w ?? 1, 1)}`,
	gridRow: `${(item.y ?? 0) + 1} / span ${Math.max(item.h ?? 1, 1)}`,
});

// --- Table Configuration ---
const options = useVxeTable<TemplateOutput>(
	{
		id: 'cctvTemplates',
		name: 'CCTV_Templates',
		columns: [
			{ type: 'checkbox', fixed: 'left', width: 45 },
			{ field: 'name', title: $t('lz.label.cctvTemplate.templateName'), minWidth: 180, showOverflow: 'tooltip', sortable: true },
			{ field: 'items', title: $t('lz.label.cctvTemplate.layout'), width: 120, slots: { default: 'row_preview' } },
			{ field: 'remark', title: $t('lz.entity.base.remark'), minWidth: 180, showOverflow: 'tooltip', sortable: true },
			{ field: 'status', title: $t('lz.entity.base.status'), width: 110, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_status' } },
			{ field: 'infoClm', title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ field: 'actionClm', title: $t('lz.label.base.action'), fixed: 'right', width: 150, showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	{
		proxyConfig: { enabled: false },
		pagerConfig: { enabled: false },
		toolbarConfig: { export: false },
		sortConfig: { remote: false, trigger: 'cell' },
		rowConfig: { height: 80 },
		data: [],
	}
);

// --- Lifecycle ---
onMounted(async () => {
	// Lấy số cột/hàng lưới từ cấu hình DB để editor template khớp với lưới dashboard
	const dimensions = await loadCctvGridDimensions();
	gridColumns.value = dimensions.columns;
	gridRows.value = dimensions.rows;
	await handleQuery();
});

// --- Actions ---

/** Truy vấn danh sách template */
const handleQuery = async () => {
	options.loading = true;
	try {
		const { name, status } = state.queryParams;
		const res = await getAPI(TemplateApi).apiCctvTemplateTemplatelistGet(
			name || undefined,
			status !== undefined && status !== null ? status : undefined,
		);
		assertApiSucceeded(res, $t('lz.message.cctvTemplate.loadListError'));
		options.data = sortTemplatesByCreateTimeDesc(res.data?.result ?? []);
	} catch (e: any) {
		ElMessage.error($t('lz.message.cctvTemplate.loadListErrorWithReason', { error: getErrorMessage(e) }));
		options.data = [];
	} finally {
		options.loading = false;
		selectChangeEvent();
	}
};

/** Tải lại sau khi lưu; template mới được giữ trên cùng kể cả khi timestamp trùng nhau. */
const handleTemplateSaved = async (payload?: { isNew: boolean; templateId?: string }) => {
	await handleQuery();
	if (!payload?.isNew || !payload.templateId) return;
	options.data = prioritizeTemplateById((options.data as TemplateOutput[]) ?? [], payload.templateId);
};

/** Reset bộ lọc */
const resetQuery = async () => {
	state.queryParams.status = undefined as unknown as any;
	state.queryParams.name = undefined as unknown as string;
	await handleQuery();
};

/** Danh sách template hiện có (dùng cho chức năng "Copy bố cục từ" trong dialog) */
const getTemplateList = () => (options.data as TemplateOutput[]) || [];

/** Mở dialog thêm mới */
const handleAdd = () => {
	editorDialogRef.value?.open({ mode: 'add', templateList: getTemplateList() });
};

/** Mở dialog chỉnh sửa */
const handleEdit = (row: TemplateOutput) => {
	editorDialogRef.value?.open({ mode: 'edit', row, templateList: getTemplateList() });
};

/** Mở dialog tạo bản sao */
const handleDuplicate = (row: TemplateOutput) => {
	editorDialogRef.value?.open({ mode: 'duplicate', row, templateList: getTemplateList() });
};

/** Xóa template */
const handleDelete = (row: TemplateOutput) => {
	if (!row.id) {
		ElMessage.warning($t('lz.message.cctvTemplate.idNotFound'));
		return;
	}
	ElMessageBox.confirm($t('lz.message.cctvTemplate.confirmDelete', { name: row.name }), $t('lz.label.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			const res = await getAPI(TemplateApi).apiCctvTemplateDeletePost({ id: row.id! });
			assertApiSucceeded(res, $t('lz.message.cctvTemplate.deleteFailed'));
			ElMessage.success($t('lz.message.cctvTemplate.deleteSuccess'));
			await handleQuery();
		})
		.catch((e) => {
			if (!isCancelAction(e)) ElMessage.error($t('lz.message.cctvTemplate.deleteFailedWithReason', { error: getErrorMessage(e) }));
		});
};

/** Xóa hàng loạt */
const handleBatchDelete = async () => {
	const selectRecords = getCheckedRecords();
	const invalidRecords = selectRecords.filter((record) => !record.id);
	if (invalidRecords.length > 0) {
		ElMessage.warning($t('lz.message.cctvTemplate.missingIdCount', { count: invalidRecords.length }));
		return;
	}
	ElMessageBox.confirm($t('lz.message.cctvTemplate.confirmBatchDelete', { count: selectRecords.length }), $t('lz.label.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			for (const record of selectRecords) {
				const res = await getAPI(TemplateApi).apiCctvTemplateDeletePost({ id: record.id! });
				assertApiSucceeded(res, $t('lz.message.cctvTemplate.deleteFailed'));
			}
			ElMessage.success($t('lz.message.base.deleteSuccess'));
			await handleQuery();
		})
		.catch((e) => {
			if (!isCancelAction(e)) ElMessage.error($t('lz.message.cctvTemplate.deleteFailedWithReason', { error: getErrorMessage(e) }));
		});
};

/** Mở cấu hình cột */
function handleCustom() {
	xGrid.value?.openCustom();
}

/** Lấy danh sách bản ghi đã chọn */
function getCheckedRecords(): TemplateOutput[] {
	return xGrid.value?.getCheckboxRecords() ?? [];
}

/** Cập nhật trạng thái hiển thị nút xóa hàng loạt */
function selectChangeEvent() {
	state.showBatchDelete = getCheckedRecords().length > 0;
}

// Table events
const gridEvents: VxeGridListeners<TemplateOutput> = {};
</script>

<style scoped lang="scss">
/* Preview grid nhỏ trong table */
.template-grid-preview {
	display: grid;
	gap: 2px;
	width: 60px;
	height: 60px;
	margin: 4px auto;
}

.template-grid-preview__item {
	display: flex;
	align-items: center;
	justify-content: center;
	min-width: 0;
	min-height: 0;
	border: 1px solid rgba(37, 99, 235, 0.18);
	background: linear-gradient(135deg, rgba(59, 130, 246, 0.18), rgba(37, 99, 235, 0.08));
	color: #1d4ed8;
	font-size: 9px;
	font-weight: 700;
	overflow: hidden;
}

</style>
