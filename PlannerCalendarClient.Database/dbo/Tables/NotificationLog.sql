CREATE TABLE [dbo].[NotificationLog]
(
    [Id] BIGINT NOT NULL PRIMARY KEY,
    [EwsId] VARCHAR(512) NOT NULL, 
    [EwsTimestamp] DATETIME2 NOT NULL, 
    [ReceiveTime] [DATETIME2] DEFAULT GETDATE(),  
	[ProcessedTime] [DATETIME2] DEFAULT GETDATE(),
    [ResponseText] NVARCHAR(MAX) NULL 
)

GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'Error message from appointment provider (Exchange)',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'NotificationLog',
    @level2type = N'COLUMN',
    @level2name = 'ResponseText'
GO

GO

GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'When the notification was handled by the EventProcessor',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'NotificationLog',
    @level2type = N'COLUMN',
    @level2name = N'ProcessedTime'