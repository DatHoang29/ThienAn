<template>
	<el-dialog
		v-model="visible"
		width="600px"
		destroy-on-close
		draggable
		:close-on-click-modal="!props.saving"
		:close-on-press-escape="!props.saving"
		:show-close="!props.saving"
		:before-close="handleBeforeClose"
		class="field-edit-dialog"
	>
		<template #header>
			<div class="fed-header">
				<el-icon class="mr5"><ele-Edit /></el-icon>
				<span>{{ dialogTitle }}</span>
			</div>
		</template>

		<el-form :model="form" :disabled="Boolean(props.saving)" label-width="auto" label-position="left">
			<el-row :gutter="10">
				<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
					<el-form-item :label="$t('lz.entity.sharedataDataSource.aliasFieldKey')" prop="key">
						<el-input v-model="form.key" disabled />
					</el-form-item>
				</el-col>
				<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
					<el-form-item :label="$t('lz.entity.sharedataDataSource.fieldName')" prop="label">
						<el-input
							v-model="form.label"
							:placeholder="form.key"
							:maxlength="EntityFieldLength.Length128"
							show-word-limit
							clearable
						/>
					</el-form-item>
				</el-col>
				<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
					<el-form-item :label="$t('lz.label.sharedataDataSource.fieldGroup')" prop="group">
						<div class="field-group-input-wrap">
							<el-select
								v-model="form.group"
								class="field-group-select"
								filterable
								allow-create
								default-first-option
								clearable
								:placeholder="$t('lz.placeholder.sharedataDataSource.selectOrCreateGroup')"
								@query-change="onGroupQueryChange"
								@change="onGroupChange"
								@blur="handleGroupBlur"
							>
								<template #prefix>
									<el-icon><ele-Folder /></el-icon>
								</template>
								<el-option v-for="g in groupOptions" :key="g" :label="g" :value="g">
									<div class="field-group-option">
										<el-icon class="mr5"><ele-Folder /></el-icon>
										<span>{{ g }}</span>
									</div>
								</el-option>
							</el-select>
							<el-tooltip :content="$t('lz.button.sharedataDataSource.addNewGroupTooltip')" placement="top">
								<el-button
									type="primary"
									plain
									class="field-add-group-btn"
									:icon="'ele-FolderAdd'"
									:aria-label="$t('lz.button.sharedataDataSource.addNewGroupTooltip')"
									@click="handlePromptAddGroup"
								/>
							</el-tooltip>
						</div>
					</el-form-item>
				</el-col>
				<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
					<el-form-item :label="$t('lz.entity.sharedataDataSource.fieldType')" prop="type">
						<el-select v-model="form.type" class="w100">
							<el-option v-for="t in typeOptions(form.type)" :key="t" :label="t" :value="t" />
						</el-select>
					</el-form-item>
				</el-col>
				<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
					<el-form-item :label="$t('lz.entity.sharedataDataSource.isRequired')" prop="required">
						<el-checkbox v-model="form.required">{{ $t('lz.label.sharedataDataSource.required') }}</el-checkbox>
					</el-form-item>
				</el-col>
				<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
					<el-form-item :label="$t('lz.label.sharedataDataSource.fieldRemark')" prop="remark">
						<el-input
							v-model="form.remark"
							type="textarea"
							:rows="2"
							:maxlength="EntityFieldLength.Length256"
							show-word-limit
							clearable
							:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.label.sharedataDataSource.fieldRemark') })"
						/>
					</el-form-item>
				</el-col>
			</el-row>
		</el-form>

		<template #footer>
			<span class="dialog-footer">
				<el-button :disabled="Boolean(props.saving)" @click="handleCancel">{{ $t('lz.button.base.cancel') }}</el-button>
				<el-button type="primary" :disabled="Boolean(props.saving)" :loading="Boolean(props.saving)" @click="handleSave">
					{{ $t('lz.button.base.save') }}
				</el-button>
			</span>
		</template>
	</el-dialog>
</template>

<script lang="ts" setup name="sharedataFieldEditDialog">
import { computed, reactive, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessageBox } from 'element-plus';
import type { SqlFieldEditInput, SqlFieldItem } from '/@/api-services/shareData/models';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { EntityFieldLength } from '/@/types/enums/baseEnum';
import { useBaseConfig } from '/@/stores/baseConfig';
import { useDialogCloseGuard } from '/@/hooks/useDialogCloseGuard';

const { t: $t } = useI18n();
const { snapshot, beforeClose } = useDialogCloseGuard();

const props = defineProps<{
	modelValue: boolean;
	packetCode: string;
	partnerName?: string | null;
	field?: SqlFieldItem | null;
	saving?: boolean;
	groups?: string[];
}>();

const emit = defineEmits<{
	(e: 'update:modelValue', val: boolean): void;
	(e: 'save', edits: SqlFieldEditInput[]): void;
}>();

const visible = computed({
	get: () => props.modelValue,
	set: (val: boolean) => emit('update:modelValue', val),
});

interface EditableFieldRow {
	key: string;
	label: string;
	type: string;
	dbType: string;
	source: string;
	required: boolean;
	skip: boolean;
	group: string;
	remark: string;
}

const form = reactive<EditableFieldRow>({
	key: '',
	label: '',
	type: '',
	dbType: '',
	source: '',
	required: false,
	skip: false,
	group: '',
	remark: '',
});

const customGroups = ref<string[]>([]);
const groupQuery = ref('');

const onGroupQueryChange = (q: string) => {
	groupQuery.value = q || '';
};

const onGroupChange = (val: string) => {
	groupQuery.value = val || '';
};

const handleGroupBlur = () => {
	if (!form.group && groupQuery.value?.trim()) {
		form.group = groupQuery.value.trim();
	}
};

const handlePromptAddGroup = async () => {
	try {
		const { value } = await ElMessageBox.prompt(
			$t('lz.prompt.sharedataDataSource.inputNewGroupName'),
			$t('lz.button.sharedataDataSource.addNewGroupTooltip'),
			{
				confirmButtonText: $t('lz.button.base.confirm'),
				cancelButtonText: $t('lz.button.base.cancel'),
				inputPattern: /\S+/,
				inputErrorMessage: $t('lz.validation.base.required', { PropertyName: $t('lz.label.sharedataDataSource.fieldGroup') }),
				inputValue: groupQuery.value || '',
			}
		);
		const newGroup = (value || '').trim();
		if (newGroup) {
			form.group = newGroup;
			groupQuery.value = newGroup;
			if (!customGroups.value.includes(newGroup)) {
				customGroups.value.push(newGroup);
			}
		}
	} catch {
		// Huỷ bỏ prompt
	}
};

const dialogTitle = computed(() => {
	const base = props.partnerName
		? $t('lz.title.sharedataDataSource.editFieldsPartner', { packet: props.packetCode || '', partner: props.partnerName })
		: $t('lz.title.sharedataDataSource.editFieldsCommon', { packet: props.packetCode || '' });
	return form.key ? `${base} · ${form.key}` : base;
});

const groupOptions = computed(() => {
	const list: string[] = [];
	const seen = new Set<string>();
	const add = (v?: string | null) => {
		const s = (v || '').trim();
		if (s && !seen.has(s.toLowerCase())) {
			seen.add(s.toLowerCase());
			list.push(s);
		}
	};
	(props.groups || []).forEach(add);
	customGroups.value.forEach(add);
	add(form.group);
	add(groupQuery.value);
	return list;
});

/** Danh sách kiểu lấy từ cấu hình hệ thống sharedata_value_type (giống luồng cũ và màn Ánh xạ); thêm kiểu đang có của trường nếu cấu hình chưa có (vd. number do bộ dò sinh). */
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const typeOptions = (current?: string) => {
	const codes = (getConfigDataByCode(BaseConfigTypeEnum.SharedataValueType) || []).map((c: any) => String(c.code));
	return current && !codes.some((c) => c.toLowerCase() === current.toLowerCase()) ? [current, ...codes] : codes;
};

watch(
	() => [props.modelValue, props.field] as const,
	([isOpen, f]) => {
		if (isOpen && f) {
			form.key = f.key ?? '';
			form.label = f.label ?? '';
			form.type = f.type ?? '';
			form.dbType = f.dbType ?? '';
			form.source = f.source ?? '';
			form.required = Boolean(f.required);
			form.skip = Boolean(f.skip);
			form.group = f.group ?? '';
			form.remark = f.remark ?? '';
			groupQuery.value = f.group ?? '';
			customGroups.value = [];
			snapshot(form);
		}
	},
	{ immediate: true }
);

const handleBeforeClose = (done: () => void) => {
	void beforeClose(form, props.saving, done);
};

const handleCancel = () => {
	void beforeClose(form, props.saving, () => {
		visible.value = false;
	});
};

const handleSave = () => {
	if (props.saving || !form.key) return;
	const finalGroup = (form.group || groupQuery.value || '').trim() || null;
	const edit: SqlFieldEditInput = {
		key: form.key,
		label: (form.label || '').trim() || null,
		required: Boolean(form.required),
		skip: Boolean(form.skip),
		group: finalGroup,
		remark: (form.remark || '').trim() || null,
		type: form.type || null,
	};
	emit('save', [edit]);
};
</script>

<style scoped lang="scss">
.fed-header {
	display: flex;
	align-items: center;
	color: #fff;
}

.field-group-input-wrap {
	display: flex;
	align-items: center;
	width: 100%;
	gap: 8px;
}

.field-group-select {
	flex: 1 1 auto;
	width: auto;
}

.field-add-group-btn {
	flex-shrink: 0;
	padding: 8px 12px;
}

.field-group-option {
	display: inline-flex;
	align-items: center;
	font-size: 13px;
}
</style>
