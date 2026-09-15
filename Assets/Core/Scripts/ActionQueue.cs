using System.Collections.Generic;

namespace Trucardo.Core
{
    public interface IGameCommand
    {
        void Execute();
    }

    /// <summary>
    /// Cola de Acciones (Command Queue).
    /// El servidor encola y resuelve comandos secuencialmente para evitar código espagueti.
    /// </summary>
    public class ActionQueue
    {
        private Queue<IGameCommand> commandQueue = new Queue<IGameCommand>();

        public void EnqueueCommand(IGameCommand command)
        {
            commandQueue.Enqueue(command);
        }

        public void ProcessNext()
        {
            if (commandQueue.Count > 0)
            {
                IGameCommand command = commandQueue.Dequeue();
                command.Execute();
            }
        }

        public bool HasCommands => commandQueue.Count > 0;
    }
}
