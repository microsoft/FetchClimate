--====================================================================================================================
--SetMapping - Updates mapping specified by @DataSourceID and @EnvironmaentalVariable. Adds new database record to VariableMappings table with current TimeStamp(UTC).
--						   If @NewDataSourceName, @NewIsProvided, @NewIsEnabled are not changed then do nothing.
--	@TypeName NVARCHAR (MAX)
--	@DataSourceID INT
--  @EnvironmaentalVariable NVARCHAR (MAX)
--	@NewDataSourceName NVARCHAR (64)
--	@NewIsProvided BIT
--	@NewIsEnabled BIT=1
--====================================================================================================================

CREATE PROCEDURE [dbo].[SetMapping]
	@DataSourceName NVARCHAR (64), 
	@EnvironmaentalVariable NVARCHAR (MAX), 
	@NewDataSourceName NVARCHAR (64), 
	@NewIsProvided BIT,
	@NewIsEnabled BIT=1
AS

DECLARE @DataSourceID INT
DECLARE @DBDataSourceID INT
DECLARE @DBEnvironmaentalVariable NVARCHAR(MAX)
DECLARE @DBDataSourceName NVARCHAR (64)
DECLARE @DBIsProvided BIT
DECLARE @DBIsEnabled BIT
DECLARE @TryCount INT

SET @DataSourceID = (SELECT ds.ID FROM [dbo].DataSources ds WHERE ds.Name = @DataSourceName)

DECLARE Cur CURSOR FOR
	SELECT TOP 1 vm.DataSourceID, vm.DataSourceVariable, vm.EnvironmentalVariable, vm.IsProvided, vm.IsEnabled  
	FROM [dbo].VariableMappingHistory AS vm
	WHERE vm.DataSourceID = @DataSourceID AND vm.EnvironmentalVariable = @EnvironmaentalVariable 
		AND vm.TimeStamp = (
					SELECT MAX(TimeStamp) 
					FROM [dbo].VariableMappingHistory AS vm2 
					WHERE vm2.DataSourceID = @DataSourceID AND vm2.EnvironmentalVariable = @EnvironmaentalVariable 
			)

OPEN Cur
FETCH NEXT FROM Cur INTO @DBDataSourceID, @DBDataSourceName, @DBEnvironmaentalVariable,  @DBIsProvided, @DBIsEnabled

CLOSE Cur 
DEALLOCATE Cur

IF((@@FETCH_STATUS < 0) OR @NewDataSourceName <> @DBDataSourceName OR @NewIsProvided <> @DBIsProvided OR @NewIsEnabled <> @DBIsEnabled)
BEGIN
	SET @TryCount = 0
	WHILE @TryCount<5
	BEGIN
		BEGIN TRY		
		INSERT INTO [dbo].VariableMappingHistory (DataSourceID,EnvironmentalVariable, DataSourceVariable, IsProvided,IsEnabled) 
		VALUES(@DataSourceID, @EnvironmaentalVariable, @NewDataSourceName, @NewIsProvided, @NewIsEnabled)
		BREAK
		END TRY
		BEGIN CATCH
			WAITFOR DELAY '00:00:01'
			SET @TryCount = @TryCount+1
		END CATCH
	END
END