# Trucardo 3D - Proyecto Multijugador

Bienvenido a **Trucardo 3D**, una implementación moderna del clásico juego de cartas Truco Argentino, desarrollado en Unity 3D con capacidades multijugador online utilizando **Netcode for GameObjects (NGO)**.

## Estado Actual del Proyecto (MVP Completado)
Actualmente hemos completado la **Fase 1 (MVP)**. El juego cuenta con las mecánicas básicas y un entorno en 3D funcional:
- **Networking:** Sistema Host/Cliente robusto. Los jugadores se sincronizan perfectamente.
- **Entorno 3D:** Un salón con una mesa y sillas. Los jugadores (representados por avatares de primitivas 3D con cabeza movible y sombreros) pueden caminar e interactuar.
- **Interacción:** El jugador camina por la escena, se acerca a la mesa y presiona `[E]` para sentarse. Cuando ambos están sentados, el Servidor reparte automáticamente.
- **Lógica de Cartas:** Las cartas se generan dinámicamente con un estilo limpio (blanco y números claros) usando *ScriptableObjects* y `CardBuilderEditor` para escalar sus propiedades.
- **Coordenadas y Posicionamiento:** La mano de cartas y las cartas jugadas se posicionan de manera absoluta relativa al centro de la mesa. Las cartas se organizan uniformemente y se rotan con precisión espacial para la cámara en primera persona.
- **Cámara y Control:** Sistema *First Person*. El jugador mueve la cabeza y puede interactuar haciendo click sobre sus cartas.

## Arquitectura de Código
- **GameModel & GameServer:** Toda la lógica de turnos, puntos y reglas puras (Canto de Envido, Truco) está aislada en memoria sin depender de GameObjects para asegurar validación Anti-Cheat.
- **PlayerController:** Maneja el input del jugador local, la rotación de la cámara (Pitch sincronizado), y el envío de acciones (RPCs) al servidor.
- **HandVisualizer:** Componente altamente parametrizable que distribuye las cartas en forma de abanico frente al jugador, utilizando coordenadas absolutas editables en tiempo real desde el Inspector.
- **CardVisual:** Utiliza interpolación y matemáticas para animar las cartas fluidamente desde la mano hacia la mesa.

Para instrucciones sobre cómo compilar, probar o modificar el juego usando IA, consulta el archivo [GUIA_DE_DESARROLLO.md](GUIA_DE_DESARROLLO.md).
