-- Escapa comodines LIKE del usuario (B6): %, _, [

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE FUNCTION [dbo].[fn_EscapeLikePattern] (@Text NVARCHAR(4000))
RETURNS NVARCHAR(4000)
WITH SCHEMABINDING
AS
BEGIN
    IF @Text IS NULL
        RETURN N'';

    SET @Text = REPLACE(@Text, N'[', N'[[]');
    SET @Text = REPLACE(@Text, N'%', N'[%]');
    SET @Text = REPLACE(@Text, N'_', N'[_]');
    RETURN @Text;
END;
GO
