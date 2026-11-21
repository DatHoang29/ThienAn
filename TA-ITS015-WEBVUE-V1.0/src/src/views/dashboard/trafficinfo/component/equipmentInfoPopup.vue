<template>
	<div class="simple-popup" v-if="state.isShowDialog">
		<div class="simple-popup--container pos-relative">
			<el-button type="danger" @click="state.isShowDialog = false" class="button-close pos-absolute top-right margin-5px" />
			<div class="panel-body">
				<el-tabs class="padding-10px">
					<el-tab-pane v-loading="state.isLoading" :element-loading-text="$t('Loading...')" :label="$t('Summary')">
						<div style="max-width:calc(100% - 10px); max-height: 300px; padding-right: 10px; overflow-y: auto;">
							<div v-if="state.editFeature.equipmentType.code === TmsEquipmentType.Vms">
								<table class="table-data">
									<tr style="border-bottom: thin solid 2px;">
										<td colspan="2">
											<h3 class="text-uppercase">{{ state.VMSData?.name }}</h3>
											<br>
										</td>
									</tr>
									<tr class="border-bottom--dashed">
										<th style="width: 130px;">{{ $t('IP_Address') }}:</th>
										<td>{{ state.VMSData?.ip }}</td>
									</tr>
									<tr class="border-bottom--dashed">
										<th style="width: 130px;">{{ $t('Port') }}:</th>
										<td>{{ state.VMSData?.port }}</td>
									</tr>
									<tr class="border-bottom--dashed">
										<th style="width: 130px;">{{ $t('Location') }}:</th>
										<td>Km ( {{ state.VMSData?.kmNumber }} +{{ state.VMSData?.metNumber }} ) </td>
									</tr>
									<tr class="border-bottom--dashed">
										<th style="width: 130px;">{{ $t('Value 1') }}:</th>
										<td>{{ state.VMSData?.value4 }}</td>
									</tr>
								</table>
								<el-card class="preview" style="margin-top: 10px;">
									<template #header>
									<h3 class="text-uppercase">Preview</h3>
									</template>
									<div class="el-card__body" style="text-align:center">
										
									<img
										v-if="selectedImageUrl"
										alt="Template Image"
										class="uploaded-image"
										style="width: 100%; height: auto; max-width: 350px; max-height: 250px; object-fit: cover;"
									/>
									</div>
								</el-card>
							</div>
							<div v-if="state.editFeature.equipmentType.code === TmsEquipmentType.FCam || 
								state.editFeature.equipmentType.code === TmsEquipmentType.TCam || 
								state.editFeature.equipmentType.code === TmsEquipmentType.PCam">
<el-alert v-if="state.statisticError" :title="state.statisticError" type="warning" :closable="false" />
<p v-if="state.TrafficStaticList?.lane">Snapshot VDS mới nhất — làn {{ state.TrafficStaticList.lane }}</p>

								<table class="table-data">
									<tr class="border-bottom--dashed">
										<th style="width: 130px;">{{ $t('avgSpeed') }}:</th>
										<td>{{ state.TrafficStaticList?.avgSpeed ?? '—' }}</td>
									</tr>
									<tr class="border-bottom--dashed">
										<th style="width: 130px;">{{ $t('Location') }}:</th>
										<td>Km({{ state.TrafficStaticList?.kmNumber }} + {{ state.TrafficStaticList?.metNumber }})</td>
									</tr>
									<tr class="border-bottom--dashed">
										<th style="width: 130px;">{{ $t('oVehicleNumber') }}:</th>
										<td>{{ state.TrafficStaticList?.oVehicleNumber ?? '—' }}  </td>
									</tr>
									<tr class="border-bottom--dashed">
										<th style="width: 130px;">{{ $t('sVehicleNumber') }}:</th>
										<td>{{ state.TrafficStaticList?.sVehicleNumber ?? '—' }}</td>
									</tr>
									<tr class="border-bottom--dashed">
										<th style="width: 130px;">{{ $t('mVehicleNumber') }}:</th>
										<td>{{ state.TrafficStaticList?.mVehicleNumber ?? '—' }}</td>
									</tr>
									<tr class="border-bottom--dashed">
										<th style="width: 130px;">{{ $t('lVehicleNumber') }}:</th>
										<td>{{ state.TrafficStaticList?.lVehicleNumber ?? '—' }}</td>
									</tr>
								</table>
							</div>
							<div v-else>
								<el-empty :image-size="100" />
							</div>
						</div>
					</el-tab-pane>
					<el-tab-pane v-if="state.editFeature.equipmentType.code === TmsEquipmentType.FCam || 
						state.editFeature.equipmentType.code === TmsEquipmentType.TCam || 
						state.editFeature.equipmentType.code === TmsEquipmentType.PCam" label="Camera">
							<el-card shadow="hover" class="card-item" style="height:320px">
								<el-empty :image-size="100" /> <!-- Adjust image size -->
							</el-card>
					</el-tab-pane>

					<el-tab-pane v-if="state.editFeature.equipmentType.code === TmsEquipmentType.Vms" label="VMS">
						<div style="max-width:calc(100% - 10px); max-height: 300px; padding-right: 10px; padding-bottom: 40px;" class="overflow-auto">
							<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="120px">
								<el-row :gutter="10">
									<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
										<el-form-item :label="$t('Action type')">
											<el-select v-model="activeVmsActionType" @change="onVmsActionlTypeChange" class="w100">
												<el-option v-for="item in state.vmsActionTypeList" :label="item.name" :value="item.code?.trim()" />
											</el-select>
										</el-form-item>
									</el-col>
								</el-row>
								<el-row :gutter="10">

									<!-- Template library  -->
									<el-col v-if="state.showVmsTextSelect" :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
										<el-form-item :label="$t('Message template')">
											<el-select v-model="activeVmsTextTemplate" @change="onVmsTextSelectChange" filterable class="w100">
												<el-option v-for="item in state.vmsTemplateAll" :label="item.name" :value="item.code?.trim()" />
											</el-select>
										</el-form-item>
									</el-col>
						
									<!-- orther -->
									<el-col v-if="state.showVmsRow1Input && !state.showVmsTextSelect"  :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
										<el-form-item :label="$t('entity.vmsMessage.content1')" :rules="[ {max: 256, message: $t('fe.base.validation.maxlength', {maxlength: 256}), trigger: 'blur'}]">
											<el-input v-model="state.ruleForm.row1" :placeholder="$t('entity.vmsMessage.content1')"
												clearable @input="updatePreview" />
										</el-form-item>
									</el-col>
									<el-col v-if="state.showVmsRow2Input && !state.showVmsTextSelect" :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
										<el-form-item :label="$t('entity.vmsMessage.content2')" :rules="[ {max: 256, message: $t('fe.base.validation.maxlength', {maxlength: 256}), trigger: 'blur'}]">
											<el-input v-model="state.ruleForm.row2" :placeholder="$t('entity.vmsMessage.content2')"
												clearable @input="updatePreview" />
										</el-form-item>
									</el-col>
									<el-col v-if="state.showVmsWordSelect1" :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
										<el-form-item :label="$t('entity.vmsMessage.content1')" :rules="[ {max: 256, message: $t('fe.base.validation.maxlength', {maxlength: 256}), trigger: 'blur'}]">
											<el-select v-model="state.ruleForm.wordList1" @change="onWordList1SelectChange" filterable clearable placeholder="Select" multiple>
												<el-option
													v-for="item in state.vmsWordTemplateAll"
													:label="item.content"
													:value="item.code"
												/>
											</el-select>
										</el-form-item>
									</el-col>
									<el-col v-if="state.showVmsWordSelect2" :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
										<el-form-item :label="$t('entity.vmsMessage.content2')" :rules="[ {max: 256, message: $t('fe.base.validation.maxlength', {maxlength: 256}), trigger: 'blur'}]">
											<el-select v-model="state.ruleForm.wordList2" @change="onWordList2SelectChange" filterable clearable placeholder="Select" multiple>
												<el-option
													v-for="item in state.vmsWordTemplateAll"
													:label="item.content"
													:value="item.code"
												/>
											</el-select>
										</el-form-item>
									</el-col>
								</el-row>
								<el-row :gutter="10" v-if="state.showVmsAdjustmentPanel">
									<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
										<el-form-item label="Font:">
										<el-select v-model="state.ruleForm.selectedFont" @change="updatePreview">
											<el-option value="Arial" label="Arial" />
											<el-option value="Tahoma" label="Tahoma" />
										</el-select>
										</el-form-item>
									</el-col>
									<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
										<el-form-item label="Text Color:">
										<el-select v-model="state.ruleForm.textColor" @change="updatePreview">
											<el-option v-for="color in state.ruleForm.textColors" :key="color" :value="color" :label="color" />
										</el-select>
										</el-form-item>
									</el-col>
									<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
										<el-form-item label="Background Color:">
										<el-select v-model="state.ruleForm.bgColor" @change="updatePreview">
											<el-option v-for="color in state.ruleForm.bgColors" :key="color" :value="color" :label="color" />
										</el-select>
										</el-form-item>
									</el-col>
									<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
										<el-form-item label="Brightness:">
										<el-slider v-model="state.ruleForm.brightness" :min="0" :max="100" :step="1" @input="updatePreview" />
										</el-form-item>
									</el-col>
									<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
											<el-form-item label="Priority:">
												<el-input-number 
													id="inputNumber"
													v-model="state.ruleForm.priority" 
													:min="1" 
													:max="999"
													controls-position="right"
													size="medium"
													style="width: 100px;"
												></el-input-number>										
											</el-form-item>
										</el-col>
									<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20" v-if="!state.showVmsTextSelect">
										<el-form-item label="Preview:">
										<div class="preview" :style="previewStyle">
											<div class="text-content" :style="textStyle">
											<div>{{ state.ruleForm.row1 }}</div>
											<div>{{ state.ruleForm.row2 }}</div>
											</div>
										</div>
										</el-form-item>
									</el-col>

									<!-- Template Library -->
									<el-col v-if="state.showVmsTextSelect">
										<el-card v-if="selectedTemplate" class="element-controls">
											<template #header>
												<h3>Element Controls</h3>
											</template>

											<el-tabs v-model="activeTab">
												<!-- Tab hiển thị Text Elements -->
												<el-tab-pane label="Text Elements" name="text">
												<div v-for="(element, index) in textElements" :key="element.id" class="element-control">
													<div class="element-header">
													<h3>{{ element.label }}</h3>
													</div>
													<div class="element-content">
														<el-input v-model="element.content" :placeholder="element.placeholder" />
													</div>
												</div>
												</el-tab-pane>

												<!-- Tab hiển thị Image Elements -->
												<el-tab-pane label="Image Elements" name="image">
												<div v-for="(element, index) in imageElements" :key="element.id" class="element-control">
													<div class="element-header">
													<h3>{{ element.label }}</h3>
													</div>
													<div class="element-content">
													<el-select v-model="element.content" placeholder="Select image" @change="updateElement">
														<el-option value="">
														<el-upload
															class="image-uploader"
															:auto-upload="false"
															:show-file-list="false"
															@change="(file) => uploadImagelibrary(element.id, file)"
														>
															<el-button>Upload Image</el-button>
														</el-upload>
														</el-option>

														<el-option
														v-for="item in state.vmsImgTemplateAll"
														:key="item.id"
														:label="item.name"
														:value="item.url"
														>
														<img :src="item.url" alt="Template Image" class="uploaded-image" />
														<span>{{ item.name }} | {{ item.type }} | {{ item.size }}px</span>
														</el-option>
													</el-select>
													</div>
													<div v-if="element.content" class="uploaded-image-preview">
													<img :src="element.content" class="uploaded-image" />
													<span>{{ getImageName(element) }}</span>
													</div>
												</div>
												</el-tab-pane>
											</el-tabs>
										</el-card>
									</el-col>
									<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20" v-if="state.showVmsTextSelect">
											<el-card v-if="selectedTemplate" class="preview">
												<template #header>
												<h3>Preview</h3>
												</template>
												<div class="el-card__body" style="text-align:center">
													<div class="preview-content" :style="containerStyle" ref="previewContainer">
														<div
															v-for="element in selectedTemplate.elements"
															:key="element.id"
															:style="getElementStyle(element, true)"
														>
															<template v-if="element.type === 'text'">
															{{ element.content }}
															</template>
															<template v-else-if="element.type === 'image'">
																	<img v-if="element.content" :src="element.content" alt="Selected image" style="width: 100%; height: 100%; object-fit: cover;" />
															</template>
														</div>
													</div>
												</div>
										</el-card>
									</el-col>
								</el-row>
								<el-row :gutter="10" v-if="state.showVmsImgTemplatePanel">
									<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item label="Image:">
									<div style="display: flex; flex-direction: column; gap: 10px;" class="w100">
									<el-select 
										v-model="state.ruleForm.imgTemplate" 
										popper-class="simple-img_select" 
										placeholder="Select" 
										filterable 
										clearable 
										class="w100"
										@change="updateSelectedImageUrl"
									>
										<el-option value="">
										<el-upload
											class="image-uploader"
											:auto-upload="false"
											:show-file-list="false"
											:on-change="handleImageUpload"
											:before-upload="beforeUpload"
										>
											<el-button>Upload Image</el-button>
										</el-upload>
										</el-option>
										<el-option
										v-for="item in state.vmsImgTemplateAll"
										:key="item.code"
										:label="item.name"
										:value="item.code"
										>
											<img
												:src="item.url"
												alt="Template Image"
												class="uploaded-image"
												style="width: 100%; height: auto; max-width: 350px; max-height: 250px; object-fit: cover;"
											/>
											<span>{{ item.name }} | {{ item.type }} | {{ item.size }}px</span>
										</el-option>
									</el-select>
									</div>
								</el-form-item>
								</el-col>
								<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
									<el-form-item label="Brightness:">
									<el-slider v-model="state.ruleForm.brightness" :min="0" :max="100" :step="1" @input="updatePreview" />
									</el-form-item>
								</el-col>
								<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
										<el-form-item label="Priority:">
											<el-input-number 
												id="inputNumber"
												v-model="state.ruleForm.priority" 
												:min="1" 
												:max="999"
												controls-position="right"
												size="medium"
												style="width: 100px;"
											></el-input-number>										
										</el-form-item>
								</el-col>	
								<!-- Preview section -->
								<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20" >
									<el-card class="preview">
										<template #header>
										<h3>Preview</h3>
										</template>
										<div class="el-card__body" style="text-align:center">
										<img
											v-if="selectedImageUrl"
											:src="selectedImageUrl"
											alt="Template Image"
											class="uploaded-image"
											style="width: 100%; height: auto; max-width: 350px; max-height: 250px; object-fit: cover;"
										/>
										</div>
									</el-card>
								</el-col>
									
								</el-row>
								
								<!-- Timeschedule -->
								<el-row v-if="state.showVmsSchedulingPanel">
									<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
										<el-form-item label="Image:">
										<div style="display: flex; flex-direction: column; gap: 10px;" class="w100">
											<el-select 
											v-model="state.ruleForm.imgTemplate" 
											popper-class="simple-img_select" 
											placeholder="Select" 
											filterable 
											clearable 
											class="w100"
											@change="handleImgTemplateChange"
											>
											<el-option
												v-for="item in state.vmsTemplateAll"
												:key="item.id"
												:label="item.name"
												:value="item.code"
											>
											<img
													:src="item.preview"
													alt="Template Image"
													class="uploaded-image"
													style="width: 50%; height: 100%; "
												/>
												<span>{{ item.name }} </span>
											</el-option>
											
											</el-select>
										</div>
										</el-form-item>
									</el-col>
									<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
										<el-form-item label="Brightness:">
										<el-slider v-model="state.ruleForm.brightness" :min="0" :max="100" :step="1" @input="updatePreview" />
										</el-form-item>
									</el-col>
									<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
										<el-form-item label="Priority:">
											<el-input-number 
												id="inputNumber"
												v-model="state.ruleForm.priority" 
												:min="1" 
												:max="999"
												controls-position="right"
												size="medium"
												style="width: 100px;"
											></el-input-number>										
										</el-form-item>
									</el-col>
									<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
										<el-card class="preview">
										<template #header>
											<h3>Preview</h3>
										</template>
											<div class="el-card__body" style="text-align:center">
												<draggable tag="ul" :list="draggableImages" @end="onDragEnd">
												<template #item="{ element, index }">
													<li class="list-group-item">
													<div class="d-flex align-items-center justify-content-between">
														<div class="flex-fill d-flex align-items-center justify-content-center">
														<img :src="element.preview" alt="Selected image" style="width: 60%; height: auto; object-fit: cover;" />
														<i 
															class="fa fa-times close" 
															@click="removeAt(index)" 
															style="cursor: pointer; color: #dc3545; font-size: 1.5em;" 
															title="Remove"
														></i>
														<el-input-number 
														id="inputNumber"
														v-model="element.timeToLive" 
														:min="5" 
														:max="999"
														controls-position="right"
														size="medium"
														style="width: 100px;"
														></el-input-number>
														</div>
													</div>
													</li>
												</template>

												</draggable>
												<rawDisplayer class="col-3" :value="listprocessFile" title="listprocessFile" />
											</div>
										</el-card>
									</el-col>
								</el-row>
								<!-- Timer Controls -->
								<el-row v-if="state.showVmsTimerPanel" >
								<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
									<el-form-item label="Image:">
										<div style="display: flex; flex-direction: column; gap: 10px;" class="w100">
										
										<el-select 
										v-model="state.ruleForm.imgTemplate" 
										popper-class="simple-img_select" 
										placeholder="Select" 
										filterable 
										clearable 
										class="w100"
										@change="handleTimerTemplateChange"
										>
										<el-option
											v-for="item in state.vmsTemplateAll"
											:key="item.id"
											:label="item.name"
											:value="item.code"
										>
										<img
												:src="item.preview"
												alt="Template Image"
												class="uploaded-image"
												style="width: 50%; height: 100%; "
											/>
											<span>{{ item.name }} </span>
										</el-option>
										
										</el-select>
										</div>
									</el-form-item>
									</el-col>
								<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
									
										<div style="display: flex; gap: 10px;">
										<el-form-item label="Start Time:">
											<el-time-picker
											v-model="state.ruleForm.startTime"
											placeholder="Select Start Time"
											format="HH:mm"
											style="width: 100%;"
											/>
										</el-form-item>
										<el-form-item label="End Time:">
											<el-time-picker
											v-model="state.ruleForm.endTime"
											placeholder="Select End Time"
											format="HH:mm"
											style="width: 100%; margin-top:-15px"
											/>
										</el-form-item>
										</div>
									
									</el-col>  
									<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
										<el-form-item label="Brightness:">
										<el-slider v-model="state.ruleForm.brightness" :min="0" :max="100" :step="1" @input="updatePreview" />
										</el-form-item>
									</el-col>
									<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
										<el-form-item label="Priority:">
											<el-input-number 
												id="inputNumber"
												v-model="state.ruleForm.priority" 
												:min="1" 
												:max="999"
												controls-position="right"
												size="medium"
												style="width: 100px;"
											></el-input-number>										
										</el-form-item>
									</el-col>
									<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
									<el-card class="preview">
										<template #header>
										<h3>Preview</h3>
										</template>
										<!-- Combine the uploaded image and template image in preview -->
										<div class="el-card__body" style="text-align:center">
										<img
										v-if="selectedImageUrl"
										:src="selectedImageUrl"
										alt="Template Image"
										class="uploaded-image"
										style="width: 100%; height: auto; max-width: 350px; max-height: 250px; object-fit: cover;"
										/>
										</div>
										
									</el-card>
									</el-col>
									
								</el-row>
								<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
									<div >
										<el-card>
											<div>
												<el-checkbox-group v-model="state.listVmsSelected">
													<el-checkbox
														border
														class="w-100 m-0 mb-2"
														v-for="vmsItem in state.equipmentList.filter(item => item.equipmentState === 1)"
														:key="vmsItem.id"
														:label="vmsItem.id"
													>
														{{ `${vmsItem.name}(Km${vmsItem.kmNumber}+${vmsItem.metNumber})` }}
        												<template v-if="isRunning(vmsItem.id)">
															<el-icon color="red" class="ml-2">
																<Loading />
															</el-icon>
														</template>
														
													</el-checkbox>
												</el-checkbox-group>
											</div>
										</el-card>
									</div>
								</el-col>
								<span class="dialog-footer" style="float:right;">
									<el-button @click="cancel">Cancel</el-button>
									<el-button v-if="state.showVmsSchedulingPanel" type="primary" @click="submitrun">Run</el-button>
									<el-button v-if="state.showVmsSchedulingPanel" type="primary" @click="submitstop">Stop</el-button>
									<el-button v-else type="primary" @click="submit">{{ $t('fe.base.button.confirm') }}</el-button>
								</span>
							</el-form>
							
						</div>
					</el-tab-pane>
				</el-tabs>
			</div>
		</div>
	</div>
</template>
<script lang="ts" setup>
import { ref, computed, onMounted, onUnmounted, watch, reactive, nextTick  } from 'vue';
import { VdsTrafficStatisticApi } from '/@/api-services/vds';
import { getAPI } from '/@/utils/axios-utils';
import { ElMessage, ElMessageBox } from 'element-plus';
import { OperateType } from '/@/types/constants';
import draggable from 'vuedraggable';
import TransporterSocketCluster from '/@/transporter/transporterSocket';
import { Loading } from '@element-plus/icons-vue';

import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';

const getConfigDataByCode = useBaseConfig().getConfigDataByCode;

// Import the necessary apis
import { SysConfigDataApi } from '/@/api-services/cfgSystem';
import { TmsModulesApi,TmsEquipmentApi,TmsEquipmentTypeApi,TmsSignalLogApi,UpdateTmsSignalLogInput } from '/@/api-services/tms';
import { VmsImageApi, VmsMessageApi,VmsTemplateApi,VmsProcessApi,VmsCurrentApi,VmsProcessImgApi } from '/@/api-services/vms';
import { UpdateVmsProcessInput,UpdateVmsCurrentInput } from '/@/api-services/vms';
import { VmsActionType, VmsMessageType, VmsCurrentType } from '/@/types/enums/vmsEnum';
import { TmsEquipmentType,TmsMapLocationRefType } from '/@/types/enums/tmsEnum';
import { SysConfigType, StatusType } from '/@/types/enums/baseEnum';	
import html2canvas from 'html2canvas';
import { DateTimeFormatResult, useI18n } from 'vue-i18n';


	// Declare the reactive state
const { locale } = useI18n();
const designContainer = ref(null);
const previewContainer = ref(null);
const hoveredElement = ref(null);
const activeMode = ref(null);
const draggedElement = ref(null);
const startX = ref(0);
const startY = ref(0);
const activeTab = ref('text');

interface RuleForm {
  row1: string;
  row2: string;
  row3: string;
  width: string;
  height: string;
  selectedFont: string;
  textColor: string;
  bgColor: string;
  brightness: number;
  title: string;
  textColors: string[];
  bgColors: string[];
  dialogWidth: string;
  previewWidth: string;
  previewHeight: string;
  startTime: string;  
  endTime: string;
  TimeToLive: string;
}

interface State {
  isShowDialog: boolean;
  showWarning: boolean;
  ruleForm: RuleForm;
}

const emits = defineEmits(['handleQuery']);
const ruleFormRef = ref();
const selectedTemplate = ref();
const state = reactive<State>({
    isShowDialog: false,
	isRunningProcess: false,
    showWarning: false,
    isLoading: false,
    editProcessForm: {} as UpdateVmsProcessInput ,
    editCurrentForm: {} as UpdateVmsCurrentInput,
	SignalLogForm: {} as UpdateTmsSignalLogInput,
    showVmsTextSelect: false,
    showVmsRow1Input: true,
    showVmsRow2Input: true,
	showVmsRow3Input: true,
    showVmsWordSelect1: false,
    showVmsWordSelect2: false,
    showVmsAdjustmentPanel: true,
    showVmsImgTemplatePanel: false,
    showVmsSchedulingPanel: false,
    showVmsTimerPanel: false,
	equipmentTypeList:[],
    vmsActionTypeList: [],
    vmsMsgTemplateAll: [],
    vmsTextTemplateAll: [],
    vmsWordTemplateAll: [],
    vmsImgTemplateAll: [],
    vmsTemplateAll: [],
	statisticError: '',
	TrafficStaticList: {} as any,
    equipmentList:[],
    listVmsSelected:[],
    statusList: [],
	showMe: false,
	VMSData: [],
	editFeature: {
		type: '',
		equipmentType:'',
		image: '',
		status: '',
		id: '',
		strokeColor: '#000000',
		strokeWidth: 2,
		strokeOpacity: 1,
		fillColor: '#ff0000',
		fillOpacity: 1,
		brightness: '',
		temperature: '',
		scheduledTime: ''
	},
    ruleForm: {
        wordList1: [],
        wordList2: [],
        imgTemplate: '',

        row1: '',
        row2: '',
		row3: '',
        selectedFont: 'Arial',
        textColor: 'Black',
        bgColor: 'Green',
        brightness: 100,
        title: 'VMS Control',
        textColors: ['Black', 'Red', 'Green', 'Yellow'],
        bgColors: ['White', 'Blue', 'Green', 'Yellow'],
        dialogWidth: '700px',
        previewWidth: '100%',
        previewHeight: '100px',
        startTime: '',
        endTime: '',
		priority: 0,
    },
});
	// Create your necessary methods
	

	const previewStyle = computed(() => ({
		fontFamily: state.ruleForm.selectedFont,
		backgroundColor: state.ruleForm.bgColor,
		textAlign: 'center',
		display: 'flex',
		flexDirection: 'column',
		justifyContent: 'center',
		padding: '10px',
		width: state.ruleForm.previewWidth,
		height: state.ruleForm.previewHeight,
	}));

	const textStyle = computed(() => ({
		color: state.ruleForm.textColor,
		filter: `brightness(${state.ruleForm.brightness *2}%)`,
	}));

	const updatePreview = () => {
		state.showWarning = state.ruleForm.textColor === state.ruleForm.bgColor;
	};
	// check thread 
	const isRunning = (id) => {
      // Ensure that isRunningProcess is an array and perform the check
      	return Array.isArray(state.isRunningProcess) && 
            state.isRunningProcess.some(process => process.id === id && process.status);
    };
	

	const toggleMyPopup = async (isShow: Boolean, data: any, row?: Partial<RuleForm>) => {
    // Hiển thị hoặc ẩn popup

		if (row) {
			state.ruleForm = { ...state.ruleForm, ...row };
		}
		
		// ===============VMS================
		draggableImages.value = [];
		state.isShowDialog = true;
		ruleFormRef.value?.resetFields();
		state.editFeature.type = data.type;
		state.editFeature.id = data.pointId;
		const popupRequestId = ++statisticRequestId;
		state.TrafficStaticList = {}; state.statisticError = '';

		if(data.type == TmsMapLocationRefType.Equipment){
			const Equipmentlist= await getAPI(TmsEquipmentApi).apiTmsTmsequipmentListGet();
			if (popupRequestId !== statisticRequestId || !state.isShowDialog) return;
			const Equipment = Equipmentlist.data.result.find(x=>x.id == data.pointId);
			state.editFeature.equipmentType = state.equipmentTypeList.find(x=>x.id == Equipment?.equipmentTypeId);
		}

		if ([TmsEquipmentType.FCam, TmsEquipmentType.TCam, TmsEquipmentType.PCam].includes(state.editFeature.equipmentType?.code)) {
			await loadVdsStatistic(data.pointId);
			return;
		}

		const equipmentIds = state.equipmentList.map(item => item.id);
		const run = await getAPI(VmsProcessApi).apiVmsVmsprocessCheckrunningPost(equipmentIds);
		state.isRunningProcess = run.data.result;
		activeVmsActionType.value = VmsActionType.TextManual;
		if (data.pointId) {
			// Lấy danh sách ID đã chọn
			state.listVmsSelected = state.equipmentList
				.filter(vmsItem => vmsItem.id === data.pointId)
				.map(vmsItem => vmsItem.id);
			
		}
		if(state.editFeature.equipmentType.code === TmsEquipmentType.Vms){
			const listVmsCurrent = await getAPI(VmsCurrentApi).apiVmsVmscurrentListGet();
			const VmsCurrent = listVmsCurrent.data.result;
			// Lấy dữ liệu hình ảnh từ API
			const response = await getAPI(VmsProcessApi).apiVmsVmsprocessListGet();
			const equipment = response.data.result;

			const process = await getAPI(VmsProcessImgApi).apiVmsVmsprocessimgListGet();
			const processImg = process.data.result;

			// Kiểm tra xem ID đã chọn có trong danh sách hình ảnh không
			const existingVms = equipment.filter(location => state.listVmsSelected.includes(location.vmsId));
			if (existingVms.length > 0) {
				// Lọc tất cả hình ảnh tương ứng với danh sách ID đã chọn
				const selectedImages = processImg.filter(img => existingVms.some(vms => vms.id === img.processId));
				// Đưa tất cả hình ảnh vào danh sách draggableImages
				selectedImages.forEach(selectedItem => {
					draggableImages.value.push({
						id: selectedItem.id,
						preview: selectedItem.url, // Giả định preview chứa URL của hình ảnh
						timeToLive: selectedItem.timeToLive // Giả định timeToLive có trong selectedItem
					});
				});

				// Lấy rowData từ existingVms
				const rowData = existingVms.map(existingVmsItem => {
				// Find the corresponding location in VmsCurrent
				const location = VmsCurrent.find(location => location.equipmentId === existingVmsItem.vmsId);
				state.VMSData = state.equipmentList.find(u=>u.id === existingVmsItem.vmsId);
				// Check if location and rowData exist, and parse rowData as JSON
				if (location && location.rowData) {
					const parsedRowData = JSON.parse(location.rowData); // Parse the rowData
					return {
						row1: parsedRowData.row1, // Assign the value for row1
						row2: parsedRowData.row2,
						row3: parsedRowData.row3 // Assign the value for row2
					};
				}
				return null; // Return null if no valid data
			}).filter(data => data !== null); // Filter out invalid values
			const activeCtrlType = activeVmsActionType?.value;
			// Check and assign values for row1 and row2
			if (rowData.length > 0 && (activeCtrlType == VmsActionType.TextManual 
			|| activeCtrlType == VmsActionType.TextTemplate
			||activeCtrlType == VmsActionType.WordTemplate)) {
				const parsedRowData = rowData[0]; 
				state.ruleForm.row1 = parsedRowData.row1 || ""; 
				state.ruleForm.row2 = parsedRowData.row2 || ""; 
				state.ruleForm.row3 = parsedRowData.row3 || "";
				state.showVmsRow1Input = true; 
				state.showVmsRow2Input = true;
				state.showVmsRow3Input = true; 
			} else {
				state.showVmsRow1Input = true; 
				state.showVmsRow2Input = true;
				state.showVmsRow3Input = true; 
			}

			}
		}else if(state.editFeature.equipmentType.code === (TmsEquipmentType.FCam ||TmsEquipmentType.TCam ||TmsEquipmentType.PCam ))
		{
		//=============CCTV=================

		} 
		
		
	};


 let statisticRequestId = 0;
 const loadVdsStatistic = async (equipmentId: string) => {
  const current = ++statisticRequestId;
  state.TrafficStaticList = {}; state.statisticError = '';
  if (!equipmentId) return;
  try {
   const toDate = new Date();
   const response = await getAPI(VdsTrafficStatisticApi).apiVdsVdstrafficstatisticSnapshotPost({
    equipmentIds: [equipmentId], fromDate: new Date(toDate.getTime() - 86400000), toDate,
   });
   if (current !== statisticRequestId || state.editFeature.id !== equipmentId || !state.isShowDialog) return;
   // Latest lane snapshot only; never add overlapping windows or average lane speeds unweighted.
   const rows = (response.data.result?.rows ?? []).filter(row => row.data?.equipmentId === equipmentId && !row.stale);
   rows.sort((a, b) => String(b.data?.fromTime ?? '').localeCompare(String(a.data?.fromTime ?? '')));
   const data = rows[0]?.data;
   state.TrafficStaticList = data ? { ...data, oVehicleNumber: data.otherVehicle,
    sVehicleNumber: data.shortVehicle, mVehicleNumber: data.mediumVehicle, lVehicleNumber: data.longVehicle } : {};
   if (!data) state.statisticError = 'Chưa có thống kê VDS còn hiệu lực trong 24 giờ qua';
  } catch (error) { if (current === statisticRequestId) state.statisticError = 'Không tải được thống kê VDS'; console.error('[equipmentInfoPopup] VDS snapshot failed', error); }
 };
	const closeDialog = () => {
		emits('handleQuery');
		state.isShowDialog = false;
		statisticRequestId++;
		
	};

	const cancel = () => {
		closeDialog();
	};

	const submit = () => {
		ruleFormRef.value.validate(async (valid: boolean) => {
			if (!valid) return;

			await nextTick();

			const previewElement = previewContainer.value;

			if (previewElement) {
			await exportPreviewToPNG(previewElement);
			} else {
			console.error('Không tìm thấy ref "previewContainer"');
			}

			processOnSubmit();
			ElMessage.success('Submitted successfully');
			closeDialog();
		});
	};
	
		// Bind your data to the DOM when the DOM has been mounted
	onMounted(async() => {
		// Load the data for vms-message: sentence-manual sentence-template, word-template

		loadVmsMessageData();
		//load equipment:
		loadEquipmentData();
		// Load the data for img-template
		loadImgTemplateData();
	});
// ========= VMS TEXT MANUAL, TEXT TEMPLATE, WORD TEMPLATE =========
		const activeVmsActionType = ref(null);
		const activeVmsTextTemplate = ref(null);
		const selectedImageUrl = ref<string | null>(null);

	// Load your data as soon as the instance of vue has been created
	const loadVmsMessageData = async() => {
		state.statusList = getConfigDataByCode(BaseConfigTypeEnum.SysStatusType);
		state.vmsActionTypeList = getConfigDataByCode(BaseConfigTypeEnum.VmsActionType);

		let response = await getAPI(VmsMessageApi).apiVmsVmsmessageListPost({lang: locale.value, sortKey: 'orderNo', sortOrder: 'desc'});
		state.vmsMsgTemplateAll = response.data.result;

	};

	const loadEquipmentData = async () => {
		try {
			// Gọi API để lấy danh sách thiết bị
			let response = await getAPI(TmsEquipmentApi).apiTmsTmsequipmentGetequipmenttypebyequipmentidVmstypeGet(TmsEquipmentType.Vms);
			state.equipmentList = response?.data?.result ?? null;

			// Gọi API để lấy danh sách loại thiết bị
			response = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet();
			state.equipmentTypeList = response?.data?.result ?? null;
		} catch (error) {
			// Nếu có lỗi xảy ra trong quá trình gọi API, gán giá trị mặc định
			console.error("Lỗi khi tải dữ liệu:", error);
			state.equipmentList = null;
			state.equipmentTypeList = null;
		}
	};

// Reset the controls to default data
	const resetControlToDefault = (ctrlType: string) => {
		state.ruleForm.row1 = '';
		state.ruleForm.row2 = '';
		state.ruleForm.row3 = '';
		state.ruleForm.wordList1 = [];
		state.ruleForm.wordList2 = [];
		state.ruleForm.imgTemplate = '';

		state.vmsWordTemplateAll = [];
		state.vmsTextTemplateAll = [];
	};


	const toggleControlDisplay = (ctrlType: string) => {
		// ---------
		state.showVmsAdjustmentPanel = (ctrlType ==  VmsActionType.TextManual || ctrlType == VmsActionType.TextTemplate || ctrlType == VmsActionType.WordTemplate);

		state.showVmsRow1Input = (ctrlType ==  VmsActionType.TextManual || ctrlType == VmsActionType.TextTemplate);
		state.showVmsRow2Input = (ctrlType == VmsActionType.TextManual || ctrlType == VmsActionType.TextTemplate);

		state.showVmsTextSelect = ctrlType == VmsActionType.TextTemplate;

		state.showVmsWordSelect1 = ctrlType == VmsActionType.WordTemplate;
		state.showVmsWordSelect2 = ctrlType == VmsActionType.WordTemplate;

		// ---------
		state.showVmsImgTemplatePanel = ctrlType == VmsActionType.ImageTemplate;    
		state.showVmsSchedulingPanel = ctrlType == VmsActionType.Scheduling;
		state.showVmsTimerPanel = ctrlType == VmsActionType.Timer;
	}
	//=======================VMS TEMPLATE LIBRARY =================

	const onVmsTextSelectChange = async (value) => {
		const template = state.vmsTemplateAll.find(x => x.code === value);
		if (!template) {
			console.error("Không tìm thấy template với mã:", value);
			return;
		}
		const response = await getAPI(VmsTemplateApi).apiVmsVmstemplateDatalistIdGet(template.id);
		if (response && response.data && Array.isArray(response.data.result)) {
			try {

			const importedDesign = response.data.result.find(item => item.code === value);
			const designData = JSON.parse(importedDesign.jsonData);
			const [width, height] = importedDesign.size.split('x').map(Number);

			state.ruleForm.width = width;
			state.ruleForm.height = height;
			selectedTemplate.value = designData;
			
			} catch (error) {
			console.error("Lỗi khi parse JSON:", error);
			}
		}
	};

	const textElements = computed(() => selectedTemplate.value?.elements.filter(e => e.type === 'text') || []);
	const imageElements = computed(() => selectedTemplate.value?.elements.filter(e => e.type === 'image') || []);

	watch(textElements, (newElements) => {
		newElements.forEach(element => {
			if (!state.ruleForm[element.id]) {
			state.ruleForm[element.id] = element.content || '';
			}
		});
	});

	const getImageName = (element) => {
      return element.fileName || 'Uploaded image';
    };
	const uploadImagelibrary = (elementId, file) => {
		const reader = new FileReader();
		reader.onload = (e) => {
		const element = selectedTemplate.value.elements.find(el => el.id === elementId);
		if (element) {
			element.content = e.target.result;
			element.fileName = file.name;
			updateElement();
		}
		};
		reader.readAsDataURL(file.raw);
	};

	const updateElement = () => {
		if (selectedTemplate.value) {
		selectedTemplate.value = { ...selectedTemplate.value }; // Trigger reactivity
		}
	};
	const getElementStyle = (element, isPreview = false) => {
      const style = {
        position: 'absolute',
        left: `${element.x}px`,
        top: `${element.y}px`,
        width: `${element.width}px`,
        height: `${element.height}px`,
      };
      if (!isPreview) {
        style.border = '1px solid #ccc';
      }
      return style;
    };

const getElementLabel = (element) => {
      return element.id;
    };
     const startDrag = (event, elementId) => {
      event.preventDefault();
      activeMode.value = 'drag';
      draggedElement.value = elementId;
      startX.value = event.clientX;
      startY.value = event.clientY;
      document.addEventListener('mousemove', onDrag);
      document.addEventListener('mouseup', stopDrag);
    };
     const onDrag = (event) => {
      if (draggedElement.value && selectedTemplate.value) {
        const element = selectedTemplate.value.elements.find(e => e.id === draggedElement.value);
        if (element) {
          const dx = event.clientX - startX.value;
          const dy = event.clientY - startY.value;
          element.x = Math.max(0, Math.min(element.x + dx, state.ruleForm.width - element.width));
          element.y = Math.max(0, Math.min(element.y + dy, state.ruleForm.height - element.height));
          startX.value = event.clientX;
          startY.value = event.clientY;
          updateElement();
        }
      }
    };

    const stopDrag = () => {
      activeMode.value = null;
      draggedElement.value = null;
      document.removeEventListener('mousemove', onDrag);
      document.removeEventListener('mouseup', stopDrag);
    };

    const startResize = (event, elementId) => {
      event.preventDefault();
      activeMode.value = 'resize';
      draggedElement.value = elementId;
      startX.value = event.clientX;
      startY.value = event.clientY;
      document.addEventListener('mousemove', onResize);
      document.addEventListener('mouseup', stopResize);
    };
	const containerStyle = computed(() => {
		if (!selectedTemplate.value || !state.ruleForm.width || !state.ruleForm.height) return {};
		return {
		color: state.ruleForm.textColor,
		fontFamily: state.ruleForm.selectedFont,
		backgroundColor: state.ruleForm.bgColor,
		width: `${state.ruleForm.width}px`,
		height: `${state.ruleForm.height}px`,
		position: 'relative',
		};
	});

	const exportPreviewToPNG = (previewElement) => {
		return new Promise((resolve, reject) => {
		if (previewElement) {
			html2canvas(previewElement)
			.then(canvas => {
				const url = canvas.toDataURL('image/png');
				state.editCurrentForm.url = url;
				resolve(); // Resolve the promise when done
			})
			.catch(error => {
				reject(error); // Reject the promise if an error occurs
			});
		} else {
			reject(new Error('Preview container is null'));
		}
		});
	};

	const addTextElement = () => {
		if (selectedTemplate.value) {
		const newIndex = textElements.value.length + 1;
		const newElement = {
			id: `row${newIndex}`,
			type: 'text',
			content: '',
			placeholder: 'Enter text',
			x: 10,
			y: 10,
			width: 200,
			height: 30,
			label: `Row ${newIndex}`,
		};
		selectedTemplate.value.elements.push(newElement);
		}
	};

	const addImageElement = () => {
		if (selectedTemplate.value && selectedSize.value) {
		const newIndex = imageElements.value.length + 1;
		const size = Math.floor(selectedSize.value.width / 4);
		const newElement = {
			id: `image${newIndex}`,
			type: 'image',
			content: '',
			x: 10,
			y: 10,
			width: size,
			height: size,
			label: `Image ${newIndex}`,
		};
		selectedTemplate.value.elements.push(newElement);
		}
	};

	const removeElement = (elementId) => {
		if (selectedTemplate.value) {
		selectedTemplate.value.elements = selectedTemplate.value.elements.filter(e => e.id !== elementId);
		}
	};


	//============ VMS WORD LISTS =============================
	const onWordList2SelectChange  = (value: Array) => {
		if((value?.length ?? 0) == 0)
		return;

		state.ruleForm.row2 = state.vmsMsgTemplateAll?.filter(item => value.includes(item.code))?.map(item => item.name?.trim().toUpperCase())?.join(' ');
	}

	const onWordList1SelectChange  = (value: Array) => {
		if((value?.length ?? 0) == 0)
		return;

		state.ruleForm.row1 = state.vmsMsgTemplateAll?.filter(item => value.includes(item.code))?.map(item => item.name?.trim().toUpperCase())?.join(' ');
	}

	// Handle your data when the select is changed
	const onVmsActionlTypeChange = (ctrlType) => {
		toggleControlDisplay(ctrlType);
		resetControlToDefault(ctrlType);
		bindVmsDataToSelect(ctrlType);
		selectedImageUrl.value = null; // Gọi hàm Uploadnull

	};

	// Bind the data to the select control
	const bindVmsDataToSelect = (ctrlType: string) => {
		try{
			state.isLoading = true;
			if(ctrlType == VmsActionType.WordTemplate){
				state.vmsWordTemplateAll = state.vmsMsgTemplateAll?.filter(item => item.type?.toLowerCase() == VmsMessageType.Word)?.map(item => ({ code: item.code, content: item.content1 }));
			}
			else if(ctrlType == VmsActionType.TextTemplate){
				state.vmsTextTemplateAll = state.vmsMsgTemplateAll?.filter(item => item.type.toLowerCase() == VmsMessageType.Sentence); // => No: map(item => ({ code: item.code, content: item.content1 }))
			}
			else if(ctrlType == VmsActionType.ImageTemplate){

			}
		}
		catch(error){
			console.log('Error: ', error);
		}
		finally{
			state.isLoading = false;
		}
	};


	// Handle the data upon submission
	const processOnSubmit = async () => {

		const activeCtrlType = activeVmsActionType?.value;
		const selectedCodeNameWithEquipment = state.listVmsSelected.map((selectedId) => {
		const equipment = state.equipmentList.find(equipment => equipment.id === selectedId);

			if (equipment) {
				return {
					Code: equipment.code,   
					Name: equipment.name,   
					EquipmentId: equipment.id     
				};
			}
			return null; // Return null if no matching equipment is found
		}).filter(item => item !== null); // Filter out any null values

		state.editCurrentForm.listEquipment = selectedCodeNameWithEquipment;
		state.editProcessForm.Brightness = state.ruleForm.brightness;
		state.editProcessForm.vmsIds = state.listVmsSelected; 
		state.editCurrentForm.color = state.ruleForm.textColor;
		state.editCurrentForm.backColor = state.ruleForm.bgColor; // Ensure Type is set

		if (activeCtrlType == VmsActionType.TextManual ) {
			let row1 = state.ruleForm.row1?.trim();
			let row2 = state.ruleForm.row2?.trim();
			// Create RowData as JSON
			state.editCurrentForm.RowData = JSON.stringify({ row1, row2 });

			try {

				const response = await getAPI(VmsCurrentApi).apiVmsVmscurrentListGet();
				const mapLocations = response.data.result;
				const existingVms = mapLocations.find(location => state.listVmsSelected.includes(location.equipmentId));
				
				if (existingVms != null) {

					state.editCurrentForm.id = existingVms.id;
					state.editCurrentForm.oldRowData = existingVms.rowData;
					state.editCurrentForm.oldUrl = existingVms.url;
					state.editCurrentForm.listEquipment = selectedCodeNameWithEquipment;
					state.editCurrentForm.color = state.ruleForm.textColor;
					state.editCurrentForm.backColor = state.ruleForm.bgColor;

					if (state.ruleForm.priority > existingVms.priority) {

						try {
							// Hiển thị modal xác nhận
							await ElMessageBox.confirm(
								"Độ ưu tiên hiện tại thấp hơn độ ưu tiên đã tồn tại. Bạn có muốn tiếp tục cập nhật không?",
								"Xác nhận",
								{
									confirmButtonText: "Đồng ý",
									cancelButtonText: "Hủy",
									type: "warning",
								}
							);

							// Người dùng chọn 'Đồng ý', tiến hành cập nhật
							state.editCurrentForm.priority = state.ruleForm.priority;

							await getAPI(VmsCurrentApi).apiVmsVmscurrentUpdatePost({ ...state.editCurrentForm });
						} catch (error) {
							// Người dùng chọn 'Hủy'
							console.log("Cập nhật đã bị hủy bởi người dùng do độ ưu tiên thấp hơn.");
						}
					} else {
						// Không có xung đột về độ ưu tiên, tiếp tục cập nhật
						state.editCurrentForm.color = state.ruleForm.textColor;
						state.editCurrentForm.backColor = state.ruleForm.bgColor;
						state.editCurrentForm.priority = state.ruleForm.priority;

						await getAPI(VmsCurrentApi).apiVmsVmscurrentUpdatePost({ ...state.editCurrentForm });
					}
				} else {
					// Insert new entry
					await getAPI(VmsCurrentApi).apiVmsVmscurrentAddPost(state.editCurrentForm);
					console.log("VMS Current added successfully.");
				}
			} catch (error) {
				console.error("Error occurred while saving VMS Current:", error);
			}
		} else if (activeCtrlType == VmsActionType.WordTemplate) {

			let wordList1 = state.ruleForm.wordList1?.map(item => state.vmsMsgTemplateAll?.find(vms => vms.code == item)?.content1);
			let wordList2 = state.ruleForm.wordList2?.map(item => state.vmsMsgTemplateAll?.find(vms => vms.code == item)?.content1);
			// Create RowData as JSON
			state.editCurrentForm.RowData = JSON.stringify({ wordList1, wordList2 });

			try {

				const response = await getAPI(VmsCurrentApi).apiVmsVmscurrentListGet();

				const mapLocations = response.data.result;

				const existingVms = mapLocations.find(location => state.listVmsSelected.includes(location.equipmentId));
				
				if (existingVms != null) {

					state.editCurrentForm.id = existingVms.id;
					state.editCurrentForm.oldRow = existingVms.rowData;
					state.editCurrentForm.oldUrl = existingVms.url;
					state.editCurrentForm.color = state.ruleForm.textColor;
					state.editCurrentForm.backColor = state.ruleForm.bgColor;

					if (state.ruleForm.priority > existingVms.priority) {

						const userConfirmation = confirm("The current priority is lower than the existing priority. Do you want to proceed with the update?");
						
						if (userConfirmation) {
							state.editCurrentForm.priority =state.ruleForm.priority;

							await getAPI(VmsCurrentApi).apiVmsVmscurrentUpdatePost({ ...state.editCurrentForm });
						} else {
							console.log("Update canceled by user due to lower priority.");
						}
					} else {
						// Proceed with the update if no priority conflict
						state.editCurrentForm.priority =state.ruleForm.priority;

						await getAPI(VmsCurrentApi).apiVmsVmscurrentUpdatePost({ ...state.editCurrentForm });
					}
				} else {
					// Insert new entry
					await getAPI(VmsCurrentApi).apiVmsVmscurrentAddPost(state.editCurrentForm);
				}
			} catch (error) {
				console.error("Error occurred while saving VMS Current:", error);
			}
		} else if ( activeCtrlType == VmsActionType.TextTemplate) {

			let row1 = state.ruleForm.row1?.trim();
			let row2 = state.ruleForm.row2?.trim();
			let row3 = state.ruleForm.row3?.trim();
			// Create RowData as JSON
			state.editCurrentForm.RowData = JSON.stringify({ row1, row2, row3});
			try {
				const response = await getAPI(VmsCurrentApi).apiVmsVmscurrentListGet();
				
				const mapLocations = response.data.result;

				const existingVms = mapLocations?.find(location => state.listVmsSelected.includes(location.equipmentId));
				
				if (existingVms != null) {
					state.editCurrentForm.id = existingVms.id;
					state.editCurrentForm.oldRowData = existingVms.rowData;
					state.editCurrentForm.oldUrl = existingVms.url;
					state.editCurrentForm.listEquipment = selectedCodeNameWithEquipment;
					state.editCurrentForm.color = state.ruleForm.textColor;
					state.editCurrentForm.backColor = state.ruleForm.bgColor;

					if (state.ruleForm.priority > existingVms.priority) {
						try {
							// Hiển thị modal xác nhận
							await ElMessageBox.confirm(
								"Độ ưu tiên hiện tại thấp hơn độ ưu tiên đã tồn tại. Bạn có muốn tiếp tục cập nhật không?",
								"Xác nhận",
								{
									confirmButtonText: "Đồng ý",
									cancelButtonText: "Hủy",
									type: "warning",
								}
							);

							// Người dùng chọn 'Đồng ý', tiến hành cập nhật
							state.editCurrentForm.priority = state.ruleForm.priority;

							await getAPI(VmsCurrentApi).apiVmsVmscurrentUpdatePost({ ...state.editCurrentForm });

						} catch (error) {
							// Người dùng chọn 'Hủy'
							console.log("Cập nhật đã bị hủy bởi người dùng do độ ưu tiên thấp hơn.");
						}
					} else {
						// Không có xung đột về độ ưu tiên, tiếp tục cập nhật
						state.editCurrentForm.color = state.ruleForm.textColor;

						state.editCurrentForm.backColor = state.ruleForm.bgColor;

						state.editCurrentForm.priority = state.ruleForm.priority;

						await getAPI(VmsCurrentApi).apiVmsVmscurrentUpdatePost({ ...state.editCurrentForm });
					}
				} else {
					// Insert new entry
					await getAPI(VmsCurrentApi).apiVmsVmscurrentAddPost(state.editCurrentForm);

				}
			} catch (error) {
				console.error("Error occurred while saving VMS Current:", error);
			}
		} else if (activeCtrlType == VmsActionType.ImageTemplate) {

			let image = selectedImageUrl.value || state.vmsImgTemplateAll?.find(item => item.code == state.ruleForm.imgTemplate)?.url;

			state.editCurrentForm.url = image;
			
			try {
				const response = await getAPI(VmsCurrentApi).apiVmsVmscurrentListGet();

				const mapLocations = response.data.result;

				const existingVms = mapLocations.find(location => state.listVmsSelected.includes(location.equipmentId));
				
				if (existingVms != null) {
					state.editCurrentForm.id = existingVms.id;

					state.editCurrentForm.oldUrl = existingVms.url;

					// Check priority before updating
					if (state.ruleForm.priority > existingVms.priority) {

						const userConfirmation = confirm("The current priority is lower than the existing priority. Do you want to proceed with the update?");
						
						if (userConfirmation) {
							state.editCurrentForm.priority =state.ruleForm.priority;

							await getAPI(VmsCurrentApi).apiVmsVmscurrentUpdatePost({ ...state.editCurrentForm });

						} else {

							console.log("Update canceled by user due to lower priority.");
						}
					} else {
						// Proceed with the update if no priority conflict
						state.editCurrentForm.priority =state.ruleForm.priority;

						await getAPI(VmsCurrentApi).apiVmsVmscurrentUpdatePost({ ...state.editCurrentForm });
					}
				} else {
					// If no existing VMS, add new entry
					await getAPI(VmsCurrentApi).apiVmsVmscurrentAddPost(state.editCurrentForm);
				}

				// Notify via socket channel
				await getAPI(TransporterSocketCluster).handleChannelData('vms.update', state.editCurrentForm);

			} catch (error) {
				console.error("Error occurred while saving VMS Process:", error);
			}
		} else if (activeCtrlType == VmsActionType.Timer) {

			const startTime = new Date(state.ruleForm.startTime);

			const endTime = new Date(state.ruleForm.endTime);

			const adjustTimezone = (date, offset) => {
				const utcTime = new Date(date.getTime() + date.getTimezoneOffset() * 60000);

				return new Date(utcTime.getTime() + offset * 60000);
			};

			const formattedstartTime = adjustTimezone(startTime, 420);

			const formattedendTime = adjustTimezone(endTime, 420);

			state.editProcessForm.Type = VmsCurrentType.Timer;

			state.editProcessForm.State = '0';

			let imageUrl = selectedImageUrl.value || state.vmsImgTemplateAll?.find(item => item.code == state.ruleForm.imgTemplate)?.url;

			if (imageUrl) {

				state.editProcessForm.processImgInput = {

					listUrl: [{ imageUrl: imageUrl }]
				};
			} else {

				state.editProcessForm.processImgInput = { listUrl: [] };
			}

			state.editProcessForm.timeStart = formattedstartTime.toISOString();

			state.editProcessForm.timeEnd = formattedendTime.toISOString();

			try {
				let response = await getAPI(VmsProcessApi).apiVmsVmsprocessListGet();

				const mapLocations = response.data.result;
				
				const existingVms = mapLocations.find(location => state.listVmsSelected.includes(location.vmsId));

				if (existingVms != null) {
					state.editProcessForm.id = existingVms.id;

					if (state.ruleForm.priority > existingVms.priority) {

						const userConfirmation = confirm("The current priority is lower than the existing priority. Do you want to proceed with the update?");
						
						if (userConfirmation) {

							state.editCurrentForm.priority =state.ruleForm.priority;
							
							await getAPI(VmsProcessApi).apiVmsVmsprocessUpdatePost({ ...state.editProcessForm });

						} else {
							console.log("Update canceled by user due to lower priority.");
						}
					} else {
						// Proceed with the update if no priority conflict
						state.editCurrentForm.priority =state.ruleForm.priority;

						await getAPI(VmsProcessApi).apiVmsVmsprocessUpdatePost({ ...state.editProcessForm });
					}
				} else {
					await getAPI(VmsProcessApi).apiVmsVmsprocessAddPost(state.editProcessForm);

					console.log("VMS Process added successfully.");
				}
			} catch (error) {

				console.error("Error occurred while saving VMS Process:", error);
			}

		} else if (activeCtrlType === VmsActionType.Scheduling) {

			const selectedImagesWithTTL = draggableImages.value.map((image) => ({

				imageUrl: image.preview,

				timeToLive: image.timeToLive || 5
			}));
			state.editProcessForm.Type = VmsCurrentType.Scheduling;  // Ensure Type is set

			state.editProcessForm.State = '0'; // Ensure State is set

			state.editProcessForm.processImgInput = { listUrl: selectedImagesWithTTL };

			try {
				
				let response = await getAPI(VmsProcessApi).apiVmsVmsprocessListGet();

				const mapLocations = response.data.result;

				const existingVms = mapLocations.find(location => state.listVmsSelected.includes(location.vmsId));

				if (existingVms) {
					state.editProcessForm.id = existingVms.id;

					if (state.ruleForm.priority > existingVms.priority) {

						const userConfirmation = confirm("The current priority is lower than the existing priority. Do you want to proceed with the update?");
						
						if (userConfirmation) {
							state.editCurrentForm.priority =state.ruleForm.priority;

							await getAPI(VmsCurrentApi).apiVmsVmscurrentUpdatePost({ ...state.editCurrentForm });

						} else {
							console.log("Update canceled by user due to lower priority.");
						}
					} else {
						// Proceed with the update if no priority conflict
						state.editCurrentForm.priority =state.ruleForm.priority;

						await getAPI(VmsCurrentApi).apiVmsVmscurrentUpdatePost({ ...state.editCurrentForm });
					}
				} else {
					await getAPI(VmsProcessApi).apiVmsVmsprocessAddPost(state.editProcessForm);

					console.log("VMS Process added successfully.");
				}
			} catch (error) {
				console.error("Error occurred while saving VMS Process:", error);
			}
		}
	};


// ========= VMS IMAGE =========
  // Load your data as soon as the instance of vue has been created
	const loadImgTemplateData = async() => {
      let response = await getAPI(VmsImageApi).apiVmsVmsimageListPost();
      state.vmsImgTemplateAll = response.data.result.filter(item => item.type === 'image');
       response = await getAPI(VmsTemplateApi).apiVmsVmstemplateListGet();
      state.vmsTemplateAll = response.data.result;
  	};

    const handleImageUpload = (fileList) => {
        if (!fileList || fileList.length === 0) {
          selectedImageUrl.value = null; // Nếu không có file nào, gán null cho selectedImageUrl
          return;
        }
        // Sử dụng hàm uploadImage để xử lý file
        uploadImage('yourElementId', fileList); // Thay 'yourElementId' bằng ID thực tế của bạn
    };

      // Hàm upload hình ảnh
    const uploadImage = (elementId: string, file: File) => {
        const reader = new FileReader();
        reader.onload = (e) => {
            // Cập nhật selectedImageUrl với hình ảnh đã upload
        	selectedImageUrl.value = e.target?.result as string;
        };
        reader.readAsDataURL(file.raw);
    };

        // Hàm kiểm tra file trước khi upload
    const beforeUpload = (file: File) => {
        const isImage = file.type.startsWith('image/');
        if (!isImage) {
        	ElMessage.error('Bạn chỉ có thể upload hình ảnh!');
        }
        return isImage; // Trả về true nếu file là hình ảnh
    };
    
        // Hàm giả định để cập nhật element, thay đổi nếu cần
    const updateSelectedImageUrl = () => {
        const selectedItem = state.vmsImgTemplateAll.find(item => item.code === state.ruleForm.imgTemplate);
        selectedImageUrl.value = selectedItem ? selectedItem.url : ''; 
    };

    const Uploadnull = () => {
        selectedImageUrl.value = null;
    };

// ========= TIMER =========

	const handleTimerTemplateChange = (selectedCode) => {
		const selectedItem = state.vmsTemplateAll.find(item => item.code === selectedCode);
		
		if (selectedItem) {
		// Add the selected image to the draggable list
		selectedImageUrl.value = selectedItem.preview;
		}
	};

// ========= SCHEDULING =========
  	const draggableImages = ref([]);  // Array to hold images for dragging

	const handleImgTemplateChange = async (selectedCode) => {
		const selectedItem = state.vmsTemplateAll.find(item => item.code === selectedCode);
		if (selectedItem) {
			const response = await getAPI(VmsTemplateApi).apiVmsVmstemplateDatalistIdGet(selectedItem.id);
			if (response && response.data && Array.isArray(response.data.result)) {
			// Find the design in the result array that matches the given ID
			const importedDesign = response.data.result.find(item => item.id === selectedItem.id);

			// Check if the design was found
			if (importedDesign && importedDesign.jsonData) {
				// Parse the jsonData from the found design
				const designData = JSON.parse(importedDesign.jsonData);
				const textElements = designData.elements.filter(element => element.type === 'text');
				// Update the selectedTemplate and selectedSizeId
					const contextData = textElements.map(element => element.content); 
			draggableImages.value.push({
				id: selectedItem.id,
				preview: selectedItem.preview,
				timeToLive: state.TimeToLive,
				Data:contextData
			});

			} else {
				console.error('No jsonData found for the specified design ID.');
			}
			} else {
			console.error('Invalid response structure or no results found.');
			}
		}
	};

	const removeAt = (idx) => {
		draggableImages.value.splice(idx, 1);
	};	

  	const submitrun =  () => {
		ruleFormRef.value.validate((valid: boolean) => {
			if (!valid) return;
			processOnRun();
			closeDialog();
		});
	};

	const submitstop =  () => {
		ruleFormRef.value.validate((valid: boolean) => {
			if (!valid) return;
			processOnStop();
			closeDialog();
		});
	};
	// chạy lập lịch 
	const processOnRun = async () => {
			const selectedCodeNameWithEquipment = state.listVmsSelected.map((selectedId) => {
			const equipment = state.equipmentList.find(equipment => equipment.id === selectedId);
			if (equipment) {
				return {
					Code: equipment.code,   
					Name: equipment.name,   
					EquipmentId: equipment.id     
				};
			}
			return null; // Return null if no matching equipment is found
		}).filter(item => item !== null); // Filter out any null values
			state.editCurrentForm.listEquipment = selectedCodeNameWithEquipment;
			state.editProcessForm.Brightness = state.ruleForm.brightness;
			state.editProcessForm.vmsIds = state.listVmsSelected;  // Ensure Type is set
			state.editProcessForm.Type = VmsCurrentType.Scheduling;  // Ensure Type is set
			state.editProcessForm.State = '0'; // Ensure State is set
			const selectedImagesWithTTL = draggableImages.value.map((image) => ({
				imageUrl: image.preview,
				timeToLive: image.timeToLive || 5,
				Data: image.Data
			}));
			state.editProcessForm.processImgInput = { listUrl: selectedImagesWithTTL };

			try {
				const response = await getAPI(VmsProcessApi).apiVmsVmsprocessListGet();
				const mapLocations = response.data.result;

				const existingVms = mapLocations?.find(location => state.listVmsSelected.includes(location.vmsId));

				
				if (existingVms) {
                state.editProcessForm.id = existingVms.id;
				state.editProcessForm.code = existingVms.code;
				state.editProcessForm.processId = existingVms.processId;
				if (state.ruleForm.priority > existingVms.priority) {
					const userConfirmation = confirm("The current priority is lower than the existing priority. Do you want to proceed with the update?");
					
					if (userConfirmation) {
						state.editCurrentForm.priority =state.ruleForm.priority;
						await getAPI(VmsProcessApi).apiVmsVmsprocessUpdatePost({ ...state.editProcessForm });
						const run = await getAPI(VmsProcessApi).apiVmsVmsprocessRunPost({ ...state.editProcessForm });					
						state.isRunningProcess = run.data.result.status;
					}
				} else {
					// Proceed with the update if no priority conflict
					state.editCurrentForm.priority =state.ruleForm.priority;
					await getAPI(VmsProcessApi).apiVmsVmsprocessUpdatePost({ ...state.editProcessForm });
					const run = await getAPI(VmsProcessApi).apiVmsVmsprocessRunPost({ ...state.editProcessForm });					
				}

            } else {
                await getAPI(VmsProcessApi).apiVmsVmsprocessAddPost(state.editProcessForm);
				const run = await getAPI(VmsProcessApi).apiVmsVmsprocessRunPost({ ...state.editProcessForm });					

            }
			
			} catch (error) {
				console.error("Error occurred while saving VMS Process:", error);
			}
	};
	const processOnStop = async () => {
			const selectedCodeNameWithEquipment = state.listVmsSelected.map((selectedId) => {
			const equipment = state.equipmentList.find(equipment => equipment.id === selectedId);
			if (equipment) {
				return {
					Code: equipment.code,   
					Name: equipment.name,   
					EquipmentId: equipment.id     
				};
			}
			return null; // Return null if no matching equipment is found
		}).filter(item => item !== null); // Filter out any null values
			state.editCurrentForm.listEquipment = selectedCodeNameWithEquipment;
			state.editProcessForm.Brightness = state.ruleForm.brightness;
			state.editProcessForm.vmsIds = state.listVmsSelected;  // Ensure Type is set
			state.editProcessForm.Type = VmsCurrentType.TextManual;  // Ensure Type is set
			state.editProcessForm.State = '0'; // Ensure State is set
			const selectedImagesWithTTL = draggableImages.value.map((image) => ({
				imageUrl: image.preview,
				timeToLive: image.timeToLive || 5,
				Data:image.Data,
			}));

			state.editProcessForm.processImgInput = { listUrl: selectedImagesWithTTL };

			try {
				const response = await getAPI(VmsProcessApi).apiVmsVmsprocessListGet();
				const mapLocations = response.data.result;

				const existingVms = mapLocations.find(location => state.listVmsSelected.includes(location.vmsId));

					state.editProcessForm.id = existingVms.id;
					state.editProcessForm.code = existingVms.code;
					state.editProcessForm.processId = existingVms.processId;
					await getAPI(VmsProcessApi).apiVmsVmsprocessRunPost({ ...state.editProcessForm });
					console.log("VMS Process updated successfully.");
			
			} catch (error) {
				console.error("Error occurred while saving VMS Process:", error);
			}
	};

	// Export toggleSimplePopup
	defineExpose({toggleMyPopup});
</script>
<style scoped>
    /* Table */
	
	.table-data{
		width: 100%;
		max-height: 100%;
		max-width: 100%;
	}

	.table-data th,
	.table-data td{
		text-align: left;
		font-weight: normal;
		padding: 5px;
	}

	.table-data th{
		text-align: right;
	}

	.table-data .border-bottom--dashed td,
	.table-data .border-bottom--dashed th{
		border-bottom: thin dashed #E5E5E5;
	}

	/* Canvas */
	#livecam-container{
		background-color: black !important;
		display: flex;
		align-items: center;
		justify-content: center;
	}

	#livecam-container canvas{
		width: 100%;
		height: auto;
		max-width: 100%;
		max-height: 100%;
	}

    /* Simple Popup */
	.simple-popup{
		position: absolute;
		top: 0px;
		left: 0px;
		display: flex;
		justify-content: center;
		align-items: center;
		z-index: 1080 !important;
		background-color: rgba(0, 0, 0, 0.5);
		width: 100%;
		height: 100%;
	}

	.simple-popup--container{
		display: flex;
		flex-direction: column;
		overflow: hidden;
		width: fit-content;
		height: 80%;
		min-width: 40%;
		min-height: 80%;
		max-width: 80%;
		max-height: 80%;
		background-color: white;
		border-radius: 5px;
	}

	.simple-popup--container .panel-header{
		height: 40px;
		max-height: 40px;
	}

	.simple-popup--container .panel-body{
		flex-grow: 1;
	}

    /* Button close */
	.button-close{
		position: absolute;
		top: 5px;
		right: 5px;
	}

	.button-close::before{
		display: block;
		position: absolute;
		left: 0;
		top: 0;
		width: 100%;
		height: 100%;
		display: flex;
		justify-content: center;
		align-items: center;
		content: '\00d7';
		font-weight: lighter;
		font-size: 20px;
	}

	/* Position */
	.pos-relative{
		position: relative;
	}

	.pos-absolute{
		position: absolute
	}

	/* Style */
	.top-right{
		top: 0px;
		right: 0px;
		z-index: 1080 !important;
	}

	.flex-inline{
		display: flex;
		gap: 10px;
	}

	.text-uppercase{
		text-transform: uppercase;
	}

	.padding-5px{
		padding: 5px;
	}

	.padding-10px{
		padding: 10px;
	}

	.margin-5px{
		margin: 5px;
	}

	.overflow-auto{
		overflow: auto;
	}
	.vms-modal-container {
  /* Add any container styles here */
}

.preview {
  border: 1px solid #ccc;
  overflow: hidden;
}
.design-container, .preview-content {
  border: 1px solid #ccc;
  margin: 0 auto;
  overflow: hidden;
}
.text-content {
  padding: 10px;
}

:deep(.el-form-item__content) {
  justify-content: flex-start;
}

/* Image simple select */
.simple-img_select{
  max-height: 50vh;
  overflow-y: auto;
  padding: 5px; 
}

.simple-img_preview,
.simple-img_select li {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px !important; /* Important!!! */
  height: auto !important; /* Important!!! */
  max-height: 70px;
}

.simple-img_select li{
  border-bottom: thin dashed #E5E5E5;
}

.simple-img_preview {
  border: thin solid #E5E5E5;
  border-radius: 5px;
}

.simple-img_preview img,
.simple-img_select li img{
  max-height: 50px !important; /* Important!!! */
  max-width: 100% !important; /* Important!!! */
}
/* Template library */
.template-designer {
  display: flex;
  flex-direction: column;
  gap: 20px;
  padding: 20px;
  max-width: 1200px;
  margin: 0 auto;
}

.element-group {
  margin-bottom: 20px;
}

.element-content {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.element-position-size {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  margin-top: 10px;
}

.input-group {
  display: flex;
  align-items: center;
}

.input-label {
  margin-right: 5px;
  font-weight: bold;
}

.design-area, .preview {
  width: 100%;
}

.design-container, .preview-content {
  border: 1px solid #ccc;
  margin: 0 auto;
  overflow: hidden;
}

.element {
  position: absolute;
  box-sizing: border-box;
}

.element:hover {
  border-color: #3a8ee6;
}

.element-content {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
}

.element-label {
  position: absolute;
  top: 0;
  left: 0;
  background-color: rgba(0, 0, 0, 0.5);
  color: white;
  font-size: 12px;
  padding: 2px 4px;
}

.element-info {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  background-color: rgba(0, 0, 0, 0.5);
  color: white;
  font-size: 12px;
  padding: 2px 4px;
  text-align: center;
}

.element-actions {
  position: absolute;
  top: 0;
  right: 0;
  display: flex;
  gap: 4px;
}

.element-actions button {
  background-color: white;
  border: 1px solid #3a8ee6;
  color: #3a8ee6;
  cursor: pointer;
  padding: 2px;
  font-size: 12px;
}

.element-actions button.active {
  background-color: #3a8ee6;
  color: white;
}

.element-control {
  margin-bottom: 20px;
}

.element-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 10px;
}

.add-remove-controls {
  margin-top: 20px;
}

.image-uploader {
  width: 100%;
}

.uploaded-image-preview {
  display: flex;
  align-items: center;
  margin-top: 10px;
  padding: 10px;
  background-color: #f0f0f0;
  border-radius: 4px;
}

.uploaded-image {
  width: 100px;
  height: 100px;
  object-fit: cover;
  margin-right: 10px;
  border-radius: 4px;
}

.import-export {
  margin-top: 20px;
}

.import-upload {
  display: inline-block;
  margin-left: 10px;
}
</style>