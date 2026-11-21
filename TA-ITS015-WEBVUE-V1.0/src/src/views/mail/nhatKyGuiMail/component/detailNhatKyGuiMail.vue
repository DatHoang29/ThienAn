<!--
	Chi tiết một lần gửi mail (RptEmailLog).
-->
<template>
	<el-drawer v-model="state.visible" :title="$t('lz.label.mailLog.detailTitle')" size="560px" destroy-on-close>
		<div v-loading="state.loading">
			<el-descriptions v-if="state.row" :column="1" border size="small" label-width="150px">
				<el-descriptions-item :label="$t('lz.entity.mailLog.occurredAt')">{{ formatTime(state.row.occurredAt) }}</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.entity.mailLog.status')">
					<el-tag v-if="state.row.status === SuccessEnum.Success" size="small" type="success" effect="plain">
						{{ $t('lz.label.mailLog.success') }}
					</el-tag>
					<el-tag v-else size="small" type="danger" effect="plain">{{ $t('lz.label.mailLog.fail') }}</el-tag>
				</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.entity.mailLog.triggerType')">
					<el-tag v-if="state.row.triggerType != null" size="small" effect="plain"
						:type="getConfigItemByCode(BaseConfigTypeEnum.MailTriggerType, String(state.row.triggerType))?.tagType">
						{{ getConfigItemByCode(BaseConfigTypeEnum.MailTriggerType, String(state.row.triggerType))?.name ?? $t('lz.label.mailLog.triggerUnknown') }}
					</el-tag>
				</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.entity.mailLog.schedule')">
					<template v-if="state.row.scheduleId">
						{{ state.row.scheduleCode }}<template v-if="state.row.scheduleName"> - {{ state.row.scheduleName }}</template>
					</template>
					<span v-else class="sub">{{ $t('lz.label.mailLog.noSchedule') }}</span>
				</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.entity.mailLog.subject')">{{ state.row.subject }}</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.entity.mailLog.recipients')"><div class="addr">{{ state.row.recipients }}</div></el-descriptions-item>
				<el-descriptions-item v-if="state.row.cc" label="CC"><div class="addr">{{ state.row.cc }}</div></el-descriptions-item>
				<el-descriptions-item v-if="state.row.bcc" label="BCC"><div class="addr">{{ state.row.bcc }}</div></el-descriptions-item>
				<el-descriptions-item :label="$t('lz.entity.mailLog.period')">
					<template v-if="state.row.periodFrom">{{ formatTime(state.row.periodFrom) }} – {{ formatTime(state.row.periodTo) }}</template>
				</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.entity.mailLog.templateCode')">{{ state.row.templateCode }}</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.entity.mailLog.fileName')"><div class="addr">{{ state.row.fileName }}</div></el-descriptions-item>
				<el-descriptions-item :label="$t('lz.entity.mailLog.byteSize')">{{ formatSize(state.row.byteSize) }}</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.entity.mailLog.attempts')">{{ state.row.attempts }}</el-descriptions-item>
				<!-- <el-descriptions-item :label="$t('lz.entity.mailLog.durationMs')">{{ state.row.durationMs }}</el-descriptions-item> -->
				<el-descriptions-item v-if="state.row.errorMessage" :label="$t('lz.entity.mailLog.errorMessage')">
					<div class="error">{{ state.row.errorMessage }}</div>
				</el-descriptions-item>
				<el-descriptions-item v-if="state.row.remark" :label="$t('lz.entity.mailLog.remark')">
					<div class="addr">{{ state.row.remark }}</div>
				</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.entity.mailLog.code')">{{ state.row.code }}</el-descriptions-item>
			</el-descriptions>
		</div>
	</el-drawer>
</template>

<script lang="ts" setup name="detailNhatKyGuiMail">
import { reactive } from 'vue';
import { useDateFormat } from '@vueuse/core';
import { getAPI } from '/@/utils/axios-utils';
import { DATE_TIME_FORMAT } from '/@/types/formats';
import { RptEmailLogApi } from '/@/api-services/mail';
import type { MailLogOutput } from '/@/api-services/mail';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';

const getConfigItemByCode = useBaseConfig().getConfigItemByCode;

/** BaseEnums.SuccessEnums của BE. */
const SuccessEnum = { Success: 1, Fail: 0 } as const;

const state = reactive({
	visible: false,
	loading: false,
	row: null as MailLogOutput | null,
});

const formatTime = (v?: Date | string | null) => (v ? useDateFormat(v, DATE_TIME_FORMAT).value : '');
const formatSize = (bytes?: number | null) => {
	if (bytes == null) return '';
	if (bytes < 1024) return `${bytes} B`;
	if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
	return `${(bytes / 1024 / 1024).toFixed(2)} MB`;
};

/** Mở drawer và nạp chi tiết theo ID. */
const open = async (id?: string | null) => {
	if (!id) return;
	state.visible = true;
	state.row = null;
	try {
		state.loading = true;
		const res = await getAPI(RptEmailLogApi).apiMailRptemaillogByidGet(id);
		state.row = res.data.result ?? null;
	} finally {
		state.loading = false;
	}
};

defineExpose({ open });
</script>

<style scoped lang="scss">
.sub {
	color: var(--el-text-color-secondary);
}

.addr {
	word-break: break-word;
}

.error {
	color: var(--el-color-danger);
	white-space: pre-wrap;
	word-break: break-word;
}
</style>
