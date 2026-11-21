import { useUserInfo } from '/@/stores/userInfo';

const stores = useUserInfo();

// Used to convert the field code to name in the Table, example as follows:
/*
import { getDictDataItem as di, getDictDataList as dl } from '/@/utils/dict-utils';

<el-table-column prop="fieldName" label="description" width="140">
    <template #default="scope">
    <el-tag :type="di('dictionaryCode', scope.row.credentialsType)?.tagType"> [{{di("dictionaryCode", scope.row.credentialsType)?.code}}]{{di("dictionaryCode", scope.row.credentialsType)?.value}} </el-tag>
    </template>
</el-table-column>
*/

export function getDictDataItem(dicName: string, dicItemCode: any): any {
	return stores.getDictItemByCode(dicName, dicItemCode);
}

export function getDictValByLabel(dicName: string, dicItemCode: any): any {
	return stores.getDictValByLabel(dicName, dicItemCode);
}

export function getDictLabelByVal(dicName: string, dicItemCode: any): any {
	return stores.getDictLabelByVal(dicName, dicItemCode);
}

// Used for select control to get the dictionary list, example as follows:
/*
import { getDictDataItem as di, getDictDataList as dl } from '/@/utils/dict-utils';

<el-select clearable v-model="ruleForm.field" placeholder="Please select a document prompt">
    <el-option v-for="(item,index) in  dl('fieldCode')"  :key="index" :value="item.code" :label="`[${item.code}] ${item.value}`"></el-option>
</el-select>
*/

export function getDictType(dicName: string): any {
	return stores.dictList[dicName];
}

export function getDictDataList(dicName: string): any {
	return stores.getDictDatasByCode(dicName);
}

// Get numeric type
export function getDictDataListInt(dicName: string): any {
	return stores.getDictIntDatasByCode(dicName);
}
