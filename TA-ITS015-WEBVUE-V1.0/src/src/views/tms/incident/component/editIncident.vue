<template>
	<div class="edit-incident-container">
		<!-- SonTH - 24/07/2026: append-to-body là BẮT BUỘC ở dialog có el-select/el-date-picker.
			 el-dialog mặc định KHÔNG teleport (appendToBody = false) nên render tại chỗ trong DOM cha,
			 còn dropdown của el-select mặc định teleport ra body. Hai bên nằm ở hai stacking context
			 khác nhau nên việc so z-index trở nên vô nghĩa: chỉ cần một tổ tiên của dialog tạo
			 stacking context có z-index cao là cả dialog đè lên dropdown.
			 Đưa dialog ra body để nó và dropdown thành hai phần tử cùng cấp, khi đó bộ đếm z-index
			 của Element Plus quyết định đúng thứ tự (mở sau nằm trên). -->
		<el-dialog v-model="state.isShowDialog" @closed="handleOnDialogClosed" draggable :close-on-click-modal="false" width="700px" append-to-body>
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-Edit /></el-icon>
					<span> {{ state.dialogTitle }} </span>
				</div>
			</template>
			<IncidentVehicleInfo :metadata="state.editForm.metadata" />
                    <el-form :model="state.editForm" ref="editFormRef" label-width="auto" label-position="left">
				<el-tabs>
					<el-tab-pane :label="$t('lz.label.base.info')">
						<br>
						<el-row :gutter="10">
							<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.tms.eventType')" prop="eventTypeId" :rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.tms.eventType')}), trigger: 'blur' }]">
									<el-select v-model="state.editForm.eventTypeId" :disabled="state.isEditMode" @change="generateCodeAndName" :loading="state.isConfigLoading" :placeholder="$t('lz.label.base.pleaseSelect', {input: $t('lz.entity.tms.eventType')})" :loading-text="$t('lz.label.base.loading')" class="w100" popper-class="event-type-select-popper">
										<template v-if="selectedEventType" #prefix>
											<span class="event-type-option__icon event-type-option__icon--prefix">
												<img :src="media.eventTypeIcon(selectedEventType)" alt="" @error="media.onEventTypeIconError" />
											</span>
										</template>
										<el-option v-for="item in state.eventTypeList" :key="item.id" :label="item.name" :value="item.id">
											<span class="event-type-option">
												<span class="event-type-option__icon">
													<img :src="media.eventTypeIcon(item)" alt="" loading="lazy" @error="media.onEventTypeIconError" />
												</span>
												<span class="event-type-option__text">{{ item.name }}</span>
											</span>
										</el-option>
										<template #loading>
											{{ $t('lz.label.base.loading') }}
										</template>
									</el-select>
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.tmsIncident.startDate')" prop="startDate" :rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.tmsIncident.startDate')}), trigger: 'blur' }, { validator: validateStartDate, trigger: 'change' }]">
										<el-date-picker
											v-model="state.editForm.startDate"
											type="datetime"
											:format="DateTimeFormatEnum.DateTimeDefault"
											:disabled-date="(time:any) => time.getTime() > Date.now()"
											:clearable="false"
											:placeholder="$t('lz.entity.tmsIncident.startDate')"
											@change="handleStartDateChange"
											class="w100"
										/>
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.label.base.location')" prop="location" :rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.label.base.location')}), trigger: 'blur' }]">
									<el-select v-model="state.editForm.location" @change="handleOnLocationChange" :loading="state.isConfigLoading" :loading-text="$t('lz.label.base.loading')" :placeholder="$t('lz.label.base.pleaseSelect', {input: $t('lz.label.base.location')})" class="w100">
									<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TmsZoneLocation)" :key="item.code" :label="item.name" :value="item.code" />
										<template #loading>
											{{ $t('lz.label.base.loading') }}
										</template>
									</el-select>
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.tmsIncident.influenceScope')" prop="influenceScope" :rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.tmsIncident.influenceScope')}), trigger: 'blur' }]">
									<el-select v-model="state.editForm.influenceScope" @change="handleOnDirectionChange" :loading="state.isConfigLoading" :loading-text="$t('lz.label.base.loading')" :placeholder="$t('lz.label.base.pleaseSelect', {input: $t('lz.entity.tmsIncident.influenceScope')})" class="w100">
										<el-option v-for="item in state.directionList" :label="item.name" :value="item.code" />
										<template #loading>
											{{ $t('lz.label.base.loading') }}
										</template>
									</el-select>
								</el-form-item>
							</el-col>
						</el-row>
						<el-row :gutter="10">
							<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.tmsIncident.kmNumber')">
									<el-input-number
										v-model="incidentKmNumber"
										:min="0"
										:placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.tmsIncident.kmNumber')})"
										:disabled="state.disableKmNumber"
										@change="generateCodeAndName"
										class="w100"
									/>
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.tmsIncident.metNumber')">
									<el-input-number
										v-model="incidentMetNumber"
										:min="0"
										:max="999"
										:placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.tmsIncident.metNumber')})"
										:disabled="state.disableMetNumber"
										@change="generateCodeAndName"
										class="w100"
									/>
								</el-form-item>
							</el-col>
						</el-row>
						<el-divider />
						<el-row :gutter="10">
							<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.tmsIncident.source')" prop="source">
									<el-select v-model="state.editForm.source" :loading="state.isConfigLoading" :loading-text="$t('lz.label.base.loading')" clearable class="w100">
										<el-option v-for="item in state.sourceDetectList" :label="item.name" :value="item.code" />
										<template #loading>
											{{ $t('lz.label.base.loading') }}
										</template>
									</el-select>
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="$t('lz.entity.base.code')" prop="code" :rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.base.code')}), trigger: 'blur' }, { max: EntityFieldLength.Length128, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.base.code'), MaxLength: EntityFieldLength.Length128}), trigger: 'blur'}]">
									<el-input v-model="state.editForm.code" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.base.code')})" :maxlength="EntityFieldLength.Length128" show-word-limit clearable />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24">
								<el-form-item :label="$t('lz.entity.base.name')" prop="name" :rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.base.name')}), trigger: 'blur' }, { max: EntityFieldLength.Length128, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.base.name'), MaxLength: EntityFieldLength.Length128}), trigger: 'blur'}]">
									<el-input v-model="state.editForm.name" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.base.name')})" :maxlength="EntityFieldLength.Length128" show-word-limit clearable />
								</el-form-item>
							</el-col>
						</el-row>
						<el-divider />
						<el-row :gutter="10">
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="$t('lz.entity.tmsIncident.zoneId')">
									<div v-if="state.isZoneLoading"class="text-center">
										{{ $t('lz.label.base.loading') }}
									</div>
									<div v-if="!state.isZoneLoading">
										<div v-if="state.zoneList?.length > 0" class="display-flex--horizontal flex-justify--start flex-gap--5px">
											<el-tag size="large" type="primary" v-for="item in state.zoneList" :key="item.id" style="height:50px">
												<p><strong>{{ item.name }}</strong></p>
												<p v-if="item.location"><small>{{ getLocationName(item.location) }}</small></p>
												<div>
													<small>Km{{ item.fromKmNumber ?? 0 }} +{{ item.fromMetNumber ?? 0 }}</small>
													<span>&nbsp;-&nbsp;</span>
													<small>Km{{ item.toKmNumber ?? 0 }}+{{ item.toMetNumber ?? 0 }}</small>
												</div>
											</el-tag>
										</div>
										<p v-else>{{ $t('lz.label.base.noData') }}</p>
									</div>
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="$t('lz.label.base.camera')">
									<div v-if="state.isEquipmentLoading" class="text-center">
										{{ $t('lz.label.base.loading') }}
									</div>
									<div v-if="!state.isEquipmentLoading">
										<div v-if="state.equipmentList?.length > 0" class="display-flex--horizontal flex-justify--start flex-gap--5px">
											<el-button type="primary" size="large" v-for="item in state.equipmentList" round style="margin:0px" @click="showCameraPopup(item)">
												<p class="display-flex--vertical fex-gap--5px">
													<strong>{{ item.name }}</strong>
													<small>Km{{ item.kmNumber ?? 0 }} +{{ item.metNumber ?? 0 }}</small>
												</p>
											</el-button>
										</div>
										<p v-else>{{ $t('lz.label.base.noData') }}</p>
									</div>
								</el-form-item>
							</el-col>
						</el-row>
					</el-tab-pane>
					<el-tab-pane :label="$t('lz.label.base.extend')">
						<br>
						<el-row :gutter="10">
							<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.tmsIncident.injuredNumber')" prop="injuredNumber">
									<el-input-number
										v-model="state.editForm.injuredNumber"
										:min="0"
										:placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.tmsIncident.injuredNumber')})"
										class="w100"
									/>
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.tmsIncident.vehicleNumber')" prop="vehicleNumber">
									<el-input-number
										v-model="state.editForm.vehicleNumber"
										:min="0"
										:placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.tmsIncident.vehicleNumber')})"
										class="w100"
									/>
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="$t('lz.entity.base.desc')" prop="seat" :rules="[{ max: EntityFieldLength.Length512, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.base.desc'), MaxLength: EntityFieldLength.Length512}), trigger: 'blur'}]">
									<el-input v-model="state.editForm.description" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.base.desc')})"
										type="textarea" rows="4" :maxlength="EntityFieldLength.Length512" show-word-limit clearable/>
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="$t('lz.entity.base.remark')" prop="remark" :rules="[{ max: EntityFieldLength.Length256, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.base.remark'), MaxLength: EntityFieldLength.Length256}), trigger: 'blur'}]">
									<el-input v-model="state.editForm.remark" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.base.remark')})"
										type="textarea" rows="4" :maxlength="EntityFieldLength.Length256" show-word-limit clearable/>
								</el-form-item>
							</el-col>
						</el-row>
					</el-tab-pane>
				</el-tabs>
			</el-form>
			<template #footer>
				<span class="dialog-footer">
					<el-button @click="cancel">{{ $t('lz.button.base.cancel') }}</el-button>
					<el-button type="primary" @click="submit" :disabled="state.isProcessing">{{ $t('lz.button.base.confirm') }}</el-button>
				</span>
			</template>
		</el-dialog>
	</div>

	<cameraPopupComp ref="cameraPopupCompRef"/>
</template>

<script lang="ts" setup>
	import IncidentVehicleInfo from './incidentVehicleInfo.vue';
import { incidentName } from '/@/utils/incidentNaming';
import { onMounted, reactive, ref, computed, defineAsyncComponent, watch,nextTick } from 'vue';
	import { getAPI } from '/@/utils/axios-utils';
	import { ElMessage } from 'element-plus';
	import { useDateFormat } from '@vueuse/core'
	import { useI18n } from 'vue-i18n';
	import { watchDebounced } from '@vueuse/core';

	import { useTmsStore } from '/@/stores/tmsStore';
	const tmsStore = useTmsStore();
	import { TmsIncidentApi } from '/@/api-services/tms';
	import { UpdateTmsIncidentInput, AddTmsIncidentInput } from '/@/api-services/tms';
	import { DateTimeFormatEnum, SpecialConfigEnum, EntityFieldLength, StatusTypeEnum } from '/@/types/enums/baseEnum';
	import { useCommonVarsAndFuncs } from '/@/stores/its/commonVarsAndFuncs';

	import { useIncidentMonitor } from '/@/stores/its/incidentMonitor';
	import { useItsMediaServerStore } from '/@/stores/its/itsMediaServer';
    const incidentMonitor = useIncidentMonitor();
	const media = useItsMediaServerStore();

	import { useBaseConfig } from '/@/stores/baseConfig';
	import { BaseConfigTypeEnum } from '/@/types/enums';
	const getConfigDataByCode = useBaseConfig().getConfigDataByCode;

	const cameraPopupComp = defineAsyncComponent(() => import('/@/views/modal/common/cameraPopup.vue'));
	const cameraPopupCompRef = ref<InstanceType<typeof cameraPopupComp>>();

	const { t: $t } = useI18n();

	const commonVarsAndFuncs = useCommonVarsAndFuncs();
	const emits = defineEmits(['onSubmitSuccess']);

	const incidentKmNumber = ref(0);
	const incidentMetNumber = ref(0);

	// Custom validator: startDate must not be in the future
	const validateStartDate = (_rule: any, value: any, callback: any) => {
		if (!value) {
			callback();
			return;
		}
		const selectedTime = new Date(value).getTime();
		if (selectedTime > Date.now()) {
			callback(new Error($t('lz.validation.base.startDateNotFuture')));
		} else {
			callback();
		}
	};

	const editFormRef = ref();
	const state = reactive({
		dialogTitle: null,

		isShowDialog: false,
		isEditMode: false,
		isProcessing: false,
		isConfigLoading: false,
		isZoneLoading: false,
		isEquipmentLoading: false,
		isIncidentInfoChanged: false,

		disableKmNumber: false,
		disableMetNumber: false,

		sourceDetectList: [] as any,
		eventTypeList: [] as any,
		locationList: [] as any,
		directionList: [] as any,
		zoneList: [] as any,
		equipmentList: [] as any,
		equipmentTypeList: [] as any,
		incidentStatusList: [] as any,

		editForm: {} as UpdateTmsIncidentInput,
	});

	// Loại sự cố đang chọn: hiện icon ở đầu ô chọn
	const selectedEventType = computed(() => (state.eventTypeList ?? []).find((item: any) => item.id === state.editForm.eventTypeId) ?? null);

	// Load default data
	const loadDefaultData = async() => {
		state.isConfigLoading = true;
		try{

			state.sourceDetectList = getConfigDataByCode(BaseConfigTypeEnum.TmsSourceDetectIncident);
			state.locationList = getConfigDataByCode(BaseConfigTypeEnum.TmsZoneLocation);
			state.incidentStatusList = getConfigDataByCode(BaseConfigTypeEnum.TmsIncidentStatus);

			// TODO: LaneList is temporarily DirectionList => by direction_config_type not tms_lane
			state.directionList = getConfigDataByCode(BaseConfigTypeEnum.TmsDirectionType);

			// Loại 'unknown' chỉ do VDS ghi khi chưa ánh xạ — không cho chọn tay khi tạo/sửa sự cố.
			const allEventTypes = (await tmsStore.getEventTypes()).filter((item: any) => item.code !== 'unknown');
			const currentEventTypeId = incidentMonitor.incidentDialogState?.incidentInfo?.eventTypeId;
			if (state.isEditMode && currentEventTypeId) {
				state.eventTypeList = allEventTypes.filter((item: any) => item.status === StatusTypeEnum.Enable || item.id === currentEventTypeId);
			} else {
				state.eventTypeList = allEventTypes.filter((item: any) => item.status === StatusTypeEnum.Enable);
			}

			state.equipmentTypeList = await tmsStore.getEquipmentTypes();
		}
		catch(error){
			console.error('editIncident.vue: loadDefaultData(...)', error);
		}
		finally{
			state.isConfigLoading = false;
		}
	};

	// Load the zone-list | equipment-list based on the id-list
	const getZoneAndEquipByIdList = async() => {
		try{
			setControlsToLoading(true);

			if(state.editForm.zoneId?.length > 0){
				state.zoneList = await tmsStore.getZonesByIds(state.editForm.zoneId);

				if(state.zoneList?.length > 0){
					state.editForm.zoneId = state.zoneList.map(item => item.id).join(',');

					// Set the location select based on the loaded zone's location
					const zoneLocation = state.zoneList.find((zone: any) => !!zone.location)?.location;
					if(zoneLocation)
						state.editForm.location = zoneLocation;
				}
			}
			await getEquipmentsByPosition(); // Must be use the keyword "await" here

			if(state.editForm.equipmentId?.length > 0){
				state.equipmentList = await tmsStore.getEquipmentByIds(state.editForm.equipmentId);

				if(state.equipmentList?.length > 0)
					state.editForm.equipmentId = state.equipmentList.map(item => item.id).join(',');
			}
		}
		catch(error){
			console.error('editIncident.vue: getZoneAndEquipByIdList(...)', error);
		}
		finally{
			setControlsToLoading(false);
		}
	};

	// Show camera popup
	const showCameraPopup = (cameraInfo: any) => {
		if(cameraInfo)
			cameraPopupCompRef.value?.openDialog(cameraInfo);
		else
			ElMessage.error($t('lz.message.base.invalidData'));
	};

	// Handle on select-controls change
	const generateCodeAndName = () =>{
		state.isIncidentInfoChanged = true;

		const eventType = state.eventTypeList ? state.eventTypeList.find((item: any) => item.id == state.editForm.eventTypeId) : null;
		const location = state.locationList ? state.locationList.find((item: any) => item.code == state.editForm.location) : null;
		const direction = state.directionList ? state.directionList.find((item: any) => item.code == state.editForm.influenceScope) : null;
		console.log('generateCodeAndName(...)', {eventType, location, direction, state: state.editForm.location,list: state.locationList});
		const kmNumber = state.editForm.kmNumber ?? 0;
		const metNumber = state.editForm.metNumber ?? 0;
		const startDate = state.editForm.startDate ?? Date.now();

		state.editForm.code = 'ICC'
			+ (eventType ? '_' + eventType.code : '')
			+ (location ? '_' + location.code : '')
			+ (direction ? '_' + direction.code : '')
			+ '_' + kmNumber
			+ '_' + metNumber
			+ '_' + useDateFormat(startDate, 'DDMMYYYYHHmmss').value;
		
        state.editForm.name = incidentName(
            [eventType?.name, location?.name, direction?.name].filter(Boolean),
            kmNumber, metNumber, useDateFormat(startDate, 'DD-MM-YYYY HH:mm:ss').value
        );
	};

	// Khi người dùng xóa trắng Thời gian => tự động đặt lại thời gian hiện tại
	// (đồng bộ với Mã & Tên vốn đang sinh theo thời gian hiện tại)
	const handleStartDateChange = () => {
		if (!state.editForm.startDate) {
			state.editForm.startDate = Date.now();
		}
		generateCodeAndName();
	};


	// Set controls to loading state
	const setControlsToLoading = (isProcessing: boolean) => {
		// Disable/Enable html controls
		state.disableKmNumber = isProcessing;
		state.disableMetNumber = isProcessing;

		state.isZoneLoading = isProcessing;
		state.isEquipmentLoading = isProcessing;

		state.isProcessing = isProcessing;
	};

	// Get zones by position (kmNumber, metNumber, directionId)
	const getZonesByPosition = async() => {
		try{
			setControlsToLoading(true);

			// Reset the zoneIdList, zoneList
			state.editForm.zoneId = null;
			state.zoneList = [];

			const kmNumber = state.editForm.kmNumber ?? 0;
			const metNumber = state.editForm.metNumber ?? 0;
			const directionId = state.editForm.influenceScope;

			// kmNumber: kmNumber, metNumber: metNumber, directionId: directionId for zone
			const zones = await tmsStore.getZonesByPosition({kmNumber: kmNumber, metNumber: metNumber, directionId: directionId, location: state.editForm.location});
			state.zoneList = zones.filter(zone => zone.hasChildInList == false);

			// Set zoneList to editForm object
			if(state.zoneList?.length > 0)
				state.editForm.zoneId = state.zoneList.map(zone => zone.id).join(',');
		}
		catch(error){
			console.error('editIncident.vue: getZonesByPosition(...)', error);
		}
		finally{
			setControlsToLoading(false);
		}
	}

	// Get location name by code
	const getLocationName = (locationCode: string) => {
		if(!locationCode) return '';
		// const locationList = getConfigDataByCode(BaseConfigTypeEnum.TmsZoneLocation);
		const location = state.locationList?.find((item: any) => item.code == locationCode);
		return location ? location.name : '';
	};

	// Get equipments based on the selected zone list (loaded by position)
	const getEquipmentsByPosition = async() => {
		try{
			setControlsToLoading(true);

			// Reset the equipmentIdList, equipmentList
			state.editForm.equipmentId = null;
			state.equipmentList = [];

			// Build the zoneId list from the zones loaded by position
			const zoneIds = state.zoneList?.map((zone: any) => zone.id).filter((id: any) => !!id) ?? [];

			// Return early if there is no zone selected
			if(zoneIds.length == 0)
				return;

			// Load equipment by the selected zone list via TmsZoneEquipment mapping
			const equipment = await tmsStore.getEquipmentByZones(zoneIds);

			// Keep only CCTV cameras
			const equipmentList = equipment.filter(equip => isCCTVCamera(equip.equipmentTypeId)) as any;

			// Set equipmentList to editForm object
			if(equipmentList?.length > 0){
				// Remove duplicates from list, ensuring each camera is unique
				state.equipmentList = equipmentList?.filter((equip: any, index: number, self: any) => index === self.findIndex((item: any) => item.id == equip.id));

				state.editForm.equipmentId = state.equipmentList?.map((item: any) => item.id).join(',');
			}
		}
		catch(error){
			console.error('editIncident.vue: getEquipmentsByPosition(...)', error);
		}
		finally{
			setControlsToLoading(false);
		}
	}

	// Open dialog function
	const setDefaultData = async(row: any = null) => {
		editFormRef.value?.resetFields();
		state.editForm = {} as UpdateTmsIncidentInput; // IMPORTANCE

		state.zoneList = [];
		state.equipmentList = [];

		row = JSON.parse(JSON.stringify(row)) ?? null;
		if(row){
			state.editForm = row;

			incidentKmNumber.value = row?.kmNumber ?? 0;
			incidentMetNumber.value = row?.metNumber ?? 0;

			await getZoneAndEquipByIdList();
		}
		else{
			state.editForm.startDate = Date.now();
			state.editForm.source = 'user';
			state.editForm.influenceScope = SpecialConfigEnum.AllDirection;

			incidentKmNumber.value = 0;
			incidentMetNumber.value = 0;
		}
	};

	// Handle on direction change
	const handleOnDirectionChange = async() => {
		generateCodeAndName();

		await getZonesByPosition(); // Must be use the keyword "await" here
		nextTick(async ()=>{
			await getEquipmentsByPosition(); // Must be use the keyword "await" here
		})
	};
	// Handle on location change
	const handleOnLocationChange = async() => {
		generateCodeAndName();

		await getZonesByPosition();

		nextTick(async ()=>{
			await getEquipmentsByPosition(); // Must be use the keyword "await" here
		})	};

	// Close dialog function
	const closeDialog = () => {
		state.isShowDialog = false; // IMPORTANCE
		state.isIncidentInfoChanged = false; // IMPORTANCE

		state.editForm.startDate = Date.now(); // IMPORTANCE
		state.editForm.zoneId = null; // IMPORTANCE
		state.editForm.equipmentId = null; // IMPORTANCE
		
		incidentKmNumber.value = 0; // IMPORTANCE
		incidentMetNumber.value = 0; // IMPORTANCE

		incidentMonitor.setIncidentDialogState(false);
	};

	// Handle on dialog closed
	const handleOnDialogClosed = () => {
		closeDialog();
	};

	// Handle on cancel
	const cancel = () => {
		state.isShowDialog = false;
	};

	// Handle on submit
	const submit = () => {
		editFormRef.value.validate(async (valid: boolean) => {
			if(!valid){
				// Enable the controls
				setControlsToLoading(false);

				return;
			}

			// Disable the controls
			state.isProcessing = true;

			try{
				const f = state.editForm;
				const payload: UpdateTmsIncidentInput | AddTmsIncidentInput = {
					code: f.code,
					name: f.name,
					eventTypeId: f.eventTypeId,
					startDate: commonVarsAndFuncs.getISODatetimeString(f.startDate) as unknown as Date,
					source: f.source,
					location: f.location,
					zoneId: f.zoneId,
					kmNumber: incidentKmNumber.value,
					metNumber: incidentMetNumber.value,
					influenceScope: f.influenceScope,
					injuredNumber: f.injuredNumber,
					vehicleNumber: f.vehicleNumber,
					trafficPlanId: f.trafficPlanId,
					equipmentId: f.equipmentId,
					processMode: f.processMode,
					description: f.description,
					remark: f.remark,
				};
				// state.editForm.kmNumber = incidentKmNumber.value;
				// state.editForm.metNumber = incidentMetNumber.value;
				
				let response = null;
				if (state.isEditMode)
					response = await getAPI(TmsIncidentApi).apiTmsTmsincidentUpdatePost({ ...payload, id: f.id });
				else {
					response = await getAPI(TmsIncidentApi).apiTmsTmsincidentAddPost(payload);
				}

				if (response?.data.result !== true) throw new Error(response?.data.message || $t('lz.message.base.processUnSuccess'));
				ElMessage.success($t('lz.message.base.processSuccess'));
				emits('onSubmitSuccess');
				closeDialog();
			}
			catch(error){
				console.error('editIncident.vue: submit(...)', error);
				ElMessage.error(error instanceof Error ? error.message : $t('lz.message.base.processUnSuccess'));
			}
			finally{
				// Enable the controls
				state.isProcessing = false;
			}

		});
	};

	// Check if the device is a camera
	const isCCTVCamera = (equipmentTypeId: string | null |undefined) => {
		const equipmentTypeCode = state.equipmentTypeList?.find((item:any) => item.id == equipmentTypeId)?.code;
		return equipmentTypeCode ? commonVarsAndFuncs.isCCTVCamera(equipmentTypeCode, 'CCTV') : false;
	}	

	onMounted(async() => {
		setDefaultData(); // No need to use the keyword "await" here
	});

	/* ---------------------------------------------------------------------------------------------- /*
	|	NOTE:
	|	- Khi SỬA incidient, thông tin Zones và Equipments được load theo zoneId và equipmentId
	|	- Khi SỬA incident, thông tin Zones và Equipments được load theo vị trí NẾU CÓ THAO TÁC
	|	- Khi THÊM incident, thông tin Equipments được load theo vị trí giữa của Zone NẾU CÓ ZoneId
	|	- Khi THÊM incident, thông tin Zones và Equipments được load theo vị trí nếu có thao tác
	/* ----------------------------------------------------------------------------------------------- */

	// Watch changes in the value of kmNumber input
	// IMPORTANCE: watchDebounced only works with the declaration "incidentKmNumber = ref(0)"
	// IMPORTANCE: watchDebounced does not work with state.kmNumber state.editForm.kmNumber
	watchDebounced(
		incidentKmNumber, 
		async() => {
			// Return when the dialog is closed
			if(!state.isShowDialog) // IMPORTANCE
				return;

			// Return when the incident info is not changed and the dialog is in edit mode
			if(!state.isIncidentInfoChanged && state.isEditMode)
				return;

			state.editForm.kmNumber = incidentKmNumber.value ?? 0; // IMPORTANCE
			state.editForm.metNumber = incidentMetNumber.value ?? 0; // IMPORTANCE

			generateCodeAndName();

			await getZonesByPosition(); // Must be use the keyword "await" here
			nextTick(async ()=>{
				await getEquipmentsByPosition(); // Must be use the keyword "await" here
			})		
		},
		{ debounce: (1 * 1000), maxWait: (20 * 1000) }
	);
	
	// Watch changes in the value of metNumber input
	// IMPORTANCE: watchDebounced only works with the declaration "incidentMetNumber = ref(0)"
	// IMPORTANCE: watchDebounced does not work with state.metNumber state.editForm.metNumber
	watchDebounced(
		incidentMetNumber,
		async() => {
			// Return when the dialog is closed
			if(!state.isShowDialog) // IMPORTANCE
				return;

			// Return when the incident info is not changed
			if(!state.isIncidentInfoChanged)
				return; // IMPORTANCE

			state.editForm.kmNumber = incidentKmNumber.value ?? 0; // IMPORTANCE
			state.editForm.metNumber = incidentMetNumber.value ?? 0; // IMPORTANCE

			generateCodeAndName();

			await getZonesByPosition(); // Must be use the keyword "await" here
			nextTick(async ()=>{
				await getEquipmentsByPosition(); // Must be use the keyword "await" here
			})


		},
		{ debounce: (1 * 1000), maxWait: (20 * 1000) }
	);

	// Watch global display requests
    watch(() => [incidentMonitor.incidentDialogState.isShowDialog], async() => {
        if(incidentMonitor.incidentDialogState.isShowDialog && !state.isShowDialog){
            state.isShowDialog = true;
			state.dialogTitle = incidentMonitor.incidentDialogState.dialogTitle;
			state.isEditMode = incidentMonitor.incidentDialogState.isEditMode;

			await loadDefaultData(); // Must be use the keyword "await" here

			setDefaultData(incidentMonitor.incidentDialogState.incidentInfo);
		}
        else if(!incidentMonitor.incidentDialogState.isShowDialog && state.isShowDialog){
            state.isShowDialog = false;
		}
    }, true);
</script>

<style lang="scss">
/* Ô chọn loại sự cố: icon tròn theo danh mục + tên (không scoped vì dropdown teleport ra body) */
.event-type-option {
	display: flex;
	align-items: center;
	gap: 8px;
	min-width: 0;
}

.event-type-option__text {
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.event-type-option__icon {
	flex: 0 0 auto;
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 28px;
	height: 28px;
	border-radius: 50%;
	background: color-mix(in srgb, var(--el-color-primary) 12%, #ffffff);

	img {
		width: 20px;
		height: 20px;
		object-fit: contain;
	}

	&--prefix {
		width: 22px;
		height: 22px;

		img {
			width: 16px;
			height: 16px;
		}
	}
}

.event-type-select-popper .el-select-dropdown__item.is-selected .event-type-option__icon {
	box-shadow: 0 0 0 1px color-mix(in srgb, var(--el-color-primary) 45%, transparent);
}
</style>
