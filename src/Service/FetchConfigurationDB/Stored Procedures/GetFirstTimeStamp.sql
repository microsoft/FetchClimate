--====================================================================================================================
--GetLatestTimeStamp - Retrieves latest TimeStamp (UTC) over all tables.
--====================================================================================================================
CREATE PROCEDURE [dbo].[GetFirstTimeStamp]
AS
	SELECT MIN(res.TimeStamp) as TimeStamp FROM
		(
				(SELECT Min(ds.TimeStamp) AS TimeStamp FROM [dbo].DataSourcesHistory AS ds)
			UNION
				(SELECT Min(fe.TimeStamp) AS TimeStamp FROM [dbo].FetchEngineHistory AS fe)
			UNION
				(SELECT Min(vm.TimeStamp) AS TimeStamp FROM [dbo].VariableMappingHistory AS vm)
		) res