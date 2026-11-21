<template>
	<div class="zone-tree-panel">
		<div class="panel-head">
			<el-input v-model="keyword" size="small" clearable prefix-icon="ele-Search" :placeholder="$t('lz.label.tmsZone.treeFilter')" />
			<el-tooltip :content="expanded ? $t('lz.button.base.collapseAll') : $t('lz.button.base.expandAll')" placement="top">
				<el-button size="small" :icon="expanded ? 'ele-Fold' : 'ele-Expand'" @click="toggleExpand" />
			</el-tooltip>
		</div>
		<div class="panel-body">
			<el-tree
				ref="treeRef"
				:data="props.data"
				node-key="id"
				:props="{ label: 'name', children: 'children' }"
				:show-checkbox="props.checkable"
				:default-expand-all="true"
				:expand-on-click-node="false"
				:highlight-current="true"
				:filter-node-method="filterNode"
				:empty-text="props.emptyText || $t('lz.label.base.noData')"
				@node-click="(data: any) => emits('node-click', data)"
				@check="onCheck"
			>
				<template #default="{ data }">
					<div class="tree-node" :class="{ 'is-dimmed': props.dimmedIds?.has(data.id) }">
						<span class="node-name" :title="data.name">{{ data.name }}</span>
						<span v-if="data.kmText" class="node-km">{{ data.kmText }}</span>
						<span class="node-badge"><slot name="badge" :data="data" /></span>
					</div>
				</template>
			</el-tree>
		</div>
	</div>
</template>

<script lang="ts" setup name="tmsZoneTreePanel">
// Author: KienLT
// Description: Cây zone dùng chung cho tab "Gán thiết bị" và "Tốc độ min/max" (bố cục cây trái – nội dung phải).
// Created date: 08/10/2026
//
// Phân cấp theo đúng Pid của zone. Mỗi tab tự gắn huy hiệu qua slot `badge`. Chế độ checkable: tick zone cha =
// tick mọi zone con (el-tree không check-strictly) — dùng làm "chọn nhanh theo zone cha" ở tab tốc độ.
import { nextTick, ref, watch } from 'vue';
import type { ZoneTreeNode } from '../utils/zoneTreeUtils';

interface Props {
	data: ZoneTreeNode[];
	checkable?: boolean;
	/** Khoá đang tick (hai chiều với v-model:checkedKeys). */
	checkedKeys?: string[];
	/** Node đang chọn (tô sáng). */
	currentKey?: string | null;
	/** Node hiển thị mờ (vd: zone cha thuần chỉ để chọn nhanh). */
	dimmedIds?: Set<string>;
	/** Trống ⇒ dùng lz.label.base.noData. */
	emptyText?: string;
}
const props = withDefaults(defineProps<Props>(), { checkable: false, checkedKeys: () => [], currentKey: null, emptyText: '' });

interface Emits {
	(e: 'node-click', node: ZoneTreeNode): void;
	(e: 'update:checkedKeys', keys: string[]): void;
}
const emits = defineEmits<Emits>();

const treeRef = ref<any>();
const keyword = ref('');
const expanded = ref(true);

const filterNode = (value: string, data: ZoneTreeNode) => {
	if (!value) return true;
	return `${data.name ?? ''} ${data.code ?? ''}`.toLowerCase().includes(value.toLowerCase());
};
watch(keyword, value => treeRef.value?.filter(value.trim()));

const toggleExpand = () => {
	expanded.value = !expanded.value;
	const nodes = treeRef.value?.store?.nodesMap ?? {};
	Object.values(nodes).forEach((node: any) => { node.expanded = expanded.value; });
};

const onCheck = () => emits('update:checkedKeys', treeRef.value?.getCheckedKeys(false) ?? []);

// Đồng bộ tick từ bên ngoài (bảng, áp hàng loạt) vào cây; dữ liệu cây đổi thì áp lại.
const syncChecked = async () => {
	if (!props.checkable) return;
	await nextTick();
	treeRef.value?.setCheckedKeys(props.checkedKeys ?? [], false);
};
watch(() => props.checkedKeys, syncChecked);
watch(() => props.data, async () => {
	await syncChecked();
	if (keyword.value) treeRef.value?.filter(keyword.value.trim());
	if (props.currentKey) treeRef.value?.setCurrentKey(props.currentKey);
});
watch(() => props.currentKey, async key => {
	await nextTick();
	treeRef.value?.setCurrentKey(key ?? null);
});
</script>

<style scoped lang="scss">
.zone-tree-panel {
	display: flex;
	flex-direction: column;
	height: 100%;
	min-height: 0;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 6px;
	background: var(--el-bg-color);
}

.panel-head {
	display: flex;
	gap: 6px;
	padding: 8px;
	border-bottom: 1px solid var(--el-border-color-lighter);
}

.panel-body {
	flex: 1;
	min-height: 0;
	overflow: auto;
	padding: 4px 2px;
}

.tree-node {
	display: flex;
	align-items: center;
	gap: 6px;
	flex: 1;
	min-width: 0;
	padding-right: 6px;
	font-size: 13px;

	&.is-dimmed .node-name {
		color: var(--el-text-color-secondary);
	}

	.node-name {
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.node-km {
		flex-shrink: 0;
		font-size: 11px;
		color: var(--el-text-color-secondary);
	}

	.node-badge {
		margin-left: auto;
		flex-shrink: 0;
		display: inline-flex;
		align-items: center;
		gap: 4px;
	}
}
</style>
