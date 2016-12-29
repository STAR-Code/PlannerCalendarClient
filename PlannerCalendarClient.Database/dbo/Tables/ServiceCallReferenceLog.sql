CREATE TABLE [dbo].[ServiceCallReferenceLog]
(
    [Id] BIGINT NOT NULL IDENTITY PRIMARY KEY,
    [ServiceCallResponseReferenceId] UNIQUEIDENTIFIER NULL, 
    [Operation] CHAR NOT NULL, 
    [CallStarted] DATETIME2 NOT NULL DEFAULT GETDATE(), 
    [CallEnded] DATETIME2 NULL, 
    [Success] BIT NULL, 
    [ResponseText] NVARCHAR(MAX) NULL, 
)

GO
