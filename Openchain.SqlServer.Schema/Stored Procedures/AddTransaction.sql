CREATE PROCEDURE [Openchain].[AddTransaction]
    @instance INT,
    @transactionHash BINARY(32),
    @mutationHash BINARY(32),
    @rawData VARBINARY(MAX),
    @records [Openchain].[RecordMutationTable] READONLY
AS
    SET XACT_ABORT ON
    SET NOCOUNT ON

    MERGE [Openchain].[Records] AS Target
    USING
        (SELECT [Key], [Value], [Version], [Name], [Type]
        FROM @records
        WHERE [Value] IS NOT NULL) AS Source
    ON (Target.[Instance] = @instance) AND (Target.[Key] = Source.[Key]) AND (Target.[Version] = Source.[Version])
    WHEN MATCHED THEN
        UPDATE SET Target.[Value] = Source.[Value], Target.[Version] = @mutationHash
    WHEN NOT MATCHED THEN
        INSERT ([Instance], [Key], [Value], [Version], [Name], [Type])
        VALUES (@instance, Source.[Key], Source.[Value], @mutationHash, Source.[Name], Source.[Type]);

    DECLARE @insertedIds TABLE (TransactionId BIGINT)

    INSERT INTO [Openchain].[Transactions]
    ([Instance], [TransactionHash], [MutationHash], [RawData])
    OUTPUT Inserted.[Id] INTO @insertedIds
    VALUES (@instance, @transactionHash, @mutationHash, @rawData);

    DECLARE @transactionId AS BIGINT
    SELECT TOP(1) @transactionId = [TransactionId] FROM @insertedIds

    INSERT INTO [Openchain].[RecordMutations]
    ([Instance], [RecordKey], [TransactionId])
    SELECT @instance, Records.[Key], @transactionId
    FROM @records AS Records;

RETURN @transactionId
