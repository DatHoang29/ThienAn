/**
 * Tiện ích định dạng và tra cứu nhãn hiển thị cho phân hệ ShareData (Gói tin, Đối tác, ...)
 */

/**
 * Định dạng nhãn hiển thị dạng "[mã] tên"
 * Ví dụ: { code: 'HNCL_01', name: 'Tuyến Hữu Nghị - Chi Lăng' } -> "[HNCL_01] Tuyến Hữu Nghị - Chi Lăng"
 */
export const codeNameLabel = (item?: { code?: string | null; name?: string | null } | any): string => {
	if (!item) return '';
	return item.code ? `[${item.code}] ${item.name ?? ''}` : (item.name ?? '');
};

/**
 * Tra cứu nhãn tên đối tác (Partner) theo ID từ danh sách đối tác
 * @param list Danh sách đối tác (partnerList)
 * @param id ID của đối tác cần tra cứu
 * @param fallback Giá trị trả về khi không tìm thấy (mặc định '')
 */
export const partnerNameOf = (list: any[] | undefined | null, id?: string | null, fallback: string = ''): string => {
	if (!id || !Array.isArray(list)) return fallback;
	const p = list.find((item: any) => item.id === id || item.code === id);
	return p ? codeNameLabel(p) : fallback;
};

/**
 * Tra cứu nhãn tên gói tin (Packet / Datatype) theo ID hoặc Code từ danh sách gói tin
 * @param list Danh sách gói tin (packetList)
 * @param id ID hoặc Code gói tin cần tra cứu
 * @param fallback Giá trị trả về khi id rỗng (mặc định '—')
 */
export const packetNameOf = (list: any[] | undefined | null, id?: string | number | null, fallback: string = '—'): string => {
	if (id === null || id === undefined || id === '') return fallback;
	if (!Array.isArray(list) || list.length === 0) return fallback;
	const target = String(id).trim();
	const p = list.find((item: any) =>
		(item?.id !== undefined && item?.id !== null && String(item.id).trim() === target) ||
		(item?.code !== undefined && item?.code !== null && String(item.code).trim() === target)
	);
	return p ? codeNameLabel(p) : String(id);
};

/** Alias tương thích theo tên gọi truyền thống datatypeLabel */
export const datatypeLabel = packetNameOf;
