import { getAPI } from '/@/utils/axios-utils';
import { ShareDataPacketApi } from '/@/api-services/shareData/apis/share-data-packet-api';

export interface SourceTable {
	id: string;
	tableName: string;
	displayName?: string;
	description?: string;
	defaultAlias?: string;
	packetCodes?: string[];
}

let sourceTablesPromise: Promise<SourceTable[]> | null = null;
const columnsCache = new Map<string, any[]>();

/** Danh mục bảng nguồn được phép (nạp một lần từ máy chủ, lỗi thì lần sau nạp lại). */
export const loadSourceTables = (): Promise<SourceTable[]> => {
	if (!sourceTablesPromise) {
		sourceTablesPromise = getAPI(ShareDataPacketApi)
			.apiSharedataSharedatapacketGetmetadatatablesGet()
			.then((res) => {
				const list = res?.data?.result ?? res?.data ?? [];
				return Array.isArray(list) ? (list as SourceTable[]) : [];
			})
			.catch(() => {
				sourceTablesPromise = null;
				return [] as SourceTable[];
			});
	}
	return sourceTablesPromise;
};

/** Tìm bảng trong danh mục theo tên (không phân biệt hoa thường); không có nghĩa là bảng không được phép. */
export const findSourceTable = async (tableName: string): Promise<SourceTable | undefined> => {
	const key = (tableName || '').trim().toLowerCase();
	if (!key) return undefined;
	return (await loadSourceTables()).find((t) => (t.tableName || '').trim().toLowerCase() === key);
};

/** Cột của một bảng nguồn: máy chủ chỉ nhận mã định danh của bảng nên tên bảng được đổi sang mã ở đây. */
export const fetchSourceTableColumns = async (tableName: string): Promise<any[]> => {
	const key = (tableName || '').trim().toLowerCase();
	if (!key) return [];
	if (columnsCache.has(key)) return columnsCache.get(key) || [];
	const table = await findSourceTable(tableName);
	if (!table?.id) return [];
	try {
		const res = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketGetmetadatafieldsGet(table.id);
		const fields = res?.data?.result ?? res?.data ?? [];
		const list = Array.isArray(fields) ? fields : [];
		columnsCache.set(key, list);
		return list;
	} catch {
		return [];
	}
};

/** Mã gói tin của bảng có khớp mã gói đang mở không (khớp nguyên văn, theo tiền tố hoặc theo số trong mã). */
export const matchesPacketCode = (itemPacketCodes: string[] | undefined, targetPacketCode: string | undefined): boolean => {
	if (!itemPacketCodes || itemPacketCodes.length === 0 || !targetPacketCode) return false;
	const rawCode = targetPacketCode.trim().toLowerCase();
	if (!rawCode) return false;
	const numericMatch = rawCode.match(/^\D*(\d+)/);
	const numericCode = numericMatch ? numericMatch[1] : '';

	return itemPacketCodes.some((p) => {
		const pClean = (p || '').trim().toLowerCase();
		if (!pClean) return false;
		if (pClean === rawCode) return true;
		if (rawCode.startsWith(`${pClean}_`) || rawCode.startsWith(`${pClean}-`)) return true;
		if (pClean.startsWith(`${rawCode}_`) || pClean.startsWith(`${rawCode}-`)) return true;
		const pNumericMatch = pClean.match(/^\D*(\d+)/);
		const pNumeric = pNumericMatch ? pNumericMatch[1] : '';
		if (numericCode && pNumeric && numericCode === pNumeric) return true;
		return false;
	});
};
