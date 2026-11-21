import { getAPI } from '/@/utils/axios-utils';
import { ShareDataPacketApi, SqlFieldItem } from '/@/api-services/shareData';

/** Trường của gói tin dùng ở màn ánh xạ, dựng từ danh sách trường của câu SQL (ShareDataSql.FieldsJson). */
export interface PacketFieldMeta {
	aliasFieldKey: string;
	name: string;
	type?: string | null;
	isRequired: boolean;
	groupName?: string | null;
	remark?: string | null;
}

export const toPacketFieldMeta = (f: SqlFieldItem): PacketFieldMeta => ({
	aliasFieldKey: f.key || '',
	name: f.label || f.key || '',
	type: f.type,
	isRequired: Boolean(f.required),
	groupName: f.group,
	remark: f.remark,
});

/**
 * Trường dùng được của gói tin: câu SQL riêng của đối tác nếu có, không thì câu mẫu chung.
 * Máy chủ đã bỏ trường mất khỏi câu SQL và trường bị đánh Bỏ qua.
 */
export const fetchPacketFields = async (datatypeId: string, partnerId?: string | null): Promise<PacketFieldMeta[]> => {
	const res = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketGetfieldsGet(datatypeId, partnerId || undefined);
	return (res.data?.result ?? []).filter((f) => f?.key).map(toPacketFieldMeta);
};
