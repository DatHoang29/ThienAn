<template>
  <div class="tms-lichsu-tuanduong-container">
    <el-card shadow="hover"
      :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
      <el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true"
        label-width="auto" style="flex: 1 1 0%">
        <el-row :gutter="10">
          <el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
            <el-form-item :label="$t('lz.entity.tms.employee')" prop="employee">
              <el-select v-model="state.queryParams.employee" filterable clearable class="w100" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.employee') })">
                <el-option v-for="item in state.employeeList" :key="item.id" :label="item.name" :value="item.id" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
            <el-form-item :label="$t('lz.label.base.fromDate')" prop="fromDate">
              <el-date-picker v-model="state.queryParams.fromDate" type="date" :placeholder="$t('lz.label.base.fromDate')" class="w100" />
            </el-form-item>
          </el-col>
          <el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
            <el-form-item :label="$t('lz.label.base.toDate')" prop="toDate">
              <el-date-picker v-model="state.queryParams.toDate" type="date" :placeholder="$t('lz.label.tmsDutySchedule.toDate')" class="w100" />
            </el-form-item>
          </el-col>
          <el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
            <el-form-item :label="$t('lz.entity.tms.shift')" prop="shift">
              <el-select v-model="state.queryParams.shift" filterable clearable class="w100" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.shift') })">
                <el-option v-for="item in state.shiftList" :key="item.id" :label="item.name" :value="item.id" />
              </el-select>
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
            <el-select v-model="state.selectedWeek" style="margin-left: 10px; width: 180px;margin-bottom: 10px;" @change="handleQuery">
            <el-option v-for="week in state.weekOptions" :key="week.value" :label="week.label" :value="week.value" />
            </el-select>
        </template>
        <template #toolbar_tools>
            <el-button type="primary" @click="handleAdd" style="margin-bottom: 10px;margin-right: 10px;">{{ $t('lz.button.tms.register') }}</el-button>
            <el-button v-show="state.hasChange" type="success" @click="handleSaveChange" style="margin-left: 10px; margin-bottom: 10px;">{{ $t('lz.button.base.saveChanges') }}</el-button>
        
        </template>
        <template #employee_cell="{ row }">
          <draggable
            :list="row.employees"
            :group="'employees'"
            item-key="id"
            :clone="cloneEmployee"
            @end="(evt) => onDragEnd(evt, row)"
            @move="(evt) => canMoveEmployee(evt, row)"
            style="display: flex; gap: 4px; min-height: 32px"
          >
            <template #item="{ element }">
              <el-tag :key="element.id" style="margin-right: 4px">{{ element.name }}</el-tag>
            </template>
          </draggable>
        </template>
        <template #row_buttons="{ row }">
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
    <EditLichSuTuanDuong ref="editLichSuTuanDuongRef" :title="state.title" :operateType="state.operateType" @handleQuery="handleQuery" />
  </div>
</template>

<script lang="ts" setup name="tmsLichSuTuanDuong">
import { useI18n } from 'vue-i18n';


import { onMounted, reactive, ref, defineAsyncComponent } from 'vue';
import { VxeGridInstance, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import draggable from 'vuedraggable';

const { t: $t } = useI18n();
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const EditLichSuTuanDuong = defineAsyncComponent(() => import('./component/editLichSuTuanDuong.vue'));

const xGrid = ref<VxeGridInstance>();
const editLichSuTuanDuongRef = ref();

const state = reactive({
  queryParams: {
    employee: undefined,
    fromDate: new Date(new Date().getFullYear(), new Date().getMonth(), 1), // tuần đầu tháng này
    toDate: new Date(new Date().getFullYear(), new Date().getMonth() + 1, 0), // tuần cuối tháng này
    shift: undefined,
  },
  employeeList: [
    { id: 1, name: '[cuonglv]Lê Văn Cường' },
    { id: 2, name: '[nhattv] Trần Văn Nhật' },
    { id: 3, name: '[tuanha] Hoàng Anh Tuấn' },
  ],
  shiftList: [
    { id: 1, name: 'Ca 1' },
    { id: 2, name: 'Ca 2' },
    { id: 3, name: 'Ca 3' },
  ],
  title: '',
  operateType: 'add',
  data: [],
  selectedWeek: '' as string,
  weekOptions: [] as Array<{label: string, value: string}>,
  hasChange: false,
});

const options = useVxeTable<any>(
  {
    id: 'lichSuTuanDuong',
    name: 'LichSuTuanDuong',
    columns: [
      { field: 'week', title: $t('lz.label.base.week'), minWidth: 70, showOverflow: 'tooltip', sortable: true },
      { field: 'shift', title: $t('lz.entity.tms.shift'), minWidth: 100, showOverflow: 'tooltip', sortable: true },
      { field: 'date', title: $t('lz.label.base.day'), minWidth: 120, showOverflow: 'tooltip', sortable: true },
      { field: 'employees', title: $t('lz.entity.tms.employee'), minWidth: 400, showOverflow: 'tooltip', slots: { default: 'employee_cell' } },
      { title: $t('lz.label.base.action'), fixed: 'right', width: 100, showOverflow: true, slots: { default: 'row_buttons' } },
    ],
  },
  {
    proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
    toolbarConfig: { export: false },
    rowConfig: { height: 40 },
  }
);

// Tạo danh sách tuần duy nhất từ dữ liệu mẫu
function getAllWeeksFromData(data: any[]) {
  const weeks = Array.from(new Set(data.map(item => item.week)));
  return weeks.map(week => ({ label: `Tuần ${week}`, value: week }));
}

const mockData = [
  { id: 1, week: '24/2025', shift: 'Ca 1', date: '03/06/2025', employees: [ { id: 1, name: '[cuonglv]Lê Văn Cường' }, { id: 2, name: '[nhattv] Trần Văn Nhật' } ] },
  { id: 2, week: '24/2025', shift: 'Ca 2', date: '03/06/2025', employees: [ { id: 3, name: '[cuonglv]Lê Văn Cường' } ] },
  { id: 3, week: '24/2025', shift: 'Ca 3', date: '03/06/2025', employees: [ { id: 1, name: '[tuanha] Hoàng Anh Tuấn' } ] },
  { id: 1, week: '24/2025', shift: 'Ca 1', date: '04/06/2025', employees: [ { id: 1, name: '[cuonglv]Lê Văn Cường' }, { id: 2, name: '[nhattv] Trần Văn Nhật' } ] },
  { id: 2, week: '24/2025', shift: 'Ca 2', date: '04/06/2025', employees: [ { id: 3, name: '[cuonglv]Lê Văn Cường' } ] },
  { id: 3, week: '24/2025', shift: 'Ca 3', date: '04/06/2025', employees: [ { id: 1, name: '[tuanha] Hoàng Anh Tuấn' } ] },
  { id: 1, week: '24/2025', shift: 'Ca 1', date: '05/06/2025', employees: [ { id: 1, name: '[cuonglv]Lê Văn Cường' }, { id: 2, name: '[nhattv] Trần Văn Nhật' } ] },
  { id: 2, week: '24/2025', shift: 'Ca 2', date: '05/06/2025', employees: [ { id: 3, name: '[cuonglv]Lê Văn Cường' } ] },
  { id: 3, week: '24/2025', shift: 'Ca 3', date: '05/06/2025', employees: [ { id: 1, name: '[tuanha] Hoàng Anh Tuấn' } ] },
  { id: 4, week: '25/2025', shift: 'Ca 1', date: '10/06/2025', employees: [ { id: 2, name: '[nhattv] Trần Văn Nhật' } ] },
  { id: 5, week: '25/2025', shift: 'Ca 2', date: '10/06/2025', employees: [ { id: 1, name: '[cuonglv]Lê Văn Cường' }, { id: 3, name: '[tuanha] Hoàng Anh Tuấn' } ] },
  { id: 6, week: '25/2025', shift: 'Ca 3', date: '10/06/2025', employees: [ { id: 2, name: '[nhattv] Trần Văn Nhật' } ] },
  { id: 7, week: '26/2025', shift: 'Ca 1', date: '17/06/2025', employees: [ { id: 1, name: '[cuonglv]Lê Văn Cường' } ] },
  { id: 8, week: '26/2025', shift: 'Ca 2', date: '17/06/2025', employees: [ { id: 2, name: '[nhattv] Trần Văn Nhật' }, { id: 3, name: '[tuanha] Hoàng Anh Tuấn' } ] },
  { id: 9, week: '27/2025', shift: 'Ca 1', date: '24/06/2025', employees: [ { id: 3, name: '[tuanha] Hoàng Anh Tuấn' } ] },
  { id: 10, week: '27/2025', shift: 'Ca 3', date: '24/06/2025', employees: [ { id: 1, name: '[cuonglv]Lê Văn Cường' } ] },
];
window.mockData = mockData;

state.weekOptions = getAllWeeksFromData(mockData);
state.selectedWeek = state.weekOptions.length > 0 ? state.weekOptions[0].value : '';

const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
  // Lọc dữ liệu mock theo điều kiện tìm kiếm
  let filtered = mockData.filter(item => {
    let match = true;
    if (state.selectedWeek) {
      match = match && item.week === state.selectedWeek;
    }
    if (state.queryParams.employee) {
      match = match && item.employees.some(e => e.id === state.queryParams.employee);
    }
    if (state.queryParams.shift) {
      match = match && item.shift === state.shiftList.find(s => s.id === state.queryParams.shift)?.name;
    }
    // if (state.queryParams.fromDate) {
    //   match = match && new Date(item.date) >= new Date(state.queryParams.fromDate);
    // }
    // if (state.queryParams.toDate) {
    //   match = match && new Date(item.date) <= new Date(state.queryParams.toDate);
    // }
    return match;
  });
  // Cập nhật lại danh sách tuần nếu có bản ghi mới
  state.weekOptions = getAllWeeksFromData(mockData);
  return { data: { result: { records: filtered, total: filtered.length } } };
};

const handleQuery = async () => {
  options.loading = true;
  await xGrid.value?.commitProxy('reload') ;
  options.loading = false;
};

const resetQuery = async () => {
  state.queryParams.employee = undefined;
  state.queryParams.fromDate = undefined;
  state.queryParams.toDate = undefined;
  state.queryParams.shift = undefined;
  await xGrid.value?.commitProxy('reload');
};

const handleEdit = (row: any) => {
  state.title = $t('lz.label.tmsPatrolSchedule.editTitle');
  state.operateType = 'edit';
  editLichSuTuanDuongRef.value?.openDialog(row);
};

const handleDelete = (row: any) => {
  // Xử lý xóa dữ liệu mock (chỉ demo)
  const idx = mockData.findIndex(item => item.id === row.id);
  if (idx !== -1) {
    mockData.splice(idx, 1);
    handleQuery();
  }
};

const handleAdd = () => {
  state.title = $t('lz.label.tmsPatrolSchedule.registerTitle');
  state.operateType = 'add';
  editLichSuTuanDuongRef.value?.openDialog({
    week: state.selectedWeek,
    date: '',
    shift: '',
    employees: [],
    multi: true, // truyền props để modal biết là đăng ký nhiều ngày/ca
  });
};

function cloneEmployee(employee: any) {
  return { ...employee };
}

function onDragEnd(evt: any, targetRow: any) {
  state.hasChange = true;

  const draggedEmpId = evt.item.__vueParentComponent.props.element.id;
  // Nếu nhân sự đã tồn tại trong ca-ngày này trước khi kéo, hủy thao tác
  const count = targetRow.employees.filter((e: any) => e.id === draggedEmpId).length;
  if (count > 1 || (count === 1 && evt.from !== evt.to)) {
    // Xóa nhân sự vừa thêm (undo drag)
    targetRow.employees = targetRow.employees.filter((e: any, idx: number, arr: any[]) => e.id !== draggedEmpId || arr.findIndex((x: any) => x.id === draggedEmpId) === idx);
    return;
  }
  // Đảm bảo không trùng nhân viên trong cùng 1 ca
  targetRow.employees = targetRow.employees.reduce((acc: any[], emp: any) => {
    if (!acc.find((e: any) => e.id === emp.id)) acc.push(emp);
    return acc;
  }, []);
  // Xóa nhân viên khỏi ca nguồn nếu khác ca đích
  if (evt.from !== evt.to) {
    const fromRow = findRowByEmployeeId(draggedEmpId, targetRow);
    if (fromRow && fromRow !== targetRow) {
      fromRow.employees = fromRow.employees.filter((e: any) => e.id !== draggedEmpId);
    }
  }
}

function findRowByEmployeeId(empId: number, exceptRow: any) {
  // Tìm ca chứa nhân viên này, trừ ca đích
  return mockData.find(row => row !== exceptRow && row.employees.some(e => e.id === empId));
}

function handleSaveChange() {
  // Ở đây có thể gọi API lưu hoặc cập nhật lại dữ liệu thực tế
  state.hasChange = false;
  // Có thể show thông báo thành công
}

function canMoveEmployee(evt: any, targetRow: any) {
  const draggedEmpId = evt.draggedContext.element.id;
  // Nếu đã có nhân sự này trong ca-ngày, không cho phép kéo
  if (targetRow.employees.some((e: any) => e.id === draggedEmpId)) {
    return false;
  }
  return true;
}
</script>

<style scoped>
.tms-lichsu-tuanduong-container {
  padding: 10px;
}
</style>
