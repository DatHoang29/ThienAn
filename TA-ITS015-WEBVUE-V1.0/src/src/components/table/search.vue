<template>
	<div class="table-search-container" v-if="props.search.length > 0">
		<el-form ref="tableSearchRef" :model="state.innerModelValue" label-width="100px" class="table-form">
			<el-row :gutter="20">
				<!-- <el-col :xs="12" :sm="8" :md="8" :lg="6" :xl="4" class="mb20"></el-col> -->
				<el-col :xs="12" :sm="5" :md="5" :lg="6" :xl="4" class="mb20" v-for="(val, key) in search" :key="key" v-show="key < 3 || state.isToggle">
					<template v-if="val.type">
						<el-form-item
							label-width="auto"
							:label="val.label"
							:prop="val.prop"
							:rules="[{ required: val.required, message: `${val.label} cannot be empty`, trigger: val.type === 'input' ? 'blur' : 'change' }]"
						>
							<el-input
								v-model="state.innerModelValue[val.prop]"
								v-bind="val.comProps"
								:placeholder="val.placeholder"
								:clearable="!val.required"
								v-if="val.type === 'input'"
								@keyup.enter="onSearch(tableSearchRef)"
								@change="val.change"
								class="w100"
							/>
							<el-date-picker
								v-model="state.innerModelValue[val.prop]"
								v-bind="val.comProps"
								type="date"
								:placeholder="val.placeholder"
								:clearable="!val.required"
								v-else-if="val.type === 'date'"
								@change="val.change"
								class="w100"
							/>
							<el-date-picker
								v-model="state.innerModelValue[val.prop]"
								v-bind="val.comProps"
								type="monthrange"
								value-format="YYYY/MM/DD"
								:placeholder="val.placeholder"
								:clearable="!val.required"
								v-else-if="val.type === 'monthrange'"
								@change="val.change"
								class="w100"
							/>
							<el-date-picker
								v-model="state.innerModelValue[val.prop]"
								v-bind="val.comProps"
								type="daterange"
								value-format="YYYY/MM/DD"
								range-separator="to"
								start-placeholder="Start Date"
								end-placeholder="End Date"
								:clearable="!val.required"
								:shortcuts="shortcuts"
								:default-time="defaultTime"
								v-else-if="val.type === 'daterange'"
								@change="val.change"
								class="w100"
							/>
							<el-select
								v-model="state.innerModelValue[val.prop]"
								v-bind="val.comProps"
								:clearable="!val.required"
								:placeholder="val.placeholder"
								v-else-if="val.type === 'select'"
								@change="val.change"
								class="w100"
							>
								<el-option v-for="item in val.options" :key="item.value" :label="item.label" :value="item.value"> </el-option>
							</el-select>
							<el-cascader
								v-else-if="val.type === 'cascader' && val.cascaderData"
								:options="val.cascaderData"
								:clearable="!val.required"
								filterable
								:props="val.cascaderProps ? val.cascaderProps : state.cascaderProps"
								:placeholder="val.placeholder"
								@change="val.change"
								class="w100"
								v-bind="val.comProps"
								v-model="state.innerModelValue[val.prop]"
							>
							</el-cascader>
						</el-form-item>
					</template>
				</el-col>
				<el-col :xs="12" :sm="9" :md="9" :lg="6" :xl="4" class="mb20">
					<el-form-item class="table-form-btn" label-width="auto">
						<div>
							<!-- Using el-button-group will cause the right border of buttons with type attribute to not display -->
							<!-- <el-button-group> -->
							<el-button plain type="primary" icon="ele-Search" @click="onSearch(tableSearchRef)"> Search </el-button>
							<el-button icon="ele-Refresh" @click="onReset(tableSearchRef)" style="margin-left: 12px"> Reset </el-button>
							<!-- </el-button-group> -->
						</div>
					</el-form-item>
				</el-col>
			</el-row>
			<el-divider style="margin-top: 5px" v-if="search.length > 3">
				<el-button :icon="state.isToggle ? 'ele-ArrowUpBold' : 'ele-ArrowDownBold'" class="divider-btn" @click="state.isToggle = !state.isToggle"> </el-button>
			</el-divider>
		</el-form>
	</div>
</template>

<script setup lang="ts" name="makeTableDemoSearch">
import { reactive, ref, watch } from 'vue';
import type { FormInstance } from 'element-plus';
import { dayjs } from 'element-plus';

// Define the values passed from the parent component
const props = defineProps({
	// Search form, type - control type (input, select, cascader, date), options - required when type is select, cascaderData, cascaderProps - required when type is cascader, properties same as elementUI, if cascaderProps is not passed, use state defaults.
	// Can include comProps attribute, corresponding to the properties of the control used
	search: {
		type: Array<TableSearchType>,
		default: () => [],
	},
	modelValue: {
		type: Object,
		default: () => ({}),
	},
});

// Define events/values passed from child component to parent
const emit = defineEmits(['search', 'reset', 'update:modelValue']);

// Define variable content
const tableSearchRef = ref<FormInstance>();
const state = reactive({
	isToggle: false,
	cascaderProps: { checkStrictly: true, emitPath: false, value: 'id', label: 'name', expandTrigger: 'hover' },
	/** Internal modelValue */
	innerModelValue: {} as EmptyObjectType,
});

/** Watch for changes in props.modelValue */
watch(
	() => props.modelValue,
	(val) => {
		state.innerModelValue = val;
	}
);

/** Watch for changes in state.innerModelValue */
watch(
	() => state.innerModelValue,
	(val) => {
		emit('update:modelValue', val);
	},
	{ deep: true }
);

// Search
const onSearch = (formEl: FormInstance | undefined) => {
	if (!formEl) return;
	formEl.validate((isValid: boolean): void => {
		if (!isValid) return;

		emit('search', state.innerModelValue);
	});
};

// Reset
const onReset = (formEl: FormInstance | undefined) => {
	if (!formEl) return;
	formEl.resetFields();
	emit('reset', state.innerModelValue);
};

/** Default time for date range */
const defaultTime = ref<[Date, Date]>([new Date(2000, 1, 1, 0, 0, 0), new Date(2000, 2, 1, 23, 59, 59)]);
/** Shortcuts for date range selection */
const shortcuts = [
	{
		text: 'Within 7 days',
		value: () => {
			const end = dayjs().endOf('day').toDate();
			const start = dayjs().startOf('day').add(-7, 'day').toDate();
			return [start, end];
		},
	},
	{
		text: 'Within 1 month',
		value: () => {
			const end = dayjs().endOf('day').toDate();
			const start = dayjs().startOf('day').add(-1, 'month').toDate();
			return [start, end];
		},
	},
	{
		text: 'Within 3 months',
		value: () => {
			const end = dayjs().endOf('day').toDate();
			const start = dayjs().startOf('day').add(-3, 'month').toDate();
			return [start, end];
		},
	},
];
</script>

<style scoped lang="scss">
.table-search-container {
	display: flex;

	.table-form {
		flex: 1;

		.table-form-btn-toggle {
			white-space: nowrap;
			user-select: none;
			display: flex;
			align-items: center;
			color: var(--el-color-primary);
		}
	}
}

.divider-btn {
	height: 20px;
	border-radius: 10px;
}
</style>
