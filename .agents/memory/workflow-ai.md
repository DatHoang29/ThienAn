# Cách làm việc với AI trong dự án

> Quy ước người dùng đã chốt với AI, gom từ memory cá nhân Claude ngày 09/11/2026. Quy tắc trong `../rules/` thắng khi trùng.

## Trả lời tiếng Việt

> Luôn trả lời/báo cáo bằng tiếng Việt, kể cả bản tóm tắt cuối phiên

Mọi câu trả lời và báo cáo cho người dùng viết bằng tiếng Việt (thuật ngữ kỹ thuật, tên mã giữ nguyên).

**Why:** 24/10/2026 báo cáo cuối viết bằng tiếng Anh, người dùng hỏi "sao báo cáo tiếng anh rồi".
**How to apply:** kể cả sau khi phiên được tóm tắt (compaction) hay khi tool/hook trả về tiếng Anh, câu trả lời vẫn là tiếng Việt. Cả tin nhắn ngắn giữa các lần gọi tool cũng viết tiếng Việt.

03/11/2026 lặp lại: bản tóm tắt sau compaction viết tiếng Anh nên các tin nhắn sau đó trôi sang tiếng Anh, người dùng phải nhắc "báo cáo tiếng việt". Sau compaction, kiểm lại ngôn ngữ trước khi gửi.

## Gortex trước tiên

> Trong repo ThienAn luôn nạp tool Gortex (ToolSearch) và dùng nó thay Read/Grep/Glob/Bash cho mã .cs/.vue/.ts đã index

Với mã nguồn đã index (.cs/.vue/.ts/.js), phải dùng tool Gortex (`mcp__gortex__explore`, `search_symbols`, `get_symbol_source`, `search_text`, `read_file`...) thay vì Read/Grep/Glob/Bash.

**Why:** Ngày 2026-10-12 tôi dùng Bash/Read trực tiếp trên test BE dù CLAUDE.md bắt buộc Gortex; user phải nhắc. Nguyên nhân: tool Gortex là deferred (chưa ToolSearch thì chưa gọi được), hook cũ chỉ nhắc chứ không chặn, và tên tool trong hướng dẫn (search/read/relations/change) lệch tên tool thật. Cũng từng nhầm tests/FE với tests/BE vì lấy file IDE đang mở làm ngữ cảnh.

**How to apply:** Đầu phiên chạy ToolSearch `select:mcp__gortex__explore,mcp__gortex__search_symbols,mcp__gortex__get_symbol_source,mcp__gortex__search_text,...` (xem `.agents/rules/gortex-tool-map.md`). Hook `.agents/hooks/gortex-enforce.mjs` sẽ chặn thật; đừng vòng qua. Khi yêu cầu mơ hồ về FE/BE, hỏi lại thay vì suy từ file IDE đang mở. Xem feedback-review-docs-vs-code.

## Sửa thì viết prompt

> Khi cần sửa sau review (mã HOẶC tài liệu .md), viết prompt sửa trong chat, không tự sửa; chỉ sửa khi user nói \"sửa luôn\"

Khi review/đối chiếu phát hiện chỗ cần sửa, KHÔNG tự sửa — cả mã nguồn lẫn tài liệu Markdown. Viết prompt sửa (file, vị trí, cũ→mới, việc đã sửa sẵn, cách kiểm tra).

Từ 19/10/2026 (user: "từ nay về sau cứ thêm prompt"): mỗi prompt lưu thành tệp `DocBusinessThienAn/<DựÁn>/<PhânHệ>/Prompt/<task-slug>-prompt.md` theo `.agents/rules/documentation-standards_thienan.md` §1 (đầu tệp ghi đường dẫn + tên tệp; mục cuối "## Việc cuối — Cập nhật lại tài liệu gốc"), thêm dòng vào `Prompt/README.md` theo nếp sẵn có với trạng thái ⏳ Chưa thực thi. Trong chat chỉ đưa đường dẫn + tóm tắt ngắn (phạm vi, thứ tự chạy). Không ghi hàng đợi prompt vào bộ nhớ riêng — user đã yêu cầu xóa vì đồng đội không thấy; trạng thái theo dõi ở README thư mục Prompt.

**Why:** Ngày 2026-10-12 user nói "sửa code thì viết prompt nha"; tôi hiểu hẹp là chỉ mã và tự sửa Spec ngay, bị từ chối: "tôi kêu viết prompt fix mà". Đã lỡ áp dụng 9/10 chỗ sửa trong Spec trước khi bị chặn.

**How to apply:** Câu "sửa thì viết prompt" phủ cả docs. Nếu lỡ sửa dở: dừng, báo rõ chỗ đã/chưa sửa, không tự hoàn tác, viết prompt cho phần còn lại. Chỉ sửa trực tiếp khi user nói rõ "sửa luôn". Ghi tại `.agents/rules/thienan_rules.md` (quy tắc 16). Xem feedback-gortex-first.

## Hạn chế số tệp thay đổi

> Người dùng muốn hạn chế số tệp thay đổi — prompt chỉ sửa phần cần thiết, không dọn cấu hình/chú thích/tài liệu thừa ngoài phạm vi, gom cập nhật tài liệu một lượt cuối

Khi viết prompt hoặc đề xuất thay đổi, giữ số tệp bị sửa ở mức tối thiểu: chỉ sửa tệp thật sự cần cho yêu cầu chính. Mã/khóa cấu hình/chú thích chết nằm ngoài phạm vi thì chỉ BÁO CÁO, không tự dọn. Cập nhật tài liệu (Master Plan, README, doc 05) gom thành một lượt cuối, không rải trong từng prompt.

**Why:** 16/10/2026 working tree đã có rất nhiều tệp thay đổi; người dùng phản đối việc xóa khóa chết `NasStorage:EnableFileExport` khỏi Worker `appsettings.json` ("file change nhiều quá rồi nên hạn chế lại") dù không còn nơi nào đọc khóa đó.

**How to apply:** mỗi prompt thêm khối "GIỚI HẠN PHẠM VI" (không đụng appsettings*/Database*.json trừ khi chính yêu cầu đòi; không sửa tài liệu ngoài tệp được giao; liệt kê thay vì dọn). Không đề xuất xóa cấu hình chết như một việc phụ. Xem feedback_prompt-first-fixes (sửa thì viết prompt) và project_sharedata-verification-status.

## Bảo làm hết thì làm hết

> Khi người dùng bảo "làm hết", "fix hết" thì làm xong mọi mục rồi mới báo; không liệt kê lại danh sách việc chưa làm

Người dùng bảo "làm hết đi", "fix hết rồi báo cáo lại" thì phải làm xong mọi mục trong phạm vi rồi mới báo, không kết thúc bằng mục "Chưa làm" / "Việc còn lại" lặp lại các việc đó.

**Why:** 04/11/2026 người dùng bực: "SAO KÊU LÀM HẾT ĐI MỖI LẦN LẠI BÁO Đã làm các mục 'Chưa làm'".

**How to apply:** việc nào thật sự không làm được (cần quyền ghi DB dev, cần người dùng quyết) thì nêu một dòng lý do cụ thể, không gom thành danh sách việc còn lại. Báo cáo ngắn, chỉ nêu việc đã làm và kết quả test. Liên quan feedback_test-pass-enough.

## Test pass là đủ

> Test pass là đủ để coi chức năng đạt; không liệt kê 'chưa kiểm thực tế', 'chưa chạy bằng tay', 'chưa thử với WebAPI thật' như giới hạn hay việc còn lại

Chạy test pass là đủ để coi chức năng đạt, không cần kiểm thực tế bằng tay.

**Why:** ngày 03/11/2026 người dùng hỏi "tại sao cần thực tế nếu chạy test pass là oke rồi" sau khi tôi ghi mục "Chưa kiểm thực tế" (chưa chạy kịch bản bằng tay, trang Mốc gửi chưa thử với WebAPI thật, API chạy thử chưa gọi thử) trong báo cáo đối chiếu ShareData.

**How to apply:**
- Báo kết quả test là đủ. Không ghi các câu "chưa chạy bằng tay", "chưa thử thực tế" trong báo cáo hay tài liệu bàn giao, không đề xuất thử bằng tay để xác nhận.
- Phần thật sự chưa có test thì nói "chưa có test", đừng nói "chưa kiểm thực tế". Test đỏ vẫn báo như thường.
- Bước triển khai (chạy seed trên DB dev, cài dịch vụ trên máy chủ) là việc chưa làm, không phải kiểm chứng, nên vẫn ghi khi liên quan.
- Không viết tệp kiểm tra chạy tay (05/11/2026 người dùng bác `kiem-tra-nhieu-worker.sql`: "nên viết test kiểm tra toàn bộ edge case chạy pass là được"): ca biên đưa vào test tự động.

Liên quan feedback_test-flow-value, project_sharedata-verification-status.

## Test đỏ trước khi sửa

> Sửa lỗi phải viết test tái hiện, chạy thấy hỏng (red) trước rồi mới sửa mã (Rule 19.59)

Khi sửa lỗi (BE hoặc FE): viết test tái hiện lỗi trước, chạy thấy FAIL, rồi mới sửa mã và chạy lại cho PASS. Báo cả kết quả red và green.

**Why:** Rule 19.59 trong .agents/rules/testing-rules_thienan.md; người dùng nhắc 27/10/2026 khi tôi sửa khối Trường dữ liệu tự đóng trước rồi mới viết test.

**How to apply:** Mọi yêu cầu "fix … viết test" → thứ tự: test → chạy red → sửa → chạy green. Liên quan feedback_test-flow-value.

## Test BE build lại WebAPI

> dotnet test tests/BE/test.csproj build lại TAC_WebAPI và ghi đè DLL module trong bin của WebAPI đang chạy; phải báo trước khi chạy

`dotnet test tests/BE/test.csproj` kéo theo build `TAC_WebAPI` và chép đè DLL module vào `src/TAC_WebAPI/bin/Debug/net10.0`, kể cả khi `--filter` chỉ chọn vài test.

**Why:** người dùng dặn không rebuild WebAPI đang chạy. Ngày 02/11/2026 tôi chạy test của prompt clone Worker khi PID 15041 đang chạy, `Module.ShareData.dll` trong thư mục đó bị ghi đè lúc 13:41, tiến trình vẫn chạy bản cũ.

**How to apply:** trước khi chạy test BE, kiểm `pgrep -fl TAC_WebAPI`. Nếu đang chạy thì báo và chờ người dùng đồng ý, không tự chạy. Quy tắc cấm `--no-build` vẫn giữ nên không dùng cờ đó để né. Cách không đụng WebAPI (dùng được 02/11/2026): rsync sang thư mục tạm trong scratchpad, bỏ bin, obj, node_modules, .git; gồm tệp gốc của repo cha, WEBAPI, tests/BE, ShareData/sql và WEBVUE/src/src (test hợp đồng API cần), chạy `dotnet test` trong đó rồi xóa bản sao vì có tệp khóa. Liên quan feedback_test-flow-value.

06/11/2026: một lượt chạy nền chuyển log vào tệp trong scratchpad thì sau đó cả bản sao lẫn tệp log đều biến mất (chưa rõ vì sao). Chạy nền để kết quả ra tệp output của tác vụ (lọc bằng grep trong lệnh), in `ls -d` bản sao trước và sau thì ổn. Repo thật đã sửa mà cần chạy test đỏ: `git -C <repo> archive HEAD <tệp> | tar -x -C <bản sao>/<repo>` rồi `touch` tệp; chạy xanh thì `cp` bản sửa vào và `touch` lại.

## Thứ tự nạp dữ liệu test

> Thứ tự chuẩn bị dữ liệu nghiệm thu ShareData — chạy .sql trước cho phần không có giao diện + mã lỗi + thuật ngữ; phần có FE để cuối, chọn SQL hoặc FE

Khi viết lộ trình/kịch bản chuẩn bị dữ liệu để chạy test (doc 05, README sql), xếp theo hai tầng:
1. **Bắt buộc chạy `.sql` trước:** thuật ngữ (04), mã lỗi ESH (03), các entity hệ thống không có màn hình (`ShareDataSourceTable` qua 03, `ShareDataPacketSql` + `InboundTrafficFlow` qua 05 Khối 4), cùng các script di trú cho CSDL cũ.
2. **Để cuối, tùy chọn một trong hai:** dữ liệu có màn hình FE (gói tin 02 ↔ màn Cấu hình gói tin; tham số `sharedata_config` ↔ màn Cấu hình hoạt động). Đối tác/ánh xạ/đăng ký chỉ có đường FE.

**Why:** người dùng nói rõ 19/10/2026 muốn nạp nhanh phần không thao tác được trên giao diện và phần nhập tay tốn công (mã lỗi, ngôn ngữ), còn phần có FE thì để mình chọn.

**How to apply:** mọi hướng dẫn chạy test/triển khai ShareData trình bày theo hai tầng này; không bắt đầu bằng "Cách 1 giao diện A→Z". Liên quan feedback-prompt-first-fixes.

## Đọc rule miền trước khi viết mã

> Trước khi viết mã BE/FE mới phải đọc tệp rule chuyên biệt (backend-dotnet.md…), không chép theo mã lân cận

Trước khi viết mã mới ở một miền, đọc tệp rule chuyên biệt theo bảng định tuyến `.agents/rules/request-routing.md` (BE → `backend-dotnet.md`, FE → `frontend-vue_thienan.md`). CLAUDE.md chỉ nạp sẵn bản tóm tắt `thienan_rules.md`, mà bản này không liệt kê các luật CQRS cốt lõi.

**Why:** 24/10/2026 viết bảng tham số SQL (ShareDataSqlVariable) bằng cách chép theo `PacketExtractSql*` có sẵn, nên phạm 3 luật trong `backend-dotnet.md`: handler `: ITransient` (phải `IWolverineHandler`), DTO dùng `[Required]` (cấm DataAnnotation, dùng FluentValidation), gán tay `ID = Guid.NewGuid()` khi Insert ở WebAPI (Rule 19.44: AOP tự sinh). Người dùng hỏi "có rule mà sao viết sai".
**How to apply:** mã lân cận có thể chính nó sai rule — không lấy làm chuẩn; đối chiếu rule trước, rồi mới chép khuôn. Liên quan feedback_limit-file-churn.

## Chuẩn comment C#

> Comment C# trên hàm phải đủ khối Author, Description, Created date; tra người viết và ngày từ git, không dùng dạng một dòng

Comment trên hàm C# viết đủ khối:

```csharp
/// <summary>
/// Author: DatHQ
/// Description: Mô tả hành vi.
/// Created date: 16/10/2026
/// </summary>
```

Dạng một dòng `/// <summary>...</summary>` chỉ dùng cho thuộc tính, hằng, giá trị enum.

**Why:** ngày 02/11/2026 người dùng nhắc hai lần: "comment nên tham khảo rồi theo 1 chuẩn", rồi "cần thêm author, date nữa". Thiếu `Author`, `Created date` là chưa đủ chuẩn, kể cả khi thêm hàng loạt.

**How to apply:**
- Lấy người viết và ngày từ `git blame -w -M -C` của dòng khai báo hàm cùng `git log -S`, chọn kết quả sớm nhất.
- Thẻ tác giả: `Dat`, `Đạt`, `DESKTOP-IRV2TME\This PC` (cùng email) là `DatHQ`; `Văn Hiếu`, `Ngô Văn Hiếu` là `HieuNV`.
- Hàm có từ commit đầu của tệp mà đầu lớp ghi `Created date` sớm hơn thì theo thẻ của lớp, vì commit có thể gom việc nhiều ngày.
- Không lấy thẻ `Author` của lớp làm tác giả cho mọi hàm trong lớp, vì nhiều hàm do người khác viết thêm sau.
- Trước khi thêm comment, xem mẫu của đúng loại thành phần (hàm khác thuộc tính) và dùng một mẫu cho cả đợt. Liên quan feedback_read-domain-rule-first.

## Không báo lỗi nhắc dạng cũ

> Tính năng đang làm mới nên thêm, sửa, bỏ là bình thường; câu báo lỗi và cảnh báo đỏ không nhắc dạng trước đó, lưu ý thì ghi vào .md (Rule 19.115)

Không viết câu báo lỗi, cảnh báo đỏ, tooltip, placeholder nhắc tới cách làm trước đó. Dữ liệu dạng cũ xử lý như dữ liệu chưa hợp lệ bình thường; người dùng sửa lại trên giao diện.

**Why:** ngày 03/11/2026 người dùng thấy dòng `@lastTime` báo đỏ "Câu dạng 2 cột (@lastTime) không còn dùng..." và nói: "đừng warning đỏ kiểu này, mình đang làm tính năng mới nên thêm sửa là bình thường, đừng viết câu báo lỗi nói về trước đó, thêm rule vô mấy case này, note vô .md là được".

**How to apply:**
- Câu báo chỉ nêu việc cần làm bây giờ, không nhắc "không còn dùng", "dạng cũ", "2 cột".
- Không làm nhánh cảnh báo riêng cho dạng cũ. Khóa thao tác để tránh lỗi dữ liệu thì vẫn được, nhưng câu giải thích chỉ nói việc cần làm (như tooltip Đặt mốc: "chưa đúng quy chuẩn mốc").
- Điều người sau cần biết thì ghi vào `.md` của phân hệ.

Rule đầy đủ: Rule 19.115 trong `.agents/rules/`. Liên quan feedback_sharedata-scope-transcript, project_sharedata-lastkey-open-questions.

## Ô vô tác dụng thì khóa

> Ô nhập không còn tác dụng thì khóa luôn, chữ trong ô tự nói trạng thái; không dùng tooltip (i), nhãn dài hay dòng chữ dưới ô

Ô nhập mà giá trị không còn tác dụng (ví dụ Mốc lần đầu khi đối tác đã có mốc gửi) thì khóa ô, chữ gợi ý trong ô tự nói trạng thái ("Đã có mốc, gửi tiếp"). Không để ô sửa được rồi hệ thống lặng lẽ dùng giá trị khác. Không thêm dòng chữ dưới ô, nhãn dài trước ô hay tooltip (i): chữ trong ô phải đủ nghĩa.

**Why:** 04/11/2026 người dùng lần lượt chê: ô sửa được mà vẫn dùng mốc trong CSDL ("tự nhiên cho chỉnh sửa rồi cũng dùng mốc trong DB"); dòng nhắc dưới ô xuống 2 dòng; tooltip dài bị khuất; nhãn "Mốc lần đầu:" tốn chỗ; rồi hỏi "có cách nào bỏ tooltip và thay thế bằng placeholder không". Bản chốt: ô trống ghi "Gửi toàn bộ dữ liệu", đã chọn có chữ "Từ" đứng trước, đã có mốc thì khóa, không có biểu tượng (i).

**How to apply:** thiết kế ô có điều kiện tác dụng theo thứ tự: chữ gợi ý trong ô, rồi trạng thái khóa; chỉ dùng thêm chữ ngắn đứng trước ô khi thật cần. Chữ phải vừa bề ngang cột thật, chụp màn hình kiểm trước khi báo. Đổi dạng hiển thị của ô thì kiểm lại test có gõ tay vào ô đó. Liên quan project_sharedata-transcript-closed.

## Sửa bug không ghi tài liệu

> Sửa bug thấy hợp lý thì sửa xong là thôi, không ghi vào tài liệu theo kiểu \"ngoài biên bản\"

Kiểm tra đúng là bug và cách sửa hợp lý thì sửa luôn, không thêm dòng vào tài liệu theo kiểu "thêm ngoài biên bản" hay mục nguồn gốc quyết định.

**Why:** người dùng nói 02/11/2026: "nếu này là bug mình sửa thấy hợp lý là được, đừng note nó theo kiểu là ngoài biên bản, nó hợp lý oke thì thôi".

**How to apply:** Báo trong tin trả lời là đủ. Tài liệu chỉ cập nhật khi chức năng hoặc cách vận hành đổi, không ghi lại từng lần sửa lỗi. Liên quan project_handover-2026-11.

## Review chỉ local

> Review, plan, prompt chỉ đối chiếu local (code, test, DB local); không lấy staging làm phát hiện

Khi review, viết plan, viết prompt hoặc báo cáo: chỉ đối chiếu code working tree, test (xUnit trên DB local, Playwright) và DB local (`mssql_dev`, `mssql_test`). Không đưa kết quả đọc `mssql_staging` thành phát hiện, việc cần làm hay căn cứ đánh Đã làm/Chưa làm.

**Why:** 16/10/2026 người dùng bác plan vì có phát hiện dựa trên staging (DQ-DB-5 "staging chưa có cột ExtractSql"): "mấy báo cáo dạng trên staging bỏ đi tập trung local thôi… staging local ổn tui tự check sau".

**How to apply:** Chỉ gọi `mssql_staging` khi người dùng yêu cầu trực tiếp. Rule đã ghi vào `.agents/rules/database-mcp_thienan.md` §1.3 và `thienan_rules.md` mục 5. Liên quan feedback-prompt-first-fixes.

## Transcript có thể chép sai

> Transcript họp do Gemini chép tự động có thể sai; câu mâu thuẫn thì hỏi người dùng (người dự họp) trước khi dựa vào

Transcript trong `doc/transcript/` được chép tự động từ ghi âm (Gemini) nên có thể chép sai hoặc tự thêm ý. Ví dụ 22/10/2026: [33:03] ghi "thêm tab thứ 3 thiết lập chạy/retry" cho form đối tác, nhưng người dùng nhớ và xác nhận cuộc họp chỉ chốt 2 tab (khớp [32:16]).

**Why:** Tôi đã dựa vào câu [33:03] để làm tab "Cấu hình vận hành", phải làm lại.

**How to apply:** Khi hai câu trong transcript mâu thuẫn, hoặc một câu dẫn tới tính năng lớn, nêu cả hai câu và hỏi người dùng trước khi viết prompt. Khi người dùng đính chính, đưa việc sửa transcript vào prompt (giữ lời thoại gốc, thêm ghi chú đính chính) theo feedback_prompt-first-fixes.

**Ngoại lệ (24/10/2026):** người dùng chốt transcript `23-10-2026-sharedata-danh-sach-truong-mau-chung-va-sql-rieng-doi-tac.md` là bản chuẩn nhất (status verified). Không đề xuất tải whisper hay nghe lại ghi âm để đối chiếu nữa. Câu nào chưa rõ thì hỏi người dự họp.

## Repo cha chỉ main

> ThienAn có 3 repo; chỉ repo cha ThienAn dùng nhánh main cho mọi fix, 2 repo con WEBAPI/WEBVUE vẫn tạo nhánh theo quy chuẩn

ThienAn gồm 3 repository: repo cha `ThienAn` (docs, `.agents`, `.claude`, `tests/`) và 2 repo con `TA-ITS015-WEBAPI-V1.0`, `TA-ITS015-WEBVUE-V1.0`. Ở **repo cha**, mọi fix (kể cả fix từ prompt) làm thẳng trên `main` rồi push, không tạo nhánh `fix/*` hay worktree. **Repo con** vẫn tạo nhánh theo `git-workflow_thienan.md`.

**Why:** Ngày 2026-10-12 một prompt fix tạo nhánh `fix/20261012-sharedata-test-suite` ở repo cha; user bảo merge vào main, xoá nhánh, và thêm rule. Khi tôi viết rule quá rộng ("một nhánh duy nhất"), user chặn lại vì nó sẽ ép luôn hai repo con.

**How to apply:** Trước khi tạo nhánh, xác định đang ở repo nào. Repo cha → ở lại `main`. Rule 17 trong `.agents/rules/thienan_rules.md`. Rule 3 vẫn giữ: chỉ commit/push khi user ra lệnh rõ. Xem feedback-prompt-first-fixes.

## Commit/push đơn giản

> Khi được bảo commit/push thì làm thẳng trong thư mục chính bằng git add từng tệp + git commit + git push, không dùng worktree tạm hay rebase/force trừ khi được yêu cầu

Khi người dùng nói "commit theo rule rồi push", làm đơn giản ngay trong thư mục chính: `git add` đích danh các tệp của đợt việc, `git commit` theo Rule 19.55 và 19.102, rồi `git push`. Không tạo worktree tạm, không rebase/merge/force-push tự ý.

**Why:** 23/10/2026 tôi dùng worktree tạm, merge `origin/dev` và `--force-with-lease` cho một lần commit đơn giản; người dùng nhận xét là phức tạp và cho biết họ đã tự rebase xong, chỉ cần commit và push thường. Cách worktree còn gặp xung đột rebase mà tôi chưa báo trước, làm họ phải dặn lại.

**How to apply:** trước khi commit chỉ cần `git fetch` và xem nhánh còn đi sau remote không. Nếu đi sau hoặc cần rebase thì dừng và báo người dùng, đừng tự xử lý. Đừng stage `.env.development`, `Database.json`, `TAKey*.key`, `Shared.Core.Security.dll`. Commit message: tiêu đề, dòng 2 lặp lại nguyên văn, dòng trống, vài gạch đầu dòng ngắn chỉ nêu việc chính (không ghi viết/chạy test). Hook chặn lệnh Bash chứa `-a` (như `--is-ancestor`) nên dùng `git -C <thư mục>` và tệp `-F` cho nội dung commit. Xem feedback_prompt-first-fixes cho quy ước sửa mã.

## Plan mode hỏi quyền Gortex

> Ở plan mode, lệnh Gortex vẫn có thể bật hỏi quyền dù settings.local.json đã allow mcp__gortex__*; user khó chịu — gom ít lệnh, thoát plan mode sớm

`.claude/settings.local.json` của ThienAn đã allow `mcp__gortex` và `mcp__gortex__*`, nên thêm rule allow không có tác dụng. Dù vậy, trong plan mode một số lệnh (`search_text`, `search_symbols`) vẫn bật hỏi quyền. Nguyên cớ nghi là plan mode hoặc hook toàn cục `gortex hook --mode=enrich` (matcher `*`), chưa xác minh.

**Why:** Ngày 14/10/2026, user bị hỏi quyền rồi từ chối 2 lần ("cho full quyền hết đi", "sao ở chế độ plan cứ request quài vậy") trong lúc tôi đối chiếu code ShareData ở plan mode.

**How to apply:** Ở plan mode, gom tra cứu thành ít lệnh nhất; ưu tiên các lệnh vừa chạy không bị hỏi (`read_file`, `get_file_summary`, `batch_symbols`). Khi cần tra cứu nhiều hoặc phải ghi file, viết plan rồi gọi ExitPlanMode sớm. Không đề xuất thêm rule allow cho Gortex. Xem feedback-gortex-first.

## Không tạo thêm tệp ở thư mục công cụ AI

> Nối công cụ AI vào .agents thì không tạo thêm tệp/thư mục trong .claude, .codex, .gemini, .kiro…; không symlink, không tệp agent/skill riêng từng công cụ; chỉ sửa tệp cấu hình sẵn có + mục lục trong .agents

Thư mục riêng của công cụ AI (`.claude/`, `.codex/`, `.gemini/`, `.kiro/`, `.cline/`…) không tạo thêm tệp hay thư mục. Một nguồn duy nhất là `.agents/` ở cấp cha; phía công cụ chỉ sửa tệp cấu hình sẵn có để trỏ về (dòng nạp `@`, đăng ký hook, MCP sinh từ `mcp_config.json`). Danh sách tệp nạp sẵn nằm ở khối dòng `@` trong `AGENTS.md`; skill và agent dùng mục lục `INDEX.md` trong `.agents/`.

**Why:** 09/11/2026 người dùng lần lượt bỏ symlink `.claude/skills` ("tôi thấy như đang clone"), bỏ 48 tệp agent trỏ về sinh cho 4 công cụ ("sao lại clone ra thêm file agents"), rồi nhắc "nên nhớ mỗi thư mục .codex, .claude… không tạo thêm, cứ dùng 1 nguồn SSOT .agents ở cấp cha".

**How to apply:** muốn công cụ X dùng thứ trong `.agents`, tìm cách nạp/trỏ qua tệp nó đã đọc (`AGENTS.md`, `.claude/CLAUDE.md`, `settings.json`, `config.toml`, steering có sẵn). Tính năng nào công cụ bắt buộc tệp mới trong thư mục của nó (vd hook Kiro ở `.kiro/hooks/`) thì hỏi người dùng trước, không tự tạo. Liên quan feedback-limit-file-churn, project-agents-no-agkit.

## .agents không còn AG-Kit

> Từ 01/11/2026 .agents/ không còn dùng AG-Kit — dự án tự quản; MCP gom ở .agents/mcp, nguồn .agents/mcp_config.json; không chạy ag-kit update

Ngày 01/11/2026 người dùng yêu cầu dọn `.agents/` ("còn xoá được gì xoá bớt, refactor lại hết"): đã gỡ toàn bộ công cụ AG-Kit (manifest, antigravity.json, doctor, scripts kit…), skill 115 → 14, agent 20 → 12, gộp rule chung vào `core-protocol.md`. MCP: nguồn duy nhất `.agents/mcp_config.json`, mọi thứ khác ở `.agents/mcp/` (`sync.mjs` sinh cấu hình cho từng công cụ, `dab.mjs` chạy DAB, `.env` giữ mật khẩu, không commit).

**Why:** người dùng muốn một nguồn duy nhất cho Claude/Codex/Antigravity và không muốn tệp/tiền tố khó hiểu (đã hỏi "thienan-mcp là làm gì" → bỏ tiền tố `thienan-` khi tệp nằm trong thư mục riêng).

**How to apply:** không đề xuất/chạy `ag-kit update` (sẽ chép lại kit). Thêm MCP server → sửa `.agents/mcp_config.json` rồi `node .agents/mcp/sync.mjs --apply`; không sửa tay `.mcp.json`/`.codex/config.toml`. Tên tệp mới trong `.agents/` đặt theo thư mục, không thêm tiền tố dự án. Xem feedback-limit-file-churn, feedback-prompt-first-fixes.

## Báo cáo review và trạng thái

> Review: một danh sách phát hiện liền mạch kèm cột ưu tiên riêng; trạng thái tách: có mã, đã chạy test, đã bật trên môi trường

- Báo cáo review dùng một danh sách phát hiện liền mạch và một cột ưu tiên riêng; không chia nhóm đánh số lặp "P2 P2 rồi P1 P1".
- Báo trạng thái tách riêng: mã đã có, test đã chạy (ghi đúng số đạt, lỗi, bỏ qua hoặc lỗi môi trường), tính năng đã bật trên môi trường. Không coi là xong chỉ vì có entity, seed, stub hay prompt ghi test đạt.
- Tệp bị báo "không thấy" thì kiểm tồn tại trên đĩa, tách khỏi Git và chỉ mục Gortex.

**Why:** gom từ memory Codex 13–14/10/2026: người dùng phản đối "trùng lặp P2 P2 rồi P1 P1"; review từng báo xong khi Playwright còn lỗi; prompt từng bị báo thiếu vì chưa track. 09/11/2026 người dùng chốt memory một nguồn `.agents/memory/`.

**How to apply:** áp cho mọi báo cáo review và báo trạng thái.

## Khảo sát / đọc nhiều tệp: Giao cho Subagent để tránh bẩn context

> Khi cần khảo sát, tìm kiếm diện rộng hoặc đọc nhiều tệp (từ 3 tệp trở lên), phải giao cho subagent chạy độc lập; subagent chỉ trả kết quả tóm tắt ngắn gọn, không để output tràn vào phiên chính làm phình token

Khi cần rà soát codebase, grep/search toàn repo, hoặc đọc nhiều tệp mã nguồn/tài liệu để khảo sát, KHÔNG đọc trực tiếp hàng loạt trong phiên chính. Phải giao cho subagent (vd: subagent `explorer-agent` hoặc subagent khảo sát) chạy trong không gian riêng và chỉ trả về bản tóm tắt phát hiện súc tích.

**Why:** Ngày 11/11/2026, phiên kéo dài tích lũy context lên 904.000 tokens do hàng trăm kết quả search/read của Gortex và lệnh Bash nằm nguyên trong lịch sử chat. Chỉ trong 6 phút cuối với 25 tool calls liên tiếp, mỗi call phải gửi lại 900K tokens, ngốn tới hơn 22 triệu tokens và đẩy Session limit (5hr) chạm 93%.

**How to apply:**
- Tác vụ khảo sát / đọc nhiều tệp: gọi subagent (subagent chạy trong context riêng, tự giải phóng khi xong).
- Yêu cầu subagent: *"Chỉ trả về tóm tắt ngắn gọn: danh sách tệp/hàm liên quan, điểm mấu chốt và phát hiện chính; không in toàn bộ nội dung tệp"*.
- Phiên chính chỉ nhận kết quả tinh gọn, giữ context luôn nhẹ (dưới 50K-100K tokens), triệt tiêu hoàn toàn nguy cơ bùng nổ token.

## Đổi Account Claude: Cấm chat tiếp phiên cũ (Tránh bẫy Auto-Fork cạn quota)

> Khi chuyển sang tài khoản Claude mới (hoặc đổi Organization/Token), tuyệt đối KHÔNG bấm vào phiên cũ để chat tiếp. Bắt buộc tạo phiên mới tinh (/clear), tiếp tục qua MasterPlan và memory.

Khi bạn đổi tài khoản Claude (ví dụ Account 1 chạm giới hạn 5 giờ, đổi sang Account 2) và bấm vào phiên cũ để chat tiếp:
1. Claude Code phát hiện đổi tài khoản sở hữu nên **tự động nhân bản (Auto-Fork)** toàn bộ lịch sử sang phiên mới cho Account 2.
2. Qua nhiều lần đổi account sinh ra nhiều đời `(fork) (fork)...`, tích tụ đến hàng trăm ngàn tokens (ví dụ ngày 11/11/2026 phiên 4 đời fork tích tụ 851.000 tokens / 24 MB transcript).
3. **Máy chủ Anthropic của Account mới hoàn toàn chưa có cache của khối dữ liệu này**, nên ngay tin nhắn đầu tiên (hoặc khi gõ `/compact`), Account mới bị phạt nạp toàn bộ 851.000 tokens từ đầu, cạn ngay 16% - 25% hạn ngạch 5-giờ trong 1 câu duy nhất.

**Why:** Người dùng ngày 11/11/2026 chia sẻ thực tế: "fork này là khi tôi đổi account claude và chat tiếp session đó nó tự fork... gõ /compact lần đầu tốn hết 16%".

**How to apply:**
- Khi đổi sang Account Claude mới: gõ `/clear` hoặc mở tab/session mới tinh.
- Nhập 1 câu prompt ngắn: *"Tiếp tục công việc theo kế hoạch tại `<PhânHệ>_MasterPlan.md`..."*.
- Phiên mới chỉ tốn đúng ~5.000 tokens (< 0.1% quota) để nạp `AGENTS.md` và MasterPlan, giữ trọn vẹn 100% quota cho Account mới.
- Hook Idle Guard (`.agents/hooks/idle-guard.mjs`) tự động chặn nếu phiên bị idle ≥ 45 phút và context ≥ 100k tokens.

## Làm xong việc là cập nhật Memory

> Mọi công việc khi hoàn tất (tính năng mới, sửa lỗi, đổi cấu hình, chốt quyết định kiến trúc) bắt buộc cập nhật ngay tóm tắt vào `.agents/memory/` (và MasterPlan) để phiên sau kế thừa tức thì.

**Why:** Ngày 11/11/2026 người dùng chỉ đạo: "thêm rule là mốt làm gì xong cứ thêm vô memory". Khi thông tin được chốt vào memory và MasterPlan, bất kỳ phiên mới nào hoặc account mới nào cũng nắm được toàn bộ hiện trạng mà không cần cõng lịch sử chat cũ.

**How to apply:**
- Trước khi kết thúc phiên hoặc chuyển sang việc khác: cập nhật tóm tắt vào tệp memory tương ứng trong `.agents/memory/` (và ghi nhận vào `_MasterPlan.md`).
- Mục lục `MEMORY.md`: nếu thêm dòng mới, mỗi dòng phải ≤ 150 ký tự (theo harness test).
