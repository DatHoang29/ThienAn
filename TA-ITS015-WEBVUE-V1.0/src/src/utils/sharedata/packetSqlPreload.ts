import { markRaw } from 'vue';
import { getAPI } from '/@/utils/axios-utils';
import { ShareDataPacketApi } from '/@/api-services/shareData';
import type { PacketSqlDetailOutput } from '/@/api-services/shareData/models';

/**
 * Cấu hình SQL mẫu chung của gói nạp một lần khi bấm gói (API GetSqlDetail), chia cho khung SQL, bảng tham số và khối câu ghi.
 * Mỗi phần chỉ lấy một lần; các lần nạp sau (sau khi Lưu, đổi tab đối tác) gọi API riêng như cũ.
 */
export type PacketSqlPreloadPart = 'extract' | 'variables' | 'write';

export interface PacketSqlPreload {
	packetCode: string;
	promise: Promise<PacketSqlDetailOutput | null>;
	taken: Set<PacketSqlPreloadPart>;
}

/** Gọi GetSqlDetail cho gói; lỗi thì trả null để từng phần tự gọi API riêng. */
export const createPacketSqlPreload = (packetCode: string): PacketSqlPreload =>
	markRaw({
		packetCode,
		taken: new Set<PacketSqlPreloadPart>(),
		promise: getAPI(ShareDataPacketApi)
			.apiSharedataSharedatapacketGetsqldetailGet(packetCode)
			.then((res) => res.data?.result ?? null)
			.catch(() => null),
	});

/** Lấy phần đã nạp sẵn đúng gói, mỗi phần một lần; không có thì trả null. */
export const takePacketSqlPreload = async (preload: PacketSqlPreload | null | undefined, packetCode: string | null | undefined, part: PacketSqlPreloadPart) => {
	if (!preload || !packetCode || preload.packetCode !== packetCode || preload.taken.has(part)) return null;
	preload.taken.add(part);
	return preload.promise;
};
