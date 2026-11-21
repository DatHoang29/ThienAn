<template>
	<!-- Dialog thêm mới / chỉnh sửa / tạo bản sao template (gồm trình chỉnh sửa bố cục GridStack) -->
	<el-dialog v-model="visible" width="850px" draggable :close-on-click-modal="false"
		@close="handleDialogClose" @opened="handleDialogOpened">
		<template #header>
			<div style="color: #fff">
				<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle">
					<ele-Edit />
				</el-icon>
				<span> {{ title }} </span>
			</div>
		</template>
		<el-form ref="formRef" :model="formData" :rules="formRules" label-width="120px" label-position="left">
			<!-- Tên template -->
			<el-form-item :label="$t('lz.label.cctvTemplate.templateName')" prop="name">
				<el-input v-model="formData.name" :placeholder="$t('lz.label.cctvTemplate.templateNamePlaceholder')"
					clearable />
			</el-form-item>
			<!-- Ghi chú -->
			<el-form-item :label="$t('lz.entity.base.remark')" prop="remark">
				<el-input v-model="formData.remark"
					:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.remark').toLowerCase() })"
					clearable />
			</el-form-item>
			<!-- Status -->
			<el-form-item :label="$t('lz.entity.base.status')" prop="status">
				<el-select v-model="formData.status" :placeholder="$t('lz.label.base.pleaseSelectStatus')" class="w100">
					<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code"
						:label="item.name" :value="parseInt(item.code)" />
				</el-select>
			</el-form-item>
			<!-- Copy từ template khác -->
			<el-form-item :label="$t('lz.label.cctvTemplate.copyFrom')">
				<el-select v-model="copyFromId" :placeholder="$t('lz.label.cctvTemplate.copyLayoutPlaceholder')"
					clearable class="w100" @change="handleCopyFromTemplate">
					<el-option v-for="tpl in templateList" :key="tpl.id" :label="tpl.name" :value="tpl.id"
						:disabled="tpl.id === formData.id" />
				</el-select>
			</el-form-item>
			<!-- Toolbar grid editor -->
			<el-form-item :label="$t('lz.label.cctvTemplate.gridLayout')">
				<div class="template-grid-editor">
					<div class="template-grid-editor__toolbar">
						<!-- Preset nhanh -->
						<el-button v-for="preset in GRID_PRESETS" :key="preset.count" size="small"
							:type="formData.items?.length === preset.count ? 'primary' : 'default'"
							@click="applyPreset(preset.count)">
							{{ $t('lz.label.cctvTemplate.cameraCount', { count: preset.count }) }}
						</el-button>
						<el-divider direction="vertical" />
						<!-- Thêm slot / Xóa tất cả -->
						<el-button size="small" type="success" icon="ele-Plus" @click="handleAddSlot">
							{{ $t('lz.button.cctvTemplate.addItem') }}
						</el-button>
						<el-button size="small" type="danger" icon="ele-Delete" @click="handleClearAll"
							:disabled="!formData.items?.length">
							{{ $t('lz.button.cctvTemplate.clearAll') }}
						</el-button>
					</div>
					<!-- GridStack container -->
					<div class="template-grid-editor__stage">
						<div ref="editorGridRef" class="grid-stack template-grid-stack"></div>
					</div>
					<div class="template-grid-editor__info">
						{{ $t('lz.label.cctvTemplate.editorHelp', { count: formData.items?.length || 0 }) }}
					</div>
				</div>
			</el-form-item>
		</el-form>
		<template #footer>
			<el-button @click="visible = false">{{ $t('lz.button.base.cancel') }}</el-button>
			<el-button type="primary" :loading="submitting" @click="handleSubmit">{{ $t('lz.button.base.save')
			}}</el-button>
		</template>
	</el-dialog>
</template>

<script lang="ts" setup name="cctvTemplateEditorDialog">
import { onBeforeUnmount, reactive, ref, nextTick } from 'vue';
import { ElMessage } from 'element-plus';
import type { FormInstance, FormRules } from 'element-plus';
import { GridStack } from 'gridstack';
import 'gridstack/dist/gridstack.min.css';
import { getAPI } from '/@/utils/axios-utils';
import { TemplateApi } from '/@/api-services/cctv';
import { TemplateCctvOutput as TemplateOutput, TemplateSlot, StatusEnum } from '/@/api-services/cctv/models';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import { useI18n } from 'vue-i18n';
import {
	findFreePosition,
	generateEqualGrid as buildEqualGrid,
	resolveSquareSize,
	renumberByReadingOrder,
	type LayoutRect,
} from '/@/views/cctv/templates/utils/templateLayout';
import { dashboardGridConfig } from '/@/views/cctv/dashboard/types';

const props = defineProps<{
	/** Số cột lưới (lấy từ cấu hình DB ở trang cha) */
	gridColumns: number;
	/** Số hàng lưới (lấy từ cấu hình DB ở trang cha) */
	gridRows: number;
}>();

const emit = defineEmits<{
	/** Phát sau khi lưu thành công để trang cha tải lại và ưu tiên bản vừa tạo */
	(e: 'saved', payload: { isNew: boolean; templateId?: string }): void;
}>();

const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const { t: $t } = useI18n();

// --- Constants ---
const GRID_CELL_HEIGHT = 70;
/** Margin lấy từ cấu hình server (dashboardGridConfig), đồng bộ với lưới dashboard */
const getGridMargin = () => dashboardGridConfig.margin;

/** Preset bố cục nhanh */
const GRID_PRESETS = [
	{ count: 1 },
	{ count: 4 },
	{ count: 6 },
	{ count: 9 },
	{ count: 16 },
];

/** Tham số mở dialog */
interface OpenPayload {
	mode: 'add' | 'edit' | 'duplicate';
	row?: TemplateOutput;
	templateList: TemplateOutput[];
}

// --- Refs / State ---
const formRef = ref<FormInstance>();
const editorGridRef = ref<HTMLElement>();
let editorGrid: GridStack | null = null;
// Bộ đếm tạo uid duy nhất cho mỗi widget (tách biệt với slotNo hiển thị)
let widgetUidSeed = 0;
// Cờ chặn xử lý sự kiện trong lúc đang cập nhật grid bằng code (tránh vòng lặp)
let isProgrammaticUpdate = false;
// Handler click (delegation) để xóa item con - lưu lại để gỡ bỏ khi destroy
let gridClickHandler: ((event: MouseEvent) => void) | null = null;

const visible = ref(false);
const title = ref('');
const isAdd = ref(true);
const submitting = ref(false);
const copyFromId = ref('');
const templateList = ref<TemplateOutput[]>([]);
const formData = reactive({
	id: '' as string,
	name: '' as string,
	remark: '' as string,
	status: StatusEnum.NUMBER_1 as number,
	items: [] as TemplateSlot[],
});

/** Form validation rules */
const formRules = reactive<FormRules>({
	name: [{ required: true, message: $t('lz.message.cctvTemplate.templateNameRequired'), trigger: 'blur' }],
	status: [{ required: true, message: $t('lz.message.cctvTemplate.statusRequired'), trigger: 'change' }],
});

// --- Helpers ---

/** Lấy message lỗi từ response */
const getErrorMessage = (e: any) => {
	return e?.response?.data?.message || e?.message || $t('lz.message.cctvTemplate.unknownError');
};

/** Kiểm tra API response thành công */
const assertApiSucceeded = (res: any, fallbackMessage: string) => {
	const data = res?.data;
	if (data?.succeeded === false || (data?.code !== undefined && data.code !== 200)) {
		throw new Error(data?.message || fallbackMessage);
	}
};

/** Tạo bố cục grid đều cho N camera (dùng logic thuần đã tách module) */
const generateEqualGrid = (count: number): TemplateSlot[] => buildEqualGrid(count, props.gridColumns, props.gridRows);

/** Chuẩn hóa items từ một template nguồn */
const mapSourceItems = (items: TemplateSlot[] | null | undefined): TemplateSlot[] =>
	(items ?? []).map((item, index) => ({
		slotNo: item.slotNo ?? (index + 1),
		x: item.x ?? 0,
		y: item.y ?? 0,
		w: item.w ?? 1,
		h: item.h ?? 1,
	}));

// --- GridStack Editor ---

/** Lấy tất cả nodes hiện tại trong grid */
const getEditorGridNodes = (): any[] => {
	if (!editorGrid) return [];
	return (editorGrid as any).engine?.nodes || [];
};

/** Tạo HTML content cho 1 widget (gồm số slot + nút xóa) */
const buildWidgetContent = (uid: string, slotNo: number) => {
	return `<div class="grid-slot-content">
		<span class="grid-slot-no">${slotNo}</span>
		<button type="button" class="grid-slot-del" data-uid="${uid}" title="${$t('lz.button.cctvTemplate.deleteItem')}">&times;</button>
	</div>`;
};

/**
 * Đánh số lại slot theo thứ tự đọc (trên→dưới, trái→phải) và đồng bộ về formData.
 * Cập nhật luôn label hiển thị trong DOM để số slot luôn liền mạch 1..N.
 */
const renumberAndSync = () => {
	if (!editorGrid) return;
	const nodes = getEditorGridNodes() as any[];
	// Dùng logic thuần để đánh số theo thứ tự đọc
	const ordered = renumberByReadingOrder(
		nodes.map((node) => ({ x: node.x ?? 0, y: node.y ?? 0, w: node.w ?? 1, h: node.h ?? 1 }))
	);
	// Cập nhật label số slot trong DOM theo đúng thứ tự (y,x)
	const sortedNodes = [...nodes].sort((a, b) => (a.y - b.y) || (a.x - b.x));
	sortedNodes.forEach((node, index) => {
		if (node.el) {
			const label = node.el.querySelector('.grid-slot-no');
			if (label) label.textContent = String(index + 1);
		}
	});
	formData.items = ordered;
};

/**
 * Enforce resize vuông (w === h) sau khi người dùng resize.
 * Có kiểm tra collision: nếu phóng to gây đè/tràn thì thu nhỏ lại để hợp lệ.
 */
const enforceSquareResize = () => {
	if (!editorGrid) return;
	isProgrammaticUpdate = true;
	editorGrid.batchUpdate();
	const nodes = getEditorGridNodes();
	nodes.forEach((node: any) => {
		if (!node.el) return;
		const w = node.w ?? 1;
		const h = node.h ?? 1;
		if (w === h) return;
		// Các item khác (để kiểm tra collision)
		const others: LayoutRect[] = nodes
			.filter((other: any) => other !== node)
			.map((other: any) => ({ x: other.x ?? 0, y: other.y ?? 0, w: other.w ?? 1, h: other.h ?? 1 }));
		// Dùng logic thuần collision-aware để tính cạnh vuông hợp lệ
		const finalSize = resolveSquareSize(
			{ x: node.x ?? 0, y: node.y ?? 0, w, h },
			others,
			props.gridColumns,
			props.gridRows
		);
		editorGrid!.update(node.el, { w: finalSize, h: finalSize });
	});
	editorGrid.batchUpdate(false);
	isProgrammaticUpdate = false;
	renumberAndSync();
};

/** Xử lý click delegation - bắt sự kiện click nút xóa trên widget con */
const handleGridClick = (event: MouseEvent) => {
	const target = event.target as HTMLElement;
	const delBtn = target.closest('.grid-slot-del') as HTMLElement | null;
	if (!delBtn) return;
	event.preventDefault();
	event.stopPropagation();
	const uid = delBtn.dataset.uid;
	if (uid) removeWidgetByUid(uid);
};

/** Khởi tạo GridStack editor */
const initEditorGrid = () => {
	destroyEditorGrid();
	if (!editorGridRef.value) return;
	editorGridRef.value.innerHTML = '';
	// GridStack mặc định render content bằng textContent (chống XSS) khiến HTML
	// (số slot + nút xóa) bị hiển thị thành text. Ghi đè renderCB để render bằng
	// innerHTML. Content do code tự sinh (số + uid), không chứa input người dùng.
	GridStack.renderCB = (el: HTMLElement, widget: any) => {
		el.innerHTML = widget.content || '';
	};
	editorGrid = GridStack.init(
		{
			column: props.gridColumns,
			// [TEST] Bỏ minRow (không ép chiều cao tối thiểu) để gần hành vi GridStack mặc định;
			// GIỮ maxRow = số hàng cấu hình để chặn lưới cao vô hạn (maxsize dọc = cấu hình).
			maxRow: props.gridRows,
			cellHeight: GRID_CELL_HEIGHT,
			margin: getGridMargin(),
			float: dashboardGridConfig.float,
			animate: dashboardGridConfig.animate,
			resizable: { handles: 'se' },
			// Không cho nút xóa kích hoạt thao tác kéo
			draggable: { cancel: '.grid-slot-del' },
		},
		editorGridRef.value
	);
	// Load items hiện tại vào grid
	loadItemsToGrid(formData.items);
	// Sự kiện: resize xong -> enforce vuông
	editorGrid.on('resizestop', () => {
		if (isProgrammaticUpdate) return;
		enforceSquareResize();
	});
	// Sự kiện: thay đổi vị trí/kích thước -> đồng bộ
	editorGrid.on('change', () => {
		if (isProgrammaticUpdate) return;
		renumberAndSync();
	});
	// Đăng ký delegation cho nút xóa item con
	gridClickHandler = handleGridClick;
	editorGridRef.value.addEventListener('click', gridClickHandler);
};

/** Hủy GridStack editor và gỡ listener */
const destroyEditorGrid = () => {
	if (gridClickHandler && editorGridRef.value) {
		editorGridRef.value.removeEventListener('click', gridClickHandler);
		gridClickHandler = null;
	}
	if (editorGrid) {
		editorGrid.destroy(false);
		editorGrid = null;
	}
};

/** Load danh sách items vào GridStack (xóa hết và dựng lại) */
const loadItemsToGrid = (items: TemplateSlot[]) => {
	if (!editorGrid) return;
	isProgrammaticUpdate = true;
	// removeDOM = true để xóa hẳn DOM cũ, tránh widget cũ còn sót chồng lên
	editorGrid.removeAll(true);
	// Sắp xếp theo thứ tự đọc để đánh số nhất quán
	const sorted = [...items].sort((a, b) => ((a.y ?? 0) - (b.y ?? 0)) || ((a.x ?? 0) - (b.x ?? 0)));
	editorGrid.batchUpdate();
	sorted.forEach((item, index) => {
		const uid = `slot-${++widgetUidSeed}`;
		editorGrid!.addWidget({
			id: uid,
			x: item.x ?? 0,
			y: item.y ?? 0,
			w: item.w ?? 1,
			h: item.h ?? 1,
			minW: 1,
			minH: 1,
			maxW: props.gridColumns,
			maxH: props.gridRows,
			content: buildWidgetContent(uid, index + 1),
		});
	});
	editorGrid.batchUpdate(false);
	isProgrammaticUpdate = false;
	renumberAndSync();
};

/** Áp dụng preset số camera - dựng lại grid theo bố cục đều */
const applyPreset = (count: number) => {
	formData.items = generateEqualGrid(count);
	loadItemsToGrid(formData.items);
};

/** Thêm 1 item (slot 1x1) vào vị trí trống đầu tiên */
const handleAddSlot = () => {
	if (!editorGrid) return;
	const currentItems = formData.items || [];
	// Map sang LayoutRect (x/y/w/h bắt buộc) cho hàm tìm vị trí trống
	const rects: LayoutRect[] = currentItems.map((item) => ({
		x: item.x ?? 0,
		y: item.y ?? 0,
		w: item.w ?? 1,
		h: item.h ?? 1,
	}));
	const pos = findFreePosition(rects, 1, 1, props.gridColumns, props.gridRows);
	if (!pos) {
		ElMessage.warning($t('lz.message.cctvTemplate.gridFull'));
		return;
	}
	const uid = `slot-${++widgetUidSeed}`;
	editorGrid.addWidget({
		id: uid,
		x: pos.x,
		y: pos.y,
		w: 1,
		h: 1,
		minW: 1,
		minH: 1,
		maxW: props.gridColumns,
		maxH: props.gridRows,
		content: buildWidgetContent(uid, currentItems.length + 1),
	});
	renumberAndSync();
};

/** Xóa 1 item theo uid */
const removeWidgetByUid = (uid: string) => {
	if (!editorGrid) return;
	const allItems = editorGrid.getGridItems();
	const targetEl = allItems.find((el) => (el as any).gridstackNode?.id === uid);
	if (targetEl) {
		// removeDOM = true để xóa hẳn phần tử khỏi giao diện
		editorGrid.removeWidget(targetEl, true);
		renumberAndSync();
	}
};

/** Xóa tất cả item trong grid */
const handleClearAll = () => {
	if (!editorGrid) return;
	// removeDOM = true để xóa hẳn toàn bộ widget khỏi giao diện
	editorGrid.removeAll(true);
	renumberAndSync();
};

/** Copy bố cục từ template khác (chỉ copy phần items/layout) */
const handleCopyFromTemplate = (templateId: string) => {
	if (!templateId) return;
	const source = templateList.value.find((t) => t.id === templateId);
	if (!source || !source.items?.length) {
		ElMessage.warning($t('lz.message.cctvTemplate.sourceLayoutEmpty'));
		return;
	}
	formData.items = mapSourceItems(source.items);
	loadItemsToGrid(formData.items);
};

/** Xử lý khi dialog mở xong (DOM ready) -> init GridStack */
const handleDialogOpened = async () => {
	await nextTick();
	initEditorGrid();
};

/** Đóng dialog và reset form */
const handleDialogClose = () => {
	destroyEditorGrid();
	copyFromId.value = '';
	formRef.value?.resetFields();
};

/** Submit form thêm mới / chỉnh sửa */
const handleSubmit = async () => {
	if (!formRef.value) return;
	// Đồng bộ lại items từ GridStack trước khi submit
	renumberAndSync();
	if (!formData.items?.length) {
		ElMessage.warning($t('lz.message.cctvTemplate.itemRequired'));
		return;
	}
	await formRef.value.validate(async (valid) => {
		if (!valid) return;
		submitting.value = true;
		try {
			const isNewTemplate = isAdd.value;
			let savedTemplateId: string | undefined;
			if (isNewTemplate) {
				// Thêm mới
				const res = await getAPI(TemplateApi).apiCctvTemplateAddPost({
					name: formData.name,
					remark: formData.remark,
					status: formData.status as StatusEnum,
					items: formData.items,
				});
				assertApiSucceeded(res, $t('lz.message.cctvTemplate.createFailed'));
				savedTemplateId = res.data?.result?.id ?? undefined;
				ElMessage.success($t('lz.message.cctvTemplate.createSuccess'));
			} else {
				// Chỉnh sửa
				const res = await getAPI(TemplateApi).apiCctvTemplateUpdatePost({
					id: formData.id,
					name: formData.name,
					remark: formData.remark,
					status: formData.status as StatusEnum,
					items: formData.items,
				});
				assertApiSucceeded(res, $t('lz.message.cctvTemplate.updateFailed'));
				ElMessage.success($t('lz.message.cctvTemplate.updateSuccess'));
			}
			visible.value = false;
			emit('saved', { isNew: isNewTemplate, templateId: savedTemplateId });
		} catch (e: any) {
			ElMessage.error($t(
				isAdd.value
					? 'lz.message.cctvTemplate.createFailedWithReason'
					: 'lz.message.cctvTemplate.updateFailedWithReason',
				{ error: getErrorMessage(e) }
			));
		} finally {
			submitting.value = false;
		}
	});
};

/**
 * Mở dialog theo chế độ:
 * - add: form trống, bố cục mặc định 4 ô.
 * - edit: prefill từ template (giữ id để cập nhật).
 * - duplicate: prefill từ template nhưng id rỗng + tên "... - Bản sao" để lưu thành template mới.
 */
const open = (payload: OpenPayload) => {
	templateList.value = payload.templateList || [];
	copyFromId.value = '';
	if (payload.mode === 'edit') {
		isAdd.value = false;
		title.value = $t('lz.label.cctvTemplate.editTitle');
		Object.assign(formData, {
			id: payload.row?.id ?? '',
			name: payload.row?.name ?? '',
			remark: payload.row?.remark ?? '',
			status: (payload.row?.status as number) ?? StatusEnum.NUMBER_1,
			items: mapSourceItems(payload.row?.items),
		});
	} else if (payload.mode === 'duplicate') {
		isAdd.value = true;
		title.value = $t('lz.label.cctvTemplate.duplicateTitle');
		Object.assign(formData, {
			id: '',
			name: $t('lz.label.cctvTemplate.copyName', { name: payload.row?.name ?? '' }),
			remark: payload.row?.remark ?? '',
			status: (payload.row?.status as number) ?? StatusEnum.NUMBER_1,
			items: mapSourceItems(payload.row?.items),
		});
	} else {
		isAdd.value = true;
		title.value = $t('lz.label.cctvTemplate.addTitle');
		Object.assign(formData, {
			id: '',
			name: '',
			remark: '',
			status: StatusEnum.NUMBER_1 as number,
			items: generateEqualGrid(4),
		});
	}
	visible.value = true;
};

// Dọn dẹp GridStack khi component bị hủy
onBeforeUnmount(() => {
	destroyEditorGrid();
});

defineExpose({ open });
</script>

<style scoped lang="scss">
/* GridStack editor trong dialog */
.template-grid-editor {
	width: 100%;
}

.template-grid-editor__toolbar {
	display: flex;
	align-items: center;
	gap: 8px;
	flex-wrap: wrap;
	margin-bottom: 12px;
}

.template-grid-editor__stage {
	border: 1px solid #dbe4ff;
	border-radius: 8px;
	background: linear-gradient(180deg, #f8fafc 0%, #eef2ff 100%);
	padding: 8px;
	min-height: 440px;
}

.template-grid-editor__info {
	margin-top: 8px;
	font-size: 12px;
	color: #64748b;
}

/* GridStack widget styling */
:deep(.template-grid-stack) {
	min-height: 420px !important;
}

:deep(.template-grid-stack .grid-stack-item-content) {
	display: flex;
	align-items: center;
	justify-content: center;
	border: 1px solid rgba(37, 99, 235, 0.3);
	background: linear-gradient(135deg, rgba(59, 130, 246, 0.15), rgba(37, 99, 235, 0.08));
	border-radius: 6px;
	cursor: move;
}

:deep(.template-grid-stack .grid-slot-content) {
	position: relative;
	width: 100%;
	height: 100%;
	display: flex;
	align-items: center;
	justify-content: center;
}

:deep(.template-grid-stack .grid-slot-no) {
	font-size: 16px;
	font-weight: 700;
	color: #1d4ed8;
}

/* Nút xóa item con - ẩn mặc định, hiện khi hover */
:deep(.template-grid-stack .grid-slot-del) {
	position: absolute;
	top: 2px;
	right: 2px;
	width: 20px;
	height: 20px;
	padding: 0;
	border: none;
	border-radius: 50%;
	background: rgba(239, 68, 68, 0.9);
	color: #fff;
	font-size: 14px;
	line-height: 1;
	cursor: pointer;
	display: none;
	align-items: center;
	justify-content: center;
}

:deep(.template-grid-stack .grid-stack-item-content:hover .grid-slot-del) {
	display: flex;
}

:deep(.template-grid-stack .grid-slot-del:hover) {
	background: rgb(220, 38, 38);
}

:deep(.template-grid-stack .grid-stack-item > .ui-resizable-handle) {
	background: none;
}

.w100 {
	width: 100%;
}
</style>
