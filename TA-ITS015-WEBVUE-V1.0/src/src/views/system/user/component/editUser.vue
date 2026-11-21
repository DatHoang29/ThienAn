<template>
	<div class="sys-user-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="700px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span>{{ props.title }}</span>
				</div>
			</template>
			<el-tabs v-loading="state.loading" v-model="state.selectedTabName"> 
				<el-tab-pane :label="$t('lz.label.base.basicInfo')" style="height: 550px; overflow-y: auto; overflow-x: hidden">
					<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto" label-position="left">
						<el-row :gutter="10">
							
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.base.name')" prop="name" :rules="[{ required: true, message: $t('lz.validation.base.required',{PropertyName: $t('lz.entity.base.name')}), trigger: 'blur' }]">
									<el-input v-model="state.ruleForm.name" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.name')})" clearable />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.base.realName')" prop="realName">
									<el-input v-model="state.ruleForm.realName" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.realName')})" clearable />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.base.phone')" prop="phone" :rules="[{ pattern: phoneRegex, message: $t('lz.validation.base.phoneInvalid'),  trigger: ['blur', 'change']}]">
									<el-input v-model="state.ruleForm.phone" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.phone')})" clearable />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.base.email')" prop="email" :rules="[ { pattern: emailRegex, message: $t('lz.validation.base.emailInvalid'), trigger: ['blur'] } ]">
									<el-input v-model="state.ruleForm.email" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.email')})" clearable />
								</el-form-item>
							</el-col>
							<el-divider border-style="dashed" content-position="center">
								<div style="color: #b1b3b8">{{ $t('lz.entity.system.org') }}</div>
							</el-divider>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.system.org')" prop="orgId" >
									<el-cascader	
										:options="props.orgData"
										:props="{ checkStrictly: true, emitPath: false, value: 'id', label: 'name', expandTrigger: 'hover' }"
										:placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.system.org')})"
										clearable
										class="w100"
										v-model="state.ruleForm.orgId"
									>
										<template #default="{ node, data }">
											<span>{{ data.name }}</span>
											<span v-if="!node.isLeaf"> ({{ data.children.length }}) </span>
										</template>
									</el-cascader>
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.system.pos')" prop="posId" >
									<el-select v-model="state.ruleForm.posId" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.system.pos')})" class="w100">
										<el-option v-for="d in state.posData" :key="d.id" :label="d.name" :value="d.id" :disabled="d.status !== StatusTypeEnum.Enable as number" />
									</el-select>
								</el-form-item>
							</el-col>
							<el-divider border-style="dashed" content-position="center">
								<div style="color: #b1b3b8">{{ $t('lz.label.sysUser.affOrg') }}</div>
							</el-divider>
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb10">
								<el-button icon="ele-Plus" type="primary" plain @click="addExtOrgRow"> {{ $t('lz.button.base.add') }}</el-button>
								<span style="font-size: 12px; color: gray; padding-left: 5px"> {{ $t('lz.label.sysUser.dataPermission') }} </span>
							</el-col>
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<template v-if="state.ruleForm.extOrgIdList != undefined && state.ruleForm.extOrgIdList.length > 0">
									<el-row :gutter="10" v-for="(v, k) in state.ruleForm.extOrgIdList" :key="k">
										<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
											<el-form-item :label="$t('lz.entity.system.org')" :prop="`extOrgIdList[${k}].orgId`" :rules="[{ required: true, message: $t('lz.validation.base.required',{PropertyName: $t('lz.entity.system.org')}), trigger: 'blur' }]">
												<template #label>
													<el-button icon="ele-Delete" type="danger" circle plain size="small" @click="deleteExtOrgRow(k)" />
													<span class="ml5">{{ $t('lz.entity.system.org') }}</span>
												</template>
												<el-cascader
													:options="props.orgData"
													:props="{ checkStrictly: true, emitPath: false, value: 'id', label: 'name', expandTrigger: 'hover' }"
													:placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.system.org')})"
													class="w100"
													v-model="state.ruleForm.extOrgIdList[k].orgId"
												>
													<template #default="{ node, data }">
														<span>{{ data.name }}</span>
														<span v-if="!node.isLeaf"> ({{ data.children.length }}) </span>
													</template>
												</el-cascader>
											</el-form-item>
										</el-col>
										<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
											<el-form-item :label="$t('lz.entity.system.pos')" :prop="`extOrgIdList[${k}].posId`" :rules="[{ required: true, message: $t('lz.validation.base.required',{PropertyName: $t('lz.entity.system.pos')}), trigger: 'blur' }]">
												<el-select v-model="state.ruleForm.extOrgIdList[k].posId" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.system.pos')})" class="w100">
													<el-option v-for="d in state.posData" :key="d.id" :label="d.name" :value="d.id" />
												</el-select>
											</el-form-item>
										</el-col>
									</el-row>
								</template>
								<el-empty :image-size="70" :description="$t('lz.label.base.noData')" v-else></el-empty>
							</el-col>
						</el-row>
					</el-form>
				</el-tab-pane>
				<el-tab-pane :label="$t('lz.label.base.otherInfo')" style="height: 550px; overflow-y: auto; overflow-x: hidden">
					<el-form :model="state.ruleForm" label-width="auto" label-position="left">
						<el-row :gutter="10">
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.base.dateOfBirth')" prop="birthday">
									<DatePicker v-model="state.ruleForm.dateOfBirth" type="date" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.base.dateOfBirth')})" format="DD-MM-YYYY"
									value-format="YYYY-MM-DD" class="w100" size="small" :maxDate="new Date()" />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.base.gender')" prop="gender">
									<el-radio-group v-model="state.ruleForm.gender" v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysGenderType)" :key="item.value">
										<el-radio :value="item.code" class="mr5">{{ item.name }}</el-radio>
									</el-radio-group>

								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="$t('lz.entity.base.address')">
									<el-input v-model="state.ruleForm.address" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.address')})" clearable type="textarea" />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="$t('lz.entity.base.remark')">
									<el-input v-model="state.ruleForm.remark" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.remark')})" clearable type="textarea" />
								</el-form-item>
							</el-col>
						</el-row>
					</el-form>
				</el-tab-pane>
			</el-tabs>
			<template #footer>
				<span class="dialog-footer">
					<el-button @click="cancel">{{ $t('lz.button.base.cancel') }}</el-button>
					<el-button type="primary" @click="submit">{{ $t('lz.button.base.confirm') }}</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="sysEditUser">
import { reactive, ref } from 'vue';
import { storeToRefs } from 'pinia';
import { useUserInfo } from '/@/stores/userInfo';
import  DatePicker from 'primevue/datepicker';
import { getAPI } from '/@/utils/axios-utils';
import { SysPosApi, SysUserApi, SysAccountApi } from '/@/api-services/system';
import { SysOrg, UpdateUserInput, PosOutput } from '/@/api-services/system';
import {OperateType } from '/@/types/constants';
import { ElMessage } from 'element-plus';
import { BaseConfigTypeEnum, StatusTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;

import { useI18n } from 'vue-i18n';
import { useNow } from '@vueuse/core';
import { emailRegex, phoneRegex } from '/@/types/validators';
const { t: $t } = useI18n();

interface Props {
	/** the type of operation */
	operateType: OperateType;
	title: String,
	orgData: Array<SysOrg>
}

const props = defineProps<Props>();
const now = useNow();  // ChuongLP: Dùng để check không cho chọn sinh nhật > now (26-03-2026)

const emits = defineEmits(['handleQuery']);
const ruleFormRef = ref();
const storesUserInfo = useUserInfo();
const { userInfos } = storeToRefs(storesUserInfo);
const state = reactive({
	loading: false,
	isShowDialog: false,
	selectedTabName: '0', // Selected tab page
	ruleForm: {} as UpdateUserInput,
	posData: [] as Array<PosOutput>, // Position data
});

// Open dialog
// Danh mục chức vụ tải mỗi lần mở (không tải sẵn khi vào trang), song song với dữ liệu của bản ghi.
// Trang gốc còn tải danh sách vai trò nhưng biểu mẫu người dùng không dùng, nên đã bỏ.
const openDialog = async (row: any) => {
	ruleFormRef.value?.resetFields();

	state.selectedTabName = '0'; // Reset to the first tab page
	state.ruleForm = JSON.parse(JSON.stringify(row));
	state.loading = true;
	try {
		const isExisting = row.id != undefined;
		const [resPos, resRole, resExtOrg] = await Promise.all([
			getAPI(SysPosApi).apiSystemSysposListGet(),
			isExisting ? getAPI(SysAccountApi).apiSystemSysaccountOwnrolelistGet(row.id) : undefined,
			isExisting ? getAPI(SysUserApi).apiSystemSysuserOwnextorglistGet(row.id) : undefined,
		]);
		state.posData = resPos.data.result ?? [];
		if (isExisting) {
			state.ruleForm.roleIdList = resRole?.data.result;
			state.ruleForm.extOrgIdList = resExtOrg?.data.result;
		} else state.ruleForm.accountType = '333'; // Default regular account type
	} finally {
		state.loading = false;
	}
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
			await getAPI(SysUserApi).apiSystemSysuserUpdatePost(state.ruleForm);
		}
		//Copy or Add
		else if (props.operateType == 'copy' || props.operateType == 'add') {
			state.ruleForm.id = '';
			await getAPI(SysUserApi).apiSystemSysuserAddPost(state.ruleForm);

		}
		ElMessage.success($t('lz.message.base.saveSuccess'));
		closeDialog();
	});
};

// Thêm hàm này để chặn các ngày trong tương lai
const disabledDate = (time: Date) => {
	// Trả về true nếu thời gian chọn lớn hơn thời gian hiện tại
	return time.getTime() > now.value.getTime();
}

// Add affiliated organization row
const addExtOrgRow = () => {
	if (state.ruleForm.extOrgIdList == undefined) state.ruleForm.extOrgIdList = [];
	state.ruleForm.extOrgIdList?.push({});
};

// Delete affiliated organization row
const deleteExtOrgRow = (k: number) => {
	state.ruleForm.extOrgIdList?.splice(k, 1);
};

// Export object
defineExpose({ openDialog });
</script>
