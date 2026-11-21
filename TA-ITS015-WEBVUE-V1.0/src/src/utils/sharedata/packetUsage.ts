import { getAPI } from '/@/utils/axios-utils';
import { ShareDataMappingApi, ShareDataSubscriptionApi, SubSubscriptionState } from '/@/api-services/shareData';
import type { ShareDataSubscriptionOutput } from '/@/api-services/shareData';

/** Khớp PacketCommandHandler.IsDatatypeInUseAsync: mọi ánh xạ hoặc đăng ký Active. */
export const isPacketInUse = async (datatypeId: string): Promise<boolean> => {
	const mappings = await getAPI(ShareDataMappingApi).apiSharedataSharedatamappingPagePost({ datatypeId, page: 1, pageSize: 1 });
	const records = mappings.data.result?.records;
	// Không coi response thiếu dữ liệu là gói tin chưa được sử dụng.
	if (!Array.isArray(records)) throw new Error('Invalid mapping usage response');
	if (records.length) return true;
	return hasActiveSubscription(datatypeId);
};

/**
 * Đăng ký Active của gói — khớp PacketCommandHandler.EnsureNoActiveSubscriptionAsync (ESH-2306):
 * đăng ký lưu DatatypeId theo ID gói (code cũ lưu theo Code) nên tra cả hai.
 */
export const listActiveSubscriptions = async (packetId: string, packetCode?: string | null): Promise<ShareDataSubscriptionOutput[]> => {
	const keys = [...new Set([packetId, packetCode].filter((k): k is string => Boolean(k)))];
	const results = await Promise.all(
		keys.map((k) =>
			getAPI(ShareDataSubscriptionApi).apiSharedataSharedatasubscriptionListGet(undefined, undefined, undefined, SubSubscriptionState.NUMBER_1, k)
		)
	);
	return results.flatMap((r) => r.data.result ?? []);
};

/** Gói tin có đăng ký Active (mọi chiều) — khớp luật chặn tắt gói ESH-2307 bên BE. */
export const hasActiveSubscription = async (datatypeId: string): Promise<boolean> => {
	const subscriptions = await getAPI(ShareDataSubscriptionApi).apiSharedataSharedatasubscriptionListGet(
		undefined, undefined, undefined, SubSubscriptionState.NUMBER_1, datatypeId
	);
	if (!Array.isArray(subscriptions.data.result)) throw new Error('Invalid subscription usage response');
	return subscriptions.data.result.length > 0;
};
