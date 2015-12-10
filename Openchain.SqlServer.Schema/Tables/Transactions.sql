CREATE TABLE [Openchain].[Transactions]
(
    [Id] BIGINT NOT NULL IDENTITY,
    [Instance] INT NOT NULL,
    [TransactionHash] BINARY(32) NOT NULL,
    [MutationHash] BINARY(32) NOT NULL,
    [RawData] VARBINARY(MAX) NOT NULL,
)
