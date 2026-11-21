<template>
	<div class="edit-message-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="700px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-Edit /></el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<el-form :model="state.editForm" ref="editFormRef" label-width="auto" label-position="left">
				<el-row :gutter="10">
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.name')" prop="name" :rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.base.name')}), trigger: 'blur' }, { max: EntityFieldLength.Length64, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.base.name'), MaxLength: EntityFieldLength.Length64}), trigger: 'blur'}]">
							<el-input v-model="state.editForm.name" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.base.name')})" :maxlength="EntityFieldLength.Length64" show-word-limit clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.code')" prop="code" :rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.base.code')}), trigger: 'blur' }, { max: EntityFieldLength.Length64, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.base.code'), MaxLength: EntityFieldLength.Length64}), trigger: 'blur'}]">
							<el-input v-model="state.editForm.code" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.base.code')})" :maxlength="EntityFieldLength.Length64" show-word-limit clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.type')" prop="type" :rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.base.type')}), trigger: 'blur' }]">
							<el-select v-model="state.editForm.type" @change="handleTypeChange" :placeholder="$t('lz.label.base.pleaseSelect', {input: $t('lz.entity.base.type')})" class="w100">
								<el-option v-for="item in state.messageTypeList" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.lang')" prop="lang" :rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.base.lang')}), trigger: 'blur' }]">
							<el-select v-model="state.editForm.lang" class="w100">
								<el-option v-for="item in state.languageList" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.vmsMessage.content1')" prop="content1" :rules="[{ max: EntityFieldLength.Length256, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.vmsMessage.content1'), MaxLength: EntityFieldLength.Length256}), trigger: 'blur'}]">
							<el-input v-model="state.editForm.content1" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.vmsMessage.content1')})" :maxlength="EntityFieldLength.Length256" show-word-limit clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20" v-if="state.editForm.type == '1'">
						<el-form-item :label="$t('lz.entity.vmsMessage.content2')" prop="content2" :rules="[{ max: EntityFieldLength.Length256, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.vmsMessage.content2'), MaxLength: EntityFieldLength.Length256}), trigger: 'blur'}]">
							<el-input v-model="state.editForm.content2" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.vmsMessage.content2')})" :maxlength="EntityFieldLength.Length256" show-word-limit clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.status')" prop="status">
							<el-radio-group v-model="state.editForm.status" v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code">
								<el-radio :value="parseInt(item.code)" class="mr5">{{ item.name }}</el-radio>
							</el-radio-group>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.orderNo')" prop="orderNo">
							<el-input-number
								v-model="state.editForm.orderNo"
								:min="0"
								:placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.base.orderNo')})"
								controls-position="right"
								class="w100"
							/>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
			<template #footer>
				<span class="dialog-footer">
					<el-button @click="cancel">{{ $t('lz.button.base.cancel') }}</el-button>
					<el-button type="primary" @click="submit" :disabled="state.isProcessing">{{ $t('lz.button.base.confirm') }}</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="vmsEditMessage">
	import { reactive, ref, onMounted } from 'vue';
	import { getAPI } from '/@/utils/axios-utils';
	import { OperateType } from '/@/types/constants';
	import { ElMessage } from 'element-plus';
	import { useI18n } from 'vue-i18n';

	import { VmsMessageApi } from '/@/api-services/vms';
	import { UpdateVmsMessageInput } from '/@/api-services/vms';

	import { useBaseConfig } from '/@/stores/baseConfig';
	import { BaseConfigTypeEnum } from '/@/types/enums';
	
	const getConfigDataByCode = useBaseConfig().getConfigDataByCode;

	import { EntityFieldLength } from '/@/types/enums/baseEnum';

	const { t: $t } = useI18n();
	interface Props {
		operateType: OperateType;
		title: string;
	}

	const props = defineProps<Props>();
	const emits = defineEmits(['handleQuery']);

	const editFormRef = ref();
	const state = reactive({
		isShowDialog: false,
		isProcessing: false,
		editForm: {} as UpdateVmsMessageInput,
		languageList: [] as any,
		messageTypeList: [] as any,
		statusList: [] as any,
	});

	onMounted(async () => {
		state.statusList = getConfigDataByCode(BaseConfigTypeEnum.SysStatusType);
		state.messageTypeList = getConfigDataByCode(BaseConfigTypeEnum.VmsMessageType);
		state.languageList = getConfigDataByCode(BaseConfigTypeEnum.SysLanguageType);
	});

	const openDialog = (row: any) => {
		editFormRef.value?.resetFields();
		state.editForm = JSON.parse(JSON.stringify(row));
		state.isShowDialog = true;
	};

	// Chỉ loại Message (type = '1') mới dùng content2. Khi đổi sang loại khác (Phrase...),
	// xóa content2 để không lưu và hiển thị nội dung thừa từ lần nhập trước.
	const handleTypeChange = (value: any) => {
		if (value != '1') {
			state.editForm.content2 = undefined;
		}
	};

	const closeDialog = () => {
		emits('handleQuery');
		state.isShowDialog = false;
	};

	const cancel = () => {
		state.isShowDialog = false;
	};

	const submit = () => {
		editFormRef.value.validate(async (valid: boolean) => {
			
			if (!valid) return;

			try{
				state.isProcessing = true;

				//Edit
				if (props.operateType == 'edit') {
					await getAPI(VmsMessageApi).apiVmsVmsmessageUpdatePost(state.editForm);
				}
				//Copy or Add
				else if (props.operateType == 'copy' || props.operateType == 'add') {
					state.editForm.id = '';

					await getAPI(VmsMessageApi).apiVmsVmsmessageAddPost(state.editForm);
				}

				ElMessage.success($t('lz.message.base.processSuccess'));
			
				closeDialog();
			}
			catch(error){}
			finally{
				state.isProcessing = false;
			}
		});
	};

	defineExpose({ openDialog });

</script>
