-- This table contain all the property from the exchange notification
CREATE TABLE [Notification]
(
    [Id] BIGINT NOT NULL IDENTITY PRIMARY KEY,
    [EwsId] VARCHAR(512) NOT NULL,
    [EwsTimestamp] DATETIME2 NOT NULL,
    [ReceiveTime] [DATETIME2] DEFAULT GETDATE(),
)

GO