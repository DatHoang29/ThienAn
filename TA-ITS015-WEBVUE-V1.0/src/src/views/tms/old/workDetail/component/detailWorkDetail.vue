<template>
  <div class="detail-work-detail-container">
    <el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="800">
      <template #header>
        <div style="color: #fff; display: flex; align-items: center;">
          <el-icon size="16" style="margin-right: 5px;">
            <ele-View />
          </el-icon>
          <span>{{ props.title }}</span>
        </div>
      </template>

      <table class="table-data w100" style="margin-bottom: 10px;">
        <tr class="title">
          <th colspan="4">
            <p>{{ $t('lz.entity.base.code') }}: {{ state.dataList.code }}</p>
          </th>
        </tr>
        <tr class="alt">
          <th>{{ $t('lz.entity.base.name') }}</th>
          <td>{{ state.dataList.name }}</td>
        <th>{{ $t('lz.entity.tmsWorkDetail.workMode') }}</th>
          <td>
            {{ state.workModeList.find((item: any) => item.code === state.dataList.workMode)?.name }}
          </td>

        </tr>
        <tr>
          <th>{{ $t('lz.entity.tmsWorkDetail.workType') }}</th>
          <td>{{ state.workTypeList.find((item: any) => item.code === state.dataList.workType)?.name }}</td>
          <th>{{ $t('lz.entity.tms.controlType') }}</th>
          <td>{{ props.controlEquipTypeList.find((item: any) => item.code === state.dataList.controlType)?.name }}</td>
          
        </tr>
        <tr class="alt">
          <th>{{ $t('lz.entity.base.remark') }}</th>
          <td>{{ state.dataList.remark }}</td>
          <th>{{ $t('lz.entity.base.status') }}</th>
          <td>
            <el-tag v-if="state.dataList.status === 1" type="success">{{ $t('lz.label.base.enable') }}</el-tag>
            <el-tag v-else type="danger">{{ $t('lz.label.base.disable') }}</el-tag>
          </td>
        </tr>
      </table>

      <el-card shadow="hover" class="work-contact-card" style="width: 100%; margin-bottom: 10px;">
        <template #header>
          <h3> {{ $t('lz.label.tmsWorkDetail.addNewContact') }} </h3>
        </template>
        <el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto" style="padding-top: 10px;">
          <el-row :gutter="10">
            <el-col :span="24" class="mb20">
              <el-form-item
                :label="$t('lz.entity.tms.workContactId')"
                prop="workContactId"
                :rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.tms.workContactId')}), trigger: 'change' }]"
              >
                <el-select 
                  v-model="state.ruleForm.workContactId" 
                  :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tms.workContactId')})"  
                  filterable 
                  clearable 
                  class="w100" 
                  allow-create 
                >
                  <el-option 
                    v-for="item in activeWorkContactList" 
                    :key="item.id" 
                    :label="item.name" 
                    :value="item.id" 
                  />
                </el-select>
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="10">
            <el-col :span="24" class="mb20">
              <el-form-item :label="$t('lz.entity.base.phone')">
                <el-select 
                  v-model="phoneWorkContactId" 
                  :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.base.phone')})"  
                  filterable 
                  clearable 
                  class="w100" 
                  allow-create 
                  :disabled="isWorkContactIdExists"
                >
                  <el-option 
                    v-for="item in activeWorkContactList?.filter((item: any) => item.phone)" 
                    :key="item.id" 
                    :label="item.phone" 
                    :value="item.phone" 
                  />
                </el-select>
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="10">
            <el-col :span="24" class="mb20">
              <el-form-item :label="$t('lz.entity.base.address')">
                <el-select 
                  v-model="addressWorkContactId" 
                  :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.base.address')})"  
                  filterable 
                  clearable 
                  class="w100" 
                  allow-create 
                  :disabled="isWorkContactIdExists"
                >
                  <el-option 
                    v-for="item in activeWorkContactList?.filter((item: any) => item.address)" 
                    :key="item.id" 
                    :label="item.address" 
                    :value="item.address"  
                  />
                </el-select>
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="10">
            <el-col :span="24" class="mb20">
              <el-form-item
                :label="$t('lz.entity.base.orderNo')"
                prop="orderNo"
                :rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.base.orderNo')}), trigger: 'change' }]"
              >
                <el-input-number v-model="state.ruleForm.orderNo" :min="1" :value-on-clear="1" class="w100" />
              </el-form-item>
            </el-col>
          </el-row>
          <el-button type="primary" @click="submit"><el-icon><Plus /></el-icon>{{ $t('lz.button.base.add') }}</el-button>

        
        </el-form>
      </el-card>
      <el-card>
        <template #header >
          <div style="display: flex; flex-direction: row; justify-content: space-between">
            <h3 >{{ $t('lz.label.tmsTrafficPlanWorkDetail.contactList') }} </h3>
            <el-button type="primary" @click="handleQuery"><el-icon><Refresh /></el-icon>{{ $t('lz.button.base.refresh') }}</el-button>
          </div>
          
        </template>
        <div class="grid-container">
          <vxe-grid
            ref="xGrid"
            class="xGrid-style"
            v-bind="options"
            v-on="gridEvents"
          >
            <template #empty>
              <!-- <el-empty :image-size="100" /> -->
            </template>
            <template #orderNo_edit="{ row }">
              <vxe-input
                v-model="row.orderNo"
                type="number"
                :min="0"
                :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.base.orderNo')})"             
                @blur="validateOrderNo(row)"
                @change="updateOrderNo(row)"
              />
            </template>
            <template #row_buttons="{ row }">
              <el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
                <el-button
                  icon="ele-Edit"
                  text
                  type="primary"
                  @click="handleEdit(row)"
                />
              </el-tooltip>
              <el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
                <el-button
                  icon="ele-Delete"
                  text
                  type="danger"
                  v-auth="'tmsworkdetail:delete'"
                  @click="handleDelete(row)"
                />
              </el-tooltip>
            </template>
          
          </vxe-grid>
        </div>
        
      </el-card>
      <template #footer>
          <span class="dialog-footer" style="float:right; padding-bottom:15px;">
            <el-button type="" @click="state.isShowDialog = false">{{ $t('lz.button.base.close') }}</el-button>
          </span>
        </template>
    </el-dialog>
  </div>  
</template>

<script lang="ts" setup>
// Import libraries
import { reactive, computed, onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Refresh, Plus } from '@element-plus/icons-vue';

// Import API and models
import { SysOrgApi } from '/@/api-services/system';
import { SysConfigDataApi } from '/@/api-services/cfgSystem';
import { TmsWorkDetailContactApi, TmsWorkContactApi } from '/@/api-services/tms';
import { PageWorkDetailOutput, PageTmsWorkDetailContactOutput, UpdateTmsWorkDetailContactInput, AddTmsWorkContactInput } from '/@/api-services/tms';

import { OperateType } from '/@/types/constants';
// Declare params
interface Props {
  operateType: OperateType;
  title: string;
  controlEquipTypeList: any;

}

const xGrid = ref<VxeGridInstance>();
const emits = defineEmits(['handleQueryApi']);
const ruleFormRef = ref();
const { t: $t } = useI18n();
const props = defineProps<Props>();

const state = reactive({
  Phone: "",
  editingRow: null,
  type: "3",
  WorkContactName: "",
  isShowDialog: false,
  workContactList: [] as any,
  workDetailContactList: [] as any,
  dataOrgList: [] as any,
  contactTypeList: [] as any,
  workModeList: [] as any,
  workTypeList: [] as any,
  // controlEquipTypeList: [] as any,
  showBatchDelete: false,
  ruleForm: {} as UpdateTmsWorkDetailContactInput,
  dataList: {} as PageWorkDetailOutput,
  showWorkDetailIdRow: false,
  isEditMode: false,
});

const options = useVxeTable<PageTmsWorkDetailContactOutput>(
  {
    id: 'tmsWorkDetailContact',
    name: 'tmsWorkDetailContact',
    columns: [
      {
        field: 'orderNo',
        title: $t('lz.entity.base.orderNo'),
        minWidth: 30,
        showOverflow: 'tooltip',
        fixed: 'left',
        slots: { edit: 'orderNo_edit' },
      },
      {
        field: 'workContactId',
        title: $t('lz.entity.base.name'),
        minWidth: 120,
        formatter: ({ cellValue }: any) =>
          state.workContactList.find((u: any) => u.id == cellValue)?.name,
        showOverflow: 'tooltip',
      },
      {
        field: 'workContactId',
        title: $t('lz.entity.base.phone'),
        minWidth: 100,
        formatter: ({ cellValue }: any) =>
          state.workContactList.find((u: any) => u.id == cellValue)?.phone,
        showOverflow: 'tooltip',
      },
      {
        field: 'workContactId',
        title: $t('lz.entity.base.address'),
        minWidth: 100,
        formatter: ({ cellValue }: any) =>
          state.workContactList.find((u: any) => u.id == cellValue)?.address,
        showOverflow: 'tooltip',
      },
     
      {
        title: $t('lz.label.base.action'),
        fixed: 'right',
        width: 80,
        showOverflow: true,
        slots: { default: 'row_buttons' },
      },
    ],
  },
  {
    proxyConfig: {
      autoLoad: true,
      ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) }
    },
    pagerConfig: {
      enabled: false
    },
    toolbarConfig: { export: false },
    rowConfig: {
      useKey: true,
      // drag: true
    },
    columnConfig: {
      useKey: true
    },
  
  }
);

// Validate order number
const validateOrderNo = (row: any) => {
  if (isNaN(row.orderNo) || row.orderNo < 0) {
    ElMessage.error($t('lz.validation.base.orderNoNumber'));
    row.orderNo = ''; // Reset the orderNo if invalid
  }
};

const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
  state.workDetailContactList =  await getAPI(TmsWorkDetailContactApi).apiTmsTmsworkdetailcontactListGet(1, state.ruleForm.workDetailId);
  return state.workDetailContactList;
};

// Page initialization
onMounted(async () => {
  const response = await getAPI(TmsWorkContactApi).apiTmsTmsworkcontactListGet();
  state.workContactList = response.data.result;
  const workdetail = await getAPI(SysOrgApi).apiSystemSysorgListGet("0");
  state.dataOrgList = workdetail.data.result;
  const contactType = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet('work_contact_type');
  state.contactTypeList = contactType.data.result;
  const workMode = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet('tms_work_mode');
  state.workModeList = workMode.data.result;
  const workType = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet('tms_work_type');
  state.workTypeList = workType.data.result;
  // const controlEquipType = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet('tms_control_device_type');
  // state.controlEquipTypeList = controlEquipType.data.result;
  await handleQuery();
});


// Open dialog
const openDialog = async (row: any) => {
  state.dataList = JSON.parse(JSON.stringify(row));
  state.ruleForm = {
    workDetailId: row.id,
    orderNo:1, // Thiết lập giá trị mặc định cho workDetailId
  };
  state.isShowDialog = true;
  await handleQuery();
};

// Query operation
const handleQuery = async () => {
  options.loading = true;
  await xGrid.value?.commitProxy('reload') ;
  options.loading = false;
  selectChangeEvent();
};

function getCheckedRecords() {
  return xGrid.value?.getCheckboxRecords() ?? [];
}

function selectChangeEvent() {
  state.showBatchDelete = getCheckedRecords().length > 0;
}

const gridEvents: VxeGridListeners<PageTmsWorkDetailContactOutput> = {

}


const GetDate = (): string => {
  const now = new Date();
  const year = now.getFullYear();
  const month = String(now.getMonth() + 1).padStart(2, '0'); // Tháng bắt đầu từ 0, cộng thêm 1
  const day = String(now.getDate()).padStart(2, '0');
  return `${year}${month}${day}`; // Trả về ngày tháng năm dưới dạng YYYYMMDD
};

// Danh sách thông tin liên hệ đang hoạt động (chỉ hiển thị liên hệ ở trạng thái Dùng vào các option)
const activeWorkContactList = computed(() => {
  return state.workContactList?.filter((item: any) => item.status === 1 || item.status === '1') ?? [];
});

const isWorkContactIdExists = computed(() => {
  return activeWorkContactList.value.some((item: any) => item.id === state.ruleForm.workContactId);
});
// - Begin - Add by KhoaND

const phoneWorkContactId = computed({
  get() {
    const selectedItem = state.workContactList.find(
      (item: any) => item.id === state.ruleForm.workContactId
    );
    return selectedItem && selectedItem.phone ? selectedItem.phone : null;
  },
  set(value) {
    const selectedItem = activeWorkContactList.value.find((item: any) => item.phone === value || item.id === value);
    state.ruleForm.workContactId = selectedItem && selectedItem.phone ? selectedItem.id : null;
  },
});

const addressWorkContactId = computed({
  get() {
    const selectedItem = state.workContactList.find(
      (item: any) => item.id === state.ruleForm.workContactId
    );
    return selectedItem && selectedItem.address ? selectedItem.address : null;
  },
  set(value) {
    const selectedItem = activeWorkContactList.value.find((item: any) => item.address === value || item.id === value);
    state.ruleForm.workContactId = selectedItem && selectedItem.address ? selectedItem.id : null;
  },
});

// - End - Add by KhoaND

const handleEdit = (row: any) => {
  state.isEditMode = true;
  state.ruleForm = {
    ...row,
    workDetailId: state.dataList.id
  };
  // Set other form fields based on the selected contact
  const selectedContact = state.workContactList.find((item: any) => item.id === row.workContactId);
  if (selectedContact) {
    state.Phone = selectedContact.phone || '';
    state.WorkContactName = selectedContact.name || '';
  }
};

const submit = async () => {
  ruleFormRef.value.validate(async (valid: boolean) => {
    if (!valid) return;

    try {
      if (state.isEditMode) {
        // Update existing record
        await getAPI(TmsWorkDetailContactApi).apiTmsTmsworkdetailcontactUpdatePost(state.ruleForm);
        ElMessage.success($t('lz.message.base.submitSuccess'));
      } else {
        // Your existing create logic
        const workContactExists = state.workContactList.some((item: any) => item.id === state.ruleForm.workContactId);
        if (!workContactExists) {
          const newWorkContact = {
            name: state.ruleForm.workContactId,
            phone: state.Phone,
            code: "WorkDetail" + GetDate(), // Cập nhật code với ngày tháng hiện tại
            type: state.type,
            status: 1
          } as AddTmsWorkContactInput;

          // Gửi yêu cầu tạo mới Work Contact
          await getAPI(TmsWorkContactApi).apiTmsTmsworkcontactAddPost(newWorkContact);
          const response = await getAPI(TmsWorkContactApi).apiTmsTmsworkcontactListGet();

          const findId = response.data.result?.find(item => item.name === newWorkContact.name);

          const newId = findId?.id;

          // Cập nhật ID mới vào state
          state.ruleForm.workContactId = newId;
        }
        await getAPI(TmsWorkDetailContactApi).apiTmsTmsworkdetailcontactAddPost(state.ruleForm);
        ElMessage.success($t('lz.message.base.createSuccess'));
      }

      // Reset form and refresh data
      state.isEditMode = false;
      state.ruleForm = { workDetailId: state.dataList.id, orderNo: 1 };
      await handleQuery();

    } catch (error) {
      ElMessage.error($t('lz.message.base.updateError'));
    }
  });
};

// Delete
const handleDelete = (row: any) => {
  const name = state.workContactList.find((item: any) => item.id === row.workContactId)?.name;
  ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: name }), $t('lz.message.base.warning'), {
    confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
    type: 'warning',
  })
    .then(async () => {
      await getAPI(TmsWorkDetailContactApi).apiTmsTmsworkdetailcontactDeletePost({ id: row.id });
      handleQuery();
      ElMessage.success($t('lz.message.base.deleteSuccess'));
    })
    .catch(() => { });
};

  // Update all edited OrderNos
  const updateOrderNo = async (row: any) => {
		const updateData = row
		await getAPI(TmsWorkDetailContactApi).apiTmsTmsworkdetailcontactUpdatePost(updateData);
		await handleQuery();
		ElMessage.success($t('lz.message.base.updateSuccess'));
		
	};

// Add event listener to update orderNo after editing

	// Export object
	defineExpose({ openDialog });
</script>

<style scoped>
.grid-container {
  padding: 5px;
  box-sizing: border-box;
  max-height: 100%;
  overflow: auto;
}

.xGrid-style {
  width: 100%;
  height: 100%;
  min-height: 300px; /* Adjust as necessary */
}
	.table-data {
		border-collapse: collapse;  
	}

	.table-data td, th {
		border-color: #E6E8EB !important;
		border-left: thin solid;
		border-top: thin solid;
		border-right: thin solid;
	}

	.table-data tr:last-child td, th {
		border-bottom: thin solid;
	}

	.table-data tr.title th {
		text-align: center;
		font-size: large;
	}

	.table-data tr.alt {
		background-color: #F0F2F5;
	}
</style>
