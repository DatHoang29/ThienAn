<template>
	<el-dialog v-model="state.isShowDialog" title="Title" width="800">
		<template #header>
			<div style="color: #fff">
				<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-View /></el-icon>
				<span> {{ props.title }} </span>
			</div>
		</template>
		<table :model="state.imageInfo" class="table-data w100">
			<tr class="alt">
				<th>{{ $t('lz.entity.base.name') }}</th>
				<td><b class="text-h2 text-upper">{{ state.imageInfo.name }}</b></td>
				<th>{{ $t('lz.entity.base.code') }}</th>
				<td>
					<el-tag class="text-h2 text-upper" size="large" type="primary">
						<b>{{ state.imageInfo.code }}</b>
					</el-tag>
				</td>
			</tr>
			<tr>
				<th>{{ $t('lz.entity.base.type') }}</th>
				<td>{{ state.imageTypeList?.find((item: any) => item.code == state.imageInfo.type)?.name }}</td>
				<th>{{ $t('lz.entity.vmsImage.size') }}</th>
				<td>
					{{ state.imageInfo.type == 'icon' ? state.icoSizeList?.find((item: any) => item.code == state.imageInfo.size)?.name : state.imgSizeList?.find((item: any) => item.code == state.imageInfo.size)?.name }}
				</td>
			</tr>
			<tr>
				<th>{{ $t('lz.entity.base.lang') }}</th>
				<td>{{ state.languageList?.find((item: any) => item.code == state.imageInfo.lang)?.name }}</td>
				<th></th>
				<td></td>
			</tr>
			<tr :class="(state.imageInfo.type == '1' ? 'alt' : '')">
				<th>{{ $t('lz.entity.base.orderNo') }}</th>
				<td>{{ state.imageInfo.orderNo }}</td>
				<th>{{ $t('lz.entity.base.status') }}</th>
				<td>
					<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, state.imageInfo.status)" />
				</td>
			</tr>
			<tr :class="(state.imageInfo.type == '1' ? '' : 'alt')">
				<th>{{ $t('lz.entity.base.image') }}</th>
				<td colspan="3">
					<div class="panel-img">
						<img v-if="state.imageInfo.url" :src="commonVarsAndFuncs.getFullUrl(state.imageInfo.url)" />
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
  
<script lang="ts" setup name="vmsDetailImage">
	import { reactive, onMounted, defineAsyncComponent } from 'vue';
	import { useI18n } from 'vue-i18n';

	import { PageVmsMessageOutput } from '/@/api-services/vms';

	import { useCommonVarsAndFuncs } from '/@/stores/its/commonVarsAndFuncs';
    const commonVarsAndFuncs = useCommonVarsAndFuncs();

	import { useBaseConfig } from '/@/stores/baseConfig';
	import { BaseConfigTypeEnum } from '/@/types/enums';
	import { OperateType } from '/@/types/constants';
	const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
	const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
	const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
	
	interface Props {
		operateType: OperateType;
		title: string;
	}
	const { t: $t } = useI18n();
	const props = defineProps<Props>();
  	const state = reactive({
		isShowDialog: false,
		imageInfo: {} as PageVmsMessageOutput,
		languageList: [] as any,
		imageTypeList: [] as any,
		imgSizeList: [] as any,
		icoSizeList: [] as any,
		statusList: [] as any,
	});

	onMounted(async () => {
		state.statusList = getConfigDataByCode(BaseConfigTypeEnum.SysStatusType);
		state.imgSizeList = getConfigDataByCode(BaseConfigTypeEnum.VmsImageSize);
		state.languageList = getConfigDataByCode(BaseConfigTypeEnum.SysLanguageType);
		state.imageTypeList = getConfigDataByCode(BaseConfigTypeEnum.VmsImageType);
		state.icoSizeList = getConfigDataByCode(BaseConfigTypeEnum.VmsIconSize);
	});

	const openDialog = async (row: any) => {
		state.imageInfo = JSON.parse(JSON.stringify(row));
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

	.panel-img img{
		max-width: 100%;
		max-height: 150px;
		padding: 10px;
	}
</style>