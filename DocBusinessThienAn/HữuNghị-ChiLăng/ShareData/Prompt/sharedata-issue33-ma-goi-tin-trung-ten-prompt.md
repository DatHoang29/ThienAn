# Issue 33 (F16) — Bổ sung mã trước tên khi hiển thị Gói tin / Đối tác (chặn nhập nhằng khi trùng tên khác mã)

**Tệp prompt:** `DocBusinessThienAn/HữuNghị-ChiLăng/ShareData/Prompt/sharedata-issue33-ma-goi-tin-trung-ten-prompt.md`

## Bối cảnh

Issue 33 (F16) ban đầu chỉ mô tả đúng 1 chỗ lỗi: `subscriptionTable.vue` dùng `packetNameOf()` chỉ hiển thị `.name`, bỏ qua `.code`, khiến 2 gói tin trùng tên khác mã (`101_commonData` / `101_commonData1` — đã xác nhận có thật trên staging qua bảng `ShareDataPacket`) không phân biệt được trên UI.

Rà soát lại toàn bộ `TA-ITS015-WEBVUE-V1.0/src/src/views/sharedata` (theo yêu cầu kiểm tra "còn sót không", 2 lượt rà — lượt 2 bắt thêm 5 file bị bỏ sót ở lượt 1 vì tên hàm khác `NameOf`) phát hiện **cùng một lỗi lặp lại ở 9 file khác**, cả cho Gói tin (packet) lẫn Đối tác (partner) — vì cả hai đều là danh mục do người dùng tự tạo, có thể trùng tên khác mã. Tổng cộng **10 file**. Quy ước sửa đã có sẵn tiền lệ ngay trong module này (`editMapping.vue:1016-1017`, dùng cho Issue 18):

```ts
/** Nhãn dạng "[mã] tên" — phân biệt bản ghi trùng tên nhưng khác mã; filterable lọc được cả mã và tên. */
const codeNameLabel = (item: any) => (item?.code ? `[${item.code}] ${item.name ?? ''}` : (item?.name ?? ''));
```

Toàn bộ thay đổi dưới đây áp đúng quy ước này. **FE thuần** — không đụng Backend, không cần migrate DB. Không có test E2E (`tests/FE/`) nào đụng tới các màn hình này nên không có rủi ro hồi quy test tự động; kiểm chứng bằng tay qua UI (xem mục Kiểm chứng).

---

## 1. `sharing/component/subscriptionTable.vue` — Bảng Cấu hình Gửi đi/Nhận về của Đối tác

File lỗi gốc được nêu trong F16. Hàm `packetNameOf()` hiện chỉ khớp theo `id` và chỉ trả `.name`.

**TRƯỚC** (dòng 94-97):
```ts
const packetNameOf = (id?: string | null) => {
	if (id === null || id === undefined || id === '') return '—';
	return packetList.value.find((p: any) => p.id === id)?.name || String(id);
};
```

**SAU**:
```ts
const packetNameOf = (id?: string | null) => {
	if (id === null || id === undefined || id === '') return '—';
	const p = packetList.value.find((item: any) => item.id === id || item.code === id);
	if (!p) return String(id);
	return p.code ? `[${p.code}] ${p.name ?? ''}` : (p.name ?? '');
};
```

Khớp thêm theo `code` (không chỉ `id`) để đồng nhất với cách `history/index.vue` đang tra cứu (`p.id === id || p.code === id`) — phòng trường hợp dữ liệu cũ lưu `code` thay vì `id`.

Cột `datatypeId` nới `minWidth` cho vừa nhãn dài hơn:

**TRƯỚC** (dòng 196-201):
```ts
			{
				field: 'datatypeId',
				title: $t('lz.entity.sharedataSubscription.datatypeId'),
				minWidth: 160,
				showOverflow: 'tooltip',
				formatter: ({ cellValue }: any) => packetNameOf(cellValue),
			},
```

**SAU** (chỉ đổi `minWidth`):
```ts
			{
				field: 'datatypeId',
				title: $t('lz.entity.sharedataSubscription.datatypeId'),
				minWidth: 220,
				showOverflow: 'tooltip',
				formatter: ({ cellValue }: any) => packetNameOf(cellValue),
			},
```

---

## 2. `sharing/component/editSubscription.vue` — Modal Thêm/Sửa đăng ký (chọn Gói tin)

Phát hiện thêm: ngay trong modal Thêm/Sửa của chính Issue 33, ô chọn **Đối tác** đã đúng (`${p.code} - ${p.name}`, dòng 26), nhưng ô chọn **Gói tin** ngay dưới lại bỏ sót mã.

**TRƯỚC** (dòng 35-38):
```vue
										<el-option v-for="d in packetList"
											:key="d.id" :label="d.name" :value="d.id">
											<span>{{ d.name }}</span>
										</el-option>
```

**SAU**:
```vue
										<el-option v-for="d in packetList"
											:key="d.id" :label="codeNameLabel(d)" :value="d.id">
											<span>{{ codeNameLabel(d) }}</span>
										</el-option>
```

Thêm hàm `codeNameLabel` ngay sau khai báo `packetList` trong `<script setup>`:

**TRƯỚC** (dòng 92-97):
```ts
const packetList = ref<any[]>([]);

const packetNameOf = (id?: string | null) => {
	if (id === null || id === undefined || id === '') return '—';
	return packetList.value.find((p: any) => p.id === id)?.name || String(id);
};
```

**SAU**:
```ts
const packetList = ref<any[]>([]);

/** Nhãn dạng "[mã] tên" — phân biệt gói tin trùng tên nhưng khác mã. */
const codeNameLabel = (item: any) => (item?.code ? `[${item.code}] ${item.name ?? ''}` : (item?.name ?? ''));

const packetNameOf = (id?: string | null) => {
	if (id === null || id === undefined || id === '') return '—';
	const p = packetList.value.find((item: any) => item.id === id);
	return p ? codeNameLabel(p) : String(id);
};
```

(Lưu ý: file này có 2 hàm cùng tên `packetNameOf` ở 2 chỗ khác nhau trong repo — đây là bản trong `editSubscription.vue`, dùng để hiển thị tóm tắt gói tin đã chọn ở nơi khác trong cùng component; không nhầm với bản trong `subscriptionTable.vue` ở mục 1.)

---

## 3. `mapping/index.vue` — Bảng Ánh xạ dữ liệu (lọc + cột Đối tác/Gói tin)

**TRƯỚC** (dòng 174-175):
```ts
const partnerNameOf = (id?: string) => (id ? state.partnerList.find((p: any) => p.id === id)?.name : '');
const packetNameOf = (id?: string) => (id ? state.packetList.find((p: any) => p.id === id)?.name || id : '—');
```

**SAU**:
```ts
/** Nhãn dạng "[mã] tên" — phân biệt bản ghi trùng tên nhưng khác mã. */
const codeNameLabel = (item: any) => (item?.code ? `[${item.code}] ${item.name ?? ''}` : (item?.name ?? ''));
const partnerNameOf = (id?: string) => {
	if (!id) return '';
	const p = state.partnerList.find((item: any) => item.id === id);
	return p ? codeNameLabel(p) : '';
};
const packetNameOf = (id?: string) => {
	if (!id) return '—';
	const p = state.packetList.find((item: any) => item.id === id);
	return p ? codeNameLabel(p) : id;
};
```

Hai ô lọc trên đầu trang cũng chỉ hiện `.name`:

**TRƯỚC** (dòng 23-25, bộ lọc Đối tác):
```vue
								<el-option v-for="item in state.partnerList" :key="item.id" :label="item.name"
									:value="item.id" />
```

**SAU**:
```vue
								<el-option v-for="item in state.partnerList" :key="item.id" :label="codeNameLabel(item)"
									:value="item.id" />
```

**TRƯỚC** (dòng 33-34, bộ lọc Gói tin):
```vue
								<el-option v-for="item in state.packetList" :key="item.id"
									:label="item.name" :value="item.id" />
```

**SAU**:
```vue
								<el-option v-for="item in state.packetList" :key="item.id"
									:label="codeNameLabel(item)" :value="item.id" />
```

Nới chiều rộng 2 cột tương ứng trong lưới (dòng 187-188) cho vừa nhãn dài hơn:

**TRƯỚC**:
```ts
			{ field: 'partnerId', title: $t('lz.entity.sharedataMapping.partner'), width: 170, showOverflow: 'tooltip', slots: { default: 'row_partner' } },
			{ field: 'datatypeId', title: $t('lz.entity.sharedataMapping.datatype'), minWidth: 180, showOverflow: 'tooltip', slots: { default: 'row_datatype' } },
```

**SAU**:
```ts
			{ field: 'partnerId', title: $t('lz.entity.sharedataMapping.partner'), width: 210, showOverflow: 'tooltip', slots: { default: 'row_partner' } },
			{ field: 'datatypeId', title: $t('lz.entity.sharedataMapping.datatype'), minWidth: 220, showOverflow: 'tooltip', slots: { default: 'row_datatype' } },
```

(Cột hiển thị `row_partner`/`row_datatype` ở template gọi thẳng `partnerNameOf(row.partnerId)`/`packetNameOf(row.datatypeId)` — đã tự động nhận định dạng mới từ bản sửa 2 hàm ở trên, không cần sửa gì thêm ở khối `<template>`.)

---

## 4. `history/index.vue` — Bảng Lịch sử chia sẻ (lọc Đối tác + cột Gói tin)

**TRƯỚC** (dòng 258-261):
```ts
function datatypeLabel(id?: string | null): string {
	if (id === null || id === undefined || id === '') return '—';
	return packetList.value.find((p: any) => p.id === id || p.code === id)?.name || String(id);
}
```

**SAU**:
```ts
/** Nhãn dạng "[mã] tên" — phân biệt gói tin trùng tên nhưng khác mã. */
const codeNameLabel = (item: any) => (item?.code ? `[${item.code}] ${item.name ?? ''}` : (item?.name ?? ''));

function datatypeLabel(id?: string | null): string {
	if (id === null || id === undefined || id === '') return '—';
	const p = packetList.value.find((item: any) => item.id === id || item.code === id);
	return p ? codeNameLabel(p) : String(id);
}
```

Ô lọc Đối tác (dòng 35-36) cũng chỉ hiện `.name`:

**TRƯỚC**:
```vue
							<el-option v-for="v in partners" :key="v.id" :label="v.name" :value="v.id" />
```

**SAU**:
```vue
							<el-option v-for="v in partners" :key="v.id" :label="codeNameLabel(v)" :value="v.id" />
```

Nới cột `datatypeId` (dòng 294-297):

**TRƯỚC**:
```ts
		{
			field: 'datatypeId', title: $t('lz.label.sharedataHistory.datatype'), minWidth: 180,
			formatter: ({ row }: any) => (row.datatypeId ? datatypeLabel(row.datatypeId) : '—'),
		},
```

**SAU** (chỉ đổi `minWidth`):
```ts
		{
			field: 'datatypeId', title: $t('lz.label.sharedataHistory.datatype'), minWidth: 220,
			formatter: ({ row }: any) => (row.datatypeId ? datatypeLabel(row.datatypeId) : '—'),
		},
```

⛔ **Không đụng cột `partnerName` (dòng 293)**: cột này đọc trực tiếp field `partnerName` do Backend trả về sẵn trong bản ghi log lịch sử (chụp lại tên đối tác tại thời điểm truyền, không tra cứu qua `partners` list hiện tại) — nằm ngoài phạm vi sửa FE của prompt này.

---

## 5. `errorLog/index.vue` — Bảng Nhật ký cảnh báo lỗi (lọc + cột Đối tác)

**TRƯỚC** (dòng 235):
```ts
const partnerNameOf = (id?: string) => (id ? state.partnerList.find((p: any) => p.id === id)?.name : '');
```

**SAU**:
```ts
/** Nhãn dạng "[mã] tên" — phân biệt đối tác trùng tên nhưng khác mã. */
const codeNameLabel = (item: any) => (item?.code ? `[${item.code}] ${item.name ?? ''}` : (item?.name ?? ''));
const partnerNameOf = (id?: string) => {
	if (!id) return '';
	const p = state.partnerList.find((item: any) => item.id === id);
	return p ? codeNameLabel(p) : '';
};
```

Ô lọc Đối tác (dòng 30-31):

**TRƯỚC**:
```vue
							<el-option v-for="item in state.partnerList" :key="item.id" :label="item.name" :value="item.id" />
```

**SAU**:
```vue
							<el-option v-for="item in state.partnerList" :key="item.id" :label="codeNameLabel(item)" :value="item.id" />
```

Nới cột `partnerId` (dòng 251):

**TRƯỚC**:
```ts
			{ field: 'partnerId', title: $t('lz.label.sharedataAlertLog.partner'), width: 160, showOverflow: 'tooltip', formatter: ({ cellValue }: any) => partnerNameOf(cellValue) || '—' },
```

**SAU** (chỉ đổi `width`):
```ts
			{ field: 'partnerId', title: $t('lz.label.sharedataAlertLog.partner'), width: 200, showOverflow: 'tooltip', formatter: ({ cellValue }: any) => partnerNameOf(cellValue) || '—' },
```

(Dòng 349 `drawerRef.value?.open({ ...row, partnerName: partnerNameOf(row.partnerId) })` tự động nhận định dạng mới, không cần sửa riêng.)

---

## 6. `sharing/index.vue` — `withPartnerName()` (nguồn cấp tên Đối tác cho modal Xem trước xuất dữ liệu)

**TRƯỚC** (dòng 345-348):
```ts
function withPartnerName(row: ShareDataSubscriptionOutput): ShareDataSubscriptionOutput {
	const name = partners.value.find((p) => p.id === row.partnerId)?.name;
	return { ...row, partnerName: name } as ShareDataSubscriptionOutput;
}
```

**SAU**:
```ts
/** Nhãn dạng "[mã] tên" — phân biệt đối tác trùng tên nhưng khác mã. */
const codeNameLabel = (item: any) => (item?.code ? `[${item.code}] ${item.name ?? ''}` : (item?.name ?? ''));

function withPartnerName(row: ShareDataSubscriptionOutput): ShareDataSubscriptionOutput {
	const p = partners.value.find((item) => item.id === row.partnerId);
	return { ...row, partnerName: p ? codeNameLabel(p) : undefined } as ShareDataSubscriptionOutput;
}
```

Giá trị `partnerName` này được truyền vào `exportPreviewDialog.vue` (mục 7 dưới đây) qua `openDialog(withPartnerName(row))` — sửa đúng 1 chỗ này là modal Xem trước tự nhận định dạng mới.

---

## 7. `sharing/component/exportPreviewDialog.vue` — Modal Xem trước xuất dữ liệu (cột Loại dữ liệu)

**TRƯỚC** (dòng 77-80):
```ts
function datatypeLabel(id?: string | null): string {
	if (id === null || id === undefined || id === '') return '—';
	return packetList.value.find((p: any) => p.id === id)?.name || String(id);
}
```

**SAU**:
```ts
/** Nhãn dạng "[mã] tên" — phân biệt gói tin trùng tên nhưng khác mã. */
const codeNameLabel = (item: any) => (item?.code ? `[${item.code}] ${item.name ?? ''}` : (item?.name ?? ''));

function datatypeLabel(id?: string | null): string {
	if (id === null || id === undefined || id === '') return '—';
	const p = packetList.value.find((item: any) => item.id === id || item.code === id);
	return p ? codeNameLabel(p) : String(id);
}
```

(Tên Đối tác trong modal này — dòng 5, `state.ctx.partnerName` — tự nhận định dạng mới từ mục 6, không cần sửa riêng trong file này.)

---

## 8. `history/component/recordDetailDrawer.vue` — Drawer chi tiết 1 bản ghi lịch sử (cột Loại dữ liệu)

**TRƯỚC** (dòng 47-50):
```ts
function datatypeLabel(id?: string | null): string {
	if (id === null || id === undefined || id === '') return '—';
	return packetList.value.find((p: any) => p.id === id || p.code === id)?.name || String(id);
}
```

**SAU**:
```ts
/** Nhãn dạng "[mã] tên" — phân biệt gói tin trùng tên nhưng khác mã. */
const codeNameLabel = (item: any) => (item?.code ? `[${item.code}] ${item.name ?? ''}` : (item?.name ?? ''));

function datatypeLabel(id?: string | null): string {
	if (id === null || id === undefined || id === '') return '—';
	const p = packetList.value.find((item: any) => item.id === id || item.code === id);
	return p ? codeNameLabel(p) : String(id);
}
```

⛔ Không đụng dòng 86 (`partnerName: $t('lz.label.sharedataRecordDetail.partnerName')`) — đây là **nhãn cột tĩnh** ("Tên đối tác"), không phải giá trị tra cứu; giá trị hiển thị đọc trực tiếp `row.partnerName` do Backend trả sẵn, ngoài phạm vi sửa FE.

---

## 9. `history/component/activityDetailDrawer.vue` — Drawer chi tiết hoạt động (cột Loại dữ liệu)

**TRƯỚC** (dòng 97-100):
```ts
function datatypeLabel(id?: string | null): string {
	if (id === null || id === undefined || id === '') return '—';
	return packetList.value.find((p: any) => p.id === id || p.code === id)?.name || String(id);
}
```

**SAU**: giống mẫu ở mục 8 (thêm `codeNameLabel` ngay trước hàm, đổi thân hàm tra theo `codeNameLabel(p)`).

⛔ Không đụng dòng 18 (`{{ row.partnerName || '—' }}`) — đọc trực tiếp field Backend trả sẵn, không phải lookup cục bộ.

---

## 10. `history/component/activityDetailDialog.vue` — Dialog chi tiết hoạt động (cột Loại dữ liệu)

**TRƯỚC** (dòng 298-301):
```ts
function datatypeLabel(id?: string | null): string {
	if (id === null || id === undefined || id === '') return '—';
	return packetList.value.find((p: any) => p.id === id || p.code === id)?.name || String(id);
}
```

**SAU**: giống mẫu ở mục 8.

⛔ Không đụng dòng 35 (`{{ row.partnerName || '—' }}`) — cùng lý do như mục 9.

---

## Kiểm chứng

Không chạy `npm run build` (dev server hot-reload theo rule 13 mục 4). Kiểm tra bằng tay trên trình duyệt:

1. **`subscriptionTable.vue`**: Mở màn Đối tác → tab Cấu hình Gửi đi/Nhận về. Nếu đối tác có 2 gói tin trùng tên khác mã, cột "Loại dữ liệu" phải hiện `[101_commonData] Gói 101 - ...` và `[101_commonData1] Gói 101 - ...` phân biệt rõ.
2. **`editSubscription.vue`**: Bấm "Thêm đăng ký" trên cùng màn, mở dropdown "Gói tin" — danh sách phải hiện `[Mã] Tên`.
3. **`mapping/index.vue`**: Mở màn Ánh xạ dữ liệu — cột Đối tác, cột Loại dữ liệu và 2 ô lọc tương ứng đều hiện `[Mã] Tên`.
4. **`history/index.vue`**: Mở màn Lịch sử chia sẻ — ô lọc Đối tác và cột Gói tin hiện `[Mã] Tên`.
5. **`errorLog/index.vue`**: Mở màn Nhật ký cảnh báo lỗi — ô lọc Đối tác và cột Đối tác hiện `[Mã] Tên`; mở drawer chi tiết 1 dòng, tên đối tác trong drawer cũng đổi theo.
6. Rà lại không có cảnh báo TypeScript nào do thiếu import/định nghĩa `codeNameLabel` ở file nào trong số 5 file trên (mỗi file tự khai báo riêng bản của mình, không import chung — đúng tiền lệ đã có trong `editMapping.vue`).

## Việc cuối — Cập nhật lại tài liệu gốc

1. **`DocBusinessThienAn/HữuNghị-ChiLăng/ShareData/KiemThu/F16-nhat-ky-loi-issue-20260929.md`**, dòng Issue 33 (hàng bảng `| **33** 🆕 | ...`):
   - Cột **Nguyên nhân**: sửa từ *"`subscriptionTable.vue` (bảng Cấu hình Gửi đi/Nhận về của Đối tác) dùng hàm `packetNameOf()` chỉ hiển thị `.name`, bỏ qua `.code`"* thành: *"Cùng một lỗi lặp lại ở 5 file: `subscriptionTable.vue`, `editSubscription.vue`, `mapping/index.vue`, `history/index.vue`, `errorLog/index.vue` — mỗi file có hàm/el-option riêng chỉ hiển thị `.name`, bỏ qua `.code`, cho cả Gói tin và Đối tác."*
   - Cột **Phương án xử lý**: sửa thành: *"**FE**: thêm hàm `codeNameLabel` cục bộ ở cả 5 file, áp dạng `[Mã] Tên` cho mọi nơi hiển thị Gói tin/Đối tác (ô lọc, cột lưới, dropdown modal), nới `minWidth`/`width` các cột liên quan — chi tiết tại `Prompt/sharedata-issue33-ma-goi-tin-trung-ten-prompt.md`."*
   - Cột **Hoàn thành**: giữ `⚠️ Chờ phản hồi`, sửa ghi chú thành: *"đã mở rộng phạm vi sau khi rà soát toàn bộ `views/sharedata`, có file prompt đầy đủ 5 file, **code chưa áp dụng**."*

2. **`DocBusinessThienAn/HữuNghị-ChiLăng/ShareData/Prompt/README.md`**, mục "⚪ Issue 33" — sửa dòng mô tả Task trong bảng thành nhắc đủ cả 10 file (`subscriptionTable.vue`, `editSubscription.vue`, `mapping/index.vue`, `history/index.vue`, `errorLog/index.vue`, `sharing/index.vue`, `exportPreviewDialog.vue`, `recordDetailDrawer.vue`, `activityDetailDrawer.vue`, `activityDetailDialog.vue`), giữ nguyên trạng thái ⚠️ "Chưa làm — chờ người dùng áp dụng".
