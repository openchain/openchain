CREATE TABLE [Openchain].[RecordMutations]
(
    [Instance] INT NOT NULL,
    [RecordKey] VARBINARY(512) NOT NULL,
    [TransactionId] BIGINT NOT NULL,

    CONSTRAINT [PK_RecordMutations]
    PRIMARY KEY ([Instance], [RecordKey], [TransactionId]),

    CONSTRAINT [FK_RecordMutations_Transactions]
    FOREIGN KEY ([TransactionId]) REFERENCES [Openchain].[Transactions] ([Id]),
)
GO