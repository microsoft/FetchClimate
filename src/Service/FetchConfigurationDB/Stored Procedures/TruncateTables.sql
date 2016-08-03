--====================================================================================================================
--TruncateTables - Truncate all tables. For debug purpose only.
--====================================================================================================================

CREATE PROCEDURE [dbo].[TruncateTables]
AS

	DELETE FROM [dbo].VariableMappingHistory
	DELETE FROM [dbo].FetchEngineHistory
	DELETE FROM [dbo].EnvironmentalVariables
	DELETE FROM [dbo].DataSourcesHistory
	DELETE FROM [dbo].DataSources