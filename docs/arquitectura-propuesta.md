# Arquitectura Propuesta para la Evolución de NEXUS

## 1. Modelo de Arquitectura en Capas
Para evolucionar el proyecto hacia una aplicación de escritorio completa en C# con Windows Forms y persistencia de datos, se adopta un diseño en **3 capas independientes**. Esto garantiza mantenibilidad, escalabilidad y la correcta separación de responsabilidades.
┌────────────────────────────────────────────────────────┐
│     Capa de Presentación (Interfaz de Usuario)        │
│      - Formularios de C# Windows Forms                 │
│      - Mapa Gráfico, Pantalla de Misiones e Inventario │
└───────────────────────────┬────────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────┐
│         Capa de Lógica de Negocio (Domain)             │
│      - Clases de C# (Jugador, Explorador, Misión)     │
│      - Reglas del Juego (Costo de Energía, Eventos)   │
└───────────────────────────┬────────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────┐
│      Capa de Acceso a Datos (DAL / Persistence)        │
│      - Patrón Repository / Consultas SQL              │
│      - Mapeo de Objetos a Tablas                       │
└───────────────────────────┬────────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────┐
│            Base de Datos (SQL Server)                  │
│      - Tablas: Jugador, Inventario, Misiones           │
└────────────────────────────────────────────────────────┘

## 2. Descripción de Componentes

1. **Capa de Presentación (Windows Forms):**
   * Responsable de la interfaz visual interactiva.
   * Contiene los wireframes renderizados (Menú Principal, Grid del Mapa, Vista de Misiones y Gestión de Inventario).
   * Captura los eventos del usuario (clics en sectores) y delega la ejecución de reglas a la capa de lógica.

2. **Capa de Lógica de Negocio (C# Core Logic):**
   * Evoluciona el código original de la versión H3.
   * Procesa la validación de movimientos, descuento de energía y el cálculo de probabilidad de éxito en misiones.
   * Es completamente independiente de la interfaz gráfica y de la base de datos.

3. **Capa de Acceso a Datos (DAL - Data Access Layer):**
   * Encargada de comunicarse con SQL Server mediante `ADO.NET` o `Dapper`.
   * Implementa funciones para guardar el progreso del jugador, guardar y cargar partidas, y consultar el inventario.

---

## 3. Justificación Técnica
* **Mantenibilidad:** Si se modifica la interfaz gráfica de Windows Forms, la lógica interna del juego no se ve afectada.
* **Viabilidad:** Permite reusar directamente las clases desarrolladas en H3 adaptándolas al modelo de capas.
* **Experiencia del Jugador:** Ofrece una transición fluida al guardar el progreso en SQL Server y retomar partidas guardadas.
