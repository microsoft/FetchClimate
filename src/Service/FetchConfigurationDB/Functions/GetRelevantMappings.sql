CREATE FUNCTION [dbo].[GetRelevantMappings]
(
	@timeStamp DateTime
)
RETURNS @returntable TABLE
(
	TimeStamp             DATETIME,     
    DataSourceID          INT,          
	DataSourceName		  NVARCHAR(64),
    DataSourceVariable    NVARCHAR(64), 
    EnvironmentalVariable NVARCHAR(64), 
    IsProvided            BIT,          
    IsEnabled             BIT          
)
AS
BEGIN
	INSERT @returntable
	SELECT vm.TimeStamp,vm.DataSourceID,ds.Name, vm.DataSourceVariable, vm.EnvironmentalVariable, vm.IsProvided, vm.IsEnabled FROM [dbo].VariableMappingHistory vm
	INNER JOIN (
		SELECT vm2.DataSourceID id,vm2.EnvironmentalVariable, max(vm2.TimeStamp) ts
		FROM [dbo].VariableMappingHistory vm2
		WHERE vm2.TimeStamp <= @timeStamp
		GROUP BY vm2.DataSourceID, vm2.EnvironmentalVariable) recentMappings ON recentMappings.ts = vm.TimeStamp AND recentMappings.id=vm.DataSourceID AND recentMappings.EnvironmentalVariable=vm.EnvironmentalVariable
	INNER JOIN [dbo].DataSources ds ON ds.ID = vm.DataSourceID
	RETURN
END