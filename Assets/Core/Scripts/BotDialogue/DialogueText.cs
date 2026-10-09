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
            string text = content.Trim().Trim('"', '“', '”');
            if (text.IndexOfAny(new[] { '\n', '\r', '<', '>', '`', '*', '#', ':' }) >= 0) return null;
            string lower = text.ToLowerInvariant();
            if (Regex.IsMatch(lower, @"\b(ia|bot|asistente|modelo|reglas|puntos|cartas|espada|espadas|basto|bastos|oro|oros|copa|copas|think|thinking)\b|\d")) return null;
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
            if (Regex.IsMatch(lower, @"\bquiero\b"))
            {
                bool negative = lower.Contains("no quiero");
                if (negative ? ev.Decision != DialogueDecision.Reject : ev.Decision != DialogueDecision.Accept) return null;
            }
            string[] words = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length < 4 || words.Length > 20) return null;
            return string.Join(" ", words);
        }
    }
}
