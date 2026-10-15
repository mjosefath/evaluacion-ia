-- Catálogo de estatus (E1): 1 = Activo, 2 = Inactivo.
-- Sin StatusId propio (catálogo de sistema). Solo CREATE; recrear BD si cambia el esquema.

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE TABLE [dbo].[RecordStatuses] (
    [Id]   TINYINT       NOT NULL,
    [Code] VARCHAR(10)   NOT NULL,
    [Name] NVARCHAR(20)  NOT NULL,
    CONSTRAINT [PK_RecordStatuses] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UX_RecordStatuses_Code] UNIQUE NONCLUSTERED ([Code] ASC),
    CONSTRAINT [UX_RecordStatuses_Name] UNIQUE NONCLUSTERED ([Name] ASC)
);
GO

INSERT INTO [dbo].[RecordStatuses] ([Id], [Code], [Name])
VALUES
    (1, 'ACTIVO', N'Activo'),
    (2, 'INACTIVO', N'Inactivo');
GO
