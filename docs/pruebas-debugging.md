# Registro de Software Testing & Debugging — NEXUS v3

## 1. Pruebas de Entradas Inválidas (UI y Validaciones)
* **Caso 1.1:** Intentar iniciar partida o registrar un jugador con el campo de nombre vacío o solo espacios.
  * **Resultado esperado:** El sistema bloquea el avance y muestra un mensaje de validación sin lanzar excepciones.
  * **Estado:** Aprobado.
* **Caso 1.2:** Selección de nave o modo de juego sin perfil activo seleccionado.
  * **Resultado esperado:** Deshabilitación de botones o alerta preventiva al usuario.
  * **Estado:** Aprobado.

## 2. Pruebas de Guardado y Recuperación de Partidas (Persistencia SQL Server)
* **Caso 2.1:** Iniciar una partida nueva, modificar XP/energía/inventario, guardar en SQL Server, cerrar completamente `NEXUS.exe` y volver a abrirlo para cargar la partida.
  * **Resultado esperado:** Los valores recuperados desde `Partidas`, `InventarioPartida` y `EstadoMisionPartida` coinciden al 100% con el estado guardado.
  * **Estado:** Aprobado.

## 3. Pruebas de Fallos de Conexión con SQL Server
* **Caso 3.1:** Simular servicio SQL Server detenido o nombre de instancia incorrecto en `GestorGuardado.CadenaConexion`.
  * **Defecto detectado / comportamiento:** Al intentar consultar o guardar partidas sin conexión activa, se captura `SqlException` dentro de los bloques `try-catch` de `GestorGuardado`.
  * **Corrección / Validación:** Se verificó el método `GestorGuardado.ProbarConexion(out string mensajeError)` para diagnosticar el estado de la conexión antes de operar y evitar el cierre abrupto de la aplicación.
  * **Estado:** Corregido y verificado.