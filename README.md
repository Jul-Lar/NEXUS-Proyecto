# NEXUS v3 — Arquitectura y Persistencia Relacional

Videojuego de acción desarrollado en **C# (.NET / Windows Forms)** con arquitectura modular, motor de renderizado propio y persistencia relacional transaccional en **Microsoft SQL Server (`NEXUS_DB`)**.

---

## 1. Estructura del Repositorio

* **`NexusForms/`**: Proyecto principal de Windows Forms y lógica del videojuego.
  * `FormIntro.cs`, `FormInicio.cs`: Gestión de perfiles, selección de nave/sector y carga de partidas guardadas.
  * `FormJuego.cs`, `FormAviones1945.cs`, `FormExploracionMetalSlug.cs`: Modos jugables y eventos de interfaz.
  * `NexusMotor.cs`: Modelo de dominio (`Jugador`, `EstadoMision`, atributos y reglas del juego).
  * `GestorGuardado.cs`: Capa de acceso a datos (ADO.NET / `Microsoft.Data.SqlClient`) con transacciones SQL (`SqlTransaction`).
  * `AsistenteVozIA.cs`, `ControlMando.cs`, `ControlMovilServer.cs`: Módulos auxiliares de interacción.
* **`Database/`**: Scripts SQL reproducibles.
  * `01_NEXUS_DB_Completa.sql`: Script integral que crea la base de datos `NEXUS_DB`, sus 5 tablas relacionales, llaves primarias/foráneas y restricciones de integridad.

---

## 2. Modelo Entidad-Relación (Base de Datos `NEXUS_DB`)

```mermaid
erDiagram
    JUGADORES ||--o{ PARTIDAS : "juega"
    NAVES ||--o{ PARTIDAS : "equipa"
    PARTIDAS ||--o{ INVENTARIO_PARTIDA : "contiene"
    PARTIDAS ||--|| ESTADO_MISION_PARTIDA : "registra"

    JUGADORES {
        int IdJugador PK
        nvarchar Nombre
        bit EsElite
        int ColorTemaArgb
    }

    NAVES {
        int IdNave PK
        nvarchar NombreNave
        nvarchar ClaseNave
    }

    PARTIDAS {
        nvarchar IdPartida PK
        int IdJugador FK
        int IdNave FK
        nvarchar RealidadAsignada
        int NivelEnergia
        int NivelEstabilidad
        int NivelPeligro
        int FragmentosDatos
        int XP
        bit EscudosActivos
        datetime FechaGuardado
    }

    INVENTARIO_PARTIDA {
        int IdInventario PK
        nvarchar IdPartida FK
        nvarchar NombreItem
        int Cantidad
    }

    ESTADO_MISION_PARTIDA {
        int IdMision PK
        nvarchar IdPartida FK
        bit ZonaAlcanzada
        bit NucleoEncontrado
        bit EntidadSobrevivida
    }