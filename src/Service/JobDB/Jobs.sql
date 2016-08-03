CREATE TABLE [dbo].[Job]
(
	[Hash] CHAR(40) NOT NULL, 
    [PartNo] INT NOT NULL, 
    [PartsCount] INT NOT NULL, 
    [Priority] TINYINT NOT NULL, 
    [Status] TINYINT NOT NULL,  -- 0:Pending, 1:Active, 2:Completed, 3:Failed
    [Touchtime] DATETIME NOT NULL,
	[SubmitTime] DATETIME NOT NULL, 
    [StartTime] DATETIME NULL, 
    [IsHeavyJob] TINYINT NOT NULL DEFAULT 0, 
    CONSTRAINT [PK_Job] PRIMARY KEY ([PartNo], [Hash])
)

GO


CREATE INDEX [Hash_Status] ON [dbo].[Job] ([Hash],[Status])

GO 

CREATE NONCLUSTERED INDEX job_status
ON dbo.Job (Status)

GO

CREATE NONCLUSTERED INDEX job_isHeavy
ON dbo.Job (IsHeavyJob)

GO

CREATE NONCLUSTERED INDEX job_status_isHeavy
ON dbo.Job (Status,IsHeavyJob)