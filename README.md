# NEXUS v3 — Simulador de Exploración Espacial y Memoria Persistente

Videojuego desarrollado en **C# (Windows Forms)** con persistencia relacional en **SQL Server** mediante **ADO.NET**[cite: 1, 3, 4].

## 📁 Estructura del Repositorio

* `NexusForms/` y `mision1 nexus/`: Código fuente en C#, formularios Windows Forms (`FormIntro`, `FormInicio`, `FormJuego`, modos `1945`, `Metal Slug` y `2.5D`), motor lógico (`NexusMotor.cs`) y acceso a datos (`GestorGuardado.cs`)[cite: 2, 3, 4].
* `Database/`: Script SQL completo para reconstruir la base de datos (`01_NEXUS_DB_Completa.sql`).
* `docs/`: Diagrama Entidad-Relación (`DER_NEXUS.png`), documentación técnica y evidencias de pruebas[cite: 2, 7].

## ⚙️ Requisitos Previos

* Windows 10/11 con **.NET Desktop Runtime / .NET Framework** instalado.
* **Microsoft SQL Server** y SQL Server Management Studio (SSMS).
* Visual Studio 2022 (solo para compilar desde el código fuente).

## 🚀 Instalación y Ejecución desde Cero (Clonación Limpia)

### 1. Reconstruir la Base de Datos en SQL Server
1. Abre **SQL Server Management Studio (SSMS)** y conéctate a tu instancia local (ej. `localhost` o `.\SQLEXPRESS`).
2. Abre y ejecuta el archivo `Database/01_NEXUS_DB_Completa.sql` para crear automáticamente la base de datos `NEXUS_DB`, las 5 tablas relacionales (`Jugadores`, `Naves`, `Partidas`, `InventarioPartida`, `EstadoMisionPartida`) y los registros iniciales[cite: 4].

### 2. Configurar la Cadena de Conexión y Compilar
1. Clona el repositorio:
   ```bash
   git clone [https://github.com/Jul-Lar/NEXUS-Proyecto.git](https://github.com/Jul-Lar/NEXUS-Proyecto.git)
