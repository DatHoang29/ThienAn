<template>
	<el-card class="full-table split-right-list" shadow="hover" style="margin-top: 5px">
		<vxe-grid ref="xGridRight" class="xGrid-style" v-bind="optionsGrid">
			<template #toolbar_buttons>
				{{ props.title }}
			</template>
			<template #toolbar_tools>
				<TableHeader :show-delete="false" :show-add="false" :show-custom-column="false" @refresh="handleQuery" @custom-column="handleCustom"> </TableHeader>
			</template>
			<template #empty>
				<el-empty :image-size="200" />
			</template>
		</vxe-grid>
	</el-card>
</template>

<script lang="ts" setup name="tollHauKiem">
import { ref, reactive, defineAsyncComponent } from 'vue';
import { VxeGridInstance } from 'vxe-table';
import { useI18n } from 'vue-i18n';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';

/* TODO(TOLL): chưa có API TOLL tương ứng - lịch sử hậu kiểm (acs10HauKiem)
import { getAPI } from '/@/utils/axios-utils';
import { GetQueryHistoryPostBikeInput, ListHistotyPostBikeOutput, HkXeMayApi } from '/@/api-services/acs10HauKiem';
import { HauKiemTheoLuotOtoQueryConst } from '/@/types/constants';
*/

const props = defineProps({
	title: {
		type: String,
		default: '',
	},
});

const state = reactive({
	localPageParam: {
		pageSize: 50 as number,
		defaultSort: { field: 'orderNo', order: 'asc' },
	},
	queryParams: {} as any,
});

defineEmits(['query', 'selectItem']);

const { t: $t } = useI18n();
const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const xGridRight = ref<VxeGridInstance>();

const optionsGrid = useVxeTable<any>(
	{
		id: 'tollHauKiemHistory',
		name: 'Right List',
		columns: [
			{ field: 'stt', title: $t('lz.label.base.idx'), width: 50, fixed: 'left' },
			{ field: 'createdDated', title: $t('lz.entity.base.updateTime'), minWidth: 120, showOverflow: 'tooltip', sortable: true },
			{ field: 'createdUName', title: $t('lz.entity.base.updateBy'), minWidth: 140, showOverflow: 'tooltip', sortable: true },
			{ field: 'note', title: $t('lz.entity.base.remark'), minWidth: 140, showOverflow: 'tooltip', sortable: true },
		],
	},
	// vxeGrid configuration parameters (can override any parameters), refer to vxe-table official documentation
	{
		// Proxy configuration
		proxyConfig: { autoLoad: true, ajax: { query: () => handleQueryApi() } },
		// // Toolbar configuration
		toolbarConfig: { export: false },
		pagerConfig: { enabled: false },
		// Row configuration
		rowConfig: { isCurrent: false, isHover: true },
		size: 'mini',
	}
);

// Query API
// TODO(TOLL): chưa có API TOLL tương ứng - lấy lịch sử hậu kiểm, tạm trả về mảng rỗng
const handleQueryApi = async () => {
	/*
	const response = await getAPI(HkXeMayApi).apiAcs10haukiemHkxemayListhistorybikePost(state.queryParams);
	return response;
	*/
	// Trả về response rỗng theo đúng dạng mà useVxeTable đọc (data.result)
	return { data: { result: [] as any[] } };
};

// Set query params
const setQueryParams = (id: any) => {
	//TODO: chỉnh lại query params
	state.queryParams.idRecheck = id;
};

// Query operation
const handleQuery = async () => {
	// emit('query');
	// TODO(TOLL): chưa có API TOLL tương ứng - hằng số loại truy vấn lịch sử (HauKiemTheoLuotOtoQueryConst)
	// if (state.queryParams) state.queryParams.type = HauKiemTheoLuotOtoQueryConst.Select_Hisory;

	await xGridRight.value?.commitProxy('query');
};

//#region button event
// Handle custom column
const handleCustom = () => {
	xGridRight.value?.openCustom();
};

//#endregion button event

// Expose methods to parent component
defineExpose({
	xGridRight,
	setQueryParams,
	handleQuery,
});
</script>

<style scoped>
.full-table {
	height: 100%;
}
</style>
