<template>
	<div class="detail-operation-log-container">
		<el-dialog v-model="state.isShowDialog" title="Title" width="800">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-View /></el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			
			<table class="table-data w100">
				<tr class="alt">
					<th>{{ $t('lz.entity.base.name') }}</th>
					<td>{{ state.signalLogInfo.equipmentName }}</td>
					<th>{{ $t('lz.entity.base.status') }}</th>
					<td>
						<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsActionType, state.signalLogInfo.state)"/>
					</td>
				</tr>
				<tr>
					<th>{{ $t('lz.label.tmsSignallog.beforeData') }}</th>
					<td>
						<div v-if="state.signalLogInfo.oldUrl" class="div-img">
							<el-image
								:src="commonVarsAndFuncs.getFullUrl(state.signalLogInfo.oldUrl)"
								:zoom-rate="1.2"
								:max-scale="7"
								:min-scale="0.2"
								:preview-src-list="[commonVarsAndFuncs.getFullUrl(state.signalLogInfo.oldUrl)]"
								:initial-index="4"
								fit="scale-down"
							>
								<template #error>
									<div class="image-error">
										<el-icon :size="30"><Picture /></el-icon>
									</div>
								</template>
							</el-image>
						</div>
						<div v-else class="div-msg">
							<p v-if="state.signalLogInfo.oldData"><vue-json-pretty :data="StringToObj(state.signalLogInfo.oldData)" /></p>
						</div>
					</td>
					<th>{{ $t('lz.label.tmsSignallog.afterData') }}</th>
					<td>
						<div v-if="state.signalLogInfo.newUrl" class="div-img">
							<el-image
								:src="commonVarsAndFuncs.getFullUrl(state.signalLogInfo.newUrl)"
								:zoom-rate="1.2"
								:max-scale="7"
								:min-scale="0.2"
								:preview-src-list="[commonVarsAndFuncs.getFullUrl(state.signalLogInfo.newUrl)]"
								:initial-index="4"
								fit="scale-down"
							>
								<template #error>
									<div class="image-error">
										<el-icon :size="30"><Picture /></el-icon>
									</div>
								</template>
							</el-image>
						</div>
						<div v-else class="div-msg">
							<p v-if="state.signalLogInfo.newData"><vue-json-pretty :data="StringToObj(state.signalLogInfo.newData)" /></p>
						</div>
					</td>
				</tr>
				<tr class="alt">
					<th>{{ $t('lz.entity.tmsSignallog.executeBy') }}</th>
					<td>
						<el-text type="primary">
							<i class="fa fa-user-circle-o"></i> {{ state.signalLogInfo.executeBy }}
						</el-text>
					</td>
					<th>{{ $t('lz.entity.tmsSignallog.executeTime') }}</th>
					<td>
						<el-text type="primary">
							<i class="fa fa-calendar"></i> {{ state.signalLogInfo.executeTime ? useDateFormat(state.signalLogInfo.executeTime, DateTimeFormatEnum.DateTimeDefault) : null }}
						</el-text>
					</td>
				</tr>
				<tr>
					<th>{{ $t('lz.entity.base.remark') }}</th>
					<td colspan="3">{{ state.signalLogInfo.remark }}</td>
				</tr>
			</table>
			
			<template #footer>
				<span class="dialog-footer">
					<el-button type="danger" @click="state.isShowDialog = false">{{ $t('lz.button.base.close') }}</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>
  
<script lang="ts" setup name="vmsOperationLogHistoryDetail">
	import { reactive, defineAsyncComponent } from 'vue';
	import { useI18n } from 'vue-i18n';
	import { useDateFormat } from '@vueuse/core';
	import { OperateType } from '/@/types/constants';
	import { Picture } from '@element-plus/icons-vue';

	import VueJsonPretty from 'vue-json-pretty';
	import 'vue-json-pretty/lib/styles.css';
	import { StringToObj } from '/@/utils/json-utils';

	const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));

	import { useCommonVarsAndFuncs } from '/@/stores/its/commonVarsAndFuncs';
	const commonVarsAndFuncs = useCommonVarsAndFuncs();
	
	import { DateTimeFormatEnum } from '/@/types/enums/baseEnum';

	import { useBaseConfig } from '/@/stores/baseConfig';
	import { BaseConfigTypeEnum } from '/@/types/enums';
	const getConfigItemByCode = useBaseConfig().getConfigItemByCode;

	interface Props {
		operateType: OperateType;
		title: string;
	}

	const { t: $t } = useI18n();
	const props = defineProps<Props>();
  	const state = reactive({
		isShowDialog: false,
		signalLogInfo: {}
	});

	const openDialog = async (row: any) => {
		state.isShowDialog = true;

		if(row){
			state.signalLogInfo = JSON.parse(JSON.stringify(row));
		}
	};

	defineExpose({openDialog});
</script>

<style lang="scss" scoped>
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

	.display-flex{
		display: flex;
		flex-wrap: wrap;
		gap: 5px 0px;
	}

	.div-img{
		max-width: 300px;
		display: flex;
		justify-content: center;
		align-items: center;
		height: 100%;
		width: 100%;
		padding: 5px;

		img{
			max-width: 100%;
			max-height: 100%;
		}
	}

	.image-error {
		display: flex;
		justify-content: center;
		align-items: center;
		width: 100%;
		height: 100%;
		color: #dddddd;
	}

	.image-error .el-icon {
		font-size: 30px;
	}
</style>