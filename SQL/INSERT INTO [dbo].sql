INSERT INTO [dbo].[EshDataSource] (
    [ID],
    [Name],
    [Kind],
    [ColumnsJson],
    [QueryText],
    [TopN]
)
VALUES (
    N'DS001',                                       -- ID (nvarchar(64))
    N'Test Data Exceed 1024',                       -- Name (nvarchar(256))
    N'SQL',                                         -- Kind (nvarchar(32))
    
    -- Tạo chuỗi JSON giả dài 2000 ký tự vào ColumnsJson
    N'{"test_long_data":"' + REPLICATE(N'A', 700) + N'"}', 
    
    -- Tạo chuỗi Query giả dài 2000 ký tự vào QueryText
    N'SELECT * FROM Table WHERE Code = ''' + REPLICATE(N'X', 700) + N'''', 
    
    100                                             -- TopN (int)
);