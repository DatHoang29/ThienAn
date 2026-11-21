<template>
	<div class="table-container">
		<div v-if="!hideTool" class="table-header mb15">
			<div>
				<slot name="command"></slot>
			</div>
			<div v-loading="state.exportLoading" class="table-footer-tool">
				<SvgIcon v-if="!config.hideRefresh" name="iconfont icon-refresh" :size="22" title="Refresh" @click="onRefreshTable" />
				<el-tooltip effect="light" :content="state.switchFixedContent" placement="bottom-start" :show-after="200" v-if="state.haveFixed">
					<el-icon :style="{ color: state.fixedIconColor }" @click="switchFixed"><ele-Switch /></el-icon>
				</el-tooltip>
				<el-dropdown v-if="!config.hideExport" trigger="click">
					<SvgIcon name="iconfont icon-download" :size="22" title="Export" />
					<template #dropdown>
						<el-dropdown-menu>
							<el-dropdown-item @click="onExportTable">Export current page data</el-dropdown-item>
							<el-dropdown-item @click="onExportTableAll">Export all data</el-dropdown-item>
						</el-dropdown-menu>
					</template>
				</el-dropdown>
				<SvgIcon v-if="!config.hidePrint" name="iconfont icon-print" :size="19" title="Print" @click="onPrintTable" />
				<el-popover v-if="!config.hideSet" placement="bottom-end" trigger="click" transition="el-zoom-in-top" popper-class="table-tool-popper" :width="300" :persistent="false" @show="onSetTable">
					<template #reference>
						<SvgIcon name="iconfont icon-settings" :size="22" title="Settings" />
					</template>
					<template #default>
						<div class="tool-box">
							<el-tooltip content="Drag to sort" placement="top-start">
								<SvgIcon name="fa fa-question-circle-o" :size="17" class="ml11" color="#909399" />
							</el-tooltip>
							<el-checkbox v-model="state.checkListAll" :indeterminate="state.checkListIndeterminate" class="ml10 mr1" label="Column display" @change="onCheckAllChange" />
							<el-checkbox v-model="getConfig.isSerialNo" class="ml12 mr1" label="Serial number" />
							<el-checkbox v-if="getConfig.showSelection" v-model="getConfig.isSelection" class="ml12 mr1" label="Multiple selection" />
						</div>
						<el-scrollbar>
							<div ref="toolSetRef" class="tool-sortable">
								<div class="tool-sortable-item" v-for="v in columns" :key="v.prop" v-show="!v.hideCheck" :data-key="v.prop">
									<i class="fa fa-arrows-alt handle cursor-pointer"></i>
									<el-checkbox v-model="v.isCheck" size="default" class="ml12 mr8" :label="v.label" @change="onCheckChange" />
								</div>
							</div>
						</el-scrollbar>
					</template>
				</el-popover>
			</div>
		</div>
		<el-table
			ref="tableRef"
			:data="state.data"
			:border="setBorder"
			:stripe="setStripe"
			v-bind="$attrs"
			row-key="id"
			default-expand-all
			style="width: 100%"
			v-loading="state.loading"
			:default-sort="defaultSort"
			@selection-change="onSelectionChange"
			@sort-change="sortChange"
		>
			<el-table-column type="selection" :reserve-selection="true" :width="30" v-if="config.isSelection && config.showSelection" />
			<el-table-column type="index" :fixed="state.currentFixed && state.serialNoFixed" label="Serial number" align="middle" :width="60" v-if="config.isSerialNo" />
			<el-table-column v-for="(item, index) in setHeader" :key="index" v-bind="item">
				<template #header v-if="!item.children && $slots[item.prop]">
					<slot :name="`${item.prop}header`" />
				</template>
				<!-- Custom column slot, slot name is the prop of the columns attribute -->
				<template #default="scope" v-if="!item.children && $slots[item.prop]">
					<formatter v-if="item.formatter" :fn="item.formatter(scope.row, scope.column, scope.cellValue, scope.index)"> </formatter>
					<slot v-else :name="item.prop" v-bind="scope"></slot>
				</template>
				<template v-else-if="!item.children" v-slot="scope">
					<formatter v-if="item.formatter" :fn="item.formatter(scope.row, scope.column, scope.cellValue, scope.index)"> </formatter>
					<template v-else-if="item.type === 'image'">
						<el-image
							:style="{ width: `${item.width}px`, height: `${item.height}px` }"
							:src="scope.row[item.prop]"
							:zoom-rate="1.2"
							:preview-src-list="[scope.row[item.prop]]"
							preview-teleported
							fit="cover"
						/>
					</template>
					<template v-else>
						{{ getProperty(scope.row, item.prop) }}
					</template>
				</template>
				<el-table-column v-for="(childrenItem, childrenIndex) in item.children" :key="childrenIndex" v-bind="childrenItem">
					<!-- Custom column slot, slot name is the prop of the columns attribute -->
					<template #default="scope" v-if="$slots[childrenItem.prop]">
						<formatter v-if="childrenItem.formatter" :fn="childrenItem.formatter(scope.row, scope.column, scope.cellValue, scope.index)"> </formatter>
						<slot v-else :name="childrenItem.prop" v-bind="scope"></slot>
					</template>
					<template v-else v-slot="scope">
						<formatter v-if="childrenItem.formatter" :fn="childrenItem.formatter(scope.row, scope.column, scope.cellValue, scope.index)"> </formatter>
						<template v-else-if="childrenItem.type === 'image'">
							<el-image
								:style="{ width: `${childrenItem.width}px`, height: `${childrenItem.height}px` }"
								:src="scope.row[childrenItem.prop]"
								:zoom-rate="1.2"
								:preview-src-list="[scope.row[childrenItem.prop]]"
								preview-teleported
								fit="cover"
							/>
						</template>
						<template v-else>
							{{ getProperty(scope.row, childrenItem.prop) }}
						</template>
					</template>
				</el-table-column>
			</el-table-column>
			<template #empty>
				<el-empty description="No data available" />
			</template>
		</el-table>
		<div v-if="!config.hidePagination && state.showPagination" class="table-footer mt15">
			<el-pagination
				v-model:current-page="state.page.page"
				v-model:page-size="state.page.pageSize"
				small
				:pager-count="5"
				:page-sizes="config.pageSizes"
				:total="state.total"
				layout="total, sizes, prev, pager, next, jumper"
				background
				@size-change="onHandleSizeChange"
				@current-change="onHandleCurrentChange"
			>
			</el-pagination>
		</div>
	</div>
</template>
