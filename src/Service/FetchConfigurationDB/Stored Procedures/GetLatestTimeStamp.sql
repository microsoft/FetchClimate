--====================================================================================================================
--GetLatestTimeStamp - Retrieves latest TimeStamp (UTC) over all tables.
--====================================================================================================================
CREATE PROCEDURE [dbo].[GetLatestTimeStamp]
AS
	SELECT MAX(res.TimeStamp) as TimeStamp FROM
		(
				(SELECT Max(ds.TimeStamp) AS TimeStamp FROM [dbo].DataSourcesHistory AS ds)
			UNION
				(SELECT Max(fe.TimeStamp) AS TimeStamp FROM [dbo].FetchEngineHistory AS fe)
			UNION
				(SELECT Max(vm.TimeStamp) AS TimeStamp FROM [dbo].VariableMappingHistory AS vm)
		) res