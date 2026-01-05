SET NOCOUNT ON;
GO

IF DB_ID('CCenterRIA') IS NULL
BEGIN
  CREATE DATABASE CCenterRIA;
END
GO

USE CCenterRIA;
GO

IF SCHEMA_ID('stg') IS NULL
  EXEC('CREATE SCHEMA stg');
GO

-- Drop (idempotente)
IF OBJECT_ID('stg.ccUsers_raw') IS NOT NULL DROP TABLE stg.ccUsers_raw;
IF OBJECT_ID('stg.ccloglogin_raw') IS NOT NULL DROP TABLE stg.ccloglogin_raw;
IF OBJECT_ID('stg.ccRIACat_Areas_raw') IS NOT NULL DROP TABLE stg.ccRIACat_Areas_raw;

IF OBJECT_ID('dbo.ccUsers') IS NOT NULL DROP TABLE dbo.ccUsers;
IF OBJECT_ID('dbo.ccloglogin') IS NOT NULL DROP TABLE dbo.ccloglogin;
IF OBJECT_ID('dbo.ccRIACat_Areas') IS NOT NULL DROP TABLE dbo.ccRIACat_Areas;
GO

-- Staging raw (todo string para evitar fallos por null/format)
CREATE TABLE stg.ccUsers_raw (
  User_id NVARCHAR(50) NULL,
  Login NVARCHAR(200) NULL,
  Nombres NVARCHAR(200) NULL,
  ApellidoPaterno NVARCHAR(200) NULL,
  ApellidoMaterno NVARCHAR(200) NULL,
  [Password] NVARCHAR(200) NULL,
  TipoUser_id NVARCHAR(50) NULL,
  [Status] NVARCHAR(50) NULL,
  fCreate NVARCHAR(50) NULL,
  IDArea NVARCHAR(50) NULL,
  LastLoginAttempt NVARCHAR(50) NULL
);

CREATE TABLE stg.ccloglogin_raw (
  User_id NVARCHAR(50) NULL,
  Extension NVARCHAR(50) NULL,
  TipoMov NVARCHAR(50) NULL,
  fecha NVARCHAR(50) NULL
);

CREATE TABLE stg.ccRIACat_Areas_raw (
  IDArea NVARCHAR(50) NULL,
  AreaName NVARCHAR(200) NULL,
  StatusArea NVARCHAR(50) NULL,
  CreateDate NVARCHAR(50) NULL
);
GO

-- BULK INSERT (Linux path dentro del contenedor SQL)
BULK INSERT stg.ccUsers_raw
FROM '/var/opt/mssql/import/ccUsers.tsv'
WITH (
  FIRSTROW = 2,
  FIELDTERMINATOR = '	',
  ROWTERMINATOR = '0x0a',
  TABLOCK
);

BULK INSERT stg.ccloglogin_raw
FROM '/var/opt/mssql/import/ccloglogin.tsv'
WITH (
  FIRSTROW = 2,
  FIELDTERMINATOR = '	',
  ROWTERMINATOR = '0x0a',
  TABLOCK
);

BULK INSERT stg.ccRIACat_Areas_raw
FROM '/var/opt/mssql/import/ccRIACat_Areas.tsv'
WITH (
  FIRSTROW = 2,
  FIELDTERMINATOR = '	',
  ROWTERMINATOR = '0x0a',
  TABLOCK
);
GO

-- Tablas finales tipadas
CREATE TABLE dbo.ccUsers (
  User_id INT NOT NULL CONSTRAINT PK_ccUsers PRIMARY KEY,
  Login VARCHAR(100) NOT NULL CONSTRAINT UQ_ccUsers_Login UNIQUE,
  Nombres NVARCHAR(200) NOT NULL,
  ApellidoPaterno NVARCHAR(200) NOT NULL,
  ApellidoMaterno NVARCHAR(200) NULL,
  [Password] CHAR(32) NOT NULL,
  TipoUser_id INT NOT NULL,
  [Status] TINYINT NOT NULL,
  fCreate DATETIME2(3) NOT NULL,
  IDArea INT NOT NULL,
  LastLoginAttempt DATETIME2(3) NULL
);

-- ccloglogin con Id identity PK (necesario para PUT/DELETE /logins/{id})
CREATE TABLE dbo.ccloglogin
(
    Id        BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ccloglogin PRIMARY KEY,
    User_id   INT NOT NULL,
    Extension INT NOT NULL,
    TipoMov   TINYINT NOT NULL,
    fecha     DATETIME2(0) NOT NULL,
    CONSTRAINT CK_ccloglogin_TipoMov CHECK (TipoMov IN (0,1))
);

CREATE TABLE dbo.ccRIACat_Areas (
  IDArea INT NOT NULL,
  AreaName NVARCHAR(200) NOT NULL,
  StatusArea TINYINT NOT NULL,
  CreateDate DATETIME2(3) NOT NULL,
  CONSTRAINT PK_ccRIACat_Areas PRIMARY KEY (IDArea, AreaName)
);
GO

-- Índices útiles para performance (consultas típicas)
CREATE INDEX IX_ccloglogin_User_Fecha
ON dbo.ccloglogin(User_id, fecha)
INCLUDE (TipoMov, Extension);

CREATE INDEX IX_ccUsers_IDArea ON dbo.ccUsers(IDArea);
GO

/* =========================
   Carga con conversión robusta
   - Se limpia CHAR(13) por si el TSV trae \r\n
   - Se filtran filas inválidas para no romper NOT NULL/UNIQUE
   ========================= */

-- ccUsers
INSERT INTO dbo.ccUsers
(
  User_id, Login, Nombres, ApellidoPaterno, ApellidoMaterno,
  [Password], TipoUser_id, [Status], fCreate, IDArea, LastLoginAttempt
)
SELECT
  v.User_id,
  v.Login,
  v.Nombres,
  v.ApellidoPaterno,
  v.ApellidoMaterno,
  v.[Password],
  v.TipoUser_id,
  v.[Status],
  v.fCreate,
  v.IDArea,
  v.LastLoginAttempt
FROM stg.ccUsers_raw r
CROSS APPLY
(
  SELECT
    TRY_CONVERT(INT, NULLIF(REPLACE(r.User_id, CHAR(13), ''), '')) AS User_id,
    CAST(NULLIF(REPLACE(r.Login, CHAR(13), ''), '') AS VARCHAR(100)) AS Login,
    COALESCE(NULLIF(REPLACE(r.Nombres, CHAR(13), ''), ''), N'') AS Nombres,
    COALESCE(NULLIF(REPLACE(r.ApellidoPaterno, CHAR(13), ''), ''), N'') AS ApellidoPaterno,
    NULLIF(REPLACE(r.ApellidoMaterno, CHAR(13), ''), '') AS ApellidoMaterno,
    CONVERT(CHAR(32), LEFT(COALESCE(NULLIF(REPLACE(r.[Password], CHAR(13), ''), ''), '0'), 32)) AS [Password],
    TRY_CONVERT(INT, NULLIF(REPLACE(r.TipoUser_id, CHAR(13), ''), '')) AS TipoUser_id,
    TRY_CONVERT(TINYINT, NULLIF(REPLACE(r.[Status], CHAR(13), ''), '')) AS [Status],
    TRY_CONVERT(DATETIME2(3), NULLIF(REPLACE(r.fCreate, CHAR(13), ''), '')) AS fCreate,
    TRY_CONVERT(INT, NULLIF(REPLACE(r.IDArea, CHAR(13), ''), '')) AS IDArea,
    TRY_CONVERT(DATETIME2(3), NULLIF(REPLACE(r.LastLoginAttempt, CHAR(13), ''), '')) AS LastLoginAttempt
) v
WHERE
  v.User_id IS NOT NULL
  AND v.Login IS NOT NULL
  AND v.TipoUser_id IS NOT NULL
  AND v.[Status] IS NOT NULL
  AND v.fCreate IS NOT NULL
  AND v.IDArea IS NOT NULL;
GO

-- ccloglogin 
INSERT INTO dbo.ccloglogin (User_id, Extension, TipoMov, fecha)
SELECT
    v.User_id,
    v.Extension,
    v.TipoMov,
    v.fecha
FROM stg.ccloglogin_raw r
CROSS APPLY (
    SELECT
        TRY_CONVERT(INT,     NULLIF(REPLACE(r.User_id, CHAR(13), ''), '')) AS User_id,
        TRY_CONVERT(INT,     NULLIF(REPLACE(r.Extension, CHAR(13), ''), '')) AS Extension,
        TRY_CONVERT(TINYINT, NULLIF(REPLACE(r.TipoMov, CHAR(13), ''), '')) AS TipoMov,
        TRY_CONVERT(DATETIME2(0), NULLIF(REPLACE(r.fecha, CHAR(13), ''), '')) AS fecha
) v
WHERE
    v.User_id IS NOT NULL
    AND v.Extension IS NOT NULL
    AND v.TipoMov IN (0,1)
    AND v.fecha IS NOT NULL;
GO

-- ccRIACat_Areas
INSERT INTO dbo.ccRIACat_Areas (IDArea, AreaName, StatusArea, CreateDate)
SELECT
  v.IDArea,
  v.AreaName,
  v.StatusArea,
  v.CreateDate
FROM stg.ccRIACat_Areas_raw r
CROSS APPLY
(
  SELECT
    TRY_CONVERT(INT, NULLIF(REPLACE(r.IDArea, CHAR(13), ''), '')) AS IDArea,
    COALESCE(NULLIF(REPLACE(r.AreaName, CHAR(13), ''), ''), N'') AS AreaName,
    TRY_CONVERT(TINYINT, NULLIF(REPLACE(r.StatusArea, CHAR(13), ''), '')) AS StatusArea,
    TRY_CONVERT(DATETIME2(3), NULLIF(REPLACE(r.CreateDate, CHAR(13), ''), '')) AS CreateDate
) v
WHERE
  v.IDArea IS NOT NULL
  AND v.StatusArea IS NOT NULL
  AND v.CreateDate IS NOT NULL;
GO
