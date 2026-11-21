<!-- SonTH: report viewer component with export and print functionality 
using @embedpdf/vue-pdf-viewer, supporting dynamic export formats and viewer controls. -->
<template>
	<div class="report-viewer-container" v-loading="loading">
		<div class="export-container" v-if="pdfSource">
			<el-select v-model="selectedFormat" placeholder="Select format" class="export-select">
				<el-option v-for="item in formatOptions" :key="item.code" :label="item.name" :value="item.code"></el-option>
			</el-select>
			<el-button type="success" icon="ele-Download" v-auth="exportAuth" @click="handleExport" :loading="exporting">
				{{ $t('lz.button.report.exportReport') }}
			</el-button>
			<el-button type="" :disabled="!pdfSource" icon="ele-Printer" v-auth="printAuth" @click="handlePrint">
				{{ $t('lz.button.base.print') }}
			</el-button>
		</div>
		<div v-if="pdfSource" class="pdf-container">
			<PDFViewer
				ref="pdfViewerRef"
				@ready="handleReady"
				:config="{
					src: pdfSource,
					disabledCategories: ['annotation', 'redaction', 'document', 'pointer', 'panel-search', 'panel-comment','annotation','insert','form'],
					theme: { preference: 'light' },
					zoom: { defaultZoomLevel: ZoomMode.Automatic, minZoomLevel: 1.5, maxZoomLevel: 3 },
					export: { defaultFileName: exportFileNameWithExt },
				}"
				:style="{ width: '100%', height: '100%' }"
			/>
		</div>
		<el-empty v-if="!pdfSource" :image-size="100" style="flex: 1; overflow: auto;"></el-empty>
	</div>
</template>

<script lang="ts" setup name="ReportViewer">
import { computed, onMounted, ref, watch } from 'vue';
import { PDFViewer, ZoomMode, type PluginRegistry, type ZoomPlugin, type PrintPlugin } from '@embedpdf/vue-pdf-viewer';

// Props definition
const props = withDefaults(defineProps<{
	/** PDF source URL or blob URL */
	pdfSource: string | null;
	/** Loading state */
	loading?: boolean;
	/** Export format options array with {code, name} */
	formatOptions: Array<{ code: string; name: string }>;
	/** Default selected format */
	defaultFormat?: string;
	/** Export file name (without extension) */
	exportFileName?: string;
	/** Auth key for export button */
	exportAuth?: string;
	/** Auth key for print button */
	printAuth?: string;
}>(), {
	loading: false,
	defaultFormat: 'pdf',
	exportFileName: 'report',
	exportAuth: '',
	printAuth: '',
});

// Emits definition
const emit = defineEmits<{
	/** Emitted khi bấm Xuất file, với mọi định dạng kể cả pdf */
	(e: 'export', format: string): void;
	/** Emitted when PDF viewer is ready */
	(e: 'ready', registry: PluginRegistry): void;
}>();

// Refs
const pdfViewerRef = ref<InstanceType<typeof PDFViewer>>();
const registry = ref<PluginRegistry | null>(null);
const selectedFormat = ref(props.defaultFormat);
const exporting = ref(false);

// Ensure we always provide a filename with a .pdf extension for downloads
const exportFileNameWithExt = computed(() => {
	const name = (props.exportFileName || 'report').trim();
	return name.toLowerCase().endsWith('.pdf') ? name : `${name}.pdf`;
});

// Watch for default format changes
watch(() => props.defaultFormat, (newVal) => {
	selectedFormat.value = newVal;
});

// Handle PDF viewer ready event
const handleReady = (r: PluginRegistry) => {
	registry.value = r;
	emit('ready', r);
};

// Zoom functions
function fitWidth() {
	const zoomPlugin = registry.value?.getPlugin<ZoomPlugin>('zoom')?.provides();
	zoomPlugin?.requestZoom(ZoomMode.FitWidth);
}

// Fit page to viewer
function fitPage() {
	const zoomPlugin = registry.value?.getPlugin<ZoomPlugin>('zoom')?.provides();
	zoomPlugin?.requestZoom(ZoomMode.FitPage);
}
// Set specific zoom level
function setZoom(level: number | ZoomMode) {
	const zoomPlugin = registry.value?.getPlugin<ZoomPlugin>('zoom')?.provides();
	zoomPlugin?.requestZoom(level);
}

// Download PDF directly using viewer's export plugin while forcing our filename
async function downloadPdfDirect() {

  const anchor = document.createElement('a');
	anchor.href = props.pdfSource as any;
	anchor.download = exportFileNameWithExt.value;
	anchor.click();
	URL.revokeObjectURL(props.pdfSource as any);
}

// Print handler
function handlePrint() {
	const printPlugin = registry.value?.getPlugin<PrintPlugin>('print')?.provides();
	printPlugin?.print();
}

// Method to set exporting state (called by parent after export completes)
function setExporting(value: boolean) {
	exporting.value = value;
}

// Export handler
const handleExport = async () => {
	// Mọi định dạng, kể cả pdf, đều đẩy ra trang cha để kết xuất lại qua API Export.
	//
	// Trước đây pdf được tải trực tiếp từ blob đang xem (downloadPdfDirect) nên không phát sinh
	// request nào. Việc đó làm mất các nghiệp vụ gắn vào API Export — cụ thể là gửi báo cáo qua
	// email: chọn pdf rồi bấm Xuất file thì không có mail nào được gửi.
	//
	// downloadPdfDirect vẫn giữ và vẫn expose để trang cha gọi khi cần tải nhanh không qua server.
	exporting.value = true;
	emit('export', selectedFormat.value);
};

const setZoomTo150 = () => {
  const zoomPlugin = registry.value?.getPlugin<ZoomPlugin>('zoom')?.provides();
  //loop 5 times to increase zoom
  for (let i = 0; i < 5; i++) {
    zoomPlugin?.zoomIn();
  }
};

// onMounted(() => {
//   // Example: Set zoom to 150% when the component is mounted
//   setZoom(ZoomMode.FitWidth);
// });
// // Watch for default format changes
// watch(() => props.pdfSource, (newVal) => {
//   setZoomTo150();
// });

// Expose for parent component
defineExpose({
	getRegistry: () => registry.value,
	fitWidth,
	fitPage,
	setZoom,
	print: handlePrint,
	downloadPdf: downloadPdfDirect,
	setExporting,
  setZoomTo150
});
</script>

<style lang="scss" scoped>
.report-viewer-container {
	flex: 1;
	display: flex;
	flex-direction: column;
	overflow: hidden;
	position: relative;
}

.pdf-container {
	flex: 1;
	overflow: auto;
}

:deep(.vue-office-pdf) {
	width: 100%;
	height: 100%;
}

.export-container {
	display: flex;
	align-items: center;
	position: absolute;
	top: 8px;
	right: 5px;
	z-index: 2;
}

.export-select {
	width: 120px;
	margin-right: 10px;
}
</style>
