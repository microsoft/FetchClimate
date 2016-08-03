CREATE PROCEDURE [dbo].[GetTimeStamps]
AS
	RETURN 
	SELECT res.[TimeStamp] FROM
	((SELECT [TimeStamp] FROM DataSourcesHistory)
	UNION
	(SELECT [TimeStamp] FROM FetchEngineHistory)
	UNION
	(SELECT [TimeStamp] FROM VariableMappingHistory)
	) res ORDER BY [TimeStamp] DESC