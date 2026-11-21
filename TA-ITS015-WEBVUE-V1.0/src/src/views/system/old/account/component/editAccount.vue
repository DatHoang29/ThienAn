<template>
	<div class="sys-account-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="700px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span>{{ $t(props.title) }}</span>
				</div>
			</template>
			<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto" label-position="left" >
				<el-row :gutter="10">
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.username')" prop="username" :rules="[{ required: true, message: $t('lz.validation.base.required',{PropertyName: $t('lz.entity.base.username')}), trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.username" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.username')})" clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.name')" prop="name" :rules="[{ required: true, message: $t('lz.validation.base.required',{PropertyName: $t('lz.entity.base.name')}), trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.name" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.name')})" clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.label.system.role')" prop="roleIdList" >
							<el-select v-model="state.ruleForm.roleIdList" multiple value-key="id" clearable :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.label.system.role')})"  class="w100" filterable>
								<el-option v-for="item in state.roleData" :key="item.id" :label="item.name" :value="item.id" :disabled="item.status !== (StatusTypeEnum.Enable as number)"/>
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.sysAccount.accountType')" prop="accountType" :rules="[{ required: true, message: $t('lz.validation.base.required',{PropertyName: $t('lz.entity.sysAccount.accountType')}), trigger: 'blur' }]">
							<el-select v-model="state.ruleForm.accountType" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.sysAccount.accountType')})" collapse-tags collapse-tags-tooltip class="w100">
								<!-- <el-option label="Super Admin" :value="'111'" :disabled="userInfos.accountType != '111'"/>
								<el-option label="System Administrator" :value="'222'" :disabled="userInfos.accountType != '111' && userInfos.accountType != '222'"/>
								<el-option label="Regular Account" :value="'333'" :disabled="userInfos.accountType != '111' && userInfos.accountType != '222' && userInfos.accountType != '333'"/>
								<el-option label="Member" :value="'444'" /> -->
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysAccountType)" :key="item.code" :label="item.name" :value="item.code"/>
							</el-select>
						</el-form-item>
					</el-col>


					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.email')" prop="email" :rules="[ { pattern: emailRegex, message: $t('lz.validation.base.emailInvalid'), trigger: ['blur'] } ]">
							<el-input v-model="state.ruleForm.email" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.email')})" clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.base.remark')">
							<el-input v-model="state.ruleForm.remark" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.remark')})" clearable type="textarea" />
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
			
			<template #footer>
				<span class="dialog-footer">
					<el-button @click="cancel">{{ $t('lz.button.base.cancel') }}</el-button>
					<el-button type="primary" @click="submit">{{ $t('lz.button.base.confirm') }}</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="sysEditAccount">
import { onMounted, reactive, ref } from 'vue';
import { getAPI } from '/@/utils/axios-utils';
import { SysRoleApi, SysAccountApi} from '/@/api-services/system';
import { RoleOutput,  UpdateAccountInput } from '/@/api-services/system';
import { OperateType } from '/@/types/constants';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
const { t: $t } = useI18n();
import { BaseConfigTypeEnum, StatusTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import { emailRegex } from '/@/types/validators';
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
interface Props {
	/** the type of operation */
	operateType: OperateType;
	title: string
}


const props = defineProps<Props>();

const emits = defineEmits(['handleQuery']);
const ruleFormRef = ref();

const state = reactive({
	loading: false,
	isShowDialog: false,
	selectedTabName: '0', // Selected tab page
	ruleForm: {} as UpdateAccountInput,
	roleData: [] as Array<RoleOutput>, // Role data
});

// Page initialization
onMounted(async () => {
	state.loading = true;
	const { data: res } = await getAPI(SysRoleApi).apiSystemSysroleListGet();
	state.roleData = res.result ?? [];
	state.loading = false;
});

// Open dialog
const openDialog = async (row: any) => {
	ruleFormRef.value?.resetFields();

	state.selectedTabName = '0'; // Reset to the first tab page
	state.ruleForm = JSON.parse(JSON.stringify(row));
	if (row.id != undefined) {
		var resRole = await getAPI(SysAccountApi).apiSystemSysaccountOwnrolelistGet(row.id);
		state.ruleForm.roleIdList = resRole.data.result;

	} else state.ruleForm.accountType = '333'; // Default regular account type
	state.isShowDialog = true;
};

// Close dialog
const closeDialog = () => {
	emits('handleQuery');
	state.isShowDialog = false;
};

// Cancel
const cancel = () => {
	state.isShowDialog = false;
};

// Submit
const submit = () => {
	ruleFormRef.value.validate(async (valid: boolean) => {
		if (!valid) return;
		//Edit
		if (props.operateType == 'edit') {
			await getAPI(SysAccountApi).apiSystemSysaccountUpdatePost(state.ruleForm);
		}
		//Copy or Add
		else if (props.operateType == 'copy' || props.operateType == 'add') {
			state.ruleForm.id = '';
			await getAPI(SysAccountApi).apiSystemSysaccountAddPost(state.ruleForm);

		}
		ElMessage.success($t('lz.message.base.saveSuccess'));
		closeDialog();
	});
};
// Export object
defineExpose({ openDialog });
</script>
