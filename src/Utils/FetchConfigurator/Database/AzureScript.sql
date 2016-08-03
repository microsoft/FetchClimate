CREATE TABLE [DataSets](
	[dataSetID] [int] IDENTITY(1,1) NOT NULL,
	[deleted] [bit] NOT NULL,
	[deflation] [int] NOT NULL,
 CONSTRAINT [PK_DataSets] PRIMARY KEY CLUSTERED 
(
	[dataSetID] ASC
)
)
GO
CREATE TABLE [Types](
	[typeID] [int] IDENTITY(1,1) NOT NULL,
	[name] [nvarchar](20) NOT NULL,
 CONSTRAINT [PK_Types] PRIMARY KEY CLUSTERED 
(
	[typeID] ASC
)
)
GO
SET IDENTITY_INSERT [dbo].[Types] ON
INSERT [dbo].[Types] ([typeID], [name]) VALUES (1, N'Int16')
INSERT [dbo].[Types] ([typeID], [name]) VALUES (2, N'Int32')
INSERT [dbo].[Types] ([typeID], [name]) VALUES (3, N'Int64')
INSERT [dbo].[Types] ([typeID], [name]) VALUES (4, N'UInt16')
INSERT [dbo].[Types] ([typeID], [name]) VALUES (5, N'UInt32')
INSERT [dbo].[Types] ([typeID], [name]) VALUES (6, N'UInt64')
INSERT [dbo].[Types] ([typeID], [name]) VALUES (7, N'Byte')
INSERT [dbo].[Types] ([typeID], [name]) VALUES (8, N'SByte')
INSERT [dbo].[Types] ([typeID], [name]) VALUES (9, N'Single')
INSERT [dbo].[Types] ([typeID], [name]) VALUES (10, N'Double')
INSERT [dbo].[Types] ([typeID], [name]) VALUES (11, N'String')
INSERT [dbo].[Types] ([typeID], [name]) VALUES (12, N'DateTime')
INSERT [dbo].[Types] ([typeID], [name]) VALUES (13, N'Boolean')
INSERT [dbo].[Types] ([typeID], [name]) VALUES (14, N'EmptyValueType')
GO
CREATE PROCEDURE [dbo].[sp_DeleteDataSet]
@dataSetID int,
@useTransaction bit
AS
BEGIN
	BEGIN TRY
		IF @useTransaction=1
		BEGIN
			BEGIN TRANSACTION
		END
		DECLARE @isDeleted bit
		SET @isDeleted = (SELECT deleted FROM DataSets
						  WHERE dataSetID = @dataSetID)
		if(@isDeleted=1)
			RAISERROR('Dataset with specified dataSetID id already marked as deleted.',16,1)
		UPDATE DataSets
		SET deleted = 1
		WHERE dataSetID = @dataSetID
		IF @useTransaction=1
		BEGIN
			COMMIT TRANSACTION
		END
	END TRY
	BEGIN CATCH
		IF @useTransaction=1
		BEGIN
			ROLLBACK TRANSACTION
		END
		DECLARE @ErrorMessage NVARCHAR(4000);
		DECLARE @ErrorSeverity INT;
		DECLARE @ErrorState INT;

		SELECT 
			@ErrorMessage = ERROR_MESSAGE(),
			@ErrorSeverity = ERROR_SEVERITY(),
			@ErrorState = ERROR_STATE();

		RAISERROR (@ErrorMessage, -- Message text.
				   @ErrorSeverity, -- Severity.
				   @ErrorState -- State.
				   );

	END CATCH
END
GO
CREATE TABLE [Variables](
	[variableID] [int] NOT NULL,
	[dataSetID] [int] NOT NULL,
	[typeID] [int] NOT NULL,
	[value] [varbinary](max) NULL,
 CONSTRAINT [PK_Variables] PRIMARY KEY CLUSTERED 
(
	[variableID] ASC,
	[dataSetID] ASC
)
)
GO
CREATE FUNCTION [dbo].[f_GetDataSetsDeflations]()
RETURNS TABLE
AS
	RETURN
		SELECT d.dataSetID, d.deflation
		FROM DataSets as d
GO
CREATE TABLE [Dimensions](
	[dimensionID] [int] IDENTITY(1,1) NOT NULL,
	[dataSetID] [int] NOT NULL,
	[name] [nvarchar](255) NOT NULL,
	[length] [int] NOT NULL,
 CONSTRAINT [PK_Dimensions] PRIMARY KEY CLUSTERED 
(
	[dimensionID] ASC
)
)
GO
CREATE TABLE [Attributes](
	[attributeID] [bigint] IDENTITY(1,1) NOT NULL,
	[variableID] [int] NOT NULL,
	[dataSetID] [int] NOT NULL,
	[typeID] [int] NOT NULL,
	[name] [nvarchar](4000) NOT NULL,
	[rank] [bit] NOT NULL,
	[value] [varbinary](max) NOT NULL,
 CONSTRAINT [PK_Attributes] PRIMARY KEY CLUSTERED 
(
	[attributeID] ASC
)
)
GO
CREATE FUNCTION [dbo].[f_GetScalarValue](@dataSetID int, @variableID int)
RETURNS varbinary(MAX)
AS
BEGIN
DECLARE @result varbinary(max)
SET @result = 
				(SELECT value
			  FROM Variables
			  WHERE dataSetID = @dataSetID
			  AND variableID = @variableID)
			  return @result
END
GO
CREATE PROCEDURE [dbo].[sp_CreateVariable_Internal]
@dataSetID int,
@typeName nvarchar(20),
@variableID int OUT,
@useTransaction bit
AS
BEGIN
BEGIN TRY
		IF @useTransaction=1
		BEGIN
			BEGIN TRANSACTION
		END
		IF NOT EXISTS(SELECT dataSetID FROM DataSets
				      WHERE dataSetID = @dataSetID)
			RAISERROR('There is no dataSet with specified dataSetID in storage',16,1)
		DECLARE @lastVariableID int
		SET @lastVariableID = (SELECT TOP(1) variableID FROM Variables
							  WHERE dataSetID = @dataSetID
							  GROUP BY variableID
							  ORDER BY variableID DESC)
		SET @variableID = @lastVariableID + 1
		DECLARE @typeID int
		SET @typeID = (SELECT typeID from [Types]
					   WHERE name = @typeName)
		IF(@typeID IS NULL)
			RAISERROR('There is no type with such name',16,1)
		INSERT INTO Variables(variableID,dataSetID,typeID)
		VALUES(@variableID, @dataSetID ,@typeID)
		IF @useTransaction=1
		BEGIN
			COMMIT TRANSACTION
		END
	END TRY
	BEGIN CATCH
		IF @useTransaction=1
		BEGIN
			ROLLBACK TRANSACTION
		END
		DECLARE @ErrorMessage NVARCHAR(4000);
		DECLARE @ErrorSeverity INT;
		DECLARE @ErrorState INT;

		SELECT 
			@ErrorMessage = ERROR_MESSAGE(),
			@ErrorSeverity = ERROR_SEVERITY(),
			@ErrorState = ERROR_STATE();

		RAISERROR (@ErrorMessage, -- Message text.
				   @ErrorSeverity, -- Severity.
				   @ErrorState -- State.
				   );

	END CATCH
END
GO
CREATE PROCEDURE [dbo].[sp_CreateDataSet]
@dataSetID int OUT,
@globalVariableID int,
@deflation int,
@useTransaction bit
AS
BEGIN
BEGIN TRY
		IF @useTransaction=1
		BEGIN
			BEGIN TRANSACTION
		END
		INSERT INTO DataSets(deleted, deflation) VALUES(0, @deflation)
		SET @dataSetID = @@IDENTITY

		DECLARE @emptyValueTypeID int
		SET @emptyValueTypeID = (SELECT typeID
								 FROM [Types]
								 WHERE name = 'EmptyValueType')
								 
		IF(@emptyValueTypeID IS NULL)
			RAISERROR('There is no EmptyValueType in Types table.',16,1);

		INSERT INTO Variables(variableID,dataSetID,typeID)
		VALUES(@globalVariableID, @dataSetID ,@emptyValueTypeID)
		IF @useTransaction=1
		BEGIN
			COMMIT TRANSACTION
		END
	END TRY
	BEGIN CATCH
		IF @useTransaction=1
		BEGIN
			ROLLBACK TRANSACTION
		END
		DECLARE @ErrorMessage NVARCHAR(4000);
		DECLARE @ErrorSeverity INT;
		DECLARE @ErrorState INT;

		SELECT 
			@ErrorMessage = ERROR_MESSAGE(),
			@ErrorSeverity = ERROR_SEVERITY(),
			@ErrorState = ERROR_STATE();

		RAISERROR (@ErrorMessage, -- Message text.
				   @ErrorSeverity, -- Severity.
				   @ErrorState -- State.
				   );

	END CATCH
END
GO
CREATE PROCEDURE [dbo].[sp_UpdateDimension]
@dataSetID int,
@dimensionName nvarchar(255),
@dimensionLength int,
@useTransaction bit
AS
BEGIN
	BEGIN TRY
		IF @useTransaction=1
		BEGIN
			BEGIN TRANSACTION
		END
		DECLARE @dimensionID int
		SET @dimensionID = (SELECT dimensionID
							FROM Dimensions
							WHERE name = @dimensionName
							AND dataSetID = @dataSetID
							GROUP BY dimensionID)
		IF @dimensionID IS NULL
		BEGIN
			RAISERROR('There is no dimension with such name in specified dataSet.',16,1)
		END

		UPDATE Dimensions
		SET length = @dimensionLength
		WHERE dimensionID = @dimensionID

		IF @useTransaction=1
		BEGIN
			COMMIT TRANSACTION
		END
	END TRY
	BEGIN CATCH
		IF @useTransaction=1
		BEGIN
			ROLLBACK TRANSACTION
		END
		DECLARE @ErrorMessage NVARCHAR(4000);
		DECLARE @ErrorSeverity INT;
		DECLARE @ErrorState INT;

		SELECT 
			@ErrorMessage = ERROR_MESSAGE(),
			@ErrorSeverity = ERROR_SEVERITY(),
			@ErrorState = ERROR_STATE();

		RAISERROR (@ErrorMessage, -- Message text.
				   @ErrorSeverity, -- Severity.
				   @ErrorState -- State.
				   );

	END CATCH
END
GO
CREATE TABLE [Variables_Dimensions](
	[variableID] [int] NOT NULL,
	[dataSetID] [int] NOT NULL,
	[dimensionID] [int] NOT NULL,
	[dimensionIndex] [int] NOT NULL,
	[chunkShape] [int] NOT NULL,
 CONSTRAINT [PK_Variables_Dimensions] PRIMARY KEY CLUSTERED 
(
	[variableID] ASC,
	[dataSetID] ASC,
	[dimensionID] ASC
)
)
GO
CREATE PROCEDURE [dbo].[sp_PutScalarValue]
@variableID int,
@dataSetID int,
@value varbinary(MAX)
AS
BEGIN
	UPDATE Variables
	SET value = @value
	WHERE dataSetID=@dataSetID
	AND variableID=@variableID
END
GO
CREATE PROCEDURE [dbo].[sp_HasScalarValue]
@dataSetID int,
@variableID int,
@result bit OUT
AS
BEGIN
SET @result = 
				(SELECT COUNT(value)
			  FROM Variables
			  WHERE dataSetID = @dataSetID
			  AND variableID = @variableID
			  AND value IS NOT NULL)
END
GO
CREATE PROCEDURE [dbo].[sp_DropDataSet]
@dataSetID int,
@useTransaction bit
AS
BEGIN
	BEGIN TRY
		IF @useTransaction=1
		BEGIN
			BEGIN TRANSACTION
		END
		DELETE FROM Variables_Dimensions
		WHERE dataSetID=@dataSetID
		DELETE FROM Attributes
		WHERE dataSetID=@dataSetID
		DELETE FROM Variables
		WHERE Variables.dataSetID = @dataSetID
		DELETE FROM Dimensions
		WHERE Dimensions.dataSetID = @dataSetID
		DELETE FROM DataSets
		WHERE dataSetID=@dataSetID
		IF @useTransaction=1
		BEGIN
			COMMIT TRANSACTION
		END
	END TRY
	BEGIN CATCH
		IF @useTransaction=1
		BEGIN
			ROLLBACK TRANSACTION
		END
		DECLARE @ErrorMessage NVARCHAR(4000);
		DECLARE @ErrorSeverity INT;
		DECLARE @ErrorState INT;

		SELECT 
			@ErrorMessage = ERROR_MESSAGE(),
			@ErrorSeverity = ERROR_SEVERITY(),
			@ErrorState = ERROR_STATE();

		RAISERROR (@ErrorMessage, -- Message text.
				   @ErrorSeverity, -- Severity.
				   @ErrorState -- State.
				   );

	END CATCH
END
GO
CREATE PROCEDURE [dbo].[sp_CreateVariableDimension_Internal]
@dataSetID int,
@variableID int,
@dimensionName nvarchar(255),
@dimensionLength int,
@dimensionIndex int,
@chunkShape int,
@useTransaction bit
AS
BEGIN
BEGIN TRY
		IF @useTransaction=1
		BEGIN
			BEGIN TRANSACTION
		END
		DECLARE @dimensionID int
		SET @dimensionID = (SELECT dimensionID
							FROM Dimensions
							WHERE name = @dimensionName
							AND dataSetID = @dataSetID
							GROUP BY dimensionID
							)
		IF @dimensionID IS NULL
		BEGIN
			INSERT INTO Dimensions(dataSetID, name,[length])
			VALUES(@dataSetID, @dimensionName, @dimensionLength)
			SET @dimensionID = @@IDENTITY
		END

		INSERT INTO Variables_Dimensions(variableID,dataSetID,dimensionID,dimensionIndex, chunkShape)
		VALUES(@variableID,@dataSetID,@dimensionID,@dimensionIndex,@chunkShape)
		IF @useTransaction=1
		BEGIN
			COMMIT TRANSACTION
		END
	END TRY
	BEGIN CATCH
		IF @useTransaction=1
		BEGIN
			ROLLBACK TRANSACTION
		END
		DECLARE @ErrorMessage NVARCHAR(4000);
		DECLARE @ErrorSeverity INT;
		DECLARE @ErrorState INT;

		SELECT 
			@ErrorMessage = ERROR_MESSAGE(),
			@ErrorSeverity = ERROR_SEVERITY(),
			@ErrorState = ERROR_STATE();

		RAISERROR (@ErrorMessage, -- Message text.
				   @ErrorSeverity, -- Severity.
				   @ErrorState -- State.
				   );

	END CATCH
END
GO
CREATE PROCEDURE [dbo].[sp_UpdateAttribute]
@dataSetID int,
@variableID int,
@name nvarchar(4000),
@rank bit,
@typeName nvarchar(20),
@value varbinary(max),
@useTransaction bit
AS
BEGIN
	BEGIN TRY
		IF @useTransaction=1
		BEGIN
			BEGIN TRANSACTION
		END
			DECLARE @typeID int
			SET @typeID = (SELECT typeID FROM [Types]
						   WHERE name = @typeName)
			IF(@typeID IS NULL)
			RAISERROR('There is no type with such name.',16,1)
			DECLARE @attributeID int
			SET @attributeID = (SELECT attributeID
							    FROM Attributes
							    WHERE dataSetID = @dataSetID
							    AND variableID = @variableID
							    AND name = @name
							    GROUP BY attributeID)
			IF (@attributeID IS NULL)
			BEGIN
				INSERT INTO Attributes
				VALUES(@variableID, @dataSetID, @typeID, @name, @rank, @value)
			END
			ELSE
			BEGIN
				UPDATE Attributes
				SET typeID = @typeID, 
				    [rank] = @rank, 
				    value = @value
				WHERE dataSetID = @dataSetID
				AND variableID = @variableID
				AND attributeID = @attributeID
			END
	IF @useTransaction=1
	BEGIN
		COMMIT TRANSACTION
	END
	END TRY
	BEGIN CATCH
	IF @useTransaction=1
	BEGIN
		ROLLBACK TRANSACTION
	END
	DECLARE @ErrorMessage NVARCHAR(4000);
    DECLARE @ErrorSeverity INT;
    DECLARE @ErrorState INT;

    SELECT 
        @ErrorMessage = ERROR_MESSAGE(),
        @ErrorSeverity = ERROR_SEVERITY(),
        @ErrorState = ERROR_STATE();

    RAISERROR (@ErrorMessage, -- Message text.
               @ErrorSeverity, -- Severity.
               @ErrorState -- State.
               );

END CATCH
END
GO
CREATE FUNCTION [dbo].[f_GetVariablesSchemas](@dataSetID int)
RETURNS TABLE
AS
	RETURN 
	SELECT v.variableID, v.dataSetID, d.name as dimensionName, d.[length] as dimensionLength, v_d.chunkShape,v_d.dimensionIndex, t.name as typeName
	FROM Variables as v
	INNER JOIN [Types] as t
	ON v.typeID = t.typeID
	LEFT OUTER JOIN Variables_Dimensions as v_d
	ON v.variableID = v_d.variableID
	AND v.dataSetID = v_d.dataSetID
	LEFT OUTER JOIN Dimensions as d
	ON v_d.dimensionID = d.dimensionID
	WHERE v.dataSetID = @dataSetID
GO
CREATE FUNCTION [dbo].[f_GetDataSetsVariableAttributes](@dataSetID int, @variableID int)
RETURNS TABLE
AS
	RETURN
		SELECT DISTINCT
		a.name,
		a.[rank],
		a.value,
		t.name as typeName
		FROM Variables as v
		LEFT OUTER JOIN Attributes as a
		ON a.variableID = v.variableID
		AND a.dataSetID = v.dataSetID
		LEFT OUTER JOIN [Types] as t
		ON a.typeID = t.typeID
		WHERE v.dataSetID = @dataSetID
		AND v.variableID = @variableID
GO
CREATE FUNCTION [dbo].[f_GetDataSetsSchemas](@attributesVariableID int)
RETURNS TABLE
AS
	RETURN
		SELECT d.dataSetID, d.deleted, d.deflation,
		a.name, a.[rank], a.[value],
		t.name as typeName
		FROM Variables as v
		INNER JOIN DataSets as d
		ON v.dataSetID = d.dataSetID
		LEFT OUTER JOIN Attributes as a
		ON v.variableID = a.variableID
		AND v.dataSetID = a.dataSetID
		LEFT OUTER JOIN [Types] as t
		ON a.typeID = t.typeID
		WHERE v.variableID = @attributesVariableID
GO
/****** Object:  Default [DF_DataSets_deflation]    Script Date: 12/04/2010 22:29:13 ******/
ALTER TABLE [DataSets] ADD  CONSTRAINT [DF_DataSets_deflation]  DEFAULT ((0)) FOR [deflation]
GO
/****** Object:  ForeignKey [FK_Attributes_Types]    Script Date: 12/04/2010 22:29:13 ******/
ALTER TABLE [Attributes]  WITH CHECK ADD  CONSTRAINT [FK_Attributes_Types] FOREIGN KEY([typeID])
REFERENCES [Types] ([typeID])
GO
ALTER TABLE [Attributes] CHECK CONSTRAINT [FK_Attributes_Types]
GO
/****** Object:  ForeignKey [FK_Attributes_Variables]    Script Date: 12/04/2010 22:29:13 ******/
ALTER TABLE [Attributes]  WITH CHECK ADD  CONSTRAINT [FK_Attributes_Variables] FOREIGN KEY([variableID], [dataSetID])
REFERENCES [Variables] ([variableID], [dataSetID])
ON DELETE CASCADE
GO
ALTER TABLE [Attributes] CHECK CONSTRAINT [FK_Attributes_Variables]
GO
/****** Object:  ForeignKey [FK_Dimensions_DataSets]    Script Date: 12/04/2010 22:29:13 ******/
ALTER TABLE [Dimensions]  WITH CHECK ADD  CONSTRAINT [FK_Dimensions_DataSets] FOREIGN KEY([dataSetID])
REFERENCES [DataSets] ([dataSetID])
ON DELETE CASCADE
GO
ALTER TABLE [Dimensions] CHECK CONSTRAINT [FK_Dimensions_DataSets]
GO
/****** Object:  ForeignKey [FK_Variables_DataSets]    Script Date: 12/04/2010 22:29:13 ******/
ALTER TABLE [Variables]  WITH CHECK ADD  CONSTRAINT [FK_Variables_DataSets] FOREIGN KEY([dataSetID])
REFERENCES [DataSets] ([dataSetID])
ON DELETE CASCADE
GO
ALTER TABLE [Variables] CHECK CONSTRAINT [FK_Variables_DataSets]
GO
/****** Object:  ForeignKey [FK_Variables_Types]    Script Date: 12/04/2010 22:29:13 ******/
ALTER TABLE [Variables]  WITH CHECK ADD  CONSTRAINT [FK_Variables_Types] FOREIGN KEY([typeID])
REFERENCES [Types] ([typeID])
GO
ALTER TABLE [Variables] CHECK CONSTRAINT [FK_Variables_Types]
GO
/****** Object:  ForeignKey [FK_Variables_Dimensions_Dimensions]    Script Date: 12/04/2010 22:29:13 ******/
ALTER TABLE [Variables_Dimensions]  WITH CHECK ADD  CONSTRAINT [FK_Variables_Dimensions_Dimensions] FOREIGN KEY([dimensionID])
REFERENCES [Dimensions] ([dimensionID])
GO
ALTER TABLE [Variables_Dimensions] CHECK CONSTRAINT [FK_Variables_Dimensions_Dimensions]
GO
/****** Object:  ForeignKey [FK_Variables_Dimensions_Variables]    Script Date: 12/04/2010 22:29:13 ******/
ALTER TABLE [Variables_Dimensions]  WITH CHECK ADD  CONSTRAINT [FK_Variables_Dimensions_Variables] FOREIGN KEY([variableID], [dataSetID])
REFERENCES [Variables] ([variableID], [dataSetID])
GO
ALTER TABLE [Variables_Dimensions] CHECK CONSTRAINT [FK_Variables_Dimensions_Variables]
GO

CREATE FUNCTION [dbo].[f_VariableExists](@dataSetID int, @variableID int)
RETURNS bit
AS
BEGIN
DECLARE @result bit
	IF EXISTS(SELECT variableID 
			 FROM Variables 
			 WHERE dataSetID = @dataSetID 
			 AND variableID = @variableID)
			 SET @result = 1
	ELSE
		SET @result = 0
	RETURN @result
END

GO

BEGIN TRANSACTION
GO
ALTER TABLE dbo.DataSets ADD
	version int NULL,
	updateInvoked bit NULL,
	lastUpdateTime datetime NULL
GO
COMMIT

UPDATE DataSets
SET version = 1,  updateInvoked = 0,  lastUpdateTime = GETDATE()

BEGIN TRANSACTION
GO
ALTER TABLE dbo.DataSets ADD CONSTRAINT
	CK_DataSets_version CHECK (version IS NOT NULL)
GO
ALTER TABLE dbo.DataSets ADD CONSTRAINT
	CK_DataSets_updateInvoked CHECK (updateInvoked IS NOT NULL)
GO
ALTER TABLE dbo.DataSets ADD CONSTRAINT
	CK_DataSets_lastUpdateTime CHECK (lastUpdateTime IS NOT NULL)
GO
COMMIT
GO
ALTER PROCEDURE [dbo].[sp_CreateDataSet]
@dataSetID int OUT,
@globalVariableID int,
@deflation int,
@useTransaction bit
AS
BEGIN
BEGIN TRY
		IF @useTransaction=1
		BEGIN
			BEGIN TRANSACTION
		END
		INSERT INTO DataSets(deleted, deflation, [version], [updateInvoked], [lastUpdateTime]) VALUES(0, @deflation, 1, 0, GETDATE())
		SET @dataSetID = @@IDENTITY

		DECLARE @emptyValueTypeID int
		SET @emptyValueTypeID = (SELECT typeID
								 FROM [Types]
								 WHERE name = 'EmptyValueType')
								 
		IF(@emptyValueTypeID IS NULL)
			RAISERROR('There is no EmptyValueType in Types table',16,1);

		INSERT INTO Variables(variableID,dataSetID,typeID)
		VALUES(@globalVariableID, @dataSetID ,@emptyValueTypeID)
		IF @useTransaction=1
		BEGIN
			COMMIT TRANSACTION
		END
	END TRY
	BEGIN CATCH
		IF @useTransaction=1
		BEGIN
			ROLLBACK TRANSACTION
		END
		DECLARE @ErrorMessage NVARCHAR(4000);
		DECLARE @ErrorSeverity INT;
		DECLARE @ErrorState INT;

		SELECT 
			@ErrorMessage = ERROR_MESSAGE(),
			@ErrorSeverity = ERROR_SEVERITY(),
			@ErrorState = ERROR_STATE();

		RAISERROR (@ErrorMessage, -- Message text.
				   @ErrorSeverity, -- Severity.
				   @ErrorState -- State.
				   );

	END CATCH
END
GO
CREATE FUNCTION dbo.f_GetDataSetVersion
(	
	@dataSetID INT
)
RETURNS int 
AS
BEGIN
DECLARE @result int
SET @result = (SELECT [version]
	FROM DataSets
	WHERE dataSetID = @dataSetID)
RETURN @result
END
GO

CREATE PROCEDURE [dbo].[sp_IncreaseDataSetVersion]
@dataSetID int,
@useTransaction bit
AS
BEGIN
	BEGIN TRY
		IF @useTransaction=1
		BEGIN
			BEGIN TRANSACTION
		END
		
		UPDATE DataSets
		SET [version] = [version] + 1
		WHERE dataSetID = @dataSetID
		
		IF @useTransaction=1
		BEGIN
			COMMIT TRANSACTION
		END
	END TRY
	BEGIN CATCH
	IF @useTransaction=1
	BEGIN
		ROLLBACK TRANSACTION
	END
	DECLARE @ErrorMessage NVARCHAR(4000);
    DECLARE @ErrorSeverity INT;
    DECLARE @ErrorState INT;

    SELECT 
        @ErrorMessage = ERROR_MESSAGE(),
        @ErrorSeverity = ERROR_SEVERITY(),
        @ErrorState = ERROR_STATE();

    RAISERROR (@ErrorMessage, -- Message text.
               @ErrorSeverity, -- Severity.
               @ErrorState -- State.
               );

END CATCH
END
GO

CREATE PROCEDURE [dbo].[sp_SetDataSetUpdating]
@dataSetID int,
@updating bit,
@useTransaction bit
AS
BEGIN
	BEGIN TRY
		IF @useTransaction=1
		BEGIN
			BEGIN TRANSACTION
		END
		
		IF @updating = 1
		BEGIN
			UPDATE DataSets
			SET updateInvoked = 1, lastUpdateTime = GETUTCDATE()
			WHERE dataSetID = @dataSetID
		END
		ELSE
		BEGIN
		UPDATE DataSets
			SET updateInvoked = 0
			WHERE dataSetID = @dataSetID
		END
		
		IF @useTransaction=1
		BEGIN
			COMMIT TRANSACTION
		END
	END TRY
	BEGIN CATCH
	IF @useTransaction=1
	BEGIN
		ROLLBACK TRANSACTION
	END
	DECLARE @ErrorMessage NVARCHAR(4000);
    DECLARE @ErrorSeverity INT;
    DECLARE @ErrorState INT;

    SELECT 
        @ErrorMessage = ERROR_MESSAGE(),
        @ErrorSeverity = ERROR_SEVERITY(),
        @ErrorState = ERROR_STATE();

    RAISERROR (@ErrorMessage, -- Message text.
               @ErrorSeverity, -- Severity.
               @ErrorState -- State.
               );

END CATCH
END
GO
CREATE FUNCTION [dbo].[f_IsDataSetUpdating](@dataSetID int, @timeout int)
RETURNS bit
AS
BEGIN

	DECLARE @result bit
	DECLARE @updateInvoked bit
	DECLARE @lastUpdateTime datetime
	SELECT @updateInvoked = updateInvoked, 
		   @lastUpdateTime = lastUpdateTime 
	FROM DataSets
	WHERE dataSetID = @dataSetID
			 
	IF(@updateInvoked = 1 AND DATEDIFF(second , @lastUpdateTime ,  GETUTCDATE()) < @timeout)
		SET @result = 1
	ELSE
		SET @result = 0
	RETURN @result
END
GO