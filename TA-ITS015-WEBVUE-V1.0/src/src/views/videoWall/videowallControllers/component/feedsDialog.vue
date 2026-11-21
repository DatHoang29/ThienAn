<template>
	<!-- Cổng ra của bộ phân phối cấp tín hiệu cho bộ hiển thị nào (VwControllerOutput.FeedsControllerId) -->
	<el-dialog v-model="state.visible" draggable :close-on-click-modal="false" width="620px" :title="$t('lz.label.videoWall.feedsTitle', { name: state.controllerName })">
		<p class="feeds-hint">{{ $t('lz.label.videoWall.feedsHint') }}</p>
		<vxe-table :data="state.outputs" border="inner" stripe max-height="420" :loading="state.loading" :row-config="{ isHover: true }">
			<vxe-column type="seq" width="50" />
			<vxe-column :title="$t('lz.entity.videoWall.outputPort')" min-width="160">
				<template #default="{ row }">
					<div>{{ row.displayName || row.name }}</div>
					<small class="feeds-muted">#{{ row.deviceOutputId ?? '—' }} · {{ row.resolution || '—' }}</small>
				</template>
			</vxe-column>
			<vxe-column :title="$t('lz.label.videoWall.feedsTo')" min-width="220">
				<template #default="{ row }">
					<el-select :model-value="row.feedsControllerId ?? ''" clearable class="w100" :placeholder="$t('lz.label.videoWall.feedsNone')"
						v-auth="'videoWallOutput:update'" @change="(v: string) => save(row, v)">
						<el-option v-for="c in children" :key="c.id!" :value="c.id!" :label="`${vwCtrlCode(c.code)} — ${c.name}`" />
					</el-select>
				</template>
			</vxe-column>
			<template #empty>
				<el-empty :image-size="80" :description="$t('lz.message.videoWall.outputNotSynced')" />
			</template>
		</vxe-table>
		<template #footer>
			<el-button @click="state.visible = false">{{ $t('lz.button.base.close') }}</el-button>
		</template>
	</el-dialog>
</template>

<script lang="ts" setup name="videoWallFeedsDialog">
import { computed, reactive } from 'vue';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { VwControllerOutputApi } from '/@/api-services/videoWall';
import type { VwControllerOutputOutput } from '/@/api-services/videoWall';
import { useVideoWallController, vwCtrlCode, VW_ROLE } from '/@/stores/its/videoWallController';

const { t: $t } = useI18n();
const vwCtrl = useVideoWallController();

const state = reactive({
	visible: false,
	loading: false,
	controllerId: '',
	controllerName: '',
	outputs: [] as VwControllerOutputOutput[],
});

/** Chỉ cấp được cho bộ hiển thị là con của chính bộ phân phối này (khớp BE). */
const children = computed(() => vwCtrl.controllers.filter((c) => c.role === VW_ROLE.Display && c.parentControllerId === state.controllerId));

const load = async () => {
	state.loading = true;
	try {
		await vwCtrl.load();
		const res = await getAPI(VwControllerOutputApi).apiVideowallVwcontrolleroutputListGet(state.controllerId);
		state.outputs = res.data.result ?? [];
	} finally {
		state.loading = false;
	}
};

const save = async (row: VwControllerOutputOutput, value: string) => {
	await getAPI(VwControllerOutputApi).apiVideowallVwcontrolleroutputSetfeedsPost({ id: row.id, feedsControllerId: value || null });
	row.feedsControllerId = value || null;
	ElMessage.success($t('lz.message.base.saveSuccess'));
};

const openDialog = async (row: { id?: string | null; code?: string | null; name?: string | null }) => {
	state.controllerId = row.id ?? '';
	state.controllerName = `${vwCtrlCode(row.code)} — ${row.name ?? ''}`;
	state.outputs = [];
	state.visible = true;
	await load();
};

defineExpose({ openDialog });
</script>

<style scoped>
.feeds-hint {
	margin: 0 0 10px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}
.feeds-muted {
	color: var(--el-text-color-secondary);
}
</style>
