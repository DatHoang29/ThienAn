<template>
	<el-dialog v-model="state.isShowDialog" title="Title" width="800">
		<template #header>
			<div style="color: #fff">
				<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-View /></el-icon>
				<span> {{ props.title }} </span>
			</div>
		</template>
		<table :model="state.dataList" class="table-data w100">
			<tr class="alt">
				<th>{{ $t('lz.entity.base.name') }}</th>
				<td><b class="text-h2 text-upper">{{ state.dataList.name }}</b></td>
				<th>{{ $t('lz.entity.base.code') }}</th>
				<td>
					<el-tag class="text-h2 text-upper" size="large" type="primary">
						<b>{{ state.dataList.code }}</b>
					</el-tag>
				</td>
			</tr>
			<tr>
				<th>{{ $t('lz.entity.base.lang') }}</th>
				<td>{{ state.languageList?.find((item: any) => item.code == state.dataList.lang)?.name }}</td>
				<th>{{ $t('lz.entity.base.type') }}</th>
				<td>{{ state.messageTypeList?.find((item: any) => item.code == state.dataList.type)?.name }}</td>
			</tr>
			<tr class="alt">
				<th>{{ $t('lz.entity.vmsMessage.content1') }}</th>
				<td colspan="3"><p class="text-h2 text-upper">{{ state.dataList.content1 }}</p></td>
			</tr>
			<tr v-if="state.dataList.type == '1'">
				<th>{{ $t('lz.entity.vmsMessage.content2') }}</th>
				<td colspan="3"><p class="text-h2 text-upper">{{ state.dataList.content2 }}</p></td>
			</tr>
			<tr :class="(state.dataList.type == '1' ? 'alt' : '')">
				<th>{{ $t('lz.entity.base.orderNo') }}</th>
				<td>{{ state.dataList.orderNo }}</td>
				<th>{{ $t('lz.entity.base.status') }}</th>
				<td>
					<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, state.dataList.status)" />
				</td>
			</tr>
			<tr :class="(state.dataList.type == '1' ? '' : 'alt')">
				<th>{{ $t('lz.entity.base.image') }}</th>
				<td colspan="3">
					<div class="panel-msg">
						<b class=" text-upper text-h2">{{ state.dataList.content1 }}</b>
						<b class=" text-upper text-h2">{{ state.dataList.content2 }}</b>
					</div>
				</td>
			</tr>
		</table>		
		<template #footer>
			<span class="dialog-footer">
				<el-button type="danger" @click="state.isShowDialog = false">{{ $t('lz.button.base.close') }}</el-button>
			</span>
		</template>
	</el-dialog>
</template>
  
<script lang="ts" setup name="vmsDetailMessage">
	import { reactive, onMounted, defineAsyncComponent } from 'vue';
	import { useI18n } from 'vue-i18n';

	import { useBaseConfig } from '/@/stores/baseConfig';
	import { BaseConfigTypeEnum } from '/@/types/enums';
	
	const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
	const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
	const getConfigItemByCode = useBaseConfig().getConfigItemByCode;

	import { PageVmsMessageOutput } from '/@/api-services/vms';

	interface Props {
		operateType: OperateType;
		title: string;
	}
	const { t: $t } = useI18n();
	const props = defineProps<Props>();
  	const state = reactive({
		isShowDialog: false,
		dataList: {} as PageVmsMessageOutput,
		languageList: [] as any,
		messageTypeList: [] as any,
		statusList: [] as any
	});

	onMounted(async () => {
		state.statusList = getConfigDataByCode(BaseConfigTypeEnum.SysStatusType);
		state.messageTypeList = getConfigDataByCode(BaseConfigTypeEnum.VmsMessageType);
		state.languageList = getConfigDataByCode(BaseConfigTypeEnum.SysLanguageType);
	});

	const openDialog = async (row: any) => {
		state.dataList = JSON.parse(JSON.stringify(row));
		state.isShowDialog = true;
	};

	defineExpose({ openDialog });
</script>

<style scoped>
	.table-data {
		border-collapse: collapse;  
	}

	.table-data td, th{
		padding: 15px 10px;
		text-align: left;
		border-color: #E6E8EB !important;
		border-left: thin solid;
		border-top: thin solid;
		border-right: thin solid;
	}

	.table-data tr:last-child td, th{
		border-bottom: thin solid;
	}

	.table-data tr.title th{
		text-align: center;
		font-size: large;
	}

	.table-data tr.alt{
		background-color: #F0F2F5;
	}

	.text-h1{
		font-size: large;
	}

	.text-upper{
		text-transform: uppercase;
	}

	.text-h2{
		font-size: 14px;
	}

	.panel-msg{
		background: black;
		display: flex;
		flex-direction: column;
		text-align: center;
		padding: 5px;
	}

	.panel-msg :first-child{
		color: red;
	}

	.panel-msg :last-child{
		color: #39e600;
	}
</style>