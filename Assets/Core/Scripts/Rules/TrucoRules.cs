using System.Collections.Generic;

namespace Trucardo.Core.Rules
{
    /// <summary>Pure hand resolution shared by authoritative play and public-information reasoning.</summary>
    public static class TrucoRules
    {
        public static ManoGanadaPor ResolveHand(
            IReadOnlyList<ManoGanadaPor> rounds, int completedRounds, ManoGanadaPor mano)
        {
            if (completedRounds < 2) return ManoGanadaPor.Nadie;
            var first = rounds[0];
            var second = rounds[1];
            if (first == ManoGanadaPor.Nadie || second == ManoGanadaPor.Nadie)
                return ManoGanadaPor.Nadie;
            if (first == ManoGanadaPor.Parda)
                return second == ManoGanadaPor.Parda ? mano : second;
            if (second == ManoGanadaPor.Parda || first == second) return first;
            if (completedRounds < 3 || rounds[2] == ManoGanadaPor.Nadie)
                return ManoGanadaPor.Nadie;
            return rounds[2] == ManoGanadaPor.Parda ? first : rounds[2];
        }

        public static int AcceptedTrucoPoints(NivelTruco level)
        {
            return level == NivelTruco.Ninguno ? 1 : (int)level;
        }

        public static int RaisedTrucoPoints(NivelTruco level)
        {
            return level == NivelTruco.Ninguno ? 2 : System.Math.Min(4, (int)level + 1);
        }

        public static bool CanCallFromContext(
            GameState gameState,
            ulong currentTurnId,
            TipoCanto pendingCall,
            ulong lastCallerId,
            ulong lastTrucoCallerId,
            ulong actorId,
            NivelTruco trucoLevel)
        {
            if (trucoLevel == NivelTruco.Vale4)
                return false;

            bool raisingPendingTruco = gameState == GameState.EsperandoRespuestaCanto
                && pendingCall == TipoCanto.Truco
                && lastCallerId != actorId
                && (trucoLevel == NivelTruco.Ninguno || trucoLevel == NivelTruco.Truco);
            if (raisingPendingTruco)
                return true;

            bool normalWindow = gameState == GameState.TurnoJugador
                && currentTurnId == actorId
                && pendingCall == TipoCanto.Ninguno
                && (trucoLevel == NivelTruco.Ninguno || lastTrucoCallerId != actorId);
            return normalWindow;
        }

        public static NivelTruco GetNextLevel(NivelTruco level)
        {
            switch (level)
            {
                case NivelTruco.Ninguno: return NivelTruco.Truco;
                case NivelTruco.Truco: return NivelTruco.Retruco;
                case NivelTruco.Retruco: return NivelTruco.Vale4;
                default: return NivelTruco.Vale4;
            }
        }

        public static NivelTruco GetPendingTrucoLevel(NivelTruco acceptedLevel)
        {
            return GetNextLevel(acceptedLevel);
        }

        public static NivelTruco GetTargetCallLevel(
            NivelTruco acceptedLevel, GameState gameState, TipoCanto pendingCall)
        {
            if (gameState == GameState.EsperandoRespuestaCanto && pendingCall == TipoCanto.Truco)
                return GetNextLevel(GetPendingTrucoLevel(acceptedLevel));
            return GetNextLevel(acceptedLevel);
        }

        public static string GetDisplayName(NivelTruco level)
        {
            switch (level)
            {
                case NivelTruco.Truco: return "Truco";
                case NivelTruco.Retruco: return "Re Truco";
                case NivelTruco.Vale4: return "Vale Cuatro";
                default: return "Truco";
            }
        }

        public static string GetButtonLabel(NivelTruco targetLevel)
        {
            switch (targetLevel)
            {
                case NivelTruco.Truco: return "[Q] Cantar Truco";
                case NivelTruco.Retruco: return "[Q] ¡Re Truco!";
                case NivelTruco.Vale4: return "[Q] ¡Vale Cuatro!";
                default: return "[Q] ¡Vale Cuatro!";
            }
        }
    }
}
