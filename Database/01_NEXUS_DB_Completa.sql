-- 1. Crear Base de Datos NEXUS_DB
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'NEXUS_DB')
BEGIN
    CREATE DATABASE NEXUS_DB;
END
GO
USE NEXUS_DB;
GO

-- 2. Crear las 5 Tablas Relacionadas
CREATE TABLE Jugadores (
    IdJugador INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(80) NOT NULL UNIQUE,
    EsElite BIT NOT NULL DEFAULT 0,
    ColorTemaArgb INT NOT NULL,
    FechaRegistro DATETIME NOT NULL DEFAULT GETDATE()
);

CREATE TABLE Naves (
    IdNave INT IDENTITY(1,1) PRIMARY KEY,
    NombreNave NVARCHAR(80) NOT NULL UNIQUE,
    ClaseNave NVARCHAR(50) NOT NULL
);

CREATE TABLE Partidas (
    IdPartida VARCHAR(16) PRIMARY KEY,
    IdJugador INT NOT NULL,
    IdNave INT NOT NULL,
    RealidadAsignada NVARCHAR(50) NOT NULL,
    NivelEnergia INT NOT NULL CHECK (NivelEnergia BETWEEN 0 AND 100),
    NivelEstabilidad INT NOT NULL CHECK (NivelEstabilidad BETWEEN 0 AND 100),
    NivelPeligro INT NOT NULL CHECK (NivelPeligro BETWEEN 0 AND 100),
    FragmentosDatos INT NOT NULL DEFAULT 0,
    XP INT NOT NULL DEFAULT 0,
    EscudosActivos BIT NOT NULL DEFAULT 1,
    FechaGuardado DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Partidas_Jugadores FOREIGN KEY (IdJugador) REFERENCES Jugadores(IdJugador),
    CONSTRAINT FK_Partidas_Naves FOREIGN KEY (IdNave) REFERENCES Naves(IdNave)
);

CREATE TABLE InventarioPartida (
    IdInventario INT IDENTITY(1,1) PRIMARY KEY,
    IdPartida VARCHAR(16) NOT NULL,
    NombreItem NVARCHAR(80) NOT NULL,
    Cantidad INT NOT NULL CHECK (Cantidad >= 0),
    CONSTRAINT FK_Inventario_Partidas FOREIGN KEY (IdPartida) REFERENCES Partidas(IdPartida) ON DELETE CASCADE
);

CREATE TABLE EstadoMisionPartida (
    IdMision INT IDENTITY(1,1) PRIMARY KEY,
    IdPartida VARCHAR(16) NOT NULL UNIQUE,
    ZonaAlcanzada BIT NOT NULL DEFAULT 0,
    NucleoEncontrado BIT NOT NULL DEFAULT 0,
    EntidadSobrevivida BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Mision_Partidas FOREIGN KEY (IdPartida) REFERENCES Partidas(IdPartida) ON DELETE CASCADE
);
GO

-- 3. Insertar Datos Iniciales de Naves
INSERT INTO Naves (NombreNave, ClaseNave) VALUES
('Caza Estelar V-9', 'Interceptor'),
('Interceptor Sombra', 'Sigilo'),
('Acorazado Titán', 'Asalto Pesado'),
('Explorador Cuántico', 'Reconocimiento');
GO