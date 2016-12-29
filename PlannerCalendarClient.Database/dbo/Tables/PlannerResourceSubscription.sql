CREATE TABLE [dbo].[PlannerResourceSubscription] (
    [PlannerResourceId] BIGINT NOT NULL,
    [SubscriptionId]    BIGINT NOT NULL,
    [CreatedDate] [DATETIME2] DEFAULT GETDATE(), -- The time when the PlannerResourceSubscription is created in the store.  
    CONSTRAINT [PK_PlannerResourceSubscription] PRIMARY KEY CLUSTERED ([PlannerResourceId] ASC),
    CONSTRAINT [FK_PlannerResourceSubscription_PlannerResource] FOREIGN KEY ([PlannerResourceId]) REFERENCES [dbo].[PlannerResource] ([Id]),
    CONSTRAINT [FK_PlannerResourceSubscription_Subscription] FOREIGN KEY ([SubscriptionId]) REFERENCES [dbo].[Subscription] ([Id])
);


GO
CREATE NONCLUSTERED INDEX [IX_Subscription]
    ON [dbo].[PlannerResourceSubscription]([SubscriptionId] ASC);

