--====================================================================================================================
--SetEnvVariableUnits - Updates Environmetal Variable Units field specified by @Name.
--	@Name NVARCHAR(64),
--	@Units NVARCHAR (64)
--====================================================================================================================

CREATE PROCEDURE [dbo].[SetEnvVariableUnits]
	@Name NVARCHAR (64),
	@Units NVARCHAR (64)
AS
	UPDATE [dbo].EnvironmentalVariables SET Units = @Units WHERE DisplayName = @Name