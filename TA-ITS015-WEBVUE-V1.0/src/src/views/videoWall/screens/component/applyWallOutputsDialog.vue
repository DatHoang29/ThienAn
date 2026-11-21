<template>
	<!-- Áp dụng Bố cục gán cổng xuống bộ con: chạy thử (đọc thiết bị, so sánh) → chọn bộ → ghi → kết quả (service ITS.VideoWall qua NATS) -->
	<el-dialog v-model="state.visible" :title="$t('lz.label.videoWall.applyTitle')" width="860px" :close-on-click-modal="false" destroy-on-close>
		<div v-if="state.phase === 'reading' || state.phase === 'writing'" class="aw-loading">
			<el-icon class="is-loading"><ele-Loading /></el-icon>
			<span>{{ state.phase === 'reading' ? $t('lz.label.videoWall.applyReading') : $t('lz.label.videoWall.applyWriting') }}</span>
		</div>

		<el-alert v-else-if="state.error" type="error" show-icon :closable="false" :title="state.error" />

		<template v-else>
			<p v-if="state.phase === 'preview'" class="aw-hint">{{ $t('lz.label.videoWall.applyHint') }}</p>
			<div v-for="c in state.controllers" :key="c.controllerId" class="aw-ctrl">
				<div class="aw-ctrl-head">
					<el-checkbox v-if="state.phase === 'preview'" v-model="c.selected" :disabled="!c.success || !changed(c).length" />
					<b>{{ c.controllerName || c.controllerId }}</b>
					<span v-if="c.wallNo" class="aw-muted">{{ $t('lz.label.videoWall.wallNo', { no: c.wallNo }) }}</span>
					<el-tag v-if="!c.success" size="small" type="danger">{{ $t('lz.label.videoWall.applyFailed') }}</el-tag>
					<el-tag v-else-if="c.applied" size="small" type="success">{{ $t('lz.label.videoWall.applyDone') }}</el-tag>
					<el-tag v-else-if="!changed(c).length" size="small" type="info">{{ $t('lz.label.videoWall.applyNoChange') }}</el-tag>
					<span class="aw-spacer" />
					<el-button v-if="c.backupXml" link type="primary" size="small" icon="ele-Download" @click="downloadBackup(c)">{{ $t('lz.button.videoWall.downloadBackup') }}</el-button>
				</div>
				<div v-if="c.message" class="aw-msg" :class="{ 'is-error': !c.success }">{{ c.message }}</div>
				<el-table v-if="changed(c).length" :data="changed(c)" size="small" border class="aw-table">
					<el-table-column :label="$t('lz.entity.videoWall.outputPort')" prop="name" min-width="140" />
					<el-table-column :label="$t('lz.label.videoWall.applyChange')" width="140">
						<template #default="{ row }">
							<el-tag size="small" :type="changeTag(row.change)">{{ changeLabel(row.change) }}</el-tag>
						</template>
					</el-table-column>
					<el-table-column :label="$t('lz.label.videoWall.oldPosition')" width="150">
						<template #default="{ row }">{{ rectLabel(row.oldRect) }}</template>
					</el-table-column>
					<el-table-column :label="$t('lz.label.videoWall.newPosition')" width="150">
						<template #default="{ row }">{{ rectLabel(row.newRect) }}</template>
					</el-table-column>
				</el-table>
				<div v-if="unchangedCount(c)" class="aw-muted aw-unchanged">{{ $t('lz.label.videoWall.applyUnchangedCount', { count: unchangedCount(c) }) }}</div>
			</div>
		</template>

		<template #footer>
			<el-button @click="state.visible = false">{{ $t(state.phase === 'done' ? 'lz.button.base.close' : 'lz.button.base.cancel') }}</el-button>
			<el-button v-if="state.phase === 'preview'" type="danger" icon="ele-Upload" :disabled="!selectedIds.length" @click="write">
				{{ $t('lz.button.videoWall.writeToDevice') }} ({{ selectedIds.length }})
			</el-button>
		</template>
	</el-dialog>
</template>

<script lang="ts" setup name="videoWallApplyWallOutputsDialog">
import { computed, reactive } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { VwDeviceSetupApi } from '/@/api-services/videoWall';
import { useVideoWallCommandResult } from '/@/transporter/hooks/useVideoWallCommandResult';

/** Một cổng: vị trí trên thiết bị hiện tại → vị trí theo Bố cục gán cổng (VwWallOutputChange phía service). */
interface WallOutputChange {
	outputId?: string;
	deviceOutputId: number;
	name?: string;
	change: 'unchanged' | 'moved' | 'added' | 'removed';
	oldRect?: number[] | null;
	newRect?: number[] | null;
}
interface ControllerResult {
	controllerId: string;
	controllerName?: string;
	wallNo?: number;
	success: boolean;
	applied: boolean;
	message?: string;
	changes: WallOutputChange[];
	backupXml?: string;
	selected: boolean;
}

const emit = defineEmits<{ (e: 'applied'): void }>();
const { t: $t } = useI18n();
const { waitFor } = useVideoWallCommandResult();

const state = reactive({
	visible: false,
	phase: 'reading' as 'reading' | 'preview' | 'writing' | 'done',
	error: '',
	controllers: [] as ControllerResult[],
});

const changed = (c: ControllerResult) => c.changes.filter((x) => x.change !== 'unchanged');
const unchangedCount = (c: ControllerResult) => c.changes.length - changed(c).length;
const selectedIds = computed(() => state.controllers.filter((c) => c.selected).map((c) => c.controllerId));

const changeLabel = (change: string) =>
	({ moved: $t('lz.label.videoWall.changeMoved'), added: $t('lz.label.videoWall.changeAdded'), removed: $t('lz.label.videoWall.changeRemoved') } as Record<string, string>)[change] ?? change;
const changeTag = (change: string) => (change === 'removed' ? 'danger' : change === 'added' ? 'success' : 'warning');
/** rect uniform [x, y, w, h] trong tường bộ con ⇒ "C{cột}·H{hàng}" (1 ô = w); không có ⇒ ngoài tường. */
const rectLabel = (rect?: number[] | null) => {
	if (!rect) return $t('lz.label.videoWall.notPlaced');
	const unit = rect[2] > 0 ? rect[2] : 1920;
	return `C${Math.floor(rect[0] / unit) + 1}·H${Math.floor(rect[1] / unit) + 1}`;
};

/** Gửi lệnh rồi chờ kết quả service theo MessageId. */
const run = async (dryRun: boolean, controllerIds?: string[]) => {
	const res = await getAPI(VwDeviceSetupApi).apiVideowallVwdevicesetupApplywalloutputsPost({ dryRun, controllerIds: controllerIds ?? null });
	const accepted = res.data.result;
	if (!accepted?.accepted || !accepted.messageId) throw new Error($t('lz.message.videoWall.commandRejected'));
	try {
		const result = await waitFor(accepted.messageId, 60_000);
		const data = result.data ?? {};
		const controllers: ControllerResult[] = (data.controllers ?? []).map((c: any) => ({
			controllerId: c.controllerId,
			controllerName: c.controllerName,
			wallNo: c.wallNo,
			success: !!c.success,
			applied: !!c.applied,
			message: c.message,
			changes: c.changes ?? [],
			backupXml: c.backupXml,
			selected: !!c.success && (c.changes ?? []).some((x: any) => x.change !== 'unchanged'),
		}));
		if (!controllers.length && !result.success) throw new Error(result.errorMessage || $t('lz.label.videoWall.applyFailed'));
		return controllers;
	} catch (e: any) {
		throw new Error(e?.message === 'timeout' ? $t('lz.message.videoWall.serviceTimeout') : e?.message);
	}
};

/** Mở hộp thoại: chạy thử ngay (chỉ đọc thiết bị, không ghi). */
const open = async () => {
	Object.assign(state, { visible: true, phase: 'reading', error: '', controllers: [] });
	try {
		state.controllers = await run(true);
		state.phase = 'preview';
	} catch (e: any) {
		state.error = e?.message ?? String(e);
		state.phase = 'done';
	}
};

const write = async () => {
	const ids = selectedIds.value;
	try {
		await ElMessageBox.confirm($t('lz.message.videoWall.confirmWriteDevice', { count: ids.length }), $t('lz.message.base.warning'), {
			confirmButtonText: $t('lz.button.videoWall.writeToDevice'),
			cancelButtonText: $t('lz.button.base.cancel'),
			type: 'warning',
		});
	} catch {
		return;
	}
	state.phase = 'writing';
	try {
		const written = await run(false, ids);
		// Giữ kết quả của bộ không ghi (chưa chọn) như lúc chạy thử
		state.controllers = state.controllers.map((c) => written.find((w) => w.controllerId === c.controllerId) ?? { ...c, selected: false });
		state.phase = 'done';
		if (written.every((c) => c.success)) ElMessage.success($t('lz.message.base.saveSuccess'));
		emit('applied');
	} catch (e: any) {
		state.error = e?.message ?? String(e);
		state.phase = 'done';
	}
};

/** Tải cấu hình tường đọc được TRƯỚC khi ghi (để khôi phục tay nếu cần). */
const downloadBackup = (c: ControllerResult) => {
	const blob = new Blob([c.backupXml ?? ''], { type: 'application/xml' });
	const a = document.createElement('a');
	a.href = URL.createObjectURL(blob);
	a.download = `videowall-${c.controllerName || c.controllerId}-wall${c.wallNo ?? ''}-${Date.now()}.xml`;
	a.click();
	URL.revokeObjectURL(a.href);
};

defineExpose({ open });
</script>

<style scoped lang="scss">
.aw-loading {
	display: flex;
	align-items: center;
	justify-content: center;
	gap: 8px;
	padding: 40px 0;
	color: var(--el-text-color-secondary);
}
.aw-hint {
	margin: 0 0 10px;
	font-size: 13px;
	color: var(--el-text-color-secondary);
}
.aw-ctrl {
	padding: 10px 12px;
	margin-bottom: 10px;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 8px;
}
.aw-ctrl-head {
	display: flex;
	align-items: center;
	gap: 8px;
}
.aw-spacer {
	flex: 1;
}
.aw-muted {
	font-size: 12px;
	color: var(--el-text-color-secondary);
}
.aw-msg {
	margin-top: 6px;
	font-size: 12px;
	color: var(--el-text-color-regular);
	&.is-error {
		color: var(--el-color-danger);
	}
}
.aw-table {
	margin-top: 8px;
}
.aw-unchanged {
	margin-top: 6px;
}
</style>
