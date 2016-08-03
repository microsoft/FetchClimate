CREATE TABLE [dbo].[FetchEngineHistory] (
    [TimeStamp]       DATETIME       DEFAULT (getutcdate()) NOT NULL,
    [FullClrTypeName] NVARCHAR (MAX) NOT NULL,
    PRIMARY KEY CLUSTERED ([TimeStamp] ASC)
);
