# Documento de Diseño del Juego: NEXUS - Simulador de Exploración Espacial

## 1. Visión del juego
NEXUS es un simulador de exploración espacial táctico y de gestión de recursos. El jugador asume el rol de un Comandante que lidera expediciones en sectores desconocidos de la galaxia para investigar anomalías, resolver misiones y asegurar la supervivencia de su tripulación.

La motivación principal del jugador radica en la toma de decisiones estratégicas: administrar un suministro limitado de energía mientras explora un mapa estelar, equipa e interactúa con sus exploradores, gestiona un inventario de suministros y completa misiones cada vez más complejas para desbloquear nuevos sectores.

---

## 2. Ciclo principal de juego

### Secuencia de una partida
1. **Inicio:** El comandante selecciona una partida guardada o inicia una nueva misión espacial desde el Menú Principal.
2. **Navegación / Exploración:** Se desplaza por las casillas del mapa espacial para revelar zonas no exploradas. Cada desplazamiento consume una cantidad fija de energía.
3. **Detección de Evento o Misión:** Al ingresar en un punto de interés (anomalía, estación abandonada, planeta), se activa una misión.
4. **Asignación y Resolución:** El jugador selecciona a los exploradores adecuados de su tripulación o utiliza ítems del inventario para resolver el reto.
5. **Obtención de Recompensas:** Un resultado exitoso otorga recursos (energía, ítems de inventario, mejoras).
6. **Gestión y Progresión:** El comandante administra su inventario, cura o equipa a sus exploradores y decide si continuar explorando o regresar a base.
7. **Fin de Partida / Misión:** La partida termina con éxito al completar los objetivos principales del sector, o en derrota si la energía total de la nave se agota a 0.

### Diagrama del Ciclo de Juego

```

[Inicio de Partida]
│
▼
[Exploración de Mapa (Gasto de Energía)] ◄──────────┐
│                                           │
▼                                           │
[Encuentro en Punto de Interés / Misión]            │
│                                           │
▼                                           │
[Asignación de Explorador e Inventario]             │
│                                           │
▼                                           │
[Resolución de Misión]                              │
├── Éxito ──► [Obtención de Recompensas / Ítems] ───┤
└── Fracaso ──► [Pérdida de Energía / Recursos] ────┤
│
[¿Energía == 0?] ──Sí──► [Fin de Partida (Derrota)] │
│                                           │
No ─────────────────────────────────────────┘

```

---

## 3. Mecánicas y reglas

### Exploradores
* Cada explorador posee estadísticas clave (Ejemplo: Resistencia, Ciencia, Pilotaje, Estado de Salud).
* Se pueden asignar a misiones específicas para aumentar la probabilidad de éxito según sus habilidades.
* Pueden sufrir desgaste o requerir ítems del inventario para mantener su eficiencia.

### Mapa Espacial
* Estructurado por una cuadrícula/grid de sectores.
* Cada sector puede estar Oculto, Revelado o Visitado.
* Revelar o moverse a una casilla contigua cuesta 10 unidades de energía.

### Energía
* Recurso crítico del juego.
* Se consume con el movimiento, exploración de sectores y fallos en misiones.
* Se puede recargar mediante recolección de celdas de combustible o resolución exitosa de ciertos eventos.

### Misiones
* Eventos interactivos en el mapa con objetivos específicos (ej. reparar antena, recolectar minerales, analizar anomalía).
* Requieren cumplir condiciones mínimas de explorador o herramientas para completarse.

### Inventario
* Sistema de almacenamiento de cuadrícula con espacio limitado.
* Permite guardar celdas de energía, herramientas de reparación, componentes y artefactos alienígenas.
* Los objetos se pueden consumir durante la exploración o equipar a los exploradores.

---

## 4. Diseño de pantallas

> *Nota: Los bocetos gráficos (wireframes) detallados se encuentran guardados en la carpeta `docs/evidencias/`.*

### Pantalla 1: Menú Principal
* **Elementos visibles:** Título del juego ("NEXUS"), botones de "Nueva Misión", "Cargar Partida", "Opciones" y "Salir".
* **Navegación:** Al presionar "Nueva Misión" o "Cargar Partida", se dirige directamente a la Pantalla de Exploración/Mapa.

### Pantalla 2: Exploración / Mapa Espacial
* **Elementos visibles:** 
  * Cuadrícula del mapa estelar mostrando sectores explorados y no explorados.
  * Barra superior de estado (Energía restante, Nombre del Sector, Coordenadas).
  * Botón lateral de acceso al "Inventario / Exploradores".
* **Navegación:** Seleccionar una casilla adyacente mueve la nave. Si la casilla contiene una misión, transiciona automáticamente a la Pantalla de Misiones.

### Pantalla 3: Misiones
* **Elementos visibles:**
  * Ventana central con la descripción del evento espacial e ilustración del entorno.
  * Requisitos de la misión y lista desplegable para seleccionar al explorador asignado.
  * Botones de acción: "Iniciar Misión", "Usar Ítem", "Retirarse".
* **Navegación:** Al completar o cancelar la misión, regresa al Mapa Espacial actualizando la energía y el inventario.

### Pantalla 4: Exploradores e Inventario
* **Elementos visibles:**
  * Panel izquierdo: Tarjetas de los exploradores con su salud, rol y atributos.
  * Panel derecho: Cuadrícula de casilleros de inventario con los ítems almacenados y botón "Usar/Equipar".
  * Botón de cierre o "Volver al Mapa".
* **Navegación:** Accesible desde el Mapa Espacial en cualquier momento fuera de combate/misión activa.

---

## 5. Experiencia de usuario (UX)

* **Legibilidad:** Uso de tipografías claras con alto contraste (texto blanco/verde neón sobre fondos oscuros espacial tipo azul noche/negro).
* **Jerarquía visual:** El medidor de energía y alertas críticas se ubican en la esquina superior izquierda con colores vivos (Rojo para energía baja, Verde para estado óptimo).
* **Consistencia:** Todos los botones de acción principal mantendrán la misma forma, estilo gráfico y sonido de confirmación.
* **Facilidad de navegación:** Se incluye un botón de "Volver" accesible en todo momento para evitar que el jugador quede atrapado en submenús.
* **Retroalimentación:** Animaciones sencillas en la interfaz, barras de carga y mensajes emergentes (ej. "+20 Energía", "Misión Completada") tras cada acción realizada.

---

## 6. Alcance y restricciones

### Funcionalidades Prioritarias (Must Have)
1. Sistema de navegación por casillas en Windows Forms.
2. Gestión del recurso de energía y condición de derrota/victoria.
3. Pantalla de gestión de inventario y exploradores.
4. Conexión y almacenamiento de partidas en SQL Server.

### Funcionalidades Opcionales (Nice to Have)
1. Reproducción de efectos de sonido y música de fondo espacial (`musica.wav`).
2. Animaciones avanzadas en la transición entre casillas.
3. Generación aleatoria de eventos en el mapa.

### Restricciones Técnicas
1. El juego será exclusivamente una aplicación de escritorio 2D desarrollada en C# con Windows Forms.
2. No se utilizarán motores de juego 3D (como Unity o Unreal Engine).
3. Todas las mecánicas deben mantener coherencia estricta con las clases creadas en la versión H3 del proyecto.

```
