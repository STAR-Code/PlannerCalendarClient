CREATE TABLE [dbo].[PlannerResourceWhitelist]
(
	[Id] BIGINT NOT NULL IDENTITY, 
    [MailAddress] NVARCHAR(260) NOT NULL, -- the resource mail address
    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),-- The time when the PlannerResource is created in the store.
	CONSTRAINT [PK_PlannerResourceWhitelist] PRIMARY KEY ([Id]),
	CONSTRAINT [AK_PlannerResourceWhitelist_MailAddress] UNIQUE([MailAddress])
)
