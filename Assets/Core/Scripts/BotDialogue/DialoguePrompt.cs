using System.Collections.Generic;
using System.Text;
using Trucardo.Core.Bots;

namespace Trucardo.Core.BotDialogue
{
    public static class DialoguePrompt
    {
        public const string System = "Sos un jugador argentino de Truco en un bar. Truco es el juego de cartas argentino; "
            + "Truco, Retruco, Vale Cuatro y Envido son cantos reales del juego, nunca consejos o trucos. "
            + "Escribí SOLO una frase corta del jugador sobre el evento que ya ocurrió. Usá vos, español rioplatense natural. "
            + "4 a 15 palabras, máximo 20. Sin explicar reglas, inventar acciones, revelar cartas o fuerza real. "
            + "Sin IA, asistente, markdown, etiquetas ni emojis.\n"
            + "Ejemplos de tono:\nAceptaste: Quiero nomás, ahora bancate el canto.\n"
            + "Rechazaste: Esta te la dejo pasar, no te agrandes.\n"
            + "Ganaste: Te avisé que venía brava la mano.\n"
            + "Rival cantó: Mirá vos, hoy viniste con ganas.\n"
            + "Escribí solo la frase, sin comillas ni introducción.";

        public static string User(BotDialogueContext context, BotDialogueEvent ev, IReadOnlyList<string> recent)
        {
            string personality;
            switch (context.Personality)
            {
                case BotPersonality.Conservative: personality = "seco, cauteloso, desconfiado"; break;
                case BotPersonality.Aggressive: personality = "provocador, confiado, desafiante"; break;
                case BotPersonality.Bluffer: personality = "pícaro, ambiguo, chamuyero; ocultá siempre tu fuerza"; break;
                case BotPersonality.Risky: personality = "impulsivo, entusiasta, algo temerario"; break;
                default: personality = "tranquilo, competitivo, sobrio, carga ocasional"; break;
            }
            string[] events = { "inicio de mano", "rival cantó", "vos cantaste", "vos aceptaste",
                "vos rechazaste", "ganaste la mano", "perdiste la mano", "ganaste la partida", "perdiste la partida" };
            string[] calls = { "ninguno", "Envido", "Real Envido", "Falta Envido", "Truco", "Retruco", "Vale Cuatro" };
            string[] decisions = { "ninguna; no anuncies decisiones", "canto ya hecho", "Quiero", "No quiero", "carta ya jugada" };
            var result = new StringBuilder(640);
            result.Append("PERSONALIDAD: ").Append(context.Personality).Append("; ").AppendLine(personality);
            result.Append("EVENTO: ").AppendLine(events[(int)ev.Type]);
            result.Append("CANTO: ").AppendLine(calls[(int)ev.Call]);
            result.Append("DECISIÓN REAL: ").AppendLine(decisions[(int)ev.Decision]);
            result.Append("MARCADOR: vos ").Append(context.BotScore).Append("; rival ").Append(context.OpponentScore).AppendLine();
            result.Append("FASE: ").Append(context.Phase).AppendLine();
            if (context.Personality != BotPersonality.Bluffer)
            {
                result.Append("CONFIANZA PRIVADA (no revelar): ").Append(context.Confidence).AppendLine();
                result.Append("FUERZA PRIVADA (no revelar): Envido ").Append(context.Envido)
                    .Append("; Truco ").Append(context.Truco).AppendLine();
            }
            result.Append("FRASES RECIENTES (no repetir): ");
            for (int i = global::System.Math.Max(0, recent.Count - 3); i < recent.Count; i++)
                result.Append(recent[i]).Append(" | ");
            result.Append("\nUna única frase de mesa, 4 a 15 palabras. No inventes otra acción ni reveles cartas.");
            return result.ToString();
        }
    }
}
