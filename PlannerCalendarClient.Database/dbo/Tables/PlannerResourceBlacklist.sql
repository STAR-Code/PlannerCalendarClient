CREATE TABLE [dbo].[PlannerResourceBlacklist]
(
	[Id] BIGINT NOT NULL IDENTITY, 
    [MailAddress] NVARCHAR(260) NOT NULL, 
    [Reason] NVARCHAR(1024) NULL,
	CONSTRAINT [PK_PlannerResourceBlacklist] PRIMARY KEY CLUSTERED ([Id] ASC)
)
