<template>
	<el-card class="box-card" shadow="hover" body-style="height:100%;overflow:auto;padding:5px;width:100%;">
		<template #header>
			<div class="card-header">
				<div class="tree-h-flex">
					<div class="tree-h-left">
						<el-input :prefix-icon="Search" v-model.lazy="filterText" clearable :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.name')})" />
					</div>
					<div class="tree-h-right">
						<el-dropdown @command="handleCommand">
							<el-button style="margin-left: 8px; width: 34px">
								<el-icon class="el-icon--center">
									<more-filled />
								</el-icon>
							</el-button>
							<template #dropdown>
								<el-dropdown-menu>
									<el-dropdown-item command="expandAll">{{ $t('lz.button.base.expandAll') }}</el-dropdown-item>
									<el-dropdown-item command="collapseAll">{{ $t('lz.button.base.collapseAll') }}</el-dropdown-item>
									<el-dropdown-item command="rootNode">{{ $t('lz.button.base.rootNode') }}</el-dropdown-item>
									<el-dropdown-item command="refresh">{{ $t('lz.button.base.refresh') }}</el-dropdown-item>
								</el-dropdown-menu>
							</template>
						</el-dropdown>
					</div>
					<el-checkbox v-if="!props.checkStrictly && state.isShowCheckbox" v-model="state.strictly" :label="$t('lz.entity.base.linkage')" style="margin-left: 8px" border />
				</div>
			</div>
		</template>
		<div style="margin-bottom: 45px" v-loading="state.loading">
			<el-scrollbar>
				<el-tree
					ref="treeRef"
					class="filter-tree"
					:data="state.orgData"
					node-key="id"
					:props="{ children: 'children', label: 'name' }"
					:filter-node-method="filterNode"
					@node-click="nodeClick"
					:show-checkbox="state.isShowCheckbox"
					:default-expanded-keys="state.orgData[0] && [state.orgData[0].id]"
					:default-checked-keys="state.ownOrgData"
					highlight-current
					:check-strictly="!state.strictly"
				>
					<template #default="{ node }">
						<el-icon v-if="node.level == 1" size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-School /></el-icon>
						<el-icon v-else-if="node.level == 2" size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-PriceTag /></el-icon>
						<el-icon v-else size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-CollectionTag /></el-icon>
						{{ node.label }}
					</template>
				</el-tree>
			</el-scrollbar>
		</div>
	</el-card>
</template>

<script lang="ts" setup>
import { onMounted, reactive, ref, watch } from 'vue';
import type { ElTree } from 'element-plus';
import { Search, MoreFilled } from '@element-plus/icons-vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysOrgApi } from '/@/api-services/system';
import { SysOrg } from '/@/api-services/system';
import { TreeKey } from 'element-plus/es/components/tree/src/tree.type';

const props = defineProps({
	checkStrictly: { type: Boolean, defaul: true },
});

const filterText = ref('');
const treeRef = ref<InstanceType<typeof ElTree>>();
const state = reactive({
	loading: false,
	orgData: [] as Array<SysOrg>,
	isShowCheckbox: false,
	ownOrgData: [],
	strictly: false,
});

// Page initialization
onMounted(async () => {
	await fetchTreeData();
});

// Query listener
watch(filterText, (val) => {
	treeRef.value!.filter(val);
});

// Get tree data
const fetchTreeData = async (showLoading: boolean = true) => {
	if (showLoading) state.loading = true;
	var res = await getAPI(SysOrgApi).apiSystemSysorgListGet("0");
	state.orgData = res.data.result ?? [];
	if (showLoading) state.loading = false;
	return res.data.result ?? [];
};

// Set default selection
const setCheckedKeys = (orgData: any) => {
	state.isShowCheckbox = true;
	treeRef.value!.setCheckedKeys([]);
	state.ownOrgData = orgData;
};

// Get selected keys
const getCheckedKeys = () => {
	return treeRef.value!.getCheckedKeys();
};

// Node filtering
const filterNode = (value: string, data: any) => {
	if (!value) return true;
	return data.name.toLowerCase().includes(value.toLowerCase());
};

// Node operations
const handleCommand = async (command: string | number | object) => {
	if ('expandAll' == command) {
		for (let i = 0; i < treeRef.value!.store._getAllNodes().length; i++) {
			treeRef.value!.store._getAllNodes()[i].expanded = true;
		}
	} else if ('collapseAll' == command) {
		for (let i = 0; i < treeRef.value!.store._getAllNodes().length; i++) {
			treeRef.value!.store._getAllNodes()[i].expanded = false;
		}
	} else if ('refresh' == command) {
		fetchTreeData();
	} else if ('rootNode' == command) {
		treeRef.value?.setCurrentKey();
		emits('node-click', { id: 0, name: '' });
	}
};

// Interaction logic with parent component
const emits = defineEmits(['node-click']);
const nodeClick = (node: any) => {
	emits('node-click', { id: node.id, name: node.name });
};

// Set current selected node
const setCurrentKey = (key?: TreeKey | undefined, shouldAutoExpandParent?: boolean | undefined) => {
	treeRef.value?.setCurrentKey(key, shouldAutoExpandParent);
};

// Export object
defineExpose({ fetchTreeData, setCheckedKeys, getCheckedKeys, setCurrentKey });
</script>

<style lang="scss" scoped>
.box-card {
	flex: 1;
	> :deep(.el-card__header) {
		padding: 5px;
	}
}
.tree-h-flex {
	display: flex;
}

.tree-h-left {
	flex: 1;
	width: 100%;
}

.tree-h-right {
	width: 42px;
	min-width: 42px;
}
</style>
