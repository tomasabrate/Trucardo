namespace Trucardo.Core.Rules
{
    /// <summary>Pure legality and stake rules for the supported Envido family.</summary>
    public static class EnvidoRules
    {
        public static bool CanCallFromContext(
            GameState gameState,
            ulong currentTurnId,
            TipoCanto pendingCall,
            ulong lastCallerId,
            ulong actorId,
            int currentRoundIndex,
            int actorPlayedCardCount,
            NivelTruco trucoLevel,
            EstadoEnvido envidoState,
            EnvidoCall call)
        {
            if (!CanCall(envidoState, call) || trucoLevel != NivelTruco.Ninguno)
                return false;

            bool raisingEnvido = gameState == GameState.EsperandoRespuestaCanto
                && pendingCall == TipoCanto.Envido
                && lastCallerId != actorId
                && envidoState != EstadoEnvido.Ninguno;
            if (raisingEnvido) return true;

            bool normalWindow = gameState == GameState.TurnoJugador
                && currentTurnId == actorId
                && pendingCall == TipoCanto.Ninguno;
            bool interruptingInitialTruco = gameState == GameState.EsperandoRespuestaCanto
                && pendingCall == TipoCanto.Truco
                && lastCallerId != actorId;
            if (envidoState != EstadoEnvido.Ninguno || currentRoundIndex != 0)
                return false;

            return (interruptingInitialTruco || normalWindow) && actorPlayedCardCount == 0;
        }

        public static bool CanCall(EstadoEnvido state, EnvidoCall call)
        {
            switch (state)
            {
                case EstadoEnvido.Ninguno:
                case EstadoEnvido.Envido:
                    return call == EnvidoCall.Envido
                        || call == EnvidoCall.RealEnvido
                        || call == EnvidoCall.FaltaEnvido;
                case EstadoEnvido.EnvidoEnvido:
                    return call == EnvidoCall.RealEnvido || call == EnvidoCall.FaltaEnvido;
                case EstadoEnvido.RealEnvido:
                case EstadoEnvido.EnvidoRealEnvido:
                case EstadoEnvido.EnvidoEnvidoRealEnvido:
                    return call == EnvidoCall.FaltaEnvido;
                default:
                    return false;
            }
        }

        public static EstadoEnvido Raise(EstadoEnvido state, EnvidoCall call)
        {
            if (!CanCall(state, call)) return state;
            if (call == EnvidoCall.FaltaEnvido) return EstadoEnvido.FaltaEnvido;

            switch (state)
            {
                case EstadoEnvido.Ninguno:
                    return call == EnvidoCall.Envido
                        ? EstadoEnvido.Envido
                        : EstadoEnvido.RealEnvido;
                case EstadoEnvido.Envido:
                    return call == EnvidoCall.Envido
                        ? EstadoEnvido.EnvidoEnvido
                        : EstadoEnvido.EnvidoRealEnvido;
                case EstadoEnvido.EnvidoEnvido:
                    return EstadoEnvido.EnvidoEnvidoRealEnvido;
                default:
                    return state;
            }
        }

        public static int GetFixedAcceptedPoints(EstadoEnvido state)
        {
            switch (state)
            {
                case EstadoEnvido.Envido: return 2;
                case EstadoEnvido.EnvidoEnvido: return 4;
                case EstadoEnvido.RealEnvido: return 3;
                case EstadoEnvido.EnvidoRealEnvido: return 5;
                case EstadoEnvido.EnvidoEnvidoRealEnvido: return 7;
                default: return 0;
            }
        }

        public static int GetAcceptedPoints(EstadoEnvido state, int scoreTeam1, int scoreTeam2)
        {
            return state == EstadoEnvido.FaltaEnvido
                ? GetFaltaEnvidoPoints(scoreTeam1, scoreTeam2)
                : GetFixedAcceptedPoints(state);
        }

        public static int GetRejectedPoints(int previouslyAcceptedPoints)
        {
            return previouslyAcceptedPoints > 0 ? previouslyAcceptedPoints : 1;
        }

        public static int GetFaltaEnvidoPoints(int scoreTeam1, int scoreTeam2)
        {
            int leaderScore = System.Math.Max(scoreTeam1, scoreTeam2);
            return System.Math.Max(1, 30 - leaderScore);
        }

        public static string GetDisplayName(EstadoEnvido state)
        {
            switch (state)
            {
                case EstadoEnvido.Envido: return "ENVIDO";
                case EstadoEnvido.EnvidoEnvido: return "ENVIDO ENVIDO";
                case EstadoEnvido.RealEnvido: return "REAL ENVIDO";
                case EstadoEnvido.EnvidoRealEnvido: return "ENVIDO REAL ENVIDO";
                case EstadoEnvido.EnvidoEnvidoRealEnvido: return "ENVIDO ENVIDO REAL ENVIDO";
                case EstadoEnvido.FaltaEnvido: return "FALTA ENVIDO";
                default: return "ENVIDO";
            }
        }
    }
}
