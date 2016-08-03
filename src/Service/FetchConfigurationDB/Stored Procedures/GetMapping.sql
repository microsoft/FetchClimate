--====================================================================================================================
--GetMapping - Retrieves all available mappings for specified @TimeStamp(UTC) adn @DataSourceName from VariableMappings table. Where Timestamp <= @TimeStamp (UTC).
--	@TimeStamp DATETIME
--  @DataSourceName VARCHAR(45)
--====================================================================================================================

CREATE PROCEDURE [dbo].[GetMapping]
	@TimeStamp DATETIME, 
	@DataSourceName VARCHAR(45)
AS
	SELECT vm.EnvironmentalVariable AS FetchVariableName, vm.DataSourceVariable AS DataVariableName, vm.IsProvided AS IsOutbound
	FROM [dbo].[GetRelevantMappings](@TimeStamp) vm
	WHERE vm.IsEnabled != 0 AND vm.DataSourceName = @DataSourceName