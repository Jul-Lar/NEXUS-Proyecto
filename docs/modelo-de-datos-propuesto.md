# Modelo de Datos Propuesto para SQL Server

## 1. Estrategia de Persistencia
El sistema utilizará una base de datos relacional en **SQL Server** para permitir guardar y recuperar el progreso del jugador entre sesiones de juego. La persistencia registrará el estado del jugador, los exploradores asignados, el progreso en el mapa y los elementos en el inventario.

---

## 2. Entidades y Tablas Principales

### Tabla: `Jugadores`
* `IdJugador` (INT, Primary Key, Identity): Identificador único del jugador.
* `Nombre` (VARCHAR(50)): Nombre del comandante/jugador.
* `EnergiaActual` (INT): Cantidad de energía disponible.
* `SectorActualX` (INT): Coordenada X actual en el mapa espacial.
* `SectorActualY` (INT): Coordenada Y actual en el mapa espacial.
* `FechaGuardado` (DATETIME): Última fecha de guardado de partida.

### Tabla: `Exploradores`
* `IdExplorador` (INT, Primary Key, Identity): Identificador único del explorador.
* `Nombre` (VARCHAR(50)): Nombre del explorador.
* `Rol` (VARCHAR(30)): Especialidad (Ej. Pilotaje, Ciencia, Resistencia).
* `Salud` (INT): Puntos de salud actuales.
* `IdJugador` (INT, Foreign Key -> `Jugadores.IdJugador`): Jugador al que pertenece.

### Tabla: `Misiones`
* `IdMision` (INT, Primary Key, Identity): Identificador único de la misión.
* `Nombre` (VARCHAR(100)): Título de la misión o anomalía.
* `Descripcion` (TEXT): Requisitos y detalles del evento.
* `Estado` (VARCHAR(20)): Estado actual (`Pendiente`, `Completada`, `Fallida`).
* `CoordenadaX` (INT): Ubicación en el mapa.
* `CoordenadaY` (INT): Ubicación en el mapa.

### Tabla: `Inventarios`
* `IdInventario` (INT, Primary Key, Identity): Registro de almacenamiento.
* `IdJugador` (INT, Foreign Key -> `Jugadores.IdJugador`): Dueño del ítem.
* `NombreItem` (VARCHAR(50)): Nombre del recurso u objeto.
* `Cantidad` (INT): Unidades almacenadas.

---

## 3. Relaciones entre Entidades
* **Jugadores (1) ── (N) Exploradores:** Un jugador posee varios exploradores a su mando.
* **Jugadores (1) ── (N) Inventarios:** Un jugador posee múltiples ítems en su inventario.
* **Jugadores (1) ── (N) Misiones:** El estado de resolución de las misiones está vinculado al progreso del jugador.

---

## 4. Estrategia de Guardado y Carga
* **Guardar Partida:** Se ejecuta una transacción SQL que actualiza la posición y energía en `Jugadores`, el estado de `Exploradores` y sincroniza las filas de `Inventarios`.
* **Cargar Partida:** Se consulta la tabla `Jugadores` por ID y se reconstruyen en C# los objetos de la partida con sus colecciones de exploradores e inventario asociadas.
