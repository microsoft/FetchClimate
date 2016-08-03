--====================================================================================================================
--SetDataSourceDescription - Updates DataSource Description specified by @Name Adds new database record to DataSources table with current TimeStamp(UTC).
--						   If @Description is not changed then do nothing.
--	@Name NVARCHAR(64),
--	@Description NVARCHAR (MAX)
--====================================================================================================================

CREATE PROCEDURE [dbo].[SetDataSourceDescription]
	@Name NVARCHAR (64),
	@Description NVARCHAR (MAX)
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
	IF(@DBDescription <> @Description OR (@DBDescription IS NULL AND @Description IS NOT NULL) OR (@DBDescription IS NOT NULL AND @Description IS NULL))
	BEGIN
		INSERT INTO [dbo].DataSourcesHistory(ID, Uri, FullClrTypeName, Description, Copyright)
		VALUES (@DBID, @DBUri, @DBFullClrTypeName, @Description, @DBCopyright)
	END