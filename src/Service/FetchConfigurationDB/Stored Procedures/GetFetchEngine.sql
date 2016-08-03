--====================================================================================================================
--GetFetchEngine - Retrieves FetchEngine for specified @TimeStamp(UTC) from FetchEngine table. Where FetchEngine Timestamp <= @TimeStamp (UTC).
--	@TimeStamp DATETIME
--====================================================================================================================

CREATE PROCEDURE [dbo].[GetFetchEngine]
	@TimeStamp datetime
AS
	SELECT TOP 1 f.FullClrTypeName FROM [dbo].FetchEngineHistory AS f WHERE f.TimeStamp <= @TimeStamp ORDER BY f.TimeStamp DESC