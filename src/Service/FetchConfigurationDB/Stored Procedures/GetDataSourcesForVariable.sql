--====================================================================================================================
--GetDataSourcesForVariable - Retrieves all available (mapping exists, variable is provided) DataSources for specified datetime and variable from DataSources table, where DataSource datetime <= @TimeStamp (UTC).
--	@TimeStamp DATETIME
--  @EnvVarName NVARCHAR(64)
--====================================================================================================================

CREATE PROCEDURE [dbo].[GetDataSourcesForVariable]
	@TimeStamp DATETIME,
	@EnvVarName NVARCHAR(64)
AS

	SELECT ds.ID, dsi.Name, ds.Copyright, ds.Description, ds.FullClrTypeName,ds.RemoteID,ds.RemoteName,ds.Uri
	FROM [dbo].DataSourcesHistory AS ds
	INNER JOIN
		(
			SELECT ds2.ID, MAX(ds2.TimeStamp) AS TimeStamp
			FROM [dbo].DataSourcesHistory AS ds2 
			WHERE ds2.TimeStamp <= @TimeStamp
			GROUP BY ds2.ID
		) 
	recent ON recent.ID=ds.ID AND recent.TimeStamp=ds.TimeStamp
	INNER JOIN 
		[dbo].DataSources AS dsi ON dsi.ID=ds.ID
	INNER JOIN 
		(
			SELECT *
			FROM [dbo].[GetRelevantMappings](@TimeStamp) AS vmj
			WHERE vmj.IsEnabled = 1 AND vmj.IsProvided = 1 AND vmj.EnvironmentalVariable = @EnvVarName
		)
	vm ON vm.DataSourceID = dsi.ID
--		[dbo].VariableMappingHistory AS vm ON vm.DataSourceID = dsi.ID AND vm.EnvironmentalVariable = @EnvVarName AND vm.IsEnabled = 1 AND vm.IsProvided = 1