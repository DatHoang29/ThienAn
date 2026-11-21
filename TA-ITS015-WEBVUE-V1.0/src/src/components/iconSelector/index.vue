<template>
	<div class="icon-selector w100 h100">
		<el-input v-model="state.fontIconSearch" :placeholder="state.fontIconPlaceholder" :clearable="true" :disabled="disabled" :size="size" ref="inputWidthRef" @clear="onClearFontIcon">
			<template #prepend>
				<SvgIcon
					:name="state.fontIconPrefix === '' ? prepend : state.fontIconPrefix"
					class="font14"
					v-if="state.fontIconPrefix === '' ? prepend?.indexOf('ele-') > -1 : state.fontIconPrefix?.indexOf('ele-') > -1"
				/>
				<i v-else :class="state.fontIconPrefix === '' ? prepend : state.fontIconPrefix" class="font14"></i>
			</template>
		</el-input>
		<el-popover placement="bottom" :width="state.fontIconWidth" transition="el-zoom-in-top" popper-class="icon-selector-popper" trigger="click" :virtual-ref="inputWidthRef" virtual-triggering>
			<template #default>
				<div class="icon-selector-warp">
					<div class="icon-selector-warp-title">{{ title }}</div>
					<el-tooltip :content="$t('lz.button.base.clear')" placement="top">
						<el-button @click="onClearFontIcon" size="small" type="danger" style="float: right;" ><i class="fa fa-eraser"></i></el-button>

					</el-tooltip>
					<el-tabs v-model="state.fontIconTabActive" @tab-click="onIconClick">
						<el-tab-pane lazy label="ali" name="ali">
							<IconList :list="fontIconSheetsFilterList" :empty="$t(emptyDescription)" :prefix="state.fontIconPrefix" @get-icon="onColClick" />
						</el-tab-pane>
						<el-tab-pane lazy label="ele" name="ele">
							<IconList :list="fontIconSheetsFilterList" :empty="$t(emptyDescription)" :prefix="state.fontIconPrefix" @get-icon="onColClick" />
						</el-tab-pane>
						<el-tab-pane lazy label="awe" name="awe">
							<IconList :list="fontIconSheetsFilterList" :empty="$t(emptyDescription)" :prefix="state.fontIconPrefix" @get-icon="onColClick" />
						</el-tab-pane>
							
					</el-tabs>
				</div>
			</template>
		</el-popover>
	</div>
</template>

<script setup lang="ts" name="iconSelector">
import { defineAsyncComponent, ref, reactive, onMounted, nextTick, computed, watch } from 'vue';
import type { TabsPaneContext } from 'element-plus';
import initIconfont from '/@/utils/getStyleSheets';
import '/@/theme/iconSelector.scss';

// Define the values passed from the parent component
const props = defineProps({
	// Prepend content for the input box
	prepend: {
		type: String,
		default: () => '',
	},
	// Placeholder text for the input box
	placeholder: {
		type: String,
		default: () => 'Please enter content to search for icons or select an icon',
	},
	// Size of the input box
	size: {
		type: String,
		default: () => 'default',
	},
	// Title of the pop-up
	title: {
		type: String,
		default: () => '',
	},
	// Disabled state
	disabled: {
		type: Boolean,
		default: () => false,
	},
	// Whether it can be cleared
	clearable: {
		type: Boolean,
		default: () => true,
	},
	// Custom empty state description text
	emptyDescription: {
		type: String,
		default: () => 'lz.ui.label.noRelatedIcon',
	},
	// Two-way binding value, default is modelValue
	modelValue: String ,
});

// Define events to pass values from child component to parent
const emit = defineEmits(['update:modelValue', 'get', 'clear']);

// Import component
const IconList = defineAsyncComponent(() => import('/@/components/iconSelector/list.vue'));

// Define variable content
const inputWidthRef = ref();
const state = reactive({
	fontIconPrefix: '',
	fontIconWidth: 0,
	fontIconSearch: '',
	fontIconPlaceholder: '',
	fontIconTabActive: 'ali',
	fontIconList: {
		ali: [],
		ele: [],
		awe: [],
	},
});

// Icon search and display
const fontIconSheetsFilterList = computed(() => {
	const list = fontIconTabNameList();
	if (!state.fontIconSearch) return list;
	let search = state.fontIconSearch.trim().toLowerCase();
	return list.filter((item: string) => {
		if (item.toLowerCase().indexOf(search) !== -1) return item;
	});
});
// Set icons based on tab name type
const fontIconTabNameList = () => {
	let iconList: any = [];
	if (state.fontIconTabActive === 'ali') iconList = state.fontIconList.ali;
	else if (state.fontIconTabActive === 'ele') iconList = state.fontIconList.ele;
	else if (state.fontIconTabActive === 'awe') iconList = state.fontIconList.awe;
	return iconList;
};
// Handle icon two-way binding value echo
const initModeValueEcho = () => {
	if (props.modelValue === '') return ((<string | undefined>state.fontIconPlaceholder) = props.placeholder);
	(<string | undefined>state.fontIconPlaceholder) = props.modelValue;
	(<string | undefined>state.fontIconPrefix) = props.modelValue;
};
// Handle icon type for echoing tab highlight and initializing data
const initFontIconName = () => {
	let name = 'ali';
	if (props.modelValue == undefined) name = 'ele';
	else if (props.modelValue!.indexOf('iconfont') > -1) name = 'ali';
	else if (props.modelValue!.indexOf('ele-') > -1) name = 'ele';
	else if (props.modelValue!.indexOf('fa') > -1) name = 'awe';
	// Initialize tab highlight echo
	state.fontIconTabActive = name;
	return name;
};
// Initialize data
const initFontIconData = async (name: string) => {
	if (name === 'ali') {
		// Ali font icon uses `iconfont xxx`
		if (state.fontIconList.ali.length > 0) return;
		await initIconfont.ali().then((res: any) => {
			state.fontIconList.ali = res.map((i: string) => `iconfont ${i}`);
		});
	} else if (name === 'ele') {
		// Element Plus icons
		if (state.fontIconList.ele.length > 0) return;
		await initIconfont.ele().then((res: any) => {
			state.fontIconList.ele = res;
		});
	} else if (name === 'awe') {
		// Font Awesome font icon uses `fa xxx`
		if (state.fontIconList.awe.length > 0) return;
		await initIconfont.awe().then((res: any) => {
			state.fontIconList.awe = res.map((i: string) => `fa ${i}`);
		});
	}
	// Initialize input placeholder
	state.fontIconPlaceholder = props.placeholder;
	// Initialize two-way binding echo
	initModeValueEcho();
};
// Icon click switch
const onIconClick = (pane: TabsPaneContext) => {
	initFontIconData(pane.paneName as string);
	inputWidthRef.value.focus();
};
// Get the currently clicked icon
const onColClick = (v: string) => {
	state.fontIconPlaceholder = v;
	state.fontIconPrefix = v;
	emit('get', state.fontIconPrefix);
	emit('update:modelValue', state.fontIconPrefix);
	inputWidthRef.value.focus();
};
// Clear the currently clicked icon
const onClearFontIcon = () => {
	state.fontIconPrefix = '';
	emit('clear', state.fontIconPrefix);
	emit('update:modelValue', state.fontIconPrefix);
};
// Get the width of the input
const getInputWidth = () => {
	nextTick(() => {
		state.fontIconWidth = inputWidthRef.value.$el.offsetWidth;
	});
};
// Listen for window width changes
const initResize = () => {
	window.addEventListener('resize', () => {
		getInputWidth();
	});
};
// On page load
onMounted(() => {
	initFontIconData(initFontIconName());
	initResize();
	getInputWidth();
});
// Watch for changes in the two-way binding modelValue
watch(
	() => props.modelValue,
	() => {
		state.fontIconSearch = '';
		initModeValueEcho();
		initFontIconName();
		getInputWidth();
	}
);
</script>
