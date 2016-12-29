CREATE TABLE [dbo].[Subscription] (
    [Id]                      BIGINT          IDENTITY (1, 1) NOT NULL,
    [GroupAffinity]           NVARCHAR (260)   NULL,
    [ServiceUserCredentialId] BIGINT          NULL,
    [Description]             NVARCHAR (1000) NOT NULL,
    [CreatedDate] [DATETIME2] DEFAULT GETDATE(), -- The time when the Subscription is created in the store.  
    [UpdatedDate] [DATETIME2] DEFAULT GETDATE(), -- The time when the Subscription is updated in the  store.  
    CONSTRAINT [PK_Subscription] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Subscription_ServiceUserCredential] FOREIGN KEY ([ServiceUserCredentialId]) REFERENCES [dbo].[ServiceUserCredential] ([Id])
);

