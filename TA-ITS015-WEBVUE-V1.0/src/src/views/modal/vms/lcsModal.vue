<!--
  Modal điều khiển LCS theo sự cố (incident).
  Song song với vmsModal.vue (VMS incident) để đảm bảo LCS incident tương tự VMS incident.
  Toàn bộ UI điều khiển + preview + gợi ý thiết bị theo sự cố dùng chung component
  /@/components/vms/controlVms với mode="incident" và device-type="lcs".
  LCS chỉ hỗ trợ điều khiển bằng hình ảnh (Image) -> actionType mặc định = Image.
  Modal này chỉ giữ vỏ dialog + nút Xác nhận/Hủy (giống vmsModal).
-->
<template>
  <div class="lcs-modal-container">
    <!-- append-to-body vì modal này được mở TỪ TRONG popup xử lý sự cố (draggable tạo stacking context).
         Không teleport ra body thì modal + các select/tooltip của controlVms (teleport ra body) sẽ bị che. -->
    <el-dialog v-model="state.isShowDialog" class="lcs-incident-dialog vms-control-dialog" append-to-body draggable overflow destroy-on-close width="800px">
      <!-- Tiêu đề dialog -->
      <template #header>
        <div style="color: #fff">
          <span>{{ $t('lz.label.lcsControl.title') }}</span>
        </div>
      </template>

      <!-- Điều khiển LCS dùng chung (mode sự cố, deviceType = LCS). Panel gợi ý thiết bị nằm bên trong component. -->
      <controlVms ref="controlRef" mode="incident" :device-type="VmsDeviceType.Lcs" @submit-success="onControlSuccess" />

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

<script lang="ts" setup name="lcsModal">
import { reactive, ref, nextTick, computed } from 'vue';
import { RefreshLeft } from '@element-plus/icons-vue';
import { useI18n } from 'vue-i18n';
import { VmsActionType, VmsDeviceType } from '/@/types/enums/vmsEnum';
// Component điều khiển dùng chung (control + preview + gợi ý thiết bị theo sự cố)
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
 * Mở dialog điều khiển LCS theo sự cố.
 * row: { incidentInfo, eventTypeCode, workExecuteId, workExecuteRemark, actionType }
 * LCS chỉ hỗ trợ điều khiển bằng hình ảnh -> actionType mặc định = Image.
 */
const openDialog = async (row?: any) => {
  state.isShowDialog = true;

  // Uỷ quyền toàn bộ điều khiển cho controlVms (mode incident, deviceType = LCS)
  await nextTick();
  try {
    await controlRef.value?.open({
      incidentInfo: row?.incidentInfo,
      incidentId: row?.incidentInfo?.id,
      eventTypeCode: row?.eventTypeCode,
      workExecuteId: row?.workExecuteId,
      workExecuteRemark: row?.workExecuteRemark,
      actionType: row?.actionType ?? VmsActionType.Image,
    });
  } catch (e) {
    console.error('[lcsModal] open controlVms failed:', e);
  }
};

const isSubmitting = ref(false); // Trạng thái submit để disable nút xác nhận

// Hàm xử lý submit: gọi submit của component con controlVms, sau đó đóng panel.
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
};

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
.lcs-modal-container {
  .el-dialog__body {
    overflow-y: scroll !important;
  }
}

/* Footer dạng flex để nút "Nội dung mặc định" (margin-right:auto) đẩy sang lề trái,
   tách khỏi nhóm Hủy/Xác nhận bên phải. Class đặt trên .el-dialog nên áp dụng kể cả khi teleport ra body. */
.lcs-incident-dialog {
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
