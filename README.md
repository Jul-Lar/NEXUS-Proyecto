# NEXUS - Simulador de Exploración Espacial

## 📌 Propósito del Juego
**NEXUS** es un videojuego de exploración espacial táctico y gestión de recursos en 2D desarrollado en C# con Windows Forms y persistencia de datos en SQL Server. El jugador toma el control de una nave exploradora y su tripulación para investigar sectores desconocidos, administrar un suministro crítico de energía, gestionar un inventario de recursos y completar misiones de alta prioridad.

Este proyecto representa la evolución técnica y conceptual de la base de código desarrollada originalmente durante la etapa H3.

---

## 🚀 Funcionalidades Implementadas y Propuestas

* **Exploración por Sectores:** Mapa estelar interactivo en cuadrícula con consumo dinámico de energía por movimiento.
* **Gestión de Exploradores e Inventario:** Control de salud, roles de tripulación y almacenamiento de ítems consumibles y herramientas.
* **Resolución de Misiones:** Eventos interactivos en el espacio con requisitos específicos y recompensas de recursos.
* **Persistencia de Partidas:** Sistema completo de guardado y carga de partidas mediante almacenamiento relacional en SQL Server.
* **Arquitectura en 3 Capas:** Separación limpia entre la Interfaz de Usuario (Presentation), la Lógica del Juego (Business) y el Acceso a Datos (Data Access).

---

## 🛠️ Tecnologías Utilizadas

* **Lenguaje:** C# (.NET Framework / .NET Core)
* **Interfaz Gráfica:** Windows Forms (WinForms)
* **Base de Datos:** Microsoft SQL Server
* **Acceso a Datos:** ADO.NET / Dapper
* **Control de Versiones:** Git & GitHub

---

## 📁 Documentación del Proyecto

Toda la documentación técnica y de diseño requerida para la evolución de NEXUS se encuentra en la carpeta [`docs/`](./docs/):

* 📄 [`docs/diseno-del-juego.md`](./docs/diseno-del-juego.md): Visión, ciclo de juego, mecánicas, diseño de pantallas (wireframes), experiencia de usuario y alcance.
* 📄 [`docs/arquitectura-actual.md`](./docs/arquitectura-actual.md): Análisis de clases, funcionalidades y limitaciones de la versión inicial (H3).
* 📄 [`docs/arquitectura-propuesta.md`](./docs/arquitectura-propuesta.md): Modelo detallado de la arquitectura en 3 capas con diagramas explicativos.
* 📄 [`docs/modelo-de-datos-propuesto.md`](./docs/modelo-de-datos-propuesto.md): Modelo ER, diseño de tablas en SQL Server y relaciones para el guardado de partidas.
* 📄 [`docs/analisis-tecnico.md`](./docs/analisis-tecnico.md): Evaluación de alternativas técnicas, justificación de tecnologías y verificación de fuentes/IA.

---

## ⚙️ Cómo Ejecutar el Proyecto

1. **Clonar el repositorio:**
   ```bash
   git clone [https://github.com/Jul-Lar/NEXUS-Proyecto.git](https://github.com/Jul-Lar/NEXUS-Proyecto.git)
