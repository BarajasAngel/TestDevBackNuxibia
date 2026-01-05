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

CREATE TABLE dbo.ccloglogin (
  LogId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ccloglogin PRIMARY KEY,
  User_id INT NOT NULL,
  Extension SMALLINT NOT NULL,
  TipoMov TINYINT NOT NULL,
  fecha DATETIME2(0) NOT NULL
);

CREATE TABLE dbo.ccRIACat_Areas (
  IDArea INT NOT NULL,
  AreaName NVARCHAR(200) NOT NULL,
  StatusArea TINYINT NOT NULL,
  CreateDate DATETIME2(3) NOT NULL,
  CONSTRAINT PK_ccRIACat_Areas PRIMARY KEY (IDArea, AreaName)
);
GO

-- Carga con conversion robusta
INSERT INTO dbo.ccUsers (User_id, Login, Nombres, ApellidoPaterno, ApellidoMaterno, [Password], TipoUser_id, [Status], fCreate, IDArea, LastLoginAttempt)
SELECT
  TRY_CONVERT(INT, NULLIF(User_id,'')),
  CAST(NULLIF(Login,'') AS VARCHAR(100)),
  COALESCE(NULLIF(Nombres,''), N''),
  COALESCE(NULLIF(ApellidoPaterno,''), N''),
  NULLIF(ApellidoMaterno,''),
  LEFT(COALESCE(NULLIF([Password],''), ''), 32),
  TRY_CONVERT(INT, NULLIF(TipoUser_id,'')),
  TRY_CONVERT(TINYINT, NULLIF([Status],'')),
  TRY_CONVERT(DATETIME2(3), NULLIF(fCreate,''), 121),
  TRY_CONVERT(INT, NULLIF(IDArea,'')),
  TRY_CONVERT(DATETIME2(3), NULLIF(LastLoginAttempt,''), 121)
FROM stg.ccUsers_raw;

INSERT INTO dbo.ccloglogin (User_id, Extension, TipoMov, fecha)
SELECT
  TRY_CONVERT(INT, NULLIF(User_id,'')),
  TRY_CONVERT(SMALLINT, NULLIF(Extension,'')),
  TRY_CONVERT(TINYINT, NULLIF(TipoMov,'')),
  TRY_CONVERT(DATETIME2(0), NULLIF(fecha,''), 121)
FROM stg.ccloglogin_raw;

INSERT INTO dbo.ccRIACat_Areas (IDArea, AreaName, StatusArea, CreateDate)
SELECT
  TRY_CONVERT(INT, NULLIF(IDArea,'')),
  COALESCE(NULLIF(AreaName,''), N''),
  TRY_CONVERT(TINYINT, NULLIF(StatusArea,'')),
  TRY_CONVERT(DATETIME2(3), NULLIF(CreateDate,''), 121)
FROM stg.ccRIACat_Areas_raw;
GO

-- Índices útiles para performance (consultas típicas)
CREATE INDEX IX_ccloglogin_User_Fecha ON dbo.ccloglogin(User_id, fecha) INCLUDE (TipoMov, Extension);
CREATE INDEX IX_ccUsers_IDArea ON dbo.ccUsers(IDArea);
GO
