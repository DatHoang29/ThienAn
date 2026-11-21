CREATE TABLE ServiceData
(
    Id          BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Source      NVARCHAR(50) NOT NULL,
    MessageId   NVARCHAR(200) NOT NULL,
    OccurredAt  DATETIME2 NOT NULL,
    DataJson    NVARCHAR(MAX) NOT NULL,
    CreateTime  DATETIME2 NOT NULL
);

CREATE UNIQUE INDEX UX_ServiceData_Source_MessageId
ON ServiceData(Source, MessageId);
