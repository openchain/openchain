CREATE PROCEDURE [Openchain].[GetRecordsFromKeyPrefix]
    @instance INT,
    @prefix VARBINARY(512)
AS
    SET NOCOUNT ON;

    SELECT Records.[Key], Records.[Value], Records.[Version]
    FROM [Openchain].[Records]
    WHERE Records.[Instance] = @instance AND LEFT(Records.[Key], LEN(@prefix)) = @prefix;

RETURN
