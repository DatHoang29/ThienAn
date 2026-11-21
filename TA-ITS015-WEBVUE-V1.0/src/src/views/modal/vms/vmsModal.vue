<!--
  Modal điều khiển VMS theo sự cố (incident).
  Toàn bộ UI điều khiển + preview + gợi ý thiết bị theo sự cố đã chuyển sang component dùng chung
  /@/components/vms/controlVms (mode="incident"). Modal này chỉ giữ vỏ dialog + nút Xác nhận/Hủy.
-->
<template>
  <div class="vms-modal-container">
    <!-- SonTH - 24/07/2026: append-to-body vì modal này được mở TỪ TRONG popup xử lý sự cố.
         Popup đó có draggable (đặt transform lên .el-dialog) nên tạo stacking context và neo cả
         position: fixed của phần tử con. Không teleport ra body thì modal này bị kẹt trong đó,
         đồng thời 18 select/tooltip của controlVms (teleport ra body) sẽ bị nó che. -->
    <el-dialog v-model="state.isShowDialog" class="vms-incident-dialog vms-control-dialog" append-to-body draggable overflow destroy-on-close width="880px">
      <!-- Tiêu đề dialog -->
      <template #header>
        <div style="color: #fff">
          <span>{{ $t('lz.label.vmsControl.title') }}</span>
        </div>
      </template>

      <!-- Điều khiển VMS dùng chung (mode sự cố). Panel gợi ý thiết bị nằm bên trong component. -->
      <controlVms ref="controlRef" mode="incident" @submit-success="onControlSuccess" />

      <!-- Nút Xác nhận/Hủy ở cha, gọi submit() của component con -->
      <template #footer>
          <el-button class="btn-default-content" :icon="RefreshLeft" style="margin-right: auto" @click="applyDefaultContent">{{ $t('lz.button.vms.defaultContent') }}</el-button>
          <el-button @click="cancel">{{ $t('lz.button.base.cancel') }}</el-button>
          <el-button type="primary" class="btn-confirm" :loading="isSubmitting" @click="handleSubmit">
            {{ $t('lz.button.base.confirm') }}<span v-if="selectedCount" class="confirm-count">{{ selectedCount }}</span>
          </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script lang="ts" setup name="vmsModal">
import { reactive, ref, nextTick, computed } from 'vue';
import { RefreshLeft } from '@element-plus/icons-vue';
import { useI18n } from 'vue-i18n';
// [REFACTOR] Component điều khiển VMS dùng chung (control + preview + gợi ý thiết bị theo sự cố)
import controlVms from '/@/components/vms/controlVms/index.vue';


const { t: $t } = useI18n();

// Cha lắng nghe 'onMeSubmitSuccess' để refresh dữ liệu sau khi điều khiển thành công
const emits = defineEmits(['activated', 'closed', 'closeOthers', 'closeAll', 'onMeSubmitSuccess']);

// ref tới component điều khiển dùng chung
const controlRef = ref();
// Số thiết bị đang chọn trong form điều khiển (hiện trên nút Xác nhận)
const selectedCount = computed(() => controlRef.value?.selectedCount ?? 0);

const state = reactive<any>({
  isShowDialog: false, // hiển thị dialog
});

//============ #SECTION: DIALOG METHODS =============================

/**
 * Mở dialog điều khiển theo sự cố.
 * row: { incidentInfo, eventTypeCode, workExecuteId, workExecuteRemark, actionType }
 */
const openDialog = async (row?: any) => {
  state.isShowDialog = true;

  // Uỷ quyền toàn bộ điều khiển cho controlVms (mode incident)
  await nextTick();
  try {
    await controlRef.value?.open({
    incidentInfo: row?.incidentInfo,
    incidentId: row?.incidentInfo?.id,
    eventTypeCode: row?.eventTypeCode,
    workExecuteId: row?.workExecuteId,
    workExecuteRemark: row?.workExecuteRemark,
    actionType: row?.actionType,
  });   // [SỬA] thêm await + try/catch
  } catch (e) {
    console.error('[vmsModal] open controlVms failed:', e);
  }
};


const isSubmitting = ref(false); // Trạng thái submit để disable nút xác nhận

// Hàm xử lý submit: gọi submit của component con controlVms, sau đó đóng panel.
const handleSubmit = async () => {
    if (isSubmitting.value) return; // Tránh submit nhiều lần
    isSubmitting.value = true;
    try {
        await controlRef.value?.submit();
        // KHÔNG đóng panel ở đây: việc đóng do controlVms phát 'submit-success' (onControlSuccess) đảm nhiệm.
        // Nếu đóng ngay sẽ huỷ modal cảnh báo độ ưu tiên (destroy-on-close) trước khi người dùng kịp xác nhận.
    } catch (error) {
        console.error('Error submitting control:', error);
    } finally {
        isSubmitting.value = false;
    }
}

// Đóng dialog
const closeDialog = () => {
  state.isShowDialog = false;
};
const cancel = () => {
  closeDialog();
};

// Component con điều khiển thành công -> báo cha refresh + đóng dialog
const onControlSuccess = () => {
  emits('onMeSubmitSuccess');
  closeDialog();
};

// Gọi component con áp dụng nội dung mặc định (tab control)
const applyDefaultContent = () => controlRef.value?.applyDefaultContent(); 


defineExpose({ openDialog });
</script>

<!-- IMPORTANCE: Do not use scoped -->
<style lang="scss">
.vms-modal-container {
  .el-dialog__body {
    overflow-y: scroll !important;
  }
}

/* Footer dạng flex để nút "Nội dung mặc định" (margin-right:auto) đẩy sang lề trái,
   tách khỏi nhóm Hủy/Xác nhận bên phải. Class đặt trên .el-dialog nên áp dụng kể cả khi teleport ra body. */
.vms-incident-dialog {
  .el-dialog__footer {
    display: flex;
    align-items: center;
  }
}

/* Thân + footer hộp thoại tông tối để khớp form controlVms (class riêng, không đụng modal phân xử sự cố
   vốn cũng dùng class vms-incident-dialog). */
.el-dialog.vms-control-dialog {
  --el-dialog-bg-color: #13233b;

  .el-dialog__footer {
    border-top: 1px solid rgba(96, 140, 255, 0.18);
  }

  @import '/@/components/vms/controlVms/footerButtons.scss';
}
</style>
