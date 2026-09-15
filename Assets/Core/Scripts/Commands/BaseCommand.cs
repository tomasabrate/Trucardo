using Trucardo.Core;
using UnityEngine;

namespace Trucardo.Core.Commands
{
    public abstract class BaseCommand : IGameCommand
    {
        protected GameModel model;

        public BaseCommand(GameModel model)
        {
            this.model = model;
        }

        public abstract void Execute();
        
        protected bool IsPlayer1(ulong clientId)
        {
            return clientId == model.Player1Id;
        }
    }
}
