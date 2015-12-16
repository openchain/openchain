CREATE PROCEDURE [Openchain].[GetTransaction]
    @instance INT,
    @mutationHash BINARY(32)
AS
    SET NOCOUNT ON;

    SELECT [RawData]
    FROM [Openchain].[Transactions]
    WHERE Transactions.[Instance] = @instance AND Transactions.[MutationHash] = @mutationHash;

RETURN;
