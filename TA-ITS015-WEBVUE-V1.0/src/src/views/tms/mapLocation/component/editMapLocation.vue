<template>
  <el-dialog :visible.sync="visible" :title="$t('lz.label.tmsMapLocation.markerDetails')" width="30%">
    <el-form :model="marker" label-width="100px">
      <el-form-item label="ID">
        <el-input v-model="marker.id" disabled />
      </el-form-item>
      <el-form-item :label="$t('lz.entity.tms.latitude')">
        <el-input-number v-model="marker.lat" :min="-90" :max="90" />
      </el-form-item>
      <el-form-item :label="$t('lz.entity.tms.longitude')">
        <el-input-number v-model="marker.lng" :min="-180" :max="180" />
      </el-form-item>
    </el-form>
    <div slot="footer" class="dialog-footer">
      <el-button @click="removeMarker">{{ $t('lz.button.base.delete') }}</el-button>
      <el-button type="primary" @click="updateMarker">{{ $t('lz.button.base.update') }}</el-button>
    </div>
  </el-dialog>
</template>

<script lang="ts" setup name="tmsEditmapLocation">
import { useI18n } from 'vue-i18n';


import { defineProps, defineEmits } from 'vue';
import { ElDialog, ElForm, ElFormItem, ElInput, ElInputNumber, ElButton } from 'element-plus';

const { t: $t } = useI18n();

const props = defineProps({
  marker: {
    type: Object,
    required: true
  },
  visible: {
    type: Boolean,
    required: true
  }
});

const emit = defineEmits(['update:visible', 'update:marker']);

const removeMarker = () => {
  emit('update:marker', { id: props.marker.id, action: 'remove' });
  emit('update:visible', false);
};

const updateMarker = () => {
  emit('update:marker', { ...props.marker, action: 'update' });
  emit('update:visible', false);
};
</script>

<style scoped>
.dialog-footer {
  text-align: right;
}
</style>
