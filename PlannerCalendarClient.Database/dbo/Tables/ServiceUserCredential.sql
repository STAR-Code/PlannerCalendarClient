CREATE TABLE [dbo].[ServiceUserCredential] (
    [Id]       BIGINT         IDENTITY (1, 1) NOT NULL,
    [UserId]   NVARCHAR (260) NOT NULL,
    [Password] NVARCHAR (60)  NOT NULL,
    CONSTRAINT [PK_ServiceUserCredential] PRIMARY KEY CLUSTERED ([Id] ASC)
);

