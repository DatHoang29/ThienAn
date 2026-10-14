/* =============================================================================
   Seed: Gói tin 105 — Dữ liệu định danh phương tiện (AVI/RFID)
   Bảng đích : ShareDataPacket   (bảng thuộc phân hệ ShareData — đúng phạm vi)
   Ngày tạo  : 24/09/2026

   VÌ SAO CẦN SCRIPT NÀY
   ---------------------
   Đo trên mssql_staging (dev_its10 @ 10.10.8.30) ngày 24/09/2026: bản ghi
   '104_rfidData' (mã lệch quy ước, thực chất là gói 105) đã bị xoá vì là dữ liệu
   rác, và chưa có bản ghi '105_rfidData' thay thế. Dãy OrderNo hiện chạy
   1,2,3,4,[thiếu 5],6,7,8,9,100.

   Hệ quả: DataOutboundService.ResolveActivePacket không phân giải được gói 105
   nên luồng gửi bỏ qua gói này hoàn toàn.

   !! CẢNH BÁO — ĐỌC TRƯỚC KHI CHẠY
   --------------------------------
   1. Script này do LẬP TRÌNH VIÊN tự chạy tay sau khi rà soát. Kiểm tra kỹ đang
      kết nối đúng database trước khi thực thi.
   2. Chạy được nhiều lần an toàn (có chốt IF NOT EXISTS), không sinh bản ghi trùng.
   3. Seed bản ghi gói tin là CHƯA ĐỦ để gói 105 gửi được dữ liệu. Còn hai việc
      độc lập khác, xem mục "SAU KHI CHẠY SCRIPT NÀY" ở cuối file.
   4. Cột ID trong hệ thống lưu dạng chuỗi. Script dùng chuỗi cố định (không dùng
      NEWID()) để chạy lại nhiều lần vẫn cho cùng một ID, và để tham chiếu được
      từ các script khác.
============================================================================= */

SET NOCOUNT ON;

DECLARE @PacketId   NVARCHAR(50)  = N'3F7B2C91-8D4E-4A15-B6C3-0A5E9D71F204';
DECLARE @PacketCode NVARCHAR(128) = N'105_rfidData';

/* ---------------------------------------------------------------------------
   Bước 1 — Chốt chặn: không tạo trùng.
   Kiểm cả 3 dạng có thể đã tồn tại:
     - đúng mã chuẩn '105_rfidData'
     - mã lệch quy ước cũ '104_rfidData' (bí danh của cùng gói 105)
     - bất kỳ bản ghi nào đang giữ OrderNo = 5
--------------------------------------------------------------------------- */
IF EXISTS (
    SELECT 1
    FROM   ShareDataPacket
    WHERE  IsDelete IS NULL
      AND (Code IN (N'105_rfidData', N'104_rfidData') OR OrderNo = 5)
)
BEGIN
    PRINT N'BO QUA: da ton tai ban ghi cho goi 105 (theo Code 105_rfidData / 104_rfidData, hoac OrderNo = 5).';
    PRINT N'Xem ket qua truy van kiem tra o Buoc 3 de biet ban ghi nao dang giu cho.';
END
ELSE
BEGIN
    INSERT INTO ShareDataPacket
    (
        ID,
        Code,
        Name,
        PacketVersion,
        Description,
        OrderNo,
        Status,
        Remark,
        TenantId,
        CreateTime,
        CreateUId,
        UpdateTime,
        UpdateUId,
        RowStatus,
        IsDelete
    )
    VALUES
    (
        @PacketId,
        @PacketCode,
        N'Gói 105 - Dữ liệu định danh phương tiện (AVI/RFID)',
        N'1.0',
        N'Định danh phương tiện qua eTag/RFID tại trạm thu phí: mã giao dịch, mã eTag, biển số, loại xe, làn, trạm.',
        5,
        1,                      -- Status = 1 (Enable), khớp 9 bản ghi gói còn lại
        N'TẠM: cần xác nhận lại Name/Description/PacketVersion thật.',
        N'Default',             -- khớp TenantId của các bản ghi gói hiện có
        GETDATE(),
        N'seed',                -- khớp CreateUId của các bản ghi gói hiện có
        NULL,
        NULL,
        NULL,
        NULL
    );

    PRINT N'DA TAO: ban ghi goi 105_rfidData voi OrderNo = 5.';
END

/* ---------------------------------------------------------------------------
   Bước 2 — Truy vấn kiểm tra: toàn bộ danh mục gói tin sau khi chạy.
   Kỳ vọng: dãy OrderNo liền mạch 1..10, không còn thiếu số 5.
--------------------------------------------------------------------------- */
SELECT
    OrderNo,
    Code,
    Name,
    Status,
    TenantId,
    CreateUId,
    IsDelete
FROM   ShareDataPacket
ORDER BY
    CASE WHEN OrderNo IS NULL THEN 1 ELSE 0 END,
    OrderNo;

/* ---------------------------------------------------------------------------
   Bước 3 — Hoàn tác, nếu cần.
   Bỏ chú thích khối dưới để xoá MỀM bản ghi vừa tạo (giữ lại lịch sử).
   KHÔNG dùng DELETE cứng: hệ thống lọc theo IsDelete IS NULL ở mọi truy vấn.
--------------------------------------------------------------------------- */
-- UPDATE ShareDataPacket
-- SET    IsDelete   = GETDATE(),
--        UpdateTime = GETDATE(),
--        UpdateUId  = N'seed-rollback'
-- WHERE  Code     = N'105_rfidData'
--   AND  IsDelete IS NULL;


/* =============================================================================
   SAU KHI CHẠY SCRIPT NÀY — còn hai việc nữa, độc lập với nhau
   =============================================================================

   (A) SỬA TRUY VẤN GÓI 105 — việc của lập trình viên, KHÔNG phải SQL
       QueryPacket105 hiện chạy FROM TollTransactionOut, mà bảng đó có 0 dòng.
       6 bản ghi RFID thật nằm ở TollTransactionIn (23/09, trạm HNCL, làn H1) —
       bảng mà truy vấn không hề đọc.
       Chưa sửa thì gói vẫn gửi 0 bản ghi dù đã có bản ghi cấu hình.
       Prompt thi công:
         DocBusinessThienAn/HữuNghị-ChiLăng/ShareData/Prompt/
           goi-105-doc-tolltransactionin-prompt.md

   (B) TẠO ĐĂNG KÝ TRỎ TỚI GÓI 105 — chưa có đăng ký nào
       Đo 24/09: cả 11 dòng ShareDataSubscription chỉ trỏ vào 2 gói:
         1EC77A6E-051D-43BA-A5B9-46A01AFC267A  = 101_commonData  (8 đăng ký)
         8B2C1C8A-433A-447E-87CD-8B88616D6BC7  = 102_cctvData    (3 đăng ký)
       Không đăng ký nào trỏ tới gói RFID, nên script này KHÔNG cần sửa tham
       chiếu mồ côi nào. Nhưng cũng nghĩa là gói 105 chưa ai đặt hàng.

       Tìm đối tác để gắn đăng ký:
         SELECT ID, Code, Name, Status, SessionState
         FROM   ShareDataPartner
         WHERE  IsDelete IS NULL;

       Mẫu đăng ký — điền <PARTNER_ID> rồi tự rà trước khi chạy:
       -- INSERT INTO ShareDataSubscription
       --     (ID, PartnerId, DatatypeId, Direction, State, SendOnNewData,
       --      IntervalSeconds, TenantId, CreateTime, CreateUId)
       -- VALUES
       --     (NEWID(), N'<PARTNER_ID>',
       --      N'3F7B2C91-8D4E-4A15-B6C3-0A5E9D71F204',   -- ID gói 105 ở trên
       --      0,        -- Direction = Outbound
       --      1,        -- State = Active
       --      1,        -- SendOnNewData: 1 = gửi ngay khi có dữ liệu mới
       --      30,       -- IntervalSeconds
       --      N'Default', GETDATE(), N'seed');

       Lưu ý: gói 105 là gói BẢN CHỤP nên mỗi lượt gửi lại toàn bộ hiện trạng,
       đã bị chặn trên 100 dòng mới nhất (SnapshotTopLimits, thi công 24/09).
============================================================================= */
