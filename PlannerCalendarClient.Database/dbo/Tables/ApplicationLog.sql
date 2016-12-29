CREATE TABLE [dbo].[ApplicationLog]
(
	[Id] INT IDENTITY(1,1) NOT NULL,
	[Date] DATETIME NOT NULL,
	[Thread] VARCHAR(255) NOT NULL, 
    [Level] VARCHAR(50) NOT NULL, 
    [Logger] VARCHAR(255) NOT NULL, 
    [Message] VARCHAR(4000) NOT NULL, 
    [Exception] VARCHAR(MAX) NULL, 
    [EventId] INT NULL, 
    [ApplicationName] VARCHAR(255) NULL, 
    CONSTRAINT [PK_ApplicationLog] PRIMARY KEY NONCLUSTERED ([Id])
)
GO
CREATE CLUSTERED INDEX [IX_ApplicationLog_Clustered] ON [dbo].[ApplicationLog] ([Date])
GO
