CREATE TABLE [Openchain].[RecordMutations]
(
    [Instance] INT NOT NULL,
    [RecordKey] VARBINARY(512) NOT NULL,
    [TransactionId] BIGINT NOT NULL,

    CONSTRAINT [PK_RecordMutations]
    PRIMARY KEY ([Instance], [RecordKey], [TransactionId]),

    CONSTRAINT [FK_RecordMutations_Transactions]
    FOREIGN KEY ([Instance], [TransactionId]) REFERENCES [Openchain].[Transactions] ([Instance], [Id]),
)
