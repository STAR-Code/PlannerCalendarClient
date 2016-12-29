CREATE TABLE [PlannerResource]
(
    [Id] BIGINT NOT NULL IDENTITY,
	[MailAddress] NVARCHAR(260) NOT NULL,                -- the resource mail address
	[PlannerResourceId] [uniqueidentifier] NULL,    -- Is this property needed?
	[GroupAffinity] NVARCHAR(260) NULL,                 -- This is an exchange grouping feature. When this feature is null the subscriber has not been assigned to a group.
    [CreatedDate] [DATETIME2] DEFAULT GETDATE(), -- The time when the PlannerResource is created in the store.  
    [UpdatedDate] [DATETIME2] DEFAULT GETDATE(), -- The time when the PlannerResource is updated in the  store.  
    [DeletedDate] [DATETIME2] NULL, -- The time when the PlannerResource is deleted in the store. 
	[ErrorCode] NVARCHAR(50) NULL , 
    [ErrorDescription] NVARCHAR(512) NULL, 
    [ErrorDate] DATETIME2 NULL, 
    [LastFullSync] DATETIME2 NULL, 
    CONSTRAINT [PK_PlannerResource] PRIMARY KEY ([Id]), 
	CONSTRAINT [AK_PlannerResource_MailAddress] UNIQUE([MailAddress]) 
)


