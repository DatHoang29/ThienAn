<template>
	<el-drawer v-model="visible" :title="$t('lz.label.sharedataAlertDetail.title')" size="520px" direction="rtl" destroy-on-close>
		<template v-if="row">
			<el-descriptions :column="1" border size="small">
				<el-descriptions-item :label="$t('lz.label.sharedataAlertDetail.occurredTime')">{{ fmt(row.occurredAt) }}</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.label.sharedataAlertDetail.severity')">
					<el-tag size="small" :type="Number(row.severity) === 1 ? 'danger' : 'warning'">{{ labelOf(SEVERITY_LIST, row.severity) }}</el-tag>
				</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.label.sharedataAlertDetail.source')">{{ labelOf(SOURCE_LIST, row.alertSource) }}</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.label.sharedataAlertDetail.alertCode')">{{ row.alertCode || '—' }}</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.label.sharedataAlertDetail.alertDesc')">{{ alertInfo?.name || '—' }}</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.label.sharedataAlertDetail.actionGuide')">{{ alertInfo?.remark || '—' }}</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.label.sharedataAlertDetail.content')">{{ row.message || '—' }}</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.label.sharedataAlertDetail.partner')">{{ row.partnerName || '—' }}</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.label.sharedataAlertDetail.datatype')">{{ row.datatypeId || '—' }}</el-descriptions-item>
				<el-descriptions-item v-if="row.sessionId" :label="$t('lz.label.sharedataAlertDetail.sessionId')">{{ row.sessionId }}</el-descriptions-item>
				<el-descriptions-item v-if="row.subscriptionId" :label="$t('lz.label.sharedataAlertDetail.subscriptionId')">{{ row.subscriptionId }}</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.label.sharedataAlertDetail.processingStatus')">
					<el-tag size="small" :type="row.acknowledged ? 'success' : 'warning'">{{ row.acknowledged ? $t('lz.label.sharedataAlertDetail.acknowledgedValue') : $t('lz.label.sharedataAlertDetail.notAcknowledgedValue') }}</el-tag>
				</el-descriptions-item>
				<el-descriptions-item v-if="row.acknowledged" :label="$t('lz.label.sharedataAlertDetail.ackBy')">{{ row.ackBy || '—' }}</el-descriptions-item>
				<el-descriptions-item v-if="row.acknowledged" :label="$t('lz.label.sharedataAlertDetail.ackAt')">{{ fmt(row.ackAt) }}</el-descriptions-item>
				<el-descriptions-item v-if="row.remark" :label="$t('lz.label.sharedataAlertDetail.remark')">{{ row.remark }}</el-descriptions-item>
			</el-descriptions>

			<div v-if="detailText" class="drawer-json">
				<div class="drawer-json__title">{{ $t('lz.label.sharedataAlertDetail.technicalDetail') }}</div>
				<pre class="drawer-json__pre">{{ detailText }}</pre>
			</div>
		</template>
		<el-empty v-else :description="$t('lz.label.base.noData')" />
	</el-drawer>
</template>

<script lang="ts" setup name="sharedataAlertDetailDrawer">
import { computed, ref } from 'vue';
import { useDateFormat } from '@vueuse/core';
import { DateTimeFormatEnum, BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import type { ShareDataPageAlertLogOutput } from '/@/api-services/shareData';
import { useI18n } from 'vue-i18n';
const { t: $t } = useI18n();

// Mức độ cảnh báo — BaseEnums.AlertSeverity: Warning = 0, Error = 1.
// TODO: chuyển sang bảng cấu hình dùng chung khi danh mục được seed.
const SEVERITY_LIST = computed(() => [
	{ code: 0, name: $t('lz.label.sharedataAlertLog.severityWarning') },
	{ code: 1, name: $t('lz.label.sharedataAlertLog.severityError') },
]);

// Nguồn phát sinh — BaseEnums.AlertSource: Session=0, Packet=1, Subscription=2, Protocol=3, Funnel=4.
const SOURCE_LIST = computed(() => [
	{ code: 0, name: $t('lz.label.sharedataAlertLog.sourceSession') },
	{ code: 1, name: $t('lz.label.sharedataAlertLog.sourcePacket') },
	{ code: 2, name: $t('lz.label.sharedataAlertLog.sourceSubscription') },
	{ code: 3, name: $t('lz.label.sharedataAlertLog.sourceProtocol') },
	{ code: 4, name: $t('lz.label.sharedataAlertLog.sourceFunnel') },
]);

const visible = ref(false);
const row = ref<ShareDataPageAlertLogOutput | null>(null);

/** Tra cứu thông tin mã lỗi ESH từ danh mục cấu hình hệ thống (sharedata_error_code). */
const alertInfo = computed(() => {
	const code = row.value?.alertCode;
	if (!code) return null;
	const item = useBaseConfig().getConfigItemByCode(BaseConfigTypeEnum.SharedataErrorCode, String(code));
	return item?.name ? item : null;
});

/** Tra nhãn của một enum số; trả '—' nếu không có giá trị hoặc không có trong danh mục. */
const labelOf = (list: { code: number; name: string }[], value: any): string => {
	if (value === null || value === undefined || value === '') return '—';
	return list.find((i) => i.code === Number(value))?.name ?? String(value);
};

/** Ngày + giờ đầy đủ; định dạng do useDateFormat lo, chỉ giữ chốt rỗng / không hợp lệ để trả '—'. */
const fmt = (value?: Date | string | null) => {
	if (!value) return '—';
	const d = new Date(value);
	return Number.isNaN(d.getTime()) ? '—' : useDateFormat(d, DateTimeFormatEnum.DateTimeDefault).value;
};

/** Khối JSON chi tiết do backend đính kèm. */
const detailText = computed(() => {
	const raw = row.value?.detailJson;
	if (!raw) return '';
	try {
		return JSON.stringify(JSON.parse(String(raw)), null, 2);
	} catch {
		return String(raw);
	}
});

function open(record: ShareDataPageAlertLogOutput) {
	row.value = record;
	visible.value = true;
}

defineExpose({ open });
</script>

<style scoped lang="scss">
:deep(.el-drawer__header) {
	margin-bottom: 0;
	padding: 14px 16px;
	border-bottom: 1px solid var(--el-border-color-lighter);
}

:deep(.el-drawer__body) {
	padding: 12px 16px 16px;
	background: var(--el-bg-color-page);
}

.drawer-json {
	margin-top: 14px;
}

.drawer-json__title {
	font-weight: 600;
	margin-bottom: 6px;
	color: var(--el-text-color-primary);
}

.drawer-json__pre {
	background: var(--el-fill-color-light);
	border: 1px solid var(--el-border-color-lighter);
	color: var(--el-text-color-primary);
	padding: 10px;
	border-radius: 4px;
	font-size: 12px;
	max-height: 300px;
	overflow: auto;
	white-space: pre-wrap;
	word-break: break-all;
}
</style>
