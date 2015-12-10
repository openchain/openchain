CREATE PROCEDURE [Openchain].[AddTransaction]
    @instance int,
    @transactionHash binary(32),
    @mutationHash binary(32),
    @rawData varbinary(MAX),
    @records [Openchain].[RecordMutation] READONLY
AS
    SET XACT_ABORT ON
    SET NOCOUNT ON

    DECLARE @transactionId AS bigint

    INSERT INTO [Openchain].[Transactions]
    ([Instance], [TransactionHash], [MutationHash], [RawData])
    OUTPUT Inserted.[Id] INTO @transactionId
    VALUES (@instance, @transactionHash, @mutationHash, @rawData);

    MERGE [Openchain].[Records] AS Target
    USING @records AS Source
    ON (Target.[Instance] = @instance) AND (Target.[Key] = Source.[Key]) AND (Target.[Version] = Source.[Version])
    WHEN MATCHED THEN
        UPDATE SET Target.[Value] = Source.[Value], Target.[Version] = @mutationHash
    WHEN NOT MATCHED THEN
        INSERT ([Instance], [Key], [Value], [Version], [Name], [Type])
        VALUES (@instance, Source.[Key], Source.[Value], @mutationHash, Source.[Name], Source.[Type]);

RETURN @transactionId
