CREATE TABLE [dbo].[EnvironmentalVariables] (
    [DisplayName] NVARCHAR (64)  NOT NULL,
    [Units]       NVARCHAR (64)  NOT NULL,
    [Description] NVARCHAR (MAX) NOT NULL,
    PRIMARY KEY CLUSTERED ([DisplayName] ASC)
);
