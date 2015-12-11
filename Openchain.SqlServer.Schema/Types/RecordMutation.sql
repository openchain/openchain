CREATE TYPE [Openchain].[RecordMutation] AS TABLE
(
    [Key] VARBINARY(512) NOT NULL,
    [Value] VARBINARY(MAX) NULL,
    [Version] VARBINARY(32) NOT NULL,
    [Name] VARCHAR(512) NOT NULL,
    [Type] TINYINT NOT NULL
)
GO