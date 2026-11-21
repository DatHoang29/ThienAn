<template>
	<div class="edit-packet-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="!state.isProcessing" :show-close="!state.isProcessing" :close-on-press-escape="!state.isProcessing" width="700px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<el-form :model="state.editForm" :disabled="state.isProcessing" ref="editFormRef" label-width="auto" label-position="left">
				<el-alert v-if="state.usageCheckFailed" :title="$t('lz.message.sharedataDataSource.usageCheckFailed')" type="error" :closable="false" class="mb20" />
				<el-alert v-if="props.operateType === 'edit' && state.identityLocked" :title="$t('lz.label.sharedataDataSource.packetIdentityLocked')" type="info" :closable="false" class="mb20" />
				<el-alert v-if="props.operateType === 'edit' && state.statusLocked" :title="$t('lz.label.sharedataDataSource.packetStatusLocked')" type="warning" :closable="false" class="mb20" />
				<el-row :gutter="10">
					<!-- Gói tin đã được dùng: khóa Mã/Tên, các thông tin mô tả vẫn sửa được theo BE. -->
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item
							:label="$t('lz.entity.base.code')"
							prop="code"
							:rules="[
								{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.code') }), trigger: 'blur' },
								{ max: EntityFieldLength.Length128, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.base.code'), MaxLength: EntityFieldLength.Length128 }), trigger: 'blur' },
							]"
						>
							<el-input
								v-model="state.editForm.code"
								:disabled="props.operateType === 'edit' && state.identityLocked"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.code') })"
								:maxlength="EntityFieldLength.Length128"
								show-word-limit
								clearable
							/>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item
							:label="$t('lz.entity.base.name')"
							prop="name"
							:rules="[
								{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.name') }), trigger: 'blur' },
								{ max: EntityFieldLength.Length128, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.base.name'), MaxLength: EntityFieldLength.Length128 }), trigger: 'blur' },
							]"
						>
							<el-input
								v-model="state.editForm.name"
								:disabled="props.operateType === 'edit' && state.identityLocked"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.name') })"
								:maxlength="EntityFieldLength.Length128"
								show-word-limit
								clearable
							/>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item
							:label="$t('lz.entity.sharedataDataSource.packetKindOrderNo')"
							prop="orderNo"
							:rules="[
								{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.sharedataDataSource.packetKindOrderNo') }), trigger: 'change' },
							]"
						>
							<el-select
								v-model="state.editForm.orderNo"
								:disabled="props.operateType === 'edit'"
								:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.sharedataDataSource.packetKindOrderNo') })"
								class="w100"
							>
								<el-option
									v-for="item in packetKindOptions"
									:key="item.value"
									:label="item.label"
									:value="item.value"
								/>
							</el-select>
							<div class="packet-order-hint">{{ $t('lz.label.sharedataDataSource.packetKindOrderNoDesc') }}</div>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.base.status')" prop="status">
							<el-radio-group v-model="state.editForm.status">
								<!-- Còn đăng ký Active thì không cho chọn Tắt (BE chặn ESH-2307) -->
								<el-radio
									v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)"
									:key="item.code"
									:value="parseInt(item.code)"
									:disabled="state.statusLocked && parseInt(item.code) === StatusEnum.NUMBER_0"
									class="mr5"
								>{{ item.name }}</el-radio>
							</el-radio-group>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item
							:label="$t('lz.entity.base.remark')"
							prop="remark"
							:rules="[
								{
									max: EntityFieldLength.Length256,
									message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.base.remark'), MaxLength: EntityFieldLength.Length256 }),
									trigger: 'blur',
								},
							]"
						>
							<el-input
								v-model="state.editForm.remark"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.remark') })"
								type="textarea"
								rows="2"
								:maxlength="EntityFieldLength.Length256"
								show-word-limit
								clearable
							/>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
			<template #footer>
				<span class="dialog-footer">
					<el-button @click="cancel" :disabled="state.isProcessing">{{ $t('lz.button.base.cancel') }}</el-button>
					<el-button type="primary" :loading="state.isProcessing" :disabled="state.isProcessing" @click="submit">{{ $t('lz.button.base.confirm') }}</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="eshEditPacket">
import { computed, nextTick, reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';

import { getAPI } from '/@/utils/axios-utils';
import { OperateType } from '/@/types/constants';

import { ShareDataPacketApi, StatusEnum } from '/@/api-services/shareData';
import { ShareDataUpdatePacketInput } from '/@/api-services/shareData';

import { EntityFieldLength } from '/@/types/enums/baseEnum';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import { hasActiveSubscription, isPacketInUse } from '/@/utils/sharedata/packetUsage';
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const { t: $t } = useI18n();

const editFormRef = ref();
interface Props {
	operateType: OperateType;
	title: string;
	refreshGrid?: () => Promise<void> | void;
}
const props = defineProps<Props>();
const emits = defineEmits(['handleQuery']);
const state = reactive({
	isShowDialog: false,
	isProcessing: false,
	identityLocked: false,
	statusLocked: false,
	usageCheckFailed: false,
	originalCode: '',
	originalName: '',
	originalStatus: undefined as StatusEnum | undefined,
	editForm: {} as ShareDataUpdatePacketInput,
});

const openDialog = async (row: any, identityLocked = false) => {
	// Trang cha vừa gán operateType rồi gọi ngay: chờ prop cập nhật, không thì đọc phải loại thao tác của lần trước
	await nextTick();
	editFormRef.value?.resetFields();
	state.editForm = JSON.parse(JSON.stringify(row));
	if (props.operateType === 'add' || state.editForm.orderNo == null || state.editForm.orderNo === '') {
		state.editForm.orderNo = 101;
	}
	state.identityLocked = identityLocked;
	state.usageCheckFailed = false;
	state.originalCode = row.code ?? '';
	state.originalName = row.name ?? '';
	state.originalStatus = row.status;
	state.statusLocked = false;
	state.isShowDialog = true;
	if (props.operateType === 'edit' && row.id && row.status !== StatusEnum.NUMBER_0) {
		hasActiveSubscription(row.id)
			.then((locked) => { state.statusLocked = locked; })
			.catch(() => { /* Lỗi API đã được interceptor hiển thị; BE vẫn chặn khi lưu. */ });
	}
};

const closeDialog = async () => {
	try {
		if (props.refreshGrid) {
			await props.refreshGrid();
		} else {
			emits('handleQuery');
		}
	} finally {
		state.isShowDialog = false;
	}
};

const cancel = () => {
	if (state.isProcessing) return;
	state.isShowDialog = false;
};

const submit = () => {
	if (state.isProcessing) return;
	editFormRef.value.validate(async (valid: boolean) => {
		if (!valid || state.isProcessing) return;

		try {
			state.isProcessing = true;

			//Edit
			if (props.operateType == 'edit') {
				state.usageCheckFailed = false;
				try {
					state.identityLocked = await isPacketInUse(state.editForm.id!);
				} catch {
					state.usageCheckFailed = true;
					return;
				}
				if (state.identityLocked && ((state.editForm.code ?? '').trim() !== state.originalCode
					|| (state.editForm.name ?? '').trim() !== state.originalName)) {
					ElMessage.warning($t('lz.label.sharedataDataSource.packetIdentityLocked'));
					// Khôi phục định danh gốc để người dùng vẫn lưu được phần mô tả.
					state.editForm.code = state.originalCode;
					state.editForm.name = state.originalName;
					return;
				}
				// Chuyển sang Tắt: kiểm lại đăng ký Active ngay lúc lưu (cờ khoá lúc mở dialog có thể chưa kịp về).
				if (state.originalStatus !== StatusEnum.NUMBER_0 && state.editForm.status === StatusEnum.NUMBER_0
					&& await hasActiveSubscription(state.editForm.id!)) {
					state.statusLocked = true;
					state.editForm.status = state.originalStatus;
					ElMessage.warning($t('lz.label.sharedataDataSource.packetStatusLocked'));
					return;
				}
				await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketUpdatePost(state.editForm);
			}
			//Copy or Add
			else if (props.operateType == 'copy' || props.operateType == 'add') {
				state.editForm.id = '';
				await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketAddPost(state.editForm);
			}

			await closeDialog();
			ElMessage.success($t('lz.message.base.processSuccess'));
		} catch (error) {
		} finally {
			state.isProcessing = false;
		}
	});
};

const packetKindOptions = computed(() => [
	{ value: 101, label: `101 · ${$t('lz.label.sharedataDataSource.packetKind101')}` },
	{ value: 102, label: `102 · ${$t('lz.label.sharedataDataSource.packetKind102')}` },
	{ value: 103, label: `103 · ${$t('lz.label.sharedataDataSource.packetKind103')}` },
	{ value: 104, label: `104 · ${$t('lz.label.sharedataDataSource.packetKind104')}` },
	{ value: 105, label: `105 · ${$t('lz.label.sharedataDataSource.packetKind105')}` },
	{ value: 106, label: `106 · ${$t('lz.label.sharedataDataSource.packetKind106')}` },
	{ value: 107, label: `107 · ${$t('lz.label.sharedataDataSource.packetKind107')}` },
	{ value: 108, label: `108 · ${$t('lz.label.sharedataDataSource.packetKind108')}` },
	{ value: 109, label: `109 · ${$t('lz.label.sharedataDataSource.packetKind109')}` },
	{ value: 110, label: `110 · ${$t('lz.label.sharedataDataSource.packetKind110')}` },
	{ value: 111, label: `111 · ${$t('lz.label.sharedataDataSource.packetKind111')}` },
]);

defineExpose({ openDialog });
</script>

<style lang="scss" scoped>
.packet-order-hint {
	font-size: 12px;
	color: var(--el-text-color-secondary);
	margin-top: 4px;
	line-height: 1.4;
}
</style>
