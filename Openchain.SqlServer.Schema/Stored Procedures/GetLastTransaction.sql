CREATE PROCEDURE [Openchain].[GetLastTransaction]
    @instance INT
AS
    SET NOCOUNT ON

    SELECT TOP(1) Transactions.[TransactionHash]
    FROM [Openchain].[Transactions]
    WHERE Transactions.[Instance] = @instance
    ORDER BY Transactions.[Id] DESC

RETURN
