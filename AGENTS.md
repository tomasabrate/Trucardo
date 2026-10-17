# Directrices y Contexto del Proyecto - Trucardo

## 1. Contexto General del Proyecto
- **Motor / Plataforma:** Unity 3D (C#) con multijugador online usando **Netcode for GameObjects (NGO)**.
- **Concepto:** Videojuego de **Truco Argentino con poderes y habilidades especiales** (ej. mate, mano de dios, efectos sobre cartas/rondas, etc.).
- **Ambientación y Temática:** Argentina antigua / tradicional (campo, pulpería, cultura gauchesca con gauchos, ponchos, sombreros, mates y barricas).
- **Estilo Artístico:** 3D Low Poly.
- **Puntos Clave de Arquitectura:**
  - `GameModel` y `GameServer`: Lógica autoritativa en servidor de reglas clásicas de truco, turnos y sincronización de estado.
  - `Assets/Poderes`: Sistema modular de poderes (`PowerSystem`, `PowerData`, `PowerEffect`, `PowerCatalog`, `PowerVisual`).
  - `PlayerController` / `HandVisualizer` / `CardVisual`: Input en primera persona, representación 3D en abanico y animaciones de cartas hacia la mesa.

---

## 2. Permisos y Alcance de Trabajo

### Dentro del Repositorio (Modo Turbo)
- El agente cuenta con modo ágil/turbo para ejecutar la mayoría de comandos de terminal, leer, crear y editar archivos **dentro del repositorio actual** sin pedir confirmaciones innecesarias en consola.

### Fuera del Repositorio (Autorización Previa Obligatoria)
- **NUNCA accedas a archivos o directorios fuera del repositorio de forma automática:** Si para alguna tarea se requiere leer, inspeccionar, modificar o ejecutar acciones que afecten archivos fuera de este proyecto (rutas del sistema, perfiles de usuario, otros repositorios, etc.):
  1. Detente antes de ejecutar la acción.
  2. Explícale al usuario qué archivo o ruta necesitas acceder, qué vas a hacer y por qué es necesario.
  3. Espera la confirmación o autorización explícita del usuario en el chat antes de continuar.

---

## 3. Reglas Estrictas de Git

### Git Commit
- **NUNCA hagas `git commit` de manera automática o proactiva:** No realices commits por iniciativa propia tras modificar código, corregir errores o completar una tarea.
- **SOLO realiza commits cuando el usuario te lo pida explícitamente:** Únicamente debes ejecutar `git commit` cuando en el mensaje o solicitud del usuario se indique claramente (ejemplos: *"hacé commit"*, *"commiteá los cambios"*, *"hacé un commit que diga..."*).
- **Ejecución directa:** Cuando el usuario te pida expresamente hacer un commit, ejecútalo directamente sin solicitar confirmación adicional por consola.
- **Formato del Mensaje de Commit (Estricto y en Español):**
  - **Idioma:** Todo el mensaje de commit debe redactarse íntegramente en **español**.
  - **Estructura obligatoria:**
    ```text
    <tipo>: <Título que abarque lo que se hizo o lo más importante>

    - <Punto breve con cambio realizado>
    - <Punto breve con cambio realizado>
    ```
  - **Nomenclatura de prefijos (`<tipo>`):**
    - `feat:` Nuevas características, mecánicas o poderes añadidos.
    - `fix:` Corrección de errores, bugs o fallas de lógica/sincronización.
    - `change:` Modificaciones o ajustes sobre lógica, balance o comportamiento existente.
    - `doc:` Modificación o adición de documentación, guías o comentarios explicativos.
    - `core:` Cambios estructurales, de arquitectura, networking (NGO) o infraestructura base.
  - **Título:** Resumir en una línea qué se hizo o el objetivo primordial del commit.
  - **Cuerpo:** Separado del título por una línea en blanco, desglosar en puntos breves (`- `) y concisos los cambios concretos.

### Git Push
- **SIEMPRE solicita confirmación antes de hacer `git push`:** NUNCA ejecutes `git push` al repositorio remoto sin haberle preguntado antes al usuario y haber recibido su confirmación explícita.

---

## 4. Documentación Obligatoria de Poderes
- **Fuente de Verdad Canónica:** Si cualquier agente necesita conocer el funcionamiento, reglas técnicas, momentos de activación o diseño de los poderes, **DEBE LEER OBLIGATORIAMENTE** el archivo:
  [`Assets/Poderes/DOCUMENTACION_PODERES.md`](Assets/Poderes/DOCUMENTACION_PODERES.md)
- **Actualización Continua Obligatoria:** Cada vez que el usuario solicite un cambio o ajuste sobre cualquier poder (mecánica, balance, interacción, UI o reglas), el agente **TIENE LA OBLIGACIÓN ESTRICTA de actualizar inmediatamente** dicho archivo de documentación (`Assets/Poderes/DOCUMENTACION_PODERES.md`) y los ScriptableObjects pertinentes para evitar pérdida de contexto y garantizar que cualquier otro agente o desarrollador conozca con exactitud el comportamiento vigente de cada poder.

