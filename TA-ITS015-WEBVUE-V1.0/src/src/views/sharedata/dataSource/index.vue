<template>
	<WorkspacePage>
	<div ref="datasourceContainerRef" class="esh-datasource-container">
		<splitpanes class="default-theme datasource-splitpanes" style="width: 100%; height: 100%; flex: 1" @resized="handleSplitterResized">
			<!-- Gói tin: begin -->
			<pane :size="packetPaneSize" :min-size="25" class="full-height packet-col packet-pane">
				<div class="packet-pane-inner">
				<el-card shadow="hover" class="search-card search-card-packet" :body-style="{ padding: '6px 8px' }">
					<el-form 
						:model="state.queryParamsPacket" 
						:show-message="false" 
						label-position="left" 
						label-width="auto" 
						:inlineMessage="true" 
						class="search-form">
						<el-row :gutter="8">
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
								<el-form-item :label="$t('lz.entity.base.code')">
									<el-input
										v-model="state.queryParamsPacket.code"
										:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.code') })"
										@keyup.enter="debouncedQueryPacket.flush()"
										:maxlength="128"
										show-word-limit
										clearable
									/>
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
								<el-form-item :label="$t('lz.entity.base.name')">
									<el-input
										v-model="state.queryParamsPacket.name"
										:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.name') })"
										@keyup.enter="debouncedQueryPacket.flush()"
										:maxlength="128"
										show-word-limit
										clearable
									/>
								</el-form-item>
							</el-col>
							<!-- Tìm gói tin theo đối tác: chọn một đối tác là bảng chỉ còn các gói đối tác đó dùng, kèm cột Nguồn SQL (Chung / Riêng) -->
							<el-col
								v-if="isSuperAdmin"
								:span="24"
								class="packet-partner-filter"
							>
								<el-form-item :label="$t('lz.label.sharedataDataSource.filterByPartner')">
									<el-select
										v-model="state.queryParamsPacket.partnerId"
										class="w100"
										filterable
										clearable
										:placeholder="$t('lz.label.sharedataDataSource.filterByPartnerPlaceholder')"
										@change="handlePartnerFilterChange"
									>
										<el-option
											v-for="p in partnerOptions"
											:key="p.id"
											:label="p.name ? `${p.name} (${p.code})` : p.code"
											:value="p.id"
										/>
									</el-select>
								</el-form-item>
							</el-col>
						</el-row>
					</el-form>
				</el-card>
				<el-card class="full-table" shadow="hover" style="margin-top: 5px">
					<vxe-grid ref="xGridPacket" height="100%" class="xGrid-style" v-bind="optionsPacketGrid" v-on="gridEventsPacket" @cell-click="handleFieldByPacket">
						<template #toolbar_buttons>
							<el-icon size="16" style="margin-right: 3px; margin-top: 2px; display: inline; vertical-align: middle"><ele-Files /></el-icon>
							{{ $t('lz.label.sharedataDataSource.packetTitle') }}
						</template>
						<template #toolbar_tools>
							<TableHeader
								:show-delete="false"
								:show-add="isSuperAdmin"
								@add="handleAddPacket"
								@refresh="handleQueryPacket"
								@custom-column="handlePacketCustom"
							/>
						</template>
						<template #empty>
							<el-empty :description="$t('lz.label.sharedataDataSource.noPacket')" :image-size="160" />
						</template>
						<template #row_code="{ row }">
							<el-tag v-if="row.code" size="small" effect="plain">{{ row.code }}</el-tag>
							<!-- Gói đang tắt: worker bỏ qua gửi, đánh dấu để không lẫn với gói đang chạy -->
							<el-tag v-if="row.status === StatusEnum.NUMBER_0" size="small" type="info" effect="light" class="ml5">{{ $t('lz.label.base.inactive') }}</el-tag>
						</template>
						<template #row_partnerCount="{ row }">
							<el-tooltip
								v-if="row.partnerCount"
								:content="(row.partnerNames || []).join(', ')"
								placement="top"
								popper-class="partner-count-tooltip"
								:disabled="!row.partnerNames || row.partnerNames.length === 0"
							>
								<el-tag size="small" type="primary" effect="plain" class="partner-count-tag">
									{{ row.partnerCount }} {{ $t('lz.label.sharedataDataSource.partnerUnit') }}
								</el-tag>
							</el-tooltip>
							<span v-else class="note">—</span>
						</template>
						<!-- Nguồn SQL của gói với đối tác đang lọc: Riêng = có SQL riêng, Chung = dùng mẫu chung -->
						<template #row_sqlSource="{ row }">
							<el-tag
								v-if="row.isPartnerCustomSql"
								size="small"
								type="primary"
								effect="light"
							>
								{{ $t('lz.label.sharedataDataSource.tagCustomSql') }}
							</el-tag>
							<el-tag
								v-else
								size="small"
								type="info"
								effect="plain"
							>
								{{ $t('lz.label.sharedataDataSource.tagSharedSql') }}
							</el-tag>
						</template>
						<!-- Ghi ngược: số câu ghi chiều nhận của gói, 0 = gói chỉ gửi -->
						<template #row_writeSql="{ row }">
							<el-tag
								size="small"
								:type="row.writeSqlCount ? 'success' : 'info'"
								effect="plain"
							>
								{{ row.writeSqlCount ? $t('lz.label.sharedataWriteSql.countTag', { count: row.writeSqlCount }) : $t('lz.label.sharedataWriteSql.sendOnly') }}
							</el-tag>
						</template>
						<template #row_record="{ row }">
							<ModifyRecord :data="row" />
						</template>
						<template #row_buttons="{ row }">
							<template v-if="isSuperAdmin">
							<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
								<el-button icon="ele-CopyDocument" text type="warning" @click.stop="handleCopyPacket(row)"> </el-button>
							</el-tooltip>
							<el-tooltip
								:content="packetActionHint(row.id, 'edit')"
								placement="top"
								popper-class="data-source-tooltip"
							>
								<span @click.stop><el-button icon="ele-Edit" text type="primary" :disabled="!packetUsageReady(row.id)" @click.stop="handleEditPacket(row)"> </el-button></span>
							</el-tooltip>
							<el-tooltip
								:content="packetActionHint(row.id, 'delete')"
								placement="top"
								popper-class="data-source-tooltip"
							>
								<span @click.stop><el-button icon="ele-Delete" text type="danger" :disabled="!row.id || packetUsage[row.id] !== 'available'" @click.stop="handleDeletePacket(row)"> </el-button></span>
							</el-tooltip>
							</template>
						</template>
					</vxe-grid>
				</el-card>
				</div>
			</pane>
			<!-- Gói tin: end -->

			<!-- Chi tiết gói tin & Cột dữ liệu (Option A - Wireframe W0.2): begin -->
			<pane :size="100 - packetPaneSize" :min-size="35" class="full-height field-col field-pane">
				<div class="field-pane-inner">
					<el-card class="full-table right-pane-card" shadow="hover">
					<!-- Chưa chọn gói: hiện khung trống như các màn khác (el-empty), chưa gọi API chi tiết; skeleton chỉ dùng lúc đang tải -->
					<div v-if="!state.currentPacket" class="empty-placeholder">
						<el-empty :description="$t('lz.label.sharedataDataSource.selectPacketToViewFields')" :image-size="160" />
					</div>

					<!-- Đổi gói tin: phủ nền trắng đặc + vòng xoay, không để lộ nội dung gói cũ -->
					<div v-else ref="packetDetailsRef" v-loading="packetSwitching" :element-loading-text="$t('lz.label.base.loading')" element-loading-background="var(--el-bg-color, #ffffff)" element-loading-custom-class="solid-loading-mask" class="packet-details-wrapper" :class="{ 'is-switching': packetSwitching, 'is-scope-switching': scopeSwitching }">
						<!-- Header Bar của Gói tin đang chọn -->
						<div class="packet-header-bar">
							<div class="packet-header-title">
								<el-icon size="18" class="mr5 text-primary"><ele-Coin /></el-icon>
								<span class="packet-title">{{ state.currentPacket.name }}</span>
								<el-tag size="small" effect="plain" class="ml5">{{ state.currentPacket.code }}</el-tag>
							</div>
							<!-- Nhìn vào là biết gói đang được bao nhiêu đối tác dùng (tóm tắt số cấu hình riêng / dùng thẳng mẫu chung); gói nhiều đối tác chỉ hiện 10 thẻ đầu (cấu hình riêng trước), bấm Xem tất cả mới hiện đủ -->
							<div v-if="isSuperAdmin" class="packet-partner-usage">
								<template v-if="partnerSqlList.length">
									<span class="usage-label">{{ $t('lz.label.sharedataDataSource.partnerUsage', { count: partnerSqlList.length }) }}</span>
									<span class="usage-label usage-summary">{{ $t('lz.label.sharedataDataSource.partnerUsageSummary', { custom: customPartnerCount, template: partnerSqlList.length - customPartnerCount }) }}</span>
									<el-tag
										v-for="p in visiblePartnerUsage"
										:key="p.partnerId"
										size="small"
										:type="p.isCustom ? 'warning' : 'info'"
										effect="plain"
									>
										{{ p.partnerCode || p.partnerName }} · {{ $t(p.isCustom ? 'lz.label.sharedataDataSource.tagCustomSql' : 'lz.label.sharedataDataSource.tagSharedSql') }}
									</el-tag>
									<el-button
										v-if="partnerSqlList.length > PARTNER_USAGE_VISIBLE_LIMIT"
										size="small"
										text
										type="primary"
										class="partner-usage-toggle"
										@click="partnerUsageExpanded = !partnerUsageExpanded"
									>
										{{ partnerUsageExpanded ? $t('lz.button.base.collapse') : $t('lz.button.sharedataDataSource.partnerUsageShowAll', { count: partnerSqlList.length }) }}
									</el-button>
								</template>
								<span v-else class="usage-label">{{ $t('lz.label.sharedataDataSource.partnerUsageNone') }}</span>
							</div>
						</div>

						<!-- Tab Chiều gửi / Chiều nhận (chỉ SuperAdmin thấy tab Chiều nhận) -->
						<el-tabs v-if="isSuperAdmin" v-model="directionTab" class="direction-tabs">
							<el-tab-pane name="send" :label="$t('lz.label.sharedataDataSource.tabSend')" />
							<el-tab-pane name="receive" :label="receiveTabLabel" />
						</el-tabs>

						<!-- Một trang Chiều gửi: gồm Thanh tab đối tác và Vùng 1, 2, 3 -->
						<div v-show="!isSuperAdmin || directionTab === 'send'" class="send-direction-container">
							<!-- Ô chọn phạm vi SQL (Mẫu chung / đối tác) và các thao tác SQL (nằm trên Vùng 1, 2, 3) -->
							<div v-if="isSuperAdmin" class="sql-header-row">
								<div class="sql-partner-tabs-bar">
									<span class="partner-tab-jump-label">{{ $t('lz.label.sharedataDataSource.selectScopeLabel') }}</span>
									<el-tooltip
										:content="activeScopeLabel"
										placement="top"
										:show-after="300"
										:trigger-keys="[]"
										popper-class="data-source-tooltip"
									>
									<el-select
										:key="scopeSelectKey"
										:model-value="activeSqlTab"
										size="small"
										filterable
										class="partner-tab-jump"
										popper-class="partner-tab-jump-popper"
										:disabled="scopeSwitching || resettingPartnerSql"
										:placeholder="$t('lz.placeholder.sharedataDataSource.selectPartner')"
										@change="handleJumpToPartnerTab"
									>
										<template #prefix>
											<el-icon><ele-Search /></el-icon>
										</template>
										<el-option
											value="standard"
											:label="$t('lz.label.sharedataDataSource.tabStandardSql')"
										/>
										<el-option
											v-for="p in displayedPartnerTabs"
											:key="p.partnerId"
											:label="partnerTabJumpLabel(p)"
											:value="p.partnerId"
										/>
									</el-select>
									</el-tooltip>
									<el-button
										size="small"
										text
										type="primary"
										class="add-partner-tab-btn"
										:disabled="!hasUnconfiguredPartners || scopeSwitching"
										@click="partnerSetupDialogVisible = true"
									>
										{{ $t('lz.button.sharedataDataSource.addPartnerSqlTab') }}
									</el-button>
								</div>

								<!-- Toolbar hành động của SQL Center -->
								<div class="sql-toolbar-bar">
									<div class="sql-toolbar-left">
										<!-- Khi ở tab đối tác: hiện Tên đối tác và chip phiên bản mẫu theo wireframe -->
										<template v-if="activeSqlTab !== 'standard' && currentPartnerItem">
											<b class="partner-toolbar-code">{{ currentPartnerItem.partnerCode || currentPartnerItem.partnerName }}</b>
											<span v-if="currentPartnerItem.partnerName && currentPartnerItem.partnerName !== currentPartnerItem.partnerCode" class="partner-toolbar-name">{{ currentPartnerItem.partnerName }}</span>
											<span v-if="activePartnerDetail?.baseVersion && activePartnerDetail?.templateVersion" class="chip-status" :class="isPartnerOutdated ? 'chip-warning' : 'chip-success'">
												{{ partnerVersionStatusText }}
											</span>
										</template>
										<template v-else>
											<!-- Gom vào dòng "Đối tác dùng gói" dưới tên gói (yêu cầu 27/10); mở lại: bỏ comment
											<span class="toolbar-meta-note">
												{{ commonToolbarMetaText }}
											</span>
											-->
										</template>
									</div>
									<div class="sql-toolbar-right vqb-header-actions">
										<!-- Nút So sánh tất cả: chỉ hiện ở tab Mẫu chung khi có ít nhất 1 đối tác có SQL riêng -->
										<el-button
											v-if="activeSqlTab === 'standard' && customPartnerCount > 0"
											size="small"
											text
											type="primary"
											class="compare-all-btn"
											:disabled="scopeSwitching"
											@click="compareAllDialogVisible = true"
										>
											{{ $t('lz.button.sharedataDataSource.compareAllBtn', { count: customPartnerCount }) }}
										</el-button>

										<!-- Nút Dùng lại mẫu chung: chỉ hiện ở tab đối tác khi isCustom -->
										<el-button
											v-if="activeSqlTab !== 'standard' && currentPartnerItem?.isCustom"
											size="small"
											text
											type="warning"
											icon="ele-RefreshLeft"
											:loading="resettingPartnerSql"
											:disabled="scopeSwitching || sqlScopeLocked || resettingPartnerSql"
											@click="handleResetPartnerSql"
										>
											{{ $t('lz.button.sharedataDataSource.resetToStandard') }}
										</el-button>

										<!-- Nút So sánh với mẫu chung: chỉ hiện ở tab đối tác khi isCustom -->
										<el-button
											v-if="activeSqlTab !== 'standard' && currentPartnerItem?.isCustom"
											size="small"
											text
											type="primary"
											icon="ele-Operation"
											:disabled="scopeSwitching || resettingPartnerSql"
											@click="handleOpenDiffDialog"
										>
											{{ $t('lz.button.sharedataDataSource.compareWithStandard') }}
										</el-button>
										<el-button
											size="small"
											type="primary"
											icon="ele-FolderChecked"
											:loading="sqlCenterState.isSaving"
											:disabled="saveDisabled || scopeSwitching || sqlScopeLocked || resettingPartnerSql"
											@click="handleSaveClick"
										>
											{{ $t('lz.button.sharedataDataSource.saveAndAnalyze') }}
										</el-button>
									</div>
								</div>
							</div>

							<!-- Vùng 1, 2, 3: Phủ nền trắng khi chuyển đổi tab đối tác hoặc đang tải dữ liệu SQL -->
							<div
								class="send-regions-wrapper"
								:class="{ 'is-scope-loading': scopeSwitching || (!packetSwitching && sqlCenterState.isLoading) }"
								v-loading="scopeSwitching || (!packetSwitching && sqlCenterState.isLoading)"
								:element-loading-text="$t('lz.label.base.loading')"
								element-loading-background="var(--el-bg-color, #ffffff)"
								element-loading-custom-class="solid-loading-mask"
							>
								<!-- Còn đăng ký gửi đang chạy: BE chặn sửa SQL / biến / đặt lại mốc (ESH-2306), báo trước thay vì để bấm lưu mới lỗi -->
								<el-alert
									v-if="isSuperAdmin && sqlScopeLocked"
									type="warning"
									:closable="false"
									show-icon
									class="mt5 mb5 sql-lock-alert"
								>
									<template #title>
										<span class="sql-lock-alert-text">{{ $t('lz.label.sharedataDataSource.sqlLockedBySubscription', { partners: visibleSqlScopeLockNames }) }}</span>
										<el-button
											v-if="sqlScopeLockPartnerList.length > SQL_LOCK_PARTNERS_VISIBLE_LIMIT"
											size="small"
											text
											type="primary"
											class="sql-lock-toggle"
											@click="sqlLockExpanded = !sqlLockExpanded"
										>
											{{ sqlLockExpanded ? $t('lz.button.base.collapse') : $t('lz.button.sharedataDataSource.partnerUsageShowAll', { count: sqlScopeLockPartnerList.length }) }}
										</el-button>
									</template>
								</el-alert>
								<!-- Hộp cảnh báo lỗi cú pháp / kiểm tra an toàn (đặt ở đầu ngay dưới toolbar) -->
								<el-alert
									v-if="sqlCenterState.validationError"
									:title="formatValidationError(sqlCenterState.validationError)"
									type="error"
									:closable="true"
									show-icon
									class="mt5 mb5 sql-error-alert"
									@close="handleDismissValidationError"
									@click="handleAlertContainerClick"
								/>

								<!-- Cảnh báo gói chưa có câu SQL -->
								<el-alert
									v-if="!sqlCenterState.isLoading && !sqlCenterState.extractSql?.trim()"
									:title="$t('lz.message.sharedataDataSource.missingExtractSqlWarning')"
									type="error"
									:closable="false"
									show-icon
									class="mt5 mb5 missing-extract-sql-alert"
								/>

								<!-- ① SQL trích xuất (Vùng 1) -->
								<div v-if="isSuperAdmin" class="merged-section sql-section" :class="{ 'is-scope-locked': sqlScopeLocked }">
									<div class="tab-pane-container sql-center-pane">
										<div class="sql-center-inner">
											<div class="sql-editor-section">
												<SqlEditor
													ref="sqlEditorRef"
													v-model="sqlCenterState.extractSql"
													:packet-code="state.currentPacket?.code"
													:partner-name="activeSqlTab !== 'standard' ? (currentPartnerItem?.partnerCode || currentPartnerItem?.partnerName) : null"
													:variables="sqlVariables"
													:saved-fields="editorSavedFields"
													:detected-columns="nonSystemDetectedColumns"
													:readonly="sqlScopeLocked"
													@change="handleSqlChangedFromBuilder"
												/>
											</div>
										</div>
									</div>
								</div>

								<!-- ② Danh sách trường (Vùng 2) -->
								<div
									v-if="fieldsMaximized"
									class="fields-maximize-backdrop"
									@click="toggleFieldsMaximized"
								/>
								<div
									class="tab-pane-container saved-fields-pane merged-section fields-section"
									:class="{ 'is-maximized': fieldsMaximized, 'is-collapsed': fieldsBodyCollapsed && !fieldsMaximized, 'is-scope-locked': sqlScopeLocked }"
								>
							<!-- Kết quả lần dò cột gần nhất: thêm / mất / đổi kiểu so với bản đã lưu -->
							<el-alert
								v-if="describeNotice.title"
								:title="describeNotice.title"
								:description="describeNotice.description"
								:type="describeNotice.type"
								show-icon
								class="mb5"
								@close="clearDescribeNotice"
							/>
							<!-- Danh sách trường đọc từ câu SQL, hiện chỉ SuperAdmin xem được -->
							<el-alert
								v-if="!isSuperAdmin"
								:title="$t('lz.label.sharedataDataSource.fieldsSuperAdminOnly')"
								type="info"
								:closable="false"
								show-icon
								class="mb5"
							/>
							<!--
							<div class="search-flat-bar search-card search-card-field">
								<el-form :model="state.queryParamsField" :show-message="false" label-position="left" :inlineMessage="true" class="search-form">
									<el-row :gutter="8">
										<el-col class="mb5 field-col-alias" :xs="24" :sm="12" :md="8" :lg="8" :xl="8">
											<el-form-item :label="$t('lz.entity.sharedataDataSource.aliasFieldKey')">
												<el-input
													v-model="state.queryParamsField.code"
													:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.sharedataDataSource.aliasFieldKey') })"
													@keyup.enter="handleQueryField"
													:maxlength="128"
													show-word-limit
													clearable
												/>
											</el-form-item>
										</el-col>
										<el-col class="mb5 field-col-type" :xs="24" :sm="12" :md="8" :lg="8" :xl="8">
											<el-form-item :label="$t('lz.entity.sharedataDataSource.fieldType')">
												<el-input
													v-model="state.queryParamsField.type"
													:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.sharedataDataSource.fieldType') })"
													@keyup.enter="handleQueryField"
													:maxlength="128"
													show-word-limit
													clearable
												/>
											</el-form-item>
										</el-col>
										<el-col class="mb5 field-col-status" :xs="24" :sm="12" :md="8" :lg="8" :xl="8">
											<el-form-item :label="$t('lz.label.sharedataDataSource.fieldState')">
												<el-select v-model="state.queryParamsField.state" class="w100" clearable @change="handleQueryField">
													<el-option v-for="item in fieldStateOptions" :key="item.value" :label="$t(item.label)" :value="item.value" />
												</el-select>
											</el-form-item>
										</el-col>
										<el-col class="mb5 field-col-btn" :xs="24" :sm="12" :md="8" :lg="8" :xl="8">
											<el-divider class="search-divider" direction="vertical" />
											<div class="search-btn-wrapper">
												<SearchButton
													@search="handleQueryField"
													@reset="resetQueryField"
													:loading="optionsFieldGrid.loading"
												/>
											</div>
										</el-col>
									</el-row>
								</el-form>
							</div>
							-->

							<!-- Thanh tiêu đề đặt ngoài lưới để thu gọn có hiệu ứng như khung soạn SQL -->
							<div class="fields-bar">
								<div class="fields-bar-left" @click="toggleFieldsCollapsed">
									<span class="fields-title">{{ $t('lz.label.sharedataDataSource.fieldsBlockTitle') }}</span>
									<span class="fields-badge-count">{{ state.fieldCount }}</span>
									<span class="fields-hint">— {{ $t('lz.label.sharedataDataSource.fieldsBlockHint') }}</span>
									<!-- Tạm ẩn "Dò lần cuối" cho gọn thanh tiêu đề (yêu cầu 27/10); mở lại: bỏ comment
									<span v-if="fieldsDescribedTime" class="rs-note">{{ $t('lz.label.sharedataDataSource.describedAt', { time: fieldsDescribedTime }) }}</span>
									-->
									<el-tag v-if="fieldsPending || fieldsEdited" size="small" type="warning" effect="plain">{{ $t('lz.label.sharedataDataSource.sqlUnsavedTag') }}</el-tag>
								</div>
								<!-- Chú giải so với mẫu + nút Chỉnh sửa / Thu gọn: một khối bên phải, thiếu chỗ thì cả khối xuống hàng, không đè tiêu đề -->
								<div class="fields-bar-right">
								<!-- Chú giải so với mẫu như wireframe: vòng tròn màu + số lượng -->
								<span v-if="inPartnerEdit && partnerFieldsAnalyzed" class="fields-compare-summary" @click.stop>
									<el-tooltip
										:content="$t('lz.tooltip.sharedataDataSource.compareLegend_same')"
										placement="top"
										popper-class="data-source-tooltip"
									>
										<span class="cmp-legend"><span class="cmp-st same">=</span>{{ partnerFieldCompare.sameCount }} {{ $t('lz.label.sharedataDataSource.compareLegend_same') }}</span>
									</el-tooltip>
									<el-tooltip
										:content="$t('lz.tooltip.sharedataDataSource.compareLegend_new')"
										placement="top"
										popper-class="data-source-tooltip"
									>
										<span class="cmp-legend"><span class="cmp-st new">+</span>{{ partnerFieldCompare.newCount }} {{ $t('lz.label.sharedataDataSource.compareLegend_new') }}</span>
									</el-tooltip>
									<el-tooltip
										:content="$t('lz.tooltip.sharedataDataSource.compareLegend_missing')"
										placement="top"
										popper-class="data-source-tooltip"
									>
										<span class="cmp-legend"><span class="cmp-st missing">-</span>{{ partnerFieldCompare.missingCount }} {{ $t('lz.label.sharedataDataSource.compareLegend_missing') }}</span>
									</el-tooltip>
									<el-tooltip
										:content="$t('lz.tooltip.sharedataDataSource.compareLegend_diff')"
										placement="top"
										popper-class="data-source-tooltip"
									>
										<span class="cmp-legend"><span class="cmp-st diff">≠</span>{{ partnerFieldCompare.diffCount }} {{ $t('lz.label.sharedataDataSource.compareLegend_diff') }}</span>
									</el-tooltip>
								</span>
								<div class="fields-bar-actions">
									<el-tooltip
										v-if="!fieldsMaximized"
										:content="fieldsBodyCollapsed ? $t('lz.button.sharedataDataSource.sqlEditorExpandBtn') : $t('lz.button.sharedataDataSource.sqlEditorCollapseBtn')"
										placement="top"
									>
										<el-button
											text
											size="small"
											type="primary"
											class="fields-collapse-btn"
											:icon="fieldsBodyCollapsed ? 'ele-ArrowDown' : 'ele-ArrowUp'"
											:aria-label="fieldsBodyCollapsed ? $t('lz.button.sharedataDataSource.sqlEditorExpandBtn') : $t('lz.button.sharedataDataSource.sqlEditorCollapseBtn')"
											@click="toggleFieldsCollapsed"
										/>
									</el-tooltip>
								</div>
								</div>
							</div>
							<el-collapse-transition>
							<div v-show="!fieldsBodyCollapsed || fieldsMaximized" class="saved-fields-grid-wrapper" :class="{ 'is-auto-height': fieldGroupsAllCollapsed && !fieldsMaximized }">
								<vxe-grid ref="xGridField" :key="optionsFieldGrid.id" class="xGrid-style" v-bind="optionsFieldGrid" :loading="false" :height="fieldsMaximized ? '100%' : fieldGroupsAllCollapsed ? undefined : 380" :min-height="fieldsMaximized ? undefined : 80" :toolbar-config="{ enabled: false }" v-on="gridEventsField">
									<template #toolbar_buttons>
										<!--
										<el-button
											text
											size="small"
											type="primary"
											class="ml5 fields-maximize-btn"
											:icon="fieldsMaximized ? 'ele-Close' : 'ele-FullScreen'"
											@click="toggleFieldsMaximized"
										>
											{{ fieldsMaximized ? $t('lz.button.sharedataDataSource.restoreFields') : $t('lz.button.sharedataDataSource.maximizeFields') }}
										</el-button>
										-->
									</template>
									<!--
									<template #toolbar_tools>
										<TableHeader
											:show-delete="false"
											:show-add="false"
											@refresh="handleQueryField"
											@custom-column="handleFieldCustom"
										/>
									</template>
									-->
									<template #empty>
										<el-empty :description="state.currentPacket ? $t('lz.label.sharedataDataSource.noPacketField') : $t('lz.label.sharedataDataSource.selectPacketToViewFields')" :image-size="160" />
									</template>
									<template #row_aliasFieldKey="{ row }">
										<span v-if="row.isGroupRow" class="field-group-label">{{ row.name }}</span>
										<span v-else class="table-name" :class="{ 'field-key-lost': row.lost, 'field-key-missing': row.missingFromPartner }">{{ row.aliasFieldKey }}</span>
									</template>
									<template #row_fieldName="{ row }">
										<template v-if="!row.isGroupRow">
											<span>{{ row.name }}</span>
										</template>
									</template>
									<!-- Tạm ẩn theo biên bản 26/10 [07:50]; mở lại: bỏ comment
									<template #row_status="{ row }">
										<template v-if="!row.isGroupRow">
											<el-tooltip v-if="row.lost" :content="$t('lz.label.sharedataDataSource.fieldStateLostHint')" placement="top">
												<el-tag size="small" type="warning" effect="plain">{{ $t('lz.label.sharedataDataSource.fieldStateLost') }}</el-tag>
											</el-tooltip>
											<el-tag v-else-if="row.skip" size="small" type="info" effect="plain">{{ $t('lz.label.sharedataDataSource.fieldStateSkipped') }}</el-tag>
											<el-tag v-else-if="row.partnerOnly" size="small" type="primary" effect="plain">{{ $t('lz.label.sharedataDataSource.fieldStateCustom') }}</el-tag>
											<el-tag v-else size="small" type="success" effect="plain">{{ $t('lz.label.sharedataDataSource.fieldStateActive') }}</el-tag>
										</template>
									</template>
									-->
									<template #row_isRequired="{ row }">
										<template v-if="!row.isGroupRow">
											<el-tag v-if="row.isRequired === 1" size="small" type="danger" effect="plain">{{ $t('lz.label.sharedataDataSource.required') }}</el-tag>
											<el-tag v-else size="small" type="info" effect="plain">{{ $t('lz.label.sharedataDataSource.notRequired') }}</el-tag>
										</template>
									</template>
									<!-- Tạm ẩn theo biên bản 26/10 [07:50]; mở lại: bỏ comment
									<template #row_skip="{ row }">
										<template v-if="!row.isGroupRow">
											<el-checkbox v-model="row.field.skip" :disabled="!canEditField(row)" @change="handleFieldEdited(true)" @click.stop />
										</template>
									</template>
									-->
									<template #row_groupName="{ row }">
										<template v-if="!row.isGroupRow">
											<span>{{ row.groupName }}</span>
										</template>
									</template>
									<template #row_remark="{ row }">
										<template v-if="!row.isGroupRow">
											<span>{{ row.field.remark }}</span>
										</template>
									</template>
									<template #row_compareState="{ row }">
										<div class="cmp-cell-wrapper" v-if="!row.isGroupRow && row.compareState">
											<el-tooltip
												:content="$t(`lz.label.sharedataDataSource.compareState_${row.compareState}`) + (row.compareTooltip ? ': ' + row.compareTooltip : '')"
												placement="top"
												popper-class="data-source-tooltip"
											>
												<span class="cmp-st" :class="row.compareState">{{ compareSymbol(row.compareState) }}</span>
											</el-tooltip>
										</div>
									</template>
									<template #row_buttons="{ row }">
										<template v-if="isSuperAdmin && !row.isGroupRow && !row.lost && !row.missingFromPartner">
											<el-tooltip
												:content="fieldEditLockedReason || $t('lz.button.sharedataDataSource.editFieldsTooltip')"
												placement="top"
												popper-class="data-source-tooltip"
											>
												<span @click.stop>
													<el-button
														class="field-edit-btn"
														icon="ele-Edit"
														text
														type="primary"
														:aria-label="$t('lz.button.sharedataDataSource.editFieldsTooltip')"
														:disabled="Boolean(fieldEditLockedReason)"
														@click.stop="handleOpenEditField(row)"
													/>
												</span>
											</el-tooltip>
										</template>
									</template>
								</vxe-grid>
							</div>
							</el-collapse-transition>
						</div>

						<!-- ③ Tham số câu truy vấn (Vùng 3) -->
						<div v-if="isSuperAdmin" class="merged-section sql-variable-section">
							<SqlVariablePanel
								ref="sqlVariablePanelRef"
								:packet-code="state.currentPacket?.code"
								:partner-id="activeSqlTab !== 'standard' ? currentPartnerItem?.partnerId : null"
								:preload="packetSqlPreload"
								:sql="sqlCenterState.isLoading ? null : sqlCenterState.extractSql"
								:silent-loading="packetSwitching || scopeSwitching || sqlCenterState.isLoading"
								:locked-partner-ids="outboundLockedPartnerIds"
								@variables-change="(items) => (sqlVariables = items)"
								@dirty-change="(d) => (variablesDirty = d)"
								@apply-sql="(sql) => (sqlCenterState.extractSql = sql)"
								@cursor-vars-change="(v) => (sqlHasCursor = v)"
							/>
						</div>

						<!-- ④ Chiều gửi: mốc đang chạy của từng đối tác trong phạm vi tab (chỉ khi câu SQL dùng mốc) -->
						<div v-if="isSuperAdmin && sqlHasCursor" class="merged-section cursor-block-section">
							<CursorBlock
								ref="cursorBlockRef"
								:packet-code="state.currentPacket?.code"
								:partner-id="activeSqlTab !== 'standard' ? currentPartnerItem?.partnerId : null"
								:partners="partnerSqlList"
								:locked-partner-ids="outboundLockedPartnerIds"
								@cursor-change="sqlVariablePanelRef?.refreshCursorState?.()"
							/>
						</div>
					</div>
					</div>

						<!-- ④ Ghi ngược (chiều nhận): câu ghi đi theo gói (không theo đối tác), nằm ở tab Chiều nhận -->
						<div v-if="isSuperAdmin" v-show="directionTab === 'receive'" class="merged-section write-sql-section">
							<WriteSqlPanel
								ref="writeSqlPanelRef"
								:packet-code="state.currentPacket?.code"
								:fields="sqlFields"
								:preload="packetSqlPreload"
								:silent-loading="packetSwitching || scopeSwitching || sqlCenterState.isLoading"
								:locked-subs="writeSqlLockSubs"
								@saved="handleWriteSqlSaved"
							/>
						</div>
					</div>
				</el-card>
				</div>
			</pane>
			<!-- Chi tiết gói tin: end -->
		</splitpanes>

		<!-- Components: begin -->
		<EditPacketComp ref="editPacketCompRef" :title="state.title" :operate-type="state.operateType" :refresh-grid="() => handleQueryPacket(true)" @handleQuery="() => handleQueryPacket(true)" />
		<SqlDiffDialog
			v-model="diffDialogVisible"
			:partner-id="diffDialogData.partnerId"
			:packet-code="diffDialogData.packetCode"
			:partner-name="diffDialogData.partnerName"
			:standard-sql="diffDialogData.standardSql"
			:custom-sql="diffDialogData.customSql"
			:common-fields="diffDialogData.commonFields"
			:partner-fields="diffDialogData.partnerFields"
			:locked="Boolean(diffDialogData.partnerId) && outboundLockedPartnerIds.includes(diffDialogData.partnerId)"
			@success="handleDiffSuccess"
		/>
		<CompareAllDialog
			v-model="compareAllDialogVisible"
			:packet-code="state.currentPacket?.code || ''"
			:partners="partnerSqlList"
			:common-fields="commonFieldsList"
			:resetting="partnerBulkResetting"
			@open-diff="handleOpenDiffFromCompareAll"
			@open-tab="handleOpenTabFromCompareAll"
			@reset-many="handleResetManyFromCompareAll"
		/>
		<PartnerSetupDialog
			v-model="partnerSetupDialogVisible"
			:packet-code="state.currentPacket?.code || ''"
			:partners="partnerSqlList"
			:loading="partnerSetupCreating"
			@confirm="handleConfirmPartnerSetup"
		/>
		<FieldEditDialog
			v-model="fieldEditDialogVisible"
			:packet-code="state.currentPacket?.code || ''"
			:partner-name="currentPartnerItem ? currentPartnerItem.partnerCode || currentPartnerItem.partnerName : null"
			:field="editingField"
			:groups="existingFieldGroups"
			:saving="sqlCenterState.isSaving"
			@save="handleSaveFieldEdits"
		/>
		<!-- Components: end -->
	</div>
	</WorkspacePage>
</template>

<script lang="ts" setup name="eshDataSource">
import { computed, defineAsyncComponent, h, nextTick, onActivated, onMounted, onUnmounted, reactive, ref, shallowRef, watch } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import WorkspacePage from '/@/components/workspace/WorkspacePage.vue';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { Splitpanes, Pane } from 'splitpanes';
import 'splitpanes/dist/splitpanes.css';
import { getAPI } from '/@/utils/axios-utils';
import { useI18n } from 'vue-i18n';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import { ShareDataPacketApi, ShareDataPartnerApi, StatusEnum } from '/@/api-services/shareData';
import type { PacketSqlVariableItemInput } from '/@/api-services/shareData';
import { type PacketSqlPreload, createPacketSqlPreload, takePacketSqlPreload } from '/@/utils/sharedata/packetSqlPreload';
import { CursorResetPartner, PacketExtractSqlOutput, ShareDataPagePacketInput, ShareDataPagePacketOutput, SqlFieldEditInput, SqlFieldItem, SqlFieldMappingUsage } from '/@/api-services/shareData';
import { OperateType } from '/@/types/constants';
import { useBaseConfig } from '/@/stores/baseConfig';
import { useUserInfo } from '/@/stores/userInfo';
import { debounce } from 'lodash-es';
import { BaseConfigTypeEnum, SharedataDirectionEnum } from '/@/types/enums';
import type { ShareDataSubscriptionOutput } from '/@/api-services/shareData';
import { isPacketInUse, listActiveSubscriptions } from '/@/utils/sharedata/packetUsage';
import { compareFields, type FieldCompareRow, type FieldCompareState } from '/@/utils/sharedata/fieldCompare';
import { optimisticRemoveGridRows, silentSyncGrid } from '/@/utils/sharedata/gridSync';
const { t: $t, te: $te } = useI18n();

const getConfigItemByCode = useBaseConfig().getConfigItemByCode;

const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
import EditPacketComp from './component/editPacket.vue';
import SqlEditor from './component/sqlEditor.vue';
import SqlVariablePanel from './component/sqlVariablePanel.vue';
import CursorBlock from './component/cursorBlock.vue';
import WriteSqlPanel from './component/writeSqlPanel.vue';
import { Local } from '/@/utils/storage';
const SqlDiffDialog = defineAsyncComponent(() => import('/@/views/sharedata/dataSource/component/sqlDiffDialog.vue'));
// Tạm ẩn theo biên bản 26/10; mở lại: bỏ comment
// const PartnerSqlTable = defineAsyncComponent(() => import('/@/views/sharedata/dataSource/component/partnerSqlTable.vue'));
const CompareAllDialog = defineAsyncComponent(() => import('/@/views/sharedata/dataSource/component/compareAllDialog.vue'));
const PartnerSetupDialog = defineAsyncComponent(() => import('/@/views/sharedata/dataSource/component/partnerSetupDialog.vue'));
const FieldEditDialog = defineAsyncComponent(() => import('/@/views/sharedata/dataSource/component/fieldEditDialog.vue'));

const xGridPacket = ref<VxeGridInstance>();
const xGridField = ref<VxeGridInstance>();
const packetDetailsRef = ref<HTMLElement>();

// Tối ưu hiệu năng khi kéo resize splitpanes / cửa sổ:
// autoResize được tắt trên grid để chặn vxe-table tự động kích hoạt ResizeObserver trên từng frame mousemove,
// ngăn chặn hoàn toàn hiện tượng Forced Synchronous Layout / Layout Thrashing (tụt FPS).
// Layout của 2 bảng được cập nhật đồng bộ một lần khi người dùng nhả chuột (@resized) hoặc sau khi resize cửa sổ.
let windowResizeTimer: number | null = null;
const handleWindowResize = () => {
	if (windowResizeTimer !== null) clearTimeout(windowResizeTimer);
	windowResizeTimer = window.setTimeout(() => {
		windowResizeTimer = null;
		requestAnimationFrame(() => {
			xGridPacket.value?.recalculate(true);
			xGridField.value?.recalculate(true);
		});
	}, 100);
};

const handleSplitterResized = (payload?: { event?: Event; panes?: { size: number }[] }) => {
	// Giữ cỡ người dùng vừa kéo và lưu localStorage để tải lại trang vẫn giữ nguyên
	const firstSize = payload?.event ? payload.panes?.[0]?.size : undefined;
	if (firstSize) {
		const clamped = Math.min(65, Math.max(25, firstSize));
		packetPaneSize.value = clamped;
		Local.set(PACKET_PANE_SIZE_KEY, clamped);
	}
	requestAnimationFrame(() => {
		xGridPacket.value?.recalculate(true);
		xGridField.value?.recalculate(true);
	});
};

// Bảng gói tin bên trái mặc định đủ rộng để thấy hết các cột (tên gói dài thì cắt "…"); splitpanes tính theo %, nên đổi px sang % theo bề rộng trang lúc mở.
const PACKET_PANE_DEFAULT_PX = 640;
const PACKET_PANE_SIZE_KEY = 'eshPacketPaneSize_v1';
const datasourceContainerRef = ref<HTMLElement>();
const savedPacketPaneSize = Number(Local.get(PACKET_PANE_SIZE_KEY));
const packetPaneSize = ref(Number.isFinite(savedPacketPaneSize) && savedPacketPaneSize > 0 ? Math.min(65, Math.max(25, savedPacketPaneSize)) : 42);
const applyDefaultPacketPaneSize = () => {
	const saved = Number(Local.get(PACKET_PANE_SIZE_KEY));
	if (Number.isFinite(saved) && saved > 0) {
		packetPaneSize.value = Math.min(65, Math.max(25, saved));
	} else {
		const width = datasourceContainerRef.value?.clientWidth;
		if (!width) return;
		// Giữ trong khoảng min-size của hai pane (25% trái, 35% phải)
		packetPaneSize.value = Math.min(65, Math.max(25, (PACKET_PANE_DEFAULT_PX / width) * 100));
	}
	// Lưới tắt autoResize: đổi cỡ pane xong phải tính lại bề rộng cột, nếu không còn khoảng trắng bên phải bảng
	nextTick(() => handleSplitterResized());
	setTimeout(() => handleSplitterResized(), 300);
};

const editPacketCompRef = ref<InstanceType<typeof EditPacketComp>>();
const sqlEditorRef = ref<InstanceType<typeof SqlEditor>>();
const sqlVariablePanelRef = ref<InstanceType<typeof SqlVariablePanel>>();
const cursorBlockRef = ref<InstanceType<typeof CursorBlock>>();
/** Câu SQL của tab đang mở có dùng mốc: hiện khối 4 Mốc đang chạy. */
const sqlHasCursor = ref(false);
/** Cấu hình SQL mẫu chung của gói đang mở, nạp một lần bằng GetSqlDetail và chia cho khung SQL, bảng tham số, khối câu ghi. */
const packetSqlPreload = shallowRef<PacketSqlPreload | null>(null);
const writeSqlPanelRef = ref<InstanceType<typeof WriteSqlPanel>>();
/** Biến đang khai ở bảng tham số câu truy vấn: ô soạn SQL chỉ gợi ý @ theo danh sách này. */
const sqlVariables = ref<{ code: string; desc?: string }[]>([]);
const variablesDirty = ref(false);

const userInfoStore = useUserInfo();
const isSuperAdmin = computed(() => {
	const u = userInfoStore.userInfos;
	return String(u?.accountType) === '111' || u?.accountType === 'SuperAdmin' || u?.accountType == 111 || u?.account === 'superadmin' || u?.account === 'admin';
});

// Lưu danh sách ID nhóm đang được mở để giữ nguyên trạng thái collapse khi tìm kiếm hoặc nạp lại
const expandedGroupIds = ref<string[]>([]);

/*
 * Danh sách trường do câu SQL đang mở sinh ra (mẫu chung hoặc SQL riêng của đối tác).
 * Nạp theo câu SQL đã lưu; bấm "Dò cột trả về" thì thay bằng kết quả dò câu đang soạn (chưa ghi).
 * Khoá và kiểu do bộ dò điền; nhãn, bắt buộc, bỏ qua, nhóm, ghi chú sửa ngay trên bảng. Bấm Lưu mới ghi cùng câu SQL.
 */
const sqlFields = ref<SqlFieldItem[]>([]);
// Danh sách và khoá của trường mẫu chung, để so sánh và đánh dấu trường chỉ SQL riêng của đối tác có
const commonFieldsList = ref<SqlFieldItem[]>([]);
const commonFieldKeys = ref<Set<string>>(new Set());
const partnerFieldCompare = computed(() => compareFields(commonFieldsList.value, sqlFields.value));
// SQL riêng chưa phân tích (chưa có trường nào): bảng rỗng, chưa so với mẫu nên không hiện ký hiệu, dòng thiếu và chú giải
const partnerFieldsAnalyzed = computed(() => sqlFields.value.some((f) => !f.lost));
const fieldsDescribedAt = ref<string | Date | null>(null);
// Kết quả dò đang khác bản đã lưu (thêm / mất / đổi kiểu), cần Lưu để ghi
const fieldsPending = ref(false);
// Người dùng đã sửa nhãn / bắt buộc / bỏ qua / nhóm / ghi chú của trường, cần Lưu để ghi
const fieldsEdited = ref(false);
const describeNotice = reactive({ title: '', description: '', type: 'success' as 'success' | 'warning' | 'info' });
const clearDescribeNotice = () => {
	describeNotice.title = '';
	describeNotice.description = '';
};
const fieldsDescribedTime = computed(() => (fieldsDescribedAt.value ? new Date(fieldsDescribedAt.value).toLocaleString('vi-VN') : ''));

// Gợi ý khoá khi gõ AS trên trình soạn SQL
const editorSavedFields = computed(() =>
	sqlFields.value.filter((f) => !f.lost && f.key).map((f) => ({ aliasFieldKey: f.key!, name: f.label || f.key!, fieldType: f.type || undefined }))
);

/** Thay danh sách trường đang hiện; dò xong mà chưa lưu thì đánh dấu pending. */
const setSqlFields = (fields?: SqlFieldItem[] | null, describedAt?: string | Date | null, pending = false, silent = false) => {
	sqlFields.value = (fields || []).filter((f) => f?.key);
	fieldsDescribedAt.value = describedAt ?? null;
	fieldsPending.value = pending;
	fieldsEdited.value = false;
	if (state.currentPacket) void handleQueryField(silent);
};

/** Phần người dùng khai trên từng trường, gửi kèm khi lưu câu SQL (máy chủ dò lại cột rồi áp lên danh sách trường). */
const buildFieldEdits = (): SqlFieldEditInput[] =>
	sqlFields.value
		.filter((f) => f.key)
		.map((f) => ({
			key: f.key!,
			label: f.label?.trim() || null,
			required: Boolean(f.required),
			skip: Boolean(f.skip),
			group: f.group?.trim() || null,
			remark: f.remark?.trim() || null,
			// Chỉ gửi kiểu khi người dùng đã tự chọn; null = BE giữ nguyên kiểu đang có
			type: f.typeOverride || null,
		}));

const state = reactive({
	title: '',
	operateType: 'add' as OperateType,
	fieldCount: 0,

	// Gói tin đang chọn ở cột bên trái — dùng cho tiêu đề và điều kiện nạp field.
	currentPacket: undefined as ShareDataPagePacketOutput | undefined,

	queryParamsPacket: {
		name: undefined,
		code: undefined,
		partnerId: undefined as string | undefined,
	},

	queryParamsField: {
		code: undefined as string | undefined,
		type: undefined as string | undefined,
		state: undefined as FieldState | undefined,
		datatypeId: undefined as string | undefined,
	},

	localPageParamPacket: {
		pageSize: 50 as number,
		defaultSort: { field: 'orderNo', order: 'asc', descStr: 'desc' } as any,
	},

	localPageParamField: {
		pageSize: 50 as number,
		defaultSort: { field: 'createTime', order: 'desc', descStr: 'desc' } as any,
	},
});

/* Partner SQL tabs state */
export interface PartnerSqlItem {
	partnerId: string;
	partnerCode?: string;
	partnerName: string;
	isCustom: boolean;
	updateTime?: string | null;
}

const partnerSqlList = ref<PartnerSqlItem[]>([]);
const partnerTabOrder = ref<string[]>([]);
let partnerTabOrderPacketCode: string | null = null;
const partnerSqlLoading = ref(false);
const partnerBulkResetting = ref(false);
const activeSqlTab = ref<string>('standard');
let currentLoadedSqlTab = 'standard';
const activePartnerDetail = ref<any>(null);
const currentPartnerItem = computed(() => {
	if (activeSqlTab.value === 'standard') return null;
	return partnerSqlList.value.find((p) => p.partnerId === activeSqlTab.value) || null;
});

/* ---- Khoá cấu hình khi gói còn đăng ký đang chạy (khớp BE ESH-2306) ----
 * Chiều gửi (câu trích xuất, biến, mốc gửi): tab Mẫu chung khoá khi còn bất kỳ đăng ký gửi nào chạy,
 * tab đối tác chỉ khoá khi đăng ký gửi của đúng đối tác đó đang chạy. Chiều nhận (câu ghi): khoá khi còn đăng ký nhận chạy.
 * Đăng ký khai Both tính cho cả hai chiều. */
const packetActiveSubs = ref<ShareDataSubscriptionOutput[]>([]);
const loadPacketActiveSubs = async (packet?: { id?: string | null; code?: string | null } | null) => {
	packetActiveSubs.value = [];
	if (!packet?.id) return;
	try {
		const subs = await listActiveSubscriptions(packet.id, packet.code);
		if (state.currentPacket?.id === packet.id) packetActiveSubs.value = subs;
	} catch {
		/* Không tải được thì không khoá; BE vẫn chặn khi lưu. */
	}
};
const subsOfDirection = (direction: SharedataDirectionEnum) =>
	packetActiveSubs.value.filter((s) => Number(s.direction) === direction || Number(s.direction) === SharedataDirectionEnum.Both);
const outboundLockedPartnerIds = computed(() => [...new Set(subsOfDirection(SharedataDirectionEnum.Outbound).map((s) => s.partnerId!))]);
/** Tên đối tác của đăng ký: danh sách đăng ký không trả kèm tên nên tra trong danh sách đối tác, không có mới hiện ID. */
const subPartnerName = (s: ShareDataSubscriptionOutput) => {
	const p = partnerOptions.value.find((x) => x.id === s.partnerId);
	return s.partnerName || s.partnerCode || p?.name || p?.code || s.partnerId || '';
};
const subPartnerNames = (subs: ShareDataSubscriptionOutput[]) => [...new Set(subs.map(subPartnerName))].join(', ');
/** Tên đối tác đang chặn sửa phần chiều gửi đang mở (tab Mẫu chung = mọi đối tác, tab đối tác = đúng đối tác đó); rỗng = không khoá. */
const sqlScopeLockNames = computed(() => {
	const partnerId = currentPartnerItem.value?.partnerId;
	const subs = subsOfDirection(SharedataDirectionEnum.Outbound).filter((s) => !partnerId || s.partnerId === partnerId);
	return subPartnerNames(subs);
});
const sqlScopeLocked = computed(() => Boolean(sqlScopeLockNames.value));
/** Đăng ký nhận đang chạy — câu ghi mẫu chung khoá khi còn bất kỳ, câu ghi riêng chỉ khoá theo đúng đối tác (writeSqlPanel tự lọc). */
const writeSqlLockSubs = computed(() =>
	subsOfDirection(SharedataDirectionEnum.Inbound).map((s) => ({ partnerId: s.partnerId, name: subPartnerName(s) }))
);
const warnSqlLocked = (names: string): void => {
	ElMessage.warning($t('lz.label.sharedataDataSource.sqlLockedBySubscription', { partners: names }));
};
// Chỉ đối tác có SQL riêng mới có tab (đối tác dùng thẳng mẫu chung xem ở dòng "Đối tác dùng" dưới tên gói); đối tác mới mở/tạo luôn xếp cuối
const displayedPartnerTabs = computed(() => {
	const tabs = partnerSqlList.value.filter((p) => p.isCustom);
	return [...tabs].sort((a, b) => {
		const ia = partnerTabOrder.value.indexOf(a.partnerId);
		const ib = partnerTabOrder.value.indexOf(b.partnerId);
		const oa = ia === -1 ? Number.MAX_SAFE_INTEGER : ia;
		const ob = ib === -1 ? Number.MAX_SAFE_INTEGER : ib;
		return oa - ob;
	});
});
const customPartnerCount = computed(() => partnerSqlList.value.filter((p) => p.isCustom).length);
const hasUnconfiguredPartners = computed(() => partnerSqlList.value.some((p) => !p.isCustom));

const isPartnerOutdated = computed(() => {
	if (!activePartnerDetail.value) return false;
	const baseVer = activePartnerDetail.value.baseVersion;
	const curVer = activePartnerDetail.value.templateVersion;
	if (baseVer != null && curVer != null) {
		return baseVer < curVer || Boolean(activePartnerDetail.value.hasDifferences);
	}
	return Boolean(activePartnerDetail.value.hasDifferences);
});

const partnerVersionStatusText = computed(() => {
	if (!currentPartnerItem.value) return '';
	const baseVer = activePartnerDetail.value?.baseVersion;
	const curVer = activePartnerDetail.value?.templateVersion;
	if (isPartnerOutdated.value) {
		return $t('lz.label.sharedataDataSource.partnerVersionOutdated', { base: baseVer, current: curVer });
	}
	return $t('lz.label.sharedataDataSource.partnerVersionSynced', { base: baseVer });
});

const commonToolbarMetaText = computed(() => {
	const customCount = customPartnerCount.value;
	return $t('lz.label.sharedataDataSource.commonToolbarMeta', { custom: customCount, template: partnerSqlList.value.length - customCount });
});

const directionTab = ref<'send' | 'receive'>('send');
const receiveTabLabel = computed(() => {
	const c = writeSqlPanelRef.value?.count || state.currentPacket?.writeSqlCount || 0;
	return c > 0
		? $t('lz.label.sharedataDataSource.tabReceiveWithCount', { count: c })
		: $t('lz.label.sharedataDataSource.tabReceiveSendOnly');
});

const compareAllDialogVisible = ref(false);
const partnerSetupDialogVisible = ref(false);
const partnerSetupCreating = ref(false);
const fieldEditDialogVisible = ref(false);
const editingField = ref<SqlFieldItem | null>(null);

const existingFieldGroups = computed<string[]>(() => {
	const set = new Set<string>();
	const defaultGroupName = $t('lz.label.sharedataDataSource.ungroupedFields');
	if (defaultGroupName) set.add(defaultGroupName);
	for (const f of sqlFields.value) {
		const g = f.group?.trim();
		if (g) set.add(g);
	}
	for (const f of commonFieldsList.value) {
		const g = f.group?.trim();
		if (g) set.add(g);
	}
	return Array.from(set);
});

const handleOpenEditField = (row: FieldRow) => {
	if (!row?.field) return;
	editingField.value = row.field;
	fieldEditDialogVisible.value = true;
};

const diffDialogVisible = ref(false);
const diffDialogData = reactive({
	partnerId: '',
	packetCode: '',
	partnerName: '',
	standardSql: '',
	customSql: '',
	commonFields: [] as SqlFieldItem[],
	partnerFields: [] as SqlFieldItem[],
});

/* SQL Center state (W0.2) */
const sqlCenterState = reactive({
	isLoading: false,
	isValidating: false,
	isSaving: false,
	extractSql: '',
	savedExtractSql: '',
	/** Nội dung vừa nạp lên khung soạn thảo; mốc để biết người dùng đã sửa hay chưa khi đổi tab. */
	loadedExtractSql: '',
	status: 1,
	remark: '',
	validationError: '',
});


// Cột của câu SQL theo danh sách trường đang hiện (bỏ trường đã mất khỏi câu SQL), làm chip cột trên trình soạn
const nonSystemDetectedColumns = computed(() =>
	sqlFields.value.filter((f) => !f.lost && f.key).map((f) => ({ fieldKey: f.key!, name: f.label || f.key!, dataType: f.type || undefined }))
);

/* Mẫu chung và SQL riêng theo đối tác */
// common: sửa mẫu chung; partners: bảng đối tác; edit: sửa SQL riêng của một đối tác.
const sqlView = ref<'common' | 'partners' | 'edit'>('common');

const normalizeSqlText = (sql?: string) => (sql || '').replace(/\r\n?/g, '\n').trim();
const sqlIsDirty = computed(() => normalizeSqlText(sqlCenterState.extractSql) !== normalizeSqlText(sqlCenterState.loadedExtractSql));

const currentPartnerStatus = computed(() => {
	const item = currentPartnerItem.value;
	if (!item?.isCustom) return { type: 'info', label: 'lz.label.sharedataDataSource.tagSharedSql' } as const;
	return { type: 'primary', label: 'lz.label.sharedataDataSource.tagCustomSql' } as const;
});

// Đang mở một đối tác chưa có SQL riêng: bản sao của mẫu chung chính là SQL riêng đầu tiên cần được tạo.
const creatingPartnerSql = computed(() => sqlView.value === 'edit' && Boolean(currentPartnerItem.value) && !currentPartnerItem.value?.isCustom);
const fieldEditLockedReason = computed(() => {
	if (sqlScopeLocked.value) return $t('lz.label.sharedataDataSource.sqlLockedBySubscription', { partners: sqlScopeLockNames.value });
	return sqlIsDirty.value || fieldsPending.value ? $t('lz.tooltip.sharedataDataSource.editFieldsLockedUnsavedSql') : '';
});
// Nút Lưu dùng chung cho câu SQL, danh sách trường và bảng tham số của tab Chiều gửi: mặc định khóa, mở khi một phần đã đổi.
// Ngoại lệ: "Tạo SQL riêng" lần đầu luôn bấm được, kể cả khi còn giống hệt mẫu chung.
// Ngoại lệ: câu SQL đã có (vd. nạp bằng seed) nhưng chưa từng phân tích ra trường nào thì vẫn bấm được để phân tích.
const sqlNeedsAnalyze = computed(() => Boolean(sqlCenterState.extractSql?.trim()) && sqlFields.value.length === 0 && !sqlCenterState.isLoading);
/** Trích xuất cờ isDirty boolean từ ref component (hỗ trợ cả ref unwrapped lẫn ComputedRef/RefImpl). */
const isPanelDirty = (panelRef: any): boolean => {
	if (!panelRef) return false;
	const val = panelRef.isDirty;
	if (typeof val === 'function') return Boolean(val());
	if (val && typeof val === 'object' && 'value' in val) return Boolean(val.value);
	return Boolean(val);
};

const saveDisabled = computed(
	() =>
		!sqlNeedsAnalyze.value &&
		!sqlIsDirty.value &&
		!fieldsPending.value &&
		!fieldsEdited.value &&
		!creatingPartnerSql.value &&
		!variablesDirty.value &&
		!isPanelDirty(sqlVariablePanelRef.value)
);

/* Gói tin: begin ======================== */

type PacketUsageState = 'checking' | 'inUse' | 'available' | 'error';
const packetUsage = reactive<Record<string, PacketUsageState>>({});
let packetUsageVersion = 0;
const packetUsageReady = (id?: string | null) => Boolean(id) && (packetUsage[id!] === 'inUse' || packetUsage[id!] === 'available');
const packetActionHint = (id?: string | null, action: 'edit' | 'delete' = 'edit') => {
	if (!id) return $t(`lz.button.base.${action}`);
	if (packetUsage[id] === 'error') return $t('lz.message.sharedataDataSource.usageCheckFailed');
	if (!packetUsageReady(id)) return $t('lz.label.sharedataDataSource.checkingUsage');
	if (packetUsage[id] === 'inUse') return $t(action === 'edit'
		? 'lz.label.sharedataDataSource.packetIdentityLocked' : 'lz.label.sharedataDataSource.packetDeleteLocked');
	return $t(`lz.button.base.${action}`);
};

const checkPacketUsage = async (id: string, version = packetUsageVersion, silent = false) => {
	if (!silent && version === packetUsageVersion) packetUsage[id] = 'checking';
	try {
		const used = await isPacketInUse(id);
		if (version === packetUsageVersion) packetUsage[id] = used ? 'inUse' : 'available';
		return used;
	} catch (error) {
		if (version === packetUsageVersion && (!silent || !packetUsage[id])) packetUsage[id] = 'error';
		throw error;
	}
};

const optionsPacketGrid = useVxeTable<ShareDataPagePacketOutput>(
	{
		// Đổi id khi đổi bề rộng cột mặc định: bề rộng cột được lưu localStorage theo id, giữ id cũ thì máy người dùng vẫn dùng bề rộng cũ
		id: 'eshPacketGrid_v2',
		name: 'eshPacketGrid',
		columns: [
			{ type: 'seq', title: $t('lz.label.sharedataAlertLog.seq'), width: 45, fixed: 'left' },
			{ field: 'name', title: $t('lz.entity.sharedataDataSource.packetName'), minWidth: 100, align: 'left', headerAlign: 'center', showOverflow: 'tooltip', sortable: true, resizable: true },
			{ field: 'code', title: $t('lz.entity.sharedataDataSource.packetCode'), width: 120, align: 'left', headerAlign: 'center', showOverflow: 'tooltip', sortable: true, resizable: true, slots: { default: 'row_code' } },
			{ field: 'partnerCount', title: $t('lz.label.sharedataDataSource.partnerColumn'), width: 80, align: 'center', headerAlign: 'center', resizable: true, slots: { default: 'row_partnerCount' } },
			// Chỉ hiện khi đang lọc theo đối tác (xem handleQueryPacketApi)
			{ field: 'sqlSource', title: $t('lz.label.sharedataDataSource.sqlSourceColumn'), width: 105, align: 'center', headerAlign: 'center', resizable: true, visible: false, slots: { default: 'row_sqlSource' } },
			{ field: 'writeSqlCount', title: $t('lz.label.sharedataWriteSql.packetColumn'), width: 80, align: 'center', headerAlign: 'center', resizable: true, slots: { default: 'row_writeSql' } },
			{ field: 'infoClm', title: $t('lz.label.base.audit'), width: 80, align: 'center', headerAlign: 'center', showOverflow: 'tooltip', resizable: true, slots: { default: 'row_record' } },
			{ field: 'actionClm', title: $t('lz.label.base.action'), fixed: 'right', width: 108, align: 'center', headerAlign: 'center', showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	{
		height: '100%',
		autoResize: true,
		showOverflow: 'tooltip',
		showHeaderOverflow: 'tooltip',
		columnConfig: { isCurrent: true, resizable: true },
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryPacketApi(page, sort) } },
		sortConfig: { defaultSort: state.localPageParamPacket.defaultSort },
		pagerConfig: {
			pageSize: state.localPageParamPacket.pageSize,
			layouts: ['PrevPage', 'JumpNumber', 'NextPage', 'Sizes', 'Total'],
		},
		toolbarConfig: { export: false },
		rowConfig: { isHover: true, isCurrent: true, keyField: 'id' },
		tooltipConfig: { popupClassName: 'data-source-tooltip' },
	}
);

const gridEventsPacket: VxeGridListeners<ShareDataPagePacketOutput> = {
	async pageChange({ pageSize }) {
		state.localPageParamPacket.pageSize = pageSize;
	},
	async sortChange({ field, order }) {
		state.localPageParamPacket.defaultSort = { field: field, order: order!, descStr: 'desc' };
	},
};

const handleQueryPacketApi = async (
	page: VxeGridPropTypes.ProxyAjaxQueryPageParams,
	sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams,
	silent = false
) => {
	const version = ++packetUsageVersion;
	if (!silent) {
		for (const id of Object.keys(packetUsage)) delete packetUsage[id];
	}
	const defaultSort = state.localPageParamPacket.defaultSort;
	const params: ShareDataPagePacketInput = {
		...state.queryParamsPacket,
		partnerId: state.queryParamsPacket.partnerId || undefined,
		page: page.currentPage,
		pageSize: page.pageSize,
		field: sort.field || defaultSort?.field || 'orderNo',
		order: sort.order || defaultSort?.order || 'asc',
		descStr: 'desc',
	};

	const response = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketPagePost(params);
	const rows = response.data.result?.records ?? [];
	// Thứ tự mặc định: tận dụng createTime tăng dần, kết hợp orderNo và mã gói (101 -> 110) để khi lọc hoặc xóa lọc thứ tự luôn nhất quán
	if (rows.length > 0 && (!sort.field || sort.field === 'orderNo')) {
		rows.sort((a, b) => {
			const timeA = a.createTime ? new Date(a.createTime).getTime() : 0;
			const timeB = b.createTime ? new Date(b.createTime).getTime() : 0;
			if (timeA && timeB && timeA !== timeB) return timeA - timeB;
			if (a.orderNo != null && b.orderNo != null && a.orderNo !== b.orderNo) return a.orderNo - b.orderNo;
			const codeCmp = (a.code || '').localeCompare(b.code || '', undefined, { numeric: true, sensitivity: 'base' });
			if (codeCmp !== 0) return codeCmp;
			return (a.name || '').localeCompare(b.name || '', undefined, { numeric: true, sensitivity: 'base' });
		});
	}
	if (silent && version === packetUsageVersion) {
		const rowIds = new Set(rows.map((r) => r.id).filter(Boolean));
		for (const id of Object.keys(packetUsage)) {
			if (!rowIds.has(id)) delete packetUsage[id];
		}
	}
	// Chỉ kiểm tra trang hiện tại, tối đa 4 gói tin đồng thời; khi đồng bộ nền chỉ kiểm tra gói mới chưa có trạng thái và không chặn hiển thị bảng.
	const rowsToCheck = silent ? rows.filter((r) => r.id && !packetUsageReady(r.id)) : rows;
	if (silent && version === packetUsageVersion) {
		for (const r of rowsToCheck) {
			if (r.id && !packetUsage[r.id]) packetUsage[r.id] = 'available';
		}
	}
	let next = 0;
	const usagePromise = Promise.all(Array.from({ length: Math.min(4, rowsToCheck.length) }, async () => {
		while (next < rowsToCheck.length && version === packetUsageVersion) {
			const id = rowsToCheck[next++]?.id;
			if (!id) continue;
			try { await checkPacketUsage(id, version, silent); }
			catch { /* Tooltip báo chưa kiểm tra được; không mở thao tác dựa trên dữ liệu thiếu. */ }
		}
	}));
	if (!silent) await usagePromise;
	// Tải chậm theo biên bản 27/10: không tự chọn gói đầu tiên để nạp chi tiết; khung phải hiện khung sườn, bấm chọn gói mới gọi API chi tiết.
	// Mở lại tự chọn gói đầu: bỏ comment
	// if (rows.length > 0 && !state.currentPacket) {
	// 	const firstRow = rows[0];
	// 	nextTick(async () => {
	// 		if (!state.currentPacket) {
	// 			xGridPacket.value?.setCurrentRow(firstRow);
	// 			await handleFieldByPacket({ row: firstRow });
	// 		}
	// 	});
	// }
	nextTick(async () => {
		// Cột Nguồn SQL chỉ có nghĩa khi đã chọn đối tác, khi đó ẩn cột Đối tác
		if (state.queryParamsPacket.partnerId) {
			await xGridPacket.value?.showColumn('sqlSource');
			await xGridPacket.value?.hideColumn('partnerCount');
		} else {
			await xGridPacket.value?.hideColumn('sqlSource');
			await xGridPacket.value?.showColumn('partnerCount');
		}
		xGridPacket.value?.recalculate(true);
	});
	return response;
};

const handleQueryPacket = async (silent: boolean | unknown = false) => {
	if (silent === true && xGridPacket.value) {
		await silentSyncGrid<ShareDataPagePacketOutput>({
			grid: xGridPacket.value as any,
			queryApi: (page, sort) => handleQueryPacketApi(page, sort, true),
			defaultSort: state.localPageParamPacket.defaultSort,
			onAfterSync: (records) => {
				if (!state.currentPacket?.id) return;
				const match = records.find((r) => r.id === state.currentPacket?.id);
				if (match) {
					Object.assign(state.currentPacket, match);
					xGridPacket.value?.setCurrentRow(match);
				}
			},
		});
		return;
	}
	await xGridPacket.value?.commitProxy('reload');
};

// Open add dialog
const handleAddPacket = () => {
	state.title = $t('lz.label.base.titleAdd', { name: $t('lz.label.sharedataDataSource.packetTitle') });
	state.operateType = 'add';
	editPacketCompRef.value?.openDialog({ orderNo: 101, status: 1 });
};

// Open copy dialog
const handleCopyPacket = (row: any) => {
	state.title = $t('lz.label.base.titleCopy', { name: $t('lz.label.sharedataDataSource.packetTitle') });
	state.operateType = 'copy';
	editPacketCompRef.value?.openDialog(row);
};

// Open edit dialog
const handleEditPacket = async (row: ShareDataPagePacketOutput) => {
	const packetId = row.id;
	if (!packetId || packetUsage[packetId] === 'checking') return;
	try {
		const identityLocked = await checkPacketUsage(packetId);
		state.title = $t('lz.label.base.titleEdit', { name: $t('lz.label.sharedataDataSource.packetTitle') });
		state.operateType = 'edit';
		editPacketCompRef.value?.openDialog(row, identityLocked);
	} catch { /* Lỗi API đã được interceptor hiển thị, tooltip cung cấp cách thử lại. */ }
};

// Delete packet
const handleDeletePacket = async (row: ShareDataPagePacketOutput) => {
	const packetId = row.id;
	if (!packetId || packetUsage[packetId] === 'checking') return;
	try {
		if (await checkPacketUsage(packetId)) {
			ElMessage.warning($t('lz.label.sharedataDataSource.packetDeleteLocked'));
			return;
		}
	} catch { return; }
	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.name }), $t('lz.label.base.confirm'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			const wasCurrent = state.currentPacket?.id === packetId;
			const prevCurrentPacket = state.currentPacket;
			const prevLastLoadedCode = lastLoadedSqlPacketCode;
			const prevExpandedGroupIds = [...expandedGroupIds.value];
			const prevSqlFields = [...sqlFields.value];
			const { rollback } = optimisticRemoveGridRows<ShareDataPagePacketOutput>({
				grid: xGridPacket.value as any,
				predicate: (r) => r.id === packetId,
			});
			if (wasCurrent) {
				state.currentPacket = undefined;
				lastLoadedSqlPacketCode = null;
				expandedGroupIds.value = [];
				sqlFields.value = [];
				xGridField.value?.loadData([]);
			}
			try {
				await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketDeletePost({ id: packetId });
				await handleQueryPacket(true);
				ElMessage.success($t('lz.message.base.deleteSuccess'));
			} catch {
				await rollback();
				if (wasCurrent) {
					state.currentPacket = prevCurrentPacket;
					lastLoadedSqlPacketCode = prevLastLoadedCode;
					expandedGroupIds.value = prevExpandedGroupIds;
					sqlFields.value = prevSqlFields;
					if (prevCurrentPacket) {
						xGridPacket.value?.setCurrentRow(prevCurrentPacket);
						void handleQueryField(true);
					}
				}
				await handleQueryPacket(true);
			}
		})
		.catch(() => {});
};

// List field & SQL by Packet (W0.2)
let lastLoadedSqlPacketCode: string | null = null;
const packetSwitching = ref(false);
/** Đang đổi tab Mẫu chung / đối tác: dùng chung mặt nạ trắng với lúc đổi gói. */
const scopeSwitching = ref(false);
let scopeSwitchVersion = 0;
let packetSwitchVersion = 0;

const handleFieldByPacket = async (scope: any) => {
	if (scope?.column?.field === 'actionClm') return;
	const row = scope?.row as ShareDataPagePacketOutput;
	if (!row?.id) return;
	if (state.currentPacket?.id === row.id) return;

	if (state.currentPacket && !packetSwitching.value) {
		const hasUnsaved =
			sqlIsDirty.value ||
			fieldsPending.value ||
			fieldsEdited.value ||
			variablesDirty.value ||
			isPanelDirty(sqlVariablePanelRef.value) ||
			isPanelDirty(writeSqlPanelRef.value);
		if (hasUnsaved) {
			try {
				await ElMessageBox.confirm(
					$t('lz.confirm.sharedataDataSource.leaveUnsavedPacket'),
					$t('lz.label.base.notification'),
					{
						type: 'warning',
						center: true,
						customClass: 'lost-keys-confirm-box',
						confirmButtonText: $t('lz.button.base.confirm'),
						cancelButtonText: $t('lz.button.base.cancel'),
					}
				);
			} catch {
				nextTick(() => {
					if (state.currentPacket) xGridPacket.value?.setCurrentRow(state.currentPacket);
				});
				return;
			}
		}
	}

	// Số thứ tự lần đổi gói: bấm gói khác khi gói trước còn đang tải thì chỉ lần mới nhất được tắt vòng xoay.
	const switchId = ++packetSwitchVersion;
	packetSwitching.value = true;
	sqlCenterState.isSaving = false;
	packetActiveSubs.value = [];
	scopeSwitching.value = false;
	if (packetDetailsRef.value) packetDetailsRef.value.scrollTop = 0;
	nextTick(() => {
		if (packetDetailsRef.value) packetDetailsRef.value.scrollTop = 0;
	});
	// Gọi GetSqlDetail trước khi đổi gói để bảng tham số và khối câu ghi (tự nạp theo gói) lấy chung một lần đọc
	packetSqlPreload.value = isSuperAdmin.value && row?.code ? createPacketSqlPreload(row.code) : null;
	partnerSqlList.value = [];
	sqlHasCursor.value = false;
	state.currentPacket = row;
	state.queryParamsField.code = undefined;
	state.queryParamsField.type = undefined;
	state.queryParamsField.state = undefined;
	state.queryParamsField.datatypeId = row.id;
	expandedGroupIds.value = [];
	commonFieldsList.value = [];
	commonFieldKeys.value = new Set();
	clearDescribeNotice();
	setSqlFields([]);

	lastLoadedSqlPacketCode = row?.code  ? row.code : null;
	activeSqlTab.value = 'standard';
	currentLoadedSqlTab = 'standard';
	sqlView.value = 'common';
	activePartnerDetail.value = null;
	try {
		// API SQL (kèm danh sách trường) chỉ dành cho SuperAdmin; người khác không gọi để khỏi báo lỗi quyền.
		if (isSuperAdmin.value) {
			await Promise.all([loadExtractSqlForPacket(row.code), loadPartnerSqlList(row.code), loadPacketActiveSubs(row)]);
		}
		// Đang lọc theo đối tác: nếu đối tác có cấu hình riêng thì mở thẳng tab của đối tác đó; nếu dùng chung thì giữ tab Mẫu chung.
		const filterPartnerId = state.queryParamsPacket.partnerId;
		if (isSuperAdmin.value && filterPartnerId && switchId === packetSwitchVersion) {
			const partnerItem = partnerSqlList.value.find((p) => p.partnerId === filterPartnerId);
			if (partnerItem?.isCustom) await handleOpenPartnerSql(partnerItem);
		}
		// Bảng tham số, khối mốc đang chạy và khối câu ghi tự nạp theo gói: chờ xong hết rồi mới tắt vòng loading chung
		await waitDetailPanelsIdle();
	} finally {
		if (switchId === packetSwitchVersion) {
			packetSwitching.value = false;
			scopeSwitching.value = false;
			sqlCenterState.isLoading = false;
		}
	}
};

/** Trích xuất cờ loading boolean từ ref component (hỗ trợ cả ref unwrapped lẫn ComputedRef/RefImpl). */
const isPanelLoading = (panelRef: any): boolean => {
	if (!panelRef) return false;
	const val = panelRef.loading;
	if (typeof val === 'function') return Boolean(val());
	if (val && typeof val === 'object' && 'value' in val) return Boolean(val.value);
	return Boolean(val);
};

/** Chờ bảng tham số, khối mốc đang chạy và khối câu ghi nạp xong hết. Có timeout an toàn để không bị treo. */
const waitDetailPanelsIdle = async () => {
	await nextTick();
	await nextTick();
	await new Promise<void>((resolve) => {
		let done = false;
		let stop: (() => void) | undefined;
		const finish = () => {
			if (done) return;
			done = true;
			clearTimeout(timer);
			stop?.();
			resolve();
		};
		const timer = setTimeout(finish, 2000);
		const isBusy = () =>
			sqlCenterState.isLoading ||
			partnerSqlLoading.value ||
			optionsFieldGrid.loading ||
			isPanelLoading(sqlVariablePanelRef.value) ||
			isPanelLoading(writeSqlPanelRef.value) ||
			(isSuperAdmin.value && sqlHasCursor.value && (!cursorBlockRef.value || isPanelLoading(cursorBlockRef.value)));
		stop = watch(
			isBusy,
			async (busy) => {
				if (busy) return;
				await nextTick();
				await nextTick();
				if (!isBusy()) finish();
			},
			{ immediate: true }
		);
		if (done) stop?.();
	});
};

function handlePacketCustom() {
	xGridPacket.value?.openCustom();
}

/* Gói tin: end ======================== */

/* SQL Center logic (W0.2): begin ======= */

const loadExtractSqlForPacket = async (packetCode?: string | null, silent = false) => {
	if (!packetCode) return;
	const isSilent = silent === true;
	try {
		if (!isSilent) {
			sqlCenterState.isLoading = true;
			sqlCenterState.extractSql = '';
			sqlCenterState.savedExtractSql = '';
			sqlCenterState.loadedExtractSql = '';
			sqlCenterState.validationError = '';
			clearDescribeNotice();
		}

		const preloaded = (await takePacketSqlPreload(packetSqlPreload.value, packetCode, 'extract'))?.extract;
		const data: PacketExtractSqlOutput | null | undefined = preloaded ?? (await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketGetextractsqlGet(packetCode)).data?.result;

		if (state.currentPacket?.code !== packetCode) return;

		if (data) {
			if (isSilent) {
				sqlCenterState.validationError = '';
				clearDescribeNotice();
			}
			sqlCenterState.extractSql = data.extractSql || '';
			sqlCenterState.savedExtractSql = data.extractSql || '';
			sqlCenterState.loadedExtractSql = sqlCenterState.savedExtractSql;
			sqlCenterState.status = data.status ?? 1;
			sqlCenterState.remark = data.remark || '';

			commonFieldsList.value = (data.fields || []).filter((f) => !f.lost && f.key);
			commonFieldKeys.value = new Set(commonFieldsList.value.map((f) => f.key!.toLowerCase()));
			setSqlFields(data.fields, data.describedTime as any, false, isSilent);
		}
	} catch {
		// Giữ nguyên trạng thái mặc định nếu gọi lỗi
	} finally {
		if (!isSilent && (state.currentPacket?.code === packetCode || !state.currentPacket)) {
			sqlCenterState.isLoading = false;
		}
	}
};

const formatDatabaseErrorDetail = (detail: string): string => {
	if (!detail) return '';
	const colMatch = detail.match(/Invalid column name '([^']+)'/i);
	if (colMatch) {
		const col = colMatch[1];
		return $te('lz.validation.sharedata.sqlColumnNotExist')
			? $t('lz.validation.sharedata.sqlColumnNotExist', { name: col })
			: `Cột '${col}' không tồn tại trên cơ sở dữ liệu.`;
	}
	const tblMatch = detail.match(/Invalid object name '([^']+)'/i);
	if (tblMatch) {
		const tbl = tblMatch[1];
		return $te('lz.validation.sharedata.sqlObjectNotExist')
			? $t('lz.validation.sharedata.sqlObjectNotExist', { name: tbl })
			: `Bảng hoặc đối tượng '${tbl}' không tồn tại trên cơ sở dữ liệu.`;
	}
	const ambMatch = detail.match(/Ambiguous column name '([^']+)'/i);
	if (ambMatch) {
		const amb = ambMatch[1];
		return $te('lz.validation.sharedata.sqlColumnAmbiguous')
			? $t('lz.validation.sharedata.sqlColumnAmbiguous', { name: amb })
			: `Cột '${amb}' bị trùng lặp (cần chỉ định rõ alias bảng).`;
	}
	return detail;
};

const formatValidationError = (msg?: string | null): string => {
	if (!msg || typeof msg !== 'string') return '';
	const trimmed = msg.trim();
	if (trimmed.startsWith('lz.') && $te(trimmed)) {
		return $t(trimmed).replace(/:\s*\[?\{0\}\]?\s*$/g, '.').replace(/\[?\{0\}\]?\s*/g, '').trim();
	}
	if (trimmed.includes(':')) {
		const colonIdx = trimmed.indexOf(':');
		const head = trimmed.substring(0, colonIdx).trim();
		const tail = trimmed.substring(colonIdx + 1).trim();
		if (tail) {
			return `${head}: ${formatDatabaseErrorDetail(tail)}`;
		}
	}
	return formatDatabaseErrorDetail(trimmed);
};

/** Gói tin và đối tác (khi đang sửa SQL riêng) gửi kèm lúc dò cột, để BE nhận các biến đã khai ở bảng Tham số câu truy vấn. */
const sqlVariableScope = () => ({
	packetCode: state.currentPacket?.code,
	partnerId: sqlView.value === 'edit' ? currentPartnerItem.value?.partnerId : undefined,
});

/** Sau khi Lưu thất bại: hỏi lại BE để lấy dòng, cột của lỗi rồi đánh dấu trên Monaco và cập nhật thông báo lỗi chi tiết kèm tên trường. */
const locateSqlError = async () => {
	try {
		const res = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketParseextractcolumnsPost({
			extractSql: sqlCenterState.extractSql,
			...sqlVariableScope(),
		});
		const result = res.data?.result;
		if (!result?.isValid) {
			if (result?.errorMessage) {
				sqlCenterState.validationError = formatValidationError(result.errorMessage);
			}
			if (result?.errorLine && result?.errorColumn) {
				sqlEditorRef.value?.setErrorMarker(result.errorLine, result.errorColumn, sqlCenterState.validationError || result?.errorMessage || '');
			} else {
				sqlEditorRef.value?.focusEditor();
			}
		}
	} catch {
		// Không xác định được vị trí thì giữ nguyên thông báo lỗi
	}
};

/** Danh sách trường đang hiện có khác bản đã lưu không (để báo "chưa lưu" và mở nút Lưu). */
const describeHasChanges = (result: { added?: string[] | null; lost?: string[] | null; typeChanged?: string[] | null }) =>
	(result.added?.length ?? 0) > 0 || (result.lost?.length ?? 0) > 0 || (result.typeChanged?.length ?? 0) > 0;

/** Hiện kết quả dò: số cột, các khoá thêm / mất / đổi kiểu và hồ sơ ánh xạ đang dùng khoá mất. */
const showDescribeNotice = (columnCount: number, result: { added?: string[] | null; lost?: string[] | null; typeChanged?: string[] | null; affectedMappings?: SqlFieldMappingUsage[] | null }) => {
	if (!describeHasChanges(result)) {
		describeNotice.type = 'success';
		describeNotice.title = $t('lz.message.sharedataDataSource.describeNoChange', { count: columnCount });
		describeNotice.description = '';
		return;
	}
	const parts: string[] = [];
	if (result.added?.length) parts.push($t('lz.label.sharedataDataSource.describeAdded', { count: result.added.length, names: result.added.join(', ') }));
	if (result.lost?.length) parts.push($t('lz.label.sharedataDataSource.describeLost', { count: result.lost.length, names: result.lost.join(', ') }));
	if (result.typeChanged?.length) parts.push($t('lz.label.sharedataDataSource.describeTypeChanged', { names: result.typeChanged.join('; ') }));
	if (result.affectedMappings?.length) parts.push($t('lz.label.sharedataDataSource.describeAffected', { names: mappingNames(result.affectedMappings) }));
	describeNotice.type = result.lost?.length ? 'warning' : 'info';
	describeNotice.title = $t('lz.message.sharedataDataSource.describeChanged', { count: columnCount });
	describeNotice.description = parts.join(' ');
};

const mappingNames = (mappings?: SqlFieldMappingUsage[] | null) =>
	(mappings || []).map((m) => `${m.mappingCode || m.mappingName || m.mappingId} (${(m.keys || []).join(', ')})`).join('; ');

/**
 * Dò cột trả về của câu SQL đang soạn trên CSDL (khoá, kiểu, nguồn) và đối chiếu với danh sách trường đã lưu.
 * Kết quả thay danh sách trường đang hiện nhưng chưa ghi; bấm Lưu thì máy chủ dò lại và ghi cùng câu SQL.
 */
const handleDescribeColumns = async () => {
	const packetCode = state.currentPacket?.code;
	if (!packetCode || !sqlCenterState.extractSql?.trim()) {
		ElMessage.warning($t('lz.validation.base.required', { PropertyName: 'SQL' }));
		return;
	}
	const targetPacketCode = packetCode;
	const targetSwitchVersion = packetSwitchVersion;
	const targetTab = activeSqlTab.value;
	const isTargetCurrent = () =>
		state.currentPacket?.code === targetPacketCode &&
		packetSwitchVersion === targetSwitchVersion &&
		activeSqlTab.value === targetTab;

	try {
		sqlCenterState.isValidating = true;
		sqlCenterState.validationError = '';
		const res = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketDescribecolumnsPost({
			packetCode,
			partnerId: inPartnerEdit.value ? currentPartnerItem.value?.partnerId : undefined,
			extractSql: sqlCenterState.extractSql,
		});
		if (!isTargetCurrent()) return;
		const result = res.data?.result;
		if (!result) return;
		if (!result.isValid) {
			clearDescribeNotice();
			sqlCenterState.validationError = formatValidationError(result.errorMessage || '') || $t('lz.message.sharedataDataSource.sqlInvalid');
			if (result.errorLine && result.errorColumn) {
				sqlEditorRef.value?.setErrorMarker(result.errorLine, result.errorColumn, sqlCenterState.validationError);
			} else {
				sqlEditorRef.value?.focusEditor();
			}
			return;
		}
		sqlEditorRef.value?.clearErrorMarker();
		const fields = result.fields || [];
		// Giữ phần người dùng vừa sửa trên bảng (chưa lưu) cho các khoá còn trong kết quả dò
		const edited = fieldsEdited.value;
		if (edited) {
			const current = new Map(sqlFields.value.filter((f) => f.key).map((f) => [f.key!.toLowerCase(), f]));
			for (const f of fields) {
				const old = f.key ? current.get(f.key.toLowerCase()) : undefined;
				if (!old) continue;
				f.label = old.label;
				f.required = old.required;
				f.skip = old.skip;
				f.group = old.group;
				f.remark = old.remark;
			}
		}
		setSqlFields(fields, fieldsDescribedAt.value, describeHasChanges(result), true);
		fieldsEdited.value = edited;
		showDescribeNotice(fields.filter((f) => !f.lost).length, result);
	} catch (err: any) {
		if (!isTargetCurrent()) return;
		sqlCenterState.validationError = formatValidationError(err?.response?.data?.message || err?.message) || $t('lz.message.sharedataDataSource.validateSqlFailed');
	} finally {
		if (isTargetCurrent()) {
			sqlCenterState.isValidating = false;
		}
	}
};

/**
 * Máy chủ chưa lưu vì thao tác làm mất khoá đang được hồ sơ ánh xạ dùng, hoặc đổi công thức cột mốc khi đối tác đang có mốc nối đuôi
 * (mốc của các đối tác đó sẽ được đặt lại, gửi lại từ mốc khởi tạo): hỏi người dùng, đồng ý thì trả true để gửi lại với force.
 */
const confirmSaveImpact = async (result: {
	lost?: string[] | null;
	affectedMappings?: SqlFieldMappingUsage[] | null;
	cursorResetPartners?: CursorResetPartner[] | null;
}) => {
	try {
		const mappings = result.affectedMappings || [];
		const usedKeys = Array.from(new Set(mappings.flatMap((m) => m.keys || [])));
		const lostList = usedKeys.length ? usedKeys : (result.lost || []);
		const [intro, outro] = mappings.length
			? $t('lz.message.sharedataDataSource.lostKeysConfirm', {
					count: lostList.length,
					keys: lostList.join(', '),
					mappings: mappingNames(mappings),
				}).split('\n')
			: [];
		const resets = result.cursorResetPartners || [];
		const content = h('div', { style: 'display: flex; flex-direction: column; gap: 8px; line-height: 1.5;' }, [
			intro ? h('div', intro) : null,
			...mappings.map((m) => {
				const title = m.mappingName && m.mappingName !== m.mappingCode ? `${m.mappingName} (${m.mappingCode})` : (m.mappingCode || m.mappingName || m.mappingId);
				return h(
					'div',
					{
						style: 'padding: 8px 10px; border-radius: 6px; background: var(--el-fill-color-light); border: 1px solid var(--el-border-color-lighter);',
					},
					[
						h('div', { style: 'font-weight: 600; font-size: 13px; color: var(--el-text-color-primary); margin-bottom: 6px; word-break: break-all;' }, title),
						h(
							'div',
							{ style: 'display: flex; flex-wrap: wrap; gap: 4px;' },
							(m.keys || []).map((k) =>
								h(
									'code',
									{
										style: 'font-family: Consolas, Monaco, monospace; font-size: 12px; padding: 1px 6px; border-radius: 4px; background: var(--el-color-danger-light-9); color: var(--el-color-danger); border: 1px solid var(--el-color-danger-light-7);',
									},
									k
								)
							)
						),
					]
				);
			}),
			outro ? h('div', { style: 'font-size: 12.5px; color: var(--el-text-color-secondary);' }, outro) : null,
			resets.length
				? h(
						'div',
						{
							class: 'cursor-reset-confirm',
							style: 'padding: 8px 10px; border-radius: 6px; background: var(--el-color-warning-light-9); border: 1px solid var(--el-color-warning-light-7);',
						},
						$t('lz.message.sharedataDataSource.cursorResetConfirm', {
							count: resets.length,
							names: resets.map((p) => p.partnerName || p.partnerCode || p.partnerId).join(', '),
						})
					)
				: null,
		]);
		await ElMessageBox.confirm(content, $t('lz.label.base.notification'), {
			type: 'warning',
			center: true,
			customClass: 'lost-keys-confirm-box',
			confirmButtonText: $t('lz.button.base.confirm'),
			cancelButtonText: $t('lz.button.base.cancel'),
			customStyle: { maxWidth: '520px', width: '92vw' },
		});
		return true;
	} catch {
		return false;
	}
};

/**
 * Lưu & phân tích: phân tích lại biến từ câu SQL rồi lấy bộ biến gửi kèm câu SQL — BE kiểm câu theo bộ biến này và lưu cả hai một lần
 * (câu bị từ chối thì bộ biến cũng không lưu). undefined = bảng biến không đổi (BE dùng bộ đang lưu), null = còn dòng lỗi, không lưu.
 */
const collectSqlVariables = (): PacketSqlVariableItemInput[] | null | undefined => {
	const panel = sqlVariablePanelRef.value;
	if (!panel) return undefined;
	panel.analyzeFromSql(sqlCenterState.extractSql);
	if (panel.warnIfCursorVarMissing()) return null;
	const hasStale = Boolean(panel.hasStaleSaved?.value ?? panel.hasStaleSaved);
	return isPanelDirty(panel) || hasStale ? panel.buildSaveItems() : undefined;
};

const handleSaveSql = async (force = false, isFieldEdit = false): Promise<boolean> => {
	if (sqlScopeLocked.value) {
		warnSqlLocked(sqlScopeLockNames.value);
		return false;
	}
	if (!state.currentPacket?.code || !sqlCenterState.extractSql?.trim()) {
		ElMessage.warning($t('lz.validation.base.required', { PropertyName: 'SQL' }));
		return false;
	}
	// Khi sửa trường dữ liệu (isFieldEdit), không phân tích lại biến hay chặn bởi bảng tham số
	const variables = isFieldEdit ? undefined : collectSqlVariables();
	if (variables === null) return false;
	const fieldsWereEmpty = state.fieldCount === 0;
	let retryWithForce = false;

	const targetPacketCode = state.currentPacket.code;
	const targetSwitchVersion = packetSwitchVersion;
	const targetTab = activeSqlTab.value;
	const isTargetCurrent = () =>
		state.currentPacket?.code === targetPacketCode &&
		packetSwitchVersion === targetSwitchVersion &&
		activeSqlTab.value === targetTab;

	try {
		sqlCenterState.isSaving = true;
		const res = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketUpdateextractsqlPost({
			packetCode: targetPacketCode,
			extractSql: sqlCenterState.extractSql,
			status: sqlCenterState.status,
			remark: sqlCenterState.remark,
			fields: buildFieldEdits(),
			variables,
			force,
		});

		if (!isTargetCurrent()) return false;

		// Chưa lưu vì câu SQL mới làm mất khoá đang được hồ sơ ánh xạ dùng, hoặc đổi công thức cột mốc khi đối tác đang có mốc: hỏi xác nhận rồi gửi lại với force
		if (res.data?.result?.requiresConfirm) {
			retryWithForce = await confirmSaveImpact(res.data.result);
			if (!retryWithForce || !isTargetCurrent()) return false;
		} else if (res.data?.result) {
			ElMessage.success(isFieldEdit ? $t('lz.message.sharedataDataSource.saveFieldSuccess') : $t('lz.message.sharedataDataSource.saveSqlSuccess'));
			sqlCenterState.savedExtractSql = sqlCenterState.extractSql.trim();
			sqlCenterState.loadedExtractSql = sqlCenterState.savedExtractSql;
			sqlCenterState.validationError = '';
			lastLoadedSqlPacketCode = targetPacketCode;
			keepFieldsOpenAfterAnalyze(fieldsWereEmpty);
			const data = res.data?.result;
			if (data) {
				if (data.status != null) {
					sqlCenterState.status = data.status;
				}
				if (data.remark != null) {
					sqlCenterState.remark = data.remark;
				}
				// Máy chủ dò lại câu SQL vừa lưu và ghi danh sách trường cùng câu SQL
				commonFieldsList.value = (data.fields || []).filter((f) => !f.lost && f.key);
				commonFieldKeys.value = new Set(commonFieldsList.value.map((f) => f.key!.toLowerCase()));
				setSqlFields(data.fields, data.describedTime as any, false, true);
				clearDescribeNotice();
			}
			await loadPartnerSqlList(targetPacketCode, true);
			if (variables && isTargetCurrent()) await sqlVariablePanelRef.value?.reload?.(true);
			// Đổi công thức cột mốc thì máy chủ bỏ mốc đang chạy của đối tác dùng mẫu chung: nạp lại khối 4
			if (isTargetCurrent()) await cursorBlockRef.value?.reload?.();
			return true;
		} else {
			const errMsg = formatValidationError(res.data?.message) || $t('lz.message.sharedataDataSource.saveSqlFailed');
			sqlCenterState.validationError = errMsg;
			await locateSqlError();
			return false;
		}
	} catch (err: any) {
		if (!isTargetCurrent()) return false;
		const errMsg = formatValidationError(err?.response?.data?.message || err?.message) || $t('lz.message.sharedataDataSource.saveSqlFailed');
		sqlCenterState.validationError = errMsg;
		await locateSqlError();
		return false;
	} finally {
		if (isTargetCurrent()) {
			sqlCenterState.isSaving = false;
		}
	}
	if (retryWithForce && isTargetCurrent()) return await handleSaveSql(true, isFieldEdit);
	return false;
};

const loadPartnerSqlList = async (packetCode?: string | null, silent = false) => {
	const isSilent = silent === true;
	if (!packetCode) {
		partnerSqlList.value = [];
		partnerTabOrder.value = [];
		partnerTabOrderPacketCode = null;
		return;
	}
	if (partnerTabOrderPacketCode !== packetCode) {
		partnerTabOrder.value = [];
		partnerTabOrderPacketCode = packetCode;
	}
	if (!isSilent) partnerSqlLoading.value = true;
	try {
		const res = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketGetpartnersqllistGet(packetCode);
		const list: PartnerSqlItem[] = (res.data?.result || []).map((item: any) => ({
			partnerId: item.partnerId,
			partnerCode: item.partnerCode,
			partnerName: item.partnerName || item.partnerCode || '',
			isCustom: Boolean(item.isCustom),
			updateTime: item.updateTime || null,
		}));
		const customIds = new Set(list.filter((p) => p.isCustom).map((p) => p.partnerId));
		const kept = partnerTabOrder.value.filter((id) => customIds.has(id));
		const added = list
			.filter((p) => p.isCustom && !kept.includes(p.partnerId))
			.sort((a, b) => {
				const ta = a.updateTime ? new Date(a.updateTime).getTime() : 0;
				const tb = b.updateTime ? new Date(b.updateTime).getTime() : 0;
				return ta - tb;
			})
			.map((p) => p.partnerId);
		partnerTabOrder.value = [...kept, ...added];
		partnerSqlList.value = list;
	} catch {
		if (!isSilent) partnerSqlList.value = [];
	} finally {
		if (!isSilent) partnerSqlLoading.value = false;
	}
};

const loadPartnerExtractSql = async (partnerId: string, silent = false) => {
	const isSilent = silent === true;
	const targetPacketCode = state.currentPacket?.code;
	const targetSwitchVersion = packetSwitchVersion;
	const targetScopeSwitchVersion = scopeSwitchVersion;
	const isCurrent = () =>
		state.currentPacket?.code === targetPacketCode &&
		packetSwitchVersion === targetSwitchVersion &&
		scopeSwitchVersion === targetScopeSwitchVersion &&
		activeSqlTab.value === partnerId;

	try {
		if (!isSilent) {
			sqlCenterState.isLoading = true;
			sqlCenterState.validationError = '';
		}
		const res = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketGetpartnerextractsqlGet(partnerId, targetPacketCode || '');
		if (!isCurrent()) return;
		const data = res.data?.result;
		if (data) {
			if (isSilent) sqlCenterState.validationError = '';
			activePartnerDetail.value = data;
			const effectiveSql = data.customSql || data.standardSql || '';
			sqlCenterState.extractSql = effectiveSql;
			sqlCenterState.savedExtractSql = data.customSql ? effectiveSql : '';
			sqlCenterState.loadedExtractSql = effectiveSql;

			// Có SQL riêng: trường của câu riêng; chưa có: đối tác đang dùng trường mẫu chung
			if (data.commonFields?.length) {
				commonFieldsList.value = data.commonFields.filter((f) => !f.lost && f.key);
				commonFieldKeys.value = new Set(commonFieldsList.value.map((f) => f.key!.toLowerCase()));
			}
			clearDescribeNotice();
			if (data.isCustom) setSqlFields(data.fields, data.describedTime as any, false, isSilent);
			else setSqlFields((data.commonFields?.length ? data.commonFields : commonFieldsList.value).filter((f) => !f.lost), undefined, false, isSilent);
		}
	} catch {
	} finally {
		if (!isSilent && isCurrent()) {
			sqlCenterState.isLoading = false;
		}
	}
};

/**
 * Chặn chuyển tab nếu câu SQL đang sửa dở chưa lưu:
 * Chặn ngay từ đầu (không cho el-tabs nhảy sang tab mới), hỏi người dùng qua hộp thoại cảnh báo:
 * - Đồng ý (Xác nhận): cho phép chuyển tab và nạp nội dung của tab mới.
 * - Hủy (Bỏ qua): giữ nguyên ở tab hiện tại, không đổi highlight hay nạp tab mới.
 */
const handleBeforeSqlTabLeave = async (newTab: any, oldTab: any): Promise<boolean> => {
	// handleSqlTabChange đã gán activeSqlTab (chuyển tab bằng mã, vd. mở tab đối tác vừa tạo): cho el-tabs đổi theo, không hỏi lại
	if (String(newTab) === activeSqlTab.value) return true;
	if (scopeSwitching.value || resettingPartnerSql.value) return false;
	const normalizeSql = (sql?: string) => (sql || '').replace(/\r\n?/g, '\n').trim();
	const isDirty =
		normalizeSql(sqlCenterState.extractSql) !== normalizeSql(sqlCenterState.loadedExtractSql) ||
		fieldsEdited.value ||
		variablesDirty.value ||
		isPanelDirty(sqlVariablePanelRef.value) ||
		isPanelDirty(writeSqlPanelRef.value);
	if (isDirty) {
		try {
			await ElMessageBox.confirm(
				$t('lz.confirm.sharedataDataSource.leaveUnsavedSql'),
				$t('lz.label.base.notification'),
				{
					type: 'warning',
					center: true,
					customClass: 'lost-keys-confirm-box',
					confirmButtonText: $t('lz.button.base.confirm'),
					cancelButtonText: $t('lz.button.base.cancel'),
				}
			);
			return true;
		} catch {
			return false;
		}
	}
	return true;
};

const handleSqlTabChange = async (tabName: any, force = false, silent = false) => {
	const isSilent = silent === true;
	if (!isSilent && scopeSwitching.value) return;
	const newTab = String(tabName);
	if (!force && newTab === currentLoadedSqlTab && newTab === activeSqlTab.value) return;
	const switchId = ++scopeSwitchVersion;
	if (!isSilent) scopeSwitching.value = true;
	sqlCenterState.isSaving = false;
	currentLoadedSqlTab = newTab;
	activeSqlTab.value = newTab;
	sqlView.value = newTab === 'standard' ? 'common' : 'edit';
	if (!isSilent && packetDetailsRef.value) packetDetailsRef.value.scrollTop = 0;

	try {
		if (newTab === 'standard') {
			activePartnerDetail.value = null;
			if (state.currentPacket?.code) {
				await Promise.all([
					loadExtractSqlForPacket(state.currentPacket.code, isSilent),
					sqlVariablePanelRef.value?.loadAll?.(isSilent) ?? Promise.resolve(),
				]);
			}
		} else {
			await Promise.all([
				loadPartnerExtractSql(newTab, isSilent),
				sqlVariablePanelRef.value?.loadAll?.(isSilent) ?? Promise.resolve(),
			]);
		}
		if (!isSilent) await waitDetailPanelsIdle();
	} finally {
		if (switchId === scopeSwitchVersion) {
			scopeSwitching.value = false;
			if (!isSilent) sqlCenterState.isLoading = false;
		}
	}
};

const handleSavePartnerSql = async (force = false, isFieldEdit = false): Promise<boolean> => {
	if (sqlScopeLocked.value) {
		warnSqlLocked(sqlScopeLockNames.value);
		return false;
	}
	if (!currentPartnerItem.value || !sqlCenterState.extractSql?.trim()) {
		ElMessage.warning($t('lz.validation.base.required', { PropertyName: 'SQL' }));
		return false;
	}
	// Khi sửa trường dữ liệu (isFieldEdit), không phân tích lại biến hay chặn bởi bảng tham số
	const variables = isFieldEdit ? undefined : collectSqlVariables();
	if (variables === null) return false;
	const fieldsWereEmpty = state.fieldCount === 0;
	let retryWithForce = false;

	const targetPacketCode = state.currentPacket?.code;
	const targetPartnerId = currentPartnerItem.value.partnerId;
	const targetSwitchVersion = packetSwitchVersion;
	const targetTab = activeSqlTab.value;
	const isTargetCurrent = () =>
		state.currentPacket?.code === targetPacketCode &&
		packetSwitchVersion === targetSwitchVersion &&
		activeSqlTab.value === targetTab &&
		currentPartnerItem.value?.partnerId === targetPartnerId;

	try {
		sqlCenterState.isSaving = true;
		const partnerId = targetPartnerId;

		const res = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketUpdatepartnerextractsqlPost({
			partnerId,
			packetCode: targetPacketCode ?? '',
			extractSql: sqlCenterState.extractSql,
			fields: buildFieldEdits(),
			variables,
			force,
		});

		if (!isTargetCurrent()) return false;

		// Chưa lưu vì câu SQL riêng làm mất khoá đang được hồ sơ ánh xạ của đối tác dùng: hỏi xác nhận rồi gửi lại với force
		if (res.data?.result?.requiresConfirm) {
			retryWithForce = await confirmSaveImpact(res.data.result);
			if (!retryWithForce || !isTargetCurrent()) return false;
		} else if (res.data?.result) {
			ElMessage.success(isFieldEdit ? $t('lz.message.sharedataDataSource.saveFieldSuccess') : $t('lz.message.sharedataDataSource.saveSqlSuccess'));
			sqlCenterState.savedExtractSql = sqlCenterState.extractSql.trim();
			sqlCenterState.loadedExtractSql = sqlCenterState.savedExtractSql;
			sqlCenterState.validationError = '';
			keepFieldsOpenAfterAnalyze(fieldsWereEmpty);

			if (targetPacketCode) {
				await loadPartnerSqlList(targetPacketCode, true);
			}
			if (isTargetCurrent()) {
				await loadPartnerExtractSql(partnerId, true);
				if (variables) await sqlVariablePanelRef.value?.reload?.(true);
				await cursorBlockRef.value?.reload?.();
			}
			return true;
		} else {
			const errMsg = formatValidationError(res.data?.message) || $t('lz.message.sharedataDataSource.saveSqlFailed');
			sqlCenterState.validationError = errMsg;
			await locateSqlError();
			return false;
		}
	} catch (err: any) {
		if (!isTargetCurrent()) return false;
		const errMsg = formatValidationError(err?.response?.data?.message || err?.message) || $t('lz.message.sharedataDataSource.saveSqlFailed');
		sqlCenterState.validationError = errMsg;
		await locateSqlError();
		return false;
	} finally {
		if (isTargetCurrent()) {
			sqlCenterState.isSaving = false;
		}
	}
	if (retryWithForce && isTargetCurrent()) return await handleSavePartnerSql(true, isFieldEdit);
	return false;
};

const handleResetPartnerSql = async () => {
	if (!currentPartnerItem.value || resettingPartnerSql.value) return;
	if (sqlScopeLocked.value) return warnSqlLocked(sqlScopeLockNames.value);
	try {
		await ElMessageBox.confirm(
			$t('lz.confirm.sharedataDataSource.resetToStandard'),
			$t('lz.label.base.notification'),
			{
				type: 'warning',
				center: true,
				customClass: 'lost-keys-confirm-box',
				confirmButtonText: $t('lz.button.base.confirm'),
				cancelButtonText: $t('lz.button.base.cancel'),
			}
		);
	} catch {
		return;
	}

	const packetCode = state.currentPacket?.code;
	const partnerId = currentPartnerItem.value.partnerId;
	const targetSwitchVersion = packetSwitchVersion;
	const isCurrent = () => state.currentPacket?.code === packetCode && packetSwitchVersion === targetSwitchVersion;

	try {
		resettingPartnerSql.value = true;
		const res = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketResetpartnerextractsqlPost({ partnerId, packetCode });
		if (!isCurrent()) return;
		// Hồ sơ ánh xạ của đối tác đang dùng khoá chỉ câu SQL riêng có: hỏi xác nhận rồi đặt lại với force
		if (res.data?.result?.requiresConfirm) {
			resettingPartnerSql.value = false;
			if (!(await confirmSaveImpact(res.data.result))) return;
			if (!isCurrent()) return;
			resettingPartnerSql.value = true;
			await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketResetpartnerextractsqlPost({ partnerId, packetCode, force: true });
			if (!isCurrent()) return;
		}
		ElMessage.success($t('lz.message.base.saveSuccess'));

		if (state.currentPacket?.code) {
			await loadPartnerSqlList(state.currentPacket.code, true);
		}
		await handleSqlTabChange('standard', false, true);
	} catch {
	} finally {
		resettingPartnerSql.value = false;
	}
};

/** Đưa nhiều đối tác về dùng mẫu chung một lượt: hỏi xác nhận một lần, xóa SQL riêng lần lượt từng đối tác rồi báo kết quả chung. */
const handleResetManyPartnerSql = async (items: PartnerSqlItem[]) => {
	const packetCode = state.currentPacket?.code;
	if (!packetCode || items.length === 0) return;
	const lockedItems = items.filter((i) => outboundLockedPartnerIds.value.includes(i.partnerId));
	if (lockedItems.length) return warnSqlLocked(lockedItems.map((i) => i.partnerName || i.partnerCode || i.partnerId).join(', '));

	const failed: string[] = [];
	// Đối tác có hồ sơ ánh xạ đang dùng khoá chỉ câu SQL riêng có, hoặc đang có mốc nối đuôi mà mẫu chung khác công thức cột mốc:
	// chưa đặt lại, hỏi xác nhận chung một lần sau khi chạy xong
	const needConfirm: Array<{ item: PartnerSqlItem; mappings: SqlFieldMappingUsage[]; cursorReset: boolean }> = [];
	let running = false;
	const partnerLabel = (item: PartnerSqlItem) => item.partnerName || item.partnerCode || item.partnerId;
	// Tuần tự để không dồn nhiều lệnh xóa cùng lúc lên máy chủ; một đối tác lỗi không làm dừng các đối tác còn lại
	const resetAll = async () => {
		for (const item of items) {
			try {
				const res = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketResetpartnerextractsqlPost({ partnerId: item.partnerId, packetCode });
				if (res.data?.result?.requiresConfirm) {
					needConfirm.push({
						item,
						mappings: res.data.result.affectedMappings || [],
						cursorReset: (res.data.result.cursorResetPartners || []).length > 0,
					});
				}
			} catch {
				failed.push(partnerLabel(item));
			}
		}
	};

	try {
		await ElMessageBox.confirm($t('lz.confirm.sharedataDataSource.resetManyToStandard', { count: items.length }), $t('lz.label.base.notification'), {
			type: 'warning',
			confirmButtonText: $t('lz.button.base.confirm'),
			cancelButtonText: $t('lz.button.base.cancel'),
			// Giữ hộp mở và cho nút Xác nhận xoay cho tới khi xóa xong; đang xóa thì bỏ qua mọi thao tác đóng hộp (Esc, nút X, Bỏ qua)
			beforeClose: async (action, instance, done) => {
				if (running) return;
				if (action !== 'confirm') {
					done();
					return;
				}
				running = true;
				partnerBulkResetting.value = true;
				instance.confirmButtonLoading = true;
				try {
					await resetAll();
				} finally {
					running = false;
					partnerBulkResetting.value = false;
					instance.confirmButtonLoading = false;
					done();
				}
			},
		});
	} catch {
		return;
	}

	if (needConfirm.length > 0) {
		let forced = false;
		const mappingItems = needConfirm.filter((x) => x.mappings.length > 0);
		const cursorItems = needConfirm.filter((x) => x.cursorReset);
		try {
			await ElMessageBox.confirm(
				h('div', { style: 'display: flex; flex-direction: column; gap: 8px; line-height: 1.5;' }, [
					mappingItems.length
						? h(
								'div',
								$t('lz.message.sharedataDataSource.resetManyNeedConfirm', {
									count: mappingItems.length,
									names: mappingItems.map((x) => `${partnerLabel(x.item)}: ${mappingNames(x.mappings)}`).join(' | '),
								})
							)
						: null,
					cursorItems.length
						? h(
								'div',
								{ class: 'cursor-reset-confirm' },
								$t('lz.message.sharedataDataSource.cursorResetConfirm', {
									count: cursorItems.length,
									names: cursorItems.map((x) => partnerLabel(x.item)).join(', '),
								})
							)
						: null,
				]),
				$t('lz.label.base.notification'),
				{ type: 'warning', confirmButtonText: $t('lz.button.base.confirm'), cancelButtonText: $t('lz.button.base.cancel') }
			);
			forced = true;
		} catch {
			/* Không đồng ý: các đối tác này giữ SQL riêng */
		}
		for (const { item } of needConfirm) {
			if (!forced) {
				failed.push(partnerLabel(item));
				continue;
			}
			try {
				await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketResetpartnerextractsqlPost({ partnerId: item.partnerId, packetCode, force: true });
			} catch {
				failed.push(partnerLabel(item));
			}
		}
	}

	await loadPartnerSqlList(packetCode, true);
	if (failed.length === 0) {
		ElMessage.success($t('lz.message.sharedataDataSource.resetManySuccess', { count: items.length }));
	} else {
		ElMessage.warning($t('lz.message.sharedataDataSource.resetManyPartial', { done: items.length - failed.length, total: items.length, names: failed.join(', ') }));
	}
};

/** Điền dữ liệu hộp so sánh: mẫu chung hiện tại (trái) và SQL riêng của đối tác (phải), kèm danh sách trường hai bên. */
const fillDiffDialog = (partnerId: string, detail: any, partnerName: string) => {
	diffDialogData.partnerId = partnerId;
	diffDialogData.packetCode = state.currentPacket?.code || '';
	diffDialogData.partnerName = partnerName;
	diffDialogData.standardSql = detail.standardSql || '';
	diffDialogData.customSql = detail.customSql || sqlCenterState.extractSql || '';
	diffDialogData.commonFields = detail.commonFields || commonFieldsList.value || [];
	diffDialogData.partnerFields = detail.fields || sqlFields.value || [];
};

/** So sánh từ màn sửa SQL riêng: đã có chi tiết đối tác nên mở ngay, không gọi lại API. */
const handleOpenDiffDialog = () => {
	if (!currentPartnerItem.value || !activePartnerDetail.value) return;
	fillDiffDialog(
		currentPartnerItem.value.partnerId,
		activePartnerDetail.value,
		currentPartnerItem.value.partnerName || currentPartnerItem.value.partnerCode || ''
	);
	diffDialogVisible.value = true;
};

/** So sánh từ bảng đối tác: lấy chi tiết SQL riêng rồi mở hộp. */
const handleComparePartnerSql = async (partnerId: string) => {
	try {
		const res = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketGetpartnerextractsqlGet(partnerId, state.currentPacket?.code || '');
		const detail = res.data?.result;
		if (!detail) return;
		const item = partnerSqlList.value.find((p) => p.partnerId === partnerId);
		fillDiffDialog(partnerId, detail, item?.partnerName || item?.partnerCode || '');
		diffDialogVisible.value = true;
	} catch {
		// Lỗi đã được bộ xử lý ngoại lệ chung báo
	}
};

/** Dùng lại mẫu chung hoặc lưu bản đã trộn xong: nạp lại danh sách và màn đang sửa, rồi đóng hộp. */
const handleDiffSuccess = async () => {
	if (state.currentPacket?.code) {
		await loadPartnerSqlList(state.currentPacket.code, true);
	}
	if (sqlView.value === 'edit' && activeSqlTab.value !== 'standard') {
		await loadPartnerExtractSql(activeSqlTab.value, true);
	}
	diffDialogVisible.value = false;
};

const handleSqlViewChange = async (view: string | number | boolean | undefined) => {
	if (view === 'partners') {
		sqlView.value = 'partners';
		if (state.currentPacket?.code) {
			await loadPartnerSqlList(state.currentPacket.code);
		}
		return;
	}
	if (activeSqlTab.value !== 'standard') {
		await handleSqlTabChange('standard');
	}
	if (activeSqlTab.value === 'standard') {
		sqlView.value = 'common';
	}
};

const handleOpenPartnerSql = async (item: PartnerSqlItem, silent = false) => {
	await handleSqlTabChange(item.partnerId, false, silent);
	if (activeSqlTab.value === item.partnerId) {
		sqlView.value = 'edit';
	}
};

const handleBackToPartnerList = async () => {
	sqlView.value = 'partners';
	if (state.currentPacket?.code) {
		await loadPartnerSqlList(state.currentPacket.code);
	}
};

/**
 * Một nút Lưu cho tab Chiều gửi: chỉ gọi API của phần có thay đổi.
 * Câu SQL hoặc danh sách trường đổi thì lưu câu SQL (danh sách trường và bảng biến gửi kèm, lưu cùng câu SQL một lần);
 * chỉ bảng biến đổi thì chỉ lưu bảng biến. Câu ghi chiều nhận có nút Lưu riêng ở tab Chiều nhận.
 */
const handleSaveClick = async () => {
	if (sqlScopeLocked.value) return warnSqlLocked(sqlScopeLockNames.value);
	if (sqlIsDirty.value || creatingPartnerSql.value || fieldsPending.value || fieldsEdited.value || sqlNeedsAnalyze.value) {
		await (activeSqlTab.value === 'standard' ? handleSaveSql() : handleSavePartnerSql());
	} else if (isPanelDirty(sqlVariablePanelRef.value) || variablesDirty.value) {
		const targetPacketCode = state.currentPacket?.code;
		const targetSwitchVersion = packetSwitchVersion;
		const isCurrent = () => state.currentPacket?.code === targetPacketCode && packetSwitchVersion === targetSwitchVersion;
		sqlCenterState.isSaving = true;
		try {
			if (await sqlVariablePanelRef.value.saveIfDirty()) {
				if (isCurrent()) ElMessage.success($t('lz.message.base.saveSuccess'));
				// Mốc bắt đầu có thể vừa đổi: cột Lượt gửi tới lấy từ của khối 4 đọc lại
				if (isCurrent()) await cursorBlockRef.value?.reload?.();
			}
		} finally {
			if (isCurrent()) {
				sqlCenterState.isSaving = false;
			}
		}
	}
};

const handleWriteSqlSaved = (count: number) => {
	if (state.currentPacket) state.currentPacket.writeSqlCount = count;
	const currentRow = xGridPacket.value?.getCurrentRecord();
	if (currentRow) currentRow.writeSqlCount = count;
};

const handleOpenDiffFromCompareAll = async (partnerId: string) => {
	compareAllDialogVisible.value = false;
	await handleComparePartnerSql(partnerId);
};

/** Từ popup So sánh tất cả: đóng popup, mở tab SQL riêng của đối tác. */
const handleOpenTabFromCompareAll = async (partnerId: string) => {
	compareAllDialogVisible.value = false;
	const item = partnerSqlList.value.find((p) => p.partnerId === partnerId);
	if (item) await handleOpenPartnerSql(item);
};

const handleResetManyFromCompareAll = async (items: PartnerSqlItem[]) => {
	await handleResetManyPartnerSql(items);
	if (activeSqlTab.value !== 'standard' && !partnerSqlList.value.find((p) => p.partnerId === activeSqlTab.value)?.isCustom) {
		await handleSqlTabChange('standard', false, true);
	}
	if (customPartnerCount.value === 0) {
		compareAllDialogVisible.value = false;
	}
};

const handleConfirmPartnerSetup = async (payload: { partnerId: string; sourcePartnerId: string }) => {
	const packetCode = state.currentPacket?.code;
	if (!packetCode || !payload.partnerId) return;
	const item = partnerSqlList.value.find((p) => p.partnerId === payload.partnerId);
	if (!item) return;
	if (outboundLockedPartnerIds.value.includes(payload.partnerId)) return warnSqlLocked(item.partnerName || item.partnerCode || payload.partnerId);

	partnerSetupCreating.value = true;
	const addedToTabOrder = !partnerTabOrder.value.includes(payload.partnerId);
	if (addedToTabOrder) {
		partnerTabOrder.value.push(payload.partnerId);
	}

	const targetSwitchVersion = packetSwitchVersion;
	const isCurrent = () => state.currentPacket?.code === packetCode && packetSwitchVersion === targetSwitchVersion;

	sqlCenterState.isSaving = true;
	try {
		let sourceSql = sqlCenterState.extractSql || '';
		let sourceFields: SqlFieldItem[] = commonFieldsList.value;
		let sourceVars: any[] | null = null;

		if (payload.sourcePartnerId && payload.sourcePartnerId !== 'standard') {
			try {
				const [sqlRes, varRes] = await Promise.all([
					getAPI(ShareDataPacketApi).apiSharedataSharedatapacketGetpartnerextractsqlGet(payload.sourcePartnerId, packetCode),
					getAPI(ShareDataPacketApi).apiSharedataSharedatapacketGetsqlvariablesGet(packetCode, payload.sourcePartnerId),
				]);
				const srcData = sqlRes.data?.result;
				if (srcData) {
					sourceSql = srcData.customSql || srcData.standardSql || sourceSql;
					sourceFields = (srcData.fields || srcData.commonFields || []).filter((f) => !f.lost && f.key);
				}
				sourceVars = varRes.data?.result?.items ?? [];
			} catch {
				// Giữ bản sao mẫu chung nếu gọi lỗi
			}
		} else if (activeSqlTab.value !== 'standard') {
			try {
				const stdRes = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketGetextractsqlGet(packetCode);
				const stdData = stdRes.data?.result;
				if (stdData) {
					sourceSql = stdData.extractSql || sourceSql;
					sourceFields = (stdData.fields || []).filter((f) => !f.lost && f.key);
				}
			} catch {
				// Giữ bản sao mẫu chung đang có
			}
		}

		// Bộ biến của đối tác nguồn đi kèm câu SQL trong cùng một lần lưu: máy chủ còn hỏi xác nhận thì chưa ghi gì
		const variables = sourceVars?.length
			? sourceVars.map((v: any) => ({
					code: v.code,
					valueKind: v.valueKind,
					dataType: v.dataType,
					value: v.value,
					defaultValue: v.defaultValue || null,
					remark: v.remark || null,
				}))
			: undefined;

		const fieldEdits: SqlFieldEditInput[] = sourceFields
			.filter((f) => f.key)
			.map((f) => ({
				key: f.key!,
				label: f.label?.trim() || null,
				required: Boolean(f.required),
				skip: Boolean(f.skip),
				group: f.group?.trim() || null,
				remark: f.remark?.trim() || null,
				type: f.typeOverride || null,
			}));

		const save = (force: boolean) =>
			getAPI(ShareDataPacketApi).apiSharedataSharedatapacketUpdatepartnerextractsqlPost({
				partnerId: payload.partnerId,
				packetCode,
				extractSql: sourceSql,
				fields: fieldEdits,
				variables,
				force,
			});
		let res = await save(false);
		if (!isCurrent()) return;

		// Câu chép sang khác công thức cột mốc của câu đối tác đang chạy: mốc đang chạy sẽ bị bỏ, hỏi trước khi tạo
		if (res.data?.result?.requiresConfirm) {
			partnerSetupDialogVisible.value = false;
			const confirmed = await confirmSaveImpact(res.data.result);
			if (!isCurrent()) return;
			if (!confirmed) {
				if (addedToTabOrder) partnerTabOrder.value = partnerTabOrder.value.filter((id) => id !== payload.partnerId);
				return;
			}
			res = await save(true);
			if (!isCurrent()) return;
		}

		if (res.data?.result) {
			ElMessage.success($t('lz.message.sharedataDataSource.saveSqlSuccess'));
			await loadPartnerSqlList(packetCode, true);
			await handleOpenPartnerSql(partnerSqlList.value.find((p) => p.partnerId === payload.partnerId) || item, true);
			partnerSetupDialogVisible.value = false;
		} else {
			await handleOpenPartnerSql(item, true);
			sqlCenterState.validationError = formatValidationError(res.data?.message) || $t('lz.message.sharedataDataSource.saveSqlFailed');
			partnerSetupDialogVisible.value = false;
		}
	} catch (err: any) {
		if (!isCurrent()) return;
		await handleOpenPartnerSql(item, true);
		sqlCenterState.validationError = formatValidationError(err?.response?.data?.message || err?.message) || $t('lz.message.sharedataDataSource.saveSqlFailed');
		partnerSetupDialogVisible.value = false;
	} finally {
		partnerSetupCreating.value = false;
		if (isCurrent()) {
			sqlCenterState.isSaving = false;
		}
	}
};

const handleSaveFieldEdits = async (edits: SqlFieldEditInput[]) => {
	const editMap = new Map(edits.map((e) => [e.key.toLowerCase(), e]));
	for (const f of sqlFields.value) {
		if (!f.key) continue;
		const edit = editMap.get(f.key.toLowerCase());
		if (!edit) continue;
		f.label = edit.label;
		f.required = edit.required;
		f.skip = edit.skip;
		f.group = edit.group;
		f.remark = edit.remark;
		// Chọn kiểu khác kiểu đang hiện thì ghi nhận là kiểu chọn tay (chọn lại đúng kiểu dò được thì BE tự bỏ chọn tay)
		if (edit.type && edit.type !== f.type) {
			f.typeOverride = edit.type;
			f.type = edit.type;
		}
	}
	fieldsEdited.value = true;
	const saved = await (activeSqlTab.value === 'standard' ? handleSaveSql(false, true) : handleSavePartnerSql(false, true));
	if (saved && !sqlCenterState.validationError) {
		fieldEditDialogVisible.value = false;
	}
};

// Đổi câu SQL đang soạn thì kết quả dò trước đó không còn đúng với câu đang soạn
watch(
	() => sqlCenterState.extractSql,
	() => {
		if (describeNotice.title) clearDescribeNotice();
	}
);

const handleSqlChangedFromBuilder = (newSql: string) => {
	sqlCenterState.extractSql = newSql;
	sqlCenterState.validationError = '';
};

onMounted(() => {
	window.addEventListener('resize', handleWindowResize, { passive: true });
	applyDefaultPacketPaneSize();
	void loadPartnerOptions();
	// Monaco nặng vài MB và chỉ nạp khi mở tab Cấu hình SQL lần đầu; nạp sẵn lúc rảnh để lần bấm đầu không phải chờ.
	// import() dùng chung một module đã nạp, nên sqlEditor / sqlDiffDialog gọi lại là có ngay.
	if (isSuperAdmin.value) {
		// Cùng lúc nạp sẵn mã của hộp rà soát (defineAsyncComponent), nếu không lần mở đầu phải chờ tải tệp.
		const preload = () => {
			void import('monaco-editor').catch(() => undefined);
			void import('/@/views/sharedata/dataSource/component/sqlDiffDialog.vue').catch(() => undefined);
		};
		if (typeof window.requestIdleCallback === 'function') window.requestIdleCallback(preload, { timeout: 3000 });
		else setTimeout(preload, 1500);
	}
	nextTick(() => {
		requestAnimationFrame(() => {
			xGridPacket.value?.recalculate(true);
			if (optionsFieldGrid.columns) void xGridField.value?.reloadColumn(optionsFieldGrid.columns);
			xGridField.value?.recalculate(true);
		});
	});
});

onUnmounted(() => {
	window.removeEventListener('resize', handleWindowResize);
	if (windowResizeTimer !== null) {
		clearTimeout(windowResizeTimer);
		windowResizeTimer = null;
	}
});
/* Table Height Resize logic: end ============================ */

/* SQL Center logic: end ================== */

/* Field của gói tin: begin ======================== */

const optionsFieldGrid = useVxeTable<any>(
	{
		id: 'eshPacketFieldGrid_v8',
		name: 'eshPacketFieldGrid',
		columns: [
			// So với mẫu chung đặt đầu bảng (chỉ hiện ở tab đối tác)
			{ field: 'compareState', title: $t('lz.label.sharedataDataSource.fieldCompareColumn'), width: 100, align: 'center', headerAlign: 'center', showOverflow: 'tooltip', resizable: true, visible: false, slots: { default: 'row_compareState' } },
			{ field: 'aliasFieldKey', title: $t('lz.entity.sharedataDataSource.aliasFieldKey'), minWidth: 200, align: 'left', headerAlign: 'center', showOverflow: 'tooltip', treeNode: true, resizable: true, slots: { default: 'row_aliasFieldKey' } },
			{ field: 'name', title: $t('lz.entity.sharedataDataSource.fieldName'), minWidth: 150, align: 'left', headerAlign: 'center', showOverflow: 'tooltip', resizable: true, slots: { default: 'row_fieldName' } },
			{ field: 'type', title: $t('lz.entity.sharedataDataSource.fieldType'), width: 95, align: 'center', headerAlign: 'center', showOverflow: 'tooltip', resizable: true },
			// Tạm ẩn theo wireframe và biên bản 26/10; mở lại: bỏ comment
			// { field: 'dbType', title: $t('lz.label.sharedataDataSource.fieldDbType'), width: 120, align: 'center', headerAlign: 'center', showOverflow: 'tooltip', resizable: true },
			// { field: 'source', title: $t('lz.label.sharedataDataSource.fieldSource'), minWidth: 130, align: 'left', headerAlign: 'center', showOverflow: 'tooltip', resizable: true },
			{ field: 'isRequired', title: $t('lz.entity.sharedataDataSource.isRequired'), width: 130, align: 'center', headerAlign: 'center', resizable: true, slots: { default: 'row_isRequired' } },
			// { field: 'skip', title: $t('lz.label.sharedataDataSource.fieldSkip'), width: 55, align: 'center', headerAlign: 'center', resizable: false, slots: { default: 'row_skip' } },
			// { field: 'groupName', title: $t('lz.label.sharedataDataSource.fieldGroup'), minWidth: 110, align: 'left', headerAlign: 'center', resizable: true, slots: { default: 'row_groupName' } },
			{ field: 'remark', title: $t('lz.label.sharedataDataSource.fieldRemark'), minWidth: 120, align: 'left', headerAlign: 'center', resizable: true, slots: { default: 'row_remark' } },
			// { field: 'status', title: $t('lz.label.sharedataDataSource.fieldState'), width: 110, align: 'center', headerAlign: 'center', showOverflow: 'tooltip', resizable: true, slots: { default: 'row_status' } },
			{ field: 'actionClm', title: $t('lz.label.base.action'), fixed: 'right', width: 90, align: 'center', headerAlign: 'center', showOverflow: true, resizable: false, slots: { default: 'row_buttons' } },
		],
	},
	{
		height: 380,
		autoResize: true,
		// Tiêu đề cột luôn một dòng, dài thì "…" kèm tooltip
		showHeaderOverflow: 'tooltip',
		columnConfig: { resizable: true },
		customConfig: {
			enabled: false,
			storage: {
				visible: false,
				resizable: true,
			},
		},
		proxyConfig: {
			autoLoad: false,
			ajax: {
				query: () => handleQueryFieldApi(),
			},
		},
		pagerConfig: { enabled: false },
		toolbarConfig: { export: false },
		rowConfig: { isHover: true, keyField: 'id' },
		treeConfig: { children: 'children', expandAll: true, trigger: 'row' },
		rowClassName: ({ row }: any) => (row.isGroupRow ? 'field-group-row' : row.lost || row.missingFromPartner ? 'field-missing-row' : ''),
		tooltipConfig: { popupClassName: 'data-source-tooltip' },
	}
);

/** Tất cả nhóm trường đang đóng: lưới cao theo nội dung thay vì 380px. */
const fieldGroupsAllCollapsed = ref(false);
/** Đọc lại trạng thái đóng / mở nhóm thật trên lưới (gọi sau mọi lần nạp dữ liệu hoặc bấm mở/đóng nhóm). */
const syncFieldGroupsCollapsed = () => {
	const read = () => {
		const grid = xGridField.value;
		if (!grid) return;
		const groups = (grid.getTableData()?.fullData ?? []).filter((r: any) => r.isGroupRow);
		fieldGroupsAllCollapsed.value = groups.length > 0 && groups.every((g: any) => !grid.isTreeExpandByRow(g));
	};
	nextTick(read);
	setTimeout(read, 120);
};

const gridEventsField: VxeGridListeners<any> = {
	async pageChange({ pageSize }) {
		state.localPageParamField.pageSize = pageSize;
	},
	async sortChange({ field, order }) {
		state.localPageParamField.defaultSort = { field: field, order: order!, descStr: 'desc' };
	},
	// Bấm cả dòng nhóm (trigger: 'row') cũng mở / đóng nhóm
	cellClick() {
		syncFieldGroupsCollapsed();
	},
	proxyQuery() {
		syncFieldGroupsCollapsed();
	},
	toggleTreeExpand({ row, expanded }) {
		// Mọi nhóm đều đóng thì lưới thấp lại theo dòng nhóm; còn nhóm mở thì giữ cao 380px
		syncFieldGroupsCollapsed();
		if (row?.isGroupRow && row.id) {
			if (expanded) {
				if (!expandedGroupIds.value.includes(row.id)) expandedGroupIds.value.push(row.id);
			} else {
				expandedGroupIds.value = expandedGroupIds.value.filter((id) => id !== row.id);
			}
		}
	},
};

/* Trạng thái một trường trong danh sách: mất khỏi câu SQL / bỏ qua / chỉ SQL riêng của đối tác có / đang dùng */
type FieldState = 'active' | 'skipped' | 'lost' | 'custom';
const fieldStateOptions: Array<{ value: FieldState; label: string }> = [
	{ value: 'active', label: 'lz.label.sharedataDataSource.fieldStateActive' },
	{ value: 'custom', label: 'lz.label.sharedataDataSource.fieldStateCustom' },
	{ value: 'skipped', label: 'lz.label.sharedataDataSource.fieldStateSkipped' },
	{ value: 'lost', label: 'lz.label.sharedataDataSource.fieldStateLost' },
];

/** Dòng của bảng trường, dựng từ một phần tử danh sách trường của câu SQL; ô sửa ghi thẳng vào phần tử đó (field). */
interface FieldRow {
	id: string;
	field: SqlFieldItem;
	name: string;
	aliasFieldKey: string;
	type?: string | null;
	dbType?: string | null;
	source?: string | null;
	isRequired: number;
	groupName?: string | null;
	lost: boolean;
	skip: boolean;
	partnerOnly: boolean;
	state: FieldState;
	missingFromPartner?: boolean;
	compareState?: FieldCompareState;
	compareTooltip?: string;
}

const compareSymbol = (s: FieldCompareState) => (s === 'same' ? '=' : s === 'new' ? '+' : s === 'missing' ? '-' : '≠');
const compareTagType = (s: FieldCompareState) => (s === 'same' ? 'success' : s === 'new' ? 'primary' : s === 'missing' ? 'danger' : 'warning');

const formatCompareTooltip = (cmpRow?: FieldCompareRow) => {
	if (!cmpRow || cmpRow.state === 'same') return '';
	if (cmpRow.state === 'new') return $t('lz.label.sharedataDataSource.fieldStateCustom');
	if (cmpRow.state === 'missing') return $t('lz.label.sharedataDataSource.fieldCompareMissingHint');
	return cmpRow.details.map((d) => `${d.aspect}: ${d.commonValue} → ${d.partnerValue}`).join(' · ');
};

const toFieldRow = (f: SqlFieldItem, cmpMap?: Map<string, FieldCompareRow>, missingFromPartner = false): FieldRow => {
	const key = f.key || '';
	const cmpRow = cmpMap?.get(key.toLowerCase());
	// Trường chỉ câu SQL riêng của đối tác có (mẫu chung không có khoá này)
	const partnerOnly = inPartnerEdit.value && !commonFieldKeys.value.has(key.toLowerCase());
	const fieldState: FieldState = f.lost ? 'lost' : f.skip ? 'skipped' : partnerOnly ? 'custom' : 'active';
	return {
		id: `__field__${key}`,
		field: f,
		name: f.label || key,
		aliasFieldKey: key,
		type: f.type,
		dbType: f.dbType,
		source: f.source,
		isRequired: f.required ? 1 : 0,
		groupName: f.group,
		lost: Boolean(f.lost),
		skip: Boolean(f.skip),
		partnerOnly,
		state: fieldState,
		missingFromPartner,
		compareState: cmpRow?.state,
		compareTooltip: formatCompareTooltip(cmpRow),
	};
};

const UNGROUPED_KEY = '__ungrouped__';
function buildGroupedFieldTree(fields: FieldRow[]): any[] {
	const groupsMap = new Map<string, FieldRow[]>();
	const groupOrder: string[] = [];
	const defaultGroupName = $t('lz.label.sharedataDataSource.ungroupedFields');

	for (const f of fields) {
		const raw = (f.groupName ?? '').trim();
		const key = !raw || (defaultGroupName && raw.toLowerCase() === defaultGroupName.toLowerCase()) ? UNGROUPED_KEY : raw;
		if (!groupsMap.has(key)) {
			groupsMap.set(key, []);
			groupOrder.push(key);
		}
		groupsMap.get(key)!.push(f);
	}

	const orderedKeys = [...groupOrder.filter((k) => k !== UNGROUPED_KEY), ...(groupOrder.includes(UNGROUPED_KEY) ? [UNGROUPED_KEY] : [])];

	return orderedKeys.map((key) => {
		const children = groupsMap.get(key)!;
		const label = key === UNGROUPED_KEY ? defaultGroupName : key;
		return {
			id: `__group__${encodeURIComponent(key)}`,
			isGroupRow: true,
			name: `${label} (${children.length})`,
			children,
		};
	});
}

/** Dựng bảng trường từ danh sách trường của câu SQL đang mở, lọc tại chỗ theo khoá, kiểu, trạng thái. */
const handleQueryFieldApi = async (): Promise<{ data: { result: any[]; message?: string | null } }> => {
	if (!state.queryParamsField.datatypeId) return { data: { result: [], message: null } };

	const code = (state.queryParamsField.code || '').trim().toLowerCase();
	const type = (state.queryParamsField.type || '').trim().toLowerCase();
	const fieldState = state.queryParamsField.state;
	const cmpMap = inPartnerEdit.value ? new Map(partnerFieldCompare.value.rows.map((r) => [r.key.toLowerCase(), r])) : undefined;
	const baseFields = inPartnerEdit.value ? sqlFields.value.filter((f) => !f.lost) : sqlFields.value;
	// Như wireframe: không hiện trường mất cột (dữ liệu cũ còn lưu Lost sẽ bị xoá ở lần Lưu & phân tích kế tiếp)
	const allFieldRows: FieldRow[] = baseFields.filter((f) => !f.lost).map((f) => toFieldRow(f, cmpMap, false));
	if (inPartnerEdit.value && partnerFieldsAnalyzed.value) {
		for (const cmpRow of partnerFieldCompare.value.rows) {
			if (cmpRow.state === 'missing' && cmpRow.common) {
				allFieldRows.push(toFieldRow(cmpRow.common, cmpMap, true));
			}
		}
	}
	const rows = allFieldRows
		.filter((r) => !code || r.aliasFieldKey.toLowerCase().includes(code) || r.name.toLowerCase().includes(code))
		.filter((r) => !type || (r.type || '').toLowerCase().includes(type) || (r.dbType || '').toLowerCase().includes(type))
		.filter((r) => !fieldState || r.state === fieldState);

	state.fieldCount = sqlFields.value.filter((f) => !f.lost).length;
	return { data: { result: buildGroupedFieldTree(rows), message: null } };
};

// Chỉ SuperAdmin lưu được câu SQL nên cũng chỉ SuperAdmin sửa được trường; trường đã mất khỏi câu SQL thì không sửa
const canEditField = (row: any) => isSuperAdmin.value && !row?.isGroupRow && !row?.lost;

/** Đã sửa một ô của trường: đánh dấu cần Lưu; đổi nhóm / bỏ qua thì dựng lại bảng để gom nhóm và trạng thái đúng. */
const handleFieldEdited = (rebuild = false) => {
	fieldsEdited.value = true;
	if (rebuild) void handleQueryField(true);
};

const restoreExpandedGroups = async () => {
	await nextTick();
	if (!expandedGroupIds.value.length) return;
	const tableData = xGridField.value?.getTableData()?.fullData ?? [];
	const rowsToExpand = tableData.filter((r: any) => r.isGroupRow && expandedGroupIds.value.includes(r.id));
	if (rowsToExpand.length > 0) {
		await xGridField.value?.setTreeExpand(rowsToExpand, true);
		syncFieldGroupsCollapsed();
	}
};

const handleQueryField = async (silent: boolean | unknown = false) => {
	const isSilent = silent === true;
	if (!state.currentPacket) {
		ElMessage.warning($t('lz.message.sharedataDataSource.selectPacketFirst'));
		return;
	}
	const currentExpanded = xGridField.value?.getTreeExpandRecords() ?? [];
	if (currentExpanded.length > 0) {
		for (const r of currentExpanded) {
			if (r.id && !expandedGroupIds.value.includes(r.id)) {
				expandedGroupIds.value.push(r.id);
			}
		}
	}
	if (!isSilent) optionsFieldGrid.loading = true;
	try {
		if (inPartnerEdit.value) await xGridField.value?.showColumn('compareState');
		else await xGridField.value?.hideColumn('compareState');
		const res = await handleQueryFieldApi();
		const treeData = res.data?.result ?? [];
		await applyFieldTree(treeData);
		xGridField.value?.recalculate(true);
	} finally {
		if (!isSilent) optionsFieldGrid.loading = false;
	}
};

const resetQueryField = async () => {
	state.queryParamsField.code = undefined;
	state.queryParamsField.type = undefined;
	state.queryParamsField.state = undefined;
	await handleQueryField();
};

// Tự tìm khi gõ Mã / Tên, chờ 500ms sau lần gõ cuối; Enter tìm ngay
const debouncedQueryPacket = debounce(() => {
	handleQueryPacket();
}, 500);

const debouncedQueryField = debounce(() => {
	if (state.currentPacket) {
		handleQueryField();
	}
}, 300);

watch(
	() => [state.queryParamsPacket.code, state.queryParamsPacket.name],
	() => {
		debouncedQueryPacket();
	}
);

watch(
	() => [state.queryParamsField.code, state.queryParamsField.type],
	() => {
		debouncedQueryField();
	}
);

function handleFieldCustom() {
	xGridField.value?.openCustom();
}

/* Field của gói tin: end ======================== */

const handleDismissValidationError = () => {
	sqlCenterState.validationError = '';
};

const handleAlertContainerClick = (e: MouseEvent) => {
	const target = e.target as HTMLElement | null;
	if (target?.closest('.el-alert__close-btn')) {
		handleDismissValidationError();
	}
};

// Đối tác để lọc bảng gói tin bên trái (chỉ SuperAdmin; API SQL theo đối tác cũng chỉ SuperAdmin).
const partnerOptions = ref<Array<{ id?: string | null; code?: string | null; name?: string | null }>>([]);
const loadPartnerOptions = async () => {
	if (!isSuperAdmin.value) return;
	try {
		const res = await getAPI(ShareDataPartnerApi).apiSharedataSharedatapartnerListGet(undefined, undefined, undefined, undefined, undefined, true as any);
		partnerOptions.value = (res.data?.result ?? []).filter((p: any) => p?.id);
	} catch {
		partnerOptions.value = [];
	}
};

/* Đang mở SQL riêng của một đối tác: trường chỉ câu riêng có được đánh dấu "Riêng đối tác" và hiện cột So với mẫu */
const inPartnerEdit = computed(() => sqlView.value === 'edit' && Boolean(currentPartnerItem.value));

// Mở rộng riêng phần danh sách trường lên khung lớn (Esc hoặc bấm nền mờ để thu nhỏ)
const fieldsMaximized = ref(false);
const onFieldsMaximizeKeydown = (e: KeyboardEvent) => {
	if (e.key === 'Escape') toggleFieldsMaximized();
};
const toggleFieldsMaximized = () => {
	fieldsMaximized.value = !fieldsMaximized.value;
	if (fieldsMaximized.value) window.addEventListener('keydown', onFieldsMaximizeKeydown);
	else window.removeEventListener('keydown', onFieldsMaximizeKeydown);
	nextTick(() => xGridField.value?.recalculate(true));
};
onUnmounted(() => window.removeEventListener('keydown', onFieldsMaximizeKeydown));

// Thu gọn danh sách trường giống khung soạn SQL: chỉ còn thanh tiêu đề, nhớ trạng thái giữa các lần mở
const FIELDS_COLLAPSED_KEY = 'sharedataDataSource:fieldsCollapsed';
const fieldsCollapsed = ref(Local.get(FIELDS_COLLAPSED_KEY) === true);
const fieldsBodyCollapsed = computed(() => fieldsCollapsed.value);
// Bảng đang mở vì chưa có trường mà Lưu & phân tích sinh ra trường: giữ mở như người dùng đang thấy, không đóng lại theo trạng thái thu gọn cũ
const keepFieldsOpenAfterAnalyze = (wasEmpty: boolean) => {
	if (!wasEmpty || !fieldsCollapsed.value) return;
	fieldsCollapsed.value = false;
	Local.set(FIELDS_COLLAPSED_KEY, false);
};
const toggleFieldsCollapsed = () => {
	if (fieldsMaximized.value) return;
	fieldsCollapsed.value = !fieldsCollapsed.value;
	Local.set(FIELDS_COLLAPSED_KEY, fieldsCollapsed.value);
	if (!fieldsCollapsed.value) nextTick(() => xGridField.value?.recalculate(true));
};

/** Đổi đối tác lọc: bỏ gói đang chọn để sau khi nạp lại, gói đầu tiên tự được chọn và mở theo đối tác mới. */
const handlePartnerFilterChange = async () => {
	debouncedQueryPacket.cancel();
	state.currentPacket = undefined;
	await xGridPacket.value?.commitProxy('reload');
};

/* Dựng lại bảng trường: luôn tự động mở toàn bộ nhóm, không đóng nhóm */
const applyFieldTree = async (treeData: any[]) => {
	if (!xGridField.value) return;
	await xGridField.value.loadData(treeData);
	await xGridField.value.setAllTreeExpand(true);
	syncFieldGroupsCollapsed();
};

const resettingPartnerSql = ref(false);

// KeepAlive also activates on initial mount; the packet grid proxy already loads then.
// Later activations reload the packet list (usage locks may change in other tabs) and refit both grids.
let activated = false;
onActivated(async () => {
	if (!activated) {
		activated = true;
		return;
	}
	handleWindowResize();
	state.currentPacket = undefined;
	lastLoadedSqlPacketCode = null;
	expandedGroupIds.value = [];
	sqlFields.value = [];
	xGridField.value?.loadData([]);
	await xGridPacket.value?.commitProxy('query');
});

/* Gói nhiều đối tác: dòng "Đối tác dùng gói" mặc định chỉ hiện 10 thẻ đầu (cấu hình riêng xếp trước), bấm Xem tất cả mới hiện đủ. */
const PARTNER_USAGE_VISIBLE_LIMIT = 10;
const partnerUsageExpanded = ref(false);
const visiblePartnerUsage = computed(() => {
	const ordered = [...partnerSqlList.value.filter((p) => p.isCustom), ...partnerSqlList.value.filter((p) => !p.isCustom)];
	return partnerUsageExpanded.value ? ordered : ordered.slice(0, PARTNER_USAGE_VISIBLE_LIMIT);
});
// Đổi gói thì dòng đối tác về lại dạng thu gọn
watch(
	() => state.currentPacket?.code,
	() => (partnerUsageExpanded.value = false)
);
const scopeSelectKey = ref(0);
const partnerTabJumpLabel = (p: PartnerSqlItem) => (p.partnerCode && p.partnerName !== p.partnerCode ? `${p.partnerCode} — ${p.partnerName}` : p.partnerCode || p.partnerName);
/** Tên đầy đủ của phạm vi đang chọn cho tooltip của ô chọn (ô hẹp nên tên dài bị cắt). */
const activeScopeLabel = computed(() => {
	if (activeSqlTab.value === 'standard') return $t('lz.label.sharedataDataSource.tabStandardSql');
	const partner = displayedPartnerTabs.value.find((p) => p.partnerId === activeSqlTab.value);
	return partner ? partnerTabJumpLabel(partner) || '' : '';
});
/** Ô chọn phạm vi SQL (Mẫu chung / đối tác có cấu hình riêng): qua bước chặn rời phạm vi khi đang sửa dở rồi mới nạp dữ liệu mới. */
const handleJumpToPartnerTab = async (targetTab: string) => {
	if (!targetTab || targetTab === activeSqlTab.value) return;
	const canLeave = await handleBeforeSqlTabLeave(targetTab, activeSqlTab.value);
	if (!canLeave) {
		scopeSelectKey.value++;
		return;
	}
	await handleSqlTabChange(targetTab);
};

/* Hộp cảnh báo khoá câu SQL khi nhiều đối tác đang bật đăng ký gửi: mặc định hiện 5 đối tác đầu, bấm Xem tất cả mới hiện đủ */
const SQL_LOCK_PARTNERS_VISIBLE_LIMIT = 5;
const sqlLockExpanded = ref(false);
const sqlScopeLockPartnerList = computed(() => {
	const partnerId = currentPartnerItem.value?.partnerId;
	const subs = subsOfDirection(SharedataDirectionEnum.Outbound).filter((s) => !partnerId || s.partnerId === partnerId);
	return [...new Set(subs.map(subPartnerName).filter(Boolean))];
});
const visibleSqlScopeLockNames = computed(() => {
	const list = sqlScopeLockPartnerList.value;
	if (sqlLockExpanded.value || list.length <= SQL_LOCK_PARTNERS_VISIBLE_LIMIT) return list.join(', ');
	return `${list.slice(0, SQL_LOCK_PARTNERS_VISIBLE_LIMIT).join(', ')} …`;
});
watch([() => state.currentPacket?.code, activeSqlTab], () => {
	sqlLockExpanded.value = false;
});
</script>

<style lang="scss" scoped>
.esh-datasource-container {
	display: flex;
	flex-direction: column;
	height: 100%;
	width: 100%;
	min-height: 0;
	overflow: hidden;

	:deep(.splitpanes.default-theme) {
		height: 100%;
		width: 100%;

		&.splitpanes--dragging {
			.splitpanes__pane {
				user-select: none;
				pointer-events: none;
				contain: layout paint;
			}
			.xGrid-style {
				pointer-events: none;
				contain: layout paint;
			}
		}

		.splitpanes__pane {
			height: 100%;
			overflow: hidden;
			display: flex;
			flex-direction: column;
			background-color: transparent;
		}

		.splitpanes__splitter {
			background-color: var(--el-border-color-lighter);
			width: 5px;
			border: none;
			border-radius: 6px;
			margin: 2px 0;
			height: calc(100% - 4px);
			box-sizing: border-box;
			position: relative;
			transition: background-color 0.2s ease;
			z-index: 2;

			&:hover,
			&:active {
				background-color: var(--el-color-primary-light-3);
			}

			&::before,
			&::after {
				display: none;
			}
		}

		&.splitpanes--dragging {
			.splitpanes__splitter {
				background-color: var(--el-color-primary-light-3);
			}
		}
	}

	.packet-pane-inner,
	.field-pane-inner {
		display: flex;
		flex-direction: column;
		height: 100%;
		width: 100%;
		min-height: 0;
		overflow: hidden;

		.search-card-packet {
			flex-shrink: 0;
		}

		.full-table {
			flex: 1 1 0%;
			min-height: 0;
			overflow: hidden;

			:deep(.el-card__body) {
				flex: 1 1 0%;
				height: 100%;
				min-height: 0;
				padding: 5px;
				display: flex;
				flex-direction: column;
				overflow: hidden;
			}

			&.right-pane-card :deep(.el-card__body) {
				padding: 8px 0 8px 8px;
			}
		}
	}

	.packet-pane {
		min-width: 240px;

		:deep(.vxe-grid--toolbar-wrapper) {
			padding-right: 8px;
		}
	}

	.field-pane {
		min-width: 280px;
	}

	.right-pane-card {
		height: 100%;
		display: flex;
		flex-direction: column;

		:deep(.el-card__body) {
			flex: 1;
			min-height: 0;
			display: flex;
			flex-direction: column;
			padding: 8px 0 8px 8px;
		}
	}

	.empty-placeholder {
		display: flex;
		align-items: center;
		justify-content: center;
		height: 100%;
		width: 100%;
	}

	.packet-details-wrapper {
		display: flex;
		flex-direction: column;
		height: 100%;
		width: 100%;
		min-height: 0;
		/* Các khối (SQL, trường, tham số, ghi ngược) cao hơn khung thì cuộn cả khung, không che khối dưới */
		overflow-y: auto;
		overflow-x: hidden;

		&.is-switching,
		&.is-scope-switching {
			overflow-y: hidden;
		}

		&.is-switching > *:not(.el-loading-mask) {
			visibility: hidden;
		}
	}

	.packet-header-bar {
		display: flex;
		/* Hết chỗ thì dòng đối tác dùng xuống hàng dưới, không đè hay cắt tên gói */
		flex-wrap: wrap;
		row-gap: 4px;
		align-items: center;
		justify-content: space-between;
		padding-bottom: 6px;
		padding-right: 8px;
		border-bottom: 1px solid var(--el-border-color-lighter);
		margin-bottom: 6px;
		flex-shrink: 0;

		.packet-header-title {
			display: flex;
			align-items: center;
			flex-shrink: 0;
			max-width: 100%;
			min-width: 0;
			overflow: hidden;
			text-overflow: ellipsis;
			white-space: nowrap;

			.packet-title {
				font-size: 14px;
				font-weight: 600;
				color: var(--el-text-color-primary);
			}
		}
	}

	.tab-pane-container {
		flex: 1;
		min-height: 0;
		display: flex;
		flex-direction: column;
	}

	/* Chiều gửi: thanh đối tác & vùng 1, 2, 3 */
	.send-direction-container {
		display: flex;
		flex-direction: column;
		flex: 1 1 auto;
		min-height: 0;
		width: 100%;

		.sql-header-row {
			display: flex;
			align-items: center;
			justify-content: space-between;
			flex-wrap: wrap;
			gap: 6px 12px;
			flex-shrink: 0;
			margin-bottom: 6px;
			margin-right: 8px;
			padding-bottom: 6px;
			border-bottom: 1px solid var(--el-border-color-lighter);
			background: var(--el-bg-color);
		}

		/* Vùng trái: ô chọn phạm vi SQL (Mẫu chung / đối tác) và nút Tạo cấu hình riêng */
		.sql-partner-tabs-bar {
			flex: 0 0 auto;
			min-width: 0;
			display: flex;
			align-items: center;
			gap: 8px;

			.partner-tab-jump-label {
				font-size: 13px;
				color: var(--el-text-color-regular);
				white-space: nowrap;
			}

			.partner-tab-jump {
				width: 180px;
				flex: 0 0 180px;
			}

			.add-partner-tab-btn {
				height: 24px;
				line-height: 22px;
				padding: 0 10px;
				margin-bottom: 0;
				font-size: 12px;
				font-weight: 500;
				flex-shrink: 0;
				border-radius: 4px;
				background-color: var(--el-color-primary-light-9);
				border: 1px solid var(--el-color-primary-light-5);
				color: var(--el-color-primary);

				&:hover:not(.is-disabled) {
					background-color: var(--el-color-primary-light-8);
					border-color: var(--el-color-primary-light-3);
				}

				&.is-disabled {
					background-color: var(--el-fill-color-light);
					border-color: var(--el-border-color-lighter);
					color: var(--el-text-color-placeholder);
				}
			}
		}

		.sql-crumb {
			display: flex;
			align-items: center;
			gap: 4px;
			min-width: 0;

			.sql-crumb-title {
				font-weight: 600;
				overflow: hidden;
				text-overflow: ellipsis;
				margin-right: 4px;
			}
		}

		.sql-toolbar-bar {
			display: flex;
			align-items: center;
			justify-content: space-between;
			flex: 1 1 auto;
			min-width: 0;
			gap: 8px 12px;
			flex-wrap: wrap;

			.sql-toolbar-left {
				display: flex;
				align-items: center;
				gap: 8px;
				font-size: 13px;
				min-width: 0;
				flex: 1 1 auto;

				.partner-toolbar-code {
					font-size: 16px;
					font-weight: 700;
					color: var(--el-text-color-primary);
					white-space: nowrap;
					flex-shrink: 0;
				}

				.partner-toolbar-name {
					color: var(--el-text-color-regular);
					/* Mã đối tác đậm 16px; tên thường, nhỏ hơn mã */
					font-size: 13px;
					font-weight: 400;
					white-space: nowrap;
					overflow: hidden;
					text-overflow: ellipsis;
					max-width: 240px;
					flex-shrink: 1;
				}

				.chip-status {
					display: inline-flex;
					align-items: center;
					border-radius: 10px;
					padding: 1px 8px;
					font-size: 11.5px;
					line-height: 18px;
					white-space: nowrap;
					flex-shrink: 0;

					&.chip-warning {
						background: #fdf5e6;
						border: 1px solid #e7d3a8;
						color: #b88230;
					}

					&.chip-success {
						background: #eef8f1;
						border: 1px solid #bfdcc8;
						color: #2e7d32;
					}
				}

				.toolbar-meta-note {
					color: var(--el-text-color-secondary);
					font-size: 12.5px;
				}
			}

			.sql-toolbar-controls {
				display: flex;
				align-items: center;
				gap: 6px;
				flex-wrap: nowrap;

				.toolbar-label {
					font-size: 12px;
					font-weight: 500;
					color: var(--el-text-color-regular);
					white-space: nowrap;
				}
			}

			.sql-toolbar-right {
				display: flex;
				align-items: center;
				justify-content: flex-end;
				gap: 8px;
				flex-shrink: 0;
				margin-left: auto;
				white-space: nowrap;

				:deep(.el-button + .el-button) {
					margin-left: 0;
				}

				:deep(.el-button.is-text) {
					height: 28px;
					padding: 0 10px;
					border-radius: 4px;
					font-weight: 500;

					&.el-button--primary {
						background-color: var(--el-color-primary-light-9);
						border: 1px solid var(--el-color-primary-light-5);
						color: var(--el-color-primary);

						&:hover:not(.is-disabled) {
							background-color: var(--el-color-primary-light-8);
							border-color: var(--el-color-primary-light-3);
						}
					}

					&.el-button--warning {
						background-color: var(--el-color-warning-light-9);
						border: 1px solid var(--el-color-warning-light-5);
						color: var(--el-color-warning-dark-2);

						&:hover:not(.is-disabled) {
							background-color: var(--el-color-warning-light-8);
							border-color: var(--el-color-warning-light-3);
						}
					}

					&.is-disabled {
						background-color: var(--el-fill-color-light);
						border-color: var(--el-border-color-lighter);
						color: var(--el-text-color-placeholder);
					}
				}
			}
		}
	}

	.send-regions-wrapper {
		display: flex;
		flex-direction: column;
		flex: 1 1 auto;
		min-height: 200px;
		width: 100%;
		position: relative;

		&.is-scope-loading > *:not(.el-loading-mask) {
			visibility: hidden;
		}
	}

	/* SQL Center Styles (W0.2) */
	.sql-center-pane {
		display: flex;
		flex-direction: column;
		min-height: 0;

		.sql-center-inner {
			display: flex;
			flex-direction: column;
			min-height: 0;
			width: 100%;
		}

		.sql-editor-section {
			min-height: 0;
			display: flex;
			flex-direction: column;
			margin-bottom: 6px;

			.sql-textarea-input {
				:deep(textarea) {
					font-family: Consolas, Monaco, "Cascadia Mono", monospace;
					font-size: 12.5px;
					line-height: 1.45;
					background-color: var(--el-fill-color-blank);
					color: var(--el-text-color-primary);
					border-color: var(--el-border-color);
				}
			}
		}

		.sql-column-diff-alerts {
			flex-shrink: 0;

			.sql-unshared-columns-title {
				display: flex;
				align-items: flex-start;
				justify-content: space-between;
				gap: 12px;
				font-size: 13px;
				width: 100%;

				.sql-unshared-columns-text {
					flex: 1;
					min-width: 0;
					line-height: 1.5;
					word-break: break-word;
				}

				.btn-declare-fields {
					flex-shrink: 0;
					font-weight: 600;
					padding: 0 4px;
					white-space: nowrap;
					margin-top: 1px;
				}
			}

			.columns-stale-note {
				font-size: 12px;
				font-style: italic;
			}
		}
	}

	.saved-fields-pane {
		display: flex;
		flex-direction: column;
		height: 100%;
		min-height: 0;
		overflow: hidden;
	}

	.search-flat-bar {
		border-radius: var(--el-border-radius-base);
		background-color: var(--el-fill-color-light);
		border: 1px solid var(--el-border-color-lighter);
		padding: 5px 8px 3px 8px;
		margin-bottom: 5px;
		width: 100%;
		max-width: 100%;
		box-sizing: border-box;
		display: flex;
		flex-direction: column;
	}

	:deep(.search-card),
	.search-flat-bar {
		container-type: inline-size;
		position: relative;

		.el-card__body {
			padding: 5px 8px 3px 8px;
			display: flex;
			flex-direction: column;
			width: 100%;
		}

		.search-form {
			width: 100%;
			margin-bottom: 2px;

			.el-row {
				width: 100%;
			}

			.el-form-item {
				display: flex;
				align-items: center;
				margin-bottom: 5px;
				width: 100%;
			}

			.el-form-item__label-wrap {
				margin-right: 6px;
				margin-left: 0;
				flex-shrink: 0;
				display: flex;
				align-items: center;
			}

			.el-form-item__label {
				padding: 0 4px 0 0;
				white-space: nowrap;
				width: auto;
				height: 32px;
				line-height: 32px;
				font-size: 13px;
				display: inline-flex;
				align-items: center;
				justify-content: flex-start;
				text-align: left;
			}

			.el-form-item__content {
				flex: 1 1 0%;
				min-width: 0;
				margin-left: 0;
				display: flex;
				align-items: center;
				line-height: 32px;
			}

			.el-input,
			.el-select {
				width: 100%;
				flex: 1;
				min-width: 0;

				.el-input__wrapper {
					width: 100%;
					padding: 1px 7px;
				}
			}

			.el-input:not(.el-select) {
				.el-input__clear[style*='visibility: hidden'] {
					display: none;
					width: 0;
					margin: 0;
				}

				.el-input__count {
					font-size: 11px;
					line-height: normal;
					user-select: none;
					color: var(--el-text-color-secondary);

					.el-input__count-inner {
						padding: 0 4px;
						background: transparent;
					}
				}

				.el-input__inner {
					font-size: 13px;
					min-width: 0;
				}
			}
		}

		.search-divider {
			display: none;
		}

		.search-btn-wrapper {
			width: 100%;
			display: flex;
			align-items: center;
			justify-content: flex-end;
			margin-bottom: 2px;
			white-space: nowrap;

			.el-col {
				width: auto;
				flex: none;
			}

			.el-row {
				flex-wrap: nowrap;
				justify-content: flex-end;
			}

			.el-button {
				padding: 7px 12px;
				font-size: 13px;
			}
		}

		&.search-card-packet {
			@container (max-width: 479px) {
				.search-form .el-row .el-col {
					max-width: 100%;
					flex: 0 0 100%;
					margin-bottom: 5px;
				}
			}

			@container (min-width: 480px) {
				.el-card__body {
					flex-direction: row;
					align-items: center;
					flex-wrap: nowrap;
				}

				.search-form {
					flex: 1 1 0%;
					min-width: 0;
					margin-bottom: 0;

					.el-row {
						align-items: center;
					}

					.el-col {
						margin-bottom: 0;
					}

					.el-form-item {
						margin-bottom: 0;
					}
				}

				.search-divider {
					display: block;
					height: 28px;
					margin: 0 8px;
					flex-shrink: 0;
					align-self: center;
				}

				.search-btn-wrapper {
					width: auto;
					flex-shrink: 0;
					margin-bottom: 0;
					display: flex;
					align-items: center;
				}
			}
		}

		&.search-card-field {
			box-sizing: border-box;
			width: 100%;
			max-width: 100%;

			.search-form {
				width: 100%;
				max-width: 100%;
				margin-bottom: 0;
				box-sizing: border-box;

				.el-row {
					display: flex;
					align-items: center;
					width: 100%;
					max-width: 100%;
					box-sizing: border-box;
				}

				.el-form-item {
					margin-bottom: 5px;
					width: 100%;
					max-width: 100%;
					box-sizing: border-box;
				}

				.el-form-item__label-wrap {
					margin-right: 6px;
					margin-left: 0;
					flex: 0 0 auto;
					width: auto;
				}

				.el-form-item__label {
					padding: 0 4px 0 0;
					white-space: nowrap;
					width: auto;
				}

				.el-form-item__content {
					flex: 1 1 0%;
					min-width: 0;
					max-width: 100%;
					box-sizing: border-box;
				}
			}

			.field-col-alias,
			.field-col-type,
			.field-col-status,
			.field-col-btn {
				box-sizing: border-box;
				min-width: 0;
			}

			.field-col-btn {
				display: flex;
				align-items: center;
				min-height: 32px;
				height: auto;
				box-sizing: border-box;
			}

			@media (min-width: 1401px) {
				.search-form .el-row {
					flex-wrap: nowrap;
				}

				.field-col-alias {
					flex: 1 1 28%;
					max-width: 28%;
					min-width: 0;
				}

				.field-col-type {
					flex: 1 1 24%;
					max-width: 24%;
					min-width: 0;
				}

				.field-col-status {
					flex: 1 1 22%;
					max-width: 22%;
					min-width: 0;
				}

				.field-col-btn {
					flex: 0 0 auto;
					max-width: none;
					margin-left: auto;

					.search-divider {
						display: block;
						height: 28px;
						margin: 0 8px;
						flex-shrink: 0;
					}

					.search-btn-wrapper {
						flex-shrink: 0;
						width: auto;
						margin-bottom: 0;
					}
				}
			}

			@media (max-width: 1400px) and (min-width: 1100px) {
				.search-form .el-row {
					flex-wrap: wrap;
				}

				.field-col-alias,
				.field-col-type {
					flex: 0 0 50%;
					max-width: 50%;
				}

				.field-col-status,
				.field-col-btn {
					flex: 0 0 50%;
					max-width: 50%;
				}

				.field-col-btn {
					justify-content: flex-end;

					.search-divider {
						display: none;
					}

					.search-btn-wrapper {
						width: 100%;
						justify-content: flex-end;
						margin-bottom: 0;
					}
				}
			}

			@media (max-width: 1099px) {
				.search-form .el-row {
					flex-wrap: wrap;
				}

				.field-col-alias,
				.field-col-type,
				.field-col-status,
				.field-col-btn {
					flex: 0 0 100%;
					max-width: 100%;
					width: 100%;
				}

				.field-col-btn {
					justify-content: flex-start;

					.search-divider {
						display: none;
					}

					.search-btn-wrapper {
						width: 100%;
						justify-content: flex-start;
						margin-bottom: 0;
						flex-wrap: wrap;
						gap: 6px;

						:deep(.el-row) {
							justify-content: flex-start;
							flex-wrap: wrap;
							gap: 6px;
						}
					}
				}
			}
		}
	}
}

.full-height {
	display: flex;
	flex-direction: column;
	height: 100%;
}

.packet-title {
	font-weight: 600;
}

.table-name {
	font-family: Consolas, Monaco, "Cascadia Mono", monospace;
	font-size: 12px;
}

/* Khoá không còn trong câu SQL: giữ lại trong danh sách nhưng gạch ngang */
.field-key-lost {
	color: var(--el-text-color-secondary);
	text-decoration: line-through;
}

/* Tab đối tác: trường mẫu chung có mà SQL riêng không trả về (ký hiệu −), chữ nhạt nghiêng như wireframe, không gạch ngang */
.field-key-missing {
	color: var(--el-text-color-secondary);
	font-style: italic;
}

.mono-code-tag {
	font-family: Consolas, Monaco, "Cascadia Mono", monospace;
	font-size: 12px;
	padding: 2px 6px;
	background-color: var(--el-fill-color-light);
	border-radius: 4px;
	border: 1px solid var(--el-border-color-lighter);
}

.text-primary {
	color: var(--el-color-primary);
}

.note {
	color: var(--el-text-color-secondary);
	font-size: 12px;
}

.ml5 {
	margin-left: 5px;
}

.ml8 {
	margin-left: 8px;
}

.mr3 {
	margin-right: 3px;
}

.mr5 {
	margin-right: 5px;
}

.mt5 {
	margin-top: 5px;
}

.mb5 {
	margin-bottom: 5px;
}

.field-group-label {
	font-weight: 600;
	font-size: 12px;
}

:deep(.field-group-row) {
	background-color: var(--el-fill-color-light);
	font-weight: 600;
}

:deep(.vxe-table .vxe-table--body .vxe-body--row.field-group-row .vxe-body--column) {
	background-color: var(--el-fill-color-light);
}

:deep(.vxe-table--fixed-right-wrapper .vxe-table--body .vxe-body--row.field-group-row .vxe-body--column) {
	background-color: var(--el-fill-color-light);
}

:deep(.full-table) {
	flex: 1 1 0%;
	min-height: 0;
	display: flex;
	flex-direction: column;
	overflow: hidden;

	.el-card__body {
		flex: 1 1 0%;
		min-height: 0;
		height: 100%;
		display: flex;
		flex-direction: column;
		padding: 5px;
		overflow: hidden;
	}

	.xGrid-style {
		flex: 1 1 0%;
		min-height: 0;
		height: 100%;
		width: 100%;
		display: flex;
		flex-direction: column;
		overflow: hidden;

		.vxe-table--body .vxe-body--row.field-group-row > .vxe-body--column {
			font-weight: 600;
		}

		.vxe-grid--layout-body-wrapper,
		.vxe-grid--layout-body-content-wrapper {
			flex: 1 1 0%;
			min-height: 0;
			height: 100%;
			display: flex;
			flex-direction: column;
			overflow: hidden;
		}

		.vxe-grid--toolbar-wrapper {
			flex-shrink: 0;
		}

		.vxe-grid--table-container {
			flex: 1 1 0%;
			min-height: 0;
			height: 100%;
			display: flex;
			flex-direction: column;
			overflow: hidden;

			.vxe-grid--table-wrapper {
				flex: 1 1 0%;
				min-height: 0;
				height: 100%;
				display: flex;
				flex-direction: column;
				overflow: hidden;
			}
		}

		.vxe-table {
			flex: 1 1 0%;
			min-height: 0;
			height: 100%;
			display: flex;
			flex-direction: column;

			.vxe-table--render-wrapper,
			.vxe-table--main-wrapper {
				flex: 1 1 0% !important;
				min-height: 0 !important;
				height: 100% !important;
				display: flex !important;
				flex-direction: column !important;
			}

			.vxe-table--body-wrapper.body--wrapper {
				flex: 1 1 0% !important;
				min-height: 0 !important;
				height: 100% !important;
				overflow-y: auto;
				overflow-x: auto;
			}

			&.is--empty .vxe-table--empty-placeholder {
				height: 100% !important;
				min-height: 200px;
				display: flex;
				align-items: center;
				justify-content: center;
			}
		}

		.vxe-grid--pager-wrapper {
			flex-shrink: 0;
			margin-top: auto;
		}

		.vxe-pager {
			background-color: transparent;
			flex-wrap: nowrap;
			justify-content: flex-end;
			height: 40px;
			min-height: 40px;
			max-height: 40px;
			box-sizing: border-box;
			padding: 4px 0;
			overflow: hidden;
		}
	}
}

/* Chuẩn hóa VXE-Grid Loading & Element Loading — 8-dots spinner bự vừa vặn */
/* Lưới vxe đang tải (bảng gói tin, bảng trường): nền trắng đặc phủ hết, không thấy dòng mờ phía sau */
:deep(.vxe-grid.is--loading > .vxe-table .vxe-loading),
:deep(.vxe-table--loading.vxe-loading) {
	background-color: var(--el-bg-color);
	backdrop-filter: none;
}

:deep(.vxe-table--loading) {
	.vxe-loading--default-icon {
		display: inline-block;
		width: 50px;
		height: 50px;
		font-size: 0;
		color: transparent;
		background-color: var(--el-color-primary, #409eff);
		mask-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 50 50'%3E%3Ccircle cx='25' cy='6' r='3.4' fill='white' opacity='1'/%3E%3Ccircle cx='38.44' cy='11.56' r='3.4' fill='white' opacity='0.875'/%3E%3Ccircle cx='44' cy='25' r='3.4' fill='white' opacity='0.75'/%3E%3Ccircle cx='38.44' cy='38.44' r='3.4' fill='white' opacity='0.625'/%3E%3Ccircle cx='25' cy='44' r='3.4' fill='white' opacity='0.5'/%3E%3Ccircle cx='11.56' cy='38.44' r='3.4' fill='white' opacity='0.375'/%3E%3Ccircle cx='6' cy='25' r='3.4' fill='white' opacity='0.25'/%3E%3Ccircle cx='11.56' cy='11.56' r='3.4' fill='white' opacity='0.125'/%3E%3C/svg%3E");
		-webkit-mask-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 50 50'%3E%3Ccircle cx='25' cy='6' r='3.4' fill='white' opacity='1'/%3E%3Ccircle cx='38.44' cy='11.56' r='3.4' fill='white' opacity='0.875'/%3E%3Ccircle cx='44' cy='25' r='3.4' fill='white' opacity='0.75'/%3E%3Ccircle cx='38.44' cy='38.44' r='3.4' fill='white' opacity='0.625'/%3E%3Ccircle cx='25' cy='44' r='3.4' fill='white' opacity='0.5'/%3E%3Ccircle cx='11.56' cy='38.44' r='3.4' fill='white' opacity='0.375'/%3E%3Ccircle cx='6' cy='25' r='3.4' fill='white' opacity='0.25'/%3E%3Ccircle cx='11.56' cy='11.56' r='3.4' fill='white' opacity='0.125'/%3E%3C/svg%3E");
		mask-repeat: no-repeat;
		-webkit-mask-repeat: no-repeat;
		mask-position: center;
		-webkit-mask-position: center;
		mask-size: contain;
		-webkit-mask-size: contain;
		animation: sharedata-roll 0.9s linear infinite;
		transform-origin: center;
		will-change: transform;

		&::before {
			display: none;
		}
	}
	.vxe-loading--text {
		font-size: 14px;
		font-weight: 500;
		color: var(--el-color-primary, #409eff);
		padding-top: 10px;
	}
}

.cmp-cell-wrapper {
	display: flex;
	align-items: center;
	justify-content: center;
	width: 100%;
}

/* Ký hiệu so với mẫu như wireframe: vòng tròn màu = giống, + mới, - thiếu, ≠ khác */
.cmp-st {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 20px;
	height: 20px;
	border-radius: 50%;
	border: 1px solid;
	font-weight: 700;
	font-size: 13px;
	line-height: 1;
	padding-bottom: 2px;
	box-sizing: border-box;
	text-align: center;
	vertical-align: middle;
	cursor: help;
	user-select: none;

	&.same {
		color: var(--el-color-success);
		background: var(--el-color-success-light-9);
		border-color: var(--el-color-success-light-5);
	}

	&.new {
		color: var(--el-color-primary);
		background: var(--el-color-primary-light-9);
		border-color: var(--el-color-primary-light-5);
	}

	&.missing {
		color: var(--el-color-danger);
		background: var(--el-color-danger-light-9);
		border-color: var(--el-color-danger-light-5);
	}

	&.diff {
		color: var(--el-color-warning-dark-2);
		background: var(--el-color-warning-light-9);
		border-color: var(--el-color-warning-light-5);
	}
}

/* Lưới trường: chữ 12px đồng bộ với các bảng khác */
.fields-section :deep(.vxe-table) {
	font-size: 12px;

	.vxe-header--column .vxe-cell {
		white-space: nowrap;
	}
}

.fields-bar-right {
	display: flex;
	align-items: center;
	flex-shrink: 0;
	gap: 8px;
	margin-left: auto;
}

.fields-compare-summary {
	display: inline-flex;
	flex-shrink: 0;
	align-items: center;
	gap: 12px;
	font-size: 12px;
	color: var(--el-text-color-secondary);

	.cmp-legend {
		display: inline-flex;
		align-items: center;
		gap: 4px;
		/* Số và chữ trên một dòng, đậm nhẹ */
		white-space: nowrap;
		font-weight: 600;
		color: var(--el-text-color-regular);
	}
}

/* Tab đối tác dùng thẳng mẫu chung: chữ nhạt, nghiêng để phân biệt với tab có SQL riêng */
.partner-tab-template {
	color: var(--el-text-color-secondary);
	font-style: italic;
}

.packet-skeleton {
	display: flex;
	flex-direction: column;
	gap: 20px;
	padding: 12px 16px;

	.packet-skeleton-hint {
		color: var(--el-text-color-secondary);
		font-size: 13px;
	}
}

.packet-partner-usage {
	display: flex;
	align-items: center;
	/* Luôn nằm hàng dưới tên gói; nhiều đối tác thì tự ngắt dòng, không cắt, không cuộn ngang */
	flex: 0 0 100%;
	flex-wrap: wrap;
	min-width: 0;
	gap: 4px;
	padding-left: 23px;
	font-size: 12px;

	.usage-label,
	:deep(.el-tag) {
		flex-shrink: 0;
	}

	.usage-label {
		color: var(--el-text-color-secondary);
	}
}

:deep(.solid-loading-mask) {
	background-color: var(--el-bg-color, #fff);
	backdrop-filter: none;
	opacity: 1;
	z-index: 100;
	position: absolute;
	inset: 0;
	min-height: 100%;
}

/* Mọi vòng loading của trang: nền trắng đặc, không làm mờ (yêu cầu 27/10) */
:deep(.el-loading-mask) {
	background-color: var(--el-bg-color, #fff);
	backdrop-filter: none;
	border-radius: inherit;
	transition: opacity 0.25s ease;
	z-index: 100;
}

:deep(.el-loading-spinner) {
	top: 50%;
	transform: translateY(-50%);
	margin-top: 0;
	width: 100%;
	text-align: center;
	position: absolute;

	.circular {
		display: inline-block;
		width: 50px;
		height: 50px;
		background-color: var(--el-color-primary, #409eff);
		mask-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 50 50'%3E%3Ccircle cx='25' cy='6' r='3.4' fill='white' opacity='1'/%3E%3Ccircle cx='38.44' cy='11.56' r='3.4' fill='white' opacity='0.875'/%3E%3Ccircle cx='44' cy='25' r='3.4' fill='white' opacity='0.75'/%3E%3Ccircle cx='38.44' cy='38.44' r='3.4' fill='white' opacity='0.625'/%3E%3Ccircle cx='25' cy='44' r='3.4' fill='white' opacity='0.5'/%3E%3Ccircle cx='11.56' cy='38.44' r='3.4' fill='white' opacity='0.375'/%3E%3Ccircle cx='6' cy='25' r='3.4' fill='white' opacity='0.25'/%3E%3Ccircle cx='11.56' cy='11.56' r='3.4' fill='white' opacity='0.125'/%3E%3C/svg%3E");
		-webkit-mask-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 50 50'%3E%3Ccircle cx='25' cy='6' r='3.4' fill='white' opacity='1'/%3E%3Ccircle cx='38.44' cy='11.56' r='3.4' fill='white' opacity='0.875'/%3E%3Ccircle cx='44' cy='25' r='3.4' fill='white' opacity='0.75'/%3E%3Ccircle cx='38.44' cy='38.44' r='3.4' fill='white' opacity='0.625'/%3E%3Ccircle cx='25' cy='44' r='3.4' fill='white' opacity='0.5'/%3E%3Ccircle cx='11.56' cy='38.44' r='3.4' fill='white' opacity='0.375'/%3E%3Ccircle cx='6' cy='25' r='3.4' fill='white' opacity='0.25'/%3E%3Ccircle cx='11.56' cy='11.56' r='3.4' fill='white' opacity='0.125'/%3E%3C/svg%3E");
		mask-repeat: no-repeat;
		-webkit-mask-repeat: no-repeat;
		mask-position: center;
		-webkit-mask-position: center;
		mask-size: contain;
		-webkit-mask-size: contain;
		animation: sharedata-roll 0.9s linear infinite;
		transform-origin: center;
		will-change: transform;

		.path {
			display: none;
		}
	}

	.el-loading-text {
		color: var(--el-color-primary, #409eff);
		font-size: 14px;
		font-weight: 500;
		letter-spacing: 0.3px;
		margin: 10px 0 0;
		line-height: 1.4;
	}

	&:not(:has(.el-loading-text))::after {
		content: 'Đang tải...';
		display: block;
		color: var(--el-color-primary, #409eff);
		font-size: 14px;
		font-weight: 500;
		letter-spacing: 0.3px;
		margin-top: 10px;
		line-height: 1.4;
	}
}

@keyframes sharedata-roll {
	from {
		transform: rotate(0deg);
	}
	to {
		transform: rotate(360deg);
	}
}

:deep(.el-alert) {
	width: fit-content;
	max-width: 100%;
	align-self: flex-start;
	flex-shrink: 0;
	padding: 4px 12px;

	&:has(.el-alert__close-btn) {
		padding-right: 34px;
	}

	.el-alert__icon {
		flex-shrink: 0;
		font-size: 14px;
		margin-right: 6px;
	}

	.el-alert__title {
		font-size: 12px;
		line-height: 1.35;
		word-break: break-word;
	}

	.el-alert__close-btn {
		cursor: pointer;
		z-index: 10;
		font-size: 16px;
		width: 20px;
		height: 20px;
		padding: 0;
		margin: 0;
		top: 50%;
		right: 8px;
		transform: translateY(-50%);
		display: inline-flex;
		align-items: center;
		justify-content: center;
		transition: color 0.2s;
	}
}

.esh-datasource-container {
	/* Một trang: ① SQL cao theo trình soạn, kéo thanh dưới để đổi, không giới hạn tối đa (cao hơn khung chi tiết thì khung chi tiết cuộn); ② Danh sách trường lấp phần còn lại */
	.sql-section {
		display: flex;
		flex-direction: column;
		/* Không co, không chặn trần theo khung: khung nhìn thấp (phóng to trang) mà chặn trần thì khung soạn bị ép còn một dòng, cắt mất thanh kéo */
		flex: 0 0 auto;
		min-height: 0;
		margin-bottom: 6px;
		margin-right: 8px;
		overflow: hidden;
		padding: 4px;
		border: 1px solid var(--el-border-color-lighter);
		border-radius: 6px;
		background: var(--el-bg-color);

		&.is-scope-locked {
			background: var(--el-fill-color-lighter);

			:deep(.sql-editor-bar) {
				background: var(--el-fill-color-lighter);
			}
		}
	}

	/* ③④ Tham số câu truy vấn và Mốc gửi: thẻ nằm trong sqlVariablePanel; chiều cao tăng tự nhiên, không ép giãn gây khoảng trắng thừa bên dưới */
	.sql-variable-section,
	.cursor-block-section {
		flex: 0 0 auto;
		height: auto;
		margin-top: 6px;
		margin-bottom: 6px;
		margin-right: 8px;
		box-sizing: border-box;
		display: flex;
		flex-direction: column;
	}

	.fields-section {
		/* Cao cố định 380px như cũ (cuộn nội bộ khi nhiều trường); đóng hết các nhóm thì thấp lại theo dòng nhóm */
		flex: 0 0 auto;
		height: auto;
		margin-bottom: 6px;
		margin-right: 8px;
		padding: 6px 8px;
		border: 1px solid var(--el-border-color-lighter);
		border-radius: 6px;
		background: var(--el-bg-color);
		box-sizing: border-box;

		&.is-scope-locked {
			background: var(--el-fill-color-lighter);

			.saved-fields-grid-wrapper {
				opacity: 0.78;

				:deep(.vxe-table--render-default .vxe-table--body-wrapper),
				:deep(.vxe-table .vxe-table--body .vxe-body--row:not(.field-group-row) .vxe-body--column) {
					background-color: var(--el-fill-color-light);
				}
			}
		}

		.saved-fields-grid-wrapper {
			min-width: 0;
			width: 100%;
			height: 380px;
			margin-top: 6px;
			display: flex;
			flex-direction: column;

			&.is-auto-height {
				height: auto;

				:deep(.xGrid-style) {
					height: auto;
				}
			}
		}

		&.is-maximized {
			padding: 12px;

			.saved-fields-grid-wrapper {
				flex: 1;
				height: 100%;
				min-height: 0;
				overflow: hidden;
			}
		}

		/* Thanh tiêu đề: giống thanh khung soạn SQL — tiêu đề bên trái, nút Thu gọn/Mở rộng sát mép phải */
		.fields-bar {
			display: flex;
			/* Hẹp thì khối bên phải (chú giải + nút) xuống hàng, không đè tiêu đề */
			flex-wrap: wrap;
			row-gap: 4px;
			column-gap: 8px;
			align-items: center;
			justify-content: space-between;
			flex-shrink: 0;
			padding-bottom: 6px;
			border-bottom: 1px solid var(--el-border-color-lighter);

			.fields-bar-left {
				display: flex;
				align-items: center;
				gap: 6px;
				flex: 1 1 auto;
				min-width: 0;
				overflow: hidden;
				white-space: nowrap;
				cursor: pointer;
				user-select: none;
			}

			.fields-title {
				font-size: 13px;
				font-weight: 600;
				color: var(--el-text-color-primary);
			}

			.fields-badge-count {
				display: inline-flex;
				align-items: center;
				justify-content: center;
				padding: 1px 7px;
				border-radius: 10px;
				font-size: 12px;
				font-weight: 500;
				background: var(--el-fill-color-darker);
				color: var(--el-text-color-regular);
				line-height: 1.2;
			}

			.fields-hint {
				font-size: 12px;
				font-weight: 400;
				color: var(--el-text-color-secondary);
				margin-left: 2px;
				/* Dòng gợi ý dài thì cắt "…" thay vì bị khối bên phải đè lên */
				min-width: 0;
				overflow: hidden;
				text-overflow: ellipsis;
			}
		}

		&.is-collapsed {
			.fields-bar {
				padding-bottom: 0;
				border-bottom: none;
			}
		}

		/* Mở rộng: phần danh sách trường nổi lên khung lớn giữa màn hình, nền mờ phía sau */
		&.is-maximized {
			position: fixed;
			inset: 24px;
			z-index: 2001;
			padding: 12px;
			background: var(--el-bg-color);
			border: 1px solid var(--el-border-color);
			border-radius: var(--el-border-radius-base);
			box-shadow: var(--el-box-shadow-dark);
		}
	}

	.fields-maximize-backdrop {
		position: fixed;
		inset: 0;
		z-index: 2000;
		background: rgba(0, 0, 0, 0.45);
	}

	.partner-compare-summary {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: 4px 8px;
		flex-shrink: 0;
		margin-bottom: 5px;
		font-size: 12px;

		.partner-compare-title {
			font-weight: 600;
		}
	}

	/* ④ Ghi ngược: cao theo nội dung, không bị flex ép nhỏ (khung chi tiết cuộn khi không đủ chỗ) */
	.write-sql-section {
		flex: 0 0 auto;
		padding-right: 8px;
		box-sizing: border-box;
	}

	.direction-tabs {
		flex-shrink: 0;
		margin-bottom: 4px;
		padding-right: 8px;

		:deep(.el-tabs__header) {
			margin-bottom: 0;
		}

		:deep(.el-tabs__nav-wrap),
		:deep(.el-tabs__nav-scroll) {
			height: 32px;
		}

		:deep(.el-tabs__item) {
			height: 32px;
			line-height: 32px;
			padding: 0 10px;
			font-size: 13px;

			&:nth-child(2) {
				padding-left: 0;
			}

			&:last-child {
				padding-right: 0;
			}
		}
	}

	.fields-bar-actions {
		display: flex;
		align-items: center;
		gap: 4px;
	}

	.fields-compare-summary {
		font-size: 12px;
		color: var(--el-text-color-secondary);
		margin-left: 4px;
	}
}

:deep(.field-missing-row) {
	opacity: 0.62;
	background-color: var(--el-fill-color-lighter);
}

:global(.lost-keys-confirm-box.el-message-box--center) {
	text-align: left;
}
:global(.lost-keys-confirm-box.el-message-box--center .el-message-box__header) {
	text-align: left;
}
:global(.lost-keys-confirm-box.el-message-box--center .el-message-box__title) {
	display: flex;
	justify-content: flex-start;
	align-items: center;
	gap: 8px;
	width: 100%;
}
:global(.lost-keys-confirm-box.el-message-box--center .el-message-box__status) {
	font-size: 20px;
}
:global(.lost-keys-confirm-box.el-message-box--center .el-message-box__btns) {
	justify-content: flex-end;
}
:global(.lost-keys-confirm-box.el-message-box--center .el-message-box__container) {
	justify-content: flex-start;
	text-align: left;
}
:global(.lost-keys-confirm-box.el-message-box--center .el-message-box__message) {
	text-align: left;
}

/* Nút Xem tất cả / Thu gọn của dòng "Đối tác dùng gói" và hộp cảnh báo khoá câu SQL */
.packet-partner-usage .partner-usage-toggle,
.sql-lock-alert .sql-lock-toggle {
	height: 20px;
	padding: 0 4px;
	font-size: 12px;
}

.sql-lock-alert .sql-lock-toggle {
	margin-left: 4px;
	font-weight: 600;
	vertical-align: baseline;
}

/* Tooltip danh sách đối tác ở cột Đối tác bảng Gói tin: giới hạn bề rộng theo tỷ lệ màn hình, có min-width gọn gàng */
:global(.el-popper.partner-count-tooltip) {
	min-width: 160px;
	width: max-content;
	max-width: 28vw;
	line-height: 1.5;
	word-break: break-word;
	white-space: normal;
}
</style>

<style lang="scss">
/* Tooltips render in <body>: styled here, not in the scoped block */
.el-popper.data-source-tooltip {
	box-sizing: border-box;
	width: max-content;
	max-width: 28vw;
	line-height: 1.5;
	white-space: pre-line;
	overflow-wrap: anywhere;
}

.vxe-tooltip--wrapper.data-source-tooltip .vxe-tooltip--content {
	box-sizing: border-box;
	max-width: 28vw;
	overflow-wrap: anywhere;
}
</style>
