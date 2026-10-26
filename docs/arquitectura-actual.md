# Arquitectura Actual del Proyecto NEXUS (H3)

## 1. Descripción General
El sistema NEXUS desarrollado en la etapa H3 constituye la base conceptual y funcional del simulador espacial. En su estado actual, el proyecto está enfocado en la lógica de negocio orientada a objetos para la gestión de misiones de exploración espacial, entidades y recursos.



## 2. Clases y Funcionalidades Principales

* **Clase Base / Entidades Principal:** Define la estructura de los elementos del universo (jugador/comandante, exploradores, sectores y misiones) y maneja los atributos básicos como identificación, nombres, contadores de salud o energía.
* **Lógica de Misiones:** Contiene los métodos para validar los requisitos de una misión y determinar la probabilidad o resultado de resolución.
* **Gestión de Recursos:** Controla variables clave en memoria durante la ejecución, como la reserva de energía y los elementos recolectados en el inventario.

---

## 3. Limitaciones Reales de la Versión H3

1. **Persistencia de Datos:** La información actual se almacena únicamente en memoria volátil (RAM) mientras la aplicación está en ejecución. No cuenta con persistencia en base de datos.
2. **Interfaz de Usuario:** La interacción con el sistema se realiza mediante la consola de comandos de C# (.NET) o controles básicos, careciendo de un mapa gráfico interactivo y wireframes visuales.
3. **Escalabilidad y Modularidad:** La lógica de negocio y la presentación de datos se encuentran acopladas en las mismas ejecuciones, lo que dificulta la incorporación de capas independientes como SQL Server o componentes gráficos avanzados de Windows Forms.
