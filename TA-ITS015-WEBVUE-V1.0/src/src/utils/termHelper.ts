import { getAPI } from "./axios-utils";
import { SysConfigApi } from "../api-services/system";
import { terminologyiDb } from "../indexedDb/cfgSystem/terminologyiDb";

//refresh terminology  client and server
async function loadTerminology(lang: string) {
    await loadTerminologyServer(lang);
    await loadTerminologyClient(lang);
}

//refresh terminology client
async function loadTerminologyClient(lang: string, overwrite: boolean = true) {
    try {
        // check if language is already loaded
        const existing = await terminologyiDb.terminology.where('lang').equals(lang).first();

        // nếu existing và overwrite = false thì bỏ qua 
        if (existing && !overwrite) return;

        // load from api Client
        const client = await getAPI(SysConfigApi).apiSystemSysconfigLoadclienttermGet(lang);
        var clientFile = client.data;

        if (existing) {

            //update
            existing.value = JSON.stringify(clientFile);
            await terminologyiDb.terminology.put(existing);
        } else {
            //save
            terminologyiDb.terminology.add({
                value: JSON.stringify(clientFile),
                lang: lang
            });
        }
    } catch (error) {
        console.error('Error processing and saving language file to indexedDB:', error);
    }
}

//Refresh terminology server
async function loadTerminologyServer(lang: string) {
    //load language server
    const server = await getAPI(SysConfigApi).apiSystemSysconfigLoadservertermGet(lang);
}


export { loadTerminology, loadTerminologyClient, loadTerminologyServer };
