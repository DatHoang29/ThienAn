<template>
	<!-- KienLT - 09/11/2026: form tìm kiếm dùng chung cho tab Theo lượt / Theo chuyến (đặt trong slot #filters của GridSearchPanel). -->
	<el-form :model="queryParams" :show-message="false" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%">
		<el-row :gutter="10">
			<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
				<el-form-item :label="$t('lz.label.base.fromDate')" prop="fromDate">
					<el-date-picker
						v-model="queryParams.fromDate"
						type="datetime"
						:disabled-date="(time: any) => time.getTime() > Date.now()"
						:format="DateTimeFormatEnum.DateTimeDefault"
						:value-format="DateTimeFormatEnum.DateTimeValidInput"
						:placeholder="$t('lz.label.base.fromDate')"
						clearable
						class="w100"
					/>
				</el-form-item>
			</el-col>
			<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
				<el-form-item :label="$t('lz.entity.tms.direction')" prop="direction">
					<el-select v-model="queryParams.direction" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.direction') })" filterable clearable class="w100">
						<el-option v-for="item in filter.lists.directionList" :key="item.code" :label="item.name" :value="item.code" />
					</el-select>
				</el-form-item>
			</el-col>
			<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
				<el-form-item :label="$t('lz.entity.base.type')" prop="type">
					<el-select v-model="queryParams.type" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.type') })" filterable clearable class="w100">
						<el-option v-for="item in filter.lists.filterTypeList" :key="item.code" :label="item.name" :value="item.code" />
					</el-select>
				</el-form-item>
			</el-col>
			<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
				<el-form-item :label="$t('lz.entity.tms.licensePlate')" prop="licensePlate">
					<el-input
						v-model="queryParams.licensePlate"
						:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.tms.licensePlate') })"
						@keyup.enter="emit('search')"
						:maxlength="EntityFieldLength.Length32"
						show-word-limit
						clearable
						class="w100"
					/>
				</el-form-item>
			</el-col>
			<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
				<el-form-item :label="$t('lz.label.base.toDate')" prop="toDate">
					<el-date-picker
						v-model="queryParams.toDate"
						type="datetime"
						:disabled-date="(time: any) => time.getTime() > Date.now()"
						:format="DateTimeFormatEnum.DateTimeDefault"
						:value-format="DateTimeFormatEnum.DateTimeValidInput"
						:placeholder="$t('lz.label.base.toDate')"
						clearable
						class="w100"
					/>
				</el-form-item>
			</el-col>
			<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
				<el-form-item :label="$t('lz.entity.tmsVehicleFilterHistory.equipmentId')" prop="equipmentId">
					<el-select v-model="queryParams.equipmentId" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tmsVehicleFilterHistory.equipmentId') })" filterable clearable class="w100">
						<el-option v-for="item in filter.vdsCameraList.value" :key="String(item.id)" :label="filter.cameraLabel(item)" :value="String(item.id)" />
					</el-select>
				</el-form-item>
			</el-col>
		</el-row>
	</el-form>
	<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />
	<el-row>
		<SearchButton authSearch="tmsVehicleFilterHistory:list" @search="emit('search')" @reset="emit('reset')" :loading="loading" />
	</el-row>
</template>

<script lang="ts" setup name="vehicleFilterSearch">
import { defineAsyncComponent } from 'vue';
import { DateTimeFormatEnum, EntityFieldLength } from '/@/types/enums/baseEnum';
import type { VehicleFilterHistoryContext } from './useVehicleFilterHistory';

const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));

const props = defineProps<{ filter: VehicleFilterHistoryContext; loading?: boolean }>();
const emit = defineEmits<{ (e: 'search'): void; (e: 'reset'): void }>();

// Bộ lọc là object reactive của trang cha (useVehicleFilterHistory); form chỉ sửa giá trị bên trong.
const queryParams = props.filter.queryParams;
</script>
