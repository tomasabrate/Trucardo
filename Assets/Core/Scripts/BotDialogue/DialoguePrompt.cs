using System.Collections.Generic;
using System.Text;
using Trucardo.Core.Bots;

namespace Trucardo.Core.BotDialogue
{
    public static class DialoguePrompt
    {
        public const string System = "Sos un gaucho criollo argentino en una pulpería campera jugando al Truco mano a mano. "
            + "El Truco es el juego de cartas argentino; Truco, Retruco, Vale Cuatro y Envido son cantos reales de la partida. "
            + "Escribí SOLO una frase corta del gaucho (3 a 12 palabras), picaresca y natural sobre lo que pasó en la mesa. "
            + "Usá voseo y modismos gauchos auténticos (chamigo, aparcero, canejo, ahura, al bulto, pingo, coraje, bravura). "
            + "Jamás uses español neutro ('tú/has'). Sin explicar reglas, inventar cantos falsos, ni revelar cartas o palos. "
            + "Sin IA, asistente, markdown, emojis ni prefijos como 'Gaucho:'.\n"
            + "Ejemplos de tono criollo:\n"
            + "Carta jugada: ¡Al bulto va, a ver si hace pata!\n"
            + "Aceptaste Truco: ¡Quiero ver si el pingo es tan gaucho!\n"
            + "Rechazaste: Paso de largo, aparcero, no hay apuro.\n"
            + "Rival cantó: ¡Mirá qué gallito se puso el mozo!\n"
            + "Ganaste mano: ¡Para que aprenda a respetar las canas!\n"
            + "Escribí únicamente la frase, directa y sin comillas.";

        public static string User(BotDialogueContext context, BotDialogueEvent ev, IReadOnlyList<string> recent)
        {
            string personality = !string.IsNullOrEmpty(context.PersonaDescription)
                ? context.PersonaDescription
                : DefaultPersonalityDescription(context.Personality);

            string scoreDesc;
            switch (context.ScoreSituation)
            {
                case ScoreSituation.GanandoComodo: scoreDesc = $"Vas ganando cómodo ({context.BotScore} a {context.OpponentScore}). Llevás buena ventaja."; break;
                case ScoreSituation.GanandoAjustado: scoreDesc = $"Vas ganando por poco ({context.BotScore} a {context.OpponentScore}). Partido ajustado."; break;
                case ScoreSituation.PerdiendoAjustado: scoreDesc = $"Vas perdiendo por poco ({context.BotScore} a {context.OpponentScore}). Estás cerca."; break;
                case ScoreSituation.PerdiendoFeo: scoreDesc = $"Vas perdiendo por bastante ({context.BotScore} a {context.OpponentScore}). Hay que remarla."; break;
                case ScoreSituation.DefinicionBuenas: scoreDesc = $"¡Definición en las buenas! ({context.BotScore} a {context.OpponentScore}). Se define el partido."; break;
                default: scoreDesc = $"Van empatados ({context.BotScore} a {context.OpponentScore}). Partido muy parejo."; break;
            }

            string trickDesc;
            switch (context.TrickState)
            {
                case TrickState.GanamosPrimera: trickDesc = "Ganaste la primera mano ('primera ganada, media jugada'). Tenés ventaja."; break;
                case TrickState.PerdimosPrimera: trickDesc = "El rival ganó la primera mano. Estás obligado a ganar esta baza."; break;
                case TrickState.PardaPrimera: trickDesc = "Empardaron la primera ('parda la mejor'). Quien gane esta se lleva todo."; break;
                case TrickState.TerceraDefinitoria: trickDesc = "Tercera mano definitoria. Se define la mano entera."; break;
                default: trickDesc = "Primera mano de la baza, recién arranca el juego."; break;
            }

            string callName = ev.Call == DialogueCall.None ? "ninguno" : ev.Call.ToString();
            string eventDesc;
            switch (ev.Type)
            {
                case BotDialogueEventType.HandStarted: eventDesc = "Comienza una nueva mano. Saludo criollo o desafío inicial."; break;
                case BotDialogueEventType.CardPlayed: eventDesc = "Se jugó una carta en la mesa. Comentario criollo sobre la parada."; break;
                case BotDialogueEventType.OpponentCalled: eventDesc = $"El rival te cantó {callName}. Te está apurando."; break;
                case BotDialogueEventType.BotCalled: eventDesc = $"Vos cantaste {callName} para apretarlo."; break;
                case BotDialogueEventType.BotAccepted: eventDesc = $"Aceptaste el canto ('Quiero'). Vas al frente."; break;
                case BotDialogueEventType.BotRejected: eventDesc = $"Rechazaste el canto ('No quiero'). Dejás pasar el lance."; break;
                case BotDialogueEventType.HandWon: eventDesc = "¡Ganaste la mano y sumaste puntos criollos!"; break;
                case BotDialogueEventType.HandLost: eventDesc = "Perdiste esta mano."; break;
                case BotDialogueEventType.MatchWon: eventDesc = "¡Ganaste el partido completo a 30 puntos! Victoria en la pulpería."; break;
                case BotDialogueEventType.MatchLost: eventDesc = "Perdiste el partido completo."; break;
                default: eventDesc = "Jugada en la mesa."; break;
            }

            var result = new StringBuilder(700);
            result.Append("PERSONA: ").Append(personality).AppendLine();
            result.Append("MARCADOR: ").AppendLine(scoreDesc);
            result.Append("MANO ACTUAL: ").Append(trickDesc).Append(" ").AppendLine(context.Role == PlayerRole.BotEsMano ? "Sos mano." : "Sos pie.");
            result.Append("EVENTO: ").AppendLine(eventDesc);

            if (context.Personality != BotPersonality.Bluffer)
            {
                result.Append("CONFIANZA PRIVADA (no revelar): ").Append(context.Confidence).AppendLine();
                result.Append("FUERZA PRIVADA (no revelar): Envido ").Append(context.Envido)
                    .Append("; Truco ").Append(context.Truco).AppendLine();
            }

            result.Append("FRASES RECIENTES (no repetir): ");
            for (int i = global::System.Math.Max(0, recent.Count - 3); i < recent.Count; i++)
                result.Append(recent[i]).Append(" | ");
            result.Append("\nFrase corta gaucha de mesa (3 a 12 palabras), picaresca y natural. Sin prefijos ni comillas.");
            return result.ToString();
        }

        private static string DefaultPersonalityDescription(BotPersonality p)
        {
            switch (p)
            {
                case BotPersonality.Conservative: return "gaucho desconfiado, seco, de pocas palabras, no arriesga al cuete";
                case BotPersonality.Aggressive: return "gaucho picante, prepotente, desafiante, te busca la lengua";
                case BotPersonality.Bluffer: return "gaucho trampero, zorro viejo, puro chamuyo; ocultá siempre tu juego";
                case BotPersonality.Risky: return "gaucho audaz, impulsivo, atropellado, le gusta el peligro";
                default: return "gaucho sereno, socarrón, buen baqueano de la mesa";
            }
        }
    }
}
