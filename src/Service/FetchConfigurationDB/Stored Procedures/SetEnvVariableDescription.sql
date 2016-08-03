--====================================================================================================================
--SetEnvVariableUnits - Updates Environmetal Variable Description field specified by @Name.
--	@Name NVARCHAR(64),
--	@Units NVARCHAR (MAX)
--====================================================================================================================

CREATE PROCEDURE [dbo].[SetEnvVariableDescription]
	@Name NVARCHAR (64),
	@Description NVARCHAR (MAX)
AS
	UPDATE [dbo].EnvironmentalVariables SET Description = @Description WHERE DisplayName = @Name