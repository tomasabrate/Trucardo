# Guía de Desarrollo - Trucardo 3D

Este documento te explicará paso a paso cómo preparar el proyecto para generar una versión jugable (Build), cómo probarlo, y cómo utilizar inteligencia artificial para modificar el código fácilmente.

---

## 1. Preparar y Generar la Build (Ejecutable)

Para poder jugar con un amigo o probar la conexión Cliente/Servidor en tu misma PC, necesitas crear un archivo ejecutable (`.exe`).

### Pasos para hacer un Build en Unity:
1. Asegúrate de haber guardado la escena actual (`Ctrl + S`).
2. En la barra superior de Unity, ve a **File -> Build Settings...**
3. Haz clic en el botón **"Add Open Scenes"** para agregar tu escena actual a la lista de escenas a buildear. Asegúrate de que tenga un tick `[x]` a la derecha.
4. En la pestaña de *Platform*, asegúrate de que **Windows, Mac, Linux** esté seleccionado.
5. Haz clic en **Build And Run**.
6. Unity te pedirá que elijas una carpeta para guardar el juego. Crea una carpeta llamada `Builds` dentro de tu proyecto (pero fuera de `Assets`) y selecciónala.
7. Espera a que termine de compilar. Cuando termine, el juego se abrirá automáticamente en una nueva ventana.

---

## 2. Cómo Probar el Multijugador en una misma PC

Puedes simular una partida completa abriendo el juego dos veces en tu computadora.

1. **Host (Servidor + Jugador 1):** 
   - En el editor de Unity, dale al botón de **Play**.
   - Haz clic en el botón de **Host** que aparece en la pantalla.
2. **Client (Jugador 2):** 
   - Abre la ventana del juego que se abrió cuando hiciste el *Build and Run* (o ve a tu carpeta Builds y abre el `.exe`).
   - Haz clic en el botón **Client**.
3. Ambos personajes aparecerán en la escena. Camina hacia las sillas y presiona **[E]** con cada uno para sentarte.
4. ¡El servidor repartirá las cartas y podrán jugar!

---

## 3. Cómo usar la Inteligencia Artificial para modificar el código

El código de Trucardo 3D fue diseñado para ser altamente modular. Esto significa que puedes pedirle a la IA (como Gemini) que cambie cosas específicas y lo hará sin romper el resto del juego.

### Consejos para pedirle cambios a la IA:
- **Sé específico sobre el rol:** El código está dividido en lo Visual (ej. `HandVisualizer.cs`, `CardVisual.cs`) y la Lógica del Servidor (`GameServer.cs`, `GameModel.cs`). Si quieres cambiar cómo se *ve* algo, dile a la IA: *"Cambia cómo se posicionan visualmente las cartas en la mano"*. Si quieres cambiar una *regla*, dile: *"Cambia la lógica del servidor para que el Truco valga 3 puntos"*.
- **Pídele herramientas en el Editor:** Trucardo tiene un menú personalizado en Unity (pestaña `Trucardo` arriba del todo). Puedes decirle a la IA: *"Créame un botón en el menú de Trucardo para cambiar el color de la mesa a verde"*. La IA escribirá un script de Editor que hará el trabajo duro por ti.
- **Adjunta capturas de pantalla:** Si algo se ve mal en Unity o tienes un error en la consola, sácale una captura de pantalla y envíasela a la IA. La IA puede "leer" la consola, ver el Inspector de Unity, y deducir exactamente por qué un prefab se rompió o por qué una carta está flotando.
- **Pide que lea tus scripts:** Si no recuerdas cómo funciona algo, dile a la IA: *"Por favor lee PlayerController.cs y explícame cómo funciona el movimiento"*.

### Ejemplos de prompts que puedes probar:
- *"La mesa es muy grande, ¿puedes achicarla un poco en el script de NetworkSetupAuto?"*
- *"Quiero que las cartas de oro tengan un brillo amarillo, modifica CardVisual.cs para agregarle ese efecto."*
- *"Agrega un botón UI en la pantalla que diga 'Salir del Juego' y que cierre la aplicación."*
- *"En GameModel.cs, modifica las reglas para que la Flor esté permitida."*
