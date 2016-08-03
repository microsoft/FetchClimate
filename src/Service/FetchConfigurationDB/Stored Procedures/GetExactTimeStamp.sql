--====================================================================================================================
--GetLatestTimeStamp - Retrieves latest TimeStamp (UTC) over all tables.
--====================================================================================================================
CREATE PROCEDURE [dbo].[GetExactTimeStamp]
	@TimeStamp DATETIME
AS
	SELECT TOP 1 res.TimeStamp FROM
		(
				(SELECT ds.TimeStamp AS TimeStamp FROM [dbo].DataSourcesHistory AS ds)
			UNION
				(SELECT fe.TimeStamp AS TimeStamp FROM [dbo].FetchEngineHistory AS fe)
			UNION
				(SELECT vm.TimeStamp AS TimeStamp FROM [dbo].VariableMappingHistory AS vm)
		) res WHERE res.TimeStamp <= @TimeStamp ORDER BY res.TimeStamp DESC
