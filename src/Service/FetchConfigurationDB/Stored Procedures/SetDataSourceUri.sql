--====================================================================================================================
--SetDataSourceUri - Updates DataSource Uri specified by @Name Adds new database record to DataSources table with current TimeStamp(UTC).
--						   If @Uri is not changed then do nothing.
--	@Name NVARCHAR(64),
--	@Uri NVARCHAR (MAX)
--====================================================================================================================

CREATE PROCEDURE [dbo].[SetDataSourceUri]
	@Name NVARCHAR (64),
	@Uri NVARCHAR (MAX)
AS
	DECLARE @DBID INT
	DECLARE @DBFullClrTypeName NVARCHAR (MAX)	
	DECLARE @DBDescription NVARCHAR (MAX)
	DECLARE @DBCopyright NVARCHAR (MAX)
	DECLARE @DBUri NVARCHAR (MAX)

	DECLARE Cur CURSOR FOR
			SELECT ds.ID, ds.Uri, ds.FullClrTypeName, ds.Description, ds.Copyright
			FROM [dbo].DataSourcesHistory AS ds 
				INNER JOIN [dbo].DataSources as dsi
				ON ds.ID = dsi.ID
			WHERE 
				dsi.Name = @Name AND 
				ds.TimeStamp =(SELECT MAX(ds2.TimeStamp) FROM [dbo].DataSourcesHistory AS ds2 WHERE ds2.ID = ds.ID)

	OPEN Cur
	FETCH NEXT FROM Cur INTO @DBID, @DBUri, @DBFullClrTypeName, @DBDescription, @DBCopyright
	CLOSE Cur 
	DEALLOCATE Cur

	-- NULL is not equal some string (FALSE)
	IF(@DBUri <> @Uri OR (@DBUri IS NULL AND @Uri IS NOT NULL) OR (@DBUri IS NOT NULL AND @Uri IS NULL))
	BEGIN
		INSERT INTO [dbo].DataSourcesHistory(ID, Uri, FullClrTypeName, Description, Copyright)
		VALUES (@DBID, @Uri, @DBFullClrTypeName, @DBDescription, @DBCopyright)
	END