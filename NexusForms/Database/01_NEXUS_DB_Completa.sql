USE master;
GO

IF DB_ID('NEXUS_DB') IS NOT NULL
BEGIN
    ALTER DATABASE NEXUS_DB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE NEXUS_DB;
END
GO

CREATE DATABASE NEXUS_DB;
GO

USE NEXUS_DB;
GO

CREATE TABLE Jugadores (
    IdJugador INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(80) NOT NULL UNIQUE,
    EsElite BIT NOT NULL DEFAULT 0,
    ColorTemaArgb INT NOT NULL,
    FechaRegistro DATETIME NOT NULL DEFAULT GETDATE()
);
GO

CREATE TABLE Naves (
    IdNave INT IDENTITY(1,1) PRIMARY KEY,
    NombreNave NVARCHAR(100) NOT NULL UNIQUE,
    ClaseNave NVARCHAR(50) NOT NULL DEFAULT 'Estándar'
);
GO

CREATE TABLE Partidas (
    IdPartida NVARCHAR(50) PRIMARY KEY,
    IdJugador INT NOT NULL,
    IdNave INT NOT NULL,
    RealidadAsignada NVARCHAR(50) NOT NULL,
    NivelEnergia INT NOT NULL DEFAULT 100,
    NivelEstabilidad INT NOT NULL DEFAULT 100,
    NivelPeligro INT NOT NULL DEFAULT 0,
    FragmentosDatos INT NOT NULL DEFAULT 0,
    XP INT NOT NULL DEFAULT 0,
    EscudosActivos BIT NOT NULL DEFAULT 1,
    FechaGuardado DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Partidas_Jugadores FOREIGN KEY (IdJugador) REFERENCES Jugadores(IdJugador) ON DELETE CASCADE,
    CONSTRAINT FK_Partidas_Naves FOREIGN KEY (IdNave) REFERENCES Naves(IdNave)
);
GO

CREATE TABLE InventarioPartida (
    IdInventario INT IDENTITY(1,1) PRIMARY KEY,
    IdPartida NVARCHAR(50) NOT NULL,
    NombreItem NVARCHAR(100) NOT NULL,
    Cantidad INT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Inventario_Partidas FOREIGN KEY (IdPartida) REFERENCES Partidas(IdPartida) ON DELETE CASCADE
);
GO

CREATE TABLE EstadoMisionPartida (
    IdMision INT IDENTITY(1,1) PRIMARY KEY,
    IdPartida NVARCHAR(50) NOT NULL UNIQUE,
    ZonaAlcanzada BIT NOT NULL DEFAULT 0,
    NucleoEncontrado BIT NOT NULL DEFAULT 0,
    EntidadSobrevivida BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Mision_Partidas FOREIGN KEY (IdPartida) REFERENCES Partidas(IdPartida) ON DELETE CASCADE
);
GO

INSERT INTO Naves (NombreNave, ClaseNave) VALUES
('Caza Estelar V-9', 'Interceptor'),
('Interceptor Sombra', 'Sigilo'),
('Acorazado Titán', 'Asalto Pesado'),
('Explorador Cuántico', 'Reconocimiento');
GO