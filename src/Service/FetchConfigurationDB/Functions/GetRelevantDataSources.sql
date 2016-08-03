CREATE FUNCTION [dbo].[GetRelevantDataSources]
(
	@timeStamp DateTime
)
RETURNS @returntable TABLE
(
	ID int,
	Name varchar(64),
	Uri varchar(MAX),
	FullClrTypeName varchar(MAX),
	Description varchar(MAX),	
	Copyright varchar(MAX),
	TimeStamp DateTime
)
AS
BEGIN
	INSERT @returntable
	SELECT ds.ID,ds.Name,dsh.Uri, dsh.FullClrTypeName,dsh.Description, dsh.Copyright,dsh.TimeStamp FROM [dbo].DataSourcesHistory dsh
			INNER JOIN
			(SELECT ds2.ID,max(ds2.TimeStamp) ts
			FROM [dbo].DataSourcesHistory ds2
			WHERE ds2.TimeStamp <= @timeStamp
			GROUP BY ds2.ID) recentDataSources ON dsh.ID=recentDataSources.ID ANd dsh.TimeStamp=recentDataSources.ts
			INNER JOIN
			[dbo].DataSources ds ON ds.ID=dsh.ID
	RETURN
END