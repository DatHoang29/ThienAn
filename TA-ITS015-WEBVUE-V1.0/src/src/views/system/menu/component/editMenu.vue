<template>
	<div class="sys-menu-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" destroy-on-close width="700px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<el-form :model="state.editForm" ref="editFormRef" label-width="auto" label-position="left">
				<el-row :gutter="10">
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.sysMenu.pid')" prop="pid" :rules="tabParentRules">
							<el-cascader
								:options="state.menuTree"
								:props="{ checkStrictly: true, emitPath: false, value: 'id', label: 'title', disabled: isParentDisabled }"
								:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.sysMenu.pid') })"
								clearable
								class="w100"
								v-model="state.editForm.pid"
							>
								<template #default="{ node, data }">
									<span>{{ data.title }}</span>
									<span v-if="!node.isLeaf"> ({{ data.children.length }}) </span>
								</template>
							</el-cascader>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item
							:label="$t('lz.entity.base.type')"
							prop="type"
							:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.type') }), trigger: 'blur' }]"
						>
							<el-radio-group v-model="state.editForm.type">
								<el-radio v-for="item in props.typeList" :key="item.code" :label="item.name" :value="item.code">
									{{ item.name }}
								</el-radio>
							</el-radio-group>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item
							:label="$t('lz.entity.base.name')"
							prop="title"
							:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.name') }), trigger: 'blur' }]"
						>
							<el-input v-model="state.editForm.title" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.name') })" clearable />
						</el-form-item>
					</el-col>
					<template v-if="state.editForm.type == 'dir' || state.editForm.type == 'menu'">
						<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
							<el-form-item :label="$t('lz.entity.sysMenu.routeName')" prop="title">
								<el-input v-model="state.editForm.name" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.sysMenu.routeName') })" clearable />
							</el-form-item>
						</el-col>
						<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
							<el-form-item :label="$t('lz.entity.sysMenu.path')" prop="path">
								<el-input v-model="state.editForm.path" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.sysMenu.path') })" clearable />
							</el-form-item>
						</el-col>
						<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
							<el-form-item :label="$t('lz.entity.sysMenu.componentPath')" prop="component">
								<el-input v-model="state.editForm.component" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.sysMenu.componentPath') })" clearable />
							</el-form-item>
						</el-col>
						<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
							<el-form-item :label="$t('lz.entity.base.icon')" prop="icon">
								<IconSelector
									v-model="state.editForm.icon as any"
									:size="getGlobalComponentSize"
									:clearable="true"
									:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.base.icon') })"
									type="all"
								/>
							</el-form-item>
						</el-col>
						<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
							<el-form-item :label="$t('lz.entity.sysMenu.redirect')">
								<el-input v-model="state.editForm.redirect" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.sysMenu.redirect') })" clearable />
							</el-form-item>
						</el-col>
						<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
							<el-form-item :label="$t('lz.entity.sysMenu.outLink')">
								<el-input v-model="state.editForm.outLink" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.sysMenu.outLink') })" clearable />
							</el-form-item>
						</el-col>
						<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
							<el-form-item :label="$t('lz.entity.base.orderNo')">
								<el-input-number v-model="state.editForm.orderNo" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.orderNo') })" class="w100" />
							</el-form-item>
						</el-col>
						<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
							<el-form-item :label="$t('lz.entity.sysMenu.isHidden')">
								<el-radio-group v-model="state.editForm.isHide">
									<el-radio v-for="item in state.booleanList" :key="item.code" :label="item.name" :value="item.code">
										{{ item.name }}
									</el-radio>
								</el-radio-group>
							</el-form-item>
						</el-col>
						<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
							<el-form-item :label="$t('lz.entity.sysMenu.isCached')">
								<el-radio-group v-model="state.editForm.isKeepAlive">
									<el-radio v-for="item in state.booleanList" :key="item.code" :label="item.name" :value="item.code">
										{{ item.name }}
									</el-radio>
								</el-radio-group>
							</el-form-item>
						</el-col>
						<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
							<el-form-item :label="$t('lz.entity.sysMenu.isFixed')">
								<el-radio-group v-model="state.editForm.isAffix">
									<el-radio v-for="item in state.booleanList" :key="item.code" :label="item.name" :value="item.code">
										{{ item.name }}
									</el-radio>
								</el-radio-group>
							</el-form-item>
						</el-col>
						<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
							<el-form-item :label="$t('lz.entity.sysMenu.isEmbedded')">
								<el-radio-group v-model="state.editForm.isIframe">
									<el-radio v-for="item in state.booleanList" :key="item.code" :label="item.name" :value="item.code">
										{{ item.name }}
									</el-radio>
								</el-radio-group>
							</el-form-item>
						</el-col>
					</template>
					<template v-if="state.editForm.type == 'tab'">
						<el-col :xs="24" :sm="12" class="mb20">
							<el-form-item
								:label="$t('lz.entity.sysMenu.routeName')"
								prop="name"
								:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.sysMenu.routeName') }), trigger: 'blur' }]"
							>
								<el-input v-model="state.editForm.name" clearable />
							</el-form-item>
						</el-col>
						<el-col :xs="24" :sm="12" class="mb20">
							<el-form-item :label="$t('lz.entity.sysMenu.componentPath')" prop="component" :rules="[{ validator: validateTabComponent, trigger: 'blur' }]">
								<el-input v-model="state.editForm.component" placeholder="system/user/index" clearable />
							</el-form-item>
						</el-col>
						<el-col :xs="24" :sm="12" class="mb20">
							<el-form-item :label="$t('lz.entity.base.icon')" prop="icon">
								<IconSelector v-model="state.editForm.icon as any" :size="getGlobalComponentSize" :clearable="true" type="all" />
							</el-form-item>
						</el-col>
						<el-col :xs="24" :sm="12" class="mb20">
							<el-form-item :label="$t('lz.entity.base.orderNo')">
								<el-input-number v-model="state.editForm.orderNo" :min="0" class="w100" />
							</el-form-item>
						</el-col>
						<el-col :xs="24" :sm="12" class="mb20" v-for="field in tabBooleanFields" :key="field.key">
							<el-form-item :label="$t(field.label)">
								<el-radio-group v-model="state.editForm[field.key]">
									<el-radio v-for="item in state.booleanList" :key="String(item.code)" :value="item.code">{{ item.name }}</el-radio>
								</el-radio-group>
							</el-form-item>
						</el-col>
					</template>
					<template v-if="state.editForm.type == 'btn'">
						<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
							<el-form-item :label="$t('lz.entity.sysMenu.permissionIdentifier')">
								<el-input v-model="state.editForm.permission" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.sysMenu.permissionIdentifier') })" clearable />
							</el-form-item>
						</el-col>
						<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
							<el-form-item :label="$t('lz.entity.base.orderNo')">
								<el-input-number v-model="state.editForm.orderNo" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.orderNo') })" min="0" class="w100" />
							</el-form-item>
						</el-col>
					</template>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.status')">
							<el-radio-group v-model="state.editForm.status" v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code">
								<el-radio :value="parseInt(item.code)" class="mr5">{{ item.name }}</el-radio>
							</el-radio-group>
						</el-form-item>
					</el-col>
					<el-col v-if="state.editForm.type != 'tab'" :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.sysMenu.isFullscreen')">
							<el-radio-group v-model="state.editForm.isFullscreen">
								<el-radio v-for="item in state.booleanList" :key="item.code" :label="item.name" :value="item.code">
									{{ item.name }}
								</el-radio>
							</el-radio-group>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.base.remark')">
							<el-input v-model="state.editForm.remark" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.remark') })" clearable type="textarea" />
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

<script lang="ts" setup name="sysEditMenu">
import { computed, nextTick, reactive, ref, watch } from 'vue';
import IconSelector from '/@/components/iconSelector/index.vue';
import other from '/@/utils/other';
import { OperateType } from '/@/types/constants';

import { getAPI } from '/@/utils/axios-utils';
import { SysMenuApi } from '/@/api-services/system';
import { SysMenu, UpdateMenuInput } from '/@/api-services/system';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import { findMenuNode, resolveViewModule } from '/@/utils/menuTabs';
import { viewModules } from '/@/router/viewModules';
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const { t: $t } = useI18n();

interface Props {
	title: String;
	menuData: Array<SysMenu>;
	operateType: OperateType;
	typeList: Array<any>;
}

const props = defineProps<Props>();

const emits = defineEmits(['handleQuery']);
const editFormRef = ref();
const state = reactive({
	isShowDialog: false,
	isProcessing: false,
	editForm: {} as UpdateMenuInput,
	booleanList: [] as any,
	// Full menu tree for the parent picker; the grid data in index.vue may be filtered
	menuTree: [] as Array<SysMenu>,
});

const tabBooleanFields = [
	{ key: 'isHide', label: 'lz.entity.sysMenu.isHidden' },
	{ key: 'isKeepAlive', label: 'lz.entity.sysMenu.isCached' },
] as const;
// Only an edited record has a real id; a copy still carries the source id until it is saved
const editingId = () => (props.operateType === 'edit' ? state.editForm.id : undefined);
// The edited node and its descendants cannot become its parent
const blockedParentIds = computed(() => {
	const ids = new Set<string>();
	const id = editingId();
	const collect = (node?: SysMenu) => {
		if (!node?.id) return;
		ids.add(node.id);
		node.children?.forEach(collect);
	};
	if (id) collect(findMenuNode<SysMenu>(state.menuTree, id));
	return ids;
});
const isParentDisabled = (data: SysMenu) => data.type === 'btn' || blockedParentIds.value.has(data.id ?? '') || (state.editForm.type === 'tab' && data.type !== 'menu');
const tabParentRules = computed(() =>
	state.editForm.type === 'tab'
		? [
				{
					validator: (_rule: unknown, value: string, callback: (error?: Error) => void) => {
						const parent = findMenuNode<SysMenu>(state.menuTree, value);
						callback(parent?.type === 'menu' && parent.id !== editingId() ? undefined : new Error($t('lz.message.sysMenu.tabParent')));
					},
					trigger: 'change',
				},
			]
		: []
);
// Re-check the parent when the type changes so a stale message does not block saving
watch(
	() => [state.editForm, state.editForm.type] as const,
	([form], [prevForm]) => {
		// Opening the dialog replaces the form; only react to the user switching the type
		if (form !== prevForm) return;
		nextTick(() => (form.pid ? editFormRef.value?.validateField('pid').catch(() => {}) : editFormRef.value?.clearValidate('pid')));
	}
);
const validateTabComponent = (_rule: unknown, value: string, callback: (error?: Error) => void) => {
	const parent = findMenuNode<SysMenu>(state.menuTree, state.editForm.pid ?? '');
	const loader = resolveViewModule(viewModules, value);
	const parentLoader = resolveViewModule(viewModules, parent?.component);
	callback(loader && loader !== parentLoader ? undefined : new Error($t('lz.message.sysMenu.tabComponent')));
};

// Get global component size
const getGlobalComponentSize = computed(() => {
	return other.globalComponentSize();
});

// Open dialog
const openDialog = async (row: any) => {
	editFormRef.value?.resetFields();
	state.editForm = JSON.parse(JSON.stringify(row));
	// Copy items instead of converting the shared config store in place
	state.booleanList = getConfigDataByCode(BaseConfigTypeEnum.SysBooleanType).map((item: any) => ({ ...item, code: item.code.toString() == 'true' }));
	state.menuTree = props.menuData ?? [];
	state.isShowDialog = true;
	nextTick(() => editFormRef.value?.clearValidate());

	try {
		const res = await getAPI(SysMenuApi).apiSystemSysmenuListGet();
		state.menuTree = res.data.result ?? state.menuTree;
	} catch {
		// Keep the grid tree passed by index.vue
	}
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
			if (props.operateType == 'edit') {
				await getAPI(SysMenuApi).apiSystemSysmenuUpdatePost(state.editForm);
			}
			//Copy or Add
			else if (props.operateType == 'copy' || props.operateType == 'add') {
				state.editForm.id = '';
				await getAPI(SysMenuApi).apiSystemSysmenuAddPost(state.editForm);
			}
			ElMessage.success($t('lz.message.base.saveSuccess'));
			closeDialog();
		} finally {
			state.isProcessing = false;
		}
	});
};

// Export object
defineExpose({ openDialog });
</script>
