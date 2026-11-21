<template>
	<div class="image-container">
		<el-card shadow="hover"
			:body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true"	label-width="auto" style="flex: 1 1 0%" label-position="left">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="24" :md="6" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.code')" prop="code">
							<el-input v-model="state.queryParams.code" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.base.code')})" @keyup.enter.native="handleQuery" :maxlength="EntityFieldLength.Length64" show-word-limit clearable class="w100"/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="24" :md="6" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.name')" prop="name">
							<el-input v-model="state.queryParams.name" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.base.name')})" @keyup.enter.native="handleQuery" :maxlength="EntityFieldLength.Length64" show-word-limit clearable class="w100"/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="24" :md="6" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.type')" prop="type">
							<el-select v-model="state.queryParams.type" :placeholder="$t('lz.label.base.pleaseSelect', {input: $t('lz.entity.base.type')})" clearable class="w100">
								<el-option v-for="item in state.imageTypeList" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="24" :md="6" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.cctvEquipment.equipmentType')" prop="equipmentTypeId">
							<el-select v-model="state.queryParams.equipmentTypeId" :placeholder="$t('lz.label.base.pleaseSelect', {input: $t('lz.label.cctvEquipment.equipmentType')})" clearable class="w100">
								<el-option v-for="item in state.equipmentTypeList" :key="item.id" :label="item.name" :value="item.id" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="24" :md="6" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.lang')" prop="lang">
							<el-select v-model="state.queryParams.lang" :placeholder="$t('lz.label.base.pleaseSelect', {input: $t('lz.entity.base.lang')})" clearable class="w100">
								<el-option v-for="item in state.languageList" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.status')" prop="status" >
							<el-select v-model="state.queryParams.status" filterable clearable class="w100" @clear="state.queryParams.status = undefined" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.base.status')})" >
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code" :label="item.name" :value="parseInt(item.code)" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />
			<el-row>
				<SearchButton authSearch="vmsImage:list" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
			</el-row>
		</el-card>
		<!-- SearchBar: end -->

		<!-- GridTable: begin -->
		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGridVmsImage" class="xGrid-style" v-bind="options" v-on="gridEvents" @checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">
				<template #toolbar_tools>
					<TableHeader @add="handleAdd" :show-delete="state.showBatchDelete" :auth-add="'vmsImage:add'" :auth-delete="'vmsImage:delete'" @refresh="handleQuery" @custom-column="handleCustom" @delete="handleBatchDelete">
					</TableHeader>
				</template>
				<template #row_equipmentType="{ row }">
						{{ state.equipmentTypeMap[row.equipmentTypeId]?.name || '-' }}
				</template>

				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_code="{ row }">
					<el-tag class="text-h1 text-upper" size="large" type="primary">
						<b>{{ row.code }}</b>
					</el-tag>
				</template>
				<template #row_name="{ row }">
					<el-text class="text-h1 text-upper" type="danger"><b>{{ row.name }}</b></el-text>
				</template>
				<template #row_image="{ row }">
					<div class="panel-img">
						<!-- Click vào ảnh: chỉ phóng to xem đúng ảnh đó (el-image preview) -->
						<el-image
							v-if="row.url"
							:src="commonVarsAndFuncs.getFullUrl(row.url)"
							:preview-src-list="[commonVarsAndFuncs.getFullUrl(row.url)]"
							:preview-teleported="true"
							hide-on-click-modal
							fit="contain"
						/>
					</div>
				</template>
				<template #row_type="{ row }">
					{{ state.imageTypeList?.find((item: any) => item.code == row.type)?.name }}
				</template>
				<template #row_size="{ row }">
					{{ row.type == 'icon' ? state.icoSizeList?.find((item: any) => item.code == row.size)?.name : state.imgSizeList?.find((item: any) => item.code == row.size)?.name }}
				</template>
				<template #row_lang="{ row }">
					{{ state.languageList?.find((item: any) => item.code == row.lang)?.name }}
				</template>
				<template #row_status="{ row }">
					<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, row.status)" />
				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
						<el-button icon="ele-CopyDocument" text type="warning" v-auth="'vmsImage:add'" @click="handleCopy(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
						<el-button icon="ele-Edit" text type="primary" v-auth="'vmsImage:update'" @click="handleEdit(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger" v-auth="'vmsImage:delete'" @click="handleDelete(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.view')" placement="top">
						<el-button icon="ele-View" text type="primary" v-auth="'tmsVehicleRegistration:view'" @click="handleView(row)"> </el-button>
					</el-tooltip>
				</template>
				<template #header_name>
					{{ $t('lz.label.vms.vmsImage') }}
				</template>
			</vxe-grid>
		</el-card>
		<!-- GridTable: end -->

		<!-- Primary components: Begin -->
		<editImageComp  ref="editImageCompRef" :title="state.title" :operateType="state.operateType" @handleQuery="handleQuery" />
		<detailImageComp ref="detailImageCompRef" :title="state.title" :operateType="state.operateType" @handleQuery="handleQuery" />
		<!-- Primary components: End -->
		
	</div>
</template>

<script lang="ts" setup name="vmsImage">

	import { onMounted, reactive, ref, defineAsyncComponent } from 'vue';
	import { ElMessageBox, ElMessage } from 'element-plus';
	import { useI18n } from 'vue-i18n';
	import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
	import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
	import { getAPI } from '/@/utils/axios-utils';
	import { OperateType } from '/@/types/constants/operate';
	import ModifyRecord from '/@/components/table/modifyRecord.vue';

	import { VmsImageApi } from '/@/api-services/vms';
	import { PageVmsImageInput, PageVmsImageOutput } from '/@/api-services/vms';
	import { useItsMediaServerStore } from '/@/stores/its/itsMediaServer';
	import { TmsEquipmentTypeApi } from '/@/api-services/tms';

	import editImageComp from '/@/views/vms/image/component/editImage.vue';
	import detailImageComp from '/@/views/vms/image/component/detailImage.vue';

	import { useCommonVarsAndFuncs } from '/@/stores/its/commonVarsAndFuncs';
	const commonVarsAndFuncs = useCommonVarsAndFuncs();

	import { useBaseConfig } from '/@/stores/baseConfig';
	import { BaseConfigTypeEnum, TmsEquipmentType } from '/@/types/enums';

	const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
	const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
	const getConfigItemByCode = useBaseConfig().getConfigItemByCode;

	// Chỉ các loại thiết bị thuộc module VMS: VMS + LCS
	const ALLOWED_TYPE_CODES = [TmsEquipmentType.Vms, TmsEquipmentType.LCS];

	import { EntityFieldLength } from '/@/types/enums/baseEnum';

	const editImageCompRef = ref<InstanceType<typeof editImageComp>>();
	const detailImageCompRef = ref<InstanceType<typeof detailImageComp>>();

	const state = reactive({
		queryParams: {
			name: undefined,
			code: undefined,
			type: undefined,
			lang: undefined,
			equipmentTypeId: undefined,
			status: undefined
		},
		localPageParam: {
			pageSize: 50 as number,
			defaultSort: { field: 'orderNo', order: 'asc', descStr: 'desc' } as any,
		},
		title: '',
		operateType: 'add' as OperateType,
		showBatchDelete: false,
		languageList: [] as any,
		imageTypeList: [] as any,
		imgSizeList: [] as any,
		icoSizeList: [] as any,
		statusList: [] as any,
		baseUrl: '' as string,
		equipmentTypeList: [] as any,
		equipmentTypeMap: {} as Record<string, any>
	});

	const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
	const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
	const { t: $t } = useI18n();
	const xGridVmsImage = ref<VxeGridInstance>();
	
	const options = useVxeTable<PageVmsImageOutput>(
		{
			id: 'vmsImageGrid',
			name: 'vmsImageGrid',
			columns: [
				{ type: 'checkbox', fixed: 'left', width: 45 },

				// Fields
				{ field: 'code', title: $t('lz.entity.base.code'), minWidth: 150, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_code' } },
				{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 200, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_name' } },
				{ field: 'type', title: $t('lz.entity.base.type'), minWidth: 60, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_type' } },
				{ field: 'equipmentTypeId', title: $t('lz.label.cctvEquipment.equipmentType'), width: 100, slots: { default: 'row_equipmentType' } },
				{ field: 'size', title: $t('lz.entity.vmsImage.size'), width: 100, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_size' } },
				{ field: '', title: $t('lz.entity.base.image'), minWidth: 200, showOverflow: 'tooltip', sortable: false, slots: { default: 'row_image' } },
				{ field: 'status', title: $t('lz.entity.base.status'), width: 110, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_status' } },

				// Action fields
				{ title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
				{ title: $t('lz.label.base.action'), fixed: 'right', width: 130, showOverflow: true, slots: { default: 'row_buttons' } },
			],
		},
		// vxeGrid configuration parameters (any parameter can be overridden here), refer to the official vxe-table documentation
		{
			// Proxy configuration
			proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },

			// Sorting configuration
			sortConfig: { defaultSort: state.localPageParam.defaultSort },

			// Pagination configuration
			pagerConfig: { pageSize: state.localPageParam.pageSize },

			// Toolbar configuration
			toolbarConfig: { export: false },

			rowConfig: {
				height: 120
			}
		}
	);

	onMounted(async () => {
		state.statusList = getConfigDataByCode(BaseConfigTypeEnum.SysStatusType);
		state.imgSizeList = getConfigDataByCode(BaseConfigTypeEnum.VmsImageSize);
		state.languageList = getConfigDataByCode(BaseConfigTypeEnum.SysLanguageType);
		state.imageTypeList = getConfigDataByCode(BaseConfigTypeEnum.VmsImageType);
		state.icoSizeList = getConfigDataByCode(BaseConfigTypeEnum.VmsIconSize);

		// Nạp danh sách loại thiết bị, chỉ giữ VMS/LCS còn hiệu lực -> map để hiển thị tên trong grid
		const resType = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet();
		state.equipmentTypeList = (resType.data?.result ?? []).filter((t: any) => t.isDelete == null && ALLOWED_TYPE_CODES.includes(t.code));
		state.equipmentTypeList.forEach((i: any) => (state.equipmentTypeMap[i.id] = i));

		// - Begin - add by KhoaND

		// Địa chỉ file server dùng chung (chỉ gọi API 1 lần cho toàn app)
		state.baseUrl = await useItsMediaServerStore().ensureLoaded();

		// - End - add by KhoaND

		await handleQuery();
	});

	// Query API
	const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
		const params = Object.assign(
			state.queryParams,
			{ page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order, descStr: 'desc' }
		) as PageVmsImageInput;
		
		return await getAPI(VmsImageApi).apiVmsVmsimagePagePost(params);
	};

	// Query operation
	const handleQuery = async () => {
		options.loading = true;
		await xGridVmsImage.value?.commitProxy('reload') ;

		options.loading = false;
		selectChangeEvent();
	};

	// Reset operation
	const resetQuery = async () => {
		state.queryParams.name = undefined;
		state.queryParams.code = undefined;
		state.queryParams.type = undefined;
		state.queryParams.lang = undefined;
		state.queryParams.equipmentTypeId = undefined;
		state.queryParams.status = undefined;
		await xGridVmsImage.value?.commitProxy('reload');
	};

	// Open add dialog
	const handleAdd = () => {
		state.title = $t('lz.label.base.titleAdd', {name: $t('lz.label.vms.vmsImage')});
		state.operateType = 'add';
		editImageCompRef.value?.openDialog({ status: 1, orderNo: 100, type: 'icon', lang: 'vi-VN' });
	};

	// Open copy dialog
	const handleCopy = (row: any) => {
		state.title = $t('lz.label.base.titleCopy', {name: $t('lz.label.vms.vmsImage')});
		state.operateType = 'copy';

		editImageCompRef.value?.openDialog(row);
	};

	// Open edit dialog
	const handleEdit = (row: any) => {
		state.title = $t('lz.label.base.titleEdit', {name: $t('lz.label.vms.vmsImage')});
		state.operateType = 'edit';

		editImageCompRef.value?.openDialog(row);
	};

	// Open view dialog
	const handleView = (row: any) => {
		state.title = $t('lz.label.base.titleView', {name: $t('lz.label.vms.vmsImage')});
		state.operateType = 'view';

		detailImageCompRef.value?.openDialog(row);
	};

	// Delete
	const handleDelete = (row: any) => {
		ElMessageBox.confirm($t('lz.message.base.confirmDelete', {name:row.name}), $t('lz.label.base.confirm'), {
			confirmButtonText: $t('lz.button.base.confirm'),
			cancelButtonText: $t('lz.button.base.cancel'),
			type: 'warning',
		})
		.then(async () => {
			try{
				await getAPI(VmsImageApi).apiVmsVmsimageDeletePost({ id: row.id });
				handleQuery();
				ElMessage.success($t('lz.message.base.processSuccess'));
			}
			catch(error){
				ElMessage.success($t('lz.message.base.processUnSuccess'));
			}
		})
		.catch(() => { });
	};

	// Custom column
	function handleCustom() {
		xGridVmsImage.value?.openCustom();
	}

	// Get selected records
	function getCheckedRecords() {
		return xGridVmsImage.value?.getCheckboxRecords() ?? [];
	}

	function selectChangeEvent() {
		state.showBatchDelete = getCheckedRecords().length > 0;
	}

	//Batch delete
	const handleBatchDelete = async () => {
		const selectRecords = getCheckedRecords();

		ElMessageBox.confirm($t('lz.message.base.confirmBatchDelete', { total: selectRecords.length }), $t('lz.label.base.confirm'), {
			confirmButtonText: $t('lz.button.base.confirm'),
			cancelButtonText: $t('lz.button.base.cancel'),
			type: 'warning',
		})
		.then(async () => {
			try{
				await getAPI(VmsImageApi).apiVmsVmsimageBatchdeletePost(selectRecords.filter(l => l.id));
				handleQuery();
				ElMessage.success($t('lz.message.base.processSuccess'));
			}
			catch(error){
				ElMessage.success($t('lz.message.base.processUnSuccess'));
			}
		})
		.catch(() => { });
	}

	// Table events
	const gridEvents: VxeGridListeners<PageVmsImageOutput> = {
		// Only valid when pager-config is configured, this event will be triggered when the page changes
		async pageChange({ pageSize }) {
			state.localPageParam.pageSize = pageSize;
		},
		// This event will be triggered when the sorting condition changes
		async sortChange({ field, order }) {
			state.localPageParam.defaultSort = { field: field, order: order! };
		},
	};
</script>

<style scoped>
	.text-h1{
		font-size: large;
	}

	.text-upper{
		text-transform: uppercase;
	}

	.text-h2{
		font-size: 14px;
	}

	.panel-img img{
		max-width: 100%;
		max-height: 120px;
		padding: 10px;
	}

	.panel-img :deep(.el-image){
		max-height: 120px;
		cursor: zoom-in;
	}

	.panel-img :deep(.el-image img){
		max-height: 120px;
	}
</style>