<template>
    <div class="tms-baotri-thietbi-container">
      <el-card shadow="hover"
        :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
        <el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true"
          label-width="auto" style="flex: 1 1 0%">
          <el-row :gutter="10">
            <el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
              <el-form-item :label="$t('lz.entity.tms.equipmentType')" prop="equipmentTypeId">
                <el-select v-model="state.queryParams.equipmentTypeId" filterable clearable class="w100" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.equipmentType') })">
                  <el-option v-for="item in state.typeList" :key="item.id" :label="item.name" :value="item.id" />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
              <el-form-item :label="$t('lz.entity.tms.equipmentName')" prop="name">
                <el-input v-model="state.queryParams.name" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.tms.equipmentName') })" clearable />
              </el-form-item>
            </el-col>
            <el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
              <el-form-item :label="$t('lz.entity.tms.equipmentCode')" prop="code">
                <el-input v-model="state.queryParams.code" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.tms.equipmentCode') })" clearable />
              </el-form-item>
            </el-col>
          </el-row>
        </el-form>
        <el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />
        <el-row>
          <SearchButton @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
        </el-row>
      </el-card>
      <el-card class="full-table" shadow="hover" style="margin-top: 5px">
        <vxe-grid ref="xGrid" class="xGrid-style custom-grid" v-bind="options"
          :row-config="{ height: 75 }">
          <template #toolbar_buttons>
            <el-button type="primary" @click="handleAdd" style="margin-bottom: 10px;margin-right: 10px;">{{ $t('lz.button.base.add') }}</el-button>
          </template>
          <template #maintenance_guide_cell="{ row }">
            <div>
              <el-button type="primary" link @click="handleEditGuide(row)">{{ $t('lz.button.tmsMaintenanceGuide.viewEdit') }}</el-button>
            </div>
          </template>
          <template #row_buttons="{ row }">
            <el-tooltip content="Xem danh sách thiết bị" placement="top">
              <el-button icon="ele-List" text type="primary" @click="handleViewEquipmentList(row)"> </el-button>
            </el-tooltip>
            <el-tooltip content="Sửa" placement="top">
              <el-button icon="ele-Edit" text type="primary" @click="handleEdit(row)"> </el-button>
            </el-tooltip>
            <el-tooltip content="Xóa" placement="top">
              <el-button icon="ele-Delete" text type="danger" @click="handleDelete(row)"> </el-button>
            </el-tooltip>
          </template>
          <template #empty>
            <el-empty :image-size="200" />
          </template>
        </vxe-grid>
      </el-card>
      <EditBaoTriThietBi ref="editBaoTriThietBiRef" :title="state.title" :operateType="state.operateType" @handleQuery="handleQuery" />
      <EditHuongDanBaoTri ref="editHuongDanBaoTriRef" :title="state.guideTitle" :equipment="state.selectedEquipment" @handleQuery="handleQuery" />
      <ViewHuongDanBaoTri ref="viewHuongDanBaoTriRef" :title="state.guideTitle" :equipment="state.selectedEquipment" @handleEdit="handleUpdateGuide" />
      <DanhSachThietBi ref="danhSachThietBiRef" :title="state.listTitle" :equipmentType="state.selectedType" @handleQuery="handleQuery" />
    </div>
  </template>
  
  <script lang="ts" setup name="tmsBaoTriThietBi">
import { useI18n } from 'vue-i18n';


  import { reactive, ref, defineAsyncComponent } from 'vue';
  import { VxeGridInstance, VxeGridPropTypes } from 'vxe-table';
  import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';

const { t: $t } = useI18n();
  const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
  const EditBaoTriThietBi = defineAsyncComponent(() => import('./component/editBaoTri.vue'));
  const EditHuongDanBaoTri = defineAsyncComponent(() => import('./component/editHuongDan.vue'));
  const ViewHuongDanBaoTri = defineAsyncComponent(() => import('./component/viewHuongDan.vue'));
  const DanhSachThietBi = defineAsyncComponent(() => import('./component/danhSachThietBi.vue'));
  
  const xGrid = ref<VxeGridInstance>();
  const editBaoTriThietBiRef = ref();
  const editHuongDanBaoTriRef = ref();
  const viewHuongDanBaoTriRef = ref();
  const danhSachThietBiRef = ref();
  
  const state = reactive({
    queryParams: {
      equipmentTypeId: undefined,
      name: '',
      code: '',
    },
    typeList: [
      { id: 1, name: 'Máy bơm' },
      { id: 2, name: 'Hệ thống lọc' },
      { id: 3, name: 'Tủ điện' },
      { id: 4, name: 'Van chính' },
      { id: 5, name: 'Máy bơm 2' },
      { id: 6, name: 'Hệ thống lọc 2' },
      { id: 7, name: 'Tủ điện 2' },
      { id: 8, name: 'Van phụ' },
    ],
    title: '',
    operateType: 'add',
    data: [
      {
        id: 1,
        type: 1,
        name: 'Bảng quang báo điện tử Chainzone',
        code: 'Chainzone VMS-012568',
        maintenanceFrequency: 'Hàng tháng',
        maintenanceTime: '2 giờ',
        maintenanceGuide: 'Kiểm tra vệ sinh, kiểm tra các kết nối',
        attachments: []
      },
      {
        id: 2,
        type: 2,
        name: 'Camera giám sát Hikvision T03',
        code: 'Hikvision T03',
        maintenanceFrequency: 'Hàng tuần',
        maintenanceTime: '1 giờ',
        maintenanceGuide: 'Kiểm tra vệ sinh, kiểm tra các kết nối',
        attachments: []
      },
      {
        id: 3,
        type: 3,
        name: 'Tủ điện điều khiển ',
        code: 'TD001',
        maintenanceFrequency: 'Hàng quý',
        maintenanceTime: '4 giờ',
        maintenanceGuide: 'Kiểm tra các kết nối điện, vệ sinh bụi, kiểm tra nhiệt độ',
        attachments: []
      },
      {
        id: 4,
        type: 4,
        name: 'Van điều khiển chính',
        code: 'VD001',
        maintenanceFrequency: 'Hàng tháng',
        maintenanceTime: '1 giờ',
        maintenanceGuide: 'Kiểm tra hoạt động, bôi trơn, thay thế gioăng nếu cần',
        attachments: []
      },
      {
        id: 5,
        type: 5,
        name: 'Máy bơm dự phòng',
        code: 'MB002',
        maintenanceFrequency: 'Hàng tháng',
        maintenanceTime: '2 giờ',
        maintenanceGuide: 'Kiểm tra áp suất, vệ sinh bộ lọc, kiểm tra rò rỉ',
        attachments: []
      },
      {
        id: 6,
        type: 6,
        name: 'Hệ thống lọc nước phụ',
        code: 'HTL002',
        maintenanceFrequency: 'Hàng tuần',
        maintenanceTime: '1 giờ',
        maintenanceGuide: 'Vệ sinh bộ lọc, kiểm tra chất lượng nước, thay thế vật liệu lọc',
        attachments: []
      },
      {
        id: 7,
        type: 7,
        name: 'Tủ điện phụ',
        code: 'TD002',
        maintenanceFrequency: 'Hàng quý',
        maintenanceTime: '4 giờ',
        maintenanceGuide: 'Kiểm tra các kết nối điện, vệ sinh bụi, kiểm tra nhiệt độ',
        attachments: []
      },
      {
        id: 8,
        type: 8,
        name: 'Van điều khiển phụ',
        code: 'VD002',
        maintenanceFrequency: 'Hàng tháng',
        maintenanceTime: '1 giờ',
        maintenanceGuide: 'Kiểm tra hoạt động, bôi trơn, thay thế gioăng nếu cần',
        attachments: []
      }
    ],
    guideTitle: '',
    selectedEquipment: null,
    listTitle: '',
    selectedType: null,
  });
  
  const options = useVxeTable<any>(
    {
      id: 'baoTriThietBi',
      name: 'BaoTriThietBi',
      columns: [
        { field: 'name', title: $t('lz.entity.tms.equipmentName'), minWidth: 200, showOverflow: 'tooltip', sortable: true },
        { field: 'code', title: $t('lz.entity.tms.equipmentCode'), minWidth: 100, showOverflow: 'tooltip', sortable: true },
        { field: 'type', title: $t('lz.entity.tms.equipmentType'), minWidth: 150, showOverflow: 'tooltip', sortable: true,
          formatter: ({ cellValue }: any) => state.typeList.find((t: any) => t.id === cellValue)?.name || $t('lz.label.base.unknown') },
        { field: 'maintenanceFrequency', title: $t('lz.entity.tms.maintenanceFrequency'), minWidth: 150, showOverflow: 'tooltip' },
        { field: 'maintenanceTime', title: $t('lz.entity.tms.maintenanceDuration'), minWidth: 150, showOverflow: 'tooltip' },
        { field: 'maintenanceGuide', title: $t('lz.entity.tms.maintenanceGuide'), minWidth: 200, showOverflow: 'tooltip', slots: { default: 'maintenance_guide_cell' } },
        { title: $t('lz.label.base.action'), fixed: 'right', width: 180, showOverflow: true, slots: { default: 'row_buttons' } },
      ],
    },
    {
      proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
      toolbarConfig: { export: false },
      rowConfig: { height: 40 },
    }
  );
  
  const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
    // Lọc dữ liệu mock theo điều kiện tìm kiếm
    let filtered = state.data.filter(item => {
      let match = true;
      if (state.queryParams.equipmentTypeId) {
        match = match && item.type === state.queryParams.equipmentTypeId;
      }
      if (state.queryParams.name) {
        match = match && item.name.includes(state.queryParams.name);
      }
      if (state.queryParams.code) {
        match = match && item.code.includes(state.queryParams.code);
      }
      return match;
    });
    return { data: { result: { records: filtered, total: filtered.length } } };
  };
  
  const handleQuery = async () => {
    options.loading = true;
    await xGrid.value?.commitProxy('reload') ;
    options.loading = false;
  };
  
  const resetQuery = async () => {
    state.queryParams.equipmentTypeId = undefined;
    state.queryParams.name = '';
    state.queryParams.code = '';
    await xGrid.value?.commitProxy('reload');
  };
  
  const handleEdit = (row: any) => {
    state.title = $t('lz.label.tms.editEquipmentInformation');
    state.operateType = 'edit';
    editBaoTriThietBiRef.value?.openDialog(row);
  };
  
  const handleDelete = (row: any) => {
    // Xử lý xóa dữ liệu mock (chỉ demo)
    const idx = state.data.findIndex(item => item.id === row.id);
    if (idx !== -1) {
      state.data.splice(idx, 1);
      handleQuery();
    }
  };
  
  const handleAdd = () => {
    state.title = $t('lz.label.tms.addEquipmentInformation');
    state.operateType = 'add';
    editBaoTriThietBiRef.value?.openDialog({
      name: '',
      code: '',
      type: state.typeList[0].id,
    });
  };
  
  const handleEditGuide = (row: any) => {
    state.guideTitle = $t('lz.label.tmsMaintenanceGuide.viewTitle');
    state.selectedEquipment = row;
    viewHuongDanBaoTriRef.value?.openDialog(row);
  };
  
  const handleUpdateGuide = (equipment: any) => {
    state.guideTitle = $t('lz.label.tmsMaintenanceGuide.updateTitle');
    state.selectedEquipment = equipment;
    editHuongDanBaoTriRef.value?.openDialog(equipment);
  };
  
  const handleViewEquipmentList = (row: any) => {
    state.listTitle = `Danh sách thiết bị - ${row.name}`;
    state.selectedType = row;
    danhSachThietBiRef.value?.openDialog(row);
  };
  </script>
  
  <style scoped>
  .tms-baotri-thietbi-container {
    padding: 10px;
  }
  :deep(.el-calendar-day){
  height: 120px!important;
}
  </style>
  