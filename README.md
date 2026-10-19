# NEXUS - Simulador de Exploración Espacial 🌌

NEXUS es una aventura de simulación espacial interactiva en consola desarrollada en C#. El jugador asume el rol de un explorador que gestiona sus niveles de Energía, Estabilidad y Peligro mientras navega por diversas realidades cuánticas, enfrenta anomalías y realiza consultas tácticas a una IA integrada.

## 🚀 Características Principales
* **Gestión de Supervivencia:** Control continuo de estadísticas vitales (Energía, Estabilidad, Peligro, Escudos).
* **Asistente Táctico con IA (IRIS):** Integración con el modelo de IA Gemini para generar diagnósticos, respuestas tácticas, auditorías de sistema y misiones procedimentales.
* **Síntesis de Voz Integrada:** Salida por voz en tiempo real con `SpeechSynthesizer`.
* **Cartografía Procedimental:** Renderizado de mapas de territorio con zonas de interés, recursos y amenazas.
* **Sistema de Combate y Hackeo:** Enfréntate a entidades entrópicas y robots defensivos mediante combate clásico, pulsos EMP o hackeos de red.

## 🛠️ Tecnologías Utilizadas
* **Lenguaje:** C# / .NET
* **Entorno de Desarrollo:** Visual Studio Community
* **Librerías principales:**
  * `System.Speech` para síntesis de voz.
  * `System.Net.Http` y `System.Text.Json` para comunicación con la API de IA.
  * `System.Media` para reproducción de efectos de sonido.

## ⚙️ Instrucciones de Ejecución
1. **Clonar o descargar** este repositorio en tu equipo local.
2. Abrir la solución (`mision1 nexus.slnx` o `.sln`) en **Visual Studio Community**.
3. **Configurar la API Key:**
   * Abre el archivo `Program.cs`.
   * En la clase `ServicioIA`, asigna tu clave de la API de Gemini a la variable `apiKey`.
4. Compilar y ejecutar la aplicación presionando `F5` o el botón **Iniciar**.
