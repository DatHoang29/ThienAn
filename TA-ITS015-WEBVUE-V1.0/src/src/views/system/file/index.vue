<template>
	<div class="sys-file-container">
		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item label="File Name" prop="fileName">
							<el-input v-model="state.queryParams.fileName" placeholder="File Name" clearable @keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item label="Start Time" prop="name">
							<el-date-picker v-model="state.queryParams.startTime" type="datetime" placeholder="Start Time" format="DD/MM/YYYY HH:mm:ss" value-format="YYYY-MM-DD HH:mm:ss" class="w100" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item label="End Time" prop="code">
							<el-date-picker v-model="state.queryParams.endTime" type="datetime" placeholder="End Time" format="DD/MM/YYYY HH:mm:ss" value-format="YYYY-MM-DD HH:mm:ss" class="w100" />
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>

			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />

			<el-row>
				<el-col>
					<el-button-group>
						<el-button type="primary" icon="ele-Search" @click="handleQuery" v-auth="'sysFile:page'" :loading="options.loading"> Query </el-button>
						<el-button icon="ele-Refresh" @click="resetQuery" :loading="options.loading"> Reset </el-button>
					</el-button-group>
				</el-col>
			</el-row>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
				<template #toolbar_buttons>
				</template>
				<template #toolbar_tools> 
					<TableHeader @add="showUpload" :show-delete="false" :auth-add="'sysFile:add'"
						:auth-delete="'sysFile:delete'" @refresh="handleQuery" @custom-column="handleCustom"
						@delete="">
						<template #prefix>
							<el-button type="primary" icon="ele-Plus" @click="showUpload" v-auth="'sysFile:uploadFile'"> Upload </el-button>
						</template>
					</TableHeader>
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_suffix="{ row }">
					<el-tag round>{{ row.suffix }}</el-tag>
				</template>
				<template #row_url="{ row }">
					<el-image
						style="width: 60px; height: 60px"
						:src="fetchFileUrl(row)"
						alt="Cannot Preview"
						lazy
						hide-on-click-modal
						:preview-src-list="[fetchFileUrl(row)]"
						:initial-index="0"
						fit="scale-down"
						preview-teleported
					></el-image>
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip content="Edit" placement="top">
						<el-button icon="ele-Edit" size="small" text type="primary" @click="handleEdit(row)" />
					</el-tooltip>
					<el-tooltip content="Delete" placement="top">
						<el-button icon="ele-Delete" size="small" text type="danger" @click="handleDelete(row)"  />
					</el-tooltip>
					<el-tooltip content="Preview" placement="top">
						<el-button icon="ele-View" size="small" text type="primary" @click="showPreviewDialog(row)"  />
					</el-tooltip>
					<el-tooltip content="Download" placement="top">
						<el-button icon="ele-Download" size="small" text type="primary" @click="handleDownload(row)"  />
					</el-tooltip>
				</template>
			</vxe-grid>
		</el-card>

		<el-dialog v-model="state.visible" :lock-scroll="false" draggable overflow destroy-on-close width="400px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-UploadFilled /> </el-icon>
					<span> Upload File </span>
				</div>
			</template>
			<div>
				<el-select v-model="state.fileType" placeholder="Please select file type" style="margin-bottom: 10px">
					<el-option label="Related Files" value="Related Files" />
					<el-option label="Archived Files" value="Archived Files" />
				</el-select>

				<el-upload ref="uploadRef" drag :auto-upload="false" :limit="1" :file-list="state.fileList" action="" :on-change="handleChange" accept=".jpg,.png,.bmp,.gif,.txt,.pdf,.xlsx,.docx">
					<el-icon class="el-icon--upload">
						<ele-UploadFilled />
					</el-icon>
					<div class="el-upload__text">Drag files here, or<em> click to upload</em></div>
					<template #tip>
						<div class="el-upload__tip">Please upload files not exceeding 10MB</div>
					</template>
				</el-upload>
			</div>
			<template #footer>
				<span class="dialog-footer">
					<el-button @click="state.visible = false">{{$t('message.common.cancel')}}</el-button>
					<el-button type="primary" @click="handleUpload">{{$t('lz.button.base.confirm')}}</el-button>
				</span>
			</template>
		</el-dialog>

		<el-drawer :title="state.fileName" v-model="state.docxVisible" size="70%" destroy-on-close>
			<vue-office-docx :src="state.docxUrl" style="height: calc(100vh - 37px)" @rendered="handleRendered" @error="handleError" />
		</el-drawer>
		<el-drawer :title="state.fileName" v-model="state.xlsxVisible" size="70%" destroy-on-close>
			<vue-office-excel :src="state.excelUrl" style="height: calc(100vh - 37px)" @rendered="handleRendered" @error="handleError" />
		</el-drawer>
		<el-drawer :title="state.fileName" v-model="state.pdfVisible" size="70%" destroy-on-close>
			<vue-office-pdf :src="state.pdfUrl" style="height: calc(100vh - 37px)" @rendered="handleRendered" @error="handleError" />
		</el-drawer>
		<el-image-viewer v-if="state.showViewer" :url-list="state.previewList" :hideOnClickModal="true" @close="state.showViewer = false"></el-image-viewer>
		<EditFile ref="editRef" title="Edit File" @handleQuery="handleQuery" />
	</div>
</template>

<script lang="ts" setup name="sysFile">
import { onMounted, reactive, ref,defineAsyncComponent } from 'vue';
import { ElMessageBox, ElMessage, UploadInstance } from 'element-plus';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { Local } from '/@/utils/storage';
import { downloadByUrl } from '/@/utils/download';
import VueOfficeDocx from '@vue-office/docx';
import VueOfficeExcel from '@vue-office/excel';
import VueOfficePdf from '@vue-office/pdf';
import '@vue-office/docx/lib/index.css';
import '@vue-office/excel/lib/index.css';
import {useI18n} from 'vue-i18n';
import EditFile from '/@/views/system/file/component/editFile.vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysFileApi } from '/@/api-services/system';
import { SysFile, PageFileInput } from '/@/api-services/system';
const {t:$t} = useI18n();
const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));

const xGrid = ref<VxeGridInstance>();
const uploadRef = ref<UploadInstance>();
const editRef = ref<InstanceType<typeof EditFile>>();
const state = reactive({
	queryParams: {
		fileName: undefined,
		startTime: undefined,
		endTime: undefined,
	},
	localPageParam: {
		pageSize: 50 as number,
		defaultSort: { field: 'orderNo', order: 'asc', descStr: 'desc' },
	},
	visible: false,
	diaglogEditFile: false,
	fileList: [] as any,
	docxVisible: false,
	xlsxVisible: false,
	pdfVisible: false,
	showViewer: false,
	docxUrl: '',
	excelUrl: '',
	pdfUrl: '',
	fileName: '',
	fileType: '',
	previewList: [] as string[],
	
});

// Local storage parameters
const localPageParamKey = 'localPageParam:sysFile';
// Table parameter configuration
const options = useVxeTable<SysFile>(
	{
		id: 'sysFile',
		name: 'File Information',
		columns: [
			// { type: 'checkbox', width: 40, fixed: 'left' },
			{ type: 'seq', title: 'No', width: 60, fixed: 'left' },
			{ field: 'fileName', title: 'Name', minWidth: 200, showOverflow: 'tooltip' },
			{ field: 'suffix', title: 'Suffix', minWidth: 80, showOverflow: 'tooltip', slots: { default: 'row_suffix' } },
			{ field: 'sizeKb', title: 'Size (KB)', minWidth: 100, showOverflow: 'tooltip' },
			{ field: 'url', title: 'Preview', minWidth: 100, slots: { default: 'row_url' } },
			{ field: 'bucketName', title: 'Storage Location', minWidth: 180, showOverflow: 'tooltip' },
			{ field: 'id', title: 'Storage Identifier', minWidth: 120, showOverflow: 'tooltip' },
			{ field: 'fileType', title: 'File Type', minWidth: 140, showOverflow: 'tooltip' },
			{ field: 'relationName', title: 'Related Object Name', minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'relationId', title: 'Related Object ID', minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'belongId', title: 'Belonging ID', minWidth: 120, showOverflow: 'tooltip' },
			// { field: 'userName', title: 'Uploader', minWidth: 150, showOverflow: 'tooltip', sortable: true },
			{ field: 'createTime', title: 'Creation Time', minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'remark', title: 'Remark', minWidth: 200, showOverflow: 'tooltip' },
			{ title: 'Actions', fixed: 'right', width: 150, showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	// vxeGrid configuration parameters (this can override any parameters), refer to vxe-table official documentation
	{
		// Proxy configuration
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		// Sorting configuration
		sortConfig: { defaultSort: Local.get(localPageParamKey)?.defaultSort || state.localPageParam.defaultSort },
		// Pagination configuration
		pagerConfig: { pageSize: Local.get(localPageParamKey)?.pageSize || state.localPageParam.pageSize },
		// Toolbar configuration
		toolbarConfig: { export: false },
		// Row settings
		rowConfig: { height: 80 },
	}
);

// Page initialization
onMounted(() => {});

// Query API
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params = Object.assign(state.queryParams, { page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order, descStr: 'desc' }) as PageFileInput;
	return getAPI(SysFileApi).apiSystemSysfilePagePost(params);
};

// Query operation
const handleQuery = async () => {
	options.loading = true;
	await xGrid.value?.commitProxy('reload') ;
	options.loading = false;
};

// Reset operation
const resetQuery = async () => {
	state.queryParams.fileName = undefined;
	state.queryParams.startTime = undefined;
	state.queryParams.endTime = undefined;
	await xGrid.value?.commitProxy('reload');
};

// Open upload page
const showUpload = () => {
	state.fileList = [];
	state.visible = true;
};

// Get file list through onChange method
const handleChange = (file: any, fileList: []) => {
	state.fileList = fileList;
};
// Upload
const handleUpload = async () => {
	if (state.fileList.length < 1) return;
	await getAPI(SysFileApi).apiSystemSysfileUploadfilePostForm(state.fileList[0].raw, state.fileType, undefined);
	handleQuery();
	ElMessage.success('Upload successful');
	state.visible = false;
};

// Download
const handleDownload = async (row: any) => {
	// var res = await getAPI(SysFileApi).sysFileDownloadPost({ id: row.id });
	var fileUrl = fetchFileUrl(row);
	downloadByUrl({ url: fileUrl });
};

// Delete
const handleDelete = (row: any) => {
	ElMessageBox.confirm($t('message.common.confirmDelete', { name:row.fileName}), $t('message.common.warn'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('message.common.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysFileApi).apiSystemSysfileDeletePost({ id: row.id });
			handleQuery();
			ElMessage.success($t('message.common.deleteSuccess'));
		})
		.catch(() => {});
};

// Custom column
function handleCustom() {
	xGrid.value?.openCustom();
}
// Table events
const gridEvents: VxeGridListeners<SysFile> = {
	// This event is only effective when pager-config is configured, it will be triggered when the pagination changes
	async pageChange({ pageSize }) {
		state.localPageParam.pageSize = pageSize;
		Local.set(localPageParamKey, state.localPageParam);
	},
	// This event will be triggered when the sorting conditions change
	async sortChange({ field, order }) {
		state.localPageParam.defaultSort = { field: field, order: order!, descStr: 'desc' };
		Local.set(localPageParamKey, state.localPageParam);
	},
};

// Open PDF preview page
const showPreviewDialog = async (row: any) => {
	if (row.suffix == '.pdf') {
		state.fileName = `【${row.fileName}${row.suffix}】`;
		state.pdfUrl = fetchFileUrl(row);
		state.pdfVisible = true;
	} else if (row.suffix == '.docx') {
		state.fileName = `【${row.fileName}${row.suffix}】`;
		state.docxUrl = fetchFileUrl(row);
		state.docxVisible = true;
	} else if (row.suffix == '.xlsx') {
		state.fileName = `【${row.fileName}${row.suffix}】`;
		state.excelUrl = fetchFileUrl(row);
		state.xlsxVisible = true;
	} else if (['.jpg', '.png', '.jpeg', '.bmp'].findIndex((e) => e == row.suffix) > -1) {
		state.previewList = [fetchFileUrl(row)];
		state.showViewer = true;
	} else {
		ElMessage.error('This file format is not supported for preview');
	}
};

// Get file URL
const fetchFileUrl = (row: SysFile): string => {
	if (row.bucketName == 'Local') {
		return `/${row.filePath}/${row.id}${row.suffix}`;
	} else {
		return row.url!;
	}
};

// Open edit page
const handleEdit = (row: any) => {
	editRef.value?.openDialog(row);
};

// File rendering completed
const handleRendered = () => {};

// File rendering failed
const handleError = () => {};

</script>