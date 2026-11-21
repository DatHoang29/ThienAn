<template>
	<div class="mail-preview" v-loading="state.loading">
		<div class="pv-head">
			<span>{{ $t('lz.label.emailSchedule.previewMail') }}</span>
			<el-tag v-if="isStale" size="small" type="warning">{{ $t('lz.label.emailSchedule.previewStale') }}</el-tag>
		</div>
		<div class="pv-subject">
			<div><b>{{ $t('lz.label.emailSchedule.subjectLabel') }}</b> {{ state.subject || '—' }}</div>
			<div v-if="to?.trim()">{{ $t('lz.label.emailSchedule.previewTo') }} {{ to }}</div>
			<div v-if="cc?.trim()">{{ $t('lz.label.emailSchedule.previewCc') }} {{ cc }}</div>
		</div>
		<div v-if="state.sendAt" class="hint">{{ $t('lz.label.emailSchedule.previewSendAt', { time: state.sendAt }) }}</div>
		<el-alert v-for="(warning, index) in state.warnings" :key="index" :title="warning" type="warning" :closable="false" show-icon />
		<el-alert v-if="state.error" :title="$t('lz.label.emailSchedule.previewFailed')" type="error" :closable="false" show-icon />
		<iframe v-if="state.html" class="pv-frame" title="Xem trước thư gửi" sandbox="" referrerpolicy="no-referrer" :srcdoc="state.html" />
	</div>
</template>

<script lang="ts" setup name="mailPreview">
import { computed, onBeforeUnmount, reactive } from 'vue';
import { useI18n } from 'vue-i18n';
import { useDateFormat } from '@vueuse/core';
import { getAPI } from '/@/utils/axios-utils';
import { DATE_TIME_FORMAT } from '/@/types/formats';
import { RptEmailScheduleApi } from '/@/api-services/mail';
import type { MailPreviewScheduleInput } from '/@/api-services/mail';

const props = defineProps<{
	form: MailPreviewScheduleInput;
	to?: string | null;
	cc?: string | null;
}>();
const { t: $t } = useI18n();
const state = reactive({ loading: false, error: false, subject: '', html: '', sendAt: '', warnings: [] as string[], previewedKey: '' });
const formKey = computed(() => JSON.stringify(props.form));
const isStale = computed(() => !!state.previewedKey && state.previewedKey !== formKey.value);
let seq = 0;

// Render with the same backend rules as sending; never write the result back into the draft.
const preview = async () => {
	const request = ++seq;
	const key = formKey.value;
	const payload: MailPreviewScheduleInput = JSON.parse(key);
	payload.items = (payload.items ?? []).filter((item) => !!item.reportCode);
	state.loading = true;
	state.error = false;
	try {
		const res = await getAPI(RptEmailScheduleApi).apiMailRptemailschedulePreviewPost(payload);
		if (request !== seq) return;
		const out = res.data.result;
		state.previewedKey = key;
		state.subject = out?.subject ?? '';
		state.html = out?.html ?? '';
		state.warnings = out?.warnings ?? [];
		state.sendAt = out?.sendAt ? useDateFormat(out.sendAt, DATE_TIME_FORMAT).value : '';
	} catch {
		if (request === seq) state.error = true;
	} finally {
		if (request === seq) state.loading = false;
	}
};
const reset = () => {
	seq++;
	Object.assign(state, { loading: false, error: false, subject: '', html: '', sendAt: '', warnings: [], previewedKey: '' });
};
onBeforeUnmount(() => { seq++; });
defineExpose({ preview, reset });
</script>

<style scoped lang="scss">
.mail-preview { display: flex; flex-direction: column; gap: 10px; min-height: 150px; }
.pv-head { display: flex; justify-content: space-between; font-weight: 600; color: var(--el-color-primary); }
.pv-subject { padding: 10px; background: var(--el-fill-color-light); border-radius: 4px; word-break: break-word; line-height: 1.6; }
.hint { font-size: 12px; color: var(--el-text-color-secondary); }
.pv-frame { width: 100%; height: 65vh; border: 1px solid var(--el-border-color); background: white; }
</style>
