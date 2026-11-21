<template>
	<div class="sys-userCenter-container">
		<el-row :gutter="5" style="width: 100%">
			<el-col :span="8" :xs="24">
				<el-card shadow="hover">
					<div class="account-center-avatarHolder">
						<el-avatar
							:size="100"
							:src="userInfos.avatar"
							@click="openCropperDialog"
							v-loading="state.avatarLoading"
							element-loading-spinner="el-icon-Upload"
							element-loading-background="rgba(0, 0, 0, 0.2)"
							@mouseenter="mouseEnterAvatar"
							@mouseleave="mouseLeaveAvatar"
						/>
						<div class="username">{{ userInfos.realName }}</div>
					</div>
					<div class="account-center-org">
						<p>
							<el-icon><ele-School /></el-icon> <span>{{ userInfos.orgName ?? $t('lz.label.system.unknownOrg') }}</span>
						</p>
						<p>
							<el-icon><ele-Mug /></el-icon> <span>{{ userInfos.posName ?? $t('lz.label.system.unknownPos') }}</span>
						</p>
						<p>
							<el-icon><ele-LocationInformation /></el-icon> <span>{{ userInfos.address ?? $t('lz.label.system.unknownAddress') }}</span>
						</p>
					</div>
					<div class="image-signature">
						<el-image :src="userInfos.signature" fit="contain" :alt="$t('lz.label.system.signature')" loading="lazy" style="width: 100%; height: 100%"> </el-image>
					</div>
					<el-button icon="ele-Edit" type="primary" @click="openSignDialog" v-auth="'sysUser:uploadSignature'"> {{$t('lz.label.system.signature')}} </el-button>
					<el-upload
						ref="uploadSignRef"
						action=""
						accept=".png"
						:limit="1"
						:show-file-list="false"
						:auto-upload="false"
						:on-change="uploadSignFile"
						:on-exceed="uploadSignFileExceed"
						style="display: inline-block; margin-left: 12px; position: absolute"
					>
						<el-button icon="ele-UploadFilled" v-auth="'sysUser:uploadSignature'"> {{$t('lz.label.system.uploadSignature')}} </el-button>
					</el-upload>
				</el-card>
			</el-col>

			<el-col :span="16" :xs="24">
				<el-card shadow="hover">
					<el-tabs>
						<el-tab-pane :label="$t('lz.label.base.basicInfo')" v-loading="state.loading">
							<el-form :model="state.ruleFormBase" ref="ruleFormBaseRef" label-width="auto" label-position="left">
								<el-row :gutter="10">
									<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
										<el-form-item :label="$t('lz.entity.base.name')">
											<el-input v-model="state.ruleFormBase.name" :placeholder="$t('lz.label.base.pleaseType',{input:$t('lz.entity.base.name')})" clearable />
										</el-form-item>
									</el-col>
									<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
										<el-form-item :label="$t('lz.entity.base.realName')" prop="realName">
											<el-input v-model="state.ruleFormBase.realName" :placeholder="$t('lz.label.base.pleaseType',{input:$t('lz.entity.base.realName')})" clearable />
										</el-form-item>
									</el-col>
									<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
										<el-form-item :label="$t('lz.entity.base.phone')" prop="phone"
											:rules="[{ validator: validatePhone, trigger: 'blur' }]">
											<el-input v-model="state.ruleFormBase.phone" :placeholder="$t('lz.label.base.pleaseType',{input:$t('lz.entity.base.phone')})" clearable />
										</el-form-item>
									</el-col>
									<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
										<el-form-item :label="$t('lz.entity.base.email')" prop="email"
											:rules="[{ validator: validateEmail, trigger: 'blur' }]">
											<el-input v-model="state.ruleFormBase.email" :placeholder="$t('lz.label.base.pleaseType',{input:$t('lz.entity.base.email')})" clearable />
										</el-form-item>
									</el-col>
									<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
										<el-form-item :label="$t('lz.entity.base.dateOfBirth')" prop="birthday">
											<el-date-picker v-model="state.ruleFormBase.dateOfBirth" type="date" :placeholder="$t('lz.label.base.pleaseSelect',{input:$t('lz.entity.base.dateOfBirth')})"
											format="YYYY-MM-DD" value-format="YYYY-MM-DD" class="w100" />
										</el-form-item>
									</el-col>
									<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
										<el-form-item :label="$t('lz.entity.base.address')">
											<el-input v-model="state.ruleFormBase.address" :placeholder="$t('lz.label.base.pleaseType',{input:$t('lz.entity.base.address')})" clearable type="textarea" />
										</el-form-item>
									</el-col>
									<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
										<el-form-item :label="$t('lz.entity.base.remark')">
											<el-input v-model="state.ruleFormBase.remark" :placeholder="$t('lz.label.base.pleaseType',{input:$t('lz.entity.base.remark')})" clearable type="textarea" />
										</el-form-item>
									</el-col>
									<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
										<el-form-item>
											<el-button icon="ele-SuccessFilled" type="primary" @click="submitUserBase" v-auth="'sysUser:baseInfo'"> {{$t('lz.button.base.save')}} </el-button>
										</el-form-item>
									</el-col>
								</el-row>
							</el-form>
						</el-tab-pane>
						<el-tab-pane :label="$t('lz.label.sysUser.changePassword')">
							<el-form ref="ruleFormPasswordRef" :model="state.ruleFormPassword" label-width="auto" label-position="left">
								<el-form-item :label="$t('lz.entity.system.passwordOld')" prop="passwordOld" :rules="[{ required: true, message: $t('lz.validation.base.required',{input:$t('lz.entity.system.passwordOld')}), trigger: 'blur' }]">
									<el-input v-model="state.ruleFormPassword.passwordOld" type="password" autocomplete="off" :placeholder="$t('lz.label.base.pleaseType',{input:$t('lz.entity.system.passwordOld')})" />
								</el-form-item>
								<el-form-item :label="$t('lz.entity.system.passwordNew')" prop="passwordNew" :rules="[{ required: true, message: $t('lz.validation.base.required',{input:$t('lz.entity.system.passwordNew')}), trigger: 'blur' }]">
									<el-input v-model="state.ruleFormPassword.passwordNew" type="password" autocomplete="off" :placeholder="$t('lz.label.base.pleaseType',{input:$t('lz.entity.system.passwordNew')})" />
								</el-form-item>
								<el-form-item :label="$t('lz.entity.system.passwordConfirm')" prop="passwordNew2" :rules="[{ validator: validatePassword, required: true, trigger: 'blur' }]">
									<el-input v-model="state.passwordNew2" type="password" autocomplete="off" :placeholder="$t('lz.label.base.pleaseType',{input:$t('lz.entity.system.passwordConfirm')})" />
								</el-form-item>
								<el-form-item>
									<el-button icon="ele-Refresh" @click="resetPassword"> {{$t('lz.button.base.reset')}} </el-button>
									<el-button icon="ele-SuccessFilled" type="primary" @click="submitPassword" v-auth="'sysUser:changePwd'"> {{$t('lz.button.base.confirm')}} </el-button>
								</el-form-item>
							</el-form>
						</el-tab-pane>
						<el-tab-pane :label="$t('lz.label.sysUser.twoFactorSetting')" v-loading="state.otpLoading">
							<div class="otp-settings-container">
								<!-- OTP Status Section -->
								<el-row v-if="state.otpStatus?.isEnabled" :gutter="24">
									<el-col :span="24">
										<el-card class="otp-status-card" shadow="never">
											<el-result
												icon="success"
												:title="$t('lz.label.sysUser.otpEnabled')"
												:sub-title="$t('lz.label.sysUser.otpMethodInfo', { method: state.otpStatus.method })"
												style="padding:5px;"
											>
												<template #extra >
													<el-space direction="vertical" alignment="center" size="large" >
														<el-tag type="success" size="large" effect="dark">
															<i class="fa fa-solid fa-shield"></i>
															{{ $t('lz.label.sysUser.highSecurity') }}
														</el-tag>
														
														<el-descriptions :column="1" border>
															<el-descriptions-item :label="$t('lz.label.sysUser.protectedAccount')">
																<el-icon color="#67c23a"><ele-Select /></el-icon>
																{{ $t('lz.label.sysUser.protectedAccountDesc') }}
															</el-descriptions-item>
															<el-descriptions-item :label="$t('lz.label.sysUser.verificationRequired')">
																<el-icon color="#67c23a"><ele-Cellphone /></el-icon>
																{{ $t('lz.label.sysUser.verificationRequiredDesc') }}
															</el-descriptions-item>
														</el-descriptions>
														
														<el-button
															type="danger"
															@click="showDisableOtpDialog"
															v-auth="'sysUser:otpDisable'"
															size="large"
														>
															<el-icon><ele-Unlock /></el-icon>
															{{ $t('lz.button.sysUser.disableOtp') }}
														</el-button>
													</el-space>
												</template>
											</el-result>
										</el-card>
									</el-col>
								</el-row>

								<el-row v-else :gutter="24">
									<el-col :span="24">
										<el-card class="otp-status-card" shadow="never">
											<el-result
												icon="warning"
												:title="$t('lz.label.sysUser.otpDisabled')"
												:sub-title="$t('lz.label.sysUser.otpDescription')"
												style="padding:5px;"
											>
												<template #extra>
													<el-space direction="vertical" alignment="center" size="large">
														<el-tag type="warning" size="large" effect="dark">
															<i class="fa fa-solid fa-exclamation"></i>
															{{ $t('lz.label.sysUser.lowSecurity') }}
														</el-tag>
														
														<el-button
															type="primary"
															size="large"
															@click="openOtpSetupModal"
															v-auth="'sysUser:otpSetup'"
														>
															<el-icon><ele-Right /></el-icon>
															{{ $t('lz.button.sysUser.setupOtp') }}
														</el-button>
													</el-space>
												</template>
											</el-result>
										</el-card>
									</el-col>
								</el-row>
							</div>
						</el-tab-pane>
					</el-tabs>
				</el-card>
			</el-col>
		</el-row>

		<el-dialog v-model="state.signDialogVisible" draggable :close-on-click-modal="false" width="600px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-EditPen /> </el-icon>
					<span> {{$t('lz.label.system.signature')}} </span>
				</div>
			</template>
			<div style="border: 1px dashed gray; width: 100%; height: 250px">
				<VueSignaturePad ref="signaturePadRef" :options="state.signOptions" style="background-color: #fff" />
			</div>
			<div style="margin-top: 10px">
				<div style="display: inline"> {{$t('lz.label.sysUser.brushThickness')}}:<el-input-number v-model="state.signOptions.minWidth" :min="0.5" :max="2.5" :step="0.1" size="small" /></div>
				<div style="display: inline; margin-left: 30px"> {{$t('lz.label.system.brushColor')}}:<el-color-picker v-model="state.signOptions.penColor" color-format="hex" size="default"> </el-color-picker></div>
			</div>
			<template #footer>
				<span class="dialog-footer">
					<el-button @click="unDoSign"> {{$t('lz.button.base.undo')}} </el-button>
					<el-button @click="clearSign"> {{$t('lz.button.base.clear')}} </el-button>
					<el-button type="primary" @click="saveUploadSign"> {{$t('lz.button.base.save')}} </el-button>
				</span>
			</template>
		</el-dialog>

		<!-- OTP Setup Modal -->
		<el-dialog
			v-model="state.otpSetupModalVisible"
			draggable
			:close-on-click-modal="false"
			width="800px"
			:before-close="handleOtpModalClose"
		>
		<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Setting /> </el-icon>
					<span>{{ $t('lz.label.sysUser.setupOtp') }}</span>
				</div>
			</template>
			<div class="otp-setup-modal">
				<!-- Step Indicator -->
				<div class="step-indicator">
					<el-steps :active="state.currentOtpStep" finish-status="success" align-center>
						<el-step :title="$t('lz.label.sysUser.step1Title')" :description="$t('lz.label.sysUser.step1Description')" />
						<el-step :title="$t('lz.label.sysUser.step2Title')" :description="$t('lz.label.sysUser.step2Description')" />
						<el-step :title="$t('lz.label.sysUser.step3Title')" :description="$t('lz.label.sysUser.step3Description')" />
					</el-steps>
				</div>

				<!-- Step 1: Authenticator App Recommendations -->
				<div v-if="state.currentOtpStep === 0" class="otp-step-content">
					<el-card shadow="never" class="step-card">
						<template #header>
							<div class="step-header">
								<span class="step-title">{{ $t('lz.label.sysUser.step1Title') }}</span>
							</div>
						</template>
						
						<div class="authenticator-apps">
							<el-alert
								:title="$t('lz.label.sysUser.installAuthApp')"
								type="info"
								:closable="false"
								show-icon
								class="mb20"
							/>
							
							<el-row :gutter="16">
								<el-col :span="8">
									<el-card shadow="hover" class="app-card">
										<div class="app-info">
											<el-avatar :size="60" :background-color="'#4285F4'">
												<span style="font-size: 24px; font-weight: bold;">G</span>
											</el-avatar>
											<h4>Google Authenticator</h4>
											<p>{{ $t('lz.label.sysUser.googleAuthDesc') }}</p>
											<el-button type="primary" size="small" @click="openAppStore('google')">
												{{ $t('lz.label.sysUser.downloadApp') }}
											</el-button>
										</div>
									</el-card>
								</el-col>
								<el-col :span="8">
									<el-card shadow="hover" class="app-card">
										<div class="app-info">
											<el-avatar :size="60" :background-color="'#00A4EF'">
												<span style="font-size: 24px; font-weight: bold;">M</span>
											</el-avatar>
											<h4>Microsoft Authenticator</h4>
											<p>{{ $t('lz.label.sysUser.microsoftAuthDesc') }}</p>
											<el-button type="primary" size="small" @click="openAppStore('microsoft')">
												{{ $t('lz.label.sysUser.downloadApp') }}
											</el-button>
										</div>
									</el-card>
								</el-col>
								<el-col :span="8">
									<el-card shadow="hover" class="app-card">
										<div class="app-info">
											<el-avatar :size="60" :background-color="'#D32F2F'">
												<span style="font-size: 24px; font-weight: bold;">A</span>
											</el-avatar>
											<h4>Authy</h4>
											<p>{{ $t('lz.label.sysUser.authyDesc') }}</p>
											<el-button type="primary" size="small" @click="openAppStore('authy')">
												{{ $t('lz.label.sysUser.downloadApp') }}
											</el-button>
										</div>
									</el-card>
								</el-col>
							</el-row>
						</div>
					</el-card>
				</div>

				<!-- Step 2: QR Code Display -->
				<div v-if="state.currentOtpStep === 1" class="otp-step-content">
					<el-card shadow="never" class="step-card">
						<template #header>
							<div class="step-header">
								<span class="step-title">{{ $t('lz.label.sysUser.step2Title') }}</span>
								<el-button
									type="text"
									@click="refreshOtpSetup"
									:loading="state.otpSetupLoading"
									class="refresh-btn"
								>
									<el-icon><ele-RefreshRight /></el-icon>
									{{ $t('lz.button.sysUser.refreshQrCode') }}
								</el-button>
							</div>
						</template>
						
						<div v-loading="state.otpSetupLoading">
							<el-row :gutter="24">
								<el-col :span="10">
									<div class="qr-section">
										<div class="qr-code-container">
											<div class="qr-code-wrapper">
												<el-image
													:src="state.otpSetupData?.qrCode || ''"
													:preview-src-list="[state.otpSetupData?.qrCode || '']"
													fit="contain"
													class="qr-code"
												>
													<template #error>
														<div class="image-error">
															<el-icon><ele-Picture /></el-icon>
															<span>{{ $t('lz.label.sysUser.qrCodeLoadError') }}</span>
														</div>
													</template>
												</el-image>
											</div>
											
											<el-text type="info" class="qr-instructions">{{ $t('lz.label.sysUser.qrCodeInstructions') }}</el-text>
										</div>
									</div>
								</el-col>
								
								<el-col :span="14">
									<div class="manual-entry-section">
										<el-collapse v-model="state.manualEntryExpanded">
											<el-collapse-item :title="$t('lz.label.sysUser.cannotScanQr')" name="manual">
												<div class="manual-entry-content">
													<el-text type="primary" tag="b" class="secret-key-label">{{ $t('lz.label.sysUser.secretKey') }}:</el-text>
													<el-input
														:model-value="state.otpSetupData?.secretKey || ''"
														readonly
														class="secret-key-input"
														size="default"
													>
														<template #append>
															<el-button
																@click="copyToClipboard(state.otpSetupData?.secretKey || '')"
																type="primary"
																size="default"
															>
																<el-icon><ele-CopyDocument /></el-icon>
															</el-button>
														</template>
													</el-input>
												</div>
											</el-collapse-item>
										</el-collapse>
									</div>
								</el-col>
							</el-row>
						</div>
					</el-card>
				</div>

				<!-- Step 3: OTP Verification -->
				<div v-if="state.currentOtpStep === 2" class="otp-step-content">
					<el-card shadow="never" class="step-card">
						<template #header>
							<div class="step-header">
								<span class="step-title">{{ $t('lz.label.sysUser.step3Title') }}</span>
							</div>
						</template>
						
						<div class="verification-section">
							<el-form ref="otpVerifyFormRef" :model="state.otpVerifyForm" :rules="otpVerifyRules" label-width="0">
								<el-form-item prop="otpCode">
									<el-input
										v-model="state.otpVerifyForm.otpCode"
										:placeholder="$t('lz.label.sysUser.enterOtpPlaceholder')"
										maxlength="6"
										size="large"
										clearable
										class="otp-input"
									>
										<template #prefix>
											<el-icon><ele-Key /></el-icon>
										</template>
									</el-input>
								</el-form-item>
							</el-form>
						</div>
					</el-card>
				</div>
			</div>

			<template #footer>
				<div class="otp-modal-footer">
					<el-button @click="handleOtpModalClose" :disabled="state.otpVerifyLoading">
						{{ $t('lz.button.base.cancel') }}
					</el-button>
					<el-button
						v-if="state.currentOtpStep > 0"
						@click="previousOtpStep"
						:disabled="state.otpVerifyLoading"
					>
						{{ $t('lz.button.base.previous') }}
					</el-button>
					<el-button
						v-if="state.currentOtpStep < 2"
						type="primary"
						@click="nextOtpStep"
						:loading="state.otpSetupLoading"
					>
						{{ $t('lz.button.base.next') }}
					</el-button>
					<el-button
						v-if="state.currentOtpStep === 2"
						type="primary"
						@click="verifyAndEnableOtp"
						:loading="state.otpVerifyLoading"
						v-auth="'sysUser:otpVerify'"
					>
						{{ $t('lz.button.sysUser.verifyAndEnable') }}
					</el-button>
				</div>
			</template>
		</el-dialog>

		<!-- Disable 2FA Modal -->
		<el-dialog
			v-model="state.disableOtpModalVisible"
			draggable
			:close-on-click-modal="false"
			width="500px"
		>
		<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Unlock /> </el-icon>
					<span>{{ $t('lz.label.sysUser.disable2faTitle') }}</span>
				</div>
			</template>
			<div class="disable-otp-modal">
				<el-alert
					:title="$t('lz.message.base.warning')"
					:description="$t('lz.label.sysUser.disable2faWarning')"
					type="warning"
					:closable="false"
					show-icon
					class="mb20"
				/>
				
				<el-form ref="disableOtpFormRef" :model="state.disableOtpForm" :rules="disableOtpRules" label-width="120px">
					<el-form-item :label="$t('lz.label.sysUser.passwordLabel')" prop="password">
						<el-input
							v-model="state.disableOtpForm.password"
							type="password"
							:placeholder="$t('lz.label.sysUser.enterPasswordPlaceholder')"
							show-password
							clearable
							:disabled="state.disableOtpLoading"
						>
							<template #prefix>
								<el-icon><ele-Lock /></el-icon>
							</template>
						</el-input>
					</el-form-item>
				</el-form>
			</div>

			<template #footer>
				<div class="disable-otp-modal-footer">
					<el-button @click="handleDisableOtpModalClose" :disabled="state.disableOtpLoading">
						{{ $t('lz.label.sysUser.cancel') }}
					</el-button>
					<el-button
						type="danger"
						@click="confirmDisableOtp"
						:loading="state.disableOtpLoading"
						v-auth="'sysUser:otpDisable'"
					>
						{{ $t('lz.label.sysUser.disable2fa') }}
					</el-button>
				</div>
			</template>
		</el-dialog>

		<CropperDialog ref="cropperDialogRef" :title="state.cropperTitle" @uploadCropperImg="uploadCropperImg" />
	</div>
</template>
<script lang="ts" setup name="sysUserCenter">
import { onMounted, watch, reactive, ref } from 'vue';
import { storeToRefs } from 'pinia';
import { ElForm, ElMessageBox, genFileId, ElMessage } from 'element-plus';
import type { UploadInstance, UploadProps, UploadRawFile } from 'element-plus';
import { useUserInfo } from '/@/stores/userInfo';
import { base64ToFile, blobToFile } from '/@/utils/base64Conver';

// import OrgTree from '/@/views/system/user/component/orgTree.vue';
import CropperDialog from '/@/components/cropper/index.vue';
import VueGridLayout from 'vue-grid-layout';
import { clearAccessTokens, getAPI } from '/@/utils/axios-utils';
import { SysFileApi, SysUserApi, SysAccountApi, SysOtpApi } from '/@/api-services/system';
import { ChangePwdInput, SysUser, SysFile, SetupTotpOutput, OtpStatusOutput, VerifyAndEnableTotpInput, DisableOtpInput } from '/@/api-services/system';
import { useI18n } from 'vue-i18n';
const {t: $t} = useI18n();
const stores = useUserInfo();
const { userInfos } = storeToRefs(stores);
const uploadSignRef = ref<UploadInstance>();
const signaturePadRef = ref<InstanceType<typeof VueGridLayout>>();
const ruleFormBaseRef = ref<InstanceType<typeof ElForm>>();
const ruleFormPasswordRef = ref<InstanceType<typeof ElForm>>();
const cropperDialogRef = ref<InstanceType<typeof CropperDialog>>();
const otpVerifyFormRef = ref<InstanceType<typeof ElForm>>();
const disableOtpFormRef = ref<InstanceType<typeof ElForm>>();
const otpVerifyRules = {
	otpCode: [
		{ required: true, message: '', trigger: 'blur' },
		{ pattern: /^\d{6}$/, message: '', trigger: 'blur' }
	]
};
const disableOtpRules = {
	password: [
		{ required: true, message: $t('lz.message.sysUser.confirmDisableOtp'), trigger: 'blur' }
	]
};
const state = reactive({
	loading: false,
	avatarLoading: false,
	signDialogVisible: false,
	ruleFormBase: {} as SysUser,
	ruleFormPassword: {} as ChangePwdInput,
	signOptions: {
		penColor: '#000000',
		minWidth: 1.0,
		onBegin: () => {
			signaturePadRef.value.resizeCanvas();
		},
	},
	signFileList: [] as any,
	passwordNew2: '',
	cropperTitle: '',
	// OTP related state
	otpLoading: false,
	otpSetupLoading: false,
	otpVerifyLoading: false,
	otpStatus: null as OtpStatusOutput | null,
	otpSetupData: null as SetupTotpOutput | null,
	otpVerifyForm: {
		otpCode: '',
		tempToken: ''
	},
	disableOtpForm: {
		password: ''
	},
	// OTP Modal state
	otpSetupModalVisible: false,
	currentOtpStep: 0,
	manualEntryExpanded: [] as string[],
	manualSecretKey: '',
	// Disable OTP Modal state
	disableOtpModalVisible: false,
	disableOtpLoading: false
});

// Initialize the page
onMounted(async () => {
	state.loading = true;
	var res = await getAPI(SysUserApi).apiSystemSysuserBaseinfoGet();
	state.ruleFormBase = res.data.result ?? { username: '' };
	state.loading = false;
	
	// Load OTP status
	await loadOtpStatus();
});

// Signature listener
watch(state.signOptions, () => {
	signaturePadRef.value.signaturePad.penColor = state.signOptions.penColor;
	signaturePadRef.value.signaturePad.minWidth = state.signOptions.minWidth;
});

// Upload avatar image
const uploadCropperImg = async (e: any) => {
	var res = await getAPI(SysFileApi).apiSystemSysfileUploadavatarPostForm(blobToFile(e.img, userInfos.value.account + '.png'));
	userInfos.value.avatar = getFileUrl(res.data.result!);
};

// Open electronic signature page
const openSignDialog = () => {
	state.signDialogVisible = true;
};

// Save and upload electronic signature
const saveUploadSign = async () => {
	const { isEmpty, data } = signaturePadRef.value.saveSignature();
	if (isEmpty) return;

	var res = await getAPI(SysFileApi).apiSystemSysfileUploadsignaturePostForm(base64ToFile(data, userInfos.value.account + '.png'));
	userInfos.value.signature = getFileUrl(res.data.result!);
	ElMessage.success($t('lz.message.base.saveSuccess'));

	clearSign();
	state.signDialogVisible = false;
};

// Undo electronic signature
const unDoSign = () => {
	signaturePadRef.value.undoSignature();
};

// Clear electronic signature
const clearSign = () => {
	signaturePadRef.value.clearSignature();
};

// Upload handwritten electronic signature
const uploadSignFile = async (file: any) => {
	var res = await getAPI(SysFileApi).apiSystemSysfileUploadsignaturePostForm(file.raw);
	userInfos.value.signature = res.data.result?.url;
	ElMessage.success($t('lz.message.base.saveSuccess'));

};

// Get electronic signature file list
const handleChangeSignFile = (_file: any, fileList: []) => {
	state.signFileList = fileList;
};

// Modify personal information
const submitUserBase = () => {
	ruleFormBaseRef.value?.validate(async (valid: boolean) => {
		if (!valid) return;
		ElMessageBox.confirm($t('lz.message.sysUser.confirmModify'), $t('lz.message.base.warning'), {
			confirmButtonText: $t('lz.button.base.confirm'),
			cancelButtonText: $t('lz.button.base.cancel'),
			type: 'warning',
		}).then(async () => {
			await getAPI(SysUserApi).apiSystemSysuserBaseinfoPost(state.ruleFormBase);
			ElMessage.success($t('lz.message.base.saveSuccess'));

		});
	});
};

// Email validation (optional field — only validates format when a value is entered)
const validateEmail = (_rule: any, value: any, callback: any) => {
    if (!value) { callback(); return; }
    const emailReg = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailReg.test(value)) {
        callback(new Error($t('lz.validation.base.emailInvalid')));
    } else {
        callback();
    }
};

// Phone validation (optional field — only validates format when a value is entered)
// Chấp nhận số điện thoại Việt Nam (0[3|5|7|8|9]xxxxxxxx hoặc +84[3|5|7|8|9]xxxxxxxx)
const validatePhone = (_rule: any, value: any, callback: any) => {
    if (!value) { callback(); return; }
    const phoneReg = /^(0|\+84)[35789]\d{8}$/;
    if (!phoneReg.test(value)) {
        callback(new Error($t('lz.validation.base.phoneInvalid')));
    } else {
        callback();
    }
};

// Password validation
const validatePassword = (_rule: any, value: any, callback: any) => {
	if (state.passwordNew2 != state.ruleFormPassword.passwordNew) {
		callback(new Error($t('lz.exception.system.passwordNotMatch')));
	} else {
		callback();
	}
};

// Reset password
const resetPassword = () => {
	state.ruleFormPassword.passwordOld = '';
	state.ruleFormPassword.passwordNew = '';
	state.passwordNew2 = '';
};

// Submit password
const submitPassword = () => {
	ruleFormPasswordRef.value?.validate(async (valid: boolean) => {
		if (!valid) return;

		// SM2 encrypt password
		const cpwd: ChangePwdInput = { passwordOld: '', passwordNew: '' };
		// const publicKey = window.__env__.VITE_SM_PUBLIC_KEY;
		// cpwd.passwordOld = sm2.doEncrypt(state.ruleFormPassword.passwordOld, publicKey, 1);
		// cpwd.passwordNew = sm2.doEncrypt(state.ruleFormPassword.passwordNew, publicKey, 1);
		cpwd.passwordOld = state.ruleFormPassword.passwordOld;
		cpwd.passwordNew = state.ruleFormPassword.passwordNew;
		await getAPI(SysAccountApi).apiSystemSysaccountChangepwdPost(cpwd);

		// Log out of the system
		ElMessageBox.confirm($t('lz.message.sysAccount.logoutWhenChangePwd'), $t('lz.message.base.warning'), {
			confirmButtonText: $t('lz.button.base.confirm'),
			cancelButtonText: $t('lz.button.base.cancel'),
			type: 'warning',
		}).then(async () => {
			clearAccessTokens();
		});


	});
};

// Open crop dialog
const openCropperDialog = () => {
	state.cropperTitle = 'Change Avatar';
	cropperDialogRef.value?.openDialog(userInfos.value.avatar);
};

// Mouse enter and leave avatar
const mouseEnterAvatar = () => {
	state.avatarLoading = true;
};

const mouseLeaveAvatar = () => {
	state.avatarLoading = false;
};

// Execute when upload signature exceeds limit
const uploadSignFileExceed: UploadProps['onExceed'] = (files) => {
	uploadSignRef.value!.clearFiles();
	const file = files[0] as UploadRawFile;
	file.uid = genFileId();
	uploadSignRef.value!.handleStart(file);
};

// Get file URL
const getFileUrl = (row: SysFile): string => {
	if (row.bucketName == 'Local') {
		return `/${row.filePath}/${row.id}${row.suffix}`;
	} else {
		return row.url!;
	}
};

// OTP related functions
// Load OTP status
const loadOtpStatus = async () => {
	try {
		state.otpLoading = true;
		const res = await getAPI(SysOtpApi).apiSystemSysotpStatusGet();
		state.otpStatus = res.data.result || null;
	} catch (error) {
		console.error('Failed to load OTP status:', error);
		ElMessage.error($t('lz.message.sysUser.otpStatusLoadFailed'));
	} finally {
		state.otpLoading = false;
	}
};

// Setup OTP
const setupOtp = async () => {
	try {
		state.otpSetupLoading = true;
		const res = await getAPI(SysOtpApi).apiSystemSysotpSetupTotpPost();
		state.otpSetupData = res.data.result || null;
		if (state.otpSetupData?.tempToken) {
			state.otpVerifyForm.tempToken = state.otpSetupData.tempToken;
		}
	} catch (error) {
		console.error('Failed to setup OTP:', error);
		ElMessage.error($t('lz.message.sysUser.otpSetupFailed'));
	} finally {
		state.otpSetupLoading = false;
	}
};

// Refresh OTP setup
const refreshOtpSetup = async () => {
	await setupOtp();
	ElMessage.success($t('lz.message.sysUser.otpSetupRefreshed'));
};

// Copy to clipboard
const copyToClipboard = async (text: string) => {
	try {
		await navigator.clipboard.writeText(text);
		ElMessage.success($t('lz.message.base.copySuccess'));
	} catch (error) {
		console.error('Failed to copy to clipboard:', error);
		ElMessage.error($t('lz.message.base.copyFailed'));
	}
};


// Cancel OTP setup
const cancelOtpSetup = () => {
	state.otpSetupData = null;
	state.otpVerifyForm.otpCode = '';
	state.otpVerifyForm.tempToken = '';
};

// Show disable OTP dialog
const showDisableOtpDialog = () => {
	state.disableOtpModalVisible = true;
	state.disableOtpForm.password = '';
};

// Handle disable OTP modal close
const handleDisableOtpModalClose = () => {
	if (!state.disableOtpLoading) {
		state.disableOtpModalVisible = false;
		state.disableOtpForm.password = '';
	}
};

// Confirm disable OTP
const confirmDisableOtp = () => {
	disableOtpFormRef.value?.validate(async (valid: boolean) => {
		if (!valid) return;
		
		try {
			state.disableOtpLoading = true;
			await disableOtp(state.disableOtpForm.password);
			
			// Close modal and reset form
			state.disableOtpModalVisible = false;
			state.disableOtpForm.password = '';
		} catch (error) {
			console.error('Failed to disable OTP:', error);
		} finally {
			state.disableOtpLoading = false;
		}
	});
};

// Disable OTP
const disableOtp = async (password: string) => {
	try {
		const disableData: DisableOtpInput = {
			password: password
		};
		
		await getAPI(SysOtpApi).apiSystemSysotpDisablePost(disableData);
		
		ElMessage.success($t('lz.message.sysUser.otpDisabled'));
		
		// Reload OTP status
		await loadOtpStatus();
	} catch (error) {
		console.error('Failed to disable OTP:', error);
		ElMessage.error($t('lz.message.sysUser.otpDisableFailed'));
		throw error;
	}
};

// OTP Modal Functions
// Open OTP Setup Modal
const openOtpSetupModal = async () => {
	try {
		state.otpSetupModalVisible = true;
		state.currentOtpStep = 0;
		state.manualEntryExpanded = [];
		state.manualSecretKey = '';
		state.otpVerifyForm.otpCode = '';
		state.otpVerifyForm.tempToken = '';
	} catch (error) {
		console.error('Failed to open OTP setup modal:', error);
		ElMessage.error($t('lz.message.sysUser.otpSetupFailed'));
	}
};

// Handle OTP Modal Close
const handleOtpModalClose = () => {
	if (!state.otpVerifyLoading) {
		state.otpSetupModalVisible = false;
		state.currentOtpStep = 0;
		state.otpSetupData = null;
		state.otpVerifyForm.otpCode = '';
		state.otpVerifyForm.tempToken = '';
		state.manualSecretKey = '';
		state.manualEntryExpanded = [];
	}
};

// Next OTP Step
const nextOtpStep = async () => {
	if (state.currentOtpStep === 0) {
		// Moving from step 1 to step 2, setup OTP data
		await setupOtp();
		if (state.otpSetupData?.secretKey) {
			state.manualSecretKey = state.otpSetupData.secretKey;
		}
	}
	if (state.currentOtpStep < 2) {
		state.currentOtpStep++;
	}
};

// Previous OTP Step
const previousOtpStep = () => {
	if (state.currentOtpStep > 0) {
		state.currentOtpStep--;
	}
};

// Open App Store
const openAppStore = (appType: string) => {
	const urls = {
		google: {
			ios: 'https://apps.apple.com/app/google-authenticator/id388497605',
			android: 'https://play.google.com/store/apps/details?id=com.google.android.apps.authenticator2'
		},
		microsoft: {
			ios: 'https://apps.apple.com/app/microsoft-authenticator/id983702846',
			android: 'https://play.google.com/store/apps/details?id=com.azure.authenticator'
		},
		authy: {
			ios: 'https://apps.apple.com/app/authy/id494168017',
			android: 'https://play.google.com/store/apps/details?id=com.authy.authy'
		}
	};

	const userAgent = navigator.userAgent.toLowerCase();
	const isIOS = /iphone|ipad|ipod/.test(userAgent);
	const isAndroid = /android/.test(userAgent);
	
	let url = '';
	if (isIOS) {
		url = urls[appType as keyof typeof urls].ios;
	} else if (isAndroid) {
		url = urls[appType as keyof typeof urls].android;
	} else {
		// For desktop, open the web version
		url = appType === 'google' ? 'https://support.google.com/accounts/answer/1066447' :
		     appType === 'microsoft' ? 'https://www.microsoft.com/en-us/account/authenticator' :
		     'https://authy.com/download/';
	}
	
	window.open(url, '_blank');
};

// Update verifyAndEnableOtp to handle modal close
const verifyAndEnableOtp = () => {
	otpVerifyFormRef.value?.validate(async (valid: boolean) => {
		if (!valid) return;
		
		try {
			state.otpVerifyLoading = true;
			const verifyData: VerifyAndEnableTotpInput = {
				tempToken: state.otpVerifyForm.tempToken,
				otpCode: state.otpVerifyForm.otpCode
			};
			
			await getAPI(SysOtpApi).apiSystemSysotpVerifyAndEnableTotpPost(verifyData);
			
			ElMessage.success($t('lz.message.sysUser.otpEnabled'));
			
			// Close modal and reset state
			state.otpSetupModalVisible = false;
			state.currentOtpStep = 0;
			state.otpSetupData = null;
			state.otpVerifyForm.otpCode = '';
			state.otpVerifyForm.tempToken = '';
			state.manualSecretKey = '';
			state.manualEntryExpanded = [];
			
			// Reload OTP status
			await loadOtpStatus();
		} catch (error) {
			console.error('Failed to verify and enable OTP:', error);
			ElMessage.error($t('lz.message.sysUser.otpVerifyFailed'));
		} finally {
			state.otpVerifyLoading = false;
		}
	});
};

// Export object
defineExpose({ handleChangeSignFile });
</script>

<style lang="scss" scoped>
.account-center-avatarHolder {
	text-align: center;
	margin-bottom: 24px;

	.username {
		font-size: 20px;
		line-height: 28px;
		font-weight: 500;
		margin-bottom: 4px;
	}
}
.account-center-org {
	margin-bottom: 8px;
	position: relative;
	p {
		margin-top: 10px;
	}
	span {
		padding-left: 10px;
	}
}
.avatar {
	margin: 0 auto;
	width: 104px;
	height: 104px;
	margin-bottom: 20px;
	border-radius: 50%;
	overflow: hidden;
	img {
		height: 100%;
		width: 100%;
	}
}

.image-signature {
	margin-top: 20px;
	margin-bottom: 10px;
	width: 100%;
	height: 150px;
	background-color: #fff;
	text-align: center;
	vertical-align: middle;
	border: solid 1px var(--el-border-color);
}


.otp-setup-modal {
	.step-indicator {
		margin-bottom: 24px;
		
		:deep(.el-steps) {
			.el-step__title {
				font-size: 12px;
				font-weight: 600;
			}
			
			.el-step__icon {
				font-size: 14px;
			}
			
			.el-step.is-finish .el-step__icon {
				background-color: var(--el-color-primary);
				border-color: var(--el-color-primary);
			}
			
			.el-step.is-finish .el-step__line {
				background-color: var(--el-color-primary);
			}
		}
	}
	
	.otp-step-content {
		min-height: 400px;
		
		.step-card {
			border-radius: 12px;
			border: 2px solid var(--el-border-color-lighter);
			transition: all 0.3s ease;
			
			&:hover {
				border-color: var(--el-color-primary-light-7);
				box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
			}
			
			.step-header {
				display: flex;
				align-items: center;
				justify-content: space-between;
				width: 100%;
				
				.step-title {
					font-size: 18px;
					font-weight: 600;
					color: var(--el-text-color-primary);
				}
				
				.refresh-btn {
					font-size: 14px;
					color: var(--el-color-primary);
					
					.el-icon {
						margin-right: 4px;
					}
				}
			}
		}
	}
	
	.authenticator-apps {
		.app-card {
			height: 100%;
			transition: transform 0.3s ease;
			text-align: center;
			
			&:hover {
				transform: translateY(-5px);
			}
			
			.app-info {
				padding: 16px;
				
				h4 {
					margin: 12px 0 8px 0;
					font-size: 16px;
					font-weight: 600;
				}
				
				p {
					margin: 0 0 16px 0;
					font-size: 14px;
					color: var(--el-text-color-regular);
					min-height: 40px;
				}
			}
		}
	}
	
	.qr-section {
		.qr-code-container {
			display: flex;
			flex-direction: column;
			align-items: center;
			padding: 20px 0;
		}
		
		.qr-code-wrapper {
			display: flex;
			justify-content: center;
			align-items: center;
			background: white;
			border-radius: 12px;
			box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
			width: fit-content;
			
			.qr-code {
				width: 180px;
				height: 180px;
				border-radius: 8px;
				border: 1px solid var(--el-border-color-light);
			}
			
			.image-error {
				display: flex;
				flex-direction: column;
				justify-content: center;
				align-items: center;
				width: 100%;
				height: 100%;
				background-color: var(--el-fill-color-light);
				
				.el-icon {
					font-size: 50px;
					color: var(--el-text-color-placeholder);
					margin-bottom: 8px;
				}
				
				span {
					font-size: 14px;
					color: var(--el-text-color-placeholder);
				}
			}
		}
		
		.qr-instructions {
			font-size: 15px;
			margin: 16px 0;
			text-align: center;
			max-width: 300px;
		}
	}
	
	.manual-entry-section {
		.manual-entry-content {
			.secret-key-label {
				font-size: 16px;
				margin-bottom: 12px;
				display: block;
			}
			
			.secret-key-input {
				:deep(.el-input__inner) {
					font-family: 'Rotobo', monospace;
					font-size: 13px;
					padding: 8px 12px;
					letter-spacing: 1px;
				}
				
				:deep(.el-input-group__append) {
					.el-button {
						padding: 0 12px;
					}
				}
			}
		}
	}
	
	.verification-section {
		min-width: 100px;
		.otp-input {
			max-width: 400px;
			margin: 0 auto;
			:deep(.el-input__inner) {
				text-align: center;
				letter-spacing: 6px;
				padding: 16px;
			}
		}
	}
}

:deep(.el-step__title) {
	font-weight: bold;
}

:deep(.el-step__icon-inner) {
	font-weight: bold;
	font-size: 12px;
}
:deep(.el-step__icon-inner) {
	font-size: 14px!important;
}

:deep(.el-step__head.is-process) {
	color: var(--el-color-primary);
	border-color: var(--el-color-primary);
}

:deep(.el-step__head.is-finish) {
	color: var(--el-color-success);
	border-color: var(--el-color-success);
}

:deep(.el-step__description){
	margin-top: 3px;
	font-weight: bold;
	color: var(--el-color-primary)!important;
}

// Disable OTP Modal Styles
.disable-otp-modal-header {
	display: flex;
	align-items: center;
	font-size: 18px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

.disable-otp-modal {
	.mb20 {
		margin-bottom: 20px;
	}
	
	.el-form {
		margin-top: 20px;
	}
}

</style>