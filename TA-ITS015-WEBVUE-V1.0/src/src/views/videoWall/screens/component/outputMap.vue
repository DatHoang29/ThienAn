<template>
	<div class="om" v-loading="state.loading">
		<!-- Thanh trên: tóm tắt + Lưu / Huỷ thay đổi (kéo thả chỉ đổi trên trang, Lưu mới gửi BE 1 lần) -->
		<div class="om-bar">
			<div class="om-summary">
				<span>{{ $t('lz.label.videoWall.mapSummary', { placed: screensOnGrid.length, total: state.screens.length, linked: linkedCount }) }}</span>
				<span v-if="missing.length" class="om-warn">· {{ $t('lz.label.videoWall.outputMissingCount', { count: missing.length }) }}</span>
				<span v-if="lastSynced" class="om-muted">· {{ $t('lz.label.videoWall.lastSynced') }}: {{ lastSynced }}</span>
			</div>
			<div class="om-actions">
				<el-tag v-if="dirtyScreens.length" type="warning" effect="light" round>
					{{ $t('lz.label.videoWall.unsavedCount', { count: dirtyScreens.length }) }}
				</el-tag>
				<!-- Bố cục màn: đặt các màn ngoài lưới vào ô trống theo thứ tự mã màn (chỉ trên trang, Lưu mới ghi) -->
				<el-tooltip v-if="isScreens" :content="$t('lz.label.videoWall.autoArrangeHint')" placement="bottom">
					<span>
						<el-button icon="ele-MagicStick" :disabled="!canAutoArrange" v-auth="'videoWallOutput:update'" @click="autoArrange">
							{{ $t('lz.button.videoWall.autoArrangeScreens') }}
						</el-button>
					</span>
				</el-tooltip>
				<!-- Bố cục gán cổng ghi ngay mỗi thao tác ⇒ không có Lưu / Huỷ thay đổi -->
				<el-button v-if="isScreens" :disabled="!dirtyScreens.length" icon="ele-RefreshLeft" @click="onDiscard">{{ $t('lz.button.videoWall.discardChanges') }}</el-button>
				<el-button v-if="isScreens" type="primary" :disabled="!dirtyScreens.length" :loading="state.saving" icon="ele-Check" v-auth="'videoWallOutput:update'" @click="saveWallMap()">
					{{ $t('lz.button.base.save') }}
				</el-button>
				<el-button icon="ele-Refresh" :disabled="state.saving" @click="onRefresh">{{ $t('lz.button.base.refresh') }}</el-button>				<!-- Bố cục màn: bật / tắt màn theo bộ hiển thị / toàn tường (lệnh thiết bị, không phụ thuộc thay đổi chưa lưu) -->
				<el-button v-if="isScreens" icon="ele-SwitchButton" v-auth="'videoWallOutput:update'" @click="screenPowerRef?.open()">
					{{ $t('lz.button.videoWall.screenPower') }}
				</el-button>
				<!-- Bố cục gán cổng: ghi bảng cổng → vị trí xuống bộ con (phải Lưu trước: service đọc Bố cục gán cổng từ DB) -->
				<!-- <el-tooltip v-if="!isScreens" :disabled="!dirtyScreens.length" :content="$t('lz.message.videoWall.saveBeforeApply')" placement="bottom">
					<span>
						<el-button type="warning" icon="ele-Upload" :disabled="!!dirtyScreens.length" v-auth="'videoWallOutput:update'" @click="applyRef?.open()">
							{{ $t('lz.button.videoWall.applyToDevice') }}
						</el-button>
					</span>
				</el-tooltip> -->
			</div>
		</div>

		<el-alert v-if="!state.loading && state.screens.length === 0" type="info" show-icon :closable="false" :title="$t('lz.message.videoWall.noScreenDeclared')" class="mb10" />
		<el-alert v-else-if="!state.loading && !hasOwnedCells" type="warning" show-icon :closable="false" :title="$t('lz.message.videoWall.noCellController')" class="mb10" />

		<div ref="bodyRef" class="om-body" :class="{ 'is-dragging': !!drag.kind }">
			<!-- Bố cục màn: cột trái = danh sách màn (kéo vào tường; thả màn trên lưới vào đây ⇒ về kho) -->
			<template v-if="isScreens">
				<div :id="leftPanel.id" class="om-side om-side--left" :style="leftPanel.panelStyle" @dragover="onSideDragOver" @dragleave="onSideDragLeave" @drop="onSideDrop">
					<ScreenPalette :screens="state.screens" :outputs="displayOutputs" :dirty-ids="dirtyIds" :selected-id="state.selectedId"
						:dragging-id="drag.kind === 'screen' ? drag.id : ''" :draggable="canEdit"
						:remove-active="drag.kind === 'screen' && !!draggingScreen?.wallBlockId" :remove-over="drag.overSide"
						@select="selectScreen" @dragstart="onScreenDragStart" @dragend="onDragEnd" />
				</div>
				<PanelSplitter side="left" class="om-splitter" :label="sideTitle" :controls="leftPanel.id"
					:width="leftPanel.size" :min="leftPanel.min" :max="leftPanel.max" :expanded="leftPanel.visible"
					:expand-label="$t('lz.label.base.extend')" :collapse-label="$t('lz.button.base.collapse')"
					:resize-hint="$t('lz.label.base.resizePanel', { name: sideTitle })"
					@resize="leftPanel.resize" @step="leftPanel.step" />
			</template>

			<!-- Sân khấu + chú thích: cùng kiểu trang ghép màn cũ (wallGrid / wallScreenTile) -->
			<div class="om-wall">
				<WallStage class="om-stage" :blocks="stageBlocks" :cell-ratio="topology.cellRatio" :gap="4" rulers block-name="auto" :min-cell-w="64" :max-cell-w="168"
					pan-exclude=".om-cell" :guide="$t('lz.label.videoWall.cellLabelHint')">
					<template #cell="{ blockNo, row, col }">
						<!-- Ô có màn. Ghép màn: kéo màn sang ô trống khác, nút về kho. Gán cổng: nhận cổng khi màn chưa có cổng, nút gỡ cổng -->
						<el-tooltip v-if="screenAt(blockNo, row, col)" placement="top" :show-after="250" :hide-after="0" :disabled="!!drag.kind" popper-class="vw-info-tip">
							<template #content>
								<VwInfoTip :title="screenAt(blockNo, row, col)!.code || ''" :subtitle="screenAt(blockNo, row, col)!.name"
									:dot="screenDot(screenAt(blockNo, row, col)!)" :rows="tipRows(screenAt(blockNo, row, col)!)" />
							</template>
							<div class="om-cell om-tile" :draggable="canEdit && isScreens"
								:class="{
									'is-selected': state.selectedId === screenAt(blockNo, row, col)!.id,
									'is-off': screenAt(blockNo, row, col)!.status === 0,
									'is-noport': !screenAt(blockNo, row, col)!.controllerOutputId,
									'is-dirty': isDirty(screenAt(blockNo, row, col)!),
									'is-drag-src': drag.id === screenAt(blockNo, row, col)!.id,
									'is-drop': canDropOutput(screenAt(blockNo, row, col)!),
									'is-over': drag.over === key(blockNo, row, col),
								}"
								:style="{ '--ctrl-color': tileColor(screenAt(blockNo, row, col)!) }"
								@dragstart="onScreenDragStart($event, screenAt(blockNo, row, col)!)" @dragend="onDragEnd"
								@dragover="onTileDragOver($event, screenAt(blockNo, row, col)!)" @dragleave="onDragLeaveCell(blockNo, row, col)"
								@drop="onTileDrop($event, screenAt(blockNo, row, col)!)" @click="selectScreen(screenAt(blockNo, row, col)!)">
								<span class="om-tile-code">{{ screenAt(blockNo, row, col)!.code }}</span>
								<span class="om-tile-port">{{ portText(screenAt(blockNo, row, col)!) }}</span>
								<el-icon v-if="isMismatch(screenAt(blockNo, row, col)!)" class="om-tile-warn"><ele-WarningFilled /></el-icon>
								<div v-if="canEdit" class="om-tile-actions">
									<button v-if="!isScreens && screenAt(blockNo, row, col)!.controllerOutputId" type="button" draggable="false" :title="$t('lz.button.videoWall.unlinkOutput')"
										@mousedown.stop @click.stop="unlinkOutput(screenAt(blockNo, row, col)!)">
										<el-icon><ele-Close /></el-icon>
									</button>
									<button v-if="isScreens" type="button" class="is-stock" draggable="false" :title="$t('lz.button.videoWall.screenToStock')"
										@mousedown.stop @click.stop="toStock(screenAt(blockNo, row, col)!)">
										<el-icon><ele-Box /></el-icon>
									</button>
								</div>
							</div>
						</el-tooltip>
						<!-- Ô trống: nhận màn khi kéo vào (màn đã có cổng ⇒ chỉ ô cùng bộ với cổng) -->
						<div v-else class="om-cell om-empty" :class="{
								'is-owned': !!ownerAt(blockNo, col, row),
								'is-free': !ownerAt(blockNo, col, row),
								'is-drop': canDropScreen(blockNo, row, col),
								'is-blocked': !!drag.kind && !canDropScreen(blockNo, row, col),
								'is-over': drag.over === key(blockNo, row, col),
							}"
							:style="{ '--ctrl-color': ownerAt(blockNo, col, row) ? vwCtrl.colorOf(ownerAt(blockNo, col, row)) : 'transparent' }"
							@dragover="onCellDragOver($event, blockNo, row, col)" @dragleave="onDragLeaveCell(blockNo, row, col)" @drop="onCellDrop($event, blockNo, row, col)">
							<span v-if="ownerAt(blockNo, col, row)" class="om-empty-code">{{ vwCtrlCode(vwCtrl.find(ownerAt(blockNo, col, row))?.code) }}</span>
							<el-icon class="om-plus"><ele-Plus /></el-icon>
						</div>
					</template>
				</WallStage>
				<!-- Chú thích: màu = bộ điều khiển, số = cổng đã gán màn / tổng cổng của bộ -->
				<div class="om-legend">
					<span v-for="l in legend" :key="l.id" class="om-legend-item">
						<i :style="{ background: vwCtrl.colorOf(l.id) }" />
						{{ l.label }}
						<b>{{ l.placed }}/{{ l.total }}</b>
					</span>
					<span class="om-legend-item is-noport"><i />{{ $t('lz.label.videoWall.screenNoOutput') }}</span>
					<span class="om-legend-item is-none"><i />{{ $t('lz.label.videoWall.cellNoController') }}</span>
				</div>
			</div>

			<PanelSplitter side="right" class="om-splitter" :label="rightTitle" :controls="sidePanel.id"
				:width="sidePanel.size" :min="sidePanel.min" :max="sidePanel.max" :expanded="sidePanel.visible"
				:expand-label="$t('lz.label.base.extend')" :collapse-label="$t('lz.button.base.collapse')"
				:resize-hint="$t('lz.label.base.resizePanel', { name: rightTitle })"
				@resize="sidePanel.resize" @step="sidePanel.step" />

			<!-- Cột phải. Bố cục màn: chỉ chi tiết màn đang chọn (thông tin thiết bị + độ sáng).
			     Gán cổng: chi tiết + danh sách cổng (thả màn trên lưới vào đây ⇒ về kho) -->
			<div :id="sidePanel.id" class="om-side" :style="sidePanel.panelStyle"
				@dragover="!isScreens && onSideDragOver($event)" @dragleave="!isScreens && onSideDragLeave($event)" @drop="!isScreens && onSideDrop($event)">
				<el-empty v-if="isScreens && !selectedScreen" :image-size="80" :description="$t('lz.label.videoWall.selectScreenHint')" />
				<div v-if="selectedScreen || detailOutput" class="om-detail">
					<div class="om-detail-head" :style="{ '--ctrl-color': detailOutput ? vwCtrl.colorOf(detailOutput.controllerId) : 'var(--el-border-color)' }">
						<div class="om-detail-title">
							<b>{{ selectedScreen ? selectedScreen.code : outputLabel(detailOutput!) }}</b>
							<span class="om-muted">{{ selectedScreen ? selectedScreen.name : vwCtrl.label(detailOutput!.controllerId) }}</span>
						</div>
						<el-button text circle size="small" icon="ele-Close" @click="clearSelection" />
					</div>
					<el-descriptions :column="1" size="small" border>
						<template v-if="selectedScreen">
							<el-descriptions-item :label="$t('lz.entity.videoWall.position')">
								<template v-if="selectedScreen.wallBlockId">{{ cellLabel(selectedScreen.blockNo, selectedScreen.gridRow, selectedScreen.gridCol, topology.isMultiBlock) }}</template>
								<span v-else class="om-muted">{{ $t('lz.label.videoWall.screenGroupStock') }}</span>
							</el-descriptions-item>
							<el-descriptions-item :label="$t('lz.entity.videoWall.outputPort')">{{ portText(selectedScreen) }}</el-descriptions-item>
							<el-descriptions-item :label="$t('lz.entity.videoWall.serialNumber')">{{ selectedScreen.serialNumber || '—' }}</el-descriptions-item>
							<el-descriptions-item :label="$t('lz.label.videoWall.deviceSerial')">
								{{ selectedScreen.deviceSerialNumber || '—' }}
								<el-tag v-if="isMismatch(selectedScreen)" size="small" type="warning" class="ml5">{{ $t('lz.label.videoWall.serialMismatch') }}</el-tag>
							</el-descriptions-item>
							<el-descriptions-item :label="$t('lz.entity.videoWall.connection')">{{ selectedScreen.screenState || '—' }}</el-descriptions-item>
							<el-descriptions-item :label="$t('lz.entity.videoWall.temperature')">{{ selectedScreen.temperature ? `${selectedScreen.temperature} °C` : '—' }}</el-descriptions-item>
							<el-descriptions-item :label="$t('lz.entity.videoWall.runTimeHours')">{{ selectedScreen.runTimeHours ? `${selectedScreen.runTimeHours} h` : '—' }}</el-descriptions-item>
							<el-descriptions-item :label="$t('lz.entity.videoWall.softwareVersion')">{{ selectedScreen.softwareVersion || '—' }}</el-descriptions-item>
							<el-descriptions-item :label="$t('lz.label.videoWall.brightness')">{{ selectedScreen.brightness ?? '—' }}</el-descriptions-item>
							<el-descriptions-item :label="$t('lz.label.videoWall.lastSynced')">{{ selectedScreen.lastCheckedAt ? formatDate(new Date(selectedScreen.lastCheckedAt), 'dd/mm/YYYY HH:MM') : '—' }}</el-descriptions-item>
						</template>
						<template v-else-if="detailOutput">
							<el-descriptions-item :label="$t('lz.entity.videoWall.outputPort')">{{ detailOutput.name }} <span class="om-muted">#{{ detailOutput.deviceOutputId ?? '—' }}</span></el-descriptions-item>
							<el-descriptions-item :label="$t('lz.entity.videoWall.screen')">{{ $t('lz.label.videoWall.noScreen') }}</el-descriptions-item>
						</template>
						<el-descriptions-item v-if="detailOutput" :label="$t('lz.entity.videoWall.resolution')">{{ detailOutput.resolution || '—' }}</el-descriptions-item>
					</el-descriptions>
					<!-- Bố cục màn: độ sáng (đèn nền 0-100) của đúng màn đang chọn — cần màn thật đang gán cổng đã lưu -->
					<div v-if="selectedScreen && canControl(selectedScreen)" class="om-brightness mt10">
						<span class="om-muted">{{ $t('lz.label.videoWall.brightness') }}</span>
						<el-icon v-if="light.reading" class="is-loading" :title="$t('lz.label.videoWall.readingDevice')"><ele-Loading /></el-icon>
						<el-slider v-model="light.value" :min="0" :max="100" size="small" :disabled="light.busy || light.reading || dirtyIds.includes(selectedScreen.id ?? '')" />
						<el-button size="small" type="primary" plain :loading="light.busy" :disabled="dirtyIds.includes(selectedScreen.id ?? '')" @click="applyBrightness(selectedScreen)">
							{{ $t('lz.button.videoWall.apply') }}
						</el-button>
					</div>
					<!-- Trường web quản lý của cổng: nút lưu riêng, không đụng tới thay đổi sơ đồ chưa lưu -->
					<el-form v-if="detailOutput" label-position="top" class="mt10" size="small">
						<el-form-item :label="$t('lz.label.videoWall.outputDisplayName')">
							<el-input v-model="edit.displayName" maxlength="64" clearable :placeholder="detailOutput.name ?? ''" />
						</el-form-item>
						<el-form-item :label="$t('lz.entity.base.remark')">
							<el-input v-model="edit.remark" type="textarea" maxlength="256" />
						</el-form-item>
						<el-form-item :label="$t('lz.entity.base.status')">
							<el-switch v-model="edit.status" :active-value="1" :inactive-value="0" />
						</el-form-item>
						<el-button type="primary" plain size="small" v-auth="'videoWallOutput:update'" @click="saveOutput">{{ $t('lz.button.base.save') }}</el-button>
					</el-form>
				</div>

				<OutputPalette v-if="!isScreens" :outputs="outputsView" :dirty-ids="dirtyOutputIds" :selected-id="detailOutput?.id ?? ''"
					:dragging-id="drag.kind === 'output' ? drag.id : ''" :draggable="canEdit"
					@select="selectOutput" @dragstart="onOutputDragStart" @dragend="onDragEnd" />
			</div>
		</div>

		<ApplyWallOutputsDialog v-if="!isScreens" ref="applyRef" @applied="reload" />
		<ScreenPowerDialog v-if="isScreens" ref="screenPowerRef" />
	</div>
</template>

<script lang="ts" setup name="videoWallOutputMap">
import { computed, onActivated, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue';
import { onBeforeRouteLeave } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { auth } from '/@/utils/authFunction';
import { VwControllerOutputApi, VwDeviceSetupApi, VwScreenApi } from '/@/api-services/videoWall';
import { useVideoWallCommandResult } from '/@/transporter/hooks/useVideoWallCommandResult';
import type { VwControllerOutputOutput, VwScreenOutput } from '/@/api-services/videoWall';
import { useVideoWallController, vwCtrlCode } from '/@/stores/its/videoWallController';
import { cellLabel, useVideoWallTopology } from '/@/stores/its/videoWallTopology';
import WallStage, { type WallStageBlock } from '/@/components/videoWall/wallStage.vue';
import VwInfoTip, { type VwTipRow } from '/@/components/videoWall/vwInfoTip.vue';
import PanelSplitter from '/@/components/workspace/PanelSplitter.vue';
import { useSidePanel } from '/@/components/workspace/useSidePanel';
import { formatDate } from '/@/utils/formatTime';
import OutputPalette from './outputPalette.vue';
import ScreenPalette from './screenPalette.vue';
import ApplyWallOutputsDialog from './applyWallOutputsDialog.vue';
import ScreenPowerDialog from './screenPowerDialog.vue';

/** Trạng thái cổng trên thiết bị (VwOutputDeviceState). */
const DEVICE_MISSING = 2;

/** Phần của màn mà sơ đồ được sửa: vị trí + cổng gán. */
type MapState = Pick<VwScreenOutput, 'wallBlockId' | 'blockNo' | 'gridRow' | 'gridCol' | 'controllerOutputId'>;

const props = withDefaults(
	defineProps<{
		/** screens = Sơ đồ ghép màn (kéo màn vào ô / về kho); outputs = Sơ đồ gán cổng (kéo cổng thả lên màn / gỡ cổng). */
		mode?: 'screens' | 'outputs';
	}>(),
	{ mode: 'screens' }
);
const isScreens = computed(() => props.mode === 'screens');

const { t: $t } = useI18n();
const vwCtrl = useVideoWallController();
const topology = useVideoWallTopology();
const canEdit = auth('videoWallOutput:update');

const state = reactive({
	loading: false,
	saving: false,
	outputs: [] as VwControllerOutputOutput[],
	/** Màn — bản nháp trên trang (kéo thả sửa trực tiếp). */
	screens: [] as VwScreenOutput[],
	/** Vị trí / cổng đã lưu của từng màn (lúc tải) ⇒ so ra thay đổi chưa lưu / huỷ thay đổi. */
	saved: {} as Record<string, MapState>,
	selectedId: '' as string,
	/** Cổng đang chọn khi cổng chưa gán màn nào. */
	selectedOutputId: '' as string,
});
const edit = reactive({ displayName: '' as string | null, remark: '' as string | null, status: 1 as number });
/** Kéo thả: loại + id đang kéo, ô đang rê qua, đang rê trên cột phải. */
const drag = reactive({ kind: '' as '' | 'screen' | 'output', id: '', over: '', overSide: false });

const bodyRef = ref<HTMLElement>();
const applyRef = ref<InstanceType<typeof ApplyWallOutputsDialog>>();
const screenPowerRef = ref<InstanceType<typeof ScreenPowerDialog>>();
const sidePanel = useSidePanel('vw-output-palette', bodyRef, { width: 320, min: 240, maxRatio: 0.35, reserve: 600 });
const sideTitle = computed(() => $t(isScreens.value ? 'lz.label.videoWall.screenListTitle' : 'lz.label.videoWall.outputListTitle'));
/** Bố cục màn: cột trái = danh sách màn, cột phải = chi tiết màn. Gán cổng: chỉ cột phải (chi tiết + danh sách cổng). */
const leftPanel = useSidePanel('vw-screen-palette', bodyRef, { width: 280, min: 220, maxRatio: 0.3, reserve: 600 });
const rightTitle = computed(() => (isScreens.value ? $t('lz.label.videoWall.screenDetail') : sideTitle.value));

const key = (block: number, row: number, col: number) => `${block}:${row}:${col}`;
const outputLabel = (o: VwControllerOutputOutput) => o.displayName || o.name || `#${o.deviceOutputId ?? '?'}`;
const mapKey = (p?: MapState) => (p ? `${p.wallBlockId ?? ''}|${p.gridRow ?? ''}:${p.gridCol ?? ''}|${p.controllerOutputId ?? ''}` : '');
const isDirty = (s: VwScreenOutput) => mapKey(s) !== mapKey(state.saved[s.id ?? '']);
const isMismatch = (s: VwScreenOutput) => !!s.deviceSerialNumber && !!s.serialNumber && s.deviceSerialNumber !== s.serialNumber;
const screenDot = (s: VwScreenOutput) => (s.screenState === 'Online' ? 'on' : s.screenState === 'Warning' ? 'warn' : 'off');

const stageBlocks = computed<WallStageBlock[]>(() => topology.blocks.map((b) => ({ blockNo: b.blockNo ?? 0, cols: b.cols ?? 0, rows: b.rows ?? 0 })));

/** Ô → bộ phụ trách, theo ControllerConfig của các khối trong bố cục đang áp dụng. */
const owners = computed(() => {
	const map = new Map<string, string>();
	for (const b of topology.blocks) {
		if (!b.controllerConfig) continue;
		try {
			for (const c of (JSON.parse(b.controllerConfig) as any[]) ?? []) {
				const id = c.ControllerId ?? c.controllerId;
				if (id) map.set(key(b.blockNo ?? 0, c.Row ?? c.row, c.Col ?? c.col), id);
			}
		} catch {
			// cấu hình lỗi: coi như chưa gán
		}
	}
	return map;
});
const hasOwnedCells = computed(() => owners.value.size > 0);
const ownerAt = (block: number, col: number, row: number) => owners.value.get(key(block, row, col));

/** Cổng của bộ hiển thị / độc lập (bỏ cổng của bộ phân phối — nối sang bộ hiển thị, không ra màn). */
const displayOutputs = computed(() => state.outputs.filter((o) => !o.isMaster));
const outputById = computed(() => new Map(displayOutputs.value.map((o) => [o.id ?? '', o])));
const missing = computed(() => displayOutputs.value.filter((o) => o.deviceState === DEVICE_MISSING));
const outputOf = (s: VwScreenOutput) => (s.controllerOutputId ? outputById.value.get(s.controllerOutputId) : undefined);

const screensOnGrid = computed(() => state.screens.filter((s) => s.wallBlockId && s.gridRow != null && s.gridCol != null));
const linkedCount = computed(() => screensOnGrid.value.filter((s) => s.controllerOutputId).length);
const cellMap = computed(() => {
	const map = new Map<string, VwScreenOutput>();
	for (const s of screensOnGrid.value) map.set(key(s.blockNo ?? 0, s.gridRow!, s.gridCol!), s);
	return map;
});
const screenAt = (block: number, row: number, col: number) => cellMap.value.get(key(block, row, col));
/** Cổng → màn (trên lưới) đang giữ cổng đó, theo bản nháp. */
const holderOf = computed(() => {
	const map = new Map<string, VwScreenOutput>();
	for (const s of screensOnGrid.value) if (s.controllerOutputId) map.set(s.controllerOutputId, s);
	return map;
});
/** Cổng kèm vị trí / mã màn theo bản nháp (cho danh sách cổng). */
const outputsView = computed(() =>
	displayOutputs.value.map((o) => {
		const s = holderOf.value.get(o.id ?? '');
		return { ...o, wallBlockId: s?.wallBlockId ?? null, blockNo: s?.blockNo ?? null, gridRow: s?.gridRow ?? null, gridCol: s?.gridCol ?? null, screenCode: s?.code ?? null, screenName: s?.name ?? null };
	})
);

const selectedScreen = computed(() => state.screens.find((s) => s.id === state.selectedId));

/* ── Độ sáng màn đang chọn (đèn nền 0-100): giá trị theo lần đồng bộ / chỉnh gần nhất ── */
const { waitFor } = useVideoWallCommandResult();
const light = reactive({ value: 70, busy: false, reading: false });
const canControl = (s?: VwScreenOutput) => !!(isScreens.value && canEdit && s?.controllerOutputId && s.deviceSerialNumber);

/** Chọn màn ⇒ hiện giá trị đã lưu, rồi đọc độ sáng thật từ thiết bị (~3,5 s); đổi màn giữa chừng ⇒ bỏ kết quả cũ. */
watch(
	() => selectedScreen.value?.id,
	async (id) => {
		const screen = selectedScreen.value;
		light.value = Number(screen?.brightness ?? 70) || 0;
		if (!id || !canControl(screen)) return;
		light.reading = true;
		try {
			const res = await getAPI(VwDeviceSetupApi).apiVideowallVwdevicesetupScreenstatusPost({ id });
			const messageId = res.data.result?.messageId;
			if (!res.data.result?.accepted || !messageId) return;
			const result = await waitFor(messageId, 30_000);
			const backlight = result.data?.backlight;
			if (result.success && backlight != null && selectedScreen.value?.id === id) {
				light.value = backlight;
				screen!.brightness = String(backlight);
			}
		} catch {
			// Không đọc được (service tắt / màn không phản hồi) ⇒ giữ giá trị đã lưu
		} finally {
			if (selectedScreen.value?.id === id) light.reading = false;
		}
	}
);

const applyBrightness = async (screen: VwScreenOutput) => {
	const value = light.value;
	light.busy = true;
	try {
		const res = await getAPI(VwDeviceSetupApi).apiVideowallVwdevicesetupScreenbacklightPost({ screenIds: [screen.id!], value });
		const accepted = res.data.result;
		if (!accepted?.accepted || !accepted.messageId) throw new Error($t('lz.message.videoWall.commandRejected'));
		const result = await waitFor(accepted.messageId, 60_000);
		if (!result.success) throw new Error(result.errorMessage || $t('lz.label.videoWall.screenPowerFailed'));
		screen.brightness = String(value);
		ElMessage.success($t('lz.label.videoWall.brightnessDone', { value }));
	} catch (e: any) {
		ElMessage.error(e?.message === 'timeout' ? $t('lz.message.videoWall.serviceTimeout') : e?.message ?? String(e));
	} finally {
		light.busy = false;
	}
};
/** Cổng hiện ở thẻ chi tiết: cổng của màn đang chọn, hoặc cổng đang chọn (chưa gán màn). */
const detailOutput = computed(() =>
	selectedScreen.value ? outputOf(selectedScreen.value) : state.selectedOutputId ? outputById.value.get(state.selectedOutputId) : undefined
);

const dirtyScreens = computed(() => state.screens.filter(isDirty));
const dirtyIds = computed(() => dirtyScreens.value.map((s) => s.id ?? ''));
/** Cổng bị đổi màn chưa lưu (cổng của màn đã đổi, trước hoặc sau). */
const dirtyOutputIds = computed(() =>
	dirtyScreens.value.flatMap((s) => [s.controllerOutputId, state.saved[s.id ?? '']?.controllerOutputId]).filter((id): id is string => !!id)
);

/** Chú thích theo bộ điều khiển có cổng: số cổng đã gán màn / tổng. */
const legend = computed(() => {
	const map = new Map<string, { id: string; label: string; placed: number; total: number }>();
	for (const o of displayOutputs.value) {
		const id = o.controllerId ?? '';
		if (!id) continue;
		const item = map.get(id) ?? { id, label: vwCtrl.label(id), placed: 0, total: 0 };
		item.total++;
		if (holderOf.value.has(o.id ?? '')) item.placed++;
		map.set(id, item);
	}
	return [...map.values()].sort((a, b) => a.label.localeCompare(b.label));
});

const tileColor = (s: VwScreenOutput) => {
	const o = outputOf(s);
	return o ? vwCtrl.colorOf(o.controllerId) : '#7a8699';
};
const portText = (s: VwScreenOutput) => {
	const o = outputOf(s);
	return o ? `${vwCtrlCode(vwCtrl.find(o.controllerId)?.code)} · ${outputLabel(o)}` : $t('lz.label.videoWall.screenNoOutput');
};

/** Chú thích ô: vị trí · cổng · serial · nhiệt độ · giờ chạy. */
const tipRows = (s: VwScreenOutput): VwTipRow[] => {
	const o = outputOf(s);
	return [
		{ icon: 'ele-Location', label: $t('lz.label.videoWall.windowPosition'), value: cellLabel(s.blockNo, s.gridRow, s.gridCol, topology.isMultiBlock) },
		{ icon: 'ele-Cpu', label: $t('lz.entity.videoWall.controller'), value: o ? vwCtrl.label(o.controllerId) : null, swatch: o ? vwCtrl.colorOf(o.controllerId) : undefined, show: !!o },
		{ icon: 'ele-Connection', label: $t('lz.entity.videoWall.outputPort'), value: portText(s) },
		{ icon: 'ele-Ticket', label: $t('lz.entity.videoWall.serialNumber'), value: s.serialNumber, show: !!s.serialNumber },
		{ icon: 'ele-Warning', label: $t('lz.label.videoWall.deviceSerial'), value: s.deviceSerialNumber, tone: 'muted', show: isMismatch(s) },
		{ icon: 'ele-Odometer', label: $t('lz.entity.videoWall.temperature'), value: s.temperature ? `${s.temperature} °C` : null, show: !!s.temperature },
		{ icon: 'ele-Timer', label: $t('lz.entity.videoWall.runTimeHours'), value: s.runTimeHours ? `${s.runTimeHours} h` : null, show: !!s.runTimeHours },
	];
};

const lastSynced = computed(() => {
	const times = state.outputs.map((o) => o.lastSyncedAt).filter(Boolean).map((d) => new Date(d as any).getTime());
	return times.length ? formatDate(new Date(Math.max(...times)), 'dd/mm/YYYY HH:MM') : '';
});

// ─── Chọn ───────────────────────────────────────────────────────────────
const fillEdit = (o?: VwControllerOutputOutput) => {
	edit.displayName = o?.displayName ?? '';
	edit.remark = o?.remark ?? '';
	edit.status = (o?.status as number) ?? 1;
};
const selectScreen = (s: VwScreenOutput) => {
	state.selectedId = s.id ?? '';
	state.selectedOutputId = '';
	fillEdit(outputOf(s));
};
const selectOutput = (o: VwControllerOutputOutput) => {
	const holder = holderOf.value.get(o.id ?? '');
	if (holder) return selectScreen(holder);
	state.selectedId = '';
	state.selectedOutputId = o.id ?? '';
	fillEdit(outputById.value.get(o.id ?? ''));
};
const clearSelection = () => {
	state.selectedId = '';
	state.selectedOutputId = '';
};

// ─── Kéo thả (chỉ đổi trên trang) ───────────────────────────────────────
const draggingScreen = computed(() => (drag.kind === 'screen' ? state.screens.find((s) => s.id === drag.id) : undefined));
const draggingOutput = computed(() => (drag.kind === 'output' ? outputById.value.get(drag.id) : undefined));

/** Thả màn vào ô trống: màn chưa có cổng ⇒ ô nào cũng được; có cổng ⇒ ô phải do bộ của cổng phụ trách. */
const canDropScreen = (block: number, row: number, col: number) => {
	const s = draggingScreen.value;
	if (!s || screenAt(block, row, col)) return false;
	const o = outputOf(s);
	return !o || ownerAt(block, col, row) === o.controllerId;
};
/** Thả cổng lên màn: màn trên lưới, chưa có cổng (có rồi ⇒ gỡ cổng cũ trước), ô do bộ của cổng phụ trách. */
const canDropOutput = (s: VwScreenOutput) => {
	const o = draggingOutput.value;
	// Màn đã có cổng khác ⇒ được thả (đổi chéo cổng, 12/11/2026); thả lại đúng cổng đang gắn ⇒ bỏ qua
	return !!o && !!s.wallBlockId && s.controllerOutputId !== o.id && ownerAt(s.blockNo ?? 0, s.gridCol ?? -1, s.gridRow ?? -1) === o.controllerId;
};

const startDrag = (e: DragEvent, kind: 'screen' | 'output', id: string) => {
	drag.kind = kind;
	drag.id = id;
	e.dataTransfer?.setData('text/plain', id);
	if (e.dataTransfer) e.dataTransfer.effectAllowed = 'move';
};
const onScreenDragStart = (e: DragEvent, s: VwScreenOutput) => {
	if (!canEdit || !isScreens.value) return e.preventDefault();
	startDrag(e, 'screen', s.id ?? '');
};
const onOutputDragStart = (e: DragEvent, o: VwControllerOutputOutput) => {
	if (!canEdit || isScreens.value || state.saving || o.deviceState === DEVICE_MISSING) return e.preventDefault();
	startDrag(e, 'output', o.id ?? '');
};
const onDragEnd = () => {
	drag.kind = '';
	drag.id = '';
	drag.over = '';
	drag.overSide = false;
};
const onDragLeaveCell = (block: number, row: number, col: number) => {
	if (drag.over === key(block, row, col)) drag.over = '';
};

const onCellDragOver = (e: DragEvent, block: number, row: number, col: number) => {
	if (!canDropScreen(block, row, col)) return;
	e.preventDefault();
	drag.over = key(block, row, col);
};
const onCellDrop = (e: DragEvent, block: number, row: number, col: number) => {
	e.preventDefault();
	const s = draggingScreen.value;
	if (s && canDropScreen(block, row, col)) {
		s.wallBlockId = topology.blocks.find((b) => b.blockNo === block)?.id ?? null;
		s.blockNo = block;
		s.gridRow = row;
		s.gridCol = col;
	}
	onDragEnd();
};

const onTileDragOver = (e: DragEvent, s: VwScreenOutput) => {
	if (!canDropOutput(s)) return;
	e.preventDefault();
	drag.over = key(s.blockNo ?? 0, s.gridRow ?? -1, s.gridCol ?? -1);
};
/**
 * Gán cổng cho màn rồi ghi ngay (DB + bộ con). Cổng đang ở màn khác ⇒ chuyển sang; màn đích đã có cổng ⇒ đổi chéo
 * (màn cũ nhận cổng của màn đích, hoặc cổng đó về danh sách chờ nếu cổng kéo chưa gắn màn nào).
 */
const onTileDrop = (e: DragEvent, s: VwScreenOutput) => {
	e.preventDefault();
	const o = draggingOutput.value;
	const ok = !!o && canDropOutput(s);
	if (o && ok) {
		const prev = holderOf.value.get(o.id ?? '');
		const displaced = s.controllerOutputId ?? null;
		if (prev) prev.controllerOutputId = displaced;
		s.controllerOutputId = o.id ?? null;
	}
	onDragEnd();
	if (ok) commitNow();
};

/** Thả màn đang trên lưới vào cột phải ⇒ về kho. */
const onSideDragOver = (e: DragEvent) => {
	if (!draggingScreen.value?.wallBlockId) return;
	e.preventDefault();
	drag.overSide = true;
};
const onSideDragLeave = (e: DragEvent) => {
	if (!(e.currentTarget as HTMLElement).contains(e.relatedTarget as Node | null)) drag.overSide = false;
};
const onSideDrop = (e: DragEvent) => {
	e.preventDefault();
	if (draggingScreen.value?.wallBlockId) toStock(draggingScreen.value);
	onDragEnd();
};

// ─── Tự xếp màn (Bố cục màn) ────────────────────────────────────────────
/** Ô trống theo thứ tự đọc: khối → hàng → cột. */
const emptyCells = computed(() =>
	topology.blocks
		.slice()
		.sort((a, b) => (a.blockNo ?? 0) - (b.blockNo ?? 0))
		.flatMap((b) =>
			Array.from({ length: (b.rows ?? 0) * (b.cols ?? 0) }, (_, i) => ({
				block: b,
				row: Math.floor(i / (b.cols ?? 1)),
				col: i % (b.cols ?? 1),
			}))
		)
		.filter((c) => !screenAt(c.block.blockNo ?? 0, c.row, c.col))
);
/** Màn ngoài lưới, xếp theo mã (SCR-2 trước SCR-10). */
const unplacedScreens = computed(() =>
	state.screens.filter((s) => !s.wallBlockId).sort((a, b) => (a.code ?? '').localeCompare(b.code ?? '', undefined, { numeric: true }))
);
const canAutoArrange = computed(() => canEdit && unplacedScreens.value.length > 0 && emptyCells.value.length > 0);

/** Đặt lần lượt màn ngoài lưới vào ô trống; chưa gán cổng. Chỉ đổi trên trang — người dùng xem rồi bấm Lưu (hoặc Huỷ thay đổi). */
const autoArrange = () => {
	const cells = emptyCells.value;
	const screens = unplacedScreens.value;
	const n = Math.min(cells.length, screens.length);
	for (let i = 0; i < n; i++) {
		const s = screens[i];
		const c = cells[i];
		s.wallBlockId = c.block.id ?? null;
		s.blockNo = c.block.blockNo ?? 0;
		s.gridRow = c.row;
		s.gridCol = c.col;
		s.controllerOutputId = null;
	}
	const left = screens.length - n;
	ElMessage({
		type: left ? 'warning' : 'success',
		message: left ? $t('lz.message.videoWall.autoArrangeLeft', { placed: n, left }) : $t('lz.message.videoWall.autoArrangeDone', { placed: n }),
	});
};

/** Gỡ cổng khỏi màn (màn ở lại ô) — Bố cục gán cổng: ghi ngay. */
const unlinkOutput = (s: VwScreenOutput) => {
	if (state.saving) return;
	s.controllerOutputId = null;
	commitNow();
};
/** Đưa màn về kho (bỏ luôn cổng). */
const toStock = (s: VwScreenOutput) => {
	s.wallBlockId = null;
	s.blockNo = null;
	s.gridRow = null;
	s.gridCol = null;
	s.controllerOutputId = null;
};

// ─── Tải / lưu ──────────────────────────────────────────────────────────
const reload = async () => {
	state.loading = true;
	try {
		await Promise.all([vwCtrl.load(), topology.load(true)]);
		const [outRes, scrRes] = await Promise.all([
			getAPI(VwControllerOutputApi).apiVideowallVwcontrolleroutputListGet(),
			getAPI(VwScreenApi).apiVideowallVwscreenListGet(),
		]);
		state.outputs = outRes.data.result ?? [];
		state.screens = scrRes.data.result ?? [];
		state.saved = Object.fromEntries(
			state.screens.map((s) => [
				s.id ?? '',
				{ wallBlockId: s.wallBlockId, blockNo: s.blockNo, gridRow: s.gridRow, gridCol: s.gridCol, controllerOutputId: s.controllerOutputId },
			])
		);
		if (selectedScreen.value) selectScreen(selectedScreen.value);
		else if (state.selectedOutputId && !outputById.value.has(state.selectedOutputId)) clearSelection();
	} finally {
		state.loading = false;
	}
};

const confirmDiscard = async () => {
	try {
		await ElMessageBox.confirm($t('lz.message.videoWall.confirmDiscardChanges'), $t('lz.message.base.warning'), {
			confirmButtonText: $t('lz.button.videoWall.discardChanges'),
			cancelButtonText: $t('lz.button.base.cancel'),
			type: 'warning',
		});
		return true;
	} catch {
		return false;
	}
};

const onRefresh = async () => {
	if (dirtyScreens.value.length && !(await confirmDiscard())) return;
	await reload();
};

/** Huỷ thay đổi: trả mọi màn về vị trí / cổng đã lưu. */
const onDiscard = async () => {
	if (!(await confirmDiscard())) return;
	for (const s of state.screens) Object.assign(s, state.saved[s.id ?? '']);
};

/** Lưu toàn bộ màn đã đổi (vị trí + cổng) trong 1 lần, xong mới tải lại. */
const saveWallMap = async () => {
	// Mỗi trang chỉ gửi phần của mình: Bố cục màn ⇒ vị trí (về kho thì BE tự bỏ cổng); Bố cục gán cổng ⇒ cổng.
	// Không ghi đè phần trang kia vừa lưu khi 2 tab cùng mở.
	const items = dirtyScreens.value.map((s) => ({
		id: s.id,
		setPlacement: isScreens.value,
		wallBlockId: s.wallBlockId ?? null,
		gridCol: s.wallBlockId ? s.gridCol : null,
		gridRow: s.wallBlockId ? s.gridRow : null,
		setOutput: !isScreens.value,
		controllerOutputId: s.wallBlockId ? s.controllerOutputId ?? null : null,
	}));
	if (!items.length) return;
	// Chỉ lưu DB — ghi xuống bộ con bằng nút "Áp dụng xuống thiết bị" (12/11/2026: chạy được cả khi chưa có service)
	state.saving = true;
	try {
		await getAPI(VwScreenApi).apiVideowallVwscreenSavewallmapPost({ items });
		ElMessage.success($t('lz.message.base.saveSuccess'));
	} finally {
		state.saving = false;
	}
	await reload();
};

/** Bố cục gán cổng: mỗi thao tác (thả cổng / gỡ cổng) lưu DB ngay (không gửi thiết bị); lỗi lưu ⇒ nạp lại (trả về như cũ). */
const commitNow = async () => {
	try {
		await saveWallMap();
	} catch {
		await reload();
	}
};

/** Lưu tên hiển thị / ghi chú / trạng thái của cổng — cập nhật tại chỗ, không tải lại (giữ thay đổi sơ đồ chưa lưu). */
const saveOutput = async () => {
	const o = detailOutput.value;
	if (!o?.id) return;
	const body = { id: o.id, displayName: edit.displayName || null, remark: edit.remark || null, status: edit.status as any };
	await getAPI(VwControllerOutputApi).apiVideowallVwcontrolleroutputUpdatePost(body);
	Object.assign(o, { displayName: body.displayName, remark: body.remark, status: body.status });
	ElMessage.success($t('lz.message.base.saveSuccess'));
};

// Rời trang / đóng tab trình duyệt khi còn thay đổi sơ đồ chưa lưu ⇒ hỏi trước
onBeforeRouteLeave(async () => (dirtyScreens.value.length ? confirmDiscard() : true));
const onBeforeUnload = (e: BeforeUnloadEvent) => {
	if (!dirtyScreens.value.length) return;
	e.preventDefault();
	e.returnValue = '';
};

onMounted(() => {
	window.addEventListener('beforeunload', onBeforeUnload);
	reload();
});
onBeforeUnmount(() => window.removeEventListener('beforeunload', onBeforeUnload));
// Tab giữ trạng thái (KeepAlive): quay lại tab ⇒ nạp lại để thấy thay đổi ở tab kia, trừ khi đang có thay đổi chưa lưu (giữ nguyên)
let firstActivation = true;
onActivated(() => {
	if (firstActivation) {
		firstActivation = false;
		return;
	}
	if (!dirtyScreens.value.length) reload();
});
defineExpose({ reload });
</script>

<style scoped lang="scss">
.om {
	height: 100%;
	display: flex;
	flex-direction: column;
	min-height: 0;
}
.om-bar {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 10px;
	margin-bottom: 8px;
}
.om-summary {
	display: flex;
	gap: 6px;
	flex-wrap: wrap;
	font-size: 13px;
}
.om-actions {
	display: flex;
	align-items: center;
	gap: 8px;
	.el-button + .el-button {
		margin-left: 0;
	}
}
.om-muted {
	color: var(--el-text-color-secondary);
}
.om-warn {
	color: var(--el-color-warning);
}
.om-body {
	flex: 1;
	min-height: 0;
	display: flex;
	gap: 0;
}
/* Sân khấu + chú thích trong 1 khung (như wallGrid cũ) */
.om-wall {
	flex: 1;
	min-width: 0;
	display: flex;
	flex-direction: column;
	border-radius: 10px;
	overflow: hidden;
}
.om-wall > .om-stage {
	flex: 1;
	min-height: 0;
	height: auto;
	border-radius: 0;
	box-shadow: none;
	user-select: none;
}

/* ─── Cột phải (cùng kiểu cột Nguồn của màn soạn kịch bản) ─── */
.om-splitter {
	flex: none;
	width: 24px;
	align-self: stretch;
}
.om-side {
	overflow-y: auto;
	flex-shrink: 0;
	border-left: 1px solid var(--el-border-color-light);
	background-color: var(--el-bg-color);
}
.om-side--left {
	border-left: none;
	border-right: 1px solid var(--el-border-color-light);
}
.om-brightness {
	display: flex;
	align-items: center;
	gap: 10px;

	.el-slider {
		flex: 1;
	}
}

.om-detail {
	margin: 12px 12px 0;
	padding: 10px;
	border-radius: 8px;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
	box-shadow: var(--el-box-shadow-lighter);
}
.om-detail-head {
	display: flex;
	align-items: flex-start;
	justify-content: space-between;
	gap: 6px;
	margin-bottom: 8px;
	padding-left: 8px;
	border-left: 3px solid var(--ctrl-color);
}
.om-detail-title {
	display: flex;
	flex-direction: column;
	min-width: 0;
	b {
		font-size: 14px;
	}
	span {
		font-size: 12px;
	}
}

/* ─── Ô trên sân khấu ─── */
.om-cell {
	position: relative;
	width: 100%;
	height: 100%;
	box-sizing: border-box;
	border-radius: 4px;
	user-select: none;
}
/* Tấm màn trên sân khấu tối (như wallScreenTile cũ): vạch trên cùng mang màu bộ điều khiển của cổng */
.om-tile {
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	gap: 2px;
	padding: 4px 4px 2px;
	border: 1px solid rgba(120, 175, 255, 0.22);
	background: linear-gradient(170deg, #1f3a63 0%, #152b4c 100%);
	box-shadow: inset 0 3px 0 var(--ctrl-color), 0 2px 6px rgba(3, 12, 30, 0.35);
	color: #f2f6fc;
	cursor: pointer;
	overflow: hidden;
	transition: border-color 0.15s, box-shadow 0.15s, opacity 0.15s, background 0.15s;
	&[draggable='true'] {
		cursor: grab;
	}
	&:hover {
		border-color: rgba(140, 190, 255, 0.55);
		background: linear-gradient(170deg, #24436f 0%, #183158 100%);
		.om-tile-actions {
			opacity: 1;
		}
	}
	&.is-selected {
		border-color: #3b8cff;
		box-shadow: inset 0 3px 0 var(--ctrl-color), 0 0 0 2px #3b8cff, 0 0 16px rgba(59, 140, 255, 0.6);
	}
	/* Màn chưa có cổng: viền cam nét đứt */
	&.is-noport {
		border-style: dashed;
		border-color: rgba(230, 162, 60, 0.6);
		.om-tile-port {
			color: #e6a23c;
		}
	}
	/* Đổi chưa lưu: viền vàng */
	&.is-dirty {
		border-style: solid;
		border-color: #e6a23c;
		box-shadow: inset 0 3px 0 var(--ctrl-color), 0 0 0 1px #e6a23c;
	}
	/* Đang kéo cổng: màn nhận được sáng lên */
	&.is-drop {
		border-color: #3b8cff;
		background: linear-gradient(170deg, #26508a 0%, #1b3a66 100%);
	}
	&.is-over {
		box-shadow: inset 0 3px 0 var(--ctrl-color), 0 0 0 2px #3b8cff, 0 0 16px rgba(59, 140, 255, 0.6);
	}
	&.is-off {
		opacity: 0.45;
	}
	&.is-drag-src {
		opacity: 0.35;
	}
}
.om-tile-code {
	max-width: 100%;
	font-size: clamp(11px, calc(var(--cell-w, 96px) * 0.12), 15px);
	font-weight: 600;
	letter-spacing: 0.2px;
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}
.om-tile-port {
	max-width: 100%;
	font-size: clamp(9px, calc(var(--cell-w, 96px) * 0.095), 12px);
	color: rgba(186, 208, 240, 0.75);
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
	font-variant-numeric: tabular-nums;
}
.om-tile-warn {
	position: absolute;
	top: 6px;
	left: 5px;
	font-size: 13px;
	color: #e6a23c;
}
/* Nút gỡ cổng / về kho: hiện khi rê chuột vào ô */
.om-tile-actions {
	position: absolute;
	top: 6px;
	right: 4px;
	display: flex;
	gap: 3px;
	opacity: 0;
	transition: opacity 0.15s;
	button {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 18px;
		height: 18px;
		padding: 0;
		border: none;
		border-radius: 50%;
		font-size: 11px;
		color: #fff;
		background: rgba(230, 162, 60, 0.92);
		cursor: pointer;
		transition: transform 0.1s;
		&:hover {
			transform: scale(1.12);
		}
		&.is-stock {
			background: rgba(245, 108, 108, 0.92);
		}
	}
}
/* Ô trống (như wallGrid cũ): nét đứt mờ; ô đã gán bộ có vạch màu bộ */
.om-empty {
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	gap: 2px;
	border: 1px dashed var(--wg-empty-border);
	background: var(--wg-empty);
	color: var(--wg-muted);
	box-shadow: inset 0 3px 0 var(--ctrl-color);
	transition: background 0.15s, border-color 0.15s, opacity 0.15s;
	&.is-free {
		opacity: 0.55;
	}
	/* Đang kéo màn: ô nhận được sáng lên, ô khác mờ đi */
	&.is-drop {
		opacity: 1;
		border-color: var(--wg-accent);
		background: rgba(59, 140, 255, 0.14);
		color: #9cc5ff;
	}
	&.is-over {
		border-style: solid;
		background: rgba(59, 140, 255, 0.32);
		.om-plus {
			opacity: 1;
		}
	}
	&.is-blocked {
		opacity: 0.25;
	}
}
.om-empty-code {
	font-size: 11px;
	font-weight: 600;
}
.om-plus {
	font-size: 18px;
	opacity: 0;
	transition: opacity 0.15s;
}
/* Đang kéo: màn không nhận thả mờ đi, trừ màn nguồn */
.om-body.is-dragging .om-tile:not(.is-drop):not(.is-drag-src) {
	opacity: 0.5;
}
/* Chú thích dưới sân khấu (như wallGrid cũ) */
.om-legend {
	flex-shrink: 0;
	display: flex;
	flex-wrap: wrap;
	align-items: center;
	gap: 6px 14px;
	padding: 8px 14px;
	background: #2d4f80;
	font-size: 12px;
	color: rgba(215, 230, 250, 0.75);
}
.om-legend-item {
	display: inline-flex;
	align-items: center;
	gap: 6px;
	i {
		width: 10px;
		height: 10px;
		border-radius: 3px;
	}
	b {
		color: #fff;
		font-variant-numeric: tabular-nums;
	}
	&.is-noport i {
		border: 1px dashed #e6a23c;
	}
	&.is-none i {
		border: 1px dashed rgba(215, 230, 250, 0.75);
	}
}
</style>
