<template>
	<div class="esh-page">
		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', alignItems: 'start' }">
			<el-form :model="state.query" ref="queryForm" :show-message="false" label-width="auto" style="flex: 1 1 0%" label-position="left">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6">
						<el-form-item :label="$t('lz.label.sharedataEventSource.keyword')"><el-input v-model="state.query.keyword" clearable :placeholder="$t('lz.label.sharedataEventSource.keywordPlaceholder')" @keyup.enter="handleQuery" /></el-form-item>
					</el-col>
				</el-row>
			</el-form>
			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />
			<el-row><SearchButton @search="handleQuery" @reset="resetQuery" :loading="options.loading" /></el-row>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options">
				<template #toolbar_tools>
					<span class="esh-hint">{{ $t('lz.label.sharedataEventSource.pendingBackendHint') }}</span>
					<el-button v-auth="'eshEventSource:add'" type="primary" icon="ele-Plus" class="ml10" @click="handleAdd">{{ $t('lz.button.sharedataEventSource.add') }}</el-button>
					<el-tooltip :content="$t('lz.button.base.refresh')" placement="top" :auto-close="1000">
						<el-button icon="ele-Refresh" circle class="ml10" @click="handleQuery" />
					</el-tooltip>
				</template>
				<template #empty><el-empty :description="$t('lz.label.sharedataEventSource.noData')" :image-size="160" /></template>
				<!-- <template #row_datatype="{ row }">{{ row.datatypeCode ? datatypeLabel(row.datatypeCode, $t) : '—' }}</template> -->
				<template #row_buttons="{ row }">
					<el-button v-auth="'eshEventSource:update'" text type="primary" icon="ele-Edit" @click="handleEdit(row)">{{ $t('lz.button.sharedataEventSource.edit') }}</el-button>
					<el-button v-auth="'eshEventSource:delete'" text type="danger" icon="ele-Delete" @click="handleDelete(row)">{{ $t('lz.button.sharedataEventSource.delete') }}</el-button>
				</template>
			</vxe-grid>
		</el-card>

		<EditEventSource ref="editRef" @handleQuery="handleQuery" />
	</div>
</template>

<script lang="ts" setup name="eshEventSource">
import { defineAsyncComponent, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { VxeGridInstance, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
// import { emptyPage, notImplemented, datatypeLabel } from './types';
import type { EventSource } from './types';

const { t: $t } = useI18n();

const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const EditEventSource = defineAsyncComponent(() => import('./component/editEventSource.vue'));

const xGrid = ref<VxeGridInstance>();
const editRef = ref<InstanceType<typeof EditEventSource>>();
const state = reactive({ query: { keyword: '' } });

const options = useVxeTable<EventSource>(
	{
		id: 'eshEventSource',
		name: 'EshEventSource',
		columns: [
			{ type: 'seq', title: $t('lz.label.sharedataEventSource.seq'), width: 60, fixed: 'left' },
			{ field: 'code', title: $t('lz.entity.sharedataEventSource.code'), width: 160, showOverflow: 'tooltip' },
			{ field: 'name', title: $t('lz.entity.sharedataEventSource.name'), minWidth: 200, showOverflow: 'tooltip' },
			{ field: 'subject', title: $t('lz.entity.sharedataEventSource.subject'), minWidth: 200, showOverflow: 'tooltip' },
			{ field: 'datatypeCode', title: $t('lz.entity.sharedataEventSource.datatypeCode'), width: 180, slots: { default: 'row_datatype' } },
			{ field: 'description', title: $t('lz.entity.sharedataEventSource.description'), minWidth: 220, showOverflow: 'tooltip' },
			{ title: $t('lz.label.sharedataEventSource.actions'), fixed: 'right', width: 150, slots: { default: 'row_buttons' } },
		],
	},
	{
		proxyConfig: { autoLoad: true, ajax: { query: ({ page }) => handleQueryApi(page) } },
		pagerConfig: { pageSize: 50 },
		toolbarConfig: { export: false },
		rowConfig: { keyField: 'id', isHover: true },
	}
);

async function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams) {
	try {
		// return emptyPage();
	} catch (e: any) {
		ElMessage.error(e?.message ? $t(e.message) : $t('lz.message.sharedataEventSource.loadDataFailed'));
		return { data: { result: { records: [], total: 0 } } };
	}
}

const handleQuery = async () => {
	options.loading = true;
	await xGrid.value?.commitProxy('reload');
	options.loading = false;
};
const resetQuery = async () => {
	state.query.keyword = '';
	await handleQuery();
};
const handleAdd = () => editRef.value?.openDialog();
const handleEdit = (row: EventSource) => editRef.value?.openDialog(row);
const handleDelete = (row: EventSource) => {
	return;
};
</script>

<style scoped lang="scss">
.esh-page {
	display: flex;
	flex-direction: column;
	height: 100%;
}
.esh-hint {
	color: var(--el-text-color-secondary);
	font-size: 12px;
}

/* Chuẩn hóa VXE-Grid Loading & Element Loading — 8-dots spinner bự vừa vặn */
:deep(.vxe-table--loading) {
	.vxe-loading--default-icon {
		display: inline-block;
		width: 50px;
		height: 50px;
		font-size: 0;
		color: transparent;
		background-color: var(--el-color-primary, #409eff);
		mask-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 50 50'%3E%3Ccircle cx='25' cy='6' r='3.4' fill='white' opacity='1'/%3E%3Ccircle cx='38.44' cy='11.56' r='3.4' fill='white' opacity='0.875'/%3E%3Ccircle cx='44' cy='25' r='3.4' fill='white' opacity='0.75'/%3E%3Ccircle cx='38.44' cy='38.44' r='3.4' fill='white' opacity='0.625'/%3E%3Ccircle cx='25' cy='44' r='3.4' fill='white' opacity='0.5'/%3E%3Ccircle cx='11.56' cy='38.44' r='3.4' fill='white' opacity='0.375'/%3E%3Ccircle cx='6' cy='25' r='3.4' fill='white' opacity='0.25'/%3E%3Ccircle cx='11.56' cy='11.56' r='3.4' fill='white' opacity='0.125'/%3E%3C/svg%3E");
		-webkit-mask-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 50 50'%3E%3Ccircle cx='25' cy='6' r='3.4' fill='white' opacity='1'/%3E%3Ccircle cx='38.44' cy='11.56' r='3.4' fill='white' opacity='0.875'/%3E%3Ccircle cx='44' cy='25' r='3.4' fill='white' opacity='0.75'/%3E%3Ccircle cx='38.44' cy='38.44' r='3.4' fill='white' opacity='0.625'/%3E%3Ccircle cx='25' cy='44' r='3.4' fill='white' opacity='0.5'/%3E%3Ccircle cx='11.56' cy='38.44' r='3.4' fill='white' opacity='0.375'/%3E%3Ccircle cx='6' cy='25' r='3.4' fill='white' opacity='0.25'/%3E%3Ccircle cx='11.56' cy='11.56' r='3.4' fill='white' opacity='0.125'/%3E%3C/svg%3E");
		mask-repeat: no-repeat;
		-webkit-mask-repeat: no-repeat;
		mask-position: center;
		-webkit-mask-position: center;
		mask-size: contain;
		-webkit-mask-size: contain;
		animation: sharedata-roll 0.9s linear infinite;
		transform-origin: center;
		will-change: transform;

		&::before {
			display: none;
		}
	}
	.vxe-loading--text {
		font-size: 14px;
		font-weight: 500;
		color: var(--el-color-primary, #409eff);
		padding-top: 10px;
	}
}

:deep(.el-loading-mask) {
	background-color: rgba(255, 255, 255, 0.78);
	backdrop-filter: blur(1px);
	border-radius: inherit;
	transition: opacity 0.25s ease;
	z-index: 100;
}

:deep(.el-loading-spinner) {
	top: 50%;
	transform: translateY(-50%);
	margin-top: 0;
	width: 100%;
	text-align: center;
	position: absolute;

	.circular {
		display: inline-block;
		width: 50px;
		height: 50px;
		background-color: var(--el-color-primary, #409eff);
		mask-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 50 50'%3E%3Ccircle cx='25' cy='6' r='3.4' fill='white' opacity='1'/%3E%3Ccircle cx='38.44' cy='11.56' r='3.4' fill='white' opacity='0.875'/%3E%3Ccircle cx='44' cy='25' r='3.4' fill='white' opacity='0.75'/%3E%3Ccircle cx='38.44' cy='38.44' r='3.4' fill='white' opacity='0.625'/%3E%3Ccircle cx='25' cy='44' r='3.4' fill='white' opacity='0.5'/%3E%3Ccircle cx='11.56' cy='38.44' r='3.4' fill='white' opacity='0.375'/%3E%3Ccircle cx='6' cy='25' r='3.4' fill='white' opacity='0.25'/%3E%3Ccircle cx='11.56' cy='11.56' r='3.4' fill='white' opacity='0.125'/%3E%3C/svg%3E");
		-webkit-mask-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 50 50'%3E%3Ccircle cx='25' cy='6' r='3.4' fill='white' opacity='1'/%3E%3Ccircle cx='38.44' cy='11.56' r='3.4' fill='white' opacity='0.875'/%3E%3Ccircle cx='44' cy='25' r='3.4' fill='white' opacity='0.75'/%3E%3Ccircle cx='38.44' cy='38.44' r='3.4' fill='white' opacity='0.625'/%3E%3Ccircle cx='25' cy='44' r='3.4' fill='white' opacity='0.5'/%3E%3Ccircle cx='11.56' cy='38.44' r='3.4' fill='white' opacity='0.375'/%3E%3Ccircle cx='6' cy='25' r='3.4' fill='white' opacity='0.25'/%3E%3Ccircle cx='11.56' cy='11.56' r='3.4' fill='white' opacity='0.125'/%3E%3C/svg%3E");
		mask-repeat: no-repeat;
		-webkit-mask-repeat: no-repeat;
		mask-position: center;
		-webkit-mask-position: center;
		mask-size: contain;
		-webkit-mask-size: contain;
		animation: sharedata-roll 0.9s linear infinite;
		transform-origin: center;
		will-change: transform;

		.path {
			display: none;
		}
	}

	.el-loading-text {
		color: var(--el-color-primary, #409eff);
		font-size: 14px;
		font-weight: 500;
		letter-spacing: 0.3px;
		margin: 10px 0 0;
		line-height: 1.4;
	}

	&:not(:has(.el-loading-text))::after {
		content: 'Đang tải...';
		display: block;
		color: var(--el-color-primary, #409eff);
		font-size: 14px;
		font-weight: 500;
		letter-spacing: 0.3px;
		margin-top: 10px;
		line-height: 1.4;
	}
}

@keyframes sharedata-roll {
	from {
		transform: rotate(0deg);
	}
	to {
		transform: rotate(360deg);
	}
}
</style>
