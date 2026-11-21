<template>
	<div class="esh-edit-mapping-dialog">
		<el-dialog ref="dialogRef" v-model="state.isShowDialog" draggable :close-on-click-modal="!state.isProcessing" :show-close="!state.isProcessing" :close-on-press-escape="!state.isProcessing" align-center class="mp-dialog" :width="dialogWidth">
			<template #header>
				<div style="color: var(--el-color-white)">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit />
					</el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>

			<el-form v-loading="state.loading" :disabled="state.isProcessing || state.loading" :model="state.editForm" ref="editFormRef" label-width="auto"
				label-position="left" autocomplete="off" class="mp-form-body">
				<el-tabs v-model="state.activeTab" class="mp-tabs">
				<el-tab-pane :label="$t('lz.label.sharedataMapping.generalInfo')" name="info">
				<el-row :gutter="10">
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<!-- Mã sinh từ đối tác/gói tin/chiều, không cho nhập tay khi sao chép. -->
						<el-form-item :label="$t('lz.entity.base.code')" prop="code" class="mp-code-item" :error="state.codeError ? $t(state.codeError) : ''"
							:rules="[{ required: true, message: $t('lz.validation.sharedataMapping.codeGenHint'), trigger: 'change' }, { max: EntityFieldLength.Length64, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.base.code'), MaxLength: EntityFieldLength.Length64 }), trigger: 'change' }]">
							<el-input v-model="state.editForm.code" :placeholder="$t('lz.label.sharedataMapping.codeAutoGen')"
								disabled>
							</el-input>
							<div class="rs-note">{{ $t(props.operateType === 'copy' ? 'lz.label.sharedataMapping.copyDestinationHint' : 'lz.label.sharedataMapping.generatedCodeHint') }}</div>
						</el-form-item>
					</el-col>

					<!-- Mã sinh từ ĐỐI TÁC _ GÓI TIN _ CHIỀU nên đứng đầu để thấy ngay kết quả.
					     Gói tin chỉ liệt kê những gói đối tác đã đăng ký đúng chiều đã chọn;
					     đổi chiều hoặc đối tác thì gói tin đang chọn bị bỏ. -->
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.sharedataMapping.partner')" prop="partnerId"
							:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.validation.sharedataMapping.partnerRequired') }), trigger: 'change' }]">
							<el-select v-model="state.editForm.partnerId" :placeholder="$t('lz.label.sharedataMapping.partnerSelect')"
								filterable class="w100" :disabled="props.operateType === 'edit'"
								@change="handlePartnerChange">
								<el-option v-for="item in props.partnerList" :key="item.id"
									:label="codeNameLabel(item)" :value="item.id" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.sharedataMapping.datatype')" prop="datatypeId"
							:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.validation.sharedataMapping.datatypeRequired') }), trigger: 'change' }]">
							<el-select v-model="state.editForm.datatypeId"
								:placeholder="state.editForm.partnerId ? $t('lz.label.sharedataMapping.datatypeId') : $t('lz.label.sharedataMapping.selectPartnerFirst')"
								filterable class="w100" v-loading="state.loadingSubscriptions"
								:disabled="props.operateType === 'edit' || !state.editForm.partnerId"
								@change="handlePacketChange">
								<el-option v-for="item in availablePacketList" :key="item.id"
									:label="codeNameLabel(item)" :value="item.id" />
							</el-select>
							<!-- Đối tác chưa đăng ký gói tin nào ở chiều này thì không có gì để ánh xạ:
							     nói thẳng nơi cần vào khai báo thay vì để người dùng ngồi tìm. -->
							<div v-if="showNoSubscriptionHint" class="direction-hint">
								{{ $t('lz.label.sharedataMapping.noSubscriptionPacketHint') }}
							</div>
						</el-form-item>
					</el-col>

					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.sharedataMapping.direction')" prop="direction"
							:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.validation.sharedataMapping.directionRequired') }), trigger: 'change' }]">
							<el-radio-group v-model="state.editForm.direction" :disabled="props.operateType === 'edit'"
								@change="handleDirectionChange">
								<el-radio v-for="item in DIRECTION_LIST" :key="item.code" :value="item.code"
									class="mr5">{{ item.name }}</el-radio>
							</el-radio-group>
							<!-- Bộ khung có $extend.expression thì KHÔNG dùng chung hai chiều được:
							     chiều gửi tính ra giá trị mới, chiều nhận không có hàm ngược để tính lại. -->
							<div v-if="state.editForm.direction === 2" class="direction-hint">
								{{ $t('lz.label.sharedataMapping.shareFrameworkBothDirectionsHint') }}
							</div>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.name')" prop="name" class="mp-name-item"
							:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.name') }), trigger: 'blur' }]">
							<el-input v-model="state.editForm.name"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.name') })"
								clearable />
						</el-form-item>
					</el-col>

					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.sharedataMapping.isActive')" prop="isActive">
							<el-switch v-model="state.editForm.isActive" />
							<span class="ml10 rs-note">{{ $t('lz.label.sharedataMapping.onlyOneActiveHint') }}</span>
						</el-form-item>
					</el-col>
					<!-- Ghi chú tách hẳn một dòng riêng: ô một hàng quá chật so với nội dung cần ghi. -->
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.base.remark')" prop="remark"
							:rules="[{ max: EntityFieldLength.Length256, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.base.remark'), MaxLength: EntityFieldLength.Length256 }), trigger: 'blur' }]">
							<el-input v-model="state.editForm.remark"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.remark') })"
								type="textarea" :rows="2" :maxlength="EntityFieldLength.Length256" show-word-limit
								clearable />
						</el-form-item>
					</el-col>
				</el-row>
				</el-tab-pane>

				<el-tab-pane :label="$t('lz.label.sharedataMapping.mappingTab')" name="map">
				<el-row :gutter="10">
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24">
						<el-row :gutter="10">
							<!-- Bên trái: khuôn đối tác đưa + bộ khung đầu ra sinh ra từ nó -->
							<el-col :xs="24" :sm="24" :md="8" :lg="7" :xl="7" class="mb20">
								<div class="mp-pane">
									<div class="mp-pane-head">
										<span class="mp-pane-title">{{ $t('lz.label.sharedataMapping.partnerJson') }}</span>
										<el-button type="primary" size="small" icon="ele-MagicStick"
											:disabled="!state.partnerJson.trim()" @click="parsePartnerJson()">
											{{ $t('lz.button.sharedataMapping.analyze') }}
										</el-button>
									</div>
									<el-input v-model="state.partnerJson" type="textarea" :rows="state.templateTree.length ? 12 : 6" resize="vertical"
										class="mp-json" spellcheck="false" />
									<div v-if="state.jsonError" class="mp-err">{{ state.jsonError }}</div>
									<div v-else class="rs-note">{{ state.paneHint }}</div>

									<!-- Bộ khung đầu ra: chính là tài liệu sẽ lưu vào TargetShapeJson và service đọc -->
									<div class="mp-pane-head mt10">
										<span class="mp-pane-title">{{ $t('lz.label.sharedataMapping.outputFramework') }}</span>
										<el-button icon="ele-Refresh" size="small" :disabled="!state.leaves.length"
											@click="refreshShape">{{ $t('lz.button.sharedataMapping.rebuild') }}</el-button>
									</div>
									<div v-if="state.shapeError" class="mp-err">{{ state.shapeError }}</div>
									<el-input v-else-if="state.shapeJson" :model-value="state.shapeJson"
										type="textarea" :rows="12" resize="vertical" readonly class="mp-json" />
									<div v-else class="mp-shape-empty rs-note">{{ $t('lz.label.sharedataMapping.analyzeToBuildFrameworkHint') }}</div>
									<div v-if="state.shapeJson" class="rs-note">
										<i18n-t keypath="lz.label.sharedataMapping.frameworkSyntaxHint" tag="span">
											<template #fieldSyntax><code>{"$field":"X"}</code></template>
											<template #extendSyntax><code>$extend</code></template>
											<template #excludeSyntax><code>{"$exclude":true}</code></template>
										</i18n-t>
									</div>
								</div>
							</el-col>

							<!-- Bên phải: cây vẽ lại ĐÚNG cấu trúc JSON của đối tác.
								 Nút nhóm không có control. Nút mảng có công tắc lặp. Nút lá chọn được trường. -->
							<el-col :xs="24" :sm="24" :md="16" :lg="17" :xl="17" class="mb20">
								<div class="mp-pane">
									<div class="mp-pane-head">
										<span class="mp-pane-title">{{ $t('lz.label.sharedataMapping.mappingByPartnerStructure') }}</span>
										<el-button icon="ele-MagicStick" size="small" :disabled="!state.leaves.length"
											@click="handleAutoMatch">
											{{ $t('lz.button.sharedataMapping.autoMatch') }}
										</el-button>
										<span v-if="state.leaves.length" class="rs-note">
											{{ $t('lz.label.sharedataMapping.fieldsCountAndMatched', { total: state.leaves.length, matched: state.mappedCount }) }}
										</span>
									</div>

									<el-alert
										v-if="Boolean(state.editForm.datatypeId) && !state.loadingFields && state.packetFields.length === 0"
										type="info"
										show-icon
										:closable="false"
										class="mb10 packet-no-fields-alert"
										:title="$t('lz.label.sharedataMapping.packetHasNoFieldsHint')"
									/>

									<div v-if="!state.templateTree.length" class="mp-tree-empty">
										<el-empty :description="$t('lz.label.sharedataMapping.pasteJsonAndAnalyzeHint')"
											:image-size="60" />
									</div>
									<!-- Chú thích icon: người dùng nhìn icon ở cột phải là biết lá đó đang khai luật gì. -->
									<div v-if="state.templateTree.length" class="mp-legend">
										<span class="mp-legend-title">{{ $t('lz.label.sharedataMapping.legendTitle') }}</span>
										<span class="mp-legend-item"><el-icon><ele-Switch /></el-icon>{{ $t('lz.label.sharedataMapping.iconCodeSet') }}</span>
										<span class="mp-legend-item"><el-icon><ele-Tickets /></el-icon>{{ $t('lz.label.sharedataMapping.iconTargetType') }}</span>
										<span class="mp-legend-item"><el-icon><ele-Calendar /></el-icon>{{ $t('lz.label.sharedataMapping.iconDateFormat') }}</span>
										<span class="mp-legend-item"><el-icon><ele-Histogram /></el-icon>{{ $t('lz.label.sharedataMapping.iconNumberFormat') }}</span>
										<span class="mp-legend-item"><el-icon><ele-MagicStick /></el-icon>{{ $t('lz.label.sharedataMapping.iconDefaultValue') }}</span>
										<span class="mp-legend-item"><el-icon><ele-EditPen /></el-icon>{{ $t('lz.label.sharedataMapping.iconConstValue') }}</span>
										<span class="mp-legend-item"><el-icon><ele-Cpu /></el-icon>{{ $t('lz.label.sharedataMapping.builtinFieldGroup') }}</span>
										<span class="mp-legend-item mp-legend-item--warn"><el-icon><ele-WarningFilled /></el-icon>{{ $t('lz.label.sharedataMapping.iconWarning') }}</span>
									</div>
									<!-- Nhắc chọn Meta cho khoá ngoài mảng dữ liệu: service gộp nhiều bản ghi vào một gói,
									     lá ngoài mảng chỉ lấy bản ghi đầu nên gắn trường gói tin vào đó gần như luôn sai. -->
									<div v-if="state.templateTree.length" class="mp-meta-hint">
										<el-icon><ele-InfoFilled /></el-icon>
										<i18n-t keypath="lz.label.sharedataMapping.metaSelectionHint" tag="span">
											<template #metaFields><b>Now, Serial, PacketCode, PartnerCode</b></template>
										</i18n-t>
									</div>
									<!-- Tiêu đề cột đặt sát trên cây để thẳng hàng với từng dòng: trái khoá đối tác, giữa trường hệ thống, phải luật quy đổi. -->
									<div v-if="state.templateTree.length" class="mp-tree-header">
										<span class="mp-tree-header-left">{{ $t('lz.label.sharedataMapping.partnerHeader') }}</span>
										<span class="mp-tree-header-mid">{{ $t('lz.label.sharedataMapping.systemHeader') }}</span>
										<span class="mp-tree-header-right">{{ $t('lz.label.sharedataMapping.rulesHeader') }}</span>
									</div>
									<el-tree v-if="state.templateTree.length" :key="state.treeVersion" v-loading="state.loadingFields"
										:data="state.templateTree" node-key="path" :props="TREE_PROPS" default-expand-all
										:expand-on-click-node="false" class="mp-tree" :style="{ maxHeight: `${treeMaxHeight}px` }">
										<template #default="{ data }">
											<div class="mp-node">
												<span class="mp-node-key">{{ data.key }}</span>

												<!-- Nút mảng: nhãn cho biết mảng sẽ nhân theo bản ghi hay giữ nguyên.
													 Quy ước suy từ nội dung: có trường được ánh xạ bên trong thì nhân. -->
												<template v-if="data.kind === 'array'">
													<el-tag size="small" :type="mappedUnder(data.path) ? 'warning' : 'info'"
														effect="plain">{{ mappedUnder(data.path) ? $t('lz.label.sharedataMapping.multiplyByRecord')
															: $t('lz.label.sharedataMapping.fixedList') }}</el-tag>
													<span class="rs-note">{{ mappedUnder(data.path) }}/{{ leavesUnder(data.path) }}</span>
												</template>

												<!-- Nút nhóm: chỉ nhãn và tiến độ nhánh -->
												<template v-else-if="data.kind === 'object'">
													<el-tag size="small" type="info" effect="plain">{{ $t('lz.label.sharedataMapping.group') }}</el-tag>
													<span class="rs-note">{{ mappedUnder(data.path) }}/{{ leavesUnder(data.path) }}</span>
												</template>

											<!-- Nút lá: trái là khoá đối tác, GIỮA là trường hệ thống, PHẢI là icon của luật. -->
											<template v-else>
												<span class="mp-node-sample">{{ data.sample }}</span>

												<span class="mp-node-mid">
													<el-select v-model="cfgOf(data.path).fieldKey" size="small" filterable
														clearable :placeholder="Boolean(state.editForm.datatypeId) && !state.packetFields.length ? $t('lz.label.sharedataMapping.packetHasNoFieldsHint') : $t('lz.label.sharedataMapping.packetField')" class="mp-node-field"
														@change="handleFieldChange(data.path)">
														<el-option-group :label="$t('lz.label.sharedataMapping.builtinFieldGroup')">
															<el-option v-for="b in BUILTIN_FIELDS" :key="b.key" :label="b.key" :value="b.key">
																<span>{{ b.key }}</span>
																<span class="mp-opt-note">{{ $t(b.labelKey) }}</span>
															</el-option>
														</el-option-group>
														<el-option-group :label="$t('lz.label.sharedataMapping.packetFieldGroup')">
															<el-option v-if="!state.packetFields.length" disabled value="" :label="$t('lz.label.sharedataMapping.packetHasNoFieldsHint')" />
															<el-option v-for="f in state.packetFields" :key="f.aliasFieldKey!"
																:label="f.aliasFieldKey!" :value="f.aliasFieldKey!">
																<span>{{ f.aliasFieldKey }}</span>
																<span class="mp-opt-note">{{ f.name }}</span>
															</el-option>
														</el-option-group>
													</el-select>

													<el-icon v-if="fieldHint(cfgOf(data.path).fieldKey)" :title="fieldHint(cfgOf(data.path).fieldKey)" class="mp-info"><ele-InfoFilled /></el-icon>
												</span>

												<!-- Thiết lập của lá + luật quy đổi của trường đã chọn.
													 trigger="manual" + state.editingPath: chỉ mở một form tại một thời điểm,
													 và chỉ đóng khi người dùng bấm OK hoặc Đóng, không tự tắt khi click ra ngoài. -->
												<!-- Cột icon là nút mở form thiết lập của lá: bấm icon nào cũng mở đúng lá đó. -->
												<span class="mp-node-icons" :class="{ 'mp-node-icons--readonly': isBuiltinLeaf(data.path) }"
													@click="toggleLeafForm(data.path)">
													<el-icon v-if="isBuiltinLeaf(data.path)" :title="$t('lz.label.sharedataMapping.builtinNoRuleHint')" class="mp-icon mp-icon--muted"><ele-Cpu /></el-icon>
													<el-icon v-if="!isBuiltinLeaf(data.path) && leafWarning(data.path)" :title="leafWarning(data.path)" class="mp-icon mp-icon--warn"><ele-WarningFilled /></el-icon>
													<el-icon v-if="!isBuiltinLeaf(data.path) && Boolean(cfgOf(data.path).fieldKey) && Boolean((cfgOf(data.path).codeSet ?? '').trim())" :title="`${$t('lz.label.sharedataMapping.iconCodeSet')}: ${cfgOf(data.path).codeSet}`" class="mp-icon"><ele-Switch /></el-icon>
													<el-icon v-if="!isBuiltinLeaf(data.path) && Boolean(cfgOf(data.path).fieldKey) && Boolean(cfgOf(data.path).targetType.trim())" :title="`${$t('lz.label.sharedataMapping.iconTargetType')}: ${cfgOf(data.path).targetType}`" class="mp-icon"><ele-Tickets /></el-icon>
													<el-icon v-if="!isBuiltinLeaf(data.path) && Boolean(cfgOf(data.path).fieldKey) && Boolean(cfgOf(data.path).dateFormat.trim())" :title="`${$t('lz.label.sharedataMapping.iconDateFormat')}: ${cfgOf(data.path).dateFormat}`" class="mp-icon"><ele-Calendar /></el-icon>
													<el-icon v-if="!isBuiltinLeaf(data.path) && Boolean(cfgOf(data.path).fieldKey) && Boolean(cfgOf(data.path).numberFormat.trim())" :title="`${$t('lz.label.sharedataMapping.iconNumberFormat')}: ${cfgOf(data.path).numberFormat}`" class="mp-icon"><ele-Histogram /></el-icon>
													<el-icon v-if="!isBuiltinLeaf(data.path) && Boolean(cfgOf(data.path).fieldKey) && Boolean(defaultValueOf(cfgOf(data.path)))" :title="defaultValueOf(cfgOf(data.path))" class="mp-icon"><ele-MagicStick /></el-icon>
													<el-icon v-if="!isBuiltinLeaf(data.path) && !cfgOf(data.path).fieldKey && Boolean(cfgOf(data.path).constValue.trim())" :title="`${$t('lz.label.sharedataMapping.iconConstValue')}: ${cfgOf(data.path).constValue}`" class="mp-icon"><ele-EditPen /></el-icon>
													<el-icon v-if="!isBuiltinLeaf(data.path) && !hasExtra(data.path) && !leafWarning(data.path)" :title="$t('lz.label.sharedataMapping.iconSettings')" class="mp-icon mp-icon--muted"><ele-Setting /></el-icon>
												</span>
												</template>
											</div>
										</template>
									</el-tree>

									<!-- Trường gói tin đối tác không yêu cầu: chỉ liệt kê để đối chiếu -->
									<el-collapse v-if="state.packetFields.length" class="mt5">
										<el-collapse-item>
											<template #title>
												<span class="rs-note">{{ $t('lz.label.sharedataMapping.remainingUnrequestedFields', { count: notRequestedFields().length }) }}</span>
											</template>
											<div class="mp-unused">
												<el-tag v-for="f in notRequestedFields()" :key="f.aliasFieldKey!" size="small"
													effect="plain" :type="f.isRequired === true ? 'danger' : 'info'">{{ f.aliasFieldKey }}</el-tag>
												<span v-if="!notRequestedFields().length" class="rs-note">{{ $t('lz.label.sharedataMapping.partnerTemplateUsedAllFields') }}</span>
											</div>
										</el-collapse-item>
									</el-collapse>
								</div>
							</el-col>
						</el-row>
					</el-col>
				</el-row>
				</el-tab-pane>
				<!-- Tab gửi đi: bản ghi thật dưới CSDL đi qua bộ khung thành payload GỬI ĐI. Chỉ hiện khi hồ sơ có chiều gửi. -->
				<el-tab-pane v-if="showSendTab" :label="$t('lz.label.sharedataMapping.previewTab')" name="preview">
					<SendPreview ref="sendPreviewRef" :shape-json="state.shapeJson" :shape-error="state.shapeError" :datatype-id="state.editForm.datatypeId"
						:partner-code="currentPartnerCode" :packet-fields="state.packetFields" :code-set-list="state.codeSetList" />
				</el-tab-pane>
				<!-- Tab nhận về: payload đối tác gửi đến đi qua bộ khung thành bản ghi nội bộ. Chỉ hiện khi hồ sơ có chiều nhận. -->
				<el-tab-pane v-if="showReceiveTab" :label="$t('lz.label.sharedataMapping.receiveTab')" name="receive">
					<ReceivePreview ref="receivePreviewRef" :shape-json="state.shapeJson" :partner-json="state.partnerJson"
						:packet-fields="state.packetFields" :code-set-list="state.codeSetList" />
				</el-tab-pane>
				</el-tabs>
			</el-form>

		<!-- Form luật của MỘT lá: dùng chung một hộp thoại cho cả cây, không mở popover theo từng dòng
		     (mỗi dòng một popover làm el-tree hỏng patch khi cây vẽ lại). -->
		<el-dialog v-model="leafFormVisible" width="min(460px, 95vw)" append-to-body draggable
			:close-on-click-modal="!state.isProcessing"
			destroy-on-close :before-close="handleLeafBeforeClose">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit />
					</el-icon>
					<span> {{ leafFormTitle }} </span>
				</div>
			</template>

			<template v-if="editingCfg">
			<el-form label-position="top" :show-message="false" class="mp-leaf-form"
				@submit.prevent="handleOkLeaf">
				<!-- Khoá không lấy từ gói tin (header, thông tin meta...) thì giá trị gửi đi do
				     người dùng tự gõ, không khoá cứng theo JSON mẫu đã dán. -->
				<template v-if="!editingCfg.fieldKey">
					<el-alert type="info" :closable="false" show-icon class="mb15"
						:title="$t('lz.label.sharedataMapping.constantLeafAlertTitle')"
						:description="$t('lz.label.sharedataMapping.constantLeafAlertDesc')" />
					<el-form-item :label="$t('lz.label.sharedataMapping.valueSent')">
						<el-input v-model="editingCfg.constValue" size="small"
							:placeholder="$t('lz.label.sharedataMapping.valueSentPlaceholder')"
							@change="refreshShape" />
						<div class="rs-note">
							<i18n-t keypath="lz.label.sharedataMapping.valueSentTypingHint" tag="span">
								<template #example1><code>12.5</code></template>
								<template #example2><code>true</code></template>
							</i18n-t>
						</div>
					</el-form-item>

					<div class="mp-disabled-section mt10 mb10">
						<el-divider content-position="left">
							{{ $t('lz.label.sharedataMapping.transformRulesDisabledTitle') }}
						</el-divider>
						<div class="rs-note mb10">
							{{ $t('lz.label.sharedataMapping.transformRulesDisabledHint') }}
						</div>
						<el-form-item :label="$t('lz.label.sharedataMapping.codeSet')">
							<el-select disabled size="small" :placeholder="$t('lz.label.sharedataMapping.codeSetPlaceholder')" class="w100" />
						</el-form-item>
						<el-form-item :label="$t('lz.label.sharedataMapping.dataType')">
							<el-select disabled size="small" :placeholder="$t('lz.label.sharedataMapping.dataTypePlaceholder')" class="w100" />
						</el-form-item>
						<el-form-item v-if="showPartnerDefault" :label="$t('lz.label.sharedataMapping.defaultPartnerValue')">
							<el-input disabled size="small" :placeholder="$t('lz.label.sharedataMapping.defaultPartnerValuePlaceholder')" />
						</el-form-item>
						<el-form-item v-if="showSourceDefault" :label="$t('lz.label.sharedataMapping.defaultSourceValue')">
							<el-input disabled size="small" :placeholder="$t('lz.label.sharedataMapping.defaultSourceValuePlaceholder')" />
						</el-form-item>
					</div>
				</template>

				<template v-if="editingCfg.fieldKey">
					<el-divider content-position="left">
						{{ $t('lz.label.sharedataMapping.ruleForField', { field: editingCfg.fieldKey }) }}
					</el-divider>
					<el-form-item :label="$t('lz.label.sharedataMapping.codeSet')">
						<div class="mp-codeset">
							<el-select v-model="editingCfg.codeSet"
								size="small" filterable clearable :placeholder="$t('lz.label.sharedataMapping.codeSetPlaceholder')"
								class="mp-codeset-select" @change="handleCodeSetChange(state.editingPath)">
								<el-option v-for="c in state.codeSetList" :key="c.code"
									:label="c.name" :value="c.code" />
							</el-select>
							<el-tooltip :content="$t('lz.label.sharedataMapping.viewEditCodeSet')" placement="top"
								:auto-close="1000">
								<el-button icon="ele-Edit" size="small" text type="primary"
									:disabled="!selectedCodeSet" @click="handleEditCodeSet" />
							</el-tooltip>
							<el-tooltip :content="$t('lz.label.sharedataMapping.addNewCodeSet')" placement="top"
								:auto-close="1000">
								<el-button icon="ele-Plus" size="small" text type="primary"
									@click="handleAddCodeSet" />
							</el-tooltip>
						</div>
					</el-form-item>
					<el-form-item :label="$t('lz.label.sharedataMapping.dataType')">
						<el-select v-model="editingCfg.targetType"
							size="small" :placeholder="$t('lz.label.sharedataMapping.dataTypePlaceholder')"
							@change="handleTargetTypeChange(state.editingPath)">
							<el-option v-for="t in getConfigDataByCode(BaseConfigTypeEnum.SharedataValueType)" :key="t.code"
								:label="t.name" :value="t.code" />
						</el-select>
					</el-form-item>

					<!-- Định dạng chỉ hiện khi kiểu ra cần nó: ngày hoặc số. -->
					<el-form-item v-if="isDateType(editingCfg.targetType)" :label="$t('lz.label.sharedataMapping.dateFormat')">
						<el-input v-model="editingCfg.dateFormat" size="small"
							:placeholder="$t('lz.label.sharedataMapping.dateFormatPlaceholder')" @change="refreshShape" />
					</el-form-item>
					<el-form-item v-else-if="isNumberType(editingCfg.targetType)" :label="$t('lz.label.sharedataMapping.numberFormat')">
						<el-input v-model="editingCfg.numberFormat" size="small"
							:placeholder="$t('lz.label.sharedataMapping.numberFormatPlaceholder')" @change="refreshShape" />
					</el-form-item>

					<!-- Mặc định tách theo chiều: gửi điền giá trị phía đối tác, nhận điền giá trị phía nội bộ.
					     Bộ mã đã có dòng mặc định riêng cho cả hai chiều nên chọn bộ mã là khoá hai ô này. -->
					<el-form-item v-if="showPartnerDefault" :label="$t('lz.label.sharedataMapping.defaultPartnerValue')">
						<el-input v-model="editingCfg.defaultPartnerValue" size="small" :disabled="hasCodeSet(state.editingPath)"
							:placeholder="hasCodeSet(state.editingPath)
								? $t('lz.label.sharedataMapping.defaultValueDisabledByCodeSet')
								: $t('lz.label.sharedataMapping.defaultPartnerValuePlaceholder')" />
						<div v-if="leafDefaultErrors(editingCfg).partner" class="mp-err">{{ leafDefaultErrors(editingCfg).partner }}</div>
						<div v-else class="rs-note">{{ $t('lz.label.sharedataMapping.defaultPartnerValueOutboundHint') }}</div>
					</el-form-item>
					<el-form-item v-if="showSourceDefault" :label="$t('lz.label.sharedataMapping.defaultSourceValue')">
						<el-input v-model="editingCfg.defaultSourceValue" size="small" :disabled="hasCodeSet(state.editingPath)"
							:placeholder="hasCodeSet(state.editingPath)
								? $t('lz.label.sharedataMapping.defaultValueDisabledByCodeSet')
								: $t('lz.label.sharedataMapping.defaultSourceValuePlaceholder')" />
						<div v-if="leafDefaultErrors(editingCfg).source" class="mp-err">{{ leafDefaultErrors(editingCfg).source }}</div>
						<div v-else class="rs-note">{{ $t('lz.label.sharedataMapping.defaultSourceValueInboundHint') }}</div>
					</el-form-item>
					<div v-if="hasCodeSet(state.editingPath)" class="rs-note">
						{{ $t('lz.label.sharedataMapping.defaultValueDisabledByCodeSetHint') }}
					</div>
					<div class="rs-note">{{ $t('lz.label.sharedataMapping.ruleOnlyForThisKeyHint', { field: editingCfg.fieldKey }) }}</div>
				</template>

				<div class="mp-form-footer">
					<el-button size="small" @click="handleCancelLeaf">{{ $t('lz.button.base.close') }}</el-button>
					<el-button size="small" type="primary" @click="handleOkLeaf">
						OK
					</el-button>
				</div>
			</el-form>
			</template>
		</el-dialog>

			<template #footer>
				<span class="dialog-footer">
					<el-button @click="cancel" :disabled="state.isProcessing">{{ $t('lz.button.base.cancel') }}</el-button>
					<el-button type="primary" @click="submit" :loading="state.isProcessing" :disabled="state.isProcessing || state.loading || state.loadFailed || state.loadingFields || state.loadingSubscriptions">{{ $t('lz.button.base.confirm') }}</el-button>
				</span>
			</template>
		</el-dialog>

		<EditCodeSet ref="editCodeSetRef" :title="state.codeSetTitle" :operateType="state.codeSetOperateType"
			@handleQuery="reloadCodeSets" />
	</div>
</template>

<script lang="ts" setup name="eshEditMapping">
import { computed, defineAsyncComponent, nextTick, onBeforeUnmount, reactive, ref, watch } from 'vue';
import { getAPI } from '/@/utils/axios-utils';
import { ShareDataCodeSetApi, ShareDataMappingApi, ShareDataPacketApi, ShareDataSubscriptionApi } from '/@/api-services/shareData';
import { ShareDataUpdateMappingInput, ShareDataSubscriptionOutput } from '/@/api-services/shareData';
import { fetchPacketFields, PacketFieldMeta } from '/@/utils/sharedata/packetFields';
import { OperateType } from '/@/types/constants';
import { ElMessage } from 'element-plus';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum, EntityFieldLength, SharedataDirectionEnum, SharedataSubStateEnum } from '/@/types/enums/baseEnum';

import { useI18n } from 'vue-i18n';
import { codeNameLabel } from '/@/utils/sharedata/labelHelper';
const { t: $t } = useI18n();

const EditCodeSet = defineAsyncComponent(() => import('/@/views/sharedata/codeSet/component/editCodeSet.vue'));
const SendPreview = defineAsyncComponent(() => import('./sendPreview.vue'));
const ReceivePreview = defineAsyncComponent(() => import('./receivePreview.vue'));

const getConfigDataByCode = useBaseConfig().getConfigDataByCode;

let editingLeafHadConfigBeforeOpen = false;
let editingLeafInitialSnapshot = '';
let currentEditingPath = '';

const handleLeafBeforeClose = (done: () => void) => {
	const targetPath = state.editingPath || currentEditingPath;
	const wrappedDone = () => {
		if (targetPath) {
			if (!editingLeafHadConfigBeforeOpen) {
				delete state.nodeConfig[targetPath];
			} else if (editingLeafInitialSnapshot) {
				try {
					state.nodeConfig[targetPath] = JSON.parse(editingLeafInitialSnapshot);
				} catch {
					// ignore
				}
			}
		}
		done();
		refreshShape();
	};
	wrappedDone();
};

const handleCancelLeaf = () => {
	handleLeafBeforeClose(() => {
		closeLeafForm();
	});
};

const handleOkLeaf = () => {
	const targetPath = state.editingPath || currentEditingPath;
	if (targetPath && !editingLeafHadConfigBeforeOpen && editingLeafInitialSnapshot) {
		const currentJson = JSON.stringify(state.nodeConfig[targetPath]);
		if (currentJson === editingLeafInitialSnapshot) {
			delete state.nodeConfig[targetPath];
		}
	}
	closeLeafForm();
};

const DIRECTION_LIST = computed(() => [
	{ code: 0, name: $t('lz.label.sharedataMapping.outbound') },
	{ code: 1, name: $t('lz.label.sharedataMapping.inbound') },
	{ code: 2, name: $t('lz.label.sharedataMapping.both') },
]);

/** Ba tab chiều cấp trên của hộp thoại — dùng chuỗi để tránh bẫy Outbound = 0 bị coi là falsy. */
const DIRECTION_TABS = computed(() => [
	{ name: 'outbound', code: 0, label: $t('lz.label.sharedataMapping.outbound') },
	{ name: 'inbound', code: 1, label: $t('lz.label.sharedataMapping.inbound') },
	{ name: 'both', code: 2, label: $t('lz.label.sharedataMapping.both') },
]);

const directionTab = computed<string>({
	get: () => {
		const dir = Number(state.editForm.direction ?? 0);
		if (dir === 1) return 'inbound';
		if (dir === 2) return 'both';
		return 'outbound';
	},
	set: (v) => {
		state.editForm.direction = (v === 'inbound' ? 1 : v === 'both' ? 2 : 0) as any;
	},
});

const FORMAT_LIST = computed(() => [
	{ code: 0, name: $t('lz.label.sharedataMapping.dataFormat') },
	{ code: 1, name: $t('lz.label.sharedataMapping.fileFormat') },
]);


/** Cấu hình cây giữ nguyên một tham chiếu: tạo object mới mỗi lần vẽ sẽ làm el-tree dựng lại store. */
const TREE_PROPS = { children: 'children', label: 'key' };

const MARK_FIELD = '$field';
const MARK_EXTEND = '$extend';
/** Giá trị hệ thống: {"$meta":"Now"} — service thấy ký hiệu này là tự tính, không tra bảng dữ liệu. */
const MARK_META = '$meta';
/** Giá trị đã biết ngay lúc lưu hồ sơ, đi kèm $meta: {"$meta":"PacketCode","$value":"101_commonData"}. */
const MARK_VALUE = '$value';

/** Một lá trong khuôn — nơi có thể gắn giá trị. */
interface PartnerLeaf {
	/** Đường dẫn trong khuôn, chỉ số mảng thu về '[]'. CHỈ dùng trong bộ nhớ, không lưu DB. */
	path: string;
	/** Tên khoá tại lá — dùng để tự khớp với fieldKey của gói tin. */
	name: string;
	/** Giá trị mẫu đối tác ghi tại lá, dạng chữ để hiển thị. */
	sample: string;
	/** Giá trị gốc trong khuôn, dạng JSON — dùng làm hằng số mặc định khi lá không ánh xạ trường. */
	raw: string;
}

/** Một nút trong cây khuôn — chỉ để vẽ lại cấu trúc trên giao diện. */
interface TemplateNode {
	key: string;
	path: string;
	kind: 'object' | 'array' | 'leaf';
	sample?: string;
	children?: TemplateNode[];
}

/**
 * Thiết lập của một lá trong khuôn — tất cả trong một chỗ, khoá tra là đường dẫn.
 *
 * Luật quy đổi gắn theo VỊ TRÍ, không theo trường: cùng một trường dùng ở hai khoá khác nhau thì
 * mỗi khoá khai luật riêng. Nhờ vậy đối tác đòi cùng giá trị ở hai chỗ với hai đơn vị khác nhau
 * vẫn làm được — điều mô hình theo trường không diễn đạt nổi.
 */
interface LeafConfig {
	fieldKey: string;
	constValue: string;
	targetType: string;
	codeSet?: string;
	expression: string;
	/** Giá trị gửi cho đối tác khi dữ liệu nội bộ rỗng — chỉ dùng ở chiều GỬI. */
	defaultPartnerValue: string;
	/** Giá trị lưu nội bộ khi đối tác không gửi giá trị — chỉ dùng ở chiều NHẬN. */
	defaultSourceValue: string;
	/** Định dạng ngày khi kiểu ra là ngày — VD dd/MM/yyyy HH:mm:ss. */
	dateFormat: string;
	/** Định dạng số khi kiểu ra là số — VD 0.#. */
	numberFormat: string;
}

interface Props {
	operateType: OperateType;
	title: string;
	partnerList: any[];
}

const props = defineProps<Props>();
const emits = defineEmits(['handleQuery']);

const editFormRef = ref();
const dialogRef = ref();
const editCodeSetRef = ref<any>();
const sendPreviewRef = ref<any>();
const receivePreviewRef = ref<any>();

const state = reactive({
	isShowDialog: false,
	editForm: {} as ShareDataUpdateMappingInput,
	loading: false,
	loadFailed: false,
	isProcessing: false,
	codeError: '',
	/** Định danh bản gốc khi sao chép: giữ nguyên đích thì mã = mã gốc + _copy. */
	copySource: { code: '', partnerId: '', datatypeId: '', direction: undefined as number | undefined },
	loadingFields: false,
	codeSetList: [] as any,
	codeSetTitle: '',
	codeSetOperateType: 'add' as 'add' | 'edit',
	packetList: [] as any,
	/**
	 * Đăng ký chia sẻ của đối tác đang chọn, đã lọc còn trạng thái Đang chạy / Tạm dừng.
	 * Đây là nguồn quyết định gói tin nào được ánh xạ — xem availablePacketList.
	 */
	partnerSubscriptions: [] as ShareDataSubscriptionOutput[],
	loadingSubscriptions: false,
	packetFields: [] as PacketFieldMeta[],
	packetFieldMap: {} as Record<string, PacketFieldMeta>,
	partnerJson: '',
	jsonError: '',
	paneHint: $t('lz.label.sharedataMapping.selectPacketFirstForComparison'),
	/** Cây khuôn để vẽ cấu trúc. */
	templateTree: [] as TemplateNode[],
	/** Tăng mỗi lần phân tích lại JSON — dùng làm key của el-tree để cây dựng lại sạch, không vá cây cũ. */
	treeVersion: 0,
	/** Mọi lá của khuôn, theo thứ tự xuất hiện. */
	leaves: [] as PartnerLeaf[],
	/** Thiết lập theo LÁ, khoá tra là đường dẫn. Chỉ tồn tại trong bộ nhớ. */
	nodeConfig: {} as Record<string, LeafConfig>,
	/** Bộ khung đầu ra đã dựng — lưu vào TargetShapeJson. */
	shapeJson: '',
	shapeError: '',
	/** Số lá đã chọn được trường. */
	mappedCount: 0,
	/** Đường dẫn của lá đang mở form thiết lập. Rỗng = không mở form nào. */
	editingPath: '',
	/** Tab đang xem trong hộp thoại. */
	activeTab: 'info',
});

/* ---- Mã hồ sơ sinh tự động ---- */

/** Hậu tố chiều trong mã hồ sơ — Outbound = 0, Inbound = 1, Both = 2. */
const DIRECTION_CODE: Record<number, string> = { 0: 'OUT', 1: 'IN', 2: 'BOTH' };

/** Chuẩn hoá một mảnh mã: bỏ dấu, chỉ giữ A-Z và 0-9 để mã luôn an toàn khi so khớp. */
const normalizeCodePart = (value?: string | null) =>
	String(value ?? '')
		.normalize('NFD')
		.replace(/[\u0300-\u036f]/g, '')
		// 'Đ' / 'đ' không tách được dấu bằng NFD nên phải quy đổi tay, nếu không sẽ bị xoá mất chữ
		// ('Đối tác Đường bộ' thành 'OITACUONGBO').
		.replace(/[Đđ]/g, 'D')
		.toUpperCase()
		.replace(/[^A-Z0-9]+/g, '');

/**
 * Mã hồ sơ = ĐỐI TÁC _ GÓI TIN _ CHIỀU.
 * Phải có cả chiều: một cặp (đối tác × gói tin) được phép có hồ sơ riêng cho từng chiều,
 * nếu mã chỉ gồm hai mảnh đầu thì hai hồ sơ đó trùng mã nhau.
 * Đối tác là BẮT BUỘC (không còn hồ sơ dùng chung): worker luôn lọc hồ sơ theo đúng đối tác của
 * đăng ký, nên hồ sơ không gắn đối tác sẽ không bao giờ được dùng.
 */
const buildMappingCode = (): string => {
	const partner = (props.partnerList ?? []).find((p: any) => p.id === state.editForm.partnerId);
	const partnerPart = normalizeCodePart(partner?.code ?? partner?.name);
	const packetPart = normalizeCodePart(state.packetList.find((p: any) => p.id === state.editForm.datatypeId)?.code) || '';
	const directionPart = DIRECTION_CODE[Number(state.editForm.direction)] ?? '';
	return [partnerPart, packetPart, directionPart].filter(Boolean).join('_');
};

/**
 * Sinh lại mã mỗi khi đổi đối tác / gói tin / chiều.
 * Bỏ qua khi đang SỬA: backend không cho đổi mã của hồ sơ đã tồn tại.
 * Chưa chọn gói tin thì để mã rỗng để validation nhắc người dùng chọn.
 */
const syncMappingCode = () => {
	if (props.operateType === 'edit') return;
	if (props.operateType === 'copy') {
		// openDialog tự sinh mã bản sao sau khi nạp xong chi tiết bản gốc.
		if (state.loading) return;
		copyCodeRequest++;
		state.codeError = '';
		state.editForm.code = state.editForm.datatypeId ? copyBaseCode() : '';
		if (!state.editForm.code) return;
		const request = copyCodeRequest;
		resolveCopyCode().catch(() => {
			if (request === copyCodeRequest) state.codeError = 'lz.validation.sharedataMapping.codeCheckFailed';
		});
		return;
	}
	state.codeError = '';
	state.editForm.code = state.editForm.datatypeId ? buildMappingCode() : '';
};

/** Lần sinh mã bản sao gần nhất; kết quả kiểm tra trùng của lần cũ bị bỏ qua. */
let copyCodeRequest = 0;

/** Giữ nguyên đối tác/gói tin/chiều của bản gốc thì thêm _copy vào mã gốc; đổi đích thì sinh mã theo đích mới. */
const copyBaseCode = (): string => {
	const source = state.copySource;
	const sameDestination = state.editForm.partnerId === source.partnerId
		&& state.editForm.datatypeId === source.datatypeId
		&& Number(state.editForm.direction) === Number(source.direction);
	return sameDestination && source.code ? `${source.code}_copy` : buildMappingCode();
};

watch(() => [state.editForm.partnerId, state.editForm.datatypeId, state.editForm.direction], syncMappingCode);

/**
 * Đảm bảo thanh tiêu đề dialog (.el-dialog__header) luôn nằm trọn vẹn trong màn hình (viewport).
 * Khi người dùng kéo modal rồi chuyển tab hoặc phân tích JSON khiến nội dung dài ra:
 * Nhờ CSS định vị top cố định, dialog chỉ nở dài xuống dưới mà không bị đẩy ngược lên.
 * Nếu toạ độ rect.top < 0 do người dùng kéo quá trớn lên trên đỉnh màn hình, gọi resetPosition()
 * của Element Plus để đưa modal về toạ độ gốc an toàn mà không làm lệch (desync) trạng thái kéo của useDraggable.
 */
const ensureDialogInViewport = () => {
	nextTick(() => {
		const dialogEl = document.querySelector('.mp-dialog') as HTMLElement;
		if (!dialogEl) return;
		const rect = dialogEl.getBoundingClientRect();
		if (rect.top < 0) {
			dialogRef.value?.resetPosition?.();
		}
	});
};

/* ---- Kéo dài hộp thoại ----
 *
 * Vùng nội dung (el-form) có tay nắm resize dọc ở góc dưới phải. Cây ánh xạ dài ra theo chiều cao
 * vùng nội dung, nên kéo hộp thoại ra là thấy thêm dòng của cây thay vì chỉ thêm khoảng trống.
 */
const TREE_MIN_HEIGHT = 300;
/** Phần chiếm chỗ phía trên/dưới cây trong tab Ánh xạ: thanh tab, tiêu đề, chú thích icon, khối trường chưa dùng. */
const TREE_RESERVED_HEIGHT = 230;
const formBodyHeight = ref(0);
const treeMaxHeight = computed(() => Math.max(TREE_MIN_HEIGHT, formBodyHeight.value - TREE_RESERVED_HEIGHT));
/** Phần chiếm chỗ quanh khung trống khi chưa phân tích JSON: thanh tab, tiêu đề vùng, lề dưới. */
const EMPTY_RESERVED_HEIGHT = 125;
/** Khung trống cao theo vùng nội dung để hộp thoại không bị hụt nửa dưới khi chưa có cây. */
const emptyPaneHeight = computed(() => Math.max(TREE_MIN_HEIGHT, formBodyHeight.value - EMPTY_RESERVED_HEIGHT));
const TREE_ROW_HEIGHT = 34;

const countTemplateNodes = (nodes: TemplateNode[]): number => {
	let total = 0;
	for (const n of nodes) {
		total += 1;
		if (n.children?.length) {
			total += countTemplateNodes(n.children);
		}
	}
	return total;
};

/** Chỉ nở rộng ở tab Ánh xạ khi đã có cây; các tab còn lại (kể cả lúc vừa mở) giữ cỡ chuẩn để sửa và tạo mới giống nhau. */
const dialogWidth = computed(() =>
	state.activeTab === 'map' && Boolean(state.editForm.datatypeId) && state.templateTree.length > 0
		? 'min(1400px, 95vw)'
		: 'min(960px, 95vw)'
);

/** Mở hồ sơ khác thì trả vùng nội dung về chiều cao tự nhiên, không giữ chiều cao đã kéo lần trước. */
const resetFormBodyHeight = () => {
	const el = editFormRef.value?.$el as HTMLElement | undefined;
	if (el) el.style.height = '';
};

watch(
	() => state.activeTab,
	() => {
		nextTick(() => {
			if (editFormRef.value?.$el) {
				editFormRef.value.$el.scrollTop = 0;
			}
			ensureDialogInViewport();
		});
	}
);

/**
 * Đổi chiều thì bỏ giá trị mặc định của chiều không còn dùng. Form ẩn ô đi nhưng nếu không
 * xoá, giá trị cũ vẫn được ghi vào bộ khung mà người dùng không nhìn thấy.
 */
/** Đổi đối tác thì mã đối tác trong $value đổi theo, phải dựng lại bộ khung. */
watch(() => state.editForm.partnerId, () => refreshShape());

watch(
	() => state.editForm.direction,
	(direction) => {
		const dir = Number(direction);
		if (dir !== SharedataDirectionEnum.Outbound && dir !== SharedataDirectionEnum.Inbound) return;

		for (const cfg of Object.values(state.nodeConfig)) {
			if (dir === SharedataDirectionEnum.Outbound) cfg.defaultSourceValue = '';
			else cfg.defaultPartnerValue = '';
		}
		// Chuyển sang chiều Nhận thì tab gửi đi bị ẩn (v-if) nên mẫu CSDL và payload đang xem tự mất theo.
		refreshShape();
	}
);


const fieldMeta = (fieldKey?: string | null) => state.packetFieldMap[fieldKey ?? ''] ?? state.packetFields.find(f => f.aliasFieldKey?.toLowerCase() === fieldKey?.toLowerCase());

/**
 * Giá trị hệ thống tự sinh lúc gửi — không nằm trong bảng dữ liệu của gói tin.
 * Service đọc đúng 4 khoá này trong $field rồi tự điền giá trị.
 */
const BUILTIN_FIELDS = [
	{ key: 'Now', labelKey: 'lz.label.sharedataMapping.builtinNow' },
	{ key: 'Serial', labelKey: 'lz.label.sharedataMapping.builtinSerial' },
	{ key: 'PacketCode', labelKey: 'lz.label.sharedataMapping.builtinPacketCode' },
	{ key: 'PartnerCode', labelKey: 'lz.label.sharedataMapping.builtinPartnerCode' },
];

const builtinField = (fieldKey?: string | null) => BUILTIN_FIELDS.find((b) => b.key === (fieldKey ?? ''));



/**
 * Giá trị của khoá Meta mà FE biết ngay lúc lưu: mã gói tin và mã đối tác của chính hồ sơ này.
 * Now và Serial phải đợi lúc gửi nên FE không điền, service tự sinh.
 */
const metaValueOf = (key?: string | null): string => {
	if (key === 'PacketCode') return state.packetList.find((p: any) => p.id === state.editForm.datatypeId)?.code ?? '';
	if (key === 'PartnerCode') {
		const partner = (props.partnerList ?? []).find((p: any) => p.id === state.editForm.partnerId);
		return partner?.code ?? '';
	}
	return '';
};

const currentPartnerCode = computed(() => {
	const partner = (props.partnerList ?? []).find((p: any) => p.id === state.editForm.partnerId);
	return partner?.code ?? '';
});

/** Gộp thông tin trường vào một dòng tooltip cho gọn cây. */
const fieldHint = (fieldKey?: string | null) => {
	const builtin = builtinField(fieldKey);
	if (builtin) return `${$t(builtin.labelKey)} · ${$t('lz.label.sharedataMapping.builtinFieldGroup')}`;

	const f = fieldMeta(fieldKey);
	if (!f) return '';
	const parts = [f.name, f.type];
	if (f.isRequired === true) parts.push($t('lz.label.sharedataMapping.requiredLower'));
	return parts.filter((p) => (p ?? '') !== '').join(' · ');
};

/**
 * Thiết lập của một lá
 */
const cfgOf = (path: string): LeafConfig => {
	let cfg = state.nodeConfig[path];
	if (!cfg) {
		cfg = { fieldKey: '', constValue: '', targetType: '', codeSet: undefined, expression: '', defaultPartnerValue: '', defaultSourceValue: '', dateFormat: '', numberFormat: '' };
		state.nodeConfig[path] = cfg;
	}
	return cfg;
};

/** Lá có khai luật quy đổi nào không — quyết định có ghi $extend vào bộ khung hay không. */
const hasRule = (cfg?: LeafConfig) =>
	!!cfg && [cfg.targetType, cfg.codeSet, cfg.expression, cfg.defaultPartnerValue, cfg.defaultSourceValue, cfg.dateFormat, cfg.numberFormat]
		.some((v) => (v ?? '').toString().trim() !== '');

/**
 * Chiều của hồ sơ quyết định ô mặc định nào có nghĩa:
 *  - Gửi  : chỉ cần giá trị đối tác mặc định.
 *  - Nhận : chỉ cần giá trị nội bộ mặc định.
 *  - Both : cần cả hai, vì hai chiều điền vào hai phía khác nhau.
 */
const showPartnerDefault = computed(() => Number(state.editForm.direction) !== SharedataDirectionEnum.Inbound);
const showSourceDefault = computed(() => Number(state.editForm.direction) !== SharedataDirectionEnum.Outbound);

/** Tab dữ liệu theo chiều: Gửi chỉ có tab gửi đi, Nhận chỉ có tab nhận về, Hai chiều có cả hai. */
const showSendTab = computed(() => Number(state.editForm.direction) !== SharedataDirectionEnum.Inbound);
const showReceiveTab = computed(() => Number(state.editForm.direction) !== SharedataDirectionEnum.Outbound);

/** Đổi chiều làm ẩn tab đang mở thì quay về tab Ánh xạ, không để hộp thoại trống nội dung. */
watch([showSendTab, showReceiveTab], () => {
	if ((state.activeTab === 'preview' && !showSendTab.value) || (state.activeTab === 'receive' && !showReceiveTab.value)) state.activeTab = 'map';
});

/** Lá có khai giá trị mặc định nào không — dùng cho icon ở cột phải. */
const defaultValueOf = (cfg?: LeafConfig) => {
	if (!cfg || !cfg.fieldKey) return '';
	const parts: string[] = [];
	if (showPartnerDefault.value && cfg.defaultPartnerValue.trim()) parts.push(`${$t('lz.label.sharedataMapping.defaultPartnerValue')}: ${cfg.defaultPartnerValue.trim()}`);
	if (showSourceDefault.value && cfg.defaultSourceValue.trim()) parts.push(`${$t('lz.label.sharedataMapping.defaultSourceValue')}: ${cfg.defaultSourceValue.trim()}`);
	return parts.join(' · ');
};

/**
 * Kiểu ra lấy từ danh mục sharedata_value_type nên mã do người quản trị đặt.
 * So khớp không phân biệt hoa thường để đổi cách viết mã không phải sửa code.
 */
const isDateType = (type?: string | null) => (type ?? '').trim().toLowerCase() === 'datetime';

const NUMBER_TYPE_CODES = ['int', 'long', 'bigint', 'number', 'decimal', 'double', 'float'];
const isNumberType = (type?: string | null) => NUMBER_TYPE_CODES.includes((type ?? '').trim().toLowerCase());

/** Miền giá trị của kiểu số nguyên, so bằng BigInt để không mất chính xác với long. */
const INTEGER_RANGES: Record<string, [string, string]> = {
	int: ['-2147483648', '2147483647'],
	long: ['-9223372036854775808', '9223372036854775807'],
	bigint: ['-9223372036854775808', '9223372036854775807'],
};
const GUID_PATTERN = /^[{(]?[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}[)}]?$/i;

/** Ngày theo định dạng khai ở lá (yyyy, MM, dd, HH, mm, ss, fff); không khai định dạng thì phải đọc được như ngày. */
const isDateValue = (text: string, dateFormat: string) => {
	if (!dateFormat || /^[oO]$/.test(dateFormat)) return !Number.isNaN(Date.parse(text));
	const tokens: Record<string, string> = { yyyy: '\\d{4}', fff: '\\d{3}', MM: '\\d{2}', dd: '\\d{2}', HH: '\\d{2}', mm: '\\d{2}', ss: '\\d{2}' };
	const pattern = dateFormat.split(/(yyyy|fff|MM|dd|HH|mm|ss)/).map((part) => tokens[part] ?? part.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')).join('');
	return new RegExp(`^${pattern}$`).test(text);
};

/**
 * Giá trị mặc định phải đọc được theo kiểu dữ liệu của lá (kiểu ra, không khai thì lấy kiểu trường gói tin).
 * Trả về thông báo lỗi; bỏ trống hoặc kiểu chuỗi thì hợp lệ.
 */
const defaultValueError = (cfg: LeafConfig, value: string) => {
	const text = (value ?? '').trim();
	const type = (cfg.targetType.trim() || fieldMeta(cfg.fieldKey)?.type || '').trim();
	const code = type.toLowerCase();
	if (!text || !code) return '';

	let valid = true;
	if (INTEGER_RANGES[code]) {
		const [min, max] = INTEGER_RANGES[code];
		valid = /^[+-]?\d+$/.test(text) && BigInt(text) >= BigInt(min) && BigInt(text) <= BigInt(max);
	} else if (isNumberType(code)) {
		// Có định dạng số thì phía đối tác có thể kèm dấu phân cách hàng nghìn.
		const number = cfg.numberFormat.trim() ? text.replace(/,/g, '') : text;
		valid = /^[+-]?(\d+\.?\d*|\.\d+)([eE][+-]?\d+)?$/.test(number) && Number.isFinite(Number(number));
	} else if (code === 'bool' || code === 'boolean') {
		valid = /^(true|false|1|0)$/i.test(text);
	} else if (code === 'guid' || code === 'uuid') {
		valid = GUID_PATTERN.test(text);
	} else if (isDateType(code)) {
		valid = isDateValue(text, cfg.dateFormat.trim());
	}
	return valid ? '' : $t('lz.validation.sharedataMapping.defaultValueTypeMismatch', { value: text, type });
};

/** Lỗi giá trị mặc định của một lá, chỉ xét ô đang hiện theo chiều hồ sơ; bộ mã lo mặc định thì bỏ qua. */
const leafDefaultErrors = (cfg?: LeafConfig | null) => {
	if (!cfg || !cfg.fieldKey || builtinField(cfg.fieldKey) || (cfg.codeSet ?? '').trim()) return { partner: '', source: '' };
	return {
		partner: showPartnerDefault.value ? defaultValueError(cfg, cfg.defaultPartnerValue) : '',
		source: showSourceDefault.value ? defaultValueError(cfg, cfg.defaultSourceValue) : '',
	};
};

/** Thông báo chặn lưu/xem trước khi còn lá có giá trị mặc định sai kiểu; rỗng nếu hợp lệ. */
const invalidDefaultMessage = () => {
	const fields = state.leaves
		.filter((leaf) => { const errors = leafDefaultErrors(state.nodeConfig[leaf.path]); return !!(errors.partner || errors.source); })
		.map((leaf) => leaf.path);
	return fields.length ? $t('lz.validation.sharedataMapping.invalidDefaultValues', { fields: fields.join(', ') }) : '';
};

/** Lá đã chọn bộ mã — bộ mã tự lo giá trị mặc định nên khoá ô mặc định của lá. */
const hasCodeSet = (path: string) => (state.nodeConfig[path]?.codeSet ?? '').trim() !== '';

/** Chọn bộ mã thì bỏ giá trị mặc định của lá, để hai nơi không khai mặc định chồng nhau. */
const handleCodeSetChange = (path: string) => {
	const cfg = cfgOf(path);
	if ((cfg.codeSet ?? '').trim()) {
		cfg.defaultPartnerValue = '';
		cfg.defaultSourceValue = '';
	}
	refreshShape();
};

/** Đổi kiểu ra thì bỏ định dạng của kiểu cũ, tránh lưu định dạng không còn dùng vào bộ khung. */
const handleTargetTypeChange = (path: string) => {
	const cfg = cfgOf(path);
	if (!isDateType(cfg.targetType)) cfg.dateFormat = '';
	if (!isNumberType(cfg.targetType)) cfg.numberFormat = '';
	refreshShape();
};

/** Cảnh báo của một lá — hiện icon cảnh báo ở cột phải. */
const leafWarning = (path: string) => {
	const cfg = state.nodeConfig[path];
	// Giá trị hệ thống luôn có giá trị lúc gửi nên không cảnh báo.
	if (builtinField(cfg?.fieldKey)) return '';
	if (cfg?.fieldKey) {
		const defaultErrors = leafDefaultErrors(cfg);
		if (defaultErrors.partner || defaultErrors.source) return defaultErrors.partner || defaultErrors.source;
		const meta = fieldMeta(cfg.fieldKey);
		return meta?.isRequired === true && defaultValueOf(cfg) === '' && (cfg.codeSet ?? '').trim() === ''
			? $t('lz.label.sharedataMapping.iconWarningRequired', { field: cfg.fieldKey })
			: '';
	}
	// Chưa chọn trường mà cũng không tự gõ giá trị thì khoá này gửi đi null.
	return (cfg?.constValue ?? '').trim() === '' ? $t('lz.label.sharedataMapping.iconWarning') : '';
};

/** Lá có thiết lập riêng nào không — để tô màu nút "…". */
/**
 * Lá có thiết lập riêng nào không — quyết định cột phải hiện icon luật hay icon bánh răng.
 * Lá đã chọn trường thì hằng số không được tính: nó không đi vào bộ khung, và hồ sơ cũ
 * có thể còn sót hằng số tự điền, tính vào sẽ làm lá không hiện icon nào.
 */
const hasExtra = (path: string) => {
	const cfg = state.nodeConfig[path];
	if (!cfg) return false;
	if (cfg.fieldKey) return hasRule(cfg);
	return cfg.constValue.trim() !== '';
};
/** Lá đang lấy một trong 4 giá trị hệ thống — BE/service tự tính, không khai luật. */
const isBuiltinLeaf = (path: string) => Boolean(builtinField(state.nodeConfig[path]?.fieldKey));

const toggleLeafForm = (path: string) => {
	// Giá trị hệ thống không có gì để cấu hình nên không mở form.
	if (isBuiltinLeaf(path)) return;
	if (state.editingPath === path) {
		closeLeafForm();
		return;
	}
	currentEditingPath = path;
	editingLeafHadConfigBeforeOpen = path in state.nodeConfig;
	// Bảo đảm có thiết lập TRƯỚC khi mở, để hộp thoại không phải tạo trong lúc đang vẽ.
	cfgOf(path);
	editingLeafInitialSnapshot = JSON.stringify(state.nodeConfig[path]);
	state.editingPath = path;
};

const closeLeafForm = () => {
	state.editingPath = '';
	refreshShape();
};

/** Thiết lập của lá đang mở hộp thoại. Null = không mở hộp thoại nào. */
const editingCfg = computed(() => (state.editingPath ? state.nodeConfig[state.editingPath] ?? null : null));

const leafFormVisible = computed({
	get: () => state.editingPath !== '',
	set: (visible: boolean) => {
		if (!visible) closeLeafForm();
	},
});

const leafFormTitle = computed(() =>
	state.editingPath ? $t('lz.label.sharedataMapping.leafRuleTitle', { key: state.editingPath }) : '',
);

/**
 * Lá có nằm dưới một nhánh hay không. Phải kiểm tới dấu phân cách, không dùng startsWith trần:
 * 'body.segmentsExtra.x' cũng startsWith 'body.segments' nên bị đếm lẫn vào nhánh khác.
 */
const isUnder = (leafPath: string, path: string) =>
	path === '' || leafPath === path || leafPath.startsWith(`${path}.`) || leafPath.startsWith(`${path}[`);

/** Số lá dưới một nhánh đã chọn được trường. */
const mappedUnder = (path: string) => {
	let n = 0;
	for (const leaf of state.leaves) {
		if (!isUnder(leaf.path, path)) continue;
		if ((state.nodeConfig[leaf.path]?.fieldKey ?? '') !== '') n++;
	}
	return n;
};

/** Tổng số lá dưới một nhánh. */
const leavesUnder = (path: string) => state.leaves.filter((l) => isUnder(l.path, path)).length;

/** Trường gói tin mà khuôn đối tác không dùng — chỉ để đối chiếu. */
const notRequestedFields = () => {
	const used = new Set<string>();
	for (const cfg of Object.values(state.nodeConfig)) {
		if (cfg.fieldKey) used.add(cfg.fieldKey);
	}
	return state.packetFields.filter((f) => f.aliasFieldKey && !used.has(f.aliasFieldKey));
};

const loadCodeSets = async () => {
	if (state.codeSetList.length) return;
	await reloadCodeSets();
};

const reloadCodeSets = async () => {
	// includeValues = true: cần cả bảng quy đổi để chạy thử ngay trên máy người dùng.
	const res = await getAPI(ShareDataCodeSetApi).apiSharedataSharedatacodesetListGet('', undefined, undefined, true);
	state.codeSetList = res.data.result ?? [];
};

const loadPacketList = async () => {
	if (state.packetList.length) return;
	const res = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketListGet();
	state.packetList = res.data.result ?? [];
};

/** Đăng ký còn hiệu lực để ánh xạ: đang chạy hoặc tạm dừng. Đã huỷ / từ chối / hết hạn thì bỏ. */
const USABLE_SUB_STATES = [SharedataSubStateEnum.Active, SharedataSubStateEnum.Paused];

/**
 * Nạp đăng ký của đối tác. Gọi một lần rồi lọc trạng thái tại máy: endpoint chỉ nhận MỘT trạng thái
 * mỗi lần nên lọc sẵn ở server sẽ phải gọi hai lượt.
 */
const loadPartnerSubscriptions = async (partnerId?: string | null) => {
	const id = (partnerId ?? '').trim();
	if (!id) {
		state.partnerSubscriptions = [];
		return;
	}

	state.loadingSubscriptions = true;
	try {
		const res = await getAPI(ShareDataSubscriptionApi).apiSharedataSharedatasubscriptionListGet(undefined, id);
		state.partnerSubscriptions = (res.data.result ?? []).filter(
			(s) => s.state != null && USABLE_SUB_STATES.includes(s.state as unknown as SharedataSubStateEnum)
		);
	} catch (e: any) {
		state.partnerSubscriptions = [];
		ElMessage.error(e?.message || $t('lz.message.sharedataMapping.cannotLoadPartnerSubscriptions'));
	} finally {
		state.loadingSubscriptions = false;
	}
};

/**
 * Gói tin được phép chọn theo chiều của hồ sơ:
 *  - Gửi đi / Nhận về : gói tin có đăng ký đúng chiều đó.
 *  - Hai chiều        : gói tin có đăng ký ở CẢ hai chiều — hồ sơ Both phục vụ cả gửi và nhận,
 *                       thiếu một chiều thì nửa hồ sơ không ai dùng.
 */

const availablePacketList = computed(() => {
	if (!state.editForm.partnerId) return [];

	const direction = Number(state.editForm.direction);
	const outbound = new Set<string>();
	const inbound = new Set<string>();

	for (const sub of state.partnerSubscriptions) {
		const datatypeId = (sub.datatypeId ?? '').trim();
		if (!datatypeId) continue;
		if (Number(sub.direction) === SharedataDirectionEnum.Inbound) inbound.add(datatypeId);
		else outbound.add(datatypeId);
	}

	let allowed: Set<string>;
	if (direction === SharedataDirectionEnum.Inbound) allowed = inbound;
	else if (direction === SharedataDirectionEnum.Outbound) allowed = outbound;
	else allowed = new Set([...outbound].filter((id) => inbound.has(id)));

	return (state.packetList as any[]).filter((p: any) => allowed.has(p.id));
});

/** Đã chọn đối tác, đã chọn chiều, nạp xong mà không còn gói tin nào thì phải nói cho người dùng biết. */
const showNoSubscriptionHint = computed(
	() =>
		props.operateType !== 'edit' &&
		!!state.editForm.partnerId &&
		state.editForm.direction != null &&
		!state.loadingSubscriptions &&
		availablePacketList.value.length === 0
);

/**
 * Đổi đối tác: nạp lại đăng ký rồi bỏ hết phần đã khai theo đối tác cũ.
 * Phải reset cả cây ánh xạ vì mã đối tác được nhúng vào $value của $meta trong bộ khung,
 * giữ lại cây cũ là mang mã đối tác cũ đi.
 */
const handlePartnerChange = async () => {
	clearPacketSelection();
	await loadPartnerSubscriptions(state.editForm.partnerId);
};

/** Đổi chiều: danh sách gói tin đổi theo, gói đang chọn không còn hợp lệ thì bỏ. */
const handleDirectionChange = () => {
	const current = (state.editForm.datatypeId ?? '').trim();
	if (!current) return;
	if (availablePacketList.value.some((p: any) => p.id === current)) return;

	clearPacketSelection();
	ElMessage.warning($t('lz.message.sharedataMapping.packetClearedByDirection'));
};

/** Bỏ gói tin đang chọn và mọi thứ đã khai dựa trên nó. */
const clearPacketSelection = () => {
	state.editForm.datatypeId = undefined as unknown as string;
	state.packetFields = [];
	state.packetFieldMap = {};
	// Bản sao phải giữ khuôn và luật để dùng lại cho đích mới. Khi chọn gói tin,
	// handlePacketChange loại các trường không tương thích và refreshShape tính lại metadata.
	if (props.operateType !== 'copy') resetMappingState();
	state.paneHint = $t('lz.label.sharedataMapping.selectPacketFirstForComparison');
};

const handleAddCodeSet = () => {
	state.codeSetOperateType = 'add';
	state.codeSetTitle = $t('lz.label.base.titleAdd', { name: $t('lz.label.sharedataCodeSet.addTitleEntity') });
	editCodeSetRef.value?.openDialog({ scope: 1, values: [] });
};

/** Bộ mã đang chọn ở khoá đang sửa — tra theo mã trong danh sách đã nạp để có ID mở chi tiết. */
const selectedCodeSet = computed(() => {
	const code = (editingCfg.value?.codeSet ?? '').trim();
	if (!code) return undefined;
	return state.codeSetList.find((c: any) => c.code === code);
});

/** Xem chi tiết / sửa bộ mã đang chọn. Hộp thoại tự nạp chi tiết theo ID; lưu xong nạp lại danh sách bộ mã. */
const handleEditCodeSet = () => {
	const codeSet = selectedCodeSet.value;
	if (!codeSet?.id) return;
	state.codeSetOperateType = 'edit';
	state.codeSetTitle = $t('lz.label.base.titleEdit', { name: $t('lz.label.sharedataCodeSet.addTitleEntity') });
	editCodeSetRef.value?.openDialog({ id: codeSet.id });
};

/**
 * editForm.datatypeId LƯU ID GÓI TIN (VD PKT101) — đúng như ShareDataMapping.DatatypeId trong CSDL, nên truyền thẳng.
 * Trường lấy từ danh sách trường của câu SQL: câu riêng của đối tác nếu có, không thì câu mẫu chung.
 */
const loadPacketFields = async (datatypeId?: string | null) => {
	const packetTypeId = (datatypeId ?? '').trim();
	if (!packetTypeId) {
		state.packetFields = [];
		state.packetFieldMap = {};
		return;
	}

	state.loadingFields = true;
	try {
		state.packetFields = await fetchPacketFields(packetTypeId, state.editForm.partnerId);
		if (!state.packetFields.length) ElMessage.warning($t('lz.message.sharedataMapping.packetNoFieldsDeclared', { datatypeId: state.editForm.datatypeId }));
	} catch (e: any) {
		state.packetFields = [];
		ElMessage.error(e?.message || $t('lz.message.sharedataMapping.cannotLoadPacketFields'));
	} finally {
		state.loadingFields = false;
	}

	const map: Record<string, PacketFieldMeta> = {};
	for (const f of state.packetFields) if (f.aliasFieldKey) map[f.aliasFieldKey] = f;
	state.packetFieldMap = map;
};

/** Đổi gói tin: nạp lại trường rồi bỏ chọn những lá trỏ vào trường không còn tồn tại. */
const handlePacketChange = async () => {
	await loadPacketFields(state.editForm.datatypeId);

	// Trường không còn trong gói tin mới: bỏ chọn VÀ xoá luôn luật đã khai cho trường đó,
	// nếu không luật cũ sẽ âm thầm sống lại khi người dùng chọn một trường khác vào lá này.
	for (const cfg of Object.values(state.nodeConfig)) {
		if (!cfg.fieldKey || state.packetFieldMap[cfg.fieldKey] || builtinField(cfg.fieldKey)) continue;
		cfg.fieldKey = '';
		cfg.codeSet = undefined;
		cfg.targetType = '';
		cfg.dateFormat = '';
		cfg.numberFormat = '';
		cfg.defaultPartnerValue = '';
		cfg.defaultSourceValue = '';
	}
	state.paneHint = state.editForm.datatypeId
		? $t('lz.message.sharedataMapping.pasteJsonAnalyzeToCreateTree')
		: $t('lz.label.sharedataMapping.selectPacketFirstForComparison');

	autoMatchLeaves();
	refreshShape();
};

/** Nút "Tự động ánh xạ": chỉ khớp lại theo tên, không nạp lại danh sách trường gói tin. */
const handleAutoMatch = () => {
	const matched = autoMatchLeaves();
	refreshShape();
	ElMessage.success($t('lz.message.sharedataMapping.autoMatched', { matched, total: state.leaves.length }));
};

const leafNameOf = (path: string) => {
	const last = path.split('.').pop() ?? '';
	return last.replace(/\[\d+\]$/, '');
};


const normalizeKey = (s?: string | null) => (s ?? '').toLowerCase().replace(/[_\-\s]/g, '');

/** Một nút object là chỉ thị nếu khoá đầu bắt đầu bằng '$'. */
const isDirective = (node: any) =>
	node !== null && typeof node === 'object' && !Array.isArray(node) && Object.keys(node).some((k) => k.startsWith('$'));




const collectLeaves = (node: any, path: string, out: PartnerLeaf[], seen: Set<string>) => {
	if (Array.isArray(node)) {
		// Dùng CHỈ SỐ THẬT, không thu về '[]'. Nếu thu chung thì mọi phần tử của mảng cố định
		// sẽ dùng chung một thiết lập và giá trị gốc bị mất — VD [1,2,3] thành [null,null,null].
		node.forEach((item, i) => collectLeaves(item, `${path}[${i}]`, out, seen));
		return;
	}

	if (node !== null && typeof node === 'object') {
		for (const key of Object.keys(node)) {
			collectLeaves(node[key], path ? `${path}.${key}` : key, out, seen);
		}
		return;
	}

	if (!path || seen.has(path)) return;
	seen.add(path);

	out.push({
		path,
		name: leafNameOf(path),
		sample: node === null ? 'null' : String(node),
		raw: JSON.stringify(node),
	});
};

/** Dựng cây để vẽ. Đi cùng luật với collectLeaves. */
const buildTemplateTree = (node: any, key: string, path: string): TemplateNode => {
	if (Array.isArray(node)) {
		const children = node.map((item, i) => buildTemplateTree(item, `[${i}]`, `${path}[${i}]`));
		return { key, path, kind: 'array', children };
	}

	if (node !== null && typeof node === 'object') {
		const children: TemplateNode[] = [];
		for (const childKey of Object.keys(node)) {
			children.push(buildTemplateTree(node[childKey], childKey, path ? `${path}.${childKey}` : childKey));
		}
		return { key, path, kind: 'object', children };
	}

	return { key, path, kind: 'leaf', sample: node === null ? 'null' : String(node) };
};


const autoMatchLeaves = () => {
	const index: Record<string, string> = {};
	for (const f of state.packetFields) {
		if (!f.aliasFieldKey) continue;
		const n = normalizeKey(f.aliasFieldKey);
		if (n && !index[n]) index[n] = f.aliasFieldKey;
	}

	let matched = 0;
	for (const leaf of state.leaves) {
		const cfg = cfgOf(leaf.path);
		if (!cfg.fieldKey) {
			const guess = index[normalizeKey(leaf.name)];
			// ShareDataPacketField chưa có codeSet riêng theo field — người dùng tự chọn bộ mã
			// trong form "Riêng trường này", không còn tự gợi ý được như khi lấy từ DataTable.
			if (guess) {
				cfg.fieldKey = guess;
				// Giống khi chọn tay: có trường rồi thì hằng số tự điền từ JSON mẫu không còn nghĩa.
				cfg.constValue = '';
			}
		}
		if (cfg.fieldKey) matched++;
	}

	state.mappedCount = matched;
	return matched;
};

/** Đổi trường ở một lá: bảo đảm có luật cho trường mới và cập nhật số đã khớp. */
const handleFieldChange = (path: string) => {
	const cfg = cfgOf(path);
	if (cfg.fieldKey) {
		// Chọn trường thì hằng số không còn nghĩa.
		cfg.constValue = '';
	} else {
		// Bỏ chọn trường: lá trở thành lá hằng số tĩnh thuần túy.
		// Dọn sạch toàn bộ luật quy đổi cũ để không lưu rác và không gây hiểu lầm.
		cfg.codeSet = undefined;
		cfg.targetType = '';
		cfg.dateFormat = '';
		cfg.numberFormat = '';
		cfg.defaultPartnerValue = '';
		cfg.defaultSourceValue = '';
	}

	// Chuyển sang giá trị hệ thống: bỏ hết luật đã khai và đóng form nếu đang mở,
	// vì giá trị do BE/service tự tính, không đi qua bộ mã hay ép kiểu của hồ sơ.
	if (builtinField(cfg.fieldKey)) {
		cfg.codeSet = undefined;
		cfg.targetType = '';
		cfg.dateFormat = '';
		cfg.numberFormat = '';
		cfg.defaultPartnerValue = '';
		cfg.defaultSourceValue = '';
		if (state.editingPath === path) state.editingPath = '';
	}
	state.mappedCount = state.leaves.filter((l) => (state.nodeConfig[l.path]?.fieldKey ?? '') !== '').length;
	refreshShape();
};

/**
 * Đọc khuôn JSON của đối tác. Nhận MỌI hình dạng: phẳng, lồng nhiều tầng, có mảng, envelope,
 * hay XML đã chuyển sang JSON — đều đi đệ quy thành cây và danh sách lá.
 * silent = true: dùng khi mở lại hồ sơ, không bắn thông báo.
 */
const parsePartnerJson = (options?: { silent?: boolean; skipAutoMatch?: boolean }) => {
	const silent = options?.silent === true;
	// Mở lại hồ sơ đã lưu thì KHÔNG tự khớp: lá người dùng cố ý để trống sẽ bị đoán lại và
	// âm thầm thành có ánh xạ, làm mất quyết định đã lưu.
	const skipAutoMatch = options?.skipAutoMatch === true;
	const text = state.partnerJson.trim();
	if (!text) {
		if (!silent) ElMessage.warning($t('lz.message.sharedataMapping.partnerJsonNotPasted'));
		return;
	}

	if (!state.editForm.datatypeId) {
		state.jsonError = $t('lz.message.sharedataMapping.selectPacketBeforeAnalysis');
		if (!silent) ElMessage.warning(state.jsonError);
		return;
	}

	let parsed: any;
	try {
		parsed = JSON.parse(text);
	} catch (e: any) {
		state.jsonError = $t('lz.message.sharedataMapping.invalidJsonWithReason', { reason: e?.message ?? '' });
		if (!silent) ElMessage.error($t('lz.message.sharedataMapping.invalidJson'));
		return;
	}

	if (parsed === null || typeof parsed !== 'object') {
		state.jsonError = $t('lz.message.sharedataMapping.jsonMustBeObjectOrArray');
		if (!silent) ElMessage.error(state.jsonError);
		return;
	}

	const leaves: PartnerLeaf[] = [];
	collectLeaves(parsed, '', leaves, new Set<string>());
	if (!leaves.length) {
		state.jsonError = $t('lz.message.sharedataMapping.templateHasNoFieldsToMap');
		if (!silent) ElMessage.error(state.jsonError);
		return;
	}

	// Khoá bắt đầu bằng '$' trùng với ký hiệu của bộ khung — cảnh báo để người dùng biết.
	const dollarKeys = leaves.map((l) => l.name).filter((n) => n.startsWith('$'));
	state.jsonError = '';
	if (dollarKeys.length && !silent)
		ElMessage.warning($t('lz.message.sharedataMapping.templateHasDollarKeys', { keys: dollarKeys.join(', ') }));

	state.leaves = leaves;

	// Tạo sẵn thiết lập cho mọi lá NGAY TẠI ĐÂY. Bắt buộc: template gọi cfgOf() để đọc thiết lập,
	// mà cfgOf() tạo mới khi chưa có — tạo trong lúc Vue đang vẽ là sửa state giữa chừng, làm hỏng
	// quá trình patch của el-tree (TypeError "Cannot set properties of null").
	//
	// KHÔNG tự điền giá trị trong JSON mẫu vào hằng số: mẫu chỉ là ví dụ của đối tác, tự điền sẽ
	// khiến mọi gói tin gửi đi mang đúng dữ liệu ví dụ đó. Khoá nào không chọn trường và không tự
	// gõ giá trị thì gửi null, người dùng nhìn icon cảnh báo là biết.
	for (const leaf of leaves) cfgOf(leaf.path);

	const rootNode = buildTemplateTree(parsed, $t('lz.label.sharedataMapping.rootNodeLabel'), '');
	state.templateTree = Array.isArray(parsed) ? [rootNode] : rootNode.children ?? [];
	state.treeVersion += 1;

	if (skipAutoMatch) state.mappedCount = 0;
	else autoMatchLeaves();

	state.paneHint = $t('lz.label.sharedataMapping.fieldsInTemplateReanalyzeHint', { count: leaves.length });
	refreshShape();
	ensureDialogInViewport();

	if (silent) return;
	ElMessage.success($t('lz.message.sharedataMapping.readFieldsAutoMatched', { total: leaves.length, matched: state.mappedCount }));
};

/* Bộ khung đầu ra ----------------------------------------------------- */

/** Nhánh này có lá nào đã chọn trường không — dùng để nhận ra phần tử khuôn của mảng. */
const hasMappedLeafUnder = (path: string) =>
	state.leaves.some((l) => isUnder(l.path, path) && !!state.nodeConfig[l.path]?.fieldKey);

/**
 * Chỉ số các phần tử của một mảng được giữ lại.
 *
 * Mảng có phần tử ánh xạ trường thì CHỈ giữ phần tử đầu tiên làm khuôn — service nhân khuôn
 * đó theo từng bản ghi. Giữ cả mấy phần tử ví dụ của đối tác sẽ làm mỗi bản ghi bị gửi lặp
 * lại nhiều lần. Mảng hằng (không phần tử nào ánh xạ) thì giữ nguyên tất cả.
 */
const templateArrayIndexes = (node: any[], path: string): number[] => {
	const all = node.map((_, i) => i);
	const mapped = all.filter((i) => hasMappedLeafUnder(`${path}[${i}]`));
	if (mapped.length <= 1) return all;
	const keep = mapped[0];
	return all.filter((i) => !mapped.includes(i) || i === keep);
};

/** Ép hằng số về kiểu JSON gốc nếu được, để số vẫn là số chứ không thành chuỗi. */
const parseConst = (raw: string): any => {
	const text = raw.trim();
	if (text === '') return null;
	try {
		return JSON.parse(text);
	} catch {
		return raw;
	}
};

/**
 * Dựng bộ khung đầu ra từ khuôn + thiết lập trên cây. Đây chính là tài liệu lưu vào
 * TargetShapeJson và service đọc để sinh payload.
 *
 * Quy ước — khớp chú thích ShareDataMapping.TargetShapeJson:
 *   {"$field":"X"}            lá có chọn trường
 *   {"$each":true,"$as":NODE} nút mảng bật lặp
 *   giá trị thật              hằng số, giữ nguyên kiểu JSON
 *   khoá không có mặt         lá bị bỏ
 */
const buildShapeNode = (node: any, path: string): any => {
	if (Array.isArray(node)) {
		if (!node.length) return [];

		// Mảng luôn là mảng thật, không có ký hiệu riêng. Bộ dựng payload tự quyết theo nội dung:
		// có $field bên trong thì nhân theo từng dòng dữ liệu, không có thì giữ nguyên như hằng số.
		// Chỉ giữ MỘT phần tử ánh xạ làm khuôn để bản ghi không bị nhân lên nhiều lần.
		return templateArrayIndexes(node, path).map((i) => buildShapeNode(node[i], `${path}[${i}]`));
	}

	if (node !== null && typeof node === 'object') {
		const out: Record<string, any> = {};
		for (const key of Object.keys(node)) {
			out[key] = buildShapeNode(node[key], path ? `${path}.${key}` : key);
		}
		return out;
	}

	const cfg = state.nodeConfig[path];

	// Giá trị hệ thống đi bằng ký hiệu riêng và không bao giờ kèm luật quy đổi.
	// Mã gói tin và mã đối tác đã biết ngay lúc lưu nên ghi kèm $value cho service khỏi tra lại.
	const builtin = builtinField(cfg?.fieldKey);
	if (builtin) {
		const metaNode: Record<string, any> = { [MARK_META]: builtin.key };
		const metaValue = metaValueOf(builtin.key);
		if (metaValue) metaNode[MARK_VALUE] = metaValue;
		return metaNode;
	}

	if (cfg?.fieldKey) {
		const node: Record<string, any> = { [MARK_FIELD]: cfg.fieldKey };
		// Ghi kiểu nguồn để service áp cùng cấu hình với bản xem trước.
		if (hasRule(cfg) || fieldMeta(cfg.fieldKey)?.type) {
			const extend: Record<string, any> = {};
			const targetType = cfg.targetType.trim() || fieldMeta(cfg.fieldKey)?.type;
			if (targetType) extend.targetType = targetType;
			if ((cfg.codeSet ?? '').trim()) extend.codeSet = cfg.codeSet;
			if (cfg.expression.trim()) extend.expression = cfg.expression.trim();
			// Mặc định tách theo chiều: gửi lấy giá trị phía đối tác, nhận lấy giá trị phía nội bộ.
			if (cfg.defaultPartnerValue.trim()) extend.defaultPartnerValue = cfg.defaultPartnerValue.trim();
			if (cfg.defaultSourceValue.trim()) extend.defaultSourceValue = cfg.defaultSourceValue.trim();
			// Định dạng đi theo kiểu ra: service đọc dateFormat cho ngày, numberFormat cho số.
			if (isDateType(cfg.targetType) && cfg.dateFormat.trim()) extend.dateFormat = cfg.dateFormat.trim();
			if (isNumberType(cfg.targetType) && cfg.numberFormat.trim()) extend.numberFormat = cfg.numberFormat.trim();
			node[MARK_EXTEND] = extend;
		}
		return node;
	}

	return parseConst(cfg?.constValue ?? '');
};

const refreshShape = () => {
	state.shapeError = '';
	state.shapeJson = '';
	// Giá trị mặc định sai kiểu không được ghi vào bộ khung; submit chặn lưu theo shapeError.
	const defaultError = invalidDefaultMessage();
	if (defaultError) {
		state.shapeError = defaultError;
		return;
	}

	const text = state.partnerJson.trim();
	if (!text) return;

	let parsed: any;
	try {
		parsed = JSON.parse(text);
	} catch {
		state.shapeError = $t('lz.message.sharedataMapping.cannotBuildFrameworkInvalidJson');
		return;
	}

	try {
		state.shapeJson = JSON.stringify(buildShapeNode(parsed, ''), null, 2);
	} catch (e: any) {
		state.shapeError = e?.message || $t('lz.message.sharedataMapping.cannotBuildFramework');
	}
};

/**
 * Đọc ngược bộ khung đã lưu để phục hồi thiết lập trên cây.
 * Lá nào không có mặt trong bộ khung thì coi là đã bị bỏ — xử lý sau khi đi hết.
 */
const loadShapeConfig = (node: any, path: string, seenPaths: Set<string>) => {
	if (isDirective(node)) {
		if (Object.prototype.hasOwnProperty.call(node, MARK_META)) {
			seenPaths.add(path);
			cfgOf(path).fieldKey = String(node[MARK_META] ?? '');
			return;
		}

		if (Object.prototype.hasOwnProperty.call(node, MARK_FIELD)) {
			seenPaths.add(path);
			const cfg = cfgOf(path);
			cfg.fieldKey = String(node[MARK_FIELD] ?? '');

			const extend = node[MARK_EXTEND];
			if (extend !== null && typeof extend === 'object') {
				cfg.targetType = String(extend.targetType ?? '');
				cfg.codeSet = extend.codeSet ?? undefined;
				cfg.expression = String(extend.expression ?? '');
				cfg.defaultPartnerValue = String(extend.defaultPartnerValue ?? '');
				cfg.defaultSourceValue = String(extend.defaultSourceValue ?? '');
				cfg.dateFormat = String(extend.dateFormat ?? '');
				cfg.numberFormat = String(extend.numberFormat ?? '');
			}
			return;
		}
		// Chỉ thị lạ: không tự suy diễn, chỉ báo cho người dùng biết.
		state.shapeError = $t('lz.message.sharedataMapping.savedFrameworkUnknownDirective', { path });
		return;
	}

	if (Array.isArray(node)) {
		node.forEach((item, i) => loadShapeConfig(item, `${path}[${i}]`, seenPaths));
		return;
	}

	if (node !== null && typeof node === 'object') {
		for (const key of Object.keys(node)) {
			loadShapeConfig(node[key], path ? `${path}.${key}` : key, seenPaths);
		}
		return;
	}

	// Hằng số. null nghĩa là "gửi null", để trống ô Hằng số.
	seenPaths.add(path);
	const cfg = cfgOf(path);
	cfg.constValue = node === null ? '' : JSON.stringify(node);
};

const resetMappingState = () => {
	// Dữ liệu xem thử thuộc về hồ sơ / đối tác cũ: xoá mẫu CSDL, xoá payload nhận để điền lại bằng JSON mẫu mới.
	sendPreviewRef.value?.reset();
	receivePreviewRef.value?.reset();
	state.partnerJson = '';
	state.jsonError = '';
	state.templateTree = [];
	state.treeVersion += 1;
	state.leaves = [];
	state.nodeConfig = {};
	state.shapeJson = '';
	state.shapeError = '';
	state.mappedCount = 0;
	state.editingPath = '';
};

/* Repeat the suffix exactly; never truncate a source code or change BE identity rules. */
const resolveCopyCode = async (): Promise<boolean> => {
	const request = copyCodeRequest;
	while (true) {
		if ((state.editForm.code ?? '').length > EntityFieldLength.Length64) {
			state.codeError = 'lz.validation.sharedataMapping.copyCodeTooLong';
			state.activeTab = 'info';
			return false;
		}
		const result = await getAPI(ShareDataMappingApi).apiSharedataSharedatamappingListGet(undefined, state.editForm.code ?? undefined);
		// Người dùng đã đổi đích trong lúc chờ: mã đã được sinh lại, bỏ kết quả cũ.
		if (request !== copyCodeRequest) return false;
		if (!Array.isArray(result.data.result)) throw new Error('Invalid mapping code lookup');
		if (!result.data.result.length) {
			state.codeError = '';
			return true;
		}
		state.editForm.code += '_copy';
	}
};

// Open dialog
const openDialog = async (row: any) => {
	editFormRef.value?.resetFields();
	state.codeError = '';
	state.editForm = JSON.parse(JSON.stringify(row));
	state.packetFields = [];
	state.packetFieldMap = {};
	resetMappingState();
	state.activeTab = 'info';
	state.paneHint = row?.datatypeId
		? $t('lz.message.sharedataMapping.pasteJsonAnalyzeToCreateTree')
		: $t('lz.label.sharedataMapping.selectPacketFirstForComparison');
	state.isShowDialog = true;
	nextTick(() => {
		const dialogEl = document.querySelector('.mp-dialog') as HTMLElement;
		if (dialogEl) dialogEl.style.transform = '';
		resetFormBodyHeight();
	});
	state.loading = true;
	state.loadFailed = false;
	try {

		state.partnerSubscriptions = [];

		await loadCodeSets();
		await loadPacketList();

		if (row?.id) {
			const res = await getAPI(ShareDataMappingApi).apiSharedataSharedatamappingByidGet(row.id);
			if (!res.data.result) throw new Error('Missing mapping detail');
			state.editForm = JSON.parse(JSON.stringify(res.data.result));
		}



		// Nạp đăng ký của đối tác để ô gói tin có danh sách ngay khi mở (kể cả khi sửa, hoặc khi
		// đối tác đã được truyền sẵn vào lúc thêm).
		if (state.editForm.partnerId) await loadPartnerSubscriptions(state.editForm.partnerId);

		if (state.editForm.datatypeId) await loadPacketFields(state.editForm.datatypeId);

		// Khuôn và bộ khung: cột targetShapeJson chưa có trong client sinh từ Swagger nên đọc qua any.
		const saved = state.editForm as any;
		state.partnerJson = saved.partnerSchemaJson ?? '';
		const shapeText = (saved.targetShapeJson ?? '').toString().trim();
		if (state.partnerJson.trim()) parsePartnerJson({ silent: true, skipAutoMatch: !!shapeText });

		if (shapeText) {
			try {
				// Bỏ khoá nay ghi tường minh bằng $exclude nên đọc thẳng, không suy từ sự vắng mặt.
				loadShapeConfig(JSON.parse(shapeText), '', new Set<string>());
				state.mappedCount = state.leaves.filter((l) => (state.nodeConfig[l.path]?.fieldKey ?? '') !== '').length;
			} catch {
				state.shapeError = $t('lz.message.sharedataMapping.savedFrameworkInvalidJson');
			}
		}

		// Chỉ sinh mã sau khi đã nạp đủ đối tác/gói tin và chi tiết bản nguồn.
		if (props.operateType !== 'edit') {
			state.editForm.id = '';
			if (props.operateType === 'copy') {
				const source = state.editForm;
				state.copySource = { code: source.code ?? '', partnerId: source.partnerId ?? '', datatypeId: source.datatypeId ?? '', direction: source.direction as number | undefined };
				copyCodeRequest++;
				state.editForm.code = copyBaseCode();
				await resolveCopyCode();
			} else syncMappingCode();
		}
		refreshShape();
		ensureDialogInViewport();
	} catch {
		state.loadFailed = true;
	} finally {
		state.loading = false;
	}
};

// Close dialog
const closeDialog = () => {
	emits('handleQuery');
	state.isShowDialog = false;
	dialogRef.value?.resetPosition?.();
};

// Cancel
const cancel = () => {
	state.isShowDialog = false;
	dialogRef.value?.resetPosition?.();
};

// Submit
const submit = async () => {
	if (state.isProcessing || state.loading || state.loadFailed || state.loadingFields || state.loadingSubscriptions) return;
	state.isProcessing = true;
	let checkingCode = false;
	try {
		const valid = await editFormRef.value.validate().catch(() => false);
		if (!valid) {
			state.activeTab = 'info';
			return;
		}

		if (props.operateType === 'copy') {
			checkingCode = true;
			// Bỏ lượt kiểm tra trùng còn dở của lần đổi đích trước, kiểm tra lại trên mã hiện tại.
			copyCodeRequest++;
			if (!(await resolveCopyCode())) return;
			checkingCode = false;
		} else if (props.operateType !== 'edit') {
			checkingCode = true;
			const result = await getAPI(ShareDataMappingApi).apiSharedataSharedatamappingListGet(undefined, state.editForm.code ?? undefined);
			checkingCode = false;
			if (!Array.isArray(result.data.result)) {
				state.codeError = 'lz.validation.sharedataMapping.codeCheckFailed';
				state.activeTab = 'info';
				return;
			}
			if (result.data.result.length) {
				state.codeError = 'lz.validation.sharedataMapping.generatedCodeExists';
				state.activeTab = 'info';
				return;
			}
			state.codeError = '';
		}

		if (!state.leaves.length) {
			state.activeTab = 'map';
			ElMessage.warning($t('lz.message.sharedataMapping.noPartnerTemplateJsonAnalyze'));
			return;
		}

		refreshShape();
		if (state.shapeError) {
			state.activeTab = 'map';
			ElMessage.error(state.shapeError);
			return;
		}

		// Phải có ít nhất một khoá lấy dữ liệu THẬT từ gói tin. Bốn giá trị hệ thống
		// (Now, Serial, PacketCode, PartnerCode) không tính, vì hồ sơ chỉ gồm chúng
		// thì gói tin gửi đi không mang dữ liệu nào.
		const mapped = state.leaves.some((l) => {
			const cfg = state.nodeConfig[l.path];
			return !!cfg && !!cfg.fieldKey && !builtinField(cfg.fieldKey);
		});

		if (!mapped) {
			state.activeTab = 'map';
			ElMessage.warning($t('lz.message.sharedataMapping.noPartnerFieldSelectedPacketField'));
			return;
		}

		// Hồ sơ có gửi đi (chiều Gửi hoặc Cả hai) phải khai đủ 4 giá trị hệ thống,
		// vì gói tin gửi cho đối tác luôn cần thời điểm, số thứ tự, mã gói tin và mã đối tác.
		if (Number(state.editForm.direction) !== SharedataDirectionEnum.Inbound) {
			const usedKeys = new Set(
				state.leaves.map((l) => state.nodeConfig[l.path]?.fieldKey).filter((k) => !!k) as string[],
			);
			const missing = BUILTIN_FIELDS.filter((b) => !usedKeys.has(b.key)).map((b) => b.key);

			if (missing.length) {
				state.activeTab = 'map';
				ElMessage.warning($t('lz.message.sharedataMapping.missingBuiltinFields', { fields: missing.join(', ') }));
				return;
			}
		}

		// Ánh xạ và luật quy đổi đều nằm trong bộ khung; DTO của backend không còn trường items.
		// targetShapeJson chưa có trong client sinh từ Swagger nên gán qua any.
		const payload = {
			...state.editForm,
			partnerSchemaJson: state.partnerJson,
			targetShapeJson: state.shapeJson,
		} as any;

		if (props.operateType == 'edit') {
			await getAPI(ShareDataMappingApi).apiSharedataSharedatamappingUpdatePost(payload);
		} else if (props.operateType == 'copy' || props.operateType == 'add') {
			payload.id = '';
			await getAPI(ShareDataMappingApi).apiSharedataSharedatamappingAddPost(payload);
		}
		ElMessage.success($t('lz.message.base.saveSuccess'));
		closeDialog();
	} catch {
		if (checkingCode) {
			state.codeError = 'lz.validation.sharedataMapping.codeCheckFailed';
			state.activeTab = 'info';
		}
		// Interceptor hiển thị lỗi API; giữ form để người dùng sửa hoặc thử lại.
	} finally {
		state.isProcessing = false;
	}
};

// Export object
defineExpose({ openDialog });
</script>

<style scoped lang="scss">
.direction-hint {
	margin-top: 4px;
	font-size: 12px;
	line-height: 1.4;
	color: var(--el-text-color-secondary);
}

.mp-pane {
	display: flex;
	flex-direction: column;
	width: 100%;
	height: 100%;
}

.mp-pane-head {
	display: flex;
	align-items: center;
	gap: 8px;
	margin-bottom: 5px;
	min-height: 28px;
}

.mp-pane-title {
	font-weight: 600;
}

.mp-json :deep(.el-textarea__inner) {
	font-family: Consolas, Monaco, monospace;
	font-size: 12px;
	line-height: 1.5;
}

.mp-codeset {
	display: flex;
	align-items: center;
	gap: 4px;
	width: 100%;

	/* Hai nút xem/sửa và thêm bộ mã: bỏ margin-left mặc định của .el-button + .el-button
	   để khoảng cách chỉ do gap quyết định, nút vuông cùng cỡ với ô chọn. */
	.el-button {
		margin-left: 0;
		width: 24px;
		height: 24px;
		padding: 0;
		flex: 0 0 auto;
	}
}

.mp-codeset-select {
	flex: 1 1 0%;
	min-width: 0;
}

.mp-err {
	font-size: 12px;
	color: var(--el-color-danger);
	line-height: 1.4;
}

.rs-note {
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.mt5 {
	margin-top: 5px;
}

.mt10 {
	margin-top: 10px;
}

/* Vùng nội dung hộp thoại: tự co giãn theo nội dung, không bị khoảng trắng thừa khi tạo mới */
.mp-form-body {
	max-height: calc(88vh - 120px);
	overflow-y: auto;
	overflow-x: hidden;
}

/* Cây khuôn: mỗi nút một dòng, control nằm ngang cùng nhãn. Chiều cao tối đa tính theo vùng nội dung. */
.mp-tree {
	overflow: auto;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 4px;
	padding: 4px 0;
}

.mp-tree :deep(.el-tree-node__content) {
	height: 34px;
}

/* Khung trống khi chưa phân tích JSON: nét đứt, nền nhạt, hình minh hoạ nằm giữa (chiều cao theo emptyPaneHeight). */
.mp-tree-empty {
	display: flex;
	align-items: center;
	justify-content: center;
	border: 1px dashed var(--el-border-color);
	border-radius: 4px;
	background-color: var(--el-fill-color-lighter);
}

/* Bộ khung đầu ra khi chưa phân tích: lấp phần còn lại của cột trái để đáy thẳng hàng với khung trống bên phải. */
.mp-shape-empty {
	flex: 1 1 auto;
	display: flex;
	align-items: center;
	justify-content: center;
	min-height: 120px;
	padding: 12px;
	text-align: center;
	border: 1px dashed var(--el-border-color);
	border-radius: 4px;
	background-color: var(--el-fill-color-lighter);
}

/* Tieu de 2 vung cua moi dong trong cay: trai la du lieu doi tac, phai la field he thong dang chon */
.mp-tree-header {
	display: flex;
	align-items: center;
	gap: 6px;
	padding: 0 8px 4px;
	font-size: 12px;
	font-weight: 600;
	color: var(--el-text-color-secondary);
}

.mp-tree-header-mid {
	margin-left: auto;
	width: 232px;
	flex: 0 0 auto;
}

.mp-tree-header-right {
	width: 124px;
	flex: 0 0 auto;
	text-align: right;
}

/* Chú thích icon: nói rõ mỗi icon ở cột phải nghĩa là gì, người dùng không phải đoán. */
.mp-legend {
	display: flex;
	align-items: center;
	flex-wrap: wrap;
	gap: 4px 12px;
	padding: 5px 8px;
	margin-bottom: 5px;
	border-radius: 4px;
	background-color: var(--el-fill-color-lighter);
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.mp-legend-title {
	font-weight: 600;
}

.mp-legend-item {
	display: inline-flex;
	align-items: center;
	gap: 3px;
}

.mp-legend-item--warn {
	color: var(--el-color-warning);
}

/* Nhắc chọn Meta cho phần header: nền xanh nhạt để tách khỏi khối chú thích icon phía trên. */
.mp-meta-hint {
	display: flex;
	align-items: flex-start;
	gap: 6px;
	padding: 6px 8px;
	margin-bottom: 5px;
	border-radius: 4px;
	background-color: var(--el-color-primary-light-9);
	color: var(--el-color-primary);
	font-size: 12px;
	line-height: 1.5;
}

.mp-meta-hint .el-icon {
	flex: 0 0 auto;
	margin-top: 2px;
}

.mp-node {
	display: flex;
	align-items: center;
	gap: 6px;
	width: 100%;
	padding-right: 8px;
}

.mp-node-key {
	font-family: Consolas, Monaco, monospace;
	font-weight: 600;
	white-space: nowrap;
}

.mp-node-sample {
	font-family: Consolas, Monaco, monospace;
	font-size: 12px;
	color: var(--el-text-color-placeholder);
	max-width: 110px;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

/* Cột GIỮA: ô chọn trường hệ thống, luôn thẳng hàng với tiêu đề "Hệ thống". */
.mp-node-mid {
	display: flex;
	align-items: center;
	gap: 6px;
	margin-left: auto;
	width: 232px;
	flex: 0 0 auto;
}

.mp-node-field {
	width: 200px;
	flex: 0 0 auto;
}

/* Cột PHẢI: icon của luật đã khai. Bấm vào đâu trong cột cũng mở form thiết lập. */
.mp-node-icons {
	display: flex;
	align-items: center;
	justify-content: flex-end;
	gap: 6px;
	width: 124px;
	flex: 0 0 auto;
	cursor: pointer;
}

.mp-icon {
	color: var(--el-color-primary);
}

.mp-node-icons--readonly {
	cursor: default;
}

.mp-icon--warn {
	color: var(--el-color-warning);
}

.mp-icon--muted {
	color: var(--el-text-color-placeholder);
}

.mp-info {
	color: var(--el-color-info);
	cursor: help;
	flex: 0 0 auto;
}

.mp-opt-note {
	float: right;
	margin-left: 12px;
	color: var(--el-text-color-secondary);
	font-size: 12px;
}

.mp-leaf-form :deep(.el-form-item) {
	margin-bottom: 10px;
}

.mp-leaf-form :deep(.el-form-item__label) {
	padding-bottom: 2px;
	font-size: 12px;
	line-height: 1.4;
	color: var(--el-text-color-regular);
}

.mp-leaf-form :deep(.el-divider) {
	margin: 10px 0 8px;
}

.mp-leaf-form :deep(.el-divider__text) {
	font-size: 12px;
	font-weight: 600;
	white-space: nowrap;
}

.mp-disabled-section {
	opacity: 0.7;
	background-color: var(--el-fill-color-lighter);
	border: 1px dashed var(--el-border-color);
	border-radius: 4px;
	padding: 8px 10px;
}

.mp-leaf-form .rs-note {
	line-height: 1.5;
}

.mp-form-footer {
	display: flex;
	justify-content: flex-end;
	gap: 6px;
	margin-top: 4px;
}

.mp-unused {
	display: flex;
	flex-wrap: wrap;
	gap: 4px;
}

.mp-direction-tabs {
	margin-bottom: 8px;

	:deep(.el-tabs__header) {
		margin: 0;
	}

	:deep(.el-tabs__nav-wrap) {
		margin-bottom: 0;

		&::after {
			height: 1px;
			background-color: var(--el-border-color-lighter);
		}
	}

	:deep(.el-tabs__nav) {
		display: flex;
		width: 100%;
	}

	:deep(.el-tabs__content) {
		display: none;
		padding: 0;
	}

	:deep(.el-tabs__item) {
		flex: 1;
		text-align: center;
		justify-content: center;
		padding: 0 16px;
		height: 38px;
		line-height: 38px;

		&.is-active {
			font-weight: 600;
			color: var(--el-color-primary);
		}
	}

	:deep(.el-tabs__active-bar) {
		background-color: var(--el-color-primary);
		height: 2px;
	}

	&.is-locked :deep(.el-tabs__item.is-disabled) {
		cursor: not-allowed;
	}
}

.mp-direction-banner {
	margin-bottom: 8px;
	padding: 6px 10px;
	border-radius: 4px;
	font-size: 12px;
	line-height: 1.5;
	color: var(--el-text-color-secondary);
	background-color: var(--el-fill-color-lighter);
}

.mp-tabs :deep(.el-tabs__header) {
	position: sticky;
	top: 0;
	z-index: 10;
	background: var(--el-bg-color);
	margin-bottom: 12px;
}

:deep(.el-overlay .el-dialog.mp-dialog) {
	max-width: 1400px;
	width: var(--el-dialog-width, min(960px, 95vw));
}

.mp-code-item :deep(.el-form-item__error) {
	position: relative;
	top: auto;
	left: auto;
	margin-top: 4px;
}

@media (max-width: 992px) {
	:deep(.el-overlay .el-dialog.mp-dialog) {
		width: 96vw;
	}
	.mp-node {
		min-width: 520px;
	}
	.mp-tree-header {
		min-width: 520px;
	}
}
</style>
