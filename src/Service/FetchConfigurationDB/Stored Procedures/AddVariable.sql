--====================================================================================================================
--AddVariable - Adds new variable to EnvironmentalVariables table.
--	@DisplayName NVARCHAR (64)
--	@Description NVARCHAR (64)
--	@Units NVARCHAR (64)
--====================================================================================================================

CREATE PROCEDURE [dbo].[AddVariable]
	@DisplayName NVARCHAR (64),
	@Description NVARCHAR (64),
	@Units NVARCHAR (64)
AS
	INSERT INTO [dbo].EnvironmentalVariables VALUES(@DisplayName, @Units, @Description)