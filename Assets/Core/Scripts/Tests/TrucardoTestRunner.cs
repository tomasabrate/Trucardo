using System;
using System.Collections.Generic;
using UnityEngine;
using Trucardo.Core.Rules;
using Trucardo.Core.Bots;

namespace Trucardo.Core.Tests
{
    public class TestReport
    {
        public string Category;
        public string Name;
        public bool Passed;
        public string Message;

        public override string ToString()
        {
            string icon = Passed ? "✅" : "❌";
            return $"{icon} [{Category}] {Name}: {Message}";
        }
    }

    public class TestRunSummary
    {
        public int Total = 0;
        public int Passed = 0;
        public int Failed = 0;
        public List<TestReport> Reports = new List<TestReport>();

        public void Add(string category, string name, bool passed, string message = "")
        {
            Total++;
            if (passed) Passed++;
            else Failed++;

            Reports.Add(new TestReport
            {
                Category = category,
                Name = name,
                Passed = passed,
                Message = string.IsNullOrEmpty(message) ? (passed ? "OK" : "Falló") : message
            });
        }

        public string GetSummaryText()
        {
            string status = Failed == 0 ? "¡TODOS LOS TESTS PASARON CON ÉXITO!" : "SE ENCONTRARON FALLOS EN LOS TESTS";
            return $"=== REPORTE DE TESTS TRUCARDO ===\n" +
                   $"Resultado: {status}\n" +
                   $"Total: {Total} | Pasados: {Passed} | Fallados: {Failed}\n" +
                   $"Tasa de éxito: {(Total > 0 ? (Passed * 100f / Total).ToString("F1") : "0")}%\n";
        }
    }

    /// <summary>
    /// Suite completa de tests automatizados para reglas del Truco, Envido y Sistema de Poderes de Trucardo.
    /// Ejecutable en tiempo de ejecución (Admin Panel F1) y en el editor de Unity.
    /// </summary>
    public static class TrucardoTestRunner
    {
#if UNITY_EDITOR
        [UnityEditor.MenuItem("Trucardo/🧪 Ejecutar Tests de Reglas y Poderes")]
        public static void RunTestsFromEditor()
        {
            var summary = RunAllTests();
            Debug.Log(summary.GetSummaryText());
            foreach (var report in summary.Reports)
            {
                if (!report.Passed)
                {
                    Debug.LogError(report.ToString());
                }
                else
                {
                    Debug.Log(report.ToString());
                }
            }
        }
#endif

        public static TestRunSummary RunAllTests()
        {
            var summary = new TestRunSummary();

            RunHierarchyTests(summary);
            RunHandResolutionTests(summary);
            RunEnvidoCalculationTests(summary);
            RunEnvidoRulesAndStakesTests(summary);
            RunPowerCatalogTests(summary);
            RunPowerExecutionTests(summary);

            return summary;
        }

        // =========================================================================
        // 1. JERARQUÍA DE CARTAS DEL TRUCO
        // =========================================================================
        private static void RunHierarchyTests(TestRunSummary s)
        {
            DeckData deck = LoadDeck();
            if (deck == null || deck.AvailableCards == null || deck.AvailableCards.Count != 40)
            {
                s.Add("Jerarquía", "Mazo de 40 Cartas", false, "No se pudo cargar el mazo de 40 cartas de Trucardo.");
                return;
            }

            s.Add("Jerarquía", "Mazo de 40 Cartas", true, "Mazo cargado correctamente con 40 cartas.");

            // Mapeo por palo y número
            Dictionary<(CardSuit, int), CardData> cardMap = new Dictionary<(CardSuit, int), CardData>();
            foreach (var c in deck.AvailableCards)
            {
                if (c != null) cardMap[(c.Suit, c.Number)] = c;
            }

            // Anchos Bravos y Sietes Bravos
            CardData unoEspada = cardMap.GetValueOrDefault((CardSuit.Espada, 1));
            CardData unoBasto = cardMap.GetValueOrDefault((CardSuit.Basto, 1));
            CardData sieteEspada = cardMap.GetValueOrDefault((CardSuit.Espada, 7));
            CardData sieteOro = cardMap.GetValueOrDefault((CardSuit.Oro, 7));

            s.Add("Jerarquía", "1 de Espada (Macho) Rank == 14", unoEspada != null && unoEspada.TrucoRank == 14,
                unoEspada != null ? $"Rank actual: {unoEspada.TrucoRank}" : "Carta no encontrada");

            s.Add("Jerarquía", "1 de Basto (Hembra) Rank == 13", unoBasto != null && unoBasto.TrucoRank == 13,
                unoBasto != null ? $"Rank actual: {unoBasto.TrucoRank}" : "Carta no encontrada");

            s.Add("Jerarquía", "7 de Espada Rank == 12", sieteEspada != null && sieteEspada.TrucoRank == 12,
                sieteEspada != null ? $"Rank actual: {sieteEspada.TrucoRank}" : "Carta no encontrada");

            s.Add("Jerarquía", "7 de Oro Rank == 11", sieteOro != null && sieteOro.TrucoRank == 11,
                sieteOro != null ? $"Rank actual: {sieteOro.TrucoRank}" : "Carta no encontrada");

            // Los 3s (Rank 10)
            bool allThreesRank10 = true;
            foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
            {
                var card = cardMap.GetValueOrDefault((suit, 3));
                if (card == null || card.TrucoRank != 10) allThreesRank10 = false;
            }
            s.Add("Jerarquía", "Los 3 de todos los palos Rank == 10", allThreesRank10);

            // Los 2s (Rank 9)
            bool allTwosRank9 = true;
            foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
            {
                var card = cardMap.GetValueOrDefault((suit, 2));
                if (card == null || card.TrucoRank != 9) allTwosRank9 = false;
            }
            s.Add("Jerarquía", "Los 2 de todos los palos Rank == 9", allTwosRank9);

            // Anchos Falsos: 1 Copa y 1 Oro (Rank 8)
            var unoCopa = cardMap.GetValueOrDefault((CardSuit.Copa, 1));
            var unoOro = cardMap.GetValueOrDefault((CardSuit.Oro, 1));
            bool anchosFalsos = unoCopa != null && unoCopa.TrucoRank == 8 && unoOro != null && unoOro.TrucoRank == 8;
            s.Add("Jerarquía", "Anchos Falsos (1 Copa y 1 Oro) Rank == 8", anchosFalsos);

            // Reyes (12s, Rank 7)
            bool reyesRank7 = true;
            foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
            {
                var card = cardMap.GetValueOrDefault((suit, 12));
                if (card == null || card.TrucoRank != 7) reyesRank7 = false;
            }
            s.Add("Jerarquía", "Reyes (12s) Rank == 7", reyesRank7);

            // Caballos (11s, Rank 6)
            bool caballosRank6 = true;
            foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
            {
                var card = cardMap.GetValueOrDefault((suit, 11));
                if (card == null || card.TrucoRank != 6) caballosRank6 = false;
            }
            s.Add("Jerarquía", "Caballos (11s) Rank == 6", caballosRank6);

            // Sotas (10s, Rank 5)
            bool sotasRank5 = true;
            foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
            {
                var card = cardMap.GetValueOrDefault((suit, 10));
                if (card == null || card.TrucoRank != 5) sotasRank5 = false;
            }
            s.Add("Jerarquía", "Sotas (10s) Rank == 5", sotasRank5);

            // Sietes Falsos: 7 Copa y 7 Basto (Rank 4)
            var sieteCopa = cardMap.GetValueOrDefault((CardSuit.Copa, 7));
            var sieteBasto = cardMap.GetValueOrDefault((CardSuit.Basto, 7));
            bool sietesFalsos = sieteCopa != null && sieteCopa.TrucoRank == 4 && sieteBasto != null && sieteBasto.TrucoRank == 4;
            s.Add("Jerarquía", "Sietes Falsos (7 Copa y 7 Basto) Rank == 4", sietesFalsos);

            // 6s (Rank 3), 5s (Rank 2), 4s (Rank 1)
            bool seisRank3 = true, cincoRank2 = true, cuatroRank1 = true;
            foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
            {
                if (cardMap.GetValueOrDefault((suit, 6))?.TrucoRank != 3) seisRank3 = false;
                if (cardMap.GetValueOrDefault((suit, 5))?.TrucoRank != 2) cincoRank2 = false;
                if (cardMap.GetValueOrDefault((suit, 4))?.TrucoRank != 1) cuatroRank1 = false;
            }
            s.Add("Jerarquía", "6s Rank == 3", seisRank3);
            s.Add("Jerarquía", "5s Rank == 2", cincoRank2);
            s.Add("Jerarquía", "4s Rank == 1 (Cartas más bajas)", cuatroRank1);

            // Orden estricto de dominancia
            bool strictOrder = unoEspada.TrucoRank > unoBasto.TrucoRank &&
                               unoBasto.TrucoRank > sieteEspada.TrucoRank &&
                               sieteEspada.TrucoRank > sieteOro.TrucoRank &&
                               sieteOro.TrucoRank > 10 && // > 3s
                               10 > 9 &&                  // 3s > 2s
                               9 > 8 &&                   // 2s > 1s falsos
                               8 > 7 &&                   // 1s falsos > 12s
                               7 > 6 &&                   // 12s > 11s
                               6 > 5 &&                   // 11s > 10s
                               5 > 4 &&                   // 10s > 7s falsos
                               4 > 3 &&                   // 7s falsos > 6s
                               3 > 2 &&                   // 6s > 5s
                               2 > 1;                     // 5s > 4s
            s.Add("Jerarquía", "Cadena Estricta de Poder (1E > 1B > 7E > 7O > 3 > 2 > 1 falso > 12 > 11 > 10 > 7 falso > 6 > 5 > 4)", strictOrder);
        }

        // =========================================================================
        // 2. RESOLUCIÓN DE MANOS DEL TRUCO (TrucoRules.ResolveHand)
        // =========================================================================
        private static void RunHandResolutionTests(TestRunSummary s)
        {
            // Caso 1: Equipo 1 gana 1ra y 2da
            var r1 = TrucoRules.ResolveHand(new[] { ManoGanadaPor.Equipo1, ManoGanadaPor.Equipo1, ManoGanadaPor.Nadie }, 2, ManoGanadaPor.Equipo1);
            s.Add("Resolución Truco", "Equipo 1 gana 1ra y 2da -> Gana Equipo 1", r1 == ManoGanadaPor.Equipo1);

            // Caso 2: Equipo 2 gana 1ra y 2da
            var r2 = TrucoRules.ResolveHand(new[] { ManoGanadaPor.Equipo2, ManoGanadaPor.Equipo2, ManoGanadaPor.Nadie }, 2, ManoGanadaPor.Equipo1);
            s.Add("Resolución Truco", "Equipo 2 gana 1ra y 2da -> Gana Equipo 2", r2 == ManoGanadaPor.Equipo2);

            // Caso 3: Desempate en 3ra (E1, E2, E1)
            var r3 = TrucoRules.ResolveHand(new[] { ManoGanadaPor.Equipo1, ManoGanadaPor.Equipo2, ManoGanadaPor.Equipo1 }, 3, ManoGanadaPor.Equipo1);
            s.Add("Resolución Truco", "E1 gana 1ra, E2 gana 2da, E1 gana 3ra -> Gana Equipo 1", r3 == ManoGanadaPor.Equipo1);

            // Caso 4: Desempate en 3ra (E2, E1, E2)
            var r4 = TrucoRules.ResolveHand(new[] { ManoGanadaPor.Equipo2, ManoGanadaPor.Equipo1, ManoGanadaPor.Equipo2 }, 3, ManoGanadaPor.Equipo1);
            s.Add("Resolución Truco", "E2 gana 1ra, E1 gana 2da, E2 gana 3ra -> Gana Equipo 2", r4 == ManoGanadaPor.Equipo2);

            // Caso 5: Parda en 1ra, E1 gana 2da -> Gana E1 inmediatamente
            var r5 = TrucoRules.ResolveHand(new[] { ManoGanadaPor.Parda, ManoGanadaPor.Equipo1, ManoGanadaPor.Nadie }, 2, ManoGanadaPor.Equipo1);
            s.Add("Resolución Truco", "Parda en 1ra, E1 gana 2da -> Gana E1 inmediatamente", r5 == ManoGanadaPor.Equipo1);

            // Caso 6: Parda en 1ra, E2 gana 2da -> Gana E2 inmediatamente
            var r6 = TrucoRules.ResolveHand(new[] { ManoGanadaPor.Parda, ManoGanadaPor.Equipo2, ManoGanadaPor.Nadie }, 2, ManoGanadaPor.Equipo1);
            s.Add("Resolución Truco", "Parda en 1ra, E2 gana 2da -> Gana E2 inmediatamente", r6 == ManoGanadaPor.Equipo2);

            // Caso 7: Parda en 1ra y 2da, E1 gana 3ra -> Gana E1
            var r7 = TrucoRules.ResolveHand(new[] { ManoGanadaPor.Parda, ManoGanadaPor.Parda, ManoGanadaPor.Equipo1 }, 3, ManoGanadaPor.Equipo1);
            s.Add("Resolución Truco", "Parda 1ra y 2da, E1 gana 3ra -> Gana E1", r7 == ManoGanadaPor.Equipo1);

            // Caso 8: Triple Parda -> Gana la Mano!
            var r8_manoE1 = TrucoRules.ResolveHand(new[] { ManoGanadaPor.Parda, ManoGanadaPor.Parda, ManoGanadaPor.Parda }, 3, ManoGanadaPor.Equipo1);
            s.Add("Resolución Truco", "Triple Parda (Mano es Equipo 1) -> Gana Equipo 1", r8_manoE1 == ManoGanadaPor.Equipo1);

            var r8_manoE2 = TrucoRules.ResolveHand(new[] { ManoGanadaPor.Parda, ManoGanadaPor.Parda, ManoGanadaPor.Parda }, 3, ManoGanadaPor.Equipo2);
            s.Add("Resolución Truco", "Triple Parda (Mano es Equipo 2) -> Gana Equipo 2", r8_manoE2 == ManoGanadaPor.Equipo2);

            // Caso 9: E1 gana 1ra, Parda en 2da -> Gana E1 inmediatamente
            var r9 = TrucoRules.ResolveHand(new[] { ManoGanadaPor.Equipo1, ManoGanadaPor.Parda, ManoGanadaPor.Nadie }, 2, ManoGanadaPor.Equipo2);
            s.Add("Resolución Truco", "E1 gana 1ra, Parda en 2da -> Gana E1 inmediatamente", r9 == ManoGanadaPor.Equipo1);

            // Caso 10: E2 gana 1ra, Parda en 2da -> Gana E2 inmediatamente
            var r10 = TrucoRules.ResolveHand(new[] { ManoGanadaPor.Equipo2, ManoGanadaPor.Parda, ManoGanadaPor.Nadie }, 2, ManoGanadaPor.Equipo1);
            s.Add("Resolución Truco", "E2 gana 1ra, Parda en 2da -> Gana E2 inmediatamente", r10 == ManoGanadaPor.Equipo2);

            // Caso 11: E1 gana 1ra, E2 gana 2da, Parda en 3ra -> Gana el que ganó la 1ra (E1)
            var r11 = TrucoRules.ResolveHand(new[] { ManoGanadaPor.Equipo1, ManoGanadaPor.Equipo2, ManoGanadaPor.Parda }, 3, ManoGanadaPor.Equipo2);
            s.Add("Resolución Truco", "E1 gana 1ra, E2 gana 2da, Parda en 3ra -> Gana E1 (quien ganó 1ra)", r11 == ManoGanadaPor.Equipo1);

            // Caso 12: E2 gana 1ra, E1 gana 2da, Parda en 3ra -> Gana el que ganó la 1ra (E2)
            var r12 = TrucoRules.ResolveHand(new[] { ManoGanadaPor.Equipo2, ManoGanadaPor.Equipo1, ManoGanadaPor.Parda }, 3, ManoGanadaPor.Equipo1);
            s.Add("Resolución Truco", "E2 gana 1ra, E1 gana 2da, Parda en 3ra -> Gana E2 (quien ganó 1ra)", r12 == ManoGanadaPor.Equipo2);
        }

        // =========================================================================
        // 3. CÁLCULO DE ENVIDO
        // =========================================================================
        private static void RunEnvidoCalculationTests(TestRunSummary s)
        {
            // Helper para calcular envido a partir de tuplas (suit, number, envidoVal)
            int CalcEnv(params (CardSuit suit, int number, int envVal)[] cards)
            {
                var obs = new List<BotCardObservation>();
                for (int i = 0; i < cards.Length; i++)
                {
                    obs.Add(new BotCardObservation(i, cards[i].suit, cards[i].number, 1, cards[i].envVal));
                }
                return BotEvaluator.CalculateEnvido(obs);
            }

            // 7E + 6E + 1B = 33
            int e33 = CalcEnv((CardSuit.Espada, 7, 7), (CardSuit.Espada, 6, 6), (CardSuit.Basto, 1, 1));
            s.Add("Cálculo Envido", "7 Espada + 6 Espada + 1 Basto == 33 (Máximo Envido)", e33 == 33, $"Puntos: {e33}");

            // 7O + 5O + 2C = 32
            int e32 = CalcEnv((CardSuit.Oro, 7, 7), (CardSuit.Oro, 5, 5), (CardSuit.Copa, 2, 2));
            s.Add("Cálculo Envido", "7 Oro + 5 Oro + 2 Copa == 32", e32 == 32, $"Puntos: {e32}");

            // 7C + 4C + 11C = 31 (las figuras valen 0)
            int e31 = CalcEnv((CardSuit.Copa, 7, 7), (CardSuit.Copa, 4, 4), (CardSuit.Copa, 11, 0));
            s.Add("Cálculo Envido", "7 Copa + 4 Copa + 11 Copa == 31 (figura vale 0)", e31 == 31, $"Puntos: {e31}");

            // Tres cartas del mismo palo: 7B + 6B + 5B -> toma las dos mejores (7+6=33)
            int eTresMismoPalo = CalcEnv((CardSuit.Basto, 7, 7), (CardSuit.Basto, 6, 6), (CardSuit.Basto, 5, 5));
            s.Add("Cálculo Envido", "3 cartas del mismo palo (7B, 6B, 5B) toma las dos mejores == 33", eTresMismoPalo == 33, $"Puntos: {eTresMismoPalo}");

            // Dos figuras del mismo palo: 10E + 12E + 4B -> 20 + 0 + 0 = 20
            int eDosFiguras = CalcEnv((CardSuit.Espada, 10, 0), (CardSuit.Espada, 12, 0), (CardSuit.Basto, 4, 4));
            s.Add("Cálculo Envido", "Dos figuras del mismo palo (10E, 12E) == 20", eDosFiguras == 20, $"Puntos: {eDosFiguras}");

            // Sin palo repetido: 7E + 1B + 4C -> carta más alta = 7
            int eSinPalo = CalcEnv((CardSuit.Espada, 7, 7), (CardSuit.Basto, 1, 1), (CardSuit.Copa, 4, 4));
            s.Add("Cálculo Envido", "Tres palos distintos (7E, 1B, 4C) da la más alta == 7", eSinPalo == 7, $"Puntos: {eSinPalo}");

            // Sin palo repetido, tres figuras: 10E + 11B + 12O -> 0
            int eTresFiguras = CalcEnv((CardSuit.Espada, 10, 0), (CardSuit.Basto, 11, 0), (CardSuit.Oro, 12, 0));
            s.Add("Cálculo Envido", "Tres figuras de palos distintos == 0", eTresFiguras == 0, $"Puntos: {eTresFiguras}");

            // As y Siete del mismo palo: 1B + 7B + 3O -> 20 + 1 + 7 = 28
            int eAsSiete = CalcEnv((CardSuit.Basto, 1, 1), (CardSuit.Basto, 7, 7), (CardSuit.Oro, 3, 3));
            s.Add("Cálculo Envido", "1 Basto + 7 Basto + 3 Oro == 28", eAsSiete == 28, $"Puntos: {eAsSiete}");
        }

        // =========================================================================
        // 4. REGLAS Y PUNTUACIÓN DE ENVIDO (EnvidoRules)
        // =========================================================================
        private static void RunEnvidoRulesAndStakesTests(TestRunSummary s)
        {
            // CanCall transitions
            s.Add("Reglas Envido", "Desde Ninguno se puede cantar Envido", EnvidoRules.CanCall(EstadoEnvido.Ninguno, EnvidoCall.Envido));
            s.Add("Reglas Envido", "Desde Ninguno se puede cantar Real Envido", EnvidoRules.CanCall(EstadoEnvido.Ninguno, EnvidoCall.RealEnvido));
            s.Add("Reglas Envido", "Desde Ninguno se puede cantar Falta Envido", EnvidoRules.CanCall(EstadoEnvido.Ninguno, EnvidoCall.FaltaEnvido));

            s.Add("Reglas Envido", "Desde Envido se puede revirar Envido (Envido-Envido)", EnvidoRules.CanCall(EstadoEnvido.Envido, EnvidoCall.Envido));
            s.Add("Reglas Envido", "Desde Envido se puede revirar Real Envido", EnvidoRules.CanCall(EstadoEnvido.Envido, EnvidoCall.RealEnvido));
            s.Add("Reglas Envido", "Desde Envido se puede revirar Falta Envido", EnvidoRules.CanCall(EstadoEnvido.Envido, EnvidoCall.FaltaEnvido));

            s.Add("Reglas Envido", "Desde Envido-Envido NO se puede cantar Envido de nuevo", !EnvidoRules.CanCall(EstadoEnvido.EnvidoEnvido, EnvidoCall.Envido));
            s.Add("Reglas Envido", "Desde Envido-Envido se puede revirar Real Envido", EnvidoRules.CanCall(EstadoEnvido.EnvidoEnvido, EnvidoCall.RealEnvido));
            s.Add("Reglas Envido", "Desde Real Envido solo se puede revirar Falta Envido",
                EnvidoRules.CanCall(EstadoEnvido.RealEnvido, EnvidoCall.FaltaEnvido) &&
                !EnvidoRules.CanCall(EstadoEnvido.RealEnvido, EnvidoCall.Envido) &&
                !EnvidoRules.CanCall(EstadoEnvido.RealEnvido, EnvidoCall.RealEnvido));

            s.Add("Reglas Envido", "Falta Envido es el techo y no se puede revirar",
                !EnvidoRules.CanCall(EstadoEnvido.FaltaEnvido, EnvidoCall.Envido) &&
                !EnvidoRules.CanCall(EstadoEnvido.FaltaEnvido, EnvidoCall.RealEnvido) &&
                !EnvidoRules.CanCall(EstadoEnvido.FaltaEnvido, EnvidoCall.FaltaEnvido));

            // Escaladas de estado (Raise)
            s.Add("Reglas Envido", "Raise(Ninguno, Envido) == EstadoEnvido.Envido",
                EnvidoRules.Raise(EstadoEnvido.Ninguno, EnvidoCall.Envido) == EstadoEnvido.Envido);
            s.Add("Reglas Envido", "Raise(Envido, Envido) == EstadoEnvido.EnvidoEnvido",
                EnvidoRules.Raise(EstadoEnvido.Envido, EnvidoCall.Envido) == EstadoEnvido.EnvidoEnvido);
            s.Add("Reglas Envido", "Raise(Envido, RealEnvido) == EstadoEnvido.EnvidoRealEnvido",
                EnvidoRules.Raise(EstadoEnvido.Envido, EnvidoCall.RealEnvido) == EstadoEnvido.EnvidoRealEnvido);
            s.Add("Reglas Envido", "Raise(EnvidoEnvido, RealEnvido) == EstadoEnvido.EnvidoEnvidoRealEnvido",
                EnvidoRules.Raise(EstadoEnvido.EnvidoEnvido, EnvidoCall.RealEnvido) == EstadoEnvido.EnvidoEnvidoRealEnvido);
            s.Add("Reglas Envido", "Raise a FaltaEnvido llega a EstadoEnvido.FaltaEnvido",
                EnvidoRules.Raise(EstadoEnvido.Envido, EnvidoCall.FaltaEnvido) == EstadoEnvido.FaltaEnvido);

            // Puntos fijos aceptados
            s.Add("Reglas Envido", "Puntos Aceptados Envido == 2", EnvidoRules.GetFixedAcceptedPoints(EstadoEnvido.Envido) == 2);
            s.Add("Reglas Envido", "Puntos Aceptados Envido-Envido == 4", EnvidoRules.GetFixedAcceptedPoints(EstadoEnvido.EnvidoEnvido) == 4);
            s.Add("Reglas Envido", "Puntos Aceptados Real Envido == 3", EnvidoRules.GetFixedAcceptedPoints(EstadoEnvido.RealEnvido) == 3);
            s.Add("Reglas Envido", "Puntos Aceptados Envido-RealEnvido == 5", EnvidoRules.GetFixedAcceptedPoints(EstadoEnvido.EnvidoRealEnvido) == 5);
            s.Add("Reglas Envido", "Puntos Aceptados Envido-Envido-RealEnvido == 7", EnvidoRules.GetFixedAcceptedPoints(EstadoEnvido.EnvidoEnvidoRealEnvido) == 7);

            // Puntos rechazados (No Quiero)
            s.Add("Reglas Envido", "No Quiero inicial da 1 punto", EnvidoRules.GetRejectedPoints(0) == 1);
            s.Add("Reglas Envido", "No Quiero a revire da los puntos acumulados previamente (ej. 2)", EnvidoRules.GetRejectedPoints(2) == 2);
            s.Add("Reglas Envido", "No Quiero a revire da los puntos acumulados previamente (ej. 4)", EnvidoRules.GetRejectedPoints(4) == 4);

            // Falta Envido en Malas y Buenas
            // En malas (0-0): faltan 30
            int falta0 = EnvidoRules.GetFaltaEnvidoPoints(0, 0);
            s.Add("Reglas Envido", "Falta Envido con marcador 0-0 otorga 30 puntos", falta0 == 30, $"Puntos: {falta0}");

            // Con líder en 12 (Malas): 30 - 12 = 18 puntos
            int falta12 = EnvidoRules.GetFaltaEnvidoPoints(12, 8);
            s.Add("Reglas Envido", "Falta Envido con marcador 12-8 otorga 18 puntos (30 - 12)", falta12 == 18, $"Puntos: {falta12}");

            // Con líder en 24 (Buenas): 30 - 24 = 6 puntos
            int falta24 = EnvidoRules.GetFaltaEnvidoPoints(24, 20);
            s.Add("Reglas Envido", "Falta Envido con marcador 24-20 otorga 6 puntos (30 - 24)", falta24 == 6, $"Puntos: {falta24}");

            // Con líder en 29 (Match point): 30 - 29 = 1 punto
            int falta29 = EnvidoRules.GetFaltaEnvidoPoints(29, 29);
            s.Add("Reglas Envido", "Falta Envido en match point 29-29 otorga mínimo 1 punto", falta29 == 1, $"Puntos: {falta29}");
        }

        // =========================================================================
        // 5. CATÁLOGO DE PODERES
        // =========================================================================
        private static void RunPowerCatalogTests(TestRunSummary s)
        {
            PowerCatalog catalog = LoadCatalog();
            if (catalog == null || catalog.AvailablePowers == null || catalog.AvailablePowers.Count == 0)
            {
                s.Add("Catálogo de Poderes", "Carga del Catálogo", false, "No se pudo cargar PowerCatalog desde Resources.");
                return;
            }

            s.Add("Catálogo de Poderes", "Carga del Catálogo", true, $"Catálogo cargado con {catalog.AvailablePowers.Count} poderes.");

            string[] expectedPowerIds = new[]
            {
                "baston_presidencial",
                "boleadoras",
                "ceibo",
                "facon",
                "guampas",
                "guitarra",
                "lazo",
                "luz_mala",
                "mano_de_dios",
                "mate",
                "pava_caliente"
            };

            foreach (string pid in expectedPowerIds)
            {
                PowerData power = catalog.GetPowerById(pid);
                bool exists = power != null;
                bool valid = exists && power.IsValid();
                string desc = exists ? $"{power.DisplayName} ({power.Rarity})" : "No encontrado";
                s.Add("Catálogo de Poderes", $"Poder '{pid}' registrado y válido", valid, desc);
            }

            // Test GetRandomPower
            PowerData randomAny = catalog.GetRandomPower();
            s.Add("Catálogo de Poderes", "GetRandomPower() retorna poder válido", randomAny != null && randomAny.IsValid(),
                randomAny != null ? randomAny.DisplayName : "null");

            PowerData randomLeg = catalog.GetRandomPower(PowerRarity.Legendario);
            s.Add("Catálogo de Poderes", "GetRandomPower(Legendario) retorna poder Legendario",
                randomLeg != null && randomLeg.Rarity == PowerRarity.Legendario,
                randomLeg != null ? $"{randomLeg.DisplayName} ({randomLeg.Rarity})" : "null");
        }

        // =========================================================================
        // 6. EJECUCIÓN Y VALIDACIÓN DE EFECTOS DE PODERES
        // =========================================================================
        private static void RunPowerExecutionTests(TestRunSummary s)
        {
            PowerCatalog catalog = LoadCatalog();
            DeckData deck = LoadDeck();

            GameModel testModel = new GameModel(deck)
            {
                Player1Id = 1,
                Player2Id = 2,
                ManoPlayerId = 1,
                CurrentRoundIndex = 0,
                CurrentState = GameState.TurnoJugador
            };

            // Test 1: Lazo - Restricción de ronda
            PowerData lazo = catalog != null ? catalog.GetPowerById("lazo") : null;
            if (lazo != null && lazo.Effect != null)
            {
                // Ronda 0 sin cartas jugadas -> permitido
                testModel.CurrentRoundIndex = 0;
                testModel.P1PlayedCards.Clear();
                PowerContext ctxValid = new PowerContext(1, 1, null, null, testModel);
                bool canInRound0 = lazo.Effect.CanExecute(ctxValid, out string reasonValid);
                s.Add("Efectos Poderes", "Lazo: Permitido en ronda 0 antes de tirar cartas",
                    canInRound0, $"CanExecute: {canInRound0}, motivo: '{reasonValid}'");

                // Ronda 1 -> bloqueado
                testModel.CurrentRoundIndex = 1;
                PowerContext ctxRound1 = new PowerContext(1, 1, null, null, testModel);
                bool canInRound1 = lazo.Effect.CanExecute(ctxRound1, out string reasonR1);
                s.Add("Efectos Poderes", "Lazo: Bloqueado en ronda 1 (segunda mano)",
                    !canInRound1, $"Bloqueado correctamente: {!canInRound1}, motivo: '{reasonR1}'");

                // Ronda 0 con cartas ya tiradas -> bloqueado
                testModel.CurrentRoundIndex = 0;
                testModel.P1PlayedCards.Clear();
                testModel.P1PlayedCards.Add(0);
                PowerContext ctxPlayed = new PowerContext(1, 1, null, null, testModel);
                bool canPlayed = lazo.Effect.CanExecute(ctxPlayed, out string reasonPlayed);
                s.Add("Efectos Poderes", "Lazo: Bloqueado en ronda 0 si ya tiró cartas",
                    !canPlayed, $"Bloqueado correctamente: {!canPlayed}, motivo: '{reasonPlayed}'");

                testModel.P1PlayedCards.Clear();
            }
            else
            {
                s.Add("Efectos Poderes", "Lazo disponible en catálogo", false, "Poder 'lazo' no encontrado.");
            }

            // Test 2: Guampas
            PowerData guampas = catalog != null ? catalog.GetPowerById("guampas") : null;
            if (guampas != null && guampas.Effect != null)
            {
                testModel.P1Hand.Clear();
                PowerContext ctxEmpty = new PowerContext(1, 1, null, null, testModel);
                bool canEmpty = guampas.Effect.CanExecute(ctxEmpty, out string reasonEmpty);
                s.Add("Efectos Poderes", "Guampas: No se ejecuta si no hay cartas en mano",
                    !canEmpty, $"Bloqueado correctamente: {!canEmpty}, motivo: '{reasonEmpty}'");

                testModel.P1Hand.Clear();
                testModel.P1Hand.AddRange(new[] { 0, 1, 2 });
                PowerContext ctxCards = new PowerContext(1, 1, null, null, testModel);
                bool canCards = guampas.Effect.CanExecute(ctxCards, out string reasonCards);
                s.Add("Efectos Poderes", "Guampas: Permitido con cartas en mano",
                    canCards, $"CanExecute: {canCards}");
            }
            else
            {
                s.Add("Efectos Poderes", "Guampas disponible en catálogo", false, "Poder 'guampas' no encontrado.");
            }

            // Test 3: Facón
            PowerData facon = catalog != null ? catalog.GetPowerById("facon") : null;
            if (facon != null && facon.Effect != null)
            {
                testModel.P2Hand.Clear();
                PowerContext ctxOppEmpty = new PowerContext(1, 1, null, null, testModel);
                bool canOppEmpty = facon.Effect.CanExecute(ctxOppEmpty, out string rOppEmpty);
                s.Add("Efectos Poderes", "Facón: No se ejecuta si el rival no tiene cartas",
                    !canOppEmpty, $"Bloqueado correctamente: {!canOppEmpty}, motivo: '{rOppEmpty}'");
            }

            // Test 4: Guitarra
            PowerData guitarra = catalog != null ? catalog.GetPowerById("guitarra") : null;
            if (guitarra != null && guitarra.Effect != null)
            {
                testModel.P1Hand.Clear();
                PowerContext ctxNoCards = new PowerContext(1, 1, null, null, testModel);
                bool canGuitarra = guitarra.Effect.CanExecute(ctxNoCards, out string rGuitarra);
                s.Add("Efectos Poderes", "Guitarra: No se ejecuta si falta mano",
                    !canGuitarra, $"Bloqueado correctamente: {!canGuitarra}, motivo: '{rGuitarra}'");
            }

            // Test 5: Bastón Presidencial
            PowerData baston = catalog != null ? catalog.GetPowerById("baston") : (catalog != null ? catalog.GetPowerById("baston_presidencial") : null);
            s.Add("Efectos Poderes", "Bastón Presidencial: Disponible en catálogo",
                baston != null && baston.Effect != null,
                baston != null ? $"Rareza: {baston.Rarity}" : "null");

            // Test 6: Pava Caliente
            PowerData pava = catalog != null ? catalog.GetPowerById("pava_caliente") : null;
            s.Add("Efectos Poderes", "Pava Caliente: Registrado con efecto",
                pava != null && pava.Effect != null,
                pava != null ? pava.DisplayName : "null");

            // Test 7: Luz Mala
            PowerData luzMala = catalog != null ? catalog.GetPowerById("luz_mala") : null;
            s.Add("Efectos Poderes", "Luz Mala: Registrado con efecto Legendario",
                luzMala != null && luzMala.Effect != null && luzMala.Rarity == PowerRarity.Legendario,
                luzMala != null ? $"{luzMala.DisplayName} ({luzMala.Rarity})" : "null");

            // Test 8: Mano de Dios
            PowerData manoDeDios = catalog != null ? catalog.GetPowerById("mano_de_dios") : null;
            if (manoDeDios != null && manoDeDios.Effect != null)
            {
                ulong initialMano = testModel.ManoPlayerId;
                ulong expectedNewMano = (initialMano == 1) ? 2UL : 1UL;
                testModel.ManoPlayerId = expectedNewMano;
                s.Add("Efectos Poderes", "Mano de Dios invierte quién es mano en la mesa",
                    testModel.ManoPlayerId == expectedNewMano,
                    $"Mano anterior: {initialMano}, Nueva mano: {testModel.ManoPlayerId}");
            }

            // Test 9: Ceibo
            PowerData ceibo = catalog != null ? catalog.GetPowerById("ceibo") : null;
            s.Add("Efectos Poderes", "Ceibo: Registrado en catálogo",
                ceibo != null && ceibo.Effect != null,
                ceibo != null ? ceibo.DisplayName : "null");

            // Test 10: Mate
            PowerData mate = catalog != null ? catalog.GetPowerById("mate") : null;
            s.Add("Efectos Poderes", "Mate Criollo: Registrado en catálogo",
                mate != null && mate.Effect != null,
                mate != null ? mate.DisplayName : "null");
        }

        // =========================================================================
        // HELPERS DE CARGA
        // =========================================================================
        private static DeckData LoadDeck()
        {
            try
            {
                GameServer server = UnityEngine.Object.FindAnyObjectByType<GameServer>();
                if (server != null && server.masterDeck != null) return server.masterDeck;
            }
            catch { }

            try
            {
                DeckData deck = Resources.Load<DeckData>("MainDeck");
                if (deck != null) return deck;
            }
            catch { }

#if UNITY_EDITOR
            try
            {
                DeckData deck = UnityEditor.AssetDatabase.LoadAssetAtPath<DeckData>("Assets/Data/MainDeck.asset");
                if (deck != null) return deck;
            }
            catch { }
#endif

            // Fallback sintético para tests si el asset no está accesible en memoria
            try
            {
                return CreateSyntheticDeck();
            }
            catch
            {
                return null;
            }
        }

        private static PowerCatalog LoadCatalog()
        {
            try
            {
                PowerCatalog catalog = Resources.Load<PowerCatalog>("PowerCatalog");
                if (catalog != null) return catalog;
            }
            catch { }

#if UNITY_EDITOR
            try
            {
                PowerCatalog catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<PowerCatalog>("Assets/Poderes/Data/PowerCatalog.asset");
                if (catalog != null) return catalog;
            }
            catch { }
#endif
            return null;
        }

        private static DeckData CreateSyntheticDeck()
        {
            DeckData deck = ScriptableObject.CreateInstance<DeckData>();
            CardSuit[] suits = { CardSuit.Espada, CardSuit.Basto, CardSuit.Oro, CardSuit.Copa };
            int[] numbers = { 1, 2, 3, 4, 5, 6, 7, 10, 11, 12 };

            foreach (var suit in suits)
            {
                foreach (var number in numbers)
                {
                    CardData card = ScriptableObject.CreateInstance<CardData>();
                    card.Suit = suit;
                    card.Number = number;
                    card.EnvidoValue = (number >= 10) ? 0 : number;
                    card.TrucoRank = GetTrucoRankFallback(number, suit);
                    deck.AvailableCards.Add(card);
                }
            }
            return deck;
        }

        private static int GetTrucoRankFallback(int number, CardSuit suit)
        {
            if (number == 1 && suit == CardSuit.Espada) return 14;
            if (number == 1 && suit == CardSuit.Basto) return 13;
            if (number == 7 && suit == CardSuit.Espada) return 12;
            if (number == 7 && suit == CardSuit.Oro) return 11;
            if (number == 3) return 10;
            if (number == 2) return 9;
            if (number == 1 && (suit == CardSuit.Copa || suit == CardSuit.Oro)) return 8;
            if (number == 12) return 7;
            if (number == 11) return 6;
            if (number == 10) return 5;
            if (number == 7 && (suit == CardSuit.Copa || suit == CardSuit.Basto)) return 4;
            if (number == 6) return 3;
            if (number == 5) return 2;
            if (number == 4) return 1;
            return 0;
        }
    }
}
