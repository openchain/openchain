CREATE PROCEDURE [Openchain].[GetRecordsFromKeyPrefix]
    @instance INT,
    @from VARBINARY(512),
    @to VARBINARY(512)
AS
    SET NOCOUNT ON;

    SELECT Records.[Key], Records.[Value], Records.[Version]
    FROM [Openchain].[Records]
    WHERE Records.[Instance] = @instance AND Records.[Key] >= @from AND Records.[Key] < @to;

RETURN
