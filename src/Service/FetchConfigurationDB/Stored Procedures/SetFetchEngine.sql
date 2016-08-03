--====================================================================================================================
--SetFetchEngine - Updates FetchEngine TypeName specified by @TypeName Adds new database record to FetchEngine table with current TimeStamp(UTC).
--						   If @TypeName is not changed then do nothing.
--	@TypeName NVARCHAR (MAX)
--====================================================================================================================

CREATE PROCEDURE [dbo].[SetFetchEngine]
	@TypeName NVARCHAR (MAX)
AS

	DECLARE @DBFullClrTypeName NVARCHAR(MAX)

	SET @DBFullClrTypeName = 
	(SELECT fe.FullClrTypeName
		FROM [dbo].FetchEngineHistory AS fe
		WHERE 
			fe.FullClrTypeName = @TypeName AND 
			fe.TimeStamp =(SELECT MAX(fe2.TimeStamp) FROM [dbo].FetchEngineHistory AS fe2 WHERE fe2.FullClrTypeName = @TypeName))

	IF(@DBFullClrTypeName <> @TypeName OR @DBFullClrTypeName IS NULL)
	BEGIN
		INSERT INTO [dbo].FetchEngineHistory (FullClrTypeName) VALUES(@TypeName)
	END