<template>
	<div class="videowall-wall-permission-container">
		<el-dialog v-model="state.isShowDialog" draggable top="5vh" :close-on-click-modal="false" width="min(1400px, 96vw)">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<el-tabs v-model="state.activeTab" v-loading="state.loading" class="wp-tabs">
				<!-- Tab 1: thông tin quyền -->
				<el-tab-pane :label="$t('lz.label.videoWall.permissionInfo')" name="info">
					<!-- Nhân bản quyền của phiên bản bố cục cũ sang bố cục hiện tại: ô giữ theo toạ độ ⇒ nhắc kiểm lại vùng -->
					<el-alert v-if="isCopyFromOldLayout" class="mb15" type="warning" :closable="false" show-icon :title="$t('lz.message.videoWall.permissionCopyFromOldLayout')" />
					<!-- Sửa quyền của bố cục khác bố cục đang dùng: lưới (tab Vùng được cấp) vẽ theo bố cục của quyền -->
					<el-alert v-if="state.layout" class="mb15" type="warning" :closable="false" show-icon
						:title="$t('lz.message.videoWall.permissionOtherLayoutEditing', { name: `${state.layout.code} — ${state.layout.name}` })" />
					<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto" label-position="left" autocomplete="off">
						<el-row :gutter="10">
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.base.code')" prop="code"
									:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.code') }), trigger: 'blur' }]">
									<el-input v-model="state.ruleForm.code" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.code') })" :disabled="props.operateType === 'edit'"
										maxlength="64" clearable />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.base.name')" prop="name"
									:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.name') }), trigger: 'blur' }]">
									<el-input v-model="state.ruleForm.name" :placeholder="$t('lz.label.videoWall.permissionDescExample')" maxlength="128" clearable />
								</el-form-item>
							</el-col>
							<!-- Một bản ghi chỉ gán cho người dùng HOẶC đơn vị, không cả hai ⇒ chọn loại trước, hiện đúng 1 ô chọn -->
							<!-- Hàng: Đối tượng | Trạng thái (2 × 2 cùng hàng Mã | Tên). "Toàn quyền" nằm ở tab Vùng được cấp vì nó thay cho việc chọn ô. -->
							<el-col :xs="24" :sm="12" class="mb20">
								<el-form-item :label="$t('lz.entity.videoWall.actor')" required>
									<el-select v-model="state.actorType" class="w100" @change="ruleFormRef?.clearValidate(['userIds', 'orgIds'])">
										<el-option value="user" :label="$t('lz.entity.videoWall.user')" />
										<el-option value="org" :label="$t('lz.entity.videoWall.org')" />
									</el-select>
								</el-form-item>
							</el-col>
							<!-- Dùng / Không dùng: độc lập với bố cục (bố cục chỉ quyết định toạ độ ô còn đúng không) -->
							<el-col :xs="24" :sm="12" class="mb20">
								<el-form-item :label="$t('lz.entity.base.status')" prop="status">
									<el-radio-group v-model="state.ruleForm.status">
										<el-radio v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code" :value="parseInt(item.code)">{{ item.name }}</el-radio>
									</el-radio-group>
								</el-form-item>
							</el-col>
							<el-col v-if="state.hadBothActors" :span="24" class="mb20">
								<el-alert type="warning" :closable="false" show-icon :title="$t('lz.message.videoWall.permissionActorBoth')" />
							</el-col>
							<!-- Chọn nhiều bằng 2 cột (trái = chưa chọn + ô tìm, phải = đã chọn); kéo cột nút giữa để đổi chiều rộng: components/pickList/transferPickList.vue -->
							<el-col v-if="state.actorType === 'user'" :span="24" class="mb20">
								<el-form-item :label="$t('lz.entity.videoWall.user')" prop="userIds" required :rules="[{ validator: validateActors, trigger: 'change' }]">
									<TransferPickList ref="userPickRef" v-model="state.ruleForm.userIds" :items="props.users" :label-of="(u) => u.realName || u.name || u.username || ''"
										:sub-of="userSub" :icon-of="() => 'ele-User'" :scroll-height="PICK_HEIGHT" :source-title="$t('lz.entity.videoWall.user')" :target-title="$t('lz.label.videoWall.selectedTitle')"
										:search-placeholder="$t('lz.label.videoWall.searchUser')" @change="revalidateActors" />
								</el-form-item>
							</el-col>
							<el-col v-else :span="24" class="mb20">
								<el-form-item :label="$t('lz.entity.videoWall.org')" prop="orgIds" required :rules="[{ validator: validateActors, trigger: 'change' }]">
									<TransferPickList ref="orgPickRef" v-model="state.ruleForm.orgIds" :items="orgItems" :label-of="(o) => o.name || ''" :sub-of="(o) => o.path"
										:indent-of="(o) => o.level" :parent-of="(o) => o.parentId" :icon-of="orgIcon" :scroll-height="PICK_HEIGHT" :source-title="$t('lz.entity.videoWall.org')" :target-title="$t('lz.label.videoWall.selectedTitle')"
										:search-placeholder="$t('lz.label.videoWall.searchOrg')" @change="revalidateActors" />
								</el-form-item>
							</el-col>
							<!-- Ghi chú chiếm cả hàng cuối -->
							<el-col :span="24" class="mb20">
								<el-form-item :label="$t('lz.entity.base.remark')" prop="remark">
									<el-input v-model="state.ruleForm.remark" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.remark') })" maxlength="256" :rows="3"
										show-word-limit type="textarea" />
								</el-form-item>
							</el-col>
						</el-row>
					</el-form>
				</el-tab-pane>

				<!-- Tab 2: lưới chọn ô — các khối xếp ngang trái → phải đúng bố cục tường -->
				<el-tab-pane name="area">
					<template #label>
						{{ $t('lz.entity.videoWall.grantedArea') }}
						<el-tag size="small" round class="ml5" :type="isFullAccess ? 'success' : 'info'">
							{{ isFullAccess ? $t('lz.entity.videoWall.fullAccess') : `${state.selected.size}/${totalCells}` }}
						</el-tag>
					</template>
					<div class="wp-area">
						<div class="wp-grid-header">
							<!-- Toàn quyền = chọn đủ mọi ô: tick ⇒ chọn tất cả, bỏ tick ⇒ bỏ chọn; tự tick khi chọn đủ mọi ô -->
							<el-checkbox v-model="isFullAccess" class="wp-full-access" :class="{ 'is-on': isFullAccess }">
								<span class="wp-full-access-title">{{ $t('lz.entity.videoWall.fullAccess') }}</span>
							</el-checkbox>
							<span class="wp-grid-count">
								{{ $t('lz.label.base.selectedCount', { count: state.selected.size, total: totalCells }) }}
							</span>
							<span style="flex: 1" />
							<div class="wp-grid-actions">
								<button type="button" class="wp-grid-btn" :disabled="isFullAccess" @click="selectAll">
									<el-icon><ele-Select /></el-icon>{{ $t('lz.button.base.selectAll') }}
								</button>
								<button type="button" class="wp-grid-btn" :disabled="!state.selected.size" @click="clearAll">
									<el-icon><ele-Close /></el-icon>{{ $t('lz.button.videoWall.deselect') }}
								</button>
							</div>
						</div>
						<!-- Sân khấu dùng chung (nền, khối, thước, zoom, kéo nền): components/videoWall/wallStage.vue. Ở đây chỉ vẽ ô chọn. -->
						<WallStage class="wp-stage" :blocks="stageBlocks" :cell-ratio="state.layout?.cellRatio || topology.cellRatio" :gap="GAP" rulers ruler-clickable
							block-name="always" :min-cell-w="CELL_MIN" :max-cell-w="CELL_MAX" pan-exclude=".wp-cell"
							:guide="`${$t('lz.label.videoWall.gridHint')} ${$t('lz.label.videoWall.cellLabelHint')}`" :guide-label="$t('lz.label.videoWall.gridGuide')"
							@ruler-click="onRulerClick">
							<template #block-meta="{ cols, rows }">
								<span class="ws-block-meta">{{ $t('lz.label.videoWall.gridColsRows', { cols, rows }) }}</span>
							</template>
							<template #cell="{ blockNo, row, col }">
								<!-- Chú thích ô: màn · vị trí · bộ điều khiển · độ phân giải · trạng thái cấp; tắt khi đang kéo chọn vùng -->
								<el-tooltip placement="top" :show-after="300" :hide-after="0" :disabled="!!state.drag" popper-class="vw-info-tip">
									<template #content>
										<VwInfoTip :title="screenAt(blockNo, col, row)?.name || $t('lz.label.videoWall.emptyCell')"
											:swatch="controllerOf(blockNo, col, row) ? vwColorVar(controllerOf(blockNo, col, row)?.colorHex) : undefined" :rows="cellTipRows(blockNo, col, row)" />
									</template>
									<div class="wp-cell"
										:class="{
											'is-selected': state.selected.has(cellKey(blockNo, col, row)),
											'is-empty': !screenAt(blockNo, col, row),
											'is-preview-add': inPreview(blockNo, col, row) && state.drag?.add,
											'is-preview-remove': inPreview(blockNo, col, row) && state.drag && !state.drag.add,
										}"
										:style="{ borderTopColor: vwColorVar(controllerOf(blockNo, col, row)?.colorHex) }"
										@pointerdown.prevent="startDrag(blockNo, col, row, $event)" @pointerenter="dragOver(blockNo, col, row)">
										<span class="wp-cell-name">{{ screenAt(blockNo, col, row)?.code || screenAt(blockNo, col, row)?.name || '—' }}</span>
										<span class="wp-cell-pos">H{{ row + 1 }} · C{{ col + 1 }}</span>
										<el-icon v-if="state.selected.has(cellKey(blockNo, col, row))" class="wp-cell-check"><ele-Check /></el-icon>
									</div>
								</el-tooltip>
							</template>
						</WallStage>
						<div class="wp-legend">
							<span class="wp-legend-item"><i class="wp-legend-cell is-selected" />{{ $t('lz.label.videoWall.cellGranted') }}</span>
							<span class="wp-legend-item"><i class="wp-legend-cell" />{{ $t('lz.label.videoWall.notGranted') }}</span>
							<template v-if="legendControllers.length">
								<span class="wp-legend-sep" />
								<span class="wp-legend-caption">{{ $t('lz.entity.videoWall.controller') }}:</span>
								<span v-for="ctrl in legendControllers" :key="ctrl.id!" class="wp-legend-item">
									<i :style="{ backgroundColor: vwColorVar(ctrl.colorHex) }" />{{ ctrl.code ? vwCtrlCode(ctrl.code) : ctrl.name }}
								</span>
							</template>
						</div>
					</div>
				</el-tab-pane>
			</el-tabs>
			<template #footer>
				<span class="dialog-footer">
					<el-button @click="cancel">{{ $t('lz.button.base.cancel') }}</el-button>
					<el-button type="primary" :loading="state.saving" @click="submit">{{ $t('lz.button.base.confirm') }}</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="videoWallEditWallPermission">
import { computed, onBeforeUnmount, reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { VwWallPermissionApi, VwWallTopologyApi, StatusEnum } from '/@/api-services/videoWall';
import type { VwWallTopologyOutput } from '/@/api-services/videoWall';
import { BaseConfigTypeEnum } from '/@/types/enums/baseEnum';
import { useBaseConfig } from '/@/stores/baseConfig';
import { VwUpdateWallPermissionInput, VwScreenOutput, VwControllerOutput } from '/@/api-services/videoWall';
import { SysOrg, UserOutput } from '/@/api-services/system';
import { OperateType } from '/@/types/constants';
import { vwColorVar, vwCtrlCode } from '/@/stores/its/videoWallController';
import { cellLabel, useVideoWallTopology } from '/@/stores/its/videoWallTopology';
import VwInfoTip, { type VwTipRow } from '/@/components/videoWall/vwInfoTip.vue';
import WallStage, { type WallStageBlock } from '/@/components/videoWall/wallStage.vue';
import TransferPickList from '/@/components/pickList/transferPickList.vue';

const { t: $t } = useI18n();
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
/** Bố cục tường: mỗi khối một lưới chọn ô. */
const topology = useVideoWallTopology();

interface Props {
	/** the type of operation */
	operateType: OperateType;
	title: string;
	/** Danh sách màn hình (VwScreen) để dựng lưới */
	screens: VwScreenOutput[];
	/** Danh sách bộ điều khiển để tô viền theo màu bộ điều khiển */
	controllers: VwControllerOutput[];
	/** Danh sách người dùng cho ô chọn */
	users: UserOutput[];
	/** Cây đơn vị cho ô chọn */
	orgTree: SysOrg[];
}
const props = defineProps<Props>();
const emits = defineEmits(['handleQuery']);

const ruleFormRef = ref();
const state = reactive({
	isShowDialog: false,
	loading: false,
	saving: false,
	/** Tab đang mở: info = thông tin quyền, area = lưới chọn ô */
	activeTab: 'info' as 'info' | 'area',
	ruleForm: {} as VwUpdateWallPermissionInput,
	/** Loại đối tượng được gán: chỉ người dùng hoặc chỉ đơn vị. */
	actorType: 'user' as 'user' | 'org',
	/** Bản ghi cũ đang gán cả 2 loại ⇒ cảnh báo lưu sẽ chỉ giữ loại đang chọn. */
	hadBothActors: false,
	/** Bố cục của bản ghi nguồn (WallTopologyId) — để nhận ra nhân bản từ phiên bản bố cục cũ. */
	sourceTopologyId: null as string | null,
	/** Sửa quyền của bố cục khác bố cục đang dùng ⇒ bố cục đó (lưới vẽ theo nó); null = bố cục đang dùng. */
	layout: null as VwWallTopologyOutput | null,
	/** Ô đang được chọn, khoá "block,col,row" */
	selected: new Set<string>(),
	/** Kéo chọn hình chữ nhật trong 1 khối: ô bắt đầu, ô hiện tại, add = true thêm ô / false bỏ ô; null = không kéo */
	drag: null as null | { block: number; startCol: number; startRow: number; endCol: number; endRow: number; add: boolean },
});

/** Khối của lưới đang vẽ: bố cục của quyền khi sửa quyền bố cục khác, còn lại bố cục đang dùng. */
const blocks = computed(() => (state.layout ? [...(state.layout.blocks ?? [])].sort((a, b) => (a.blockNo ?? 0) - (b.blockNo ?? 0)) : topology.blocks));
const isCellInside = (block: number, row: number, col: number) => {
	const b = blocks.value.find((x) => x.blockNo === block);
	return !!b && row >= 0 && col >= 0 && row < (b.rows ?? 0) && col < (b.cols ?? 0);
};
const totalCells = computed(() => blocks.value.reduce((t, b) => t + (b.cols ?? 0) * (b.rows ?? 0), 0));
const isCopyFromOldLayout = computed(
	() => props.operateType === 'copy' && !!state.sourceTopologyId && !!topology.topology?.id && state.sourceTopologyId !== topology.topology.id
);

// ── Lưới chọn ô: sân khấu dùng chung WallStage (tự co giãn ô, zoom, kéo nền) ──
/** Khe giữa ô, giới hạn bề rộng ô ở mức vừa khung (px). */
const GAP = 4;
const CELL_MIN = 72;
const CELL_MAX = 160;

const stageBlocks = computed<WallStageBlock[]>(() => blocks.value.map((b) => ({ blockNo: b.blockNo ?? 0, cols: b.cols ?? 0, rows: b.rows ?? 0 })));

const cellKey = (block: number, col: number, row: number) => `${block},${col},${row}`;

// Tra màn hình theo vị trí lưới (khối, cột, hàng). Màn chỉ nằm trên bố cục đang dùng ⇒ lưới bố cục khác không có màn.
const screenMap = computed(() => {
	const map = new Map<string, VwScreenOutput>();
	if (state.layout) return map;
	for (const s of props.screens) if (s.gridCol != null && s.gridRow != null) map.set(cellKey(s.blockNo ?? 0, s.gridCol, s.gridRow), s);
	return map;
});
const screenAt = (block: number, col: number, row: number) => screenMap.value.get(cellKey(block, col, row));
const controllerOf = (block: number, col: number, row: number) => {
	const id = screenAt(block, col, row)?.controllerId;
	return id ? props.controllers.find((c) => c.id === id) : undefined;
};
/** Các dòng chú thích của 1 ô (ô trống chỉ có vị trí + trạng thái cấp). */
const cellTipRows = (block: number, col: number, row: number): VwTipRow[] => {
	const s = screenAt(block, col, row);
	const ctrl = controllerOf(block, col, row);
	return [
		{ icon: 'ele-CollectionTag', label: $t('lz.entity.base.code'), value: s?.code || $t('lz.label.videoWall.emptyCell') },
		{ icon: 'ele-Location', label: $t('lz.label.videoWall.windowPosition'), value: cellLabel(block, row, col, blocks.value.length > 1) },
		{
			icon: 'ele-Cpu',
			label: $t('lz.entity.videoWall.controller'),
			value: ctrl ? `${ctrl.code ? vwCtrlCode(ctrl.code) : ctrl.name}${s?.outputName ? ` · ${s.outputName}` : ''}` : $t('lz.label.videoWall.unassigned'),
			swatch: ctrl ? vwColorVar(ctrl.colorHex) : undefined,
			tone: ctrl ? undefined : 'warning',
			show: !!s,
		},
		{ icon: 'ele-Monitor', label: $t('lz.entity.videoWall.resolution'), value: s?.resolution, show: !!s?.resolution },
		{
			icon: 'ele-Lock',
			label: $t('lz.entity.videoWall.grantedArea'),
			value: state.selected.has(cellKey(block, col, row)) ? $t('lz.label.videoWall.selectedTitle') : $t('lz.label.videoWall.notGranted'),
		},
	];
};

// Chú thích: chỉ bộ điều khiển của màn ĐANG HIỆN trên lưới. Màn chưa đặt vào ô (gridCol / gridRow rỗng)
// hoặc nằm ngoài bố cục hiện tại (khối / ô đã bị cắt) không có trên lưới ⇒ không liệt kê bộ điều khiển của nó.
const legendControllers = computed(() => {
	const ids = new Set<string>();
	for (const [key, s] of screenMap.value) {
		const [block, col, row] = key.split(',').map(Number);
		if (s.controllerId && isCellInside(block, row, col)) ids.add(s.controllerId);
	}
	return props.controllers.filter((c) => c.id && ids.has(c.id));
});


/** Cây đơn vị trải phẳng cho PickList: cấp để thụt lề, đường dẫn cha để phân biệt đơn vị trùng tên. */
const orgItems = computed(() => {
	const out: { id: string; name: string; path: string; level: number; parentId: string | null }[] = [];
	const walk = (nodes: SysOrg[] | null | undefined, path: string[], level: number, parentId: string | null) => {
		for (const n of nodes ?? []) {
			if (n.id) out.push({ id: n.id, name: n.name ?? '', path: path.join(' / '), level, parentId });
			walk(n.children, [...path, n.name ?? ''], level + 1, n.id ?? parentId);
		}
	};
	walk(props.orgTree, [], 0, null);
	return out;
});

/** Icon đơn vị: có đơn vị con ⇒ toà nhà (cấp quản lý), không có ⇒ nhóm người (đội / tổ). */
const orgParentIds = computed(() => new Set(orgItems.value.map((o) => o.parentId).filter(Boolean)));
const orgIcon = (o: any) => (orgParentIds.value.has(o.id) ? 'ele-OfficeBuilding' : 'ele-Avatar');

/** Dòng phụ của người dùng: tài khoản · đơn vị (đường dẫn đầy đủ để phân biệt đơn vị trùng tên). */
const orgPathMap = computed(() => new Map(orgItems.value.map((o) => [o.id, o.path ? `${o.path} / ${o.name}` : o.name])));
const userSub = (u: UserOutput) => [u.username, (u.orgId && orgPathMap.value.get(u.orgId)) || u.orgName].filter(Boolean).join(' · ');

/** Chiều cao vùng cuộn của khung chọn người dùng / đơn vị (~4 dòng) — mặc định 260px quá to so với form. */
const PICK_HEIGHT = '190px';

const userPickRef = ref<InstanceType<typeof TransferPickList>>();
const orgPickRef = ref<InstanceType<typeof TransferPickList>>();

// ── Chọn ô ──────────────────────────────────────────────────────────
const setCell = (block: number, col: number, row: number, on: boolean) => {
	const key = cellKey(block, col, row);
	if (on) state.selected.add(key);
	else state.selected.delete(key);
};

const startDrag = (block: number, col: number, row: number, e: PointerEvent) => {
	if (e.button > 0) return;
	(e.target as Element)?.releasePointerCapture?.(e.pointerId);
	state.drag = { block, startCol: col, startRow: row, endCol: col, endRow: row, add: !state.selected.has(cellKey(block, col, row)) };
	window.addEventListener('pointerup', commitDrag, { once: true });
	// capture = true: chặn Esc trước khi el-dialog nhận (không đóng dialog khi chỉ huỷ lượt kéo)
	window.addEventListener('keydown', onDragKeydown, true);
};

// Vùng kéo chỉ trong 1 khối: đi qua khối khác thì bỏ qua
const dragOver = (block: number, col: number, row: number) => {
	if (!state.drag || state.drag.block !== block) return;
	state.drag.endCol = col;
	state.drag.endRow = row;
};

// Khối đang kéo (đã chuẩn hoá min / max)
const dragRect = computed(() => {
	const d = state.drag;
	if (!d) return null;
	return {
		colFrom: Math.min(d.startCol, d.endCol),
		colTo: Math.max(d.startCol, d.endCol),
		rowFrom: Math.min(d.startRow, d.endRow),
		rowTo: Math.max(d.startRow, d.endRow),
	};
});

const inPreview = (block: number, col: number, row: number) => {
	const rect = dragRect.value;
	return !!rect && state.drag?.block === block && col >= rect.colFrom && col <= rect.colTo && row >= rect.rowFrom && row <= rect.rowTo;
};

const commitDrag = () => {
	const d = state.drag;
	const rect = dragRect.value;
	stopDrag();
	if (!d || !rect) return;
	for (let c = rect.colFrom; c <= rect.colTo; c++) for (let r = rect.rowFrom; r <= rect.rowTo; r++) setCell(d.block, c, r, d.add);
};

const stopDrag = () => {
	state.drag = null;
	window.removeEventListener('pointerup', commitDrag);
	window.removeEventListener('keydown', onDragKeydown, true);
};

const onDragKeydown = (e: KeyboardEvent) => {
	if (e.key === 'Escape') {
		e.stopPropagation();
		stopDrag();
	}
};

const toggleCol = (block: number, col: number) => {
	const rows = blocks.value.find((b) => b.blockNo === block)?.rows ?? 0;
	const allOn = Array.from({ length: rows }, (_, r) => r).every((r) => state.selected.has(cellKey(block, col, r)));
	for (let r = 0; r < rows; r++) setCell(block, col, r, !allOn);
};

const toggleRow = (block: number, row: number) => {
	const cols = blocks.value.find((b) => b.blockNo === block)?.cols ?? 0;
	const allOn = Array.from({ length: cols }, (_, c) => c).every((c) => state.selected.has(cellKey(block, c, row)));
	for (let c = 0; c < cols; c++) setCell(block, c, row, !allOn);
};

const onRulerClick = (block: number, type: 'col' | 'row', index: number) => (type === 'col' ? toggleCol(block, index) : toggleRow(block, index));

const selectAll = () => {
	for (const b of blocks.value) for (let c = 0; c < (b.cols ?? 0); c++) for (let r = 0; r < (b.rows ?? 0); r++) setCell(b.blockNo ?? 0, c, r, true);
};

const clearAll = () => {
	state.selected.clear();
};

/** Toàn quyền = chọn đủ mọi ô của lưới: tick ⇒ chọn tất cả, bỏ tick ⇒ bỏ chọn. */
const isFullAccess = computed({
	get: () => totalCells.value > 0 && state.selected.size === totalCells.value,
	set: (on: boolean) => (on ? selectAll() : clearAll()),
});

onBeforeUnmount(() => stopDrag());

// ── Mở / đóng dialog ───────────────────────────────────────────────
// Phải chọn ít nhất 1 mục của loại đối tượng đang chọn (chỉ ô chọn đó được hiện)
const validateActors = (_rule: any, _value: any, callback: (error?: Error) => void) => {
	const ids = state.actorType === 'user' ? state.ruleForm.userIds : state.ruleForm.orgIds;
	if (ids?.length) callback();
	else
		callback(
			new Error($t('lz.validation.base.required', { PropertyName: $t(state.actorType === 'user' ? 'lz.entity.videoWall.user' : 'lz.entity.videoWall.org') }))
		);
};
const revalidateActors = () => {
	ruleFormRef.value?.validateField(state.actorType === 'user' ? 'userIds' : 'orgIds').catch(() => {});
};

// Open dialog
const openDialog = async (row: any) => {
	ruleFormRef.value?.resetFields();
	// Sửa quyền của bố cục khác bố cục đang dùng ⇒ nạp bố cục đó để vẽ lưới và lọc ô cho đúng
	state.layout = null;
	if (props.operateType === 'edit' && row.wallTopologyId && row.wallTopologyId !== topology.topology?.id) {
		const res = await getAPI(VwWallTopologyApi).apiVideowallVwwalltopologyByidGet(row.wallTopologyId).catch(() => null);
		state.layout = res?.data.result ?? null;
	}
	state.ruleForm = JSON.parse(JSON.stringify(row));
	state.ruleForm.userIds = state.ruleForm.userIds ?? [];
	state.ruleForm.orgIds = state.ruleForm.orgIds ?? [];
	// Dữ liệu cũ chưa có trạng thái ⇒ coi như Dùng
	state.ruleForm.status = state.ruleForm.status ?? StatusEnum.NUMBER_1;
	// Mặc định người dùng; bản ghi chỉ có đơn vị ⇒ mở tab đơn vị
	state.actorType = !state.ruleForm.userIds.length && state.ruleForm.orgIds.length ? 'org' : 'user';
	state.hadBothActors = state.ruleForm.userIds.length > 0 && state.ruleForm.orgIds.length > 0;
	state.sourceTopologyId = state.ruleForm.wallTopologyId ?? null;
	userPickRef.value?.reset();
	orgPickRef.value?.reset();
	state.selected = new Set<string>();
	state.activeTab = 'info';
	// Config dạng [{"Block":0,"Col":0,"Row":0},...] — cấu hình cũ chưa có Block thuộc khối đầu tiên
	try {
		// Toàn quyền ⇒ chọn đủ mọi ô của lưới (bản ghi cũ lưu Config = null / ô theo lưới cũ)
		if (state.ruleForm.isFullAccess) selectAll();
		const cells = state.ruleForm.config && !state.ruleForm.isFullAccess ? (JSON.parse(state.ruleForm.config) as any[]) : [];
		for (const c of cells ?? []) {
			const block = c.Block ?? c.block ?? 0;
			const col = c.Col ?? c.col;
			const r = c.Row ?? c.row;
			// Bỏ ô nằm ngoài bố cục tường (BE sẽ từ chối khi lưu)
			if (Number.isInteger(block) && Number.isInteger(col) && Number.isInteger(r) && isCellInside(block, r, col))
				state.selected.add(cellKey(block, col, r));
		}
	} catch {
		ElMessage.warning($t('lz.message.videoWall.oldConfigInvalid'));
	}
	state.isShowDialog = true;
};

// Close dialog
const closeDialog = () => {
	emits('handleQuery');
	state.isShowDialog = false;
};

// Cancel
const cancel = () => {
	state.isShowDialog = false;
};

// Submit
const submit = () => {
	ruleFormRef.value.validate(async (valid: boolean) => {
		// Lỗi nhập nằm ở tab Thông tin / chưa chọn ô ở tab Vùng ⇒ mở đúng tab để người dùng thấy
		if (!valid) {
			state.activeTab = 'info';
			return;
		}

		if (state.selected.size === 0) {
			state.activeTab = 'area';
			ElMessage.warning($t('lz.message.videoWall.noCellSelected'));
			return;
		}

		// Sắp theo Block, Row rồi Col, không trùng — khớp validator BE
		const cells = Array.from(state.selected)
			.map((k) => k.split(',').map(Number))
			.map(([block, col, row]) => ({ Block: block, Col: col, Row: row }))
			.sort((a, b) => a.Block - b.Block || a.Row - b.Row || a.Col - b.Col);

		// Chỉ gửi các trường nghiệp vụ; chuỗi rỗng ⇒ null
		const body: VwUpdateWallPermissionInput = {
			id: state.ruleForm.id,
			code: state.ruleForm.code?.trim() || null,
			name: state.ruleForm.name?.trim() || null,
			// Chỉ gửi loại đối tượng đang chọn, loại còn lại rỗng
			userIds: state.actorType === 'user' ? state.ruleForm.userIds ?? [] : [],
			orgIds: state.actorType === 'org' ? state.ruleForm.orgIds ?? [] : [],
			// Toàn quyền chỉ bật cờ, không lưu danh sách ô
			isFullAccess: isFullAccess.value,
			config: isFullAccess.value ? null : JSON.stringify(cells),
			remark: state.ruleForm.remark?.trim() || null,
			status: state.ruleForm.status ?? StatusEnum.NUMBER_1,
		};

		state.saving = true;
		try {
			//Edit
			if (props.operateType == 'edit') {
				await getAPI(VwWallPermissionApi).apiVideowallVwwallpermissionUpdatePost(body);
			}
			//Copy or Add
			else if (props.operateType == 'copy' || props.operateType == 'add') {
				body.id = '';
				await getAPI(VwWallPermissionApi).apiVideowallVwwallpermissionAddPost(body);
			}
			ElMessage.success($t('lz.message.base.saveSuccess'));
			closeDialog();
		} finally {
			state.saving = false;
		}
	});
};

// Export object
defineExpose({ openDialog });
</script>

<style scoped lang="scss">
@import '/@/theme/its/videoWallStage.scss';

/*
 * Tab Vùng được cấp: sân khấu tối như tab Bố cục tường; lưới cuộn riêng.
 * Tab Thông tin giữ chiều cao tự nhiên; dialog neo top 5vh nên chỉ dãn xuống dưới.
 * Chiều cao trừ phần đầu / tab / chân dialog (~210px) ⇒ cả dialog luôn nằm trong 90vh, nút xác nhận luôn thấy.
 */
.wp-area {
	@include vw-stage;
	display: flex;
	flex-direction: column;
	height: min(calc(90vh - 210px), 720px);
	min-height: 320px;
}
.wp-grid-header {
	display: flex;
	align-items: center;
	gap: 8px;
	padding: 8px 14px;
	border-bottom: 1px solid var(--wg-line-strong);
	background: var(--wg-panel);
	.wp-grid-count {
		padding-left: 12px;
		border-left: 1px solid var(--wg-line-strong);
		color: var(--wg-muted);
		font-size: 12px;
		font-variant-numeric: tabular-nums;
	}
}
/* Toàn quyền: chip trong suốt trên nền tối, bật thì viền / nền xanh nhấn */
.wp-full-access {
	height: 30px;
	margin-right: 0;
	padding: 0 12px 0 10px;
	border: 1px solid var(--wg-line-strong);
	border-radius: 8px;
	background: rgba(255, 255, 255, 0.04);
	font-size: 12px;
	color: var(--wg-muted);
	cursor: pointer;
	transition: border-color 0.15s, background 0.15s;
	&:hover {
		border-color: var(--wg-frame);
	}
	&.is-on {
		border-color: var(--wg-accent);
		background: rgba(59, 140, 255, 0.18);
		color: #fff;
	}
	:deep(.el-checkbox__label) {
		display: inline-flex;
		align-items: center;
		gap: 8px;
		font-size: 12px;
		color: inherit;
	}
	.wp-full-access-title {
		font-weight: 600;
		color: #fff;
	}
}
/* Chọn tất cả / Bỏ chọn: nhóm nút dính liền, trong suốt hợp sân khấu tối (thay nút trắng mặc định) */
.wp-grid-actions {
	display: inline-flex;
	border: 1px solid var(--wg-frame);
	border-radius: 8px;
	overflow: hidden;
}
.wp-grid-btn {
	display: inline-flex;
	align-items: center;
	gap: 6px;
	height: 28px;
	padding: 0 12px;
	border: none;
	background: rgba(255, 255, 255, 0.06);
	color: #fff;
	font-size: 12px;
	font-weight: 500;
	cursor: pointer;
	transition: background 0.15s, color 0.15s;
	& + & {
		border-left: 1px solid var(--wg-frame);
	}
	.el-icon {
		font-size: 13px;
	}
	&:hover:not(:disabled) {
		background: rgba(59, 140, 255, 0.35);
	}
	&:active:not(:disabled) {
		background: rgba(59, 140, 255, 0.5);
	}
	&:disabled {
		color: var(--wg-muted);
		opacity: 0.5;
		cursor: not-allowed;
	}
}
/* Sân khấu dùng chung nằm trong khung .wp-area (đã có nền) ⇒ bỏ bo góc / viền riêng; quyền toàn bộ ⇒ mờ, không chọn ô */
.wp-area > .wp-stage {
	flex: 1;
	min-height: 0;
	height: auto;
	border-radius: 0;
	box-shadow: none;
	user-select: none;
	touch-action: none;
}
.wp-cell {
	@include vw-stage-tile;
	position: relative;
	width: 100%;
	height: 100%;
	box-sizing: border-box;
	display: flex;
	flex-direction: column;
	gap: 2px;
	align-items: center;
	justify-content: center;
	padding: 4px;
	border-top-width: 3px;
	cursor: pointer;
	transition: background 0.1s, border-color 0.1s, box-shadow 0.1s;
	/* Ô chưa được cấp: tối mờ (đậm hơn nền sân khấu), lùi ra sau; ô được cấp (is-selected) cùng màu tấm màn Video Wall, nổi lên */
	background: rgba(21, 43, 76, 0.32);
	border-left-color: rgba(190, 215, 250, 0.3);
	border-right-color: rgba(190, 215, 250, 0.3);
	border-bottom-color: rgba(190, 215, 250, 0.3);
	box-shadow: none;
	color: rgba(240, 246, 255, 0.85);
	&:hover {
		border-left-color: rgba(140, 190, 255, 0.55);
		border-right-color: rgba(140, 190, 255, 0.55);
		border-bottom-color: rgba(140, 190, 255, 0.55);
	}
	/* Ô chưa có màn: nền trống nét đứt như ô trống ở tab Bố cục tường */
	&.is-empty {
		background: var(--wg-empty);
		border-left: 1px dashed var(--wg-empty-border);
		border-right: 1px dashed var(--wg-empty-border);
		border-bottom: 1px dashed var(--wg-empty-border);
		.wp-cell-name {
			color: var(--wg-muted);
		}
	}
	&.is-selected {
		background: linear-gradient(170deg, #1f3a63 0%, #152b4c 100%);
		border-left: 1px solid var(--wg-accent);
		border-right: 1px solid var(--wg-accent);
		border-bottom: 1px solid var(--wg-accent);
		box-shadow: 0 2px 8px rgba(3, 12, 30, 0.45);
		color: #fff;
		.wp-cell-name,
		.wp-cell-pos {
			color: #fff;
		}
	}
	&.is-preview-add {
		background: rgba(59, 140, 255, 0.3);
		border-left: 1px dashed var(--wg-accent);
		border-right: 1px dashed var(--wg-accent);
		border-bottom: 1px dashed var(--wg-accent);
	}
	&.is-preview-remove {
		background: rgba(245, 108, 108, 0.25);
		border-left: 1px dashed var(--el-color-danger);
		border-right: 1px dashed var(--el-color-danger);
		border-bottom: 1px dashed var(--el-color-danger);
	}
	.wp-cell-name {
		max-width: 100%;
		font-size: clamp(11px, calc(var(--cell-w, 96px) * 0.11), 14px);
		font-weight: 600;
		text-align: center;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}
	.wp-cell-pos {
		font-size: 10px;
		color: var(--wg-muted);
		white-space: nowrap;
		font-variant-numeric: tabular-nums;
	}
	.wp-cell-check {
		position: absolute;
		right: 3px;
		bottom: 3px;
		color: #fff;
	}
}
.wp-legend-caption {
	font-weight: 600;
	color: #fff;
}
.wp-legend {
	display: flex;
	flex-wrap: wrap;
	align-items: center;
	gap: 6px 14px;
	padding: 8px 14px;
	border-top: 1px solid var(--wg-line-strong);
	background: var(--wg-panel);
	font-size: 12px;
	color: var(--wg-muted);
	.wp-legend-item {
		display: inline-flex;
		align-items: center;
		gap: 6px;
		i {
			display: inline-block;
			width: 10px;
			height: 10px;
			border-radius: 3px;
		}
		/* Mẫu ô: cùng màu ô được cấp / chưa cấp trên lưới */
		.wp-legend-cell {
			width: 16px;
			border: 1px solid rgba(190, 215, 250, 0.35);
			background: rgba(21, 43, 76, 0.32);
			&.is-selected {
				border-color: var(--wg-accent);
				background: linear-gradient(170deg, #1f3a63 0%, #152b4c 100%);
			}
		}
	}
	.wp-legend-sep {
		width: 1px;
		height: 14px;
		background: var(--wg-line-strong);
	}
}
</style>
