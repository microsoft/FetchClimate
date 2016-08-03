--====================================================================================================================
--GetVariablesForDataSource - Retrieves all enabled variables for specified @TimeStamp(UTC) and @DataSourceName from EnvironmentalVariables, VariableMappings, DataSources, DataSourceIDs tables. 
--							  Where DataSource Timestamp <= @TimeStamp (UTC).
--	@TimeStamp DATETIME
--  @DataSourceName
--====================================================================================================================

CREATE PROCEDURE [dbo].[GetVariablesForDataSource]
	@TimeStamp DATETIME,
	@DataSourceName NVARCHAR(64)
AS
SELECT vars.DisplayName, vars.Description, vars.Units FROM [dbo].[GetRelevantMappings](@TimeStamp) vm
INNER JOIN [dbo].EnvironmentalVariables vars ON vars.DisplayName=vm.EnvironmentalVariable
WHERE vm.DataSourceName=@DataSourceName AND vm.IsEnabled=1