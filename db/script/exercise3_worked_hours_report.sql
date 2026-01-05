USE CCenterRIA;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Report_WorkedHours
AS
BEGIN
    SET NOCOUNT ON;

    /* Empareja logins(1) con su logout(0) dentro de la misma "sesión"
       definida por el contador acumulado de logins por usuario. */

    ;WITH Events AS
    (
        SELECT
            l.Id,
            l.User_id,
            l.TipoMov,
            l.fecha,
            SessionNo =
                SUM(CASE WHEN l.TipoMov = 1 THEN 1 ELSE 0 END)
                OVER (PARTITION BY l.User_id ORDER BY l.fecha, l.Id ROWS UNBOUNDED PRECEDING)
        FROM dbo.ccloglogin AS l
    ),
    Sessions AS
    (
        SELECT
            User_id,
            SessionNo,
            LoginAt  = MIN(CASE WHEN TipoMov = 1 THEN fecha END),
            LogoutAt = MIN(CASE WHEN TipoMov = 0 THEN fecha END)
        FROM Events
        GROUP BY User_id, SessionNo
    ),
    ValidSessions AS
    (
        SELECT
            User_id,
            DurationSeconds = DATEDIFF(SECOND, LoginAt, LogoutAt)
        FROM Sessions
        WHERE
            SessionNo > 0
            AND LoginAt IS NOT NULL
            AND LogoutAt IS NOT NULL
            AND LogoutAt > LoginAt
    ),
    Totals AS
    (
        SELECT
            User_id,
            TotalSeconds = SUM(CAST(DurationSeconds AS BIGINT))
        FROM ValidSessions
        GROUP BY User_id
    )
    SELECT
        u.Login                                         AS [Login],
        LTRIM(RTRIM(CONCAT(u.Nombres, ' ', u.ApellidoPaterno, ' ', ISNULL(u.ApellidoMaterno, ''))))
                                                       AS [FullName],
        COALESCE(a.AreaName, N'')                        AS [Area],
        CAST(COALESCE(t.TotalSeconds, 0) / 3600.0 AS DECIMAL(18,2))
                                                       AS [TotalHours]
    FROM dbo.ccUsers u
    OUTER APPLY
    (
        SELECT TOP (1) ar.AreaName
        FROM dbo.ccRIACat_Areas ar
        WHERE ar.IDArea = u.IDArea
        ORDER BY ar.StatusArea DESC, ar.CreateDate DESC, ar.AreaName
    ) a
    LEFT JOIN Totals t
        ON t.User_id = u.User_id
    ORDER BY [TotalHours] DESC, u.User_id ASC;
END
GO
