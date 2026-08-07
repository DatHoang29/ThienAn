-- ============================================================================
-- FILE: 00_TEST_CHAN_DOAN.SQL
-- MUC DICH: Khoanh vung vi sao 01_CREATE_TABLES khong tao duoc bang.
--           File nay CO Y viet khong dau, khong emoji, khong BEGIN/END long nhau
--           de loai tru moi nguyen nhan ve ma hoa va cu phap.
-- CACH DUNG: Chay TOAN BO file. Doc ket qua tung buoc o tab Results/Messages.
-- ============================================================================

-- BUOC 1: Dang o dau, la ai
SELECT
    DB_NAME()                      AS DangKetNoiToiDatabase,
    SUSER_SNAME()                  AS DangDangNhapBang,
    @@SERVERNAME                   AS TenServer,
    @@VERSION                      AS PhienBan;
GO

-- BUOC 2: Co quyen tao bang trong database nay khong
SELECT
    HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'CREATE TABLE') AS CoQuyenCreateTable,
    HAS_PERMS_BY_NAME('dbo', 'SCHEMA', 'ALTER')              AS CoQuyenAlterSchemaDbo,
    IS_SRVROLEMEMBER('sysadmin')                             AS LaSysadmin;
GO

-- BUOC 3: Thu tao 1 bang that don gian
IF OBJECT_ID('dbo.ShareDataTestTam', 'U') IS NOT NULL
    DROP TABLE dbo.ShareDataTestTam;
GO

CREATE TABLE dbo.ShareDataTestTam (
    ID   NVARCHAR(64) NOT NULL PRIMARY KEY,
    Ghi  NVARCHAR(50) NULL
);
GO

-- BUOC 4: Bang vua tao co that su ton tai khong
SELECT
    CASE WHEN OBJECT_ID('dbo.ShareDataTestTam', 'U') IS NULL
         THEN 'THAT BAI - khong tao duoc bang'
         ELSE 'THANH CONG - tao bang duoc binh thuong'
    END AS KetQuaBuoc3,
    DB_NAME() AS TaoTrongDatabase;
GO

-- BUOC 5: Dem bang ShareData hien co
SELECT COUNT(*) AS SoBangShareDataHienCo
FROM sys.tables WHERE name LIKE 'ShareData%';
GO

-- BUOC 6: Don dep bang thu nghiem
IF OBJECT_ID('dbo.ShareDataTestTam', 'U') IS NOT NULL
    DROP TABLE dbo.ShareDataTestTam;
GO

-- ============================================================================
-- DOC KET QUA
--   BUOC 1 khac 'dev_its10'          -> dang chay nham database
--   BUOC 2 co cot nao = 0            -> thieu quyen tao bang
--   BUOC 4 bao THAT BAI              -> moi truong chan DDL
--   BUOC 4 THANH CONG, BUOC 5 = 0    -> script 01 chet giua chung,
--                                       xem tab Messages tim dong mau do dau tien
-- ============================================================================
