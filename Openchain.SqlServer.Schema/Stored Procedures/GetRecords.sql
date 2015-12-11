CREATE PROCEDURE [Openchain].[GetRecords]
    @instance INT,
    @ids [Openchain].[IdTable] READONLY
AS

    SELECT [Key], [Value], [Version]
    FROM [Openchain].[Records]
    INNER JOIN @ids AS Ids ON Records.[Key] = Ids.[Id]
    WHERE Records.[Instance] = @instance

RETURN
