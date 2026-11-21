import { defineStore } from 'pinia';
import { getAPI } from '/@/utils/axios-utils';
import { TmsIncidentApi } from '/@/api-services/tms';

export const useIncidentMonitor = defineStore('incidentMonitor', {
    // Using reactive to react and share this global-instance for components.
    state: () => ({
        accountToken: null,
        broadcastObject: null,
        broadcastMessage: null,
        broadcastCode: null,
        broadcastDateTime: null,
        isShowIncidentPopup: false,
        selectedIncidentId: null,

        // Track if dialog is in collapsed state (showing as FAB)
        // false = dialog should be open, true = dialog should be collapsed to FAB
        isCollapsed: false,

        allIncidentByAccount: [] as any, // Default must be an empty array
        allAllowedActionByAccount: [] as any, // Default must be an empty array

        incidentDialogState: {
            isShowDialog: false,
            dialogTitle: null as string | null,
            isEditMode: false,
            incidentInfo: null
        }
    }),
    getters: {

    },
    actions: {
        setIncidentDialogState(isShowDialog: boolean, dialogTitle: string | null = null, isEditMode: boolean = false, incidentInfo: any = null) {
            this.incidentDialogState.isShowDialog = isShowDialog; // Show or hide dialog
            this.incidentDialogState.dialogTitle = isShowDialog ? dialogTitle : null;
            this.incidentDialogState.isEditMode = isShowDialog ? isEditMode : false;
            this.incidentDialogState.incidentInfo = isShowDialog ? incidentInfo : null;
        },
        // Check if the current user can access the data
        async userCanViewData() {
            let response = await getAPI(TmsIncidentApi).apiTmsTmsincidentCheckincidentPost();
            return response.data.result;
        }
    }
});
