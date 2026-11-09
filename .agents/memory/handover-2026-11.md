# Bàn giao 11/2026

> Gom từ memory cá nhân Claude ngày 09/11/2026.

## Bàn giao 11/2026

> Người dùng thôi việc, bàn giao ShareData và VideoWall; hai bộ tài liệu tách riêng trong ThienAn/bangiao; repo ThienAn đang công khai trên GitHub kèm khóa và bản sao CSDL trong lịch sử

Người dùng chấm dứt hợp đồng, bàn giao cho Trần Hồng Sơn theo biên bản FMS-HRD-03_BM.02 (lập 02/11/2026).

Tài liệu bàn giao mã nguồn nằm ở `bangiao/` tại gốc repo ThienAn (chuyển khỏi WEBAPI ngày 04/11/2026; người dùng đã đổi tên thư mục vài lần, tìm theo tên tệp nếu không thấy), người dùng tải lên Drive công ty: DOC_ITS / DOC_ITS015 - HUU NGHI - CHI LANG / DOC13_TaiLieuDangXuLy / Handover / Dat / BAN_GIAO, tách theo phân hệ, mỗi phân hệ có bản `.html` và `.md`: `BAN-GIAO-MA-NGUON-SHAREDATA`, `BAN-GIAO-MA-NGUON-VIDEOWALL`. Sửa một phân hệ thì sửa cả hai bản của phân hệ đó, không gộp lại.

**Why:** sếp cần tài liệu chỉ rõ "luồng này liên quan hàm a b c", hàm chưa có comment thì ghi rõ làm gì, lưu ý gì. Người dùng yêu cầu tách, không gộp chung, có thêm bản Markdown, gom một thư mục, bảng "ai làm" tách cột BE, FE.

**How to apply:**
- Tài liệu bàn giao chỉ nói về tệp trong `TA-ITS015-WEBAPI-V1.0` và `TA-ITS015-WEBVUE-V1.0`. Không nhắc `DocBusinessThienAn`, MasterPlan, đặc tả, biên bản họp, script seed, `.agents`, repo `ThienAn`, vì người dùng tự lưu riêng (yêu cầu 02/11/2026).
- Comment bàn giao chỉ làm ở BE và Workers, không comment FE (người dùng chốt 02/11/2026, đã gỡ 442 dòng comment FE). Tài liệu chỉ nêu tên hàm và điểm cần lưu ý, chi tiết nằm ở comment BE. HTML là bản gốc, `.md` sinh từ HTML.
- Test case là người dùng tự test riêng: không đưa số liệu, mục kiểm thử, thư mục `tests/` vào tài liệu bàn giao và biên bản. Ngày 02/11/2026 người dùng bỏ ở cả hai tài liệu: dòng "Tài liệu cùng đợt", mục "Đọc trước" (gồm khung "Ba điểm dễ gây sự cố"), mục "Phần nào do ai làm"; riêng ShareData bỏ thêm khung "Bẫy SqlSugar", câu máy và thư mục triển khai Worker, mục "Comment đã cũ, đừng tin", mục "Việc cần làm khi nhận"; bỏ mục giới hạn và việc còn lại ở cả hai tài liệu. Chiều 02/11 hai tài liệu được viết lại ngoài phiên Claude này (đã stage), có mục cuối "Hạng mục chưa làm và lưu ý khi tiếp nhận" thay cho mục đã bỏ; coi bản đó là bản hiện hành, đừng tự xóa mục này hay thêm lại các mục cũ.
- Comment bàn giao VideoWall (77 tệp, 823 dòng) đã gỡ hết ngày 02/11/2026 theo yêu cầu người dùng: code VideoWall về đúng HEAD, không comment thêm trừ khi được yêu cầu lại. Tài liệu VideoWall vì vậy khác ShareData: giữ nhãn "chưa comment" và ghi rõ làm gì, lưu ý gì cho hàm không có comment. Comment cũ sai có từ trước giữ nguyên, tài liệu liệt kê ở mục lưu ý.
- 02/11/2026 người dùng chốt tạm ẩn mục "4. Mốc gửi" ở màn dataSource (hằng `CURSOR_MANAGEMENT_VISIBLE` trong `sqlVariablePanel.vue`), cùng ngày đã làm trang riêng `views/sharedata/cursor` (menu `shareDataCursor`, chưa nối WebAPI thật) và trả lại dải Quy chuẩn mốc; mã ẩn cũ trong panel và hằng đó đã dọn cùng ngày.
- Tài liệu bàn giao không ghi bước nâng cấp CSDL cũ (gỡ chỉ mục, chuyển mốc, xóa bảng cũ): người dùng chốt 02/11/2026 là triển khai làm mới lại, việc nào xong rồi thì thôi.
- `git blame` ghi VideoWall dưới tên Văn Hiếu qua commit `bbd178fe`, nhưng Worker và WPF phần lớn do người dùng viết (nhánh `feat/20260909-videowall-device`, `bdb2472e`). Đừng kết luận theo blame.
- VideoWall 02/11/2026: 233 ca, 220 đạt, 13 lỗi ở `WebApi/Controllers`, phần lớn do quy tắc mới của `cd27c426`; chưa sửa. Liên quan project_sharedata-verification-status.

Trang Mốc gửi (`views/sharedata/cursor`): 05/11/2026 người dùng đổi ý vì cuối tháng mới nghỉ, cho làm hết mục 9. Đã khôi phục seed `sharedata-seed-menu-SysMenu.sql` (tệp 13), bỏ dòng này khỏi mục 9, thêm mục 12 ở kịch bản `doc/03`. Không gán vai trò: API của trang chỉ nhận SuperAdmin, các menu ShareData khác trên DB dev cũng không có dòng `SysRoleMenu`. Seed chưa chạy trên DB dev: `SysMenu` không có trên DB test nên chỉ đối chiếu cột.

05/11/2026 người dùng bỏ lớp mã hóa `EncryptManager` khỏi tài liệu bàn giao (mục 9, bảng hàm, ESH-1507), mã nguồn giữ nguyên. Đừng thêm lại vào bàn giao.

06/11/2026 xếp lại bàn giao: nối đuôi là mục 5 (ngay sau Luồng xử lý), Hạng mục chưa làm thành mục 10 ở cuối; mọi ghi chú "mục 9" trước ngày này nay là mục 10. Mục 10 còn đúng một dòng: chạy nhiều Worker trên máy chủ. Cách làm là clone thư mục publish theo `bangiao/HUONG-DAN-CLONE-WORKER.md`. Bước chạy trên máy chủ do người dùng làm vì không được đụng các máy chủ. Chạy xong thì bỏ dòng này khỏi mục 9.

## Lưu ý bàn giao chỉ ghi pending

> Mục lưu ý trong tài liệu bàn giao chỉ ghi việc đã chốt nhưng chưa xong hoặc đang pending; không ghi lưu ý về dạng cũ vì tính năng đang làm mới

Mục lưu ý, hạng mục chưa làm trong tài liệu bàn giao chỉ ghi việc đã chốt nhưng chưa xong, hoặc đang pending.

**Why:** ngày 03/11/2026 người dùng bảo xóa dòng "Câu riêng dạng 2 cột đã lưu" khỏi mục 9: "mình đang làm mới mà lưu ý làm gì nữa, lưu ý là chỉ nên những cái đã chốt nhưng chưa xong hoặc pending".

04/11/2026 người dùng tiếp tục xóa các dòng DB dev (câu riêng còn `@lastTime`, gói 111), "Tự động dọn tệp bằng chứng" (đã chốt chưa cần làm) và "Gói mới muốn gửi tức thì" (lưu ý cấu hình): "không cần note vô".

**How to apply:** không thêm lưu ý về dữ liệu dạng cũ, tình trạng một CSDL cụ thể, việc đã chốt là chưa cần làm, hay lưu ý cấu hình vào mục này; mục này (từ 06/11/2026 là mục 10, trước là mục 9) hiện chỉ còn việc thật sự phải làm tiếp (chạy nhiều Worker trên máy chủ, nối mã hóa). Liên quan feedback-no-legacy-messages, project_handover-2026-11.

## Sổ tay: trong bàn giao

> Sổ tay nối đuôi ShareData đã gom vào bangiao/BAN-GIAO-MA-NGUON-SHAREDATA.html mục 5 Nối đuôi theo mốc (06/11/2026), xóa hai bản HTML cũ; chỉ sửa ở đó, không đăng artifact

06/11/2026 người dùng yêu cầu gom Sổ tay nối đuôi vào tệp bàn giao HTML ("tập trung 1 nguồn thôi"), rồi gom luôn phần 4.2 Nối đuôi theo mốc và các hàm FE của mốc vào cùng mục, và để Hạng mục chưa làm xuống cuối. Người dùng bảo thêm: nối đuôi nằm ngay sau Luồng xử lý. Bàn giao nay: 1–4 như cũ, mục 5 Nối đuôi theo mốc (5.1 Kết luận, 5.2 Mốc lưu ở đâu, 5.3 Biến và mốc, 5.4 Luồng chảy, 5.5 Mẫu chung và cấu hình riêng, 5.6 Hàm chính, 5.7 Vận hành, 5.8 Mẫu câu, 5.9 Thông báo hay gặp, 5.10 Giới hạn và rủi ro, 5.11 Đối chiếu biên bản, 5.12 Hiệu năng), 6 API, 7 FE, 8 Cấu hình vận hành, 9 Nhật ký và mã lỗi, mục 10 Hạng mục chưa làm ở cuối. CSS riêng của sổ tay chỉ áp trong `#so-tay`. Mục lục có nhóm thu gọn (`details.toc-group`) cho mục 4 và 5. Hai bản Sổ tay cũ đã xóa. Bản `.md` có 5.2, 5.5, 5.6 (không đánh số) và một dòng trỏ sang HTML cho phần Sổ tay vì có sơ đồ SVG.

**Why:** người dùng muốn một nguồn tài liệu; trước đây phải sửa hai bản giống hệt và artifact trên claude.ai gây nhầm.

**How to apply:** sửa quy tắc nối đuôi thì cập nhật mục 5 bản HTML (bảng "Câu nào lưu được" ở 5.8, "Thông báo hay gặp" 5.9, "Giới hạn và rủi ro" 5.10) và phần tương ứng của bản `.md`. Không đăng lại artifact, không nhắc artifact cũ.
