CREATE TABLE [dbo].[CalendarEvent] (
    [Id]                     BIGINT           IDENTITY (1, 1) NOT NULL,
    [MailAddress]            NVARCHAR (260)   NOT NULL,
    [CalId]                  NVARCHAR (512)   NOT NULL,
    [PlannerCalendarEventId] UNIQUEIDENTIFIER NULL,
    [PlannerResourceId]      UNIQUEIDENTIFIER NULL,
    [IsDeleted]              BIT              DEFAULT ((0)) NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC) 
);


GO
CREATE NONCLUSTERED INDEX [IX_CalendarEvent_CalId]
    ON [dbo].[CalendarEvent]([CalId] ASC);

GO

CREATE UNIQUE INDEX [IX_CalendarEvent_MailAddress_CalId] ON [dbo].[CalendarEvent] ([MailAddress], [CalId])
