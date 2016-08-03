--====================================================================================================================
--GetEnvVariables - Retrieves all available variables --for specified @TimeStamp(UTC) and @EnvVarName from EnvironmentalVariables table.
--	@TimeStamp DATETIME
--  @EnvVarName NVARCHAR(64)
--====================================================================================================================

CREATE PROCEDURE [dbo].[GetEnvVariables]
--	@TimeStamp datetime
AS

SELECT DISTINCT vars.DisplayName, vars.Description, vars.Units FROM [dbo].EnvironmentalVariables vars
-- [dbo].[GetRelevantMappings](@TimeStamp) vm
--INNER JOIN [dbo].EnvironmentalVariables vars ON vars.DisplayName=vm.EnvironmentalVariable
--WHERE vm.DataSourceID IN 
--			(SELECT ds.ID FROM [dbo].[GetRelevantDataSources](@TimeStamp) ds)