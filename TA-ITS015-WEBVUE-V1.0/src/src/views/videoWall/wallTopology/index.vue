<template>
	<div class="videowall-topology-container">
		<!-- Mở qua tab menu hệ thống (mode) ⇒ ẩn thanh tab nội bộ, chỉ hiện đúng phần của tab đó -->
		<el-tabs v-model="state.activeTab" class="vw-topology-tabs" :class="{ 'is-single': !!props.mode }" :before-leave="onBeforeTabLeave" @tab-change="onTabChange">
			<!-- Bố cục tường: chọn 1 bố cục để xem / sửa lưới khối + kích thước ô -->
			<el-tab-pane name="layout">
				<template #label>
					<span class="vw-tab-label"><el-icon><ele-ArrowRight /></el-icon>{{ $t('lz.label.videoWall.currentLayoutTab') }}</span>
				</template>
				<div class="wt" v-loading="state.loading">
					<!-- Thanh trên — cùng bố cục với trang Giám sát & Điều khiển: tiêu đề / thẻ chọn bố cục · dải thông tin · thao tác -->
					<div class="wt-toolbar">
						<div class="wt-toolbar-header">
							<span class="wt-toolbar-title">
								<el-icon :size="18"><ele-Grid /></el-icon>
								<span>{{ $t('lz.label.videoWall.currentLayoutTab') }}</span>
							</span>
							<el-tag v-if="state.editing" type="warning" effect="light" round>
								<el-icon class="wt-editing-icon"><ele-EditPen /></el-icon>{{ $t('lz.label.videoWall.layoutEditing') }}
							</el-tag>
						</div>
						<div class="wt-toolbar-row">
							<div class="wt-toolbar-left">
								<!-- Chưa có bố cục ⇒ chỉ khởi tạo; có ⇒ chọn bố cục để xem / sửa lưới (lần đầu chọn sẵn bố cục đang dùng) -->
								<el-tag v-if="isDefault" type="warning" effect="light" round size="large">{{ $t('lz.label.videoWall.layoutNotInitialized') }}</el-tag>
								<el-popover v-else v-model:visible="state.pickerOpen" placement="bottom-start" :width="380" :offset="8" :show-arrow="false" trigger="click"
									popper-class="vw-layout-popover" :disabled="state.editing" @show="state.layoutKeyword = ''">
									<template #reference>
										<button type="button" class="wt-picker" :class="{ 'is-open': state.pickerOpen, 'is-disabled': state.editing }" :disabled="state.editing">
											<span class="wt-picker__icon" :class="{ 'is-live': !!selected && isInUse(selected) }"><el-icon><ele-Grid /></el-icon></span>
											<span class="wt-picker__text">
												<span class="wt-picker__label">{{ $t('lz.label.videoWall.selectLayout') }}</span>
												<span class="wt-picker__name">{{ selected ? `${selected.code} — ${selected.name}` : '—' }}</span>
											</span>
											<el-icon class="wt-picker__arrow"><ele-ArrowDown /></el-icon>
										</button>
									</template>
									<div class="wt-pick-panel">
										<div class="wt-pick-panel__head">
											<div class="wt-pick-panel__title-row">
												<span class="wt-pick-panel__title">{{ $t('lz.label.videoWall.layoutSwitchTitle') }}</span>
												<span class="wt-pick-panel__summary">{{ $t('lz.label.videoWall.layoutCountN', { count: state.layouts.length }) }}</span>
											</div>
											<!-- Tìm theo tên / mã bố cục, không phân biệt dấu — cùng kiểu ô chọn kịch bản (components/videoWall/scenePicker.vue) -->
											<el-input v-model="state.layoutKeyword" clearable prefix-icon="ele-Search" class="wt-pick-panel__search"
												:placeholder="$t('lz.label.videoWall.layoutSearchPlaceholder')" />
										</div>
										<div class="wt-pick-panel__body">
											<div v-if="!filteredLayouts.length" class="wt-pick-panel__empty">
												<el-icon><ele-Search /></el-icon>
												<span>{{ $t('lz.label.base.noData') }}</span>
											</div>
											<button v-for="l in filteredLayouts" :key="l.id!" type="button" class="wt-pick-item" :class="{ 'is-active': l.id === state.selectedId }"
												@click="onPickLayout(l.id!)">
												<!-- Tên / mã bên trái, lưới thu nhỏ dồn sang phải -->
												<span class="wt-pick-item__text">
													<span class="wt-pick-item__name">{{ l.name || l.code }}</span>
													<span class="wt-pick-item__meta">{{ l.code }} · {{ ratioOf(l) }}</span>
												</span>
												<span v-if="isInUse(l)" class="wt-pick-item__live"><i />{{ $t('lz.label.videoWall.currentLayout') }}</span>
												<span class="wt-pick-item__grid"><TopologyMiniGrid :blocks="l.blocks" :cell-ratio="ratioOf(l)" /></span>
												<el-icon v-if="l.id === state.selectedId" class="wt-pick-item__check"><ele-CircleCheckFilled /></el-icon>
											</button>
										</div>
									</div>
								</el-popover>

								<!-- Dải thông tin bố cục: icon · nhãn · giá trị, ngăn bằng vạch dọc -->
								<div class="wt-meta">
									<div class="wt-meta-item">
										<span class="wt-meta-item__icon"><el-icon><ele-Files /></el-icon></span>
										<span class="wt-meta-item__label">{{ $t('lz.label.videoWall.blockCount') }}</span>
										<span class="wt-meta-item__value">{{ state.blocks.length }}</span>
									</div>
									<div class="wt-meta-item">
										<span class="wt-meta-item__icon"><el-icon><ele-Grid /></el-icon></span>
										<span class="wt-meta-item__label">{{ $t('lz.label.videoWall.cellCount') }}</span>
										<!-- Có số ô ⇒ "tổng ô lưới / số ô", lệch thì tô cảnh báo -->
										<span class="wt-meta-item__value" :class="{ 'is-mismatch': cellMismatch }">{{ cellTarget != null ? `${cellTotal}/${cellTarget}` : cellTotal }}</span>
									</div>
									<div class="wt-meta-item">
										<span class="wt-meta-item__icon"><i class="wt-meta-ratio" :style="ratioShapeStyle(state.cellRatio, 14)" /></span>
										<span class="wt-meta-item__label">{{ $t('lz.entity.videoWall.cellRatio') }}</span>
										<span class="wt-meta-item__value">{{ state.cellRatio }}</span>
									</div>
									<div class="wt-meta-item">
										<span class="wt-meta-item__icon"><el-icon><ele-Film /></el-icon></span>
										<span class="wt-meta-item__label">{{ $t('lz.label.videoWall.sceneCount') }}</span>
										<span class="wt-meta-item__value">{{ sceneCount }}</span>
									</div>
									<div v-if="selected" class="wt-meta-item">
										<span class="wt-meta-item__icon" :class="isInUse(selected) ? 'is-success' : 'is-info'">
											<el-icon><ele-CircleCheck v-if="isInUse(selected)" /><ele-Remove v-else /></el-icon>
										</span>
										<span class="wt-meta-item__label">{{ $t('lz.label.base.status') }}</span>
										<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, selected.status)" />
									</div>
								</div>
							</div>

							<span class="wt-spacer" />

							<div class="wt-toolbar-right">
								<template v-if="state.editing">
									<el-button round :disabled="state.saving" @click="onCancel">
										<el-icon><ele-Close /></el-icon> {{ $t('lz.button.base.cancel') }}
									</el-button>
									<el-button type="primary" round :loading="state.saving" :disabled="(!isDefault && !isDirty) || cellMismatch" @click="onSave">
										<el-icon><ele-Check /></el-icon> {{ isDefault ? $t('lz.button.videoWall.initLayout') : $t('lz.button.videoWall.saveLayout') }}
									</el-button>
								</template>
								<el-button v-else type="primary" round v-auth="'videoWallTopology:update'" @click="onStartEdit">
									<el-icon><ele-Plus v-if="isDefault" /><ele-Edit v-else /></el-icon>
									{{ isDefault ? $t('lz.button.videoWall.initLayout') : $t('lz.button.videoWall.editLayout') }}
								</el-button>
							</div>
						</div>
					</div>

					<div ref="bodyRef" class="wt-body">
						<!-- Cột trái: gán bộ hiển thị / độc lập vào ô — chọn bộ rồi kéo trên lưới. Một khối có thể có nhiều bộ, mỗi ô một bộ.
						     Mở cột này ⇒ cột cấu hình bên phải tự thu gọn (và ngược lại) -->
						<aside :id="ctrlPanel.id" class="wt-side" :style="ctrlPanel.panelStyle">
							<div class="wt-side-head">{{ $t('lz.label.videoWall.blockControllers') }}</div>
							<div class="wt-side-body">
								<p class="wt-hint">{{ state.editing ? $t('lz.label.videoWall.blockControllersHint') : $t('lz.label.videoWall.blockControllersView') }}</p>
								<div class="wt-ctrl-list">
									<!-- Icon bộ điều khiển (cùng icon menu Bộ điều khiển) trên nền màu của bộ = màu ô trên lưới -->
									<button v-for="c in displayControllers" :key="c.id!" type="button" class="wt-ctrl" :class="{ 'is-active': state.editing && state.brush === c.id }"
										:style="{ '--ctrl-color': vwCtrl.colorOf(c.id) }" :disabled="!state.editing" @click="state.brush = c.id!">
										<span class="wt-ctrl-icon"><el-icon><ele-Cpu /></el-icon></span>
										<span class="wt-ctrl-text">
											<b>{{ vwCtrlCode(c.code) }}</b>
											<span class="wt-ctrl-name">{{ c.name }}</span>
										</span>
										<small class="wt-ctrl-count">{{ $t('lz.label.videoWall.cellCountN', { count: ownedCount(c.id!) }) }}</small>
									</button>
									<button v-if="state.editing" type="button" class="wt-ctrl wt-ctrl--eraser" :class="{ 'is-active': state.brush === ERASER }" @click="state.brush = ERASER">
										<span class="wt-ctrl-icon"><el-icon><ele-Delete /></el-icon></span>
										<span class="wt-ctrl-name">{{ $t('lz.label.videoWall.clearCellController') }}</span>
									</button>
									<el-empty v-if="!displayControllers.length" :image-size="48" :description="$t('lz.label.videoWall.noDisplayController')" />
								</div>
							</div>
						</aside>
						<PanelSplitter side="left" class="wt-splitter" :label="$t('lz.label.videoWall.blockControllers')" :controls="ctrlPanel.id"
							:width="ctrlPanel.size" :min="ctrlPanel.min" :max="ctrlPanel.max" :expanded="ctrlPanel.visible"
							:expand-label="$t('lz.label.base.extend')" :collapse-label="$t('lz.button.base.collapse')"
							:resize-hint="$t('lz.label.base.resizePanel', { name: $t('lz.label.videoWall.blockControllers') })"
							@resize="ctrlPanel.resize" @step="ctrlPanel.step" />

						<!-- Lưới ô: chỉ vị trí hàng / cột, không vẽ màn hình -->
						<div class="wt-canvas">
							<WallStage :blocks="stageBlocks" :cell-ratio="state.cellRatio" :gap="GAP" rulers block-name="always" :min-cell-w="CELL_MIN"
								:max-cell-w="CELL_MAX" :guide="$t('lz.label.videoWall.cellLabelHint')">
								<template #block-meta="{ cols, rows }">
									<span class="ws-block-meta">{{ $t('lz.label.videoWall.gridColsRows', { cols, rows }) }}</span>
								</template>
								<!-- Ô tô màu theo bộ điều khiển phụ trách (ControllerConfig). Đang sửa: kéo chọn vùng ⇒ gán bộ đang chọn / xoá gán -->
								<template #cell="{ blockNo, row, col }">
									<!-- Cùng kiểu ô chọn của Phân quyền vùng: viền trên màu bộ, ô đã gán nổi lên, kéo ⇒ xem trước thêm (xanh) / xoá (đỏ) -->
									<div class="wt-cell"
										:class="{
											'is-new': isNewCell(blockNo, row, col),
											'is-owned': !!ownerOf(blockNo, col, row),
											'is-paint': state.editing && !!state.brush,
											'is-preview-add': inPreview(blockNo, col, row) && state.brush !== ERASER,
											'is-preview-remove': inPreview(blockNo, col, row) && state.brush === ERASER,
										}"
										:style="cellStyle(blockNo, col, row)" @pointerdown.prevent="startDrag(blockNo, col, row)" @pointerenter="dragOver(blockNo, col, row)">
										<span class="wt-cell-name">{{ ownerOf(blockNo, col, row) ? vwCtrlCode(vwCtrl.find(ownerOf(blockNo, col, row))?.code) : '—' }}</span>
										<span class="wt-cell-pos">{{ cellLabel(blockNo, row, col, multiBlock) }}</span>
										<el-icon v-if="ownerOf(blockNo, col, row)" class="wt-cell-check"><ele-Check /></el-icon>
									</div>
								</template>
							</WallStage>
						</div>

						<!-- Cột cấu hình bên phải: kéo đổi rộng / thu gọn -->
						<PanelSplitter side="right" class="wt-splitter" :label="$t('lz.label.videoWall.layoutConfig')" :controls="sidePanel.id"
							:width="sidePanel.size" :min="sidePanel.min" :max="sidePanel.max" :expanded="sidePanel.visible"
							:expand-label="$t('lz.label.base.extend')" :collapse-label="$t('lz.button.base.collapse')"
							:resize-hint="$t('lz.label.base.resizePanel', { name: $t('lz.label.videoWall.layoutConfig') })"
							@resize="sidePanel.resize" @step="sidePanel.step" />
						<aside :id="sidePanel.id" class="wt-side" :style="sidePanel.panelStyle">
							<div class="wt-side-head">{{ $t('lz.label.videoWall.layoutConfig') }}</div>
							<div class="wt-side-body">
								<!-- Số ô của bố cục (cấu hình ở danh sách / sửa tại đây): tổng cột × hàng các khối phải đúng bằng số này mới lưu được -->
								<div class="wt-cell-count" :class="cellTarget == null ? 'is-unset' : cellTotal === cellTarget ? 'is-ok' : 'is-mismatch'">
									<div class="wt-cell-count__head">
										<span class="wt-cell-count__icon"><el-icon><ele-Grid /></el-icon></span>
										<span class="wt-cell-count__text">
											<b>{{ $t('lz.entity.videoWall.cellCount') }}</b>
											<small>{{ $t('lz.label.videoWall.cellCountHint') }}</small>
										</span>
										<span class="wt-cell-count__value">{{ cellTarget != null ? `${cellTotal}/${cellTarget}` : $t('lz.label.videoWall.cellCountUnset') }}</span>
									</div>
									<div v-if="cellTarget != null" class="wt-cell-count__bar">
										<i :style="{ width: `${Math.min(100, (cellTotal / cellTarget) * 100)}%` }" />
									</div>
									<el-input-number v-if="state.editing" v-model="state.cellCount" :min="1" :max="MAX_CELL_COUNT" :step="1" step-strictly controls-position="right"
										class="w100" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.videoWall.cellCount') })" />
								</div>
								<div v-for="(b, i) in state.blocks" :key="i" class="wt-block">
									<div class="wt-block-head">
										<span class="wt-badge">{{ blockLabel(i) }}</span>
										<b>{{ $t('lz.label.videoWall.blockName', { name: blockLabel(i) }) }}</b>
										<span class="wt-block-count">{{ $t('lz.label.videoWall.cellCountN', { count: b.cols * b.rows }) }}</span>
										<!-- Đổi chỗ khối (= vị trí trái → phải trên tường) / xoá khối. BE đánh số lại khối cho màn, cửa sổ, phân quyền;
										     khối bị xoá còn dữ liệu thì BE chặn khi lưu -->
										<span v-if="state.editing" class="wt-block-actions">
											<el-button text size="small" icon="ele-ArrowUp" :disabled="i === 0" @click="onMoveBlock(i, -1)" />
											<el-button text size="small" icon="ele-ArrowDown" :disabled="i === state.blocks.length - 1" @click="onMoveBlock(i, 1)" />
											<el-button text type="danger" size="small" icon="ele-Delete" :disabled="state.blocks.length <= 1" @click="onRemoveBlock(i)" />
										</span>
									</div>
									<!-- Có số ô ⇒ khối chỉ nhận phần ô còn lại sau khi trừ các khối khác -->
									<GridSizePicker :cols="b.cols" :rows="b.rows" :max-cols="MAX_BLOCK_COLS" :max-rows="MAX_BLOCK_ROWS"
										:max-count="cellTarget != null ? cellTarget - (cellTotal - b.cols * b.rows) : null" :cell-ratio="state.cellRatio" :disabled="!state.editing"
										@change="(c: number, r: number) => onGridPick(i, c, r)" />
								</div>
								<el-button v-if="state.editing" class="w100" plain icon="ele-Plus"
									:disabled="state.blocks.length >= MAX_BLOCKS || (cellTarget != null && cellTotal >= cellTarget)" @click="onAddBlock">
									{{ $t('lz.button.videoWall.addBlock') }}
								</el-button>
								<p v-if="state.editing && cellMismatch" class="wt-hint is-warning">{{ $t('lz.message.videoWall.cellCountMismatch', { count: cellTotal, total: cellTarget }) }}</p>

								<el-divider />
								<div class="wt-section-title">{{ $t('lz.entity.videoWall.cellRatio') }}</div>
								<!-- Chọn tỉ lệ ⇒ đổi hình dạng ô trên lưới; toạ độ cửa sổ kịch bản không đổi -->
								<div class="wt-ratio">
									<button v-for="r in CELL_RATIOS" :key="r" type="button" class="wt-ratio-item" :class="{ 'is-active': state.cellRatio === r }"
										:disabled="!state.editing || cellRatioLocked" @click="state.cellRatio = r">
										<i class="wt-ratio-shape" :style="ratioShapeStyle(r)" />
										<b>{{ r }}</b>
										<small>{{ $t(RATIO_LABEL[r]) }}</small>
									</button>
								</div>
								<p v-if="cellRatioLocked" class="wt-hint">{{ $t('lz.message.videoWall.cellRatioLocked', { count: sceneCount }) }}</p>
							</div>

							<!-- Quy tắc sửa -->
							<div class="wt-rules">
								<div class="wt-rule"><el-icon><ele-CircleCheck /></el-icon><span>{{ $t('lz.label.videoWall.layoutRuleExpand') }}</span></div>
								<div class="wt-rule"><el-icon><ele-Warning /></el-icon><span>{{ $t('lz.label.videoWall.layoutRuleShrink') }}</span></div>
							</div>
						</aside>
					</div>
				</div>
			</el-tab-pane>
			<!-- Danh sách bố cục: thêm / sửa / nhân bản / xoá / đặt làm bố cục đang dùng -->
			<el-tab-pane name="list" lazy>
				<template #label>
					<span class="vw-tab-label"><el-icon><ele-ArrowRight /></el-icon>{{ $t('lz.label.videoWall.layoutListTab') }}</span>
				</template>
				<div class="wt-list">
					<GridSearchPanel id="videowall-topology-search">
						<template #filters>
						<el-form :model="state.queryParams" :show-message="false" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%" label-position="left">
							<el-row :gutter="10">
								<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
									<el-form-item :label="$t('lz.label.videoWall.codeOrName')">
										<el-input v-model="state.queryParams.keyword" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.label.videoWall.codeOrName') })" clearable
											@keyup.enter.native="handleQuery" />
									</el-form-item>
								</el-col>
							</el-row>
						</el-form>
						<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />
						<el-row>
							<SearchButton :auth-search="'videoWallTopology:page'" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
						</el-row>
						</template>

					<el-card class="full-table" shadow="hover">
						<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options">
							<template #toolbar_tools>
								<TableHeader @add="handleAdd" :show-delete="false" :auth-add="'videoWallTopology:add'" @refresh="handleQuery" @custom-column="xGrid?.openCustom()" />
							</template>
							<template #empty>
								<el-empty :image-size="200" />
							</template>
							<!-- Bật / tắt Dùng – Không dùng; bố cục đang áp dụng luôn Dùng nên khoá switch -->
							<template #row_status="{ row }">
								<el-switch :model-value="row.status === StatusEnum.NUMBER_1" :disabled="isInUse(row)" :loading="state.toggling === row.id"
									v-auth="'videoWallTopology:update'" @change="(on: any) => toggleStatus(row, !!on)" />
							</template>
							<!-- Đang áp dụng lên tường (cờ IsApply) -->
							<template #row_apply="{ row }">
								<el-tag v-if="isInUse(row)" size="small" type="success">{{ $t('lz.label.videoWall.currentLayout') }}</el-tag>
							</template>
							<template #row_blocks="{ row }">
								<div class="wt-blocks-cell">
									<TopologyMiniGrid :blocks="row.blocks" :cell-ratio="ratioOf(row)" />
									<!-- <span class="wt-blocks-text">{{ blocksText(row) }}</span> -->
								</div>
							</template>
							<template #row_record="{ row }">
								<ModifyRecord :data="row" />
							</template>
							<template #row_buttons="{ row }">
								<el-tooltip :content="$t('lz.button.videoWall.apply')" placement="top" :auto-close="1000">
									<el-button icon="ele-CircleCheck" text type="success" :disabled="isInUse(row)" v-auth="'videoWallTopology:setCurrent'" @click="handleSetCurrent(row)" />
								</el-tooltip>
								<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
									<el-button icon="ele-CopyDocument" text type="warning" v-auth="'videoWallTopology:add'" @click="handleCopy(row)" />
								</el-tooltip>
								<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
									<el-button icon="ele-Edit" text type="primary" v-auth="'videoWallTopology:update'" @click="handleEdit(row)" />
								</el-tooltip>
								<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
									<el-button icon="ele-Delete" text type="danger" :disabled="isInUse(row)" v-auth="'videoWallTopology:delete'" @click="handleDelete(row)" />
								</el-tooltip>
							</template>
						</vxe-grid>
					</el-card>
					</GridSearchPanel>
				</div>
			</el-tab-pane>
		</el-tabs>

		<EditWallTopology ref="editRef" :title="state.editTitle" :operate-type="state.operateType" @handle-query="onListChanged" />
	</div>
</template>

<script lang="ts" setup name="videoWallTopology">
import { computed, defineAsyncComponent, onActivated, onMounted, reactive, ref, watch } from 'vue';
import GridSearchPanel from '/@/components/gridPage/GridSearchPanel.vue';
import { onBeforeRouteLeave } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import { useI18n } from 'vue-i18n';
import type { VxeGridInstance } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { getAPI } from '/@/utils/axios-utils';
import { DefaultEnum, StatusEnum, VwSceneApi, VwWallTopologyApi, YesNoEnum } from '/@/api-services/videoWall';
import type { VwWallTopologyOutput } from '/@/api-services/videoWall';
import type { OperateType } from '/@/types/constants/operate';
import { blockLabel, CELL_RATIOS, cellLabel, cellRatioParts, DEFAULT_CELL_RATIO, useVideoWallTopology } from '/@/stores/its/videoWallTopology';
import WallStage, { type WallStageBlock } from '/@/components/videoWall/wallStage.vue';
import { useVideoWallController, vwCtrlCode, VW_ROLE } from '/@/stores/its/videoWallController';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import GridSizePicker from '../screens/component/gridSizePicker.vue';
import { MAX_BLOCKS } from '../screens/component/wallLayoutUtils';
import EditWallTopology from './component/editWallTopology.vue';
import TopologyMiniGrid from './component/topologyMiniGrid.vue';
import PanelSplitter from '/@/components/workspace/PanelSplitter.vue';
import { useSidePanel } from '/@/components/workspace/useSidePanel';

const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));

/**
 * mode: trang được mở như 1 tab của menu "Bố cục tường" (menu dùng /tabPage/index):
 * 'layout' = chỉ phần xem / sửa lưới, 'list' = chỉ danh sách bố cục. Không truyền ⇒ 2 tab nội bộ như cũ.
 */
const props = defineProps<{ mode?: 'layout' | 'list' }>();

const { t: $t } = useI18n();
const topology = useVideoWallTopology();
const vwCtrl = useVideoWallController();

/** Cột cấu hình bên phải của tab Bố cục: kéo đổi rộng / thu gọn (chừa chỗ cho sơ đồ lưới). */
const bodyRef = ref<HTMLElement>();
const sidePanel = useSidePanel('vw-topology-side', bodyRef, { width: 340, min: 260, maxRatio: 0.45, reserve: 480 });
/** Cột bộ điều khiển bên trái — chỉ mở 1 trong 2 cột: mở cột này ⇒ thu cột cấu hình, và ngược lại. Mặc định mở cột cấu hình. */
const ctrlPanel = useSidePanel('vw-topology-ctrl', bodyRef, { width: 300, min: 240, maxRatio: 0.4, reserve: 480 });
ctrlPanel.resize(0);
watch(() => ctrlPanel.visible, (open) => open && sidePanel.resize(0));
watch(() => sidePanel.visible, (open) => open && ctrlPanel.resize(0));
const xGrid = ref<VxeGridInstance>();
const editRef = ref<InstanceType<typeof EditWallTopology>>();

/** Lưới tối đa 1 khối — khớp BE VwWallProfile.MaxBlockCols / MaxBlockRows. */
const MAX_BLOCK_COLS = 32;
const MAX_BLOCK_ROWS = 16;
/** Số ô tối đa của 1 bố cục — khớp BE VwWallProfile.MaxCellCount. */
const MAX_CELL_COUNT = 100;
/** Khối thêm mới mặc định. */
const NEW_BLOCK = { cols: 4, rows: 2 };
const CELL_MIN = 56;
const CELL_MAX = 168;
const GAP = 4;

interface EditBlock {
	cols: number;
	rows: number;
	/** Số khối đã lưu của khối này (null = khối mới) — gửi BE để đánh số lại khi đổi chỗ / xoá khối. */
	source: number | null;
	/** Ô → bộ điều khiển phụ trách: khoá "col,row" (VwWallBlock.ControllerConfig). */
	cells: Record<string, string>;
}

/** Cọ "xoá gán" khi kéo chọn ô. */
const ERASER = '__erase__';
const cellKey = (col: number, row: number) => `${col},${row}`;

/** ControllerConfig (JSON [{Col,Row,ControllerId}]) → map ô; hỏng định dạng ⇒ rỗng. */
const parseCells = (json?: string | null): Record<string, string> => {
	const map: Record<string, string> = {};
	if (!json) return map;
	try {
		for (const c of (JSON.parse(json) as any[]) ?? []) {
			const id = c.ControllerId ?? c.controllerId;
			if (id) map[cellKey(c.Col ?? c.col, c.Row ?? c.row)] = id;
		}
	} catch {
		// cấu hình lỗi: coi như chưa gán
	}
	return map;
};
/** Map ô → JSON gửi BE (bỏ ô ngoài khối); không còn ô ⇒ "[]" để BE xoá cấu hình. */
const toControllerConfig = (b: EditBlock) =>
	JSON.stringify(
		Object.entries(b.cells)
			.map(([k, id]) => {
				const [col, row] = k.split(',').map(Number);
				return { Col: col, Row: row, ControllerId: id };
			})
			.filter((c) => c.Col < b.cols && c.Row < b.rows)
			.sort((a, z) => a.Row - z.Row || a.Col - z.Col)
	);

const state = reactive({
	/** Tab đang mở: bố cục đang dùng (lưới) hoặc danh sách bố cục (bảng). */
	activeTab: (props.mode ?? 'layout') as 'layout' | 'list',
	queryParams: { keyword: '' },
	editTitle: '',
	operateType: 'add' as OperateType,
	/** Tab Bố cục tường: danh sách bố cục + bố cục đang chọn để xem / sửa lưới. */
	layouts: [] as VwWallTopologyOutput[],
	selectedId: '',
	loading: false,
	saving: false,
	/** Mặc định chỉ xem; bấm "Chỉnh sửa / Khởi tạo bố cục" mới sửa được. */
	editing: false,
	blocks: [] as EditBlock[],
	/** Tỉ lệ ô (hình dạng ô khi vẽ), VD "16:9". */
	cellRatio: DEFAULT_CELL_RATIO as string,
	/** Số ô của bố cục (null = chưa giới hạn) — sửa cùng lưới. */
	cellCount: null as number | null,
	/** Panel chọn bố cục đang mở. */
	pickerOpen: false,
	/** Từ khoá tìm trong panel chọn bố cục. */
	layoutKeyword: '',
	/** ID bố cục đang lưu switch Trạng thái. */
	toggling: '',
	/** Bộ điều khiển đang chọn để gán vào ô (ERASER = xoá gán). */
	brush: '' as string,
	/** Đang kéo chọn ô để gán bộ. */
	drag: null as null | { block: number; startCol: number; startRow: number; endCol: number; endRow: number },
});

/** Nhãn mô tả từng tỉ lệ ô. */
const RATIO_LABEL: Record<string, string> = { '16:9': 'lz.label.videoWall.cellRatioLandscape', '3:4': 'lz.label.videoWall.cellRatioPortrait' };
/** Hình minh hoạ tỉ lệ ô: cạnh dài `size` px. */
const ratioShapeStyle = (ratio: string, size = 22) => {
	const r = cellRatioParts(ratio) ?? { rw: 16, rh: 9 };
	const k = size / Math.max(r.rw, r.rh);
	return { width: `${r.rw * k}px`, height: `${r.rh * k}px` };
};
/** Tỉ lệ ô của 1 bố cục (DTO chưa có trường ⇒ mặc định). */
const ratioOf = (l?: VwWallTopologyOutput | null) => l?.cellRatio || DEFAULT_CELL_RATIO;

/** Chưa có bố cục nào ⇒ chỉ khởi tạo (bố cục đầu tiên tự thành bố cục đang dùng). */
const isDefault = computed(() => !state.layouts.length);
const selected = computed(() => state.layouts.find((l) => l.id === state.selectedId) ?? null);
/** Bỏ dấu tiếng Việt để tìm "bo cuc" ra "Bố cục". */
const normalize = (text: unknown) =>
	String(text ?? '')
		.toLowerCase()
		.normalize('NFD')
		.replace(/[̀-ͯ]/g, '')
		.replace(/đ/g, 'd');
const filteredLayouts = computed(() => {
	const key = normalize(state.layoutKeyword.trim());
	return key ? state.layouts.filter((l) => normalize(l.name).includes(key) || normalize(l.code).includes(key)) : state.layouts;
});
const sceneCount = computed(() => selected.value?.sceneCount ?? 0);
/** Cửa sổ kịch bản đã dựng theo hình dạng ô ⇒ bố cục (dùng hay không) đã có kịch bản thì không đổi tỉ lệ ô — khớp BE. */
const cellRatioLocked = computed(() => !!selected.value && sceneCount.value > 0);
const multiBlock = computed(() => state.blocks.length > 1);
const cellTotal = computed(() => state.blocks.reduce((t, b) => t + b.cols * b.rows, 0));
/** Số ô đang nhập (null = chưa giới hạn); đang sửa lưới thì tổng ô phải đúng bằng số này mới lưu được — khớp BE. */
const cellTarget = computed(() => state.cellCount ?? null);
const cellMismatch = computed(() => state.editing && cellTarget.value != null && cellTotal.value !== cellTarget.value);const stageBlocks = computed<WallStageBlock[]>(() => state.blocks.map((b, i) => ({ blockNo: i, cols: b.cols, rows: b.rows })));

/** Lưới đã lưu của bố cục đang chọn (chưa có bố cục ⇒ lưới mặc định của BE) — đánh dấu ô mới thêm khi đang sửa. */
const savedBlocks = computed<EditBlock[]>(() =>
	[...(selected.value?.blocks?.length ? selected.value.blocks : topology.blocks)]
		.sort((a, b) => (a.blockNo ?? 0) - (b.blockNo ?? 0))
		.map((b, i) => ({ cols: b.cols ?? 1, rows: b.rows ?? 1, source: b.blockNo ?? i, cells: parseCells(b.controllerConfig) }))
);

/* ── Gán bộ điều khiển vào ô ── */

/** Bộ có màn (hiển thị / độc lập), đang dùng ⇒ được gán vào ô. */
const displayControllers = computed(() =>
	vwCtrl.controllers.filter((c) => (c.role === VW_ROLE.Display || c.role === VW_ROLE.Standalone) && Number(c.status ?? 1) === 1)
);
const ownerOf = (block: number, col: number, row: number) => state.blocks[block]?.cells[cellKey(col, row)];
const ownedCount = (id: string) => state.blocks.reduce((t, b) => t + Object.entries(b.cells).filter(([k, v]) => {
	const [c, r] = k.split(',').map(Number);
	return v === id && c < b.cols && r < b.rows;
}).length, 0);
/** Ô đã gán: tô đặc màu bộ phụ trách. Đang kéo qua ô ⇒ nhường cho màu xem trước. */
const cellStyle = (block: number, col: number, row: number) => {
	const id = ownerOf(block, col, row);
	if (!id || inPreview(block, col, row)) return undefined;
	const color = vwCtrl.colorOf(id);
	return { background: color, borderColor: `color-mix(in srgb, ${color} 75%, #000)` };
};

const dragRect = computed(() => {
	const d = state.drag;
	if (!d) return null;
	return { colFrom: Math.min(d.startCol, d.endCol), colTo: Math.max(d.startCol, d.endCol), rowFrom: Math.min(d.startRow, d.endRow), rowTo: Math.max(d.startRow, d.endRow) };
});
const inPreview = (block: number, col: number, row: number) => {
	const r = dragRect.value;
	return !!r && state.drag?.block === block && col >= r.colFrom && col <= r.colTo && row >= r.rowFrom && row <= r.rowTo;
};
/** Bắt đầu kéo (chỉ khi đang sửa và đã chọn bộ / cọ xoá). */
const startDrag = (block: number, col: number, row: number) => {
	if (!state.editing || !state.brush) return;
	state.drag = { block, startCol: col, startRow: row, endCol: col, endRow: row };
	window.addEventListener('pointerup', endDrag, { once: true });
};
const dragOver = (block: number, col: number, row: number) => {
	if (!state.drag || state.drag.block !== block) return;
	state.drag.endCol = col;
	state.drag.endRow = row;
};
/** Thả chuột ⇒ gán bộ đang chọn (hoặc xoá gán) cho cả vùng vừa kéo. */
function endDrag() {
	const d = state.drag;
	const r = dragRect.value;
	state.drag = null;
	const b = d ? state.blocks[d.block] : undefined;
	if (!d || !r || !b) return;
	const cells = { ...b.cells };
	for (let c = r.colFrom; c <= r.colTo; c++)
		for (let w = r.rowFrom; w <= r.rowTo; w++) {
			if (state.brush === ERASER) delete cells[cellKey(c, w)];
			else cells[cellKey(c, w)] = state.brush;
		}
	state.blocks[d.block] = { ...b, cells };
}
/** Ô mới thêm khi đang sửa — so với lưới đã lưu của CHÍNH khối đó (khối có thể đã đổi chỗ). */
const isNewCell = (block: number, row: number, col: number) => {
	if (!state.editing || isDefault.value) return false;
	const source = state.blocks[block]?.source;
	const b = source == null ? undefined : savedBlocks.value.find((s) => s.source === source);
	return !b || row >= b.rows || col >= b.cols;
};

const sig = () => JSON.stringify([state.blocks.map((b) => [b.cols, b.rows, b.source, toControllerConfig(b)]), state.cellRatio, state.cellCount]);
let baseline = '';
const isDirty = computed(() => state.editing && sig() !== baseline);


function resetFromStore() {
	state.blocks = savedBlocks.value.map((b) => ({ ...b, cells: { ...b.cells } }));
	state.cellRatio = selected.value ? ratioOf(selected.value) : topology.cellRatio;
	state.cellCount = selected.value?.cellCount ?? null;
	baseline = sig();
}

/**
 * Nạp danh sách bố cục (+ bố cục đang dùng vào store dùng chung). Giữ bố cục đang chọn nếu còn;
 * lần đầu / bố cục đã bị xoá ⇒ chọn bố cục đang dùng.
 */
async function load() {
	state.loading = true;
	try {
		const [res] = await Promise.all([getAPI(VwWallTopologyApi).apiVideowallVwwalltopologyListGet(), topology.load(true), vwCtrl.load()]);
		state.layouts = res.data.result ?? [];
		if (!state.layouts.some((l) => l.id === state.selectedId)) state.selectedId = (state.layouts.find(isInUse) ?? state.layouts[0])?.id ?? '';
		state.editing = false;
		resetFromStore();
	} finally {
		state.loading = false;
	}
}

/** Chọn bố cục khác ⇒ bỏ thay đổi đang sửa (hỏi trước). */
async function onSelectLayout(id: string) {
	if (id === state.selectedId) return;
	if (isDirty.value && !(await confirmDiscard())) return;
	state.selectedId = id;
	state.editing = false;
	resetFromStore();
}

/** Chọn bố cục trong panel ⇒ đóng panel rồi chuyển (hỏi trước nếu còn thay đổi chưa lưu). */
function onPickLayout(id: string) {
	state.pickerOpen = false;
	onSelectLayout(id);
}

onMounted(load);

/** Tab "Bố cục tường" được giữ (keep-alive) để không mất thay đổi đang sửa; quay lại tab mà không sửa dở ⇒ nạp lại. */
let firstActivation = true;
onActivated(() => {
	// Lần kích hoạt đầu đi ngay sau onMounted (đã nạp) ⇒ bỏ qua
	if (firstActivation) {
		firstActivation = false;
		return;
	}
	if (isDirty.value) return;
	if (state.activeTab === 'list' && xGrid.value) handleQuery();
	else load();
});

/** Rời trang khi còn thay đổi chưa lưu ⇒ hỏi trước. */
onBeforeRouteLeave(async () => {
	if (!isDirty.value) return true;
	return confirmDiscard();
});

async function confirmDiscard() {
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
}

function onStartEdit() {
	resetFromStore();
	state.editing = true;
}

async function onCancel() {
	if (isDirty.value && !(await confirmDiscard())) return;
	resetFromStore();
	state.editing = false;
}

function onGridPick(i: number, cols: number, rows: number) {
	state.blocks[i] = { ...state.blocks[i], cols, rows };
}

/** Thêm khối vào bên phải — khối cũ giữ nguyên số thứ tự nên dữ liệu đang có không bị ảnh hưởng. */
function onAddBlock() {
	if (state.blocks.length >= MAX_BLOCKS) return;
	// Có số ô ⇒ khối mới không vượt phần ô còn lại (còn ít hơn khối mặc định thì 1 hàng)
	const left = cellTarget.value != null ? cellTarget.value - cellTotal.value : Infinity;
	if (left < 1) return;
	state.blocks.push(left >= NEW_BLOCK.cols * NEW_BLOCK.rows ? { ...NEW_BLOCK, source: null, cells: {} } : { cols: Math.min(left, MAX_BLOCK_COLS), rows: 1, source: null, cells: {} });
}

/** Xoá 1 khối bất kỳ — khối sau dồn lên; khi lưu BE đánh số lại (khối còn màn / cửa sổ / phân quyền thì bị chặn). */
function onRemoveBlock(i: number) {
	if (state.blocks.length > 1) state.blocks.splice(i, 1);
}

/** Đổi chỗ khối với khối liền trước (-1) / liền sau (+1) ⇒ đổi vị trí trái → phải trên tường. */
function onMoveBlock(i: number, step: -1 | 1) {
	const j = i + step;
	if (j < 0 || j >= state.blocks.length) return;
	[state.blocks[i], state.blocks[j]] = [state.blocks[j], state.blocks[i]];
}

async function onSave() {
	if ((!isDefault.value && !isDirty.value) || cellMismatch.value) return;
	try {
		await ElMessageBox.confirm(
			isDefault.value ? $t('lz.message.videoWall.confirmInitLayout') : $t('lz.message.videoWall.confirmSaveWallTopology'),
			$t('lz.message.base.warning'),
			{ confirmButtonText: $t('lz.button.base.confirm'), cancelButtonText: $t('lz.button.base.cancel'), type: 'warning' }
		);
	} catch {
		return;
	}

	state.saving = true;
	try {
		// sourceBlockNo: số khối đã lưu ⇒ BE đánh số lại dữ liệu khi đổi chỗ / xoá khối
		const blocks = state.blocks.map((b, i) => ({ blockNo: i, name: blockLabel(i), cols: b.cols, rows: b.rows, sourceBlockNo: b.source, controllerConfig: toControllerConfig(b) }));
		const size = { cellRatio: state.cellRatio };
		const s = selected.value;
		if (!s) {
			// Chưa có bố cục nào ⇒ thêm bố cục đầu tiên (BE tự đặt làm bố cục đang dùng)
			// Số ô của bố cục đầu tiên = tổng ô của lưới vừa khởi tạo
			await getAPI(VwWallTopologyApi).apiVideowallVwwalltopologyAddPost({
				id: '',
				code: 'BC01',
				name: $t('lz.label.videoWall.defaultLayoutName', { code: 'BC01' }),
				...size,
				cellCount: state.cellCount ?? cellTotal.value,
				blocks,
			});
		} else {
			// Sửa lưới của bố cục đang chọn — bố cục đang dùng: BE chặn thu nhỏ đè lên màn / cửa sổ / phân quyền.
			// Bố cục cũ chưa có số ô (không nhập) ⇒ lấy tổng ô của lưới.
			await getAPI(VwWallTopologyApi).apiVideowallVwwalltopologyUpdatePost({ ...s, ...size, cellCount: state.cellCount ?? cellTotal.value, blocks });
		}
		ElMessage.success($t('lz.message.base.saveSuccess'));
		await load();
	} finally {
		state.saving = false;
	}
}

/* ── Đổi tab ── */

/** Rời tab Bố cục đang dùng khi còn thay đổi chưa lưu ⇒ hỏi trước. */
const onBeforeTabLeave = async (_name: string | number, oldName: string | number) => {
	if (oldName !== 'layout' || !isDirty.value) return true;
	if (!(await confirmDiscard())) return false;
	resetFromStore();
	state.editing = false;
	return true;
};

/** Đổi tab ⇒ nạp lại tab vừa mở để thấy thay đổi làm ở tab kia. */
const onTabChange = (name: string | number) => {
	if (name === 'list' && xGrid.value) handleQuery();
	if (name === 'layout') load();
};

/* ── Danh sách bố cục ── */
const options = useVxeTable<VwWallTopologyOutput>(
	{
		id: 'videoWallTopology',
		name: 'VideoWallTopology',
		columns: [
			// Cột ngắn cố định độ rộng; chỉ Tên giãn theo màn hình
			{ field: 'code', title: $t('lz.entity.base.code'), width: 160, showOverflow: 'tooltip' },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 240, showOverflow: 'tooltip' },
			{ field: 'blocks', title: $t('lz.label.videoWall.layoutGrid'), width: 240, slots: { default: 'row_blocks' }, align: 'center' },
			// Có số ô ⇒ "tổng ô lưới / số ô"
			{
				field: 'totalScreens',
				title: $t('lz.label.videoWall.cellCount'),
				width: 100,
				align: 'center',
				formatter: ({ row }: any) => (row.cellCount != null ? `${row.totalScreens}/${row.cellCount}` : row.totalScreens),
			},
			{ field: 'cellRatio', title: $t('lz.entity.videoWall.cellRatio'), width: 100, align: 'center', formatter: ({ row }: any) => ratioOf(row) },
			// { field: 'sceneCount', title: $t('lz.label.videoWall.sceneCount'), width: 90, align: 'center' },
			// { field: 'permissionCount', title: $t('lz.label.videoWall.permissionCount'), width: 100, align: 'center' },
			// { field: 'remark', title: $t('lz.entity.base.remark'), minWidth: 160, showOverflow: 'tooltip' },
			// Trạng thái Dùng / Không dùng (switch) và cột Áp dụng (cờ IsApply, đổi bằng nút "Áp dụng") đứng cạnh nhau
			{ field: 'status', title: $t('lz.entity.base.status'), width: 110, align: 'center', slots: { default: 'row_status' } },
			{ field: 'isApply', title: $t('lz.label.videoWall.currentLayout'), width: 110, align: 'center', slots: { default: 'row_apply' } },
			{ field: '', title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 160, showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	{
		// Khung đổi kích thước (thu / mở menu, đổi tab) ⇒ tính lại độ rộng cột, cột Hành động luôn sát mép phải
		autoResize: true,
		// Số bố cục ít ⇒ nạp toàn bộ (API list), không phân trang; bố cục đang dùng đứng đầu
		proxyConfig: { autoLoad: true, ajax: { query: () => getAPI(VwWallTopologyApi).apiVideowallVwwalltopologyListGet(state.queryParams.keyword || undefined) } },
		pagerConfig: { enabled: false },
		rowConfig: { keyField: 'id', height: 72 },
		toolbarConfig: { export: false },
	}
);

/** Bố cục đang áp dụng lên tường (IsApply = 1): không áp dụng lại / không xoá / không tắt được. */
const isInUse = (row: VwWallTopologyOutput) => row.isApply === YesNoEnum.NUMBER_1;

/** Switch Trạng thái: lưu ngay (gửi lại nguyên lưới khối); lỗi thì switch tự về như cũ vì hiển thị theo dữ liệu. */
async function toggleStatus(row: VwWallTopologyOutput, on: boolean) {
	state.toggling = row.id ?? '';
	try {
		await getAPI(VwWallTopologyApi).apiVideowallVwwalltopologyUpdatePost({
			...row,
			status: on ? StatusEnum.NUMBER_1 : StatusEnum.NUMBER_0,
			blocks: row.blocks ?? [],
		});
		ElMessage.success($t('lz.message.base.saveSuccess'));
		await onListChanged();
	} finally {
		state.toggling = '';
	}
}

const blocksText = (row: VwWallTopologyOutput) => (row.blocks ?? []).map((b) => `${blockLabel(b.blockNo)} ${b.cols}×${b.rows}`).join(' · ');

const handleQuery = async () => {
	options.loading = true;
	await xGrid.value?.commitProxy('reload');
	options.loading = false;
};

const resetQuery = async () => {
	state.queryParams.keyword = '';
	await handleQuery();
};

/** Thêm / sửa / xoá / đổi bố cục đang dùng ⇒ nạp lại bảng + bố cục đang dùng (store dùng chung cho các trang Video Wall). */
const onListChanged = async () => {
	await Promise.all([handleQuery(), load()]);
};

/** Mã kế tiếp dạng BC01, BC02… */
const nextCode = () => {
	const rows = (xGrid.value?.getTableData().fullData ?? []) as VwWallTopologyOutput[];
	const max = Math.max(0, ...rows.map((r) => Number(/^BC(\d+)$/i.exec(r.code ?? '')?.[1] ?? 0)));
	return `BC${String(max + 1).padStart(2, '0')}`;
};

const toBlocks = (row: VwWallTopologyOutput) => (row.blocks ?? []).map((b) => ({ cols: b.cols ?? 1, rows: b.rows ?? 1 }));

const handleAdd = () => {
	state.editTitle = $t('lz.label.base.titleAdd', { name: $t('lz.entity.videoWall.wallTopology') });
	state.operateType = 'add';
	editRef.value?.openDialog({ code: nextCode(), name: '', cellRatio: topology.cellRatio, remark: null }, []);
};

const handleCopy = (row: VwWallTopologyOutput) => {
	state.editTitle = $t('lz.label.base.titleCopy', { name: $t('lz.entity.videoWall.wallTopology') });
	state.operateType = 'copy';
	editRef.value?.openDialog(
		{ ...row, id: '', code: nextCode(), name: `${row.name ?? ''} ${$t('lz.label.videoWall.copySuffix')}`.trim(), status: StatusEnum.NUMBER_0, blocks: [] },
		toBlocks(row)
	);
};

const handleEdit = (row: VwWallTopologyOutput) => {
	state.editTitle = $t('lz.label.base.titleEdit', { name: $t('lz.entity.videoWall.wallTopology') });
	state.operateType = 'edit';
	editRef.value?.openDialog({ ...row, blocks: [] }, toBlocks(row));
};

const handleDelete = async (row: VwWallTopologyOutput) => {
	try {
		await ElMessageBox.confirm(
			$t('lz.message.videoWall.confirmDeleteWallTopology', { name: `${row.code} — ${row.name}`, scenes: row.sceneCount ?? 0, permissions: row.permissionCount ?? 0 }),
			$t('lz.message.base.warning'),
			{ confirmButtonText: $t('lz.button.base.delete'), cancelButtonText: $t('lz.button.base.cancel'), type: 'warning' }
		);
	} catch {
		return;
	}
	await getAPI(VwWallTopologyApi).apiVideowallVwwalltopologyDeletePost({ id: row.id! });
	ElMessage.success($t('lz.message.base.deleteSuccess'));
	await onListChanged();
};

/**
 * Đặt làm bố cục đang dùng: BE chuyển kịch bản / lập lịch / liên kết sự kiện của bố cục khác sang Không dùng,
 * xoá kịch bản đang phát, mọi màn về khay ngoài lưới. Xong thì hỏi kích hoạt kịch bản mặc định của bố cục mới (nếu có và đang dùng).
 */
const handleSetCurrent = async (row: VwWallTopologyOutput) => {
	try {
		await ElMessageBox.confirm($t('lz.message.videoWall.confirmSetCurrentLayout', { name: `${row.code} — ${row.name}` }), $t('lz.message.base.warning'), {
			confirmButtonText: $t('lz.button.base.confirm'),
			cancelButtonText: $t('lz.button.base.cancel'),
			type: 'warning',
		});
	} catch {
		return;
	}
	await getAPI(VwWallTopologyApi).apiVideowallVwwalltopologySetcurrentPost({ id: row.id! });
	ElMessage.success($t('lz.message.base.saveSuccess'));
	await onListChanged();
	await offerActivateDefaultScene(row.id!);
};

async function offerActivateDefaultScene(topologyId: string) {
	const res = await getAPI(VwSceneApi).apiVideowallVwscenePagePost({ page: 1, pageSize: 500, wallTopologyId: topologyId });
	const scene = (res.data.result?.records ?? []).find((s) => s.wallTopologyId === topologyId && s.isDefault === DefaultEnum.NUMBER_1);
	if (!scene?.code || scene.status !== StatusEnum.NUMBER_1) return;
	try {
		await ElMessageBox.confirm($t('lz.message.videoWall.confirmActivateDefaultScene', { name: scene.name ?? scene.code }), $t('lz.label.base.info'), {
			confirmButtonText: $t('lz.button.videoWall.apply'),
			cancelButtonText: $t('lz.button.base.cancel'),
			type: 'info',
		});
	} catch {
		return;
	}
	await getAPI(VwSceneApi).apiVideowallVwsceneActivePost({ code: scene.code });
	ElMessage.success($t('lz.message.videoWall.sceneActivating', { name: scene.name ?? scene.code }));
}
</script>

<style scoped lang="scss">
@import '/@/theme/its/videoWallStage.scss';

.videowall-topology-container {
	display: flex;
	flex-direction: column;
}
/* Thanh tab dạng nút như trang Màn hình: tab đang chọn tô nền primary, không gạch chân */
.vw-topology-tabs {
	flex: 1;
	/* Mở qua tab menu hệ thống: ẩn thanh tab nội bộ */
	&.is-single > :deep(.el-tabs__header) {
		display: none;
	}
	min-height: 0;
	display: flex;
	flex-direction: column;
	:deep(.el-tabs__header) {
		margin-bottom: 8px;
	}
	:deep(.el-tabs__content) {
		flex: 1;
		min-height: 0;
	}
	:deep(.el-tab-pane) {
		height: 100%;
	}
	:deep(.el-tabs__nav-wrap::after),
	:deep(.el-tabs__active-bar) {
		display: none;
	}
	:deep(.el-tabs__nav) {
		gap: 6px;
	}
	:deep(.el-tabs__item) {
		height: 34px;
		padding: 0 16px !important;
		border-radius: 6px;
		color: var(--el-text-color-regular);
		font-weight: 500;
		transition: background 0.15s, color 0.15s;
		&:hover:not(.is-active) {
			color: var(--el-color-primary);
			background: var(--el-color-primary-light-9);
		}
		&.is-active {
			color: #fff;
			background: var(--el-color-primary);
			box-shadow: 0 2px 6px rgba(var(--el-color-primary-rgb), 0.35);
		}
	}
}
.vw-tab-label {
	display: inline-flex;
	align-items: center;
	gap: 6px;
	.el-icon {
		font-size: 12px;
	}
}
.wt-list {
	height: 100%;
	display: flex;
	flex-direction: column;
}
.wt-blocks-cell {
	display: flex;
	align-items: center;
	justify-content: center;
	gap: 10px;
}
.wt-blocks-text {
	font-size: 12px;
	color: var(--el-text-color-secondary);
	white-space: nowrap;
	font-variant-numeric: tabular-nums;
}
.wt {
	display: flex;
	flex-direction: column;
	gap: 10px;
	height: 100%;
	min-height: 0;
}
/* ── Thanh trên: cùng kiểu monitorToolbar.vue (khung trắng · dòng tiêu đề · dòng thao tác) ── */
.wt-toolbar {
	display: flex;
	flex-direction: column;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 10px;
	background: var(--el-bg-color);
	box-shadow: 0 1px 4px rgba(15, 23, 42, 0.04);
	flex-shrink: 0;
	:deep(.el-button + .el-button) {
		margin-left: 0;
	}
}
.wt-toolbar-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 16px;
	min-height: 32px;
	padding: 8px 16px;
	border-bottom: 1px solid var(--el-border-color-light);
}
.wt-toolbar-title {
	display: inline-flex;
	align-items: center;
	gap: 8px;
	font-size: 18px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	white-space: nowrap;
}
.wt-editing-icon {
	margin-right: 4px;
	vertical-align: -2px;
}
.wt-toolbar-row {
	display: flex;
	align-items: center;
	gap: 12px;
	padding: 12px 16px;
}
.wt-toolbar-left,
.wt-toolbar-right {
	display: flex;
	align-items: center;
	gap: 8px;
	min-width: 0;
	flex-shrink: 0;
}
/* Thiếu chỗ ⇒ cụm trái co, nút thao tác giữ nguyên */
.wt-toolbar-left {
	flex-shrink: 1;
}
.wt-spacer {
	flex: 1;
}

/* Thẻ chọn bố cục — cùng kiểu components/videoWall/scenePicker.vue */
.wt-picker {
	display: flex;
	align-items: center;
	gap: 10px;
	width: 280px;
	flex-shrink: 0;
	padding: 4px 10px 4px 4px;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 10px;
	background: var(--el-bg-color);
	color: var(--el-text-color-primary);
	text-align: left;
	cursor: pointer;
	transition: border-color 0.15s, box-shadow 0.15s;
	&:hover:not(.is-disabled),
	&.is-open {
		border-color: var(--el-color-primary-light-5);
		box-shadow: 0 0 0 3px var(--el-color-primary-light-9);
	}
	&.is-disabled {
		cursor: not-allowed;
		opacity: 0.6;
	}
}
.wt-picker__icon {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 34px;
	height: 34px;
	border-radius: 9px;
	font-size: 18px;
	flex-shrink: 0;
	color: var(--el-color-primary);
	background: var(--el-color-primary-light-9);
	/* Bố cục đang dùng ⇒ nền xanh như kịch bản đang phát */
	&.is-live {
		color: #fff;
		background: linear-gradient(135deg, var(--el-color-success) 0%, var(--el-color-success-dark-2, #529b2e) 100%);
		box-shadow: 0 2px 8px rgba(var(--el-color-success-rgb), 0.35);
	}
}
.wt-picker__text {
	flex: 1;
	min-width: 0;
	display: flex;
	flex-direction: column;
	gap: 1px;
}
.wt-picker__label {
	font-size: 11px;
	line-height: 14px;
	color: var(--el-text-color-secondary);
}
.wt-picker__name {
	font-size: 14px;
	font-weight: 600;
	line-height: 1.3;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}
.wt-picker__arrow {
	flex-shrink: 0;
	font-size: 14px;
	color: var(--el-text-color-secondary);
	transition: transform 0.2s;
	.is-open & {
		transform: rotate(180deg);
	}
}

/* Panel danh sách bố cục (popover vw-layout-popover: tràn viền, bo góc — xem style không scoped cuối file) */
.wt-pick-panel {
	display: flex;
	flex-direction: column;
	max-height: min(480px, 70vh);
}
.wt-pick-panel__head {
	display: flex;
	flex-direction: column;
	gap: 10px;
	padding: 14px 16px 12px;
	border-bottom: 1px solid var(--el-border-color-lighter);
	background: linear-gradient(180deg, var(--el-fill-color-lighter) 0%, var(--el-bg-color-overlay) 100%);
}
.wt-pick-panel__title-row {
	display: flex;
	align-items: center;
	gap: 8px;
}
.wt-pick-panel__search {
	:deep(.el-input__wrapper) {
		min-height: 36px;
		border-radius: 8px;
	}
}
.wt-pick-panel__empty {
	display: flex;
	flex-direction: column;
	align-items: center;
	gap: 6px;
	padding: 24px 0;
	font-size: 14px;
	color: var(--el-text-color-secondary);
}
.wt-pick-panel__title {
	flex: 1;
	font-size: 15px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}
.wt-pick-panel__summary {
	padding: 3px 10px;
	border-radius: 999px;
	background: var(--el-fill-color-light);
	color: var(--el-text-color-regular);
	font-size: 13px;
	font-weight: 600;
	white-space: nowrap;
}
.wt-pick-panel__body {
	flex: 1;
	min-height: 0;
	overflow-y: auto;
	display: flex;
	flex-direction: column;
	gap: 6px;
	padding: 10px 12px 12px;
}
.wt-pick-item {
	display: flex;
	align-items: center;
	gap: 12px;
	width: 100%;
	min-height: 56px;
	padding: 8px 12px;
	border: 1px solid transparent;
	border-radius: 12px;
	background: transparent;
	color: var(--el-text-color-primary);
	text-align: left;
	cursor: pointer;
	flex-shrink: 0;
	transition: all 0.15s ease;
	&:hover {
		background: var(--el-fill-color-light);
	}
	&.is-active {
		border-color: var(--el-color-primary-light-5);
		background: var(--el-color-primary-light-9);
	}
}
.wt-pick-item__text {
	flex: 1;
	min-width: 0;
	display: flex;
	flex-direction: column;
	gap: 3px;
}
.wt-pick-item__name {
	font-size: 14px;
	font-weight: 600;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}
.wt-pick-item__meta {
	font-size: 12px;
	color: var(--el-text-color-secondary);
}
.wt-pick-item__live {
	display: inline-flex;
	align-items: center;
	gap: 5px;
	padding: 2px 9px;
	border-radius: 999px;
	background: var(--el-color-success);
	color: #fff;
	font-size: 12px;
	font-weight: 600;
	white-space: nowrap;
	flex-shrink: 0;
	i {
		width: 6px;
		height: 6px;
		border-radius: 50%;
		background: #fff;
	}
}
.wt-pick-item__grid {
	display: inline-flex;
	justify-content: flex-end;
	flex-shrink: 0;
	margin-left: auto;
}
.wt-pick-item__check {
	flex-shrink: 0;
	font-size: 20px;
	color: var(--el-color-primary);
}

/* Dải thông tin bố cục — cùng kiểu .monitor-toolbar-meta */
.wt-meta {
	display: flex;
	align-items: center;
	min-width: 0;
	height: 44px;
	margin-left: 4px;
	padding: 0 6px;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 10px;
	background: var(--el-fill-color-lighter);
	overflow: hidden;
}
.wt-meta-item {
	display: inline-flex;
	align-items: center;
	gap: 8px;
	flex-shrink: 0;
	padding: 0 12px;
	& + & {
		border-left: 1px solid var(--el-border-color);
	}
}
.wt-meta-item__icon {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 24px;
	height: 24px;
	border-radius: 7px;
	font-size: 14px;
	flex-shrink: 0;
	color: var(--el-color-primary);
	background: var(--el-color-primary-light-9);
	&.is-success {
		color: var(--el-color-success);
		background: var(--el-color-success-light-9);
	}
	&.is-info {
		color: var(--el-text-color-secondary);
		background: var(--el-fill-color);
	}
}
/* Hình tỉ lệ ô trong icon */
.wt-meta-ratio {
	display: block;
	border: 1.5px solid currentColor;
	border-radius: 2px;
}
.wt-meta-item__label {
	font-size: 12px;
	font-weight: 600;
	color: var(--el-text-color-secondary);
	white-space: nowrap;
}
.wt-meta-item__value {
	font-size: 14px;
	font-weight: 700;
	font-variant-numeric: tabular-nums;
	color: var(--el-text-color-primary);
	white-space: nowrap;
	&.is-mismatch {
		color: var(--el-color-warning);
	}
}
.wt-body {
	display: flex;
	flex: 1;
	min-height: 0;
}
/* Thanh chia dọc giữa sơ đồ lưới và cột cấu hình (thay khoảng cách gap cũ) */
.wt-splitter {
	flex: none;
	width: 24px;
	align-self: stretch;
}
.wt-canvas {
	flex: 1;
	min-width: 0;
	min-height: 420px;
	display: flex;
	flex-direction: column;
	border-radius: 10px;
	overflow: hidden;
	> :first-child {
		flex: 1;
		min-height: 0;
	}
}
.wt-cell {
	display: flex;
	align-items: center;
	justify-content: center;
	width: 100%;
	height: 100%;
	box-sizing: border-box;
	border: 1px solid rgba(120, 160, 220, 0.35);
	border-radius: 4px;
	background: rgba(40, 80, 140, 0.25);
	color: #c9dcf5;
	font-size: 13px;
	font-variant-numeric: tabular-nums;
	white-space: nowrap;
	overflow: hidden;
	user-select: none;
	&.is-new {
		border-style: dashed;
		border-color: var(--el-color-primary-light-3);
		background: rgba(59, 140, 255, 0.28);
		color: #fff;
	}
}
/* Ô gán bộ điều khiển: cùng kiểu ô chọn của Phân quyền vùng (editWallPermission .wp-cell) */
.wt-cell {
	@include vw-stage-tile;
	position: relative;
	flex-direction: column;
	gap: 2px;
	padding: 4px;
	border-top-width: 3px;
	transition: background 0.1s, border-color 0.1s, box-shadow 0.1s;
	/* Chưa gán: tối mờ, lùi ra sau */
	background: rgba(21, 43, 76, 0.32);
	border-left-color: rgba(190, 215, 250, 0.3);
	border-right-color: rgba(190, 215, 250, 0.3);
	border-bottom-color: rgba(190, 215, 250, 0.3);
	box-shadow: none;
	color: rgba(240, 246, 255, 0.85);
	&.is-paint {
		cursor: pointer;
		&:hover {
			border-left-color: rgba(140, 190, 255, 0.55);
			border-right-color: rgba(140, 190, 255, 0.55);
			border-bottom-color: rgba(140, 190, 255, 0.55);
		}
	}
	/* Đã gán bộ: nền đặc màu bộ (style inline), chữ trắng */
	&.is-owned {
		border-width: 1px;
		border-style: solid;
		box-shadow: 0 2px 6px rgba(3, 12, 30, 0.35);
		color: #fff;
		text-shadow: 0 1px 2px rgba(0, 0, 0, 0.35);
		.wt-cell-name,
		.wt-cell-pos {
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
	.wt-cell-name {
		max-width: 100%;
		font-size: clamp(11px, calc(var(--cell-w, 96px) * 0.11), 14px);
		font-weight: 600;
		text-align: center;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}
	.wt-cell-pos {
		font-size: 10px;
		color: var(--wg-muted);
		white-space: nowrap;
		font-variant-numeric: tabular-nums;
	}
	.wt-cell-check {
		position: absolute;
		right: 3px;
		bottom: 3px;
		color: #fff;
	}
}
.wt-ctrl-list {
	display: flex;
	flex-direction: column;
	gap: 6px;
}
/* Mỗi bộ: icon màu bộ · mã + tên (2 dòng) · số ô. Đang sửa: thẻ chọn cọ, thẻ đang chọn có vạch + nền nhạt màu bộ */
.wt-ctrl {
	--ctrl-color: var(--el-color-primary);
	position: relative;
	display: flex;
	align-items: center;
	gap: 10px;
	width: 100%;
	padding: 8px 10px;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 8px;
	background: var(--el-bg-color);
	color: var(--el-text-color-regular);
	text-align: left;
	cursor: pointer;
	overflow: hidden;
	transition: border-color 0.15s, background-color 0.15s, box-shadow 0.15s;
	&:not(:disabled):hover {
		border-color: var(--ctrl-color);
		background: color-mix(in srgb, var(--ctrl-color) 6%, var(--el-bg-color));
	}
	&:disabled {
		cursor: default;
	}
	&.is-active {
		border-color: var(--ctrl-color);
		background: color-mix(in srgb, var(--ctrl-color) 10%, var(--el-bg-color));
		box-shadow: 0 0 0 1px var(--ctrl-color);
		&::before {
			content: '';
			position: absolute;
			inset: 0 auto 0 0;
			width: 4px;
			background: var(--ctrl-color);
		}
	}
}
.wt-ctrl-icon {
	flex: none;
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 32px;
	height: 32px;
	border-radius: 8px;
	background: var(--ctrl-color);
	color: #fff;
	font-size: 18px;
}
.wt-ctrl-text {
	flex: 1;
	min-width: 0;
	display: flex;
	flex-direction: column;
	line-height: 1.3;
	b {
		font-size: 13px;
		color: var(--el-text-color-primary);
	}
}
.wt-ctrl-name {
	overflow: hidden;
	white-space: nowrap;
	text-overflow: ellipsis;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}
.wt-ctrl-count {
	flex: none;
	padding: 1px 8px;
	border-radius: 999px;
	background: color-mix(in srgb, var(--ctrl-color) 12%, transparent);
	color: var(--ctrl-color);
	font-size: 11px;
	font-weight: 600;
}
/* Cọ "Xoá gán": viền nét đứt, icon đỏ nhạt */
.wt-ctrl--eraser {
	--ctrl-color: var(--el-color-danger);
	border-style: dashed;
	.wt-ctrl-icon {
		background: var(--el-color-danger-light-9);
		color: var(--el-color-danger);
	}
	.wt-ctrl-name {
		color: var(--el-color-danger);
	}
}
.wt-side {
	display: flex;
	flex-direction: column;
	/* Chiều rộng theo thanh chia (useSidePanel) */
	flex-shrink: 0;
	min-height: 0;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 10px;
	background: var(--el-bg-color);
	overflow: hidden;
}
.wt-side-head {
	flex-shrink: 0;
	padding: 10px 12px;
	border-bottom: 1px solid var(--el-border-color-lighter);
	font-size: 13px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}
.wt-side-body {
	flex: 1;
	min-height: 0;
	overflow-y: auto;
	padding: 12px;
	display: flex;
	flex-direction: column;
	gap: 8px;
	.el-divider {
		margin: 8px 0;
	}
}
.wt-block {
	display: flex;
	flex-direction: column;
	gap: 8px;
	padding: 10px;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 8px;
	background: var(--el-fill-color-lighter);
}
.wt-block-head {
	display: flex;
	align-items: center;
	gap: 8px;
	font-size: 13px;
	.wt-badge {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		width: 22px;
		height: 22px;
		border-radius: 6px;
		font-size: 12px;
		font-weight: 700;
		color: #fff;
		background: var(--el-color-primary);
	}
	.wt-block-count {
		margin-left: auto;
		font-size: 12px;
		color: var(--el-text-color-secondary);
	}
	/* Nút lên / xuống / xoá khối: sát nhau, không đẩy dòng tiêu đề */
	.wt-block-actions {
		display: inline-flex;
		.el-button + .el-button {
			margin-left: 0;
		}
	}
}
.wt-section-title {
	font-size: 13px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}
/* Chọn tỉ lệ ô: mỗi lựa chọn có hình minh hoạ đúng tỉ lệ */
.wt-ratio {
	display: grid;
	grid-template-columns: 1fr 1fr;
	gap: 8px;
}
.wt-ratio-item {
	display: flex;
	flex-direction: column;
	align-items: center;
	gap: 4px;
	padding: 10px 6px 8px;
	border: 1px solid var(--el-border-color);
	border-radius: 8px;
	background: var(--el-bg-color);
	color: var(--el-text-color-regular);
	font-size: 13px;
	cursor: pointer;
	transition: border-color 0.15s, background 0.15s, color 0.15s;
	small {
		font-size: 11px;
		color: var(--el-text-color-secondary);
	}
	&:hover:not(:disabled):not(.is-active) {
		border-color: var(--el-color-primary-light-5);
	}
	&.is-active {
		border-color: var(--el-color-primary);
		background: var(--el-color-primary-light-9);
		color: var(--el-color-primary);
		.wt-ratio-shape {
			border-color: var(--el-color-primary);
			background: var(--el-color-primary-light-7);
		}
	}
	&:disabled {
		cursor: not-allowed;
		opacity: 0.6;
	}
}
.wt-ratio-shape {
	display: block;
	margin-bottom: 2px;
	border: 1.5px solid var(--el-text-color-secondary);
	border-radius: 2px;
	background: var(--el-fill-color);
}
/* Thẻ Số ô: màu theo trạng thái — khớp (xanh) · lệch (cam) · chưa giới hạn (xám) */
.wt-cell-count {
	--cc-color: var(--el-color-primary);
	display: flex;
	flex-direction: column;
	gap: 10px;
	margin-bottom: 4px;
	padding: 12px;
	border: 1px solid color-mix(in srgb, var(--cc-color) 30%, transparent);
	border-radius: 10px;
	background: color-mix(in srgb, var(--cc-color) 6%, var(--el-bg-color));
	&.is-ok {
		--cc-color: var(--el-color-success);
	}
	&.is-mismatch {
		--cc-color: var(--el-color-warning);
	}
	&.is-unset {
		--cc-color: var(--el-color-info);
	}
}
.wt-cell-count__head {
	display: flex;
	align-items: center;
	gap: 10px;
}
.wt-cell-count__icon {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	flex-shrink: 0;
	width: 32px;
	height: 32px;
	border-radius: 8px;
	font-size: 16px;
	color: var(--cc-color);
	background: color-mix(in srgb, var(--cc-color) 14%, transparent);
}
.wt-cell-count__text {
	display: flex;
	flex-direction: column;
	flex: 1;
	min-width: 0;
	b {
		font-size: 13px;
		color: var(--el-text-color-primary);
	}
	small {
		font-size: 11px;
		line-height: 1.35;
		color: var(--el-text-color-secondary);
	}
}
.wt-cell-count__value {
	flex-shrink: 0;
	padding: 3px 10px;
	border-radius: 999px;
	font-size: 13px;
	font-weight: 700;
	font-variant-numeric: tabular-nums;
	color: var(--cc-color);
	background: color-mix(in srgb, var(--cc-color) 12%, transparent);
}
/* Thanh tỉ lệ ô đã dùng / số ô */
.wt-cell-count__bar {
	height: 4px;
	border-radius: 999px;
	background: color-mix(in srgb, var(--cc-color) 15%, transparent);
	overflow: hidden;
	i {
		display: block;
		height: 100%;
		border-radius: inherit;
		background: var(--cc-color);
		transition: width 0.2s;
	}
}
.wt-hint {
	margin: 0;
	font-size: 12px;
	color: var(--el-text-color-secondary);
	&.is-warning {
		margin-top: 8px;
		color: var(--el-color-warning);
	}
}
.wt-rules {
	flex-shrink: 0;
	display: flex;
	flex-direction: column;
	gap: 4px;
	padding: 10px 12px;
	border-top: 1px solid var(--el-border-color-lighter);
	background: var(--el-fill-color-blank);
}
.wt-rule {
	display: flex;
	align-items: flex-start;
	gap: 6px;
	font-size: 12px;
	line-height: 1.45;
	color: var(--el-text-color-secondary);
	.el-icon {
		margin-top: 2px;
		flex-shrink: 0;
	}
}
</style>

<style lang="scss">
/* Popover chọn bố cục: bỏ padding mặc định để panel tràn viền (cùng kiểu popover chọn kịch bản) */
.el-popover.el-popper.vw-layout-popover {
	padding: 0;
	border-radius: 14px;
	border: 1px solid var(--el-border-color-lighter);
	box-shadow: 0 12px 32px rgba(15, 23, 42, 0.16);
	overflow: hidden;
}
</style>
