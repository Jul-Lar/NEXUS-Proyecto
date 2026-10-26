# Análisis Técnico y Evaluación de Alternativas - NEXUS

## 1. Alternativas Evaluadas

### Capa de Presentación (UI)
* **Opción A: Consola de C# (.NET Core)**
  * *Ventajas:* Implementación rápida y libre de sobrecarga gráfica.
  * *Desventajas:* Experiencia de usuario muy limitada, imposibilidad de mostrar un mapa gráfico interactivo o mapas visuales de sectores.
* **Opción B: Windows Forms (Elegida)**
  * *Ventajas:* Integración nativa con .NET, facilidad para gestionar controles visuales (grids, botones, paneles de inventario) y curva de aprendizaje óptima para la evolución desde H3.
  * *Desventajas:* Menor flexibilidad para animaciones 3D avanzadas comparado con motores dedicados.

### Capa de Persistencia / Base de Datos
* **Opción A: Archivos Locales (JSON / XML)**
  * *Ventajas:* No requiere la instalación de un motor de base de datos externo.
  * *Desventajas:* Falta de integridad referencial, riesgo de corrupción de datos en escrituras concurrentes.
* **Opción B: SQL Server (Elegida)**
  * *Ventajas:* Garantía de ACID (Atomisidad, Consistencia, Aislamiento, Durabilidad), relacionalidad mediante claves foráneas y facilidad de consulta con ADO.NET / Dapper.
  * *Desventajas:* Requiere configurar la cadena de conexión e instalar SQL Server Express o equivalente.

---

## 2. Decisiones Recomendadas
Se recomienda proceder con la implementación en **C# Windows Forms + SQL Server**, estructurada estrictamente bajo el patrón de arquitectura en 3 capas. Esta configuración equilibra la factibilidad técnica del tiempo disponible con la ambición de convertir el proyecto H3 en una aplicación gráfica robusta.

---

## 3. Fuentes Técnicas y Verificación de IA
* **Documentación de Microsoft .NET / Windows Forms:** Referencia oficial para la gestión de formularios y eventos en C#.
* **Microsoft SQL Server Docs:** Guías de diseño relacional, transacciones y tipos de datos.
* **Verificación de Información con IA:** Las propuestas de estructuración en capas y modelado de tablas presentadas en este documento han sido asistidas por Inteligencia Artificial y validadas manualmente contra los archivos de código fuente existentes en el repositorio H3 para garantizar la coherencia y viabilidad del desarrollo.
