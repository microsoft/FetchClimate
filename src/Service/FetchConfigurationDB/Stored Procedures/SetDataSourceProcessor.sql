--====================================================================================================================
--SetDataSourceProcessor - Updates DataSource TypeName specified by @Name Adds new database record to DataSources table with current TimeStamp(UTC).
--						   If @TypeName is not changed then do nothing.
--	@Name NVARCHAR(64),
--	@TypeName NVARCHAR (MAX)
--====================================================================================================================

CREATE PROCEDURE [dbo].[SetDataSourceProcessor]
	@Name NVARCHAR (64),
	@TypeName NVARCHAR (MAX) = NULL,
	@RemoteID INT = NULL,
	@RemoteName NVARCHAR (64) = NULL
AS
	DECLARE @DBTypeName NVARCHAR (MAX)
	DECLARE @DBID INT
	DECLARE @DBFullClrTypeName NVARCHAR (MAX)	
	DECLARE @DBDescription NVARCHAR (MAX)
	DECLARE @DBCopyright NVARCHAR (MAX)
	DECLARE @DBUri NVARCHAR (MAX)
	DECLARE @DBRemoteID INT
	DECLARE @DBRemoteName NVARCHAR (64)

	IF ((@TypeName IS NULL AND (@RemoteID IS NOT NULL AND @RemoteName IS NOT NULL)) 
		OR (@TypeName IS NOT NULL AND (@RemoteID IS NULL AND @RemoteName IS NULL)))
	BEGIN
		DECLARE Cur CURSOR FOR
			SELECT ds.ID, ds.Uri, ds.FullClrTypeName, ds.Description, ds.Copyright, ds.RemoteID, ds.RemoteName
			FROM [dbo].DataSourcesHistory AS ds 
				INNER JOIN [dbo].DataSources as dsi
				ON ds.ID = dsi.ID
			WHERE 
				dsi.Name = @Name AND 
				ds.TimeStamp =(SELECT MAX(ds2.TimeStamp) FROM [dbo].DataSourcesHistory AS ds2 WHERE ds2.ID = dsi.ID)

		OPEN Cur
		FETCH NEXT FROM Cur INTO @DBID, @DBUri, @DBFullClrTypeName, @DBDescription, @DBCopyright, @DBRemoteID, @DBRemoteName
		--SELECT @DBID, @DBUri, @DBFullClrTypeName, @DBIsHidden, @DBDescription, @DBCopyright
		CLOSE Cur 
		DEALLOCATE Cur
		
		IF (@DBID IS NOT NULL AND ((@TypeName IS NOT NULL AND (@TypeName <> @DBFullClrTypeName OR @DBFullClrTypeName IS NULL)) OR
			(@RemoteName IS NOT NULL AND (@RemoteID <> @DBRemoteID OR @RemoteName <> @DBRemoteName OR @DBRemoteID IS NULL OR @DBRemoteName IS NULL)))) 
		BEGIN
			INSERT INTO [dbo].DataSourcesHistory(ID, Uri, FullClrTypeName, Description, Copyright, RemoteID, RemoteName)
			VALUES (@DBID, @DBUri, @TypeName, @DBDescription, @DBCopyright, @RemoteID, @RemoteName)
		END
	END
	ELSE
	BEGIN
		RAISERROR('Failed to SET DataSource: handler and remoteName parameters cannot be set at the same time.', 16, 1)
	END