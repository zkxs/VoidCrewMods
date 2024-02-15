using System;

namespace RecentPlayers
{
    [Serializable]
    internal class ReflectionException : Exception
    {
        public ReflectionException(string message) : base(message)
        {
        }
    }
}
