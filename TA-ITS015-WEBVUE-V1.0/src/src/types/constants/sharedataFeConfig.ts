import { SharedataFeConfigCodeEnum } from '/@/types/enums/baseEnum';

// Giá trị dự phòng của cấu hình ShareData (min/max/default các ô nhập số ShareData).
// Nguồn nay là SysOpConfig nhóm sharedata_config (script 03). Dùng khi DB chưa có dòng cấu hình hoặc giá trị không hợp lệ.
export const SHAREDATA_FE_CONFIG_FALLBACK: Readonly<Record<SharedataFeConfigCodeEnum, number>> = {
	[SharedataFeConfigCodeEnum.SubscriptionIntervalSecMin]: 5,
	[SharedataFeConfigCodeEnum.SubscriptionIntervalSecMax]: 86400,
	[SharedataFeConfigCodeEnum.SubscriptionIntervalSecDefault]: 30,
	[SharedataFeConfigCodeEnum.SubscriptionDebounceSecMin]: 0,
	[SharedataFeConfigCodeEnum.SubscriptionDebounceSecMax]: 3600,
	[SharedataFeConfigCodeEnum.SubscriptionDebounceSecDefault]: 30,
	[SharedataFeConfigCodeEnum.SubscriptionPriorityMin]: 0,
	[SharedataFeConfigCodeEnum.SubscriptionPriorityMax]: 10,
	[SharedataFeConfigCodeEnum.SubscriptionPriorityDefault]: 5,
	[SharedataFeConfigCodeEnum.PartnerHeartbeatSecMin]: 5,
	[SharedataFeConfigCodeEnum.PartnerHeartbeatSecMax]: 300,
	[SharedataFeConfigCodeEnum.PartnerHeartbeatSecDefault]: 30,
	[SharedataFeConfigCodeEnum.PartnerResponseTimeoutSecMin]: 3,
	[SharedataFeConfigCodeEnum.PartnerResponseTimeoutSecMax]: 60,
	[SharedataFeConfigCodeEnum.PartnerResponseTimeoutSecDefault]: 10,
	[SharedataFeConfigCodeEnum.PartnerDatagramSizeMin]: 512,
	[SharedataFeConfigCodeEnum.PartnerDatagramSizeMax]: 65507,
	[SharedataFeConfigCodeEnum.PartnerDatagramSizeDefault]: 1024,
	[SharedataFeConfigCodeEnum.MappingSampleTopMin]: 1,
	[SharedataFeConfigCodeEnum.MappingSampleTopMax]: 50,
	[SharedataFeConfigCodeEnum.MappingSampleTopDefault]: 5,
	[SharedataFeConfigCodeEnum.HistoryRangeDefaultDays]: 7,
	[SharedataFeConfigCodeEnum.HistoryRangeMaxDays]: 31,
	[SharedataFeConfigCodeEnum.SqlSampleTopMin]: 1,
	[SharedataFeConfigCodeEnum.SqlSampleTopMax]: 50,
	[SharedataFeConfigCodeEnum.SqlSampleTopDefault]: 10,
	[SharedataFeConfigCodeEnum.LogListDefaultTop]: 1000,
};

export { ConnectionTypeEnum } from '/@/types/enums/baseEnum';

