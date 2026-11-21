<template>
  <div class="right-panel" :class="{ 'collapsed': isPanelCollapsed }" :style="{ height: panelHeight }" ref="panelRef">
    <!-- Collapse Toggle Button -->
    <div class="collapse-toggle" @click="togglePanel">
      <el-icon>
        <component :is="isPanelCollapsed ? 'ele-ArrowLeft' : 'ele-ArrowRight'" />
      </el-icon>
    </div>

    <!-- Resize Bar: kéo để chỉnh chiều cao + nút thao tác nhanh -->
    <div class="resize-bar" v-show="!isPanelCollapsed" @mousedown="startResize"
      :title="$t('lz.label.tmsMapLocation.dragToResizeHeight')">
      <div class="resize-bar__grip"></div>
      <div class="resize-bar__actions" @mousedown.stop @click.stop>
        <el-tooltip :content="$t('lz.button.tmsMapLocation.heightHalf')" placement="top">
          <el-button size="small" circle @click="setPanelHeight('50%')">
            <span class="resize-bar__label">½</span>
          </el-button>
        </el-tooltip>
        <el-tooltip :content="$t('lz.button.tmsMapLocation.heightFull')" placement="top">
          <el-button size="small" circle @click="setPanelHeight('100%')">
            <el-icon><ele-FullScreen /></el-icon>
          </el-button>
        </el-tooltip>
        <el-tooltip :content="$t('lz.button.tmsMapLocation.heightReset')" placement="top">
          <el-button size="small" circle @click="setPanelHeight(DEFAULT_PANEL_HEIGHT)">
            <el-icon><ele-RefreshLeft /></el-icon>
          </el-button>
        </el-tooltip>
      </div>
    </div>

    <!-- Panel Content -->
    <div class="panel-content" v-show="!isPanelCollapsed">
      <!-- Toolbar: chọn nhóm + đối tượng để thêm lên bản đồ | màu nền (bản đồ ảnh) + Lưu -->
      <div class="toolbar">
        <div class="toolbar__add">
          <el-select v-model="state.queryParams.itemGroup"
            :placeholder="$t('lz.label.tmsMapLocation.selectItemGroup')"
            filterable clearable size="small" class="toolbar__select">
            <template #prefix><el-icon><ele-CirclePlus /></el-icon></template>
            <el-option v-for="item in state.itemGroup" :key="item.code" :label="item.name" :value="item.code" />
          </el-select>
          <el-cascader v-if="!isAnnotationGroup(state.queryParams.itemGroup)"
            v-model="state.queryParams.itemId" :options="filteredItemData" :disabled="isItemIdDisabled"
            :placeholder="$t('lz.label.tmsMapLocation.selectItemToAdd')" size="small" class="toolbar__cascader" filterable
            :props="{ value: 'id', label: 'name', children: 'children', disabled: 'disabled', emitPath: false }" clearable>
            <template #default="{ node, data }">
              <span :class="{ 'text-disabled': data.disabled }">{{ data.name }}</span>
              <span v-if="data.children && data.children.length">({{ data.children.length }})</span>
              <span v-if="data.disabled" class="text-muted">({{ $t('lz.label.base.used') }})</span>
            </template>
          </el-cascader>
          <!-- Nút thêm cho device/unit (cần chọn item) -->
          <el-button size="small" type="primary" @click="handleAddItem"
            v-if="state.queryParams.itemGroup !== 'zone' && !isAnnotationGroup(state.queryParams.itemGroup)"
            :disabled="!state.queryParams.itemId">
            <el-icon><ele-Plus /></el-icon>
            <span>{{ $t('lz.button.tmsMapLocation.addToMap') }}</span>
          </el-button>
          <!-- Nút thêm nhãn văn bản -->
          <el-button size="small" type="primary" @click="openAddTextDialog"
            v-else-if="state.queryParams.itemGroup === 'text'">
            <el-icon><ele-Plus /></el-icon>
            <span>{{ $t('lz.button.tmsMapLocation.addText') }}</span>
          </el-button>
          <!-- Nút chèn hình ảnh lên bản đồ -->
          <el-button size="small" type="primary" @click="openAddImageDialog"
            v-else-if="state.queryParams.itemGroup === 'image'">
            <el-icon><ele-Plus /></el-icon>
            <span>{{ $t('lz.button.tmsMapLocation.addImage') }}</span>
          </el-button>
          <!-- Nút chèn khối HTML (chữ + hình) lên bản đồ -->
          <el-button size="small" type="primary" @click="openAddHtmlDialog"
            v-else-if="state.queryParams.itemGroup === 'html'">
            <el-icon><ele-Plus /></el-icon>
            <span>{{ $t('lz.button.tmsMapLocation.addHtml') }}</span>
          </el-button>
          <el-button size="small" type="primary" @click="handleStartDrawingPolygon" v-else>
            <el-icon><ele-EditPen /></el-icon>
            <span>{{ $t('lz.button.tmsMapLocation.drawPolygon') }}</span>
          </el-button>
        </div>
        <div class="toolbar__actions">
          <el-button size="small" type="success" @click="handleSave" class="toolbar__save">
            <el-icon><ele-Select /></el-icon>
            <span>{{ $t('lz.button.tmsMapLocation.save') }}</span>
          </el-button>
        </div>
      </div>

      <el-tabs v-model="activeTab" class="main-tabs">
        <!-- Tab Vị trí: danh sách đối tượng trên bản đồ theo nhóm (thiết bị / zone / đơn vị / chú thích) -->
        <el-tab-pane :label="$t('lz.label.tms.location')" name="table">
          <div class="table-container">
            <div class="table-toolbar">
              <el-input v-model="tableSearch" :placeholder="$t('lz.label.tmsMapLocation.searchByNameOrType')"
                clearable size="small" @clear="handleSearchClear" prefix-icon="Search" class="table-toolbar__search" />
              <el-tooltip :content="$t(isAllGroupsCollapsed ? 'lz.label.tmsMapLocation.expandAllGroups' : 'lz.label.tmsMapLocation.collapseAllGroups')">
                <el-button size="small" class="table-toolbar__toggle" @click="toggleAllGroups">
                  <el-icon><component :is="isAllGroupsCollapsed ? 'ele-Expand' : 'ele-Fold'" /></el-icon>
                </el-button>
              </el-tooltip>
            </div>

            <el-tabs v-model="activeName" class="sub-tabs" @tab-click="handleClickTabAccordion">
              <el-tab-pane v-for="tab in locationTabs" :key="tab.code" :name="tab.code">
                <template #label>
                  <span class="loc-tab">
                    <el-icon><component :is="tab.icon" /></el-icon>
                    <span>{{ tab.name }}</span>
                    <span class="loc-tab__count">{{ tab.count }}</span>
                  </span>
                </template>

                <!-- Trống: chưa có đối tượng / không khớp tìm kiếm -->
                <div v-if="!locationGroups[tab.code]?.length" class="loc-empty">
                  <el-icon class="loc-empty__icon"><component :is="inputSearchDataTable ? 'ele-Search' : tab.icon" /></el-icon>
                  <span>{{ inputSearchDataTable ? $t('lz.label.tmsMapLocation.locationNoMatch') : $t('lz.label.tmsMapLocation.locationEmpty') }}</span>
                </div>

                <SettingsSection v-for="group in locationGroups[tab.code]" :key="group.key" class="loc-group"
                  :title="group.title" :icon="tab.icon" :count="group.items.length"
                  :collapsed="!!collapsedAccordion[group.key]" @update:collapsed="(val: boolean) => (collapsedAccordion[group.key] = val)">
                  <div v-for="item in group.items" :key="item.id" class="item-row" :data-item-id="item.id"
                    :class="{ 'is-expanded': isRowExpanded(item), 'is-block-only': item.blockOnly }">
                    <!-- Dòng chính: icon | tên + thông tin tóm tắt | thao tác | mở chi tiết -->
                    <div class="item-row__head" :class="{ 'is-expandable': isRowExpandable(item) }" @click="toggleRow(item)">
                      <span class="item-row__icon" :class="`item-row__icon--${item.itemGroup}`">
                        <img v-if="getRowIcon(item)" :src="getRowIcon(item)" alt="" @error="hideBrokenRowIcon" />
                        <el-icon v-else><component :is="getRowFallbackIcon(item)" /></el-icon>
                      </span>
                      <div class="item-row__main">
                        <div class="item-row__name" :title="item.name">{{ item.name }}</div>
                        <div class="item-row__meta">
                          <!-- Thiết bị nằm trong block: hiện block · tầng chứa nó -->
                          <span v-if="item.blockPlacement" class="item-row__tag item-row__tag--block" :title="$t('lz.label.tmsMapLocation.inBlock')">
                            <el-icon><ele-Grid /></el-icon>
                            <span>{{ item.blockPlacement.blockName }}<template v-if="item.blockPlacement.levelName"> · {{ item.blockPlacement.levelName }}</template></span>
                          </span>
                          <span v-if="item.blockOnly" class="item-row__muted">{{ $t('lz.label.tmsMapLocation.blockOnlyHint') }}</span>
                          <span v-else-if="item.geometryType === 'Point'" class="item-row__coords-text">{{ formatCoord(item.lat) }}, {{ formatCoord(item.lng) }}</span>
                          <template v-if="item.itemGroup === 'zone' && item.geometryType === 'Polygon'">
                            <span v-if="item.fromKmNumber != null" class="item-row__tag">
                              Km{{ item.fromKmNumber }}+{{ item.fromMetNumber || 0 }} – Km{{ item.toKmNumber }}+{{ item.toMetNumber || 0 }}
                            </span>
                            <span v-if="item.flowDirection && item.flowDirection !== TmsZoneKmDirection.Auto" class="item-row__tag item-row__tag--flow" :title="$t('lz.label.tmsMapLocation.flowDirection')">
                              <el-icon><Right /></el-icon>{{ getFlowDirectionLabel(item.flowDirection) }}
                            </span>
                            <span v-if="item.kmDirection && item.kmDirection !== TmsZoneKmDirection.Auto" class="item-row__tag item-row__tag--km" :title="$t('lz.label.tmsMapLocation.kmDirection')">
                              <el-icon><Guide /></el-icon>{{ getKmDirectionLabel(item.kmDirection) }}
                            </span>
                            <!-- Tốc độ luôn hiện ở dòng chính để thấy ngay ô đang sửa / sai (Min > Max) mà không cần mở chi tiết -->
                            <span v-if="mapStore.zoneSpeedRows[item.id]" class="item-row__tag item-row__tag--speed" :class="getZoneSpeedRowClass(item.id)"
                              :title="$t('lz.label.tmsMapLocation.zoneSpeedHint')">
                              <el-icon><ele-Odometer /></el-icon>{{ getZoneSpeedSummary(item.id) }}
                            </span>
                          </template>
                        </div>
                      </div>
                      <div class="item-row__actions" @click.stop>
                        <el-tooltip :content="$t('lz.button.tmsMapLocation.locateOnMap')">
                          <el-button size="small" text class="item-row__btn" @click="handleLocateOnMap(item)" :icon="Aim" />
                        </el-tooltip>
                        <!-- Chú thích (chữ/ảnh/HTML): sửa nội dung trong hộp thoại; kéo thả trực tiếp trên bản đồ -->
                        <template v-if="isAnnotationGroup(item.itemGroup)">
                          <el-tooltip :content="$t('lz.button.base.edit')">
                            <el-button size="small" text class="item-row__btn" @click="openAnnotationRowEditor(item)" :icon="Edit" />
                          </el-tooltip>
                        </template>
                        <!-- Thiết bị chỉ nằm trong block: sửa/gỡ ở tab "Thiết lập block" -->
                        <el-tooltip v-else-if="item.blockOnly" :content="$t('lz.label.tmsMapLocation.openBlockSetup')">
                          <el-button size="small" text class="item-row__btn" @click="openBlockSetup" :icon="Setting" />
                        </el-tooltip>
                        <template v-else>
                          <el-tooltip v-if="item.geometryType === 'Polygon'" :content="$t('lz.label.tmsMapLocation.editShape')">
                            <el-button size="small" text class="item-row__btn" @click="handleEditOnMap(item)" :icon="Edit" />
                          </el-tooltip>
                          <el-tooltip :content="$t('lz.button.tmsMapLocation.moveOnMap')">
                            <el-button size="small" text class="item-row__btn" @click="handleMoveOnMap(item)" :icon="Rank" />
                          </el-tooltip>
                          <!-- Lật ngang icon thiết bị đặt lẻ (vd 2 barrier quay mặt trái/phải) -->
                          <el-tooltip v-if="item.itemGroup === 'device' && item.geometryType === 'Point'" :content="$t('lz.button.tmsMapLocation.flipIconHorizontal')">
                            <el-button size="small" text class="item-row__btn" :type="isDeviceIconFlipped(item) ? 'primary' : 'default'"
                              :aria-pressed="isDeviceIconFlipped(item)" @click="handleToggleIconFlip(item)" :icon="FlipIcon" />
                          </el-tooltip>
                        </template>
                        <el-tooltip v-if="!item.blockOnly" :content="$t('lz.button.base.delete')">
                          <el-button size="small" text class="item-row__btn item-row__btn--danger" @click="handleDelete(item)" :icon="Delete" />
                        </el-tooltip>
                      </div>
                      <el-icon v-if="isRowExpandable(item)" class="item-row__chevron"><ele-ArrowDown /></el-icon>
                    </div>

                    <!-- Chi tiết (mở khi bấm vào dòng): toạ độ, hướng, tốc độ. v-if để không dựng hàng trăm ô nhập bị ẩn. -->
                    <el-collapse-transition>
                      <div v-if="isRowExpanded(item)" class="item-row__body">
                        <template v-if="item.geometryType === 'Point'">
                          <label class="item-field">
                            <span class="item-field__label">{{ $t('lz.label.tmsMapLocation.latitude') }}</span>
                            <el-input-number v-model="item.lat" :min="0" :precision="6" :step="0.0001" size="small" controls-position="right"
                              @change="(val: any) => handleLocationUpdate(item, val, 'lat')" />
                          </label>
                          <label class="item-field">
                            <span class="item-field__label">{{ $t('lz.label.tmsMapLocation.longitude') }}</span>
                            <el-input-number v-model="item.lng" :min="0" :precision="6" :step="0.0001" size="small" controls-position="right"
                              @change="(val: any) => handleLocationUpdate(item, val, 'lng')" />
                          </label>
                        </template>
                        <template v-else-if="item.geometryType === 'Polygon'">
                          <label class="item-field">
                            <span class="item-field__label">{{ $t('lz.label.tmsMapLocation.firstPointLatitude') }}</span>
                            <el-input-number :model-value="item.topLeft.lat" :min="0" :precision="6" :step="0.0001" size="small" controls-position="right"
                              @change="(val: any) => handlePolygonUpdate(item, val, 'lat')" />
                          </label>
                          <label class="item-field">
                            <span class="item-field__label">{{ $t('lz.label.tmsMapLocation.firstPointLongitude') }}</span>
                            <el-input-number :model-value="item.topLeft.lng" :min="0" :precision="6" :step="0.0001" size="small" controls-position="right"
                              @change="(val: any) => handlePolygonUpdate(item, val, 'lng')" />
                          </label>
                          <template v-if="item.itemGroup === 'zone'">
                            <!-- Hướng DI CHUYỂN của tuyến (flow) -->
                            <label class="item-field">
                              <span class="item-field__label">{{ $t('lz.label.tmsMapLocation.flowDirection') }}</span>
                              <el-select v-model="item.flowDirection" size="small" @change="(val: any) => handleFlowDirectionChange(item, val)">
                                <template #prefix><el-icon><Right /></el-icon></template>
                                <el-option :label="$t('lz.label.tmsMapLocation.flowDirectionAuto')" :value="TmsZoneKmDirection.Auto" />
                                <el-option :label="$t('lz.label.tmsMapLocation.flowDirectionForward')" :value="TmsZoneKmDirection.Forward" />
                                <el-option :label="$t('lz.label.tmsMapLocation.flowDirectionReverse')" :value="TmsZoneKmDirection.Reverse" />
                              </el-select>
                            </label>
                            <!-- Hướng LÝ TRÌNH Km (định vị sự cố) -->
                            <label class="item-field">
                              <span class="item-field__label">{{ $t('lz.label.tmsMapLocation.kmDirection') }}</span>
                              <el-select v-model="item.kmDirection" size="small" @change="(val: any) => handleKmDirectionChange(item, val)">
                                <template #prefix><el-icon><Guide /></el-icon></template>
                                <el-option :label="$t('lz.label.tmsMapLocation.kmDirectionAuto')" :value="TmsZoneKmDirection.Auto" />
                                <el-option :label="$t('lz.label.tmsMapLocation.kmDirectionForward')" :value="TmsZoneKmDirection.Forward" />
                                <el-option :label="$t('lz.label.tmsMapLocation.kmDirectionReverse')" :value="TmsZoneKmDirection.Reverse" />
                              </el-select>
                            </label>
                            <!-- Tốc độ min–max (km/h): để trống = mặc định theo hướng; lưu cùng "Lưu bản đồ" -->
                            <div v-if="mapStore.zoneSpeedRows[item.id]" class="item-field item-field--wide item-row__speed" :class="getZoneSpeedRowClass(item.id)">
                              <span class="item-field__label">{{ $t('lz.label.tmsMapLocation.zoneSpeedLabel') }}</span>
                              <div class="item-field__pair">
                                <el-input-number :model-value="mapStore.getZoneSpeedValue(item.id)?.minSpeed ?? undefined"
                                  :min="1" :max="ZONE_SPEED_MAX_KMH" :step="5" :precision="0" size="small" controls-position="right"
                                  :placeholder="getZoneSpeedPlaceholder(item.id, 'minSpeed')"
                                  @change="(val: any) => handleZoneSpeedChange(item.id, 'minSpeed', val)">
                                  <template #prefix><span class="item-row__speed-tag">{{ $t('lz.label.tmsMapLocation.zoneSpeedMin') }}</span></template>
                                </el-input-number>
                                <span class="item-field__dash">–</span>
                                <el-input-number :model-value="mapStore.getZoneSpeedValue(item.id)?.maxSpeed ?? undefined"
                                  :min="1" :max="ZONE_SPEED_MAX_KMH" :step="5" :precision="0" size="small" controls-position="right"
                                  :placeholder="getZoneSpeedPlaceholder(item.id, 'maxSpeed')"
                                  @change="(val: any) => handleZoneSpeedChange(item.id, 'maxSpeed', val)">
                                  <template #prefix><span class="item-row__speed-tag">{{ $t('lz.label.tmsMapLocation.zoneSpeedMax') }}</span></template>
                                </el-input-number>
                              </div>
                              <span class="item-field__hint">{{ $t('lz.label.tmsMapLocation.zoneSpeedHint') }}</span>
                            </div>
                          </template>
                        </template>
                      </div>
                    </el-collapse-transition>
                  </div>
                </SettingsSection>
              </el-tab-pane>
            </el-tabs>
          </div>
        </el-tab-pane>
    <!-- Config Tab: 3 tab con
         - "Khung nhìn" + "Hiển thị": lưu cùng bản đồ, áp dụng mọi màn hình
         - "Chỉ trang thiết lập": minh hoạ hỗ trợ cấu hình (nhãn zone, mũi tên), không lưu -->
    <el-tab-pane :label="$t('lz.label.tms.config')" name="configMap">
      <el-tabs v-model="activeConfigTab" class="config-subtabs">
        <!-- Khung nhìn: quan trọng nhất nên là tab đầu tiên; lưu cùng bản đồ, áp dụng mọi màn hình -->
        <el-tab-pane name="view">
          <template #label>
            <span class="config-subtabs__label"><el-icon><ele-MapLocation /></el-icon>{{ $t('lz.label.tmsMapLocation.configViewTab') }}</span>
          </template>
          <div class="config-container">
            <div class="config-scope config-scope--shared">
              <el-icon><ele-CircleCheckFilled /></el-icon>
              <span>{{ $t('lz.label.tmsMapLocation.configViewHint') }}</span>
            </div>

            <!-- Mỗi giá trị: hiện tại (chỉ đọc) → giá trị mới (xem trước ngay trên bản đồ) → ✓ áp dụng.
                 Cột cố định (xem .view-field) để mọi dòng, mọi nhóm thẳng hàng nhau. -->
            <SettingsSection v-for="group in viewFieldGroups" :key="group.key" :title="group.title" :icon="group.icon"
              v-model:collapsed="collapsedConfig[group.key]">
              <div class="view-field view-field--head">
                <span />
                <span>{{ $t('lz.label.tmsMapLocation.currentValue') }}</span>
                <span />
                <span>{{ $t('lz.label.tmsMapLocation.newValue') }}</span>
                <span />
              </div>
              <div v-for="item in group.fields" :key="item.field" class="view-field"
                :class="{ 'is-changed': isViewFieldChanged(item) }">
                <span class="view-field__label" :title="item.label">{{ item.label }}</span>
                <span class="view-field__current">{{ formatViewValue(state.defaultValue[item.field], item.precision) }}</span>
                <el-icon class="view-field__arrow"><ele-Right /></el-icon>
                <el-input-number v-model="state.queryParams[item.field]" class="view-field__adjust" :precision="item.precision"
                  :step="item.step" :min="item.min" :max="item.max" :placeholder="item.placeholder" size="default"
                  controls-position="right" @change="(val: any) => handleViewFieldChange(item.field, val)" />
                <el-tooltip :content="$t('lz.label.tmsMapLocation.applyValue')" placement="top">
                  <el-button size="default" type="primary" :plain="!isViewFieldChanged(item)" class="view-field__apply"
                    :aria-label="$t('lz.label.tmsMapLocation.applyValue')" @click="handleSetConfig($event, item.field)">
                    <el-icon><ele-Check /></el-icon>
                  </el-button>
                </el-tooltip>
              </div>
              <!-- Khóa zoom: chỉ áp dụng ở bản đồ chính; màn cấu hình này vẫn zoom tự do -->
              <SettingRow v-if="group.key === 'zoom'" :label="$t('lz.label.tmsMapLocation.lockZoom')" :hint="$t('lz.label.tmsMapLocation.lockZoomHint')">
                <el-switch :model-value="props.lockZoom" @change="(val: any) => emit('toggle-lock-zoom', !!val)" />
              </SettingRow>
              <!-- Khóa xoay: bản đồ chính giữ đúng góc xoay "Hiện tại"; màn cấu hình này vẫn xoay tự do -->
              <SettingRow v-if="group.key === 'rotation'" :label="$t('lz.label.tmsMapLocation.lockRotation')" :hint="$t('lz.label.tmsMapLocation.lockRotationHint')">
                <el-switch :model-value="props.lockRotation" @change="(val: any) => emit('toggle-lock-rotation', !!val)" />
              </SettingRow>
              <el-button v-if="group.key === 'boundRadius'" size="default" type="warning" plain class="view-field__capture"
                @click="handleCaptureBoundFromView">
                <el-icon><ele-FullScreen /></el-icon>
                <span>{{ $t('lz.button.tmsMapLocation.captureBoundFromView') }}</span>
              </el-button>
            </SettingsSection>
          </div>
        </el-tab-pane>

        <!-- Hiển thị: lưu cùng bản đồ, áp dụng mọi màn hình -->
        <el-tab-pane name="shared">
          <template #label>
            <span class="config-subtabs__label"><el-icon><ele-Brush /></el-icon>{{ $t('lz.label.tmsMapLocation.configDisplayTab') }}</span>
          </template>
          <div class="config-container">
            <div class="config-scope config-scope--shared">
              <el-icon><ele-CircleCheckFilled /></el-icon>
              <span>{{ $t('lz.label.tmsMapLocation.configSharedHint') }}</span>
            </div>

            <SettingsSection :title="$t('lz.label.tmsMapLocation.deviceIconGroup')" icon="ele-Cpu" v-model:collapsed="collapsedConfig.deviceIcon">
              <SettingRow :label="$t('lz.label.base.iconBackground')" :hint="$t('lz.label.tmsMapLocation.iconBackgroundHint')">
                <el-switch :model-value="props.showIconBackground" @change="(val: any) => emit('toggle-icon-background', !!val)" />
              </SettingRow>
              <SettingRow :label="$t('lz.label.tmsMapLocation.iconSize')" :hint="$t('lz.label.tmsMapLocation.iconSizeHint')">
                <el-input-number v-model="mapIconSize" :min="0" :step="1" :precision="0" size="default" controls-position="right" class="config-number" />
                <span>px</span>
              </SettingRow>
            </SettingsSection>

            <SettingsSection :title="$t('lz.label.tmsMapLocation.incidentGroup')" icon="ele-WarningFilled" v-model:collapsed="collapsedConfig.incident">
              <!-- Sự kiện chỉ vẽ ở màn giám sát → không xem trước được tại trang này -->
              <div class="config-note">
                <el-icon><ele-InfoFilled /></el-icon>
                <span>{{ $t('lz.label.tmsMapLocation.incidentRuntimeOnlyHint') }}</span>
              </div>
              <SettingRow :label="$t('lz.label.tmsMapLocation.incidentIconSize')" :hint="$t('lz.label.tmsMapLocation.incidentIconSizeHint')">
                <el-input-number v-model="mapIncidentIconSize" :min="0" :step="1" :precision="0" size="default" controls-position="right" class="config-number" />
                <span>px</span>
              </SettingRow>
              <SettingRow :label="$t('lz.label.tmsMapLocation.incidentZoneGap')" :hint="$t('lz.label.tmsMapLocation.incidentZoneGapHint')">
                <el-input-number v-model="mapIncidentZoneGap" :min="0" :step="1" :precision="1" size="default" controls-position="right" class="config-number"
                  :aria-label="$t('lz.label.tmsMapLocation.incidentZoneGap')" />
                <span>px</span>
              </SettingRow>
              <SettingRow :label="$t('lz.label.tmsMapLocation.incidentLineStyle')" :hint="$t('lz.label.tmsMapLocation.incidentLineStyleHint')" stacked>
                <el-segmented v-model="mapIncidentLineStyle" :options="incidentLineStyleOptions" size="default" block class="config-segmented" />
              </SettingRow>
            </SettingsSection>

            <!-- Zone & đường định hướng: độ dày viền zone + màu đường định hướng THEO HƯỚNG -->
            <SettingsSection :title="$t('lz.label.tmsMapLocation.zoneRouteGroup')" icon="ele-Guide" v-model:collapsed="collapsedConfig.zoneRoute">
              <SettingRow :label="$t('lz.label.tmsMapLocation.zoneBorderWidth')" :hint="$t('lz.label.tmsMapLocation.zoneBorderWidthHint')">
                <el-input-number v-model="mapZoneBorderWidth" :min="0" :max="ZONE_BORDER_WIDTH_MAX" :step="0.5" :precision="1" size="default"
                  controls-position="right" class="config-number" :aria-label="$t('lz.label.tmsMapLocation.zoneBorderWidth')" />
                <span>px</span>
              </SettingRow>

              <SettingRow :label="$t('lz.label.tmsMapLocation.flowLineWidth')" :hint="$t('lz.label.tmsMapLocation.flowLineWidthHint')">
                <el-input-number v-model="mapFlowLineWidth" :min="FLOW_LINE_WIDTH_MIN" :max="FLOW_LINE_WIDTH_MAX" :step="0.5" :precision="1" size="default"
                  controls-position="right" class="config-number" :aria-label="$t('lz.label.tmsMapLocation.flowLineWidth')" />
                <span>px</span>
              </SettingRow>

              <SettingRow :label="$t('lz.label.tmsMapLocation.flowLineColor')" :hint="$t('lz.label.tmsMapLocation.flowLineColorHint')" stacked>
                <div class="flow-color-list">
                  <div v-for="item in flowLineColorRows" :key="item.key" class="flow-color-row" :class="{ 'is-all': item.key === FLOW_LINE_ALL_KEY }">
                    <span class="flow-color-row__name" :title="item.name">{{ item.name }}</span>
                    <span v-if="item.zoneCount" class="flow-color-row__count">{{ $t('lz.label.tmsMapLocation.flowLineZoneCount', { count: item.zoneCount }) }}</span>
                    <el-color-picker :model-value="props.flowLineColors?.[item.key] || ''" size="default" :predefine="flowLineColorPresets"
                      @change="(val: any) => handleFlowLineColorChange(item.key, val)" />
                  </div>
                </div>
              </SettingRow>
            </SettingsSection>

            <!-- Màu nền: chỉ bản đồ dạng ảnh (bản đồ online luôn nền trắng) -->
            <SettingsSection v-if="props.currentMapType === 'image'" :title="$t('lz.label.tmsMapLocation.mapBackgroundColor')" icon="ele-PictureFilled"
              v-model:collapsed="collapsedConfig.background">
              <SettingRow :label="$t('lz.label.tmsMapLocation.mapBackgroundColor')" :hint="$t('lz.label.tmsMapLocation.mapBackgroundColorHint')">
                <el-color-picker v-model="state.queryParams.backgroundColor" show-alpha size="default" @change="handleBackgroundColorChange" />
              </SettingRow>
            </SettingsSection>
          </div>
        </el-tab-pane>

        <el-tab-pane name="local">
          <template #label>
            <span class="config-subtabs__label"><el-icon><ele-Tools /></el-icon>{{ $t('lz.label.tmsMapLocation.configLocalTab') }}</span>
          </template>
          <div class="config-container">
            <div class="config-scope config-scope--local">
              <el-icon><ele-View /></el-icon>
              <span>{{ $t('lz.label.tmsMapLocation.configLocalHint') }}</span>
            </div>

            <SettingsSection :title="$t('lz.label.tmsMapLocation.zoneLabelGroup')" icon="ele-PriceTag" v-model:collapsed="collapsedConfig.zoneLabel">
              <SettingRow :label="$t('lz.label.tmsMapLocation.showZoneName')">
                <el-switch :model-value="props.zoneLabelDisplay.name" @change="(val: any) => emit('toggle-zone-label', { key: 'name', value: !!val })" />
              </SettingRow>
              <!-- Ngưỡng tốc độ min–max nối vào nhãn zone -->
              <SettingRow :label="$t('lz.label.tmsMapLocation.showZoneSpeed')">
                <el-switch :model-value="props.zoneLabelDisplay.speed" @change="(val: any) => emit('toggle-zone-label', { key: 'speed', value: !!val })" />
              </SettingRow>
            </SettingsSection>

            <!-- Nhóm CHIỀU DI CHUYỂN (flow) -->
            <SettingsSection :title="$t('lz.label.tmsMapLocation.groupFlow')" icon="ele-Right" v-model:collapsed="collapsedConfig.flow">
              <SettingRow :label="$t('lz.label.tmsMapLocation.showFlowArrow')">
                <el-switch :model-value="props.zoneLabelDisplay.flowArrow" @change="(val: any) => emit('toggle-zone-label', { key: 'flowArrow', value: !!val })" />
              </SettingRow>
            </SettingsSection>

            <!-- Nhóm LÝ TRÌNH Km (định vị sự cố) -->
            <SettingsSection :title="$t('lz.label.tmsMapLocation.groupKmRoute')" icon="ele-Guide" v-model:collapsed="collapsedConfig.kmRoute">
              <SettingRow :label="$t('lz.label.tmsMapLocation.showKmArrow')">
                <el-switch :model-value="props.zoneLabelDisplay.kmArrow" @change="(val: any) => emit('toggle-zone-label', { key: 'kmArrow', value: !!val })" />
              </SettingRow>
              <SettingRow :label="$t('lz.label.tmsMapLocation.showArrowFrom')">
                <el-switch :model-value="props.zoneLabelDisplay.arrowFrom" @change="(val: any) => emit('toggle-zone-label', { key: 'arrowFrom', value: !!val })" />
              </SettingRow>
              <SettingRow :label="$t('lz.label.tmsMapLocation.showArrowTo')">
                <el-switch :model-value="props.zoneLabelDisplay.arrowTo" @change="(val: any) => emit('toggle-zone-label', { key: 'arrowTo', value: !!val })" />
              </SettingRow>
            </SettingsSection>
          </div>
        </el-tab-pane>
      </el-tabs>
    </el-tab-pane>

        <!-- Block Tab: xuất hiện cho cả Block và BlockVms -->
        <el-tab-pane v-if="props.mapMode === TmsTrafficInfoMap.Block || props.mapMode === TmsTrafficInfoMap.BlockVms" :label="$t('lz.label.tmsMapLocation.blockSetup')" name="block">
          <BlockEditor
            :config="props.blockConfig"
            :map="props.map"
            :geo-json-data="props.allGeoJsonData"
            @update:config="handleBlockConfigUpdate"
          />
        </el-tab-pane>

        <!-- GeoJSON Tab -->
        <el-tab-pane :label="$t('lz.label.tmsMapLocation.geoJson')" name="geojson">
          <div class="geojson-container">
            <div class="geojson-toolbar">
              <el-button size="small" type="primary" @click="handleExportGeoJson">
                <el-icon><ele-Download /></el-icon>
                <span>{{ $t('lz.button.tmsMapLocation.exportGeoJson') }}</span>
              </el-button>
              <el-button size="small" type="warning" @click="triggerImportGeoJson">
                <el-icon><ele-Upload /></el-icon>
                <span>{{ $t('lz.button.tmsMapLocation.importGeoJson') }}</span>
              </el-button>
              <input ref="geoJsonFileInput" type="file" accept=".json,.geojson,application/json"
                style="display:none" @change="handleImportGeoJsonFile" />
            </div>
            <!-- Tóm tắt nội dung file sẽ xuất: đối tượng trên bản đồ + block + cấu hình bản đồ -->
            <div class="geojson-summary">
              <span v-for="item in geoJsonSummary" :key="item.key" class="geojson-summary__item">
                <el-icon><component :is="item.icon" /></el-icon>
                {{ item.label }} <b>{{ item.value }}</b>
              </span>
              <span class="geojson-summary__item geojson-summary__item--config">
                <el-icon><ele-Setting /></el-icon>
                {{ $t('lz.label.tmsMapLocation.transferMapConfig') }}
              </span>
            </div>
            <div class="geojson-hint">
              <el-icon><ele-InfoFilled /></el-icon>
              <span>{{ $t('lz.label.tmsMapLocation.transferHint') }}</span>
            </div>
            <pre class="geojson-content">{{ JSON.stringify(geoJsonPreview, null, 2) }}</pre>
          </div>
        </el-tab-pane>

      </el-tabs>
    </div>

    <!-- Hộp thoại thêm/sửa nhãn văn bản trên bản đồ (đồng bộ giao diện với các modal khác) -->
    <el-dialog v-model="textDialog.visible" width="560px" append-to-body draggable destroy-on-close
      :close-on-click-modal="false" class="text-editor-dialog">
      <template #header>
        <div style="color: #fff">
          {{ textDialog.mode === 'add' ? $t('lz.label.tmsMapLocation.addText') : $t('lz.label.tmsMapLocation.editText') }}
        </div>
      </template>
      <el-form :model="textDialog.form" label-width="auto" label-position="left">
        <el-row :gutter="10">
          <el-col :span="24" class="mb20">
            <el-form-item :label="$t('lz.label.tmsMapLocation.textContent')">
              <el-input v-model="textDialog.form.text" type="textarea" :autosize="{ minRows: 2, maxRows: 4 }"
                :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.label.tmsMapLocation.textContent') })" />
            </el-form-item>
          </el-col>

          <el-col :xs="24" :sm="12" class="mb20">
            <el-form-item :label="$t('lz.label.tmsMapLocation.fontSize')">
              <el-input-number v-model="textDialog.form.fontSize" :min="6" :max="400" :step="1"
                controls-position="right" class="w100" />
            </el-form-item>
          </el-col>
          <el-col :xs="24" :sm="12" class="mb20">
            <el-form-item :label="$t('lz.label.tmsMapLocation.rotation')">
              <el-input-number v-model="textDialog.form.rotation" :min="0" :max="360" :step="5"
                controls-position="right" class="w100" />
            </el-form-item>
          </el-col>

          <!-- Bật để nhãn co giãn theo mức zoom; bắt buộc bật nếu muốn liên kết thành cụm -->
          <el-col :span="24" class="mb20">
            <el-form-item :label="$t('lz.label.tmsMapLocation.annotationScaleWithZoom')">
              <el-switch v-model="textDialog.form.scaleWithZoom" />
              <span class="text-muted" style="margin-left: 8px">{{ $t('lz.label.tmsMapLocation.annotationScaleWithZoomHint') }}</span>
            </el-form-item>
          </el-col>

          <!-- Bật: xoay bản đồ thì xoay theo; tắt: luôn giữ hướng trên màn hình -->
          <el-col :span="24" class="mb20">
            <el-form-item :label="$t('lz.label.tmsMapLocation.annotationRotateWithMap')">
              <el-switch v-model="textDialog.form.rotateWithMap" />
              <span class="text-muted" style="margin-left: 8px">{{ $t('lz.label.tmsMapLocation.annotationRotateWithMapHint') }}</span>
            </el-form-item>
          </el-col>

          <el-col :xs="24" :sm="12" class="mb20">
            <el-form-item :label="$t('lz.label.tmsMapLocation.textShape')">
              <el-select v-model="textDialog.form.shape" class="w100">
                <el-option :label="$t('lz.label.tmsMapLocation.shapeNone')" value="none" />
                <el-option :label="$t('lz.label.tmsMapLocation.shapeRect')" value="rect" />
                <el-option :label="$t('lz.label.tmsMapLocation.shapePill')" value="pill" />
                <el-option :label="$t('lz.label.tmsMapLocation.shapeOutline')" value="outline" />
                <el-option :label="$t('lz.label.tmsMapLocation.shapeUnderline')" value="underline" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :xs="24" :sm="12" class="mb20">
            <el-form-item :label="$t('lz.label.tmsMapLocation.bold')">
              <el-switch v-model="textDialog.form.bold" />
            </el-form-item>
          </el-col>

          <el-col :xs="24" :sm="12" class="mb20">
            <el-form-item :label="$t('lz.label.tmsMapLocation.textColor')">
              <el-color-picker v-model="textDialog.form.textColor" />
            </el-form-item>
          </el-col>
          <el-col :xs="24" :sm="12" class="mb20">
            <el-form-item :label="$t('lz.label.tmsMapLocation.bgColor')">
              <el-color-picker v-model="textDialog.form.bgColor" show-alpha :predefine="bgColorPresets" />
            </el-form-item>
          </el-col>

          <el-col :span="24">
            <el-form-item :label="$t('lz.label.base.preview')">
              <div class="text-editor-dialog__preview">
                <span class="text-editor-dialog__preview-chip"
                  :class="[`map-text-shape--${textDialog.form.shape}`, { 'has-bg': !!textDialog.form.bgColor }]"
                  :style="previewStyle">{{ textDialog.form.text || $t('lz.label.tmsMapLocation.textContent') }}</span>
              </div>
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
      <template #footer>
        <el-button @click="textDialog.visible = false">{{ $t('lz.button.base.cancel') }}</el-button>
        <el-button type="primary" @click="confirmTextDialog">{{ $t('lz.button.base.confirm') }}</el-button>
      </template>
    </el-dialog>

    <!-- Hộp thoại chèn/sửa hình ảnh trên bản đồ -->
    <el-dialog v-model="imageDialog.visible" width="560px" append-to-body draggable destroy-on-close
      :close-on-click-modal="false" class="text-editor-dialog">
      <template #header>
        <div style="color: #fff">
          {{ imageDialog.mode === 'add' ? $t('lz.label.tmsMapLocation.addImage') : $t('lz.label.tmsMapLocation.editImage') }}
        </div>
      </template>
      <el-form :model="imageDialog.form" label-width="auto" label-position="left">
        <el-row :gutter="10">
          <el-col :span="24" class="mb20">
            <el-form-item :label="$t('lz.label.tmsMapLocation.imageFile')">
              <!-- Ảnh được đọc thành base64 và lưu trong geojson, không upload lên file server -->
              <el-upload ref="imageUploadRef" class="image-editor-dialog__upload" :show-file-list="false"
                :auto-upload="false" accept=".jpg,.jpeg,.png,.svg,.gif" action="" :limit="1"
                :on-change="handleImageFileChange">
                <div v-if="imageDialog.form.imageUrl" class="image-editor-dialog__preview">
                  <img :src="imageDialog.form.imageUrl" alt="" />
                </div>
                <div v-else class="image-editor-dialog__placeholder">
                  <el-icon><ele-Plus /></el-icon>
                  <span>{{ $t('lz.label.tmsMapLocation.selectImage') }}</span>
                </div>
              </el-upload>
            </el-form-item>
          </el-col>

          <el-col :xs="24" :sm="12" class="mb20">
            <el-form-item :label="$t('lz.entity.base.name')">
              <el-input v-model="imageDialog.form.name" clearable
                :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.name') })" />
            </el-form-item>
          </el-col>
          <el-col :xs="24" :sm="12" class="mb20">
            <el-form-item :label="$t('lz.label.tmsMapLocation.imageWidth')">
              <el-input-number v-model="imageDialog.form.width" :min="8" :max="4000" :step="10"
                controls-position="right" class="w100" />
            </el-form-item>
          </el-col>

          <!-- Chiều cao: 0 = tự động theo tỉ lệ ảnh gốc (kéo tay cầm cạnh dưới sẽ tự đặt giá trị) -->
          <el-col :xs="24" :sm="12" class="mb20">
            <el-form-item :label="$t('lz.label.tmsMapLocation.imageHeight')">
              <el-input-number v-model="imageDialog.form.height" :min="0" :max="4000" :step="10"
                controls-position="right" class="w100" />
            </el-form-item>
          </el-col>

          <el-col :xs="24" :sm="12" class="mb20">
            <el-form-item :label="$t('lz.label.tmsMapLocation.rotation')">
              <el-input-number v-model="imageDialog.form.rotation" :min="0" :max="360" :step="5"
                controls-position="right" class="w100" />
            </el-form-item>
          </el-col>
          <el-col :xs="24" :sm="12" class="mb20">
            <el-form-item :label="$t('lz.label.tmsMapLocation.imageBorderRadius')">
              <el-input-number v-model="imageDialog.form.borderRadius" :min="0" :max="200" :step="2"
                controls-position="right" class="w100" />
            </el-form-item>
          </el-col>

          <!-- Bật để ảnh co giãn theo mức zoom; bắt buộc bật nếu muốn liên kết thành cụm -->
          <el-col :span="24" class="mb20">
            <el-form-item :label="$t('lz.label.tmsMapLocation.annotationScaleWithZoom')">
              <el-switch v-model="imageDialog.form.scaleWithZoom" />
              <span class="text-muted" style="margin-left: 8px">{{ $t('lz.label.tmsMapLocation.annotationScaleWithZoomHint') }}</span>
            </el-form-item>
          </el-col>

          <!-- Bật: xoay bản đồ thì xoay theo; tắt: luôn giữ hướng trên màn hình -->
          <el-col :span="24" class="mb20">
            <el-form-item :label="$t('lz.label.tmsMapLocation.annotationRotateWithMap')">
              <el-switch v-model="imageDialog.form.rotateWithMap" />
              <span class="text-muted" style="margin-left: 8px">{{ $t('lz.label.tmsMapLocation.annotationRotateWithMapHint') }}</span>
            </el-form-item>
          </el-col>

          <el-col :span="24">
            <el-form-item :label="$t('lz.label.tmsMapLocation.imageOpacity')">
              <el-slider v-model="imageDialog.form.opacity" :min="0.1" :max="1" :step="0.05" show-input />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
      <template #footer>
        <span class="image-editor-dialog__footer">
          <el-button v-if="imageDialog.mode === 'edit'" type="danger" plain @click="deleteEditingImage">
            {{ $t('lz.button.base.delete') }}
          </el-button>
          <span>
            <el-button @click="imageDialog.visible = false">{{ $t('lz.button.base.cancel') }}</el-button>
            <el-button type="primary" @click="confirmImageDialog">{{ $t('lz.button.base.confirm') }}</el-button>
          </span>
        </span>
      </template>
    </el-dialog>

    <!-- Hộp thoại thêm/sửa khối HTML (chữ + hình tự do) soạn bằng Jodit -->
    <el-dialog v-model="htmlDialog.visible" width="860px" append-to-body draggable destroy-on-close
      :close-on-click-modal="false" class="text-editor-dialog">
      <template #header>
        <div style="color: #fff">
          {{ htmlDialog.mode === 'add' ? $t('lz.label.tmsMapLocation.addHtml') : $t('lz.label.tmsMapLocation.editHtml') }}
        </div>
      </template>
      <el-form :model="htmlDialog.form" label-width="auto" label-position="left">
        <el-row :gutter="10">
          <el-col :span="24" class="mb20">
            <!-- Ảnh chèn trong khối được nhúng base64 nên lưu cùng geojson, không cần file server -->
            <JoditEditor
              v-if="htmlDialog.visible"
              :key="htmlDialog.editorKey"
              v-model:getHtml="htmlDialog.form.html"
              :buttons="htmlEditorButtons"
              :plugins="htmlEditorPlugins"
              :upload-image="readHtmlImageAsDataUrl"
              mode="simple"
              height="360px"
              resizable
              :placeholder="$t('lz.label.tmsMapLocation.htmlPlaceholder')"
            />
            <div class="text-muted html-editor-dialog__hint">{{ $t('lz.label.tmsMapLocation.htmlHint') }}</div>
          </el-col>

          <!-- Bề rộng khung: chữ tự xuống dòng theo bề rộng này (kéo tay cầm cạnh phải cũng đổi được) -->
          <el-col :xs="24" :sm="12" class="mb20">
            <el-form-item :label="$t('lz.label.tmsMapLocation.htmlWidth')">
              <el-input-number v-model="htmlDialog.form.width" :min="40" :max="4000" :step="10"
                controls-position="right" class="w100" />
            </el-form-item>
          </el-col>
          <el-col :xs="24" :sm="12" class="mb20">
            <el-form-item :label="$t('lz.label.tmsMapLocation.rotation')">
              <el-input-number v-model="htmlDialog.form.rotation" :min="0" :max="360" :step="5"
                controls-position="right" class="w100" />
            </el-form-item>
          </el-col>

          <el-col :span="24" class="mb20">
            <el-form-item :label="$t('lz.label.tmsMapLocation.annotationScaleWithZoom')">
              <el-switch v-model="htmlDialog.form.scaleWithZoom" />
              <span class="text-muted" style="margin-left: 8px">{{ $t('lz.label.tmsMapLocation.annotationScaleWithZoomHint') }}</span>
            </el-form-item>
          </el-col>

          <el-col :span="24">
            <el-form-item :label="$t('lz.label.tmsMapLocation.annotationRotateWithMap')">
              <el-switch v-model="htmlDialog.form.rotateWithMap" />
              <span class="text-muted" style="margin-left: 8px">{{ $t('lz.label.tmsMapLocation.annotationRotateWithMapHint') }}</span>
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
      <template #footer>
        <span class="image-editor-dialog__footer">
          <el-button v-if="htmlDialog.mode === 'edit'" type="danger" plain @click="deleteEditingHtml">
            {{ $t('lz.button.base.delete') }}
          </el-button>
          <span>
            <el-button @click="htmlDialog.visible = false">{{ $t('lz.button.base.cancel') }}</el-button>
            <el-button type="primary" @click="confirmHtmlDialog">{{ $t('lz.button.base.confirm') }}</el-button>
          </span>
        </span>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, reactive, onMounted, onUnmounted, watch, PropType, defineAsyncComponent } from 'vue'
import { Search, Delete, Edit, Position, Guide, Right, Setting, Aim, Rank, Switch as FlipIcon } from '@element-plus/icons-vue'
import { directionOfZone, ZONE_SPEED_MAX_KMH } from '/@/services/tms/zone-speed'
import type { ZoneSpeedValue } from '/@/stores/its/mapStore'
import { getAPI } from '/@/utils/axios-utils'
import { ElMessageBox, ElMessage, ElForm, TabsPaneContext, UploadInstance } from 'element-plus'
import L from 'leaflet'
import 'leaflet/dist/leaflet.css'
import {
  TmsEquipmentApi,
  TmsZoneApi,
  TmsWorkUnitApi
} from '/@/api-services/tms'
import { useThemeConfig } from '/@/stores/themeConfig'
import { useMapStore } from '/@/stores/its/mapStore'
import { UpdateTmsMapLocationInput } from '/@/api-services/tms';
import { useI18n } from 'vue-i18n'
import { watchDebounced } from '@vueuse/core'
import BlockEditor from './BlockEditor.vue'
import SettingsSection from './settings/SettingsSection.vue'
import SettingRow from './settings/SettingRow.vue'
import { getAnnotationHtmlSummary, hasAnnotationHtmlContent } from '../helpers/annotationHtml'
import { HTML_WIDTH_DEFAULT } from '../helpers/annotationUtils'
// Jodit khá nặng (toàn bộ plugin + CSS) → chỉ tải khi mở hộp thoại soạn khối HTML.
const JoditEditor = defineAsyncComponent(() => import('/@/components/joditEditor/index.vue'))

/** Nhóm annotation tự do (không gắn entity backend): text / image / html. */
const isAnnotationGroup = (group: any) => group === 'text' || group === 'image' || group === 'html'
import { INCIDENT_LINE_DEFAULT_STYLE, INCIDENT_ZONE_GAP_PX, resolveIncidentLineStyle, resolveIncidentZoneGap } from '../helpers/incidentPlacement'
import type { IncidentLineStyle } from '../helpers/incidentPlacement';
import { FLOW_LINE_ALL_KEY, FLOW_LINE_WIDTH_DEFAULT, FLOW_LINE_WIDTH_MAX, FLOW_LINE_WIDTH_MIN, ZONE_BORDER_WIDTH_DEFAULT, ZONE_BORDER_WIDTH_MAX, normalizeFlowLineColors, resolveFlowLineWidth, resolveZoneBorderWidth } from '../helpers/zoneDisplayStyle'
import type { FlowLineColors } from '../helpers/zoneDisplayStyle'
import { normalizeBlockConfiguration } from '../blockTypes'
import type { BlockConfiguration } from '../blockTypes'
import { buildGeoJsonTransfer, isSameCoordinateSystem, parseGeoJsonTransfer } from '../helpers/geoJsonTransfer'
import { locateBlockOnMap } from '../helpers/blockLocate'
import type { MapViewTransfer, ParsedGeoJsonTransfer } from '../helpers/geoJsonTransfer'


// Import 2 thư viện này mới vẽ được polygon trên zone
import '@geoman-io/leaflet-geoman-free';
import '@geoman-io/leaflet-geoman-free/dist/leaflet-geoman.css';

// Import transform helper
import { transformWorkUnitData, transformZonesData, transformEquipmentData } from '/@/views/bigScreen/mapSample/transformDataHelper'
 import {useBaseConfig} from '/@/stores/baseConfig'
import { BaseConfigTypeEnum, TmsTrafficInfoMap, TmsZoneKmDirection } from '/@/types/enums'
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;


const themeConfig = useThemeConfig()
const mapStore = useMapStore()

// ===== Setup & State =====
const { t: $t } = useI18n();
const ruleFormRef = ref<InstanceType<typeof ElForm>>();

const props = defineProps({
  /**
   * Thêm props cấu hình màu nền cho image map
   */
  currentMapType: {
      type: String,
      default: ''
    },
  mapBackgroundColor: {
      type: String,
      default: '#d3d3d3'
    },
  mapId: String,
  /**
   * Mốc đánh dấu config view đã nạp xong từ CSDL (LeafletMap tăng ở cuối loadMapData).
   * Mỗi lần giá trị này đổi ⇒ props lat/lng/zoom đang là dữ liệu DB của map hiện tại
   * ⇒ nạp lại cả 4 ô (hiện tại + điều chỉnh).
   */
  configVersion: {
    type: Number,
    default: 0
  },
  mapMode: {
    type: String,
    default: ''
  },
  blockConfig: {
    type: Object as PropType<BlockConfiguration>,
    default: () => ({ blocks: [] })
  },
  geoJsonData: {
    type: Object,
    required: true
  },
  allGeoJsonData: {
    type: Object,
    default: () => ({ type: "FeatureCollection", features: [] })
  },
  markersLayer: L.layerGroup() as L.LayerGroup<L.Marker>,
  polygonsLayer: L.layerGroup() as L.LayerGroup<L.Polygon>,
 maxZoom: {
    type: Number,
    default: 0
  },
  minZoom: {
    type: Number,
    default: 0
  },
  latitude: {
    type: Number,
    default: 0
  },
  longtitude: {
    type: Number,
    default: 0
  },
  defaultZoom: {
    type: Number,
    default: 0
  },
  rotation: {
    type: Number,
    default: 0
  },
  boundRadius: {
    type: Number,
    default: 0
  },
   scrollViewZoom: {
    type: Number,
    default: 0
  },
  zoneLabelDisplay: {
    type: Object as PropType<{ name: boolean; kmArrow: boolean; arrowFrom: boolean; arrowTo: boolean; flowArrow: boolean; speed: boolean }>,
    default: () => ({ name: true, kmArrow: true, arrowFrom: true, arrowTo: true, flowArrow: true, speed: true })
  },
  showIconBackground: {
    type: Boolean,
    default: true,
  },
  /** Khóa zoom trên bản đồ chính (lưu cùng config bản đồ). */
  lockZoom: {
    type: Boolean,
    default: false,
  },
  /** Khóa xoay bản đồ chính (lưu cùng config bản đồ). */
  lockRotation: {
    type: Boolean,
    default: false,
  },
  iconSize: {
    type: Number,
    default: 0,
  },
  incidentIconSize: {
    type: Number,
    default: 0,
  },
  incidentZoneGap: {
    type: Number,
    default: INCIDENT_ZONE_GAP_PX,
  },
  incidentLineStyle: {
    type: String,
    default: INCIDENT_LINE_DEFAULT_STYLE,
  },
  /** Độ dày viền zone ở màn giám sát (px). */
  zoneBorderWidth: {
    type: Number,
    default: ZONE_BORDER_WIDTH_DEFAULT,
  },
  /** Màu đường định hướng theo hướng (mã hướng → màu; '*' = mọi hướng). */
  flowLineColors: {
    type: Object as PropType<FlowLineColors>,
    default: () => ({}),
  },
  /** Độ dày đường định hướng (px). */
  flowLineWidth: {
    type: Number,
    default: FLOW_LINE_WIDTH_DEFAULT,
  },
  previousGeoJsonFeature:{
    type: Array,
    require: true
  },
  previousFeatureGeoJsonDataZone:{
    type: Array,
    require: true
  },
  map: null as L.Map | null
})

const emit = defineEmits([
  //   'update-location',
  //   'update-polygon-location',
  'locate-on-map',
  'start-editing',
  'start-moving',
  'confirm-delete',
  'add-item',
  'render-map',
  'handle-save',
  'handle-start-drawing-polygon',
  'mark-unsaved',
  'update-map-background-color',
  'toggle-zone-label',
  'toggle-icon-background',
  'toggle-lock-zoom',
  'toggle-lock-rotation',
  'update-icon-flip',
  'update-icon-size',
  'update-incident-icon-size',
  'update-incident-zone-gap',
  'update-incident-line-style',
  'update-zone-border-width',
  'update-flow-line-colors',
  'update-flow-line-width',
  'update-text',
  'update-image',
  'update-html',
  'update-block-config',
  'import-geojson'
])


const state = reactive({
queryParams: {
    itemGroup: undefined as string | undefined,
    itemId: undefined as string | undefined | null,
    mapId: undefined,
    maxZoom: 0 as number | undefined,
    minZoom: 0 as number | undefined,
    latitude: 0 as number | undefined,
    longtitude: 0 as number | undefined,
    defaultZoom: 0 as number | undefined,
    rotation: 0 as number | undefined,
    boundRadius: 0 as number | undefined,
    backgroundColor: '#d3d3d3'
  },
  itemGroup: [] as any,
  itemList: [] as any,
  equipmentTypeList: [] as any,
  equipmentList: [] as any,
  zoneList: [] as any,
  zoneTypeList: [] as any,
  workUnitTypeList: [] as any,
  workUnitList: [] as any,
  unsavedChanges: new Set<string>(),
  tileLayer: null as L.TileLayer | null,
  imageOverlay: null as L.ImageOverlay | null,
  currentMode: null as 'edit' | 'move' | null,
  highlightLayer: null as L.Layer | null,
  activeFeatureId: null as string | null,
  editingLayer: null as any,
  originalCoordinates: null as any,
  currentCoordinates: null as any,
  markersLayer: props.markersLayer,
  polygonsLayer: props.polygonsLayer,
  ruleForm: {} as UpdateTmsMapLocationInput,
  activeTabItemGroup: '' as any,
  listEquipmentType: [] as any,
  //inputSearchDataTable: '' as string,
  listDataTable: [] as any,
  defaultValue: {
    latitude: 0,
    longtitude: 0,
    minZoom:0,
    maxZoom:0,
    defaultZoom:0,
    rotation: 0,
    boundRadius: 0
  }
  //listActiveTabItemGroup: [] as any
})

/**
 * Đồng bộ config view từ props xuống state khi map thay đổi (pan/zoom/rotate).
 *
 * Nguyên tắc:
 * - Khi load map từ CSDL (mapId thay đổi): cả 4 field (2 "hiện tại" + 2 "điều chỉnh")
 *   phải load đúng giá trị từ DB.
 * - Khi pan/edit trên map (cùng mapId): CHỈ cập nhật 2 ô "điều chỉnh" (queryParams).
 * - Bấm nút ✓ (xác nhận): copy queryParams → defaultValue.
 * - Bấm Lưu: ghi defaultValue → CSDL.
 * - Load lại: cả 4 field reload từ CSDL.
 *
 * Cơ chế: dựa vào `props.configVersion` — mốc do LeafletMap tăng ở CUỐI loadMapData,
 * tức thời điểm mapViewState chắc chắn đã mang dữ liệu DB của map hiện tại.
 * KHÔNG dùng mapId để nhận biết, vì mapId đổi trước khi loadMapData chạy xong nên props
 * lat/lng lúc đó vẫn là của map trước đó.
 */
let lastSyncedConfigVersion = -1;

const syncMapViewConfig = () => {
  const viewConfig = {
    latitude: Number(props.latitude),
    longtitude: Number(props.longtitude),
    maxZoom: Number(props.maxZoom),
    minZoom: Number(props.minZoom),
    defaultZoom: Number(props.defaultZoom),
    rotation: Number(props.rotation),
    boundRadius: Number(props.boundRadius),
  }

  // configVersion đổi ⇒ vừa nạp xong config từ CSDL của map hiện tại
  const isDbConfigLoaded = props.configVersion !== lastSyncedConfigVersion;

  // Ô "điều chỉnh" luôn phản ánh trạng thái map thực tế (pan/zoom/rotate)
  state.queryParams.latitude = viewConfig.latitude
  state.queryParams.longtitude = viewConfig.longtitude
  state.queryParams.maxZoom = viewConfig.maxZoom
  state.queryParams.minZoom = viewConfig.minZoom
  state.queryParams.defaultZoom = viewConfig.defaultZoom
  state.queryParams.rotation = viewConfig.rotation
  state.queryParams.boundRadius = viewConfig.boundRadius

  // Ô "hiện tại" chỉ nạp lại khi có config mới từ CSDL.
  // Pan/zoom sau đó KHÔNG tác động — chờ user bấm ✓ (handleSetConfig).
  if (isDbConfigLoaded) {
    lastSyncedConfigVersion = props.configVersion;

    state.defaultValue.latitude = viewConfig.latitude
    state.defaultValue.longtitude = viewConfig.longtitude
    state.defaultValue.maxZoom = viewConfig.maxZoom
    state.defaultValue.minZoom = viewConfig.minZoom
    state.defaultValue.defaultZoom = viewConfig.defaultZoom
    state.defaultValue.rotation = viewConfig.rotation
    state.defaultValue.boundRadius = viewConfig.boundRadius
  }
}

// assign ref to detect
const isPanelCollapsed = ref(false)

// ===== Panel Height (resize + nút thao tác nhanh) =====
const DEFAULT_PANEL_HEIGHT = '50%'
const panelRef = ref<HTMLElement | null>(null)
const panelHeight = ref<string>(DEFAULT_PANEL_HEIGHT)

// Đặt nhanh chiều cao panel (½ / full / reset)
const setPanelHeight = (h: string) => {
  panelHeight.value = h
}

// Kéo cạnh trên của panel để chỉnh chiều cao
let isResizingPanel = false
let resizeStartY = 0
let resizeStartHeight = 0

const onResizingPanel = (e: MouseEvent) => {
  if (!isResizingPanel || !panelRef.value) return
  const parentH = panelRef.value.parentElement?.clientHeight || window.innerHeight
  // Kéo LÊN (clientY giảm) => panel cao hơn (panel neo ở đáy)
  let newH = resizeStartHeight + (resizeStartY - e.clientY)
  const minH = 160
  const maxH = parentH
  newH = Math.max(minH, Math.min(newH, maxH))
  panelHeight.value = newH + 'px'
}

const stopResizePanel = () => {
  if (!isResizingPanel) return
  isResizingPanel = false
  document.body.style.userSelect = ''
  document.body.style.cursor = ''
  document.removeEventListener('mousemove', onResizingPanel)
  document.removeEventListener('mouseup', stopResizePanel)
}

const startResize = (e: MouseEvent) => {
  if (!panelRef.value) return
  isResizingPanel = true
  resizeStartY = e.clientY
  resizeStartHeight = panelRef.value.offsetHeight
  document.body.style.userSelect = 'none'
  document.body.style.cursor = 'ns-resize'
  document.addEventListener('mousemove', onResizingPanel)
  document.addEventListener('mouseup', stopResizePanel)
}

onUnmounted(() => {
  // Dọn listener nếu component bị hủy giữa lúc kéo
  document.removeEventListener('mousemove', onResizingPanel)
  document.removeEventListener('mouseup', stopResizePanel)
})

const activeTab = ref('table')
const activeName = ref('device')
const tableSearch = ref('')
const inputSearchDataTable = ref('')

/** Binding hai chiều cho input kích thước icon; trống/0 sẽ dùng size của từng loại thiết bị. */
const mapIconSize = computed<number | undefined>({
  get: () => (Number(props.iconSize) > 0 ? Number(props.iconSize) : undefined),
  set: (value) => emit('update-icon-size', Number(value) > 0 ? Number(value) : 0),
})

/** Binding hai chiều cho input kích thước chung icon sự kiện sự cố; trống/0 sẽ dùng size của từng loại sự kiện. */
const mapIncidentIconSize = computed<number | undefined>({
  get: () => (Number(props.incidentIconSize) > 0 ? Number(props.incidentIconSize) : undefined),
  set: (value) => emit('update-incident-icon-size', Number(value) > 0 ? Number(value) : 0),
})

/** Khoảng hở icon sự kiện: 0px = sát biên, bỏ trống = mặc định 2px. */
const mapIncidentZoneGap = computed<number>({
  get: () => resolveIncidentZoneGap(props.incidentZoneGap),
  set: (value) => emit('update-incident-zone-gap', resolveIncidentZoneGap(value)),
})

/** Kiểu nét đường nối icon sự kiện → vị trí sự kiện; mặc định nét liền. */
const mapIncidentLineStyle = computed<IncidentLineStyle>({
  get: () => resolveIncidentLineStyle(props.incidentLineStyle),
  set: (value) => emit('update-incident-line-style', resolveIncidentLineStyle(value)),
})

/** Độ dày viền zone (0 = ẩn viền, trống = mặc định 1.5px). */
const mapZoneBorderWidth = computed<number>({
  get: () => resolveZoneBorderWidth(props.zoneBorderWidth),
  set: (value) => emit('update-zone-border-width', resolveZoneBorderWidth(value)),
})

/** Độ dày đường định hướng (1–6px, trống = mặc định 2px). */
const mapFlowLineWidth = computed<number>({
  get: () => resolveFlowLineWidth(props.flowLineWidth),
  set: (value) => emit('update-flow-line-width', resolveFlowLineWidth(value)),
})

/** Bảng màu gợi ý cho đường định hướng (nổi trên nền zone xanh/vàng/đỏ). */
const flowLineColorPresets = ['#ffffff', '#0f172a', '#2563eb', '#7c3aed', '#db2777', '#0891b2', '#f59e0b', '#e11d48']

/**
 * Các dòng chọn màu: "Mọi hướng" + từng HƯỚNG có zone trên bản đồ này
 * (hướng = zone.laneId, tên lấy từ danh mục TmsDirectionType; không có tên thì hiện mã).
 * Hướng đã có màu nhưng không còn zone nào vẫn được liệt kê để người dùng xoá được.
 */
const flowLineColorRows = computed(() => {
  const zoneCountByLane = new Map<string, number>()
  getFullFeatureList().forEach((feature: any) => {
    if (feature?.geometry?.type !== 'Polygon') return
    const zone = mapStore.zoneList.find((z: any) => String(z?.id) === String(feature.properties?.id))
    if (zone?.laneId == null || zone.laneId === '') return
    const key = String(zone.laneId)
    zoneCountByLane.set(key, (zoneCountByLane.get(key) || 0) + 1)
  })
  Object.keys(props.flowLineColors || {}).forEach((key) => {
    if (key !== FLOW_LINE_ALL_KEY && !zoneCountByLane.has(key)) zoneCountByLane.set(key, 0)
  })

  const directionList: any[] = useBaseConfig().getConfigDataByCode(BaseConfigTypeEnum.TmsDirectionType) || []
  const laneRows = [...zoneCountByLane.entries()].map(([key, zoneCount]) => ({
    key,
    name: directionList.find((item: any) => String(item?.code) === key)?.name || key,
    zoneCount,
  }))
  laneRows.sort((a, b) => a.name.localeCompare(b.name))
  return [{ key: FLOW_LINE_ALL_KEY, name: $t('lz.label.tmsMapLocation.flowLineAllDirections'), zoneCount: 0 }, ...laneRows]
})

/** Đổi màu 1 hướng; xoá màu (null) = bỏ khoá đó → dùng màu "Mọi hướng" / mặc định. */
const handleFlowLineColorChange = (key: string, value: string | null) => {
  const next: FlowLineColors = { ...normalizeFlowLineColors(props.flowLineColors) }
  if (value) next[key] = value
  else delete next[key]
  emit('update-flow-line-colors', next)
}

// const ruleFormRef = ref<InstanceType<typeof ElForm>>();
watch(() => props.markersLayer, (newVal) => {
  state.markersLayer = newVal
})

watch(() => props.polygonsLayer, (newVal) => {
  state.polygonsLayer = newVal
})

watch(
  () => [
    props.mapId,
    // Bắt buộc có: đây là mốc nhận biết config DB đã nạp xong cho map hiện tại
    props.configVersion,
    props.latitude,
    props.longtitude,
    props.maxZoom,
    props.minZoom,
    props.defaultZoom,
    props.rotation,
    props.boundRadius,
  ],
  syncMapViewConfig,
  { immediate: true, flush: 'post' }
)

watch(() => props.scrollViewZoom, (newVal) => {
  const nextDefaultZoom = newVal.defaultZoom as number;
  const queryMinZoom = state.queryParams.minZoom as number;
  const queryMaxZoom = state.queryParams.maxZoom as number;

  if (nextDefaultZoom < queryMinZoom || nextDefaultZoom > queryMaxZoom) {
      ElMessage.warning($t('lz.message.tmsMapLocation.defaultZoomOutOfRange'));
      const fallbackZoom = state.defaultValue.defaultZoom;
      state.queryParams.defaultZoom = fallbackZoom;
      return;
  }

  state.queryParams.defaultZoom = newVal as number
})

/**
 * Watch theo dõi thay đổi màu nền
 */
watch(
  () => props.mapBackgroundColor,
  (newVal) => {
    if (newVal) {
      state.queryParams.backgroundColor = newVal;
    }
  },
  { immediate: true }
);

onMounted(async () => {
  //console.log("Location Edit - onMounted")
  await loadData();

  // Handle item group change
  handleItemGroupChange();

  // active tab device first
  state.activeTabItemGroup ='device' 
 //console.log(" state.queryParams", state.queryParams)
})

async function loadData() {
  // Fetch other data after map is loadeds
  await mapStore.fetchItemGroupList();
  // Clone để không mutate list trong store khi chèn nhóm 'text' (annotation client-side)
  state.itemGroup = [...(mapStore.itemGroupList || [])];

  // Get list code equipment type (sorted by name)
  if(state.itemGroup && state.itemGroup.length > 0){
    state.listEquipmentType = state.itemGroup.filter((item: any) => {
      if(item.code && !Number(item.code)){
        return {
          equipmentTypeName: item?.name,
          equipmentTypeCode: item?.code
        }
      }
    })
  }

  // Chèn nhóm 'text' (nhãn văn bản tự do) và 'image' (hình ảnh tự do) vào dropdown chọn
  // nhóm — SAU khi tính listEquipmentType để không tạo tab bảng thừa. Đây là các nhóm
  // annotation client-side (không lấy từ config backend).
  if (!state.itemGroup.some((g: any) => g.code === 'text')) {
    state.itemGroup.push({ code: 'text', name: $t('lz.label.tmsMapLocation.textItemGroup') });
  }
  if (!state.itemGroup.some((g: any) => g.code === 'image')) {
    state.itemGroup.push({ code: 'image', name: $t('lz.label.tmsMapLocation.imageItemGroup') });
  }
  if (!state.itemGroup.some((g: any) => g.code === 'html')) {
    state.itemGroup.push({ code: 'html', name: $t('lz.label.tmsMapLocation.htmlItemGroup') });
  }

  await mapStore.fetchEquipmentTypeList();
  state.equipmentTypeList = mapStore.equipmentTypeList;

  await mapStore.fetchZoneTypeList();
  state.zoneTypeList = mapStore.zoneTypeList;

  await mapStore.fetchWorkUnitTypeList();
  state.workUnitTypeList = mapStore.workUnitTypeList;
}

/**
 * handleSaveConfig
 */
const handleSetConfig = (event:MouseEvent,  field:'latitude'|'longtitude'|'maxZoom'|'minZoom'|'defaultZoom'|'rotation'|'boundRadius') => {
    switch(field){
      case 'latitude':
        state.defaultValue.latitude = state.queryParams.latitude as number
        return;
      case 'longtitude':
        state.defaultValue.longtitude = state.queryParams.longtitude as number
        return;
      case 'maxZoom':
        const maxZoom = Number(state.queryParams.maxZoom ?? 0)
        state.defaultValue.maxZoom = maxZoom

        // Nếu defaultZoom đang lớn hơn maxZoom thì ép xuống
        if (Number(state.queryParams.defaultZoom ?? 0) > maxZoom) {
          state.queryParams.defaultZoom = maxZoom
          state.defaultValue.defaultZoom = maxZoom
        }
        return;
      case 'minZoom':
        const minZoom = Number(state.queryParams.minZoom ?? 0)
        state.defaultValue.minZoom = minZoom

        // Nếu defaultZoom nhỏ hơn minZoom thì ép lên
        if (Number(state.queryParams.defaultZoom ?? 0) < minZoom) {
          state.queryParams.defaultZoom = minZoom
          state.defaultValue.defaultZoom = minZoom
        }
        return;
      case 'defaultZoom':
          let defaultZoom = Number(state.queryParams.defaultZoom ?? 0)
          const minZoomConfig = Number(state.queryParams.minZoom ?? 0)
          const maxZoomConfig = Number(state.queryParams.maxZoom ?? 0)

          // Kẹp defaultZoom trong khoảng [minZoom, maxZoom]
          if (defaultZoom < minZoomConfig) defaultZoom = minZoomConfig
          if (maxZoomConfig > 0 && defaultZoom > maxZoomConfig) defaultZoom = maxZoomConfig

          state.queryParams.defaultZoom = defaultZoom
          state.defaultValue.defaultZoom = defaultZoom
        return;
      case 'rotation':
          let rotation = Number(state.queryParams.rotation ?? 0)
          // Kẹp rotation trong khoảng [0, 360]
          if (rotation < 0) rotation = 0
          if (rotation > 360) rotation = 360

          state.queryParams.rotation = rotation
          state.defaultValue.rotation = rotation
          // Áp dụng rotation lên map ngay
          if (props.map && typeof (props.map as any).setBearing === 'function') {
            (props.map as any).setBearing(rotation);
          }
        return;
      case 'boundRadius':
          let boundRadius = Number(state.queryParams.boundRadius ?? 0)
          if (boundRadius < 0) boundRadius = 0
          state.queryParams.boundRadius = boundRadius
          state.defaultValue.boundRadius = boundRadius
          // Hiển thị preview vùng giới hạn trên map (nếu có)
          drawBoundPreview(boundRadius)
        return;
    }
  
}

/**
 * Cập nhật bán kính giới hạn khi user gõ tay
 */
const handleBoundRadiusUpdate = (val: any) => {
  if (val === undefined || val === null) return;
  state.queryParams.boundRadius = Number(val);
  drawBoundPreview(Number(val));
}

/**
 * Tính bán kính giới hạn theo khung màn hình hiện tại.
 * Lấy khoảng cách từ tâm map đến góc viewport (đơn vị mét).
 * Cách này bất biến với góc xoay vì chỉ dựa trên khoảng cách.
 */
const handleCaptureBoundFromView = () => {
  if (!props.map) return;
  const center = props.map.getCenter();
  const bounds = props.map.getBounds();
  const corner = bounds.getNorthEast();
  const radius = Math.round(center.distanceTo(corner));
  state.queryParams.boundRadius = radius;
  // Chưa apply vào defaultValue — user có thể chỉnh lại rồi bấm nút Apply
  drawBoundPreview(radius);
  ElMessage.info($t('lz.label.tmsMapLocation.boundRadius') + ': ' + radius + ' m');
}

/**
 * Vẽ vòng tròn preview vùng giới hạn quanh tâm mặc định trên map cấu hình.
 */
let boundPreviewCircle: L.Circle | null = null;
const drawBoundPreview = (radius: number) => {
  if (!props.map) return;
  if (boundPreviewCircle) {
    props.map.removeLayer(boundPreviewCircle);
    boundPreviewCircle = null;
  }
  if (!radius || radius <= 0) return;
  const center: [number, number] = [
    state.queryParams.latitude ?? props.map.getCenter().lat,
    state.queryParams.longtitude ?? props.map.getCenter().lng
  ];
  boundPreviewCircle = L.circle(center, {
    radius,
    color: 'var(--el-color-warning)',
    weight: 2,
    fillOpacity: 0.05,
    dashArray: '6 6'
  }).addTo(props.map);
}

/**
 * Hàm đổi màu
 */
const handleBackgroundColorChange = (color: string) => {
  state.queryParams.backgroundColor = color;
  emit('update-map-background-color', color);
};

/**
 * handleHeaderAccordionClick
 */
const collapsedAccordion = ref<Record<string, boolean>>({})

/** Thu gọn từng nhóm trong tab Cấu hình (key trùng với SettingsSection tương ứng). */
const collapsedConfig = reactive<Record<string, boolean>>({
  // Tab "Khung nhìn"
  center: false,
  zoom: false,
  rotation: false,
  boundRadius: false,
  // Tab "Hiển thị"
  deviceIcon: false,
  incident: false,
  zoneRoute: false,
  background: false,
  // Tab "Chỉ trang thiết lập"
  zoneLabel: false,
  flow: false,
  kmRoute: false,
})

/**
 * Tab con trong tab Cấu hình:
 * - view (Khung nhìn) và shared (Hiển thị): lưu cùng bản đồ, áp dụng mọi màn hình.
 * - local: chỉ hỗ trợ thao tác trên trang thiết lập, không lưu.
 */
const activeConfigTab = ref<'view' | 'shared' | 'local'>('view')

const incidentLineStyleOptions = computed(() => [
  { label: $t('lz.label.tmsMapLocation.lineStyleSolid'), value: 'solid' },
  { label: $t('lz.label.tmsMapLocation.lineStyleDashed'), value: 'dashed' },
  { label: $t('lz.label.tmsMapLocation.lineStyleDotted'), value: 'dotted' },
])

type ViewField = 'latitude' | 'longtitude' | 'defaultZoom' | 'maxZoom' | 'minZoom' | 'rotation' | 'boundRadius'
type ViewFieldItem = { field: ViewField; label: string; precision: number; step: number; min?: number; max?: number; placeholder?: string }

/**
 * Các nhóm của tab "Khung nhìn". Mỗi giá trị hiển thị: hiện tại (chỉ đọc) → giá trị mới
 * (xem trước trên bản đồ) → nút ✓ áp dụng (handleSetConfig) — giữ nguyên logic cũ.
 */
const viewFieldGroups = computed<{ key: string; title: string; icon: string; fields: ViewFieldItem[] }[]>(() => [
  {
    key: 'center',
    title: $t('lz.label.tmsMapLocation.mapCenterGroup'),
    icon: 'ele-Location',
    fields: [
      // Vĩ độ cho phép âm (bản đồ ảnh CRS.Simple có thể đặt tâm dưới mép ảnh)
      { field: 'latitude', label: $t('lz.label.tmsMapLocation.latitude'), precision: 6, step: 0.0001 },
      { field: 'longtitude', label: $t('lz.label.tmsMapLocation.longitude'), precision: 6, step: 0.0001, min: 0 },
    ],
  },
  {
    key: 'zoom',
    title: $t('lz.label.tmsMapLocation.zoomGroup'),
    icon: 'ele-ZoomIn',
    fields: [
      { field: 'defaultZoom', label: $t('lz.label.tmsMapLocation.defaultZoom'), precision: 1, step: 0.2, min: -2 },
      { field: 'maxZoom', label: $t('lz.label.tmsMapLocation.maxZoom'), precision: 1, step: 0.2, min: -2 },
      { field: 'minZoom', label: $t('lz.label.tmsMapLocation.minZoom'), precision: 1, step: 0.2, min: -3 },
    ],
  },
  {
    key: 'rotation',
    title: $t('lz.label.tmsMapLocation.editRotation'),
    icon: 'ele-RefreshRight',
    fields: [{ field: 'rotation', label: $t('lz.label.tmsMapLocation.rotation'), precision: 1, step: 1, min: 0, max: 360, placeholder: '0-360' }],
  },
  {
    key: 'boundRadius',
    title: $t('lz.label.tmsMapLocation.editBoundRadius'),
    icon: 'ele-Aim',
    fields: [{ field: 'boundRadius', label: $t('lz.label.tmsMapLocation.boundRadius'), precision: 0, step: 100, min: 0 }],
  },
])

/** Giá trị "Hiện tại" dạng chữ theo đúng số chữ số thập phân của ô nhập. */
const formatViewValue = (value: any, precision: number) => {
  const numberValue = Number(value)
  return Number.isFinite(numberValue) ? numberValue.toFixed(precision) : '—'
}

/** Giá trị mới khác giá trị hiện tại → tô nổi nút ✓ để nhắc bấm áp dụng. */
const isViewFieldChanged = (item: ViewFieldItem) => {
  const current = Number(state.defaultValue[item.field])
  const next = Number(state.queryParams[item.field])
  if (!Number.isFinite(next)) return false
  return formatViewValue(current, item.precision) !== formatViewValue(next, item.precision)
}

/** Xem trước giá trị mới trên bản đồ (bán kính giới hạn có xử lý riêng). */
const handleViewFieldChange = (field: ViewField, val: any) => {
  if (field === 'boundRadius') handleBoundRadiusUpdate(val)
  else handleLocationConfigUpdate(val, field)
}

const handleHeaderAccordionClick = (
  event: MouseEvent,
  equipmentType: string
) => {
  collapsedAccordion.value[equipmentType] = !collapsedAccordion.value[equipmentType]

  const panelAccordionAll = document.querySelectorAll(`.filter_${equipmentType}`)

  panelAccordionAll.forEach((el) => {
    el.classList.toggle('d-none-accordion')
  })
}

/**
 * Handle click tab
 */
 const handleClickTabAccordion = (tab: TabsPaneContext, event: Event) => {
  //console.log(tab, event)
  const props = {...tab.props}
  //console.log("props", props)
  state.activeTabItemGroup = props?.name
}


/**
 * Handle zoom
 */
const handleLocationConfigUpdate = (
  val: any,
  field: 'maxZoom' | 'minZoom' | 'defaultZoom' | 'latitude' | 'longtitude' | 'rotation'
) => {
  if (!props.map) return;
  if (val === undefined || val === null) return;

  if (field === 'latitude') {
    state.queryParams.latitude = val;
    props.map.setView(
      [state.queryParams.latitude ?? 0, state.queryParams.longtitude ?? 0],
      state.queryParams.defaultZoom
    );
    return;
  }

  if (field === 'longtitude') {
    state.queryParams.longtitude = val;
    props.map.setView(
      [state.queryParams.latitude ?? 0, state.queryParams.longtitude ?? 0],
      state.queryParams.defaultZoom
    );
    return;
  }

  if (field === 'minZoom') {
    state.queryParams.minZoom = val;
    console.log("state.queryParams.minZoom",  state.queryParams.minZoom)
    props.map.setMinZoom(val);
  } else if (field === 'maxZoom') {
    state.queryParams.maxZoom = val;
    props.map.setMaxZoom(val);
  } else if (field === 'defaultZoom') {
    state.queryParams.defaultZoom = val;
  } else if (field === 'rotation') {
    state.queryParams.rotation = val;
    if (typeof (props.map as any).setBearing === 'function') {
      (props.map as any).setBearing(val);
    }
    return;
  }

  props.map.setView(
    [state.queryParams.latitude ?? 0, state.queryParams.longtitude ?? 0],
    val
  );
};

// ===== Equipment Type Management =====
/**
 * Handles changes to equipment type selection and loads corresponding data
 */
const handleItemGroupChange = () => {
  //console.log("handleItemGroupChange")
  watch(() => state.queryParams.itemGroup, async (itemGroup: any) => {
    // reset item đã chọn cũ
    state.queryParams.itemId = undefined;
    state.itemList = [];

    //console.log("itemGroup", itemGroup)      
    if (!itemGroup) return;
    state.itemList = [];
    try {
      switch (itemGroup) {
        case 'device': {

          // Load equipment data — chỉ lấy thiết bị đang hoạt động (status = 1 = Enable), bỏ thiết bị "Không dùng"
          const equipmentResponse = await getAPI(TmsEquipmentApi).apiTmsTmsequipmentListGet(undefined, undefined, undefined, undefined, undefined, undefined, undefined, undefined, 1 as any);

          const equipmentData = equipmentResponse?.data?.result || [];
          //console.log("equipmentData", equipmentData)
          // Transform data into grouped structure
          state.itemList = transformEquipmentData(equipmentData, state.equipmentTypeList, isIdInGeoJson);
          //console.log(state.itemList)
          break;
        }
        case 'zone': {
          // Load zones data
          const zonesResponse = await getAPI(TmsZoneApi).apiTmsTmszoneListGet();
          const zonesData = (zonesResponse?.data?.result || []).filter((z: any) => z.status === 1);

          // Transform data into grouped structure
          state.itemList = await transformZonesData(zonesData, isIdInGeoJson);
          break;
        }
        case 'unit': {
          // Load work unit data — chỉ lấy đơn vị đang hoạt động (status = 1 = Enable), bỏ đơn vị "Không dùng"
          const workUnitResponse = await getAPI(TmsWorkUnitApi).apiTmsTmsworkunitListGet(undefined, undefined, undefined, undefined, 1 as any);
          const workUnitData = workUnitResponse?.data?.result || [];

          // Transform data into grouped structure
          state.itemList = await transformWorkUnitData(workUnitData, isIdInGeoJson);
          break;
        }
        default:
          console.warn(`Unhandled object type: ${itemGroup}`);
          state.itemList = [];
      }
    } catch (error) {
      console.error('Error fetching data:', error);
      state.itemList = [];
    }
  });
};


const handleBlockConfigUpdate = (config: BlockConfiguration) => {
  emit('update-block-config', config)
}

// ===== Form Submission =====
/**
 * Submits the form to create new map location
 */
/**
 * Giá trị khung nhìn đã nhập/kéo bản đồ nhưng CHƯA bấm ✓ → "Lưu bản đồ" chỉ ghi cột "Hiện tại"
 * nên trước đây các giá trị này bị bỏ qua mà không báo. Hỏi người dùng áp dụng luôn hay giữ giá trị cũ.
 * @returns false nếu người dùng đóng hộp thoại (huỷ lưu)
 */
const resolvePendingViewFields = async (): Promise<boolean> => {
  const pendingFields = viewFieldGroups.value.flatMap((group) => group.fields).filter((item) => isViewFieldChanged(item))
  if (!pendingFields.length) return true

  const rows = pendingFields
    .map((item) => `<li><strong>${item.label}:</strong> ${formatViewValue(state.defaultValue[item.field], item.precision)} → ${formatViewValue(state.queryParams[item.field], item.precision)}</li>`)
    .join('')
  try {
    await ElMessageBox.confirm(
      `<p>${$t('lz.message.tmsMapLocation.pendingViewValues')}</p><ul>${rows}</ul>`,
      $t('lz.message.base.warning'),
      {
        type: 'warning',
        dangerouslyUseHTMLString: true,
        distinguishCancelAndClose: true,
        confirmButtonText: $t('lz.button.tmsMapLocation.applyAndSave'),
        cancelButtonText: $t('lz.button.tmsMapLocation.keepCurrentValue'),
      }
    )
    // Áp dụng như bấm ✓ từng ô — giữ nguyên logic kẹp zoom/rotation/bán kính của handleSetConfig.
    // minZoom/maxZoom trước defaultZoom để defaultZoom được kẹp theo khoảng mới.
    const order: ViewField[] = ['minZoom', 'maxZoom', 'defaultZoom', 'latitude', 'longtitude', 'rotation', 'boundRadius']
    order
      .filter((field) => pendingFields.some((item) => item.field === field))
      .forEach((field) => handleSetConfig(undefined as unknown as MouseEvent, field))
    return true
  } catch (action) {
    // 'cancel' = giữ giá trị cũ và vẫn lưu; 'close' (X / Esc) = huỷ lưu
    return action === 'cancel'
  }
}

const handleSave = async () => {
  // BE từ chối cả lô nếu có zone khai Min > Max — chặn trước khi lưu bản đồ.
  if (mapStore.getInvalidZoneSpeedDraftIds().length) {
    ElMessage.warning($t('lz.message.tmsMapLocation.zoneSpeedMinGreaterThanMax'));
    return;
  }
  if (!(await resolvePendingViewFields())) return;
  try {
    await ElMessageBox.confirm(
      $t('lz.message.tmsMapLocation.confirmSaveMap'),
      $t('lz.message.base.warning'),
      {
        confirmButtonText: $t('lz.button.base.confirm'),
        cancelButtonText: $t('lz.button.base.cancel'),
        type: 'warning',
      }
    );

    const saveData = props.allGeoJsonData?.features?.length ? props.allGeoJsonData : props.geoJsonData;
    emit('handle-save', {
      geoJson: JSON.stringify(saveData),
      status: 1,
      defaultZoom: state.defaultValue.defaultZoom,
      maxZoom: state.defaultValue.maxZoom,
      minZoom: state.defaultValue.minZoom,
      latitude: state.defaultValue.latitude,
      longtitude: state.defaultValue.longtitude,
      rotation: state.defaultValue.rotation,
      boundRadius: state.defaultValue.boundRadius,
    });
  } catch {
    // Người dùng hủy lưu bản đồ
  }
};


// ===== GeoJSON Export / Import =====
const geoJsonFileInput = ref<HTMLInputElement | null>(null)

/**
 * Tập feature ĐẦY ĐỦ, không phụ thuộc filter đang bật.
 * allGeoJsonData luôn giữ toàn bộ feature; geoJsonData chỉ là tập đang hiển thị nên
 * nếu xuất theo geoJsonData sẽ mất các thiết bị/zone đang bị filter ẩn.
 */
const getFullFeatureList = (): any[] => {
  if (props.allGeoJsonData?.features?.length) return props.allGeoJsonData.features

  // Fallback cho dữ liệu cũ chưa có allGeoJsonData: ghép lại phần đang bị filter ẩn.
  return [
    ...(props.geoJsonData?.features || []),
    ...((props.previousGeoJsonFeature as any[]) || []),
    ...((props.previousFeatureGeoJsonDataZone as any[]) || []),
  ].filter((feature: any) => feature != undefined)
}

const isBlockMapMode = () => props.mapMode === TmsTrafficInfoMap.Block || props.mapMode === TmsTrafficInfoMap.BlockVms

/**
 * Cấu hình cấp bản đồ đi kèm file — đúng những gì nút "Lưu" ghi vào CSDL:
 * - view: giá trị "Hiện tại" ở tab Khung nhìn (state.defaultValue), không phải giá trị đang gõ dở.
 * - display: icon, sự kiện, nhãn zone, màu nền (chỉ bản đồ ảnh).
 */
const buildMapConfigTransfer = () => ({
  view: { ...state.defaultValue },
  display: {
    showIconBackground: props.showIconBackground,
    lockZoom: props.lockZoom,
    lockRotation: props.lockRotation,
    iconSize: Number(props.iconSize) > 0 ? Number(props.iconSize) : 0,
    incidentIconSize: Number(props.incidentIconSize) > 0 ? Number(props.incidentIconSize) : 0,
    incidentZoneGap: resolveIncidentZoneGap(props.incidentZoneGap),
    incidentLineStyle: resolveIncidentLineStyle(props.incidentLineStyle),
    zoneBorderWidth: resolveZoneBorderWidth(props.zoneBorderWidth),
    flowLineColors: normalizeFlowLineColors(props.flowLineColors),
    flowLineWidth: resolveFlowLineWidth(props.flowLineWidth),
    backgroundColor: props.currentMapType === 'image' ? props.mapBackgroundColor : undefined,
    zoneLabelDisplay: { ...props.zoneLabelDisplay },
  },
})

/**
 * Payload dùng cho cả khung xem trước và file xuất ra (xem helpers/geoJsonTransfer.ts):
 * FeatureCollection đầy đủ + blockConfig + mapConfig + meta.
 * blockConfig luôn chuẩn hoá để giữ cả tuỳ chọn hiển thị card (mờ, tên tầng) khi chưa có block nào.
 * Ảnh / khối HTML lưu base64 trong properties nên đi kèm feature.
 */
const buildGeoJsonPayload = (exportedAt?: string) => buildGeoJsonTransfer({
  meta: { mapId: props.mapId || undefined, mapType: props.currentMapType || undefined, mapMode: props.mapMode || undefined, exportedAt },
  mapConfig: buildMapConfigTransfer(),
  blockConfig: normalizeBlockConfiguration(props.blockConfig),
  features: getFullFeatureList(),
})

/** Nội dung hiển thị ở tab GeoJSON — khớp đúng với file sẽ xuất ra (trừ thời điểm xuất). */
const geoJsonPreview = computed(() => buildGeoJsonPayload())

/** Đếm nhanh nội dung sẽ xuất để người dùng biết file gồm những gì. */
const geoJsonSummary = computed(() => {
  const features = getFullFeatureList()
  const countGroup = (groups: string[]) => features.filter((feature: any) => groups.includes(feature?.properties?.itemGroup)).length
  const blocks = props.blockConfig?.blocks || []
  return [
    { key: 'device', icon: 'ele-Cpu', label: $t('lz.label.tmsMapLocation.transferDevices'), value: countGroup(['device']) },
    { key: 'zone', icon: 'ele-Crop', label: $t('lz.label.tmsMapLocation.transferZones'), value: countGroup(['zone']) },
    { key: 'unit', icon: 'ele-OfficeBuilding', label: $t('lz.label.tmsMapLocation.transferUnits'), value: countGroup(['unit']) },
    { key: 'annotation', icon: 'ele-PictureRounded', label: $t('lz.label.tmsMapLocation.transferAnnotations'), value: countGroup(['text', 'image', 'html']) },
    { key: 'block', icon: 'ele-Grid', label: $t('lz.label.tmsMapLocation.transferBlocks'), value: blocks.length, hidden: !isBlockMapMode() && !blocks.length },
  ].filter((item) => !item.hidden)
})

/**
 * Xuất dữ liệu GeoJSON hiện tại ra file (.geojson) để tải về.
 */
const handleExportGeoJson = () => {
  const blob = new Blob([JSON.stringify(buildGeoJsonPayload(new Date().toISOString()), null, 2)], { type: 'application/json' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `map_${props.mapId || 'geojson'}_${Date.now()}.geojson`
  document.body.appendChild(a)
  a.click()
  document.body.removeChild(a)
  URL.revokeObjectURL(url)
}

/** Mở hộp thoại chọn file để nhập GeoJSON */
const triggerImportGeoJson = () => {
  geoJsonFileInput.value?.click()
}

/** Đọc file được chọn, parse JSON và áp dụng lên bản đồ */
const handleImportGeoJsonFile = async (e: Event) => {
  const input = e.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file) return
  try {
    const text = await file.text()
    const parsed = JSON.parse(text)
    applyImportedGeoJson(parsed)
  } catch (err) {
    ElMessage.error($t('lz.message.tmsMapLocation.importGeoJsonInvalid'))
  } finally {
    // Reset để có thể chọn lại cùng 1 file lần sau
    input.value = ''
  }
}

/**
 * Áp dụng khung nhìn từ file như thể người dùng đã bấm ✓ cho từng ô:
 * ghi vào defaultValue (giá trị "Lưu" sẽ dùng) + queryParams, rồi đưa bản đồ tới đó để xem trước.
 */
const applyImportedView = (view?: MapViewTransfer) => {
  if (!view) return
  const next = { ...state.defaultValue }
  ;(Object.keys(next) as (keyof typeof next)[]).forEach((key) => {
    const value = view[key]
    if (value !== undefined) next[key] = value
  })
  next.rotation = Math.min(360, Math.max(0, next.rotation))
  next.boundRadius = Math.max(0, next.boundRadius)
  if (next.minZoom > next.maxZoom && next.maxZoom > 0) next.minZoom = next.maxZoom
  next.defaultZoom = Math.max(next.minZoom, next.maxZoom > 0 ? Math.min(next.defaultZoom, next.maxZoom) : next.defaultZoom)

  Object.assign(state.defaultValue, next)
  Object.assign(state.queryParams, next)

  if (!props.map) return
  props.map.setView([next.latitude, next.longtitude], next.defaultZoom, { animate: false })
  if (typeof (props.map as any).setBearing === 'function') (props.map as any).setBearing(next.rotation)
  drawBoundPreview(next.boundRadius)
}

/** Cảnh báo kèm trong hộp xác nhận: khác hệ toạ độ (ảnh ↔ online) hoặc file có block nhưng bản đồ không ở chế độ block. */
const buildImportWarnings = (imported: ParsedGeoJsonTransfer) => {
  const warnings: string[] = []
  if (imported.mapConfig) {
    warnings.push($t('lz.message.tmsMapLocation.importGeoJsonReplaceConfig'))
  }
  if (imported.meta?.mapId && props.mapId && imported.meta.mapId !== props.mapId) {
    warnings.push($t('lz.message.tmsMapLocation.importGeoJsonOtherMap'))
  }
  if (!isSameCoordinateSystem(imported.meta?.mapType, props.currentMapType)) {
    warnings.push($t('lz.message.tmsMapLocation.importGeoJsonMapTypeMismatch'))
  }
  if (imported.blockConfig?.blocks?.length && !isBlockMapMode()) {
    warnings.push($t('lz.message.tmsMapLocation.importGeoJsonBlockIgnored'))
  }
  return warnings
}

/**
 * Áp dụng dữ liệu GeoJSON đã nhập: ghi đè features + blockConfig + cấu hình bản đồ,
 * render lại & đánh dấu chưa lưu (bấm "Lưu" để ghi vào CSDL).
 *
 * Việc ghi đè features/blockConfig/cấu hình hiển thị do LeafletMap thực hiện (emit 'import-geojson')
 * vì nó sở hữu geoJsonData/allGeoJsonData/blockConfig và cache filter — chỉ ở đó mới reset
 * đồng bộ được toàn bộ và render 1 lần. Khung nhìn thuộc state của component này.
 */
const applyImportedGeoJson = (parsed: any) => {
  const imported = parseGeoJsonTransfer(parsed)
  if (!imported) {
    ElMessage.error($t('lz.message.tmsMapLocation.importGeoJsonInvalid'))
    return
  }
  if (!imported.features.length) {
    ElMessage.error($t('lz.message.tmsMapLocation.importGeoJsonEmpty'))
    return
  }

  const sameCoordinateSystem = isSameCoordinateSystem(imported.meta?.mapType, props.currentMapType)
  const warnings = buildImportWarnings(imported)
  const confirmText = $t('lz.message.tmsMapLocation.importGeoJsonConfirm', { count: imported.features.length })

  ElMessageBox.confirm(
    warnings.length ? `${confirmText}\n\n⚠ ${warnings.join('\n⚠ ')}` : confirmText,
    $t('lz.message.base.warning'),
    {
      confirmButtonText: $t('lz.button.base.confirm'),
      cancelButtonText: $t('lz.button.base.cancel'),
      type: 'warning',
      customStyle: { whiteSpace: 'pre-line' },
    }
  )
    .then(() => {
      emit('import-geojson', {
        features: imported.features,
        blockConfig: imported.blockConfig,
        mapDisplay: imported.mapConfig?.display,
      })
      // Khung nhìn của bản đồ khác hệ toạ độ không dùng được (lat/lng ≠ pixel ảnh).
      if (sameCoordinateSystem) applyImportedView(imported.mapConfig?.view)

      const ignored = imported.skipped + imported.duplicates
      if (ignored > 0) {
        ElMessage.warning($t('lz.message.tmsMapLocation.importGeoJsonSkipped', { count: ignored }))
      }

      // Báo rõ số ảnh và số block để người dùng biết 2 phần này đã vào được.
      ElMessage.success(
        $t('lz.message.tmsMapLocation.importGeoJsonSuccessDetail', {
          count: imported.features.length,
          images: imported.features.filter((feature: any) => feature?.properties?.itemGroup === 'image').length,
          blocks: imported.blockConfig?.blocks?.length ?? 0,
        })
      )
    })
    .catch((err: any) => {
      // 'cancel'/'close' = người dùng huỷ; lỗi thật khi áp dụng thì ghi log để còn lần ra.
      if (err !== 'cancel' && err !== 'close') console.error('[GeoJSON import]', err)
    })
}


// Toggle panel visibility
const togglePanel = () => {
  isPanelCollapsed.value = !isPanelCollapsed.value
}

// Clear search input
const handleSearchClear = () => {
  tableSearch.value = ''
}


/**
 * Vị trí của từng thiết bị trong block (chỉ ở chế độ block): deviceId → block/tầng chứa nó.
 * Thiết bị trong block không bắt buộc có marker trên bản đồ nên không có feature —
 * dùng map này để vẫn liệt kê chúng ở tab Vị trí → Thiết bị.
 */
const blockDevicePlacements = computed(() => {
  const placements = new Map<string, { blockId: string; blockName: string; levelName: string }>()
  if (!isBlockMapMode()) return placements
  ;(props.blockConfig?.blocks || []).forEach((block: any, blockIndex: number) => {
    const blockName = block?.name || $t('lz.label.tmsMapLocation.defaultBlockName', { index: blockIndex + 1 })
    ;(block?.levels || []).forEach((level: any) => {
      ;(level?.devices || []).forEach((device: any) => {
        const deviceId = String(device?.deviceId ?? '').trim()
        if (deviceId && !placements.has(deviceId)) {
          placements.set(deviceId, { blockId: block.id, blockName, levelName: level?.name || '' })
        }
      })
    })
  })
  return placements
})

/**
 * Thêm vào bảng các thiết bị chỉ nằm trong block (không có feature). Dòng này chỉ để xem/định vị,
 * KHÔNG được ghi vào features nên không ảnh hưởng lưu/xuất GeoJSON hay danh sách "đã có trên bản đồ".
 * Thiết bị vừa có marker vừa nằm trong block: giữ 1 dòng của marker, gắn thêm nhãn block.
 */
const appendBlockDeviceRows = (rows: any[]) => {
  const placements = blockDevicePlacements.value
  if (!placements.size) return rows
  const featureDeviceIds = new Set<string>()
  rows.forEach((row: any) => {
    if (row?.itemGroup !== 'device') return
    featureDeviceIds.add(String(row.id))
    row.blockPlacement = placements.get(String(row.id))
  })
  placements.forEach((placement, deviceId) => {
    if (featureDeviceIds.has(deviceId)) return
    // blockConfig lưu deviceId dạng chuỗi, getItemDetails so sánh ===  → lấy id gốc từ danh sách thiết bị.
    const rawId = mapStore.equipmentList.find((item: any) => String(item.id) === deviceId)?.id ?? deviceId
    const itemDetails = mapStore.getItemDetails(rawId, 'device')
    rows.push({
      id: deviceId,
      name: itemDetails?.name || $t('lz.label.tmsMapLocation.unNamed'),
      geometryType: 'Block',
      itemGroup: 'device',
      equipmentType: itemDetails?.type,
      location: itemDetails,
      blockPlacement: placement,
      blockOnly: true,
    })
  })
  return rows
}

// Transform GeoJSON data into table data
const tableData = computed(() => {
  const source = props.allGeoJsonData?.features?.length ? props.allGeoJsonData : props.geoJsonData
  const rows = (source?.features || []).map((feature: any) => {
    const itemDetails = mapStore.getItemDetails(feature.properties?.id, feature.properties?.itemGroup)
    const displayName = feature.properties?.name || (itemDetails?.name && itemDetails.name !== 'Unknown Zone' ? itemDetails.name : '') ||  $t('lz.label.tmsMapLocation.unNamed');
    const baseData: any = {
      id: feature.properties?.id,
      name: displayName,
      geometryType: feature.geometry.type,
      itemGroup: feature.properties?.itemGroup,
      equipmentType: itemDetails?.type,
      location: itemDetails
    }

    if (feature.properties?.itemGroup === 'zone') {
      // Hướng tính toán chiều Km của polygon (lưu trong properties của geojson)
      baseData.kmDirection = feature.properties?.kmDirection || TmsZoneKmDirection.Auto
      // Hướng di chuyển (flow) của tuyến — cấu hình riêng
      baseData.flowDirection = feature.properties?.flowDirection || TmsZoneKmDirection.Auto
      const zone = mapStore.zoneList.find((z: any) => z.id === feature.properties?.id)
      if (zone) {
        baseData.fromKmNumber = zone.fromKmNumber
        baseData.fromMetNumber = zone.fromMetNumber
        baseData.toKmNumber = zone.toKmNumber
        baseData.toMetNumber = zone.toMetNumber
      }
    }

    // Nhãn văn bản / hình ảnh không gắn entity backend -> itemDetails.type undefined khiến
    // accordion hiển thị "undefined". Gán equipmentType = itemGroup ('text'/'image') để
    // getAccordionTitle trả về đúng tên nhóm.
    if (isAnnotationGroup(feature.properties?.itemGroup)) {
      baseData.equipmentType = feature.properties?.itemGroup
      if (feature.properties?.itemGroup === 'image') baseData.thumbnail = feature.properties?.imageUrl
    }

    if (feature.geometry.type === 'Point') {
      const [lng, lat] = feature.geometry.coordinates
      return {
        ...baseData,
        lat,
        lng
      }
    } else {
      const coordinates = feature.geometry.coordinates[0]
      const [firstLng, firstLat] = coordinates[0]
      return {
        ...baseData,
        topLeft: { lat: firstLat, lng: firstLng }
      }
    }
  })
  return appendBlockDeviceRows(rows)
})

/**
 * useDebounce to input search
*/
watchDebounced(tableSearch, () => {
  //console.log("watchDebounced - tableSearch", tableSearch)
  inputSearchDataTable.value = tableSearch.value;
}, { debounce: 500, maxWait: 5000 })




// ===== Danh sách tab Vị trí: tab con theo nhóm, nhóm theo loại, dòng mở rộng xem chi tiết =====

/** Tab gộp các chú thích tự do (chữ / ảnh / khối HTML) — không có trong danh sách nhóm của backend. */
const ANNOTATION_TAB = 'annotation'
const ANNOTATION_GROUPS = ['text', 'image', 'html']

/** Tab con chứa itemGroup tương ứng (chữ/ảnh/HTML cùng nằm ở tab Chú thích). */
const getLocationTabCode = (itemGroup?: string) => (isAnnotationGroup(itemGroup) ? ANNOTATION_TAB : itemGroup || '')

const LOCATION_TAB_ICONS: Record<string, string> = {
  device: 'ele-Cpu',
  zone: 'ele-Crop',
  unit: 'ele-OfficeBuilding',
  [ANNOTATION_TAB]: 'ele-PictureRounded',
}
const ANNOTATION_ICONS: Record<string, string> = {
  text: 'ele-EditPen',
  image: 'ele-Picture',
  html: 'ele-Memo',
}

/** Kết quả tìm kiếm (theo tên / loại hình học / tên block) dùng chung cho mọi tab. */
const searchedTableData = computed(() => {
  const search = inputSearchDataTable.value.toLowerCase().trim()
  if (!search) return tableData.value
  return tableData.value.filter((item: any) =>
    item.name?.toLowerCase().includes(search) ||
    item.geometryType?.toLowerCase().includes(search) ||
    item.blockPlacement?.blockName?.toLowerCase().includes(search)
  )
})

/**
 * Nhóm theo tab → theo loại (equipmentType), sắp xếp sẵn theo tên.
 * Tính 1 lần trong computed thay vì sort ngay trong template (sort mutate mảng mỗi lần render).
 */
const locationGroups = computed(() => {
  const result: Record<string, { key: string; title: string; items: any[] }[]> = {}
  const buckets = new Map<string, { key: string; tab: string; type: string; items: any[] }>()
  searchedTableData.value.forEach((item: any) => {
    const tab = getLocationTabCode(item?.itemGroup)
    if (!tab) return
    const type = String(item.equipmentType ?? '')
    const key = `${tab}:${type}`
    if (!buckets.has(key)) buckets.set(key, { key, tab, type, items: [] })
    buckets.get(key)!.items.push(item)
  })
  ;[...buckets.values()]
    .map((bucket) => ({ ...bucket, title: getAccordionTitle(bucket.type) }))
    .sort((a, b) => String(a.title).localeCompare(String(b.title)))
    .forEach((bucket) => {
      bucket.items.sort((a: any, b: any) => String(a.name ?? '').localeCompare(String(b.name ?? '')))
      ;(result[bucket.tab] ||= []).push({ key: bucket.key, title: bucket.title, items: bucket.items })
    })
  return result
})

/** Tab con: nhóm từ backend (thiết bị, zone, đơn vị…) + tab Chú thích; kèm số đối tượng (theo tìm kiếm). */
const locationTabs = computed(() =>
  [
    ...state.listEquipmentType.map((group: any) => ({ code: group.code, name: group.name })),
    { code: ANNOTATION_TAB, name: $t('lz.label.tmsMapLocation.annotationTab') },
  ].map((tab: any) => ({
    ...tab,
    icon: LOCATION_TAB_ICONS[tab.code] || 'ele-Location',
    count: (locationGroups.value[tab.code] || []).reduce((total, group) => total + group.items.length, 0),
  }))
)

/** Mọi nhóm của tab đang mở đều đang thu gọn? (đổi biểu tượng nút Thu gọn / Mở rộng tất cả) */
const isAllGroupsCollapsed = computed(() => {
  const groups = locationGroups.value[activeName.value] || []
  return groups.length > 0 && groups.every((group) => collapsedAccordion.value[group.key])
})

const toggleAllGroups = () => {
  const collapse = !isAllGroupsCollapsed.value
  ;(locationGroups.value[activeName.value] || []).forEach((group) => {
    collapsedAccordion.value[group.key] = collapse
  })
}

// ---- Mở rộng từng dòng (toạ độ, hướng, tốc độ) ----
const expandedRowIds = reactive(new Set<string>())

/** Zone đang có tốc độ Min > Max: luôn mở chi tiết để người dùng thấy ngay chỗ sai. */
const invalidSpeedZoneIds = computed(() => new Set((mapStore.getInvalidZoneSpeedDraftIds() || []).map((id: any) => String(id))))

const isRowExpandable = (item: any) => !item?.blockOnly && (item?.geometryType === 'Point' || item?.geometryType === 'Polygon')
const isRowExpanded = (item: any) =>
  isRowExpandable(item) && (expandedRowIds.has(String(item.id)) || invalidSpeedZoneIds.value.has(String(item.id)))

const toggleRow = (item: any) => {
  if (!isRowExpandable(item)) return
  const id = String(item.id)
  if (expandedRowIds.has(id)) expandedRowIds.delete(id)
  else expandedRowIds.add(id)
}

// ---- Hiển thị dòng ----
const toAssetUrl = (url?: string) => {
  if (!url) return ''
  if (/^(data:|blob:|https?:)/i.test(url)) return url
  return `${mapStore.baseUrl}/${url}`
}

/** Icon theo loại: thiết bị dùng icon loại thiết bị, đơn vị dùng icon loại đơn vị, ảnh dùng chính ảnh. */
const getRowIcon = (item: any) => {
  if (item.itemGroup === 'image') return toAssetUrl(item.thumbnail)
  if (item.itemGroup === 'device') return toAssetUrl(item.location?.iconUrl)
  if (item.itemGroup === 'unit') {
    const unitIcon = mapStore.workUnitTypeList.find((type: any) => type.code == item.location?.type)?.value
    return toAssetUrl(unitIcon || item.location?.iconUrl)
  }
  return ''
}

const getRowFallbackIcon = (item: any) =>
  ANNOTATION_ICONS[item.itemGroup] || LOCATION_TAB_ICONS[item.itemGroup] || (item.geometryType === 'Polygon' ? 'ele-Crop' : 'ele-Location')

const hideBrokenRowIcon = (event: Event) => {
  (event.target as HTMLImageElement).style.visibility = 'hidden'
}

const formatCoord = (value: any) => {
  const numberValue = Number(value)
  return Number.isFinite(numberValue) ? numberValue.toFixed(6) : '—'
}

const getFlowDirectionLabel = (value: any) =>
  value === TmsZoneKmDirection.Reverse ? $t('lz.label.tmsMapLocation.flowDirectionReverse') : $t('lz.label.tmsMapLocation.flowDirectionForward')
const getKmDirectionLabel = (value: any) =>
  value === TmsZoneKmDirection.Reverse ? $t('lz.label.tmsMapLocation.kmDirectionReverse') : $t('lz.label.tmsMapLocation.kmDirectionForward')

/** "60 – 100 km/h"; ô trống hiện giá trị mặc định theo hướng (giống placeholder ô nhập). */
const getZoneSpeedSummary = (zoneId: string) => {
  const value = mapStore.getZoneSpeedValue(zoneId)
  const min = value?.minSpeed ?? getZoneSpeedPlaceholder(zoneId, 'minSpeed')
  const max = value?.maxSpeed ?? getZoneSpeedPlaceholder(zoneId, 'maxSpeed')
  return `${min} – ${max} km/h`
}

/** Mở hộp thoại sửa nội dung chú thích (chữ / ảnh / HTML) từ danh sách. */
const openAnnotationRowEditor = (item: any) => {
  const feature = (props.allGeoJsonData?.features || []).find((f: any) => f?.properties?.id === item.id)
    || props.geoJsonData?.features?.find((f: any) => f?.properties?.id === item.id)
  if (!feature) return
  if (item.itemGroup === 'text') openTextEditor(feature)
  else if (item.itemGroup === 'image') openImageEditor(feature)
  else if (item.itemGroup === 'html') openHtmlEditor(feature)
}

//console.log("LocationEditor - filteredTableData", filteredTableData)

/**
 * Computed property for filtered item data
 */
const filteredItemData = computed(() => {
  //console.log("filteredItemData", state.itemList)
  const list = state.itemList.map((group: any) => ({
    ...group,
    children: group.children.map((item: any) => ({
      ...item,
      disabled: isIdInGeoJson(item.id)
    }))
  }));
  //console.log("filteredItemData - list",list)
  return list 
});


// ===== Map State Management =====
/**
 * Checks if an ID exists in the GeoJSON data
 */
const isIdInGeoJson = (id: string): boolean => {
  if (!id || id.startsWith('type_')) return false;

  //console.log("isIdInGeoJson - props.previousGeoJsonFeature", props.previousGeoJsonFeature)

  // Check in list previous first
  if(props.previousGeoJsonFeature && props.previousGeoJsonFeature.length > 0) 
  {
    const isIdExistInListPrevious = props.previousGeoJsonFeature.some((feature: any) =>
        feature.properties?.id === id
    );
    console.log("isIdInGeoJson - isIdExistInListPrevious", isIdExistInListPrevious)

    if(isIdExistInListPrevious){
      return true;
    }
  }
   // Check in list previous first
  if(props.previousFeatureGeoJsonDataZone && props.previousFeatureGeoJsonDataZone.length > 0) 
  {
    const isIdExistInListPrevious = props.previousFeatureGeoJsonDataZone.some((feature: any) =>
        feature.properties?.id === id
    );  

    if(isIdExistInListPrevious){
      return true;
    }
  }
  
  return props.geoJsonData.features.some((feature: any) =>
    feature.properties?.id === id
  );
};


// Handle location updates
const handleLocationUpdate = (row: any, value: number, field: 'lat' | 'lng') => {
  if (row.geometryType === 'Point') {
    const feature = props.geoJsonData?.features.find((f: any) => f.properties?.id === row.id)
      || props.allGeoJsonData?.features?.find((f: any) => f.properties?.id === row.id);
    if (!feature) return;

    // Update the row data first
    row[field] = value;

    // Update the feature coordinates
    if (field === 'lat') {
      feature.geometry.coordinates[1] = value;
    } else {
      feature.geometry.coordinates[0] = value;
    }

    // Update marker position on the map
    state.markersLayer?.eachLayer((layer: any) => {
      if (layer.feature?.properties?.id === row.id ||
        (layer.options && layer.options.id === row.id)) {
        layer.setLatLng([row.lat, row.lng]);
      }
    });

    emit('mark-unsaved', row.id);
  }
  else if (row.geometryType === 'Polygon') {
    handlePolygonUpdate(row, value, field);
  }

  //   emit('update-location', row, value, field)
}

const handlePolygonUpdate = (row: any, value: number, field: 'lat' | 'lng') => {
  const feature = props.geoJsonData.features.find((f: any) => f.properties?.id === row.id)
    || props.allGeoJsonData?.features?.find((f: any) => f.properties?.id === row.id);
  if (!feature || feature.geometry.type !== 'Polygon') return;


  const diff = value - row.topLeft[field];
  ////console.log("handlePolygonUpdate - diff", diff)
  const polygonCoords = feature.geometry.coordinates[0];
  const updatedCoords = polygonCoords.map((coord: number[]) => {
    const newCoord = [...coord];
    if (field === 'lat') {
      newCoord[1] += diff;
    } else {
      newCoord[0] += diff;
    }
    return newCoord;
  });

  feature.geometry.coordinates[0] = updatedCoords;

  // Update the polygon on the map
  state.polygonsLayer?.eachLayer((layer: any) => {
    if (layer.feature?.properties?.id === row.id ||
      (layer.options && layer.options.id === row.id)) {

      // //console.log("Update the polygon on the map", state.polygonsLayer)
      const latLngs = updatedCoords.map(([lng, lat]: number[]) => [lat, lng]);
      layer.setLatLngs(latLngs);
    }
  });

  // Update the row's topLeft coordinates
  row.topLeft[field] = value;
  emit('mark-unsaved', row.id);

  // emit('update-polygon-location', row, value, field)
}


/**
 * Hàm lấy title trong accordion
 */
const getAccordionTitle = (code: string) => {
  const found = state.itemGroup.find((item: any) => item.code === code)
  return found?.name || getConfigItemByCode(BaseConfigTypeEnum.TmsWorkUnitType,code)?.name || code
}

// ===== Map Navigation =====
/**
 * Locates and highlights a feature on the map
 */
const handleLocateOnMap = (row: any) => {
  if (row.blockOnly) {
    locateBlockDevice(row);
    return;
  }
  highlightFeature(row.id, 1500);
};

/** Đưa bản đồ tới card block chứa thiết bị và nhấp nháy thiết bị đó. */
const locateBlockDevice = (row: any) => {
  const blockId = row.blockPlacement?.blockId
  const block = props.blockConfig?.blocks?.find((item: any) => item.id === blockId)
  locateBlockOnMap(props.map as any, block, String(row.id))
};

/** Mở tab "Thiết lập block" để sửa/gỡ thiết bị trong block. */
const openBlockSetup = () => {
  activeTab.value = 'block'
};

// ===== Feature Editing Controls =====
/**
 * Starts editing mode for a feature
 */
const handleEditOnMap = (row: any) => {
  //console.log('start-editing')
  //console.log(row)
  emit('start-editing', row)
};

/**
 * Cập nhật hướng tính toán chiều Km của polygon zone và lưu vào properties GeoJSON.
 * Sau đó đánh dấu thay đổi chưa lưu để map render lại mũi tên hướng + tính lại vị trí icon sự cố.
 */
const handleKmDirectionChange = (row: any, val: string) => {
  const feature = props.geoJsonData?.features.find((f: any) => f.properties?.id === row.id)
    || props.allGeoJsonData?.features?.find((f: any) => f.properties?.id === row.id);
  if (!feature) return;
  if (!feature.properties) feature.properties = {};
  feature.properties.kmDirection = val;
  row.kmDirection = val;
  emit('mark-unsaved', row.id);
};

/**
 * Cập nhật HƯỚNG DI CHUYỂN (flow) của polygon zone — độc lập với kmDirection (lý trình).
 * Lưu vào feature.properties.flowDirection rồi đánh dấu chưa lưu để render lại dòng chảy.
 */
const handleFlowDirectionChange = (row: any, val: string) => {
  const feature = props.geoJsonData?.features.find((f: any) => f.properties?.id === row.id)
    || props.allGeoJsonData?.features?.find((f: any) => f.properties?.id === row.id);
  if (!feature) return;
  if (!feature.properties) feature.properties = {};
  feature.properties.flowDirection = val;
  row.flowDirection = val;
  emit('mark-unsaved', row.id);
};

// ===== Tốc độ min–max của zone =====

/** Placeholder ô min/max = mặc định theo hướng của zone (để trống thì dùng giá trị này). */
const getZoneSpeedPlaceholder = (zoneId: string, key: keyof ZoneSpeedValue) => {
  const row = mapStore.zoneSpeedRows[String(zoneId)];
  const direction = row ? directionOfZone(row) : null;
  const fallback = direction ? mapStore.zoneSpeedDefaults.find(item => item.direction === direction) : undefined;
  const value = key === 'minSpeed' ? fallback?.minSpeed : fallback?.maxSpeed;
  return value != null ? String(value) : '—';
};

/** Đang sửa chưa lưu / ngưỡng hiệu lực chưa hợp lệ (thiếu hoặc Min > Max). */
const getZoneSpeedRowClass = (zoneId: string) => ({
  'is-dirty': !!mapStore.zoneSpeedDrafts[String(zoneId)],
  'is-invalid': mapStore.getEffectiveZoneSpeed(zoneId)?.valid === false,
});

/** Ghi bản nháp rồi vẽ lại để nhãn trên bản đồ cập nhật ngay. */
const handleZoneSpeedChange = (zoneId: string, key: keyof ZoneSpeedValue, value: number | null | undefined) => {
  mapStore.setZoneSpeedDraft(zoneId, key, value == null || Number.isNaN(value) ? null : Math.round(value));
  emit('render-map');
};

/**
 * Starts moving mode for a feature
 */
const handleMoveOnMap = (row: any) => {
  emit('start-moving', row)
};

/** Feature Point của thiết bị đặt lẻ ứng với dòng trong danh sách. */
const findDevicePointFeature = (row: any) =>
  (props.allGeoJsonData?.features || []).find((feature: any) =>
    feature?.geometry?.type === 'Point' && feature.properties?.itemGroup === 'device' && String(feature.properties?.id) === String(row.id))

/** Icon thiết bị đặt lẻ đang được lật ngang hay không. */
const isDeviceIconFlipped = (row: any) => findDevicePointFeature(row)?.properties?.flipX === true

/** Bật/tắt lật ngang icon thiết bị đặt lẻ; LeafletMap ghi vào feature.properties.flipX và vẽ lại. */
const handleToggleIconFlip = (row: any) => {
  emit('update-icon-flip', { id: String(row.id), flipX: !isDeviceIconFlipped(row) })
};




// Add new function to finish editing/moving
const finishEditing = () => {
  if (!state.currentMode || !state.editingLayer) return;

  if (state.currentMode === 'edit') {
    (state.editingLayer as any).pm?.disable();
  } else if (state.currentMode === 'move') {
    if (state.editingLayer instanceof L.Marker) {
      (state.editingLayer as L.Marker).dragging?.disable();
    } else {
      (state.editingLayer as any).pm?.disable();
    }
  }

  state.currentMode = null;
  state.activeFeatureId = null;
  state.editingLayer = null;
  state.originalCoordinates = null;
  state.currentCoordinates = null;
  emit('render-map');
  //   renderGeoJson();
};

/**
 * Highlights a feature on the map
 */
const highlightFeature = (id: string, duration?: number) => {
  // Don't create highlight if we're in edit mode
  if (state.currentMode === 'edit') return;

  // Clear existing highlight
  clearHighlight();

  const feature = props.geoJsonData.features.find((f: any) => f.properties?.id === id);

  if (!feature || !props.map) return;
  if (feature.geometry.type === 'Point') {
    const [lng, lat] = feature.geometry.coordinates;
    state.highlightLayer = L.circle([lat, lng], {
      color: themeConfig.themeConfig.primary,
      fillColor: themeConfig.themeConfig.primary,
      fillOpacity: 0.3,
      radius: 50,
      weight: 2,
      className: 'highlight-pulse'
    }).addTo(props.map);

    props.map.setView([lat, lng], props.map.getZoom());
  } else if (feature.geometry.type === 'Polygon') {
    const coords = feature.geometry.coordinates[0].map(([lng, lat]: number[]) => [lat, lng]);
    state.highlightLayer = L.polygon(coords, {
      color: themeConfig.themeConfig.primary,
      weight: 3,
      opacity: 1,
      fillColor: themeConfig.themeConfig.primary,
      fillOpacity: 0.3,
      className: 'highlight-pulse'
    }).addTo(props.map);

    props.map.fitBounds(state.highlightLayer.getBounds());
  }
  emit('render-map');
  // Clear highlight after duration if specified
  if (duration) {
    setTimeout(() => {
      clearHighlight();
    }, duration);
  }
};


// Add cleanup for highlight when needed
const clearHighlight = () => {
  if (state.highlightLayer) {
    props.map.removeLayer(state.highlightLayer);
    state.highlightLayer = null;
  }
  emit('render-map');
};

// ===== Change Tracking =====
/**
 * Marks a feature as having unsaved changes
 */
const markUnsaved = (id: string) => {
  state.unsavedChanges.add(id);
  emit('render-map', {
    activeLayerId: id
  })
};

/**
 * Clears all unsaved changes tracking
 */
const clearUnsavedChanges = () => {
  state.unsavedChanges.clear();
  //   renderGeoJson();
};



// ===== Feature Deletion =====
/**
 * Shows confirmation dialog for feature deletion
 */
const handleDelete = (row: any): void => {
  ElMessageBox.confirm(
   $t('lz.message.base.confirmDelete', { name: row.name }),
    $t('lz.message.base.warning'),
    {
      confirmButtonText: $t('lz.button.base.confirm'),
      cancelButtonText: $t('lz.button.base.cancel'),
      type: 'warning',
      dangerouslyUseHTMLString: true,
      message: `
        <p>${$t('lz.message.tmsLocation.thisWillDelteItem')}:</p>
        <ul>
          <li><strong>${$t('lz.entity.base.name')}:</strong> ${row.name}</li>
          <li><strong>${$t('lz.entity.base.type')}:</strong> ${row.geometryType}</li>
          ${row.geometryType === 'Point'
          ? `<li><strong>${$t('lz.entity.tmsMapLocation.location')}:</strong> ${row.lat}, ${row.lng}</li>`
          : ''}
        </ul>
      `,
    }
  )
    .then(() => {
      removeFeatureById(row.id);
      ElMessage({
        type: 'success',
        message: `${$t('lz.message.base.deleteSuccess')} "${row.name}"`,
      });
    })
    .catch(() => {
      ElMessage({
        type: 'info',
        message: $t('lz.message.base.deleteCancel'),
      });
    });
};

/**
 * Removes a feature by its ID
 */
const removeFeatureById = (id: string) => {
  const featureToRemove = props.geoJsonData.features.find((feature: any) => feature.properties?.id === id);

  props.geoJsonData.features = props.geoJsonData.features.filter((feature: any) => feature.properties?.id !== id);

  if (props.allGeoJsonData?.features) {
    props.allGeoJsonData.features = props.allGeoJsonData.features.filter((feature: any) => feature.properties?.id !== id);
  }

  state.markersLayer.eachLayer((layer: any) => {
    if (layer.feature?.properties?.id === id) {
      state.markersLayer.removeLayer(layer);
    }
  });

  state.polygonsLayer.eachLayer((layer: any) => {
    if (layer.feature?.properties?.id === id) {
      state.polygonsLayer.removeLayer(layer);
    }
  });
  emit('render-map');
  //   renderGeoJson();
  //console.log('Removed feature:', featureToRemove);
};

/*
// Thêm item trên map
*/
const handleAddItem = () => {
  //console.log("handleAddItem - LocationEditor - queryParams - itemId", state.queryParams.itemId)
  emit('add-item', {
    group: state.queryParams.itemGroup,
    id: state.queryParams.itemId
  })
  state.queryParams.itemId = undefined // Reset selection after adding
}

// ===== Nhãn văn bản tự do (text annotation) =====
// Hộp thoại thêm/sửa: nội dung, cỡ chữ, xoay, hình dạng, màu, in đậm, co giãn theo zoom.
const textDialog = reactive({
  visible: false,
  mode: 'add' as 'add' | 'edit',
  editingId: '' as string,
  form: {
    text: '',
    fontSize: 14,
    rotation: 0,
    shape: 'none',
    textColor: '#1f2d3d',
    bgColor: '',
    bold: true,
    /**
     * true = kích thước "dính" vào bản đồ: cỡ chữ nhập ở đây là cỡ tại mức zoom hiện
     * tại, zoom vào/ra sẽ to/nhỏ theo. false = luôn giữ đúng số px trên màn hình.
     * Bắt buộc bật khi muốn nhiều phần tử liên kết thành cụm zoom đồng bộ.
     */
    scaleWithZoom: true,
    /** true = cố định theo bản đồ (xoay bản đồ thì xoay theo); false = luôn giữ hướng trên màn hình. */
    rotateWithMap: true,
  },
})

/** Mức zoom hiện tại của bản đồ — dùng làm mốc refZoom khi bật co giãn theo zoom. */
const getCurrentMapZoom = () => Number((props.map as any)?.getZoom?.() ?? 0)

/** Thuộc tính co giãn gửi kèm payload add/update của annotation. */
const buildScaleModeProps = (scaleWithZoom: boolean) =>
  scaleWithZoom ? { scaleMode: 'geo', refZoom: getCurrentMapZoom() } : { scaleMode: 'fixed', refZoom: undefined }

// Màu nền gợi ý sẵn cho nhãn văn bản
const bgColorPresets = [
  'rgba(255,255,255,0.85)', '#ffffff', '#000000', 'rgba(0,0,0,0.55)',
  '#f56c6c', '#e6a23c', '#67c23a', '#409eff', '#909399',
]

// Xem trước kiểu chữ trong hộp thoại (đồng bộ với cách render trên bản đồ)
const previewStyle = computed(() => {
  const f = textDialog.form
  const s: Record<string, string> = {
    transform: `rotate(${f.rotation || 0}deg)`,
    fontSize: `${f.fontSize || 14}px`,
    color: f.textColor || '#1f2d3d',
    fontWeight: f.bold ? '700' : '400',
  }
  if (f.bgColor) s.background = f.bgColor
  return s
})

const openAddTextDialog = () => {
  textDialog.mode = 'add'
  textDialog.editingId = ''
  textDialog.form = { text: '', fontSize: 14, rotation: 0, shape: 'none', textColor: '#1f2d3d', bgColor: '', bold: true, scaleWithZoom: true, rotateWithMap: true }
  textDialog.visible = true
}

/**
 * Mở hộp thoại chỉnh sửa 1 nhãn văn bản đã có (gọi từ useMapAnnotations khi nhấp đúp
 * vào nhãn trên bản đồ hoặc bấm "Sửa" trên thanh công cụ).
 */
const openTextEditor = (feature: any) => {
  const p = feature?.properties || {}
  if (p.itemGroup !== 'text') return
  textDialog.mode = 'edit'
  textDialog.editingId = p.id
  textDialog.form = {
    text: p.text ?? p.name ?? '',
    fontSize: Number(p.fontSize) || 14,
    rotation: Number(p.rotation) || 0,
    shape: p.shape || 'none',
    textColor: p.textColor || '#1f2d3d',
    bgColor: p.bgColor || '',
    bold: p.bold !== false,
    scaleWithZoom: p.scaleMode === 'geo',
    rotateWithMap: p.rotateWithMap !== false,
  }
  textDialog.visible = true
}

const confirmTextDialog = () => {
  const text = (textDialog.form.text || '').trim()
  if (!text) {
    ElMessage.warning($t('lz.message.tmsLocation.warningPleaseInputText'))
    return
  }
  const payloadProps = {
    text,
    fontSize: textDialog.form.fontSize,
    rotation: textDialog.form.rotation,
    shape: textDialog.form.shape,
    textColor: textDialog.form.textColor,
    bgColor: textDialog.form.bgColor || '',
    bold: textDialog.form.bold,
    ...buildScaleModeProps(textDialog.form.scaleWithZoom),
    rotateWithMap: textDialog.form.rotateWithMap,
  }
  if (textDialog.mode === 'add') {
    emit('add-item', { group: 'text', id: '', ...payloadProps })
  } else {
    emit('update-text', { id: textDialog.editingId, props: payloadProps })
  }
  textDialog.visible = false
}

// ===== Hình ảnh tự do trên bản đồ (image annotation) =====
// Ảnh được đọc thành base64 rồi lưu thẳng trong properties.imageUrl của feature nên
// persist cùng geojson, không phụ thuộc file server (giống map dạng ảnh trong TmsMap.url).
const imageUploadRef = ref<UploadInstance>()
const IMAGE_MAX_SIZE_MB = 2

const imageDialog = reactive({
  visible: false,
  mode: 'add' as 'add' | 'edit',
  editingId: '' as string,
  form: {
    name: '',
    imageUrl: '',
    width: 120,
    /** 0 = chiều cao tự động theo tỉ lệ ảnh gốc. */
    height: 0,
    rotation: 0,
    opacity: 1,
    borderRadius: 0,
    /** Xem chú thích ở textDialog.form.scaleWithZoom. */
    scaleWithZoom: true,
    /** true = cố định theo bản đồ (xoay bản đồ thì xoay theo); false = luôn giữ hướng trên màn hình. */
    rotateWithMap: true,
  },
})

const createEmptyImageForm = () => ({ name: '', imageUrl: '', width: 120, height: 0, rotation: 0, opacity: 1, borderRadius: 0, scaleWithZoom: true, rotateWithMap: true })

/**
 * Đọc file ảnh người dùng chọn thành base64. Giới hạn dung lượng vì ảnh nằm trong
 * geojson của cấu hình bản đồ; ảnh quá lớn sẽ làm bản ghi cấu hình phình to.
 */
const handleImageFileChange = (file: any) => {
  imageUploadRef.value?.clearFiles()
  const rawFile: File | undefined = file?.raw
  if (!rawFile) return

  if (rawFile.size > IMAGE_MAX_SIZE_MB * 1024 * 1024) {
    ElMessage.warning($t('lz.message.tmsMapLocation.warningImageTooLarge', { size: IMAGE_MAX_SIZE_MB }))
    return
  }

  const reader = new FileReader()
  reader.onload = () => {
    imageDialog.form.imageUrl = reader.result as string
    if (!imageDialog.form.name) imageDialog.form.name = rawFile.name
  }
  reader.onerror = () => ElMessage.error($t('lz.message.tmsMapLocation.errorReadImage'))
  reader.readAsDataURL(rawFile)
}

const openAddImageDialog = () => {
  imageDialog.mode = 'add'
  imageDialog.editingId = ''
  imageDialog.form = createEmptyImageForm()
  imageDialog.visible = true
}

/**
 * Mở hộp thoại chỉnh sửa 1 hình ảnh đã có (gọi từ useMapAnnotations khi nhấp đúp vào
 * ảnh trên bản đồ hoặc bấm "Sửa" trên thanh công cụ).
 */
const openImageEditor = (feature: any) => {
  const p = feature?.properties || {}
  if (p.itemGroup !== 'image') return
  imageDialog.mode = 'edit'
  imageDialog.editingId = p.id
  imageDialog.form = {
    name: p.name ?? '',
    imageUrl: p.imageUrl ?? '',
    width: Number(p.width) > 0 ? Number(p.width) : 120,
    height: Number(p.height) > 0 ? Number(p.height) : 0,
    rotation: Number(p.rotation) || 0,
    opacity: Number(p.opacity) > 0 ? Number(p.opacity) : 1,
    borderRadius: Number(p.borderRadius) || 0,
    scaleWithZoom: p.scaleMode === 'geo',
    rotateWithMap: p.rotateWithMap !== false,
  }
  imageDialog.visible = true
}

const confirmImageDialog = () => {
  const imageUrl = (imageDialog.form.imageUrl || '').trim()
  if (!imageUrl) {
    ElMessage.warning($t('lz.message.tmsMapLocation.warningPleaseSelectImage'))
    return
  }
  const payloadProps = {
    name: (imageDialog.form.name || '').trim() || $t('lz.label.tmsMapLocation.imageItemGroup'),
    imageUrl,
    width: imageDialog.form.width,
    height: imageDialog.form.height,
    rotation: imageDialog.form.rotation,
    opacity: imageDialog.form.opacity,
    borderRadius: imageDialog.form.borderRadius,
    ...buildScaleModeProps(imageDialog.form.scaleWithZoom),
    rotateWithMap: imageDialog.form.rotateWithMap,
  }
  if (imageDialog.mode === 'add') {
    emit('add-item', { group: 'image', id: '', ...payloadProps })
  } else {
    emit('update-image', { id: imageDialog.editingId, props: payloadProps })
  }
  imageDialog.visible = false
}

/** Xoá ảnh đang sửa khỏi bản đồ (ảnh không có dòng riêng trong bảng vị trí). */
const deleteEditingImage = () => {
  const id = imageDialog.editingId
  if (!id) return
  removeFeatureById(id)
  imageDialog.visible = false
  ElMessage.success($t('lz.message.base.deleteSuccess'))
}

// ===== Khối HTML tự do trên bản đồ (html annotation) =====
// Ghép chữ định dạng + nhiều hình trong 1 phần tử, soạn bằng Jodit. HTML lưu trong
// properties.html và luôn được lọc (sanitize) khi hiển thị lên bản đồ.


const htmlEditorButtons: string[] = [
  'undo', 'redo', '|',
  'bold', 'italic', 'underline', 'strikethrough', '|',
  'font', 'fontsize', 'brush', 'paragraph', '|',
  'ul', 'ol', 'align', '|',
  'customUploadImage', 'table', 'hr', '|',
  'eraser', 'source',
]
const htmlEditorPlugins: string[] = [
  'enter', 'backspace', 'delete-command', 'hotkeys', 'size', 'placeholder',
  'wrap-nodes', 'dtd', 'clean-html', 'clipboard', 'paste',
  'bold', 'font', 'color', 'format-block', 'ordered-list', 'justify',
  'redo-undo', 'image', 'table', 'hr', 'source',
  'inline-popup', 'select-cells', 'resize-cells', 'resizer',
]

const createEmptyHtmlForm = () => ({ html: '', width: HTML_WIDTH_DEFAULT, rotation: 0, scaleWithZoom: true, rotateWithMap: true })

const htmlDialog = reactive({
  visible: false,
  mode: 'add' as 'add' | 'edit',
  editingId: '' as string,
  /** Tăng mỗi lần mở để Jodit tạo mới với đúng nội dung. */
  editorKey: 0,
  form: createEmptyHtmlForm(),
})

/** Ảnh chèn vào khối HTML: đọc thành base64 (cùng giới hạn dung lượng với ảnh tự do). */
const readHtmlImageAsDataUrl = (file: File): Promise<string> => {
  if (file.size > IMAGE_MAX_SIZE_MB * 1024 * 1024) {
    ElMessage.warning($t('lz.message.tmsMapLocation.warningImageTooLarge', { size: IMAGE_MAX_SIZE_MB }))
    return Promise.resolve('')
  }
  return new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.onload = () => resolve(reader.result as string)
    reader.onerror = () => reject(reader.error)
    reader.readAsDataURL(file)
  })
}

const openAddHtmlDialog = () => {
  htmlDialog.mode = 'add'
  htmlDialog.editingId = ''
  htmlDialog.form = createEmptyHtmlForm()
  htmlDialog.editorKey++
  htmlDialog.visible = true
}

/** Mở hộp thoại sửa 1 khối HTML đã có (nhấp đúp trên bản đồ hoặc nút bút chì). */
const openHtmlEditor = (feature: any) => {
  const p = feature?.properties || {}
  if (p.itemGroup !== 'html') return
  htmlDialog.mode = 'edit'
  htmlDialog.editingId = p.id
  htmlDialog.form = {
    html: p.html ?? '',
    width: Number(p.width) > 0 ? Number(p.width) : HTML_WIDTH_DEFAULT,
    rotation: Number(p.rotation) || 0,
    scaleWithZoom: p.scaleMode === 'geo',
    rotateWithMap: p.rotateWithMap !== false,
  }
  htmlDialog.editorKey++
  htmlDialog.visible = true
}

const confirmHtmlDialog = () => {
  const html = htmlDialog.form.html || ''
  if (!hasAnnotationHtmlContent(html)) {
    ElMessage.warning($t('lz.message.tmsMapLocation.warningPleaseInputHtml'))
    return
  }
  const payloadProps = {
    // Tên trong bảng vị trí = đoạn chữ thuần rút gọn của nội dung
    name: getAnnotationHtmlSummary(html) || $t('lz.label.tmsMapLocation.htmlItemGroup'),
    html,
    width: htmlDialog.form.width,
    rotation: htmlDialog.form.rotation,
    ...buildScaleModeProps(htmlDialog.form.scaleWithZoom),
    rotateWithMap: htmlDialog.form.rotateWithMap,
  }
  if (htmlDialog.mode === 'add') {
    emit('add-item', { group: 'html', id: '', ...payloadProps })
  } else {
    emit('update-html', { id: htmlDialog.editingId, props: payloadProps })
  }
  htmlDialog.visible = false
}

/** Xoá khối HTML đang sửa khỏi bản đồ. */
const deleteEditingHtml = () => {
  const id = htmlDialog.editingId
  if (!id) return
  removeFeatureById(id)
  htmlDialog.visible = false
  ElMessage.success($t('lz.message.base.deleteSuccess'))
}

/*
// Vẽ zone trên map
*/
const handleStartDrawingPolygon = () => {
  //console.log("TypeId", state.queryParams.itemId)
  if (props.map) {
    emit('handle-start-drawing-polygon', {
      typeId: state.queryParams.itemId,
      group: state.queryParams.itemGroup,
    })
  } else {
    //console.log("Map doesn't initial")
  }

}

/**
 * Xử lý tìm tên item được thêm vào bản đồ theo id
 * @param list 
 * @param id 
 */
const findItemNameById = (list: any[], id: string | any): string => {
  for (const item of list) {
    if (item.id === id) return item.name || '';

    if (item.children && item.children.length) {
      const found = findItemNameById(item.children, id);
      if (found) return found;
    }
  }
  return '';
};

// ChuongLP - 16/04/2026 - Xử lý disable itemId khi chưa chọn item group hoặc list item trống
const isItemIdDisabled = computed(() => {
  return !state.queryParams.itemGroup || filteredItemData.value.length === 0; // Xử lý trường hợp chưa chọn item group hoặc list item trống
});

/**
 * Hàm lấy tên item được thêm vào bản đồ
 */
const handleGetItemName = () => {
  return findItemNameById(state.itemList, state.queryParams.itemId);
};

/**
 * Hàm reset type id bằng null
 */
const handleResetTypeIdEqualNull = () => {
  state.queryParams.itemId = undefined;
}

/**
 * Hàm lấy id item khi chọn để add trên bản đồ
 */
const handleGetItemId = () => {
  return state.queryParams.itemId;
}

const handleGetItemGroup = () => {
  return state.queryParams.itemGroup;
}

// function $t(arg0: string): import("element-plus").MessageParamsWithType {
//   throw new Error('Function not implemented.')
// }

/**
 * Scroll đến item trong bảng LocationEditor khi user click icon trên bản đồ.
 * - Mở panel nếu đang collapsed
 * - Chuyển sang tab "table"
 * - Chuyển sub-tab đúng itemGroup
 * - Mở accordion chứa item
 * - Scroll & highlight dòng item
 */
const scrollToItem = (itemId: string, itemGroup?: string) => {
  if (!itemId) return;

  // 1. Mở panel
  isPanelCollapsed.value = false;

  // 2. Chuyển tab table
  activeTab.value = 'table';

  // 3. Chuyển sub-tab itemGroup
  if (itemGroup) {
    activeName.value = getLocationTabCode(itemGroup);
  }

  // 4. Mở tất cả accordion trong nhóm để đảm bảo item visible
  // (collapsedAccordion key = equipmentType - set false = mở)
  Object.keys(collapsedAccordion.value).forEach((key) => {
    collapsedAccordion.value[key] = false;
  });

  // 5. Scroll đến item sau khi DOM cập nhật
  setTimeout(() => {
    const el = document.querySelector(`[data-item-id="${itemId}"]`) as HTMLElement;
    if (el) {
      el.scrollIntoView({ behavior: 'smooth', block: 'center' });
      // Highlight tạm thời
      el.classList.add('item-row--highlighted');
      setTimeout(() => el.classList.remove('item-row--highlighted'), 2500);
    }
  }, 150);
};

defineExpose({
  ruleFormRef,
  itemGroup: state.queryParams.itemGroup,
  itemId: state.queryParams.itemId,
  handleResetTypeIdEqualNull,
  handleItemGroupChange,
  handleGetItemId,
  handleGetItemGroup,
  handleGetItemName,
  scrollToItem,
  openTextEditor,
  openImageEditor,
  openHtmlEditor
});

</script>


<style scoped>

/* ===== Hộp thoại hình ảnh trên bản đồ: vùng chọn ảnh + xem trước ===== */
.image-editor-dialog__upload {
  width: 100%;
}
.image-editor-dialog__upload :deep(.el-upload) {
  width: 100%;
}
.image-editor-dialog__preview,
.image-editor-dialog__placeholder {
  width: 100%;
  min-height: 120px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 6px;
  padding: 8px;
  border: 1px dashed var(--el-border-color);
  border-radius: 6px;
  background: var(--el-fill-color-lighter);
  color: var(--el-text-color-secondary);
  font-size: 12px;
  transition: border-color 0.2s ease, background-color 0.2s ease;
}
.image-editor-dialog__preview:hover,
.image-editor-dialog__placeholder:hover {
  border-color: var(--el-color-primary);
  background: var(--el-color-primary-light-9);
}
.image-editor-dialog__preview img {
  max-width: 100%;
  max-height: 180px;
  object-fit: contain;
}
.html-editor-dialog__hint {
  margin-top: 6px;
  font-size: 12px;
}

.image-editor-dialog__footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
}

/* ===== Hộp thoại nhãn văn bản: khu vực xem trước ===== */
.text-editor-dialog__preview {
  width: 100%;
  min-height: 56px;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 10px;
  border: 1px dashed var(--el-border-color);
  border-radius: 6px;
  background:
    linear-gradient(45deg, #f5f7fa 25%, transparent 25%),
    linear-gradient(-45deg, #f5f7fa 25%, transparent 25%),
    linear-gradient(45deg, transparent 75%, #f5f7fa 75%),
    linear-gradient(-45deg, transparent 75%, #f5f7fa 75%);
  background-size: 16px 16px;
  background-position: 0 0, 0 8px, 8px -8px, -8px 0;
  overflow: hidden;
}
.text-editor-dialog__preview-chip {
  display: inline-block;
  white-space: nowrap;
  line-height: 1.2;
  text-shadow: 0 0 3px #fff, 0 0 3px #fff, 1px 1px 2px rgba(0, 0, 0, 0.35);
  transition: all 0.15s ease;
}
/* Hình dạng cho preview (khớp với bản đồ) */
.text-editor-dialog__preview-chip.map-text-shape--rect {
  padding: 3px 8px;
  border-radius: 4px;
  background: rgba(255, 255, 255, 0.92);
  border: 1px solid rgba(0, 0, 0, 0.15);
  text-shadow: none;
}
.text-editor-dialog__preview-chip.map-text-shape--pill {
  padding: 3px 12px;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.92);
  border: 1px solid rgba(0, 0, 0, 0.15);
  text-shadow: none;
}
.text-editor-dialog__preview-chip.map-text-shape--outline {
  padding: 3px 8px;
  border-radius: 4px;
  background: transparent;
  border: 1.5px solid currentColor;
}
.text-editor-dialog__preview-chip.map-text-shape--underline {
  padding-bottom: 2px;
  border-bottom: 2px solid currentColor;
}
/* Có màu nền + hình dạng không-khung/gạch-chân: thêm đệm + bo góc, bỏ viền sáng chữ */
.text-editor-dialog__preview-chip.map-text-shape--none.has-bg,
.text-editor-dialog__preview-chip.map-text-shape--underline.has-bg {
  padding: 2px 8px;
  border-radius: 4px;
  text-shadow: none;
}

:deep(.highlight-pulse) {
  animation: highlightPulse 1.5s ease-in-out infinite;
}

:deep(.highlight-pulse path) {
  animation: highlightPulse 1.5s ease-in-out infinite;
}

/* ===== Panel ===== */
.right-panel {
  position: absolute;
  bottom: 0;
  right: 0;
  width: 680px;
  height: 50%;
  background: var(--el-bg-color);
  /* Viền trái + trên tách panel khỏi bản đồ; góc trên-trái bo tròn */
  border: 1px solid var(--el-border-color);
  border-right: none;
  border-bottom: none;
  border-top-left-radius: 12px;
  transition: all 0.3s ease;
  z-index: 1000;
  box-shadow: -6px -4px 20px rgba(15, 23, 42, 0.14);
  display: flex;
  flex-direction: column;
}

.collapsed {
  transform: translateX(100%);
}

.collapse-toggle {
  position: absolute;
  left: -29px;
  top: 50%;
  transform: translateY(-50%);
  display: flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 48px;
  background: var(--el-bg-color);
  color: var(--el-color-primary);
  border-radius: 10px 0 0 10px;
  border: 1px solid var(--el-border-color);
  border-right: none;
  cursor: pointer;
  box-shadow: -4px 0 10px rgba(15, 23, 42, 0.1);
  transition: background-color 0.2s, color 0.2s;
}

.collapse-toggle:hover {
  background: var(--el-color-primary);
  color: #ffffff;
}

/* ===== Resize Bar (kéo chỉnh chiều cao + nút thao tác nhanh) ===== */
.resize-bar {
  position: relative;
  height: 22px;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: ns-resize;
  background: var(--el-fill-color-light);
  border-bottom: 1px solid var(--el-border-color-lighter);
  /* Vạch màu chủ đạo ở mép trên: điểm nhấn của panel cấu hình */
  box-shadow: inset 0 3px 0 var(--el-color-primary);
  border-top-left-radius: 11px;
  user-select: none;
}

.resize-bar:hover .resize-bar__grip {
  background: var(--el-color-primary);
}

.resize-bar__grip {
  width: 44px;
  height: 4px;
  border-radius: 3px;
  background: var(--el-border-color-darker);
  transition: background-color 0.15s ease;
}

.resize-bar__actions {
  position: absolute;
  right: 6px;
  top: 50%;
  transform: translateY(-50%);
  display: flex;
  align-items: center;
  gap: 4px;
  cursor: default;
}

.resize-bar__actions .el-button.is-circle {
  width: 20px !important;
  height: 20px !important;
  padding: 0 !important;
}

.resize-bar__actions .el-button .el-icon {
  font-size: 12px;
}

.resize-bar__label {
  font-size: 13px;
  font-weight: 700;
  line-height: 1;
}

.panel-content {
  flex: 1;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  padding: 0;
}

/* ===== Toolbar (top bar): thêm đối tượng | màu nền + Lưu ===== */
.toolbar {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 12px;
  border-bottom: 1px solid var(--el-border-color-lighter);
  background: var(--el-fill-color-lighter);
}

.toolbar__add {
  display: flex;
  flex: 1;
  align-items: center;
  gap: 6px;
  min-width: 0;
}

.toolbar__select {
  width: 150px;
  flex-shrink: 0;
}

.toolbar__cascader {
  flex: 1;
  min-width: 120px;
}

.toolbar__add .el-button,
.toolbar__actions .el-button {
  font-size: 12px;
}

.toolbar__add .el-button .el-icon,
.toolbar__actions .el-button .el-icon {
  margin-right: 2px;
}

.toolbar__actions {
  display: flex;
  flex-shrink: 0;
  align-items: center;
  gap: 6px;
  padding-left: 8px;
  border-left: 1px solid var(--el-border-color-lighter);
}

.toolbar__save {
  font-weight: 600;
}

/* ===== Main Tabs ===== */
.main-tabs {
  flex: 1;
  display: flex;
  flex-direction: column;
  height: calc(100% - 80px);
  padding: 0 10px;
}

.main-tabs :deep(.el-tabs__header) {
  margin-bottom: 0;
}

.main-tabs :deep(.el-tabs__nav-wrap::after) {
  height: 1px;
}

.main-tabs :deep(.el-tabs__content) {
  flex: 1;
  overflow: auto;
  padding-top: 6px;
}

.main-tabs :deep(.el-tab-pane) {
  height: 100%;
}

/* ===== Tab Vị trí ===== */
.table-container {
  height: 100%;
  overflow: auto;
  display: flex;
  flex-direction: column;
}

.table-toolbar {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 2px 2px 8px;
  /* Giữ khung tìm kiếm cố định ở đầu khi cuộn danh sách */
  position: sticky;
  top: 0;
  z-index: 5;
  background: var(--el-bg-color);
}

.table-toolbar__search {
  flex: 1;
}

.table-toolbar__search :deep(.el-input__wrapper) {
  border-radius: 8px;
  background: var(--el-fill-color-lighter);
}

.table-toolbar__search :deep(.el-input__wrapper.is-focus) {
  background: var(--el-bg-color);
}

.table-toolbar__toggle {
  width: 28px;
  padding: 0;
  border-radius: 8px;
}

/* Tab con dạng "viên thuốc": icon + tên + số lượng */
.sub-tabs :deep(.el-tabs__header) {
  margin-bottom: 8px;
}

.sub-tabs :deep(.el-tabs__nav-wrap::after) {
  display: none;
}

.sub-tabs :deep(.el-tabs__active-bar) {
  display: none;
}

.sub-tabs :deep(.el-tabs__nav) {
  display: flex;
  gap: 6px;
}

.sub-tabs :deep(.el-tabs__item) {
  height: 28px;
  padding: 0 10px !important;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 999px;
  background: var(--el-fill-color-lighter);
  color: var(--el-text-color-regular);
  font-size: 12px;
  line-height: 26px;
  transition: background-color 0.15s ease, border-color 0.15s ease, color 0.15s ease;
}

.sub-tabs :deep(.el-tabs__item:hover) {
  border-color: var(--el-color-primary-light-5);
  color: var(--el-color-primary);
}

.sub-tabs :deep(.el-tabs__item.is-active) {
  border-color: var(--el-color-primary);
  background: var(--el-color-primary);
  color: #ffffff;
}

.sub-tabs :deep(.el-tabs__content) {
  padding: 0 0 6px;
}

.loc-tab {
  display: inline-flex;
  align-items: center;
  gap: 5px;
}

.loc-tab__count {
  min-width: 18px;
  padding: 0 5px;
  border-radius: 999px;
  background: var(--el-fill-color-dark);
  color: var(--el-text-color-regular);
  font-size: 11px;
  font-weight: 600;
  line-height: 16px;
  text-align: center;
}

.sub-tabs :deep(.el-tabs__item.is-active) .loc-tab__count {
  background: rgba(255, 255, 255, 0.25);
  color: #ffffff;
}

/* Trạng thái trống */
.loc-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6px;
  padding: 28px 12px;
  border: 1px dashed var(--el-border-color);
  border-radius: 10px;
  color: var(--el-text-color-secondary);
  font-size: 12px;
  text-align: center;
}

.loc-empty__icon {
  font-size: 26px;
  color: var(--el-text-color-placeholder);
}

/* Nhóm theo loại (SettingsSection): thân danh sách sát mép hơn nhóm cài đặt */
.loc-group :deep(.settings-section__body) {
  padding: 4px 6px 6px;
}

.loc-group + .loc-group {
  margin-top: 8px;
}

/* ===== Dòng đối tượng ===== */
.item-row {
  border-radius: 8px;
  transition: background-color 0.15s ease, box-shadow 0.15s ease;
}

.item-row + .item-row {
  margin-top: 2px;
}

.item-row:hover,
.item-row.is-expanded {
  background: var(--el-fill-color-light);
}

.item-row.is-expanded {
  box-shadow: inset 3px 0 0 var(--el-color-primary);
}

.item-row--highlighted {
  background: var(--el-color-primary-light-9) !important;
  outline: 2px solid var(--el-color-primary-light-5);
  outline-offset: -1px;
  animation: highlightFade 2.5s ease-out forwards;
}

@keyframes highlightFade {
  0% { background: var(--el-color-primary-light-7); }
  70% { background: var(--el-color-primary-light-9); }
  100% { background: transparent; outline-color: transparent; }
}

.item-row__head {
  display: flex;
  align-items: center;
  gap: 8px;
  min-height: 40px;
  padding: 4px 6px;
}

.item-row__head.is-expandable {
  cursor: pointer;
}

.item-row__icon {
  display: inline-flex;
  flex: 0 0 auto;
  align-items: center;
  justify-content: center;
  width: 30px;
  height: 30px;
  overflow: hidden;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  background: var(--el-bg-color);
  color: var(--el-color-primary);
  font-size: 15px;
}

.item-row__icon img {
  width: 22px;
  height: 22px;
  object-fit: contain;
}

.item-row__icon--image img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.item-row__icon--zone {
  color: var(--el-color-success);
}

.item-row__icon--unit {
  color: var(--el-color-warning);
}

.item-row__icon--text,
.item-row__icon--html {
  color: var(--el-color-info);
}

.item-row__main {
  flex: 1;
  min-width: 0;
}

.item-row__name {
  overflow: hidden;
  color: var(--el-text-color-primary);
  font-size: 12px;
  font-weight: 600;
  line-height: 1.35;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.item-row__meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 4px 6px;
  margin-top: 2px;
  color: var(--el-text-color-secondary);
  font-size: 11px;
  line-height: 18px;
}

.item-row__coords-text {
  font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
  font-size: 10.5px;
}

.item-row__muted {
  font-style: italic;
}

.item-row__tag {
  display: inline-flex;
  align-items: center;
  gap: 3px;
  max-width: 100%;
  padding: 0 6px;
  border-radius: 999px;
  background: var(--el-fill-color);
  color: var(--el-text-color-regular);
  font-size: 11px;
  line-height: 18px;
  white-space: nowrap;
}

.item-row__tag > span {
  overflow: hidden;
  text-overflow: ellipsis;
}

.item-row__tag--block {
  background: var(--el-color-warning-light-9);
  color: var(--el-color-warning-dark-2);
}

.item-row__tag--flow {
  background: var(--el-color-primary-light-9);
  color: var(--el-color-primary);
}

.item-row__tag--km {
  background: var(--el-color-success-light-9);
  color: var(--el-color-success);
}

.item-row__tag--speed.is-dirty {
  background: var(--el-color-warning-light-9);
  color: var(--el-color-warning-dark-2);
}

.item-row__tag--speed.is-invalid {
  background: var(--el-color-danger-light-9);
  color: var(--el-color-danger);
}

/* Thao tác: nút icon phẳng, hiện màu khi rê chuột */
.item-row__actions {
  display: flex;
  flex-shrink: 0;
  align-items: center;
  gap: 2px;
}

.item-row__actions .item-row__btn {
  width: 26px;
  height: 26px;
  margin: 0;
  padding: 0;
  border-radius: 6px;
  color: var(--el-text-color-secondary);
}

.item-row__actions .item-row__btn:hover {
  background: var(--el-color-primary-light-9);
  color: var(--el-color-primary);
}

.item-row__actions .item-row__btn--danger:hover {
  background: var(--el-color-danger-light-9);
  color: var(--el-color-danger);
}

.item-row__chevron {
  flex: 0 0 auto;
  color: var(--el-text-color-placeholder);
  font-size: 12px;
  transition: transform 0.2s ease;
}

.item-row:not(.is-expanded) .item-row__chevron {
  transform: rotate(-90deg);
}

/* Chi tiết: lưới 2 cột nhãn trên – ô nhập dưới */
.item-row__body {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 8px 10px;
  padding: 4px 10px 10px 44px;
}

.item-field {
  display: flex;
  flex-direction: column;
  gap: 3px;
  min-width: 0;
}

.item-field--wide {
  grid-column: 1 / -1;
}

.item-field__label {
  color: var(--el-text-color-secondary);
  font-size: 11px;
  font-weight: 600;
}

.item-field :deep(.el-input-number),
.item-field :deep(.el-select) {
  width: 100%;
}

.item-field :deep(.el-input__inner) {
  font-size: 12px;
}

.item-field__pair {
  display: flex;
  align-items: center;
  gap: 6px;
}

.item-field__pair :deep(.el-input-number) {
  flex: 1;
}

.item-field__dash {
  color: var(--el-text-color-placeholder);
}

.item-field__hint {
  color: var(--el-text-color-placeholder);
  font-size: 11px;
  line-height: 1.4;
}

.item-row__speed-tag {
  color: var(--el-text-color-secondary);
  font-size: 10px;
  font-weight: 600;
}

.item-row__speed.is-dirty :deep(.el-input__wrapper) {
  box-shadow: 0 0 0 1px var(--el-color-warning) inset;
}

.item-row__speed.is-invalid :deep(.el-input__wrapper) {
  box-shadow: 0 0 0 1px var(--el-color-danger) inset;
}

/* ===== Tab Cấu hình: tab con + nhóm cài đặt (SettingsSection/SettingRow) ===== */
.config-subtabs {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.config-subtabs :deep(.el-tabs__header) {
  margin-bottom: 8px;
}

.config-subtabs :deep(.el-tabs__item) {
  height: 32px;
  padding: 0 12px;
  font-size: 13px;
}

.config-subtabs :deep(.el-tabs__content) {
  flex: 1;
  overflow: auto;
}

.config-subtabs__label {
  display: inline-flex;
  align-items: center;
  gap: 6px;
}

.config-container {
  padding: 2px 2px 10px;
}

.config-number {
  width: 120px;
}

.config-segmented {
  width: 100%;
}

/* Băng phạm vi ảnh hưởng ở đầu mỗi tab con */
.config-scope {
  display: flex;
  align-items: flex-start;
  gap: 8px;
  margin-bottom: 12px;
  padding: 8px 10px;
  border: 1px solid;
  border-radius: 8px;
  font-size: 13px;
  line-height: 1.45;
}

.config-scope .el-icon {
  flex: 0 0 auto;
  margin-top: 2px;
  font-size: 14px;
}

.config-scope--shared {
  border-color: var(--el-color-success-light-7);
  background: var(--el-color-success-light-9);
  color: var(--el-text-color-regular);
}

.config-scope--shared .el-icon {
  color: var(--el-color-success);
}

.config-scope--local {
  border-color: var(--el-border-color-lighter);
  background: var(--el-fill-color-lighter);
  color: var(--el-text-color-regular);
}

.config-scope--local .el-icon {
  color: var(--el-text-color-secondary);
}

/* Ghi chú nhỏ trong nhóm */
.config-note {
  display: flex;
  align-items: flex-start;
  gap: 6px;
  margin: 2px 0 8px;
  color: var(--el-text-color-secondary);
  font-size: 12px;
  line-height: 1.45;
}

.config-note .el-icon {
  flex: 0 0 auto;
  margin-top: 2px;
  color: var(--el-color-info);
}

.config-hint {
  display: flex;
  align-items: flex-start;
  gap: 6px;
  margin-bottom: 10px;
  padding: 8px 10px;
  border-radius: 8px;
  background: var(--el-color-primary-light-9);
  color: var(--el-text-color-regular);
  font-size: 12px;
  line-height: 1.45;
}

.config-hint .el-icon {
  flex: 0 0 auto;
  margin-top: 2px;
  color: var(--el-color-primary);
}

/* Khung nhìn: lưới 5 cột CỐ ĐỊNH — nhãn | hiện tại | → | giá trị mới | ✓ — để mọi dòng thẳng hàng.
   Cột "hiện tại" đủ rộng cho toạ độ 6 chữ số thập phân (vd 106.617220); nhãn dài được xuống dòng. */
.view-field {
  display: grid;
  grid-template-columns: 124px 108px 16px minmax(0, 1fr) 32px;
  align-items: center;
  gap: 10px;
  padding: 9px 0;
}

.view-field + .view-field {
  border-top: 1px dashed var(--el-border-color-lighter);
}

.view-field--head {
  padding: 4px 0 2px;
  color: var(--el-text-color-secondary);
  font-size: 12px;
  font-weight: 600;
  text-align: center;
}

.view-field--head + .view-field {
  border-top: 0;
}

.view-field__label {
  color: var(--el-text-color-primary);
  font-size: 13px;
  font-weight: 600;
  line-height: 1.3;
  overflow-wrap: anywhere;
}

/* Giá trị hiện tại: chip chỉ đọc cao bằng ô nhập (size default = 32px) */
.view-field__current {
  display: block;
  height: 32px;
  padding: 0 8px;
  overflow: hidden;
  border-radius: 6px;
  background: var(--el-fill-color-light);
  color: var(--el-text-color-regular);
  font-size: 13px;
  font-variant-numeric: tabular-nums;
  line-height: 32px;
  text-align: center;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.view-field__arrow {
  justify-self: center;
  color: var(--el-text-color-placeholder);
  font-size: 14px;
}

.view-field.is-changed .view-field__arrow {
  color: var(--el-color-primary);
}

.view-field__adjust {
  width: 100% !important;
}

.view-field__adjust :deep(.el-input__inner) {
  font-size: 13px;
  font-variant-numeric: tabular-nums;
}

.view-field.is-changed .view-field__adjust :deep(.el-input__wrapper) {
  box-shadow: 0 0 0 1px var(--el-color-primary) inset;
}

/* Nút ✓ vuông, cao bằng ô nhập */
.view-field .view-field__apply {
  width: 32px;
  height: 32px;
  padding: 0;
  border-radius: 6px;
}

.view-field__capture {
  width: 100%;
  margin-top: 6px;
}

/* ===== GeoJSON ===== */
.geojson-container {
  height: 100%;
  overflow: auto;
  padding: 8px;
}

.geojson-toolbar {
  display: flex;
  gap: 8px;
  padding-bottom: 8px;
  position: sticky;
  top: 0;
  z-index: 5;
  background: var(--el-bg-color, #ffffff);
}

.geojson-toolbar .el-button .el-icon {
  margin-right: 4px;
}

.geojson-summary {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin-bottom: 8px;
}

.geojson-summary__item {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 2px 8px;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 999px;
  background: var(--el-fill-color-light);
  color: var(--el-text-color-regular);
  font-size: 12px;
}

.geojson-summary__item b {
  color: var(--el-color-primary);
}

.geojson-summary__item--config {
  border-color: var(--el-color-success-light-7);
  background: var(--el-color-success-light-9);
  color: var(--el-color-success);
}

.geojson-hint {
  display: flex;
  align-items: flex-start;
  gap: 6px;
  margin-bottom: 8px;
  padding: 6px 10px;
  border-radius: 8px;
  background: var(--el-color-primary-light-9);
  color: var(--el-text-color-regular);
  font-size: 12px;
  line-height: 1.45;
}

.geojson-hint .el-icon {
  flex: 0 0 auto;
  margin-top: 2px;
  color: var(--el-color-primary);
}

.geojson-content {
  margin: 0;
  white-space: pre-wrap;
  word-wrap: break-word;
}

/* ===== PrimeVue Fieldset ===== */
:deep(.p-fieldset) {
  margin-bottom: 4px;
  border: 1px solid #e7edf4;
  border-radius: 6px;
  background: #ffffff;
  box-shadow: 0 1px 4px rgba(15, 23, 42, 0.04);
  transition: all 0.2s ease;
}

:deep(.p-fieldset:hover) {
  border-color: #d8e2ee;
  box-shadow: 0 2px 8px rgba(15, 23, 42, 0.06);
}

:deep(.p-fieldset-legend) {
  background: linear-gradient(180deg, #f8fbff 0%, #f4f8fc 100%);
  border: 1px solid #e7edf4;
  border-radius: 4px;
  padding: 0;
  font-size: 12px;
  font-weight: 600;
  color: #0f172a;
  margin: 2px 4px;
  display: flex;
  align-items: center;
}

:deep(.p-fieldset-legend a),
:deep(.p-fieldset-legend-button) {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 26px;
  height: 26px;
  color: #64748b;
  text-decoration: none;
  flex-shrink: 0;
}

:deep(.p-fieldset-legend a:hover),
:deep(.p-fieldset-legend-button:hover) {
  color: #334155;
}

:deep(.p-fieldset-legend-label) {
  padding: 3px 8px;
}

:deep(.p-fieldset-toggle-icon) {
  color: #64748b;
  font-size: 14px;
}

:deep(.p-fieldset-content) {
  padding: 6px 8px;
  overflow: visible !important;
}

/* ===== Màu đường định hướng theo hướng ===== */
.flow-color-list {
  display: flex;
  flex-direction: column;
  gap: 4px;
  width: 100%;
}

.flow-color-row {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 4px 6px 4px 10px;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  background: var(--el-fill-color-lighter);
}

.flow-color-row.is-all {
  border-style: dashed;
  background: transparent;
}

.flow-color-row__name {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-size: 13px;
  color: var(--el-text-color-primary);
}

.flow-color-row.is-all .flow-color-row__name {
  font-weight: 600;
}

.flow-color-row__count {
  flex-shrink: 0;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}
</style>
