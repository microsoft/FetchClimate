--====================================================================================================================
--AddDataSource - Adds new DataSource with given parameters to DataSources table. If DataSource with @DisplayName is not exists then adds new DataSource to DataSourceIDs table.
--		  		  This procedure uses transaction. In case something is going wrong it throws exception.
--	@DisplayName NVARCHAR (64),
--	@Description NVARCHAR (MAX),
--	@Copyright NVARCHAR (MAX),
--	@TypeName NVARCHAR (MAX),
--	@Uri NVARCHAR (MAX) = NULL,
--====================================================================================================================

CREATE PROCEDURE [dbo].[AddDataSource]
	@DisplayName NVARCHAR (64),
	@Description NVARCHAR (MAX),
	@Copyright NVARCHAR (MAX),
	@TypeName NVARCHAR (MAX) = NULL,
	@Uri NVARCHAR (MAX) = NULL,
	@RemoteID INT = NULL,
	@RemoteName NVARCHAR (64) = NULL
AS
	BEGIN TRANSACTION
		IF ((@TypeName IS NULL AND (@RemoteID IS NOT NULL AND @RemoteName IS NOT NULL)) 
		OR (@TypeName IS NOT NULL AND (@RemoteID IS NULL AND @RemoteName IS NULL)))
		BEGIN
			DECLARE @SourceId INT
			SET @SourceId = (SELECT ds.ID FROM [dbo].DataSources AS ds WHERE ds.Name = @DisplayName )
			IF(@SourceId IS NULL)
			BEGIN
				INSERT INTO [dbo].DataSources (Name) VALUES(@DisplayName)
				SET @SourceId = @@IDENTITY
			END
			-- Rollback the transaction if there were any errors
			IF @@ERROR <> 0
			BEGIN
				-- Rollback the transaction
				ROLLBACK

				-- Raise an error and return
				RAISERROR ('Error in inserting name in DataSourceIDs.', 16, 1)
				RETURN 0
			END

			DECLARE @DBDisplayName NVARCHAR (64)
			DECLARE @DBDescription NVARCHAR (MAX)
			DECLARE @DBCopyright NVARCHAR (MAX)
			DECLARE @DBTypeName NVARCHAR (MAX)
			DECLARE @DBUri NVARCHAR (MAX)		
			DECLARE @DBRemoteID INT
			DECLARE @DBRemoteName NVARCHAR (64)	

			DECLARE Cur CURSOR FOR
				SELECT ds.ID, ds.Description, ds.Copyright, ds.FullClrTypeName, ds.Uri, ds.RemoteID, ds.RemoteName
				FROM [dbo].DataSourcesHistory AS ds 
				WHERE ds.TimeStamp = (SELECT MAX(ds2.TimeStamp) FROM [dbo].DataSourcesHistory as ds2 WHERE ds2.ID = @SourceId)
		
			OPEN Cur
			FETCH NEXT FROM Cur INTO @SourceId, @DBDescription, @DBCopyright, @DBTypeName, @DBUri, @DBRemoteID, @DBRemoteName
			CLOSE Cur
			DEALLOCATE Cur

			IF(@@FETCH_STATUS < 0)
			BEGIN
				INSERT INTO [dbo].DataSourcesHistory(ID, Description, Copyright, FullClrTypeName, Uri, RemoteID, RemoteName) 
				VALUES (@SourceId, @Description, @Copyright, @TypeName, @Uri, @RemoteID, @RemoteName)
			
			END
			ELSE
			BEGIN
				-- Rollback the transaction
				ROLLBACK
				RAISERROR('Failed to add DataSource as it is already exist', 16, 1)
				RETURN 0
			END

		END
		ELSE
		BEGIN
			-- Rollback the transaction
			ROLLBACK
			RAISERROR('Failed to add DataSource: handler and remoteName parameters cannot be set at the same time.', 16, 1)
			RETURN 0
		END
	COMMIT TRANSACTION
	--RETURN @SourceId