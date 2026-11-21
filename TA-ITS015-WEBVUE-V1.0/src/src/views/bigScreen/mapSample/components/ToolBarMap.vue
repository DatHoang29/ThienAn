<template>
  <div class="map-controls">
    <el-row class="mb5 is-justify-space-between"  >    <!--:style="{backgroundColor: 'white', border:'1px solid red',  padding: '5px' }"-->
        <el-col lass="mb5" :xs="7" :sm="7" :md="7" :lg="7" :xl="7">
          <el-form-item prop="mapId">
              <el-select 
                v-model="queryParams.mapId" 
                filterable 
                class="w200" 
                @change="handleMapChange"             
              >
                <el-option 
                  v-for="item in mapList" 
                  :key="item.id" 
                  :label="item.name" 
                  :value="item.id" 
                />
              </el-select>
           </el-form-item>
        </el-col>        
    </el-row>
  </div>    
</template>
<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { getAPI } from '/@/utils/axios-utils'
import { TmsMapApi, MapOutput } from '/@/api-services/tms'


const emit = defineEmits(
    ['map-change']
)

const mapList = ref<MapOutput[]>([])

// variable for select map
const queryParams = reactive({
    mapId: undefined,
})


const props = defineProps({
    geoJsonData: [] as any,
    selectedMapId: '' as any
})

const handleMapChange = () => {
    emit('map-change', queryParams.mapId)
}

const fetchMapList = async () => {
    try {
        const response = await getAPI(TmsMapApi).apiTmsTmsmapListGet()
        mapList.value = response?.data?.result || []
        //console.log("fetchMapList - mapList.value", mapList.value)
    } 
    catch (error) {
        console.error('Error fetching map list:', error)
    }
}

// Initialize filters on mount
onMounted(async() => {
    fetchMapList()

    // Assign selectedMapId
    queryParams.mapId = props.selectedMapId
})

</script>

<style scoped>
.map-controls {
  position: absolute;
  top: 50px;
  left: 10px;
  z-index: 2000;
  width: 500px;
  /* background: white; */
  padding: 10px;
  border-radius: 4px;
  /* box-shadow: 0 2px 5px rgba(0,0,0,0.2); */
}

.control-buttons {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-top: 10px;
}

.el-button {
  width: 35px;
  height: 35px;
  opacity: 0.5;
  transition: opacity 0.3s, transform 0.3s;
  margin-left: 0px!important;
}

.el-button:hover {
  opacity: 1;
  transform: scale(1.1);
}

.w100 {
  width: 200px;
}

.filter-content {
  max-height: 300px;
  overflow-y: auto;
}

.filter-group {
  margin-bottom: 10px;
}

.filter-items {
  margin-left: 20px;
  display: flex;
  flex-direction: column;
  gap: 5px;
}
</style> 