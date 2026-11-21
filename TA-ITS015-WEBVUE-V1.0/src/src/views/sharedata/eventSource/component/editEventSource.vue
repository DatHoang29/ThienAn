<!-- <template>
	<el-dialog v-model="state.isShow" :title="state.isEdit ? $t('lz.label.sharedataEventSource.editTitle') : $t('lz.label.sharedataEventSource.addTitle')" width="600px" draggable :close-on-click-modal="!state.loading" :close-on-press-escape="!state.loading" :show-close="!state.loading">
		<el-form :model="state.form" :rules="rules" ref="formRef" label-width="120px" label-position="right">
			<el-form-item :label="$t('lz.entity.sharedataEventSource.code')" prop="code"><el-input v-model="state.form.code" :placeholder="$t('lz.label.sharedataEventSource.code')" /></el-form-item>
			<el-form-item :label="$t('lz.entity.sharedataEventSource.name')" prop="name"><el-input v-model="state.form.name" /></el-form-item>
			<el-form-item :label="$t('lz.entity.sharedataEventSource.subject')" prop="subject"><el-input v-model="state.form.subject" :placeholder="$t('lz.label.sharedataEventSource.subject')" /></el-form-item>
			<el-form-item :label="$t('lz.entity.sharedataEventSource.datatypeCodeOptional')">
				<el-select v-model="state.form.datatypeCode" clearable filterable class="w100" :placeholder="$t('lz.label.sharedataEventSource.datatypeCode')">
					<el-option v-for="d in DATATYPES" :key="d.id" :label="datatypeLabel(d.id, $t)" :value="String(d.id)" />
				</el-select>
			</el-form-item>
			<el-form-item :label="$t('lz.entity.sharedataEventSource.description')"><el-input v-model="state.form.description" type="textarea" :rows="2" /></el-form-item>
		</el-form>
		<template #footer>
			<el-button @click="handleCancel">{{ $t('lz.button.base.cancel') }}</el-button>
			<el-button type="primary" :loading="state.loading" @click="submit">{{ $t('lz.button.base.save') }}</el-button>
		</template>
	</el-dialog>
</template>

<script lang="ts" setup name="eshEditEventSource">
import { reactive, ref } from 'vue';
import type { FormInstance, FormRules } from 'element-plus';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { notImplemented, DATATYPES, datatypeLabel } from '../types';
import type { EventSource, EventSourceInput } from '../types';

const { t: $t } = useI18n();

interface Emits { (e: 'handleQuery'): void }
const emit = defineEmits<Emits>();

const formRef = ref<FormInstance>();

const handleCancel = () => {
	state.isShow = false;
};

function defaultForm(): EventSourceInput & { id?: string } {
	return { code: '', name: '', subject: '', datatypeCode: '', description: '' };
}
const state = reactive({ isShow: false, isEdit: false, loading: false, form: defaultForm() as EventSourceInput & { id?: string } });

const rules: FormRules = {
	code: [{ required: true, message: $t('lz.validation.sharedataEventSource.codeRequired'), trigger: 'blur' }],
	name: [{ required: true, message: $t('lz.validation.sharedataEventSource.nameRequired'), trigger: 'blur' }],
	subject: [{ required: true, message: $t('lz.validation.sharedataEventSource.subjectRequired'), trigger: 'blur' }],
};

function openDialog(row?: EventSource) {
	if (row) {
		state.isEdit = true;
		state.form = JSON.parse(JSON.stringify(row));
	} else {
		state.isEdit = false;
		state.form = defaultForm();
	}
	state.isShow = true;
}

async function submit() {
	if (!formRef.value) return;
	await formRef.value.validate(async (valid) => {
		if (!valid) return;
		state.loading = true;
		try {
			if (state.isEdit) notImplemented();
			else notImplemented();
			ElMessage.success($t('lz.message.sharedataEventSource.saveSuccess'));
			state.isShow = false;
			emit('handleQuery');
		} catch (e: any) {
			ElMessage.error(e?.message ? $t(e.message) : $t('lz.message.sharedataEventSource.saveFailed'));
		} finally {
			state.loading = false;
		}
	});
}

defineExpose({ openDialog });
</script> -->
<template></template>