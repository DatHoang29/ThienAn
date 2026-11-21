<template>
	<el-dialog v-model="state.isShow" :title="$t('lz.label.sharedataExportPreview.title')" width="720px" draggable>
		<div v-if="state.ctx" class="ex-head">
			<el-descriptions :column="3" size="small" border>
				<el-descriptions-item :label="$t('lz.entity.sharedataExportPreview.partner')">{{ state.ctx.partnerName || state.ctx.partnerId }}</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.entity.sharedataExportPreview.datatype')">{{ datatypeLabel(packetList, state.ctx.datatypeId) }}</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.label.sharedataExportPreview.previewRowCount')">{{ state.records.length }}</el-descriptions-item>
			</el-descriptions>
		</div>

		<el-alert v-if="state.error" type="warning" :closable="false" class="mt10" :title="state.error" />
		<el-alert v-if="state.missing.length" type="error" :closable="false" class="mt10"
			:title="$t('lz.label.sharedataExportPreview.missingRequiredFields', { fields: state.missing.join(', ') })" />

		<div class="ex-json mt10" v-loading="state.loading" :element-loading-text="$t('lz.label.base.loading')" v-if="!state.error">
			<pre>{{ prettyJson }}</pre>
		</div>

		<el-alert v-if="state.result" type="success" :closable="false" class="mt10">
			<template #title>
				<div><b>{{ $t('lz.label.sharedataExportPreview.exportedRecordInfo', { count: state.result.recordCount, size: state.result.byteSize }) }}</b></div>
				<div>{{ $t('lz.label.sharedataExportPreview.filePath') }} <code>{{ state.result.filePath }}</code></div>
				<div>{{ $t('lz.label.sharedataExportPreview.hash') }} <code>{{ state.result.hash }}</code></div>
			</template>
		</el-alert>

		<template #footer>
			<el-button @click="state.isShow = false">{{ $t('lz.button.base.close') }}</el-button>
			<el-button v-auth="'sharedataSharing:run'" type="primary" :loading="state.exporting" :disabled="state.loading || !!state.missing.length || !!state.error"
				@click="doExport">{{ $t('lz.button.sharedataExportPreview.export') }}</el-button>
		</template>
	</el-dialog>
</template>

<script lang="ts" setup name="sharedataExportPreview">
import { computed, onMounted, reactive, ref } from 'vue';
import { getAPI } from '/@/utils/axios-utils';
import { ShareDataPacketApi } from '/@/api-services/shareData';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import type { ShareDataSubscriptionOutput } from '/@/api-services/shareData';
import { datatypeLabel } from '/@/utils/sharedata/labelHelper';

const { t: $t } = useI18n();

/**
 * Xuất dữ liệu / xem trước — BACKEND CHƯA CÓ CONTROLLER.
 * Trước đây gọi qua stub `exportService`; nay khai báo tại chỗ để bỏ hẳn tầng service.
 * TODO(BE): bổ sung ShareDataExport/Preview + /Run rồi thay 2 hàm dưới bằng lời gọi thật.
 */
const EXPORT_PENDING_MESSAGE = computed(() => $t('lz.message.sharedataExportPreview.pendingBackend'));

/** Kết quả một lần xuất file bằng chứng. */
interface ExportResult {
	filePath?: string;
	hash?: string;
	recordCount?: number;
}

/**
 * Tên gói tin lấy thẳng từ danh sách gói tin (ShareDataPacket).
 * DatatypeId lưu ID gói tin, nên tra theo ID; không còn dùng danh mục shareData_type.
 */
const packetList = ref<any[]>([]);

const loadPacketList = async () => {
	if (packetList.value.length) return;
	try {
		const res = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketListGet();
		packetList.value = res.data.result ?? [];
	} catch {
		packetList.value = [];
	}
};

onMounted(loadPacketList);


interface Ctx { partnerId: string; partnerName?: string; datatypeId: string; subscriptionId?: string }

const state = reactive({
	isShow: false,
	loading: false,
	exporting: false,
	ctx: null as Ctx | null,
	records: [] as Record<string, unknown>[],
	missing: [] as string[],
	error: '' as string,
	result: null as ExportResult | null,
});

const prettyJson = computed(() => (state.records.length ? JSON.stringify(state.records, null, 2) : $t('lz.label.sharedataExportPreview.noPreviewData')));

async function openDialog(sub: ShareDataSubscriptionOutput) {
	state.ctx = {
		partnerId: sub.partnerId ?? '',
		partnerName: sub.partnerName ?? undefined,
		datatypeId: sub.datatypeId ?? '',
		subscriptionId: sub.id ?? undefined,
	};
	state.records = [];
	state.missing = [];
	state.result = null;
	// Chưa có API xem trước — báo ngay thay vì để trống, tránh hiểu là không có dữ liệu.
	state.error = EXPORT_PENDING_MESSAGE.value;
	state.loading = false;
	state.isShow = true;
}

async function doExport() {
	if (!state.ctx) return;
	ElMessage.error(EXPORT_PENDING_MESSAGE.value);
}

defineExpose({ openDialog });
</script>

<style scoped lang="scss">
.ex-json {
	max-height: 320px;
	overflow: auto;
	background: var(--el-fill-color-light);
	border-radius: 6px;
	padding: 8px 10px;
}
.ex-json pre {
	margin: 0;
	font-size: 12px;
	white-space: pre-wrap;
	word-break: break-word;
}
code {
	word-break: break-all;
}
</style>
