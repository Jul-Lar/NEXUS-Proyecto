# NEXUS — Manual Técnico y Preparación para el Examen H3

**Universidad Privada "Franz Tamayo"**  
**Asignatura:** Programación II  
**Estudiante:** Julio Lara Laura  
**Docente:** Ing. Diego Patrick Cárdenas Sejas  
**Gestión:** 2026  

---

## 3. Descubrimiento del Proyecto con Herramientas Modernas

Para la auditoría, análisis y comprensión profunda de la estructura de código del simulador **NEXUS**, se emplearon herramientas modernas de ingeniería de software y asistentes de Inteligencia Artificial:

* **Gitingest:** Convierte la estructura completa del repositorio y sus archivos fuente (`.cs`, `.csproj`, `.slnx`) en un texto plano estructurado y formateado en Markdown, facilitando el análisis contextual rápido.
* **GitDiagram:** Genera mapas de arquitectura y diagramas visuales automáticos a partir de los directorios y clases expuestas en el repositorio público de GitHub.
* **Repomix:** Organiza, empaqueta e hidrata el código fuente eliminando metadatos innecesarios para enviar un contexto limpio y optimizado a modelos de lenguaje (LLM).
* **DeepWiki:** Asistente de exploración que genera documentación viva y permite investigar las dependencias e iteraciones de funciones dentro del código C#.
* **GitMCP (Model Context Protocol):** Conecta el estado en tiempo real del repositorio local y remoto con asistentes de IA compatibles para realizar consultas sobre la arquitectura.

---

## 4. Manual Técnico de Programación II

### I. Arquitectura y Programación Orientada a Objetos

#### Propósito y funcionamiento general
**NEXUS** es un sistema de simulación interactivo en consola desarrollado en C#. Representa un traje táctico espacial donde el usuario controla a un explorador en diversos sectores cuánticos, administrando recursos vitales (Energía, Estabilidad y Peligro), enfrentando entidades mediante combate/hackeo, mapeando territorio procedimental y consultando a una IA táctica llamada **IRIS** integrada con la API de Google Gemini.

#### Estructura del proyecto y responsabilidades de clases

| Clase / Componente | Tipo | Responsabilidad Principal |
| :--- | :--- | :--- |
| `Program` | `static class` | Contiene el punto de entrada (`Main`), el bucle principal de simulación (`BuclePrincipalSimulacion`), menús interactivos, renderizado ASCII/cartográfico y lógica de eventos. |
| `Jugador` | `class` (Hereda de `Personaje`) | Modela las estadísticas del explorador (Energía, Estabilidad, Peligro, Escudos, Rango Élite), inventario de suministros, registro de logros y estado de misión activo. |
| `ServicioIA` | `static class` | Gestiona la comunicación HTTP asíncrona con la API de Gemini (`HttpClient`), procesamiento de JSON (`JsonDocument`) y síntesis de voz (`SpeechSynthesizer`). |
| `Personaje` | `abstract class` | Superclase abstracta que define la propiedad fundamental compartida: `Nombre`. |
| `EntidadSistema` | `class` (Hereda de `Personaje`) | Modela entes interactivos con atributos de `Tipo` y `Salud`. |
| `Robot` | `class` (Hereda de `EntidadSistema`) | Entidad defensiva especializada con salud inicial de 80. |
| `Enemigo` | `class` (Hereda de `EntidadSistema`) | Entidad hostil "Sombra Entrópica" con salud inicial de 120. |
| `EstadoMision` | `class` | Estructura de datos y lógica booleana para evaluar el cumplimiento de los 4 hitos de la misión "La Anomalía Perdida". |

#### Diagrama de Clases y Relaciones entre Objetos

```text
|   abstract Personaje    |
+-------------------------+
| + Nombre: string        |
+-------------------------+
           ^
           | (Herencia)
    +------+------------------------+
    |                               |
|        Jugador          |  |     EntidadSistema      |
+-------------------------+  +-------------------------+
| + RealidadAsignada      |  | + Tipo: string          |
| + NivelEnergia: int     |  | + Salud: int            |
| + NivelEstabilidad: int |  +-------------------------+
| + NivelPeligro: int     |               ^
| + Inventario: Dictionary|               | (Herencia)
| + Logros: List<string>  |       +-------+-------+
| + MisionActual          |       |               |
+-------------------------+  |    Robot   | |   Enemigo   |
           | (Composición)   +------------+ +-------------+
           v
|      EstadoMision       |
+-------------------------+
| + ZonaAlcanzada: bool   |
| + NucleoEncontrado: bool|
| + EsCompletada(): bool  |
+-------------------------+
