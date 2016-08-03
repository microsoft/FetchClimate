CREATE TABLE [dbo].[DataSourcesHistory] (
    [ID]              INT            NOT NULL,
    [TimeStamp]       DATETIME       DEFAULT (getutcdate()) NOT NULL,
    [Uri]             NVARCHAR (MAX) DEFAULT ('') NOT NULL,
    [FullClrTypeName] NVARCHAR (MAX) NULL,
    [Description]     NVARCHAR (MAX) NOT NULL,
    [Copyright]       NVARCHAR (MAX) NOT NULL,
    [RemoteID] INT NULL, 
    [RemoteName] NVARCHAR(64) NULL, 
    PRIMARY KEY CLUSTERED ([TimeStamp] ASC, [ID] ASC),
    CONSTRAINT [FK_DataSources_ToDataSourceIDs] FOREIGN KEY ([ID]) REFERENCES [dbo].[DataSources] ([ID])
);

GO

CREATE TRIGGER [dbo].[Trigger_FederatedOrLocalControl]
    ON [dbo].[DataSourcesHistory]
    INSTEAD OF INSERT
    AS
    BEGIN
        SET NoCount ON
		DECLARE @HandlerType NVARCHAR(MAX)
		DECLARE @RemoteId int
		DECLARE @RemoteName NVARCHAR(64)
		SELECT @HandlerType=FullClrTypeName from inserted
		SELECT @RemoteId=RemoteID from inserted
		SELECT @RemoteName=RemoteName from inserted
		IF (((@HandlerType IS NULL) AND (@RemoteId IS NOT NULL) AND (@RemoteName IS NOT NULL)) OR ((@HandlerType IS NOT NULL) AND (@RemoteId IS NULL) AND (@RemoteName IS NULL)))
		BEGIN
			INSERT INTO DataSourcesHistory SELECT ID,TimeStamp,Uri,FullClrTypeName,Description,Copyright,RemoteID,RemoteName FROM inserted
		END
    END
