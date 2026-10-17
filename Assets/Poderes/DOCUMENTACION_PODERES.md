# Registro Maestro y Documentación de Poderes - Trucardo

> [!IMPORTANT]
> **DIRECTIVA PARA AGENTES DE IA (LECTURA Y ACTUALIZACIÓN OBLIGATORIA):**
> Este documento es la **Fuente Canónica de Verdad** sobre los poderes de Trucardo.
> 1. Si cualquier agente necesita conocer las reglas, mecánicas, interacciones o balance de cualquier poder, **DEBE CONSULTAR OBLIGATORIAMENTE ESTE DOCUMENTO**.
> 2. Cada vez que el usuario solicite un cambio o ajuste sobre cualquier poder (mecánica, balance, interacción, UI o reglas), **EL AGENTE TIENE LA OBLIGACIÓN ESTRICTA de actualizar inmediatamente este archivo** y los ScriptableObjects pertinentes para evitar pérdida de contexto.

Este documento contiene el diseño de juego, reglas técnicas, descripción in-game y especificación visual de los **11 Poderes de Trucardo** (8 Comunes y 3 Legendarios), actualizado con el sistema de acumulación, sinergias y línea de tiempo de la UI.

---

## 1. Resumen General del Sistema

- **Adquisición y Acumulación:**
  - Cada jugador (humano y bot/rival) recibe **1 poder aleatorio por ronda** al repartirse las cartas.
  - **Acumulación en mesa:** Si un poder no se utiliza durante la ronda, **se conserva y se acumula** en la mesa para las rondas siguientes, hasta un **máximo de 4 poderes acumulados** por jugador.
  - **Uso múltiple por ronda:** Si un jugador tiene varios poderes acumulados en su mesa, puede activar más de un poder dentro de una misma ronda táctica.
- **Ubicación en la Mesa:**
  - Los poderes se ubican siempre sobre el **sector izquierdo de la mesa** (desde la perspectiva del jugador local, que corresponde a la derecha del rival).
  - Están dispuestos en una cuadrícula compacta de 2x2 ranuras para acomodar hasta 4 poderes sin superponerse ni tapar las cartas.
  - El jugador local puede ver sus propios poderes en primer plano y los poderes del rival al frente.
- **Interacción e Interfaz:**
  - El jugador apunta al objeto 3D con la vista y presiona **Clic izquierdo del mouse** para activar el poder.
  - Al mirar el objeto propio, el indicador de interfaz muestra: `[Clic] <Nombre del Poder>`.
  - Al mirar el objeto del rival, el indicador de interfaz muestra: `[Rival] <Nombre del Poder>` (sin opción de clic, solo informativo).
- **Línea de Tiempo de Poderes en la Ronda (UI Izquierda):**
  - Cada vez que un jugador (local o rival) activa un poder, este se registra inmediatamente en una **línea de tiempo cronológica vertical** ubicada a la izquierda de la pantalla.
  - Posee estética de madera tallada criolla con distintivos visuales claros:
    - **Poder propio:** Placa con ribete dorado y etiqueta `★ VOS ACTIVASTE`.
    - **Poder rival:** Placa con ribete naranja quemado y etiqueta `☠ RIVAL ACTIVÓ`.
  - Si un jugador activa 2 poderes, el rival activa 1 y luego el jugador activa otro, se apilan verticalmente en ese orden exacto de arriba a abajo.
  - La línea de tiempo se **reinicia automáticamente al comenzar cada nueva ronda**.
- **Modo Pruebas / Admin (F1):** Permite forzar qué poder y qué cartas aparecen para realizar pruebas controladas de balance y depuración.

---

## 2. Catálogo de Poderes

### Poderes Comunes

---

#### 1. Facón
- **Identificador (ID):** `facon`
- **Rareza:** Común
- **Objeto Tradicional:** Cuchillo criollo gaucho / Facón tradicional con mango de alpaca/asta y vaina de cuero.
- **Modelo 3D:**
  - *Estado:* Provisorio / Pendiente.
  - *Representación actual:* Prisma afilado metálico grisáceo.
  - *Modelo definitivo planificado:* Modelo 3D de facón criollo apoyado sobre su vaina.
- **Descripción Corta (In-Game):**
  > *"Rompe una carta a elección del rival (haciéndole clic en 3D); se revela brevemente y se le repone otra del mazo."*
- **Descripción Larga y Reglas:**
  - **Momento de activación:** Durante el turno del jugador antes de tirar una carta a la mesa.
  - **Mecánica interactiva en 3D:**
    1. Al activar el Facón en su mesa, se inicia el modo de puntería táctica y el HUD superior muestra: `⚔️ Facón: Selecciona una carta del rival para romper`.
    2. El jugador apunta con la vista y hace **clic izquierdo sobre la carta del rival** que desea destruir.
    3. La carta del rival seleccionada se gira y se revela de frente en 3D durante 1.2 segundos para que ambos jugadores vean qué carta era (ej. *"¡Era el 7 de Espadas!"*).
    4. La carta destruida se descarta. Inmediatamente el servidor toma una carta aleatoria no jugada del mazo y se la entrega al rival para reponer su mano.
    5. **Cancelación:** En cualquier momento antes de hacer clic en la carta rival, el jugador puede presionar **Clic derecho o Escape** para cancelar la acción sin consumir el poder.
  - **Sinergia con Mate:** A simple vista las cartas del rival se ven de espaldas (a ciegas). Si el jugador utilizó previamente el **Mate** y recuerda la posición física de una carta poderosa del rival, puede apuntar y hacer clic en esa carta específica para destruirla.
  - **Lógica para Bots:** Si el bot usa el Facón, busca romper la carta más valiosa que conozca del rival (por Mate previo) o elige una al azar.

---

#### 2. Mate
- **Identificador (ID):** `mate`
- **Rareza:** Común
- **Objeto Tradicional:** Mate de calabaza curada con virola labrada y bombilla de alpaca.
- **Modelo 3D:**
  - *Estado:* Provisorio implementado (`Prefab_Poder_Mate.prefab`).
  - *Representación actual:* Cilindro verde oscuro / madera de escala reducida (8x9 cm).
  - *Modelo definitivo planificado:* Mate realista 3D con yerba mate y bombilla inclinada.
- **Descripción Corta (In-Game):**
  > *"Revela una carta de la mano del rival. La víctima sabe cuál le vieron."*
- **Descripción Larga y Reglas:**
  - **Momento de activación:** En cualquier momento durante el turno del jugador mientras el oponente conserve cartas en mano.
  - **Mecánica:** Selecciona aleatoriamente una carta oculta de la mano del rival y la voltea suavemente erguida de frente hacia el invocador durante 5 segundos.
  - **Notificación a la víctima:** El oponente recibe un aviso visual en pantalla (Toast) informándole con total precisión qué carta suya fue vista: *"¡El rival tomó un mate y espió tu [Número] de [Palo]!"*.
  - **Sinergia con Facón y Guitarra:** La posición física de la carta espiada queda en la memoria del jugador. Los poderes como Facón y Guitarra no revelan nombres ni carteles automáticos en UI: el jugador debe acordarse en qué posición estaba la carta para romperla con el Facón o robarla con la Guitarra.

---

#### 3. Guampas
- **Identificador (ID):** `guampas`
- **Rareza:** Común
- **Objeto Tradicional:** Par de cuernos de buey / guampas criollas pulidas o vaso de guampa labrado.
- **Modelo 3D:**
  - *Estado:* Provisorio / Pendiente.
  - *Representación actual:* Cápsula o toroide arqueado color hueso.
  - *Modelo definitivo planificado:* Par de guampas criollas con detalles en cuero o alpaca.
- **Descripción Corta (In-Game):**
  > *"Robá del mazo; hacé clic en una de tus cartas para cambiarla o clic derecho para descartarla."*
- **Descripción Larga y Reglas:**
  - **Momento de activación:** En el turno del jugador antes de tirar una carta a la mesa.
  - **Mecánica interactiva en 3D (Sin menús 2D invasivos):**
    1. Al activar Guampas en su mesa, el servidor toma una carta aleatoria no jugada del pozo del mazo y se la muestra al invocador en el HUD superior: `📯 Guampas: Robaste [Carta] (Truco: X, Envido: Y). Clic en una de tus cartas para cambiarla | Clic derecho para descartar`.
    2. El jugador puede:
       - **Hacer clic izquierdo en una de sus cartas 3D:** Reemplaza inmediatamente esa carta por la robada (la anterior se descarta).
       - **Hacer clic derecho o presionar Escape:** Conserva su mano intacta y descarta la carta robada del mazo.
    3. Si se reintenta activar o ya había un estado pendiente anterior, el sistema limpia la selección previa de forma segura sin trabar el poder ni bloquear turnos.
  - **Impacto táctico:** Permite buscar una carta clave para mejorar el Envido o conseguir una carta de mayor jerarquía para el Truco sin interrumpir la inmersión en primera persona.

---

#### 4. Lazo
- **Identificador (ID):** `lazo`
- **Rareza:** Común
- **Objeto Tradicional:** Lazo gaucho trenzado de cuero crudo en rollo con presilla y argolla de hierro.
- **Modelo 3D:**
  - *Estado:* Provisorio / Pendiente.
  - *Representación actual:* Toroide / Anillo marrón cuero.
  - *Modelo definitivo planificado:* Rollo de lazo trenzado de cuero vacuno con argolla.
- **Descripción Corta (In-Game):**
  > *"Solo en primera mano: descarta todas tus cartas y roba 3 cartas nuevas."*
- **Descripción Larga y Reglas:**
  - **Momento de activación:** **Únicamente durante la primera mano (baza 0)**, antes de haber jugado ninguna carta y antes de que se cierre el turno de Envido.
  - **Mecánica:** El jugador descarta sus 3 cartas iniciales completas al mazo/descarte y recibe 3 cartas completamente nuevas extraídas del mazo.
  - **Restricción estricta:** Si el jugador ya tiró una carta o la mano avanzó a segunda o tercera baza, el poder no se puede activar.

---

#### 5. Guitarra
- **Identificador (ID):** `guitarra`
- **Rareza:** Común
- **Objeto Tradicional:** Guitarra criolla / criolla de madera con roseta tradicional y cuerdas de nylon.
- **Modelo 3D:**
  - *Estado:* Provisorio / Pendiente.
  - *Representación actual:* Figura acústica de madera estilizada.
  - *Modelo definitivo planificado:* Guitarra criolla clásica apoyada.
- **Descripción Corta (In-Game):**
  > *"Intercambia una de tus cartas por una del rival seleccionándolas directamente con clic."*
- **Descripción Larga y Reglas:**
  - **Momento de activación:** Durante el turno del jugador antes de tirar su carta.
  - **Mecánica interactiva en 3D (Sin menús 2D invasivos):**
    1. Al activar la Guitarra, el HUD superior muestra: `🎸 Guitarra Criolla: 1. Selecciona una carta tuya para cambiar`.
    2. El jugador hace clic en la carta propia que desea entregar. La carta seleccionada **se eleva y se adelanta suavemente** en su mano para confirmar su selección.
    3. El HUD superior pasa al paso 2: `🎸 Guitarra Criolla: 2. Selecciona una carta del rival`.
    4. El jugador apunta a la mano del rival y hace clic en la carta que desea arrebatar. No se muestra información en pantalla de qué carta es; la elección es a ciegas o basada en la memoria de la carta espiada con el **Mate**.
    5. Se realiza el intercambio inmediato en el servidor y las manos se sincronizan en red.
    6. **Cancelación:** En cualquier momento antes de completar el segundo clic, el jugador puede presionar **Clic derecho o Escape** para cancelar; la carta propia regresa a su altura habitual y el poder no se consume.
  - **Lógica para Bots:** Si el bot usa la Guitarra, entrega su carta de menor jerarquía (`TrucoRank`) y roba la mejor carta conocida del jugador (si usó Mate) o una al azar.

---

#### 6. Pava Caliente
- **Identificador (ID):** `pava_caliente`
- **Rareza:** Común
- **Objeto Tradicional:** Pava de campo tiznada de chapa enlozada negra o acero inoxidable para fuego a leña.
- **Modelo 3D:**
  - *Estado:* Provisorio / Pendiente.
  - *Representación actual:* Cilindro cónico con asa / figura de pava metálica.
  - *Modelo definitivo planificado:* Pava criolla enlozada de campo con pico y asa de madera.
- **Descripción Corta (In-Game):**
  > *"El rival tiene solo 5 segundos por turno durante toda la mano, o jugará al azar."*
- **Descripción Larga y Reglas:**
  - **Momento de activación:** En cualquier momento durante la ronda.
  - **Mecánica:** Aplica un temporizador de presión extrema ("la pava está hirviendo"). Desde su activación y durante toda la mano, el rival dispone de únicamente **5 segundos** para responder a cantos o tirar una carta.
  - **Contador en HUD:** Cuando el jugador bajo efecto entra en su turno, se muestra una alerta visual y un contador regresivo de 5 segundos.
  - **Penalización por tiempo agotado:** Si el contador llega a cero, el servidor fuerza automáticamente la acción: si debía tirar carta, tira su primera carta disponible; si había un canto pendiente, lo acepta.

---

#### 7. Mano de Dios
- **Identificador (ID):** `mano_de_dios`
- **Rareza:** Común
- **Objeto Tradicional:** Escultura dorada de una mano abierta apuntando hacia el cielo (alusión a la jugada mítica).
- **Modelo 3D:**
  - *Estado:* Provisorio implementado (`Prefab_Poder_ManoDeDios.prefab`).
  - *Representación actual:* Esfera dorada brillante compacta (8 cm).
  - *Modelo definitivo planificado:* Escultura dorada estilizada de mano o guantelete.
- **Descripción Corta (In-Game):**
  > *"Sos Mano de la ronda (ganás empates) y le cedés el tiro de la baza al rival."*
- **Descripción Larga y Reglas:**
  - **Momento de activación:** Durante la ronda, preferentemente antes del desenlace de empates o cuando el jugador desea forzar al rival a iniciar el tiro de una baza.
  - **Mecánicas combinadas:**
    1. **Adquisición de la Mano de la ronda:** Asigna `ManoPlayerId = invocador`. El jugador que lo activa pasa a ser considerado la "Mano" oficial de la partida para toda la ronda.
    2. **Ventaja de desempate:** En caso de empate en puntos de Envido, el jugador gana automáticamente. En caso de "parda" (empate en la primera baza del Truco), quien es mano define la ventaja para adjudicarse la ronda al ganar la siguiente baza.
    3. **Cesión del tiro de la baza:** Si el poder se activa durante `TurnoJugador` en el turno del invocador (`CurrentTurnId == Invocador`), **transfiere de inmediato el tiro inicial de la baza al rival** (`CurrentTurnId = Rival`), reprograma el temporizador de Pava Caliente si estuviera activo y ordena al bot/rival tirar su carta primero a la mesa sin que el jugador quede esperando.

---

#### 8. Boleadoras
- **Identificador (ID):** `boleadoras`
- **Rareza:** Común
- **Objeto Tradicional:** Boleadoras de tres ramales de tientos de cuero crudo trenzado con bolas de piedra ("tres marías").
- **Modelo 3D:**
  - *Estado:* Provisorio / Pendiente.
  - *Representación actual:* Tres esferas conectadas por cilindros finos.
  - *Modelo definitivo planificado:* Boleadoras gauchas con tientos de cuero trenzados.
- **Descripción Corta (In-Game):**
  > *"Traba al rival: le impide recibir su próximo poder en la siguiente ronda."*
- **Descripción Larga y Reglas:**
  - **Momento de activación:** Durante cualquier momento de la ronda en curso.
  - **Mecánica:** Aplica el estado "Boleado" al oponente. En la próxima ronda repartida, el oponente **no recibirá ningún poder nuevo en su mesa**.

---

### Poderes Legendarios

---

#### 9. Ceibo
- **Identificador (ID):** `ceibo`
- **Rareza:** Legendario
- **Objeto Tradicional:** Ramillete o rama florecida del Ceibo (Flor Nacional Argentina) con pétalos rojo carmesí.
- **Modelo 3D:**
  - *Estado:* Provisorio / Pendiente.
  - *Representación actual:* Flor roja estilizada / prisma pentagonal carmesí.
  - *Modelo definitivo planificado:* Rama tallada con flores rojas de ceibo y hojas verdes.
- **Descripción Corta (In-Game):**
  > *"Cambia el palo de todas tus cartas al mismo palo aleatorio (alto Envido asegurado)."*
- **Descripción Larga y Reglas:**
  - **Momento de activación:** En cualquier momento antes de jugar todas las cartas (ideal antes de cantar Envido).
  - **Mecánica:** Transforma el palo de todas las cartas que el jugador tiene en su mano actual a un mismo palo elegido al azar (todas Espadas, todas Bastos, todas Oros o todas Copas), manteniendo los números originales.
  - **Sinergia con Envido:** Al tener las 3 cartas del mismo palo, el jugador suma automáticamente los 20 puntos base de combinación de palo más la suma de sus dos cartas más altas.

---

#### 10. Luz Mala
- **Identificador (ID):** `luz_mala`
- **Rareza:** Legendario
- **Objeto Tradicional:** Fuego fatuo espectral / candil antiguo de campo que despide un resplandor verdoso fantasmal.
- **Modelo 3D:**
  - *Estado:* Provisorio / Pendiente.
  - *Representación actual:* Esfera translúcida con emisión verde fosforescente.
  - *Modelo definitivo planificado:* Calavera o candil campero con llama verde sobrenatural.
- **Descripción Corta (In-Game):**
  > *"Vuelve blancas y mezcla las cartas del rival: no sabrá cuál es cuál al jugar."*
- **Descripción Larga y Reglas:**
  - **Momento de activación:** Durante el turno del jugador antes de que el rival juegue sus cartas clave.
  - **Mecánica:** Envuelve las cartas del oponente en un halo fantasmal. Las cartas en la mano del rival se vuelven completamente blancas (sus números y figuras desaparecen) y se reorganizan aleatoriamente en sus ranuras.
  - **Efecto de confusión:** El rival debe jugar sus bazas a ciegas por pura intuición durante el resto de la mano. Si es un bot, juega una carta aleatoria.

---

#### 11. Bastón Presidencial
- **Identificador (ID):** `baston_presidencial`
- **Rareza:** Legendario
- **Objeto Tradicional:** Bastón de mando presidencial de madera de urunday con empuñadura y regatón de plata cincelada.
- **Modelo 3D:**
  - *Estado:* Provisorio / Pendiente.
  - *Representación actual:* Bastón cilíndrico fino marrón oscuro con pomo plateado.
  - *Modelo definitivo planificado:* Bastón de mando cincelado con escudo nacional en el pomo.
- **Descripción Corta (In-Game):**
  > *"Obliga al rival a aceptar (decir '¡Quiero!') a tu próximo canto."*
- **Descripción Larga y Reglas:**
  - **Momento de activación:** Antes de efectuar un canto (Envido, Real Envido, Falta Envido, Truco, Retruco o Vale Cuatro).
  - **Mecánica:** Bloquea la opción de "No Quiero" en el rival (tanto humano como bot). El servidor fuerza la respuesta "¡Quiero!", obligándolo a disputar la apuesta.

---

## 3. Matriz Comparativa Rápida

| Poder | Rareza | Objeto Criollo | Modelo 3D Actual | Descripción Corta (HUD) | Sinergia / Detalle Clave |
|---|---|---|---|---|---|
| **Facón** | Común | Cuchillo criollo | Prisma metálico | Rompe una carta del rival (con clic); se revela y se le repone otra del mazo. | Selección 3D. Destruye carta del rival, se revela brevemente y el mazo le entrega una nueva al rival. |
| **Mate** | Común | Mate con bombilla | Cilindro verde/madera (8x9 cm) | Revela una carta del rival (5s). | La víctima sabe cuál carta le vieron. Sinergia de memoria táctica con Facón y Guitarra. |
| **Guampas** | Común | Cuernos de toro | Cápsula arqueada | Robá del mazo; elegí si cambiar una de tus cartas o descartarla. | Interacción 3D directa: clic izquierdo en carta propia para reemplazar o clic derecho para descartar robada. |
| **Lazo** | Común | Rollo de lazo gaucho | Anillo/toroide cuero | (1ra mano) Descarta tus 3 cartas y saca 3 nuevas. | Restringido estrictamente a baza 0. |
| **Guitarra** | Común | Guitarra criolla | Silueta acústica madera | Intercambia una de tus cartas por una del rival seleccionándolas con clic. | Selección 3D directa con elevación de carta propia; a ciegas o recordando la posición del Mate. |
| **Pava Caliente** | Común | Pava tiznada de campo | Cilindro con pico | Rival tiene solo 5s por turno o juega al azar. | Temporizador regresivo visual en HUD con juego forzado. |
| **Mano de Dios** | Común | Mano dorada | Esfera dorada (8 cm) | Sos Mano de la ronda (ganás empates) y le cedés el tiro al rival. | Asigna la condición de Mano (desempates/pardas a favor) y cede el tiro de baza al rival al instante. |
| **Boleadoras** | Común | Boleadoras tres marías | Tres esferas unidas | Niega al rival recibir su poder la próxima ronda. | Neutraliza la siguiente ventaja del oponente. |
| **Ceibo** | Legendario | Flor del Ceibo | Flor roja prisma | Cambia el palo de todas tus cartas al mismo palo. | Garantiza combinaciones monstruosas de Envido (ej. 33). |
| **Luz Mala** | Legendario | Fuego fatuo / Candil | Esfera verde brillante | Vuelve blancas y mezcla las cartas del rival. | Fuerza a jugar a ciegas tanto a humanos como a bots. |
| **Bastón Presidencial** | Legendario | Bastón de mando | Bastón con pomo plata | Obliga al rival a aceptar tu próximo canto. | Impide el "No Quiero", cobro garantizado de puntos. |

---

## 4. Guía de Archivos y Arquitectura en el Proyecto

- **Definición de Datos:** [`Assets/Poderes/Scripts/Core/PowerData.cs`](file:///C:/Users/Tomas%20Abrate/Trucardo/Assets/Poderes/Scripts/Core/PowerData.cs)
- **Efectos Base:** [`Assets/Poderes/Scripts/Core/PowerEffect.cs`](file:///C:/Users/Tomas%20Abrate/Trucardo/Assets/Poderes/Scripts/Core/PowerEffect.cs)
- **Implementación de Efectos:** [`Assets/Poderes/Scripts/Effects/`](file:///C:/Users/Tomas%20Abrate/Trucardo/Assets/Poderes/Scripts/Effects/)
  - `FaconEffect.cs`
  - `MateEffect.cs`
  - `GuampasEffect.cs`
  - `LazoEffect.cs`
  - `GuitarraEffect.cs`
  - `PavaCalienteEffect.cs`
  - `ManoDeDiosEffect.cs`
  - `BoleadorasEffect.cs`
  - `CeiboEffect.cs`
  - `LuzMalaEffect.cs`
  - `BastonPresidencialEffect.cs`
- **Visualizadores de Mesa:** [`Assets/Poderes/Scripts/Core/PowerHandVisualizer.cs`](file:///C:/Users/Tomas%20Abrate/Trucardo/Assets/Poderes/Scripts/Core/PowerHandVisualizer.cs) (cuadrícula 2x2 para hasta 4 poderes acumulados).
- **Línea de Tiempo de Ronda en UI:** [`Assets/Core/Scripts/UI/RoundPowersUI.cs`](file:///C:/Users/Tomas%20Abrate/Trucardo/Assets/Core/Scripts/UI/RoundPowersUI.cs) y [`Assets/Core/Scripts/UI/HUDController.cs`](file:///C:/Users/Tomas%20Abrate/Trucardo/Assets/Core/Scripts/UI/HUDController.cs).
- **ScriptableObjects (Assets):** [`Assets/Poderes/Data/`](file:///C:/Users/Tomas%20Abrate/Trucardo/Assets/Poderes/Data/)
- **Prefabs 3D:** [`Assets/Poderes/Prefabs/`](file:///C:/Users/Tomas%20Abrate/Trucardo/Assets/Poderes/Prefabs/)
- **Panel de Pruebas / Modo Admin:** [`Assets/Poderes/Scripts/Core/PowerAdminController.cs`](file:///C:/Users/Tomas%20Abrate/Trucardo/Assets/Poderes/Scripts/Core/PowerAdminController.cs) (tecla `F1`)
