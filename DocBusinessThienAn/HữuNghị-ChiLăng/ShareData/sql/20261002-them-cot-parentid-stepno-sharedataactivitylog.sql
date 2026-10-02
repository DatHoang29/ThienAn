/* ============================================================================
   Them 2 cot ParentId + StepNo cho bang ShareDataActivityLog (log cha - con)
   Task: BE-5 (Sharedata_MasterPlan.md dong 573)
   Ngay soan: 02/10/2026
   CSDL dich: CSDL CHINH (mac dinh) - vi du staging DEV_ITS10 tai 10.10.8.30
   Pham vi: CHI bang ShareDataActivityLog. CHI THEM cot va index, KHONG sua du lieu.
   An toan chay lai nhieu lan (idempotent).

   !! KHONG PHAI script nay: file 20261002-them-cot-receivelogid-sharedatainboundpacket.sql
      tac dong bang ShareDataInboundPacket o CSDL INBOUND (ket noi khac han).
      Hai file lam hai viec khac nhau tren hai CSDL khac nhau - chay du ca hai.
   ============================================================================ */

SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.ShareDataActivityLog', N'U') IS NULL
BEGIN
    RAISERROR(N'Khong tim thay bang ShareDataActivityLog. Dung lai, khong lam gi.', 16, 1);
    RETURN;
END

/* ---- Cot ParentId: ID cua dong log cha. NULL = chinh no la log cha ---- */
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID(N'dbo.ShareDataActivityLog')
                 AND name = N'ParentId')
BEGIN
    ALTER TABLE dbo.ShareDataActivityLog ADD ParentId NVARCHAR(64) NULL;
    PRINT N'Da them cot ParentId NVARCHAR(64) NULL.';
END
ELSE
    PRINT N'Cot ParentId da ton tai - bo qua.';

/* ---- Cot StepNo: 1 = buoc 1, 2 = buoc 2. NULL = log cha ----
   LUU Y: INT, KHONG co do dai. Gan do dai cho kieu so se gay loi SQL 2716. */
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID(N'dbo.ShareDataActivityLog')
                 AND name = N'StepNo')
BEGIN
    ALTER TABLE dbo.ShareDataActivityLog ADD StepNo INT NULL;
    PRINT N'Da them cot StepNo INT NULL.';
END
ELSE
    PRINT N'Cot StepNo da ton tai - bo qua.';

/* ---- Index cho truy van cay cha - con (WHERE ParentId = @id ORDER BY StepNo) ----
   Loc ParentId IS NOT NULL: index chi phuc vu dong CON, nen khong om ca bang. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID(N'dbo.ShareDataActivityLog')
                 AND name = N'index_ShareDataActivityLog_ParentId')
BEGIN
    CREATE NONCLUSTERED INDEX index_ShareDataActivityLog_ParentId
        ON dbo.ShareDataActivityLog (ParentId, StepNo)
        WHERE ParentId IS NOT NULL;
    PRINT N'Da tao index index_ShareDataActivityLog_ParentId.';
END
ELSE
    PRINT N'Index index_ShareDataActivityLog_ParentId da ton tai - bo qua.';

/* ---- Doi chieu ket qua ---- */
SELECT name AS ColumnName, TYPE_NAME(system_type_id) AS DataType, max_length, is_nullable
FROM   sys.columns
WHERE  object_id = OBJECT_ID(N'dbo.ShareDataActivityLog')
  AND  name IN (N'ParentId', N'StepNo');
