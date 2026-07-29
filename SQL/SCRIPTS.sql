USE [test];
GO

-- 1. SELECT KIỂM TRA
SELECT TOP (1000) [ID]
      ,[Name]
      ,[Kind]
      ,[DbRef]
      ,[TableOrView]
      ,[ColumnsJson]
      ,[QueryText]
      ,[TopN]
      ,[TenantId]
      ,[Code]
      ,[CreateTime]
      ,[CreateUId]
      ,[UpdateTime]
      ,[UpdateUId]
      ,[RowStatus]
      ,[IsDelete]
      ,[Notes]
  FROM [dbo].[EshDataSource];

-- 2. INSERT TEST DATA DÀI > 1024
DECLARE @ColumnsJson NVARCHAR(MAX) = N'{"test_long_data":"' + REPLICATE(N'A', 700) + N'"}';
DECLARE @QueryText NVARCHAR(MAX) = N'SELECT * FROM Table WHERE Code = ''' + REPLICATE(N'X', 700) + N'''';

INSERT INTO [dbo].[EshDataSource] (
    [ID],
    [Name],
    [Kind],
    [ColumnsJson],
    [QueryText],
    [TopN]
)
VALUES (
    N'DS001',
    N'Test Data Exceed 1024',
    N'FIELD_PICKER',
    @ColumnsJson,
    @QueryText,
    100
);

-- 3. DROP INDEX AN TOÀN
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'index_EshDataSource_CT' AND object_id = OBJECT_ID('dbo.EshDataSource'))
    DROP INDEX [index_EshDataSource_CT] ON [dbo].[EshDataSource];

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EshDataSource_Code' AND object_id = OBJECT_ID('dbo.EshDataSource'))
    DROP INDEX [IX_EshDataSource_Code] ON [dbo].[EshDataSource];

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_EshDataSource_Code' AND object_id = OBJECT_ID('dbo.EshDataSource'))
    DROP INDEX [UX_EshDataSource_Code] ON [dbo].[EshDataSource];