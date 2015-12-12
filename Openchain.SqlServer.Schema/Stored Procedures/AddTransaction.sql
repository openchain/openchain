CREATE PROCEDURE [Openchain].[AddTransaction]
    @instance INT,
    @transactionHash BINARY(32),
    @mutationHash BINARY(32),
    @rawData VARBINARY(MAX),
    @records [Openchain].[RecordMutationTable] READONLY
AS
    SET XACT_ABORT ON;
    SET NOCOUNT ON;

    SELECT UpdatedRecords.[Key]
    FROM @records AS UpdatedRecords
    LEFT OUTER JOIN [Openchain].[Records] ON (UpdatedRecords.[Key] = Records.[Key] AND Records.[Instance] = @instance)
    WHERE UpdatedRecords.[Version] <> ISNULL(Records.[Version], CAST(0x AS VARBINARY(32)));

    IF @@Rowcount > 0
        RETURN;

    MERGE [Openchain].[Records] AS Target
    USING @records AS Source
    ON (Target.[Instance] = @instance) AND (Target.[Key] = Source.[Key])
    WHEN MATCHED THEN
        UPDATE SET
            Target.[Value] = ISNULL(Source.[Value], Target.[Value]),
            Target.[Version] = IIF(Source.[Value] IS NULL, Source.[Version], @mutationHash)
    WHEN NOT MATCHED THEN
        INSERT ([Instance], [Key], [Value], [Version], [Name], [Type])
        VALUES (
            @instance,
            Source.[Key],
            ISNULL(Source.[Value], CAST(0x AS VARBINARY(32))),
            IIF(Source.[Value] IS NULL, Source.[Version], @mutationHash),
            Source.[Name],
            Source.[Type]);

    DECLARE @insertedIds TABLE (TransactionId BIGINT);

    INSERT INTO [Openchain].[Transactions]
    ([Instance], [TransactionHash], [MutationHash], [RawData])
    OUTPUT Inserted.[Id] INTO @insertedIds
    VALUES (@instance, @transactionHash, @mutationHash, @rawData);

    DECLARE @transactionId AS BIGINT;
    SELECT TOP(1) @transactionId = [TransactionId] FROM @insertedIds;

    INSERT INTO [Openchain].[RecordMutations]
    ([Instance], [RecordKey], [TransactionId])
    SELECT @instance, Records.[Key], @transactionId
    FROM @records AS Records;

RETURN;
