# Implementación y validación — 8 de octubre de 2026

Sin stage, commit, push, stash, reset ni cambios a EnvidoRules.
Se preservaron la modificación previa de `.gitattributes` y los binarios/modelo previamente presentes.

## A/B. Archivos

Modificados por esta implementación:
- `Assets/Core/Scripts/Bots/BotBrain.cs`: barrera exclusiva de Falta.
- `Assets/Core/Scripts/GameServer.cs`: puntos de emisión posteriores al procesamiento y ciclo de vida del bot.

Creados en `Assets/Core/Scripts/BotDialogue/`, cada `.cs` con su `.meta`:
- `BotDialogueContext.cs`: contexto inmutable, evento y enums permitidos.
- `BotDialogueSettings.cs`: configuración serializable en GameServer.
- `BotDialogueController.cs`: frecuencia, cancelación, relevancia e historial.
- `IBotDialogueModel.cs`, `LlamaCppDialogueService.cs`: generación HTTP compartida.
- `IBotDialogueSink.cs`, `ConsoleBotDialogueSink.cs`: salida reemplazable.
- `DialoguePrompt.cs`, `DialogueText.cs`, `DialogueHttp.cs`: prompt, filtro y transporte.
- `LocalLlmProcessManager.cs`, `OwnedProcessJob.cs`: proceso y limpieza Windows.
- `BotDialogueRuntime.cs`: bootstrap/lifetime único.
- `GameServer.BotDialogue.cs`: adaptación de gameplay a contexto filtrado.
- `Editor/BotDialogueValidation.cs`: harness optativo, fuera de builds.
- Metadatos de las carpetas `BotDialogue` y `Editor`.

Documentos nuevos: `Docs/LOCAL_LLM.md` (30 líneas) y este informe.
Unity también generó los `.meta` faltantes de StreamingAssets; inventario exacto al final.
Los logs y auxiliares de prueba están en `Logs/DialogueValidation/`, ignorado por Git.
Se restauraron 37 fuentes/materiales ensuciados por Unity que estaban limpios al inicio.
SampleScene, PlayerController, poderes, reglas, TableDefinition y NetworkUI no tienen cambios.

## C. Tuning exacto

Se mantiene íntegra la utilidad previa, sus valores de puntos y la evaluación de respuestas.
Solo al considerar CANTAR Falta se requiere `EnvidoWinProbability >= umbral`.
Se usa la probabilidad estimada cruda, antes de los ajustes de percepción de dificultad;
así Normal/Easy no quedan imposibilitados de superar el umbral por su DecisionQuality.
Si no alcanza, la utilidad de esa opción es menos infinito; el ruido de decisión no la habilita.

```text
exposición = clamp((puntosAceptadosFalta - 2) / 13, 0, 1)
umbral = 0.62 + 0.12 * (1 - RiskTolerance) + 0.26 * exposición
si abre Envido desde Ninguno: +0.04 * exposición
si rival >= bot y rival + puntosAceptadosFalta >= 30: +0.015
umbral = clamp(umbral, 0.62, 0.999)
```

No se agregaron tiradas aleatorias. Una Falta de 1–2 puntos tiene exposición cero.
Escalar un Envido ya iniciado evita el suplemento de apertura. Interrumpir Truco sigue siendo apertura.
La puntuación reglamentaria donde Falta aceptada vale menos que el rechazo previo sigue intacta.

400 manos idénticas por perfil/score, semilla 887; evaluación Normal con 512 muestras.
La tabla cuenta elecciones por utilidad, sin ruido final, sin Truco competidor y sin partidas completas.
“Antes” reutiliza la utilidad matemática anterior sin la barrera; “después” incluye la barrera.
Score siempre bot–rival; cada celda muestra antes→después y el umbral.

| Perfil | 0–0 | 15–12 | 29–20 | 20–29 |
|---|---|---|---|---|
| Balanced | 141→10; .995 | 196→14; .980 | 400→142; .680 | 0→0; .695 |
| Conservative | 106→10; .999 | 170→10; .999 | 400→130; .716 | 0→0; .731 |
| Aggressive | 154→18; .971 | 217→22; .956 | 400→146; .656 | 0→0; .671 |
| Risky | 182→34; .941 | 254→39; .926 | 400→150; .626 | 0→0; .641 |
| Bluffer | 146→13; .983 | 199→19; .968 | 400→146; .668 | 0→0; .683 |

Early/mid: Falta cae a 2.5–9.75% según perfil; Balanced queda en 2.5–3.5%.
Si el rival está en 29, la utilidad existente prefiere otras opciones: la barrera permite Falta fuerte,
pero no fuerza su elección. También se probó Falta habilitada con 33 puntos al interrumpir Truco
y en todos los estados legales de escalada cerca del cierre.

Límite relevante: la utilidad existente favorece Real sobre Envido simple en muchas manos.
Por ejemplo, Balanced 0–0: Envido 12, Real 162, Falta 10 sobre 400.
No se retocó esa relación porque el alcance pedido fue exclusivamente Falta;
en Aggressive/Risky, Envido simple todavía puede ser menos frecuente que Falta.

## D/E/F. Arquitectura, privacidad y frecuencia

GameServer procesa → evento/contexto filtrado → controlador → IBotDialogueModel → llama.cpp
→ filtro de texto → IBotDialogueSink → ConsoleBotDialogueSink → Debug.Log.
BotBrain sigue decidiendo gameplay; GameServer y comandos siguen aplicándolo.
No existe ruta del texto hacia comandos, cartas, puntos, poderes ni GameModel.
BotDialogueController no usa Debug.Log; `BotDialogueRuntime.SetSink()` permite incorporar HUD.

El contexto es una lista permitida de enums e ints: personalidad, score, fase, fuerza derivada y confianza.
No contiene BotObservation, GameModel, cartas, índices, números, palos ni referencias autoritativas.
La reducción local usa únicamente cartas propias del bot; nunca consulta cartas ocultas del rival.
Bluffer ni siquiera envía las categorías de fuerza/confianza al prompt.
El filtro descarta razonamiento, formato largo, referencias a palos/cartas, cifras y contradicciones evidentes.
El filtro no garantiza detectar toda alucinación semántica; el texto nunca ejecuta acciones.

Eventos: comienzo de mano; Envido/Real/Falta/Truco/Retruco/Vale Cuatro propios o rivales;
aceptación/rechazo del bot; victoria/derrota de mano o partida.
El resultado terminal tiene prioridad sobre la respuesta que lo produjo.
Una carta común no genera diálogo y conserva la relevancia del último canto.
Otro canto, respuesta, nueva mano, fin o salida de sesión cancela lo anterior.

Probabilidad = base × personalidad × importancia, limitada a 1.
Personalidad: Conservative .5, Balanced .85, otras 1.15.
Importancia: inicio .45, fin de partida 2, Falta/Vale Cuatro 1.5, otros 1.
Cooldown de intentos = 12 s / factor de personalidad, incluyendo intentos descartados.
Historial: últimas 3 frases publicadas; comparación sin puntuación ni diferencia de mayúsculas.
Una generación por bot y una global; si está ocupado se omite el evento, sin cola.
Eventos terminales también pueden omitirse por cooldown, probabilidad o una cancelación todavía en curso.

## G/H. Proceso y configuración

Se crea automáticamente al registrar el primer bot lógico en la autoridad servidor/host.
No requiere referencias nuevas ni cambios de escena. El runtime sobrevive cambios de escena;
los controladores pertenecen a cada sesión y se eliminan con ella.
Configuración en GameServer → Local Bot Dialogue, antes de entrar a Play Mode.

Defaults: enabled=true, puerto 8080, modelo Qwen_Qwen3.5-0.8B-Q4_1.gguf,
startup 60 s, request 8 s, TTL 8 s, contexto 2048, 48 tokens, temperatura .9,
cooldown 12 s, probabilidad .45, threads 2, logDebug=false.

```text
--model "<StreamingAssets>/LLM/Models/Qwen_Qwen3.5-0.8B-Q4_1.gguf"
--alias "Qwen_Qwen3.5-0.8B-Q4_1.gguf" --host 127.0.0.1 --port 8080
--ctx-size 2048 --reasoning off --reasoning-budget 0 --no-webui --offline
--parallel 1 --threads 2 --threads-batch 2 --poll 0 --log-disable
```

Flags comprobados con el `--help` del ejecutable presente, build b11514-de7fa0a3c.
También se envía `enable_thinking=false` y presupuesto de razonamiento cero en cada request.
JSON serializado con JsonUtility y bytes explícitos UTF-8; UnityWebRequest con timeout y Abort.
Sin descargas, PATH, Python, Ollama ni API externa; HTTP solo loopback y sin redirecciones.
Se eliminan variables heredadas LLAMA_* al lanzar el proceso.
Readiness comprueba /health, /props y /v1/models; el modelo debe coincidir.
Un servidor compatible existente se reutiliza sin adquirir su propiedad ni matarlo.
Puerto incompatible/ocupado: se deshabilita diálogo, sin tocar el proceso ajeno.
Se verifica la firma GGUF/MZ para detectar archivos ausentes o punteros LFS.
Fallo de inicio: warning una vez; gameplay continúa. Tres fallos HTTP consecutivos deshabilitan el servicio.

Start corre fuera del hilo principal. Las esperas HTTP/startup son async con timers, sin Update/polling por frame.
Sin ventana visible. Solo se retiene y cierra el Process propio, sin WaitForExit bloqueante.
Un Job Object Windows con KILL_ON_JOB_CLOSE incluye descendientes y cubre cierre abrupto del editor/player.
Stop Play Mode, salida, destrucción y recarga de assemblies cancelan requests y cierran el proceso propio.

## I. Pruebas ejecutadas

Unity 6000.6.0f1, Windows, harness batch: **ALL TESTS PASSED**.
La ejecución inicial en sandbox no encontró licencia; fuera del sandbox pudo usar la licencia local.
Compilación C# runtime/editor e IL postprocessing de Unity sin errores.
Log: `Logs/DialogueValidation/unity-final.log`; resultados: `Logs/DialogueValidation/results.txt`.

- 8.000 evaluaciones comparativas de Falta: 400 × 5 perfiles × 4 marcadores.
- Escaladas legales e interrupción de Truco; Falta aceptada 1 vs rechazo previo 7.
- Contexto limitado a enums/ints; fuerza omitida para Bluffer; JSON vacío/inválido y UTF-8.
- Filtro de cartas, razonamiento y acción contradictoria; repetición; cambio de sink.
- Una solicitud por bot/global, TTL, respuesta tardía y bot destruido.
- Fin real de GameModel a 30 con request pendiente, a través del adaptador de GameServer:
  texto anterior descartado y score sin alteración por diálogo (autoridad simulada en el harness).
- Servidor HTTP de prueba: timeout de request/startup, cancelación durante startup, caída,
  JSON inválido, puerto incompatible y preservación de servidor ajeno.
- Runtime/modelo ausentes y ejecutable sin DLL: inicio falla limpiamente.
- Servidor y GGUF reales: readiness, reutilización, POST UTF-8, parseo y cierre sin huérfanos.
- Respuesta real final: `No me lo quiebro.`; sin reasoning; 341 tokens de prompt, 8 de salida.
  Medición de esa solicitud: ~4.26 s de inferencia total, con 2 threads, sin garantizar tiempos en otras PCs.
- Play Mode real: arranque automático, sustitución de escena mediante carga/descarga,
  descarte de solicitud del bot anterior y una única instancia persistente.
- Stop Play Mode real: proceso propio cerrado; comprobación posterior sin llama-server.
- `git diff --check` limpio y estado final revisado; EnvidoRules sin cambios.

Los logs headless incluyen mensajes de TMP_EditorResourceManager al actualizar recursos/Console.
No se modificó código TMP; sus assets ensuciados se restauraron junto con los materiales.

## J/K. No ejecutado, límites y pasos manuales

No se ejecutó un build Windows standalone/IL2CPP, una partida interactiva completa, multijugador
con máquinas separadas, ni un crash forzado del editor. La protección de Job Object sí se ejercitó al cerrar.
Se probó sustitución/unload de escena; no se guardó ni recargó SampleScene.
No se ensayó recompilar scripts en medio de Play Mode ni todas las combinaciones de Enter Play Mode Options.
El test de Play usó las opciones existentes del proyecto, con Reload Domain y Reload Scene desactivados.
La ausencia de archivos se probó con rutas de prueba, preservando los assets reales.

Solo Windows dispone del runtime suministrado. Plataformas restantes omiten diálogo con un aviso.
Multijugador: solo la autoridad genera y ve Console; no hay RPC de texto ni HUD en este MVP.
Qwen 0.8B puede producir frases poco naturales aun cuando cumplen longitud/formato.
Los ejemplos del prompt mejoraron la aceptación, pero queda pendiente juzgar el tono jugando.
Los comentarios muy tardíos, truncados, repetidos o fuera del filtro se omiten, sin reintentos por evento.
El primer evento durante carga del modelo puede omitirse. El proceso permanece hasta salir de Play/app.

Para probar: abrir Unity, confirmar modelo/runtime descargados, jugar contra un bot lógico y buscar
`[Bot Dialogue]` en Console. No hace falta iniciar ningún servidor manualmente.
Verificar manualmente ritmo/tono, ajustar cooldown/probabilidad si hace falta y probar un build Windows.
El harness se puede repetir desde el menú Trucardo/Validation o batch usando
`-executeMethod Trucardo.Core.BotDialogue.Editor.BotDialogueValidation.Run` (sin `-quit`).

## L. Estado Git final

```text
 M .gitattributes                          (cambio previo, preservado)
 M Assets/Core/Scripts/Bots/BotBrain.cs
 M Assets/Core/Scripts/GameServer.cs
?? Assets/Core/Scripts/BotDialogue.meta
?? Assets/Core/Scripts/BotDialogue/
?? Assets/StreamingAssets.meta
?? Assets/StreamingAssets/                 (binarios/modelo previos + nuevos metadatos)
?? Docs/
```

## M. Comandos para ejecutar manualmente

Los siguientes comandos NO fueron ejecutados. El segundo `git add` incorpora tus assets y regla LFS previos.

```powershell
git diff --check
git add -- Assets/Core/Scripts/Bots/BotBrain.cs Assets/Core/Scripts/GameServer.cs Assets/Core/Scripts/BotDialogue.meta Assets/Core/Scripts/BotDialogue Docs/LOCAL_LLM.md Docs/LOCAL_LLM_IMPLEMENTATION.md
git add -- .gitattributes Assets/StreamingAssets.meta Assets/StreamingAssets
git diff --cached --stat
git commit -m "feat: tune Falta Envido and add local bot dialogue"
git push origin feature/bot-ai
```

## Inventario exacto adicional de metadatos generados por Unity

- `Assets/StreamingAssets.meta`
- `Assets/StreamingAssets/LLM.meta`
- `Assets/StreamingAssets/LLM/Models.meta`
- `Assets/StreamingAssets/LLM/Models/Qwen_Qwen3.5-0.8B-Q4_1.gguf.meta`
- `Assets/StreamingAssets/LLM/Runtime.meta`
- `Assets/StreamingAssets/LLM/Runtime/ggml.dll.meta`
- `Assets/StreamingAssets/LLM/Runtime/ggml-base.dll.meta`
- `Assets/StreamingAssets/LLM/Runtime/ggml-cpu-alderlake.dll.meta`
- `Assets/StreamingAssets/LLM/Runtime/ggml-cpu-cannonlake.dll.meta`
- `Assets/StreamingAssets/LLM/Runtime/ggml-cpu-cascadelake.dll.meta`
- `Assets/StreamingAssets/LLM/Runtime/ggml-cpu-cooperlake.dll.meta`
- `Assets/StreamingAssets/LLM/Runtime/ggml-cpu-haswell.dll.meta`
- `Assets/StreamingAssets/LLM/Runtime/ggml-cpu-icelake.dll.meta`
- `Assets/StreamingAssets/LLM/Runtime/ggml-cpu-ivybridge.dll.meta`
- `Assets/StreamingAssets/LLM/Runtime/ggml-cpu-piledriver.dll.meta`
- `Assets/StreamingAssets/LLM/Runtime/ggml-cpu-sandybridge.dll.meta`
- `Assets/StreamingAssets/LLM/Runtime/ggml-cpu-sapphirerapids.dll.meta`
- `Assets/StreamingAssets/LLM/Runtime/ggml-cpu-skylakex.dll.meta`
- `Assets/StreamingAssets/LLM/Runtime/ggml-cpu-sse42.dll.meta`
- `Assets/StreamingAssets/LLM/Runtime/ggml-cpu-x64.dll.meta`
- `Assets/StreamingAssets/LLM/Runtime/ggml-cpu-zen4.dll.meta`
- `Assets/StreamingAssets/LLM/Runtime/ggml-rpc.dll.meta`
- `Assets/StreamingAssets/LLM/Runtime/libomp.dll.meta`
- `Assets/StreamingAssets/LLM/Runtime/llama.dll.meta`
- `Assets/StreamingAssets/LLM/Runtime/llama-batched-bench-impl.dll.meta`
- `Assets/StreamingAssets/LLM/Runtime/llama-bench-impl.dll.meta`
- `Assets/StreamingAssets/LLM/Runtime/llama-cli-impl.dll.meta`
- `Assets/StreamingAssets/LLM/Runtime/llama-common.dll.meta`
- `Assets/StreamingAssets/LLM/Runtime/llama-completion-impl.dll.meta`
- `Assets/StreamingAssets/LLM/Runtime/llama-fit-params-impl.dll.meta`
- `Assets/StreamingAssets/LLM/Runtime/llama-perplexity-impl.dll.meta`
- `Assets/StreamingAssets/LLM/Runtime/llama-quantize-impl.dll.meta`
- `Assets/StreamingAssets/LLM/Runtime/llama-server.exe.meta`
- `Assets/StreamingAssets/LLM/Runtime/llama-server-impl.dll.meta`
- `Assets/StreamingAssets/LLM/Runtime/mtmd.dll.meta`
