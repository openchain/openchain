CREATE PROCEDURE [Openchain].[GetTransactionLog]
    @instance INT,
    @from VARBINARY(512),
    @count INT
AS
    SET NOCOUNT ON;

    IF @from IS NULL
        SELECT TOP(@count) Transactions.[RawData]
        FROM [Openchain].[Transactions]
        WHERE Transactions.[Instance] = @instance
        ORDER BY Transactions.[Id];
    ELSE
        SELECT TOP(@count) Transactions.[RawData]
        FROM [Openchain].[Transactions]
        WHERE Transactions.[Instance] = @instance
            AND Transactions.[Id] >
                (SELECT Transactions.[Id]
                FROM [Openchain].[Transactions]
                WHERE Transactions.[Instance] = @instance AND Transactions.[TransactionHash] = @from)
        ORDER BY Transactions.[Id];

RETURN
