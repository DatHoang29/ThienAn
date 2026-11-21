<template>
	<!-- Bật / tắt màn: ScreenCtrl openAll / closeAll gửi tới bộ hiển thị (service ITS.VideoWall qua NATS, kết quả theo MessageId).
	     Thiết bị không bật/tắt được từng màn ⇒ chỉ theo bộ và toàn tường. -->
	<el-dialog v-model="state.visible" :title="$t('lz.label.videoWall.screenPowerTitle')" width="600px" :close-on-click-modal="false" destroy-on-close>
		<p class="sp-hint">{{ $t('lz.label.videoWall.screenPowerHint') }}</p>

		<!-- Toàn tường = mọi bộ hiển thị -->
		<div class="sp-row sp-row--all">
			<b>{{ $t('lz.label.videoWall.screenPowerWholeWall') }}</b>
			<span class="sp-spacer" />
			<el-button type="success" plain icon="ele-VideoPlay" :loading="state.busy === 'all:on'" :disabled="!!state.busy || !state.controllers.length" @click="run(true)">
				{{ $t('lz.button.videoWall.turnAllOn') }}
			</el-button>
			<el-button type="danger" plain icon="ele-SwitchButton" :loading="state.busy === 'all:off'" :disabled="!!state.busy || !state.controllers.length" @click="run(false)">
				{{ $t('lz.button.videoWall.turnAllOff') }}
			</el-button>
		</div>

		<div v-loading="state.loading">
			<el-empty v-if="!state.loading && !state.controllers.length" :image-size="60" :description="$t('lz.label.videoWall.noDisplayController')" />
			<div v-for="c in state.controllers" :key="c.id" class="sp-row">
				<div class="sp-name">
					<b>{{ c.name || c.code || c.id }}</b>
					<span class="sp-muted">{{ c.ip }}</span>
				</div>
				<el-tag v-if="c.result" size="small" :type="c.result.success ? 'success' : 'danger'">
					{{ c.result.success ? $t(c.result.on ? 'lz.label.videoWall.screenPowerOnDone' : 'lz.label.videoWall.screenPowerOffDone') : $t('lz.label.videoWall.screenPowerFailed') }}
				</el-tag>
				<span class="sp-spacer" />
				<el-button size="small" type="success" plain :loading="state.busy === `${c.id}:on`" :disabled="!!state.busy" @click="run(true, c)">
					{{ $t('lz.button.videoWall.turnOn') }}
				</el-button>
				<el-button size="small" type="danger" plain :loading="state.busy === `${c.id}:off`" :disabled="!!state.busy" @click="run(false, c)">
					{{ $t('lz.button.videoWall.turnOff') }}
				</el-button>
				<div v-if="c.result && !c.result.success && c.result.message" class="sp-msg">{{ c.result.message }}</div>
			</div>
		</div>

		<template #footer>
			<el-button @click="state.visible = false">{{ $t('lz.button.base.close') }}</el-button>
		</template>
	</el-dialog>
</template>

<script lang="ts" setup name="videoWallScreenPowerDialog">
import { reactive } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { VwControllerApi, VwDeviceSetupApi } from '/@/api-services/videoWall';
import { useVideoWallCommandResult } from '/@/transporter/hooks/useVideoWallCommandResult';

interface PowerResult {
	on: boolean;
	success: boolean;
	message?: string;
}
interface DisplayController {
	id: string;
	code?: string | null;
	name?: string | null;
	ip?: string | null;
	result?: PowerResult;
}

/** Màn cắm ở bộ hiển thị / bộ độc lập (cùng điều kiện với service ScreenPower). */
const DISPLAY_ROLES = ['display', 'standalone'];

const { t: $t } = useI18n();
const { waitFor } = useVideoWallCommandResult();

const state = reactive({
	visible: false,
	loading: false,
	/** Nút đang chạy: "all:on" | "all:off" | "{controllerId}:on" | "{controllerId}:off" */
	busy: '',
	controllers: [] as DisplayController[],
});

const open = async () => {
	Object.assign(state, { visible: true, loading: true, busy: '', controllers: [] });
	try {
		const res = await getAPI(VwControllerApi).apiVideowallVwcontrollerListGet();
		state.controllers = (res.data.result ?? [])
			.filter((c) => c.id && c.ip && DISPLAY_ROLES.includes(c.role ?? ''))
			.sort((a, b) => (a.code ?? '').localeCompare(b.code ?? ''))
			.map((c) => ({ id: c.id!, code: c.code, name: c.name, ip: c.ip }));
	} finally {
		state.loading = false;
	}
};

/** target trống ⇒ toàn tường (mọi bộ hiển thị). Tắt ⇒ hỏi xác nhận vì tường tối đến khi bật lại. */
const run = async (on: boolean, target?: DisplayController) => {
	const label = target ? target.name || target.code || target.id : $t('lz.label.videoWall.screenPowerWholeWall');
	if (!on) {
		try {
			await ElMessageBox.confirm($t('lz.message.videoWall.confirmScreenOff', { target: label }), $t('lz.message.base.warning'), {
				confirmButtonText: $t('lz.button.videoWall.turnOff'),
				cancelButtonText: $t('lz.button.base.cancel'),
				type: 'warning',
			});
		} catch {
			return;
		}
	}

	state.busy = `${target?.id ?? 'all'}:${on ? 'on' : 'off'}`;
	try {
		const res = await getAPI(VwDeviceSetupApi).apiVideowallVwdevicesetupScreenpowerPost({ on, controllerIds: target ? [target.id] : null });
		const accepted = res.data.result;
		if (!accepted?.accepted || !accepted.messageId) throw new Error($t('lz.message.videoWall.commandRejected'));

		const result = await waitFor(accepted.messageId, 150_000); // openAll/closeAll ~12 s / 8 màn, toàn tường chạy lần lượt 3 bộ
		const items: any[] = result.data?.controllers ?? [];
		if (!items.length && !result.success) throw new Error(result.errorMessage || $t('lz.label.videoWall.screenPowerFailed'));
		for (const item of items) {
			const c = state.controllers.find((x) => x.id === item.controllerId);
			if (c) c.result = { on, success: !!item.success, message: item.message };
		}
		if (result.success) ElMessage.success($t(on ? 'lz.label.videoWall.screenPowerOnDone' : 'lz.label.videoWall.screenPowerOffDone'));
		else ElMessage.error(result.errorMessage || $t('lz.label.videoWall.screenPowerFailed'));
	} catch (e: any) {
		ElMessage.error(e?.message === 'timeout' ? $t('lz.message.videoWall.serviceTimeout') : e?.message ?? String(e));
	} finally {
		state.busy = '';
	}
};

defineExpose({ open });
</script>

<style scoped lang="scss">
.sp-hint {
	margin: 0 0 12px;
	color: var(--el-text-color-secondary);
	font-size: 13px;
}

.sp-row {
	display: flex;
	flex-wrap: wrap;
	align-items: center;
	gap: 8px;
	padding: 10px 12px;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 8px;

	& + & {
		margin-top: 8px;
	}

	&--all {
		margin-bottom: 12px;
		background: var(--el-fill-color-lighter);
	}
}

.sp-name {
	display: flex;
	flex-direction: column;
	min-width: 0;
}

.sp-muted {
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.sp-spacer {
	flex: 1;
}

.sp-msg {
	flex-basis: 100%;
	font-size: 12px;
	color: var(--el-color-danger);
}

/* Element Plus tự thêm margin-left giữa 2 el-button liền kề ⇒ để 'gap' quyết định khoảng cách */
:deep(.el-button + .el-button) {
	margin-left: 0;
}
</style>
