<template>
 <!-- Modal for editing/removing markers and polygons -->
 <el-dialog  :editDialogVisible="props.editDialogVisible" v-model="dialogVisible" 
            :title="$t('lz.label.tmsMapLocation.editFeature')">
        <el-form :model="props.editFeature">
          <el-form-item label="Feature ID">
            <el-input v-model="props.editFeature.id" disabled></el-input>
          </el-form-item>
          <el-form-item v-if="props.editFeature.type === 'Polygon'" :label="$t('lz.label.tmsMapLocation.strokeColor')">
            <el-color-picker v-model="props.editFeature.strokeColor" />
          </el-form-item>
          <el-form-item v-if="props.editFeature.type === 'Polygon'" :label="$t('lz.label.tmsMapLocation.strokeWidth')">
            <el-input-number v-model="props.editFeature.strokeWidth" />
          </el-form-item>
          <el-form-item v-if="props.editFeature.type === 'Polygon'" :label="$t('lz.label.tmsMapLocation.strokeOpacity')">
            <el-slider v-model="props.editFeature.strokeOpacity" :min="0" :max="1" step="0.1" />
          </el-form-item>
          <el-form-item v-if="props.editFeature.type === 'Polygon'" :label="$t('lz.label.tmsMapLocation.fillOpacity')">
            <el-slider v-model="props.editFeature.fillOpacity" :min="0" :max="1" step="0.1" />
          </el-form-item>
          <el-form-item v-if="props.editFeature.type === 'Point'" :label="$t('lz.label.base.status')">
            <el-select v-model="props.editFeature.status" 
              :placeholder="$t('lz.label.base.pleaseSelect', {input:$t('lz.label.base.status')})">
              <el-option :label="$t('lz.label.base.active')" value="active"></el-option>
              <el-option :label="$t('lz.label.base.inactive')" value="inactive"></el-option>
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-button type="danger" @click="removeFeature"> {{ $t('lz.button.tmsMaplocation.remove') }}</el-button>
            <el-button type="primary" @click="saveEdit">{{ $t('lz.button.base.save') }}</el-button>
          </el-form-item>
        </el-form>
      </el-dialog>

</template>
<script lang="ts" setup>

import L from 'leaflet';

import { ref } from 'vue';

const props = defineProps({
  // geoJsonData: {
  //   type: Object,
  //   required: true
  // },
  editDialogVisible:{
    type:Boolean,    
  },
  editFeature: {
    type:Object,
    default:{
        id: '',
        status: '',
        strokeColor: '#000000',
        strokeWidth: 2,
        strokeOpacity: 1,
        fillOpacity: 1,
        type: '',
        layer: null as L.Layer | null,
        }
  },
  // currentCoordinates: null as any,
  // markersLayer: L.layerGroup() as L.LayerGroup<L.Marker>,
  // polygonsLayer: L.layerGroup() as L.LayerGroup<L.Polygon>,
});

const dialogVisible = ref(false);
const showModal = () => {
  //console.log("show modal")
      dialogVisible.value = props.editDialogVisible;
    };

const emit = defineEmits([
  'save-edit-modal',
  'remove-feature-modal'
])

// const emit = defineEmits<{
//   (e: 'render-geo-json'): void;
//   (e: 'update:editDialogVisible', value: boolean): void
// }>();

// const openEditDialog = () => {
//   emit('update:editDialogVisible', true);
// };

// const closeEditDialog = () => {
//   emit('update:editDialogVisible', false);
// };

/**
 * Saves edits made to a feature
 */
 const saveEdit = () => {
  emit('save-edit-modal')
  //const { id, type, layer, ...propsOther } = props.editFeature;
  // const feature = props.geoJsonData.features.find((f:any) => f.properties?.id === id);
  // if (feature) {
  //   Object.assign(feature.properties, props);
    
  //   // If we have current coordinates from editing, use those
  //   if (props.currentCoordinates && feature.geometry.type === 'Polygon') {
  //     feature.geometry.coordinates = props.currentCoordinates;
  //   }
    
  //   if (type === 'Polygon') {
  //     const polygonLayer = layer as L.Polygon;
  //     polygonLayer.setStyle({
  //       color: propsOther.strokeColor,
  //       weight: propsOther.strokeWidth,
  //       opacity: propsOther.strokeOpacity,
  //       fillOpacity: propsOther.fillOpacity,
  //     });
  //   }
  //   if (type === 'Point') {
  //     const marker = layer as L.Marker;
  //     const status = propsOther.status;
  //   }

  //   // Close modal, set editDialogVisible to false
  //   closeEditDialog();

  //   emit('render-geo-json')
    //renderGeoJson();
  }


/**
 * Removes a feature from the map and GeoJSON data
 */
const removeFeature = () => {
  emit('remove-feature-modal')
  // const { layer, id, type } = props.editFeature;

  // if (type === 'Polygon') {
  //   props.polygonsLayer.removeLayer(layer as L.Polygon);
  // } else if (type === 'Point') {
  //   props.markersLayer.removeLayer(layer as L.Marker);
  // }

  // props.geoJsonData.features = props.geoJsonData.features.filter((feature:any) => feature.properties.id !== id);

  //  // Close modal, set editDialogVisible to false
  //  closeEditDialog();
  // emit('render-geo-json')
  //renderGeoJson();
};

// const modalEditRef = ref(null)
defineExpose({
  showModal
});
</script>
<style></style>