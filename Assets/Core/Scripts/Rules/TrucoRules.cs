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
    }
}
