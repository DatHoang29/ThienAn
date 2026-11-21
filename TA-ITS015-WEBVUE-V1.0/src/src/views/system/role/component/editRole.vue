<template>
	<div class="sys-role-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="700px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span>{{ props.title }}</span>
				</div>
			</template>
			<el-form :model="state.editForm" ref="editFormRef" label-width="auto" label-position="left">
				<el-row :gutter="10">
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item
							:label="$t('lz.entity.base.name')"
							prop="name"
							label-position="left"
							:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.name') }), trigger: 'blur' }]"
						>
							<el-input v-model="state.editForm.name" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.name') })" clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item
							:label="$t('lz.entity.base.code')"
							prop="code"
							label-position="left"
							:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.code') }), trigger: 'blur' }]"
						>
							<el-input
								v-model="state.editForm.code"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.code') })"
								clearable
								:disabled="state.editForm.code == 'sys_admin' && state.editForm.id != undefined"
							/>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.system.app')" prop="appDataList" label-position="left">
							<el-select v-model="state.editForm.appId" value-key="id" clearable :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.system.app') })" class="w100" filterable>
								<el-option v-for="item in state.appData" :key="item.id" :label="item.name" :value="item.id" :disabled="item.status !== (StatusTypeEnum.Enable as number)" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.orderNo')" label-position="left">
							<el-input-number v-model="state.editForm.orderNo" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.orderNo') })" class="w100" />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.status')" label-position="left">
							<el-radio-group v-model="state.editForm.status" v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.value">
								<el-radio :value="parseInt(item.code)" class="mr5">{{ item.name }}</el-radio>
							</el-radio-group>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.base.remark')" label-position="left">
							<el-input v-model="state.editForm.remark" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.remark') })" clearable type="textarea" />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.sysRole.defaultMenu')" label-position="left">
							<el-cascader
								:options="state.defaultMenuData"
								:props="{ checkStrictly: true, emitPath: false, value: 'id', label: 'title' }"
								:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.sysRole.defaultMenu') })"
								clearable
								class="w100"
								v-model="state.editForm.defaultMenu"
							>
								<template #default="{ node, data }">
									<span>{{ data.title }}</span>
									<span v-if="!node.isLeaf"> ({{ data.children.length }}) </span>
								</template>
							</el-cascader>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.sysRole.menuPermission')" v-loading="state.loading" label-position="left">
							<el-input v-model="filterText" style="width: 240px" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.sysRole.menuPermission') })" />
							<el-tree
								ref="treeRef"
								:data="state.menuData"
								node-key="id"
								show-checkbox
								:props="{ children: 'children', label: 'title', class: treeNodeClass, disabled: 'disabled' }"
								icon="ele-Menu"
								highlight-current
								default-expand-all
								:filter-node-method="filterNode"
								@check="handleCheck"
								style="height: 500px; overflow-y: auto; width: 100%"
							/>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
			<template #footer>
				<span class="dialog-footer">
					<el-button @click="cancel">{{ $t('lz.button.base.cancel') }}</el-button>
					<el-button type="primary" :disabled="state.isProcessing" @click="submit">{{ $t('lz.label.base.confirm') }}</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="sysEditRole">
import { nextTick, reactive, ref, watch, computed } from 'vue';
import type { ElTree } from 'element-plus';

import { getAPI } from '/@/utils/axios-utils';
import { AppOutput, SysAppApi, SysMenuApi, SysRoleApi } from '/@/api-services/system';
import { SysMenu, UpdateRoleInput } from '/@/api-services/system';
import { OperateType } from '/@/types/constants';
import { ElMessage } from 'element-plus';

import { BaseConfigTypeEnum, StatusTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import { excludeTabMenus } from '/@/utils/menuTabs';
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;

import { useI18n } from 'vue-i18n';
const { t: $t } = useI18n();

interface Props {
	/** the type of operation */
	operateType: OperateType;
	title: string;
}
const filterText = ref('');
const props = defineProps<Props>();
const emits = defineEmits(['handleQuery']);
const editFormRef = ref();
const treeRef = ref<InstanceType<typeof ElTree>>();
const state = reactive({
	loading: false,

	isShowDialog: false,
	isProcessing: false,
	editForm: {} as UpdateRoleInput,
	menuData: [] as Array<SysMenu>, // Menu data
	defaultMenuData: [] as Array<SysMenu>, // Menu data
	appData: [] as Array<AppOutput>, // App data
});

// Danh mục chỉ tải khi mở dialog để không gọi API ngầm khi đổi tab.
const fetchCatalogs = async () => {
	state.loading = true;
	try {
		const [menus, apps] = await Promise.all([getAPI(SysMenuApi).apiSystemSysmenuListGet(), getAPI(SysAppApi).apiSystemSysappListGet()]);
		state.menuData = menus.data.result ?? [];
		state.defaultMenuData = excludeTabMenus(state.menuData);
		state.appData = apps.data.result ?? [];
	} finally {
		state.loading = false;
	}
};

const menu = computed(() => {
	// add disabled property to menu data if status is != 1
	const addDisabledProperty = (menuList: Array<SysMenu>) => {
		menuList.forEach((menu: any) => {
			menu.disabled = menu.status != 1;
			if (menu.children && menu.children.length > 0) {
				addDisabledProperty(menu.children);
			}
		});
	};
	return addDisabledProperty(state.menuData);
});

watch(filterText, (val) => {
	treeRef.value!.filter(val);
});
const filterNode = (value: string, data: SysMenu) => {
	if (!value) return true;
	return data!.title?.toLowerCase().includes(value?.toLocaleLowerCase());
};
const handleCheck = (checkedKeys: Array<string>, checkedNodes: Array<SysMenu>) => {
	// const list =  treeRef.value?.getCheckedKeys() as Array<string>;
	// state.defaultMenuData = state.menuData.filter(item => list.includes(item.id as string) && item.type != "3");
};

// Open dialog
const openDialog = async (row: any) => {
	editFormRef.value?.resetFields();
	treeRef.value?.setCheckedKeys([]); // Clear selected values
	state.editForm = JSON.parse(JSON.stringify(row));
	filterText.value = '';
	const [, ownMenus] = await Promise.all([fetchCatalogs(), row.id != undefined ? getAPI(SysRoleApi).apiSystemSysroleOwnmenulistGet(row.id) : Promise.resolve(undefined)]);
	state.isShowDialog = true;
	await nextTick();
	treeRef.value?.setCheckedKeys(ownMenus?.data.result ?? []);
};

// Close dialog
const closeDialog = () => {
	emits('handleQuery');
	state.isShowDialog = false;
};

// Cancel
const cancel = () => {
	if (state.isProcessing) return;
	state.isShowDialog = false;
};

// Submit
const submit = () => {
	if (state.isProcessing) return;
	editFormRef.value.validate(async (valid: boolean) => {
		if (!valid || state.isProcessing) return;
		state.isProcessing = true;
		try {
			state.editForm.menuIdList = treeRef.value?.getCheckedKeys() as Array<string>; //.concat(treeRef.value?.getHalfCheckedKeys());
			//Edit
			if (props.operateType == 'edit') {
				await getAPI(SysRoleApi).apiSystemSysroleUpdatePost(state.editForm);
			}
			//Copy or Add
			else if (props.operateType == 'copy' || props.operateType == 'add') {
				state.editForm.id = '';
				await getAPI(SysRoleApi).apiSystemSysroleAddPost(state.editForm);
			}
			ElMessage.success($t('lz.message.base.saveSuccess'));
			closeDialog();
		} finally {
			state.isProcessing = false;
		}
	});
};

// Leaf node inline display style
const treeNodeClass = (node: SysMenu) => {
	let addClass = true; // Add inline display style for leaf nodes
	for (var key in node.children) {
		// If there are child nodes that are not leaf nodes, do not add the style
		if (node.children[key].children?.length ?? 0 > 0) {
			addClass = false;
			break;
		}
	}
	return addClass ? 'penultimate-node' : '';
};

// Hàm thêm thuộc tính disabled vào menu data nếu status != 1
const disabledMenus = computed(() => {
	// add disabled property to menu data if status is != 1
	const addDisabledProperty = (menuList: Array<SysMenu>) => {
		menuList.forEach((menu: any) => {
			menu.disabled = menu.status != 1;
			if (menu.children && menu.children.length > 0) {
				addDisabledProperty(menu.children);
			}
		});
	};
	return addDisabledProperty(state.menuData);
});

// Export object
defineExpose({ openDialog });
</script>

<style lang="scss" scoped>
.menu-data-tree {
	width: 100%;
	border: 1px solid var(--el-border-color);
	border-radius: var(--el-input-border-radius, var(--el-border-radius-base));
	padding: 5px;
}

:deep(.penultimate-node) {
	.el-tree-node__children {
		padding-left: 40px;
		white-space: pre-wrap;
		line-height: 100%;

		.el-tree-node {
			display: inline-block;
		}

		.el-tree-node__content {
			padding-left: 5px !important;
			padding-right: 5px;

			// .el-tree-node__expand-icon {
			// 	display: none;
			// }
		}
	}
}
</style>
