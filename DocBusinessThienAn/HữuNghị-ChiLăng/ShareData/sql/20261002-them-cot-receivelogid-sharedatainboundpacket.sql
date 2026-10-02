/* ============================================================================
   Them cot ReceiveLogId cho bang ShareDataInboundPacket (noi log cha - con chieu NHAN)
   Task: SV-8b (Sharedata_MasterPlan.md dong 109)
   Ngay soan: 02/10/2026
   CSDL dich: CSDL INBOUND - KET NOI RIENG, KHONG phai CSDL chinh.
             Entity khai [Tenant(Its015Const.ConnectionConst.ShareData)].
             Xem ConnectionStrings:InboundConnection trong appsettings cua ShareDataWorker.
   Pham vi: CHI bang ShareDataInboundPacket. CHI THEM 1 cot, KHONG sua du lieu.
   An toan chay lai nhieu lan (idempotent).

   !! KHONG PHAI script nay: file 20261002-them-cot-parentid-stepno-sharedataactivitylog.sql
      tac dong bang ShareDataActivityLog o CSDL CHINH.
      Hai file lam hai viec khac nhau tren hai CSDL khac nhau - chay du ca hai.

   Vi sao can cot nay: chieu nhan ghi 2 buoc o 2 TIEN TRINH khac nhau
   (WebAPI ghi buoc Tiep nhan, Worker ghi buoc Anh xa & ghi CSDL). Hai bang
   ShareDataActivityLog va ShareDataInboundPacket nam o 2 CSDL khac nhau nen
   KHONG join duoc. Cot nay mang ID cua dong log CHA theo dong goi tin, de
   Worker biet gan dong CON vao dau.

   Du lieu cu: cac dong da co se co ReceiveLogId = NULL. Worker phai chap nhan
   truong hop NULL va ghi nhat ky theo cach cu (1 dong roi), KHONG duoc vang loi.
   ============================================================================ */

SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.ShareDataInboundPacket', N'U') IS NULL
BEGIN
    RAISERROR(N'Khong tim thay bang ShareDataInboundPacket. Co the ban dang chay tren CSDL chinh thay vi CSDL Inbound. Dung lai, khong lam gi.', 16, 1);
    RETURN;
END

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID(N'dbo.ShareDataInboundPacket')
                 AND name = N'ReceiveLogId')
BEGIN
    ALTER TABLE dbo.ShareDataInboundPacket ADD ReceiveLogId NVARCHAR(64) NULL;
    PRINT N'Da them cot ReceiveLogId NVARCHAR(64) NULL.';
END
ELSE
    PRINT N'Cot ReceiveLogId da ton tai - bo qua.';

/* ---- Doi chieu ket qua ---- */
SELECT name AS ColumnName, TYPE_NAME(system_type_id) AS DataType, max_length, is_nullable
FROM   sys.columns
WHERE  object_id = OBJECT_ID(N'dbo.ShareDataInboundPacket')
  AND  name = N'ReceiveLogId';
