--====================================================================================================================
--GetDataSources - Retrieves all registered DataSources (with and without mappings) for specified datetime from DataSources table, where DataSource datetime <= @TimeStamp (UTC).
--	@TimeStamp datetime
--====================================================================================================================

CREATE PROCEDURE [dbo].[GetDataSources]
	@TimeStamp datetime
AS

SELECT ds.ID, dsi.Name, ds.Copyright, ds.Description, ds.FullClrTypeName, ds.Uri, ds.RemoteName, ds.RemoteID
FROM [dbo].DataSourcesHistory AS ds
INNER JOIN
	(
		SELECT ds2.ID, MAX(TimeStamp) AS TimeStamp
		FROM DataSourcesHistory AS ds2 
		WHERE ds2.TimeStamp <= CONVERT(DATETIME, @TimeStamp)
		GROUP BY ID
	) 
recent ON recent.ID=ds.ID AND recent.TimeStamp=ds.TimeStamp
INNER JOIN 
	[dbo].DataSources AS dsi ON dsi.ID=ds.ID