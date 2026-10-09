# Diálogo local

El LLM NO juega: Qwen solo genera frases después de las acciones reales.
Las decisiones siguen en BotBrain y GameServer. El texto sale por Console.
Todo corre local: sin Internet, sin API externa ni clave, sin Ollama y sin Python.
Unity inicia y cierra llama-server automáticamente al jugar contra bots (Windows).

Git LFS se instala una sola vez en cada PC. Comprobar:
```sh
git lfs version
```
Si está instalado, ejecutar en el repositorio:
```sh
git lfs install
git pull
git lfs pull
```

Modelo (~540 MiB):
`Assets/StreamingAssets/LLM/Models/Qwen_Qwen3.5-0.8B-Q4_1.gguf`
Runtime y sus DLL: `Assets/StreamingAssets/LLM/Runtime/llama-server.exe`
Si falta el modelo/runtime, el juego funciona sin diálogo y avisa una vez.

Configuración: GameServer → Local Bot Dialogue (sin referencias manuales).
Defaults: puerto 8080, contexto 2048, 48 tokens, temperatura 0.9, 4 threads.
Startup 60 s; request 8 s; cooldown 3.5 s; probabilidad base 85%.
Persona: Gaucho criollo tradicional en pulpería, voseo y modismos camperos argentinos auténticos.
Contexto rico: lectura de ventaja de marcador, estado de la mano (primera, parda, definitoria), rol de mano y bazas.
Reasoning desactivado; un proceso compartido y una generación global.
Solo el servidor/host genera. En este MVP los clientes remotos no reciben texto.
Para HUD, implementar `IBotDialogueSink` y llamar `BotDialogueRuntime.SetSink`.
El puerto ocupado solo se reutiliza si expone el modelo compatible; nunca se mata un proceso ajeno.
