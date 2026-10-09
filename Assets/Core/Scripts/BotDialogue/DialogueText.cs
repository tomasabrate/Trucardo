using System;
using System.Text.RegularExpressions;

namespace Trucardo.Core.BotDialogue
{
    public static class DialogueText
    {
        public static string Normalize(string text) => Regex.Replace(text.ToLowerInvariant(), @"[^\p{L}\p{N}]", "");

        public static string Filter(string content, BotDialogueEvent ev)
        {
            if (string.IsNullOrWhiteSpace(content) || content.Length > 240) return null;

            // Take the first non-empty line to handle leading/trailing newlines gracefully
            string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0) return null;

            string text = lines[0].Trim().Trim('"', '“', '”', '\'', '«', '»');
            // Sanitize common LLM speaker prefixes like "Gaucho: ", "Bot: ", "Jugador: "
            text = Regex.Replace(text, @"^(gaucho|bot|jugador|rival|yo|respuesta)\s*:\s*", "", RegexOptions.IgnoreCase).Trim();
            // Sanitize roleplay actions like *se acomoda el sombrero*
            text = Regex.Replace(text, @"\*[^*]+\*", "").Trim();
            text = text.Trim('"', '“', '”', '\'', '`');

            if (string.IsNullOrWhiteSpace(text)) return null;
            if (text.IndexOfAny(new[] { '<', '>', '`', '#', ':' }) >= 0) return null;

            string lower = text.ToLowerInvariant();
            if (Regex.IsMatch(lower, @"\b(ia|bot|asistente|modelo|reglas|espada|espadas|basto|bastos|oro|oros|copa|copas|think|thinking)\b|\d")) return null;

            // Reject a contradictory or invented call, rather than trying to execute/interpret it.
            bool hasCall = Regex.IsMatch(lower, @"\b(envido|truco|retruco|vale cuatro)\b");
            if (hasCall)
            {
                string allowed = ev.Call == DialogueCall.Envido ? "envido" : ev.Call == DialogueCall.RealEnvido ? "real envido"
                    : ev.Call == DialogueCall.FaltaEnvido ? "falta envido" : ev.Call == DialogueCall.Truco ? "truco"
                    : ev.Call == DialogueCall.Retruco ? "retruco" : ev.Call == DialogueCall.ValeCuatro ? "vale cuatro" : null;
                if (allowed == null || !lower.Contains(allowed)) return null;
                string remainder = lower.Replace(allowed, "");
                if (Regex.IsMatch(remainder, @"\b(envido|truco|retruco|vale cuatro|real|falta)\b")) return null;
            }

            if (ev.Decision == DialogueDecision.Accept)
            {
                if (lower.Contains("no quiero")) return null;
            }
            else if (ev.Decision == DialogueDecision.Reject)
            {
                if (Regex.IsMatch(lower, @"\bquiero\b") && !lower.Contains("no quiero")) return null;
            }
            else
            {
                // If not answering a call, reject standalone decision announcements
                if (Regex.IsMatch(lower, @"^(no\s+quiero|quiero)(\b|[!,\.])")) return null;
            }

            string[] words = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length < 2 || words.Length > 20) return null;
            return string.Join(" ", words);
        }
    }
}
