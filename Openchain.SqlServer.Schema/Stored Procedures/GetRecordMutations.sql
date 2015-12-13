CREATE PROCEDURE [Openchain].[GetRecordMutations]
    @instance INT,
    @recordKey VARBINARY(256)
AS
    SET NOCOUNT ON;

    SELECT Transactions.[MutationHash]
    FROM [Openchain].[RecordMutations]
    INNER JOIN [Openchain].[Transactions] ON RecordMutations.[TransactionId] = Transactions.[Id]
    WHERE RecordMutations.[Instance] = @instance AND RecordMutations.[RecordKey] = @recordKey;

RETURN
