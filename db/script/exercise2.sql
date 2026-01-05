USE CCenterRIA;
GO

/* =========================
   Base: sesiones válidas
   ========================= */
WITH Events AS
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
        LoginAt,
        LogoutAt,
        DurationSeconds = DATEDIFF(SECOND, LoginAt, LogoutAt)
    FROM Sessions
    WHERE
        SessionNo > 0
        AND LoginAt IS NOT NULL
        AND LogoutAt IS NOT NULL
        AND LogoutAt > LoginAt
),
UserTotals AS
(
    SELECT
        User_id,
        TotalSeconds = SUM(CAST(DurationSeconds AS BIGINT))
    FROM ValidSessions
    GROUP BY User_id
)

/* =========================
   1) Usuario con MÁS tiempo logueado
   ========================= */
SELECT TOP (1)
    ut.User_id,
    ut.TotalSeconds,
    CONCAT(
        ut.TotalSeconds / 86400, ' días, ',
        (ut.TotalSeconds % 86400) / 3600, ' horas, ',
        (ut.TotalSeconds % 3600) / 60, ' minutos, ',
        ut.TotalSeconds % 60, ' segundos'
    ) AS TiempoTotal
FROM UserTotals ut
WHERE ut.TotalSeconds > 0
ORDER BY ut.TotalSeconds DESC;
GO

/* =========================
   2) Usuario con MENOS tiempo logueado
   ========================= */
WITH Events AS
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
        LoginAt,
        LogoutAt,
        DurationSeconds = DATEDIFF(SECOND, LoginAt, LogoutAt)
    FROM Sessions
    WHERE
        SessionNo > 0
        AND LoginAt IS NOT NULL
        AND LogoutAt IS NOT NULL
        AND LogoutAt > LoginAt
),
UserTotals AS
(
    SELECT
        User_id,
        TotalSeconds = SUM(CAST(DurationSeconds AS BIGINT))
    FROM ValidSessions
    GROUP BY User_id
)
SELECT TOP (1)
    ut.User_id,
    ut.TotalSeconds,
    CONCAT(
        ut.TotalSeconds / 86400, ' días, ',
        (ut.TotalSeconds % 86400) / 3600, ' horas, ',
        (ut.TotalSeconds % 3600) / 60, ' minutos, ',
        ut.TotalSeconds % 60, ' segundos'
    ) AS TiempoTotal
FROM UserTotals ut
WHERE ut.TotalSeconds > 0
ORDER BY ut.TotalSeconds ASC;
GO

/* =========================
   3) Promedio de logueo por mes (promedio por sesión del mes)
   ========================= */
WITH Events AS
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
        LoginAt,
        LogoutAt,
        DurationSeconds = DATEDIFF(SECOND, LoginAt, LogoutAt),
        [Month] = DATEFROMPARTS(YEAR(LoginAt), MONTH(LoginAt), 1)
    FROM Sessions
    WHERE
        SessionNo > 0
        AND LoginAt IS NOT NULL
        AND LogoutAt IS NOT NULL
        AND LogoutAt > LoginAt
),
Monthly AS
(
    SELECT
        User_id,
        [Month],
        SessionsCount = COUNT_BIG(*),
        AvgSeconds = AVG(CAST(DurationSeconds AS BIGINT)),
        TotalSeconds = SUM(CAST(DurationSeconds AS BIGINT))
    FROM ValidSessions
    GROUP BY User_id, [Month]
)
SELECT
    User_id,
    [Month],
    SessionsCount,
    AvgSeconds,
    CONCAT(
        AvgSeconds / 86400, ' días, ',
        (AvgSeconds % 86400) / 3600, ' horas, ',
        (AvgSeconds % 3600) / 60, ' minutos, ',
        AvgSeconds % 60, ' segundos'
    ) AS PromedioPorSesion,
    TotalSeconds
FROM Monthly
ORDER BY [Month], User_id;
GO
