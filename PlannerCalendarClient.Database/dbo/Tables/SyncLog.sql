CREATE TABLE [dbo].[SyncLog] (
    [Id]                        BIGINT         IDENTITY (1, 1) NOT NULL,
    [CreatedDate]               DATETIME2 (7)  NOT NULL,
    [CalendarEventId]           BIGINT         NOT NULL,
    [CalendarStart]             DATETIME2 (7)  NOT NULL,
    [CalendarEnd]               DATETIME2 (7)  NOT NULL,
    [Operation]                 CHAR (1)       NOT NULL,
    [SyncDate]                  DATETIME2 (7)  NULL,
    [ServiceCallReferenceLogId] BIGINT         NULL,
    [PlannerSyncSuccess]        BIT            NULL,
    [PlannerSyncResponse]       NVARCHAR (MAX) NULL,
    [PlannerEventErrorCode]     INT            NULL,
    [NotificationLogId] BIGINT NULL, 
    PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_SyncLog_CalendarEvent] FOREIGN KEY ([CalendarEventId]) REFERENCES [dbo].[CalendarEvent] ([Id]),
    CONSTRAINT [FK_SyncLog_ServiceCallReferenceLog] FOREIGN KEY ([ServiceCallReferenceLogId]) REFERENCES [dbo].[ServiceCallReferenceLog] ([Id])
);



GO

CREATE INDEX [IX_SyncLog_SyncDate] ON [dbo].[SyncLog] ([SyncDate])

GO
CREATE NONCLUSTERED INDEX [IX_SyncLog_CreatedDate]
    ON [dbo].[SyncLog]([CreatedDate] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_SyncLog_CalendarEventId]
    ON [dbo].[SyncLog]([CalendarEventId] ASC);

