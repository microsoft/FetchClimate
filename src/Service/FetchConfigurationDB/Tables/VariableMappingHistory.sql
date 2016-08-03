CREATE TABLE [dbo].[VariableMappingHistory] (
    [TimeStamp]             DATETIME      DEFAULT (getutcdate()) NOT NULL,
    [DataSourceID]          INT           NOT NULL,
    [DataSourceVariable]    NVARCHAR (64) NOT NULL,
    [EnvironmentalVariable] NVARCHAR (64) NOT NULL,
    [IsProvided]            BIT           NOT NULL,
    [IsEnabled]             BIT           DEFAULT ((1)) NOT NULL,
    PRIMARY KEY CLUSTERED ([TimeStamp] ASC),
    CONSTRAINT [FK_VariableMappings_ToDataSourceIDs] FOREIGN KEY ([DataSourceID]) REFERENCES [dbo].[DataSources] ([ID]),
    CONSTRAINT [FK_VariableMappings_ToEnvironmentalVariables] FOREIGN KEY ([EnvironmentalVariable]) REFERENCES [dbo].[EnvironmentalVariables] ([DisplayName]),
    --UNIQUE NONCLUSTERED ([TimeStamp] ASC, [DataSourceID] ASC, [DataSourceVariable] ASC) --As simultaneous mappings air -> temp and air_land -> temp are valid
);

