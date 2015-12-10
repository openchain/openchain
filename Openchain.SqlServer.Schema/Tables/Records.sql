CREATE TABLE [Openchain].[Records]
(
    [Id] INT NOT NULL IDENTITY,
    [Instance] INT NOT NULL,
    [Key] VARBINARY(512) NOT NULL,
    [Value] VARBINARY(MAX) NOT NULL,
    [Version] VARBINARY(32) NOT NULL,
    [Name] VARCHAR(512) NOT NULL,
    [Type] TINYINT NOT NULL,
)
